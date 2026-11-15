using Goose;
using Goose.Events;
using Goose.Scripting;
using Goose.Testing;
using Xunit;

namespace Goose.Tests.Revive;

public class ReviveDeathTests
{
    [Fact]
    public void Death_leaves_a_corpse_on_the_tile()
    {
        using var kit = new ReviveTestKit();
        var p = kit.PlayerAt(5, 5);

        kit.Kill(p);

        Assert.True(p.IsDead);
        Assert.Equal(0, p.CurrentHP);
        Assert.Equal(0, p.CurrentMP);
        Assert.Same(kit.Map, p.Map);
        Assert.Equal(5, p.MapX);
        Assert.Equal(5, p.MapY);
    }

    [Fact]
    public void Death_schedules_the_respawn_timeout()
    {
        using var kit = new ReviveTestKit(reviveTimeout: 60);
        var p = kit.PlayerAt(5, 5);
        long before = kit.World.TimeNow;

        kit.Kill(p);

        Assert.NotNull(p.PendingRespawnTimeout);
        Assert.InRange(p.RespawnDeadline, before + 59 * kit.World.TimerFrequency, kit.World.TimeNow + 60 * kit.World.TimerFrequency);
    }

    [Fact]
    public void Revive_disabled_respawns_at_bind_like_today()
    {
        using var kit = new ReviveTestKit(reviveTimeout: 0);
        var p = kit.PlayerAt(5, 5);

        kit.Kill(p);

        Assert.False(p.IsDead);
        Assert.Equal(kit.BoundMap.ID, p.MapID);
        Assert.Equal(5, p.MapX);
        Assert.Equal(6, p.MapY);
        Assert.Equal(50, p.CurrentHP);
        Assert.Equal(10, p.CurrentMP);
    }

    [Fact]
    public void Map_opt_out_respawns_at_bind_like_today()
    {
        using var kit = new ReviveTestKit();
        kit.Map.Script = ScriptStub.For<IMapScript>(new NoReviveMapScript());
        var p = kit.PlayerAt(5, 5);

        kit.Kill(p);

        Assert.False(p.IsDead);
        Assert.Equal(kit.BoundMap.ID, p.MapID);
    }

    [Fact]
    public void Throwing_opt_out_hook_respawns_at_bind_like_today()
    {
        using var kit = new ReviveTestKit();
        kit.Map.Script = ScriptStub.For<IMapScript>(new ThrowingReviveMapScript());
        var p = kit.PlayerAt(5, 5);

        kit.Kill(p);

        Assert.False(p.IsDead);
        Assert.Equal(kit.BoundMap.ID, p.MapID);
    }

    [Fact]
    public void Death_still_runs_OnPlayerDied()
    {
        using var kit = new ReviveTestKit();
        var recorder = new RecordingMapScript();
        kit.Map.Script = ScriptStub.For<IMapScript>(recorder);
        var p = kit.PlayerAt(5, 5);

        kit.Kill(p);

        Assert.Equal(1, recorder.Deaths);
        Assert.True(p.IsDead);
    }

    [Fact]
    public void Timeout_respawns_at_bind_with_half_hp_and_tenth_mp()
    {
        using var kit = new ReviveTestKit();
        var p = kit.PlayerAt(5, 5);
        kit.Kill(p);

        p.PendingRespawnTimeout!.Ready(kit.World);

        Assert.False(p.IsDead);
        Assert.Null(p.PendingRespawnTimeout);
        Assert.Equal(kit.BoundMap.ID, p.MapID);
        Assert.Equal(5, p.MapX);
        Assert.Equal(6, p.MapY);
        Assert.Equal(50, p.CurrentHP);
        Assert.Equal(10, p.CurrentMP);
    }

    [Fact]
    public void Timeout_uses_the_location_OnPlayerDied_returned()
    {
        using var kit = new ReviveTestKit();
        kit.Map.Script = ScriptStub.For<IMapScript>(new RecordingMapScript { Respawn = new RespawnLocation(kit.Map, 7, 8) });
        var p = kit.PlayerAt(5, 5);
        kit.Kill(p);

        p.PendingRespawnTimeout!.Ready(kit.World);

        Assert.Same(kit.Map, p.Map);
        Assert.Equal(7, p.MapX);
        Assert.Equal(8, p.MapY);
    }

    [Fact]
    public void Timeout_during_cross_map_load_respawns_at_the_warp_destination()
    {
        using var kit = new ReviveTestKit();
        var p = kit.PlayerAt(5, 5);
        kit.Kill(p);
        var other = kit.Fixture.AddBaseMap(3, "Far", 40, 40);

        p.WarpTo(kit.World, other, 9, 9);
        Assert.Null(p.Map);
        Assert.Equal(Player.States.LoadingMap, p.State);

        p.PendingRespawnTimeout!.Ready(kit.World);

        Assert.False(p.IsDead);
        Assert.Null(p.PendingRespawnTimeout);
        Assert.Equal(other.ID, p.MapID);
        Assert.Equal(9, p.MapX);
        Assert.Equal(9, p.MapY);
        Assert.Equal(50, p.CurrentHP);

        new DoneLoadingMapEvent { Player = p }.Ready(kit.World);

        Assert.Same(other, p.Map);
        Assert.Equal(Player.States.Ready, p.State);
    }

