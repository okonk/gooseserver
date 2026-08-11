# Currency system design

Part 5 of the dimensions work (`docs/dimensions.md`). It builds the generic
currency subsystem the spirit economy needs, and registers spirit as the first
script-defined currency. The economy itself — rebirth, `/resetitem`, vendor
stock — is deliberately out of scope and follows in part 6.

Parts 1–4 shipped the cloned world, entry gating, per-dimension spells, and
per-dimension items. Dimension item templates already carry
`Value = base × 3^dim`, intended as a spirit price that nothing yet charges.

## Goals

- A currency abstraction generic enough that spirit is defined entirely in
  script, with nothing dimension-specific in the server.
- Gold and credits rebuilt on that abstraction, with no observable change to
  either economy.
- Dimension items priced in spirit at every vendor.

## Non-goals

- Rebirth, `/resetitem`, SP→gold and SP→XP conversions, cosmetic sinks.
- Repointing dimension vendor stock at dimension item templates.
- Scriptable or dynamic pricing.
- A `currency` column on `items`. Currency is runtime-only, set by scripts.

## The abstraction

`Goose/Currency/ICurrency.cs`:

```csharp
public interface ICurrency
{
    string Id { get; }          // "gold", "credits", "spirit"
    string Name { get; }        // "gold" — interpolated into player messages
    string ShortName { get; }   // "gp" — interpolated into LogHandler entries
    long GetBalance(Player player);
    long GetBuyPrice(ItemTemplate template, int stack);  // < 0 = not purchasable
    long GetSellPrice(Item item, int stack);             // < 0 = vendor refuses
    void Add(Player player, long amount, GameWorld world);
    void Remove(Player player, long amount, GameWorld world);
}
```

Buy takes an `ItemTemplate` and sell takes an `Item` because that is what each
vendor path actually holds. The sell path reads `slot.Item.Value`, which item
modifiers can move away from the template value, so collapsing the two
signatures would lose information.

`CurrencyHandler` hangs off `GameWorld` beside the other handlers: a
case-insensitive dictionary, `Register` (throws on duplicate id — silently
overwriting a currency is how a wallet goes missing), `Get` (null on unknown),
a `Gold` property, and:

```csharp
ICurrency Resolve(ItemTemplate template, NPC vendor);   // item override ?? vendor ?? gold
```

Built-ins register during world load, **before** global scripts' `OnLoaded`, so
`Dimensions.csx` can register spirit there. This ordering must be pinned
explicitly in `GameWorld`'s load sequence; it is the kind of thing that works by
accident until a constructor is reordered.

## Currency resolution

Two carriers, because this codebase treats the two existing currencies
differently and both behaviours have to survive:

- **`NPCTemplate.CurrencyId`** — the currency a vendor trades in. Defaults to
  `"gold"`; `NPCHandler` maps the existing `credit_dealer` column to
  `"credits"` at load. No migration.
- **`ItemTemplate.CurrencyId`** — an optional override, default `null`. Scripts
  set it; sheet data never does. Null rather than `"gold"`, so that an unset
  item can inherit a credit dealer's currency.

The item override wins wherever it is set.

Credits could not be derived from the item. `credits_value` defaults to `0`
(`Goose/sql/items.sql:46`), so `Item.Credits >= 0` is true for essentially every
row — the check at `VendorSellInventoryEvent.cs:75` is not "we don't buy credit
items back" but "credit dealers don't buy anything at all". Credits are a
property of the vendor in this codebase, and the model reflects that.

Every existing behaviour falls out of `Resolve` with no currency-specific code
left in either event:

| Transaction | Resolves to | Result |
|---|---|---|
| Buy at credit dealer | credits (vendor) | `Credits × stack`, unchanged |
| Sell at credit dealer | credits (vendor) | `GetSellPrice` < 0 → refused, unchanged |
| Sell ordinary item, ordinary vendor | gold (vendor) | `Value / 2` gold, unchanged |
| Buy/sell dimension item, any vendor | spirit (item override) | spirit — the new behaviour |

The last row includes credit dealers, which gain the ability to buy dimension
loot for spirit where today they buy nothing. Accepted: they are donation shops,
and the payout matches what any other vendor would pay.

`NPCTemplate.CreditDealer` stays as a loaded field so nothing else breaks. It is
simply no longer consulted.

## Built-in currencies

`GoldCurrency` wraps what exists rather than reimplementing it: `GetBalance` is
`Player.Gold`, `Add`/`Remove` delegate to `AddGold`/`RemoveGold` with the
bank-overflow behaviour untouched, `GetBuyPrice` is `Value × stack`,
`GetSellPrice` is `Value × stack / 2` returning `-1` when `Value == 0` to
reproduce today's refusal. `GetBalance` deliberately ignores banked gold,
matching the current purchase check.

`CreditsCurrency` reads `Player.Credits`, prices buys at `Credits × stack`, and
returns `-1` from `GetSellPrice` unconditionally. `Player.Credits` is an `int`
while the interface is `long`; the narrowing lives inside `CreditsCurrency` and
clamps rather than wraps.

## Vendor event retrofit

Both events lose every currency branch and collapse to one path.
`VendorPurchaseInventoryEvent.cs:78-126` becomes roughly:

