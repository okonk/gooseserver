# Arena Part 1: Data-Driven Currencies and `/wallet` — Implementation Plan

**Goal:** Let game data define currencies and tag item/NPC templates with one, replace
`credit_dealer`, and add a `/wallet` command listing every currency with its icon.

**Architecture:** A new optional `Currencies` sheet generates a `currencies` table. Each row
becomes a `DataCurrency : ICurrency` whose balance lives in `Player.Properties`. A new
`currency_id` column on Items and NPCs loads into the existing runtime
`ItemTemplate.CurrencyId` / `NPCTemplate.CurrencyId`, and unknown ids are cleared after global
scripts have registered theirs. `/wallet` opens an `OptionListWindow`.
Design: `docs/plans/2026-11-05-arena-design.md` (Part 1 of 4).

**Tech Stack:** .NET 10, C#, xUnit, System.Data.SQLite, ClosedXML, NLog.

**Conventions:** follow `AGENTS.md` — no new comments or doc strings unless the "why" is
non-obvious. Update or remove existing comments that become wrong (several mention
`credit_dealer`).

---

## APIs verified

| API | Location |
|---|---|
| `ICurrency` (Id, Name, ShortName, GetBalance, GetBuyPrice, GetSellPrice, Add, Remove) | `Goose/Currency/ICurrency.cs:9-31` |
| `CurrencyHandler.Register` throws on duplicate; `Get`; `Resolve(template, vendor)` item → vendor → gold, throws on unregistered | `Goose/Currency/CurrencyHandler.cs:46-82` |
| Built-ins registered in the `GameWorld` constructor | `Goose/GameWorld.cs:233-237` |
| `GoldCurrency` / `CreditsCurrency` sell rules (gold: half value, refuse 0; credits: refuse all) | `Goose/Currency/GoldCurrency.cs`, `Goose/Currency/CreditsCurrency.cs:27` |
| `Start()` load order: … Item Templates `:406` … NPC Templates `:435` … Global Scripts `:476` … Players `:490` | `Goose/GameWorld.cs:344-499` |
| `LoadStep(name, action, countFn)` | `Goose/GameWorld.cs:522` |
| `NPCHandler.LoadNPCTemplates` reuses existing template objects; `credit_dealer` → `CreditDealer` + `CurrencyId` | `Goose/NPCHandler.cs:67`, `:148-153` |
| `NPCHandler.GetTemplates()` | `Goose/NPCHandler.cs:19` |
| `ItemHandler.LoadTemplates` reuses existing objects (`GetTemplate(id) ?? new`) | `Goose/ItemHandler.cs:55-66` |
| `ItemHandler.GetTemplates()` | `Goose/ItemHandler.cs:41` |
| `ItemTemplate.CurrencyId` (runtime only today), copied by clone copy | `Goose/ItemTemplate.cs:118`, `:172` |
| `NPCTemplate.CreditDealer` / `CurrencyId`, copied at `:259-260` | `Goose/NPCTemplate.cs:199-203` |
| `NPC.CreditDealer` / `NPC.CurrencyId` pass-throughs | `Goose/NPC.cs:374-376` |
| `/reloadsql` reloads spell effects, spells, item templates, quests, goals, NPC templates on the game thread | `Goose/Commands/ReloadSqlCommand.cs:33-46` |
| `Player.Properties` (`PropertiesDictionary`), persisted in `players.player_properties` on every save | `Goose/Player.cs:474-487`, `:1015`, `:1255` |
| `PropertiesDictionary.GetProperty<T>(key, default)` converts numerics (JSON round-trip safe) | `Goose/PropertiesDictionary.cs:43-51`, `ConvertValue` |
| `OptionListWindow(player, world, title, lines, onLineClicked, npc, startPage, lineGraphics, …)`; graphic tuple `(Sheet, Graphic, R, G, B, A)` → `P.WindowLine` | `Goose/OptionListWindow.cs:12`, `:56-61` |
| Command pattern: `[Command("/x", Section=…, Help=…)] sealed class : BaseCommand` with `Execute(CommandContext)` | `Goose/Commands/CreditsCommand.cs` |
| Packet tooltip uses `Resolve(item.Template, null).Name` | `Goose/Packets.cs:569` |
| Column descriptors: `Col.Text/Int/Bool/Id`, `.PrimaryKey()`, `.HeaderText()` | `CsvToSql/CsvToSql.Core/Schema/Col.cs`, `Column.cs:73` |
| `SchemaRegistry.Tables` (emission order; `optional:` → DDL emitted even when the sheet is absent) | `CsvToSql/CsvToSql.Core/Schema/SchemaRegistry.cs:44-74`, `CsvToSqlConverter.cs:36-44` |
| NPC descriptors: `credit_dealer` at `:86`, header `"credit dealer (0)"` | `CsvToSql/CsvToSql.Core/NpcCsvToSql.cs:86` |
| Items descriptors end with `script_params` (no headers verified for Items) | `CsvToSql/CsvToSql.Core/ItemsCsvToSql.cs:66-69` |
| Header contract test covers Maps, NPCs, NPC Spawns, Warptiles | `Goose.IntegrationTests/Schema/WorkbookHeaderContractTests.cs:8-12` |
| Snapshot regeneration `GOOSE_UPDATE_SNAPSHOT=1` | `docs/testing.md` |
| SchemaGen command and drift guard `Checked_in_schema_js_is_up_to_date` | `tools/README.md` (SchemaGen), `tools/Tools.Tests/SchemaJsTests.cs` |
| DataEditor layout groups `credit_dealer` under "Links" | `tools/DataEditor/src/layout.js:50` |
| Integration DB harness `PlayerFirstSaveTestBase(schemaFiles, generatedTables)`; `SchemaDdl.For(table)` from descriptors | `Goose.IntegrationTests/PlayerFirstSaveTests.cs:7-45`, `TestSupport/SchemaDdl.cs` |
| Fast test fixtures: `TestWorldFixture`, `CommandPlayerOn`, `RunCommand`; `VendorFixture.VendorDealsIn` sets `CreditDealer` | `TestSupport/TestWorldFixture.cs:142-186`, `Goose.Tests/Fixtures/VendorFixture.cs:85-91` |

