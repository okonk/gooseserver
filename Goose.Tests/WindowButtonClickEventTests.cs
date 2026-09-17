using Goose;
using Goose.Events;
using Goose.Tests.Fixtures;
using Xunit;

namespace Goose.Tests;

public class WindowButtonClickEventTests
{
    private sealed class RecordingWindow : Window
    {
        public List<(int Button, int Line)> Calls { get; } = new();
        public override void Clicked(ButtonTypes buttonid, int npcid, int id2, int id3, Player player, GameWorld world)
            => Calls.Add(((int)buttonid, -1));
        public override void LineClicked(int line, int npcid, Player player, GameWorld world)
            => Calls.Add((-1, line));
    }

    private static (VendorFixture Fixture, RecordingWindow Window) Setup()
    {
        var fixture = new VendorFixture();
        var window = new RecordingWindow { ID = 55 };
        fixture.Player.Windows = new List<Window> { window };
        return (fixture, window);
    }

    [Fact]
    public void LineClickOffset_DoesNotOverlapButtonTypes()
        => Assert.True(Window.LineClickOffset >= Enum.GetValues(typeof(Window.ButtonTypes)).Length);

    [Fact]
    public void LineClick_DispatchesLineClickedWithOffsetStripped()
    {
        var (fixture, window) = Setup();
        // WBC button ids 20..29 are option-list line clicks (line = id - 20).
        new WindowButtonClickEvent { Player = fixture.Player, Data = "WBC22,55,0,0,0" }.Ready(fixture.World);
        Assert.Single(window.Calls);
        Assert.Equal((-1, 2), window.Calls[0]);
    }

    [Fact]
    public void LineClick_WrongWindowId_IsIgnored()
    {
        var (fixture, window) = Setup();
        new WindowButtonClickEvent { Player = fixture.Player, Data = "WBC22,56,0,0,0" }.Ready(fixture.World);
        Assert.Empty(window.Calls);
    }

    [Fact]
    public void LineClick_OutOfRangeLine_IsIgnored()
    {
        var (fixture, window) = Setup();
        new WindowButtonClickEvent { Player = fixture.Player, Data = "WBC30,55,0,0,0" }.Ready(fixture.World);
        new WindowButtonClickEvent { Player = fixture.Player, Data = "WBC19,55,0,0,0" }.Ready(fixture.World);
        Assert.Empty(window.Calls);
    }

    [Fact]
    public void RegularButton_StillDispatchesClicked()
    {
        var (fixture, window) = Setup();
        new WindowButtonClickEvent { Player = fixture.Player, Data = "WBC2,55,0,0,0" }.Ready(fixture.World);
        Assert.Single(window.Calls);
        Assert.Equal((2, -1), window.Calls[0]);
    }

    [Fact]
    public void UnknownButton_IsIgnored()
    {
        var (fixture, window) = Setup();
        new WindowButtonClickEvent { Player = fixture.Player, Data = "WBC7,55,0,0,0" }.Ready(fixture.World);
        Assert.Empty(window.Calls);
    }
}
