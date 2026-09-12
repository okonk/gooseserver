# NPC Spawn Properties — Part 1 (Data Layer + Server) Implementation Plan

**Goal:** Add a `properties` JSON text column to `npc_spawns`, parse it at load into
`npc.Properties`, and let the `canMove` key override `NPC.CanMove` for that one spawn.

**Architecture:** The column is appended last in the `NpcSpawnsCsvToSql` descriptor, so the
existing positional cell contract is untouched. `NPCHandler.LoadNPCs` — the only consumer of
the table — parses the cell into a `PropertiesDictionary` and passes it through optional
trailing parameters on `SpawnNPC` and `LoadFromTemplate`, which store it on the NPC and read
one known key with the template value as the fallback. Dimension copies of a base spawn pass
a clone of the same dictionary, so a mirrored mob behaves like the row it mirrors.

**Tech Stack:** C# / .NET 10, xUnit, NLog, System.Data.SQLite, `CsvToSql.Core` descriptors,
`tools/SchemaGen`, openpyxl for the test fixture workbook, `gws` for the live worksheet.

**Design doc:** `docs/plans/2026-09-12-npc-spawn-properties-design.md`

This is Part 1 of 2. Part 1 deliberately leaves `Goose2ClientGodot` untouched, so that repo's
four-column schema pins stay valid. Part 2 (`-part2-map-editor.md`) regenerates the map
editor's schema artifact and carries the value through its pull/edit/push paths.

---

## APIs verified

| API | Citation |
| --- | --- |
| `NpcSpawnsCsvToSql.GetColumnDescriptors()` | `CsvToSql/CsvToSql.Core/NpcSpawnsCsvToSql.cs:7` |
| `Col.Text(string, SqlType, string def)` | `CsvToSql/CsvToSql.Core/Schema/Col.cs:18` |
| `Col.Make` marks `Required` when `def == null` | `CsvToSql/CsvToSql.Core/Schema/Col.cs:35-36` |
| `Column.HeaderText(string)` | `CsvToSql/CsvToSql.Core/Schema/Column.cs:61-68` |
| `TableDdl.Emit` writes `Default` verbatim | `CsvToSql/CsvToSql.Core/Schema/TableDdl.cs:32-33` |
| `DescriptorTransform.Apply` single-quotes `Text` | `CsvToSql/CsvToSql.Core/Schema/DescriptorTransform.cs:11-13` |
| `PropertiesDictionary.GetProperty<T>(string, T)` | `Goose/PropertiesDictionary.cs:40-48` |
| `PropertiesDictionary.ConvertValue` (no numeric→bool path) | `Goose/PropertiesDictionary.cs:74-121` |
| `PropertiesDictionary.Clone()` | `Goose/PropertiesDictionary.cs:140-146` |
| `JsonHelper.Deserialize<T>(string)` | `Goose/JsonHelper.cs:48-49` |
| Converter returns null on `null`, throws on a non-object root | `Goose/PropertiesDictionaryJsonConverter.cs:19-27` |
| `Player.Properties` precedent | `Goose/Player.cs:475` |
| `NPC.LoadFromTemplate` | `Goose/NPC.cs:595` |
| `this.CanMove = template.CanMove` | `Goose/NPC.cs:630` |
| `this.Spawn(world)` at the end of load | `Goose/NPC.cs:672` |
| `CanMove` is only consulted at move-tick time | `Goose/NPC.cs:411-430` |
| `NPCHandler.SpawnNPC` | `Goose/NPCHandler.cs:349-356` |
| `NPCHandler.LoadNPCs` / `SELECT * FROM npc_spawns` | `Goose/NPCHandler.cs:307-335`, `:312` |
| `NPCHandler.log` (static NLog) | `Goose/NPCHandler.cs:13` |
| `DataReaderExtensions.GetString` | `Goose/DataReaderExtensions.cs:13-14` |
| `Database.Start(string)` | `Goose/Database.cs:46` |
| Dimension mirror `SpawnNPC` call | `Goose/Data/Illutia/Scripts/Global/Dimensions/Npcs.csx:36` |
| `DimensionConstants.Offset = 100000` | `Goose/Data/Illutia/Scripts/Global/Dimensions/DimensionConstants.csx:15` |
| Server copies `sql/*.sql` to test output | `Goose/Goose.csproj:29-31` |
| `TestWorldFixture` compiled into both test projects | `Goose.Tests/Goose.Tests.csproj:28`, `Goose.IntegrationTests/Goose.IntegrationTests.csproj:64` |
| `TestWorldFixture.AddBaseMap` / `SeedClass` | `TestSupport/TestWorldFixture.cs:45`, `:158` |
| DB-backed test pattern | `Goose.Tests/Part2PetsTests.cs:275-280` |
| `CapturingLog` | `Goose.Tests/CapturingLog.cs:7` |
| `GlobalScriptFixture.CompileShipped` | `Goose.IntegrationTests/Fixtures/GlobalScriptFixture.cs:52-58` |
| `DimensionsScriptTests.Run` | `Goose.IntegrationTests/DimensionsScriptTests.cs:11-15` |
| Snapshot regeneration command | `Goose.IntegrationTests/CsvToSqlSnapshotTests.cs:31-50` |
| Workbook header contract | `Goose.IntegrationTests/Schema/WorkbookHeaderContractTests.cs:8-30` |
| `write_range(title, first_row, first_col, values, render_option)` | `.agents/skills/goose-game-data/scripts/gsheets.py:221` |
| Column index is 0-based (A = 0) | `.agents/skills/goose-game-data/scripts/gsheets.py:330` |

