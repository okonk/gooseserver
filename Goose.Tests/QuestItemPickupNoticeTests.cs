using Goose;
using Goose.Events;
using Goose.Quests;
using Goose.Testing;
using Xunit;

namespace Goose.Tests;

public class QuestItemPickupNoticeTests
{
    private const int BaseId = 100;
    private const int VariantId = 1001;
    private const int UnrelatedId = 200;

    private sealed class Harness : IDisposable
    {
        public TestWorldFixture World = null!;
        public TestWorldFixture.CapturingPlayer Player = null!;
        public Map Map = null!;

        public void Dispose() => this.World.Dispose();
    }

    private static Harness Setup(Action<GooseSettings>? configure = null)
    {
        var world = new TestWorldFixture(s =>
        {
            s.ItemIDStartpoint = 5002;
            configure?.Invoke(s);
        });
        RegisterTemplate(world, BaseId, "Herb");
        RegisterTemplate(world, VariantId, "Herb I", variantOf: BaseId);
        RegisterTemplate(world, UnrelatedId, "Rock");
        // ItemHandler.CurrentID starts at ItemIDStartpoint, the gold item's id; consume it
        // so picked-up test items are never routed down PickupItemEvent's gold branch.
        var gold = new Item();
        world.World.ItemHandler.AddAndAssignId(gold, world.World);
        var map = world.AddBaseMap(1, "Test", 10, 10);
        var player = world.CommandPlayerOn(map, 5, 5);
        return new Harness { World = world, Player = player, Map = map };
    }

    private static ItemTemplate RegisterTemplate(TestWorldFixture world, int id, string name, int? variantOf = null)
    {
        var template = new ItemTemplate
        {
            ID = id, Name = name, Description = name,
            UseType = ItemTemplate.UseTypes.NoUse,
            StackSize = 10,
            BaseStats = new AttributeSet(),
            ScriptParams = "",
            VariantOfTemplateID = variantOf,
        };
        world.World.ItemHandler.AddTemplate(template);
        return template;
    }

    private static Quest HerbQuest(int id, string name, params (int TemplateId, long Quantity)[] rows)
    {
        var quest = new Quest
        {
            Id = id,
            Name = name,
            Description = "desc",
            FailText = "fail",
            PassText = "pass",
            MinLevel = 0,
            MinExperience = 0,
            Repeatable = false,
            ShowProgress = true,
        };
        foreach (var (templateId, quantity) in rows)
        {
            quest.Requirements.Add(new QuestRequirement
            {
                Id = quest.Requirements.Count + 1,
                Type = RequirementType.Item,
                Value = templateId,
                Value2 = quantity,
                Quest = quest,
            });
        }
        return quest;
    }

    private static Item Give(Harness setup, int templateId, int stack)
    {
        var item = new Item();
        item.LoadFromTemplate(setup.World.World.ItemHandler.GetTemplate(templateId)!);
        setup.World.World.ItemHandler.AddAndAssignId(item, setup.World.World);
        Assert.True(setup.Player.Inventory.AddItem(item, stack, setup.World.World));
        return item;
    }

    private static void Pickup(Harness setup, int templateId, int stack)
    {
        var item = new Item();
        item.LoadFromTemplate(setup.World.World.ItemHandler.GetTemplate(templateId)!);
        setup.World.World.ItemHandler.AddAndAssignId(item, setup.World.World);
        setup.Map.SetTile(5, 5, new ItemTile
        {
            X = 5, Y = 5,
            ItemSlot = new ItemSlot { Item = item, Stack = stack },
            PickupTime = 0,
        });
        new PickupItemEvent { Player = setup.Player }.Ready(setup.World.World);
    }

    private static List<string> Notices(TestWorldFixture.CapturingPlayer player)
        => player.Sent.Where(s => s.StartsWith("NTF1,")).ToList();

    [Fact]
    public void Pickup_ActiveQuest_SendsCreditWithNewCount()
    {
        using var setup = Setup();
        Give(setup, BaseId, 2);
        setup.Player.QuestsStarted.Add(HerbQuest(1, "Herb Quest", (BaseId, 5)));

        Pickup(setup, BaseId, 1);

        var notices = Notices(setup.Player);
        Assert.Single(notices);
        Assert.StartsWith("NTF1,Quest Credit: Herb Quest (3/5)", notices[0]);
    }

