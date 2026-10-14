using System.Data.SQLite;
using Goose.CommunityGoals;

namespace Goose.IntegrationTests;

public class CommunityGoalLoadTests : IDisposable
{
    private readonly string dbPath =
        Path.Combine(Path.GetTempPath(), "communitygoal-load-" + Guid.NewGuid().ToString("N") + ".db");

    private readonly GameWorld world;

    public CommunityGoalLoadTests()
    {
        var settings = new GooseSettings
        {
            InventorySize = 30, EquippedSize = 20, CombineBagSize = 10, SpellbookSize = 30,
        };
        world = new GameWorld(settings, new GameServer(settings));
        var db = world.Database;
        db.Start(dbPath);
        db.Execute(conn => RunSql(conn, File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "sql", "community_goals_state.sql"))));
        db.Execute(conn => RunSql(conn,
            "CREATE TABLE community_goals (" +
            "  id INTEGER PRIMARY KEY," +
            "  name TEXT NOT NULL," +
            "  description TEXT NOT NULL DEFAULT ''," +
            "  npc_template_id INT NOT NULL," +
            "  prerequisite_goals TEXT NOT NULL DEFAULT ''," +
            "  script_path TEXT NOT NULL DEFAULT ''," +
            "  properties TEXT NOT NULL DEFAULT ''" +
            ")"));
    }

    [Fact]
    public void Load_ReadsDefinitionsStateAndFlags()
    {
        var db = world.Database;
        db.Execute(conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO community_goals (id, name, description, npc_template_id, prerequisite_goals, script_path, properties) VALUES (@id, @name, @desc, @npc, @prereqs, @script, @props)";
            cmd.Parameters.AddWithValue("@id", 1);
            cmd.Parameters.AddWithValue("@name", "Slay the Goblins");
            cmd.Parameters.AddWithValue("@desc", "");
            cmd.Parameters.AddWithValue("@npc", 200);
            cmd.Parameters.AddWithValue("@prereqs", "");
            cmd.Parameters.AddWithValue("@script", "");
            cmd.Parameters.AddWithValue("@props",
                """{ "requirements": [ { "key": "kills", "type": "Kill", "npc": 7, "target": 100 } ] }""");
            cmd.ExecuteNonQuery();
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@id", 2);
            cmd.Parameters.AddWithValue("@name", "Collect Logs");
            cmd.Parameters.AddWithValue("@desc", "");
            cmd.Parameters.AddWithValue("@npc", 201);
            cmd.Parameters.AddWithValue("@prereqs", "1");
            cmd.Parameters.AddWithValue("@script", "");
            cmd.Parameters.AddWithValue("@props",
                """{ "requirements": [ { "key": "logs", "type": "Item", "item": 120, "target": 5000 } ] }""");
            cmd.ExecuteNonQuery();
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@id", 3);
            cmd.Parameters.AddWithValue("@name", "Broken");
            cmd.Parameters.AddWithValue("@desc", "");
            cmd.Parameters.AddWithValue("@npc", 0);
            cmd.Parameters.AddWithValue("@prereqs", "");
            cmd.Parameters.AddWithValue("@script", "");
            cmd.Parameters.AddWithValue("@props", "not json");
            cmd.ExecuteNonQuery();
        });

        var started = new DateTime(2025, 6, 1, 8, 30, 0, DateTimeKind.Utc);
        var completed = new DateTime(2025, 6, 8, 8, 30, 0, DateTimeKind.Utc);
        db.Execute(conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO community_goal_runs (goal_id, run, status, started_at, completed_at, totals, results) VALUES (@g, @r, @s, @start, @end, @totals, @results)";
            cmd.Parameters.AddWithValue("@g", 1);
            cmd.Parameters.AddWithValue("@r", 1);
            cmd.Parameters.AddWithValue("@s", "Completed");
            cmd.Parameters.AddWithValue("@start", started.ToString("o"));
            cmd.Parameters.AddWithValue("@end", completed.ToString("o"));
            cmd.Parameters.AddWithValue("@totals", """{ "kills": 100 }""");
            cmd.Parameters.AddWithValue("@results", """{ "score": 100 }""");
            cmd.ExecuteNonQuery();

            cmd.Parameters.Clear();
            cmd.CommandText = "INSERT INTO community_goal_contributions (goal_id, run, player_id, amounts, last_at, claimed_bracket, claimed_at) VALUES (@g, @r, @p, @amounts, @last, @bracket, @claimed)";
            cmd.Parameters.AddWithValue("@g", 1);
            cmd.Parameters.AddWithValue("@r", 1);
            cmd.Parameters.AddWithValue("@p", 42);
            cmd.Parameters.AddWithValue("@amounts", """{ "kills": 100 }""");
            cmd.Parameters.AddWithValue("@last", completed.ToString("o"));
            cmd.Parameters.AddWithValue("@bracket", 1);
            cmd.Parameters.AddWithValue("@claimed", completed.ToString("o"));
            cmd.ExecuteNonQuery();

            cmd.Parameters.Clear();
            cmd.CommandText = "INSERT INTO world_flags (name, value, set_at, source_goal_id) VALUES (@name, @value, @set, @goal)";
            cmd.Parameters.AddWithValue("@name", "goal1.round1.completed");
            cmd.Parameters.AddWithValue("@value", 1);
            cmd.Parameters.AddWithValue("@set", completed.ToString("o"));
            cmd.Parameters.AddWithValue("@goal", 1);
            cmd.ExecuteNonQuery();
        });

        world.CommunityGoals.Load(world);

        var goals = world.CommunityGoals.Goals;
        Assert.Equal(new[] { 1, 2 }, goals.Keys.OrderBy(k => k).ToArray());
        Assert.Equal(7, goals[1].Definition!.Requirements[0].NpcTemplateId);
        Assert.Equal(new[] { 1 }, goals[2].PrerequisiteGoals);

        var kills = Assert.Single(world.CommunityGoals.KillIndex[7]);
        Assert.Equal(1, kills.Item1.Id);

        var run = world.CommunityGoals.CurrentRun(1);
        Assert.NotNull(run);
        Assert.Equal(RunStatus.Completed, run!.Status);
        Assert.Equal(started, run.StartedAt);
        Assert.Equal(completed, run.CompletedAt);
        Assert.Equal(100, run.Totals["kills"]);
        Assert.Equal(100, run.Results!["score"]!.GetValue<int>());
        var contribution = Assert.Single(run.Contributions).Value;
        Assert.Equal(42, contribution.PlayerId);
        Assert.Equal(100, contribution.Amounts["kills"]);
        Assert.Equal(1, contribution.ClaimedBracket);
        Assert.Equal(completed, contribution.ClaimedAt);

        Assert.Equal(1, world.CommunityGoals.GetFlag("goal1.round1.completed"));
        Assert.Equal(0, world.CommunityGoals.GetFlag("missing"));

        var npcGoals = world.CommunityGoals.GoalsForNpc(200).Select(g => g.Id).ToArray();
        Assert.Equal(new[] { 1 }, npcGoals);
    }

    [Fact]
    public void Load_MalformedPrerequisites_SkipsGoalSiblingsLoad()
    {
        var db = world.Database;
        db.Execute(conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO community_goals (id, name, description, npc_template_id, prerequisite_goals, script_path, properties) VALUES (@id, @name, @desc, @npc, @prereqs, @script, @props)";
            cmd.Parameters.AddWithValue("@id", 1);
            cmd.Parameters.AddWithValue("@name", "Good One");
            cmd.Parameters.AddWithValue("@desc", "");
            cmd.Parameters.AddWithValue("@npc", 0);
            cmd.Parameters.AddWithValue("@prereqs", "");
            cmd.Parameters.AddWithValue("@script", "");
            cmd.Parameters.AddWithValue("@props",
                """{ "requirements": [ { "key": "kills", "type": "Kill", "npc": 7, "target": 100 } ] }""");
            cmd.ExecuteNonQuery();
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@id", 2);
            cmd.Parameters.AddWithValue("@name", "Bad Prereqs");
            cmd.Parameters.AddWithValue("@desc", "");
            cmd.Parameters.AddWithValue("@npc", 0);
            cmd.Parameters.AddWithValue("@prereqs", "1,two");
            cmd.Parameters.AddWithValue("@script", "");
            cmd.Parameters.AddWithValue("@props",
                """{ "requirements": [ { "key": "logs", "type": "Item", "item": 120, "target": 5000 } ] }""");
            cmd.ExecuteNonQuery();
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@id", 3);
            cmd.Parameters.AddWithValue("@name", "Good Two");
            cmd.Parameters.AddWithValue("@desc", "");
            cmd.Parameters.AddWithValue("@npc", 0);
            cmd.Parameters.AddWithValue("@prereqs", "1");
            cmd.Parameters.AddWithValue("@script", "");
            cmd.Parameters.AddWithValue("@props",
                """{ "requirements": [ { "key": "gold", "type": "Gold", "target": 1000 } ] }""");
            cmd.ExecuteNonQuery();
        });

        world.CommunityGoals.Load(world);

        var goals = world.CommunityGoals.Goals;
        Assert.Equal(new[] { 1, 3 }, goals.Keys.OrderBy(k => k).ToArray());
        Assert.Equal(7, goals[1].Definition!.Requirements[0].NpcTemplateId);
        Assert.Equal(new[] { 1 }, goals[3].PrerequisiteGoals);
    }

    private static void RunSql(SQLiteConnection conn, string sql)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    public void Dispose()
    {
        world.Database.Stop();
        foreach (var suffix in new[] { "", "-wal", "-shm" })
        {
            var path = dbPath + suffix;
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
