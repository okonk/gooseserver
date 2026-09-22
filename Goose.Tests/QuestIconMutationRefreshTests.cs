using Goose;
using Goose.Commands;
using Goose.Quests;
using Goose.Testing;
using Xunit;

namespace Goose.Tests;

public class QuestIconMutationRefreshTests
{
    private sealed class ContainerWindow : ItemContainerWindow
    {
        public ContainerWindow(ItemContainer container)
        {
            this.ItemContainer = container;
        }

        public override void SendSlot(int slotIndex, Player player, GameWorld world) { }
    }

    private static Quest MakeQuest(int id, params QuestRequirement[] requirements)
    {
        var quest = new Quest
        {
            Id = id,
            Name = "Quest " + id,
            Description = "desc",
            FailText = "fail",
            PassText = "pass",
        };
        foreach (var r in requirements)
        {
            r.Quest = quest;
            quest.Requirements.Add(r);
        }
        return quest;
    }

    private static QuestRequirement Req(int id, RequirementType type, long value, long value2)
        => new() { Id = id, Type = type, Value = value, Value2 = value2 };

    private static (TestWorldFixture World, Map Map, TestWorldFixture.CapturingPlayer Player, NPC Npc) Setup(
        Action<GooseSettings>? configure = null, params Quest[] quests)
    {
        var world = new TestWorldFixture(configure);
        var map = world.AddBaseMap(1, "Town");
        var player = world.CommandPlayerOn(map, 2, 2, "Hero");
        player.Level = 1;
        player.Spellbook = new Spellbook(player, world.Settings);
        var npc = new NPC
        {
            NPCTemplate = new NPCTemplate { NPCTemplateID = 5, Name = "Quest NPC" },
            Quests = quests.ToList(),
            MapX = 3, MapY = 3,
        };
        map.AddNPC(npc);
        return (world, map, player, npc);
    }

    private static List<string> Chis(TestWorldFixture.CapturingPlayer player, int npcLoginId)
        => player.Sent.Where(s => s.StartsWith("CHI" + npcLoginId + ",")).Select(s => s[..^1]).ToList();

    private static string? LastChi(TestWorldFixture.CapturingPlayer player, int npcLoginId)
        => Chis(player, npcLoginId).LastOrDefault();

    private static string ReadyChi(TestWorldFixture world, int npcLoginId)
        => $"CHI{npcLoginId},{world.Settings.QuestReadyIconSheet},{world.Settings.QuestReadyIconGraphic}";

    private static string AvailableChi(TestWorldFixture world, int npcLoginId)
        => $"CHI{npcLoginId},{world.Settings.QuestAvailableIconSheet},{world.Settings.QuestAvailableIconGraphic}";

    private static string ClearChi(int npcLoginId) => $"CHI{npcLoginId},0,0";

    private static Item MakeItem(TestWorldFixture world, int templateId,
                                  ItemTemplate.UseTypes useType = ItemTemplate.UseTypes.NoUse)
    {
        var template = world.AddBaseItemTemplate(templateId, "Item" + templateId, useType);
        var item = new Item();
        item.LoadFromTemplate(template);
        world.World.ItemHandler.AddAndAssignId(item, world.World);
        return item;
    }

    [Fact]
    public void AddItem_CrossingItemRequirement_PublishesReadyIcon()
    {
        var quest = MakeQuest(1, Req(1, RequirementType.Item, 100, 1));
        var (world, _, player, npc) = Setup(null, quest);
        player.QuestsStarted.Add(quest);
        var item = MakeItem(world, 100);

        Assert.True(player.Inventory.AddItem(item, 1, world.World));

        Assert.Equal(ReadyChi(world, npc.LoginID), LastChi(player, npc.LoginID));
    }

    [Fact]
    public void RemoveItem_CrossingItemRequirement_ClearsIcon()
    {
        var quest = MakeQuest(1, Req(1, RequirementType.Item, 100, 1));
        var (world, _, player, npc) = Setup(null, quest);
        player.QuestsStarted.Add(quest);
        var item = MakeItem(world, 100);
        Assert.True(player.Inventory.AddItem(item, 1, world.World));
        player.Sent.Clear();

        Assert.NotNull(player.Inventory.RemoveItem(item, 1, world.World));

        Assert.Equal(ClearChi(npc.LoginID), LastChi(player, npc.LoginID));
    }

