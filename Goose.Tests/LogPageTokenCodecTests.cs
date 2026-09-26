using Goose.Logs;
using Xunit;

namespace Goose.Tests
{
    public class LogPageTokenCodecTests
    {
        [Fact]
        public void Generated_tokens_are_22_char_canonical_unpadded_base64url()
        {
            for (int i = 0; i < 500; i++)
            {
                string token = LogPageTokenCodec.Create();
                Assert.Equal(22, token.Length);
                Assert.True(token.All(c => char.IsLetterOrDigit(c) || c is '-' or '_'));
                Assert.True(LogPageTokenCodec.IsCanonical(token));
                Assert.True(LogPageTokenCodec.TryDecode(token, out byte[] bytes));
                Assert.Equal(16, bytes.Length);
            }
        }

        [Fact]
        public void All_128_bits_are_represented_through_injected_rng()
        {
            byte[] bits = Enumerable.Range(0, 16).Select(i => (byte)(i * 17 + 3)).ToArray();
            string token = LogPageTokenCodec.Create(span => bits.CopyTo(span));
            Assert.True(LogPageTokenCodec.IsCanonical(token));
            Assert.True(LogPageTokenCodec.TryDecode(token, out byte[] decoded));
            Assert.Equal(bits, decoded);
        }

        [Fact]
        public void Identical_random_bits_produce_identical_tokens()
        {
            byte[] bits = Enumerable.Range(0, 16).Select(i => (byte)(255 - i * 7)).ToArray();
            string first = LogPageTokenCodec.Create(span => bits.CopyTo(span));
            string second = LogPageTokenCodec.Create(span => bits.CopyTo(span));
            Assert.Equal(first, second);

            bits[15] ^= 1;
            Assert.NotEqual(first, LogPageTokenCodec.Create(span => bits.CopyTo(span)));
        }

        [Fact]
        public void All_ff_bits_encode_to_canonical_token()
        {
            byte[] bits = new byte[16];
            Array.Fill(bits, (byte)0xFF);
            string token = LogPageTokenCodec.Create(span => bits.CopyTo(span));
            Assert.Equal("_____________________w", token);
            Assert.True(LogPageTokenCodec.IsCanonical(token));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("A")]
        [InlineData("AAAAAAAAAAAAAAAAAAAAA")]
        [InlineData("AAAAAAAAAAAAAAAAAAAAAB")]
        [InlineData("AAAAAAAAAAAAAAAAAAAA+")]
        [InlineData("AAAAAAAAAAAAAAAAAAAA/")]
        [InlineData("AAAAAAAAAAAAAAAAAAAA=")]
        [InlineData("AAAAAAAAAAAAAAAAAAAA ")]
        [InlineData("AAAAAAAAAAAAAAAAAAAA$")]
        [InlineData("AAAAAAAAAAAAAAAAAAAAAN")]
        [InlineData("AAAAAAAAAAAAAAAAAAAAAE")]
        [InlineData("AAAAAAAAAAAAAAAAAAAAA\n")]
        public void Rejects_noncanonical_tokens(string token)
        {
            Assert.False(LogPageTokenCodec.IsCanonical(token));
            Assert.False(LogPageTokenCodec.TryDecode(token, out byte[] bytes));
            Assert.Empty(bytes);
        }

        [Fact]
        public void Single_character_flip_stays_canonical_but_changes_dictionary_key()
        {
            string token = LogPageTokenCodec.Create();
            int index = token[0] == 'A' ? 1 : 0;
            char replacement = token[index] == 'B' ? 'C' : 'B';
            string flipped = token[..index] + replacement + token[(index + 1)..];

            Assert.Equal(22, flipped.Length);
            Assert.True(LogPageTokenCodec.IsCanonical(flipped));
            Assert.True(LogPageTokenCodec.TryDecode(token, out byte[] original));
            Assert.True(LogPageTokenCodec.TryDecode(flipped, out byte[] flippedBytes));
            Assert.NotEqual(original, flippedBytes);
        }
    }
}
