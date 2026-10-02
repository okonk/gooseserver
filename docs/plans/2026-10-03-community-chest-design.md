# Community chest and world state design

Date: 2026-10-03
Branch: `feat-community-chest`

## Purpose

Add a community chest NPC: a shared, persistent inventory that every player can deposit to
and withdraw from, shaped like the player bank but owned by the world instead of the player.
Introduce the generic persistence service behind it — `WorldState`, a key/value store of JSON
blobs — as the foundation for future persistent world data.

## Current behavior

- `PlayerBank` (`Goose/PlayerBank.cs`) keeps one `ItemContainer` per banker NPC template,
  loaded at player load and saved as a JSON `ItemSlot[]` blob into
  `bank_items(npc_id, player_id, serialized_data)` as part of the player save
  (`Goose/PlayerBank.cs:26-129`).
- `BankWindow` (`Goose/BankWindow.cs`) subclasses `ItemContainerWindow`, uses frame 26 (Bank),
  pages with `SlotsPerPage` from settings and `MaxPages` from `player.NumberOfBankPages`, and
  gates every drag on `Map.InRange` to the banker.
- Right-click dispatch happens server-side in `PlayerRightClickEvent`
  (`Goose/Events/PlayerRightClickEvent.cs:64-67`): `NPCType == Banker` opens the bank window.
  The Godot client has a matching `CharacterType` enum (`Banker = 11`) used only for minimap
  colour and targeting, so a new NPC type value would be cross-repo work.
- All container movement enters through exactly three packet handlers — `ITW`, `WTI`, `WTW`
  (`Goose/EventHandler.cs:146-158`) — which call `ItemContainerWindow.InventoryToWindow`,
  `WindowToInventory` and the static `WindowToWindow`. Every write into a container goes
  through `ItemContainer.SetSlot` (`Goose/ItemContainer.cs:25`).
- `GuildHandler` is the existing precedent for shared persistent state: load at startup,
  dirty flags, periodic `GuildSaveEvent` (`Goose/GuildHandler.cs:86-120`).
- `PlayerHandler.RemovePlayer(Player)` (`Goose/PlayerHandler.cs:82-90`) is the single
  chokepoint for every session teardown: normal logout (`Goose/Events/LogoutEvent.cs:91`),
  lost-connection logout (`LogoutEvent.cs:33,38`) and duplicate-login replacement
  (`Goose/Events/LoginEvent.cs:249`). It zeroes `player.LoginID`; logout does not clear
  `player.Windows` (only the next login does, `LoginEvent.cs:264`).
- Item flags relevant to transfer rules: `Item.IsBound` (`Goose/Item.cs:72`), `Item.IsLore`
  (`Item.cs:86`), `Item.IsBindOnPickup` (`Item.cs:88`). Bind-on-pickup is applied at every
  acquisition site (vendor `VendorPurchaseInventoryEvent.cs:114`, ground pickup
  `PickupItemEvent.cs:114`, inventory adds `Inventory.cs:270,1283`), so such items are always
  bound in practice. The laundering hazard is documented at `Goose/Item.cs:185-186`.
- Lore enforcement today: `item.IsLore && player.HasItem(templateId)` refuses ground pickup
  (`PickupItemEvent.cs:89`); `Player.HasItem` scans inventory and bank (`Player.cs:1829`).
- Item scripts expose `CanPickup(player, item, world)` returning a refusal message or null
  (`Goose/Scripting/IItemScript.cs:15`); `PickupItemEvent` consults it fail-closed
  (`PickupItemEvent.cs:94-108`). Dimension scripts use it to block items the player cannot
  use in the current dimension.
- NPC spawn rows carry a JSON `properties` column surfaced as `NPC.Properties`
  (`Goose/NPC.cs:50`) with typed reads via `PropertiesDictionary.GetProperty<T>(key, default)`
  (design: `docs/plans/2026-09-12-npc-spawn-properties-design.md`). Runtime-spawned NPCs
  (`/spawnnpc`, scripts) get an empty dictionary.
- `BankWindow` hardcodes its window ID to 21 (`BankWindow.cs:25`) while `Window.Create`
  allocates `++player.LastWindowID` (`Window.cs:114`). `WindowToWindowEvent` resolves window
  IDs against `player.Windows`, first match wins.
