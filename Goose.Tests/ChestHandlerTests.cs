using Goose;
using Goose.CommunityGoals;
using Goose.Testing;
using Xunit;

namespace Goose.Tests;

public class ChestHandlerTests
{
    private const int Chest = 77;
    private const int Template = 100;

    private static ItemSlot[] Blob(int length, params (int Index, int ItemId, int Tid)[] items)
    {
        var slots = new ItemSlot[length];
        foreach (var (index, itemId, tid) in items)
            slots[index] = new ItemSlot { Item = new Item { ItemID = itemId, TemplateID = tid } };
        return slots;
    }

    private static void Seed(TestWorldFixture fixture, ItemSlot[] slots)
    {
        fixture.World.WorldState.LoadRows(new[]
        {
            new KeyValuePair<string, string>("chest:" + Chest, JsonHelper.Serialize(slots)),
        });
    }

    private static void SeedPagesFlag(TestWorldFixture fixture, int npcTemplateId, int pages)
    {
        fixture.World.CommunityGoals.LoadStateRows(
            Array.Empty<CommunityGoalRun>(),
            Array.Empty<(int, int, Contribution)>(),
            new[]
            {
                new WorldFlag
                {
                    Name = "chest_pages:" + npcTemplateId,
                    Value = pages,
                    SetAt = new DateTime(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc),
                },
            });
    }

    private static NPC SpawnChest(TestWorldFixture fixture, int templateId)
    {
        var template = new NPCTemplate
        {
            NPCTemplateID = templateId, Name = "Chest " + templateId, Level = 50, ClassID = 0,
            BaseStats = new AttributeSet(),
        };
        fixture.World.NPCHandler.AddTemplate(template);
        return fixture.World.NPCHandler.SpawnNPC(
            fixture.World, 1, 5, 5, template, shouldRespawn: true,
            new PropertiesDictionary { ["communityChest"] = true })!;
    }

    private static TestWorldFixture.CapturingPlayer OnlinePlayer(TestWorldFixture fixture, Map map, string name)
    {
        var player = fixture.CommandPlayerOn(map, 5, 5, name);
        player.Windows = new List<Window>();
        fixture.AddOnlinePlayer(player);
        return player;
    }

    [Fact]
    public void Load_RebuildsContainersAndRegistersItems()
    {
        using var fixture = new TestWorldFixture();
        fixture.AddBaseItemTemplate(Template, "Chest Sword", ItemTemplate.UseTypes.NoUse);
        Seed(fixture, Blob(31, (1, 9001, Template)));

        fixture.World.ChestHandler.Load(fixture.World);

        var container = fixture.World.ChestHandler.GetOrCreateContainer(fixture.World, Chest);
        var slot = container.GetSlot(1);
        Assert.NotNull(slot);
        Assert.Equal(9001, slot.Item.ItemID);
        Assert.NotNull(slot.Item.Template);
        Assert.Equal(Template, slot.Item.Template.ID);
        Assert.Contains(fixture.World.ItemHandler.GetItems(), i => i.ItemID == 9001);
    }

    [Fact]
    public void Load_DropsUnknownTemplateSlots()
    {
        using var fixture = new TestWorldFixture();
        fixture.AddBaseItemTemplate(Template, "Chest Sword", ItemTemplate.UseTypes.NoUse);
        Seed(fixture, Blob(31, (1, 9002, Template), (2, 9003, 999)));

        fixture.World.ChestHandler.Load(fixture.World);

        var container = fixture.World.ChestHandler.GetOrCreateContainer(fixture.World, Chest);
        Assert.NotNull(container.GetSlot(1));
        Assert.Null(container.GetSlot(2));
        Assert.DoesNotContain(fixture.World.ItemHandler.GetItems(), i => i.ItemID == 9003);
    }

    [Fact]
    public void GetOrCreateContainer_RegistersWithWorldState()
    {
        using var fixture = new TestWorldFixture();

        var container = fixture.World.ChestHandler.GetOrCreateContainer(fixture.World, 42);

        Assert.Contains("chest:42", fixture.World.WorldState.KeysWithPrefix("chest:"));
        Assert.Same(container, fixture.World.WorldState.Get<ItemContainer>("chest:42"));
    }

    [Fact]
    public void Load_GrowsContainerToWholePages()
    {
        using var fixture = new TestWorldFixture(s => s.CommunityChestPages = 1);
        fixture.AddBaseItemTemplate(Template, "Chest Sword", ItemTemplate.UseTypes.NoUse);
        Seed(fixture, Blob(40, (39, 9004, Template)));

        fixture.World.ChestHandler.Load(fixture.World);

        var container = fixture.World.ChestHandler.GetOrCreateContainer(fixture.World, Chest);
        Assert.Equal(61, container.MaxSlots);
        Assert.NotNull(container.GetSlot(39));
        for (int i = 40; i < container.MaxSlots; i++)
            Assert.Null(container.GetSlot(i));
    }

