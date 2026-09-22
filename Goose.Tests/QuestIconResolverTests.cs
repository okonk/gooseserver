using System.Net;
using System.Net.Sockets;
using Goose;
using Goose.Quests;
using Goose.Scripting;
using Goose.Testing;
using Goose.Tests.Fixtures;
using Xunit;

namespace Goose.Tests;

public class QuestIconResolverTests
{
    private static Quest MakeQuest(int id, params QuestRequirement[] requirements)
    {
        var quest = new Quest
        {
            Id = id,
            Name = "Quest " + id,
            Description = "desc",
        };
        foreach (var r in requirements)
        {
            r.Quest = quest;
            quest.Requirements.Add(r);
        }
        return quest;
    }

    private static QuestRequirement Req(int id, RequirementType type, long value = 0, long value2 = 0)
        => new() { Id = id, Type = type, Value = value, Value2 = value2 };

    private static (TestWorldFixture World, NPC Npc, TestWorldFixture.CapturingPlayer Player) Setup(params Quest[] quests)
    {
        var world = new TestWorldFixture();
        var npc = new NPC
        {
            NPCTemplate = new NPCTemplate { NPCTemplateID = 5, Name = "Quest NPC" },
            Quests = quests.ToList(),
        };
        var player = new TestWorldFixture.CapturingPlayer
        {
            Class = world.World.ClassHandler.GetClass(0)!,
            BaseStats = new AttributeSet(),
            MaxStats = new AttributeSet(),
        };
        player.Inventory = new Inventory(player, world.Settings);
        player.Spellbook = new Spellbook(player, world.Settings);
        return (world, npc, player);
    }

    [Fact]
    public void NoQuests_ResolvesNone_AndSendIconSendsClear()
    {
        var (world, npc, player) = Setup();

        Assert.Equal(QuestIconState.None, QuestStateResolver.Resolve(npc, player, world.World));

        world.World.QuestHandler.SendIcon(player, npc, world.World);

        Assert.Contains(player.Sent, s => s.StartsWith($"CHI{npc.LoginID},0,0"));
    }

    [Fact]
    public void InactiveEligibleQuest_ResolvesAvailable()
    {
        var (world, npc, player) = Setup(MakeQuest(1));

        Assert.Equal(QuestIconState.Available, QuestStateResolver.Resolve(npc, player, world.World));

        world.World.QuestHandler.SendIcon(player, npc, world.World);

        var s = world.Settings;
        Assert.Contains(player.Sent, s2 => s2.StartsWith($"CHI{npc.LoginID},{s.QuestAvailableIconSheet},{s.QuestAvailableIconGraphic}"));
    }

    [Fact]
    public void StartedIncompleteQuest_ResolvesNone()
    {
        var (world, npc, player) = Setup(MakeQuest(1, Req(1, RequirementType.Gold, 100)));
        player.Gold = 50;
        player.QuestsStarted.Add(npc.Quests[0]);

        Assert.Equal(QuestIconState.None, QuestStateResolver.Resolve(npc, player, world.World));
    }

    [Fact]
    public void StartedCompleteQuest_ResolvesReady()
    {
        var (world, npc, player) = Setup(MakeQuest(1, Req(1, RequirementType.Gold, 100)));
        player.Gold = 100;
        player.QuestsStarted.Add(npc.Quests[0]);

        Assert.Equal(QuestIconState.Ready, QuestStateResolver.Resolve(npc, player, world.World));

        world.World.QuestHandler.SendIcon(player, npc, world.World);

        var s = world.Settings;
        Assert.Contains(player.Sent, s2 => s2.StartsWith($"CHI{npc.LoginID},{s.QuestReadyIconSheet},{s.QuestReadyIconGraphic}"));
    }

    [Fact]
    public void OneAvailablePlusOneReady_ResolvesReady()
    {
        var (world, npc, player) = Setup(MakeQuest(1), MakeQuest(2, Req(1, RequirementType.Gold, 100)));
        player.Gold = 100;
        player.QuestsStarted.Add(npc.Quests[1]);

        Assert.Equal(QuestIconState.Ready, QuestStateResolver.Resolve(npc, player, world.World));
    }

