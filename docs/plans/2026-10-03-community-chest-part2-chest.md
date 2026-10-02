# Community chest Part 2: Community chest — Implementation Plan

**Goal:** Build the community chest NPC — a shared, persistent, live-synced container window
with deposit/withdraw validation — on top of the part 1 foundation (`WorldState`,
`ItemContainer.SlotChanged`, `PlayerHandler.PlayerRemoved`).

**Architecture:** `ChestHandler` owns `ItemContainer`s keyed by NPC template ID and stored in
`WorldState` under `chest:{npcTemplateId}`; `CommunityChestWindow` reuses the Bank frame and
is the only window type whose sends flow through the handler's viewer-registry broadcast
(fired by `SlotChanged`). Transfer rules run through new `CanDeposit`/`CanWithdraw` virtuals
on `ItemContainerWindow`. Design:
`docs/plans/2026-10-03-community-chest-design.md`; foundation:
`docs/plans/2026-10-03-community-chest-part1-worldstate.md`.

**Tech Stack:** .NET 10, C#, xUnit, System.Data.SQLite, NLog.

**Conventions:** `AGENTS.md` — no new comments except non-obvious invariants.

## APIs verified

- Validation-dance template: `PlayerBank.Load` slot loop — `Goose/PlayerBank.cs:64-90`
  (null/unknown-template discard, `ItemHandler.AddItem`, `Template` re-attach,
  `RefreshStats`); container sizing idiom `pages * slotsPerPage + 1` — `:136`.
- `ItemHandler.AddItem(Item, GameWorld)` — `Goose/ItemHandler.cs:252`; `GetItems()` — `:46`;
  `GetTemplate(int)` — used at `PlayerBank.cs:75`.
- `Item.Script` → `Script<IItemScript>?` from template — `Goose/Item.cs:132`; flags
  `IsBound` `:72`, `IsLore` `:86`, `IsBindOnPickup` `:88`; `RefreshStats()` — `:248`.
- Lore + fail-closed `CanPickup` pattern to mirror — `Goose/Events/PickupItemEvent.cs:89` and
  `:94-108`; `IItemScript.CanPickup` — `Goose/Scripting/IItemScript.cs:15`;
  `Player.HasItem` (inventory + bank) — `Goose/Player.cs:1829`.
- Drag paths to guard: `ItemContainerWindow.InventoryToWindow` / `WindowToInventory` /
  static `WindowToWindow` (bank special-case at `:71-72`) — `Goose/ItemContainerWindow.cs:29-84`.
- Window model: `WindowTypes` enum — `Goose/Window.cs:63-84`; `Window.NPC` — `:102`;
  `ID = ++player.LastWindowID` — `:114`, `Player.cs:388,575` (starts at 1000 — bank's fixed
  21 at `BankWindow.cs:25` must not be copied).
- `BankWindow` structure to mirror — `Goose/BankWindow.cs:27-51` (ctor/Open), `:66-116`
  (range check, slot offset, `SendSlot`), `:118-146` (paging buttons).
- Packets: `P.BankSlot(window, item, world, slotId, stack)` — `Goose/Packets.cs:606`;
  `P.ClearBankSlot` — `:611`; `P.ServerMessage` — `:38`.
- Right-click dispatch — `Goose/Events/PlayerRightClickEvent.cs:53-69`
  (`Map.GetNPCsInRange`, `NPCType == Banker` at `:64`); `Map.InRange` — `Goose/Map.cs:144`.
- Spawn properties: `NPC.Properties` — `Goose/NPC.cs:50`;
  `GetProperty<T>(key, default)` — `Goose/PropertiesDictionary.cs:43`;
  `NPCHandler.SpawnNPC(..., PropertiesDictionary? properties = null)` — `Goose/NPCHandler.cs:367-375`.
- Part 1 surfaces: `WorldState.Get<T>/Set/Remove/KeysWithPrefix/Save` —
  `docs/plans/2026-10-03-community-chest-part1-worldstate.md` Task 1;
  `ItemContainer.SlotChanged` — part 1 Task 4; `PlayerHandler.PlayerRemoved` — part 1 Task 5.
