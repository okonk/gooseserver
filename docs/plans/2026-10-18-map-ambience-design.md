# Map Ambience (Weather, Day/Night, Default Player Light) — Design

Date: 2026-10-18
Branch: `map-ambience` (server and client)
Source: 3dMMO-Server `docs/plans/2026-03-04-map-ambience-design.md`,
`client/docs/plans/2026-03-04-map-ambience-client-design.md`, and the default-light part of
`docs/plans/2026-03-30-player-lighting-design.md`.

## Goal

Port 3dMMO's map ambience into Goose and Goose2ClientGodot: per-map weather, a day/night tint
cycle, the `settime`/`setweather` GM commands, and the client-only default light on the local
player. Config comes from two new columns on the maps sheet with the same names and JSON format
as 3dMMO.

## Scope

In scope:
- `weather` and `lighting` columns on the maps sheet (CsvToSql descriptors, DataEditor, map editor schema)
- Server `MapAmbience`, per-map `WeatherRollEvent`, `AMB` packet, `/settime`, `/setweather`
- Client `AmbienceManager` (CanvasModulate, weather particles, fog, lightning) and the default
  local-player `PointLight2D`

Deferred:
- Item/equipment lights (`LightResolver`, item light properties, light state on character packets,
  lights on other players/NPCs)
- Weather audio, gameplay effects, shadows

## Decisions

| Decision | Choice |
|---|---|
| Column names / format | Same as 3dMMO: `weather` `{"Clear":0.7,"Rain":0.3}`, `lighting` `{"preset":"outside","cycleDuration":1440}` |
| Column position | Between `pets_enabled` and `script_path` |
| Default when blank | New `none` preset: no tint, no day clock. Blank weather = Clear, no change |
| Weather tick | Per-map self-rescheduling `WeatherRollEvent` (like `ClearMapItemsEvent`) |
| Weather change rule | Every 10 min roll 50%; hit = weighted pick among configured types other than current; miss = keep for another 10 min |
| Day clock | Calculated on demand from a start timestamp; no ticking |
| Dimension clones | Copy config and day-clock start (time matches parent); weather rolls independently |
| GM commands | `/settime <time>`, `/setweather <weather>`; current map only |
| Default light | Included (client-only); item lights deferred |
| Light texture | Copy 3dMMO `light_gradient.png` |
| DB migration | None; DB is regenerated from the sheet |
| Sheet edits | Done by the user; we change the schema only |

## 1. Data and config

**CsvToSql** (`CsvToSql/CsvToSql.Core/MapsCsvToSql.cs`), inserted after `pets_enabled`:

```csharp
Col.Text("weather", def: "''").HeaderText("weather"),
Col.Text("lighting", def: "''").HeaderText("lighting"),
```

Descriptors map to sheet columns by position, so the sheet must gain both columns at the same
place (the user updates the sheets).

**Lighting JSON fields:** `preset`, `cycleDuration` (seconds), `startAlpha`, `endAlpha`,
`tintColor`, `fillColor` (`#rrggbbaa`). Overrides apply on top of the preset.

**Presets:**

| Preset | StartAlpha | EndAlpha | Cycle |
|---|---|---|---|
| none (default) | 0 | 0 | 0 (static) |
| outside | 0 | 0.70 | 1440 s |
| inside | 0.1 | 0.2 | 1440 s |
| cave | 0.70 | 0.70 | 1440 s |

Fill `#001429ff`, tint `#090909ff` for all presets unless overridden. If `cycleDuration` is not
given, it comes from the preset.

**Weather JSON:** type name → weight. Types: `Clear=0, Rain=1, Fog=2, Storm=3, Snow=4`.

**Server types** (new, in `Goose/`): `WeatherType`, `LightingConfig`, `WeatherDistribution`,
`ColorUtils.ParseHex` (`#rgb`, `#rrggbb`, `#rrggbbaa` → `uint`, packed `0xRRGGBBAA`).

**Loading:** `MapHandler.LoadMaps` reads both columns into `Map.Weather` / `Map.Lighting` using
System.Text.Json (case-insensitive). Error handling:
- blank, invalid JSON, or unknown preset → `none`
- invalid weather JSON → no weather
- unknown weather key → skipped
- each bad cell logs a warning with the map id

**Clones:** `Weather` and `Lighting` are added to the `CloneAs` field block.

## 2. Server runtime

**`MapAmbience`** (`Goose/MapAmbience.cs`), one per `Map`, created at the end of `LoadData` and
in `CloneAs`.
- Day clock: stores `cycleStartTicks`; elapsed = `(now − cycleStartTicks) mod duration`. A new map
  starts at 9am. `SetTimeOfDay(hours)` moves `cycleStartTicks`. A clone copies the parent's
  `cycleStartTicks`.
- Weather: starting weather is a weighted roll at creation (Clear if no config).
  `CurrentWeather`, `SetWeather(type)`.
- `Start(world)`: schedules the `WeatherRollEvent` if the weather config is non-empty. Called from
  `MapHandler.LoadMaps` (next to `ClearMapItemsEvent`) and from
  `Goose/Data/Illutia/Scripts/Global/Dimensions/Maps.csx` for clones (next to the sweep event).
- Builds the `AMB` packet; sends to one player or broadcasts with `world.SendToMap`.
- Keeps a reference to its pending roll event so `/setweather` can remove and reschedule it.

**`WeatherRollEvent`** (`Goose/Events/`), `Data = map`. On `Ready`:
1. Roll 50%.
2. On a hit, pick by weight among configured types ≠ current. If one is picked, set it and
   broadcast. If none qualify (for example a single-type map already at that type), do nothing.
3. Reschedule for 10 minutes later.

