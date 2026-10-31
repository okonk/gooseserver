using Goose;
using Goose.Testing;
using Xunit;

namespace Goose.Tests;

public class EnchantWindowConfirmTests
{
    private const int TableId = 900;
    private const int SwordId = 901;
    private const int EssenceId = 902;
    private const int ProtectionId = 903;

    private sealed class EnchantRollRandom(params int[] enchantRolls) : Random
    {
        private readonly Queue<int> rolls = new(enchantRolls);

        public override int Next(int minValue, int maxValue)
        {
            if (minValue == 1 && maxValue == 101)
                return rolls.Dequeue();
            return base.Next(minValue, maxValue);
        }
    }

    private static (TestWorldFixture Fixture, Map Map, TestWorldFixture.CapturingPlayer Player, NPC Npc,
                    ItemTemplate Sword, ItemTemplate Essence, ItemTemplate Protection) Setup(Random? random = null)
    {
        var fixture = new TestWorldFixture(s =>
        {
            s.Enchanting.EssenceTemplates[EssenceId] = EnchantElement.Earth;
            s.Enchanting.ProtectionTemplateId = ProtectionId;
        }, random);
        var map = fixture.AddBaseMap(1, "Town", 40, 40);
        var player = fixture.CommandPlayerOn(map, 5, 5, "Tester");
        fixture.AddOnlinePlayer(player);
        var template = fixture.AddNpcTemplate(TableId, "Enchanting Table", t => t.NPCType = NPCTemplate.Types.Ambient);
        var npc = fixture.SpawnNpc(map, 5, 5, template, properties: new PropertiesDictionary { ["enchantTable"] = true });
        var sword = fixture.AddBaseItemTemplate(SwordId, "Iron Sword", ItemTemplate.UseTypes.Weapon);
        var essence = fixture.AddBaseItemTemplate(EssenceId, "Earth Essence", ItemTemplate.UseTypes.OneTime,
            t => t.StackSize = 5);
        var protection = fixture.AddBaseItemTemplate(ProtectionId, "Protection Charm", ItemTemplate.UseTypes.OneTime);
        return (fixture, map, player, npc, sword, essence, protection);
    }

    private static Item PlaceItem(TestWorldFixture.CapturingPlayer player, ItemTemplate template, int slot, long stack = 1)
    {
        var item = new Item();
        item.LoadFromTemplate(template);
        player.Inventory.SetSlot(slot, new ItemSlot { Item = item, Stack = stack });
        return item;
    }

    private static EnchantWindow OpenWindow(TestWorldFixture fixture, TestWorldFixture.CapturingPlayer player, NPC npc)
    {
        Assert.True(fixture.RunCommand(player, "RC" + npc.MapX + "," + npc.MapY));
        var window = Assert.Single(player.Windows);
        return Assert.IsType<EnchantWindow>(window);
    }

    private static Log EnchantLog(TestWorldFixture fixture)
    {
        var log = Assert.Single(fixture.World.LogHandler.Pending, l => l.Type == Log.Types.Enchant);
        return log;
    }

    [Fact]
    public void Ewc_success_consumes_essence_enchants_item_and_logs()
    {
        var (fixture, map, player, npc, sword, essence, protection) = Setup();
        using (fixture)
        {
            var window = OpenWindow(fixture, player, npc);
            var item = PlaceItem(player, sword, 1);
            var essenceItem = PlaceItem(player, essence, 2, stack: 3);
            Assert.True(fixture.RunCommand(player, "EWS1,2,0"));
            player.Sent.Clear();

            Assert.True(fixture.RunCommand(player, "EWC1,2,0"));

            var slot = player.Inventory.GetSlot(2);
            Assert.NotNull(slot);
            Assert.Same(essenceItem, slot.Item);
            Assert.Equal(2, slot.Stack);
            Assert.Equal(1, Enchanting.Count(item, EnchantElement.Earth));
            Assert.Equal("Iron Sword +1", item.Name);
            Assert.Contains(player.Sent, s => s.StartsWith("SIS1|"));
            Assert.Contains(player.Sent, s => s.Contains("Your Iron Sword +1 glows (+1)."));

            var log = EnchantLog(fixture);
            Assert.Equal("Enchant: template " + SwordId + " element Earth level 0 -> 1 max 10 protected 0 outcome Success",
                log.Text);
            Assert.Equal(item.ItemID, log.OtherID);
        }
    }

