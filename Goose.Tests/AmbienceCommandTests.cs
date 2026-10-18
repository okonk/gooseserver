using System.Collections.Generic;
using Goose.Testing;
using Xunit;

namespace Goose.Tests;

public class AmbienceCommandTests
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

    private sealed class Ctx : IDisposable
    {
        public TestWorldFixture Fixture { get; }
        public Map Map { get; }
        public TestWorldFixture.CapturingPlayer Gm { get; }

        public Ctx(Action<Map>? configureMap = null)
        {
            this.Fixture = new TestWorldFixture();
            this.Map = this.Fixture.AddBaseMap(1, "m", 60, 40);
            configureMap?.Invoke(this.Map);
            this.Gm = this.Fixture.CommandPlayerOn(this.Map, 5, 5, "Gm");
            this.Gm.Access = Player.AccessStatus.GameMaster;
            this.Map.AddPlayer(this.Gm, this.Fixture.World);
        }

        public void Dispose() => this.Fixture.Dispose();
    }

    private static void WithDayCycle(Ctx ctx)
    {
        ctx.Map.Lighting = LightingConfig.Parse("outside", out _);
        ctx.Map.Ambience = new MapAmbience(ctx.Map.Lighting, ctx.Map.Weather, new FakeRandom(0.0), ctx.Fixture.World.TimeNow);
    }

    [Fact]
    public void Settime_Dusk_MovesClockAndBroadcasts()
    {
        using var ctx = new Ctx();
        WithDayCycle(ctx);

        Assert.True(ctx.Fixture.RunCommand(ctx.Gm, "/settime dusk"));

        long elapsed = ctx.Map.Ambience.ElapsedMs(ctx.Fixture.World.TimeNow);
        Assert.InRange(elapsed, 1_080_000 - 1_000, 1_080_000 + 1_000);
        Assert.Contains(ctx.Gm.Sent, s => s.StartsWith("AMB"));
    }

    [Fact]
    public void Settime_NumericHours()
    {
        using var ctx = new Ctx();
        WithDayCycle(ctx);

        Assert.True(ctx.Fixture.RunCommand(ctx.Gm, "/settime 13.5"));

        Assert.Contains(ctx.Gm.Sent, s => s.StartsWith("$7Set time to 13:30."));
    }

    [Fact]
    public void Settime_NoDayCycle_Errors_NoBroadcast()
    {
        using var ctx = new Ctx();

        Assert.True(ctx.Fixture.RunCommand(ctx.Gm, "/settime dusk"));

        Assert.Contains(ctx.Gm.Sent, s => s.StartsWith("$7This map has no day cycle."));
        Assert.DoesNotContain(ctx.Gm.Sent, s => s.StartsWith("AMB"));
    }

    [Fact]
    public void Settime_Invalid_Errors()
    {
        using var ctx = new Ctx();
        WithDayCycle(ctx);

        Assert.True(ctx.Fixture.RunCommand(ctx.Gm, "/settime 25"));
        Assert.True(ctx.Fixture.RunCommand(ctx.Gm, "/settime brunch"));

        Assert.Equal(2, ctx.Gm.Sent.Count(s => s.StartsWith("$7Invalid time. Use 0-24 or dawn, noon, dusk, night.")));
        Assert.DoesNotContain(ctx.Gm.Sent, s => s.StartsWith("AMB"));
    }

    [Fact]
    public void Setweather_SetsBroadcastsAndRestartsTimer()
    {
        using var ctx = new Ctx(m =>
        {
            m.Weather = new WeatherDistribution { { WeatherType.Clear, 1.0f }, { WeatherType.Rain, 1.0f } };
        });
        var world = ctx.Fixture.World;
        var ambience = new MapAmbience(ctx.Map.Lighting, ctx.Map.Weather, new FakeRandom(0.0), world.TimeNow);
        ctx.Map.Ambience = ambience;
        ambience.ScheduleWeatherRoll(ctx.Map, world);
        Event? first = ambience.PendingRoll;
        int countBefore = world.EventHandler.Count;

        Assert.True(ctx.Fixture.RunCommand(ctx.Gm, "/setweather storm"));

        Assert.Equal(WeatherType.Storm, ctx.Map.Ambience.CurrentWeather);
        Assert.NotNull(ctx.Map.Ambience.PendingRoll);
        Assert.NotSame(first, ctx.Map.Ambience.PendingRoll);
        Assert.Equal(countBefore, world.EventHandler.Count);
        Assert.Contains(ctx.Gm.Sent, s => s.StartsWith("AMB"));
        Assert.Contains(ctx.Gm.Sent, s => s.StartsWith("$7Set weather to Storm."));
    }

    [Fact]
    public void Setweather_NoConfig_StillSets_NoEvent()
    {
        using var ctx = new Ctx();
        var world = ctx.Fixture.World;
        int countBefore = world.EventHandler.Count;

        Assert.True(ctx.Fixture.RunCommand(ctx.Gm, "/setweather fog"));

        Assert.Equal(WeatherType.Fog, ctx.Map.Ambience.CurrentWeather);
        Assert.Null(ctx.Map.Ambience.PendingRoll);
        Assert.Equal(countBefore, world.EventHandler.Count);
        Assert.Contains(ctx.Gm.Sent, s => s.StartsWith("$7Set weather to Fog."));
    }

    [Fact]
    public void Setweather_Unknown_Errors()
    {
        using var ctx = new Ctx();

        Assert.True(ctx.Fixture.RunCommand(ctx.Gm, "/setweather hail"));
        Assert.True(ctx.Fixture.RunCommand(ctx.Gm, "/setweather 7"));

        Assert.Equal(2, ctx.Gm.Sent.Count(s => s.StartsWith("$7Unknown weather. Use: Clear, Rain, Fog, Storm, Snow.")));
        Assert.DoesNotContain(ctx.Gm.Sent, s => s.StartsWith("AMB"));
    }

    [Fact]
    public void NormalPlayer_Refused()
    {
        using var ctx = new Ctx(m =>
        {
            m.Weather = new WeatherDistribution { { WeatherType.Clear, 1.0f } };
        });
        ctx.Gm.Access = Player.AccessStatus.Normal;

        Assert.True(ctx.Fixture.RunCommand(ctx.Gm, "/setweather rain"));

        Assert.Equal(WeatherType.Clear, ctx.Map.Ambience.CurrentWeather);
        Assert.Empty(ctx.Gm.Sent);
    }
}
