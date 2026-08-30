using Goose;
using Goose.Commands;
using Goose.IntegrationTests.Fixtures;
using Xunit;

namespace Goose.IntegrationTests
{
    public class Part3MigrationTests
    {
        private static (GlobalScriptFixture fixture, GlobalScriptFixture.CapturingPlayer player, Map map) WorldAndPlayer()
        {
            var fixture = new GlobalScriptFixture();
            var map = fixture.AddBaseMap(1, "Test");
            var player = fixture.CommandPlayerOn(map, 1, 2, "Tester");
            player.Access = Player.AccessStatus.Normal;
            return (fixture, player, map);
        }

        private static List<string> HelpLines(GlobalScriptFixture fixture, Player player, string? name)
            => HelpFormatter.BuildPages(player, fixture.World.Commands, name)!.SelectMany(p => p).ToList();

        [Fact]
        public void Warp_gm_caller_warps_to_map_and_position()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                player.Access = Player.AccessStatus.GameMaster;
                fixture.AddBaseMap(2, "Warp");

                Assert.True(fixture.RunCommand(player, "/warp 2 5 5"));

                Assert.Equal(2, player.MapID);
                Assert.Equal(5, player.MapX);
                Assert.Equal(5, player.MapY);
            }
        }

        /// <summary>Denial is swallowed (anti-probing): the packet is consumed but the
        /// player gets no reply and does not move.</summary>
        [Fact]
        public void Warp_normal_caller_is_swallowed()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                fixture.AddBaseMap(2, "Warp");

                Assert.True(fixture.RunCommand(player, "/warp 2 5 5"));

                Assert.Empty(player.Sent);
                Assert.Equal(1, player.MapID);
                Assert.Equal(1, player.MapX);
                Assert.Equal(2, player.MapY);
            }
        }

        private static (ItemTemplate ticket, ItemTemplate stats, ItemTemplate look) Templates(GlobalScriptFixture fixture)
        {
            var ticket = fixture.AddBaseItemTemplate(823, "Custom Ticket", ItemTemplate.UseTypes.Armor);
            var stats = fixture.AddBaseItemTemplate(900, "Steel Sword", ItemTemplate.UseTypes.Weapon);
            var look = fixture.AddBaseItemTemplate(901, "Shadow Sword", ItemTemplate.UseTypes.Weapon);
            return (ticket, stats, look);
        }

        private static Item LoadItem(ItemTemplate template)
        {
            var item = new Item();
            item.LoadFromTemplate(template);
            return item;
        }

        [Fact]
        public void Custom_make_full_flow_creates_the_custom_item()
        {
            var fixture = new GlobalScriptFixture();
            fixture.Settings.CustomTicketId = 823;
            using (fixture)
            {
                var map = fixture.AddBaseMap(1, "Test");
                var player = fixture.CommandPlayerOn(map, 5, 5, "Tester");
                player.Access = Player.AccessStatus.Normal;

                var (ticket, stats, look) = Templates(fixture);
                var bag = player.Inventory.GetCombineBagContainer();
                bag.SetSlot(1, new ItemSlot { Item = LoadItem(ticket) });
                bag.SetSlot(2, new ItemSlot { Item = LoadItem(stats) });
                bag.SetSlot(3, new ItemSlot { Item = LoadItem(look) });

                Assert.True(fixture.RunCommand(player, "/custom make 255 0 0 255 MySword"));

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
        public void Help_gm_sees_all_seven_builtin_sections()
        {
            var (fixture, _, map) = WorldAndPlayer();
            using (fixture)
            {
                var gm = fixture.CommandPlayerOn(map, 2, 2, name: "GM");
                gm.Access = Player.AccessStatus.GameMaster;

                Assert.True(fixture.RunCommand(gm, "/help"));
                Assert.Contains(gm.Windows, w => w is HelpWindow);

                // No dimension scripts are loaded in this fixture, so the registry
                // holds exactly the seven built-in sections.
                var sections = fixture.World.Commands.Sections.Select(s => s.Name).OrderBy(n => n).ToArray();
                Assert.Equal(["Admin", "Customs", "General", "GM", "Guild", "Party", "Pets"], sections);

                var lines = HelpLines(fixture, gm, null);
                foreach (var header in new[]
                    { "General (20)", "GM (23)", "Admin (10)", "Guild (7)", "Pets (6)", "Party (4)", "Customs (1)" })
                    Assert.Contains(lines, l => l == header);
            }
        }

        /// <summary>A Normal player sees the GM section with count 1: /givecredits is
        /// Open (legacy table) but lives in the GM section, and a section is visible if it
        /// contains at least one usable command. Admin has no Open commands, so it is
        /// hidden entirely.</summary>
        [Fact]
        public void Help_normal_sees_only_the_open_sections()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                Assert.True(fixture.RunCommand(player, "/help"));
                Assert.Contains(player.Windows, w => w is HelpWindow);

                var lines = HelpLines(fixture, player, null);
                foreach (var header in new[]
                    { "General (20)", "GM (1)", "Guild (7)", "Pets (6)", "Party (4)", "Customs (1)" })
                    Assert.Contains(lines, l => l == header);
                Assert.DoesNotContain(lines, l => l.StartsWith("Admin ("));
                Assert.Contains(lines, l => l.StartsWith("Usage: /givecredits "));
                Assert.DoesNotContain(lines, l => l.StartsWith("Usage: /warp "));
            }
        }

        [Fact]
        public void Help_custom_lists_the_four_subcommands_with_make_as_the_name()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                Assert.True(fixture.RunCommand(player, "/help custom"));
                Assert.Contains(player.Windows, w => w is HelpWindow);

                var text = string.Join(" ", HelpLines(fixture, player, "custom").Select(l => l.TrimStart()));
                Assert.Contains("Usage: /custom help - Show the custom instructions.", text);
                Assert.Contains("Usage: /custom kill - Remove the custom preview from the map.", text);
                Assert.Contains("Usage: /custom preview <r> <g> <b> <a> <name...> - Preview the custom's colour and look.", text);
                Assert.Contains("Usage: /custom make <r> <g> <b> <a> <name...>", text);
                Assert.Equal(4, text.Split("Usage: /custom ", StringSplitOptions.RemoveEmptyEntries).Length - 1);
                Assert.DoesNotContain("/custom create", text);
            }
        }
    }
}
