using System;
using System.Collections.Generic;
using System.Linq;
using Goose;
using Goose.Scripting;
using Newtonsoft.Json;

public class ItemModifierScript : BaseItemModifierScript
{
    public enum OperationType
    {
        StatMultiplier,
        WeaponDamage,
        HP,
        MP,
        SP,
        AC,
        STR,
        INT,
        STA,
        DEX,
        SpellDamage,
        SpellCrit,
        MeleeDamage,
        MeleeCrit,
        Haste,
        DamageReduction,
        HPPercentRegen,
        HPStaticRegen,
        MPPercentRegen,
        MPStaticRegen
    }

    public class ModifierOperation
    {
        public OperationType type { get; set; }

        public double min { get; set; }

        public double max { get; set; }

        public double value { get; set; }
    }

    public override void OnExecuteEvent(ItemModifier modifier, Item item, GameWorld world)
    {
        var operations = JsonConvert.DeserializeObject<ModifierOperation[]>(modifier.ScriptParams);

        foreach (var operation in operations)
        {
            double value = operation.value;
            if (value == 0)
                value = world.Random.Next((int)operation.min, (int)operation.max + 1);

            this.AddStats(item, operation, value);
            item.RefreshStats();
        }
    }

    private void AddStats(Item item, ModifierOperation operation, double value)
    {
        switch (operation.type)
        {
            case OperationType.StatMultiplier:
                item.StatMultiplier *= value;
                break;
            case OperationType.WeaponDamage: 
                item.WeaponDamage += (int)value; 
                break;
            case OperationType.HP: 
                item.BaseStats.HP += (int)value; 
                break;
            case OperationType.MP: 
                item.BaseStats.MP += (int)value; 
                break;
            case OperationType.SP: 
                item.BaseStats.SP += (int)value; 
                break;
            case OperationType.AC: 
                item.BaseStats.AC += (int)value; 
                break;
            case OperationType.STR: 
                item.BaseStats.Strength += (int)value; 
                break;
            case OperationType.INT: 
                item.BaseStats.Intelligence += (int)value; 
                break;
            case OperationType.STA: 
                item.BaseStats.Stamina += (int)value; 
                break;
            case OperationType.DEX: 
                item.BaseStats.Dexterity += (int)value; 
                break;
            case OperationType.SpellDamage: 
                item.BaseStats.SpellDamage += (decimal)(value / 100); 
                break;
            case OperationType.SpellCrit: 
                item.BaseStats.SpellCrit += (decimal)(value / 100); 
                break;
            case OperationType.MeleeDamage: 
                item.BaseStats.MeleeDamage += (decimal)(value / 100); 
                break;
            case OperationType.MeleeCrit: 
                item.BaseStats.MeleeCrit += (decimal)(value / 100); 
                break;
            case OperationType.Haste: 
                item.BaseStats.Haste += (decimal)(value / 100); 
                break;
            case OperationType.DamageReduction: 
                item.BaseStats.DamageReduction += (decimal)(value / 100); 
                break;
            case OperationType.HPPercentRegen: 
                item.BaseStats.HPPercentRegen += (decimal)(value / 100); 
                break;
            case OperationType.HPStaticRegen: 
                item.BaseStats.HPStaticRegen += (int)value; 
                break;
            case OperationType.MPPercentRegen: 
                item.BaseStats.MPPercentRegen += (decimal)(value / 100); 
                break;
            case OperationType.MPStaticRegen: 
                item.BaseStats.MPStaticRegen += (int)value; 
                break;
        }
    }
}

return typeof(ItemModifierScript);