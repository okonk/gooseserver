# NPC Unreachable-Teleport (anti-cheese) — Design

**Date:** 2026-10-03
**Status:** Approved
**Branch:** `feat-npc-unreachable-teleport`

## Problem

Players cheese NPCs that are visible to them but blocked from walking to them —
typically a long-range mage attacking from a spot the NPC can never path to. The
NPC can never reach or attack the player, so it takes free damage.

The current mitigation is the NPC's *stuck behaviour*: if it hasn't attacked for
`stuck_timeout` seconds it teleports the aggro target to itself
(`TeleportAggro`) or itself to the target (`TeleportToAggro`) —
`NPC.HandleAttackEvent` (`Goose/NPC.cs:1336`). On one boss map the timeout was
dropped to 5s to shrink the cheese window, but that teleports legitimate players
too: an NPC with a large aggro range that is still walking across the screen
simply hasn't arrived within 5s, so it warps a player who did nothing wrong.

The trigger is purely *time-since-last-attack*, which cannot distinguish
"hasn't reached yet" (legitimate approach) from "can never reach" (cheese).

## Goal

Add an opt-in stuck behaviour that teleports the player **immediately** when the
NPC is provably unable to reach them, while keeping the existing timer behaviour
for everything else. Legitimate players who outrun an NPC are still teleported by
the timer — that is intended, since these NPCs are meant to be tanked.

## Approach (chosen: opt-in behaviour mode)

A new `BehaviourTypes` value, `TeleportAggroIfUnreachable`, that behaves exactly
like `TeleportAggro` (warp-on-timeout, so kiting is still punished) **plus** an
immediate warp when a reachability check proves the aggro target is unreachable.

The boss map switches to this mode and its `stuck_timeout` returns to the standard
20s; the cheese window shrinks from 5s-per-cycle to roughly one attack tick after
the pull.

### Rejected alternatives

- **Upgrade `TeleportAggro` globally** with the same check — no data change, but
  alters every NPC using that mode with no opt-out. Rejected in favour of opt-in.
- **Progress-based stuck detection** (teleport only when the NPC hasn't closed
  distance) — softens timer teleports we want to keep, and doesn't shorten the
  unreachable-mage window. Rejected.

## Design

### Data

Append `TeleportAggroIfUnreachable = 3` to:

- `NPCTemplate.BehaviourTypes` (`Goose/NPCTemplate.cs:26`)
- `CsvToSql.Core/NpcCsvToSql.BehaviourTypes` (`CsvToSql/CsvToSql.Core/NpcCsvToSql.cs:111`)

Appended (not inserted) so existing DB int values don't shift. `NPCHandler`
(`Goose/NPCHandler.cs:142`) int-casts the column; existing rows are unaffected.

### Reachability check — `Map.CanReachTile`

```
bool CanReachTile(int fromX, int fromY, int targetX, int targetY, int radius)
```

Bounded 4-directional BFS from the NPC's tile:

- **Clipped to the aggro-hold box** — `|dx| < Map.RANGE_X` (24), `|dy| < Map.RANGE_Y`
  (16) around the NPC, so at most ~47×31 ≈ 1,457 tiles. This matches the range in
  which the NPC holds aggro (`NPC.HandleMoveEvent`, `Goose/NPC.cs:442`): outside it
  the NPC drops aggro anyway, so "reachable within the vision box" is exactly what
  the NPC can actually accomplish.
- **Static blocking only.** Uses a new `Map.IsTileStaticBlocked(x, y)` — bounds +
  `BlockedTile` + `WarpTile`. It does **not** use `IsTileBlocked`, which counts
  character occupancy; a temporary crowd is not cheese, so characters are
  pass-through.
- **Success condition:** any reached tile with Chebyshev distance ≤ `radius` from
  the target point (`radius = NPC.AttackRange`), matching the attack check at
  `Goose/NPC.cs:1376-1377` (`|dx| <= AttackRange && |dy| <= AttackRange`).
- **Early exit** on success; the common "legit player in the open" case resolves
  after a few dozen tiles.
- **Visited buffer:** a scratch stamp buffer sized to the clipped box, reused
  across calls (the game loop is single-threaded), so no per-map allocation.

