using Goose.Testing;

namespace Goose.IntegrationTests;

public class ShardOfInvincibilityScriptTests
{
    [Fact]
    public void ShardBuff_MeleeHit_DealsNoDamageAndSendsMiss()
    {
        using var fixture = CreateFixture();
        var map = fixture.AddBaseMap(1, "Test");
        var victim = CreatePlayer(fixture, map, 5, 5, "Victim");
        AddShardBuff(victim, fixture);
        var attacker = CreateNpc(map, 4, 5);

        victim.Attacked(attacker, 100, fixture.World);

        Assert.Equal(1000, victim.CurrentHP);
        Assert.Contains(P.BattleTextMiss(victim) + "\x01", victim.Sent);
    }

    [Fact]
    public void NoShardBuff_MeleeHit_DealsFullDamage()
    {
        using var fixture = CreateFixture();
        var map = fixture.AddBaseMap(1, "Test");
        var victim = CreatePlayer(fixture, map, 5, 5, "Victim");
        var attacker = CreateNpc(map, 4, 5);

        victim.Attacked(attacker, 100, fixture.World);

        Assert.Equal(900, victim.CurrentHP);
        Assert.Contains(victim.Sent, s => s.StartsWith(P.BattleTextDamage(victim, 100)));
    }

    [Fact]
    public void ShardBuff_Heal_PassesThrough()
    {
        using var fixture = CreateFixture();
        var map = fixture.AddBaseMap(1, "Test");
        var victim = CreatePlayer(fixture, map, 5, 5, "Victim");
        victim.MaxStats.HP = 2000;
        AddShardBuff(victim, fixture);
        var attacker = CreateNpc(map, 4, 5);

        victim.Attacked(attacker, -50, fixture.World);

        Assert.Equal(1050, victim.CurrentHP);
        Assert.Contains(victim.Sent, s => s.StartsWith(P.BattleTextHeal(victim, -50)));
    }

    [Fact]
    public void ShardBuff_NpcTarget_DealsNoDamageAndSendsMiss()
    {
        using var fixture = CreateFixture();
        var map = fixture.AddBaseMap(1, "Test");
        var npc = CreateNpc(map, 5, 5);
        map.SetCharacter(npc, 5, 5);
        AddShardBuff(npc, fixture);
        var attacker = CreatePlayer(fixture, map, 4, 5, "Attacker");
        map.SetCharacter(attacker, 4, 5);
        map.AddPlayer(attacker, fixture.World);

        npc.Attacked(attacker, 100, fixture.World);

        Assert.Equal(1000, npc.CurrentHP);
        Assert.Contains(P.BattleTextMiss(npc) + "\x01", attacker.Sent);
    }

    [Fact]
    public void ShardBuff_GetItemDescription_ReturnsCustomLine()
    {
        using var fixture = CreateFixture();
        var map = fixture.AddBaseMap(1, "Test");
        var victim = CreatePlayer(fixture, map, 5, 5, "Victim");
        var effect = AddShardBuff(victim, fixture);

        Assert.Contains("Reduces all damage taken to 0.", effect.GetItemDescription(fixture.World));
    }

    [Fact]
    public void ShardBuff_EndToEnd_PlayerAttack_DealsNoDamage()
    {
        using var fixture = CreateFixture();
        var map = fixture.AddBaseMap(1, "Test");
        map.CanPVP = true;
        var victim = CreatePlayer(fixture, map, 5, 5, "Victim");
        AddShardBuff(victim, fixture);
        var attacker = CreatePlayer(fixture, map, 4, 5, "Attacker");
        attacker.MaxStats.Strength = 10;

        attacker.Attack(victim, fixture.World);

        Assert.Equal(1000, victim.CurrentHP);
        Assert.Contains(P.BattleTextMiss(victim) + "\x01", victim.Sent);
    }

    [Fact]
    public void EndToEnd_PlayerAttack_NoShard_DealsPvpScaledDamage()
    {
        using var fixture = CreateFixture();
        var map = fixture.AddBaseMap(1, "Test");
        map.CanPVP = true;
        var victim = CreatePlayer(fixture, map, 5, 5, "Victim");
        var attacker = CreatePlayer(fixture, map, 4, 5, "Attacker");
        attacker.MaxStats.Strength = 10;

        attacker.Attack(victim, fixture.World);

        Assert.Equal(997, victim.CurrentHP);
    }

    private static TestWorldFixture CreateFixture()
    {
        return new TestWorldFixture(settings =>
        {
            settings.DamageModifier = 1.0;
            settings.MaxAC = 100;
        });
    }

    private static TestWorldFixture.CapturingPlayer CreatePlayer(
        TestWorldFixture fixture, Map map, int x, int y, string name)
    {
        var player = fixture.CommandPlayerOn(map, x, y, name);
        player.LoginID = 202;
        player.Level = 10;
        player.MaxStats.HP = 1000;
        player.MaxStats.Dexterity = -1;
        player.CurrentHP = 1000;
        return player;
    }

    private static NPC CreateNpc(Map map, int x, int y)
    {
        var npc = new NPC
        {
            LoginID = 404,
            Name = "NPC",
            Map = map,
            MapID = map.ID,
            MapX = x,
            MapY = y,
            Facing = Direction.Down,
            Level = 10,
            BaseStats = new AttributeSet(),
            MaxStats = new AttributeSet { HP = 1000 },
            NPCTemplate = new NPCTemplate(),
            CanBeKilled = true,
            Buffs = [],
            AggroTargetToValue = []
        };
        npc.CurrentHP = 1000;
        return npc;
    }

    private static SpellEffect AddShardBuff(ICharacter target, TestWorldFixture fixture)
    {
        var scriptBody = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "SpellScripts", "ShardOfInvincibility.csx"));
        var effect = fixture.AddBaseSpellEffect(192, "Shard of Invincibility", e =>
        {
            e.EffectType = SpellEffect.EffectTypes.Buff;
            e.Script = fixture.CompileSpellEffectScript(scriptBody, "ShardOfInvincibility.csx");
        });
        target.Buffs.Add(new Buff { Caster = target, Target = target, SpellEffect = effect });
        return effect;
    }
}
