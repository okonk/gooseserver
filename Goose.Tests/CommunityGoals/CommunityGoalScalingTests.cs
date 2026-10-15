using Goose.CommunityGoals;
using Goose.Testing;
using Xunit;

namespace Goose.Tests.CommunityGoals
{
    [Collection("NLog")]
    public class CommunityGoalScalingTests : IDisposable
    {
        private readonly TestWorldFixture fixture;
        private readonly GameWorld world;
        private readonly CommunityGoalHandler handler;
        private readonly DateTime started = new(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        private readonly List<CommunityGoalRun> runs = new();
        private DateTime now;

        public CommunityGoalScalingTests()
        {
            fixture = new TestWorldFixture();
            world = fixture.World;
            handler = world.CommunityGoals;
            now = started;
            handler.UtcNow = () => now;
        }

        public void Dispose() => fixture.Dispose();

        private CommunityGoal LoadGoal(string properties)
        {
            handler.LoadDefinitionRows(new[] { new CommunityGoal { Id = 1, Name = "Goal 1", PropertiesJson = properties } }, world);
            return handler.Goals[1];
        }

        private CommunityGoalRun AddRun(int run, RunStatus status)
        {
            var runObj = new CommunityGoalRun
            {
                GoalId = 1,
                Run = run,
                Status = status,
                StartedAt = started,
                CompletedAt = status == RunStatus.Active ? null : started,
            };
            runs.Add(runObj);
            handler.LoadStateRows(runs, Array.Empty<(int, int, Contribution)>(), Array.Empty<WorldFlag>());
            return runObj;
        }

        private static GoalRequirement Req(long? target = null, long[]? targets = null, double? growth = null) =>
            new() { Key = "xp", Kind = RequirementKind.Experience, Target = target, Targets = targets, Growth = growth };

        private static IReadOnlyList<string> Validate(string json) =>
            CommunityGoalDefinitionParser.Validate(CommunityGoalDefinitionParser.Parse(json), false);

        [Fact]
        public void TargetAt_SingleTarget_StaysTheSame()
        {
            Assert.Equal(new long?[] { 100, 100, 100 }, new[] { 0, 1, 5 }.Select(Req(target: 100).TargetAt));
        }

        [Fact]
        public void TargetAt_TargetWithGrowth_Multiplies()
        {
            Assert.Equal(new long?[] { 10, 20, 40, 80 }, new[] { 0, 1, 2, 3 }.Select(Req(target: 10, growth: 2).TargetAt));
        }

        [Fact]
        public void TargetAt_ListWithoutGrowth_RepeatsLastValue()
        {
            Assert.Equal(new long?[] { 10, 25, 25 }, new[] { 0, 1, 2 }.Select(Req(targets: [10, 25]).TargetAt));
        }

        [Fact]
        public void TargetAt_ListWithGrowth_MultipliesFromLastValue()
        {
            Assert.Equal(new long?[] { 10, 25, 50, 100 }, new[] { 0, 1, 2, 3 }.Select(Req(targets: [10, 25], growth: 2).TargetAt));
        }

        [Fact]
        public void TargetAt_FractionalGrowth_RoundsDown()
        {
            Assert.Equal(15, Req(target: 10, growth: 1.55).TargetAt(1));
        }

        [Fact]
        public void TargetAt_Overflow_ClampsToMax()
        {
            Assert.Equal(long.MaxValue, Req(target: long.MaxValue / 2, growth: 10).TargetAt(5));
        }

        [Fact]
        public void TargetAt_NoTarget_IsNull()
        {
            Assert.Null(Req().TargetAt(3));
        }

        [Fact]
        public void Parse_TargetsGrowthAndMaxCompletions()
        {
            var def = CommunityGoalDefinitionParser.Parse("""
                { "requirements": [ { "key": "xp", "type": "Experience", "targets": [10, 25], "growth": 1.5 } ],
                  "repeat": "onClose", "maxCompletions": 4 }
                """);

            var req = Assert.Single(def.Requirements);
            Assert.Equal(new long[] { 10, 25 }, req.Targets);
            Assert.Equal(1.5, req.Growth);
            Assert.Equal(4, def.MaxCompletions);
            Assert.True(def.HasTargets);
            Assert.Empty(CommunityGoalDefinitionParser.Validate(def, false));
        }

        [Fact]
        public void Parse_TargetAndTargets_Throws()
        {
            var ex = Assert.Throws<CommunityGoalDefinitionException>(() => CommunityGoalDefinitionParser.Parse("""
                { "requirements": [ { "key": "xp", "type": "Experience", "target": 5, "targets": [10] } ] }
                """));
            Assert.Contains("both 'target' and 'targets'", ex.Message);
        }

        [Fact]
        public void Parse_TargetsNotArray_Throws()
        {
            Assert.Throws<CommunityGoalDefinitionException>(() => CommunityGoalDefinitionParser.Parse("""
                { "requirements": [ { "key": "xp", "type": "Experience", "targets": 10 } ] }
                """));
        }

        [Theory]
        [InlineData("""{ "key": "xp", "type": "Experience", "targets": [] }""", "'targets' must not be empty")]
        [InlineData("""{ "key": "xp", "type": "Experience", "target": 10, "growth": 1 }""", "'growth' must be > 1")]
        [InlineData("""{ "key": "xp", "type": "Experience", "target": 10, "growth": 0.5 }""", "'growth' must be > 1")]
        [InlineData("""{ "key": "xp", "type": "Experience", "growth": 2 }""", "has 'growth' but no 'target' or 'targets'")]
        public void Validate_RejectsBadScaling(string requirement, string expected)
        {
            var errors = Validate("""{ "requirements": [ """ + requirement + """ ], "duration": "1d" }""");

            Assert.Contains(errors, e => e.Contains(expected));
        }

        [Fact]
        public void Validate_MaxCompletionsBelowOne_Rejected()
        {
            var errors = Validate("""
                { "requirements": [ { "key": "xp", "type": "Experience", "target": 10 } ],
                  "repeat": "onClose", "maxCompletions": 0 }
                """);

            Assert.Contains("'maxCompletions' must be >= 1", errors);
        }

        [Fact]
        public void Validate_MaxCompletionsWithoutRepeat_Rejected()
        {
            var errors = Validate("""
                { "requirements": [ { "key": "xp", "type": "Experience", "target": 10 } ], "maxCompletions": 3 }
                """);

            Assert.Contains("'maxCompletions' requires 'repeat'", errors);
        }

        [Fact]
        public void Level_CountsEarlierCompletedRunsOnly()
        {
            LoadGoal("""{ "requirements": [ { "key": "gold", "type": "Gold", "target": 10 } ], "repeat": "onClose", "duration": "1d" }""");
            AddRun(1, RunStatus.Completed);
            AddRun(2, RunStatus.Failed);
            AddRun(3, RunStatus.Completed);
            var current = AddRun(4, RunStatus.Active);

            Assert.Equal(2, handler.Level(current));
            Assert.Equal(0, handler.Level(runs[0]));
        }

        [Fact]
        public void Contribute_LaterRun_UsesScaledTarget()
        {
            var goal = LoadGoal("""{ "requirements": [ { "key": "gold", "type": "Gold", "targets": [10, 25], "growth": 2 } ], "repeat": "onClose" }""");
            AddRun(1, RunStatus.Completed);
            AddRun(2, RunStatus.Completed);
            var run = AddRun(3, RunStatus.Active);

            Assert.Equal(50, handler.Remaining(goal, "gold"));
            handler.Contribute(goal, null, "gold", 25, world);
            Assert.Equal(RunStatus.Active, run.Status);
            Assert.Equal(25, handler.Remaining(goal, "gold"));

            var result = handler.Contribute(goal, null, "gold", 40, world);

            Assert.Equal(25, result.Accepted);
            Assert.Equal(RunStatus.Completed, run.Status);
            Assert.Equal(4, handler.CurrentRun(1)!.Run);
            Assert.Equal(100, handler.Remaining(goal, "gold"));
        }

        [Fact]
        public void Completion_AtMaxCompletions_DoesNotStartAnotherRun()
        {
            var goal = LoadGoal("""{ "requirements": [ { "key": "gold", "type": "Gold", "target": 10 } ], "repeat": "onClose", "maxCompletions": 2 }""");
            AddRun(1, RunStatus.Completed);
            var run = AddRun(2, RunStatus.Active);

            handler.Contribute(goal, null, "gold", 10, world);
            now = now.AddHours(1);
            handler.Tick(world);

            Assert.Equal(RunStatus.Completed, run.Status);
            Assert.Same(run, handler.CurrentRun(1));
        }

        [Fact]
        public void Completion_BelowMaxCompletions_StartsNextRun()
        {
            var goal = LoadGoal("""{ "requirements": [ { "key": "gold", "type": "Gold", "target": 10 } ], "repeat": "onClose", "maxCompletions": 3 }""");
            AddRun(1, RunStatus.Completed);
            AddRun(2, RunStatus.Active);

            handler.Contribute(goal, null, "gold", 10, world);

            Assert.Equal(3, handler.CurrentRun(1)!.Run);
            Assert.Equal(RunStatus.Active, handler.CurrentRun(1)!.Status);
        }
    }
}
