using System.IO;
using System.Text.Json;
using Goose.Events;
using Goose.Testing;
using Xunit;

namespace Goose.Tests
{
    public class LogSaveEventTests
    {
        [Fact]
        public void ScheduleLogSave_EnqueuesLogSaveEvent_EvenWithPlayerCountScalingDisabled()
        {
            using var fixture = new TestWorldFixture(s => s.PlayerCountExperienceModifierEnabled = false);
            var world = fixture.World;

            int before = world.EventHandler.Count;
            world.ScheduleLogSave();

            Assert.Equal(before + 1, world.EventHandler.Count);
            Assert.IsType<LogSaveEvent>(world.EventHandler.Peek());
        }

        [Fact]
        public void Ready_FlushesPendingLogsAndReschedules()
        {
            using var fixture = new TestWorldFixture();
            var world = fixture.World;
            StartLogsDatabase(fixture);
            world.LogHandler.Log(Log.Types.ViewLogs, 1, "probe");

            int before = world.EventHandler.Count;
            new LogSaveEvent { Ticks = world.TimeNow }.Ready(world);

            Assert.Empty(world.LogHandler.Pending);
            long saved = world.Database.Execute(conn =>
            {
                using var command = conn.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM logs WHERE text = 'probe';";
                return (long)command.ExecuteScalar()!;
            });
            Assert.Equal(1, saved);
            Assert.Equal(before + 1, world.EventHandler.Count);
            Assert.IsType<LogSaveEvent>(world.EventHandler.Peek());
        }

        [Fact]
        public void ZeroLogSavePeriod_ReschedulesInFuture()
        {
            using var fixture = new TestWorldFixture();
            var world = fixture.World;
            world.Settings.LogSavePeriod = 0;

            int beforeCount = world.EventHandler.Count;
            long before = world.TimeNow;
            new LogSaveEvent { Ticks = before }.Ready(world);

            var rescheduled = Assert.IsType<LogSaveEvent>(world.EventHandler.Peek());
            Assert.True(rescheduled.Ticks > before);

            world.EventHandler.Update(world);
            Assert.Equal(beforeCount + 1, world.EventHandler.Count);
        }

        [Fact]
        public void MissingSettingDefaultsTo600()
        {
            var settings = JsonSerializer.Deserialize<GooseSettings>("{}", JsonHelper.SettingsOptions)!;
            Assert.Equal(600, settings.LogSavePeriod);
        }

        private static void StartLogsDatabase(TestWorldFixture fixture)
        {
            fixture.World.Database.Start(Path.Combine(fixture.DataDirectory, "test.db"));
            fixture.World.Database.Execute(conn =>
            {
                using var command = conn.CreateCommand();
                command.CommandText = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "sql", "logs.sql"));
                command.ExecuteNonQuery();
            });
        }
    }
}
