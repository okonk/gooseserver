namespace Goose
{
    public enum EnchantOutcome { Success, ProtectedFailure, Destroyed }

    public static class Enchanting
    {
        public static int Count(Item item, EnchantElement element)
        {
            return item.GetProperty<int>(ToProperty(element));
        }

        public static int Level(Item item)
        {
            return Count(item, EnchantElement.Earth)
                + Count(item, EnchantElement.Water)
                + Count(item, EnchantElement.Fire)
                + Count(item, EnchantElement.Air)
                + Count(item, EnchantElement.Spirit);
        }

        public static int Max(Item item, EnchantSettings settings)
        {
            return settings.MaxLevel - item.GetProperty<int>(ItemProperty.EnchantMaxLoss);
        }

        public static int FailPercent(Item item, EnchantSettings s)
        {
            int level = Level(item);
            return level >= Max(item, s) ? 0 : s.FailPercent[level];
        }

        public static bool CanEnchant(Item item, EnchantSettings s, out string reason)
        {
            if (item.UseType is not (ItemTemplate.UseTypes.Armor or ItemTemplate.UseTypes.Weapon))
            {
                reason = "Only armor and weapons can be enchanted.";
                return false;
            }
            if (item.Slot is ItemTemplate.ItemSlots.Mount or ItemTemplate.ItemSlots.Backpack)
            {
                reason = "This item cannot be enchanted.";
                return false;
            }
            if (Level(item) >= Max(item, s))
            {
                reason = "This item has reached its maximum enchant level.";
                return false;
            }
            reason = "";
            return true;
        }

        public static EnchantOutcome Resolve(Item item, EnchantElement element, bool protectionAvailable,
                                             EnchantSettings s, Random random)
        {
            if (!CanEnchant(item, s, out _))
                throw new InvalidOperationException();

            int failPercent = FailPercent(item, s);
            if (failPercent > 0 && random.Next(1, 101) <= failPercent)
            {
                if (protectionAvailable)
                {
                    item.ItemProperties[ItemProperty.EnchantMaxLoss] =
                        item.GetProperty<int>(ItemProperty.EnchantMaxLoss) + 1;
                    return EnchantOutcome.ProtectedFailure;
                }
                return EnchantOutcome.Destroyed;
            }

            item.Name = StripSuffix(item);
            item.ItemProperties[ToProperty(element)] = Count(item, element) + 1;
            ApplySuffix(item);
            return EnchantOutcome.Success;
        }

        public static string StripSuffix(Item item)
        {
            string suffix = $" +{Level(item)}";
            if (Level(item) > 0 && item.Name.EndsWith(suffix))
                return item.Name[..^suffix.Length];
            return item.Name;
        }

        public static void ApplySuffix(Item item)
        {
            item.Name = StripSuffix(item);
            if (Level(item) > 0)
                item.Name += $" +{Level(item)}";
        }

        public static AttributeSet Stats(Item item, EnchantSettings settings)
        {
            var stats = new AttributeSet();
            stats.SpellDamage += Count(item, EnchantElement.Earth) * settings.EarthSpellDamagePercent / 100;
            stats.SpellCrit += Count(item, EnchantElement.Water) * settings.WaterSpellCritPercent / 100;
            stats.DamageReduction += Count(item, EnchantElement.Fire) * settings.FireDamageReductionPercent / 100;
            stats.MPPercentRegen += Count(item, EnchantElement.Air) * settings.AirMPPercentRegenPercent / 100;
            stats.HPPercentRegen += Count(item, EnchantElement.Spirit) * settings.SpiritHPPercentRegenPercent / 100;
            return stats;
        }

        private static ItemProperty ToProperty(EnchantElement element) => element switch
        {
            EnchantElement.Earth => ItemProperty.EnchantEarth,
            EnchantElement.Water => ItemProperty.EnchantWater,
            EnchantElement.Fire => ItemProperty.EnchantFire,
            EnchantElement.Air => ItemProperty.EnchantAir,
            EnchantElement.Spirit => ItemProperty.EnchantSpirit,
            _ => throw new System.ArgumentOutOfRangeException(nameof(element)),
        };
    }
}
