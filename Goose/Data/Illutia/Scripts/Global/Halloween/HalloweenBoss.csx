#load "HalloweenConfig.csx"
#load "HalloweenCommon.csx"
using System;
using System.Collections.Generic;
using Goose;

public class BossState
{
    public Dictionary<int, BleedEntry> Bleeds { get; } = new();
}

public class BleedEntry
{
    public int Stacks { get; set; }
    public long ExpiresAt { get; set; }
}

public static class HalloweenBoss
{
    public static long PercentHit(NPC npc, Player target, double percent, GameWorld world)
    {
        // Bypasses AC and damage reduction on purpose: NPC.Attack applies them, but boss
        // hits are a fixed % of max HP.
        return PercentHit(npc, target, percent, world, sendPacket: true);
    }

    private static long PercentHit(NPC npc, Player target, double percent, GameWorld world, bool sendPacket)
    {
        long damage = (long)Math.Ceiling(target.MaxHP * percent);
        if (sendPacket)
        {
            string packet = P.Attack(npc);
            foreach (var player in npc.Map.GetPlayersInRange(npc))
                world.Send(player, packet);
        }
        target.Attacked(npc, damage, world);
        return damage;
    }

    public static bool IsStraggler(Player target, double radius)
    {
        if (target.Map is null) return true;
        return PlayersNear(target.Map, target.MapX, target.MapY, radius, target.PlayerID).Count == 0;
    }

    public static void AddBleed(NPC npc, Player target, long now, GameWorld world)
    {
        var cfg = HalloweenConfig.Get(world);
        var state = State(npc);
        if (state.Bleeds.TryGetValue(target.PlayerID, out var entry))
        {
            entry.Stacks = Math.Min(entry.Stacks + 1, cfg.Weeper.BleedMaxStacks);
        }
        else
        {
            entry = new BleedEntry { Stacks = 1 };
            state.Bleeds[target.PlayerID] = entry;
        }
        entry.ExpiresAt = now + Halloween.Ticks(world, cfg.Weeper.BleedSeconds);
    }

    public static void TickBleeds(NPC npc, long now, GameWorld world)
    {
        var cfg = HalloweenConfig.Get(world);
        var state = State(npc);
        foreach (var (playerId, entry) in state.Bleeds.ToList())
        {
            if (entry.ExpiresAt <= now)
            {
                state.Bleeds.Remove(playerId);
                continue;
            }
            var target = npc.Map.Players.FirstOrDefault(p => p.PlayerID == playerId);
            if (target is null || target.State != Player.States.Ready)
            {
                state.Bleeds.Remove(playerId);
                continue;
            }
            PercentHit(npc, target, entry.Stacks * cfg.Weeper.BleedPercentPerStack, world, sendPacket: false);
            if (target.State != Player.States.Ready || target.Map != npc.Map)
                state.Bleeds.Remove(playerId);
        }
    }

    public static string Compass(int fromX, int fromY, int toX, int toY)
    {
        int dx = toX - fromX;
        int dy = toY - fromY;
        // +y is north on Illutia maps (see Halloween.SpawnTile's Up => (0, 1)).
        string? northSouth = dy > 0 ? "north" : dy < 0 ? "south" : null;
        string? eastWest = dx > 0 ? "east" : dx < 0 ? "west" : null;
        if (northSouth is null) return eastWest ?? "north";
        if (eastWest is null) return northSouth;
        return northSouth + "-" + eastWest;
    }

    public static List<Player> PlayersNear(NPC npc, double radius)
        => PlayersNear(npc.Map, npc.MapX, npc.MapY, radius, -1);

    private static List<Player> PlayersNear(Map map, int x, int y, double radius, int excludePlayerId)
    {
        List<Player> result = [];
        foreach (var player in map.Players)
        {
            if (player.State != Player.States.Ready || player.IsGM) continue;
            if (player.PlayerID == excludePlayerId) continue;
            if (Math.Max(Math.Abs(player.MapX - x), Math.Abs(player.MapY - y)) <= radius)
                result.Add(player);
        }
        return result;
    }

    private static BossState State(NPC npc)
    {
        if (npc.ScriptStore is BossState state) return state;
        state = new BossState();
        npc.ScriptStore = state;
        return state;
    }
}
