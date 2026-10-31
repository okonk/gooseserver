namespace Goose
{
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
