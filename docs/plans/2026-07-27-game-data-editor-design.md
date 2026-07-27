# Game Data Editor — Design

Date: 2026-07-27
Branch: `game-data-editor`

## Problem

Game data lives in a Google Sheet, exported as `.xlsx`, converted to SQL by
`CsvToSql.Core`, and loaded into SQLite. Editing works, but:

- **No validation.** A misspelled enum name or an `item_id` pointing at nothing
  either throws mid-import or produces broken data. `/updatesql` fails after the
  fact, in game.
- **Bare ids.** `NPC Drops` is `npc_template_id` + `item_template_id`. Nothing
  tells you what `4471` is.
- **No graphics.** `graphic_tile` is a number. Whether an item or NPC looks right
  can only be checked by importing and logging in.

## Goals

A GUI over the same sheet that validates before publish, resolves ids to names,
and previews graphics for items, spells, NPCs and spell effects.

Non-goals: replacing the sheet, editing the live database, changing the game
server.

## Decisions

| Decision | Choice |
|---|---|
| Source of truth | Google Sheet (unchanged) |
| Hosting | Container-bound Apps Script web app, hosted from the sheet |
| Scope | All 21 worksheets |
| Cross-sheet refs | Searchable typeahead picker writing the id |
| Schema source | Column descriptors in `CsvToSql.Core` |
| `sqlTemplate.sql` | Deleted — DDL generated in memory from descriptors |
| Validation | Blocks saving an invalid record |
| New ids | Auto-suggested `max + 1`, editable |
| Publish | Manual `/updatesql` then `/reloadsql` |
| Field grouping | Editor-side, not in descriptors |
| Editor location | `tools/` in this repo; client repo alongside |

## Architecture

Four components, three in this repo.

### Descriptor layer (`CsvToSql.Core`)

`GetColumns()` returns `Column[]` instead of `string[]`. `CsvToSqlBase` derives
value transformation from the descriptor kind, so the column list and the
escaping logic can no longer disagree — today they are two parallel switch
statements that drift.

```csharp
protected override Column[] GetColumns() => new[] {
    Col.Id("npc_template_id").Ref("NPCs"),
    Col.Id("item_template_id").Ref("Items"),
    Col.Int("stack", def: 1),
    Col.Decimal("droprate"),
};
```

Scalar kinds: `Id`, `Int`, `Decimal`, `Text`, `Bool`, `Enum<T>`, each carrying SQL
width, default, required-ness, and optional `.Ref(sheet)`.

Composite kinds — several columns behind one control:

| Kind | Columns | Control |
|---|---|---|
| `Col.Graphic(tile, file:)` | 2 | Graphic picker with preview |
| `Col.Rgba(r, g, b, a)` | 4 | Colour swatch + blend slider |
| `Col.Bitmask(col, from:)` | 1 | Checkbox list from referenced sheet |
| `Col.IdList(col, ref:)` | 1 | Multi-picker writing a delimited list |
| `Col.EquipSlots(col)` | 1 | Six labelled graphic pickers |

A public registry maps sheet name → table + descriptors. Both the SQL builder and
`SchemaGen` read it; no reflection needed. `CsvToSqlConverter` drops its `dynamic`
dictionary.

`CsvToSqlConverter` builds the whole script in memory: `BEGIN TRANSACTION`, then
per table `DROP TABLE IF EXISTS` / `CREATE TABLE` / indexes / `INSERT`s, then
`COMMIT`. Descriptors gain `.PrimaryKey()` (12 of 21 tables have one) and a
per-table `.Index(col)` (only two exist today:
`npc_vendor_items_npc_template_id_idx`, `map_required_items_map_id_idx`).

Both `sqlTemplate.sql` copies and the `EmbeddedResource` entry are deleted.

### `tools/SchemaGen`

Serialises the registry to `schema.js`: per sheet the table name, and per column
the kind, SQL width, default, required-ness, enum members, FK target sheet, and
graphic role.

### `tools/SpriteBundle`

Reads `../Goose2ClientGodot/Assets/Sprites/manifest.json`, the sheet PNGs, and the
per-part `animations.tres` files. Emits three shelf-packed atlases plus rect
indices, base64-inlined into generated HTML files.

