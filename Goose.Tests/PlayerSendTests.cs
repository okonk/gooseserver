using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Goose;
using Xunit;

namespace Goose.Tests
{
    public class PlayerSendTests
    {
        private static Socket NewUnconnectedSocket()
        {
            return new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) { Blocking = false };
        }

        private static (Socket client, Socket accepted) NewLoopbackPair()
        {
            var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Listen(1);

            var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            client.Connect(new IPEndPoint(IPAddress.Loopback, ((IPEndPoint)listener.LocalEndPoint).Port));
            var accepted = listener.Accept();
            listener.Close();

            return (client, accepted);
        }

        [Fact]
        public void Send_WhenSocketWouldBlock_BuffersFullPayload()
        {
            var p = new Player(0);
            p.OnLogin();
            using var sock = NewUnconnectedSocket();
            p.Sock = sock;

            var ok = p.Send("ABC\x1");

            Assert.True(ok);
            Assert.Equal(new byte[] { (byte)'A', (byte)'B', (byte)'C', 1 }, p.SendBuffer);
        }

        [Fact]
        public void Send_WhenSendBufferHasPendingBytes_AppendsToBufferInsteadOfSendingDirectly()
        {
            var p = new Player(0);
            p.OnLogin();

            using (var unconnected = NewUnconnectedSocket())
            {
                p.Sock = unconnected;
                p.Send("AB\x1");
            }
            var payload1 = new byte[] { (byte)'A', (byte)'B', 1 };
            Assert.Equal(payload1, p.SendBuffer);

            var (client, accepted) = NewLoopbackPair();
            try
            {
                p.Sock = accepted;
                var payload2 = new byte[] { (byte)'C', (byte)'D', 1 };

                var ok = p.Send("CD\x1");

                Assert.True(ok);
                Assert.Equal(payload1.Concat(payload2).ToList(), p.SendBuffer);

                client.ReceiveTimeout = 200;
                Assert.Throws<SocketException>(() => client.Receive(new byte[1]));
            }
            finally
            {
                client.Close();
                accepted.Close();
            }
        }

        [Fact]
        public void Send_WhenSendBufferExceedsCap_ReturnsFalse()
        {
            var p = new Player(0);
            p.OnLogin();
            using var sock = NewUnconnectedSocket();
            p.Sock = sock;

            for (int i = 0; i < Player.MaxSendBufferSize; i++)
                p.SendBuffer.Add(0x00);

            var ok = p.Send("AB\x1");

            Assert.False(ok);
            Assert.Equal(Player.MaxSendBufferSize + 3, p.SendBuffer.Count);
        }

        [Fact]
        public void Send_WhenSocketHasRoom_DeliversPacket()
        {
            var (client, accepted) = NewLoopbackPair();
            try
            {
                accepted.Blocking = false;

                var p = new Player(0);
                p.OnLogin();
                p.Sock = accepted;

                var ok = p.Send("HI\x1");

                Assert.True(ok);

                var buf = new byte[3];
                client.ReceiveTimeout = 5000;
                int total = 0;
                while (total < buf.Length)
                {
                    int n = client.Receive(buf.AsSpan(total));
                    Assert.True(n > 0);
                    total += n;
                }
                Assert.Equal(new byte[] { (byte)'H', (byte)'I', 1 }, buf);
            }
            finally
            {
                client.Close();
                accepted.Close();
            }
        }

        [Fact]
        public void GameWorldSend_WhenSendBufferExceedsCap_DropsConnection()
        {
            var world = new GameWorld(new GameServer());
            var (client, accepted) = NewLoopbackPair();
            try
            {
                accepted.Blocking = false;

                var p = new Player(0);
                p.OnLogin();
                p.Sock = accepted;

                for (int i = 0; i < Player.MaxSendBufferSize + 1; i++)
                    p.SendBuffer.Add(0x00);

                int eventsBefore = world.EventHandler.Count;

                world.Send(p, "x");

                Assert.True(world.EventHandler.Count > eventsBefore);
                Assert.Throws<ObjectDisposedException>(() => accepted.Send(new byte[] { 1 }));
            }
            finally
            {
                client.Close();
                accepted.Dispose();
            }
        }
    }
}
