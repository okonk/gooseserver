using System.Linq;
using Goose;
using Goose.IntegrationTests.Fixtures;
using Xunit;

namespace Goose.IntegrationTests;

public class DimensionResetItemTests
{
    private const int Offset = 100000;   // must match DimensionConstants.Offset

    /// <summary>DimensionItemScriptTests.Run (`:12-20`) plus the pieces a command needs: a
    /// vendor-less town map, a Ready player holding items, and spirit already registered by
    /// OnLoaded.</summary>
    private static (GlobalScriptFixture Fixture, GlobalScriptFixture.CapturingPlayer Player) Loaded(
        long spiritBalance = 10_000)
    {
        var fixture = new GlobalScriptFixture();
        fixture.AddBaseMap(1, "Town", width: 100, height: 100);
        fixture.AddBaseItemTemplate(50, "Sword", ItemTemplate.UseTypes.Weapon, t => t.Value = 100);
        // ShouldClone (Items.csx) only clones Armor/Weapon/Scroll-with-spell,
        // so a dimension's non-equipment item is its tome clone, not a potion clone.
        fixture.AddBaseSpellEffect(7, "Firestorm Effect");
        fixture.AddBaseSpell(91, "Firestorm", 7);
        fixture.AddBaseItemTemplate(70, "Tome of Firestorm", ItemTemplate.UseTypes.Scroll,
            t => { t.Value = 10; t.LearnSpellID = 91; });
        fixture.CompileShipped().Object.OnLoaded(fixture.World);

        var player = fixture.CommandPlayerOn(fixture.World.MapHandler.GetMap(1), 5, 5);
        player.Properties["dimension.max"] = 6;
        fixture.World.CurrencyHandler.Get("spirit").Add(player, spiritBalance, fixture.World);
        player.Sent.Clear();

        return (fixture, player);
    }

    private static Item Carry(GlobalScriptFixture fixture, Player player, int templateId, int stack = 1)
    {
        var item = new Item();
        item.LoadFromTemplate(fixture.World.ItemHandler.GetTemplate(templateId));
        fixture.World.ItemHandler.AddAndAssignId(item, fixture.World);
        player.Inventory.AddItem(item, stack, fixture.World);
        return item;
    }

    private static long Spirit(GlobalScriptFixture fixture, Player player)
        => fixture.World.CurrencyHandler.Get("spirit").GetBalance(player);

    // ---- the paid reroll flow --------------------------------------------

    /// <summary>A drop rolls a 45% suffix chance; a paid reroll always lands one. That
    /// asymmetry is why DimensionRolls keeps RollDrop and Reroll as separate entry
    /// points. 200 rerolls of the same item against a 45% chance makes a false pass
    /// vanishingly unlikely - each reroll strips the previous suffix first, so a reroll
    /// that failed to land would surface as a missing SurnameId.</summary>
    [Fact]
    public void Reroll_always_lands_a_suffix()
    {
        var (fixture, player) = Loaded();
        using var _ = fixture;

        // 27 spirit each at dimension 3; 200 rerolls fit the 10,000 starting balance.
        Carry(fixture, player, 50 + Offset * 3);

        for (int i = 0; i < 200; i++)
        {
            Assert.True(fixture.RunCommand(player, "/resetitem 1"));

            var item = player.Inventory.GetSlot(1).Item;
            Assert.True(item.HasProperty(ItemProperty.SurnameId), $"reroll {i} landed no suffix");
        }
    }

    /// <summary>ResetModifiers runs first, so a second reroll replaces rather than appends.
    /// The name is the visible symptom; the stats are the one that matters.</summary>
    [Fact]
    public void Reroll_clears_the_previous_suffix_rather_than_stacking()
    {
        var (fixture, player) = Loaded();
        using var _ = fixture;

        Carry(fixture, player, 50 + Offset * 3);

        for (int i = 0; i < 20; i++)
        {
            Assert.True(fixture.RunCommand(player, "/resetitem 1"));

            var item = player.Inventory.GetSlot(1).Item;

            // One base name plus exactly one " of the ..." suffix, never two.
            // The rarity title (2% Legendary / 2% Stunted) may legally precede the base
            // name, so Contains, not StartsWith. The anti-stacking property is the
            // exactly-one-suffix count below.
            Assert.Contains(item.Template.Name, item.Name);
            Assert.Equal(1, item.Name.Split(" of ").Length - 1);
        }
    }

    // ---- the command ----------------------------------------------------

    [Fact]
    public void Charges_three_to_the_dimension()
    {
        foreach (var (dim, cost) in new[] { (1, 3L), (3, 27L), (6, 729L) })
        {
            var (fixture, player) = Loaded();
            using var _ = fixture;
            Carry(fixture, player, 50 + Offset * dim);
            var before = Spirit(fixture, player);

            Assert.True(fixture.RunCommand(player, "/resetitem 1"));

            Assert.Equal(before - cost, Spirit(fixture, player));
        }
    }

