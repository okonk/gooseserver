using System.Diagnostics;
using Goose.Events;

namespace Goose
{
    public sealed class MapAmbience
    {
        public const int WeatherRollSeconds = 600;
        public const double StartHour = 9.0;

        private readonly LightingConfig lighting;
        private readonly WeatherDistribution weather;
        private readonly Random random;
        private long cycleStartTicks;

        public MapAmbience(LightingConfig lighting, WeatherDistribution weather, Random random, long nowTicks)
        {
            this.lighting = lighting;
            this.weather = weather;
            this.random = random;
            this.cycleStartTicks = nowTicks - SecondsToTicks(StartHour / 24.0 * lighting.CycleDurationSeconds);
            this.CurrentWeather = this.weather.Count == 0 ? WeatherType.Clear : this.RollFrom(this.ConfiguredTypes());
        }

        public LightingConfig Lighting => this.lighting;
        public WeatherDistribution Weather => this.weather;
        public bool HasDayCycle => this.lighting.CycleDurationSeconds > 0;
        public WeatherType CurrentWeather { get; private set; }
        internal Event? PendingRoll { get; set; }

        private static long SecondsToTicks(double seconds) => (long)(seconds * Stopwatch.Frequency);

        public long ElapsedMs(long nowTicks)
        {
            if (!this.HasDayCycle) return 0;

            long durationMs = (long)(this.lighting.CycleDurationSeconds * 1000.0);
            return (long)((nowTicks - this.cycleStartTicks) * 1000.0 / Stopwatch.Frequency) % durationMs;
        }

        public void SetTimeOfDay(double hours, long nowTicks)
        {
            if (!this.HasDayCycle) throw new InvalidOperationException();

            this.cycleStartTicks = nowTicks - SecondsToTicks(hours / 24.0 * this.lighting.CycleDurationSeconds);
        }

        public void SetWeather(WeatherType weather)
        {
            this.CurrentWeather = weather;
        }

        public bool TryRollWeather()
        {
            if (this.random.NextDouble() >= 0.5) return false;

            List<WeatherType> candidates = this.ConfiguredTypes()
                .Where(t => t != this.CurrentWeather)
                .ToList();
            if (candidates.Count == 0) return false;

            this.CurrentWeather = this.RollFrom(candidates);
            return true;
        }

        private List<WeatherType> ConfiguredTypes() => Enum.GetValues<WeatherType>()
            .Where(t => this.weather.ContainsKey(t))
            .ToList();

        private WeatherType RollFrom(List<WeatherType> candidates)
        {
            double total = 0;
            foreach (WeatherType t in candidates) total += this.weather[t];

            double r = this.random.NextDouble() * total;
            WeatherType last = candidates[^1];
            foreach (WeatherType t in candidates)
            {
                r -= this.weather[t];
                if (r < 0) return t;
                last = t;
            }
            return last;
        }

        public string BuildPacket(long nowTicks)
        {
            return P.MapAmbience(
                (int)Math.Round(this.lighting.StartAlpha * 1000),
                (int)Math.Round(this.lighting.EndAlpha * 1000),
                this.lighting.TintColor,
                this.lighting.FillColor,
                this.ElapsedMs(nowTicks),
                (long)(this.lighting.CycleDurationSeconds * 1000),
                this.CurrentWeather);
        }

        public void Broadcast(Map map, GameWorld world)
        {
            world.SendToMap(map, this.BuildPacket(world.TimeNow));
        }

        public void ScheduleWeatherRoll(Map map, GameWorld world)
        {
            if (this.PendingRoll is not null)
            {
                world.EventHandler.RemoveEvent(this.PendingRoll);
                this.PendingRoll = null;
            }

            if (this.weather.Count == 0) return;

            WeatherRollEvent ev = new()
            {
                Ticks = world.TimeNow + world.TimerFrequency * WeatherRollSeconds,
                Data = map,
            };
            world.EventHandler.AddEvent(ev);
            this.PendingRoll = ev;
        }

        public MapAmbience CloneForNewMap(long nowTicks)
        {
            MapAmbience clone = new MapAmbience(this.lighting, this.weather, this.random, nowTicks);
            clone.cycleStartTicks = this.cycleStartTicks;
            return clone;
        }
    }
}
