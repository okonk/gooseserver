# Stylist — Design

## Purpose

Let players change their hair style and face after character creation. Today every character
starts on `StartingHairID` / `StartingFaceID` and the only way to change either is a
`HairCutItem` or a permanent spell effect. The stylist is an NPC-bound window that shows every
hair and face as an icon, previews the selection on the player's own character, and charges
gold for the change.

## Rules

- Offered styles: Illutia hairs **1–46** and faces (Eyes sprites) **1–18**. The imported
  Aspereta hairs (10001–10073) are not offered.
- Cost: **2,000 gold per changed slot** (`StylistChangeCost`). Hair only or face only costs
  2,000; both costs 4,000; nothing changed costs nothing and does nothing.
- Hair colour is kept.
- The window opens only from an NPC with the property `stylist: true`. There is no chat
  command. Placing a stylist NPC is out of scope (added later as data).
- Illusioned or non-layered-body players may still buy; the change is applied to `HairID` /
  `FaceID` and shows once the base body is visible again.

## Server

### Opening

- `PlayerRightClickEvent`: beside the `enchantTable` branch, an NPC with
  `Properties.GetProperty("stylist", false)` calls `StylistWindow.Open(world, player, npc)`.
- New enum values: `Window.WindowFrames.Stylist = 34`, `Window.WindowTypes.Stylist`.

### `Goose/StylistWindow.cs : Window`

Modelled on `EnchantWindow`: `Open` (closes any open stylist window first), `FindOpen`,
`Clicked` (Exit/Close removes the window). Title "Stylist", close-only buttons. No `Populate`
round-trip; the client already knows the player's current appearance.

### Change: client → server `STC<hairId>,<faceId>`

`StylistWindowChangeEvent`, registered as `("STC", …)` in `EventHandler.cs`, parses two ints
and calls `StylistWindow.Change(world, player, hair, face)`, which checks in order:

1. A stylist window is open — otherwise ignore.
2. `Map.InRange(player, window.NPC)` — otherwise notice "You are too far from the stylist."
3. `1 ≤ hair ≤ StylistMaxHairID` and `1 ≤ face ≤ StylistMaxFaceID` — otherwise ignore
   (only a tampered client sends these).
4. `cost = StylistChangeCost × (changed slots)`. Zero → notice "Nothing changed." and stop.
5. `player.Gold < cost` → notice "You need {cost} gold."
6. `RemoveGold(cost)`, set `HairID` / `FaceID`, broadcast `P.UpdateCharacter(player)` to the
   player and players in range (as `HairCutItem.csx`), notice "Your new look cost {cost} gold.",
   and log via `world.LogHandler` (new `Log.Types.Stylist` unless a fitting type exists).

The window stays open after a change. Walking away leaves it open; Change is refused out of
range (same as the enchanting table).

### Settings

`GooseSettings` + `GooseSettings.json`: `StylistChangeCost = 2000`, `StylistMaxHairID = 46`,
`StylistMaxFaceID = 18`.

## Client

### Window

`Scripts/UI/StylistWindow.cs : BaseWindow` + `Scenes/UI/StylistWindow.tscn`, hidden by
default. Like `EnchantWindow`, it handles the window-create packet itself, opens on
`WindowFrames.Stylist = 34` (added to `Scripts/WindowFrames.cs`), keeps the server window id,
and closes on `CLW` / `ENW`. Registered in `GameHud` and in `DefaultWindowLayout`'s centred
popups.

### Layout

```
+-- Stylist --------------------------------+
| [Hair] [Face]            +-------------+  |
| +--+--+--+--+--+--+      |             |  |
| |  |  |##|  |  |  |      |   preview   |  |
| +--+--+--+--+--+--+      |             |  |
| |  |  |  |  |  |  |      +-------------+  |
| +--+--+--+--+--+--+ ^       [◀]   [▶]     |
| |  |  |  |  |  |  | v    Cost: 2,000      |
| +--+--+--+--+--+--+     [Reset] [Change]  |
+-------------------------------------------+
```

- Left: a `TabBar` (Hair | Face) over a scrollable `GridContainer` of icon buttons for
  `StylistCatalog.HairIds` (1–46) or `FaceIds` (1–18). The current style has a "current"
  frame; the selected style is highlighted.
- Right: a `CustomPreviewControl` with the helm hidden, ◀ ▶ rotate buttons, the cost label,
  **Reset** (selection back to current) and **Change** (sends `STC<hair>,<face>`).
- Cost is computed client-side for display only (2,000 per changed slot). Change is disabled
  when the cost is 0 or exceeds the player's gold (from `StatusInfoPacket`); an unaffordable
  cost is shown in red. The server remains authoritative.

### Icons

`StylistIcon` loads `Assets/Sprites/{Hair|Eyes}/{id}/animations.tres`, takes frame 0 of the
down-facing idle clip (`AnimationNames.Candidates` + `FrameTextureCache`), crops to the opaque
bounds, and caches the result. Hair icons are tinted with the player's current hair colour via
`TintMaterial`; face icons are untinted. Built once when the window first opens.

### Preview

`CustomPreviewControl` gains:

- `SetSlotGraphic(CharacterSlot, int id)` / `ClearSlotGraphics()` — per-slot id overrides that
  keep the player's tint and pose, so Hair and Eyes can be overridden together. The existing
  single `SetCustomGraphic` API is unchanged for `/custom` and the hair dye window.
- `SetFacing(Direction?)` — a facing override; `null` follows the local player as today. While
  set, `_Process` no longer snaps back to the player's facing. ◀ ▶ cycle down/left/up/right.

### After a change

The server's `UpdateCharacter` raises `CharacterUpdated`; the window re-reads the current
hair and face, moves the "current" frames, and the cost returns to 0.

## Testing

Server (`Goose.Tests`, following the enchant window tests):

- Hair only −2,000; face only −2,000; both −4,000; neither: no charge, no change, notice.
- Rejected with no change: insufficient gold, NPC out of range, no window open, ids out of
  range (0, 47, 19, negative).
- `UpdateCharacter` reaches the player and nearby players.
- Right-clicking a `stylist: true` NPC opens a frame-34 window.

Client (`tests/Goose2Client.Tests`):

- `StylistCatalog` is exactly hairs 1–46 and faces 1–18, and every id has a sprite resource.
- `STC` formatting from `NetworkClient`.
- Cost and enabled state: 0 / 2,000 / 4,000, disabled when unaffordable.
- `CustomPreviewControl` multi-slot overrides and facing override.
- `DefaultWindowLayoutTests` updated for "Stylist".

Manual: local server + client, right-click a stylist NPC, check icons and rotation, buy a
change, confirm a second client sees it.

## Rollout

Server and client ship together. Old clients never receive frame 34; new clients against an
old server never get the window.

## Deferred / to verify during planning

- **Persistence:** confirm `HairID` / `FaceID` and gold are written by the same save path; if
  not, the plan adds a save so gold cannot be lost without the new look persisting.
- **Gold refresh:** confirm `RemoveGold` pushes a status update so the client's affordability
  check stays current. If not, the only effect is a server "You need X gold" notice.
- **Icon framing:** cropping to opaque bounds is expected to look right; verify visually.
- **Stylist NPC placement:** added later as data.
