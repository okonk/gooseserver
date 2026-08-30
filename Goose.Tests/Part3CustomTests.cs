using Goose;
using Goose.Testing;
using Xunit;

namespace Goose.Tests
{
    public class Part3CustomTests
    {
        private static (TestWorldFixture fixture, TestWorldFixture.CapturingPlayer player, Map map) WorldAndPlayer()
        {
            var fixture = new TestWorldFixture(s => s.CustomTicketId = 823);
            var map = fixture.AddBaseMap(1, "Test");
            var player = fixture.CommandPlayerOn(map, 5, 5, "Tester");
            return (fixture, player, map);
        }

        private static (ItemTemplate ticket, ItemTemplate stats, ItemTemplate look) Templates(TestWorldFixture fixture)
        {
            var ticket = fixture.AddBaseItemTemplate(823, "Custom Ticket", ItemTemplate.UseTypes.Armor);
            var stats = fixture.AddBaseItemTemplate(900, "Steel Sword", ItemTemplate.UseTypes.Weapon);
            var look = fixture.AddBaseItemTemplate(901, "Shadow Sword", ItemTemplate.UseTypes.Weapon);
            return (ticket, stats, look);
        }

        private static void PlaceCombineBag(TestWorldFixture.CapturingPlayer player, ItemTemplate ticket, ItemTemplate stats, ItemTemplate look)
        {
            var bag = player.Inventory.GetCombineBagContainer();
            bag.SetSlot(1, new ItemSlot { Item = LoadItem(ticket) });
            bag.SetSlot(2, new ItemSlot { Item = LoadItem(stats) });
            bag.SetSlot(3, new ItemSlot { Item = LoadItem(look) });
        }

        private static Item LoadItem(ItemTemplate template)
        {
            var item = new Item();
            item.LoadFromTemplate(template);
            return item;
        }

        [Fact]
        public void Bare_custom_lists_subcommands_without_ticket_check()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                Assert.True(fixture.RunCommand(player, "/custom"));

