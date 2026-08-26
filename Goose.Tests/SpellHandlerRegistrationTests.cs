namespace Goose.Tests;

public class SpellHandlerRegistrationTests
{
    [Fact]
    public void Registered_effects_are_retrievable_and_enumerable()
    {
        var handler = new SpellHandler();
        var effect = new SpellEffect { ID = 100042, Name = "Powerful Firestorm" };

        handler.AddSpellEffect(effect);

        Assert.Same(effect, handler.GetSpellEffect(100042));
        Assert.Contains(effect, handler.GetSpellEffects());
        Assert.Equal(1, handler.EffectCount);
    }

    [Fact]
    public void Registered_spells_are_retrievable_and_enumerable()
    {
        var handler = new SpellHandler();
        var spell = new Spell { ID = 100091, Name = "Powerful Bless" };

        handler.AddSpell(spell);

        Assert.Same(spell, handler.GetSpell(100091));
        Assert.Contains(spell, handler.GetSpells());
        Assert.Equal(1, handler.Count);
    }

    /// <summary>Overwriting is deliberate and matches NPCHandler.AddTemplate. The dimension
    /// script preflights for collisions itself rather than relying on the handler to refuse.</summary>
    [Fact]
    public void Registering_the_same_id_twice_overwrites()
    {
        var handler = new SpellHandler();
        handler.AddSpell(new Spell { ID = 5, Name = "First" });
        handler.AddSpell(new Spell { ID = 5, Name = "Second" });

        Assert.Equal("Second", handler.GetSpell(5)!.Name);
        Assert.Equal(1, handler.Count);
    }
}
