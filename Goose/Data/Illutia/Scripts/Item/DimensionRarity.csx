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
        // Invariant: ScriptParams is generated here with '.', and nothing pins a process
        // culture - under de-DE the plain overload parses "1.25" as 125, silently inflating
        // every stat 100x, and under fr-FR/ar-SA it throws a FormatException that
        // ItemModifier.ApplyStats swallows. Same convention as Dimensions.csx's ScaleFormula.
        item.StatMultiplier *= double.Parse(modifier.ScriptParams, System.Globalization.CultureInfo.InvariantCulture);
        item.RefreshStats();
    }
}

return typeof(DimensionRarity);
