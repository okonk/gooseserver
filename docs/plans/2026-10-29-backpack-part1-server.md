# Backpack Part 1 (Server) Implementation Plan

**Goal:** Add an equippable, upgradeable, storage-only backpack to the server: equip slot,
per-player persisted contents, a grid window toggled by `OBP`, and a quest-script upgrade path.

**Architecture:** The backpack container is owned by `Inventory` exactly like the combine bag,
persisted in a new `players.backpack` column that `Inventory` reads and writes itself. Size lives
on the equipped item (`ItemProperty.BackpackSlots`/`BackpackColumns`, template `properties`
fallback). A non-empty backpack can never leave the equip slot, enforced in the single unequip
choke point `Inventory.UnequipCore`. `BackpackWindow : ItemContainerWindow` reuses all
drag/swap code and sends its size in `MKW`'s two trailing ints.

**Tech Stack:** C# / .NET 10, System.Data.SQLite, System.Text.Json, xUnit, Roslyn `.csx` scripts.

**Design:** `docs/plans/2026-10-29-backpack-design.md`. This is Part 1 of 2; Part 2 is the
Godot client: `Goose2ClientGodot/docs/plans/2026-10-29-backpack-part2-client.md` (branch `backpack`).

**Conventions:** Follow `AGENTS.md`: no new comments or doc strings unless the "why" is
non-obvious (invariants, wire format). Commit messages follow the repo style
(`feat(backpack): ...`).

---

## APIs verified

