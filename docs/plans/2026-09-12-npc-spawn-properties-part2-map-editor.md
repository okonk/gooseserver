# NPC Spawn Properties — Part 2 (Map Editor Pass-Through) Implementation Plan

**Goal:** Make the Goose2ClientGodot map editor read, preserve and write the new
`npc_spawns.properties` column, so a value authored in the sheet survives a pull → edit →
push round trip and is never silently dropped.

**Architecture:** The map editor's schema artifact currently declares four `NPC Spawns`
columns; regenerate it from the server descriptors, then carry the value on `NpcSpawnRow`.
Every write path rebuilds a touched row from `SheetRowMapper.ToCells` and appends it
(`GoogleBatchBuilder` emits only `DeleteDimension` + `AppendCells`), so a column the mapper
does not write is lost without any error — the mapper pair plus the clipboard are the whole
risk surface.

**Tech Stack:** C# / .NET 10, Avalonia, xUnit, Google Sheets batchUpdate API,
`tools/SchemaGen` (server repo) for the schema artifact.

**Design doc:** `docs/plans/2026-09-12-npc-spawn-properties-design.md` in the server repo.

**Depends on Part 1** (`-part1-server.md`) for the descriptor change that regenerates this
repo's schema artifact, and on the `NPC Spawns!E1` header being live in the worksheet.

---

## APIs verified

| API | Citation |
| --- | --- |
| `NpcSpawnRow` record | `src/MapEditor.GameData/Rows/GameDataRows.cs:18` |
| Mapper field declarations for spawns | `src/MapEditor.GameData/Rows/SheetRowMapper.cs:33-36` |
| Mapper resolves fields by name (`GetRequiredColumn` throws when absent) | `SheetRowMapper.cs:49`, `:157-161` |
| `MapSpawn` | `src/MapEditor.GameData/Rows/SheetRowMapper.cs:115-123` |
| `ToCells(NpcSpawnRow)` | `src/MapEditor.GameData/Rows/SheetRowMapper.cs:136-144` |
| `ReadText` / blank → schema default | `SheetRowMapper.cs:189-202`, `:219-241` |
| `Cell` returns null past the end | `SheetRowMapper.cs:164-165` |
| Push writes `DeleteDimension` + `AppendCells` only | `src/MapEditor.GameData.Google/Sheets/GoogleBatchBuilder.cs:36-76` |
| Trailing empty cells omitted from `AppendCells` | `GoogleBatchBuilder.cs:55-71` |
| Range derived from `schema.Columns.Count` | `src/MapEditor.GameData.Google/Sheets/GoogleSheetsGateway.cs:374-381`, `:426-441` |
| `EditorClipboardPayload.FromSpawn(int, string)` | `src/MapEditor.App/ViewModels/EditorClipboard.cs:47-48` |
| Copy / cut sites | `src/MapEditor.App/ViewModels/MapDocumentViewModel.cs:607`, `:637` |
| New-spawn site | `src/MapEditor.App/ViewModels/MapDocumentViewModel.cs:1045` |
| Paste site | `src/MapEditor.App/ViewModels/MapDocumentViewModel.cs:1268` |
| `SheetEditSession.MoveSpawn` uses `with` | `src/MapEditor.GameData/Editing/SheetEditSession.cs:72` |
| Planner matches remote rows by value | `src/MapEditor.GameData/Replacement/ReplacementPlanner.cs:59-68` |
| Test `Row(...)` cell-array helper | `tests/MapEditor.GameData.Tests/Rows/SheetRowMapperTests.cs:621-629` |
| Seven inline 4-column spawn schemas to update | `SheetRowMapperTests.cs:117`, `:382`, `:440`, `:494`, `:551`, `:593`; `tests/MapEditor.GameData.Tests/Replacement/ReplacementPlannerTests.cs:455` |
| Schema pins | `tests/MapEditor.GameData.Tests/Schema/GameDataSchemaTests.cs:71`, `:88` |
| Range literals to update | `tests/MapEditor.GameData.Google.Tests/Sheets/GoogleSheetsGatewayTests.cs:95,117,164,188,209,265,288,352,393,425,452`; `tests/MapEditor.GameData.Google.Tests/Sheets/GoogleSheetsTransportIntegrationTests.cs:85` |
| Spawn cell expectations in the planner tests | `ReplacementPlannerTests.cs:58`, `:76-77`, `:131`, `:174-176` (warp expectations at `:254`, `:272-273`, `:327`, `:369-372` are unchanged) |

