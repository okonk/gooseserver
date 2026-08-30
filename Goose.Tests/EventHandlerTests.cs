using Goose;
using Goose.Commands;
using Goose.Testing;
using Xunit;

namespace Goose.Tests
{
    [Collection("NLog")]
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

        [Fact]
        public void RegisterEvent_slash_key_rejected_not_registered_and_error_logged()
        {
            using var log = new CapturingLog();
            using var world = new TestWorldFixture();
            var map = world.AddBaseMap(1, "Test");
            var player = world.CommandPlayerOn(map, 1, 1);

            world.World.EventHandler.RegisterEvent("/evil ", (p, d) => new SlashRejectedEvent { Player = p, Data = d });

            Assert.False(world.World.Commands.TryGet("/evil ", out _));
            Assert.False(world.RunCommand(player, "/evil "));
            Assert.Empty(player.Sent);
            Assert.Contains(log.Messages, m => m.Contains("/evil"));
        }

        [Fact]
        public void RegisterEvent_non_slash_key_registers_and_dispatches()
        {
            using var world = new TestWorldFixture();
            var map = world.AddBaseMap(1, "Test");
            var player = world.CommandPlayerOn(map, 1, 1);

            world.World.EventHandler.RegisterEvent("GID", (p, d) => new GidStubEvent { Player = p, Data = d });

            Assert.True(world.RunCommand(player, "GID"));
            Assert.Contains(player.Sent, s => s.Contains("gid ran"));
        }
    }

    internal sealed class SlashRejectedEvent : Event
    {
        public override void Ready(GameWorld world) => world.Send(this.Player, "slash ran");
    }

    internal sealed class GidStubEvent : Event
    {
        public override void Ready(GameWorld world) => world.Send(this.Player, "gid ran");
    }
}
