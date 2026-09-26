using Goose.Logs;
using Xunit;

namespace Goose.IntegrationTests
{
    public class LogQueryFilteringTests
    {
        private static readonly long StartTicks = DateTime.UnixEpoch.Ticks + 10_000_000_000L;
        private static readonly long EndTicks = StartTicks + TimeSpan.TicksPerDay;
        private const long SnapshotCeiling = 1_000_000_000L;

        private static long TicksAfterStart(long seconds) => StartTicks + seconds * TimeSpan.TicksPerSecond;

        [Fact]
        public void Every_query_shape_contains_the_canonical_date_storage_predicate()
        {
            using var fixture = new LogQueryFixture();

            var commands = new (LogQueryCommand Command, int Branches)[]
            {
                (fixture.BuildCommand(LogQueryFixture.SearchQuery(StartTicks, EndTicks)), 1),
                (fixture.BuildCommand(LogQueryFixture.SearchQuery(StartTicks, EndTicks, participant: 7)), 2),
                (fixture.BuildCommand(LogQueryFixture.SearchQuery(StartTicks, EndTicks, types: new[] { 11, 12 })), 1),
                (fixture.BuildCommand(LogQueryFixture.SearchQuery(StartTicks, EndTicks, map: 3)), 1),
                (fixture.BuildCommand(LogQueryFixture.SearchQuery(StartTicks, EndTicks, text: "hello")), 1),
                (fixture.BuildCommand(LogQueryFixture.SearchQuery(StartTicks, EndTicks, participant: 7, map: 3,
                    types: new[] { 13 }, text: "x",
                    cursor: new LogPageCursor(SnapshotCeiling, TicksAfterStart(3600), 42))), 2),
            };

            foreach (var (command, branches) in commands)
            {
                int count = CountOccurrences(command.CommandText, "typeof(log_date) = 'integer'");
                Assert.Equal(branches, count);
            }
        }

        [Fact]
        public void Exact_start_ticks_are_included_and_exact_end_ticks_are_excluded()
        {
            using var fixture = new LogQueryFixture();
            fixture.InsertLog(StartTicks - 1, 0, 1, 0, 0, 0, 0, "before start");
            long atStart = fixture.InsertLog(StartTicks, 0, 1, 0, 0, 0, 0, "at start");
            long mid = fixture.InsertLog(TicksAfterStart(3600), 0, 1, 0, 0, 0, 0, "mid");
            long beforeEnd = fixture.InsertLog(EndTicks - 1, 0, 1, 0, 0, 0, 0, "before end");
            fixture.InsertLog(EndTicks, 0, 1, 0, 0, 0, 0, "at end");

            var rows = fixture.Query(LogQueryFixture.SearchQuery(StartTicks, EndTicks), SnapshotCeiling);

            Assert.Equal(new long[] { beforeEnd, mid, atStart }, rows.Select(r => r.RowId).ToArray());
        }

        [Fact]
        public void Malformed_date_storage_is_preserved_in_sqlite_but_excluded_from_results()
        {
            using var fixture = new LogQueryFixture();
            long good = fixture.InsertLog(TicksAfterStart(100), 0, 1, 0, 0, 0, 0, "canonical");
            fixture.InsertRawLog("2024-01-01 10:00:00", 0, 1, 0, 0, 0, 0, "text date");
            fixture.InsertRawLog(7.5, 0, 1, 0, 0, 0, 0, "real date");
            fixture.InsertRawLog(new byte[] { 1 }, 0, 1, 0, 0, 0, 0, "blob date");

            Assert.Equal(4, fixture.CountLogs());
            Assert.Equal(new[] { "integer", "text", "real", "blob" }, fixture.LogDateStorageTypes());

            var rows = fixture.Query(LogQueryFixture.SearchQuery(StartTicks, EndTicks), SnapshotCeiling);

            Assert.Equal(new long[] { good }, rows.Select(r => r.RowId).ToArray());
        }

