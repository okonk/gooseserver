# Ground AoE Zones — Part 1: Server Foundation Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** A `GroundZone` spell effect type that, when cast, leaves a fixed set of tiles on the map that
apply the effect's HP/MP formula to whoever stands on them every `zone_tick_ms`, until `effect_duration`
runs out, with newest-exclusive-zone-wins tile ownership.

**Architecture:** The shape loops in `SpellEffect.Cast()` are extracted into a static
`SpellEffect.ShapeTiles` (plus `RandomTiles`), pinned first by characterisation tests. A new
`GroundZones` object per `Map` owns the zone list and a per-tile owner dictionary; each zone reschedules
its own `GroundZoneTickEvent`. `SpellEffect.Cast()` gets a `GroundZone` branch that anchors on the
target's tile (which is the caster's tile for `Self` spells, since `Player.CastSpell` already rewrites
the target). No packets, no zone buffs, no tile-click casting — those are Part 2.

**Tech Stack:** C# / .NET 10, xunit 2.9.3, SQLite (System.Data.SQLite), CsvToSql descriptors →
`SchemaGen` → `tools/DataEditor/schema.js`.

**Design doc:** `docs/plans/2026-11-21-ground-aoe-design.md` (Part 1 of 3).

**Worktree:** `/home/hayden/code/illutiagooseserver/.worktrees/ground-aoe`, branch `ground-aoe`. All paths
below are relative to it.

**House rule (AGENTS.md):** do not add comments or doc strings to new code unless the *why* is
non-obvious. The plan's code follows that; keep it that way.

---

## APIs verified

Every citation below was opened and read in this worktree.

| API | Location | Notes |
|---|---|---|
| `SpellEffect.EffectTypes` (last member `Revive`, no trailing comma) | `Goose/SpellEffect.cs:70-94` | append `GroundZone` = 23 |
| `SpellEffect.TargetTypes` | `Goose/SpellEffect.cs:30-39` | Target, LineFront, Cross, Plus, Random, Area, TriangleFront |
| `SpellEffect(SpellEffect other)` copy ctor | `Goose/SpellEffect.cs:243-299` | every property must be copied; guarded by `SpellCloneTests` (`Goose.Tests/SpellCloneTests.cs:24-33`) |
| `struct Point` (only used by Random) | `Goose/SpellEffect.cs:303-313` | removed by Task 3 |
| `SpellEffect.Cast(caster, target, world, cast)` | `Goose/SpellEffect.cs:1158-1400` | `WorksInPVP` check at 1160, `BreakInvisibility` 1162, `Target` short-circuit 1164-1167, shape branches 1182-1384, packet send 1386-1393 |
| `SpellEffect.CastSpell` dispatch | `Goose/SpellEffect.cs:1070-1115` | `GroundZone` falls to `_ => false`; unchanged |
| `CastFormulaSpell(...)` | `Goose/SpellEffect.cs:572-628` | `CanCastSpell` at 581; animation packets at 603-604; taunt at 606-611 |
| `CanCastSpell(caster, target)` | `Goose/SpellEffect.cs:877-925` | map check uses `caster.Map` (878) — wrong for a zone whose caster left |
| `BuiltInItemDescription` | `Goose/SpellEffect.cs:490-566` | `Tick` case at 529-537 is the model for the new case |
| `GetFormulaDescription(formula, label, prefix)` | used at `Goose/SpellEffect.cs:532` | |
| `SpellHandler.LoadSpellEffects` | `Goose/SpellHandler.cs:28-201` | `only_hits_one_npc` read at 136, `this.effects[effect.ID] = effect;` at 145 |
| `SpellHandler.LoadSpells` | `Goose/SpellHandler.cs:230-272` | null-effect skip 264-268, `this.spells[spell.ID] = spell;` at 270 |
| `Spell` + copy ctor | `Goose/Spell.cs:11-63` | copy ends at 61 |
| `Spell.SpellTargets` | `Goose/Spell.cs:12-17` | Target=0, Self, Group |
| `Player.CastSpell` | `Goose/Player.cs:2381-2510` | Self/Group rewrite target to `this` (2415-2421); `SpellEffect.Cast` at 2446/2451/2455 |
| `Map()` ctor, `CloneAs` | `Goose/Map.cs:88-94`, `:120-151` | `CloneAs` builds via `new Map { … }`, so a ctor-initialised field is per-clone automatically |
| `Map.GetCharacterAt` / `SetCharacter` | `Goose/Map.cs:905-920` | out-of-bounds → null / no-op; valid x is `1..Width`, y `1..Height` |
| `Map.CanCast`, `Map.CanPVP` | `Goose/Map.cs:47,51` | |
| `Map.RANGE_X/RANGE_Y` | `Goose/Map.cs:17-18` | not used in Part 1 |
| `Event` (`Ticks`, `Data`) | `Goose/Event.cs:7-25` | ctor sets `Ticks = Stopwatch.GetTimestamp()` |
| `EventHandler.AddEvent(Event)`, `Count`, `Peek()` | `Goose/EventHandler.cs:301-322` | `Count`/`Peek` are `internal`; `InternalsVisibleTo` Goose.Tests and Goose.IntegrationTests (`Goose/Goose.csproj:19-24`) |
| `EventHandler.Update` re-enqueue invariant | `Goose/EventHandler.cs:326-336` | recurring events must schedule a future tick → clamp the tick interval |
| `GameWorld.TimeNow` (real `Stopwatch`), `TimerFrequency` | `Goose/GameWorld.cs:182-192` | not mockable → `GroundZones.Tick` takes `now` explicitly |
| `GameWorld.Send(Player, string)` | `Goose/GameWorld.cs:824-846` | appends `\x1`, calls `player.Send` |
| `BuffTickEvent` | `Goose/Events/BuffTickEvent.cs` | model for the tick event and the `State != Ready` skip |
| `NPC.Attacked` | `Goose/NPC.cs:1190-1300` | only does damage when `character is Player` |
| `Player.Attacked` | `Goose/Player.cs:2106-2150` | negative damage heals; not `Ready` or dead → ignored |
| `P.SpellPlayer` = `SPP…`, `P.Cast` = `CST…`, `P.SpellTile` = `SPA x,y,anim,file` | `Goose/Packets.cs:50`, `:412-415`, `:51-58` | |
| `Direction` | `Goose/Direction.cs` | Up=1, Right=2, Down=3, Left=4 (0 = unset → LineFront/TriangleFront repeat the anchor) |
| `TestWorldFixture` (`AddBaseMap`, `AddBaseSpellEffect` sets `MaximumLevelEffected = 99`, `AddNpcTemplate`, `SpawnNpc`, `PlayerOn`, `CommandPlayerOn`, `AddOnlinePlayer`, `CapturingPlayer.Sent`) | `TestSupport/TestWorldFixture.cs:73-180` | |
| `ReviveTestKit.PlayerAt` (map placement pattern) | `Goose.Tests/Revive/ReviveTestKit.cs:26-44` | `Map.Players.Add` + `Map.SetCharacter` |
| `Settings.DamageModifier` must be 1 in tests | `Goose/SpellEffect.cs:661`, `Goose.Tests/OverkillScriptTests.cs:40` | default 0 zeroes every formula |
| `new Spellbook(player, settings)`, `Spellbook.AddSpell` → slot 1 | `Goose/Spellbook.cs:24`, `:244-272` | |
| CsvToSql descriptors | `CsvToSql/CsvToSql.Core/SpellEffectsCsvToSql.cs:7-109`, enum `:166-190`; `CsvToSql/CsvToSql.Core/SpellsCsvToSql.cs:7-25` | `Col.Int`, `Col.Bool`, `Col.Id(..).Ref(..)` patterns at `:96-104` |
| `SchemaDdl.ForAll(...)` | `TestSupport/SchemaDdl.cs:21-27` | linked into Goose.IntegrationTests (`Goose.IntegrationTests.csproj:105`) |
| DB test pattern | `Goose.IntegrationTests/DimensionModifierTests.cs:132-156`, `CommunityGoalLoadTests.cs:186-201` | `new GameWorld(settings, new GameServer(settings))`, `Database.Start(path)`, delete `-wal`/`-shm` |
| Snapshot regeneration | `Goose.IntegrationTests/CsvToSqlSnapshotTests.cs:22-27` | `GOOSE_UPDATE_SNAPSHOT=1 dotnet test Goose.IntegrationTests --filter FullyQualifiedName~CsvToSqlSnapshot` |
| `SchemaGen <schema.js>` | `tools/SchemaGen/Program.cs:5-20` | precedent: commit `aed32cc` (Revive) changed only the enum + `schema.js` |

### Behaviour notes the tasks rely on

- `CastFormulaSpell` ignores `SPFormula` today. Zones inherit that; the design's "HP/MP/SP" is HP/MP in
  practice. Don't add SP here.
