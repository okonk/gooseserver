using Goose.Events;
using Goose.Testing;
using Goose.Scripting;
using Goose.Tests.Collections;

namespace Goose.Tests;

[Collection(GameWorldSettingsCollection.Name)]
public class ItemScriptHookTests
{
    private readonly GooseSettings settings;

    public ItemScriptHookTests()
    {
        // The native surname roll is a RollChance(ItemSurnameChancePercent) call
        // (ItemHandler.cs). The shipped GooseSettings.json puts that at 0.5, which would
        // make Returning_false_leaves_the_native_rolls_running a coin flip, so pin the
        // chances: surnames always roll, titles never do. ItemIDStartpoint is pinned so a
        // freshly built Item (ItemID 0) is never mistaken for the gold item in
        // PickupItemEvent, and InventorySize so a pickup that passes its gates can land.
        settings = new GooseSettings
        {
            InventorySize = 30, EquippedSize = 20, CombineBagSize = 10, SpellbookSize = 30,
            ItemIDStartpoint = 5002,
            ItemSurnameChancePercent = 1.0,
            ItemTitleChancePercent = 0.0,
        };
    }

    private sealed class SpyScript : BaseItemScript
    {
        public bool Suppress;
        public int RollCalls;

        public override bool OnRollModifiersEvent(Item item, GameWorld world)
        {
            this.RollCalls++;
            return this.Suppress;
        }
    }

    private sealed class ThrowingPickupScript : BaseItemScript
    {
        public override string CanPickup(Player player, Item item, GameWorld world)
            => throw new InvalidOperationException("boom");
    }

    /// <summary>Player.Send is virtual so a subclass can capture what the server would
    /// have written to the socket. world.Send routes through Player.Send.</summary>
    private sealed class CapturingPlayer : Player
    {
        public List<string> Sent { get; } = new();

        public override bool Send(string data) { Sent.Add(data); return true; }
    }

    private (GameWorld World, Item Item, SpyScript Spy) Arrange()
    {
        var world = new GameWorld(settings);
        var spy = new SpyScript();
        var template = new ItemTemplate
        {
            ID = 1, Name = "Sword", Description = "",
            UseType = ItemTemplate.UseTypes.Weapon, Slot = ItemTemplate.ItemSlots.OneHanded,
            BaseStats = new AttributeSet(),
            Script = ScriptStub.For<IItemScript>(spy),
        };
        world.ItemHandler.AddTemplate(template);

        var item = new Item();
        item.LoadFromTemplate(template);
        return (world, item, spy);
    }

    [Fact]
    public void Roll_hook_runs_before_the_use_type_filter()
    {
        var (world, item, spy) = Arrange();
        // A Scroll would be filtered out by RollTitleAndSurname's early return - the hook
        // must still see it, because dimension tomes need CanPickup and the upgrade rule.
        item.Template.UseType = ItemTemplate.UseTypes.Scroll;

        world.ItemHandler.RollTitleAndSurname(item, world);

        Assert.Equal(1, spy.RollCalls);
    }

    [Fact]
    public void Returning_true_suppresses_the_native_rolls()
    {
        var (world, item, spy) = Arrange();
        spy.Suppress = true;
        world.ItemHandler.AddSurname(new ItemModifier
        {
            Id = 1, Name = "of the Bear", Chance = 1.0,
            Slot = ItemTemplate.ItemSlots.Misc, UseType = ItemTemplate.UseTypes.Weapon,
        });

        world.ItemHandler.RollTitleAndSurname(item, world);

        Assert.False(item.HasProperty(ItemProperty.SurnameId));
        Assert.Equal("Sword", item.Name);
    }

    [Fact]
    public void Returning_false_leaves_the_native_rolls_running()
    {
        var (world, item, spy) = Arrange();
        spy.Suppress = false;
        world.ItemHandler.AddSurname(new ItemModifier
        {
            Id = 1, Name = "of the Bear", Chance = 1.0,
            Slot = ItemTemplate.ItemSlots.Misc, UseType = ItemTemplate.UseTypes.Weapon,
        });

        world.ItemHandler.RollTitleAndSurname(item, world);

        Assert.True(item.HasProperty(ItemProperty.SurnameId));
        Assert.Equal("Sword of the Bear", item.Name);
    }

    [Fact]
    public void An_item_with_no_script_rolls_natively()
    {
        var world = new GameWorld(settings);
        var template = new ItemTemplate
        {
            ID = 1, Name = "Sword", Description = "",
            UseType = ItemTemplate.UseTypes.Weapon, Slot = ItemTemplate.ItemSlots.OneHanded,
            BaseStats = new AttributeSet(),
        };
        var item = new Item();
        item.LoadFromTemplate(template);

        var exception = Record.Exception(() => world.ItemHandler.RollTitleAndSurname(item, world));

        Assert.Null(exception);
    }

    [Fact]
    public void CanPickup_defaults_to_allowing()
    {
        var (world, item, _) = Arrange();
        Assert.Null(new BaseItemScript().CanPickup(new Player(0), item, world));
    }

    /// <summary>Fail closed, mirroring Map.PlayerCanJoin: a throwing CanPickup gate must
    /// refuse the pickup with a generic message rather than let the exception bubble to
    /// EventHandler's catch-all, which would silently drop the event with no feedback.
    /// Exercises PickupItemEvent.Ready end to end - the exact path with the bug.</summary>
    [Fact]
    public void A_throwing_CanPickup_script_refuses_pickup_with_a_message()
    {
        var world = new GameWorld(settings);
        var template = new ItemTemplate
        {
            ID = 1, Name = "Sword", Description = "",
            UseType = ItemTemplate.UseTypes.Weapon, Slot = ItemTemplate.ItemSlots.OneHanded,
            BaseStats = new AttributeSet(),
            Script = ScriptStub.For<IItemScript>(new ThrowingPickupScript()),
        };
        var item = new Item();
        item.LoadFromTemplate(template);

        var map = new Map { ID = 1, Name = "Test", Width = 10, Height = 10 };
        map.tiles = new ITile[(map.Width + 1) * (map.Height + 1)];
        map.SetTile(5, 5, new ItemTile
        {
            X = 5, Y = 5,
            ItemSlot = new ItemSlot { Item = item, Stack = 1 },
            PickupTime = 0, // 0 < world.TimeNow, so ownership never blocks the pickup
        });

        var player = new CapturingPlayer
        {
            State = Player.States.Ready,
            Map = map, MapID = map.ID, MapX = 5, MapY = 5,
        };
        player.Inventory = new Inventory(player, world.Configuration);

        new PickupItemEvent { Player = player }.Ready(world);

        Assert.Contains("You cannot pick that up right now.", string.Join("", player.Sent));
        Assert.DoesNotContain(player.Inventory.GetInventorySlots(), slot => slot != null && slot.Item == item);
    }
}
