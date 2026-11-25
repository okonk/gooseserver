using Goose.Testing;

namespace Goose.Tests.GroundZones;

public class GroundTargetCastTests
{
    private static GroundZoneTestKit Kit(out TestWorldFixture.CapturingPlayer caster, Action<Spell>? configure = null)
    {
        var kit = new GroundZoneTestKit();
        caster = kit.PlayerAt(10, 10);
        var effect = kit.Zone(1, e => e.HPFormula = "0");
        var spell = kit.Fixture.AddBaseSpell(1, "Fire Patch", effect.ID, s =>
        {
            s.Target = Spell.SpellTargets.Target;
            s.GroundTargetable = true;
            configure?.Invoke(s);
        });
        caster.Spellbook.AddSpell(spell, kit.World);
        return kit;
    }

    [Fact]
    public void Casting_on_a_tile_drops_the_zone_there()
    {
        using var kit = Kit(out var caster);

        kit.Fixture.RunCommand(caster, "CAST1,0,15,12");

        var zone = Assert.Single(kit.Map.GroundZones.All);
        Assert.Contains((15, 12), zone.Tiles);
        Assert.Equal(9, zone.Tiles.Count);
    }

    [Fact]
    public void Tile_casts_pay_costs_and_start_the_cooldown()
    {
        using var kit = Kit(out var caster, s => { s.MPStaticCost = 30; s.Aether = 10000; });

        kit.Fixture.RunCommand(caster, "CAST1,0,15,12");
        kit.Fixture.RunCommand(caster, "CAST1,0,16,12");

        Assert.Equal(70, caster.CurrentMP);
        Assert.Single(kit.Map.GroundZones.All);
    }

    [Fact]
    public void Spells_that_are_not_ground_targetable_ignore_tile_casts()
    {
        using var kit = Kit(out var caster, s => s.GroundTargetable = false);

        kit.Fixture.RunCommand(caster, "CAST1,0,15,12");

        Assert.Empty(kit.Map.GroundZones.All);
        Assert.Equal(1, caster.SuspectedMacroCount);
    }

    [Theory]
    [InlineData(0, 12)]
    [InlineData(41, 12)]
    [InlineData(34, 12)]
    [InlineData(10, 26)]
    public void Tiles_off_the_map_or_out_of_view_are_rejected(int x, int y)
    {
        using var kit = Kit(out var caster);

        kit.Fixture.RunCommand(caster, $"CAST1,0,{x},{y}");

        Assert.Empty(kit.Map.GroundZones.All);
    }

    [Fact]
    public void A_nonzero_target_with_coordinates_is_rejected()
    {
        using var kit = Kit(out var caster);

        kit.Fixture.RunCommand(caster, "CAST1,5,15,12");

        Assert.Empty(kit.Map.GroundZones.All);
    }

    [Fact]
    public void Ordinary_two_part_casts_still_work()
    {
        using var kit = Kit(out var caster);
        var npc = kit.NpcAt(12, 10);

        kit.Fixture.RunCommand(caster, "CAST1," + npc.LoginID);

        var zone = Assert.Single(kit.Map.GroundZones.All);
        Assert.Contains((12, 10), zone.Tiles);
    }
}
