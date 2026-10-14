using System.Data.SQLite;
using Goose.CommunityGoals;

namespace Goose.IntegrationTests;

public class CommunityGoalPersistenceTests : PlayerFirstSaveTestBase
{
    private const string runsDdl =
        "CREATE TABLE community_goal_runs (" +
        "  goal_id INT NOT NULL," +
        "  run INT NOT NULL," +
        "  status TEXT NOT NULL," +
        "  started_at TEXT NOT NULL," +
        "  completed_at TEXT NULL," +
        "  totals TEXT NOT NULL DEFAULT '{}'," +
        "  results TEXT NULL," +
        "  PRIMARY KEY (goal_id, run)" +
        ")";

    private static readonly DateTime started = new(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc);
    private DateTime now = started;

    public CommunityGoalPersistenceTests() :
        base(["players", "community_goals_state"], ["community_goals"])
    {
        world.CommunityGoals.UtcNow = () => now;
    }

    [Fact]
    public void Contribute_Save_RoundTripsThroughNewWorld()
    {
        InsertGoal(1, """{ "requirements": [ { "key": "gold", "type": "Gold", "target": 200 } ] }""");
        InsertRun(1, 1, "Active", started);
        world.CommunityGoals.Load(world);

        var goal = world.CommunityGoals.Goals[1];
        var player = MakePlayer();

        Assert.Equal(100, world.CommunityGoals.Contribute(goal, player, "gold", 100, world).Accepted);
        world.CommunityGoals.SaveSync(world);
        Assert.Equal(1, Count("SELECT COUNT(*) FROM community_goal_runs WHERE goal_id=1 AND run=1"));
        Assert.Equal(1, Count("SELECT COUNT(*) FROM community_goal_contributions WHERE goal_id=1 AND run=1 AND player_id=1"));

        Assert.Equal(100, world.CommunityGoals.Contribute(goal, player, "gold", 100, world).Accepted);
        world.CommunityGoals.SaveSync(world);
        Assert.Equal(1, Count("SELECT COUNT(*) FROM community_goal_runs WHERE goal_id=1 AND run=1"));
        Assert.Equal(1, Count("SELECT COUNT(*) FROM community_goal_contributions WHERE goal_id=1 AND run=1 AND player_id=1"));

        var world2 = NewWorld();
        try
        {
            world2.CommunityGoals.Load(world2);

            var run = world2.CommunityGoals.CurrentRun(1);
            Assert.NotNull(run);
            Assert.Equal(RunStatus.Completed, run!.Status);
            Assert.Equal(started, run.StartedAt);
            Assert.Equal(started, run.CompletedAt);
            Assert.Equal(200, run.Totals["gold"]);
            Assert.Null(run.Results);
            var contribution = Assert.Single(run.Contributions).Value;
            Assert.Equal(1, contribution.PlayerId);
            Assert.Equal(200, contribution.Amounts["gold"]);
            Assert.Equal(started, contribution.LastAt);
            Assert.Null(contribution.ClaimedBracket);
            Assert.Null(contribution.ClaimedAt);
        }
        finally
        {
            world2.Database.Stop();
        }
    }

    [Fact]
    public void Flags_SetAndAdd_RoundTripThroughNewWorld()
    {
        world.CommunityGoals.Load(world);

        world.CommunityGoals.SetFlag("chest_pages:77", 2, 5);
        world.CommunityGoals.AddToFlag("chest_pages:77", 1, null);
        world.CommunityGoals.AddToFlag("goal1.round1.completed", 1, 1);

        world.CommunityGoals.SaveSync(world);

        Assert.Equal(2, Count("SELECT COUNT(*) FROM world_flags"));
        Assert.Equal(3, Count("SELECT value FROM world_flags WHERE name='chest_pages:77'"));
        Assert.Equal(1, Count("SELECT value FROM world_flags WHERE name='goal1.round1.completed'"));
        Assert.Equal(1, Count("SELECT source_goal_id FROM world_flags WHERE name='goal1.round1.completed'"));

        var world2 = NewWorld();
        try
        {
            world2.CommunityGoals.Load(world2);
            Assert.Equal(3, world2.CommunityGoals.GetFlag("chest_pages:77"));
            Assert.Equal(1, world2.CommunityGoals.GetFlag("goal1.round1.completed"));
            Assert.Equal(0, world2.CommunityGoals.GetFlag("missing"));
        }
        finally
        {
            world2.Database.Stop();
        }
    }

