#load "ArenaBoss.csx"
#load "ArenaConfig.csx"
#load "ArenaMath.csx"
#load "ArenaSpells.csx"
using System;
using Goose;
using Goose.Scripting;

public class ArenaMob : BaseNPCScript
{
    public override void OnSpawnEvent(NPC npc, GameWorld world)
    {
        if (!npc.Properties.TryGetProperty<string>("arena.run", out _)) return;
        var cfg = ArenaConfig.Get(world);
        int round = npc.Properties.GetProperty<int>("arena.round", 1);
        MobTier tier = (MobTier)npc.Properties.GetProperty<int>("arena.tier", 0);
        int alive = npc.Properties.GetProperty<int>("arena.alive", 1);
        npc.TemporaryMaxHP = ArenaMath.MobHp(round, tier, alive, cfg);
        if (cfg.XpAnchor > 0) npc.Experience = ArenaMath.MobXp(round, tier, cfg);
        if (tier == MobTier.Boss) ArenaBoss.OnSpawn(npc, world, cfg);
        if (tier == MobTier.Elite && cfg.EliteTint is { Length: >= 4 })
        {
            npc.Title = cfg.EliteTitle;
            npc.BodyR = cfg.EliteTint[0];
            npc.BodyG = cfg.EliteTint[1];
            npc.BodyB = cfg.EliteTint[2];
            npc.BodyA = cfg.EliteTint[3];
        }
    }

    public override long InterceptDamageTaken(NPC npc, ICharacter attacker, long damage, GameWorld world)
    {
        if (!npc.Properties.TryGetProperty<string>("arena.run", out _)) return damage;
        bool boss = ArenaBoss.IsBoss(npc);
        if (boss && ArenaBoss.IsShielded(npc)) return 0;
        if (attacker is not Player player) return damage;
        var cfg = ArenaConfig.Get(world);
        if (!cfg.Valid) return damage;
        long eff = ArenaMath.EffectiveDamage(damage, ArenaMath.PowerFraction(player.ExperienceSold, cfg), cfg);
        if (boss) eff = ArenaBoss.CapForSummon(npc, eff, cfg);
        string key = "arena.dmg." + player.PlayerID;
        long dealt = Math.Min(eff, Math.Max(0, npc.CurrentHP));
        if (dealt > 0) npc.Properties[key] = npc.Properties.GetProperty<long>(key, 0) + dealt;
        if (boss) ArenaBoss.CheckSummon(npc, npc.CurrentHP - eff, world, cfg);
        return eff;
    }

    public override void OnMoveEvent(NPC npc, GameWorld world)
    {
        if (ArenaBoss.IsBoss(npc))
        {
            if (ArenaBoss.IsBusy(npc, world))
            {
                npc.AddMoveEvent(world);
                return;
            }
            ArenaBoss.HoldTarget(npc, world);
        }
        npc.HandleMoveEvent(world);
    }

    public override void OnAttackEvent(NPC npc, GameWorld world)
    {
        if (ArenaBoss.IsBoss(npc))
        {
            var cfg = ArenaConfig.Get(world);
            if (cfg.Valid && (ArenaBoss.IsBusy(npc, world) || ArenaBoss.Think(npc, world, cfg)))
            {
                npc.AddAttackEvent(world);
                return;
            }
            ArenaBoss.HoldTarget(npc, world);
        }
        if (npc.Properties.TryGetProperty<string>("arena.run", out _) && npc.AggroTarget is not null)
        {
            var cfg = ArenaConfig.Get(world);
            if (cfg.Valid && ArenaSpells.TryCast(npc, world, cfg))
            {
                npc.AddAttackEvent(world);
                return;
            }
        }
        npc.HandleAttackEvent(world);
    }

    public override double MeleeBaseDamage(NPC npc, ICharacter target, double baseDamage, GameWorld world)
    {
        if (target is not Player player || !npc.Properties.TryGetProperty<string>("arena.run", out _))
            return baseDamage;
        var cfg = ArenaConfig.Get(world);
        if (!cfg.Valid) return baseDamage;
        int round = npc.Properties.GetProperty<int>("arena.round", 1);
        MobTier tier = (MobTier)npc.Properties.GetProperty<int>("arena.tier", 0);
        return ArenaMath.MobWeaponDamage(round, tier, ArenaMath.PowerFraction(player.ExperienceSold, cfg), cfg);
    }
}

return typeof(ArenaMob);
