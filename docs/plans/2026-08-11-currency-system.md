# Currency System Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Build a generic currency subsystem in the server, rebuild gold and credits on top of it with no observable behaviour change, and register spirit as a script-defined currency that prices dimension items.

**Architecture:** An `ICurrency` interface plus a `CurrencyHandler` registry on `GameWorld`. Currency is selected per transaction by `CurrencyHandler.Resolve(itemTemplate, vendorNpc)` — an item-level override wins, otherwise the vendor's currency, otherwise gold. Both vendor events collapse from three inline currency branches to one resolve-check-charge path. `Dimensions.csx` declares a `SpiritCurrency` backed by `BaseStats.SP` and stamps `CurrencyId = "spirit"` onto dimension item clones.

**Tech Stack:** C# / .NET 10, xUnit, SQLite. Scripts are `.csx` compiled by `ScriptHandler` at load.

**Design doc:** `docs/plans/2026-08-11-currency-system-design.md`

---

## APIs verified

Every cross-file call in this plan, cited from source:

| API | Location | Signature / fact |
|---|---|---|
| Handler construction | `Goose/GameWorld.cs:145-158` | All handlers `new`ed in one block, no DB access |
| Global scripts load last | `Goose/GameWorld.cs:355` | `LoadStep("Global Scripts", ...)` after NPC spawns (`:326`) |
| `OnLoaded` dispatch | `Goose/GameWorld.cs:692` | `script.Object.OnLoaded(this)` |
| `Player.Gold` | `Goose/Player.cs:294` | `public long Gold { get; set; }` |
| `Player.AddGold` | `Goose/Player.cs:1471` | `void AddGold(long amount, GameWorld world)` — adds, sends `StatusInfo`. No bank logic |
| `Player.RemoveGold` | `Goose/Player.cs:1482` | `void RemoveGold(long, GameWorld)` — **silently returns if `amount > Gold`** |
| `Player.Credits` | `Goose/Player.cs:427` | `public int Credits { get; set; }` — int, not long |
| `Player.AddStats` | `Goose/Player.cs:1494` | `void AddStats(AttributeSet, GameWorld, bool updateCharacter = true)` — raises `MaxStats`, clamps currents, sends `StatusInfo` |
| `Player.RemoveStats` | `Goose/Player.cs:1535` | `void RemoveStats(AttributeSet, GameWorld, bool changeCurrentHPMP = true, bool updateCharacter = false)` |
| `Player.CurrentSP` | `Goose/Player.cs:185-192` | Setter clamps to `MaxSP`, so it must be assigned *after* `AddStats` |
| `Player.MaxSP` | `Goose/Player.cs:210-215` | `TemporaryMaxSP ?? MaxStats.SP` |
| `Player.Send` | `Goose/Player.cs:2389` | `public virtual void Send(string data)` — returns early when `sock == null`; **virtual, so tests override it** |
| `GameWorld.Send` | `Goose/GameWorld.cs:585` | Swallows all exceptions; safe for socketless players |
| `Player(int)` ctor | `Goose/Player.cs:476` | Initialises `Windows`, `Buffer`, `Buffs`. Does **not** create `Inventory` |
| `Inventory` ctor | `Goose/Inventory.cs:45` | `public Inventory(Player player)` |
| `Inventory.AddItem` | `Goose/Inventory.cs:78` | `bool AddItem(Item item, long stack, GameWorld world)` |
| `Inventory.GetSlot` | `Goose/Inventory.cs:173` | `ItemSlot GetSlot(int i)` |
| `Inventory.RemoveItem` | `Goose/Inventory.cs:444` | `ItemSlot RemoveItem(Item item, long number, GameWorld world)` |
| `NPCVendorSlot` | `Goose/NPCVendorSlot.cs:8-14` | `Slot`, `ItemTemplate`, `Stack`, `CanSeeStats` |
| `NPC.CreditDealer` | `Goose/NPC.cs:344` | `{ get { return this.NPCTemplate.CreditDealer; } }` — passthrough idiom to copy |
| `ItemTemplate` copy ctor | `Goose/ItemTemplate.cs:120-159` | Copies every field explicitly; `Credits` at `:156` |
| `NPCTemplate` copy ctor | `Goose/NPCTemplate.cs:209-253` | Copies every field; `CreditDealer` at `:245` |
| `credit_dealer` load | `Goose/NPCHandler.cs:105` | `npc.CreditDealer = ("0".Equals(...) ? false : true)` |
| `credits_value` load | `Goose/ItemHandler.cs:128` | `template.Credits = Convert.ToInt32(reader["credits_value"])` |
| `credits_value` default | `Goose/sql/items.sql:46` | `INT DEFAULT 0 NOT NULL` — **why credits cannot be derived from the item** |
| `AttributeSet.SP` | `Goose/AttributeSet.cs:16` | `public int SP { get; set; }` — widened to `long` in Task 5 |
| `AttributeSet` operator+ | `Goose/AttributeSet.cs:105-111` | Returns a new instance, every field summed |
| `Map.RANGE_X` | `Goose/Map.cs:19` | `public static int RANGE_X = 16` |
| `ItemHandler.AddAndAssignId` | `Goose/ItemHandler.cs:230` | `void AddAndAssignId(Item item, GameWorld world)` |
| `ItemHandler.AddTemplate` | `Goose/ItemHandler.cs` | Used by `GlobalScriptFixture.AddBaseItemTemplate` |
| Event loop catches | `Goose/EventHandler.cs:369-377` | `try { ev?.Ready(world); } catch` — a throw is contained to one event |
| `GlobalScriptFixture` | `Goose.Tests/Fixtures/GlobalScriptFixture.cs` | `World`, `CompileShipped()`, `AddBaseItemTemplate(...)`, `PlayerOn(map, x, y)` |
| Dimension clone loop | `Dimensions.csx:610` | `Value = (long)(basic.Value * Math.Pow(3, dim))` — where `CurrencyId` gets stamped |

**Not verified, deliberately:** the live `items` and `npc_templates` table contents. The database is not in the repo. Nothing in this plan depends on row values.

---

## Task 1: The currency contract and registry

**Files:**
- Create: `Goose/Currency/ICurrency.cs`
- Create: `Goose/Currency/Currency.cs`
- Create: `Goose/Currency/CurrencyHandler.cs`
- Modify: `Goose/GameWorld.cs:34-48` (property), `Goose/GameWorld.cs:145-158` (construction)
- Test: `Goose.Tests/CurrencyHandlerTests.cs`

**Step 1: Write the failing test**

