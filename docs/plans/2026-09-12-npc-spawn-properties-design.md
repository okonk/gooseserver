# NPC Spawn Properties Design

Paths are relative to `illutiagooseserver` unless written `<godot>/`, which is the
`Goose2ClientGodot` checkout.

## Goal

Add a `properties` text column to each NPC spawn row, holding a properties dictionary
serialised as JSON. The server reads it, stores it on the spawned NPC as `npc.Properties`,
and interprets a small set of known keys as overrides to the NPC that row spawns. The first
key is `canMove`, which makes a mob whose template is stationary wander at that one
placement.

The column is deliberately generic so later keys cost no schema change.

## Scope

- Add the `properties` column to the `NPC Spawns` descriptor, the generated DDL, the
  checked-in `npcs.sql`, and the live worksheet.
- Regenerate both schema artifacts: `tools/DataEditor/schema.js` and
  `<godot>/src/MapEditor.GameData/Schema/game-data-schema.json`.
- Add `NPC.Properties` and the load path that fills it from the spawn row.
- Interpret one key, `canMove`, at load.
- Carry the value through the map editor's pull, edit and push paths so it can never be
  silently dropped.
- Carry it into dimension copies of a spawn row.

Out of scope:

- Any UI for editing the value. It is authored in the sheet or the Data Editor; the map
  editor only preserves it.
- JSON validation on any write path (sheet, Data Editor, map editor). The server's load-time
  log is the only check.
- Plumbing `properties` to runtime spawners that build an NPC from a template with no spawn
  row: GM `/spawnnpc`, the `/placespawn` item, `PlaceSpawnHelper.csx`, `SpawnNPC.csx`,
  `TestSpell1.csx`, `EasterEvent.csx`. They spawn plain template NPCs and get an empty
  `Properties` dictionary.
- Moving the existing `stationary` template column into spawn properties. It is the obvious
  future candidate, and the mechanism now exists for it.

## Data Model

`properties` is the fifth column, appended last because cells are read positionally against
the descriptors.

```csharp
Col.Text("properties", SqlType.Text, "''").HeaderText("properties"),
```

Generated DDL: `properties TEXT DEFAULT '' NOT NULL`. The header is plain `properties`
because the sheet only shows a default in parentheses when there is one to show
(`facing (3)`, `equipped items (0,*,…)`), and other empty-default text columns are bare
(`script params`, `alliance`).

**The empty string is the single representation of "no overrides".** This follows
`npc_alliance` and `script_params`, which are both `TEXT DEFAULT ''`. It also means a blank
cell stays blank through a map editor pull → move → push round trip, with no normalisation
to `{}` on the way. The server treats `NULL`, `''`, and an explicitly typed `{}` identically.

The live `NPC Spawns` worksheet already has the right hole for it: column E is blank in all
4,358 rows, and the two `(Don't touch)` helper columns already sit in F and G, so no helper
column moves. Only E1 changes; every data row stays blank and takes the SQL default.

The Aspereta workbook (`DataLinkId` `1Ig7u4XHc1Vjk4Y1502bwHEVEDba3JTCUcrKwrcOPWyQ`, currently
commented out in `Goose/GooseSettings.json`) has the identical shape — `A`–`D` data, blank
`E`, helpers in `F`/`G` — and needs the same E1 write before it is used with the new schema.
The server tolerates it either way because it reads positionally, but the map editor's
header validator would reject a sheet that is behind the schema.

## Server

### `NPC.Properties`

`NPC.cs` gains a dictionary mirroring `Player.Properties` (`Player.cs:475`):

```csharp
public PropertiesDictionary Properties { get; set; } = new PropertiesDictionary();
```

Always non-null, so no read path needs a null check, and csx scripts get the
`npc.Properties.GetProperty<T>(…)` access they already know from players. Each spawned NPC
gets its own dictionary instance, cloned per dimension copy; nothing shares one.

### Load path

`NPCHandler.LoadNPCs` is the only consumer of `npc_spawns`, so it owns the column's format.
It reads `reader.GetString("properties")` beside the four existing columns and resolves it:

- blank or whitespace → a new empty `PropertiesDictionary`, no log (every row today);
- malformed JSON, or a root that is not an object → one `log.Error` naming npc id, map id
  and coordinates plus the reason, then an empty dictionary and the spawn proceeds.

Keeping this here means `NPC.cs` never sees raw JSON, and it sits beside the loader's
existing row-level reporting (`npc spawns: bad npc id {0}`).

The dictionary is then passed down through optional trailing parameters, so the eight script
call sites that spawn from a template stay byte-identical:

```csharp
public NPC? SpawnNPC(GameWorld world, int mapId, int mapX, int mapY, NPCTemplate template,
                     bool shouldRespawn, PropertiesDictionary? properties = null)
public bool LoadFromTemplate(GameWorld world, int map_id, int map_x, int map_y, NPCTemplate template,
                             bool shouldRespawn, PropertiesDictionary? properties = null)
```

`LoadFromTemplate` sets `this.Properties = properties ?? new PropertiesDictionary();`
alongside the other template copies.

### The `canMove` read

At `NPC.cs:630`, `this.CanMove = template.CanMove;` becomes:

```csharp
this.CanMove = this.Properties.GetProperty("canMove", template.CanMove);
```

The template value is the fallback, so an absent key is a no-op. Applied once at load,
before `this.Spawn(world)` at the end of the method.

Nothing needs reordering to make that work: `CanMove` is consulted only in `OnMoveEvent` at
move-tick time (`NPC.cs:411`), while `Spawn` → `AddMoveEvent` gates on `MoveSpeed > 0`, not
`CanMove`. Respawn (`NPCSpawnEvent` → `npc.Spawn`) reuses the same NPC object and never
re-reads the template, so the override persists for free and no re-application is needed.

What the key concretely buys, and therefore what the smoke test checks:

- `{"canMove":true}` on a stationary template: the NPC wanders, and walks home when it is
  more than 10 tiles from its spawn point (`NPC.cs:411-430`, the `CanMove` branch).
- `{"canMove":false}` on a movable template: it never wanders, and if displaced it walks
  back to its spawn point instead.

### Respawn, reload and scripts

- `/reloadsql` does not reload spawns — `NPC Spawns` is already in the Data Editor's
  `RESTART_ONLY` list — so a properties edit needs a restart. Unchanged behaviour.
- Nothing in this cut writes back to `npc.Properties`. A script may set a key at runtime and
  it survives respawns because the object does, but the key is read **once** at load: setting
  `npc.Properties["canMove"]` after the NPC exists changes the dictionary and never
  `CanMove`. Use `npc.CanMove` for a runtime change.
- No `MigrateDatabaseSchema` entry is added for the column. The import drops and recreates
  `npc_spawns` on every startup while `DataLinkId` is set, which is every environment here,
  so the migration's own rationale — `players` holds live data and is never dropped — does
  not apply. The `npcs.sql` edit covers fresh databases; the uncovered case is an empty
  `DataLinkId` against a database created before this change.

## Dimension copies

`<godot>` is not involved here; this is `Goose/Data/Illutia/Scripts/Global/Dimensions/Npcs.csx`.

The dimension system mirrors every base-map NPC into each dimension by spawning a per-dimension
cloned template at `Map.ID + Offset * dim` (`Npcs.csx:36`). It does not read `npc_spawns`, so
without a change a base spawn row with `{"canMove":true}` would wander while every dimension
copy of the same mob stayed stationary.

That call passes `basic.Properties?.Clone()`, so a dimension copy behaves like the spawn row it
mirrors. A clone rather than the same instance, so the two can never share mutations later.

Verified as the only spawn-row mirror: the warden (`:315`) and rebirth keeper (`:463`) spawn
script-created templates with no backing row and correctly get nothing, the other
`LoadFromTemplate` callers are the out-of-scope runtime spawners listed above, and there is no
`Dimensions` directory under `Goose/Data/Aspereta/Scripts/`.

## Map editor

`<godot>` paths from here.

The schema artifact `src/MapEditor.GameData/Schema/game-data-schema.json` gains the fifth
column, and the repo's reproducibility gate on that file is re-run.

