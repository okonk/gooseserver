using Goose;
using Goose.Logs;
using Xunit;

namespace Goose.Tests
{
    public class LogSearchPacketTests
    {
        private static string ValidFresh() =>
            "LQS7,42,F,1700000000000,1700000036000000," +
            ProtocolTextCodec.EncodeText("#123") + ",5,3|14|0," +
            ProtocolTextCodec.EncodeText("hello,|world");

        private static string ValidPage() =>
            "LQS7,42,P," + LogPageTokenCodec.Create();

        [Fact]
        public void Parses_valid_fresh_packet_into_identity_action_and_input()
        {
            Assert.True(LogSearchPacket.TryParse(ValidFresh(), out LogSearchRequest? request));
            Assert.Equal(7, request!.WindowId);
            Assert.Equal(42, request!.RequestId);
            Assert.Equal(LogSearchAction.Fresh, request!.Action);
            Assert.Null(request!.PageToken);
            LogFreshSearchInput input = request!.Fresh;
            Assert.Equal(1_700_000_000_000L, input.StartUtcMilliseconds);
            Assert.Equal(1_700_000_036_000_000L, input.EndUtcMilliseconds);
            Assert.Equal("#123", input.Participant);
            Assert.Equal(5, input.MapId);
            Assert.Equal(new[] { 3, 14, 0 }, input.EventTypeIds);
            Assert.Equal("hello,|world", input.Text);
        }

        [Fact]
        public void Parses_valid_page_packet_into_identity_action_and_opaque_token()
        {
            string packet = ValidPage();
            string token = packet[(packet.LastIndexOf(',') + 1)..];
            Assert.True(LogSearchPacket.TryParse(packet, out LogSearchRequest? request));
            Assert.Equal(7, request!.WindowId);
            Assert.Equal(42, request!.RequestId);
            Assert.Equal(LogSearchAction.Page, request!.Action);
            Assert.Null(request!.Fresh);
            Assert.Equal(token, request!.PageToken);
        }

        [Fact]
        public void Fresh_packet_never_exposes_token_or_cursor_and_page_never_exposes_input()
        {
            Assert.True(LogSearchPacket.TryParse(ValidFresh(), out LogSearchRequest? fresh));
            Assert.Null(fresh!.PageToken);
            Assert.NotNull(fresh!.Fresh);

            Assert.True(LogSearchPacket.TryParse(ValidPage(), out LogSearchRequest? page));
            Assert.Null(page!.Fresh);
            Assert.NotNull(page!.PageToken);
        }

        [Theory]
        [InlineData("LQS7,42,F,1700000000000,170000003600000,,5,3|14")]
        [InlineData("LQS7,42,F,1700000000000,170000003600000,,5,3|14,,extra")]
        [InlineData("LQS7,42,P")]
        [InlineData("LQS7,42,P,AAAAAAAAAAAAAAAAAAAAA,extra")]
        [InlineData("LQS7,42,f,1700000000000,170000003600000,,5,3|14,")]
        [InlineData("LQS7,42,p,AAAAAAAAAAAAAAAAAAAAAA")]
        [InlineData("LQS7,42,X,1700000000000,170000003600000,,5,3|14,")]
        [InlineData("LQS7,42,P,1700000000000,170000003600000,,5,3|14,")]
        [InlineData("LQS7,42,F,AAAAAAAAAAAAAAAAAAAAAA")]
        [InlineData("LQS0,42,F,1700000000000,170000003600000,,5,3|14,")]
        [InlineData("LQS-1,42,F,1700000000000,170000003600000,,5,3|14,")]
        [InlineData("LQS7,0,F,1700000000000,170000003600000,,5,3|14,")]
        [InlineData("LQS7,2147483648,F,1700000000000,170000003600000,,5,3|14,")]
        [InlineData("LQS2147483648,42,F,1700000000000,170000003600000,,5,3|14,")]
        [InlineData("LQS7,42,F,1.5,170000003600000,,5,3|14,")]
        [InlineData("LQS7,42,F,1700000000000,1e12,,5,3|14,")]
        [InlineData("LQS7,42,F, 1700000000000,170000003600000,,5,3|14,")]
        [InlineData("LQS7,42,F,+1700000000000,170000003600000,,5,3|14,")]
        [InlineData("LQS7,42,F,9223372036854775808,170000003600000,,5,3|14,")]
        [InlineData("LQS7,42,F,1700000000000,-9223372036854775809,,5,3|14,")]
        [InlineData("LQS7,42,F,1700000000000,170000003600000,-1,5,3|14,")]
        [InlineData("LQS7,42,F,1700000000000,170000003600000,,2147483648,3|14,")]
        [InlineData("LQS7,42,F,1700000000000,170000003600000,,5,3|,")]
        [InlineData("LQS7,42,F,1700000000000,170000003600000,,5,|3,")]
        [InlineData("LQS7,42,F,1700000000000,170000003600000,,5,3||4,")]
        [InlineData("LQS7,42,F,1700000000000,170000003600000,,5,a,")]
        [InlineData("LQS7,42,F,1700000000000,170000003600000,,5,-1,")]
        [InlineData("LQS7,42,F,1700000000000,170000003600000,,5,2147483648,")]
        [InlineData("LQS7,42,F,1700000000000,170000003600000,,5,hello,")]
        [InlineData("LQS7,42,F,1700000000000,170000003600000,,5,3|14,Zg")]
        [InlineData("LQS7,42,F,1700000000000,170000003600000,,5,3|14,ZgR=")]
        [InlineData("LQS7,42,F,1700000000000,170000003600000,,5,3|14,//8=")]
        [InlineData("LQS7,42,P,AAAAAAAAAAAAAAAAAAAAA")]
        [InlineData("LQS7,42,P,AAAAAAAAAAAAAAAAAAAA+")]
        [InlineData("LQS7,42,P,AAAAAAAAAAAAAAAAAAAAAN")]
        public void Rejects_malformed_packet(string packet)
        {
            Assert.False(LogSearchPacket.TryParse(packet, out LogSearchRequest? request));
            Assert.Null(request);
        }