**Production data:** exactly one NPC has `credit_dealer = 1`: **180 "Credit Exchange"**
(checked against the production snapshot).

**Persistence strategy:**
- `currencies`, and the new `currency_id` columns, are **game data tables**: they arrive via the
  spreadsheet import (`updatesql` / `DataLinkId` re-import), which drops and recreates them. No
  migration code.
- Balances live in `players.player_properties`, which already exists and is migrated
  (`GameWorld.cs:287`). No schema change.
- **Manual live-sheet steps** (the implementer lists these for the user in the final report;
  they are not code):
  1. NPCs sheet: rename header `credit dealer (0)` → `currency id`, clear the `0`s, set NPC 180
     to `credits`.
  2. Items sheet: append a `currency_id` column after `script_params`.
  3. Add a `Currencies` sheet with headers matching the descriptor order, and an `arena_coin`
     row (`Arena Coins`, `ac`, icon `0`/`0`, buyback `0`).

---

### Task 1: `ICurrency` icon, registry enumeration, `DataCurrency`

**Files:**
- Modify: `Goose/Currency/ICurrency.cs`, `Goose/Currency/CurrencyHandler.cs`
- Create: `Goose/Currency/CurrencyIcon.cs`, `Goose/Currency/DataCurrency.cs`
- Test: `Goose.Tests/DataCurrencyTests.cs`, extend `Goose.Tests/CurrencyHandlerTests.cs`

**Contract:**

```csharp
public readonly record struct CurrencyIcon(int Sheet, int Graphic);

// ICurrency
CurrencyIcon? Icon => null;

// CurrencyHandler
public IReadOnlyList<ICurrency> All { get; }    // registration order

public sealed class DataCurrency : ICurrency
{
    public DataCurrency(string id, string name, string shortName, CurrencyIcon? icon, bool vendorBuyback);
    public void Update(string name, string shortName, CurrencyIcon? icon, bool vendorBuyback);
    public static string PropertyKey(string id) => "currency." + id;
}
```

- `GetBalance` = `player.Properties.GetProperty<long>(PropertyKey(Id), 0)`.
- `Add` / `Remove` write the new balance back to `player.Properties[PropertyKey(Id)]` as a
  `long`. Saturate at `long.MaxValue`, floor at `0`. `world` may be null (mirrors
  `CreditsCurrency`). No packet: nothing on the client shows the balance yet.
- `GetBuyPrice` = `template.Value * stack`.
- `GetSellPrice` = `-1` if `!vendorBuyback` or `item.Value == 0`, else `stack * item.Value / 2`.
- `Icon` = `null` when the row's sheet and graphic are both `0`.
- `All` keeps an insertion-ordered list beside the dictionary. `Register` adds to both.

**Mutation impact:**
- Source of truth: `player.Properties["currency.<id>"]` (new key).
- Readers: `DataCurrency.GetBalance`, the purchase check in `VendorPurchaseInventoryEvent`
  (via `ICurrency`), `/wallet` (Task 4), arena scripts (Part 3). Persisted by the existing save at
  `Player.cs:1015`.
