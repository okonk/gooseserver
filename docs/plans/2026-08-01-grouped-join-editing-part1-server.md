# Grouped Join-Table Editing — Part 1: Core Prerequisites and the Server API

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Make the editor's control layer safe to render many rows of one sheet at once, and give `Code.gs` a batched, multi-sheet, all-or-nothing save that can delete rows.

**Architecture:** Two independent halves. Client side, control element ids stop being hardcoded to `'f-' + column.name` and take a prefix from `ctx`, so N copies of one column's control can coexist. Server side, `writeRow`'s per-cell diff is extracted into a pure planning helper, and a new `saveBatch(ops)` plans every sheet in the batch before writing anything, then applies writes → appends → deletes (bottom-up) per sheet under one document lock.

**Tech Stack:** Google Apps Script (`Code.gs`, ES5 — no `let`, `const`, arrow functions or `Array.prototype.includes` in that file), plain-JS client modules under `tools/DataEditor/src/`, `node --test` with a hand-written fake DOM and fake SpreadsheetApp.

**Part 1 of 2.** Part 2 builds the group UI (`Layout.GROUP_PARENT`, `src/groups.js`, `app.js` wiring). Nothing in this plan changes what the editor shows or does — every existing test must stay green throughout.

**Design doc:** `docs/plans/2026-08-01-grouped-join-editing-design.md`

---

## Working agreements

**Worktree:** `/home/hayden/code/illutiagooseserver/.worktrees/grouped-join-editing`, branch `grouped-join-editing`. All paths below are relative to that directory.

**Run tests with the glob, not the directory:**

```bash
node --test tools/DataEditor/test/*.test.js
```

`node --test tools/DataEditor/test/` fails on Node 22 with `MODULE_NOT_FOUND` — it treats the bare directory as a module path. Baseline on this branch: **911 tests, 901 pass, 0 fail, 10 skipped.**

**Do not run `build.mjs` as part of these tasks.** `dist/` is committed and regenerated as its own step; the tests read `src/` and `Code.gs` directly (`test/real-bundles.js` is the exception and it is not touched here).

**`Code.gs` is ES5.** It runs in Apps Script's V8 runtime but the file is written in ES5 throughout, and `test/fake-sheets.js` loads it with `runInContext`. Match the surrounding style: `var`, `function`, no arrow functions.

---

## APIs verified

Every citation below was read in this worktree before this plan was written.

**Client:**
- `Forms.el(tag, attrs, text)` — `src/forms.js:9`
- `Forms.scalarControl(column, rawValue)` — `src/forms.js:157`; hardcoded ids at `:161` (enum `<select>`) and `:180` (text `<input>`)
- `boolControl(column, value)` — `src/forms.js:99`, module-private; hardcoded ids at `:107` (bad-value text input) and `:115` (checkbox)
- `columnControl(opts)` with `opts = { column, ctx, sheet, values, effective }` — `src/forms.js:214`; routes to `Pickers.fkControl` at `:222`, `Pickers.partControl` at `:227`, `scalarControl` at `:237`
- `Forms` exports — `src/forms.js:427-436`: `render, collect, showErrors, scalarControl, placeholderFor, defaultOf, effective, el`. **`columnControl` is not among them.**
- `Pickers.fkControl(column, value, ctx)` — `src/pickers.js:212`; `listId` at `:214`, input `id` at `:218`, `aria-controls: listId` at `:229`. Exported at `src/pickers.js:931`.
- `ctx` already carries an optional `gallery` and is read defensively as `ctx && ctx.gallery` — `src/forms.js:233`. `ctx.idPrefix` follows that established pattern.
- `Layout.partGraphic(sheet, column)` — `src/layout.js:224`, backed by `PART_GRAPHICS` at `src/layout.js:136-152`, which contains **only** `Items`, `NPCs` and `Spell Effects`. No sheet that Part 2 groups has a part graphic.
- No sheet that Part 2 groups has a composite either (per `schema.js`: composites exist only on Items, NPCs, Quests, Spells, Spell Effects, Combinations).

**Server (`Code.gs`):**
- `requireSheet_(sheetName)` — `:163`
- `headerWidth_(sheet)` — `:178`, width from the header row, trailing blanks trimmed
- `isBlank_(value)` — `:189`, true for `null`/`undefined`/`''`
- `isDate_(value)` — `:205`, tag test not `instanceof`
- `cellText_(raw, display)` — `:238`. **Reads `display` only for a Date** (`:240`); everything else is `String(raw)`. This is why one raw read plus a conditional display read is sufficient.
- `idKey_(value)` — `:261`, folds `651`, `'651'`, `'651.00'`, `'1,024'`, `' 651 '` to one key
- `readSheet` — `:281`; its raw-scan-then-conditional-display-read pattern is at `:300-312` and is copied by `planSheetOps_` below
- `writeRow(sheetName, rowNumber, cells, idColumnIndex, options)` — `:430`; the per-cell diff loop to extract is `:554-600`, the duplicate-id scan is `:518-528`, `insertRowsAfter` growth is `:535-536`
- Accepted-risk note "No LockService" — `:120-123`. Task 3 closes it and the note must be updated.

**Real Apps Script surfaces the fake must mirror:**
- `Sheet.deleteRows(rowPosition, howMany)` — 1-based position, rows below shift up, grid shrinks
- `Sheet.insertRowsAfter(afterPosition, howMany)` — already modelled at `test/fake-sheets.js:125`
- `LockService.getDocumentLock()` → `Lock`, with `waitLock(timeoutInMillis)` (throws on timeout), `releaseLock()`, `hasLock()`

**Fake (`test/fake-sheets.js`):**
- `FakeSheet` constructor — `:66`; `this.writes` and `this.reads` are the assertion surfaces
- `getRange` throws past the grid — `:113-123`
- `insertRowsAfter` — `:125-130`
- `FakeRange.setValues` copies arrays across the realm boundary with `Array.from` — `:177-194`. **Any new array crossing out of the vm needs the same treatment.**
- `loadCodeGs(sheetsByName, options = {})` — `:199`; sandbox at `:206-211`; returns `{ sheets, flushes, readSheet, readSheetIndex, writeRow }` at `:223-229`
- Header comment lists LockService among the things deliberately absent — `:17`. Task 2 makes that stale; update it.

---

## Task 1: Give controls a per-instance id prefix

Today every control for a column gets `id="f-" + column.name`. One record on screen, one control, no problem. A group table renders the same column once per row, so the ids duplicate and every `<label for>` resolves to the first row's input. This task adds an optional prefix carried on `ctx`, defaulting to `'f-'` so nothing visible changes.

**Scope note:** only `scalarControl`, `boolControl`, `fkControl` and `columnControl` are threaded. `Pickers.graphicControl` (`src/pickers.js:552`), `Pickers.partControl` (`src/pickers.js:815`) and `Composites` (`src/composites.js:623-624`) keep the hardcoded prefix, because no sheet Part 2 groups has a composite or a part graphic (verified above). Threading them would be speculative work for an unreachable case. Part 2 does not need it; a future parent-centric editor embedding a composite sheet would.

**Files:**
- Modify: `tools/DataEditor/src/forms.js:99`, `:107`, `:115`, `:157`, `:161`, `:171`, `:180`, `:214`, `:237`, `:427-436`
- Modify: `tools/DataEditor/src/pickers.js:212-230`
- Test: `tools/DataEditor/test/forms.test.js`, `tools/DataEditor/test/pickers.test.js`

**Step 1: Write the failing tests**

Append to `tools/DataEditor/test/forms.test.js`. Match the file's existing helpers — it already has a `column(sheet, name)` helper used at `:402` and `:445`; reuse it rather than inventing schema literals.

