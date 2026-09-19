using Goose;
using Goose.Testing;
using Xunit;

namespace Goose.Tests
{
    public class CustomWindowPacketTests
    {
        private static (TestWorldFixture fixture, TestWorldFixture.CapturingPlayer player,
            ItemTemplate ticket, ItemTemplate stats, ItemTemplate look, CustomWindow? window) Setup(
            Action<GooseSettings>? settings = null,
            Action<ItemTemplate>? ticket = null,
            Action<ItemTemplate>? stats = null,
            Action<ItemTemplate>? look = null,
            bool openWindow = true,
            int ticketSlot = 7, int statsSlot = 6, int lookSlot = 5,
            int lookGraphicEquipped = 777, int lookBodyState = 6)
        {
            var fixture = new TestWorldFixture(s =>
            {
                s.CustomTicketId = 823;
                settings?.Invoke(s);
            });
            var map = fixture.AddBaseMap(1, "Test");
            var player = fixture.CommandPlayerOn(map, 5, 5, "Tester");
            var ticketTemplate = fixture.AddBaseItemTemplate(823, "Custom Ticket", ItemTemplate.UseTypes.Armor, ticket);
            var statsTemplate = fixture.AddBaseItemTemplate(900, "Steel Sword", ItemTemplate.UseTypes.Weapon, stats);
            var lookTemplate = fixture.AddBaseItemTemplate(901, "Shadow Sword", ItemTemplate.UseTypes.Weapon, t =>
            {
                t.GraphicEquipped = lookGraphicEquipped;
                t.BodyState = lookBodyState;
                look?.Invoke(t);
            });

            player.Inventory.SetSlot(ticketSlot, new ItemSlot { Item = LoadItem(ticketTemplate) });
            player.Inventory.SetSlot(lookSlot, new ItemSlot { Item = LoadItem(lookTemplate) });
            player.Inventory.SetSlot(statsSlot, new ItemSlot { Item = LoadItem(statsTemplate) });

            CustomWindow? window = openWindow ? new CustomWindow(player, fixture.World, ticketSlot) : null;
            return (fixture, player, ticketTemplate, statsTemplate, lookTemplate, window);
        }

        private static Item LoadItem(ItemTemplate template)
        {
            var item = new Item();
            item.LoadFromTemplate(template);
            return item;
        }

        private static bool SentCWG(TestWorldFixture.CapturingPlayer player)
            => player.Sent.Any(s => s.StartsWith("CWG"));

        private static void AssertNothingConsumed(TestWorldFixture.CapturingPlayer player)
        {
            Assert.Equal(823, player.Inventory.GetSlot(7)!.Item.TemplateID);
            Assert.Equal(901, player.Inventory.GetSlot(5)!.Item.TemplateID);
            Assert.Equal(900, player.Inventory.GetSlot(6)!.Item.TemplateID);
        }

        [Fact]
        public void Cws_valid_look_only_sends_cwg()
        {
            var (fixture, player, _, _, _, _) = Setup();
            using (fixture)
            {
                Assert.True(fixture.RunCommand(player, "CWS5,0"));

                Assert.Contains(player.Sent, s => s.StartsWith("CWG777,6"));
            }
        }

        [Fact]
        public void Cws_valid_stats_only_sends_no_cwg()
        {
            var (fixture, player, _, _, _, _) = Setup();
            using (fixture)
            {
                Assert.True(fixture.RunCommand(player, "CWS0,6"));

                Assert.False(SentCWG(player));
            }
        }

        [Fact]
        public void Cws_both_valid_same_type_sends_cwg_once()
        {
            var (fixture, player, _, _, _, _) = Setup();
            using (fixture)
            {
                Assert.True(fixture.RunCommand(player, "CWS5,6"));

                Assert.Equal(1, player.Sent.Count(s => s.StartsWith("CWG")));
            }
        }

        [Fact]
        public void Cws_both_valid_different_types_sends_message()
        {
            var (fixture, player, _, _, _, _) = Setup(
                stats: t => t.Slot = ItemTemplate.ItemSlots.Chest);
            using (fixture)
            {
                Assert.True(fixture.RunCommand(player, "CWS5,6"));

                Assert.Contains(player.Sent, s => s.Contains("Items to be customised"));
                Assert.False(SentCWG(player));
            }
        }

