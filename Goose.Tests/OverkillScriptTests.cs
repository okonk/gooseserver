using Goose.Scripting;
using Goose.Testing;

namespace Goose.Tests;

public class OverkillScriptTests
{
    [Theory]
    [InlineData("", 750)]
    [InlineData("cap=0.6", 600)]
    [InlineData("cap=0.9", 750)]
    public void Refund_is_the_wasted_fraction_limited_by_the_cap(string scriptParams, long expectedMp)
    {
        using var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(1, "Test");
        var player = fixture.CommandPlayerOn(map, 1, 1);
        player.BaseStats = new AttributeSet { HP = 1000, MP = 1000 };
        player.MaxStats = player.BaseStats + new AttributeSet();
        player.CurrentHP = 1000;
        player.CurrentMP = 0;
        var effect = OverkillEffect(fixture, scriptParams);
        var cast = new SpellCast(new Spell { ID = 1, Name = "Test" }, player)
        {
            MPPercentCostPaid = 1000,
            TotalDamage = 4000,
            UsefulDamage = 1000,
        };

        effect.Script.Object.OnCastCompleted(effect, cast, fixture.World);

        Assert.Equal(expectedMp, player.CurrentMP);
    }

    [Fact]
    public void Description_shows_the_cap()
    {
        using var fixture = new TestWorldFixture();
        var effect = OverkillEffect(fixture, "cap=0.6");

        Assert.Contains("Overkill refunds unused HP/MP cost (up to 60%)", effect.GetItemDescription(fixture.World));
    }

    private static SpellEffect OverkillEffect(TestWorldFixture fixture, string scriptParams)
    {
        var scriptBody = File.ReadAllText(Path.Combine(FindIllutiaDataDirectory(), "Scripts", "Spell", "Overkill.csx"));
        return new SpellEffect
        {
            ID = 134,
            Name = "Arcane Blast",
            EffectType = SpellEffect.EffectTypes.Script,
            HPFormula = "-1.9 * %ccmp",
            MPFormula = "0",
            ScriptParams = scriptParams,
            Script = fixture.CompileSpellEffectScript(scriptBody, "Overkill.csx"),
        };
    }

    private static string FindIllutiaDataDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            foreach (var candidate in new[]
            {
                Path.Combine(directory.FullName, "Data", "Illutia"),
                Path.Combine(directory.FullName, "Goose", "Data", "Illutia"),
            })
            {
                if (Directory.Exists(candidate)) return candidate;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate Data/Illutia from " + AppContext.BaseDirectory);
    }
}
