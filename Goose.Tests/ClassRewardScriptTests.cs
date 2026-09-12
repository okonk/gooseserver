using Goose;
using Goose.Quests;
using Goose.Scripting;
using Goose.Testing;
using Goose.Tests.Fixtures;

namespace Goose.Tests;

public class ClassRewardScriptTests
{
    // Compiled once for the class rather than once per test: the script object holds no state
    // (every hook takes the world as an argument), and a Roslyn compile of the shipped script per
    // test measured ~70 ms, which was 1 s of the fast project's budget for the same file.
    private static readonly Lazy<IQuestScript> ShippedScript = new(CompileShippedScript);

    private static IQuestScript CompileShippedScript()
    {
        // The scratch fixture is only needed to reach a ScriptHandler; Script<T> reads and
        // compiles the file in its constructor (Script.cs:26), so the loaded object outlives it.
        using var scratch = new QuestScriptFixture();
        return scratch.Compile(
            File.ReadAllText(Path.Combine(IllutiaDataDirectory(), "Scripts", "Quest", "ClassReward.csx")),
            "ClassReward.csx").Object;
    }

    private static void RegisterItem(GameWorld world, int id, string name, int stackSize = 1) =>
        world.ItemHandler.AddTemplate(new ItemTemplate
        {
            ID = id, Name = name, Description = name, StackSize = stackSize,
            BaseStats = new AttributeSet(), ScriptParams = "",
        });

    private static Player PlayerOfClass(QuestScriptFixture fixture, int classId)
    {
        // Player(0), not Player() — the parameterless ctor leaves Inventory null.
        var player = new Player(0) { ClassID = classId };
        player.Inventory = new Inventory(player, fixture.Settings);
        return player;
    }

    private static QuestReward Reward(string scriptParams) =>
        new QuestReward { Id = 1, Type = RewardType.Script, ScriptParams = scriptParams };

    private static IEnumerable<ItemSlot> Items(Player player) =>
        player.Inventory.GetInventorySlots().Where(s => s is not null)!;

    private static ItemSlot GivenItem(Player player) => Assert.Single(Items(player));

    [Fact]
    public void Each_class_gets_the_item_mapped_to_it()
    {
        using var fixture = new QuestScriptFixture();
        var script = ShippedScript.Value;
        RegisterItem(fixture.World, 10, "Sword");
        RegisterItem(fixture.World, 11, "Dagger");
        RegisterItem(fixture.World, 25, "Staff");
        var reward = Reward("""{"1": 10, "2": 11, "3": 25, "4": 11}""");

        foreach (var (classId, itemId) in new[] { (1, 10), (2, 11), (3, 25), (4, 11) })
        {
            var player = PlayerOfClass(fixture, classId);

            Assert.Null(script.CanComplete(reward, player, fixture.World));
            script.GiveReward(reward, null!, player, fixture.World);

            // Two classes sharing an item id is the point of the mapping, not a mistake.
            Assert.Equal(itemId, GivenItem(player).Item.TemplateID);
            Assert.Equal(1, GivenItem(player).Stack);
        }
    }

    [Fact]
    public void A_class_that_is_not_a_key_gets_nothing()
    {
        using var fixture = new QuestScriptFixture();
        var script = ShippedScript.Value;
        RegisterItem(fixture.World, 10, "Sword");
        var reward = Reward("""{"1": 10}""");
        var player = PlayerOfClass(fixture, 5);

        Assert.Null(script.CanComplete(reward, player, fixture.World));

        script.GiveReward(reward, null!, player, fixture.World);

        Assert.Empty(Items(player));
    }

    [Fact]
    public void The_top_level_qty_applies_to_every_bare_item()
    {
        using var fixture = new QuestScriptFixture();
        var script = ShippedScript.Value;
        RegisterItem(fixture.World, 10, "Potion", stackSize: 10);
        RegisterItem(fixture.World, 11, "Elixir", stackSize: 10);
        // qty last, so the map cannot depend on the key order the author typed.
        var reward = Reward("""{"1": 10, "2": 11, "qty": 3}""");

        foreach (var (classId, itemId) in new[] { (1, 10), (2, 11) })
        {
            var player = PlayerOfClass(fixture, classId);
            script.GiveReward(reward, null!, player, fixture.World);

            var slot = GivenItem(player);
            Assert.Equal(itemId, slot.Item.TemplateID);
            Assert.Equal(3, slot.Stack);
        }
    }

    [Fact]
    public void The_object_form_overrides_qty_for_one_class()
    {
        using var fixture = new QuestScriptFixture();
        var script = ShippedScript.Value;
        RegisterItem(fixture.World, 11, "Potion", stackSize: 10);
        RegisterItem(fixture.World, 25, "Staff", stackSize: 10);
        var reward = Reward("""{"qty": 1, "2": 11, "3": {"item": 25, "qty": 5}}""");

        var warrior = PlayerOfClass(fixture, 2);
        script.GiveReward(reward, null!, warrior, fixture.World);
        var mage = PlayerOfClass(fixture, 3);
        script.GiveReward(reward, null!, mage, fixture.World);

        Assert.Equal(11, GivenItem(warrior).Item.TemplateID);
        Assert.Equal(1, GivenItem(warrior).Stack);
        Assert.Equal(25, GivenItem(mage).Item.TemplateID);
        Assert.Equal(5, GivenItem(mage).Stack);
    }

