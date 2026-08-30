using Goose;
using Goose.Commands;
using Goose.Testing;
using Xunit;

namespace Goose.Tests
{
    public class CommandDispatchTests
    {
        private static void IntHandler(CommandContext ctx, int n) => ctx.Send($"testcmd {n}");
        private static void RestrictedHandler(CommandContext ctx) => ctx.Send("restricted ran");

        private static (TestWorldFixture world, TestWorldFixture.CapturingPlayer player, Map map) WorldAndPlayer(
            Player.AccessStatus access = Player.AccessStatus.Normal)
        {
            var world = new TestWorldFixture();
            var map = world.AddBaseMap(1, "Test");
            var player = world.CommandPlayerOn(map, 1, 1);
            player.Access = access;
            return (world, player, map);
        }

        [Fact]
        public void RegisteredOpenCommandRuns()
        {
            var (world, player, _) = WorldAndPlayer();
            Assert.True(world.World.Commands.Register("/testcmd ", "Test", "test command", IntHandler));

            Assert.True(world.RunCommand(player, "/testcmd 5"));
            Assert.Contains(player.Sent, s => s.Contains("testcmd 5"));
        }

        [Fact]
        public void RegisteredRestrictedCommandNormalPlayerSwallowed()
        {
            var (world, player, _) = WorldAndPlayer();
            Assert.True(world.World.Commands.Register("/restricted", AccessPrivilege.Ban, "Test", "restricted", RestrictedHandler));

            Assert.True(world.RunCommand(player, "/restricted"));
            Assert.Empty(player.Sent);
        }

        [Fact]
        public void RegisteredRestrictedCommandGMRuns()
        {
            var (world, player, _) = WorldAndPlayer(Player.AccessStatus.GameMaster);
            Assert.True(world.World.Commands.Register("/restricted", AccessPrivilege.Ban, "Test", "restricted", RestrictedHandler));

            Assert.True(world.RunCommand(player, "/restricted"));
            Assert.Contains(player.Sent, s => s.Contains("restricted ran"));
        }

        [Fact]
        public void LegacyWhoUnchanged()
        {
            var (world, player, map) = WorldAndPlayer();
            map.Players.Add(player);

            Assert.True(world.RunCommand(player, "/who"));
            Assert.Contains(player.Sent, s => s.Contains("#[Matched 1 players]"));
            Assert.Contains(player.Sent, s => s.Contains("#[Test] ") && s.Contains("Tester (Level 0 Default)"));
        }

        [Fact]
        public void LegacyRestrictedCommandStillGated()
        {
            var (world, player, map) = WorldAndPlayer();
            world.World.Running = true;

            Assert.True(world.RunCommand(player, "/shutdown"));
            Assert.True(world.World.Running);
            Assert.Empty(player.Sent);

            var gm = world.CommandPlayerOn(map, 2, 2, "GM");
            gm.Access = Player.AccessStatus.GameMaster;
            Assert.True(world.RunCommand(gm, "/shutdown"));
            Assert.False(world.World.Running);
        }

        [Fact]
        public void UnknownPlayerParamSendsFixedMessage()
        {
            var (world, player, _) = WorldAndPlayer();
            Assert.True(world.World.Commands.Register("/itell ", "General", "Test.",
                (CommandContext ctx, Player target) => ctx.Send("told " + target.Name)));

            Assert.True(world.RunCommand(player, "/itell nosuchplayer"));
            Assert.Equal([P.ServerMessage("Couldn't find player nosuchplayer.") + "\x01"], player.Sent);
        }

        [Fact]
        public void StaticExecuteCommandRuns()
        {
            var (world, player, _) = WorldAndPlayer();
            world.World.Commands.SeedAttributedTypes([typeof(StaticExecuteCommand), typeof(StaticSubCommand)]);

            Assert.True(world.RunCommand(player, "/sstatic"));
            Assert.Contains(player.Sent, s => s.Contains("static ran"));

            Assert.True(world.RunCommand(player, "/ssub spin"));
            Assert.Contains(player.Sent, s => s.Contains("static spin ran"));
        }

        [Fact]
        public void UnknownCommandNoMatch()
        {
            var (world, player, _) = WorldAndPlayer();

            Assert.False(world.RunCommand(player, "/nope"));
            Assert.Empty(player.Sent);
        }

