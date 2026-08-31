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
            Assert.Equal([new string('a', 53)], HelpFormatter.Wrap(new string('a', 53)));
        }

        [Fact]
        public void Wrap_breaks_at_last_word_boundary()
        {
            Assert.Equal(
                ["The quick brown fox jumps over the lazy dog and the", "hungry wolf"],
                HelpFormatter.Wrap("The quick brown fox jumps over the lazy dog and the hungry wolf"));
        }

        [Fact]
        public void Wrap_hard_breaks_overlong_word()
        {
            Assert.Equal(
                ["ab", new string('a', 53), new string('a', 5)],
                HelpFormatter.Wrap("ab " + new string('a', 58)));
            Assert.Equal(
                [new string('b', 53), new string('b', 7)],
                HelpFormatter.Wrap(new string('b', 60)));
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
            Assert.Equal(["Pub", "", "/open - An open command"], normalPages[1]);

            var gmPages = HelpFormatter.BuildPages(gm, registry, null);
            Assert.NotNull(gmPages);
            Assert.Equal(2, gmPages!.Count);
            Assert.Equal(["Pub (1)", "Priv (1)"], gmPages[0]);
            Assert.Equal(
                ["Pub", "", "/open - An open command", "", "Priv", "", "/secret - A restricted command"],
                gmPages[1]);
        }

        [Fact]
        public void BuildPages_section_page_lines_are_wrapped_with_indent()
        {
            var (_, player, _) = WorldAndPlayer();
            var registry = new CommandRegistry();
            Assert.True(registry.Register("/custom ", "Crafting", "Build a custom item from the provided recipe name.", OneString));

            var pages = HelpFormatter.BuildPages(player, registry, null);
            Assert.NotNull(pages);
            Assert.Equal(
                ["Crafting", "", "/custom <name> - Build a custom item from the", "  provided recipe name."],
                pages![1]);
        }

        [Fact]
        public void BuildPages_section_page_lists_subcommands_below_command()
        {
            var (_, normal, _) = WorldAndPlayer();
            var registry = new CommandRegistry();
            registry.SeedAttributedTypes([typeof(Warp2Command)]);

            var pages = HelpFormatter.BuildPages(normal, registry, "Travel");
            Assert.NotNull(pages);
            Assert.Single(pages!);
            Assert.Equal(
                [
                    "Travel",
                    "",
                    "/warp2 <name> - Warp players around.",
                    "/warp2 here <target> - Warp to a location.",
                ],
                pages[0]);
        }

        [Fact]
        public void BuildPages_closed_static_delegate_uses_unbound_parameter_names()
        {
            var (_, player, _) = WorldAndPlayer();
            var registry = new CommandRegistry();
            var capture = new ClosedCapture();
            var closed = (Action<CommandContext, int>)Delegate.CreateDelegate(
                typeof(Action<CommandContext, int>), capture,
                typeof(ClosedDelegateTargets).GetMethod("ClosedStatic")!);
            Assert.True(registry.Register("/closed ", "Test", "closed test", closed));

            var pages = HelpFormatter.BuildPages(player, registry, "closed");
            Assert.NotNull(pages);
            Assert.Equal(["closed test", "/closed <n>"], pages![0]);
        }

        [Fact]
        public void BuildPages_every_rendered_line_fits_53_chars()
        {
            var (_, player, _) = WorldAndPlayer();
            var registry = new CommandRegistry();
            var help = new string('a', 55) + " " + new string('b', 54);
            Assert.True(registry.Register("/long", "Big", help, NoArgs));

            var pages = HelpFormatter.BuildPages(player, registry, null);
            Assert.NotNull(pages);
            Assert.All(pages!.SelectMany(p => p), line => Assert.True(line.Length <= 53, line));
            Assert.Contains(pages.SelectMany(p => p), line => line == "  " + new string('b', 51));
        }

        [Fact]
        public void BuildPages_hard_break_then_full_word_continuation_fits_53()
        {
            var (_, player, _) = WorldAndPlayer();
            var registry = new CommandRegistry();
            var help = new string('a', 60) + " " + new string('b', 50);
            Assert.True(registry.Register("/lb", "Big", help, NoArgs));

            var pages = HelpFormatter.BuildPages(player, registry, "lb");
            Assert.NotNull(pages);
            Assert.All(pages!.SelectMany(p => p), line => Assert.True(line.Length <= 53, line));
            Assert.Equal(
                [
                    new string('a', 53),
                    "  " + new string('a', 7),
                    "  " + new string('b', 50),
                    "/lb",
                ],
                pages[0]);
        }

        [Fact]
        public void BuildPages_long_section_name_wraps_in_list()
        {
            var (_, player, _) = WorldAndPlayer();
            var registry = new CommandRegistry();
            var section = new string('S', 60);
            Assert.True(registry.Register("/longsec", section, "h", NoArgs));

            var pages = HelpFormatter.BuildPages(player, registry, null);
            Assert.NotNull(pages);
            Assert.Equal([new string('S', 53), new string('S', 7), "(1)"], pages![0]);
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
            Assert.True(registry.RegisterKeys(["/dup2 ", "/dup"], AccessPrivilege.Ban, "Box", "Restricted dup.", NoArgs));
            Assert.True(registry.Register("/dup ", "Box", "Open dup.", NoArgs));

            Assert.Null(HelpFormatter.BuildPages(player, registry, "dup2"));
            var pages = HelpFormatter.BuildPages(player, registry, "dup");
            Assert.NotNull(pages);
            Assert.Single(pages!);
            Assert.Equal(["Open dup.", "/dup"], pages[0]);
        }

        [Fact]
        public void BuildPages_alias_name_resolves_definition_with_primary_usage()
        {
            var (_, player, _) = WorldAndPlayer();
            var registry = new CommandRegistry();
            Assert.True(registry.RegisterKeys(["/alpha ", "/alp "], null, "Box", "Alias help.", NoArgs));

            var pages = HelpFormatter.BuildPages(player, registry, "alp");
            Assert.NotNull(pages);
            Assert.Equal(["Alias help.", "/alpha"], pages![0]);

            var slashPages = HelpFormatter.BuildPages(player, registry, "/ALP ");
            Assert.NotNull(slashPages);
            Assert.Equal(["Alias help.", "/alpha"], slashPages![0]);
        }

        [Fact]
        public void BuildPages_overlong_word_after_prefix_fits_with_indent()
        {
            var (_, player, _) = WorldAndPlayer();
            var registry = new CommandRegistry();
            var help = "ab " + new string('c', 60);
            Assert.True(registry.Register("/ow", "Big", help, NoArgs));

            var pages = HelpFormatter.BuildPages(player, registry, "ow");
            Assert.NotNull(pages);
            Assert.All(pages!.SelectMany(p => p), line => Assert.True(line.Length <= HelpFormatter.MaxLineLength, line));
            Assert.Equal(
                [
                    "ab",
                    "  " + new string('c', 51),
                    "  " + new string('c', 9),
                    "/ow",
                ],
                pages[0]);
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
        public void BuildPages_name_matches_command_and_section_shows_section_only()
        {
            var (_, player, _) = WorldAndPlayer();
            var registry = new CommandRegistry();
            Assert.True(registry.Register("/test ", "Misc", "A test command.", NoArgs));
            Assert.True(registry.Register("/other", "test", "Another command.", NoArgs));

            var pages = HelpFormatter.BuildPages(player, registry, "test");
            Assert.NotNull(pages);
            Assert.Single(pages!);
            Assert.Equal(["test", "", "/other - Another command."], pages[0]);
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
            var expected = new List<List<string>> { new() { "admin", "", "/other - A public command." } };
            Assert.Equal(expected, normalPages!);

            var gmPages = HelpFormatter.BuildPages(gm, registry, "admin");
            Assert.NotNull(gmPages);
            Assert.Single(gmPages!);
            Assert.Equal(["admin", "", "/other - A public command."], gmPages[0]);
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
                    "/warp2 <name>",
                    "/warp2 here <target> - Warp to a location.",
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
                    "/warp2 <name>",
                    "/warp2 here <target> - Warp to a location.",
                ],
                normalPages![0]);

            var gmPages = HelpFormatter.BuildPages(gm, registry, "warp2");
            Assert.NotNull(gmPages);
            Assert.Equal(
                [
                    "Warp players around.",
                    "/warp2 <name>",
                    "/warp2 here <target> - Warp to a location.",
                    "/warp2 all - Warp everyone.",
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
            Assert.Equal(20, pages[0].Count);
            Assert.Equal(20, pages[1].Count);
            Assert.Equal(2, pages[2].Count);

            var expected = new List<string> { "Big", "" };
            expected.AddRange(Enumerable.Range(1, 40).Select(i => $"/c{i:D2} - h{i}"));
            Assert.Equal(expected, pages.SelectMany(p => p).ToList());
        }

        [Fact]
        public void BuildPages_wrapped_command_that_does_not_fit_moves_to_next_page()
        {
            var (_, player, _) = WorldAndPlayer();
            var registry = new CommandRegistry();
            for (var i = 1; i <= 19; i++)
                Assert.True(registry.Register($"/f{i:D2}", "Big", $"h{i}", NoArgs));
            Assert.True(registry.Register("/wrap", "Big", "aaaaaaaaaa bbbbbbbbbb cccccccccc dddddddddd eeeee", NoArgs));

            var pages = HelpFormatter.BuildPages(player, registry, "Big");
            Assert.NotNull(pages);
            Assert.Equal(2, pages!.Count);
            Assert.Equal(20, pages[0].Count);
            Assert.Equal(
                [
                    "/f19 - h19",
                    "/wrap - aaaaaaaaaa bbbbbbbbbb cccccccccc dddddddddd",
                    "  eeeee",
                ],
                pages[1]);
        }

        [Fact]
        public void BuildPages_sections_fitting_exactly_share_page()
        {
            var (_, player, _) = WorldAndPlayer();
            var registry = new CommandRegistry();
            Assert.True(registry.Register("/a1", "First", "h1", NoArgs));
            for (var i = 1; i <= 14; i++)
                Assert.True(registry.Register($"/b{i:D2}", "Second", $"h{i}", NoArgs));

            var pages = HelpFormatter.BuildPages(player, registry, null);
            Assert.NotNull(pages);
            Assert.Equal(2, pages!.Count);
            Assert.Equal(20, pages[1].Count);
            Assert.Equal("First", pages[1][0]);
            Assert.Equal("", pages[1][3]);
            Assert.Equal("Second", pages[1][4]);
        }

        [Fact]
        public void BuildPages_section_that_does_not_fit_moves_to_next_page()
        {
            var (_, player, _) = WorldAndPlayer();
            var registry = new CommandRegistry();
            Assert.True(registry.Register("/a1", "First", "h1", NoArgs));
            for (var i = 1; i <= 16; i++)
                Assert.True(registry.Register($"/b{i:D2}", "Second", $"h{i}", NoArgs));

            var pages = HelpFormatter.BuildPages(player, registry, null);
            Assert.NotNull(pages);
            Assert.Equal(3, pages!.Count);
            Assert.Equal(["First", "", "/a1 - h1"], pages[1]);
            Assert.Equal("Second", pages[2][0]);
            Assert.Equal(18, pages[2].Count);
        }

        [Fact]
        public void BuildPages_block_taller_than_page_is_hard_split()
        {
            var (_, player, _) = WorldAndPlayer();
            var registry = new CommandRegistry();
            Assert.True(registry.Register("/big", "Solo", new string('a', 1100), NoArgs));

            var pages = HelpFormatter.BuildPages(player, registry, "big");
            Assert.NotNull(pages);
            Assert.Equal(2, pages!.Count);
            Assert.Equal(20, pages[0].Count);
            Assert.Equal(3, pages[1].Count);
            Assert.Equal("/big", pages[1][2]);
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
