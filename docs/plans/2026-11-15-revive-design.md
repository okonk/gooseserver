# Revive System Design

Date: 2026-11-15
Status: Approved

## Overview

Players who die enter a **dead** state instead of instantly respawning. While dead they
lie on the tile where they died, can chat and watch, but cannot move, attack, cast, use
items or interact with NPCs. Another player can cast a **Revive** spell on the corpse to
bring them back where they lie at half HP/MP. If nobody revives them within the revive
timeout, or they press "Respawn now", they respawn exactly as deaths work today.

Spans the server (this repo) and the client (`../Goose2ClientGodot`).

## Naming

- `Player.Respawn(world)` — timeout, "Respawn now" button, or logout: today's respawn flow.
- `Player.Revive(world)` — revive spell: stand up in place.
- `GooseSettings.ReviveTimeout` — the window (seconds) during which a revive is possible.
- `RespawnTimeoutEvent` — scheduled at death, calls `Respawn`.

## Server: state and lifecycle

New on `Player`: `IsDead`, `DeathRespawn` (`RespawnLocation?` captured at death),
`RespawnDeadline` (tick the timeout fires) and the scheduled `RespawnTimeoutEvent`
reference.

New setting `GooseSettings.ReviveTimeout` (seconds, default 60). `0` disables revive
server-wide (every death is an instant respawn, as today).

New map script hook `bool AllowsRevive(Map map, Player player, ICharacter killer, GameWorld world)`
on `IMapScript`, default `true` in `BaseMapScript`. `ArenaMap.csx` overrides it to
`false`. Dimension maps keep the default (revive allowed).

### On death (`Player.Attacked`, the `CurrentHP <= 0` branch)

1. Broadcast "X was slain by Y." and call `NotifyDied` as today, so map and global
   `OnPlayerDied` scripts still run at the moment of death. Store the returned
   `RespawnLocation` in `DeathRespawn`.
2. If `ReviveTimeout == 0` or `AllowsRevive` returns false: run today's instant respawn
   (the existing code path, now `Respawn`). Done.
3. Otherwise:
   - `IsDead = true`, `CurrentHP = 0`, `CurrentMP = 0`.
   - Remove non-item buffs (existing loop) and send the buff bar.
   - Dismount via the existing dismount path (mount item buff removed normally).
   - Destroy all pets (`pet.Destroy(world)`, as `LogoutEvent` does). Today pets despawn
     only as a side effect of the death warp; without a warp this must be explicit.
   - Clear aggro on the player from in-range NPCs.
   - Close all open windows (vendors, stylist, enchant, custom, amount).
   - Schedule `RespawnTimeoutEvent` at now + `ReviveTimeout`; store `RespawnDeadline`.
   - Broadcast CHP (carrying the respawn seconds, see Wire protocol) to in-range players
     and the player; send vitals and status.

### `Respawn(world)`

