using System;
using System.Linq;
using Goose;

public partial class Dimensions
{

    /// <summary>Second pass over the maps Task 3 created: repoint every warp at the same
    /// dimension's clone. tiles are shared between base and clone (Map.CloneAs is a shallow
    /// copy), so each rewired warp must be a NEW WarpTile - mutating the shared one would
    /// retarget the base map too.</summary>
    private void RewireWarps(GameWorld world)
    {
        for (int dim = 1; dim <= DimensionCount; dim++)
        {
            foreach (var basic in world.MapHandler.Maps.Values.Where(m => m.ID < Offset).ToList())
            {
                var clone = world.MapHandler.GetMap(basic.ID + Offset * dim);

                for (int i = 0; i < clone.tiles.Length; i++)
                {
                    var warp = clone.tiles[i] as WarpTile;
                    if (warp == null) continue;

                    var target = warp.WarpMap == null
                        ? null
                        : world.MapHandler.GetMap(warp.WarpMap.ID + Offset * dim);

                    // A warp whose target has no clone stays pointed at the base map - it is
                    // an exit from the dimension rather than a broken link.
                    clone.tiles[i] = new WarpTile
                    {
                        WarpMap = target ?? warp.WarpMap,
                        WarpX = warp.WarpX,
                        WarpY = warp.WarpY,
                    };
                }
            }
        }
    }

    /// <summary>Map.java:251-260. Dimensions 1-4 scale; 5 and 6 take a flat floor that
    /// ignores the base value.</summary>
    private long MinExperienceFor(long baseMin, int dim)
    {
        if (dim == 5) return Dim5MinExperience;
        if (dim >= 6) return Dim6MinExperience;

        return baseMin * (dim * 5) * (dim * 5);
    }

    private void CloneMaps(GameWorld world)
    {
        var baseMaps = world.MapHandler.Maps.Values.ToList();
        var mapScript = world.ScriptHandler.GetScript<IMapScript>("Scripts/Global/Dimensions/DimensionMap.csx");

        for (int dim = 1; dim <= DimensionCount; dim++)
        {
            foreach (var basic in baseMaps)
            {
                int id = basic.ID + Offset * dim;

                // MapHandler.Maps is a plain dictionary - a collision would silently replace a
                // real map and strand whoever is standing on it.
                if (world.MapHandler.GetMap(id) != null)
                    throw new Exception($"Dimension map id {id} (base {basic.ID}, dim {dim}) already exists. "
                                        + "Offset is too small for this data set.");

                // Map.CloneAs (Part 1 task 5) carries everything across, including the private
                // requiredItems list and Muted. Rebuilding public fields here instead would
                // drop item-gated entry on every dimension copy of a key-gated map.
                var clone = basic.CloneAs(id, basic.Name + " (" + dim + ")");

                clone.CanPVP = true;                      // forced on in every dimension
                // Entry gates scale by (dim*5)^2; dimensions 5-6 take a flat floor instead
                clone.MinExperience = MinExperienceFor(basic.MinExperience, dim);
                clone.MaxExperience = basic.MaxExperience * (dim * 5) * (dim * 5);
                clone.Script = mapScript;                 // replaces, not composes - DimensionMap forwards to the base script itself
                // ScriptParams passes through untouched so a delegated base script reads the
                // params it was written against. DimensionMap takes its dimension from the
                // map id, which already encodes it.
                clone.ScriptParams = basic.ScriptParams;

                world.MapHandler.Maps[clone.ID] = clone;

                // MapHandler.LoadMaps:78 schedules one of these per map; clones need it too
                // or dropped items never sweep off the ground.
                Event sweep = new ClearMapItemsEvent();
                sweep.Ticks += world.TimerFrequency * world.Settings.ItemGroundSweepTime;
                sweep.Data = clone;
                world.EventHandler.AddEvent(sweep);
            }
        }
    }
}