- Player-on-player effects only apply on non-PvP maps when `works_not_in_pvp = 1`
  (`SpellEffect.cs:885`). A Priest heal zone needs that flag set in data. This is existing behaviour.
- Kill credit from a zone whose caster logged out goes through `NPC.Attacked(caster, …)`, the same path as a
  `Tick` DoT whose caster logged out (`BuffTickEvent.cs:37`). Task 6 checks that a zone keeps ticking after
  its caster leaves the map; no new kill-credit behaviour is introduced.

---

### Task 1: Schema columns, enum member, and properties

**Files:**
- Modify: `CsvToSql/CsvToSql.Core/SpellEffectsCsvToSql.cs:104` (after `only_hits_one_npc`), `:189` (enum)
- Modify: `CsvToSql/CsvToSql.Core/SpellsCsvToSql.cs:12` (after `spell_target`)
- Modify: `Goose/SpellEffect.cs:93` (enum), properties after `OnlyHitsOneNPC` (`:141`), copy ctor `:272`
- Modify: `Goose/Spell.cs` (property + copy ctor)
- Modify: `Goose/SpellHandler.cs:136` and `:259`
- Create: `Goose.IntegrationTests/GroundZoneLoadTests.cs`
- Regenerate: `Goose.IntegrationTests/Fixtures/generated.snapshot`, `tools/DataEditor/schema.js`

**Step 1: Write the failing test**

`Goose.IntegrationTests/GroundZoneLoadTests.cs`:

```csharp
using System.Data.SQLite;

namespace Goose.IntegrationTests;

public class GroundZoneLoadTests : IDisposable
{
    private readonly string dbPath =
        Path.Combine(Path.GetTempPath(), "groundzone-load-" + Guid.NewGuid().ToString("N") + ".db");

    private readonly GameWorld world;

    public GroundZoneLoadTests()
    {
        var settings = new GooseSettings
        {
            InventorySize = 30, EquippedSize = 20, CombineBagSize = 10, SpellbookSize = 30,
        };
        world = new GameWorld(settings, new GameServer(settings));
        world.Database.Start(dbPath);
        world.Database.Execute(conn => RunSql(conn, SchemaDdl.ForAll("spell_effects", "spells")));
    }

    private void Insert(string sql) => world.Database.Execute(conn => RunSql(conn, sql));

    private void Load()
    {
        world.SpellHandler.LoadSpellEffects(world);
        world.SpellHandler.LoadSpells(world);
    }

    [Fact]
    public void Reads_the_ground_zone_columns()
    {
        Insert("INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, " +
               "effect_duration, target_type, target_size, zone_tick_ms, zone_buff_effect_id, zone_overlaps) " +
               "VALUES (1, 'Fire Patch', 6, 23, 8, 5, 1, 1500, 0, 1)");
        Insert("INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, spell_effect_id, ground_targetable) " +
               "VALUES (1, 'Fire Patch', 0, 1, 1, 1)");

        Load();

        var effect = world.SpellHandler.GetSpellEffect(1)!;
        Assert.Equal(SpellEffect.EffectTypes.GroundZone, effect.EffectType);
        Assert.Equal(1500, effect.ZoneTickMs);
        Assert.Equal(0, effect.ZoneBuffEffectID);
        Assert.True(effect.ZoneOverlaps);
        Assert.True(world.SpellHandler.GetSpell(1)!.GroundTargetable);
    }

    [Fact]
    public void Defaults_apply_when_the_columns_are_omitted()
    {
        Insert("INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type) " +
               "VALUES (2, 'Plain', 2, 0)");
        Insert("INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, spell_effect_id) " +
               "VALUES (2, 'Plain', 0, 1, 2)");

        Load();

        var effect = world.SpellHandler.GetSpellEffect(2)!;
        Assert.Equal(1000, effect.ZoneTickMs);
        Assert.False(effect.ZoneOverlaps);
        Assert.False(world.SpellHandler.GetSpell(2)!.GroundTargetable);
    }

    private static void RunSql(SQLiteConnection conn, string sql)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    public void Dispose()
    {
        world.Database.Stop();
        foreach (var suffix in new[] { "", "-wal", "-shm" })
        {
            var path = dbPath + suffix;
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
```

**Step 2: Run it to verify it fails**

Run: `dotnet test Goose.IntegrationTests --filter FullyQualifiedName~GroundZoneLoadTests`
Expected: build error — `EffectTypes` has no `GroundZone`; `ZoneTickMs`, `ZoneOverlaps`, `ZoneBuffEffectID`,
`GroundTargetable` not found.

**Step 3: Implement**

`CsvToSql/CsvToSql.Core/SpellEffectsCsvToSql.cs`, after `Col.Bool("only_hits_one_npc", def: false),`:

```csharp
            Col.Int("zone_tick_ms", SqlType.Int, def: 1000),
            Col.Id("zone_buff_effect_id", SqlType.Int, def: 0).Ref("Spell Effects"),
            Col.Bool("zone_overlaps", def: false),
```

and in its `EffectTypes` enum, after `Revive,`:

```csharp
            GroundZone,
```

`CsvToSql/CsvToSql.Core/SpellsCsvToSql.cs`, after the `spell_target` line:

```csharp
            Col.Bool("ground_targetable", def: false),
```

`Goose/SpellEffect.cs` enum — change the last member:

```csharp
            Revive,
            GroundZone
```

Properties, after `public bool OnlyHitsOneNPC { get; set; }`:

```csharp
        public int ZoneTickMs { get; set; }
        public int ZoneBuffEffectID { get; set; }
        public bool ZoneOverlaps { get; set; }
```

Copy ctor, after `this.OnlyHitsOneNPC = other.OnlyHitsOneNPC;`:

```csharp
            this.ZoneTickMs = other.ZoneTickMs;
            this.ZoneBuffEffectID = other.ZoneBuffEffectID;
            this.ZoneOverlaps = other.ZoneOverlaps;
```

`Goose/Spell.cs`: add `public bool GroundTargetable { get; set; }` after `public SpellTargets Target { get; set; }`,
and `this.GroundTargetable = other.GroundTargetable;` after `this.Target = other.Target;` in the copy ctor.

`Goose/SpellHandler.cs`, after the `only_hits_one_npc` read:

```csharp
                effect.ZoneTickMs = reader.GetInt32("zone_tick_ms");
                effect.ZoneBuffEffectID = reader.GetInt32("zone_buff_effect_id");
                effect.ZoneOverlaps = reader.GetString("zone_overlaps") != "0";
```

and after `spell.SPStaticCost = reader.GetInt32("sp_static_cost");`:

```csharp
                spell.GroundTargetable = reader.GetString("ground_targetable") != "0";
```

**Step 4: Run the tests to verify they pass**

Run: `dotnet test Goose.IntegrationTests --filter FullyQualifiedName~GroundZoneLoadTests`
Expected: PASS (2 tests).

Run: `dotnet test Goose.Tests --filter FullyQualifiedName~SpellCloneTests`
Expected: PASS (the reflection test now covers the four new properties).

**Step 5: Regenerate generated artifacts and review them**

```bash
GOOSE_UPDATE_SNAPSHOT=1 dotnet test Goose.IntegrationTests --filter FullyQualifiedName~CsvToSqlSnapshot
git diff --stat Goose.IntegrationTests/Fixtures/generated.snapshot
git diff Goose.IntegrationTests/Fixtures/generated.snapshot | grep '^[-+]' | grep -v 'zone_\|ground_targetable' | head
dotnet run --project tools/SchemaGen -- tools/DataEditor/schema.js
git diff tools/DataEditor/schema.js | head -80
dotnet test Goose.IntegrationTests --filter FullyQualifiedName~CsvToSqlSnapshot
```

Expected: the first command reports "Rewrote …" and fails on purpose. The snapshot diff only adds the
four columns: their DDL plus a default value on every existing `spell_effects`/`spells` row. The
`grep -v` line should print only the `---`/`+++` headers and changed row lines whose only difference is the
appended defaults; if anything else changed, stop and investigate. `schema.js` gains the four columns and
`"GroundZone"` / `23` in the `effect_type` enum lists. The last command passes.

**Step 6: Commit**

```bash
git add CsvToSql/CsvToSql.Core/SpellEffectsCsvToSql.cs CsvToSql/CsvToSql.Core/SpellsCsvToSql.cs \
  Goose/SpellEffect.cs Goose/Spell.cs Goose/SpellHandler.cs \
  Goose.IntegrationTests/GroundZoneLoadTests.cs Goose.IntegrationTests/Fixtures/generated.snapshot \
  tools/DataEditor/schema.js
git commit -m "feat(spells): add GroundZone effect type and zone columns"
```

---

### Task 2: Characterise today's area shapes

Pins the exact tiles (and order) that `SpellEffect.Cast()` hits for each shape before Task 3 touches it.
The case table is a JSON file so Part 3 can copy it into the client as the shared shape fixture.