`NpcSpawnRow` (`src/MapEditor.GameData/Rows/GameDataRows.cs:18`) gains a trailing defaulted
field:

```csharp
public readonly record struct NpcSpawnRow(int NpcId, int MapId, int MapX, int MapY, string Properties = "");
```

Non-null with a `""` default, because the mapper resolves a blank cell to the schema default
`''` anyway. That keeps record equality meaning "blank ≡ no overrides", so an untouched
pull → push produces an empty plan and does not report a spurious conflict; it also keeps
every existing construction site source-compatible.

The mapper is the reason the column survives at all. Push rewrites any touched row as
delete + append built from `ToCells`, so a value `ToCells` does not write is gone with no
error:

- `SheetRowMapper.MapSpawn` reads the fifth cell through the existing optional-text path
  (blank → the `''` default);
- `SheetRowMapper.ToCells` writes it into slot 4;
- `EditorClipboard.FromSpawn` and the paste branch of `MapDocumentViewModel.PasteGameData`
  carry it, so a copied spawn keeps its overrides;
- `MapDocumentViewModel.AddSpawnAt` and the paste branch fall to the `""` default, which is
  right for a brand-new spawn.

Not changed: the spawn UI (the `Spawn Properties` panel is untouched, so
`SheetEditSession.UpdateSpawn` still has no caller for this field) and rendering
(`MapCanvas.BuildSpawnMarkers` and `BuildNpcPreviews` read position and npc id only, so the
column is additive there).

## Data editor

No code change. `forms.js` already renders a `Text` column as a single-line input,
`validation.js` accepts any value except one starting with `=` (JSON starts with `{`), and
`Code.gs` pins text-column number format and writes only `plan.width` cells, so F and G stay
untouched and a blank cell stays blank. Only `schema.js` is regenerated.

## Rollout

Order matters, because the worksheet header and the descriptor have to agree before the map
editor is used on this sheet:

1. Write `properties` into `NPC Spawns!E1` with the `gsheets.py`/`gws` helper (RAW writes).
   Safe first: the importer reads positionally, so a fifth header is inert while the
   descriptor still has four columns, and the map editor's header validator walks only schema
   columns, so a trailing extra header is accepted.
2. Land the code in both repos, plus both regenerated schema artifacts.
3. Patch the fixture workbook's E1 header with openpyxl. **Patch, do not re-download**: the
   fixture is a real workbook frozen on 2026-08-25, and re-fetching pulls in six weeks of
   unrelated data drift that would swamp the snapshot review.
4. `GOOSE_UPDATE_SNAPSHOT=1 dotnet test Goose.IntegrationTests --filter FullyQualifiedName~CsvToSqlSnapshot`,
   read the diff, then re-run without the flag.
5. Restart the server and smoke test.

### What the diffs must show

- **Generated artifacts**: exactly one new column block in the NPC Spawns sheet in each file,
  and nothing else changed on any sheet or key.
- **Snapshot**: `npc_spawns` DDL gains `properties TEXT DEFAULT '' NOT NULL`, and every
  `INSERT INTO npc_spawns` gains a fifth value of `''`. No other table's DDL or rows move.
- **Fixture workbook**: exactly one changed cell, E1.

Anything outside those bounds means the positional contract was disturbed, which is the one
thing worth stopping for.

### Docs

- `.agents/skills/goose-game-data/SKILL.md` is what an agent reads before touching spawns:
  update the worksheet map row (`NPC Spawns | npc_spawns | 4` → `5`) and the spawn column
  reference (`A npc id · B map id · C map x · D map y` → append `E properties`). That section
  is also where the key vocabulary has to live, because it has no other author-facing home:
  key names are camelCase NPC property names, this cut supports exactly `canMove`, and the
  value is a **JSON boolean** — not `0`/`1`, which is how the sheet writes its Bool columns.
- The same file's `N stationary` paragraph already notes it "behaves as a property of a
  *placement* rather than of the mob"; cross-reference this mechanism and record folding
  `stationary` into properties as the obvious future move.
- `.agents/` is gitignored (`.gitignore:40`), so this edit is local-only and cannot be
  committed or reviewed in a PR.