        [Fact]
        public void Cws_1h_and_2h_weapons_sends_cwg()
        {
            var (fixture, player, _, _, _, _) = Setup(
                stats: t => t.Slot = ItemTemplate.ItemSlots.TwoHanded);
            using (fixture)
            {
                Assert.True(fixture.RunCommand(player, "CWS5,6"));

                Assert.Contains(player.Sent, s => s.StartsWith("CWG777,6"));
            }
        }

        [Fact]
        public void Cws_non_equipment_item_sends_message()
        {
            var (fixture, player, _, _, _, _) = Setup(
                look: t => t.UseType = ItemTemplate.UseTypes.NoUse);
            using (fixture)
            {
                Assert.True(fixture.RunCommand(player, "CWS5,0"));

                Assert.Contains(player.Sent, s => s.Contains("Items to be customised"));
                Assert.False(SentCWG(player));
            }
        }

        [Fact]
        public void Cws_look_equals_stats_sends_message()
        {
            var (fixture, player, _, _, _, _) = Setup();
            using (fixture)
            {
                Assert.True(fixture.RunCommand(player, "CWS5,5"));

                Assert.Contains(player.Sent, s => s.Contains("Items to be customised"));
                Assert.False(SentCWG(player));
            }
        }

        [Fact]
        public void Cws_ticket_slot_id_sends_message()
        {
            var (fixture, player, _, _, _, _) = Setup();
            using (fixture)
            {
                Assert.True(fixture.RunCommand(player, "CWS7,6"));

                Assert.Contains(player.Sent, s => s.Contains("Items to be customised"));
                Assert.False(SentCWG(player));
            }
        }

        [Fact]
        public void Cws_empty_inventory_slot_sends_message()
        {
            var (fixture, player, _, _, _, _) = Setup();
            using (fixture)
            {
                Assert.True(fixture.RunCommand(player, "CWS8,0"));

                Assert.Contains(player.Sent, s => s.Contains("Items missing for customisation"));
                Assert.False(SentCWG(player));
            }
        }

        [Fact]
        public void Cws_no_open_window_sends_nothing()
        {
            var (fixture, player, _, _, _, _) = Setup(openWindow: false);
            using (fixture)
            {
                player.Sent.Clear();
                Assert.True(fixture.RunCommand(player, "CWS5,0"));

                Assert.Empty(player.Sent);
            }
        }

        [Fact]
        public void Cwc_happy_path_creates_item_and_consumes_sources()
        {
            var (fixture, player, _, _, _, window) = Setup();
            using (fixture)
            {
                Assert.True(fixture.RunCommand(player, "CWS5,6"));
                Assert.True(fixture.RunCommand(player, "CWC5,6,10,20,30,40,My Sword"));

                Assert.Null(player.Inventory.GetSlot(5));
                Assert.Null(player.Inventory.GetSlot(6));

                var result = player.Inventory.GetSlot(7)!;
                Assert.Equal("My Sword", result.Item.Name);
                Assert.Equal(900, result.Item.TemplateID);
                Assert.Equal(10, result.Item.GraphicR);
                Assert.Equal(20, result.Item.GraphicG);
                Assert.Equal(30, result.Item.GraphicB);
                Assert.Equal(40, result.Item.GraphicA);
                Assert.Equal(777, result.Item.GraphicEquipped);
                Assert.Equal(6, result.Item.BodyState);
                Assert.Equal("Custom created by Tester", result.Item.Description);

                Assert.Contains(player.Sent, s => s.Contains("Created custom: My Sword"));
                Assert.Contains(player.Sent, s => s.StartsWith("CLW" + window!.ID));
                Assert.Null(CustomWindow.FindOpen(player));

                var logEntry = fixture.World.LogHandler.Pending
                    .FirstOrDefault(l => l.Type == Log.Types.CreatedCustom);
                Assert.NotNull(logEntry);
                Assert.Equal("My Sword (900) 901|10,20,30,40", logEntry!.Text);
                Assert.Equal(result.Item.ItemID, logEntry.OtherID);
            }
        }

