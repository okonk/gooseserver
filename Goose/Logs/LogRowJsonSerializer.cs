using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Goose.Logs
{
    public static class LogRowJsonSerializer
    {
        public const int MaxSerializedBytes = 262_144;

        private static readonly JsonWriterOptions WriterOptions = new()
        {
            Indented = false,
            SkipValidation = false,
            Encoder = JavaScriptEncoder.Default,
        };

        public static bool TrySerialize(LogQueryRow row, out byte[] utf8Json)
        {
            if (row.UtcTicks < 0 || row.UtcTicks > DateTime.MaxValue.Ticks)
            {
                utf8Json = Array.Empty<byte>();
                return false;
            }

            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream, WriterOptions))
            {
                writer.WriteStartObject();
                writer.WriteNumber("rowId", row.RowId);
                writer.WriteNumber("utcMilliseconds",
                    new DateTimeOffset(row.UtcTicks, TimeSpan.Zero).ToUnixTimeMilliseconds());
                writer.WriteNumber("typeId", row.Type);
                writer.WriteBoolean("typeIsInteger", row.TypeIsInteger);
                writer.WriteString("eventLabel", row.TypeLabel);
                writer.WriteString("eventGroup", row.GroupLabel);
                writer.WriteString("otherIdKind", row.OtherIdKind.ToString());
                WriteEntity(writer, "primary", row.Primary);
                WriteEntity(writer, "related", row.Related);
                WriteMap(writer, row.Map);
                WriteRaw(writer, row);
                writer.WriteString("summary", row.Summary);
                writer.WriteString("originalText", row.Text);
                writer.WriteEndObject();
            }

            byte[] bytes = stream.ToArray();
            if (bytes.Length > MaxSerializedBytes)
            {
                utf8Json = Array.Empty<byte>();
                return false;
            }

            utf8Json = bytes;
            return true;
        }

        private static void WriteEntity(Utf8JsonWriter writer, string name, LogRelatedEntity? entity)
        {
            writer.WritePropertyName(name);
            if (entity is null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            writer.WriteString("label", entity.Label);
            writer.WriteString("kind", entity.Kind.ToString());
            if (entity.Id is long id)
            {
                writer.WriteNumber("id", id);
            }
            else
            {
                writer.WritePropertyName("id");
                writer.WriteNullValue();
            }
            if (entity.Name is string entityName)
            {
                writer.WriteString("name", entityName);
            }
            else
            {
                writer.WritePropertyName("name");
                writer.WriteNullValue();
            }
            writer.WriteBoolean("canQuickFilter", entity.CanQuickFilter);
            writer.WriteEndObject();
        }

        private static void WriteMap(Utf8JsonWriter writer, LogRelatedEntity? map)
        {
            writer.WritePropertyName("map");
            if (map is null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            if (map.Id is long id)
            {
                writer.WriteNumber("id", id);
            }
            else
            {
                writer.WritePropertyName("id");
                writer.WriteNullValue();
            }
            if (map.Name is string name)
            {
                writer.WriteString("name", name);
            }
            else
            {
                writer.WritePropertyName("name");
                writer.WriteNullValue();
            }
            writer.WriteBoolean("canQuickFilter", map.CanQuickFilter);
            writer.WriteEndObject();
        }

        private static void WriteRaw(Utf8JsonWriter writer, LogQueryRow row)
        {
            writer.WritePropertyName("raw");
            writer.WriteStartObject();
            writer.WriteNumber("playerId", row.PlayerId);
            writer.WriteBoolean("playerIdIsInteger", row.PlayerIdIsInteger);
            writer.WriteNumber("otherId", row.OtherId);
            writer.WriteBoolean("otherIdIsInteger", row.OtherIdIsInteger);
            writer.WriteNumber("mapId", row.MapId);
            writer.WriteBoolean("mapIdIsInteger", row.MapIdIsInteger);
            writer.WriteNumber("mapX", row.MapX);
            writer.WriteBoolean("mapXIsInteger", row.MapXIsInteger);
            writer.WriteNumber("mapY", row.MapY);
            writer.WriteBoolean("mapYIsInteger", row.MapYIsInteger);
            writer.WriteEndObject();
        }
    }
}
