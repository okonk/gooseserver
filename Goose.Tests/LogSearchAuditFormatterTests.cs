using Goose.Logs;
using Xunit;

namespace Goose.Tests
{
    public class LogSearchAuditFormatterTests
    {
        [Fact]
        public void Format_EmptyFilters_ProducesExactCompactJsonWithAllGroup()
        {
            string json = LogSearchAuditFormatter.Format(new LogFreshSearchInput
            {
                StartUtcMilliseconds = -5,
                EndUtcMilliseconds = 1_700_000_000_000,
                Participant = null,
                MapId = 0,
                EventTypeIds = Array.Empty<int>(),
                Text = null,
            });

            Assert.Equal(
                "{\"startUtcMilliseconds\":-5,\"endUtcMilliseconds\":1700000000000," +
                "\"participant\":\"\",\"mapId\":0,\"eventIds\":[],\"groups\":[\"All\"],\"text\":\"\"}",
                json);
        }

        [Fact]
        public void Format_SortsAndDeduplicatesTypeIdsAndUsesRegistryOrderGroups()
        {
            string json = LogSearchAuditFormatter.Format(new LogFreshSearchInput
            {
                StartUtcMilliseconds = 1,
                EndUtcMilliseconds = 2,
                Participant = "Bob",
                MapId = 7,
                EventTypeIds = new[] { 14, 0, 14, 1 },
                Text = "hi",
            });

            Assert.Equal(
                "{\"startUtcMilliseconds\":1,\"endUtcMilliseconds\":2,\"participant\":\"Bob\"," +
                "\"mapId\":7,\"eventIds\":[0,1,14],\"groups\":[\"Communication\",\"Other/Retired\"],\"text\":\"hi\"}",
                json);
        }

        [Fact]
        public void Format_GroupLabelsFollowRegistryOrderNotInputOrder()
        {
            string json = LogSearchAuditFormatter.Format(new LogFreshSearchInput
            {
                StartUtcMilliseconds = 0,
                EndUtcMilliseconds = 10,
                Participant = null,
                MapId = 0,
                EventTypeIds = new[] { 10001, 0, 5, 10013, 8, 11 },
                Text = null,
            });

            Assert.Equal(
                "{\"startUtcMilliseconds\":0,\"endUtcMilliseconds\":10,\"participant\":\"\"," +
                "\"mapId\":0,\"eventIds\":[0,5,8,11,10001,10013]," +
                "\"groups\":[\"Communication\",\"Social\",\"Items/Economy\",\"GM Actions\"],\"text\":\"\"}",
                json);
        }

        [Fact]
        public void Format_AllUnknownTypeIdsFallBackToAll()
        {
            string json = LogSearchAuditFormatter.Format(new LogFreshSearchInput
            {
                StartUtcMilliseconds = 0,
                EndUtcMilliseconds = 10,
                Participant = null,
                MapId = 0,
                EventTypeIds = new[] { 99999, 20000 },
                Text = null,
            });

            Assert.Equal(
                "{\"startUtcMilliseconds\":0,\"endUtcMilliseconds\":10,\"participant\":\"\"," +
                "\"mapId\":0,\"eventIds\":[20000,99999],\"groups\":[\"All\"],\"text\":\"\"}",
                json);
        }

        [Fact]
        public void Format_EscapesQuotesBackslashesControlsAndPassesThroughUnicode()
        {
            string json = LogSearchAuditFormatter.Format(new LogFreshSearchInput
            {
                StartUtcMilliseconds = 0,
                EndUtcMilliseconds = 9,
                Participant = "a\"b\\c",
                MapId = 0,
                EventTypeIds = Array.Empty<int>(),
                Text = "l1\nl2\tend\u0001é😀",
            });

            Assert.Equal(
                "{\"startUtcMilliseconds\":0,\"endUtcMilliseconds\":9,\"participant\":\"a\\\"b\\\\c\"," +
                "\"mapId\":0,\"eventIds\":[],\"groups\":[\"All\"],\"text\":\"l1\\nl2\\tend\\u0001é😀\"}",
                json);
        }

        [Fact]
        public void Format_EscapesRemainingControlCharactersWithLowercaseHex()
        {
            string json = LogSearchAuditFormatter.Format(new LogFreshSearchInput
            {
                StartUtcMilliseconds = 0,
                EndUtcMilliseconds = 9,
                Participant = "\u0001",
                MapId = 0,
                EventTypeIds = Array.Empty<int>(),
                Text = "\u001f",
            });

            Assert.Equal(
                "{\"startUtcMilliseconds\":0,\"endUtcMilliseconds\":9,\"participant\":\"\\u0001\"," +
                "\"mapId\":0,\"eventIds\":[],\"groups\":[\"All\"],\"text\":\"\\u001f\"}",
                json);
        }
    }
}
