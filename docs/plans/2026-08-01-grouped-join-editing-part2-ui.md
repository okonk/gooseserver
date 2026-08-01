# Grouped Join-Table Editing — Part 2: The Group UI

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** For the ten join sheets, list one entry per parent (`1 — Mouse (3)`) and edit all of that parent's child rows in one table, saved together through `saveBatch`.

**Architecture:** A new `src/groups.js` owns the parent list and the group table. `app.js` branches once in `openSheet` on `Layout.groupParent(sheet)`. Cells are built with `Forms.columnControl` and a per-row `ctx.idPrefix`; validation is `Validation.validateRecord` per row; the save is a one-element `saveBatch` batch. `Groups` renders into containers passed in, never fixed ids, so a future parent-centric editor mounts several tables inside one form.

**Tech Stack:** Plain-JS IIFE modules under `tools/DataEditor/src/`, resolving each other as free globals at call time; `node --test` with the fake DOM in `test/fake-dom.js`.

**Part 2 of 2. Part 1 must be merged first** — this plan calls `Forms.columnControl`, `ctx.idPrefix` and `saveBatch`, none of which exist without it.

**Design doc:** `docs/plans/2026-08-01-grouped-join-editing-design.md`

---

## Working agreements

**Worktree:** `/home/hayden/code/illutiagooseserver/.worktrees/grouped-join-editing`, branch `grouped-join-editing`. Paths below are relative to it.

```bash
node --test tools/DataEditor/test/*.test.js
```

