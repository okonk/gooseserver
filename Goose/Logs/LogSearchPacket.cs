using System.Globalization;

namespace Goose.Logs
{
    public enum LogSearchAction
    {
        Fresh,
        Page,
    }

    public sealed class LogSearchRequest
    {
        public int WindowId { get; }
        public int RequestId { get; }
        public LogSearchAction Action { get; }
        public LogFreshSearchInput? Fresh { get; }
        public string? PageToken { get; }

        private LogSearchRequest(int windowId, int requestId, LogSearchAction action,
            LogFreshSearchInput? fresh, string? pageToken)
        {
            if (fresh is null == pageToken is null)
                throw new ArgumentException("Exactly one of fresh input or page token must be present.");
            WindowId = windowId;
            RequestId = requestId;
            Action = action;
            Fresh = fresh;
            PageToken = pageToken;
        }

        internal static LogSearchRequest ForFresh(int windowId, int requestId, LogFreshSearchInput input)
            => new(windowId, requestId, LogSearchAction.Fresh, input, null);

        internal static LogSearchRequest ForPage(int windowId, int requestId, string pageToken)
            => new(windowId, requestId, LogSearchAction.Page, null, pageToken);
    }

    public static class LogSearchPacket
    {
        public const int MaxPacketLength = 8_192;
        public const int MaxParticipantUtf8Bytes = 64;
        public const int MaxTextUtf8Bytes = 4_096;

        private const string Prefix = "LQS";

        public static bool TryParse(string packet, out LogSearchRequest? request)
        {
            request = null;
            if (packet is null || packet.Length > MaxPacketLength || !packet.StartsWith(Prefix))
                return false;

            string[] fields = packet.Split(',');
            if (fields.Length is not (4 or 9))
                return false;

            if (!TryParsePositiveInt32(fields[0][Prefix.Length..], out int windowId)
                || !TryParsePositiveInt32(fields[1], out int requestId))
                return false;

            if (fields.Length == 9)
            {
                if (fields[2] != "F")
                    return false;
                if (!TryParseSignedInt64(fields[3], out long startMs)
                    || !TryParseSignedInt64(fields[4], out long endMs)
                    || !ProtocolTextCodec.TryDecodeText(fields[5], MaxParticipantUtf8Bytes, out string participant)
                    || !int.TryParse(fields[6], NumberStyles.None, CultureInfo.InvariantCulture, out int mapId)
                    || mapId < 0
                    || !TryParseTypeIds(fields[7], out int[] typeIds)
                    || !ProtocolTextCodec.TryDecodeText(fields[8], MaxTextUtf8Bytes, out string text))
                    return false;

                request = LogSearchRequest.ForFresh(windowId, requestId, new LogFreshSearchInput
                {
                    StartUtcMilliseconds = startMs,
                    EndUtcMilliseconds = endMs,
                    Participant = participant,
                    MapId = mapId,
                    EventTypeIds = typeIds,
                    Text = text,
                });
                return true;
            }

            if (fields[2] != "P" || !LogPageTokenCodec.IsCanonical(fields[3]))
                return false;
            request = LogSearchRequest.ForPage(windowId, requestId, fields[3]);
            return true;
        }

        public static bool TryIdentify(string packet, out int windowId, out int requestId)
        {
            windowId = 0;
            requestId = 0;
            if (packet is null || !packet.StartsWith(Prefix))
                return false;

            int firstComma = packet.IndexOf(',');
            if (firstComma < 0)
                return false;
            int secondComma = packet.IndexOf(',', firstComma + 1);
            if (secondComma < 0)
                return false;

            return TryParsePositiveInt32(packet[(Prefix.Length)..firstComma], out windowId)
                && TryParsePositiveInt32(packet[(firstComma + 1)..secondComma], out requestId);
        }

        private static bool TryParseSignedInt64(string field, out long value)
        {
            value = 0;
            if (field.StartsWith('+'))
                return false;
            return long.TryParse(field, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryParsePositiveInt32(string field, out int value)
        {
            return int.TryParse(field, NumberStyles.None, CultureInfo.InvariantCulture, out value) && value > 0;
        }

        private static bool TryParseTypeIds(string field, out int[] typeIds)
        {
            typeIds = Array.Empty<int>();
            if (field.Length == 0)
                return true;

            string[] parts = field.Split('|');
            if (parts.Length > LogEventRegistry.Known.Count)
                return false;

            var ids = new int[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                if (!int.TryParse(parts[i], NumberStyles.None, CultureInfo.InvariantCulture, out int id) || id < 0)
                    return false;
                ids[i] = id;
            }

            typeIds = ids;
            return true;
        }
    }
}
