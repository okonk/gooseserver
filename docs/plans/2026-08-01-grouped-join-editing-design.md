# Grouped Join-Table Editing — Design

Date: 2026-08-01
Branch: `grouped-join-editing`

## Problem

The data editor lists one record per sheet row. On the join sheets that is a list of
bare id pairs — `1 — 1`, `1 — 2`, `1 — 4471` — and editing an NPC's drops means
opening each row separately, with no view of the set.

## Goal

For join sheets, list one entry per parent (`1 — Mouse`) and edit all of that parent's
child rows in one table, saved together.

Non-goal for this design: the parent-centric editor where an NPC row and its spawns,
drops and vendor items are edited on one screen. That is the eventual target, and the
decisions here are made so it becomes an embed rather than a rewrite — but it is not
built here.

## Decisions

| Decision | Choice |
|---|---|
| Scope | Grouped child editing only |
| Group layout | Inline table, one row per child record |
| Removing a row | Real sheet delete (`deleteRows`), sheet shifts up |
| Parent column config | Editor-side table in `layout.js` |
| NPC Spawns grouping | By `map_id` |
| Class Info | Not grouped — keeps today's flat form |
| Empty parents | Not listed; reached via a "New group" parent picker |
| Save | One batched server call, pre-validated, under a document lock |
| Save shape | Multi-sheet from day one (`saveBatch`) |
| Flat view | Replaced for grouped sheets, not retained |
| Duplicate child rows | Warn, do not block |

## Grouping model

`Layout` gains a frozen `GROUP_PARENT` table and `Layout.groupParent(sheet)`, returning
the parent column name or `null`:

| Sheet | Parent column | Parent sheet |
|---|---|---|
| NPC Drops | `npc_template_id` | NPCs |
| NPC Vendor Items | `npc_template_id` | NPCs |
| NPC Spawns | `map_id` | Maps |
| Warptiles | `map_id` | Maps |
| Map Required Items | `map_id` | Maps |
| Quest Reqs | `quest_id` | Quests |
| Quest Rewards | `quest_id` | Quests |
| Combination Item Required | `combination_id` | Combinations |
| Combination Item Result | `combination_id` | Combinations |
| Class Levelup Spells | `class_id` | Classes |

Only the column is named. The parent *sheet* comes from that column's existing `ref` in
`GOOSE_SCHEMA`, so the two cannot drift. A sheet absent from the table is untouched.

Grouping is client-side and needs no new read call: `openSheet` already reads the whole
sheet, and `loadReferencedSheets` already fetches every referenced sheet's id+name list
for the FK pickers — which for a grouped sheet includes the parent sheet. The parent
list is a reduce over `state.rows` keyed by the parent cell, joined to
`state.pickerData[ref]` for labels, ordered by parent id numerically.

None of the ten grouped sheets has a composite (only Items, NPCs, Quests, Spells, Spell
Effects and Combinations do), so every cell in a group table is a plain scalar or an FK.

### Rows with no resolvable parent

A child row whose parent id is not in the parent sheet would become invisible under
grouping. Instead it gets its own group, labelled `4471 — (unknown NPC)`, sorted last.
Rows with a blank parent cell collect into one `(no parent)` group. Both are editable
and deletable: this is dead data you want to find, not hide.

## The group panel

Choosing a grouped sheet renders the parent list into `#records` — one button per group,
`1 — Mouse (3)` — above a **New group** button. That button opens a typeahead over the
parent sheet listing *all* parents; picking one that already has a group jumps to it, so
there is a single way to reach any parent.

Choosing a group renders into `#form`:

```
NPC 1 — Mouse                                    3 rows

  Item                    Stack     Droprate
  [Cheese            ▾]   [1    ]   [0.25  ]   [×]
  [Rat Tail          ▾]   [1    ]   [0.10  ]   [×]
  [Gold Coin         ▾]   [5    ]   [1.00  ]   [×]

  [+ Add drop]                        [Save group]
```

- Table columns are `schema.columns` minus the parent column, which is implied by the
  group and written automatically.
- Each cell is `Forms.columnControl({ column, ctx, sheet, values, effective })`. FK cells
  get the same typeahead as the single-record form; enums the same select.
- Quest Reqs and Quest Rewards carry their own `id` pk. It renders read-only; new rows
  draw from `Validation.nextId` over the whole sheet's ids, incrementing locally so
  several new rows in one save do not collide.
- `×` removes the row from the panel and records its original row number in a pending
  delete list. `+ Add` appends a blank row model with row number 0.
- Row order within a group is sheet order.

