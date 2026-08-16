using System;
using System.Collections.Generic;
using Goose;
using Goose.Scripting;

/// <summary>Replaces SpellEffect.CastTeleportSpell (SpellEffect.cs:702) for every
/// teleport effect, including dimension 0. The only behaviour change is that the
/// destination resolves in the caster's dimension - abyss does the same thing by passing
/// the caster's dimension to getMap (SpellEffect.java:833).
///
/// One instance serves every teleport effect: ScriptHandler caches one object per path
/// (ScriptHandler.cs:19), so this class must stay stateless and read everything from
/// thisEffect and its ScriptParams.</summary>
public class DimensionTeleport : BaseSpellEffectScript
{
    /// <summary>Must match Dimensions.csx's Offset. Scripts compile independently,
    /// so this cannot be shared.</summary>
    private const int Offset = 100000;

    private int OffsetOf(SpellEffect effect)
    {
        int offset;
        return int.TryParse(effect.ScriptParams, out offset) && offset > 0 ? offset : Offset;
    }

    /// <summary>The dimension is encoded in the map id (baseId + Offset*dim), the same
    /// convention DimensionMap.csx reads - ScriptParams is passed through to the base map's
    /// script, so it can no longer carry the dimension.</summary>
    private int DimensionOf(Map map)
    {
        return map == null ? 0 : map.ID / Offset;
    }

    public override bool Cast(SpellEffect thisEffect, ICharacter caster, ICharacter target,
                              GameWorld world)
    {
        // CastSpell guards Teleport with "target is Player" (SpellEffect.cs:939);
        // CastScriptSpell (:975) has no such guard, so it has to be repeated here.
        var player = target as Player;
        if (player == null) return false;

        if (!thisEffect.CanCastSpell(caster, target)) return false;

        if (thisEffect.Animation != 0)
        {
            var range = target.Map.GetPlayersInRange(target);
            string packet = P.SpellPlayer(target.LoginID, thisEffect.Animation, thisEffect.AnimationFile);
            world.Send(player, packet);
            foreach (var other in range) world.Send(other, packet);
        }

        Map map = ResolveDestination(thisEffect, caster, world);

        // A missing destination means "return to bound spot" - used for gate spells.
        if (map == null)
        {
            player.WarpTo(world, player.BoundMap, player.BoundX, player.BoundY);
            return true;
        }

        if (!map.PlayerCanJoin(player, world)) return false;

        player.WarpTo(world, map, thisEffect.TeleportMapX, thisEffect.TeleportMapY);
        return true;
    }

    /// <summary>The dimension clone of the destination, falling back to the base map when
    /// that dimension has no copy - an exit from the dimension rather than a dead spell.</summary>
    private Map ResolveDestination(SpellEffect thisEffect, ICharacter caster, GameWorld world)
    {
        if (thisEffect.TeleportMapID == 0) return null;

        int dim = DimensionOf(caster.Map);
        return world.MapHandler.GetMap(thisEffect.TeleportMapID + OffsetOf(thisEffect) * dim)
            ?? world.MapHandler.GetMap(thisEffect.TeleportMapID);
    }

    /// <summary>Restores the line the built-in switch would have produced
    /// (SpellEffect.cs:446), which the rewrite to EffectTypes.Script would otherwise drop.
    /// Resolved against the base map, since a description is rendered outside any cast.</summary>
    public override IEnumerable<string> GetItemDescription(SpellEffect thisEffect, GameWorld world)
    {
        var map = world.MapHandler.GetMap(thisEffect.TeleportMapID);
        if (map == null) return new[] { "Teleport to bound location" };

        return new[]
        {
            "Teleport to " + map.Name + " (" + thisEffect.TeleportMapX + ", "
                + thisEffect.TeleportMapY + ") in your current dimension"
        };
    }
}

return typeof(DimensionTeleport);
