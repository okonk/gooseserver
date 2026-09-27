using System.Reflection;
using Goose;
using Goose.Events;
using Goose.Testing;
using Xunit;

namespace Goose.Tests;

[Collection("NLog")]
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

        public Fixture(bool equipMount = true, bool seedBaseSpeed = true)
        {
            this.World = new TestWorldFixture();
            var map = this.World.AddBaseMap(1, "Test");
            map.CanUseItems = true;
            this.OtherMap = this.World.AddBaseMap(2, "Test2");
            this.World.Settings.MOTD = "";

            this.Player = this.World.CommandPlayerOn(map, 1, 2, "Tester");
            this.Player.LoginID = 7;
            this.Player.Level = 1;
            this.Player.Experience = 100;
            this.Player.BaseStats.HP = 100;
            if (seedBaseSpeed) SeedBaseMoveSpeed(this.Player, BaseSpeed);

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
    public void Equipping_the_mount_sets_the_mounted_state()
    {
        using var fixture = new Fixture(equipMount: false);

        Assert.True(fixture.Player.Inventory.Equip(fixture.Mount, fixture.World.World));

        Assert.True(fixture.Player.Mounted);
        Assert.True(fixture.Player.IsMounted(fixture.World.World));
        Assert.Equal(MountSpeed, fixture.Player.CalculateMoveSpeed());
    }

    [Fact]
    public void IsMounted_false_when_equipped_but_state_off()
    {
        using var fixture = new Fixture();

        fixture.Player.Mounted = false;

        Assert.False(fixture.Player.IsMounted(fixture.World.World));
    }

    [Fact]
    public void Mount_display_shown_while_mounted()
    {
        using var fixture = new Fixture();

        Assert.EndsWith("273,255,255,255,100,", fixture.MakeCharacter());
        Assert.EndsWith("273,255,255,255,100,", P.UpdateCharacter(fixture.Player));
    }

    [Fact]
    public void Mount_display_hidden_when_equipped_but_dismounted()
    {
        using var fixture = new Fixture();
        fixture.Player.Mounted = false;

        Assert.EndsWith("0,*", fixture.MakeCharacter());
        Assert.EndsWith("0,*", P.UpdateCharacter(fixture.Player));
    }

    [Fact]
    public void Unequipping_the_mount_clears_the_mounted_state()
    {
        using var fixture = new Fixture();

        Assert.True(fixture.Player.Inventory.Unequip(Inventory.EquipSlots.Mount, fixture.World.World));

        Assert.False(fixture.Player.Mounted);
        Assert.False(fixture.Player.IsMounted(fixture.World.World));
        Assert.Equal(BaseSpeed, fixture.Player.CalculateMoveSpeed());
    }

    [Fact]
    public void Equipping_a_second_mount_switches_speed_and_stays_mounted()
    {
        using var fixture = new Fixture();

        var effect2 = fixture.World.AddBaseSpellEffect(260, "Mount Speed III", e =>
        {
            e.EffectType = SpellEffect.EffectTypes.Buff;
            e.Stats = new AttributeSet { MoveSpeed = 200 };
        });
        var template2 = fixture.World.AddBaseItemTemplate(652, "Horse", ItemTemplate.UseTypes.Armor, t =>
        {
            t.Slot = ItemTemplate.ItemSlots.Mount;
            t.GraphicEquipped = 274;
            t.SpellEffect = effect2;
        });
        var horse = new Item();
        horse.LoadFromTemplate(template2);
        fixture.World.World.ItemHandler.AddAndAssignId(horse, fixture.World.World);
        Assert.True(fixture.Player.Inventory.AddItem(horse, 1, fixture.World.World));
        Assert.True(fixture.Player.Inventory.Equip(horse, fixture.World.World));

        Assert.True(fixture.Player.Mounted);
        Assert.Equal(200, fixture.Player.CalculateMoveSpeed());
        Assert.Contains(fixture.Player.Inventory.GetInventorySlots(),
            s => s is not null && s.Item.TemplateID == 651);
    }

    [Fact]
    public void Relogin_dismounts_a_mounted_player()
    {
        using var fixture = new Fixture();
        Assert.True(fixture.Player.Mounted);
        Assert.Equal(MountSpeed, fixture.Player.CalculateMoveSpeed());

        fixture.Player.Spellbook = new Spellbook(fixture.Player, fixture.World.Settings);
        fixture.Player.State = Player.States.LoadingGame;
        fixture.Player.Sent.Clear();
        fixture.World.World.EventHandler.AddEvent(fixture.Player, "LCNT");
        fixture.World.World.EventHandler.Update(fixture.World.World);

        Assert.False(fixture.Player.Mounted);
        Assert.Equal(BaseSpeed, fixture.Player.CalculateMoveSpeed());
        Assert.DoesNotContain(fixture.Player.Buffs, b => b.ItemBuff);
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

    [Fact]
    public void Adding_stats_to_an_empty_speed_queue_publishes_the_new_speed()
    {
        using var fixture = new Fixture(equipMount: false, seedBaseSpeed: false);
        fixture.Player.Sent.Clear();

        fixture.Player.AddStats(new AttributeSet { MoveSpeed = MountSpeed }, fixture.World.World);

        Assert.Equal(MountSpeed, fixture.Player.CalculateMoveSpeed());
        Assert.Contains(fixture.Player.Sent, s => s.Contains("CHP"));
    }

    [Fact]
    public void Removing_stats_from_an_empty_speed_queue_does_not_throw_and_warns()
    {
        using var log = new CapturingLog();
        using var fixture = new Fixture(equipMount: false, seedBaseSpeed: false);

        fixture.Player.RemoveStats(new AttributeSet { MoveSpeed = BaseSpeed }, fixture.World.World);

        Assert.Equal(0, fixture.Player.CalculateMoveSpeed());
        Assert.Contains(log.Messages, m => m.Contains("move speed queue emptied"));
    }

    [Fact]
    public void MNT_toggles_the_mounted_state_and_speed()
    {
        using var fixture = new Fixture();
        Assert.True(fixture.Player.Mounted);

        fixture.Player.Sent.Clear();
        fixture.World.World.EventHandler.AddEvent(fixture.Player, "MNT");
        fixture.World.World.EventHandler.Update(fixture.World.World);

        Assert.False(fixture.Player.Mounted);
        Assert.False(fixture.Player.IsMounted(fixture.World.World));
        Assert.Equal(BaseSpeed, fixture.Player.CalculateMoveSpeed());
        var chp = fixture.Player.Sent.Single(s => s.StartsWith("CHP"));
        Assert.EndsWith("0,*", chp);

        fixture.Player.Sent.Clear();
        fixture.World.World.EventHandler.AddEvent(fixture.Player, "MNT");
        fixture.World.World.EventHandler.Update(fixture.World.World);

        Assert.True(fixture.Player.Mounted);
        Assert.Equal(MountSpeed, fixture.Player.CalculateMoveSpeed());
        var chp2 = fixture.Player.Sent.Single(s => s.StartsWith("CHP"));
        Assert.EndsWith("273,255,255,255,100,", chp2);
    }

    [Fact]
    public void MNT_without_a_mount_is_a_noop()
    {
        using var fixture = new Fixture(equipMount: false);
        fixture.Player.Sent.Clear();

        fixture.World.World.EventHandler.AddEvent(fixture.Player, "MNT");
        fixture.World.World.EventHandler.Update(fixture.World.World);

        Assert.False(fixture.Player.Mounted);
        Assert.DoesNotContain(fixture.Player.Sent, s => s.StartsWith("CHP"));
    }

    [Fact]
    public void MNT_refuses_to_mount_on_a_no_items_map_but_allows_dismount()
    {
        using var fixture = new Fixture();
        Assert.True(fixture.Player.Mounted);

        fixture.Player.Map.CanUseItems = false;

        fixture.Player.Sent.Clear();
        fixture.World.World.EventHandler.AddEvent(fixture.Player, "MNT");
        fixture.World.World.EventHandler.Update(fixture.World.World);
        Assert.False(fixture.Player.Mounted);
        Assert.Equal(BaseSpeed, fixture.Player.CalculateMoveSpeed());

        fixture.Player.Sent.Clear();
        fixture.World.World.EventHandler.AddEvent(fixture.Player, "MNT");
        fixture.World.World.EventHandler.Update(fixture.World.World);
        Assert.False(fixture.Player.Mounted);
        Assert.Contains(fixture.Player.Sent, s => s.Contains("#You can't use items in this map."));
    }

    [Fact]
    public void MNT_works_for_a_visual_only_mount_without_a_buff()
    {
        using var fixture = new Fixture(equipMount: false);

        var template = fixture.World.AddBaseItemTemplate(653, "Statue", ItemTemplate.UseTypes.Armor, t =>
        {
            t.Slot = ItemTemplate.ItemSlots.Mount;
            t.GraphicEquipped = 300;
        });
        var statue = new Item();
        statue.LoadFromTemplate(template);
        fixture.World.World.ItemHandler.AddAndAssignId(statue, fixture.World.World);
        Assert.True(fixture.Player.Inventory.AddItem(statue, 1, fixture.World.World));
        Assert.True(fixture.Player.Inventory.Equip(statue, fixture.World.World));

        Assert.True(fixture.Player.Mounted);
        Assert.True(fixture.Player.IsMounted(fixture.World.World));
        Assert.Empty(fixture.Player.Buffs);
        Assert.Equal(BaseSpeed, fixture.Player.CalculateMoveSpeed());
        Assert.EndsWith("300,255,255,255,100,", fixture.MakeCharacter());

        fixture.Player.Sent.Clear();
        fixture.World.World.EventHandler.AddEvent(fixture.Player, "MNT");
        fixture.World.World.EventHandler.Update(fixture.World.World);

        Assert.False(fixture.Player.Mounted);
        Assert.False(fixture.Player.IsMounted(fixture.World.World));
        var chp = fixture.Player.Sent.Single(s => s.StartsWith("CHP"));
        Assert.EndsWith("0,*", chp);
    }

    [Fact]
    public void Death_keeps_the_player_mounted()
    {
        using var fixture = new Fixture();
        var map = fixture.Player.Map;
        fixture.Player.BoundMap = map;
        fixture.Player.BoundID = map.ID;
        fixture.Player.BoundX = fixture.Player.MapX;
        fixture.Player.BoundY = fixture.Player.MapY;
        fixture.Player.MaxStats.HP = 100;
        fixture.Player.MaxStats.Dexterity = -1; // dodge check is Random.Next(0, 10001) <= dex*100/100 (Player.cs:1992-1997); negative dex makes the 1/10001 dodge impossible
        fixture.Player.CurrentHP = 1;

        var attacker = new NPC { Name = "Slime", LoginID = 99 };
        fixture.Player.Attacked(attacker, 100, fixture.World.World);

        Assert.True(fixture.Player.Mounted);
        Assert.True(fixture.Player.IsMounted(fixture.World.World));
        Assert.Equal(MountSpeed, fixture.Player.CalculateMoveSpeed());
        Assert.Contains(fixture.Player.Buffs, b => b.ItemBuff);
    }

    [Fact]
    public void USE_on_the_mount_slot_unequips_and_dismounts()
    {
        using var fixture = new Fixture();
        Assert.True(fixture.Player.Mounted);

        // Client mount slot id: InventorySize(30) + (int)EquipSlots.Mount(14) + 1 = 45
        fixture.World.World.EventHandler.AddEvent(fixture.Player, "USE45");
        fixture.World.World.EventHandler.Update(fixture.World.World);

        Assert.False(fixture.Player.Mounted);
        Assert.False(fixture.Player.IsMounted(fixture.World.World));
        Assert.Equal(BaseSpeed, fixture.Player.CalculateMoveSpeed());
        Assert.Contains(fixture.Player.Inventory.GetInventorySlots(),
            s => s is not null && s.Item.TemplateID == 651);
    }
}
