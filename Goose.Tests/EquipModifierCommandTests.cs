using Goose;
using Goose.Commands;
using Goose.Testing;
using Xunit;
using static Goose.Inventory;

namespace Goose.Tests;

public class EquipModifierCommandTests
{
    private sealed record SetupResult(TestWorldFixture World, TestWorldFixture.CapturingPlayer Player, CommandContext Ctx);

    private static SetupResult Setup(Player.AccessStatus access = Player.AccessStatus.GameMaster)
    {
        var world = new TestWorldFixture();
        var map = world.AddBaseMap(1, "Town");
        var player = world.CommandPlayerOn(map, 5, 5, "Gm");
        player.Access = access;

        world.World.ItemHandler.AddTitle(new ItemModifier { Id = 10, Name = "Legendary" });
        world.World.ItemHandler.AddTitle(new ItemModifier { Id = 11, Name = "Stunted" });
        world.World.ItemHandler.AddSurname(new ItemModifier { Id = 20, Name = "of the Bear" });
        world.World.ItemHandler.AddSurname(new ItemModifier { Id = 21, Name = "of the Wolf" });

        var template = world.AddBaseItemTemplate(200, "Sword", ItemTemplate.UseTypes.Weapon);
        var item = new Item();
        item.LoadFromTemplate(template);
        world.World.ItemHandler.AddAndAssignId(item, world.World);
        Assert.True(player.Inventory.AddItem(item, 1, world.World));
        Assert.True(player.Inventory.Equip(item, world.World));
        player.Sent.Clear();

        var ctx = new CommandContext(player, world.World, new CommandRegistry(), [], "");
        return new SetupResult(world, player, ctx);
    }

    private static Item Equipped(SetupResult s) => s.Player.Inventory.GetEquippedSlot(EquipSlots.Weapon)!.Item;

    [Fact]
    public void Execute_TitleAndSurname_BuildName()
    {
        var s = Setup();

        new EquipTitleCommand().Execute(s.Ctx, "weapon", 10);
        new EquipSurnameCommand().Execute(s.Ctx, "weapon", 20);

        Item item = Equipped(s);
        Assert.Equal("Legendary Sword of the Bear", item.Name);
        Assert.Equal(10, item.GetProperty<int>(ItemProperty.TitleId));
        Assert.Equal(20, item.GetProperty<int>(ItemProperty.SurnameId));
    }

    [Fact]
    public void Execute_Replace_SwapsOnlyThatModifier()
    {
        var s = Setup();
        new EquipTitleCommand().Execute(s.Ctx, "weapon", 10);
        new EquipSurnameCommand().Execute(s.Ctx, "weapon", 20);

        new EquipTitleCommand().Execute(s.Ctx, "weapon", 11);
        new EquipSurnameCommand().Execute(s.Ctx, "weapon", 21);

        Assert.Equal("Stunted Sword of the Wolf", Equipped(s).Name);
    }

    [Fact]
    public void Execute_Zero_ClearsModifier()
    {
        var s = Setup();
        new EquipTitleCommand().Execute(s.Ctx, "weapon", 10);
        new EquipSurnameCommand().Execute(s.Ctx, "weapon", 20);

        new EquipTitleCommand().Execute(s.Ctx, "weapon", 0);

        Item item = Equipped(s);
        Assert.Equal("Sword of the Bear", item.Name);
        Assert.False(item.HasProperty(ItemProperty.TitleId));
        Assert.True(item.HasProperty(ItemProperty.SurnameId));
    }

    [Fact]
    public void Execute_CustomName_IsKept()
    {
        var s = Setup();
        Equipped(s).Name = "Excalibur";

        new EquipSurnameCommand().Execute(s.Ctx, "weapon", 20);
        new EquipSurnameCommand().Execute(s.Ctx, "weapon", 21);

        Assert.Equal("Excalibur of the Wolf", Equipped(s).Name);
    }

    [Fact]
    public void Execute_UnknownId_LeavesItemUnchanged()
    {
        var s = Setup();

        new EquipTitleCommand().Execute(s.Ctx, "weapon", 999);

        Assert.Equal("Sword", Equipped(s).Name);
        Assert.False(Equipped(s).HasProperty(ItemProperty.TitleId));
        Assert.Contains(s.Player.Sent, m => m.Contains("No item title with id 999"));
    }

    [Fact]
    public void Execute_EmptySlot_Reports()
    {
        var s = Setup();

        new EquipTitleCommand().Execute(s.Ctx, "head", 10);

        Assert.Contains(s.Player.Sent, m => m.Contains("nothing equipped"));
    }

    [Fact]
    public void Dispatch_NormalPlayer_Swallowed()
    {
        var s = Setup(Player.AccessStatus.Normal);

        Assert.True(s.World.RunCommand(s.Player, "/equiptitle weapon 10"));

        Assert.Empty(s.Player.Sent);
        Assert.Equal("Sword", Equipped(s).Name);
    }

    [Fact]
    public void Dispatch_Gm_SetsSurname()
    {
        var s = Setup();

        Assert.True(s.World.RunCommand(s.Player, "/equipsurname weapon 20"));

        Assert.Equal("Sword of the Bear", Equipped(s).Name);
    }

    [Fact]
    public void ChangeEquipModifier_IsAppendedAndGmOnly()
    {
        Assert.Equal(39, (int)AccessPrivilege.ChangeEquipModifier);

        foreach (Player.AccessStatus access in Enum.GetValues<Player.AccessStatus>())
        {
            var player = new Player(0) { Access = access };
            Assert.Equal(access == Player.AccessStatus.GameMaster,
                AccessLevels.HasPrivilege(player, AccessPrivilege.ChangeEquipModifier));
        }
    }
}