    [Fact]
    public void CompletedNonRepeatable_ResolvesNone()
    {
        var (world, npc, player) = Setup(MakeQuest(1));
        player.QuestsCompleted.Add(npc.Quests[0]);

        Assert.Equal(QuestIconState.None, QuestStateResolver.Resolve(npc, player, world.World));
    }

    [Fact]
    public void CompletedRepeatableInactive_ResolvesAvailable()
    {
        var (world, npc, player) = Setup(MakeQuest(1));
        npc.Quests[0].Repeatable = true;
        player.QuestsCompleted.Add(npc.Quests[0]);

        Assert.Equal(QuestIconState.Available, QuestStateResolver.Resolve(npc, player, world.World));
    }

    [Fact]
    public void CompletedNonRepeatableStillStarted_AdversarialOverlap_ResolvesNone()
    {
        var (world, npc, player) = Setup(MakeQuest(1, Req(1, RequirementType.Gold, 100)));
        npc.Quests[0].Repeatable = false;
        player.Gold = 100;
        player.QuestsStarted.Add(npc.Quests[0]);
        player.QuestsCompleted.Add(npc.Quests[0]);

        Assert.Equal(QuestIconState.None, QuestStateResolver.Resolve(npc, player, world.World));
    }

    [Fact]
    public void ClassRestrictionFailure_ResolvesNone()
    {
        var (world, npc, player) = Setup(MakeQuest(1));
        player.Class = world.World.ClassHandler.GetClass(1)!;
        npc.Quests[0].ClassRestrictions = 1L << 3;

        Assert.Equal(QuestIconState.None, QuestStateResolver.Resolve(npc, player, world.World));
    }

    [Fact]
    public void MinimumLevelFailure_ResolvesNone()
    {
        var (world, npc, player) = Setup(MakeQuest(1));
        player.Level = 1;
        npc.Quests[0].MinLevel = 5;

        Assert.Equal(QuestIconState.None, QuestStateResolver.Resolve(npc, player, world.World));
    }

    [Fact]
    public void MaximumLevelFailure_ResolvesNone()
    {
        var (world, npc, player) = Setup(MakeQuest(1));
        player.Level = 10;
        npc.Quests[0].MaxLevel = 5;

        Assert.Equal(QuestIconState.None, QuestStateResolver.Resolve(npc, player, world.World));
    }

    [Fact]
    public void MinimumExperienceFailure_ResolvesNone()
    {
        var (world, npc, player) = Setup(MakeQuest(1));
        player.Experience = 0;
        npc.Quests[0].MinExperience = 1000;

        Assert.Equal(QuestIconState.None, QuestStateResolver.Resolve(npc, player, world.World));
    }

    [Fact]
    public void MaximumExperienceFailure_ResolvesNone()
    {
        var (world, npc, player) = Setup(MakeQuest(1));
        player.Experience = 1000;
        npc.Quests[0].MaxExperience = 500;

        Assert.Equal(QuestIconState.None, QuestStateResolver.Resolve(npc, player, world.World));
    }

    [Fact]
    public void PrerequisiteFailure_ResolvesNone()
    {
        var (world, npc, player) = Setup(MakeQuest(1));
        npc.Quests[0].PrerequisiteQuests.Add(99);

        Assert.Equal(QuestIconState.None, QuestStateResolver.Resolve(npc, player, world.World));
    }

    [Fact]
    public void ItemRequirement_IsEvaluated()
    {
        var (world, npc, player) = Setup(MakeQuest(1, Req(1, RequirementType.Item, 900, 3)));
        world.AddBaseItemTemplate(900, "Loot", ItemTemplate.UseTypes.NoUse);
        var item = new Item();
        item.LoadFromTemplate(world.World.ItemHandler.GetTemplate(900)!);
        world.World.ItemHandler.AddAndAssignId(item, world.World);
        Assert.True(player.Inventory.AddItem(item, 5, world.World));
        player.QuestsStarted.Add(npc.Quests[0]);

        Assert.Equal(QuestIconState.Ready, QuestStateResolver.Resolve(npc, player, world.World));

        npc.Quests[0].Requirements[0].Value2 = 7;

        Assert.False(QuestStateResolver.MeetsRequirements(npc.Quests[0], player, world.World));
    }

