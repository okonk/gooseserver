using System;
using System.Net;
using System.Net.Sockets;
using Goose;
using Xunit;

namespace Goose.Tests
{
    // H7 (docs/code-review-2026-08-15.md): bind failures (bad IP, port in use)
    // restarted the server forever every 10s instead of fast-failing.
    [Collection(Goose.Tests.Collections.GameWorldSettingsCollection.Name)]
    public class GameServerStartupTests
    {
        [Fact]
        public void CreateListenSocket_InvalidIP_ThrowsFatalStartupException()
        {
            string oldIp = GameWorld.Settings.GameServerIP;
            int oldPort = GameWorld.Settings.GameServerPort;
            try
            {
                GameWorld.Settings.GameServerIP = "not-an-ip";
                GameWorld.Settings.GameServerPort = 17000;

                var server = new GameServer(GameWorld.Settings);

                var ex = Assert.Throws<FatalStartupException>(() => server.CreateListenSocket());
                Assert.Contains("not-an-ip", ex.Message);
                Assert.Contains("17000", ex.Message);
            }
            finally
            {
                GameWorld.Settings.GameServerIP = oldIp;
                GameWorld.Settings.GameServerPort = oldPort;
            }
        }

        [Fact]
        public void CreateListenSocket_PortInUse_ThrowsFatalStartupException()
        {
            using var blocker = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            blocker.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            blocker.Listen(10);
            int port = ((IPEndPoint)blocker.LocalEndPoint).Port;

            string oldIp = GameWorld.Settings.GameServerIP;
            int oldPort = GameWorld.Settings.GameServerPort;
            try
            {
                GameWorld.Settings.GameServerIP = "127.0.0.1";
                GameWorld.Settings.GameServerPort = port;

                var server = new GameServer(GameWorld.Settings);

                Assert.Throws<FatalStartupException>(() => server.CreateListenSocket());
            }
            finally
            {
                GameWorld.Settings.GameServerIP = oldIp;
                GameWorld.Settings.GameServerPort = oldPort;
            }
        }

        [Fact]
        public void CreateListenSocket_ValidIPAndPort_ReturnsBoundSocket()
        {
            string oldIp = GameWorld.Settings.GameServerIP;
            int oldPort = GameWorld.Settings.GameServerPort;
            try
            {
                GameWorld.Settings.GameServerIP = "127.0.0.1";
                GameWorld.Settings.GameServerPort = 0;

                var server = new GameServer(GameWorld.Settings);

                using var socket = server.CreateListenSocket();

                Assert.True(socket.IsBound);
                Assert.NotEqual(0, ((IPEndPoint)socket.LocalEndPoint).Port);
            }
            finally
            {
                GameWorld.Settings.GameServerIP = oldIp;
                GameWorld.Settings.GameServerPort = oldPort;
            }
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
            Assert.Throws<ArgumentNullException>(() => new GameServer(null));
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
