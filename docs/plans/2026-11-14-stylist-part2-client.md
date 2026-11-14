# Stylist (Part 2 of 2: Client) Implementation Plan

**Goal:** A Stylist window in Goose2ClientGodot with Hair/Face tabs of sprite-rendered icons,
a rotatable live preview, a cost label, Reset and Change (sends `STC<hair>,<face>`).

**Architecture:** `StylistWindow : BaseWindow, IWindow` opens on `MKW` frame 34, exactly like
`EnchantWindow`. Selection/cost logic lives in a pure `StylistWindowState` (unit-tested); the
Godot window only renders it. `CustomPreviewControl` gains per-slot id overrides and a facing
override. Icons come from the existing hair/eyes `SpriteFrames`.

**Tech Stack:** Godot 4.6 C# (GodotSharp 4.6.2), xUnit (`tests/Goose2Client.Tests`, which
compiles `Scripts/**/*.cs` — pure logic only; no engine calls like `GD.Load` in tests).

Work in the client worktree: `~/code/Goose2ClientGodot/.worktrees/stylist` (branch `stylist`).
Depends on Part 1's protocol: server sends `MKW<id>,34,Stylist,0,1,0,0,1,<npc>,…` then
`ENW<id>`; closes with `CLW<id>`; client sends `STC<hair>,<face>`, close via `WBC`.

Follow `AGENTS.md`: no new comments or doc strings unless the "why" is non-obvious.

---

## APIs verified (client repo paths)

| API | Where |
|---|---|
| `WindowFrames` enum ends `Enchant = 33` | `Scripts/WindowFrames.cs:37` |
| `IWindow { WindowFrame; WindowId; }` | `Scripts/UI/IWindow.cs:5-9` |
| Frame-filtered open/close pattern: `Listen<MakeWindowPacket>` / `EndWindowPacket` / `CloseWindowPacket`, `_listenersRegistered`, `Remove` in `_ExitTree`, `OnClosePressed` → `NetworkClient.WindowButtonClick(WindowButtons.Close, WindowId, 0)` | `Scripts/UI/EnchantWindow.cs:46-89`, `226-231` |
| Send helpers live on `NetworkClient` and delegate to a static packet `Format…` | `Scripts/Network/NetworkClient.cs:322-330`, `Scripts/Network/Packets/EnchantPreviewPacket.cs` (`FormatEwc`) |
| `StatusInfoPacket.Gold` (long) | `Scripts/Network/Packets/StatusInfoPacket.cs:28,57` |
| Local player: `GameManager.Instance.CurrentMapManager?.LocalPlayer`; `Character.GetAppearance()` → `AppearanceData` (`HairId`, `HairColor`, `FaceId`); `Character.Facing`, `BodyState` | `Scripts/Character/Character.cs:16,43,383`, `Scripts/Character/AppearanceData.cs:9-10` |
| `GameManager.Instance.CharacterUpdated` event (appearance changes) | `Scripts/GameManager.cs:98` |
| `CustomPreviewControl`: `Refresh()`, `_Process` facing poll, `SetCustomGraphic`, `HideSlot`, `SetSlotTint` | `Scripts/UI/CustomPreviewControl.cs:40-180` |
| `CharacterSlot { …, Eyes, …, Hair, Helm, … }`; `CharacterLayout.TypeFolder(slot)` (`Hair`, `Eyes`) | `Scripts/Character/CharacterLayout.cs:6,43` |
| `AnimationNames.Candidates(string motion, int bodyState, Direction d)` | `Scripts/Character/AnimationNames.cs:36` |
| `FrameTextureCache.Get(SpriteFrames, StringName, int)` | `Scripts/FrameTextureCache.cs:13` |
| `TintMaterial.Apply(CanvasItem, Texture2D?, Color)` | `Scripts/TintMaterial.cs:35` |
| `Direction { Up, Right, Down, Left }` | `Scripts/Direction.cs:3-9` |
| HUD registration `Add<T>(scenePath)` | `Scripts/UI/GameHud.cs:40,104-105` |
| `BaseWindow.WindowName` export (scene sets `WindowName = "Hairdye"`) | `Scripts/UI/BaseWindow.cs:13`, `Scenes/UI/HairdyeWindow.tscn:42` |
| Centred-dialog list `DefaultWindowLayout.IsDialog` | `Scripts/UI/DefaultWindowLayout.cs:34-35`; test `tests/Goose2Client.Tests/DefaultWindowLayoutTests.cs:16-36` |
| Sprite folders: `Assets/Sprites/Hair/{1..46}`, `Assets/Sprites/Eyes/{1..18}` each with `animations.tres` | verified by `ls` |

