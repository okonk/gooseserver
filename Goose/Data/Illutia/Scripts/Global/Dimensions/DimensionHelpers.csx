using System;
using Goose;
using Goose.Scripting;

/// <summary>Stateless dimension math shared by every dimension script. #loaded, so
/// each host compilation gets its own copy - keep this file free of mutable statics
/// (pinned by ScriptLoadDirectiveTests). Everything here is a read or a compute.</summary>
public static class DimensionHelpers
{
    /// <summary>The dimension an id encodes: baseId + Offset*dim.</summary>
    public static int DimensionOf(int id) { return id / DimensionConstants.Offset; }

    /// <summary>The dimension-0 base id under a dimension id.</summary>
    public static int BaseId(int id) { return id % DimensionConstants.Offset; }

    /// <summary>The player's unlocked maximum dimension (0 = dimension 0 only).</summary>
    public static int MaxDimensionOf(Player player)
    {
        return player.Properties.GetProperty<int>(DimensionConstants.MaxDimensionProperty, 0);
    }

    /// <summary>The dimension-0 map's script, or null if the base map or its script is
    /// missing. The dimension script's delegation target.</summary>
    public static IMapScript BaseMapScript(Map map, GameWorld world)
    {
        return world.MapHandler.GetMap(BaseId(map.ID))?.Script?.Object;
    }

    /// <summary>The base template's script, or null. Same delegation role for items.</summary>
    public static IItemScript BaseItemScript(Item item, GameWorld world)
    {
        return world.ItemHandler.GetTemplate(BaseId(item.TemplateID))?.Script?.Object;
    }

    /// <summary>AttributeSet.java:405-419 tier, computed from the BASE template (a clone's
    /// value is already scaled by 3^dim and would put everything in the top tier). A
    /// missing base template - the feature was disabled and re-enabled around a data
    /// change - scores the lowest tier rather than throwing inside a roll.
    /// Abyss's top tier (1.5) keys off an SP-priced template; goose has no SP value, so
    /// that tier has no equivalent and is dropped.</summary>
    public static double Tier(ItemTemplate basic)
    {
        if (basic == null) return 0.25;
        if (basic.Value >= 10000000) return 1.0;
        if (basic.MinExperience > 0) return 0.75;
        if (basic.MinLevel == 50) return 0.5;
        return 0.25;
    }

    /// <summary>Shared gate refusal for /dimension, map entry and login re-check.</summary>
    public static string MaxDimensionRefusal(int max)
    {
        return "The void has rejected you. You have a maximum dimension of " + max + ".";
    }
}
