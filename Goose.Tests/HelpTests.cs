using Goose;
using Goose.Commands;
using Goose.Testing;
using Xunit;

namespace Goose.Tests
{
    public class HelpTests
    {
        private static void NoArgs(CommandContext ctx) { }
        private static void OneString(CommandContext ctx, string name) { }

        private static (TestWorldFixture world, TestWorldFixture.CapturingPlayer player, Map map) WorldAndPlayer(
            Player.AccessStatus access = Player.AccessStatus.Normal)
        {
            var world = new TestWorldFixture();
            var map = world.AddBaseMap(1, "Test");
            var player = world.CommandPlayerOn(map, 1, 1);
            player.Access = access;
            return (world, player, map);
        }

        [Fact]
        public void Wrap_short_line_is_unchanged()
        {
            Assert.Equal(["hello world"], HelpFormatter.Wrap("hello world"));
            Assert.Equal([new string('a', 42)], HelpFormatter.Wrap(new string('a', 42)));
        }

        [Fact]
        public void Wrap_breaks_at_last_word_boundary()
        {
            Assert.Equal(
                ["The quick brown fox jumps over the lazy", "dog"],
                HelpFormatter.Wrap("The quick brown fox jumps over the lazy dog"));
        }

        [Fact]
        public void Wrap_hard_breaks_overlong_word()
        {
            Assert.Equal(
                ["ab", new string('a', 42), new string('a', 3)],
                HelpFormatter.Wrap("ab " + new string('a', 45)));
            Assert.Equal(
                [new string('b', 42), new string('b', 8)],
                HelpFormatter.Wrap(new string('b', 50)));
        }

        [Fact]
        public void BuildPages_normal_hides_sections_without_visible_commands()
        {
            var (_, normal, _) = WorldAndPlayer();
            var (world, gm, _) = WorldAndPlayer(Player.AccessStatus.GameMaster);
            var registry = new CommandRegistry();
            Assert.True(registry.Register("/open", "Pub", "An open command", NoArgs));
            Assert.True(registry.Register("/secret", AccessPrivilege.Ban, "Priv", "A restricted command", NoArgs));

            var normalPages = HelpFormatter.BuildPages(normal, registry, null);
            Assert.NotNull(normalPages);
            Assert.Equal(2, normalPages!.Count);
            Assert.Equal(["Pub (1)"], normalPages[0]);
            Assert.Equal(["Usage: /open - An open command"], normalPages[1]);

            var gmPages = HelpFormatter.BuildPages(gm, registry, null);
            Assert.NotNull(gmPages);
            Assert.Equal(3, gmPages!.Count);
            Assert.Equal(["Pub (1)", "Priv (1)"], gmPages[0]);
            Assert.Equal(["Usage: /secret - A restricted command"], gmPages[2]);
        }

        [Fact]
        public void BuildPages_section_page_lines_are_wrapped_with_indent()
        {
            var (_, player, _) = WorldAndPlayer();
            var registry = new CommandRegistry();
            Assert.True(registry.Register("/custom ", "Crafting", "Build a custom item.", OneString));

            var pages = HelpFormatter.BuildPages(player, registry, null);
            Assert.NotNull(pages);
            Assert.Equal(
                ["Usage: /custom <name> - Build a custom", "  item."],
                pages![1]);
        }

        [Fact]
        public void BuildPages_name_without_privilege_returns_null()
        {
            var (_, player, _) = WorldAndPlayer();
            var registry = new CommandRegistry();
            Assert.True(registry.Register("/secret", AccessPrivilege.Ban, "Priv", "A restricted command", NoArgs));

            Assert.Null(HelpFormatter.BuildPages(player, registry, "secret"));
            Assert.Null(HelpFormatter.BuildPages(player, registry, "SECRET"));
        }