    [Fact]
    public void KillRequirement_IsEvaluated()
    {
        var (world, npc, player) = Setup(MakeQuest(1, Req(1, RequirementType.Kill, 123, 10)));
        var quest = npc.Quests[0];
        player.QuestsStarted.Add(quest);
        player.QuestProgress.Add(new QuestProgress { Requirement = quest.Requirements[0], Value = 9 });

        Assert.Equal(QuestIconState.None, QuestStateResolver.Resolve(npc, player, world.World));

        player.QuestProgress[0].Value = 10;

        Assert.Equal(QuestIconState.Ready, QuestStateResolver.Resolve(npc, player, world.World));
    }

    [Fact]
    public void TalkToNPCRequirement_IsEvaluated()
    {
        var (world, npc, player) = Setup(MakeQuest(1, Req(1, RequirementType.TalkToNPC, 456, 1)));
        var quest = npc.Quests[0];
        player.QuestsStarted.Add(quest);
        player.QuestProgress.Add(new QuestProgress { Requirement = quest.Requirements[0], Value = 0 });

        Assert.Equal(QuestIconState.None, QuestStateResolver.Resolve(npc, player, world.World));

        player.QuestProgress[0].Value = 1;

        Assert.Equal(QuestIconState.Ready, QuestStateResolver.Resolve(npc, player, world.World));
    }

    [Fact]
    public void ExperienceRequirements_AreEvaluated()
    {
        var (world, npc, player) = Setup(
            MakeQuest(1, Req(1, RequirementType.ExperienceBanked, 500)),
            MakeQuest(2, Req(2, RequirementType.ExperienceSold, 200)));
        player.Experience = 500;
        player.ExperienceSold = 200;
        player.QuestsStarted.Add(npc.Quests[0]);
        player.QuestsStarted.Add(npc.Quests[1]);

        Assert.Equal(QuestIconState.Ready, QuestStateResolver.Resolve(npc, player, world.World));

        player.Experience = 499;
        player.ExperienceSold = 199;

        Assert.Equal(QuestIconState.None, QuestStateResolver.Resolve(npc, player, world.World));
    }

    [Fact]
    public void NothingEquippedRequirement_IsEvaluated()
    {
        var (world, npc, player) = Setup(MakeQuest(1, Req(1, RequirementType.NothingEquipped)));
        var map = world.AddBaseMap(1, "Town");
        player.Map = map;
        player.MapID = map.ID;
        player.MapX = 1;
        player.MapY = 1;
        var quest = npc.Quests[0];
        player.QuestsStarted.Add(quest);

        Assert.Equal(QuestIconState.Ready, QuestStateResolver.Resolve(npc, player, world.World));

        world.AddBaseItemTemplate(901, "Sword", ItemTemplate.UseTypes.NoUse);
        var item = new Item();
        item.LoadFromTemplate(world.World.ItemHandler.GetTemplate(901)!);
        world.World.ItemHandler.AddAndAssignId(item, world.World);
        Assert.True(player.Inventory.AddItem(item, 1, world.World));
        Assert.True(player.Inventory.Equip(item, world.World));

        Assert.Equal(QuestIconState.None, QuestStateResolver.Resolve(npc, player, world.World));
    }