| API | Location |
|-----|----------|
| `Inventory.EquipSlots` (Mount = 14 is last) | `Goose/Inventory.cs:17-33` |
| `Inventory` ctor, arrays sized `size + 1` | `Goose/Inventory.cs:44-54` |
| `Inventory.EquipCore` (unequips target slot via `UnequipCore`, then `RemoveItem`, sets `equipped[]`, `SendEquippedSlot`) | `Goose/Inventory.cs:357-430` |
| `Inventory.Unequip(EquipSlots)` → `UnequipCore` → `FlushEquipEvents` | `Goose/Inventory.cs:606-611` |
| `Inventory.UnequipCore` (the only code that clears an equip slot at runtime) | `Goose/Inventory.cs:613-673` |
| `Inventory.Unequip(int id)` wire id = `InventorySize + slot + 1` | `Goose/Inventory.cs:703-710` |
| `Inventory.GetEquippedSlot(EquipSlots)` indexes the array directly (throws if slot > `EquippedSize`) | `Goose/Inventory.cs:778-781` |
| `Inventory.SendEquippedSlot` | `Goose/Inventory.cs:913-926` |
| `Inventory.HasItem(int)` (inventory, equipped, combine) | `Goose/Inventory.cs:933-950` |
| `Inventory.BuildSave` (runs inside the player save transaction) | `Goose/Inventory.cs:1114-1151` |
| `Inventory.Load` (equipped loop with two discard branches; combine slot resolve pattern) | `Goose/Inventory.cs:1157-1253` |
| Equipped discard branches (null item / missing template) | `Goose/Inventory.cs:1194-1207` |
| `Player.HasItem(int)` = inventory ∪ bank; all lore gates call it | `Goose/Player.cs:1859-1862` |
| Lore gate callers | `CommunityChestWindow.cs:64`, `Inventory.cs:1403`, `Events/PickupItemEvent.cs:105`, `Events/VendorPurchaseInventoryEvent.cs:72`, `Map.cs:883` |
| Player save order: player row, then `Inventory.BuildSave()`, same transaction | `Goose/Player.cs:1068-1069`, `:1094-1100` |
| `Player.LoadAdditional` calls `Inventory.Load` | `Goose/Player.cs:910-914` |
| `Player.Windows`, `Player.LastWindowID` | `Goose/Player.cs:388-389`, init `:579-580` |
| Mount-slot-exists guard pattern (`(int)slot > EquippedSize`) | `Goose/Player.cs:2621-2622` |
| `GameWorld.MigrateDatabaseSchema`, `AddColumnIfMissing` | `Goose/GameWorld.cs:279-292`, `:314-321` |
| `players` DDL (`player_properties` is last column) | `Goose/sql/players.sql:1-56` |
| `Item.ItemProperties`, `GetProperty<T>` (handles `JsonElement`), `HasProperty` | `Goose/Item.cs:74-75`, `:228-246` |
| `Item.LoadFromTemplate` sets `Description = Template.Description` | `Goose/Item.cs:152-163` |
| `ItemProperty` enum | `Goose/Item.cs:11-15` |
| `ItemTemplate.ItemSlots` (Mount = 13, Misc = 20) | `Goose/ItemTemplate.cs:24-40` |
| `ItemTemplate.Properties` + `hideOtherSlots` precedent | `Goose/ItemTemplate.cs:119-121` |
| `PropertiesDictionary.GetProperty<T>(key, default)` | `Goose/PropertiesDictionary.cs:43-51` |
| `ItemContainer.Resize`, `SetSlot`, `GetSlot`, enumerable | `Goose/ItemContainer.cs:21-25`, `:35-50`, `:63-72` |
| `ItemContainerWindow` (Populate 1..MaxSlots-1, swap/validate, `ValidateSlotIndex`) | `Goose/ItemContainerWindow.cs:1-128` |
| `Window.WindowFrames`, `Window.WindowTypes`, `Window.Clicked` (default ignores unknown types), `SendCreate`, `Close` (sends `CLW`) | `Goose/Window.cs:23-57`, `:64-87`, `:132-163`, `:315-338` |
| `P.MakeWindow` (trailing `"0,0"`) | `Goose/Packets.cs:754-763` |
| `P.GenericWindowSlot` / `P.ClearGenericWindowSlot` | `Goose/Packets.cs:645-650` |
| `P.EquipSlot` wire id = slot + 31 | `Goose/Packets.cs:662-670` |
| `CommunityChestWindow` (window id `++player.LastWindowID`, `SendSlot`, `Clicked`) | `Goose/CommunityChestWindow.cs:24-36`, `:172-213` |
| `OpenCombineBagEvent` (event pattern), `EventHandler` registration table | `Goose/Events/OpenCombineBagEvent.cs`, `Goose/EventHandler.cs:145` |
| Window events route by `window.ID` in `player.Windows` | `Goose/Events/WindowToInventoryEvent.cs:30-38` |
| `DestroyItemEvent` unequips first and aborts on failure | `Goose/Events/DestroyItemEvent.cs:57` |
| `BaseQuestScript` hooks (`CanComplete`, `GiveReward`, `GetRequiredInventorySpace`) | `Goose/Scripting/BaseQuestScript.cs:25-38` |
| Script reward `CanComplete` runs before requirements are taken | `Goose/Quests/QuestWindow.cs:386-395`, `:397-432` |
| Shipped-script test pattern | `Goose.Tests/ClassRewardScriptTests.cs:14-24`, `:287+` |
| `QuestScriptFixture` (EquippedSize 20) | `Goose.Tests/Fixtures/QuestScriptFixture.cs:11-20` |
| `VendorFixture` (`Player` is a capturing player on a map) | `Goose.Tests/Fixtures/VendorFixture.cs:12-29` |
| Equip test pattern via `VendorFixture` | `Goose.Tests/DestroyItemEventTests.cs:18-45` |
| `PlayerFirstSaveTestBase` (reads shipped `sql/*.sql`, EquippedSize 20) | `Goose.IntegrationTests/PlayerFirstSaveTestBase.cs` |
| Equipped-load integration pattern | `Goose.IntegrationTests/MountLoadTests.cs` |
| `SchemaMigrationTests` | `Goose.Tests/SchemaMigrationTests.cs` |
| CsvToSql item slot enum (DataEditor source) | `CsvToSql/CsvToSql.Core/ItemsCsvToSql.cs:39`, `:91-107` |
| `schema.js` regen + drift test | `tools/README.md:48-60`, `tools/Tools.Tests` `Checked_in_schema_js_is_up_to_date` |
| Editor test pinning the item_slot enum list | `tools/DataEditor/test/appearance.test.js:365-380` |