        [Fact]
        public void BuildPages_same_name_prefers_first_usable_definition()
        {
            var (_, player, _) = WorldAndPlayer();
            var registry = new CommandRegistry();
            Assert.True(registry.Register("/dup", AccessPrivilege.Ban, "Box", "Restricted dup.", NoArgs));
            Assert.True(registry.Register("/dup ", "Box", "Open dup.", NoArgs));

            var pages = HelpFormatter.BuildPages(player, registry, "dup");
            Assert.NotNull(pages);
            Assert.Single(pages!);
            Assert.Equal(["Open dup.", "Usage: /dup"], pages[0]);
        }

        [Fact]
        public void BuildPages_unknown_name_returns_null()
        {
            var (_, player, _) = WorldAndPlayer();
            var registry = new CommandRegistry();
            Assert.True(registry.Register("/open", "Pub", "An open command", NoArgs));

            Assert.Null(HelpFormatter.BuildPages(player, registry, "nope"));
        }

        [Fact]
        public void BuildPages_name_matches_command_and_section_shows_both_in_order()
        {
            var (_, player, _) = WorldAndPlayer();
            var registry = new CommandRegistry();
            Assert.True(registry.Register("/test ", "Misc", "A test command.", NoArgs));
            Assert.True(registry.Register("/other", "test", "Another command.", NoArgs));

            var pages = HelpFormatter.BuildPages(player, registry, "test");
            Assert.NotNull(pages);
            Assert.Equal(2, pages!.Count);
            Assert.Equal(["A test command.", "Usage: /test"], pages[0]);
            Assert.Equal(["Usage: /other - Another command."], pages[1]);
        }

        [Fact]
        public void BuildPages_restricted_command_same_name_as_public_section()
        {
            var (_, normal, _) = WorldAndPlayer();
            var (_, gm, _) = WorldAndPlayer(Player.AccessStatus.GameMaster);
            var registry = new CommandRegistry();
            Assert.True(registry.Register("/admin", AccessPrivilege.Ban, "Other", "Admin stuff.", NoArgs));
            Assert.True(registry.Register("/other", "admin", "A public command.", NoArgs));

            var normalPages = HelpFormatter.BuildPages(normal, registry, "admin");
            Assert.NotNull(normalPages);
            var expected = new List<List<string>> { new() { "Usage: /other - A public command." } };
            Assert.Equal(expected, normalPages!);

            var gmPages = HelpFormatter.BuildPages(gm, registry, "admin");
            Assert.NotNull(gmPages);
            Assert.Equal(2, gmPages!.Count);
            Assert.Equal(["Admin stuff.", "Usage: /admin"], gmPages[0]);
            Assert.Equal(["Usage: /other - A public command."], gmPages[1]);
        }

        [Fact]
        public void BuildPages_command_page_format()
        {
            var (_, player, _) = WorldAndPlayer();
            var registry = new CommandRegistry();
            registry.SeedAttributedTypes([typeof(Warp2Command)]);

            var pages = HelpFormatter.BuildPages(player, registry, "warp2");
            Assert.NotNull(pages);
            Assert.Equal(
                [
                    "Warp players around.",
                    "Usage: /warp2 <name>",
                    "Usage: /warp2 here <target> - Warp to a",
                    "  location.",
                ],
                pages![0]);
        }

        [Fact]
        public void BuildPages_filters_restricted_subcommands_by_privilege()
        {
            var (_, normal, _) = WorldAndPlayer();
            var (_, gm, _) = WorldAndPlayer(Player.AccessStatus.GameMaster);
            var registry = new CommandRegistry();
            registry.SeedAttributedTypes([typeof(Warp2Command)]);

            var normalPages = HelpFormatter.BuildPages(normal, registry, "warp2");
            Assert.NotNull(normalPages);
            Assert.Equal(
                [
                    "Warp players around.",
                    "Usage: /warp2 <name>",
                    "Usage: /warp2 here <target> - Warp to a",
                    "  location.",
                ],
                normalPages![0]);

            var gmPages = HelpFormatter.BuildPages(gm, registry, "warp2");
            Assert.NotNull(gmPages);
            Assert.Equal(
                [
                    "Warp players around.",
                    "Usage: /warp2 <name>",
                    "Usage: /warp2 here <target> - Warp to a",
                    "  location.",
                    "Usage: /warp2 all - Warp everyone.",
                ],
                gmPages![0]);
        }

