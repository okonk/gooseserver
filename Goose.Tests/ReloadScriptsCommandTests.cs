using Goose.Commands;
using Goose.Scripting;
using Goose.Testing;
using Xunit;

namespace Goose.Tests;

public class ReloadScriptsCommandTests : IDisposable
{
    private readonly TestWorldFixture fixture = new();
    private readonly TestWorldFixture.CapturingPlayer player;

    public ReloadScriptsCommandTests()
    {
        var map = fixture.AddBaseMap(1, "Town");
        player = fixture.CommandPlayerOn(map, 5, 5, "Gm");
    }

    public void Dispose() => fixture.Dispose();

    private static string VersionScript(string className, int version) => $@"
public class {className} : BaseGlobalScript
{{
    public int Version() => {version};
}}

return typeof({className});
";

    private void Write(string relative, string body) =>
        File.WriteAllText(Path.Combine(fixture.DataDirectory, relative), body);

    private static int Version(Script<IGlobalScript> script) =>
        (int)script.Object.GetType().GetMethod("Version")!.Invoke(script.Object, null)!;

    private void ReloadAndAwaitCompletion()
    {
        new ReloadScriptsCommand().Execute(new CommandContext(player, fixture.World, new CommandRegistry(), [], ""));

        var deadline = DateTime.UtcNow.AddSeconds(60);
        while (fixture.World.PendingCompletionCount == 0)
        {
            Assert.True(DateTime.UtcNow < deadline, "reload never enqueued its completion");
            Thread.Sleep(20);
        }
    }

    [Fact]
    public void Reload_SwapsScriptsOnlyOnTheGameThread()
    {
        Write("Scripts/Global/A.csx", VersionScript("A", 1));
        var script = fixture.World.ScriptHandler.GetScript<IGlobalScript>("Scripts/Global/A.csx");
        Write("Scripts/Global/A.csx", VersionScript("A", 2));

        ReloadAndAwaitCompletion();
        Assert.Equal(1, Version(script));

        fixture.World.Update();

        Assert.Equal(2, Version(script));
        Assert.Contains(player.Sent, s => s.Contains("Reloaded scripts."));
    }

    [Fact]
    public void Reload_CompileFailureLeavesEveryScriptUnchanged()
    {
        Write("Scripts/Global/A.csx", VersionScript("A", 1));
        Write("Scripts/Global/B.csx", VersionScript("B", 1));
        var a = fixture.World.ScriptHandler.GetScript<IGlobalScript>("Scripts/Global/A.csx");
        var b = fixture.World.ScriptHandler.GetScript<IGlobalScript>("Scripts/Global/B.csx");
        Write("Scripts/Global/A.csx", VersionScript("A", 2));
        Write("Scripts/Global/B.csx", "this does not compile");

        ReloadAndAwaitCompletion();
        fixture.World.Update();

        Assert.Equal(1, Version(a));
        Assert.Equal(1, Version(b));
        Assert.Contains(player.Sent, s => s.Contains("Failed loading"));
    }

    [Fact]
    public void Reload_RunsNewGlobalScriptOnLoadedOnTheGameThread()
    {
        Write("Scripts/Global/New.csx", @"
public class New : BaseGlobalScript
{
    public override void OnLoaded(GameWorld world)
    {
        world.Settings.ServerName = System.Environment.CurrentManagedThreadId.ToString();
    }
}

return typeof(New);
");
        fixture.World.Settings.ServerName = "unset";

        ReloadAndAwaitCompletion();
        Assert.Equal("unset", fixture.World.Settings.ServerName);

        fixture.World.Update();

        Assert.Equal(Environment.CurrentManagedThreadId.ToString(), fixture.World.Settings.ServerName);
    }

    [Fact]
    public void Reload_DoesNotRerunOnLoadedForAlreadyLoadedGlobalScripts()
    {
        Write("Scripts/Global/Once.csx", @"
public class Once : BaseGlobalScript
{
    public override void OnLoaded(GameWorld world)
    {
        world.Settings.ServerName += ""x"";
    }
}

return typeof(Once);
");
        fixture.World.Settings.ServerName = "";
        fixture.World.LoadGlobalScripts();

        ReloadAndAwaitCompletion();
        fixture.World.Update();

        Assert.Equal("x", fixture.World.Settings.ServerName);
    }
}
