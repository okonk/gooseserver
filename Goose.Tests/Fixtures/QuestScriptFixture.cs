using Goose.Scripting;

namespace Goose.Tests.Fixtures;

public sealed class QuestScriptFixture : IDisposable
{
    private readonly GooseSettings previousSettings = GameWorld.Settings;

    public string DataDirectory { get; }
    public GameWorld World { get; }

    public QuestScriptFixture()
    {
        DataDirectory = Path.Combine(Path.GetTempPath(), "quest-script-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(DataDirectory, "Scripts", "Quest"));
        GameWorld.Settings = new GooseSettings
        {
            DataPath = DataDirectory, ExperienceModifier = 1,
            InventorySize = 30, EquippedSize = 20, CombineBagSize = 10, SpellbookSize = 30,
        };
        World = new GameWorld(null);
    }

    public Script<IQuestScript> Compile(string body, string fileName = "T.csx")
    {
        var relativePath = "Scripts/Quest/" + fileName;
        File.WriteAllText(Path.Combine(DataDirectory, relativePath), body);
        return World.ScriptHandler.GetScript<IQuestScript>(relativePath);
    }

    public void Dispose()
    {
        GameWorld.Settings = previousSettings;
        if (Directory.Exists(DataDirectory)) Directory.Delete(DataDirectory, recursive: true);
    }
}
