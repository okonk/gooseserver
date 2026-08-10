using System.Reflection;
using System.Runtime.CompilerServices;
using Goose.Scripting;

namespace Goose.Tests;

public class SpellCloneTests
{
    /// <summary>Fails the day someone adds a property to Spell without adding it to the copy
    /// constructor - which is the entire reason that constructor is hand-written.</summary>
    [Fact]
    public void Spell_copy_carries_every_property()
    {
        var original = new Spell();
        var expected = FillEveryProperty(original);

        var copy = new Spell(original);

        AssertEveryPropertyCarried(expected, copy);
        Assert.Equal(7, original.ID);          // and the source is untouched
    }

    /// <summary>Same guard for SpellEffect, which has ~50 properties and is the one that will
    /// actually grow.</summary>
    [Fact]
    public void SpellEffect_copy_carries_every_property()
    {
        var original = new SpellEffect();
        var expected = FillEveryProperty(original);

        var copy = new SpellEffect(original);

        AssertEveryPropertyCarried(expected, copy);
    }

    /// <summary>Task 5 rewires each clone's stacking lists independently. Sharing the list
    /// instance would make every dimension's rewiring overwrite the last.</summary>
    [Fact]
    public void SpellEffect_copy_detaches_the_stacking_lists()
    {
        var other = new SpellEffect { ID = 1 };
        var original = new SpellEffect { ID = 42 };
        original.BuffStacksOver.Add(other);
        original.BuffDoesntStackOver.Add(other);

        var copy = new SpellEffect(original) { ID = 400042 };
        copy.BuffStacksOver.Clear();
        copy.BuffDoesntStackOver.Add(new SpellEffect { ID = 2 });

        Assert.Single(original.BuffStacksOver);
        Assert.Same(other, original.BuffStacksOver[0]);
        Assert.Single(original.BuffDoesntStackOver);
        Assert.Equal(2, copy.BuffDoesntStackOver.Count);
    }

    /// <summary>Task 4 scales the clone's stats in place. A shared AttributeSet would scale
    /// the base effect too, and every dimension would compound on the last.</summary>
    [Fact]
    public void SpellEffect_copy_gets_its_own_AttributeSet()
    {
        var original = new SpellEffect { ID = 42 };
        original.Stats.HP = 100;
        original.Stats.MoveSpeed = 5;

        var copy = new SpellEffect(original) { ID = 400042 };
        copy.Stats.HP = 999;

        Assert.NotSame(original.Stats, copy.Stats);
        Assert.Equal(100, original.Stats.HP);
        Assert.Equal(5, copy.Stats.MoveSpeed);
    }

    /// <summary>Compiled scripts are cached per path and stateless, so sharing the reference
    /// is correct - and it is what lets Task 7 assign one script to all teleport effects.</summary>
    [Fact]
    public void SpellEffect_copy_shares_the_script_reference()
    {
        // Script<T>'s only constructor compiles a file off disk (Script.cs:20), so build the
        // instance without running it - this test is about reference identity, nothing else.
        var script = (Script<ISpellEffectScript>)RuntimeHelpers
            .GetUninitializedObject(typeof(Script<ISpellEffectScript>));
        var original = new SpellEffect { ID = 42, Script = script, ScriptParams = "x" };

        var copy = new SpellEffect(original);

        Assert.Same(script, copy.Script);   // shared on purpose: compiled scripts are cached
        Assert.Equal("x", copy.ScriptParams);
    }

    // ---- The reflection walk -------------------------------------------------------

    private const int Marker = 4242;

    private static readonly Dictionary<Type, object> SampleValues = new()
    {
        [typeof(int)] = 7,
        [typeof(long)] = 7L,
        [typeof(decimal)] = 1.5m,
        [typeof(bool)] = true,
        [typeof(string)] = "sample",
    };

    /// <summary>Sets every public property to a distinct non-default value and returns what
    /// it set, keyed by name. Non-default matters: if a property were left at zero on both
    /// sides, "the copy matches" would prove nothing.
    ///
    /// An unrecognised property type fails the test rather than being skipped. That is the
    /// point - it forces whoever adds the property to decide whether the copy shares the
    /// instance or clones it, instead of the guard quietly ignoring it.</summary>
    private static Dictionary<string, object> FillEveryProperty(object target)
    {
        var assigned = new Dictionary<string, object>();

        foreach (var property in target.GetType()
                     .GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            Assert.True(property.CanWrite,
                $"{property.Name} is read-only. Decide what the copy constructor does with it, then teach this test.");

            var type = property.PropertyType;
            object value;

            if (SampleValues.TryGetValue(type, out var sample)) value = sample;
            else if (type.IsEnum) value = Enum.GetValues(type).Cast<object>().Last();
            else if (type == typeof(SpellEffect)) value = new SpellEffect { ID = Marker };
            else if (type == typeof(AttributeSet)) value = new AttributeSet { HP = Marker };
            else if (type == typeof(List<SpellEffect>))
                value = new List<SpellEffect> { new SpellEffect { ID = Marker } };
            else if (type == typeof(Script<ISpellEffectScript>))
                value = RuntimeHelpers.GetUninitializedObject(type);
            else throw new Xunit.Sdk.XunitException(
                $"{property.Name} is a {type.Name}, which this guard cannot fill. Add it to the "
                + "table above, and say in the copy constructor whether it is shared or cloned.");

            property.SetValue(target, value);
            assigned[property.Name] = value;
        }

        return assigned;
    }

    /// <summary>Every property arrived. The two members the copy constructor deliberately
    /// clones are compared by content - their instance identity is what the three tests above
    /// cover, and asserting it here would just duplicate them.</summary>
    private static void AssertEveryPropertyCarried(Dictionary<string, object> expected, object copy)
    {
        foreach (var property in copy.GetType()
                     .GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var want = expected[property.Name];
            var actual = property.GetValue(copy);

            switch (actual)
            {
                case AttributeSet stats:
                    Assert.True(((AttributeSet)want).HP == stats.HP,
                        $"{property.Name}: expected HP {((AttributeSet)want).HP}, got {stats.HP}");
                    break;
                case List<SpellEffect> list:
                    Assert.True(((List<SpellEffect>)want).Select(e => e.ID).SequenceEqual(list.Select(e => e.ID)),
                        $"{property.Name}: expected IDs [" + string.Join(",", ((List<SpellEffect>)want).Select(e => e.ID)) + "], got [" + string.Join(",", list.Select(e => e.ID)) + "]");
                    break;
                default:
                    // Reference equality for SpellEffect and Script<T>; value equality for the rest.
                    Assert.True(Equals(want, actual),
                        $"{property.Name}: expected {want}, got {actual}");
                    break;
            }
        }
    }
}