---

## Task 1: The `properties` column, fixtures and snapshot

**Files:**
- Modify: `CsvToSql/CsvToSql.Core/NpcSpawnsCsvToSql.cs:7-13`
- Modify: `Goose/sql/npcs.sql:69-74`
- Modify: `Goose.IntegrationTests/Fixtures/aspereta-data.xlsx` (`NPC Spawns` E1)
- Regenerate: `Goose.IntegrationTests/Fixtures/generated.snapshot`

**Mutation impact:**
- Source of truth changed: the `npc_spawns` column list, `NpcSpawnsCsvToSql.cs:7`.
- Important readers: `NPCHandler.LoadNPCs` (`Goose/NPCHandler.cs:312`, positional read of A–D
  today), `tools/SchemaGen/SchemaModel.cs` (feeds both generated schema artifacts), the
  map editor's `SheetRowMapper`, and `Goose/sql/npcs.sql` for a fresh database.
- Derived/cached state affected: `generated.snapshot` (regenerated below) and, from Part 2,
  `tools/DataEditor/schema.js` and the map editor's `game-data-schema.json`.
- Required propagation sequence:
  1. Descriptor gains the fifth column.
  2. Shipped DDL gains the matching column.
  3. Fixture workbook gains the matching E1 header, or the header contract test fails.
  4. Snapshot is regenerated and its diff reviewed.
- Invariants to preserve: cells are read by index, so the new column must be last and no
  existing descriptor may move; `properties` must NOT be `required` (an existing blank cell
  must resolve to the default, not be rejected).
- Observable proof required: the workbook header contract passes; the snapshot diff is
  confined to `npc_spawns`; the fixture diff is exactly one cell.

**Step 1: Add the descriptor**

```csharp
public override Column[] GetColumnDescriptors() => new[]
{
    Col.Id("npc_id", SqlType.Int).Ref("NPCs").HeaderText("npc id"),
    Col.Id("map_id", SqlType.SmallInt).Ref("Maps").HeaderText("map id"),
    Col.Int("map_x", SqlType.SmallInt).HeaderText("map x"),
    Col.Int("map_y", SqlType.SmallInt).HeaderText("map y"),
    Col.Text("properties", SqlType.Text, "''").HeaderText("properties"),
};
```

The `"''"` default is what keeps `Required` false (`Col.cs:35-36`) and emits
`DEFAULT ''` (`TableDdl.cs:32`). A header of plain `properties` matches the sheet's convention
that an empty default shows no parentheses.

**Step 2: Add the column to the shipped DDL**

In `Goose/sql/npcs.sql`, turn `map_y SMALLINT NOT NULL` into `map_y SMALLINT NOT NULL,` and
append:

```sql
  properties TEXT DEFAULT '' NOT NULL
```

A database created from this file must have the column; the loader reads it by name.

**Step 3: Run the two contract tests and confirm they fail for the expected reasons (red)**

```bash
dotnet test Goose.IntegrationTests --filter "FullyQualifiedName~WorkbookHeaderContractTests|FullyQualifiedName~CsvToSqlSnapshotTests"
```

Expected: `Every_descriptor_header_matches_the_workbook_row_one` fails for `NPC Spawns`
(expected `"properties"`, actual empty), and the snapshot test fails naming `npc_spawns` in
its single reported line of difference. If any *other* test fails, something moved that
should not have — stop and investigate before continuing.

**Step 4: Patch the fixture workbook's E1 header**

The fixture is a frozen copy of a real workbook. Patch it in place rather than re-downloading
it, and do not let values change anywhere else. The round trip below was verified to preserve
every cell value on all 21 sheets.

```bash
python3 - <<'EOF'
import openpyxl
path = 'Goose.IntegrationTests/Fixtures/aspereta-data.xlsx'
wb = openpyxl.load_workbook(path)
ws = wb['NPC Spawns']
assert ws.cell(row=1, column=5).value in (None, ''), ws.cell(row=1, column=5).value
ws.cell(row=1, column=5).value = 'properties'
wb.save(path)
EOF
```

**Step 5: Verify exactly one cell changed**

```bash
python3 - <<'EOF'
import subprocess, openpyxl, io
path = 'Goose.IntegrationTests/Fixtures/aspereta-data.xlsx'
old_bytes = subprocess.run(['git', 'show', 'HEAD:' + path], capture_output=True, check=True).stdout

def values(src):
    wb = openpyxl.load_workbook(io.BytesIO(src) if isinstance(src, bytes) else src,
                                read_only=True, data_only=False)
    out = {n: [list(r) for r in wb[n].iter_rows(values_only=True)] for n in wb.sheetnames}
    wb.close()
    return out

old, new = values(old_bytes), values(path)
assert list(old) == list(new), (list(old), list(new))
diffs = []
for sheet in old:
    assert len(old[sheet]) == len(new[sheet]), (sheet, len(old[sheet]), len(new[sheet]))
    for i, (ra, rb) in enumerate(zip(old[sheet], new[sheet])):
        assert len(ra) == len(rb), (sheet, i + 1, len(ra), len(rb))
        for j, (ca, cb) in enumerate(zip(ra, rb)):
            if ca != cb:
                diffs.append((sheet, i + 1, j + 1, ca, cb))

print("changed cells:", diffs)
assert diffs == [('NPC Spawns', 1, 5, None, 'properties')], diffs
print("fixture diff is exactly one cell")
EOF
```

