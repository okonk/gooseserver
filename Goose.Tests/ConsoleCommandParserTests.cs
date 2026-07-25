using Goose.ConsoleCommands;
using Xunit;

namespace Goose.Tests
{
    public class ConsoleCommandParserTests
    {
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t")]
        public void Parse_ReturnsNullForBlankLines(string line)
        {
            Assert.Null(ConsoleCommandParser.Parse(line));
        }

        [Theory]
        [InlineData("/who")]
        [InlineData("who")]
        [InlineData("  /WHO  ")]
        public void Parse_StripsSlashAndLowercasesName(string line)
        {
            var parsed = ConsoleCommandParser.Parse(line);

            Assert.Equal("who", parsed.Name);
            Assert.Empty(parsed.Args);
        }

        [Fact]
        public void Parse_SplitsArgumentsOnRunsOfWhitespace()
        {
            var parsed = ConsoleCommandParser.Parse("/setaccess   Bob    guide");

            Assert.Equal("setaccess", parsed.Name);
            Assert.Equal(new[] { "Bob", "guide" }, parsed.Args);
        }

        [Fact]
        public void Parse_PreservesArgumentCase()
        {
            var parsed = ConsoleCommandParser.Parse("/setaccess BoB");

            Assert.Equal(new[] { "BoB" }, parsed.Args);
        }
    }
}
