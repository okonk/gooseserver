using System.Linq;
using Goose;
using Goose.Events;
using Goose.IntegrationTests.Fixtures;
using Xunit;

namespace Goose.IntegrationTests;

public class SpiritCurrencyTests
{
    private const int Offset = 100000;   // must match DimensionConstants.Offset

    private const int MerchantId = 60;

    /// <summary>OnLoaded spawns the dimension-0 warden on map 1 (WardenMapId), so every
    /// test that drives OnLoaded needs a base map first.</summary>
    private static void Loaded(GlobalScriptFixture fixture)
    {
        fixture.AddBaseMap(1, "Town", width: 100, height: 100);
        fixture.CompileShipped().Object.OnLoaded(fixture.World);
    }

    private static GlobalScriptFixture LoadedWithVendor(Action<ItemTemplate>? configure)
    {
        var fixture = new GlobalScriptFixture();
        fixture.AddBaseMap(1, "Town", width: 100, height: 100);
        fixture.AddBaseItemTemplate(1, "Sword", ItemTemplate.UseTypes.Weapon, t =>
        {
            t.Value = 100;
            configure?.Invoke(t);
        });

        var merchant = new NPCTemplate
        {
            NPCTemplateID = MerchantId, Name = "Merchant", Level = 1, ClassID = 3,
            NPCType = NPCTemplate.Types.Vendor,
            CanBeKilled = false, CanMove = false,
            AttackSpeed = 1.0, MoveSpeed = 1.0,
            AlliesString = "", Allies = new List<NPCTemplate>(), Drops = new List<NPCDropInfo>(),
            BaseStats = new AttributeSet { HP = 100 },
        };
        merchant.VendorItems = new NPCVendorSlot[fixture.Settings.VendorSlotSize + 1];
        merchant.VendorItems[1] = new NPCVendorSlot
        {
            Slot = 1, ItemTemplate = fixture.World.ItemHandler.GetTemplate(1)!,
            Stack = 1, CanSeeStats = true,
        };
        fixture.World.NPCHandler.AddTemplate(merchant);
        fixture.World.NPCHandler.SpawnNPC(fixture.World, 1, 20, 20, merchant, shouldRespawn: false);

        fixture.CompileShipped().Object.OnLoaded(fixture.World);
        return fixture;
    }

    [Fact]
    public void OnLoaded_RegistersSpirit()
    {
        using var fixture = new GlobalScriptFixture();
        fixture.AddBaseItemTemplate(1, "Sword", ItemTemplate.UseTypes.Weapon, t => t.Value = 100);

        Loaded(fixture);

        Assert.NotNull(fixture.World.CurrencyHandler.Get("spirit"));
    }

    [Fact]
    public void Unmarked_base_item_clone_keeps_no_currency_override_and_scales_value()
    {
        using var fixture = new GlobalScriptFixture();
        fixture.AddBaseItemTemplate(1, "Sword", ItemTemplate.UseTypes.Weapon, t => t.Value = 100);

        Loaded(fixture);

        var clone = fixture.World.ItemHandler.GetTemplate(1 + Offset)!;
        Assert.Null(clone.CurrencyId);
        Assert.Equal(300L, clone.Value);

        var basic = fixture.World.ItemHandler.GetTemplate(1)!;
        Assert.Null(basic.CurrencyId);
        Assert.Equal(100L, basic.Value);
    }

    [Fact]
    public void Spirit_marked_base_item_clone_stays_spirit_and_transactions_move_spirit_not_gold()
    {
        using var fixture = LoadedWithVendor(t => t.CurrencyId = "spirit");
        var spirit = fixture.World.CurrencyHandler.Get("spirit")!;
        var clone = fixture.World.ItemHandler.GetTemplate(1 + Offset)!;

        Assert.Equal("spirit", clone.CurrencyId);

        var map = fixture.World.MapHandler.GetMap(1 + Offset)!;
        var vendor = map.NPCs.Single(n => n.NPCTemplateID == MerchantId + Offset);
        var buyer = fixture.CommandPlayerOn(map, 21, 20);
        buyer.Properties["dimension.max"] = 6;
        buyer.Gold = 1_000_000;
        spirit.Add(buyer, 10_000, fixture.World);
        buyer.Windows.Add(new Window { Type = Window.WindowTypes.Vendor, NPC = vendor });
        buyer.Sent.Clear();

        var spiritBefore = spirit.GetBalance(buyer);
        var goldBefore = buyer.Gold;

        new VendorPurchaseInventoryEvent
        {
            Player = buyer,
            Data = "VPI" + vendor.LoginID + ",1",
        }.Ready(fixture.World);

        Assert.Equal(spiritBefore - clone.Value, spirit.GetBalance(buyer));
        Assert.Equal(goldBefore, buyer.Gold);
        Assert.Equal(1 + Offset, buyer.Inventory.GetSlot(1)!.Item.TemplateID);

        var baseMap = fixture.World.MapHandler.GetMap(1)!;
        var baseVendor = baseMap.NPCs.Single(n => n.NPCTemplateID == MerchantId);
        var seller = fixture.CommandPlayerOn(baseMap, 21, 20);
        var sold = new Item();
        sold.LoadFromTemplate(clone);
        seller.Inventory.SetSlot(1, new ItemSlot { Item = sold, Stack = 1 });
        seller.Windows.Add(new Window { Type = Window.WindowTypes.Vendor, NPC = baseVendor });
        seller.Sent.Clear();

        var spiritBeforeSale = spirit.GetBalance(seller);
        var goldBeforeSale = seller.Gold;

        new VendorSellInventoryEvent
        {
            Player = seller,
            Data = "VSI" + baseVendor.LoginID + ",1,1",
        }.Ready(fixture.World);

        Assert.Equal(spiritBeforeSale + clone.Value / 2, spirit.GetBalance(seller));
        Assert.Equal(goldBeforeSale, seller.Gold);
        Assert.Null(seller.Inventory.GetSlot(1));
    }

