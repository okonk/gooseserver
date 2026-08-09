using System.Data.SQLite;

namespace Goose.Tests;

public class SchemaMigrationTests
{
    [Fact]
    public void Adds_a_missing_column_and_is_idempotent()
    {
        using var conn = new SQLiteConnection("Data Source=:memory:");
        conn.Open();
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "CREATE TABLE players (player_id INT PRIMARY KEY);";
            cmd.ExecuteNonQuery();
        }

        Assert.False(GameWorld.ColumnExists(conn, "players", "player_properties"));

        GameWorld.AddColumnIfMissing(conn, "players", "player_properties", "TEXT DEFAULT '' NOT NULL");
        Assert.True(GameWorld.ColumnExists(conn, "players", "player_properties"));

        // Running again must not throw - migrations run on every startup.
        GameWorld.AddColumnIfMissing(conn, "players", "player_properties", "TEXT DEFAULT '' NOT NULL");
        Assert.True(GameWorld.ColumnExists(conn, "players", "player_properties"));
    }
}
