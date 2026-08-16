using System.Linq;
using Goose;
using Goose.Events;
using Goose.Tests.Collections;
using Goose.Tests.Fixtures;
using Xunit;

namespace Goose.Tests;

[Collection(GameWorldSettingsCollection.Name)]
public class DimensionVendorStockTests
{
    private const int Offset = 100000;   // must match DimensionConstants.Offset

    private const int SwordId = 50;      // cloned: gear, priced in spirit
    private const int PotionId = 51;     // cloned: a consumable, still gold at the till
    private const int MerchantId = 60;

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

        var merchant = new NPCTemplate
        {
            NPCTemplateID = MerchantId, Name = "Merchant", Level = 1, ClassID = 3,
            NPCType = NPCTemplate.Types.Vendor,
            CanBeKilled = false, CanMove = false,
            AttackSpeed = 1m, MoveSpeed = 1m,
            AlliesString = "", Allies = new List<NPCTemplate>(), Drops = new List<NPCDropInfo>(),
            BaseStats = new AttributeSet { HP = 100 },
        };
        // NPCHandler.LoadNPCs sizes the array VendorSlotSize + 1 and leaves index 0 null
        // (NPCHandler.cs:183-197). Mirror that, so the repoint pass sees a realistic array.
        merchant.VendorItems = new NPCVendorSlot[GameWorld.Settings.VendorSlotSize + 1];
        merchant.VendorItems[1] = new NPCVendorSlot
        {
            Slot = 1, ItemTemplate = fixture.World.ItemHandler.GetTemplate(SwordId),
            Stack = 1, CanSeeStats = true,
        };
        merchant.VendorItems[2] = new NPCVendorSlot
        {
            Slot = 2, ItemTemplate = fixture.World.ItemHandler.GetTemplate(PotionId),
            Stack = 5, CanSeeStats = false,
        };
        fixture.World.NPCHandler.AddTemplate(merchant);
        fixture.World.NPCHandler.SpawnNPC(fixture.World, 1, 20, 20, merchant, shouldRespawn: false);

        fixture.CompileShipped().Object.OnLoaded(fixture.World);
        return fixture;
    }

    private static NPCVendorSlot[] StockOf(GlobalScriptFixture fixture, int dim)
        => fixture.World.NPCHandler.GetNPCTemplate(MerchantId + Offset * dim).VendorItems;

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

        var basic = fixture.World.NPCHandler.GetNPCTemplate(MerchantId).VendorItems;

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

    /// <summary>Resolution puts the item override above the vendor
    /// (CurrencyHandler.cs:41-52), so repointed gear prices in spirit with no vendor change,
    /// and an unrepointed consumable in the same window still prices in gold.</summary>
    [Fact]
    public void Repointed_stock_resolves_to_spirit_and_base_stock_stays_gold()
    {
        using var fixture = Loaded();
        var handler = fixture.World.CurrencyHandler;
        var vendor = fixture.World.MapHandler.GetMap(1 + Offset * 3).NPCs
            .Single(n => n.NPCTemplateID == MerchantId + Offset * 3);

        Assert.Equal("spirit", handler.Resolve(StockOf(fixture, 3)[1].ItemTemplate, vendor).Id);
        // The potion slot was never repointed (no clone exists), so it still prices gold
        // at the very same vendor.
        Assert.Equal(Currency.Gold,
            handler.Resolve(StockOf(fixture, 3)[2].ItemTemplate, vendor).Id);
        Assert.Equal(Currency.Gold,
            handler.Resolve(fixture.World.ItemHandler.GetTemplate(SwordId), vendor).Id);
    }

    /// <summary>Resolve agreeing is not the same as the till charging. This buys from the
    /// spawned dimension-3 merchant through the real VendorPurchaseInventoryEvent and
    /// asserts the spirit wallet moved, the gold wallet did not, and the dimension clone
    /// landed in the inventory.</summary>
    [Fact]
    public void An_actual_purchase_from_a_spawned_dimension_vendor_charges_spirit()
    {
        using var fixture = Loaded();
        var map = fixture.World.MapHandler.GetMap(1 + Offset * 3);
        var vendor = map.NPCs.Single(n => n.NPCTemplateID == MerchantId + Offset * 3);

        var player = fixture.CommandPlayerOn(map, 21, 20);
        // DimensionItem.CanPickup refuses an item above the player's unlocked dimension
        // (CanPickup in DimensionItem.csx), and a purchase is a pickup.
        player.Properties["dimension.max"] = 6;
        player.Gold = 5_000_000;
        fixture.World.CurrencyHandler.Get("spirit").Add(player, 100_000, fixture.World);
        player.Windows.Add(new Window { Type = Window.WindowTypes.Vendor, NPC = vendor });
        player.Sent.Clear();

        var spiritBefore = fixture.World.CurrencyHandler.Get("spirit").GetBalance(player);
        var goldBefore = player.Gold;

        // VendorFixture.Purchase's packet shape (VendorPurchaseCurrencyTests.cs:11-19).
        new VendorPurchaseInventoryEvent
        {
            Player = player,
            Data = "VPI" + vendor.LoginID + ",1",
        }.Ready(fixture.World);

        var clonePrice = fixture.World.ItemHandler.GetTemplate(SwordId + Offset * 3).Value;

        Assert.Equal(spiritBefore - clonePrice,
                     fixture.World.CurrencyHandler.Get("spirit").GetBalance(player));
        Assert.Equal(goldBefore, player.Gold);
        Assert.Equal(SwordId + Offset * 3, player.Inventory.GetSlot(1).Item.TemplateID);
        Assert.Contains(player.Sent, m => m.Contains("spirit"));
    }
}
