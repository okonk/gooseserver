using Goose;
using Goose.Commands;
using Goose.Testing;
using Xunit;
using static Goose.Inventory;

namespace Goose.Tests;

public class EquipGraphicCommandTests
{
    private static readonly EquipSlots WeaponSlot = EquipSlots.Weapon;

    private sealed record SetupResult(
        TestWorldFixture World,
        Map Map,
        TestWorldFixture.CapturingPlayer Player,
        ItemTemplate Template,
        CommandContext Ctx);

    private static SetupResult Setup(Player.AccessStatus access = Player.AccessStatus.GameMaster)
    {
        var world = new TestWorldFixture();
        var map = world.AddBaseMap(1, "Town");
        var player = world.CommandPlayerOn(map, 5, 5, "Gm");
        player.Access = access;

        var template = world.AddBaseItemTemplate(200, "Sword", ItemTemplate.UseTypes.Weapon, t =>
        {
            t.GraphicEquipped = 101;
            t.GraphicTile = 7;
            t.GraphicFile = 3;
            t.GraphicR = 1;
            t.GraphicG = 2;
            t.GraphicB = 3;
            t.GraphicA = 4;
        });

        var item = new Item();
        item.LoadFromTemplate(template);
        world.World.ItemHandler.AddAndAssignId(item, world.World);
        Assert.True(player.Inventory.AddItem(item, 1, world.World));
        Assert.True(player.Inventory.Equip(item, world.World));
        player.Sent.Clear();

        var ctx = new CommandContext(player, world.World, new CommandRegistry(), [], "")
        {
            Usage = "Usage: /equipgraphic <slot> <id> [r g b a] [sheet tile]",
        };
        return new SetupResult(world, map, player, template, ctx);
    }

    private static Item Equipped(SetupResult s) => s.Player.Inventory.GetEquippedSlot(WeaponSlot)!.Item;

    [Fact]
    public void Execute_GraphicOnly_ChangesInstanceNotTemplate()
    {
        var s = Setup();

        new EquipGraphicCommand().Execute(s.Ctx, "weapon", 45);

        Assert.Equal(45, Equipped(s).GraphicEquipped);
        Assert.Equal(101, s.Template.GraphicEquipped);
        Assert.Equal(1, Equipped(s).GraphicR);
        Assert.Equal(3, Equipped(s).GraphicFile);
        Assert.Contains(s.Player.Sent, m => m.StartsWith("CHP") && m.Contains(",45,1,2,3,4,"));
    }

    [Fact]
    public void Execute_WithColour_SetsTintOnInstanceOnly()
    {
        var s = Setup();

        new EquipGraphicCommand().Execute(s.Ctx, "weapon", 45, 9, 8, 7, 6);

        Item item = Equipped(s);
        Assert.Equal((45, 9, 8, 7, 6), (item.GraphicEquipped, item.GraphicR, item.GraphicG, item.GraphicB, item.GraphicA));
        Assert.Equal((101, 1, 2, 3, 4),
            (s.Template.GraphicEquipped, s.Template.GraphicR, s.Template.GraphicG, s.Template.GraphicB, s.Template.GraphicA));
        Assert.Contains(s.Player.Sent, m => m.StartsWith("CHP") && m.Contains(",45,9,8,7,6,"));
    }

    [Fact]
    public void Execute_WithSheetAndTile_SetsIconAndClearsTint()
    {
        var s = Setup();

        new EquipGraphicCommand().Execute(s.Ctx, "weapon", 45, 0, 0, 0, 0, 12, 34);

        Item item = Equipped(s);
        Assert.Equal(12, item.GraphicFile);
        Assert.Equal(34, item.GraphicTile);
        Assert.Equal(0, item.GraphicA);
        Assert.Equal(3, s.Template.GraphicFile);
        Assert.Equal(7, s.Template.GraphicTile);

        // Equipped slots reach the owner's client as SIS on slot id + 31, tile then sheet.
        Assert.Contains(s.Player.Sent, m => m.StartsWith("SIS32|34|12|"));
        Assert.Contains(s.Player.Sent, m => m.StartsWith("CHP") && m.Contains(",45,*,"));
    }

    [Fact]
    public void Execute_BroadcastsCharacterUpdateToNearbyPlayers()
    {
        var s = Setup();
        var nearby = s.World.CommandPlayerOn(s.Map, 6, 5, "Nearby");
        s.Map.AddPlayer(nearby, s.World.World);

        new EquipGraphicCommand().Execute(s.Ctx, "weapon", 45);

        Assert.Contains(nearby.Sent, m => m.StartsWith("CHP") && m.Contains(",45,1,2,3,4,"));
        Assert.DoesNotContain(nearby.Sent, m => m.StartsWith("SIS"));
    }

    [Fact]
    public void Execute_EmptySlot_SendsMessageAndSendsNoUpdate()
    {
        var s = Setup();

        new EquipGraphicCommand().Execute(s.Ctx, "HEAD", 45);

        Assert.Contains(s.Player.Sent, m => m.Contains("nothing equipped in your head slot"));
        Assert.DoesNotContain(s.Player.Sent, m => m.StartsWith("CHP"));
        Assert.DoesNotContain(s.Player.Sent, m => m.StartsWith("SIS"));
    }

