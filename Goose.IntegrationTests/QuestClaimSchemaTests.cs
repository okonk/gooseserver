using System.Data.SQLite;

namespace Goose.IntegrationTests;

public class QuestClaimSchemaTests : PlayerFirstSaveTestBase
{
    public QuestClaimSchemaTests() : base(["players", "logs"]) { }

    [Fact]
    public void Fresh_schema_script_creates_the_table()
    {
        world.Database.Execute(conn => RunSqlFile(conn, "quest_claims"));
        Assert.Equal(1, Count("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='quest_claims'"));
    }

    [Fact]
    public void Migration_creates_the_table_on_an_existing_database()
    {
        world.MigrateDatabaseSchema();
        Assert.Equal(1, Count("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='quest_claims'"));
    }

    private static void RunSqlFile(SQLiteConnection conn, string name)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "sql", name + ".sql"));
        cmd.ExecuteNonQuery();
    }
}