Expected: `changed cells: [('NPC Spawns', 1, 5, None, 'properties')]` and no assertion error.

**Step 6: Regenerate the snapshot and review the diff**

```bash
GOOSE_UPDATE_SNAPSHOT=1 dotnet test Goose.IntegrationTests --filter FullyQualifiedName~CsvToSqlSnapshot
```

Expected: this run **fails on purpose** with `Rewrote .../generated.snapshot. Review the diff,
commit it if it is what you meant...` (`CsvToSqlSnapshotTests.cs:45-50`). That failure is the
confirmation the file was rewritten, not a problem.

**Step 7: Prove the diff is confined to `npc_spawns`**

```bash
git show HEAD:Goose.IntegrationTests/Fixtures/generated.snapshot | awk '/^TABLE: /{t=$2} t!="npc_spawns"' > /tmp/snap-before-rest
awk '/^TABLE: /{t=$2} t!="npc_spawns"' Goose.IntegrationTests/Fixtures/generated.snapshot > /tmp/snap-after-rest
diff /tmp/snap-before-rest /tmp/snap-after-rest && echo "nothing outside npc_spawns changed"

git show HEAD:Goose.IntegrationTests/Fixtures/generated.snapshot | awk '/^TABLE: npc_spawns/{t=1} /^TABLE: /{if ($2!="npc_spawns") t=0} t' | wc -l
awk '/^TABLE: npc_spawns/{t=1} /^TABLE: /{if ($2!="npc_spawns") t=0} t' Goose.IntegrationTests/Fixtures/generated.snapshot | wc -l
```

Expected: `nothing outside npc_spawns changed`, and both line counts equal (the block gains one
`COL:` line and the row count is unchanged). Then read the `npc_spawns` diff yourself: it must
be one added `COL: properties TEXT DEFAULT '' NOT NULL` line plus every row line gaining a
trailing `|properties=`.

**Step 8: Confirm green**

```bash
dotnet test Goose.IntegrationTests
```

Expected: all pass. `Goose.Tests` and `Tools.Tests` are unaffected by this task.

**Step 9: Commit**

```bash
git add CsvToSql/CsvToSql.Core/NpcSpawnsCsvToSql.cs Goose/sql/npcs.sql \
        Goose.IntegrationTests/Fixtures/aspereta-data.xlsx \
        Goose.IntegrationTests/Fixtures/generated.snapshot
git commit -m "feat: add properties column to npc_spawns"
```

| Invariant | Proved by |
| --- | --- |
| The descriptor header matches the workbook header | `WorkbookHeaderContractTests` (Task 1 Step 8) |
| The generated DDL carries the column | snapshot `COL: properties TEXT DEFAULT '' NOT NULL` (Step 7) |
| A blank cell is not `required` and resolves to the default | 4,322 unchanged rows with `properties=` in the snapshot (Step 7) |
| No other table, column or row moved | `diff /tmp/snap-before-rest /tmp/snap-after-rest` (Step 7) |
| The fixture changed in exactly one cell | Step 5 assertion |

---

## Task 2: `NPC.Properties`, the plumbing and the `canMove` read

**Files:**
- Modify: `Goose/NPC.cs` (property; `:595` signature; `:630` read)
- Modify: `Goose/NPCHandler.cs:349-356` (`SpawnNPC` signature and forwarding)
- Create: `Goose.Tests/NPCSpawnPropertiesTests.cs`

**Mutation impact:**
- Source of truth changed: `NPC.CanMove` (`Goose/NPC.cs:282`) and the new `NPC.Properties`.
- Important readers: `NPC.OnMoveEvent` reads `CanMove` at move-tick time
  (`Goose/NPC.cs:411-430`); `NPCMoveEvent` drives it. `NPCTemplate.CanMove` is read only at
  `NPC.cs:630`, so overriding the NPC field cannot leak into the shared template or into other
  NPCs spawned from it.
- Derived/cached state affected: no derived state. `AddMoveEvent` (`NPC.cs:729-751`) is gated
  on `MoveSpeed > 0`, not `CanMove`, so no event wiring depends on the value.
- Required propagation sequence:
  1. `LoadFromTemplate` assigns `this.Properties`, then assigns `this.CanMove` from the key
     with `template.CanMove` as the fallback.
  2. Both assignments must happen **before** `this.Map.AddNPC(this)` (`NPC.cs:670`), which is
     the publication point where other threads can first see the NPC. Assigning the dictionary
     alongside the other template copies near the top of the method satisfies this; do not move
     it below `AddNPC`.
  3. `this.Spawn(world)` (`NPC.cs:672`) runs afterwards and reads nothing that depends on the
     value, so no ordering constraint exists between those two.
  4. Respawn reuses the NPC object (`NPCSpawnEvent` → `npc.Spawn`), which never re-reads the
     template, so the value persists with no extra code.