---

### Task 0: Frame, catalog, STC format

**Files:**
- Modify: `Scripts/WindowFrames.cs` (add `Stylist = 34`)
- Create: `Scripts/UI/StylistCatalog.cs` — `static IReadOnlyList<int> HairIds` (1..46),
  `FaceIds` (1..18)
- Create: `Scripts/Network/Packets/StylistPackets.cs` — `static string FormatStc(int hair, int face) => $"STC{hair},{face}"`
- Modify: `Scripts/Network/NetworkClient.cs:330` — `public void StylistChange(int hair, int face) => Send(StylistPackets.FormatStc(hair, face));`
- Test: `tests/Goose2Client.Tests/StylistCatalogTests.cs`, `StylistPacketsTests.cs`

**Tests**
- `FormatStc(12, 5) == "STC12,5"`.
- Catalog is exactly 1..46 / 1..18.
- Adversarial asset check: walk up from `AppContext.BaseDirectory` to the directory containing
  `project.godot`; every catalog id has `Assets/Sprites/Hair/{id}/animations.tres` /
  `Assets/Sprites/Eyes/{id}/animations.tres` (fails if the catalog drifts from assets).

Red: compile failure. Green: `dotnet test tests/Goose2Client.Tests --filter "FullyQualifiedName~Stylist"`.
Commit: `feat(stylist): window frame, catalog and STC packet`

---

### Task 1: `StylistWindowState` (pure)

**Files:**
- Create: `Scripts/UI/StylistWindowState.cs`
- Test: `tests/Goose2Client.Tests/StylistWindowStateTests.cs`

No Godot types except `Direction`. Shape:

```csharp
public enum StylistTab { Hair, Face }

public sealed class StylistWindowState
{
    public const int ChangeCost = 2000;
    public int CurrentHair { get; private set; }
    public int CurrentFace { get; private set; }
    public int SelectedHair { get; private set; }
    public int SelectedFace { get; private set; }
    public long Gold { get; set; }
    public StylistTab Tab { get; set; }
    public Direction Facing { get; private set; } = Direction.Down;

    public void Open(int hair, int face);          // current and selection
    public void SetCurrent(int hair, int face);   // current only
    public void Select(int id);                    // applies to Tab
    public void Reset();                           // selection := current
    public int Cost { get; }                       // ChangeCost × changed slots
    public bool CanAfford => Gold >= Cost;
    public bool CanChange => Cost > 0 && CanAfford;
    public void RotateLeft();  public void RotateRight();   // Down → Left → Up → Right → Down
}
```

`ChangeCost` duplicates the server default for display only; server is authoritative.

Rule for `SetCurrent`: add `Open(int hair, int face)`, which sets current **and** selection
(used on `MKW`). `SetCurrent` updates current only and keeps the selection, so an unrelated
`CharacterUpdated` (e.g. equipping a helm) doesn't wipe what the player is browsing. After a
successful change the server's new values equal the selection, so `Cost` becomes 0.

