using System.Text;

namespace Goose
{
    public static class ProtocolTextCodec
    {
        private static readonly UTF8Encoding StrictUtf8 = new(false, true);

        public static string EncodeText(string text)
        {
            return Convert.ToBase64String(StrictUtf8.GetBytes(text));
        }

        public static bool TryDecodeText(string base64, int maxBytes, out string text)
        {
            text = string.Empty;
            if (!TryDecodeBytes(base64, maxBytes, out byte[] bytes))
                return false;
            try
            {
                text = StrictUtf8.GetString(bytes);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        public static bool TryDecodeBytes(string base64, int maxBytes, out byte[] bytes)
        {
            bytes = Array.Empty<byte>();
            if (maxBytes < 0 || base64 is null || base64.Length % 4 != 0)
                return false;

            if (base64.Length == 0)
                return true;

            int padding = 0;
            if (base64[^1] == '=')
            {
                padding = 1;
                if (base64[^2] == '=')
                    padding = 2;
            }

            int dataLength = base64.Length - padding;
            int byteLength = dataLength * 3 / 4;
            if (byteLength > maxBytes)
                return false;

            var result = new byte[byteLength];
            int output = 0;
            for (int i = 0; i < dataLength; i += 4)
            {
                int a = StandardValue(base64[i]);
                int b = StandardValue(base64[i + 1]);
                bool hasThird = i + 2 < dataLength;
                int c = hasThird ? StandardValue(base64[i + 2]) : 0;
                bool hasFourth = i + 3 < dataLength;
                int d = hasFourth ? StandardValue(base64[i + 3]) : 0;
                if (a < 0 || b < 0 || (hasThird && c < 0) || (hasFourth && d < 0))
                    return false;

                int unusedBits = hasFourth ? 0 : hasThird ? 2 : 4;
                int lastValue = hasThird ? c : b;
                if ((lastValue & ((1 << unusedBits) - 1)) != 0)
                    return false;

                int quantum = (a << 18) | (b << 12) | (c << 6) | d;
                result[output++] = (byte)(quantum >> 16);
                if (hasThird)
                    result[output++] = (byte)(quantum >> 8);
                if (hasFourth)
                    result[output++] = (byte)quantum;
            }

            bytes = result;
            return true;
        }

        private static int StandardValue(char c) => c switch
        {
            >= 'A' and <= 'Z' => c - 'A',
            >= 'a' and <= 'z' => c - 'a' + 26,
            >= '0' and <= '9' => c - '0' + 52,
            '+' => 62,
            '/' => 63,
            _ => -1,
        };
    }
}