    [Fact]
    public void Respawn_during_cross_map_load_does_not_throw()
    {
        using var kit = new ReviveTestKit();
        var p = kit.PlayerAt(5, 5);
        kit.Kill(p);
        var other = kit.Fixture.AddBaseMap(3, "Far", 40, 40);

        p.WarpTo(kit.World, other, 9, 9);
        Assert.Null(p.Map);
        Assert.Equal(Player.States.LoadingMap, p.State);
        p.Respawn(kit.World);

        Assert.False(p.IsDead);
        Assert.Equal(other.ID, p.MapID);
        Assert.Equal(50, p.CurrentHP);
    }

    [Fact]
    public void Stale_timeout_does_nothing()
    {
        using var kit = new ReviveTestKit();
        var p = kit.PlayerAt(5, 5);
        p.BoundMap = kit.Map;
        p.BoundX = 10;
        p.BoundY = 10;
        kit.Kill(p);
        var first = p.PendingRespawnTimeout!;
        p.Respawn(kit.World);
        kit.Kill(p);

        first.Ready(kit.World);

        Assert.True(p.IsDead);
        Assert.NotSame(first, p.PendingRespawnTimeout);
    }

    [Fact]
    public void Corpse_ignores_damage_and_heals()
    {
        using var kit = new ReviveTestKit();
        var p = kit.PlayerAt(5, 5);
        kit.Kill(p);

        p.Attacked(kit.Killer, 50, kit.World);
        p.Attacked(kit.Killer, -50, kit.World);

        Assert.Equal(0, p.CurrentHP);
        Assert.True(p.IsDead);
    }

    [Fact]
    public void Corpse_does_not_regen()
    {
        using var kit = new ReviveTestKit();
        var p = kit.PlayerAt(5, 5);
        p.MaxStats.HPPercentRegen = 0.5;
        kit.Kill(p);

        new RegenEvent { Data = p }.Ready(kit.World);

        Assert.Equal(0, p.CurrentHP);
    }

    [Fact]
    public void Death_clears_npc_aggro_and_corpses_cannot_gain_aggro()
    {
        using var kit = new ReviveTestKit();
        var brute = kit.Fixture.SpawnNpc(kit.Map, 6, 5, kit.Fixture.AddNpcTemplate(501, "Brute"));
        var p = kit.PlayerAt(5, 5);
        brute.AddAggro(p, 10, kit.World);
        Assert.Same(p, brute.AggroTarget);

        kit.Kill(p);
        Assert.Null(brute.AggroTarget);

        brute.AggroIfInRange(p, kit.World);
        brute.AddAggro(p, 10, kit.World);
        Assert.Null(brute.AggroTarget);
    }

    [Fact]
    public void Death_destroys_pets()
    {
        using var kit = new ReviveTestKit();
        var p = kit.PlayerAt(5, 5);
        var pet = new Pet
        {
            LoginID = 1001, Name = "Pet", Owner = p,
            Class = kit.World.ClassHandler.GetClass(0)!,
            BaseStats = new AttributeSet { HP = 100 }, MaxStats = new AttributeSet { HP = 100 },
            HairA = 255, FaceID = 70, State = Player.States.Ready,
            Map = kit.Map, MapID = kit.Map.ID, MapX = 6, MapY = 6,
        };
        pet.CurrentHP = 100;
        kit.Map.Players.Add(pet);
        kit.Map.SetCharacter(pet, 6, 6);
        p.Pets.Add(pet);

        kit.Kill(p);

        Assert.False(pet.IsAlive);
    }

    [Fact]
    public void Death_closes_windows()
    {
        using var kit = new ReviveTestKit();
        var p = kit.PlayerAt(5, 5);
        p.Windows.Add(new Window { ID = 42, Type = Window.WindowTypes.Vendor });

        kit.Kill(p);

        Assert.Empty(p.Windows);
    }

    [Fact]
    public void Death_dismounts()
    {
        using var kit = new ReviveTestKit();
        var p = kit.PlayerAt(5, 5);
        var template = kit.Fixture.AddBaseItemTemplate(651, "Tank", ItemTemplate.UseTypes.Armor, t =>
        {
            t.Slot = ItemTemplate.ItemSlots.Mount;
            t.GraphicEquipped = 273;
        });
        var mount = new Item();
        mount.LoadFromTemplate(template);
        kit.World.ItemHandler.AddAndAssignId(mount, kit.World);
        Assert.True(p.Inventory.AddItem(mount, 1, kit.World));
        Assert.True(p.Inventory.Equip(mount, kit.World));
        Assert.True(p.Mounted);

        kit.Kill(p);

        Assert.False(p.Mounted);
    }

    private sealed class NoReviveMapScript : BaseMapScript
    {
        public override bool AllowsRevive(Map map, Player player, ICharacter killer, GameWorld world) => false;
    }

    private sealed class ThrowingReviveMapScript : BaseMapScript
    {
        public override bool AllowsRevive(Map map, Player player, ICharacter killer, GameWorld world)
            => throw new InvalidOperationException("boom");
    }

    private sealed class RecordingMapScript : BaseMapScript
    {
        public int Deaths { get; private set; }
        public RespawnLocation? Respawn { get; init; }

        public override RespawnLocation? OnPlayerDied(Map map, Player player, ICharacter killer, GameWorld world)
        {
            this.Deaths++;
            return this.Respawn;
        }
    }
}
