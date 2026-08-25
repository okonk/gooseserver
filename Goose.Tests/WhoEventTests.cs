using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Goose;
using Goose.Events;
using Xunit;

namespace Goose.Tests
{
    public class WhoEventTests
    {
        private class CapturingPlayer : Player
        {
            public List<string> Sent { get; } = new();

            public override bool Send(string data)
            {
                Sent.Add(data);
                return true;
            }
        }

        private (GameWorld world, Map mapA, Map mapB) NewWorld()
        {
            var world = new GameWorld(new GooseSettings { MaxPlayers = 200 });
            return (world, new Map { Name = "MapA" }, new Map { Name = "MapB" });
        }

        private static CapturingPlayer NewPlayer(GameWorld world, Map map, string name,
            Player.AccessStatus access = Player.AccessStatus.Normal, string surname = null)
        {
            var player = new CapturingPlayer
            {
                Sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp),
                Name = name,
                Surname = surname,
                Level = 10,
                Access = access,
                State = Player.States.Ready,
                Map = map,
                Class = new Class { ClassName = "Fighter" },
            };
            world.PlayerHandler.AddPlayer(player, world);
            map.AddPlayer(player, world);
            return player;
        }

        // Strips the \x01 packet delimiter; keeps the payload for assertion.
        private static List<string> RunWho(CapturingPlayer requester, GameWorld world, string packet)
        {
            requester.Sent.Clear();
            var ev = new WhoEvent { Player = requester, Data = packet };
            ev.Ready(world);
            return requester.Sent.Select(s => s.Length > 0 && s[^1] == '\x01' ? s[..^1] : s).ToList();
        }

        // Expected values must be List<string>, not string[]: xunit's default comparer
        // for array-vs-list sequences falls back to culture-sensitive string comparison,
        // which would treat a trailing \x01 as ignorable.
        private static List<string> Expected(params string[] lines) => new List<string>(lines);

        [Fact]
        public void Who_ListsOnlyCurrentMap()
        {
            var (world, mapA, mapB) = NewWorld();
            var requester = NewPlayer(world, mapA, "Alfred");
            NewPlayer(world, mapA, "Bob");
            NewPlayer(world, mapB, "Carol");

            var sent = RunWho(requester, world, "/who");

            Assert.Contains("#[MapA] Alfred (Level 10 Fighter)", sent);
            Assert.Contains("#[MapA] Bob (Level 10 Fighter)", sent);
            Assert.DoesNotContain("#[MapB] Carol (Level 10 Fighter)", sent);
            Assert.Equal("#[Matched 2 players]", sent[^1]);
        }

        [Fact]
        public void WhoAll_ListsEveryMap()
        {
            var (world, mapA, mapB) = NewWorld();
            var requester = NewPlayer(world, mapA, "Alfred");
            NewPlayer(world, mapB, "Carol");

            var sent = RunWho(requester, world, "/who all");

            Assert.Contains("#[MapA] Alfred (Level 10 Fighter)", sent);
            Assert.Contains("#[MapB] Carol (Level 10 Fighter)", sent);
            Assert.Equal("#[Matched 2 players]", sent[^1]);
        }

        [Fact]
        public void WhoName_ExactMatch_FindsPlayerOnOtherMap()
        {
            var (world, mapA, mapB) = NewWorld();
            var requester = NewPlayer(world, mapA, "Alfred");
            NewPlayer(world, mapA, "Bob");
            NewPlayer(world, mapB, "Nambro");

            var sent = RunWho(requester, world, "/who nambro");

            Assert.Equal(Expected(
                "#[MapB] Nambro (Level 10 Fighter)",
                "#[Matched 1 players]"), sent);
        }

        [Fact]
        public void WhoName_PartialMatch_IsCaseInsensitive()
        {
            var (world, mapA, mapB) = NewWorld();
            var requester = NewPlayer(world, mapA, "Alfred");
            NewPlayer(world, mapB, "Nambro");

            var sent = RunWho(requester, world, "/who NAM");

            Assert.Equal(Expected(
                "#[MapB] Nambro (Level 10 Fighter)",
                "#[Matched 1 players]"), sent);
        }

        [Fact]
        public void WhoName_NoMatch_SaysZeroPlayers()
        {
            var (world, mapA, _) = NewWorld();
            var requester = NewPlayer(world, mapA, "Alfred");
            NewPlayer(world, mapA, "Bob");

            var sent = RunWho(requester, world, "/who zzz");

            Assert.Equal(Expected("#[Matched 0 players]"), sent);
        }

        [Fact]
        public void WhoName_MatchesSurname()
        {
            var (world, mapA, mapB) = NewWorld();
            var requester = NewPlayer(world, mapA, "Alfred");
            NewPlayer(world, mapB, "Nambro", surname: "The Third");

            var sent = RunWho(requester, world, "/who third");

            Assert.Equal(Expected(
                "#[MapB] Nambro The Third (Level 10 Fighter)",
                "#[Matched 1 players]"), sent);
        }

        [Fact]
        public void WhoAllName_OnlyMatchesAreListed()
        {
            var (world, mapA, mapB) = NewWorld();
            var requester = NewPlayer(world, mapA, "Alfred");
            NewPlayer(world, mapA, "Nambro");
            NewPlayer(world, mapB, "Bob");

            var sent = RunWho(requester, world, "/who all nambro");

            Assert.Equal(Expected(
                "#[MapA] Nambro (Level 10 Fighter)",
                "#[Matched 1 players]"), sent);
        }

        [Fact]
        public void WhoGuildName_OnlyGuildMembersAreSearched()
        {
            var (world, mapA, mapB) = NewWorld();
            var requester = NewPlayer(world, mapA, "Alfred");
            var guild = new Guild();
            requester.Guild = guild;
            guild.OnlineMembers.Add(requester);

            NewPlayer(world, mapA, "Nambro");
            var nambro = NewPlayer(world, mapB, "Nambro");
            nambro.Guild = guild;
            guild.OnlineMembers.Add(nambro);

            var sent = RunWho(requester, world, "/who guild nambro");

            Assert.Equal(Expected(
                "#[MapB] Nambro (Level 10 Fighter)",
                "#[Matched 1 players]"), sent);
        }

        [Fact]
        public void WhoName_WhoInvisiblePlayer_HiddenFromNormalPlayer()
        {
            var (world, mapA, mapB) = NewWorld();
            var requester = NewPlayer(world, mapA, "Alfred");
            NewPlayer(world, mapB, "Nambro", access: Player.AccessStatus.Guide);

            var sent = RunWho(requester, world, "/who nambro");

            Assert.Equal(Expected("#[Matched 0 players]"), sent);
        }

        [Fact]
        public void WhoName_WhoInvisiblePlayer_VisibleToHigherAccess()
        {
            var (world, mapA, mapB) = NewWorld();
            var requester = NewPlayer(world, mapA, "Alfred", access: Player.AccessStatus.GameMaster);
            NewPlayer(world, mapB, "Nambro", access: Player.AccessStatus.Guide);

            var sent = RunWho(requester, world, "/who nambro");

            Assert.Equal(Expected(
                "#[MapB] *** Invisible *** Nambro (Level 10 Guide)",
                "#[Matched 1 players]"), sent);
        }
    }
}
