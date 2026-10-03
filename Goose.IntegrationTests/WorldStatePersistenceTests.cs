using System.Collections.Generic;
using System.Reflection;
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

    [Fact]
    public void Baseline_Update_AfterCommit()
    {
        var state = new WorldState();
        state.Load(world.Database);
        state.Set("probe:key", Slots((5, 2)));
        state.Save(world);
        world.Database.Execute(conn => { });

        var plan = state.PlanSave();
        Assert.Empty(plan.Upserts);
        Assert.Empty(plan.Deletes);
    }

    [Fact]
    public void Save_SingleFlight_DefersAndTrails()
    {
        var state = new WorldState();
        state.Load(world.Database);

        var blocker = new ManualResetEventSlim(false);
        world.Database.Enqueue(conn => blocker.Wait());

        state.Set("probe:key", Slots((5, 2)));
        state.Save(world);

        state.Remove("probe:key");
        state.Save(world);

        Assert.Equal(1, world.Database.PendingCount);
        Assert.True(Flag(state, "inFlight"));
        Assert.True(Flag(state, "trailing"));

        blocker.Set();
        world.Database.Execute(conn => { });

        Assert.Equal(1, CompletionCount(world));

        world.Update();
        world.Database.Execute(conn => { });

        Assert.Equal(0, Count("SELECT COUNT(*) FROM world_state WHERE key='probe:key'"));
        Assert.False(Flag(state, "inFlight"));
        Assert.False(Flag(state, "trailing"));

        state.Set("probe:key", Slots((8, 4)));
        state.Save(world);
        world.Database.Execute(conn => { });

        Assert.Equal(1, Count("SELECT COUNT(*) FROM world_state WHERE key='probe:key'"));
        Assert.Equal(JsonHelper.Serialize(Slots((8, 4))), Value("probe:key"));
    }

    [Fact]
    public void TrailingFlush_DoesNotRearmPeriodicCadence()
    {
        var state = new WorldState();
        state.Load(world.Database);

        var blocker = new ManualResetEventSlim(false);
        world.Database.Enqueue(conn => blocker.Wait());

        state.Set("probe:key", Slots((5, 2)));
        state.Save(world);
        state.Remove("probe:key");
        state.Save(world);

        int eventsAfterSaves = world.EventHandler.Count;

        blocker.Set();
        world.Database.Execute(conn => { });
        Assert.Equal(eventsAfterSaves, world.EventHandler.Count);

        world.Update();
        world.Database.Execute(conn => { });

        Assert.Equal(eventsAfterSaves, world.EventHandler.Count);
    }

    [Fact]
    public void SaveSync_PersistsWithoutPump()
    {
        var state = new WorldState();
        state.Load(world.Database);
        state.Set("probe:key", Slots((5, 2)));
        state.Set("probe:other", 42);

        state.SaveSync(world);

        Assert.Equal(2, Count("SELECT COUNT(*) FROM world_state"));
        var plan = state.PlanSave();
        Assert.Empty(plan.Upserts);
        Assert.Empty(plan.Deletes);
    }

    [Fact]
    public void SaveSync_SecondWorldStateWriteFails_RollsBackWholePlan()
    {
        var state = new WorldState();
        state.Load(world.Database);
        state.Set("probe:del", Slots((5, 2)));
        state.Set("probe:upd", Slots((7, 1)));
        state.SaveSync(world);

        string updBefore = Value("probe:upd")!;
        Assert.Equal(1, Count("SELECT COUNT(*) FROM world_state WHERE key='probe:del'"));

        RunSql("CREATE TRIGGER abort_upd BEFORE UPDATE ON world_state " +
            "FOR EACH ROW WHEN OLD.key='probe:upd' BEGIN SELECT RAISE(ABORT, 'forced'); END;");

        state.Remove("probe:del");
        state.Set("probe:upd", Slots((9, 3)));

        Assert.ThrowsAny<Exception>(() => state.SaveSync(world));

        Assert.Equal(1, Count("SELECT COUNT(*) FROM world_state WHERE key='probe:del'"));
        Assert.Equal(updBefore, Value("probe:upd"));

        RunSql("DROP TRIGGER abort_upd;");
        state.SaveSync(world);

        Assert.Equal(0, Count("SELECT COUNT(*) FROM world_state WHERE key='probe:del'"));
        Assert.NotEqual(updBefore, Value("probe:upd"));
    }

    [Fact]
    public void FailedTransaction_ClearsInFlight_PersistenceAndShutdownSurvive()
    {
        var state = new WorldState();
        state.Load(world.Database);
        state.Set("probe:key", Slots((5, 2)));
        state.Save(world);
        world.Database.Execute(conn => { });
        Assert.Equal(1, Count("SELECT COUNT(*) FROM world_state WHERE key='probe:key'"));

        RunSql("DROP TABLE world_state;");
        state.Set("probe:key", Slots((6, 1)));
        state.Save(world);
        world.Database.Execute(conn => { });

        Assert.False(Flag(state, "inFlight"));
        Assert.False(Flag(state, "trailing"));

        RunSql("CREATE TABLE world_state (key TEXT PRIMARY KEY, value TEXT NOT NULL);");
        state.Set("probe:key", Slots((7, 1)));
        state.Save(world);
        world.Database.Execute(conn => { });
        Assert.Equal(1, Count("SELECT COUNT(*) FROM world_state WHERE key='probe:key'"));

        state.SaveSync(world);
    }

    private void RunSql(string sql)
    {
        world.Database.Execute(conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        });
    }

    private string? Value(string key)
    {
        return world.Database.Execute<string?>(conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT value FROM world_state WHERE key=@key;";
            cmd.Parameters.AddWithValue("@key", key);
            return cmd.ExecuteScalar() as string;
        });
    }

    private static bool Flag(WorldState state, string name)
    {
        return (bool)typeof(WorldState).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(state)!;
    }

    private static int CompletionCount(GameWorld world)
    {
        var field = typeof(GameWorld).GetField("completionQueue", BindingFlags.NonPublic | BindingFlags.Instance)!;
        return ((Queue<Action>)field.GetValue(world)!).Count;
    }
}
