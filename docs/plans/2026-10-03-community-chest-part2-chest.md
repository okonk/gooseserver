# Community chest Part 2: Community chest — Implementation Plan

**Goal:** Build the community chest NPC — a shared, persistent, live-synced container window
with deposit/withdraw validation — on top of the part 1 foundation (`WorldState`,
`ItemContainer.SlotChanged`/`NotifySlotChanged`, `PlayerHandler.PlayerRemoved`).

**Architecture:** `ChestHandler` owns `ItemContainer`s keyed by NPC template ID and stored in
`WorldState` under `chest:{npcTemplateId}`; `CommunityChestWindow` reuses the Bank frame and
is the only window type whose sends flow through the handler's viewer-registry broadcast
(fired by `SlotChanged`). At most **one Bank-frame window per player** — a protocol reality,
not a preference: `SBS`/`CBS` carry no window ID (`Goose/Packets.cs:606-614,485-497`) and the
client renders the Bank frame as one globally-listening widget
(`../Goose2ClientGodot/Scripts/UI/BankWindow.cs:10-16,60-61`). Transfer rules run through
`CanDeposit`/`CanWithdraw` virtuals on `ItemContainerWindow`, applied to **both directions of
every swap**. Design: `docs/plans/2026-10-03-community-chest-design.md`; foundation:
`docs/plans/2026-10-03-community-chest-part1-worldstate.md`.

**Tech Stack:** .NET 10, C#, xUnit, System.Data.SQLite, NLog.

**Conventions:** `AGENTS.md` — no new comments except non-obvious invariants.

## APIs verified

- **Swap semantics** — `ItemSlot.SwapSlots` (`Goose/ItemSlot.cs:62-84`): same-reference and
  null↔null early returns; one side null → move; both occupied and stackable → **merge in
  place** (`to.Stack += from.Stack`, destination reference survives); otherwise → reference
  exchange. Every drag is simultaneously a deposit and a withdrawal.
- Validation-dance template: `PlayerBank.Load` slot loop — `Goose/PlayerBank.cs:64-90`;
  container sizing idiom `pages * slotsPerPage + 1` — `:136`.
- `ItemHandler.AddItem(Item, GameWorld)` — `Goose/ItemHandler.cs:252`; `GetItems()` — `:46`;
  `GetTemplate(int)` — used at `PlayerBank.cs:75`.
- `Item.Script` → `Script<IItemScript>?` from template — `Goose/Item.cs:132`; flags
  `IsBound` `:72`, `IsLore` `:86`, `IsBindOnPickup` `:88`; `RefreshStats()` — `:248`.
- Lore + fail-closed `CanPickup` pattern to mirror — `Goose/Events/PickupItemEvent.cs:89` and
  `:94-108`; `IItemScript.CanPickup` — `Goose/Scripting/IItemScript.cs:15`;
  `Player.HasItem` (inventory + bank) — `Goose/Player.cs:1829`.
- Drag paths: `ItemContainerWindow.InventoryToWindow` / `WindowToInventory` / static
  `WindowToWindow` (bank special-case at `:71-72`) — `Goose/ItemContainerWindow.cs:29-84`;
  window-side sends at `:42`, `:60`, `:82-83`; `SetSlot` wrapper + private
  `BankWindow.GetSlotOffset` — `Goose/BankWindow.cs:71-89`.
- Window model: `WindowTypes` enum — `Goose/Window.cs:63-84`; `Window.NPC` — `:102`;
  `ID = ++player.LastWindowID` — `:114`, `Player.cs:388,575` (starts at 1000 — bank's fixed
  21 at `BankWindow.cs:25` must not be copied); `BankWindow.Open` replace-by-NPC idiom —
  `BankWindow.cs:42-51`; paging/`Clicked` — `:118-146`; range check — `:66-69`; `SendSlot` —
  `:105-116`.
- Packets: `P.BankSlot` → `"SBS" + ItemSlot(...)` payload starts at slot id —
  `Goose/Packets.cs:606-609,485-497`; `P.ClearBankSlot` → `"CBS" + slotId` — `:611-614`;
  `P.ServerMessage` — `:38`.
- Right-click dispatch — `Goose/Events/PlayerRightClickEvent.cs:53-69`
  (`Map.GetNPCsInRange`, `NPCType == Banker` at `:64`); `Map.InRange` — `Goose/Map.cs:144`.
