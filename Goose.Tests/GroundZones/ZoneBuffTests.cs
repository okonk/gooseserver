namespace Goose.Tests.GroundZones;

public class ZoneBuffTests
{
    private static SpellEffect Buff(GroundZoneTestKit kit, int id, SpellEffect.EffectTypes type = SpellEffect.EffectTypes.Buff) =>
        kit.Fixture.AddBaseSpellEffect(id, "Buff " + id, e =>
        {
            e.EffectType = type;
            e.Effected = SpellEffect.SpellEffected.SelfNPCPlayer;
            e.WorksNotInPVP = true;
            e.Duration = 30;
        });

    private static SpellEffect BuffZone(GroundZoneTestKit kit, int id, SpellEffect buff, Action<SpellEffect>? configure = null) =>
        kit.Zone(id, e =>
        {
            e.HPFormula = "0";
            e.Effected = SpellEffect.SpellEffected.SelfNPCPlayer;
            e.WorksNotInPVP = true;
            e.ZoneBuffEffectID = buff.ID;
            e.ZoneBuffEffect = buff;
            configure?.Invoke(e);
        });

    private static ICharacter Caster(GroundZoneTestKit kit)
    {
        var caster = kit.Fixture.PlayerOn(kit.Map, 30, 30);
        caster.Facing = Direction.Down;
        return caster;
    }

    private static Buff? Held(ICharacter c, SpellEffect effect) =>
        c.Buffs.SingleOrDefault(b => b.SpellEffect == effect && b.SourceZone is not null);

    [Fact]
    public void Creating_a_zone_buffs_everyone_on_it()
    {
        using var kit = new GroundZoneTestKit();
        var ward = Buff(kit, 50);
        var p = kit.PlayerAt(10, 10, "Walker");
        var npc = kit.NpcAt(11, 11);
        var outside = kit.NpcAt(13, 10);

        var zone = kit.Map.GroundZones.Create(BuffZone(kit, 1, ward), Caster(kit), 10, 10, kit.World);

        Assert.Same(zone, Held(p, ward)!.SourceZone);
        Assert.NotNull(Held(npc, ward));
        Assert.Null(Held(outside, ward));
    }

    [Fact]
    public void Expiry_removes_the_buff()
    {
        using var kit = new GroundZoneTestKit();
        var ward = Buff(kit, 50);
        var p = kit.PlayerAt(10, 10, "Walker");
        var zone = kit.Map.GroundZones.Create(BuffZone(kit, 1, ward), Caster(kit), 10, 10, kit.World);

        kit.Map.GroundZones.Tick(zone, zone.ExpiresAt, kit.World);

        Assert.Null(Held(p, ward));
    }

    [Fact]
    public void A_newer_zone_with_another_buff_swaps_it()
    {
        using var kit = new GroundZoneTestKit();
        var ward = Buff(kit, 50);
        var haste = Buff(kit, 51);
        var p = kit.PlayerAt(10, 10, "Walker");
        kit.Map.GroundZones.Create(BuffZone(kit, 1, ward), Caster(kit), 10, 10, kit.World);

        kit.Map.GroundZones.Create(BuffZone(kit, 2, haste), Caster(kit), 10, 10, kit.World);

        Assert.Null(Held(p, ward));
        Assert.NotNull(Held(p, haste));
    }

    [Fact]
    public void A_newer_zone_with_the_same_buff_takes_it_over_without_reapplying()
    {
        using var kit = new GroundZoneTestKit();
        var ward = Buff(kit, 50);
        var p = kit.PlayerAt(10, 10, "Walker");
        kit.Map.GroundZones.Create(BuffZone(kit, 1, ward), Caster(kit), 10, 10, kit.World);
        var original = Held(p, ward)!;

        var newer = kit.Map.GroundZones.Create(BuffZone(kit, 2, ward), Caster(kit), 10, 10, kit.World);

        Assert.Same(original, Held(p, ward));
        Assert.Same(newer, original.SourceZone);
    }

