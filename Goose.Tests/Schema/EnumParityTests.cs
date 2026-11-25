using CsvToSql;

namespace Goose.Tests.Schema;

public class EnumParityTests
{
    [Fact]
    public void Importer_effect_types_match_game_effect_types()
    {
        var importer = Enum.GetValues<SpellEffectsCsvToSql.EffectTypes>().ToDictionary(e => e.ToString(), e => (int)e);
        var game = Enum.GetValues<Goose.SpellEffect.EffectTypes>().ToDictionary(e => e.ToString(), e => (int)e);

        Assert.Equal(game, importer);
    }
}
