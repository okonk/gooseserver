using System.Text;
using System.Text.Json;
using Goose;
using Goose.Logs;
using Xunit;

namespace Goose.Tests
{
    public class LogRowJsonSerializerTests
    {
        private static LogQueryRow Row(
            long rowId = 1, long utcTicks = 638_000_000_000_000_000, long type = 0, bool typeIsInteger = true,
            long playerId = 0, bool playerIdIsInteger = true, long otherId = 0, bool otherIdIsInteger = true,
            long mapId = 0, bool mapIdIsInteger = true, long mapX = 0, bool mapXIsInteger = true,
            long mapY = 0, bool mapYIsInteger = true, string typeLabel = "Chat",
            string groupLabel = "Communication", LogOtherIdKind otherIdKind = LogOtherIdKind.Unused,
            LogRelatedEntity? primary = null, LogRelatedEntity? related = null,
            LogRelatedEntity? map = null, string summary = "", string text = "")
            => new(rowId, utcTicks, type, typeIsInteger,
                playerId, playerIdIsInteger, otherId, otherIdIsInteger,
                mapId, mapIdIsInteger, mapX, mapXIsInteger, mapY, mapYIsInteger,
                typeLabel, groupLabel, otherIdKind, primary, related, map, summary, text);

        private static byte[] Serialize(LogQueryRow row)
        {
            Assert.True(LogRowJsonSerializer.TrySerialize(row, out byte[] json));
            return json;
        }

        [Fact]
        public void Serializes_exact_property_order_names_and_explicit_nulls()
        {
            byte[] json = Serialize(Row(rowId: 7, utcTicks: DateTimeOffset.FromUnixTimeMilliseconds(1_700_000_000_123).UtcTicks,
                type: 14, typeLabel: "Received Credits (Retired)", groupLabel: "OtherRetired",
                summary: "s", text: "t"));

            string expected =
                "{\"rowId\":7,\"utcMilliseconds\":1700000000123,\"typeId\":14,\"typeIsInteger\":true," +
                "\"eventLabel\":\"Received Credits (Retired)\",\"eventGroup\":\"OtherRetired\"," +
                "\"otherIdKind\":\"Unused\",\"primary\":null,\"related\":null,\"map\":null," +
                "\"raw\":{\"playerId\":0,\"playerIdIsInteger\":true,\"otherId\":0,\"otherIdIsInteger\":true," +
                "\"mapId\":0,\"mapIdIsInteger\":true,\"mapX\":0,\"mapXIsInteger\":true,\"mapY\":0,\"mapYIsInteger\":true}," +
                "\"summary\":\"s\",\"originalText\":\"t\"}";

            Assert.Equal((byte)'{', json[0]);
            Assert.Equal(expected, Encoding.UTF8.GetString(json));
        }

        [Fact]
        public void Serializes_populated_entities_in_exact_shape()
        {
            byte[] json = Serialize(Row(
                primary: new LogRelatedEntity("Player", LogEntityKind.Player, 42, "Alpha", true),
                related: new LogRelatedEntity("Item", LogEntityKind.Item, 7, "Sword", false),
                map: new LogRelatedEntity("Map", LogEntityKind.Map, 3, "Town", true)));

            string expected =
                "\"primary\":{\"label\":\"Player\",\"kind\":\"Player\",\"id\":42,\"name\":\"Alpha\",\"canQuickFilter\":true}," +
                "\"related\":{\"label\":\"Item\",\"kind\":\"Item\",\"id\":7,\"name\":\"Sword\",\"canQuickFilter\":false}," +
                "\"map\":{\"id\":3,\"name\":\"Town\",\"canQuickFilter\":true}";

            string actual = Encoding.UTF8.GetString(json);
            int start = actual.IndexOf("\"primary\":", StringComparison.Ordinal);
            int end = actual.IndexOf("\"raw\":", StringComparison.Ordinal);
            Assert.Equal(expected, actual[start..(end - 1)]);
        }

        [Fact]
        public void Null_entity_id_and_name_are_explicit_json_nulls()
        {
            byte[] json = Serialize(Row(
                related: new LogRelatedEntity("Gold", LogEntityKind.Gold, null, null, false)));
            string actual = Encoding.UTF8.GetString(json);
            Assert.Contains("\"related\":{\"label\":\"Gold\",\"kind\":\"Gold\",\"id\":null,\"name\":null,\"canQuickFilter\":false}", actual);
        }

        [Fact]
        public void Every_raw_numeric_round_trips_at_int64_extremes()
        {
            byte[] json = Serialize(Row(
                rowId: long.MaxValue, type: long.MinValue,
                playerId: long.MaxValue, playerIdIsInteger: false,
                otherId: long.MinValue, otherIdIsInteger: false,
                mapId: long.MaxValue, mapIdIsInteger: false,
                mapX: long.MinValue, mapXIsInteger: false,
                mapY: long.MaxValue, mapYIsInteger: false));

            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;
            Assert.Equal(long.MaxValue, root.GetProperty("rowId").GetInt64());
            Assert.Equal(long.MinValue, root.GetProperty("typeId").GetInt64());
            JsonElement raw = root.GetProperty("raw");
            Assert.Equal(long.MaxValue, raw.GetProperty("playerId").GetInt64());
            Assert.False(raw.GetProperty("playerIdIsInteger").GetBoolean());
            Assert.Equal(long.MinValue, raw.GetProperty("otherId").GetInt64());
            Assert.False(raw.GetProperty("otherIdIsInteger").GetBoolean());
            Assert.Equal(long.MaxValue, raw.GetProperty("mapId").GetInt64());
            Assert.False(raw.GetProperty("mapIdIsInteger").GetBoolean());
            Assert.Equal(long.MinValue, raw.GetProperty("mapX").GetInt64());
            Assert.False(raw.GetProperty("mapXIsInteger").GetBoolean());
            Assert.Equal(long.MaxValue, raw.GetProperty("mapY").GetInt64());
            Assert.False(raw.GetProperty("mapYIsInteger").GetBoolean());
        }

