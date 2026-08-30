using Goose;
using Goose.Commands;
using Goose.Testing;
using Xunit;

namespace Goose.IntegrationTests
{
    public class Part2MigrationTests
    {
        private static (TestWorldFixture fixture, TestWorldFixture.CapturingPlayer player, Map map) WorldAndPlayer()
        {
            var fixture = new TestWorldFixture();
            var map = fixture.AddBaseMap(1, "Test");
            var player = fixture.CommandPlayerOn(map, 1, 2, "Tester");
            player.Access = Player.AccessStatus.Normal;
            return (fixture, player, map);
        }

        private static Guild MakeGuild(Player member, Guild.GuildRanks rank)
        {
            var guild = new Guild { ID = 5, Name = "TestGuild", MOTD = "old" };
            guild.AddMember(member.PlayerID, rank, true, true);
            guild.OnlineMembers.Add(member);
            member.Guild = guild;
            return guild;
        }

        private static List<string> HelpLines(Player player, CommandRegistry registry)
            => HelpFormatter.BuildPages(player, registry, null)!.SelectMany(p => p).ToList();

        private static readonly string[] MigratedKeys =
        [
            "/aether", "/auction", "/buymana", "/buyvita", "/changepassword", "/charinfo",
            "/credits", "/dropgold", "/hairdye", "/help", "/location", "/mc", "/playtime",
            "/random", "/rank", "/refresh", "/shout", "/tell", "/toggle", "/who",
            "/invite", "/group", "/disband", "/togglegroup",
            "/guildadd", "/guild", "/guildcreate", "/guildmotd", "/guildofficer", "/guildowner", "/guildremove",
            "/petdamage", "/petdelete", "/petinfo", "/petlist", "/petspawn", "/petvita",
        ];

        [Fact]
        public void Tell_online_target_reaches_both_players()
        {
            var (fixture, player, map) = WorldAndPlayer();
            using (fixture)
            {
                var bob = fixture.CommandPlayerOn(map, 3, 2, "Bob");
                fixture.RegisterOnlinePlayer(bob);

                Assert.True(fixture.RunCommand(player, "/tell Bob hello there"));

                Assert.Contains(player.Sent, s => s.Contains("[tell to] Bob: hello there"));
                Assert.Contains(bob.Sent, s => s.Contains(P.Tell(player, "hello there")));
            }
        }

        [Fact]
        public void Tell_missing_target_replies_fixed_message()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                Assert.True(fixture.RunCommand(player, "/tell Ghost hello"));

                Assert.Contains(player.Sent, s => s.Contains("Couldn't find player Ghost."));
            }
        }

        [Fact]
        public void Toggle_gminvisible_as_normal_is_swallowed()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                Assert.True(fixture.RunCommand(player, "/toggle gm-invisible"));

                Assert.Empty(player.Sent);
                Assert.Equal(0, (int)player.ToggleSettings);
            }
        }

        [Fact]
        public void Toggle_gminvisible_as_gm_toggles_state()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                player.Access = Player.AccessStatus.GameMaster;

                Assert.True(fixture.RunCommand(player, "/toggle gm-invisible"));

                // Legacy quirk: the GMInvisible flag being set reads as "visible" to the player.
                Assert.Contains(player.Sent, s => s.Contains("You are now visible."));
                Assert.NotEqual(0, (int)(player.ToggleSettings & Player.ToggleSetting.GMInvisible));
            }
        }

        [Fact]
        public void GuildMotd_multiword_message_is_preserved()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                player.PlayerID = 1;
                var guild = MakeGuild(player, Guild.GuildRanks.Officer);

                Assert.True(fixture.RunCommand(player, "/guildmotd We own this server"));

                Assert.Equal("We own this server", guild.MOTD);
                Assert.Contains(player.Sent, s => s.Contains("[guild-notice] MOTD: We own this server"));
            }
        }

        [Fact]
        public void Who_all_lists_players_in_the_player_handler()
        {
            var (fixture, player, map) = WorldAndPlayer();
            using (fixture)
            {
                var bob = fixture.CommandPlayerOn(map, 3, 2, "Bob");
                fixture.AddOnlinePlayer(player);
                fixture.AddOnlinePlayer(bob);

                Assert.True(fixture.RunCommand(player, "/who all"));

                Assert.Contains(player.Sent, s => s.StartsWith("#") && s.Contains("[Test] Tester"));
                Assert.Contains(player.Sent, s => s.StartsWith("#") && s.Contains("[Test] Bob"));
                Assert.Contains(player.Sent, s => s.Contains("[Matched 2 players]"));
            }
        }

        [Fact]
        public void Help_lists_migrated_commands_for_gm_and_hides_nothing_new_for_normal()
        {
            var (fixture, normal, map) = WorldAndPlayer();
            using (fixture)
            {
                var gm = fixture.CommandPlayerOn(map, 2, 2, name: "GM");
                gm.Access = Player.AccessStatus.GameMaster;

                Assert.True(fixture.RunCommand(gm, "/help"));
                Assert.Contains(gm.Sent, m => m.StartsWith("MKW"));
                Assert.Contains(gm.Sent, m => m.StartsWith("ENW"));
                Assert.Contains(gm.Windows, w => w is HelpWindow);

                var gmLines = HelpLines(gm, fixture.World.Commands);
                foreach (var header in new[] { "General (21)", "Party (4)", "Guild (7)", "Pets (6)" })
                    Assert.Contains(gmLines, l => l.Contains(header));
                foreach (var key in MigratedKeys)
                    Assert.Contains(gmLines, l => l.StartsWith("Usage: " + key + " "));

                var normalLines = HelpLines(normal, fixture.World.Commands);
                foreach (var header in new[] { "General (21)", "Party (4)", "Guild (7)", "Pets (6)" })
                    Assert.Contains(normalLines, l => l.Contains(header));
                foreach (var key in MigratedKeys)
                    Assert.Contains(normalLines, l => l.StartsWith("Usage: " + key + " "));
            }
        }

        [Fact]
        public void Parse_failure_replies_usage_line_end_to_end()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                Assert.True(fixture.RunCommand(player, "/dropgold abc"));

                Assert.Contains(player.Sent, s => s.Contains("Usage: /dropgold <gold>"));
            }
        }
    }
}