        [Fact]
        public void Cwc_alpha_201_refused_and_consumes_nothing()
        {
            var (fixture, player, _, _, _, window) = Setup();
            using (fixture)
            {
                Assert.True(fixture.RunCommand(player, "CWS5,6"));
                Assert.True(fixture.RunCommand(player, "CWC5,6,10,20,30,201,X"));

                Assert.Contains(player.Sent, s => s.Contains("/custom: invalid a value"));
                AssertNothingConsumed(player);
                Assert.Same(window, CustomWindow.FindOpen(player));
            }
        }

        [Fact]
        public void Cwc_alpha_200_succeeds()
        {
            var (fixture, player, _, _, _, _) = Setup();
            using (fixture)
            {
                Assert.True(fixture.RunCommand(player, "CWS5,6"));
                Assert.True(fixture.RunCommand(player, "CWC5,6,10,20,30,200,X"));

                Assert.Null(player.Inventory.GetSlot(5));
                Assert.Null(player.Inventory.GetSlot(6));
                Assert.Equal(200, player.Inventory.GetSlot(7)!.Item.GraphicA);
                Assert.Contains(player.Sent, s => s.Contains("Created custom: X"));
            }
        }

        [Fact]
        public void Cwc_empty_name_refused_and_consumes_nothing()
        {
            var (fixture, player, _, _, _, window) = Setup();
            using (fixture)
            {
                Assert.True(fixture.RunCommand(player, "CWS5,6"));
                Assert.True(fixture.RunCommand(player, "CWC5,6,10,20,30,40,"));

                Assert.Contains(player.Sent, s => s.Contains("Custom name cannot be empty."));
                AssertNothingConsumed(player);
                Assert.Same(window, CustomWindow.FindOpen(player));
            }
        }

        [Fact]
        public void Cwc_name_with_commas_is_stripped()
        {
            var (fixture, player, _, _, _, _) = Setup();
            using (fixture)
            {
                Assert.True(fixture.RunCommand(player, "CWS5,6"));
                Assert.True(fixture.RunCommand(player, "CWC5,6,10,20,30,40,My,Sword"));

                Assert.Equal("MySword", player.Inventory.GetSlot(7)!.Item.Name);
            }
        }

        [Fact]
        public void Cwc_look_moved_after_cws_refused_and_consumes_nothing()
        {
            var (fixture, player, _, _, _, window) = Setup();
            using (fixture)
            {
                Assert.True(fixture.RunCommand(player, "CWS5,6"));
                Assert.True(SentCWG(player));

                var lookItem = player.Inventory.GetSlot(5)!.Item;
                player.Inventory.SetSlot(5, null);
                player.Inventory.SetSlot(9, new ItemSlot { Item = lookItem });

                Assert.True(fixture.RunCommand(player, "CWC5,6,10,20,30,40,X"));

                Assert.Contains(player.Sent, s => s.Contains("Items missing for customisation"));
                Assert.Equal(901, player.Inventory.GetSlot(9)!.Item.TemplateID);
                Assert.Equal(823, player.Inventory.GetSlot(7)!.Item.TemplateID);
                Assert.Equal(900, player.Inventory.GetSlot(6)!.Item.TemplateID);
                Assert.Same(window, CustomWindow.FindOpen(player));
            }
        }

        [Fact]
        public void Cwc_look_equals_stats_refused_and_consumes_nothing()
        {
            var (fixture, player, _, _, _, window) = Setup();
            using (fixture)
            {
                Assert.True(fixture.RunCommand(player, "CWC5,5,10,20,30,40,X"));

                Assert.Contains(player.Sent, s => s.Contains("Items missing for customisation"));
                AssertNothingConsumed(player);
                Assert.Same(window, CustomWindow.FindOpen(player));
            }
        }

        [Fact]
        public void Cwc_without_prior_cws_refused_and_consumes_nothing()
        {
            var (fixture, player, _, _, _, window) = Setup();
            using (fixture)
            {
                Assert.True(fixture.RunCommand(player, "CWC5,6,10,20,30,40,X"));

                Assert.Contains(player.Sent, s => s.Contains("Items changed in the custom window"));
                AssertNothingConsumed(player);
                Assert.Same(window, CustomWindow.FindOpen(player));
            }
        }