## Test commands

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --no-restore --filter "FullyQualifiedName~Backpack"
dotnet test Goose.IntegrationTests/Goose.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~Backpack"
dotnet test Goose.sln --no-restore
node --test "tools/DataEditor/test/*.test.js"
```

Known baseline failure, unrelated: `LogSearchDeliveryTests.ValidationAndDbErrorsEmitPacedLrxOnly_NlogRetainsFullDbException`.

## Cross-cutting invariants

1. **Backpack slot may not exist.** `EquippedSize` < 15 (e.g. Aspereta configs) means no
   backpack. Every backpack path checks `Inventory.HasBackpackSlot` before calling
   `GetEquippedSlot(EquipSlots.Backpack)`, which would otherwise index out of range
   (`Inventory.cs:778-781`).
2. **Non-empty ⇒ equipped.** If `backpackContainer` holds any item, the backpack slot holds a
   backpack. The only runtime writer that clears the slot is `UnequipCore`.
3. **Container size = equipped backpack's `BackpackSlots + 1`** at all times while equipped;
   `ItemContainer(1)` (no usable slots) when not equipped.
4. **Preserved contents are never overwritten.** If load could not resolve the equipped
   backpack, the raw column JSON is kept verbatim, re-saved verbatim, and equipping any
   backpack is refused until a GM fixes the data and the player relogs.

---

### Task 0: Prerequisites (enums, settings, schema, window hook)

**Files:**
- Modify: `Goose/Inventory.cs:17-33` — add `Backpack` after `Mount` (= 15).
- Modify: `Goose/ItemTemplate.cs:24-40` — add `Backpack` after `Mount` (= 14).
- Modify: `Goose/Inventory.cs` `ItemSlotToEquipSlot` (`:719-775`) — map `ItemSlots.Backpack → EquipSlots.Backpack`.
- Modify: `Goose/Item.cs:11-15` — add `BackpackSlots`, `BackpackColumns` to `ItemProperty` (append; values are serialized).
- Modify: `Goose/Window.cs` — `WindowFrames.GridContainer = 32`; `WindowTypes.Backpack` (append after `CommunityGoal`); add `public virtual int FrameParam1 => 0;` and `public virtual int FrameParam2 => 0;`.
- Modify: `Goose/Packets.cs:754-763` — `P.MakeWindow` emits `window.FrameParam1 + "," + window.FrameParam2` instead of literal `"0,0"`.
- Modify: `Goose/GooseSettings.json:14` and `:36` — `EquippedSize` 14 → 15.
- Modify: `Goose/sql/players.sql` — add `backpack TEXT DEFAULT '' NOT NULL` after `player_properties`.
- Modify: `Goose/GameWorld.cs:284` — `AddColumnIfMissing(conn, "players", "backpack", "TEXT DEFAULT '' NOT NULL");`
- Modify: `CsvToSql/CsvToSql.Core/ItemsCsvToSql.cs:91-107` — add `Backpack` after `Mount`.
- Regenerate: `dotnet run --project tools/SchemaGen -- tools/DataEditor/schema.js` (only `schema.js` is tracked).
- Modify: `tools/DataEditor/test/appearance.test.js:365-380` — add `'Backpack'` to the never-drawn list and the pinned enum list ("sixteen members").
- Test: `Goose.Tests/BackpackPrerequisiteTests.cs` (new).

**Mutation impact:**
- `EquippedSize` 14→15 grows `equipped` to 16 entries. Readers: `Inventory.SendAll` (`:206`, sends `CIS46` for the empty slot; old clients drop it via the bounds check at Goose2ClientGodot `CharacterWindow.cs:113-114`), `GetEquippedSlot(int)` (`:790-800`), `InventoryUseEvent.cs:40`, `LoadSlots` padding (`:1255-1288`, logs a Warn "has 15 slots, expected 16" once per player on first login; acceptable).
- `P.MakeWindow` output is unchanged for every existing window (both params default 0).
- No derived state from the enum additions.

**Step 1: failing tests**
- `MakeWindow_UsesFrameParams`: a `Window` subclass overriding `FrameParam1 => 20`, `FrameParam2 => 5` produces a packet ending in `",20,5"`; a plain `Window` still ends in `",0,0"` (regression guard).
- `ItemSlotToEquipSlot_Backpack`: returns `EquipSlots.Backpack`.
- `AddColumnIfMissing` for `backpack` follows `SchemaMigrationTests` exactly (idempotent).

**Step 2:** red (compile failure on the missing members). **Step 3:** implement. **Step 4:** green, plus
`node --test "tools/DataEditor/test/*.test.js"` and `dotnet test tools/Tools.Tests` (schema drift).

**Step 5: Commit** `feat(backpack): add backpack slot enums, window frame and players.backpack column`

---

### Task 1: Item backpack size and derived description

**Files:**
- Modify: `Goose/Item.cs` — add:
  - `[JsonIgnore] public int BackpackSlots` — `HasProperty(ItemProperty.BackpackSlots) ? GetProperty<int>(...) : Template.Properties.GetProperty("backpackSlots", 0)`.
  - `[JsonIgnore] public int BackpackColumns` — same with `"backpackColumns"`, default `5`.
  - `public void SetBackpackSize(int slots, int columns)` — writes both `ItemProperties`, then `RefreshBackpackDescription()`.
  - `public void RefreshBackpackDescription()` — `Description = Template.Description + " Holds N items."` (trim a leading space when the template description is empty). Space separator, not a newline: description travels inside the item slot packet.
- Test: `Goose.Tests/BackpackItemTests.cs` (new).

**Mutation impact:**
- Source of truth: `Item.ItemProperties` (persisted in item JSON, `Item.cs:74-75`) and `Item.Description` (persisted, `Item.cs:36`).
- Readers: item slot packets (`P.ItemSlot` via `SendEquippedSlot`/`SendSlot`), `CloneWithoutId` copies both (`Item.cs:193-224`).
- Propagation: setters only mutate the item. Callers (Tasks 3, 5) must call `Inventory.SendEquippedSlot(EquipSlots.Backpack, world)` afterwards; this task does not send.
- Invariant: calling `RefreshBackpackDescription` twice does not append twice (it rebuilds from `Template.Description`).

**Tests (red → green):**
- Template-only: template `Properties["backpackSlots"]=10` → `BackpackSlots == 10`, `BackpackColumns == 5`.
- Item override wins over template.
- Round trip: `SetBackpackSize(20, 5)`, `JsonHelper.Serialize` → `Deserialize<Item>`, re-attach `Template` → `BackpackSlots == 20` (proves the `JsonElement` path).
- Adversarial: `RefreshBackpackDescription` called twice yields exactly one "Holds 20 items."
- Template with no property and no override → `BackpackSlots == 0`.

**Commit** `feat(backpack): per-item backpack size with derived description`

---

### Task 2: Backpack container and persistence

**Files:**
- Modify: `Goose/Inventory.cs`:
  - Field `ItemContainer backpackContainer = new(1)`; field `string? preservedBackpackJson`.
  - `public bool HasBackpackSlot => (int)EquipSlots.Backpack <= settings.EquippedSize;`
  - `public ItemContainer GetBackpackContainer()`.
  - `public Item? EquippedBackpack => HasBackpackSlot ? GetEquippedSlot(EquipSlots.Backpack)?.Item : null;`
  - `public bool BackpackHasItems => backpackContainer.Any(s => s is not null);`
  - `public bool BackpackContentsPreserved => preservedBackpackJson is not null;`
  - `Load`: inside the existing `world.Database.Execute` callback, after the equipped loop. In the equipped loop's two discard branches (`:1194-1207`) record `backpackDiscarded = true` when `i == (int)EquipSlots.Backpack`.
  - `BuildSave`: snapshot `preservedBackpackJson ?? JsonHelper.Serialize(backpackContainer)` on the game thread and execute `UPDATE players SET backpack=@backpack WHERE player_id=@player_id` inside the returned action.
- Optional refactor: extract the combine-bag slot-resolve block (`:1229-1250`) into `private bool TryResolveLoadedSlot(ItemSlot slot, int playerId, GameWorld world)` and reuse it for the backpack. Pure extraction: same logs, same `AddItem`/`Template`/`RefreshStats` sequence.
- Test: `Goose.IntegrationTests/BackpackPersistenceTests.cs` (new, `PlayerFirstSaveTestBase(["players", "banks"])`, following `MountLoadTests`).

**Load algorithm** (raw = `SELECT backpack FROM players WHERE player_id=@id`; a missing row or empty string = no contents):
1. `!HasBackpackSlot` → if raw has items, log Error with the raw JSON; container stays `new(1)`.
2. `backpackDiscarded` and raw has items → `preservedBackpackJson = raw`; log Error "backpack contents preserved, backpack item could not be loaded"; container stays `new(1)`.
3. Backpack equipped → `item.RefreshBackpackDescription()`; container `new(item.BackpackSlots + 1)`; deserialize raw as `ItemSlot?[]`; resolve each slot (template lookup, `AddItem`, `RefreshStats`); slots at index ≥ container size are logged ("excess discarded") and dropped, matching `PlayerBank.cs:82-83`; corrupt JSON → log Error, start empty (matches `LoadSlots`).
4. No backpack equipped (slot was empty in the save) and raw has items → log Error once per item (template id, name, stack, item JSON), leave container `new(1)`. The next save writes the empty container, which is the "clear".
5. If the equipped item's template slot is not `ItemSlots.Backpack` → log Warn only; treat it as a backpack with `BackpackSlots` (likely 0).

**Mutation impact:**
- Source of truth: `players.backpack` (persisted) ↔ `Inventory.backpackContainer` (runtime).
- Readers: `BuildSave`, `HasItem` (Task 3), `BackpackWindow` (Task 4), upgrade (Task 5).
- Propagation: load runs before the player is in the world (`Player.LoadAdditional`), so no packets are sent; the client gets the equip slot through the existing `SendAll` at login.
- Save ordering: the player row is written before `Inventory.BuildSave()` in the same transaction (`Player.cs:1068-1069`), so the `UPDATE` hits an existing row for brand-new players too. Integration test proves this.
- Invariants: round trip preserves items and stacks; preserved JSON is re-saved byte-for-byte; a failed save transaction rolls back the backpack with everything else.

**Invariant → test matrix:**

| Invariant | Proved by |
|-----------|-----------|
| Round trip | `Backpack_contents_round_trip_through_save_and_load` (equip a backpack with `SetBackpackSize(10,5)`, put 2 items in slots 1 and 10, save via `Player.SaveToDatabase` path used by `GuildSaveTests`/`PlayerFirstSaveTestBase`, new `Inventory`, `Load`, assert slots) |
| New player first save writes the column | `First_save_of_a_new_player_persists_backpack` (`AutoCreatedNotSaved = true`) |
| Orphans cleared and logged | `Contents_without_an_equipped_backpack_are_logged_and_cleared` (seed `equipped` with no slot 15 and `players.backpack` with items; load; assert container empty; save; assert column holds no items; assert log via `CapturingLog` if the integration project has an equivalent, otherwise assert only state) |
| **Adversarial:** template deletion does not wipe | `Contents_are_preserved_when_the_backpack_template_is_missing` (equipped slot 15 has an unknown template id; load; save; assert the column is byte-identical to the seeded JSON) |
| Excess dropped | `Contents_beyond_backpack_size_are_discarded` (10-slot backpack, item at index 15) |
| Column migration | `MigrateDatabaseSchema_adds_backpack_to_an_old_players_table` |
| Old 15-entry equipped blob loads | Covered by `MountLoadTests` style: seed a 15-entry `equipped` array with the settings at 15, assert no exception and slot 15 is null |

**Commit** `feat(backpack): persist backpack contents in players.backpack`

---

### Task 3: Equip/unequip rules and lore visibility

**Files:**
- Modify: `Goose/Inventory.cs`:
  - `EquipCore` (`:357`): if `equipslot == EquipSlots.Backpack` and `!HasBackpackSlot` → return false. If `BackpackContentsPreserved` → notice "Your backpack's contents need GM attention before you can equip a backpack." and return false (invariant 4).
  - `EquipCore`, after `this.equipped[(int)equipslot] = slot;`: if Backpack → `slot.Item.RefreshBackpackDescription()` then `backpackContainer = new ItemContainer(slot.Item.BackpackSlots + 1)`. Replacing the instance is safe here because invariant 2 guarantees it is empty and no window references it (Task 4 closes windows on unequip). Log Warn if `BackpackSlots == 0`.
  - `UnequipCore` (`:613`), first lines after the null check: if `equipslot == EquipSlots.Backpack && BackpackHasItems` → `world.Send(player, P.Notice(NoticeType.Info, "Empty your backpack first."))`, return false. After a successful backpack unequip: `backpackContainer = new ItemContainer(1)`.
  - `HasItem(int)` (`:933`): add a loop over `backpackContainer`.
- Test: `Goose.Tests/BackpackEquipTests.cs` (new, `VendorFixture`; template `Slot = ItemSlots.Backpack`, `UseType = Armor`, `Properties["backpackSlots"] = 10`).

**Mutation impact:**
- Source of truth: `Inventory.equipped[15]`, `Inventory.backpackContainer`.
- Readers: every `UnequipCore` caller. `Unequip(EquipSlots)` (`:606`), `Unequip(int)` from `InventoryUseEvent.cs:42` and `DestroyItemEvent.cs:57`, `EquipCore` target-slot swap (`:369-372`), `UnequipConflictingSlots` (weapon/shield only, never Backpack).
- Failure: a refused `UnequipCore` returns before `AddItem`, so nothing is moved. `EquipCore` already returns false on `UnequipCore` failure before removing the new item from inventory (`:369-372`), so a refused swap leaves both items where they were.
- `HasItem` feeds every lore gate via `Player.HasItem` (`Player.cs:1859-1862`); no other readers change. `HasItem(id, n)`, `RemoveItem` and the family helpers stay inventory-only (storage-only decision).

**Invariant → test matrix:**

| Invariant | Proved by |
|-----------|-----------|
| Equip sizes container | `Equipping_a_backpack_sizes_the_container_from_the_item` (`MaxSlots == 11`, description ends "Holds 10 items.") |
| Unequip refused when non-empty | `Unequip_is_refused_while_the_backpack_holds_items` (state unchanged, notice sent, item still equipped, not in inventory) |
| **Adversarial:** swap path | `Equipping_a_second_backpack_is_refused_while_the_first_holds_items` (second backpack stays in inventory, first stays equipped, contents intact) |
| **Adversarial:** destroy path | `Destroying_an_equipped_non_empty_backpack_is_refused` (via `DestroyItemEvent` like `DestroyItemEventTests.cs:32-38`) |
| Empty unequip works | `Unequipping_an_empty_backpack_returns_it_to_inventory` (container `MaxSlots == 1`) |
| Lore sees the backpack | `Lore_item_in_backpack_blocks_picking_up_another` (put a lore item in the container, assert `player.HasItem(id)`; plus one real gate: `fixture.Stock(1, loreTemplate)` (`VendorFixture.cs:67`) then a `VendorPurchaseInventoryEvent`, invoked the way `Goose.Tests/VendorPurchaseCurrencyTests.cs` does, and assert the inventory gains nothing (`Events/VendorPurchaseInventoryEvent.cs:72`)) |
| Preserved contents block equip | `Equip_is_refused_while_backpack_contents_are_preserved` (set the field via a test seam: `internal void PreserveBackpackJsonForTest(string)` or reflection, following `TestWorldFixture` reflection style) |
| No backpack slot configured | `Backpack_cannot_be_equipped_when_EquippedSize_is_14` |

**Commit** `feat(backpack): equip rules, empty-to-unequip and lore visibility`

---

### Task 4: BackpackWindow, OBP toggle, open on equip, close on unequip

**Files:**
- Create: `Goose/BackpackWindow.cs`:
  - `BackpackWindow : ItemContainerWindow`; ctor `(GameWorld world, Player player)`: `ItemContainer = player.Inventory.GetBackpackContainer()`, `ID = ++player.LastWindowID`, `Title = "Backpack"`, `Buttons = "0,1,0,0,0"`, `Frame = WindowFrames.GridContainer`, `Type = WindowTypes.Backpack`, then `SendCreate`.
  - `FrameParam1 => ItemContainer.MaxSlots - 1`; `FrameParam2 => player's equipped backpack BackpackColumns` (store the columns in a field set at construction and on `Resize`).
  - `SendSlot`: same as `CommunityChestWindow.SendSlot` (`:172-183`).
  - `Clicked`: `Exit`/`Close` → `player.Windows.Remove(this)`; otherwise base.
  - `public static void Toggle(GameWorld world, Player player)`: existing `Backpack` window → `existing.Close(player, world)`; else if `player.Inventory.EquippedBackpack is null` → notice "You don't have a backpack equipped."; else `Open`.
  - `public static void Open(GameWorld world, Player player)`: remove any existing backpack window from `player.Windows` (no `CLW`; the new `MKW` has a new id and the old one is closed with `Close` to keep the client in sync, since the client is multi-instance per id), then add a new one.
  - `public void Resize(Player player, GameWorld world)`: rebind `ItemContainer` to the current `GetBackpackContainer()`, update columns, `SendCreate` (same window id, so the client rebuilds in place).
  - `public static void CloseFor(Player player, GameWorld world)`: `Close` every `Backpack` window.
