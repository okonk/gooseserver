using Goose;
using Goose.Events;
using Goose.Tests.Fixtures;
using Xunit;

namespace Goose.Tests;

public class BackpackEquipTests
{
    private static ItemTemplate BackpackTemplate(int id, int slots = 10) =>
        new ItemTemplate
        {
            ID = id, Name = "Backpack", Description = "A backpack", Value = 10,
            BaseStats = new AttributeSet(), StackSize = 1, ScriptParams = "",
            Slot = ItemTemplate.ItemSlots.Backpack, UseType = ItemTemplate.UseTypes.Armor,
            Properties = new PropertiesDictionary { ["backpackSlots"] = slots },
        };

    private static ItemTemplate SwordTemplate(int id) =>
        new ItemTemplate
        {
            ID = id, Name = "Sword", Description = "A sword", Value = 10,
            BaseStats = new AttributeSet(), StackSize = 1, ScriptParams = "",
            Slot = ItemTemplate.ItemSlots.OneHanded,
        };

    private static Item MakeItem(VendorFixture fixture, ItemTemplate template)
    {
        var item = new Item();
        item.LoadFromTemplate(template);
        fixture.World.ItemHandler.AddAndAssignId(item, fixture.World);
        return item;
    }

    private static void EquipBackpack(VendorFixture fixture, Item backpack)
    {
        Assert.True(fixture.Player.Inventory.AddItem(backpack, 1, fixture.World));
        Assert.True(fixture.Player.Inventory.Equip(backpack, fixture.World));
    }

    private static void PutInBackpack(VendorFixture fixture, Item item)
    {
        fixture.Player.Inventory.GetBackpackContainer().SetSlot(1, new ItemSlot { Item = item, Stack = 1 });
    }

    private static Item MakeBackpack(VendorFixture fixture, int id) =>
        MakeItem(fixture, BackpackTemplate(id));

    [Fact]
    public void Equipping_a_backpack_sizes_the_container_from_the_item()
    {
        using var fixture = new VendorFixture();
        var backpack = MakeBackpack(fixture, 100);
        EquipBackpack(fixture, backpack);

        Assert.Same(backpack, fixture.Player.Inventory.GetEquippedSlot(Inventory.EquipSlots.Backpack)?.Item);
        Assert.Equal(11, fixture.Player.Inventory.GetBackpackContainer().MaxSlots);
        Assert.EndsWith("Holds 10 items.", backpack.Description);
    }

    [Fact]
    public void Unequip_is_refused_while_the_backpack_holds_items()
    {
        using var fixture = new VendorFixture();
        var backpack = MakeBackpack(fixture, 100);
        EquipBackpack(fixture, backpack);
        PutInBackpack(fixture, MakeItem(fixture, SwordTemplate(200)));

        Assert.False(fixture.Player.Inventory.Unequip(Inventory.EquipSlots.Backpack, fixture.World));

        Assert.Contains(fixture.Player.Sent, m => m.Contains("Empty your backpack first."));
        Assert.Same(backpack, fixture.Player.Inventory.GetEquippedSlot(Inventory.EquipSlots.Backpack)?.Item);
        foreach (var slot in fixture.Player.Inventory.GetInventorySlots())
        {
            Assert.NotSame(backpack, slot?.Item);
        }
    }

    [Fact]
    public void Equipping_a_second_backpack_is_refused_while_the_first_holds_items()
    {
        using var fixture = new VendorFixture();
        var first = MakeBackpack(fixture, 100);
        EquipBackpack(fixture, first);
        var sword = MakeItem(fixture, SwordTemplate(200));
        PutInBackpack(fixture, sword);

        var second = MakeBackpack(fixture, 101);
        Assert.True(fixture.Player.Inventory.AddItem(second, 1, fixture.World));

        Assert.False(fixture.Player.Inventory.Equip(second, fixture.World));

        Assert.Contains(fixture.Player.Inventory.GetInventorySlots(), s => ReferenceEquals(s?.Item, second));
        Assert.Same(first, fixture.Player.Inventory.GetEquippedSlot(Inventory.EquipSlots.Backpack)?.Item);
        Assert.Same(sword, fixture.Player.Inventory.GetBackpackContainer().GetSlot(1)?.Item);
    }

    [Fact]
    public void Destroying_an_equipped_non_empty_backpack_is_refused()
    {
        using var fixture = new VendorFixture();
        var backpack = MakeBackpack(fixture, 100);
        EquipBackpack(fixture, backpack);
        var sword = MakeItem(fixture, SwordTemplate(200));
        PutInBackpack(fixture, sword);

        // Equipped client slot ids are InventorySize + (int)EquipSlot + 1
        // (see Inventory.Unequip(int, GameWorld)).
        int slotId = fixture.Settings.InventorySize + (int)Inventory.EquipSlots.Backpack + 1;
        var ev = new DestroyItemEvent
        {
            Player = fixture.Player,
            Data = $"DITM{slotId}",
        };
        ev.Ready(fixture.World);

        Assert.Same(backpack, fixture.Player.Inventory.GetEquippedSlot(Inventory.EquipSlots.Backpack)?.Item);
        Assert.Same(sword, fixture.Player.Inventory.GetBackpackContainer().GetSlot(1)?.Item);
        foreach (var slot in fixture.Player.Inventory.GetInventorySlots())
        {
            Assert.NotSame(backpack, slot?.Item);
        }
    }