The bare directory argument fails on Node 22. Expected count entering this plan: **950 tests, 940 pass, 10 skipped** (Part 1's end state).

**Client modules are ES5-flavoured IIFEs.** `var`, `function`, no arrow functions, no `Array.prototype.includes` (`indexOf` is used throughout). Each ends with `if (typeof module !== 'undefined') module.exports = { X: X };`.

**Modules resolve each other as free globals at call time.** `Groups` may reference `Forms`, `Layout`, `Pickers` and `Validation` inside functions but must not touch them at definition time.

---

## APIs verified

Read in this worktree before writing this plan.

**From Part 1 (must be present before starting):**
- `Forms.columnControl(opts)` exported, `opts = { column, ctx, sheet, values, effective }`, honouring `ctx.idPrefix`
- `Pickers.fkControl` prefixing its input id, listbox id and `aria-controls` together
- `saveBatch(ops)` on the server, `ops = [{ sheet, idColumnIndex, textColumns, writes, appends, deletes }]`

**Client:**
- `Forms.el(tag, attrs, text)` — `src/forms.js:9`
- `Forms.collect(container, schema)` — `src/forms.js:378`. Sweeps `[name]` across the container and returns **exactly** the schema's columns; a stray name is dropped. Because it is scoped to the container it is given, a single table ROW element is a valid container.
- `Forms.showErrors(container, errors)` — `src/forms.js:408`. Matches on `[data-error-for]` slots within the container. Same scoping: per-row slots in a per-row container work unchanged.
- `Forms.effective(values, columns)` — exported as `effective` at `src/forms.js:434`, the function `effectiveValues` at `:54`
- `Validation.validateRecord(columns, values, idSets, ownId)` — `src/validation.js:193`
- `Validation.validateCell(column, raw, idSets)` — `src/validation.js:41`, returns `{ ok, write, message }`. `write: false` means "blank — do not write this cell".
- `Validation.nextId(ids)` — `src/validation.js:179`, accepts an array or a Set, returns `max + 1` flooring at 0
- `Layout` exports and its `deepFreeze` / `own` / `twoLevel` helpers — `src/layout.js:207-216`, `:360-390`. `RESTART_ONLY` and every table are exported frozen so `layout.test.js` can check them against `schema.js` in both directions.
- `App.__state` — `src/app.js:1281`, the shared state object; fields at `:66-107`
- `App` state fields this plan touches: `schema`, `sheetName`, `sheetToken` (`:69`), `formToken` (`:70`), `saving` (`:71`), `rows` (`:78`), `rowNumber` (`:79`), `ids` (`:80`), `idSets` (`:81`), `pickerData` (`:82`), `loaded` (`:87`), `refErrors` (`:105`)
- `ctx()` — `src/app.js:118`, returns `{ bundles, images, pickerData, refErrors, onImagesReady, onFormChange }`
- `openSheet(sheetName)` — `src/app.js:370`; `clearPreviews` `:376`, `clearForm` `:377`, `clearRecords` `:378`, schema swap `:380`, token bump `:384`, `readSheet` call `:387-405`
- `collectIds()` — `src/app.js:411`; `loadReferencedSheets(done)` — `:429`; `rowToValues(row)` — `:471`; `renderList()` — `:479`; `editRow(index)` — `:495`; `newRecord()` — `:505`
- **`rows[i]` is spreadsheet row `i + 2`** — `src/app.js:493`, and `Code.gs:276`
- `nameIndex(schema)` — `src/app.js:279`, the 0-based index of the first Text column, defaulting to 1. This is what `pickerData` entries' `name` comes from.
- `save()` — `src/app.js:899`; its gate order is worth copying: saving/pending `:906-910`, empty container `:917`, `unverifiedRefs` `:940`, validation `:964`, cell folding via `validateCell(...).write` `:973-976`, `idIndex` -1 for no-pk sheets `:980`, `loadedCells` `:986`, `textColumns` `:992-995`, post-save cache drop `:1018-1019`
- `init()` — `src/app.js:1242`; `#new-record` wired at `:1249`, `#save` at `:1250`, delegated form listener at `:1262-1265`
- `App` exports — `src/app.js:1272-1282`
- `Editor.html` shell: `#sheet-picker`, `#new-record`, `#save`, `#publish-check`, `#status` in `<header>`; `#records`, `#form`, `#previews`, `#publish-results` in `<main>`; `#modal` after it. Include list at the end of the file.
- `GOOSE_SCHEMA` sheet entries carry `sheet`, `table`, `columns[]`, `composites[]`; a column carries `name`, `kind`, `sql`, `required`, `pk`, optional `default`, `ref`, `enumNames`.

**Grouped sheets and their parents, verified against `schema.js`:**

| Sheet | Parent column | Column index | Parent sheet |
|---|---|---|---|
| NPC Drops | `npc_template_id` | 0 | NPCs |
| NPC Vendor Items | `npc_template_id` | 0 | NPCs |
| NPC Spawns | `map_id` | 1 | Maps |
| Warptiles | `map_id` | 0 | Maps |
| Map Required Items | `map_id` | 0 | Maps |
| Quest Reqs | `quest_id` | 1 | Quests |
| Quest Rewards | `quest_id` | 1 | Quests |
| Combination Item Required | `combination_id` | 0 | Combinations |
| Combination Item Result | `combination_id` | 0 | Combinations |
| Class Levelup Spells | `class_id` | 0 | Classes |

Quest Reqs and Quest Rewards are the only two with a pk of their own (`id`, index 0). The other eight have none. **No grouped sheet has a composite or a part graphic** (composites exist only on Items, NPCs, Quests, Spells, Spell Effects, Combinations; `Layout.PART_GRAPHICS` covers only Items, NPCs, Spell Effects). Every cell in a group table is therefore a plain scalar or an FK.

**Test infrastructure:**
- `installFakeDom()`, `installFakeImage()`, `installGoogleScriptRun(server)`, `fire(node, type, init)`, `walk(node)` — `test/fake-dom.js:637`, `:649`, `:698`, `:620`, `:757`
- `installGoogleScriptRun` builds a runner from `Object.keys(server)`, so a `saveBatch` key on the fake server is all that is needed to make it callable — `test/fake-dom.js:708`
- Its return value has `calls`, `queue`, `flush(limit)` and `step()` — `:730-749`
- `test/app.test.js` loads the **real** `schema.js` (`:11-13`) and has helpers `rowFor(sheet, values)` (`:50`), `ITEM`, `NPC`, `MAP`, `COMBO` and `DROP` (`:56-68`), plus `makeServer(sheets, options)` (`:71`). Reuse them.
- `test/editor-html.test.js:17` already asserts every `src/*.js` is in Editor.html's include list — adding `src/groups.js` without the include fails that existing test. `:44` asserts every id `app.js` looks up exists in the markup.
- `test/layout.test.js` checks every Layout table's column names against `schema.js`. A new table must be checked the same way.

---

## Task 1: `Layout.groupParent`

**Files:**
- Modify: `tools/DataEditor/src/layout.js` (new table beside `GALLERIES` at `:200`, accessor beside `galleryBundle` at `:263`, export at `:368`)
- Test: `tools/DataEditor/test/layout.test.js`

**Step 1: Write the failing test**

Append to `test/layout.test.js`, matching how that file checks the other tables against the schema:

```js
// ------------------------------------------------------------------ GROUP_PARENT

test('every grouped sheet names a column that sheet really has', () => {
  Object.keys(Layout.GROUP_PARENT).forEach((sheet) => {
    const schema = schemaOf(sheet);
    assert.ok(schema, sheet + ' is not a sheet in the schema');
    const names = schema.columns.map((c) => c.name);
    assert.ok(names.includes(Layout.GROUP_PARENT[sheet]),
              sheet + ' has no column ' + Layout.GROUP_PARENT[sheet]);
  });
});

test('every parent column is a foreign key, so the parent sheet is derivable', () => {
  // The table names only the COLUMN. The parent SHEET comes from that column's ref in the
  // schema, so the two cannot drift — but only if every entry actually has a ref.
  Object.keys(Layout.GROUP_PARENT).forEach((sheet) => {
    const column = schemaOf(sheet).columns
      .filter((c) => c.name === Layout.GROUP_PARENT[sheet])[0];
    assert.ok(column.ref, sheet + '.' + column.name + ' has no ref');
  });
});

test('groupParent answers null for a sheet that is not grouped', () => {
  assert.equal(Layout.groupParent('Items'), null);
  assert.equal(Layout.groupParent('NPCs'), null);
  // Class Info is class_id + level, 26 columns wide and ~99 rows per class. Deliberately out:
  // that shape is not what the inline table is for.
  assert.equal(Layout.groupParent('Class Info'), null);
});

test('groupParent answers the parent column for a grouped sheet', () => {
  assert.equal(Layout.groupParent('NPC Drops'), 'npc_template_id');
  // Spawns group by MAP, not by NPC: spawns are authored a zone at a time, and it gives more
  // evenly sized groups than 4,322 rows split across every NPC.
  assert.equal(Layout.groupParent('NPC Spawns'), 'map_id');
});

test('GROUP_PARENT is frozen', () => {
  assert.ok(Object.isFrozen(Layout.GROUP_PARENT));
});

test('no grouped sheet has a composite', () => {
  // The group table builds every cell with Forms.columnControl, which routes a composite's
  // columns nowhere. If a grouped sheet ever gains one, the table would silently render its
  // columns as bare text boxes — so this fails first instead.
  Object.keys(Layout.GROUP_PARENT).forEach((sheet) => {
    assert.deepEqual(schemaOf(sheet).composites || [], [], sheet + ' gained a composite');
  });
});

test('no grouped sheet has a part graphic', () => {
  // Same reasoning: partControl needs a canvas and the parts atlas, neither of which a table
  // cell has room for.
  Object.keys(Layout.GROUP_PARENT).forEach((sheet) => {
    schemaOf(sheet).columns.forEach((c) => {
      assert.equal(Layout.partGraphic(sheet, c.name), null, sheet + '.' + c.name);
    });
  });
});
```

`test/layout.test.js` already has a `schemaOf` helper; reuse it rather than adding another.

**Step 2: Run to verify it fails**

```bash
node --test tools/DataEditor/test/layout.test.js
```

Expected: failures on `Layout.GROUP_PARENT` being `undefined` and `Layout.groupParent` not being a function.

**Step 3: Implement**

Add to `src/layout.js` after `GALLERIES` (`:202`):

```js
  // WHICH COLUMN MAKES A JOIN SHEET'S ROWS BELONG TO SOMETHING. A sheet listed here is edited as
  // one table per parent — "1 — Mouse" and all three of its drops at once — instead of as a flat
  // list of id pairs.
  //
  // Presentation, so it lives here rather than in the descriptors: the importer does not care how
  // rows are grouped, and two of these have more than one defensible parent. Only the COLUMN is
  // named; the parent SHEET is that column's `ref` in the schema, so the two cannot drift.
  //
  // The two judgement calls, both checked by layout.test.js:
  //   NPC Spawns refs NPCs and Maps. By map, because spawns are authored a zone at a time and it
  //     splits 4,322 rows more evenly than by NPC.
  //   Warptiles refs Maps twice (map_id, warp_id). By map_id — where the tile IS, not where it
  //     goes.
  // Quest Reqs and Quest Rewards keep their own `id` pk; the parent here is quest_id, which is
  // the second column, not the first.
  //
  // NOT LISTED, deliberately: Class Info. It is class_id + level, 26 columns wide with a row per
  // level, so a group would be a ~99 x 25 grid — the flat form is the better shape for it.
  var GROUP_PARENT = {
    'NPC Drops': 'npc_template_id',
    'NPC Vendor Items': 'npc_template_id',
    'NPC Spawns': 'map_id',
    'Warptiles': 'map_id',
    'Map Required Items': 'map_id',
    'Quest Reqs': 'quest_id',
    'Quest Rewards': 'quest_id',
    'Combination Item Required': 'combination_id',
    'Combination Item Result': 'combination_id',
    'Class Levelup Spells': 'class_id',
  };
```

Add the accessor beside `galleryBundle`:

```js
  /// The column a sheet's rows are grouped by, or null when the sheet is edited flat. One
  /// accessor for both consumers — app.js branches on it, groups.js builds the grouping from it —
  /// so no two of them can disagree about which sheets are grouped.
  function groupParent(sheet) {
    var column = own(GROUP_PARENT, String(sheet));
    return column === undefined ? null : column;
  }
```

Add to the exports object:

```js
    groupParent: groupParent,
    GROUP_PARENT: deepFreeze(GROUP_PARENT),
```

**Step 4: Run**

```bash
node --test tools/DataEditor/test/*.test.js
```

Expected: **957 tests, 947 pass, 0 fail, 10 skipped.**

**Step 5: Commit**

```bash
git add tools/DataEditor/src/layout.js tools/DataEditor/test/layout.test.js
git commit -m "feat(editor): name the parent column of every grouped join sheet"
```

---

## Task 2: The grouping model

Pure data: rows in, groups out. No DOM.

**Files:**
- Create: `tools/DataEditor/src/groups.js`
- Modify: `tools/DataEditor/Editor.html` (include list)
- Test: `tools/DataEditor/test/groups.test.js` (create)

**Step 1: Write the failing test**

Create `test/groups.test.js`:

```js
// The grouping model: which parent each child row belongs to, and what the list of parents looks
// like. Pure data — no DOM until the next task — because this is where the awkward cases live and
// they are far easier to state as values than as markup.

import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';

const schemaSource = readFileSync(fileURLToPath(new URL('../schema.js', import.meta.url)), 'utf8');
globalThis.GOOSE_SCHEMA = new Function(schemaSource + '\nreturn GOOSE_SCHEMA;')();

const { Layout } = await import('../src/layout.js');
globalThis.Layout = Layout;
const { Validation } = await import('../src/validation.js');
globalThis.Validation = Validation;
const { Groups } = await import('../src/groups.js');

const schemaOf = (sheet) => GOOSE_SCHEMA.sheets.filter((s) => s.sheet === sheet)[0];

function rowFor(sheet, values) {
  return schemaOf(sheet).columns.map((c) => (values[c.name] === undefined ? '' : String(values[c.name])));
}

const DROP = (npcId, itemId, rate) => rowFor('NPC Drops', {
  npc_template_id: npcId, item_template_id: itemId, stack: 1, droprate: rate || '0.10',
});

// The id + name lists app.js already loads for the FK pickers, in pickerData's shape.
const NPCS = [{ id: '1', name: 'Mouse' }, { id: '2', name: 'Bat' }];

// ------------------------------------------------------------------ parentOf

test('parentOf answers the column and its index for a grouped sheet', () => {
  assert.deepEqual(Groups.parentOf(schemaOf('NPC Drops')),
                   { column: 'npc_template_id', index: 0, ref: 'NPCs' });
});

test('parentOf finds a parent that is not the first column', () => {
  // Quest Reqs is id, quest_id, ... — the pk comes first and is not the parent.
  assert.deepEqual(Groups.parentOf(schemaOf('Quest Reqs')),
                   { column: 'quest_id', index: 1, ref: 'Quests' });
});

test('parentOf answers null for a sheet that is not grouped', () => {
  assert.equal(Groups.parentOf(schemaOf('Items')), null);
});

// ------------------------------------------------------------------ build

test('build groups rows under their parent, with the parent name and a count', () => {
  const groups = Groups.build(schemaOf('NPC Drops'), [DROP(1, 10), DROP(2, 20), DROP(1, 30)], NPCS);
  assert.deepEqual(groups.map((g) => g.label), ['1 — Mouse', '2 — Bat']);
  assert.deepEqual(groups.map((g) => g.count), [2, 1]);
});

test('build records each row spreadsheet row number', () => {
  // rows[i] is sheet row i + 2 (app.js:493). Everything downstream writes and deletes by that
  // number, so getting it wrong here corrupts a different record than the one on screen.
  const groups = Groups.build(schemaOf('NPC Drops'), [DROP(1, 10), DROP(2, 20), DROP(1, 30)], NPCS);
  assert.deepEqual(groups[0].rows.map((r) => r.rowNumber), [2, 4]);
  assert.deepEqual(groups[1].rows.map((r) => r.rowNumber), [3]);
});

test('build keeps each row values keyed by column name', () => {
  const groups = Groups.build(schemaOf('NPC Drops'), [DROP(1, 10, '0.25')], NPCS);
  assert.equal(groups[0].rows[0].values.item_template_id, '10');
  assert.equal(groups[0].rows[0].values.droprate, '0.25');
});

test('build orders groups by parent id numerically, not as text', () => {
  const npcs = [{ id: '2', name: 'Bat' }, { id: '10', name: 'Wolf' }, { id: '1', name: 'Mouse' }];
  const groups = Groups.build(schemaOf('NPC Drops'), [DROP(10, 1), DROP(2, 1), DROP(1, 1)], npcs);
  assert.deepEqual(groups.map((g) => g.id), ['1', '2', '10']);
});

test('build gives a row whose parent is not in the parent sheet its own group, sorted last', () => {
  // Without this the row is invisible under grouping — dead data you cannot find, let alone fix.
  const groups = Groups.build(schemaOf('NPC Drops'), [DROP(4471, 1), DROP(1, 1)], NPCS);
  assert.deepEqual(groups.map((g) => g.label), ['1 — Mouse', '4471 — (not in NPCs)']);
  assert.equal(groups[1].orphan, true);
});

test('build collects rows with a blank parent into one group, last of all', () => {
  const groups = Groups.build(schemaOf('NPC Drops'),
                              [DROP('', 1), DROP(4471, 1), DROP(1, 1), DROP('', 2)], NPCS);
  assert.deepEqual(groups.map((g) => g.label),
                   ['1 — Mouse', '4471 — (not in NPCs)', '(no parent)']);
  assert.equal(groups[2].count, 2);
});

test('build folds id formatting so 1 and "1.00" are one group', () => {
  // The sheet stores ids as numbers and the client holds them as strings; a group keyed on the
  // raw text would split one NPC's drops across two entries.
  const rows = [DROP('1', 10), DROP('1.00', 20), DROP(' 1 ', 30)];
  const groups = Groups.build(schemaOf('NPC Drops'), rows, NPCS);
  assert.equal(groups.length, 1);
  assert.equal(groups[0].count, 3);
});

test('build returns nothing for a sheet with no rows', () => {
  assert.deepEqual(Groups.build(schemaOf('NPC Drops'), [], NPCS), []);
});

test('build tolerates a missing parent list', () => {
  // loadReferencedSheets can fail; the editor must still show the rows (unverifiedRefs is what
  // stops them being SAVED). Every group reads as an orphan, which is the honest label.
  const groups = Groups.build(schemaOf('NPC Drops'), [DROP(1, 10)], null);
  assert.equal(groups.length, 1);
  assert.equal(groups[0].orphan, true);
});

// ------------------------------------------------------------------ missingParents

test('missingParents lists parents that have no rows yet', () => {
  // The New-group picker's contents. Every parent is offered, not only the empty ones, so there
  // is one way to reach any parent — but the caller needs to know which already have a group.
  const groups = Groups.build(schemaOf('NPC Drops'), [DROP(1, 10)], NPCS);
  assert.deepEqual(Groups.missingParents(groups, NPCS).map((e) => e.id), ['2']);
});
```

**Step 2: Run to verify it fails**

```bash
node --test tools/DataEditor/test/groups.test.js
```

Expected: `Cannot find module '../src/groups.js'`.

**Step 3: Implement**

Create `src/groups.js`:

```js
// Join sheets, edited a parent at a time. `NPC Drops` is npc_template_id + item_template_id, and
// as a flat list of records that reads "1 — 1", "1 — 2", "1 — 4471": every entry is a pair of
// numbers, and an NPC's drops are however many of them happen to be adjacent. Grouped, it is one
// entry per NPC and one table per click.
//
// This half is the model — which parent a row belongs to, and what the parent list looks like.
// The table itself is below it and the ops a save posts are below that; keeping the three apart
// is what lets the awkward cases (a parent that is not in the parent sheet, a blank parent cell,
// an id the sheet stores as a number and the client holds as text) be stated as values.
var Groups = (function () {

  function str(value) {
    return (value === undefined || value === null) ? '' : String(value);
  }

  // The same folding Code.gs's idKey_ does, and for the same reason: the sheet holds ids as
  // numbers, the client holds them as strings, and a group keyed on raw text would split one
  // NPC's drops across "1", "1.00" and " 1 ".
  function idKey(value) {
    var text = str(value).trim().replace(/,/g, '');
    if (text === '') return '';
    var n = Number(text);
    return isNaN(n) ? text : String(n);
  }

  /// The parent column of a grouped sheet — `{ column, index, ref }` — or null.
  ///
  /// The COLUMN comes from Layout, the REF from the schema. Nothing here names a sheet, so the
  /// table and the schema cannot come to disagree about which sheet a parent id points at.
  function parentOf(schema) {
    if (!schema) return null;
    var name = Layout.groupParent(schema.sheet);
    if (!name) return null;

    for (var i = 0; i < schema.columns.length; i++) {
      if (schema.columns[i].name === name) {
        return { column: name, index: i, ref: schema.columns[i].ref || null };
      }
    }
    return null;
  }

  /// The sheet's rows, grouped. `rows` is state.rows — row i is spreadsheet row i + 2 — and
  /// `entries` is the parent sheet's id + name list as loadReferencedSheets stores it, or null.
  ///
  /// Returns [{ key, id, label, count, orphan, rows: [{ rowNumber, values }] }], real groups
  /// first in numeric id order, then orphans, then the one blank-parent group. Sorting the
  /// unreachable ones last keeps them out of the way without hiding them: a drop pointing at an
  /// NPC that no longer exists is dead data the editor should let you find and delete.
  function build(schema, rows, entries) {
    var parent = parentOf(schema);
    if (!parent) return [];

    var names = {};
    (entries || []).forEach(function (e) { names[idKey(e.id)] = str(e.name); });

    var byKey = {};
    var order = [];

    (rows || []).forEach(function (row, i) {
      var key = idKey(row[parent.index]);

      if (!Object.prototype.hasOwnProperty.call(byKey, key)) {
        var known = key !== '' && Object.prototype.hasOwnProperty.call(names, key);
        byKey[key] = {
          key: key,
          id: key,
          // The referenced SHEET's name, not a hand-written singular per sheet: "(not in NPCs)"
          // needs no table of nouns and says exactly where to go and look.
          label: key === '' ? '(no parent)'
               : known ? key + ' — ' + names[key]
               : key + ' — (not in ' + (parent.ref || 'the parent sheet') + ')',
          orphan: !known,
          rows: [],
        };
        order.push(byKey[key]);
      }

      var values = {};
      schema.columns.forEach(function (c, j) {
        values[c.name] = row && row[j] !== undefined ? str(row[j]) : '';
      });

      byKey[key].rows.push({ rowNumber: i + 2, values: values });
    });

    order.sort(function (a, b) {
      // Blank last of all, then orphans, then real groups by id. Rank first so the numeric
      // comparison below only ever runs between two groups of the same kind.
      var rank = function (g) { return g.key === '' ? 2 : (g.orphan ? 1 : 0); };
      if (rank(a) !== rank(b)) return rank(a) - rank(b);
      var na = Number(a.key);
      var nb = Number(b.key);
      if (isNaN(na) || isNaN(nb)) return a.key < b.key ? -1 : (a.key > b.key ? 1 : 0);
      return na - nb;
    });

    order.forEach(function (g) { g.count = g.rows.length; });
    return order;
  }

  /// The parent entries with no group yet. The New-group picker offers EVERY parent — one way to
  /// reach any of them — and uses this only to say which are empty.
  function missingParents(groups, entries) {
    var has = {};
    (groups || []).forEach(function (g) { has[g.key] = true; });
    return (entries || []).filter(function (e) { return !has[idKey(e.id)]; });
  }

  return {
    idKey: idKey,
    parentOf: parentOf,
    build: build,
    missingParents: missingParents,
  };
})();

if (typeof module !== 'undefined') module.exports = { Groups: Groups };
```

Add to `Editor.html`'s include list, after `forms` and before `app` (`Groups` reads `Forms`, `Layout` and `Validation` at call time, so the position is free, but the list is kept in dependency order):

```html
<?!= include('groups'); ?>
```

and add `groups -> Forms, Layout, Pickers, Validation` to the dependency comment above the list.

**Step 4: Run**

```bash
node --test tools/DataEditor/test/*.test.js
```

Expected: **970 tests, 960 pass, 0 fail, 10 skipped.** `editor-html.test.js` passes because the include was added; drop it and it fails, which is the point.

**Step 5: Commit**

```bash
git add tools/DataEditor/src/groups.js tools/DataEditor/Editor.html tools/DataEditor/test/groups.test.js
git commit -m "feat(editor): group join-sheet rows by their parent"
```

---

## Task 3: The group table

**Files:**
- Modify: `tools/DataEditor/src/groups.js`
- Test: `tools/DataEditor/test/groups.test.js`

**Step 1: Write the failing test**

`test/groups.test.js` needs the fake DOM from here on. Add `installFakeDom()` and the `Forms`/`Pickers` imports to its preamble, mirroring `test/app.test.js:5-33`, then append:

```js
// ------------------------------------------------------------------ render

function ctxFor(pickerData) {
  return { pickerData: pickerData || {}, refErrors: [], bundles: {}, images: {},
           onImagesReady() {}, onFormChange() {} };
}

function panelFor(sheet, rows, entries) {
  const schema = schemaOf(sheet);
  const groups = Groups.build(schema, rows, entries);
  const container = createElement('div');
  Groups.render({ container, schema, group: groups[0], ctx: ctxFor({ NPCs: entries, Items: [] }),
                  ids: [] });
  return { container, schema, groups };
}

test('render draws one table row per child record', () => {
  const { container } = panelFor('NPC Drops', [DROP(1, 10), DROP(1, 20)], NPCS);
  assert.equal(container.querySelectorAll('[data-group-row]').length, 2);
});

test('render omits the parent column — it is implied by the group', () => {
  const { container } = panelFor('NPC Drops', [DROP(1, 10)], NPCS);
  const named = container.querySelectorAll('[name]');
  const names = [...named].map((n) => n.getAttribute('name'));
  assert.ok(!names.includes('npc_template_id'), 'the parent cell must not be editable');
  assert.ok(names.includes('item_template_id'));
  assert.ok(names.includes('droprate'));
});

test('render gives every row its own id prefix so controls do not collide', () => {
  // The bug this whole design turns on: without a per-row prefix both rows' item inputs carry
  // id="f-item_template_id" and every label points at row 1.
  const { container } = panelFor('NPC Drops', [DROP(1, 10), DROP(1, 20)], NPCS);
  const ids = [...container.querySelectorAll('input[type=text]')].map((n) => n.id);
  assert.equal(new Set(ids).size, ids.length, 'duplicate control ids: ' + ids.join(', '));
});

test('render seeds each cell from its stored value', () => {
  const { container } = panelFor('NPC Drops', [DROP(1, 10, '0.25')], NPCS);
  const cell = container.querySelectorAll('[name=droprate]')[0];
  assert.equal(cell.value, '0.25');
});

test('collect reads the rows back, parent cell included', () => {
  // The parent is not on screen but every posted record must carry it, so a row cannot be
  // saved into the wrong group by omission.
  const { container, schema } = panelFor('NPC Drops', [DROP(1, 10, '0.25')], NPCS);
  const read = Groups.collect(container, schema);
  assert.equal(read.length, 1);
  assert.equal(read[0].rowNumber, 2);
  assert.equal(read[0].values.npc_template_id, '1');
  assert.equal(read[0].values.item_template_id, '10');
});

test('addRow appends an editable blank row with row number 0', () => {
  // 0 is writeRow's and saveBatch's append sentinel.
  const { container, schema } = panelFor('NPC Drops', [DROP(1, 10)], NPCS);
  Groups.addRow(container);
  const read = Groups.collect(container, schema);
  assert.equal(read.length, 2);
  assert.equal(read[1].rowNumber, 0);
  assert.equal(read[1].values.npc_template_id, '1');
});

test('removing a row takes it out of collect and records it for deletion', () => {
  const { container, schema } = panelFor('NPC Drops', [DROP(1, 10), DROP(1, 20)], NPCS);
  const remove = container.querySelectorAll('[data-remove]')[0];
  fire(remove, 'click');
  const read = Groups.collect(container, schema);
  assert.deepEqual(read.map((r) => r.rowNumber), [3]);
  assert.deepEqual(Groups.removed(container).map((r) => r.rowNumber), [2]);
});

test('removing a row that was never saved records no deletion', () => {
  // A row added and then removed before saving does not exist in the sheet; posting a delete
  // for row 0 would be a delete of nothing at best.
  const { container } = panelFor('NPC Drops', [DROP(1, 10)], NPCS);
  Groups.addRow(container);
  const removes = container.querySelectorAll('[data-remove]');
  fire(removes[removes.length - 1], 'click');
  assert.deepEqual(Groups.removed(container), []);
});

test('a sheet with its own pk renders the id read-only and allocates the next one', () => {
  const schema = schemaOf('Quest Reqs');
  const rows = [rowFor('Quest Reqs', { id: 5, quest_id: 1, requirement_type: 'Item' })];
  const container = createElement('div');
  Groups.render({ container, schema, group: Groups.build(schema, rows, [{ id: '1', name: 'Q' }])[0],
                  ctx: ctxFor({ Quests: [{ id: '1', name: 'Q' }] }), ids: [5] });

  const existing = container.querySelectorAll('[name=id]')[0];
  assert.equal(existing.value, '5');
  assert.ok(existing.readOnly || existing.getAttribute('readonly') !== null);

  Groups.addRow(container);
  Groups.addRow(container);
  const read = Groups.collect(container, schema);
  // Allocated locally and incrementing, so two new rows in one save cannot take the same id.
  assert.deepEqual(read.slice(1).map((r) => r.values.id), ['6', '7']);
});

test('render draws an error slot per cell', () => {
  // Forms.showErrors matches [data-error-for] within the container it is given, so a row
  // element is a valid container and each row reports its own problems.
  const { container } = panelFor('NPC Drops', [DROP(1, 10)], NPCS);
  const row = container.querySelectorAll('[data-group-row]')[0];
  assert.ok(row.querySelectorAll('[data-error-for=droprate]').length);
});
```

**Step 2: Run to verify it fails**

```bash
node --test tools/DataEditor/test/groups.test.js
```

Expected: `Groups.render is not a function`.

**Step 3: Implement**

Add to `src/groups.js`, before the return block.

The panel keeps its state on the container as `__group` — the same technique `app.js` uses for `__frozen` and `__graphicError` (`src/app.js:796`, `:893`), and the reason is the same: the alternative is a module-level singleton, and a singleton is exactly what stops a future parent-centric editor mounting three of these tables at once.

```js
  // A row's controls all share this prefix, and no two rows share one. Sequential rather than
  // derived from the row number, because an appended row has no row number yet and two of them
  // would collide on 0.
  function prefixFor(seq) {
    return 'g' + seq + '-';
  }

  // The columns a group table shows: everything but the parent, which the group already says.
  function visibleColumns(schema, parent) {
    return schema.columns.filter(function (c) { return c.name !== parent.column; });
  }

  /// Renders one group's rows as a table into `container`.
  ///
  /// opts: { container, schema, group, ctx, ids }
  ///   group — one entry from build(); its `rows` are the records, `id` the parent
  ///   ctx    — app.js's ctx(), for the pickers. A per-row idPrefix is added on top of it.
  ///   ids    — every id already in the sheet, for allocating a pk on a new row
  ///
  /// The container is passed in rather than looked up: a parent-centric editor mounts several of
  /// these inside one form, and a module that reaches for document.getElementById('form') could
  /// only ever have one.
  function render(opts) {
    var container = opts.container;
    var schema = opts.schema;
    var group = opts.group;
    var parent = parentOf(schema);

    container.innerHTML = '';

    var state = {
      schema: schema,
      parent: parent,
      parentId: group ? group.id : '',
      ids: (opts.ids || []).slice(),
      ctx: opts.ctx,
      seq: 0,
      removed: [],
      body: null,
    };
    container.__group = state;

    var columns = visibleColumns(schema, parent);
    var pk = schema.columns.filter(function (c) { return c.pk; })[0];

    var head = Forms.el('div', { class: 'group-head' });
    head.appendChild(Forms.el('h3', null, group ? group.label : ''));
    head.appendChild(Forms.el('span', { class: 'count' },
      (group ? group.count : 0) + (group && group.count === 1 ? ' row' : ' rows')));
    container.appendChild(head);

    var table = Forms.el('div', { class: 'group-table' });
    var header = Forms.el('div', { class: 'group-header' });
    columns.forEach(function (c) { header.appendChild(Forms.el('span', null, c.name)); });
    header.appendChild(Forms.el('span', null, ''));       // the remove button's column
    table.appendChild(header);

    var body = Forms.el('div', { class: 'group-body' });
    table.appendChild(body);
    container.appendChild(table);
    state.body = body;

    (group ? group.rows : []).forEach(function (row) {
      body.appendChild(buildRow(state, columns, pk, row.rowNumber, row.values));
    });

    return container;
  }

  // One record. `rowNumber` is 0 for a row being added.
  function buildRow(state, columns, pk, rowNumber, values) {
    var schema = state.schema;
    var prefix = prefixFor(state.seq++);

    var row = Forms.el('div', { class: 'group-row', 'data-group-row': String(rowNumber) });
    row.__rowNumber = rowNumber;
    // The record AS LOADED, so the save can tell an edited cell from one another editor touched.
    // Null for an appended row: there is nothing it was loaded from.
    row.__loaded = rowNumber > 0 ? values : null;
    // The parent cell never reaches the DOM, so it is carried here and put back by collect().
    row.__parent = state.parentId;

    // Blanks resolved to their SQL defaults, for the controls that read a neighbouring cell.
    // No grouped sheet has such a control today; passed anyway so that stays true by
    // construction rather than by luck.
    var effective = Forms.effective(values, schema.columns);

    // ONE ctx per row, differing only in idPrefix. Object.assign is not available in this
    // dialect, so it is spelled out; the rest of ctx is shared by reference, which is what
    // makes refErrors' mutation-in-place visible to every row.
    var ctx = {};
    Object.keys(state.ctx || {}).forEach(function (k) { ctx[k] = state.ctx[k]; });
    ctx.idPrefix = prefix;

    columns.forEach(function (column) {
      var cell = Forms.el('div', { class: 'group-cell' });

      if (column.pk) {
        // Allocated, not typed. The id of a child row is bookkeeping — nothing in the game
        // refers to a quest requirement by id — so offering it for editing is offering a way to
        // collide with another row for no gain.
        var id = Forms.el('input', { name: column.name, type: 'text', readonly: 'readonly',
                                     id: prefix + column.name, class: 'pk' });
        id.readOnly = true;
        id.value = values[column.name];
        cell.appendChild(id);
      } else {
        cell.appendChild(Forms.columnControl({
          column: column, ctx: ctx, sheet: schema.sheet, values: values, effective: effective,
        }));
      }

      cell.appendChild(Forms.el('div', { class: 'error', 'data-error-for': column.name }));
      row.appendChild(cell);
    });

    var remove = Forms.el('button', {
      type: 'button', class: 'remove', 'data-remove': '',
      title: 'remove this row', 'aria-label': 'remove this row',
    }, '×');
    remove.addEventListener('click', function () {
      // A row that exists in the sheet is recorded for deletion; one that was only ever on
      // screen just goes. Posting a delete for row 0 would be a delete of nothing.
      if (row.__rowNumber > 0) {
        state.removed.push({ rowNumber: row.__rowNumber, loaded: row.__loaded });
      }
      if (row.parentNode) row.parentNode.removeChild(row);
    });
    row.appendChild(remove);

    return row;
  }

  /// Adds a blank row to an open panel. The parent cell is filled from the group and a pk, if the
  /// sheet has one, is allocated locally — incrementing, so several new rows in one save cannot
  /// take the same id.
  function addRow(container) {
    var state = container.__group;
    if (!state) return null;

    var schema = state.schema;
    var columns = visibleColumns(schema, state.parent);
    var pk = schema.columns.filter(function (c) { return c.pk; })[0];

    var values = {};
    schema.columns.forEach(function (c) { values[c.name] = ''; });
    values[state.parent.column] = state.parentId;

    if (pk) {
      var id = Validation.nextId(state.ids);
      values[pk.name] = String(id);
      state.ids.push(id);
    }

    var row = buildRow(state, columns, pk, 0, values);
    state.body.appendChild(row);
    return row;
  }

  /// The rows still on screen, as [{ rowNumber, values, loaded }].
  ///
  /// Forms.collect is scoped to the container it is given, so each row is collected on its own
  /// and the result holds exactly this sheet's columns. The parent cell is put back afterwards
  /// because it is never rendered — which is also what makes reparenting impossible by accident.
  function collect(container, schema) {
    var state = container.__group;
    if (!state) return [];

    var rows = state.body.querySelectorAll('[data-group-row]');
    var out = [];
    for (var i = 0; i < rows.length; i++) {
      var values = Forms.collect(rows[i], schema);
      values[state.parent.column] = rows[i].__parent;
      out.push({ rowNumber: rows[i].__rowNumber, values: values, loaded: rows[i].__loaded });
    }
    return out;
  }

  /// The rows removed since the panel was rendered, as [{ rowNumber, loaded }].
  function removed(container) {
    var state = container.__group;
    return state ? state.removed.slice() : [];
  }
```

and add `render`, `addRow`, `collect`, `removed` to the exports.

**Step 4: Run**

```bash
node --test tools/DataEditor/test/*.test.js
```

Expected: **981 tests, 971 pass, 0 fail, 10 skipped.**

**Step 5: Commit**

```bash
git add tools/DataEditor/src/groups.js tools/DataEditor/test/groups.test.js
git commit -m "feat(editor): edit a whole group of child rows in one table"
```

---

## Task 4: The op-diff builder

Turns an open panel into the `saveBatch` op-set. Pure function of collected rows plus removals — no DOM, no server.

**Files:**
- Modify: `tools/DataEditor/src/groups.js`
- Test: `tools/DataEditor/test/groups.test.js`

**Step 1: Write the failing test**

```js
// ------------------------------------------------------------------ ops

function opsFor(sheet, present, gone) {
  return Groups.ops(schemaOf(sheet), present, gone || [], {});
}

const LOADED = (npcId, itemId, rate) => {
  const values = {};
  schemaOf('NPC Drops').columns.forEach((c) => { values[c.name] = ''; });
  values.npc_template_id = String(npcId);
  values.item_template_id = String(itemId);
  values.stack = '1';
  values.droprate = rate;
  return values;
};

test('an untouched row produces no operation at all', () => {
  // The single-record rule, carried into the batch: a cell where posted equals loaded is never
  // written, so another editor's concurrent change to it survives.
  const row = LOADED(1, 10, '0.25');
  const ops = opsFor('NPC Drops', [{ rowNumber: 2, values: row, loaded: row }]);
  assert.deepEqual(ops.writes, []);
  assert.deepEqual(ops.appends, []);
  assert.deepEqual(ops.deletes, []);
});

test('an edited row produces a write carrying the loaded snapshot', () => {
  const loaded = LOADED(1, 10, '0.25');
  const values = LOADED(1, 10, '0.50');
  const ops = opsFor('NPC Drops', [{ rowNumber: 2, values, loaded }]);
  assert.equal(ops.writes.length, 1);
  assert.equal(ops.writes[0].row, 2);
  assert.ok(Array.isArray(ops.writes[0].loaded), 'the snapshot must be posted');
  assert.equal(ops.writes[0].cells.length, schemaOf('NPC Drops').columns.length);
});

test('a new row produces an append with no snapshot', () => {
  const ops = opsFor('NPC Drops', [{ rowNumber: 0, values: LOADED(1, 99, '0.10'), loaded: null }]);
  assert.deepEqual(ops.writes, []);
  assert.equal(ops.appends.length, 1);
  assert.equal(ops.appends[0].loaded, undefined);
});

test('a removed row produces a delete carrying its snapshot', () => {
  const loaded = LOADED(1, 10, '0.25');
  const ops = opsFor('NPC Drops', [], [{ rowNumber: 3, loaded }]);
  assert.equal(ops.deletes.length, 1);
  assert.equal(ops.deletes[0].row, 3);
  assert.equal(ops.deletes[0].loaded.length, schemaOf('NPC Drops').columns.length);
});

test('cells are ordered by the schema, which is the sheet column order', () => {
  // The importer reads cells by index (CsvToSqlBase.cs:35), so this ordering is the contract.
  const ops = opsFor('NPC Drops', [{ rowNumber: 0, values: LOADED(1, 99, '0.10'), loaded: null }]);
  const names = schemaOf('NPC Drops').columns.map((c) => c.name);
  assert.equal(ops.appends[0].cells[names.indexOf('item_template_id')], '99');
  assert.equal(ops.appends[0].cells[names.indexOf('npc_template_id')], '1');
});

test('a blank optional cell is posted as null so the SQL default applies', () => {
  // Blank means "use the default" (CsvToSqlBase.cs skips empty cells). Writing 0 would pin a
  // value that was tracking the default.
  const values = LOADED(1, 99, '');
  const ops = opsFor('NPC Drops', [{ rowNumber: 0, values, loaded: null }]);
  const at = schemaOf('NPC Drops').columns.map((c) => c.name).indexOf('droprate');
  assert.equal(ops.appends[0].cells[at], null);
});

test('ops names the sheet, its id column and its Text columns', () => {
  const ops = opsFor('NPC Drops', []);
  assert.equal(ops.sheet, 'NPC Drops');
  // -1: NPC Drops has no pk, and its column A is an FK that legitimately repeats.
  assert.equal(ops.idColumnIndex, -1);
  assert.deepEqual(ops.textColumns, []);
});

test('ops reports the id column of a sheet that has a pk', () => {
  const ops = Groups.ops(schemaOf('Quest Reqs'), [], [], {});
  assert.equal(ops.idColumnIndex, 0);
});

test('ops reports Text columns so the server pins their format', () => {
  const ops = Groups.ops(schemaOf('Quest Reqs'), [], [], {});
  const names = schemaOf('Quest Reqs').columns.map((c) => c.name);
  ops.textColumns.forEach((i) => {
    assert.equal(schemaOf('Quest Reqs').columns[i].kind, 'Text', names[i]);
  });
});

// ------------------------------------------------------------------ validate

test('validate reports a problem per row, keyed to the row', () => {
  const bad = LOADED(1, 10, 'not a number');
  const result = Groups.validate(schemaOf('NPC Drops'),
                                 [{ rowNumber: 2, values: bad, loaded: bad }], {});
  assert.equal(result.ok, false);
  assert.equal(result.rows[0].errors.length, 1);
  assert.equal(result.rows[0].errors[0].column, 'droprate');
});

test('validate passes a row whose parent cell is filled in behind the scenes', () => {
  // npc_template_id is required and never on screen. Validating the collected record without
  // it would report every row as broken.
  const row = LOADED(1, 10, '0.25');
  assert.equal(Groups.validate(schemaOf('NPC Drops'),
                               [{ rowNumber: 2, values: row, loaded: row }], {}).ok, true);
});

test('validate flags rows that duplicate another row in the same group', () => {
  // A warning, not a refusal: two Map Required Items for one item is meaningless, but two drops
  // of one item for one NPC is arguable, and the editor is not the place to settle it.
  const a = LOADED(1, 10, '0.25');
  const b = LOADED(1, 10, '0.50');
  const result = Groups.validate(schemaOf('NPC Drops'),
                                 [{ rowNumber: 2, values: a, loaded: a },
                                  { rowNumber: 3, values: b, loaded: b }], {});
  assert.equal(result.ok, true, 'duplicates must not block the save');
  assert.equal(result.duplicates, 2);
});
```

**Step 2: Run to verify it fails**

Expected: `Groups.ops is not a function`.

**Step 3: Implement**

Add to `src/groups.js`:

```js
  // The record as an array in schema column order — which IS the sheet's column order, because
  // the importer reads cells by index (CsvToSqlBase.cs:35). A cell Validation says not to write
  // becomes null, so a blank optional cell stays blank and the column's SQL default applies;
  // writing 0 instead would pin a value that was tracking the default.
  function cellsOf(schema, values, idSets) {
    return schema.columns.map(function (c) {
      var check = Validation.validateCell(c, values[c.name], idSets);
      return check.write ? values[c.name] : null;
    });
  }

  function snapshotOf(schema, loaded) {
    return schema.columns.map(function (c) { return str(loaded[c.name]); });
  }

  /// One sheet's op-set for saveBatch, from the rows on screen and the rows removed.
  ///
  /// `present` is collect()'s output, `gone` is removed()'s. A row whose every cell still equals
  /// what it was loaded with yields NOTHING — the server would skip it anyway, and leaving it out
  /// keeps the reported count honest.
  function ops(schema, present, gone, idSets) {
    var writes = [];
    var appends = [];

    (present || []).forEach(function (row) {
      var cells = cellsOf(schema, row.values, idSets);

      if (row.rowNumber > 0 && row.loaded) {
        var loaded = snapshotOf(schema, row.loaded);
        var changed = false;
        for (var i = 0; i < cells.length; i++) {
          if (str(cells[i]) !== loaded[i]) { changed = true; break; }
        }
        if (changed) writes.push({ row: row.rowNumber, cells: cells, loaded: loaded });
        return;
      }

      appends.push({ cells: cells });
    });

    var deletes = (gone || []).map(function (row) {
      return { row: row.rowNumber, loaded: snapshotOf(schema, row.loaded || {}) };
    });

    var pk = schema.columns.filter(function (c) { return c.pk; })[0];
    var textColumns = [];
    schema.columns.forEach(function (c, i) { if (c.kind === 'Text') textColumns.push(i); });

    return {
      sheet: schema.sheet,
      // -1 for the eight grouped sheets with no pk: their column A is an Id-kind FK that
      // legitimately repeats, and 0 would make the server reject every second drop or spawn.
      idColumnIndex: pk ? schema.columns.indexOf(pk) : -1,
      textColumns: textColumns,
      writes: writes,
      appends: appends,
      deletes: deletes,
    };
  }

  /// Every row validated, plus a count of rows that duplicate another row in the group.
  ///
  /// ownId is null throughout: a child row's pk is allocated by addRow and never edited, so the
  /// duplicate-id check has nothing to exempt.
  function validate(schema, present, idSets) {
    var rows = (present || []).map(function (row) {
      var result = Validation.validateRecord(schema.columns, row.values, idSets, null);
      return { rowNumber: row.rowNumber, errors: result.errors };
    });

    // A DUPLICATE IS EVERY COLUMN BUT THE PK. Two rows differing only in an id nothing refers to
    // are the same row twice.
    var seen = {};
    var duplicates = 0;
    (present || []).forEach(function (row) {
      var key = schema.columns.filter(function (c) { return !c.pk; })
        .map(function (c) { return str(row.values[c.name]); }).join(' ');
      seen[key] = (seen[key] || 0) + 1;
      if (seen[key] === 2) duplicates += 2;
      else if (seen[key] > 2) duplicates += 1;
    });

    return {
      ok: rows.every(function (r) { return r.errors.length === 0; }),
      rows: rows,
      duplicates: duplicates,
    };
  }
```

Add `ops` and `validate` to the exports.

**Step 4: Run**

```bash
node --test tools/DataEditor/test/*.test.js
```

Expected: **994 tests, 984 pass, 0 fail, 10 skipped.**

**Step 5: Commit**

```bash
git add tools/DataEditor/src/groups.js tools/DataEditor/test/groups.test.js
git commit -m "feat(editor): turn an open group into a saveBatch op-set"
```

---

## Task 5: Wire the group path into `app.js`

The one branch, the button modes, the staleness token, the save and the reload discipline.

**Files:**
- Modify: `tools/DataEditor/src/app.js:66-107` (state), `:118` (ctx), `:327` (clearForm), `:370` (openSheet), `:505` (newRecord), `:899` (save), `:1242` (init), `:1272` (exports)
- Modify: `tools/DataEditor/Editor.html` (the group panel's own Save)
- Test: `tools/DataEditor/test/app.test.js`

**Step 1: Write the failing test**

`test/app.test.js`'s `makeServer` needs a `saveBatch`. Add it beside `writeRow`, recording what it was asked to do:

```js
    saveBatch(batch) {
      if (opts.saveBatchFails) throw new Error('batch boom');
      batches.push(batch);
      return batch.map((entry) => ({
        sheet: entry.sheet,
        written: (entry.writes || []).length,
        appended: (entry.appends || []).length,
        deleted: (entry.deletes || []).length,
      }));
    },
```

with `const batches = [];` beside `const writes = []` and `batches` added to whatever `makeServer` returns alongside `writes`.

Then append:

```js
// --- grouped sheets --------------------------------------------------------------------------

test('opening a grouped sheet lists parents, not rows', () => {
  const run = install({ 'NPC Drops': [DROP(1, 10), DROP(1, 20), DROP(2, 30)],
                        NPCs: [NPC(1, 'Mouse'), NPC(2, 'Bat')] });
  App.openSheet('NPC Drops');
  run.flush();

  const records = document.getElementById('records').querySelectorAll('.record');
  assert.deepEqual([...records].map((n) => n.textContent),
                   ['1 — Mouse (2)', '2 — Bat (1)']);
});

test('opening an ungrouped sheet is unchanged', () => {
  const run = install({ Items: [ITEM(1, 'Sword'), ITEM(2, 'Shield')] });
  App.openSheet('Items');
  run.flush();
  const records = document.getElementById('records').querySelectorAll('.record');
  assert.equal(records.length, 2);
  assert.match(records[0].textContent, /Sword/);
});

test('clicking a parent opens all of its rows at once', () => {
  const run = install({ 'NPC Drops': [DROP(1, 10), DROP(1, 20), DROP(2, 30)],
                        NPCs: [NPC(1, 'Mouse')], Items: [ITEM(10, 'Cheese'), ITEM(20, 'Tail')] });
  App.openSheet('NPC Drops');
  run.flush();
  fire(document.getElementById('records').querySelectorAll('.record')[0], 'click');

  assert.equal(document.getElementById('form').querySelectorAll('[data-group-row]').length, 2);
});

test('saving a group posts one batch for the sheet', () => {
  const run = install({ 'NPC Drops': [DROP(1, 10)], NPCs: [NPC(1, 'Mouse')],
                        Items: [ITEM(10, 'Cheese')] });
  App.openSheet('NPC Drops');
  run.flush();
  fire(document.getElementById('records').querySelectorAll('.record')[0], 'click');

  const cell = document.getElementById('form').querySelectorAll('[name=droprate]')[0];
  cell.value = '0.75';
  App.save();
  run.flush();

  const call = run.calls.filter((c) => c.name === 'saveBatch').pop();
  assert.ok(call, 'a group save must go through saveBatch, not writeRow');
  assert.equal(call.args[0].length, 1);
  assert.equal(call.args[0][0].sheet, 'NPC Drops');
  assert.equal(call.args[0][0].writes.length, 1);
});

test('a group save re-reads the sheet afterwards', () => {
  // Deletion shifts every row below it, so no cached row number survives a save. A reload is
  // the only honest position, and it is unconditional rather than only-when-something-was-deleted
  // so there is one path to get right.
  const run = install({ 'NPC Drops': [DROP(1, 10)], NPCs: [NPC(1, 'Mouse')],
                        Items: [ITEM(10, 'Cheese')] });
  App.openSheet('NPC Drops');
  run.flush();
  fire(document.getElementById('records').querySelectorAll('.record')[0], 'click');
  const before = run.calls.filter((c) => c.name === 'readSheet').length;

  document.getElementById('form').querySelectorAll('[name=droprate]')[0].value = '0.75';
  App.save();
  run.flush();

  assert.ok(run.calls.filter((c) => c.name === 'readSheet').length > before);
});

test('a FAILED group save also re-reads the sheet', () => {
  // Load-bearing, and easy to miss: without it a retry after a batch that threw part-way would
  // re-append rows that already landed. After the reload the diff sees them as existing rows.
  const run = install({ 'NPC Drops': [DROP(1, 10)], NPCs: [NPC(1, 'Mouse')],
                        Items: [ITEM(10, 'Cheese')] }, { saveBatchFails: true });
  App.openSheet('NPC Drops');
  run.flush();
  fire(document.getElementById('records').querySelectorAll('.record')[0], 'click');
  const before = run.calls.filter((c) => c.name === 'readSheet').length;

  document.getElementById('form').querySelectorAll('[name=droprate]')[0].value = '0.75';
  App.save();
  run.flush();

  assert.match(document.getElementById('status').textContent, /boom/);
  assert.ok(run.calls.filter((c) => c.name === 'readSheet').length > before);
});

test('a group save refuses while an invalid row is on screen', () => {
  const run = install({ 'NPC Drops': [DROP(1, 10)], NPCs: [NPC(1, 'Mouse')],
                        Items: [ITEM(10, 'Cheese')] });
  App.openSheet('NPC Drops');
  run.flush();
  fire(document.getElementById('records').querySelectorAll('.record')[0], 'click');

  document.getElementById('form').querySelectorAll('[name=droprate]')[0].value = 'nonsense';
  App.save();
  run.flush();

  assert.equal(run.calls.filter((c) => c.name === 'saveBatch').length, 0);
  assert.match(document.getElementById('status').textContent, /problem/);
});

test('switching sheets while a group read is in flight discards it', () => {
  // The token discipline app.js is built around (app.js:52-65), one level down. Without it the
  // reply for NPC Drops renders its parents under whatever sheet is now open.
  const run = install({ 'NPC Drops': [DROP(1, 10)], NPCs: [NPC(1, 'Mouse')],
                        Items: [ITEM(1, 'Sword')] });
  App.openSheet('NPC Drops');
  App.openSheet('Items');
  run.flush();

  assert.equal(App.__state.sheetName, 'Items');
  assert.equal(document.getElementById('form').querySelectorAll('[data-group-row]').length, 0);
});

test('opening a second group discards the first one pending controls', () => {
  const run = install({ 'NPC Drops': [DROP(1, 10), DROP(2, 20)],
                        NPCs: [NPC(1, 'Mouse'), NPC(2, 'Bat')], Items: [ITEM(10, 'Cheese')] });
  App.openSheet('NPC Drops');
  run.flush();
  const records = document.getElementById('records').querySelectorAll('.record');
  fire(records[0], 'click');
  fire(records[1], 'click');
  run.flush();

  const rows = document.getElementById('form').querySelectorAll('[data-group-row]');
  assert.equal(rows.length, 1, 'only the second group may be on screen');
});

test('the header Save is hidden on a grouped sheet and back on an ungrouped one', () => {
  const run = install({ 'NPC Drops': [DROP(1, 10)], NPCs: [NPC(1, 'Mouse')],
                        Items: [ITEM(1, 'Sword')] });
  App.openSheet('NPC Drops');
  run.flush();
  assert.equal(document.getElementById('save').hidden, true);

  App.openSheet('Items');
  run.flush();
  assert.equal(document.getElementById('save').hidden, false);
});

test('New opens the parent picker on a grouped sheet', () => {
  const run = install({ 'NPC Drops': [DROP(1, 10)], NPCs: [NPC(1, 'Mouse'), NPC(2, 'Bat')] });
  App.openSheet('NPC Drops');
  run.flush();
  App.newRecord();

  // Every parent is offered, so there is one way to reach any of them — the ones that already
  // have a group jump to it rather than starting a second.
  assert.ok(document.getElementById('modal').querySelectorAll('[data-parent]').length >= 2);
});

test('adding the first row to a parent with none appends it', () => {
  const run = install({ 'NPC Drops': [DROP(1, 10)], NPCs: [NPC(1, 'Mouse'), NPC(2, 'Bat')],
                        Items: [ITEM(10, 'Cheese')] });
  App.openSheet('NPC Drops');
  run.flush();
  App.openGroup('2');
  document.getElementById('form').querySelectorAll('[data-add]')[0].dispatchEvent(
    new Event('click', { bubbles: true }));
  document.getElementById('form').querySelectorAll('[name=item_template_id]')[0].value = '10';
  App.save();
  run.flush();

  const call = run.calls.filter((c) => c.name === 'saveBatch').pop();
  assert.equal(call.args[0][0].appends.length, 1);
  const names = schemaOf('NPC Drops').columns.map((c) => c.name);
  assert.equal(call.args[0][0].appends[0].cells[names.indexOf('npc_template_id')], '2');
});
```

`install(...)` is `test/app.test.js`'s existing harness helper — read its definition and match the call shape; the second argument is `makeServer`'s `options`.

**Step 2: Run to verify it fails**

```bash
node --test tools/DataEditor/test/app.test.js
```

**Step 3: Implement**

**Editor.html** — the group panel builds its own Save inside `#form`, so no new shell element is needed, but `#save` must be hideable. It already is (`FakeNode` supports `hidden`, `test/fake-dom.js:264`). No markup change beyond the include added in Task 2.

**`src/app.js`:**

Add to `state` (`:66-107`), beside `formToken`:

```js
    // The open GROUP, and the same guard one level along from formToken: a group's controls are
    // built from picker data that may still be in flight, so clicking group A then group B must
    // discard A's render rather than let it land under B's parent id. Bumped by openGroup.
    groupToken: 0,
    group: null,       // the open group entry from Groups.build, or null
    groups: [],        // every group of the open sheet
```

In `clearForm` (`:327`), beside the `formToken` bump, add `state.groupToken++; state.group = null;`.

In `openSheet` (`:370`), replace the `renderList()` call inside the success handler (`:400`) with:

```js
          if (Layout.groupParent(sheetName)) renderGroups();
          else renderList();
```

and add, near `renderList`:

```js
  // Whether the OPEN sheet is edited a group at a time. Read from Layout rather than from a flag
  // set at open time, so there is one answer and it cannot go stale.
  function grouped() {
    return !!(state.schema && Layout.groupParent(state.schema.sheet));
  }

  // The parent list. Built from the rows the sheet read plus the parent sheet's id + name list,
  // which loadReferencedSheets has already fetched for the FK pickers — so this costs no extra
  // round trip.
  function renderGroups() {
    var list = document.getElementById('records');
    list.innerHTML = '';

    var parent = Groups.parentOf(state.schema);
    state.groups = Groups.build(state.schema, state.rows,
                                state.pickerData[parent.ref]);

    state.groups.forEach(function (group) {
      var button = Forms.el('button', { type: 'button', class: 'record' },
                            group.label + ' (' + group.count + ')');
      button.addEventListener('click', function () { openGroup(group.key); });
      list.appendChild(button);
    });
  }

  /// Opens one group's table. `key` is the folded parent id, as Groups.build reports it.
  function openGroup(key) {
    if (!grouped()) return;

    var group = state.groups.filter(function (g) { return g.key === key; })[0];
    // A parent with no rows yet has no group; the panel opens empty and the first Add creates
    // its first row. Nothing is written until the user saves, so no empty group is ever made by
    // accident.
    if (!group) {
      var entry = (state.pickerData[Groups.parentOf(state.schema).ref] || [])
        .filter(function (e) { return Groups.idKey(e.id) === key; })[0];
      group = { key: key, id: key, count: 0, orphan: !entry, rows: [],
                label: entry ? key + ' — ' + entry.name : key };
    }

    clearPreviews();
    var token = ++state.groupToken;
    state.group = group;
    // The single-record bookkeeping must not be left describing a record that is no longer on
    // screen — save() branches on grouped(), but publishCheck and the preview path do not.
    state.rowNumber = 0;
    state.loaded = {};

    var container = document.getElementById('form');
    container.innerHTML = '';

    loadReferencedSheets(function () {
      // The one staleness check for the group path. loadReferencedSheets' own handlers do not
      // guard — an id + name list is group-agnostic and worth keeping — but RENDERING must not
      // happen for a group the user has already moved off.
      if (token !== state.groupToken) return;

      Groups.render({
        container: container, schema: state.schema, group: group, ctx: ctx(), ids: state.ids,
      });

      var add = Forms.el('button', { type: 'button', 'data-add': '' }, '+ Add row');
      add.addEventListener('click', function () { Groups.addRow(container); });
      container.appendChild(add);

      // The group owns its Save. The header's is for the single-record form and is hidden while
      // a grouped sheet is open, so there is exactly one Save on screen at a time.
      var save = Forms.el('button', { type: 'button', 'data-save-group': '' }, 'Save group');
      save.addEventListener('click', saveGroup);
      container.appendChild(save);

      status(group.count + ' row' + (group.count === 1 ? '' : 's') + ' in ' + group.label);
    });
  }

  function saveGroup() {
    if (state.saving) { status('Still saving — one moment', true); return; }

    var container = document.getElementById('form');
    if (!container.__group) {
      status('Open a group first — click one in the list.', true);
      return;
    }

    var present = Groups.collect(container, state.schema);
    var gone = Groups.removed(container);

    // Same gate, same reason as the single-record path (app.js:940): validation waves an fk
    // through when its id set is absent, so a list that FAILED to load must block the save.
    var refs = {};
    state.schema.columns.forEach(function (c) {
      if (c.ref && state.refErrors.indexOf(c.ref) !== -1) refs[c.ref] = true;
    });
    var unverified = Object.keys(refs);
    if (unverified.length) {
      status('Cannot check these rows\' ids against ' + unverified.join(' and ') +
             ' — that list failed to load, so saving now could store an id that does not ' +
             'exist. Reloading it; try saving again in a moment.', true);
      retryReferencedSheets(unverified);
      return;
    }

    var check = Groups.validate(state.schema, present, state.idSets);
    var rows = container.querySelectorAll('[data-group-row]');
    for (var i = 0; i < rows.length && i < check.rows.length; i++) {
      Forms.showErrors(rows[i], check.rows[i].errors);
    }

    if (!check.ok) {
      var count = check.rows.reduce(function (n, r) { return n + r.errors.length; }, 0);
      status(count + ' problem(s) — fix them before saving', true);
      return;
    }

    var batch = [Groups.ops(state.schema, present, gone, state.idSets)];
    if (!batch[0].writes.length && !batch[0].appends.length && !batch[0].deletes.length) {
      status('Nothing to save.');
      return;
    }

    status('Saving…');
    state.saving = true;
    var savedSheet = state.sheetName;
    var savedToken = state.sheetToken;
    var savedKey = state.group ? state.group.key : null;

    // ALWAYS RELOAD, on success AND on failure. Deleting a row shifts every row below it, so no
    // cached row number survives a success — and a batch that threw part-way may have landed
    // some of its appends, so a retry without a reload would append them again. After the reload
    // the diff sees them as existing rows and leaves them alone.
    function reload() {
      delete state.pickerData[savedSheet];
      delete state.idSets[savedSheet];
      if (savedToken !== state.sheetToken) return;
      openSheet(savedSheet);
      // Reopening the same group is best-effort: openSheet's read is asynchronous and the group
      // may no longer exist (its last row deleted). openGroup is a no-op for an unknown key on a
      // sheet that is no longer grouped, and builds an empty panel otherwise.
      if (savedKey !== null) state.reopenGroup = savedKey;
    }

    google.script.run
      .withFailureHandler(function (e) {
        state.saving = false;
        if (savedToken === state.sheetToken) status(e.message, true);
        reload();
      })
      .withSuccessHandler(function (results) {
        state.saving = false;
        var r = (results && results[0]) || { written: 0, appended: 0, deleted: 0 };
        var note = check.duplicates
          ? ' ' + check.duplicates + ' rows duplicate another row in this group.'
          : '';
        if (savedToken === state.sheetToken) {
          status('Saved ' + r.written + ' edited, ' + r.appended + ' added, ' + r.deleted +
                 ' removed. Run /updatesql then /reloadsql in game to publish.' + note,
                 !!check.duplicates);
        }
        reload();
      })
      .saveBatch(batch);
  }
```

In `openSheet`'s success handler, after `renderGroups()`, honour a pending reopen:

```js
          if (state.reopenGroup !== null && state.reopenGroup !== undefined) {
            var key = state.reopenGroup;
            state.reopenGroup = null;
            openGroup(key);
          }
```

and seed `reopenGroup: null` in `state`.

Route the header buttons. In `save()` (`:899`), as the first line after the `state.schema` guard:

```js
    // A grouped sheet has no single-record form; its panel owns its own Save. Routed rather than
    // refused so the header button and the keyboard path both land somewhere sensible.
    if (grouped()) { saveGroup(); return; }
```

In `newRecord()` (`:505`), likewise:

```js
    if (grouped()) { openParentPicker(); return; }
```

with a picker that lists every parent:

```js
  // The New-group picker. Every parent is offered, not only the ones with no rows: two controls
  // for "reach a parent" is one more than the job needs, so picking one that already has a group
  // simply opens it.
  function openParentPicker() {
    var parent = Groups.parentOf(state.schema);
    var modal = document.getElementById('modal');
    modal.innerHTML = '';
    modal.hidden = false;

    (state.pickerData[parent.ref] || []).forEach(function (entry) {
      var key = Groups.idKey(entry.id);
      var has = state.groups.filter(function (g) { return g.key === key; })[0];
      var button = Forms.el('button', { type: 'button', 'data-parent': key },
                            key + ' — ' + entry.name + (has ? ' (' + has.count + ')' : ''));
      button.addEventListener('click', function () {
        modal.hidden = true;
        modal.innerHTML = '';
        openGroup(key);
      });
      modal.appendChild(button);
    });
  }
```

Hide the header Save on a grouped sheet. In `openSheet`, after `state.schema` is set (`:380`):

```js
    // One Save on screen at a time: the group panel builds its own, so the header's would be a
    // second button doing the same thing from a different place.
    document.getElementById('save').hidden = !!Layout.groupParent(sheetName);
    document.getElementById('new-record').textContent =
      Layout.groupParent(sheetName) ? 'New group' : 'New';
```

Export `openGroup` and `saveGroup` from `App` (`:1272`), alongside the existing internals-for-tests.

**Step 4: Run**

```bash
node --test tools/DataEditor/test/*.test.js
```

Expected: **1006 tests, 996 pass, 0 fail, 10 skipped.**

**Step 5: Commit**

```bash
git add tools/DataEditor/src/app.js tools/DataEditor/test/app.test.js
git commit -m "feat(editor): edit join sheets a group at a time"
```

---

## Task 6: Large groups and duplicate rows

Two finishing behaviours, both about a group being bigger or messier than the happy path.

**Files:**
- Modify: `tools/DataEditor/src/groups.js`, `tools/DataEditor/Editor.html` (styles)
- Test: `tools/DataEditor/test/groups.test.js`

**Step 1: Write the failing test**

```js
// ------------------------------------------------------------------ large groups

test('render draws at most the first 100 rows', () => {
  // NPC Spawns is 4,322 rows grouped by map, and how many land on the busiest map is not
  // knowable from the repo. Several hundred FK typeaheads in one panel is worth not finding out
  // the hard way; the rest are one click away.
  const rows = [];
  for (let i = 0; i < 150; i++) rows.push(DROP(1, i + 1));
  const { container } = panelFor('NPC Drops', rows, NPCS);
  assert.equal(container.querySelectorAll('[data-group-row]').length, 100);
  assert.equal(container.querySelectorAll('[data-show-all]').length, 1);
});

test('show all draws the rest', () => {
  const rows = [];
  for (let i = 0; i < 150; i++) rows.push(DROP(1, i + 1));
  const { container } = panelFor('NPC Drops', rows, NPCS);
  fire(container.querySelectorAll('[data-show-all]')[0], 'click');
  assert.equal(container.querySelectorAll('[data-group-row]').length, 150);
  assert.equal(container.querySelectorAll('[data-show-all]').length, 0);
});

test('a group under the cap gets no show-all control', () => {
  const { container } = panelFor('NPC Drops', [DROP(1, 10)], NPCS);
  assert.equal(container.querySelectorAll('[data-show-all]').length, 0);
});

test('collect reads the undrawn rows too', () => {
  // THE ONE THAT MATTERS. If the cap were a cap on the MODEL rather than on the rendering, a
  // save from a capped group would post 100 rows and the other 50 would look like removals —
  // silently deleting them. Undrawn rows must survive a save untouched.
  const rows = [];
  for (let i = 0; i < 150; i++) rows.push(DROP(1, i + 1));
  const { container, schema } = panelFor('NPC Drops', rows, NPCS);
  const ops = Groups.ops(schema, Groups.collect(container, schema),
                         Groups.removed(container), {});
  assert.deepEqual(ops.deletes, [], 'undrawn rows must not be posted as deletions');
  assert.deepEqual(ops.writes, [], 'undrawn rows are unchanged and must produce no write');
});

// ------------------------------------------------------------------ duplicates

test('duplicate rows are marked on screen', () => {
  const { container } = panelFor('NPC Drops', [DROP(1, 10, '0.25'), DROP(1, 10, '0.25')], NPCS);
  Groups.markDuplicates(container, schemaOf('NPC Drops'));
  assert.equal(container.querySelectorAll('.duplicate').length, 2);
});

test('marking duplicates twice does not leave stale marks', () => {
  const { container, schema } = panelFor('NPC Drops',
                                         [DROP(1, 10, '0.25'), DROP(1, 10, '0.25')], NPCS);
  Groups.markDuplicates(container, schema);
  container.querySelectorAll('[name=droprate]')[1].value = '0.50';
  Groups.markDuplicates(container, schema);
  assert.equal(container.querySelectorAll('.duplicate').length, 0);
});
```

**Step 2: Run to verify it fails**

Expected: 150 rows drawn, no `[data-show-all]`, `Groups.markDuplicates is not a function`.

**Step 3: Implement**

In `src/groups.js`, add the cap near the top:

```js
  // How many rows a group draws before it asks. Every row is a handful of controls, and an FK
  // cell is a typeahead with its own listeners — cheap individually, not in the hundreds.
  //
  // A CAP ON DRAWING, NOT ON THE MODEL. The undrawn rows stay in state.pending and are put back
  // by collect(), so a save from a capped group leaves them exactly as they were. Capping the
  // model instead would post the drawn rows and make the rest look like removals.
  var RENDER_CAP = 100;
```

In `render`, after the rows loop, keep the overflow and offer it:

```js
    var all = group ? group.rows : [];
    var shown = all.slice(0, RENDER_CAP);
    state.pending = all.slice(RENDER_CAP);

    shown.forEach(function (row) {
      body.appendChild(buildRow(state, columns, pk, row.rowNumber, row.values));
    });

    if (state.pending.length) {
      var more = Forms.el('button', { type: 'button', 'data-show-all': '' },
                          'Show all ' + all.length + ' rows');
      more.addEventListener('click', function () {
        state.pending.forEach(function (row) {
          body.appendChild(buildRow(state, columns, pk, row.rowNumber, row.values));
        });
        state.pending = [];
        if (more.parentNode) more.parentNode.removeChild(more);
      });
      container.appendChild(more);
    }
```

In `collect`, append the undrawn rows after the drawn ones:

```js
    // The rows the cap did not draw, unchanged. `loaded` is the same object as `values`, so the
    // op builder finds nothing changed and emits no write — which is exactly right: the user
    // never saw them.
    (state.pending || []).forEach(function (row) {
      out.push({ rowNumber: row.rowNumber, values: row.values, loaded: row.values });
    });
```

Add the marker:

```js
  /// Marks rows that duplicate another row of the same group. A warning, not a refusal: two
  /// Map Required Items naming one item is meaningless, but two drops of one item for one NPC is
  /// arguable, and the editor is not where that gets settled.
  ///
  /// Every mark is cleared before any is applied, so a duplicate the user has just fixed does not
  /// stay flagged.
  function markDuplicates(container, schema) {
    var state = container.__group;
    if (!state) return 0;

    var rows = state.body.querySelectorAll('[data-group-row]');
    var keys = [];
    var counts = {};

    for (var i = 0; i < rows.length; i++) {
      rows[i].classList.remove('duplicate');
      var values = Forms.collect(rows[i], schema);
      var key = schema.columns.filter(function (c) { return !c.pk; })
        .map(function (c) { return str(values[c.name]); }).join(' ');
      keys.push(key);
      counts[key] = (counts[key] || 0) + 1;
    }

    var marked = 0;
    for (var j = 0; j < rows.length; j++) {
      if (counts[keys[j]] > 1) { rows[j].classList.add('duplicate'); marked++; }
    }
    return marked;
  }
```

Export `markDuplicates` and `RENDER_CAP`.

In `app.js`'s `saveGroup`, call `Groups.markDuplicates(container, state.schema)` just before building the batch, so the status-line count and the on-screen marks come from one pass.

Add to `Editor.html`'s stylesheet, beside the existing `.warn` rule:

```css
  .group-table { display: table; width: 100%; }
  .group-header, .group-row { display: table-row; }
  .group-header > span, .group-cell { display: table-cell; padding: 2px 4px; vertical-align: top; }
  .group-row.duplicate .group-cell { background: #3a3218; }
```

and the dark-scheme override beside the other `.warn` entry.

**Step 4: Run**

```bash
node --test tools/DataEditor/test/*.test.js
```

Expected: **1012 tests, 1002 pass, 0 fail, 10 skipped.**

**Step 5: Commit**

```bash
git add tools/DataEditor/src/groups.js tools/DataEditor/src/app.js \
        tools/DataEditor/Editor.html tools/DataEditor/test/groups.test.js
git commit -m "feat(editor): cap large group renders and flag duplicate rows"
```

---

## Before the branch is done

**Rebuild `dist/`.** `build.mjs` discovers `src/*.js` automatically, so `groups.js` is picked up with no build change — but `dist/` is committed and would otherwise be a version behind.

```bash
cd tools/DataEditor && node build.mjs && cd -
git add tools/DataEditor/dist
git commit -m "build: regenerate the editor bundle"
```

**Live smoke test.** `Code.gs`'s VERIFICATION LIST is the project's record of what only a real spreadsheet can settle; Part 1 added the `saveBatch` entries. Work through these too, and add anything they turn up:

- open NPC Drops: the list reads `1 — Mouse (3)`, not `1 — 1`
- open a group, edit two rows, add one, remove one, save: the sheet matches, and the list refreshes with the new count
- remove the last row of a group: the group disappears from the list
- New group → pick an NPC with no drops → add a row → save: it appends with the right `npc_template_id`
- New group → pick an NPC that already has drops: it opens the existing group rather than a second one
- open a map on NPC Spawns with more than 100 spawns: the cap and "Show all" behave, and a save without clicking "Show all" leaves the undrawn rows untouched
- Quest Reqs: add two rows in one save and confirm they take consecutive ids
- a drop pointing at a deleted NPC: it appears as `— (not in NPCs)` and can be removed
- two tabs on one group: the second save is refused naming the row, and nothing is written
- switch sheets while a big group is loading: nothing from the old sheet renders

## Done when

- [ ] `node --test tools/DataEditor/test/*.test.js` — 0 failures
- [ ] All ten join sheets list parents and edit as a table; Class Info and every ungrouped sheet are unchanged
- [ ] Orphan and blank-parent rows are reachable, editable and deletable
- [ ] A group save posts one `saveBatch` batch and reloads the sheet on success **and** on failure
- [ ] Undrawn rows of a capped group survive a save untouched
- [ ] Duplicate rows are flagged and counted, and do not block the save
- [ ] Exactly one Save button is on screen at a time
- [ ] `dist/` regenerated and committed
