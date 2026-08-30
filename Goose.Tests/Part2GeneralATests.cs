using System.Net;
using System.Net.Sockets;
using Goose;
using Goose.Testing;
using Xunit;

namespace Goose.Tests
{
    public class Part2GeneralATests
    {
        private static (TestWorldFixture fixture, TestWorldFixture.CapturingPlayer player, Map map) WorldAndPlayer()
        {
            var fixture = new TestWorldFixture();
            var map = fixture.AddBaseMap(1, "Test");
            var player = fixture.CommandPlayerOn(map, 1, 2, "Tester");
            return (fixture, player, map);
        }

        private static void AddOnline(TestWorldFixture fixture, Player player)
        {
            player.Sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            fixture.World.PlayerHandler.AddPlayer(player, fixture.World);
        }

        [Fact]
        public void Tell_online_target_sends_to_both_players()
        {
            var (fixture, player, map) = WorldAndPlayer();
            using (fixture)
            {
                var bob = fixture.CommandPlayerOn(map, 3, 2, "Bob");
                AddOnline(fixture, bob);

                fixture.RunCommand(player, "/tell Bob hello there");

                Assert.Contains(player.Sent, s => s.Contains("[tell to] Bob: hello there"));
                Assert.Contains(bob.Sent, s => s.Contains(P.Tell(player, "hello there")));
            }
        }

        [Fact]
        public void Tell_unknown_target_sends_fixed_message()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                fixture.RunCommand(player, "/tell Ghost hi");