```csharp
var currency = world.CurrencyHandler.Resolve(slot.ItemTemplate, npc);
long cost = currency.GetBuyPrice(slot.ItemTemplate, slot.Stack);

if (cost < 0 || currency.GetBalance(this.Player) < cost)
{ "Can't purchase X as you don't have enough " + currency.Name + "."; return; }

// unchanged: new Item, LoadFromTemplate, RollTitleAndSurname, AddAndAssignId, AddItem

currency.Remove(this.Player, cost, world);
world.Send(..., "Purchased X for " + cost + " " + currency.Name + ".");
world.LogHandler.Log(..., $"... ({cost} {currency.ShortName})", ...);
```

Ordering is preserved exactly: balance checked before the item is created,
charged after `AddItem` succeeds, so a full inventory still costs nothing. With
`Name` of `"gold"`/`"credits"` and `ShortName` of `"gp"`/`"cr"`, every
player-facing string and log line comes out byte-identical to today.

Sell is the same shape — resolve, `GetSellPrice`, refuse on negative *before*
`RemoveItem`, then `currency.Add`. Both existing refusals survive as return
values rather than inline conditions.

`cost < 0` as "not purchasable" is new on the buy path. Nothing produces it
today; it gives future currencies a way to mark stock display-only.

## Spirit, entirely in script

`ICurrency` is a plain interface in the `Goose` namespace, so `Dimensions.csx`
declares a class implementing it and registers it in `OnLoaded`. No new script
type, no `ScriptHandler` change.

The wallet is `BaseStats.SP`, which already persists as `players.player_sp`.
`MaxStats.SP` is separate accounting, so a balance change touches both:

```csharp
public void Add(Player player, long amount, GameWorld world)
{
    var delta = new AttributeSet(); delta.SP = amount;
    player.BaseStats.SP += amount;
    player.AddStats(delta, world);       // raises MaxStats.SP, sends StatusInfo
    player.CurrentSP = player.MaxSP;     // clamped by the setter, so must follow AddStats
    world.Send(player, P.StatusInfo(player));
}
```

`GetBalance` reads `BaseStats.SP`; `Remove` mirrors via `RemoveStats`.
`GetBuyPrice` is `Value × stack`, `GetSellPrice` is `Value × stack / 2`. The
existing clone loop (`Dimensions.csx:610`) stamps `CurrencyId = "spirit"`
alongside the `Value` it already sets.

SP regen is already zeroed in `GooseSettings.json`, so the balance only moves
when a currency operation moves it.

### SP widened to long

`AttributeSet.cs:14-16` has `HP` and `MP` as `long` and `SP` as `int` — an
inconsistency rather than a decision. It matters here: a dimension-6 item's
`Value` is `base × 3^6 = 729×`, and base values reach 10,000,000
(`Dimensions.csx:732`), so a single item can be priced above `int.MaxValue`.

`SP` becomes `long`, updating the `Convert.ToInt32` call sites at
`ClassHandler.cs:87`, `ItemHandler.cs:81`, and `Player.cs:710`. No migration —
SQLite integers are already 64-bit.

### Accepted leaks

Casting deducts SP unconditionally: `Player.cs:1996-2004` subtracts
`SPStaticCost` and `SPPercentCost`, and the affordability check at `:1969` is
commented out. A spell row with a nonzero SP cost will silently drain wallets.
Likewise a class granting per-level SP (`ClassHandler.cs:87`) mints spirit. Both
are left unguarded by decision: if the data changes, the change is intentional.

Item SP grants (`ItemHandler.cs:81`) are harmless. Equipping raises
`MaxStats.SP` only, and the balance reads `BaseStats.SP`, so gear cannot inflate
the wallet. It does let `MaxSP` exceed the balance, which is cosmetic in the
client's SP bar.

## Testing

The existing `Goose.Tests` suite and part 4's `GlobalScriptFixture` cover this.

- **Currency units** — duplicate registration throws; `Get` returns null on
  unknown; `Resolve` precedence has a test per rung (item override, vendor,
  gold default).
- **Parity** — for each row of the resolution table, assert the balance change,
  the exact player message, and the exact log string. Written against current
  behaviour, so they fail if the refactor shifts anything.
- **Vendor events** — insufficient balance refuses before item creation; a full
  inventory charges nothing; credit dealers refuse every sell; a spirit-priced
  item bought at a gold vendor debits `BaseStats.SP` and leaves gold alone.
- **Spirit** — `Add`/`Remove` move `BaseStats.SP` and `MaxStats.SP` together and
  leave `CurrentSP == MaxSP`; the balance round-trips through save/load;
  equipping an SP-granting item raises `MaxSP` without changing the balance.
- **Clones** — dimension templates carry `CurrencyId = "spirit"`, base templates
  carry the default, and the value survives `ItemTemplate`'s copy constructor.

## Known gaps, deferred by decision

- **Vendor price display.** `Packets.cs:472` sends `item.Value` as the price
  regardless of currency, so credit dealers have always displayed the gold value
  rather than the credit price. Spirit happens to display correctly, because a
  dimension item's `Value` *is* its spirit price. `VendorItemSlot` is a
  reassignable `Func` if this is worth decorating later.
- **Dimension vendors stock dimension-0 goods.** `NPCTemplate`'s copy
  constructor gives clones `CurrencyId = "gold"`, and stock repointing belongs
  to part 6. Until then a vendor standing in dimension 5 sells base-tier items
  at base prices for gold.
- **Spirit balances persist regardless of `Enabled`.** Flipping
  `Dimensions.Enabled = false` later leaves earned spirit in `players.player_sp`
  with nothing to spend it on. That is the design working as intended, but worth
  knowing.
- **Vendor-level currency has no data path** beyond `credit_dealer`. A future
  spirit *vendor* needs either a script stamping `NPCTemplate.CurrencyId` or a
  new column.
