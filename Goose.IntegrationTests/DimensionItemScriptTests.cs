using Goose.Scripting;
using Goose.IntegrationTests.Fixtures;
using Goose.Testing;

namespace Goose.IntegrationTests;

public class DimensionItemScriptTests
{
    private const string MaxDimension = "dimension.max";

    private static GlobalScriptFixture Run(Action<GlobalScriptFixture>? arrange = null)
    {
        var fixture = new GlobalScriptFixture();
        fixture.AddBaseMap(1, "Town", width: 100, height: 100);
        fixture.AddBaseItemTemplate(50, "Sword", ItemTemplate.UseTypes.Weapon, t => t.MinLevel = 50);
        arrange?.Invoke(fixture);
        fixture.CompileShipped().Object.OnLoaded(fixture.World);
        return fixture;
    }

    private static Item ItemOf(GlobalScriptFixture fixture, int templateId)
    {
        var item = new Item();
        item.LoadFromTemplate(fixture.World.ItemHandler.GetTemplate(templateId)!);
        return item;
    }

    [Fact]
    public void Every_clone_carries_the_dimension_script()
    {
        using var fixture = Run();
        var script = fixture.World.ItemHandler.GetTemplate(100050)!.Script;

        Assert.NotNull(script);
        // ScriptHandler caches by path (ScriptHandler.cs:24), so every clone shares one object.
        Assert.Same(script, fixture.World.ItemHandler.GetTemplate(600050)!.Script);
        // The base template is untouched.
        Assert.Null(fixture.World.ItemHandler.GetTemplate(50)!.Script);
    }

    [Fact]
    public void Rolls_a_suffix_on_roughly_forty_five_percent_of_items_in_even_bands()
    {
        using var fixture = Run();
        var counts = new Dictionary<string, int>();
        int suffixed = 0;

        for (int i = 0; i < 4000; i++)
        {
            var item = ItemOf(fixture, 300050);
            fixture.World.ItemHandler.RollTitleAndSurname(item, fixture.World);

            if (!item.HasProperty(ItemProperty.SurnameId)) continue;

            suffixed++;
            var name = fixture.World.ItemHandler.GetSurname(item.GetProperty<int>(ItemProperty.SurnameId))!.Name;
            counts[name] = counts.GetValueOrDefault(name) + 1;
        }

        // Item.java:359-388 - 45% total, six equal 7.5% bands. Wide bounds: this is a
        // distribution check, not an exact-value check.
        Assert.InRange(suffixed, 4000 * 0.38, 4000 * 0.52);
        Assert.Equal(6, counts.Count);
        foreach (var count in counts.Values)
            Assert.InRange(count, 4000 * 0.045, 4000 * 0.105);
    }

    [Fact]
    public void Rolls_rarity_titles_at_two_percent_each()
    {
        using var fixture = Run();
        int legendary = 0, stunted = 0;

        for (int i = 0; i < 4000; i++)
        {
            var item = ItemOf(fixture, 300050);
            fixture.World.ItemHandler.RollTitleAndSurname(item, fixture.World);

            if (!item.HasProperty(ItemProperty.TitleId)) continue;
            if (item.GetProperty<int>(ItemProperty.TitleId) == 900100) legendary++; else stunted++;
        }

        Assert.InRange(legendary, 4000 * 0.008, 4000 * 0.035);   // Item.java:393
        Assert.InRange(stunted, 4000 * 0.008, 4000 * 0.035);     // Item.java:397
    }

    [Fact]
    public void Applies_the_rolled_modifier_to_the_items_name_and_stats()
    {
        using var fixture = Run();

        // Roll until a suffix lands - the roll is random by design.
        Item? item = null;
        for (int i = 0; i < 200 && item == null; i++)
        {
            var candidate = ItemOf(fixture, 300050);
            fixture.World.ItemHandler.RollTitleAndSurname(candidate, fixture.World);
            if (candidate.HasProperty(ItemProperty.SurnameId)) item = candidate;
        }

        Assert.NotNull(item);
        var surname = fixture.World.ItemHandler.GetSurname(item.GetProperty<int>(ItemProperty.SurnameId));
        // The rarity title (2% Legendary / 2% Stunted) may legally precede the base
        // name, so Contains, not Equal (same reasoning as DimensionResetItemTests).
        Assert.Contains(item.Template.Name, item.Name);   // the dim-3 clone name, "Supreme Sword"
        Assert.Contains(surname!.Name, item.Name);         // the rolled suffix, after the base name
        Assert.NotEqual(new AttributeSet(), item.BaseStats);
    }

    [Fact]
    public void Suppresses_the_native_rolls_on_dimension_items()
    {
        using var fixture = Run();
        fixture.World.ItemHandler.AddSurname(new ItemModifier
        {
            Id = 1, Name = "of the Bear", Chance = 1.0,
            Slot = ItemTemplate.ItemSlots.Misc, UseType = ItemTemplate.UseTypes.Weapon,
        });

        for (int i = 0; i < 50; i++)
        {
            var item = ItemOf(fixture, 300050);
            fixture.World.ItemHandler.RollTitleAndSurname(item, fixture.World);
            Assert.DoesNotContain("of the Bear", item.Name);
        }
    }

