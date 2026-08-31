using Goose;
using Goose.Testing;
using Xunit;

namespace Goose.Tests
{
    public class Part3GmATests
    {
        private static (TestWorldFixture fixture, TestWorldFixture.CapturingPlayer gm, Map map) WorldAndGm()
        {
            var fixture = new TestWorldFixture();
            var map = fixture.AddBaseMap(1, "Test");
            var gm = fixture.CommandPlayerOn(map, 1, 2, "Tester");
            gm.Access = Player.AccessStatus.GameMaster;
            return (fixture, gm, map);
        }

        private static TestWorldFixture.CapturingPlayer MakeBob(TestWorldFixture fixture, Map map,
            Player.States state = Player.States.NotLoggedIn)
        {
            var bob = new TestWorldFixture.CapturingPlayer
            {
                Name = "Bob",
                Map = map, MapID = map.ID, MapX = 3, MapY = 4,
                State = state,
                BaseStats = new AttributeSet(),
                MaxStats = new AttributeSet(),
                Class = fixture.World.ClassHandler.GetClass(0)!,
            };
            bob.Inventory = new Inventory(bob, fixture.Settings);
            bob.Bank = new PlayerBank();
            return bob;
        }

        [Fact]
        public void Ban_bans_registered_player_for_given_days()
        {
            var (fixture, gm, map) = WorldAndGm();
            using (fixture)
            {
                var bob = MakeBob(fixture, map, Player.States.Ready);
                bob.Access = Player.AccessStatus.Normal;
                fixture.RegisterOnlinePlayer(bob);
                fixture.RegisterDatabasePlayer(bob);

                Assert.True(fixture.RunCommand(gm, "/ban Bob 30"));

                Assert.Contains(gm.Sent, s => s.Contains("Banned Bob for 30 days."));
                Assert.Equal(Player.AccessStatus.Banned, bob.Access);
                Assert.NotNull(bob.UnbanDate);
                Assert.InRange(bob.UnbanDate!.Value, DateTime.Now.AddDays(29), DateTime.Now.AddDays(31));
            }
        }

        [Fact]
        public void Ban_without_days_defaults_to_1000()
        {
            var (fixture, gm, map) = WorldAndGm();
            using (fixture)
            {
                var bob = MakeBob(fixture, map, Player.States.Ready);
                fixture.RegisterOnlinePlayer(bob);
                fixture.RegisterDatabasePlayer(bob);

                Assert.True(fixture.RunCommand(gm, "/ban Bob"));

                Assert.Contains(gm.Sent, s => s.Contains("Banned Bob for 1000 days."));
                Assert.Equal(Player.AccessStatus.Banned, bob.Access);
            }
        }

        [Fact]
        public void Ban_normal_player_is_swallowed_without_reply()
        {
            var (fixture, gm, map) = WorldAndGm();
            using (fixture)
            {
                gm.Access = Player.AccessStatus.Normal;
                var bob = MakeBob(fixture, map, Player.States.Ready);
                bob.Access = Player.AccessStatus.Normal;
                fixture.RegisterOnlinePlayer(bob);
                fixture.RegisterDatabasePlayer(bob);

                Assert.True(fixture.RunCommand(gm, "/ban Bob 30"));

                Assert.Empty(gm.Sent);
                Assert.Equal(Player.AccessStatus.Normal, bob.Access);
                Assert.Null(bob.UnbanDate);
            }
        }

        [Fact]
        public void Ban_unknown_player_reports_not_found()
        {
            var (fixture, gm, _) = WorldAndGm();
            using (fixture)
            {
                Assert.True(fixture.RunCommand(gm, "/ban Ghost 30"));

                Assert.Contains(gm.Sent, s => s.Contains("Couldn't find player."));
            }
        }

        [Fact]
        public void Summon_unknown_target_reports_name()
        {
            var (fixture, gm, _) = WorldAndGm();
            using (fixture)
            {
                Assert.True(fixture.RunCommand(gm, "/summon Ghost"));

                Assert.Contains(gm.Sent, s => s.Contains("Couldn't find player Ghost."));
            }
        }