- Create: `Goose/Events/ToggleBackpackEvent.cs` — like `OpenCombineBagEvent`, `State == Ready` → `BackpackWindow.Toggle`.
- Modify: `Goose/EventHandler.cs:145` — register `("OBP", Open(typeof(ToggleBackpackEvent)))`.
- Modify: `Goose/Inventory.cs`:
  - `Equip` (public wrapper, `:345-356`): after a successful equip of a backpack (`item.Slot == ItemSlots.Backpack`), call `BackpackWindow.Open(world, player)`. In the wrapper, not `EquipCore`, so the window opens after the equip packets are sent and the equip events are flushed.
  - `UnequipCore`: after a successful backpack unequip, `BackpackWindow.CloseFor(player, world)` before replacing the container.
- Test: `Goose.Tests/BackpackWindowTests.cs` (new, `VendorFixture`).

**Lifecycle / publication:**
- Create order: construct → bind container → assign id → `SendCreate` (MKW, slots, ENW) → add to `player.Windows`. Window events can only find it after it's added, and all of this runs on the game thread, so nothing observes the intermediate state.
- Teardown order: `Close` = remove from `player.Windows` and send `CLW` (`Window.cs:333-337`). Client-initiated close (WBC) = remove only.
- Container identity: a window holds a reference to the container. The container is replaced only on equip (when no window is open, since equip opens a new window afterwards) and on unequip (after `CloseFor`). Upgrade (Task 5) resizes **in place** and calls `Resize`.

