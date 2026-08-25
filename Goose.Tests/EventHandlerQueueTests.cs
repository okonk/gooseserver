using System;
using System.Collections.Generic;
using Goose;
using Xunit;

namespace Goose.Tests
{
    /// <summary>
    /// Tests for the PriorityQueue-backed event scheduling in EventHandler.
    /// </summary>
    public class EventHandlerQueueTests
    {
        private readonly GameWorld _world;
        private readonly EventHandler _handler;

        public EventHandlerQueueTests()
        {
            _world = new GameWorld(new GameServer(GameWorld.Settings));
            _handler = _world.EventHandler;
        }

        private sealed class Collector
        {
            public List<string> Executed { get; } = new List<string>();
        }

        private sealed class RecordingEvent : Event
        {
            public string? Name { get; set; }
            public Action<RecordingEvent, GameWorld>? OnReady { get; set; }

            public override void Ready(GameWorld world)
            {
                OnReady?.Invoke(this, world);
            }
        }

        private RecordingEvent NewEvent(Collector collector, string name, long ticks)
        {
            return new RecordingEvent
            {
                Name = name,
                Ticks = ticks,
                OnReady = (ev, _) => collector.Executed.Add(ev.Name ?? ""),
            };
        }

        [Fact]
        public void Update_RunsDueEventsInTickOrder()
        {
            var collector = new Collector();
            long now = _world.TimeNow;

            var c = NewEvent(collector, "c", now - 300);
            var a = NewEvent(collector, "a", now - 100);
            var b = NewEvent(collector, "b", now - 200);

            // Enqueue out of order; the heap must still run them earliest-first.
            _handler.AddEvent(c);
            _handler.AddEvent(a);
            _handler.AddEvent(b);

            _handler.Update(_world);

            Assert.Equal(new[] { "c", "b", "a" }, collector.Executed);
        }

        [Fact]
        public void Update_RunsAllEventsSharingTheSameTick()
        {
            var collector = new Collector();
            long now = _world.TimeNow;

            var a = NewEvent(collector, "a", now - 100);
            var b = NewEvent(collector, "b", now - 100);
            var c = NewEvent(collector, "c", now - 100);

            _handler.AddEvent(a);
            _handler.AddEvent(b);
            _handler.AddEvent(c);

            _handler.Update(_world);

            Assert.Equal(new[] { "a", "b", "c" }.OrderBy(x => x), collector.Executed.OrderBy(x => x));
            Assert.Equal(3, collector.Executed.Count);
        }

        [Fact]
        public void AddEvent_DoesNotMutateTicksOnSameTickCollision()
        {
            // Regression: the old SortedList implementation bumped e.Ticks (and
            // recursed) when two events landed on the same tick.
            var a = NewEvent(new Collector(), "a", 12345);
            var b = NewEvent(new Collector(), "b", 12345);

            _handler.AddEvent(a);
            _handler.AddEvent(b);

            Assert.Equal(12345, a.Ticks);
            Assert.Equal(12345, b.Ticks);
        }

        [Fact]
        public void Update_LeavesNotYetDueEventsQueued()
        {
            var collector = new Collector();
            long now = _world.TimeNow;

            var due = NewEvent(collector, "due", now - 100);
            var future = NewEvent(collector, "future", now + 10_000_000);

            _handler.AddEvent(future);
            _handler.AddEvent(due);

            _handler.Update(_world);
            Assert.Equal(new[] { "due" }, collector.Executed);

            // A second due event must run while the future one is still skipped.
            var due2 = NewEvent(collector, "due2", now - 50);
            _handler.AddEvent(due2);

            _handler.Update(_world);
            Assert.Equal(new[] { "due", "due2" }, collector.Executed);
        }

        [Fact]
        public void RemoveEvent_CancelsAPendingEvent()
        {
            var collector = new Collector();
            long now = _world.TimeNow;

            var removed = NewEvent(collector, "removed", now - 100);
            var kept = NewEvent(collector, "kept", now - 50);

            _handler.AddEvent(removed);
            _handler.AddEvent(kept);

            _handler.RemoveEvent(removed);

            _handler.Update(_world);

            Assert.Equal(new[] { "kept" }, collector.Executed);
        }

        [Fact]
        public void Update_ContinuesAfterAnEventThrows()
        {
            var collector = new Collector();
            long now = _world.TimeNow;

            var bad = new RecordingEvent
            {
                Name = "bad",
                Ticks = now - 100,
                OnReady = (_, _) => throw new InvalidOperationException("boom"),
            };
            var good = NewEvent(collector, "good", now - 50);

            _handler.AddEvent(bad);
            _handler.AddEvent(good);

            _handler.Update(_world); // must not throw

            Assert.Equal(new[] { "good" }, collector.Executed);
        }

        [Fact]
        public void Update_RescheduledEventRunsOnceAndIsNotReprocessedInSameUpdate()
        {
            var collector = new Collector();
            long now = _world.TimeNow;

            var recurring = new RecordingEvent
            {
                Name = "recurring",
                Ticks = now - 100,
                OnReady = (ev, world) =>
                {
                    collector.Executed.Add(ev.Name ?? "");
                    // Mimics ScriptTimerEvent.Reschedule / BuffTickEvent: re-enqueue
                    // with a future tick.
                    ev.Ticks = world.TimeNow + 10_000_000;
                    world.EventHandler.AddEvent(ev);
                },
            };

            _handler.AddEvent(recurring);

            _handler.Update(_world);
            Assert.Equal(new[] { "recurring" }, collector.Executed);

            // Not due yet on the next update, so it must not run again.
            _handler.Update(_world);
            Assert.Equal(new[] { "recurring" }, collector.Executed);
        }
    }
}
