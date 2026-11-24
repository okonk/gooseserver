using Goose;
using Goose.Testing;
using Xunit;

namespace Goose.Tests;

public class TextInputWindowTests
{
    private static (TestWorldFixture Fixture, TestWorldFixture.CapturingPlayer Player, TextInputWindow Window, List<string> Confirmed) Setup(
        string prompt = "Enter a name:", string buttonText = "OK", string defaultText = "", int maxLength = 20)
    {
        var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(1, "Test");
        var player = fixture.CommandPlayerOn(map, 5, 5, "Tester");
        var confirmed = new List<string>();
        var window = new TextInputWindow(player, fixture.World, "Rename", prompt, buttonText, defaultText, maxLength,
            (text, _, _) => confirmed.Add(text));
        return (fixture, player, window, confirmed);
    }

    [Fact]
    public void Create_sends_mkw_with_frame_35_then_txi_fields_in_order()
    {
        var (fixture, player, window, _) = Setup();
        using (fixture)
        {
            Assert.Contains(window, player.Windows);
            Assert.Equal(Window.WindowFrames.TextInput, window.Frame);
            Assert.Equal(Window.WindowTypes.TextInput, window.Type);
            Assert.Equal("0,1,0,0,0", window.Buttons);

            var mkw = player.Sent.Single(s => s.StartsWith("MKW")).TrimEnd('\x1');
            Assert.Equal($"MKW{window.ID},35,Rename,0,1,0,0,0,0,0,0", mkw);

            var txi = player.Sent.Single(s => s.StartsWith("TXI")).TrimEnd('\x1');
            Assert.Equal($"TXI{window.ID}|20|OK||Enter a name:", txi);

            Assert.Equal($"ENW{window.ID}", player.Sent.Single(s => s.StartsWith("ENW")).TrimEnd('\x1'));
        }
    }

    [Fact]
    public void Create_strips_pipes_from_button_and_default_text_but_keeps_prompt_pipes()
    {
        var (fixture, player, window, _) = Setup(prompt: "Enter 1|2|3", buttonText: "O|K", defaultText: "a|b");
        using (fixture)
        {
            var txi = player.Sent.Single(s => s.StartsWith("TXI")).TrimEnd('\x1');
            Assert.Equal($"TXI{window.ID}|20|OK|ab|Enter 1|2|3", txi);

            var fields = txi.Split('|', 5);
            Assert.Equal(5, fields.Length);
            Assert.Equal("Enter 1|2|3", fields[^1]);
        }
    }

    [Fact]
    public void Constructor_max_length_below_one_throws()
    {
        var fixture = new TestWorldFixture();
        using (fixture)
        {
            var map = fixture.AddBaseMap(1, "Test");
            var player = fixture.CommandPlayerOn(map, 5, 5, "Tester");

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new TextInputWindow(player, fixture.World, "Rename", "Enter a name:", "OK", "", 0, (_, _, _) => { }));

            Assert.Empty(player.Windows);
        }
    }

    [Fact]
    public void Constructor_empty_prompt_throws()
    {
        var fixture = new TestWorldFixture();
        using (fixture)
        {
            var map = fixture.AddBaseMap(1, "Test");
            var player = fixture.CommandPlayerOn(map, 5, 5, "Tester");

            Assert.Throws<ArgumentException>(() =>
                new TextInputWindow(player, fixture.World, "Rename", "", "OK", "", 20, (_, _, _) => { }));
            Assert.Throws<ArgumentException>(() =>
                new TextInputWindow(player, fixture.World, "Rename", null!, "OK", "", 20, (_, _, _) => { }));

            Assert.Empty(player.Windows);
            Assert.Equal(1000, player.LastWindowID);
        }
    }

    [Fact]
    public void Submit_trims_and_strips_control_chars_before_callback()
    {
        var (fixture, player, window, confirmed) = Setup();
        using (fixture)
        {
            window.Submit("\x01  ho\x1bse\r\n \x00 ", player, fixture.World);

            Assert.Equal(["hose"], confirmed);
        }
    }

    [Fact]
    public void Submit_empty_after_trim_never_invokes_callback()
    {
        var (fixture, player, window, confirmed) = Setup();
        using (fixture)
        {
            window.Submit("   ", player, fixture.World);
            window.Submit("\x00\x01", player, fixture.World);

            Assert.Empty(confirmed);
            Assert.Contains(window, player.Windows);
        }
    }

    [Theory]
    [InlineData("123456789012345678901")]
    [InlineData("1234567890123456789012345")]
    public void Submit_longer_than_max_length_never_invokes_callback(string text)
    {
        var (fixture, player, window, confirmed) = Setup(maxLength: 20);
        using (fixture)
        {
            window.Submit(text, player, fixture.World);

            Assert.Empty(confirmed);
            Assert.Contains(window, player.Windows);
        }
    }

    [Fact]
    public void Submit_at_max_length_invokes_callback()
    {
        var (fixture, player, window, confirmed) = Setup(maxLength: 20);
        using (fixture)
        {
            window.Submit("12345678901234567890", player, fixture.World);

            Assert.Equal(["12345678901234567890"], confirmed);
        }
    }

    [Fact]
    public void Submit_does_not_close_window_when_on_confirm_does_not_close()
    {
        var (fixture, player, window, confirmed) = Setup();
        using (fixture)
        {
            window.Submit("hello", player, fixture.World);

            Assert.Equal(["hello"], confirmed);
            Assert.Contains(window, player.Windows);
            Assert.DoesNotContain(player.Sent, s => s.StartsWith("CLW"));
        }
    }

    [Fact]
    public void Clicked_exit_or_close_removes_window()
    {
        var (fixture, player, window, _) = Setup();
        using (fixture)
        {
            window.Clicked(Window.ButtonTypes.Exit, 0, 0, 0, player, fixture.World);

            Assert.DoesNotContain(window, player.Windows);
        }

        var (fixture2, player2, window2, _) = Setup();
        using (fixture2)
        {
            window2.Clicked(Window.ButtonTypes.Close, 0, 0, 0, player2, fixture2.World);

            Assert.DoesNotContain(window2, player2.Windows);
        }
    }
}
