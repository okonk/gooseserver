using System.IO;

using Goose;
using Goose.Events;
using Goose.Testing;
using Xunit;

namespace Goose.Tests
{
    public class PlayerCountExperienceSettingTests
    {
        [Fact]
        public void Scaling_event_is_not_scheduled_when_disabled()
        {
            using TestWorldFixture fixture = new(s => s.PlayerCountExperienceModifierEnabled = false);

            int before = fixture.World.EventHandler.Count;
            fixture.World.SchedulePlayerCountExperienceModifier();

            Assert.Equal(before, fixture.World.EventHandler.Count);
        }

        [Fact]
        public void Scaling_event_is_scheduled_when_enabled()
        {
            using TestWorldFixture fixture = new(s => s.PlayerCountExperienceModifierEnabled = true);

            int before = fixture.World.EventHandler.Count;
            fixture.World.SchedulePlayerCountExperienceModifier();

            Assert.Equal(before + 1, fixture.World.EventHandler.Count);
            Assert.IsType<PlayerCountExperienceModifierUpdateEvent>(fixture.World.EventHandler.Peek());
        }

        [Fact]
        public void Shipped_settings_disable_scaling()
        {
            string gooseDir = FindGooseDirectory();

            GooseSettings settings = GooseSettingsLoader.Load(gooseDir, gooseDir);

            Assert.False(settings.PlayerCountExperienceModifierEnabled);
        }

        private static string FindGooseDirectory()
        {
            string dir = AppContext.BaseDirectory;
            while (true)
            {
                string candidate = Path.Combine(dir, "Goose", "GooseSettings.json");
                if (File.Exists(candidate)) return Path.Combine(dir, "Goose");
                string? parent = Path.GetDirectoryName(dir);
                if (parent is null) throw new FileNotFoundException("Goose/GooseSettings.json", candidate);
                dir = parent;
            }
        }
    }
}