- Stack splitting (`Inventory.SplitSlots`) is inventory-only — both slots are validated
  against `InventorySize` (`Events/InventorySplitEvent.cs:40-42`). No split path into a
  window, and no window→ground drag path exists; drops are inventory-originated.
- `PropertiesDictionary` cannot back `WorldState`: its converter boxes nested JSON into
  `Dictionary<string, object?>` (`PropertiesDictionaryJsonConverter.cs:107-132`) and
  `ConvertValue` cannot materialize typed values like `ItemSlot` from that (it throws
  `InvalidCastException`, `PropertiesDictionary.cs:77-144`); its writer serializes with
  camelCase options (`PropertiesDictionaryJsonConverter.cs:12-16`) while item blobs are
  contractually bound to `JsonHelper.DatabaseOptions` ("must remain compatible with
  historical Newtonsoft output", `Goose/JsonHelper.cs:10`).

## Design

### Architecture

```
CommunityChestWindow ──binds──▶ ItemContainer ◀──owns── ChestHandler
        (UI + sync)                  (generic)              │
                                                            │ Get/Set "chest:{npcTemplateId}"
                                                            ▼
                                                       WorldState
                                                  (generic KV persistence)
                                                            │
                                                  world_state table (SQLite)
```

Three new pieces plus small edits. The layering is what makes the deferred random-chest idea
cheap: a random chest is an `ItemContainer` built from a loot table, handed to a chest-style
window, with no handler and no `WorldState` entry behind it.

### WorldState (generic persistence)

In-memory `Dictionary<string, object>` keyed by opaque string. Values are raw JSON strings
straight from the database until a tenant's first `Get<T>` materializes them into a typed
object, which is then stored back into the dictionary. `Set<T>(key, value)` stores the live
object; `Remove(key)` drops the entry and queues a delete.

- **API**: `Load(Database)`, `Get<T>(string key)` (materializes; on a corrupt blob logs an
  error and returns default), `Set(string key, object value)`, `Remove(string key)`,
  `Save(GameWorld)`, `KeysWithPrefix(string prefix)`.
- **Dirty tracking by diff-on-save**: no tenant marks anything dirty. Each save serializes
  every tracked value on the game thread (strings pass through, objects via
  `JsonHelper.Serialize`), diffs against the last-saved baseline, and `EnqueueTransaction`s
  the changed upserts plus pending deletes. The on-commit callback sets baseline := snapshot,
  so a mutation landing mid-write fails the next diff and is written next cycle — the
  recompute pattern `Guild.BuildSave` uses.
- **Table**: `sql/world_state.sql` with
  `CREATE TABLE IF NOT EXISTS world_state (key TEXT PRIMARY KEY, value TEXT NOT NULL)`, added
  to the `CreateDatabaseSchema` file list (`GameWorld.cs:245-251`), plus a matching
  `CreateTableIfMissing` line in `MigrateDatabaseSchema` so existing databases get it too
  (the `quest_status` precedent, `GameWorld.cs:277-278`).
- **Save cadence**: `WorldSaveEvent` (clone of `GuildSaveEvent`) re-armed every
  `WorldSavePeriod` seconds (new setting, default 300), plus one flush in `GameWorld.Stop`
  before the pending-writes wait (`GameWorld.cs:529-533`).
- **Load order**: `LoadStep("World State", ...)` after *Global Scripts* and before *Players*
  (`GameWorld.cs:453-457`) — script-registered item templates must exist before item blobs
  deserialize.
- **Delete-vs-upsert ordering**: a key present in memory at save time is never deleted, so a
  tenant's remove-then-set within one cycle cannot lose the row.
- Not a `PropertiesDictionary` subclass (see Current behavior for the two hard blockers);
  same ergonomics, standalone class.

### ChestHandler (first tenant)

`Dictionary<int, ItemContainer>` keyed by NPC template ID — the same keying as banks, so every
spawned instance of one chest template shares one inventory, and two chest templates are two
chests. Storage key `chest:{npcTemplateId}`; prefixes are the namespace convention.

- **Load**: enumerate `chest:*` keys, materialize each into an `ItemContainer`, then replay
  the `PlayerBank.Load` validation dance (`PlayerBank.cs:64-90`): null slots and unknown
  templates discarded with a log, surviving items re-registered via `ItemHandler.AddItem` and
  `RefreshStats`. Container size is `max(CommunityChestPages * BankSlotsPerPage + 1, blob
  length)` — shrinking the setting must not silently delete overflow items on the next save;
  page count is grow-only in effect.
- **Viewer registry**: `Dictionary<ItemContainer, List<(Player, CommunityChestWindow)>>`.
  Entries are added when a chest window is created. There is no explicit removal path:
  - logout — `PlayerHandler` gains `public event Action<Player>? PlayerRemoved;`, fired at
    the end of `RemovePlayer` after the maps are updated; `ChestHandler` subscribes during its
    load step and drops that player's entries, discarding empty lists;
  - WBC close / re-open replacement — validated lazily at broadcast time with
    `player.Windows.Contains(window)`; a closed or replaced window fails the check and the
    entry is pruned in place.
- **Broadcast**: subscribes to the container's `SlotChanged` event (below) and pushes the
  changed slot to every valid viewer whose page shows that container index, using each
  viewer's own window ID and `SendSlot`/`ClearBankSlot`. The acting player is included — it
  is the only send path (see window section).

### ItemContainer change event

`ItemContainer` gains `event Action<int, ItemSlot?, ItemSlot?>? SlotChanged` (index, old,
new), fired from `SetSlot` when the reference actually changes. `SetSlot` is the only write
path into any container, so broadcast cannot be forgotten by a future code path — the same
"diff instead of remembering" philosophy as WorldState's dirty tracking. Bank and combine-bag
containers never subscribe and pay one null-delegate check.

### CommunityChestWindow

Sibling of `BankWindow`, subclassing `ItemContainerWindow`.

- `Frame = WindowFrames.Bank` (26) so the client renders it with zero changes; new
  server-side-only `WindowTypes.CommunityChest` value (the client never sees `Type`).
- Title `"Community Chest Page {X}/{Y}"`; back/next buttons exactly like banks.
- `SlotsPerPage` reuses `BankSlotsPerPage` (30); `MaxPages` from new setting
  `CommunityChestPages` (default 3). Container allocated `pages * slotsPerPage + 1`.
- Window ID comes from the standard `++player.LastWindowID` allocation — never a fixed ID
  like the bank's 21 — because a player can hold a bank window and chest windows at the same
  time and `WindowToWindowEvent` resolves IDs first-match.
- Range check (`Map.InRange`) gates every drag, same as `BankerInRange`.
- `Open()` closes any existing chest window bound to the same NPC before creating — bank
  idiom. A player near two instances of one chest template can hold two windows bound to the
  same container; they live-sync each other through the broadcast path. That is coherent,
  not a bug.
- `protected virtual bool PushesViaBroadcast => false` on `ItemContainerWindow` guards the
  window-side `this.SendSlot(...)` calls in the three drag paths (`InventoryToWindow`,
  `WindowToInventory`, static `WindowToWindow` both sides). `CommunityChestWindow` overrides
  it to `true`: the `SlotChanged` broadcast is the only window-side send for chest drags, so
  the actor receives exactly one slot packet per change, via the viewer path. The
  inventory-side `player.Inventory.SendSlot` calls stay; `Populate`/`SendCreate` renders are
  untouched. Rejected drags mutate nothing, fire no event, send no slot packets.
- The event fires synchronously inside `SetSlot`, mid-drag-method, so the actor's window is
  by definition registered when its own change broadcasts.

### Transfer validation

Two virtuals on `ItemContainerWindow`, checked before any swap, defaulting to `true` (bank
and combine bag keep permissive behaviour):

```
CanDeposit(Player, ItemSlot?)  → bool   // inventory→window; WTW target side
CanWithdraw(Player, ItemSlot?) → bool   // window→inventory; WTW source side
```

All container movement already funnels through the three packet entry points, so the seam is
complete coverage: chest→bank runs `CanWithdraw`, bank→chest runs `CanDeposit`, chest→chest
runs both before swapping.

`CommunityChestWindow` implements:

- **CanDeposit** — refuse when `item.IsBound || item.IsBindOnPickup`. The second term is
  redundant today (bind-on-pickup items are bound at every acquisition site) but closes the
  script/GM-generated hole; the laundering hazard is documented at `Item.cs:185-186`.
- **CanWithdraw** — refuse when:
  1. `item.IsLore && player.HasItem(templateId)` — the pickup expression verbatim; `HasItem`
     covers inventory and bank, so withdrawing a lore duplicate into the bank is also caught.
  2. `item.Script?.Object.CanPickup(player, item, world)` returns a refusal — same
     fail-closed try/catch as `PickupItemEvent.cs:94-108` (broken gate script refuses with a
     generic message, detail to the log). Reusing the pickup hook is deliberate: leaving a
     chest is acquisition, and the dimension scripts already speak this language.
  3. `item.IsBound` — should be unpossible via the deposit gate, but if one ever appears,
     letting anyone take it is the laundering bug again. Recovery of a stuck item is a
     DB/GM problem.
- Refusals send a `ServerMessage` with the reason (the script's own message for `CanPickup`,
  short static strings otherwise). Silent rejection reads as lag at a shared window.

These are transfer-time rules, not storage invariants: a lore item can sit in a chest
withdrawable by nobody. Same semantics as ground drops.

### NPC wiring

No new NPC type, no client change, no schema change.

- A chest is a `Banker`-type NPC whose spawn row carries `properties: {"communityChest":
  true}`, read with `npc.Properties.GetProperty("communityChest", false)` — the second
  consumer of the spawn-properties system.
- `PlayerRightClickEvent`: Banker + property → `CommunityChestWindow.Open`; otherwise the
  unchanged `BankWindow.Open`. Runtime-spawned copies get empty properties and stay plain
  bankers (same exclusion list as the spawn-properties design doc).
- Authoring (goose-game-data pipeline): an `npcs` row with `npc_type = Banker` plus a
  `spawns` row with the property JSON. The graphic is independent of type — a banker-type NPC
  can use a chest-like body. Prefer authoring chests as new templates: flipping an active
  banker to a chest orphans players' old `bank_items` rows for that template.

### Settings

- `CommunityChestPages` — default 3.
- `WorldSavePeriod` — default 300, sibling of `GuildSavePeriod`.
- `BankSlotsPerPage` reused unchanged.

### Testing

Fast tests in `Goose.Tests`, real-DB tests in `Goose.IntegrationTests`.

- **WorldState (unit)**: `Set`/`Get<T>` materialization from raw JSON; diff-on-save writes
  only changed keys; unchanged values produce no SQL; `Remove` queues a delete;
  remove-then-set in one cycle upserts; corrupt row → `Get<T>` logs and yields default.
- **WorldState (integration)**: full round-trip — set chest → save → new instance → load →
  containers materialize with items re-registered in `ItemHandler`; delete persistence.
- **Sync (unit, Fakes)**: two players, one chest — A withdraws slot 3, B receives exactly one
  slot packet at its page-relative index and the actor exactly one (the suppression
  invariant); a page-2 viewer gets nothing for page-1 changes; registry prunes on WBC close,
  re-open replacement, and `PlayerRemoved`; out-of-range drag refused with no broadcast.
- **Validation (unit)**: bound and bind-on-pickup deposits refused; lore withdrawal refused
  when `HasItem` hits inventory or bank, allowed otherwise; `CanPickup` refusal message
  forwarded; script throwing → fail-closed refusal; chest→chest runs both sides.
- **Wiring (unit)**: `communityChest` property routes right-click to the chest window;
  absent property → bank window; runtime-spawned NPC → bank window.

## Deferred (consciously)

- **Withdrawal audit logging** — `LogHandler` entries for chest in/out; declined for v1,
  cheap to add later.
- **Random/loot chests** — non-persistent containers with vanishing loot; the window binds
  any `ItemContainer` and subscribes the same broadcast helper, so this is a consumer, not a
  redesign.
- **Guild chests** — the generic owner key (`chest:{npcTemplateId}` → e.g.
  `chest:guild:{id}`) is the extension point.
- **Multi-server deployments** — last-write-wins per key; single-instance assumed.
- **Chest graphic authoring** — content task, not code.
- **Bank window fixed ID 21** — pre-existing collision quirk between two bank windows; the
  chest just doesn't join it.
