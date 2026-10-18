# Map Ambience Part 1 (Server) Implementation Plan

**Goal:** Goose reads `weather`/`lighting` from the maps sheet, runs a per-map day clock and weather
roll, sends an `AMB` packet on map load and on change, and provides `/settime` and `/setweather`.

**Architecture:** A `MapAmbience` object per `Map` holds resolved lighting, the day-clock start
timestamp, and current weather. The day clock is calculated on demand from `now`. A per-map
self-rescheduling `WeatherRollEvent` (same pattern as `ClearMapItemsEvent`) rolls every 10 minutes.
Config is parsed once in `MapHandler.LoadMaps` and copied to dimension clones in `Map.CloneAs`.

**Tech Stack:** .NET 10, xUnit, System.Text.Json, ClosedXML (CsvToSql), Node test runner (DataEditor).

Design: `docs/plans/2026-10-18-map-ambience-design.md`. Part 2 (client) is planned separately in
the Goose2ClientGodot worktree.

**Repo rule (AGENTS.md):** no new comments or doc strings unless the "why" is non-obvious. The
only expected comment is the wire format above `P.MapAmbience`, matching the `MWP` precedent.

---

## APIs verified

| API | Location |
|---|---|
| `Col.Text(string name, SqlType type = null, string def = null)` | `CsvToSql/CsvToSql.Core/Schema/Col.cs:18` |
| `Column.HeaderText(string)` | `CsvToSql/CsvToSql.Core/Schema/Column.cs:61` |
| Descriptors map to sheet columns **by position** (`row.Cell(i + 1)`) | `CsvToSql/CsvToSql.Core/CsvToSqlBase.cs:33-35` |
| Fixture header contract (Maps headers must equal fixture row 1, by index) | `Goose.IntegrationTests/Schema/WorkbookHeaderContractTests.cs:13-26` |
| Snapshot fixture input `Fixtures/aspereta-data.xlsx`, regen via `GOOSE_UPDATE_SNAPSHOT=1` | `Goose.IntegrationTests/CsvToSqlSnapshotTests.cs:26,31` |
| SchemaGen emits `schema.js` + map editor `schema.json` (Maps is a consumed sheet) | `tools/README.md:48-60`, `tools/SchemaGen/SchemaJson.cs:18` |
| `Checked_in_schema_js_is_up_to_date` | `tools/Tools.Tests/SchemaJsTests.cs:112` |
| DataEditor `LAYOUTS` only for the wide sheets (no Maps entry) | `tools/DataEditor/src/layout.js:1-10` |
| `DbDataReader.GetString(this, string column)` | `Goose/DataReaderExtensions.cs:13` |
| `MapHandler.LoadMaps` row loop, then `LoadData` + `ClearMapItemsEvent` per map | `Goose/MapHandler.cs:34-85` |
| `Map` field block + `CloneAs` | `Goose/Map.cs:62-63`, `Goose/Map.cs:116-146` |
| `Map` logger `log` | `Goose/Map.cs:12` |
| `GameWorld.TimeNow` = `Stopwatch.GetTimestamp()`; `TimerFrequency` = `Stopwatch.Frequency` | `Goose/GameWorld.cs:189-192`, `Goose/GameWorld.cs:212` |
| `GameWorld.Random` (`System.Random`) | `Goose/GameWorld.cs:183-187` |
| `GameWorld.SendToMap(Map, string)` sends only to `Players` with `State == Ready` | `Goose/GameWorld.cs:878-887` |
| `EventHandler.AddEvent(Event)` / `RemoveEvent(Event)` (reference equality) | `Goose/EventHandler.cs:296`, `Goose/EventHandler.cs:309-312` |
| `Event.Ticks`, `Event.Data`, `abstract Ready(GameWorld)` | `Goose/Event.cs:9-25` |
| Recurring per-map event pattern | `Goose/Events/ClearMapItemsEvent.cs:10-37` |
| `DoneLoadingMapEvent` sends `DSM`, then `MWP` lines, then `MKC` | `Goose/Events/DoneLoadingMapEvent.cs:43-47` |
| Packet builder style (`public static Func<…, string>` on `P`) | `Goose/Packets.cs:459-460` |
| Clone creation + manual sweep-event scheduling | `Goose/Data/Illutia/Scripts/Global/Dimensions/Maps.csx:90-113` |
| `[Command(key, privilege, Section, Help, Usage)]`; key ends in a space when args follow | `Goose/Commands/CommandAttribute.cs`, `Goose/Commands/GrantTitleCommand.cs:3` |
| `CommandContext.Player/World/Send` | `Goose/Commands/CommandContext.cs:5-21` |
| Missing required arg → binder returns usage | `Goose/Commands/CommandBinder.cs:75-86` |
| GameMaster gets every `AccessPrivilege`; `RespawnMap` granted to no other level | `Goose/AccessLevels.cs:56-58` |
| `TestWorldFixture.AddBaseMap / CommandPlayerOn / RunCommand`, `CapturingPlayer.Sent` | `TestSupport/TestWorldFixture.cs:69,101-121,146` |
| `MapWarpLoadTests` map-load harness (`DriveMapLoad`, `Packets`) | `Goose.Tests/MapWarpLoadTests.cs:91-99` |
| `System.Random.NextDouble()` is `virtual` (fakes subclass `Random`) | .NET BCL |
| `InternalsVisibleTo` test assemblies | `Goose/Goose.csproj:20-25` |