- Test helpers: `TestWorldFixture.CommandPlayerOn` — `TestSupport/TestWorldFixture.cs:94`
  (**sets `Inventory` but not `Bank`** — tests using `HasItem` must assign
  `player.Bank = new PlayerBank()` or they null-ref); `CompileItemScript` — `:46`;
  `AddBaseItemTemplate` — `:157`; `AddOnlinePlayer` — `:107`; `RunCommand` — `:135`;
  `CapturingPlayer.Sent` — `:82-85`; item idiom `new Item(); LoadFromTemplate(template)` —
  `Goose.Tests/CombineBagTests.cs:56-61`; NPC spawn idiom —
  `Goose.Tests/NPCSpawnPropertiesTests.cs:25`.
- Settings: `BankSlotsPerPage` — `Goose/GooseSettings.cs:29`, json `:18` (per-server-type
  block).

---

### Task 1: ChestHandler containers, load, and world wiring

**Files:**
- Create: `Goose/ChestHandler.cs`
- Modify: `Goose/GameWorld.cs` (property beside `GuildHandler` `:39`, ctor init beside
  `:220`, `LoadStep("Community Chests", ...)` immediately after the part 1 *World State*
  step), `Goose/GooseSettings.cs` (add `public int CommunityChestPages { get; set; }` beside
  `BankSlotsPerPage` `:29`), `Goose/GooseSettings.json` (add `"CommunityChestPages": 3`
  beside `"StartingBankPages"` `:17`, and in the commented Illutia block `:38`)
- Test: `Goose.Tests/ChestHandlerTests.cs`

**Contract:**

```csharp
public class ChestHandler
{
    public void Load(GameWorld world);
    public ItemContainer GetOrCreateContainer(GameWorld world, int npcTemplateId);
    internal string KeyFor(int npcTemplateId);   // "chest:" + id
}
```

- `GetOrCreateContainer`: dictionary hit → return. Miss → container sized
  `Math.Max(1, world.Settings.CommunityChestPages) * world.Settings.BankSlotsPerPage + 1`
  (clamp because fixture settings leave it 0), subscribe the broadcast handler (Task 3 —
  until then a no-op lambda), `world.WorldState.Set(key, container)`, store, return.
  Contract: after this call the container is a live value in `WorldState`, so diff-on-save
  persists every future `SetSlot` without further tenant code.
- `Load`: for each `KeysWithPrefix("chest:")` key — parse the template id (malformed suffix →
  log + skip), `var slots = world.WorldState.Get<ItemSlot[]>(key)` (null/corrupt → start
  empty), build the container sized `max(settingsSize, slots?.Length ?? 0)` — **grow-only
  rule**: a blob longer than the configured page count keeps its length so the next save
  cannot delete overflow items — replay the `PlayerBank.Load` dance
  (`Goose/PlayerBank.cs:64-90`): null slot → skip; `GetTemplate` miss → log + skip; else
  `ItemHandler.AddItem(item, world)`, re-attach `Template`, `RefreshStats()`,
  `container.SetSlot(i, slot)`. Then `Set(key, container)` (replaces the raw string/`ItemSlot[]`
  with the live container) and subscribe the broadcast handler.
- Subscriptions and mutation are game-thread only; `Load` runs inside a `LoadStep`.

**Tests** (fixture; seed `WorldState` via its internal `LoadRows` from part 1):

- `Load_RebuildsContainersAndRegistersItems`: seed `("chest:77", jsonOfSlots)` with a known
  template → container slot holds the item, `Template` attached, and
  `ItemHandler.GetItems()` contains its `ItemID` (re-registration, not just deserialization).
- `Load_DropsUnknownTemplateSlots`: blob mixing a valid and a bogus `TemplateID` → valid
  slot survives, bogus is null.
- `GetOrCreateContainer_RegistersWithWorldState`: fresh world → `GetOrCreateContainer` →
  `WorldState.KeysWithPrefix("chest:")` contains the key.
- `Load_GrowsContainerBeyondSettingsSize` (adversarial for the shrink bug): settings pages 1
  (size 31) but blob of 40 slots → `MaxSlots >= 41` and slot 39 still populated.

**Commit** — `feat(chests): ChestHandler with world-state-backed containers`