    [Fact]
    public void Ewc_protected_failure_consumes_protection_and_lowers_max()
    {
        var (fixture, map, player, npc, sword, essence, protection) = Setup(new EnchantRollRandom(10));
        using (fixture)
        {
            var window = OpenWindow(fixture, player, npc);
            var item = PlaceItem(player, sword, 1);
            item.ItemProperties[ItemProperty.EnchantEarth] = 5;
            item.Name = "Iron Sword +5";
            var essenceItem = PlaceItem(player, essence, 2, stack: 3);
            var protectionItem = PlaceItem(player, protection, 3);
            Assert.True(fixture.RunCommand(player, "EWS1,2,3"));
            player.Sent.Clear();

            Assert.True(fixture.RunCommand(player, "EWC1,2,3"));

            Assert.Null(player.Inventory.GetSlot(3));
            var essenceSlot = player.Inventory.GetSlot(2);
            Assert.NotNull(essenceSlot);
            Assert.Same(essenceItem, essenceSlot.Item);
            Assert.Equal(2, essenceSlot.Stack);
            Assert.Same(item, player.Inventory.GetSlot(1)!.Item);
            Assert.Equal(9, Enchanting.Max(item, fixture.Settings.Enchanting));
            Assert.Equal(5, Enchanting.Count(item, EnchantElement.Earth));
            Assert.Contains(player.Sent, s => s.Contains(
                "The enchantment fizzled. Your protection held, but the item can now only reach +9."));

            var log = EnchantLog(fixture);
            Assert.Equal("Enchant: template " + SwordId + " element Earth level 5 -> 5 max 9 protected 1 outcome ProtectedFailure",
                log.Text);
        }
    }

    [Fact]
    public void Ewc_unprotected_failure_destroys_item_and_logs_it()
    {
        var (fixture, map, player, npc, sword, essence, protection) = Setup(new EnchantRollRandom(10));
        using (fixture)
        {
            var window = OpenWindow(fixture, player, npc);
            var item = PlaceItem(player, sword, 1);
            item.ItemProperties[ItemProperty.EnchantEarth] = 5;
            item.Name = "Iron Sword +5";
            var essenceItem = PlaceItem(player, essence, 2, stack: 3);
            Assert.True(fixture.RunCommand(player, "EWS1,2,0"));
            player.Sent.Clear();

            Assert.True(fixture.RunCommand(player, "EWC1,2,0"));

            Assert.Null(player.Inventory.GetSlot(1));
            var essenceSlot = player.Inventory.GetSlot(2);
            Assert.NotNull(essenceSlot);
            Assert.Same(essenceItem, essenceSlot.Item);
            Assert.Equal(2, essenceSlot.Stack);
            Assert.Contains(player.Sent, s => s.Contains("The enchantment failed and your Iron Sword +5 shattered."));

            var log = EnchantLog(fixture);
            Assert.StartsWith(
                "Enchant: template " + SwordId + " element Earth level 5 -> 5 max 10 protected 0 outcome Destroyed item ",
                log.Text);
            Assert.Contains(" item {", log.Text);
            Assert.Equal(item.ItemID, log.OtherID);
        }
    }

