using System.Data.SQLite;

namespace Goose.Tests;

/// <summary>Exercises the real INSERT/UPDATE strings in Player.cs. The parse-helper tests
/// above cannot catch an unbound @playerProperties parameter or a missing comma.</summary>
public class PlayerPropertiesPersistenceTests : IDisposable
{
    private readonly string dbPath =
        Path.Combine(Path.GetTempPath(), "player-props-" + Guid.NewGuid().ToString("N") + ".db");

    private SQLiteConnection OpenWithPlayersTable()
    {
        var conn = new SQLiteConnection("Data Source=" + dbPath + "; Version=3;");
        conn.Open();
        using var cmd = conn.CreateCommand();
        // The shipped schema, so a column added to players.sql without being added to the
        // INSERT column list fails here rather than in production.
        cmd.CommandText = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "sql", "players.sql"));
        cmd.ExecuteNonQuery();
        return conn;
    }

    [Fact]
    public void A_new_player_row_persists_and_reloads_its_properties()
    {
        using var conn = OpenWithPlayersTable();

        var player = MakeMinimalPlayer(playerId: 1);
        player.Properties["dimension.max"] = 3;
        RunInsert(player, conn);

        Assert.Equal(3, ReloadProperties(conn, 1).GetProperty<int>("dimension.max"));
    }

    [Fact]
    public void An_existing_player_row_persists_a_changed_property()
    {
        using var conn = OpenWithPlayersTable();

        var player = MakeMinimalPlayer(playerId: 1);
        player.Properties["dimension.max"] = 3;
        RunInsert(player, conn);

        player.Properties["dimension.max"] = 5;
        RunUpdate(player, conn);

        Assert.Equal(5, ReloadProperties(conn, 1).GetProperty<int>("dimension.max"));
    }

    private static PropertiesDictionary ReloadProperties(SQLiteConnection conn, int playerId)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT player_properties FROM players WHERE player_id=" + playerId;
        var loaded = new Player(0);
        loaded.LoadPropertiesFromColumn(Convert.ToString(cmd.ExecuteScalar()));
        return loaded.Properties;
    }

    private static Player MakeMinimalPlayer(int playerId)
    {
        // Only what the NOT NULL columns in players.sql need; every other field keeps its
        // default. The save path dereferences BaseStats, so that gets a fresh set.
        var player = new Player(0)
        {
            PlayerID = playerId,
            Name = "Test",
            Title = "",
            Surname = "",
            PasswordHash = "hash",
            PasswordSalt = "salt",
            BaseStats = new AttributeSet(),
        };
        return player;
    }

    private static void RunInsert(Player player, SQLiteConnection conn)
    {
        // Mirror SaveToDatabase: build the query string on the (game) thread, snapshot the
        // properties, then execute the real INSERT string from Player.cs.
        using var command = player.BuildInsertCommand(
            conn,
            player.BuildInsertQuery(),
            player.GuildID,
            player.Name,
            player.Title,
            player.Surname,
            player.UnbanDate.HasValue ? (object)player.UnbanDate.Value : DBNull.Value,
            JsonHelper.Serialize(player.Properties.Clone()));
        command.ExecuteNonQuery();
    }

    private static void RunUpdate(Player player, SQLiteConnection conn)
    {
        // Mirror SaveToDatabase: build the query string on the (game) thread, snapshot the
        // properties, then execute the real UPDATE string from Player.cs.
        using var command = player.BuildUpdateCommand(
            conn,
            player.BuildUpdateQuery(),
            player.GuildID,
            player.Name,
            player.Title,
            player.Surname,
            player.UnbanDate.HasValue ? (object)player.UnbanDate.Value : DBNull.Value,
            JsonHelper.Serialize(player.Properties.Clone()));
        command.ExecuteNonQuery();
    }

    public void Dispose()
    {
        SQLiteConnection.ClearAllPools();
        if (File.Exists(dbPath)) File.Delete(dbPath);
    }
}
