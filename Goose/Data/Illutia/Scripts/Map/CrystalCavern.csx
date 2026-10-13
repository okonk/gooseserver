using Goose;
using Goose.Events;
using Goose.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;

public class CrystalCavern : BaseMapScript
{
    private const int ColdEffect = 307;
    private const int WarmbloodEffect = 308;
    private const int BlockerTemplate = 239;
    private const int RavenousEffect = 314;
    private const long RecheckMs = 5000;
    private const double BlockerHomeRetrySeconds = 2;

    private static readonly int[] ChompExcluded = [242, 248];

    private static readonly Dictionary<(int, int), (int, int)[]> BlockerTiles = new()
    {
        [(47, 25)] = [(45, 24), (46, 24)],
        [(50, 40)] = [(54, 74), (55, 74)],
    };

    private readonly Dictionary<int, long> lastCheck = [];

    public override void OnPlayerEntered(Map map, Player player, GameWorld world)
    {
        ApplyCold(player, world);
        ClearBlockers(map, player, world, false);
        lastCheck[player.LoginID] = world.TimeNow;
    }

    public override void OnPlayerLeft(Map map, Player player, GameWorld world)
    {
        foreach (var id in new[] { ColdEffect, WarmbloodEffect })
        {
            var buff = player.Buffs.FirstOrDefault(b => b.SpellEffect.ID == id);
            if (buff != null)
                player.RemoveBuff(buff, world);
        }
        lastCheck.Remove(player.LoginID);
    }

    public override void OnNPCSpawnEvent(Map map, NPC npc, GameWorld world)
    {
        if (npc.NPCTemplateID == BlockerTemplate)
            ReturnBlockerHome(map, npc, npc.LoginID, world);

        if (ChompExcluded.Contains(npc.NPCTemplateID)) return;

        var ravenous = world.SpellHandler.GetSpellEffect(RavenousEffect);
        ravenous?.Cast(npc, npc, world);
    }

    public override void OnPlayerMove(Map map, Player player, GameWorld world)
    {
        ClearBlockers(map, player, world, true);

        if (lastCheck.TryGetValue(player.LoginID, out long last) && world.TimeNow - last < RecheckMs)
            return;
        lastCheck[player.LoginID] = world.TimeNow;
        ApplyCold(player, world);
    }

    private void ClearBlockers(Map map, Player player, GameWorld world, bool announce)
    {
        if (!BlockerTiles.TryGetValue((player.MapX, player.MapY), out (int, int)[] tiles))
            return;

        var blockers = map.NPCs
            .Where(n => n.NPCTemplateID == BlockerTemplate && n.State == NPC.States.Alive &&
                        tiles.Contains((n.SpawnX, n.SpawnY)))
            .ToList();

        if (blockers.Count == 0)
        {
            if (announce)
                world.Send(player, P.ServerMessage("Nothing happens."));
            return;
        }

        foreach (var npc in blockers)
        {
            var watchers = map.GetPlayersInRange(npc);

            map.SetCharacter(null, npc.MapX, npc.MapY);
            npc.State = NPC.States.Dead;
            npc.MoveEvent = null;
            npc.CurrentHP = 0;

            if (npc.ShouldRespawn)
                npc.AddRespawnEvent(world);
            else
                map.RemoveNPC(npc);

            var packet = P.EraseCharacter(npc.LoginID);
            foreach (var p in watchers)
                world.Send(p, packet);
        }

        world.SendToMap(map, P.ServerMessage("The blockers have been cleared!"));
    }

    // OnNPCSpawnEvent fires before Spawn places the NPC, and the login id changes on
    // every respawn, so it marks which life this retry chain belongs to.
    private static void ReturnBlockerHome(Map map, NPC npc, int loginId, GameWorld world)
    {
        ScriptTimerEvent.Create(() =>
        {
            if (npc.LoginID != loginId || npc.State != NPC.States.Alive || npc.Map != map) return;
            if (npc.MapX == npc.SpawnX && npc.MapY == npc.SpawnY) return;

            if (map.IsTileBlocked(npc, npc.SpawnX, npc.SpawnY))
                ReturnBlockerHome(map, npc, loginId, world);
            else
                npc.MoveTo(world, npc.SpawnX, npc.SpawnY);
        }, TimeSpan.FromSeconds(BlockerHomeRetrySeconds), world);
    }

    private void ApplyCold(Player player, GameWorld world)
    {
        if (player.Buffs.Any(b => b.SpellEffect.ID == WarmbloodEffect)) return;
        if (player.Buffs.Any(b => b.SpellEffect.ID == ColdEffect)) return;

        var cold = world.SpellHandler.GetSpellEffect(ColdEffect);
        cold?.Cast(player, player, world);
    }
}

return typeof(CrystalCavern);
