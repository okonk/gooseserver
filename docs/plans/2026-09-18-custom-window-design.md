# Custom Item Window — Design

A client window for customising items, replacing the chat-driven `/custom` flow. Opened by
using a custom ticket (item script). The player drags an item for the look and an item for
the stats into two slots, picks a tint via a 2D gradient + RGB sliders + alpha slider, sees
a live full-body preview, names the item, and presses Create.

Spans two repos:

- Server: `illutiagooseserver` (branch `custom-window`)
- Client: `Goose2ClientGodot` (branch `custom-window`)

The `/custom` command is unchanged and keeps working alongside the window.

## Protocol

New `WindowFrames.Custom = 28` (server `Window.WindowFrames`, client `WindowFrames`),
`WindowTypes.Custom` (server). Window opened via existing `MKW`: title "Custom", buttons
`0,1,0,0,1` (close + OK-as-Create). No new `ButtonTypes` — Create is the OK button.
No `WTL` lines.

Three new packets, comma-separated, matching existing style:

### `CWS<lookInvSlot>,<statsInvSlot>` — client → server

Sent on every drop or clear into a window slot, carrying the full current state of both
window slots; `0` = empty. The client never waits until both are filled — dropping a look
item first must produce the preview immediately.

Server (routed to the player's open `CustomWindow`; the "one window per player" invariant
means no window id is needed):

- Resolves each non-zero slot against the **main inventory container only** (combine bag,
  equipped, and bank are unreachable by construction).
- Validates each non-zero item with the shared equipment rules (below).
- Applies the same-type rule only when both slots are non-zero.
- On success with a non-zero look slot: replies `CWG<equippedId>,<pose>`.
- On failure: server message; the client clears the slot its `CWS` changed.

The server keeps no per-slot state between `CWS` messages — each carries the full state.

### `CWG<equippedId>,<pose>` — server → client

Sent only in response to a valid `CWS` with a non-zero look slot. `equippedId` is the look
item's `GraphicEquipped` (0 is valid — allowed, no special handling). `pose` is the look
item's `BodyState`; the client needs it for weapon art variants (1H/2H/staff clips differ
by pose) and does not carry it in local `ItemStats`. The client applies it to the look slot
of the pending `CWS` (strict 1:1 request/response over the single ordered connection).

### `CWC<lookInvSlot>,<statsInvSlot>,<r>,<g>,<b>,<a>,<name>` — client → server

The create request. Name is trimmed and comma-stripped by the client before sending; the
server re-applies the same sanitization.

Errors to the player are plain server messages, same as `/custom`.

## Validation rules (shared)

Factored out of `CustomCommand.ValidateCustomSlots` into a reusable helper used by both
`/custom` and the window:

- Both items: `UseType` Armor or Weapon.
- Excluded slots: Ring, Necklace, Pauldrons, Cloak, Belt, Gloves.
- Same equipment type for look and stats, with the existing exception that OneHanded and
  TwoHanded weapons may mix.
- `GraphicEquipped == 0` is allowed, handled no differently.
- Mounts pass (same as `/custom`); the preview simply has no mount layer.

RGBA: R/G/B 0–255, A 0–200. Validated on both client (slider/gradient limits) and server
(create and `CWS` are not colour-bearing, so at create). A modified client sending out-of-
range values is rejected with a server message.

Name: required (non-empty after trim), max 255 (client limits the field, server truncates
as backstop), commas stripped both sides — same treatment as `/custom`.

Source restriction: look/stats items must come from the main inventory. Client only accepts
drops whose drag source is the inventory window. Server resolves slot numbers against the
main inventory container only. Equipped items cannot be customised in place (unequip first,
same as `/custom`).

## Server side

### `CustomWindow : Window` (`Goose/CustomWindow.cs`)

- `WindowTypes.Custom`, `WindowFrames.Custom = 28`, buttons `0,1,0,0,1`.
- Constructed by the ticket's item script in `OnUseConsumableEvent`: stores the ticket's
  `ItemSlot` on the window and sends the `MKW`. If the player already has a `CustomWindow`
  open, the use is refused ("already customising").
- `Clicked(ShowOk)` → create logic. Close/Exit removes the window from `player.Windows`;
  nothing is consumed.
- Handles `CWS` (validation + `CWG` reply) and `CWC` (create).

### Ticket item script

A new item script bound to the custom ticket template (`GooseSettings.CustomTicketId`)
opens the window on use. Use does not fire from the combine bag, so the ticket is
effectively main-inventory-only; no extra check.

### Create logic (on OK / `CWC`)

1. Re-fetch all three items fresh from inventory (ticket slot, look slot, stats slot) —
   nothing cached from drop time.
2. Re-run full validation: ticket still the ticket; both items present; equipment rules;
   same-type rule; RGBA ranges; name non-empty after trim, commas stripped, truncated to
   255.
