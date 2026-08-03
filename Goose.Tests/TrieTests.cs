using Goose;
using Xunit;

namespace Goose.Tests
{
    public class TrieTests
    {
        [Fact]
        public void Insert_And_ExactMatch_ReturnsValue()
        {
            var trie = new Trie<string>();
            trie.Insert("LOGIN", "login-handler");

            Assert.True(trie.TryGetValue("LOGIN", out string? value));
            Assert.Equal("login-handler", value);
        }

        [Fact]
        public void TryGetValue_NonExistentKey_ReturnsFalse()
        {
            var trie = new Trie<int>();
            trie.Insert("GET", 42);

            Assert.False(trie.TryGetValue("SET", out _));
        }

        [Fact]
        public void ContainsKey_ReturnsTrueForInsertedKey()
        {
            var trie = new Trie<int>();
            trie.Insert("ATT", 1);

            Assert.True(trie.ContainsKey("ATT"));
            Assert.False(trie.ContainsKey("ATTACK"));
        }

        [Fact]
        public void ContainsKey_PartialPathIsNotAKey()
        {
            var trie = new Trie<int>();
            trie.Insert("LOGIN", 1);

            // "LOG" is on the path but was never inserted as a key
            Assert.False(trie.ContainsKey("LOG"));
        }

        [Fact]
        public void LongestPrefixMatch_ExactMatch()
        {
            var trie = new Trie<string>();
            trie.Insert("M1", "move");

            Assert.True(trie.TryGetLongestPrefix("M1", out string? value, out int length));
            Assert.Equal("move", value);
            Assert.Equal(2, length);
        }

        [Fact]
        public void LongestPrefixMatch_PrefixOfLongerQuery()
        {
            var trie = new Trie<string>();
            trie.Insert("/group ", "group-chat");

            Assert.True(trie.TryGetLongestPrefix("/group add someone", out string? value, out int length));
            Assert.Equal("group-chat", value);
            Assert.Equal(7, length);
        }

        [Fact]
        public void LongestPrefixMatch_PicksLongestWhenMultiplePrefixesMatch()
        {
            var trie = new Trie<string>();
            trie.Insert("/group", "group-info");
            trie.Insert("/group ", "group-chat");
            trie.Insert("/groupadd ", "group-add");

            // "/group add" matches "/group" (6) and "/group " (7) — longest wins
            Assert.True(trie.TryGetLongestPrefix("/group add", out string? value, out int length));
            Assert.Equal("group-chat", value);
            Assert.Equal(7, length);

            // "/groupadd member" matches "/group" (6), "/group " (7), "/groupadd " (10)
            Assert.True(trie.TryGetLongestPrefix("/groupadd member", out value, out length));
            Assert.Equal("group-add", value);
            Assert.Equal(10, length);
        }

        [Fact]
        public void LongestPrefixMatch_NoMatch_ReturnsFalse()
        {
            var trie = new Trie<int>();
            trie.Insert("LOGIN", 1);

            Assert.False(trie.TryGetLongestPrefix("LOGOUT", out _, out _));
        }

        [Fact]
        public void LongestPrefixMatch_NoCommonPrefix_ReturnsFalse()
        {
            var trie = new Trie<int>();
            trie.Insert("ATT", 1);
            trie.Insert("CAST", 2);

            Assert.False(trie.TryGetLongestPrefix("HEAL", out _, out _));
        }

        [Fact]
        public void LongestPrefixMatch_IntermediateValueOnPath()
        {
            var trie = new Trie<string>();
            trie.Insert("S", "single-char");
            trie.Insert("SID", "spell-info");

            Assert.True(trie.TryGetLongestPrefix("S", out string? value, out int length));
            Assert.Equal("single-char", value);
            Assert.Equal(1, length);

            Assert.True(trie.TryGetLongestPrefix("SID", out value, out length));
            Assert.Equal("spell-info", value);
            Assert.Equal(3, length);

            Assert.True(trie.TryGetLongestPrefix("SIDE", out value, out length));
            Assert.Equal("spell-info", value);
            Assert.Equal(3, length);
        }

        [Fact]
        public void Insert_OverwritesExistingValue()
        {
            var trie = new Trie<string>();
            trie.Insert("GET", "pickup");
            trie.Insert("GET", "new-pickup");

            Assert.True(trie.TryGetValue("GET", out string? value));
            Assert.Equal("new-pickup", value);
        }

        [Fact]
        public void LongestPrefixMatch_EmptyQuery_ReturnsFalse()
        {
            var trie = new Trie<int>();
            trie.Insert("LOGIN", 1);

            Assert.False(trie.TryGetLongestPrefix("", out _, out _));
        }

        [Fact]
        public void LongestPrefixMatch_DivergesMidway_ReturnsFalse()
        {
            var trie = new Trie<string>();
            trie.Insert("/toggle ", "toggle");
            trie.Insert("/togglegroup", "toggle-group");

            // "/togglex" diverges after "/toggle" — but "/toggle" (7 chars no space)
            // was NOT inserted, so no match
            Assert.False(trie.TryGetLongestPrefix("/togglex", out _, out _));
        }

        [Fact]
        public void LongestPrefixMatch_DivergesMidway_IntermediateMatches()
        {
            var trie = new Trie<string>();
            trie.Insert("/toggle", "toggle-cmd");
            trie.Insert("/toggle ", "toggle-with-arg");

            // "/togglex" matches "/toggle" (7 chars)
            Assert.True(trie.TryGetLongestPrefix("/togglex", out string? value, out int length));
            Assert.Equal("toggle-cmd", value);
            Assert.Equal(7, length);
        }

        [Fact]
        public void SingleCharacterKeys()
        {
            var trie = new Trie<string>();
            trie.Insert(";", "chat");
            trie.Insert("M", "move");

            Assert.True(trie.TryGetLongestPrefix("; hello world", out string? value, out int length));
            Assert.Equal("chat", value);
            Assert.Equal(1, length);

            Assert.True(trie.TryGetLongestPrefix("M1", out value, out length));
            Assert.Equal("move", value);
            Assert.Equal(1, length);
        }

        [Fact]
        public void LongestPrefixMatch_MultiByteCharacters()
        {
            var trie = new Trie<string>();
            trie.Insert("/café", "coffee");

            Assert.True(trie.TryGetLongestPrefix("/café latte", out string? value, out int length));
            Assert.Equal("coffee", value);
            Assert.Equal("/café".Length, length);
        }
    }
}
