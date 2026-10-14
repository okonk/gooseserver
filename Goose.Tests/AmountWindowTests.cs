using Goose;
using Goose.Testing;
using Xunit;

namespace Goose.Tests;

public class AmountWindowTests
{
    private static (TestWorldFixture Fixture, TestWorldFixture.CapturingPlayer Player, AmountWindow Window, List<long> Confirmed) Setup(
        string prompt = "How many?", long min = 0, long max = 100, long def = 10)
    {
        var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(1, "Test");
        var player = fixture.CommandPlayerOn(map, 5, 5, "Tester");
        var confirmed = new List<long>();
        var window = new AmountWindow(player, fixture.World, "Donate", prompt,
            (1, 2, 255, 128, 0, 200), min, max, def, (amount, _, _) => confirmed.Add(amount));
        return (fixture, player, window, confirmed);
    }

    [Fact]
    public void Create_sends_mkw_with_frame_31_then_exact_ami()
    {
        var (fixture, player, window, _) = Setup();
        using (fixture)
        {
            Assert.Contains(window, player.Windows);
            Assert.Equal(Window.WindowFrames.Amount, window.Frame);
            Assert.Equal(Window.WindowTypes.Amount, window.Type);
            Assert.Equal("0,1,0,0,0", window.Buttons);

            var mkw = player.Sent.Single(s => s.StartsWith("MKW")).TrimEnd('\x1');
            Assert.Equal($"MKW{window.ID},31,Donate,0,1,0,0,0,0,0,0", mkw);

            var ami = player.Sent.Single(s => s.StartsWith("AMI")).TrimEnd('\x1');
            Assert.Equal($"AMI{window.ID}|1|2|255|128|0|200|0|100|10|How many?", ami);

            Assert.Equal($"ENW{window.ID}", player.Sent.Single(s => s.StartsWith("ENW")).TrimEnd('\x1'));
        }
    }

    [Fact]
    public void Create_prompt_with_pipe_round_trips_as_final_field()
    {
        var (fixture, player, window, _) = Setup(prompt: "Enter 1|2|3");
        using (fixture)
        {
            var ami = player.Sent.Single(s => s.StartsWith("AMI")).TrimEnd('\x1');
            Assert.Equal($"AMI{window.ID}|1|2|255|128|0|200|0|100|10|Enter 1|2|3", ami);

            var fields = ami.Split('|', 11);
            Assert.Equal(11, fields.Length);
            Assert.Equal("Enter 1|2|3", fields[^1]);
        }
    }

    [Fact]
    public void Submit_valid_amount_invokes_callback_once_and_closes()
    {
        var (fixture, player, window, confirmed) = Setup();
        using (fixture)
        {
            Assert.True(fixture.RunCommand(player, $"AMT{window.ID},42"));

            Assert.Equal([42L], confirmed);
            Assert.DoesNotContain(window, player.Windows);
            Assert.Equal($"CLW{window.ID}", player.Sent.Single(s => s.StartsWith("CLW")).TrimEnd('\x1'));
        }
    }

    [Theory]
    [InlineData("101")]
    [InlineData("-1")]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData("3.5")]
    public void Submit_out_of_range_negative_or_non_numeric_never_invokes_callback(string amount)
    {
        var (fixture, player, window, confirmed) = Setup();
        using (fixture)
        {
            fixture.RunCommand(player, $"AMT{window.ID},{amount}");

            Assert.Empty(confirmed);
            Assert.Contains(window, player.Windows);
            Assert.DoesNotContain(player.Sent, s => s.StartsWith("CLW"));
        }
    }

    [Fact]
    public void Submit_unknown_window_id_never_invokes_callback()
    {
        var (fixture, player, window, confirmed) = Setup();
        using (fixture)
        {
            fixture.RunCommand(player, $"AMT{window.ID + 999},42");

            Assert.Empty(confirmed);
            Assert.Contains(window, player.Windows);
            Assert.DoesNotContain(player.Sent, s => s.StartsWith("CLW"));
        }
    }

    [Fact]
    public void Submit_second_submit_after_close_invokes_callback_only_once()
    {
        var (fixture, player, window, confirmed) = Setup();
        using (fixture)
        {
            fixture.RunCommand(player, $"AMT{window.ID},42");
            fixture.RunCommand(player, $"AMT{window.ID},42");

            Assert.Equal([42L], confirmed);
            Assert.DoesNotContain(window, player.Windows);
            Assert.Equal(1, player.Sent.Count(s => s.StartsWith("CLW")));
        }
    }

    [Fact]
    public void Constructor_min_greater_than_max_throws()
    {
        var fixture = new TestWorldFixture();
        using (fixture)
        {
            var map = fixture.AddBaseMap(1, "Test");
            var player = fixture.CommandPlayerOn(map, 5, 5, "Tester");

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new AmountWindow(player, fixture.World, "Donate", "How many?",
                    (1, 2, 255, 128, 0, 200), 100, 0, 10, (_, _, _) => { }));

            Assert.Empty(player.Windows);
        }
    }

    [Fact]
    public void Submit_player_not_ready_never_invokes_callback()
    {
        var (fixture, player, window, confirmed) = Setup();
        using (fixture)
        {
            player.State = Player.States.NotLoggedIn;
            fixture.RunCommand(player, $"AMT{window.ID},42");

            Assert.Empty(confirmed);
            Assert.Contains(window, player.Windows);
            Assert.DoesNotContain(player.Sent, s => s.StartsWith("CLW"));
        }
    }
}
