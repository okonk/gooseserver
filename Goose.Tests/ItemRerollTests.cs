using Goose;
using Goose.Scripting;
using Goose.Tests.Collections;
using Goose.Tests.Fixtures;
using Xunit;

namespace Goose.Tests;

/// <summary>Fixture-based, and in GameWorldSettingsCollection: GlobalScriptFixture swaps
/// the static GameWorld.Settings in its constructor and restores it on dispose
/// (GlobalScriptFixture.cs:7,:38), so a class that mutates settings must not run in
/// parallel with the other suites that do the same.</summary>
[Collection(GameWorldSettingsCollection.Name)]
public class ItemRerollTests
{
    /// <summary>Modelled on ItemScriptHookTests.Arrange (`ItemScriptHookTests.cs:62-77`):
    /// a template registered in the handler, an Item loaded from it, and
    /// `ScriptStub.For<IItemScript>` for the script — the established way to attach an
    /// in-memory script object without touching disk (`Fixtures/ScriptStub.cs`).
    ///
    /// The template carries non-zero BaseStats and WeaponDamage on purpose: both are
    /// accumulated by RefreshStats (`Item.cs:247-256`), so they are what a non-idempotent
    /// reset would double.</summary>
    private static Item ItemWithModifiers(GlobalScriptFixture fixture, IItemScript script = null)
    {
        var template = fixture.AddBaseItemTemplate(1, "Sword", ItemTemplate.UseTypes.Weapon, t =>
        {
            t.WeaponDamage = 7;
            t.BaseStats = new AttributeSet { Strength = 10 };
            if (script != null) t.Script = ScriptStub.For(script);
        });

        var item = new Item();
        item.LoadFromTemplate(template);
        return item;
    }

    /// <summary>ResetModifiers must return the item to template state. Item.cs:14-18 has
    /// exactly two ItemProperty members, both of which a modifier sets — and
    /// ItemModifierScript.csx:67 also writes item.WeaponDamage, which RefreshStats folds
    /// into TotalWeaponDamage (Item.cs:256).</summary>
    [Fact]
    public void ResetModifiers_clears_name_stats_multiplier_weapon_damage_and_properties()
    {
        using var fixture = new GlobalScriptFixture();
        var item = ItemWithModifiers(fixture);
        item.Name = "Legendary Sword of Speed";
        item.BaseStats.Strength = 50;
        item.WeaponDamage = 40;
        item.StatMultiplier = 1.25;
        item.ItemProperties[ItemProperty.TitleId] = 900100;
        item.ItemProperties[ItemProperty.SurnameId] = 900005;

        fixture.World.ItemHandler.ResetModifiers(item);

        Assert.Equal(item.Template.Name, item.Name);
        Assert.Equal(0, item.BaseStats.Strength);
        Assert.Equal(0, item.WeaponDamage);
        Assert.Equal(1, item.StatMultiplier);
        Assert.Equal(10, item.TotalStats.Strength);
        Assert.Equal(7, item.TotalWeaponDamage);
        Assert.False(item.ItemProperties.ContainsKey(ItemProperty.TitleId));
        Assert.False(item.ItemProperties.ContainsKey(ItemProperty.SurnameId));
    }

    /// <summary>The guard that matters. Item.LoadFromTemplate does TotalStats +=
    /// (Item.cs:159), so a reset built on it would double-count the template's stats on
    /// every call — and a reset that forgets item.WeaponDamage lets repeated paid rerolls
    /// stack weapon damage forever. ResetModifiers must be safe to call repeatedly, in
    /// both fields.</summary>
    [Fact]
    public void ResetModifiers_is_idempotent_in_stats_and_weapon_damage()
    {
        using var fixture = new GlobalScriptFixture();
        var item = ItemWithModifiers(fixture);
        item.BaseStats.Strength = 50;
        item.WeaponDamage = 40;
        item.StatMultiplier = 1.25;

        fixture.World.ItemHandler.ResetModifiers(item);
        var statsAfterOnce = item.TotalStats.Strength;
        var damageAfterOnce = item.TotalWeaponDamage;
        fixture.World.ItemHandler.ResetModifiers(item);

        Assert.Equal(statsAfterOnce, item.TotalStats.Strength);
        Assert.Equal(damageAfterOnce, item.TotalWeaponDamage);
        Assert.Equal(10, item.TotalStats.Strength);   // the template's, counted once
        Assert.Equal(7, item.TotalWeaponDamage);      // ditto
    }

    [Fact]
    public void RerollModifiers_prefers_the_script_hook()
    {
        using var fixture = new GlobalScriptFixture();
        var item = ItemWithModifiers(fixture, new RerollingStub());

        fixture.World.ItemHandler.RerollModifiers(item, fixture.World);

        Assert.Equal("rerolled", item.Name);
    }

    [Fact]
    public void RerollModifiers_falls_through_when_the_hook_declines()
    {
        // A stub returning false must leave the item at template state — the native
        // RollTitleAndSurname path runs, and with zero-chance settings adds nothing.
        using var fixture = new GlobalScriptFixture();
        GameWorld.Settings.ItemTitleChancePercent = 0;
        GameWorld.Settings.ItemSurnameChancePercent = 0;
        var item = ItemWithModifiers(fixture, new DecliningStub());
        item.Name = "Legendary Sword of Speed";

        fixture.World.ItemHandler.RerollModifiers(item, fixture.World);

        Assert.Equal(item.Template.Name, item.Name);
    }

    /// <summary>The realistic failure, and the reason "no exception was thrown" is not a
    /// sufficient assertion: a hook that applies a suffix and *then* throws would otherwise
    /// leave the item carrying a modifier the reset already stripped and the catch never
    /// undoes — free stats on a charge that also refunds nothing. RerollModifiers must
    /// reset again in the catch so the native fallback rolls against template state.</summary>
    [Fact]
    public void RerollModifiers_returns_a_half_applied_throwing_hook_to_template_state()
    {
        using var fixture = new GlobalScriptFixture();
        GameWorld.Settings.ItemTitleChancePercent = 0;
        GameWorld.Settings.ItemSurnameChancePercent = 0;
        var item = ItemWithModifiers(fixture, new HalfApplyingThrowingStub());

        var ex = Record.Exception(() => fixture.World.ItemHandler.RerollModifiers(item, fixture.World));

        Assert.Null(ex);
        Assert.Equal(item.Template.Name, item.Name);
        Assert.False(item.HasProperty(ItemProperty.SurnameId));
        Assert.Equal(0, item.WeaponDamage);
        Assert.Equal(1, item.StatMultiplier);
        Assert.Equal(10, item.TotalStats.Strength);
        Assert.Equal(7, item.TotalWeaponDamage);
    }

    private class RerollingStub : BaseItemScript
    {
        public override bool OnRerollModifiersEvent(Item item, GameWorld world)
        {
            item.Name = "rerolled";
            return true;
        }
    }

    private class DecliningStub : BaseItemScript { }

    /// <summary>Mutates the way DimensionItem.OnRerollModifiersEvent does — name, the
    /// surname property, then stats — and throws partway through.</summary>
    private class HalfApplyingThrowingStub : BaseItemScript
    {
        public override bool OnRerollModifiersEvent(Item item, GameWorld world)
        {
            item.Name = item.Name + " of the Bear";
            item.ItemProperties[ItemProperty.SurnameId] = 900005;
            item.BaseStats.Strength += 25;
            item.WeaponDamage += 40;
            item.StatMultiplier *= 1.25;
            item.RefreshStats();
            throw new InvalidOperationException("boom");
        }
    }
}
