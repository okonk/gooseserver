using System.Linq;
using Goose;
using Goose.Events;
using Goose.IntegrationTests.Fixtures;
using Xunit;

namespace Goose.IntegrationTests;

public class DimensionVendorStockTests
{
    private const int Offset = 100000;   // must match DimensionConstants.Offset

    private const int SwordId = 50;      // cloned: ordinary gold gear
    private const int PotionId = 51;     // cloned: a consumable, still gold at the till
    private const int SpiritSwordId = 53;   // explicitly spirit-marked, keeps spirit everywhere
    private const int MerchantId = 60;
    private const int CreditSwordId = 52;   // credit-only gear: value 0, credits 500
    private const int CreditDealerId = 61;

    /// <summary>A base map with a vendor NPC standing on it, stocking a weapon, a
    /// consumable, and one item that has no clone. OnLoaded then clones the world.</summary>
    private static GlobalScriptFixture Loaded()
    {
        var fixture = new GlobalScriptFixture();
        fixture.AddBaseMap(1, "Town", width: 100, height: 100);

        fixture.AddBaseItemTemplate(SwordId, "Sword", ItemTemplate.UseTypes.Weapon, t => t.Value = 100);
        fixture.AddBaseItemTemplate(PotionId, "Potion", ItemTemplate.UseTypes.OneTime, t =>
        {
            t.Value = 10;
            t.StackSize = 20;
        });
        fixture.AddBaseItemTemplate(SpiritSwordId, "Spirit Sword", ItemTemplate.UseTypes.Weapon, t =>
        {
            t.Value = 40;
            t.CurrencyId = "spirit";
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
        // NPCHandler.LoadNPCs sizes the array VendorSlotSize + 1 and leaves index 0 null
        // (NPCHandler.cs:183-197). Mirror that, so the repoint pass sees a realistic array.
        merchant.VendorItems = new NPCVendorSlot[fixture.Settings.VendorSlotSize + 1];
        merchant.VendorItems[1] = new NPCVendorSlot
        {
            Slot = 1, ItemTemplate = fixture.World.ItemHandler.GetTemplate(SwordId)!,
            Stack = 1, CanSeeStats = true,
        };
        merchant.VendorItems[2] = new NPCVendorSlot
        {
            Slot = 2, ItemTemplate = fixture.World.ItemHandler.GetTemplate(PotionId)!,
            Stack = 5, CanSeeStats = false,
        };
        merchant.VendorItems[3] = new NPCVendorSlot
        {
            Slot = 3, ItemTemplate = fixture.World.ItemHandler.GetTemplate(SpiritSwordId)!,
            Stack = 1, CanSeeStats = true,
        };
        fixture.World.NPCHandler.AddTemplate(merchant);
        fixture.World.NPCHandler.SpawnNPC(fixture.World, 1, 20, 20, merchant, shouldRespawn: false);

        fixture.CompileShipped().Object.OnLoaded(fixture.World);
        return fixture;
    }

    private static NPCVendorSlot[] StockOf(GlobalScriptFixture fixture, int dim)
        => fixture.World.NPCHandler.GetNPCTemplate(MerchantId + Offset * dim)!.VendorItems!;

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(6)]
    public void Dimension_vendor_slots_point_at_same_dimension_clones(int dim)
    {
        using var fixture = Loaded();

        var stock = StockOf(fixture, dim);

        Assert.Equal(SwordId + Offset * dim, stock[1].ItemTemplate.ID);
        // The OneTime potion is not clone-eligible (ShouldClone (Items.csx) —
        // Armor/Weapon/Scroll-with-spell only), so its slot keeps the base template and
        // stays gold-priced.
        Assert.Equal(PotionId, stock[2].ItemTemplate.ID);
        Assert.Equal(SpiritSwordId + Offset * dim, stock[3].ItemTemplate.ID);
    }

    /// <summary>A slot whose template the clone pass skips must keep the base template
    /// rather than becoming a null hole or a stale clone: the pass's `?? slot.ItemTemplate`
    /// fallback keeps the exact same template object the base shop shows.</summary>
    [Fact]
    public void Slots_with_no_clone_keep_the_base_template()
    {
        using var fixture = Loaded();

        var stock = StockOf(fixture, 2);

        // The OneTime potion in slot 2 is the deliberately-skipped case.
        Assert.Same(fixture.World.ItemHandler.GetTemplate(PotionId), stock[2].ItemTemplate);
        Assert.Equal(PotionId, stock[2].ItemTemplate.ID);
    }

    /// <summary>The trap. NPCTemplate's copy constructor does VendorItems = other.VendorItems
    /// (NPCTemplate.cs:254) — the array AND its slot objects are shared with the base
    /// template, so an in-place edit rewrites dimension 0's shops.</summary>
    [Fact]
    public void Dimension_zero_vendor_stock_is_untouched()
    {
        using var fixture = Loaded();

        var basic = fixture.World.NPCHandler.GetNPCTemplate(MerchantId)!.VendorItems!;

        Assert.Equal(SwordId, basic[1].ItemTemplate.ID);
        Assert.Equal(PotionId, basic[2].ItemTemplate.ID);

        for (int dim = 1; dim <= 6; dim++)
        {
            var stock = StockOf(fixture, dim);
            Assert.NotSame(basic, stock);                       // new array
            Assert.NotSame(basic[1], stock[1]);                 // and new slot objects
        }
    }

    [Fact]
    public void Slot_stack_and_can_see_stats_survive_the_repoint()
    {
        using var fixture = Loaded();

        var stock = StockOf(fixture, 4);

        Assert.Equal(1, stock[1].Slot);
        Assert.Equal(1, stock[1].Stack);
        Assert.True(stock[1].CanSeeStats);
        Assert.Equal(2, stock[2].Slot);
        Assert.Equal(5, stock[2].Stack);
        Assert.False(stock[2].CanSeeStats);
    }

    /// <summary>A credit dealer stocking credit-only gear. A credit item placed on an
    /// ordinary gold merchant is not intended dimension behaviour, so the fixture keeps the
    /// valid pairing only; the clone assertions still cover the credit sword itself.</summary>
    private static GlobalScriptFixture LoadedCreditDealer()
    {
        var fixture = new GlobalScriptFixture();
        fixture.AddBaseMap(1, "Town", width: 100, height: 100);

        fixture.AddBaseItemTemplate(CreditSwordId, "Credit Sword", ItemTemplate.UseTypes.Weapon, t =>
        {
            t.Value = 0;
            t.Credits = 500;
        });

        var dealer = new NPCTemplate
        {
            NPCTemplateID = CreditDealerId, Name = "Credit Dealer", Level = 1, ClassID = 3,
            NPCType = NPCTemplate.Types.Vendor,
            CanBeKilled = false, CanMove = false,
            CreditDealer = true, CurrencyId = Currency.Credits,
            AttackSpeed = 1.0, MoveSpeed = 1.0,
            AlliesString = "", Allies = new List<NPCTemplate>(), Drops = new List<NPCDropInfo>(),
            BaseStats = new AttributeSet { HP = 100 },
        };
        dealer.VendorItems = new NPCVendorSlot[fixture.Settings.VendorSlotSize + 1];
        dealer.VendorItems[1] = new NPCVendorSlot
        {
            Slot = 1, ItemTemplate = fixture.World.ItemHandler.GetTemplate(CreditSwordId)!,
            Stack = 1, CanSeeStats = true,
        };
        fixture.World.NPCHandler.AddTemplate(dealer);
        fixture.World.NPCHandler.SpawnNPC(fixture.World, 1, 25, 20, dealer, shouldRespawn: false);

        fixture.CompileShipped().Object.OnLoaded(fixture.World);
        return fixture;
    }

    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    public void Credit_only_gear_clone_keeps_zero_value_and_scales_credits_without_a_currency_override(int dim)
    {
        using var fixture = LoadedCreditDealer();

        var clone = fixture.World.ItemHandler.GetTemplate(CreditSwordId + Offset * dim)!;

        Assert.Null(clone.CurrencyId);
        Assert.Equal(0L, clone.Value);
        Assert.Equal((int)(500L * (long)Math.Pow(3, dim)), clone.Credits);
    }

    [Fact]
    public void Buying_credit_only_gear_from_a_dimension_credit_dealer_costs_scaled_credits()
    {
        using var fixture = LoadedCreditDealer();
        int dim = 3;
        var map = fixture.World.MapHandler.GetMap(1 + Offset * dim)!;
        var vendor = map.NPCs.Single(n => n.NPCTemplateID == CreditDealerId + Offset * dim);

        Assert.Equal(CreditSwordId + Offset * dim, vendor.VendorItems![1].ItemTemplate.ID);

        var player = fixture.CommandPlayerOn(map, 26, 20);
        player.Properties["dimension.max"] = 6;
        player.Credits = 1_000_000;
        player.Windows.Add(new Window { Type = Window.WindowTypes.Vendor, NPC = vendor });
        player.Sent.Clear();

        var creditsBefore = player.Credits;
        var goldBefore = player.Gold;

        new VendorPurchaseInventoryEvent
        {
            Player = player,
            Data = "VPI" + vendor.LoginID + ",1",
        }.Ready(fixture.World);

        var price = 500L * (long)Math.Pow(3, dim);

        Assert.Equal(creditsBefore - price, player.Credits);
        Assert.Equal(goldBefore, player.Gold);
        Assert.Equal(CreditSwordId + Offset * dim, player.Inventory.GetSlot(1)!.Item.TemplateID);
    }

    /// <summary>Resolution puts the item override above the vendor
    /// (CurrencyHandler.cs:41-52), so an explicitly spirit-marked clone keeps spirit at an
    /// ordinary dimension vendor, while the same vendor's ordinary gear prices in gold.</summary>
    [Fact]
    public void An_explicit_item_currency_wins_over_the_vendor_currency_in_dimensions()
    {
        using var fixture = Loaded();
        var handler = fixture.World.CurrencyHandler;
        var vendor = fixture.World.MapHandler.GetMap(1 + Offset * 3)!.NPCs
            .Single(n => n.NPCTemplateID == MerchantId + Offset * 3);

        Assert.Equal("spirit", handler.Resolve(StockOf(fixture, 3)[3].ItemTemplate, vendor).Id);
        Assert.Equal(Currency.Gold,
            handler.Resolve(StockOf(fixture, 3)[1].ItemTemplate, vendor).Id);
        // The potion slot was never repointed (no clone exists), so it still prices gold
        // at the very same vendor.
        Assert.Equal(Currency.Gold,
            handler.Resolve(StockOf(fixture, 3)[2].ItemTemplate, vendor).Id);
        Assert.Equal(Currency.Gold,
            handler.Resolve(fixture.World.ItemHandler.GetTemplate(SwordId), vendor).Id);
    }

    [Fact]
    public void Base_vendor_purchase_charges_the_base_gold_price_and_delivers_the_base_template()
    {
        using var fixture = Loaded();
        var map = fixture.World.MapHandler.GetMap(1)!;
        var vendor = map.NPCs.Single(n => n.NPCTemplateID == MerchantId);

        var player = fixture.CommandPlayerOn(map, 21, 20);
        player.Gold = 10_000;
        player.Windows.Add(new Window { Type = Window.WindowTypes.Vendor, NPC = vendor });
        player.Sent.Clear();

        new VendorPurchaseInventoryEvent
        {
            Player = player,
            Data = "VPI" + vendor.LoginID + ",1",
        }.Ready(fixture.World);

        Assert.Equal(10_000L - 100L, player.Gold);
        Assert.Equal(SwordId, player.Inventory.GetSlot(1)!.Item.TemplateID);
    }

    [Fact]
    public void An_actual_purchase_from_a_spawned_dimension_vendor_charges_scaled_gold()
    {
        using var fixture = Loaded();
        var map = fixture.World.MapHandler.GetMap(1 + Offset * 3)!;
        var vendor = map.NPCs.Single(n => n.NPCTemplateID == MerchantId + Offset * 3);

        var player = fixture.CommandPlayerOn(map, 21, 20);
        player.Properties["dimension.max"] = 6;
        player.Gold = 5_000_000;
        var spirit = fixture.World.CurrencyHandler.Get("spirit")!;
        spirit.Add(player, 100_000, fixture.World);
        player.Windows.Add(new Window { Type = Window.WindowTypes.Vendor, NPC = vendor });
        player.Sent.Clear();

        var spiritBefore = spirit.GetBalance(player);
        var goldBefore = player.Gold;

        // VendorFixture.Purchase's packet shape (VendorPurchaseCurrencyTests.cs:11-19).
        new VendorPurchaseInventoryEvent
        {
            Player = player,
            Data = "VPI" + vendor.LoginID + ",1",
        }.Ready(fixture.World);

        var clonePrice = fixture.World.ItemHandler.GetTemplate(SwordId + Offset * 3)!.Value;
        Assert.Equal(2700L, clonePrice);

        Assert.Equal(goldBefore - clonePrice, player.Gold);
        Assert.Equal(spiritBefore, spirit.GetBalance(player));
        Assert.Equal(SwordId + Offset * 3, player.Inventory.GetSlot(1)!.Item.TemplateID);
        Assert.Contains(player.Sent, m => m.Contains("gold"));
    }

    [Fact]
    public void Selling_a_dimension_item_to_a_base_vendor_and_a_dimension_vendor_pays_the_same_gold()
    {
        using var fixture = Loaded();
        int dim = 3;
        var clone = fixture.World.ItemHandler.GetTemplate(SwordId + Offset * dim)!;
        var expected = clone.Value / 2;
        var spirit = fixture.World.CurrencyHandler.Get("spirit")!;

        var baseMap = fixture.World.MapHandler.GetMap(1)!;
        var baseVendor = baseMap.NPCs.Single(n => n.NPCTemplateID == MerchantId);
        var baseSeller = fixture.CommandPlayerOn(baseMap, 21, 20);
        var baseItem = new Item();
        baseItem.LoadFromTemplate(clone);
        baseSeller.Inventory.SetSlot(1, new ItemSlot { Item = baseItem, Stack = 1 });
        baseSeller.Windows.Add(new Window { Type = Window.WindowTypes.Vendor, NPC = baseVendor });
        baseSeller.Sent.Clear();

        var goldBefore = baseSeller.Gold;
        new VendorSellInventoryEvent
        {
            Player = baseSeller,
            Data = "VSI" + baseVendor.LoginID + ",1,1",
        }.Ready(fixture.World);

        Assert.Equal(goldBefore + expected, baseSeller.Gold);
        Assert.Equal(0, spirit.GetBalance(baseSeller));
        Assert.Null(baseSeller.Inventory.GetSlot(1));

        var dimMap = fixture.World.MapHandler.GetMap(1 + Offset * dim)!;
        var dimVendor = dimMap.NPCs.Single(n => n.NPCTemplateID == MerchantId + Offset * dim);
        var dimSeller = fixture.CommandPlayerOn(dimMap, 21, 20);
        var dimItem = new Item();
        dimItem.LoadFromTemplate(clone);
        dimSeller.Inventory.SetSlot(1, new ItemSlot { Item = dimItem, Stack = 1 });
        dimSeller.Windows.Add(new Window { Type = Window.WindowTypes.Vendor, NPC = dimVendor });
        dimSeller.Sent.Clear();

        var goldBeforeDim = dimSeller.Gold;
        new VendorSellInventoryEvent
        {
            Player = dimSeller,
            Data = "VSI" + dimVendor.LoginID + ",1,1",
        }.Ready(fixture.World);

        Assert.Equal(goldBeforeDim + expected, dimSeller.Gold);
        Assert.Equal(0, spirit.GetBalance(dimSeller));
        Assert.Null(dimSeller.Inventory.GetSlot(1));
    }
}