    [Fact]
    public void FailedRemoveItem_SendsNoIconRefresh()
    {
        var quest = MakeQuest(1, Req(1, RequirementType.Item, 100, 1));
        var (world, _, player, npc) = Setup(null, quest);
        player.QuestsStarted.Add(quest);
        var item = MakeItem(world, 100);
        Assert.True(player.Inventory.AddItem(item, 1, world.World));
        player.Sent.Clear();

        Assert.Null(player.Inventory.RemoveItem(item, 5, world.World));

        Assert.Empty(Chis(player, npc.LoginID));
    }

    [Fact]
    public void Equip_CrossingNothingEquipped_PublishesExactlyOneFinalIcon()
    {
        var quest = MakeQuest(1, Req(1, RequirementType.NothingEquipped, 0, 0));
        var (world, _, player, npc) = Setup(null, quest);
        player.QuestsStarted.Add(quest);
        var weapon = MakeItem(world, 200, ItemTemplate.UseTypes.Weapon);
        Assert.True(player.Inventory.AddItem(weapon, 1, world.World));
        player.Sent.Clear();

        Assert.True(player.Inventory.Equip(weapon, world.World));

        Assert.Equal([ClearChi(npc.LoginID)], Chis(player, npc.LoginID));
    }

    [Fact]
    public void Unequip_CrossingNothingEquipped_PublishesExactlyOneReadyIcon()
    {
        var quest = MakeQuest(1, Req(1, RequirementType.NothingEquipped, 0, 0));
        var (world, _, player, npc) = Setup(null, quest);
        player.QuestsStarted.Add(quest);
        var weapon = MakeItem(world, 200, ItemTemplate.UseTypes.Weapon);
        Assert.True(player.Inventory.AddItem(weapon, 1, world.World));
        Assert.True(player.Inventory.Equip(weapon, world.World));
        player.Sent.Clear();

        Assert.True(player.Inventory.Unequip(Inventory.EquipSlots.Weapon, world.World));

        Assert.Equal([ReadyChi(world, npc.LoginID)], Chis(player, npc.LoginID));
    }

    [Fact]
    public void Equip_PartiallyMutatedFailure_PublishesExactlyOneFinalIcon()
    {
        var quest = MakeQuest(1, Req(1, RequirementType.Item, 100, 1));
        var (world, _, player, npc) = Setup(null, quest);
        player.QuestsStarted.Add(quest);

        world.AddBaseItemTemplate(100, "Shield", ItemTemplate.UseTypes.Armor,
            t => t.Slot = ItemTemplate.ItemSlots.Shield);
        world.AddBaseItemTemplate(200, "Sword", ItemTemplate.UseTypes.Weapon);
        world.AddBaseItemTemplate(300, "Greatsword", ItemTemplate.UseTypes.Weapon,
            t => t.Slot = ItemTemplate.ItemSlots.TwoHanded);
        Item FromTemplate(int id)
        {
            var item = new Item();
            item.LoadFromTemplate(world.World.ItemHandler.GetTemplate(id)!);
            world.World.ItemHandler.AddAndAssignId(item, world.World);
            return item;
        }
        var shield = FromTemplate(100);
        var sword = FromTemplate(200);
        var greatsword = FromTemplate(300);

        // Leave exactly one free inventory slot with shield + sword equipped and the
        // 2H weapon in the bag: equipping it moves the shield into the last free slot,
        // then the sword cannot be unequipped because the inventory is now full.
        for (var i = 0; i < world.Settings.InventorySize - 2; i++)
            Assert.True(player.Inventory.AddItem(MakeItem(world, 999), 1, world.World));
        Assert.True(player.Inventory.AddItem(greatsword, 1, world.World));
        Assert.True(player.Inventory.AddItem(shield, 1, world.World));
        Assert.True(player.Inventory.Equip(shield, world.World));
        Assert.True(player.Inventory.AddItem(sword, 1, world.World));
        Assert.True(player.Inventory.Equip(sword, world.World));
        player.Sent.Clear();

        Assert.False(player.Inventory.Equip(greatsword, world.World));

        Assert.NotNull(player.Inventory.GetEquippedSlot(Inventory.EquipSlots.Weapon));
        Assert.Null(player.Inventory.GetEquippedSlot(Inventory.EquipSlots.Shield));
        Assert.True(player.Inventory.HasItem(100, 1));
        Assert.True(player.Inventory.HasItem(300, 1));

        Assert.Equal([ReadyChi(world, npc.LoginID)], Chis(player, npc.LoginID));
    }

