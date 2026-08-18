using System.IO;
using System.Linq;
using Goose;
using Goose.Quests;
using Goose.Scripting;
using Goose.Tests.Collections;
using Goose.Tests.Fixtures;
using Xunit;

namespace Goose.Tests;

/// <summary>In GameWorldSettingsCollection: GlobalScriptFixture swaps the static
/// GameWorld.Settings, and Task 4's reward test writes
/// ChangeClassExperienceLossPercent. Every other fixture-based dimension suite is in this
/// collection for the same reason (DimensionsScriptTests.cs:7).</summary>
[Collection(GameWorldSettingsCollection.Name)]
public class DimensionRebirthTests
{
    private const int RebirthTemplateId = 810000;
    private const int RebirthQuestId = 910000;
    private const int RebirthX = 44;
    private const int RebirthY = 50;

    /// <summary>DimensionsScriptTests' world seeding (`DimensionsScriptTests.cs:20-25`),
    /// factored out because every test here needs it: a base map wide enough for the
    /// warden at (43,50) and the keeper at (44,50), and the boss template the unlock chain
    /// requires.</summary>
    private static GlobalScriptFixture Seeded()
    {
        var fixture = new GlobalScriptFixture();
        fixture.AddBaseMap(1, "Town", width: 100, height: 100);

        var boss = new NPCTemplate { NPCTemplateID = 162, Name = "Shadow Dog", Level = 40 };
        boss.BaseStats = new AttributeSet { HP = 3704 };
        fixture.World.NPCHandler.AddTemplate(boss);

        return fixture;
    }

    [Fact]
    public void Creates_the_rebirth_npc_template_and_quest()
    {
        using var fixture = Seeded();
        var script = fixture.CompileShipped();
        script.Object.OnLoaded(fixture.World);

        var template = fixture.World.NPCHandler.GetNPCTemplate(RebirthTemplateId);
        Assert.NotNull(template);
        Assert.False(template.CanBeKilled);
        Assert.False(template.CanMove);
        Assert.Equal(NPCTemplate.Types.Quest, template.NPCType);

        var quest = fixture.World.QuestHandler.Get(RebirthQuestId);
        Assert.NotNull(quest);
        Assert.True(quest.Repeatable);
        Assert.Contains(quest, template.Quests);
    }

    [Fact]
    public void Rebirth_quest_carries_a_nothing_equipped_and_a_script_requirement()
    {
        using var fixture = Seeded();
        var script = fixture.CompileShipped();
        script.Object.OnLoaded(fixture.World);

        var quest = fixture.World.QuestHandler.Get(RebirthQuestId);

        Assert.Contains(quest.Requirements, r => r.Type == RequirementType.NothingEquipped);

        var scripted = Assert.Single(quest.Requirements, r => r.Type == RequirementType.Script);
        Assert.Equal(RebirthQuestId + 2, scripted.Id);
        // Load-bearing: QuestWindow runs TakeRequirements before GiveRewards
        // (QuestWindow.cs:341-342), so a consuming requirement would zero the experience
        // the reward has to read.
        Assert.True(scripted.KeepRequirement);
    }

    [Fact]
    public void Rebirth_reward_is_a_script_reward_carrying_the_rate()
    {
        using var fixture = Seeded();
        var script = fixture.CompileShipped();
        script.Object.OnLoaded(fixture.World);

        var reward = Assert.Single(fixture.World.QuestHandler.Get(RebirthQuestId).Rewards);
        Assert.Equal(RewardType.Script, reward.Type);
        Assert.Equal(RebirthQuestId + 11, reward.Id);
        Assert.Equal("100000000", reward.ScriptParams);
    }

    /// <summary>Exactly one, in dimension 0. Rebirth strips you naked and drops you to
    /// level 1, and every dimension above 0 has CanPVP forced on.</summary>
    [Fact]
    public void Only_dimension_zero_gets_a_rebirth_npc()
    {
        using var fixture = Seeded();
        var script = fixture.CompileShipped();
        script.Object.OnLoaded(fixture.World);

        for (int dim = 1; dim <= 6; dim++)
            Assert.Null(fixture.World.NPCHandler.GetNPCTemplate(RebirthTemplateId + 100000 * dim));
    }

    /// <summary>Map.SetCharacter returns silently on an out-of-range coordinate
    /// (Map.cs:643-648), so a keeper placed off the map would be registered, listed in
    /// Map.NPCs, and invisible. Assert both halves: listed AND holding the tile.</summary>
    [Fact]
    public void Rebirth_keeper_is_spawned_on_dimension_zero_and_holds_its_tile()
    {
        using var fixture = Seeded();
        var script = fixture.CompileShipped();
        script.Object.OnLoaded(fixture.World);

        var map = fixture.World.MapHandler.GetMap(1);
        var keeper = Assert.Single(map.NPCs, n => n.NPCTemplateID == RebirthTemplateId);

        Assert.Same(keeper, map.GetCharacterAt(RebirthX, RebirthY));
        Assert.Equal(RebirthX, keeper.MapX);
        Assert.Equal(RebirthY, keeper.MapY);
        // Load-bearing: the warden already stands on (43,50), so the keeper must not be
        // configured onto an occupied tile.
        Assert.NotEqual(keeper, map.GetCharacterAt(43, 50));
    }

    /// <summary>The preflight, not the symptom. An occupied or blocked destination must
    /// stop the load rather than produce an NPC nobody can see.</summary>
    [Fact]
    public void Refuses_to_load_when_the_keepers_tile_is_blocked()
    {
        using var fixture = Seeded();
        var map = fixture.World.MapHandler.GetMap(1);
        map.tiles[RebirthY * map.Width + RebirthX] = new BlockedTile();

        var script = fixture.CompileShipped();

        var ex = Assert.Throws<Exception>(() => script.Object.OnLoaded(fixture.World));
        Assert.Contains("Rebirth keeper cannot stand", ex.Message);
    }

