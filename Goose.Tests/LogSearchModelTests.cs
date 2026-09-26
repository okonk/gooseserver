using System.Data;
using System.Data.SQLite;
using System.Reflection;
using Goose.Logs;
using Xunit;

namespace Goose.Tests
{
    public class LogSearchModelTests
    {
        private static readonly Type[] Part1Types =
        {
            typeof(LogFreshSearchInput),
            typeof(LogSearchQuery),
            typeof(LogPageCursor),
            typeof(LogValidationResult),
        };

        [Fact]
        public void No_public_string_or_base64_cursor_api_exists()
        {
            foreach (Type type in Part1Types)
            {
                foreach (MemberInfo member in type.GetMembers(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance))
                {
                    Assert.False(
                        member.Name.Contains("Base64", StringComparison.OrdinalIgnoreCase),
                        $"{type.Name}.{member.Name} is a Base64 cursor API");
                }
            }

            foreach (PropertyInfo property in typeof(LogPageCursor).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                Assert.NotEqual(typeof(string), property.PropertyType);
                foreach (ParameterInfo index in property.GetIndexParameters())
                    Assert.NotEqual(typeof(string), index.ParameterType);
            }

            PropertyInfo cursor = typeof(LogSearchQuery).GetProperty("Cursor")!;
            Assert.Equal(typeof(LogPageCursor), cursor.PropertyType);

            foreach (PropertyInfo property in typeof(LogFreshSearchInput).GetProperties())
            {
                Assert.False(
                    property.Name.Contains("Cursor", StringComparison.OrdinalIgnoreCase)
                        || property.Name.Contains("Token", StringComparison.OrdinalIgnoreCase)
                        || property.Name.Contains("Action", StringComparison.OrdinalIgnoreCase),
                    $"LogFreshSearchInput.{property.Name} is a cursor/token/action field");
            }
        }

        [Fact]
        public void WithCursor_preserves_every_validated_filter_and_participant()
        {
            using var connection = OpenPlayersDb();
            var result = LogQueryValidator.ValidateFresh(connection, new LogFreshSearchInput
            {
                StartUtcMilliseconds = 1_700_000_000_000,
                EndUtcMilliseconds = 1_700_000_004_000,
                Participant = "Alpha",
                MapId = 7,
                EventTypeIds = new[] { 14, 3 },
                Text = "hello",
            });
            Assert.True(result.IsSuccess);
            LogSearchQuery query = result.Query;
            Assert.Null(query.Cursor);

            LogPageCursor cursor = new(123, 9_000, 55);
            LogSearchQuery paged = query.WithCursor(cursor);

            Assert.Same(cursor, paged.Cursor);
            Assert.Equal(query.StartUtcTicks, paged.StartUtcTicks);
            Assert.Equal(query.EndUtcTicks, paged.EndUtcTicks);
            Assert.Equal(1, paged.ParticipantId);
            Assert.Equal(7, paged.MapId);
            Assert.Equal(new[] { 3, 14 }, paged.EventTypeIds);
            Assert.Equal("hello", paged.Text);
        }

        [Fact]
        public void First_page_cursor_has_null_boundaries_and_continuation_populates_both()
        {
            var first = new LogPageCursor(10, null, null);
            Assert.Null(first.BeforeUtcTicks);
            Assert.Null(first.BeforeRowId);

            var continuation = new LogPageCursor(10, 1_000, 2);
            Assert.Equal(1_000, continuation.BeforeUtcTicks);
            Assert.Equal(2, continuation.BeforeRowId);
        }

        [Fact]
        public void Query_event_ids_are_decoupled_from_the_input_list()
        {
            using var connection = OpenPlayersDb();
            var list = new List<int> { 14, 3 };
            var result = LogQueryValidator.ValidateFresh(connection, new LogFreshSearchInput
            {
                StartUtcMilliseconds = 1_700_000_000_000,
                EndUtcMilliseconds = 1_700_000_004_000,
                EventTypeIds = list,
            });
            Assert.True(result.IsSuccess);

            list.Add(99_999);
            list.Clear();

            Assert.Equal(new[] { 3, 14 }, result.Query.EventTypeIds);
        }

        private static SQLiteConnection OpenPlayersDb()
        {
            var connection = new SQLiteConnection("Data Source=:memory:");
            connection.Open();
            {
                var command = connection.CreateCommand();
                command.CommandText = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "sql", "players.sql"));
                command.ExecuteNonQuery();
            }
            using (var insert = connection.CreateCommand())
            {
                insert.CommandText = "INSERT INTO players (player_id, player_name, access_status, password_hash, password_salt) VALUES (1, 'Alpha', 2, 'h', 's')";
                insert.ExecuteNonQuery();
            }
            return connection;
        }
    }
}
