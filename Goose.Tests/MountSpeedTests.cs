using System.Reflection;
using Goose;
using Goose.Events;
using Goose.Testing;
using Xunit;

namespace Goose.Tests;

public class MountSpeedTests
{
    private const int BaseSpeed = 320;
    private const int MountSpeed = 128;

    private static void SeedBaseMoveSpeed(Player player, int speed)
    {
        player.BaseStats.MoveSpeed = speed;
        var queue = (PriorityQueue<int, int>)typeof(Player)
            .GetProperty("moveSpeed", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(player)!;
        queue.Enqueue(speed, speed);
        queue.Enqueue(speed, speed);
    }

    private sealed class Fixture : IDisposable
    {
        public TestWorldFixture World { get; }
        public TestWorldFixture.CapturingPlayer Player { get; }
        public Map OtherMap { get; }
        public Item Mount { get; }

        public Fixture(bool equipMount = true)
        {
            this.World = new TestWorldFixture();
            var map = this.World.AddBaseMap(1, "Test");
            this.OtherMap = this.World.AddBaseMap(2, "Test2");

            this.Player = this.World.CommandPlayerOn(map, 1, 2, "Tester");
            this.Player.LoginID = 7;
            this.Player.Level = 1;
            this.Player.Experience = 100;
            this.Player.BaseStats.HP = 100;
            SeedBaseMoveSpeed(this.Player, BaseSpeed);

            var effect = this.World.AddBaseSpellEffect(259, "Mount Speed II", e =>
            {
                e.EffectType = SpellEffect.EffectTypes.Buff;
                e.Stats = new AttributeSet { MoveSpeed = MountSpeed };
            });

            var template = this.World.AddBaseItemTemplate(651, "Tank", ItemTemplate.UseTypes.Armor, t =>
            {
                t.Slot = ItemTemplate.ItemSlots.Mount;
                t.GraphicEquipped = 273;
                t.SpellEffect = effect;
            });

            this.Mount = new Item();
            this.Mount.LoadFromTemplate(template);
            this.World.World.ItemHandler.AddAndAssignId(this.Mount, this.World.World);
            Assert.True(this.Player.Inventory.AddItem(this.Mount, 1, this.World.World));
            if (equipMount) Assert.True(this.Player.Inventory.Equip(this.Mount, this.World.World));
        }

        public string MakeCharacter() => P.MakeCharacter(this.Player);

        public string MakeCharacterOnNextMapLoad()
        {
            this.Player.Sent.Clear();
            this.Player.WarpTo(this.World.World, this.OtherMap, 3, 4);

            var ev = new DoneLoadingMapEvent { Player = this.Player, Ticks = this.World.World.TimeNow };
            this.World.World.EventHandler.AddEvent(ev);
            this.World.World.EventHandler.Update(this.World.World);

            return this.Player.Sent.Single(s => s.StartsWith("MKC"));
        }

        public void Dispose() => this.World.Dispose();
    }

    [Fact]
    public void Equipping_the_mount_makes_the_mount_speed_win()
    {
        using var fixture = new Fixture();

        Assert.True(fixture.Player.IsMounted(fixture.World.World));
        Assert.Equal(MountSpeed, fixture.Player.CalculateMoveSpeed());
        Assert.Contains($",{MountSpeed},", fixture.MakeCharacter());
    }

    [Fact]
    public void Unequipping_the_mount_restores_the_base_speed()
    {
        using var fixture = new Fixture();

        Assert.True(fixture.Player.Inventory.Unequip(Inventory.EquipSlots.Mount, fixture.World.World));

        Assert.False(fixture.Player.IsMounted(fixture.World.World));
        Assert.Equal(BaseSpeed, fixture.Player.CalculateMoveSpeed());
    }

    [Fact]
    public void Removing_and_readding_the_base_stats_keeps_the_mount_speed()
    {
        using var fixture = new Fixture();

        fixture.Player.RemoveStats(fixture.Player.BaseStats, fixture.World.World, false);
        fixture.Player.AddStats(fixture.Player.BaseStats, fixture.World.World);

        Assert.True(fixture.Player.IsMounted(fixture.World.World));
        Assert.Equal(MountSpeed, fixture.Player.CalculateMoveSpeed());
    }

    [Fact]
    public void Removing_stats_that_were_never_added_keeps_the_mount_speed()
    {
        using var fixture = new Fixture();

        fixture.Player.RemoveStats(new AttributeSet { MoveSpeed = 500 }, fixture.World.World);

        Assert.True(fixture.Player.IsMounted(fixture.World.World));
        Assert.Equal(MountSpeed, fixture.Player.CalculateMoveSpeed());
    }

    [Fact]
    public void BuyVita_while_mounted_keeps_the_mount_speed()
    {
        using var fixture = new Fixture();
        fixture.World.Settings.IncreaseVitaBuyAmount = 100;
        fixture.World.Settings.VitaBuyAmount = 10;
        fixture.World.World.ClassHandler.GetClass(0)!.VitaCost = 10;

        fixture.World.RunCommand(fixture.Player, "/buyvita");

        Assert.Contains(fixture.Player.Sent, s => s.Contains("Bought 10 hp"));
        Assert.True(fixture.Player.IsMounted(fixture.World.World));
        Assert.Equal(MountSpeed, fixture.Player.CalculateMoveSpeed());
    }

    [Fact]
    public void BuyVita_then_changing_maps_still_walks_at_mount_speed()
    {
        using var fixture = new Fixture();
        fixture.World.Settings.IncreaseVitaBuyAmount = 100;
        fixture.World.Settings.VitaBuyAmount = 10;
        fixture.World.World.ClassHandler.GetClass(0)!.VitaCost = 10;

        fixture.World.RunCommand(fixture.Player, "/buyvita");
        string mkc = fixture.MakeCharacterOnNextMapLoad();

        Assert.True(fixture.Player.IsMounted(fixture.World.World));
        Assert.Contains($",{MountSpeed},", mkc);
    }

    [Fact]
    public void Changing_maps_without_mounting_keeps_the_base_speed()
    {
        using var fixture = new Fixture(equipMount: false);

        string mkc = fixture.MakeCharacterOnNextMapLoad();

        Assert.Contains($",{BaseSpeed},", mkc);
    }
}