## Threading, persistence, publication

- **Thread:** `LoadMaps` runs at startup before the event loop. `WeatherRollEvent.Ready`, command
  `Execute`, and `DoneLoadingMapEvent.Ready` all run inside `EventHandler.Update` on the game
  thread. `MapAmbience` is single-threaded and needs no locks.
- **Persistence:** two new `maps` columns. **No migration.** The DB is regenerated from the sheet
  (confirmed by the user). A stale DB without the columns makes `GetString("weather")` throw at
  startup, which is the intended loud failure.
- **Publication:** for base maps the order is: `LoadMaps` builds `Map` → parses config → creates
  `Ambience` → registers in `maps` → `LoadData` → `ScheduleWeatherRoll`. For clones, `Maps.csx`
  calls `CloneAs` (copies config + ambience) → `MapHandler.Maps[id] = clone` → `ScheduleWeatherRoll`
  next to the sweep event. The roll event only fires from `EventHandler.Update`, after
  registration. Maps are never removed at runtime (`Maps.csx:82-84` throws on duplicate ids), so
  there is no teardown path.

---

### Task 1: Weather/lighting types and config parsing

Pure types with no world dependency.

**Files:**
- Create: `Goose/WeatherType.cs`, `Goose/ColorUtils.cs`, `Goose/LightingConfig.cs`, `Goose/WeatherDistribution.cs`
- Test: `Goose.Tests/AmbienceConfigTests.cs`

**API:**

```csharp
public enum WeatherType : byte { Clear = 0, Rain = 1, Fog = 2, Storm = 3, Snow = 4 }

public static class ColorUtils
{
    public static bool TryParseHex(string? hex, out uint rgba);
}

public sealed record LightingConfig(double StartAlpha, double EndAlpha, uint TintColor, uint FillColor, double CycleDurationSeconds)
{
    public static readonly LightingConfig None;
    public static LightingConfig Parse(string? raw, out string? warning);
}

public sealed class WeatherDistribution : Dictionary<WeatherType, float>
{
    public static WeatherDistribution Parse(string? raw, out string? warning);
}
```

**Rules:**
- `TryParseHex` accepts `#rgb`, `#rrggbb`, `#rrggbbaa` (leading `#` required, case-insensitive).
  It packs as `0xRRGGBBAA`; `#rgb` expands each nibble (`#fff` → `0xFFFFFFFF`); a missing alpha is
  `FF`. Anything else returns false.
- Presets (case-insensitive, trimmed): `none` (0, 0, cycle 0), `outside` (0, 0.70, 1440),
  `inside` (0.1, 0.2, 1440), `cave` (0.70, 0.70, 1440). Default tint `#090909ff`, fill `#001429ff`.
