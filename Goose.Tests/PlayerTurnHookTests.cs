using Goose;
using Goose.Events;
using Goose.Scripting;
using Goose.Testing;
using Xunit;

namespace Goose.Tests;

public class PlayerTurnHookTests
{
    private sealed class TurnCounter : BaseMapScript
    {
        public int Turns;
        public override void OnPlayerTurn(Map map, Player player, GameWorld world) => Turns++;
    }

    [Fact]
    public void Turning_calls_the_map_scripts_turn_hook_only_when_the_facing_changes()
    {
        using var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(1, "Town", 20, 20);
        var counter = new TurnCounter();
        var script = (Script<IMapScript>)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Script<IMapScript>));
        typeof(Script<IMapScript>).GetProperty("Object")!.SetValue(script, counter);
        map.Script = script;
        var player = fixture.CommandPlayerOn(map, 5, 5, "A");
        player.Facing = Direction.Down;

        new FacingEvent { Player = player, Data = "F1" }.Ready(fixture.World);
        new FacingEvent { Player = player, Data = "F1" }.Ready(fixture.World);

        Assert.Equal(Direction.Up, player.Facing);
        Assert.Equal(1, counter.Turns);
    }
}
