using Goose;
using Goose.Quests;
using Goose.Testing;
using Xunit;

namespace Goose.Tests;

public class QuestWindowItemRequirementTests
{
    private const int BaseId = 100;
    private const int D1Id = 1001;
    private const int D2Id = 1002;

    private static (TestWorldFixture World, NPC Npc, TestWorldFixture.CapturingPlayer Player, Quest Quest) Setup(params QuestRequirement[] requirements)
    {
        var world = new TestWorldFixture();
        RegisterFamily(world);
        var npc = new NPC
        {
            NPCTemplate = new NPCTemplate { NPCTemplateID = 5, Name = "Quest NPC" },
        };
        var player = new TestWorldFixture.CapturingPlayer
        {
            BaseStats = new AttributeSet(),
            MaxStats = new AttributeSet(),
            Class = world.World.ClassHandler.GetClass(0)!,
        };
        player.Inventory = new Inventory(player, world.Settings);
        player.Spellbook = new Spellbook(player, world.Settings);

        var quest = new Quest
        {
            Id = 77,
            Name = "Item Quest",
            Description = "desc",
            FailText = "fail",
            PassText = "pass",
            MinLevel = 0,
            MinExperience = 0,
            Repeatable = false,
            ShowProgress = true,
        };
        foreach (var r in requirements)
        {
            r.Quest = quest;
            quest.Requirements.Add(r);
        }
        return (world, npc, player, quest);
    }

    private static void RegisterFamily(TestWorldFixture world)
    {
        RegisterTemplate(world, BaseId, "Herb");
        RegisterTemplate(world, D1Id, "Herb I", variantOf: BaseId, dimension: 1);
        RegisterTemplate(world, D2Id, "Herb II", variantOf: BaseId, dimension: 2);
    }

    private static ItemTemplate RegisterTemplate(TestWorldFixture world, int id, string name, int? variantOf = null, int dimension = 0)
    {
        var template = new ItemTemplate
        {
            ID = id, Name = name, Description = name,
            UseType = ItemTemplate.UseTypes.NoUse,
            StackSize = 10,
            BaseStats = new AttributeSet(),
            ScriptParams = "",
            VariantOfTemplateID = variantOf,
            Dimension = dimension,
        };
        world.World.ItemHandler.AddTemplate(template);
        return template;
    }

    private static void Give(Player player, TestWorldFixture world, int templateId, int stack, bool custom = false)
    {
        var item = new Item();
        item.LoadFromTemplate(world.World.ItemHandler.GetTemplate(templateId)!);
        if (custom) item.Description = "Custom created by GM";
        world.World.ItemHandler.AddAndAssignId(item, world.World);
        Assert.True(player.Inventory.AddItem(item, stack, world.World));
    }

    private static QuestRequirement ItemReq(int id, int templateId, long quantity, bool keep = false)
        => new() { Id = id, Type = RequirementType.Item, Value = templateId, Value2 = quantity, KeepRequirement = keep };

    [Fact]
    public void ProgressText_SumsDuplicateFamilyRowsIntoOneLine()
    {
        var (world, npc, player, quest) = Setup(
            ItemReq(1, BaseId, 3),
            ItemReq(2, D1Id, 4));
        Give(player, world, D2Id, 5);

        var window = new QuestWindow(npc, player, quest, world.World);
        string text = window.GetQuestProgressText(player, world.World);

        Assert.Contains("Herb (5/7)", text);
        Assert.Equal(1, text.Split("\\n").Count(l => l.StartsWith("Herb")));
    }

    [Fact]
    public void ProgressText_VariantOnlyCount_AgreesWithReadiness()
    {
        var (world, npc, player, quest) = Setup(ItemReq(1, BaseId, 4));
        Give(player, world, D1Id, 4);

        var window = new QuestWindow(npc, player, quest, world.World);
        string text = window.GetQuestProgressText(player, world.World);

        Assert.Contains("Herb (4/4)", text);
        Assert.True(QuestStateResolver.MeetsRequirements(quest, player, world.World));
    }

