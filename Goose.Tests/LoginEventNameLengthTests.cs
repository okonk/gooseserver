using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using Goose;
using Goose.Events;
using Goose.Tests.Collections;
using Xunit;

namespace Goose.Tests
{
    [Collection(GameWorldSettingsCollection.Name)]
    public class LoginEventNameLengthTests
    {
        private static (GameWorld world, Socket client, Socket accepted) NewWorldAndLoopbackPair()
        {
            var world = new GameWorld(new GameServer(GameWorld.Settings));
            // Only assigned during the Run/load sequence, not the constructor
            world.LoginThrottle = new LoginThrottle();
            world.CharactersCreatedPerIP = new Dictionary<string, int>();
            RegisterStartingClass(world);

            var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Listen(1);

            var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            client.Connect(new IPEndPoint(IPAddress.Loopback, ((IPEndPoint)listener.LocalEndPoint).Port));
            var accepted = listener.Accept();
            listener.Close();
            accepted.Blocking = false;

            return (world, client, accepted);
        }

        // ClassHandler has no public registration path (classes come from the database via
        // LoadClasses), and LoadFromAutoCreate dereferences Class.GetLevel unconditionally.
        private static void RegisterStartingClass(GameWorld world)
        {
            int id = GameWorld.Settings.StartingClassID;
            var cls = new Class { ClassID = id, ClassName = "Default", ACMultiplier = 1m };
            cls.AddLevel(new ClassLevel { Level = GameWorld.Settings.StartingLevel, BaseStats = new AttributeSet() });

            var classes = (Dictionary<int, Class>)typeof(ClassHandler)
                .GetField("classes", BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(world.ClassHandler)!;
            classes[id] = cls;
        }

        private static void EnqueueLogin(GameWorld world, Socket accepted, string packet)
        {
            var ev = new LoginEvent();
            ev.Data = new object[] { accepted, packet };
            world.EventHandler.AddEvent(ev);
        }

        [Fact]
        public void AutoCreate_17LetterName_IsRejectedAndNoPlayerIsCreated()
        {
            var (world, client, accepted) = NewWorldAndLoopbackPair();
            using (client)
            using (accepted)
            {
                string name = new string('a', 17);
                bool previous = GameWorld.Settings.AutoCharacterCreation;
                GameWorld.Settings.AutoCharacterCreation = true;
                try
                {
                    EnqueueLogin(world, accepted, "LOGIN" + name + ",passw0rd,ALPHA33,3.5.2");
                    world.EventHandler.Update(world);

                    Assert.Null(world.PlayerHandler.GetPlayerFromData(name));
                    Assert.Null(world.PlayerHandler.GetPlayer(accepted));
                }
                finally
                {
                    GameWorld.Settings.AutoCharacterCreation = previous;
                }

                byte[] buf = new byte[512];
                client.ReceiveTimeout = 5000;
                int total = 0;
                int n;
                do
                {
                    n = client.Receive(buf.AsSpan(total));
                    total += n;
                }
                while (n > 0 && total < buf.Length);

                Assert.Equal(0, n);
                Assert.StartsWith("LNO", Encoding.ASCII.GetString(buf, 0, total));
            }
        }

        [Fact]
        public void AutoCreate_16LetterName_IsAcceptedAndPlayerIsCreated()
        {
            var (world, client, accepted) = NewWorldAndLoopbackPair();
            using (client)
            using (accepted)
            {
                string name = new string('a', 16);
                bool previous = GameWorld.Settings.AutoCharacterCreation;
                GameWorld.Settings.AutoCharacterCreation = true;
                try
                {
                    EnqueueLogin(world, accepted, "LOGIN" + name + ",passw0rd,ALPHA33,3.5.2");
                    world.EventHandler.Update(world);

                    var player = world.PlayerHandler.GetPlayerFromData(name);
                    Assert.NotNull(player);
                    Assert.Equal(name, player.Name);
                    Assert.Same(accepted, player.Sock);
                }
                finally
                {
                    GameWorld.Settings.AutoCharacterCreation = previous;
                }

                byte[] buf = new byte[512];
                client.ReceiveTimeout = 1000;
                int total = 0;
                try
                {
                    int n;
                    do
                    {
                        n = client.Receive(buf.AsSpan(total));
                        if (n <= 0) break;
                        total += n;
                    }
                    while (total < buf.Length);
                }
                catch (SocketException)
                {
                }

                Assert.Contains("LOK", Encoding.ASCII.GetString(buf, 0, total));
            }
        }
    }
}
