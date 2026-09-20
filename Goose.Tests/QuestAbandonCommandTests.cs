using Goose;
using Goose.Commands;
using Goose.Quests;
using Goose.Testing;
using Xunit;

namespace Goose.Tests;

public class QuestAbandonCommandTests
{
    private sealed class CapturingPlayer : Player
    {
        public CapturingPlayer() : base(0) { }
        public List<string> Sent { get; } = new();
        public override bool Send(string data) { this.Sent.Add(data); return true; }
    }

    private static (TestWorldFixture World, CapturingPlayer Player, CommandContext Ctx) Setup(params (int Id, string Name)[] quests)
    {
        var world = new TestWorldFixture();
        var player = new CapturingPlayer
        {
            Class = world.World.ClassHandler.GetClass(0)!,
        };
        foreach (var (id, name) in quests)
        {
            var quest = new Quest
            {
                Id = id,
                Name = name,
                Description = "desc",
                FailText = "fail",
                PassText = "pass",
            };
            player.QuestsStarted.Add(quest);
        }
        var ctx = new CommandContext(player, world.World, new CommandRegistry(), [], "");
        return (world, player, ctx);
    }

    [Fact]
    public void Execute_NoActiveQuests_SendsMessage()
    {
        var (_, player, ctx) = Setup();

        new QuestAbandonCommand().Execute(ctx);

        Assert.Empty(player.Windows);
        Assert.Contains(player.Sent, s => s.Contains("You have no active quests."));
    }

    [Fact]
    public void Execute_OpensOptionListWithActiveQuestNames()
    {
        var (_, player, ctx) = Setup((1, "Quest One"), (2, "Quest Two"));

        new QuestAbandonCommand().Execute(ctx);

        Assert.Single(player.Windows);
        var list = Assert.IsType<OptionListWindow>(player.Windows[0]);
        Assert.Equal("Abandon Quest", list.Title);
        Assert.Null(list.NPC);
        var lines = player.Sent.Where(s => s.StartsWith("WNF")).ToArray();
        Assert.Equal(2, lines.Length);
        Assert.Contains(lines, s => s.Contains("Quest One"));
        Assert.Contains(lines, s => s.Contains("Quest Two"));
    }

    [Fact]
    public void LineClick_OpensConfirmWindowForClickedQuest()
    {
        var (_, player, ctx) = Setup((1, "Quest One"), (2, "Quest Two"));

        new QuestAbandonCommand().Execute(ctx);
        player.Windows[0].LineClicked(1, 0, player, ctx.World);

        var confirm = Assert.IsType<AbandonConfirmWindow>(Assert.Single(player.Windows));
        Assert.Equal("Abandon Quest", confirm.Title);
        Assert.Contains(player.Sent, s => s.Contains("Quest Two"));
    }

    [Fact]
    public void Confirm_Next_RemovesTheQuestFromTheActiveListAndProgress()
    {
        var (_, player, ctx) = Setup((1, "Quest One"), (2, "Quest Two"));
        var questTwo = player.QuestsStarted[1];
        var requirement = new QuestRequirement { Id = 99, Quest = questTwo, Type = RequirementType.Kill, Value = 5, Value2 = 10 };
        questTwo.Requirements.Add(requirement);
        player.QuestProgress.Add(new QuestProgress { Requirement = requirement, Value = 4 });

        new QuestAbandonCommand().Execute(ctx);
        player.Windows[0].LineClicked(1, 0, player, ctx.World);
        var confirm = Assert.IsType<AbandonConfirmWindow>(Assert.Single(player.Windows));
        confirm.Clicked(Window.ButtonTypes.Next, 0, 0, 0, player, ctx.World);

        Assert.DoesNotContain(player.QuestsStarted, q => q.Id == questTwo.Id);
        Assert.Contains(player.QuestsStarted, q => q.Id == 1);
        Assert.Empty(player.QuestProgress);
        Assert.Empty(player.Windows);
        Assert.Contains(player.Sent, s => s.Contains("Abandoned quest: Quest Two"));
    }

