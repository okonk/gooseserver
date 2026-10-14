using Goose.Testing;

namespace Goose.IntegrationTests;

public class TickSnareScriptTests
{
    [Fact]
    public void Ticks_use_the_caster_mp_from_cast_time()
    {
        using var fixture = CreateFixture();
        var (effect, caster, npc) = CreateScenario(fixture);

        effect.Cast(caster, npc, fixture.World);
        caster.CurrentMP = 0;
        Tick(effect, npc, fixture.World);

        Assert.Equal(1000 - 100 - 100, npc.CurrentHP);
    }

    [Fact]
    public void Recasting_refreshes_the_mp_snapshot()
    {
        using var fixture = CreateFixture();
        var (effect, caster, npc) = CreateScenario(fixture);

        effect.Cast(caster, npc, fixture.World);
        caster.CurrentMP = 40;
        effect.Cast(caster, npc, fixture.World);
        caster.CurrentMP = 0;
        Tick(effect, npc, fixture.World);

        Assert.Equal(1000 - 100 - 40, npc.CurrentHP);
    }

    private static void Tick(SpellEffect effect, NPC npc, GameWorld world) =>
        effect.Script!.Object.OnBuffTick(Assert.Single(npc.Buffs), world);

    private static TestWorldFixture CreateFixture() =>
        new(settings => settings.DamageModifier = 1.0);

    private static (SpellEffect Effect, TestWorldFixture.CapturingPlayer Caster, NPC Npc) CreateScenario(
        TestWorldFixture fixture)
    {
        var map = fixture.AddBaseMap(1, "Test");
        map.CanCast = true;

        var caster = fixture.CommandPlayerOn(map, 5, 5, "Caster");
        caster.LoginID = 101;
        caster.Level = 10;
        caster.MaxStats.MP = 1000;
        caster.CurrentMP = 100;

        var npc = new NPC
        {
            LoginID = 404,
            Name = "NPC",
            Map = map,
            MapID = map.ID,
            MapX = 6,
            MapY = 5,
            Level = 10,
            State = NPC.States.Alive,
            BaseStats = new AttributeSet(),
            MaxStats = new AttributeSet { HP = 1000 },
            NPCTemplate = new NPCTemplate(),
            CanBeKilled = true,
            Buffs = [],
            AggroTargetToValue = [],
        };
        npc.CurrentHP = 1000;
        map.SetCharacter(npc, 6, 5);

        var scriptBody = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "SpellScripts", "TickSnare.csx"));
        var effect = fixture.AddBaseSpellEffect(302, "Blizzard", e =>
        {
            e.EffectType = SpellEffect.EffectTypes.Tick;
            e.TargetType = SpellEffect.TargetTypes.Target;
            e.Effected = SpellEffect.SpellEffected.NPC;
            e.MinimumLevelEffected = 0;
            e.MaximumLevelEffected = 99;
            e.WorksNotInPVP = true;
            e.Duration = 10;
            e.HPFormula = "-1 * %ccmp";
            e.MPFormula = "0";
            e.SPFormula = "0";
            e.ScriptParams = "";
            e.Script = fixture.CompileSpellEffectScript(scriptBody, "TickSnare.csx");
        });
        return (effect, caster, npc);
    }
}
