using Goose;
using Goose.Testing;
using Xunit;

namespace Goose.Tests
{
    public class CustomWindowTests
    {
        private static (TestWorldFixture fixture, TestWorldFixture.CapturingPlayer player) WorldAndPlayer()
        {
            var fixture = new TestWorldFixture();
            var map = fixture.AddBaseMap(1, "Test");
            var player = fixture.CommandPlayerOn(map, 5, 5, "Tester");
            return (fixture, player);
        }

        [Fact]
        public void Constructor_registers_window_and_sends_mkwenw()
        {
            var (fixture, player) = WorldAndPlayer();
            using (fixture)
            {
                var window = new CustomWindow(player, fixture.World, 5);

                Assert.Contains(window, player.Windows);
                Assert.Equal(Window.WindowFrames.Custom, window.Frame);
                Assert.Equal(Window.WindowTypes.Custom, window.Type);
                Assert.Equal("0,1,0,0,1", window.Buttons);
                Assert.Equal("Custom", window.Title);
                Assert.Equal(5, window.TicketSlotId);

                var mkwIndex = player.Sent.FindIndex(s => s.StartsWith("MKW") && s.Contains(",28,Custom,0,1,0,0,1,"));
                Assert.True(mkwIndex >= 0);
                Assert.StartsWith("ENW" + window.ID, player.Sent[mkwIndex + 1]);
            }
        }

        [Fact]
        public void Clicked_close_removes_window_without_clw()
        {
            var (fixture, player) = WorldAndPlayer();
            using (fixture)
            {
                var window = new CustomWindow(player, fixture.World, 5);
                player.Sent.Clear();

                window.Clicked(Window.ButtonTypes.Close, 0, 0, 0, player, fixture.World);

                Assert.DoesNotContain(window, player.Windows);
                Assert.DoesNotContain(player.Sent, s => s.StartsWith("CLW"));
            }
        }

        [Fact]
        public void Clicked_exit_removes_window_without_clw()
        {
            var (fixture, player) = WorldAndPlayer();
            using (fixture)
            {
                var window = new CustomWindow(player, fixture.World, 5);
                player.Sent.Clear();

                window.Clicked(Window.ButtonTypes.Exit, 0, 0, 0, player, fixture.World);

                Assert.DoesNotContain(window, player.Windows);
                Assert.DoesNotContain(player.Sent, s => s.StartsWith("CLW"));
            }
        }

        [Fact]
        public void FindOpen_returns_open_window()
        {
            var (fixture, player) = WorldAndPlayer();
            using (fixture)
            {
                Assert.Null(CustomWindow.FindOpen(player));

                var window = new CustomWindow(player, fixture.World, 5);
                Assert.Same(window, CustomWindow.FindOpen(player));

                window.Clicked(Window.ButtonTypes.Close, 0, 0, 0, player, fixture.World);
                Assert.Null(CustomWindow.FindOpen(player));
            }
        }
    }
}