**Mutation impact:**
- `player.Windows` (runtime registry). Readers: `WindowToInventoryEvent`, `InventoryToWindowEvent`, `WindowToWindowEvent`, `WindowButtonClickEvent` (lookup by id).
- Client observes `MKW`/`GWS`/`GWC`/`ENW`/`CLW`.

**Invariant → test matrix:**

| Invariant | Proved by |
|-----------|-----------|
| MKW carries size | `Open_sends_MKW_with_slot_count_and_columns` (assert `Sent` contains `MKW{id},32,Backpack,0,1,0,0,0,0,10,5`) |
| Toggle closes | `OBP_twice_opens_then_closes` (second run sends `CLW{id}`, `player.Windows` has no backpack window) |
| No backpack | `OBP_without_a_backpack_sends_a_notice_and_opens_nothing` |
| Drag in/out | `Dragging_inventory_to_backpack_and_back_moves_the_item` (via `InventoryToWindow`/`WindowToInventory`) |
| **Adversarial:** out-of-range slot | `Dragging_to_slot_past_backpack_size_is_ignored` (slot 11 on a 10-slot backpack; inventory item stays) |
| Equip opens | `Equipping_a_backpack_opens_its_window` |
| **Adversarial:** unequip closes, no stale window | `Unequipping_closes_the_window_and_drags_to_its_old_id_do_nothing` |

