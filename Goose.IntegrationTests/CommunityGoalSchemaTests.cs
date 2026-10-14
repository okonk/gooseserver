using System.Data.SQLite;
using System.Reflection;

namespace Goose.IntegrationTests;

public class CommunityGoalSchemaTests : IDisposable
{
    private readonly string dbPath =
        Path.Combine(Path.GetTempPath(), "communitygoal-schema-" + Guid.NewGuid().ToString("N") + ".db");

    private readonly GameWorld world;

    public CommunityGoalSchemaTests()
    {
        var settings = new GooseSettings
        {
            InventorySize = 30, EquippedSize = 20, CombineBagSize = 10, SpellbookSize = 30,
        };
        world = new GameWorld(settings, new GameServer(settings));
        world.Database.Start(dbPath);
    }

    [Fact]
    public void CreateDatabaseSchema_CreatesCommunityGoalTables()
    {
        typeof(GameWorld)
            .GetMethod("CreateDatabaseSchema", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(world, null);
        world.Database.Execute(conn => { });

        AssertTables();
    }

    [Fact]
    public void MigrateDatabaseSchema_AddsCommunityGoalTables()
    {
        foreach (var file in new[] { "players", "logs" })
        {
            world.Database.Execute(conn => RunSql(conn, File.ReadAllText(
                Path.Combine(AppContext.BaseDirectory, "sql", file + ".sql"))));
        }

        typeof(GameWorld)
            .GetMethod("MigrateDatabaseSchema", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(world, null);
        world.Database.Execute(conn => { });

        AssertTables();
    }

    [Fact]
    public void MigrateDatabaseSchema_RunningTwiceIsANoOp()
    {
        foreach (var file in new[] { "players", "logs" })
        {
            world.Database.Execute(conn => RunSql(conn, File.ReadAllText(
                Path.Combine(AppContext.BaseDirectory, "sql", file + ".sql"))));
        }

        var migrate = typeof(GameWorld)
            .GetMethod("MigrateDatabaseSchema", BindingFlags.NonPublic | BindingFlags.Instance)!;
        migrate.Invoke(world, null);
        migrate.Invoke(world, null);
        world.Database.Execute(conn => { });

        AssertTables();
    }

    private void AssertTables()
    {
        Assert.True(TableExists("community_goal_runs"));
        Assert.True(TableExists("community_goal_contributions"));
        Assert.True(TableExists("world_flags"));

        AssertColumns("community_goal_runs",
            "goal_id", "run", "status", "started_at", "completed_at", "totals", "results");
        AssertColumns("community_goal_contributions",
            "goal_id", "run", "player_id", "amounts", "last_at", "claimed_bracket", "claimed_at");
        AssertColumns("world_flags", "name", "value", "set_at", "source_goal_id");
    }

    private bool TableExists(string table)
    {
        return world.Database.Execute(conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name=@name";
            cmd.Parameters.AddWithValue("@name", table);
            return cmd.ExecuteScalar() is not null;
        });
    }

    private void AssertColumns(string table, params string[] columns)
    {
        var actual = world.Database.Execute(conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA table_info(" + table + ")";
            using var reader = cmd.ExecuteReader();
            var names = new List<string>();
            while (reader.Read()) names.Add(reader.GetString("name"));
            return names;
        });

        foreach (var column in columns)
            Assert.Contains(column, actual);
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
