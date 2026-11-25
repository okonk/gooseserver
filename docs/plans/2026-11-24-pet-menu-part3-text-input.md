# Pet Menu Part 3 — Generic Text Input Window Implementation Plan

**Goal:** A reusable server-driven text input window (title, prompt, button text, default text,
max length) on the server and the Godot client.

**Architecture:** Mirrors `AmountWindow` end to end: server `TextInputWindow` sends `TXI` after
`MakeWindow`; the client submits `TXT<windowId>,<text>`; the server validates and calls the
caller's `onConfirm`, which closes the window on success (CLW) or leaves it open for retry.

**Tech Stack:** Server C# / .NET 10 + xUnit; client Godot 4 C# (`~/code/Goose2ClientGodot`) + xUnit.

Design: `docs/plans/2026-11-24-pet-menu-design.md`. Part 3 of 4 — independent of Parts 1–2.

---

## APIs verified

| API | Location |
|---|---|
| Server `WindowFrames` (last `Stylist = 34`) | `Goose/Window.cs:25-60` |
| Server `WindowTypes` (last `Stylist`) | `Goose/Window.cs:70-95` |
| `AmountWindow` (template for ctor/Populate/Submit/Clicked) | `Goose/AmountWindow.cs` |
| `P.AmountInfo` format `AMI{id}|...|{prompt}` | `Goose/Packets.cs:794-798` |
| `AmountWindowSubmitEvent` parsing + guards (`RefuseWhileDead`, `State == Ready`) | `Goose/Events/AmountWindowSubmitEvent.cs:15-35` |
| Event prefix registry (`("AMT", Open(typeof(...)))`) | `Goose/EventHandler.cs:161` |
| `Window.Close` sends `CLW` | `Goose/Window.cs:348-352` |
| Client `AmountInfoPacket` (`Prefix`, `Parse`, `FormatAmt`) | `Goose2ClientGodot/Scripts/Network/Packets/AmountInfoPacket.cs` |
| Client `PacketParser.GetString/GetInt32/GetRemaining/LengthRemaining` (`GetRemaining` throws at end) | `Goose2ClientGodot/Scripts/Network/PacketParser.cs:27-104` |
| Client handlers register lazily via `PacketManager.Listen<T>` | `Goose2ClientGodot/Scripts/Network/PacketManager.cs:12-22` |
| Client `AmountWindow` (MakeWindow/EndWindow/CloseWindow listeners, focus on EndWindow) | `Goose2ClientGodot/Scripts/UI/AmountWindow.cs` |
| Client `WindowFrames.Amount = 31` | `Goose2ClientGodot/Scripts/WindowFrames.cs:35` |
| `GameHud.Add<AmountWindow>("res://Scenes/UI/AmountWindow.tscn")` | `Goose2ClientGodot/Scripts/UI/GameHud.cs:33,110` |
| `DefaultWindowLayout.IsDialog` list | `Goose2ClientGodot/Scripts/UI/DefaultWindowLayout.cs:34-35` |
| `NetworkClient.SubmitAmount` / `WindowButtonClick` | `Goose2ClientGodot/Scripts/Network/NetworkClient.cs:345,299` |
| Typing in any focused `LineEdit` already suppresses movement/hotkeys | `Scripts/Character/Character.cs:679,745`, `Scripts/UI/HotbarWindow.cs:346`, `Scripts/GameManager.cs:190` |
| Client tests: `AmountInfoPacketTests`, `AmountWindowSceneTests` (text-asserts on `.tscn`) | `Goose2ClientGodot/tests/Goose2Client.Tests/` |

Prefixes `TXI` (server→client) and `TXT` (client→server) are unused on both sides (checked the client
`Prefix` list and server registry).

---

### Task 1: Server window, frame and packet

**Files:**
- Create: `Goose/TextInputWindow.cs`
- Modify: `Goose/Window.cs` (`TextInput = 35` in `WindowFrames`; `TextInput` appended to `WindowTypes`)
- Modify: `Goose/Packets.cs` (after `AmountInfo`, ~line 798)
- Test: `Goose.Tests/TextInputWindowTests.cs`

Packet:

```csharp
public static Func<int, int, string, string, string, string> TextInputInfo =
    (windowId, maxLength, buttonText, defaultText, prompt) =>
        $"TXI{windowId}|{maxLength}|{buttonText.Replace("|", "")}|{defaultText.Replace("|", "")}|{prompt}";
```

Window:

```csharp
public TextInputWindow(Player player, GameWorld world, string title, string prompt, string buttonText,
    string defaultText, int maxLength, Action<string, Player, GameWorld> onConfirm)
public void Submit(string text, Player player, GameWorld world)
```

