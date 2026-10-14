using Goose.CommunityGoals;
using Goose.Testing;
using Xunit;

namespace Goose.Tests
{
    public class CommunityGoalContributeTests : IDisposable
    {
        private readonly TestWorldFixture fixture;
        private readonly GameWorld world;
        private readonly CommunityGoalHandler handler;
        private readonly DateTime started = new(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        private DateTime now;

        public CommunityGoalContributeTests()
        {
            fixture = new TestWorldFixture();
            world = fixture.World;
            handler = world.CommunityGoals;
            now = started;
            handler.UtcNow = () => now;
        }

        public void Dispose() => fixture.Dispose();

        private static string GoldProps(long? target, string? extra = null)
        {
            var req = target.HasValue
                ? """{ "key": "gold", "type": "Gold", "target": """ + target.Value + " }"
                : """{ "key": "gold", "type": "Gold", "label": "gold" }""";
            return "{ \"requirements\": [ " + req + " ]" + (extra is null ? "" : ", " + extra) + " }";
        }

        private CommunityGoal LoadGoal(string properties, string scriptPath = "")
        {
            var goal = new CommunityGoal
            {
                Id = 1,
                Name = "Goal 1",
                ScriptPath = scriptPath,
                PropertiesJson = properties,
            };
            handler.LoadDefinitionRows(new[] { goal }, world);
            return handler.Goals[1];
        }

        private CommunityGoalRun LoadRun(params (string Key, long Value)[] totals)
        {
            var run = new CommunityGoalRun { GoalId = 1, Run = 1, StartedAt = started };
            foreach (var (key, value) in totals)
                run.Totals[key] = value;
            handler.LoadStateRows(new[] { run }, Array.Empty<(int, int, Contribution)>(), Array.Empty<WorldFlag>());
            return run;
        }

        private static Player Player() => new(0);

        [Fact]
        public void Contribute_TargetedRequirement_ClampsToRemainingAndMarksDirty()
        {
            var goal = LoadGoal(GoldProps(5000));
            Assert.Equal(0, handler.Remaining(goal, "gold"));
            var run = LoadRun(("gold", 4990));
            Assert.Equal(10, handler.Remaining(goal, "gold"));

            var result = handler.Contribute(goal, Player(), "gold", 50, world);

            Assert.Equal(10, result.Accepted);
            Assert.Null(result.Refusal);
            Assert.Equal(5000, run.Totals["gold"]);
            Assert.Equal(10, run.Contributions[0].Amounts["gold"]);
            Assert.Equal(now, run.Contributions[0].LastAt);
            Assert.True(handler.IsRunDirty(1, 1));
            Assert.True(handler.IsContributionDirty(1, 1, 0));
            Assert.Equal(RunStatus.Completed, run.Status);
        }

        [Fact]
        public void Contribute_UntargetedRequirement_NeverClamps()
        {
            var goal = LoadGoal(GoldProps(null, """ "duration": "1h" """));
            var run = LoadRun(("gold", 500));
            Assert.Equal(long.MaxValue, handler.Remaining(goal, "gold"));

            var result = handler.Contribute(goal, Player(), "gold", 1000000, world);

            Assert.Equal(1000000, result.Accepted);
            Assert.Null(result.Refusal);
            Assert.Equal(1000500, run.Totals["gold"]);
            Assert.Equal(1000000, run.Contributions[0].Amounts["gold"]);
            Assert.Equal(RunStatus.Active, run.Status);
        }

        [Fact]
        public void Contribute_NoActiveRun_RefusedWithoutMutation()
        {
            var goal = LoadGoal(GoldProps(100));

            var result = handler.Contribute(goal, Player(), "gold", 10, world);

            Assert.Equal(0, result.Accepted);
            Assert.Equal("This goal has ended.", result.Refusal);
            Assert.Null(handler.CurrentRun(1));
            Assert.False(handler.IsRunDirty(1, 1));
            Assert.False(handler.IsContributionDirty(1, 1, 0));
        }

        [Fact]
        public void Contribute_CompletedRun_RefusedWithoutMutation()
        {
            var goal = LoadGoal(GoldProps(100));
            var run = LoadRun(("gold", 100));
            run.Status = RunStatus.Completed;

            var result = handler.Contribute(goal, Player(), "gold", 10, world);

            Assert.Equal(0, result.Accepted);
            Assert.Equal("This goal has ended.", result.Refusal);
            Assert.Equal(100, run.Totals["gold"]);
            Assert.Empty(run.Contributions);
            Assert.False(handler.IsRunDirty(1, 1));
            Assert.False(handler.IsContributionDirty(1, 1, 0));
        }

        [Fact]
        public void Contribute_ScriptRefusal_LeavesTotalsContributionsAndDirtySetsUnchanged()
        {
            fixture.CompileCommunityGoalScript(@"
public class G : BaseCommunityGoalScript
{
    public override CommunityGoalDefinition Define(CommunityGoal goal, GameWorld world)
        => new() { Requirements = new[] { Requirement.Gold(""gold"", 5000) } };
    public override string? CanContribute(CommunityGoal goal, CommunityGoalRun run, Player player, string key, long amount, GameWorld world)
        => ""Nope"";
}
return typeof(G);
");
            var goal = LoadGoal("{}", scriptPath: "Scripts/CommunityGoal/G.csx");
            var run = LoadRun();

            var result = handler.Contribute(goal, Player(), "gold", 100, world);

            Assert.Equal(0, result.Accepted);
            Assert.Equal("Nope", result.Refusal);
            Assert.Empty(run.Totals);
            Assert.Empty(run.Contributions);
            Assert.Equal(RunStatus.Active, run.Status);
            Assert.False(handler.IsRunDirty(1, 1));
            Assert.False(handler.IsContributionDirty(1, 1, 0));
        }

        [Fact]
        public void Contribute_ThrowingCanContribute_FailsClosedWithoutMutation()
        {
            fixture.CompileCommunityGoalScript(@"
public class G : BaseCommunityGoalScript
{
    public override CommunityGoalDefinition Define(CommunityGoal goal, GameWorld world)
        => new() { Requirements = new[] { Requirement.Gold(""gold"", 5000) } };
    public override string? CanContribute(CommunityGoal goal, CommunityGoalRun run, Player player, string key, long amount, GameWorld world)
        => throw new InvalidOperationException(""boom"");
}
return typeof(G);
");
            var goal = LoadGoal("{}", scriptPath: "Scripts/CommunityGoal/G.csx");
            var run = LoadRun();

            var result = handler.Contribute(goal, Player(), "gold", 100, world);

            Assert.Equal(0, result.Accepted);
            Assert.Equal("You cannot contribute to this goal right now.", result.Refusal);
            Assert.Empty(run.Totals);
            Assert.Empty(run.Contributions);
            Assert.Equal(RunStatus.Active, run.Status);
            Assert.False(handler.IsRunDirty(1, 1));
            Assert.False(handler.IsContributionDirty(1, 1, 0));
        }

        [Fact]
        public void Contribute_ReachingTarget_CompletesOnceAndRaisesEventOnce()
        {
            var goal = LoadGoal(GoldProps(100));
            var run = LoadRun();
            var events = new List<(CommunityGoal Goal, CommunityGoalRun Run)>();
            handler.RunCompleted += (g, r, w) => events.Add((g, r));

            var result = handler.Contribute(goal, Player(), "gold", 100, world);

            Assert.Equal(100, result.Accepted);
            Assert.Equal(RunStatus.Completed, run.Status);
            Assert.Equal(now, run.CompletedAt);
            Assert.Same(goal, events[0].Goal);
            Assert.Same(run, events[0].Run);
            Assert.Equal(1, events.Count);

            var second = handler.Contribute(goal, Player(), "gold", 10, world);
            Assert.Equal(0, second.Accepted);
            Assert.Equal("This goal has ended.", second.Refusal);
            Assert.Equal(100, run.Totals["gold"]);
            Assert.Equal(1, events.Count);
        }

        [Fact]
        public void Contribute_ThrowingRunCompletedSubscriber_LaterSubscribersStillRun()
        {
            var goal = LoadGoal(GoldProps(100));
            var run = LoadRun();
            var ran = new List<int>();
            handler.RunCompleted += (g, r, w) => throw new InvalidOperationException("boom");
            handler.RunCompleted += (g, r, w) => ran.Add(1);

            using var log = new CapturingLog();
            var result = handler.Contribute(goal, Player(), "gold", 100, world);

            Assert.Equal(100, result.Accepted);
            Assert.Equal(RunStatus.Completed, run.Status);
            Assert.Equal(new[] { 1 }, ran);
            Assert.Contains(log.Messages, m => m.Contains("RunCompleted subscriber Exception"));
        }

        [Fact]
        public void Contribute_TimeoutWithUnmetTargets_FailsWithoutEvent()
        {
            var goal = LoadGoal(GoldProps(1000, """ "duration": "1h" """));
            var run = LoadRun();
            var events = new List<(CommunityGoal Goal, CommunityGoalRun Run)>();
            handler.RunCompleted += (g, r, w) => events.Add((g, r));
            now = started.AddHours(2);

            var result = handler.Contribute(goal, Player(), "gold", 10, world);

            Assert.Equal(10, result.Accepted);
            Assert.Equal(RunStatus.Failed, run.Status);
            Assert.Equal(now, run.CompletedAt);
            Assert.Empty(events);
            Assert.True(handler.IsRunDirty(1, 1));
        }

        [Fact]
        public void Contribute_ClosesAtTimeoutWithUnmetTargets_FailsWithoutEvent()
        {
            var goal = LoadGoal(GoldProps(1000, """ "closesAt": "2025-06-01T13:00:00Z" """));
            var run = LoadRun();
            var events = new List<(CommunityGoal Goal, CommunityGoalRun Run)>();
            handler.RunCompleted += (g, r, w) => events.Add((g, r));
            now = started.AddHours(2);

            var result = handler.Contribute(goal, Player(), "gold", 10, world);

            Assert.Equal(10, result.Accepted);
            Assert.Equal(RunStatus.Failed, run.Status);
            Assert.Equal(now, run.CompletedAt);
            Assert.Empty(events);
            Assert.True(handler.IsRunDirty(1, 1));
        }

        [Fact]
        public void Contribute_UntargetedTimeout_Completes()
        {
            var goal = LoadGoal(GoldProps(null, """ "duration": "1h" """));
            var run = LoadRun();
            var events = new List<(CommunityGoal Goal, CommunityGoalRun Run)>();
            handler.RunCompleted += (g, r, w) => events.Add((g, r));
            now = started.AddHours(1);

            var result = handler.Contribute(goal, Player(), "gold", 5, world);

            Assert.Equal(5, result.Accepted);
            Assert.Equal(RunStatus.Completed, run.Status);
            Assert.Equal(now, run.CompletedAt);
            Assert.Single(events);
        }

        [Fact]
        public void Contribute_ScriptIsComplete_CompletesBeforeTargetsMet()
        {
            fixture.CompileCommunityGoalScript(@"
public class G : BaseCommunityGoalScript
{
    public override CommunityGoalDefinition Define(CommunityGoal goal, GameWorld world)
        => new() { Requirements = new[] { Requirement.Gold(""gold"", 1000) } };
    public override bool IsComplete(CommunityGoal goal, CommunityGoalRun run, GameWorld world) => true;
}
return typeof(G);
");
            var goal = LoadGoal("{}", scriptPath: "Scripts/CommunityGoal/G.csx");
            var run = LoadRun();
            var events = new List<(CommunityGoal Goal, CommunityGoalRun Run)>();
            handler.RunCompleted += (g, r, w) => events.Add((g, r));

            var result = handler.Contribute(goal, Player(), "gold", 10, world);

            Assert.Equal(10, result.Accepted);
            Assert.Equal(RunStatus.Completed, run.Status);
            Assert.Equal(now, run.CompletedAt);
            Assert.Single(events);
        }

        [Fact]
        public void Contribute_NullPlayer_UpdatesTotalsOnly()
        {
            var goal = LoadGoal(GoldProps(1000));
            var run = LoadRun();

            var result = handler.Contribute(goal, null, "gold", 50, world);

            Assert.Equal(50, result.Accepted);
            Assert.Null(result.Refusal);
            Assert.Equal(50, run.Totals["gold"]);
            Assert.Empty(run.Contributions);
            Assert.Equal(RunStatus.Active, run.Status);
            Assert.True(handler.IsRunDirty(1, 1));
            Assert.False(handler.IsContributionDirty(1, 1, 0));
        }

        [Fact]
        public void Contribute_AmountNotNeeded_RefusedWithoutMutation()
        {
            var goal = LoadGoal(GoldProps(100));
            var run = LoadRun(("gold", 100));

            var result = handler.Contribute(goal, Player(), "gold", 10, world);

            Assert.Equal(0, result.Accepted);
            Assert.Equal("That's no longer needed.", result.Refusal);
            Assert.Equal(100, run.Totals["gold"]);
            Assert.Empty(run.Contributions);
            Assert.Equal(RunStatus.Active, run.Status);
            Assert.False(handler.IsRunDirty(1, 1));
            Assert.False(handler.IsContributionDirty(1, 1, 0));
        }
    }
}