        [Fact]
        public void Malformed_dates_cannot_disturb_ordering()
        {
            using var fixture = new LogQueryFixture();
            long t2 = fixture.InsertLog(TicksAfterStart(200), 0, 1, 0, 0, 0, 0, "second");
            fixture.InsertRawLog("garbage", 0, 1, 0, 0, 0, 0, "malformed one");
            long t1 = fixture.InsertLog(TicksAfterStart(100), 0, 1, 0, 0, 0, 0, "first");
            fixture.InsertRawLog(1.5, 0, 1, 0, 0, 0, 0, "malformed two");
            long t3 = fixture.InsertLog(TicksAfterStart(300), 0, 1, 0, 0, 0, 0, "third");

            var rows = fixture.Query(LogQueryFixture.SearchQuery(StartTicks, EndTicks), SnapshotCeiling);

            Assert.Equal(new long[] { t3, t2, t1 }, rows.Select(r => r.RowId).ToArray());
        }

        [Fact]
        public void Single_type_filter_matches_only_that_type()
        {
            using var fixture = new LogQueryFixture();
            long pickup = fixture.InsertLog(TicksAfterStart(10), 11, 1, 0, 0, 0, 0, "12 1 Iron 2");
            fixture.InsertLog(TicksAfterStart(20), 12, 1, 0, 0, 0, 0, "12 1 Iron 2");
            fixture.InsertLog(TicksAfterStart(30), 0, 1, 0, 0, 0, 0, "chat");

            var rows = fixture.Query(
                LogQueryFixture.SearchQuery(StartTicks, EndTicks, types: new[] { 11 }), SnapshotCeiling);

            Assert.Equal(new long[] { pickup }, rows.Select(r => r.RowId).ToArray());
        }

        [Fact]
        public void Multiple_type_filter_matches_each_selected_type()
        {
            using var fixture = new LogQueryFixture();
            long tell = fixture.InsertLog(TicksAfterStart(10), 13, 1, 2, 0, 0, 0, "hi");
            fixture.InsertLog(TicksAfterStart(20), 0, 1, 0, 0, 0, 0, "chat");
            long ban = fixture.InsertLog(TicksAfterStart(30), 10010, 7, 2, 0, 0, 0, "");

            var rows = fixture.Query(
                LogQueryFixture.SearchQuery(StartTicks, EndTicks, types: new[] { 13, 10010 }), SnapshotCeiling);

            Assert.Equal(new long[] { ban, tell }, rows.Select(r => r.RowId).ToArray());
        }

        [Fact]
        public void Group_expanded_type_filter_matches_all_group_types()
        {
            using var fixture = new LogQueryFixture();
            var groupTypes = LogEventRegistry.Known
                .Where(d => d.Group == LogEventGroup.ItemsEconomy)
                .Select(d => d.Id)
                .ToList();
            var expected = new List<long>();
            foreach (int type in groupTypes)
            {
                expected.Add(fixture.InsertLog(TicksAfterStart(10 + type), type, 1, 0, 0, 0, 0, "item row"));
            }
            fixture.InsertLog(TicksAfterStart(9000), 0, 1, 0, 0, 0, 0, "chat");

            var rows = fixture.Query(
                LogQueryFixture.SearchQuery(StartTicks, EndTicks, types: groupTypes), SnapshotCeiling);

            Assert.Equal(expected.OrderByDescending(id => id).ToArray(), rows.Select(r => r.RowId).ToArray());
        }

        [Fact]
        public void Map_filter_matches_only_the_selected_map()
        {
            using var fixture = new LogQueryFixture();
            long inMap = fixture.InsertLog(TicksAfterStart(10), 0, 1, 0, 3, 5, 6, "in map");
            fixture.InsertLog(TicksAfterStart(20), 0, 1, 0, 4, 5, 6, "other map");
            fixture.InsertLog(TicksAfterStart(30), 0, 1, 0, 0, 5, 6, "no map");

            var rows = fixture.Query(LogQueryFixture.SearchQuery(StartTicks, EndTicks, map: 3), SnapshotCeiling);

            Assert.Equal(new long[] { inMap }, rows.Select(r => r.RowId).ToArray());
        }

