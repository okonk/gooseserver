# Ground Zone Styles — Design

Ground zones are drawn by the client from a named style: a tinted floor, edges along the zone's
outside, corner marks and particles. Today's per-tile looping sprite becomes optional. Four styles
ship (`heal`, `earthquake`, `empower`, `poison`) plus a default, together with two new spells and a
resize of the existing two.

Builds on `docs/plans/2026-11-21-ground-aoe-design.md`. Nothing from either design is deployed yet, so
`ZADD` and `SSS` change format freely. Server, client and sheet go live together.

## Decisions

| Topic | Decision |
|---|---|
| Where the style comes from | New `spell_effects.zone_style` text column, sent in `ZADD` and `SSS`. The client owns the style list; the server doesn't validate names. |
| Rendering approach | Drawn in code: one `ZoneView` node per zone (`_Draw` for floor/edges/corners) plus one `CpuParticles2D` per zone. No art assets. |
| Tile sprite | Optional. `spell_animation = 0` means none. A style's own `TileAnimation` beats the packet's. |
| Edges | From each tile's neighbours, not from a bounding rectangle, so any shape (Plus, Cross, map-clipped) outlines correctly, including inner corners. |
| Exclusive overlaps | Every zone draws in full; the client does not compute ownership. Visuals can overstate an older exclusive zone where a newer one covers it. |
| Night | Zones darken with the `CanvasModulate` like everything else. No glow or lights. |
| Particle layer | Below characters, on the zone layer (z 12). |
| Lifecycle | 0.3s fade in on add, 0.3s fade out on removal. No expiry warning. |
| Preview | Styled spells preview with their style (static, no particles); spells without a style keep the plain square. |
| Zone size | All four zones are 7×7 (`target_size = 3`). Area is always odd-width; 6×6 was ruled out. |

## 1. Server and data

**Column.** `zone_style` (Text, default `''`) on `spell_effects`, via the CsvToSql descriptor only (no
`onetimeupdates.sql`); regenerate the snapshot and `tools/DataEditor/schema.js`.
`SpellEffect.ZoneStyle` is read by the loader and copied in the copy constructor.

**Load check.** A GroundZone whose `zone_style` contains a comma is warned and skipped, alongside the
existing zone checks.

**Packets.**
- `ZADD id,anim,file,style,remainingMs,x1,y1,…` — the style sits before the tile list because the
  tiles run to the end of the packet. Empty when unset.
- `SSS…,groundTargetable,shape,size,zoneStyle` — one trailing field, empty for non-zone spells. The
  empty-slot `SSS` is unchanged.

**Sheet rows.**

| Spell | Style | Size | Placement | Anim | Notes |
|---|---|---|---|---|---|
| Sanctuary (167 / effect 344) | `heal` | 3 | Target + ground | 0 | unchanged otherwise |
| Earthquake (168 / effect 345) | `earthquake` | 3 | **Self**, `ground_targetable = 0` | 0 | description updated |
| Empowering Ground *(new)* | `empower` | 3 | Target + ground | 0 | Magus; 30s buff-only zone; zone buff = new Buff effect `spell_damage = 0.25`; SelfPlayer, `works_not_in_pvp = 1`, `zone_overlaps = 1`; 100% MP, 60s aether; scroll |
| Poison Cloud *(new)* | `poison` | 3 | Target + ground | 0 | Rogue; `-0.1 * %cchp - 0.1 * %ccmp`, snapshotted at cast; NPC only; 1s tick, 10s; 100% MP, 30s aether; scroll |

The spell-damage buff stacks with other spell-damage buffs. Zone buffs don't expire on their own, so
"30s" is the zone's duration. Scrolls follow the existing pattern: min level 50, lore, bind on pickup,
graphic 820110/20398.

The sheet needs a `zone style` header at the end of Spell Effects (column CB), and the DataEditor
rebuilt and pushed for the new column.

## 2. Client data

**Packets.** `ZoneAddPacket.Style` (string, after `file`). `SpellbookSlotPacket` reads an optional
trailing `zoneStyle` into `SpellInfo.ZoneStyle` (empty when missing).

**`ZoneEdges`** (pure). For a tile set, per tile: open sides (no zone neighbour), outer corners (two
adjacent open sides) and inner corners (two adjacent closed sides with the diagonal missing).

**`ZoneStyle` record.** `Floor`, `FloorPulse`, `PulseSeconds`, `FloorPattern` {None, Cracks, Haze};
`Edge`, `EdgeWidth`, `EdgeKind` {Solid, Rough, Soft}; `Corner`, `CornerLength` (0 = none); `Particle`
{None, Plus, Dust, Ember, Fog}, `ParticleColor`, `ParticleRate` (per tile per second),
`ParticleLifetime`, `ParticleRise`; `TileAnimation` (optional).