```csharp
using Goose;
using Xunit;

namespace Goose.Tests;

public class CurrencyHandlerTests
{
    /// <summary>A minimal ICurrency for registry tests. Real implementations arrive in Task 4.</summary>
    private sealed class StubCurrency : ICurrency
    {
        public StubCurrency(string id) { Id = id; }
        public string Id { get; }
        public string Name => Id;
        public string ShortName => Id;
        public long GetBalance(Player player) => 0;
        public long GetBuyPrice(ItemTemplate template, int stack) => 0;
        public long GetSellPrice(Item item, int stack) => 0;
        public void Add(Player player, long amount, GameWorld world) { }
        public void Remove(Player player, long amount, GameWorld world) { }
    }

    [Fact]
    public void Get_ReturnsARegisteredCurrency()
    {
        var handler = new CurrencyHandler();
        var spirit = new StubCurrency("spirit");

        handler.Register(spirit);

        Assert.Same(spirit, handler.Get("spirit"));
    }

    [Fact]
    public void Get_IsCaseInsensitive()
    {
        var handler = new CurrencyHandler();
        handler.Register(new StubCurrency("spirit"));

        Assert.NotNull(handler.Get("SPIRIT"));
    }

    [Fact]
    public void Get_ReturnsNullForUnknownCurrency()
    {
        Assert.Null(new CurrencyHandler().Get("doubloons"));
    }

    /// <summary>Silently overwriting a currency would repoint every item priced in it at a
    /// different wallet. That must be loud.</summary>
    [Fact]
    public void Register_RejectsADuplicateId()
    {
        var handler = new CurrencyHandler();
        handler.Register(new StubCurrency("spirit"));

        var ex = Assert.Throws<InvalidOperationException>(() => handler.Register(new StubCurrency("spirit")));
        Assert.Contains("spirit", ex.Message);
    }

    [Fact]
    public void Register_RejectsADuplicateIdDifferingOnlyByCase()
    {
        var handler = new CurrencyHandler();
        handler.Register(new StubCurrency("spirit"));

        Assert.Throws<InvalidOperationException>(() => handler.Register(new StubCurrency("Spirit")));
    }

    [Fact]
    public void Register_RejectsAnEmptyId()
    {
        Assert.Throws<ArgumentException>(() => new CurrencyHandler().Register(new StubCurrency("")));
    }
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test --filter FullyQualifiedName~CurrencyHandlerTests`
Expected: FAIL — `error CS0246: The type or namespace name 'ICurrency' could not be found`

**Step 3: Write minimal implementation**

`Goose/Currency/ICurrency.cs`:

```csharp
namespace Goose
{
    /// <summary>A currency a vendor can transact in. Gold and credits ship as built-ins;
    /// scripts register their own (see Scripts/Global/Dimensions.csx for spirit).
    ///
    /// Buy takes an ItemTemplate and sell takes an Item because that is what each vendor
    /// path actually holds — the sell path reads slot.Item.Value, which item modifiers can
    /// move away from the template value.</summary>
    public interface ICurrency
    {
        /// <summary>Registry key, and the value ItemTemplate.CurrencyId references.</summary>
        string Id { get; }

        /// <summary>Interpolated into player-facing messages: "for 500 gold".</summary>
        string Name { get; }

        /// <summary>Interpolated into LogHandler entries: "(500 gp)".</summary>
        string ShortName { get; }

        long GetBalance(Player player);

        /// <summary>Cost to buy. Negative means this item is not purchasable in this currency.</summary>
        long GetBuyPrice(ItemTemplate template, int stack);

        /// <summary>Payout for selling. Negative means the vendor refuses to buy it.</summary>
        long GetSellPrice(Item item, int stack);

        void Add(Player player, long amount, GameWorld world);

        void Remove(Player player, long amount, GameWorld world);
    }
}
```

`Goose/Currency/Currency.cs`:

```csharp
namespace Goose
{
    /// <summary>Ids of the built-in currencies. Script currencies use their own string
    /// literals - nothing here needs to know about them.</summary>
    public static class Currency
    {
        public const string Gold = "gold";
        public const string Credits = "credits";
    }
}
```

`Goose/Currency/CurrencyHandler.cs`:

```csharp
using System;
using System.Collections.Generic;

namespace Goose
{
    /// <summary>Registry of every currency the server knows about. Built-ins are registered
    /// during GameWorld construction; scripts register theirs from OnLoaded, which runs much
    /// later (GameWorld.cs:355), so the ordering is safe by a wide margin.</summary>
    public class CurrencyHandler
    {
        private readonly Dictionary<string, ICurrency> currencies =
            new Dictionary<string, ICurrency>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Throws on a duplicate id. Overwriting would silently repoint every item
        /// priced in that currency at a different wallet.</summary>
        public void Register(ICurrency currency)
        {
            if (currency == null) throw new ArgumentNullException(nameof(currency));
            if (string.IsNullOrWhiteSpace(currency.Id))
                throw new ArgumentException("Currency id must not be empty.", nameof(currency));

            if (this.currencies.ContainsKey(currency.Id))
                throw new InvalidOperationException($"Currency '{currency.Id}' is already registered.");

            this.currencies[currency.Id] = currency;
        }

        /// <summary>The currency with this id, or null if nothing registered it.</summary>
        public ICurrency Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return this.currencies.TryGetValue(id, out var currency) ? currency : null;
        }
    }
}
```

In `Goose/GameWorld.cs`, add the property beside the other handlers (after `:48`):

```csharp
        public CurrencyHandler CurrencyHandler { get; set; }
```

and construct it in the handler block, after `this.ScriptHandler = new ScriptHandler();` at `:158`:

```csharp
            this.CurrencyHandler = new CurrencyHandler();
```

**Step 4: Run test to verify it passes**

Run: `dotnet test --filter FullyQualifiedName~CurrencyHandlerTests`
Expected: PASS — 6 tests

**Step 5: Commit**

```bash
git add Goose/Currency Goose/GameWorld.cs Goose.Tests/CurrencyHandlerTests.cs
git commit -m "Add ICurrency and CurrencyHandler registry"
```

---

## Task 2: Currency carriers on item and NPC templates

Two carriers, because gold and credits are selected differently in this codebase. `credits_value` defaults to `0` (`Goose/sql/items.sql:46`), so `Item.Credits >= 0` is true for essentially every row — credits cannot be derived from the item and are a property of the vendor.

**Files:**
- Modify: `Goose/ItemTemplate.cs` (field + copy ctor at `:156`)
- Modify: `Goose/NPCTemplate.cs` (field + copy ctor at `:245`)
- Modify: `Goose/NPC.cs:344` (passthrough)
- Modify: `Goose/NPCHandler.cs:105` (load mapping)
- Test: `Goose.Tests/CurrencyCarrierTests.cs`

**Step 1: Write the failing test**

```csharp
using Goose;
using Xunit;

namespace Goose.Tests;

public class CurrencyCarrierTests
{
    /// <summary>Null, not "gold". An unset item must be able to inherit its vendor's
    /// currency - a credit dealer's stock has no per-item override.</summary>
    [Fact]
    public void ItemTemplate_DefaultsToNullCurrency()
    {
        Assert.Null(new ItemTemplate().CurrencyId);
    }

    /// <summary>The copy constructor is how Dimensions.csx builds every clone. A field it
    /// forgets is a field the clones silently lose.</summary>
    [Fact]
    public void ItemTemplate_CopyConstructorCarriesCurrency()
    {
        var basic = new ItemTemplate { ID = 5, Name = "Sword", BaseStats = new AttributeSet(), CurrencyId = "spirit" };

        Assert.Equal("spirit", new ItemTemplate(basic).CurrencyId);
    }

    [Fact]
    public void NPCTemplate_DefaultsToNullCurrency()
    {
        Assert.Null(new NPCTemplate().CurrencyId);
    }

    [Fact]
    public void NPCTemplate_CopyConstructorCarriesCurrency()
    {
        var basic = new NPCTemplate { NPCTemplateID = 7, Name = "Merchant", CurrencyId = Currency.Credits };

        Assert.Equal(Currency.Credits, new NPCTemplate(basic).CurrencyId);
    }

    [Fact]
    public void NPC_ReadsCurrencyFromItsTemplate()
    {
        var npc = new NPC { NPCTemplate = new NPCTemplate { CurrencyId = Currency.Credits } };

        Assert.Equal(Currency.Credits, npc.CurrencyId);
    }
}
```

If `NPC.NPCTemplate` is not publicly settable, construct the NPC the way `NPCSpawnRegistrationTests` does and adjust — check that file before assuming.

