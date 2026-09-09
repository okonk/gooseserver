using Goose.Testing;

namespace Goose.IntegrationTests;

public class BackstabScriptTests
{
    [Theory]
    [InlineData(1, 5, 4)]
    [InlineData(2, 6, 5)]
    [InlineData(3, 5, 6)]
    [InlineData(4, 4, 5)]
    public void Cast_hits_the_character_one_tile_in_front_of_the_self_target(int facing, int targetX, int targetY)
    {
        using var fixture = CreateFixture();
        var (map, effect, caster) = CreateScenario(fixture, facing);
        var victim = CreatePlayer(fixture, map, targetX, targetY, "Victim", 202, DifferentFacing(facing));
        map.SetCharacter(victim, targetX, targetY);

        var result = effect.Cast(caster, caster, fixture.World);

        Assert.True(result);
        Assert.Equal(987, victim.CurrentHP);
        Assert.Contains(ExpectedAnimation(caster, targetX, targetY, effect), caster.Sent);
    }

    [Fact]
    public void Cast_hits_an_npc_one_tile_in_front()
    {
        using var fixture = CreateFixture();
        var (map, effect, caster) = CreateScenario(fixture, 2);
        var victim = CreateNpc(map, 6, 5, 3);
        map.SetCharacter(victim, 6, 5);

        var result = effect.Cast(caster, caster, fixture.World);

        Assert.True(result);
        Assert.Equal(960, victim.CurrentHP);
    }

    [Fact]
    public void Cast_uses_a_decimal_configured_multiplier_when_facings_differ()
    {
        using var fixture = CreateFixture();
        var (map, effect, caster) = CreateScenario(fixture, 2, "1.5");
        var victim = CreatePlayer(fixture, map, 6, 5, "Victim", 202, 3);
        map.SetCharacter(victim, 6, 5);

        effect.Cast(caster, caster, fixture.World);

        Assert.Equal(990, victim.CurrentHP);
    }

