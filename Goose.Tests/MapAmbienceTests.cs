using System.Diagnostics;
using Goose;
using Xunit;

namespace Goose.Tests
{
    public class MapAmbienceTests
    {
        private sealed class FakeRandom : Random
        {
            private readonly Queue<double> values;

            public FakeRandom(params double[] values)
            {
                this.values = new Queue<double>(values);
            }

            public override double NextDouble()
            {
                return this.values.Dequeue();
            }
        }

        private static long Ticks(long seconds) => (long)(seconds * Stopwatch.Frequency);

        private static MapAmbience Outside(FakeRandom random, long now)
        {
            return new MapAmbience(LightingConfig.Parse("outside", out _), new WeatherDistribution(), random, now);
        }

        [Fact]
        public void NewAmbience_UndefinedNumericWeatherConfig_DoesNotThrow()
        {
            WeatherDistribution dist = WeatherDistribution.Parse("{\"1\":1}", out _);
            MapAmbience ambience = new(LightingConfig.None, dist, new Random(1), 0);

            Assert.Equal(WeatherType.Clear, ambience.CurrentWeather);
        }

        [Fact]
        public void NewAmbience_StartsAtNineAm()
        {
            long now = Stopwatch.GetTimestamp();
            MapAmbience ambience = Outside(new FakeRandom(), now);

            Assert.Equal(540_000, ambience.ElapsedMs(now));
        }

        [Fact]
        public void ElapsedMs_WrapsAtCycleEnd()
        {
            long now = Stopwatch.GetTimestamp();
            MapAmbience ambience = Outside(new FakeRandom(), now);

            Assert.Equal(600_000, ambience.ElapsedMs(now + Ticks(1500)));
        }

        [Fact]
        public void ElapsedMs_DoesNotOverflowAfterLongUptime()
        {
            long now = Stopwatch.GetTimestamp();
            MapAmbience ambience = Outside(new FakeRandom(), now);

            long later = now + Ticks(200 * 86400);
            long elapsed = ambience.ElapsedMs(later);

            Assert.InRange(elapsed, 0, 1_440_000 - 1);
            Assert.Equal((540_000 + 200L * 86400 * 1000) % 1_440_000, elapsed);
        }

        [Fact]
        public void SetTimeOfDay_MovesClock()
        {
            long now = Stopwatch.GetTimestamp();
            MapAmbience ambience = Outside(new FakeRandom(), now);

            ambience.SetTimeOfDay(18, now);

            Assert.Equal(1_080_000, ambience.ElapsedMs(now));
        }

        [Fact]
        public void None_HasNoDayCycle_ElapsedZero_SetTimeThrows()
        {
            long now = Stopwatch.GetTimestamp();
            MapAmbience ambience = new MapAmbience(LightingConfig.None, new WeatherDistribution(), new FakeRandom(), now);

            Assert.False(ambience.HasDayCycle);
            Assert.Equal(0, ambience.ElapsedMs(now));
            Assert.Throws<InvalidOperationException>(() => ambience.SetTimeOfDay(12, now));
        }

        [Fact]
        public void CloneForNewMap_SharesClock()
        {
            long t0 = Stopwatch.GetTimestamp();
            MapAmbience parent = Outside(new FakeRandom(), t0);
            parent.SetTimeOfDay(20, t0);

            MapAmbience clone = parent.CloneForNewMap(t0 + Ticks(30));

            Assert.Equal(parent.ElapsedMs(t0 + Ticks(60)), clone.ElapsedMs(t0 + Ticks(60)));
        }

        [Fact]
        public void TryRollWeather_Miss_KeepsWeather()
        {
            long now = Stopwatch.GetTimestamp();
            var weather = new WeatherDistribution { { WeatherType.Clear, 1.0f } };
            MapAmbience ambience = new MapAmbience(LightingConfig.None, weather, new FakeRandom(0.0, 0.7), now);

            Assert.Equal(WeatherType.Clear, ambience.CurrentWeather);
            Assert.False(ambience.TryRollWeather());
            Assert.Equal(WeatherType.Clear, ambience.CurrentWeather);
        }

