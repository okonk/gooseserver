using System;
using Goose;
using Goose.Events;
using Xunit;

namespace Goose.Tests
{
    // Regression tests for H6 (docs/code-review-2026-08-15.md): a 0/negative
    // interval setting re-enqueues a recurring event at or before now, and
    // EventHandler.Update then spins forever.
    [Collection(Goose.Tests.Collections.GameWorldSettingsCollection.Name)]
    public class EventHandlerIntervalTests
    {
        private readonly GameWorld _world;
        private readonly EventHandler _handler;

        public EventHandlerIntervalTests()
        {
            // PlayerCountExperienceModifierUpdateEvent.Ready divides by this interval.
            var settings = new GooseSettings { PlayerCountExperienceModifierInterval = 1000 };
            _world = new GameWorld(settings, new GameServer(settings));
            _handler = _world.EventHandler;
        }

        [Fact]
        public void ClearMapItemsEvent_ZeroSweepTime_ReschedulesInFuture()
        {
            _world.Configuration.ItemGroundSweepTime = 0;

            long before = _world.TimeNow;
            var ev = new ClearMapItemsEvent
            {
                Data = new Map(),
                Ticks = before,
            };

            ev.Ready(_world); // re-enqueues itself

            Assert.True(ev.Ticks > before);
            _handler.Update(_world); // must return, not spin
        }

        [Fact]
        public void PlayerCountExperienceModifierUpdateEvent_ZeroIdleTimeout_ReschedulesInFuture()
        {
            _world.Configuration.IdleTimeout = 0;

            long before = _world.TimeNow;
            var ev = new PlayerCountExperienceModifierUpdateEvent
            {
                Ticks = before,
            };

            ev.Ready(_world); // re-enqueues itself

            Assert.True(ev.Ticks > before);
            _handler.Update(_world); // must return, not spin
        }

        [Fact]
        public void ScriptTimerEvent_Create_ZeroPeriod_SchedulesAfterNow()
        {
            long before = _world.TimeNow;

            var ev = ScriptTimerEvent.Create(() => { }, TimeSpan.Zero, _world);

            Assert.True(ev.Ticks > before);
            _handler.Update(_world); // must return, not spin
        }

        [Fact]
        public void ScriptTimerEvent_Reschedule_ZeroPeriod_ReschedulesAfterNow()
        {
            long before = _world.TimeNow;
            var ev = new ScriptTimerEvent
            {
                Ticks = _world.TimeNow,
            };

            ev.Reschedule(TimeSpan.Zero, _world);

            // The clamp is one tick (one second), so assert against the scheduling
            // point, not a later TimeNow read, which has already passed it under load.
            Assert.True(ev.Ticks > before);
            _handler.Update(_world); // must return, not spin
        }

        [Fact]
        public void AddSaveEvent_ZeroPlayerSavePeriod_EnqueuesFutureSave()
        {
            _world.Configuration.PlayerSavePeriod = 0;

            var player = new Player(0);

            int beforeCount = _handler.Count;
            long before = _world.TimeNow;
            player.AddSaveEvent(_world);

            Assert.Equal(beforeCount + 1, _handler.Count);
            var ev = Assert.IsType<PlayerSaveEvent>(_handler.Peek());
            Assert.Same(player, ev.Player);
            Assert.True(ev.Ticks > before);

            _handler.Update(_world); // must return, not spin
            Assert.Equal(beforeCount + 1, _handler.Count);
        }

        [Fact]
        public void AddRegenEvent_ZeroRegenSpeed_EnqueuesFutureRegen()
        {
            _world.Configuration.RegenSpeed = 0m;

            var player = new Player(0)
            {
                State = Player.States.Ready,
                TemporaryMaxHP = 10,
                TemporaryMaxMP = 10,
                TemporaryMaxSP = 10,
                CurrentHP = 5,
                CurrentMP = 5,
                CurrentSP = 5,
            };

            int beforeCount = _handler.Count;
            long before = _world.TimeNow;
            player.AddRegenEvent(_world);

            Assert.Equal(beforeCount + 1, _handler.Count);
            var ev = Assert.IsType<RegenEvent>(_handler.Peek());
            Assert.Same(player, ev.Data);
            Assert.True(ev.Ticks > before);

            _handler.Update(_world); // must return, not spin
            Assert.Equal(beforeCount + 1, _handler.Count);
        }

        [Fact]
        public void GuildSaveEvent_ZeroGuildSavePeriod_ReschedulesInFuture()
        {
            _world.Configuration.GuildSavePeriod = 0;

            // Save re-enqueues a fresh GuildSaveEvent, not the instance that ran.
            int beforeCount = _handler.Count;
            long before = _world.TimeNow;
            new GuildSaveEvent { Ticks = before }.Ready(_world);

            Assert.Equal(beforeCount + 1, _handler.Count);
            var rescheduled = Assert.IsType<GuildSaveEvent>(_handler.Peek());
            Assert.True(rescheduled.Ticks > before);

            _handler.Update(_world); // must return, not spin
            Assert.Equal(beforeCount + 1, _handler.Count);
        }
    }
}