        [Fact]
        public void Text_binding_is_wildcarded_and_escaped_in_order()
        {
            using var fixture = new LogQueryFixture();

            var command = fixture.BuildCommand(
                LogQueryFixture.SearchQuery(StartTicks, EndTicks, text: "a\\b%c_d"), SnapshotCeiling);

            var textParameter = command.Parameters.Single(p => p.ParameterName == "@text");
            Assert.Equal("%a\\\\b\\%c\\_d%", textParameter.Value);
            Assert.Contains("text LIKE @text ESCAPE '\\' COLLATE NOCASE", command.CommandText);
        }

        [Fact]
        public void Wildcards_and_backslash_match_literally_and_text_is_case_insensitive()
        {
            using var fixture = new LogQueryFixture();
            long percent = fixture.InsertLog(TicksAfterStart(10), 0, 1, 0, 0, 0, 0, "100% sure");
            long underscore = fixture.InsertLog(TicksAfterStart(20), 0, 1, 0, 0, 0, 0, "a_b");
            long backslash = fixture.InsertLog(TicksAfterStart(30), 0, 1, 0, 0, 0, 0, "back\\slash");
            long upper = fixture.InsertLog(TicksAfterStart(40), 0, 1, 0, 0, 0, 0, "A_B");
            long plain = fixture.InsertLog(TicksAfterStart(50), 0, 1, 0, 0, 0, 0, "plain text");

            Assert.Equal(new long[] { percent },
                fixture.Query(LogQueryFixture.SearchQuery(StartTicks, EndTicks, text: "100%"), SnapshotCeiling)
                    .Select(r => r.RowId).ToArray());
            Assert.Equal(new long[] { upper, underscore },
                fixture.Query(LogQueryFixture.SearchQuery(StartTicks, EndTicks, text: "_"), SnapshotCeiling)
                    .Select(r => r.RowId).ToArray());
            Assert.Equal(new long[] { backslash },
                fixture.Query(LogQueryFixture.SearchQuery(StartTicks, EndTicks, text: "\\"), SnapshotCeiling)
                    .Select(r => r.RowId).ToArray());
            Assert.Equal(new long[] { plain },
                fixture.Query(LogQueryFixture.SearchQuery(StartTicks, EndTicks, text: "PLAIN"), SnapshotCeiling)
                    .Select(r => r.RowId).ToArray());
        }

        [Fact]
        public void Combined_time_type_map_and_text_filters_intersect()
        {
            using var fixture = new LogQueryFixture();
            long match = fixture.InsertLog(TicksAfterStart(10), 11, 1, 0, 3, 0, 0, "sword in hand");
            fixture.InsertLog(StartTicks - 1, 11, 1, 0, 3, 0, 0, "sword in hand");
            fixture.InsertLog(TicksAfterStart(30), 12, 1, 0, 3, 0, 0, "sword in hand");
            fixture.InsertLog(TicksAfterStart(40), 11, 1, 0, 4, 0, 0, "sword in hand");
            fixture.InsertLog(TicksAfterStart(50), 12, 1, 0, 4, 0, 0, "sword in hand");

            var rows = fixture.Query(
                LogQueryFixture.SearchQuery(StartTicks, EndTicks, map: 3, types: new[] { 11 }, text: "sword in hand"),
                SnapshotCeiling);

            Assert.Equal(new long[] { match }, rows.Select(r => r.RowId).ToArray());
        }

