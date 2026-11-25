using Goose.Tests.Revive;

namespace Goose.Tests.GroundZones;

public class ZoneBuffMovementTests
{
    private static (SpellEffect ward, SpellEffect zone) Effects(Goose.Testing.TestWorldFixture fixture)
    {
        var ward = fixture.AddBaseSpellEffect(50, "Ward", e =>
        {
            e.EffectType = SpellEffect.EffectTypes.Buff;
            e.Effected = SpellEffect.SpellEffected.SelfNPCPlayer;
            e.WorksNotInPVP = true;
            e.Duration = 30;
        });
        var zone = fixture.AddBaseSpellEffect(1, "Sanctuary", e =>
        {
            e.EffectType = SpellEffect.EffectTypes.GroundZone;
            e.TargetType = SpellEffect.TargetTypes.Area;
            e.TargetSize = 1;
            e.Duration = 10;
            e.ZoneTickMs = 1000;
            e.Effected = SpellEffect.SpellEffected.SelfNPCPlayer;
            e.WorksNotInPVP = true;
            e.HPFormula = "0";
            e.MPFormula = "0";
            e.WorksInPVP = true;
            e.ZoneBuffEffectID = ward.ID;
            e.ZoneBuffEffect = ward;
        });
        return (ward, zone);
    }

    private static Player Caster(Goose.Testing.TestWorldFixture fixture, Map map)
    {
        var caster = fixture.PlayerOn(map, 30, 30);
        caster.Facing = Direction.Down;
        return caster;
    }

    private static bool HasZoneBuff(ICharacter c, SpellEffect ward) =>
        c.Buffs.Any(b => b.SpellEffect == ward && b.SourceZone is not null);

    private static Pet PetAt(GroundZoneTestKit kit, int x, int y, Player owner)
    {
        var pet = new Pet
        {
            LoginID = 1001,
            Name = "Pet",
            Owner = owner,
            Class = kit.World.ClassHandler.GetClass(0)!,
            BaseStats = new AttributeSet { HP = 100 },
            MaxStats = new AttributeSet { HP = 100 },
            HairA = 255,
            FaceID = 70,
            State = Player.States.Ready,
            Map = kit.Map,
            MapID = kit.Map.ID,
            MapX = x,
            MapY = y,
        };
        pet.CurrentHP = 100;
        pet.Level = 1;
        kit.Map.AddPlayer(pet, kit.World);
        kit.Map.PlaceCharacter(pet);
        kit.Map.SetCharacter(pet, x, y);
        return pet;
    }

    [Fact]
    public void Walking_in_and_out_adds_and_removes_the_buff()
    {
        using var kit = new GroundZoneTestKit();
        var (ward, zone) = Effects(kit.Fixture);
        var p = kit.PlayerAt(12, 10, "Walker");
        kit.Map.GroundZones.Create(zone, Caster(kit.Fixture, kit.Map), 10, 10, kit.World);
        Assert.False(HasZoneBuff(p, ward));

        p.MoveTo(kit.World, 11, 10);
        Assert.True(HasZoneBuff(p, ward));

        p.MoveTo(kit.World, 12, 10);
        Assert.False(HasZoneBuff(p, ward));
    }

    [Fact]
    public void Npcs_walking_in_and_out_are_tracked_too()
    {
        using var kit = new GroundZoneTestKit();
        var (ward, zone) = Effects(kit.Fixture);
        var npc = kit.NpcAt(12, 10);
        kit.Map.GroundZones.Create(zone, Caster(kit.Fixture, kit.Map), 10, 10, kit.World);

        npc.MoveTo(kit.World, 11, 10);
        Assert.True(HasZoneBuff(npc, ward));

        npc.MoveTo(kit.World, 12, 10);
        Assert.False(HasZoneBuff(npc, ward));
    }

    [Fact]
    public void A_same_map_warp_out_of_the_zone_removes_the_buff()
    {
        using var kit = new GroundZoneTestKit();
        var (ward, zone) = Effects(kit.Fixture);
        var p = kit.PlayerAt(10, 10, "Walker");
        kit.Map.GroundZones.Create(zone, Caster(kit.Fixture, kit.Map), 10, 10, kit.World);

        p.WarpTo(kit.World, kit.Map, 25, 25);

        Assert.False(HasZoneBuff(p, ward));
    }

