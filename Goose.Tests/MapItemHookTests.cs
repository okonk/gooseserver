using Goose.Events;
using Goose.Scripting;
using Goose.Testing;
using Xunit;

namespace Goose.Tests;

public class MapItemHookTests
{
    private sealed class RecordingMapScript : BaseMapScript
    {
        public string? DropRefusal;
        public string? PickupRefusal;
        public bool DropGateThrows;
        public int DropGateCalls;
        public int PickupGateCalls;
        public int DroppedCalls;
        public ItemTile? DroppedTile;

        public override string? CanPlayerDropItem(Map map, Player player, Item item, long stack, GameWorld world)
        {
            this.DropGateCalls++;
            if (this.DropGateThrows) throw new InvalidOperationException("boom");
            return this.DropRefusal;
        }

        public override void OnPlayerDroppedItem(Map map, Player player, ItemTile tile, GameWorld world)
        {
            this.DroppedCalls++;
            this.DroppedTile = tile;
        }

        public override string? CanPlayerPickupItem(Map map, Player player, ItemTile tile, GameWorld world)
        {
            this.PickupGateCalls++;
            return this.PickupRefusal;
        }
    }

    private sealed class Harness : IDisposable
    {
        public TestWorldFixture Fixture = null!;
        public Map Map = null!;
        public TestWorldFixture.CapturingPlayer Player = null!;
        public RecordingMapScript Script = null!;

        public void Dispose() => this.Fixture.Dispose();
    }

    private static Harness Setup()
    {
        var fixture = new TestWorldFixture(s => s.ItemIDStartpoint = 5002);
        // Consume the gold item's id (ItemIDStartpoint) so test items never hit the gold branch.
        var gold = new Item();
        fixture.World.ItemHandler.AddAndAssignId(gold, fixture.World);
        var map = fixture.AddBaseMap(1, "Test", 10, 10);
        var script = new RecordingMapScript();
        map.Script = ScriptStub.For<IMapScript>(script);
        var player = fixture.CommandPlayerOn(map, 5, 5);
        return new Harness { Fixture = fixture, Map = map, Player = player, Script = script };
    }

    private static Item NewItem(Harness h, int templateId = 1, int stackSize = 10)
    {
        var template = h.Fixture.AddBaseItemTemplate(templateId, "Candle", ItemTemplate.UseTypes.NoUse,
            t => t.StackSize = stackSize);
        var item = new Item();
        item.LoadFromTemplate(template);
        h.Fixture.World.ItemHandler.AddAndAssignId(item, h.Fixture.World);
        return item;
    }

    private static int SlotOf(Harness h, Item item)
    {
        var slots = h.Player.Inventory.GetInventorySlots();
        for (int i = 1; i < slots.Length; i++)
            if (slots[i] is not null && slots[i]!.Item == item) return i;
        Assert.Fail("item not in inventory");
        return 0;
    }

    private static void Drop(Harness h, Item item, int stack = 1)
    {
        new PlayerDropItemEvent { Player = h.Player, Data = $"DRP{SlotOf(h, item)},{stack}" }.Ready(h.Fixture.World);
    }

    private static void Pickup(Harness h)
    {
        new PickupItemEvent { Player = h.Player }.Ready(h.Fixture.World);
    }

    private static ItemTile? FindTile(Harness h, Item item)
    {
        foreach (var t in h.Map.tiles)
            if (t is ItemTile it && it.ItemSlot.Item == item) return it;
        return null;
    }

    [Fact]
    public void Refused_drop_keeps_item_in_inventory_and_sends_message()
    {
        using var h = Setup();
        h.Script.DropRefusal = "No dropping here.";
        var item = NewItem(h);
        Assert.True(h.Player.Inventory.AddItem(item, 1, h.Fixture.World));
        int slot = SlotOf(h, item);

        Drop(h, item);

        Assert.Contains(h.Player.Sent, m => m.Contains("NTF0,No dropping here."));
        Assert.Same(item, h.Player.Inventory.GetSlot(slot)!.Item);
        Assert.Null(FindTile(h, item));
        Assert.Equal(1, h.Script.DropGateCalls);
        Assert.Equal(0, h.Script.DroppedCalls);
    }

