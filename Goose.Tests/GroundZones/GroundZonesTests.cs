using Goose.Events;

namespace Goose.Tests.GroundZones;

public class GroundZonesTests
{
    [Fact]
    public void Create_hits_everyone_on_its_tiles_straight_away()
    {
        using var kit = new GroundZoneTestKit();
        var caster = kit.PlayerAt(30, 30);
        var centre = kit.NpcAt(10, 10);
        var corner = kit.NpcAt(11, 11);
        var outside = kit.NpcAt(13, 10);
        long full = outside.CurrentHP;

        kit.Map.GroundZones.Create(kit.Zone(1), caster, 10, 10, kit.World);

        Assert.Equal(full - 10, centre.CurrentHP);
        Assert.Equal(full - 10, corner.CurrentHP);
        Assert.Equal(full, outside.CurrentHP);
    }

    [Fact]
    public void Ticks_until_expiry_then_removes_itself()
    {
        using var kit = new GroundZoneTestKit();
        var caster = kit.PlayerAt(30, 30);
        var npc = kit.NpcAt(10, 10);
        long full = npc.CurrentHP;
        var zone = kit.Map.GroundZones.Create(kit.Zone(1, e => e.Duration = 3), caster, 10, 10, kit.World);

        kit.Map.GroundZones.Tick(zone, kit.At(zone, 1), kit.World);
        kit.Map.GroundZones.Tick(zone, kit.At(zone, 2), kit.World);
        kit.Map.GroundZones.Tick(zone, kit.At(zone, 3), kit.World);
        kit.Map.GroundZones.Tick(zone, kit.At(zone, 4), kit.World);

        Assert.Equal(full - 30, npc.CurrentHP);
        Assert.Empty(kit.Map.GroundZones.All);
        Assert.Null(kit.Map.GroundZones.OwnerAt(10, 10));
    }

    [Fact]
    public void Create_schedules_the_next_tick_one_interval_later()
    {
        using var kit = new GroundZoneTestKit();
        var caster = kit.Fixture.PlayerOn(kit.Map, 30, 30);
        caster.Facing = Direction.Down;
        int before = kit.World.EventHandler.Count;

        var zone = kit.Map.GroundZones.Create(kit.Zone(1, e => e.ZoneTickMs = 1500), caster, 10, 10, kit.World);

        Assert.Equal(before + 1, kit.World.EventHandler.Count);
        var ev = Assert.IsType<GroundZoneTickEvent>(kit.World.EventHandler.Peek());
        Assert.Same(zone, ev.Data);
        Assert.Equal(kit.At(zone, 1.5), ev.Ticks);
    }

    [Fact]
    public void The_last_tick_is_scheduled_at_expiry_not_past_it()
    {
        using var kit = new GroundZoneTestKit();
        var caster = kit.Fixture.PlayerOn(kit.Map, 30, 30);
        caster.Facing = Direction.Down;

        var zone = kit.Map.GroundZones.Create(kit.Zone(1, e => { e.Duration = 1; e.ZoneTickMs = 5000; }),
            caster, 10, 10, kit.World);

        Assert.Equal(zone.ExpiresAt, kit.World.EventHandler.Peek().Ticks);
    }

    [Fact]
    public void A_tiny_tick_interval_is_clamped()
    {
        using var kit = new GroundZoneTestKit();
        var caster = kit.Fixture.PlayerOn(kit.Map, 30, 30);
        caster.Facing = Direction.Down;

        var zone = kit.Map.GroundZones.Create(kit.Zone(1, e => e.ZoneTickMs = 0), caster, 10, 10, kit.World);

        Assert.Equal(kit.At(zone, Goose.GroundZones.MinTickMs / 1000.0), kit.World.EventHandler.Peek().Ticks);
    }

    [Fact]
    public void Tiles_are_clipped_to_the_map()
    {
        using var kit = new GroundZoneTestKit();
        var caster = kit.PlayerAt(30, 30);

        var zone = kit.Map.GroundZones.Create(kit.Zone(1), caster, 1, 1, kit.World);

        Assert.Equal(new HashSet<(int, int)> { (1, 1), (2, 1), (1, 2), (2, 2) }, zone.Tiles);
    }

    [Fact]
    public void The_newest_exclusive_zone_owns_shared_tiles()
    {
        using var kit = new GroundZoneTestKit();
        var caster = kit.PlayerAt(30, 30);
        var onlyOld = kit.NpcAt(9, 10);
        var shared = kit.NpcAt(10, 10);
        long full = shared.CurrentHP;
        var older = kit.Map.GroundZones.Create(kit.Zone(1), caster, 10, 10, kit.World);
        var newer = kit.Map.GroundZones.Create(kit.Zone(2, e => e.HPFormula = "-1"), caster, 11, 10, kit.World);

        kit.Map.GroundZones.Tick(older, kit.At(older, 1), kit.World);

        Assert.Same(older, kit.Map.GroundZones.OwnerAt(9, 10));
        Assert.Same(newer, kit.Map.GroundZones.OwnerAt(10, 10));
        Assert.Equal(full - 20, onlyOld.CurrentHP);
        Assert.Equal(full - 10 - 1, shared.CurrentHP);
    }

