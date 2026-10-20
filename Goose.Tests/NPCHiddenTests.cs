using Goose;
using Goose.Events;
using Goose.Testing;
using Xunit;

namespace Goose.Tests;

public class NPCHiddenTests
{
    [Fact]
    public void Hidden_npc_does_not_regen()
    {
        using var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(1, "Town", 30, 30);
        var npc = fixture.SpawnNpc(map, 5, 5, fixture.AddNpcTemplate(9001, "Boss", t => t.BaseStats.HP = 1000));
        npc.CurrentHP = 100;
        npc.Hide(fixture.World);

        new RegenEvent { NPC = npc }.Ready(fixture.World);

        Assert.Equal(100, npc.CurrentHP);
    }

    [Fact]
    public void Despawning_while_hidden_leaves_whoever_stands_on_the_tile()
    {
        using var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(1, "Town", 30, 30);
        var npc = fixture.SpawnNpc(map, 5, 5, fixture.AddNpcTemplate(9001, "Boss"));
        npc.Hide(fixture.World);
        var player = fixture.CommandPlayerOn(map, 5, 5, "A");
        map.SetCharacter(player, 5, 5);

        npc.Despawn(fixture.World);

        Assert.Equal(player, map.GetCharacterAt(5, 5));
        Assert.False(npc.Hidden);
    }

    [Fact]
    public void Hidden_npc_does_not_move_or_attack()
    {
        using var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(1, "Town", 30, 30);
        var npc = fixture.SpawnNpc(map, 5, 5, fixture.AddNpcTemplate(9001, "Boss", t => { t.MoveSpeed = 1; t.AggroRange = 10; }));
        var player = fixture.CommandPlayerOn(map, 5, 8, "A");
        map.AddPlayer(player, fixture.World);
        npc.AddAggro(player, 1, fixture.World);
        long hp = player.CurrentHP;
        npc.Hide(fixture.World);

        npc.HandleMoveEvent(fixture.World);
        npc.HandleAttackEvent(fixture.World);

        Assert.Equal((5, 5), (npc.MapX, npc.MapY));
        Assert.Equal(hp, player.CurrentHP);
    }
}