---

## Task 1: Regenerate the schema artifact and update the pins

**Files:**
- Regenerate: `src/MapEditor.GameData/Schema/game-data-schema.json`
- Modify: `tests/MapEditor.GameData.Tests/Schema/GameDataSchemaTests.cs:71`, `:88`
- Modify: the twelve `'NPC Spawns'!A1:D` literals listed above

**Mutation impact:**
- Source of truth changed: none; the artifact is regenerated from the server descriptors.
- Important readers: `GameDataSchema.LoadEmbedded` (`src/MapEditor.GameData/Schema/GameDataSchema.cs:12`),
  `SheetRowMapper`'s `Resolve` calls, `GoogleSheetsGateway.BuildRange`, and
  `SchemaHeaderValidator` at `GoogleSheetsGateway.cs:410-420`.
- Derived/cached state affected: every `'NPC Spawns'!A1:*` range string, because it is derived
  from `schema.Columns.Count`.
- Required propagation sequence:
  1. Regenerate the artifact with the second `SchemaGen` argument.
  2. Update the two schema pins and the twelve range literals so the suite compiles and passes.
- Invariants to preserve: the artifact stays a generated file — never hand-edit it beyond the
  regenerated output; the four consumed sheets and their order do not change.
- Observable proof required: `dotnet test` green in this repo, and a bounded diff containing
  only the new column block inside `NPC Spawns`.

**Prerequisite:** the Part 1 descriptor change is committed. The live worksheet's
`NPC Spawns!E1` reading `properties` is a prerequisite only for the hand-off smoke test in
Task 4, not for the suite: the unit tests load the embedded artifact and fakes, and
`SchemaHeaderValidator` — which compares schema headers against row 1 — only runs against the
real sheet.

**Step 1: Regenerate the artifact**

`SchemaGen` lives in the server repo, so run it from the server worktree with an absolute
target path into this repo's worktree.

```bash
cd /home/hayden/code/illutiagooseserver/.worktrees/npc-spawn-properties
dotnet run --project tools/SchemaGen -- tools/DataEditor/schema.js \
  /home/hayden/code/Goose2ClientGodot/.worktrees/npc-spawn-properties/src/MapEditor.GameData/Schema/game-data-schema.json
```

Passing the second argument rewrites `tools/DataEditor/schema.js` as well. Part 1 already
regenerated and committed that file, so it must come out byte-identical here — check with
`git status` in the server worktree and expect no change to it.

**Step 2: Run the suite and collect the failures (red)**

```bash
cd /home/hayden/code/Goose2ClientGodot/.worktrees/npc-spawn-properties
dotnet test Goose2ClientGodot.sln
```

Expected: `GameDataSchemaTests.GetRequiredSheet_ReturnsSheetByOrdinalName` fails on
`Assert.Equal(4, sheet.Columns.Count)`, the `'NPC Spawns'!A1:D` literals fail, and
`GetColumnIndex_ReturnsPositionalIndex` still passes (it only checks `npc_id` and `map_y`).
The mapper tests will also fail with `KeyNotFoundException` for `properties` — that is Task 2's
work, so if you prefer a green boundary here, fold Step 3 and Task 2's inline-schema edit
together and commit once.

**Step 3: Update the pins and range literals**

- `GameDataSchemaTests.cs:71`: `Assert.Equal(4, sheet.Columns.Count)` becomes
  `Assert.Equal(5, sheet.Columns.Count)`.
- `GameDataSchemaTests.cs:88`: after the existing `map_y` assertion, add
  `Assert.Equal(4, sheet.GetColumnIndex("properties"));`.
- Add the column's metadata to `LoadEmbedded_ContainsExactColumnMetadata` (`:25-58`), which
  currently covers `NPCs`, `Warptiles` and `Maps` but not spawns:

```csharp
var properties = schema.GetRequiredSheet("NPC Spawns").GetRequiredColumn("properties");
Assert.Equal("properties", properties.Header);
Assert.Equal("Text", properties.Kind);
Assert.Equal("TEXT", properties.Sql);
Assert.Equal("''", properties.Default);
Assert.False(properties.Required);
Assert.False(properties.IsPrimaryKey);
Assert.Null(properties.RefSheet);
```

- Replace every `'NPC Spawns'!A1:D` with `'NPC Spawns'!A1:E` in the two Google test files.