    [Fact]
    public void Expiry_hands_tiles_back_to_the_older_zone()
    {
        using var kit = new GroundZoneTestKit();
        var caster = kit.PlayerAt(30, 30);
        var older = kit.Map.GroundZones.Create(kit.Zone(1), caster, 10, 10, kit.World);
        var newer = kit.Map.GroundZones.Create(kit.Zone(2, e => e.Duration = 2), caster, 11, 10, kit.World);

        kit.Map.GroundZones.Tick(newer, newer.ExpiresAt, kit.World);

        Assert.Same(older, kit.Map.GroundZones.OwnerAt(10, 10));
        Assert.Null(kit.Map.GroundZones.OwnerAt(12, 10));
        Assert.Single(kit.Map.GroundZones.All);
    }

    [Fact]
    public void Overlapping_zones_neither_take_nor_lose_tiles()
    {
        using var kit = new GroundZoneTestKit();
        var caster = kit.PlayerAt(30, 30);
        var npc = kit.NpcAt(10, 10);
        long full = npc.CurrentHP;
        var exclusive = kit.Map.GroundZones.Create(kit.Zone(1), caster, 10, 10, kit.World);
        var overlap = kit.Map.GroundZones.Create(kit.Zone(2, e => { e.ZoneOverlaps = true; e.HPFormula = "-1"; }),
            caster, 10, 10, kit.World);
        var newerExclusive = kit.Map.GroundZones.Create(kit.Zone(3, e => e.HPFormula = "-100"), caster, 10, 10, kit.World);

        kit.Map.GroundZones.Tick(overlap, kit.At(overlap, 1), kit.World);

        Assert.Same(newerExclusive, kit.Map.GroundZones.OwnerAt(10, 10));
        Assert.True(kit.Map.GroundZones.IsActiveAt(overlap, 10, 10));
        Assert.False(kit.Map.GroundZones.IsActiveAt(exclusive, 10, 10));
        Assert.Equal(full - 10 - 1 - 100 - 1, npc.CurrentHP);
    }

    [Fact]
    public void Skips_characters_the_effect_cannot_affect()
    {
        using var kit = new GroundZoneTestKit();
        var caster = kit.PlayerAt(10, 10);
        var bystander = kit.PlayerAt(11, 10, "Bystander");

        kit.Map.GroundZones.Create(kit.Zone(1, e => e.Effected = SpellEffect.SpellEffected.NPC), caster, 10, 10, kit.World);

        Assert.Equal(100, caster.CurrentHP);
        Assert.Equal(100, bystander.CurrentHP);
    }

    [Fact]
    public void A_zone_with_no_formula_touches_nobody()
    {
        using var kit = new GroundZoneTestKit();
        var caster = kit.PlayerAt(30, 30);
        var npc = kit.NpcAt(10, 10);
        long full = npc.CurrentHP;

        kit.Map.GroundZones.Create(kit.Zone(1, e => e.HPFormula = "0"), caster, 10, 10, kit.World);

        Assert.Equal(full, npc.CurrentHP);
    }

    [Fact]
    public void Keeps_working_after_the_caster_leaves_for_a_no_cast_map()
    {
        using var kit = new GroundZoneTestKit();
        var caster = kit.PlayerAt(30, 30);
        var npc = kit.NpcAt(10, 10);
        long full = npc.CurrentHP;
        var zone = kit.Map.GroundZones.Create(kit.Zone(1), caster, 10, 10, kit.World);
        var town = kit.Fixture.AddBaseMap(2, "Town", 20, 20);
        town.CanCast = false;
        kit.Map.SetCharacter(null, 30, 30);
        kit.Map.Players.Remove(caster);
        caster.Map = town;

        kit.Map.GroundZones.Tick(zone, kit.At(zone, 1), kit.World);

        Assert.Equal(full - 20, npc.CurrentHP);
    }

    [Fact]
    public void A_removed_zone_ignores_stale_tick_events()
    {
        using var kit = new GroundZoneTestKit();
        var caster = kit.PlayerAt(30, 30);
        var npc = kit.NpcAt(10, 10);
        var zone = kit.Map.GroundZones.Create(kit.Zone(1, e => e.Duration = 1), caster, 10, 10, kit.World);
        kit.Map.GroundZones.Tick(zone, zone.ExpiresAt, kit.World);
        long after = npc.CurrentHP;

        kit.Map.GroundZones.Tick(zone, kit.At(zone, 0.5), kit.World);

        Assert.Equal(after, npc.CurrentHP);
    }
}
