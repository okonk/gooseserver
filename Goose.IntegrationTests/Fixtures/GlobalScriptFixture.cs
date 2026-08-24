using System.Reflection;
using Goose.Scripting;
using Goose.Testing;

namespace Goose.IntegrationTests.Fixtures;

public sealed class GlobalScriptFixture : TestWorldFixture
{
    /// <summary>Every dimension script, by the relative path the server resolves it at.
    /// Copied to output by Goose.IntegrationTests.csproj. Add to BOTH lists together - a
    /// script missing here fails inside OnLoaded, not at compile time.</summary>
    ///
    /// <remarks>All eight dimension scripts ship: the global orchestration, the map entry
    /// gate, the quest reward that grants the unlocked dimension, the spell that
    /// teleports the player between dimensions, the item scripts that roll abyss
    /// suffixes and rarity titles onto dimension equipment, and the rebirth script that
    /// trades banked experience for spirit. The seven entry scripts all live in one
    /// folder, Scripts/Global/Dimensions/, plus the six part files and three shared
    /// files that Dimensions.csx #loads; the entry orchestration stays in Scripts/Global/.</remarks>
    private static readonly (string Source, string Relative)[] ShippedScripts =
    {
        ("Dimensions.csx",           "Scripts/Global/Dimensions.csx"),
        ("Npcs.csx",                 "Scripts/Global/Dimensions/Npcs.csx"),
        ("Maps.csx",                 "Scripts/Global/Dimensions/Maps.csx"),
        ("Items.csx",                "Scripts/Global/Dimensions/Items.csx"),
        ("Spells.csx",               "Scripts/Global/Dimensions/Spells.csx"),
        ("Commands.csx",             "Scripts/Global/Dimensions/Commands.csx"),
        ("SpiritCurrency.csx",       "Scripts/Global/Dimensions/SpiritCurrency.csx"),
        ("DimensionConstants.csx",   "Scripts/Global/Dimensions/DimensionConstants.csx"),
        ("DimensionHelpers.csx",     "Scripts/Global/Dimensions/DimensionHelpers.csx"),
        ("DimensionRolls.csx",       "Scripts/Global/Dimensions/DimensionRolls.csx"),
        ("DimensionMap.csx",         "Scripts/Global/Dimensions/DimensionMap.csx"),
        ("DimensionUnlock.csx",      "Scripts/Global/Dimensions/DimensionUnlock.csx"),
        ("DimensionTeleport.csx",    "Scripts/Global/Dimensions/DimensionTeleport.csx"),
        ("DimensionItem.csx",        "Scripts/Global/Dimensions/DimensionItem.csx"),
        ("DimensionSurname.csx",     "Scripts/Global/Dimensions/DimensionSurname.csx"),
        ("DimensionRarity.csx",      "Scripts/Global/Dimensions/DimensionRarity.csx"),
        ("Rebirth.csx",              "Scripts/Global/Dimensions/Rebirth.csx"),
    };

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
                    $"{source} is not in the test output. Add its <None Include> to Goose.IntegrationTests.csproj.", from);

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
        return World.ScriptHandler.GetScript<IMapScript>("Scripts/Global/Dimensions/" + fileName);
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
}