    [Fact]
    public void UnchangedState_ProducesEmptyPlanAndNoSql()
    {
        InsertGoal(1, """{ "requirements": [ { "key": "gold", "type": "Gold", "target": 100 } ] }""");
        world.CommunityGoals.Load(world);

        var plan = world.CommunityGoals.PlanSave();
        Assert.True(plan.IsEmpty);

        world.CommunityGoals.Save(world);
        Assert.Equal(0, world.Database.PendingCount);
        Assert.Equal(0, Count("SELECT COUNT(*) FROM community_goal_runs"));
        Assert.Equal(0, Count("SELECT COUNT(*) FROM community_goal_contributions"));
        Assert.Equal(0, Count("SELECT COUNT(*) FROM world_flags"));
    }

    [Fact]
    public void Expiry_DeletesContributionRows()
    {
        InsertGoal(1,
            """{ "requirements": [ { "key": "gold", "type": "Gold", "target": 100 } ], "repeat": "onClose", "claimWindow": "1h" }""");
        InsertRun(1, 1, "Completed", started, started, """{ "gold": 100 }""");
        InsertContribution(1, 1, 1, """{ "gold": 100 }""", started);
        world.CommunityGoals.Load(world);

        now = started.AddHours(2);
        world.CommunityGoals.Tick(world);

        world.CommunityGoals.SaveSync(world);

        Assert.Equal(0, Count("SELECT COUNT(*) FROM community_goal_contributions WHERE goal_id=1 AND run=1"));
        Assert.Equal(1, Count("SELECT COUNT(*) FROM community_goal_runs WHERE goal_id=1 AND run=1 AND status='Completed'"));
    }

    [Fact]
    public void FailedSaveTransaction_ReDirtiesAndLaterSaveWrites()
    {
        InsertGoal(1, """{ "requirements": [ { "key": "gold", "type": "Gold", "target": 100 } ] }""");
        InsertRun(1, 1, "Active", started);
        world.CommunityGoals.Load(world);

        var result = world.CommunityGoals.Contribute(
            world.CommunityGoals.Goals[1], MakePlayer(), "gold", 40, world);
        Assert.Equal(40, result.Accepted);
        Assert.True(world.CommunityGoals.IsRunDirty(1, 1));
        Assert.True(world.CommunityGoals.IsContributionDirty(1, 1, 1));

        world.Database.Execute(conn => RunSql(conn, "DROP TABLE community_goal_runs"));

        world.CommunityGoals.Save(world);
        WaitDrained();
        WaitReDirtyEnqueued();
        world.Update();

        Assert.True(world.CommunityGoals.IsRunDirty(1, 1));
        Assert.True(world.CommunityGoals.IsContributionDirty(1, 1, 1));
        Assert.Equal(0, Count("SELECT COUNT(*) FROM community_goal_contributions WHERE goal_id=1 AND run=1 AND player_id=1"));

        world.Database.Execute(conn => RunSql(conn, runsDdl));

        world.CommunityGoals.SaveSync(world);

        Assert.Equal(1, Count("SELECT COUNT(*) FROM community_goal_runs WHERE goal_id=1 AND run=1"));
        Assert.Equal(1, Count("SELECT COUNT(*) FROM community_goal_contributions WHERE goal_id=1 AND run=1 AND player_id=1"));
    }

    [Fact]
    public void Stop_FlushesUnsavedContributions()
    {
        InsertGoal(1, """{ "requirements": [ { "key": "gold", "type": "Gold", "target": 100 } ] }""");
        InsertRun(1, 1, "Active", started);
        world.CommunityGoals.Load(world);

        var result = world.CommunityGoals.Contribute(
            world.CommunityGoals.Goals[1], MakePlayer(), "gold", 40, world);
        Assert.Equal(40, result.Accepted);

        world.Stop();

        var world2 = NewWorld();
        try
        {
            world2.CommunityGoals.Load(world2);
            var run = world2.CommunityGoals.CurrentRun(1);
            Assert.NotNull(run);
            Assert.Equal(RunStatus.Active, run!.Status);
            Assert.Equal(40, run.Totals["gold"]);
            var contribution = Assert.Single(run.Contributions).Value;
            Assert.Equal(40, contribution.Amounts["gold"]);
            Assert.Equal(started, contribution.LastAt);
        }
        finally
        {
            world2.Database.Stop();
        }
    }

