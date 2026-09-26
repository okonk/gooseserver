using System.Reflection;
using Goose.Logs;
using Xunit;

namespace Goose.IntegrationTests
{
    public class LogQueryPagingTests
    {
        private static readonly long StartTicks = DateTime.UnixEpoch.Ticks + 10_000_000_000L;
        private static readonly long EndTicks = StartTicks + TimeSpan.TicksPerDay;

        private static long TicksAfter(long seconds) => StartTicks + seconds * TimeSpan.TicksPerSecond;

        private static LogSearchPage Execute(LogQueryFixture fixture, LogSearchQuery query)
            => LogQueryEngine.Execute(fixture.Connection, query);

        private static LogSearchQuery Query(LogPageCursor? cursor = null)
            => LogQueryFixture.SearchQuery(StartTicks, EndTicks, cursor: cursor);

        private static void AssertSqlContains(string sql, string fragment)
            => Assert.True(sql.Contains(fragment, StringComparison.OrdinalIgnoreCase));

        private static void AssertSqlExcludes(string sql, string fragment)
            => Assert.False(sql.Contains(fragment, StringComparison.OrdinalIgnoreCase));

        [Fact]
        public void Zero_rows_return_empty_page_without_next_cursor()
        {
            using var fixture = new LogQueryFixture();

            var page = Execute(fixture, Query());

            Assert.Empty(page.Rows);
            Assert.False(page.HasMore);
            Assert.Null(page.NextCursor);
        }

        [Fact]
        public void Exactly_fifty_rows_return_fifty_with_no_next_cursor()
        {
            using var fixture = new LogQueryFixture();
            for (int i = 1; i <= 50; i++)
                fixture.InsertLog(TicksAfter(i), 0, 1, 0, 0, 0, 0, "row " + i);

            var page = Execute(fixture, Query());

            Assert.Equal(50, page.Rows.Count);
            Assert.False(page.HasMore);
            Assert.Null(page.NextCursor);
        }

        [Fact]
        public void Fifty_one_rows_return_fifty_and_next_cursor_uses_fiftieth_row_not_lookahead()
        {
            using var fixture = new LogQueryFixture();
            var rowIds = new long[51];
            for (int i = 1; i <= 51; i++)
                rowIds[i - 1] = fixture.InsertLog(TicksAfter(i), 0, 1, 0, 0, 0, 0, "row " + i);

            var page1 = Execute(fixture, Query());

            Assert.Equal(50, page1.Rows.Count);
            Assert.True(page1.HasMore);
            Assert.NotNull(page1.NextCursor);
            Assert.Equal(51, page1.NextCursor.SnapshotCeiling);
            Assert.Equal(TicksAfter(2), page1.NextCursor.BeforeUtcTicks);
            Assert.Equal(rowIds[1], page1.NextCursor.BeforeRowId);

            var page2 = Execute(fixture, Query(page1.NextCursor));

            Assert.Equal(new[] { rowIds[0] }, page2.Rows.Select(r => r.RowId).ToArray());
            Assert.False(page2.HasMore);
            Assert.Null(page2.NextCursor);
        }

        [Fact]
        public void One_hundred_one_rows_traverse_50_50_1_without_duplicates_or_omissions()
        {
            using var fixture = new LogQueryFixture();
            var allRowIds = new HashSet<long>();
            for (int i = 1; i <= 101; i++)
                allRowIds.Add(fixture.InsertLog(TicksAfter(i), 0, 1, 0, 0, 0, 0, "row " + i));

            var seen = new List<long>();
            var page = Execute(fixture, Query());
            int pages = 0;
            while (true)
            {
                pages++;
                seen.AddRange(page.Rows.Select(r => r.RowId));
                if (!page.HasMore)
                {
                    Assert.Equal(1, page.Rows.Count);
                    Assert.Null(page.NextCursor);
                    break;
                }
                Assert.Equal(50, page.Rows.Count);
                Assert.NotNull(page.NextCursor);
                page = Execute(fixture, Query(page.NextCursor));
            }

            Assert.Equal(3, pages);
            Assert.Equal(101, seen.Count);
            Assert.Equal(101, seen.Distinct().Count());
            Assert.Equal(allRowIds, seen.ToHashSet());
        }

