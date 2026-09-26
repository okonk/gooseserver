using System.Text.RegularExpressions;
using Goose.Logs;
using Xunit;

namespace Goose.IntegrationTests
{
    public class LogQueryPlanTests
    {
        private static readonly long StartTicks = DateTime.UnixEpoch.Ticks + 10_004_000_000L;
        private static readonly long EndTicks = StartTicks + TimeSpan.TicksPerSecond * 100;
        private const long SnapshotCeiling = 1_000_000_000L;

        [Fact]
        public void Production_commands_search_logs_through_declared_indexes()
        {
            using var fixture = new LogQueryFixture();
            fixture.SeedPlanData();

            var timeCommand = fixture.BuildCommand(LogQueryFixture.SearchQuery(StartTicks, EndTicks));
            AssertIndexUsage(fixture, timeCommand, new[] { "logs_log_date_idx" });

            var participantCommand = fixture.BuildCommand(
                LogQueryFixture.SearchQuery(StartTicks, EndTicks, participant: 5));
            AssertIndexUsage(fixture, participantCommand,
                new[] { "logs_playerid_log_date_idx", "logs_log_date_idx", "logs_otherid_log_date_idx" });
            var participantLines = LogLines(fixture, participantCommand);
            Assert.Equal(2, participantLines.Count);
            AssertIndexIn(participantLines[0], new[] { "logs_playerid_log_date_idx", "logs_log_date_idx" });
            AssertIndexIn(participantLines[1], new[] { "logs_otherid_log_date_idx", "logs_log_date_idx" });

            var typeCommand = fixture.BuildCommand(
                LogQueryFixture.SearchQuery(StartTicks, EndTicks, types: new[] { 11 }));
            AssertIndexUsage(fixture, typeCommand,
                new[] { "logs_log_type_log_date_idx", "logs_log_date_idx" });

            var mapCommand = fixture.BuildCommand(LogQueryFixture.SearchQuery(StartTicks, EndTicks, map: 3));
            AssertIndexUsage(fixture, mapCommand, new[] { "logs_mapid_log_date_idx", "logs_log_date_idx" });

            var textCommand = fixture.BuildCommand(
                LogQueryFixture.SearchQuery(StartTicks, EndTicks, text: "event"));
            AssertIndexUsage(fixture, textCommand, new[] { "logs_log_date_idx" });

            var boundaryCommand = fixture.BuildCommand(LogQueryFixture.SearchQuery(StartTicks, EndTicks,
                cursor: new LogPageCursor(SnapshotCeiling, StartTicks + TimeSpan.TicksPerSecond * 50, 5_000)));
            AssertIndexUsage(fixture, boundaryCommand, new[] { "logs_log_date_idx" });
        }

        [Fact]
        public void Production_sql_never_pins_an_index()
        {
            using var fixture = new LogQueryFixture();
            var commands = new[]
            {
                fixture.BuildCommand(LogQueryFixture.SearchQuery(StartTicks, EndTicks)),
                fixture.BuildCommand(LogQueryFixture.SearchQuery(StartTicks, EndTicks, participant: 5)),
                fixture.BuildCommand(LogQueryFixture.SearchQuery(StartTicks, EndTicks, types: new[] { 11 })),
                fixture.BuildCommand(LogQueryFixture.SearchQuery(StartTicks, EndTicks, map: 3)),
                fixture.BuildCommand(LogQueryFixture.SearchQuery(StartTicks, EndTicks, text: "event")),
            };

            foreach (var command in commands)
            {
                Assert.DoesNotContain("INDEXED BY", command.CommandText, StringComparison.OrdinalIgnoreCase);
            }
        }

        private static void AssertIndexUsage(LogQueryFixture fixture, LogQueryCommand command, string[] allowed)
        {
            Assert.DoesNotContain("INDEXED BY", command.CommandText, StringComparison.OrdinalIgnoreCase);
            var lines = LogLines(fixture, command);
            Assert.NotEmpty(lines);
            foreach (string line in lines)
            {
                AssertIndexIn(line, allowed);
            }
        }

        private static void AssertIndexIn(string line, string[] allowed)
        {
            Match match = IndexUsagePattern().Match(line);
            Assert.True(match.Success, "logs branch does not search through an index: " + line);
            Assert.Contains(match.Groups[1].Value, allowed, StringComparer.Ordinal);
        }

        private static IReadOnlyList<string> LogLines(LogQueryFixture fixture, LogQueryCommand command)
        {
            return fixture.ExplainQueryPlan(command)
                .Where(line => line.StartsWith("SCAN logs", StringComparison.Ordinal)
                    || line.StartsWith("SEARCH logs", StringComparison.Ordinal))
                .ToList();
        }

        private static Regex IndexUsagePattern() => new(
            @"^(?:SEARCH|SCAN) logs USING (?:COVERING )?INDEX (\S+)",
            RegexOptions.Compiled);
    }
}