**Step 2: Run test to verify it fails**

Run: `dotnet test --filter FullyQualifiedName~CurrencyCarrierTests`
Expected: FAIL — `'ItemTemplate' does not contain a definition for 'CurrencyId'`

**Step 3: Write minimal implementation**

In `Goose/ItemTemplate.cs`, beside `public int Credits { get; set; }`:

```csharp
        /// <summary>Overrides the vendor's currency for this item. Null means "use whatever
        /// the vendor deals in". Runtime-only - there is no items column for it, so sheet
        /// data never sets it; scripts do (Scripts/Global/Dimensions.csx).</summary>
        public string CurrencyId { get; set; }
```

and in the copy constructor after `this.Credits = other.Credits;` (`:156`):

```csharp
            this.CurrencyId = other.CurrencyId;
```

In `Goose/NPCTemplate.cs`, beside `CreditDealer`:

```csharp
        /// <summary>The currency this vendor trades in. Null means gold. Set from the
        /// credit_dealer column at load (NPCHandler.cs:105).</summary>
        public string CurrencyId { get; set; }
```

and in the copy constructor after `this.CreditDealer = other.CreditDealer;` (`:245`):

```csharp
            this.CurrencyId = other.CurrencyId;
```

In `Goose/NPC.cs`, beside the `CreditDealer` passthrough at `:344`:

```csharp
        public string CurrencyId { get { return this.NPCTemplate.CurrencyId; } }
```

In `Goose/NPCHandler.cs`, immediately after the `CreditDealer` assignment at `:105`:

```csharp
                        // Credit dealers are the only vendors with a non-gold currency in
                        // sheet data. Null (not "gold") so Resolve's fallback chain stays
                        // uniform: item override, then vendor, then gold.
                        npc.CurrencyId = npc.CreditDealer ? Currency.Credits : null;
```

**Step 4: Run test to verify it passes**

Run: `dotnet test --filter FullyQualifiedName~CurrencyCarrierTests`
Expected: PASS — 5 tests

**Step 5: Commit**

```bash
git add Goose/ItemTemplate.cs Goose/NPCTemplate.cs Goose/NPC.cs Goose/NPCHandler.cs Goose.Tests/CurrencyCarrierTests.cs
git commit -m "Carry a currency id on item and NPC templates"
```

**Coverage note:** the `NPCHandler.cs:105` mapping runs only against a real database, so no unit test drives it. Task 9's smoke test is what confirms it. Do not skip Task 9.

---

## Task 3: Currency resolution

**Files:**
- Modify: `Goose/Currency/CurrencyHandler.cs`
- Test: `Goose.Tests/CurrencyHandlerTests.cs` (extend)

**Step 1: Write the failing test**

Add to `CurrencyHandlerTests`, reusing the `StubCurrency` already there:

```csharp
    private static CurrencyHandler HandlerWith(params string[] ids)
    {
        var handler = new CurrencyHandler();
        handler.Register(new StubCurrency(Currency.Gold));
        foreach (var id in ids) handler.Register(new StubCurrency(id));
        return handler;
    }

    private static NPC VendorWith(string currencyId) =>
        new NPC { NPCTemplate = new NPCTemplate { CurrencyId = currencyId } };

    [Fact]
    public void Resolve_FallsBackToGoldWhenNeitherSetsACurrency()
    {
        var handler = HandlerWith();

        var resolved = handler.Resolve(new ItemTemplate(), VendorWith(null));

        Assert.Equal(Currency.Gold, resolved.Id);
    }

    [Fact]
    public void Resolve_UsesTheVendorCurrencyWhenTheItemHasNoOverride()
    {
        var handler = HandlerWith(Currency.Credits);

        var resolved = handler.Resolve(new ItemTemplate(), VendorWith(Currency.Credits));

        Assert.Equal(Currency.Credits, resolved.Id);
    }

    /// <summary>The decision from the design: a dimension item is worth spirit wherever it
    /// is traded, including at a credit dealer.</summary>
    [Fact]
    public void Resolve_ItemOverrideBeatsTheVendorCurrency()
    {
        var handler = HandlerWith(Currency.Credits, "spirit");

        var resolved = handler.Resolve(new ItemTemplate { CurrencyId = "spirit" }, VendorWith(Currency.Credits));

        Assert.Equal("spirit", resolved.Id);
    }

    [Fact]
    public void Resolve_HandlesANullVendor()
    {
        var handler = HandlerWith("spirit");

        Assert.Equal("spirit", handler.Resolve(new ItemTemplate { CurrencyId = "spirit" }, null).Id);
    }

    /// <summary>Falling back to gold here would sell a spirit item for gold at the till.
    /// Fail loud - the event loop contains the throw (EventHandler.cs:369).</summary>
    [Fact]
    public void Resolve_ThrowsWhenTheNamedCurrencyIsNotRegistered()
    {
        var handler = HandlerWith();

        var ex = Assert.Throws<InvalidOperationException>(
            () => handler.Resolve(new ItemTemplate { CurrencyId = "doubloons" }, null));

        Assert.Contains("doubloons", ex.Message);
    }
```

**Step 2: Run test to verify it fails**

Run: `dotnet test --filter FullyQualifiedName~CurrencyHandlerTests`
Expected: FAIL — `'CurrencyHandler' does not contain a definition for 'Resolve'`

**Step 3: Write minimal implementation**

Add to `CurrencyHandler`:

```csharp
        /// <summary>The currency a transaction settles in: the item's override if it has
        /// one, else the vendor's, else gold.
        ///
        /// An item override wins over the vendor deliberately - a dimension item is worth
        /// spirit wherever it is traded, including at a credit dealer, which today buys
        /// nothing at all.</summary>
        public ICurrency Resolve(ItemTemplate template, NPC vendor)
        {
            string id = template?.CurrencyId;
            if (string.IsNullOrEmpty(id)) id = vendor?.CurrencyId;
            if (string.IsNullOrEmpty(id)) id = Currency.Gold;

            var currency = this.Get(id);
            if (currency == null)
                throw new InvalidOperationException(
                    $"Currency '{id}' is not registered. Scripts must register a currency before stamping it onto templates.");

            return currency;
        }
```

**Step 4: Run test to verify it passes**

Run: `dotnet test --filter FullyQualifiedName~CurrencyHandlerTests`
Expected: PASS — 11 tests

**Step 5: Commit**

```bash
git add Goose/Currency/CurrencyHandler.cs Goose.Tests/CurrencyHandlerTests.cs
git commit -m "Resolve a transaction currency from item then vendor then gold"
```

---

## Task 4: Gold and credits as built-in currencies

**Files:**
- Create: `Goose/Currency/GoldCurrency.cs`
- Create: `Goose/Currency/CreditsCurrency.cs`
- Modify: `Goose/GameWorld.cs:158` area (register built-ins)
- Test: `Goose.Tests/BuiltInCurrencyTests.cs`

**Step 1: Write the failing test**