Clears `IsDead`, sets HP 50% / MP 10% (today's values), warps to
`DeathRespawn ?? bind`, restarts regen, sends vitals/status. The warp's ERC + MKC
(alive) resets the sprite for viewers.

### `Revive(world)` (called with the reviver)

Clears `IsDead`, sets `CurrentHP = MaxHP / 2`, `CurrentMP = MaxMP / 2`, no warp, discards
`DeathRespawn`, restarts regen, broadcasts CHP (seconds = 0), sends vitals/status, and
sends the target `P.Notice(NoticeType.Info, "<reviver> revived you.")`. The reviver gets
no notice. Public so scripts can call it.

### Timeout staleness

`RespawnTimeoutEvent` only acts if the player `IsDead` and the event is still the one
stored on the player (a revive followed by a second death schedules a new event; the
old one becomes a no-op).

### Logout / disconnect

`LogoutEvent` calls `Respawn` before saving when `IsDead`, so the player is saved at the
respawn point and logs back in alive. A crash while dead needs no special handling:
login already sets HP/MP to 80% (`LoginEvent`), so they log in alive at the corpse tile.

### Warping a corpse

GM summon/approach or script warps leave the player dead. The re-sent MKC carries the
dead state and remaining seconds; the timeout keeps running; `DeathRespawn` still points
at the location captured at death.

### Regen and tiles

`RegenEvent` early-outs while `IsDead`. The corpse still occupies its tile and blocks
movement like a living player.

## Server: Revive spell

New `SpellEffect.EffectTypes.Revive = 22` (appended after `Script`). Configured in spell
data like any other effect (`TargetType = Target`, `Effected = Player`, cost, cooldown,
animations, `WorksInPVP`).

Single gate at the top of `SpellEffect.CastSpell`:

- Target is a dead `Player` → only `Revive` proceeds; every other effect type (formula,
  buffs, teleport, script, …) returns false. AoE spells therefore skip corpses.
- `EffectTypes.Revive when target is Player { IsDead: true }` → `CastReviveSpell` →
  `target.Revive(world)`, returns true.
- Revive on a living player, NPC or pet returns false. Mana is spent as with any other
  failed cast (existing behaviour, unchanged).

Formula fields are ignored for Revive; HP/MP are always half max.

Corpse immunity outside `CastSpell`: `Attacked` returns early when `IsDead` (damage and
heals), and `NPC.AggroIfInRange` skips dead players.

## Server: action gating while dead

Early return when `Player.IsDead`.

Silent: `MoveEvent`, `FacingEvent`, `PlayerAttackEvent`, `ToggleMountEvent`, `EmoteEvent`.

With `P.Notice(NoticeType.Info, "You can't do that while dead.")`:

- `PlayerCastSpellEvent`
- `InventoryUseEvent`, `PlayerDropItemEvent`, `PickupItemEvent`, `DestroyItemEvent`,
  `OpenCombineBagEvent`
- NPC branch of `PlayerLeftClickEvent` and `PlayerRightClickEvent` (clicking players
  still works)
- `WindowButtonClickEvent`, `VendorPurchaseInventoryEvent`, `VendorSellInventoryEvent`,
  `InventoryToWindowEvent`, `WindowToInventoryEvent`, `WindowToWindowEvent`,
  `EnchantWindowSlotEvent`, `EnchantWindowCreateEvent`, `CustomWindowSlotEvent`,
  `CustomWindowCreateEvent`, `StylistWindowChangeEvent`, `AmountWindowSubmitEvent`
- `/dropgold`

Allowed: chat (`;`, `/tell`, `/shout`, group/guild chat), info commands, group commands,
inventory slot moves/splits, spellbook paging/swap, backpack toggle, kill-buff, all GM
commands. `/buyvita` and `/buymana` are allowed unless they refill current HP/MP, in which
case they are blocked (check during implementation).

## Wire protocol

### `MountDisplay` fix (prerequisite)

`Inventory.MountDisplay()`'s tinted branch emits a trailing comma (`id,r,g,b,a,`) while
the other branches (`0,*`, `id,*`) do not. All callers use it as the last token, so the
comma is removed. Separate commit.

### MKC / CHP trailing field: `RespawnSeconds`

Players' MKC (`P.MakeCharacter`) and CHP (`P.UpdateCharacter`) gain a final token:
seconds until respawn, `0` = alive, clamped to `>= 1` while dead. Appended as
`"," + seconds` after the mount for layered bodies; for non-layered bodies the existing
tail already ends with a separator. NPC and pet packets are unchanged. Hand-built MKCs in
`HairdyeCommand`, `GmHaxCommand`, `CustomCommand` and `BodyStealerNPC.csx` are unchanged
(the client treats the missing token as alive).

While dead, both builders send `player.BodyID` instead of `CurrentBodyID` (also for the
`layered` decision), so item/script illusions show the real body on the ground. No state
is mutated; on revive the next CHP sends `CurrentBodyID` again.

Sent when:

- Death → CHP to in-range players and self (seconds > 0).
- Revive → CHP (seconds = 0).
- Respawn → warp ERC + MKC (seconds = 0).
- Any MKC of a corpse (viewer enters range, map load, warped while dead) carries the
  remaining seconds, so late viewers get the correct countdown.

### `RSPN` (client → server)

No arguments. Registered in `EventHandler._SeedCommands` as `RespawnRequestEvent`. Calls
`Player.Respawn(world)` if `IsDead`, otherwise ignored. Check for prefix clashes in the
packet trie.

### Notice

`"<reviver> revived you."` to the target via `P.Notice(NoticeType.Info, …)`.

## Client (Goose2ClientGodot)

### Parsing

- `MakeCharacterPacket` and `UpdateCharacterPacket` read an optional trailing
  `RespawnSeconds` (default 0): after the mount for layered bodies, after the last
  existing token for non-layered.
- Remove `MakeCharacterPacket`'s `EndsWith('*')` hide_name fallback for servers that
  predate hide_name (the server always sends it; client and server ship together).
- Add an `RSPN` send helper.

### Character

- `IsDead` and `RespawnDeadline` (now + seconds) set from every MKC/CHP.
- Dead: each slot shows the static `death-<facing>` frame (it's a lying-on-the-ground
  sprite, not an animation). Slots without death art (hands/weapons/shields, bodies
  without the clip) are hidden. `AnimationNames.Candidates` gains a `"death"` case that
  resolves to blank when absent.
- The pose is locked while dead: movement, facing, attack and cast poses don't override
  it. Dead → alive returns to idle.
- Name label shows the countdown, e.g. `Name (0:47)`, ticking locally from
  `RespawnDeadline` for every corpse in view.
- HP bar reads empty (HP% is 0 in MKC).

### Local player gating

While the local character `IsDead`, the client does not send moves/turns, `ATT`, `CAST`
(hotbar and hold-cast), `USE`, `DRP`, `GET`, `MNT`, or left/right clicks on NPCs.
Clicking players, chat, inventory rearranging and windows keep working. Corpses remain
targetable (Revive needs to target them); the server rejects non-Revive spells.

### Respawn window (`RespawnWindow`)

A small `BaseWindow` (like `ConfirmWindow`), opened when the local player becomes dead,
deadline refreshed on each MKC/CHP. Text "You have died. Respawning in 0:47…" counting
down locally; one button "Respawn now" sends `RSPN`. Not closable while dead; button
disables at 0 while waiting for the server warp. Closes on dead → alive. Default
centred placement.

## Testing

Server (`Goose.Tests`, synthetic world):

1. `MountDisplay` tinted branch has no trailing comma; other branches unchanged.
2. Death with revive on: `IsDead`, HP/MP 0, non-item buffs removed, dismounted, pets
   destroyed, NPC aggro cleared, windows closed, `OnPlayerDied` called, `DeathRespawn`
   stored, timeout scheduled, no warp. Revive disabled (`ReviveTimeout = 0` or
   `AllowsRevive` false) → instant respawn as today.
3. Respawn: timeout → warp to `DeathRespawn` or bind, 50%/10%. Stale timeout is a no-op.
   `RSPN` dead → respawn; alive → ignored. Logout while dead → respawn before save.
4. Revive: half HP/MP, same tile, notice to target only, CHP with 0. Revive on living
   player / NPC / pet → false. Other effect types on a corpse → false; AoE skips corpses.
5. Corpse immunity: `Attacked` no-op (damage and heal); `AggroIfInRange` skips; regen
   doesn't tick.
6. Gating: each blocked handler is a no-op while dead (notice where specified); chat,
   `/tell`, inventory slot change still work.
7. Packets: MKC/CHP end with `RespawnSeconds` (0 alive, >= 1 dead) for unmounted,
   untinted-mount, tinted-mount and non-layered shapes; dead player's packets carry
   `BodyID`; NPC/pet packets unchanged.
8. Warping a corpse keeps it dead and the new MKC carries the remaining seconds.

Server (`Goose.IntegrationTests`): `ArenaMap.csx` `AllowsRevive` returns false.

Client (`tests/Goose2Client.Tests`):

1. MKC/CHP parse trailing `RespawnSeconds` for each tail shape; absent → 0; existing
   MKC fixtures still parse with the hide_name fallback removed.
2. `AnimationNames`: `"death"` → `death-<dir>`, blank when absent.
3. Pure-logic helpers: dead pose lock rule, countdown formatting (`0:47`, clamp `0:00`),
   dead input gate predicate.
4. Manual: die, watch countdown, revive from a second client, respawn early, timeout
   expiry, summoned while dead.

## Settled decisions

- Approach: standalone `IsDead` flag (Player `State` stays `Ready`), not a new `States`
  value or a buff.
- Timer expiry and the early-respawn button both run today's respawn.
- Map scripts opt out via `AllowsRevive`; arena opts out, dimensions don't.
- Logout while dead respawns first; dead state is never persisted.
- Corpses stay dead when warped.
- Corpses can't be damaged, healed or hold aggro; Revive is the only action on them.
- Reviver gets no notice; failed Revive casts spend mana.
- Dead state travels as a trailing MKC/CHP field holding the respawn seconds; no
  separate packet. Everyone in range sees the countdown above the name.
- Dead players render with their base `BodyID`.
- Pets are destroyed on death; players dismount on death.
- No GM `/revive` command.

## Deferred gaps

- G1: Party frames show a dead member only as 0 HP; no dead indicator.
- G2: Old clients ignore the trailing field and draw corpses standing; the server still
  blocks their input.
- G3: Hand-built MKC/CHPs in GM/cosmetic commands and `BodyStealerNPC.csx` don't carry the
  dead field (shown as alive if used on a corpse). The two hand-built preview MKCs (Hairdye,
  Custom) gain a `hide_name` 0 token so the client can drop its no-hide_name fallback.
- G4: Assumes all player base bodies are layered; a non-layered base body would render
  nothing while dead.
