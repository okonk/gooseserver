#load "../Global/Halloween/HalloweenConfig.csx"
#load "../Global/Halloween/HalloweenCommon.csx"
using System;
using System.Linq;
using Goose;
using Goose.Scripting;

public class Stalker : BaseNPCScript
{
    public override void OnSpawnEvent(NPC npc, GameWorld world)
    {
        npc.MoveSpeed = HalloweenConfig.Get(world).StalkerChase.StartDelaySeconds;
    }

    public override void OnMoveEvent(NPC npc, GameWorld world)
    {
        if (!MoveStep(npc, world.TimeNow, world))
            npc.AddMoveEvent(world);
    }

    public bool MoveStep(NPC npc, long now, GameWorld world)
    {
        var cfg = HalloweenConfig.Get(world);
        if (npc.State != NPC.States.Alive) return true;
        if (!npc.Properties.TryGetProperty<int>("stalkerTarget", out int targetId) ||
            !npc.Properties.TryGetProperty<string>("stalkerMode", out var mode))
            return EndChase(npc, null, null, false, now, world);
        var target = npc.Map.Players.OfType<Player>().FirstOrDefault(p => p.PlayerID == targetId && p.State == Player.States.Ready);
        if (target is null) return EndChase(npc, null, null, false, now, world);

        if (mode is "chase")
        {
            if (Halloween.RoomAt(cfg, target.MapX, target.MapY)?.Name is "Foyer")
            {
                Halloween.Feed(world, target, "The revving stops at the doorway.");
                return EndChase(npc, target, mode, false, now, world);
            }
            long spawnedAt = npc.Properties.TryGetProperty<long>("spawnedAt", out var t) ? t : now;
            if (now - spawnedAt >= Halloween.Ticks(world, cfg.StalkerChase.ChaseSeconds))
                return EndChase(npc, target, mode, true, now, world);
        }
        else if (Halloween.IsInCostume(target, cfg))
            return EndChase(npc, target, mode, false, now, world);

        if (Math.Abs(npc.MapX - target.MapX) <= 1 && Math.Abs(npc.MapY - target.MapY) <= 1)
            return Caught(npc, target, mode, now, world);

        double pace = Math.Max(target.CalculateMoveSpeed() / 1000.0, cfg.StalkerChase.MinDelaySeconds);
        double speed = pace + (mode is "chase" ? cfg.StalkerChase.SpeedOffset : -cfg.StalkerChase.SpeedOffset);
        npc.MoveSpeed = Math.Max(0.05, speed);

        var dir = npc.PathStepTo(target.MapX, target.MapY, world, (x, y) => npc.Map.GetCharacterAt(x, y) is NPC);
        if (dir is not null)
        {
            (int nx, int ny) = dir.Value switch
            {
                Direction.Up => (npc.MapX, npc.MapY - 1),
                Direction.Down => (npc.MapX, npc.MapY + 1),
                Direction.Left => (npc.MapX - 1, npc.MapY),
                _ => (npc.MapX + 1, npc.MapY),
            };
            var occupant = npc.Map.GetCharacterAt(nx, ny);
            if (occupant is Player p and not Pet && p != target)
                BodyBlock(p, world);
            else
                npc.StepTo(dir.Value, world);
        }

        if (Math.Max(Math.Abs(npc.MapX - target.MapX), Math.Abs(npc.MapY - target.MapY)) <= cfg.StalkerChase.WarnRadius &&
            now >= Halloween.GetState(target, "hw.stalkerWarnAt"))
        {
            Halloween.Feed(world, target, "He's right behind you…");
            Halloween.SetState(target, "hw.stalkerWarnAt", now + Halloween.Ticks(world, cfg.StalkerChase.WarnSeconds));
        }
        return false;
    }

    private bool Caught(NPC npc, Player target, string mode, long now, GameWorld world)
    {
        var cfg = HalloweenConfig.Get(world);
        Swing(npc, target, world);
        if (mode is "chase")
        {
            if (cfg.Maps.FoyerSpawn.Length >= 2 &&
                world.MapHandler.Maps.TryGetValue(cfg.Maps.House, out var house) && house is not null)
            {
                target.WarpTo(world, house, cfg.Maps.FoyerSpawn[0], cfg.Maps.FoyerSpawn[1]);
                Halloween.Feed(world, target, "You wake up in the foyer, shaking.");
            }
        }
        else
        {
            if (cfg.Maps.GroveExit.Length >= 2 &&
                world.MapHandler.Maps.TryGetValue(cfg.Maps.Grove, out var grove) && grove is not null)
            {
                target.WarpTo(world, grove, cfg.Maps.GroveExit[0], cfg.Maps.GroveExit[1]);
                Halloween.Feed(world, target, "He throws you out of the front door.");
            }
        }
        return EndChase(npc, target, mode, false, now, world);
    }

    private static void Swing(NPC npc, Player target, GameWorld world)
    {
        int dx = target.MapX - npc.MapX;
        int dy = target.MapY - npc.MapY;
        var facing = Math.Abs(dx) >= Math.Abs(dy)
            ? (dx > 0 ? Direction.Right : Direction.Left)
            : (dy > 0 ? Direction.Down : Direction.Up);
        npc.FaceTo(facing, world);
        string packet = P.Attack(npc);
        foreach (var player in npc.Map.GetPlayersInRange(npc))
            world.Send(player, packet);
    }

    private void BodyBlock(Player p, GameWorld world)
    {
        var cfg = HalloweenConfig.Get(world);
        if (cfg.Maps.GroveExit.Length < 2) return;
        if (!world.MapHandler.Maps.TryGetValue(cfg.Maps.Grove, out var grove) || grove is null) return;
        p.WarpTo(world, grove, cfg.Maps.GroveExit[0], cfg.Maps.GroveExit[1]);
        Halloween.Feed(world, p, "The chainsaw catches you. You land in the pumpkin patch.");
    }

    private bool EndChase(NPC npc, Player? target, string? mode, bool survived, long now, GameWorld world)
    {
        var cfg = HalloweenConfig.Get(world);
        if (survived && target is not null)
        {
            Halloween.Feed(world, target, "The revving fades into the walls.");
            Halloween.GiveCandy(world, target, cfg.Candy.StalkerSurvived);
        }
        if (target is not null)
        {
            if (mode is "chase")
                Halloween.SetState(target, "hw.stalkerGraceUntil", now + Halloween.Ticks(world, cfg.Stalker.GraceSeconds));
            Halloween.SetState(target, "hw.stalkerNpc", 0);
        }
        npc.Despawn(world);
        return true;
    }
}

return typeof(Stalker);