        [Fact]
        public void Exact_ticks_differing_by_one_order_correctly()
        {
            using var fixture = new LogQueryFixture();
            long lower = fixture.InsertLog(TicksAfter(100), 0, 1, 0, 0, 0, 0, "lower");
            long higher = fixture.InsertLog(TicksAfter(100) + 1, 0, 1, 0, 0, 0, 0, "higher");

            var page = Execute(fixture, Query());

            Assert.Equal(new[] { higher, lower }, page.Rows.Select(r => r.RowId).ToArray());
        }

        [Fact]
        public void Identical_ticks_order_by_descending_rowid_across_page_boundary()
        {
            using var fixture = new LogQueryFixture();
            var rowIds = new long[51];
            for (int i = 1; i <= 51; i++)
                rowIds[i - 1] = fixture.InsertLog(TicksAfter(100), 0, 1, 0, 0, 0, 0, "row " + i);

            var page1 = Execute(fixture, Query());

            Assert.Equal(rowIds[1..].Reverse().ToArray(), page1.Rows.Select(r => r.RowId).ToArray());
            Assert.Equal(TicksAfter(100), page1.NextCursor!.BeforeUtcTicks);
            Assert.Equal(rowIds[1], page1.NextCursor.BeforeRowId);

            var page2 = Execute(fixture, Query(page1.NextCursor));

            Assert.Equal(new[] { rowIds[0] }, page2.Rows.Select(r => r.RowId).ToArray());
        }

        [Fact]
        public void Mixed_dates_order_correctly_across_month_and_year_boundaries()
        {
            using var fixture = new LogQueryFixture();
            long start = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;
            long end = new DateTime(2025, 7, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;
            long jan2024 = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc).Ticks;
            long dec2024 = new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc).Ticks;
            long jan2025 = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;
            long jun2025 = new DateTime(2025, 6, 15, 8, 30, 0, DateTimeKind.Utc).Ticks;

            long rJan2024 = fixture.InsertLog(jan2024, 0, 1, 0, 0, 0, 0, "jan 2024");
            long rDec2024 = fixture.InsertLog(dec2024, 0, 1, 0, 0, 0, 0, "dec 2024");
            long rJan2025 = fixture.InsertLog(jan2025, 0, 1, 0, 0, 0, 0, "jan 2025");
            long rJun2025 = fixture.InsertLog(jun2025, 0, 1, 0, 0, 0, 0, "jun 2025");

            var page = Execute(fixture, LogQueryFixture.SearchQuery(start, end));

            Assert.Equal(new[] { rJun2025, rJan2025, rDec2024, rJan2024 },
                page.Rows.Select(r => r.RowId).ToArray());
        }

        [Fact]
        public void Continuation_sql_uses_exact_tick_rowid_keyset_and_no_offset()
        {
            using var fixture = new LogQueryFixture();
            var query = Query();
            var cursor = new LogPageCursor(100, TicksAfter(50), 42);

            var continuation = fixture.BuildCommand(query.WithCursor(cursor));

            AssertSqlContains(continuation.CommandText,
                "(log_date < @beforeTicks OR (log_date = @beforeTicks AND rowid < @beforeRowId))");
            var beforeTicks = continuation.Parameters.Single(p => p.ParameterName == "@beforeTicks");
            var beforeRowId = continuation.Parameters.Single(p => p.ParameterName == "@beforeRowId");
            Assert.Equal(TicksAfter(50), beforeTicks.Value);
            Assert.Equal(42L, beforeRowId.Value);
            AssertSqlExcludes(continuation.CommandText, "OFFSET");

            var fresh = fixture.BuildCommand(query);

            AssertSqlExcludes(fresh.CommandText, "@beforeTicks");
            AssertSqlExcludes(fresh.CommandText, "OFFSET");
        }

