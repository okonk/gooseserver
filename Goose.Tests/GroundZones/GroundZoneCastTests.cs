namespace Goose.Tests.GroundZones;

public class GroundZoneCastTests
{
    private static Spell Learn(GroundZoneTestKit kit, Player caster, SpellEffect effect, Spell.SpellTargets target)
    {
        var spell = kit.Fixture.AddBaseSpell(1, "Zone Spell", effect.ID, s => s.Target = target);
        Assert.True(caster.Spellbook.AddSpell(spell, kit.World));
        return spell;
    }

    [Fact]
    public void A_self_spell_drops_the_zone_on_the_caster()
    {
        using var kit = new GroundZoneTestKit();
        var caster = kit.PlayerAt(10, 10);
        var npc = kit.NpcAt(11, 10);
        long full = npc.CurrentHP;
        Learn(kit, caster, kit.Zone(1, e => e.Effected = SpellEffect.SpellEffected.NPC), Spell.SpellTargets.Self);

        caster.CastSpell(1, caster, kit.World);

        var zone = Assert.Single(kit.Map.GroundZones.All);
        Assert.Contains((10, 10), zone.Tiles);
        Assert.Equal(9, zone.Tiles.Count);
        Assert.Equal(full - 10, npc.CurrentHP);
    }

    [Fact]
    public void A_target_spell_pins_the_zone_where_the_target_stood()
    {
        using var kit = new GroundZoneTestKit();
        var caster = kit.PlayerAt(20, 20);
        var npc = kit.NpcAt(10, 10);
        Learn(kit, caster, kit.Zone(1), Spell.SpellTargets.Target);

        caster.CastSpell(1, npc, kit.World);
        npc.MoveTo(kit.World, 15, 15);

        var zone = Assert.Single(kit.Map.GroundZones.All);
        Assert.Contains((10, 10), zone.Tiles);
        Assert.DoesNotContain((15, 15), zone.Tiles);
    }

    [Fact]
    public void No_zone_is_created_on_a_map_that_blocks_casting()
    {
        using var kit = new GroundZoneTestKit();
        kit.Map.CanCast = false;
        var caster = kit.PlayerAt(10, 10);
        Learn(kit, caster, kit.Zone(1), Spell.SpellTargets.Self);

        caster.CastSpell(1, caster, kit.World);

        Assert.Empty(kit.Map.GroundZones.All);
    }

    [Fact]
    public void The_cast_animation_plays_once_on_creation_not_per_hit()
    {
        using var kit = new GroundZoneTestKit();
        var caster = kit.PlayerAt(10, 10);
        kit.NpcAt(11, 10);
        kit.NpcAt(9, 10);
        Learn(kit, caster, kit.Zone(1, e => { e.DoCastAnimation = true; e.Effected = SpellEffect.SpellEffected.NPC; }),
            Spell.SpellTargets.Self);

        caster.CastSpell(1, caster, kit.World);

        var packets = string.Concat(caster.Sent).Split('\x1');
        Assert.Single(packets, p => p == "CST" + caster.LoginID);
    }

    [Fact]
    public void Description_names_the_zone_and_its_tick()
    {
        using var kit = new GroundZoneTestKit();
        var effect = kit.Zone(1, e => { e.Duration = 8; e.ZoneTickMs = 1500; });

        var lines = effect.GetItemDescription(kit.World).ToList();

        Assert.Contains("Ground zone for 8s, ticks every 1.5s", lines);
        Assert.Contains(lines, l => l.StartsWith("Zone> ") && l.Contains("HP"));
    }
}
