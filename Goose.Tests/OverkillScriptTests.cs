using Goose.Scripting;
using Goose.Testing;

namespace Goose.Tests;

public class OverkillScriptTests
{
    [Theory]
    [InlineData("", 750)]
    [InlineData("cap=0.6", 600)]
    [InlineData("cap=0.9", 750)]
    public void Refund_is_the_wasted_fraction_limited_by_the_cap(string scriptParams, long expectedMp)
    {
        using var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(1, "Test");
        var player = fixture.CommandPlayerOn(map, 1, 1);
        player.BaseStats = new AttributeSet { HP = 1000, MP = 1000 };
        player.MaxStats = player.BaseStats + new AttributeSet();
        player.CurrentHP = 1000;
        player.CurrentMP = 0;
        var effect = OverkillEffect(fixture, scriptParams);
        var cast = new SpellCast(new Spell { ID = 1, Name = "Test" }, player)
        {
            MPPercentCostPaid = 1000,
            TotalDamage = 4000,
            UsefulDamage = 1000,
        };

        effect.Script.Object.OnCastCompleted(effect, cast, fixture.World);

        Assert.Equal(expectedMp, player.CurrentMP);
    }

    [Fact]
    public void Waste_is_measured_from_damage_applied_after_the_target_scales_it()
    {
        using var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(1, "Test");
        map.CanCast = true;
        fixture.Settings.DamageModifier = 1;
        var player = fixture.CommandPlayerOn(map, 1, 1);
        var folder = Path.Combine(fixture.DataDirectory, "Scripts", "NPC");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "Times4.csx"), """
            using Goose;
            using Goose.Scripting;
            public class Times4 : BaseNPCScript
            {
                public override long InterceptDamageTaken(NPC npc, ICharacter attacker, long damage, GameWorld world) => damage * 4;
            }
            return typeof(Times4);
            """);
        var template = fixture.AddNpcTemplate(1, "Scaled", t =>
        {
            t.BaseStats = new AttributeSet { HP = 1000 };
            t.Script = fixture.World.ScriptHandler.GetScript<INPCScript>("Scripts/NPC/Times4.csx");
        });
        var npc = fixture.SpawnNpc(map, 2, 1, template);
        var effect = OverkillEffect(fixture, "");
        effect.HPFormula = "-500";
        effect.Effected = SpellEffect.SpellEffected.NPC;
        effect.MaximumLevelEffected = 100;
        var cast = new SpellCast(new Spell { ID = 1, Name = "Test" }, player);

        Assert.True(effect.Script!.Object.Cast(effect, player, npc, fixture.World, cast));

        Assert.Equal(NPC.States.Dead, npc.State);
        Assert.Equal(2000, cast.TotalDamage);
        Assert.Equal(1000, cast.UsefulDamage);
    }

    [Fact]
    public void Description_shows_the_cap()
    {
        using var fixture = new TestWorldFixture();
        var effect = OverkillEffect(fixture, "cap=0.6");

        Assert.Contains("Overkill refunds unused HP/MP cost (up to 60%)", effect.GetItemDescription(fixture.World));
    }

    private static SpellEffect OverkillEffect(TestWorldFixture fixture, string scriptParams)
    {
        var scriptBody = File.ReadAllText(Path.Combine(FindIllutiaDataDirectory(), "Scripts", "Spell", "Overkill.csx"));
        return new SpellEffect
        {
            ID = 134,
            Name = "Arcane Blast",
            EffectType = SpellEffect.EffectTypes.Script,
            HPFormula = "-1.9 * %ccmp",
            MPFormula = "0",
            ScriptParams = scriptParams,
            Script = fixture.CompileSpellEffectScript(scriptBody, "Overkill.csx"),
        };
    }

    private static string FindIllutiaDataDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            foreach (var candidate in new[]
            {
                Path.Combine(directory.FullName, "Data", "Illutia"),
                Path.Combine(directory.FullName, "Goose", "Data", "Illutia"),
            })
            {
                if (Directory.Exists(candidate)) return candidate;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate Data/Illutia from " + AppContext.BaseDirectory);
    }
}
