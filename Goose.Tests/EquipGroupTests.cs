using Goose.Testing;
using Xunit;

namespace Goose.Tests
{
    [Collection("NLog")]
    public class EquipGroupTests
    {
        private static Item GiveRing(TestWorldFixture world, Player player, int templateId, string? equipGroup)
        {
            var template = world.World.ItemHandler.GetTemplate(templateId) ??
                world.AddBaseItemTemplate(templateId, "Ring" + templateId, ItemTemplate.UseTypes.Armor, t =>
                {
                    t.Slot = ItemTemplate.ItemSlots.Ring;
                    if (equipGroup is not null) t.Properties["equipGroup"] = equipGroup;
                });
            var item = new Item();
            item.LoadFromTemplate(template);
            world.World.ItemHandler.AddAndAssignId(item, world.World);
            Assert.True(player.Inventory.AddItem(item, 1, world.World));
            return item;
        }

        private static (TestWorldFixture World, Player Player) NewPlayer()
        {
            var world = new TestWorldFixture();
            var map = world.AddBaseMap(1, "Town");
            var player = world.CommandPlayerOn(map, 1, 2, "Hax");
            player.LoginID = 7;
            return (world, player);
        }

        private static Item? Worn(Player player, Inventory.EquipSlots slot) =>
            player.Inventory.GetEquippedSlot(slot)?.Item;

        [Fact]
        public void Rings_without_a_group_fill_both_ring_slots()
        {
            var (world, player) = NewPlayer();
            using (world)
            {
                var first = GiveRing(world, player, 300, null);
                var second = GiveRing(world, player, 301, null);

                Assert.True(player.Inventory.Equip(first, world.World));
                Assert.True(player.Inventory.Equip(second, world.World));

                Assert.Same(first, Worn(player, Inventory.EquipSlots.Ring2));
                Assert.Same(second, Worn(player, Inventory.EquipSlots.Ring1));
            }
        }

        [Fact]
        public void A_ring_from_the_same_group_replaces_the_worn_one_in_its_slot()
        {
            var (world, player) = NewPlayer();
            using (world)
            {
                var basic = GiveRing(world, player, 300, "air-bracelet");
                var other = GiveRing(world, player, 301, null);
                var enchanted = GiveRing(world, player, 302, "air-bracelet");

                Assert.True(player.Inventory.Equip(basic, world.World));
                Assert.True(player.Inventory.Equip(other, world.World));
                Assert.True(player.Inventory.Equip(enchanted, world.World));

                Assert.Same(enchanted, Worn(player, Inventory.EquipSlots.Ring2));
                Assert.Same(other, Worn(player, Inventory.EquipSlots.Ring1));
                Assert.True(player.Inventory.HasItem(300));
            }
        }

        [Fact]
        public void A_second_copy_of_a_grouped_ring_replaces_the_first()
        {
            var (world, player) = NewPlayer();
            using (world)
            {
                var first = GiveRing(world, player, 300, "air-bracelet");
                var second = GiveRing(world, player, 300, "air-bracelet");

                Assert.True(player.Inventory.Equip(first, world.World));
                Assert.True(player.Inventory.Equip(second, world.World));

                Assert.Same(second, Worn(player, Inventory.EquipSlots.Ring2));
                Assert.Null(Worn(player, Inventory.EquipSlots.Ring1));
            }
        }

        [Fact]
        public void Rings_from_different_groups_can_be_worn_together()
        {
            var (world, player) = NewPlayer();
            using (world)
            {
                var air = GiveRing(world, player, 300, "air-bracelet");
                var fire = GiveRing(world, player, 301, "fire-bracelet");

                Assert.True(player.Inventory.Equip(air, world.World));
                Assert.True(player.Inventory.Equip(fire, world.World));

                Assert.Same(air, Worn(player, Inventory.EquipSlots.Ring2));
                Assert.Same(fire, Worn(player, Inventory.EquipSlots.Ring1));
            }
        }
    }
}
