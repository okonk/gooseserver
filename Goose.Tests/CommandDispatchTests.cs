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
        public void Register_closed_static_delegate_is_accepted_and_runs()
        {
            var (world, player, _) = WorldAndPlayer();
            var capture = new ClosedCapture();
            var closed = (Action<CommandContext, int>)Delegate.CreateDelegate(
                typeof(Action<CommandContext, int>), capture,
                typeof(ClosedDelegateTargets).GetMethod("ClosedStatic")!);

            Assert.True(world.World.Commands.Register("/closed", "Test", "closed test", closed));

            Assert.True(world.RunCommand(player, "/closed 7"));
            Assert.Equal(7, capture.Value);
            Assert.Contains(player.Sent, s => s.Contains("closed 7"));

            Assert.True(world.RunCommand(player, "/closed"));
            Assert.Contains(player.Sent, s => s.Contains("Usage: /closed <n>"));
            Assert.DoesNotContain(player.Sent, s => s.Contains("capture"));
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
            Assert.DoesNotContain(player.Sent, s => s.Contains("Usage: /tcustom make"));
        }

        [Fact]
        public void MixedCommand_bare_key_runs_default_execute()
        {
            var (world, player, _) = WorldAndPlayer();
            world.World.Commands.SeedAttributedTypes([typeof(MixedCommand)]);

            Assert.True(world.RunCommand(player, "/tmixed"));
            Assert.Contains(player.Sent, s => s.Contains("default"));
            Assert.DoesNotContain(player.Sent, s => s.Contains("Usage: /tmixed"));
        }

        [Fact]
        public void MixedCommand_matching_selector_runs_subcommand()
        {
            var (world, player, _) = WorldAndPlayer();
            world.World.Commands.SeedAttributedTypes([typeof(MixedCommand)]);

            Assert.True(world.RunCommand(player, "/tmixed make"));
            Assert.Contains(player.Sent, s => s.Contains("mixed make ran"));
            Assert.DoesNotContain(player.Sent, s => s.Contains("exec make"));
        }

        [Fact]
        public void MixedCommand_unknown_selector_falls_through_to_execute()
        {
            var (world, player, _) = WorldAndPlayer();
            world.World.Commands.SeedAttributedTypes([typeof(MixedCommand)]);

            Assert.True(world.RunCommand(player, "/tmixed bogus"));
            Assert.Contains(player.Sent, s => s.Contains("exec bogus"));
            Assert.DoesNotContain(player.Sent, s => s.Contains("Usage: /tmixed"));
        }

        [Fact]
        public void MixedCommand_restricted_selector_falls_through_for_normal_and_runs_for_gm()
        {
            var (world, player, map) = WorldAndPlayer();
            world.World.Commands.SeedAttributedTypes([typeof(MixedCommand)]);

            Assert.True(world.RunCommand(player, "/tmixed secret"));
            Assert.Contains(player.Sent, s => s.Contains("exec secret"));
            Assert.DoesNotContain(player.Sent, s => s.Contains("mixed secret ran"));
            Assert.DoesNotContain(player.Sent, s => s.Contains("Usage: /tmixed"));

            var gm = world.CommandPlayerOn(map, 2, 2, "GM");
            gm.Access = Player.AccessStatus.GameMaster;
            Assert.True(world.RunCommand(gm, "/tmixed secret"));
            Assert.Contains(gm.Sent, s => s.Contains("mixed secret ran"));
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

            Assert.True(world.World.Commands.TryGet("/evil ", out var def));
            Assert.NotNull(def!.LegacyFactory);
            Assert.Null(def.LegacyType);

            Assert.True(world.RunCommand(player, "/evil "));
            Assert.Contains(player.Sent, s => s.Contains("evil ran"));
        }

        [Fact]
        public void RegisterEventNonSlashKeyStillWorks()
        {
            var (world, player, _) = WorldAndPlayer();
            world.World.EventHandler.RegisterEvent("GID", (p, d) => new StubEvent { Player = p, Data = d });

            Assert.False(world.World.Commands.TryGet("GID", out _));

            Assert.True(world.RunCommand(player, "GID"));
            Assert.Contains(player.Sent, s => s.Contains("evil ran"));
        }

        [Fact]
        public void RegisterEventOpenFactoryShadowedByRestrictedBuiltinCommand()
        {
            var (world, player, map) = WorldAndPlayer();
            var factoryCalled = false;
            world.World.EventHandler.RegisterEvent("/shutdown", (p, d) =>
            {
                factoryCalled = true;
                return new StubEvent { Player = p, Data = d };
            });
            world.World.Running = true;

            Assert.True(world.World.Commands.TryGet("/shutdown", out var def));
            Assert.Equal(AccessPrivilege.Shutdown, def!.Privilege);
            Assert.Null(def!.LegacyType);
            Assert.IsType<ShutdownCommand>(def.Instance);

            Assert.True(world.RunCommand(player, "/shutdown"));
            Assert.True(world.World.Running);
            Assert.Empty(player.Sent);
            Assert.False(factoryCalled);

            var gm = world.CommandPlayerOn(map, 2, 2, "GM");
            gm.Access = Player.AccessStatus.GameMaster;
            Assert.True(world.RunCommand(gm, "/shutdown"));
            Assert.False(world.World.Running);
            Assert.False(factoryCalled);

            Assert.True(world.World.Commands.TryGet("/shutdown", out def));
            Assert.Equal(AccessPrivilege.Shutdown, def!.Privilege);
            Assert.IsType<ShutdownCommand>(def.Instance);
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

    internal sealed class ClosedCapture
    {
        public int Value;
    }

    internal static class ClosedDelegateTargets
    {
        public static void ClosedStatic(ClosedCapture capture, CommandContext ctx, int n)
        {
            capture.Value = n;
            ctx.Send($"closed {n}");
        }
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

    [Command("/tmixed", Help = "Test mixed command.")]
    internal sealed class MixedCommand : BaseCommand
    {
        public void Execute(CommandContext ctx, string? token = null)
            => ctx.Send(token is null ? "default" : $"exec {token}");

        [Subcommand("make")]
        public void Make(CommandContext ctx) => ctx.Send("mixed make ran");

        [Subcommand("secret", AccessPrivilege.Ban)]
        public void Secret(CommandContext ctx) => ctx.Send("mixed secret ran");
    }
}
