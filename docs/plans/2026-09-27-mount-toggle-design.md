# Mount toggle design

Date: 2026-09-27
Branch: `feat/mount-toggle`

## Purpose

Change how mounts work so the mount item stays equipped as the normal state, and a
client hotkey toggles the mounted state instead of the player unequipping/re-equipping
the item. This frees the inventory slot the mount would otherwise occupy when not in
use, and keeps the mount out of the inventory where it can be accidentally sold.

## Current behavior

- A mount item (e.g. "Tank", item 651) is equipped in the mount equip slot
  (`EquipSlots.Mount` = 14). Equipping it:
  - shows the mount graphic in `MKC`/`CHP` packets via `Inventory.MountDisplay()`,
  - applies the item's spell effect as an item buff (`EquipCore`, `Inventory.cs`) —
    e.g. effect 259 "Mount Speed II", +128% move speed,
  - makes `Player.IsMounted(world)` true, which blocks `ATT`/`CAST`.
- The client dismounts by unequipping: `USE<InventorySize + 15>` (and can destroy an
  equipped item via `DITM`). The item then sits in the inventory, where it can be sold
  (`VSI`) or dropped (`DRP`).
- On login, `Inventory.Load` re-applies stats and item buffs for every equipped item.
- `IsMounted` already guards for worlds without a mount slot
  (`EquippedSize < 14`, i.e. Aspereta).

## Design

### State

- `Player.Mounted`: a session-only `bool`, default `false`. Not persisted — no
  `toggle_settings` bit, no `player_properties` key, no schema change.
- `Player.IsMounted(world)` becomes: mount slot exists (existing Aspereta guard) **and**
  a mount item is equipped **and** `Mounted` is true.
- Transitions:
  - Equipping a mount item (first mount or replacement) → `Mounted = true`.
  - `MNT` packet → flips `Mounted` (no-op without a mount item equipped).
  - Login → always dismounted. Two paths: server startup — `Inventory.Load` skips
    the mount buff and `Mounted` defaults to false; relogin — the `Player` object is
    reused (players load once at startup, `LogoutEvent` keeps item buffs), so
    `LoginContinuedEvent` (LCNT) dismounts explicitly: `Mounted = false` + remove the
    mount buff, before the buff bar is sent.
  - Death/respawn, map warps, teleports → state is kept. Death needs no special
    handling: item buffs already survive death.

### `MNT` packet and `ToggleMountEvent`

- Register `("MNT", Open(typeof(ToggleMountEvent)))` in
  `EventHandler._SeedCommands`. No payload, no privilege.
- New `Goose/Events/ToggleMountEvent.cs`, same shape as the other client events:
  - Acts only when `Player.State == Ready`.
  - Silent no-op if the world has no mount slot or the player has no mount item
    equipped.
  - Mounting (currently dismounted) is refused when `Map.CanUseItems == false` — the
    same gate that today blocks equipping a mount from the inventory. Dismounting is
    never gated, so a mounted player can always get off.
  - Flips `Player.Mounted` first, then applies/removes the mount's item buff via the
    helpers below, then sends `CHP` (`P.UpdateCharacter`) to the player and every
    player in range. CHP carries both the mount graphic and the move speed, so no
    `SNF` is needed. The client knows locally (it sent the packet), so no ack.

### Mount speed buff lifecycle

Today the item buff is applied in `EquipCore` and removed in `Unequip` (and re-applied
in `Inventory.Load` at login). It moves to the mount-state transitions.

