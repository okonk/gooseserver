using Goose.Scripting;
using Xunit;

namespace Goose.Tests;

/// <summary>Regression test for #load support in the Script engine: pins the #load shapes
/// the dimensions cleanup relies on, through the real ScriptHandler/Script path.
/// Shapes covered:
///   1. #load must be the first tokens in the file (before usings) - CS8098 otherwise.
///   2. sibling form:      #load "LoadSibling.csx"
///   3. subdirectory form: #load "Dimensions/LoadConstants.csx"
///   4. parent form:       #load "../Global/Dimensions/LoadConstants.csx"
///   5. multiple #loads in one host
///   6. one loaded file referencing declarations of ANOTHER loaded file (part -> constants)
///   7. partial class assembled across a loaded file and the host
///   8. per-host independence: each host compilation gets its own copy of a mutable static</summary>
[Collection(Goose.Tests.Collections.GameWorldSettingsCollection.Name)]
public class ScriptLoadDirectiveTests : IDisposable
{
    private readonly GooseSettings settings;
    private readonly string dir;
    private readonly GameWorld world;

    public ScriptLoadDirectiveTests()
    {
        dir = Path.Combine(Path.GetTempPath(), "script-load-" + Guid.NewGuid().ToString("N"));
        foreach (var d in new[] { "Scripts/Global/Dimensions", "Scripts/Item" })
            Directory.CreateDirectory(Path.Combine(dir, d));

        settings = new GooseSettings
        {
            DataPath = dir, ExperienceModifier = 1,
            InventorySize = 30, EquippedSize = 20, CombineBagSize = 10, SpellbookSize = 30,
            VendorSlotSize = 30, MaxPlayers = 200, MaxNPCs = 15000,
        };
        world = new GameWorld(settings);
    }

    public void Dispose()
    {
        if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
    }

    private void WriteFile(string relative, string body)
    {
        File.WriteAllText(Path.Combine(dir, relative), body);
    }

    private static int Check(object scriptObject)
    {
        return (int)scriptObject.GetType().GetMethod("Check").Invoke(scriptObject, null);
    }

    [Fact]
    public void LoadShapes_CompilesAndResolves()
    {
        // Declarations only: usings, const, mutable static (independence probe),
        // static method.
        WriteFile("Scripts/Global/Dimensions/LoadConstants.csx", @"
using System;
public static class LoadConstants
{
    public const int Offset = 12345;
    public static int Mut = 42;
    public static int DimOf(int id) { return id / Offset; }
}
");

        // A second loaded file that references LoadConstants from the FIRST loaded
        // file (part -> constants), plus one half of a partial class.
        WriteFile("Scripts/Global/Dimensions/LoadPart.csx", @"
using System;
public static class LoadPart
{
    public static int UsesOtherLoadedFile() { return LoadConstants.DimOf(LoadConstants.Offset); }
}
public partial class LoadHost
{
    public static int FromPart() { return LoadConstants.Offset; }
}
");

        // Sibling in the same directory as the host (no folder component).
        WriteFile("Scripts/Global/LoadSibling.csx", @"
public static class LoadSibling
{
    public const int Sibling = 7;
}
");

        // Host in Scripts/Global with three #loads: a sibling file and two subdirectory
        // files. The other half of the partial class lives here.
        WriteFile("Scripts/Global/LoadA.csx", @"
#load ""LoadSibling.csx""
#load ""Dimensions/LoadConstants.csx""
#load ""Dimensions/LoadPart.csx""
using System;

public partial class LoadHost
{
    public int Half() { return 1; }
}

public class LoadA : BaseGlobalScript
{
    public static int Check()
    {
        LoadConstants.Mut = 1;
        return LoadConstants.Offset
             + LoadConstants.DimOf(LoadConstants.Offset * 3)
             + LoadPart.UsesOtherLoadedFile()
             + LoadHost.FromPart()
             + LoadSibling.Sibling;
    }
}

return typeof(LoadA);
");

        // Second host, different directory, loading the same shared file through the
        // parent-relative form.
        WriteFile("Scripts/Item/LoadB.csx", @"
#load ""../Global/Dimensions/LoadConstants.csx""
using System;

public class LoadB : BaseGlobalScript
{
    public static int Check()
    {
        return LoadConstants.Offset * 1000 + LoadConstants.Mut;
    }
}

return typeof(LoadB);
");

        var a = world.ScriptHandler.GetScript<IGlobalScript>("Scripts/Global/LoadA.csx").Object;
        var b = world.ScriptHandler.GetScript<IGlobalScript>("Scripts/Item/LoadB.csx").Object;

        // 12345 (const) + 3 (DimOf) + 1 (part -> other loaded file) + 12345 (partial half
        // in a loaded file, called from the host) + 7 (sibling)
        Assert.Equal(12345 + 3 + 1 + 12345 + 7, Check(a));

        // Per-host independence: A set Mut to 1 in ITS copy; B's copy must still be 42.
        // If the loaded file were shared across compilations this would read 1.
        Assert.Equal(12345000 + 42, Check(b));
    }
}