    [Fact]
    public void ProgressText_MissingTemplate_KeepsUnknownLine()
    {
        var (world, npc, player, quest) = Setup(ItemReq(1, 999, 5));

        var window = new QuestWindow(npc, player, quest, world.World);
        string text = window.GetQuestProgressText(player, world.World);

        Assert.Contains("Unknown item (0/5)", text);
    }

    [Fact]
    public void Completion_DimensionVariantSatisfiesDisplaysAndIsConsumedByBaseIdRequirement()
    {
        var (world, npc, player, quest) = Setup(ItemReq(1, BaseId, 3));
        Give(player, world, D2Id, 3);

        var window = new QuestWindow(npc, player, quest, world.World);
        Assert.Contains("Herb (3/3)", window.GetQuestProgressText(player, world.World));

        window.Clicked(Window.ButtonTypes.Next, npc.NPCTemplate.NPCTemplateID, 0, 0, player, world.World);

        Assert.Contains(player.QuestsCompleted, q => q.Id == quest.Id);
        Assert.Equal(0, player.Inventory.GetInventoryItemFamilyCount(BaseId));
        Assert.Null(player.Inventory.GetInventorySlots()[1]);
    }

    [Fact]
    public void Completion_KeptPlusConsumedRowsLeaveTheKeptQuantity()
    {
        var (world, npc, player, quest) = Setup(
            ItemReq(1, BaseId, 5, keep: true),
            ItemReq(2, D1Id, 3));
        Give(player, world, BaseId, 8);

        var window = new QuestWindow(npc, player, quest, world.World);
        window.Clicked(Window.ButtonTypes.Next, npc.NPCTemplate.NPCTemplateID, 0, 0, player, world.World);

        Assert.Contains(player.QuestsCompleted, q => q.Id == quest.Id);
        Assert.Equal(5, player.Inventory.GetInventoryItemFamilyCount(BaseId));
    }

    [Fact]
    public void Completion_DuplicateRowsCannotDoubleCountOneShortStack()
    {
        var (world, npc, player, quest) = Setup(
            ItemReq(1, BaseId, 5),
            ItemReq(2, D1Id, 5));
        Give(player, world, BaseId, 8);

        var window = new QuestWindow(npc, player, quest, world.World);
        window.Clicked(Window.ButtonTypes.Next, npc.NPCTemplate.NPCTemplateID, 0, 0, player, world.World);

        Assert.DoesNotContain(player.QuestsCompleted, q => q.Id == quest.Id);
        Assert.Equal(8, player.Inventory.GetInventoryItemFamilyCount(BaseId));
    }

    [Fact]
    public void Completion_CustomItemsNeitherSatisfyNorGetConsumed()
    {
        var (world, npc, player, quest) = Setup(ItemReq(1, BaseId, 3));
        Give(player, world, BaseId, 3, custom: true);

        var window = new QuestWindow(npc, player, quest, world.World);
        window.Clicked(Window.ButtonTypes.Next, npc.NPCTemplate.NPCTemplateID, 0, 0, player, world.World);

        Assert.DoesNotContain(player.QuestsCompleted, q => q.Id == quest.Id);
        Assert.Equal(3, player.Inventory.GetInventorySlots()[1]!.Stack);
        Assert.True(player.Inventory.GetInventorySlots()[1]!.Item.Custom);
    }

    [Fact]
    public void Completion_AbortsAtomicallyWhenTheRemovableFamilyIsShort()
    {
        var (world, npc, player, quest) = Setup(
            ItemReq(1, BaseId, 5, keep: true),
            ItemReq(2, D1Id, 4));
        quest.Rewards.Add(new QuestReward { Id = 100, Type = RewardType.Gold, LongValue = 100 });
        Give(player, world, BaseId, 6);

        var window = new QuestWindow(npc, player, quest, world.World);
        window.CompleteQuest(npc, player, world.World);

        Assert.DoesNotContain(player.QuestsCompleted, q => q.Id == quest.Id);
        Assert.Equal(0, player.Gold);
        Assert.Equal(6, player.Inventory.GetInventoryItemFamilyCount(BaseId));
        Assert.Equal(6, player.Inventory.GetInventorySlots()[1]!.Stack);
    }
}
