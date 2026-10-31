using System.Text.Json.Serialization;

namespace Goose
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum EnchantElement { Earth, Water, Fire, Air, Spirit }

    public class EnchantSettings
    {
        public int MaxLevel { get; set; } = 10;
        public int[] FailPercent { get; set; } = [0, 0, 0, 0, 0, 30, 40, 50, 60, 70];
        public double EarthSpellDamagePercent { get; set; } = 1;
        public double WaterSpellCritPercent { get; set; } = 1;
        public double FireDamageReductionPercent { get; set; } = 0.5;
        public double AirMPPercentRegenPercent { get; set; } = 0.04;
        public double SpiritHPPercentRegenPercent { get; set; } = 0.04;
        public Dictionary<int, EnchantElement> EssenceTemplates { get; set; } = [];
        public int ProtectionTemplateId { get; set; }

        public void Validate(ItemHandler items)
        {
            if (this.MaxLevel < 1)
                throw new FatalStartupException("Enchanting MaxLevel must be at least 1");
            if (this.FailPercent is null || this.FailPercent.Length != this.MaxLevel)
                throw new FatalStartupException(
                    "Enchanting FailPercent length must equal MaxLevel (" + this.MaxLevel + ")");
            for (int i = 0; i < this.FailPercent.Length; i++)
            {
                if (this.FailPercent[i] < 0 || this.FailPercent[i] > 100)
                    throw new FatalStartupException(
                        "Enchanting FailPercent[" + i + "] must be between 0 and 100");
            }
            if (this.EssenceTemplates is null)
                throw new FatalStartupException("Enchanting EssenceTemplates must not be null");
            foreach (var (templateId, element) in this.EssenceTemplates)
            {
                if (items.GetTemplate(templateId) is null)
                    throw new FatalStartupException(
                        "Enchanting essence template " + templateId + " not found");
                if (!Enum.IsDefined(typeof(EnchantElement), element))
                    throw new FatalStartupException(
                        "Enchanting essence template " + templateId
                        + " has undefined element value " + (int)element);
            }
            if (this.ProtectionTemplateId != 0 && items.GetTemplate(this.ProtectionTemplateId) is null)
                throw new FatalStartupException(
                    "Enchanting protection template " + this.ProtectionTemplateId + " not found");
        }
    }
}
