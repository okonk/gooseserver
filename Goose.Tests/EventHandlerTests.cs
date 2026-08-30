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

        [Fact]
        public void RegisterEvent_slash_factory_reregistration_runs_only_new_factory()
        {
            using var world = new TestWorldFixture();
            var map = world.AddBaseMap(1, "Test");
            var player = world.CommandPlayerOn(map, 1, 1);

            world.World.EventHandler.RegisterEvent("/evil ", (p, d) => new FirstFactoryEvent { Player = p, Data = d });
            world.World.EventHandler.RegisterEvent("/evil ", (p, d) => new SecondFactoryEvent { Player = p, Data = d });

            Assert.True(world.World.Commands.TryGet("/evil ", out var def));
            Assert.NotNull(def!.LegacyFactory);

            Assert.True(world.RunCommand(player, "/evil "));
            Assert.Contains(player.Sent, s => s.Contains("second ran"));
            Assert.DoesNotContain(player.Sent, s => s.Contains("first ran"));
            Assert.True(SecondFactoryEvent.ClientOriginatedSeen);
        }

        [Fact]
        public void RegisterEvent_null_slash_factory_refuses_and_keeps_prior_definition()
        {
            using var world = new TestWorldFixture();
            var map = world.AddBaseMap(1, "Test");
            var player = world.CommandPlayerOn(map, 1, 1);

            world.World.EventHandler.RegisterEvent("/null ", (p, d) => new NullFactoryEvent { Player = p, Data = d });
            var snapshot = world.World.Commands.Snapshot;
            Assert.True(world.World.Commands.TryGet("/null ", out var def));
            Assert.NotNull(def!.LegacyFactory);

            world.World.EventHandler.RegisterEvent("/null ", null!);

            Assert.Same(snapshot, world.World.Commands.Snapshot);
            Assert.True(world.World.Commands.TryGet("/null ", out def));
            Assert.NotNull(def!.LegacyFactory);

            Assert.True(world.RunCommand(player, "/null "));
            Assert.Contains(player.Sent, s => s.Contains("null ran"));
        }

        [Fact]
        public void RegisterEvent_restricted_slash_factory_is_swallowed_for_normal_and_runs_for_gm()
        {
            using var world = new TestWorldFixture();
            var map = world.AddBaseMap(1, "Test");
            var player = world.CommandPlayerOn(map, 1, 1);

            world.World.EventHandler.RegisterEvent("/restr ", (p, d) => new RestrictedFactoryEvent { Player = p, Data = d },
                AccessPrivilege.Ban);

            Assert.True(world.World.Commands.TryGet("/restr ", out var def));
            Assert.Equal(AccessPrivilege.Ban, def!.Privilege);
            Assert.NotNull(def.LegacyFactory);

            Assert.True(world.RunCommand(player, "/restr "));
            Assert.Empty(player.Sent);

            var gm = world.CommandPlayerOn(map, 2, 2, "GM");
            gm.Access = Player.AccessStatus.GameMaster;
            Assert.True(world.RunCommand(gm, "/restr "));
            Assert.Contains(gm.Sent, s => s.Contains("restr ran"));
        }
    }

    internal sealed class FirstFactoryEvent : Event
    {
        public override void Ready(GameWorld world) => world.Send(this.Player, "first ran");
    }

    internal sealed class SecondFactoryEvent : Event
    {
        public static bool ClientOriginatedSeen;

        public override void Ready(GameWorld world)
        {
            ClientOriginatedSeen = this.ClientOriginated;
            world.Send(this.Player, "second ran");
        }
    }

    internal sealed class NullFactoryEvent : Event
    {
        public override void Ready(GameWorld world) => world.Send(this.Player, "null ran");
    }

    internal sealed class RestrictedFactoryEvent : Event
    {
        public override void Ready(GameWorld world) => world.Send(this.Player, "restr ran");
    }
}
