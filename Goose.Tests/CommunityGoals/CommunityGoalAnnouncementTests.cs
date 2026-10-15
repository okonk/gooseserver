using System.Text.Json.Nodes;
using Goose.CommunityGoals;
using Goose.Testing;
using Xunit;

namespace Goose.Tests
{
    public class CommunityGoalAnnouncementTests : IDisposable
    {
        private readonly TestWorldFixture fixture;
        private readonly GameWorld world;
        private readonly CommunityGoalHandler handler;
        private readonly Map map;
        private readonly DateTime started = new(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        private DateTime now;

        public CommunityGoalAnnouncementTests()
        {
            fixture = new TestWorldFixture();
            world = fixture.World;
            handler = world.CommunityGoals;
            map = fixture.AddBaseMap(1, "Map 1");
            now = started;
            handler.UtcNow = () => now;
        }

        public void Dispose() => fixture.Dispose();

        private void LoadWorld(string properties, string name = "Goal 1")
        {
            world.Database.Start(Path.Combine(fixture.DataDirectory, "test.db"));
            world.Database.Execute(conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = """
                    CREATE TABLE community_goals (id INTEGER PRIMARY KEY, name TEXT, description TEXT, npc_template_id INTEGER, prerequisite_goals TEXT, script_path TEXT, properties TEXT);
                    CREATE TABLE community_goal_runs (goal_id INTEGER, run INTEGER, status TEXT, started_at TEXT, completed_at TEXT, totals TEXT, results TEXT);
                    CREATE TABLE community_goal_contributions (goal_id INTEGER, run INTEGER, player_id INTEGER, amounts TEXT, last_at TEXT, claimed_bracket INTEGER, claimed_at TEXT);
                    CREATE TABLE world_flags (name TEXT PRIMARY KEY, value INTEGER, set_at TEXT, source_goal_id INTEGER);
                    """;
                cmd.ExecuteNonQuery();
                using var ins = conn.CreateCommand();
                ins.CommandText = "INSERT INTO community_goals (id, name, description, npc_template_id, prerequisite_goals, script_path, properties) VALUES (1, @name, '', 0, '', '', @props)";
                ins.Parameters.AddWithValue("@name", name);
                ins.Parameters.AddWithValue("@props", properties);
                ins.ExecuteNonQuery();
            });
            handler.Load(world);
        }

        private TestWorldFixture.CapturingPlayer AddOnlinePlayer(string name)
        {
            var player = fixture.CommandPlayerOn(map, 1, 1, name);
            fixture.AddOnlinePlayer(player);
            return player;
        }

        private static int AnnouncementCount(TestWorldFixture.CapturingPlayer player, string expected)
        {
            // GameWorld.Send appends the \x1 packet terminator before Player.Send.
            return player.Sent.Count(s => s == expected + "\x1");
        }

        [Fact]
        public void Completion_BroadcastsMessageOnceToAllOnlinePlayers()
        {
            LoadWorld("""{ "requirements": [ { "key": "gold", "type": "Gold", "target": 1000 } ] }""");
            var alice = AddOnlinePlayer("Alice");
            var bob = AddOnlinePlayer("Bob");
            var expected = P.ServerMessage("[Community] Goal 1 has been completed!");

            handler.Tick(world);
            handler.Contribute(handler.Goals[1], new Player(0), "gold", 1000, world);

            Assert.Equal(RunStatus.Completed, handler.CurrentRun(1)!.Status);
            Assert.Equal(1, AnnouncementCount(alice, expected));
            Assert.Equal(1, AnnouncementCount(bob, expected));
        }

