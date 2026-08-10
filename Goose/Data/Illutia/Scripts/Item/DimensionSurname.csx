using System;
using Goose;
using Goose.Scripting;

/// <summary>The six abyss suffixes. Each applies the suffix-specific terms from
/// AttributeSet.dimensionDefault (AttributeSet.java:376) - the flat part is already baked
/// into the dimension template by Dimensions.csx.
///
/// ScriptParams carries the suffix index 0-5, matching the registration order in
/// Dimensions.csx. The generic ItemModifierScript.csx cannot express this: its operations
/// are fixed JSON values with no access to the item's dimension.</summary>
public class DimensionSurname : BaseItemModifierScript
{
    /// <summary>Must match Dimensions.csx's Offset. Scripts compile independently.</summary>
    private const int Offset = 100000;

    public override void OnExecuteEvent(ItemModifier modifier, Item item, GameWorld world)
    {
        int dim = item.TemplateID / Offset;
        if (dim <= 0) return;

        double tier = Tier(world.ItemHandler.GetTemplate(item.TemplateID % Offset));
        decimal scale = (decimal)(dim * tier);
        int index = int.Parse(modifier.ScriptParams);

        switch (index)
        {
            case 0:   // of Vita Regen - AttributeSet.java:430,431
                item.BaseStats.HPPercentRegen += 0.015m * scale;
                item.BaseStats.HPStaticRegen += (int)(1500 * dim * tier);
                break;
            case 1:   // of Mana Regen - AttributeSet.java:435,436
                item.BaseStats.MPPercentRegen += 0.015m * scale;
                item.BaseStats.MPStaticRegen += (int)(1500 * dim * tier);
                break;
            case 2:   // of Criticality - AttributeSet.java:437
                item.BaseStats.SpellCrit += 0.04m * scale;
                break;
            case 3:   // of Spell Damage - AttributeSet.java:438
                item.BaseStats.SpellDamage += 0.04m * scale;
                break;
            case 4:   // of Reduction - AttributeSet.java:422
                item.BaseStats.DamageReduction += 0.04m * scale;
                break;
            case 5:   // of Speed - AttributeSet.java:428
                item.BaseStats.Haste += 0.04m * scale;
                break;
        }

        item.RefreshStats();
    }

    /// <summary>AttributeSet.java:405-419, on the base template. A missing base template
    /// (the feature was disabled and re-enabled around a data change) scores the lowest
    /// tier rather than throwing inside a roll.</summary>
    private double Tier(ItemTemplate basic)
    {
        if (basic == null) return 0.25;
        if (basic.Value >= 10000000) return 1.0;
        if (basic.MinExperience > 0) return 0.75;
        if (basic.MinLevel == 50) return 0.5;
        return 0.25;
    }
}

return typeof(DimensionSurname);