    [Fact]
    public void Pickup_VariantItem_CountsTowardFamily()
    {
        using var setup = Setup();
        setup.Player.QuestsStarted.Add(HerbQuest(1, "Herb Quest", (BaseId, 5)));

        Pickup(setup, VariantId, 2);

        var notices = Notices(setup.Player);
        Assert.Single(notices);
        Assert.StartsWith("NTF1,Quest Credit: Herb Quest (2/5)", notices[0]);
    }

    [Fact]
    public void Pickup_OverRequirement_ShowsOverCount()
    {
        using var setup = Setup();
        Give(setup, BaseId, 5);
        setup.Player.QuestsStarted.Add(HerbQuest(1, "Herb Quest", (BaseId, 5)));

        Pickup(setup, BaseId, 1);

        var notices = Notices(setup.Player);
        Assert.Single(notices);
        Assert.StartsWith("NTF1,Quest Credit: Herb Quest (6/5)", notices[0]);
    }

    [Fact]
    public void Pickup_DuplicateRequirementRows_SumIntoOneLine()
    {
        using var setup = Setup();
        setup.Player.QuestsStarted.Add(HerbQuest(1, "Herb Quest", (BaseId, 2), (BaseId, 3)));

        Pickup(setup, BaseId, 1);

        var notices = Notices(setup.Player);
        Assert.Single(notices);
        Assert.StartsWith("NTF1,Quest Credit: Herb Quest (1/5)", notices[0]);
    }

    [Fact]
    public void Pickup_QuestNotStarted_SendsNothing()
    {
        using var setup = Setup();
        HerbQuest(1, "Herb Quest", (BaseId, 5));

        Pickup(setup, BaseId, 1);

        Assert.Empty(Notices(setup.Player));
    }

    [Fact]
    public void Pickup_CompletedNonRepeatableQuest_SendsNothing()
    {
        using var setup = Setup();
        var quest = HerbQuest(1, "Herb Quest", (BaseId, 5));
        setup.Player.QuestsStarted.Add(quest);
        setup.Player.QuestsCompleted.Add(quest);

        Pickup(setup, BaseId, 1);

        Assert.Empty(Notices(setup.Player));
    }

    [Fact]
    public void Pickup_CreditFilterEnabled_SendsNothingButStillPicksUp()
    {
        using var setup = Setup();
        setup.Player.ToggleSettings |= Player.ToggleSetting.QuestCredit;
        setup.Player.QuestsStarted.Add(HerbQuest(1, "Herb Quest", (BaseId, 5)));

        Pickup(setup, BaseId, 1);

        Assert.Empty(Notices(setup.Player));
        Assert.Equal(1, setup.Player.Inventory.GetInventoryItemFamilyCount(BaseId));
    }

    [Fact]
    public void Pickup_UnrelatedItem_SendsNothing()
    {
        using var setup = Setup();
        setup.Player.QuestsStarted.Add(HerbQuest(1, "Herb Quest", (BaseId, 5)));

        Pickup(setup, UnrelatedId, 1);

        Assert.Empty(Notices(setup.Player));
    }

    [Fact]
    public void Pickup_TwoQuestsShareFamily_SendsOneLineEach()
    {
        using var setup = Setup();
        setup.Player.QuestsStarted.Add(HerbQuest(1, "Herb Quest A", (BaseId, 5)));
        setup.Player.QuestsStarted.Add(HerbQuest(2, "Herb Quest B", (BaseId, 5)));

        Pickup(setup, BaseId, 1);

        var notices = Notices(setup.Player);
        Assert.Equal(2, notices.Count);
        Assert.Contains(notices, s => s.StartsWith("NTF1,Quest Credit: Herb Quest A (1/5)"));
        Assert.Contains(notices, s => s.StartsWith("NTF1,Quest Credit: Herb Quest B (1/5)"));
    }

    [Fact]
    public void Pickup_InventoryFull_SendsNothing()
    {
        using var setup = Setup(w => w.InventorySize = 1);
        Give(setup, UnrelatedId, 1);
        setup.Player.QuestsStarted.Add(HerbQuest(1, "Herb Quest", (BaseId, 5)));

        Pickup(setup, BaseId, 1);

        Assert.Empty(Notices(setup.Player));
        Assert.Equal(0, setup.Player.Inventory.GetInventoryItemFamilyCount(BaseId));
    }
}