        [Fact]
        public void Summon_loading_target_is_refused_and_stays_put()
        {
            var (fixture, gm, map) = WorldAndGm();
            using (fixture)
            {
                var bob = MakeBob(fixture, map, Player.States.LoadingMap);
                fixture.RegisterOnlinePlayer(bob);

                Assert.True(fixture.RunCommand(gm, "/summon Bob"));

                Assert.Contains(gm.Sent, s => s.Contains("Player is still loading a map."));
                Assert.Equal(3, bob.MapX);
                Assert.Equal(4, bob.MapY);
            }
        }

        [Fact]
        public void Summon_ready_target_warps_to_caller()
        {
            var (fixture, gm, map) = WorldAndGm();
            using (fixture)
            {
                var bob = MakeBob(fixture, map, Player.States.Ready);
                fixture.RegisterOnlinePlayer(bob);

                Assert.True(fixture.RunCommand(gm, "/summon Bob"));

                Assert.Equal(gm.MapX, bob.MapX);
                Assert.Equal(gm.MapY, bob.MapY);
                Assert.Same(map, bob.Map);
            }
        }

        [Fact]
        public void Approach_moves_caller_to_target()
        {
            var (fixture, gm, map) = WorldAndGm();
            using (fixture)
            {
                var bob = MakeBob(fixture, map, Player.States.Ready);
                fixture.RegisterOnlinePlayer(bob);

                Assert.True(fixture.RunCommand(gm, "/approach Bob"));

                Assert.Equal(3, gm.MapX);
                Assert.Equal(4, gm.MapY);
                Assert.Same(map, gm.Map);
            }
        }

        [Fact]
        public void Kick_online_player_is_silent()
        {
            var (fixture, gm, map) = WorldAndGm();
            using (fixture)
            {
                var bob = MakeBob(fixture, map, Player.States.Ready);
                fixture.AddOnlinePlayer(bob);

                Assert.True(fixture.RunCommand(gm, "/kick Bob"));

                Assert.Empty(gm.Sent);
            }
        }

        [Fact]
        public void Kick_unknown_target_reports_name()
        {
            var (fixture, gm, _) = WorldAndGm();
            using (fixture)
            {
                Assert.True(fixture.RunCommand(gm, "/kick Ghost"));

                Assert.Contains(gm.Sent, s => s.Contains("Couldn't find player Ghost."));
            }
        }

        [Fact]
        public void Unban_clears_ban_and_saves_offline_player()
        {
            var (fixture, gm, map) = WorldAndGm();
            using (fixture)
            {
                var bob = MakeBob(fixture, map, Player.States.Ready);
                bob.Access = Player.AccessStatus.Banned;
                bob.UnbanDate = DateTime.Now.AddDays(10);
                fixture.RegisterOnlinePlayer(bob);
                fixture.RegisterDatabasePlayer(bob);

                Assert.True(fixture.RunCommand(gm, "/unban Bob"));

                Assert.Contains(gm.Sent, s => s.Contains("Unbanned Bob."));
                Assert.Equal(Player.AccessStatus.Normal, bob.Access);
                Assert.Null(bob.UnbanDate);
            }
        }

        [Fact]
        public void Broadcast_gm_message_is_prefixed_with_access()
        {
            var (fixture, gm, map) = WorldAndGm();
            using (fixture)
            {
                var bob = MakeBob(fixture, map, Player.States.Ready);
                fixture.AddOnlinePlayer(bob);

                Assert.True(fixture.RunCommand(gm, "/broadcast hello world"));

                Assert.Contains(bob.Sent, s => s.Contains("[Game Master]: hello world"));
            }
        }

        [Fact]
        public void Broadcast_empty_input_is_silent()
        {
            var (fixture, gm, map) = WorldAndGm();
            using (fixture)
            {
                var bob = MakeBob(fixture, map, Player.States.Ready);
                fixture.AddOnlinePlayer(bob);

                Assert.True(fixture.RunCommand(gm, "/broadcast "));

                Assert.Empty(bob.Sent);
            }
        }