- Threading: `LoadNPCs` runs inside `world.Database.Execute`'s callback on the database thread
  (`Database.cs:171`), exactly as the existing spawn work does; this change adds a parse and
  two field assignments to that same callback and introduces no new cross-thread access. The
  move-tick read of `CanMove` on the game thread is covered by the ordering above.
- Invariants to preserve: every existing caller keeps working — the new parameter is optional
  and trailing; a null dictionary yields an empty, non-null `Properties`.
- Observable proof required: an NPC spawned with `{["canMove"] = true}` against a stationary
  template has `CanMove == true` **and** `Properties["canMove"]` present, so the test cannot
  pass by accident through some other path.

**Step 1: Write the failing tests**

Create `Goose.Tests/NPCSpawnPropertiesTests.cs`. `TestWorldFixture` is already compiled into
this project (`Goose.Tests.csproj:28`) and seeds class 0 with levels 1–50
(`TestWorldFixture.cs:158`), so `ClassID = 0, Level = 50` resolves.

```csharp
using Goose.Testing;

namespace Goose.Tests;

public class NPCSpawnPropertiesTests
{
    private const int MapId = 1;

    private static NPCTemplate Template(bool canMove) => new()
    {
        NPCTemplateID = 1,
        Name = "Test NPC",
        Level = 50,
        ClassID = 0,
        BaseStats = new AttributeSet(),
        CanMove = canMove,
    };

    private static NPC Spawn(TestWorldFixture fixture, bool templateCanMove,
                             PropertiesDictionary? properties)
    {
        fixture.AddBaseMap(MapId, "Town");
        var template = Template(templateCanMove);
        fixture.World.NPCHandler.AddTemplate(template);
        return fixture.World.NPCHandler.SpawnNPC(
            fixture.World, MapId, 5, 5, template, shouldRespawn: true, properties)!;
    }

    [Fact]
    public void canMove_true_overrides_a_stationary_template()
    {
        using var fixture = new TestWorldFixture();

        var npc = Spawn(fixture, templateCanMove: false,
                        new PropertiesDictionary { ["canMove"] = true });

        Assert.True(npc.CanMove);
        Assert.True(npc.Properties.GetProperty<bool>("canMove"));
    }

    [Fact]
    public void canMove_false_overrides_a_movable_template()
    {
        using var fixture = new TestWorldFixture();

        var npc = Spawn(fixture, templateCanMove: true,
                        new PropertiesDictionary { ["canMove"] = false });

        Assert.False(npc.CanMove);
    }

    [Fact]
    public void An_empty_dictionary_keeps_the_template_value()
    {
        using var fixture = new TestWorldFixture();

        var npc = Spawn(fixture, templateCanMove: true, new PropertiesDictionary());

        Assert.True(npc.CanMove);
        Assert.Empty(npc.Properties);
    }

    [Fact]
    public void Omitting_the_properties_argument_keeps_the_template_value()
    {
        using var fixture = new TestWorldFixture();
        fixture.AddBaseMap(MapId, "Town");
        var template = Template(canMove: true);
        fixture.World.NPCHandler.AddTemplate(template);

        var npc = fixture.World.NPCHandler.SpawnNPC(
            fixture.World, MapId, 5, 5, template, shouldRespawn: true)!;

        Assert.True(npc.CanMove);
        Assert.Empty(npc.Properties);
    }

    [Fact]
    public void A_numeric_canMove_throws_rather_than_being_coerced()
    {
        using var fixture = new TestWorldFixture();

        // Accepted risk, recorded in the design doc: the sheet writes its Bool columns as
        // 0/1, so this is the mistake authors actually make, and it stops the load.
        Assert.Throws<InvalidCastException>(() =>
            Spawn(fixture, templateCanMove: false,
                  new PropertiesDictionary { ["canMove"] = 1L }));
    }
}
```

**Step 2: Run them and confirm they fail (red)**

```bash
dotnet test Goose.Tests --filter FullyQualifiedName~NPCSpawnPropertiesTests
```

Expected: compile failure — `SpawnNPC` has no overload taking 7 arguments, and `NPC` has no
`Properties`.

**Step 3: Add `NPC.Properties` and the optional parameters**

In `Goose/NPC.cs`, immediately after `ShouldRespawn` (`NPC.cs:48`):

```csharp
public PropertiesDictionary Properties { get; set; } = new PropertiesDictionary();
```

In `Goose/NPCHandler.cs`, `SpawnNPC` gains a trailing optional parameter and forwards it:

```csharp
public NPC? SpawnNPC(GameWorld world, int mapId, int mapX, int mapY, NPCTemplate template,
                     bool shouldRespawn, PropertiesDictionary? properties = null)
{
    var npc = new NPC();
    if (!npc.LoadFromTemplate(world, mapId, mapX, mapY, template, shouldRespawn, properties)) return null;

    this.AddNPC(npc);
    return npc;
}
```

In `Goose/NPC.cs`, `LoadFromTemplate` gains the same trailing parameter, assigns the
dictionary next to the other template copies, and changes the `CanMove` line (`NPC.cs:630`):

```csharp
this.Properties = properties ?? new PropertiesDictionary();
...
this.CanMove = this.Properties.GetProperty("canMove", template.CanMove);
```

**Step 4: Run to green**

