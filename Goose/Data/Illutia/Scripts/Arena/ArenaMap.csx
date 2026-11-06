#load "ArenaConfig.csx"
#load "ArenaCore.csx"

public class ArenaMap : BaseMapScript
{
    public override string? CanPlayerJoin(Map map, Player player, GameWorld world)
    {
        if (player.IsGM) return null;
        if (ArenaCore.IsParticipant(world, player.PlayerID)) return null;
        return "The arena is closed. Wait in the lobby for the next run.";
    }

    public override void OnPlayerEntered(Map map, Player player, GameWorld world)
    {
        if (player.IsGM) return;
        if (ArenaCore.IsParticipant(world, player.PlayerID)) return;
        world.Send(player, P.ServerMessage("The arena is closed. Wait in the lobby for the next run."));
        var cfg = ArenaConfig.Get(world);
        if (!cfg.Enabled(world)) return;
        if (!world.MapHandler.Maps.TryGetValue(cfg.LobbyMap, out var lobby)) return;
        player.WarpTo(world, lobby, cfg.LobbySpawn[0], cfg.LobbySpawn[1]);
    }

    public override RespawnLocation? OnPlayerDied(Map map, Player player, ICharacter killer, GameWorld world)
    {
        var cfg = ArenaConfig.Get(world);
        if (!cfg.Enabled(world)) return null;
        if (!world.MapHandler.Maps.TryGetValue(cfg.LobbyMap, out var lobby)) return null;
        // EndRun warps dead participants and the engine warps again after OnPlayerDied;
        // a second cross-map warp NREs, so attach to the lobby first to make it same-map.
        player.Map = lobby;
        player.MapID = lobby.ID;
        if (ArenaCore.IsParticipant(world, player.PlayerID))
            ArenaCore.Eliminate(world, player.PlayerID, "slain by " + killer.Name);
        map.RemovePlayer(player, world);
        return new RespawnLocation(lobby, cfg.LobbySpawn[0], cfg.LobbySpawn[1]);
    }

    public override void OnPlayerLeft(Map map, Player player, GameWorld world)
    {
        if (ArenaCore.IsParticipant(world, player.PlayerID))
            ArenaCore.Eliminate(world, player.PlayerID, "left the arena");
    }
}

return typeof(ArenaMap);
