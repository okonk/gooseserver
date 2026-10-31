using System.Text.Json;
using Goose;
using Xunit;

namespace Goose.Tests
{
    public class EnchantSettingsTests
    {
        private static ItemHandler ItemHandlerWithTemplates(params int[] ids)
        {
            var handler = new ItemHandler();
            foreach (int id in ids)
            {
                handler.AddTemplate(new ItemTemplate
                {
                    ID = id, Name = "Template " + id, BaseStats = new AttributeSet(),
                });
            }
            return handler;
        }

        [Fact]
        public void Defaults_match_design_table()
        {
            var s = new EnchantSettings();
            Assert.Equal(10, s.MaxLevel);
            Assert.Equal([0, 0, 0, 0, 0, 10, 20, 30, 40, 50], s.FailPercent);
            Assert.Equal(1, s.EarthSpellDamagePercent);
            Assert.Equal(1, s.WaterSpellCritPercent);
            Assert.Equal(0.5, s.FireDamageReductionPercent);
            Assert.Equal(0.04, s.AirMPPercentRegenPercent);
            Assert.Equal(0.04, s.SpiritHPPercentRegenPercent);
            Assert.Empty(s.EssenceTemplates);
            Assert.Equal(0, s.ProtectionTemplateId);
        }

        [Fact]
        public void Validate_defaults_with_no_essences_passes()
        {
            var s = new EnchantSettings();
            s.Validate(ItemHandlerWithTemplates());
        }

        [Fact]
        public void Validate_known_templates_passes()
        {
            var s = new EnchantSettings
            {
                EssenceTemplates = { [5] = EnchantElement.Fire, [6] = EnchantElement.Water },
                ProtectionTemplateId = 7,
            };
            s.Validate(ItemHandlerWithTemplates(5, 6, 7));
        }

        [Fact]
        public void Validate_max_level_below_one_throws()
        {
            var s = new EnchantSettings { MaxLevel = 0, FailPercent = [] };
            Assert.Throws<FatalStartupException>(() => s.Validate(ItemHandlerWithTemplates()));
        }

        [Fact]
        public void Validate_fail_percent_length_mismatch_throws()
        {
            var s = new EnchantSettings { FailPercent = [0, 10] };
            Assert.Throws<FatalStartupException>(() => s.Validate(ItemHandlerWithTemplates()));
        }

        [Fact]
        public void Validate_fail_percent_out_of_range_throws()
        {
            var s = new EnchantSettings { FailPercent = [0, 0, 0, 0, 0, 101, 20, 30, 40, 50] };
            Assert.Throws<FatalStartupException>(() => s.Validate(ItemHandlerWithTemplates()));
        }

        [Fact]
        public void Validate_fail_percent_negative_throws()
        {
            var s = new EnchantSettings { FailPercent = [0, 0, 0, 0, 0, -1, 20, 30, 40, 50] };
            Assert.Throws<FatalStartupException>(() => s.Validate(ItemHandlerWithTemplates()));
        }

        [Fact]
        public void Validate_unknown_essence_template_throws()
        {
            var s = new EnchantSettings { EssenceTemplates = { [5] = EnchantElement.Fire } };
            Assert.Throws<FatalStartupException>(() => s.Validate(ItemHandlerWithTemplates(6)));
        }

        [Fact]
        public void Validate_unknown_protection_template_throws()
        {
            var s = new EnchantSettings { ProtectionTemplateId = 7 };
            Assert.Throws<FatalStartupException>(() => s.Validate(ItemHandlerWithTemplates(5)));
        }

        [Fact]
        public void Deserialize_essence_templates_uses_enum_names()
        {
            var json = """{"Enchanting":{"EssenceTemplates":{"5":"Fire"}}}""";
            var settings = JsonSerializer.Deserialize<GooseSettings>(json, JsonHelper.SettingsOptions);

            Assert.NotNull(settings);
            Assert.NotNull(settings.Enchanting);
            Assert.Equal(EnchantElement.Fire, settings.Enchanting!.EssenceTemplates[5]);
        }
    }
}
