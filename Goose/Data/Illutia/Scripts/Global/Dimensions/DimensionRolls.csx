using System;
using Goose;

/// <summary>The abyss rolls shared by the drop path (DimensionItem.csx) and the paid
/// reroll (/resetitem in Commands.csx) so the two cannot drift. #loaded into both
/// compilations, which are separate - each host gets its own copy, so this stays
/// stateless like DimensionHelpers (pinned by ScriptLoadDirectiveTests).
///
/// The hosts already #load DimensionConstants.csx, so this file #loads nothing
/// itself: a file reached through two #load chains would be included twice in one
/// compilation (duplicate declarations), a shape the regression tests do not pin.
///
/// Caller contract: the item is dimension equipment. For Reroll the caller has run
/// ItemHandler.ResetModifiers first - these methods only apply, never strip.</summary>
public static class DimensionRolls
{
    /// <summary>A drop roll, Item.java:359-401: six equal 7.5% bands over the top 45%
    /// of the roll (Item.java:363-387), rarity independent. Equipment only - a
    /// dimension tome rolls nothing, though its hook still claims the item.</summary>
    public static void RollDrop(Item item, GameWorld world)
    {
        if (item.UseType != ItemTemplate.UseTypes.Armor && item.UseType != ItemTemplate.UseTypes.Weapon)
            return;

        double roll = world.Random.NextDouble();
        if (roll >= 0.55)
        {
            int index = Math.Min((int)((roll - 0.55) / 0.075), 5);
            ApplySuffix(item, world, index);
        }

        ApplyRarity(item, world);
    }

    /// <summary>A paid reroll: the suffix is guaranteed, where a drop rolls 45%. The
    /// rarity roll is unchanged (2% Legendary / 2% Stunted, independent of the
    /// suffix).</summary>
    public static void Reroll(Item item, GameWorld world)
    {
        if (item.UseType != ItemTemplate.UseTypes.Armor && item.UseType != ItemTemplate.UseTypes.Weapon)
            return;

        ApplySuffix(item, world, world.Random.Next(6));
        ApplyRarity(item, world);
    }

    /// <summary>The abyss suffix for one of the six bands. Shared by the drop roll and
    /// the paid reroll so the two paths cannot drift.</summary>
    private static void ApplySuffix(Item item, GameWorld world, int index)
    {
        Apply(world.ItemHandler.GetSurname(DimensionConstants.SurnameIdBase + index), item, world, prefix: false);
    }

    /// <summary>Item.java:391-401 - 2% each, rolled independently of the suffix.</summary>
    private static void ApplyRarity(Item item, GameWorld world)
    {
        double rarity = world.Random.NextDouble();
        if (rarity > 0.98) Apply(world.ItemHandler.GetTitle(DimensionConstants.TitleIdBase), item, world, prefix: true);
        else if (rarity > 0.96) Apply(world.ItemHandler.GetTitle(DimensionConstants.TitleIdBase + 1), item, world, prefix: true);
    }

    /// <summary>Mirrors ItemHandler.RollTitleAndSurname's own application: name, then
    /// the id property, then the modifier's stats.</summary>
    private static void Apply(ItemModifier modifier, Item item, GameWorld world, bool prefix)
    {
        if (modifier == null) return;

        item.Name = prefix ? modifier.Name + " " + item.Name : item.Name + " " + modifier.Name;
        item.ItemProperties[prefix ? ItemProperty.TitleId : ItemProperty.SurnameId] = modifier.Id;
        modifier.ApplyStats(item, world);
    }
}