**Commit** `feat(backpack): backpack window and OBP toggle`

---

### Task 5: Upgrade API and BackpackUpgrade quest script

**Files:**
- Modify: `Goose/Inventory.cs` — `public bool UpgradeBackpack(int slots, int columns, GameWorld world)`:
  - Preconditions: `EquippedBackpack` not null; `slots + 1 >= backpackContainer.MaxSlots` (never shrinks). Returns false otherwise, no mutation.
  - Sequence: `item.SetBackpackSize(slots, columns)` → `backpackContainer.Resize(slots + 1)` (in place; `ItemContainer.Resize` keeps existing slots, `ItemContainer.cs:21-25`) → `SendEquippedSlot(EquipSlots.Backpack, world)` (tooltip/description) → for each open `BackpackWindow` in `player.Windows`: `Resize(player, world)`.
- Create: `Goose/Data/Illutia/Scripts/Quest/BackpackUpgrade.csx` (`BaseQuestScript`, modeled on `ClassReward.csx`):
  - Params `{"from": int, "to": int, "columns": int?}`, parsed per call from `reward.ScriptParams`, never cached in a field.
  - `CanComplete`: malformed params / `to <= from` → `"This quest's reward is misconfigured.\\nPlease report this to a GM."` (same string as `ClassReward.csx`); no equipped backpack → `"You need to be wearing your backpack."`; `BackpackSlots != from` → `$"This upgrade is for a {from}-slot backpack."`; else null.
  - `GiveReward`: re-validate (defensive, same as `ClassReward.Resolve`); `player.Inventory.UpgradeBackpack(to, columns ?? item.BackpackColumns, world)`; `world.Send(player, P.ServerMessage($"[Quest Reward]: Backpack upgraded to {to} slots."))`.
  - `GetRequiredInventorySpace` → 0.