    [Fact]
    public void Refuses_pickup_above_the_players_unlocked_dimension()
    {
        using var fixture = Run();
        var script = fixture.World.ItemHandler.GetTemplate(300050)!.Script!.Object;
        var player = new Player(0);
        player.Properties[MaxDimension] = 2;

        Assert.NotNull(script.CanPickup(player, ItemOf(fixture, 300050), fixture.World));
        Assert.Null(script.CanPickup(player, ItemOf(fixture, 200050), fixture.World));
        Assert.Null(script.CanPickup(player, ItemOf(fixture, 100050), fixture.World));
    }

    [Fact]
    public void A_tome_upgrades_a_lower_dimension_spell_in_place()
    {
        using var fixture = Run(f =>
        {
            f.AddBaseSpellEffect(7, "Firestorm Effect");
            f.AddBaseSpell(91, "Firestorm", 7);
            f.AddBaseItemTemplate(70, "Tome of Firestorm", ItemTemplate.UseTypes.Scroll,
                t => t.LearnSpellID = 91);
        });

        var player = new Player(0) { Spellbook = null! };
        player.Spellbook = new Spellbook(player, fixture.Settings);
        player.Spellbook.AddSpell(fixture.World.SpellHandler.GetSpell(100091)!, fixture.World);

        var tome = ItemOf(fixture, 300070);
        var consumed = tome.Script!.Object.OnUseConsumableEvent(player, tome, fixture.World);

        Assert.True(consumed);
        Assert.Equal(1, CountSpells(fixture, player, 300091));
        Assert.Equal(0, CountSpells(fixture, player, 100091));
    }

    [Fact]
    public void A_tome_refuses_when_the_known_spell_is_equal_or_higher()
    {
        using var fixture = Run(f =>
        {
            f.AddBaseSpellEffect(7, "Firestorm Effect");
            f.AddBaseSpell(91, "Firestorm", 7);
            f.AddBaseItemTemplate(70, "Tome of Firestorm", ItemTemplate.UseTypes.Scroll,
                t => t.LearnSpellID = 91);
        });

        var player = new Player(0);
        player.Spellbook = new Spellbook(player, fixture.Settings);
        player.Spellbook.AddSpell(fixture.World.SpellHandler.GetSpell(500091)!, fixture.World);

        var tome = ItemOf(fixture, 300070);

        // false = do not consume. Inventory.cs:433 removes the item only when true.
        Assert.False(tome.Script!.Object.OnUseConsumableEvent(player, tome, fixture.World));
        Assert.Equal(1, CountSpells(fixture, player, 500091));
    }

    [Fact]
    public void A_tome_teaches_an_unknown_spell_outright()
    {
        using var fixture = Run(f =>
        {
            f.AddBaseSpellEffect(7, "Firestorm Effect");
            f.AddBaseSpell(91, "Firestorm", 7);
            f.AddBaseItemTemplate(70, "Tome of Firestorm", ItemTemplate.UseTypes.Scroll,
                t => t.LearnSpellID = 91);
        });

        var player = new Player(0);
        player.Spellbook = new Spellbook(player, fixture.Settings);

        var tome = ItemOf(fixture, 300070);

        Assert.True(tome.Script!.Object.OnUseConsumableEvent(player, tome, fixture.World));
        Assert.Equal(1, CountSpells(fixture, player, 300091));
    }

    [Fact]
    public void Forwards_to_the_base_templates_script()
    {
        using var fixture = new GlobalScriptFixture();
        fixture.AddBaseMap(1, "Town", width: 100, height: 100);

        // A base template whose script records the calls it receives.
        var inner = new RecordingScript();
        fixture.AddBaseItemTemplate(51, "Okonk Sword", ItemTemplate.UseTypes.Weapon,
            t => { t.Script = ScriptStub.For<IItemScript>(inner); t.ScriptParams = "inner-params"; });
        fixture.CompileShipped().Object.OnLoaded(fixture.World);

        var item = ItemOf(fixture, 100051);
        item.Script!.Object.OnMeleeEvent(new Player(0), item, fixture.World);

        Assert.Equal(1, inner.MeleeCalls);
        // LoadFromTemplate copies ScriptParams (Item.cs:176), so the inner script still
        // reads the params it was written against.
        Assert.Equal("inner-params", item.ScriptParams);
    }

    private sealed class RecordingScript : BaseItemScript
    {
        public int MeleeCalls;
        public override void OnMeleeEvent(Player player, Item item, GameWorld world) => this.MeleeCalls++;
    }

    private static int CountSpells(GlobalScriptFixture fixture, Player player, int spellId)
    {
        int found = 0;
        for (int slot = 1; slot <= fixture.Settings.SpellbookSize; slot++)
            if (player.Spellbook.GetSlot(slot)?.ID == spellId) found++;
        return found;
    }
}
