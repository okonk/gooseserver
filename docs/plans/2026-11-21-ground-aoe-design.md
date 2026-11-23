# Ground AoE Zones — Design

A new spell effect type that leaves a zone on the ground for a set duration. Every tick it
damages or heals whoever stands on its tiles, and it can hold a buff on anyone standing inside
(removed the moment they leave).

## Decisions

| Topic | Decision |
|---|---|
| Placement | Per spell. `Self` → caster's tile. `Target` → target's tile, pinned there. `Target` + `ground_targetable` also accepts a click on any tile in view range. |
| Tick rate | Per effect, `zone_tick_ms`, default 1000. First tick lands on cast. |
| Scaling | Flat per tick, using the existing HP/MP/SP formulas. Crits roll per tick. |
| Count per caster | Unlimited. Balanced with `Aether`. |
| Overlap | Default *exclusive*: the newest exclusive zone owns a tile, of **any** spell or caster; older exclusive zones go quiet on that tile. `zone_overlaps = 1` keeps a zone out of ownership entirely: it always ticks on all its tiles and never suppresses or is suppressed. |
| Duration | `effect_duration` (seconds). |
| Caster gone | The zone lasts its full duration. It holds the `Caster` reference like `Buff.Caster` does, so formulas use live caster stats, same as existing DoTs. |
| Zone buffs | `zone_buff_effect_id` links a Buff/Snare/TickBuff effect, held while standing on an active tile and removed on leaving. Stun/Root rejected at load. |
| Shapes | Reuse `target_type`/`target_size`. `Random` rejected for zones at load. |
| Visual | New persistent `ZADD`/`ZDEL` packets with a tile list; the client loops the animation. No per-tick packets. |
| Schema | New columns go in CsvToSql schema/data only — no `onetimeupdates.sql` entries. |

## 1. Data model

**`spell_effects`**
- `effect_type = 23` → `EffectTypes.GroundZone`
- `zone_tick_ms` (int, default 1000)
- `zone_buff_effect_id` (int, default 0 = none)
- `zone_overlaps` (bool, default 0)
- Reused: `target_type`, `target_size`, `effect_duration`, `hp/mp/sp_change_formula` (empty = buff-only
  zone), `spell_effected`, `min/max_level_effected`, `works_in_pvp`/`works_not_in_pvp`,
  `spell_animation`/`spell_animation_file` (looping ground visual), `taunt_aggro`.
- `SpellEffect` gains `ZoneTickMs`, `ZoneBuffEffectID`, `ZoneBuffEffect` (resolved in the post-load pass
  alongside `OnMeleeAttackSpell`), `ZoneOverlaps`. All copied in the copy constructor (`SpellCloneTests`).

**`spells`**
- `ground_targetable` (bool, default 0). Only meaningful with `spell_target = Target`.

**Load validation (warn and skip):** a GroundZone with `target_type = Random`; a zone buff whose effect is
Stun or Root; an unknown `zone_buff_effect_id`.

**Examples**
- *Fire Patch* — Target + ground_targetable, Area 1 (3×3), 8s, 1000ms, HP `-(INT*0.4)`, Effected NPC|Player.
- *Sanctuary* — Self, Area 2 (5×5), 10s, 2000ms, HP `+(INT*0.3)`, +AC zone buff, `zone_overlaps = 1`.
- *Tar Pit* — Target, Area 1, 6s, no formula, 40% snare zone buff.

## 2. Server runtime

**Types**
- `GroundZone`: `Id` (world-unique), `Effect`, `Caster`, `Map`, `Tiles` (fixed set), `CreatedAt`,
  `ExpiresAt`, `Overlaps`.
- `GroundZones` (one per `Map`, held by the map): the zone list plus `tileOwner`
  (`Dictionary<(x,y), GroundZone>`, exclusive zones only).

**Shape tiles.** Extract the shape loops in `SpellEffect.Cast()` into
`GetShapeTiles(shape, size, anchor, facing)`, used by both normal area spells and zones. Characterisation
tests pin current behaviour before the extraction. The client ports this function for previews.

**Cast**
1. `SpellEffect.Cast()` gets a `GroundZone` branch. Anchor: caster tile (Self), target tile (Target), or
   clicked tile (§3).
2. Compute tiles with `GetShapeTiles`.
3. Create the zone. Exclusive zones take ownership of every tile. Characters on tiles whose owner changed
   get a zone-buff re-check.
4. Send `ZADD` to the map, run the first tick immediately, and schedule a `GroundZoneTickEvent` every
   `ZoneTickMs`.

**Tick.** For each tile: skip it if the zone is exclusive and not the owner. Otherwise apply to the
character on it, if they are alive, pass `CanCastSpell` (Effected bits, level limits, PvP flags) and
haven't been hit by this zone this tick. Applying means `CastFormulaSpell(caster, target)` when there is a
formula, plus taunt/aggro. Ticks don't touch buffs and send no animations; battle text comes from the
formula path.

**Zone-buff re-check (per character)**
- Wanted: the `ZoneBuffEffect` of each zone that covers their tile, is active there (owner, or
  overlapping) and can affect them.
- Held: buffs with `Buff.SourceZone != null`.
- Remove what is held but not wanted. Add what is missing as non-expiring buffs through normal
  `AddBuff`, like item buffs. For a same-effect handover, re-point `SourceZone` instead of removing and
  re-adding, so the buff bar doesn't flicker.
- Runs after: `Player.MoveTo`, `NPC.MoveTo`, `WarpTo`, spawn/respawn, revive, a tile's owner changing,
  and zone expiry (for characters on its tiles).