    [Fact]
    public void Load_FlagLoadedBeforeChests_SizesContainerWithBonusPages()
    {
        using var fixture = new TestWorldFixture(s => s.CommunityChestPages = 1);
        fixture.AddBaseItemTemplate(Template, "Chest Sword", ItemTemplate.UseTypes.NoUse);
        SeedPagesFlag(fixture, Chest, 1);
        Seed(fixture, Blob(31, (1, 9006, Template)));

        fixture.World.ChestHandler.Load(fixture.World);

        var container = fixture.World.ChestHandler.GetOrCreateContainer(fixture.World, Chest);
        Assert.Equal(61, container.MaxSlots);
        Assert.NotNull(container.GetSlot(1));
    }

    [Fact]
    public void GetOrCreateContainer_ChestCreatedAfterFlag_HasBonusPages()
    {
        using var fixture = new TestWorldFixture(s => s.CommunityChestPages = 1);
        SeedPagesFlag(fixture, Chest, 2);

        var container = fixture.World.ChestHandler.GetOrCreateContainer(fixture.World, Chest);

        Assert.Equal(91, container.MaxSlots);
    }

    [Fact]
    public void ApplyBonusPages_GrowsSameContainerAndRendersViewers()
    {
        using var fixture = new TestWorldFixture(s => s.CommunityChestPages = 1);
        var map = fixture.AddBaseMap(1, "Town");
        var chest = SpawnChest(fixture, Chest);
        var a = OnlinePlayer(fixture, map, "A");
        var b = OnlinePlayer(fixture, map, "B");

        CommunityChestWindow.Open(fixture.World, a, chest);
        CommunityChestWindow.Open(fixture.World, b, chest);
        var aWin = (CommunityChestWindow)a.Windows[0];
        var bWin = (CommunityChestWindow)b.Windows[0];
        var container = fixture.World.ChestHandler.GetOrCreateContainer(fixture.World, Chest);
        Assert.Equal(31, container.MaxSlots);
        Assert.Equal(1, aWin.MaxPages);

        fixture.World.CommunityGoals.SetFlag("chest_pages:" + Chest, 1, null);
        a.Sent.Clear();
        b.Sent.Clear();
        fixture.World.ChestHandler.ApplyBonusPages(fixture.World, Chest);

        Assert.Same(container, fixture.World.ChestHandler.GetOrCreateContainer(fixture.World, Chest));
        Assert.Equal(61, container.MaxSlots);
        Assert.Equal(2, aWin.MaxPages);
        Assert.Equal(2, bWin.MaxPages);
        Assert.Contains(a.Sent, p => p.StartsWith("MKW" + aWin.ID + ",30,Community Chest Page 1/2"));
        Assert.Contains(b.Sent, p => p.StartsWith("MKW" + bWin.ID + ",30,Community Chest Page 1/2"));
    }

    [Fact]
    public void ApplyBonusPages_NoContainerYet_NoOp()
    {
        using var fixture = new TestWorldFixture(s => s.CommunityChestPages = 1);
        SeedPagesFlag(fixture, Chest, 1);

        fixture.World.ChestHandler.ApplyBonusPages(fixture.World, Chest);

        var container = fixture.World.ChestHandler.GetOrCreateContainer(fixture.World, Chest);
        Assert.Equal(61, container.MaxSlots);
    }

    [Fact]
    public void ApplyBonusPages_WhenFlagDropped_DoesNotShrinkOrResend()
    {
        using var fixture = new TestWorldFixture(s => s.CommunityChestPages = 1);
        var map = fixture.AddBaseMap(1, "Town");
        var chest = SpawnChest(fixture, Chest);
        var a = OnlinePlayer(fixture, map, "A");

        SeedPagesFlag(fixture, Chest, 1);
        CommunityChestWindow.Open(fixture.World, a, chest);
        var win = (CommunityChestWindow)a.Windows[0];
        var container = fixture.World.ChestHandler.GetOrCreateContainer(fixture.World, Chest);
        Assert.Equal(61, container.MaxSlots);
        Assert.Equal(2, win.MaxPages);

        fixture.World.CommunityGoals.LoadStateRows(
            Array.Empty<CommunityGoalRun>(),
            Array.Empty<(int, int, Contribution)>(),
            Array.Empty<WorldFlag>());
        a.Sent.Clear();
        fixture.World.ChestHandler.ApplyBonusPages(fixture.World, Chest);

        Assert.Equal(61, container.MaxSlots);
        Assert.Equal(2, win.MaxPages);
        Assert.DoesNotContain(a.Sent, p => p.StartsWith("MKW"));
    }

    [Fact]
    public void Load_CapacityStableAcrossRestarts()
    {
        using var fixture = new TestWorldFixture(s => s.CommunityChestPages = 1);
        fixture.AddBaseItemTemplate(Template, "Chest Sword", ItemTemplate.UseTypes.NoUse);
        Seed(fixture, Blob(31, (1, 9005, Template)));

        for (int i = 0; i < 2; i++)
        {
            fixture.World.ChestHandler.Load(fixture.World);
            var container = fixture.World.ChestHandler.GetOrCreateContainer(fixture.World, Chest);
            Assert.Equal(31, container.MaxSlots);
            Assert.NotNull(container.GetSlot(1));

            Seed(fixture, JsonHelper.Deserialize<ItemSlot[]>(JsonHelper.Serialize(container))!);
        }
    }
}