- Test: `Goose.Tests/BackpackUpgradeScriptTests.cs` (new; `QuestScriptFixture`; compile the shipped script once via `Lazy<IQuestScript>` like `ClassRewardScriptTests.cs:14-24`; reuse its `IllutiaDataDirectory` helper by copying it, or move it to `Goose.Tests/Fixtures` if a second user justifies it).

**Mutation impact:**
- Source of truth: item `ItemProperties` + `Description`, `backpackContainer` size.
- Readers: `BuildSave` (next save persists both), `SendEquippedSlot`, open `BackpackWindow` (`FrameParam1/2`, `Populate`), `ValidateSlotIndex` (uses `ItemContainer.MaxSlots`, so new slots become valid immediately).
- `Resize` sends a fresh `MKW` + all slots + `ENW` for the same window id; `Populate` iterates `1..MaxSlots-1` (`ItemContainerWindow.cs:9-15`), so the new empty slots are cleared on the client.
- Failure: `UpgradeBackpack` validates before mutating; `CanComplete` runs before `TakeRequirements` (`QuestWindow.cs:386-432`), so a blocked upgrade costs nothing.

**Invariant → test matrix:**

| Invariant | Proved by |
|-----------|-----------|
| Upgrade in place keeps contents | `Upgrade_from_10_to_20_keeps_items_and_grows_container` (item in slot 7 survives; `MaxSlots == 21`; `BackpackSlots == 20`; description "Holds 20 items.") |
| **Adversarial:** wrong tier blocked | `Upgrade_for_20_slot_backpack_is_refused_on_a_10_slot_one` (CanComplete message; GiveReward not called) |
| **Adversarial:** double completion | `Running_the_same_tier_twice_is_refused_the_second_time` |
| No backpack | `Upgrade_without_a_backpack_is_refused` |
| Misconfigured | `Bad_params_or_to_not_greater_than_from_is_misconfigured` (`""`, `"{}"`, `{"from":20,"to":10}`) |
| Open window resizes | `Upgrade_resends_MKW_for_an_open_backpack_window` (`UpgradeBackpack` directly on a `VendorFixture` player with an open window; assert the latest `MKW{id}` ends `,20,5`) |
| Never shrinks | `UpgradeBackpack_to_fewer_slots_returns_false_and_changes_nothing` |

