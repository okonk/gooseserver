using System.Text.Json.Nodes;
using Goose.CommunityGoals;
using Goose.Testing;
using Xunit;

namespace Goose.Tests
{
    public class CommunityGoalHandlerLoadTests : IDisposable
    {
        private readonly TestWorldFixture fixture;
        private readonly GameWorld world;
        private readonly CommunityGoalHandler handler;

        public CommunityGoalHandlerLoadTests()
        {
            fixture = new TestWorldFixture();
            world = fixture.World;
            handler = world.CommunityGoals;
        }

        public void Dispose() => fixture.Dispose();

        private static CommunityGoal Goal(int id, string properties, int npc = 0, string scriptPath = "")
            => new()
            {
                Id = id,
                Name = "Goal " + id,
                NpcTemplateId = npc,
                ScriptPath = scriptPath,
                PropertiesJson = properties,
            };

        private static string KillProps(int npc, long target = 100)
            => "{ \"requirements\": [ { \"key\": \"kills\", \"type\": \"Kill\", \"npc\": " + npc + ", \"target\": " + target + " } ] }";

        [Fact]
        public void LoadDefinitionRows_ValidRows_LoadGoalsAndIndexKills()
        {
            handler.LoadDefinitionRows(new[]
            {
                Goal(1, KillProps(7), npc: 200),
                Goal(2, """{ "requirements": [ { "key": "gold", "type": "Gold", "target": 1000 } ] }""", npc: 201),
            }, world);

            Assert.Equal(new[] { 1, 2 }, handler.Goals.Keys.OrderBy(k => k).ToArray());
            var goal1 = handler.Goals[1];
            Assert.NotNull(goal1.Definition);
            Assert.Equal(RequirementKind.Kill, goal1.Definition!.Requirements[0].Kind);
            Assert.Null(goal1.Script);

            var kills = Assert.Single(handler.KillIndex[7]);
            Assert.Same(goal1, kills.Item1);
            Assert.Equal("kills", kills.Item2.Key);
            Assert.False(handler.KillIndex.ContainsKey(8));
        }

        [Fact]
        public void LoadDefinitionRows_ScriptPath_UsesCustomScript()
        {
            var script = fixture.CompileCommunityGoalScript(@"
public class G : BaseCommunityGoalScript
{
    public override CommunityGoalDefinition Define(CommunityGoal goal, GameWorld world)
        => new() { Requirements = new[] { Requirement.Custom(""votes"", ""Votes"", 1000) } };
}
return typeof(G);
");

            handler.LoadDefinitionRows(new[] { Goal(1, "{}", scriptPath: "Scripts/CommunityGoal/G.csx") }, world);

            var goal = handler.Goals[1];
            Assert.NotNull(goal.Script);
            Assert.Same(script.Object, goal.Script!.Object);
            var req = Assert.Single(goal.Definition!.Requirements);
            Assert.Equal("votes", req.Key);
        }

        [Fact]
        public void LoadDefinitionRows_InvalidRow_SkippedSiblingsLoad()
        {
            handler.LoadDefinitionRows(new[]
            {
                Goal(1, "not json"),
                Goal(2, KillProps(7)),
            }, world);

            Assert.Equal(2, Assert.Single(handler.Goals).Key);
            Assert.True(handler.KillIndex.ContainsKey(7));
        }

        [Fact]
        public void LoadDefinitionRows_ValidateFailure_Skipped()
        {
            handler.LoadDefinitionRows(new[]
            {
                Goal(1, """{ "requirements": [ { "key": "kills", "type": "Kill", "target": 10 } ] }"""),
                Goal(2, KillProps(7)),
            }, world);

            Assert.Equal(2, Assert.Single(handler.Goals).Key);
        }

        [Fact]
        public void LoadDefinitionRows_MalformedPrerequisites_SkipsGoalSiblingsLoad()
        {
            using var log = new CapturingLog();
            handler.LoadDefinitionRows(new[]
            {
                new CommunityGoal { Id = 1, Name = "Goal 1", PropertiesJson = KillProps(7), PrerequisiteGoalsRaw = "1,two" },
                Goal(2, KillProps(8)),
            }, world);

            Assert.Equal(2, Assert.Single(handler.Goals).Key);
            Assert.True(handler.KillIndex.ContainsKey(8));
            Assert.False(handler.KillIndex.ContainsKey(7));
            Assert.Contains(log.Messages, m => m.Contains("Failed to load community goal 1"));
        }

        [Fact]
        public void LoadDefinitionRows_RewardScriptMissing_SkipsGoalSiblingsLoad()
        {
            using var log = new CapturingLog();
            handler.LoadDefinitionRows(new[]
            {
                Goal(1, """
                    { "requirements": [ { "key": "gold", "type": "Gold", "target": 100 } ],
                      "brackets": [ { "rank": 1, "rewards": [ { "type": "Script", "script": "Scripts/CommunityGoal/Missing.csx" } ] } ] }
                    """),
                Goal(2, KillProps(8)),
            }, world);

            Assert.Equal(2, Assert.Single(handler.Goals).Key);
            Assert.Contains(log.Messages, m => m.Contains("Failed to load community goal 1"));
        }

        [Fact]
        public void LoadDefinitionRows_RewardScriptCompiles_Loads()
        {
            Directory.CreateDirectory(Path.Combine(fixture.DataDirectory, "Scripts", "CommunityGoal"));
            File.WriteAllText(Path.Combine(fixture.DataDirectory, "Scripts", "CommunityGoal", "R.csx"),
                "public class R : BaseCommunityRewardScript { }\nreturn typeof(R);\n");

            handler.LoadDefinitionRows(new[]
            {
                Goal(1, """
                    { "requirements": [ { "key": "gold", "type": "Gold", "target": 100 } ],
                      "brackets": [ { "rank": 1, "rewards": [ { "type": "Script", "script": "Scripts/CommunityGoal/R.csx" } ] } ] }
                    """),
            }, world);

            Assert.Equal(1, Assert.Single(handler.Goals).Key);
        }

        [Fact]
        public void LoadDefinitions_DatabaseFailure_LeavesPreviousStateUntouched()
        {
            handler.LoadDefinitionRows(new[] { Goal(1, KillProps(7)) }, world);
            var previous = handler.Goals[1];

            Assert.Throws<InvalidOperationException>(() => handler.LoadDefinitions(world));

            Assert.Same(previous, handler.Goals[1]);
            Assert.Same(previous, handler.KillIndex[7][0].Item1);
        }

        [Fact]
        public void LoadStateRows_RunsContributionsAndFlags_Load()
        {
            handler.LoadDefinitionRows(new[] { Goal(1, KillProps(7)) }, world);
            var started = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);

            handler.LoadStateRows(
                new[] { MakeRun(1, 1, started) },
                new[] { (1, 1, MakeContribution(5, started, 42)) },
                new[] { new WorldFlag
                {
                    Name = "goal1.done", Value = 3, SetAt = started, SourceGoalId = 1,
                } });

            var run = handler.CurrentRun(1);
            Assert.NotNull(run);
            Assert.Equal(42, run!.Totals["kills"]);
            Assert.Equal(1, run.Results!["score"]!.GetValue<int>());
            Assert.Equal(42, run.Contributions[5].Amounts["kills"]);
            Assert.Equal(3, handler.GetFlag("goal1.done"));
            Assert.Equal(0, handler.GetFlag("missing"));
        }

