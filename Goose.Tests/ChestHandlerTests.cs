using Goose;
using Goose.Testing;

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
