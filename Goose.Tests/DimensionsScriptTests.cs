using Goose.Tests.Collections;
using Goose.Tests.Fixtures;

namespace Goose.Tests;

[Collection(GameWorldSettingsCollection.Name)]
public class DimensionsScriptTests
{
    private static GlobalScriptFixture Run(Action<GlobalScriptFixture> arrange)
    {
        var fixture = new GlobalScriptFixture();
        arrange(fixture);
        fixture.CompileShipped().Object.OnLoaded(fixture.World);
        return fixture;
    }

    [Fact]
    public void Disabled_by_configuration_changes_nothing()
    {
        using var fixture = new GlobalScriptFixture();
        fixture.AddBaseMap(1, "Town", width: 100, height: 100);
        var boss = new NPCTemplate { NPCTemplateID = 162, Name = "Shadow Dog", Level = 40 };
        boss.BaseStats = new AttributeSet { HP = 3704 };
        fixture.World.NPCHandler.AddTemplate(boss);

        var source = File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "DimensionScripts", "Dimensions.csx"));
        var disabled = source.Replace("public const bool Enabled = true;",
                                      "public const bool Enabled = false;");
        Assert.NotEqual(source, disabled);   // the flag line moved - fix this test, not the script

        fixture.CompileSource(disabled, "DimensionsDisabled.csx").Object.OnLoaded(fixture.World);

        Assert.Single(fixture.World.MapHandler.Maps);
        Assert.Null(fixture.World.MapHandler.GetMap(100001));
        Assert.Null(fixture.World.NPCHandler.GetNPCTemplate(100162));
        Assert.Null(fixture.World.QuestHandler.Get(900000));
        Assert.Equal(0, fixture.World.NPCHandler.NPCCount);
        // No /dimension command either - the whole feature is off, not just the world.
        Assert.False(fixture.World.EventHandler.AddEvent(new Player(0), "/dimension 1"));
    }

    [Fact]
    public void Clones_each_template_once_per_dimension_with_scaled_stats()
    {
        using var fixture = Run(f =>
        {
            var t = new NPCTemplate { NPCTemplateID = 162, Name = "Shadow Dog", Level = 40,
                                      WeaponDamage = 365, RespawnTime = 50, Experience = 750,
                                      AttackSpeed = 1.5m, MoveSpeed = 1.5m, AttackRange = 1,
                                      CanBeRooted = true, CanBeStunned = true, CanBeSlowed = false };
            t.BaseStats = new AttributeSet { HP = 3704 };
            f.World.NPCHandler.AddTemplate(t);
        });

        var dim3 = fixture.World.NPCHandler.GetNPCTemplate(162 + 100000 * 3);
        Assert.NotNull(dim3);
        Assert.Equal("Shadow Dog (3)", dim3.Name);

        // NPC.java:927 - (base + 100000*2^dim) * 4.7^dim
        Assert.Equal((long)((3704 + 100000 * Math.Pow(2, 3)) * Math.Pow(4.7, 3)), dim3.BaseStats.HP);
        // NPC.java:936 - base*4^dim + 100000*max(0, 4^dim-3)
        Assert.Equal((long)(365 * Math.Pow(4, 3) + 100000 * Math.Max(0, Math.Pow(4, 3) - 3)), dim3.WeaponDamage);
        // NPC.java:954 - (exp + level*100) * 3^min(4,dim)
        Assert.Equal((long)((750 + 40 * 100) * Math.Pow(3, 3)), dim3.Experience);
        // NPC.java:899 - every dimension mob is level 50
        Assert.Equal(50, dim3.Level);
        // NPC.java:881 - immune to root and stun, but slowable
        Assert.False(dim3.CanBeRooted);
        Assert.False(dim3.CanBeStunned);
        Assert.True(dim3.CanBeSlowed);
        // NPC.java:869 - attack range grows with dimension
        Assert.Equal(1 + 3, dim3.AttackRange);
    }

    [Fact]
    public void Leaves_the_base_template_untouched()
    {
        using var fixture = Run(f =>
        {
            var t = new NPCTemplate { NPCTemplateID = 162, Name = "Shadow Dog", Level = 40 };
            t.BaseStats = new AttributeSet { HP = 3704 };
            f.World.NPCHandler.AddTemplate(t);
        });

        var basic = fixture.World.NPCHandler.GetNPCTemplate(162);
        Assert.Equal("Shadow Dog", basic.Name);
        Assert.Equal(3704, basic.BaseStats.HP);
        Assert.Equal(40, basic.Level);
    }

    [Fact]
    public void Applies_the_dimension_five_multipliers()
    {
        using var fixture = Run(f =>
        {
            var t = new NPCTemplate { NPCTemplateID = 162, Name = "Shadow Dog", Level = 40, WeaponDamage = 365 };
            t.BaseStats = new AttributeSet { HP = 3704 };   // <= 35,000,000, so HP doubles at dim >= 5
            f.World.NPCHandler.AddTemplate(t);
        });

        var dim5 = fixture.World.NPCHandler.GetNPCTemplate(162 + 100000 * 5);

        Assert.Equal((long)((3704 + 100000 * Math.Pow(2, 5)) * Math.Pow(4.7, 5)) * 2, dim5.BaseStats.HP);
        // base < 10,000,000 so damage is multiplied by 20
        Assert.Equal((long)(365 * Math.Pow(4, 5) + 100000 * Math.Max(0, Math.Pow(4, 5) - 3)) * 20, dim5.WeaponDamage);
        // This value exceeds int.MaxValue - it only fits because Part 1 widened the fields.
        Assert.True(dim5.BaseStats.HP > int.MaxValue);
    }
}
