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
        if (ArenaCore.IsParticipant(world, player.PlayerID))
        {
            ArenaHype.Mayor(world, cfg, "eliminated", ("player", player.Name), ("killer", killer.Name));
            ArenaCore.Eliminate(world, player.PlayerID, "slain by " + killer.Name);
        }
        if (!cfg.Enabled(world)) return null;
        if (!world.MapHandler.Maps.TryGetValue(cfg.LobbyMap, out var lobby)) return null;
        // Engine warps again after OnPlayerDied, and WarpTo NREs when Map is null.
        // Do the protocol-complete cross-map warp here (EndRun already did it for the last
        // participant), then re-attach so the engine's warp is a same-map move.
        if (player.Map is null) player.Map = lobby;
        else if (player.Map != lobby)
        {
            player.WarpTo(world, lobby, cfg.LobbySpawn[0], cfg.LobbySpawn[1]);
            player.Map = lobby;
        }
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