**Files:**
- Create: `Goose.Tests/Spells/shape-tiles.json`
- Create: `Goose.Tests/Spells/ShapeTilesTests.cs`
- Modify: `Goose.Tests/Goose.Tests.csproj` (copy the JSON to output)

**Step 1: Write the fixture**

`Goose.Tests/Spells/shape-tiles.json` (anchor is always 10,10):

```json
[
  { "name": "Target",         "shape": "Target",        "size": 0, "facing": "Down",  "legacy": false, "tiles": [[10,10]] },
  { "name": "Area 1",         "shape": "Area",          "size": 1, "facing": "Down",  "legacy": true,
    "tiles": [[9,9],[10,9],[11,9],[9,10],[10,10],[11,10],[9,11],[10,11],[11,11]] },
  { "name": "Plus 2",         "shape": "Plus",          "size": 2, "facing": "Down",  "legacy": true,
    "tiles": [[10,8],[10,9],[8,10],[9,10],[10,10],[11,10],[12,10],[10,11],[10,12]] },
  { "name": "Cross 2",        "shape": "Cross",         "size": 2, "facing": "Down",  "legacy": true,
    "tiles": [[8,8],[12,8],[9,9],[11,9],[10,10],[9,11],[11,11],[8,12],[12,12]] },
  { "name": "Line 3 up",      "shape": "LineFront",     "size": 3, "facing": "Up",    "legacy": true, "tiles": [[10,9],[10,8],[10,7]] },
  { "name": "Line 3 right",   "shape": "LineFront",     "size": 3, "facing": "Right", "legacy": true, "tiles": [[11,10],[12,10],[13,10]] },
  { "name": "Line 3 down",    "shape": "LineFront",     "size": 3, "facing": "Down",  "legacy": true, "tiles": [[10,11],[10,12],[10,13]] },
  { "name": "Line 3 left",    "shape": "LineFront",     "size": 3, "facing": "Left",  "legacy": true, "tiles": [[9,10],[8,10],[7,10]] },
  { "name": "Triangle 2 up",  "shape": "TriangleFront", "size": 2, "facing": "Up",    "legacy": true, "tiles": [[10,9],[9,8],[10,8],[11,8]] },
  { "name": "Triangle 2 right","shape": "TriangleFront","size": 2, "facing": "Right", "legacy": true, "tiles": [[11,10],[12,9],[12,10],[12,11]] },
  { "name": "Triangle 2 down","shape": "TriangleFront", "size": 2, "facing": "Down",  "legacy": true, "tiles": [[10,11],[9,12],[10,12],[11,12]] },
  { "name": "Triangle 2 left","shape": "TriangleFront", "size": 2, "facing": "Left",  "legacy": true, "tiles": [[9,10],[8,9],[8,10],[8,11]] }
]
```

`Target` is `legacy: false` because `Cast()` short-circuits `Target` (`SpellEffect.cs:1164-1167`) without
emitting tiles; only `ShapeTiles` (Task 3) is checked against it.

Add to `Goose.Tests/Goose.Tests.csproj`:

```xml
  <ItemGroup>
    <None Include="Spells\shape-tiles.json" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>
```

**Step 2: Write the characterisation tests**

`Goose.Tests/Spells/ShapeTilesTests.cs`:

```csharp
using System.Text.Json;
using Goose.Testing;

namespace Goose.Tests.Spells;

public class ShapeTilesTests
{
    public sealed record ShapeCase(string Name, string Shape, int Size, string Facing, bool Legacy, int[][] Tiles)
    {
        public SpellEffect.TargetTypes TargetType => Enum.Parse<SpellEffect.TargetTypes>(Shape);
        public Direction Direction => Enum.Parse<Direction>(Facing);
        public List<(int X, int Y)> Expected => Tiles.Select(t => (t[0], t[1])).ToList();
    }

    public static IReadOnlyList<ShapeCase> Cases { get; } = JsonSerializer.Deserialize<List<ShapeCase>>(
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Spells", "shape-tiles.json")),
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

    private static List<(int X, int Y)> LegacyCastTiles(SpellEffect.TargetTypes shape, int size, Direction facing,
        double randomJoinChance = 0)
    {
        using var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(1, "Field", 40, 40);
        var effect = fixture.AddBaseSpellEffect(1, "Shape", e =>
        {
            e.EffectType = SpellEffect.EffectTypes.Formula;
            e.Effected = SpellEffect.SpellEffected.NPC;
            e.Display = SpellEffect.SpellDisplays.Tile;
            e.Animation = 77;
            e.AnimationFile = 3;
            e.TargetType = shape;
            e.TargetSize = size;
            e.WorksInPVP = true;
            e.RandomJoinChance = randomJoinChance;
        });
        var caster = fixture.CommandPlayerOn(map, 30, 30, "Caster");
        caster.Facing = facing;
        var target = fixture.CommandPlayerOn(map, 10, 10, "Target");

        effect.Cast(caster, target, fixture.World);

        return string.Concat(target.Sent).Split('\x1')
            .Where(p => p.StartsWith("SPA"))
            .Select(p => p.Substring(3).Split(','))
            .Select(f => (int.Parse(f[0]), int.Parse(f[1])))
            .ToList();
    }

    [Fact]
    public void Legacy_cast_hits_the_recorded_tiles_in_order()
    {
        foreach (var c in Cases.Where(c => c.Legacy))
            Assert.True(c.Expected.SequenceEqual(LegacyCastTiles(c.TargetType, c.Size, c.Direction)),
                $"{c.Name}: got {string.Join(" ", LegacyCastTiles(c.TargetType, c.Size, c.Direction))}");
    }

    [Fact]
    public void Legacy_random_with_no_join_chance_is_only_the_anchor()
    {
        Assert.Equal([(10, 10)], LegacyCastTiles(SpellEffect.TargetTypes.Random, 2, Direction.Down, 0));
    }

    [Fact]
    public void Legacy_random_with_full_join_chance_fills_the_square()
    {
        var tiles = LegacyCastTiles(SpellEffect.TargetTypes.Random, 2, Direction.Down, 100);

        Assert.Equal(25, tiles.Count);
        Assert.Equal((10, 10), tiles[0]);
        Assert.All(tiles, t => Assert.InRange(t.X, 8, 12));
        Assert.All(tiles, t => Assert.InRange(t.Y, 8, 12));
        Assert.Equal(25, tiles.Distinct().Count());
    }
}
```

**Step 3: Run them against the unmodified code**

Run: `dotnet test Goose.Tests --filter FullyQualifiedName~ShapeTilesTests`
Expected: PASS (3 tests). These are characterisation tests: they must pass *before* any refactor. If a case
fails, the JSON is wrong — fix the JSON to match the code (the code is the authority here), re-derive
from `SpellEffect.cs:1182-1384`, and note the correction in the commit message.

**Step 4: Commit**

```bash
git add Goose.Tests/Spells/shape-tiles.json Goose.Tests/Spells/ShapeTilesTests.cs Goose.Tests/Goose.Tests.csproj
git commit -m "test(spells): characterise area spell shape tiles"
```

---

### Task 3: Extract `ShapeTiles` / `RandomTiles` and use them in `Cast()`

**Files:**
- Modify: `Goose/SpellEffect.cs` — replace `struct Point` (`:302-313`) and the shape branches (`:1182-1384`)
- Test: `Goose.Tests/Spells/ShapeTilesTests.cs`

**Step 1: Write the failing test**

Append to `ShapeTilesTests`:

```csharp
    [Fact]
    public void ShapeTiles_matches_every_recorded_case()
    {
        foreach (var c in Cases)
            Assert.True(c.Expected.SequenceEqual(SpellEffect.ShapeTiles(c.TargetType, c.Size, 10, 10, c.Direction)),
                $"{c.Name}: got {string.Join(" ", SpellEffect.ShapeTiles(c.TargetType, c.Size, 10, 10, c.Direction))}");
    }

    [Fact]
    public void ShapeTiles_returns_nothing_for_random()
    {
        Assert.Empty(SpellEffect.ShapeTiles(SpellEffect.TargetTypes.Random, 2, 10, 10, Direction.Down));
    }
```

**Step 2: Run to verify it fails**

Run: `dotnet test Goose.Tests --filter FullyQualifiedName~ShapeTilesTests`
Expected: build error — `SpellEffect.ShapeTiles` does not exist.

**Step 3: Implement**

In `Goose/SpellEffect.cs`, delete `struct Point` and its `// Used in random spell` comment, and add in its place:

