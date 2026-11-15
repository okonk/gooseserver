using Goose;
using Goose.Testing;
using Xunit;

namespace Goose.Tests.Revive;

internal sealed class ReviveTestKit : IDisposable
{
    public TestWorldFixture Fixture { get; }
    public GameWorld World => Fixture.World;
    public Map Map { get; }
    public Map BoundMap { get; }
    public NPC Killer { get; }

    public ReviveTestKit(int reviveTimeout = 60)
    {
        Fixture = new TestWorldFixture(s => s.ReviveTimeout = reviveTimeout);
        Map = Fixture.AddBaseMap(1, "Field", 40, 40);
        Map.CanCast = true;
        Map.CanUseItems = true;
        BoundMap = Fixture.AddBaseMap(2, "Town", 40, 40);
        var template = Fixture.AddNpcTemplate(500, "Wolf", t => t.AggroRange = 0);
        Killer = Fixture.SpawnNpc(Map, 8, 5, template);
    }

    public TestWorldFixture.CapturingPlayer PlayerAt(int x, int y, string name = "Tester")
    {
        var p = Fixture.CommandPlayerOn(Map, x, y, name);
        Fixture.AddOnlinePlayer(p);
        p.Level = 1;
        p.MaxStats = new AttributeSet { HP = 100, MP = 100 };
        p.CurrentHP = 100;
        p.CurrentMP = 100;
        p.BodyID = 1;
        p.CurrentBodyID = 1;
        p.HairA = 255;
        p.FaceID = 70;
        p.BoundMap = BoundMap;
        p.BoundX = 5;
        p.BoundY = 6;
        Map.Players.Add(p);
        Map.SetCharacter(p, x, y);
        return p;
    }

    public void Kill(Player p) => p.Attacked(Killer, p.CurrentHP + 1000, World);

    public void Dispose() => Fixture.Dispose();
}
