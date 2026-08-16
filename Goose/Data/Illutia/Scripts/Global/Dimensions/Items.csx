using System;
using System.Linq;
using Goose;
using Goose.Scripting;

public partial class Dimensions
{

    // ---- Item pass --------------------------------------------------------

    /// <summary>Abyss suffix names, in the band order of Item.java:363-387.</summary>
    private static readonly string[] SurnameNames =
    {
        "of Vita Regen", "of Mana Regen", "of Criticality",
        "of Spell Damage", "of Reduction", "of Speed",
    };

    /// <summary>Registers the eight dimension modifiers. All at Chance 0: RollModifier
    /// (ItemHandler.cs:270) sizes each modifier's selection range as (int)(Chance * 100),
    /// so zero yields an empty range and these can never land on dimension-0 loot. The
    /// dimension script selects them explicitly by id.</summary>
    private void RegisterModifiers(GameWorld world)
    {
        var surnameScript = world.ScriptHandler.GetScript<IItemModifierScript>(
            "Scripts/Global/Dimensions/DimensionSurname.csx");

        for (int i = 0; i < SurnameNames.Length; i++)
        {
            world.ItemHandler.AddSurname(new ItemModifier
            {
                Id = SurnameIdBase + i,
                Name = SurnameNames[i],
                Chance = 0,
                Slot = ItemTemplate.ItemSlots.Misc,   // ModifierAppliesToItem treats Misc as "any slot"
                Script = surnameScript,
                ScriptParams = i.ToString(),
            });
        }

        var rarityScript = world.ScriptHandler.GetScript<IItemModifierScript>(
            "Scripts/Global/Dimensions/DimensionRarity.csx");

        world.ItemHandler.AddTitle(new ItemModifier
        {
            Id = TitleIdBase, Name = "Legendary", Chance = 0,
            Slot = ItemTemplate.ItemSlots.Misc,
            Script = rarityScript, ScriptParams = "1.25",
        });
        world.ItemHandler.AddTitle(new ItemModifier
        {
            Id = TitleIdBase + 1, Name = "Stunted", Chance = 0,
            Slot = ItemTemplate.ItemSlots.Misc,
            Script = rarityScript, ScriptParams = "0.5",
        });
    }

    /// <summary>Equipment and spell tomes get a copy per dimension. Consumables never scale
    /// in abyss (Item.java:404); money and NoUse items have nothing to scale.</summary>
    private bool ShouldClone(ItemTemplate t)
    {
        return t.UseType == ItemTemplate.UseTypes.Armor
            || t.UseType == ItemTemplate.UseTypes.Weapon
            || (t.UseType == ItemTemplate.UseTypes.Scroll && t.LearnSpellID > 0);
    }

    private void CloneItemTemplates(GameWorld world)
    {
        // Snapshot first: AddTemplate mutates the dictionary GetTemplates() enumerates
        // (ItemHandler.cs:42 hands back the live values collection).
        var baseTemplates = world.ItemHandler.GetTemplates()
            .Where(t => t.ID < Offset && ShouldClone(t)).ToList();

        // One shared script for every clone - ScriptHandler caches by path
        // (ScriptHandler.cs:24), and DimensionItem recovers its dimension from each
        // item, so a single stateless instance serves all of them.
        var itemScript = world.ScriptHandler.GetScript<IItemScript>("Scripts/Global/Dimensions/DimensionItem.csx");

        for (int dim = 1; dim <= DimensionCount; dim++)
        {
            foreach (var basic in baseTemplates)
            {
                int id = basic.ID + Offset * dim;

                // AddTemplate overwrites silently, so a collision would quietly replace a
                // real item and change what every stored Item with that id resolves to.
                if (world.ItemHandler.GetTemplate(id) != null)
                    throw new Exception($"Dimension item template id {id} (base {basic.ID}, dim {dim}) "
                                        + "already exists. Offset is too small for this data set.");

                world.ItemHandler.AddTemplate(ScaleItemTemplate(world, basic, dim, itemScript));
            }
        }
    }