    [Fact]
    public void Leaving_the_map_removes_the_buff()
    {
        using var kit = new GroundZoneTestKit();
        var (ward, zone) = Effects(kit.Fixture);
        var town = kit.Fixture.AddBaseMap(2, "Town", 20, 20);
        var p = kit.PlayerAt(10, 10, "Walker");
        kit.Map.GroundZones.Create(zone, Caster(kit.Fixture, kit.Map), 10, 10, kit.World);

        p.WarpTo(kit.World, town, 5, 5);

        Assert.False(HasZoneBuff(p, ward));
    }

    [Fact]
    public void Joining_the_map_on_a_zone_tile_applies_the_buff()
    {
        using var kit = new GroundZoneTestKit();
        var (ward, zone) = Effects(kit.Fixture);
        kit.Map.GroundZones.Create(zone, Caster(kit.Fixture, kit.Map), 10, 10, kit.World);
        var p = kit.Fixture.CommandPlayerOn(kit.Map, 10, 10, "Arriving");
        kit.Map.SetCharacter(p, 10, 10);

        kit.Map.AddPlayer(p, kit.World);

        Assert.True(HasZoneBuff(p, ward));
    }

    [Fact]
    public void Death_removes_the_buff_and_revive_brings_it_back()
    {
        using var kit = new ReviveTestKit();
        var (ward, zone) = Effects(kit.Fixture);
        var p = kit.PlayerAt(10, 10);
        kit.Map.GroundZones.Create(zone, Caster(kit.Fixture, kit.Map), 10, 10, kit.World);
        Assert.True(HasZoneBuff(p, ward));

        kit.Kill(p);
        Assert.True(p.IsDead);
        Assert.False(HasZoneBuff(p, ward));

        kit.Map.GroundZones.RecheckBuffs(p, kit.World);
        Assert.False(HasZoneBuff(p, ward));

        p.Revive(p, kit.World);
        Assert.True(HasZoneBuff(p, ward));
    }

    [Fact]
    public void No_revive_respawn_keeps_the_zone_buff()
    {
        using var kit = new GroundZoneTestKit();
        kit.World.Settings.ReviveTimeout = 0;
        var (ward, zone) = Effects(kit.Fixture);
        var p = kit.PlayerAt(10, 10, "Walker");
        p.BoundMap = kit.Map;
        p.BoundX = 10;
        p.BoundY = 10;
        var killer = kit.Fixture.SpawnNpc(kit.Map, 20, 20, kit.Template);

        kit.Map.GroundZones.Create(zone, Caster(kit.Fixture, kit.Map), 10, 10, kit.World);
        Assert.True(HasZoneBuff(p, ward));

        p.Attacked(killer, p.CurrentHP + 1000, kit.World);

        Assert.False(p.IsDead);
        Assert.True(HasZoneBuff(p, ward));
    }

    [Fact]
    public void Hidden_npcs_lose_the_buff_and_get_it_back_when_revealed_inside()
    {
        using var kit = new GroundZoneTestKit();
        var (ward, zone) = Effects(kit.Fixture);
        var npc = kit.NpcAt(10, 10);
        kit.Map.GroundZones.Create(zone, Caster(kit.Fixture, kit.Map), 10, 10, kit.World);

        npc.Hide(kit.World);
        Assert.False(HasZoneBuff(npc, ward));

        npc.Reveal(kit.World, 11, 10);
        Assert.True(HasZoneBuff(npc, ward));
    }

    [Fact]
    public void A_destroyed_pet_keeps_no_zone_buff()
    {
        using var kit = new GroundZoneTestKit();
        var (ward, zone) = Effects(kit.Fixture);
        var owner = kit.PlayerAt(30, 30, "Owner");
        var pet = PetAt(kit, 10, 10, owner);
        kit.Map.GroundZones.Create(zone, Caster(kit.Fixture, kit.Map), 10, 10, kit.World);
        Assert.True(HasZoneBuff(pet, ward));

        pet.Destroy(kit.World);

        Assert.False(HasZoneBuff(pet, ward));
    }

    [Fact]
    public void A_pet_walking_out_of_the_zone_loses_the_buff()
    {
        using var kit = new GroundZoneTestKit();
        var (ward, zone) = Effects(kit.Fixture);
        var owner = kit.PlayerAt(30, 30, "Owner");
        var pet = PetAt(kit, 10, 10, owner);
        kit.Map.GroundZones.Create(zone, Caster(kit.Fixture, kit.Map), 10, 10, kit.World);
        Assert.True(HasZoneBuff(pet, ward));

        pet.MoveTo(kit.World, 12, 10);

        Assert.False(HasZoneBuff(pet, ward));
    }
}
