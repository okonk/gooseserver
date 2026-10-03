using Goose.Testing;

namespace Goose.IntegrationTests;

public class WorldStatePersistenceTests : PlayerFirstSaveTestBase
{
    public WorldStatePersistenceTests() : base(["players", "world_state"]) { }

    private static ItemSlot[] Slots(params (int Tid, long Stack)[] slots) =>
        slots.Select(s => new ItemSlot { Item = new Item { TemplateID = s.Tid }, Stack = s.Stack }).ToArray();

    [Fact]
    public void Set_Save_Load_RoundTripsTypedValue()
    {
        var state = new WorldState();
        state.Load(world.Database);
        state.Set("probe:key", Slots((5, 2), (7, 1)));
        state.Save(world);
        world.Database.Execute(conn => { });

        var loaded = new WorldState();
        loaded.Load(world.Database);
        var result = loaded.Get<ItemSlot[]>("probe:key");
        Assert.NotNull(result);
        Assert.Equal(2, result.Length);
        Assert.Equal(5, result[0].Item.TemplateID);
        Assert.Equal(2, result[0].Stack);
        Assert.Equal(7, result[1].Item.TemplateID);
        Assert.Equal(1, result[1].Stack);
    }

    [Fact]
    public void Save_WritesNothing_WhenUnchanged()
    {
        var state = new WorldState();
        state.Load(world.Database);
        state.Set("probe:key", Slots((5, 2)));
        state.Save(world);
        world.Database.Execute(conn => { });
        Assert.Equal(1, Count("SELECT COUNT(*) FROM world_state WHERE key='probe:key'"));

        var plan = state.PlanSave();
        Assert.Empty(plan.Upserts);
        Assert.Empty(plan.Deletes);
    }

    [Fact]
    public void Remove_PersistsDelete()
    {
        var state = new WorldState();
        state.Load(world.Database);
        state.Set("probe:key", Slots((5, 2)));
        state.Save(world);
        world.Database.Execute(conn => { });
        Assert.Equal(1, Count("SELECT COUNT(*) FROM world_state WHERE key='probe:key'"));

        state.Remove("probe:key");
        state.Save(world);
        world.Database.Execute(conn => { });

        Assert.Equal(0, Count("SELECT COUNT(*) FROM world_state WHERE key='probe:key'"));
    }
}