    /// <summary>Every refusal, in one table, all asserting the same thing: the balance did
    /// not move and the item did not change. Part 5 established that a Remove call is not
    /// itself a guard, so "refusals charge nothing" is the property under test, not a
    /// nicety.</summary>
    [Theory]
    // slot parsing
    [InlineData("/resetitem ", "empty argument")]
    [InlineData("/resetitem abc", "unparseable slot")]
    [InlineData("/resetitem 0", "slot below range")]
    [InlineData("/resetitem 999", "slot above InventorySize")]
    [InlineData("/resetitem 2", "empty slot")]
    public void Refuses_bad_slots_and_charges_nothing(string command, string why)
    {
        var (fixture, player) = Loaded();
        using var _ = fixture;
        Carry(fixture, player, 50 + Offset * 2);
        var before = Spirit(fixture, player);

        fixture.RunCommand(player, command);

        var item = player.Inventory.GetSlot(1).Item;
        Assert.Equal(before, Spirit(fixture, player));
        // No reroll ran, so the item still carries its clone name with no suffix. (Clones
        // are named PrefixFor(dim) + base name, the clone naming in ScaleItemTemplate (Items.csx) — never assert a literal
        // "Sword" against a dimension template.) Refused because: why
        Assert.Equal(item.Template.Name, item.Name);
    }

    [Fact]
    public void Refuses_a_non_dimension_item_and_charges_nothing()
    {
        var (fixture, player) = Loaded();
        using var _ = fixture;
        Carry(fixture, player, 50);         // the base template, dim 0
        var before = Spirit(fixture, player);

        fixture.RunCommand(player, "/resetitem 1");

        Assert.Equal(before, Spirit(fixture, player));
        Assert.Contains(player.Sent, m => m.Contains("higher plane"));
    }

    /// <summary>Two ways to be a high-id item that is not a generated clone, and the
    /// division alone catches neither. `50 + Offset*9` divides to dimension 9, which does
    /// not exist — Math.Pow(3, 9) would price it at 19,683. `77 + Offset*2` divides to a
    /// real dimension but has no base template behind it, so nothing cloned it and no
    /// dimension script is attached. The division says nothing about either case; the
    /// registered-template check refuses both before any spirit moves.</summary>
    [Theory]
    [InlineData(50 + Offset * 9)]
    [InlineData(77 + Offset * 2)]
    public void Refuses_a_high_id_template_that_is_not_a_generated_clone_and_charges_nothing(int templateId)
    {
        var (fixture, player) = Loaded();
        using var _ = fixture;
        // Registered after OnLoaded, so nothing cloned it and it has no dimension script.
        fixture.AddBaseItemTemplate(templateId, "Impostor", ItemTemplate.UseTypes.Weapon);
        Carry(fixture, player, templateId);
        var before = Spirit(fixture, player);

        fixture.RunCommand(player, "/resetitem 1");

        Assert.Equal(before, Spirit(fixture, player));
        Assert.Equal("Impostor", player.Inventory.GetSlot(1).Item.Name);
        Assert.Contains(player.Sent, m => m.Contains("higher plane"));
    }

    [Fact]
    public void Refuses_a_tome_or_other_non_equipment_and_charges_nothing()
    {
        var (fixture, player) = Loaded();
        using var _ = fixture;
        Carry(fixture, player, 70 + Offset * 2);    // the Scroll tome clone
        var before = Spirit(fixture, player);

        fixture.RunCommand(player, "/resetitem 1");

        Assert.Equal(before, Spirit(fixture, player));
        Assert.Contains(player.Sent, m => m.Contains("weapons and armor"));
    }

    /// <summary>One Item backs a whole stack (ItemSlot.cs:17-19), so a reroll on a stack of
    /// two rewrites both for the price of one and hands the player a free copy.</summary>
    [Fact]
    public void Refuses_a_stacked_slot_and_charges_nothing()
    {
        var (fixture, player) = Loaded();
        using var _ = fixture;
        // Force a stackable dimension weapon: StackSize travels to the clone, so raise it
        // on the base before OnLoaded is not possible here — set it on the clone directly.
        fixture.World.ItemHandler.GetTemplate(50 + Offset * 2).StackSize = 10;
        Carry(fixture, player, 50 + Offset * 2, stack: 2);
        var before = Spirit(fixture, player);

        fixture.RunCommand(player, "/resetitem 1");

        Assert.Equal(before, Spirit(fixture, player));
        Assert.Equal(2, player.Inventory.GetSlot(1).Stack);
    }

    [Fact]
    public void Refuses_when_the_balance_is_short_and_charges_nothing()
    {
        var (fixture, player) = Loaded(spiritBalance: 700);
        using var _ = fixture;
        Carry(fixture, player, 50 + Offset * 6);        // costs 729

        fixture.RunCommand(player, "/resetitem 1");

        Assert.Equal(700, Spirit(fixture, player));
        Assert.Contains(player.Sent, m => m.Contains("Not enough spirit"));
    }

    /// <summary>A dedicated log type, not CreatedCustom: an economy audit has to be able to
    /// separate player rerolls from GM item creation.</summary>
    [Fact]
    public void Logs_a_reset_item_entry_with_the_cost()
    {
        var (fixture, player) = Loaded();
        using var _ = fixture;
        Carry(fixture, player, 50 + Offset * 2);

        fixture.RunCommand(player, "/resetitem 1");

        var entry = Assert.Single(fixture.World.LogHandler.Pending, l => l.Type == Log.Types.ResetItem);
        Assert.Contains("9", entry.Text);       // 3^2
    }
}
