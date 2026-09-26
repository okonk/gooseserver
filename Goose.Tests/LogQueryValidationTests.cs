using System.Data;
using System.Data.SQLite;
using Goose.Logs;
using Xunit;

namespace Goose.Tests
{
    public class LogQueryValidationTests
    {
        private const long DefaultStart = 1_700_000_000_000;
        private const long DefaultEnd = DefaultStart + 3_600_000;
        private const long ThirtyOneDaysMs = 31L * 86_400_000;
        private const long SevenDaysMs = 7L * 86_400_000;

        [Fact]
        public void Unix_millisecond_bounds_convert_exactly_to_utc_ticks()
        {
            using var connection = OpenPlayersDb();
            var result = LogQueryValidator.ValidateFresh(connection, Input());

            Assert.True(result.IsSuccess);
            Assert.Equal(DateTime.UnixEpoch.Ticks + DefaultStart * TimeSpan.TicksPerMillisecond, result.Query.StartUtcTicks);
            Assert.Equal(DateTime.UnixEpoch.Ticks + DefaultEnd * TimeSpan.TicksPerMillisecond, result.Query.EndUtcTicks);
        }

        [Fact]
        public void Start_equal_to_end_fails()
        {
            using var connection = OpenPlayersDb();
            var result = LogQueryValidator.ValidateFresh(connection, Input(start: 100, end: 100));

            Assert.Equal(LogValidationError.InvalidBounds, result.Code);
            Assert.Null(result.Query);
        }

        [Fact]
        public void Start_after_end_fails()
        {
            using var connection = OpenPlayersDb();
            var result = LogQueryValidator.ValidateFresh(connection, Input(start: 200, end: 100));

            Assert.Equal(LogValidationError.InvalidBounds, result.Code);
        }

        [Theory]
        [InlineData(long.MinValue)]
        [InlineData(long.MaxValue)]
        public void Out_of_range_unix_values_fail(long value)
        {
            using var connection = OpenPlayersDb();
            var result = LogQueryValidator.ValidateFresh(connection, Input(start: value, end: value + 1));

            Assert.Equal(LogValidationError.InvalidBounds, result.Code);
        }

