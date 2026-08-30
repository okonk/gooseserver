using Goose;
using Goose.Commands;
using Goose.Testing;
using Xunit;

namespace Goose.Tests
{
    public class EventHandlerTests
    {
        [Fact]
        public void Register_ReplacingRestrictedLegacyCommandWithOpenRefused()
        {
            using var world = new TestWorldFixture();

            Assert.False(world.World.Commands.Register("/shutdown", "Admin", "shut down", new Action<CommandContext>(_ => { })));

            Assert.True(world.World.Commands.TryGet("/shutdown", out var def));
            Assert.Equal(AccessPrivilege.Shutdown, def!.Privilege);
        }
    }
}