`app.js` branches once, in `openSheet`: if `Layout.groupParent(sheet)` is set, hand off
to `Groups` instead of `renderList`. `Groups` renders into containers passed in, not
fixed ids, so the eventual parent-centric editor mounts several of them inside the NPC
form with no rewrite.

The header `#save` button hides in group mode — the panel owns its Save. `#new-record`
reads "New group" and opens the parent picker.

## Save protocol

One server entry point, `saveBatch(ops)`, taking a list of per-sheet op-sets:

```js
saveBatch([
  { sheet: 'NPCs',      idColumnIndex: 0,  textColumns: [...],
    writes: [...], appends: [...], deletes: [...] },
  { sheet: 'NPC Drops', idColumnIndex: -1, textColumns: [...],
    writes:  [{ row: 12, cells: [...], loaded: [...] }],
    appends: [{ cells: [...] }],
    deletes: [{ row: 15, loaded: [...] }] },
])
```

A group save is a one-element batch. The parent-centric editor's "save the NPC and its
three child sheets together" is the same call with four elements — which is why it is
built multi-sheet now rather than retrofitted later.

The client builds each op-set by diffing the panel's row models against what it loaded;
an unchanged existing row produces no op at all. The parent cell is filled in from the
group, never from the form.

Server flow in `Code.gs`:

1. One `LockService.getDocumentLock()` for the entire batch, released in a `finally`.
   `writeRow` takes the same lock — the comment at `Code.gs:405` calls its
   check-then-write window out as out of scope, and a group save that did not coordinate
   with single-record saves would only widen it.
2. Shape checks per sheet: every `cells`/`loaded` array exactly the header width; no row
   number in two ops; no sheet twice.
3. One `getDataRange().getValues()` per sheet, used for every check below.
4. **Conflict and duplicate-id checks for every sheet, before anything anywhere is
   written.** Writes use the rule `writeRow` already has: posted equals loaded → never
   written; current equals posted → skip; current differs from loaded → conflict. Deletes
   are stricter — the row must still match `loaded` cell for cell, because deleting a row
   another editor just changed is the case worth being paranoid about. All conflicts
   collect into one error naming sheet, row and columns; nothing is written.
5. Apply per sheet, **parent sheets first**, each sheet **writes → appends → deletes
   (bottom-up, contiguous runs coalesced into `deleteRows(start, count)`)**. Deletes last
   means no row number needs adjusting: every write and append targets a row number still
   valid when it runs.
6. `SpreadsheetApp.flush()`, return per-sheet counts.

The per-cell diff currently inline in `writeRow` is extracted into a shared helper so both
entry points provably use one rule rather than two that agree today.

### New ids across sheets

The client allocates ids with `Validation.nextId` before posting, so a brand-new NPC's
`npc_id` is known when its drops' `npc_template_id` is filled in — no server-side id
resolution is needed. The duplicate-id scan on the NPCs append is what protects it, and
because it runs in step 4 a collision refuses the *whole* batch rather than leaving drops
pointing at an id that never got written.

### Reload discipline

**After any save, successful or failed, the client re-reads the sheet** and reopens the
same group. Deletion shifts every row below it, so no cached row number survives a
success. And a reload after *failure* is load-bearing: without it, retrying a batch that
threw mid-apply would re-append rows that already landed. After the reload the diff sees
them as existing rows and leaves them alone.

### Accepted risk: partial failure

If step 5 throws part-way — quota, a transient Sheets error — earlier writes stand, and
in a multi-sheet batch that can mean a saved NPC missing some of its drops. Steps 2–4
remove the realistic causes, and Apps Script offers no rollback; a snapshot-and-restore
of every affected row is a lot of machinery for a failure nobody has hit. Parent sheets
are applied first so the failure mode is a benign incomplete parent rather than orphan
children pointing at a row that does not exist. The status message says a save failed
part-way when it does.

### Accepted limitation: no reparenting

The parent cell is always written from the group, so moving a drop from Mouse to Bat
means removing it from one group and adding it to the other. Deliberate: it makes
reparenting impossible by accident.

## Validation and edge cases

Per-row validation reuses `Validation.validateRecord(columns, values, idSets, ownId)`
unchanged, with the parent column injected into `values` first so a required FK passes on
a row whose parent cell the panel never showed. Errors render on the offending cell and
Save is refused while any row has one, reporting how many rows are bad.

Gates carried over from the single-record path:

- **`unverifiedRefs`** — if a referenced sheet failed to load, saving is blocked. A group
  table is mostly FK cells, so this matters more here than on the flat form.
