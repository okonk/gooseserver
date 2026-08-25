using Goose;
using Goose.Quests;
using Goose.Scripting;
using Goose.Tests.Collections;
using Goose.Tests.Fixtures;

namespace Goose.Tests;

[Collection(GameWorldSettingsCollection.Name)]
public class QuestScriptTests
{
    [Fact]
    public void A_script_can_name_the_quest_types_it_is_handed()
    {
        using var fixture = new QuestScriptFixture();
        // Red before Task 1: Quest/QuestRequirement/RequirementType are internal, so Roslyn
        // reports CS0122 and Script<T>.LoadScript throws out of fixture.Compile().
        var script = fixture.Compile(@"
using Goose; using Goose.Quests; using Goose.Scripting;
public class T : BaseQuestScript
{
    public override bool IsMet(QuestRequirement requirement, Player player, GameWorld world)
        => requirement.Type == RequirementType.Script && requirement.Quest != null;
}
return typeof(T);
");
        var quest = new Quest { Id = 1 };
        var req = new QuestRequirement { Type = RequirementType.Script, Quest = quest };

        // Player(0), not Player() — the parameterless ctor leaves collections null (Player.cs:465).
        Assert.True(script.Object.IsMet(req, new Player(0), fixture.World));
    }

    [Fact]
    public void Base_defaults_allow_completion_and_add_nothing()
    {
        var script = new BaseQuestScript();
        var player = new Player(0);
        var req = new QuestRequirement { Type = RequirementType.Script };
        var reward = new QuestReward { Type = RewardType.Script };

        Assert.True(script.IsMet(req, player, null));
        Assert.Equal("", script.GetProgressText(req, player, null));
        Assert.Null(script.CanComplete(reward, player, null));
        script.OnTakeRequirement(req, player, null);   // must not throw
        script.GiveReward(reward, null, player, null); // must not throw
    }

    [Fact]
    public void The_shipped_example_quest_script_compiles()
    {
        var settings = new GooseSettings { DataPath = FindIllutiaDataDirectory() };
        var world = new GameWorld(settings);
        var script = world.ScriptHandler.GetScript<IQuestScript>("Scripts/Quest/ExampleQuestScript.csx");
        Assert.NotNull(script.Object);
    }

    /// <summary>Locates the real Data/Illutia directory without assuming the process working
    /// directory: walk up from the test output (Goose.Tests/bin/Debug/net10.0/) until a directory
    /// containing Data/Illutia is found, falling back to the Goose/ source tree copy.</summary>
    private static string FindIllutiaDataDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            foreach (var candidate in new[]
            {
                Path.Combine(dir.FullName, "Data", "Illutia"),
                Path.Combine(dir.FullName, "Goose", "Data", "Illutia"),
            })
            {
                if (Directory.Exists(candidate)) return candidate;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException(
            "Could not locate Data/Illutia from " + AppContext.BaseDirectory);
    }
}
