using System.Reflection;
using Goose.Scripting;

namespace Goose.Tests.Fixtures;

public sealed class GlobalScriptFixture : IDisposable
{
    private readonly GooseSettings previousSettings = GameWorld.Settings;

    /// <summary>Every dimension script, by the relative path the server resolves it at.
    /// Copied to output by Goose.Tests.csproj. Add to BOTH lists together - a script
    /// missing here fails inside OnLoaded, not at compile time.</summary>
    ///
    /// <remarks>This starts with only Dimensions.csx and DimensionMap.csx.
    /// DimensionUnlock.csx is added in Task 7, when the
    /// <c>&lt;None Include&gt;</c> entry that copies it to the test output first exists
    /// (Dimensions.csx loads it at run time, so listing a script before its file exists
    /// would make InstallShippedScripts throw FileNotFoundException for every test).</remarks>
    private static readonly (string Source, string Relative)[] ShippedScripts =
    {
        ("Dimensions.csx",      "Scripts/Global/Dimensions.csx"),
        ("DimensionMap.csx",    "Scripts/Map/DimensionMap.csx"),
    };

    public string DataDirectory { get; }
    public GameWorld World { get; }

    public GlobalScriptFixture()
    {
        DataDirectory = Path.Combine(Path.GetTempPath(), "global-script-" + Guid.NewGuid().ToString("N"));
        foreach (var dir in new[] { "Global", "Map", "Quest" })
            Directory.CreateDirectory(Path.Combine(DataDirectory, "Scripts", dir));

        GameWorld.Settings = new GooseSettings
        {
            DataPath = DataDirectory, ExperienceModifier = 1,
            InventorySize = 30, EquippedSize = 20, CombineBagSize = 10, SpellbookSize = 30,
            // NPC spawns need a login-id range: GetNewID draws from (MaxPlayers, MaxNPCs]
            // (NPCHandler.cs:244). Same values NPCSpawnRegistrationTests uses.
            MaxPlayers = 200, MaxNPCs = 15000,
        };
        World = new GameWorld(null);

        // Seed classes so NPC spawning works (see ORCHESTRATION NOTE 2).
        SeedClass(0, "Default", 50);
        SeedClass(3, "Warrior", 50);
    }

    /// <summary>Installs every shipped dimension script into the temp data dir. Call this
    /// before compiling anything - Dimensions.csx loads the map and quest scripts while it
    /// runs, so a partial install fails at OnLoaded rather than at compile time.</summary>
    public void InstallShippedScripts()
    {
        foreach (var (source, relative) in ShippedScripts)
        {
            var from = Path.Combine(AppContext.BaseDirectory, "DimensionScripts", source);
            if (!File.Exists(from))
                throw new FileNotFoundException(
                    $"{source} is not in the test output. Add its <None Include> to Goose.Tests.csproj.", from);

            File.Copy(from, Path.Combine(DataDirectory, relative), overwrite: true);
        }
    }

    /// <summary>Compiles the real shipped Dimensions.csx, so tests exercise what ships
    /// rather than a paraphrase of it.</summary>
    public Script<IGlobalScript> CompileShipped(string fileName = "Dimensions.csx")
    {
        InstallShippedScripts();
        return World.ScriptHandler.GetScript<IGlobalScript>("Scripts/Global/" + fileName);
    }

    /// <summary>As CompileShipped, for the map script - Task 5's tests drive it directly.</summary>
    public Script<IMapScript> CompileShippedMapScript(string fileName = "DimensionMap.csx")
    {
        InstallShippedScripts();
        return World.ScriptHandler.GetScript<IMapScript>("Scripts/Map/" + fileName);
    }

    /// <summary>Compiles an arbitrary script body, for the one test that needs a variant of
    /// the shipped script (the disabled-mode test).</summary>
    public Script<IGlobalScript> CompileSource(string body, string fileName)
    {
        InstallShippedScripts();
        var relativePath = "Scripts/Global/" + fileName;
        File.WriteAllText(Path.Combine(DataDirectory, relativePath), body);
        return World.ScriptHandler.GetScript<IGlobalScript>(relativePath);
    }

    /// <summary>A base map with hand-built tile arrays. Real maps get theirs from
    /// Map.LoadData reading a .map file; clones never call it, so a synthetic base is
    /// enough to exercise the clone path.</summary>
    public Map AddBaseMap(int id, string name, int width = 10, int height = 10)
    {
        var map = new Map
        {
            ID = id, Name = name, FileName = "Map" + id + ".map",
            Width = width, Height = height,
            tiles = new ITile[(width + 1) * (height + 1)],
            characters = new ICharacter[(width + 1) * (height + 1)],
        };
        World.MapHandler.Maps[id] = map;
        return map;
    }

    /// <summary>Registers a class with levels 1..maxLevel directly into ClassHandler's private
    /// dictionary. ClassHandler has no public registration path (classes come from the DB), so
    /// this mirrors NPCSpawnRegistrationTests.RegisterClass.</summary>
    public void SeedClass(int classId, string name, int maxLevel)
    {
        var cls = new Class { ClassID = classId, ClassName = name, ACMultiplier = 1m };
        for (int level = 1; level <= maxLevel; level++)
            cls.AddLevel(new ClassLevel { Level = level, BaseStats = new AttributeSet() });

        var classes = (Dictionary<int, Class>)typeof(ClassHandler)
            .GetField("classes", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(World.ClassHandler)!;
        classes[classId] = cls;
    }

    /// <summary>Removes one level row from a seeded class. Needed by the warden-class validation
    /// test (Task 7): the warden uses class 3 at level 50, and that test must be able to take
    /// the level-50 row away to prove the script rejects the misconfiguration up front.</summary>
    public void RemoveClassLevel(int classId, int level)
    {
        var cls = World.ClassHandler.GetClass(classId);
        if (cls == null) return;

        var levels = (Dictionary<int, ClassLevel>)typeof(Class)
            .GetField("levels", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(cls)!;
        levels.Remove(level);
    }

    public void Dispose()
    {
        GameWorld.Settings = previousSettings;
        if (Directory.Exists(DataDirectory)) Directory.Delete(DataDirectory, recursive: true);
    }
}
