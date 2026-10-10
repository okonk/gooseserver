# Dimension Vendor Currency Parity Implementation Plan

## Goal

Match Abyss's currency-selection rule for dimension transactions: dimension changes the price, but does not choose the currency. Ordinary dimension items and vendors therefore use gold; an explicitly currency-marked Goose item still overrides its vendor, and a vendor with an explicit Goose currency keeps that currency in every dimension.

The checked-in Goose loaders and production scripts do not author spirit `CurrencyId` values for base items or vendors. Deployed spreadsheet/custom-script data has not been enumerated. With the checked-in behavior, dimension gear will buy and sell for scaled gold after this change. Spirit remains registered for rebirth, reset-item, transfer, and conversion commands.

## Parity boundary and Goose-specific decisions

Abyss proves these behaviors:

- Every NPC spawn is instantiated in every dimension; there is no vendor-specific exclusion.
- An NPC retains its base vendor-stock templates. Its dimension is applied to the item instance when purchased.
- The item template's `IsSPValue` flag selects SP versus gold; vendor dimension does not select currency.
- Runtime item `Value` and `SPValue` are independently multiplied by `3^dimension`.

Goose represents dimensions with cloned item and NPC templates rather than runtime composite identity. Goose also has donation credits and vendor-level currency, neither of which exists in Abyss. This plan makes the following Goose-specific adaptation:

- clone all vendor types, including credit dealers;
- preserve item and vendor `CurrencyId` through their existing copy constructors;
- multiply `Value` and `Credits` independently by `3^dimension`;
- never convert `Credits` into `Value`;
- continue using Goose's one selected `Value` as the price for an explicitly spirit-marked item. This preserves Goose's existing explicit-currency model but is not equivalent to Abyss's separate simultaneous gold and SP prices.

Credit-only stock is valid at credit dealers. A `Value = 0` credit item placed on an ordinary gold merchant is already a free base-world purchase under the generic purchase event; this plan will not broaden scope by changing that global behavior. The integration fixture will stop treating that invalid pairing as intended dimension behavior, while retaining an assertion that cloning does not synthesize a gold value from credits.

The checked-in Abyss Java supports `item_sp`, but its checked-in SQL schema does not contain that column. Code support for SP-valued items is established; committed or live SP item rows are not.

## APIs verified

- `ItemTemplate(ItemTemplate)` copies `Value`, `Credits`, and `CurrencyId`: `Goose/ItemTemplate.cs:124-160`.
- `NPCTemplate(NPCTemplate)` copies `CreditDealer`, `CurrencyId`, and vendor stock: `Goose/NPCTemplate.cs:214-266`.
- `Dimensions.ScaleItemTemplate` currently converts a credits-only price into `Value`, scales it, and forces `CurrencyId = "spirit"`: `Goose/Data/Illutia/Scripts/Global/Dimensions/Items.csx:95-128`.
- `Dimensions.CloneTemplates` currently excludes credit dealers: `Goose/Data/Illutia/Scripts/Global/Dimensions/Npcs.csx:75-99`.
- `Dimensions.RepointVendorStock` points a Goose dimension-vendor clone at Goose dimension-item clones: `Goose/Data/Illutia/Scripts/Global/Dimensions/Items.csx:188-228`.
- `CurrencyHandler.Resolve` chooses item override, then vendor currency, then gold: `Goose/Currency/CurrencyHandler.cs:33-50`.
- `CreditsCurrency` buys with `ItemTemplate.Credits` and refuses all sales: `Goose/Currency/CreditsCurrency.cs:11-17`.
- Purchase and sale events both use `CurrencyHandler.Resolve`: `Goose/Events/VendorPurchaseInventoryEvent.cs:79-108`, `Goose/Events/VendorSellInventoryEvent.cs:72-88`.
- Abyss instantiates every NPC spawn in every dimension: `../abyssserver/src/Abyss/NPCHandler.java:189-207`.
- Abyss purchase creates a dimension item from the vendor's base stock template and chooses currency from the item template: `../abyssserver/src/Abyss/Events/VendorPurchaseInventoryEvent.java:59-141`.
- Abyss sale chooses SP from the sold item and otherwise pays gold: `../abyssserver/src/Abyss/Events/VendorSellInventoryEvent.java:77-98`.
- Abyss independently scales runtime `Value` and `SPValue`: `../abyssserver/src/Abyss/Item.java:429-446`.

## Task 1: Add failing item and transaction regressions

**Files:**
- Modify: `Goose.IntegrationTests/SpiritCurrencyTests.cs`
- Modify: `Goose.IntegrationTests/DimensionVendorStockTests.cs`
- Modify: `Goose.IntegrationTests/DimensionTooltipStatsTests.cs`
- Modify: `Goose.IntegrationTests/DimensionsScriptTests.cs`

Add or rewrite tests before changing production scripts:

