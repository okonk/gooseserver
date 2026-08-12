using Goose;

namespace Goose.Tests;

public class DimensionScalingOverflowTests
{
    /// <summary>abyss NPC.java:927 - (base + 100000*2^dim) * 4.7^dim.
    /// King Terror at dimension 3 is 3.21e9, past int.MaxValue.</summary>
    [Fact]
    public void HP_holds_values_past_int_max()
    {
        long scaled = (long)((30_143_269L + 100_000L * (long)Math.Pow(2, 3)) * Math.Pow(4.7, 3));
        Assert.True(scaled > int.MaxValue);

        var stats = new AttributeSet { HP = scaled, MP = scaled };

        Assert.Equal(scaled, stats.HP);
        Assert.Equal(scaled, stats.MP);
    }

    /// <summary>abyss NPC.java:936 - base*4^dim + 100000*max(0, 4^dim-3), x20 when base < 10m.
    /// A 200k-damage mob at dimension 5 is 6.1e9.</summary>
    [Fact]
    public void WeaponDamage_holds_values_past_int_max()
    {
        long scaled = (long)(200_000L * Math.Pow(4, 5) + 100_000L * Math.Max(0, Math.Pow(4, 5) - 3)) * 20L;
        Assert.True(scaled > int.MaxValue);

        Assert.Equal(scaled, new NPCTemplate { WeaponDamage = scaled }.WeaponDamage);
    }

    /// <summary>Guards AttributeSet.cs:180 - operator* must not narrow-cast the (already long)
    /// HP/MP/SP fields back to int: a (int) cast compiles fine (int widens to long on
    /// assignment) and truncates silently. The SP case is pinned in AttributeSetSPTests;
    /// this guards HP and MP.</summary>
    [Fact]
    public void Multiplying_an_AttributeSet_does_not_truncate_past_int_max()
    {
        var stats = new AttributeSet { HP = 3_000_000_000L, MP = 3_000_000_000L };

        var doubled = stats * 2.0;

        Assert.Equal(6_000_000_000L, doubled.HP);
        Assert.Equal(6_000_000_000L, doubled.MP);
    }

    [Fact]
    public void Copying_an_AttributeSet_does_not_truncate_past_int_max()
    {
        var stats = new AttributeSet { HP = 6_000_000_000L, MP = 6_000_000_000L };

        Assert.Equal(6_000_000_000L, stats.Clone().HP);
        Assert.Equal(6_000_000_000L, stats.Clone().MP);
        // operator+ against an empty set is the copy idiom the NPCTemplate copy ctor uses.
        Assert.Equal(6_000_000_000L, (stats + new AttributeSet()).HP);
        Assert.Equal(12_000_000_000L, (stats + stats).MP);
    }
}