```js
// ------------------------------------------------------------------ id prefixes
//
// A group table renders one column's control once per row. With the id hardcoded to
// 'f-' + name every row's input carried the SAME id and every <label for> resolved to
// row 1's — so clicking any row's label focused the first row. The prefix is what makes
// N controls for one column distinguishable; the default is what keeps the single-record
// form byte-identical.

test('scalarControl keeps the f- prefix when none is given', () => {
  assert.equal(Forms.scalarControl(column('Items', 'player_hp'), '5').id, 'f-player_hp');
});

test('scalarControl applies a given prefix to a text input', () => {
  assert.equal(Forms.scalarControl(column('Items', 'player_hp'), '5', 'g3-').id, 'g3-player_hp');
});

test('scalarControl applies a given prefix to an enum select', () => {
  const control = Forms.scalarControl(column('Items', 'item_usetype'), 'Weapon', 'g3-');
  assert.equal(control.tagName, 'SELECT');
  assert.equal(control.id, 'g3-item_usetype');
});

test('scalarControl applies a given prefix to a bool checkbox', () => {
  // boolControl builds a wrapper; the checkbox is the element carrying the id, and the
  // hidden cell beside it carries the name. Only the id moves.
  const wrap = Forms.scalarControl(column('Items', 'lore'), '1', 'g3-');
  const box = wrap.querySelectorAll('input[type=checkbox]')[0];
  assert.equal(box.id, 'g3-lore');
});

test('scalarControl applies a given prefix to a bool cell holding a non-0/1 value', () => {
  // The fallback text input at forms.js:107 carries both the name and the id.
  const wrap = Forms.scalarControl(column('Items', 'lore'), 'maybe', 'g3-');
  const input = wrap.querySelectorAll('input[type=text]')[0];
  assert.equal(input.id, 'g3-lore');
  assert.equal(input.getAttribute('name'), 'lore');
});

test('two controls for one column under different prefixes get different ids', () => {
  // The bug, stated directly.
  const a = Forms.scalarControl(column('Items', 'player_hp'), '1', 'g0-');
  const b = Forms.scalarControl(column('Items', 'player_hp'), '2', 'g1-');
  assert.notEqual(a.id, b.id);
});

test('columnControl is exported', () => {
  // The group table's only entry point into the control layer.
  assert.equal(typeof Forms.columnControl, 'function');
});

test('columnControl passes ctx.idPrefix through to a scalar control', () => {
  const c = column('Items', 'player_hp');
  const control = Forms.columnControl({
    column: c, ctx: { idPrefix: 'g3-' }, sheet: 'Items', values: { player_hp: '5' },
  });
  assert.equal(control.id, 'g3-player_hp');
});

test('columnControl with no ctx keeps the f- prefix', () => {
  const c = column('Items', 'player_hp');
  const control = Forms.columnControl({ column: c, sheet: 'Items', values: { player_hp: '5' } });
  assert.equal(control.id, 'f-player_hp');
});
```

Append to `tools/DataEditor/test/pickers.test.js`, following that file's existing way of building a `ctx`:

```js
// ------------------------------------------------------------------ id prefixes

test('fkControl keeps the f- prefix when ctx names none', () => {
  const c = column('NPC Drops', 'item_template_id');
  const wrap = Pickers.fkControl(c, '7', ctxWith({}));
  const input = wrap.querySelectorAll('input[type=text]')[0];
  assert.equal(input.id, 'f-item_template_id');
  assert.equal(input.getAttribute('aria-controls'), 'f-item_template_id-list');
});

test('fkControl applies ctx.idPrefix to both the input and its listbox', () => {
  // aria-controls must follow the list's id or the combobox announces a listbox that
  // does not exist — and in a group table the unprefixed id would be another row's.
  const c = column('NPC Drops', 'item_template_id');
  const wrap = Pickers.fkControl(c, '7', ctxWith({ idPrefix: 'g3-' }));
  const input = wrap.querySelectorAll('input[type=text]')[0];
  const list = wrap.querySelectorAll('[role=listbox]')[0];
  assert.equal(input.id, 'g3-item_template_id');
  assert.equal(list.id, 'g3-item_template_id-list');
  assert.equal(input.getAttribute('aria-controls'), list.id);
});

test('fkControl still carries the column name on its hidden cell under a prefix', () => {
  // The prefix moves ids only. Forms.collect sweeps [name], so a prefixed NAME would
  // drop the cell out of the record entirely.
  const c = column('NPC Drops', 'item_template_id');
  const wrap = Pickers.fkControl(c, '7', ctxWith({ idPrefix: 'g3-' }));
  const hidden = wrap.querySelectorAll('input[type=hidden]')[0];
  assert.equal(hidden.getAttribute('name'), 'item_template_id');
  assert.equal(hidden.value, '7');
});
```

Read the top of `test/pickers.test.js` first and reuse its existing `column` and ctx-building helpers; if it has no `ctxWith`, add a two-line local helper rather than restructuring the file.

**Step 2: Run the tests to verify they fail**

```bash
node --test tools/DataEditor/test/forms.test.js tools/DataEditor/test/pickers.test.js
```

Expected: the new tests fail. `columnControl is exported` fails on `typeof undefined !== 'function'`; the prefix tests fail with the received id being `f-…` where `g3-…` was expected.

**Step 3: Implement**

In `src/forms.js`, add a resolver just above `boolControl` (line 99):

```js
  // The prefix every control's element id is built from. One definition, because a control and
  // the <label for> pointing at it are built in different places and a second spelling of the
  // default would silently unlink them.
  function idPrefixOf(idPrefix) {
    return typeof idPrefix === 'string' && idPrefix !== '' ? idPrefix : 'f-';
  }
```

Change `boolControl(column, value)` to `boolControl(column, value, idPrefix)` and add `var prefix = idPrefixOf(idPrefix);` as its first line. Replace `'f-' + column.name` at `:107` and `:115` with `prefix + column.name`.

Change `scalarControl(column, rawValue)` to `scalarControl(column, rawValue, idPrefix)` and add `var prefix = idPrefixOf(idPrefix);` after the existing `var value = str(rawValue);`. Replace `'f-' + column.name` at `:161` and `:180` with `prefix + column.name`, and change the bool branch at `:171` to `return boolControl(column, value, prefix);`.

In `columnControl` (`:214`), after `var value = values[column.name];`, add:

```js
    // Carried on ctx rather than as a parameter of its own, exactly as `gallery` is (see the
    // fkControl call below): the group table sets it once per row and every control the row
    // builds inherits it, with no signature growing a parameter that only one caller passes.
    var idPrefix = ctx && ctx.idPrefix;
```

and change the `scalarControl` call at `:237` to `return scalarControl(column, value, idPrefix);`. The `fkControl` call at `:222` already receives `ctx` and needs no change here.

Add `columnControl: columnControl,` to the exports object at `src/forms.js:427`.

In `src/pickers.js`, inside `fkControl` (`:212`), before `var wrap`:

```js
    // See forms.js columnControl for why this rides on ctx.
    var prefix = (ctx && typeof ctx.idPrefix === 'string' && ctx.idPrefix !== '')
      ? ctx.idPrefix
      : 'f-';
```

Replace `'f-' + column.name + '-list'` at `:214` with `prefix + column.name + '-list'`, and `'f-' + column.name` at `:218` with `prefix + column.name`.

**Step 4: Run the full suite**

```bash
node --test tools/DataEditor/test/*.test.js
```

Expected: **923 tests, 913 pass, 0 fail, 10 skipped** (911 + 12 new). Every pre-existing test passes untouched — that is the proof the default is byte-identical.

**Step 5: Commit**

```bash
git add tools/DataEditor/src/forms.js tools/DataEditor/src/pickers.js \
        tools/DataEditor/test/forms.test.js tools/DataEditor/test/pickers.test.js
git commit -m "feat(editor): give form controls a per-instance id prefix"
```

---

## Task 2: Teach the fake SpreadsheetApp to delete rows and hold a lock

`test/fake-sheets.js` models exactly what `Code.gs` calls and nothing else — its header says so and lists LockService as deliberately absent. Tasks 3–5 call both, so the fake gains both first.

**Files:**
- Modify: `tools/DataEditor/test/fake-sheets.js:1-37` (header), `:66-83` (constructor), `:125-130` (beside `insertRowsAfter`), `:199-230` (`loadCodeGs`)
- Create: `tools/DataEditor/test/fake-sheets.test.js`

**Step 1: Write the failing test**

Create `tools/DataEditor/test/fake-sheets.test.js`:

