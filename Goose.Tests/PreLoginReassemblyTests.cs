using System.Net.Sockets;
using Goose;
using Goose.Events;
using Xunit;

namespace Goose.Tests
{
    public class PreLoginReassemblyTests
    {
        private static (GameWorld world, Socket sock) NewWorldAndSocket()
        {
            var settings = new GooseSettings();
            var world = new GameWorld(settings, new GameServer(settings));
            var sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) { Blocking = false };
            return (world, sock);
        }

        [Fact]
        public void ClassicLogin_SplitAcrossTwoSegments_AssembledBeforeDispatch()
        {
            var (world, sock) = NewWorldAndSocket();
            using (sock)
            {
                const string full = "LOGINabcd,passw0rd,ALPHA33,3.5.2";

                world.Received(sock, "LOGINa");
                Assert.Equal("LOGINa", world.PreLoginPending(sock));
                Assert.Equal(0, world.EventHandler.Count);

                world.Received(sock, full.Substring(6));
                Assert.Null(world.PreLoginPending(sock));
                Assert.Equal(1, world.EventHandler.Count);

                var ev = world.EventHandler.Peek();
                Assert.IsType<LoginEvent>(ev);
                Assert.Equal(full, ((object[])ev.Data)[1]);
            }
        }

        [Fact]
        public void ClassicLogin_SplitAfterFirstComma_HeldUntilThePasswordFieldIsComplete()
        {
            var (world, sock) = NewWorldAndSocket();
            using (sock)
            {
                world.Received(sock, "LOGINabcd,");
                Assert.Equal("LOGINabcd,", world.PreLoginPending(sock));
                Assert.Equal(0, world.EventHandler.Count);

                world.Received(sock, "passw0rd,ALPHA33,3.5.2");
                Assert.Null(world.PreLoginPending(sock));
                Assert.Equal(1, world.EventHandler.Count);

                var ev = world.EventHandler.Peek();
                Assert.IsType<LoginEvent>(ev);
                Assert.Equal("LOGINabcd,passw0rd,ALPHA33,3.5.2", ((object[])ev.Data)[1]);
            }
        }

        [Fact]
        public void ClassicLogin_CompleteInOneSegment_DispatchesImmediately()
        {
            var (world, sock) = NewWorldAndSocket();
            using (sock)
            {
                world.Received(sock, "LOGINabcd,passw0rd,ALPHA33,3.5.2");
                Assert.Null(world.PreLoginPending(sock));
                Assert.Equal(1, world.EventHandler.Count);
                Assert.IsType<LoginEvent>(world.EventHandler.Peek());
            }
        }

        [Fact]
        public void IllutiaLogin_71BytesInThreeChunks_NoEventUntilThird()
        {
            var (world, sock) = NewWorldAndSocket();
            using (sock)
            {
                string payload = new string('A', 71);

                world.Received(sock, payload.Substring(0, 30));
                Assert.Equal(30, world.PreLoginPending(sock).Length);
                Assert.Equal(0, world.EventHandler.Count);

                world.Received(sock, payload.Substring(30, 25));
                Assert.Equal(55, world.PreLoginPending(sock).Length);
                Assert.Equal(0, world.EventHandler.Count);

                world.Received(sock, payload.Substring(55));
                Assert.Null(world.PreLoginPending(sock));
                Assert.Equal(1, world.EventHandler.Count);
                Assert.IsType<LoginEvent>(world.EventHandler.Peek());
                Assert.Equal(payload, ((object[])world.EventHandler.Peek().Data)[1]);
            }
        }

        [Fact]
        public void PreLoginBuffer_ExceedsCap_DropsConnection()
        {
            var settings = new GooseSettings();
            var world = new GameWorld(settings, new GameServer(settings));
            using var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            using var sock = NewLoopbackServerSocket(client);

            world.Received(sock, new string('x', 4200));
            Assert.Null(world.PreLoginPending(sock));

            var ev = world.EventHandler.Peek();
            Assert.IsType<LogoutEvent>(ev);
            Assert.Equal(sock, ev.Data);
        }

        private static Socket NewLoopbackServerSocket(Socket client)
        {
            var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.Parse("127.0.0.1"), 0));
            listener.Listen(1);
            client.Connect(new System.Net.IPEndPoint(((System.Net.IPEndPoint)listener.LocalEndPoint).Address, ((System.Net.IPEndPoint)listener.LocalEndPoint).Port));
            Socket accepted = listener.Accept();
            listener.Close();
            return accepted;
        }

        [Fact]
        public void LostConnection_RemovesPendingBuffer()
        {
            var (world, sock) = NewWorldAndSocket();
            using (sock)
            {
                world.Received(sock, "LOGINa");
                Assert.Equal("LOGINa", world.PreLoginPending(sock));

                world.LostConnection(sock);
                Assert.Null(world.PreLoginPending(sock));
            }
        }
    }
}