- Derived state: none.
- Invariants: balance never negative; survives a JSON round-trip of `Properties` (stored `long`
  comes back as `JsonElement`/`long`, and `GetProperty<long>` converts both).

**Step 1: Red.** Tests:
- `Add_then_Remove_updates_balance`
- `Remove_below_zero_floors_at_zero` (adversarial)
- `Add_saturates_at_long_max` (adversarial: `long.MaxValue - 1` + `5`)
- `Balance_survives_properties_json_round_trip`: `Add(500)`, serialise `Properties` with
  `JsonHelper.Serialize`, reload with `player.LoadPropertiesFromColumn(json)`, assert 500.
- `GetSellPrice_refuses_when_buyback_disabled` / `GetSellPrice_pays_half_when_enabled` /
  `GetSellPrice_refuses_zero_value`
- `Icon_is_null_for_zero_sheet_and_graphic`
- `CurrencyHandlerTests.All_lists_builtins_then_registered_in_order`

Run: `dotnet test Goose.Tests --filter "FullyQualifiedName~DataCurrency|FullyQualifiedName~CurrencyHandlerTests"` → compile failure.

**Step 2: Green.** Implement. Re-run → PASS.

**Step 3: Commit** `feat(currency): DataCurrency, currency icons and registry enumeration`

---

### Task 2: Schema — `Currencies` sheet, `currency_id` columns, drop `credit_dealer`

**Files:**
- Create: `CsvToSql/CsvToSql.Core/CurrenciesCsvToSql.cs`
- Modify: `CsvToSql/CsvToSql.Core/Schema/SchemaRegistry.cs`, `CsvToSql/CsvToSql.Core/NpcCsvToSql.cs:86`,
  `CsvToSql/CsvToSql.Core/ItemsCsvToSql.cs` (append after `script_params`),
  `tools/DataEditor/src/layout.js:50`
- Modify (binary): `Goose.IntegrationTests/Fixtures/aspereta-data.xlsx`
- Regenerate: `Goose.IntegrationTests/Fixtures/generated.snapshot`, `tools/DataEditor/schema.js`,
  `tools/DataEditor/schema.json`

**Descriptors:**

```csharp
// CurrenciesCsvToSql
Col.Text("currency_id").PrimaryKey(),
Col.Text("name"),
Col.Text("short_name", def: "''"),
Col.Int("icon_sheet", SqlType.Int, def: 0),
Col.Int("icon_graphic", SqlType.Int, def: 0),
Col.Bool("vendor_buyback", def: false),

// NpcCsvToSql — replaces credit_dealer in the same position, so nothing after it shifts
Col.Text("currency_id", def: "''").HeaderText("currency id"),

// ItemsCsvToSql — appended last
Col.Text("currency_id", def: "''"),
```

Registry: `new TableSchema("Currencies", "currencies", new CurrenciesCsvToSql(), optional: true)`,
placed **first** in `Tables` (nothing references it, and templates conceptually depend on it).

**Mutation impact:**
- Source of truth: the descriptors.
- Readers: CsvToSql import (positional), `SchemaDdl` (test DDL), SchemaGen → `schema.js` /
  `schema.json`, `WorkbookHeaderContractTests` (NPCs), `CsvToSqlSnapshotTests`, DataEditor
  `layout.js`.
- Propagation: descriptors → fixture workbook → snapshot → SchemaGen → layout.
- Invariants: NPC columns from `quest_ids` onward do not shift; the Items
  `script_path`/`script_params` values do not move.

**Step 1: Descriptors** as above.

**Step 2: Red.** `dotnet test Goose.IntegrationTests --filter "FullyQualifiedName~WorkbookHeaderContract"`
→ FAIL on NPCs at the old `credit dealer (0)` position.

**Step 3: Fixture workbook.** In the scratchpad (not the repo), a throwaway console project
referencing `ClosedXML` at the version in `CsvToSql.Core.csproj`. It opens the fixture and:
- NPCs: find the column whose row-1 header is `credit dealer (0)`. Abort unless exactly one.
  Set the header to `currency id` and clear that column's data cells.
- Items: find the last used header column. Abort unless it is `script_params`. Write
  `currency_id` in the next column's row 1.
- Save.

**Step 4: Green.** Header contract test → PASS.

**Step 5: Snapshot.** `GOOSE_UPDATE_SNAPSHOT=1 dotnet test Goose.IntegrationTests --filter FullyQualifiedName~CsvToSqlSnapshot`,
then again without the flag → PASS. Review the diff: a new `currencies` DDL block; NPCs
`credit_dealer` → `currency_id` with empty values; Items gains `currency_id`; no other value
moves.

