using Goose;
using Goose.Commands;
using Goose.Quests;
using Goose.Testing;
using Xunit;

namespace Goose.Tests;

public class QuestsCommandTests
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

    private static (TestWorldFixture World, CapturingPlayer Player, CommandContext Ctx) Setup(params (int Id, string Name)[] quests)
    {
        var world = new TestWorldFixture();
        var player = new CapturingPlayer
        {
            Class = world.World.ClassHandler.GetClass(0)!,
        };
        foreach (var (id, name) in quests)
            player.QuestsStarted.Add(MakeQuest(id, name));
        var ctx = new CommandContext(player, world.World, new CommandRegistry(), [], "");
        return (world, player, ctx);
    }

    [Fact]
    public void Execute_NoActiveQuests_SendsMessage()
    {
        var (_, player, ctx) = Setup();

        new QuestsCommand().Execute(ctx);

        Assert.Empty(player.Windows);
        Assert.Contains(player.Sent, s => s.Contains("You have no active quests."));
    }

    [Fact]
    public void Execute_SingleActiveQuest_OpensInfoWindowDirectly()
    {
        var (_, player, ctx) = Setup((1, "Quest One"));

        new QuestsCommand().Execute(ctx);

        Assert.Single(player.Windows);
        Assert.IsType<QuestInfoWindow>(player.Windows[0]);
    }

    [Fact]
    public void Execute_MultipleActiveQuests_OpensOptionListWithGrantingNpc()
    {
        var (world, player, ctx) = Setup((1, "Quest One"), (2, "Quest Two"));
        world.World.NPCHandler.AddTemplate(new NPCTemplate
        {
            NPCTemplateID = 5,
            Name = "Giver",
            BaseStats = new AttributeSet(),
            Quests = [player.QuestsStarted[0]],
        });

        new QuestsCommand().Execute(ctx);

        Assert.Single(player.Windows);
        var list = Assert.IsType<OptionListWindow>(player.Windows[0]);
        Assert.Null(list.NPC);
        Assert.Equal(["WNF1001,1,Quest One (Giver)|0|0|0|0|*", "WNF1001,2,Quest Two|0|0|0|0|*"],
            player.Sent.Where(s => s.StartsWith("WNF")).ToArray());
    }

    [Fact]
    public void Execute_CompletedNonRepeatableQuest_IsExcluded()
    {
        var (_, player, ctx) = Setup((1, "Quest One"), (2, "Quest Two"));
        player.QuestsCompleted.Add(player.QuestsStarted[1]);

        new QuestsCommand().Execute(ctx);

        Assert.Single(player.Windows);
        Assert.IsType<QuestInfoWindow>(player.Windows[0]);
    }

    [Fact]
    public void Execute_RepeatedRun_ClosesPreviousListAndInfoWindows()
    {
        var (_, player, ctx) = Setup((1, "Quest One"), (2, "Quest Two"));

        new QuestsCommand().Execute(ctx);
        new QuestsCommand().Execute(ctx);

        Assert.Single(player.Windows);
        Assert.IsType<OptionListWindow>(player.Windows[0]);
        Assert.Contains(player.Sent, s => s.StartsWith("CLW1001"));
    }

    [Fact]
    public void LineClick_OpensInfoWindowForClickedQuest()
    {
        var (_, player, ctx) = Setup((1, "Quest One"), (2, "Quest Two"));

        new QuestsCommand().Execute(ctx);
        player.Windows[0].LineClicked(1, 0, player, ctx.World);

        Assert.Single(player.Windows);
        Assert.IsType<QuestInfoWindow>(player.Windows[0]);
    }

    [Fact]
    public void BackButton_ReopensQuestListOnSamePage()
    {
        var quests = Enumerable.Range(1, Window.LineClickCount + 4).Select(i => (i, $"Quest {i}")).ToArray();
        var (_, player, ctx) = Setup(quests);

        new QuestsCommand().Execute(ctx);
        player.Windows[0].Clicked(Window.ButtonTypes.Next, 0, 0, 0, player, ctx.World);
        player.Windows[0].LineClicked(0, 0, player, ctx.World);

        var info = Assert.IsType<QuestInfoWindow>(player.Windows[0]);
        Assert.Equal("0,1,1,1,0", info.Buttons);

        player.Sent.Clear();
        info.Clicked(Window.ButtonTypes.Back, 0, 0, 0, player, ctx.World);

        Assert.Contains(player.Sent, s => s.StartsWith($"CLW{info.ID}"));
        Assert.Single(player.Windows);
        var list = Assert.IsType<OptionListWindow>(player.Windows[0]);
        Assert.Equal(1, list.Page);
        var lines = player.Sent.Where(s => s.StartsWith("WNF")).ToArray();
        Assert.Equal(4, lines.Length);
        Assert.Contains(lines, s => s.Contains($"Quest {Window.LineClickCount + 1}"));
        Assert.Contains(lines, s => s.Contains($"Quest {Window.LineClickCount + 4}"));
    }

    [Fact]
    public void Execute_MoreThanEightActiveQuests_PagesAndOpensCorrectQuest()
    {
        var quests = Enumerable.Range(1, Window.LineClickCount + 4).Select(i => (i, $"Quest {i}")).ToArray();
        var (_, player, ctx) = Setup(quests);

        new QuestsCommand().Execute(ctx);
        var lines = player.Sent.Where(s => s.StartsWith("WNF")).ToArray();
        Assert.Equal(Window.LineClickCount, lines.Length);

        player.Sent.Clear();
        player.Windows[0].Clicked(Window.ButtonTypes.Next, 0, 0, 0, player, ctx.World);
        lines = player.Sent.Where(s => s.StartsWith("WNF")).ToArray();
        Assert.Equal(4, lines.Length);

        player.Windows[0].LineClicked(1, 0, player, ctx.World);

        Assert.Single(player.Windows);
        Assert.IsType<QuestInfoWindow>(player.Windows[0]);
    }

    [Fact]
    public void InfoWindow_ShowsDescriptionThenRequirementsWithoutTurnIn()
    {
        var (world, player, ctx) = Setup((1, "Quest One"));
        var quest = player.QuestsStarted[0];
        quest.Requirements.Add(new QuestRequirement { Id = 99, Quest = quest, Type = RequirementType.Gold, Value = 100 });

        new QuestsCommand().Execute(ctx);
        var info = player.Windows[0];

        Assert.Contains(player.Sent, s => s.Contains("desc"));
        player.Sent.Clear();

        info.Clicked(Window.ButtonTypes.Next, 0, 0, 0, player, world.World);
        Assert.Contains(player.Sent, s => s.Contains("Requirements"));
        Assert.Contains(player.Sent, s => s.Contains("100 gp"));
        Assert.Equal("0,1,0,0,0", info.Buttons);
        player.Sent.Clear();

        info.Clicked(Window.ButtonTypes.Next, 0, 0, 0, player, world.World);
        Assert.Single(player.Windows);
        Assert.Equal("0,1,0,0,0", info.Buttons);

        info.Clicked(Window.ButtonTypes.Close, 0, 0, 0, player, world.World);
        Assert.Empty(player.Windows);
    }
}