- **Blank means "use the SQL default"** — a blank optional cell is valid and is never
  written, as `validateCell` already decides.

| Case | Behaviour |
|---|---|
| Group emptied of all rows | Allowed; deletes every row, group vanishes on reload |
| Orphan rows | Own `(unknown NPC)` group, sorted last, editable and deletable |
| Blank parent cell | One `(no parent)` group, same treatment |
| Parent with zero rows | Reached via the New-group picker; nothing written until a row is added and saved |
| Quest Reqs / Quest Rewards `id` | Read-only, auto-allocated, incrementing across new rows |
| Sheet not in `GROUP_PARENT` | Untouched — today's flat list and single-record form |
| Duplicate child rows | Cells marked, count in the status line, save allowed |

## Prerequisites in the tested core

Three changes to existing modules that the group panel depends on. The first is a
blocker and warrants its own task and tests.

1. **Control ids collide in a table.** `Forms.scalarControl`, `Forms.render`'s
   `<label for>`, `Pickers.fkControl` and three other picker sites hardcode
   `id: 'f-' + column.name` — `forms.js:107,115,161,180,328`, `pickers.js:214,218,552,815`,
   `composites.js:623`. Fine for one record on screen; in a group table every row's item
   cell would get `id="f-item_template_id"`, duplicating ids and pointing every label at
   row 1's input. Fix: thread an id prefix through `ctx` (`ctx.idPrefix`, defaulting to
   `'f-'`), with the group panel passing a per-row prefix.
2. **`Forms.columnControl` is not exported.** The exports are `render, collect,
   showErrors, scalarControl, placeholderFor, defaultOf, effective, el`. The cell-level
   dispatcher the table rests on is module-private.
3. **`writeRow` takes the document lock**, and its per-cell diff is extracted into the
   helper `saveBatch` shares.

`writeRow` itself stays: the existing single-record form keeps using it. Once the
parent-centric editor lands, the single-record save becomes a one-element batch and
`writeRow` can retire — not worth the churn here.

## Staleness

`app.js` is built around `state.sheetToken` and `state.formToken` guarding async replies,
and its comments record real bugs from exactly this — a reply for sheet A landing after
the user moved to sheet B. The group panel has the same shape: click group A, click group
B while a picker or bundle resolves. `Groups` needs its own token discipline, treated as a
first-class requirement rather than something noticed later.

## Performance

NPC Spawns is 4,322 rows grouped by map; how many land on the busiest map is not visible
from the repo. Several hundred rows would mean several hundred typeahead controls in one
panel. `fkControl` builds its results list lazily so there is no 649-option DOM per cell,
but per-control listeners add up. Render the first 100 rows with a "show all" control, and
only build further if it proves slow.

## Testing

`node --test tools/DataEditor/test/*.test.js` — note the glob; passing the bare directory
fails on Node 22. Baseline on this branch: 911 tests, 901 pass, 10 skipped.

- **`test/fake-sheets.js`** gains `deleteRows(start, count)` and a `LockService` stub.
  Its header comment currently lists LockService as unimplemented, and there is no delete
  anywhere in the codebase.
- **`test/code-gs.test.js`** covers `saveBatch`: all three op kinds in one call; a
  conflicting delete target refusing the entire batch including the other sheet's ops; a
  write conflict doing the same; non-contiguous deletes applied bottom-up leaving the
  right rows; duplicate id on append refused; width mismatch refused; the lock released on
  the throwing path.
- **`test/groups.test.js`** (new) covers the client: the grouping reduce including orphan
  and blank-parent groups; parent-list labels and counts; add and remove row models; the
  op-diff builder — given a loaded group and an edited one, exactly which writes, appends
  and deletes come out, and that untouched rows produce none; pk allocation across several
  new Quest Reqs rows.
- **`test/real-bundles.js`** picks up `src/groups.js` so the built bundle is under test.
- **`test/forms.test.js` / `test/pickers.test.js`** cover the `ctx.idPrefix` change,
  including that the default keeps today's ids byte-identical.

`build.mjs` discovers `src/*.js` automatically and sorts alphabetically, so `groups.js`
needs no build change — provided, like every other module, it references `Layout`,
`Forms` and `Pickers` only at call time.

## Deferred

- The parent-centric editor (NPC row plus its spawns, drops and vendor items on one
  screen). This design's container-not-id rendering and multi-sheet `saveBatch` exist to
  make it an embed.
- Retiring `writeRow` in favour of a one-element batch.
- Rollback for a batch that fails part-way.
- Grouping Class Info by `class_id`; its 26-column, ~99-level shape is not what the
  inline table is for.
