using Goose;
using Xunit;

namespace Goose.Tests
{
    public class DodgeTests
    {
        private sealed class FixedRollRandom(int roll) : Random
        {
            public override int Next(int minValue, int maxValue)
            {
                Assert.Equal(1, minValue);
                Assert.Equal(10001, maxValue);
                return roll;
            }
        }

        [Theory]
        [InlineData(20, 100, true)]
        [InlineData(20, 101, false)]
        [InlineData(200, 1000, true)]
        [InlineData(200, 1001, false)]
        [InlineData(458, 2290, true)]
        [InlineData(458, 2291, false)]
        [InlineData(1000, 5000, true)]
        [InlineData(5000, 5000, true)]
        [InlineData(5000, 5001, false)]
        public void Dodges_AtOnePercentPerTwentyDexterityCappedAtFiftyPercent(int dexterity, int roll, bool expected)
        {
            Assert.Equal(expected, Utils.Dodges(dexterity, 20, 0.5, new FixedRollRandom(roll)));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-10)]
        public void Dodges_NeverWithoutDexterity(int dexterity)
        {
            Assert.False(Utils.Dodges(dexterity, 20, 0.5, new FixedRollRandom(1)));
        }
    }
}
