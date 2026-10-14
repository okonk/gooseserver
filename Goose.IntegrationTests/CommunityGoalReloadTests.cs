using System.Data.SQLite;
using Goose;
using Goose.Commands;
using Goose.Testing;

namespace Goose.IntegrationTests;

public class CommunityGoalReloadTests : IDisposable
{
    private readonly string dbPath =
        Path.Combine(Path.GetTempPath(), "communitygoal-reload-" + Guid.NewGuid().ToString("N") + ".db");

    private readonly TestWorldFixture fixture = new();
    private readonly TestWorldFixture.CapturingPlayer player = new() { State = Player.States.Ready };

    public CommunityGoalReloadTests()
    {
        var db = fixture.World.Database;
        db.Start(dbPath);
        db.Execute(conn => RunSql(conn, File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "sql", "community_goals_state.sql"))));
        db.Execute(conn => RunSql(conn, SchemaDdl.ForAll(
            "spell_effects", "spells", "item_templates", "quests", "quest_requirements", "quest_rewards",
            "npc_templates", "npc_drops", "npc_vendor_items", "community_goals")));
    }

    private void Sql(string sql) => fixture.World.Database.Execute(conn => RunSql(conn, sql));

    private void InsertGoal(int id, string scriptPath, string properties)
    {
        fixture.World.Database.Execute(conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO community_goals (id, name, description, npc_template_id, prerequisite_goals, script_path, properties) VALUES (@id, @name, '', 0, '', @script, @props)";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@name", "Goal " + id);
            cmd.Parameters.AddWithValue("@script", scriptPath);
            cmd.Parameters.AddWithValue("@props", properties);
            cmd.ExecuteNonQuery();
        });
    }

    private void ReloadSql()
    {
        new ReloadSqlCommand().Execute(new CommandContext(player, fixture.World, new CommandRegistry(), [], ""));
        AwaitCompletion();
    }

    private void ReloadScripts()
    {
        new ReloadScriptsCommand().Execute(new CommandContext(player, fixture.World, new CommandRegistry(), [], ""));
        AwaitCompletion();
    }

    private void AwaitCompletion()
    {
        var deadline = DateTime.UtcNow.AddSeconds(60);
        while (fixture.World.PendingCompletionCount == 0)
        {
            Assert.True(DateTime.UtcNow < deadline, "reload never enqueued its completion");
            Thread.Sleep(20);
        }
    }

    private void WriteGoalScript(long target)
    {
        fixture.CompileCommunityGoalScript($$"""
            public class Goal : BaseCommunityGoalScript
            {
                public override CommunityGoalDefinition Define(CommunityGoal goal, GameWorld world)
                    => new() { Requirements = new[] { Requirement.Kill("kills", 7, {{target}}) } };
            }

            return typeof(Goal);
            """, "Goal.csx");
    }

    [Fact]
    public void ReloadSql_ChangesDefinitionAndKeepsRunProgress()
    {
        InsertGoal(1, "", """{ "requirements": [ { "key": "kills", "type": "Kill", "npc": 7, "target": 100 } ] }""");
        Sql("""
            INSERT INTO community_goal_runs (goal_id, run, status, started_at, totals)
            VALUES (1, 1, 'Active', '2025-06-01T12:00:00Z', '{ "kills": 40 }')
            """);
        fixture.World.CommunityGoals.Load(fixture.World);

        Assert.Equal(100, fixture.World.CommunityGoals.Goals[1].Definition!.Requirements[0].Target);

        Sql("""
            UPDATE community_goals
            SET properties = '{ "requirements": [ { "key": "kills", "type": "Kill", "npc": 7, "target": 250 } ] }'
            WHERE id = 1
            """);

        ReloadSql();
        fixture.World.Update();

        Assert.Equal(250, fixture.World.CommunityGoals.Goals[1].Definition!.Requirements[0].Target);
        var run = fixture.World.CommunityGoals.CurrentRun(1);
        Assert.NotNull(run);
        Assert.Equal(40, run!.Totals["kills"]);
        Assert.Contains(player.Sent, s => s.Contains("Reloaded sql data."));
    }

    [Fact]
    public void ReloadScripts_ChangesDefineOutput()
    {
        InsertGoal(1, "Scripts/CommunityGoal/Goal.csx", "");
        WriteGoalScript(100);
        fixture.World.CommunityGoals.Load(fixture.World);

        Assert.Equal(100, fixture.World.CommunityGoals.Goals[1].Definition!.Requirements[0].Target);

        WriteGoalScript(250);

        ReloadScripts();
        fixture.World.Update();

        Assert.Equal(250, fixture.World.CommunityGoals.Goals[1].Definition!.Requirements[0].Target);
        Assert.Contains(player.Sent, s => s.Contains("Reloaded scripts."));
    }

    [Fact]
    public void ReloadScripts_BrokenDefineKeepsPreviousDefinition()
    {
        InsertGoal(1, "Scripts/CommunityGoal/Goal.csx", "");
        WriteGoalScript(100);
        fixture.World.CommunityGoals.Load(fixture.World);

        Assert.Equal(100, fixture.World.CommunityGoals.Goals[1].Definition!.Requirements[0].Target);

        File.WriteAllText(Path.Combine(fixture.DataDirectory, "Scripts", "CommunityGoal", "Goal.csx"), """
            public class Goal : BaseCommunityGoalScript
            {
                public override CommunityGoalDefinition Define(CommunityGoal goal, GameWorld world)
                    => throw new Exception("boom");
            }

            return typeof(Goal);
            """);

        ReloadScripts();
        fixture.World.Update();

        Assert.Equal(100, fixture.World.CommunityGoals.Goals[1].Definition!.Requirements[0].Target);
        Assert.Contains(player.Sent, s => s.Contains("Reloaded scripts."));
    }

    [Fact]
    public void ReloadScripts_BrokenDefine_KeepsOldDefinitionAndOldScript()
    {
        InsertGoal(1, "Scripts/CommunityGoal/Goal.csx", "");
        fixture.CompileCommunityGoalScript("""
            public class Goal : BaseCommunityGoalScript
            {
                public override CommunityGoalDefinition Define(CommunityGoal goal, GameWorld world)
                    => new() { Requirements = new[] { Requirement.Kill("kills", 7, 100) } };
                public override string? CanContribute(CommunityGoal goal, CommunityGoalRun run, Player player, string key, long amount, GameWorld world)
                    => "S1 refusal";
            }

            return typeof(Goal);
            """, "Goal.csx");
        fixture.World.CommunityGoals.Load(fixture.World);

        Assert.Equal(100, fixture.World.CommunityGoals.Goals[1].Definition!.Requirements[0].Target);

        File.WriteAllText(Path.Combine(fixture.DataDirectory, "Scripts", "CommunityGoal", "Goal.csx"), """
            public class Goal : BaseCommunityGoalScript
            {
                public override CommunityGoalDefinition Define(CommunityGoal goal, GameWorld world)
                    => throw new Exception("boom");
                public override string? CanContribute(CommunityGoal goal, CommunityGoalRun run, Player player, string key, long amount, GameWorld world)
                    => "S2 refusal";
            }

            return typeof(Goal);
            """);

        ReloadScripts();
        fixture.World.Update();

        Assert.Equal(100, fixture.World.CommunityGoals.Goals[1].Definition!.Requirements[0].Target);

        fixture.World.CommunityGoals.Tick(fixture.World);
        var goal = fixture.World.CommunityGoals.Goals[1];
        var result = fixture.World.CommunityGoals.Contribute(goal, player, "kills", 1, fixture.World);
        Assert.Equal("S1 refusal", result.Refusal);
    }

    private static void RunSql(SQLiteConnection conn, string sql)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    public void Dispose()
    {
        fixture.World.Database.Stop();
        foreach (var suffix in new[] { "", "-wal", "-shm" })
        {
            var path = dbPath + suffix;
            if (File.Exists(path)) File.Delete(path);
        }
        fixture.Dispose();
    }
}