                var list = string.Join("\n", player.Sent);
                Assert.Contains("help", list);
                Assert.Contains("kill", list);
                Assert.Contains("preview", list);
                Assert.Contains("make", list);
                Assert.Contains("Usage: /custom make <r> <g> <b> <a> <name...>", list);
                Assert.Contains("Usage: /custom preview <r> <g> <b> <a> <name...>", list);
                Assert.DoesNotContain("You need a custom ticket", list);
            }
        }

        [Fact]
        public void Unknown_subcommand_lists_subcommands()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                Assert.True(fixture.RunCommand(player, "/custom frobnicate"));

                var list = string.Join("\n", player.Sent);
                Assert.Contains("make", list);
                Assert.Contains("Usage: /custom make <r> <g> <b> <a> <name...>", list);
                Assert.DoesNotContain("You need a custom ticket", list);
            }
        }

        [Fact]
        public void Help_with_ticket_sends_instructions()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                var (ticket, stats, look) = Templates(fixture);
                PlaceCombineBag(player, ticket, stats, look);

                Assert.True(fixture.RunCommand(player, "/custom help"));

                Assert.Contains(player.Sent, s => s.Contains("Place custom ticket in first slot. Place the item you want the stats of in second slot. Place the item you want the look of in the third slot."));
                Assert.Contains(player.Sent, s => s.Contains("Type /custom preview <r> <g> <b> <a> <custom name> to preview the colour and look"));
                Assert.Contains(player.Sent, s => s.Contains("Type /custom make <r> <g> <b> <a> <custom name> to make the custom. It will destroy your custom ticket and source items."));
            }
        }

        [Fact]
        public void Help_without_ticket_sends_refusal()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                Assert.True(fixture.RunCommand(player, "/custom help"));

                Assert.Contains(player.Sent, s => s.Contains("You need a custom ticket in your first combine bag slot to use this command."));
            }
        }

        [Fact]
        public void Make_creates_item_and_consumes_ticket()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                var (ticket, stats, look) = Templates(fixture);
                PlaceCombineBag(player, ticket, stats, look);

                Assert.True(fixture.RunCommand(player, "/custom make 255 0 0 255 MySword"));

                var bag = player.Inventory.GetCombineBagContainer();
                var result = bag.GetSlot(1)!;
                Assert.Equal("MySword", result.Item.Name);
                Assert.Equal(900, result.Item.TemplateID);
                Assert.Equal(255, result.Item.GraphicR);
                Assert.Equal(0, result.Item.GraphicG);
                Assert.Equal(0, result.Item.GraphicB);
                Assert.Equal(255, result.Item.GraphicA);
                Assert.Null(bag.GetSlot(2));
                Assert.Null(bag.GetSlot(3));
            }
        }

        [Fact]
        public void Make_with_ticket_stack_above_one_keeps_ticket_and_replaces_stats_slot()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                var ticket = fixture.AddBaseItemTemplate(823, "Custom Ticket", ItemTemplate.UseTypes.Armor, t => t.StackSize = 10);
                var stats = fixture.AddBaseItemTemplate(900, "Steel Sword", ItemTemplate.UseTypes.Weapon);
                var look = fixture.AddBaseItemTemplate(901, "Shadow Sword", ItemTemplate.UseTypes.Weapon);
                var bag = player.Inventory.GetCombineBagContainer();
                bag.SetSlot(1, new ItemSlot { Item = LoadItem(ticket), Stack = 2 });
                bag.SetSlot(2, new ItemSlot { Item = LoadItem(stats) });
                bag.SetSlot(3, new ItemSlot { Item = LoadItem(look) });

                Assert.True(fixture.RunCommand(player, "/custom make 255 0 0 255 MySword"));

                var ticketSlot = bag.GetSlot(1)!;
                Assert.Equal(823, ticketSlot.Item.TemplateID);
                Assert.Equal(1, ticketSlot.Stack);

                var customSlot = bag.GetSlot(2)!;
                Assert.Equal("MySword", customSlot.Item.Name);
                Assert.Equal(900, customSlot.Item.TemplateID);
                Assert.Equal(255, customSlot.Item.GraphicR);
                Assert.Equal(0, customSlot.Item.GraphicG);
                Assert.Equal(0, customSlot.Item.GraphicB);
                Assert.Equal(255, customSlot.Item.GraphicA);

                Assert.Null(bag.GetSlot(3));
            }
        }

        [Fact]
        public void Create_alias_behaves_identically_to_make()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                var (ticket, stats, look) = Templates(fixture);
                PlaceCombineBag(player, ticket, stats, look);

                Assert.True(fixture.RunCommand(player, "/custom create 255 0 0 255 MySword"));

                var bag = player.Inventory.GetCombineBagContainer();
                var result = bag.GetSlot(1)!;
                Assert.Equal("MySword", result.Item.Name);
                Assert.Equal(900, result.Item.TemplateID);
                Assert.Equal(255, result.Item.GraphicR);
                Assert.Equal(0, result.Item.GraphicG);
                Assert.Equal(0, result.Item.GraphicB);
                Assert.Equal(255, result.Item.GraphicA);
                Assert.Null(bag.GetSlot(2));
                Assert.Null(bag.GetSlot(3));
            }
        }

        [Fact]
        public void Create_alias_usage_line_uses_primary_name()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                var (ticket, stats, look) = Templates(fixture);
                PlaceCombineBag(player, ticket, stats, look);

                Assert.True(fixture.RunCommand(player, "/custom create 1 2 3 4"));

                Assert.Contains(player.Sent, s => s.Contains("Usage: /custom make <r> <g> <b> <a> <name...>"));
                Assert.DoesNotContain(player.Sent, s => s.Contains("/custom create"));
            }
        }

        [Fact]
        public void Make_multiword_name_joins_tail()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                var (ticket, stats, look) = Templates(fixture);
                PlaceCombineBag(player, ticket, stats, look);

                Assert.True(fixture.RunCommand(player, "/custom make 255 0 0 255 My Sword"));

                var bag = player.Inventory.GetCombineBagContainer();
                Assert.Equal("My Sword", bag.GetSlot(1)!.Item.Name);
            }
        }

        [Fact]
        public void Make_out_of_range_r_keeps_legacy_message()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                var (ticket, stats, look) = Templates(fixture);
                PlaceCombineBag(player, ticket, stats, look);

                Assert.True(fixture.RunCommand(player, "/custom make 300 0 0 0 X"));

                Assert.Contains(player.Sent, s => s.Contains("/custom: invalid r value"));
                Assert.Equal("Custom Ticket", player.Inventory.GetCombineBagContainer().GetSlot(1)!.Item.Name);
            }
        }

        [Fact]
        public void Make_without_name_sends_usage()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                var (ticket, stats, look) = Templates(fixture);
                PlaceCombineBag(player, ticket, stats, look);

                Assert.True(fixture.RunCommand(player, "/custom make 1 2 3 4"));

                Assert.Contains(player.Sent, s => s.Contains("Usage: /custom make <r> <g> <b> <a> <name...>"));
            }
        }

        [Fact]
        public void Preview_without_name_sends_usage()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                var (ticket, stats, look) = Templates(fixture);
                PlaceCombineBag(player, ticket, stats, look);

                Assert.True(fixture.RunCommand(player, "/custom preview 1 2 3 4"));

                Assert.Contains(player.Sent, s => s.Contains("Usage: /custom preview <r> <g> <b> <a> <name...>"));
            }
        }

        [Fact]
        public void Preview_sends_mkc_packet()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                var (ticket, stats, look) = Templates(fixture);
                PlaceCombineBag(player, ticket, stats, look);

                Assert.True(fixture.RunCommand(player, "/custom preview 10 20 30 255 Shadow"));

                Assert.Contains(player.Sent, s => s.StartsWith("MKC9000,"));
                Assert.Contains(player.Sent, s => s.Contains("Custom Preview,"));
            }
        }

        [Fact]
        public void Make_without_ticket_sends_refusal()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                Assert.True(fixture.RunCommand(player, "/custom make 255 0 0 255 X"));

                Assert.Contains(player.Sent, s => s.Contains("You need a custom ticket in your first combine bag slot to use this command."));
            }
        }

        [Fact]
        public void Kill_with_ticket_erases_preview_character()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                var (ticket, stats, look) = Templates(fixture);
                PlaceCombineBag(player, ticket, stats, look);

                Assert.True(fixture.RunCommand(player, "/custom kill"));

                Assert.Contains(player.Sent, s => s.StartsWith("ERC9000"));
            }
        }
    }
}
