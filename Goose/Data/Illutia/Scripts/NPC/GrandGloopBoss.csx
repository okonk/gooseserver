using Goose;
using Goose.Events;
using Goose.Scripting;
using System;
using System.Linq;

public class GrandGloopBoss : BaseNPCScript
{
    private const int MarkAnimation = 65020;
    private const int StrikeAnimation = 815033;

    private const long StrikeDamage = 20000;
    private const double StrikeCooldown = 5;
    private const double MarkCooldown = 8;
    private const double MarkDelay = 2.5;
    private const double MarkRepeat = 1.5;
    private const int FrozenEffect = 315;

    private class BossState
    {
        public long NextStrike;
        public long NextMark;
        public bool MarkPending;
    }

    public override void OnSpawnEvent(NPC npc, GameWorld world)
    {
        npc.ScriptStore = new BossState();
    }

    public override void OnAttackEvent(NPC npc, GameWorld world)
    {
        if (npc.ScriptStore is not BossState state)
            npc.ScriptStore = state = new BossState();

        if (npc.AggroTarget != null &&
            !npc.Buffs.Any(b => b.SpellEffect.EffectType == SpellEffect.EffectTypes.Stun))
        {
            if (npc.CurrentHP * 2 > npc.MaxStats.HP)
                TryIceStrike(npc, state, world);
            else
                TryMark(npc, state, world);
        }

        npc.HandleAttackEvent(world);
    }

    private void TryIceStrike(NPC npc, BossState state, GameWorld world)
    {
        if (world.TimeNow < state.NextStrike) return;

        var target = RandomPlayer(npc, world);
        if (target == null) return;

        state.NextStrike = world.TimeNow + (long)(StrikeCooldown * world.TimerFrequency);

        var packet = P.SpellPlayer(target.LoginID, StrikeAnimation, 0);
        foreach (var p in npc.Map.GetPlayersInRange(npc))
            world.Send(p, packet);

        target.Attacked(npc, StrikeDamage, world);
    }

    private void TryMark(NPC npc, BossState state, GameWorld world)
    {
        if (state.MarkPending || world.TimeNow < state.NextMark) return;

        var target = RandomPlayer(npc, world);
        if (target == null) return;

        state.MarkPending = true;
        state.NextMark = world.TimeNow + (long)(MarkCooldown * world.TimerFrequency);

        var map = npc.Map;
        var tiles = new[] { (0, 0), (-1, -1), (1, -1), (-1, 1), (1, 1) }
            .Select(d => (X: target.MapX + d.Item1, Y: target.MapY + d.Item2))
            .Where(t => t.X >= 1 && t.X <= map.Width && t.Y >= 1 && t.Y <= map.Height)
            .ToList();

        world.Send(target, P.ServerMessage($"{npc.Name} fixes its gaze on you. Move!"));

        void ShowMark()
        {
            var mark = string.Join("\x1", tiles.Select(t => P.SpellTile(t.X, t.Y, MarkAnimation, 0)));
            foreach (var p in map.GetPlayersInRange(npc))
                world.Send(p, mark);
        }

        ShowMark();

        ScriptTimerEvent.Create(() =>
        {
            if (npc.State == NPC.States.Alive && npc.Map == map)
                ShowMark();
        }, TimeSpan.FromSeconds(MarkRepeat), world);

        ScriptTimerEvent.Create(() =>
        {
            state.MarkPending = false;

            if (npc.State != NPC.States.Alive || npc.Map != map) return;

            var frozen = world.SpellHandler.GetSpellEffect(FrozenEffect);
            if (frozen == null) return;

            foreach (var (x, y) in tiles)
            {
                if (map.GetCharacterAt(x, y) is not Player victim || victim.State != Player.States.Ready) continue;
                if (!frozen.Cast(npc, victim, world)) continue;

                var packet = P.SpellPlayer(victim.LoginID, frozen.Animation, frozen.AnimationFile);
                world.Send(victim, packet);
                foreach (var p in map.GetPlayersInRange(victim))
                    world.Send(p, packet);
            }
        }, TimeSpan.FromSeconds(MarkDelay), world);
    }

    private static Player RandomPlayer(NPC npc, GameWorld world)
    {
        var players = npc.Map.GetPlayersInRange(npc)
            .Where(p => p.State == Player.States.Ready)
            .ToList();

        return players.Count == 0 ? null : players[world.Random.Next(players.Count)];
    }
}

return typeof(GrandGloopBoss);
