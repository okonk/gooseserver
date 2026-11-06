using Goose;
using Goose.Events;
using Xunit;

namespace Goose.IntegrationTests;

public class CurrencyDataLoadTests : PlayerFirstSaveTestBase
{
    public CurrencyDataLoadTests()
        : base([], ["currencies", "item_templates", "npc_templates", "npc_drops", "npc_vendor_items"]) { }

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
    public void Data_currency_row_registers_and_resolves()
    {
        Insert("INSERT INTO currencies (currency_id, name, short_name, icon_sheet, icon_graphic, vendor_buyback) " +
               "VALUES ('arena_coin', 'Arena Coin', 'ac', 2, 5, 1)");

        world.CurrencyHandler.LoadDataCurrencies(world);

        var currency = world.CurrencyHandler.Get("arena_coin");
        Assert.IsType<DataCurrency>(currency);
        Assert.Equal("Arena Coin", currency!.Name);
        Assert.Equal("ac", currency.ShortName);
        Assert.Equal(new CurrencyIcon(2, 5), currency.Icon);

        var vendor = new NPC
        {
            NPCTemplate = new NPCTemplate { NPCTemplateID = 1, Name = "Vendor", CurrencyId = "arena_coin" },
        };
        Assert.Same(currency, world.CurrencyHandler.Resolve(null, vendor));
    }

    [Fact]
    public void Reload_updates_an_existing_data_currency_in_place()
    {
        Insert("INSERT INTO currencies (currency_id, name) VALUES ('arena_coin', 'Arena Coin')");
        world.CurrencyHandler.LoadDataCurrencies(world);
        var first = world.CurrencyHandler.Get("arena_coin")!;

        Insert("UPDATE currencies SET name='Arena Token' WHERE currency_id='arena_coin'");
        world.CurrencyHandler.LoadDataCurrencies(world);

        Assert.Same(first, world.CurrencyHandler.Get("arena_coin"));
        Assert.Equal("Arena Token", first.Name);
    }

    [Fact]
    public void Data_row_cannot_redefine_gold()
    {
        Insert("INSERT INTO currencies (currency_id, name) VALUES ('gold', 'Not Gold')");

        world.CurrencyHandler.LoadDataCurrencies(world);

        Assert.IsType<GoldCurrency>(world.CurrencyHandler.Get("gold"));
    }

    [Fact]
    public void Missing_currencies_table_is_tolerated()
    {
        Insert("DROP TABLE currencies");

        world.CurrencyHandler.LoadDataCurrencies(world);

        Assert.Null(world.CurrencyHandler.Get("arena_coin"));
    }

    [Fact]
    public void Npc_currency_id_credits_resolves_to_credits()
    {
        Insert("INSERT INTO npc_templates (npc_id, npc_name, currency_id, script_path) " +
               "VALUES (1, 'Credit Dealer', 'credits', '')");

        world.NPCHandler.LoadNPCTemplates(world);

        var npc = world.NPCHandler.GetNPCTemplate(1)!;
        Assert.Equal(Currency.Credits, npc.CurrencyId);
        Assert.IsType<CreditsCurrency>(
            world.CurrencyHandler.Resolve(null, new NPC { NPCTemplate = npc }));
    }

    [Fact]
    public void Npc_currency_id_zero_is_treated_as_none()
    {
        Insert("INSERT INTO npc_templates (npc_id, npc_name, currency_id, script_path) " +
               "VALUES (1, 'Dealer', '0', '')");

        world.NPCHandler.LoadNPCTemplates(world);

        Assert.Null(world.NPCHandler.GetNPCTemplate(1)!.CurrencyId);
    }

    [Fact]
    public void Item_currency_id_wins_at_a_gold_vendor_and_refuses_sale()
    {
        Insert("INSERT INTO currencies (currency_id, name, vendor_buyback) VALUES ('arena_coin', 'Arena Coin', 0)");
        Insert("INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, currency_id) " +
               "VALUES (1, 0, 'Arena Sword', 100, 0, 'arena_coin')");
        Insert("INSERT INTO npc_templates (npc_id, npc_name, script_path) VALUES (1, 'Gold Vendor', '')");

        world.CurrencyHandler.LoadDataCurrencies(world);
        world.ItemHandler.LoadTemplates(world);
        world.NPCHandler.LoadNPCTemplates(world);

        var map = new Map { ID = 1, Name = "Town" };
        world.MapHandler.Maps[map.ID] = map;

        var vendor = new NPC
        {
            LoginID = 900,
            State = NPC.States.Alive,
            Map = map, MapID = map.ID, MapX = 5, MapY = 5,
            NPCTemplate = world.NPCHandler.GetNPCTemplate(1),
        };

        var player = MakePlayer();
        player.Map = map;
        player.MapID = map.ID;
        player.MapX = 5;
        player.MapY = 5;
        player.State = Player.States.Ready;

        var item = new Item();
        item.LoadFromTemplate(world.ItemHandler.GetTemplate(1)!);
        world.ItemHandler.AddAndAssignId(item, world);
        player.Inventory.AddItem(item, 1, world);
        player.Windows.Add(new Window { Type = Window.WindowTypes.Vendor, NPC = vendor });

        var ev = new VendorSellInventoryEvent
        {
            Player = player,
            Data = "VSI900,1,1",
        };
        ev.Ready(world);

        Assert.Equal(0, player.Gold);
        Assert.NotNull(player.Inventory.GetSlot(1));
    }

    [Fact]
    public void Unknown_template_currency_is_cleared_and_counted()
    {
        Insert("INSERT INTO item_templates (item_template_id, item_usetype, item_name, graphic_tile, currency_id) " +
               "VALUES (1, 0, 'Ghost Sword', 0, 'ghost_coin')");
        Insert("INSERT INTO npc_templates (npc_id, npc_name, currency_id, script_path) " +
               "VALUES (1, 'Ghost Vendor', 'ghost_coin', '')");

        world.ItemHandler.LoadTemplates(world);
        world.NPCHandler.LoadNPCTemplates(world);

        int cleared = world.CurrencyHandler.ClearUnknownTemplateCurrencies(world);

        Assert.Equal(2, cleared);
        Assert.Null(world.ItemHandler.GetTemplate(1)!.CurrencyId);
        Assert.Null(world.NPCHandler.GetNPCTemplate(1)!.CurrencyId);
    }
}
