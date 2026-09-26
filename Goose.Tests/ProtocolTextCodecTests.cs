using System.Text;
using Goose;
using Xunit;

namespace Goose.Tests
{
    public class ProtocolTextCodecTests
    {
        [Theory]
        [InlineData("")]
        [InlineData("plain ascii 0123456789")]
        [InlineData("comma,pipe|slash/dot.dot:colon;semicolon")]
        [InlineData("control\u0001 \u001f tab\t newline\n cr\r bell\u0007")]
        [InlineData("café ☕ 🎮 日本語 한국어 العربية")]
        [InlineData("combining é\u0301 zero-width a\u200Bb bidi \u05D0\u05D1\u202E")]
        [InlineData("quotes\"backslash\\braces{}brackets[]")]
        public void Round_trips_text(string text)
        {
            string encoded = ProtocolTextCodec.EncodeText(text);
            Assert.True(ProtocolTextCodec.TryDecodeText(encoded, int.MaxValue, out string decoded));
            Assert.Equal(text, decoded);
        }

        [Theory]
        [InlineData("", "")]
        [InlineData("f", "Zg==")]
        [InlineData("fo", "Zm8=")]
        [InlineData("foo", "Zm9v")]
        [InlineData("foob", "Zm9vYg==")]
        [InlineData("fooba", "Zm9vYmE=")]
        [InlineData("foobar", "Zm9vYmFy")]
        [InlineData("???", "Pz8/")]
        [InlineData("????", "Pz8/Pw==")]
        public void Encodes_canonical_padded_vectors(string text, string expected)
        {
            Assert.Equal(expected, ProtocolTextCodec.EncodeText(text));
            Assert.True(ProtocolTextCodec.TryDecodeText(expected, int.MaxValue, out string decoded));
            Assert.Equal(text, decoded);
        }

        [Theory]
        [InlineData("Zg")]
        [InlineData("Zm9vYg")]
        [InlineData("Zg===")]
        [InlineData("Zg==A")]
        [InlineData("Zg =")]
        [InlineData("Zm 9v")]
        [InlineData("Zg Q")]
        [InlineData("Z!9v")]
        [InlineData("Zm9_Yg==")]
        [InlineData("Zg-A")]
        [InlineData("ZgR=")]
        [InlineData("Zh==")]
        [InlineData("ZgEg==")]
        [InlineData("//8=")]
        [InlineData("aGVsbG8=")]
        public void Rejects_malformed_base64(string base64)
        {
            if (base64 == "aGVsbG8=")
            {
                Assert.True(ProtocolTextCodec.TryDecodeText(base64, 5, out _));
                Assert.False(ProtocolTextCodec.TryDecodeText(base64, 4, out _));
                return;
            }

            Assert.False(ProtocolTextCodec.TryDecodeText(base64, int.MaxValue, out _));
        }

        [Fact]
        public void Accepts_valid_padding_bits_and_empty_input()
        {
            Assert.True(ProtocolTextCodec.TryDecodeText("ZgQ=", int.MaxValue, out string one));
            Assert.Equal("f\u0004", one);
            Assert.True(ProtocolTextCodec.TryDecodeText("Zg==", int.MaxValue, out string two));
            Assert.Equal("f", two);
            Assert.True(ProtocolTextCodec.TryDecodeText("", 0, out string empty));
            Assert.Equal(string.Empty, empty);
            Assert.False(ProtocolTextCodec.TryDecodeText("Zg==", 0, out _));
        }

        [Fact]
        public void Byte_limit_applies_to_utf8_bytes_not_characters()
        {
            string text = new string('€', 21) + "a";
            byte[] utf8 = Encoding.UTF8.GetBytes(text);
            Assert.Equal(64, utf8.Length);
            string encoded = ProtocolTextCodec.EncodeText(text);
            Assert.True(ProtocolTextCodec.TryDecodeText(encoded, 64, out string decoded));
            Assert.Equal(text, decoded);
            Assert.False(ProtocolTextCodec.TryDecodeText(encoded, 63, out _));
        }

        [Fact]
        public void Failure_is_non_throwing()
        {
            string[] malformed =
            {
                "Zg", "Zm9vYg", "Zg===", "Zg==A", "Zg =", "Zm 9v", "Zg Q", "Z!9v",
                "Zm9_Yg==", "Zg-A", "ZgR=", "ZgEg==", "//8=", "", " ",
            };
            foreach (string input in malformed)
            {
                Assert.False(ProtocolTextCodec.TryDecodeText(input, int.MaxValue, out string text));
                Assert.Equal(string.Empty, text);
            }

            Assert.ThrowsAny<ArgumentException>(() => ProtocolTextCodec.EncodeText("lone \uD800 surrogate"));
        }
    }
}
