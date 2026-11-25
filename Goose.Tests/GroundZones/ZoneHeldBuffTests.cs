namespace Goose.Tests.GroundZones;

public class ZoneHeldBuffTests
{
    private static (GroundZoneTestKit kit, GroundZone zone, SpellEffect ward) Setup()
    {
        var kit = new GroundZoneTestKit();
        var ward = kit.Fixture.AddBaseSpellEffect(50, "Ward", e =>
        {
            e.EffectType = SpellEffect.EffectTypes.Buff;
            e.Effected = SpellEffect.SpellEffected.NPCPlayer;
            e.Duration = 30;
            e.BuffCanBeRemoved = true;
            e.Stats = new AttributeSet { AC = 5 };
        });
        var caster = kit.Fixture.PlayerOn(kit.Map, 30, 30);
        caster.Facing = Direction.Down;
        var zone = kit.Map.GroundZones.Create(kit.Zone(1, e => e.HPFormula = "0"), caster, 10, 10, kit.World);
        return (kit, zone, ward);
    }

    private static Buff ZoneBuff(ICharacter target, GroundZone zone, SpellEffect effect, GameWorld world) => new()
    {
        Target = target, Caster = zone.Caster, SpellEffect = effect, TimeCast = world.TimeNow, SourceZone = zone,
    };

    [Fact]
    public void A_player_zone_buff_gets_no_expiry_event()
    {
        var (kit, zone, ward) = Setup();
        using var _ = kit;
        var p = kit.PlayerAt(10, 10, "Walker");
        var buff = ZoneBuff(p, zone, ward, kit.World);

        p.AddBuff(buff, kit.World);

        Assert.Contains(buff, p.Buffs);
        Assert.Null(buff.BuffExpireEvent);
        Assert.Equal((0, 0), buff.GetDurations(kit.World));
    }

    [Fact]
    public void An_npc_zone_buff_gets_no_expiry_event()
    {
        var (kit, zone, ward) = Setup();
        using var _ = kit;
        var npc = kit.NpcAt(10, 10);
        var buff = ZoneBuff(npc, zone, ward, kit.World);

        npc.AddBuff(buff, kit.World);

        Assert.Contains(buff, npc.Buffs);
        Assert.Null(buff.BuffExpireEvent);
    }

    [Fact]
    public void An_ordinary_buff_still_expires()
    {
        var (kit, _, ward) = Setup();
        using var _k = kit;
        var p = kit.PlayerAt(10, 10, "Walker");
        var buff = new Buff { Target = p, Caster = p, SpellEffect = ward, TimeCast = kit.World.TimeNow };

        p.AddBuff(buff, kit.World);

        Assert.NotNull(buff.BuffExpireEvent);
    }

    [Fact]
    public void A_zone_buff_can_not_be_clicked_off()
    {
        var (kit, zone, ward) = Setup();
        using var _ = kit;
        var p = kit.PlayerAt(10, 10, "Walker");
        p.AddBuff(ZoneBuff(p, zone, ward, kit.World), kit.World);

        kit.Fixture.RunCommand(p, "KBUF1");

        Assert.Single(p.Buffs);
    }
}
