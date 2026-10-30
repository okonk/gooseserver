using Goose;
using Goose.Events;
using Goose.Testing;
using Xunit;

namespace Goose.Tests;

public class RejectedMoveFacingSyncTests
{
    private static void AddBuff(Player player, SpellEffect.EffectTypes type) =>
        player.Buffs.Add(new Buff
        {
            Caster = player, Target = player,
            SpellEffect = new SpellEffect { EffectType = type, Duration = 1000 },
        });

    [Fact]
    public void Rooted_move_turns_the_player_without_moving_them()
    {
        using var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(1, "Town", 20, 20);
        var player = fixture.CommandPlayerOn(map, 5, 5);
        player.Facing = Direction.Down;
        AddBuff(player, SpellEffect.EffectTypes.Root);

        new MoveEvent { Player = player, Data = "M2" }.Ready(fixture.World);

        Assert.Equal(Direction.Right, player.Facing);
        Assert.Equal((5, 5), (player.MapX, player.MapY));
        Assert.Contains(P.ChangeHeading(player) + "\x01", player.Sent);
        Assert.Contains(P.SetYourPosition(player) + "\x01", player.Sent);
    }

    [Fact]
    public void Move_with_a_vendor_open_turns_the_player_without_moving_them()
    {
        using var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(1, "Town", 20, 20);
        var player = fixture.CommandPlayerOn(map, 5, 5);
        player.Facing = Direction.Down;
        player.Windows.Add(new Window { Type = Window.WindowTypes.Vendor });

        new MoveEvent { Player = player, Data = "M4" }.Ready(fixture.World);

        Assert.Equal(Direction.Left, player.Facing);
        Assert.Equal((5, 5), (player.MapX, player.MapY));
        Assert.Contains(P.ChangeHeading(player) + "\x01", player.Sent);
    }

    [Fact]
    public void Stunned_move_keeps_facing_and_corrects_the_clients_heading()
    {
        using var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(1, "Town", 20, 20);
        var player = fixture.CommandPlayerOn(map, 5, 5);
        player.Facing = Direction.Down;
        AddBuff(player, SpellEffect.EffectTypes.Stun);

        new MoveEvent { Player = player, Data = "M1" }.Ready(fixture.World);

        Assert.Equal(Direction.Down, player.Facing);
        Assert.Contains(P.ChangeHeading(player) + "\x01", player.Sent);
        Assert.Contains(P.SetYourPosition(player) + "\x01", player.Sent);
    }

    [Fact]
    public void Stunned_turn_keeps_facing_and_corrects_the_clients_heading()
    {
        using var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(1, "Town", 20, 20);
        var player = fixture.CommandPlayerOn(map, 5, 5);
        player.Facing = Direction.Down;
        AddBuff(player, SpellEffect.EffectTypes.Stun);

        new FacingEvent { Player = player, Data = "F1" }.Ready(fixture.World);

        Assert.Equal(Direction.Down, player.Facing);
        Assert.Contains(P.ChangeHeading(player) + "\x01", player.Sent);
    }
}