        [Fact]
        public void NonCommandPacketsStillDispatch()
        {
            var (world, player, map) = WorldAndPlayer();
            map.CanChat = true;

            Assert.True(world.RunCommand(player, ";hello"));
            Assert.Contains(player.Sent, s => s.Contains("^0,Tester: hello"));

            Assert.True(world.RunCommand(player, "PONG"));
        }

        [Fact]
        public void ParseErrorSendsUsage()
        {
            var (world, player, _) = WorldAndPlayer();
            Assert.True(world.World.Commands.Register("/narg", "Test", "test command", IntHandler));

            Assert.True(world.RunCommand(player, "/narg"));
            Assert.Contains(player.Sent, s => s.Contains("Usage: /narg <n>"));
        }

        [Fact]
        public void NotReadyPlayerCommandNoop()
        {
            var (world, player, _) = WorldAndPlayer();
            Assert.True(world.World.Commands.Register("/testcmd ", "Test", "test command", IntHandler));
            player.State = Player.States.LoadingMap;

            Assert.True(world.RunCommand(player, "/testcmd 5"));
            Assert.Empty(player.Sent);
        }

        [Fact]
        public void CheckAccessOverrideNormalDeniedGMAllowed()
        {
            var (world, player, map) = WorldAndPlayer();
            world.World.Commands.SeedAttributedTypes([typeof(GatedCommand)]);

            Assert.True(world.RunCommand(player, "/gated invis"));
            Assert.Empty(player.Sent);

            var gm = world.CommandPlayerOn(map, 2, 2, "GM");
            gm.Access = Player.AccessStatus.GameMaster;
            Assert.True(world.RunCommand(gm, "/gated invis"));
            Assert.Contains(gm.Sent, s => s.Contains("gated ran"));
        }

        [Fact]
        public void SubcommandBareKeySendsPrivilegeFilteredList()
        {
            var (world, player, _) = WorldAndPlayer();
            world.World.Commands.SeedAttributedTypes([typeof(CustomCommand)]);

            Assert.True(world.RunCommand(player, "/tcustom"));
            Assert.Contains(player.Sent, s => s.Contains("make") &&
                s.Contains("Usage: /tcustom make <r> <g> <b> <a> <name>"));
            Assert.DoesNotContain(player.Sent, s => s.Contains("delete"));
        }

        [Fact]
        public void SubcommandUnknownSendsList()
        {
            var (world, player, _) = WorldAndPlayer();
            world.World.Commands.SeedAttributedTypes([typeof(CustomCommand)]);

            Assert.True(world.RunCommand(player, "/tcustom bogus"));
            Assert.Contains(player.Sent, s => s.Contains("make") &&
                s.Contains("Usage: /tcustom make <r> <g> <b> <a> <name>"));
            Assert.DoesNotContain(player.Sent, s => s.Contains("delete"));
        }

        [Fact]
        public void SubcommandBindsTokensAfterSubcommandToken()
        {
            var (world, player, _) = WorldAndPlayer();
            world.World.Commands.SeedAttributedTypes([typeof(CustomCommand)]);

            Assert.True(world.RunCommand(player, "/tcustom make 1 2 3 4 Name"));
            Assert.Contains(player.Sent, s => s.Contains("made 1 2 3 4 Name"));
        }

        [Fact]
        public void SubcommandListGMSeesRestrictedSub()
        {
            var (world, player, _) = WorldAndPlayer(Player.AccessStatus.GameMaster);
            world.World.Commands.SeedAttributedTypes([typeof(CustomCommand)]);

            Assert.True(world.RunCommand(player, "/tcustom"));
            Assert.Contains(player.Sent, s => s.Contains("make"));
            Assert.Contains(player.Sent, s => s.Contains("delete"));
        }

        [Fact]
        public void SubcommandPrivilegeDeniesNormal()
        {
            var (world, player, _) = WorldAndPlayer();
            world.World.Commands.SeedAttributedTypes([typeof(CustomCommand)]);

            Assert.True(world.RunCommand(player, "/tcustom delete x"));
            Assert.Empty(player.Sent);
        }

        [Fact]
        public void CheckAccessDenialPrecedesSubcommandList()
        {
            var (world, player, map) = WorldAndPlayer();
            world.World.Commands.SeedAttributedTypes([typeof(DeniedCommand)]);

            Assert.True(world.RunCommand(player, "/denied"));
            Assert.True(world.RunCommand(player, "/denied bogus"));
            Assert.Empty(player.Sent);

            var gm = world.CommandPlayerOn(map, 2, 2, "GM");
            gm.Access = Player.AccessStatus.GameMaster;
            Assert.True(world.RunCommand(gm, "/denied sub"));
            Assert.Contains(gm.Sent, s => s.Contains("sub ran"));
        }