- `LightingConfig.Parse`: blank → `None`, no warning. Invalid JSON → `None` + warning. A missing
  `preset` key → `none`. Unknown preset → `None` + warning. Overrides (`cycleDuration`,
  `startAlpha`, `endAlpha`, `tintColor`, `fillColor`) apply on top of the preset. A bad color
  override keeps the preset color + warning. Alphas clamp to [0, 1]. A negative or non-finite
  `cycleDuration` keeps the preset value + warning.
- `WeatherDistribution.Parse`: blank → empty, no warning. Invalid JSON → empty + warning. Unknown
  type names are skipped (case-insensitive `Enum.TryParse`) + warning. Non-positive or non-finite
  weights are skipped.
- JSON: `System.Text.Json` with `PropertyNameCaseInsensitive = true`, deserializing to a private
  DTO with nullable fields.

**Step 1: Write failing tests** (`AmbienceConfigTests`):
- `ParseHex`: `#fff` → `0xFFFFFFFF`; `#001429` → `0x001429FF`; `#090909ff` → `0x090909FF`;
  `"090909"`, `"#12"`, `"#gggggg"`, `null`, `""` → false.
- `Lighting_Blank_IsNoneWithoutWarning`: `""` and `null` → `LightingConfig.None`, warning null.
- `Lighting_EachPreset`: values per table above.
- `Lighting_MissingPresetKey_IsNone` (adversarial: 3dMMO defaulted to `outside`):
  `{"cycleDuration":100}` → alphas 0/0, cycle 100.
- `Lighting_UnknownPreset_FallsBackToNoneWithWarning`: `{"preset":"dusky"}` → `None`, warning not null.
- `Lighting_InvalidJson_FallsBackToNoneWithWarning`: `{preset:` → `None`, warning.
- `Lighting_OverridesApplyOnTopOfPreset`: `{"preset":"cave","endAlpha":0.9,"tintColor":"#ff0000"}`
  → start 0.70, end 0.9, tint `0xFF0000FF`, fill default, cycle 1440.
- `Lighting_BadColorOverride_KeepsPresetColorWithWarning`.
- `Lighting_AlphaClamped`: `{"preset":"outside","endAlpha":4}` → end 1.0.
- `Weather_ParsesCaseInsensitiveKeys`: `{"clear":0.7,"RAIN":0.3}` → {Clear:0.7, Rain:0.3}.
- `Weather_UnknownKeySkippedWithWarning`: `{"Clear":1,"Hail":1}` → {Clear:1}, warning.
- `Weather_NonPositiveWeightSkipped`: `{"Clear":0,"Rain":1}` → {Rain:1}.
- `Weather_InvalidJson_EmptyWithWarning`.

**Step 2:** `dotnet test Goose.Tests --filter FullyQualifiedName~AmbienceConfigTests` → compile failure (types missing).

**Step 3:** Implement the four files.

**Step 4:** Same command → PASS.

**Step 5: Commit** `feat(map-ambience): weather/lighting config types and parsing`

---

### Task 2: Schema columns, fixture workbook, generated schemas

**Files:**
- Modify: `CsvToSql/CsvToSql.Core/MapsCsvToSql.cs` (insert after `pets_enabled`, before `script_path`)
- Modify (binary): `Goose.IntegrationTests/Fixtures/aspereta-data.xlsx` (Maps sheet)
- Regenerate: `Goose.IntegrationTests/Fixtures/generated.snapshot`, `tools/DataEditor/schema.js`, `tools/DataEditor/schema.json`

**Mutation impact:**
- Source of truth: `MapsCsvToSql.GetColumnDescriptors()` (`MapsCsvToSql.cs:7-26`).
- Readers: `SchemaRegistry.Tables` (`CsvToSql/CsvToSql.Core/Schema/SchemaRegistry.cs:60`) → CsvToSql
  import (positional), `TestSupport/SchemaDdl.cs` (test DDL), SchemaGen → DataEditor `schema.js`
  and map editor `schema.json`, `WorkbookHeaderContractTests`, `CsvToSqlSnapshotTests`.
- Derived state: `schema.js` and `schema.json` (checked in, regenerated), the snapshot (checked in, regenerated).
- Propagation: descriptor edit → insert fixture columns → run SchemaGen → regenerate snapshot.
  Part 2 copies `schema.json` into the client.