- Spawn properties: `NPC.Properties` — `Goose/NPC.cs:50`; `NPC.NPCTemplateID` — `:147`;
  `GetProperty<T>(key, default)` — `Goose/PropertiesDictionary.cs:43`;
  `NPCHandler.SpawnNPC(..., PropertiesDictionary? properties = null)` —
  `Goose/NPCHandler.cs:367-375`.
- Part 1 surfaces: `WorldState.Get<T>/Set/Remove/KeysWithPrefix/Save` + `RawJson`
  representation + `SavePlan`/`ApplyCommit`; `ItemContainer.SlotChanged` +
  `NotifySlotChanged(int)`; `PlayerHandler.PlayerRemoved` —
  `docs/plans/2026-10-03-community-chest-part1-worldstate.md`.
- Test helpers: `TestWorldFixture(Action<GooseSettings>? configure)` —
  `TestSupport/TestWorldFixture.cs:19` (**`BankSlotsPerPage` defaults to 0** — every chest
  test must configure a nonzero value via the `configure` hook or containers are one slot
  with zero visible slots); `CommandPlayerOn` — `:94` (**sets `Inventory` but not `Bank`** —
  assign `player.Bank = new PlayerBank()` before anything calls `HasItem`);
  `CompileItemScript` — `:46`; `AddBaseItemTemplate` — `:157`; `AddOnlinePlayer` — `:107`;
  `RunCommand` — `:135`; `CapturingPlayer.Sent` — `:82-85`; item idiom
  `new Item(); LoadFromTemplate(template)` — `Goose.Tests/CombineBagTests.cs:56-61`; NPC
  spawn idiom — `Goose.Tests/NPCSpawnPropertiesTests.cs:25`.
- Integration harness: `PlayerFirstSaveTestBase(schemaFiles, generatedTables,
  withQuestStatus)` reads `AppContext.BaseDirectory/sql/*.sql` —
  `Goose.IntegrationTests/PlayerFirstSaveTests.cs:20-41`; `quest_status` comes from
  `players.sql:86`, so `"players"` must be in `schemaFiles` unless
  `withQuestStatus: true` is safe; drain = synchronous fence `world.Database.Execute(conn =>
  { });` (`Database.cs:163-189`), never `PendingCount` polling (`:41-44`).
- Settings: `BankSlotsPerPage` — `Goose/GooseSettings.cs:29`, json `:18`; property
  initializers as defaults — `GooseSettings.cs:97-99`.

---

### Task 0: Test settings groundwork

**Files:**
- Modify: `TestSupport/TestWorldFixture.cs`

