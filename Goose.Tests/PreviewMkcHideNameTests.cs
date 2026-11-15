using Goose;
using Goose.Testing;
using Xunit;

namespace Goose.Tests
{
    public class PreviewMkcHideNameTests
    {
        [Fact]
        public void HairdyePreview_Illutia_SendsHideNameBetweenInvisAndFace()
        {
            using var fixture = new TestWorldFixture(s => s.ServerType = "Illutia");
            var map = fixture.AddBaseMap(1, "Test");
            var player = fixture.CommandPlayerOn(map, 5, 5, "Tester");
            player.FaceID = 71;

            Assert.True(fixture.RunCommand(player, "/hairdye preview 10 20 30 255"));

            var previewMkc = Assert.Single(player.Sent, s => s.StartsWith("MKC9000,"));
            Assert.Contains("Hairdye Preview,", previewMkc);
            Assert.Contains(",0,0," + player.FaceID + ",", previewMkc);
        }

        [Fact]
        public void CustomPreview_Illutia_SendsHideNameBetweenInvisAndFace()
        {
            using var fixture = new TestWorldFixture(s =>
            {
                s.ServerType = "Illutia";
                s.CustomTicketId = 823;
            });
            var map = fixture.AddBaseMap(1, "Test");
            var player = fixture.CommandPlayerOn(map, 5, 5, "Tester");
            player.FaceID = 71;
            player.HairA = 255;

            var ticket = fixture.AddBaseItemTemplate(823, "Custom Ticket", ItemTemplate.UseTypes.Armor);
            var stats = fixture.AddBaseItemTemplate(900, "Steel Sword", ItemTemplate.UseTypes.Weapon);
            var look = fixture.AddBaseItemTemplate(901, "Shadow Sword", ItemTemplate.UseTypes.Weapon);
            var bag = player.Inventory.GetCombineBagContainer();
            bag.SetSlot(1, new ItemSlot { Item = LoadItem(ticket) });
            bag.SetSlot(2, new ItemSlot { Item = LoadItem(stats) });
            bag.SetSlot(3, new ItemSlot { Item = LoadItem(look) });

            Assert.True(fixture.RunCommand(player, "/custom preview 10 20 30 255 Shadow"));

            var previewMkc = Assert.Single(player.Sent, s => s.StartsWith("MKC9000,"));
            Assert.Contains("Custom Preview,", previewMkc);
            Assert.Contains(",0,0," + player.FaceID + ",", previewMkc);
        }

        private static Item LoadItem(ItemTemplate template)
        {
            var item = new Item();
            item.LoadFromTemplate(template);
            return item;
        }
    }
}