```bash
dotnet test Goose.Tests --filter FullyQualifiedName~NPCSpawnPropertiesTests
```

Expected: 5 passed. The fifth is expected to pass by throwing: if it fails, either the
dictionary gained a numeric→bool coercion (a design decision that must be taken deliberately,
not drifted into) or the read moved off `GetProperty`.

**Step 5: Confirm nothing else broke**

```bash
dotnet test Goose.Tests
```

Expected: all pass, including `NPCSpawnRegistrationTests`, which calls `SpawnNPC` without the
new argument.

**Step 6: Commit**

```bash
git add Goose/NPC.cs Goose/NPCHandler.cs Goose.Tests/NPCSpawnPropertiesTests.cs
git commit -m "feat: add NPC.Properties and the canMove spawn override"
```

| Invariant | Proved by |
| --- | --- |
| `canMove:true` turns a stationary spawn movable | `canMove_true_overrides_a_stationary_template` |
| `canMove:false` turns a movable spawn stationary | `canMove_false_overrides_a_movable_template` (adversarial: an implementation that ORs rather than assigns fails it) |
| The dictionary reaches the NPC | `npc.Properties.GetProperty<bool>("canMove")` asserted alongside `CanMove` |
| Unknown/absent key is a no-op | `An_empty_dictionary_keeps_the_template_value` |
| Existing script spawners keep compiling and behaving | `Omitting_the_properties_argument_keeps_the_template_value`, plus all of `Goose.Tests` |
| A numeric value throws rather than coercing | `A_numeric_canMove_throws_rather_than_being_coerced` |

---

## Task 3: `LoadNPCs` reads and parses the cell

**Files:**
- Modify: `Goose/NPCHandler.cs:307-335`
- Create: `Goose.Tests/NPCSpawnPropertiesLoadTests.cs`

**Mutation impact:**
- Source of truth changed: nothing new is mutated; this is the read path that fills
  `NPC.Properties` from `npc_spawns.properties`.
- Important readers: `NPC.LoadFromTemplate` via the new parameter (Task 2); indirectly
  `NPC.OnMoveEvent` through `CanMove`.
- Derived/cached state affected: no derived state.
- Required propagation sequence:
  1. `LoadNPCs` reads each row's `properties` cell.
  2. The cell is parsed to a `PropertiesDictionary` (empty on blank or unreadable).
  3. The dictionary is passed to `SpawnNPC`, which forwards it to `LoadFromTemplate`.
  4. `LoadFromTemplate` assigns `Properties` and reads `canMove`.
- Invariants to preserve: a blank cell means "no overrides"; an unreadable cell must not stop
  the spawn (a bad override never removes a mob from the world), but the error must name the
  row; a valid cell with a wrongly-typed known key is *not* guarded and will throw (accepted).
- Observable proof required: a row read out of a real SQLite `npc_spawns` table changes
  `npc.CanMove`, which proves the whole DB→NPC path rather than a hand-passed dictionary.

**Step 1: Write the failing tests**

Create `Goose.Tests/NPCSpawnPropertiesLoadTests.cs`. This uses the shipped `sql/npcs.sql` DDL
from the test output (`Goose.csproj:29-31`) so a column added to the descriptor but not to the
shipped DDL fails here, and the DB pattern from `Part2PetsTests.cs:275-280`.

```csharp
using System.Data.SQLite;
using Goose.Testing;

namespace Goose.Tests;

public class NPCSpawnPropertiesLoadTests
{
    private const int MapId = 1;

    private static TestWorldFixture WorldWithSpawnRow(string? propertiesCell)
    {
        var fixture = new TestWorldFixture();
        fixture.AddBaseMap(MapId, "Town");
        fixture.World.NPCHandler.AddTemplate(new NPCTemplate
        {
            NPCTemplateID = 1, Name = "Test NPC", Level = 50, ClassID = 0,
            BaseStats = new AttributeSet(), CanMove = false,
        });

        fixture.World.Database.Start(Path.Combine(fixture.DataDirectory, "test.db"));
        fixture.World.Database.Execute(conn =>
        {
            using (var schema = conn.CreateCommand())
            {
                schema.CommandText = File.ReadAllText(
                    Path.Combine(AppContext.BaseDirectory, "sql", "npcs.sql"));
                schema.ExecuteNonQuery();
            }

            using var insert = conn.CreateCommand();
            insert.CommandText =
                "INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y, properties) " +
                "VALUES (1, " + MapId + ", 5, 5, @p)";
            insert.Parameters.Add(new SQLiteParameter("@p", propertiesCell));
            insert.ExecuteNonQuery();
        });

        return fixture;
    }

    private static NPC OnlyNpc(TestWorldFixture fixture) =>
        Assert.Single(fixture.World.MapHandler.GetMap(MapId)!.NPCs);

    [Fact]
    public void A_spawn_row_cell_reaches_the_loaded_npc()
    {
        using var fixture = WorldWithSpawnRow("{\"canMove\":true}");

        fixture.World.NPCHandler.LoadNPCs(fixture.World);

        var npc = OnlyNpc(fixture);
        Assert.True(npc.CanMove);
        Assert.True(npc.Properties.GetProperty<bool>("canMove"));
    }

    [Fact]
    public void A_blank_cell_keeps_the_template_value()
    {
        using var fixture = WorldWithSpawnRow("");

        fixture.World.NPCHandler.LoadNPCs(fixture.World);

        var npc = OnlyNpc(fixture);
        Assert.False(npc.CanMove);
        Assert.Empty(npc.Properties);
    }

    [Fact]
    public void An_unreadable_cell_still_spawns_the_npc_and_names_the_row()
    {
        using var log = new CapturingLog();
        using var fixture = WorldWithSpawnRow("{\"canMove\":");

        fixture.World.NPCHandler.LoadNPCs(fixture.World);

        var npc = OnlyNpc(fixture);
        Assert.False(npc.CanMove);
        Assert.Empty(npc.Properties);
        Assert.Contains(log.Messages, m =>
            m.Contains("npc_spawns") && m.Contains(MapId.ToString()) && m.Contains("properties"));
    }

    [Fact]
    public void A_json_root_that_is_not_an_object_is_unreadable_rather_than_fatal()
    {
        using var fixture = WorldWithSpawnRow("[]");

        fixture.World.NPCHandler.LoadNPCs(fixture.World);

        Assert.False(OnlyNpc(fixture).CanMove);
    }
}
```

