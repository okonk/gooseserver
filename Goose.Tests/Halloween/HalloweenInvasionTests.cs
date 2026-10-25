using System.Linq;
using System.Text.Json.Nodes;
using Goose;
using Goose.Scripting;
using Goose.Testing;
using Xunit;

namespace Goose.Tests.Halloween;

[Collection("NLog")]
public class HalloweenInvasionTests
{
    private static readonly long Now = 1000;
    private const int TownMapId = 38;
    private static readonly int[] Rect = [1, 1, 10, 10];
    private static readonly int[] WaveRng = [0, 0, 0, 0, 0, 1, 0, 0, 2, 0, 0, 3, 0, 0, 4, 0];

    [Fact]
    public void No_wave_before_tier3()
    {
        var (kit, script, map, listener) = Setup();
        kit.World.CommunityGoals.SetFlag("halloween.active", 1, null);
        kit.World.CommunityGoals.SetFlag("halloween.tier1", 1, null);
        kit.World.CommunityGoals.SetFlag("halloween.tier2", 1, null);

        HalloweenTestKit.Call(script.Object, "Tick", kit.World, Now, Rng([0, 0, 0, 0]));

        Assert.Empty(map.NPCs);
        Assert.DoesNotContain(listener.Sent, s => s.Contains("[Event]"));
    }

    [Fact]
    public void Wave_spawns_count_in_rect_and_announces()
    {
        var (kit, script, map, listener) = Setup();
        On(kit);

        HalloweenTestKit.Call(script.Object, "Tick", kit.World, Now, Rng(WaveRng));

        Assert.Equal(5, map.NPCs.Count);
        Assert.All(map.NPCs, n =>
        {
            Assert.Equal(kit.WailingGhostId, n.NPCTemplateID);
            Assert.False(n.ShouldRespawn);
            Assert.True(n.Properties.TryGetProperty<int>("invasion", out int tag));
            Assert.Equal(1, tag);
            Assert.False(n.Properties.TryGetProperty<int>("candyMin", out _));
            Assert.False(n.Properties.TryGetProperty<int>("candyMax", out _));
            Assert.True(n.Properties.TryGetProperty<long>("spawnedAt", out long spawnedAt));
            Assert.Equal(Now, spawnedAt);
            Assert.InRange(n.MapX, Rect[0], Rect[0] + Rect[2] - 1);
            Assert.InRange(n.MapY, Rect[1], Rect[1] + Rect[3] - 1);
        });
        Assert.Contains(listener.Sent, s => s.Contains("[Event] The party has spilled into Grove!"));

        HalloweenTestKit.Call(script.Object, "Tick", kit.World, Now + 1, Rng([0, 0, 0, 0]));
        Assert.Equal(5, map.NPCs.Count);
        Assert.Equal(1, listener.Sent.Count(s => s.Contains("The party has spilled into Grove!")));
    }

    [Fact]
    public void Invaders_wander_and_head_for_a_home_near_the_center()
    {
        var (kit, script, map, _) = Setup();
        kit.SetConfig(cfg =>
        {
            cfg["invasion"]!["towns"]![0]!["center"] = new JsonArray(20, 20);
            cfg["invasion"]!["towns"]![0]!["homeRadius"] = 2;
        });
        On(kit);

        HalloweenTestKit.Call(script.Object, "Tick", kit.World, Now, Rng(WaveRng));

        Assert.Equal(5, map.NPCs.Count);
        Assert.All(map.NPCs, n =>
        {
            Assert.True(n.CanMove);
            Assert.Equal((18, 18), (n.SpawnX, n.SpawnY));
            Assert.InRange(n.MapX, Rect[0], Rect[0] + Rect[2] - 1);
        });
    }

    [Fact]
    public void Invaders_without_a_center_wander_around_where_they_spawned()
    {
        var (kit, script, map, _) = Setup();
        On(kit);

        HalloweenTestKit.Call(script.Object, "Tick", kit.World, Now, Rng(WaveRng));

        Assert.All(map.NPCs, n =>
        {
            Assert.True(n.CanMove);
            Assert.Equal((n.MapX, n.MapY), (n.SpawnX, n.SpawnY));
        });
    }

    [Fact]
    public void Next_wave_despawns_survivors()
    {
        var (kit, script, map, listener) = Setup();
        On(kit);

        HalloweenTestKit.Call(script.Object, "Tick", kit.World, Now, Rng(WaveRng));
        var firstWave = map.NPCs.ToList();
        Assert.Equal(5, firstWave.Count);

        long nextWave = Now + kit.World.TimerFrequency * 60 * 30;
        HalloweenTestKit.Call(script.Object, "Tick", kit.World, nextWave, Rng(WaveRng));

        Assert.Equal(5, map.NPCs.Count);
        Assert.DoesNotContain(map.NPCs, n => firstWave.Contains(n));
        Assert.All(map.NPCs, n =>
            Assert.True(n.Properties.TryGetProperty<long>("spawnedAt", out long t) && t == nextWave));
        Assert.Equal(2, listener.Sent.Count(s => s.Contains("The party has spilled into Grove!")));
    }

