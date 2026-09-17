using Goose;
using Goose.Quests;
using Goose.Testing;
using Xunit;

namespace Goose.Tests;

public class QuestOptionListTests
{
    private sealed class CapturingPlayer : Player
    {
        public CapturingPlayer() : base(0) { }
        public List<string> Sent { get; } = new();
        public override bool Send(string data) { Sent.Add(data); return true; }
    }

    private static Quest MakeQuest(int id, string name) => new()
    {
        Id = id,
        Name = name,
        Description = "desc",
        FailText = "fail",
        PassText = "pass",
    };

    private static (TestWorldFixture World, NPC Npc, CapturingPlayer Player) Setup(params Quest[] quests)
    {
        var world = new TestWorldFixture();
        var npc = new NPC
        {
            NPCTemplate = new NPCTemplate { NPCTemplateID = 5, Name = "Quest NPC" },
            Quests = quests.ToList(),
        };
        var player = new CapturingPlayer
        {
            Class = world.World.ClassHandler.GetClass(0)!,
        };
        return (world, npc, player);
    }

    [Fact]
    public void Handle_SingleAvailableQuest_OpensQuestWindowDirectly()
    {
        var (world, npc, player) = Setup(MakeQuest(1, "Quest One"));

        QuestWindow.Handle(npc, player, world.World);

        Assert.Single(player.Windows);
        Assert.Equal(Window.WindowTypes.Quest, player.Windows[0].Type);
        Assert.Contains(player.QuestsStarted, q => q.Id == 1);
    }

    [Fact]
    public void Handle_MultipleAvailableQuests_OpensOptionListWithQuestNames()
    {
        var (world, npc, player) = Setup(MakeQuest(1, "Quest One"), MakeQuest(2, "Quest Two"), MakeQuest(3, "Quest Three"));

        QuestWindow.Handle(npc, player, world.World);

        Assert.Single(player.Windows);
        var list = Assert.IsType<OptionListWindow>(player.Windows[0]);
        Assert.Equal(Window.WindowFrames.OptionList, list.Frame);
        Assert.Equal(npc, list.NPC);
        Assert.Equal(["WNF1001,1,Quest One|0|0|0|0|*", "WNF1001,2,Quest Two|0|0|0|0|*", "WNF1001,3,Quest Three|0|0|0|0|*"],
            player.Sent.Where(s => s.StartsWith("WNF")).ToArray());
        Assert.Empty(player.QuestsStarted);
    }

    [Fact]
    public void Handle_NoAvailableQuests_OpensNothing()
    {
        var (world, npc, player) = Setup(MakeQuest(1, "Quest One"));
        player.QuestsCompleted.Add(npc.Quests[0]);

        QuestWindow.Handle(npc, player, world.World);

        Assert.Empty(player.Windows);
    }

    [Fact]
    public void Handle_FiltersOutUnavailableQuests()
    {
        var (world, npc, player) = Setup(
            MakeQuest(1, "Available"),
            MakeQuest(2, "Completed"),
            MakeQuest(3, "Gated"),
            MakeQuest(4, "Too High Level"),
            MakeQuest(5, "Class Restricted"));

        player.QuestsCompleted.Add(npc.Quests[1]);
        npc.Quests[2].PrerequisiteQuests.Add(99);
        npc.Quests[3].MaxLevel = 1;
        player.Level = 5;
        npc.Quests[4].ClassRestrictions = 1L << 1;

        QuestWindow.Handle(npc, player, world.World);

        Assert.Single(player.Windows);
        Assert.Equal(Window.WindowTypes.Quest, player.Windows[0].Type);
        Assert.Contains(player.QuestsStarted, q => q.Id == 1);
    }

    [Fact]
    public void LineClick_ClosesOptionListAndOpensQuestWindowForClickedQuest()
    {
        var (world, npc, player) = Setup(MakeQuest(1, "Quest One"), MakeQuest(2, "Quest Two"));

        QuestWindow.Handle(npc, player, world.World);
        player.Windows[0].LineClicked(1, npc.LoginID, player, world.World);

        Assert.Single(player.Windows);
        Assert.Equal(Window.WindowTypes.Quest, player.Windows[0].Type);
        Assert.Contains(player.QuestsStarted, q => q.Id == 2);
        Assert.DoesNotContain(player.QuestsStarted, q => q.Id == 1);
        Assert.Contains(player.Sent, s => s.StartsWith("CLW1001"));
    }

    [Fact]
    public void QuestWindow_Next_AlreadyCompleted_ClosesWindowWithCLW()
    {
        var (world, npc, player) = Setup(MakeQuest(1, "Quest One"));
        var quest = npc.Quests[0];
        var window = new QuestWindow(npc, player, quest, world.World);
        player.QuestsCompleted.Add(quest);

        window.Clicked(Window.ButtonTypes.Next, npc.LoginID, 0, 0, player, world.World);

        Assert.Empty(player.Windows);
        Assert.Contains(player.Sent, s => s.StartsWith("CLW1001"));
    }