**Step 4: Confirm the diff is confined**

```bash
git diff src/MapEditor.GameData/Schema/game-data-schema.json
```

Expected: one hunk inside the `NPC Spawns` sheet, adding
`{"name":"properties","header":"properties","kind":"Text","sql":"TEXT","default":"''","required":false,"pk":false}`.
No other sheet, key or ordering change.

**Step 5: Commit**

```bash
git add src/MapEditor.GameData/Schema/game-data-schema.json \
        tests/MapEditor.GameData.Tests/Schema/GameDataSchemaTests.cs \
        tests/MapEditor.GameData.Google.Tests/Sheets/GoogleSheetsGatewayTests.cs \
        tests/MapEditor.GameData.Google.Tests/Sheets/GoogleSheetsTransportIntegrationTests.cs
git commit -m "chore: regenerate the game data schema for npc_spawns.properties"
```

---

## Task 2: `NpcSpawnRow.Properties` and the mapper pair

**Files:**
- Modify: `src/MapEditor.GameData/Rows/GameDataRows.cs:18`
- Modify: `src/MapEditor.GameData/Rows/SheetRowMapper.cs` (field `:33-36`, ctor `:72-75`,
  `MapSpawn` `:115-123`, `ToCells` `:136-144`)
- Modify: the seven inline spawn schemas in the tests
- Test: `tests/MapEditor.GameData.Tests/Rows/SheetRowMapperTests.cs`

**Mutation impact:**
- Source of truth changed: `NpcSpawnRow` gains a value that participates in record equality.
- Important readers: `ReplacementPlanner` buckets remote rows by `NpcSpawnRow` value
  (`ReplacementPlanner.cs:59-68`), `SnapshotComparer` / `GameDataSnapshots` compare by
  `GetHashCode`, `SheetEditSession.RowsEqual`, `EditorClipboard`, and `ToCells` itself.
- Derived/cached state affected: pulled-row equality. If the mapper's blank resolution does not
  match the record's default, every remote row mismatches its local copy and each push
  degenerates into delete-everything + re-append-everything — blanking the column wholesale
  and raising a spurious conflict dialog.
- Required propagation sequence:
  1. Record gains `string Properties = ""`.
  2. Mapper resolves the field, reads it in `MapSpawn`, writes it in `ToCells`.
  3. Every inline test schema declares the column, or the mapper constructor throws.
- Invariants to preserve: a blank cell and `""` are the same value; existing construction sites
  keep compiling; a value written by `ToCells` reads back identically through `MapSpawn`.
- Observable proof required: a non-empty properties value survives `ToCells` → `MapSpawn`, and
  a blank cell maps to exactly the record default.

**Step 1: Write the failing tests**

Add to `tests/MapEditor.GameData.Tests/Rows/SheetRowMapperTests.cs`, using the existing
`Row(...)` helper (`:621-629`).

```csharp
[Fact]
public void MapSpawn_BlankPropertiesCell_YieldsTheRecordDefault()
{
    var schema = GameDataSchema.LoadEmbedded();
    var mapper = new SheetRowMapper(schema);
    var spawns = schema.GetRequiredSheet("NPC Spawns");

    var cells = Row(spawns, ("npc_id", "7"), ("map_id", "1"), ("map_x", "2"), ("map_y", "3"));

    Assert.Equal(new NpcSpawnRow(7, 1, 1, 2), mapper.MapSpawn(cells));
    Assert.Equal(string.Empty, mapper.MapSpawn(cells).Properties);
}

[Fact]
public void ToCells_Spawn_WritesAndRoundTripsProperties()
{
    var schema = GameDataSchema.LoadEmbedded();
    var mapper = new SheetRowMapper(schema);
    var spawns = schema.GetRequiredSheet("NPC Spawns");

    var row = new NpcSpawnRow(7, 1, 2, 3, "{\"canMove\":true}");
    var cells = mapper.ToCells(row);

    Assert.Equal("{\"canMove\":true}", cells[spawns.GetColumnIndex("properties")]);
    Assert.Equal(row, mapper.MapSpawn(cells));
}
```

**Step 2: Run and confirm they fail (red)**

```bash
dotnet test tests/MapEditor.GameData.Tests --filter FullyQualifiedName~SheetRowMapperTests
```

Expected at this point: the whole fixture fails to construct because the inline spawn schemas
have no `properties` column (`KeyNotFoundException` from `GetRequiredColumn`), and the file does
not compile until `NpcSpawnRow` has five members. Both are the work of this task.

