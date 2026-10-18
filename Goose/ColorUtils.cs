namespace Goose
{
    public static class ColorUtils
    {
        public static bool TryParseHex(string? hex, out uint rgba)
        {
            rgba = 0;
            if (hex is null || hex.Length == 0 || hex[0] != '#')
                return false;

            string digits = hex[1..];
            if (digits.Length == 3)
                digits = $"{digits[0]}{digits[0]}{digits[1]}{digits[1]}{digits[2]}{digits[2]}";
            if (digits.Length == 6)
                digits += "FF";

            if (digits.Length != 8)
                return false;

            uint value = 0;
            foreach (char c in digits)
            {
                int nibble = c switch
                {
                    >= '0' and <= '9' => c - '0',
                    >= 'a' and <= 'f' => c - 'a' + 10,
                    >= 'A' and <= 'F' => c - 'A' + 10,
                    _ => -1
                };
                if (nibble < 0)
                    return false;
                value = (value << 4) | (uint)nibble;
            }

            rgba = value;
            return true;
        }
    }
}
