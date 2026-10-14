# Community Goals Part 4 (Godot Client Amount Window) Implementation Plan

**Goal:** Add the generic amount window to the Godot client: frame 31, `AMI` parsing, numeric input with Max, and `AMT` submission.

**Architecture:** A single-instance `AmountWindow` widget (like `GenericContainerWindow`) listens for `MakeWindowPacket` with frame 31, fills from `AmountInfoPacket`, and sends `AMT<windowId>,<amount>` on Donate. It knows nothing about community goals.

**Tech Stack:** Godot 4 C# (.NET 10), xUnit (`tests/Goose2Client.Tests`).

**Repository:** `/home/hayden/code/Goose2ClientGodot` — create a worktree there (`.worktrees/feat-amount-window`) before starting. Design: server repo `docs/plans/2026-10-14-community-goals-design.md`. **Part 4 of 4**; the wire contract comes from server Part 3 Task 2.

---

## Wire contract (from server Part 3)

- Window create: `MKW<id>,31,<title>,<buttons>,<npcLoginId>,0,0` then `ENW<id>`.
- Content: `AMI<windowId>|<sheet>|<graphic>|<r>|<g>|<b>|<a>|<min>|<max>|<default>|<prompt>` — prompt is the **last** field and may contain `|`.
- Confirm: client sends `AMT<windowId>,<amount>`. Cancel/close: existing `WBC` close (`NetworkClient.WindowButtonClick`).
- Server re-validates the amount; the client clamps for UX only.

## APIs verified

- `WindowFrames` enum `Scripts/WindowFrames.cs:34` (`GenericContainer = 30` is last).
- `PacketHandler` subclasses with `Prefix` + `Parse(PacketParser)`: `Scripts/Network/Packets/ClearGenericWindowSlotPacket.cs:6-20`. Handlers are created by `PacketManager.Listen<T>` (`Scripts/Network/PacketManager.cs:12`) — no central registry.
- `PacketParser.Delimeter` (settable) `Scripts/Network/PacketParser.cs:13`, `GetInt32/GetInt64/GetString` `:63-80`, `GetRemaining()` `:27-33` (**throws** when nothing remains — an empty prompt must be handled).
- Single-instance frame widget pattern `Scripts/UI/GenericContainerWindow.cs:8-110` (`OnMakeWindow` filter by frame, `OnEndWindow` shows, `_ExitTree` removes listeners).
- `LineEdit` usage in `Scripts/UI/CustomWindow.cs:19,51`.
- Icon rendering `Icon.Apply(TextureRect, file, id, r, g, b, a)` `Scripts/UI/Icon.cs:7`.
- HUD registration `Scripts/UI/GameHud.cs:95-99` (`Add<T>("res://Scenes/UI/....tscn")`); dialog list `Scripts/UI/DefaultWindowLayout.cs:34`.
- `NetworkClient.Send` `Scripts/Network/NetworkClient.cs:90`, `WindowButtonClick` `:288-291`.
- Packet test pattern `tests/Goose2Client.Tests/CustomWindowPacketsTests.cs` (parse via `new PacketParser(raw, prefix)`; static `Format*` helpers for outgoing packets); scene-structure tests `tests/Goose2Client.Tests/GenericContainerWindowSceneTests.cs`.
- Client `AGENTS.md`: no new comments unless non-obvious.

---

### Task 1: Frame and packets

**Files:**
- Modify: `Scripts/WindowFrames.cs` — `Amount = 31`.
- Create: `Scripts/Network/Packets/AmountInfoPacket.cs` — `Prefix = "AMI"`; `Parse`: `p.Delimeter = '|'`; read `WindowId, Sheet, Graphic, R, G, B, A` (int), `Min, Max, Default` (long), then `Prompt` = remaining text or `""` when the packet ends right after the default (guard `GetRemaining` throwing). Static `FormatAmt(int windowId, long amount) => $"AMT{windowId},{amount}"` (invariant culture).
- Modify: `Scripts/Network/NetworkClient.cs` — `public void SubmitAmount(int windowId, long amount) => Send(AmountInfoPacket.FormatAmt(windowId, amount));`.
- Test: `tests/Goose2Client.Tests/AmountInfoPacketTests.cs`.