        [Fact]
        public void Cwc_item_replaced_in_same_slot_refused_and_consumes_nothing()
        {
            var (fixture, player, _, _, _, window) = Setup();
            using (fixture)
            {
                Assert.True(fixture.RunCommand(player, "CWS5,6"));

                var replacement = LoadItem(fixture.AddBaseItemTemplate(902, "Mystic Sword", ItemTemplate.UseTypes.Weapon));
                player.Inventory.SetSlot(5, new ItemSlot { Item = replacement });

                Assert.True(fixture.RunCommand(player, "CWC5,6,10,20,30,40,X"));

                Assert.Contains(player.Sent, s => s.Contains("Items changed in the custom window"));
                Assert.DoesNotContain(player.Sent, s => s.Contains("Created custom"));
                Assert.Equal(902, player.Inventory.GetSlot(5)!.Item.TemplateID);
                Assert.Equal(900, player.Inventory.GetSlot(6)!.Item.TemplateID);
                Assert.Equal(823, player.Inventory.GetSlot(7)!.Item.TemplateID);
                Assert.Same(window, CustomWindow.FindOpen(player));
            }
        }

        [Fact]
        public void Cwc_slot_ids_differ_from_confirmed_refused_and_consumes_nothing()
        {
            var (fixture, player, _, _, _, window) = Setup();
            using (fixture)
            {
                Assert.True(fixture.RunCommand(player, "CWS5,6"));

                var otherLook = LoadItem(fixture.AddBaseItemTemplate(902, "Mystic Sword", ItemTemplate.UseTypes.Weapon));
                var otherStats = LoadItem(fixture.AddBaseItemTemplate(903, "Iron Sword", ItemTemplate.UseTypes.Weapon));
                player.Inventory.SetSlot(1, new ItemSlot { Item = otherLook });
                player.Inventory.SetSlot(2, new ItemSlot { Item = otherStats });

                Assert.True(fixture.RunCommand(player, "CWC1,2,10,20,30,40,X"));

                Assert.Contains(player.Sent, s => s.Contains("Items changed in the custom window"));
                AssertNothingConsumed(player);
                Assert.Equal(902, player.Inventory.GetSlot(1)!.Item.TemplateID);
                Assert.Equal(903, player.Inventory.GetSlot(2)!.Item.TemplateID);
                Assert.Same(window, CustomWindow.FindOpen(player));
            }
        }

        [Fact]
        public void Cwc_look_equals_ticket_slot_refused_and_consumes_nothing()
        {
            var (fixture, player, _, _, _, window) = Setup();
            using (fixture)
            {
                Assert.True(fixture.RunCommand(player, "CWC7,6,10,20,30,40,X"));

                Assert.Contains(player.Sent, s => s.Contains("Items missing for customisation"));
                AssertNothingConsumed(player);
                Assert.Same(window, CustomWindow.FindOpen(player));
            }
        }

        [Fact]
        public void Cwc_out_of_range_slot_id_refused_and_consumes_nothing()
        {
            var (fixture, player, _, _, _, window) = Setup();
            using (fixture)
            {
                Assert.True(fixture.RunCommand(player, "CWC999,6,10,20,30,40,X"));

                Assert.Contains(player.Sent, s => s.Contains("Items missing for customisation"));
                AssertNothingConsumed(player);
                Assert.Same(window, CustomWindow.FindOpen(player));
            }
        }

        [Fact]
        public void Cws_look_graphic_equipped_zero_sends_cwg_zero()
        {
            var (fixture, player, _, _, _, _) = Setup(lookGraphicEquipped: 0);
            using (fixture)
            {
                Assert.True(fixture.RunCommand(player, "CWS5,0"));

                Assert.Contains(player.Sent, s => s.StartsWith("CWG0,6"));
            }
        }

        [Fact]
        public void Cwc_look_graphic_equipped_zero_succeeds()
        {
            var (fixture, player, _, _, _, _) = Setup(lookGraphicEquipped: 0);
            using (fixture)
            {
                Assert.True(fixture.RunCommand(player, "CWS5,6"));
                Assert.True(fixture.RunCommand(player, "CWC5,6,10,20,30,40,X"));

                Assert.Equal(0, player.Inventory.GetSlot(7)!.Item.GraphicEquipped);
            }
        }