- All zone buffs are cleared on death, logout, `Map.RemovePlayer`/`RemoveNPC`. Dead players (corpses stay
  on their tile) are never eligible.
- `buff_removable` is ignored for zone buffs; a removed one comes back on the next step.

**Expiry.** Remove the zone, rebuild `tileOwner` for its tiles (the newest older exclusive zone takes
over), re-check buffs on those tiles, send `ZDEL`. Zones are memory-only; a restart clears them.

**Visibility.** `ZADD`/`ZDEL` go to the whole map. `Map.AddPlayer` sends a `ZADD` for each active zone to
a joining player.

## 3. Client protocol and UI

| Dir | Packet | Notes |
|---|---|---|
| S→C | `SSS…,remainingMs,groundTargetable,shape,size` | 3 trailing fields, sent for every spell. |
| C→S | `CAST slot,0,x,y` | Tile click. |
| S→C | `ZADD id,anim,file,remainingMs,x1,y1,x2,y2,…` | On creation, and on map join with the remaining time. |
| S→C | `ZDEL id` | On expiry. The client also expires zones itself when `remainingMs` runs out. |

**Server `PlayerCastSpellEvent`.** A 4-part `CAST` is accepted only when the slot's spell is
`ground_targetable`, the tile is on the map, and it is inside the caster's view box (`RANGE_X`/`RANGE_Y`).
A rejected one counts toward `SuspectedMacroCount`. An accepted one goes through `Player.CastSpell` (costs,
aether, stun, mount) via an anchor-tile overload.

**Client**
- `ZoneManager` per map: looping tile-animation sprites per zone id; cleared on map change.
- `SpellTargetManager`: keyboard reticle and character click unchanged. For ground-targetable spells, a
  left click on a tile with nobody on it sends `CAST slot,0,x,y`.
- Preview shadow for spells whose shape is neither `Target` nor `Random`: a translucent overlay on the
  hovered tile (ground-targetable spells) or on the reticle target, greyed out of range. `Self` spells
  stay instant with no preview.
- `ShapeTiles`, a port of `GetShapeTiles`, tested against a fixture table shared with the server.

## 4. Testing

**Server unit (`Goose.Tests`)**
- Shape characterisation per type/size/facing, before the extraction; exported as the shared fixture.
- Load validation and `SpellCloneTests` coverage.
- Ownership: newest exclusive wins; handback on expiry; overlapping zones ignore ownership both ways.
- Ticks: only active tiles; one hit per tick; Effected/level/PvP filters; first tick on cast; stops at
  expiry.
- Zone buffs: enter/leave; same-buff handover without remove/re-add; takeover and expiry remove; death,
  warp and logout clear; overlapping zones keep the buff until the last one ends.
- 4-part `CAST` validation.
- Exact `ZADD`/`ZDEL`/`SSS` strings; `ZADD` on map join with the correct `remainingMs`.

**Server integration** — CsvToSql creates the columns; spells load from the DB.

**Client** — `SSS` parse with and without the new fields; `ZADD`/`ZDEL` parse; `ShapeTiles` vs the
fixture; `ZoneManager` add/remove/clear; tile click sends 4-part `CAST` only for ground-targetable spells.

**Manual** — Fire Patch, Sanctuary and Tar Pit CSV rows: preview vs landing, looping art, mid-zone map
join.

## Implementation split

1. **Part 1 — Server foundation:** shape characterisation and `GetShapeTiles`; new columns and load
   validation; `GroundZone`, ownership, ticks (Self/Target placement).
2. **Part 2 — Server buffs and protocol:** zone buffs and movement hooks; `ZADD`/`ZDEL`, map-join sync;
   `SSS` fields; 4-part `CAST`.
3. **Part 3 — Client:** packets, `ZoneManager`, tile click, preview, `ShapeTiles`.

## Deferred / open

- **Looping art (open):** do existing tile animations loop cleanly, or do zones need new assets? Check
  in-game during Part 3.
- **Particle effects:** discuss later; could be an optional field on `ZADD`.
- **`SPAS`** batched one-shot tile packet for existing area spells and arena telegraphs.
- **Ally concept / group-only heals:** heal zones with `Effected=Player` also heal enemy players in PvP,
  same as today's targeted heals.
- **Keyboard tile cursor** for picking an empty tile without a mouse.
- **Server-wide zone cap:** none for now.
- **To verify in planning:** kill credit/XP from a zone whose caster has logged out; client handling of
  unknown packet prefixes (old clients and `ZADD`); `PacketParser` tolerating extra trailing `SSS` fields;
  empty-ground clicks not clashing with click-to-move.

## Changes made during planning

Recorded in the part plans; listed here so the design stays the single overview.

- **Zone tick interval** is clamped to at least 100 ms in code rather than validated at load.
- **Zone buffs can't be clicked off** (the design said they'd come back on the next step).
- **Allowed zone buff types:** Buff, TickBuff, Snare. TickBuff zone buffs don't replay their tick animation.
- **A timed copy of the same buff is left alone;** the zone copy appears on the next step after it expires.
- **Zone-buff eligibility** checks both the zone's and the buff's `CanCastSpell`; Snare stays NPC-only.
- **Group-target ground zone spells** are rejected at load.
- **Previews** show for every targeted spell with a predictable area shape, not only ground zones.
- **The `ZADD` animation file** is ignored by the client, as with `SPA`.
- **Heal zones for other players** need `works_not_in_pvp = 1` on normal maps, as targeted heals do today.
- **Dimension clones grow `target_size` by the dimension number**, so a 3×3 zone becomes 15×15 at dimension 6.
  Not changed; needs a balance decision.
