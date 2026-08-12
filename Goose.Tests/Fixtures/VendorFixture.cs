using System.Collections.Generic;
using Goose;

namespace Goose.Tests.Fixtures;

/// <summary>A player standing at a vendor, wired closely enough to drive the real
/// VendorPurchaseInventoryEvent and VendorSellInventoryEvent.
///
/// Builds on GlobalScriptFixture so GameWorld.Settings (inventory size, etc.) is populated
/// and restored on dispose.</summary>
public sealed class VendorFixture : IDisposable
{
    private readonly GlobalScriptFixture inner = new GlobalScriptFixture();

    /// <summary>Player.Send is virtual and returns early on a null socket (Player.cs:2389),
    /// so overriding it is how tests read the server's messages back.</summary>
    public sealed class CapturingPlayer : Player
    {
        public CapturingPlayer() : base(0) { }
        public List<string> Sent { get; } = new List<string>();
        public override void Send(string data) { this.Sent.Add(data); }
    }

    public GameWorld World => inner.World;
    public Map Map { get; }
    public CapturingPlayer Player { get; }
    public NPC Vendor { get; }

    public VendorFixture()
    {
        Map = inner.AddBaseMap(1, "Town");

        Player = new CapturingPlayer
        {
            Map = Map, MapID = Map.ID, MapX = 5, MapY = 5,
            State = Goose.Player.States.Ready,
        };
        Player.Inventory = new Inventory(Player);

        Vendor = new NPC
        {
            LoginID = 900,
            State = NPC.States.Alive,
            Map = Map, MapX = 5, MapY = 5,
            NPCTemplate = new NPCTemplate { NPCTemplateID = 50, Name = "Merchant" },
        };
        // NPC.VendorItems reads through to NPCTemplate.VendorItems (NPC.cs:335) and has no
        // setter, so the array is sized on the template the way NPCHandler.cs:183 does.
        Vendor.NPCTemplate.VendorItems = new NPCVendorSlot[GameWorld.Settings.VendorSlotSize + 1];

        Player.Windows.Add(new Window { Type = Window.WindowTypes.Vendor, NPC = Vendor });
    }

    /// <summary>Puts a template in a vendor slot so a purchase can name it.</summary>
    public NPCVendorSlot Stock(int slotId, ItemTemplate template, int stack = 1)
    {
        var slot = new NPCVendorSlot { Slot = slotId, ItemTemplate = template, Stack = stack };
        Vendor.VendorItems[slotId] = slot;
        return slot;
    }

    /// <summary>Puts an item in the player's inventory so a sale can name it.</summary>
    public Item Carry(ItemTemplate template, int stack = 1)
    {
        var item = new Item();
        item.LoadFromTemplate(template);
        World.ItemHandler.AddAndAssignId(item, World);
        Player.Inventory.AddItem(item, stack, World);
        return item;
    }

    /// <summary>Marks the vendor as dealing in a currency, the way NPCHandler.cs:105 does
    /// for credit dealers.</summary>
    public void VendorDealsIn(string currencyId) { Vendor.NPCTemplate.CurrencyId = currencyId; }

    public void Dispose() { inner.Dispose(); }
}