        [Fact]
        public void Broadcast_normal_player_is_swallowed()
        {
            var (fixture, gm, map) = WorldAndGm();
            using (fixture)
            {
                gm.Access = Player.AccessStatus.Normal;
                var bob = MakeBob(fixture, map, Player.States.Ready);
                fixture.AddOnlinePlayer(bob);

                Assert.True(fixture.RunCommand(gm, "/broadcast hello world"));

                Assert.Empty(bob.Sent);
                Assert.Empty(gm.Sent);
            }
        }

        [Fact]
        public void GiveCredits_zero_or_negative_is_silent_noop()
        {
            var (fixture, gm, map) = WorldAndGm();
            using (fixture)
            {
                gm.Credits = 100;
                var bob = MakeBob(fixture, map, Player.States.Ready);
                fixture.RegisterOnlinePlayer(bob);
                fixture.RegisterDatabasePlayer(bob);

                Assert.True(fixture.RunCommand(gm, "/givecredits Bob 0"));
                Assert.Empty(gm.Sent);
                Assert.Empty(bob.Sent);
                Assert.Equal(100, gm.Credits);
                Assert.Equal(0, bob.Credits);

                Assert.True(fixture.RunCommand(gm, "/givecredits Bob -5"));
                Assert.Empty(gm.Sent);
                Assert.Equal(100, gm.Credits);
                Assert.Equal(0, bob.Credits);
            }
        }

        [Fact]
        public void GiveCredits_transfers_credits_to_online_player()
        {
            var (fixture, gm, map) = WorldAndGm();
            using (fixture)
            {
                gm.Credits = 100;
                var bob = MakeBob(fixture, map, Player.States.Ready);
                fixture.RegisterOnlinePlayer(bob);
                fixture.RegisterDatabasePlayer(bob);

                Assert.True(fixture.RunCommand(gm, "/givecredits Bob 30"));

                Assert.Equal(70, gm.Credits);
                Assert.Equal(30, bob.Credits);
                Assert.Contains(bob.Sent, s => s.Contains("gave you 30 donation credits."));
            }
        }

        [Fact]
        public void GiveCredits_insufficient_balance_is_refused()
        {
            var (fixture, gm, map) = WorldAndGm();
            using (fixture)
            {
                gm.Credits = 5;
                var bob = MakeBob(fixture, map, Player.States.Ready);
                fixture.RegisterOnlinePlayer(bob);
                fixture.RegisterDatabasePlayer(bob);

                Assert.True(fixture.RunCommand(gm, "/givecredits Bob 10"));

                Assert.Contains(gm.Sent, s => s.Contains("You don't have enough credits."));
                Assert.Equal(5, gm.Credits);
                Assert.Equal(0, bob.Credits);
            }
        }

        [Fact]
        public void GiveGold_to_online_player()
        {
            var (fixture, gm, map) = WorldAndGm();
            using (fixture)
            {
                var bob = MakeBob(fixture, map, Player.States.Ready);
                fixture.RegisterOnlinePlayer(bob);
                fixture.RegisterDatabasePlayer(bob);

                Assert.True(fixture.RunCommand(gm, "/givegold Bob 500"));

                Assert.Equal(500, bob.Gold);
                Assert.Contains(gm.Sent, s => s.Contains("Gave 500 gold to Bob."));
                Assert.Contains(bob.Sent, s => s.Contains("gave you 500 gold."));
            }
        }

        [Fact]
        public void GiveGold_unknown_player_reports_not_found()
        {
            var (fixture, gm, _) = WorldAndGm();
            using (fixture)
            {
                Assert.True(fixture.RunCommand(gm, "/givegold Ghost 500"));

                Assert.Contains(gm.Sent, s => s.Contains("Player Ghost doesn't exist."));
            }
        }

        [Fact]
        public void GiveExperience_to_online_player()
        {
            var (fixture, gm, map) = WorldAndGm();
            using (fixture)
            {
                var bob = MakeBob(fixture, map, Player.States.Ready);
                bob.Experience = 100;
                fixture.RegisterOnlinePlayer(bob);
                fixture.RegisterDatabasePlayer(bob);

                Assert.True(fixture.RunCommand(gm, "/giveexperience Bob 500"));

                Assert.Equal(600, bob.Experience);
                Assert.Contains(gm.Sent, s => s.Contains("Added experience successfully."));
            }
        }

