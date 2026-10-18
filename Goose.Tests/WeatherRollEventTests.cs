using System.Diagnostics;
using Goose;
using Goose.Testing;
using Xunit;

namespace Goose.Tests
{
    public class WeatherRollEventTests
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

        private static (TestWorldFixture fixture, Map map, TestWorldFixture.CapturingPlayer player, GameWorld world)
            Setup(Action<Map>? configure = null)
        {
            var fixture = new TestWorldFixture();
            var map = fixture.AddBaseMap(1, "m");
            configure?.Invoke(map);
            var player = fixture.CommandPlayerOn(map, 1, 1);
            map.Players.Add(player);
            return (fixture, map, player, fixture.World);
        }

        private static MapAmbience Ambience(Map map, FakeRandom random)
        {
            var ambience = new MapAmbience(map.Lighting, map.Weather, random, Stopwatch.GetTimestamp());
            map.Ambience = ambience;
            return ambience;
        }

        [Fact]
        public void Schedule_EmptyConfig_QueuesNothing()
        {
            var (fixture, map, player, world) = Setup();
            var ambience = Ambience(map, new FakeRandom());

            int before = world.EventHandler.Count;
            ambience.ScheduleWeatherRoll(map, world);

            Assert.Null(ambience.PendingRoll);
            Assert.Equal(before, world.EventHandler.Count);
        }

        [Fact]
        public void Schedule_Twice_LeavesOneEvent()
        {
            var (fixture, map, player, world) = Setup(m =>
                m.Weather = new WeatherDistribution { { WeatherType.Clear, 1.0f } });
            var ambience = Ambience(map, new FakeRandom(0.0));

            int before = world.EventHandler.Count;
            ambience.ScheduleWeatherRoll(map, world);
            Event first = ambience.PendingRoll!;
            ambience.ScheduleWeatherRoll(map, world);

            Assert.NotSame(first, ambience.PendingRoll);
            Assert.Equal(before + 1, world.EventHandler.Count);
        }

        [Fact]
        public void Ready_Hit_ChangesWeatherBroadcastsAndReschedules()
        {
            var (fixture, map, player, world) = Setup(m =>
                m.Weather = new WeatherDistribution { { WeatherType.Clear, 0.9f }, { WeatherType.Rain, 0.1f } });
            var ambience = Ambience(map, new FakeRandom(0.0, 0.0, 0.1));
            Assert.Equal(WeatherType.Clear, ambience.CurrentWeather);

            ambience.ScheduleWeatherRoll(map, world);
            Event ev = ambience.PendingRoll!;
            // PriorityQueue keys are fixed at enqueue time, so re-enqueue to advance the tick
            world.EventHandler.RemoveEvent(ev);
            ev.Ticks = world.TimeNow;
            world.EventHandler.AddEvent(ev);
            world.EventHandler.Update(world);

            Assert.Equal(WeatherType.Rain, ambience.CurrentWeather);
            var amb = player.Sent.Where(s => s.StartsWith("AMB")).ToList();
            Assert.Single(amb);
            Assert.EndsWith(",1", amb[0]);
            Assert.Same(ev, ambience.PendingRoll);
            long now = world.TimeNow;
            Assert.InRange(ambience.PendingRoll!.Ticks, now + Ticks(599), now + Ticks(601));
        }

        [Fact]
        public void Ready_Miss_NoBroadcastButReschedules()
        {
            var (fixture, map, player, world) = Setup(m =>
                m.Weather = new WeatherDistribution { { WeatherType.Clear, 0.5f }, { WeatherType.Rain, 0.5f } });
            var ambience = Ambience(map, new FakeRandom(0.0, 0.9));

            ambience.ScheduleWeatherRoll(map, world);
            Event ev = ambience.PendingRoll!;
            // PriorityQueue keys are fixed at enqueue time, so re-enqueue to advance the tick
            world.EventHandler.RemoveEvent(ev);
            ev.Ticks = world.TimeNow;
            world.EventHandler.AddEvent(ev);
            world.EventHandler.Update(world);

            Assert.Equal(WeatherType.Clear, ambience.CurrentWeather);
            Assert.DoesNotContain(player.Sent, s => s.StartsWith("AMB"));
            Assert.Same(ev, ambience.PendingRoll);
            long now = world.TimeNow;
            Assert.InRange(ambience.PendingRoll!.Ticks, now + Ticks(599), now + Ticks(601));
        }

        [Fact]
        public void Ready_StaleEvent_DoesNothing()
        {
            var (fixture, map, player, world) = Setup(m =>
                m.Weather = new WeatherDistribution { { WeatherType.Clear, 0.5f }, { WeatherType.Rain, 0.5f } });
            var ambience = Ambience(map, new FakeRandom(0.0, 0.0, 0.1));

            ambience.ScheduleWeatherRoll(map, world);
            Event first = ambience.PendingRoll!;
            ambience.ScheduleWeatherRoll(map, world);
            Event second = ambience.PendingRoll!;
            Assert.NotSame(first, second);

            // Re-enqueue to actually fire it: the second schedule removed it, and
            // PriorityQueue keys are fixed at enqueue time
            first.Ticks = world.TimeNow;
            world.EventHandler.AddEvent(first);
            int before = world.EventHandler.Count;
            world.EventHandler.Update(world);

            Assert.DoesNotContain(player.Sent, s => s.StartsWith("AMB"));
            Assert.Equal(before - 1, world.EventHandler.Count);
            Assert.Same(second, ambience.PendingRoll);
        }
    }
}
