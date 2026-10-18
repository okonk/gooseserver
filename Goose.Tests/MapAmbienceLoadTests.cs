using Goose;
using Xunit;

namespace Goose.Tests
{
    public class MapAmbienceLoadTests
    {
        [Fact]
        public void NewMap_DefaultsToNoneAndNoWeather()
        {
            Map map = new Map();

            Assert.Equal(LightingConfig.None, map.Lighting);
            Assert.Empty(map.Weather);
        }

        [Fact]
        public void CloneAs_CopiesLightingAndWeather()
        {
            Map source = new Map
            {
                ID = 1,
                Name = "1",
                characters = Array.Empty<ICharacter?>(),
                tiles = Array.Empty<ITile?>(),
            };
            source.Lighting = LightingConfig.Parse("cave", out _);
            source.Weather = WeatherDistribution.Parse("{\"Rain\":1,\"Snow\":2}", out _);

            Map clone = source.CloneAs(2, "2");

            Assert.Same(source.Lighting, clone.Lighting);
            Assert.Same(source.Weather, clone.Weather);
        }
    }
}
