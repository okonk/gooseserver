using Goose;
using Goose.Testing;
using Xunit;

namespace Goose.Tests
{
    public class CharInfoRegenTests
    {
        private static (TestWorldFixture fixture, TestWorldFixture.CapturingPlayer player, Map map) WorldAndPlayer()
        {
            var fixture = new TestWorldFixture();
            var map = fixture.AddBaseMap(1, "Test");
            var player = fixture.CommandPlayerOn(map, 1, 2, "Tester");
            return (fixture, player, map);
        }

        [Fact]
        public void CharInfo_HP_regen_shows_two_decimals()
        {
            var (fixture, player, map) = WorldAndPlayer();
            using (fixture)
            {
                player.BoundMap = map;
                player.MaxStats.HPPercentRegen = 0.0204;

                fixture.RunCommand(player, "/charinfo");

                Assert.Contains(player.Sent, s => s.Contains("HP Regeneration: 2.04%"));
            }
        }

        [Fact]
        public void CharInfo_HP_regen_whole_percent_drops_trailing_zeros()
        {
            var (fixture, player, map) = WorldAndPlayer();
            using (fixture)
            {
                player.BoundMap = map;
                player.MaxStats.HPPercentRegen = 0.02;

                fixture.RunCommand(player, "/charinfo");

                var line = Assert.Single(player.Sent, s => s.Contains("HP Regeneration:"));
                Assert.Contains("HP Regeneration: 2%", line);
                Assert.DoesNotContain("HP Regeneration: 2.00%", line);
            }
        }

        [Fact]
        public void CharInfo_MP_regen_shows_two_decimals()
        {
            var (fixture, player, map) = WorldAndPlayer();
            using (fixture)
            {
                player.BoundMap = map;
                player.MaxStats.MPPercentRegen = 0.015;

                fixture.RunCommand(player, "/charinfo");

                Assert.Contains(player.Sent, s => s.Contains("MP Regeneration: 1.5%"));
            }
        }
    }
}
