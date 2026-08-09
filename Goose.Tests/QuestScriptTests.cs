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
        //
        // RequirementType.Gold stands in for RequirementType.Script: the Script member is added
        // in Task 2, and this test only needs to prove the .csx can NAME the quest types.
        var script = fixture.Compile(@"
using Goose; using Goose.Quests; using Goose.Scripting;
public class T : BaseQuestScript
{
    public override bool IsMet(QuestRequirement requirement, Player player, GameWorld world)
        => requirement.Type == RequirementType.Gold && requirement.Quest != null;
}
return typeof(T);
");
        var quest = new Quest { Id = 1 };
        var req = new QuestRequirement { Type = RequirementType.Gold, Quest = quest };

        // Player(0), not Player() — the parameterless ctor leaves collections null (Player.cs:465).
        Assert.True(script.Object.IsMet(req, new Player(0), fixture.World));
    }

    [Fact]
    public void Base_defaults_allow_completion_and_add_nothing()
    {
        var script = new BaseQuestScript();
        var player = new Player(0);
        // Gold stands in for Script/RewardType.Script: those members are added in Task 2.
        var req = new QuestRequirement { Type = RequirementType.Gold };
        var reward = new QuestReward { Type = RewardType.Gold };

        Assert.True(script.IsMet(req, player, null));
        Assert.Equal("", script.GetProgressText(req, player, null));
        Assert.Null(script.CanComplete(reward, player, null));
        script.OnTakeRequirement(req, player, null);   // must not throw
        script.GiveReward(reward, null, player, null); // must not throw
    }
}
