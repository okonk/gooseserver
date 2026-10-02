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
  through `ItemContainer.SetSlot` (`Goose/ItemContainer.cs:25`). Two consequences matter:
  `ItemSlot.SwapSlots` **merges stacks in place** (`to.Stack += from.Stack`,
  `Goose/ItemSlot.cs:75-79`), so the destination slot reference survives unchanged; and when
  both sides are occupied the swap moves an item in *both* directions — every drag is
  simultaneously a deposit and a withdrawal (`Goose/ItemSlot.cs:80-84`).
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
  IDs against `player.Windows`, first match wins. The client→server drag packets already carry
  window IDs (`ITW<invSlot>,<windowId>,<slot>`, `WTI`, `WTW` —
  `../../Goose2ClientGodot/Scripts/Network/NetworkClient.cs:230-240`), but the server→client
  slot updates do not: `P.BankSlot` = `"SBS" + ItemSlot(...)` starts at the slot number
  (`Goose/Packets.cs:606-609,485-497`), and the client implements the Bank frame as a single
  widget (`../Goose2ClientGodot/Scripts/UI/BankWindow.cs:10-16` — "hidden until a
  MakeWindow/EndWindow pair for this frame arrives") listening globally for `SBS`/`CBS`
  (`:60-61`). Two coexisting Bank-frame windows per player are therefore unrepresentable on
  the client — which is why the chest gets its own frame and ID-bearing slot packets instead
  of reusing the bank's (see §Protocol additions).
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

In-memory `Dictionary<string, object>` keyed by opaque string. Unmaterialized database rows
are held in a distinct `RawJson` wrapper record — never as a bare `string` — so a tenant's
live string value and a persisted JSON blob can never be confused (`Set("k", "hello")`
serializes to `"\"hello\""`; `Get<string>` on a `RawJson` row deserializes `"\"hello\""` to
`hello`). The first `Get<T>` on a `RawJson` entry deserializes, caches the typed object in
place, and returns it; `Set<T>(key, value)` stores the live object; `Remove(key)` drops the
entry and records a pending delete.

- **API**: `Load(Database)`, `Get<T>(string key)` (materializes; on a corrupt blob logs an
  error and returns default), `Set(string key, object value)`, `Remove(string key)`,
  `Save(GameWorld world)`, `KeysWithPrefix(string prefix)`.
- **Dirty tracking by diff-on-save**: no tenant marks anything dirty. Each save builds an
  **immutable plan on the game thread** — upserts for every value whose serialization
  (`RawJson` passes through its text, live objects via `JsonHelper.Serialize`) differs from
  the baseline, deletes for pending-delete keys not present in the live dictionary. The
  database transaction executes **only from the captured plan** (deletes first, then upserts)
  and never reads live state. The on-commit callback (DB thread) applies the plan:
  `baseline[key] := json` for upserts, `baseline.Remove(key)` for deletes, and
  `pendingDeletes.ExceptWith(plan.Deletes)`.
- **Single-flight saves**: at most one periodic plan in flight. A `Save` while one is in
  flight sets a trailing flag and returns; the commit callback (via
  `GameWorld.EnqueueCompletion`, `GameWorld.cs:83`) clears the flag and re-saves, so the
  trailing plan is rebuilt from *current* state *after* the earlier `ApplyCommit` landed.
  Without this, per-key tombstones are ambiguous across generations: enqueue delete D, set +
  enqueue upsert U, delete again — D commits and clears the *newer* tombstone for the same
  key, U commits and restores the row, and the next plan sees neither value nor tombstone.
  Re-planning after each commit makes that interleaving impossible.
- **In-flight always settles**: `inFlight` is cleared on **both** outcomes. `onCommit` never
  runs after a rollback or a failed COMMIT (`Database.cs:252-254`), and
  `EnqueueTransaction` currently reports failure to nobody — so it gains an optional
  `onSettled(Exception?)` passed through to `Enqueue`'s completion, which the loop invokes
  with `null` on success and the exception after logging on failure (`Database.cs` async
  branch in `Loop`). A synchronous throw from the enqueue itself clears the flag in the
  `catch`. Without this a single SQL failure wedges every later save (always "in flight")
  and hangs shutdown's spin. `GameWorld.Stop` uses a synchronous variant (the
  `Database.Execute` path, which also fences any in-flight work) so shutdown cannot strand a
  trailing save whose completion would never be pumped.