    [Fact]
    public void Ewc_protection_on_safe_level_is_not_consumed()
    {
        var (fixture, map, player, npc, sword, essence, protection) = Setup();
        using (fixture)
        {
            var window = OpenWindow(fixture, player, npc);
            var item = PlaceItem(player, sword, 1);
            item.ItemProperties[ItemProperty.EnchantEarth] = 2;
            item.Name = "Iron Sword +2";
            PlaceItem(player, essence, 2);
            var protectionItem = PlaceItem(player, protection, 3);
            Assert.True(fixture.RunCommand(player, "EWS1,2,3"));
            player.Sent.Clear();

            Assert.True(fixture.RunCommand(player, "EWC1,2,3"));

            var protectionSlot = player.Inventory.GetSlot(3);
            Assert.NotNull(protectionSlot);
            Assert.Same(protectionItem, protectionSlot.Item);
            Assert.Equal(3, Enchanting.Level(item));
            Assert.Equal(3, Enchanting.Count(item, EnchantElement.Earth));
            Assert.Equal("Iron Sword +3", item.Name);
            Assert.Contains(player.Sent, s => s.Contains("Your Iron Sword +3 glows (+3)."));

            var log = EnchantLog(fixture);
            Assert.Equal("Enchant: template " + SwordId + " element Earth level 2 -> 3 max 10 protected 0 outcome Success",
                log.Text);
        }
    }

    [Fact]
    public void Ewc_refused_when_item_moved_after_ews()
    {
        var (fixture, map, player, npc, sword, essence, protection) = Setup();
        using (fixture)
        {
            var window = OpenWindow(fixture, player, npc);
            var item = PlaceItem(player, sword, 1);
            var essenceItem = PlaceItem(player, essence, 2, stack: 3);
            Assert.True(fixture.RunCommand(player, "EWS1,2,0"));

            player.Inventory.SetSlot(4, player.Inventory.GetSlot(1));
            player.Inventory.SetSlot(1, null);
            player.Sent.Clear();

            Assert.True(fixture.RunCommand(player, "EWC1,2,0"));

            Assert.Contains(player.Sent, s => s.Contains("Items changed, place them again."));
            var essenceSlot = player.Inventory.GetSlot(2);
            Assert.NotNull(essenceSlot);
            Assert.Same(essenceItem, essenceSlot.Item);
            Assert.Equal(3, essenceSlot.Stack);
            Assert.Equal(0, Enchanting.Count(item, EnchantElement.Earth));
            Assert.Empty(fixture.World.LogHandler.Pending);
        }
    }

    [Fact]
    public void Ewc_refused_when_ids_differ_from_last_ews()
    {
        var (fixture, map, player, npc, sword, essence, protection) = Setup();
        using (fixture)
        {
            var window = OpenWindow(fixture, player, npc);
            PlaceItem(player, sword, 1);
            var essenceItem = PlaceItem(player, essence, 2, stack: 3);
            Assert.True(fixture.RunCommand(player, "EWS1,2,0"));
            player.Sent.Clear();

            Assert.True(fixture.RunCommand(player, "EWC1,3,0"));

            Assert.Contains(player.Sent, s => s.Contains("Items changed, place them again."));
            var essenceSlot = player.Inventory.GetSlot(2);
            Assert.NotNull(essenceSlot);
            Assert.Same(essenceItem, essenceSlot.Item);
            Assert.Equal(3, essenceSlot.Stack);
            Assert.Empty(fixture.World.LogHandler.Pending);
        }
    }

    [Fact]
    public void Ewc_last_essence_consumed_followup_preview_has_zero_essence()
    {
        var (fixture, map, player, npc, sword, essence, protection) = Setup();
        using (fixture)
        {
            var window = OpenWindow(fixture, player, npc);
            var item = PlaceItem(player, sword, 1);
            PlaceItem(player, essence, 2, stack: 1);
            Assert.True(fixture.RunCommand(player, "EWS1,2,0"));
            player.Sent.Clear();

            Assert.True(fixture.RunCommand(player, "EWC1,2,0"));

            Assert.Null(player.Inventory.GetSlot(2));
            Assert.Equal(1, Enchanting.Count(item, EnchantElement.Earth));
            Assert.Contains(player.Sent, s => s.StartsWith(
                "EWP" + window.ID + ",1,0,0,0" +
                "|Iron Sword +1 (max 10)" +
                "|Earth 1: +1% spell damage" +
                "|Failure chance: none" +
                "|Protected: not needed"));
        }
    }
}
