using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose
{
    /**
     * Item interface
     * 
     * so can interchange ItemTemplate/Items mainly used for Item info window
     * 
     */
    public interface IItem
    {
        string Name { get; set; }
        string Description { get; set; }

        int GraphicEquipped { get; set; }
        int GraphicTile { get; set; }
        int GraphicR { get; set; }
        int GraphicG { get; set; }
        int GraphicB { get; set; }
        int GraphicA { get; set; }

        int BodyState { get; set; }

        AttributeSet BaseStats { get; set; }

        long Value { get; set; }


        int WeaponDamage { get; set; }
        int WeaponDelay { get; }
        int StackSize { get; }
        bool IsLore { get; }
        bool IsBindOnPickup { get; }
        bool IsBindOnEquip { get; }
        bool IsEvent { get; }
        ItemTemplate.ItemSlots Slot { get; }
        ItemTemplate.ItemTypes Type { get; }
        ItemTemplate.UseTypes UseType { get; }
        int MinLevel { get; }
        int MaxLevel { get; }
        long MinExperience { get; }
        long MaxExperience { get; }

        long ClassRestrictions { get; }
        SpellEffect SpellEffect { get; }
        decimal SpellEffectChance { get; }
        int LearnSpellID { get; }

        int Credits { get; }
    }
}