        [Fact]
        public void Primary_participant_branch_matches_every_playerid_row()
        {
            using var fixture = new LogQueryFixture();
            long chat = fixture.InsertLog(TicksAfterStart(10), 0, 7, 0, 0, 0, 0, "hello");
            long item = fixture.InsertLog(TicksAfterStart(20), 11, 7, 0, 0, 0, 0, "12 1 Iron 2");
            long unknown = fixture.InsertLog(TicksAfterStart(30), 999, 7, 0, 0, 0, 0, "mystery");
            fixture.InsertLog(TicksAfterStart(40), 0, 8, 0, 0, 0, 0, "other player");

            var rows = fixture.Query(LogQueryFixture.SearchQuery(StartTicks, EndTicks, participant: 7), SnapshotCeiling);

            Assert.Equal(new long[] { unknown, item, chat }, rows.Select(r => r.RowId).ToArray());
        }

        [Fact]
        public void Secondary_participant_branch_matches_only_player_valued_types()
        {
            using var fixture = new LogQueryFixture();
            long tell = fixture.InsertLog(TicksAfterStart(10), 13, 1, 7, 0, 0, 0, "hi");
            long ban = fixture.InsertLog(TicksAfterStart(20), 10010, 7, 7, 0, 0, 0, "");
            long joinGuild = fixture.InsertLog(TicksAfterStart(30), 5, 2, 7, 0, 0, 0, "5");

            var rows = fixture.Query(LogQueryFixture.SearchQuery(StartTicks, EndTicks, participant: 7), SnapshotCeiling);

            Assert.Equal(new long[] { joinGuild, ban, tell }, rows.Select(r => r.RowId).ToArray());
        }

        [Fact]
        public void Item_guild_npc_map_and_unknown_numeric_collisions_do_not_match()
        {
            using var fixture = new LogQueryFixture();
            fixture.InsertLog(TicksAfterStart(10), 17, 1, 7, 0, 0, 0, "Custom Sword (12) 34|255,0,0,255");
            fixture.InsertLog(TicksAfterStart(20), 7, 1, 7, 0, 0, 0, "guild message");
            fixture.InsertLog(TicksAfterStart(30), 18, 1, 7, 0, 0, 0, "Health Potion (12) x3 (900 gold)");
            fixture.InsertLog(TicksAfterStart(40), 10005, 1, 7, 7, 0, 0, "");
            fixture.InsertLog(TicksAfterStart(50), 999, 1, 7, 0, 0, 0, "mystery");
            fixture.InsertLog(TicksAfterStart(55), 14, 1, 7, 0, 0, 0, "retired credits");
            fixture.InsertLog(TicksAfterStart(60), 0, 1, 0, 7, 0, 0, "chat on map");
            long control = fixture.InsertLog(TicksAfterStart(70), 0, 7, 0, 0, 0, 0, "own chat");

            var rows = fixture.Query(LogQueryFixture.SearchQuery(StartTicks, EndTicks, participant: 7), SnapshotCeiling);

            Assert.Equal(new long[] { control }, rows.Select(r => r.RowId).ToArray());
        }

        [Fact]
        public void A_row_matching_both_roles_appears_exactly_once()
        {
            using var fixture = new LogQueryFixture();
            long selfTell = fixture.InsertLog(TicksAfterStart(10), 13, 7, 7, 0, 0, 0, "to myself");

            var rows = fixture.Query(LogQueryFixture.SearchQuery(StartTicks, EndTicks, participant: 7), SnapshotCeiling);

            Assert.Equal(new long[] { selfTell }, rows.Select(r => r.RowId).ToArray());
        }

        [Fact]
        public void Time_map_and_text_predicates_apply_to_the_secondary_branch()
        {
            using var fixture = new LogQueryFixture();
            fixture.InsertLog(StartTicks - 1, 13, 1, 7, 0, 0, 0, "match");
            fixture.InsertLog(TicksAfterStart(20), 13, 1, 7, 4, 0, 0, "match");
            fixture.InsertLog(TicksAfterStart(30), 13, 1, 7, 3, 0, 0, "unrelated");
            long match = fixture.InsertLog(TicksAfterStart(40), 13, 1, 7, 3, 0, 0, "match");

            var rows = fixture.Query(
                LogQueryFixture.SearchQuery(StartTicks, EndTicks, participant: 7, map: 3, text: "match"),
                SnapshotCeiling);

            Assert.Equal(new long[] { match }, rows.Select(r => r.RowId).ToArray());
        }