- Invariants: descriptor index *i* header == fixture Maps row-1 cell *i*. Existing Maps rows keep
  `script_path`/`script_params` values (no shift).
- Proof: `WorkbookHeaderContractTests` (Maps) and the snapshot diff show only the two new empty
  columns with no `script_*` values moved.

**Step 1: Descriptor.** Add:

```csharp
Col.Text("weather", def: "''").HeaderText("weather"),
Col.Text("lighting", def: "''").HeaderText("lighting"),
```

**Step 2: Red.** `dotnet test Goose.IntegrationTests --filter "FullyQualifiedName~WorkbookHeaderContract"`
→ FAIL on Maps column 16 (expected `script path`, got `weather`).

**Step 3: Fixture workbook.** In the scratchpad (not the repo), create a throwaway console project
referencing `ClosedXML` 0.105.0 (same version as `CsvToSql.Core.csproj:8`). It opens the fixture's
`Maps` sheet, runs `ws.Column(16).InsertColumnsBefore(2)`, sets `ws.Cell(1,16) = "weather"` and
`ws.Cell(1,17) = "lighting"`, and saves. Before saving, confirm `ws.Cell(1,15)` is
`pets_enabled (1)` and the old `ws.Cell(1,16)` was `script path`. Abort otherwise.

**Step 4: Green.** Re-run the header contract test → PASS.

**Step 5: Snapshot.**
`GOOSE_UPDATE_SNAPSHOT=1 dotnet test Goose.IntegrationTests --filter FullyQualifiedName~CsvToSqlSnapshot`
(fails on purpose), then run again without the flag → PASS. Review the
`git diff Goose.IntegrationTests/Fixtures/generated.snapshot`: the only changes should be two new
`COL:` lines (`lighting TEXT DEFAULT '' NOT NULL`, `weather …`) and no changed row values.

**Step 6: SchemaGen.**
`dotnet run --project tools/SchemaGen -- tools/DataEditor/schema.js tools/DataEditor/schema.json`
Then `dotnet test tools/Tools.Tests` → PASS (drift guard), and
`node --test "tools/DataEditor/test/*.test.js"` → PASS (Maps has no `LAYOUTS` entry, so no layout change).

**Step 7: Commit** `feat(map-ambience): weather and lighting columns on the maps schema`

---

### Task 3: Load config onto `Map` and copy to clones

**Files:**
- Modify: `Goose/Map.cs` (field block near `:56`, `CloneAs` `:116-146`)
- Modify: `Goose/MapHandler.cs:44-70` (MapHandler has no logger yet; add
  `private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();` as in `Goose/Map.cs:12`)
- Test: `Goose.Tests/MapAmbienceLoadTests.cs`, `Goose.IntegrationTests/MapWarpLoadTests.cs`

**Mutation impact:**
- Source of truth: new `Map.Lighting` (`LightingConfig`, default `LightingConfig.None`) and
  `Map.Weather` (`WeatherDistribution`, default empty), both set in `LoadMaps`.
- Readers: `MapAmbience` construction (Task 4), `CloneAs`.
- Derived state: `Map.Ambience` (Task 4) is built from these. No other derived state found.
- Propagation: `LoadMaps` row → `LightingConfig.Parse(reader.GetString("lighting"), out w)`;
  if `w` is not null, `log.Warn("Map {0} ({1}) lighting: {2}", id, name, w)`. Same for weather.
- Invariant: a clone has the same `Lighting`/`Weather` as its source.
- Proof: tests below.

Add the two properties **inside** the field block. Add them to the `CloneAs` initializer (sharing
the instances is fine; they are never mutated after load).

**Tests:**
- `CloneAs_CopiesLightingAndWeather`.
- `NewMap_DefaultsToNoneAndNoWeather` (tests and `Maps.csx` build maps without `LoadMaps`).
- `LoadMaps_ReadsWeatherAndLightingColumns` goes in `Goose.IntegrationTests/MapWarpLoadTests.cs`.
  That class already builds the `maps` table from the generated DDL, writes real `.map` files, and
  calls `world.MapHandler.LoadMaps(world)` (`:8-41`). `LoadMaps` always runs `LoadData`, which opens
  the `.map` file, so it can't live in the fast project. Add a third map row with
  `weather = '{"Rain":1,"Snow":1}'` and `lighting = '{"preset":"cave"}'`, then assert `Map.Weather`
  and `Map.Lighting`. After Task 5, extend the same test to assert `Ambience.PendingRoll` is not null
  for that map and null for map 1.