**`ZoneStyles.Get(name)`** — case-insensitive; empty → none; unknown → Default.

**Render decision** (pure):

| Style | Anim | Draw |
|---|---|---|
| known | any | style, plus sprite from style or packet |
| unknown | any | default outline, plus packet sprite if any |
| empty | ≠ 0 | looping sprite only (today's behaviour) |
| empty | 0 | default outline |

## 3. Rendering (`GroundZoneLayer`)

`ZoneView` per zone replaces the per-zone sprite list.

- **Floor:** a rect per tile; alpha × `1 + FloorPulse·sin(2πt/PulseSeconds)`; redraw every frame while
  pulsing.
- **Patterns:** `Cracks` — a few jagged lines per tile from a fixed per-tile seed, alpha flickering with
  the pulse. `Haze` — two or three faint drifting circles per tile.
- **Edges:** along open sides, inset 1px. `Solid` continuous; `Rough` broken segments with fixed
  jitter; `Soft` low alpha 1px.
- **Corners:** L marks of `CornerLength` at outer corners; smaller at inner corners.
- **Sprite:** the existing looping `SpellAnimation` per tile when an animation applies.
- **Fade:** `Modulate.A` 0→1 over 0.3s on add. On `ZDEL` or local expiry: stop emitting, 1→0 over 0.3s,
  free. Re-adding the same id replaces the old view immediately.
- **Particles:** one `CpuParticles2D` per zone, `EmissionShape.Points` from four seeded random points per
  tile. Rate `ParticleRate × tiles`, at most 60 alive. Textures generated in code once: `Plus` 7×7
  white `+` (tinted by `ParticleColor`), `Dust` soft 4px dot, `Ember` 2px dot, `Fog` soft 24px circle
  that drifts sideways.
- **Preview:** a `ZoneView` in preview mode (no particles, pulse or fade); greyed at half alpha out of
  range. Self-cast spells (Earthquake) have no preview.

## 4. Starting styles

Colours are RGBA 0–1.

| | `heal` | `earthquake` | `empower` | `poison` | Default |
|---|---|---|---|---|---|
| Floor | .25,.85,.35,.14 | .35,.24,.14,.22 | .8,.12,.15,.14 | .6,.75,.15,.12 | 1,1,1,.06 |
| Pulse / period | .3 / 2s | .15 / 0.6s | .35 / 1.5s | .25 / 3s | 0 |
| Pattern | None | Cracks | None | Haze | None |
| Edge | Solid .45,1,.55, 2px | Rough .6,.45,.3, 2px | Solid 1,.3,.3, 2px | Soft .7,.85,.25 | Solid 1,1,1,.5, 1px |
| Corners | 6px pale green | none | 6px orange-red | none | none |
| Particles | green `Plus`, .4, 1.2s, rise 18 | tan `Dust`, .3, 0.8s, rise 8 | orange `Ember`, .5, 0.9s, rise 24 | yellow-green `Fog`, .15, 3s, rise 0 | none |

Expect an in-game tuning pass; tuning is client-only.

## 5. Testing

**Client unit:** `ZADD`/`SSS` parse with and without a style; `ZoneEdges` for a 7×7 square, a Plus, a
single tile and a map-clipped zone; `ZoneStyles` lookup (known, case, empty, unknown); render decision
per row; particle points stay inside tiles and repeat for a seed.

**Server unit:** exact `ZADD`/`SSS` strings with a style; comma style rejected; `SpellCloneTests`
covers `ZoneStyle`. **Integration:** CsvToSql creates and loads the column.

**Manual:** for each of the four spells: look matches the style; edges/corners on 7×7 and map-clipped;
fade in/out; visible at night; preview matches landing and greys out of range; mid-zone map join;
several zones don't hurt frame rate.

Client baseline note: `StylistCatalogTests.EveryCatalogId_HasAnimationAsset` fails in a fresh client
worktree (generated sprite assets are untracked); it passes in the main checkout.

## Deferred / open

- **Dimension clones** grow `target_size` by the dimension number: 7×7 becomes 19×19 (361 tiles, ~2.5KB
  `ZADD`) at dimension 6. Balance decision still open.
- **Client-side ownership** for exclusive overlaps (option A) — not now.
- **Glow at night**, an expiry warning, particles above characters, per-style shaders — not now.
- **Off-screen redraws** — pulsing views redraw even when off screen; optimise only if it shows up.
