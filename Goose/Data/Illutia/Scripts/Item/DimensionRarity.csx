using System;
using Goose;
using Goose.Scripting;

/// <summary>Legendary and Stunted, Item.java:391-401. ScriptParams carries the multiplier.
/// StatMultiplier scales the whole item including the baked dimension bonus, matching
/// abyss (Item.java:463).</summary>
public class DimensionRarity : BaseItemModifierScript
{
    public override void OnExecuteEvent(ItemModifier modifier, Item item, GameWorld world)
    {
        item.StatMultiplier *= double.Parse(modifier.ScriptParams);
        item.RefreshStats();
    }
}

return typeof(DimensionRarity);