```csharp
        public static List<(int X, int Y)> ShapeTiles(TargetTypes shape, int size, int ox, int oy, Direction facing)
        {
            var tiles = new List<(int X, int Y)>();

            switch (shape)
            {
                case TargetTypes.Target:
                    tiles.Add((ox, oy));
                    break;

                case TargetTypes.LineFront:
                {
                    int x = ox;
                    int y = oy;
                    for (int i = 1; i <= size; i++)
                    {
                        (x, y) = facing switch
                        {
                            Direction.Up => (x, y - 1),
                            Direction.Right => (x + 1, y),
                            Direction.Down => (x, y + 1),
                            Direction.Left => (x - 1, y),
                            _ => (x, y),
                        };
                        tiles.Add((x, y));
                    }
                    break;
                }

                case TargetTypes.Area:
                    for (int y = oy - size; y <= oy + size; y++)
                        for (int x = ox - size; x <= ox + size; x++)
                            tiles.Add((x, y));
                    break;

                case TargetTypes.Cross:
                    for (int y = oy - size; y <= oy + size; y++)
                        for (int x = ox - size; x <= ox + size; x++)
                            if (x == ox - (y - oy) || x == ox + (y - oy))
                                tiles.Add((x, y));
                    break;

                case TargetTypes.Plus:
                    for (int y = oy - size; y <= oy + size; y++)
                        for (int x = ox - size; x <= ox + size; x++)
                            if (x == ox || y == oy)
                                tiles.Add((x, y));
                    break;

                case TargetTypes.TriangleFront:
                {
                    int x = ox;
                    int y = oy;
                    for (int i = 1; i <= size; i++)
                    {
                        (x, y) = facing switch
                        {
                            Direction.Up => (ox - i, y - 1),
                            Direction.Right => (x + 1, oy - i),
                            Direction.Down => (ox - i, y + 1),
                            Direction.Left => (x - 1, oy - i),
                            _ => (x, y),
                        };

                        for (int j = 0; j < i * 2 - 1; j++)
                        {
                            (x, y) = facing switch
                            {
                                Direction.Up => (x + 1, y),
                                Direction.Right => (x, y + 1),
                                Direction.Down => (x + 1, y),
                                Direction.Left => (x, y + 1),
                                _ => (x, y),
                            };
                            tiles.Add((x, y));
                        }
                    }
                    break;
                }
            }

            return tiles;
        }

        public List<(int X, int Y)> RandomTiles(int ox, int oy, Random random)
        {
            var points = new List<(int X, int Y)> { (ox, oy) };
            var done = new List<(int X, int Y)>();

            while (points.Count > 0)
            {
                var point = points[0];
                points.RemoveAt(0);

                if (point.X >= ox - this.TargetSize &&
                    point.X <= ox + this.TargetSize &&
                    point.Y >= oy - this.TargetSize &&
                    point.Y <= oy + this.TargetSize &&
                    !done.Contains(point))
                {
                    done.Add(point);

                    if (random.Next(1, 10001) <= Utils.ExactProduct(this.RandomJoinChance, 100))
                        points.Add((point.X - 1, point.Y));
                    if (random.Next(1, 10001) <= Utils.ExactProduct(this.RandomJoinChance, 100))
                        points.Add((point.X + 1, point.Y));
                    if (random.Next(1, 10001) <= Utils.ExactProduct(this.RandomJoinChance, 100))
                        points.Add((point.X, point.Y - 1));
                    if (random.Next(1, 10001) <= Utils.ExactProduct(this.RandomJoinChance, 100))
                        points.Add((point.X, point.Y + 1));
                }
            }

            return done;
        }
```

`RandomTiles` keeps the four `random.Next` calls in the same order as the original (`:1304-1319`), so the
RNG sequence a seeded world sees does not change.

In `Cast()`, replace everything from `if (this.TargetType == TargetTypes.LineFront)` (`:1182`) through the
closing brace of the `TriangleFront` branch (`:1384`) with:

```csharp
                var tiles = this.TargetType == TargetTypes.Random
                    ? this.RandomTiles(ox, oy, world.Random)
                    : ShapeTiles(this.TargetType, this.TargetSize, ox, oy, caster.Facing);

                foreach (var (x, y) in tiles)
                {
                    if ((hit = map.GetCharacterAt(x, y)) is not null)
                    {
                        this.CastSpell(caster, hit, world, cast);

                        if (this.OnlyHitsOneNPC && hit is NPC) hitone = true;
                    }
                    if (this.Display == SpellDisplays.Tile && this.Animation != 0)
                    {
                        packet += "\x1" + P.SpellTile(x, y, this.Animation, this.AnimationFile);
                    }

                    if (hitone) break;
                }
```

Every original branch did cast → tile packet → `break` on `hitone`, in that order, so this loop is
behaviour-identical. Leave the `packet.TrimStart('\x1');` line and the send loop below it untouched.

**Step 4: Run the tests**

Run: `dotnet test Goose.Tests --filter FullyQualifiedName~ShapeTilesTests`
Expected: PASS (5 tests — the three legacy tests still pass against the refactored `Cast()`).

Run: `dotnet test Goose.Tests`
Expected: PASS, same count as the baseline (2939) plus the new tests.

**Step 5: Commit**

```bash
git add Goose/SpellEffect.cs Goose.Tests/Spells/ShapeTilesTests.cs
git commit -m "refactor(spells): extract area shape tiles from SpellEffect.Cast"
```

---

### Task 4: `CanCastSpell` on an explicit map, and a non-animating formula apply

A zone must check the *zone's* map for `CanCast` (not wherever its caster has gone), and its ticks must not
replay the cast animation or the effect animation on every target every second.

**Files:**
- Modify: `Goose/SpellEffect.cs:572-628` (`CastFormulaSpell`), `:877-879` (`CanCastSpell`)
- Create: `Goose.Tests/Spells/FormulaApplyTests.cs`

**Step 1: Write the failing tests**

```csharp
using Goose.Testing;

namespace Goose.Tests.Spells;

public class FormulaApplyTests
{
    private static (TestWorldFixture fixture, Map map, TestWorldFixture.CapturingPlayer caster, NPC npc, SpellEffect effect) Setup()
    {
        var fixture = new TestWorldFixture(s => s.DamageModifier = 1);
        var map = fixture.AddBaseMap(1, "Field", 40, 40);
        map.CanCast = true;
        var caster = fixture.CommandPlayerOn(map, 10, 12, "Caster");
        map.Players.Add(caster);
        map.SetCharacter(caster, 10, 12);
        var npc = fixture.SpawnNpc(map, 10, 10, fixture.AddNpcTemplate(500, "Dummy", t => t.AggroRange = 0));
        var effect = fixture.AddBaseSpellEffect(1, "Burn", e =>
        {
            e.EffectType = SpellEffect.EffectTypes.Formula;
            e.Effected = SpellEffect.SpellEffected.NPC;
            e.HPFormula = "-10";
            e.MPFormula = "0";
            e.Animation = 5;
            e.DoCastAnimation = true;
        });
        return (fixture, map, caster, npc, effect);
    }

    private static string Received(TestWorldFixture.CapturingPlayer p) => string.Concat(p.Sent);

    [Fact]
    public void CastFormulaSpell_still_sends_the_effect_and_cast_animations()
    {
        var (fixture, _, caster, npc, effect) = Setup();
        using var _f = fixture;

        Assert.True(effect.CastFormulaSpell(caster, npc, fixture.World));

        Assert.Contains("SPP" + npc.LoginID + ",5,", Received(caster));
        Assert.Contains("CST" + caster.LoginID, Received(caster));
    }

    [Fact]
    public void ApplyFormula_without_animation_damages_but_sends_no_animations()
    {
        var (fixture, _, caster, npc, effect) = Setup();
        using var _f = fixture;
        long before = npc.CurrentHP;

        effect.ApplyFormula(caster, npc, fixture.World, animate: false, out long hp, out _);

        Assert.Equal(-10, hp);
        Assert.Equal(before - 10, npc.CurrentHP);
        Assert.DoesNotContain("SPP", Received(caster));
        Assert.DoesNotContain("CST", Received(caster));
    }

    [Fact]
    public void CanCastSpell_checks_the_given_map_instead_of_the_casters()
    {
        var (fixture, map, caster, npc, effect) = Setup();
        using var _f = fixture;
        var town = fixture.AddBaseMap(2, "Town", 20, 20);
        town.CanCast = false;
        caster.Map = town;

        Assert.False(effect.CanCastSpell(caster, npc));
        Assert.True(effect.CanCastSpell(caster, npc, map));
    }
}
```

**Step 2: Run to verify they fail**

Run: `dotnet test Goose.Tests --filter FullyQualifiedName~FormulaApplyTests`
Expected: build error — no `ApplyFormula`, no 3-argument `CanCastSpell`.

**Step 3: Implement**

`CanCastSpell` — new signature and first two lines; the rest is unchanged:

```csharp
        public bool CanCastSpell(ICharacter caster, ICharacter target, Map? castMap = null)
        {
            if (target is null) return false;
            if (!MapAllowsCasting(caster, castMap ?? caster.Map)) return false;
```

Add next to it:

```csharp
        private static bool MapAllowsCasting(ICharacter caster, Map? map)
            => map is null || map.CanCast ||
               (caster is Player player && player.HasPrivilege(AccessPrivilege.CastSpellsWhileBlocked));
```

