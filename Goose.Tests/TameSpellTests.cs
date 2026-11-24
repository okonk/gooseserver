using Goose.Testing;

namespace Goose.Tests;

public sealed class TameSpellTests
{
    private static Pet MakePet(TestWorldFixture fixture, Player owner, int id, string name)
    {
        var pet = new Pet
        {
            PetID = id,
            Name = name,
            Owner = owner,
            Level = 1,
            BaseStats = new AttributeSet(),
            MaxStats = new AttributeSet(),
            Class = fixture.World.ClassHandler.GetClass(0)!,
        };
        owner.Pets.Add(pet);
        return pet;
    }

    [Fact]
    public void Untameable_npc_refuses_with_a_notice_and_no_pet()
    {
        using var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(1, "Test");
        var player = fixture.CommandPlayerOn(map, 1, 1);
        var template = fixture.AddNpcTemplate(1, "Arena Ghast", t => t.MoveSpeed = 1);
        var npc = fixture.SpawnNpc(map, 2, 1, template, properties: new PropertiesDictionary { ["untameable"] = true });
        player.Sent.Clear();

        Assert.False(new SpellEffect().CastTameSpell(player, npc, fixture.World));

        Assert.Empty(player.Pets);
        Assert.Contains(player.Sent, p => p.Contains("Arena Ghast can't be tamed."));
    }

    [Fact]
    public void Tame_refused_at_max_pets_with_slot_bonus()
    {
        using var fixture = new TestWorldFixture(s => s.PetCountLimit = 1);
        var map = fixture.AddBaseMap(1, "Test");
        var player = fixture.CommandPlayerOn(map, 1, 1);
        player.PetSlotsBonus = 1;
        MakePet(fixture, player, 1, "Rex");
        MakePet(fixture, player, 2, "Fido");
        var template = fixture.AddNpcTemplate(1, "Arena Ghast", t => t.MoveSpeed = 1);
        var npc = fixture.SpawnNpc(map, 2, 1, template);
        player.Sent.Clear();

        Assert.False(new SpellEffect().CastTameSpell(player, npc, fixture.World));

        Assert.Equal(2, player.Pets.Count);
        Assert.Contains(player.Sent, p => p.Contains("You have reached the maximum amount of pets. Use /pets to release one."));
    }

    [Fact]
    public void Tame_refused_when_pet_count_above_max_pets()
    {
        using var fixture = new TestWorldFixture(s => s.PetCountLimit = 1, new AlwaysSucceedsRandom());
        var map = fixture.AddBaseMap(1, "Test");
        var player = fixture.CommandPlayerOn(map, 1, 1);
        MakePet(fixture, player, 1, "Rex");
        MakePet(fixture, player, 2, "Fido");
        MakePet(fixture, player, 3, "Buddy");
        var template = fixture.AddNpcTemplate(1, "Arena Ghast", t => t.MoveSpeed = 1);
        var npc = fixture.SpawnNpc(map, 2, 1, template);
        player.Sent.Clear();

        Assert.False(new SpellEffect().CastTameSpell(player, npc, fixture.World));

        Assert.Equal(3, player.Pets.Count);
        Assert.Contains(player.Sent, p => p.Contains("You have reached the maximum amount of pets. Use /pets to release one."));
    }

    private sealed class AlwaysSucceedsRandom : Random
    {
        public override int Next(int minValue, int maxValue) => minValue;
    }
}