        [Fact]
        public void LoadStateRows_OrphanGoalRows_KeptInMemory()
        {
            handler.LoadDefinitionRows(new[] { Goal(1, KillProps(7)) }, world);
            var started = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);

            using var log = new CapturingLog();
            handler.LoadStateRows(
                new[]
                {
                    new CommunityGoalRun { GoalId = 1, Run = 1, StartedAt = started },
                    new CommunityGoalRun { GoalId = 99, Run = 3, StartedAt = started },
                },
                new[] { (99, 3, MakeContribution(5, started, 1)) },
                Array.Empty<WorldFlag>());

            Assert.NotNull(handler.CurrentRun(99));
            Assert.Equal(3, handler.CurrentRun(99)!.Run);
            Assert.Single(handler.CurrentRun(99)!.Contributions);
            Assert.NotNull(handler.CurrentRun(1));

            Assert.Equal(1, log.Messages.Count(m => m.Contains("unknown goal 99")));
        }

        private static CommunityGoalRun MakeRun(int goalId, int run, DateTime started)
        {
            var runObj = new CommunityGoalRun
            {
                GoalId = goalId, Run = run, Status = RunStatus.Active, StartedAt = started,
                Results = JsonNode.Parse("""{ "score": 1 }""")!.AsObject(),
            };
            runObj.Totals["kills"] = 42;
            return runObj;
        }

        private static Contribution MakeContribution(int playerId, DateTime lastAt, long kills)
        {
            var contribution = new Contribution { PlayerId = playerId, LastAt = lastAt };
            contribution.Amounts["kills"] = kills;
            return contribution;
        }

        [Fact]
        public void LoadStateRows_MultipleRuns_CurrentRunIsHighest()
        {
            var started = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            handler.LoadStateRows(
                new[]
                {
                    new CommunityGoalRun { GoalId = 1, Run = 2, StartedAt = started },
                    new CommunityGoalRun { GoalId = 1, Run = 1, StartedAt = started },
                    new CommunityGoalRun { GoalId = 1, Run = 5, StartedAt = started },
                },
                Array.Empty<(int, int, Contribution)>(),
                Array.Empty<WorldFlag>());

            Assert.Equal(5, handler.CurrentRun(1)!.Run);
            Assert.Null(handler.CurrentRun(2));
        }

        [Fact]
        public void GoalsForNpc_ReturnsGoalsForThatNpc()
        {
            handler.LoadDefinitionRows(new[]
            {
                Goal(1, KillProps(7), npc: 200),
                Goal(2, KillProps(7), npc: 200),
                Goal(3, KillProps(7), npc: 201),
            }, world);

            var ids = handler.GoalsForNpc(200).Select(g => g.Id).OrderBy(i => i).ToArray();
            Assert.Equal(new[] { 1, 2 }, ids);
        }

        [Fact]
        public void Redefine_AfterEditingPropertiesJson_UpdatesDefinitionAndKillIndex()
        {
            handler.LoadDefinitionRows(new[] { Goal(1, KillProps(7)) }, world);
            Assert.True(handler.KillIndex.ContainsKey(7));

            handler.Goals[1].PropertiesJson = KillProps(8);
            handler.Redefine(world);

            Assert.True(handler.KillIndex.ContainsKey(8));
            Assert.False(handler.KillIndex.ContainsKey(7));
            Assert.Equal(8, handler.Goals[1].Definition!.Requirements[0].NpcTemplateId);
        }

        [Fact]
        public void Redefine_ThrowingDefine_KeepsOldDefinition()
        {
            handler.LoadDefinitionRows(new[] { Goal(1, KillProps(7)) }, world);
            var previous = handler.Goals[1].Definition;

            handler.Goals[1].PropertiesJson = "not json";
            handler.Redefine(world);

            Assert.Same(previous, handler.Goals[1].Definition);
            Assert.Same(handler.Goals[1], handler.KillIndex[7][0].Item1);
        }
    }
}
