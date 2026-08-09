using System;
using Goose;
using Goose.Scripting;

public class DimensionMap : BaseMapScript
{
    private const string MaxDimensionProperty = "dimension.max";

    /// <summary>Must match Dimensions.csx's Offset. Scripts compile independently,
    /// so this cannot be shared.</summary>
    private const int Offset = 100000;

    /// <summary>Dimensions.csx sets ScriptParams to the dimension number when it clones the map.</summary>
    private int DimensionOf(Map map)
    {
        int dim;
        return int.TryParse(map.ScriptParams, out dim) ? dim : 0;
    }

    private int MaxDimensionOf(Player player)
    {
        return player.Properties.GetProperty<int>(MaxDimensionProperty, 0);
    }

    /// <summary>Gates warps (MoveEvent.cs:123) and teleport spells (SpellEffect.cs:727).</summary>
    public override string CanPlayerJoin(Map map, Player player, GameWorld world)
    {
        int max = MaxDimensionOf(player);
        if (DimensionOf(map) <= max) return null;

        // Map.java:588
        return "The void has rejected you. You have a maximum dimension of " + max + ".";
    }

    /// <summary>Login places a player straight onto their saved map without consulting
    /// PlayerCanJoin, so the gate is re-checked here and violators are sent to the
    /// dimension-0 copy of wherever they were. The bind is clamped too - see below.</summary>
    public override void OnPlayerEntered(Map map, Player player, GameWorld world)
    {
        int max = MaxDimensionOf(player);

        // Order matters: clamp the bind first, so it is corrected even when this map is
        // allowed and the early return below fires.
        ClampBind(player, max, world);

        if (DimensionOf(map) <= max) return;

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
}

return typeof(DimensionMap);
