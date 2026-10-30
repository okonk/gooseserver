using Goose;
using Goose.Events;
using Goose.Logs;
using Goose.Tests.Fixtures;
using Xunit;

namespace Goose.Tests;

public class BackpackWindowTests
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

    private static Item MakeBackpack(VendorFixture fixture, int id) =>
        MakeItem(fixture, BackpackTemplate(id));

    private static void EquipBackpack(VendorFixture fixture, Item backpack)
    {
        Assert.True(fixture.Player.Inventory.AddItem(backpack, 1, fixture.World));
        Assert.True(fixture.Player.Inventory.Equip(backpack, fixture.World));
    }

    private static BackpackWindow GetBackpackWindow(VendorFixture fixture) =>
        fixture.Player.Windows.OfType<BackpackWindow>().Single();

    [Fact]
    public void Open_sends_MKW_with_slot_count_and_columns()
    {
        using var fixture = new VendorFixture();
        EquipBackpack(fixture, MakeBackpack(fixture, 100));
        GetBackpackWindow(fixture).Close(fixture.Player, fixture.World);

        fixture.Player.Sent.Clear();
        BackpackWindow.Open(fixture.World, fixture.Player);

        var win = GetBackpackWindow(fixture);
        Assert.Contains(fixture.Player.Sent, m => m.StartsWith($"MKW{win.ID},32,Backpack,0,1,0,0,0,0,10,5"));
        Assert.Same(fixture.Player.Inventory.GetBackpackContainer(), win.ItemContainer);
    }

    [Fact]
    public void OBP_twice_opens_then_closes()
    {
        using var fixture = new VendorFixture();
        EquipBackpack(fixture, MakeBackpack(fixture, 100));
        GetBackpackWindow(fixture).Close(fixture.Player, fixture.World);

        fixture.Player.Sent.Clear();
        Assert.True(fixture.World.EventHandler.AddEvent(fixture.Player, "OBP"));
        fixture.World.EventHandler.Update(fixture.World);
        var win = GetBackpackWindow(fixture);
        Assert.Contains(fixture.Player.Sent, m => m.StartsWith($"MKW{win.ID}"));

        fixture.Player.Sent.Clear();
        Assert.True(fixture.World.EventHandler.AddEvent(fixture.Player, "OBP"));
        fixture.World.EventHandler.Update(fixture.World);

        Assert.Contains(fixture.Player.Sent, m => m.TrimEnd('\x1') == $"CLW{win.ID}");
        Assert.Empty(fixture.Player.Windows.OfType<BackpackWindow>());
    }

    [Fact]
    public void OBP_without_a_backpack_sends_a_notice_and_opens_nothing()
    {
        using var fixture = new VendorFixture();

        fixture.Player.Sent.Clear();
        Assert.True(fixture.World.EventHandler.AddEvent(fixture.Player, "OBP"));
        fixture.World.EventHandler.Update(fixture.World);

        Assert.Contains(fixture.Player.Sent, m => m.Contains("You don't have a backpack equipped."));
        Assert.DoesNotContain(fixture.Player.Sent, m => m.StartsWith("MKW"));
        Assert.Empty(fixture.Player.Windows.OfType<BackpackWindow>());
    }

    [Fact]
    public void Dragging_inventory_to_backpack_and_back_moves_the_item()
    {
        using var fixture = new VendorFixture();
        EquipBackpack(fixture, MakeBackpack(fixture, 100));
        var win = GetBackpackWindow(fixture);
        var sword = MakeItem(fixture, SwordTemplate(200));
        fixture.Player.Inventory.SetSlot(1, new ItemSlot { Item = sword, Stack = 1 });

        Assert.True(fixture.World.EventHandler.AddEvent(fixture.Player, $"ITW1,{win.ID},1"));
        fixture.World.EventHandler.Update(fixture.World);

        Assert.Null(fixture.Player.Inventory.GetSlot(1));
        Assert.Same(sword, fixture.Player.Inventory.GetBackpackContainer().GetSlot(1)?.Item);

        Assert.True(fixture.World.EventHandler.AddEvent(fixture.Player, $"WTI{win.ID},1,1"));
        fixture.World.EventHandler.Update(fixture.World);

        Assert.Same(sword, fixture.Player.Inventory.GetSlot(1)?.Item);
        Assert.Null(fixture.Player.Inventory.GetBackpackContainer().GetSlot(1));
    }

    [Fact]
    public void Dragging_to_slot_past_backpack_size_is_ignored()
    {
        using var fixture = new VendorFixture();
        EquipBackpack(fixture, MakeBackpack(fixture, 100));
        var win = GetBackpackWindow(fixture);
        var sword = MakeItem(fixture, SwordTemplate(200));
        fixture.Player.Inventory.SetSlot(1, new ItemSlot { Item = sword, Stack = 1 });

        fixture.Player.Sent.Clear();
        Assert.True(fixture.World.EventHandler.AddEvent(fixture.Player, $"ITW1,{win.ID},11"));
        fixture.World.EventHandler.Update(fixture.World);

        Assert.Same(sword, fixture.Player.Inventory.GetSlot(1)?.Item);
        Assert.DoesNotContain(fixture.Player.Inventory.GetBackpackContainer(), s => ReferenceEquals(s?.Item, sword));
    }

    [Fact]
    public void Equipping_a_backpack_opens_its_window()
    {
        using var fixture = new VendorFixture();
        var backpack = MakeBackpack(fixture, 100);
        Assert.True(fixture.Player.Inventory.AddItem(backpack, 1, fixture.World));
        Assert.True(fixture.Player.Inventory.Equip(backpack, fixture.World));

        var win = GetBackpackWindow(fixture);
        Assert.Same(fixture.Player.Inventory.GetBackpackContainer(), win.ItemContainer);
        Assert.Contains(fixture.Player.Sent, m => m.StartsWith($"MKW{win.ID},32,Backpack,0,1,0,0,0,0,10,5"));
    }

    [Fact]
    public void Unequipping_closes_the_window_and_drags_to_its_old_id_do_nothing()
    {
        using var fixture = new VendorFixture();
        var backpack = MakeBackpack(fixture, 100);
        Assert.True(fixture.Player.Inventory.AddItem(backpack, 1, fixture.World));
        Assert.True(fixture.Player.Inventory.Equip(backpack, fixture.World));
        var win = GetBackpackWindow(fixture);

        Assert.True(fixture.Player.Inventory.Unequip(Inventory.EquipSlots.Backpack, fixture.World));

        Assert.Contains(fixture.Player.Sent, m => m.TrimEnd('\x1') == $"CLW{win.ID}");
        Assert.Empty(fixture.Player.Windows.OfType<BackpackWindow>());

        var sword = MakeItem(fixture, SwordTemplate(200));
        fixture.Player.Inventory.SetSlot(1, new ItemSlot { Item = sword, Stack = 1 });
        fixture.Player.Sent.Clear();
        Assert.True(fixture.World.EventHandler.AddEvent(fixture.Player, $"WTI{win.ID},1,1"));
        fixture.World.EventHandler.Update(fixture.World);

        Assert.Same(sword, fixture.Player.Inventory.GetSlot(1)?.Item);
        Assert.DoesNotContain(fixture.Player.Sent, m => m.StartsWith("GWS") || m.StartsWith("GWC"));
    }

    [Fact]
    public void WindowButtonClick_close_removes_the_window_without_a_clw_echo()
    {
        using var fixture = new VendorFixture();
        EquipBackpack(fixture, MakeBackpack(fixture, 100));
        var win = GetBackpackWindow(fixture);

        fixture.Player.Sent.Clear();
        new WindowButtonClickEvent { Player = fixture.Player, Data = $"WBC2,{win.ID},0,0,0" }.Ready(fixture.World);

        Assert.Empty(fixture.Player.Windows.OfType<BackpackWindow>());
        Assert.DoesNotContain(fixture.Player.Sent, m => m.StartsWith("CLW"));
    }

    [Fact]
    public void Equipping_a_second_backpack_closes_the_old_window_and_opens_a_new_one()
    {
        using var fixture = new VendorFixture();
        var first = MakeBackpack(fixture, 100);
        Assert.True(fixture.Player.Inventory.AddItem(first, 1, fixture.World));
        Assert.True(fixture.Player.Inventory.Equip(first, fixture.World));
        var old = GetBackpackWindow(fixture);

        var second = MakeBackpack(fixture, 101);
        Assert.True(fixture.Player.Inventory.AddItem(second, 1, fixture.World));
        Assert.True(fixture.Player.Inventory.Equip(second, fixture.World));

        var fresh = GetBackpackWindow(fixture);
        Assert.NotEqual(old.ID, fresh.ID);
        Assert.Contains(fixture.Player.Sent, m => m.TrimEnd(LogProtocolPackets.PacketDelimiter) == $"CLW{old.ID}");
        Assert.Contains(fixture.Player.Sent, m => m.StartsWith($"MKW{fresh.ID},32,Backpack,"));
    }
}