Shelf packing: sort tallest-first, fill a fixed-width row, start a new row when a
sprite will not fit. Measured 95–98% area efficiency, so TexturePacker would add a
binary dependency for no meaningful gain. Palette quantisation is **not** viable —
the icon atlas has 26,179 distinct colours, so a 256-entry palette is lossy.

A checked-in `sheets.json` maps sheet numbers to categories, seeded from the
derivation below and hand-editable to add sheets later.

### `tools/DataEditor`

The Apps Script project: `Code.gs` (sheet I/O), `Editor.html` (UI), plus generated
`schema.js` and the sprite bundles. Deployed via `doGet()` as a web app and via a
sheet menu item. `clasp` is optional — files can be pasted in the Apps Script
editor; the multi-MB bundles are the tedious part and change rarely.

Deployed once per spreadsheet (Illutia and Aspereta), with identical contents.

Nothing new on the game server.

## Sprite bundles

Derived from what the data references, at **sheet** granularity rather than by id
range. `FrameManifestBuilder` builds the manifest from `.adf` files — filename is
the sheet number, each frame's `Index` is the graphic id — and Aspereta graphics
are re-keyed as `700000 + original id` (`AsperetaSheets.GraphicBase`). This
explains why `aspereta-info/spellbookids.txt` documents 110000–110036 while the
data uses 810000–810036: same range, plus the offset. Those notes are therefore
usable as picker labels.

Including whole sheets rather than only used graphics widens the palette for free
(Aspereta: 217 icons in use, 1,894 available in the same sheets).

| Bundle | Contents | PNG | Inlined |
|---|---|---|---|
| Icons | 4,846 sprites from 125 sheets, both datasets | 1.23 MB | 1.64 MB |
| Character parts | 3,261 frames — first frame of `idle-no-equip-down`, `idle-down`, `idle-equip-down`, `mounted-idle-down` for all 1,744 part ids | 1.12 MB | 1.50 MB |
| Effects | 2,412 frames — all ~4 frames of 564 effect animations | 0.60 MB | 0.80 MB |
| **Total** | 10,519 sprites | **2.95 MB** | **3.94 MB** |

Under the ~10 MB Apps Script project ceiling (undocumented; community-measured).
Icons load with the editor; parts and effects load on demand.

Character parts need only idle frames: `AnimationNames.Candidates` shows
`body_state` selects equip-vs-no-equip for idle, and its 4/5/6/7 weapon variants
affect only `attack-*` clips. An attack-pose toggle would later need
`attack-{no-equip,1hand,staff,2hand,bow}-down` for `Bodies` and `Hands` only.

## Data flow

**Read.** Opening a sheet loads its rows into a client-side cache. FK columns lazily
load the referenced sheet's id and name columns.

**Write.** Validate, then write the single row with one `setValues`. **Blank stays
blank** — `CsvToSqlBase` skips empty cells (`if (value.Length == 0) continue`), so a
blank cell means "use the SQL default", not zero. Forms show the default as
placeholder text and only write a cell when the user enters something. Otherwise
opening and saving records would convert thousands of blanks into explicit values,
silently pinning values that were tracking the default.

**Publish.** Manual: `/updatesql` (sheet → DB, already transactional with rollback
in `Events/UpdateSqlCommandEvent.cs`) then `/reloadsql` (DB → in-memory). The
editor shows both commands, and warns on `Maps`, `Classes`, `Class Info`,
`Combinations` and the combination child sheets that those need a **full restart** —
`LoadMaps`, `LoadClasses`, `LoadCombinations` and `LoadNPCs` are commented out in
`Events/ReloadSQLCommandEvent.cs`.

## What each editor displays

The form is not one widget per column; composite kinds collapse groups into single
controls. Field grouping and order live in the editor.

**Items** (46 columns). Sticky preview: inventory icon with tint, plus — when
`graphic_equip > 0` — that graphic rendered on a reference body (fixed default,
changeable via dropdown). Groups: Identity · Requirements (incl. class-restriction
checkboxes) · Stats (stats and five resistances as a grid) · Weapon · Flags · Value ·
Effects (FK pickers) · Scripting.

**Spells** (15 columns). Spellbook icon preview, plus the linked `spell_effect_id`'s
animation as a 4-frame loop. Groups: Identity · Target · Class restrictions · Costs
(static/percent × HP/MP/SP grid).