- **Locking**: one private lock guards `baseline` and `pendingDeletes` — taken by `PlanSave`
  and `ApplyCommit` (DB thread) **and by `Remove`**, which mutates `pendingDeletes` on the
  game thread; an unlocked `HashSet` mutated against `ExceptWith` is a data race. `values` is
  game-thread-only and needs no lock.
- **Table**: `sql/world_state.sql` with
  `CREATE TABLE IF NOT EXISTS world_state (key TEXT PRIMARY KEY, value TEXT NOT NULL)`, added
  to the `CreateDatabaseSchema` file list (`GameWorld.cs:245-251`), plus a matching
  `CreateTableIfMissing` line in `MigrateDatabaseSchema` so existing databases get it too
  (the `quest_status` precedent, `GameWorld.cs:277-278`).
- **Save cadence**: `WorldSaveEvent` (clone of `GuildSaveEvent`) re-armed every
  `WorldSavePeriod` seconds (new setting, default 300 as a property initializer, since
  missing ints deserialize to 0), scheduled at startup like `GuildHandler.AddSaveEvent`
  (`GameWorld.cs:375-379`) and re-armed in a `finally` — the event pump dequeues before
  running and drops the event if `Ready` throws (`EventHandler.cs:337,355-366`), so a
  transient failure must not end the cadence. Plus one **synchronous** flush in
  `GameWorld.Stop` (the `Database.Execute` path, which also drains any in-flight plan before
  it) ahead of the pending-writes wait (`GameWorld.cs:529-533`).
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

- **Load**: enumerate `chest:*` keys, `Get<ItemSlot[]>` each (materializing from `RawJson`),
  then replay the `PlayerBank.Load` validation dance (`PlayerBank.cs:64-90`): null slots and
  unknown templates discarded with a log, surviving items re-registered via
  `ItemHandler.AddItem` and `RefreshStats`. The validated container is then `Set` back under
  the key, so diff-on-save sees the live object. Container size is
  `maxPages * BankSlotsPerPage + 1` where
  `maxPages = max(CommunityChestPages, ceil((blobLength - 1) / BankSlotsPerPage))` — the
  `- 1` matters: containers serialize with the unused sentinel slot 0, so a healthy one-page
  blob is 31 long; without it every restart would round up one page (31 → 61 → 91, an
  unbounded capacity ratchet). Shrinking the setting must not silently delete overflow items
  on the next save, and the container always covers **whole pages**, so no window can address
  a slot the container lacks: a partial final page would let a drag swap against an
  out-of-range `GetSlot` that returns null and a `SetSlot` that discards with a log —
  clearing the inventory side and destroying the item. The window's `MaxPages` derives from
  `MaxSlots` so preserved overflow stays reachable (see window section).
- **Viewer registry**: `Dictionary<ItemContainer, List<(Player, CommunityChestWindow)>>`.
  Entries are added when a chest window is created. There is no explicit removal path:
  - logout — `PlayerHandler` gains `public event Action<Player>? PlayerRemoved;`, fired at
    the end of `RemovePlayer` after the maps are updated; `ChestHandler` subscribes during its
    load step and drops that player's entries, discarding empty lists;
  - WBC close / re-open replacement — validated lazily at broadcast time with
    `player.Windows.Contains(window)`; a closed or replaced window fails the check and the
    entry is pruned in place.
- **Broadcast**: subscribes to the container's `SlotChanged` event (below) and pushes the
  changed slot to every valid viewer whose page shows that container index, addressed by each
  viewer's own window ID (`GWS`/`GWC`, §Protocol additions) so coexisting windows never
  cross-talk. The acting player is included — it is the only send path (see window section).

### ItemContainer change event

`ItemContainer` gains `event Action<int, ItemSlot?, ItemSlot?>? SlotChanged` (index, old,
new), fired from `SetSlot` when the reference actually changes, plus
`NotifySlotChanged(int index)` for **in-place** mutations. `SwapSlots` merges stacks by
mutating the destination `ItemSlot` (`ItemSlot.cs:75-79`), after which `SetSlot` re-stores
the same reference and fires nothing — so each drag path captures the container slot's stack
before the swap and calls `NotifySlotChanged` when the same slot object comes back with a
different stack. `SetSlot` remains the only *replacement* write path, so reference-change
broadcast cannot be forgotten by a future code path — the same "diff instead of remembering"
philosophy as WorldState's dirty tracking. Bank and combine-bag containers never subscribe
and pay one null-delegate check.

### Protocol additions