    [Fact]
    public void FailedInFlightSave_ThenStop_StillPersists()
    {
        InsertGoal(1, """{ "requirements": [ { "key": "gold", "type": "Gold", "target": 100 } ] }""");
        InsertRun(1, 1, "Active", started);
        world.CommunityGoals.Load(world);

        var result = world.CommunityGoals.Contribute(
            world.CommunityGoals.Goals[1], MakePlayer(), "gold", 40, world);
        Assert.Equal(40, result.Accepted);

        world.Database.Execute(conn => RunSql(conn, "DROP TABLE community_goal_runs"));

        world.CommunityGoals.Save(world);
        WaitDrained();
        WaitReDirtyEnqueued();

        world.Database.Execute(conn => RunSql(conn, runsDdl));

        world.Stop();

        var world2 = NewWorld();
        try
        {
            world2.CommunityGoals.Load(world2);
            var run = world2.CommunityGoals.CurrentRun(1);
            Assert.NotNull(run);
            Assert.Equal(RunStatus.Active, run!.Status);
            Assert.Equal(40, run.Totals["gold"]);
            var contribution = Assert.Single(run.Contributions).Value;
            Assert.Equal(40, contribution.Amounts["gold"]);
            Assert.Equal(started, contribution.LastAt);
        }
        finally
        {
            world2.Database.Stop();
        }
    }

    private GameWorld NewWorld()
    {
        var world = new GameWorld(settings, new GameServer(settings));
        world.Database.Start(dbPath);
        return world;
    }

    private void InsertGoal(int id, string properties)
    {
        world.Database.Execute(conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO community_goals (id, name, description, npc_template_id, prerequisite_goals, script_path, properties) VALUES (@id, @name, @desc, @npc, @prereqs, @script, @props)";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@name", "Goal " + id);
            cmd.Parameters.AddWithValue("@desc", "");
            cmd.Parameters.AddWithValue("@npc", 0);
            cmd.Parameters.AddWithValue("@prereqs", "");
            cmd.Parameters.AddWithValue("@script", "");
            cmd.Parameters.AddWithValue("@props", properties);
            cmd.ExecuteNonQuery();
        });
    }

    private void InsertRun(int goalId, int run, string status, DateTime startedAt,
                           DateTime? completedAt = null, string totals = "{}", string? results = null)
    {
        world.Database.Execute(conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO community_goal_runs (goal_id, run, status, started_at, completed_at, totals, results) VALUES (@g, @r, @s, @start, @end, @totals, @results)";
            cmd.Parameters.AddWithValue("@g", goalId);
            cmd.Parameters.AddWithValue("@r", run);
            cmd.Parameters.AddWithValue("@s", status);
            cmd.Parameters.AddWithValue("@start", startedAt.ToString("o"));
            cmd.Parameters.AddWithValue("@end", (object?)completedAt?.ToString("o") ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@totals", totals);
            cmd.Parameters.AddWithValue("@results", (object?)results ?? DBNull.Value);
            cmd.ExecuteNonQuery();
        });
    }

    private void InsertContribution(int goalId, int run, int playerId, string amounts, DateTime lastAt,
                                    int? claimedBracket = null, DateTime? claimedAt = null)
    {
        world.Database.Execute(conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO community_goal_contributions (goal_id, run, player_id, amounts, last_at, claimed_bracket, claimed_at) VALUES (@g, @r, @p, @amounts, @last, @bracket, @claimed)";
            cmd.Parameters.AddWithValue("@g", goalId);
            cmd.Parameters.AddWithValue("@r", run);
            cmd.Parameters.AddWithValue("@p", playerId);
            cmd.Parameters.AddWithValue("@amounts", amounts);
            cmd.Parameters.AddWithValue("@last", lastAt.ToString("o"));
            cmd.Parameters.AddWithValue("@bracket", (object?)claimedBracket ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@claimed", (object?)claimedAt?.ToString("o") ?? DBNull.Value);
            cmd.ExecuteNonQuery();
        });
    }

    private void WaitDrained()
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (world.Database.PendingCount > 0 && DateTime.UtcNow < deadline)
            Thread.Sleep(10);
        Assert.Equal(0, world.Database.PendingCount);
    }

    private void WaitReDirtyEnqueued()
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (world.PendingCompletionCount == 0 && DateTime.UtcNow < deadline)
            Thread.Sleep(10);
        Assert.True(world.PendingCompletionCount > 0);
    }

    private static void RunSql(SQLiteConnection conn, string sql)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }
}