        [Fact]
        public void Cwc_optimistic_capacity_full_inventory_succeeds()
        {
            var (fixture, player, _, _, _, _) = Setup(
                settings: s => s.InventorySize = 5,
                ticketSlot: 1, statsSlot: 3, lookSlot: 2);
            using (fixture)
            {
                var filler = fixture.AddBaseItemTemplate(950, "Filler", ItemTemplate.UseTypes.Armor);
                player.Inventory.SetSlot(4, new ItemSlot { Item = LoadItem(filler) });
                player.Inventory.SetSlot(5, new ItemSlot { Item = LoadItem(filler) });
                Assert.Equal(0, player.Inventory.GetNumberOfFreeSlots());

                Assert.True(fixture.RunCommand(player, "CWS2,3"));
                Assert.True(fixture.RunCommand(player, "CWC2,3,10,20,30,40,X"));

                Assert.Equal("X", player.Inventory.GetSlot(1)!.Item.Name);
                Assert.Equal(900, player.Inventory.GetSlot(1)!.Item.TemplateID);
                Assert.Null(player.Inventory.GetSlot(2));
                Assert.Null(player.Inventory.GetSlot(3));
                Assert.Equal(950, player.Inventory.GetSlot(4)!.Item.TemplateID);
                Assert.Equal(950, player.Inventory.GetSlot(5)!.Item.TemplateID);
            }
        }

        [Fact]
        public void Cwc_ticket_stack_2_keeps_ticket_and_uses_free_slot()
        {
            var (fixture, player, _, _, _, _) = Setup(
                ticket: t => t.StackSize = 10);
            using (fixture)
            {
                var ticketItem = player.Inventory.GetSlot(7)!.Item;
                player.Inventory.SetSlot(7, new ItemSlot { Item = ticketItem, Stack = 2 });

                Assert.True(fixture.RunCommand(player, "CWS5,6"));
                Assert.True(fixture.RunCommand(player, "CWC5,6,10,20,30,40,X"));

                var ticketSlot = player.Inventory.GetSlot(7)!;
                Assert.Equal(823, ticketSlot.Item.TemplateID);
                Assert.Equal(1, ticketSlot.Stack);

                var customSlot = player.Inventory.GetSlot(1)!;
                Assert.Equal("X", customSlot.Item.Name);
                Assert.Equal(900, customSlot.Item.TemplateID);
                Assert.Null(player.Inventory.GetSlot(5));
                Assert.Null(player.Inventory.GetSlot(6));
            }
        }

        [Fact]
        public void Cwc_stacked_ticket_full_inventory_uses_freed_look_slot()
        {
            var (fixture, player, _, _, _, window) = Setup(
                settings: s => s.InventorySize = 5,
                ticket: t => t.StackSize = 10,
                ticketSlot: 1, statsSlot: 3, lookSlot: 2);
            using (fixture)
            {
                var ticketItem = player.Inventory.GetSlot(1)!.Item;
                player.Inventory.SetSlot(1, new ItemSlot { Item = ticketItem, Stack = 2 });
                var filler = fixture.AddBaseItemTemplate(950, "Filler", ItemTemplate.UseTypes.Armor);
                player.Inventory.SetSlot(4, new ItemSlot { Item = LoadItem(filler) });
                player.Inventory.SetSlot(5, new ItemSlot { Item = LoadItem(filler) });
                Assert.Equal(0, player.Inventory.GetNumberOfFreeSlots());

                Assert.True(fixture.RunCommand(player, "CWS2,3"));
                Assert.True(fixture.RunCommand(player, "CWC2,3,10,20,30,40,X"));

                var ticketSlot = player.Inventory.GetSlot(1)!;
                Assert.Equal(823, ticketSlot.Item.TemplateID);
                Assert.Equal(1, ticketSlot.Stack);

                var customSlot = player.Inventory.GetSlot(2)!;
                Assert.Equal("X", customSlot.Item.Name);
                Assert.Equal(900, customSlot.Item.TemplateID);
                Assert.Null(player.Inventory.GetSlot(3));
                Assert.Equal(950, player.Inventory.GetSlot(4)!.Item.TemplateID);
                Assert.Equal(950, player.Inventory.GetSlot(5)!.Item.TemplateID);

                Assert.Contains(player.Sent, s => s.Contains("Created custom: X"));
                Assert.Contains(player.Sent, s => s.StartsWith("CLW" + window!.ID));
                Assert.Null(CustomWindow.FindOpen(player));
            }
        }

