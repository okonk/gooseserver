using Goose;
using Goose.Testing;
using Xunit;

namespace Goose.Tests;

public class MountDisplayTests
{
    private static (TestWorldFixture Fixture, TestWorldFixture.CapturingPlayer Player) Setup(int graphicA)
    {
        var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(1, "Test");
        var player = fixture.CommandPlayerOn(map, 1, 2);
        var template = fixture.AddBaseItemTemplate(651, "Tank", ItemTemplate.UseTypes.Armor, t =>
        {
            t.Slot = ItemTemplate.ItemSlots.Mount;
            t.GraphicEquipped = 273;
            t.GraphicR = 10;
            t.GraphicG = 20;
            t.GraphicB = 30;
            t.GraphicA = graphicA;
        });
        var mount = new Item();
        mount.LoadFromTemplate(template);
        fixture.World.ItemHandler.AddAndAssignId(mount, fixture.World);
        Assert.True(player.Inventory.AddItem(mount, 1, fixture.World));
        Assert.True(player.Inventory.Equip(mount, fixture.World));
        return (fixture, player);
    }

    [Fact]
    public void Tinted_mount_has_no_trailing_comma()
    {
        var (fixture, player) = Setup(graphicA: 100);
        using (fixture)
            Assert.Equal("273,10,20,30,100", player.Inventory.MountDisplay());
    }

    [Fact]
    public void Untinted_mount_is_id_star()
    {
        var (fixture, player) = Setup(graphicA: 0);
        using (fixture)
            Assert.Equal("273,*", player.Inventory.MountDisplay());
    }

    [Fact]
    public void Unmounted_is_zero_star()
    {
        var (fixture, player) = Setup(graphicA: 100);
        using (fixture)
        {
            player.Mounted = false;
            Assert.Equal("0,*", player.Inventory.MountDisplay());
        }
    }
}
