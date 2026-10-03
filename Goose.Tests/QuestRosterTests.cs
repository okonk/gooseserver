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
}