- Ctor: `maxLength < 1` or empty `prompt` → `ArgumentOutOfRangeException` / `ArgumentException`
  (an empty trailing prompt would make the client's `GetRemaining` throw). Frame/Type `TextInput`,
  `Buttons = "0,1,0,0,0"`, add to `player.Windows`, `SendCreate`.
- `Populate` sends `P.TextInputInfo(...)`.
- `Submit`: strip `char.IsControl` chars, `Trim()`; empty or `Length > maxLength` → return
  (log debug as `AmountWindow.Submit` does); otherwise `onConfirm(text, player, world)`.
  **Does not close** — the caller closes on success.
- `Clicked`: Exit/Close → `player.Windows.Remove(this)` (as `AmountWindow.cs:57-66`).

**Tests:** create sends `TXI` with fields in order and `|` stripped from button/default but kept in
prompt; Submit trims and strips control chars before `onConfirm`; empty / too long don't call
`onConfirm`; **adversarial:** window still in `player.Windows` after a valid Submit whose
`onConfirm` doesn't close (fails if Submit closes like AmountWindow).

Commit `feat(ui): generic text input window (server)`.

---

### Task 2: Server submit event

**Files:**
- Create: `Goose/Events/TextInputSubmitEvent.cs`
- Modify: `Goose/EventHandler.cs:161` (add `("TXT", Open(typeof(TextInputSubmitEvent))),`)
- Test: `Goose.Tests/TextInputWindowTests.cs`

Format `TXT<windowId>,<text>`: `Substring(3).Split(',', 2)`; same dead/state guards as
`AmountWindowSubmitEvent.cs:17-19`; `int.TryParse` invariant; find
`Player.Windows.OfType<TextInputWindow>()` by id; `Submit(t[1], ...)`.

**Tests** via `fixture.RunCommand(player, "TXT{id},Sir, Fluff")`: text `"Sir, Fluff"` reaches
`onConfirm` (commas preserved); missing comma / non-numeric id / unknown id → no call.

Commit `feat(ui): text input submit event`.

---

### Task 3: Client worktree, frame, packet, network send

Client repo: `~/code/Goose2ClientGodot` — create a worktree with @using-git-worktrees:
`git worktree add .worktrees/text-input -b feature/text-input` (`.worktrees/` is gitignored there,
`.gitignore:49-50`). Run the client test suite for a baseline.

**Files (client):**
- Modify: `Scripts/WindowFrames.cs` (`TextInput = 35` after `Stylist = 34`)
- Create: `Scripts/Network/Packets/TextInputInfoPacket.cs`
- Modify: `Scripts/Network/NetworkClient.cs` (beside `SubmitAmount`, line 345)
- Test: `tests/Goose2Client.Tests/TextInputInfoPacketTests.cs`

Packet fields: `WindowId`, `MaxLength`, `ButtonText`, `DefaultText`, `Prompt`. Parse with
`Delimeter = '|'`: `GetInt32`, `GetInt32`, `GetString`, `GetString`, then
`Prompt = p.LengthRemaining() > 0 ? p.GetRemaining() : ""` (same guard as `AmountInfoPacket`).

`public static string FormatTxt(int windowId, string text) => $"TXT{windowId.ToString(CultureInfo.InvariantCulture)},{text}";`
and `NetworkClient.SubmitText(int windowId, string text) => Send(TextInputInfoPacket.FormatTxt(windowId, text));`

**Tests:** full parse; empty default (`TXI7|16|Rename||New name:`); prompt containing `|`;
`FormatTxt` keeps commas.

Commit (client) `feat(ui): text input packet`.

---

### Task 4: Client window scene

**Files (client):**
- Create: `Scenes/UI/TextInputWindow.tscn` (copy `AmountWindow.tscn` layout; nodes `TitleBar/TitleLabel`,
  `TitleBar/CloseButton`, `Content/Prompt` (Label, autowrap), `Content/TextField` (LineEdit),
  `Content/ConfirmButton`, `Content/CancelButton`; `WindowName = "TextInput"`; no icon/max button)
- Create: `Scripts/UI/TextInputWindow.cs`
- Modify: `Scripts/UI/GameHud.cs:33,110` (property + `Add<TextInputWindow>(...)`)
- Modify: `Scripts/UI/DefaultWindowLayout.cs:34-35` (add `"TextInput"` to `IsDialog`)
- Test: `tests/Goose2Client.Tests/TextInputWindowSceneTests.cs`

Script behaviour (mirrors `AmountWindow.cs`, differences called out):
- Listens to `MakeWindowPacket` (frame `TextInput`), `TextInputInfoPacket`, `EndWindowPacket`,
  `CloseWindowPacket`; removes listeners in `_ExitTree`.
- On info: prompt text, `TextField.MaxLength = MaxLength`, `TextField.Text = DefaultText`,
  `ConfirmButton.Text = ButtonText`.
- On EndWindow: show, `GrabFocus`, `SelectAll`.
- Submit (button or `TextSubmitted`): trimmed text empty → ignore; else `SubmitText`.
  **Does not hide** — the server closes it with CLW on success; on invalid input it stays open with
  the server's notice. (Differs from `AmountWindow.Submit`, which hides.)
- Cancel / close button: `WindowButtonClick(WindowButtons.Close, ...)` then hide.

**Tests:** scene references `res://Scripts/UI/TextInputWindow.cs`; has title bar/label/close
button; has `Content/Prompt`, `Content/TextField` (LineEdit), `Content/ConfirmButton`;
`WindowName = "TextInput"` (assertion style of `AmountWindowSceneTests.cs`).

Manual smoke (after Part 4 lands): use a name tag in game; typing doesn't move the character; an
invalid name keeps the window open; a valid one closes it.

Commit (client) `feat(ui): text input window`.

---

## Final check

- Server: `dotnet test Goose.sln` → all pass.
- Client: run the client test project → all pass.

## Invariant-to-test matrix

| Invariant | Proved by |
|---|---|
| Server always re-validates client text | Submit empty/too long/control-char tests |
| Window stays open until caller closes | adversarial Submit test; client no-hide-on-submit |
| Commas survive the wire | `TXT` event test, `FormatTxt` test |
| `|` can't break TXI fields | packet strip test; client prompt-with-pipe test |