```csharp
using Goose;
using Xunit;

namespace Goose.Tests;

public class BuiltInCurrencyTests
{
    private static ItemTemplate Template(long value = 100, int credits = 0) =>
        new ItemTemplate { ID = 1, Name = "Sword", Value = value, Credits = credits, BaseStats = new AttributeSet() };

    private static Item ItemOf(ItemTemplate template)
    {
        var item = new Item();
        item.LoadFromTemplate(template);
        return item;
    }

    [Fact]
    public void Gold_NamesMatchTheStringsTheVendorEventsPrint()
    {
        var gold = new GoldCurrency();

        Assert.Equal(Currency.Gold, gold.Id);
        Assert.Equal("gold", gold.Name);
        Assert.Equal("gp", gold.ShortName);
    }

    [Fact]
    public void Gold_PricesBuysAtValueTimesStack()
    {
        Assert.Equal(300, new GoldCurrency().GetBuyPrice(Template(value: 100), 3));
    }

    [Fact]
    public void Gold_PaysHalfValueOnSell()
    {
        Assert.Equal(150, new GoldCurrency().GetSellPrice(ItemOf(Template(value: 100)), 3));
    }

    /// <summary>Reproduces VendorSellInventoryEvent.cs:78 - a worthless item is refused.</summary>
    [Fact]
    public void Gold_RefusesToBuyAWorthlessItem()
    {
        Assert.True(new GoldCurrency().GetSellPrice(ItemOf(Template(value: 0)), 1) < 0);
    }

    [Fact]
    public void Gold_BalanceIsThePlayersGold()
    {
        var player = new Player(0) { Gold = 4200 };

        Assert.Equal(4200, new GoldCurrency().GetBalance(player));
    }

    [Fact]
    public void Credits_NamesMatchTheStringsTheVendorEventsPrint()
    {
        var credits = new CreditsCurrency();

        Assert.Equal(Currency.Credits, credits.Id);
        Assert.Equal("credits", credits.Name);
        Assert.Equal("cr", credits.ShortName);
    }

    [Fact]
    public void Credits_PricesBuysAtTheCreditsValue()
    {
        Assert.Equal(20, new CreditsCurrency().GetBuyPrice(Template(credits: 10), 2));
    }

    /// <summary>Reproduces VendorSellInventoryEvent.cs:75 - credit dealers buy nothing.
    /// Unconditional, because credits_value defaults to 0 (items.sql:46) so the old
    /// Credits >= 0 test was true for every row.</summary>
    [Fact]
    public void Credits_RefusesEverySale()
    {
        Assert.True(new CreditsCurrency().GetSellPrice(ItemOf(Template(value: 500, credits: 10)), 1) < 0);
    }

    /// <summary>Player.Credits is an int (Player.cs:427). Clamp rather than wrap.</summary>
    [Fact]
    public void Credits_ClampsRatherThanWrappingOnOverflow()
    {
        var player = new Player(0) { Credits = 5 };

        new CreditsCurrency().Add(player, (long)int.MaxValue + 1000, world: null);

        Assert.Equal(int.MaxValue, player.Credits);
    }

    [Fact]
    public void BuiltInsAreRegisteredOnAFreshWorld()
    {
        var world = new GameWorld(null);

        Assert.NotNull(world.CurrencyHandler.Get(Currency.Gold));
        Assert.NotNull(world.CurrencyHandler.Get(Currency.Credits));
    }
}
```

`BuiltInsAreRegisteredOnAFreshWorld` needs `GameWorld.Settings` populated; if `new GameWorld(null)` throws without it, use `GlobalScriptFixture` (which sets settings in its constructor) and assert against `fixture.World`.

`Credits_ClampsRatherThanWrappingOnOverflow` passes `world: null` — verify `CreditsCurrency.Add` tolerates that, or give the test a real world from the fixture.

**Step 2: Run test to verify it fails**

Run: `dotnet test --filter FullyQualifiedName~BuiltInCurrencyTests`
Expected: FAIL — `The type or namespace name 'GoldCurrency' could not be found`

**Step 3: Write minimal implementation**

`Goose/Currency/GoldCurrency.cs`:

```csharp
using System;

namespace Goose
{
    /// <summary>Gold, the default currency. Wraps Player.Gold rather than reimplementing it,
    /// so AddGold/RemoveGold keep sending the StatusInfo packets clients expect.</summary>
    public class GoldCurrency : ICurrency
    {
        public string Id { get { return Currency.Gold; } }
        public string Name { get { return "gold"; } }
        public string ShortName { get { return "gp"; } }

        /// <summary>Carried gold only, ignoring the bank - this is what the purchase check
        /// has always compared against.</summary>
        public long GetBalance(Player player) { return player.Gold; }

        public long GetBuyPrice(ItemTemplate template, int stack) { return template.Value * stack; }

        /// <summary>Half value, and a refusal for worthless items - VendorSellInventoryEvent.cs:78.</summary>
        public long GetSellPrice(Item item, int stack)
        {
            if (item.Value == 0) return -1;
            return stack * item.Value / 2;
        }

        public void Add(Player player, long amount, GameWorld world) { player.AddGold(amount, world); }

        /// <summary>RemoveGold silently no-ops when the player is short (Player.cs:1482), so
        /// the caller's balance check is load-bearing. That is unchanged from today.</summary>
        public void Remove(Player player, long amount, GameWorld world) { player.RemoveGold(amount, world); }
    }
}
```

`Goose/Currency/CreditsCurrency.cs`:

```csharp
using System;

namespace Goose
{
    /// <summary>Donation credits. Selected by the vendor, never by the item: credits_value
    /// defaults to 0 (items.sql:46), so an item-level test would match every row.</summary>
    public class CreditsCurrency : ICurrency
    {
        public string Id { get { return Currency.Credits; } }
        public string Name { get { return "credits"; } }
        public string ShortName { get { return "cr"; } }

        public long GetBalance(Player player) { return player.Credits; }

        public long GetBuyPrice(ItemTemplate template, int stack) { return (long)template.Credits * stack; }

        /// <summary>Credit dealers buy nothing at all - the historic behaviour of
        /// VendorSellInventoryEvent.cs:75.</summary>
        public long GetSellPrice(Item item, int stack) { return -1; }

        public void Add(Player player, long amount, GameWorld world)
        {
            player.Credits = Clamp((long)player.Credits + amount);
            if (world != null) world.Send(player, P.StatusInfo(player));
        }

        public void Remove(Player player, long amount, GameWorld world)
        {
            player.Credits = Clamp((long)player.Credits - amount);
            if (world != null) world.Send(player, P.StatusInfo(player));
        }

        /// <summary>Player.Credits is an int while the interface is long. Saturate instead of
        /// wrapping - a wrapped balance is a negative wallet.</summary>
        private static int Clamp(long value)
        {
            if (value > int.MaxValue) return int.MaxValue;
            if (value < int.MinValue) return int.MinValue;
            return (int)value;
        }
    }
}
```

In `Goose/GameWorld.cs`, extend the construction added in Task 1:

```csharp
            this.CurrencyHandler = new CurrencyHandler();
            // Before LoadGlobalScripts (:355), so scripts can register their own currencies
            // from OnLoaded and resolve against these.
            this.CurrencyHandler.Register(new GoldCurrency());
            this.CurrencyHandler.Register(new CreditsCurrency());
```

**Step 4: Run test to verify it passes**

Run: `dotnet test --filter FullyQualifiedName~BuiltInCurrencyTests`
Expected: PASS — 11 tests

**Step 5: Commit**

```bash
git add Goose/Currency Goose/GameWorld.cs Goose.Tests/BuiltInCurrencyTests.cs
git commit -m "Ship gold and credits as built-in currencies"
```

---

## Task 5: Widen SP to long

`AttributeSet.cs:14-16` has `HP` and `MP` as `long` and `SP` as `int`. A dimension-6 item's `Value` is `base × 3^6 = 729×` and base values reach 10,000,000 (`Dimensions.csx:732`), so one item can be priced above `int.MaxValue`.