**Step 6: SchemaGen + editor.**
`dotnet run --project tools/SchemaGen -- tools/DataEditor/schema.js tools/DataEditor/schema.json`.
In `layout.js:50` replace `'credit_dealer'` with `'currency_id'`. Then
`dotnet test tools/Tools.Tests` and `node --test "tools/DataEditor/test/*.test.js"` → PASS.

**Step 7: Commit** `feat(currency): currencies sheet and currency_id columns replace credit_dealer`

---

### Task 3: Load currencies and template currency ids; remove `CreditDealer`

**Files:**
- Modify: `Goose/Currency/CurrencyHandler.cs` (add loader + validation),
  `Goose/NPCHandler.cs:148-153`, `Goose/ItemHandler.cs` (in `LoadTemplates`), `Goose/NPCTemplate.cs:196-203,259`,
  `Goose/NPC.cs:374`, `Goose/GameWorld.cs` (`Start`), `Goose/Commands/ReloadSqlCommand.cs:33-46`
- Modify tests that set `CreditDealer`: `Goose.Tests/Fixtures/VendorFixture.cs:85-91`,
  `Goose.IntegrationTests/DimensionVendorStockTests.cs:199`, `Goose.IntegrationTests/DimensionsScriptTests.cs:188,199`
- Test: `Goose.IntegrationTests/CurrencyDataLoadTests.cs` (new)

**Contract:**

```csharp
// CurrencyHandler
public void LoadDataCurrencies(GameWorld world);           // upsert from the currencies table
public int ClearUnknownTemplateCurrencies(GameWorld world); // returns how many were cleared
```

- `LoadDataCurrencies`: if `sqlite_master` has no `currencies` table, log a warning and
  return. For each row: `Get(id)` is a `DataCurrency` → `Update(...)`; is any other `ICurrency`
  → log error and skip (data cannot redefine `gold`/`credits`/`spirit`); is null →
  `Register(new DataCurrency(...))`. Rows removed from the sheet stay registered until restart.
  Log, don't throw.
- Template loaders: `currency_id` of `""` or `"0"` → `null`, otherwise the trimmed string. The
  `"0"` case covers a live sheet that still holds the old boolean values.
- `ClearUnknownTemplateCurrencies`: for every `ItemHandler.GetTemplates()` and
  `NPCHandler.GetTemplates()` whose non-empty `CurrencyId` has no `Get(id)`, log an error naming
  the template and id, and set it to `null`.
- Remove `NPCTemplate.CreditDealer`, its copy line, and `NPC.CreditDealer`. Fix the doc
  comment on `NPCTemplate.CurrencyId` (it cites the `credit_dealer` column).

**Wiring and order:**
- `Start()`: new `LoadStep("Currencies", () => this.CurrencyHandler.LoadDataCurrencies(this))`
  **before** "Item Templates" (`GameWorld.cs:406`). Validation runs **after** "Global Scripts"
  (`:476`), because spirit registers in `OnLoaded` and dimension clones copy `CurrencyId` there:
  `LoadStep("Currency Ids", () => this.CurrencyHandler.ClearUnknownTemplateCurrencies(this))`.
  It must run before "Players" (`:490`), which resolves currencies for inventories.
- `ReloadSqlCommand`: call `LoadDataCurrencies` first, before `LoadSpellEffects`, and
  `ClearUnknownTemplateCurrencies` last, after `LoadNPCTemplates`. All on the game thread, inside
  the existing `EnqueueCompletion`.

**Mutation impact:**
- Source of truth: `ItemTemplate.CurrencyId` / `NPCTemplate.CurrencyId` (now from data);
  `CurrencyHandler` registry.
- Readers: `CurrencyHandler.Resolve` from `VendorPurchaseInventoryEvent.cs:79`,
  `VendorSellInventoryEvent.cs:72`, `Packets.cs:569,630`. `Resolve` **throws** on an
  unregistered id, which is why validation must run before any player can trade.
- Derived state: dimension clone templates copy `CurrencyId` at clone time
  (`ItemTemplate.cs:172`, `NPCTemplate.cs:260`); validation iterates all templates, clones
  included.
- Invariants: after startup no template references an unregistered currency; credits
  vendors still trade in credits; script-stamped ids on clones survive (clones are not DB rows,
  so loaders never overwrite them).

