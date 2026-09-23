using Goose;
using Goose.Testing;
using Xunit;

namespace Goose.Tests
{
    public class CustomItemTests
    {
        private static (TestWorldFixture fixture, TestWorldFixture.CapturingPlayer player) WorldAndPlayer()
        {
            var fixture = new TestWorldFixture(s => s.CustomTicketId = 823);
            var map = fixture.AddBaseMap(1, "Test");
            var player = fixture.CommandPlayerOn(map, 5, 5, "Tester");
            return (fixture, player);
        }

        private static Item LoadItem(ItemTemplate template)
        {
            var item = new Item();
            item.LoadFromTemplate(template);
            return item;
        }

        private static (ItemTemplate ticket, ItemTemplate stats, ItemTemplate look) Templates(TestWorldFixture fixture)
        {
            var ticket = fixture.AddBaseItemTemplate(823, "Custom Ticket", ItemTemplate.UseTypes.Armor);
            var stats = fixture.AddBaseItemTemplate(900, "Steel Sword", ItemTemplate.UseTypes.Weapon);
            var look = fixture.AddBaseItemTemplate(901, "Shadow Sword", ItemTemplate.UseTypes.Weapon);
            return (ticket, stats, look);
        }

        [Fact]
        public void ValidateItems_both_valid_same_type_equipment_passes()
        {
            var (fixture, player) = WorldAndPlayer();
            using (fixture)
            {
                var stats = fixture.AddBaseItemTemplate(900, "Steel Chest", ItemTemplate.UseTypes.Armor, t => t.Slot = ItemTemplate.ItemSlots.Chest);
                var look = fixture.AddBaseItemTemplate(901, "Shadow Chest", ItemTemplate.UseTypes.Armor, t => t.Slot = ItemTemplate.ItemSlots.Chest);

                Assert.True(CustomItem.ValidateItems(fixture.World, player, LoadItem(stats), LoadItem(look)));
                Assert.Empty(player.Sent);
            }
        }

        [Fact]
        public void ValidateItems_different_equipment_types_fails()
        {
            var (fixture, player) = WorldAndPlayer();
            using (fixture)
            {
                var stats = fixture.AddBaseItemTemplate(900, "Steel Chest", ItemTemplate.UseTypes.Armor, t => t.Slot = ItemTemplate.ItemSlots.Chest);
                var look = fixture.AddBaseItemTemplate(901, "Shadow Helmet", ItemTemplate.UseTypes.Armor, t => t.Slot = ItemTemplate.ItemSlots.Helmet);

                Assert.False(CustomItem.ValidateItems(fixture.World, player, LoadItem(stats), LoadItem(look)));
                Assert.Contains(player.Sent, s => s.Contains("Items to be customised must be of the same equipment type."));
            }
        }

        [Fact]
        public void ValidateItems_one_handed_stats_with_two_handed_look_passes()
        {
            var (fixture, player) = WorldAndPlayer();
            using (fixture)
            {
                var stats = fixture.AddBaseItemTemplate(900, "Steel Sword", ItemTemplate.UseTypes.Weapon, t => t.Slot = ItemTemplate.ItemSlots.OneHanded);
                var look = fixture.AddBaseItemTemplate(901, "Shadow Staff", ItemTemplate.UseTypes.Weapon, t => t.Slot = ItemTemplate.ItemSlots.TwoHanded);

                Assert.True(CustomItem.ValidateItems(fixture.World, player, LoadItem(stats), LoadItem(look)));
                Assert.Empty(player.Sent);
            }
        }

        [Theory]
        [InlineData(ItemTemplate.ItemSlots.Ring)]
        [InlineData(ItemTemplate.ItemSlots.Necklace)]
        [InlineData(ItemTemplate.ItemSlots.Pauldrons)]
        [InlineData(ItemTemplate.ItemSlots.Cloak)]
        [InlineData(ItemTemplate.ItemSlots.Belt)]
        [InlineData(ItemTemplate.ItemSlots.Gloves)]
        public void ValidateItems_excluded_slot_on_stats_item_fails(ItemTemplate.ItemSlots slot)
        {
            var (fixture, player) = WorldAndPlayer();
            using (fixture)
            {
                var stats = fixture.AddBaseItemTemplate(900, "Steel Ring", ItemTemplate.UseTypes.Armor, t => t.Slot = slot);
                var look = fixture.AddBaseItemTemplate(901, "Shadow Chest", ItemTemplate.UseTypes.Armor, t => t.Slot = ItemTemplate.ItemSlots.Chest);

                Assert.False(CustomItem.ValidateItems(fixture.World, player, LoadItem(stats), LoadItem(look)));
                Assert.Contains(player.Sent, s => s.Contains("Items to be customised must be equipment and must be visible items."));
            }
        }

