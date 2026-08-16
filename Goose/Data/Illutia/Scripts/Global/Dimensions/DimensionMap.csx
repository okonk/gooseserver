#load "DimensionConstants.csx"
using System;
using Goose;
using Goose.Scripting;

/// <summary>Entry gate and delegation layer for dimension map clones. The dimension is
/// encoded in the map id (baseId + Offset*dim), so nothing needs ScriptParams, which is
/// passed through untouched to the base map's own script - every member forwards to it.
///
/// Known limitation: base scripts that keep per-map state in Map.ScriptStore (e.g.
/// ZombieTownMap.csx, which initializes it in OnLoad/OnLoadTile) cannot work on dimension
/// clones. Clones are created by Map.CloneAs, which deliberately skips LoadData (re-parsing
/// the .map file and the SQL), so the load hooks never run for them and ScriptStore stays
/// null. Their forwarded events then throw inside the script; the engine's call sites
/// swallow the exceptions, so the clone stays inert - exactly as before this delegation
/// landed - rather than crashing. Fixing this needs per-clone load-hook replication with
/// independent MapScriptData per dimension, deferred as out of scope for the dimensions
/// part 4 plan (user decision).</summary>
public class DimensionMap : BaseMapScript
{
    /// <summary>Must match Dimensions.csx's Offset. Scripts compile independently,
    /// so this cannot be shared.</summary>
    private const int Offset = 100000;

    /// <summary>The dimension is encoded in the map id (baseId + Offset*dim), so nothing
    /// needs to be stashed in ScriptParams - which is passed through to the base map's
    /// script instead.</summary>
    private int DimensionOf(Map map)
    {
        return map.ID / Offset;
    }

    private IMapScript Inner(Map map, GameWorld world)
    {
        return world.MapHandler.GetMap(map.ID % Offset)?.Script?.Object;
    }

    private int MaxDimensionOf(Player player)
    {
        return player.Properties.GetProperty<int>(DimensionConstants.MaxDimensionProperty, 0);
    }

    /// <summary>Gates warps (MoveEvent.cs:123) and teleport spells (SpellEffect.cs:727),
    /// then delegates the rest of the decision to the base map's script, which still gets
    /// to refuse entry (item gates, scripted closures, ...). The dimension gate checks
    /// first, so it wins over a permissive base script.</summary>
    public override string CanPlayerJoin(Map map, Player player, GameWorld world)
    {
        int max = MaxDimensionOf(player);
        if (DimensionOf(map) > max)
            return "The void has rejected you. You have a maximum dimension of " + max + ".";

        return Inner(map, world)?.CanPlayerJoin(map, player, world);
    }

    /// <summary>Login places a player straight onto their saved map without consulting
    /// PlayerCanJoin, so the gate is re-checked here and violators are sent to the
    /// dimension-0 copy of wherever they were. The bind is clamped too - see below.
    ///
    /// Once the gate has passed, the base map's script is told about the entry - a
    /// dimension Arena still runs the arena logic. A rejected entry is NOT forwarded: the
    /// base script must not see an entry that was warped straight back out.</summary>
    public override void OnPlayerEntered(Map map, Player player, GameWorld world)
    {
        // GMs bypass the gate on the warp path (Map.PlayerCanJoin checks
        // IgnoreMapRequirements), so the login gate must not fight them either -
        // relocating or re-binding a privileged account would be silently destructive.
        // They still get the base script's entry logic.
        if (player.HasPrivilege(AccessPrivilege.IgnoreMapRequirements))
        {
            Inner(map, world)?.OnPlayerEntered(map, player, world);
            return;
        }

        int max = MaxDimensionOf(player);

        // Order matters: clamp the bind first, so it is corrected even when this map is
        // allowed and the early return below fires.
        ClampBind(player, max, world);

        if (DimensionOf(map) <= max)
        {
            Inner(map, world)?.OnPlayerEntered(map, player, world);
            return;
        }

        var fallback = world.MapHandler.GetMap(map.ID % Offset);
        if (fallback == null) return;

        world.Send(player, "$7The void has rejected you. You have a maximum dimension of "
                           + max + ".");
        player.WarpTo(world, fallback, player.MapX, player.MapY);
    }

    /// <summary>Relocating the player is not enough on its own. BoundID/BoundMap
    /// (Player.cs:226-238) are what death warps to (Player.cs:1775), so a bind set inside a
    /// dimension survives the relocation and lets a player whose progress was reduced walk
    /// straight back in by dying. Clamp it to the dimension-0 map, keeping the coordinates.
    ///
    /// A dimension map's id is baseId + Offset*dim, so the base is id % Offset. If that map
    /// has somehow gone (a re-import between sessions), fall back to the starting map -
    /// leaving a bind pointing at a map that does not exist would strand the player on
    /// death.</summary>
    private void ClampBind(Player player, int max, GameWorld world)
    {
        int boundDim = player.BoundID / Offset;
        if (boundDim <= max) return;

        var baseMap = world.MapHandler.GetMap(player.BoundID % Offset);
        if (baseMap != null)
        {
            // Same map one dimension down to 0, same coordinates.
            player.BoundID = baseMap.ID;
            player.BoundMap = baseMap;
            return;
        }

        // The base map is gone - a re-import between sessions. A bind pointing at a map
        // that does not exist strands the player on death, so send them to the start.
        var start = world.MapHandler.GetMap(GameWorld.Settings.StartingMapID);
        if (start == null) return;

        player.BoundID = start.ID;
        player.BoundMap = start;
        player.BoundX = GameWorld.Settings.StartingMapX;
        player.BoundY = GameWorld.Settings.StartingMapY;
    }

    // ---- Delegation to the base map's script ----------------------------------------
    // Every remaining member is forwarded verbatim. The clone's map id already encodes the
    // dimension; the base script receives the clone's map and reads the parameters it was
    // written against from ScriptParams, which Dimensions.csx passes through untouched.

    public override void OnLoad(Map map, GameWorld world)
    {
        Inner(map, world)?.OnLoad(map, world);
    }

    public override void OnLoadTile(Map map, int x, int y, int layerNumber, int graphic, short sheet, int flags, GameWorld world)
    {
        Inner(map, world)?.OnLoadTile(map, x, y, layerNumber, graphic, sheet, flags, world);
    }

    public override void OnFinishedLoad(Map map, GameWorld world)
    {
        Inner(map, world)?.OnFinishedLoad(map, world);
    }

    public override void OnPlayerLeft(Map map, Player player, GameWorld world)
    {
        Inner(map, world)?.OnPlayerLeft(map, player, world);
    }

    public override void OnPlayerMove(Map map, Player player, GameWorld world)
    {
        Inner(map, world)?.OnPlayerMove(map, player, world);
    }

    public override void OnPlayerChatEvent(Map map, Player player, string message, GameWorld world)
    {
        Inner(map, world)?.OnPlayerChatEvent(map, player, message, world);
    }

    public override void OnNPCKilledEvent(Map map, NPC npc, ICharacter killer, GameWorld world)
    {
        Inner(map, world)?.OnNPCKilledEvent(map, npc, killer, world);
    }

    public override void OnNPCSpawnEvent(Map map, NPC npc, GameWorld world)
    {
        Inner(map, world)?.OnNPCSpawnEvent(map, npc, world);
    }

    public override void OnPetMove(Map map, Pet pet, GameWorld world)
    {
        Inner(map, world)?.OnPetMove(map, pet, world);
    }
}

return typeof(DimensionMap);
