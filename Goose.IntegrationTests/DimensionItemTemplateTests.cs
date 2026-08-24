using Goose.IntegrationTests.Collections;
using Goose.IntegrationTests.Fixtures;

namespace Goose.IntegrationTests;

[Collection(GameWorldSettingsCollection.Name)]
public class DimensionItemTemplateTests
{
    private static GlobalScriptFixture Run(Action<GlobalScriptFixture> arrange)
    {
        var fixture = new GlobalScriptFixture();
        fixture.AddBaseMap(1, "Town", width: 100, height: 100);
        arrange(fixture);
        fixture.CompileShipped().Object.OnLoaded(fixture.World);
        return fixture;
    }

    [Fact]
    public void Clones_equipment_once_per_dimension_with_prefix_and_recolour()
    {
        using var fixture = Run(f => f.AddBaseItemTemplate(50, "Sword", ItemTemplate.UseTypes.Weapon,
            t => { t.GraphicR = 200; t.GraphicG = 100; t.GraphicB = 20; t.GraphicA = 100; t.Value = 500; }));

        for (int dim = 1; dim <= 6; dim++)
            Assert.NotNull(fixture.World.ItemHandler.GetTemplate(50 + 100000 * dim));

        var dim3 = fixture.World.ItemHandler.GetTemplate(50 + 100000 * 3);
        Assert.Equal("Supreme Sword", dim3.Name);              // Item.java:416
        Assert.Equal("Abyss (3) A Sword", dim3.Description);   // Item.java:429
        Assert.Equal(110, dim3.GraphicR);                      // 200 - 30*3
        Assert.Equal(10, dim3.GraphicG);                       // 100 - 30*3
        Assert.Equal(0, dim3.GraphicB);                        // clamped at 0
        Assert.Equal(190, dim3.GraphicA);                      // 100 + 30*3
        Assert.Equal(500 * 27, dim3.Value);                    // base * 3^dim

        var dim6 = fixture.World.ItemHandler.GetTemplate(50 + 100000 * 6);
        Assert.Equal("Godly Sword", dim6.Name);
        Assert.Equal(200, dim6.GraphicA);                      // clamped at 200
    }

    [Fact]
    public void Clears_bind_and_lore_flags_on_clones()
    {
        using var fixture = Run(f => f.AddBaseItemTemplate(50, "Sword", ItemTemplate.UseTypes.Weapon,
            t => { t.IsLore = true; t.IsBindOnPickup = true; t.IsBindOnEquip = true; }));

        var dim1 = fixture.World.ItemHandler.GetTemplate(100050);
        Assert.False(dim1.IsLore);          // Item.java:225-260
        Assert.False(dim1.IsBindOnPickup);
        Assert.False(dim1.IsBindOnEquip);

        // The base template is untouched.
        Assert.True(fixture.World.ItemHandler.GetTemplate(50).IsLore);
    }

    [Fact]
    public void Does_not_clone_consumables_money_or_no_use_items()
    {
        using var fixture = Run(f =>
        {
            f.AddBaseItemTemplate(60, "Potion", ItemTemplate.UseTypes.OneTime);
            f.AddBaseItemTemplate(61, "Gold", ItemTemplate.UseTypes.Money);
            f.AddBaseItemTemplate(62, "Quest Token", ItemTemplate.UseTypes.NoUse);
        });

        Assert.Null(fixture.World.ItemHandler.GetTemplate(100060));
        Assert.Null(fixture.World.ItemHandler.GetTemplate(100061));
        Assert.Null(fixture.World.ItemHandler.GetTemplate(100062));
    }

    [Theory]
    [InlineData(10_000_000, 0, 0, 1.0)]     // Value >= 10M
    [InlineData(0, 500, 0, 0.75)]           // MinExperience > 0
    [InlineData(0, 0, 50, 0.5)]             // MinLevel == 50
    [InlineData(0, 0, 20, 0.25)]            // everything else
    public void Scales_stats_by_dimension_and_tier(long value, long minExp, int minLevel, double tier)
    {
        using var fixture = Run(f => f.AddBaseItemTemplate(50, "Sword", ItemTemplate.UseTypes.Weapon,
            t => { t.Value = value; t.MinExperience = minExp; t.MinLevel = minLevel;
                   t.BaseStats = new AttributeSet { AC = 10, Strength = 20, HP = 100 }; }));

        var dim2 = fixture.World.ItemHandler.GetTemplate(50 + 100000 * 2);

        // AttributeSet.java:421 - a1.AC * (0.5*dim) + 10*dim*tier
        Assert.Equal(10 + (int)(10 * 1.0 + 10 * 2 * tier), dim2.BaseStats.AC);
        // AttributeSet.java:442 - a1.Strength * (0.5*dim) + 100*dim*tier
        Assert.Equal(20 + (int)(20 * 1.0 + 100 * 2 * tier), dim2.BaseStats.Strength);
        // AttributeSet.java:429 - a1.HP * dim + (10*dim)^4 * tier
        Assert.Equal(100 + (long)(100 * 2 + Math.Pow(20, 4) * tier), dim2.BaseStats.HP);
    }

