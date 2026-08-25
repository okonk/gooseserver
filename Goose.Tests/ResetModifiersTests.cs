using Goose;
using Goose.Testing;
using Xunit;

namespace Goose.Tests;

public class ResetModifiersTests
{
    /// <summary>Modelled on ItemScriptHookTests.Arrange (`ItemScriptHookTests.cs:62-77`):
    /// a template registered in the handler and an Item loaded from it.
    ///
    /// The template carries non-zero BaseStats and WeaponDamage on purpose: both are
    /// accumulated by RefreshStats (`Item.cs:247-256`), so they are what a non-idempotent
    /// reset would double.</summary>
    private static Item ItemWithModifiers(TestWorldFixture fixture)
    {
        var template = fixture.AddBaseItemTemplate(1, "Sword", ItemTemplate.UseTypes.Weapon, t =>
        {
            t.WeaponDamage = 7;
            t.BaseStats = new AttributeSet { Strength = 10 };
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
        using var fixture = new TestWorldFixture();
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
        using var fixture = new TestWorldFixture();
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
}