    [Fact]
    public void Execute_UnknownSlot_ListsSlots()
    {
        var s = Setup();

        new EquipGraphicCommand().Execute(s.Ctx, "boots", 45);

        Assert.Contains(s.Player.Sent, m => m.Contains("Unknown slot boots") && m.Contains("ring1"));
        Assert.Equal(101, Equipped(s).GraphicEquipped);
    }

    [Fact]
    public void Execute_NegativeGraphic_SendsMessage()
    {
        var s = Setup();

        new EquipGraphicCommand().Execute(s.Ctx, "weapon", -1);

        Assert.Contains(s.Player.Sent, m => m.Contains("graphic id must be 0 or more"));
        Assert.Equal(101, Equipped(s).GraphicEquipped);
    }

    [Fact]
    public void Execute_PartialColour_SendsUsageAndChangesNothing()
    {
        var s = Setup();

        new EquipGraphicCommand().Execute(s.Ctx, "weapon", 45, 9, 8, 7);

        Assert.Contains(s.Player.Sent, m => m.Contains("Usage: /equipgraphic <slot> <id> [r g b a] [sheet tile]"));
        Assert.Equal(101, Equipped(s).GraphicEquipped);
        Assert.DoesNotContain(s.Player.Sent, m => m.StartsWith("CHP"));
    }

    [Fact]
    public void Execute_ColourOutOfRange_SendsMessage()
    {
        var s = Setup();

        new EquipGraphicCommand().Execute(s.Ctx, "weapon", 45, 9, 256, 7, 6);

        Assert.Contains(s.Player.Sent, m => m.Contains("invalid g value"));
        Assert.Equal(101, Equipped(s).GraphicEquipped);
    }

    [Fact]
    public void Execute_SheetWithoutRgba_SendsUsage()
    {
        var s = Setup();

        new EquipGraphicCommand().Execute(s.Ctx, "weapon", 45, null, null, null, null, 12, 34);

        Assert.Contains(s.Player.Sent, m => m.Contains("Usage: /equipgraphic"));
        Assert.Equal(3, Equipped(s).GraphicFile);
    }

    [Fact]
    public void Execute_SheetWithoutTile_SendsUsage()
    {
        var s = Setup();

        new EquipGraphicCommand().Execute(s.Ctx, "weapon", 45, 9, 8, 7, 6, 12);

        Assert.Contains(s.Player.Sent, m => m.Contains("Usage: /equipgraphic"));
        Assert.Equal(3, Equipped(s).GraphicFile);
    }

    [Fact]
    public void Execute_NegativeTile_SendsMessage()
    {
        var s = Setup();

        new EquipGraphicCommand().Execute(s.Ctx, "weapon", 45, 0, 0, 0, 0, -1, 5);

        Assert.Contains(s.Player.Sent, m => m.Contains("sheet and tile must be 0 or more"));
        Assert.Equal(3, Equipped(s).GraphicFile);
        Assert.Equal(7, Equipped(s).GraphicTile);
    }

    [Fact]
    public void Dispatch_GraphicOnly_Runs()
    {
        var s = Setup();

        Assert.True(s.World.RunCommand(s.Player, "/equipgraphic weapon 45"));

        Item item = Equipped(s);
        Assert.Equal(45, item.GraphicEquipped);
        Assert.Equal((1, 4, 3, 7), (item.GraphicR, item.GraphicA, item.GraphicFile, item.GraphicTile));
    }

    [Fact]
    public void Dispatch_ColourAndTile_BindInOrder()
    {
        var s = Setup();

        Assert.True(s.World.RunCommand(s.Player, "/equipgraphic weapon 45 9 8 7 6 12 34"));

        Item item = Equipped(s);
        Assert.Equal((45, 9, 8, 7, 6, 12, 34),
            (item.GraphicEquipped, item.GraphicR, item.GraphicG, item.GraphicB, item.GraphicA,
             item.GraphicFile, item.GraphicTile));
    }

    [Fact]
    public void Dispatch_MissingGraphic_SendsUsage()
    {
        var s = Setup();

        Assert.True(s.World.RunCommand(s.Player, "/equipgraphic weapon"));

        Assert.Contains(s.Player.Sent, m => m.Contains("Usage: /equipgraphic <slot> <id> [r g b a] [sheet tile]"));
        Assert.Equal(101, Equipped(s).GraphicEquipped);
    }

    [Fact]
    public void Dispatch_NormalPlayer_Swallowed()
    {
        var s = Setup(Player.AccessStatus.Normal);

        Assert.True(s.World.RunCommand(s.Player, "/equipgraphic weapon 45"));

        Assert.Empty(s.Player.Sent);
        Assert.Equal(101, Equipped(s).GraphicEquipped);
    }

    [Fact]
    public void ChangeEquipGraphic_IsAppendedAndGmOnly()
    {
        Assert.Equal(36, (int)AccessPrivilege.ChangeEquipGraphic);

        foreach (Player.AccessStatus access in Enum.GetValues<Player.AccessStatus>())
        {
            var player = new Player(0) { Access = access };
            Assert.Equal(access == Player.AccessStatus.GameMaster,
                AccessLevels.HasPrivilege(player, AccessPrivilege.ChangeEquipGraphic));
        }
    }
}