        [Fact]
        public void Rows_inserted_after_page_one_are_excluded_from_continuation()
        {
            using var fixture = new LogQueryFixture();
            var rowIds = new long[51];
            for (int i = 1; i <= 51; i++)
                rowIds[i - 1] = fixture.InsertLog(TicksAfter(i), 0, 1, 0, 0, 0, 0, "row " + i);

            var page1 = Execute(fixture, Query());

            long newer = fixture.InsertLog(TicksAfter(100), 0, 1, 0, 0, 0, 0, "newer than all");
            long between = fixture.InsertLog(TicksAfter(25) + 1, 0, 1, 0, 0, 0, 0, "between existing rows");
            long older = fixture.InsertLog(StartTicks + 1, 0, 1, 0, 0, 0, 0, "older than all");

            var page2 = Execute(fixture, Query(page1.NextCursor));

            Assert.Equal(new[] { rowIds[0] }, page2.Rows.Select(r => r.RowId).ToArray());
            Assert.DoesNotContain(page2.Rows, r => r.RowId == newer || r.RowId == between || r.RowId == older);
        }

        [Fact]
        public void Fresh_query_after_insert_captures_new_snapshot_and_sees_new_rows()
        {
            using var fixture = new LogQueryFixture();
            long first = fixture.InsertLog(TicksAfter(10), 0, 1, 0, 0, 0, 0, "first");

            var page1 = Execute(fixture, Query());

            Assert.Equal(new[] { first }, page1.Rows.Select(r => r.RowId).ToArray());

            long second = fixture.InsertLog(TicksAfter(20), 0, 1, 0, 0, 0, 0, "second");

            var page2 = Execute(fixture, Query());

            Assert.Equal(new[] { second, first }, page2.Rows.Select(r => r.RowId).ToArray());
        }

        [Fact]
        public void Boundary_less_first_page_replay_is_stable_across_inserts()
        {
            using var fixture = new LogQueryFixture();
            var rowIds = new long[51];
            for (int i = 1; i <= 51; i++)
                rowIds[i - 1] = fixture.InsertLog(TicksAfter(i), 0, 1, 0, 0, 0, 0, "row " + i);

            var page1 = Execute(fixture, Query());
            long snapshot = page1.NextCursor!.SnapshotCeiling;
            var originalRowIds = page1.Rows.Select(r => r.RowId).ToArray();

            for (int i = 100; i < 110; i++)
                fixture.InsertLog(TicksAfter(i), 0, 1, 0, 0, 0, 0, "late " + i);

            var replay = Execute(fixture, Query(LogPageCursor.ForFirstPage(snapshot)));

            Assert.Equal(originalRowIds, replay.Rows.Select(r => r.RowId).ToArray());
            Assert.True(replay.HasMore);
            Assert.Equal(page1.NextCursor, replay.NextCursor);
        }

        [Fact]
        public void Row_updated_after_page_one_remains_in_insertion_snapshot()
        {
            using var fixture = new LogQueryFixture();
            var rowIds = new long[51];
            for (int i = 1; i <= 51; i++)
                rowIds[i - 1] = fixture.InsertLog(TicksAfter(i), 0, 1, 0, 0, 0, 0, "row " + i);

            var page1 = Execute(fixture, Query());
            long updatedRowId = rowIds[2];

            fixture.UpdateLogText(updatedRowId, "updated text");

            var replay = Execute(fixture, Query(LogPageCursor.ForFirstPage(page1.NextCursor!.SnapshotCeiling)));

            Assert.Equal(50, replay.Rows.Count);
            var updated = replay.Rows.Single(r => r.RowId == updatedRowId);
            Assert.Equal("updated text", updated.Text);
        }

