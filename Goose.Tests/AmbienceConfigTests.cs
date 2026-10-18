using Goose;
using Xunit;

namespace Goose.Tests
{
    public class AmbienceConfigTests
    {
        [Theory]
        [InlineData("#fff", 0xFFFFFFFFu)]
        [InlineData("#001429", 0x001429FFu)]
        [InlineData("#090909ff", 0x090909FFu)]
        public void ParseHex_valid_forms(string hex, uint expected)
        {
            Assert.True(ColorUtils.TryParseHex(hex, out uint rgba));
            Assert.Equal(expected, rgba);
        }

        [Theory]
        [InlineData("090909")]
        [InlineData("#12")]
        [InlineData("#gggggg")]
        [InlineData(null)]
        [InlineData("")]
        public void ParseHex_invalid_forms(string? hex)
        {
            Assert.False(ColorUtils.TryParseHex(hex, out _));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Lighting_Blank_IsNoneWithoutWarning(string? raw)
        {
            LightingConfig config = LightingConfig.Parse(raw, out string? warning);
            Assert.Equal(LightingConfig.None, config);
            Assert.Null(warning);
        }

        [Fact]
        public void Lighting_EachPreset()
        {
            LightingConfig outside = LightingConfig.Parse("outside", out string? warning);
            Assert.Null(warning);
            Assert.Equal(0, outside.StartAlpha);
            Assert.Equal(0.70, outside.EndAlpha);
            Assert.Equal(1440, outside.CycleDurationSeconds);
            Assert.Equal(0x090909FFu, outside.TintColor);
            Assert.Equal(0x001429FFu, outside.FillColor);

            LightingConfig inside = LightingConfig.Parse("inside", out warning);
            Assert.Null(warning);
            Assert.Equal(0.1, inside.StartAlpha);
            Assert.Equal(0.2, inside.EndAlpha);
            Assert.Equal(1440, inside.CycleDurationSeconds);

            LightingConfig cave = LightingConfig.Parse("cave", out warning);
            Assert.Null(warning);
            Assert.Equal(0.70, cave.StartAlpha);
            Assert.Equal(0.70, cave.EndAlpha);
            Assert.Equal(1440, cave.CycleDurationSeconds);

            LightingConfig none = LightingConfig.Parse("none", out warning);
            Assert.Null(warning);
            Assert.Equal(LightingConfig.None, none);
        }

        [Fact]
        public void Lighting_MissingPresetKey_IsNone()
        {
            LightingConfig config = LightingConfig.Parse("{\"cycleDuration\":100}", out string? warning);
            Assert.Null(warning);
            Assert.Equal(0, config.StartAlpha);
            Assert.Equal(0, config.EndAlpha);
            Assert.Equal(100, config.CycleDurationSeconds);
        }

        [Fact]
        public void Lighting_SubSecondCycleDuration_KeepsPresetWithWarning()
        {
            LightingConfig config = LightingConfig.Parse("{\"preset\":\"outside\",\"cycleDuration\":0.5}", out string? warning);
            Assert.NotNull(warning);
            Assert.Equal(1440, config.CycleDurationSeconds);
        }

        [Fact]
        public void Lighting_UnknownPreset_FallsBackToNoneWithWarning()
        {
            LightingConfig config = LightingConfig.Parse("{\"preset\":\"dusky\"}", out string? warning);
            Assert.Equal(LightingConfig.None, config);
            Assert.NotNull(warning);
        }

        [Fact]
        public void Lighting_InvalidJson_FallsBackToNoneWithWarning()
        {
            LightingConfig config = LightingConfig.Parse("{preset:", out string? warning);
            Assert.Equal(LightingConfig.None, config);
            Assert.NotNull(warning);
        }

        [Fact]
        public void Lighting_OverridesApplyOnTopOfPreset()
        {
            LightingConfig config = LightingConfig.Parse("{\"preset\":\"cave\",\"endAlpha\":0.9,\"tintColor\":\"#ff0000\"}", out string? warning);
            Assert.Null(warning);
            Assert.Equal(0.70, config.StartAlpha);
            Assert.Equal(0.9, config.EndAlpha);
            Assert.Equal(0xFF0000FFu, config.TintColor);
            Assert.Equal(0x001429FFu, config.FillColor);
            Assert.Equal(1440, config.CycleDurationSeconds);
        }

        [Fact]
        public void Lighting_BadColorOverride_KeepsPresetColorWithWarning()
        {
            LightingConfig config = LightingConfig.Parse("{\"preset\":\"outside\",\"fillColor\":\"#zzz\"}", out string? warning);
            Assert.NotNull(warning);
            Assert.Equal(0x001429FFu, config.FillColor);
            Assert.Equal(0x090909FFu, config.TintColor);
        }

        [Fact]
        public void Lighting_AlphaClamped()
        {
            LightingConfig config = LightingConfig.Parse("{\"preset\":\"outside\",\"endAlpha\":4}", out string? warning);
            Assert.Equal(1.0, config.EndAlpha);
        }

        [Fact]
        public void Weather_ParsesCaseInsensitiveKeys()
        {
            WeatherDistribution dist = WeatherDistribution.Parse("{\"clear\":0.7,\"RAIN\":0.3}", out string? warning);
            Assert.Null(warning);
            Assert.Equal(2, dist.Count);
            Assert.Equal(0.7f, dist[WeatherType.Clear]);
            Assert.Equal(0.3f, dist[WeatherType.Rain]);
        }

        [Fact]
        public void Weather_UnknownKeySkippedWithWarning()
        {
            WeatherDistribution dist = WeatherDistribution.Parse("{\"Clear\":1,\"Hail\":1}", out string? warning);
            Assert.NotNull(warning);
            Assert.Equal(1, dist.Count);
            Assert.Equal(1f, dist[WeatherType.Clear]);
        }

        [Fact]
        public void Weather_NonPositiveWeightSkipped()
        {
            WeatherDistribution dist = WeatherDistribution.Parse("{\"Clear\":0,\"Rain\":1}", out string? warning);
            Assert.Equal(1, dist.Count);
            Assert.Equal(1f, dist[WeatherType.Rain]);
        }

        [Fact]
        public void Weather_NullWeightSkipped_KeepsOtherEntries()
        {
            WeatherDistribution dist = WeatherDistribution.Parse("{\"snow\":0.1,\"fog\":null}", out string? warning);
            Assert.NotNull(warning);
            Assert.Equal(1, dist.Count);
            Assert.Equal(0.1f, dist[WeatherType.Snow]);
        }

        [Fact]
        public void Weather_InvalidJson_EmptyWithWarning()
        {
            WeatherDistribution dist = WeatherDistribution.Parse("{clear:", out string? warning);
            Assert.Empty(dist);
            Assert.NotNull(warning);
        }

        [Fact]
        public void Weather_Blank_IsEmptyWithoutWarning()
        {
            Assert.Empty(WeatherDistribution.Parse("", out string? warning));
            Assert.Null(warning);
        }
    }
}