        [Fact]
        public void RegisterEventSlashKeyStillWorks()
        {
            var (world, player, _) = WorldAndPlayer();
            world.World.EventHandler.RegisterEvent("/evil ", (p, d) => new StubEvent { Player = p, Data = d });

            Assert.True(world.RunCommand(player, "/evil "));
            Assert.Contains(player.Sent, s => s.Contains("evil ran"));
        }

        [Fact]
        public void RegisterEventNonSlashKeyStillWorks()
        {
            var (world, player, _) = WorldAndPlayer();
            world.World.EventHandler.RegisterEvent("GID", (p, d) => new StubEvent { Player = p, Data = d });

            Assert.True(world.RunCommand(player, "GID"));
            Assert.Contains(player.Sent, s => s.Contains("evil ran"));
        }

        [Fact]
        public void RegisterEventOpenFactoryShadowedByRestrictedLegacyCommand()
        {
            var (world, player, map) = WorldAndPlayer();
            var factoryCalled = false;
            world.World.EventHandler.RegisterEvent("/shutdown", (p, d) =>
            {
                factoryCalled = true;
                return new StubEvent { Player = p, Data = d };
            });
            world.World.Running = true;

            Assert.True(world.RunCommand(player, "/shutdown"));
            Assert.True(world.World.Running);
            Assert.Empty(player.Sent);
            Assert.False(factoryCalled);

            var gm = world.CommandPlayerOn(map, 2, 2, "GM");
            gm.Access = Player.AccessStatus.GameMaster;
            Assert.True(world.RunCommand(gm, "/shutdown"));
            Assert.False(world.World.Running);
            Assert.False(factoryCalled);
        }
    }

    [Collection("NLog")]
    public class CommandDispatchLoggingTests
    {
        private static void ThrowingHandler(CommandContext ctx) => throw new InvalidOperationException("boom");

        [Fact]
        public void RegisterEventSlashKeyLogsWarning()
        {
            using var log = new CapturingLog();
            var world = new TestWorldFixture();
            world.World.EventHandler.RegisterEvent("/evil ", (p, d) => new StubEvent { Player = p, Data = d });

            Assert.Contains(log.Messages, m => m.Contains("/evil"));
        }

        [Fact]
        public void ThrowingHandlerLogsOriginalExceptionType()
        {
            using var log = new CapturingLog();
            var world = new TestWorldFixture();
            var map = world.AddBaseMap(1, "Test");
            var player = world.CommandPlayerOn(map, 1, 1);
            player.Access = Player.AccessStatus.Normal;
            Assert.True(world.World.Commands.Register("/boom", "Test", "test command", ThrowingHandler));

            world.RunCommand(player, "/boom");

            Assert.Contains(log.Messages, m => m.Contains("InvalidOperationException"));
            Assert.DoesNotContain(log.Messages, m => m.Contains("TargetInvocationException"));
        }
    }

    internal sealed class StubEvent : Event
    {
        public override void Ready(GameWorld world) => world.Send(this.Player, "evil ran");
    }

    [Command("/gated", Help = "Test gated command.")]
    internal sealed class GatedCommand : BaseCommand
    {
        protected override AccessPrivilege? CheckAccess(CommandContext ctx, string[] args)
            => args.Contains("invis") ? AccessPrivilege.Ban : null;

        public void Execute(CommandContext ctx) => ctx.Send("gated ran");
    }

    [Command("/tcustom", Help = "Test custom command.")]
    internal sealed class CustomCommand : BaseCommand
    {
        [Subcommand("make")]
        public void Make(CommandContext ctx, int r, int g, int b, int a, string name)
            => ctx.Send($"made {r} {g} {b} {a} {name}");

        [Subcommand("delete", AccessPrivilege.Ban)]
        public void Delete(CommandContext ctx, string name) => ctx.Send($"deleted {name}");
    }

    [Command("/denied", Help = "Test denied command.")]
    internal sealed class DeniedCommand : BaseCommand
    {
        protected override AccessPrivilege? CheckAccess(CommandContext ctx, string[] args) => AccessPrivilege.Ban;

        [Subcommand("sub")]
        public void Sub(CommandContext ctx) => ctx.Send("sub ran");
    }
}