    [Fact]
    public void ScriptRequirement_IsEvaluatedThroughProductionPredicate()
    {
        using var scripts = new QuestScriptFixture();
        var met = scripts.Compile(@"
using Goose; using Goose.Quests; using Goose.Scripting;
public class T : BaseQuestScript
{
    public override bool IsMet(QuestRequirement requirement, Player player, GameWorld world)
        => true;
}
return typeof(T);
");
        var unmet = scripts.Compile(@"
using Goose; using Goose.Quests; using Goose.Scripting;
public class U : BaseQuestScript
{
    public override bool IsMet(QuestRequirement requirement, Player player, GameWorld world)
        => false;
}
return typeof(U);
", "U.csx");

        var metQuest = MakeQuest(1, new QuestRequirement { Id = 99, Type = RequirementType.Script, Script = met });
        var unmetQuest = MakeQuest(2, new QuestRequirement { Id = 98, Type = RequirementType.Script, Script = unmet });

        var player = new Player(0) { Class = new Class { ClassID = 0, ClassName = "Default" } };
        player.QuestsStarted.Add(metQuest);
        player.QuestsStarted.Add(unmetQuest);

        Assert.True(QuestStateResolver.MeetsRequirements(metQuest, player, scripts.World));
        Assert.False(QuestStateResolver.MeetsRequirements(unmetQuest, player, scripts.World));

        var npc = new NPC
        {
            NPCTemplate = new NPCTemplate { NPCTemplateID = 5, Name = "Quest NPC" },
            Quests = new List<Quest> { metQuest, unmetQuest },
        };

        Assert.Equal(QuestIconState.Ready, QuestStateResolver.Resolve(npc, player, scripts.World));
    }

    [Fact]
    public void FullInventoryAndBlockingRewardScript_DoNotSuppressReady()
    {
        using var scripts = new QuestScriptFixture();
        var blocker = scripts.Compile(@"
using Goose; using Goose.Quests; using Goose.Scripting;
public class T : BaseQuestScript
{
    public override string? CanComplete(QuestReward reward, Player player, GameWorld world)
        => ""Not today"";
}
return typeof(T);
");

        var quest = MakeQuest(1, Req(1, RequirementType.Gold, 100));
        quest.Rewards.Add(new QuestReward { Id = 100, Type = RewardType.Script, Script = blocker });

        var npc = new NPC
        {
            NPCTemplate = new NPCTemplate { NPCTemplateID = 5, Name = "Quest NPC" },
            Quests = new List<Quest> { quest },
        };

        var player = new Player(0) { Class = new Class { ClassID = 0, ClassName = "Default" } };
        player.Inventory = new Inventory(player, scripts.Settings);
        player.Spellbook = new Spellbook(player, scripts.Settings);
        player.Gold = 100;
        player.QuestsStarted.Add(quest);

        scripts.World.ItemHandler.AddTemplate(new ItemTemplate
        {
            ID = 900, Name = "Loot", Description = "Loot", StackSize = 1,
            BaseStats = new AttributeSet(), ScriptParams = "",
        });
        var template = scripts.World.ItemHandler.GetTemplate(900)!;
        for (var i = 0; i < scripts.Settings.InventorySize; i++)
        {
            var item = new Item();
            item.LoadFromTemplate(template);
            scripts.World.ItemHandler.AddAndAssignId(item, scripts.World);
            Assert.True(player.Inventory.AddItem(item, 1, scripts.World));
        }

        Assert.Equal(QuestIconState.Ready, QuestStateResolver.Resolve(npc, player, scripts.World));
    }

    [Fact]
    public void SetConfig_IconChange_RefreshesReadyPlayersWithNewPayload()
    {
        using var world = new TestWorldFixture();
        var map = world.AddBaseMap(1, "Town");

        var gm = world.CommandPlayerOn(map, 1, 1, "GM");
        gm.Access = Player.AccessStatus.GameMaster;
        gm.Sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        world.World.PlayerHandler.AddPlayer(gm, world.World);

        var hero = world.CommandPlayerOn(map, 2, 2, "Hero");
        hero.Sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        world.World.PlayerHandler.AddPlayer(hero, world.World);

        var quest = MakeQuest(1);
        hero.QuestsStarted.Add(quest);

        var npc = new NPC
        {
            NPCTemplate = new NPCTemplate { NPCTemplateID = 5, Name = "Quest NPC" },
            Quests = new List<Quest> { quest },
            MapX = 3, MapY = 3,
        };
        map.AddNPC(npc);

        Assert.True(world.RunCommand(gm, "/setconfig QuestReadyIconGraphic 424242"));

        Assert.Contains(hero.Sent, s => s.StartsWith($"CHI{npc.LoginID},{world.Settings.QuestReadyIconSheet},424242"));
    }
}
