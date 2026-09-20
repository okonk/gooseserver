using Goose;
using Goose.Commands;
using Goose.Quests;
using Goose.Testing;

namespace Goose.Tests;

public class QuestCompletionTests
{
    internal sealed class Fixture : IDisposable
    {
        public TestWorldFixture World { get; }
        public NPC Npc { get; }
        public CapturingPlayer Player { get; }
        public Quest Quest { get; }

        public Fixture(int questId, string name, long classRestrictions, bool repeatable = false)
        {
            this.World = new TestWorldFixture();
            this.Npc = new NPC
            {
                NPCTemplate = new NPCTemplate { NPCTemplateID = 5, Name = "Trainer" },
            };
            this.Player = new CapturingPlayer
            {
                Level = 5,
                Class = this.World.World.ClassHandler.GetClass(0)!,
            };
            this.Player.Inventory = new Inventory(this.Player, this.World.Settings);
            this.Player.Spellbook = new Spellbook(this.Player, this.World.Settings);
            this.Quest = new Quest
            {
                Id = questId,
                Name = name,
                Description = "desc",
                FailText = "fail",
                PassText = "pass",
                MinLevel = 5,
                MaxLevel = 5,
                ClassRestrictions = classRestrictions,
                Repeatable = repeatable,
                ShowProgress = true,
            };
        }

        public void Dispose() => this.World.Dispose();
    }

    internal sealed class CapturingPlayer : Player
    {
        public CapturingPlayer() : base(0) { }
        public List<string> Sent { get; } = new();
        public override bool Send(string data) { this.Sent.Add(data); return true; }
    }

    private static void CompleteViaWindow(Fixture fixture)
    {
        var window = new QuestWindow(fixture.Npc, fixture.Player, fixture.Quest, fixture.World.World);
        window.Clicked(Window.ButtonTypes.Next, fixture.Npc.NPCTemplate.NPCTemplateID, 0, 0, fixture.Player, fixture.World.World);
    }

    [Fact]
    public void A_completed_repeatable_quest_is_removed_from_the_active_list()
    {
        using var fixture = new Fixture(9, "Repeatable", 0, repeatable: true);

        fixture.Player.QuestsStarted.Add(fixture.Quest);
        CompleteViaWindow(fixture);

        Assert.Contains(fixture.Player.QuestsCompleted, q => q.Id == fixture.Quest.Id);
        Assert.DoesNotContain(fixture.Player.QuestsStarted, q => q.Id == fixture.Quest.Id);
    }

    [Fact]
    public void Talking_to_the_npc_again_readds_a_completed_repeatable_quest()
    {
        using var fixture = new Fixture(9, "Repeatable", 0, repeatable: true);

        fixture.Player.QuestsStarted.Add(fixture.Quest);
        CompleteViaWindow(fixture);

        QuestWindow.StartQuest(fixture.Quest, fixture.Player);

        Assert.Contains(fixture.Player.QuestsStarted, q => q.Id == fixture.Quest.Id);
    }

    [Fact]
    public void A_completed_repeatable_quest_is_still_offered_by_its_npc()
    {
        using var fixture = new Fixture(9, "Repeatable", 0, repeatable: true);
        fixture.Npc.Quests = [fixture.Quest];
        fixture.Player.QuestsStarted.Add(fixture.Quest);
        CompleteViaWindow(fixture);

        var available = QuestWindow.GetAvailableQuests(fixture.Npc, fixture.Player);

        Assert.Contains(available, q => q.Id == fixture.Quest.Id);
    }

    [Fact]
    public void A_restarted_repeatable_quest_cannot_be_turned_in_again_without_redoing_its_progress()
    {
        using var fixture = new Fixture(9, "Repeatable", 0, repeatable: true);
        var requirement = new QuestRequirement { Id = 99, Quest = fixture.Quest, Type = RequirementType.Kill, Value = 5, Value2 = 10 };
        fixture.Quest.Requirements.Add(requirement);
        fixture.Player.QuestsStarted.Add(fixture.Quest);
        fixture.Player.QuestProgress.Add(new QuestProgress { Requirement = requirement, Value = 10 });

        CompleteViaWindow(fixture);
        QuestWindow.StartQuest(fixture.Quest, fixture.Player);

        var progress = Assert.Single(fixture.Player.QuestProgress);
        Assert.Equal(0, progress.Value);
        var window = new QuestWindow(fixture.Npc, fixture.Player, fixture.Quest, fixture.World.World);
        Assert.False(window.PlayerMeetsRequirements(fixture.Player, fixture.World.World));
    }

    [Fact]
    public void A_completed_non_class_restricted_quest_stays_in_the_active_list()
    {
        using var fixture = new Fixture(10, "Mouse Killer", 0);

        fixture.Player.QuestsStarted.Add(fixture.Quest);
        CompleteViaWindow(fixture);

        Assert.Contains(fixture.Player.QuestsCompleted, q => q.Id == fixture.Quest.Id);
        Assert.Contains(fixture.Player.QuestsStarted, q => q.Id == fixture.Quest.Id);
    }

    [Fact]
    public void A_turn_in_blocked_by_inventory_space_keeps_the_quest_active()
    {
        using var fixture = new Fixture(2, "Path of the Magus", 253);
        fixture.Quest.Rewards.Add(new QuestReward { Id = 100, Type = RewardType.Item, LongValue = 900 });
        fixture.World.World.ItemHandler.AddTemplate(new ItemTemplate
        {
            ID = 900, Name = "Loot", Description = "Loot", StackSize = 1,
            BaseStats = new AttributeSet(), ScriptParams = "",
        });
        var template = fixture.World.World.ItemHandler.GetTemplate(900)!;
        for (var i = 0; i < fixture.World.Settings.InventorySize; i++)
        {
            var item = new Item();
            item.LoadFromTemplate(template);
            fixture.World.World.ItemHandler.AddAndAssignId(item, fixture.World.World);
            Assert.True(fixture.Player.Inventory.AddItem(item, 1, fixture.World.World));
        }

        fixture.Player.QuestsStarted.Add(fixture.Quest);
        var window = new QuestWindow(fixture.Npc, fixture.Player, fixture.Quest, fixture.World.World);
        window.Clicked(Window.ButtonTypes.Next, fixture.Npc.NPCTemplate.NPCTemplateID, 0, 0, fixture.Player, fixture.World.World);

        Assert.DoesNotContain(fixture.Player.QuestsCompleted, q => q.Id == fixture.Quest.Id);
        Assert.Contains(fixture.Player.QuestsStarted, q => q.Id == fixture.Quest.Id);
    }

    [Fact]
    public void The_active_quest_list_excludes_completed_class_selection_quests()
    {
        using var fixture = new Fixture(4, "Path of the Priest", 253);
        fixture.Player.QuestsStarted.Add(fixture.Quest);
        fixture.Player.QuestsCompleted.Add(fixture.Quest);
        var ctx = new CommandContext(fixture.Player, fixture.World.World, new CommandRegistry(), [], "");

        new QuestsCommand().Execute(ctx);

        Assert.Empty(fixture.Player.Windows);
        Assert.Contains(fixture.Player.Sent, s => s.Contains("You have no active quests."));
    }
}