| Invariant | Proved by |
|-----------|-----------|
| Chest items re-enter `ItemHandler` at load | `Load_RebuildsContainersAndRegistersItems` |
| Shrinking `CommunityChestPages` cannot delete items | `Load_GrowsContainerBeyondSettingsSize` |
| Every container is diff-save visible | `GetOrCreateContainer_RegistersWithWorldState` |

---

### Task 2: ItemContainerWindow seams (validation + send suppression)

**Files:**
- Modify: `Goose/ItemContainerWindow.cs:29-84`
- Test: `Goose.Tests/ItemContainerWindowSeamTests.cs`

Add to the base:

```csharp
public virtual bool CanDeposit(Player player, ItemSlot? slot, GameWorld world) => true;
public virtual bool CanWithdraw(Player player, ItemSlot? slot, GameWorld world) => true;
protected virtual bool PushesViaBroadcast => false;
```

- `InventoryToWindow` (`:29-46`): after reading `containerSlot`, before `SwapSlots`,
  `if (!CanDeposit(player, containerSlot, world)) return;` — refusal messaging is the
  override's job (it has the context; base stays silent like the out-of-range path).
- `WindowToInventory` (`:48-65`): same with `CanWithdraw` on the container slot.
- `WindowToWindow` (`:67-84`): `CanWithdraw(fromWindow…)` and `CanDeposit(toWindow…)` before
  the swap, alongside the existing `BankWindow` range special-case at `:71-72`.
- Guard the three window-side `this.SendSlot(...)` calls (in each method) with
  `if (!PushesViaBroadcast)`. The inventory-side `player.Inventory.SendSlot` calls stay
  untouched. `Populate`/`SendCreate` paths are untouched — only drag-path sends are guarded.

**Mutation impact:**
- Source of truth: container slots, unchanged; the seams only gate/announce.
- Important readers: existing `BankWindow` (`Goose/BankWindow.cs:91-103`) and
  `CombineBagWindow`/`CustomWindow` drag paths — defaults must keep them byte-identical.
- Propagation: no new state; packet suppression only applies when an override opts in
  (nothing does until Task 4).
- Invariants: bank/combine behavior cannot change; a refused drag mutates nothing and sends
  no slot packets.
- Proof: regression tests below assert bank drags still emit exactly one window-side slot
  packet per side, and a `false` seam blocks the swap entirely.

**Tests:**

- `BankDrag_StillSendsWindowSlotDirectly` (regression: fails if the guard is inverted or
  applied to the bank): inventory→bank drag via `ITW` packet through `fixture.RunCommand`
  → `CapturingPlayer.Sent` contains an `SBS` packet for the target slot (`P.BankSlot` emits
  `SBS`, `P.ClearBankSlot` emits `CBS` — `Goose/Packets.cs:606-614`).
- `RefusedDeposit_LeavesSlotsUntouched`: a test-only subclass returning `false` from
  `CanDeposit` → swap does not happen (both slots unchanged) and no window slot packet sent.

**Commit** — `feat(windows): deposit/withdraw and broadcast-push seams on ItemContainerWindow`

---

### Task 3: CommunityChestWindow, viewer registry, live broadcast

**Files:**
- Create: `Goose/CommunityChestWindow.cs`
- Modify: `Goose/Window.cs:63-83` (append `CommunityChest` to `WindowTypes` — after
  `LogViewer` so existing values keep their numbering), `Goose/ChestHandler.cs` (registry +
  `PlayerRemoved` subscription in `Load`)
- Test: `Goose.Tests/CommunityChestSyncTests.cs`

**Window** — mirror `BankWindow` with these deltas:

- ctor `(GameWorld world, Player player, NPC npc)`: `SlotsPerPage =
  world.Settings.BankSlotsPerPage`; `MaxPages = Math.Max(1, world.Settings.CommunityChestPages)`;
  `ItemContainer = world.ChestHandler.GetOrCreateContainer(world, npc.NPCTemplateID)`;
  `ID = ++player.LastWindowID` (**not** the bank's fixed 21 — `Window.cs:114`,
  `BankWindow.cs:25`); register viewer in the handler registry **before** `SendCreate`;
  `Frame = WindowFrames.Bank`; `Type = WindowTypes.CommunityChest`; `NPC = npc`.
- `Open(world, player, npc)`: remove existing `CommunityChest` window with the same `NPC`,
  then add — bank idiom (`BankWindow.cs:42-51`).