        [Fact]
        public void Rejects_packet_longer_than_8192_characters()
        {
            string token = LogPageTokenCodec.Create();
            string atLimit = "LQS7,42,P," + token + new string('A', 8192 - 32);
            Assert.Equal(8192, atLimit.Length);
            Assert.False(LogSearchPacket.TryParse(atLimit, out _));

            string overLimit = atLimit + "A";
            Assert.Equal(8193, overLimit.Length);
            Assert.False(LogSearchPacket.TryParse(overLimit, out _));
        }

        [Fact]
        public void Accepts_signed_milliseconds_empty_filters_and_zero_map()
        {
            string packet = "LQS1,2,F,-5,5,,0,,";
            Assert.True(LogSearchPacket.TryParse(packet, out LogSearchRequest? request));
            LogFreshSearchInput input = request!.Fresh;
            Assert.Equal(-5L, input.StartUtcMilliseconds);
            Assert.Equal(5L, input.EndUtcMilliseconds);
            Assert.Equal(string.Empty, input.Participant);
            Assert.Equal(0, input.MapId);
            Assert.Empty(input.EventTypeIds);
            Assert.Equal(string.Empty, input.Text);
        }

        [Fact]
        public void Type_selection_accepts_known_count_and_rejects_one_over()
        {
            string max = string.Join("|", Enumerable.Range(0, LogEventRegistry.Known.Count));
            string atMax = "LQS1,2,F,0,1,,0," + max + ",";
            Assert.True(LogSearchPacket.TryParse(atMax, out LogSearchRequest? request));
            Assert.Equal(LogEventRegistry.Known.Count, request!.Fresh.EventTypeIds.Count);

            string over = string.Join("|", Enumerable.Range(0, LogEventRegistry.Known.Count + 1));
            Assert.False(LogSearchPacket.TryParse("LQS1,2,F,0,1,,0," + over + ",", out _));
        }

        [Fact]
        public void Participant_and_text_accept_exact_byte_limits_and_reject_one_over()
        {
            string at64 = new string('€', 21) + "a";
            string over64 = new string('€', 22);
            string at4096 = new string('€', 1365) + "a";
            string over4096 = new string('€', 1365) + "ab";

            Assert.True(LogSearchPacket.TryParse(
                "LQS1,2,F,0,1," + ProtocolTextCodec.EncodeText(at64) + ",0,," + ProtocolTextCodec.EncodeText(at4096),
                out LogSearchRequest? request));
            Assert.Equal(at64, request!.Fresh.Participant);
            Assert.Equal(at4096, request!.Fresh.Text);

            Assert.False(LogSearchPacket.TryParse(
                "LQS1,2,F,0,1," + ProtocolTextCodec.EncodeText(over64) + ",0,,", out _));
            Assert.False(LogSearchPacket.TryParse(
                "LQS1,2,F,0,1,,0,," + ProtocolTextCodec.EncodeText(over4096), out _));
        }

        [Fact]
        public void TryIdentify_recovers_bounded_positive_ids_for_malformed_packets()
        {
            Assert.True(LogSearchPacket.TryIdentify("LQS7,42,P,not-a-token", out int window, out int request));
            Assert.Equal(7, window);
            Assert.Equal(42, request);

            Assert.True(LogSearchPacket.TryIdentify("LQS7,42,F,garbage", out window, out request));
            Assert.Equal(7, window);
            Assert.Equal(42, request);

            Assert.False(LogSearchPacket.TryIdentify("LQS0,42,P,x", out _, out _));
            Assert.False(LogSearchPacket.TryIdentify("LQS-1,42,P,x", out _, out _));
            Assert.False(LogSearchPacket.TryIdentify("LQS2147483648,42,P,x", out _, out _));
            Assert.False(LogSearchPacket.TryIdentify("LQS7,0,P,x", out _, out _));
            Assert.False(LogSearchPacket.TryIdentify("LQS7,2147483648,P,x", out _, out _));
            Assert.False(LogSearchPacket.TryIdentify("LQS7,P,x", out _, out _));
            Assert.False(LogSearchPacket.TryIdentify("LQS7", out _, out _));
            Assert.False(LogSearchPacket.TryIdentify("XQS7,42,P,x", out _, out _));
            Assert.False(LogSearchPacket.TryIdentify("", out _, out _));
        }
    }
}
