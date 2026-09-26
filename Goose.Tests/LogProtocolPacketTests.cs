using Goose;
using Goose.Logs;
using Xunit;

namespace Goose.Tests
{
    public class LogProtocolPacketTests
    {
        private static string Token() => LogPageTokenCodec.Create();

        [Fact]
        public void Builds_exact_capability_and_control_shapes()
        {
            Assert.Equal(
                "LMT5,7," + ProtocolTextCodec.EncodeText("GmActions") + "," + ProtocolTextCodec.EncodeText("RespawnMap"),
                LogProtocolPackets.BuildTypeMetadata(5, 7, "GmActions", "RespawnMap"));
            Assert.Equal(
                "LMM5,3," + ProtocolTextCodec.EncodeText("Town Square"),
                LogProtocolPackets.BuildMapMetadata(5, 3, "Town Square"));
            Assert.Equal("LMD2,-1000,1700000000000", LogProtocolPackets.BuildDefaultRange(2, -1000, 1_700_000_000_000));
            Assert.Equal("LRB2,9", LogProtocolPackets.BuildSearchBegin(2, 9));
            Assert.Equal("LRX1,2," + ProtocolTextCodec.EncodeText("safe message,|pipe"),
                LogProtocolPackets.BuildSearchError(1, 2, "safe message,|pipe"));
        }

        [Fact]
        public void Numerics_are_invariant_int64_and_metadata_ids_are_int32()
        {
            Assert.Equal("LMD1,-9223372036854775808,9223372036854775807",
                LogProtocolPackets.BuildDefaultRange(1, long.MinValue, long.MaxValue));
            Assert.Equal(
                "LMT1,2147483647," + ProtocolTextCodec.EncodeText("g") + "," + ProtocolTextCodec.EncodeText("l"),
                LogProtocolPackets.BuildTypeMetadata(1, int.MaxValue, "g", "l"));
            Assert.Equal(
                "LMM1,2147483647," + ProtocolTextCodec.EncodeText("m"),
                LogProtocolPackets.BuildMapMetadata(1, int.MaxValue, "m"));
        }

        [Fact]
        public void Lrf_requires_current_token_and_next_token_only_when_has_more()
        {
            string current = Token();
            string next = Token();
            Assert.Equal($"LRF1,2,0,{current},", LogProtocolPackets.BuildSearchFinish(1, 2, false, current, ""));
            Assert.Equal($"LRF1,2,1,{current},{next}", LogProtocolPackets.BuildSearchFinish(1, 2, true, current, next));

            Assert.Throws<ArgumentException>(() => LogProtocolPackets.BuildSearchFinish(1, 2, false, "", ""));
            Assert.Throws<ArgumentException>(() => LogProtocolPackets.BuildSearchFinish(1, 2, true, current, ""));
            Assert.Throws<ArgumentException>(() => LogProtocolPackets.BuildSearchFinish(1, 2, false, current, next));
            Assert.Throws<ArgumentException>(() => LogProtocolPackets.BuildSearchFinish(1, 2, false, "short", ""));
        }

        [Fact]
        public void Row_chunks_are_zero_based_bounded_and_reassemble_before_single_decode()
        {
            byte[] json = new byte[100_000];
            Array.Fill(json, (byte)'x');
            string base64 = Convert.ToBase64String(json);
            int chunkCount = (base64.Length + LogProtocolPackets.MaxSegmentLength - 1) / LogProtocolPackets.MaxSegmentLength;
            Assert.True(chunkCount > 1);

            IReadOnlyList<string> packets = LogProtocolPackets.BuildRowChunks(3, 7, 2, json);
            Assert.Equal(chunkCount, packets.Count);
            for (int i = 0; i < chunkCount; i++)
            {
                int start = i * LogProtocolPackets.MaxSegmentLength;
                int length = Math.Min(LogProtocolPackets.MaxSegmentLength, base64.Length - start);
                Assert.Equal($"LRD3,7,2,{i},{chunkCount}," + base64[start..(start + length)], packets[i]);
                Assert.True(length <= LogProtocolPackets.MaxSegmentLength);
            }

            string concatenated = string.Join(string.Empty,
                packets.Select(packet => packet[(packet.LastIndexOf(',') + 1)..]));
            Assert.Equal(base64, concatenated);
            Assert.Equal(json, Convert.FromBase64String(concatenated));
        }

        [Fact]
        public void Row_chunk_segments_never_exceed_maximum_length()
        {
            byte[] json = new byte[LogRowJsonSerializer.MaxSerializedBytes];
            IReadOnlyList<string> packets = LogProtocolPackets.BuildRowChunks(1, 1, 0, json);
            Assert.True(packets.Count > 1);
            foreach (string packet in packets)
            {
                string segment = packet[(packet.LastIndexOf(',') + 1)..];
                Assert.True(segment.Length <= LogProtocolPackets.MaxSegmentLength);
            }
        }

