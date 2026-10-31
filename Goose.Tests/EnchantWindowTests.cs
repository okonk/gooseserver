using Goose;
using Goose.Testing;
using Xunit;

namespace Goose.Tests;

public class EnchantWindowTests
{
    private const int TableId = 900;
    private const int SwordId = 901;
    private const int EssenceId = 902;
    private const int ProtectionId = 903;
    private const int MountId = 904;

    private static (TestWorldFixture Fixture, Map Map, TestWorldFixture.CapturingPlayer Player, NPC Npc,
                    ItemTemplate Sword, ItemTemplate Essence, ItemTemplate Protection, ItemTemplate Mount) Setup()
    {
        var fixture = new TestWorldFixture(s =>
        {
            s.Enchanting.EssenceTemplates[EssenceId] = EnchantElement.Earth;
            s.Enchanting.ProtectionTemplateId = ProtectionId;
        });
        var map = fixture.AddBaseMap(1, "Town", 40, 40);
        var player = fixture.CommandPlayerOn(map, 5, 5, "Tester");
        fixture.AddOnlinePlayer(player);
        var template = fixture.AddNpcTemplate(TableId, "Enchanting Table", t => t.NPCType = NPCTemplate.Types.Ambient);
        var npc = fixture.SpawnNpc(map, 5, 5, template, properties: new PropertiesDictionary { ["enchantTable"] = true });
        var sword = fixture.AddBaseItemTemplate(SwordId, "Iron Sword", ItemTemplate.UseTypes.Weapon);
        var essence = fixture.AddBaseItemTemplate(EssenceId, "Earth Essence", ItemTemplate.UseTypes.OneTime);
        var protection = fixture.AddBaseItemTemplate(ProtectionId, "Protection Charm", ItemTemplate.UseTypes.OneTime);
        var mount = fixture.AddBaseItemTemplate(MountId, "War Steed", ItemTemplate.UseTypes.Weapon,
            t => t.Slot = ItemTemplate.ItemSlots.Mount);
        return (fixture, map, player, npc, sword, essence, protection, mount);
    }

    private static Item PlaceItem(TestWorldFixture.CapturingPlayer player, ItemTemplate template, int slot)
    {
        var item = new Item();
        item.LoadFromTemplate(template);
        player.Inventory.SetSlot(slot, new ItemSlot { Item = item, Stack = 1 });
        return item;
    }

    private static EnchantWindow OpenWindow(TestWorldFixture fixture, TestWorldFixture.CapturingPlayer player, NPC npc)
    {
        Assert.True(fixture.RunCommand(player, "RC" + npc.MapX + "," + npc.MapY));
        var window = Assert.Single(player.Windows);
        return Assert.IsType<EnchantWindow>(window);
    }

    [Fact]
    public void RightClick_opens_window_and_sends_mkwenw()
    {
        var (fixture, map, player, npc, sword, essence, protection, mount) = Setup();
        using (fixture)
        {
            var window = OpenWindow(fixture, player, npc);

            Assert.Equal("Enchanting Table", window.Title);
            Assert.Equal(Window.WindowFrames.Enchant, window.Frame);
            Assert.Equal(Window.WindowTypes.Enchant, window.Type);
            Assert.Equal("0,1,0,0,1", window.Buttons);
            Assert.Same(npc, window.NPC);

            Assert.Contains(player.Sent, s => s.StartsWith($"MKW{window.ID},33,Enchanting Table,0,1,0,0,1,{npc.LoginID},"));
            Assert.Contains(player.Sent, s => s.StartsWith("EWP" + window.ID + ",0,0,0,0"));
            Assert.Contains(player.Sent, s => s.StartsWith("ENW" + window.ID));
        }
    }

    [Fact]
    public void RightClick_second_open_replaces_first()
    {
        var (fixture, map, player, npc, sword, essence, protection, mount) = Setup();
        using (fixture)
        {
            var first = OpenWindow(fixture, player, npc);
            var second = OpenWindow(fixture, player, npc);

            Assert.Single(player.Windows);
            Assert.NotSame(first, second);
            Assert.Contains(player.Sent, s => s.StartsWith("CLW" + first.ID));
        }
    }