    [Fact]
    public void Equipping_a_second_backpack_swaps_when_the_first_is_empty()
    {
        using var fixture = new VendorFixture();
        var first = MakeBackpack(fixture, 100);
        EquipBackpack(fixture, first);
        var second = MakeItem(fixture, BackpackTemplate(101, slots: 5));
        Assert.True(fixture.Player.Inventory.AddItem(second, 1, fixture.World));

        Assert.True(fixture.Player.Inventory.Equip(second, fixture.World));

        Assert.Same(second, fixture.Player.Inventory.GetEquippedSlot(Inventory.EquipSlots.Backpack)?.Item);
        Assert.Contains(fixture.Player.Inventory.GetInventorySlots(), s => ReferenceEquals(s?.Item, first));
        Assert.Equal(6, fixture.Player.Inventory.GetBackpackContainer().MaxSlots);
    }

    [Fact]
    public void Unequipping_an_empty_backpack_returns_it_to_inventory()
    {
        using var fixture = new VendorFixture();
        var backpack = MakeBackpack(fixture, 100);
        EquipBackpack(fixture, backpack);

        Assert.True(fixture.Player.Inventory.Unequip(Inventory.EquipSlots.Backpack, fixture.World));

        Assert.Null(fixture.Player.Inventory.GetEquippedSlot(Inventory.EquipSlots.Backpack));
        Assert.Contains(fixture.Player.Inventory.GetInventorySlots(), s => ReferenceEquals(s?.Item, backpack));
        Assert.Equal(1, fixture.Player.Inventory.GetBackpackContainer().MaxSlots);
    }

    [Fact]
    public void Equipping_a_zero_slot_backpack_succeeds_with_a_single_slot_container()
    {
        using var fixture = new VendorFixture();
        var template = new ItemTemplate
        {
            ID = 100, Name = "Backpack", Description = "A backpack", Value = 10,
            BaseStats = new AttributeSet(), StackSize = 1, ScriptParams = "",
            Slot = ItemTemplate.ItemSlots.Backpack, UseType = ItemTemplate.UseTypes.Armor,
        };
        var backpack = MakeItem(fixture, template);
        EquipBackpack(fixture, backpack);

        Assert.Same(backpack, fixture.Player.Inventory.GetEquippedSlot(Inventory.EquipSlots.Backpack)?.Item);
        Assert.Equal(1, fixture.Player.Inventory.GetBackpackContainer().MaxSlots);
        Assert.EndsWith("Holds 0 items.", backpack.Description);
    }

    [Fact]
    public void Lore_item_in_backpack_blocks_picking_up_another()
    {
        using var fixture = new VendorFixture();
        var backpack = MakeBackpack(fixture, 100);
        EquipBackpack(fixture, backpack);

        var loreTemplate = new ItemTemplate
        {
            ID = 300, Name = "LoreSword", Description = "A lore sword", Value = 100,
            BaseStats = new AttributeSet(), StackSize = 1, ScriptParams = "",
            Slot = ItemTemplate.ItemSlots.OneHanded, IsLore = true,
        };
        PutInBackpack(fixture, MakeItem(fixture, loreTemplate));

        fixture.Player.Bank = new PlayerBank();
        Assert.True(fixture.Player.HasItem(300));

        fixture.Stock(1, loreTemplate);
        var ev = new VendorPurchaseInventoryEvent
        {
            Player = fixture.Player,
            Data = "VPI" + fixture.Vendor.LoginID + ",1",
        };
        ev.Ready(fixture.World);

        foreach (var slot in fixture.Player.Inventory.GetInventorySlots())
        {
            Assert.NotEqual(300, slot?.Item.TemplateID);
        }
        Assert.Contains(fixture.Player.Sent, m => m.Contains("already have this item"));
    }

    [Fact]
    public void Equip_is_refused_while_backpack_contents_are_preserved()
    {
        using var fixture = new VendorFixture();
        fixture.Player.Inventory.PreserveBackpackJsonForTest("[{\"Item\":{\"TemplateID\":1}}]");
        var backpack = MakeBackpack(fixture, 100);
        Assert.True(fixture.Player.Inventory.AddItem(backpack, 1, fixture.World));

        Assert.False(fixture.Player.Inventory.Equip(backpack, fixture.World));

        Assert.Contains(fixture.Player.Sent, m => m.Contains("need GM attention before you can equip a backpack."));
        Assert.Contains(fixture.Player.Inventory.GetInventorySlots(), s => ReferenceEquals(s?.Item, backpack));
        Assert.Null(fixture.Player.Inventory.GetEquippedSlot(Inventory.EquipSlots.Backpack));
    }

    [Fact]
    public void Backpack_cannot_be_equipped_when_EquippedSize_is_14()
    {
        using var fixture = new VendorFixture();
        fixture.Settings.EquippedSize = 14;
        var backpack = MakeBackpack(fixture, 100);
        Assert.True(fixture.Player.Inventory.AddItem(backpack, 1, fixture.World));

        Assert.False(fixture.Player.Inventory.Equip(backpack, fixture.World));

        Assert.Null(fixture.Player.Inventory.GetEquippedSlot(Inventory.EquipSlots.Backpack));
        Assert.Contains(fixture.Player.Inventory.GetInventorySlots(), s => ReferenceEquals(s?.Item, backpack));
    }

    [Fact]
    public void Unequip_backpack_is_a_successful_noop_when_there_is_no_backpack_slot()
    {
        using var fixture = new VendorFixture();
        fixture.Settings.EquippedSize = 14;
        fixture.Player.Inventory = new Inventory(fixture.Player, fixture.Settings);

        Assert.True(fixture.Player.Inventory.Unequip(Inventory.EquipSlots.Backpack, fixture.World));
    }
}