**Step 1: Red.** Integration tests on `PlayerFirstSaveTestBase([], ["currencies", "item_templates", "npc_templates", "npc_drops", "npc_vendor_items"])`
(the last two because `LoadNPCTemplates` reads them; confirm against `QuestReloadTests`):
- `Data_currency_row_registers_and_resolves`
- `Reload_updates_an_existing_data_currency_in_place`: same instance, new name.
- `Data_row_cannot_redefine_gold` (adversarial): a `gold` row → `Get("gold")` is still `GoldCurrency`.
- `Missing_currencies_table_is_tolerated`: drop the table first; no throw.
- `Npc_currency_id_credits_resolves_to_credits`
- `Npc_currency_id_zero_is_treated_as_none` (adversarial for live sheets still holding `0`).
- `Item_currency_id_wins_at_a_gold_vendor_and_refuses_sale` (adversarial; the leak):
  `arena_coin` row with buyback 0, item tagged `arena_coin`, `Value 100`, sold to a gold vendor →
  refused, gold unchanged. Uses `VendorSellInventoryEvent` the way `VendorSellCurrencyTests.cs:10-18` does.
- `Unknown_template_currency_is_cleared_and_counted`

**Step 2: Green.** Implement. Fix the four test files that set `CreditDealer` (drop the
assignments and asserts; keep the `CurrencyId` asserts). Run
`dotnet test Goose.sln --no-restore` and
`dotnet test Goose.IntegrationTests --no-restore` → PASS.

**Step 3: Commit** `feat(currency): load data currencies and template currency ids`

---

### Task 4: `/wallet`

**Files:**
- Create: `Goose/Commands/WalletCommand.cs`
- Test: `Goose.Tests/WalletCommandTests.cs`

**Behaviour:**
- `[Command("/wallet", Section = "General", Help = "Show your currency balances.")]`
- One line per `CurrencyHandler.All`, in registration order:
  `$"{Capitalised(currency.Name)}: {currency.GetBalance(player):N0}"`.
- `lineGraphics`: `(icon.Sheet, icon.Graphic, 0, 0, 0, 0)` or all zeros when `Icon` is null.
- Title `"Wallet"`. Clicking a line does nothing. The window is not attached to an NPC.

**Step 1: Red.** Tests using `TestWorldFixture` + `CommandPlayerOn` + `RunCommand(player, "/wallet")`:
  `P.WindowLine` (`Packets.cs:493-497`) emits `WNF{windowId},{lineNo},{line}|0|0|{sheet}|{graphic}|{color}`,
  with color `*` when alpha is 0.
- `Wallet_lists_gold_and_credits_with_balances`: gold 1234 → a `WNF` packet containing
  `,Gold: 1,234|0|0|0|0|*`.
- `Wallet_includes_registered_data_currency_with_icon`: register a `DataCurrency` named
  `Arena Coins` with icon `(5, 7)` and add 40 → a `WNF` packet containing
  `,Arena Coins: 40|0|0|5|7|*`.
- `Wallet_line_click_closes_without_side_effects`.

**Step 2: Green.** Implement. Re-run → PASS.

**Step 3: Commit** `feat(currency): /wallet command`

---

### Task 5: Full verification

1. `dotnet test Goose.sln --no-restore` → all pass.
2. `dotnet test Goose.IntegrationTests --no-restore` → all pass.
3. `dotnet test tools/Tools.Tests` and `node --test "tools/DataEditor/test/*.test.js"` → pass.
4. `grep -rn "credit_dealer\|CreditDealer" Goose CsvToSql tools TestSupport Goose.Tests Goose.IntegrationTests --include=*.cs --include=*.js`
   → only the regenerated snapshot's history, nothing live.
5. The final report lists the three manual live-sheet steps.

## Invariant-to-test matrix

| Invariant | Proved by |
|---|---|
| Data balance persists through player properties | `Balance_survives_properties_json_round_trip` |
| Balance never negative / never wraps | `Remove_below_zero_floors_at_zero`, `Add_saturates_at_long_max` |
| Arena items cannot be sold for gold | `Item_currency_id_wins_at_a_gold_vendor_and_refuses_sale` |
| Data cannot hijack built-in currencies | `Data_row_cannot_redefine_gold` |
| Old `0` cells don't produce a bogus currency | `Npc_currency_id_zero_is_treated_as_none` |
| No template references an unregistered currency after load | `Unknown_template_currency_is_cleared_and_counted` |
| Credit Exchange still trades credits | `Npc_currency_id_credits_resolves_to_credits` + updated dimension tests |
| NPC columns after `currency_id` don't shift | `WorkbookHeaderContractTests` (NPCs), snapshot diff review |
| Editor schema current | `Checked_in_schema_js_is_up_to_date` |