**Step 3: Add the field and the mapper read/write**

```csharp
public readonly record struct NpcSpawnRow(int NpcId, int MapId, int MapX, int MapY,
                                          string Properties = "");
```

In `SheetRowMapper`, declare `_spawnProperties` beside the other spawn fields (`:33-36`),
resolve it in the constructor next to `_spawnMapY`, and:

```csharp
public NpcSpawnRow MapSpawn(IReadOnlyList<string?> cells)
{
    // Sheet coordinates are 1-indexed; tile coordinates are 0-indexed.
    return new NpcSpawnRow(
        ReadInt(_spawnNpcId, cells),
        ReadInt(_spawnMapId, cells),
        ReadInt(_spawnMapX, cells) - 1,
        ReadInt(_spawnMapY, cells) - 1,
        ReadText(_spawnProperties, cells));
}

public IReadOnlyList<string?> ToCells(NpcSpawnRow row)
{
    var cells = new string?[_spawnNpcId.Sheet.Columns.Count];
    cells[_spawnNpcId.Index] = Format(row.NpcId);
    cells[_spawnMapId.Index] = Format(row.MapId);
    cells[_spawnMapX.Index] = Format(row.MapX + 1);
    cells[_spawnMapY.Index] = Format(row.MapY + 1);
    cells[_spawnProperties.Index] = row.Properties;
    return new ReadOnlyCollection<string?>(cells);
}
```

`ReadText` returns the schema default — `""` — for a blank cell, which is what makes blank and
`""` the same value. `ToCells` writing `""` is deliberate: `GoogleBatchBuilder` drops trailing
empty cells (`GoogleBatchBuilder.cs:55-71`), so the appended row keeps a blank cell.

**Step 4: Add `properties` to the seven inline schemas**

Each is a 4-column `npc_spawns` object. Append, matching those files' bare-JSON-boolean style:

```json
{"name":"properties","header":"props","kind":"Text","sql":"TEXT","default":"''","required":false,"pk":false}
```

The `"default":"''"` matters: without a default, `ParseTextDefault` throws for a blank cell on
a non-required column (`SheetRowMapper.cs:219-224`).

**Step 5: Run to green**

```bash
dotnet test tests/MapEditor.GameData.Tests --filter FullyQualifiedName~SheetRowMapperTests
```

Expected: all pass. `MapSpawn_WithExtraCells_IgnoresThem` (`:281-293`) stays green unchanged —
`Row(...)` now returns five cells, the fifth blank maps to `""`, and the two junk cells still
sit beyond the schema width, so its premise is still exactly right.
`ToCells_Spawn_PreservesValuesAndRoundTrips` (`:298-313`) also stays green.

**Step 6: Commit**

```bash
git add src/MapEditor.GameData/Rows/GameDataRows.cs src/MapEditor.GameData/Rows/SheetRowMapper.cs \
        tests/MapEditor.GameData.Tests/Rows/SheetRowMapperTests.cs \
        tests/MapEditor.GameData.Tests/Replacement/ReplacementPlannerTests.cs
git commit -m "feat: carry npc spawn properties through the row mapper"
```

| Invariant | Proved by |
| --- | --- |
| A non-empty value survives the round trip | `ToCells_Spawn_WritesAndRoundTripsProperties` (adversarial: it fails if either mapper half is missed) |
| A blank cell equals the record default | `MapSpawn_BlankPropertiesCell_YieldsTheRecordDefault` |
| The mapper still tolerates cells beyond the schema width | `MapSpawn_WithExtraCells_IgnoresThem` (unchanged) |
| Every schema the mapper loads declares the column | all seven inline schemas updated, or the fixture throws |

---

## Task 3: Clipboard and new-spawn defaults

**Files:**
- Modify: `src/MapEditor.App/ViewModels/EditorClipboard.cs:9-48`
- Modify: `src/MapEditor.App/ViewModels/MapDocumentViewModel.cs:607`, `:637`, `:1268`
- Test: `tests/MapEditor.App.Tests/GameDataClipboardTests.cs`

**Mutation impact:**
- Source of truth changed: the clipboard payload's spawn branch carries a properties value.
- Important readers: `MapDocumentViewModel.PasteGameData` (`:1260-1271`) constructs the pasted
  row; `AddSpawnAt` (`:1045`) constructs a brand-new row.