Add to the fixture's default `GooseSettings` initializer (near `VendorSlotSize = 30`,
`TestWorldFixture.cs:26`): `BankSlotsPerPage = 30, CommunityChestPages = 3`. Without a
nonzero `BankSlotsPerPage` every chest container is one slot with zero visible slots and
Tasks 3-5 tests cannot exercise slot 3. This matches the shipped json (`GooseSettings.json:18`)
and cannot change existing bank tests (no test constructs a `BankWindow` against the
fixture's zero value today — verify with a full `dotnet test Goose.Tests` run).

**Commit** — `test(fixtures): bank-sized settings in TestWorldFixture` (grouped with Task 1's
first commit if preferred)

---

### Task 1: ChestHandler containers, load, and world wiring

**Files:**
- Create: `Goose/ChestHandler.cs`
- Modify: `Goose/GameWorld.cs` (property beside `GuildHandler` `:39`, ctor init beside
  `:220`, `LoadStep("Community Chests", () => this.ChestHandler.Load(this))` immediately
  after the part 1 *World State* step), `Goose/GooseSettings.cs` (add
  `public int CommunityChestPages { get; set; } = 3;` beside `BankSlotsPerPage` `:29`),
  `Goose/GooseSettings.json` (add `"CommunityChestPages": 3` beside `"StartingBankPages"`
  `:17`, and in the commented Illutia block `:38`)
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
  `Math.Max(1, world.Settings.CommunityChestPages) * world.Settings.BankSlotsPerPage + 1`,
  subscribe the broadcast handler (Task 3 — until then a no-op lambda),
  `world.WorldState.Set(key, container)`, store, return. Contract: after this call the
  container is a live value in `WorldState`, so diff-on-save persists every future change
  without further tenant code.
- `Load`: for each `KeysWithPrefix("chest:")` key — parse the template id (malformed suffix →
  log + skip), `var slots = world.WorldState.Get<ItemSlot[]>(key)` (materializes from
  `RawJson`; null/corrupt → start empty), build the container sized
  `max(settingsSize, slots?.Length ?? 0)` — **grow-only**: a blob longer than the configured
  size keeps its length so the next save cannot delete overflow items — replay the
  `PlayerBank.Load` dance (`Goose/PlayerBank.cs:64-90`): null slot → skip; `GetTemplate`
  miss → log + skip; else `ItemHandler.AddItem(item, world)`, re-attach `Template`,
  `RefreshStats()`, `container.SetSlot(i, slot)`. Then `Set(key, container)` (replaces the
  `RawJson`/array with the live container) and subscribe the broadcast handler.
- Subscriptions and mutation are game-thread only; `Load` runs inside a `LoadStep`.

**Tests** (fixture; seed `WorldState` via its internal `LoadRows` from part 1):

- `Load_RebuildsContainersAndRegistersItems`: seed `("chest:77", jsonOfSlots)` with a known
  template → container slot holds the item, `Template` attached, and
  `ItemHandler.GetItems()` contains its `ItemID` (re-registration, not just deserialization).
- `Load_DropsUnknownTemplateSlots`: blob mixing a valid and a bogus `TemplateID` → valid
  slot survives, bogus is null.
- `GetOrCreateContainer_RegistersWithWorldState`: fresh world → `GetOrCreateContainer` →
  `WorldState.KeysWithPrefix("chest:")` contains the key.
- `Load_GrowsContainerBeyondSettingsSize` (adversarial for the shrink bug): configure
  `CommunityChestPages = 1` (size 31), seed a 40-slot blob → `MaxSlots == 40` and slot 39
  still populated. (Reachability of the overflow pages is Task 3's `MaxPages` growth.)

**Commit** — `feat(chests): ChestHandler with world-state-backed containers`

| Invariant | Proved by |
|-----------|-----------|
| Chest items re-enter `ItemHandler` at load | `Load_RebuildsContainersAndRegistersItems` |
| Shrinking `CommunityChestPages` cannot delete items | `Load_GrowsContainerBeyondSettingsSize` |
| Every container is diff-save visible | `GetOrCreateContainer_RegistersWithWorldState` |

---

### Task 2: ItemContainerWindow seams (bidirectional validation, merge announce, send guard)

**Files:**
- Modify: `Goose/ItemContainerWindow.cs:29-92`, `Goose/BankWindow.cs:71-74` (promote
  `GetSlotOffset` to `protected virtual int GetSlotOffset() => 0` on the base, override in
  `BankWindow`)
- Test: `Goose.Tests/ItemContainerWindowSeamTests.cs`

Add to the base:

```csharp
public virtual bool CanDeposit(Player player, ItemSlot? incoming, GameWorld world) => true;
public virtual bool CanWithdraw(Player player, ItemSlot? outgoing, GameWorld world) => true;
protected virtual bool PushesViaBroadcast => false;
protected virtual int GetSlotOffset() => 0;
```

**Every drag is a swap** — both directions must be validated before anything mutates:

- `InventoryToWindow`: read both slots; refuse unless
  `CanWithdraw(player, containerSlot, world) && CanDeposit(player, inventorySlot, world)`.
  (An occupied chest target receives `inventorySlot` **and** releases `containerSlot` into
  the player's hands — validating only the incoming item lets a bound item be smuggled in by
  swapping it onto anything, and validating only the "withdrawal direction" never runs on
  this path at all.)
- `WindowToInventory`: same pair — `CanWithdraw(containerSlot)` and `CanDeposit(inventorySlot)`.
- `WindowToWindow`: four checks — `fromWindow.CanWithdraw(fromSlot)`,
  `toWindow.CanDeposit(fromSlot)`, `toWindow.CanWithdraw(toSlot)`,
  `fromWindow.CanDeposit(toSlot)` — alongside the existing `BankWindow` range special-case
  (`:71-72`), which gains a `CommunityChestWindow` case.
- Refusal messaging is the override's job (it has the context); base returns silently, like
  the out-of-range path.

**Merge announce + send guard** — in all three methods, replace the bare
`this.SetSlot(index, slot)` on the window side with a helper:

```csharp
protected void SetSlotAnnouncing(Player player, int windowSlotIndex, ItemSlot? newSlot,
                                 ItemSlot? priorSlot, long priorStack, GameWorld world)
{
    this.SetSlot(windowSlotIndex, newSlot);
    if (ReferenceEquals(priorSlot, newSlot) && priorSlot is not null &&
        priorSlot.Stack != priorStack)
        this.ItemContainer.NotifySlotChanged(windowSlotIndex + GetSlotOffset());
    if (!PushesViaBroadcast) this.SendSlot(windowSlotIndex, player, world);
}
```

`priorSlot`/`priorStack` are captured before `SwapSlots`. A reference exchange fires via
`SetSlot`; a stack merge survives as the same reference and is announced by
`NotifySlotChanged`; a no-op swap fires nothing. Either way exactly one event per changed
container slot, so the actor's suppressed send is fully replaced. The inventory-side
`player.Inventory.SendSlot` calls stay; `Populate`/`SendCreate` renders are untouched.

**Mutation impact:**
- Source of truth: container slots, unchanged; the seams gate and announce.
- Important readers: existing `BankWindow` (`Goose/BankWindow.cs:91-103`) and
  `CombineBagWindow`/`CustomWindow` drag paths — defaults must keep them byte-identical.
- Propagation: no new state until an override opts in (nothing does until Task 3).
- Invariants: bank/combine behavior unchanged; a refused drag mutates nothing and sends no
  slot packets; every changed container slot produces exactly one event.
- Proof: regression tests below.

**Tests:**

- `BankDrag_StillSendsWindowSlotDirectly` (regression: fails if the guard is inverted or
  applied to the bank): inventory→bank drag via `ITW` packet through `fixture.RunCommand`
  → `CapturingPlayer.Sent` contains an `SBS` packet for the target slot
  (`Goose/Packets.cs:606-614`).
- `RefusedDeposit_LeavesSlotsUntouched`: test-only subclass returning `false` from
  `CanDeposit` → both slots unchanged, no window slot packet.
- `OccupiedTarget_WithdrawDirectionValidated` (adversarial for the reverse-swap hole):
  subclass refusing `CanWithdraw` → an inventory→window drag onto an **occupied** slot is
  refused even though the incoming item alone would pass `CanDeposit`.

**Commit** — `feat(windows): bidirectional validation, merge announce, and push seams`

---

### Task 3: CommunityChestWindow, viewer registry, live broadcast

**Files:**
- Create: `Goose/CommunityChestWindow.cs`
- Modify: `Goose/Window.cs:63-83` (append `CommunityChest` to `WindowTypes` — after
  `LogViewer` so existing values keep their numbering), `Goose/BankWindow.cs:42-51`
  (`Open` also removes chest windows), `Goose/ChestHandler.cs` (registry +
  `PlayerRemoved` subscription in `Load`)
- Test: `Goose.Tests/CommunityChestSyncTests.cs`

**Window** — mirror `BankWindow` with these deltas:

- ctor `(GameWorld world, Player player, NPC npc)`: `SlotsPerPage =
  world.Settings.BankSlotsPerPage`; container from
  `world.ChestHandler.GetOrCreateContainer(world, npc.NPCTemplateID)`;
  `MaxPages = Math.Max(Math.Max(1, world.Settings.CommunityChestPages),
  (container.MaxSlots - 1 + SlotsPerPage - 1) / SlotsPerPage)` — grow-only storage stays
  reachable; `ID = ++player.LastWindowID` (`Window.cs:114`; never the bank's fixed 21 —
  `WBC`/`WTW` resolve by ID first-match); register viewer in the handler registry **before**
  `SendCreate`; `Frame = WindowFrames.Bank`; `Type = WindowTypes.CommunityChest`; `NPC = npc`.
- **One Bank-frame window per player.** `Open(world, player, npc)` removes every window whose
  `Type` is `Bank` **or** `CommunityChest` from `player.Windows`, then adds the new chest —
  because `SBS`/`CBS` carry no window ID and the client's Bank frame is a single widget; two
  server-side Bank-frame windows per player cannot both be true on one client. `BankWindow.Open`
  gains the symmetric removal of chest windows. Consequence: chest↔chest and chest↔bank
  `WTW` drags are unreachable; chest↔combine-bag remains reachable (different frame).
- Range check gates `InventoryToWindow`/`WindowToInventory` (mirror `BankWindow.cs:66-103`);
  `WindowToWindow` gets the matching case.
- `PushesViaBroadcast => true`.
- `SendSlot(slotIndex, player, world)` identical to `BankWindow.cs:105-116`.

**Registry + broadcast** in `ChestHandler`:

- `Dictionary<ItemContainer, List<(Player, CommunityChestWindow)>> viewers`.
- `internal void AddViewer(ItemContainer, Player, CommunityChestWindow)` — called from the
  window ctor. No removal path exists by design.
- `Load` subscribes `world.PlayerHandler.PlayerRemoved -= OnPlayerRemoved; += OnPlayerRemoved`
  (drops the player from every list; delete empty lists; `-=` first so a reload cannot
  double-subscribe).
- Broadcast attached to each container's `SlotChanged` (closure captures `world`, game thread
  only):

```csharp
void Broadcast(GameWorld world, ItemContainer container, int index)
{
    if (!viewers.TryGetValue(container, out var list)) return;
    list.RemoveAll(v => !v.Player.Windows.Contains(v.Window));   // WBC close / replace prune
    foreach (var (player, window) in list)
    {
        int visible = index - (window.CurrentPage - 1) * window.SlotsPerPage;
        if (visible is >= 1 and <= window.SlotsPerPage)
            window.SendSlot(visible, player, world);
    }
}
```

The actor is included — with `PushesViaBroadcast` the broadcast is the actor's only
window-side send. The packet's lack of a window ID is safe because the one-window rule
guarantees each viewer's client widget belongs to this chest.

**Tests** (fixture with Task 0 settings; two `CapturingPlayer`s, one chest NPC via
`NPCHandler.SpawnNPC(..., properties: {"communityChest": true})`, both `AddOnlinePlayer`,
both windows opened via `CommunityChestWindow.Open`):

- `Withdraw_PushesOnePacketToActorAndViewer`: A drags chest slot 3 → each player's `Sent`
  contains exactly one `SBS` packet for slot 3 (adversarial: fails on double-send without
  suppression, zero-send without the registry).
- `StackMerge_StillPushes`: A deposits a stackable item onto B's watched slot 3 (merge —
  same slot reference) → B gets exactly one `SBS` for slot 3 (adversarial for the
  reference-only event: fails if `NotifySlotChanged` is skipped).
