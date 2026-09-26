using System.Globalization;
using Goose;
using Goose.Testing;
using Xunit;

namespace Goose.Tests
{
    [Collection("ProcessEnvironment")]
    public class LogUtcTests
    {
        [Fact]
        public void New_log_uses_utc_in_a_non_utc_process_timezone()
        {
            using var scope = new ProcessEnvironmentScope("Pacific/Honolulu", CultureInfo.InvariantCulture);

            var before = DateTime.UtcNow;
            var log = new Log(Log.Types.Chat, 1, "hello");
            var after = DateTime.UtcNow;

            Assert.Equal(DateTimeKind.Utc, log.Time.Kind);
            Assert.InRange(log.Time, before, after);
        }

        [Fact]
        public void Saved_log_is_an_integer_with_the_exact_utc_ticks()
        {
            using var fixture = new TestWorldFixture();
            StartLogsDatabase(fixture);

            var time = new DateTime(2024, 1, 15, 10, 30, 45, DateTimeKind.Utc).AddTicks(1234567);
            var log = new Log(Log.Types.Chat, 1, "hello");
            log.Time = time;
            log.SaveToDatabase(fixture.World);
            fixture.World.Database.Execute(conn => { });

            var (storage, value) = fixture.World.Database.Execute(conn =>
            {
                using var command = conn.CreateCommand();
                command.CommandText = "SELECT typeof(log_date), log_date FROM logs";
                using var reader = command.ExecuteReader();
                reader.Read();
                return (reader.GetString(0), reader.GetValue(1));
            });

            Assert.Equal("integer", storage);
            Assert.Equal(time.Ticks, Convert.ToInt64(value));
        }

        [Fact]
        public void Save_rejects_non_utc_time_before_enqueue()
        {
            using var fixture = new TestWorldFixture();
            StartLogsDatabase(fixture);

            var local = new Log(Log.Types.Chat, 1, "local");
            local.Time = DateTime.Now;
            local.SaveToDatabase(fixture.World);

            var unspecified = new Log(Log.Types.Chat, 2, "unspecified");
            unspecified.Time = new DateTime(2024, 1, 15, 10, 30, 45, DateTimeKind.Unspecified);
            unspecified.SaveToDatabase(fixture.World);

            fixture.World.Database.Execute(conn => { });

            var count = fixture.World.Database.Execute(conn =>
            {
                using var command = conn.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM logs";
                return (long)command.ExecuteScalar();
            });

            Assert.Equal(0, count);
        }

        [Fact]
        public void Saved_ticks_order_across_month_and_year_boundaries()
        {
            using var fixture = new TestWorldFixture();
            StartLogsDatabase(fixture);

            var times = new[]
            {
                new DateTime(2023, 12, 31, 23, 59, 59, DateTimeKind.Utc).AddTicks(9999999),
                new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2024, 1, 31, 23, 59, 59, DateTimeKind.Utc).AddTicks(9999999),
                new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks(1),
            };
            for (int i = 0; i < times.Length; i++)
            {
                var log = new Log(Log.Types.Chat, i + 1, "t" + i);
                log.Time = times[i];
                log.SaveToDatabase(fixture.World);
            }
            fixture.World.Database.Execute(conn => { });

            var stored = fixture.World.Database.Execute(conn =>
            {
                using var command = conn.CreateCommand();
                command.CommandText = "SELECT log_date FROM logs ORDER BY log_date DESC";
                using var reader = command.ExecuteReader();
                var values = new List<long>();
                while (reader.Read()) values.Add(reader.GetInt64(0));
                return values;
            });

            var expected = times.Select(t => t.Ticks).OrderByDescending(t => t).ToList();
            Assert.Equal(expected, stored);
        }

        private static void StartLogsDatabase(TestWorldFixture fixture)
        {
            fixture.World.Database.Start(Path.Combine(fixture.DataDirectory, "test.db"));
            fixture.World.Database.Execute(conn =>
            {
                using var command = conn.CreateCommand();
                command.CommandText = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "sql", "logs.sql"));
                command.ExecuteNonQuery();
            });
        }
    }
}
