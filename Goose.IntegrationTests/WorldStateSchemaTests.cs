using System.Data.SQLite;
using System.Reflection;

namespace Goose.IntegrationTests;

public class WorldStateSchemaTests : IDisposable
{
    private readonly string dbPath =
        Path.Combine(Path.GetTempPath(), "worldstate-schema-" + Guid.NewGuid().ToString("N") + ".db");

    private readonly GameWorld world;

    public WorldStateSchemaTests()
    {
        var settings = new GooseSettings
        {
            InventorySize = 30, EquippedSize = 20, CombineBagSize = 10, SpellbookSize = 30,
        };
        world = new GameWorld(settings, new GameServer(settings));
        world.Database.Start(dbPath);
    }

    [Fact]
    public void CreateDatabaseSchema_CreatesWorldStateTable()
    {
        typeof(GameWorld)
            .GetMethod("CreateDatabaseSchema", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(world, null);
        world.Database.Execute(conn => { });

        AssertWorldStateTable();
    }

    [Fact]
    public void MigrateDatabaseSchema_AddsWorldStateTable()
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

        AssertWorldStateTable();
    }

    private void AssertWorldStateTable()
    {
        Assert.True(world.Database.Execute(conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name='world_state'";
            return cmd.ExecuteScalar() is not null;
        }));

        Assert.True(ColumnExists("key"));
        Assert.True(ColumnExists("value"));
    }

    private bool ColumnExists(string column)
    {
        return world.Database.Execute(conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA table_info(world_state)";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                if (reader.GetString("name") == column) return true;
            }
            return false;
        });
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
