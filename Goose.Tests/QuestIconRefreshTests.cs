using Goose;
using Goose.Quests;
using Goose.Testing;
using Xunit;

namespace Goose.Tests;

public class QuestIconRefreshTests
{
    private static Quest MakeQuest(int id, params QuestRequirement[] requirements)
    {
        var quest = new Quest
        {
            Id = id,
            Name = "Quest " + id,
            Description = "desc",
            FailText = "fail",
            PassText = "pass",
        };
        foreach (var r in requirements)
        {
            r.Quest = quest;
            quest.Requirements.Add(r);
        }
        return quest;
    }

    private static QuestRequirement Req(int id, RequirementType type, long value, long value2)
        => new() { Id = id, Type = type, Value = value, Value2 = value2 };

    private static (TestWorldFixture World, Map Map, TestWorldFixture.CapturingPlayer Player, NPC Npc) Setup(params Quest[] quests)
    {
        var world = new TestWorldFixture();
        var map = world.AddBaseMap(1, "Town");
        var player = world.CommandPlayerOn(map, 2, 2, "Hero");
        player.Spellbook = new Spellbook(player, world.Settings);
        var npc = new NPC
        {
            NPCTemplate = new NPCTemplate { NPCTemplateID = 5, Name = "Quest NPC" },
            Quests = quests.ToList(),
            MapX = 3, MapY = 3,
        };
        map.AddNPC(npc);
        return (world, map, player, npc);
    }

    private static string? LastChi(TestWorldFixture.CapturingPlayer player, int npcLoginId)
    {
        var prefix = "CHI" + npcLoginId + ",";
        string? last = null;
        foreach (var s in player.Sent)
            if (s.StartsWith(prefix)) last = s[..^1];
        return last;
    }

    [Fact]
    public void Acceptance_CreatesProgress_AndClearsAvailableIcon()
    {
        var quest = MakeQuest(1, Req(1, RequirementType.Kill, 99, 5));
        var (world, map, player, npc) = Setup(quest);

        QuestWindow.Handle(npc, player, world.World);

        Assert.Contains(player.QuestsStarted, q => q.Id == quest.Id);
        var progress = Assert.Single(player.QuestProgress);
        Assert.Equal(0, progress.Value);
        Assert.Equal($"CHI{npc.LoginID},0,0", LastChi(player, npc.LoginID));
    }

    [Fact]
    public void MatchingKill_ReachesTarget_RefreshesReadyIcon()
    {
        var quest = MakeQuest(1, Req(1, RequirementType.Kill, 99, 2));
        var (world, map, player, npc) = Setup(quest);
        player.QuestsStarted.Add(quest);
        player.QuestProgress.Add(new QuestProgress { Requirement = quest.Requirements[0], Value = 1 });
        var killTarget = new NPC { NPCTemplate = new NPCTemplate { NPCTemplateID = 99, Name = "Rat" } };

        player.Killed(killTarget, world.World);

        Assert.Equal(2, player.QuestProgress[0].Value);
        var s = world.Settings;
        Assert.Equal($"CHI{npc.LoginID},{s.QuestReadyIconSheet},{s.QuestReadyIconGraphic}", LastChi(player, npc.LoginID));
    }

    [Fact]
    public void MatchingTalk_ReachesTarget_RefreshesReadyIcon()
    {
        var quest = MakeQuest(1, Req(1, RequirementType.TalkToNPC, 77, 1));
        var (world, map, player, npc) = Setup(quest);
        player.QuestsStarted.Add(quest);
        player.QuestProgress.Add(new QuestProgress { Requirement = quest.Requirements[0], Value = 0 });
        var talkTarget = new NPC { NPCTemplate = new NPCTemplate { NPCTemplateID = 77, Name = "Villager" } };

        player.TalkedTo(talkTarget, world.World);

        Assert.Equal(1, player.QuestProgress[0].Value);
        var s = world.Settings;
        Assert.Equal($"CHI{npc.LoginID},{s.QuestReadyIconSheet},{s.QuestReadyIconGraphic}", LastChi(player, npc.LoginID));
    }

    [Fact]
    public void Completion_UnlocksPrerequisiteFollowUp_RefreshesAvailableIcon()
    {
        var first = MakeQuest(1);
        var followUp = MakeQuest(2);
        followUp.PrerequisiteQuests.Add(1);
        var (world, map, player, npc) = Setup(first, followUp);
        player.QuestsStarted.Add(first);

        var window = new QuestWindow(npc, player, first, world.World);
        window.Clicked(Window.ButtonTypes.Next, npc.NPCTemplate.NPCTemplateID, 0, 0, player, world.World);

        Assert.Contains(player.QuestsCompleted, q => q.Id == first.Id);
        Assert.DoesNotContain(player.QuestsStarted, q => q.Id == followUp.Id);
        var s = world.Settings;
        Assert.Equal($"CHI{npc.LoginID},{s.QuestAvailableIconSheet},{s.QuestAvailableIconGraphic}", LastChi(player, npc.LoginID));
    }

    [Fact]
    public void Abandonment_RemovesStartedAndProgress_RefreshesAvailableIcon()
    {
        var quest = MakeQuest(1, Req(1, RequirementType.Kill, 99, 5));
        var (world, map, player, npc) = Setup(quest);
        player.QuestsStarted.Add(quest);
        player.QuestProgress.Add(new QuestProgress { Requirement = quest.Requirements[0], Value = 1 });

        var confirm = new AbandonConfirmWindow(quest, player, world.World);
        confirm.Clicked(Window.ButtonTypes.Next, 0, 0, 0, player, world.World);

        Assert.DoesNotContain(player.QuestsStarted, q => q.Id == quest.Id);
        Assert.Empty(player.QuestProgress);
        var s = world.Settings;
        Assert.Equal($"CHI{npc.LoginID},{s.QuestAvailableIconSheet},{s.QuestAvailableIconGraphic}", LastChi(player, npc.LoginID));
    }

    [Fact]
    public void UnrelatedKillAndTalk_EmitNoIconRefresh()
    {
        var quest = MakeQuest(1, Req(1, RequirementType.Kill, 99, 5));
        var (world, map, player, npc) = Setup(quest);
        player.QuestsStarted.Add(quest);
        player.QuestProgress.Add(new QuestProgress { Requirement = quest.Requirements[0], Value = 1 });

        player.Killed(new NPC { NPCTemplate = new NPCTemplate { NPCTemplateID = 100, Name = "Other" } }, world.World);
        player.TalkedTo(new NPC { NPCTemplate = new NPCTemplate { NPCTemplateID = 101, Name = "Stranger" } }, world.World);

        Assert.Equal(1, player.QuestProgress[0].Value);
        Assert.DoesNotContain(player.Sent, s => s.StartsWith("CHI"));
    }
}