**Files:**
- Modify: `Goose/AttributeSet.cs:16` (field), `:182` (the `StatMultiplier` cast)
- Modify: `Goose/ClassHandler.cs:87`, `Goose/ItemHandler.cs:81`, `Goose/Player.cs:710`, `Goose/Pet.cs:232`, `Goose/SpellHandler.cs:89`, `Goose/NPCHandler.cs:83` (each `Convert.ToInt32` → `Convert.ToInt64`)
- Test: `Goose.Tests/AttributeSetSPTests.cs`

**Step 1: Write the failing test**

```csharp
using Goose;
using Xunit;

namespace Goose.Tests;

public class AttributeSetSPTests
{
    /// <summary>A dimension-6 item costs base x 3^6 = 729x, and base values reach
    /// 10,000,000 - past int.MaxValue. SP is the spirit wallet, so it must hold it.</summary>
    [Fact]
    public void SP_HoldsValuesBeyondIntMax()
    {
        long beyondInt = (long)int.MaxValue + 1000;

        var stats = new AttributeSet { SP = beyondInt };

        Assert.Equal(beyondInt, stats.SP);
    }

    [Fact]
    public void SP_SumsWithoutOverflowing()
    {
        var a = new AttributeSet { SP = int.MaxValue };
        var b = new AttributeSet { SP = int.MaxValue };

        Assert.Equal(2L * int.MaxValue, (a + b).SP);
    }
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test --filter FullyQualifiedName~AttributeSetSPTests`
Expected: FAIL — `error CS0266: Cannot implicitly convert type 'long' to 'int'`

**Step 3: Write minimal implementation**

`Goose/AttributeSet.cs:16`:

```csharp
        public long SP { get; set; }
```

`Goose/AttributeSet.cs:182` — the multiply operator ceilings into the field, so widen its cast:

```csharp
            temp.SP = (long)Math.Ceiling(a1.SP * multiplier);
```

Then each loader, `Convert.ToInt32` → `Convert.ToInt64`:

- `Goose/ClassHandler.cs:87` — `c.BaseStats.SP`
- `Goose/ItemHandler.cs:81` — `template.BaseStats.SP`
- `Goose/Player.cs:710` — `this.BaseStats.SP`
- `Goose/Pet.cs:232` — `pet.BaseStats.SP`
- `Goose/SpellHandler.cs:89` — `effect.Stats.SP`
- `Goose/NPCHandler.cs:83` — `npc.BaseStats.SP`

`Player.cs:577` (`GameWorld.Settings.StartingSP`) needs no change — int widens implicitly. Persistence needs no migration: SQLite integers are already 64-bit, and `Player.cs:937` / `:1018` interpolate the value into SQL as a string.

**Step 4: Run the full suite**

Run: `dotnet build` then `dotnet test`
Expected: build clean, all tests pass. The compiler will point at any `Convert.ToInt32` site missed above — fix each the same way.

**Step 5: Commit**

```bash
git add Goose Goose.Tests/AttributeSetSPTests.cs
git commit -m "Widen AttributeSet.SP to long to match HP and MP"
```

---

## Task 6: A vendor test fixture

Both retrofit tasks need a player who is socketless but whose messages can be read back. `Player.Send` is `virtual` (`Player.cs:2389`), so a subclass captures them.

**Files:**
- Create: `Goose.Tests/Fixtures/VendorFixture.cs`
- Test: `Goose.Tests/VendorFixtureTests.cs`

**Step 1: Write the failing test**

```csharp
using Goose;
using Goose.Tests.Fixtures;
using Xunit;

namespace Goose.Tests;

public class VendorFixtureTests
{
    [Fact]
    public void CapturingPlayer_RecordsWhatTheServerSends()
    {
        using var fixture = new VendorFixture();

        fixture.World.Send(fixture.Player, "hello");

        Assert.Contains(fixture.Player.Sent, m => m.Contains("hello"));
    }

    [Fact]
    public void Vendor_IsInRangeAndVisibleToThePlayer()
    {
        using var fixture = new VendorFixture();

        Assert.Same(fixture.Player.Map, fixture.Vendor.Map);
        Assert.Contains(fixture.Player.Windows, w => w.Type == Window.WindowTypes.Vendor);
    }
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test --filter FullyQualifiedName~VendorFixtureTests`
Expected: FAIL — `The type or namespace name 'VendorFixture' could not be found`

**Step 3: Write minimal implementation**

`Goose.Tests/Fixtures/VendorFixture.cs`:

```csharp
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
            VendorItems = new NPCVendorSlot[GameWorld.Settings.VendorSlotSize + 1],
        };

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
```

Field names on `NPC`, `Window`, and `Map` are all settable properties — if the compiler disagrees on any (for instance if `NPC.NPCTemplate` is init-only), check how `NPCSpawnRegistrationTests` builds its NPCs and mirror that. **Do not** add a setter to production code to make the fixture compile without first confirming there is no existing construction path.

`GameWorld.Settings.VendorSlotSize` must be set in `GlobalScriptFixture`'s settings block — if it is not, add it there alongside `InventorySize` and note the change in the commit.

**Step 4: Run test to verify it passes**

Run: `dotnet test --filter FullyQualifiedName~VendorFixtureTests`
Expected: PASS — 2 tests

**Step 5: Commit**

```bash
git add Goose.Tests/Fixtures/VendorFixture.cs Goose.Tests/VendorFixtureTests.cs
git commit -m "Add a vendor test fixture with a message-capturing player"
```

---

## Task 7: Retrofit the purchase event

**Files:**
- Modify: `Goose/Events/VendorPurchaseInventoryEvent.cs:78-126`
- Test: `Goose.Tests/VendorPurchaseCurrencyTests.cs`

**Step 1: Write the failing test**