        [Fact]
        public void SetTitle_sets_title_with_spaces()
        {
            var (fixture, gm, map) = WorldAndGm();
            using (fixture)
            {
                var bob = MakeBob(fixture, map, Player.States.Ready);
                fixture.RegisterOnlinePlayer(bob);
                fixture.RegisterDatabasePlayer(bob);

                Assert.True(fixture.RunCommand(gm, "/settitle Bob Lord of the Vast"));

                Assert.Equal("Lord of the Vast", bob.Title);
                Assert.Contains(gm.Sent, s => s.Contains("Changed title successfully."));
            }
        }

        [Fact]
        public void SetSurname_sets_surname()
        {
            var (fixture, gm, map) = WorldAndGm();
            using (fixture)
            {
                var bob = MakeBob(fixture, map, Player.States.Ready);
                fixture.RegisterOnlinePlayer(bob);
                fixture.RegisterDatabasePlayer(bob);

                Assert.True(fixture.RunCommand(gm, "/setsurname Bob Smith"));

                Assert.Equal("Smith", bob.Surname);
                Assert.Contains(gm.Sent, s => s.Contains("Changed surname successfully."));
            }
        }

        [Fact]
        public void ChangeClass_binds_decimal_modifier()
        {
            var (fixture, gm, map) = WorldAndGm();
            using (fixture)
            {
                var bob = MakeBob(fixture, map, Player.States.Ready);
                bob.Level = 5;
                bob.Experience = 100;
                bob.Spellbook = new Spellbook(bob, fixture.Settings);
                fixture.RegisterOnlinePlayer(bob);
                fixture.RegisterDatabasePlayer(bob);

                Assert.True(fixture.RunCommand(gm, "/changeclass Bob Warrior 1.5"));

                Assert.Equal("Warrior", bob.Class.ClassName);
                Assert.Equal(150, bob.Experience);
                Assert.Contains(gm.Sent, s => s.Contains("Changed class successfully."));
            }
        }

        [Fact]
        public void ChangeClass_omitted_modifier_defaults_to_one()
        {
            var (fixture, gm, map) = WorldAndGm();
            using (fixture)
            {
                var bob = MakeBob(fixture, map, Player.States.Ready);
                bob.Level = 5;
                bob.Experience = 100;
                bob.Spellbook = new Spellbook(bob, fixture.Settings);
                fixture.RegisterOnlinePlayer(bob);
                fixture.RegisterDatabasePlayer(bob);

                Assert.True(fixture.RunCommand(gm, "/changeclass Bob Warrior"));

                Assert.Equal("Warrior", bob.Class.ClassName);
                Assert.Equal(100, bob.Experience);
                Assert.Contains(gm.Sent, s => s.Contains("Changed class successfully."));
            }
        }

        [Fact]
        public void ChangeName_renames_registered_player()
        {
            var (fixture, gm, map) = WorldAndGm();
            using (fixture)
            {
                var bob = MakeBob(fixture, map, Player.States.Ready);
                fixture.RegisterOnlinePlayer(bob);
                fixture.RegisterDatabasePlayer(bob);

                Assert.True(fixture.RunCommand(gm, "/changename Bob Bobby"));

                Assert.Equal("Bobby", bob.Name);
                Assert.Contains(gm.Sent, s => s.Contains("Changed name successfully."));
                Assert.Null(fixture.World.PlayerHandler.GetPlayerFromData("Bob"));
                Assert.Same(bob, fixture.World.PlayerHandler.GetPlayerFromData("Bobby"));
            }
        }

        [Fact]
        public void ChangeName_refused_when_new_name_is_taken()
        {
            var (fixture, gm, map) = WorldAndGm();
            using (fixture)
            {
                var bob = MakeBob(fixture, map, Player.States.Ready);
                var alice = MakeBob(fixture, map, Player.States.NotLoggedIn);
                alice.Name = "Alice";
                fixture.RegisterOnlinePlayer(bob);
                fixture.RegisterDatabasePlayer(bob);
                fixture.RegisterDatabasePlayer(alice);

                Assert.True(fixture.RunCommand(gm, "/changename Bob Alice"));

                Assert.Contains(gm.Sent, s => s.Contains("New name Alice is already used."));
                Assert.Equal("Bob", bob.Name);
            }
        }