        [Fact]
        public void BuildResponse_prebuilds_immutable_packet_list_with_exact_byte_accounting()
        {
            byte[] row1 = new byte[100_000];
            byte[] row2 = new byte[10];
            string current = Token();
            string next = Token();

            Assert.True(LogProtocolPackets.TryBuildSearchResponse(
                1, 2, new[] { row1, row2 }, true, current, next, out LogProtocolPackets.SearchResponse? response));

            int expectedChunks1 = (Convert.ToBase64String(row1).Length + LogProtocolPackets.MaxSegmentLength - 1) / LogProtocolPackets.MaxSegmentLength;
            int expectedChunks2 = (Convert.ToBase64String(row2).Length + LogProtocolPackets.MaxSegmentLength - 1) / LogProtocolPackets.MaxSegmentLength;
            Assert.Equal(2 + expectedChunks1 + expectedChunks2, response!.Packets.Count);
            Assert.Equal(response!.Packets.Sum(packet => packet.Length + 1), response!.TotalBytes);
            Assert.True(response!.TotalBytes <= LogProtocolPackets.MaxResponseBytes);

            Assert.Equal("LRB1,2", response!.Packets[0]);
            Assert.StartsWith("LRD1,2,0,0,", response!.Packets[1]);
            Assert.StartsWith($"LRD1,2,0,{expectedChunks1 - 1},{expectedChunks1},", response!.Packets[1 + expectedChunks1 - 1]);
            Assert.StartsWith($"LRD1,2,1,0,{expectedChunks2},", response!.Packets[1 + expectedChunks1]);
            Assert.Equal($"LRF1,2,1,{current},{next}", response!.Packets[^1]);
        }

        [Fact]
        public void BuildResponse_final_page_omits_next_token()
        {
            byte[] row = new byte[10];
            string current = Token();
            Assert.True(LogProtocolPackets.TryBuildSearchResponse(
                1, 2, new[] { row }, false, current, "", out LogProtocolPackets.SearchResponse? response));
            Assert.Equal($"LRF1,2,0,{current},", response!.Packets[^1]);
        }

        [Fact]
        public void BuildResponse_fails_safely_on_contract_violations()
        {
            byte[] row = new byte[10];
            string current = Token();
            string next = Token();

            Assert.False(LogProtocolPackets.TryBuildSearchResponse(0, 2, new[] { row }, false, current, "", out _));
            Assert.False(LogProtocolPackets.TryBuildSearchResponse(1, 0, new[] { row }, false, current, "", out _));
            Assert.False(LogProtocolPackets.TryBuildSearchResponse(1, 2, new[] { row }, false, "", "", out _));
            Assert.False(LogProtocolPackets.TryBuildSearchResponse(1, 2, new[] { row }, false, "short", "", out _));
            Assert.False(LogProtocolPackets.TryBuildSearchResponse(1, 2, new[] { row }, false, current, next, out _));
            Assert.False(LogProtocolPackets.TryBuildSearchResponse(1, 2, new[] { row }, true, current, "", out _));
            Assert.False(LogProtocolPackets.TryBuildSearchResponse(1, 2, new[] { Array.Empty<byte>() }, false, current, "", out _));
            Assert.False(LogProtocolPackets.TryBuildSearchResponse(
                1, 2, new[] { new byte[LogRowJsonSerializer.MaxSerializedBytes + 1] }, false, current, "", out LogProtocolPackets.SearchResponse? response));
            Assert.Null(response);
        }

        [Fact]
        public void Response_budget_is_exactly_4_mib_including_delimiters()
        {
            // Only row-size set that makes the accounted total (delimiters included) exactly 4 MiB.
            int[] sizes =
            {
                239_614, 239_614, 239_614, 239_614, 239_614, 239_614, 239_614,
                239_614, 239_614, 239_614, 239_614, 247_801, 258_049,
            };
            byte[][] rows = sizes.Select(size => new byte[size]).ToArray();
            string current = Token();

            Assert.True(LogProtocolPackets.TryBuildSearchResponse(
                1, 2, rows, false, current, "", out LogProtocolPackets.SearchResponse? response));
            Assert.Equal(LogProtocolPackets.MaxResponseBytes, response!.TotalBytes);
            Assert.Equal(LogProtocolPackets.MaxResponseBytes, response.Packets.Sum(packet => packet.Length + 1));

            rows[^1] = new byte[sizes[^1] + 3];
            Assert.False(LogProtocolPackets.TryBuildSearchResponse(1, 2, rows, false, current, "", out _));
        }
    }
}