        [Fact]
        public void LotteryCompletion_AnnouncesFirstPlaceWinner()
        {
            LoadWorld("""{ "requirements": [ { "key": "gold", "type": "Gold", "label": "gold" } ], "ranking": "lottery", "entryCost": { "gold": 10 }, "brackets": [ { "rank": 1 } ], "duration": "1h" }""");
            var alice = AddOnlinePlayer("Alice");
            fixture.RegisterDatabasePlayer(new Player { PlayerID = 5, Name = "Winner" });
            var expected = P.ServerMessage("[Community] Goal 1 has been completed! Winner: Winner");

            handler.Tick(world);
            handler.Contribute(handler.Goals[1], new Player { PlayerID = 5 }, "gold", 10, world);
            now = started.AddHours(2);
            handler.Tick(world);

            Assert.Equal(RunStatus.Completed, handler.CurrentRun(1)!.Status);
            Assert.Equal(1, AnnouncementCount(alice, expected));
        }

        [Fact]
        public void LotteryCompletion_AnnouncesUnknownWhenWinnerNameUnresolvable()
        {
            LoadWorld("""{ "requirements": [ { "key": "gold", "type": "Gold", "label": "gold" } ], "ranking": "lottery", "entryCost": { "gold": 10 }, "brackets": [ { "rank": 1 } ], "duration": "1h" }""");
            var alice = AddOnlinePlayer("Alice");
            var expected = P.ServerMessage("[Community] Goal 1 has been completed! Winner: Unknown");

            handler.Tick(world);
            handler.Contribute(handler.Goals[1], new Player { PlayerID = 5 }, "gold", 10, world);
            now = started.AddHours(2);
            handler.Tick(world);

            Assert.Equal(RunStatus.Completed, handler.CurrentRun(1)!.Status);
            Assert.Equal(1, AnnouncementCount(alice, expected));
        }

        [Fact]
        public void LotteryCompletion_MalformedFirstPlace_AnnouncesUnknownWithoutThrowing()
        {
            LoadWorld("""{ "requirements": [ { "key": "gold", "type": "Gold", "label": "gold" } ], "ranking": "lottery", "entryCost": { "gold": 10 }, "brackets": [ { "rank": 1 } ], "duration": "1h" }""");
            var alice = AddOnlinePlayer("Alice");

            handler.Tick(world);
            handler.Contribute(handler.Goals[1], new Player { PlayerID = 5 }, "gold", 10, world);
            now = started.AddHours(2);
            handler.Tick(world);

            var run = handler.CurrentRun(1)!;
            Assert.Equal(RunStatus.Completed, run.Status);
            ((JsonArray)run.Results!["places"]!)[0] = JsonValue.Create("abc");
            alice.Sent.Clear();

            handler.AnnounceCompletion(handler.Goals[1], run, world);

            Assert.Equal(1, AnnouncementCount(alice, P.ServerMessage("[Community] Goal 1 has been completed! Winner: Unknown")));
        }

        [Fact]
        public void EmptyLotteryWithRankBrackets_AnnouncesNoWinners()
        {
            LoadWorld("""{ "requirements": [ { "key": "gold", "type": "Gold", "label": "gold" } ], "ranking": "lottery", "entryCost": { "gold": 10 }, "brackets": [ { "rank": 1 } ], "duration": "1h" }""");
            var alice = AddOnlinePlayer("Alice");
            var expected = P.ServerMessage("[Community] Goal 1 has been completed! No winners.");

            handler.Tick(world);
            now = started.AddHours(2);
            handler.Tick(world);

            Assert.Equal(RunStatus.Completed, handler.CurrentRun(1)!.Status);
            Assert.Equal(1, AnnouncementCount(alice, expected));
        }

        [Fact]
        public void FailedRun_SendsNoAnnouncement()
        {
            LoadWorld("""{ "requirements": [ { "key": "gold", "type": "Gold", "target": 1000 } ], "duration": "1h" }""");
            var alice = AddOnlinePlayer("Alice");

            handler.Tick(world);
            now = started.AddHours(2);
            handler.Tick(world);

            Assert.Equal(RunStatus.Failed, handler.CurrentRun(1)!.Status);
            Assert.Empty(alice.Sent);
        }
    }
}
