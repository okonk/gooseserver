using Goose;
using Xunit;

namespace Goose.Tests
{
    public class CritMultiplierTests
    {
        private sealed class FixedRollRandom(int roll) : Random
        {
            public int Calls { get; private set; }

            public override int Next(int minValue, int maxValue)
            {
                Assert.Equal(1, minValue);
                Assert.Equal(10001, maxValue);
                this.Calls++;
                return roll;
            }
        }

        [Theory]
        [InlineData(0.25, 2500, 2)]
        [InlineData(0.25, 2501, 1)]
        [InlineData(0.0, 1, 1)]
        [InlineData(-0.5, 1, 1)]
        [InlineData(1.0, 10000, 2)]
        [InlineData(1.2, 2000, 3)]
        [InlineData(1.2, 2001, 2)]
        [InlineData(3.2, 2000, 5)]
        [InlineData(3.2, 2001, 4)]
        [InlineData(0.07, 700, 2)]
        [InlineData(0.07, 701, 1)]
        public void CritMultiplier_AddsOneHitPerFullHundredPercentAndRollsTheRemainder(double crit, int roll, long expected)
        {
            var random = new FixedRollRandom(roll);

            Assert.Equal(expected, Utils.CritMultiplier(crit, random));
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(-1.0)]
        [InlineData(0.5)]
        [InlineData(2.0)]
        public void CritMultiplier_AlwaysRollsExactlyOnce(double crit)
        {
            var random = new FixedRollRandom(5000);

            Utils.CritMultiplier(crit, random);

            Assert.Equal(1, random.Calls);
        }

        [Fact]
        public void CritMultiplier_AveragesOnePlusCrit()
        {
            var random = new Random(1234);
            const int samples = 200000;
            long total = 0;
            for (int i = 0; i < samples; i++)
            {
                total += Utils.CritMultiplier(2.35, random);
            }

            Assert.InRange(total / (double)samples, 3.33, 3.37);
        }
    }
}