### Trigger — `NPC.HandleAttackEvent`

The fast path runs **before** the existing timeout switch, gated by `!rooted`
(stunned NPCs already return earlier; rooted NPCs must never teleport). For each
attack-event tick, when:

- `Behaviour == TeleportAggroIfUnreachable`, and
- `AggroTarget` is non-null and on the same map, and
- Chebyshev distance to target > `Math.Max(4, AttackRange + 1)`

run `CanReachTile` with `radius = AttackRange`. If **unreachable** → warp the
player to the NPC with the same `WarpTo(..., loseaggro: false)` call as
`TeleportAggro`, set `LastAttackTime = TimeNow`, and send `StuckMessage` via
`SendMessageToRange` (the warp falls through to the attack check like the
`TeleportAggro` timer branch, rather than returning). If **reachable** →
fall through to the untouched timeout logic.

The `LastAttackTime` reset doubles as a spam guard: worst case is one warp per
attack cycle, never a per-tick loop.

The existing timeout switch gains `TeleportAggroIfUnreachable` on the
`TeleportAggro` branch so the new mode still timer-warps a reachable-but-fleeing
player.

### Threshold rationale

`Math.Max(4, AttackRange + 1)`: below this the NPC is about to attack anyway, so
the BFS is pointless. The floor of 4 (the user's suggested value) avoids BFS for
nearby targets; the `AttackRange + 1` term ensures we never fast-path when the
NPC could already be attacking (which would reset `LastAttackTime` regardless).

## Decisions & accepted trade-offs

- **First check lands on the first attack tick after aggro**, not on `AddAggro`
  itself (aggro sets `LastAttackTime` so it doesn't teleport instantly —
  `Goose/NPC.cs:943`). Cheese window ≈ one attack-speed cycle. Revisit if too slow.
- **`radius = AttackRange`, uncapped.** A large-`AttackRange` boss reads as
  "reachable" unless fully walled off. Accepted.
- **Fast path evaluates `AggroTarget` only.** A non-top-aggro cheese player in a
  group is not fast-pathed. Matches existing `TeleportAggro` semantics; the lone
  mage (the motivating case) is always the target. Accepted.
- **Box clipping** can misjudge a player reachable only via an off-box path (big
  U-shaped wall). Not a regression: the greedy `NextStepTo` can't follow that path
  either, so today's `TeleportAggro` timer-warps that same player anyway. Accepted;
  a future "expand clip box to NPC∪player bounds" refinement is noted but not built.

## Testing

Integration tests (`Goose.IntegrationTests`, following existing event-driven NPC
test patterns), using a synthetic map with a sealed pocket where existing fixtures
lack one:

1. **Cheese** — walled pocket, player inside, NPC in new mode, long timeout: player
   warped within ~2 attack ticks (proves the fast path, not the timer).
2. **Legit approach** — player in the open across the map within the vision box: no
   warp before the timer; warped at timeout (kiting rule preserved).
3. **Threshold** — player adjacent: no BFS, normal attack, no warp.
4. **Rooted** — rooted NPC with unreachable target: no warp.
5. **`CanReachTile` unit tests** — gap in a wall → reachable; sealed pocket →
   unreachable; portal-only route → unreachable; crowd in a doorway → reachable.

## Rollout

Data change, not code: switch the boss-map NPC to `TeleportAggroIfUnreachable` and
restore `stuck_timeout` from 5s to 20s. Every other NPC keeps current behaviour
until opted in. Rollback = set the mode back to `TeleportAggro`.

## Non-goals

Leash/reset, progress tracking, BFS result caching, and an `AddAggro`-time hook —
all revisit only if field data warrants.

## Files touched

- `Goose/NPCTemplate.cs` — new enum value
- `CsvToSql/CsvToSql.Core/NpcCsvToSql.cs` — mirrored enum value
- `Goose/Map.cs` — `CanReachTile`, `IsTileStaticBlocked`, scratch buffer
- `Goose/NPC.cs` — fast path in `HandleAttackEvent`; new mode on the timeout branch
- `Goose.IntegrationTests/*` — new tests