- Range check gates `InventoryToWindow`/`WindowToInventory` (mirror `BankWindow.cs:66-103`);
  add the `CommunityChestWindow` case to `ItemContainerWindow.WindowToWindow`'s special-case
  block.
- `PushesViaBroadcast => true`.
- `SendSlot(slotIndex, player, world)` identical to `BankWindow.cs:105-116`.

**Registry + broadcast** in `ChestHandler`:

- `Dictionary<ItemContainer, List<(Player, CommunityChestWindow)>> viewers`.
- `internal void AddViewer(ItemContainer, Player, CommunityChestWindow)` — called from the
  window ctor. No removal path exists by design.
- `Load` subscribes `world.PlayerHandler.PlayerRemoved += p => RemoveViewer(p)` (drops the
  player from every list; delete empty lists). Subscribe with `-=` then `+=` so a reload
  can't double-subscribe.
- Broadcast handler attached to each container's `SlotChanged` (closure captures `world`,
  game thread only):

```csharp
void Broadcast(GameWorld world, ItemContainer container, int index)
{
    if (!viewers.TryGetValue(container, out var list)) return;
    list.RemoveAll(v => !v.Player.Windows.Contains(v.Window));   // WBC close / re-open prune
    foreach (var (player, window) in list)
    {
        int visible = index - (window.CurrentPage - 1) * window.SlotsPerPage;
        if (visible is >= 1 and <= window.SlotsPerPage)
            window.SendSlot(visible, player, world);
    }
}
```

The actor is included — with `PushesViaBroadcast` the broadcast is the actor's only
window-side send.

**Tests** (two `CapturingPlayer`s, one chest NPC via
`NPCHandler.SpawnNPC(..., properties: {"communityChest": true})`, both `AddOnlinePlayer`,
both windows opened via `CommunityChestWindow.Open`):

- `Withdraw_PushesOnePacketToActorAndViewer`: A drags chest slot 3 → each player's `Sent`
  contains exactly one bank-slot packet for slot 3 (adversarial: fails on double-send when
  suppression is missing, and on zero-send when the registry is skipped).
- `PageTwoViewer_GetsNothingForPageOneChange`: B on page 2 → A's slot-3 change produces no
  slot packet for B.
- `ViewerWindowClosed_PruneOnNextChange`: B closes via `Clicked(Exit,...)` → A's next change
  sends B nothing **and** the registry list shrank (assert via an internal count).
- `Logout_PrunesViewer`: `PlayerHandler.RemovePlayer(B)` → A's next change sends B nothing.
- `ReopenReplacement_NoDoubleSend`: B re-opens the chest (bank-style replace) → A's change
  sends B exactly one packet, not two.

**Commit** — `feat(chests): community chest window with live viewer sync`

| Invariant | Proved by |
|-----------|-----------|
| One slot packet per viewer per change, actor included | `Withdraw_PushesOnePacketToActorAndViewer`, `ReopenReplacement_NoDoubleSend` |
| Off-page changes are not sent | `PageTwoViewer_GetsNothingForPageOneChange` |
| Registry cannot leak closed windows or sessions | `ViewerWindowClosed_PruneOnNextChange`, `Logout_PrunesViewer` |

---

### Task 4: Transfer validation

**Files:**
- Modify: `Goose/CommunityChestWindow.cs`
- Test: `Goose.Tests/CommunityChestValidationTests.cs`

Implement the Task 2 virtuals on the chest window:

- `CanDeposit`: `slot?.Item is { IsBound: true } or { IsBindOnPickup: true }` → send
  `P.ServerMessage("That item is bound to you.")` (`Goose/Packets.cs:38`) and return false.
  The `IsBindOnPickup` term guards the script/GM-generated hole; acquisition sites normally
  bind first (`Goose/Inventory.cs:270,1283`).
- `CanWithdraw`, in order, each refusal sending its message then returning false:
  1. `item.IsBound` → "That item is bound."
  2. `item.IsLore && player.HasItem(item.Template.ID)` → lore message. Mirror
     `PickupItemEvent.cs:89`; `HasItem` covers inventory + bank (`Player.cs:1829`).
  3. `item.Script?.Object.CanPickup(player, item, world)` inside try/catch — exception →
     log + generic refusal string, exactly the fail-closed block at
     `PickupItemEvent.cs:94-108`; non-null → that message.
