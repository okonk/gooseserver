using Goose;
using Goose.Quests;
using Goose.Testing;

namespace Goose.Tests;

public class QuestTalkToNpcTurnInTests
{
    private const int GiverTemplateId = 4;
    private const int DestinationTemplateId = 5;

    private static NPC Npc(int templateId) => new()
    {
        NPCTemplate = new NPCTemplate { NPCTemplateID = templateId, Name = "NPC " + templateId },
    };

    private static (Player player, Quest quest) Setup(TestWorldFixture world)
    {
        var player = new QuestCompletionTests.CapturingPlayer
        {
            Class = world.World.ClassHandler.GetClass(0)!,
        };
        player.Inventory = new Inventory(player, world.Settings);
        player.Spellbook = new Spellbook(player, world.Settings);

        var quest = new Quest
        {
            Id = 88,
            Name = "Word on the Cold",
            Description = "desc",
            FailText = "fail",
            PassText = "pass",
            ShowProgress = true,
        };
        quest.Requirements.Add(new QuestRequirement
        {
            Id = 96,
            Quest = quest,
            Type = RequirementType.TalkToNPC,
            Value = DestinationTemplateId,
            Value2 = 1,
        });

        QuestWindow.StartQuest(quest, player, world.World);
        return (player, quest);
    }

    [Fact]
    public void Opening_the_quest_at_its_talk_destination_skips_the_description_and_completes_it()
    {
        using var world = new TestWorldFixture();
        var (player, quest) = Setup(world);

        var window = new QuestWindow(Npc(DestinationTemplateId), player, quest, world.World);

        Assert.Equal("pass", window.GetCurrentText(player, world.World));
        Assert.Contains(player.QuestsCompleted, q => q.Id == quest.Id);
    }

    [Fact]
    public void Opening_the_quest_at_the_giver_still_shows_the_description()
    {
        using var world = new TestWorldFixture();
        var (player, quest) = Setup(world);

        var window = new QuestWindow(Npc(GiverTemplateId), player, quest, world.World);

        Assert.Equal("desc", window.GetCurrentText(player, world.World));
        Assert.DoesNotContain(player.QuestsCompleted, q => q.Id == quest.Id);
    }

    [Fact]
    public void Clicking_a_quest_npc_with_nothing_left_to_offer_still_counts_as_talking_to_them()
    {
        using var world = new TestWorldFixture();
        var (player, quest) = Setup(world);
        var done = new Quest { Id = 89, Name = "Already done" };
        var destination = Npc(DestinationTemplateId);
        destination.Quests = [done];
        player.QuestsCompleted.Add(done);

        QuestWindow.Handle(destination, player, world.World);

        Assert.Equal(1, player.QuestProgress.Single(p => p.Requirement.Quest.Id == quest.Id).Value);
    }
}