```csharp
using Goose;
using Goose.Events;
using Goose.Tests.Fixtures;
using Xunit;

namespace Goose.Tests;

public class VendorPurchaseCurrencyTests
{
    private static void Purchase(VendorFixture fixture, int slotId)
    {
        var ev = new VendorPurchaseInventoryEvent
        {
            Player = fixture.Player,
            Data = "VPI" + fixture.Vendor.LoginID + "," + slotId,
        };
        ev.Ready(fixture.World);
    }

    private static ItemTemplate Sword(GlobalScriptFixture _, long value = 100, int credits = 0) =>
        new ItemTemplate
        {
            ID = 1, Name = "Sword", Description = "A Sword", Value = value, Credits = credits,
            BaseStats = new AttributeSet(), StackSize = 1, ScriptParams = "",
            Slot = ItemTemplate.ItemSlots.OneHanded,
        };

    /// <summary>Parity: an ordinary item at an ordinary vendor still costs gold, and the
    /// message still reads exactly as it did.</summary>
    [Fact]
    public void GoldPurchase_DebitsGoldAndKeepsTheMessage()
    {
        using var fixture = new VendorFixture();
        fixture.Player.Gold = 500;
        fixture.Stock(1, Sword(null, value: 100));

        Purchase(fixture, 1);

        Assert.Equal(400, fixture.Player.Gold);
        Assert.Contains(fixture.Player.Sent, m => m.Contains("Purchased Sword for 100 gold."));
    }

    [Fact]
    public void GoldPurchase_RefusesWhenTheBalanceIsShort()
    {
        using var fixture = new VendorFixture();
        fixture.Player.Gold = 10;
        fixture.Stock(1, Sword(null, value: 100));

        Purchase(fixture, 1);

        Assert.Equal(10, fixture.Player.Gold);
        Assert.Contains(fixture.Player.Sent, m => m.Contains("you don't have enough gold."));
    }

    /// <summary>Parity: a credit dealer still charges the credits_value, not the gold value.</summary>
    [Fact]
    public void CreditPurchase_DebitsCreditsAndKeepsTheMessage()
    {
        using var fixture = new VendorFixture();
        fixture.VendorDealsIn(Currency.Credits);
        fixture.Player.Credits = 50;
        fixture.Player.Gold = 9999;
        fixture.Stock(1, Sword(null, value: 100, credits: 10));

        Purchase(fixture, 1);

        Assert.Equal(40, fixture.Player.Credits);
        Assert.Equal(9999, fixture.Player.Gold);
        Assert.Contains(fixture.Player.Sent, m => m.Contains("Purchased Sword for 10 credits."));
    }

    /// <summary>The new behaviour: an item override wins over the vendor's currency.</summary>
    [Fact]
    public void ItemOverride_ChargesTheOverrideCurrencyAtAGoldVendor()
    {
        using var fixture = new VendorFixture();
        fixture.World.CurrencyHandler.Register(new TestSpiritCurrency());
        fixture.Player.Gold = 9999;
        fixture.Player.BaseStats.SP = 500;

        var template = Sword(null, value: 100);
        template.CurrencyId = "spirit";
        fixture.Stock(1, template);

        Purchase(fixture, 1);

        Assert.Equal(400, fixture.Player.BaseStats.SP);
        Assert.Equal(9999, fixture.Player.Gold);
    }

    /// <summary>A full inventory must cost nothing - the charge happens only after AddItem
    /// succeeds, exactly as before the retrofit.</summary>
    [Fact]
    public void FullInventory_ChargesNothing()
    {
        using var fixture = new VendorFixture();
        fixture.Player.Gold = 500;
        fixture.Stock(1, Sword(null, value: 100));
        FillInventory(fixture);

        Purchase(fixture, 1);

        Assert.Equal(500, fixture.Player.Gold);
        Assert.Contains(fixture.Player.Sent, m => m.Contains("your inventory is full."));
    }

    private static void FillInventory(VendorFixture fixture)
    {
        var filler = new ItemTemplate
        {
            ID = 99, Name = "Rock", Value = 1, BaseStats = new AttributeSet(),
            StackSize = 1, ScriptParams = "", Slot = ItemTemplate.ItemSlots.OneHanded,
        };
        for (int i = 0; i < GameWorld.Settings.InventorySize; i++) fixture.Carry(filler);
    }

    /// <summary>A minimal spirit stand-in. The real one lives in Dimensions.csx (Task 9);
    /// this test must not depend on that script compiling.</summary>
    private sealed class TestSpiritCurrency : ICurrency
    {
        public string Id => "spirit";
        public string Name => "spirit";
        public string ShortName => "sp";
        public long GetBalance(Player player) => player.BaseStats.SP;
        public long GetBuyPrice(ItemTemplate template, int stack) => template.Value * stack;
        public long GetSellPrice(Item item, int stack) => stack * item.Value / 2;
        public void Add(Player player, long amount, GameWorld world) => player.BaseStats.SP += amount;
        public void Remove(Player player, long amount, GameWorld world) => player.BaseStats.SP -= amount;
    }
}
```

`Player.BaseStats` must be non-null on a fixture player — if it is not, initialise it in `VendorFixture` and say so in the commit.

**Step 2: Run test to verify it fails**

Run: `dotnet test --filter FullyQualifiedName~VendorPurchaseCurrencyTests`
Expected: FAIL — `ItemOverride_ChargesTheOverrideCurrencyAtAGoldVendor` charges gold instead of spirit. The three parity tests should already **pass** against the unmodified event; if any parity test fails here, the test is wrong, not the server — fix the test before touching production code.

**Step 3: Write minimal implementation**

Replace `VendorPurchaseInventoryEvent.cs:78-126` — the two affordability checks and the whole `if (npc.CreditDealer) … else …` block — with:

```csharp
                var currency = world.CurrencyHandler.Resolve(slot.ItemTemplate, npc);
                long cost = currency.GetBuyPrice(slot.ItemTemplate, slot.Stack);

                if (cost < 0 || currency.GetBalance(this.Player) < cost)
                {
                    world.Send(this.Player, P.ServerMessage("Can't purchase " + slot.ItemTemplate.Name +
                        (slot.Stack > 1 ? " (" + slot.Stack + ")" : "") +
                        " as you don't have enough " + currency.Name + "."));
                    return;
                }

                Item item = new Item();
                item.LoadFromTemplate(slot.ItemTemplate);

                world.ItemHandler.RollTitleAndSurname(item, world);

                world.ItemHandler.AddAndAssignId(item, world);

                if (this.Player.Inventory.AddItem(item, slot.Stack, world))
                {
                    // Charged only after the item lands, so a full inventory costs nothing.
                    currency.Remove(this.Player, cost, world);

                    world.Send(this.Player, P.ServerMessage("Purchased " + item.Name +
                        (slot.Stack > 1 ? " (" + slot.Stack + ")" : "") +
                        " for " + cost + " " + currency.Name + "."));

                    world.LogHandler.Log(Log.Types.BuyFromVendor, this.Player.PlayerID,
                        $"{item.Name} ({item.TemplateID}) x{slot.Stack} ({cost} {currency.ShortName})",
                        npc.NPCTemplateID, this.Player.Map.ID, this.Player.MapX, this.Player.MapY);

                    if (item.IsBindOnPickup)
                    {
                        item.IsBound = true;
                    }

                    return;
                }
                else
                {
                    world.Send(this.Player, P.ServerMessage("Can't purchase " + slot.ItemTemplate.Name +
                        " as your inventory is full."));
                    return;
                }
```

The LORE check above (`:71-76`) is untouched.

**Step 4: Run test to verify it passes**

Run: `dotnet test --filter FullyQualifiedName~VendorPurchaseCurrencyTests`
Expected: PASS — 5 tests

**Step 5: Commit**

```bash
git add Goose/Events/VendorPurchaseInventoryEvent.cs Goose.Tests/VendorPurchaseCurrencyTests.cs
git commit -m "Route vendor purchases through the currency registry"
```

---

## Task 8: Retrofit the sell event

**Files:**
- Modify: `Goose/Events/VendorSellInventoryEvent.cs:71-95`
- Test: `Goose.Tests/VendorSellCurrencyTests.cs`

**Step 1: Write the failing test**

