using Goose;
using Goose.ConsoleCommands;
using Xunit;

namespace Goose.Tests
{
    public class SetAccessCommandTests
    {
        [Fact]
        public void TryParse_DefaultsToGameMaster()
        {
            Assert.True(SetAccessCommand.TryParse(new[] { "Bob" }, out var request, out _));

            Assert.Equal("Bob", request!.Name);
            Assert.Equal(Player.AccessStatus.GameMaster, request.Level);
        }

        [Theory]
        [InlineData("guide")]
        [InlineData("GUIDE")]
        [InlineData("Guide")]
        public void TryParse_MatchesLevelNameCaseInsensitively(string level)
        {
            Assert.True(SetAccessCommand.TryParse(new[] { "Bob", level }, out var request, out _));

            Assert.Equal(Player.AccessStatus.Guide, request!.Level);
        }

        [Fact]
        public void TryParse_RejectsMissingName()
        {
            Assert.False(SetAccessCommand.TryParse(new string[0], out var request, out string? error));

            Assert.Null(request);
            Assert.Contains("Usage: /setaccess", error!);
        }

        [Fact]
        public void TryParse_RejectsUnknownLevelName()
        {
            Assert.False(SetAccessCommand.TryParse(new[] { "Bob", "wizard" }, out _, out string? error));

            Assert.Contains("Unknown access level 'wizard'.", error!);
            Assert.Contains("GameMaster", error);
        }

        /**
         * Enum.TryParse would accept these. The in game /setaccess matches by name
         * only, and "42" is not even a defined value, so both must be refused.
         */
        [Theory]
        [InlineData("9")]
        [InlineData("42")]
        public void TryParse_RejectsNumericLevels(string level)
        {
            Assert.False(SetAccessCommand.TryParse(new[] { "Bob", level }, out _, out _));
        }

        [Fact]
        public void TryParse_IgnoresExtraTrailingArguments()
        {
            Assert.True(SetAccessCommand.TryParse(new[] { "Bob", "guide", "junk" }, out var request, out _));

            Assert.Equal("Bob", request!.Name);
            Assert.Equal(Player.AccessStatus.Guide, request.Level);
        }
    }
}