        [Fact]
        public void TryRollWeather_Hit_NeverPicksCurrent()
        {
            long now = Stopwatch.GetTimestamp();
            var weather = new WeatherDistribution { { WeatherType.Clear, 0.9f }, { WeatherType.Rain, 0.1f } };
            MapAmbience ambience = new MapAmbience(LightingConfig.None, weather, new FakeRandom(0.0, 0.1, 0.0), now);

            Assert.Equal(WeatherType.Clear, ambience.CurrentWeather);
            Assert.True(ambience.TryRollWeather());
            Assert.Equal(WeatherType.Rain, ambience.CurrentWeather);
        }

        [Fact]
        public void TryRollWeather_SingleTypeAtThatType_NoChange()
        {
            long now = Stopwatch.GetTimestamp();
            var weather = new WeatherDistribution { { WeatherType.Snow, 1.0f } };
            MapAmbience ambience = new MapAmbience(LightingConfig.None, weather, new FakeRandom(0.0, 0.1), now);

            Assert.Equal(WeatherType.Snow, ambience.CurrentWeather);
            Assert.False(ambience.TryRollWeather());
            Assert.Equal(WeatherType.Snow, ambience.CurrentWeather);
        }

        [Fact]
        public void TryRollWeather_SingleTypeAfterGmChange_ReturnsToConfigured()
        {
            long now = Stopwatch.GetTimestamp();
            var weather = new WeatherDistribution { { WeatherType.Snow, 1.0f } };
            MapAmbience ambience = new MapAmbience(LightingConfig.None, weather, new FakeRandom(0.0, 0.1, 0.0), now);
            ambience.SetWeather(WeatherType.Rain);

            Assert.True(ambience.TryRollWeather());
            Assert.Equal(WeatherType.Snow, ambience.CurrentWeather);
        }

        [Fact]
        public void TryRollWeather_MultipleCandidates_WalksWeights()
        {
            long now = Stopwatch.GetTimestamp();
            var weather = new WeatherDistribution { { WeatherType.Rain, 0.2f }, { WeatherType.Fog, 0.3f }, { WeatherType.Storm, 0.5f } };

            MapAmbience midWalk = new MapAmbience(LightingConfig.None, weather, new FakeRandom(0.0, 0.0, 0.49), now);
            midWalk.SetWeather(WeatherType.Clear);
            Assert.True(midWalk.TryRollWeather());
            Assert.Equal(WeatherType.Fog, midWalk.CurrentWeather);

            MapAmbience lastCandidate = new MapAmbience(LightingConfig.None, weather, new FakeRandom(0.0, 0.0, 0.99), now);
            lastCandidate.SetWeather(WeatherType.Clear);
            Assert.True(lastCandidate.TryRollWeather());
            Assert.Equal(WeatherType.Storm, lastCandidate.CurrentWeather);

            MapAmbience boundary = new MapAmbience(LightingConfig.None, weather, new FakeRandom(0.0, 0.0, 0.2), now);
            boundary.SetWeather(WeatherType.Clear);
            Assert.True(boundary.TryRollWeather());
            Assert.Equal(WeatherType.Fog, boundary.CurrentWeather);
        }

        [Fact]
        public void BuildPacket_Format()
        {
            long now = Stopwatch.GetTimestamp();
            var weather = new WeatherDistribution { { WeatherType.Rain, 1.0f } };
            MapAmbience ambience = new MapAmbience(LightingConfig.Parse("outside", out _), weather, new FakeRandom(0.0), now);

            Assert.Equal("AMB0,700,151587327,1321471,540000,1440000,1", ambience.BuildPacket(now));
        }

        [Fact]
        public void BuildPacket_None()
        {
            long now = Stopwatch.GetTimestamp();
            MapAmbience ambience = new MapAmbience(LightingConfig.None, new WeatherDistribution(), new FakeRandom(), now);

            Assert.Equal("AMB0,0,151587327,1321471,0,0,0", ambience.BuildPacket(now));
        }
    }
}