    [Fact]
    public void Equip_NoMutationFailure_SendsNoIconRefresh()
    {
        var quest = MakeQuest(1, Req(1, RequirementType.Item, 100, 1));
        var (world, _, player, npc) = Setup(null, quest);
        player.QuestsStarted.Add(quest);
        world.AddBaseItemTemplate(100, "Shield", ItemTemplate.UseTypes.Armor,
            t => t.Slot = ItemTemplate.ItemSlots.Shield);
        var shield = new Item();
        shield.LoadFromTemplate(world.World.ItemHandler.GetTemplate(100)!);
        world.World.ItemHandler.AddAndAssignId(shield, world.World);
        Assert.True(player.Inventory.AddItem(shield, 1, world.World));
        Assert.True(player.Inventory.Equip(shield, world.World));
        // Full inventory: the shield cannot be unequipped to make room, so the
        // operation fails before mutating anything and must publish nothing.
        for (var i = 0; i < world.Settings.InventorySize; i++)
            Assert.True(player.Inventory.AddItem(MakeItem(world, 999), 1, world.World));
        player.Sent.Clear();

        Assert.False(player.Inventory.Equip(shield, world.World));

        Assert.Empty(Chis(player, npc.LoginID));
    }

    [Fact]
    public void InventoryToWindow_CrossingItemRequirement_ClearsIcon()
    {
        var quest = MakeQuest(1, Req(1, RequirementType.Item, 100, 1));
        var (world, _, player, npc) = Setup(null, quest);
        player.QuestsStarted.Add(quest);
        var item = MakeItem(world, 100);
        Assert.True(player.Inventory.AddItem(item, 1, world.World));
        player.Sent.Clear();

        var window = new ContainerWindow(new ItemContainer(11));
        window.InventoryToWindow(player, 1, 1, world.World);

        Assert.Null(player.Inventory.GetSlot(1));
        Assert.Equal(ClearChi(npc.LoginID), LastChi(player, npc.LoginID));
    }

    [Fact]
    public void WindowToInventory_CrossingItemRequirement_PublishesReadyIcon()
    {
        var quest = MakeQuest(1, Req(1, RequirementType.Item, 100, 1));
        var (world, _, player, npc) = Setup(null, quest);
        player.QuestsStarted.Add(quest);
        var item = MakeItem(world, 100);

        var window = new ContainerWindow(new ItemContainer(11));
        window.SetSlot(1, new ItemSlot { Item = item, Stack = 1 });
        window.WindowToInventory(player, 1, 1, world.World);

        Assert.NotNull(player.Inventory.GetSlot(1));
        Assert.Equal(ReadyChi(world, npc.LoginID), LastChi(player, npc.LoginID));
    }

    [Fact]
    public void AddGold_CrossingGoldThreshold_PublishesReadyIcon()
    {
        var quest = MakeQuest(1, Req(1, RequirementType.Gold, 100, 0));
        var (world, _, player, npc) = Setup(null, quest);
        player.QuestsStarted.Add(quest);
        player.Gold = 99;

        player.AddGold(1, world.World);

        Assert.Equal(ReadyChi(world, npc.LoginID), LastChi(player, npc.LoginID));
    }

    [Fact]
    public void RemoveGold_CrossingGoldThreshold_ClearsIcon()
    {
        var quest = MakeQuest(1, Req(1, RequirementType.Gold, 100, 0));
        var (world, _, player, npc) = Setup(null, quest);
        player.QuestsStarted.Add(quest);
        player.Gold = 100;

        player.RemoveGold(1, world.World);

        Assert.Equal(ClearChi(npc.LoginID), LastChi(player, npc.LoginID));
    }

