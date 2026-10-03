# Community chest Part 3: Client generic container window — Implementation Plan

**Goal:** Give the client a widget for the new `GenericContainer` frame (30) so the community
chest renders as a paged, draggable 30-slot container whose slot updates are addressed by
window ID — coexisting cleanly with the bank window.

**Architecture:** clone the proven `BankWindow` widget into `GenericContainerWindow` (frame
30, single instance, retargeted by `MKW` like the bank widget), plus two window-ID-carrying
server→client packets (`GWS`/`GWC`). Client→server traffic (`MKW` handling, `ITW`/`WTI`/`WTW`
drags, `WBC` paging/close) is reused unchanged — it already addresses windows by ID.

**Tech Stack:** Godot 4 / C# (.NET 10), xUnit (`tests/Goose2Client.Tests`).

**Repo note:** this part lands in `../Goose2ClientGodot` on its own `feat-community-chest`
branch (worktree per that repo's convention). The server side is
`docs/plans/2026-10-03-community-chest-part2-chest.md`; design:
`docs/plans/2026-10-03-community-chest-design.md` §Protocol additions.

## APIs verified

- Client frame enum mirrors the server's 1-29 — `Scripts/WindowFrames.cs:3-34`; add
  `GenericContainer = 30`. Server side adds the same value in part 2 Task 3
  (`Goose/Window.cs:24-55`).
- Widget template — `Scripts/UI/BankWindow.cs`: ctor/`_Ready` builds a 30-slot grid from
  `Scenes/UI/ItemSlot.tscn` with `slot.Window = this; slot.OnDropItem = DropItem` (`:44-55`);
  listeners `MakeWindow/EndWindow/BankSlot/ClearBankSlot` registered in `_Ready` and removed
  in `_ExitTree` (`:57-74`); `OnMakeWindow` filters `p.WindowFrame != WindowFrames.Bank`,
  stores `NpcId/Title/WindowId`, sets Back/Next visibility from `WindowButtonFlags`
  (`:96-105`); `OnEndWindow` shows on `p.WindowId == WindowId` (`:107-111`); `DropItem` routes
  inventory→`MoveInventoryToWindow(fromSlot, WindowId, toSlot)` and window→window
  `MoveWindowToWindow(...)` (`:112-119`); paging/close send `WindowButtonClick(button,
  WindowId, NpcId)` (`:121-137`); `OnItemDropOnWindow` + `NearestSlot` for drops landing on
  the widget (`:139-146`).
- Drag senders — `Scripts/Network/NetworkClient.cs:230-240`: `ITW<from+1>,<windowId>,<to+1>`,
  `WTI<windowId>,<from+1>,<to+1>`, `WTW<fromId>,<from+1>,<toId>,<to+1>`. Already ID-based.
- Packet base — `Scripts/Network/Packets/InventorySlotPacket.cs`: `Prefix = "SIS"`, `Parse`
  sets `p.Delimeter = '|'` and reads ~44 fields ending with optional `CurrencyName`,
  `ExtraStats`, `MinExperience` guarded by `p.LengthRemaining()` (`:69-120`).
  `BankSlotPacket` (`:8-30`) duplicates that field list under prefix `SBS` — the new packet
  must not add a third copy: extract the shared read.
- `ClearBankSlotPacket` — comma-delimited `SlotNumber = p.GetInt32() - 1`, prefix `CBS`
  (`Scripts/Network/Packets/ClearBankSlotPacket.cs`).
- HUD registration — `Scripts/UI/GameHud.cs:93` `Bank = Add<BankWindow>(
  "res://Scenes/UI/BankWindow.tscn");` — same idiom for the new widget.
- Test pattern — `tests/Goose2Client.Tests/BuffBarPacketTests.cs:11-18`:
  `new XPacket().Parse(new PacketParser("<raw>", "<prefix>"))`, plain xUnit, no engine
  bootstrap needed for parse tests.

---

### Task 1: WindowFrames value + GWS/GWC packets

**Files:**
- Modify: `Scripts/WindowFrames.cs` (append `GenericContainer = 30`)
- Modify: `Scripts/Network/Packets/InventorySlotPacket.cs` (extract shared field reader)
- Create: `Scripts/Network/Packets/GenericWindowSlotPacket.cs`
- Create: `Scripts/Network/Packets/ClearGenericWindowSlotPacket.cs`
- Test: `tests/Goose2Client.Tests/GenericWindowSlotPacketTests.cs`

- `InventorySlotPacket.Parse` keeps its shape but delegates the field list to a new
  `protected static void ReadFieldsInto(PacketParser p, InventorySlotPacket pkt)`;
  `BankSlotPacket.Parse` switches to `p.Delimeter = '|'; var pkt = new BankSlotPacket();
  ReadFieldsInto(p, pkt); return pkt;` — behavior-preserving refactor of the existing
  duplicate, verified by its current tests plus a new golden-string parse test.
- `GenericWindowSlotPacket : InventorySlotPacket`, `Prefix = "GWS"`, adds
  `public int WindowId { get; set; }`; `Parse`: `p.Delimeter = '|'; pkt.WindowId =
  p.GetInt32(); ReadFieldsInto(p, pkt);` — field 0 is the ID, the rest is the identical
  `ItemSlot` payload (`Goose/Packets.cs` part 2 Task 3).
- `ClearGenericWindowSlotPacket : PacketHandler`, `Prefix = "GWC"`, comma-delimited:
  `WindowId = p.GetInt32(); SlotNumber = p.GetInt32() - 1;` (the `-1` matches `CBS`:
  wire slots are 1-based, widget slots 0-based).

**Tests:**

- `GWS_ParsesWindowIdThenSlotFields`: golden packet `"GWS1042|3|7|0||Sword||||100|0|..."` →
  `WindowId == 1042`, `SlotNumber == 2` (0-based), `Name == "Sword"`.
- `GWS_TailFieldsMatchSIS`: same payload with and without the ID prefix parses to equal
  field values (guards the shared reader against drift).
- `GWC_ParsesWindowIdAndSlot`: `"GWC1042,4"` → `WindowId == 1042`, `SlotNumber == 3`.
- `SBS_Regression`: existing bank parse unchanged.

**Commit** — `feat(packets): GenericContainer frame + window-id-addressed slot packets`

| Invariant | Proved by |
|-----------|-----------|
| GWS payload ≡ SIS/SBS payload + leading window ID | `GWS_TailFieldsMatchSIS` |
| Existing bank parsing untouched | `SBS_Regression` |

---

### Task 2: GenericContainerWindow widget

**Files:**
- Create: `Scripts/UI/GenericContainerWindow.cs`
- Create: `Scenes/UI/GenericContainerWindow.tscn` (clone `BankWindow.tscn`, root script swapped)
- Modify: `Scripts/UI/GameHud.cs:93` (register: `Generic = Add<GenericContainerWindow>(...)`,
  property beside `Bank` `:24`)

Clone `BankWindow` with exactly these deltas:

- `WindowFrame => WindowFrames.GenericContainer`; `OnMakeWindow` filters on that frame.
- Listens to `GenericWindowSlotPacket` / `ClearGenericWindowSlotPacket` instead of the bank
  pair, and **both handlers start with `if (p.WindowId != WindowId) return;`** — the whole
  point: a stale or replaced window's broadcasts are dropped, not mis-rendered.
- `EndWindow` shows on matching ID (same as bank).
- Grid: 30 slots (server pages with `CommunityChestPages` × `BankSlotsPerPage`; the widget
  shows one page, Back/Next via `WindowButtonClick` — identical to bank paging).
- Drag wiring identical (`ITW`/`WTW` already carry `WindowId`).

Single-instance-per-frame is intentional v1 scope: a second `MKW` retargets the widget (new
`WindowId`), and the server prunes the replaced window from its registry on the next change
(`player.Windows.Contains`), so stale `GWS` traffic stops by construction. Multi-instantiation
is a deferred client refactor (design §Deferred).

**Verification:**

- `dotnet test tests/Goose2Client.Tests` — packet tests green, existing suites unchanged.
- Scene test following the `BaseWindowSceneTests` idiom (text-parsing the `.tscn` files, no
  Godot runtime — `tests/Goose2Client.Tests/BaseWindowSceneTests.cs:9-30`): assert
  `Scenes/UI/GenericContainerWindow.tscn` exists, references the new script, and contains the
  `Content/SlotGrid` + `Content/BackButton` + `Content/NextButton` + `Content/CloseButton`
  node names the widget's `_Ready` resolves.
- Manual smoke (server part 2 + this): open bank and chest simultaneously — both render,
  bank drags still update via `SBS`, chest drags via `GWS`; page 2 of a chest shows
  container slots 31-60; two players watching one chest see each other's moves live; close
  one window, the other keeps updating.

**Commit** — `feat(ui): generic container window for frame 30`

---

## Design alignment check (part 3 promises)

- `GenericContainer = 30` on both sides matches design §Protocol additions.
- `GWS<windowId>|<ItemSlot payload>` / `GWC<windowId>,<slotId>` formats match; client→server
  packets untouched, matching the design's "reuses ITW/WTI/WTW/WBC unchanged".
- Window-ID filtering on slot updates matches design §CommunityChestWindow ("every packet
  self-identifies"); single-instance widget + retargeting is the documented deferred
  boundary, matching design §Deferred.