- `PageTwoViewer_GetsNothingForPageOneChange`: B on page 2 → A's slot-3 change sends B
  nothing.
- `ViewerWindowClosed_PruneOnNextChange`: B closes via `Clicked(Exit,...)` → A's next change
  sends B nothing and the registry list shrank (internal count).
- `Logout_PrunesViewer`: `PlayerHandler.RemovePlayer(B)` → A's next change sends B nothing.
- `OpenBankWindow_ClosesChestWindow` / `OpenChestWindow_ClosesBankWindow` (the single
  Bank-frame rule; also proves the replaced chest prunes via `Contains` on the next change).
- `OverflowPagesReachable`: container of 40 slots, settings pages 1 → `MaxPages == 2` and
  page 2 shows slot 39.

**Commit** — `feat(chests): community chest window with live viewer sync`

| Invariant | Proved by |
|-----------|-----------|
| One slot packet per viewer per change, actor included | `Withdraw_PushesOnePacketToActorAndViewer` |
| In-place stack merges still sync | `StackMerge_StillPushes` |
| Off-page changes are not sent | `PageTwoViewer_GetsNothingForPageOneChange` |
| Registry cannot leak closed windows or sessions | `ViewerWindowClosed_PruneOnNextChange`, `Logout_PrunesViewer`, `OpenBankWindow_ClosesChestWindow` |
| Grow-only storage stays reachable | `OverflowPagesReachable` |

