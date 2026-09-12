using Goose;
using Goose.Quests;
using Goose.Scripting;
using Goose.Tests.Fixtures;

namespace Goose.Tests;

public class QuestWindowScriptTests
{
    /// <summary>
    /// Builds an NPC, player and Script-only quest wired to the given script handle. Test quests
    /// must contain only Script rows: a Gold or stat row makes CompleteQuest NRE inside
    /// Player.RemoveGold → MaxStats (Player(0) has no MaxStats). The script arms under test do
    /// not need built-in rows.
    /// </summary>
    private static (NPC npc, Player player, Quest quest) QuestFixture(
        Script<IQuestScript> script,
        GooseSettings settings,
        RequirementType? requirementType = null,
        RewardType? rewardType = null)
    {
        // The NPC needs a template: QuestWindow's ctor calls player.TalkedTo(npc, world), which
        // dereferences npc.NPCTemplate.NPCTemplateID.
        var npc = new NPC
        {
            NPCTemplate = new NPCTemplate { NPCTemplateID = 5, Name = "Quest NPC" },
        };

        // Player(0), not Player() — the parameterless ctor leaves the collections null.
        var player = new Player(0);
        // Player(0) does not initialize Inventory/Spellbook; Clicked checks both before
        // completing (PlayerHasEnoughInventorySpaceForReward / ...SpellbookSpaceForReward).
        player.Inventory = new Inventory(player, settings);
        player.Spellbook = new Spellbook(player, settings);

        var quest = new Quest
        {
            Id = 77,
            Name = "Scripted Quest",
            Description = "desc",
            FailText = "fail",
            PassText = "pass",
            // Player(0) has Level 0; MinLevel 0 keeps QuestWindow's ctor in QuestDescription.
            MinLevel = 0,
            MinExperience = 0,
            Repeatable = false,
            ShowProgress = true,
        };

        if (requirementType.HasValue)
        {
            quest.Requirements.Add(new QuestRequirement
            {
                Id = 99,
                Quest = quest,
                Type = requirementType.Value,
                Value = 1,
                Value2 = 1,
                Script = script,
            });
        }

        if (rewardType.HasValue)
        {
            quest.Rewards.Add(new QuestReward
            {
                Id = 100,
                Type = rewardType.Value,
                Script = script,
            });
        }

        return (npc, player, quest);
    }

    /// <summary>Fills inventory slots with unstackable loot, leaving <paramref name="leaveFreeSlots"/>
    /// free, so the engine's space check has nothing to hand over.</summary>
    private static void FillInventory(Player player, QuestScriptFixture fixture, int leaveFreeSlots = 0)
    {
        fixture.World.ItemHandler.AddTemplate(new ItemTemplate
        {
            ID = 900, Name = "Loot", Description = "Loot", StackSize = 1,
            BaseStats = new AttributeSet(), ScriptParams = "",
        });

        var template = fixture.World.ItemHandler.GetTemplate(900)!;
        for (var i = 0; i < fixture.Settings.InventorySize - leaveFreeSlots; i++)
        {
            var item = new Item();
            item.LoadFromTemplate(template);
            fixture.World.ItemHandler.AddAndAssignId(item, fixture.World);
            Assert.True(player.Inventory.AddItem(item, 1, fixture.World));
        }
    }