```js
// The fake's own behaviour, where it is subtle enough to get wrong. deleteRows is the whole of
// that: everything below the deleted rows shifts up, which is the reason saveBatch applies
// deletes last and bottom-up, and a fake that shifted the wrong way would let a broken
// implementation go green.

import { test } from 'node:test';
import assert from 'node:assert/strict';
import { loadCodeGs } from './fake-sheets.js';

const GRID = [
  ['id', 'name'],
  [1, 'one'],
  [2, 'two'],
  [3, 'three'],
  [4, 'four'],
];

function sheetOf() {
  return loadCodeGs({ Items: GRID.map((row) => row.slice()) }).sheets.Items;
}

test('deleteRows removes the rows and shifts everything below up', () => {
  const sheet = sheetOf();
  sheet.deleteRows(3, 2);            // sheet rows 3 and 4 — the '2' and '3' records
  assert.deepEqual(sheet.raw(), [['id', 'name'], [1, 'one'], [4, 'four']]);
});

test('deleteRows shrinks the grid', () => {
  const sheet = sheetOf();
  assert.equal(sheet.getMaxRows(), 5);
  sheet.deleteRows(2, 1);
  assert.equal(sheet.getMaxRows(), 4);
  assert.equal(sheet.getLastRow(), 4);
});

test('deleteRows records the call for assertions', () => {
  const sheet = sheetOf();
  sheet.deleteRows(4, 2);
  sheet.deleteRows(2, 1);
  assert.deepEqual(sheet.deletes, [{ row: 4, count: 2 }, { row: 2, count: 1 }]);
});

test('deleteRows refuses a range past the grid', () => {
  const sheet = sheetOf();
  assert.throws(() => sheet.deleteRows(4, 5), /out of bounds/);
  assert.throws(() => sheet.deleteRows(0, 1), /out of bounds/);
});

test('the lock stub counts acquisition and release', () => {
  const gs = loadCodeGs({ Items: GRID.map((row) => row.slice()) });
  assert.deepEqual(gs.locks(), { acquired: 0, released: 0, held: false });
});

test('a lock that cannot be obtained throws from waitLock', () => {
  const gs = loadCodeGs({ Items: GRID.map((row) => row.slice()) }, {}, { lockFails: true });
  assert.throws(() => gs.writeRow('Items', 2, ['1', 'ONE'], -1), /lock/i);
});
```