        [Fact]
        public void Snapshot_ceiling_excludes_rows_in_both_branches()
        {
            using var fixture = new LogQueryFixture();
            long inside = fixture.InsertLog(TicksAfterStart(10), 0, 7, 0, 0, 0, 0, "inside");
            long outsidePrimary = fixture.InsertLog(TicksAfterStart(20), 0, 7, 0, 0, 0, 0, "outside primary");
            long outsideSecondary = fixture.InsertLog(TicksAfterStart(30), 13, 1, 7, 0, 0, 0, "outside secondary");

            var rows = fixture.Query(
                LogQueryFixture.SearchQuery(StartTicks, EndTicks, participant: 7), inside);

            Assert.Equal(new long[] { inside }, rows.Select(r => r.RowId).ToArray());
            Assert.True(outsidePrimary > inside);
            Assert.True(outsideSecondary > inside);
        }

        [Fact]
        public void Continuation_boundary_excludes_rows_at_or_after_the_boundary()
        {
            using var fixture = new LogQueryFixture();
            long older = fixture.InsertLog(TicksAfterStart(10), 0, 1, 0, 0, 0, 0, "older");
            long atBoundaryLower = fixture.InsertLog(TicksAfterStart(200), 0, 1, 0, 0, 0, 0, "at boundary a");
            long atBoundaryHigher = fixture.InsertLog(TicksAfterStart(200), 0, 1, 0, 0, 0, 0, "at boundary b");
            fixture.InsertLog(TicksAfterStart(300), 0, 1, 0, 0, 0, 0, "newer");

            var cursor = new LogPageCursor(SnapshotCeiling, TicksAfterStart(200), atBoundaryHigher);
            var rows = fixture.Query(
                LogQueryFixture.SearchQuery(StartTicks, EndTicks, cursor: cursor), SnapshotCeiling);

            Assert.Equal(new long[] { atBoundaryLower, older }, rows.Select(r => r.RowId).ToArray());
        }

        [Fact]
        public void Empty_type_selection_includes_unknown_and_out_of_int32_types()
        {
            using var fixture = new LogQueryFixture();
            long chat = fixture.InsertLog(TicksAfterStart(10), 0, 1, 0, 0, 0, 0, "chat");
            long unknown = fixture.InsertLog(TicksAfterStart(20), 999, 1, 0, 0, 0, 0, "mystery");
            long outOfInt32 = fixture.InsertLog(TicksAfterStart(30), 5_000_000_000L, 1, 0, 0, 0, 0, "future");

            var all = fixture.Query(LogQueryFixture.SearchQuery(StartTicks, EndTicks), SnapshotCeiling);
            Assert.Equal(new long[] { outOfInt32, unknown, chat }, all.Select(r => r.RowId).ToArray());

            var knownOnly = fixture.Query(
                LogQueryFixture.SearchQuery(StartTicks, EndTicks, types: new[] { 0 }), SnapshotCeiling);
            Assert.Equal(new long[] { chat }, knownOnly.Select(r => r.RowId).ToArray());
        }

