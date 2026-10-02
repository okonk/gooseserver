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

    private static TestWorldFixture.CapturingPlayer MakePlayer(TestWorldFixture world, string name, int playerId, long gold)
    {
        var player = new TestWorldFixture.CapturingPlayer
        {
            Name = name,
            PlayerID = playerId,
            Class = world.World.ClassHandler.GetClass(0)!,
            BaseStats = new AttributeSet(),
            MaxStats = new AttributeSet(),
            Gold = gold,
        };
        player.Inventory = new Inventory(player, world.Settings);
        player.Spellbook = new Spellbook(player, world.Settings);
        return player;
    }

    private static void StartDatabase(TestWorldFixture world)
    {
        world.World.Database.Start(Path.Combine(world.DataDirectory, "test.db"));
        world.World.Database.Execute(conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "sql", "quest_claims.sql"));
            cmd.ExecuteNonQuery();
        });
    }

    [Fact]
    public void A_second_player_turning_in_a_claimed_quest_is_blocked_and_evicted()
    {
        var (world, npc, _, quest) = SetupOneTime();
        Claim(world, quest.Id);
        var claimer = MakePlayer(world, "Claimer", 1, 0);
        world.RegisterDatabasePlayer(claimer);

        var goldRequirement = new QuestRequirement { Id = 98, Quest = quest, Type = RequirementType.Gold, Value = 50 };
        var killRequirement = new QuestRequirement { Id = 99, Quest = quest, Type = RequirementType.Kill, Value = 5, Value2 = 3 };
        quest.Requirements.Add(goldRequirement);
        quest.Requirements.Add(killRequirement);

        var player = MakePlayer(world, "Second", 2, 100);
        player.QuestsStarted.Add(quest);
        player.QuestProgress.Add(new QuestProgress { Requirement = killRequirement, Value = 3 });

        var window = new QuestWindow(npc, player, quest, world.World);
        window.Clicked(Window.ButtonTypes.Next, npc.NPCTemplate.NPCTemplateID, 0, 0, player, world.World);

        var text = window.GetCurrentText(player, world.World);
        Assert.Contains("Claimer has already completed this quest", text);
        Assert.Contains("It can only be completed once", text);
        Assert.Equal(100, player.Gold);
        Assert.DoesNotContain(player.QuestsStarted, q => q.Id == quest.Id);
        Assert.Empty(player.QuestProgress);
        Assert.DoesNotContain(player.QuestsCompleted, q => q.Id == quest.Id);

        world.Dispose();
    }

    [Fact]
    public void A_second_player_turning_in_a_claimed_quest_sees_Someone_for_an_unregistered_claimer()
    {
        var (world, npc, _, quest) = SetupOneTime();
        Claim(world, quest.Id);

        var player = MakePlayer(world, "Second", 2, 0);
        player.QuestsStarted.Add(quest);

        var window = new QuestWindow(npc, player, quest, world.World);
        window.Clicked(Window.ButtonTypes.Next, npc.NPCTemplate.NPCTemplateID, 0, 0, player, world.World);

        var text = window.GetCurrentText(player, world.World);
        Assert.Contains("Someone has already completed this quest", text);
        Assert.DoesNotContain(player.QuestsStarted, q => q.Id == quest.Id);

        world.Dispose();
    }

    [Fact]
    public void The_first_completion_claims_the_quest_and_blocks_a_second_player()
    {
        var (world, npc, _, quest) = SetupOneTime();
        StartDatabase(world);

        var first = MakePlayer(world, "Alice", 1, 0);
        world.RegisterDatabasePlayer(first);
        first.QuestsStarted.Add(quest);
        var firstWindow = new QuestWindow(npc, first, quest, world.World);
        firstWindow.Clicked(Window.ButtonTypes.Next, npc.NPCTemplate.NPCTemplateID, 0, 0, first, world.World);

        Assert.Contains(first.QuestsCompleted, q => q.Id == quest.Id);
        Assert.True(world.World.QuestHandler.IsClaimed(quest.Id));

        int claimRows = 0;
        world.World.Database.Execute(conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM quest_claims WHERE quest_id=" + quest.Id;
            claimRows = Convert.ToInt32(cmd.ExecuteScalar()!);
        });
        Assert.Equal(1, claimRows);

        var second = MakePlayer(world, "Bob", 2, 0);
        second.QuestsStarted.Add(quest);
        var secondWindow = new QuestWindow(npc, second, quest, world.World);
        secondWindow.Clicked(Window.ButtonTypes.Next, npc.NPCTemplate.NPCTemplateID, 0, 0, second, world.World);

        var text = secondWindow.GetCurrentText(second, world.World);
        Assert.Contains("Alice has already completed this quest", text);
        Assert.DoesNotContain(second.QuestsCompleted, q => q.Id == quest.Id);
        Assert.DoesNotContain(second.QuestsStarted, q => q.Id == quest.Id);

        world.World.Database.Stop();
        world.Dispose();
    }

    [Fact]
    public void A_repeatable_one_time_quest_cannot_be_claimed_twice_by_the_claimer()
    {
        var (world, npc, _, quest) = SetupOneTime();
        quest.Repeatable = true;
        quest.Rewards.Add(new QuestReward { Id = 100, Type = RewardType.Gold, LongValue = 100 });
        StartDatabase(world);

        var claimer = MakePlayer(world, "Alice", 1, 50);
        world.RegisterDatabasePlayer(claimer);
        claimer.QuestsStarted.Add(quest);
        var firstWindow = new QuestWindow(npc, claimer, quest, world.World);
        firstWindow.Clicked(Window.ButtonTypes.Next, npc.NPCTemplate.NPCTemplateID, 0, 0, claimer, world.World);

        Assert.Contains(claimer.QuestsCompleted, q => q.Id == quest.Id);
        Assert.True(world.World.QuestHandler.IsClaimed(quest.Id));
        Assert.Equal(150, claimer.Gold);

        claimer.QuestsStarted.Add(quest);
        var secondWindow = new QuestWindow(npc, claimer, quest, world.World);
        secondWindow.Clicked(Window.ButtonTypes.Next, npc.NPCTemplate.NPCTemplateID, 0, 0, claimer, world.World);

        var text = secondWindow.GetCurrentText(claimer, world.World);
        Assert.Contains("Alice has already completed this quest", text);
        Assert.Equal(150, claimer.Gold);
        Assert.DoesNotContain(claimer.QuestsStarted, q => q.Id == quest.Id);

        world.World.Database.Stop();
        world.Dispose();
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