- Derived/cached state affected: no derived state.
- Required propagation sequence: copy/cut put `row.Properties` in the payload; paste passes
  `payload.SpawnProperties` into the new `NpcSpawnRow`; a brand-new spawn takes the record
  default `""`.
- Invariants to preserve: a pasted spawn keeps the properties of the row it was copied from; a
  freshly placed spawn is blank; tile and warp payload branches are untouched.
- Observable proof required: after copy + paste at a new tile, the pasted row's `Properties`
  equals the source's, and a newly added spawn's is `""`.

**Step 1: Write the failing test**

Add to `tests/MapEditor.App.Tests/GameDataClipboardTests.cs`, following the existing
copy/paste test shape in that file (it drives `CopySelection` / paste through the view model's
public methods).

```csharp
[Fact]
public void A_pasted_spawn_keeps_the_properties_of_the_row_it_was_copied_from()
{
    // arrange: a session with one spawn carrying properties, selected, on the spawn tool
    // act: CopySelection(), then paste at a different tile
    // assert: the new row has the same Properties as the source row
}

[Fact]
public void A_newly_placed_spawn_has_no_properties()
{
    // act: AddSpawnAt(npcId, x, y)
    // assert: the added row's Properties is string.Empty
}
```

Fill both bodies using the same fixture the neighbouring tests use; the assertions are the
point, not the arrangement.

**Step 2: Run and confirm they fail (red)**

```bash
dotnet test tests/MapEditor.App.Tests --filter FullyQualifiedName~GameDataClipboardTests
```

Expected: both fail to compile (`EditorClipboardPayload` has no spawn properties member), then
the paste test fails on the value once it compiles.

**Step 3: Extend the payload and the call sites**

`EditorClipboardPayload` gains `SpawnProperties` alongside `SpawnNpcId`, threaded through the
private constructor, and `FromSpawn` takes it:

```csharp
public static EditorClipboardPayload FromSpawn(int npcId, string properties, string sourceSpreadsheetId)
    => new(EditorClipboardKind.Spawn, null, npcId, properties, null, null, null, sourceSpreadsheetId);
```

Update the two copy/cut call sites to pass `row.Properties`, the tile and warp factories to
pass `null` for the new parameter, and the paste site to build
`new NpcSpawnRow(payload.SpawnNpcId!.Value, session.MapId, x, y, payload.SpawnProperties ?? string.Empty)`.

`AddSpawnAt` (`:1045`) keeps its four-argument construction, which now means blank properties.

**Step 4: Run to green**

```bash
dotnet test tests/MapEditor.App.Tests
```

Expected: all pass.

**Step 5: Commit**

```bash
git add src/MapEditor.App/ViewModels/EditorClipboard.cs src/MapEditor.App/ViewModels/MapDocumentViewModel.cs \
        tests/MapEditor.App.Tests/GameDataClipboardTests.cs
git commit -m "feat: carry spawn properties through copy and paste"
```

| Invariant | Proved by |
| --- | --- |
| A pasted spawn keeps its properties | `A_pasted_spawn_keeps_the_properties_of_the_row_it_was_copied_from` |
| A new spawn is blank | `A_newly_placed_spawn_has_no_properties` |
| Tile and warp clipboard branches are unaffected | the rest of `GameDataClipboardTests` |

---

## Task 4: Push-path proof and end-to-end hand-off

**Files:**
- Modify: `tests/MapEditor.GameData.Tests/Replacement/ReplacementPlannerTests.cs:58`, `:76-77`, `:131`, `:174-176`

**Mutation impact:**
- Source of truth changed: none; this task proves the mutation path end to end.
- Important readers: `GoogleBatchBuilder` turns `Inserts[].CellValues` into `AppendCells` rows.
- Derived/cached state affected: none.
- Required propagation sequence: a moved spawn is deleted and re-appended from `ToCells`, so the
  value must appear in the insert's cells — this is the only path by which the editor writes it.
- Invariants to preserve: a row the user never touches is never rewritten, so its value is
  untouched; a row the user moves or re-assigns keeps its value.
- Observable proof required: the insert cells for a moved spawn contain the properties value,
  and a properties-only difference is what makes two otherwise identical rows different.

**Step 1: Update the four spawn cell expectations**

Append `""` as the fifth element (the trailing cell `GoogleBatchBuilder` then omits):