Red → green → commit `feat(map-ambience): load weather and lighting config onto maps`

---

### Task 4: `MapAmbience` (day clock, weather state, packet)

**Files:**
- Create: `Goose/MapAmbience.cs`
- Modify: `Goose/Packets.cs` (add `P.MapAmbience` next to `MapWarp`, `:459-460`)
- Modify: `Goose/Map.cs` (`Ambience` property; `CloneAs`)
- Modify: `Goose/MapHandler.cs` (create ambience after parsing config)
- Test: `Goose.Tests/MapAmbienceTests.cs`

**API:**

```csharp
public sealed class MapAmbience
{
    public const int WeatherRollSeconds = 600;
    public const double StartHour = 9.0;

    public MapAmbience(LightingConfig lighting, WeatherDistribution weather, Random random, long nowTicks);

    public LightingConfig Lighting { get; }
    public WeatherDistribution Weather { get; }
    public bool HasDayCycle { get; }
    public WeatherType CurrentWeather { get; private set; }
    internal Event? PendingRoll { get; set; }

    public long ElapsedMs(long nowTicks);
    public void SetTimeOfDay(double hours, long nowTicks);
    public void SetWeather(WeatherType weather);
    public bool TryRollWeather();
    public string BuildPacket(long nowTicks);
    public void Broadcast(Map map, GameWorld world);
    public MapAmbience CloneForNewMap(long nowTicks);
}
```

`Map`: `public MapAmbience Ambience { get; internal set; } = new(LightingConfig.None, new WeatherDistribution(), Random.Shared, Stopwatch.GetTimestamp());`
`MapHandler.LoadMaps`, after parsing config: `map.Ambience = new MapAmbience(map.Lighting, map.Weather, world.Random, world.TimeNow);`
`CloneAs`: `Ambience = this.Ambience.CloneForNewMap(Stopwatch.GetTimestamp())`. This goes in the field block.

**Day clock (critical logic):**
- `cycleStartTicks = nowTicks − SecondsToTicks(StartHour / 24 × duration)` at construction.
- `ElapsedMs(now)`: returns 0 if there's no cycle. Otherwise
  `(long)((now − cycleStartTicks) * 1000.0 / Stopwatch.Frequency) % durationMs`.
  **Use double math.** `(now − start) * 1000` in `long` overflows after about 106 days of uptime
  at `Stopwatch.Frequency` = 1e9 (Linux).
- `SetTimeOfDay(hours, now)`: `cycleStartTicks = now − SecondsToTicks(hours / 24 × duration)`.
  Throws `InvalidOperationException` if there's no cycle (the command checks `HasDayCycle` first).
- `CloneForNewMap`: same `Lighting`, `Weather`, random, and **same `cycleStartTicks`**.
  `CurrentWeather` comes from a fresh weighted roll. `PendingRoll` is null.

**Weather:**
- Initial weather is a weighted roll over all configured types, or `Clear` if there are none.
- `TryRollWeather()`: `random.NextDouble() < 0.5` is a hit. On a miss, return false. On a hit,
  the candidates are configured types ≠ `CurrentWeather`; if there are none, return false.
  Otherwise pick by weight (`NextDouble() × total`, walking candidates in enum order), set
  `CurrentWeather`, return true.
- `SetWeather` sets `CurrentWeather` only.

**Packet** (`Goose/Packets.cs`):

```csharp
// Wire format: AMB<startAlpha‰>,<endAlpha‰>,<tint>,<fill>,<elapsedMs>,<durationMs>,<weather> — alphas are thousandths, colors 0xRRGGBBAA as decimal (client: MapAmbiencePacket)
public static Func<int, int, uint, uint, long, long, WeatherType, string> MapAmbience = …;
```

