using System;
using System.Collections.Generic;
using System.Linq;
using Goose;

public static class ArenaSpells
{
    private const string CooldownPrefix = "arena.cd.";

    public static string[] SpellNames(NPC npc)
    {
        string parameters = npc.ScriptParams ?? "";
        int at = parameters.IndexOf("spells=", StringComparison.OrdinalIgnoreCase);
        if (at < 0) return [];
        return parameters.Substring(at + "spells=".Length)
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    public static bool TryCast(NPC npc, GameWorld world, ArenaConfig cfg)
    {
        if (npc.Map is null || npc.Hidden) return false;
        if (npc.Buffs.Any(b => b.SpellEffect.EffectType == SpellEffect.EffectTypes.Stun)) return false;

        foreach (var name in SpellNames(npc))
        {
            if (!cfg.MobSpells.TryGetValue(name, out var tuning)) continue;
            if (world.TimeNow < npc.Properties.GetProperty<long>(CooldownPrefix + name, 0)) continue;
            var effect = world.SpellHandler.GetSpellByName(name)?.SpellEffect;
            if (effect is null) continue;

            bool cast = tuning.Heal > 0
                ? Heal(npc, world, effect, tuning)
                : Strike(npc, world, cfg, effect, tuning);
            if (!cast) continue;

            npc.Properties[CooldownPrefix + name] = world.TimeNow + (long)(tuning.Cooldown * world.TimerFrequency);
            return true;
        }
        return false;
    }

    public static List<(int X, int Y)> FrontTiles(SpellEffect.TargetTypes shape, int size, int ox, int oy, Direction facing)
    {
        var tiles = new List<(int X, int Y)>();
        for (int i = 1; i <= Math.Max(1, size); i++)
        {
            int width = shape == SpellEffect.TargetTypes.TriangleFront ? i - 1 : 0;
            for (int j = -width; j <= width; j++)
                tiles.Add(facing switch
                {
                    Direction.Up => (ox + j, oy - i),
                    Direction.Down => (ox + j, oy + i),
                    Direction.Left => (ox - i, oy + j),
                    _ => (ox + i, oy + j),
                });
        }
        return tiles;
    }

    private static bool Strike(NPC npc, GameWorld world, ArenaConfig cfg, SpellEffect effect, ArenaMobSpell tuning)
    {
        var map = npc.Map;
        List<(int X, int Y)> tiles;
        if (effect.TargetType is SpellEffect.TargetTypes.LineFront or SpellEffect.TargetTypes.TriangleFront)
        {
            var aimed = AimFront(npc, effect, map);
            if (aimed is null) return false;
            npc.FaceTo(aimed.Value, world);
            tiles = FrontTiles(effect.TargetType, effect.TargetSize, npc.MapX, npc.MapY, aimed.Value);
        }
        else
        {
            if (npc.AggroTarget is not Player target || target.Map != map) return false;
            if (Chebyshev(npc.MapX, npc.MapY, target.MapX, target.MapY) > Math.Max(1, tuning.Range)) return false;
            tiles = effect.TargetType == SpellEffect.TargetTypes.Plus
                ? PlusTiles(target.MapX, target.MapY, effect.TargetSize)
                : [(target.MapX, target.MapY)];
        }

        npc.BreakInvisibility(world);
        var packets = new List<string>();
        if (effect.DoAttackAnimation) packets.Add(P.Attack(npc));
        if (effect.DoCastAnimation) packets.Add(P.Cast(npc));
        var hits = new List<Player>();
        bool single = effect.TargetType == SpellEffect.TargetTypes.Target;
        foreach (var (x, y) in tiles)
        {
            if (map.GetCharacterAt(x, y) is Player hit) hits.Add(hit);
            if (!single && effect.Animation != 0) packets.Add(P.SpellTile(x, y, effect.Animation, effect.AnimationFile));
        }
        if (single && effect.Animation != 0 && hits.Count > 0)
            packets.Add(P.SpellPlayer(hits[0].LoginID, effect.Animation, effect.AnimationFile));
        SendToRange(npc, world, packets);

        int round = npc.Properties.GetProperty<int>("arena.round", 1);
        var tier = (MobTier)npc.Properties.GetProperty<int>("arena.tier", 0);
        foreach (var player in hits)
        {
            if (player.Map != map) continue;
            double weapon = ArenaMath.MobWeaponDamage(round, tier, ArenaMath.PowerFraction(player.ExperienceSold, cfg), cfg);
            long damage = (long)npc.MitigateMelee(player, weapon * tuning.Damage, world);
            if (damage > 0) player.Attacked(npc, damage, world);
        }
        return true;
    }

    private static Direction? AimFront(NPC npc, SpellEffect effect, Map map)
    {
        var order = new List<Direction> { npc.Facing };
        foreach (var d in new[] { Direction.Up, Direction.Right, Direction.Down, Direction.Left })
            if (d != npc.Facing) order.Add(d);
        foreach (var facing in order)
            foreach (var (x, y) in FrontTiles(effect.TargetType, effect.TargetSize, npc.MapX, npc.MapY, facing))
                if (map.GetCharacterAt(x, y) is Player) return facing;
        return null;
    }

    private static bool Heal(NPC npc, GameWorld world, SpellEffect effect, ArenaMobSpell tuning)
    {
        if (!npc.Properties.TryGetProperty<string>("arena.run", out var runId)) return false;
        int range = Math.Max(1, tuning.Range);
        var patient = npc.Map.NPCs
            .Where(n => n.State == NPC.States.Alive && !n.Hidden && n.MaxHP > 0 &&
                        n.Properties.TryGetProperty<string>("arena.run", out var tag) && tag == runId &&
                        Chebyshev(npc.MapX, npc.MapY, n.MapX, n.MapY) <= range &&
                        (double)n.CurrentHP / n.MaxHP < tuning.HealBelow)
            .OrderBy(n => (double)n.CurrentHP / n.MaxHP)
            .FirstOrDefault();
        if (patient is null) return false;

        long healed = Math.Min((long)(patient.MaxHP * tuning.Heal), patient.MaxHP - patient.CurrentHP);
        if (healed <= 0) return false;
        patient.CurrentHP += healed;

        var packets = new List<string> { P.Cast(npc), P.BattleTextHeal(patient, -healed), P.VitalsPercentage(patient) };
        if (effect.Animation != 0) packets.Add(P.SpellPlayer(patient.LoginID, effect.Animation, effect.AnimationFile));
        SendToRange(patient, world, packets);
        return true;
    }

    private static List<(int X, int Y)> PlusTiles(int ox, int oy, int size)
    {
        var tiles = new List<(int X, int Y)> { (ox, oy) };
        for (int i = 1; i <= size; i++)
            tiles.AddRange([(ox, oy - i), (ox + i, oy), (ox, oy + i), (ox - i, oy)]);
        return tiles;
    }

    private static void SendToRange(ICharacter origin, GameWorld world, List<string> packets)
    {
        string packet = string.Join("\x1", packets.Where(p => p.Length > 0));
        if (packet.Length == 0) return;
        foreach (var player in origin.Map.GetPlayersInRange(origin))
            world.Send(player, packet);
    }

    private static int Chebyshev(int x1, int y1, int x2, int y2)
        => Math.Max(Math.Abs(x1 - x2), Math.Abs(y1 - y2));
}
