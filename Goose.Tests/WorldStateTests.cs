namespace Goose.Tests;

public class WorldStateTests
{
    private static ItemSlot[] Slots(params (int Tid, long Stack)[] slots) =>
        slots.Select(s => new ItemSlot { Item = new Item { TemplateID = s.Tid }, Stack = s.Stack }).ToArray();

    private static WorldState Loaded(params (string Key, string Json)[] rows)
    {
        var state = new WorldState();
        state.LoadRows(rows.Select(r => new KeyValuePair<string, string>(r.Key, r.Json)));
        return state;
    }

    [Fact]
    public void KeysWithPrefix_ReturnsOnlyMatchingKeys()
    {
        var state = Loaded(("chest:a", "1"), ("chest:b", "2"), ("other", "3"));

        Assert.Equal(new[] { "chest:a", "chest:b" }, state.KeysWithPrefix("chest:").OrderBy(k => k));
    }

    [Fact]
    public void Get_MaterializesRawJsonOnce()
    {
        var json = JsonHelper.Serialize(Slots((5, 2), (7, 1)));
        var state = Loaded(("k", json));

        var first = state.Get<ItemSlot[]>("k");
        Assert.NotNull(first);
        Assert.Equal(2, first.Length);
        Assert.Equal(5, first[0].Item.TemplateID);
        Assert.Equal(2, first[0].Stack);

        var second = state.Get<ItemSlot[]>("k");
        Assert.Same(first, second);
    }

    [Fact]
    public void Get_CorruptJson_LogsAndReturnsDefault()
    {
        var state = Loaded(("k", "{ not json"));
        using var log = new CapturingLog();
        log.Target.Layout = "${level}: ${message}";

        Assert.Null(state.Get<ItemSlot[]>("k"));
        Assert.Contains(log.Messages, m => m.StartsWith("Error") && m.Contains("k"));
        Assert.Empty(state.PlanSave().Upserts);
    }

    [Fact]
    public void StringValues_RoundTripAsJson()
    {
        var state = new WorldState();
        state.Set("k", "hello");

        var plan = state.PlanSave();
        var upsert = Assert.Single(plan.Upserts);
        Assert.Equal("k", upsert.Key);
        Assert.Equal("\"hello\"", upsert.Json);

        var loaded = Loaded(("k", "\"hello\""));
        Assert.Equal("hello", loaded.Get<string>("k"));
    }

    [Fact]
    public void PlanSave_SkipsUnchangedValues()
    {
        var json = JsonHelper.Serialize(Slots((5, 2)));
        var state = Loaded(("k", json));
        state.Set("k", JsonHelper.Deserialize<ItemSlot[]>(json));

        Assert.Empty(state.PlanSave().Upserts);
    }

    [Fact]
    public void PlanSave_EmitsChangedAndNewKeys()
    {
        var json = JsonHelper.Serialize(Slots((5, 2)));
        var state = Loaded(("k", json));
        state.Set("other", Slots((9, 1)));

        var upserts = state.PlanSave().Upserts;
        var upsert = Assert.Single(upserts);
        Assert.Equal("other", upsert.Key);
    }

    [Fact]
    public void Remove_QueuesDelete()
    {
        var state = Loaded(("k", JsonHelper.Serialize(Slots((5, 2)))));
        state.Remove("k");

        Assert.Contains("k", state.PlanSave().Deletes);
    }

    [Fact]
    public void RemoveThenSet_SameCycle_UpsertsNotDeletes()
    {
        var state = Loaded(("k", JsonHelper.Serialize(Slots((5, 2)))));
        state.Remove("k");
        state.Set("k", Slots((5, 3)));

        var plan = state.PlanSave();
        Assert.Empty(plan.Deletes);
        var upsert = Assert.Single(plan.Upserts);
        Assert.Equal("k", upsert.Key);
    }

    [Fact]
    public void InterleavedRemove_AfterPlan_StillDeletedNextCycle()
    {
        var state = Loaded(("k", JsonHelper.Serialize(Slots((5, 2)))));
        state.Set("k", Slots((5, 3)));

        var plan = state.PlanSave();
        Assert.Contains("k", plan.Upserts.Select(u => u.Key));
        Assert.Empty(plan.Deletes);

        state.Remove("k");
        state.ApplyCommit(plan);

        Assert.Contains("k", state.PlanSave().Deletes);
    }

    [Fact]
    public void InterleavedSet_AfterDeletePlan_RestoredNextCycle()
    {
        var state = Loaded(("k", JsonHelper.Serialize(Slots((5, 2)))));
        state.Remove("k");

        var plan = state.PlanSave();
        Assert.Contains("k", plan.Deletes);

        state.Set("k", Slots((5, 4)));
        state.ApplyCommit(plan);

        var upsert = Assert.Single(state.PlanSave().Upserts);
        Assert.Equal("k", upsert.Key);
    }
}