    [Fact]
    public void Next_wave_comes_after_a_random_gap_between_min_and_max()
    {
        var (kit, script, map, listener) = Setup();
        kit.SetConfig(cfg =>
        {
            cfg["invasion"]!["everyMinMinutes"] = 120;
            cfg["invasion"]!["everyMaxMinutes"] = 240;
        });
        On(kit);

        HalloweenTestKit.Call(script.Object, "Tick", kit.World, Now, new HalloweenTestKit.FixedRandom([0.5], WaveRng));

        long minute = kit.World.TimerFrequency * 60;
        HalloweenTestKit.Call(script.Object, "Tick", kit.World, Now + 179 * minute, Rng(WaveRng));
        Assert.Equal(1, listener.Sent.Count(s => s.Contains("The party has spilled into Grove!")));

        HalloweenTestKit.Call(script.Object, "Tick", kit.World, Now + 180 * minute, Rng(WaveRng));
        Assert.Equal(2, listener.Sent.Count(s => s.Contains("The party has spilled into Grove!")));
    }

    [Fact]
    public void Survivors_despawn_after_lifetime()
    {
        var (kit, script, map, listener) = Setup();
        On(kit);

        HalloweenTestKit.Call(script.Object, "Tick", kit.World, Now, Rng(WaveRng));
        Assert.Equal(5, map.NPCs.Count);

        long expiry = Now + kit.World.TimerFrequency * 60 * 15;
        HalloweenTestKit.Call(script.Object, "Tick", kit.World, expiry, Rng([0, 0, 0, 0]));

        Assert.Empty(map.NPCs);
        Assert.Equal(1, listener.Sent.Count(s => s.Contains("The party has spilled into Grove!")));
    }

    [Fact]
    public void Non_positive_town_rect_dimension_disables_config()
    {
        var kit = new HalloweenTestKit();
        kit.SetConfig(cfg =>
        {
            cfg["invasion"]!["towns"] = new JsonArray(new JsonObject
            {
                ["map"] = TownMapId,
                ["rect"] = new JsonArray(1, 1, -10, 10),
            });
        });
        using var log = new CapturingLog();

        Assert.Equal(0, InvasionValue(kit, "Count"));
        Assert.Equal(0, InvasionValue(kit, "Count"));
        Assert.Equal(1, log.Messages.Count(m => m.Contains("halloween.json")));
    }

    private static (HalloweenTestKit Kit, Script<IGlobalScript> Script, Map Map, TestWorldFixture.CapturingPlayer Listener) Setup()
    {
        var kit = new HalloweenTestKit();
        var map = kit.Fixture.AddBaseMap(TownMapId, "Grove", 40, 40);
        var listener = kit.Fixture.CommandPlayerOn(map, 1, 1);
        kit.Fixture.AddOnlinePlayer(listener);
        kit.SetConfig(cfg =>
        {
            cfg["invasion"] = new JsonObject
            {
                ["towns"] = new JsonArray(new JsonObject
                {
                    ["map"] = TownMapId,
                    ["rect"] = new JsonArray(Rect[0], Rect[1], Rect[2], Rect[3]),
                }),
                ["templates"] = new JsonArray(kit.WailingGhostId),
                ["count"] = 5,
                ["everyMinMinutes"] = 30,
                ["everyMaxMinutes"] = 30,
                ["lifetimeMinutes"] = 15,
            };
        });
        var script = kit.Compile<IGlobalScript>("Global", "HalloweenInvasion.csx");
        return (kit, script, map, listener);
    }

    private static void On(HalloweenTestKit kit)
    {
        kit.World.CommunityGoals.SetFlag("halloween.active", 1, null);
        kit.World.CommunityGoals.SetFlag("halloween.tier3", 1, null);
    }

    private static Random Rng(int[] ints) => new HalloweenTestKit.FixedRandom([], ints);

    private static int InvasionValue(HalloweenTestKit kit, string name)
    {
        var config = (object)HalloweenTestKit.Call(kit.Host, "Config", kit.World)!;
        var group = (object)config.GetType().GetProperty("Invasion")!.GetValue(config)!;
        return (int)group.GetType().GetProperty(name)!.GetValue(group)!;
    }
}