**Packet** — server `P.MapAmbience`, client `MapAmbiencePacket`:

```
AMB<startAlpha‰>,<endAlpha‰>,<tint>,<fill>,<elapsedMs>,<durationMs>,<weather>
```

Alphas are integer thousandths (0.70 → 700) because the client parser has no float reader.
Colors are decimal `uint` (client reads with `GetInt64`). Weather is the enum byte.

**When it's sent:**
- `DoneLoadingMapEvent`, after the `MWP` lines (login and every map change)
- broadcast to the map on a weather change, `/settime`, `/setweather`

## 3. GM commands

`Goose/Commands/SetTimeCommand.cs` and `SetWeatherCommand.cs`, following `RespawnMapCommand`.
Both act on `ctx.Player.Map`, `Section = "Admin"`.

- `/settime <time>`: 0–24 or `dawn` (6), `noon` (12), `dusk` (18), `night` (0). Errors on a map
  with no day cycle. Replies `Set time to 18:00.`
- `/setweather <weather>`: `Clear|Rain|Fog|Storm|Snow`, case-insensitive. Sets the weather,
  broadcasts, and removes and reschedules the pending roll event 10 minutes out. On a map with no
  weather config, the GM weather lasts until the next `/setweather` or a restart. Replies
  `Set weather to Rain.`
- New `AccessPrivilege.SetMapTime` and `SetMapWeather`, granted to the same access-level sets as
  `RespawnMap`.
- No logging.

## 4. Client (Goose2ClientGodot)

**`MapAmbiencePacket`** (`Scripts/Network/Packets/`): prefix `AMB`; alphas `‰ / 1000f`; colors
`(uint)GetInt64()`; client-side `WeatherType` enum; unknown values → Clear.

**`AmbienceManager`** (`Node2D`): created by `MapManager._Ready` as a child of `World`, inside the
map's SubViewport. A port of 3dMMO `client/Assets/Scripts/AmbienceManager.cs`:
- Listens for `MapAmbiencePacket`; removes the listener in `_ExitTree`.
- `CanvasModulate` day/night tint: `Darkness = lerp(start, end, 1 − sin(timeOfDay·π))`; color
  blends from white toward the tint by `Darkness`. Duration 0 = static at `startAlpha`. Inside the
  SubViewport it tints only the world; the HUD and `WorldTextBridge` sit outside and are not tinted.
- `CpuParticles2D` rain/snow/storm attached to the map `Camera2D`. The emission rect is sized from
  the viewport size when created (3dMMO hard-codes 1920 px / −1200 px).
- `CanvasLayer` inside the viewport holding a fog `ColorRect` (alpha 0.3, 2 s tween) and a
  lightning flash (every 5–15 s during Storm).
- Exposes `Darkness`, `TimeOfDay`, `CurrentWeather`, `WeatherChanged`.
- Resets on its own with each new Map scene. Packets that arrive while loading are queued until
  after `MapManager._Ready`, so none are lost.
- The darkness curve and `UintToModulateColor` go in a static helper so they can be tested
  without Godot.

**Default local light:** in `MapManager.AttachLocalPlayer`, the local `Character` gets a
`PointLight2D`: radius 80 (texture base radius 128), additive blend, warm white `#FFF0E0`,
energy `0.6 × ambience.Darkness` each frame. Texture: `light_gradient.png` copied from 3dMMO
`client/Assets/Textures/`. Non-local characters get no light.

## 5. Schema, editors, tests

**Schema regeneration:**

```bash
dotnet run --project tools/SchemaGen -- tools/DataEditor/schema.js tools/DataEditor/schema.json
```

- DataEditor: `schema.js` is checked in and guarded by `Checked_in_schema_js_is_up_to_date`.
  Check that the new fields land sensibly in the Maps form (`layout.js`); Maps is already
  `RESTART_ONLY`.
- Map editor: copy `schema.json` to the client's
  `src/MapEditor.GameData/Schema/game-data-schema.json`. The map editor only reads Maps but checks
  headers by position.
- Rollout order: the user adds the columns to the sheet, then the new map editor build is
  published. Old builds will report a Maps header mismatch once the sheet changes.
- `CsvToSqlSnapshotTests` snapshot is regenerated (`GOOSE_UPDATE_SNAPSHOT=1`) once the sheet has
  the columns.

**Server tests (`Goose.Tests`):**
- `ColorUtils.ParseHex` formats, invalid, null/empty
- Config parsing: blank/invalid/unknown → `none`; preset values; overrides; unknown weather key;
  bad weather JSON
- `MapAmbience` with a fake clock: elapsed time wraps; `SetTimeOfDay`; `none` → duration 0; a
  clone's time matches its parent
- `WeatherRollEvent` with a fake random: miss keeps weather and reschedules; hit picks a different
  configured type and broadcasts; single-type map at its type does nothing; `Start` schedules
  nothing without a config
- `AMB` packet format; `DoneLoadingMapEvent` sends `AMB` after `MWP` (pattern: `MapWarpLoadTests`)
- `/settime` and `/setweather`: parsing, no-day-cycle error, rescheduling, privilege gate

**Client tests (`tests/Goose2Client.Tests`):**
- `MapAmbiencePacketTests`: fields, ‰ conversion, colors above `int.MaxValue`, unknown weather → Clear
- Darkness curve and modulate color math

**Manual check:** set a map to `{"preset":"outside","cycleDuration":120}` and
`{"Rain":0.5,"Snow":0.5}`; use `/settime night` and `/setweather Storm`.

## Assumptions

- Only Goose2 clients connect; older Goose2 builds ignore the unknown `AMB` prefix.
- Clones are never replaced at runtime (`Maps.csx` throws on a duplicate id), so the weather event
  needs no "map was replaced" check.