        [Fact]
        public void Participant_union_paging_preserves_order_and_deduplication()
        {
            using var fixture = new LogQueryFixture();
            var rowIds = new long[51];
            for (int i = 1; i <= 51; i++)
            {
                int player = i % 3 == 1 ? 0 : 7;
                int other = i % 3 == 0 ? 0 : 7;
                long type = i % 3 == 0 ? 0 : 10010;
                rowIds[i - 1] = fixture.InsertLog(TicksAfter(i), type, player, other, 0, 0, 0, "row " + i);
            }

            var query = LogQueryFixture.SearchQuery(StartTicks, EndTicks, participant: 7);
            var seen = new List<long>();
            var page = Execute(fixture, query);
            while (true)
            {
                seen.AddRange(page.Rows.Select(r => r.RowId));
                if (!page.HasMore)
                {
                    Assert.Equal(1, page.Rows.Count);
                    break;
                }
                Assert.Equal(50, page.Rows.Count);
                page = Execute(fixture, query.WithCursor(page.NextCursor!));
            }

            Assert.Equal(51, seen.Count);
            Assert.Equal(51, seen.Distinct().Count());
            Assert.Equal(rowIds.Reverse().ToArray(), seen.ToArray());
        }

        [Fact]
        public void Participant_identity_is_stable_after_rename_or_duplicate_name()
        {
            using var fixture = new LogQueryFixture();
            fixture.InsertPlayer(1, "alpha");
            var rowIds = new long[51];
            for (int i = 1; i <= 51; i++)
                rowIds[i - 1] = fixture.InsertLog(TicksAfter(i), 0, 1, 0, 0, 0, 0, "row " + i);

            var input = new LogFreshSearchInput
            {
                StartUtcMilliseconds = (StartTicks - DateTime.UnixEpoch.Ticks) / TimeSpan.TicksPerMillisecond,
                EndUtcMilliseconds = (EndTicks - DateTime.UnixEpoch.Ticks) / TimeSpan.TicksPerMillisecond,
                Participant = "alpha",
            };
            var result = LogQueryValidator.ValidateFresh(fixture.Connection, input);
            Assert.True(result.IsSuccess);
            var query = result.Query!;

            var page1 = Execute(fixture, query);
            Assert.Equal(50, page1.Rows.Count);

            fixture.RenamePlayer(1, "beta");
            fixture.InsertPlayer(2, "alpha");
            long impostor = fixture.InsertLog(StartTicks + 1, 0, 2, 0, 0, 0, 0, "impostor");

            var page2 = Execute(fixture, query.WithCursor(page1.NextCursor!));

            Assert.Equal(new[] { rowIds[0] }, page2.Rows.Select(r => r.RowId).ToArray());
            Assert.DoesNotContain(page2.Rows, r => r.RowId == impostor);
        }

        [Fact]
        public void Pending_memory_rows_are_excluded_from_results()
        {
            using var fixture = new LogQueryFixture();
            long persisted = fixture.InsertLog(TicksAfter(100), 0, 1, 0, 0, 0, 0, "persisted");

            var handler = new LogHandler();
            handler.Log(Goose.Log.Types.Chat, 1, "pending only", 0, 0, 0, 0);
            Assert.Equal(1, handler.Pending.Count);

            var page = Execute(fixture, Query());

            Assert.Equal(new[] { persisted }, page.Rows.Select(r => r.RowId).ToArray());
        }

        [Fact]
        public void Generated_sql_uses_max_rowid_snapshot_and_never_count_or_offset()
        {
            using var fixture = new LogQueryFixture();

            AssertSqlContains(LogQueryEngine.SnapshotSql, "MAX(rowid)");
            AssertSqlExcludes(LogQueryEngine.SnapshotSql, "COUNT(*)");
            AssertSqlExcludes(LogQueryEngine.SnapshotSql, "OFFSET");

            var query = Query();
            var commands = new[]
            {
                fixture.BuildCommand(query),
                fixture.BuildCommand(query.WithCursor(new LogPageCursor(100, TicksAfter(50), 42))),
            };
            foreach (var command in commands)
            {
                AssertSqlExcludes(command.CommandText, "COUNT(*)");
                AssertSqlExcludes(command.CommandText, "OFFSET");
            }
        }

