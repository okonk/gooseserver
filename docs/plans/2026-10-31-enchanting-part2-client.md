# Enchanting Part 2: Client enchanting table window — Implementation Plan

**Goal:** A Godot widget for the `Enchant` frame (33): three drop slots (item, essence,
protection), server-rendered preview lines, and an Enchant button, speaking the
`EWS`/`EWP`/`EWC` protocol from Part 1.

**Architecture:** Mirror `CustomWindow` (single-instance HUD widget retargeted by `MKW`),
reusing `CustomWindowSlot` unchanged for the three slots. The client holds no enchant rules:
it sends slot ids on every change, and on `EWP` adopts the server's accepted slots (clearing
any echoed as 0), shows the lines, and enables the button from `canEnchant`.

**Tech Stack:** Godot 4 / C# (.NET 10), xUnit (`tests/Goose2Client.Tests`).

**Repo note:** lands in `../Goose2ClientGodot` on its own `feat-enchanting` branch
(worktree per that repo's convention). Depends on Part 1's protocol
(`docs/plans/2026-10-31-enchanting-part1-server.md`, Task 6). Follow that repo's `AGENTS.md`
(no new comments).

---

## APIs verified

- Frame enum mirrors the server — `Scripts/WindowFrames.cs:3-37` (ends `GridContainer = 32`);
  add `Enchant = 33`.
- Widget template — `Scripts/UI/CustomWindow.cs`: `_Ready` wires `GetNode<CustomWindowSlot>`
  slots with `OnDrop` (`:34-60`), `Listen<MakeWindowPacket/EndWindowPacket/CloseWindowPacket>`
  (`:56-61`) and removes them in `_ExitTree` (`:66-74`); `OnMakeWindow` filters on
  `p.WindowFrame`, resets state when `p.WindowId` changes (`:91-104`); `OnSlotDrop` accepts
  only inventory sources via `CustomWindowValidation.IsInventorySource(src.Window)` and uses
  `src.SlotNumber + 1` as the 1-based server id (`:150-180`); `OnClosePressed` sends
  `WindowButtonClick(WindowButtons.Close, WindowId, 0)` (`:293-299`); button visibility via
  `WindowButtonFlags.IsEnabled(_mkwButtons, WindowButtons.OK)` (`:268`).
- Reusable slot — `Scripts/UI/CustomWindowSlot.cs` (`SetItem(ItemStats)`, `ClearItem()`,
  `SlotId`, `OnDrop`, drag-out support).
- `MakeWindowPacket` fields `WindowId, WindowFrame, Title, Buttons, NpcId` —
  `Scripts/Network/Packets/MakeWindowPacket.cs:8-15`.
- Packet base/registration — `PacketHandler` subclass with `Prefix` + `Parse(PacketParser)`;
  `PacketManager.Listen<T>` registers on first listen (`Scripts/Network/PacketManager.cs:12`).
  `PacketParser.GetRemaining()` — `Scripts/Network/PacketParser.cs:27`.
- Sender idiom — `NetworkClient.CustomWindowSlots/CustomWindowCreate` →
  `Send(CustomWindowGraphicPacket.FormatCws(...))` (`Scripts/Network/NetworkClient.cs:311-320`).
- HUD registration — `Scripts/UI/GameHud.cs:27,101`
  (`Custom = Add<CustomWindow>("res://Scenes/UI/CustomWindow.tscn")`).
- Scene template — `Scenes/UI/CustomWindow.tscn` (Background, TitleBar, `Content/...Slot`
  using `CustomWindowSlot.cs` + `slot-frame.png`).
- Tests compile all of `Scripts/**/*.cs` (`tests/Goose2Client.Tests/Goose2Client.Tests.csproj:14`);
  packet test pattern `tests/Goose2Client.Tests/CustomWindowPacketsTests.cs`.

## Protocol (from Part 1, Task 6)

- `EWS<itemSlot>,<essenceSlot>,<protectionSlot>` — 1-based inventory ids, 0 = empty.
- `EWP<windowId>,<itemSlot>,<essenceSlot>,<protectionSlot>,<canEnchant>` followed by zero or
  more `|<line>`. Lines may contain commas; never `|`.
- `EWC<itemSlot>,<essenceSlot>,<protectionSlot>`.

---

### Task 1: Packets

**Files:** Create `Scripts/Network/Packets/EnchantPreviewPacket.cs`; Modify
`Scripts/WindowFrames.cs`, `Scripts/Network/NetworkClient.cs`; Test
`tests/Goose2Client.Tests/EnchantPacketsTests.cs`.

- `EnchantPreviewPacket : PacketHandler`, `Prefix = "EWP"`, properties `WindowId, ItemSlot,
  EssenceSlot, ProtectionSlot, CanEnchant, Lines (string[])`. Parse: `var parts =
  p.GetRemaining().Split('|'); var h = parts[0].Split(',');` then `Lines = parts[1..]`.
- Static `FormatEws(int item, int essence, int protection)` / `FormatEwc(...)` on the packet
  (same idiom as `CustomWindowGraphicPacket`), and `NetworkClient.EnchantWindowSlots` /
  `EnchantWindowConfirm`.

**Tests (red → green):** header + lines parse; line containing commas stays whole
(adversarial: fails with comma-based parsing); no lines → empty array; `canEnchant 0/1`;
`FormatEws(5,0,7) == "EWS5,0,7"`, `FormatEwc` likewise.

Run: `dotnet test tests/Goose2Client.Tests --filter EnchantPacketsTests`.

`git commit -m "feat(enchanting): EWP/EWS/EWC packets"`

---

### Task 2: EnchantWindow widget

**Files:** Create `Scripts/UI/EnchantWindow.cs`, `Scenes/UI/EnchantWindow.tscn`,
`Scripts/UI/EnchantWindowState.cs` (pure state, testable); Modify `Scripts/UI/GameHud.cs`;
Test `tests/Goose2Client.Tests/EnchantWindowStateTests.cs`.

**`EnchantWindowState` (no Godot types):** holds `ItemSlot, EssenceSlot, ProtectionSlot`,
`CanEnchant`, `Lines`, `Pending`.
- `Place(role, slotId)` / `Clear(role)` → returns the `EWS` triple to send; placing an id
  already used by another role clears that role first; sets `Pending`.
- `Apply(EnchantPreviewPacket)` → ignore if `WindowId` differs; adopt the three slot ids
  (returns which roles were cleared so the widget clears their icons); set lines,
  `CanEnchant`; clear `Pending`.
- `ConfirmTriple()` → ids for `EWC`; `CanConfirm => CanEnchant && !Pending`.
- `Reset()`.

**Widget:** clone `CustomWindow` structure without preview/picker/name: three
`CustomWindowSlot` nodes (`Content/ItemSlot`, `Content/EssenceSlot`,
`Content/ProtectionSlot`), a `VBoxContainer` of `Label`s for lines, an Enchant `Button`
(visible when the OK flag is set, disabled unless `CanConfirm`). Listens to
`MakeWindowPacket` (frame `Enchant`), `EndWindowPacket`, `CloseWindowPacket`,
`EnchantPreviewPacket`. Drops: inventory sources only, any item allowed (the server
decides), id = `src.SlotNumber + 1`; dragging between enchant slots moves the id; dropping a
slot onto itself clears it. Every change sends `EWS`. Enchant button sends `EWC` and sets
`Pending`. Close → `WindowButtonClick(Close, WindowId, 0)`, reset, hide. Register in
`GameHud` next to `Custom`.

**Tests:** place item → triple `(5,0,0)`; same id into essence clears item role; `Apply`
with essence echoed 0 reports essence cleared; stale `WindowId` ignored; `CanConfirm` false
while pending even if last `EWP` said 1 (adversarial: double-click sends two `EWC`s);
`Reset` clears all.

`git commit -m "feat(enchanting): enchanting table window"`

---

### Task 3: Manual smoke (with Part 1 server)

Configure `Enchanting.EssenceTemplates` / `ProtectionTemplateId` with existing test templates
in a local `GooseSettings.json`, spawn an NPC with `{"enchantTable": true}`, then verify:
open/close, drop item + essence → lines show, enchant to +5 (never fails), +6.. with and
without protection, destroyed item clears its slot, last essence clears its slot, walking
away blocks slot changes with a notice, tooltip shows the new stats and name `+N`.

---

## Design alignment check

- Three slots referencing inventory ids, server-rendered breakdown, max shown only in the
  window, Enchant button — Task 2 + Part 1 Task 6.
- Client mirrors frame 33 and the exact packet formats of Part 1 Task 6 — Task 1.