    [Fact]
    public void Clicked_close_removes_window_without_clw()
    {
        var (fixture, map, player, npc, sword, essence, protection, mount) = Setup();
        using (fixture)
        {
            var window = OpenWindow(fixture, player, npc);
            player.Sent.Clear();

            window.Clicked(Window.ButtonTypes.Close, 0, 0, 0, player, fixture.World);

            Assert.DoesNotContain(window, player.Windows);
            Assert.DoesNotContain(player.Sent, s => s.StartsWith("CLW"));
        }
    }

    [Fact]
    public void Clicked_exit_removes_window_without_clw()
    {
        var (fixture, map, player, npc, sword, essence, protection, mount) = Setup();
        using (fixture)
        {
            var window = OpenWindow(fixture, player, npc);
            player.Sent.Clear();

            window.Clicked(Window.ButtonTypes.Exit, 0, 0, 0, player, fixture.World);

            Assert.DoesNotContain(window, player.Windows);
            Assert.DoesNotContain(player.Sent, s => s.StartsWith("CLW"));
        }
    }

    [Fact]
    public void Ews_valid_item_and_essence_sends_preview()
    {
        var (fixture, map, player, npc, sword, essence, protection, mount) = Setup();
        using (fixture)
        {
            var window = OpenWindow(fixture, player, npc);
            player.Sent.Clear();
            PlaceItem(player, sword, 1);
            PlaceItem(player, essence, 2);

            Assert.True(fixture.RunCommand(player, "EWS1,2,0"));

            Assert.Equal(1, window.ItemSlotId);
            Assert.Equal(2, window.EssenceSlotId);
            Assert.Equal(0, window.ProtectionSlotId);
            Assert.Contains(player.Sent, s => s.StartsWith(
                "EWP" + window.ID + ",1,2,0,1" +
                "|Iron Sword (max 10)" +
                "|Next: Earth +1% spell damage" +
                "|Failure chance: none" +
                "|Protected: not needed"));
        }
    }

    [Fact]
    public void Ews_mount_item_echoed_zero_with_reason()
    {
        var (fixture, map, player, npc, sword, essence, protection, mount) = Setup();
        using (fixture)
        {
            var window = OpenWindow(fixture, player, npc);
            player.Sent.Clear();
            PlaceItem(player, mount, 1);
            PlaceItem(player, essence, 2);

            Assert.True(fixture.RunCommand(player, "EWS1,2,0"));

            Assert.Equal(0, window.ItemSlotId);
            Assert.Equal(2, window.EssenceSlotId);
            Assert.Contains(player.Sent, s => s.StartsWith(
                "EWP" + window.ID + ",0,2,0,0" +
                "|This item cannot be enchanted."));
        }
    }

    [Fact]
    public void Ews_unknown_essence_echoed_zero()
    {
        var (fixture, map, player, npc, sword, essence, protection, mount) = Setup();
        using (fixture)
        {
            var window = OpenWindow(fixture, player, npc);
            player.Sent.Clear();
            PlaceItem(player, sword, 1);
            PlaceItem(player, protection, 2);

            Assert.True(fixture.RunCommand(player, "EWS1,2,0"));

            Assert.Equal(1, window.ItemSlotId);
            Assert.Equal(0, window.EssenceSlotId);
            Assert.Contains(player.Sent, s => s.StartsWith(
                "EWP" + window.ID + ",1,0,0,0" +
                "|Iron Sword (max 10)" +
                "|Failure chance: none" +
                "|Protected: not needed"));
        }
    }

    [Fact]
    public void Ews_duplicate_slot_id_rejects_later_slot()
    {
        var (fixture, map, player, npc, sword, essence, protection, mount) = Setup();
        using (fixture)
        {
            var window = OpenWindow(fixture, player, npc);
            player.Sent.Clear();
            PlaceItem(player, sword, 1);
            PlaceItem(player, essence, 2);
            PlaceItem(player, protection, 3);

            Assert.True(fixture.RunCommand(player, "EWS1,2,2"));

            Assert.Equal(1, window.ItemSlotId);
            Assert.Equal(2, window.EssenceSlotId);
            Assert.Equal(0, window.ProtectionSlotId);
            Assert.Contains(player.Sent, s => s.StartsWith(
                "EWP" + window.ID + ",1,2,0,1" +
                "|Iron Sword (max 10)" +
                "|Next: Earth +1% spell damage" +
                "|Failure chance: none" +
                "|Protected: not needed"));
        }
    }