This is the original condition from `:878` negated:
`(map not null && !map.CanCast) && (caster is Player && !HasPrivilege)` → blocked.

Split `CastFormulaSpell`: the 5-argument overload keeps its `CanCastSpell` guard and delegates; the body
moves into `ApplyFormula` with the two animation lines gated on `animate`:

```csharp
        public bool CastFormulaSpell(ICharacter caster, ICharacter target, GameWorld world, out long hpChange, out long hpCritMultiplier)
        {
            hpChange = 0;
            hpCritMultiplier = 1;
            if (!this.CanCastSpell(caster, target)) return false;

            this.ApplyFormula(caster, target, world, animate: true, out hpChange, out hpCritMultiplier);
            return true;
        }

        internal void ApplyFormula(ICharacter caster, ICharacter target, GameWorld world, bool animate,
            out long hpChange, out long hpCritMultiplier)
        {
            var range = target.Map.GetPlayersInRange(target);
            var packets = new List<string>();

            var (hpresult, mpresult) = this.CalculateFormulaResults(
                this.HPFormula, this.MPFormula, caster, target, world, out hpCritMultiplier);
            hpChange = hpresult;
            target.CurrentMP += mpresult;
            if (hpresult != 0)
            {
                target.Attacked(caster, -hpresult, world);
            }
            else
            {
                packets.Add(P.VitalsPercentage(target));

                if (target is Player)
                {
                    world.Send((Player)target, P.StatusInfo((Player)target));
                }
            }

            if (animate && this.Animation != 0) packets.Add(P.SpellPlayer(target.LoginID, this.Animation, this.AnimationFile));
            if (animate && this.DoCastAnimation) packets.Add(P.Cast(caster));

            if (target is NPC && this.TauntAggro > 0)
            {
                ((NPC)target).AddAggro((Player)caster, this.TauntAggro, true, world);

                packets.Add(P.BattleTextYellow(target, "Taunted"));
            }

            if (packets.Count == 0)
                return;

            var packet = string.Join("\x1", packets);

            if (target is Player) world.Send((Player)target, packet);
            foreach (var player in range)
            {
                world.Send(player, packet);
            }
        }
```

**Step 4: Run the tests**

Run: `dotnet test Goose.Tests --filter FullyQualifiedName~FormulaApplyTests`
Expected: PASS (3 tests).

Run: `dotnet test Goose.Tests`
Expected: PASS (no regressions — every existing `CanCastSpell` caller uses the 2-argument form).

**Step 5: Commit**

```bash
git add Goose/SpellEffect.cs Goose.Tests/Spells/FormulaApplyTests.cs
git commit -m "refactor(spells): split formula apply from cast checks"
```

---

### Task 5: Load validation for ground zones

**Files:**
- Modify: `Goose/SpellHandler.cs` — static checks; wiring before `:145` and before `:270`
- Create: `Goose.Tests/Spells/GroundZoneValidationTests.cs`
- Modify: `Goose.IntegrationTests/GroundZoneLoadTests.cs`

**Step 1: Write the failing tests**

`Goose.Tests/Spells/GroundZoneValidationTests.cs`:

```csharp
namespace Goose.Tests.Spells;

public class GroundZoneValidationTests
{
    private static SpellEffect Zone(Action<SpellEffect>? configure = null)
    {
        var effect = new SpellEffect
        {
            ID = 1, Name = "Zone",
            EffectType = SpellEffect.EffectTypes.GroundZone,
            TargetType = SpellEffect.TargetTypes.Area,
            TargetSize = 1,
            Duration = 8,
        };
        configure?.Invoke(effect);
        return effect;
    }

    [Fact]
    public void A_well_formed_zone_has_no_problem()
    {
        Assert.Null(SpellHandler.GroundZoneProblem(Zone()));
    }

    [Fact]
    public void Non_zone_effects_are_never_flagged()
    {
        Assert.Null(SpellHandler.GroundZoneProblem(new SpellEffect { TargetType = SpellEffect.TargetTypes.Random }));
    }

    [Fact]
    public void Random_shape_is_rejected()
    {
        Assert.NotNull(SpellHandler.GroundZoneProblem(Zone(e => e.TargetType = SpellEffect.TargetTypes.Random)));
    }

    [Fact]
    public void Zero_duration_is_rejected()
    {
        Assert.NotNull(SpellHandler.GroundZoneProblem(Zone(e => e.Duration = 0)));
    }

    [Fact]
    public void Group_target_zone_spell_is_rejected()
    {
        var spell = new Spell { ID = 1, Name = "Zone", Target = Spell.SpellTargets.Group, SpellEffect = Zone() };

        Assert.NotNull(SpellHandler.SpellProblem(spell));
    }

    [Theory]
    [InlineData(Spell.SpellTargets.Self)]
    [InlineData(Spell.SpellTargets.Target)]
    public void Self_and_target_zone_spells_are_fine(Spell.SpellTargets target)
    {
        var spell = new Spell { ID = 1, Name = "Zone", Target = target, SpellEffect = Zone() };

        Assert.Null(SpellHandler.SpellProblem(spell));
    }
}
```

Append to `Goose.IntegrationTests/GroundZoneLoadTests.cs`:

```csharp
    [Fact]
    public void Skips_invalid_zones_and_the_spells_that_use_them()
    {
        Insert("INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, " +
               "effect_duration, target_type, target_size) VALUES (3, 'Random Zone', 6, 23, 8, 4, 1)");
        Insert("INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, " +
               "effect_duration, target_type, target_size) VALUES (4, 'Good Zone', 6, 23, 8, 5, 1)");
        Insert("INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, spell_effect_id) " +
               "VALUES (3, 'Random Zone', 0, 1, 3)");
        Insert("INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, spell_effect_id) " +
               "VALUES (4, 'Group Zone', 2, 1, 4)");

        Load();

        Assert.Null(world.SpellHandler.GetSpellEffect(3));
        Assert.Null(world.SpellHandler.GetSpell(3));
        Assert.NotNull(world.SpellHandler.GetSpellEffect(4));
        Assert.Null(world.SpellHandler.GetSpell(4));
    }
```

**Step 2: Run to verify they fail**

Run: `dotnet test Goose.Tests --filter FullyQualifiedName~GroundZoneValidationTests`
Expected: build error — `SpellHandler.GroundZoneProblem` / `SpellProblem` do not exist.

**Step 3: Implement**

In `Goose/SpellHandler.cs`, add:

```csharp
        internal static string? GroundZoneProblem(SpellEffect effect)
        {
            if (effect.EffectType != SpellEffect.EffectTypes.GroundZone) return null;
            if (effect.TargetType == SpellEffect.TargetTypes.Random) return "ground zones can't use the Random shape";
            if (effect.Duration <= 0) return "ground zones need an effect_duration above 0";
            return null;
        }

        internal static string? SpellProblem(Spell spell)
        {
            if (spell.SpellEffect.EffectType == SpellEffect.EffectTypes.GroundZone &&
                spell.Target == Spell.SpellTargets.Group)
                return "ground zone spells can't target Group";
            return null;
        }
```

In `LoadSpellEffects`, replace `this.effects[effect.ID] = effect;` (`:145`) with:

```csharp
                if (GroundZoneProblem(effect) is string problem)
                {
                    log.Warn("spell effect {0} ({1}): {2}", effect.Name, effect.ID, problem);
                    this.effects.Remove(effect.ID);
                    continue;
                }

                this.effects[effect.ID] = effect;
```

In `LoadSpells`, replace `this.spells[spell.ID] = spell;` (`:270`) with:

```csharp
                if (SpellProblem(spell) is string problem)
                {
                    log.Warn("spell {0} ({1}): {2}", spell.Name, spell.ID, problem);
                    this.spells.Remove(spell.ID);
                    continue;
                }

                this.spells[spell.ID] = spell;
```

`Remove` (not just skipping) matters on `/reloadsql`: the entry from the previous load would otherwise
survive with stale data.

**Step 4: Run the tests**

Run: `dotnet test Goose.Tests --filter FullyQualifiedName~GroundZoneValidationTests`
Expected: PASS (7 tests).

Run: `dotnet test Goose.IntegrationTests --filter FullyQualifiedName~GroundZoneLoadTests`
Expected: PASS (3 tests).

**Step 5: Commit**

```bash
git add Goose/SpellHandler.cs Goose.Tests/Spells/GroundZoneValidationTests.cs Goose.IntegrationTests/GroundZoneLoadTests.cs
git commit -m "feat(spells): reject random-shaped, durationless and group ground zones at load"
```

---

### Task 6: `GroundZone`, per-map `GroundZones`, ownership and ticks

