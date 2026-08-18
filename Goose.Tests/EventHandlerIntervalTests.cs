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
            _world = new GameWorld(new GameServer());
            _handler = _world.EventHandler;
        }

        [Fact]
        public void ClearMapItemsEvent_ZeroSweepTime_ReschedulesInFuture()
        {
            int old = GameWorld.Settings.ItemGroundSweepTime;
            GameWorld.Settings.ItemGroundSweepTime = 0;
            try
            {
                var ev = new ClearMapItemsEvent
                {
                    Data = new Map(),
                    Ticks = _world.TimeNow,
                };

                ev.Ready(_world); // re-enqueues itself

                Assert.True(ev.Ticks > _world.TimeNow);
                _handler.Update(_world); // must return, not spin
            }
            finally
            {
                GameWorld.Settings.ItemGroundSweepTime = old;
            }
        }

        [Fact]
        public void PlayerCountExperienceModifierUpdateEvent_ZeroIdleTimeout_ReschedulesInFuture()
        {
            int old = GameWorld.Settings.IdleTimeout;
            GameWorld.Settings.IdleTimeout = 0;
            try
            {
                var ev = new PlayerCountExperienceModifierUpdateEvent
                {
                    Ticks = _world.TimeNow,
                };

                ev.Ready(_world); // re-enqueues itself

                Assert.True(ev.Ticks > _world.TimeNow);
                _handler.Update(_world); // must return, not spin
            }
            finally
            {
                GameWorld.Settings.IdleTimeout = old;
            }
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

            // The clamp is one tick, so assert against the scheduling point, not a
            // later TimeNow read (which has already passed now + 1 tick).
            Assert.True(ev.Ticks > before);
            _handler.Update(_world); // must return, not spin
        }

        [Fact]
        public void AddSaveEvent_ZeroPlayerSavePeriod_EnqueuesFutureSave()
        {
            int old = GameWorld.Settings.PlayerSavePeriod;
            GameWorld.Settings.PlayerSavePeriod = 0;
            try
            {
                var player = new Player(0);

                int before = _handler.Count;
                player.AddSaveEvent(_world);

                Assert.Equal(before + 1, _handler.Count);
                var ev = Assert.IsType<PlayerSaveEvent>(_handler.Peek());
                Assert.Same(player, ev.Player);
                Assert.True(ev.Ticks > _world.TimeNow);

                _handler.Update(_world); // must return, not spin
                Assert.Equal(before + 1, _handler.Count);
            }
            finally
            {
                GameWorld.Settings.PlayerSavePeriod = old;
            }
        }

        [Fact]
        public void AddRegenEvent_ZeroRegenSpeed_EnqueuesFutureRegen()
        {
            decimal old = GameWorld.Settings.RegenSpeed;
            GameWorld.Settings.RegenSpeed = 0m;
            try
            {
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

                int before = _handler.Count;
                player.AddRegenEvent(_world);

                Assert.Equal(before + 1, _handler.Count);
                var ev = Assert.IsType<RegenEvent>(_handler.Peek());
                Assert.Same(player, ev.Data);
                Assert.True(ev.Ticks > _world.TimeNow);

                _handler.Update(_world); // must return, not spin
                Assert.Equal(before + 1, _handler.Count);
            }
            finally
            {
                GameWorld.Settings.RegenSpeed = old;
            }
        }

        [Fact]
        public void GuildSaveEvent_ZeroGuildSavePeriod_ReschedulesInFuture()
        {
            int old = GameWorld.Settings.GuildSavePeriod;
            GameWorld.Settings.GuildSavePeriod = 0;
            try
            {
                // Save re-enqueues a fresh GuildSaveEvent, not the instance that ran.
                int before = _handler.Count;
                new GuildSaveEvent { Ticks = _world.TimeNow }.Ready(_world);

                Assert.Equal(before + 1, _handler.Count);
                var rescheduled = Assert.IsType<GuildSaveEvent>(_handler.Peek());
                Assert.True(rescheduled.Ticks > _world.TimeNow);

                _handler.Update(_world); // must return, not spin
                Assert.Equal(before + 1, _handler.Count);
            }
            finally
            {
                GameWorld.Settings.GuildSavePeriod = old;
            }
        }
    }
}