    private ItemTemplate ScaleItemTemplate(GameWorld world, ItemTemplate basic, int dim, Script<IItemScript> itemScript)
    {
        var clone = new ItemTemplate(basic)
        {
            ID = basic.ID + Offset * dim,
            Name = DimensionPrefixes[dim] + basic.Name,
            Description = "Abyss (" + dim + ") " + basic.Description,

            // Replaces the base script rather than composing with it - DimensionItem.csx
            // forwards to the base template's script itself, so nothing is lost.
            Script = itemScript,

            // Item.java:441-444
            GraphicR = Math.Max(basic.GraphicR - 30 * dim, 0),
            GraphicG = Math.Max(basic.GraphicG - 30 * dim, 0),
            GraphicB = Math.Max(basic.GraphicB - 30 * dim, 0),
            GraphicA = Math.Min(basic.GraphicA + 30 * dim, 200),

            // Item.java:445. This is the spirit price. CurrencyId stamps the clones as
            // spirit-priced (below), and CurrencyHandler.Resolve makes that override win
            // at every vendor, so this value is never read as gold.
            Value = (long)(basic.Value * Math.Pow(3, dim)),

            // Dimension items are priced in spirit wherever they are traded. The currency
            // is registered just above in OnLoaded, so stamping can validate against it.
            CurrencyId = SpiritCurrencyId,

            // Item.java:225-260 - dimension gear is freely tradeable.
            IsLore = false,
            IsBindOnPickup = false,
            IsBindOnEquip = false,
        };

        // Equipment only. AttributeSet.java:380-382 returns an empty set for anything that
        // is not equipment, so a tome must not pick up AC, attributes, HP/MP, resistances
        // or melee damage. Most of it would be inert on a consumable, but the generated
        // data would still be wrong - and it renders in the item window (Packets.cs:443).
        if (basic.UseType == ItemTemplate.UseTypes.Armor || basic.UseType == ItemTemplate.UseTypes.Weapon)
            clone.BaseStats += DimensionStats(basic, dim);

        // Spell tomes: teach the dimension's copy of the spell, and become consumables so
        // DimensionItem.csx can implement the upgrade rule. Inventory.cs:277 learns Scroll
        // items directly with no script hook; Inventory.cs:423 gives OneTime items one.
        //
        // A spell with no dimension clone (PreflightSpellIds can skip ids) keeps its base
        // id and stays a plain Scroll - a tome pointing at a nonexistent spell would fail
        // silently at Spellbook.cs:203.
        if (basic.UseType == ItemTemplate.UseTypes.Scroll
            && world.SpellHandler.GetSpell(basic.LearnSpellID + Offset * dim) != null)
        {
            clone.UseType = ItemTemplate.UseTypes.OneTime;
            clone.LearnSpellID = basic.LearnSpellID + Offset * dim;
        }

        return clone;
    }

    /// <summary>Points each dimension NPC's drops at that dimension's item templates.
    /// Items with no clone - gold, consumables, quest tokens - keep the base template.
    ///
    /// Every entry is a NEW NPCDropInfo: NPCTemplate's copy constructor copies the list but
    /// shares its elements (NPCTemplate.cs:251), so mutating one in place would retarget the
    /// base template's drop table and every other dimension's along with it.</summary>
    private void RepointDrops(GameWorld world)
    {
        for (int dim = 1; dim <= DimensionCount; dim++)
        {
            foreach (var basic in world.NPCHandler.GetTemplates()
                                       .Where(t => t.NPCTemplateID < Offset).ToList())
            {
                var clone = world.NPCHandler.GetNPCTemplate(basic.NPCTemplateID + Offset * dim);
                if (clone == null || basic.Drops == null) continue;

                var drops = new List<NPCDropInfo>();
                foreach (var drop in basic.Drops)
                {
                    var dimTemplate = world.ItemHandler.GetTemplate(drop.ItemTemplate.ID + Offset * dim);

                    drops.Add(new NPCDropInfo
                    {
                        ItemTemplate = dimTemplate ?? drop.ItemTemplate,
                        DropRate = drop.DropRate,
                        Stack = drop.Stack,
                    });
                }

                clone.Drops = drops;
            }
        }
    }