        [Fact]
        public void BuildPages_section_over_page_height_splits_with_no_loss()
        {
            var (_, player, _) = WorldAndPlayer();
            var registry = new CommandRegistry();
            for (var i = 1; i <= 40; i++)
                Assert.True(registry.Register($"/c{i:D2}", "Big", $"h{i}", NoArgs));

            var pages = HelpFormatter.BuildPages(player, registry, "Big");
            Assert.NotNull(pages);
            Assert.Equal(3, pages!.Count);
            Assert.Equal(19, pages[0].Count);
            Assert.Equal(19, pages[1].Count);
            Assert.Equal(2, pages[2].Count);

            var expected = Enumerable.Range(1, 40).Select(i => $"Usage: /c{i:D2} - h{i}").ToList();
            Assert.Equal(expected, pages.SelectMany(p => p).ToList());
        }

        [Fact]
        public void BuildPages_section_list_follows_registration_order()
        {
            var (_, player, _) = WorldAndPlayer();
            var registry = new CommandRegistry();
            Assert.True(registry.Register("/a1", "Alpha", "a1", NoArgs));
            Assert.True(registry.Register("/a2", "Alpha", "a2", NoArgs));
            Assert.True(registry.Register("/b1", "Beta", "b1", NoArgs));

            var pages = HelpFormatter.BuildPages(player, registry, null);
            Assert.NotNull(pages);
            Assert.Equal(["Alpha (2)", "Beta (1)"], pages![0]);
        }

        [Fact]
        public void HelpWindow_open_sends_create_packets_and_registers()
        {
            var (world, player, _) = WorldAndPlayer();
            var pages = new List<List<string>> { new() { "line1", "line2" }, new() { "line3" } };

            HelpWindow.Open(world.World, player, pages);

            var window = Assert.IsType<HelpWindow>(player.Windows[^1]);
            Assert.Equal(Window.WindowTypes.Help, window.Type);
            Assert.Equal(Window.WindowFrames.Quest, window.Frame);
            Assert.Equal("Command Help", window.Title);
            Assert.Contains(P.MakeWindow(window) + "\x1", player.Sent);
            Assert.Contains(P.WindowTextLine(window.ID, 1, "line1") + "\x1", player.Sent);
            Assert.Contains(P.WindowTextLine(window.ID, 2, "line2") + "\x1", player.Sent);
            Assert.Contains(P.EndWindow(window) + "\x1", player.Sent);
        }

        [Fact]
        public void HelpWindow_buttons_hide_back_on_first_and_next_on_last()
        {
            var (world, player, _) = WorldAndPlayer();
            var pages = new List<List<string>> { new() { "p0a" }, new() { "p1a" }, new() { "p2a" } };
            HelpWindow.Open(world.World, player, pages);
            var window = player.Windows[^1];

            Assert.Equal("0,1,0,1,0", window.Buttons);
            window.Clicked(Window.ButtonTypes.Next, 0, 0, 0, player, world.World);
            Assert.Equal("0,1,1,1,0", window.Buttons);
            window.Clicked(Window.ButtonTypes.Next, 0, 0, 0, player, world.World);
            Assert.Equal("0,1,1,0,0", window.Buttons);
        }

