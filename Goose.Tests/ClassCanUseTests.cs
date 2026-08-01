using Goose;

namespace Goose.Tests;

/// <summary>class_restrictions is an ALLOW list — the bit at index class_id is set for every class
/// that CAN use the row — and this is the only place that convention is stated as a test rather
/// than as a comment.
///
/// It was a DENY list until the inversion, and the two conventions are the same shape: the same
/// column, the same type, the same bit indices, masks that are legal values under both. Nothing
/// fails loudly if the data and the server disagree about which way round it is — items simply
/// become usable by the wrong classes. See docs/class-restrictions-migration.md.</summary>
public class ClassCanUseTests
{
    private static Class Cls(int id) => new Class { ClassID = id, ClassName = "Class " + id };

    [Fact]
    public void A_set_bit_permits_that_class_and_a_clear_bit_does_not()
    {
        // 68 = bits 2 and 6: the migrated Rogue-only mask from the shipped data.
        Assert.True(Cls(2).CanUse(68));
        Assert.True(Cls(6).CanUse(68));
        Assert.False(Cls(1).CanUse(68));
        Assert.False(Cls(3).CanUse(68));
        Assert.False(Cls(5).CanUse(68));
    }

    [Fact]
    public void Zero_is_no_restriction_at_all_rather_than_no_class()
    {
        // The one value that names no set of classes. It is what an untouched row holds, so
        // reading it as "nobody" would lock every class out of most of the game.
        foreach (var id in new[] { 1, 2, 3, 4, 5, 6, 7 })
        {
            Assert.True(Cls(id).CanUse(0));
        }
    }

    [Fact]
    public void A_class_the_mask_does_not_mention_is_excluded()
    {
        // The whole point of the inversion: a class added after the data was written gets the
        // unrestricted rows and nothing else, instead of getting everything.
        Assert.False(Cls(7).CanUse(68));
        Assert.True(Cls(7).CanUse(0));
    }

    [Fact]
    public void Bit_zero_belongs_to_no_class_and_permits_nobody()
    {
        // Most of the pre-migration masks set bit 0, where there is no class 0. Carried across
        // unconverted it makes a row that meant "everyone" mean "nobody", which is why the
        // migration drops it.
        foreach (var id in new[] { 1, 2, 3, 4, 5, 6 })
        {
            Assert.False(Cls(id).CanUse(1));
        }
    }

    [Fact]
    public void The_high_bits_are_reachable_because_the_mask_is_64_bit()
    {
        // Convert.ToInt64(Math.Pow(2, id)) was the old spelling and 1 << id is the int32 trap;
        // both would misread a class id past 31. There is no such class today — this pins the
        // column's stated capacity of "about 64 classes" instead.
        Assert.True(Cls(40).CanUse(1L << 40));
        Assert.False(Cls(40).CanUse(1L << 39));
        Assert.True(Cls(62).CanUse(1L << 62));
    }
}