1. An unmarked base item's clone keeps `CurrencyId = null`, scales `Value`, and leaves the base template unchanged.
2. A base item explicitly marked `spirit` produces a clone that remains `spirit`; real purchase and sale events move spirit and leave gold unchanged.
3. A base-vendor purchase charges the base gold price and delivers the base template ID.
4. A dimension-vendor purchase charges scaled gold and delivers the dimension clone ID.
5. Selling a dimension item to both a dimension-zero vendor and a dimension vendor pays the same scaled `Value / 2` in gold and leaves spirit unchanged. This proves vendor dimension does not gate or select sale currency.
6. Inventory and vendor-stock tooltip packets label ordinary dimension gear as gold.
7. A credit-only clone keeps `Value = 0`, scales `Credits` independently, and does not acquire an item currency override.
8. Credit dealers receive dimension template/spawn clones, retain `CreditDealer` and `CurrencyId = credits`, stock the dimension item clone, and a real purchase charges scaled credits.
9. An explicit item currency still wins over an inherited vendor currency in the dimension integration path.
10. Update `Spirit_PricesBuysAtValueAndSellsAtHalf` so it uses an explicitly spirit-marked template rather than implying every dimension clone is spirit-priced.

Run the four focused test classes and confirm the changed expectations fail against the current forced-spirit and credit-dealer-exclusion implementation.

## Task 2: Preserve currency and scale each authored price independently

**File:**
- Modify: `Goose/Data/Illutia/Scripts/Global/Dimensions/Items.csx`

1. Remove the forced `CurrencyId = SpiritCurrencyId` assignment. The copy constructor will preserve a base item's explicit currency.
2. Set clone `Value` from `basic.Value * 3^dimension` only.
3. Set clone `Credits` from `basic.Credits * 3^dimension` independently, using checked integer conversion so an invalid price fails during load instead of wrapping.
4. Remove the credits-to-`Value` fallback.
5. Remove or rewrite stale comments claiming all dimension clones are spirit-priced. Keep `RepointVendorStock`; Goose needs cloned stock references because its persistence model encodes dimension in template IDs.

Run the focused item/vendor tests and confirm ordinary clones resolve to gold, explicit item overrides survive, and credit prices do not become gold prices.

## Task 3: Treat credit dealers like other dimension vendors

**File:**
- Modify: `Goose/Data/Illutia/Scripts/Global/Dimensions/Npcs.csx`

1. Remove the `!t.CreditDealer` filter from base NPC template cloning.
2. Let `NPCTemplate`'s copy constructor preserve the dealer's credit flags and currency.
3. Rely on the existing spawn cloning and vendor-stock repointing passes to create the dimension dealer and point eligible stock at scaled item clones.
4. Remove the obsolete comment that justified exclusion through forced spirit repricing.

Run `DimensionsScriptTests` and `DimensionVendorStockTests` to confirm clone, spawn, stock, currency, and real credit-purchase behavior.

## Task 4: Remove stale forced-spirit assumptions

**Files:**
- Modify: `Goose/Data/Illutia/Scripts/Global/Dimensions.csx`
- Modify: `Goose/Data/Illutia/Scripts/Global/Dimensions/DimensionConstants.csx`
- Modify: `Goose/Data/Illutia/Scripts/Global/Dimensions/SpiritCurrency.csx`
- Modify: `Goose/Currency/CurrencyHandler.cs`
- Modify: `Goose/ItemTemplate.cs`
- Modify only stale names/comments in the affected currency and dimension tests

Remove or rewrite only comments and test names that claim dimension items are inherently spirit-priced. Preserve the generic item-overrides-vendor rule and its tests. Spirit registration must remain because dimension commands still use it; registration no longer needs to precede item cloning for clone stamping.

Follow repository policy: do not add explanatory code comments unless a non-obvious invariant requires one, and remove stale comments on touched behavior rather than replacing them with equivalent narration.

## Task 5: Update current documentation

**File:**
- Modify: `docs/dimensions.md`

Update both the summary and Goose implementation sections:

- dimension item `Value` and `Credits` are independently scaled;
- item and vendor currencies are inherited rather than changed by dimension;
- ordinary dimension transactions use gold under checked-in loaders/scripts;
- credit dealers clone normally and continue charging credits;
- explicit item currency overrides the vendor as a Goose extension;
- there is no dimension-zero sale block;
- spirit remains used by dimension commands and rebirth;
- Abyss code supports SP items, but the checked-in SQL does not establish SP-valued game data;
- remove the obsolete vendor-override design and forced-spirit packet examples.

Do not rewrite historical implementation-plan documents.

## Task 6: Verify

1. Run focused integration tests for `SpiritCurrencyTests`, `DimensionVendorStockTests`, `DimensionTooltipStatsTests`, and `DimensionsScriptTests`.
2. Run relevant generic currency unit tests to ensure item → vendor → gold precedence is unchanged.
3. Run `dotnet test Goose.sln --no-restore`.
4. Review the diff for forced-spirit assumptions, credit-dealer exclusions, accidental global zero-price behavior changes, and unrelated files.

Baseline: `dotnet test Goose.sln` passes with 146/172 tools tests (26 skipped), 1,744/1,744 unit tests, and 461/461 integration tests. NuGet vulnerability-cache warnings are environmental and non-fatal.
