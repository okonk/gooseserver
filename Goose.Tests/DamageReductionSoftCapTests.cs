using Goose;
using Xunit;

namespace Goose.Tests
{
    public class DamageReductionSoftCapTests
    {
        [Theory]
        [InlineData(0.0, 0.0)]
        [InlineData(0.05, 0.05)]
        [InlineData(0.3, 0.3)]
        [InlineData(0.5, 0.5)]
        [InlineData(-0.2, -0.2)]
        [InlineData(0.54, 0.537037037)]
        [InlineData(1.0, 0.75)]
        [InlineData(1.5, 0.833333333)]
        [InlineData(2.0, 0.875)]
        public void EffectiveDamageReduction_IsLinearToTheSoftCapThenTapers(double damageReduction, double expected)
        {
            Assert.Equal(expected, Utils.EffectiveDamageReduction(damageReduction, 0.5), 6);
        }

        [Fact]
        public void EffectiveDamageReduction_NeverReachesImmunity()
        {
            Assert.True(Utils.EffectiveDamageReduction(1000, 0.5) < 1);
        }

        [Fact]
        public void EffectiveDamageReduction_KeepsIncreasingPastTheSoftCap()
        {
            double previous = Utils.EffectiveDamageReduction(0.5, 0.5);
            for (double dr = 0.6; dr <= 5; dr += 0.1)
            {
                double current = Utils.EffectiveDamageReduction(dr, 0.5);
                Assert.True(current > previous);
                previous = current;
            }
        }

        [Theory]
        [InlineData(1.0)]
        [InlineData(1.5)]
        public void EffectiveDamageReduction_SoftCapAtOrAboveOneKeepsTheOldLinearBehaviour(double softCap)
        {
            Assert.Equal(1.2, Utils.EffectiveDamageReduction(1.2, softCap));
        }

        [Fact]
        public void Settings_DefaultSoftCapIsHalf()
        {
            Assert.Equal(0.5, new GooseSettings().DamageReductionSoftCap);
        }
    }
}