**Commit** `feat(backpack): BackpackUpgrade quest script`

---

### Task 6: Full verification and design doc sync

- Run all four test commands. Expected: only the known `LogSearchDeliveryTests` failure.
- Update `docs/plans/2026-10-29-backpack-design.md` if anything diverged (e.g. the `OBP`/`MKW` formats).
- Manual smoke (optional, needs Part 2 or the old client for drag only): start the server, `/item` a backpack template with `{"backpackSlots":10,"backpackColumns":5}` properties, use it, check the window opens, drag in an item, relog, item still there.

**Commit** (only if docs changed) `docs(backpack): sync design with implementation`

---

## Final review notes

- **Threading:** everything here runs on the game thread (events, equip, quest completion). `Inventory.Load` runs inside `world.Database.Execute` like the existing loads; it only builds state and sends nothing.
- **Persistence strategy:** automatic migration (`AddColumnIfMissing` on every startup) plus the fresh-schema DDL in `sql/players.sql`. No manual SQL.
- **Registry boundaries:** `player.Windows` add/remove order specified in Task 4.
- **Fallbacks:** load edge cases in Task 2; refused unequip/equip/upgrade leave state untouched.
- **Design alignment:**
  - Persistence moved from `Player.BuildUpdateQuery` to an `Inventory`-owned `UPDATE players SET backpack` in the same transaction.
  - "Keep until a valid backpack is equipped" is now concrete: while contents are preserved, equipping a backpack is refused until a GM fixes the data and the player relogs.

  Task 6 records both in the design doc.