- Two new helpers on `Inventory`. They take the item as a parameter because
  `Unequip` nulls the slot array entry before the buff is removed:
  - `ApplyMountBuff(item, world)` — no-op if the item has no spell effect; otherwise
    builds the item buff (same shape as today: `Caster`/`Target` = player,
    `ItemBuff = true`) and `AddBuff(..., refreshbar: true, updateCharacter: false)`.
  - `RemoveMountBuff(item, world)` — no-op if the item has no effect or no matching
    buff; otherwise finds the buff by `ItemBuff && SpellEffect == item.SpellEffect`
    (today's matching logic in `Unequip`) and
    `RemoveBuff(..., refreshbar: true, updateCharacter: false)`.
- Call sites:
  - `EquipCore`: the generic spell-effect buff block becomes
    `if (equipslot == Mount) { player.Mounted = true; ApplyMountBuff(slot.Item, world); }
    else if (SpellEffect != null) { …existing… }`. The mount branch sits outside the
    null-effect check so visual-only mounts still set the flag.
  - `Unequip` (reachable for the mount via the replacement path and via `USE`/`DITM`):
    mirror image — `player.Mounted = false; RemoveMountBuff(slot.Item, world)` instead
    of the generic buff removal.
  - `Inventory.Load`: skips the item buff for the mount slot (login is always
    dismounted); stats handling unchanged.
  - `LoginContinuedEvent`: relogin dismount (see Transitions above).
  - `ToggleMountEvent`: flip flag, call helper with the equipped mount item, send CHP
    to player + range (helpers use `updateCharacter: false` so CHP goes out exactly
    once for stat-only effects, and it is sent even for visual-only mounts with no
    buff).
- Buff bar: the speed buff shows while mounted and disappears when dismounted — same
  as today, since it is the same buff. It still cannot be killed via `KBUF`
  (`BuffCanBeRemoved` is false).

### Mount slot and display

- No `USE` or `DITM` refusal: unequipping and destroying the mount still work exactly
  as today, for the deliberate "I want to sell this mount" case. When the item is
  unequipped the `Unequip` mount branch runs (`Mounted = false`, buff removed); if the
  player re-equips it later, `EquipCore` auto-mounts again. The protection is
  behavioral — the hotkey removes the unequip-dance from normal play, so the item
  simply stays equipped — not enforced.
- `MountDisplay()` returns `"0,*"` unless the player is mounted **and** has a mount
  item equipped (check `player.Mounted` + the slot; no signature change). `MKC`, `CHP`
  and `/gmhax` pick it up automatically.
- `/custom` GM command: unchanged — its mount preview keeps reading the equipped item
  directly (GM appearance tool, not gated by the mounted state).

### Client/server contract

The client (Goose2ClientGodot, separate repo) must:

1. Bind a hotkey that sends `MNT` (no payload).
2. Render the mount for all characters — self and others — from the mount field of
   `MKC`/`CHP`.

The server side is decoupled from the client's hotkey implementation.

## Deferred / out of scope

- A script or GM command that removes the mount item straight out of the equip slot
  (bypassing `Unequip`): the item check in `IsMounted` makes the state inert, but the
  speed buff could linger. Same class of hole that already exists for every other
  equipped item's stats; not handled.
- Old clients: the equip/unequip dance still works, but after a relog the mount is
  equipped-but-dismounted, so an old client would need to unequip + re-equip to ride
  again. The new hotkey client does not have that quirk.
- Client-side work (hotkey binding, own-character rendering) is out of scope for this
  repo.

## Testing

Existing tests that must keep passing (equipping a mount now auto-mounts):

- `Goose.Tests/MountSpeedTests.cs` — all 8 facts.
- `Goose.Tests/CharacterAppearancePacketTests.cs` — the fixture equips a mount, which
  auto-mounts, so the `MKC`/`CHP` mount-graphic expectations hold unchanged.

New tests (dispatch `MNT` via
`fixture.World.EventHandler.AddEvent(player, "MNT")`, the same path as the real
socket):

1. Toggle on: equipped + dismounted → `MNT` → `IsMounted` true, mount speed active,
   `CHP` to the player with the mount graphic, buff present.
2. Toggle off: mounted → `MNT` → `IsMounted` false, base speed, `CHP` without the
   graphic, buff gone.
3. `MNT` with no mount equipped → no-op (no state change, no CHP).
4. `CanUseItems == false` map: `MNT` refuses to mount; dismounting via `MNT` still
   works.
5. Equip auto-mounts: equipping a mount from the inventory leaves the player mounted
   (speed + graphic).
6. Replacement: equip a second mount while mounted → still mounted, speed switches to
   the new mount's effect, old mount in inventory.
7. Unequip dismounts: `USE` on the mount slot → item in inventory, `Mounted` false,
   buff removed.
8. Login dismounts (integration-style, `Inventory.Load` path): saved player with
   equipped mount → fresh load → mount equipped, `Mounted` false, no speed buff.
9. Death keeps mounted: killing blow while mounted → after respawn, still mounted with
   the speed buff.
10. Visual-only mount (no spell effect): toggle still flips the graphic and
    `IsMounted` (combat block) with no buff involved.
11. Relogin dismounts: a mounted player who logs out and back in (same `Player`
    object, `LCNT` path) is dismounted with the speed buff removed.

## Scope

One plan, ~5 tasks:

1. `Player.Mounted` + `IsMounted` change + relogin dismount hook.
2. `Inventory.ApplyMountBuff`/`RemoveMountBuff` + `EquipCore`/`Unequip`/`Load` mount
   branches.
3. `ToggleMountEvent` + `MNT` registration.
4. `MountDisplay()` gating.
5. New toggle/edge tests (items 1–7, 10, 11) + login-dismount and death tests
   (items 8–9).
