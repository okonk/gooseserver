using Goose;
using Xunit;

namespace Goose.Tests.Revive;

public class RevivePacketTests
{
    private static Item EquipMount(ReviveTestKit kit, Player p, int graphicA)
    {
        var template = kit.Fixture.AddBaseItemTemplate(651, "Tank", ItemTemplate.UseTypes.Armor, t =>
        {
            t.Slot = ItemTemplate.ItemSlots.Mount;
            t.GraphicEquipped = 273;
            t.GraphicA = graphicA;
        });
        var mount = new Item();
        mount.LoadFromTemplate(template);
        kit.World.ItemHandler.AddAndAssignId(mount, kit.World);
        Assert.True(p.Inventory.AddItem(mount, 1, kit.World));
        Assert.True(p.Inventory.Equip(mount, kit.World));
        return mount;
    }

    private static int LastToken(string packet) => int.Parse(packet[(packet.LastIndexOf(',') + 1)..]);

    [Fact]
    public void Alive_player_packets_end_with_zero()
    {
        using var kit = new ReviveTestKit();
        var p = kit.PlayerAt(5, 5);

        Assert.EndsWith(",0,*,0", P.MakeCharacter(p));
        Assert.EndsWith(",0,*,0", P.UpdateCharacter(p));
    }

    [Fact]
    public void Corpse_packets_end_with_remaining_seconds()
    {
        using var kit = new ReviveTestKit(reviveTimeout: 60);
        var p = kit.PlayerAt(5, 5);
        kit.Kill(p);

        Assert.InRange(LastToken(P.MakeCharacter(p)), 59, 60);
        Assert.InRange(LastToken(P.UpdateCharacter(p)), 59, 60);
        Assert.EndsWith(",0,*," + p.RespawnSeconds, P.MakeCharacter(p));
    }

    [Fact]
    public void Untinted_mount_tail()
    {
        using var kit = new ReviveTestKit();
        var p = kit.PlayerAt(5, 5);
        EquipMount(kit, p, graphicA: 0);

        Assert.EndsWith(",273,*,0", P.MakeCharacter(p));
        Assert.EndsWith(",273,*,0", P.UpdateCharacter(p));
    }

    [Fact]
    public void Tinted_mount_tail()
    {
        using var kit = new ReviveTestKit();
        var p = kit.PlayerAt(5, 5);
        EquipMount(kit, p, graphicA: 100);

        Assert.EndsWith(",273,255,255,255,100,0", P.MakeCharacter(p));
        Assert.EndsWith(",273,255,255,255,100,0", P.UpdateCharacter(p));
    }

    [Fact]
    public void Non_layered_body_tail()
    {
        using var kit = new ReviveTestKit();
        var p = kit.PlayerAt(5, 5);
        p.CurrentBodyID = 105;

        int speed = p.CalculateMoveSpeed();

        Assert.EndsWith(",0,0," + speed + ",0,0", P.MakeCharacter(p));
        Assert.EndsWith(",0," + speed + ",0", P.UpdateCharacter(p));
    }

    [Fact]
    public void Corpse_shows_base_body_not_illusion()
    {
        using var kit = new ReviveTestKit();
        var p = kit.PlayerAt(5, 5);
        p.CurrentBodyID = 105;
        kit.Kill(p);

        Assert.StartsWith("CHP" + p.LoginID + ",1,", P.UpdateCharacter(p));
        Assert.Contains(",1,", P.MakeCharacter(p));
        Assert.DoesNotContain(",105,", P.MakeCharacter(p));
        Assert.Equal(105, p.CurrentBodyID);
    }

    [Fact]
    public void Npc_packets_are_unchanged_by_dead_state()
    {
        using var kit = new ReviveTestKit();
        string before = P.MakeNPCCharacter(kit.Killer);
        var p = kit.PlayerAt(5, 5);
        kit.Kill(p);

        Assert.Equal(before, P.MakeNPCCharacter(kit.Killer));
    }

    [Fact]
    public void Corpse_warped_while_dead_stays_dead_with_seconds()
    {
        using var kit = new ReviveTestKit();
        var viewer = kit.PlayerAt(10, 10, "Viewer");
        var p = kit.PlayerAt(5, 5);
        kit.Kill(p);
        viewer.Sent.Clear();

        p.WarpTo(kit.World, kit.Map, 9, 10);

        Assert.True(p.IsDead);
        var mkc = Assert.Single(viewer.Sent, s => s.StartsWith("MKC" + p.LoginID + ","));
        Assert.True(LastToken(mkc.TrimEnd('\x1')) >= 1);
    }
}