    [Fact]
    public void RejectedRemoveGold_SendsNoIconRefresh()
    {
        var quest = MakeQuest(1, Req(1, RequirementType.Gold, 100, 0));
        var (world, _, player, npc) = Setup(null, quest);
        player.QuestsStarted.Add(quest);
        player.Gold = 50;

        player.RemoveGold(100, world.World);

        Assert.Equal(50, player.Gold);
        Assert.Empty(Chis(player, npc.LoginID));
    }

    [Fact]
    public void AddExperience_CrossingMinExperience_PublishesAvailableIcon()
    {
        var quest = MakeQuest(1);
        quest.MinExperience = 100;
        var (world, _, player, npc) = Setup(null, quest);
        player.Experience = 99;

        player.AddExperience(1, world.World, Player.ExperienceMessage.Normal);

        Assert.Equal(100, player.Experience);
        Assert.Equal(AvailableChi(world, npc.LoginID), LastChi(player, npc.LoginID));
    }

    [Fact]
    public void LevelUp_CrossingMinLevel_PublishesAvailableIcon()
    {
        var quest = MakeQuest(1);
        quest.MinLevel = 2;
        var (world, _, player, npc) = Setup(null, quest);

        var cls = new Class { ClassID = 2, ClassName = "Leveler", ACMultiplier = 1.0 };
        cls.AddLevel(new ClassLevel { Level = 1, BaseStats = new AttributeSet(), Experience = 100, Spells = new List<Spell>() });
        cls.AddLevel(new ClassLevel { Level = 2, BaseStats = new AttributeSet(), Experience = 1000, Spells = new List<Spell>() });
        player.Class = cls;
        player.ClassID = 2;

        player.AddExperience(150, world.World, Player.ExperienceMessage.Normal);

        Assert.Equal(2, player.Level);
        Assert.Equal(AvailableChi(world, npc.LoginID), LastChi(player, npc.LoginID));
    }

    [Fact]
    public void ClassChange_CrossingClassRestriction_PublishesAvailableIcon()
    {
        var quest = MakeQuest(1);
        quest.ClassRestrictions = 1L << 1;
        var (world, _, player, npc) = Setup(null, quest);

        player.ChangeClass(1, 1, world.World);

        Assert.Equal(1, player.ClassID);
        Assert.Equal(AvailableChi(world, npc.LoginID), LastChi(player, npc.LoginID));
    }

    [Fact]
    public void BuyVita_CrossingExperienceSoldRequirement_PublishesReadyIcon()
    {
        var quest = MakeQuest(1, Req(1, RequirementType.ExperienceSold, 50, 0));
        var (world, _, player, npc) = Setup(s =>
        {
            s.VitaBuyAmount = 10;
            s.IncreaseVitaBuyAmount = 100;
        }, quest);
        world.World.ClassHandler.GetClass(0)!.VitaCost = 50;
        player.QuestsStarted.Add(quest);
        player.Experience = 100;

        var ctx = new CommandContext(player, world.World, new CommandRegistry(), [], "");
        new BuyVitaCommand().Execute(ctx, 1);

        Assert.Equal(50, player.ExperienceSold);
        Assert.Equal(ReadyChi(world, npc.LoginID), LastChi(player, npc.LoginID));
    }

    [Fact]
    public void MaplessPlayer_Mutations_DoNotThrow()
    {
        var world = new TestWorldFixture();
        var player = new TestWorldFixture.CapturingPlayer
        {
            Name = "Ghost",
            State = Player.States.Ready,
            Level = 1,
            BaseStats = new AttributeSet(),
            MaxStats = new AttributeSet(),
            Class = world.World.ClassHandler.GetClass(0)!,
        };
        player.Inventory = new Inventory(player, world.Settings);
        player.Spellbook = new Spellbook(player, world.Settings);
        var item = MakeItem(world, 100);

        player.AddGold(10, world.World);
        Assert.True(player.Inventory.AddItem(item, 1, world.World));
        player.RemoveGold(5, world.World);
        player.AddExperience(50, world.World, Player.ExperienceMessage.None);

        Assert.Equal(5, player.Gold);
        Assert.Equal(50, player.Experience);
    }
}