**Step 2: Run and confirm they fail (red)**

```bash
dotnet test Goose.Tests --filter FullyQualifiedName~NPCSpawnPropertiesLoadTests
```

Expected: `A_spawn_row_cell_reaches_the_loaded_npc` fails on `Assert.True(npc.CanMove)` — the
cell is not read yet. The blank-cell test passes already (it is the control); the unreadable
tests fail on their assertions. If the INSERT fails with "no such column: properties", the
shipped DDL was not updated in Task 1 — fix that rather than working around it.

**Step 3: Implement the read and parse**

In `LoadNPCs`, add the parse and pass the result on:

```csharp
int npc_id = reader.GetInt32("npc_id");
int map_id = reader.GetInt32("map_id");
int map_x = reader.GetInt32("map_x");
int map_y = reader.GetInt32("map_y");
var properties = ParseSpawnProperties(reader.GetString("properties"), npc_id, map_id, map_x, map_y);
```

```csharp
if (this.SpawnNPC(world, map_id, map_x, map_y, template, shouldRespawn: true, properties) is null)
```

Add the helper to `NPCHandler`:

```csharp
internal static PropertiesDictionary ParseSpawnProperties(string json,
    int npcId, int mapId, int mapX, int mapY)
{
    if (string.IsNullOrWhiteSpace(json)) return new PropertiesDictionary();

    try
    {
        return JsonHelper.Deserialize<PropertiesDictionary>(json) ?? new PropertiesDictionary();
    }
    catch (Exception e)
    {
        log.Error(e, "npc_spawns: npc {0} on map {1} at {2},{3} has unreadable properties: {4}",
            npcId, mapId, mapX, mapY, json);
        return new PropertiesDictionary();
    }
}
```

Nothing guards the `canMove` read itself: a well-formed cell with a wrongly-typed value still
throws out of `LoadFromTemplate`. That is the accepted risk recorded in the design doc.

**Step 4: Run to green**

```bash
dotnet test Goose.Tests --filter FullyQualifiedName~NPCSpawnPropertiesLoadTests
```

Expected: 4 passed.

**Step 5: Full suite**

```bash
dotnet test
```

Expected: everything passes. `Goose.IntegrationTests` is where the DDL/snapshot contract lives
and must still be green from Task 1.

**Step 6: Commit**

```bash
git add Goose/NPCHandler.cs Goose.Tests/NPCSpawnPropertiesLoadTests.cs
git commit -m "feat: read npc_spawns.properties at load"
```

| Invariant | Proved by |
| --- | --- |
| The cell reaches the NPC through the DB read path | `A_spawn_row_cell_reaches_the_loaded_npc` (adversarial: it fails if only the plumbing, not the read, exists) |
| Blank means no overrides | `A_blank_cell_keeps_the_template_value` |
| An unreadable cell never removes a mob and names the row | `An_unreadable_cell_still_spawns_the_npc_and_names_the_row` |
| A non-object JSON root is tolerated, not fatal | `A_json_root_that_is_not_an_object_is_unreadable_rather_than_fatal` |
| The shipped DDL carries the column | the INSERT in `WorldWithSpawnRow` throws without it |

---

## Task 4: Dimension copies inherit spawn properties

**Files:**
- Modify: `Goose/Data/Illutia/Scripts/Global/Dimensions/Npcs.csx:36`
- Test: `Goose.IntegrationTests/DimensionsScriptTests.cs`

**Mutation impact:**
- Source of truth changed: the properties dictionary handed to a dimension copy's
  `SpawnNPC` call.