- `<godot>/docs/map-editor-google-sheets-setup.md` does not mention `npc_spawns`, so it
  likely needs no change; confirm during implementation rather than assuming.

## Validation

Automated:

- Server root `dotnet test` — `Goose.Tests`, `Tools.Tests` and `Goose.IntegrationTests` are
  all in `Goose.sln`.
- New `Goose.Tests` coverage beside `NPCSpawnRegistrationTests`, which already builds a bare
  `GameWorld` plus map and injected class and calls `SpawnNPC`: stationary template with
  `{"canMove":true}` → `npc.CanMove` is true; empty dictionary → false; `{"canMove":false}`
  on a movable template → false. Plus parse tolerance: blank, malformed, and a JSON array
  root.
- Data editor: `node --test "tools/DataEditor/test/*.test.js"` (the glob matters — the bare
  directory trips `MODULE_NOT_FOUND` on Node 22), then `node tools/DataEditor/build.mjs` to
  rebuild the gitignored `dist/`.
- SchemaGen:
  `dotnet run --project tools/SchemaGen -- tools/DataEditor/schema.js <godot>/src/MapEditor.GameData/Schema/game-data-schema.json`,
  then the clean-tree gate on the artifact.
- Map editor `dotnet test`: `MapEditor.GameData.Tests`, `MapEditor.GameData.Google.Tests`,
  `MapEditor.App.Tests`. Updates needed: the NPC Spawns column count in
  `GameDataSchemaTests`, its column metadata assertions, the twelve `'NPC Spawns'!A1:D`
  range literals (`GoogleSheetsGatewayTests`, `GoogleSheetsTransportIntegrationTests`), and
  the `SheetRowMapperTests` round-trip count plus `MapSpawn_WithExtraCells_IgnoresThem`,
  whose premise changes now that the fifth cell is read. New coverage: a moved spawn keeps
  its properties through the delete + insert path, a blank cell and `""` compare equal so an
  untouched push is empty, and the clipboard round trip.

Manual smoke test:

- `{"canMove":true}` on one stationary mob's spawn row → restart → it wanders and returns.
- Blank it → restart → it holds position.
- `{"canMove":` → restart → the malformed log names the row and the mob still spawns.

## Accepted risks and deferred work

- **A wrongly-typed value takes down startup.** `{"canMove":"yes"}` is valid JSON, so the
  parse guard passes it, and `PropertiesDictionary.ConvertValue` has no numeric→`bool` path
  (a `bool` is neither numeric nor an enum), so `GetProperty<bool>` throws
  `InvalidCastException` inside `LoadFromTemplate`, outside any try/catch. It propagates out
  of `LoadNPCs` and kills startup or `/reloadsql`. This is accepted deliberately: the throw
  stays, with no row-context logging and no coercion around it. Note how likely it is —
  every Bool column in this workbook is written `0`/`1`, so `{"canMove":1}` is the mistake
  authors will actually make, and `true`/`false` is the only accepted form.
- **No JSON validation anywhere on the write path.** The sheet, the Data Editor and the map
  editor all accept broken JSON and round-trip it faithfully. The load-time log is the only
  check, so the editor can preserve a value that stops the server.
- **No way to set the value from the map editor.** Properties are authored in the sheet or
  the Data Editor; the editor preserves what it reads. The feature cannot be exercised
  end-to-end from the map editor alone in this cut.
- **Manual sheet edits surface as push conflicts.** `ReplacementPlanner` matches remote rows
  by value equality, so editing a `properties` cell in the sheet while the editor holds a
  stale copy raises the existing conflict dialog. Unchanged mechanism, considered correct.
- **`{"canMove":true}` cannot be authored in a spreadsheet cell typed as `=…`** — the Data
  Editor refuses a leading `=` for the same reason it refuses it anywhere. Not a practical
  constraint for JSON.
- **The map editor normalises nothing, so `""` and `{}` both remain possible in the sheet.**
  Both mean "no overrides" to the server. Accepted rather than unifying them on write.
- **Key vocabulary is one string literal in `NPC.cs`.** With one key it does not warrant a
  registry; a second or third key is the point at which the list should move somewhere
  greppable.
