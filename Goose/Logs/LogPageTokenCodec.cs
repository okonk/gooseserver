using System.Security.Cryptography;

namespace Goose.Logs
{
    public static class LogPageTokenCodec
    {
        public const int TokenLength = 22;
        public const int TokenBytes = 16;

        private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";

        public static string Create() => Create(span => RandomNumberGenerator.Fill(span));

        internal static string Create(Action<Span<byte>> fill)
        {
            Span<byte> bytes = stackalloc byte[TokenBytes];
            fill(bytes);
            return Encode(bytes);
        }

        public static bool IsCanonical(string token)
        {
            if (token is null || token.Length != TokenLength)
                return false;
            Span<byte> bytes = stackalloc byte[TokenBytes];
            if (!TryDecode(token, bytes))
                return false;
            return Encode(bytes) == token;
        }

        public static bool TryDecode(string token, out byte[] bytes)
        {
            bytes = Array.Empty<byte>();
            if (!IsCanonical(token))
                return false;
            Span<byte> span = stackalloc byte[TokenBytes];
            _ = TryDecode(token, span);
            bytes = span.ToArray();
            return true;
        }

        private static string Encode(ReadOnlySpan<byte> bytes)
        {
            Span<char> chars = stackalloc char[TokenLength];
            for (int i = 0; i < 5; i++)
            {
                int a = bytes[i * 3];
                int b = bytes[i * 3 + 1];
                int c = bytes[i * 3 + 2];
                chars[i * 4] = Alphabet[a >> 2];
                chars[i * 4 + 1] = Alphabet[((a & 3) << 4) | (b >> 4)];
                chars[i * 4 + 2] = Alphabet[((b & 15) << 2) | (c >> 6)];
                chars[i * 4 + 3] = Alphabet[c & 63];
            }
            chars[20] = Alphabet[bytes[15] >> 2];
            chars[21] = Alphabet[(bytes[15] & 3) << 4];
            return new string(chars);
        }

        private static bool TryDecode(string token, Span<byte> bytes)
        {
            if (token is null || token.Length != TokenLength)
                return false;
            Span<int> values = stackalloc int[TokenLength];
            for (int i = 0; i < TokenLength; i++)
            {
                values[i] = UrlValue(token[i]);
                if (values[i] < 0)
                    return false;
            }

            for (int i = 0; i < 5; i++)
            {
                int quantum = (values[i * 4] << 18) | (values[i * 4 + 1] << 12)
                    | (values[i * 4 + 2] << 6) | values[i * 4 + 3];
                bytes[i * 3] = (byte)(quantum >> 16);
                bytes[i * 3 + 1] = (byte)(quantum >> 8);
                bytes[i * 3 + 2] = (byte)quantum;
            }
            bytes[15] = (byte)((values[20] << 2) | (values[21] >> 4));
            return true;
        }

        private static int UrlValue(char c)
        {
            int index = Alphabet.IndexOf(c);
            return index;
        }
    }
}
