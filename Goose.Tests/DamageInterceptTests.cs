using Goose.Testing;

namespace Goose.Tests;

public class DamageInterceptTests
{
    private const string BaseBody = """
        using Goose;
        using Goose.Scripting;

        public class T : BaseSpellEffectScript
        {
        }

        return typeof(T);
        """;

    private const string DoubleBody = """
        using Goose;
        using Goose.Scripting;

        public class T : BaseSpellEffectScript
        {
            public override long InterceptDamage(Buff buff, ICharacter attacker, long rawDamage, long currentDamage, GameWorld world)
            {
                return currentDamage * 2;
            }
        }

        return typeof(T);
        """;

    private const string PlusTenBody = """
        using Goose;
        using Goose.Scripting;

        public class T : BaseSpellEffectScript
        {
            public override long InterceptDamage(Buff buff, ICharacter attacker, long rawDamage, long currentDamage, GameWorld world)
            {
                return currentDamage + 10;
            }
        }

        return typeof(T);
        """;

    private const string RawTimes10PlusCurrentBody = """
        using Goose;
        using Goose.Scripting;

        public class T : BaseSpellEffectScript
        {
            public override long InterceptDamage(Buff buff, ICharacter attacker, long rawDamage, long currentDamage, GameWorld world)
            {
                return rawDamage * 10 + currentDamage;
            }
        }

        return typeof(T);
        """;

    private const string ThrowBody = """
        using Goose;
        using Goose.Scripting;

        public class T : BaseSpellEffectScript
        {
            public override long InterceptDamage(Buff buff, ICharacter attacker, long rawDamage, long currentDamage, GameWorld world)
            {
                throw new Exception("boom");
            }
        }

        return typeof(T);
        """;

    [Fact]
    public void Apply_NoBuffs_ReturnsDamageUnchanged()
    {
        using var fixture = new TestWorldFixture();
        var target = CreateTarget(fixture);

        Assert.Equal(50, DamageIntercept.Apply(target, target, 50, fixture.World));
    }

    [Fact]
    public void Apply_BuffWithoutScript_ReturnsDamageUnchanged()
    {
        using var fixture = new TestWorldFixture();
        var target = CreateTarget(fixture);
        AddBuff(target, fixture, "noscript", e => { });

        Assert.Equal(50, DamageIntercept.Apply(target, target, 50, fixture.World));
    }

    [Fact]
    public void Apply_BaseScript_PassesDamageThrough()
    {
        using var fixture = new TestWorldFixture();
        var target = CreateTarget(fixture);
        AddBuff(target, fixture, "base", e => e.Script = fixture.CompileSpellEffectScript(BaseBody, "Base.csx"));

        Assert.Equal(50, DamageIntercept.Apply(target, target, 50, fixture.World));
    }

    [Fact]
    public void Apply_ModifyingScript_ReturnsScriptValue()
    {
        using var fixture = new TestWorldFixture();
        var target = CreateTarget(fixture);
        AddBuff(target, fixture, "double", e => e.Script = fixture.CompileSpellEffectScript(DoubleBody, "Double.csx"));

        Assert.Equal(100, DamageIntercept.Apply(target, target, 50, fixture.World));
    }

    [Fact]
    public void Apply_TwoScripts_SecondSeesFirstOutputAndOriginalRaw()
    {
        using var fixture = new TestWorldFixture();
        var target = CreateTarget(fixture);
        AddBuff(target, fixture, "plus10", e => e.Script = fixture.CompileSpellEffectScript(PlusTenBody, "PlusTen.csx"));
        AddBuff(target, fixture, "raw10pluscurrent", e => e.Script = fixture.CompileSpellEffectScript(RawTimes10PlusCurrentBody, "Raw10.csx"));

        Assert.Equal(1110, DamageIntercept.Apply(target, target, 100, fixture.World));
    }

    [Fact]
    public void Apply_ThrowingScript_SkippedAndLaterScriptsStillRun()
    {
        using var fixture = new TestWorldFixture();
        var target = CreateTarget(fixture);
        AddBuff(target, fixture, "boom", e => e.Script = fixture.CompileSpellEffectScript(ThrowBody, "Throw.csx"));
        AddBuff(target, fixture, "double", e => e.Script = fixture.CompileSpellEffectScript(DoubleBody, "Double2.csx"));

        Assert.Equal(100, DamageIntercept.Apply(target, target, 50, fixture.World));
    }

    [Fact]
    public void Apply_Heal_PassesThroughUnmodifiedByDefault()
    {
        using var fixture = new TestWorldFixture();
        var target = CreateTarget(fixture);
        AddBuff(target, fixture, "base", e => e.Script = fixture.CompileSpellEffectScript(BaseBody, "Base2.csx"));

        Assert.Equal(-50, DamageIntercept.Apply(target, target, -50, fixture.World));
    }

    private static Player CreateTarget(TestWorldFixture fixture)
    {
        var map = fixture.AddBaseMap(1, "m");
        return fixture.CommandPlayerOn(map, 1, 1);
    }

    private static void AddBuff(Player target, TestWorldFixture fixture, string fileName, Action<SpellEffect> configure)
    {
        var effect = fixture.AddBaseSpellEffect(1, fileName, e =>
        {
            e.EffectType = SpellEffect.EffectTypes.Buff;
            configure(e);
        });
        target.Buffs.Add(new Buff { Caster = target, Target = target, SpellEffect = effect });
    }
}