    [Fact]
    public void Overlapping_zones_keep_the_buff_until_the_last_one_ends()
    {
        using var kit = new GroundZoneTestKit();
        var ward = Buff(kit, 50);
        var p = kit.PlayerAt(10, 10, "Walker");
        var first = kit.Map.GroundZones.Create(BuffZone(kit, 1, ward, e => e.ZoneOverlaps = true), Caster(kit), 10, 10, kit.World);
        var second = kit.Map.GroundZones.Create(BuffZone(kit, 2, ward, e => e.ZoneOverlaps = true), Caster(kit), 10, 10, kit.World);

        kit.Map.GroundZones.Tick(second, second.ExpiresAt, kit.World);
        Assert.Same(first, Held(p, ward)!.SourceZone);

        kit.Map.GroundZones.Tick(first, first.ExpiresAt, kit.World);
        Assert.Null(Held(p, ward));
    }

    [Fact]
    public void Characters_the_zone_cannot_affect_get_nothing()
    {
        using var kit = new GroundZoneTestKit();
        var ward = Buff(kit, 50);
        var p = kit.PlayerAt(10, 10, "Walker");

        kit.Map.GroundZones.Create(BuffZone(kit, 1, ward, e => e.Effected = SpellEffect.SpellEffected.NPC),
            Caster(kit), 10, 10, kit.World);

        Assert.Empty(p.Buffs);
    }

    [Fact]
    public void Snare_zones_only_snare_npcs()
    {
        using var kit = new GroundZoneTestKit();
        var tar = Buff(kit, 50, SpellEffect.EffectTypes.Snare);
        var p = kit.PlayerAt(10, 10, "Walker");
        var npc = kit.Fixture.SpawnNpc(kit.Map, 11, 10,
            kit.Fixture.AddNpcTemplate(501, "Slowable", t => { t.AggroRange = 0; t.CanBeSlowed = true; }));

        kit.Map.GroundZones.Create(BuffZone(kit, 1, tar), Caster(kit), 10, 10, kit.World);

        Assert.Empty(p.Buffs);
        Assert.NotNull(Held(npc, tar));
    }

    [Fact]
    public void A_stun_zone_holds_its_target_until_it_expires()
    {
        using var kit = new GroundZoneTestKit();
        var stun = Buff(kit, 50, SpellEffect.EffectTypes.Stun);
        var p = kit.PlayerAt(10, 10, "Walker");

        var zone = kit.Map.GroundZones.Create(BuffZone(kit, 1, stun), Caster(kit), 10, 10, kit.World);

        Assert.NotNull(Held(p, stun));
        Assert.Contains("BT" + p.LoginID + ",50", string.Concat(p.Sent));

        kit.Map.GroundZones.Tick(zone, zone.ExpiresAt, kit.World);

        Assert.Null(Held(p, stun));
    }

    [Fact]
    public void Root_zones_respect_npc_immunity()
    {
        using var kit = new GroundZoneTestKit();
        var root = Buff(kit, 50, SpellEffect.EffectTypes.Root);
        var immune = kit.NpcAt(10, 10);
        var rootable = kit.Fixture.SpawnNpc(kit.Map, 11, 10,
            kit.Fixture.AddNpcTemplate(501, "Rootable", t => { t.AggroRange = 0; t.CanBeRooted = true; }));

        kit.Map.GroundZones.Create(BuffZone(kit, 1, root), Caster(kit), 10, 10, kit.World);

        Assert.Null(Held(immune, root));
        Assert.NotNull(Held(rootable, root));
    }

    [Fact]
    public void A_timed_copy_of_the_buff_is_left_alone()
    {
        using var kit = new GroundZoneTestKit();
        var ward = Buff(kit, 50);
        var p = kit.PlayerAt(10, 10, "Walker");
        var timed = new Buff { Target = p, Caster = p, SpellEffect = ward, TimeCast = kit.World.TimeNow };
        p.AddBuff(timed, kit.World);

        kit.Map.GroundZones.Create(BuffZone(kit, 1, ward), Caster(kit), 10, 10, kit.World);

        Assert.Same(timed, Assert.Single(p.Buffs));
    }
}
