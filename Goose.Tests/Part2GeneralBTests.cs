using Goose;
using Goose.Events;
using Goose.Testing;
using Xunit;

namespace Goose.Tests
{
    public class Part2GeneralBTests
    {
        private static (TestWorldFixture fixture, TestWorldFixture.CapturingPlayer player, Map map) WorldAndPlayer()
        {
            var fixture = new TestWorldFixture();
            var map = fixture.AddBaseMap(1, "Test");
            var player = fixture.CommandPlayerOn(map, 1, 2, "Tester");
            return (fixture, player, map);
        }

        [Fact]
        public void BuyVita_bare_buys_one()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                fixture.Settings.IncreaseVitaBuyAmount = 100;
                fixture.Settings.VitaBuyAmount = 10;
                fixture.World.ClassHandler.GetClass(0)!.VitaCost = 10;
                player.Level = 50;
                player.Experience = 100;
                player.BaseStats.HP = 100;

                fixture.RunCommand(player, "/buyvita");

                Assert.Contains(player.Sent, s => s.Contains("Bought 10 hp for 12 experience."));
                Assert.Equal(110, player.BaseStats.HP);
                Assert.Equal(88, player.Experience);
            }
        }

        [Fact]
        public void BuyVita_bad_token_sends_usage()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                fixture.RunCommand(player, "/buyvita abc");

                Assert.Contains(player.Sent, s => s.Contains("Usage: /buyvita [buys]"));
            }
        }

        [Fact]
        public void BuyMana_bare_buys_one()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                fixture.Settings.IncreaseManaBuyAmount = 100;
                fixture.Settings.ManaBuyAmount = 5;
                fixture.World.ClassHandler.GetClass(0)!.ManaCost = 10;
                player.Level = 50;
                player.Experience = 100;
                player.BaseStats.MP = 100;

                fixture.RunCommand(player, "/buymana");

                Assert.Contains(player.Sent, s => s.Contains("Bought 5 mp for 12 experience."));
                Assert.Equal(105, player.BaseStats.MP);
                Assert.Equal(88, player.Experience);
            }
        }

        [Fact]
        public void Rank_no_arg_sends_usage()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                fixture.RunCommand(player, "/rank");

                Assert.Contains(player.Sent, s => s.Contains("Usage: /rank [all, gold, <classname>]"));
                Assert.Empty(player.Windows);
            }
        }

        [Fact]
        public void Rank_all_opens_all_ranks_window()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                fixture.RunCommand(player, "/rank all");

                var window = Assert.Single(player.Windows);
                Assert.Equal(Window.WindowTypes.Rank, window.Type);
                Assert.Equal("All Ranks", window.Title);
            }
        }

        [Fact]
        public void Rank_all_extra_tokens_still_opens_all_ranks_window()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                fixture.RunCommand(player, "/rank all extra");

                var window = Assert.Single(player.Windows);
                Assert.Equal(Window.WindowTypes.Rank, window.Type);
                Assert.Equal("All Ranks", window.Title);
            }
        }

        [Fact]
        public void Rank_gold_opens_gold_ranks_window()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                fixture.RunCommand(player, "/rank gold");

                var window = Assert.Single(player.Windows);
                Assert.Equal(Window.WindowTypes.Rank, window.Type);
                Assert.Equal("Gold Ranks", window.Title);
            }
        }

        [Fact]
        public void Rank_class_name_opens_class_ranks_window()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                fixture.World.RankHandler.ClassRanks["commoner"] = new Ranks(Ranks.RankTypes.Class, 1);

                fixture.RunCommand(player, "/rank commoner");

                var window = Assert.Single(player.Windows);
                Assert.Equal("Commoner Ranks", window.Title);
            }
        }

        [Fact]
        public void Rank_unknown_class_sends_usage()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                fixture.RunCommand(player, "/rank ghost");

                Assert.Contains(player.Sent, s => s.Contains("Usage: /rank [all, gold, <classname>]"));
                Assert.Empty(player.Windows);
            }
        }

        [Fact]
        public void Hairdye_accept_charges_cost_and_dyed()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                fixture.Settings.HairdyeCommandCost = 100;
                player.Gold = 100;

                fixture.RunCommand(player, "/hairdye accept 255 0 0 255");

                Assert.Equal(0, player.Gold);
                Assert.Equal(255, player.HairR);
                Assert.Equal(0, player.HairG);
                Assert.Equal(0, player.HairB);
                Assert.Equal(255, player.HairA);
                Assert.Contains(player.Sent, s => s.Contains(P.UpdateCharacter(player)));
            }
        }

        [Fact]
        public void Hairdye_accept_insufficient_gold_is_refused()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                fixture.Settings.HairdyeCommandCost = 100;
                player.Gold = 50;

                fixture.RunCommand(player, "/hairdye accept 255 0 0 255");

                Assert.Contains(player.Sent, s => s.Contains("/hairdye accept requires 100 gold."));
                Assert.Equal(50, player.Gold);
                Assert.Equal(0, player.HairR);
            }
        }

        [Fact]
        public void Hairdye_bare_numeric_without_verb_is_silent_noop()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                fixture.Settings.HairdyeCommandCost = 100;
                player.Gold = 100;

                fixture.RunCommand(player, "/hairdye 255 0 0 255");

                Assert.Empty(player.Sent);
                Assert.Equal(100, player.Gold);
                Assert.Equal(0, player.HairR);
            }
        }

        [Fact]
        public void Hairdye_accept_out_of_range_sends_refusal()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                fixture.Settings.HairdyeCommandCost = 100;
                player.Gold = 100;

                fixture.RunCommand(player, "/hairdye accept 300 0 0 0");

                Assert.Contains(player.Sent, s => s.Contains("/hairdye: invalid r value"));
                Assert.Equal(100, player.Gold);
            }
        }

        [Fact]
        public void Hairdye_bare_sends_usage()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                fixture.RunCommand(player, "/hairdye");

                Assert.Contains(player.Sent, s => s.Contains("Usage: /hairdye [preview|kill|accept] <r> <g> <b> <a>"));
            }
        }

        [Fact]
        public void Hairdye_help_sends_usage()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                fixture.RunCommand(player, "/hairdye help");

                Assert.Contains(player.Sent, s => s.Contains("Usage: /hairdye [preview|kill|accept] <r> <g> <b> <a>"));
            }
        }

        [Fact]
        public void Aether_sets_threshold()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                fixture.RunCommand(player, "/aether 1.5");

                Assert.Equal(1.5m, player.AetherThreshold);
                Assert.Empty(player.Sent);
            }
        }

        [Fact]
        public void Aether_bad_token_sends_usage()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                fixture.RunCommand(player, "/aether abc");

                Assert.Contains(player.Sent, s => s.Contains("Usage: /aether <thres>"));
                Assert.Equal(0, player.AetherThreshold);
            }
        }

        [Fact]
        public void Aether_extra_tokens_are_ignored()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                fixture.RunCommand(player, "/aether 1.5 junk");

                Assert.Empty(player.Sent);
                Assert.Equal(1.5m, player.AetherThreshold);
            }
        }

        [Fact]
        public void Aether_bare_gets_usage_reply()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                player.AetherThreshold = 2.5m;

                fixture.RunCommand(player, "/aether ");

                Assert.Contains(player.Sent, s => s.Contains("Usage: /aether <thres>"));
                Assert.Equal(2.5m, player.AetherThreshold);
            }
        }

        [Fact]
        public void MacroConfirm_no_pending_check_is_refused()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                fixture.RunCommand(player, "/mc abc");

                Assert.Contains(player.Sent, s => s.Contains("You don't have a current macrocheck to do."));
            }
        }

        [Fact]
        public void MacroConfirm_wrong_code_is_refused()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                player.MacroCheckEvent = new MacroCheckEvent { Code = "abc def" };

                fixture.RunCommand(player, "/mc abc x");

                Assert.Contains(player.Sent, s => s.Contains("Macrocheck code doesn't match.. try again."));
                Assert.NotNull(player.MacroCheckEvent);
            }
        }

        [Fact]
        public void MacroConfirm_correct_code_awards_experience()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                player.MacroCheckEvent = new MacroCheckEvent { Code = "abc def" };

                fixture.RunCommand(player, "/mc abc def");

                Assert.Contains(player.Sent, s => s.Contains("Macrocheck passed. You earned 1mil experience."));
                Assert.Null(player.MacroCheckEvent);
                Assert.Equal(1000000, player.Experience);
            }
        }

        [Fact]
        public void Toggle_gminvisible_as_normal_is_swallowed()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                fixture.RunCommand(player, "/toggle gm-invisible");

                Assert.Empty(player.Sent);
                Assert.Equal(0, (int)player.ToggleSettings);
            }
        }

        [Fact]
        public void Toggle_gminvisible_as_gm_flips_state()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                player.Access = Player.AccessStatus.GameMaster;

                fixture.RunCommand(player, "/toggle gm-invisible");

                Assert.Contains(player.Sent, s => s.Contains("You are now visible."));
                Assert.NotEqual(0, (int)(player.ToggleSettings & Player.ToggleSetting.GMInvisible));

                fixture.RunCommand(player, "/toggle gm-invisible");

                Assert.Contains(player.Sent, s => s.Contains("You are now invisible."));
                Assert.Equal(0, (int)(player.ToggleSettings & Player.ToggleSetting.GMInvisible));
            }
        }

        [Fact]
        public void Toggle_gminvisible_is_case_insensitive_privilege_gate()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                fixture.RunCommand(player, "/toggle GM-Invisible");

                Assert.Empty(player.Sent);
                Assert.Equal(0, (int)player.ToggleSettings);
            }
        }

        [Fact]
        public void Toggle_whoinvisible_as_normal_is_swallowed()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                fixture.RunCommand(player, "/toggle who-invisible");

                Assert.Empty(player.Sent);
                Assert.Equal(0, (int)player.ToggleSettings);
            }
        }

        [Fact]
        public void Toggle_whoinvisible_as_gm_flips_state()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                player.Access = Player.AccessStatus.GameMaster;

                fixture.RunCommand(player, "/toggle who-invisible");

                Assert.Contains(player.Sent, s => s.Contains("You are now who-visible."));
                Assert.NotEqual(0, (int)(player.ToggleSettings & Player.ToggleSetting.WhoInvisible));

                fixture.RunCommand(player, "/toggle who-invisible");

                Assert.Contains(player.Sent, s => s.Contains("You are now who-invisible."));
                Assert.Equal(0, (int)(player.ToggleSettings & Player.ToggleSetting.WhoInvisible));
            }
        }

        [Fact]
        public void Toggle_exp_as_normal_works()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                fixture.RunCommand(player, "/toggle exp");

                Assert.Contains(player.Sent, s => s.Contains("Experience display is disabled."));
                Assert.NotEqual(0, (int)(player.ToggleSettings & Player.ToggleSetting.Experience));
            }
        }

        [Fact]
        public void Toggle_unknown_setting_sends_setting_list()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                fixture.RunCommand(player, "/toggle bogus");

                Assert.Contains(player.Sent, s => s.Contains("/toggle [experience|tell|curse|quest|itembuffs]"));
            }
        }
    }
}
