using Goose;
using Goose.Quests;
using Goose.Testing;

namespace Goose.Tests;

public class QuestWindowNullTemplateTests
{
    private static (NPC npc, Player player, Quest quest) Fixture(TestWorldFixture world, RequirementType type)
    {
        var npc = new NPC
        {
            NPCTemplate = new NPCTemplate { NPCTemplateID = 5, Name = "Quest NPC" },
        };

        var player = new Player(0);
        player.Inventory = new Inventory(player, world.Settings);
        player.Spellbook = new Spellbook(player, world.Settings);

        var quest = new Quest
        {
            Id = 77,
            Name = "Null Template Quest",
            Description = "desc",
            FailText = "fail",
            PassText = "pass",
            MinLevel = 0,
            MinExperience = 0,
            Repeatable = false,
            ShowProgress = true,
        };
        quest.Requirements.Add(new QuestRequirement
        {
            Id = 99,
            Quest = quest,
            Type = type,
            // 999: no item/NPC template registered in the fixture world.
            Value = 999,
            Value2 = 3,
        });

        return (npc, player, quest);
    }

    [Fact]
    public void GetQuestProgressText_UnknownItemTemplate_RendersUnknownInsteadOfThrowing()
    {
        using var world = new TestWorldFixture();
        var (npc, player, quest) = Fixture(world, RequirementType.Item);

        var window = new QuestWindow(npc, player, quest, world.World);

        string text = window.GetQuestProgressText(player, world.World);

        Assert.Contains("Unknown item", text);
    }

    [Fact]
    public void GetQuestProgressText_UnknownTalkNpcTemplate_RendersUnknownInsteadOfThrowing()
    {
        using var world = new TestWorldFixture();
        var (npc, player, quest) = Fixture(world, RequirementType.TalkToNPC);

        var window = new QuestWindow(npc, player, quest, world.World);

        string text = window.GetQuestProgressText(player, world.World);

        Assert.Contains("Unknown NPC", text);
    }

    [Fact]
    public void GetQuestProgressText_UnknownKillNpcTemplate_RendersUnknownInsteadOfThrowing()
    {
        using var world = new TestWorldFixture();
        var (npc, player, quest) = Fixture(world, RequirementType.Kill);

        var window = new QuestWindow(npc, player, quest, world.World);

        string text = window.GetQuestProgressText(player, world.World);

        Assert.Contains("Unknown NPC", text);
    }
}
