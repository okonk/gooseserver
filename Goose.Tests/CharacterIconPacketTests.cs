using Goose;
using Goose.Testing;
using Xunit;

namespace Goose.Tests
{
    public class CharacterIconPacketTests
    {
        [Fact]
        public void CharacterIcon_FormatsSheetAndGraphic()
        {
            var character = new Player(0) { LoginID = 42 };

            Assert.Equal("CHI42,2276,332038", P.CharacterIcon(character, 2276, 332038));
        }

        [Fact]
        public void CharacterIcon_ClearIsExplicitZeroZero()
        {
            var character = new Player(0) { LoginID = 7 };

            Assert.Equal("CHI7,0,0", P.CharacterIcon(character, 0, 0));
        }

        [Fact]
        public void SendCharacterIcon_SendsCHIForTheTargetCharacter()
        {
            using var fixture = new TestWorldFixture();
            var viewer = fixture.CommandPlayerOn(fixture.AddBaseMap(1, "Town"), 1, 1);
            var target = new Player(0) { LoginID = 99 };

            fixture.World.SendCharacterIcon(viewer, target, 2276, 332038);

            Assert.Contains("CHI99,2276,332038\x1", viewer.Sent);
        }

        [Fact]
        public void SendCharacterIcon_ClearSendsZeroZero()
        {
            using var fixture = new TestWorldFixture();
            var viewer = fixture.CommandPlayerOn(fixture.AddBaseMap(1, "Town"), 1, 1);
            var target = new Player(0) { LoginID = 99 };

            fixture.World.SendCharacterIcon(viewer, target, 0, 0);

            Assert.Contains("CHI99,0,0\x1", viewer.Sent);
        }
    }
}
