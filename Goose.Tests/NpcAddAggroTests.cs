using Goose.Testing;

namespace Goose.Tests;

public sealed class NpcAddAggroTests
{
    [Fact]
    public void Aggro_from_a_script_on_an_idle_npc_starts_its_attack_loop()
    {
        using var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(1, "Arena", 30, 30);
        var npc = fixture.SpawnNpc(map, 5, 5, fixture.AddNpcTemplate(9001, "Arena Nagan Beast", t => t.MoveSpeed = 1));
        var player = fixture.CommandPlayerOn(map, 5, 8, "A");
        map.AddPlayer(player, fixture.World);
        Assert.Null(npc.AggroTarget);

        npc.AddAggro(player, 1, fixture.World);

        Assert.Same(player, npc.AggroTarget);
        Assert.NotNull(npc.AttackEvent);
        Assert.NotNull(npc.MoveEvent);
    }
}
