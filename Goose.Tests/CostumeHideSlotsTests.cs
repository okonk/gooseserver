using System.Data.SQLite;
using Goose.Testing;
using Xunit;

namespace Goose.Tests
{
    [Collection("NLog")]
    public class CostumeHideSlotsTests
    {
        private static Item EquipNew(TestWorldFixture world, Player player, int templateId,
            ItemTemplate.ItemSlots slot, int graphic, bool hidesOtherSlots = false)
        {
            var template = world.AddBaseItemTemplate(templateId, "Item" + templateId, ItemTemplate.UseTypes.Armor, t =>
            {
                t.Slot = slot;
                t.GraphicEquipped = graphic;
                t.GraphicA = 0;
                if (hidesOtherSlots) t.Properties["hideOtherSlots"] = true;
            });
            var item = new Item();
            item.LoadFromTemplate(template);
            world.World.ItemHandler.AddAndAssignId(item, world.World);
            Assert.True(player.Inventory.AddItem(item, 1, world.World));
            Assert.True(player.Inventory.Equip(item, world.World));
            return item;
        }

        private static (TestWorldFixture World, Player Player) NewPlayer()
        {
            var world = new TestWorldFixture();
            var map = world.AddBaseMap(1, "Town");
            var player = world.CommandPlayerOn(map, 1, 2, "Hax");
            player.LoginID = 7;
            player.CurrentBodyID = 10001;
            player.HairID = 11;
            player.HairR = 5;
            player.HairG = 6;
            player.HairB = 7;
            player.HairA = 8;
            player.FaceID = 70;

            EquipNew(world, player, 300, ItemTemplate.ItemSlots.Helmet, 31);
            EquipNew(world, player, 301, ItemTemplate.ItemSlots.Shoes, 32);
            return (world, player);
        }

        [Fact]
        public void Without_a_costume_every_slot_and_the_hair_are_shown()
        {
            var (world, player) = NewPlayer();
            using (world)
            {
                Assert.Equal("0,*,31,*,0,*,32,*,0,*,0,*,", player.Inventory.EquippedDisplay());
                Assert.Equal(11, player.Inventory.DisplayedHairID);
            }
        }

        [Fact]
        public void A_costume_hides_the_other_slots_and_the_hair()
        {
            var (world, player) = NewPlayer();
            using (world)
            {
                EquipNew(world, player, 302, ItemTemplate.ItemSlots.Chest, 33, hidesOtherSlots: true);

                Assert.Equal("33,*,0,*,0,*,0,*,0,*,0,*,", player.Inventory.EquippedDisplay());
                Assert.Contains(",0,33,*,0,*,0,*,0,*,0,*,0,*,5,6,7,8,", P.UpdateCharacter(player));
                Assert.Contains(",0,33,*,0,*,0,*,0,*,0,*,0,*,5,6,7,8,", P.MakeCharacter(player));
            }
        }

        [Fact]
        public void A_costume_in_a_slot_the_packet_does_not_draw_still_hides_everything()
        {
            var (world, player) = NewPlayer();
            using (world)
            {
                EquipNew(world, player, 302, ItemTemplate.ItemSlots.Cloak, 33, hidesOtherSlots: true);

                Assert.Equal("0,*,0,*,0,*,0,*,0,*,0,*,", player.Inventory.EquippedDisplay());
                Assert.Equal(0, player.Inventory.DisplayedHairID);
            }
        }

        [Fact]
        public void Unequipping_the_costume_restores_the_other_slots_and_the_hair()
        {
            var (world, player) = NewPlayer();
            using (world)
            {
                var costume = EquipNew(world, player, 302, ItemTemplate.ItemSlots.Chest, 33, hidesOtherSlots: true);
                var slot = player.Inventory.GetEquippedSlot(Inventory.EquipSlots.Chest)!;
                Assert.Same(costume, slot.Item);

                Assert.True(player.Inventory.Unequip(Inventory.EquipSlots.Chest, world.World));

                Assert.Equal("0,*,31,*,0,*,32,*,0,*,0,*,", player.Inventory.EquippedDisplay());
                Assert.Equal(11, player.Inventory.DisplayedHairID);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(1L)]
        [InlineData("true")]
        public void Only_a_json_true_value_sets_the_flag(object value)
        {
            var template = new ItemTemplate();
            template.Properties["hideOtherSlots"] = value;
            Assert.False(template.HidesOtherSlots);
        }

        [Fact]
        public void A_cloned_template_gets_its_own_properties()
        {
            var original = new ItemTemplate { BaseStats = new AttributeSet() };
            original.Properties["hideOtherSlots"] = true;

            var clone = new ItemTemplate(original);
            clone.Properties.Remove("hideOtherSlots");

            Assert.True(original.HidesOtherSlots);
            Assert.False(clone.HidesOtherSlots);
        }

        private static TestWorldFixture WorldWithTemplateRow(string propertiesCell)
        {
            var fixture = new TestWorldFixture();
            fixture.World.Database.Start(Path.Combine(fixture.DataDirectory, "test.db"));
            fixture.World.Database.Execute(conn =>
            {
                using (var schema = conn.CreateCommand())
                {
                    schema.CommandText = SchemaDdl.For("item_templates");
                    schema.ExecuteNonQuery();
                }

                using var insert = conn.CreateCommand();
                insert.CommandText =
                    "INSERT INTO item_templates (item_template_id, item_usetype, item_name, graphic_tile, properties) " +
                    "VALUES (900, 2, 'Costume', 1, @p)";
                insert.Parameters.Add(new SQLiteParameter("@p", propertiesCell));
                insert.ExecuteNonQuery();
            });
            return fixture;
        }

        [Fact]
        public void A_properties_cell_reaches_the_loaded_template()
        {
            using var fixture = WorldWithTemplateRow("{\"hideOtherSlots\":true}");

            fixture.World.ItemHandler.LoadTemplates(fixture.World);

            Assert.True(fixture.World.ItemHandler.GetTemplate(900)!.HidesOtherSlots);
        }

        [Fact]
        public void An_unreadable_properties_cell_still_loads_the_template_and_names_it()
        {
            using var log = new CapturingLog();
            using var fixture = WorldWithTemplateRow("{\"hideOtherSlots\":");

            fixture.World.ItemHandler.LoadTemplates(fixture.World);

            Assert.Empty(fixture.World.ItemHandler.GetTemplate(900)!.Properties);
            Assert.Contains(log.Messages, m => m.Contains("item_templates") && m.Contains("900"));
        }
    }
}
