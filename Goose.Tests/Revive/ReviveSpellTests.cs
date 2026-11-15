using Goose;
using Xunit;

namespace Goose.Tests.Revive;

public class ReviveSpellTests
{
    private static SpellEffect ReviveEffect(ReviveTestKit kit) => kit.Fixture.AddBaseSpellEffect(900, "Revive", e =>
    {
        e.EffectType = SpellEffect.EffectTypes.Revive;
        e.Effected = SpellEffect.SpellEffected.Player;
        e.WorksNotInPVP = true;
    });

    [Fact]
    public void Revive_stands_the_corpse_up_in_place_at_half_hp_and_mp()
    {
        using var kit = new ReviveTestKit();
        var healer = kit.PlayerAt(4, 5, "Healer");
        var corpse = kit.PlayerAt(5, 5, "Corpse");
        kit.Kill(corpse);

        Assert.True(ReviveEffect(kit).Cast(healer, corpse, kit.World));

        Assert.False(corpse.IsDead);
        Assert.Null(corpse.PendingRespawnTimeout);
        Assert.Null(corpse.DeathRespawn);
        Assert.Same(kit.Map, corpse.Map);
        Assert.Equal(5, corpse.MapX);
        Assert.Equal(5, corpse.MapY);
        Assert.Equal(50, corpse.CurrentHP);
        Assert.Equal(50, corpse.CurrentMP);
    }

    [Fact]
    public void Revive_notifies_only_the_revived_player()
    {
        using var kit = new ReviveTestKit();
        var healer = kit.PlayerAt(4, 5, "Healer");
        var corpse = kit.PlayerAt(5, 5, "Corpse");
        kit.Kill(corpse);
        healer.Sent.Clear();

        ReviveEffect(kit).Cast(healer, corpse, kit.World);

        Assert.Contains(corpse.Sent, s => s.Contains(P.Notice(NoticeType.Info, "Healer revived you.")));
        Assert.DoesNotContain(healer.Sent, s => s.Contains("revived"));
    }

    [Fact]
    public void Revive_broadcasts_chp()
    {
        using var kit = new ReviveTestKit();
        var healer = kit.PlayerAt(4, 5, "Healer");
        var corpse = kit.PlayerAt(5, 5, "Corpse");
        kit.Kill(corpse);
        healer.Sent.Clear();

        ReviveEffect(kit).Cast(healer, corpse, kit.World);

        Assert.Contains(healer.Sent, s => s.StartsWith("CHP" + corpse.LoginID + ","));
    }

    [Fact]
    public void Revive_on_a_living_player_fails()
    {
        using var kit = new ReviveTestKit();
        var healer = kit.PlayerAt(4, 5, "Healer");
        var target = kit.PlayerAt(5, 5, "Target");
        target.CurrentHP = 20;

        Assert.False(ReviveEffect(kit).Cast(healer, target, kit.World));
        Assert.Equal(20, target.CurrentHP);
    }

    [Fact]
    public void Revive_on_an_npc_fails()
    {
        using var kit = new ReviveTestKit();
        var healer = kit.PlayerAt(4, 5, "Healer");
        var effect = ReviveEffect(kit);
        effect.Effected = SpellEffect.SpellEffected.NPCPlayer;

        Assert.False(effect.Cast(healer, kit.Killer, kit.World));
    }

    [Fact]
    public void Other_effects_do_nothing_to_a_corpse()
    {
        using var kit = new ReviveTestKit();
        var healer = kit.PlayerAt(4, 5, "Healer");
        var corpse = kit.PlayerAt(5, 5, "Corpse");
        kit.Kill(corpse);
        var buff = kit.Fixture.AddBaseSpellEffect(901, "Shield", e =>
        {
            e.EffectType = SpellEffect.EffectTypes.Buff;
            e.Effected = SpellEffect.SpellEffected.Player;
            e.WorksNotInPVP = true;
            e.Duration = 1000;
        });

        Assert.False(buff.Cast(healer, corpse, kit.World));
        Assert.Empty(corpse.Buffs);
        Assert.True(corpse.IsDead);
    }
}