```csharp
Assert.Equal(new[] { "1", "5", "100", "100", "" }, plan.Inserts[0].CellValues);
```

Sites: `:58`, `:76-77`, `:131`, `:174-176`. The warp expectations at `:254`, `:272-273`, `:327`
and `:369-372` are six-cell and unchanged. The assertion at `:204-211` derives its length from
`spawns.Columns.Count` and needs no change.

**Step 2: Add the round-trip proof**

```csharp
[Fact]
public void PlanSpawn_MovedRow_KeepsItsPropertiesInTheInsert()
{
    var planner = Planner();
    var remote = new[] { new RemoteRow<NpcSpawnRow>(2, new NpcSpawnRow(1, 5, 10, 11, "{\"canMove\":true}")) };
    var desired = new[] { new NpcSpawnRow(1, 5, 12, 13, "{\"canMove\":true}") };

    var plan = planner.PlanSpawnReplacement(remote, desired, 5);

    Assert.Equal(new RowDelete(2), Assert.Single(plan.Deletes));
    Assert.Equal(new[] { "1", "5", "13", "14", "{\"canMove\":true}" },
        Assert.Single(plan.Inserts).CellValues);
}

[Fact]
public void PlanSpawn_SamePositionDifferentProperties_YieldsDeleteAndInsert()
{
    var planner = Planner();
    var remote = new[] { new RemoteRow<NpcSpawnRow>(2, new NpcSpawnRow(1, 5, 10, 11)) };
    var desired = new[] { new NpcSpawnRow(1, 5, 10, 11, "{\"canMove\":true}") };

    var plan = planner.PlanSpawnReplacement(remote, desired, 5);

    Assert.Equal(new RowDelete(2), Assert.Single(plan.Deletes));
    Assert.Single(plan.Inserts);
}
```

The second test records deliberate behaviour rather than guarding a bug: properties participate
in row equality, so a properties cell edited by hand in the sheet while the editor holds a stale
copy surfaces as the existing push conflict.

**Step 3: Run the whole repo suite**

```bash
cd /home/hayden/code/Goose2ClientGodot/.worktrees/npc-spawn-properties
dotnet test Goose2ClientGodot.sln
```

Expected: all projects pass, 0 failures. Baseline before this work was exit 0 with
`MapEditor.App.Tests` at 759 passed among the others.

**Step 4: Commit**

```bash
git add tests/MapEditor.GameData.Tests/Replacement/ReplacementPlannerTests.cs
git commit -m "test: prove spawn properties survive the push path"
```

**Step 5: End-to-end hand-off (needs credentials and both halves; not runnable offline)**

Record the outcome rather than claiming success:

1. In the live sheet, set `{"canMove":true}` in `NPC Spawns` E on one stationary mob's row.
2. In the map editor, pull that map, move the spawn one tile, push.
3. Re-read the sheet: the row is at the new tile **and** E still holds `{"canMove":true}`.
4. Place a brand-new spawn and push: its E cell is blank.
5. Pull a map with a value already in E, move an unrelated spawn on it, push: the untouched
   row's E is unchanged.
6. Then re-run the Part 1 server smoke test (restart and watch the mob wander) now that the
   sheet round-trips cleanly.

| Invariant | Proved by |
| --- | --- |
| A moved spawn's insert carries its properties | `PlanSpawn_MovedRow_KeepsItsPropertiesInTheInsert` (adversarial: fails if `ToCells` omits the cell) |
| Properties participate in row identity | `PlanSpawn_SamePositionDifferentProperties_YieldsDeleteAndInsert` |
| Untouched rows are never rewritten | the planner emits inserts only for desired rows that are absent from remote; existing reorder/no-op tests stay green |
| A blank cell and `""` compare equal, so a pull→push is a no-op | `MapSpawn_BlankPropertiesCell_YieldsTheRecordDefault` plus `PlanSpawn_ReorderedNoOp_YieldsEmptyOperations` |
| The column survives a real push | hand-off Step 3 |

---

## Out of scope for Part 2

- Any UI for editing a spawn's properties: the `Spawn Properties` panel is untouched, so
  `SheetEditSession.UpdateSpawn` still has no caller for this field.
- JSON validation on pull or push. `GameDataValidator.ValidateSpawns` keeps checking only NPC
  existence and bounds, so the editor will round-trip broken JSON that stops the server.
- Rendering: `MapCanvas.BuildSpawnMarkers` and `BuildNpcPreviews` read position and npc id only.
