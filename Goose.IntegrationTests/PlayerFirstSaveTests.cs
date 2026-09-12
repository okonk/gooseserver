using Goose.Testing;
using System.Data.SQLite;

namespace Goose.IntegrationTests;

public abstract class PlayerFirstSaveTestBase : IDisposable
{
    private readonly string dbPath =
        Path.Combine(Path.GetTempPath(), "first-save-" + Guid.NewGuid().ToString("N") + ".db");

    protected readonly GooseSettings settings = new()
    {
        InventorySize = 30, EquippedSize = 20, CombineBagSize = 10, SpellbookSize = 30,
    };

    protected readonly GameWorld world;

    /// <param name="schemaFiles">Hand-written schemas, read from the shipped sql/ scripts.</param>
    /// <param name="generatedTables">Game data tables, whose DDL the spreadsheet pipeline owns.</param>
    protected PlayerFirstSaveTestBase(string[] schemaFiles, string[]? generatedTables = null,
                                      bool withQuestStatus = true)
    {
        world = new GameWorld(settings, new GameServer(settings));
        var db = world.Database;
        db.Start(dbPath);
        foreach (var file in schemaFiles)
        {
            db.Execute(conn => RunSql(conn, File.ReadAllText(
                Path.Combine(AppContext.BaseDirectory, "sql", file + ".sql"))));
        }
        if (generatedTables is { Length: > 0 })
        {
            var ddl = SchemaDdl.ForAll(generatedTables);
            db.Execute(conn => RunSql(conn, ddl));
        }
        if (!withQuestStatus)
        {
            // The quest upsert is the last statement of a player save, so dropping its table
            // makes the save fail there and roll back everything queued before it.
            db.Execute(conn => RunSql(conn, "DROP TABLE quest_status"));
        }
    }

    private static void RunSql(SQLiteConnection conn, string sql)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    protected int Count(string sql)
    {
        return world.Database.Execute<int>(conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            return Convert.ToInt32(cmd.ExecuteScalar());
        });
    }

    protected Player MakePlayer()
    {
        var player = new Player(0)
        {
            PlayerID = 1,
            Name = "Test",
            Title = "",
            Surname = "",
            PasswordHash = "hash",
            PasswordSalt = "salt",
            BaseStats = new AttributeSet(),
        };
        player.Inventory = new Inventory(player, world.Settings);
        player.Spellbook = new Spellbook(player, world.Settings);
        player.Bank = new PlayerBank();
        return player;
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

public class PlayerFirstSaveTests : PlayerFirstSaveTestBase
{
    public PlayerFirstSaveTests() :
        base(["players", "banks", "pets"]) { }

    [Fact]
    public void A_new_player_and_new_pet_are_persisted_and_marked_saved()
    {
        var player = MakePlayer();
        player.AutoCreatedNotSaved = true;

        var pet = new Pet
        {
            PetID = 1,
            Name = "Pet",
            Title = "",
            Surname = "",
            Class = new Class { ClassID = 1 },
            Owner = player,
            BaseStats = new AttributeSet(),
            AutoCreatedNotSaved = true,
        };
        player.Pets.Add(pet);

        player.SaveToDatabase(world);

        Assert.Equal(1, Count("SELECT COUNT(*) FROM players WHERE player_id=1"));
        Assert.Equal(1, Count("SELECT COUNT(*) FROM pets WHERE pet_id=1"));
        Assert.False(player.AutoCreatedNotSaved);
        Assert.False(pet.AutoCreatedNotSaved);
    }
}

public class PlayerFirstSaveRollbackTests : PlayerFirstSaveTestBase
{
    public PlayerFirstSaveRollbackTests()
        : base(["players", "banks", "pets"], withQuestStatus: false) { }

    [Fact]
    public void A_failed_later_statement_rolls_back_and_keeps_the_first_save_flag()
    {
        var player = MakePlayer();
        player.AutoCreatedNotSaved = true;

        player.SaveToDatabase(world);

        Assert.Equal(0, Count("SELECT COUNT(*) FROM players WHERE player_id=1"));
        Assert.True(player.AutoCreatedNotSaved);
    }
}
