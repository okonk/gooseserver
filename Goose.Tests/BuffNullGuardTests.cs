using Goose.Testing;

namespace Goose.Tests;

public class BuffNullGuardTests
{
    [Fact]
    public void AddBuff_ZeroDurationTickEffect_DoesNotThrow()
    {
        using var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(1, "m");
        var player = fixture.CommandPlayerOn(map, 1, 1);
        var effect = fixture.AddBaseSpellEffect(1, "tick0",
            e => { e.EffectType = SpellEffect.EffectTypes.Tick; e.Duration = 0; });
        var buff = new Buff { Caster = player, Target = player, SpellEffect = effect };

        player.AddBuff(buff, fixture.World);

        Assert.Contains(buff, player.Buffs);
    }

    [Fact]
    public void AddBuff_ZeroDurationTickEffect_Npc_DoesNotThrow()
    {
        using var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(1, "m");
        var template = new NPCTemplate
        {
            NPCTemplateID = 1,
            Name = "Test NPC",
            Level = 5,
            ClassID = 1,
            BaseStats = new AttributeSet(),
        };
        var npc = fixture.World.NPCHandler.SpawnNPC(fixture.World, 1, 3, 3, template, false)!;
        var effect = fixture.AddBaseSpellEffect(1, "tick0",
            e => { e.EffectType = SpellEffect.EffectTypes.Tick; e.Duration = 0; });
        var buff = new Buff { Caster = npc, Target = npc, SpellEffect = effect };

        npc.AddBuff(buff, fixture.World);

        Assert.Contains(buff, npc.Buffs);
    }

    [Fact]
    public void OnMeleeHit_NullLinkedSpell_DoesNotThrow()
    {
        using var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(1, "m");
        var player = fixture.CommandPlayerOn(map, 1, 1);
        var effect = fixture.AddBaseSpellEffect(1, "onhit",
            e => { e.EffectType = SpellEffect.EffectTypes.OnMeleeHit; e.OnMeleeHitSpellChance = 100m; });
        player.Buffs.Add(new Buff { Caster = player, Target = player, SpellEffect = effect });

        player.OnMeleeHit(player, fixture.World);

        Assert.Single(player.Buffs);
    }

    [Fact]
    public void OnMeleeAttack_NullLinkedSpell_DoesNotThrow()
    {
        using var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(1, "m");
        var player = fixture.CommandPlayerOn(map, 1, 1);
        var effect = fixture.AddBaseSpellEffect(1, "onattack",
            e => { e.EffectType = SpellEffect.EffectTypes.OnAttack; e.OnMeleeAttackSpellChance = 100m; });
        player.Buffs.Add(new Buff { Caster = player, Target = player, SpellEffect = effect });

        player.OnMeleeAttack(player, fixture.World);

        Assert.Single(player.Buffs);
    }

    [Fact]
    public void Cast_ScriptEffectWithoutScript_ReturnsFalse()
    {
        using var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(1, "m");
        var player = fixture.CommandPlayerOn(map, 1, 1);
        var effect = fixture.AddBaseSpellEffect(1, "scriptless",
            e => e.EffectType = SpellEffect.EffectTypes.Script);

        Assert.False(effect.Cast(player, player, fixture.World));
    }
}