| Invariant | Test |
|---|---|
| `Open(33,2)` selects current, cost 0, `CanChange` false | `Open_selects_current_and_costs_nothing` |
| **Adversarial:** `SetCurrent` with unchanged ids keeps a browsed selection | `SetCurrent_keeps_browsed_selection` |
| Hair only / face only / both → 2000 / 2000 / 4000 | `[Theory] Cost_counts_changed_slots` |
| Selecting the current id again is free | `Selecting_current_again_costs_nothing` |
| Tab routes `Select` to the right slot | `Select_applies_to_active_tab` |
| Reset restores current | `Reset_restores_current` |
| **Adversarial:** gold one short disables change | `Gold_one_short_cannot_change` — Gold 1999, cost 2000 → `CanAfford` false, `CanChange` false |
| After server applies the selection, cost returns to 0 | `SetCurrent_to_selection_clears_cost` |
| Rotation cycle | `RotateRight_cycles_down_left_up_right` and `RotateLeft` inverse |

Commit: `feat(stylist): window selection and cost state`

---

### Task 2: `CustomPreviewControl` overrides

**Files:**
- Modify: `Scripts/UI/CustomPreviewControl.cs`

**Mutation impact:**
- Source of truth: new `_slotGraphics : Dictionary<CharacterSlot,int>` and
  `_facingOverride : Direction?` on the control.
- Readers: `Refresh()` (`:54-142`) and `_Process` (`:40-46`) only.
- Propagation: setters call `Refresh()`. In `Refresh`, id resolution order per slot:
  hidden → `_customSlot` (existing) → `_slotGraphics[slot]` (id override; tint from
  `source.TryGetSlotGraphic` or `_slotTints`, pose `source.BodyState`) → source graphic.
  `facing = _facingOverride ?? source.Facing`. `_Process` returns early while
  `_facingOverride` is set.
- Invariants: with no overrides set, behaviour is byte-for-byte today's (Hairdye and `/custom`
  windows unaffected). Hair override with no source hair (bald slot missing) still draws using
  the player's hair tint from `GetAppearance().HairColor`.

API:

```csharp
public void SetSlotGraphic(CharacterSlot slot, int id);
public void ClearSlotGraphics();
public void SetFacing(Direction? facing);
```

Not unit-testable (engine resource loading); proved by the manual smoke in Task 5 and by
regression: open `/hairdye` and `/custom` and confirm they still preview correctly.
Commit: `feat(preview): per-slot graphic and facing overrides`

---

### Task 3: Icons

**Files:**
- Create: `Scripts/UI/StylistIcon.cs`

`static Texture2D? Build(CharacterSlot slot, int id)`:
load `res://Assets/Sprites/{CharacterLayout.TypeFolder(slot)}/{id}/animations.tres`
(`ResourceLoader.Exists` guard → null); first `AnimationNames.Candidates("idle", 3, Direction.Down)`
clip present with frames; `FrameTextureCache.Get(frames, clip, 0)`; crop via
`Image.GetUsedRect()` → `ImageTexture.CreateFromImage(image.GetRegion(rect))`; cache in a static
`Dictionary<(CharacterSlot,int), Texture2D?>`. Tinting is applied by the caller with
`TintMaterial.Apply(button icon rect, tex, hairColor)` so the cache stays colour-independent.

Empty used-rect (fully transparent frame) → return the uncropped texture.
Not unit-testable; covered by Task 5 smoke. Commit with Task 4.

---

### Task 4: `StylistWindow` scene + script, HUD wiring

**Files:**
- Create: `Scenes/UI/StylistWindow.tscn` (copy structure of `HairdyeWindow.tscn`: Background,
  TitleBar/TitleLabel/CloseButton, Content; `WindowName = "Stylist"`)
  - `Content/Tabs` (`TabBar`: Hair, Face)
  - `Content/Scroll/Grid` (`ScrollContainer` + `GridContainer`, 6 columns)
  - `Content/Preview` (`CustomPreviewControl`), `Content/RotateLeft`, `Content/RotateRight`
  - `Content/CostLabel`, `Content/ResetButton`, `Content/ChangeButton`
