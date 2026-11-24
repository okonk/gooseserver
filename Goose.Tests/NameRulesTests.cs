using Xunit;

namespace Goose.Tests;

public class NameRulesTests
{
    [Theory]
    [InlineData("Bob")]
    [InlineData("Abcdefghijklmnop")]
    public void IsValidPlayerName_validNames_accepted(string name)
    {
        Assert.True(NameRules.IsValidPlayerName(name));
    }

    [Theory]
    [InlineData("Bo")]
    [InlineData("abcdefghijklmnopq")]
    [InlineData("Bob1")]
    [InlineData("Bob Smith")]
    [InlineData("B\u00f3b")]
    public void IsValidPlayerName_invalidNames_rejected(string name)
    {
        Assert.False(NameRules.IsValidPlayerName(name));
    }

    [Theory]
    [InlineData("Rex")]
    [InlineData("Sir Fluff")]
    [InlineData("A B C")]
    [InlineData("Ab cd ef gh ij k")]
    public void IsValidPetName_validNames_accepted(string name)
    {
        Assert.True(NameRules.IsValidPetName(name));
    }

    [Theory]
    [InlineData("Re")]
    [InlineData("Ab cd ef gh ij kl")]
    [InlineData("Sir  Fluff")]
    [InlineData(" Rex")]
    [InlineData("Rex ")]
    [InlineData("Rex2")]
    [InlineData("Rex,Jr")]
    [InlineData("F\u00edf\u00ed")]
    [InlineData("")]
    public void IsValidPetName_invalidNames_rejected(string name)
    {
        Assert.False(NameRules.IsValidPetName(name));
    }
}