```csharp
using Goose;
using Goose.Events;
using Goose.Tests.Fixtures;
using Xunit;

namespace Goose.Tests;

public class VendorSellCurrencyTests
{
    private static void Sell(VendorFixture fixture, int slotId, int stack)
    {
        var ev = new VendorSellInventoryEvent
        {
            Player = fixture.Player,
            Data = "VSI" + fixture.Vendor.LoginID + "," + slotId + "," + stack,
        };
        ev.Ready(fixture.World);
    }

    private static ItemTemplate Sword(long value = 100) =>
        new ItemTemplate
        {
            ID = 1, Name = "Sword", Description = "A Sword", Value = value,
            BaseStats = new AttributeSet(), StackSize = 1, ScriptParams = "",
            Slot = ItemTemplate.ItemSlots.OneHanded,
        };

    [Fact]
    public void GoldSale_PaysHalfValueAndKeepsTheMessage()
    {
        using var fixture = new VendorFixture();
        fixture.Carry(Sword(value: 100));

        Sell(fixture, 1, 1);

        Assert.Equal(50, fixture.Player.Gold);
        Assert.Contains(fixture.Player.Sent, m => m.Contains("Sold Sword for 50 gold."));
    }

    [Fact]
    public void GoldSale_RefusesAWorthlessItem()
    {
        using var fixture = new VendorFixture();
        fixture.Carry(Sword(value: 0));

        Sell(fixture, 1, 1);

        Assert.Equal(0, fixture.Player.Gold);
        Assert.Contains(fixture.Player.Sent, m => m.Contains("I have no interest in purchasing Sword."));
        Assert.NotNull(fixture.Player.Inventory.GetSlot(1));
    }

    /// <summary>Parity: credit dealers buy nothing, and the item stays in the bag.</summary>
    [Fact]
    public void CreditDealer_RefusesEverySale()
    {
        using var fixture = new VendorFixture();
        fixture.VendorDealsIn(Currency.Credits);
        fixture.Carry(Sword(value: 100));

        Sell(fixture, 1, 1);

        Assert.Equal(0, fixture.Player.Gold);
        Assert.Equal(0, fixture.Player.Credits);
        Assert.NotNull(fixture.Player.Inventory.GetSlot(1));
    }

    /// <summary>The new behaviour, and the decision from the design: an item override wins,
    /// so a dimension item sells for spirit even at a credit dealer that buys nothing else.</summary>
    [Fact]
    public void ItemOverride_PaysTheOverrideCurrencyEvenAtACreditDealer()
    {
        using var fixture = new VendorFixture();
        fixture.World.CurrencyHandler.Register(new TestSpiritCurrency());
        fixture.VendorDealsIn(Currency.Credits);

        var template = Sword(value: 100);
        template.CurrencyId = "spirit";
        fixture.Carry(template);

        Sell(fixture, 1, 1);

        Assert.Equal(50, fixture.Player.BaseStats.SP);
        Assert.Equal(0, fixture.Player.Gold);
    }

    private sealed class TestSpiritCurrency : ICurrency
    {
        public string Id => "spirit";
        public string Name => "spirit";
        public string ShortName => "sp";
        public long GetBalance(Player player) => player.BaseStats.SP;
        public long GetBuyPrice(ItemTemplate template, int stack) => template.Value * stack;
        public long GetSellPrice(Item item, int stack) => stack * item.Value / 2;
        public void Add(Player player, long amount, GameWorld world) => player.BaseStats.SP += amount;
        public void Remove(Player player, long amount, GameWorld world) => player.BaseStats.SP -= amount;
    }
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test --filter FullyQualifiedName~VendorSellCurrencyTests`
Expected: FAIL — `ItemOverride_PaysTheOverrideCurrencyEvenAtACreditDealer` gets refused by the old `CreditDealer` check. The three parity tests should pass unmodified.

**Step 3: Write minimal implementation**

Replace `VendorSellInventoryEvent.cs:71-95` — both refusals and the gold payout — with:

```csharp
                var currency = world.CurrencyHandler.Resolve(slot.Item.Template, npc);
                long price = currency.GetSellPrice(slot.Item, stack);

                // Refused before the item leaves the bag.
                if (price < 0)
                {
                    world.Send(this.Player, P.ServerMessage("I have no interest in purchasing " + slot.Item.Name + "."));
                    return;
                }

                ItemSlot sellslot = this.Player.Inventory.RemoveItem(slot.Item, stack, world);

                currency.Add(this.Player, price, world);

                world.Send(this.Player, P.ServerMessage("Sold " + sellslot.Item.Name +
                    (sellslot.Stack > 1 ? " (" + sellslot.Stack + ")" : "") +
                    " for " + price + " " + currency.Name + "."));

                world.LogHandler.Log(Log.Types.SellToVendor, this.Player.PlayerID,
                    $"{slot.Item.Name} ({slot.Item.TemplateID}) x{slot.Stack} ({price} {currency.ShortName})",
                    npc.NPCTemplateID, this.Player.Map.ID, this.Player.MapX, this.Player.MapY);
```

Note the original computed the message price and the logged price from separate expressions (`sellslot.Stack * sellslot.Item.Value / 2` versus `price`); both collapse to the single `price`, which is what `GetSellPrice(slot.Item, stack)` returns.

**Step 4: Run test to verify it passes**

Run: `dotnet test --filter FullyQualifiedName~VendorSellCurrencyTests`
Expected: PASS — 4 tests

Then run the whole suite: `dotnet test`
Expected: all green, no regressions.

**Step 5: Commit**

```bash
git add Goose/Events/VendorSellInventoryEvent.cs Goose.Tests/VendorSellCurrencyTests.cs
git commit -m "Route vendor sales through the currency registry"
```

---

## Task 9: Spirit, in script

**Files:**
- Modify: `Goose/Data/Illutia/Scripts/Global/Dimensions.csx` (config, `OnLoaded`, `CloneItemTemplates` around `:610`)
- Test: `Goose.Tests/SpiritCurrencyTests.cs`

**Step 1: Write the failing test**

```csharp
using Goose;
using Goose.Tests.Fixtures;
using Xunit;

namespace Goose.Tests;

public class SpiritCurrencyTests
{
    private const int Offset = 100000;   // Dimensions.csx:19

    [Fact]
    public void OnLoaded_RegistersSpirit()
    {
        using var fixture = new GlobalScriptFixture();
        fixture.AddBaseItemTemplate(1, "Sword", ItemTemplate.UseTypes.Equipped, t => t.Value = 100);

        fixture.CompileShipped().Object.OnLoaded(fixture.World);

        Assert.NotNull(fixture.World.CurrencyHandler.Get("spirit"));
    }

    [Fact]
    public void DimensionClones_ArePricedInSpirit()
    {
        using var fixture = new GlobalScriptFixture();
        fixture.AddBaseItemTemplate(1, "Sword", ItemTemplate.UseTypes.Equipped, t => t.Value = 100);

        fixture.CompileShipped().Object.OnLoaded(fixture.World);

        var clone = fixture.World.ItemHandler.GetTemplate(1 + Offset);
        Assert.Equal("spirit", clone.CurrencyId);
    }

    /// <summary>Base templates must keep the default so ordinary vendors keep taking gold.</summary>
    [Fact]
    public void BaseTemplates_KeepTheDefaultCurrency()
    {
        using var fixture = new GlobalScriptFixture();
        fixture.AddBaseItemTemplate(1, "Sword", ItemTemplate.UseTypes.Equipped, t => t.Value = 100);

        fixture.CompileShipped().Object.OnLoaded(fixture.World);

        Assert.Null(fixture.World.ItemHandler.GetTemplate(1).CurrencyId);
    }

    [Fact]
    public void Spirit_PricesBuysAtValueAndSellsAtHalf()
    {
        using var fixture = new GlobalScriptFixture();
        fixture.AddBaseItemTemplate(1, "Sword", ItemTemplate.UseTypes.Equipped, t => t.Value = 100);
        fixture.CompileShipped().Object.OnLoaded(fixture.World);

        var spirit = fixture.World.CurrencyHandler.Get("spirit");
        var clone = fixture.World.ItemHandler.GetTemplate(1 + Offset);
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
        fixture.AddBaseItemTemplate(1, "Sword", ItemTemplate.UseTypes.Equipped, t => t.Value = 100);
        fixture.CompileShipped().Object.OnLoaded(fixture.World);

        var map = fixture.AddBaseMap(1, "Town");
        var player = fixture.PlayerOn(map, 5, 5);
        var spirit = fixture.World.CurrencyHandler.Get("spirit");

        spirit.Add(player, 500, fixture.World);

        Assert.Equal(500, spirit.GetBalance(player));
        Assert.Equal(500, player.MaxStats.SP);
        Assert.Equal(player.MaxSP, player.CurrentSP);
    }

    [Fact]
    public void Spirit_RemoveMovesBothBack()
    {
        using var fixture = new GlobalScriptFixture();
        fixture.AddBaseItemTemplate(1, "Sword", ItemTemplate.UseTypes.Equipped, t => t.Value = 100);
        fixture.CompileShipped().Object.OnLoaded(fixture.World);

        var map = fixture.AddBaseMap(1, "Town");
        var player = fixture.PlayerOn(map, 5, 5);
        var spirit = fixture.World.CurrencyHandler.Get("spirit");

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
        fixture.AddBaseItemTemplate(1, "Sword", ItemTemplate.UseTypes.Equipped, t => t.Value = 100);
        fixture.CompileShipped().Object.OnLoaded(fixture.World);

        var map = fixture.AddBaseMap(1, "Town");
        var player = fixture.PlayerOn(map, 5, 5);
        var spirit = fixture.World.CurrencyHandler.Get("spirit");

        spirit.Add(player, 100, fixture.World);
        player.AddStats(new AttributeSet { SP = 1000 }, fixture.World);   // as equipping would

        Assert.Equal(100, spirit.GetBalance(player));
    }
}
```

