using System.Linq;

namespace Goose
{
    public static class NameRules
    {
        public const int MinLength = 3;
        public const int MaxLength = 16;

        public static bool IsLetters(string name)
            => name.Length > 0 && name.All(IsLetter);

        public static bool IsValidPlayerName(string name)
            => name.Length >= MinLength && name.Length <= MaxLength && IsLetters(name);

        public static bool IsValidPetName(string name)
            => name.Length >= MinLength
               && name.Length <= MaxLength
               && IsLetter(name[0])
               && IsLetter(name[^1])
               && name.All(c => c == ' ' || IsLetter(c))
               && !name.Contains("  ");

        private static bool IsLetter(char c)
            => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
    }
}
