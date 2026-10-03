using System.Text.Json;
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

    private static void ClaimWithRoster(TestWorldFixture world, int questId, params int[] roster)
    {
        world.World.QuestHandler.Claims[questId] = new QuestClaim
        {
            QuestId = questId,
            PlayerId = 1,
            CompletedAt = DateTime.UtcNow,
            Roster = [.. roster],
        };
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

    private static (QuestRequirement Gold, QuestRequirement Kill) AddRequirements(Quest quest)
    {
        var gold = new QuestRequirement { Id = 98, Quest = quest, Type = RequirementType.Gold, Value = 50 };
        var kill = new QuestRequirement { Id = 99, Quest = quest, Type = RequirementType.Kill, Value = 5, Value2 = 3 };
        quest.Requirements.Add(gold);
        quest.Requirements.Add(kill);
        return (gold, kill);
    }

    [Fact]
    public void Claim_rosters_active_requirement_meeting_players_online_and_offline_and_excludes_everyone_else()
    {
        var (world, _, _, quest) = SetupOneTime();
        StartDatabase(world);
        var (_, kill) = AddRequirements(quest);

        var offlineDid = MakePlayer(world, "OfflineDid", 2, 50);
        world.RegisterDatabasePlayer(offlineDid);
        offlineDid.QuestsStarted.Add(quest);
        offlineDid.QuestProgress.Add(new QuestProgress { Requirement = kill, Value = 3 });

        var onlineDid = MakePlayer(world, "OnlineDid", 3, 50);
        world.RegisterDatabasePlayer(onlineDid);
        onlineDid.QuestsStarted.Add(quest);
        onlineDid.QuestProgress.Add(new QuestProgress { Requirement = kill, Value = 3 });
        onlineDid.State = Player.States.Ready;

        var alice = MakePlayer(world, "Alice", 1, 50);
        world.RegisterDatabasePlayer(alice);
        alice.QuestsStarted.Add(quest);
        alice.QuestProgress.Add(new QuestProgress { Requirement = kill, Value = 3 });

        var bobNeverStarted = MakePlayer(world, "BobNeverStarted", 4, 50);
        world.RegisterDatabasePlayer(bobNeverStarted);
        bobNeverStarted.QuestProgress.Add(new QuestProgress { Requirement = kill, Value = 3 });

        var carolNoTicket = MakePlayer(world, "CarolNoTicket", 5, 50);
        world.RegisterDatabasePlayer(carolNoTicket);
        carolNoTicket.QuestsStarted.Add(quest);
        carolNoTicket.QuestProgress.Add(new QuestProgress { Requirement = kill, Value = 2 });

        var danDone = MakePlayer(world, "DanDone", 6, 50);
        world.RegisterDatabasePlayer(danDone);
        danDone.QuestsCompleted.Add(quest);

        world.World.QuestHandler.Claim(quest, alice, world.World);

        Assert.Equal(
            new HashSet<int> { offlineDid.PlayerID, onlineDid.PlayerID },
            new HashSet<int>(world.World.QuestHandler.Claims[quest.Id].Roster));

        Assert.Contains(onlineDid.Sent, s => s.Contains("You helped complete") && s.Contains("turn it in at"));
        Assert.Empty(offlineDid.Sent);

        string playerIds = "";
        world.World.Database.Execute(conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT player_ids FROM quest_claims WHERE quest_id=" + quest.Id;
            playerIds = Convert.ToString(cmd.ExecuteScalar()!)!;
        });
        Assert.Equal(
            new HashSet<int> { offlineDid.PlayerID, onlineDid.PlayerID },
            new HashSet<int>(JsonSerializer.Deserialize<HashSet<int>>(playerIds, JsonHelper.DatabaseOptions)!));

        world.World.Database.Stop();
        world.Dispose();
    }

    [Fact]
    public void Nudge_names_the_granting_npc_and_falls_back_when_none_grants_the_quest()
    {
        var (world, _, _, quest) = SetupOneTime();
        StartDatabase(world);
        var (_, kill) = AddRequirements(quest);
        world.World.NPCHandler.AddTemplate(new NPCTemplate { NPCTemplateID = 7, Name = "Grantor", BaseStats = new AttributeSet(), Quests = [quest] });

        var helper = MakePlayer(world, "Helper", 2, 50);
        world.RegisterDatabasePlayer(helper);
        helper.QuestsStarted.Add(quest);
        helper.QuestProgress.Add(new QuestProgress { Requirement = kill, Value = 3 });
        helper.State = Player.States.Ready;

        var completer = MakePlayer(world, "Alice", 1, 50);
        world.RegisterDatabasePlayer(completer);
        completer.QuestsStarted.Add(quest);

        world.World.QuestHandler.Claim(quest, completer, world.World);

        Assert.Contains(helper.Sent, s => s.Contains("Grantor"));
        world.World.Database.Stop();

        var (world2, _, _, quest2) = SetupOneTime();
        StartDatabase(world2);
        var (_, kill2) = AddRequirements(quest2);

        var helper2 = MakePlayer(world2, "Helper", 2, 50);
        world2.RegisterDatabasePlayer(helper2);
        helper2.QuestsStarted.Add(quest2);
        helper2.QuestProgress.Add(new QuestProgress { Requirement = kill2, Value = 3 });
        helper2.State = Player.States.Ready;

        var completer2 = MakePlayer(world2, "Alice", 1, 50);
        world2.RegisterDatabasePlayer(completer2);
        completer2.QuestsStarted.Add(quest2);

        world2.World.QuestHandler.Claim(quest2, completer2, world2.World);

        Assert.Contains(helper2.Sent, s => s.Contains("the quest giver"));
        world2.World.Database.Stop();

        world.Dispose();
        world2.Dispose();
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

    [Fact]
    public void Rostered_player_sees_lists_starts_and_turns_in_a_claimed_quest_normally()
    {
        var (world, npc, _, quest) = SetupOneTime();
        var player = MakePlayer(world, "Rostered", 2, 100);
        var goldRequirement = new QuestRequirement { Id = 98, Quest = quest, Type = RequirementType.Gold, Value = 50 };
        quest.Requirements.Add(goldRequirement);
        ClaimWithRoster(world, quest.Id, player.PlayerID);
        player.QuestsStarted.Add(quest);

        Assert.Contains(QuestWindow.GetAvailableQuests(npc, player, world.World), q => q.Id == quest.Id);
        Assert.Equal(QuestIconState.Ready, QuestStateResolver.Resolve(npc, player, world.World));

        var window = new QuestWindow(npc, player, quest, world.World);
        window.Clicked(Window.ButtonTypes.Next, npc.NPCTemplate.NPCTemplateID, 0, 0, player, world.World);

        Assert.Equal(quest.PassText, window.GetCurrentText(player, world.World));
        Assert.Equal(50, player.Gold);
        Assert.Contains(player.QuestsCompleted, q => q.Id == quest.Id);
        Assert.Equal(1, world.World.QuestHandler.Claims[quest.Id].PlayerId);
        Assert.Equal([player.PlayerID], world.World.QuestHandler.Claims[quest.Id].Roster);

        world.Dispose();
    }

    [Fact]
    public void Rostered_player_who_already_completed_is_blocked_from_re_turn_in_including_repeatable()
    {
        var (world, npc, _, quest) = SetupOneTime();
        quest.Repeatable = true;
        var player = MakePlayer(world, "Rostered", 2, 0);
        ClaimWithRoster(world, quest.Id, player.PlayerID);
        player.QuestsCompleted.Add(quest);
        player.QuestsStarted.Add(quest);

        var window = new QuestWindow(npc, player, quest, world.World);
        window.Clicked(Window.ButtonTypes.Next, npc.NPCTemplate.NPCTemplateID, 0, 0, player, world.World);

        var text = window.GetCurrentText(player, world.World);
        Assert.Contains("Someone has already completed this quest", text);
        Assert.Contains("It can only be completed once", text);
        Assert.DoesNotContain(player.QuestsStarted, q => q.Id == quest.Id);
        Assert.Equal(1, world.World.QuestHandler.Claims[quest.Id].PlayerId);

        var (world2, npc2, _, quest2) = SetupOneTime();
        var player2 = MakePlayer(world2, "Rostered", 2, 0);
        ClaimWithRoster(world2, quest2.Id, player2.PlayerID);
        player2.QuestsCompleted.Add(quest2);
        player2.QuestsStarted.Add(quest2);

        var window2 = new QuestWindow(npc2, player2, quest2, world2.World);
        window2.Clicked(Window.ButtonTypes.Next, npc2.NPCTemplate.NPCTemplateID, 0, 0, player2, world2.World);

        Assert.DoesNotContain(player2.Windows, w => w == window2);
        Assert.Equal(1, world2.World.QuestHandler.Claims[quest2.Id].PlayerId);

        world.Dispose();
        world2.Dispose();
    }

    [Fact]
    public void First_completion_broadcasts_world_first_to_online_players_and_a_rostered_turn_in_never_does()
    {
        var (world, npc, _, quest) = SetupOneTime();
        StartDatabase(world);

        var observer = MakePlayer(world, "Observer", 99, 0);
        observer.State = Player.States.Ready;
        world.AddOnlinePlayer(observer);

        var alice = MakePlayer(world, "Alice", 1, 0);
        world.RegisterDatabasePlayer(alice);
        alice.QuestsStarted.Add(quest);
        var firstWindow = new QuestWindow(npc, alice, quest, world.World);
        firstWindow.Clicked(Window.ButtonTypes.Next, npc.NPCTemplate.NPCTemplateID, 0, 0, alice, world.World);

        Assert.Contains(alice.QuestsCompleted, q => q.Id == quest.Id);
        Assert.Contains(observer.Sent, s => s.Contains("[World First] One Time has been completed by Alice!"));

        var bob = MakePlayer(world, "Bob", 2, 0);
        world.RegisterDatabasePlayer(bob);
        world.World.QuestHandler.Claims[quest.Id].Roster = [bob.PlayerID];
        bob.QuestsStarted.Add(quest);
        var secondWindow = new QuestWindow(npc, bob, quest, world.World);
        secondWindow.Clicked(Window.ButtonTypes.Next, npc.NPCTemplate.NPCTemplateID, 0, 0, bob, world.World);

        Assert.Contains(bob.QuestsCompleted, q => q.Id == quest.Id);
        Assert.Single(observer.Sent, s => s.Contains("[World First]"));

        world.World.Database.Stop();
        world.Dispose();
    }

    [Fact]
    public void Rostered_player_can_restart_a_claimed_quest_and_a_non_rostered_one_still_cannot()
    {
        var (world, _, _, quest) = SetupOneTime();
        ClaimWithRoster(world, quest.Id, 2);

        var rostered = MakePlayer(world, "Rostered", 2, 0);
        var other = MakePlayer(world, "Other", 3, 0);

        QuestWindow.StartQuest(quest, rostered, world.World);
        QuestWindow.StartQuest(quest, other, world.World);

        Assert.Contains(rostered.QuestsStarted, q => q.Id == quest.Id);
        Assert.DoesNotContain(other.QuestsStarted, q => q.Id == quest.Id);

        world.Dispose();
    }
}
