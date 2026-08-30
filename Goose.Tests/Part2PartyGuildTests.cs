using Goose;
using Goose.Commands;
using Goose.Testing;
using Xunit;

namespace Goose.Tests
{
    public class Part2PartyGuildTests
    {
        private static (TestWorldFixture fixture, TestWorldFixture.CapturingPlayer player, Map map) WorldAndPlayer()
        {
            var fixture = new TestWorldFixture();
            var map = fixture.AddBaseMap(1, "Test");
            var player = fixture.CommandPlayerOn(map, 1, 2, "Tester");
            return (fixture, player, map);
        }

        private static Group MakeGroup(params Player[] players)
        {
            var group = new Group();
            foreach (var p in players)
            {
                group.Players.Add(p);
                p.Group = group;
            }
            return group;
        }

        private static Guild MakeGuild(params (Player player, Guild.GuildRanks rank)[] members)
        {
            var guild = new Guild { ID = 5, Name = "TestGuild", MOTD = "old" };
            foreach (var (player, rank) in members)
            {
                guild.AddMember(player.PlayerID, rank, true, true);
                guild.OnlineMembers.Add(player);
                player.Guild = guild;
            }
            return guild;
        }

        [Fact]
        public void Invite_alias_keys_bind_exact_name()
        {
            var (fixture, player, map) = WorldAndPlayer();
            using (fixture)
            {
                var bob = fixture.CommandPlayerOn(map, 3, 2, "Bob");
                bob.GroupInvitesEnabled = true;
                fixture.RegisterOnlinePlayer(bob);

                Assert.True(fixture.RunCommand(player, "/invite Bob"));

                Assert.DoesNotContain(player.Sent, s => s.Contains("Couldn't find player."));
                Assert.NotNull(bob.Group);
                Assert.Same(player.Group, bob.Group);
                Assert.Contains(bob.Sent, s => s.Contains("You have joined a group."));

                var ann = fixture.CommandPlayerOn(map, 4, 2, "Ann");
                ann.GroupInvitesEnabled = true;
                fixture.RegisterOnlinePlayer(ann);

                Assert.True(fixture.RunCommand(player, "/groupadd Ann"));

                Assert.Same(player.Group, ann.Group);
                Assert.Contains(ann.Sent, s => s.Contains("You have joined a group."));
            }
        }

        [Fact]
        public void GroupRemove_trailing_space_is_silent_noop()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                MakeGroup(player);

                Assert.True(fixture.RunCommand(player, "/groupremove "));

                Assert.NotNull(player.Group);
                Assert.Empty(player.Sent);
            }
        }

        [Fact]
        public void GroupRemove_named_player_removes_them()
        {
            var (fixture, player, map) = WorldAndPlayer();
            using (fixture)
            {
                var bob = fixture.CommandPlayerOn(map, 3, 2, "Bob");
                fixture.RegisterOnlinePlayer(bob);
                MakeGroup(player, bob);

                Assert.True(fixture.RunCommand(player, "/groupremove Bob"));

                Assert.Null(bob.Group);
            }
        }

        [Fact]
        public void Disband_leaves_group()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                MakeGroup(player);

                Assert.True(fixture.RunCommand(player, "/disband"));

                Assert.Null(player.Group);
                Assert.Contains(player.Sent, s => s.Contains("You have left the group."));
            }
        }

        [Fact]
        public void Help_shows_first_alias_key_only()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                var pages = HelpFormatter.BuildPages(player, fixture.World.Commands, "groupadd");

                Assert.NotNull(pages);
                var lines = pages!.SelectMany(p => p).ToList();
                Assert.Contains(lines, l => l.Contains("/invite"));
                Assert.DoesNotContain(lines, l => l.Contains("/groupadd"));
            }
        }

        [Fact]
        public void GuildMotd_sets_and_clears()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                player.PlayerID = 1;
                var guild = MakeGuild((player, Guild.GuildRanks.Leader));

                Assert.True(fixture.RunCommand(player, "/guildmotd hello world"));
                Assert.Equal("hello world", guild.MOTD);
                Assert.Contains(player.Sent, s => s.Contains("[guild-notice] MOTD: hello world"));

                Assert.True(fixture.RunCommand(player, "/guildmotd"));
                Assert.Equal("", guild.MOTD);
            }
        }

        [Fact]
        public void GuildCreate_joins_multiword_name()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                Assert.True(fixture.RunCommand(player, "/guildcreate Test Guild"));

                Assert.NotNull(player.Guild);
                Assert.Equal("Test Guild", player.Guild!.Name);
                Assert.Equal(Guild.GuildRanks.Leader, player.Guild.GetRank(player));
            }
        }

        [Fact]
        public void GroupChat_sends_to_group()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                MakeGroup(player);

                Assert.True(fixture.RunCommand(player, "/group hi"));

                Assert.Contains(player.Sent, s => s.Contains("[group] Tester: hi"));
            }
        }
    }
}