        [Fact]
        public void CheckName_reports_used_and_unused()
        {
            var (fixture, gm, map) = WorldAndGm();
            using (fixture)
            {
                var bob = MakeBob(fixture, map, Player.States.NotLoggedIn);
                fixture.RegisterDatabasePlayer(bob);

                Assert.True(fixture.RunCommand(gm, "/checkname Bob"));
                Assert.Contains(gm.Sent, s => s.Contains("Bob is used."));

                gm.Sent.Clear();
                Assert.True(fixture.RunCommand(gm, "/checkname Ghost"));
                Assert.Contains(gm.Sent, s => s.Contains("Ghost is currently unused."));
            }
        }

        [Fact]
        public void SetPassword_sets_password_and_validates_length()
        {
            var (fixture, gm, map) = WorldAndGm();
            using (fixture)
            {
                var bob = MakeBob(fixture, map, Player.States.NotLoggedIn);
                fixture.RegisterDatabasePlayer(bob);

                Assert.True(fixture.RunCommand(gm, "/setpassword Bob ab"));
                Assert.Contains(gm.Sent, s => s.Contains("Password needs to be more than 3 characters long."));

                gm.Sent.Clear();
                Assert.True(fixture.RunCommand(gm, "/setpassword Bob " + new string('x', 17)));
                Assert.Contains(gm.Sent, s => s.Contains("Password needs to be 16 characters or fewer."));

                gm.Sent.Clear();
                Assert.True(fixture.RunCommand(gm, "/setpassword Bob hunter two"));
                Assert.Contains(gm.Sent, s => s.Contains("Password has been changed."));
                Assert.NotEqual(string.Empty, bob.PasswordHash);
            }
        }

        [Fact]
        public void MacroCheck_starts_check_on_online_player()
        {
            var (fixture, gm, map) = WorldAndGm();
            using (fixture)
            {
                var bob = MakeBob(fixture, map, Player.States.Ready);
                // LastMacroCheckTime is in stopwatch ticks; backdate past the 2h cooldown so the guard
                // passes regardless of machine uptime (a fresh player has LastMacroCheckTime = 0).
                bob.LastMacroCheckTime = fixture.World.TimeNow - (long)(TimeSpan.FromHours(3).TotalSeconds * fixture.World.TimerFrequency);
                fixture.RegisterOnlinePlayer(bob);

                Assert.True(fixture.RunCommand(gm, "/macrocheck Bob"));

                Assert.Null(gm.Sent.FirstOrDefault(s => s.Contains("Couldn't find")));
                Assert.NotNull(bob.MacroCheckEvent);
                Assert.Equal(10, bob.MacroCheckEvent!.Code.Length);
                Assert.Contains(bob.Windows, w => w.Type == Window.WindowTypes.MacroCheck);
            }
        }

        [Fact]
        public void MacroCheck_unknown_target_reports_name()
        {
            var (fixture, gm, _) = WorldAndGm();
            using (fixture)
            {
                Assert.True(fixture.RunCommand(gm, "/macrocheck Ghost"));

                Assert.Contains(gm.Sent, s => s.Contains("Couldn't find player Ghost."));
            }
        }

        [Fact]
        public void PlayerInfo_opens_window_for_registered_player()
        {
            var (fixture, gm, map) = WorldAndGm();
            using (fixture)
            {
                var bob = MakeBob(fixture, map, Player.States.NotLoggedIn);
                fixture.RegisterDatabasePlayer(bob);

                Assert.True(fixture.RunCommand(gm, "/playerinfo Bob"));

                Assert.Contains(gm.Windows, w => w is PlayerInfoWindow);
            }
        }

        [Fact]
        public void PlayerInfo_unknown_player_reports_not_found()
        {
            var (fixture, gm, _) = WorldAndGm();
            using (fixture)
            {
                Assert.True(fixture.RunCommand(gm, "/playerinfo Ghost"));

                Assert.Contains(gm.Sent, s => s.Contains("Couldn't find player."));
            }
        }
    }
}
