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

        private static ItemTemplate PotionTemplate(TestWorldFixture fixture, int id = 900)
        {
            fixture.Settings.HairDyePotionId = id;
            return fixture.AddBaseItemTemplate(id, "Hair Dye", ItemTemplate.UseTypes.OneTime, t =>
            {
                t.StackSize = 99;
                t.GraphicTile = 821122;
                t.GraphicFile = 20408;
            });
        }

        private static Item NewPotion(ItemTemplate template, string name, int r, int g, int b, int a)
        {
            var item = new Item();
            item.LoadFromTemplate(template);
            item.Name = name;
            item.Description = "Custom created by Tester";
            item.GraphicR = r;
            item.GraphicG = g;
            item.GraphicB = b;
            item.GraphicA = a;
            item.ScriptParams = $"{r},{g},{b},{a}";
            return item;
        }

        private static ItemSlot? FindPotion(Player player, string name)
        {
            for (int i = 1; i <= 30; i++)
            {
                var slot = player.Inventory.GetSlot(i);
                if (slot is not null && slot.Item.Name == name) return slot;
            }

            return null;
        }

        [Fact]
        public void Hairdye_create_charges_cost_and_gives_five_potions()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                PotionTemplate(fixture);
                fixture.Settings.HairdyeCommandCost = 5000;
                player.Gold = 5000;

                fixture.RunCommand(player, "/hairdye create 255 0 0 200 Blood Red");

                Assert.Equal(0, player.Gold);
                Assert.Equal(0, player.HairR);
                Assert.Equal(0, player.HairA);

                var potion = FindPotion(player, "Blood Red");
                Assert.NotNull(potion);
                Assert.Equal(5, potion!.Stack);
                Assert.Equal("Custom created by Tester", potion.Item.Description);
                Assert.True(potion.Item.Custom);
                Assert.Equal(821122, potion.Item.GraphicTile);
                Assert.Equal(20408, potion.Item.GraphicFile);
                Assert.Equal(255, potion.Item.GraphicR);
                Assert.Equal(0, potion.Item.GraphicG);
                Assert.Equal(0, potion.Item.GraphicB);
                Assert.Equal(200, potion.Item.GraphicA);
                Assert.Equal("255,0,0,200", potion.Item.ScriptParams);
                Assert.Contains(player.Sent, s => s.Contains("Bought 5 Blood Red for 5000 gold."));
                Assert.Contains(player.Sent, s => s.Contains(P.StatusInfo(player)));
            }
        }

        [Fact]
        public void Hairdye_create_insufficient_gold_is_refused()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                PotionTemplate(fixture);
                fixture.Settings.HairdyeCommandCost = 5000;
                player.Gold = 4999;

                fixture.RunCommand(player, "/hairdye create 255 0 0 200 Blood Red");

                Assert.Contains(player.Sent, s => s.Contains("/hairdye create requires 5000 gold."));
                Assert.Equal(4999, player.Gold);
                Assert.Null(FindPotion(player, "Blood Red"));
            }
        }

        [Fact]
        public void Hairdye_create_out_of_range_sends_refusal()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                PotionTemplate(fixture);
                fixture.Settings.HairdyeCommandCost = 5000;
                player.Gold = 5000;

                fixture.RunCommand(player, "/hairdye create 300 0 0 0 Blood Red");

                Assert.Contains(player.Sent, s => s.Contains("/hairdye: invalid r value"));
                Assert.Equal(5000, player.Gold);
            }
        }

        [Fact]
        public void Hairdye_create_without_name_sends_usage()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                PotionTemplate(fixture);
                fixture.Settings.HairdyeCommandCost = 5000;
                player.Gold = 5000;

                fixture.RunCommand(player, "/hairdye create 255 0 0 200");

                Assert.Contains(player.Sent, s => s.Contains("Usage: /hairdye create <r> <g> <b> <a> <name...>"));
                Assert.Equal(5000, player.Gold);
            }
        }

        [Fact]
        public void Hairdye_create_strips_packet_delimiters_from_the_name()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                PotionTemplate(fixture);
                fixture.Settings.HairdyeCommandCost = 5000;
                player.Gold = 5000;

                fixture.RunCommand(player, "/hairdye create 255 0 0 200 Blood,|Red");

                var potion = FindPotion(player, "BloodRed");
                Assert.NotNull(potion);
                Assert.Equal(5, potion!.Stack);
                var packet = Assert.Single(player.Sent, s => s.StartsWith("SIS"));
                Assert.Equal("BloodRed", packet.Split('|')[4]);
            }
        }

        [Fact]
        public void Hairdye_create_merges_into_an_identical_stack()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                var template = PotionTemplate(fixture);
                fixture.Settings.HairdyeCommandCost = 5000;
                player.Gold = 5000;

                var existing = NewPotion(template, "Blood Red", 255, 0, 0, 200);
                fixture.World.ItemHandler.AddAndAssignId(existing, fixture.World);
                player.Inventory.SetSlot(3, new ItemSlot { Item = existing, Stack = 2 });
                int itemCount = fixture.World.ItemHandler.GetItems().Count();

                fixture.RunCommand(player, "/hairdye create 255 0 0 200 Blood Red");

                Assert.Equal(7, player.Inventory.GetSlot(3)!.Stack);
                Assert.Equal(itemCount, fixture.World.ItemHandler.GetItems().Count());
            }
        }

        [Fact]
        public void Hairdye_create_merges_into_an_unlimited_stack_when_inventory_is_full()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                var template = PotionTemplate(fixture);
                template.StackSize = 0;
                fixture.Settings.HairdyeCommandCost = 5000;
                player.Gold = 5000;

                var existing = NewPotion(template, "Blood Red", 255, 0, 0, 200);
                fixture.World.ItemHandler.AddAndAssignId(existing, fixture.World);
                player.Inventory.SetSlot(1, new ItemSlot { Item = existing, Stack = 100 });

                var filler = fixture.AddBaseItemTemplate(901, "Filler", ItemTemplate.UseTypes.OneTime, t => t.StackSize = 99);
                var fillerItem = new Item();
                fillerItem.LoadFromTemplate(filler);
                for (int i = 2; i <= fixture.Settings.InventorySize; i++)
                    player.Inventory.SetSlot(i, new ItemSlot { Item = fillerItem, Stack = 1 });

                fixture.RunCommand(player, "/hairdye create 255 0 0 200 Blood Red");

                Assert.Equal(105, player.Inventory.GetSlot(1)!.Stack);
                Assert.Equal(0, player.Gold);
            }
        }

        [Fact]
        public void Hairdye_create_refuses_a_template_stack_smaller_than_the_purchase()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                var template = PotionTemplate(fixture);
                template.StackSize = 4;
                fixture.Settings.HairdyeCommandCost = 5000;
                player.Gold = 5000;

                fixture.RunCommand(player, "/hairdye create 255 0 0 200 Blood Red");

                Assert.Equal(5000, player.Gold);
                Assert.Null(FindPotion(player, "Blood Red"));
            }
        }

        [Fact]
        public void Hairdye_create_never_merges_into_another_colour()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                var template = PotionTemplate(fixture);
                fixture.Settings.HairdyeCommandCost = 5000;
                player.Gold = 5000;

                var blue = NewPotion(template, "Blood Red", 0, 0, 255, 200);
                fixture.World.ItemHandler.AddAndAssignId(blue, fixture.World);
                player.Inventory.SetSlot(1, new ItemSlot { Item = blue, Stack = 5 });

                fixture.RunCommand(player, "/hairdye create 255 0 0 200 Blood Red");

                Assert.Equal(5, player.Inventory.GetSlot(1)!.Stack);
                Assert.Equal("0,0,255,200", player.Inventory.GetSlot(1)!.Item.ScriptParams);

                var red = player.Inventory.GetSlot(2);
                Assert.NotNull(red);
                Assert.Equal(5, red!.Stack);
                Assert.Equal("255,0,0,200", red.Item.ScriptParams);
                Assert.Equal(255, red.Item.GraphicR);
            }
        }

        [Fact]
        public void Hairdye_create_without_inventory_space_is_refused()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                PotionTemplate(fixture);
                fixture.Settings.HairdyeCommandCost = 5000;
                player.Gold = 5000;

                var filler = fixture.AddBaseItemTemplate(901, "Filler", ItemTemplate.UseTypes.OneTime, t => t.StackSize = 99);
                var fillerItem = new Item();
                fillerItem.LoadFromTemplate(filler);
                for (int i = 1; i <= fixture.Settings.InventorySize; i++)
                    player.Inventory.SetSlot(i, new ItemSlot { Item = fillerItem, Stack = 1 });

                fixture.RunCommand(player, "/hairdye create 255 0 0 200 Blood Red");

                Assert.Contains(player.Sent, s => s.Contains("You don't have enough inventory space for the potions."));
                Assert.Equal(5000, player.Gold);
                Assert.Null(FindPotion(player, "Blood Red"));
            }
        }

        [Fact]
        public void Hairdye_bare_lists_subcommands()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                fixture.RunCommand(player, "/hairdye");

                var list = string.Join("\n", player.Sent);
                Assert.Contains("preview", list);
                Assert.Contains("kill", list);
                Assert.Contains("create", list);
                Assert.Contains("Usage: /hairdye create <r> <g> <b> <a> <name...>", list);
            }
        }

        [Fact]
        public void Hairdye_unknown_subcommand_lists_subcommands()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                fixture.RunCommand(player, "/hairdye 255 0 0 255");

                var list = string.Join("\n", player.Sent);
                Assert.Contains("Usage: /hairdye create <r> <g> <b> <a> <name...>", list);
            }
        }

        [Fact]
        public void Aether_sets_threshold()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                fixture.RunCommand(player, "/aether 1.5");

                Assert.Equal(1.5, player.AetherThreshold);
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
                Assert.Equal(1.5, player.AetherThreshold);
            }
        }

        [Fact]
        public void Aether_bare_gets_usage_reply()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                player.AetherThreshold = 2.5;

                fixture.RunCommand(player, "/aether ");

                Assert.Contains(player.Sent, s => s.Contains("Usage: /aether <thres>"));
                Assert.Equal(2.5, player.AetherThreshold);
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