**NPCs** (58 columns). Composite appearance preview — body, face, hair and six
equipment slots layered in `Character.cs:226-237` order (Hair → Eyes → Chest → Helm
→ Legs → Feet) with `CharacterLayout` underwear fallbacks and per-slot tints,
falling back to the bare body sprite when `body_id >= 100` (`Packets.cs:161` only
sends equipment below that). `equipped_items` is six `graphic,tint` pairs in slot
order Chest, Head, Legs, Feet, Shield, Weapon — graphic ids, not item ids, so no
cross-sheet dependency. Groups: Identity · Appearance · Combat · Behaviour · Regen ·
Links · Scripting.

**Spell Effects** (76 columns). Its own animation and buff icon, plus a full
appearance-override block (`body_id`, `face_id`, `hair_id`, tints) reusing the NPC
composite preview.

The other 17 sheets use the generic generated form.

## Validation and error handling

Client-side from `schema.js`: enum membership, required columns, numeric range for
the SQL width, duplicate ids, FK resolution. Saving is blocked while any field is
invalid, with the failing field highlighted and the offending id named.

Consequence of blocking: parents must be created before children. Empty optional
FKs are valid; only non-empty-and-unresolvable blocks.

Graphic references: `graphic_tile` (items) and `spellbook_graphic` (spells) are
`NOT NULL` with **no default**, so they are mandatory. `graphic_file`,
`graphic_equip`, `spell_animation`, `spell_animation_file`, `buff_graphic` and
`buff_graphic_file` all default to 0, so blank correctly means "none" — no sentinel
handling needed. A non-blank, non-zero graphic must resolve in the bundle.

`SpreadsheetApp` failures surface as a banner with the raw error rather than being
swallowed.

## Testing

**Golden SQL comparison is load-bearing.** Capture the full generated script from
the current working tree *before* any descriptor work, then assert the
descriptor-driven builder reproduces it byte-for-byte. Without this, a subtle
escaping change corrupts data silently.

Note: the working tree currently has uncommitted changes to
`CsvToSql.Core/sqlTemplate.sql` — `npc_templates.face_id` default 70→0 and
`hair_id` 26→0. These are intentional behaviour changes and must be captured in
the descriptors, so the golden file comes from the working tree, not `HEAD`.

Also: unit tests on the descriptor→DDL emitter (defaults, nullability, primary
keys, both indexes), on `SchemaGen` output shape, and a golden check that
`SpriteBundle` rect indices match the source manifest for known `(file, id)` pairs.

The Apps Script UI is untested — no sane harness exists, and the validation rules
worth testing live in `schema.js` generated from tested C#.

## Deferred

- **Parent-centric child editing** — editing an NPC's drops, spawns and vendor items
  as child rows. Explicitly the direction, but after the generic forms work.
- **One-button publish** — `UrlFetchApp` to an authenticated server endpoint running
  both commands. The console-command handler may be a cheaper hook than Kestrel.
- **BIGINT scientific notation.** `min_experience`/`max_experience` are `BIGINT`;
  Sheets may render large values as `1.2E+10`, and the importer reads cells with
  `GetValue<string>()`. Fix is writing numeric cells with an explicit plain-number
  format. The existing sheet should be checked for damage.
- **Concurrent id collisions.** `max + 1` suggested client-side with no locking.
  Mitigation would be a duplicate re-check immediately before writing.
- **`quest_ids` delimiter** — all current values are single ids, so the separator is
  not observable. Confirm from `QuestHandler.cs` before building the widget.
- **`aspereta-info/*.txt` as picker labels** — ~700 human-written names, currently
  unused by anything. Needs `+700000` applied.
- **Fork updates.** A copied sheet inherits the editor code but has its own script
  id, so there is no upgrade path short of re-copying.

## Notes

- `CsvToSql.Console/Program.cs` passes a full export URL into
  `CsvToSqlConverter.Convert(dataLinkId)`, which interpolates it into another URL
  template. That path is broken and its hardcoded sheet id is stale. The server
  path works because it passes a bare id. Worth deleting or fixing while nearby.
- `ItemIDStartpoint` (5000) offsets runtime item *instances*, not template ids, so
  it reserves no template id range.