The chest uses a **new frame, `WindowFrames.GenericContainer = 30`** (server `Window.cs` and
client `WindowFrames.cs` enums stay mirrored), so the client routes it to its own widget and
the bank's widget, protocol, and paging quirks are untouched. The only new wire packets are
the server→client slot updates, which carry the window ID the Bank frame's `SBS` lacks:

- `GWS<windowId>|<ItemSlot payload>` — same pipe-separated `ItemSlot` payload as `SBS`
  (`Packets.cs:485-497`), window ID prepended as field 0.
- `GWC<windowId>,<slotId>` — clear, comma style like `CBS`.

Client→server traffic reuses `ITW`/`WTI`/`WTW`/`WBC` unchanged — they already address windows
by ID. The bank keeps `SBS`/`CBS`; migrating it onto `GWS` is deferred.

### CommunityChestWindow

Sibling of `BankWindow`, subclassing `ItemContainerWindow`.

- `Frame = WindowFrames.GenericContainer` (30); new server-side-only
  `WindowTypes.CommunityChest` value (the client never sees `Type`). The client widget is a
  clone of `BankWindow` filtered by frame 30 and by window ID on `GWS`/`GWC` (part 3).
- Title `"Community Chest Page {X}/{Y}"`; back/next buttons exactly like banks.
- `SlotsPerPage` reuses `BankSlotsPerPage` (30); `MaxPages = (MaxSlots - 1 + SlotsPerPage - 1)
  / SlotsPerPage` — derived from the container, which the loader sizes to whole pages, so
  every page-visible index is a real container slot. `CommunityChestPages` (default 3) sets
  the floor.
- Bank and chest windows **coexist** — different frames, and slot updates are window-ID
  addressed, so neither can corrupt the other's client state; `WTW` between them is reachable
  and validated on both sides.
- **One chest window per player.** `Open()` closes *every* chest window (any NPC), not just
  one bound to the same NPC: the client's v1 widget is single-instance per frame and a second
  `MKW` retargets it, so a second server-side chest window would be a ghost — invisible to
  that client, unprunable by the `Contains` check, and still mutating a container nobody can
  see. Replacing on open keeps server state and widget state identical. Multi-instance client
  widgets are deferred; until then chest↔chest `WTW` is unreachable by construction.
- `ValidateSlotIndex` checks the **absolute** index (`index > 0 && index + GetSlotOffset() <
  ItemContainer.MaxSlots`), not just the page-local one — the base check would pass a page-2
  index into a page-1 container, where `GetSlot` returns null, `SwapSlots` moves the item
  out of the inventory, and `SetSlot` discards it with a log.
- Window ID comes from the standard `++player.LastWindowID` allocation — never a fixed ID
  like the bank's 21 — because `WBC`/`WTW` resolve IDs against `player.Windows` first-match.
- Range check (`Map.InRange`) gates every drag, same as `BankerInRange`.
- `protected virtual bool PushesViaBroadcast => false` on `ItemContainerWindow` guards the
  window-side `this.SendSlot(...)` calls in the three drag paths (`InventoryToWindow`,
  `WindowToInventory`, static `WindowToWindow` both sides). `CommunityChestWindow` overrides
  it to `true`: the `SlotChanged` broadcast is the only window-side send for chest drags, so
  the actor receives exactly one slot packet per change, via the viewer path. The
  inventory-side `player.Inventory.SendSlot` calls stay; `Populate`/`SendCreate` renders are
  untouched. Rejected drags mutate nothing, fire no event, send no slot packets.
- The event fires synchronously inside `SetSlot`, mid-drag-method, so the actor's window is
  by definition registered when its own change broadcasts.

### Transfer persistence (atomic with the player side)

A chest drag moves an item between two owners — the world container and a player container.
Persisting them on separate schedules is the exact dupe the `EnqueueTransaction` docs warn
about (`Goose/Database.cs:225-236`): withdraw an item, the player save commits the inventory
row with it, a crash lands before the periodic world save, and reload restores the item in
**both** places (the reverse order loses it). So every accepted chest drag — on all three
paths, including the static `WindowToWindow` — ends with
`ChestHandler.CommitTransfer(world, player, container)`, reached through an `AfterTransfer`
virtual on `ItemContainerWindow` that each drag path invokes after mutating (the static
`WTW` has no `this`, so it calls the hook on each window that owns a chest container):