        [Theory]
        [MemberData(nameof(MalformedCursors))]
        public void Malformed_cursors_are_rejected_before_page_sql(object cursor)
        {
            using var fixture = new LogQueryFixture();
            fixture.Connection.Close();

            Assert.Throws<ArgumentException>(() => Execute(fixture, Query().WithCursor((LogPageCursor)cursor)));
        }

        [Fact]
        public void Well_formed_cursor_reaches_page_sql_on_closed_connection()
        {
            using var fixture = new LogQueryFixture();
            fixture.Connection.Close();

            var exception = Assert.Throws<InvalidOperationException>(
                () => Execute(fixture, Query().WithCursor(new LogPageCursor(10, TicksAfter(100), 5))));
            Assert.Contains("not open", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        public static IEnumerable<object[]> MalformedCursors()
        {
            yield return new object[] { new LogPageCursor(-1, null, null) };
            yield return new object[] { new LogPageCursor(10, TicksAfter(100), null) };
            yield return new object[] { new LogPageCursor(10, null, 5) };
            yield return new object[] { new LogPageCursor(10, -1, 5) };
            yield return new object[] { new LogPageCursor(10, DateTime.MaxValue.Ticks + 1, 5) };
            yield return new object[] { new LogPageCursor(10, StartTicks - 1, 5) };
            yield return new object[] { new LogPageCursor(10, EndTicks, 5) };
            yield return new object[] { new LogPageCursor(10, TicksAfter(100), 0) };
            yield return new object[] { new LogPageCursor(10, TicksAfter(100), -5) };
            yield return new object[] { new LogPageCursor(10, TicksAfter(100), 11) };
        }

        [Fact]
        public void Cursorless_calls_capture_fresh_ceiling_and_boundary_less_cursors_reuse_stored_ceiling()
        {
            using var fixture = new LogQueryFixture();
            for (int i = 1; i <= 51; i++)
                fixture.InsertLog(TicksAfter(i), 0, 1, 0, 0, 0, 0, "row " + i);

            var page1 = Execute(fixture, Query());

            Assert.Equal(51, page1.NextCursor!.SnapshotCeiling);

            for (int i = 100; i < 105; i++)
                fixture.InsertLog(TicksAfter(i), 0, 1, 0, 0, 0, 0, "late " + i);

            var replay = Execute(fixture, Query(LogPageCursor.ForFirstPage(51)));

            Assert.Equal(50, replay.Rows.Count);
            Assert.DoesNotContain(replay.Rows, r => r.RowId > 51);
            Assert.Equal(51, replay.NextCursor!.SnapshotCeiling);
        }

        [Fact]
        public void Next_cursor_is_internal_object_with_exact_int64_fields_and_no_string_representation()
        {
            using var fixture = new LogQueryFixture();
            for (int i = 1; i <= 51; i++)
                fixture.InsertLog(TicksAfter(i), 0, 1, 0, 0, 0, 0, "row " + i);

            var page = Execute(fixture, Query());

            Assert.NotNull(page.NextCursor);
            Assert.IsType<LogPageCursor>(page.NextCursor);
            Assert.Equal(51, page.NextCursor!.SnapshotCeiling);
            Assert.Equal(page.Rows[49].UtcTicks, page.NextCursor.BeforeUtcTicks);
            Assert.Equal(page.Rows[49].RowId, page.NextCursor.BeforeRowId);

            var stringProperties = typeof(LogSearchPage)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.PropertyType == typeof(string))
                .ToList();
            Assert.Empty(stringProperties);
        }
    }
}
