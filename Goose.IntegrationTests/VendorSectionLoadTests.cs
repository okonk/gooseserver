using Goose;
using Xunit;

namespace Goose.IntegrationTests;

public class VendorSectionLoadTests : PlayerFirstSaveTestBase
{
    public VendorSectionLoadTests()
        : base([], ["item_templates", "npc_templates", "npc_drops", "npc_vendor_items"]) { }

    private void Insert(string sql)
    {
        world.Database.Execute(conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        });
    }

    [Fact]
    public void Rows_load_into_plain_stock_and_named_sections_in_first_seen_order()
    {
        for (int id = 1; id <= 4; id++)
            Insert("INSERT INTO item_templates (item_template_id, item_usetype, item_name, graphic_tile) " +
                   $"VALUES ({id}, 0, 'Item {id}', 0)");
        Insert("INSERT INTO npc_templates (npc_id, npc_name, script_path) VALUES (1, 'Arena Shop', '')");
        Insert("INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot, section) VALUES " +
               "(1, 1, 1, 1, 1, ''), (1, 2, 1, 1, 1, 'Enchanting'), (1, 3, 1, 1, 2, 'Enchanting'), (1, 4, 1, 1, 1, 'Head Cosmetics')");

        settings.VendorSlotSize = 30;
        world.ItemHandler.LoadTemplates(world);
        world.NPCHandler.LoadNPCTemplates(world);

        var shop = world.NPCHandler.GetNPCTemplate(1)!;
        Assert.Equal(1, shop.VendorItems![1]!.ItemTemplate.ID);
        Assert.Equal(new[] { "Enchanting", "Head Cosmetics" }, shop.VendorSections!.Select(s => s.Name).ToArray());
        Assert.Equal(2, shop.VendorSections[0].Slots[1]!.ItemTemplate.ID);
        Assert.Equal(3, shop.VendorSections[0].Slots[2]!.ItemTemplate.ID);
        Assert.Equal(4, shop.VendorSections[1].Slots[1]!.ItemTemplate.ID);
    }
}