1. **Build every save part on the game thread before enqueueing**: snapshot the chest
   container's JSON, and capture `part = player.Inventory.BuildSave()` and
   `bankPart = player.Bank.BuildSave(player)` as already-built actions. These APIs snapshot
   *at build time* and are documented for exactly that sequencing (`Goose/Player.cs:1045-1048`
   "Build every part of the save on the game thread, snapshotting state as we go";
   `Goose/Inventory.cs:972-977`, covering inventory + equipped + combine bag;
   `Goose/PlayerBank.cs:101-107`). Calling `BuildSave()` *inside* the transaction lambda
   would snapshot on the DB thread — two rapid transfers could then pair chest snapshot 1
   with post-transfer-2 player state, recreating the dupe/loss window the transaction exists
   to close.
2. `world.Database.EnqueueTransaction(conn => { chest upsert; part(conn); bankPart(conn); },
   onCommit: () => world.WorldState.NoteCommitted(key, json))` — the lambda only executes
   captured actions and plan data. The bank part is included unconditionally so chest↔bank
   drags are covered; writing an unchanged bank row is idempotent. Enqueue order is
   game-thread order and the DB thread is single-threaded FIFO, so an atomic transfer and an
   in-flight periodic plan commit in the order the game thread made them — the later write
   always reflects the later state.
3. `onCommit` advances `WorldState`'s baseline for the chest key (under the lock), so the
   periodic diff sees no delta and won't rewrite.

The periodic save + shutdown flush stay as the safety net for non-drag mutations (load-time
`Set`, future GM tooling).

### Transfer validation

Two virtuals on `ItemContainerWindow`, defaulting to `true` (bank and combine bag keep
permissive behaviour):

```
CanDeposit(Player, ItemSlot? incoming, GameWorld)  → bool
CanWithdraw(Player, ItemSlot? outgoing, GameWorld) → bool
```

Every drag is a **swap**, so each side simultaneously sends its current slot out and takes
the other side's slot in — validating only the "intended" direction would let an occupied
target smuggle items both ways (deposit a bound item by swapping it onto a chest item; take a
lore-restricted item out by swapping it onto a junk item). Each drag path therefore runs,
before mutating anything:

- `InventoryToWindow`: `window.CanWithdraw(containerSlot)` **and**
  `window.CanDeposit(inventorySlot)`.
- `WindowToInventory`: `window.CanWithdraw(containerSlot)` **and**
  `window.CanDeposit(inventorySlot)`.
- `WindowToWindow`: all four — `fromWindow.CanWithdraw(fromSlot)`,
  `toWindow.CanDeposit(fromSlot)`, `toWindow.CanWithdraw(toSlot)`,
  `fromWindow.CanDeposit(toSlot)`.

All container movement funnels through the three packet entry points, so the seam is
complete coverage. Chest↔bank and chest↔chest `WTW` drags are reachable (different frames,
ID-addressed) and run the full four-way checks.

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
  4. `item.IsBindOnPickup && !item.IsBound` — the pickup path binds after acquisition
      (`PickupItemEvent.cs:114`); a chest withdrawal is acquisition too, and the deposit gate
      should make an unbound BOP item unpossible in the container. If one exists anyway
      (persisted before the gate, GM/script-generated), refusing the withdrawal is safer
      than silently stamping `IsBound` on the swap target.
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
- **Client (part 3, `Goose2Client.Tests`)**: `GWS`/`GWC` golden parses; `GWS` tail parses
  identically to `SIS`/`SBS` (shared reader); `SBS` regression; scene-file structure test;
  manual smoke for bank+chest coexistence and two-player live sync.

## Deferred (consciously)

- **Migrating the bank onto `GWS`/`GWC`** — the bank keeps `SBS`/`CBS` and its fixed window
  ID 21; unifying both container windows on one protocol + one client widget is a later
  consolidation.
- **Multi-instance client widget** — the v1 generic-container widget is single-instance per
  frame (a second `MKW` retargets it, exactly like the bank widget today); pooling widgets
  per window ID is a later client refactor. Server-side state stays correct either way.
- **Withdrawal audit logging** — `LogHandler` entries for chest in/out; declined for v1,
  cheap to add later.
- **Random/loot chests** — non-persistent containers with vanishing loot; the window binds
  any `ItemContainer` and subscribes the same broadcast helper, so this is a consumer, not a
  redesign.
- **Guild chests** — the generic owner key (`chest:{npcTemplateId}` → e.g.
  `chest:guild:{id}`) is the extension point.
- **Multi-server deployments** — last-write-wins per key; single-instance assumed.
- **Chest graphic authoring** — content task, not code.