- Test setup note: `CommandPlayerOn` leaves `Bank` null — assign `player.Bank = new
  PlayerBank()` in these tests before anything calls `HasItem`.

**Tests:**

- `DepositBound_Refused` / `DepositBindOnPickup_Refused`: slot unchanged, message sent.
- `WithdrawBound_Refused`: a bound item planted directly in the container (bypassing the
  deposit gate) cannot be taken out.
- `WithdrawLore_RefusedWhenHeldInInventory` and `...InBank` (adversarial: a `HasItem` that
  only scans inventory fails the bank case), `WithdrawLore_AllowedWhenNotHeld`.
- `WithdrawDimensionRefusal_ForwardsScriptMessage`: `fixture.CompileItemScript` body
  returning `"Not in this dimension."` → that exact text in `Sent`.
- `Withdraw_ScriptThrows_RefusesClosed` (security-critical): script body that throws →
  generic refusal, item stays in the chest.
- `ChestToChest_RulesRunBothSides`: two chest windows, drag bound item chest→chest →
  refused.

**Commit** — `feat(chests): deposit and withdrawal validation rules`

| Invariant | Proved by |
|-----------|-----------|
| Bound items cannot enter the shared container | `DepositBound_Refused`, `DepositBindOnPickup_Refused` |
| Bound items cannot leave it either | bound case of `ChestToChest_RulesRunBothSides` plus a `WithdrawBound_Refused` test added alongside `WithdrawLore_*` |
| Lore uniqueness holds across inventory and bank | `...InBank` |
| Broken gate script refuses, never admits | `Withdraw_ScriptThrows_RefusesClosed` |

---

### Task 5: Right-click wiring + end-to-end persistence

**Files:**
- Modify: `Goose/Events/PlayerRightClickEvent.cs:64-67`
- Test: `Goose.Tests/CommunityChestWiringTests.cs`,
  `Goose.IntegrationTests/CommunityChestPersistenceTests.cs`

Dispatch becomes:

```csharp
if (npc.NPCType == NPCTemplate.Types.Banker)
{
    if (npc.Properties.GetProperty("communityChest", false))
        CommunityChestWindow.Open(world, this.Player, npc);
    else
        BankWindow.Open(world, this.Player, npc);
}
```

**Wiring tests** (banker-type template + `SpawnNPC` with/without the properties dictionary,
`RunCommand(player, "RC" + x + "," + y)`):

- `PropertyTrue_OpensChestWindow`, `NoProperty_OpensBankWindow` (regression: the bank path
  must be untouched), `RuntimeSpawnEmptyProperties_OpensBankWindow`.

**Integration** — `class CommunityChestPersistenceTests : PlayerFirstSaveTestBase(["world_state"],
withQuestStatus: false)` plus a registered item template (pattern: part 1 Task 2):

- `ChestSurvivesWorldRestart`: seed `chest:{id}` via `WorldState.Set` of a live container
  holding a registered item → `Save` → drain → fresh `WorldState` + `ChestHandler.Load` →
  slot holds an item with the right `TemplateID`, re-registered in `ItemHandler.GetItems()`
  (`Goose/ItemHandler.cs:46`) → withdraw via `WindowToInventory` → `Save` → drain → reload →
  slot empty and the item's `ItemID` now in the player's inventory rows (assert via
  `Count`/`Execute` on the DB, mirroring `PlayerBank.BuildSave`'s table
  `Goose/PlayerBank.cs:121`).

**Commit** — `feat(chests): right-click wiring and persistence round trip`

---

## Design alignment check (part 2 promises)

- Key `chest:{npcTemplateId}`, grow-only sizing, and the `PlayerBank.Load` dance match design
  §ChestHandler; registry + `PlayerRemoved` + `Contains` prune match §Viewer registry;
  broadcast-including-actor and `PushesViaBroadcast` match §CommunityChestWindow; the three
  withdrawal rules and two deposit flags match §Transfer validation; spawn-property routing
  matches §NPC wiring; `CommunityChestPages` default 3 and `BankSlotsPerPage` reuse match
  §Settings; unique window ID matches gap-check rule 1 of the design doc.
- Deferred items stay deferred: no audit logging, no random chests, no guild keys.