                Assert.Contains(player.Sent, s => s.Contains("Couldn't find player Ghost."));
            }
        }

        [Fact]
        public void Tell_empty_message_is_silent()
        {
            var (fixture, player, map) = WorldAndPlayer();
            using (fixture)
            {
                var bob = fixture.CommandPlayerOn(map, 3, 2, "Bob");
                AddOnline(fixture, bob);

                fixture.RunCommand(player, "/tell Bob");

                Assert.Empty(player.Sent);
                Assert.Empty(bob.Sent);
            }
        }

        [Fact]
        public void Tell_target_with_tells_disabled_is_told()
        {
            var (fixture, player, map) = WorldAndPlayer();
            using (fixture)
            {
                var bob = fixture.CommandPlayerOn(map, 3, 2, "Bob");
                bob.ToggleSettings |= Player.ToggleSetting.Tell;
                AddOnline(fixture, bob);

                fixture.RunCommand(player, "/tell Bob hi");

                Assert.Contains(player.Sent, s => s.Contains("Bob has tells disabled."));
                Assert.Empty(bob.Sent);
            }
        }

        [Fact]
        public void Who_all_lists_all_registered_players()
        {
            var (fixture, player, map) = WorldAndPlayer();
            using (fixture)
            {
                var bob = fixture.CommandPlayerOn(map, 3, 2, "Bob");
                map.AddPlayer(player, fixture.World);
                map.AddPlayer(bob, fixture.World);
                AddOnline(fixture, player);
                AddOnline(fixture, bob);

                fixture.RunCommand(player, "/who all");

                Assert.Contains(player.Sent, s => s.Contains("[Test] Tester (Level 0 Default)"));
                Assert.Contains(player.Sent, s => s.Contains("[Test] Bob (Level 0 Default)"));
                Assert.Contains(player.Sent, s => s.Contains("[Matched 2 players]"));
            }
        }

        [Fact]
        public void Who_all_with_query_filters_by_name()
        {
            var (fixture, player, map) = WorldAndPlayer();
            using (fixture)
            {
                var bob = fixture.CommandPlayerOn(map, 3, 2, "Bob");
                map.AddPlayer(player, fixture.World);
                map.AddPlayer(bob, fixture.World);
                AddOnline(fixture, player);
                AddOnline(fixture, bob);

                fixture.RunCommand(player, "/who all BO");

                Assert.Contains(player.Sent, s => s.Contains("[Test] Bob (Level 0 Default)"));
                Assert.DoesNotContain(player.Sent, s => s.Contains("Tester (Level 0 Default)"));
                Assert.Contains(player.Sent, s => s.Contains("[Matched 1 players]"));
            }
        }

        [Fact]
        public void Who_no_args_lists_map_players()
        {
            var (fixture, player, map) = WorldAndPlayer();
            using (fixture)
            {
                var bob = fixture.CommandPlayerOn(map, 3, 2, "Bob");
                var otherMap = fixture.AddBaseMap(2, "Elsewhere");
                var carol = fixture.CommandPlayerOn(otherMap, 1, 1, "Carol");
                map.AddPlayer(player, fixture.World);
                map.AddPlayer(bob, fixture.World);
                otherMap.AddPlayer(carol, fixture.World);

                fixture.RunCommand(player, "/who");

                Assert.Contains(player.Sent, s => s.Contains("[Test] Tester (Level 0 Default)"));
                Assert.Contains(player.Sent, s => s.Contains("[Test] Bob (Level 0 Default)"));
                Assert.DoesNotContain(player.Sent, s => s.Contains("Carol"));
                Assert.Contains(player.Sent, s => s.Contains("[Matched 2 players]"));
            }
        }

        [Fact]
        public void DropGold_decreases_gold_and_places_item()
        {
            var (fixture, player, map) = WorldAndPlayer();
            using (fixture)
            {
                player.Level = 10;
                player.Gold = 100;
                var gold = new Item { ItemID = fixture.World.Settings.ItemIDStartpoint + fixture.World.Settings.GoldItemID };
                fixture.World.ItemHandler.AddItem(gold, fixture.World);

                fixture.RunCommand(player, "/dropgold 50");

                Assert.Equal(50, player.Gold);
                var tile = map.GetTile(player.MapX, player.MapY) as ItemTile;
                Assert.NotNull(tile);
                Assert.Equal(50, tile!.ItemSlot.Stack);
            }
        }

        [Fact]
        public void DropGold_below_level_10_is_refused()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                player.Level = 5;
                player.Gold = 100;

                fixture.RunCommand(player, "/dropgold 50");

                Assert.Contains(player.Sent, s => s.Contains("You need to be level 10 or higher to drop gold."));
                Assert.Equal(100, player.Gold);
            }
        }

        [Fact]
        public void DropGold_bad_number_sends_usage()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                fixture.RunCommand(player, "/dropgold abc");

                Assert.Contains(player.Sent, s => s.Contains("Usage: /dropgold <gold>"));
            }
        }

        [Fact]
        public void ChangePassword_accepts_short_password()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                fixture.RunCommand(player, "/changepassword abc");

                Assert.Contains(player.Sent, s => s.Contains("Your password has been changed."));
                Assert.DoesNotContain(player.Sent, s => s.Contains("Usage:"));
            }
        }

        [Fact]
        public void ChangePassword_rejects_too_short()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                fixture.RunCommand(player, "/changepassword ab");

                Assert.Contains(player.Sent, s => s.Contains("Your password needs to be more than 3 characters long."));
            }
        }

        [Fact]
        public void ChangePassword_multiword_password_reaches_handler_joined()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                fixture.RunCommand(player, "/changepassword my secret pw");

                Assert.Contains(player.Sent, s => s.Contains("Your password has been changed."));
                Assert.True(PasswordHasher.Verify("my secret pw", player.PasswordHash, player.PasswordSalt));
            }
        }

        [Fact]
        public void Refresh_sends_position_and_RPU_packet_still_dispatches()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                fixture.RunCommand(player, "/refresh");
                Assert.Contains(player.Sent, s => s.Contains(P.SetYourPosition(player)));

                player.Sent.Clear();
                Assert.True(fixture.RunCommand(player, "RPU"));
                Assert.Contains(player.Sent, s => s.Contains(P.SetYourPosition(player)));
            }
        }

        [Fact]
        public void Shout_broadcasts_to_map_players()
        {
            var (fixture, player, map) = WorldAndPlayer();
            using (fixture)
            {
                map.CanShout = true;
                var bob = fixture.CommandPlayerOn(map, 3, 2, "Bob");
                map.AddPlayer(player, fixture.World);
                map.AddPlayer(bob, fixture.World);

                fixture.RunCommand(player, "/shout hello there");

                Assert.Contains(player.Sent, s => s.Contains("Tester shouts: hello there"));
                Assert.Contains(bob.Sent, s => s.Contains("Tester shouts: hello there"));
            }
        }

        [Fact]
        public void Shout_muted_map_is_refused()
        {
            var (fixture, player, map) = WorldAndPlayer();
            using (fixture)
            {
                map.CanShout = true;
                map.Muted = true;

                fixture.RunCommand(player, "/shout hello");

                Assert.Contains(player.Sent, s => s.Contains("Shouting is disabled in this map."));
            }
        }

        [Fact]
        public void Shout_empty_message_is_silent()
        {
            var (fixture, player, map) = WorldAndPlayer();
            using (fixture)
            {
                map.CanShout = true;

                fixture.RunCommand(player, "/shout ");

                Assert.Empty(player.Sent);
            }
        }

        [Fact]
        public void Auction_muted_map_is_refused()
        {
            var (fixture, player, map) = WorldAndPlayer();
            using (fixture)
            {
                map.CanAuction = true;
                map.Muted = true;

                fixture.RunCommand(player, "/auction hello");

                Assert.Contains(player.Sent, s => s.Contains("Auction is disabled in this map."));
            }
        }

        [Fact]
        public void Random_muted_map_is_refused()
        {
            var (fixture, player, map) = WorldAndPlayer();
            using (fixture)
            {
                map.CanChat = true;
                map.Muted = true;

                fixture.RunCommand(player, "/random");

                Assert.Contains(player.Sent, s => s.Contains("Chat is disabled in this map."));
            }
        }

        [Fact]
        public void Random_rolls_and_broadcasts()
        {
            var (fixture, player, map) = WorldAndPlayer();
            using (fixture)
            {
                map.CanChat = true;
                map.AddPlayer(player, fixture.World);

                fixture.RunCommand(player, "/random");

                Assert.Contains(player.Sent, s => s.Contains("Tester rolls ") && s.Contains(" out of 1000."));
            }
        }

        [Fact]
        public void Random_with_max_rolls_to_that_max()
        {
            var (fixture, player, map) = WorldAndPlayer();
            using (fixture)
            {
                map.CanChat = true;
                map.AddPlayer(player, fixture.World);

                fixture.RunCommand(player, "/random 50");

                Assert.Contains(player.Sent, s => s.Contains("Tester rolls ") && s.Contains(" out of 50."));
            }
        }

        [Fact]
        public void Random_with_extra_tokens_falls_back_to_default()
        {
            var (fixture, player, map) = WorldAndPlayer();
            using (fixture)
            {
                map.CanChat = true;
                map.AddPlayer(player, fixture.World);

                fixture.RunCommand(player, "/random 50 60");

                Assert.Contains(player.Sent, s => s.Contains(" out of 1000."));
                Assert.DoesNotContain(player.Sent, s => s.Contains("Usage:"));
            }
        }

        [Fact]
        public void Random_with_non_number_falls_back_to_default_silently()
        {
            var (fixture, player, map) = WorldAndPlayer();
            using (fixture)
            {
                map.CanChat = true;
                map.AddPlayer(player, fixture.World);

                fixture.RunCommand(player, "/random abc");

                Assert.Contains(player.Sent, s => s.Contains(" out of 1000."));
                Assert.DoesNotContain(player.Sent, s => s.Contains("Usage:"));
            }
        }

        [Fact]
        public void Location_reports_map_and_coordinates()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                fixture.RunCommand(player, "/location");

                Assert.Contains(player.Sent, s => s.Contains("You are in Test at 1,2."));
            }
        }

        [Fact]
        public void Credits_reports_balance()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                player.Credits = 5;

                fixture.RunCommand(player, "/credits");

                Assert.Contains(player.Sent, s => s.Contains("You have 5 donation credits."));
            }
        }

        [Fact]
        public void Playtime_reports_play_and_afk_time()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                player.TotalAfkTime = 3600;
                player.TotalPlayTime = 3661;

                fixture.RunCommand(player, "/playtime");

                Assert.Contains(player.Sent, s => s.Contains("You have spent 1 hours AFK. And 1 hours, 1 minutes playing."));
            }
        }

        [Fact]
        public void CharInfo_opens_window_once_and_refreshes()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                fixture.RunCommand(player, "/charinfo");
                Assert.Contains(player.Windows, w => w.Type == Window.WindowTypes.CharInfo);

                fixture.RunCommand(player, "/charinfo");
                Assert.Equal(1, player.Windows.Count(w => w.Type == Window.WindowTypes.CharInfo));
            }
        }
    }
}