    [Fact]
    public void A_script_requirement_that_is_not_met_fails_the_quest()
    {
        using var scripts = new QuestScriptFixture();
        var script = scripts.Compile(@"
using Goose; using Goose.Quests; using Goose.Scripting;
public class T : BaseQuestScript
{
    public override bool IsMet(QuestRequirement requirement, Player player, GameWorld world)
        => false;
}
return typeof(T);
");
        var (npc, player, quest) = QuestFixture(script, scripts.Settings, requirementType: RequirementType.Script);

        var window = new QuestWindow(npc, player, quest, scripts.World);

        Assert.False(window.PlayerMeetsRequirements(player, scripts.World));
    }

    [Fact]
    public void A_script_requirement_that_is_met_passes_the_quest()
    {
        // Load-bearing: without the Script arm PlayerMeetsRequirements hits default: return
        // false, so this fails red until the arm exists. The "not met" test alone would pass
        // green-ishly for the wrong reason.
        using var scripts = new QuestScriptFixture();
        var script = scripts.Compile(@"
using Goose; using Goose.Quests; using Goose.Scripting;
public class T : BaseQuestScript
{
    public override bool IsMet(QuestRequirement requirement, Player player, GameWorld world)
        => true;
}
return typeof(T);
");
        var (npc, player, quest) = QuestFixture(script, scripts.Settings, requirementType: RequirementType.Script);

        var window = new QuestWindow(npc, player, quest, scripts.World);

        Assert.True(window.PlayerMeetsRequirements(player, scripts.World));
    }

    [Fact]
    public void Script_progress_text_is_appended_after_built_in_lines()
    {
        // The one test that mixes a built-in row with a scripted one: GetQuestProgressText only
        // formats strings (never calls RemoveGold), so a Gold row is safe here.
        using var scripts = new QuestScriptFixture();
        var script = scripts.Compile(@"
using Goose; using Goose.Quests; using Goose.Scripting;
public class T : BaseQuestScript
{
    public override string GetProgressText(QuestRequirement requirement, Player player, GameWorld world)
        => ""scripted line"";
}
return typeof(T);
");
        var (npc, player, quest) = QuestFixture(script, scripts.Settings, requirementType: RequirementType.Script);
        quest.Requirements.Add(new QuestRequirement
        {
            Id = 98,
            Quest = quest,
            Type = RequirementType.Gold,
            Value = 500,
        });

        var window = new QuestWindow(npc, player, quest, scripts.World);

        // OrderBy(r => r.Type) sorts Gold (0) before Script (7), so the scripted line is last.
        Assert.Equal("Requirements\\n\\n500 gp\\nscripted line\\n",
                     window.GetQuestProgressText(player, scripts.World));
    }

    [Fact]
    public void An_empty_script_progress_text_adds_no_line()
    {
        using var scripts = new QuestScriptFixture();
        var script = scripts.Compile(@"
using Goose; using Goose.Quests; using Goose.Scripting;
public class T : BaseQuestScript { }
return typeof(T);
");
        var (npc, player, quest) = QuestFixture(script, scripts.Settings, requirementType: RequirementType.Script);

        var window = new QuestWindow(npc, player, quest, scripts.World);

        // Base GetProgressText returns "" — nothing is appended to the built-in header.
        Assert.Equal("Requirements\\n\\n", window.GetQuestProgressText(player, scripts.World));
    }

    [Fact]
    public void A_script_reward_needing_a_slot_is_refused_by_the_engine_space_check()
    {
        // The gate must run before the quest completes, and must use the engine's own message:
        // GiveRewards ignores AddItem's false, so a reward that does not fit is gone for good
        // once CompleteQuest has run.
        using var scripts = new QuestScriptFixture();
        var script = scripts.Compile(@"
using Goose; using Goose.Quests; using Goose.Scripting;
public class T : BaseQuestScript
{
    public static bool GaveReward = false;
    public override int GetRequiredInventorySpace(QuestReward reward, Player player, GameWorld world)
        => 1;
    public override void GiveReward(QuestReward reward, NPC npc, Player player, GameWorld world)
        => GaveReward = true;
}
return typeof(T);
");
        var (npc, player, quest) = QuestFixture(script, scripts.Settings, rewardType: RewardType.Script);
        FillInventory(player, scripts);

        var window = new QuestWindow(npc, player, quest, scripts.World);
        window.Clicked(Window.ButtonTypes.Next, npc.NPCTemplate.NPCTemplateID, 0, 0, player, scripts.World);

        Assert.DoesNotContain(player.QuestsCompleted, q => q.Id == quest.Id);
        Assert.False((bool)script.Object.GetType().GetField("GaveReward")!.GetValue(null)!);
        Assert.Equal("You don't have enough inventory space to accept \\nthe reward.\\nDelete an item and try again.",
                     window.GetCurrentText(player, scripts.World));
    }

    [Fact]
    public void A_script_reward_needing_no_slots_completes_a_full_inventory()
    {
        using var scripts = new QuestScriptFixture();
        var script = scripts.Compile(@"
using Goose; using Goose.Quests; using Goose.Scripting;
public class T : BaseQuestScript { }
return typeof(T);
");
        var (npc, player, quest) = QuestFixture(script, scripts.Settings, rewardType: RewardType.Script);
        FillInventory(player, scripts);

        var window = new QuestWindow(npc, player, quest, scripts.World);
        window.Clicked(Window.ButtonTypes.Next, npc.NPCTemplate.NPCTemplateID, 0, 0, player, scripts.World);

        Assert.Contains(player.QuestsCompleted, q => q.Id == quest.Id);
    }

    [Fact]
    public void Script_and_item_reward_rows_add_up()
    {
        // One free slot, one Item row and one script row wanting a slot each: the engine's own
        // count alone would allow the turn-in and drop one of the two rewards.
        using var scripts = new QuestScriptFixture();
        var script = scripts.Compile(@"
using Goose; using Goose.Quests; using Goose.Scripting;
public class T : BaseQuestScript
{
    public override int GetRequiredInventorySpace(QuestReward reward, Player player, GameWorld world)
        => 1;
}
return typeof(T);
");
        var (npc, player, quest) = QuestFixture(script, scripts.Settings, rewardType: RewardType.Script);
        quest.Rewards.Add(new QuestReward { Id = 101, Type = RewardType.Item, LongValue = 4242 });
        FillInventory(player, scripts, leaveFreeSlots: 1);

        var window = new QuestWindow(npc, player, quest, scripts.World);
        window.Clicked(Window.ButtonTypes.Next, npc.NPCTemplate.NPCTemplateID, 0, 0, player, scripts.World);

        Assert.DoesNotContain(player.QuestsCompleted, q => q.Id == quest.Id);
    }

    [Fact]
    public void A_script_cannot_loosen_the_gate_with_a_negative_count()
    {
        using var scripts = new QuestScriptFixture();
        var script = scripts.Compile(@"
using Goose; using Goose.Quests; using Goose.Scripting;
public class T : BaseQuestScript
{
    public override int GetRequiredInventorySpace(QuestReward reward, Player player, GameWorld world)
        => -5;
}
return typeof(T);
");
        var (npc, player, quest) = QuestFixture(script, scripts.Settings, rewardType: RewardType.Script);
        quest.Rewards.Add(new QuestReward { Id = 101, Type = RewardType.Item, LongValue = 4242 });
        FillInventory(player, scripts);

        var window = new QuestWindow(npc, player, quest, scripts.World);
        window.Clicked(Window.ButtonTypes.Next, npc.NPCTemplate.NPCTemplateID, 0, 0, player, scripts.World);

        Assert.DoesNotContain(player.QuestsCompleted, q => q.Id == quest.Id);
    }

    [Fact]
    public void A_blocking_CanComplete_leaves_the_quest_uncompleted()
    {
        // Adversarial: this must execute Clicked. Calling the helper directly would pass even if
        // Clicked forgot the gate or called it after CompleteQuest.
        using var scripts = new QuestScriptFixture();
        var script = scripts.Compile(@"
using Goose; using Goose.Quests; using Goose.Scripting;
public class T : BaseQuestScript
{
    public static bool GaveReward = false;
    public override string CanComplete(QuestReward reward, Player player, GameWorld world)
        => ""No room in your pack."";
    public override void GiveReward(QuestReward reward, NPC npc, Player player, GameWorld world)
        => GaveReward = true;
}
return typeof(T);
");
        var (npc, player, quest) = QuestFixture(script, scripts.Settings, rewardType: RewardType.Script);
        var progress = new QuestProgress
        {
            Requirement = new QuestRequirement { Id = 99, Quest = quest }, Value = 1
        };
        player.QuestProgress.Add(progress);
        var window = new QuestWindow(npc, player, quest, scripts.World);

        window.Clicked(Window.ButtonTypes.Next, npc.NPCTemplate.NPCTemplateID, 0, 0,
                       player, scripts.World);

        Assert.DoesNotContain(player.QuestsCompleted, q => q.Id == quest.Id);
        Assert.Contains(progress, player.QuestProgress);                         // progress not removed
        Assert.False((bool)script.Object.GetType().GetField("GaveReward")!.GetValue(null)!);
        Assert.Equal("No room in your pack.", window.GetCurrentText(player, scripts.World));
    }

    [Fact]
    public void A_null_CanComplete_allows_completion()
    {
        using var scripts = new QuestScriptFixture();
        var script = scripts.Compile(@"
using Goose; using Goose.Quests; using Goose.Scripting;
public class T : BaseQuestScript { }
return typeof(T);
");
        var (npc, player, quest) = QuestFixture(script, scripts.Settings, rewardType: RewardType.Script);
        var window = new QuestWindow(npc, player, quest, scripts.World);

        window.Clicked(Window.ButtonTypes.Next, npc.NPCTemplate.NPCTemplateID, 0, 0,
                       player, scripts.World);

        Assert.Contains(player.QuestsCompleted, q => q.Id == quest.Id);
    }

    [Fact]
    public void An_empty_CanComplete_allows_completion()
    {
        using var scripts = new QuestScriptFixture();
        var script = scripts.Compile(@"
using Goose; using Goose.Quests; using Goose.Scripting;
public class T : BaseQuestScript
{
    public override string CanComplete(QuestReward reward, Player player, GameWorld world)
        => """";
}
return typeof(T);
");
        var (npc, player, quest) = QuestFixture(script, scripts.Settings, rewardType: RewardType.Script);
        var window = new QuestWindow(npc, player, quest, scripts.World);

        window.Clicked(Window.ButtonTypes.Next, npc.NPCTemplate.NPCTemplateID, 0, 0,
                       player, scripts.World);

        Assert.Contains(player.QuestsCompleted, q => q.Id == quest.Id);
    }

    [Fact]
    public void GiveReward_runs_on_completion()
    {
        using var scripts = new QuestScriptFixture();
        var script = scripts.Compile(@"
using Goose; using Goose.Quests; using Goose.Scripting;
public class T : BaseQuestScript
{
    public static bool GaveReward = false;
    public override void GiveReward(QuestReward reward, NPC npc, Player player, GameWorld world)
        => GaveReward = true;
}
return typeof(T);
");
        var (npc, player, quest) = QuestFixture(script, scripts.Settings, rewardType: RewardType.Script);

        new QuestWindow(npc, player, quest, scripts.World).CompleteQuest(npc, player, scripts.World);

        Assert.True((bool)script.Object.GetType().GetField("GaveReward")!.GetValue(null)!);
    }

    [Fact]
    public void OnTakeRequirement_runs_when_the_requirement_is_not_kept()
    {
        using var scripts = new QuestScriptFixture();
        var script = scripts.Compile(@"
using Goose; using Goose.Quests; using Goose.Scripting;
public class T : BaseQuestScript
{
    public static int TakeCalls = 0;
    public override void OnTakeRequirement(QuestRequirement requirement, Player player, GameWorld world)
        => TakeCalls++;
}
return typeof(T);
");
        var (npc, player, quest) = QuestFixture(script, scripts.Settings, requirementType: RequirementType.Script);

        new QuestWindow(npc, player, quest, scripts.World).CompleteQuest(npc, player, scripts.World);

        Assert.Equal(1, (int)script.Object.GetType().GetField("TakeCalls")!.GetValue(null)!);
    }

    [Fact]
    public void OnTakeRequirement_is_skipped_when_keep_requirement_is_set()
    {
        // keep_requirement is the configured way to say "consume nothing", so the hook must not be
        // called at all. An implementation that adds the arm outside the guard at :463 fails here.
        using var scripts = new QuestScriptFixture();
        var script = scripts.Compile(@"
using Goose; using Goose.Quests; using Goose.Scripting;
public class T : BaseQuestScript
{
    public static int TakeCalls = 0;
    public override void OnTakeRequirement(QuestRequirement requirement, Player player, GameWorld world)
        => TakeCalls++;
}
return typeof(T);
");
        var (npc, player, quest) = QuestFixture(script, scripts.Settings, requirementType: RequirementType.Script);
        quest.Requirements[0].KeepRequirement = true;

        new QuestWindow(npc, player, quest, scripts.World).CompleteQuest(npc, player, scripts.World);

        Assert.Equal(0, (int)script.Object.GetType().GetField("TakeCalls")!.GetValue(null)!);
    }
}