        [Fact]
        public void Full_signed_64_bit_values_do_not_throw_or_truncate()
        {
            using var fixture = new LogQueryFixture();
            long rowId = fixture.InsertLog(TicksAfterStart(10), long.MaxValue, long.MinValue, long.MaxValue,
                70_000, -70_000, long.MaxValue, "extremes");

            var rows = fixture.Query(LogQueryFixture.SearchQuery(StartTicks, EndTicks), SnapshotCeiling);

            Assert.Single(rows);
            LogQueryRow row = rows[0];
            Assert.Equal(rowId, row.RowId);
            Assert.True(row.TypeIsInteger);
            Assert.Equal(long.MaxValue, row.Type);
            Assert.True(row.PlayerIdIsInteger);
            Assert.Equal(long.MinValue, row.PlayerId);
            Assert.True(row.OtherIdIsInteger);
            Assert.Equal(long.MaxValue, row.OtherId);
            Assert.True(row.MapIdIsInteger);
            Assert.Equal(70_000, row.MapId);
            Assert.True(row.MapXIsInteger);
            Assert.Equal(-70_000, row.MapX);
            Assert.True(row.MapYIsInteger);
            Assert.Equal(long.MaxValue, row.MapY);
        }

        [Fact]
        public void Malformed_numeric_cells_are_unusable_and_never_resolved_or_quick_filtered()
        {
            using var fixture = new LogQueryFixture();
            fixture.InsertPlayer(1, "Alice");
            long tell = fixture.InsertRawLog(TicksAfterStart(10), 13, "abc", 7.5, new byte[] { 1 }, null, 20L, "hi");
            long badType = fixture.InsertRawLog(TicksAfterStart(20), "13x", 1L, 2L, 0L, null, null, "mystery");

            var rows = fixture.Query(LogQueryFixture.SearchQuery(StartTicks, EndTicks), SnapshotCeiling);
            Assert.Equal(new long[] { badType, tell }, rows.Select(r => r.RowId).ToArray());

            LogQueryRow tellRow = rows.Single(r => r.RowId == tell);
            Assert.Equal("Tell", tellRow.TypeLabel);
            Assert.False(tellRow.PlayerIdIsInteger);
            Assert.Equal(0, tellRow.PlayerId);
            Assert.False(tellRow.OtherIdIsInteger);
            Assert.Equal(0, tellRow.OtherId);
            Assert.False(tellRow.MapIdIsInteger);
            Assert.Equal(0, tellRow.MapId);
            Assert.False(tellRow.MapXIsInteger);
            Assert.Equal(0, tellRow.MapX);
            Assert.True(tellRow.MapYIsInteger);
            Assert.Equal(20, tellRow.MapY);
            Assert.Null(tellRow.Primary);
            Assert.Null(tellRow.Related);
            Assert.Null(tellRow.Map);

            LogQueryRow badTypeRow = rows.Single(r => r.RowId == badType);
            Assert.False(badTypeRow.TypeIsInteger);
            Assert.Equal(0, badTypeRow.Type);
            Assert.Equal("Unknown event", badTypeRow.TypeLabel);
            Assert.False(badTypeRow.Related!.CanQuickFilter);
        }

        [Fact]
        public void Sql_injection_strings_remain_literal_parameters()
        {
            using var fixture = new LogQueryFixture();
            long injection = fixture.InsertLog(TicksAfterStart(10), 0, 1, 0, 0, 0, 0, "Robert'); DROP TABLE logs;--");
            long tautology = fixture.InsertLog(TicksAfterStart(20), 0, 1, 0, 0, 0, 0, "1=1;--");
            fixture.InsertLog(TicksAfterStart(30), 0, 1, 0, 0, 0, 0, "harmless");

            var rows = fixture.Query(
                LogQueryFixture.SearchQuery(StartTicks, EndTicks, text: "Robert'); DROP TABLE logs;--"),
                SnapshotCeiling);
            Assert.Equal(new long[] { injection }, rows.Select(r => r.RowId).ToArray());

            var tautologyRows = fixture.Query(
                LogQueryFixture.SearchQuery(StartTicks, EndTicks, text: "1=1"), SnapshotCeiling);
            Assert.Equal(new long[] { tautology }, tautologyRows.Select(r => r.RowId).ToArray());

            Assert.Equal(3, fixture.CountLogs());
        }

        private static int CountOccurrences(string haystack, string needle)
        {
            int count = 0;
            int index = 0;
            while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += needle.Length;
            }
            return count;
        }
    }
}