- Create: `Scripts/UI/StylistWindow.cs`
- Modify: `Scripts/UI/GameHud.cs:29,105` — `public StylistWindow Stylist { get; private set; }`,
  `Stylist = Add<StylistWindow>("res://Scenes/UI/StylistWindow.tscn");`
- Modify: `Scripts/UI/DefaultWindowLayout.cs:35` — add `or "Stylist"`
- Test: `tests/Goose2Client.Tests/DefaultWindowLayoutTests.cs` — `[InlineData("Stylist", true)]`

Behaviour (mirrors `EnchantWindow` lifecycle):
- `_Ready`: wire nodes; `_preview.HideSlot(CharacterSlot.Helm)`; listen to `MakeWindowPacket`,
  `EndWindowPacket`, `CloseWindowPacket`, `StatusInfoPacket` (store `_state.Gold`); subscribe
  `GameManager.Instance.CharacterUpdated`; `ScaleRegister()`. `_ExitTree` removes all.
- `OnMakeWindow` (frame 34 only): store `WindowId`, `Title`; read local player
  `GetAppearance()` → `_state.Open(HairId, FaceId)`; build grid for the active tab;
  `Visible = true`; `Sync()`.
- `OnCloseWindow` for our id / `OnClosePressed` (`WindowButtonClick(Close, WindowId, 0)`): hide,
  `_preview.ClearSlotGraphics()`, `_preview.SetFacing(null)`, `WindowId = 0`.
- Tab change → rebuild grid. Grid cell = `Button` with a `TextureRect` icon
  (`StylistIcon.Build`), hair tinted with `HairColor`; selected cell highlighted, current cell
  framed. Click → `_state.Select(id)`; `Sync()`.
- `CharacterUpdated` for the local player while visible → `SetCurrent` from new appearance,
  re-tint hair icons, `Sync()`.
- Rotate buttons → `_state.RotateLeft/Right()`; `_preview.SetFacing(_state.Facing)`.
- Reset → `_state.Reset()`; `Sync()`. Change → if `_state.CanChange`,
  `NetworkClient.StylistChange(_state.SelectedHair, _state.SelectedFace)`.
- `Sync()`: `_preview.SetSlotGraphic(Hair, SelectedHair)`, `SetSlotGraphic(Eyes, SelectedFace)`;
  cost label `"Cost: " + Cost.ToString("N0")`, red when `!CanAfford`; Change disabled unless
  `CanChange`; Reset disabled when cost is 0; grid highlight refresh.

Red/green: `DefaultWindowLayoutTests` row fails then passes. Build: `dotnet build`. Full
`dotnet test tests/Goose2Client.Tests` (baseline 1,794 + new).
Commit: `feat(stylist): stylist window`

---

### Task 5: Manual smoke (both repos)

1. Server worktree: set `"stylist": true` on a nearby town NPC's properties in a local DB (or
   spawn a test NPC); run the server.
2. Client worktree: `./run.sh` against the local server.
3. Right-click the NPC → window opens centred, titled "Stylist".
4. Hair and Face tabs show 46 / 18 recognisable icons; hair icons tinted in your hair colour.
5. Clicking icons updates the preview; ◀ ▶ rotate it; walking doesn't snap the preview back.
6. Cost shows 0 / 2,000 / 4,000; Change disabled at 0 and when gold is short (red label).
7. Change → gold drops, your character updates, a second client sees the new look; cost
   returns to 0; window stays open.
8. Walk away and press Change → "You are too far from the stylist."
9. Regression: `/hairdye` and `/custom` previews still behave as before.

Report any icon-framing issues (design "verify visually" item).

---

## Design alignment check

- Tabs + grid layout, rotate buttons, Reset/Change, cost label — match design.
- Change disabled at cost 0 and when unaffordable (red) — matches design decision.
- Preview hides helm; hair colour kept; facing override doesn't affect other windows.
- No chat command; opens only from frame 34.