    [Fact]
    public void Dropped_item_hook_receives_final_tile_coordinates()
    {
        using var h = Setup();
        var blocker = NewItem(h, templateId: 2, stackSize: 1);
        h.Map.SetTile(5, 5, new ItemTile
        {
            X = 5, Y = 5,
            ItemSlot = new ItemSlot { Item = blocker, Stack = 1 },
        });
        var item = NewItem(h);
        Assert.True(h.Player.Inventory.AddItem(item, 1, h.Fixture.World));

        Drop(h, item);

        var dropped = FindTile(h, item);
        Assert.NotNull(dropped);
        Assert.True(dropped!.X != 5 || dropped.Y != 5);
        Assert.Equal(1, h.Script.DroppedCalls);
        Assert.Equal(dropped.X, h.Script.DroppedTile!.X);
        Assert.Equal(dropped.Y, h.Script.DroppedTile.Y);
    }

    [Fact]
    public void Dropped_item_hook_receives_existing_tile_when_stacking()
    {
        using var h = Setup();
        var existing = NewItem(h);
        var tile = new ItemTile
        {
            X = 5, Y = 5,
            ItemSlot = new ItemSlot { Item = existing, Stack = 3 },
        };
        h.Map.SetTile(5, 5, tile);
        var item = NewItem(h);
        Assert.True(h.Player.Inventory.AddItem(item, 2, h.Fixture.World));

        Drop(h, item, stack: 2);

        Assert.Same(tile, h.Script.DroppedTile);
        Assert.Equal(5, tile.ItemSlot.Stack);
        Assert.Equal(1, h.Script.DroppedCalls);
    }

    [Fact]
    public void Throwing_drop_gate_fails_closed()
    {
        using var h = Setup();
        h.Script.DropGateThrows = true;
        var item = NewItem(h);
        Assert.True(h.Player.Inventory.AddItem(item, 1, h.Fixture.World));
        int slot = SlotOf(h, item);

        Drop(h, item);

        Assert.Contains(h.Player.Sent, m => m.Contains("NTF0,You cannot drop that here."));
        Assert.Same(item, h.Player.Inventory.GetSlot(slot)!.Item);
        Assert.Null(FindTile(h, item));
        Assert.Equal(0, h.Script.DroppedCalls);
    }

    [Fact]
    public void Refused_pickup_leaves_tile_on_map()
    {
        using var h = Setup();
        h.Script.PickupRefusal = "Not now.";
        var item = NewItem(h);
        var tile = new ItemTile
        {
            X = 5, Y = 5,
            ItemSlot = new ItemSlot { Item = item, Stack = 1 },
            PickupTime = 0,
        };
        h.Map.SetTile(5, 5, tile);

        Pickup(h);

        Assert.Contains(h.Player.Sent, m => m.Contains("NTF0,Not now."));
        Assert.Same(tile, h.Map.GetTile(5, 5));
        Assert.DoesNotContain(h.Player.Inventory.GetInventorySlots(), s => s is not null);
        Assert.Equal(1, h.Script.PickupGateCalls);
    }

    [Fact]
    public void Pickup_gate_runs_for_gold()
    {
        using var h = Setup();
        var gold = new Item { ItemID = h.Fixture.Settings.ItemIDStartpoint + h.Fixture.Settings.GoldItemID };
        var tile = new ItemTile
        {
            X = 5, Y = 5,
            ItemSlot = new ItemSlot { Item = gold, Stack = 100 },
            PickupTime = 0,
        };
        h.Map.SetTile(5, 5, tile);

        Pickup(h);

        Assert.Equal(1, h.Script.PickupGateCalls);
        Assert.Equal(100, h.Player.Gold);
        Assert.Null(h.Map.GetTile(5, 5));
    }

    [Fact]
    public void No_map_script_behaves_as_before()
    {
        using var h = Setup();
        h.Map.Script = null;
        var item = NewItem(h);
        Assert.True(h.Player.Inventory.AddItem(item, 1, h.Fixture.World));
        int slot = SlotOf(h, item);

        Drop(h, item);

        var dropped = FindTile(h, item);
        Assert.NotNull(dropped);
        Assert.Null(h.Player.Inventory.GetSlot(slot));

        h.Player.MapX = dropped!.X;
        h.Player.MapY = dropped.Y;
        Pickup(h);

        Assert.Null(h.Map.GetTile(dropped.X, dropped.Y));
        Assert.Equal(1, h.Player.Inventory.GetInventorySlots().Count(s => s is not null && s.Item == item));
    }
}
