using System.Globalization;

namespace Goose.Logs
{
    public static class LogProtocolPackets
    {
        public const int MaxSegmentLength = 12_288;
        public const int MaxResponseBytes = 4_194_304;
        public const char PacketDelimiter = '\x01';

        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        public sealed class SearchResponse
        {
            public IReadOnlyList<string> Packets { get; }
            public int TotalBytes { get; }

            internal SearchResponse(IReadOnlyList<string> packets, int totalBytes)
            {
                Packets = packets;
                TotalBytes = totalBytes;
            }
        }

        public static string BuildTypeMetadata(int windowId, int knownTypeId, string group, string label)
            => string.Format(Invariant, "LMT{0},{1},{2},{3}", windowId, knownTypeId,
                ProtocolTextCodec.EncodeText(group), ProtocolTextCodec.EncodeText(label));

        public static string BuildMapMetadata(int windowId, int knownMapId, string mapName)
            => string.Format(Invariant, "LMM{0},{1},{2}", windowId, knownMapId, ProtocolTextCodec.EncodeText(mapName));

        public static string BuildDefaultRange(int windowId, long startUnixMs, long endUnixMs)
            => string.Format(Invariant, "LMD{0},{1},{2}", windowId, startUnixMs, endUnixMs);

        public static string BuildSearchBegin(int windowId, int requestId)
            => string.Format(Invariant, "LRB{0},{1}", windowId, requestId);

        public static string BuildSearchError(int windowId, int requestId, string message)
            => string.Format(Invariant, "LRX{0},{1},{2}", windowId, requestId, ProtocolTextCodec.EncodeText(message));

        public static string BuildSearchFinish(int windowId, int requestId, bool hasMore,
            string currentPageToken, string nextPageToken)
        {
            if (!LogPageTokenCodec.IsCanonical(currentPageToken))
                throw new ArgumentException("Current page token must be canonical.", nameof(currentPageToken));
            if (hasMore)
            {
                if (!LogPageTokenCodec.IsCanonical(nextPageToken))
                    throw new ArgumentException("Next page token must be canonical when hasMore.", nameof(nextPageToken));
            }
            else if (nextPageToken.Length != 0)
            {
                throw new ArgumentException("Next page token must be empty when not hasMore.", nameof(nextPageToken));
            }

            return string.Format(Invariant, "LRF{0},{1},{2},{3},{4}", windowId, requestId,
                hasMore ? "1" : "0", currentPageToken, nextPageToken);
        }

        public static IReadOnlyList<string> BuildRowChunks(int windowId, int requestId, int rowOrdinal, byte[] rowJsonUtf8)
        {
            string base64 = Convert.ToBase64String(rowJsonUtf8);
            int chunkCount = Math.Max(1, (base64.Length + MaxSegmentLength - 1) / MaxSegmentLength);
            var packets = new string[chunkCount];
            for (int chunkIndex = 0; chunkIndex < chunkCount; chunkIndex++)
            {
                int start = chunkIndex * MaxSegmentLength;
                int length = Math.Min(MaxSegmentLength, base64.Length - start);
                packets[chunkIndex] = string.Format(Invariant, "LRD{0},{1},{2},{3},{4},{5}",
                    windowId, requestId, rowOrdinal, chunkIndex, chunkCount, base64.Substring(start, length));
            }
            return packets;
        }

        public static bool TryBuildSearchResponse(int windowId, int requestId, IReadOnlyList<byte[]> rowJsonUtf8s,
            bool hasMore, string currentPageToken, string nextPageToken, out SearchResponse? response)
        {
            response = null;
            if (windowId <= 0 || requestId <= 0 || rowJsonUtf8s is null
                || !LogPageTokenCodec.IsCanonical(currentPageToken))
                return false;
            if (hasMore)
            {
                if (!LogPageTokenCodec.IsCanonical(nextPageToken))
                    return false;
            }
            else if (nextPageToken.Length != 0)
            {
                return false;
            }

            var packets = new List<string>();
            int total = 0;
            void Add(string packet)
            {
                packets.Add(packet);
                total += packet.Length + 1;
            }

            Add(BuildSearchBegin(windowId, requestId));
            for (int ordinal = 0; ordinal < rowJsonUtf8s.Count; ordinal++)
            {
                byte[] row = rowJsonUtf8s[ordinal];
                if (row.Length == 0 || row.Length > LogRowJsonSerializer.MaxSerializedBytes)
                    return false;
                foreach (string chunk in BuildRowChunks(windowId, requestId, ordinal, row))
                    Add(chunk);
            }
            Add(BuildSearchFinish(windowId, requestId, hasMore, currentPageToken, nextPageToken));

            if (total > MaxResponseBytes)
                return false;

            response = new SearchResponse(packets.ToArray(), total);
            return true;
        }
    }
}