`fixture.PlayerOn` returns `new Player(0) { … }` (`GlobalScriptFixture.cs`) — confirm `BaseStats` and `MaxStats` are non-null on it, and initialise them in the fixture helper if not.

**Step 2: Run test to verify it fails**

Run: `dotnet test --filter FullyQualifiedName~SpiritCurrencyTests`
Expected: FAIL — `OnLoaded_RegistersSpirit` gets null from `Get("spirit")`

**Step 3: Write minimal implementation**

In `Dimensions.csx`, add to the configuration block near `SurnameIdBase`:

```csharp
    /// <summary>Registry id for the spirit currency. Dimension items are priced in it;
    /// their Value is already the spirit price (x3^dim, see CloneItemTemplates).</summary>
    public const string SpiritCurrencyId = "spirit";
```

Add the implementation at the end of the file, beside the other script classes:

```csharp
/// <summary>Spirit, the dimension currency. The wallet is BaseStats.SP, which already
/// persists as players.player_sp, so no schema change is needed.
///
/// MaxStats.SP is separate accounting from BaseStats.SP: MaxSP reads MaxStats
/// (Player.cs:210), and CurrentSP's setter clamps to MaxSP (Player.cs:185). So a balance
/// change moves both, and CurrentSP is topped up afterwards - regen is zeroed in
/// GooseSettings.json, so nothing else ever moves it.
///
/// Gear granting SP raises MaxStats only, so it cannot inflate the balance. It can make
/// MaxSP exceed the balance, which is cosmetic in the client's SP bar.</summary>
public class SpiritCurrency : ICurrency
{
    public string Id { get { return Dimensions.SpiritCurrencyId; } }
    public string Name { get { return "spirit"; } }
    public string ShortName { get { return "sp"; } }

    public long GetBalance(Player player) { return player.BaseStats.SP; }

    public long GetBuyPrice(ItemTemplate template, int stack) { return template.Value * stack; }

    public long GetSellPrice(Item item, int stack) { return stack * item.Value / 2; }

    public void Add(Player player, long amount, GameWorld world)
    {
        player.BaseStats.SP += amount;

        var delta = new AttributeSet();
        delta.SP = amount;
        player.AddStats(delta, world);        // raises MaxStats.SP and sends StatusInfo

        player.CurrentSP = player.MaxSP;      // setter clamps, so this must follow AddStats
        world.Send(player, P.StatusInfo(player));
    }

    public void Remove(Player player, long amount, GameWorld world)
    {
        player.BaseStats.SP -= amount;

        var delta = new AttributeSet();
        delta.SP = amount;
        player.RemoveStats(delta, world);

        player.CurrentSP = player.MaxSP;
        world.Send(player, P.StatusInfo(player));
    }
}
```

Register it in `OnLoaded`, **before** `CloneItemTemplates` so the stamping can validate against it:

```csharp
        world.CurrencyHandler.Register(new SpiritCurrency());
```

In `CloneItemTemplates`, beside the existing `Value` assignment at `:610`:

```csharp
            CurrencyId = SpiritCurrencyId,
```

If the clone is built via the `ItemTemplate` copy constructor rather than an object initialiser at that line, set `clone.CurrencyId = SpiritCurrencyId;` immediately after the `Value` assignment instead. Read the surrounding code before editing.

Add a stamping-time guard right after the register call, so an unregistered currency fails at load rather than at a till:

```csharp
        if (world.CurrencyHandler.Get(SpiritCurrencyId) == null)
            throw new Exception($"Currency '{SpiritCurrencyId}' failed to register.");
```

**Step 4: Run test to verify it passes**

Run: `dotnet test --filter FullyQualifiedName~SpiritCurrencyTests`
Expected: PASS — 7 tests

Then the full suite: `dotnet test`
Expected: all green. `DimensionItemTemplateTests` and `DimensionsScriptTests` exercise the same clone path — if either fails, the stamping went in the wrong branch of `CloneItemTemplates`.

**Step 5: Commit**

```bash
git add Goose/Data/Illutia/Scripts/Global/Dimensions.csx Goose.Tests/SpiritCurrencyTests.cs
git commit -m "Dimensions: register spirit and price dimension items in it"
```

---

## Task 10: Manual smoke test

Two things in this plan are not reachable from unit tests: the `credit_dealer` → `CurrencyId` mapping at `NPCHandler.cs:105` (needs a real database) and the end-to-end client experience of a spirit purchase. Do not skip this.

**Step 1: Start the server against a real data directory**

Follow `docs/DEPLOY.md`. Confirm the log shows Global Scripts loading with no exception — a failed currency registration throws there and aborts the load step.

**Step 2: Verify gold is untouched**

Buy and sell an ordinary item at an ordinary vendor. Confirm the messages read `"Purchased X for N gold."` and `"Sold X for N gold."`, and that the gold total moves by exactly N.

**Step 3: Verify credits are untouched**

At a credit dealer: buy something and confirm it debits credits, not gold. Try to sell it anything and confirm `"I have no interest in purchasing X."` This is the only check on the `NPCHandler.cs:105` mapping.

**Step 4: Verify spirit**

`/dimension 1`, then kill something and pick up a dimension drop. Sell it to any vendor and confirm the SP bar rises by `Value / 2` and gold does not move. Confirm the sale message says `spirit`.

**Step 5: Record the result**

```bash
git commit --allow-empty -m "Smoke test: gold, credits and spirit verified against a live server"
```

If any step fails, stop and fix before merging — a currency bug that reaches players is not recoverable from logs alone.

---

## Design alignment

Checked against `docs/plans/2026-08-11-currency-system-design.md`:

- `ICurrency` members match the design's interface exactly, including the asymmetric buy/sell parameter types and the negative-return refusal convention.
- Resolution order is item override → vendor → gold, with the item override winning at credit dealers — the decision recorded in the design.
- Gold and credits messages and log short-names reproduce today's strings byte for byte (`"gold"`/`"gp"`, `"credits"`/`"cr"`), which the parity tests assert directly.
- No `items` or `npc_templates` column is added; `CurrencyId` is runtime-only on both carriers.
- No SP-cost or class-grant preflight, per the decision to treat such data changes as intentional.
- `AttributeSet.SP` widened to `long`.

**Out of scope, as designed:** rebirth, `/resetitem`, SP conversions, and repointing dimension vendor stock. After this plan a vendor standing in dimension 5 still sells base-tier goods for gold — that is Part 6.
