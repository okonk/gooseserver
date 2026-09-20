using Goose;
using Xunit;

namespace Goose.Tests;

public class WindowLinePacketTests
{
    [Fact]
    public void WindowLine_NoTintUsesStarShortcut()
    {
        Assert.Equal("WNF1001,1,Cloth|0|0|0|0|*",
            P.WindowLine(1001, 1, "Cloth", 0, 0, 0, 0, 0, 0));
    }

    [Fact]
    public void WindowLine_CarriesSheetGraphicAndTint()
    {
        Assert.Equal("WNF1001,2,Do it|0|0|5|7|255|128|0|255",
            P.WindowLine(1001, 2, "Do it", 5, 7, 255, 128, 0, 255));
    }

    [Fact]
    public void WindowLine_SheetGraphicWithoutTint()
    {
        Assert.Equal("WNF1001,1,Cloth|0|0|5|7|*",
            P.WindowLine(1001, 1, "Cloth", 5, 7, 0, 0, 0, 0));
    }

    [Fact]
    public void OpeningLine_SendsMessageAfterFirstComma()
    {
        Assert.Equal("WNL1002,Welcome to my shop",
            P.OpeningLine(1002, "Welcome to my shop"));
    }
}
