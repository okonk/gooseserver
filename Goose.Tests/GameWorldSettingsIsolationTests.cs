using Goose.Scripting;
using Xunit;

namespace Goose.Tests;

[Collection(Goose.Tests.Collections.GameWorldSettingsCollection.Name)]
public class GameWorldSettingsIsolationTests : IDisposable
{
    private const string RelativePath = "Scripts/Global/Sample.csx";

    private readonly GooseSettings previousSettings = GameWorld.Settings;
    private readonly List<string> roots = new();
    private readonly string rootA;
    private readonly string rootB;
    private readonly GooseSettings settingsA;
    private readonly GooseSettings settingsB;
    private readonly GameWorld worldA;
    private readonly GameWorld worldB;

    public GameWorldSettingsIsolationTests()
    {
        rootA = MakeRoot("iso-a");
        rootB = MakeRoot("iso-b");

        settingsA = new GooseSettings { DataPath = rootA, ExperienceModifier = 2.5m };
        settingsB = new GooseSettings { DataPath = rootB, ExperienceModifier = 3.5m };

        WriteSample(rootA, "Alpha", 11, "Sample.csx");
        WriteSample(rootB, "Beta", 22, "Sample.csx");

        worldA = new GameWorld(settingsA);
        worldB = new GameWorld(settingsB);
    }

    public void Dispose()
    {
        GameWorld.Settings = previousSettings;
        foreach (var root in roots)
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
    }

    private string MakeRoot(string prefix)
    {
        var root = Path.Combine(Path.GetTempPath(), prefix + "-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "Scripts", "Global"));
        roots.Add(root);
        return root;
    }

    private static void WriteSample(string root, string className, int value, string fileName)
    {
        File.WriteAllText(Path.Combine(root, "Scripts", "Global", fileName), $@"
public class {className} : BaseGlobalScript
{{
    public static int Check() {{ return {value}; }}
}}

return typeof({className});
");
    }

    private static int Check(object scriptObject)
    {
        return (int)scriptObject.GetType().GetMethod("Check").Invoke(scriptObject, null);
    }

    [Fact]
    public void WorldsRetainTheirOwnSettingsReferenceAndExperienceModifier()
    {
        Assert.Same(settingsA, worldA.Configuration);
        Assert.Same(settingsB, worldB.Configuration);
        Assert.NotSame(worldA.Configuration, worldB.Configuration);
        Assert.Equal(2.5m, worldA.ExperienceModifier);
        Assert.Equal(3.5m, worldB.ExperienceModifier);
    }

    [Fact]
    public void WorldsResolveIdenticalRelativePathsAgainstTheirOwnDataRoot()
    {
        var scriptA = worldA.ScriptHandler.GetScript<IGlobalScript>(RelativePath);
        var scriptB = worldB.ScriptHandler.GetScript<IGlobalScript>(RelativePath);

        Assert.Equal(11, Check(scriptA.Object));
        Assert.Equal(22, Check(scriptB.Object));
        Assert.StartsWith(rootA, scriptA.FilePath, StringComparison.Ordinal);
        Assert.StartsWith(rootB, scriptB.FilePath, StringComparison.Ordinal);
    }

    [Fact]
    public void RepeatedRelativePathsHitThePerWorldCache()
    {
        var first = worldA.ScriptHandler.GetScript<IGlobalScript>(RelativePath);
        var second = worldA.ScriptHandler.GetScript<IGlobalScript>(RelativePath);

        Assert.Same(first, second);
        Assert.NotSame(first, worldB.ScriptHandler.GetScript<IGlobalScript>(RelativePath));
    }

    [Fact]
    public void ChangingTheStaticSettingsAfterConstructionDoesNotRedirectWorlds()
    {
        worldA.ScriptHandler.GetScript<IGlobalScript>(RelativePath);
        worldB.ScriptHandler.GetScript<IGlobalScript>(RelativePath);

        GameWorld.Settings = new GooseSettings
        {
            DataPath = MakeRoot("iso-hijack"), ExperienceModifier = 99m,
        };
        try
        {
            Assert.Equal(11, Check(worldA.ScriptHandler.GetScript<IGlobalScript>(RelativePath).Object));
            Assert.Equal(22, Check(worldB.ScriptHandler.GetScript<IGlobalScript>(RelativePath).Object));
            Assert.Equal(2.5m, worldA.ExperienceModifier);
            Assert.Equal(3.5m, worldB.ExperienceModifier);
        }
        finally
        {
            GameWorld.Settings = previousSettings;
        }
    }

    [Fact]
    public void FailedScriptLoadDoesNotPublishACacheEntry()
    {
        const string missing = "Scripts/Global/Missing.csx";

        Assert.Throws<FileNotFoundException>(() => worldB.ScriptHandler.GetScript<IGlobalScript>(missing));

        WriteSample(rootB, "Gamma", 33, "Missing.csx");
        Assert.Equal(33, Check(worldB.ScriptHandler.GetScript<IGlobalScript>(missing).Object));
    }

    [Fact]
    public void GameServerRestartSeam_ConstructsWorldFromItsOwnSettings()
    {
        var settings = new GooseSettings { DataPath = MakeRoot("iso-server"), ExperienceModifier = 4.5m };
        var server = new GameServer(settings);

        var world = server.CreateWorld();

        Assert.Same(settings, world.Configuration);
        Assert.Same(server, world.GameServer);
        Assert.Equal(4.5m, world.ExperienceModifier);
    }
}