**Tests:** golden parse of `AMI7|1|42|0|0|0|0|1|237|237|Donate how many Logs? You have 237.`; prompt containing `|` (`...|Pick | choose`) parses whole; empty prompt (`AMI7|0|0|0|0|0|0|1|5|1|`) → `""` (adversarial: must not throw); large values (`max = 9_000_000_000`) parse as long; `FormatAmt(7, 5)` → `"AMT7,5"`.

Commit `feat(amount-window): AMI/AMT packets`.

---

### Task 2: Amount widget

**Files:**
- Create: `Scenes/UI/AmountWindow.tscn` — `TitleBar` (`TitleLabel`, `CloseButton`), `Content` with `Icon` (TextureRect), `Prompt` (Label, autowrap), `AmountField` (LineEdit), `MaxButton`, `DonateButton`, `CancelButton`. Base it on `GenericContainerWindow.tscn`'s window chrome.
- Create: `Scripts/UI/AmountWindow.cs` (`BaseWindow, IWindow`, `DefaultVisible => false`, `WindowFrame => WindowFrames.Amount`):
  - `OnMakeWindow`: frame 31 only → store `WindowId`, `NpcId`, `Title`.
  - `OnAmountInfo`: matching window id → icon via `Icon.Apply`, prompt text, store min/max, field text = default.
  - `OnEndWindow`: matching id → visible, focus field, select all.
  - Field: digits only (`TextChanged` strips non-digits); `MaxButton` sets max.
  - `DonateButton` / Enter in field: parse long; clamp to [min, max]; `SubmitAmount`; hide.
  - `CancelButton` / `CloseButton`: `WindowButtonClick(WindowButtons.Close, WindowId, NpcId)`; hide.
  - Server `CLW` for this id hides it (follow how `GenericContainerWindow`/`BaseWindow` handle close — reuse, do not duplicate).
  - Extract the parse/clamp into a static `AmountInput.Parse(string text, long min, long max) → long?` so it is unit-testable without Godot.
- Modify: `Scripts/UI/GameHud.cs:95-99` — `Amount = Add<AmountWindow>("res://Scenes/UI/AmountWindow.tscn")` (+ property); `Scripts/UI/DefaultWindowLayout.cs:34` — add `"Amount"` to `IsDialog`.
- Tests: `tests/Goose2Client.Tests/AmountInputTests.cs` (empty → null; non-digits stripped; above max clamps to max; below min clamps to min; overflow string → max), `tests/Goose2Client.Tests/AmountWindowSceneTests.cs` (clone of `GenericContainerWindowSceneTests`: script reference and every node path above).

Commit `feat(amount-window): amount input widget`.

---

### Task 3: Manual smoke (with server Parts 1–3 running)

Checklist — record results in the PR description:
1. Left-click a goal NPC → hub; click a goal → description; Next → contribute list with icons and totals.
2. Click Logs → amount window with log icon, prompt and default = max; Max button works; typing letters is ignored.
3. Donate 10 → window closes, inventory drops by 10, contribute list reopens with updated totals.
4. Ticket purchase on a lottery: default 1, gold drops by the ticket price × amount.
5. Cancel and the title-bar close both close the window server-side (open another window from the NPC without a ghost amount window).
6. Two players donating at once see consistent totals after reopening.
7. Window scales with the UI scale setting like other dialogs.

---

## Invariant-to-test matrix

| Invariant | Proved by |
|---|---|
| Prompt may contain `|` | pipe-in-prompt parse test |
| Empty prompt does not crash | empty-prompt adversarial test |
| Amount clamped and numeric | `AmountInputTests` |
| Outgoing packet matches server parser | `FormatAmt` golden test (server `AmountWindowSubmitEvent` parses `AMT<id>,<amount>`) |
| Scene nodes exist where the script looks | `AmountWindowSceneTests` |

## Final checks

`dotnet test tests/Goose2Client.Tests` green; Godot project builds (`./build.sh` or the editor build); smoke checklist done.
