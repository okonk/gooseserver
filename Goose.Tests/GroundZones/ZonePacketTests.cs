namespace Goose.Tests.GroundZones;

public class ZonePacketTests
{
    private static List<string> Packets(Goose.Testing.TestWorldFixture.CapturingPlayer p) =>
        string.Concat(p.Sent).Split('\x1').Where(s => s.Length > 0).ToList();

    [Fact]
    public void ZoneAdd_lists_tiles_row_by_row()
    {
        var zone = new GroundZone
        {
            Id = 7,
            Effect = new SpellEffect { Animation = 120, AnimationFile = 4 },
            Tiles = [(11, 10), (10, 11), (10, 10)],
        };

        Assert.Equal("ZADD7,120,4,8000,10,10,11,10,10,11", P.ZoneAdd(zone, 8000));
        Assert.Equal("ZDEL7", P.ZoneDelete(7));
    }

    [Fact]
    public void Creation_and_expiry_are_sent_to_the_whole_map()
    {
        using var kit = new GroundZoneTestKit();
        var near = kit.PlayerAt(11, 10, "Near");
        var far = kit.PlayerAt(39, 39, "Far");
        var caster = kit.Fixture.PlayerOn(kit.Map, 30, 30);
        caster.Facing = Direction.Down;

        var zone = kit.Map.GroundZones.Create(kit.Zone(1, e => { e.Duration = 8; e.HPFormula = "0"; }),
            caster, 10, 10, kit.World);
        kit.Map.GroundZones.Tick(zone, zone.ExpiresAt, kit.World);

        foreach (var p in new[] { near, far })
        {
            Assert.Contains(Packets(p), s => s.StartsWith("ZADD" + zone.Id + ",") && s.Contains(",8000,"));
            Assert.Contains("ZDEL" + zone.Id, Packets(p));
        }
    }

    [Fact]
    public void A_joining_player_is_told_about_active_zones_with_the_time_left()
    {
        using var kit = new GroundZoneTestKit();
        var caster = kit.Fixture.PlayerOn(kit.Map, 30, 30);
        caster.Facing = Direction.Down;
        var zone = kit.Map.GroundZones.Create(kit.Zone(1, e => { e.Duration = 8; e.HPFormula = "0"; }),
            caster, 10, 10, kit.World);
        var arriving = kit.Fixture.CommandPlayerOn(kit.Map, 20, 20, "Arriving");
        kit.Map.SetCharacter(arriving, 20, 20);

        kit.Map.AddPlayer(arriving, kit.World);

        var add = Assert.Single(Packets(arriving), s => s.StartsWith("ZADD"));
        long remaining = long.Parse(add.Substring(4).Split(',')[3]);
        Assert.InRange(remaining, 7000, 8000);
    }
}