    /// <summary>Point each dimension vendor's stock at that dimension's item clones.
    ///
    /// New array AND new slot objects, never an in-place edit: NPCTemplate's copy
    /// constructor shares VendorItems with the base template (NPCTemplate.cs:254), so
    /// mutating either would rewrite dimension 0's shops. Same rule as RepointDrops.
    ///
    /// No vendor-side CurrencyId is set. The clones carry CurrencyId = "spirit" on the
    /// item, and Resolve puts the item override above the vendor (CurrencyHandler.cs:41),
    /// so repointed gear sells for spirit while unrepointed consumables stay gold.</summary>
    private void RepointVendorStock(GameWorld world)
    {
        for (int dim = 1; dim <= DimensionCount; dim++)
        {
            foreach (var basic in world.NPCHandler.GetTemplates()
                                       .Where(t => t.NPCTemplateID < Offset).ToList())
            {
                var clone = world.NPCHandler.GetNPCTemplate(basic.NPCTemplateID + Offset * dim);
                if (clone == null || basic.VendorItems == null) continue;

                var slots = new NPCVendorSlot[basic.VendorItems.Length];
                for (int i = 0; i < basic.VendorItems.Length; i++)
                {
                    var slot = basic.VendorItems[i];
                    if (slot == null) continue;

                    var dimTemplate = slot.ItemTemplate == null
                        ? null
                        : world.ItemHandler.GetTemplate(slot.ItemTemplate.ID + Offset * dim);

                    slots[i] = new NPCVendorSlot
                    {
                        Slot = slot.Slot,
                        ItemTemplate = dimTemplate ?? slot.ItemTemplate,
                        Stack = slot.Stack,
                        CanSeeStats = slot.CanSeeStats,
                    };
                }

                clone.VendorItems = slots;
            }
        }
    }

    /// <summary>AttributeSet.java:376, with itemType 0 - the flat per-dimension bonus only.
    /// The six suffix-specific terms live in DimensionSurname.csx, applied at roll time.
    ///
    /// Callers must apply this to equipment only: abyss returns an empty set for every
    /// other use type (AttributeSet.java:380-382). ScaleItemTemplate holds that guard.
    ///
    /// Baking this into the template rather than adding it per item is equivalent: abyss
    /// computes (template + item + dimensionDefault) * StatMultiplier (Item.java:459), and
    /// goose computes (template + item) * StatMultiplier (Item.cs:247), so folding it into
    /// the template leaves Legendary/Stunted multiplying the same total.</summary>
    private AttributeSet DimensionStats(ItemTemplate basic, int dim)
    {
        var a1 = basic.BaseStats;
        double tier = DimensionHelpers.Tier(basic);
        double half = 0.5 * dim;

        return new AttributeSet
        {
            AC = (int)(a1.AC * half + 10 * dim * tier),
            AirResist = (int)(a1.AirResist * half + 10 * dim * tier),
            EarthResist = (int)(a1.EarthResist * half + 10 * dim * tier),
            FireResist = (int)(a1.FireResist * half + 10 * dim * tier),
            WaterResist = (int)(a1.WaterResist * half + 10 * dim * tier),
            SpiritResist = (int)(a1.SpiritResist * half + 10 * dim * tier),
            Dexterity = (int)(a1.Dexterity * half + 15 * dim * tier),
            Stamina = (int)(a1.Stamina * half + 100 * dim * tier),
            Intelligence = (int)(a1.Intelligence * half + 100 * dim * tier),
            Strength = (int)(a1.Strength * half + 100 * dim * tier),

            HP = (long)(a1.HP * dim + Math.Pow(10 * dim, 4) * tier),
            MP = (long)(a1.MP * dim + Math.Pow(10 * dim, 4) * tier),

            DamageReduction = a1.DamageReduction * (decimal)half,
            Haste = a1.Haste * (decimal)half,
            SpellCrit = a1.SpellCrit * (decimal)half,
            SpellDamage = a1.SpellDamage * (decimal)half,
            HPPercentRegen = a1.HPPercentRegen * (decimal)half,
            MPPercentRegen = a1.MPPercentRegen * (decimal)half,
            HPStaticRegen = (int)(a1.HPStaticRegen * half),
            MPStaticRegen = (int)(a1.MPStaticRegen * half),

            // AttributeSet.java:433 casts the whole term to int. Ported faithfully, cast
            // included: the flat 10*dim*tier term dominates, and any base MeleeDamage
            // product below 1.0 truncates to nothing. MeleeDamage is a fraction on both
            // servers - damage *= (1 + MeleeDamage) at Player.java:316 and Player.cs:1616 -
            // so this is a very large bonus by design. User decision, 2026-08-10.
            MeleeDamage = (int)((double)a1.MeleeDamage * dim + 10 * dim * tier),
        };
    }
}