- Important readers: `NPC.LoadFromTemplate` → `NPC.CanMove`, exactly as for base spawns.
- Derived/cached state affected: no derived state.
- Required propagation sequence: the base NPC already carries `Properties`
  (`Npcs.csx:22-23` iterates the base maps' `NPCs`); the mirror call passes
  `basic.Properties?.Clone()`; the clone is read by `LoadFromTemplate` as usual.
- Invariants to preserve: the clone is per-dimension, so a later mutation on one cannot affect
  the base NPC or another dimension; script-created templates with no backing spawn row (the
  warden at `:315`, the rebirth keeper at `:463`) keep getting no properties.
- Observable proof required: the dimension copy of a spawn row that set `canMove:true` against
  a stationary template is itself movable.

**Step 1: Write the failing test**

Add to `Goose.IntegrationTests/DimensionsScriptTests.cs`, mirroring the existing
`Run(...)` helper (`:11-15`) and the clone test's template shape (`:60-68`). Template ids
below `Offset` are cloned to `baseId + 100000 * dim`
(`DimensionConstants.csx:15`), and `TestWorldFixture` seeds class 0 through level 50.

```csharp
[Fact]
public void Dimension_copies_inherit_the_spawn_rows_properties()
{
    var template = new NPCTemplate
    {
        NPCTemplateID = 162, Name = "Shadow Dog", Level = 40, ClassID = 0,
        MoveSpeed = 1.5, CanBeKilled = true, CanMove = false,
    };
    template.BaseStats = new AttributeSet { HP = 3704 };

    using var fixture = Run(f =>
    {
        f.AddBaseMap(1, "Town", width: 100, height: 100);
        f.World.NPCHandler.AddTemplate(template);
        f.World.NPCHandler.SpawnNPC(f.World, 1, 5, 5, template, shouldRespawn: true,
            new PropertiesDictionary { ["canMove"] = true });
    });

    var dimensionCopy = fixture.World.MapHandler.GetMap(1 + 100000)!
        .NPCs.Single(n => n.NPCTemplateID == 162 + 100000);

    Assert.True(dimensionCopy.CanMove);
}
```

**Step 2: Run and confirm it fails (red)**

```bash
dotnet test Goose.IntegrationTests --filter FullyQualifiedName~DimensionsScriptTests
```

Expected: `Assert.True(dimensionCopy.CanMove)` fails — the copy is stationary because the
mirror call passes no properties. If `GetMap(100001)` or the `.Single(...)` throws instead,
the base NPC was not registered before `OnLoaded`; check that the base spawn happened inside
the `arrange` action.

**Step 3: Pass the properties through**

At `Npcs.csx:36`, add the argument:

```csharp
world.NPCHandler.SpawnNPC(world, basic.Map.ID + Offset * dim,
                          basic.SpawnX, basic.SpawnY, template, shouldRespawn: true,
                          basic.Properties?.Clone());
```

**Step 4: Run to green, and the rest of the dimension suite**

```bash
dotnet test Goose.IntegrationTests --filter "FullyQualifiedName~DimensionsScriptTests|FullyQualifiedName~DimensionVendorStockTests|FullyQualifiedName~DimensionDropTests"
```

Expected: all pass. These compile the shipped script, so a csx syntax error surfaces here.

**Step 5: Commit**

```bash
git add Goose/Data/Illutia/Scripts/Global/Dimensions/Npcs.csx Goose.IntegrationTests/DimensionsScriptTests.cs
git commit -m "feat: carry spawn properties into dimension copies"
```

| Invariant | Proved by |
| --- | --- |
| A dimension copy inherits the row's properties | `Dimension_copies_inherit_the_spawn_rows_properties` |
| The copy gets a clone, not the shared instance | `Clone()` in the call; observable as no shared-state path, no separate test |
| Script-created templates still get no properties | existing warden/keeper tests in `DimensionsScriptTests` stay green |

---

## Task 5: Regenerate the Data Editor schema

**Files:**
- Regenerate: `tools/DataEditor/schema.js`

**Mutation impact:**
- Source of truth changed: none; a generated artifact is refreshed from the descriptors.
- Important readers: the Apps Script Data Editor reads `schema.js` for column kinds, widths
  and headers (`Code.gs` writes `plan.width` cells per row).
- Derived/cached state affected: the gitignored `tools/DataEditor/dist/`, rebuilt separately.
- Required propagation sequence: run `SchemaGen` with the `schema.js` argument only. On
  purpose, do **not** pass the second argument — the map editor artifact belongs to Part 2, and
  writing it here would break that repo's column-count pins while Part 1 is still open.
- Invariants to preserve: exactly one block changes, inside `NPC Spawns`
  (`tools/DataEditor/schema.js:1036-1090` today); no other sheet gains or loses a column.
- Observable proof required: a bounded diff, and the Data Editor test suite green.

**Step 1: Regenerate**

```bash
dotnet run --project tools/SchemaGen -- tools/DataEditor/schema.js
```

Expected: `Wrote /.../tools/DataEditor/schema.js (N bytes)`.

**Step 2: Check the diff is confined to NPC Spawns**

```bash
git diff --stat tools/DataEditor/schema.js
git diff tools/DataEditor/schema.js
```

Expected: one hunk, adding the `properties` column object after `map_y`, with `"kind": "Text"`,
`"sql": "TEXT"`, `"default": "''"`, `"required": false`, `"pk": false`, and no `ref`.

**Step 3: Run the Data Editor suite**

```bash
node --test "tools/DataEditor/test/*.test.js"
```

Expected: all pass, 0 failures. The glob is required; the bare directory fails on Node 22 with
`MODULE_NOT_FOUND`.

**Step 4: Rebuild the local (gitignored) bundle**

```bash
node tools/DataEditor/build.mjs
```

Expected: no error. `dist/` is gitignored — do not commit it.

**Step 5: Commit**

```bash
git add tools/DataEditor/schema.js
git commit -m "chore: regenerate the data editor schema for npc_spawns.properties"
```

---

## Task 6: Live worksheet header and documentation

**Files:**
- Modify (live sheet, not in git): `NPC Spawns` E1 of spreadsheet
  `1O2mbze7WGIt2JLeqDctR1zFSL6CdaNhf7iZlaqE4ieU`
- Modify (local-only, gitignored): `.agents/skills/goose-game-data/SKILL.md:124`, `:216`, and
  the `N stationary` note at `:207-209`

**Mutation impact:**
- Source of truth changed: the live worksheet's header row, which the map editor validates
  against the schema on every Pull and Push preflight.
- Important readers: `CsvToSqlConverter.Convert` reads cells positionally (a blank E resolves
  to the SQL default), and the map editor's `SchemaHeaderValidator` compares row 1 against the
  schema columns.
- Derived/cached state affected: none in git. The running server does not see the header until
  its next import, which drops and recreates `npc_spawns`.
- Required propagation sequence: write E1 → verify by reading it back → (later, outside this
  plan) restart the server so the import and `LoadNPCs` run.
- Invariants to preserve: column E is a blank spacer in all 4,358 rows and the two
  `(Don't touch)` helper columns already sit in F and G, so nothing shifts; write with RAW so
  the header stays literal text.
- Observable proof required: reading E1 back returns `properties`, and every data row's E cell
  is still blank.

**Step 1: Write the header**

`gsheets.py` lives in the main checkout (`<repo>/.agents/...`), which is also where its `.gws`
config resolves, so run it from an absolute path rather than from a worktree. Column indices
are 0-based, so E is 4, and row numbering is 1-based.

```bash
python3 - <<'EOF'
import sys
sys.path.insert(0, '/home/hayden/code/illutiagooseserver/.agents/skills/goose-game-data/scripts')
import gsheets
gsheets.write_range('NPC Spawns', 1, 4, ['properties'], 'RAW')
print("wrote E1")
EOF
```

**Step 2: Verify the write and that nothing else moved**

```bash
python3 - <<'EOF'
import sys, json
sys.path.insert(0, '/home/hayden/code/illutiagooseserver/.agents/skills/goose-game-data/scripts')
import gsheets
d = json.loads(gsheets.gws(["values", "get"], {
    "spreadsheetId": gsheets.SPREADSHEET_ID,
    "range": "'NPC Spawns'!A1:G4358",
    "valueRenderOption": "FORMULA"}))
rows = d.get("values", [])
print("header:", rows[0])
assert rows[0][4] == "properties", rows[0]
assert rows[0][5].startswith("NPC Name"), rows[0]
non_blank_e = [i + 1 for i, r in enumerate(rows) if len(r) > 4 and str(r[4]).strip()]
print("non-blank E cells:", non_blank_e)
assert non_blank_e == [1], non_blank_e
print("row count:", len(rows))
EOF
```

Expected: `header: ['npc id', 'map id', 'map x', 'map y', 'properties', "NPC Name (Don't touch)", "Map Name (Don't touch)"]`,
`non-blank E cells: [1]`, `row count: 4358`.

**Step 3: Update the local skill documentation**

`.agents/` is gitignored (`.gitignore:40`), so this edit is local-only and cannot be committed.
Make it in the main checkout, not in the worktree, or it will be lost.

- `SKILL.md:124` — worksheet map: `| NPC Spawns | npc_spawns | 4 | — (composite) |` becomes
  `| NPC Spawns | npc_spawns | 5 | — (composite) |`.
- `SKILL.md:216` — the spawn bullet becomes
  ``- **NPC Spawns** — `A npc id` · `B map id` · `C map x` · `D map y` · `E properties` (JSON; blank = no overrides), the first four required.``
  followed by the key contract: keys are camelCase NPC property names, the only supported key
  is `canMove`, and its value must be the **JSON boolean** `true`/`false` — `1`/`0` is what the
  sheet's Bool columns use and it throws at load, stopping the server.
- `SKILL.md:207-209` — where `N stationary` is described as behaving like a placement property,
  add that a spawn row's `properties` is now the per-placement override mechanism and that
  folding `stationary` into it is a live option.

**Step 4: Hand-off smoke test (needs a running server; not runnable by the implementer)**

Record the outcome rather than claiming it:

1. Set `{"canMove":true}` in `NPC Spawns` E on one row whose NPC template is stationary.
2. Restart the server so the import and `LoadNPCs` run.
3. Observe the mob wandering, and returning when it drifts more than 10 tiles from spawn.
4. Blank the cell, restart, and confirm the mob holds position.
5. Set `{"canMove":` (malformed), restart, and confirm the log names the row's npc id, map id
   and coordinates while the mob still spawns.

---

## Out of scope for Part 1

- Regenerating the map editor's `game-data-schema.json`, and every map editor change: Part 2.
- Any UI for editing properties; JSON validation on any write path.
- `properties` for runtime spawners with no spawn row (`/spawnnpc`, the `/placespawn` item,
  `PlaceSpawnHelper.csx`, `SpawnNPC.csx`, `TestSpell1.csx`, `EasterEvent.csx`).
- The Aspereta workbook (its `DataLinkId` is commented out) — it needs the same E1 write before
  it is used with the new schema.