        [Theory]
        [InlineData(ItemTemplate.ItemSlots.Ring)]
        [InlineData(ItemTemplate.ItemSlots.Necklace)]
        [InlineData(ItemTemplate.ItemSlots.Pauldrons)]
        [InlineData(ItemTemplate.ItemSlots.Cloak)]
        [InlineData(ItemTemplate.ItemSlots.Belt)]
        [InlineData(ItemTemplate.ItemSlots.Gloves)]
        public void ValidateItems_excluded_slot_on_look_item_fails(ItemTemplate.ItemSlots slot)
        {
            var (fixture, player) = WorldAndPlayer();
            using (fixture)
            {
                var stats = fixture.AddBaseItemTemplate(900, "Steel Chest", ItemTemplate.UseTypes.Armor, t => t.Slot = ItemTemplate.ItemSlots.Chest);
                var look = fixture.AddBaseItemTemplate(901, "Shadow Ring", ItemTemplate.UseTypes.Armor, t => t.Slot = slot);

                Assert.False(CustomItem.ValidateItems(fixture.World, player, LoadItem(stats), LoadItem(look)));
                Assert.Contains(player.Sent, s => s.Contains("Items to be customised must be equipment and must be visible items."));
            }
        }

        [Theory]
        [InlineData(ItemTemplate.UseTypes.NoUse)]
        [InlineData(ItemTemplate.UseTypes.Scroll)]
        public void ValidateItems_non_equipment_use_type_fails(ItemTemplate.UseTypes useType)
        {
            var (fixture, player) = WorldAndPlayer();
            using (fixture)
            {
                var badStats = fixture.AddBaseItemTemplate(900, "Steel Scroll", useType, t => t.Slot = ItemTemplate.ItemSlots.Chest);
                var validLook = fixture.AddBaseItemTemplate(901, "Shadow Chest", ItemTemplate.UseTypes.Armor, t => t.Slot = ItemTemplate.ItemSlots.Chest);

                Assert.False(CustomItem.ValidateItems(fixture.World, player, LoadItem(badStats), LoadItem(validLook)));
                Assert.Contains(player.Sent, s => s.Contains("Items to be customised must be equipment and must be visible items."));

                player.Sent.Clear();

                var validStats = fixture.AddBaseItemTemplate(902, "Steel Chest", ItemTemplate.UseTypes.Armor, t => t.Slot = ItemTemplate.ItemSlots.Chest);
                var badLook = fixture.AddBaseItemTemplate(903, "Shadow Scroll", useType, t => t.Slot = ItemTemplate.ItemSlots.Chest);

                Assert.False(CustomItem.ValidateItems(fixture.World, player, LoadItem(validStats), LoadItem(badLook)));
                Assert.Contains(player.Sent, s => s.Contains("Items to be customised must be equipment and must be visible items."));
            }
        }

        [Fact]
        public void Make_name_is_truncated_to_255_before_commas_are_stripped()
        {
            var (fixture, player) = WorldAndPlayer();
            using (fixture)
            {
                var (ticket, stats, look) = Templates(fixture);
                var bag = player.Inventory.GetCombineBagContainer();
                bag.SetSlot(1, new ItemSlot { Item = LoadItem(ticket) });
                bag.SetSlot(2, new ItemSlot { Item = LoadItem(stats) });
                bag.SetSlot(3, new ItemSlot { Item = LoadItem(look) });

                var name = new string('a', 254) + "," + new string('b', 45);
                Assert.Equal(300, name.Length);

                Assert.True(fixture.RunCommand(player, $"/custom make 255 0 0 255 {name}"));

                var result = bag.GetSlot(1)!;
                Assert.Equal(new string('a', 254), result.Item.Name);
            }
        }