`BuildPacket` rounds alphas with `(int)Math.Round(a * 1000)`. `durationMs = (long)(CycleDurationSeconds * 1000)`.
Build the string with invariant formatting (all values are integers).
`Broadcast(map, world)` = `world.SendToMap(map, BuildPacket(world.TimeNow))`.

**Tests** (fake `Random` subclass overriding `NextDouble()` from a queue; ticks from `Stopwatch.Frequency`):
- `NewAmbience_StartsAtNineAm`: outside, cycle 1440 → `ElapsedMs(now)` == 540 000.
- `ElapsedMs_WrapsAtCycleEnd`: now + 1500 s → 540 000 + 1 500 000 − 1 440 000 = 600 000.
- `ElapsedMs_DoesNotOverflowAfterLongUptime` (adversarial): now + 200 days of ticks → a value in
  [0, durationMs) equal to the expected modulo. The long-math version goes negative or wrong here.
- `SetTimeOfDay_MovesClock`: `SetTimeOfDay(18, now)` → 1 080 000.
- `None_HasNoDayCycle_ElapsedZero_SetTimeThrows`.
- `CloneForNewMap_SharesClock` (adversarial: "start a fresh 9am clock" fails): parent
  `SetTimeOfDay(20, t0)`, clone at t0 + 30 s → both `ElapsedMs(t0 + 60 s)` are equal.
- `TryRollWeather_Miss_KeepsWeather`: queue [0.7] → false, unchanged.
- `TryRollWeather_Hit_NeverPicksCurrent` (adversarial): {Clear:0.9, Rain:0.1}, current Clear, queue
  [0.1, 0.0] → Rain. A plain weighted roll at 0.0 would pick Clear.
- `TryRollWeather_SingleTypeAtThatType_NoChange`: {Snow:1}, current Snow → false.
- `TryRollWeather_SingleTypeAfterGmChange_ReturnsToConfigured`: {Snow:1}, `SetWeather(Rain)`, hit → Snow.
- `BuildPacket_Format`: outside, tint `#090909ff`, at 9am, Rain →
  `"AMB0,700,151587327,1321471,540000,1440000,1"` (`0x090909FF` = 151587327, `0x001429FF` = 1321471).
- `BuildPacket_None`: `"AMB0,0,151587327,1321471,0,0,0"`.

Red → green → commit `feat(map-ambience): per-map ambience with day clock and weather roll`

---

### Task 5: `WeatherRollEvent` and scheduling

**Files:**
- Create: `Goose/Events/WeatherRollEvent.cs`
- Modify: `Goose/MapAmbience.cs` (add `ScheduleWeatherRoll`)
- Modify: `Goose/MapHandler.cs:73-85` (call next to `ClearMapItemsEvent`)
- Modify: `Goose/Data/Illutia/Scripts/Global/Dimensions/Maps.csx:107-113` (call after the sweep event)
- Test: `Goose.Tests/WeatherRollEventTests.cs`

**API:**

```csharp
public void ScheduleWeatherRoll(Map map, GameWorld world);
```

Contract:
- It owns only `PendingRoll` and the event queue entry. It does not change weather or send packets.
- Precondition: `map.Ambience == this`, on the game thread (or at startup).
- Postcondition: any previous `PendingRoll` has been removed with `world.EventHandler.RemoveEvent`.
  If `Weather.Count == 0`, `PendingRoll` is null. Otherwise exactly one new
  `WeatherRollEvent { Data = map, Ticks = world.TimeNow + world.TimerFrequency * WeatherRollSeconds }`
  is queued and stored in `PendingRoll`.
- Safe to call repeatedly. This is what `/setweather` uses to restart the timer (Task 7).

`WeatherRollEvent.Ready(world)`:
1. `var map = (Map)Data; var amb = map.Ambience;`
2. If `!ReferenceEquals(amb.PendingRoll, this)`, return without rescheduling. This guards against
   a stale instance after `ScheduleWeatherRoll` replaced it.
3. If `amb.TryRollWeather()`, call `amb.Broadcast(map, world)`.
4. `Ticks = world.TimeNow + world.TimerFrequency * MapAmbience.WeatherRollSeconds; world.EventHandler.AddEvent(this);`

