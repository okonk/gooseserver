#load "DimensionConstants.csx"
#load "DimensionHelpers.csx"
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
    /// dimension item never takes goose's native title/surname roll on top.</summary>
    public override bool OnRollModifiersEvent(Item item, GameWorld world)
    {
        int dim = DimensionHelpers.DimensionOf(item.TemplateID);
        if (dim <= 0) return false;

        if (item.UseType == ItemTemplate.UseTypes.Armor || item.UseType == ItemTemplate.UseTypes.Weapon)
        {
            // Six equal 7.5% bands over the top 45% of the roll (Item.java:363-387).
            double roll = world.Random.NextDouble();
            if (roll >= 0.55)
            {
                int index = Math.Min((int)((roll - 0.55) / 0.075), 5);
                ApplySuffix(item, world, index);
            }

            ApplyRarity(item, world);
        }

        DimensionHelpers.BaseItemScript(item, world)?.OnRollModifiersEvent(item, world);
        return true;
    }

    /// <summary>A paid reroll: the suffix is guaranteed, where a drop rolls 45%. The
    /// rarity roll is unchanged (2% Legendary / 2% Stunted, independent of the suffix).
    ///
    /// ItemHandler.RerollModifiers has already reset the item to template state, so this
    /// only applies - it never has to strip anything first.</summary>
    public override bool OnRerollModifiersEvent(Item item, GameWorld world)
    {
        int dim = DimensionHelpers.DimensionOf(item.TemplateID);
        if (dim <= 0) return false;
        if (item.UseType != ItemTemplate.UseTypes.Armor && item.UseType != ItemTemplate.UseTypes.Weapon)
            return false;

        // Guaranteed, where OnRollModifiersEvent gates it behind roll >= 0.55.
        ApplySuffix(item, world, world.Random.Next(6));
        ApplyRarity(item, world);
        return true;
    }

    /// <summary>The abyss suffix for one of the six bands. Shared by the drop roll and
    /// the paid reroll so the two paths cannot drift.</summary>
    private void ApplySuffix(Item item, GameWorld world, int index)
    {
        Apply(world.ItemHandler.GetSurname(DimensionConstants.SurnameIdBase + index), item, world, prefix: false);
    }

    /// <summary>Item.java:391-401 - 2% each, rolled independently of the suffix.</summary>
    private void ApplyRarity(Item item, GameWorld world)
    {
        double rarity = world.Random.NextDouble();
        if (rarity > 0.98) Apply(world.ItemHandler.GetTitle(DimensionConstants.TitleIdBase), item, world, prefix: true);
        else if (rarity > 0.96) Apply(world.ItemHandler.GetTitle(DimensionConstants.TitleIdBase + 1), item, world, prefix: true);
    }

    /// <summary>Mirrors ItemHandler.RollTitleAndSurname's own application (ItemHandler.cs:247-265):
    /// name, then the id property, then the modifier's stats.</summary>
    private void Apply(ItemModifier modifier, Item item, GameWorld world, bool prefix)
    {
        if (modifier == null) return;

        item.Name = prefix ? modifier.Name + " " + item.Name : item.Name + " " + modifier.Name;
        item.ItemProperties[prefix ? ItemProperty.TitleId : ItemProperty.SurnameId] = modifier.Id;
        modifier.ApplyStats(item, world);
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
        for (int slot = 1; slot <= GameWorld.Settings.SpellbookSize; slot++)
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