**Files:**
- Create: `Goose/GroundZone.cs`
- Create: `Goose/GroundZones.cs`
- Create: `Goose/Events/GroundZoneTickEvent.cs`
- Modify: `Goose/Map.cs:88-94` (ctor) and add a property
- Modify: `Goose/SpellEffect.cs` (add `HasFormula`)
- Create: `Goose.Tests/GroundZones/GroundZoneTestKit.cs`
- Create: `Goose.Tests/GroundZones/GroundZonesTests.cs`
- Modify: `Goose.Tests/MapCloneTests.cs`

**Step 1: Write the test kit**

`Goose.Tests/GroundZones/GroundZoneTestKit.cs`:

```csharp
using Goose.Testing;

namespace Goose.Tests.GroundZones;

internal sealed class GroundZoneTestKit : IDisposable
{
    public TestWorldFixture Fixture { get; }
    public GameWorld World => Fixture.World;
    public Map Map { get; }
    public NPCTemplate Template { get; }

    public GroundZoneTestKit()
    {
        Fixture = new TestWorldFixture(s => s.DamageModifier = 1);
        Map = Fixture.AddBaseMap(1, "Field", 40, 40);
        Map.CanCast = true;
        Template = Fixture.AddNpcTemplate(500, "Dummy", t => t.AggroRange = 0);
    }

    public TestWorldFixture.CapturingPlayer PlayerAt(int x, int y, string name = "Caster")
    {
        var p = Fixture.CommandPlayerOn(Map, x, y, name);
        Fixture.AddOnlinePlayer(p);
        p.Level = 1;
        p.MaxStats = new AttributeSet { HP = 100, MP = 100 };
        p.CurrentHP = 100;
        p.CurrentMP = 100;
        p.Facing = Direction.Down;
        p.Spellbook = new Spellbook(p, Fixture.Settings);
        Map.Players.Add(p);
        Map.SetCharacter(p, x, y);
        return p;
    }

    public NPC NpcAt(int x, int y)
    {
        var npc = Fixture.SpawnNpc(Map, x, y, Template);
        Assert.Same(npc, Map.GetCharacterAt(x, y));
        return npc;
    }

    public SpellEffect Zone(int id, Action<SpellEffect>? configure = null) =>
        Fixture.AddBaseSpellEffect(id, "Zone " + id, e =>
        {
            e.EffectType = SpellEffect.EffectTypes.GroundZone;
            e.TargetType = SpellEffect.TargetTypes.Area;
            e.TargetSize = 1;
            e.Duration = 10;
            e.ZoneTickMs = 1000;
            e.Effected = SpellEffect.SpellEffected.NPCPlayer;
            e.HPFormula = "-10";
            e.MPFormula = "0";
            e.SPFormula = "0";
            e.WorksInPVP = true;
            configure?.Invoke(e);
        });

    public long At(GroundZone zone, double seconds) => zone.CreatedAt + (long)(seconds * World.TimerFrequency);

    public void Dispose() => Fixture.Dispose();
}
```

`NpcAt` asserts the spawn really occupies the tile, so a harness surprise shows up as a clear failure
instead of a confusing "no damage" one.

**Step 2: Write the failing tests**

`Goose.Tests/GroundZones/GroundZonesTests.cs`:

```csharp
using Goose.Events;

namespace Goose.Tests.GroundZones;

public class GroundZonesTests
{
    [Fact]
    public void Create_hits_everyone_on_its_tiles_straight_away()
    {
        using var kit = new GroundZoneTestKit();
        var caster = kit.PlayerAt(30, 30);
        var centre = kit.NpcAt(10, 10);
        var corner = kit.NpcAt(11, 11);
        var outside = kit.NpcAt(13, 10);
        long full = outside.CurrentHP;

        kit.Map.GroundZones.Create(kit.Zone(1), caster, 10, 10, kit.World);

        Assert.Equal(full - 10, centre.CurrentHP);
        Assert.Equal(full - 10, corner.CurrentHP);
        Assert.Equal(full, outside.CurrentHP);
    }

    [Fact]
    public void Ticks_until_expiry_then_removes_itself()
    {
        using var kit = new GroundZoneTestKit();
        var caster = kit.PlayerAt(30, 30);
        var npc = kit.NpcAt(10, 10);
        long full = npc.CurrentHP;
        var zone = kit.Map.GroundZones.Create(kit.Zone(1, e => e.Duration = 3), caster, 10, 10, kit.World);

        kit.Map.GroundZones.Tick(zone, kit.At(zone, 1), kit.World);
        kit.Map.GroundZones.Tick(zone, kit.At(zone, 2), kit.World);
        kit.Map.GroundZones.Tick(zone, kit.At(zone, 3), kit.World);
        kit.Map.GroundZones.Tick(zone, kit.At(zone, 4), kit.World);

        Assert.Equal(full - 30, npc.CurrentHP);
        Assert.Empty(kit.Map.GroundZones.All);
        Assert.Null(kit.Map.GroundZones.OwnerAt(10, 10));
    }

    [Fact]
    public void Create_schedules_the_next_tick_one_interval_later()
    {
        using var kit = new GroundZoneTestKit();
        var caster = kit.Fixture.PlayerOn(kit.Map, 30, 30);
        caster.Facing = Direction.Down;
        int before = kit.World.EventHandler.Count;

        var zone = kit.Map.GroundZones.Create(kit.Zone(1, e => e.ZoneTickMs = 1500), caster, 10, 10, kit.World);

        Assert.Equal(before + 1, kit.World.EventHandler.Count);
        var ev = Assert.IsType<GroundZoneTickEvent>(kit.World.EventHandler.Peek());
        Assert.Same(zone, ev.Data);
        Assert.Equal(kit.At(zone, 1.5), ev.Ticks);
    }

    [Fact]
    public void The_last_tick_is_scheduled_at_expiry_not_past_it()
    {
        using var kit = new GroundZoneTestKit();
        var caster = kit.Fixture.PlayerOn(kit.Map, 30, 30);
        caster.Facing = Direction.Down;

        var zone = kit.Map.GroundZones.Create(kit.Zone(1, e => { e.Duration = 1; e.ZoneTickMs = 5000; }),
            caster, 10, 10, kit.World);

        Assert.Equal(zone.ExpiresAt, kit.World.EventHandler.Peek().Ticks);
    }

    [Fact]
    public void A_tiny_tick_interval_is_clamped()
    {
        using var kit = new GroundZoneTestKit();
        var caster = kit.Fixture.PlayerOn(kit.Map, 30, 30);
        caster.Facing = Direction.Down;

        var zone = kit.Map.GroundZones.Create(kit.Zone(1, e => e.ZoneTickMs = 0), caster, 10, 10, kit.World);

        Assert.Equal(kit.At(zone, GroundZones.MinTickMs / 1000.0), kit.World.EventHandler.Peek().Ticks);
    }

    [Fact]
    public void Tiles_are_clipped_to_the_map()
    {
        using var kit = new GroundZoneTestKit();
        var caster = kit.PlayerAt(30, 30);

        var zone = kit.Map.GroundZones.Create(kit.Zone(1), caster, 1, 1, kit.World);

        Assert.Equal(new HashSet<(int, int)> { (1, 1), (2, 1), (1, 2), (2, 2) }, zone.Tiles);
    }

    [Fact]
    public void The_newest_exclusive_zone_owns_shared_tiles()
    {
        using var kit = new GroundZoneTestKit();
        var caster = kit.PlayerAt(30, 30);
        var onlyOld = kit.NpcAt(9, 10);
        var shared = kit.NpcAt(10, 10);
        long full = shared.CurrentHP;
        var older = kit.Map.GroundZones.Create(kit.Zone(1), caster, 10, 10, kit.World);
        var newer = kit.Map.GroundZones.Create(kit.Zone(2, e => e.HPFormula = "-1"), caster, 11, 10, kit.World);

        kit.Map.GroundZones.Tick(older, kit.At(older, 1), kit.World);

        Assert.Same(older, kit.Map.GroundZones.OwnerAt(9, 10));
        Assert.Same(newer, kit.Map.GroundZones.OwnerAt(10, 10));
        Assert.Equal(full - 20, onlyOld.CurrentHP);
        Assert.Equal(full - 10 - 1, shared.CurrentHP);
    }

    [Fact]
    public void Expiry_hands_tiles_back_to_the_older_zone()
    {
        using var kit = new GroundZoneTestKit();
        var caster = kit.PlayerAt(30, 30);
        var older = kit.Map.GroundZones.Create(kit.Zone(1), caster, 10, 10, kit.World);
        var newer = kit.Map.GroundZones.Create(kit.Zone(2, e => e.Duration = 2), caster, 11, 10, kit.World);

        kit.Map.GroundZones.Tick(newer, newer.ExpiresAt, kit.World);

        Assert.Same(older, kit.Map.GroundZones.OwnerAt(10, 10));
        Assert.Null(kit.Map.GroundZones.OwnerAt(12, 10));
        Assert.Single(kit.Map.GroundZones.All);
    }

    [Fact]
    public void Overlapping_zones_neither_take_nor_lose_tiles()
    {
        using var kit = new GroundZoneTestKit();
        var caster = kit.PlayerAt(30, 30);
        var npc = kit.NpcAt(10, 10);
        long full = npc.CurrentHP;
        var exclusive = kit.Map.GroundZones.Create(kit.Zone(1), caster, 10, 10, kit.World);
        var overlap = kit.Map.GroundZones.Create(kit.Zone(2, e => { e.ZoneOverlaps = true; e.HPFormula = "-1"; }),
            caster, 10, 10, kit.World);
        var newerExclusive = kit.Map.GroundZones.Create(kit.Zone(3, e => e.HPFormula = "-100"), caster, 10, 10, kit.World);

        kit.Map.GroundZones.Tick(overlap, kit.At(overlap, 1), kit.World);

        Assert.Same(newerExclusive, kit.Map.GroundZones.OwnerAt(10, 10));
        Assert.True(kit.Map.GroundZones.IsActiveAt(overlap, 10, 10));
        Assert.False(kit.Map.GroundZones.IsActiveAt(exclusive, 10, 10));
        Assert.Equal(full - 10 - 1 - 100 - 1, npc.CurrentHP);
    }

    [Fact]
    public void Skips_characters_the_effect_cannot_affect()
    {
        using var kit = new GroundZoneTestKit();
        var caster = kit.PlayerAt(10, 10);
        var bystander = kit.PlayerAt(11, 10, "Bystander");

        kit.Map.GroundZones.Create(kit.Zone(1, e => e.Effected = SpellEffect.SpellEffected.NPC), caster, 10, 10, kit.World);

        Assert.Equal(100, caster.CurrentHP);
        Assert.Equal(100, bystander.CurrentHP);
    }

    [Fact]
    public void A_zone_with_no_formula_touches_nobody()
    {
        using var kit = new GroundZoneTestKit();
        var caster = kit.PlayerAt(30, 30);
        var npc = kit.NpcAt(10, 10);
        long full = npc.CurrentHP;

        kit.Map.GroundZones.Create(kit.Zone(1, e => e.HPFormula = "0"), caster, 10, 10, kit.World);

        Assert.Equal(full, npc.CurrentHP);
    }

    [Fact]
    public void Keeps_working_after_the_caster_leaves_for_a_no_cast_map()
    {
        using var kit = new GroundZoneTestKit();
        var caster = kit.PlayerAt(30, 30);
        var npc = kit.NpcAt(10, 10);
        long full = npc.CurrentHP;
        var zone = kit.Map.GroundZones.Create(kit.Zone(1), caster, 10, 10, kit.World);
        var town = kit.Fixture.AddBaseMap(2, "Town", 20, 20);
        town.CanCast = false;
        kit.Map.SetCharacter(null, 30, 30);
        kit.Map.Players.Remove(caster);
        caster.Map = town;

        kit.Map.GroundZones.Tick(zone, kit.At(zone, 1), kit.World);

        Assert.Equal(full - 20, npc.CurrentHP);
    }

    [Fact]
    public void A_removed_zone_ignores_stale_tick_events()
    {
        using var kit = new GroundZoneTestKit();
        var caster = kit.PlayerAt(30, 30);
        var npc = kit.NpcAt(10, 10);
        var zone = kit.Map.GroundZones.Create(kit.Zone(1, e => e.Duration = 1), caster, 10, 10, kit.World);
        kit.Map.GroundZones.Tick(zone, zone.ExpiresAt, kit.World);
        long after = npc.CurrentHP;

        kit.Map.GroundZones.Tick(zone, kit.At(zone, 0.5), kit.World);

        Assert.Equal(after, npc.CurrentHP);
    }
}
```