**Mutation impact:**
- Source of truth: `MapAmbience.CurrentWeather`, `MapAmbience.PendingRoll`.
- Readers: `BuildPacket` (map load, broadcasts), `/setweather`.
- Propagation: a weather change → `Broadcast` → `world.SendToMap` → every Ready player on the map.
  Loading players get the current state from `DoneLoadingMapEvent` (Task 6).
- Invariants: at most one live roll event per map. Empty config → no event.

**Tests** (`TestWorldFixture`, map via `AddBaseMap`, a `CommandPlayerOn` player added to
`map.Players` with `State = Ready`, ambience replaced with a fake-random instance):
- `Schedule_EmptyConfig_QueuesNothing`: `PendingRoll` null and `world.EventHandler.Count` unchanged.
- `Schedule_Twice_LeavesOneEvent` (adversarial): `Count` goes up by exactly 1.
- `Ready_Hit_ChangesWeatherBroadcastsAndReschedules`: set `PendingRoll` event `Ticks = world.TimeNow`,
  run `world.EventHandler.Update(world)` → the player received one `AMB…` with the new weather byte,
  and the event is back in the queue with `Ticks` about 600 s out.
- `Ready_Miss_NoBroadcastButReschedules`.
- `Ready_StaleEvent_DoesNothing`: schedule, keep the first event, schedule again, fire the first →
  no broadcast and not re-added.

Red → green. Also run `dotnet test Goose.IntegrationTests` (it compiles the shipped `Maps.csx`) → PASS.
Commit `feat(map-ambience): per-map weather roll event`

---

### Task 6: Send `AMB` on map load