    [Fact]
    public void Confirm_Close_KeepsTheQuestActive()
    {
        var (_, player, ctx) = Setup((1, "Quest One"));

        new QuestAbandonCommand().Execute(ctx);
        player.Windows[0].LineClicked(0, 0, player, ctx.World);
        var confirm = Assert.IsType<AbandonConfirmWindow>(Assert.Single(player.Windows));
        confirm.Clicked(Window.ButtonTypes.Close, 0, 0, 0, player, ctx.World);

        Assert.Contains(player.QuestsStarted, q => q.Id == 1);
        Assert.Empty(player.Windows);
        Assert.DoesNotContain(player.Sent, s => s.Contains("Abandoned quest"));
    }

    [Fact]
    public void Abandoning_a_repeatable_quest_allows_it_to_be_restarted_with_fresh_progress()
    {
        using var fixture = new QuestCompletionTests.Fixture(9, "Repeatable", 0, repeatable: true);
        var requirement = new QuestRequirement { Id = 99, Quest = fixture.Quest, Type = RequirementType.Kill, Value = 5, Value2 = 10 };
        fixture.Quest.Requirements.Add(requirement);
        fixture.Player.QuestsStarted.Add(fixture.Quest);
        fixture.Player.QuestProgress.Add(new QuestProgress { Requirement = requirement, Value = 4 });
        var ctx = new CommandContext(fixture.Player, fixture.World.World, new CommandRegistry(), [], "");

        new QuestAbandonCommand().Execute(ctx);
        fixture.Player.Windows[0].LineClicked(0, 0, fixture.Player, fixture.World.World);
        var confirm = Assert.IsType<AbandonConfirmWindow>(Assert.Single(fixture.Player.Windows));
        confirm.Clicked(Window.ButtonTypes.Next, 0, 0, 0, fixture.Player, fixture.World.World);

        Assert.DoesNotContain(fixture.Player.QuestsStarted, q => q.Id == fixture.Quest.Id);
        Assert.Empty(fixture.Player.QuestProgress);

        QuestWindow.StartQuest(fixture.Quest, fixture.Player);

        Assert.Contains(fixture.Player.QuestsStarted, q => q.Id == fixture.Quest.Id);
        var progress = Assert.Single(fixture.Player.QuestProgress);
        Assert.Equal(0, progress.Value);
    }

    [Fact]
    public void Confirming_after_the_quest_is_no_longer_active_closes_without_abandoning()
    {
        using var fixture = new QuestCompletionTests.Fixture(9, "Repeatable", 0, repeatable: true);
        fixture.Player.QuestsStarted.Add(fixture.Quest);
        var ctx = new CommandContext(fixture.Player, fixture.World.World, new CommandRegistry(), [], "");

        new QuestAbandonCommand().Execute(ctx);
        fixture.Player.Windows[0].LineClicked(0, 0, fixture.Player, fixture.World.World);
        var confirm = Assert.IsType<AbandonConfirmWindow>(Assert.Single(fixture.Player.Windows));

        fixture.Player.QuestsStarted.Remove(fixture.Quest);
        confirm.Clicked(Window.ButtonTypes.Next, 0, 0, 0, fixture.Player, fixture.World.World);

        Assert.Empty(fixture.Player.Windows);
        Assert.DoesNotContain(fixture.Player.Sent, s => s.Contains("Abandoned quest"));
    }

    [Fact]
    public void CompletedNonRepeatableQuest_IsExcludedFromTheList()
    {
        var (_, player, ctx) = Setup((1, "Quest One"), (2, "Quest Two"));
        player.QuestsCompleted.Add(player.QuestsStarted[1]);

        new QuestAbandonCommand().Execute(ctx);

        Assert.IsType<OptionListWindow>(Assert.Single(player.Windows));
        var lines = player.Sent.Where(s => s.StartsWith("WNF")).ToArray();
        Assert.Single(lines);
        Assert.Contains(lines, s => s.Contains("Quest One"));
        Assert.DoesNotContain(lines, s => s.Contains("Quest Two"));
    }
}