    [Fact]
    public void The_class_owed_an_item_reserves_one_slot()
    {
        using var fixture = new QuestScriptFixture();
        var script = ShippedScript.Value;
        RegisterItem(fixture.World, 10, "Sword");
        var reward = Reward("""{"1": 10, "qty": 9}""");

        // One slot however large the stack: AddItem never splits (Inventory.cs:78).
        Assert.Equal(1, script.GetRequiredInventorySpace(reward, PlayerOfClass(fixture, 1), fixture.World));
        // Nothing is handed over for a class that is not a key, so nothing is reserved.
        Assert.Equal(0, script.GetRequiredInventorySpace(reward, PlayerOfClass(fixture, 5), fixture.World));
    }

    [Fact]
    public void A_full_inventory_refuses_the_turn_in()
    {
        using var fixture = new QuestScriptFixture();
        var script = ShippedScript.Value;
        RegisterItem(fixture.World, 10, "Sword");
        RegisterItem(fixture.World, 99, "Loot");

        var npc = new NPC { NPCTemplate = new NPCTemplate { NPCTemplateID = 5, Name = "Quest NPC" } };
        var player = PlayerOfClass(fixture, 1);
        player.Spellbook = new Spellbook(player, fixture.Settings);
        var quest = new Quest
        {
            Id = 77, Name = "Class Reward", Description = "desc", FailText = "fail", PassText = "pass",
            MinLevel = 0, MinExperience = 0, Repeatable = false, ShowProgress = true,
        };
        quest.Rewards.Add(new QuestReward
        {
            Id = 100, Type = RewardType.Script, Script = ScriptStub.For(script),
            ScriptParams = """{"1": 10}""",
        });

        var filler = fixture.World.ItemHandler.GetTemplate(99)!;
        for (var i = 0; i < fixture.Settings.InventorySize; i++)
        {
            var item = new Item();
            item.LoadFromTemplate(filler);
            fixture.World.ItemHandler.AddAndAssignId(item, fixture.World);
            Assert.True(player.Inventory.AddItem(item, 1, fixture.World));
        }

        var window = new QuestWindow(npc, player, quest, fixture.World);
        window.Clicked(Window.ButtonTypes.Next, npc.NPCTemplate.NPCTemplateID, 0, 0, player, fixture.World);

        // The engine's own space gate, not the script's: the script reserves the slot through
        // GetRequiredInventorySpace and never sees the click.
        Assert.DoesNotContain(player.QuestsCompleted, q => q.Id == quest.Id);
        Assert.Contains("enough inventory space", window.GetCurrentText(player, fixture.World));
    }

    [Fact]
    public void A_reward_that_cannot_be_given_blocks_completion()
    {
        using var fixture = new QuestScriptFixture();
        var script = ShippedScript.Value;
        var player = PlayerOfClass(fixture, 1);
        var reward = Reward("""{"1": 999}""");

        Assert.NotNull(script.CanComplete(reward, player, fixture.World));
        // No item exists, so nothing is handed over and no slot may be reserved for it.
        Assert.Equal(0, script.GetRequiredInventorySpace(reward, player, fixture.World));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("""{"1": 10, "defualt": 11}""")]
    [InlineData("""{"1": {"qty": 2}}""")]
    [InlineData("""{"1": {"item": 10, "count": 2}}""")]
    [InlineData("""{"qty": 0, "1": 10}""")]
    [InlineData("""{"1": -10}""")]
    public void Malformed_params_block_completion_instead_of_completing_empty_handed(string scriptParams)
    {
        using var fixture = new QuestScriptFixture();
        var script = ShippedScript.Value;
        RegisterItem(fixture.World, 10, "Sword");
        var player = PlayerOfClass(fixture, 1);
        var reward = Reward(scriptParams);

        var message = script.CanComplete(reward, player, fixture.World);

        Assert.NotNull(message);
        // 0 keeps an unreadable row from blocking on space, so the player sees the
        // misconfiguration message instead of the engine's generic one.
        Assert.Equal(0, script.GetRequiredInventorySpace(reward, player, fixture.World));
        script.GiveReward(reward, null!, player, fixture.World);
        Assert.Empty(Items(player));
    }

    [Fact]
    public void The_reward_message_matches_the_built_in_item_reward()
    {
        using var fixture = new QuestScriptFixture();
        var script = ShippedScript.Value;
        RegisterItem(fixture.World, 10, "Sword", stackSize: 5);
        var player = new TestWorldFixture.CapturingPlayer { ClassID = 1 };
        player.Inventory = new Inventory(player, fixture.Settings);

        script.GiveReward(Reward("""{"1": 10, "qty": 2}"""), null!, player, fixture.World);

        Assert.Contains(player.Sent, s => s.Contains("[Quest Reward]: Item: Sword (2)"));
    }

    private static string IllutiaDataDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            foreach (var candidate in new[]
            {
                Path.Combine(dir.FullName, "Data", "Illutia"),
                Path.Combine(dir.FullName, "Goose", "Data", "Illutia"),
            })
            {
                if (Directory.Exists(candidate)) return candidate;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException(
            "Could not locate Data/Illutia from " + AppContext.BaseDirectory);
    }
}