    [Fact]
    public void Ews_protection_on_safe_item_not_needed()
    {
        var (fixture, map, player, npc, sword, essence, protection, mount) = Setup();
        using (fixture)
        {
            var window = OpenWindow(fixture, player, npc);
            player.Sent.Clear();
            var item = PlaceItem(player, sword, 1);
            item.ItemProperties[ItemProperty.EnchantEarth] = 2;
            item.Name = "Iron Sword +2";
            PlaceItem(player, essence, 2);
            PlaceItem(player, protection, 3);

            Assert.True(fixture.RunCommand(player, "EWS1,2,3"));

            Assert.Equal(3, window.ProtectionSlotId);
            Assert.Contains(player.Sent, s => s.StartsWith(
                "EWP" + window.ID + ",1,2,3,1" +
                "|Iron Sword +2 (max 10)" +
                "|Earth 2: +2% spell damage" +
                "|Next: Earth +1% spell damage" +
                "|Failure chance: none" +
                "|Protected: not needed"));
        }
    }

    [Fact]
    public void Ews_protection_on_risky_item_shows_yes()
    {
        var (fixture, map, player, npc, sword, essence, protection, mount) = Setup();
        using (fixture)
        {
            var window = OpenWindow(fixture, player, npc);
            player.Sent.Clear();
            var item = PlaceItem(player, sword, 1);
            item.ItemProperties[ItemProperty.EnchantEarth] = 5;
            item.Name = "Iron Sword +5";
            PlaceItem(player, essence, 2);
            PlaceItem(player, protection, 3);

            Assert.True(fixture.RunCommand(player, "EWS1,2,3"));

            Assert.Contains(player.Sent, s => s.StartsWith(
                "EWP" + window.ID + ",1,2,3,1" +
                "|Iron Sword +5 (max 10)" +
                "|Earth 5: +5% spell damage" +
                "|Next: Earth +1% spell damage" +
                "|Failure chance: 30%" +
                "|Protected: yes"));
        }
    }

    [Fact]
    public void Ews_out_of_range_notices_and_keeps_stored_slots()
    {
        var (fixture, map, player, npc, sword, essence, protection, mount) = Setup();
        using (fixture)
        {
            var window = OpenWindow(fixture, player, npc);
            PlaceItem(player, sword, 1);
            PlaceItem(player, essence, 2);
            Assert.True(fixture.RunCommand(player, "EWS1,2,0"));

            player.MapX = 30;
            player.Sent.Clear();

            Assert.True(fixture.RunCommand(player, "EWS1,2,0"));

            Assert.Contains(player.Sent, s => s.Contains("You are too far from the enchanting table."));
            Assert.Equal(1, window.ItemSlotId);
            Assert.Equal(2, window.EssenceSlotId);
            Assert.DoesNotContain(player.Sent, s => s.StartsWith("EWP"));
        }
    }

    [Fact]
    public void Ews_dormant_feature_sends_not_available()
    {
        var fixture = new TestWorldFixture();
        using (fixture)
        {
            var map = fixture.AddBaseMap(1, "Town", 40, 40);
            var player = fixture.CommandPlayerOn(map, 5, 5, "Tester");
            fixture.AddOnlinePlayer(player);
            var template = fixture.AddNpcTemplate(TableId, "Enchanting Table", t => t.NPCType = NPCTemplate.Types.Ambient);
            var npc = fixture.SpawnNpc(map, 5, 5, template, properties: new PropertiesDictionary { ["enchantTable"] = true });
            var sword = fixture.AddBaseItemTemplate(SwordId, "Iron Sword", ItemTemplate.UseTypes.Weapon);
            var item = PlaceItem(player, sword, 1);

            var window = OpenWindow(fixture, player, npc);
            player.Sent.Clear();

            Assert.True(fixture.RunCommand(player, "EWS1,0,0"));

            Assert.Equal(0, window.ItemSlotId);
            Assert.Contains(player.Sent, s => s.StartsWith(
                "EWP" + window.ID + ",0,0,0,0" +
                "|Enchanting is not available."));
        }
    }
}