    [Fact]
    public void Ports_the_melee_damage_truncation_faithfully()
    {
        using var fixture = Run(f => f.AddBaseItemTemplate(50, "Sword", ItemTemplate.UseTypes.Weapon,
            t => { t.MinLevel = 20; t.BaseStats = new AttributeSet { MeleeDamage = 0.5m }; }));

        var dim2 = fixture.World.ItemHandler.GetTemplate(100050 + 100000);

        // AttributeSet.java:433 casts the whole term to int, so 0.5*2 = 1.0 survives but
        // any sub-1.0 product is truncated away. Tier 0.25, dim 2 -> (int)(1.0 + 10*2*0.25) = 6.
        Assert.Equal(0.5m + 6m, dim2.BaseStats.MeleeDamage);
    }

    [Fact]
    public void Refuses_to_overwrite_an_existing_template_id()
    {
        using var fixture = new GlobalScriptFixture();
        fixture.AddBaseMap(1, "Town", width: 100, height: 100);
        fixture.AddBaseItemTemplate(50, "Sword", ItemTemplate.UseTypes.Weapon);
        fixture.AddBaseItemTemplate(100050, "Impostor", ItemTemplate.UseTypes.Weapon);

        var script = fixture.CompileShipped();

        var exception = Assert.Throws<Exception>(() => script.Object.OnLoaded(fixture.World));
        Assert.Contains("100050", exception.Message);
    }

    [Fact]
    public void Clones_tomes_as_consumables_pointing_at_the_dimension_spell()
    {
        using var fixture = Run(f =>
        {
            f.AddBaseSpellEffect(7, "Firestorm Effect");
            f.AddBaseSpell(91, "Firestorm", 7);
            f.AddBaseItemTemplate(70, "Tome of Firestorm", ItemTemplate.UseTypes.Scroll,
                t => t.LearnSpellID = 91);
        });

        var dim2 = fixture.World.ItemHandler.GetTemplate(70 + 100000 * 2);
        Assert.NotNull(dim2);
        // OneTime, not Scroll: Inventory.cs:277 learns scrolls with no script hook, so the
        // upgrade rule needs the consumable path (Inventory.cs:423).
        Assert.Equal(ItemTemplate.UseTypes.OneTime, dim2.UseType);
        Assert.Equal(91 + 100000 * 2, dim2.LearnSpellID);
        Assert.Equal("Super Powerful Tome of Firestorm", dim2.Name);
    }

    [Fact]
    public void Leaves_a_tome_alone_when_its_spell_was_never_cloned()
    {
        using var fixture = Run(f =>
            // No spell 91 registered, so Part 3's spell pass produces no clone for it.
            f.AddBaseItemTemplate(70, "Tome of Nothing", ItemTemplate.UseTypes.Scroll,
                t => t.LearnSpellID = 91));

        var dim2 = fixture.World.ItemHandler.GetTemplate(70 + 100000 * 2);
        Assert.NotNull(dim2);
        // Pointing at a spell that does not exist would make the tome silently unusable
        // (Spellbook.LearnSpell returns false at Spellbook.cs:203). Keep the base spell.
        Assert.Equal(91, dim2.LearnSpellID);
        Assert.Equal(ItemTemplate.UseTypes.Scroll, dim2.UseType);
    }

    [Fact]
    public void Does_not_clone_scrolls_that_teach_nothing()
    {
        using var fixture = Run(f =>
            f.AddBaseItemTemplate(71, "Blank Scroll", ItemTemplate.UseTypes.Scroll));

        Assert.Null(fixture.World.ItemHandler.GetTemplate(100071));
    }

    [Fact]
    public void Tome_clones_get_no_equipment_stats()
    {
        using var fixture = Run(f =>
        {
            f.AddBaseSpellEffect(7, "Firestorm Effect");
            f.AddBaseSpell(91, "Firestorm", 7);
            f.AddBaseItemTemplate(70, "Tome of Firestorm", ItemTemplate.UseTypes.Scroll,
                t => { t.LearnSpellID = 91; t.MinLevel = 50; });   // tier 0.5, dim 6 -> huge if applied
        });

        var basic = fixture.World.ItemHandler.GetTemplate(70);
        var dim6 = fixture.World.ItemHandler.GetTemplate(70 + 100000 * 6);

        // AttributeSet.java:380-382 - dimensionDefault returns an empty set for anything
        // that is not equipment, so the tome's stats must match the base template exactly.
        Assert.Equal(basic.BaseStats.AC, dim6.BaseStats.AC);
        Assert.Equal(basic.BaseStats.HP, dim6.BaseStats.HP);
        Assert.Equal(basic.BaseStats.MP, dim6.BaseStats.MP);
        Assert.Equal(basic.BaseStats.Strength, dim6.BaseStats.Strength);
        Assert.Equal(basic.BaseStats.Stamina, dim6.BaseStats.Stamina);
        Assert.Equal(basic.BaseStats.Intelligence, dim6.BaseStats.Intelligence);
        Assert.Equal(basic.BaseStats.Dexterity, dim6.BaseStats.Dexterity);
        Assert.Equal(basic.BaseStats.FireResist, dim6.BaseStats.FireResist);
        Assert.Equal(basic.BaseStats.MeleeDamage, dim6.BaseStats.MeleeDamage);

        // The rest of the clone still applies - this is a stat guard, not a clone opt-out.
        Assert.Equal("Godly Tome of Firestorm", dim6.Name);
        Assert.Equal(91 + 100000 * 6, dim6.LearnSpellID);
    }
}