        [Fact]
        public void Cwc_full_inventory_all_stacked_refused_and_consumes_nothing()
        {
            var (fixture, player, _, _, _, window) = Setup(
                settings: s => s.InventorySize = 5,
                ticket: t => t.StackSize = 10,
                stats: t => t.StackSize = 10,
                look: t => t.StackSize = 10,
                ticketSlot: 1, statsSlot: 3, lookSlot: 2);
            using (fixture)
            {
                foreach (var slotId in new[] { 1, 2, 3 })
                {
                    var item = player.Inventory.GetSlot(slotId)!.Item;
                    player.Inventory.SetSlot(slotId, new ItemSlot { Item = item, Stack = 2 });
                }
                var filler = fixture.AddBaseItemTemplate(950, "Filler", ItemTemplate.UseTypes.Armor);
                player.Inventory.SetSlot(4, new ItemSlot { Item = LoadItem(filler) });
                player.Inventory.SetSlot(5, new ItemSlot { Item = LoadItem(filler) });
                Assert.Equal(0, player.Inventory.GetNumberOfFreeSlots());

                Assert.True(fixture.RunCommand(player, "CWS2,3"));
                Assert.True(fixture.RunCommand(player, "CWC2,3,10,20,30,40,X"));

                Assert.Contains(player.Sent, s => s.Contains("Not enough inventory space for the custom."));
                Assert.Equal(823, player.Inventory.GetSlot(1)!.Item.TemplateID);
                Assert.Equal(2, player.Inventory.GetSlot(1)!.Stack);
                Assert.Equal(901, player.Inventory.GetSlot(2)!.Item.TemplateID);
                Assert.Equal(2, player.Inventory.GetSlot(2)!.Stack);
                Assert.Equal(900, player.Inventory.GetSlot(3)!.Item.TemplateID);
                Assert.Equal(2, player.Inventory.GetSlot(3)!.Stack);
                Assert.Equal(950, player.Inventory.GetSlot(4)!.Item.TemplateID);
                Assert.Equal(950, player.Inventory.GetSlot(5)!.Item.TemplateID);
                Assert.Same(window, CustomWindow.FindOpen(player));
            }
        }

        [Fact]
        public void Cwc_zero_stack_stats_refused_and_consumes_nothing()
        {
            var (fixture, player, _, _, _, window) = Setup(
                settings: s => s.InventorySize = 5,
                ticketSlot: 1, statsSlot: 3, lookSlot: 2);
            using (fixture)
            {
                var statsItem = player.Inventory.GetSlot(3)!.Item;
                player.Inventory.SetSlot(3, new ItemSlot { Item = statsItem, Stack = 0 });

                Assert.True(fixture.RunCommand(player, "CWS2,3"));
                Assert.True(fixture.RunCommand(player, "CWC2,3,10,20,30,40,X"));

                Assert.Contains(player.Sent, s => s.Contains("Items missing for customisation"));
                Assert.DoesNotContain(player.Sent, s => s.Contains("Created custom"));
                Assert.Equal(901, player.Inventory.GetSlot(2)!.Item.TemplateID);
                Assert.Equal(1, player.Inventory.GetSlot(2)!.Stack);
                Assert.Equal(900, player.Inventory.GetSlot(3)!.Item.TemplateID);
                Assert.Equal(0, player.Inventory.GetSlot(3)!.Stack);
                Assert.Equal(823, player.Inventory.GetSlot(1)!.Item.TemplateID);
                Assert.Equal(1, player.Inventory.GetSlot(1)!.Stack);
                Assert.Same(window, CustomWindow.FindOpen(player));
            }
        }
    }
}
