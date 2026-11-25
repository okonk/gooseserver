using Goose.Testing;

namespace Goose.Tests.GroundZones;

internal sealed class GroundZoneTestKit : IDisposable
{
    public TestWorldFixture Fixture { get; }
    public GameWorld World => Fixture.World;
    public Map Map { get; }
    public NPCTemplate Template { get; }

    public GroundZoneTestKit()
    {
        Fixture = new TestWorldFixture(s => s.DamageModifier = 1);
        Map = Fixture.AddBaseMap(1, "Field", 40, 40);
        Map.CanCast = true;
        Template = Fixture.AddNpcTemplate(500, "Dummy", t => t.AggroRange = 0);
    }

    public TestWorldFixture.CapturingPlayer PlayerAt(int x, int y, string name = "Caster")
    {
        var p = Fixture.CommandPlayerOn(Map, x, y, name);
        Fixture.AddOnlinePlayer(p);
        p.Level = 1;
        p.MaxStats = new AttributeSet { HP = 100, MP = 100 };
        p.CurrentHP = 100;
        p.CurrentMP = 100;
        p.Facing = Direction.Down;
        p.Spellbook = new Spellbook(p, Fixture.Settings);
        Map.Players.Add(p);
        Map.SetCharacter(p, x, y);
        return p;
    }

    public NPC NpcAt(int x, int y)
    {
        var npc = Fixture.SpawnNpc(Map, x, y, Template);
        Assert.Same(npc, Map.GetCharacterAt(x, y));
        return npc;
    }

    public SpellEffect Zone(int id, Action<SpellEffect>? configure = null) =>
        Fixture.AddBaseSpellEffect(id, "Zone " + id, e =>
        {
            e.EffectType = SpellEffect.EffectTypes.GroundZone;
            e.TargetType = SpellEffect.TargetTypes.Area;
            e.TargetSize = 1;
            e.Duration = 10;
            e.ZoneTickMs = 1000;
            e.Effected = SpellEffect.SpellEffected.NPCPlayer;
            e.HPFormula = "-10";
            e.MPFormula = "0";
            e.SPFormula = "0";
            e.WorksInPVP = true;
            configure?.Invoke(e);
        });

    public long At(GroundZone zone, double seconds) => zone.CreatedAt + (long)(seconds * World.TimerFrequency);

    public void Dispose() => Fixture.Dispose();
}
