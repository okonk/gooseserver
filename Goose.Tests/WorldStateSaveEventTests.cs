using System.Text.Json;
using Goose.Events;
using Goose.Testing;
using Xunit;

namespace Goose.Tests
{
    public class WorldStateSaveEventTests
    {
        [Fact]
        public void Save_NoChanges_EnqueuesNoDbWork()
        {
            using var fixture = new TestWorldFixture();
            var world = fixture.World;
            var state = new WorldState();

            var plan = state.PlanSave();
            Assert.Empty(plan.Upserts);
            Assert.Empty(plan.Deletes);

            state.Save(world);
        }

        [Fact]
        public void Save_RearmsWorldSaveEvent()
        {
            using var fixture = new TestWorldFixture();
            var world = fixture.World;
            var state = new WorldState();
            state.LoadRows(new[] { new KeyValuePair<string, string>("probe:key", "[]") });

            int before = world.EventHandler.Count;
            state.Save(world);

            Assert.Equal(before + 1, world.EventHandler.Count);
            Assert.IsType<WorldSaveEvent>(world.EventHandler.Peek());
        }

        [Fact]
        public void MissingSettingDefaultsTo300()
        {
            var settings = JsonSerializer.Deserialize<GooseSettings>("{}", JsonHelper.SettingsOptions)!;
            Assert.Equal(300, settings.WorldSavePeriod);
        }
    }
}
