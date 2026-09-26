using Goose;
using Goose.Testing;
using Xunit;

namespace Goose.Tests;

public class GameWorldStoppingTests
{
    [Fact]
    public void StoppingTransition_ClearsQueuesWithoutExecuting()
    {
        using var fixture = new TestWorldFixture();
        var world = fixture.World;
        var map = fixture.AddBaseMap(1, "Test");
        var player = fixture.CommandPlayerOn(map, 5, 5);
        var executed = new List<string>();

        Assert.True(world.EnqueueCompletion(() => executed.Add("completion")));
        Assert.True(world.EnqueueDelivery(() =>
        {
            executed.Add("delivery");
            world.Send(player, "late");
        }));

        world.BeginStopping();

        Assert.Equal(0, world.PendingCompletionCount);
        Assert.Equal(0, world.PendingDeliveryCount);

        world.Update();
        Assert.Empty(executed);
        Assert.Empty(player.Sent);
    }

    [Fact]
    public void EnqueueRacingStopping_NeverStrandsWork()
    {
        using var fixture = new TestWorldFixture();
        var world = fixture.World;
        var executed = new List<string>();

        var worker = new Thread(() =>
        {
            while (world.EnqueueCompletion(() => executed.Add("stranded")))
                Thread.SpinWait(1);
        });
        worker.Start();
        Thread.Sleep(5);
        world.BeginStopping();
        worker.Join();

        Assert.Equal(0, world.PendingCompletionCount);
        Assert.Empty(executed);
    }

    [Fact]
    public void DbCallbackHeldUntilAfterStoppingBegins_IsRejected()
    {
        using var fixture = new TestWorldFixture();
        var world = fixture.World;
        var executed = new List<string>();

        Action publish = () =>
        {
            if (world.EnqueueCompletion(() => executed.Add("late")))
                executed.Add("published");
        };

        world.BeginStopping();
        publish();

        Assert.Empty(executed);
        Assert.Equal(0, world.PendingCompletionCount);
    }

    [Fact]
    public void DbCallbackAfterSimulatedStopReturnBoundary_IsStillRejected()
    {
        using var fixture = new TestWorldFixture();
        var world = fixture.World;
        world.Database.Start(Path.Combine(fixture.DataDirectory, "test.db"));

        var executed = new List<string>();
        var workerGate = new ManualResetEventSlim(false);
        var publishGate = new ManualResetEventSlim(false);
        var callbackRan = new ManualResetEventSlim(false);
        bool accepted = true;
        world.Database.Enqueue(conn => workerGate.Wait(), ex =>
        {
            publishGate.Wait();
            accepted = world.EnqueueCompletion(() => executed.Add("post-stop"));
            callbackRan.Set();
        });

        world.BeginStopping();
        workerGate.Set();
        world.BeginStopping();
        publishGate.Set();
        Assert.True(callbackRan.Wait(TimeSpan.FromSeconds(10)));

        Assert.False(accepted);
        Assert.Equal(0, world.PendingCompletionCount);
        Assert.Empty(executed);

        world.Database.Stop();
    }

    [Fact]
    public void RepeatedStoppingAndClearing_IsIdempotent()
    {
        using var fixture = new TestWorldFixture();
        var world = fixture.World;

        world.BeginStopping();
        world.BeginStopping();
        world.BeginStopping();

        Assert.False(world.EnqueueCompletion(() => { }));
        Assert.False(world.EnqueueDelivery(() => { }));
        Assert.Equal(0, world.PendingCompletionCount);
        Assert.Equal(0, world.PendingDeliveryCount);

        world.Update();
    }
}