        [Fact]
        public void HelpWindow_navigation_resends_pages_and_close_removes()
        {
            var (world, player, _) = WorldAndPlayer();
            var pages = new List<List<string>> { new() { "p0a" }, new() { "p1a", "p1b" }, new() { "p2a" } };
            HelpWindow.Open(world.World, player, pages);
            var window = player.Windows[^1];

            window.Clicked(Window.ButtonTypes.Next, 0, 0, 0, player, world.World);
            Assert.Contains(P.MakeWindow(window) + "\x1", player.Sent);
            Assert.Contains(P.WindowTextLine(window.ID, 1, "p1a") + "\x1", player.Sent);
            Assert.Contains(P.WindowTextLine(window.ID, 2, "p1b") + "\x1", player.Sent);

            window.Clicked(Window.ButtonTypes.Back, 0, 0, 0, player, world.World);
            Assert.Contains(P.WindowTextLine(window.ID, 1, "p0a") + "\x1", player.Sent);

            window.Clicked(Window.ButtonTypes.Close, 0, 0, 0, player, world.World);
            Assert.DoesNotContain(window, player.Windows);
        }

        [Fact]
        public void HelpWindow_navigation_clamps_at_edges()
        {
            var (world, player, _) = WorldAndPlayer();
            var pages = new List<List<string>> { new() { "p0a" }, new() { "p1a" }, new() { "p2a" } };
            HelpWindow.Open(world.World, player, pages);
            var window = player.Windows[^1];

            window.Clicked(Window.ButtonTypes.Next, 0, 0, 0, player, world.World);
            window.Clicked(Window.ButtonTypes.Next, 0, 0, 0, player, world.World);
            var count = player.Sent.Count;
            window.Clicked(Window.ButtonTypes.Next, 0, 0, 0, player, world.World);
            Assert.Equal(count, player.Sent.Count);
            Assert.Equal("0,1,1,0,0", window.Buttons);

            window.Clicked(Window.ButtonTypes.Back, 0, 0, 0, player, world.World);
            window.Clicked(Window.ButtonTypes.Back, 0, 0, 0, player, world.World);
            count = player.Sent.Count;
            window.Clicked(Window.ButtonTypes.Back, 0, 0, 0, player, world.World);
            Assert.Equal(count, player.Sent.Count);
            Assert.Equal("0,1,0,1,0", window.Buttons);
        }

        [Fact]
        public void HelpWindow_text_lines_are_one_based_on_every_page()
        {
            var (world, player, _) = WorldAndPlayer();
            var pages = new List<List<string>> { new() { "a", "b", "c" }, new() { "d" } };
            HelpWindow.Open(world.World, player, pages);
            var window = player.Windows[^1];

            Assert.Contains($"WNF{window.ID},1,a|0|0|0|0|*\x1", player.Sent);
            Assert.Contains($"WNF{window.ID},2,b|0|0|0|0|*\x1", player.Sent);
            Assert.Contains($"WNF{window.ID},3,c|0|0|0|0|*\x1", player.Sent);

            window.Clicked(Window.ButtonTypes.Next, 0, 0, 0, player, world.World);
            var mkw = P.MakeWindow(window) + "\x1";
            var index = player.Sent.FindLastIndex(s => s == mkw);
            Assert.Equal(P.WindowTextLine(window.ID, 1, "d") + "\x1", player.Sent[index + 1]);
        }

        [Fact]
        public void HelpCommand_opens_window_and_silences_unknown_name()
        {
            var (world, player, _) = WorldAndPlayer();

            Assert.True(world.RunCommand(player, "/help"));
            Assert.Contains(player.Windows, w => w is HelpWindow);
            Assert.Contains(P.MakeWindow(player.Windows[^1]) + "\x1", player.Sent);

            var windowCount = player.Windows.Count;
            Assert.True(world.RunCommand(player, "/help nope"));
            Assert.Equal(windowCount, player.Windows.Count);
        }

        [Command("/warp2 ", Section = "Travel", Help = "Warp players around.")]
        public sealed class Warp2Command : BaseCommand
        {
            public void Execute(CommandContext ctx, string name) { }

            [Subcommand("here", Help = "Warp to a location.")]
            public void Here(CommandContext ctx, Player target) { }

            [Subcommand("all", AccessPrivilege.Ban, Help = "Warp everyone.")]
            public void All(CommandContext ctx) { }
        }
    }
}