The last test depends on Task 3 having put `writeRow` under the lock; leave it failing until then — **note it in the commit message** rather than pretending Task 2 is fully green. (Alternatively, land it in Task 3's commit. Do not weaken the assertion to make Task 2 look clean.)

**Step 2: Run to verify it fails**

```bash
node --test tools/DataEditor/test/fake-sheets.test.js
```

Expected: all six fail — `sheet.deleteRows is not a function`, `gs.locks is not a function`.

**Step 3: Implement**

In the `FakeSheet` constructor (`:66-83`), beside `this.writes = []` and `this.reads = []`:

```js
    this.deletes = [];
```

Add to `FakeSheet`, immediately after `insertRowsAfter` (`:130`):

```js
  // The mirror image of insertRowsAfter: the rows go, everything below shifts UP, and the grid
  // shrinks. That shift is why saveBatch applies deletes last and from the bottom — a plan built
  // against the pre-delete sheet stays valid only until the first deleteRows call.
  //
  // 1-based position, `howMany` rows, matching Sheet.deleteRows(rowPosition, howMany).
  deleteRows(position, howMany) {
    if (position < 1 || howMany < 1 || position + howMany - 1 > this.maxRows) {
      throw new Error('fake sheet: deleteRows out of bounds (' + position + ', ' + howMany +
                      ') on a grid of ' + this.maxRows);
    }
    this.deletes.push({ row: position, count: howMany });
    this.cells.splice(position - 1, howMany);
    this.maxRows -= howMany;
  }
```

Change the `loadCodeGs` signature (`:199`) to take a third argument, and add the lock:

```js
export function loadCodeGs(sheetsByName, options = {}, settings = {}) {
```

`options` stays keyed by sheet name; `settings` is for everything that is not a sheet, so a workbook with a sheet called `lockFails` cannot collide with a switch. Inside, before `const context = createContext(sandbox)`:

```js
  // LockService, which the fake used to leave out on the grounds that Code.gs never called it.
  // It does now. Modelled to the three methods Code.gs uses, and no further: waitLock either
  // takes the lock or throws (the real one throws on timeout — it does not return false; that is
  // tryLock), and releaseLock is expected in a finally.
  const locks = { acquired: 0, released: 0, held: false };
  sandbox.LockService = {
    getDocumentLock: () => ({
      waitLock: (timeoutMs) => {
        if (settings.lockFails) {
          throw new Error('Could not obtain lock after ' + timeoutMs + 'ms.');
        }
        locks.acquired += 1;
        locks.held = true;
      },
      releaseLock: () => { locks.released += 1; locks.held = false; },
      hasLock: () => locks.held,
    }),
  };
```

and add to the returned object (`:223-229`):

```js
    locks: () => ({ ...locks }),
```

Update the header comment. At `:16-17` the line reading

```
// Everything Code.gs does not call is absent, deliberately: getFormulas, getUi, merged cells,
// data validation, LockService. setNumberFormat is present but only RECORDED ...
```

becomes

```
// Everything Code.gs does not call is absent, deliberately: getFormulas, getUi, merged cells,
// data validation. LockService IS here — writeRow and saveBatch both take a document lock — but
// only as a counter: it models that the lock is taken and released, not contention, because one
// vm context has no second caller to contend with. setNumberFormat is present but only RECORDED ...
```

and extend the `sheet.writes` / `sheet.reads` paragraph at `:29-37` with a sentence on `sheet.deletes`:

```
// Every deleteRows call is recorded in sheet.deletes as { row, count }, in call order. Order is
// the assertion that matters: deletes must run bottom-up, and a top-down implementation leaves
// the same final grid when the runs happen not to overlap — so only the call sequence can tell
// the two apart.
```

**Step 4: Run**

```bash
node --test tools/DataEditor/test/fake-sheets.test.js
```

Expected: 5 pass, 1 fail (`a lock that cannot be obtained throws from waitLock` — `writeRow` is not under the lock until Task 3).

**Step 5: Commit**

```bash
git add tools/DataEditor/test/fake-sheets.js tools/DataEditor/test/fake-sheets.test.js
git commit -m "test(editor): model deleteRows and LockService in the fake SpreadsheetApp

The lock-failure test fails until writeRow takes the lock in the next commit."
```

---

## Task 3: Extract the cell diff and put `writeRow` under a document lock

`saveBatch` must decide what to write for many rows across many sheets *before* writing any of them, which `writeRow`'s inline diff loop cannot do — it reads, decides and writes in one pass. Extracting it makes the rule usable from both places and, more importantly, makes it provably one rule rather than two that currently agree.

No behaviour changes. All 51 existing `code-gs.test.js` tests must stay green.

**Files:**
- Modify: `tools/DataEditor/Code.gs:120-123` (the accepted-risk note), `:430-613` (`writeRow`)
- Test: `tools/DataEditor/test/code-gs.test.js`, `tools/DataEditor/test/fake-sheets.test.js`

**Step 1: Write the failing tests**

Append to `tools/DataEditor/test/code-gs.test.js`:

```js
// --- the document lock ----------------------------------------------------------------------
//
// Code.gs's header listed "no LockService" as accepted residual risk: the duplicate-id scan and
// the row write are not atomic, so two editors inside that window could both take one id. A batch
// save widens the window from one row to many rows across many sheets, which is more than the
// loaded-snapshot merge can narrow on its own.

test('writeRow takes the document lock and releases it', () => {
  const gs = sheet([ROW]);
  gs.writeRow('Items', 2, ['7', 'Steel Sword', '0.1235', '1500', '1', '185.25'], 0);
  assert.deepEqual(gs.locks(), { acquired: 1, released: 1, held: false });
});

test('writeRow releases the lock when it throws', () => {
  // The finally, stated as a test. A refused save that kept the lock would wedge every later
  // save in the document until the script instance went away.
  const gs = sheet([ROW]);
  assert.throws(() => gs.writeRow('Items', 1, ['x', 'y', 'z', 'w', 'v', 'u'], 0), /header row/);
  assert.deepEqual(gs.locks(), { acquired: 1, released: 1, held: false });
});

test('writeRow does not write when the lock cannot be taken', () => {
  const gs = loadCodeGs({ Items: [HEADER, ROW] }, {}, { lockFails: true });
  assert.throws(() => gs.writeRow('Items', 2, ['7', 'x', '0', '0', '0', '0'], 0), /lock/i);
  assert.deepEqual(gs.sheets.Items.writes, []);
});
```

`code-gs.test.js` currently imports only `loadCodeGs` (`:17`) and builds sheets through its local `sheet()` helper (`:31`); the third test needs `loadCodeGs` directly, which is already imported.

**Step 2: Run to verify they fail**

```bash
node --test tools/DataEditor/test/code-gs.test.js
```

Expected: all three fail — `gs.locks is not a function` is fixed by Task 2, so these fail on `acquired: 0` where `1` was expected, and the third writes the row instead of throwing.

**Step 3: Implement**

Add near the top of `Code.gs`, just below the header block (after line 125):

```js
/**
 * How long a save waits for the document lock. Generous: the alternative to waiting is telling
 * a user their save failed because someone else was mid-save, and a save is milliseconds of
 * sheet work. Well inside the 6-minute execution limit.
 */
var LOCK_TIMEOUT_MS = 30000;

/**
 * Internal: runs fn holding the document lock, and releases it whatever fn does.
 *
 * A DOCUMENT lock, not a script lock: the editor is container-bound, so the thing two callers
 * contend for is this spreadsheet. waitLock THROWS on timeout — it does not return false, which
 * is tryLock — so a caller that reaches fn is holding the lock.
 *
 * This closes the check-then-write window the header used to list as accepted risk. It is not a
 * transaction: fn can still fail part-way through its own writes, and nothing rolls those back.
 * What it buys is that no two saves interleave.
 */
function withDocumentLock_(fn) {
  var lock = LockService.getDocumentLock();
  lock.waitLock(LOCK_TIMEOUT_MS);
  try {
    return fn();
  } finally {
    lock.releaseLock();
  }
}

/**
 * Internal: which cells of a row a save must write, and which columns another editor changed
 * underneath it. Reads nothing and writes nothing — the caller supplies the row as it currently
 * stands, which is what lets a batch plan every row of every sheet from one read before it
 * touches anything.
 *
 * `currentRaw` / `currentShown` are the row's cells as getValues / getDisplayValues give them;
 * `out` is the posted record with blanks already folded to ''; `loaded` is the record AS THE
 * CLIENT READ IT, or null for a caller that has no snapshot.
 *
 * The three-way rule, unchanged from where it grew up inside writeRow:
 *   - a cell where posted equals loaded was never edited and is NEVER written, so a concurrent
 *     edit to it survives instead of being reverted to the stale copy;
 *   - a cell that already holds what the user wants needs no write;
 *   - a cell the user DID edit whose current value matches neither loaded nor posted was also
 *     changed by someone else — a conflict, reported rather than overwritten.
 * Without `loaded` it degrades to "write what differs", which is last-writer-wins per cell.
 *
 * Returns { writeAt: number[], conflicts: number[] }, both 0-based column indexes, ascending.
 */
function planRowWrite_(currentRaw, currentShown, out, loaded, width) {
  var writeAt = [];
  var conflicts = [];

  for (var c = 0; c < width; c++) {
    var current = cellText_(currentRaw[c], currentShown ? currentShown[c] : '');
    var posted = String(out[c]);

    if (loaded) {
      var was = isBlank_(loaded[c]) ? '' : String(loaded[c]);
      if (posted === was) continue;
      if (current === posted) continue;
      if (current !== was) { conflicts.push(c); continue; }
    } else if (current === posted) {
      continue;
    }

    writeAt.push(c);
  }

  return { writeAt: writeAt, conflicts: conflicts };
}

/**
 * Internal: writes the planned cells of one row as contiguous runs, so an ordinary edit is one
 * setValues call rather than one per column. Pins '@' on a Text cell BEFORE writing it —
 * setValues parses strings like typed entry, so "1-2" in a description becomes a Date and "01"
 * becomes 1.
 *
 * `textColumns` is a 0-based index -> true map, not an array: it is consulted per written cell.
 */
function writeRuns_(sheet, target, out, writeAt, textColumns) {
  var runs = [];
  writeAt.forEach(function (c) {
    var open = runs.length ? runs[runs.length - 1] : null;
    if (open && open.at + open.values.length === c) open.values.push(out[c]);
    else runs.push({ at: c, values: [out[c]] });
  });

  runs.forEach(function (run) {
    run.values.forEach(function (value, k) {
      if (textColumns[run.at + k] && !isBlank_(value)) {
        sheet.getRange(target, run.at + k + 1).setNumberFormat('@');
      }
    });
    sheet.getRange(target, run.at + 1, 1, run.values.length).setValues([run.values]);
  });
}
```

Now rewrite `writeRow`'s body. Change the declaration at `:430` to wrap everything in the lock:

```js
function writeRow(sheetName, rowNumber, cells, idColumnIndex, options) {
  return withDocumentLock_(function () {
    return writeRowLocked_(sheetName, rowNumber, cells, idColumnIndex, options);
  });
}

function writeRowLocked_(sheetName, rowNumber, cells, idColumnIndex, options) {
```

— i.e. rename the existing function to `writeRowLocked_` and keep its whole body, then add the four-line `writeRow` wrapper above it. Move the existing JSDoc block to sit above `writeRow`.

Inside `writeRowLocked_`, replace the diff-and-write block at `:554-600` with:

```js
  if (target <= lastRow) {
    var before = sheet.getRange(target, 1, 1, width);
    var plan = planRowWrite_(before.getValues()[0], before.getDisplayValues()[0], out, loaded, width);

    if (plan.conflicts.length) {
      var header = sheet.getRange(1, 1, 1, width).getDisplayValues()[0];
      var names = plan.conflicts.map(function (c) { return String(header[c]); });
      throw new Error(
        sheetName + ' row ' + target + ': ' + names.join(', ') + ' changed in the sheet while ' +
        'you were editing — nothing was written. Reload the record and re-apply your edit.');
    }

    writeRuns_(sheet, target, out, plan.writeAt, textColumns);
  } else {
```

The `else` branch (the append at `:601-608`) is unchanged. The comment block at `:540-553` explaining the changed-cells-only write moves onto `planRowWrite_`; leave a one-line pointer where it was.

Finally, update the accepted-risk note at `Code.gs:120-123`:

```js
 *   Residual risk, accepted per plan scope: the check and the write are not a transaction. A
 *     save that fails part-way through its own writes leaves the earlier ones standing, and
 *     nothing rolls them back. Two editors no longer interleave — every write path holds the
 *     document lock (withDocumentLock_) — so an id can no longer be taken twice inside the
 *     check-then-write window. The loaded-snapshot merge still narrows a genuine two-editor
 *     conflict to the cells both sides edited.
```

**Step 4: Run the full suite**

```bash
node --test tools/DataEditor/test/*.test.js
```

Expected: **927 tests, 917 pass, 0 fail, 10 skipped.** Critically, all 51 pre-existing `code-gs.test.js` tests pass unchanged — that is what "no behaviour change" means here — and `fake-sheets.test.js`'s lock-failure test now passes too.

**Step 5: Commit**

```bash
git add tools/DataEditor/Code.gs tools/DataEditor/test/code-gs.test.js
git commit -m "refactor(editor): extract the cell diff and lock every write path"
```

---

## Task 4: `saveBatch` — plan and refuse, without writing

`saveBatch` plans every sheet in the batch, collects every problem across all of them, and throws one error naming all of them if there are any. This task builds the planning and refusal half; it deliberately does **not** apply anything, so its tests can assert that a refused batch wrote nothing without that assertion being vacuous.

**Files:**
- Modify: `tools/DataEditor/Code.gs` (append after `writeRowLocked_`)
- Modify: `tools/DataEditor/test/fake-sheets.js:223-229` (expose `saveBatch`)
- Test: `tools/DataEditor/test/code-gs.test.js`

**Step 1: Write the failing tests**

Append to `tools/DataEditor/test/code-gs.test.js`. Add a two-sheet helper beside the existing `sheet()` at `:31`:

```js
// --- saveBatch: planning and refusal ---------------------------------------------------------

const DROPS_HEADER = ['npc_template_id', 'item_template_id', 'stack', 'droprate'];

function batchGs(itemRows, dropRows, settings) {
  return loadCodeGs({
    Items: [HEADER].concat(itemRows),
    'NPC Drops': [DROPS_HEADER].concat(dropRows),
  }, {}, settings);
}

test('saveBatch refuses a sheet listed twice', () => {
  const gs = batchGs([ROW], [[1, 4471, 1, 0.25]]);
  assert.throws(
    () => gs.saveBatch([{ sheet: 'Items', writes: [] }, { sheet: 'Items', appends: [] }]),
    /appears twice/);
});

test('saveBatch refuses a cells array that is not the header width', () => {
  const gs = batchGs([ROW], [[1, 4471, 1, 0.25]]);
  assert.throws(
    () => gs.saveBatch([{ sheet: 'NPC Drops', idColumnIndex: -1,
                          appends: [{ cells: ['1', '4471', '1'] }] }]),
    /3 values for a header 4 columns wide/);
});

test('saveBatch refuses a row number in two operations', () => {
  const gs = batchGs([ROW], [[1, 4471, 1, 0.25], [1, 12, 1, 0.5]]);
  assert.throws(
    () => gs.saveBatch([{
      sheet: 'NPC Drops', idColumnIndex: -1,
      writes: [{ row: 2, cells: ['1', '4471', '2', '0.25'], loaded: ['1', '4471', '1', '0.25'] }],
      deletes: [{ row: 2, loaded: ['1', '4471', '1', '0.25'] }],
    }]),
    /row 2 appears in more than one operation/);
});

test('saveBatch refuses the header row as a write or delete target', () => {
  const gs = batchGs([ROW], [[1, 4471, 1, 0.25]]);
  assert.throws(
    () => gs.saveBatch([{ sheet: 'NPC Drops', idColumnIndex: -1,
                          deletes: [{ row: 1, loaded: DROPS_HEADER.map(String) }] }]),
    /header row/);
});

test('saveBatch refuses a row past the end of the sheet', () => {
  const gs = batchGs([ROW], [[1, 4471, 1, 0.25]]);
  assert.throws(
    () => gs.saveBatch([{ sheet: 'NPC Drops', idColumnIndex: -1,
                          deletes: [{ row: 99, loaded: ['1', '4471', '1', '0.25'] }] }]),
    /past the end of the sheet/);
});

test('saveBatch refuses a delete whose row no longer matches what the client loaded', () => {
  // Stricter than a write on purpose: a write may skip cells the user did not edit, but there
  // is no such thing as deleting only the cells you were looking at.
  const gs = batchGs([ROW], [[1, 4471, 1, 0.25]]);
  assert.throws(
    () => gs.saveBatch([{ sheet: 'NPC Drops', idColumnIndex: -1,
                          deletes: [{ row: 2, loaded: ['1', '4471', '1', '0.99'] }] }]),
    /changed in the sheet/);
});

test('saveBatch refuses a write whose cell another editor changed', () => {
  const gs = batchGs([ROW], [[1, 4471, 1, 0.25]]);
  assert.throws(
    () => gs.saveBatch([{
      sheet: 'NPC Drops', idColumnIndex: -1,
      writes: [{ row: 2, cells: ['1', '4471', '1', '0.50'],
                 loaded: ['1', '4471', '1', '0.10'] }],   // sheet holds 0.25, neither value
    }]),
    /droprate/);
});

test('a conflict in one sheet refuses the whole batch, including the other sheet', () => {
  // The reason a parent and its children post as ONE call rather than two.
  const gs = batchGs([ROW], [[1, 4471, 1, 0.25]]);
  assert.throws(() => gs.saveBatch([
    { sheet: 'Items', idColumnIndex: 0,
      writes: [{ row: 2, cells: ['7', 'Steel Sword', '0.1235', '1500', '1', '185.25'],
                 loaded: ['7', 'Iron Sword', '0.1235', '1500', '1', '185.25'] }] },
    { sheet: 'NPC Drops', idColumnIndex: -1,
      deletes: [{ row: 2, loaded: ['1', '4471', '1', '0.99'] }] },
  ]), /changed in the sheet/);
  assert.deepEqual(gs.sheets.Items.writes, []);
  assert.deepEqual(gs.sheets['NPC Drops'].deletes, []);
});

test('saveBatch reports every problem in the batch, not just the first', () => {
  const gs = batchGs([ROW], [[1, 4471, 1, 0.25], [1, 12, 1, 0.5]]);
  let message = '';
  try {
    gs.saveBatch([{
      sheet: 'NPC Drops', idColumnIndex: -1,
      deletes: [{ row: 2, loaded: ['1', '4471', '1', '0.99'] },
                { row: 3, loaded: ['1', '12', '1', '0.99'] }],
    }]);
  } catch (e) { message = e.message; }
  assert.match(message, /row 2/);
  assert.match(message, /row 3/);
});

test('saveBatch refuses an appended id another row already holds', () => {
  const gs = batchGs([ROW], []);
  assert.throws(
    () => gs.saveBatch([{ sheet: 'Items', idColumnIndex: 0,
                          appends: [{ cells: ['7', 'Copy', '0', '0', '0', '0'] }] }]),
    /id 7 .*already used by row 2/);
});

test('saveBatch allows an appended id that a row being deleted is giving up', () => {
  // The plan is checked against the sheet AS IT WILL BE, not as it is. Without this, replacing a
  // record in one batch would be refused by the id it is itself releasing.
  const gs = batchGs([ROW], []);
  assert.doesNotThrow(() => gs.saveBatch([{
    sheet: 'Items', idColumnIndex: 0,
    appends: [{ cells: ['7', 'Replacement', '0', '0', '0', '0'] }],
    deletes: [{ row: 2, loaded: ['7', 'Iron Sword', '0.1235', '1500', '1', '185.25'] }],
  }]));
});

test('saveBatch leaves a duplicate id it did not create alone', () => {
  // writeRow's rule (Code.gs:516): a duplicate already in the column is the publish check's to
  // report. Only an id this batch CLAIMS can collide.
  const gs = batchGs([ROW, [7, 'Twin', 0, 0, 0, 0]], []);
  assert.doesNotThrow(() => gs.saveBatch([{
    sheet: 'Items', idColumnIndex: 0,
    writes: [{ row: 2, cells: ['7', 'Renamed', '0.1235', '1500', '1', '185.25'],
               loaded: ['7', 'Iron Sword', '0.1235', '1500', '1', '185.25'] }],
  }]));
});

test('saveBatch takes the lock and releases it on the throwing path', () => {
  const gs = batchGs([ROW], [[1, 4471, 1, 0.25]]);
  assert.throws(() => gs.saveBatch([{ sheet: 'NPC Drops', idColumnIndex: -1,
                                      deletes: [{ row: 2, loaded: ['x', 'x', 'x', 'x'] }] }]),
                /changed in the sheet/);
  assert.deepEqual(gs.locks(), { acquired: 1, released: 1, held: false });
});
```

Two of these (`allows an appended id that a row being deleted is giving up`, `leaves a duplicate id it did not create alone`) use `assert.doesNotThrow` and will pass trivially once `saveBatch` exists and applies nothing. They become meaningful in Task 5 and are worth having now as regression anchors.

**Step 2: Run to verify they fail**

```bash
node --test tools/DataEditor/test/code-gs.test.js
```

Expected: all fail with `gs.saveBatch is not a function`.

**Step 3: Implement**

Expose it in `test/fake-sheets.js`'s return object (`:223-229`):

```js
    saveBatch: (...args) => toHost(sandbox.saveBatch(...args)),
```

Append to `Code.gs`, after `writeRowLocked_`:

```js
/**
 * Saves several sheets' worth of edits, appends and deletions as one operation.
 *
 * `ops` is an array of per-sheet entries, in the order they should be APPLIED — put a parent
 * sheet before its children, so a batch that fails part-way leaves an incomplete parent (which
 * a retry completes) rather than children pointing at a row that was never written:
 *
 *   [{ sheet, idColumnIndex, textColumns, writes, appends, deletes }, ...]
 *
 *   writes:  [{ row, cells, loaded }]   row is 1-based including the header, as writeRow's is
 *   appends: [{ cells }]
 *   deletes: [{ row, loaded }]
 *
 * `cells` and `loaded` are arrays exactly as wide as that sheet's header, same encoding as
 * writeRow's. `idColumnIndex` is 0-based, or -1 for the nine sheets with no primary key.
 *
 * EVERY SHEET IS PLANNED BEFORE ANY SHEET IS WRITTEN. That is the whole point of the call: a
 * stale row in the last entry refuses the first entry's writes too, so an NPC and its drops
 * cannot half-save against a sheet that moved. All problems across all sheets are collected and
 * reported together, so a user fixes them in one pass rather than one error at a time.
 *
 * NOT A TRANSACTION. Once the checks pass, the writes go out sheet by sheet, and an Apps Script
 * failure part-way through leaves the earlier ones standing. The document lock means no other
 * save interleaves; it does not mean this one is atomic. See the header's residual-risk note.
 */
function saveBatch(ops) {
  if (!Array.isArray(ops) || ops.length === 0) {
    throw new Error('saveBatch: ops must be a non-empty array');
  }

  var seen = {};
  ops.forEach(function (entry) {
    var name = String(entry && entry.sheet);
    if (Object.prototype.hasOwnProperty.call(seen, name)) {
      throw new Error('saveBatch: sheet "' + name + '" appears twice in one batch');
    }
    seen[name] = true;
  });

  return withDocumentLock_(function () {
    var plans = ops.map(planSheetOps_);

    var problems = [];
    plans.forEach(function (plan) { problems = problems.concat(plan.problems); });
    if (problems.length) throw new Error(problems.join('\n'));

    return applyPlans_(plans);
  });
}

/**
 * Internal: one sheet's ops checked against the sheet as it currently stands. READS ONLY.
 *
 * Returns everything applying needs — the resolved sheet, the header width, the folded cells,
 * the per-row write plans — plus `problems`, the human-readable refusals. A shape error that
 * makes planning impossible (no header, wrong-width array) throws immediately; a data
 * disagreement (a conflict, a duplicate id) goes into `problems` so the caller can report every
 * one of them at once.
 */
function planSheetOps_(entry) {
  var sheetName = String(entry.sheet);
  var sheet = requireSheet_(sheetName);

  var width = headerWidth_(sheet);
  if (width === 0) {
    throw new Error('saveBatch: sheet "' + sheetName + '" has no header row — nothing to write against');
  }

  var writes = Array.isArray(entry.writes) ? entry.writes : [];
  var appends = Array.isArray(entry.appends) ? entry.appends : [];
  var deletes = Array.isArray(entry.deletes) ? entry.deletes : [];

  var textColumns = {};
  (Array.isArray(entry.textColumns) ? entry.textColumns : []).forEach(function (i) {
    if (typeof i === 'number' && i >= 0 && i < width) textColumns[Math.floor(i)] = true;
  });

  var idIndex = typeof entry.idColumnIndex === 'number' &&
                entry.idColumnIndex >= 0 && entry.idColumnIndex < width
    ? Math.floor(entry.idColumnIndex)
    : -1;

  var lastRow = sheet.getLastRow();

  // ONE read of the sheet, with the display values fetched only if a Date turns up — the same
  // trade readSheet makes (Code.gs:290-308), and for the same reason: cellText_ reads `display`
  // for a Date and for nothing else, and no column in the schema is a date.
  var raw = [];
  var shown = null;
  if (lastRow >= 1) {
    var range = sheet.getDataRange();
    raw = range.getValues();
    for (var r = 0; r < raw.length && !shown; r++) {
      for (var c = 0; c < raw[r].length; c++) {
        if (isDate_(raw[r][c])) { shown = range.getDisplayValues(); break; }
      }
    }
  }

  var problems = [];
  var claimed = {};

  function fold(cells, what) {
    if (!Array.isArray(cells) || cells.length !== width) {
      throw new Error(
        sheetName + ': got ' + (Array.isArray(cells) ? cells.length : 'no') +
        ' values for a header ' + width + ' columns wide (' + what + ')');
    }
    return cells.map(function (cell) { return isBlank_(cell) ? '' : cell; });
  }

  function targetRow(row, what) {
    if (typeof row !== 'number' && !(typeof row === 'string' && String(row).trim() !== '')) {
      throw new Error(sheetName + ': invalid row ' + row + ' (' + what + ')');
    }
    var n = Number(row);
    if (!isFinite(n) || Math.floor(n) !== n || n < 0) {
      throw new Error(sheetName + ': invalid row ' + row + ' (' + what + ')');
    }
    if (n === 1) throw new Error(sheetName + ': refusing to touch the header row');
    if (n > lastRow) {
      throw new Error(
        sheetName + ': row ' + n + ' is past the end of the sheet (' + lastRow +
        ' rows) — reload and retry');
    }
    if (Object.prototype.hasOwnProperty.call(claimed, String(n))) {
      problems.push(sheetName + ': row ' + n + ' appears in more than one operation');
    }
    claimed[String(n)] = what;
    return n;
  }

  var header = sheet.getRange(1, 1, 1, width).getDisplayValues()[0];

  // WRITES. Planned, not applied — planRowWrite_ takes the row as it stands and answers which
  // cells to write, so nothing here touches the sheet.
  var plannedWrites = [];
  writes.forEach(function (w) {
    var row = targetRow(w.row, 'write');
    var out = fold(w.cells, 'write row ' + row);
    var loaded = Array.isArray(w.loaded) ? fold(w.loaded, 'write row ' + row + ' snapshot') : null;

    var currentRaw = raw[row - 1] || [];
    var currentShown = shown ? shown[row - 1] : null;
    var plan = planRowWrite_(currentRaw, currentShown, out, loaded, width);

    if (plan.conflicts.length) {
      var names = plan.conflicts.map(function (c) { return String(header[c]); });
      problems.push(
        sheetName + ' row ' + row + ': ' + names.join(', ') + ' changed in the sheet while you ' +
        'were editing — nothing was written. Reload and re-apply your edit.');
      return;
    }

    // A row whose every cell already agrees is not an operation. Dropping it here keeps the
    // returned count honest and saves a setValues call per untouched row.
    if (plan.writeAt.length) {
      plannedWrites.push({ row: row, out: out, writeAt: plan.writeAt });
    }
  });

  // DELETES. Stricter than a write: the row must still be, cell for cell, what the client was
  // looking at. A write may leave alone the cells the user did not edit; there is no partial
  // version of removing a row, so anything that moved under it is a refusal.
  var plannedDeletes = [];
  deletes.forEach(function (d) {
    var row = targetRow(d.row, 'delete');
    var loaded = fold(d.loaded, 'delete row ' + row + ' snapshot');

    var currentRaw = raw[row - 1] || [];
    var currentShown = shown ? shown[row - 1] : null;
    var moved = [];
    for (var c = 0; c < width; c++) {
      var current = cellText_(currentRaw[c], currentShown ? currentShown[c] : '');
      if (current !== String(loaded[c])) moved.push(String(header[c]));
    }

    if (moved.length) {
      problems.push(
        sheetName + ' row ' + row + ': ' + moved.join(', ') + ' changed in the sheet while you ' +
        'were editing — the row was not deleted. Reload and re-apply your edit.');
      return;
    }

    plannedDeletes.push({ row: row });
  });

  var plannedAppends = appends.map(function (a, i) {
    return { out: fold(a.cells, 'new row ' + (i + 1)) };
  });

  if (idIndex >= 0) {
    checkBatchIds_(sheetName, idIndex, raw, lastRow, plannedWrites, plannedAppends,
                   plannedDeletes, writes, problems);
  }

  return {
    name: sheetName,
    sheet: sheet,
    width: width,
    textColumns: textColumns,
    writes: plannedWrites,
    appends: plannedAppends,
    deletes: plannedDeletes,
    problems: problems,
  };
}

/**
 * Internal: no id this batch CLAIMS may collide, checked against the sheet AS IT WILL BE.
 *
 * Two things this deliberately does NOT do, both inherited from writeRow:
 *   - a duplicate already sitting in the column that this batch does not touch is left alone.
 *     This save did not make it; the publish check is what reports it. So the untouched rows are
 *     seeded first and silently, and only a claimed id can be refused.
 *   - a write that does not MOVE its id claims nothing, so an ordinary field edit on a sheet
 *     that already has a duplicate is not refused for a collision it did not cause.
 * And one it does: a row being DELETED releases its id in the same batch, so replacing a record
 * in one call is not refused by the id it is itself giving up.
 */
function checkBatchIds_(sheetName, idIndex, raw, lastRow, plannedWrites, plannedAppends,
                        plannedDeletes, rawWrites, problems) {
  var deleted = {};
  plannedDeletes.forEach(function (d) { deleted[String(d.row)] = true; });

  // Every write's posted id, by row — including the ones planRowWrite_ dropped as no-ops, since
  // a row whose id did not move still HOLDS that id in the post-batch sheet.
  var posted = {};
  rawWrites.forEach(function (w) {
    var row = Number(w.row);
    if (Array.isArray(w.cells)) posted[String(row)] = idKey_(w.cells[idIndex]);
  });

  var byKey = {};
  var claims = [];

  for (var row = 2; row <= lastRow; row++) {
    if (deleted[String(row)]) continue;

    var currentKey = idKey_((raw[row - 1] || [])[idIndex]);
    var isPosted = Object.prototype.hasOwnProperty.call(posted, String(row));
    var key = isPosted ? posted[String(row)] : currentKey;
    if (key === '') continue;

    // A posted id equal to the one already there is not a claim — writeRow's idUnchanged
    // (Code.gs:518). Treat the row as untouched so a pre-existing duplicate is not blamed on it.
    if (isPosted && key !== currentKey) claims.push({ key: key, who: 'row ' + row });
    else if (!byKey[key]) byKey[key] = 'row ' + row;
  }

  plannedAppends.forEach(function (a, i) {
    var key = idKey_(a.out[idIndex]);
    if (key !== '') claims.push({ key: key, who: 'new row ' + (i + 1) });
  });

  claims.forEach(function (claim) {
    if (byKey[claim.key]) {
      problems.push(sheetName + ': id ' + claim.key + ' (' + claim.who + ') is already used by ' +
                    byKey[claim.key]);
    } else {
      byKey[claim.key] = claim.who;
    }
  });
}

/** Internal: applies the plans. Task 5 fills this in; planning must be provably separate. */
function applyPlans_(plans) {
  return plans.map(function (plan) {
    return { sheet: plan.name, written: 0, appended: 0, deleted: 0 };
  });
}
```

**Step 4: Run**

```bash
node --test tools/DataEditor/test/*.test.js
```

Expected: **940 tests, 930 pass, 0 fail, 10 skipped.** Every refusal test passes; the two `doesNotThrow` tests pass trivially because nothing is applied yet.

**Step 5: Commit**

```bash
git add tools/DataEditor/Code.gs tools/DataEditor/test/code-gs.test.js tools/DataEditor/test/fake-sheets.js
git commit -m "feat(editor): plan and refuse a multi-sheet batch save"
```

---

## Task 5: `saveBatch` — apply the plans

Fills in `applyPlans_`. Writes → appends → deletes per sheet, sheets in the order given; deletes bottom-up with contiguous runs coalesced.

Deletes go last because that is what keeps every row number in the plan valid at the moment it is used: nothing shifts until everything else has landed, so no index arithmetic is needed anywhere.

**Files:**
- Modify: `tools/DataEditor/Code.gs` (`applyPlans_`, and the header VERIFICATION LIST)
- Test: `tools/DataEditor/test/code-gs.test.js`

**Step 1: Write the failing tests**

```js
// --- saveBatch: applying ---------------------------------------------------------------------

test('saveBatch applies writes, appends and deletes in one call', () => {
  const gs = batchGs([ROW], [[1, 4471, 1, 0.25], [1, 12, 1, 0.5], [1, 99, 1, 0.1]]);
  const result = gs.saveBatch([{
    sheet: 'NPC Drops', idColumnIndex: -1,
    writes: [{ row: 2, cells: ['1', '4471', '3', '0.25'], loaded: ['1', '4471', '1', '0.25'] }],
    appends: [{ cells: ['1', '5000', '1', '0.05'] }],
    deletes: [{ row: 3, loaded: ['1', '12', '1', '0.5'] }],
  }]);

  assert.deepEqual(result, [{ sheet: 'NPC Drops', written: 1, appended: 1, deleted: 1 }]);
  assert.deepEqual(gs.sheets['NPC Drops'].raw(), [
    DROPS_HEADER,
    [1, 4471, '3', 0.25],       // stack edited; the untouched cells keep their stored types
    [1, 99, 1, 0.1],            // shifted up by the delete
    ['1', '5000', '1', '0.05'],
  ]);
});

test('saveBatch writes only the cells that changed', () => {
  // The single-record guarantee, carried into the batch: a formula cell the user did not edit
  // still holds its formula afterwards.
  const gs = batchGs([ROW], []);
  gs.saveBatch([{
    sheet: 'Items', idColumnIndex: 0,
    writes: [{ row: 2, cells: ['7', 'Steel Sword', '0.1235', '1500', '1', '185.25'],
               loaded: ['7', 'Iron Sword', '0.1235', '1500', '1', '185.25'] }],
  }]);
  assert.deepEqual(gs.sheets.Items.writes.map((w) => w.col), [2]);
  assert.deepEqual(gs.sheets.Items.cell(2, 6), { formula: '=C2*D2', value: 185.25 });
});

test('saveBatch deletes bottom-up so earlier rows keep their numbers', () => {
  // Top-down would delete row 2, shift everything up, and then delete whatever had moved into
  // row 4 — a different record. The final grid is the assertion; the call ORDER is the proof.
  const gs = batchGs([ROW], [[1, 10, 1, 0.1], [1, 20, 1, 0.2], [1, 30, 1, 0.3], [1, 40, 1, 0.4]]);
  gs.saveBatch([{
    sheet: 'NPC Drops', idColumnIndex: -1,
    deletes: [{ row: 2, loaded: ['1', '10', '1', '0.1'] },
              { row: 4, loaded: ['1', '30', '1', '0.3'] }],
  }]);
  assert.deepEqual(gs.sheets['NPC Drops'].raw(), [DROPS_HEADER, [1, 20, 1, 0.2], [1, 40, 1, 0.4]]);
  assert.deepEqual(gs.sheets['NPC Drops'].deletes, [{ row: 4, count: 1 }, { row: 2, count: 1 }]);
});

test('saveBatch coalesces contiguous deletes into one call', () => {
  const gs = batchGs([ROW], [[1, 10, 1, 0.1], [1, 20, 1, 0.2], [1, 30, 1, 0.3], [1, 40, 1, 0.4]]);
  gs.saveBatch([{
    sheet: 'NPC Drops', idColumnIndex: -1,
    deletes: [{ row: 3, loaded: ['1', '20', '1', '0.2'] },
              { row: 2, loaded: ['1', '10', '1', '0.1'] },
              { row: 4, loaded: ['1', '30', '1', '0.3'] }],
  }]);
  assert.deepEqual(gs.sheets['NPC Drops'].deletes, [{ row: 2, count: 3 }]);
  assert.deepEqual(gs.sheets['NPC Drops'].raw(), [DROPS_HEADER, [1, 40, 1, 0.4]]);
});

test('saveBatch appends below the data even when the sheet is trimmed to it', () => {
  // Exercises the grid growth. A sheet with no spare rows must be grown before getRange, which
  // throws past the grid (fake-sheets.js:118).
  const gs = loadCodeGs({ 'NPC Drops': [DROPS_HEADER, [1, 10, 1, 0.1]] },
                        { 'NPC Drops': { maxRows: 2 } });
  gs.saveBatch([{ sheet: 'NPC Drops', idColumnIndex: -1,
                  appends: [{ cells: ['1', '20', '1', '0.2'] },
                            { cells: ['1', '30', '1', '0.3'] }] }]);
  assert.deepEqual(gs.sheets['NPC Drops'].raw(), [
    DROPS_HEADER, [1, 10, 1, 0.1], ['1', '20', '1', '0.2'], ['1', '30', '1', '0.3'],
  ]);
});

test('saveBatch pins the text format on appended Text cells before writing them', () => {
  // "1-2" in a description becomes a Date without the '@' pin, and "01" becomes 1.
  const gs = batchGs([], []);
  gs.saveBatch([{ sheet: 'Items', idColumnIndex: 0, textColumns: [1],
                  appends: [{ cells: ['9', '1-2', '0', '0', '0', '0'] }] }]);
  const writes = gs.sheets.Items.writes;
  const pin = writes.findIndex((w) => w.format === '@');
  const put = writes.findIndex((w) => w.values);
  assert.ok(pin !== -1, 'the text column was pinned');
  assert.ok(pin < put, 'pinned before the values were written');
});

test('saveBatch applies sheets in the order given', () => {
  // Parent first, so a failure part-way leaves an incomplete parent rather than orphan children.
  const gs = batchGs([ROW], [[1, 10, 1, 0.1]]);
  gs.saveBatch([
    { sheet: 'Items', idColumnIndex: 0,
      writes: [{ row: 2, cells: ['7', 'Renamed', '0.1235', '1500', '1', '185.25'],
                 loaded: ['7', 'Iron Sword', '0.1235', '1500', '1', '185.25'] }] },
    { sheet: 'NPC Drops', idColumnIndex: -1, appends: [{ cells: ['1', '20', '1', '0.2'] }] },
  ]);
  assert.equal(gs.sheets.Items.raw()[1][1], 'Renamed');
  assert.equal(gs.sheets['NPC Drops'].raw().length, 3);
});

test('saveBatch flushes once at the end', () => {
  const gs = batchGs([ROW], [[1, 10, 1, 0.1]]);
  gs.saveBatch([{ sheet: 'NPC Drops', idColumnIndex: -1,
                  appends: [{ cells: ['1', '20', '1', '0.2'] }] }]);
  assert.equal(gs.flushes(), 1);
});

test('saveBatch releases the lock on the happy path', () => {
  const gs = batchGs([ROW], [[1, 10, 1, 0.1]]);
  gs.saveBatch([{ sheet: 'NPC Drops', idColumnIndex: -1,
                  appends: [{ cells: ['1', '20', '1', '0.2'] }] }]);
  assert.deepEqual(gs.locks(), { acquired: 1, released: 1, held: false });
});

test('a batch that empties a group deletes every one of its rows', () => {
  const gs = batchGs([], [[1, 10, 1, 0.1], [1, 20, 1, 0.2]]);
  gs.saveBatch([{ sheet: 'NPC Drops', idColumnIndex: -1,
                  deletes: [{ row: 2, loaded: ['1', '10', '1', '0.1'] },
                            { row: 3, loaded: ['1', '20', '1', '0.2'] }] }]);
  assert.deepEqual(gs.sheets['NPC Drops'].raw(), [DROPS_HEADER]);
});
```

Also revisit the two `doesNotThrow` tests from Task 4 — now that applying is real, strengthen `saveBatch allows an appended id that a row being deleted is giving up` to assert the resulting grid holds exactly one row with id 7 and the name `Replacement`.

**Step 2: Run to verify they fail**

```bash
node --test tools/DataEditor/test/code-gs.test.js
```

Expected: failures showing zero counts and unchanged grids — `applyPlans_` is still the stub.

**Step 3: Implement**

Replace the `applyPlans_` stub with:

```js
/**
 * Internal: applies the checked plans. Sheets in the order given — parent before children, so a
 * failure part-way leaves an incomplete parent rather than children referencing a row that was
 * never written.
 */
function applyPlans_(plans) {
  var results = plans.map(applySheetPlan_);
  SpreadsheetApp.flush();
  return results;
}

/**
 * Internal: one sheet's plan, applied.
 *
 * WRITES, then APPENDS, then DELETES BOTTOM-UP. The order is the whole trick: every row number in
 * the plan was resolved against the sheet as it was read, and deleting a row shifts everything
 * below it up. Doing the deletes last means no row number is ever used after the shift that would
 * invalidate it, so nothing here needs to adjust an index. Bottom-up means the same within the
 * deletes themselves.
 */
function applySheetPlan_(plan) {
  var sheet = plan.sheet;

  plan.writes.forEach(function (w) {
    writeRuns_(sheet, w.row, w.out, w.writeAt, plan.textColumns);
  });

  if (plan.appends.length) {
    var at = sheet.getLastRow() + 1;
    var maxRows = sheet.getMaxRows();
    var need = at + plan.appends.length - 1;
    // getRange past the bottom of the grid throws; a sheet trimmed to exactly its data hits this
    // on the first append. Same growth writeRow does (Code.gs:535), sized for the whole block.
    if (need > maxRows) sheet.insertRowsAfter(maxRows, need - maxRows);

    // Pinned BEFORE the values go in, for every appended row, for the same reason writeRow pins:
    // setValues parses strings like typed entry.
    plan.appends.forEach(function (a, i) {
      for (var t = 0; t < plan.width; t++) {
        if (plan.textColumns[t] && !isBlank_(a.out[t])) {
          sheet.getRange(at + i, t + 1).setNumberFormat('@');
        }
      }
    });

    sheet.getRange(at, 1, plan.appends.length, plan.width).setValues(
      plan.appends.map(function (a) { return a.out; }));
  }

  var rows = plan.deletes.map(function (d) { return d.row; });
  rows.sort(function (a, b) { return b - a; });

  var deleted = 0;
  var i = 0;
  while (i < rows.length) {
    // rows is descending, so a run is consecutive when each next row is one LESS than the last.
    var top = rows[i];
    var j = i;
    while (j + 1 < rows.length && rows[j + 1] === rows[j] - 1) j++;
    var bottom = rows[j];
    sheet.deleteRows(bottom, top - bottom + 1);
    deleted += top - bottom + 1;
    i = j + 1;
  }

  return {
    sheet: plan.name,
    written: plan.writes.length,
    appended: plan.appends.length,
    deleted: deleted,
  };
}
```

Then extend `Code.gs`'s header VERIFICATION LIST with a `saveBatch` section, in the style of the `writeRow` one (`:83-123`). This file's culture is that every guard that cannot be unit-tested is written down as a live check; the batch adds several:

```js
 * saveBatch
 *   Checked here by test/code-gs.test.js: refusals (double-listed sheet, wrong width, a row in
 *     two ops, the header row, a row past the end, a moved delete target, a conflicting write, a
 *     claimed duplicate id), that a conflict in one sheet refuses the whole batch, and that
 *     writes/appends/deletes land in the right order with deletes bottom-up and coalesced.
 *   Live, each one its own test:
 *     - delete the LAST row of a sheet, then append: the append must land where the deleted row
 *       was, not one past it (getLastRow after a delete)
 *     - delete a row while a second tab has that sheet open, then save from the second tab: it
 *       must be refused for a row past the end, not write into whatever moved up
 *     - a batch touching two sheets where the SECOND is stale: neither sheet may be written
 *     - two tabs saving overlapping groups at the same instant: the lock must serialise them,
 *       and the loser must be refused by the snapshot check rather than silently winning
 *     - a group save of ~200 rows on NPC Spawns (the 4,322-row sheet): within the 6-minute limit
 *       and the google.script.run payload cap
 *     - delete every row of a sheet, leaving only the header: the next append must go to row 2
 *   Residual risk, accepted: not a transaction. If a write fails part-way, earlier sheets and
 *     earlier rows stand. Sheets are applied parent-first so the survivor is an incomplete parent
 *     rather than orphan children, and the client reloads after a FAILED save as well as a
 *     successful one — without that reload a retry would re-append rows that already landed.
```

**Step 4: Run the full suite**

```bash
node --test tools/DataEditor/test/*.test.js
```

Expected: **950 tests, 940 pass, 0 fail, 10 skipped.** All 51 original `code-gs.test.js` tests still pass.

**Step 5: Commit**

```bash
git add tools/DataEditor/Code.gs tools/DataEditor/test/code-gs.test.js
git commit -m "feat(editor): apply a batched multi-sheet save with row deletion"
```

---

## Done when

- [ ] `node --test tools/DataEditor/test/*.test.js` — 0 failures, and all 911 baseline tests still pass
- [ ] `Forms.columnControl` is exported and honours `ctx.idPrefix`; the default keeps every existing id at `f-<column>`
- [ ] `Pickers.fkControl` prefixes its input id, its listbox id and `aria-controls` together
- [ ] `test/fake-sheets.js` models `deleteRows` and `LockService`, and its header no longer claims LockService is absent
- [ ] `writeRow` and `saveBatch` both run under `withDocumentLock_` and share `planRowWrite_` and `writeRuns_`
- [ ] A conflict anywhere in a batch leaves every sheet in the batch unwritten
- [ ] `Code.gs`'s VERIFICATION LIST covers `saveBatch` and its residual risk, and the stale "No LockService" note is gone
- [ ] Nothing the editor displays has changed — this part ships no UI

**Not in this part:** `Layout.GROUP_PARENT`, `src/groups.js`, the `app.js` branch, the parent list, the group table, the op-diff builder, row caps, duplicate-row warnings. All Part 2.
