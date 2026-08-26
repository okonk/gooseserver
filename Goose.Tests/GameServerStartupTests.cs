using System;
using System.Net;
using System.Net.Sockets;
using Goose;
using Xunit;

namespace Goose.Tests
{
    // H7 (docs/code-review-2026-08-15.md): bind failures (bad IP, port in use)
    // restarted the server forever every 10s instead of fast-failing.
    public class GameServerStartupTests
    {
        [Fact]
        public void CreateListenSocket_InvalidIP_ThrowsFatalStartupException()
        {
            var settings = new GooseSettings { GameServerIP = "not-an-ip", GameServerPort = 17000 };
            var server = new GameServer(settings);

            var ex = Assert.Throws<FatalStartupException>(() => server.CreateListenSocket());
            Assert.Contains("not-an-ip", ex.Message);
            Assert.Contains("17000", ex.Message);
        }

        [Fact]
        public void CreateListenSocket_PortInUse_ThrowsFatalStartupException()
        {
            using var blocker = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            blocker.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            blocker.Listen(10);
            int port = ((IPEndPoint)blocker.LocalEndPoint!).Port;

            var settings = new GooseSettings { GameServerIP = "127.0.0.1", GameServerPort = port };
            var server = new GameServer(settings);

            Assert.Throws<FatalStartupException>(() => server.CreateListenSocket());
        }

        [Fact]
        public void CreateListenSocket_ValidIPAndPort_ReturnsBoundSocket()
        {
            var settings = new GooseSettings { GameServerIP = "127.0.0.1", GameServerPort = 0 };
            var server = new GameServer(settings);

            using var socket = server.CreateListenSocket();

            Assert.True(socket.IsBound);
            Assert.NotEqual(0, ((IPEndPoint)socket.LocalEndPoint!).Port);
        }

        [Fact]
        public void Constructor_RetainsTheExactSuppliedSettingsObject()
        {
            var settings = new GooseSettings();
            var server = new GameServer(settings);

            Assert.Same(settings, server.Settings);
        }

        [Fact]
        public void Constructor_NullSettings_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new GameServer(null!));
        }

        [Fact]
        public void Settings_ChangedThroughSuppliedObject_IsVisibleOnServer()
        {
            var settings = new GooseSettings();
            var server = new GameServer(settings);

            settings.GameServerPort = 12345;

            Assert.Equal(12345, server.Settings.GameServerPort);
        }
    }
}
