using Goose;
using Xunit;

namespace Goose.Tests;

public class AttributeSetSPTests
{
    /// <summary>A dimension-6 item costs base x 3^6 = 729x, and base values reach
    /// 10,000,000 - past int.MaxValue. SP is the spirit wallet, so it must hold it.</summary>
    [Fact]
    public void SP_HoldsValuesBeyondIntMax()
    {
        long beyondInt = (long)int.MaxValue + 1000;

        var stats = new AttributeSet { SP = beyondInt };

        Assert.Equal(beyondInt, stats.SP);
    }

    [Fact]
    public void SP_SumsWithoutOverflowing()
    {
        var a = new AttributeSet { SP = int.MaxValue };
        var b = new AttributeSet { SP = int.MaxValue };

        Assert.Equal(2L * int.MaxValue, (a + b).SP);
    }

    /// <summary>The StatMultiplier operator must not truncate SP past int.MaxValue - a
    /// dimension-6 item costs base x 729 and base values reach 10,000,000. A (int) cast
    /// here would compile (int widens to long) and silently truncate.</summary>
    [Fact]
    public void SP_MultiplyDoesNotTruncatePastIntMax()
    {
        Assert.Equal(6_000_000_000L, (new AttributeSet { SP = 3_000_000_000L } * 2.0).SP);
        Assert.Equal(7_290_000_000L, (new AttributeSet { SP = 10_000_000L } * 729).SP);
    }
}