    [Fact]
    public void LineClick_OutOfRange_IsIgnored()
    {
        var (world, npc, player) = Setup(MakeQuest(1, "Quest One"), MakeQuest(2, "Quest Two"));

        QuestWindow.Handle(npc, player, world.World);
        player.Windows[0].LineClicked(5, npc.LoginID, player, world.World);

        Assert.Single(player.Windows);
        Assert.Equal(Window.WindowTypes.OptionList, player.Windows[0].Type);
    }

    [Fact]
    public void Close_RemovesWindow()
    {
        var (world, npc, player) = Setup(MakeQuest(1, "Quest One"), MakeQuest(2, "Quest Two"));

        QuestWindow.Handle(npc, player, world.World);
        player.Windows[0].Clicked(Window.ButtonTypes.Close, npc.LoginID, 0, 0, player, world.World);

        Assert.Empty(player.Windows);
        Assert.DoesNotContain(player.Sent, s => s.StartsWith("CLW"));
    }

    [Fact]
    public void Handle_ClosesPreviousOptionListForSameNpc()
    {
        var (world, npc, player) = Setup(MakeQuest(1, "Quest One"), MakeQuest(2, "Quest Two"));

        QuestWindow.Handle(npc, player, world.World);
        QuestWindow.Handle(npc, player, world.World);

        Assert.Single(player.Windows);
        Assert.Contains(player.Sent, s => s.StartsWith("CLW1001"));
    }

    [Fact]
    public void Handle_MoreThanTenQuests_PagesOptionListAtLineLimit()
    {
        var quests = Enumerable.Range(1, 11).Select(i => MakeQuest(i, $"Quest {i}")).ToArray();
        var (world, npc, player) = Setup(quests);

        QuestWindow.Handle(npc, player, world.World);

        Assert.Single(player.Windows);
        var list = player.Windows[0];
        Assert.Equal("0,1,0,1,0", list.Buttons);
        var lines = player.Sent.Where(s => s.StartsWith("WNF")).ToArray();
        Assert.Equal(10, lines.Length);
        Assert.DoesNotContain(lines, s => s.Contains("Quest 11|"));
    }

    [Fact]
    public void NextPage_SendsRemainingLinesAndHidesNextButton()
    {
        var quests = Enumerable.Range(1, 11).Select(i => MakeQuest(i, $"Quest {i}")).ToArray();
        var (world, npc, player) = Setup(quests);

        QuestWindow.Handle(npc, player, world.World);
        player.Sent.Clear();
        player.Windows[0].Clicked(Window.ButtonTypes.Next, npc.LoginID, 0, 0, player, world.World);

        var list = player.Windows[0];
        Assert.Equal("0,1,1,0,0", list.Buttons);
        Assert.Equal(["WNF1001,1,Quest 11|0|0|0|0|*"], player.Sent.Where(s => s.StartsWith("WNF")).ToArray());
    }

    [Fact]
    public void NextPage_LineClick_UsesGlobalQuestIndex()
    {
        var quests = Enumerable.Range(1, 11).Select(i => MakeQuest(i, $"Quest {i}")).ToArray();
        var (world, npc, player) = Setup(quests);

        QuestWindow.Handle(npc, player, world.World);
        player.Windows[0].Clicked(Window.ButtonTypes.Next, npc.LoginID, 0, 0, player, world.World);
        player.Windows[0].LineClicked(0, npc.LoginID, player, world.World);

        Assert.Single(player.Windows);
        Assert.Equal(Window.WindowTypes.Quest, player.Windows[0].Type);
        Assert.Contains(player.QuestsStarted, q => q.Id == 11);
    }

    [Fact]
    public void Back_ReturnsToFirstPage()
    {
        var quests = Enumerable.Range(1, 11).Select(i => MakeQuest(i, $"Quest {i}")).ToArray();
        var (world, npc, player) = Setup(quests);

        QuestWindow.Handle(npc, player, world.World);
        player.Windows[0].Clicked(Window.ButtonTypes.Next, npc.LoginID, 0, 0, player, world.World);
        player.Sent.Clear();
        player.Windows[0].Clicked(Window.ButtonTypes.Back, npc.LoginID, 0, 0, player, world.World);

        var list = player.Windows[0];
        Assert.Equal("0,1,0,1,0", list.Buttons);
        var lines = player.Sent.Where(s => s.StartsWith("WNF")).ToArray();
        Assert.Equal(10, lines.Length);
        Assert.Contains(lines, s => s.Contains("Quest 1|"));
    }

    [Fact]
    public void Paging_AtBounds_IsIgnored()
    {
        var quests = Enumerable.Range(1, 11).Select(i => MakeQuest(i, $"Quest {i}")).ToArray();
        var (world, npc, player) = Setup(quests);

        QuestWindow.Handle(npc, player, world.World);
        player.Sent.Clear();
        player.Windows[0].Clicked(Window.ButtonTypes.Back, npc.LoginID, 0, 0, player, world.World);
        Assert.Empty(player.Sent);

        player.Windows[0].Clicked(Window.ButtonTypes.Next, npc.LoginID, 0, 0, player, world.World);
        player.Windows[0].Clicked(Window.ButtonTypes.Next, npc.LoginID, 0, 0, player, world.World);
        Assert.Equal(1, player.Sent.Count(s => s.StartsWith("MKW")));
    }
}
