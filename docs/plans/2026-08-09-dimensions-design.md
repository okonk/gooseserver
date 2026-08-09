# Dimensions — Design

Date: 2026-08-09

## Summary

Port the abyssserver "dimension" feature to goose: parallel, scaled-up copies of the
entire world. Dimension 0 is the normal game; dimensions 1–6 are procedurally amplified
clones of it.

Dimension identity is encoded in **ID offsets** (`id + 100000·dim`) rather than composite
keys, so map IDs, spellbook entries and inventory round-trip through the existing schema
unchanged.

This document covers **part 1 of the feature**: the world itself. Spells, items and the
spirit economy are separate later designs — see [Out of scope](#out-of-scope).

Source of truth for behaviour is `~/code/abyssserver` (Java). Line references below point
at it.

## Scope

**In scope**

- Server extension points (7, listed below)
- World cloning: maps, NPC templates, NPC spawns, warp rewiring
- NPC stat scaling
- `/dimension <n>`, entry gating, unlock progress
- Warden NPC + unlock quest chain

**Out of scope** — later parts, unchanged from `docs/dimensions.md`

- Per-dimension spell and spell-effect clones
- Item template clones, deterministic prefixes, rarity rolls, surnames, `/resetitem`
- Spirit (SP) economy, vendor purchase/sell overrides, `/rebirth`

## Decisions

| Decision | Value | Rationale |
|---|---|---|
| Data set | Illutia only | Aspereta unaffected; scripts live under `Goose/Data/Illutia/Scripts` |
| Dimensions | 6 | Matches abyss |
| ID offset | 100000 | Illutia map IDs already reach **10044**, so the 10000 offset in `docs/dimensions.md` collides. Highest generated ID is 610044 |
| Cloning strategy | Eager, at `OnLoaded` | Faithful to abyss; avoids despawn edge cases around respawn timers, ground loot and aggro state |
| Scaling location | Baked into cloned **NPC templates** | Abyss scales per spawn (`NPC.java:860`); baking into the template is equivalent, cheaper, and makes each dimension's boss a distinct template ID |
| NPC ID capacity | `MaxNPCs` 15000 → 250000 | ~82k NPCs. Login IDs are decimal text in packets, so there is no wire-format ceiling. Headroom keeps `NPCHandler.GetNewID`'s random probe at ~1.5 tries |
| Clone construction | Array copy, **not** `LoadData` | See [Map cloning](#map-cloning) |
| Unlock storage | `Player.Properties["dimension.max"]` | New `PropertiesDictionary`, ported from the 3dMMO server |
| Unlock mechanism | Warden NPC + `RequirementType.Kill` | Per-dimension boss templates make the stock `Kill` requirement dimension-aware for free |
| Boss template | `BossTemplateId = 162`, configurable | Abyss used template 162 (King Terror). Illutia's 162 is Shadow Dog |

## Scale

Measured against `~/Downloads/IllutiaGoose_2025-04-15.db` and `Goose/Data/Illutia/Maps/`:

| Entity | Base | ×7 (dims 0–6) |
|---|---|---|
| Maps | 160 | 1,120 |
| NPC spawns | 11,670 | 81,690 |
| NPC templates | 655 | 4,585 |

Item templates (max ID 1606) and spells (max ID 208) are comfortably below the offset.

---

## Server changes

Seven changes. All are generic — none mention dimensions.

### 1. `Player.Properties`

A `PropertiesDictionary` ported from
`~/code/3dMMO-Server/server/MMO.Server/Utilities/PropertiesDictionary.cs`, into
`Goose/PropertiesDictionary.cs`, together with `PropertiesDictionaryJsonConverter.cs`.

Keep `GetProperty<T>`, both overloads, `TryGetProperty<T>`, `ConvertValue`/`IsNumericType`
and `Clone()`. Drop the `[RegisterOrmLiteConverter]` attribute — there is no OrmLite here.

`Clone()` matters: the save path serialises off the game thread, and the snapshot
rationale in that file's doc comment applies here too.

Persisted as JSON in a new column:

```sql
-- sql/players.sql
player_properties TEXT DEFAULT '' NOT NULL

-- sql/onetimeupdates.sql
ALTER TABLE players ADD COLUMN player_properties TEXT DEFAULT '' NOT NULL;
```

Wired into `Player.LoadFromReader` and the hand-rolled INSERT/UPDATE in `Player.cs`
(~lines 661, 842, 918) via the existing `JsonHelper`. An empty string deserialises to an
empty dictionary.

### 2. Widen HP and weapon damage to `long`

`AttributeSet.HP`, `AttributeSet.MP` and `NPCTemplate.WeaponDamage` become `long`.

**Why this is required.** The abyss scaling formulas overflow `int.MaxValue` (2.147e9)
inside the 6-dimension range, wrapping negative — a mob with negative HP dies instantly
and one with negative damage heals its target.

| Mob | dim 3 | dim 4 | dim 5 |
|---|---|---|---|
| Shadow Dog (3,704 HP) | 83M | 782M | **7.35e9 overflow** |
| King Terror (30.1M HP) | **3.21e9 overflow** | 1.55e10 | 7.0e10 |

Damage overflows at dimension 5 for typical mobs (base 200,000 → 6.1e9).

**Why it is tractable.** 34 `.HP` usages and 43 `WeaponDamage` usages. **No migration** —
SQLite `INT` columns have INTEGER affinity and already store 64-bit values. The client
parses HP as `long` (`Goose2Client/Assets/Scripts/Network/Packets/StatusInfoPacket.cs:12,15`).

### 3. `NPCHandler.AddTemplate(NPCTemplate)`

Registers script-generated templates into the handler's dictionary.

### 4. `NPCTemplate` copy support

A copy constructor or equivalent so scripts can derive dimension variants from base
templates. `NPCTemplate.Quests` becomes public (currently `internal`) so the script can
attach quests to the warden template.

`NPC.cs:637` does `this.Quests = template.Quests` — an alias, not a copy — so attaching
to the template before spawns are created is sufficient.

### 5. `QuestHandler` made public, with `AddQuest`

`QuestHandler` and `GameWorld.QuestHandler` are currently `internal`. Both become public,
plus an `AddQuest(Quest)` method.

Note the load-order constraint: `NPCHandler.cs:108` resolves `npc_templates.quest_ids`
against `QuestHandler` at template-load time, which runs **before** global scripts. Sheet-
authored `quest_ids` therefore can never reference script-created quests; the script must
attach them itself.

### 6. `IMapScript.CanPlayerJoin(Map, Player, GameWorld)`

Returns a refusal string, or `null` to allow. Consulted at the top of `Map.PlayerCanJoin`
before the existing level/experience/required-item checks.

`PlayerCanJoin` is the single choke point for both warps (`MoveEvent.cs:123`) and
teleport spells (`SpellEffect.cs:727`), which is why one hook covers both.

### 7. `MaxNPCs` → 250000

`GooseSettings.json`. `BaseSPPercentRegen` and `BaseSPStaticRegen` are already `0`, so no
settings change is needed for the later spirit work.

---

## Scripts

Both under `Goose/Data/Illutia/Scripts/`.

### `Global/Dimensions.csx`

All configuration at the top:

```
Enabled          = true
Dimensions       = 6
Offset           = 100000
StartMapId       = <dimension entry map>
BossTemplateId   = 162
WardenTemplateId, WardenMapId, WardenX, WardenY
```

`Enabled = false` reverts the server to stock behaviour.

Runs at `OnLoaded`, which fires after maps, NPC templates and spawns have loaded.

#### Pass 1 — NPC templates

For each of 655 base templates × 6 dimensions, clone to `npc_id + 100000·dim` and apply
the abyss formulas from `NPC.java:927–967`:

| Property | Formula | Source |
|---|---|---|
| HP | `(base + 100000·2^dim) · 4.7^dim`; `×2` when `dim ≥ 5 && base ≤ 35,000,000` | `NPC.java:927` |
| HP regen | `base + 0.004·(dim + 1)` | `NPC.java:879` |
| Damage | `base·4^dim + 100000·max(0, 4^dim − 3)`; `×20` when `dim ≥ 5 && base < 10,000,000` | `NPC.java:936` |
| Attack speed | `max(base − 0.175·dim, 0.2)`; forced to `0.7` when `dim ≥ 5` and the result is `> 0.5` | `NPC.java:945` |
| Move speed | `max(base − 0.15·dim, 0.15)` | `NPC.java:907` |
| Attack range | `base + dim` | `NPC.java:869` |
| Experience | `(exp + level·100) · 3^min(4,dim)`, `×2^(dim−4)` when `dim ≥ 5` | `NPC.java:954` |
| Respawn | `min(base · 0.85^dim, 3600/(1+dim))`, `dim` capped at 4 | `NPC.java:963` |
| Level | `50` for all dimensions > 0 | `NPC.java:899` |
| Root / stun / slow | rootable `false`, stunnable `false`, slowable `true` | `NPC.java:881` |
| Recolour | RGB `max(base − dim·30, 0)`; alpha `min(base + dim·30, 200)`, hair and equipment alike | `NPC.java:1019` |

Name gets a `" (n)"` suffix. Registered via `NPCHandler.AddTemplate`.

Two details the summary in `docs/dimensions.md` omitted and which are included here: the
HP-regen bonus, and the attack-speed clamp to `0.7`.

#### Pass 2 — Map cloning

For each of 160 base maps × 6 dimensions, build a clone at `map_id + 100000·dim`:

```
Name        = base.Name + " (n)"
CanPVP      = true
Min/MaxExperience = base × (dim·5)²
tiles       = (ITile[])base.tiles.Clone()      // shallow
characters  = new ICharacter[(W+1)*(H+1)]      // fresh — this is what isolates dimensions
```

then insert into `MapHandler.Maps` and attach `DimensionMap.csx`.

**`LoadData` is deliberately not called.** It re-reads and re-parses the map file from
disk and issues two SQL queries keyed on the clone's ID (`Map.cs:466–520`) — queries that
return nothing, because only base maps have `warptiles` and `map_required_items` rows.
Calling it on 960 clones would mean 960 redundant file parses and 1,920 empty queries on
the single-threaded `Database` service.

The array copy is safe because there are only three `ITile` implementations:

| Tile | Present at load? | Handling |
|---|---|---|
| `BlockedTile` | yes | Shared reference — it is an empty marker class with no state |
| `WarpTile` | yes | Replaced in pass 3 |
| `ItemTile` | no — written at runtime by `AddItem`/`RemoveItem` (`Map.cs:191`, `208`) | n/a |

#### Pass 3 — Warp rewiring

Walk each clone's tiles and replace every `WarpTile` with a new instance whose `WarpMap`
points at the same-dimension clone of the original target. This keeps players inside their
dimension, and is why maps are all created before warps are touched.

#### Pass 4 — Spawns

For each of 11,670 base spawns × 6 dimensions:

```csharp
npc.LoadFromTemplate(world, baseMapId + Offset*dim, x, y, dimensionTemplate, shouldRespawn: true)
```

Respawn is self-sustaining on the NPC, so nothing further is required.

#### Pass 5 — Wardens and quests

- One warden template (`NPCTypes.Quest = 12`), cloned per dimension like any other, spawned
  at the configured position on each dimension's starting map.
- Six quests at deterministic IDs `900000 + n`, requirements and rewards at
  `900000 + n·10 + k`. **Deterministic IDs are required**: `QuestProgress` persists keyed on
  `requirement.Id`, so a counter-assigned ID would orphan in-flight kill progress across a
  restart. The base must not collide with sheet-authored quest rows.
- Quest *n*: `RequirementType.Kill` on `BossTemplateId + 100000·n` ×1, with
  `PrerequisiteQuests = [n−1]`, and a `RewardType.Script` reward setting
  `dimension.max = n+1`.

`Player.cs:1020` already records kill progress as
`UpdatePossibleQuestProgress(RequirementType.Kill, npc.NPCTemplate.NPCTemplateID, world)`,
keyed on template ID — so per-dimension boss templates make the stock requirement
dimension-aware with no quest-engine changes.

This differs from abyss, where quest 1 was auto-granted at character creation
(`Player.java:1072`) and auto-completed on the kill with no NPC involved
(`Quests.java:80`). Goose quests are NPC-offered through `QuestWindow` with explicit
accept and turn-in, so the warden supplies the missing giver rather than the quest engine
gaining an auto-grant path.

### `Map/DimensionMap.csx`

Attached to every clone. Because scaling lives in the cloned template, this script is
small:

- `CanPlayerJoin` — refuses when the map's dimension exceeds `dimension.max`, with abyss's
  message: *"The void has rejected you. You have a maximum dimension of N."* (`Map.java:588`)

### `/dimension <n>`

Registered via `world.EventHandler.RegisterEvent`. Rejects `n > dimension.max`, otherwise
warps to `StartMapId + 100000·n`.

### Login clamp

`PlayerCanJoin` covers warps and teleports but **not login** — a player whose saved
`map_id` is a dimension map re-enters with no check. Abyss checked separately at
`Player.java:1458`.

On login, if the saved map's dimension exceeds `dimension.max`, relocate to the
dimension-0 equivalent. Same for `bound_id`.

---

## Pre-implementation verification

**Does `BossTemplateId` 162 have a spawn?** In the April 2025 snapshot,
`SELECT * FROM npc_spawns WHERE npc_id = 162` returns zero rows. If that holds against
current data, quest 0 is uncompletable and the entire chain is blocked at the first step —
nobody reaches dimension 1.

That snapshot is stale (its map IDs top out at 124, versus 10044 in the current `Maps/`
directory), so this must be checked against live data before implementing pass 5. If there
is genuinely no spawn, the script must place one.

## Known limitations

Accepted for this part, listed so they are not mistaken for bugs during testing.

1. **Teleport spells escape dimensions.** `SpellEffect.cs:718` resolves
   `world.MapHandler.GetMap(this.TeleportMapID)` with no dimension component, so a
   teleport from dimension 3 lands in dimension 0. Abyss resolved against the caster's
   dimension (`SpellEffect.java:833`). The goose fix is per-dimension spell clones
   carrying `TeleportMapID = base + 100000·d`, which lands with the spell-cloning part.
   Binds are unaffected — `CastBindSpell` stores the current `Map` object, which already
   *is* the dimension clone.
2. **Dimension NPCs drop dimension-0 loot** until item cloning lands.
3. **PVP is forced on** in every dimension > 0, faithful to abyss. A player unlocking
   dimension 1 steps straight into open PVP.
4. **Groups can straddle dimensions.** Members in different dimensions occupy different
   `Map` objects with separate character lists. Group chat is unaffected; anything doing
   range or same-map checks is untested here.
5. **`/updatesql` requires a restart** when dimensions are enabled. Re-importing reloads
   templates and maps, but clones are created by a script at `OnLoaded` and are not
   recreated, leaving players on maps that no longer exist.

## Follow-ups

Deferred, but should be revisited.

1. **Cloned maps lose the base map's script.** Clones are assigned `DimensionMap.csx`, so
   a base map with its own script — Illutia has `ArenaMap.csx` and `ZombieTownMap.csx` —
   behaves as a plain map in every dimension. Skipping `LoadData` also means `OnLoad`,
   `OnLoadTile` and `OnFinishedLoad` never fire on clones. The fix is for `DimensionMap`
   to compose with, rather than replace, the base map's script.
2. **Idle NPC tick suppression.** `NPC.HandleMoveEvent` reschedules unconditionally, so
   ~82,000 NPCs tick even with nobody in any dimension. Deferred pending measurement; the
   fix is to stop re-arming move events on player-less maps and re-arm them in
   `Map.AddPlayer`. It would benefit the base game too.
3. **Teleport dimension resolution**, per known limitation 1. A `Map.BaseMap` /
   `Map.VariantKey` runtime tag plus `MapHandler.GetVariant(baseId, key)` was considered
   and set aside in favour of the ID-offset approach; it remains the more general answer
   if instanced maps are ever wanted.

## Testing

**Unit-testable in `Goose.Tests`** — the `PropertiesDictionary` round-trip through the
`player_properties` column, the `long` widening, the `CanPlayerJoin` script hook,
`NPCHandler.AddTemplate`, `QuestHandler.AddQuest`, and the schema migration.

**Manual** — the cloning runs inside a `.csx` at `OnLoaded` against live sheet data, so it
is verified by starting the server and checking map count (1,120), NPC count (~82k) and
startup time, then walking a warp in dimension 3 to confirm it lands in dimension 3 rather
than dimension 0.

## Implementation parts

**Part 1 — server extension points.** The seven changes above, with unit tests. No
behaviour change while `Dimensions.csx` is absent.

**Part 2 — scripts.** `Dimensions.csx` and `DimensionMap.csx`: cloning, scaling, warp
rewiring, `/dimension`, warden and quest chain, login clamp.
