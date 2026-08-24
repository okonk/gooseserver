using Goose.Scripting;
using Goose.Testing;
using Goose.Tests.Collections;
using Goose.Tests.Fixtures;

namespace Goose.Tests;

[Collection(GameWorldSettingsCollection.Name)]
public class SpellEffectScriptDescriptionTests
{
    private const string DescribingScript = @"
using System.Collections.Generic;
using Goose;
using Goose.Scripting;

public class Describer : BaseSpellEffectScript
{
    public override IEnumerable<string> GetItemDescription(SpellEffect thisEffect, GameWorld world)
    {
        return new[] { ""Scripted line one"", ""Scripted line two"" };
    }
}

return typeof(Describer);
";

    private const string SilentScript = @"
using System.Collections.Generic;
using Goose;
using Goose.Scripting;

public class Silent : BaseSpellEffectScript { }

return typeof(Silent);
";

    [Fact]
    public void A_script_supplying_lines_replaces_the_built_in_description()
    {
        using var fixture = new TestWorldFixture();
        var effect = new SpellEffect
        {
            ID = 1, EffectType = SpellEffect.EffectTypes.Script,
            Script = fixture.CompileSpellEffectScript(DescribingScript, "Describer.csx"),
        };

        Assert.Equal(new[] { "Scripted line one", "Scripted line two" },
                     effect.GetItemDescription(fixture.World).ToArray());
    }

    /// <summary>Returning null must fall through, so every existing spell-effect script is
    /// unaffected by this change.</summary>
    [Fact]
    public void A_script_returning_null_falls_through_to_the_built_in_switch()
    {
        using var fixture = new TestWorldFixture();
        var effect = new SpellEffect
        {
            ID = 1, EffectType = SpellEffect.EffectTypes.Stun,
            Script = fixture.CompileSpellEffectScript(SilentScript, "Silent.csx"),
        };

        Assert.Equal(new[] { "Stun" }, effect.GetItemDescription(fixture.World).ToArray());
    }

    [Fact]
    public void An_effect_with_no_script_uses_the_built_in_switch()
    {
        using var fixture = new TestWorldFixture();
        var effect = new SpellEffect { ID = 1, EffectType = SpellEffect.EffectTypes.Root };

        Assert.Equal(new[] { "Root" }, effect.GetItemDescription(fixture.World).ToArray());
    }
}
