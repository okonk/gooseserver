using System.Text.Json.Nodes;
using Goose.CommunityGoals;
using Goose.Testing;
using Xunit;

namespace Goose.Tests
{
    public class CommunityGoalBracketTests : IDisposable
    {
        private readonly TestWorldFixture fixture;
        private readonly GameWorld world;
        private readonly CommunityGoalHandler handler;
        private readonly DateTime started = new(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        private DateTime now;

        public CommunityGoalBracketTests()
        {
            fixture = new TestWorldFixture();
            world = fixture.World;
            handler = world.CommunityGoals;
            now = started;
            handler.UtcNow = () => now;
        }

        public void Dispose() => fixture.Dispose();

        private static string MixedProps() => """
            { "requirements": [ { "key": "gold", "type": "Gold", "target": 5000 } ],
              "brackets": [ { "rank": 1 }, { "minScore": 2000 }, { "minScore": 1000 }, { "minScore": 0 } ] }
            """;

        private CommunityGoal LoadGoal(string properties)
        {
            var goal = new CommunityGoal
            {
                Id = 1,
                Name = "Goal 1",
                PropertiesJson = properties,
            };
            handler.LoadDefinitionRows(new[] { goal }, world);
            return handler.Goals[1];
        }

        private static Player Player(int id) => new(0) { PlayerID = id, Name = "P" + id };

        private CommunityGoalRun LoadRun(string key, params (int Id, long Amount)[] contributors)
        {
            var run = new CommunityGoalRun { GoalId = 1, Run = 1, StartedAt = started };
            foreach (var (id, amount) in contributors)
            {
                var contribution = new Contribution { PlayerId = id, LastAt = started };
                contribution.Amounts[key] = amount;
                run.Contributions[id] = contribution;
                run.Totals[key] = run.Totals.GetValueOrDefault(key) + amount;
            }
            handler.LoadStateRows(new[] { run }, Array.Empty<(int, int, Contribution)>(), Array.Empty<WorldFlag>());
            return run;
        }

        [Fact]
        public void GetBracket_MixedBrackets_FirstMatchWinsInDefinitionOrder()
        {
            var goal = LoadGoal(MixedProps());
            var run = LoadRun("gold", (5, 3000), (7, 2500), (6, 1200), (9, 500));
            run.Results = JsonNode.Parse("""{ "places": [5, 7, 6, 9], "top": [], "totals": {} }""")!.AsObject();
            var brackets = goal.Definition!.Brackets;

            Assert.Same(brackets[0], goal.ResolvedScript!.GetBracket(goal, run, Player(5), world));
            Assert.Same(brackets[1], goal.ResolvedScript.GetBracket(goal, run, Player(7), world));
            Assert.Same(brackets[2], goal.ResolvedScript.GetBracket(goal, run, Player(6), world));
            Assert.Same(brackets[3], goal.ResolvedScript.GetBracket(goal, run, Player(9), world));
        }

        [Fact]
        public void GetBracket_MinScoreMeasure_UsesRawAmountNotWeightedScore()
        {
            var goal = LoadGoal("""
                { "requirements": [ { "key": "logs", "type": "Gold", "target": 1000 } ],
                  "score": { "logs": 10 },
                  "brackets": [ { "minScore": 100, "measure": "logs" } ] }
                """);
            var run = LoadRun("logs", (5, 15), (7, 100));
            run.Results = JsonNode.Parse("""{ "places": [5, 7], "top": [], "totals": {} }""")!.AsObject();

            Assert.Null(goal.ResolvedScript!.GetBracket(goal, run, Player(5), world));
            Assert.Same(goal.Definition!.Brackets[0], goal.ResolvedScript.GetBracket(goal, run, Player(7), world));
        }

        [Fact]
        public void GetBracket_NonContributor_ReturnsNull()
        {
            var goal = LoadGoal(MixedProps());
            var run = LoadRun("gold", (5, 3000));
            run.Results = JsonNode.Parse("""{ "places": [5], "top": [], "totals": {} }""")!.AsObject();

            Assert.Null(goal.ResolvedScript!.GetBracket(goal, run, Player(42), world));
        }

        [Fact]
        public void BracketIndex_ReturnsIndexAndNullWhenNoBracket()
        {
            var goal = LoadGoal(MixedProps());
            var run = LoadRun("gold", (5, 3000), (7, 2500), (6, 1200), (9, 500));
            run.Results = JsonNode.Parse("""{ "places": [5, 7, 6, 9], "top": [], "totals": {} }""")!.AsObject();

            Assert.Equal(0, handler.BracketIndex(goal, run, Player(5)));
            Assert.Equal(1, handler.BracketIndex(goal, run, Player(7)));
            Assert.Equal(2, handler.BracketIndex(goal, run, Player(6)));
            Assert.Equal(3, handler.BracketIndex(goal, run, Player(9)));
            Assert.Null(handler.BracketIndex(goal, run, Player(42)));
        }

        private void LoadViaDatabase(string properties)
        {
            world.Database.Start(Path.Combine(fixture.DataDirectory, "test.db"));
            world.Database.Execute(conn =>
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "sql", "community_goals_state.sql"));
                    cmd.ExecuteNonQuery();
                }
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "CREATE TABLE community_goals (id INT PRIMARY KEY, name TEXT NOT NULL, description TEXT NOT NULL, npc_template_id INT NOT NULL, prerequisite_goals TEXT NOT NULL, script_path TEXT NOT NULL, properties TEXT NOT NULL)";
                    cmd.ExecuteNonQuery();
                }
                using var insert = conn.CreateCommand();
                insert.CommandText = "INSERT INTO community_goals VALUES (1, 'Goal 1', '', 0, '', '', @p)";
                insert.Parameters.AddWithValue("@p", properties);
                insert.ExecuteNonQuery();
            });
            handler.Load(world);
        }

        [Fact]
        public void Completion_AppliesCommunityRewards()
        {
            LoadViaDatabase("""
                { "requirements": [ { "key": "gold", "type": "Gold", "target": 100 } ],
                  "communityRewards": [
                    { "type": "ChestPages", "npc": 900, "pages": 1 },
                    { "type": "WorldFlag", "name": "bridge_repaired" } ] }
                """);
            var run = LoadRun("gold", (5, 80));

            var result = handler.Contribute(handler.Goals[1], Player(5), "gold", 20, world);

            Assert.Equal(20, result.Accepted);
            Assert.Equal(RunStatus.Completed, run.Status);
            Assert.Equal(world.Settings.CommunityChestPages + 1, world.ChestHandler.GetPages(world, 900));
            Assert.Equal(1, handler.GetFlag("bridge_repaired"));
        }

        [Fact]
        public void FailedRun_AppliesNoCommunityRewards()
        {
            LoadViaDatabase("""
                { "requirements": [ { "key": "gold", "type": "Gold", "target": 1000 } ],
                  "duration": "1h",
                  "communityRewards": [
                    { "type": "ChestPages", "npc": 900, "pages": 1 },
                    { "type": "WorldFlag", "name": "bridge_repaired" } ] }
                """);
            var run = LoadRun("gold", (5, 40));
            now = started.AddHours(2);

            handler.Tick(world);

            Assert.Equal(RunStatus.Failed, run.Status);
            Assert.Equal(world.Settings.CommunityChestPages, world.ChestHandler.GetPages(world, 900));
            Assert.Equal(0, handler.GetFlag("bridge_repaired"));
        }
    }
}