In `Goose.Tests/MapCloneTests.cs`, add:

```csharp
    [Fact]
    public void Clone_gets_its_own_ground_zones()
    {
        var basic = MakeBase();

        var clone = basic.CloneAs(100001, "Town (1)");

        Assert.NotSame(basic.GroundZones, clone.GroundZones);
    }
```

**Step 3: Run to verify they fail**

Run: `dotnet test Goose.Tests --filter "FullyQualifiedName~GroundZonesTests|FullyQualifiedName~MapCloneTests"`
Expected: build error — `GroundZone`, `GroundZones`, `GroundZoneTickEvent`, `Map.GroundZones` do not exist.

**Step 4: Implement**

`Goose/GroundZone.cs`:

```csharp
namespace Goose
{
    public class GroundZone
    {
        public int Id { get; init; }
        public SpellEffect Effect { get; init; } = null!;
        public ICharacter Caster { get; init; } = null!;
        public Map Map { get; init; } = null!;
        public HashSet<(int X, int Y)> Tiles { get; init; } = [];
        public long CreatedAt { get; init; }
        public long ExpiresAt { get; init; }

        public bool Overlaps => this.Effect.ZoneOverlaps;
    }
}
```

`Goose/GroundZones.cs`:

```csharp
using Goose.Events;

namespace Goose
{
    public class GroundZones
    {
        public const int MinTickMs = 100;

        private static int nextId;

        private readonly Map map;
        private readonly List<GroundZone> zones = [];
        private readonly Dictionary<(int X, int Y), GroundZone> owners = [];

        public GroundZones(Map map)
        {
            this.map = map;
        }

        public IReadOnlyList<GroundZone> All => this.zones;

        public GroundZone? OwnerAt(int x, int y) => this.owners.GetValueOrDefault((x, y));

        public bool IsActiveAt(GroundZone zone, int x, int y) => zone.Overlaps || this.OwnerAt(x, y) == zone;

        public GroundZone Create(SpellEffect effect, ICharacter caster, int x, int y, GameWorld world)
        {
            long now = world.TimeNow;
            var zone = new GroundZone
            {
                Id = ++nextId,
                Effect = effect,
                Caster = caster,
                Map = this.map,
                Tiles = SpellEffect.ShapeTiles(effect.TargetType, effect.TargetSize, x, y, caster.Facing)
                    .Where(t => t.X >= 1 && t.X <= this.map.Width && t.Y >= 1 && t.Y <= this.map.Height)
                    .ToHashSet(),
                CreatedAt = now,
                ExpiresAt = now + effect.Duration * world.TimerFrequency,
            };

            this.zones.Add(zone);
            if (!zone.Overlaps)
                foreach (var tile in zone.Tiles)
                    this.owners[tile] = zone;

            this.Apply(zone, world);
            this.Schedule(zone, now, world);
            return zone;
        }

        internal void Tick(GroundZone zone, long now, GameWorld world)
        {
            if (!this.zones.Contains(zone)) return;

            if (now >= zone.ExpiresAt)
            {
                this.Remove(zone);
                return;
            }

            this.Apply(zone, world);
            this.Schedule(zone, now, world);
        }

        private void Remove(GroundZone zone)
        {
            this.zones.Remove(zone);
            if (zone.Overlaps) return;

            foreach (var tile in zone.Tiles)
            {
                if (this.owners.GetValueOrDefault(tile) != zone) continue;

                var next = this.zones.LastOrDefault(z => !z.Overlaps && z.Tiles.Contains(tile));
                if (next is null) this.owners.Remove(tile);
                else this.owners[tile] = next;
            }
        }

        private void Apply(GroundZone zone, GameWorld world)
        {
            if (!zone.Effect.HasFormula) return;

            foreach (var (x, y) in zone.Tiles)
            {
                if (!this.IsActiveAt(zone, x, y)) continue;

                var target = this.map.GetCharacterAt(x, y);
                if (target is null || !IsAlive(target)) continue;
                if (!zone.Effect.CanCastSpell(zone.Caster, target, this.map)) continue;

                zone.Effect.ApplyFormula(zone.Caster, target, world, animate: false, out _, out _);
            }
        }

        private static bool IsAlive(ICharacter character) => character switch
        {
            Player player => player.State == Player.States.Ready && !player.IsDead,
            NPC npc => npc.State == NPC.States.Alive,
            _ => false,
        };

        private void Schedule(GroundZone zone, long now, GameWorld world)
        {
            long interval = Math.Max(MinTickMs, zone.Effect.ZoneTickMs) * world.TimerFrequency / 1000;
            var ev = new GroundZoneTickEvent { Data = zone };
            ev.Ticks = Math.Min(now + interval, zone.ExpiresAt);
            world.EventHandler.AddEvent(ev);
        }
    }
}
```

`Goose/Events/GroundZoneTickEvent.cs`:

```csharp
namespace Goose.Events
{
    public class GroundZoneTickEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            var zone = (GroundZone)this.Data;
            zone.Map.GroundZones.Tick(zone, world.TimeNow, world);
        }
    }
}
```

`Goose/Map.cs` — add near the other runtime state (after `public object? ScriptStore { get; set; }`):

```csharp
        public GroundZones GroundZones { get; }
```

and in the `Map()` constructor, after `this.items = [];`:

```csharp
            this.GroundZones = new GroundZones(this);
```