**Files:**
- Modify: `Goose/Events/DoneLoadingMapEvent.cs:44-47` (after the `MWP` loop; add `AMB` to the header comment's response list)
- Test: `Goose.Tests/MapAmbienceLoadPacketTests.cs` (copy the `MapWarpLoadTests` harness, `:20-99`)

Add `world.Send(this.Player, map.Ambience.BuildPacket(world.TimeNow));` right after the `MWP` loop.

**Tests:**
- `MapLoad_SendsOneAmbAfterDsmAndWarps`: exactly one packet starting with `AMB`, and its index is
  greater than the `DSM` index and every `MWP` index.
- `MapLoad_DefaultMap_SendsNoneAmbience`: `AMB0,0,151587327,1321471,0,0,0`.

Red → green → commit `feat(map-ambience): send ambience on map load`

---

### Task 7: `/settime` and `/setweather`

**Files:**
- Modify: `Goose/AccessLevels.cs` (append `SetMapTime`, `SetMapWeather` at the end of the enum, after `ChangeEquipGraphic`)
- Create: `Goose/Commands/SetTimeCommand.cs`, `Goose/Commands/SetWeatherCommand.cs`
- Test: `Goose.Tests/AmbienceCommandTests.cs` (pattern: `Goose.Tests/GrantTitleCommandTests.cs`)

Privileges are appended (not inserted) so existing ordinals don't shift. Only GameMaster gets them,
automatically (`AccessLevels.cs:57`). No other level is edited, matching `RespawnMap`.

```csharp
[Command("/settime ", AccessPrivilege.SetMapTime, Section = "Admin", Help = "Set the time of day on your map.", Usage = "/settime <0-24|dawn|noon|dusk|night>")]
public void Execute(CommandContext ctx, string time)

[Command("/setweather ", AccessPrivilege.SetMapWeather, Section = "Admin", Help = "Set the weather on your map.", Usage = "/setweather <Clear|Rain|Fog|Storm|Snow>")]
public void Execute(CommandContext ctx, string weather)
```

Behavior (map = `ctx.Player.Map`, ambience = `map.Ambience`):
- `/settime`: parse `dawn`=6, `noon`=12, `dusk`=18, `night`=0, or a number 0–24
  (`double.TryParse`, invariant culture). Invalid input →
  `ctx.Send("Invalid time. Use 0-24 or dawn, noon, dusk, night.")`. No day cycle →
  `ctx.Send("This map has no day cycle.")`. Otherwise
  `SetTimeOfDay(hours, world.TimeNow)` → `Broadcast` → `ctx.Send($"Set time to {h}:{m:D2}.")`.
- `/setweather`: `Enum.TryParse<WeatherType>(ignoreCase: true)` and `Enum.IsDefined`. Invalid →
  `ctx.Send("Unknown weather. Use: Clear, Rain, Fog, Storm, Snow.")`. Otherwise `SetWeather` →
  `ScheduleWeatherRoll(map, world)` → `Broadcast` → `ctx.Send($"Set weather to {weather}.")`.

**Tests** (GM on map with `Access = GameMaster`, added to `map.Players`, Ready):
- `Settime_Dusk_MovesClockAndBroadcasts`: outside map → `ElapsedMs(world.TimeNow)` within ±1 000 of
  1 080 000, and the GM received an `AMB` packet.
- `Settime_NumericHours`: `/settime 13.5` → reply `Set time to 13:30.`
- `Settime_NoDayCycle_Errors_NoBroadcast`: default (none) map.
- `Settime_Invalid_Errors`: `/settime 25`, `/settime brunch`.
- `Setweather_SetsBroadcastsAndRestartsTimer` (adversarial): map with {Clear:1, Rain:1}, schedule
  once, save `PendingRoll`; `/setweather storm` → `CurrentWeather == Storm`, `PendingRoll` is a
  **different** instance, `world.EventHandler.Count` unchanged (old removed, new added).
- `Setweather_NoConfig_StillSets_NoEvent`.
- `Setweather_Unknown_Errors`: `/setweather hail`, and `/setweather 7`, which `Enum.TryParse`
  accepts as a number and `IsDefined` must reject.
- `NormalPlayer_Refused`: `Access = Normal` → `RunCommand` returns true (refusals are swallowed, `EventHandler.cs:215-223`), weather unchanged, nothing sent to the player.

Red → green → commit `feat(map-ambience): settime and setweather GM commands`

---

### Task 8: Full verification

1. `dotnet build Goose.sln` → 0 errors, no new warnings in touched files.
2. `dotnet test Goose.sln` → all pass (baseline: Tools 146 passed / 26 skipped, Goose 2134).
3. `dotnet test Goose.IntegrationTests` → all pass (baseline 515).
4. `node --test "tools/DataEditor/test/*.test.js"` → pass.
5. Server smoke (optional, no client): with a local DB rebuilt from a sheet that has the columns, set
   a map to `{"preset":"outside","cycleDuration":120}` / `{"Rain":0.5,"Snow":0.5}`, log in with the
   current client (which ignores `AMB`), and run `/settime dusk` and `/setweather storm` → replies
   received, no errors in the log.

---

## Invariant-to-test matrix

| Invariant | Proved by |
|---|---|
| Blank/invalid lighting never darkens a map | `Lighting_Blank_*`, `Lighting_MissingPresetKey_IsNone`, `Lighting_UnknownPreset_*`, `Lighting_InvalidJson_*` |
| Fixture header order matches descriptors; no `script_*` shift | `WorkbookHeaderContractTests` (Maps), snapshot diff review |
| Day clock correct over long uptime | `ElapsedMs_DoesNotOverflowAfterLongUptime` |
| Clone time matches parent | `CloneForNewMap_SharesClock` |
| Hit always changes to a different configured type | `TryRollWeather_Hit_NeverPicksCurrent` |
| Miss keeps weather another 10 min | `TryRollWeather_Miss_KeepsWeather`, `Ready_Miss_NoBroadcastButReschedules` |
| At most one live roll event per map | `Schedule_Twice_LeavesOneEvent`, `Ready_StaleEvent_DoesNothing`, `Setweather_SetsBroadcastsAndRestartsTimer` |
| Joining players get current state | `MapLoad_SendsOneAmbAfterDsmAndWarps` |
| Weather/time changes reach players on the map | `Ready_Hit_*`, `Settime_Dusk_*` |
| Commands are privilege-gated server-side | `NormalPlayer_Refused` |
| Shipped `Maps.csx` compiles against the new API | `Goose.IntegrationTests` run in Task 5 |
| Generated editor schemas current | `Checked_in_schema_js_is_up_to_date` |
