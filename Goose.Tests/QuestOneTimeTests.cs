using Goose;
using Goose.Quests;
using Goose.Testing;
using Xunit;

namespace Goose.Tests;

public class QuestOneTimeTests
{
    private static (TestWorldFixture World, NPC Npc, TestWorldFixture.CapturingPlayer Player, Quest Quest) SetupOneTime()
    {
        var world = new TestWorldFixture();
        var quest = new Quest
        {
            Id = 9,
            Name = "One Time",
            Description = "desc",
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

    private static void Claim(TestWorldFixture world, int questId)
    {
        world.World.QuestHandler.Claims[questId] = new QuestClaim
        {
            QuestId = questId,
            PlayerId = 1,
            CompletedAt = DateTime.UtcNow,
        };
    }

    [Fact]
    public void A_claimed_one_time_quest_is_hidden_from_a_fresh_player_but_visible_to_a_starter()
    {
        var (world, npc, player, quest) = SetupOneTime();
        Claim(world, quest.Id);

        Assert.DoesNotContain(QuestWindow.GetAvailableQuests(npc, player, world.World), q => q.Id == quest.Id);

        player.QuestsStarted.Add(quest);

        Assert.Contains(QuestWindow.GetAvailableQuests(npc, player, world.World), q => q.Id == quest.Id);
    }

    [Fact]
    public void A_claimed_one_time_quest_resolves_none_for_a_non_starter_and_ready_for_a_starter()
    {
        var (world, npc, player, quest) = SetupOneTime();
        Claim(world, quest.Id);

        Assert.Equal(QuestIconState.None, QuestStateResolver.Resolve(npc, player, world.World));

        player.QuestsStarted.Add(quest);

        Assert.Equal(QuestIconState.Ready, QuestStateResolver.Resolve(npc, player, world.World));
    }

    [Fact]
    public void Starting_a_claimed_one_time_quest_is_refused_for_a_player_who_never_started_it()
    {
        var (world, npc, player, quest) = SetupOneTime();
        Claim(world, quest.Id);

        QuestWindow.StartQuest(quest, player, world.World);

        Assert.DoesNotContain(player.QuestsStarted, q => q.Id == quest.Id);
    }

    [Fact]
    public void No_quest_is_claimed_before_any_claim_exists()
    {
        using var fixture = new TestWorldFixture();
        Assert.False(fixture.World.QuestHandler.IsClaimed(9));
    }

    [Fact]
    public void GetPlayerName_resolves_registered_player_and_null_for_unknown_id()
    {
        using var fixture = new TestWorldFixture();
        var player = new TestWorldFixture.CapturingPlayer { Name = "Bob", PlayerID = 42 };
        fixture.RegisterDatabasePlayer(player);

        Assert.Equal("Bob", fixture.World.PlayerHandler.GetPlayerName(42));
        Assert.Null(fixture.World.PlayerHandler.GetPlayerName(9999));
    }
}