        [Fact]
        public void Unknown_type_above_int32_retains_raw_value_label_group_and_text()
        {
            byte[] json = Serialize(Row(
                type: 5_000_000_000L, typeIsInteger: true,
                typeLabel: "Unknown event", groupLabel: "OtherRetired",
                related: new LogRelatedEntity("Item", LogEntityKind.StoredValue, 99, "Sword", false),
                text: "the full original text"));

            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;
            Assert.Equal(5_000_000_000L, root.GetProperty("typeId").GetInt64());
            Assert.True(root.GetProperty("typeIsInteger").GetBoolean());
            Assert.Equal("Unknown event", root.GetProperty("eventLabel").GetString());
            Assert.Equal("OtherRetired", root.GetProperty("eventGroup").GetString());
            Assert.Equal("StoredValue", root.GetProperty("related").GetProperty("kind").GetString());
            Assert.Equal("the full original text", root.GetProperty("originalText").GetString());
        }

        [Theory]
        [InlineData(11, LogOtherIdKind.Unused, LogEntityKind.Item)]
        [InlineData(10001, LogOtherIdKind.Unused, LogEntityKind.Item)]
        [InlineData(10005, LogOtherIdKind.Unused, LogEntityKind.Map)]
        [InlineData(18, LogOtherIdKind.NpcTemplate, LogEntityKind.NpcTemplate)]
        [InlineData(99_999_999, LogOtherIdKind.Unused, LogEntityKind.Player)]
        public void OtherIdKind_remains_distinct_from_independently_projected_related(
            int typeId, LogOtherIdKind kind, LogEntityKind relatedKind)
        {
            byte[] json = Serialize(Row(
                type: typeId, typeLabel: "X", groupLabel: "G", otherIdKind: kind,
                related: new LogRelatedEntity("E", relatedKind, 5, "n", true)));

            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;
            Assert.Equal(kind.ToString(), root.GetProperty("otherIdKind").GetString());
            Assert.Equal(relatedKind.ToString(), root.GetProperty("related").GetProperty("kind").GetString());
            Assert.Equal(5L, root.GetProperty("related").GetProperty("id").GetInt64());
        }

        [Fact]
        public void Utc_ticks_map_exactly_to_unix_milliseconds()
        {
            long baseTicks = new DateTimeOffset(2023, 1, 15, 10, 30, 0, TimeSpan.Zero).UtcTicks;
            byte[] truncated = Serialize(Row(utcTicks: baseTicks + TimeSpan.FromMilliseconds(123).Ticks + 45_670));
            using (JsonDocument doc = JsonDocument.Parse(truncated))
                Assert.Equal(1_673_778_600_127L, doc.RootElement.GetProperty("utcMilliseconds").GetInt64());

            long preEpochTicks = new DateTimeOffset(1950, 1, 1, 0, 0, 0, TimeSpan.Zero).UtcTicks;
            byte[] preEpoch = Serialize(Row(utcTicks: preEpochTicks));
            using (JsonDocument doc = JsonDocument.Parse(preEpoch))
                Assert.Equal(-631_152_000_000L, doc.RootElement.GetProperty("utcMilliseconds").GetInt64());

            byte[] epoch = Serialize(Row(utcTicks: DateTimeOffset.UnixEpoch.UtcTicks));
            using (JsonDocument doc = JsonDocument.Parse(epoch))
                Assert.Equal(0L, doc.RootElement.GetProperty("utcMilliseconds").GetInt64());
        }

        [Fact]
        public void Hostile_original_text_survives_wire_round_trip_byte_for_byte()
        {
            string text = "ünïcödé, comma | pipe \u0001 NUL \u0000 end " + new string('x', 100_000);
            byte[] json = Serialize(Row(text: text));

            IReadOnlyList<string> chunks = LogProtocolPackets.BuildRowChunks(1, 1, 0, json);
            string concatenated = string.Join(string.Empty,
                chunks.Select(packet => packet[(packet.LastIndexOf(',') + 1)..]));
            byte[] decoded = Convert.FromBase64String(concatenated);

            Assert.Equal(json, decoded);
            using JsonDocument doc = JsonDocument.Parse(decoded);
            Assert.Equal(text, doc.RootElement.GetProperty("originalText").GetString());
        }

        [Fact]
        public void Accepts_exactly_256_kib_and_rejects_one_byte_over_without_truncation()
        {
            int overhead = Serialize(Row()).Length;
            string filler = new string('a', LogRowJsonSerializer.MaxSerializedBytes - overhead);

            byte[] exact = Serialize(Row(text: filler));
            Assert.Equal(LogRowJsonSerializer.MaxSerializedBytes, exact.Length);

            Assert.False(LogRowJsonSerializer.TrySerialize(Row(text: filler + "a"), out byte[] oversize));
            Assert.Empty(oversize);
        }
    }
}