---

### Task 4: Transfer validation

**Files:**
- Modify: `Goose/CommunityChestWindow.cs`
- Test: `Goose.Tests/CommunityChestValidationTests.cs`

Implement the Task 2 virtuals on the chest window (both take the item actually crossing this
window's boundary, which Task 2 wires on **both** sides of every swap):

- `CanDeposit(player, incoming, world)`: `incoming?.Item is { IsBound: true } or
  { IsBindOnPickup: true }` → send `P.ServerMessage("That item is bound to you.")`
  (`Goose/Packets.cs:38`) and return false. The `IsBindOnPickup` term guards the
  script/GM-generated hole; acquisition sites normally bind first
  (`Goose/Inventory.cs:270,1283`).
- `CanWithdraw(player, outgoing, world)`, in order, each refusal sending its message then
  returning false:
  1. `item.IsBound` → "That item is bound."
  2. `item.IsLore && player.HasItem(item.Template.ID)` — mirror `PickupItemEvent.cs:89`;
     `HasItem` covers inventory + bank (`Player.cs:1829`).
  3. `item.Script?.Object.CanPickup(player, item, world)` inside try/catch — exception →
     log + generic refusal string, exactly the fail-closed block at
     `PickupItemEvent.cs:94-108`; non-null → that message.
- Test setup: assign `player.Bank = new PlayerBank()` (`CommandPlayerOn` leaves it null).

**Tests:**

- `DepositBound_Refused` / `DepositBindOnPickup_Refused`: slot unchanged, message sent.
- `DepositBound_OntoOccupiedChestSlot_Refused` (adversarial for the reverse-swap hole: a
  seam that only validated the intended direction on `InventoryToWindow` passes this drag).
- `WithdrawBound_Refused`: bound item planted directly in the container.
- `WithdrawLore_RefusedWhenHeldInInventory` / `...InBank` (adversarial: a `HasItem` that only
  scans inventory fails the bank case) / `WithdrawLore_AllowedWhenNotHeld`.
- `WithdrawLore_SwappingOntoJunk_Refused` (reverse-swap hole on the `WindowToInventory` side).
- `WithdrawDimensionRefusal_ForwardsScriptMessage`: `fixture.CompileItemScript` body
  returning `"Not in this dimension."` → that exact text in `Sent`.
- `Withdraw_ScriptThrows_RefusesClosed` (security-critical): throwing script → generic
  refusal, item stays in the chest.
- `CombineToChest_RulesRun`: stackable-container drag into the chest with a bound incoming
  item → refused (proves the `WTW` four-way wiring; chest↔chest is unreachable under the
  one-window rule, so the combine bag stands in for the second container).

**Commit** — `feat(chests): deposit and withdrawal validation rules`

| Invariant | Proved by |
|-----------|-----------|
| Bound items cannot enter, by any drag direction | `DepositBound_Refused`, `DepositBound_OntoOccupiedChestSlot_Refused` |
| Bound items cannot leave either | `WithdrawBound_Refused` |
| Lore uniqueness holds across inventory and bank | `...InBank` |
| Reverse swaps cannot bypass either rule | both `..._Refused` occupied-target tests |
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

**Integration** — `class CommunityChestPersistenceTests : PlayerFirstSaveTestBase(["players",
"banks", "pets", "guilds", "world_state"], ["quest_requirements", "quest_rewards"])`
(mirrors `GuildSaveTests`; `players.sql` supplies `quest_status` so `withQuestStatus` stays
default), plus a registered item template via the same pattern as part 1 Task 2 and
`MakePlayer()` (`PlayerFirstSaveTests.cs:62+`):

- `ChestSurvivesWorldRestart`: seed `chest:{id}` via `WorldState.Set` of a live container
  holding a registered item → `Save` → fence → fresh `WorldState` + `ChestHandler.Load` →
  slot holds an item with the right `TemplateID`, re-registered in
  `ItemHandler.GetItems()` (`Goose/ItemHandler.cs:46`) → withdraw via `WindowToInventory`
  (player's `Bank` assigned) → `player.SaveToDatabase(world)` + `WorldState.Save(world)` →
  fence → reload → chest slot empty and the item's `ItemID` present in the `inventory` table
  rows for the player (assert via `Count`/`Execute`, mirroring the inventory save path).

**Commit** — `feat(chests): right-click wiring and persistence round trip`

---

## Design alignment check (part 2 promises)

- Key `chest:{npcTemplateId}`, grow-only sizing + reachable `MaxPages`, and the
  `PlayerBank.Load` dance match design §ChestHandler.
- One Bank-frame window per player, `++LastWindowID` allocation, and the no-window-ID
  rationale match design §CommunityChestWindow.
- Bidirectional validation on all three drag paths matches design §Transfer validation.
- Merge announce via `NotifySlotChanged` matches design §ItemContainer change event.
- Spawn-property routing, `CommunityChestPages` default 3 (initializer + json), and
  `BankSlotsPerPage` reuse match design §NPC wiring / §Settings.
- Deferred items stay deferred: no audit logging, no random chests, no guild keys.
