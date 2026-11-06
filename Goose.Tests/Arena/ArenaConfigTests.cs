using Goose;
using Goose.Scripting;
using Xunit;

namespace Goose.Tests.Arena;

[Collection("NLog")]
public sealed class ArenaConfigTests : IDisposable
{
    private readonly ArenaTestKit kit = new();

    public void Dispose() => kit.Dispose();

    private static object Config(ArenaTestKit kit) => (object)ArenaTestKit.Call(kit.Host, "Config", kit.World)!;

    private static bool Enabled(ArenaTestKit kit) => (bool)ArenaTestKit.Call(kit.Host, "Enabled", kit.World)!;

    private static T Prop<T>(object config, string name) => (T)config.GetType().GetProperty(name)!.GetValue(config)!;

    [Fact]
    public void Shipped_config_parses_and_is_disabled_until_maps_are_set()
    {
        var shipped = Path.Combine(ArenaTestKit.ShippedDataDirectory, "Scripts", "Arena", "arena.json");
        File.Copy(shipped, kit.ConfigPath, true);
        File.SetLastWriteTimeUtc(kit.ConfigPath, File.GetLastWriteTimeUtc(kit.ConfigPath).AddSeconds(1));

        var config = Config(kit);
        Assert.True(Prop<bool>(config, "Valid"));
        Assert.Equal(0, Prop<int>(config, "LobbyMap"));
        Assert.Equal(0, Prop<int>(config, "ArenaMap"));
        var bands = (System.Collections.IList)Prop<object>(config, "Bands")!;
        Assert.Single(bands);
        Assert.False(Enabled(kit));
    }

    [Fact]
    public void Valid_test_config_is_enabled()
    {
        Assert.True(Enabled(kit));
    }

    [Fact]
    public void Invalid_exponent_disables_and_logs_once()
    {
        kit.SetConfig(json => json["a"] = 3.5);
        using var log = new CapturingLog();
        var first = Config(kit);
        var second = Config(kit);
        Assert.False(Prop<bool>(first, "Valid"));
        Assert.False(Prop<bool>(second, "Valid"));
        Assert.False(Enabled(kit));
        Assert.Single(log.Messages.Where(m => m.Contains("arena", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void Malformed_anchor_utc_disables()
    {
        kit.SetConfig(json => json["anchorUtc"] = "13.30");
        var config = Config(kit);
        Assert.False(Prop<bool>(config, "Valid"));
        Assert.False(Enabled(kit));
    }

    [Fact]
    public void Zero_boss_every_disables()
    {
        kit.SetConfig(json => json["bossEvery"] = 0);
        var config = Config(kit);
        Assert.False(Prop<bool>(config, "Valid"));
        Assert.False(Enabled(kit));
    }

    [Fact]
    public void Zero_anchor_mob_hp_disables()
    {
        kit.SetConfig(json => json["anchorMobHp"] = 0);
        var config = Config(kit);
        Assert.False(Prop<bool>(config, "Valid"));
        Assert.False(Enabled(kit));
    }

    [Fact]
    public void Editing_the_file_reloads_without_script_reload()
    {
        Assert.Equal(10, Prop<int>(Config(kit), "CountdownSeconds"));
        kit.SetConfig(json => json["countdownSeconds"] = 25);
        Assert.Equal(25, Prop<int>(Config(kit), "CountdownSeconds"));
    }

    [Fact]
    public void Shim_compiles_and_loads_controller()
    {
        var script = kit.World.ScriptHandler.GetScript<IGlobalScript>("Scripts/Global/Arena.csx");
        Assert.Equal("ArenaController", script.Object.GetType().Name);
    }
}