    /// <summary>Base templates must keep the default so ordinary vendors keep taking gold.</summary>
    [Fact]
    public void BaseTemplates_KeepTheDefaultCurrency()
    {
        using var fixture = new GlobalScriptFixture();
        fixture.AddBaseItemTemplate(1, "Sword", ItemTemplate.UseTypes.Weapon, t => t.Value = 100);

        Loaded(fixture);

        Assert.Null(fixture.World.ItemHandler.GetTemplate(1)!.CurrencyId);
    }

    [Fact]
    public void Spirit_PricesBuysAtValueAndSellsAtHalf()
    {
        using var fixture = new GlobalScriptFixture();
        fixture.AddBaseItemTemplate(1, "Sword", ItemTemplate.UseTypes.Weapon, t =>
        {
            t.Value = 100;
            t.CurrencyId = "spirit";
        });
        Loaded(fixture);

        var spirit = fixture.World.CurrencyHandler.Get("spirit")!;
        var clone = fixture.World.ItemHandler.GetTemplate(1 + Offset)!;
        var item = new Item();
        item.LoadFromTemplate(clone);

        Assert.Equal(clone.Value * 2, spirit.GetBuyPrice(clone, 2));
        Assert.Equal(clone.Value / 2, spirit.GetSellPrice(item, 1));
    }

    /// <summary>The wallet is BaseStats.SP, which persists as players.player_sp. MaxStats.SP
    /// is separate accounting, so both must move together or MaxSP clamps CurrentSP down.</summary>
    [Fact]
    public void Spirit_AddMovesBothTheBalanceAndTheMaximum()
    {
        using var fixture = new GlobalScriptFixture();
        fixture.AddBaseItemTemplate(1, "Sword", ItemTemplate.UseTypes.Weapon, t => t.Value = 100);
        Loaded(fixture);

        var map = fixture.World.MapHandler.GetMap(1)!;
        var player = fixture.PlayerOn(map, 5, 5);
        var spirit = fixture.World.CurrencyHandler.Get("spirit")!;

        spirit.Add(player, 500, fixture.World);

        Assert.Equal(500, spirit.GetBalance(player));
        Assert.Equal(500, player.MaxStats.SP);
        Assert.Equal(player.MaxSP, player.CurrentSP);
    }

    [Fact]
    public void Spirit_RemoveMovesBothBack()
    {
        using var fixture = new GlobalScriptFixture();
        fixture.AddBaseItemTemplate(1, "Sword", ItemTemplate.UseTypes.Weapon, t => t.Value = 100);
        Loaded(fixture);

        var map = fixture.World.MapHandler.GetMap(1)!;
        var player = fixture.PlayerOn(map, 5, 5);
        var spirit = fixture.World.CurrencyHandler.Get("spirit")!;

        spirit.Add(player, 500, fixture.World);
        spirit.Remove(player, 200, fixture.World);

        Assert.Equal(300, spirit.GetBalance(player));
        Assert.Equal(300, player.MaxStats.SP);
        Assert.Equal(player.MaxSP, player.CurrentSP);
    }

    /// <summary>Gear that grants SP raises MaxStats.SP only, so it cannot inflate the wallet.
    /// Documenting the asymmetry rather than guarding it - see the design doc.</summary>
    [Fact]
    public void Spirit_BalanceIgnoresGearGrantedSP()
    {
        using var fixture = new GlobalScriptFixture();
        fixture.AddBaseItemTemplate(1, "Sword", ItemTemplate.UseTypes.Weapon, t => t.Value = 100);
        Loaded(fixture);

        var map = fixture.World.MapHandler.GetMap(1)!;
        var player = fixture.PlayerOn(map, 5, 5);
        var spirit = fixture.World.CurrencyHandler.Get("spirit")!;

        spirit.Add(player, 100, fixture.World);
        player.AddStats(new AttributeSet { SP = 1000 }, fixture.World);   // as equipping would

        Assert.Equal(100, spirit.GetBalance(player));
    }
}
