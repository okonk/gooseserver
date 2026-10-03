using Goose;
using Goose.Quests;
using Goose.Testing;
using Xunit;

namespace Goose.Tests;

public class QuestRosterTests
{
    private static (TestWorldFixture World, NPC Npc, TestWorldFixture.CapturingPlayer Player, Quest Quest) SetupOneTime()
    {
        var world = new TestWorldFixture();
        var quest = new Quest
        {
            Id = 9,
            Name = "One Time",
            Description = "desc",
            FailText = "fail",
            PassText = "pass",
            OnlyOnePlayerCanComplete = true,
        };
        var npc = new NPC
        {
            NPCTemplate = new NPCTemplate { NPCTemplateID = 5, Name = "Quest NPC" },
            Quests = [quest],
        };
        var player = new TestWorldFixture.CapturingPlayer
        {
            Class = world.World.ClassHandler.GetClass(0)!,
            BaseStats = new AttributeSet(),
            MaxStats = new AttributeSet(),
        };
        player.Inventory = new Inventory(player, world.Settings);
        player.Spellbook = new Spellbook(player, world.Settings);
        return (world, npc, player, quest);
    }

    [Theory]
    [InlineData(true, false, false)]
    [InlineData(true, true, true)]
    [InlineData(false, false, true)]
    [InlineData(false, true, true)]
    public void Gate_follows_roster_and_completion(bool rostered, bool completed, bool expectBlocked)
    {
        var (world, _, player, quest) = SetupOneTime();
        world.World.QuestHandler.Claims[quest.Id] = new QuestClaim
        {
            QuestId = quest.Id,
            PlayerId = 1,
            CompletedAt = DateTime.UtcNow,
            Roster = rostered ? [player.PlayerID] : [],
        };
        if (completed) player.QuestsCompleted.Add(quest);

        Assert.Equal(expectBlocked, world.World.QuestHandler.IsClaimedFor(quest, player));
    }

    [Fact]
    public void Gate_never_blocks_non_one_time_or_unclaimed_quests()
    {
        var (world, _, player, quest) = SetupOneTime();
        quest.OnlyOnePlayerCanComplete = false;
        Assert.False(world.World.QuestHandler.IsClaimedFor(quest, player));

        quest.OnlyOnePlayerCanComplete = true;
        Assert.False(world.World.QuestHandler.IsClaimedFor(quest, player));
    }
}