    [Fact]
    public void Cast_applies_same_facing_bonus_after_formula_processing()
    {
        using var fixture = CreateFixture();
        var (map, effect, caster) = CreateScenario(fixture, 2);
        var victim = CreatePlayer(fixture, map, 6, 5, "Victim", 202, 2);
        map.SetCharacter(victim, 6, 5);

        effect.Cast(caster, caster, fixture.World);

        Assert.Equal(980, victim.CurrentHP);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-number")]
    [InlineData("0")]
    [InlineData("-3")]
    public void Cast_falls_back_to_multiplier_two_for_invalid_or_non_positive_params(string? scriptParams)
    {
        using var fixture = CreateFixture();
        var (map, effect, caster) = CreateScenario(fixture, 2, scriptParams);
        var victim = CreatePlayer(fixture, map, 6, 5, "Victim", 202, 3);
        map.SetCharacter(victim, 6, 5);

        effect.Cast(caster, caster, fixture.World);

        Assert.Equal(987, victim.CurrentHP);
    }

    [Fact]
    public void Cast_rejects_an_ineligible_player_but_still_sends_animation()
    {
        using var fixture = CreateFixture();
        var (map, effect, caster) = CreateScenario(fixture, 2);
        effect.Effected = SpellEffect.SpellEffected.NPC;
        var victim = CreatePlayer(fixture, map, 6, 5, "Victim", 202, 3);
        map.SetCharacter(victim, 6, 5);

        var result = effect.Cast(caster, caster, fixture.World);

        Assert.True(result);
        Assert.Equal(1000, victim.CurrentHP);
        Assert.Contains(ExpectedAnimation(caster, 6, 5, effect), caster.Sent);
    }

    [Fact]
    public void Cast_sends_animation_to_caster_and_nearby_players_on_an_empty_tile()
    {
        using var fixture = CreateFixture();
        var (map, effect, caster) = CreateScenario(fixture, 2);
        var observer = CreatePlayer(fixture, map, 4, 5, "Observer", 303, 1);
        map.AddPlayer(caster, fixture.World);
        map.AddPlayer(observer, fixture.World);

        var result = effect.Cast(caster, caster, fixture.World);
        var packet = ExpectedAnimation(caster, 6, 5, effect);

        Assert.True(result);
        Assert.Contains(packet, caster.Sent);
        Assert.Contains(packet, observer.Sent);
    }

    [Fact]
    public void Cast_sends_animation_for_an_out_of_bounds_front_tile()
    {
        using var fixture = CreateFixture();
        var (map, effect, caster) = CreateScenario(fixture, 1);
        caster.MapX = 1;
        caster.MapY = 1;

        var result = effect.Cast(caster, caster, fixture.World);

        Assert.True(result);
        Assert.Contains(ExpectedAnimation(caster, 1, 0, effect), caster.Sent);
    }

    private static TestWorldFixture CreateFixture()
    {
        return new TestWorldFixture(settings => settings.DamageModifier = 1m);
    }

    private static (Map Map, SpellEffect Effect, TestWorldFixture.CapturingPlayer Caster) CreateScenario(
        TestWorldFixture fixture,
        int facing,
        string? scriptParams = "2")
    {
        var map = fixture.AddBaseMap(1, "Test");
        map.CanCast = true;
        map.CanPVP = true;
        var caster = CreatePlayer(fixture, map, 5, 5, "Caster", 101, facing);
        caster.Level = 10;
        caster.MaxStats.Strength = 9;
        var scriptBody = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "SpellScripts", "Backstab.csx"));
        var effect = fixture.AddBaseSpellEffect(1, "Backstab", e =>
        {
            e.EffectType = SpellEffect.EffectTypes.Script;
            e.TargetType = SpellEffect.TargetTypes.Target;
            e.Effected = SpellEffect.SpellEffected.NPCPlayer;
            e.MinimumLevelEffected = 0;
            e.MaximumLevelEffected = 99;
            e.WorksInPVP = true;
            e.Animation = 12;
            e.AnimationFile = 3;
            e.ScriptParams = scriptParams!;
            e.Script = fixture.CompileSpellEffectScript(scriptBody, "Backstab.csx");
        });
        return (map, effect, caster);
    }

    private static TestWorldFixture.CapturingPlayer CreatePlayer(
        TestWorldFixture fixture,
        Map map,
        int x,
        int y,
        string name,
        int loginId,
        int facing)
    {
        var player = fixture.CommandPlayerOn(map, x, y, name);
        player.LoginID = loginId;
        player.Facing = facing;
        player.Level = 10;
        player.MaxStats.HP = 1000;
        player.MaxStats.MP = 100;
        player.MaxStats.Dexterity = -1;
        player.CurrentHP = 1000;
        player.CurrentMP = 100;
        return player;
    }

    private static NPC CreateNpc(Map map, int x, int y, int facing)
    {
        var npc = new NPC
        {
            LoginID = 404,
            Name = "NPC",
            Map = map,
            MapID = map.ID,
            MapX = x,
            MapY = y,
            Facing = facing,
            Level = 10,
            BaseStats = new AttributeSet(),
            MaxStats = new AttributeSet { HP = 1000, MP = 100 },
            NPCTemplate = new NPCTemplate(),
            CanBeKilled = true,
            Buffs = [],
            AggroTargetToValue = []
        };
        npc.CurrentHP = 1000;
        npc.CurrentMP = 100;
        return npc;
    }

    private static int DifferentFacing(int facing)
    {
        return facing == 4 ? 1 : facing + 1;
    }

    private static string ExpectedAnimation(ICharacter caster, int x, int y, SpellEffect effect)
    {
        return $"ATT{caster.LoginID}\x1SPA{x},{y},{effect.Animation},{effect.AnimationFile}\x1";
    }
}
