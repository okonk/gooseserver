using Goose.Events;
using Goose.Testing;

namespace Goose.Tests;

public class MapTransitionEventGuardTests
{
    [Fact]
    public void TwoMovesEnqueuedBeforeWarp_SecondIsDropped()
    {
        using var fixture = new TestWorldFixture();
        var map1 = fixture.AddBaseMap(1, "a");
        var map2 = fixture.AddBaseMap(2, "b");
        map1.tiles[2 * map1.Width + 2] = new WarpTile { WarpMap = map2, WarpX = 1, WarpY = 1 };
        var player = fixture.CommandPlayerOn(map1, 2, 3);

        fixture.World.EventHandler.AddEvent(player, "M1");
        fixture.World.EventHandler.AddEvent(player, "M1");
        fixture.World.EventHandler.Update(fixture.World);

        Assert.Equal(Player.States.LoadingMap, player.State);
        Assert.Null(player.Map);
        Assert.Equal(1, fixture.World.EventHandler.DroppedDuringMapLoad);
    }

    [Fact]
    public void Update_DuringLoadingMap_DropsNonDlmPlayerEvents()
    {
        using var fixture = new TestWorldFixture();
        var player = new Player(0) { Name = "t", State = Player.States.LoadingMap };

        fixture.World.EventHandler.AddEvent(player, "M1");
        fixture.World.EventHandler.Update(fixture.World);

        Assert.Equal(1, fixture.World.EventHandler.DroppedDuringMapLoad);
    }

    [Fact]
    public void Update_DuringLoadingMap_AllowsPong()
    {
        using var fixture = new TestWorldFixture();
        var player = new Player(0) { Name = "t", State = Player.States.LoadingMap };

        fixture.World.EventHandler.AddEvent(player, "PONG");
        fixture.World.EventHandler.Update(fixture.World);

        Assert.Equal(0, fixture.World.EventHandler.DroppedDuringMapLoad);
    }

    [Fact]
    public void Update_DuringLoadingMap_RunsInternalExpireEvent()
    {
        using var fixture = new TestWorldFixture();
        var player = new Player(0) { Name = "t", State = Player.States.LoadingMap };
        var effect = fixture.AddBaseSpellEffect(1, "d0", e => e.Duration = 0);
        var buff = new Buff { Caster = player, Target = player, SpellEffect = effect };
        player.Buffs.Add(buff);
        var expire = new BuffExpireEvent { Player = player, Data = buff, Ticks = 0 };
        buff.BuffExpireEvent = expire;

        fixture.World.EventHandler.AddEvent(expire);
        fixture.World.EventHandler.Update(fixture.World);

        Assert.DoesNotContain(player.Buffs, b => b == buff);
        Assert.Equal(0, fixture.World.EventHandler.DroppedDuringMapLoad);
    }

    [Fact]
    public void Update_DuringLoadingGame_AllowsLoginContinued()
    {
        using var fixture = new TestWorldFixture(s => { s.MOTD = ""; });
        var map = fixture.AddBaseMap(1, "m");
        var player = fixture.CommandPlayerOn(map, 2, 3);
        player.Spellbook = new Spellbook(player, fixture.Settings);
        player.State = Player.States.LoadingGame;

        fixture.World.EventHandler.AddEvent(player, "LCNT");
        fixture.World.EventHandler.Update(fixture.World);

        Assert.Equal(0, fixture.World.EventHandler.DroppedDuringMapLoad);
        Assert.Equal(Player.States.LoadingMap, player.State);
    }

    [Fact]
    public void Update_DuringLoadingGame_DropsNonLcntPlayerEvents()
    {
        using var fixture = new TestWorldFixture();
        var player = new Player(0) { Name = "t", State = Player.States.LoadingGame };

        fixture.World.EventHandler.AddEvent(player, "M1");
        fixture.World.EventHandler.Update(fixture.World);

        Assert.Equal(1, fixture.World.EventHandler.DroppedDuringMapLoad);
    }

    [Fact]
    public void Update_WhenReady_DropsNothing()
    {
        using var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(1, "m");
        var player = fixture.CommandPlayerOn(map, 2, 3);

        fixture.RunCommand(player, "M1");

        Assert.Equal(0, fixture.World.EventHandler.DroppedDuringMapLoad);
        Assert.Equal(2, player.MapY);
    }
}