        [Fact]
        public void Make_look_item_in_excluded_slot_is_refused_with_equipment_message()
        {
            var (fixture, player) = WorldAndPlayer();
            using (fixture)
            {
                var ticket = fixture.AddBaseItemTemplate(823, "Custom Ticket", ItemTemplate.UseTypes.Armor);
                var stats = fixture.AddBaseItemTemplate(900, "Steel Chest", ItemTemplate.UseTypes.Armor, t => t.Slot = ItemTemplate.ItemSlots.Chest);
                var look = fixture.AddBaseItemTemplate(901, "Shadow Ring", ItemTemplate.UseTypes.Armor, t => t.Slot = ItemTemplate.ItemSlots.Ring);
                var bag = player.Inventory.GetCombineBagContainer();
                bag.SetSlot(1, new ItemSlot { Item = LoadItem(ticket) });
                bag.SetSlot(2, new ItemSlot { Item = LoadItem(stats) });
                bag.SetSlot(3, new ItemSlot { Item = LoadItem(look) });

                Assert.True(fixture.RunCommand(player, "/custom make 255 0 0 255 X"));

                Assert.Contains(player.Sent, s => s.Contains("Items to be customised must be equipment and must be visible items."));
                Assert.Equal(823, bag.GetSlot(1)!.Item.TemplateID);
                Assert.NotNull(bag.GetSlot(2));
                Assert.NotNull(bag.GetSlot(3));
            }
        }

        [Theory]
        [InlineData(0, 0, 0, 0)]
        [InlineData(255, 255, 255, 255)]
        public void ParseRGBA_valid_boundaries_return_null(int r, int g, int b, int a)
        {
            Assert.Null(CustomItem.ParseRGBA(r, g, b, a));
        }

        [Theory]
        [InlineData(-1, 0, 0, 0, "/custom: invalid r value")]
        [InlineData(256, 0, 0, 0, "/custom: invalid r value")]
        [InlineData(0, -1, 0, 0, "/custom: invalid g value")]
        [InlineData(0, 256, 0, 0, "/custom: invalid g value")]
        [InlineData(0, 0, -1, 0, "/custom: invalid b value")]
        [InlineData(0, 0, 256, 0, "/custom: invalid b value")]
        [InlineData(0, 0, 0, -1, "/custom: invalid a value")]
        public void ParseRGBA_out_of_range_channels_return_error(int r, int g, int b, int a, string expected)
        {
            Assert.Equal(expected, CustomItem.ParseRGBA(r, g, b, a));
        }

        [Fact]
        public void ParseRGBA_max_alpha_limits_alpha_channel()
        {
            Assert.Null(CustomItem.ParseRGBA(10, 20, 30, 200, maxAlpha: 200));
            Assert.Equal("/custom: invalid a value", CustomItem.ParseRGBA(10, 20, 30, 201, maxAlpha: 200));
        }

        [Fact]
        public void ParseRGBA_default_max_alpha_allows_255()
        {
            Assert.Null(CustomItem.ParseRGBA(10, 20, 30, 255));
            Assert.Equal("/custom: invalid a value", CustomItem.ParseRGBA(10, 20, 30, 256));
        }

        [Fact]
        public void SanitizeName_removes_packet_delimiters_and_control_characters()
        {
            Assert.Equal("My Sword", CustomItem.SanitizeName("  My Sword  "));
            Assert.Equal("abcdef", CustomItem.SanitizeName("a,b|c\u0001d\ne\tf"));
        }

        [Fact]
        public void SanitizeName_truncates_to_255()
        {
            var result = CustomItem.SanitizeName(new string('x', 300));
            Assert.Equal(255, result!.Length);
            Assert.Equal(new string('x', 255), result);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(",,")]
        public void SanitizeName_returns_null_when_empty(string raw)
        {
            Assert.Null(CustomItem.SanitizeName(raw));
        }

        [Fact]
        public void BuildCustomItem_copies_stats_from_stats_item_and_look_from_look_item()
        {
            var (fixture, _) = WorldAndPlayer();
            using (fixture)
            {
                var statsTemplate = fixture.AddBaseItemTemplate(900, "Steel Sword", ItemTemplate.UseTypes.Weapon, t =>
                {
                    t.Slot = ItemTemplate.ItemSlots.OneHanded;
                    t.BodyState = 1;
                    t.WeaponDamage = 42;
                    t.ScriptParams = "stats-params";
                });
                var lookTemplate = fixture.AddBaseItemTemplate(901, "Shadow Sword", ItemTemplate.UseTypes.Weapon, t =>
                {
                    t.Slot = ItemTemplate.ItemSlots.OneHanded;
                    t.BodyState = 2;
                    t.GraphicEquipped = 7;
                    t.GraphicTile = 8;
                    t.GraphicFile = 9;
                });

                var statsItem = LoadItem(statsTemplate);
                statsItem.StatMultiplier = 1.5;
                statsItem.BaseStats = new AttributeSet { Strength = 10 };
                statsItem.TotalStats = new AttributeSet { Stamina = 20 };
                statsItem.IsBound = true;
                statsItem.ItemProperties[ItemProperty.TitleId] = 111;
                statsItem.ItemProperties[ItemProperty.SurnameId] = 222;

                var lookItem = LoadItem(lookTemplate);

                Item item = CustomItem.BuildCustomItem(statsItem, lookItem, 10, 20, 30, 40, "My Custom", "Tester")!;

                Assert.Equal(900, item.TemplateID);
                Assert.Equal("My Custom", item.Name);
                Assert.Equal("Custom created by Tester", item.Description);
                Assert.True(item.Custom);

                Assert.Equal(1.5, item.StatMultiplier);
                Assert.Equal(10, item.BaseStats.Strength);
                Assert.Equal(20, item.TotalStats.Stamina);
                Assert.Equal(42, item.TotalWeaponDamage);
                Assert.True(item.IsBound);
                Assert.Equal("stats-params", item.ScriptParams);
                Assert.Equal(111, item.ItemProperties[ItemProperty.TitleId]);
                Assert.Equal(222, item.ItemProperties[ItemProperty.SurnameId]);

                Assert.Equal(2, item.BodyState);
                Assert.Equal(7, item.GraphicEquipped);
                Assert.Equal(8, item.GraphicTile);
                Assert.Equal(9, item.GraphicFile);
                Assert.Equal(10, item.GraphicR);
                Assert.Equal(20, item.GraphicG);
                Assert.Equal(30, item.GraphicB);
                Assert.Equal(40, item.GraphicA);
            }
        }

