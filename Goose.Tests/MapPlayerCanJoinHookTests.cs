using Goose;
using Goose.Scripting;
using Goose.Tests.Collections;

namespace Goose.Tests;

[Collection(GameWorldSettingsCollection.Name)]
public class MapPlayerCanJoinHookTests : IDisposable
{
    private sealed class RefusingMapScript : BaseMapScript
    {
        public override string CanPlayerJoin(Map map, Player player, GameWorld world) => "denied";
    }

    private sealed class ThrowingMapScript : BaseMapScript
    {
        public override string CanPlayerJoin(Map map, Player player, GameWorld world)
            => throw new InvalidOperationException("boom");
    }

    /// <summary>Player.Send is virtual so a subclass can capture what the server would
    /// have written to the socket. world.Send routes through Player.Send.</summary>
    private sealed class CapturingPlayer : Player
    {
        public List<string> Sent { get; } = new();

        public override bool Send(string data) { Sent.Add(data); return true; }
    }

    private readonly GooseSettings previousSettings = GameWorld.Settings;
    private readonly string dataDirectory;
    private readonly GameWorld world;

    public MapPlayerCanJoinHookTests()
    {
        // Same shape as QuestScriptFixture: a throwaway data dir + settings so
        // Script<IMapScript> can compile a real .csx through the ScriptHandler.
        dataDirectory = Path.Combine(Path.GetTempPath(), "map-script-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(dataDirectory, "Scripts", "Map"));
        GameWorld.Settings = new GooseSettings
        {
            DataPath = dataDirectory, ExperienceModifier = 1,
            InventorySize = 30, EquippedSize = 20, CombineBagSize = 10, SpellbookSize = 30,
        };
        world = new GameWorld(null);
    }

    public void Dispose()
    {
        GameWorld.Settings = previousSettings;
        if (Directory.Exists(dataDirectory)) Directory.Delete(dataDirectory, recursive: true);
    }

    [Fact]
    public void A_map_with_no_script_still_allows_entry()
    {
        // Script is null on most maps; the ?. must not turn into a refusal.
        Assert.True(MapWith(script: null).PlayerCanJoin(OrdinaryPlayer(), world));
    }

    [Fact]
    public void A_refusing_script_stops_entry_and_the_player_is_told_why()
    {
        var player = OrdinaryPlayer();

        Assert.False(MapWith(new RefusingMapScript()).PlayerCanJoin(player, world));
        Assert.Contains("denied", SentTo(player));
    }

    [Fact]
    public void A_GM_bypasses_the_script_gate()
    {
        // The privilege check is before the hook, so the script never runs.
        var gm = PlayerWith(AccessPrivilege.IgnoreMapRequirements);

        Assert.True(MapWith(new RefusingMapScript()).PlayerCanJoin(gm, world));
    }

    /// <summary>Fail closed. A gate script that throws must not admit the player.</summary>
    [Fact]
    public void A_throwing_script_refuses_entry()
    {
        var player = OrdinaryPlayer();

        Assert.False(MapWith(new ThrowingMapScript()).PlayerCanJoin(player, world));
        Assert.NotEmpty(SentTo(player));
    }

    /// <summary>Wraps a BaseMapScript in whatever shape Map.Script expects. Script&lt;T&gt;
    /// compiles from a file path, so the real path is exercised by writing a one-line .csx
    /// (mirroring the passed script) into the fixture's Scripts/Map directory and compiling
    /// it through the ScriptHandler.</summary>
    private Map MapWith(BaseMapScript script)
    {
        if (script == null) return new Map { Name = "test", Script = null };

        var (fileName, body) = script switch
        {
            RefusingMapScript => ("Refusing.csx", RefusingBody),
            ThrowingMapScript => ("Throwing.csx", ThrowingBody),
            _ => throw new ArgumentOutOfRangeException(nameof(script)),
        };
        File.WriteAllText(Path.Combine(dataDirectory, "Scripts", "Map", fileName), body);
        return new Map { Name = "test", Script = world.ScriptHandler.GetScript<IMapScript>("Scripts/Map/" + fileName) };
    }

    private const string RefusingBody = """
        using Goose;
        using Goose.Scripting;

        public class T : BaseMapScript
        {
            public override string CanPlayerJoin(Map map, Player player, GameWorld world) => "denied";
        }

        return typeof(T);
        """;

    private const string ThrowingBody = """
        using Goose;
        using Goose.Scripting;

        public class T : BaseMapScript
        {
            public override string CanPlayerJoin(Map map, Player player, GameWorld world)
                => throw new InvalidOperationException("boom");
        }

        return typeof(T);
        """;

    private static CapturingPlayer OrdinaryPlayer() => new() { Access = Player.AccessStatus.Normal };

    private static CapturingPlayer PlayerWith(AccessPrivilege privilege)
    {
        // GameMaster access is granted every privilege (AccessLevels.cs), which includes
        // IgnoreMapRequirements - the bypass PlayerCanJoin checks before the hook.
        var player = new CapturingPlayer { Access = Player.AccessStatus.GameMaster };
        Assert.True(player.HasPrivilege(privilege));
        return player;
    }

    private static string SentTo(Player player) => string.Join("", ((CapturingPlayer)player).Sent);
}