`CloneAs` needs no change: it constructs through `new Map { … }`, which runs this constructor.

`Goose/SpellEffect.cs` — add next to `ShapeTiles`:

```csharp
        public bool HasFormula => IsFormulaSet(this.HPFormula) || IsFormulaSet(this.MPFormula);

        private static bool IsFormulaSet(string? formula) => !string.IsNullOrWhiteSpace(formula) && formula != "0";
```

**Step 5: Run the tests**

Run: `dotnet test Goose.Tests --filter "FullyQualifiedName~GroundZonesTests|FullyQualifiedName~MapCloneTests"`
Expected: PASS.

If `Create_schedules_the_next_tick_one_interval_later` sees an extra event, something in the fixture enqueued
one (e.g. an NPC attack event). That test deliberately spawns no NPCs and uses a non-online `PlayerOn`
caster; keep it that way rather than loosening the assertion.

**Step 6: Commit**

```bash
git add Goose/GroundZone.cs Goose/GroundZones.cs Goose/Events/GroundZoneTickEvent.cs Goose/Map.cs Goose/SpellEffect.cs \
  Goose.Tests/GroundZones Goose.Tests/MapCloneTests.cs
git commit -m "feat(spells): ground zones with tile ownership and ticking"
```

---

### Task 7: Cast ground zones from spells, and describe them

**Files:**
- Modify: `Goose/SpellEffect.cs` — `Cast()` (`:1158-1167`), `BuiltInItemDescription` (`:490-566`)
- Create: `Goose.Tests/GroundZones/GroundZoneCastTests.cs`

**Step 1: Write the failing tests**

```csharp
namespace Goose.Tests.GroundZones;

public class GroundZoneCastTests
{
    private static Spell Learn(GroundZoneTestKit kit, Player caster, SpellEffect effect, Spell.SpellTargets target)
    {
        var spell = kit.Fixture.AddBaseSpell(1, "Zone Spell", effect.ID, s => s.Target = target);
        Assert.True(caster.Spellbook.AddSpell(spell, kit.World));
        return spell;
    }

    [Fact]
    public void A_self_spell_drops_the_zone_on_the_caster()
    {
        using var kit = new GroundZoneTestKit();
        var caster = kit.PlayerAt(10, 10);
        var npc = kit.NpcAt(11, 10);
        long full = npc.CurrentHP;
        Learn(kit, caster, kit.Zone(1, e => e.Effected = SpellEffect.SpellEffected.NPC), Spell.SpellTargets.Self);

        caster.CastSpell(1, caster, kit.World);

        var zone = Assert.Single(kit.Map.GroundZones.All);
        Assert.Contains((10, 10), zone.Tiles);
        Assert.Equal(9, zone.Tiles.Count);
        Assert.Equal(full - 10, npc.CurrentHP);
    }

    [Fact]
    public void A_target_spell_pins_the_zone_where_the_target_stood()
    {
        using var kit = new GroundZoneTestKit();
        var caster = kit.PlayerAt(20, 20);
        var npc = kit.NpcAt(10, 10);
        Learn(kit, caster, kit.Zone(1), Spell.SpellTargets.Target);

        caster.CastSpell(1, npc, kit.World);
        npc.MoveTo(kit.World, 15, 15);

        var zone = Assert.Single(kit.Map.GroundZones.All);
        Assert.Contains((10, 10), zone.Tiles);
        Assert.DoesNotContain((15, 15), zone.Tiles);
    }

    [Fact]
    public void No_zone_is_created_on_a_map_that_blocks_casting()
    {
        using var kit = new GroundZoneTestKit();
        kit.Map.CanCast = false;
        var caster = kit.PlayerAt(10, 10);
        Learn(kit, caster, kit.Zone(1), Spell.SpellTargets.Self);

        caster.CastSpell(1, caster, kit.World);

        Assert.Empty(kit.Map.GroundZones.All);
    }

    [Fact]
    public void The_cast_animation_plays_once_on_creation_not_per_hit()
    {
        using var kit = new GroundZoneTestKit();
        var caster = kit.PlayerAt(10, 10);
        kit.NpcAt(11, 10);
        kit.NpcAt(9, 10);
        Learn(kit, caster, kit.Zone(1, e => { e.DoCastAnimation = true; e.Effected = SpellEffect.SpellEffected.NPC; }),
            Spell.SpellTargets.Self);

        caster.CastSpell(1, caster, kit.World);

        var packets = string.Concat(caster.Sent).Split('\x1');
        Assert.Single(packets, p => p == "CST" + caster.LoginID);
    }

    [Fact]
    public void Description_names_the_zone_and_its_tick()
    {
        using var kit = new GroundZoneTestKit();
        var effect = kit.Zone(1, e => { e.Duration = 8; e.ZoneTickMs = 1500; });

        var lines = effect.GetItemDescription(kit.World).ToList();

        Assert.Contains("Ground zone for 8s, ticks every 1.5s", lines);
        Assert.Contains(lines, l => l.StartsWith("Zone> ") && l.Contains("HP"));
    }
}
```

`npc.MoveTo` is `NPC.MoveTo(GameWorld, int, int)` (`Goose/NPC.cs:562`).

**Step 2: Run to verify they fail**

Run: `dotnet test Goose.Tests --filter FullyQualifiedName~GroundZoneCastTests`
Expected: FAIL — `Cast()` has no `GroundZone` branch, so `TargetType = Area` runs the normal area-spell
path and `CastSpell` returns `false` for every tile (no zone is created); description tests fail too.

**Step 3: Implement**

In `Cast()`, directly after `caster.BreakInvisibility(world);`:

```csharp
            if (this.EffectType == EffectTypes.GroundZone)
                return this.CastGroundZone(caster, target, world);
```

Add the method next to `Cast()`:

```csharp
        private bool CastGroundZone(ICharacter caster, ICharacter anchor, GameWorld world)
        {
            var map = anchor.Map;
            if (!MapAllowsCasting(caster, map)) return false;

            map.GroundZones.Create(this, caster, anchor.MapX, anchor.MapY, world);

            if (this.DoCastAnimation)
            {
                string packet = P.Cast(caster);
                if (caster is Player player) world.Send(player, packet);
                foreach (var p in map.GetPlayersInRange(caster))
                    world.Send(p, packet);
            }

            return true;
        }
```

In `BuiltInItemDescription`, add a case before `default:`:

```csharp
                case EffectTypes.GroundZone:
                    yield return $"Ground zone for {this.Duration}s, ticks every {Math.Max(GroundZones.MinTickMs, this.ZoneTickMs) / 1000.0:0.#}s";
                    if (!string.IsNullOrWhiteSpace(this.HPFormula) && this.HPFormula != "0")
                        yield return GetFormulaDescription(this.HPFormula, "HP", "Zone> ");
                    if (!string.IsNullOrWhiteSpace(this.MPFormula) && this.MPFormula != "0")
                        yield return GetFormulaDescription(this.MPFormula, "MP", "Zone> ");
                    break;
```

`:0.#` formats with the current culture. If `Description_names_the_zone_and_its_tick` fails on a
comma-decimal machine, use `string.Create(CultureInfo.InvariantCulture, $"…")` for that line instead of
changing the test.

**Step 4: Run the tests**

Run: `dotnet test Goose.Tests --filter FullyQualifiedName~GroundZoneCastTests`
Expected: PASS (5 tests).

**Step 5: Commit**

```bash
git add Goose/SpellEffect.cs Goose.Tests/GroundZones/GroundZoneCastTests.cs
git commit -m "feat(spells): cast ground zones from self and target spells"
```

---

### Task 8: Full verification and manual smoke

**Step 1: Run every suite**

```bash
dotnet build Goose.sln -v q
dotnet test Goose.Tests
dotnet test Goose.IntegrationTests
```

Expected: 0 build errors; all tests pass. Report the actual counts. If an integration test failed on the
master baseline too (it wasn't run when the worktree was created), say so instead of fixing it here.

**Step 2: Manual smoke with the existing client**

1. Add a test effect to the game data: `effect_type` GroundZone, `target_type` Area, `target_size` 1,
   `effect_duration` 8, `zone_tick_ms` 1000, `hp_change_formula` `-5`, `spell_effected` NPC,
   `do_cast_animation` 1. Add a Self spell and a Target spell using it, and give both to a GM character.
2. Run CsvToSql and start the server.
3. Cast the Self spell next to a training dummy. Expected: one cast animation, then damage numbers on
   every mob in the 3×3 around you, once a second for 8 seconds. **Nothing is drawn on the ground** —
   that's expected until Parts 2 and 3.
4. Cast the Target spell on a mob, then pull it out of the area. Expected: damage stops when it steps
   out, and starts again if it walks back in before the zone expires.
5. Cast two overlapping zones with different formulas. Expected: on shared tiles only the newer zone's
   numbers appear.

**Step 3: Commit anything left over**

If you changed nothing in this task, skip this step.