        [Fact]
        public void BuildCustomItem_does_not_copy_title_or_surname_when_absent()
        {
            var (fixture, _) = WorldAndPlayer();
            using (fixture)
            {
                var statsTemplate = fixture.AddBaseItemTemplate(900, "Steel Sword", ItemTemplate.UseTypes.Weapon);
                var lookTemplate = fixture.AddBaseItemTemplate(901, "Shadow Sword", ItemTemplate.UseTypes.Weapon);

                Item item = CustomItem.BuildCustomItem(LoadItem(statsTemplate), LoadItem(lookTemplate), 0, 0, 0, 255, "X", "Tester")!;

                Assert.False(item.ItemProperties.ContainsKey(ItemProperty.TitleId));
                Assert.False(item.ItemProperties.ContainsKey(ItemProperty.SurnameId));
            }
        }

        [Fact]
        public void BuildCustomItem_stat_clones_are_independent_of_source()
        {
            var (fixture, _) = WorldAndPlayer();
            using (fixture)
            {
                var statsTemplate = fixture.AddBaseItemTemplate(900, "Steel Sword", ItemTemplate.UseTypes.Weapon);
                var lookTemplate = fixture.AddBaseItemTemplate(901, "Shadow Sword", ItemTemplate.UseTypes.Weapon);

                var statsItem = LoadItem(statsTemplate);
                statsItem.BaseStats = new AttributeSet { Strength = 10 };

                Item item = CustomItem.BuildCustomItem(statsItem, LoadItem(lookTemplate), 0, 0, 0, 255, "X", "Tester")!;

                statsItem.BaseStats.Strength = 999;

                Assert.Equal(10, item.BaseStats.Strength);
            }
        }

        [Fact]
        public void Make_with_invalid_stats_template_aborts_silently_without_consuming_items()
        {
            var (fixture, player) = WorldAndPlayer();
            using (fixture)
            {
                var ticket = fixture.AddBaseItemTemplate(823, "Custom Ticket", ItemTemplate.UseTypes.Armor);
                var invalidTemplate = new ItemTemplate
                {
                    ID = 950,
                    Name = "   ",
                    UseType = ItemTemplate.UseTypes.Armor,
                    Slot = ItemTemplate.ItemSlots.Chest,
                    BaseStats = new AttributeSet(),
                };

                Item InvalidItem()
                {
                    var item = new Item();
                    item.Template = invalidTemplate;
                    item.TemplateID = invalidTemplate.ID;
                    item.BaseStats = new AttributeSet();
                    item.TotalStats = new AttributeSet();
                    return item;
                }

                var bag = player.Inventory.GetCombineBagContainer();
                bag.SetSlot(1, new ItemSlot { Item = LoadItem(ticket) });
                bag.SetSlot(2, new ItemSlot { Item = InvalidItem() });
                bag.SetSlot(3, new ItemSlot { Item = InvalidItem() });

                Assert.True(fixture.RunCommand(player, "/custom make 255 0 0 255 X"));

                Assert.Equal(823, bag.GetSlot(1)!.Item.TemplateID);
                Assert.Equal(950, bag.GetSlot(2)!.Item.TemplateID);
                Assert.Equal(950, bag.GetSlot(3)!.Item.TemplateID);
            }
        }
    }
}
