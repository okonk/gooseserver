using Goose;
using Goose.Testing;
using Xunit;

namespace Goose.Tests;

public class GameWorldCompletionQueueTests
{
    private sealed class RecordingEvent : Event
    {
        private readonly Action _onReady;

        public RecordingEvent(long ticks, Action onReady)
        {
            this.Ticks = ticks;
            this._onReady = onReady;
        }

        public override void Ready(GameWorld world) => this._onReady();
    }

    [Fact]
    public void Completion_RunsOnlyOnTheUpdateThread()
    {
        using var fixture = new TestWorldFixture();
        var world = fixture.World;

        long? completionThread = null;
        var published = new ManualResetEventSlim(false);
        var worker = new Thread(() =>
        {
            Assert.True(world.EnqueueCompletion(() => completionThread = Environment.CurrentManagedThreadId));
            published.Set();
        });
        worker.Start();
        Assert.True(published.Wait(TimeSpan.FromSeconds(10)));

        world.Update();

        Assert.Equal(Environment.CurrentManagedThreadId, completionThread);
        Assert.NotEqual(worker.ManagedThreadId, completionThread);
    }

    [Fact]
    public void Completions_RunFifoAfterDueEvents()
    {
        using var fixture = new TestWorldFixture();
        var world = fixture.World;
        var order = new List<string>();

        world.EventHandler.AddEvent(new RecordingEvent(world.TimeNow - 100, () => order.Add("event")));
        Assert.True(world.EnqueueCompletion(() => order.Add("one")));
        Assert.True(world.EnqueueCompletion(() => order.Add("two")));
        Assert.True(world.EnqueueCompletion(() => order.Add("three")));

        world.Update();

        Assert.Equal(new[] { "event", "one", "two", "three" }, order);
    }

    [Fact]
    public void ThrowingCompletion_DoesNotBlockTheRest()
    {
        using var fixture = new TestWorldFixture();
        var world = fixture.World;
        var order = new List<string>();

        Assert.True(world.EnqueueCompletion(() => order.Add("before")));
        Assert.True(world.EnqueueCompletion(() => throw new InvalidOperationException("boom")));
        Assert.True(world.EnqueueCompletion(() => order.Add("after")));

        world.Update();

        Assert.Equal(new[] { "before", "after" }, order);
    }

    [Fact]
    public void DbCallbackPublication_DoesNotMutateGameUntilUpdate()
    {
        using var fixture = new TestWorldFixture();
        var world = fixture.World;
        world.Database.Start(Path.Combine(fixture.DataDirectory, "test.db"));

        var order = new List<string>();
        var callbackRan = new ManualResetEventSlim(false);
        world.Database.Enqueue(conn => { }, ex =>
        {
            world.EnqueueCompletion(() => order.Add("completion"));
            callbackRan.Set();
        });
        world.Database.Execute(conn => order.Add("execute"));
        Assert.True(callbackRan.Wait(TimeSpan.FromSeconds(10)));

        Assert.Equal(new[] { "execute" }, order);

        world.Update();
        Assert.Equal(new[] { "execute", "completion" }, order);

        world.Database.Stop();
    }

    [Fact]
    public void Update_OrdersEventsThenCompletionsThenDeliveryPump()
    {
        using var fixture = new TestWorldFixture();
        var world = fixture.World;
        var order = new List<string>();

        world.EventHandler.AddEvent(new RecordingEvent(world.TimeNow - 100, () => order.Add("event")));
        Assert.True(world.EnqueueCompletion(() => order.Add("completion")));
        world.DeliveryPump = () => order.Add("delivery");

        world.Update();

        Assert.Equal(new[] { "event", "completion", "delivery" }, order);
    }
}