        [Fact]
        public void Exactly_31_days_is_valid()
        {
            using var connection = OpenPlayersDb();
            var result = LogQueryValidator.ValidateFresh(connection, Input(start: 0, end: ThirtyOneDaysMs));

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void Thirty_one_days_plus_one_millisecond_fails()
        {
            using var connection = OpenPlayersDb();
            var result = LogQueryValidator.ValidateFresh(connection, Input(start: 0, end: ThirtyOneDaysMs + 1));

            Assert.Equal(LogValidationError.RangeTooWide, result.Code);
        }

        [Fact]
        public void Exactly_7_days_with_text_is_valid()
        {
            using var connection = OpenPlayersDb();
            var result = LogQueryValidator.ValidateFresh(connection, Input(start: 0, end: SevenDaysMs, text: "x"));

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void Seven_days_plus_one_millisecond_with_text_fails()
        {
            using var connection = OpenPlayersDb();
            var result = LogQueryValidator.ValidateFresh(connection, Input(start: 0, end: SevenDaysMs + 1, text: "x"));

            Assert.Equal(LogValidationError.TextRangeTooWide, result.Code);
        }

        [Fact]
        public void Whitespace_only_text_uses_the_seven_day_limit()
        {
            using var connection = OpenPlayersDb();
            var result = LogQueryValidator.ValidateFresh(connection, Input(start: 0, end: SevenDaysMs + 1, text: "   "));

            Assert.Equal(LogValidationError.TextRangeTooWide, result.Code);
        }

        [Fact]
        public void Empty_text_uses_the_thirty_one_day_limit()
        {
            using var connection = OpenPlayersDb();
            var result = LogQueryValidator.ValidateFresh(connection, Input(start: 0, end: 10L * 86_400_000, text: ""));

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void Embedded_nul_in_text_fails_as_invalid_text()
        {
            using var connection = OpenPlayersDb();
            var result = LogQueryValidator.ValidateFresh(connection, Input(text: "ab\0cd"));

            Assert.Equal(LogValidationError.InvalidText, result.Code);
        }

        [Fact]
        public void Embedded_nul_in_participant_fails_before_database_lookup()
        {
            using var connection = OpenPlayersDb();
            connection.Close();
            var result = LogQueryValidator.ValidateFresh(connection, Input(participant: "ab\0cd"));

            Assert.Equal(LogValidationError.InvalidParticipant, result.Code);
        }

        [Fact]
        public void Empty_type_list_means_all()
        {
            using var connection = OpenPlayersDb();
            var result = LogQueryValidator.ValidateFresh(connection, Input(types: Array.Empty<int>()));

            Assert.True(result.IsSuccess);
            Assert.Empty(result.Query.EventTypeIds);
        }

        [Fact]
        public void Duplicate_type_ids_are_sorted_and_deduplicated()
        {
            using var connection = OpenPlayersDb();
            var result = LogQueryValidator.ValidateFresh(connection, Input(types: new[] { 10013, 14, 3, 14 }));

            Assert.True(result.IsSuccess);
            Assert.Equal(new[] { 3, 14, 10013 }, result.Query.EventTypeIds);
        }

        [Fact]
        public void Retired_and_gm_type_ids_pass()
        {
            using var connection = OpenPlayersDb();
            var result = LogQueryValidator.ValidateFresh(connection, Input(types: new[] { 14, 10013 }));

            Assert.True(result.IsSuccess);
            Assert.Equal(new[] { 14, 10013 }, result.Query.EventTypeIds);
        }

        [Fact]
        public void Unknown_type_id_fails()
        {
            using var connection = OpenPlayersDb();
            var result = LogQueryValidator.ValidateFresh(connection, Input(types: new[] { 999 }));

            Assert.Equal(LogValidationError.InvalidEventType, result.Code);
        }

        [Fact]
        public void Map_zero_means_all()
        {
            using var connection = OpenPlayersDb();
            var result = LogQueryValidator.ValidateFresh(connection, Input(mapId: 0));

            Assert.True(result.IsSuccess);
            Assert.Null(result.Query.MapId);
        }

        [Fact]
        public void Any_positive_map_is_accepted_for_retired_maps()
        {
            using var connection = OpenPlayersDb();
            var result = LogQueryValidator.ValidateFresh(connection, Input(mapId: 9999));

            Assert.True(result.IsSuccess);
            Assert.Equal(9999, result.Query.MapId);
        }

        [Fact]
        public void Negative_map_fails()
        {
            using var connection = OpenPlayersDb();
            var result = LogQueryValidator.ValidateFresh(connection, Input(mapId: -1));

            Assert.Equal(LogValidationError.InvalidMap, result.Code);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Trimmed_empty_participant_means_all(string? participant)
        {
            using var connection = OpenPlayersDb();
            var result = LogQueryValidator.ValidateFresh(connection, Input(participant: participant));

            Assert.True(result.IsSuccess);
            Assert.Null(result.Query.ParticipantId);
        }

        [Fact]
        public void Hash_id_resolves_without_requiring_a_player_row()
        {
            using var connection = OpenPlayersDb();
            var result = LogQueryValidator.ValidateFresh(connection, Input(participant: "#42"));

            Assert.True(result.IsSuccess);
            Assert.Equal(42, result.Query.ParticipantId);
        }

        [Theory]
        [InlineData("#")]
        [InlineData("#abc")]
        [InlineData("#0")]
        [InlineData("#-1")]
        [InlineData("#1.5")]
        [InlineData("#99999999999")]
        public void Malformed_id_forms_fail_as_id_errors(string participant)
        {
            using var connection = OpenPlayersDb();
            var result = LogQueryValidator.ValidateFresh(connection, Input(participant: participant));

            Assert.Equal(LogValidationError.InvalidParticipant, result.Code);
        }

        [Fact]
        public void Exact_name_is_case_insensitive()
        {
            using var connection = OpenPlayersDb();
            var result = LogQueryValidator.ValidateFresh(connection, Input(participant: "alpha"));

            Assert.True(result.IsSuccess);
            Assert.Equal(1, result.Query.ParticipantId);
        }

        [Fact]
        public void Deleted_row_is_found_by_name()
        {
            using var connection = OpenPlayersDb();
            var result = LogQueryValidator.ValidateFresh(connection, Input(participant: "DeletedGuy"));

            Assert.True(result.IsSuccess);
            Assert.Equal(4, result.Query.ParticipantId);
        }

        [Fact]
        public void Missing_name_fails()
        {
            using var connection = OpenPlayersDb();
            var result = LogQueryValidator.ValidateFresh(connection, Input(participant: "nobody"));

            Assert.Equal(LogValidationError.ParticipantNotFound, result.Code);
        }

        [Fact]
        public void Two_case_insensitive_matches_are_ambiguous()
        {
            using var connection = OpenPlayersDb();
            var result = LogQueryValidator.ValidateFresh(connection, Input(participant: "Beta"));

            Assert.Equal(LogValidationError.ParticipantAmbiguous, result.Code);
        }

        [Fact]
        public void Successful_output_stores_only_the_resolved_participant_id()
        {
            using var connection = OpenPlayersDb();
            var result = LogQueryValidator.ValidateFresh(connection, Input(participant: "Alpha"));

            Assert.True(result.IsSuccess);
            Assert.Equal(1, result.Query.ParticipantId);
            Assert.DoesNotContain(typeof(LogSearchQuery).GetProperties(), property => property.PropertyType == typeof(string) && property.Name != "Text");
        }

        [Fact]
        public void Text_with_sql_metacharacters_is_stored_unchanged()
        {
            string literal = "100%_off\\back'quote'\"dq\" DROP TABLE logs;--";
            using var connection = OpenPlayersDb();
            var result = LogQueryValidator.ValidateFresh(connection, Input(text: literal));

            Assert.True(result.IsSuccess);
            Assert.Equal(literal, result.Query.Text);
        }

        private static LogFreshSearchInput Input(long? start = null, long? end = null, string? participant = null, int mapId = 0, int[]? types = null, string? text = null)
        {
            long s = start ?? DefaultStart;
            long e = end ?? DefaultEnd;
            return new LogFreshSearchInput
            {
                StartUtcMilliseconds = s,
                EndUtcMilliseconds = e,
                Participant = participant,
                MapId = mapId,
                EventTypeIds = types ?? Array.Empty<int>(),
                Text = text,
            };
        }

        private static SQLiteConnection OpenPlayersDb()
        {
            var connection = new SQLiteConnection("Data Source=:memory:");
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "sql", "players.sql"));
            command.ExecuteNonQuery();
            InsertPlayer(connection, 1, "Alpha", 2);
            InsertPlayer(connection, 2, "beta", 2);
            InsertPlayer(connection, 3, "BETA", 2);
            InsertPlayer(connection, 4, "DeletedGuy", 0);
            return connection;
        }

        private static void InsertPlayer(SQLiteConnection connection, int id, string name, int accessStatus)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO players (player_id, player_name, access_status, password_hash, password_salt) VALUES (@id, @name, @status, @hash, @salt)";
            command.Parameters.Add(new SQLiteParameter("@id", id));
            command.Parameters.Add(new SQLiteParameter("@name", name));
            command.Parameters.Add(new SQLiteParameter("@status", accessStatus));
            command.Parameters.Add(new SQLiteParameter("@hash", "h"));
            command.Parameters.Add(new SQLiteParameter("@salt", "s"));
            command.ExecuteNonQuery();
        }
    }
}