    /// <summary>Rebirth changes the player to class 1 level 1. A dataset without that
    /// class_info row must fail at load, not halfway through a completed quest.</summary>
    [Fact]
    public void Refuses_to_load_when_the_destination_class_has_no_level_one()
    {
        using var fixture = Seeded();
        fixture.RemoveClassLevel(1, 1);

        var script = fixture.CompileShipped();

        var ex = Assert.Throws<Exception>(() => script.Object.OnLoaded(fixture.World));
        Assert.Contains("no level 1 row in class_info", ex.Message);
    }

    [Fact]
    public void Disabled_creates_no_rebirth_npc_or_quest()
    {
        using var fixture = Seeded();
        // DimensionsScriptTests.cs:27-32 — read the shipped source, flip the Enabled
        // literal, assert the replacement actually changed something, then CompileSource.
        var source = File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "DimensionScripts", "Dimensions.csx"));
        var disabled = source.Replace("public const bool Enabled = true;",
                                      "public const bool Enabled = false;");
        Assert.NotEqual(source, disabled);

        fixture.CompileSource(disabled, "DimensionsDisabled.csx").Object.OnLoaded(fixture.World);

        Assert.Null(fixture.World.NPCHandler.GetNPCTemplate(RebirthTemplateId));
        Assert.Null(fixture.World.QuestHandler.Get(RebirthQuestId));
        Assert.Empty(fixture.World.MapHandler.GetMap(1).NPCs);
    }

    [Fact]
    public void IsMet_only_at_or_above_the_threshold()
    {
        using var fixture = Seeded();
        var script = fixture.CompileShipped();
        script.Object.OnLoaded(fixture.World);

        var requirement = fixture.World.QuestHandler.Get(RebirthQuestId)
            .Requirements.Single(r => r.Type == RequirementType.Script);

        var map = fixture.World.MapHandler.GetMap(1);
        var player = fixture.PlayerOn(map, 1, 1);

        player.Experience = 99_999_999; player.ExperienceSold = 0;
        Assert.False(requirement.Script.Object.IsMet(requirement, player, fixture.World));

        player.Experience = 100_000_000;
        Assert.True(requirement.Script.Object.IsMet(requirement, player, fixture.World));

        // Split across both fields — the threshold is on the sum, as every other
        // experience gate in the codebase is (Map.cs:638, QuestWindow.cs:36).
        player.Experience = 50_000_000; player.ExperienceSold = 50_000_000;
        Assert.True(requirement.Script.Object.IsMet(requirement, player, fixture.World));
    }

    [Fact]
    public void GiveReward_mints_floor_of_total_and_resets_the_character()
    {
        using var fixture = Seeded();
        GameWorld.Settings.ChangeClassExperienceLossPercent = 0.07;
        var script = fixture.CompileShipped();
        script.Object.OnLoaded(fixture.World);

        var reward = fixture.World.QuestHandler.Get(RebirthQuestId).Rewards.Single();
        var map = fixture.World.MapHandler.GetMap(1);
        var player = fixture.PlayerOn(map, 1, 1);
        player.ClassID = 3;
        player.Class = fixture.World.ClassHandler.GetClass(3);
        player.Level = 50;
        player.Experience = 250_000_000;
        player.ExperienceSold = 0;
        // PlayerOn builds the player via Player(int), which skips the Spellbook login
        // gives (created at login); ChangeClass touches it. Same fix Task 1's tests use.
        player.Spellbook = new Spellbook(player);

        reward.Script.Object.GiveReward(reward, null, player, fixture.World);

        var spirit = fixture.World.CurrencyHandler.Get("spirit");
        Assert.Equal(2, spirit.GetBalance(player));       // floor(250M / 100M)
        Assert.Equal(0, player.Experience);               // remainder destroyed
        Assert.Equal(0, player.ExperienceSold);           // and no 7% shave to observe
        Assert.Equal(1, player.Level);
        Assert.Equal(1, player.ClassID);
    }

    /// <summary>Enabled = false leaves spirit unregistered. Resetting the player and
    /// minting nothing would be strictly worse than refusing.</summary>
    [Fact]
    public void CanComplete_refuses_when_spirit_is_not_registered()
    {
        using var fixture = new GlobalScriptFixture();

        // InstallShippedScripts FIRST. GetScript compiles from disk immediately
        // (Script.cs:26 -> LoadScript), so resolving the path before the copy throws
        // FileNotFoundException and the test never reaches its assertion.
        //
        // And deliberately no OnLoaded: this is the Enabled = false world, where
        // SpiritCurrency was never registered. Rebirth.csx must refuse rather than reset
        // the character and mint nothing.
        fixture.InstallShippedScripts();
        var script = fixture.World.ScriptHandler.GetScript<IQuestScript>("Scripts/Global/Dimensions/Rebirth.csx");

        var reward = new QuestReward
        {
            Type = RewardType.Script,
            ScriptParams = "100000000",
            Script = script,
        };

        var map = fixture.AddBaseMap(9200, "No Currency Map");
        var player = fixture.PlayerOn(map, 1, 1);
        player.Experience = 500_000_000;

        var message = reward.Script.Object.CanComplete(reward, player, fixture.World);

        Assert.False(string.IsNullOrEmpty(message));
        Assert.Null(fixture.World.CurrencyHandler.Get("spirit"));
    }
}
