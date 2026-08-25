#load "DimensionConstants.csx"
#load "DimensionHelpers.csx"
#load "DimensionRolls.csx"
using System;
using Goose;
using Goose.Scripting;

/// <summary>Attached to every generated dimension item template by Dimensions.csx. One
/// shared, stateless instance serves all of them - ScriptHandler caches by path
/// (ScriptHandler.cs:24) - so the dimension is recovered from the item, never stored.
///
/// Also forwards every call to the base template's script, so a scripted base item
/// (OkonkIllusionSword.csx, ZombieLegIllusion.csx) keeps working in every dimension.</summary>
public class DimensionItem : BaseItemScript
{
    /// <summary>The abyss roll, Item.java:359-401. Returns true unconditionally: a
    /// dimension item never takes goose's native title/surname roll on top. The roll
    /// itself lives in DimensionRolls so /resetitem's paid reroll shares it.</summary>
    public override bool OnRollModifiersEvent(Item item, GameWorld world)
    {
        int dim = DimensionHelpers.DimensionOf(item.TemplateID);
        if (dim <= 0) return false;

        DimensionRolls.RollDrop(item, world);

        DimensionHelpers.BaseItemScript(item, world)?.OnRollModifiersEvent(item, world);
        return true;
    }

    public override string CanPickup(Player player, Item item, GameWorld world)
    {
        int dim = DimensionHelpers.DimensionOf(item.TemplateID);
        if (dim > DimensionHelpers.MaxDimensionOf(player))
            return "The void keeps what you cannot carry. You have a maximum dimension of "
                   + DimensionHelpers.MaxDimensionOf(player) + ".";

        return DimensionHelpers.BaseItemScript(item, world)?.CanPickup(player, item, world);
    }

    /// <summary>Dimension tomes. Returning false leaves the item in the inventory
    /// (Inventory.cs:433). A known copy of the same spell at a lower dimension is
    /// replaced in place rather than accumulating a slot per dimension.</summary>
    public override bool OnUseConsumableEvent(Player player, Item item, GameWorld world)
    {
        var incoming = world.SpellHandler.GetSpell(item.LearnSpellID);
        if (incoming == null) return DimensionHelpers.BaseItemScript(item, world)?.OnUseConsumableEvent(player, item, world) ?? true;

        int baseId = DimensionHelpers.BaseId(incoming.ID);
        for (int slot = 1; slot <= world.Settings.SpellbookSize; slot++)
        {
            var known = player.Spellbook.GetSlot(slot);
            if (known == null || DimensionHelpers.BaseId(known.ID) != baseId) continue;

            if (DimensionHelpers.DimensionOf(known.ID) >= DimensionHelpers.DimensionOf(incoming.ID))
            {
                world.Send(player, P.ServerMessage("You already know a spell of that power."));
                return false;
            }

            player.Spellbook.RemoveSpell(slot, world);
            break;
        }

        return player.Spellbook.AddSpell(incoming, world);
    }

    public override void OnCreateEvent(Item item, GameWorld world)
    {
        DimensionHelpers.BaseItemScript(item, world)?.OnCreateEvent(item, world);
    }

    public override void OnMeleeEvent(Player player, Item item, GameWorld world)
    {
        DimensionHelpers.BaseItemScript(item, world)?.OnMeleeEvent(player, item, world);
    }
}

return typeof(DimensionItem);
