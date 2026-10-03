using System.Data.SQLite;

namespace Goose.IntegrationTests;

public class DatabaseTransactionTests : IDisposable
{
    private readonly string dbPath =
        Path.Combine(Path.GetTempPath(), "db-txn-" + Guid.NewGuid().ToString("N") + ".db");
    private readonly Database db = new();

    public DatabaseTransactionTests()
    {
        db.Start(dbPath);
        db.Execute(conn => RunSql(conn, "CREATE TABLE t (id INTEGER PRIMARY KEY, v TEXT);"));
    }

    [Fact]
    public void A_failed_transaction_rolls_back_and_does_not_run_on_commit()
    {
        bool committed = false;
        db.EnqueueTransaction(conn =>
        {
            RunSql(conn, "INSERT INTO t (id, v) VALUES (1, 'x');");
            throw new Exception("forced failure");
        }, () => committed = true);

        // Enqueued after the transaction item, so this runs only after the rollback.
        int rows = CountRows();

        Assert.Equal(0, rows);
        Assert.False(committed);
    }

    [Fact]
    public void A_successful_transaction_commits_and_runs_on_commit()
    {
        bool committed = false;
        db.EnqueueTransaction(conn =>
        {
            RunSql(conn, "INSERT INTO t (id, v) VALUES (1, 'x');");
        }, () => committed = true);

        int rows = CountRows();

        Assert.Equal(1, rows);
        Assert.True(committed);
    }

    [Fact]
    public void ExecuteTransaction_SecondStatementFails_RollsBackFirst()
    {
        db.ExecuteTransaction(conn =>
        {
            RunSql(conn, "INSERT INTO t (id, v) VALUES (1, 'x');");
            RunSql(conn, "INSERT INTO t (id, v) VALUES (2, 'y');");
        });

        Assert.ThrowsAny<Exception>(() => db.ExecuteTransaction(conn =>
        {
            RunSql(conn, "UPDATE t SET v='z' WHERE id=1;");
            RunSql(conn, "INSERT INTO missing_table (id) VALUES (1);");
        }));

        Assert.Equal(2, CountRows());
        Assert.Equal("x|y", Values());
    }

    private string Values()
    {
        return db.Execute(conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT v FROM t ORDER BY id;";
            using var reader = cmd.ExecuteReader();
            var values = new List<string>();
            while (reader.Read()) values.Add(reader.GetString(0));
            return string.Join("|", values);
        });
    }

    private int CountRows()
    {
        return db.Execute<int>(conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM t;";
            return Convert.ToInt32(cmd.ExecuteScalar());
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
        db.Stop();
        foreach (var suffix in new[] { "", "-wal", "-shm" })
        {
            var path = dbPath + suffix;
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
