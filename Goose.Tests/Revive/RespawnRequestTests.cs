using Xunit;

namespace Goose.Tests.Revive;

public class RespawnRequestTests
{
    [Fact]
    public void Rspn_while_dead_respawns()
    {
        using var kit = new ReviveTestKit();
        var p = kit.PlayerAt(5, 5);
        kit.Kill(p);

        Assert.True(kit.Fixture.RunCommand(p, "RSPN"));

        Assert.False(p.IsDead);
        Assert.Equal(kit.BoundMap.ID, p.MapID);
    }

    [Fact]
    public void Rspn_while_alive_is_ignored()
    {
        using var kit = new ReviveTestKit();
        var p = kit.PlayerAt(5, 5);

        Assert.True(kit.Fixture.RunCommand(p, "RSPN"));

        Assert.Same(kit.Map, p.Map);
        Assert.Equal(5, p.MapX);
        Assert.Equal(100, p.CurrentHP);
    }
}