3. Optimistic capacity check: free inventory slots + the 3 slots being consumed ≥ 1. A
   full inventory is allowed because the consumes free slots; the check is a backstop.
   Runs after all validation, before any consumption.
4. All valid → consume ticket, look item, and stats item; build the `Item` exactly as
   `CustomCommand.Make` does (stats from the stats item, look/pose/graphic from the look
   item, new RGBA, name, `Description = "Custom created by <player>"`, bound flag, script
   params, title/surname properties, `GraphicTile`/`GraphicFile` copied from the look
   item); place it in the ticket's slot (a freed slot, stack 1); log via
   `Log.Types.CreatedCustom` in the same format as `/custom`.
5. Send the updated inventory slots, a server message, and close the window (CLW).

On any failure: server message, nothing consumed, window stays open for retry.

## Client side (Goose2ClientGodot)

### `CustomWindow` (`Scripts/UI/CustomWindow.cs`)

A `BaseMultipleWindow` matched on `WindowFrames.Custom` via a manager (pattern of
`OptionListWindow`/`QuestWindow`).

Layout: title bar + close; two labeled drop slots ("Look" and "Stats") showing local item
icons; full-body preview panel; colour section (2D RGB gradient pad + R/G/B sliders 0–255
+ A slider 0–200 with numeric readouts, all kept in sync); name `LineEdit` (max 255,
commas filtered on input); Create button (the OK button from `MKW` flags).

- **Drop handling:** reuses the `CombineBagContainerWindow` drag-and-drop plumbing.
  Client pre-validation: armor/weapon use-type only, no invisible slots, same-type rule
  with the 1H/2H exception, drag source must be the inventory window. Rejected drops bounce
  back with no round-trip. Accepted drop/clear → update the slot icon, send `CWS` with the
  full two-slot state (0 for empty).
- **Full-body preview:** a static idle-pose render of the **local player** — body, hair,
  face, and all current equipment layers (no mount) — composited the same way
  `Character`/`VitalsCharacterDisplay` do (per-slot `SpriteFrames` from
  `res://Assets/Sprites/...`, idle-down frame). The layer matching the look item's
  equipment type is replaced with the custom graphic from `CWG` (equipped id + pose; pose
  selects the weapon clip variant), tinted with the current RGBA via the existing tint
  shader (alpha as blend factor).
- **Live updates:** slider/gradient input only updates the tint shader param on the
  preview layer — no re-render, no server traffic. The preview layer is (re)built on
  `CWG` arrival, on window open (empty look slot → normal equipment shown), and when the
  local player's appearance updates (re-equip while the window is open).
- **Create:** enabled when both slots are filled, RGBA in range, and name non-empty.
  Sends `CWC` (name trimmed, commas stripped), then disables the button until CLW or an
  error message.
- **Errors:** a server message while a `CWS` is pending → clear the slot that `CWS`
  changed. `CWC` failure → window stays open (no CLW) so the player can adjust and retry.
- Closing the window (X or CLW) discards everything.

Layout math lives in `CustomWindowMetrics.cs`, unit-tested like `OptionListMetrics`.

## Edge cases

- Item moved after drop, before create: create re-validates fresh state and is refused
  with a server message; the preview may be stale (accepted, no sync mechanism).
- Ticket used twice: refused while a `CustomWindow` is open.
- Window closed without creating: nothing consumed.
- Logout/death with window open: window dies with the session; nothing consumed.
- Rapid `CWS`: each validated independently; last write wins per slot.
- Out-of-range RGBA via modified client: rejected server-side.

## Testing

Server (`Goose.Tests` / `Goose.IntegrationTests`):

- Unit: RGBA validation (RGB 0–255, A 0–200, both ends); name sanitization (trim, comma
  strip, 255 truncate, empty refusal); the shared slot-validation helper (equipment rules,
  invisible-slot exclusions, same-type rule, 1H/2H exception) exercised by both `/custom`
  and the window.
- Integration (script-driven, like the existing item-script tests): use ticket → window
  opens; second use refused while open; `CWS` valid/invalid drops (right `CWG` / right
  error, `CWG` only for the look slot); create happy path (all three consumed, item in the
  freed slot with correct stats/look/RGBA/name/description/bound/title-surname, log entry,
  window closed); create failures (item moved away, full inventory with no freed slot, bad
  RGBA, empty name, non-inventory slot number) — nothing consumed on failure; optimistic
  capacity (full inventory, consumes free a slot → allowed).

Client (`tests/Goose2Client.Tests`):

- `CustomWindowMetrics` layout tests (pattern of `OptionListMetricsTests`).
- Drop pre-validation logic as pure functions (use-type, invisible slots, same-type +
  1H/2H exception, source-window check), unit-tested without Godot scene instantiation
  (pattern of `WindowButtonFlagsTests`).
- `CWS`/`CWC` packet parse/format round-trips and `CWG` parse.

Manual: end-to-end in the Godot client against a dev server — drag/drop, gradient +
sliders live-tinting the full-body preview, weapon pose variants, create flow, error paths.
