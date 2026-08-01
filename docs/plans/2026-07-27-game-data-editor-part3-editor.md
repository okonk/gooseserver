# Game Data Editor — Part 3: The Apps Script Editor

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** A container-bound Apps Script web app that edits all 21 worksheets with schema-driven forms, id pickers, graphic previews, and blocking validation.

**Architecture:** Pure-logic modules live as `.js` under `tools/DataEditor/src/` and are unit-tested with `node --test`. A build step wraps each into the `.html` files Apps Script requires and assembles `Editor.html`. `Code.gs` does sheet I/O over `SpreadsheetApp`; everything else runs client-side against the `GOOSE_SCHEMA` and `GOOSE_SPRITES` globals from Part 2.

**Tech Stack:** Google Apps Script (V8), vanilla JS + Canvas 2D, `node:test` for unit tests.

**Design doc:** `docs/plans/2026-07-27-game-data-editor-design.md`
**Depends on:** Part 1 (`…-part1-descriptors.md`) and Part 2 (`…-part2-generators.md`), both complete.

**Part 3 of 3.** Deferred and explicitly out of scope: parent-centric child editing, one-button publish endpoint, the BIGINT scientific-notation fix, id-collision handling beyond a pre-write duplicate re-check, `aspereta-info` picker labels.

---

## APIs verified

| Fact | Location |
|---|---|
| `quest_ids` splits on **space or comma** | `Goose/NPCHandler.cs:107` — `Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries)` |
| Equipment slot order is Chest, Helm, Legs, Feet, Shield, Weapon, Mount | `Goose2ClientGodot/Scripts/Character/Character.cs:206-212` |
| Each slot is `id,*` **or** `id,r,g,b,a` — not a fixed pair | `Goose2ClientGodot/Scripts/Network/Packets/MakeCharacterPacket.cs:107-128` |
| `body_id >= 100` zeroes hair, face, and all six equipment slots plus mount | `Goose2ClientGodot/Scripts/Character/Character.cs:218-223` |
| Underwear: body 1 → legs 3; body 11 → legs 4 and chest 8; only when the slot is empty | `Goose2ClientGodot/Scripts/Character/CharacterLayout.cs:56-69` |
| Draw order is `(int)slot + 2`, Shield/Weapon flip by facing | `Goose2ClientGodot/Scripts/Character/CharacterLayout.cs:23-38` |
| `CharacterSlot` order: Mount, Body, Eyes, Feet, Legs, Chest, Hair, Helm, Shield, Weapon | `Goose2ClientGodot/Scripts/Character/CharacterLayout.cs:6` |
| Slot → sprite folder mapping (Shield and Weapon both render from `Hands`) | `Goose2ClientGodot/Scripts/Character/CharacterLayout.cs:41-54` |
| Vertical anchor: `max((h-48)/2, 0) - h/2`, sprite centred | `Goose2ClientGodot/Scripts/Character/CharacterAnchor.cs:12` |
| Tint is a blend, not opacity: `mix(t.rgb, tint.rgb, tint.a)`, source alpha preserved | `Goose2ClientGodot/Scripts/UI/Icon.cs:9-11` |
| Server sends equipment only when `body_id < 100` | `Goose/Packets.cs:161` |
| `/reloadsql` skips maps, classes, combinations, NPCs | `Goose/Events/ReloadSQLCommandEvent.cs:33-41` |
| Blank cell means "use the SQL default" | `CsvToSql/CsvToSql.Core/CsvToSqlBase.cs:27` |
| Node 22.23.1 with `node:test` available | verified locally |

### Three corrections to the design doc

**1. The draw order in the design doc is wrong.** It says "Hair → Eyes → Chest → Helm → Legs → Feet"
— that is the `ApplySlot` *call* order at `Character.cs:231-239`, which does not determine
rendering. Actual draw order is `CharacterLayout.SortOrder` = `(int)slot + 2`. For a
down-facing preview, back to front:

```
Mount(2) → Body(3) → Eyes(4) → Feet(5) → Legs(6) → Chest(7) → Hair(8) → Helm(9) → Shield(10) → Weapon(11)
```

Using the `ApplySlot` order would draw hair under the chest and legs over the body. Build the
preview from `SortOrder`.

**2. `equipped_items` is not six `graphic,tint` pairs.** Each slot emits either **two** tokens
(`id,*` — no tint) or **five** (`id,r,g,b,a`). Real rows are `0,*,0,*,0,*,0,*,0,*,0,*` — six
slots in the two-token form. A fixed pairwise parse breaks the moment a slot carries a tint.

**3. Apps Script has no `.js` file type.** Projects contain only `.gs` and `.html` files, so
client-side JS must be embedded in `<script>` tags inside `.html`. Part 2 emits `schema.js`,
which therefore needs wrapping too — Task 1's build step handles it. The three
`sprites-*.html` files Part 2 emits are already correct.

### One improvement over the design doc

The design says the editor is untested because no harness exists. That is true of the DOM glue,
but the load-bearing logic — validation, `equipped_items` parsing, appearance layering, tint
maths — is pure JS and testable under `node --test`. This plan TDDs those modules and leaves
only rendering and Apps Script calls to manual verification.

---

## Task 0: Confirm the `quest_ids` delimiter

The design flagged this as unverified. Confirm before building the `IdList` widget.

**Step 1: Read the parser**

```bash
grep -n "quest_ids" Goose/NPCHandler.cs
```

Expected output:

```
107:  var questIds = Convert.ToString(reader["quest_ids"]).Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries).Select(q => Convert.ToInt32(q));
```

**Step 2: Confirm nothing else parses it**

```bash
grep -rn "QuestIds\|quest_ids" --include=*.cs Goose | grep -v NPCHandler
```

Expected: no output.

**Conclusion for the widget:** both space and comma are accepted, empty entries ignored. Write
**space-separated** (matching `StartingItems` in `GooseSettings.json`) and accept either on read.
No further verification needed downstream.

---

## Task 1: Scaffold the Apps Script project and build step

**Files:**
- Create: `tools/DataEditor/appsscript.json`
- Create: `tools/DataEditor/.claspignore`
- Create: `tools/DataEditor/build.mjs`
- Create: `tools/DataEditor/src/.gitkeep`
- Modify: `tools/README.md`

**Step 1: Apps Script manifest**

`tools/DataEditor/appsscript.json`:

```json
{
  "timeZone": "Etc/UTC",
  "dependencies": {},
  "exceptionLogging": "STACKDRIVER",
  "runtimeVersion": "V8",
  "webapp": {
    "executeAs": "USER_ACCESSING",
    "access": "ANYONE"
  },
  "oauthScopes": [
    "https://www.googleapis.com/auth/spreadsheets.currentonly",
    "https://www.googleapis.com/auth/script.container.ui"
  ]
}
```

`access` takes one of `MYSELF`, `DOMAIN`, `ANYONE`, `ANYONE_ANONYMOUS` — nothing else is
accepted. `ANYONE` is the "anyone with a Google account" level; `ANYONE_ANONYMOUS` is the one
that drops the sign-in requirement, which this app must not do.

`executeAs: USER_ACCESSING` means each editor acts as themselves, so sheet permissions are the
access control and `Session.getActiveUser()` identifies who is editing.
`spreadsheets.currentonly` restricts the script to its own container.

**Step 2: Build script**

Wraps each `src/*.js` into a `dist/*.html` Apps Script can include, and copies Part 2's
generated files through.

`tools/DataEditor/build.mjs`:

```javascript
import { readdir, readFile, writeFile, mkdir, copyFile } from 'node:fs/promises';
import { existsSync } from 'node:fs';
import { dirname, join, basename } from 'node:path';
import { fileURLToPath } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
const src = join(here, 'src');
const dist = join(here, 'dist');

await mkdir(dist, { recursive: true });

// 1. Wrap each pure-JS module in <script> tags. Apps Script has no .js file type.
const modules = (await readdir(src)).filter((f) => f.endsWith('.js')).sort();
for (const file of modules) {
  const code = await readFile(join(src, file), 'utf8');
  const name = basename(file, '.js');
  await writeFile(
    join(dist, `${name}.html`),
    `<script>\n// Built from src/${file}. Do not edit.\n${code}\n</script>\n`
  );
}

// 2. Part 2 emits schema.js, which also needs wrapping.
const schema = join(here, 'schema.js');
if (existsSync(schema)) {
  await writeFile(
    join(dist, 'schema.html'),
    `<script>\n${await readFile(schema, 'utf8')}\n</script>\n`
  );
} else {
  console.warn('WARNING: schema.js missing — run tools/SchemaGen first');
}

// 3. Sprite bundles are already <script>-wrapped by Part 2.
for (const name of ['icons', 'parts', 'effects']) {
  const from = join(here, `sprites-${name}.html`);
  if (existsSync(from)) await copyFile(from, join(dist, `sprites-${name}.html`));
  else console.warn(`WARNING: sprites-${name}.html missing — run tools/SpriteBundle first`);
}

// 4. Static files pass through.
for (const f of ['Code.gs', 'Editor.html', 'appsscript.json']) {
  if (existsSync(join(here, f))) await copyFile(join(here, f), join(dist, f));
}

console.log(`Built ${modules.length} modules into dist/`);
```

**Step 3: `.claspignore`**

```
**
!dist/**
```

So `clasp push` uploads only `dist/`.

**Step 4: Verify the build runs**

```bash
node tools/DataEditor/build.mjs
```

Expected: `Built 0 modules into dist/` plus warnings about missing generated files if Parts 1–2
have not been run yet. Both are fine at this stage.

**Step 5: Document it**

Append to `tools/README.md`:

```markdown
## DataEditor

Apps Script sources live in `tools/DataEditor/`. Pure logic is under `src/` as plain `.js` so it
can be unit-tested:

    node --test tools/DataEditor/test

Apps Script has no `.js` file type, so build wraps each module into `dist/*.html`:

    node tools/DataEditor/build.mjs

Then deploy `dist/` — either `clasp push` (needs the container-bound script id, which differs
per spreadsheet) or paste the files into the Apps Script editor. Deploy once per spreadsheet.
```

**Step 6: Commit**

```bash
git add tools/DataEditor tools/README.md
git commit -m "chore: scaffold Apps Script data editor project and build step"
```

---

## Task 2: Validation module

The blocking-save rules. All pure functions, fully testable.

**Files:**
- Create: `tools/DataEditor/src/validation.js`
- Test: `tools/DataEditor/test/validation.test.js`

**Step 1: Write the failing test**

```javascript
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';

// Modules assign globals and export for node; load once.
const { Validation } = await import('../src/validation.js');

const col = (over = {}) => ({
  name: 'x', kind: 'Int', sql: 'INT', required: false, pk: false, ...over,
});

test('blank optional value is valid and writes nothing', () => {
  const r = Validation.validateCell(col({ default: '0' }), '');
  assert.equal(r.ok, true);
  assert.equal(r.write, false);
});

test('blank required value is invalid', () => {
  const r = Validation.validateCell(col({ required: true }), '');
  assert.equal(r.ok, false);
  assert.match(r.message, /required/i);
});

test('enum accepts a declared member and rejects others', () => {
  const c = col({ kind: 'Enum', enumNames: ['Weapon', 'Armour'] });
  assert.equal(Validation.validateCell(c, 'Weapon').ok, true);
  const bad = Validation.validateCell(c, 'weapon');
  assert.equal(bad.ok, false);
  assert.match(bad.message, /Weapon/);
});

test('integer range follows the SQL width', () => {
  assert.equal(Validation.validateCell(col({ sql: 'SMALLINT' }), '32767').ok, true);
  assert.equal(Validation.validateCell(col({ sql: 'SMALLINT' }), '32768').ok, false);
  assert.equal(Validation.validateCell(col({ sql: 'INT' }), '2147483647').ok, true);
  assert.equal(Validation.validateCell(col({ sql: 'INT' }), '2147483648').ok, false);
  assert.equal(Validation.validateCell(col({ sql: 'BIGINT' }), '9223372036854775807').ok, true);
});

test('non-numeric text in a numeric column is rejected', () => {
  const r = Validation.validateCell(col({ sql: 'INT' }), 'abc');
  assert.equal(r.ok, false);
  assert.match(r.message, /number/i);
});

test('bool accepts only 0 and 1', () => {
  const c = col({ kind: 'Bool', sql: 'CHAR(1)' });
  assert.equal(Validation.validateCell(c, '0').ok, true);
  assert.equal(Validation.validateCell(c, '1').ok, true);
  assert.equal(Validation.validateCell(c, 'true').ok, false);
});

test('text is accepted as-is', () => {
  assert.equal(Validation.validateCell(col({ kind: 'Text', sql: 'TEXT' }), "Bob's Hat").ok, true);
});

test('empty optional FK is valid', () => {
  const c = col({ kind: 'Id', ref: 'Items' });
  const r = Validation.validateCell(c, '', { Items: new Set([1, 2]) });
  assert.equal(r.ok, true);
});

test('unresolvable FK is rejected and names the id', () => {
  const c = col({ kind: 'Id', ref: 'Items' });
  const r = Validation.validateCell(c, '4471', { Items: new Set([1, 2]) });
  assert.equal(r.ok, false);
  assert.match(r.message, /4471/);
  assert.match(r.message, /Items/);
});

test('resolvable FK passes', () => {
  const c = col({ kind: 'Id', ref: 'Items' });
  assert.equal(Validation.validateCell(c, '2', { Items: new Set([1, 2]) }).ok, true);
});

test('zero FK is treated as none, not a broken reference', () => {
  const c = col({ kind: 'Id', ref: 'Items' });
  assert.equal(Validation.validateCell(c, '0', { Items: new Set([1]) }).ok, true);
});

test('duplicate id is rejected, own id is allowed', () => {
  const existing = new Set([1, 2, 3]);
  assert.equal(Validation.validateId('4', existing, null).ok, true);
  assert.equal(Validation.validateId('2', existing, null).ok, false);
  // Editing row with id 2 — its own id must not count as a duplicate.
  assert.equal(Validation.validateId('2', existing, 2).ok, true);
});

test('nextId returns max plus one, and 1 for an empty sheet', () => {
  assert.equal(Validation.nextId([3, 1, 7]), 8);
  assert.equal(Validation.nextId([]), 1);
});

test('validateRecord collects every failure', () => {
  const columns = [
    col({ name: 'id', kind: 'Id', pk: true, required: true }),
    col({ name: 'name', kind: 'Text', sql: 'TEXT', required: true }),
    col({ name: 'kind', kind: 'Enum', enumNames: ['A'] }),
  ];
  const r = Validation.validateRecord(columns, { id: '5', name: '', kind: 'B' }, {});
  assert.equal(r.ok, false);
  assert.equal(r.errors.length, 2);
  assert.deepEqual(r.errors.map((e) => e.column).sort(), ['kind', 'name']);
});
```

**Step 2: Run it to verify it fails**

Run: `node --test tools/DataEditor/test`
Expected: FAIL — cannot resolve `../src/validation.js`.

**Step 3: Write the implementation**

```javascript
// Validation rules derived from the column descriptors in CsvToSql.Core.
// Blank means "use the SQL default" (CsvToSqlBase.cs:27 skips empty cells), so a blank
// optional cell is valid AND must not be written — writing 0 would pin a value that was
// previously tracking the default.
var Validation = (function () {
  var RANGES = {
    SMALLINT: [-32768, 32767],
    INT: [-2147483648, 2147483647],
    INTEGER: [-2147483648, 2147483647],
    BIGINT: [-9223372036854775808, 9223372036854775807],
  };

  function isNumericSql(sql) {
    return RANGES[sql] !== undefined || sql.indexOf('DECIMAL') === 0;
  }

  function validateCell(column, raw, idSets) {
    var value = (raw === null || raw === undefined) ? '' : String(raw).trim();

    if (value === '') {
      if (column.required) {
        return { ok: false, write: false, message: column.name + ' is required' };
      }
      return { ok: true, write: false };
    }

    if (column.kind === 'Enum') {
      var names = column.enumNames || [];
      if (names.indexOf(value) === -1) {
        return {
          ok: false, write: true,
          message: '"' + value + '" is not a valid ' + column.name +
                   ' — expected one of: ' + names.join(', '),
        };
      }
      return { ok: true, write: true };
    }

    if (column.kind === 'Bool') {
      if (value !== '0' && value !== '1') {
        return { ok: false, write: true, message: column.name + ' must be 0 or 1' };
      }
      return { ok: true, write: true };
    }

    if (column.kind === 'Text') {
      return { ok: true, write: true };
    }

    // Numeric kinds: Id, Int, Decimal.
    if (isNumericSql(column.sql) || column.kind !== 'Text') {
      if (!/^-?\d+(\.\d+)?$/.test(value)) {
        return { ok: false, write: true, message: column.name + ' must be a number' };
      }

      var range = RANGES[column.sql];
      if (range) {
        var n = Number(value);
        if (!Number.isInteger(n)) {
          return { ok: false, write: true, message: column.name + ' must be a whole number' };
        }
        if (n < range[0] || n > range[1]) {
          return {
            ok: false, write: true,
            message: column.name + ' must be between ' + range[0] + ' and ' + range[1] +
                     ' (' + column.sql + ')',
          };
        }
      }
    }

    // Foreign key: 0 and blank both mean "none".
    if (column.ref && idSets && value !== '0') {
      var known = idSets[column.ref];
      if (known && !known.has(Number(value))) {
        return {
          ok: false, write: true,
          message: column.name + ' = ' + value + ' does not exist in ' + column.ref,
        };
      }
    }

    return { ok: true, write: true };
  }

  function validateId(raw, existingIds, ownId) {
    var value = String(raw || '').trim();
    if (value === '') return { ok: false, message: 'id is required' };
    if (!/^\d+$/.test(value)) return { ok: false, message: 'id must be a whole number' };

    var n = Number(value);
    if (existingIds.has(n) && n !== ownId) {
      return { ok: false, message: 'id ' + n + ' is already used' };
    }
    return { ok: true };
  }

  function nextId(ids) {
    if (!ids || ids.length === 0) return 1;
    return Math.max.apply(null, ids) + 1;
  }

  function validateRecord(columns, values, idSets, ownId) {
    var errors = [];

    for (var i = 0; i < columns.length; i++) {
      var c = columns[i];
      var raw = values[c.name];

      if (c.pk) {
        var idResult = validateId(raw, (idSets && idSets.__self) || new Set(), ownId);
        if (!idResult.ok) errors.push({ column: c.name, message: idResult.message });
        continue;
      }

      var r = validateCell(c, raw, idSets);
      if (!r.ok) errors.push({ column: c.name, message: r.message });
    }

    return { ok: errors.length === 0, errors: errors };
  }

  return {
    validateCell: validateCell,
    validateId: validateId,
    nextId: nextId,
    validateRecord: validateRecord,
  };
})();

if (typeof module !== 'undefined') module.exports = { Validation: Validation };
```

**Step 4: Run the tests**

Run: `node --test tools/DataEditor/test`
Expected: PASS, 14 tests.

**Step 5: Commit**

```bash
git add tools/DataEditor/src/validation.js tools/DataEditor/test/validation.test.js
git commit -m "feat: add editor validation rules with tests"
```

---

## Task 3: `equipped_items` parsing

**Files:**
- Create: `tools/DataEditor/src/equipped.js`
- Test: `tools/DataEditor/test/equipped.test.js`

**Step 1: Write the failing test**

```javascript
import { test } from 'node:test';
import assert from 'node:assert/strict';

const { Equipped } = await import('../src/equipped.js');

test('parses the common untinted form', () => {
  const slots = Equipped.parse('0,*,0,*,0,*,0,*,0,*,0,*');
  assert.equal(slots.length, 6);
  assert.deepEqual(slots[0], { graphic: 0, r: 0, g: 0, b: 0, a: 0, tinted: false });
});

test('parses a five-token tinted slot', () => {
  // MakeCharacterPacket.cs:113-128 — a slot is either id,* or id,r,g,b,a.
  const slots = Equipped.parse('5,255,0,0,128,0,*,0,*,0,*,0,*,0,*');
  assert.equal(slots.length, 6);
  assert.deepEqual(slots[0], { graphic: 5, r: 255, g: 0, b: 0, a: 128, tinted: true });
  assert.equal(slots[1].graphic, 0);
});

test('slot order is Chest, Helm, Legs, Feet, Shield, Weapon', () => {
  assert.deepEqual(Equipped.SLOTS, ['Chest', 'Helm', 'Legs', 'Feet', 'Shield', 'Weapon']);
});

test('empty string yields six empty slots', () => {
  const slots = Equipped.parse('');
  assert.equal(slots.length, 6);
  assert.ok(slots.every((s) => s.graphic === 0));
});

test('formats back to the untinted form', () => {
  assert.equal(Equipped.format(Equipped.parse('0,*,0,*,0,*,0,*,0,*,0,*')),
               '0,*,0,*,0,*,0,*,0,*,0,*');
});

test('round-trips a tinted slot', () => {
  const input = '5,255,0,0,128,0,*,0,*,0,*,0,*,0,*';
  assert.equal(Equipped.format(Equipped.parse(input)), input);
});

test('a slot with zero blend alpha formats as untinted', () => {
  const slots = Equipped.parse('0,*,0,*,0,*,0,*,0,*,0,*');
  slots[0] = { graphic: 7, r: 10, g: 20, b: 30, a: 0, tinted: true };
  // a === 0 means no blend, so the compact form is equivalent and matches existing data.
  assert.equal(Equipped.format(slots), '7,*,0,*,0,*,0,*,0,*,0,*');
});

test('tolerates trailing whitespace and extra tokens', () => {
  const slots = Equipped.parse(' 0,*,0,*,0,*,0,*,0,*,0,* ');
  assert.equal(slots.length, 6);
});
```

**Step 2: Run it to verify it fails**

Run: `node --test tools/DataEditor/test`
Expected: FAIL — cannot resolve `../src/equipped.js`.

**Step 3: Write the implementation**

```javascript
// equipped_items is a comma-separated token stream, NOT fixed-width pairs. Each slot is
// either "<graphic>,*" (no tint) or "<graphic>,<r>,<g>,<b>,<a>" — see
// Scripts/Network/Packets/MakeCharacterPacket.cs:113-128. The DB string is spliced straight
// into the packet by Goose/Packets.cs:161, so this format is the wire format.
var Equipped = (function () {
  // Character.cs:206-211. The mount is slot 6 and is not part of equipped_items.
  var SLOTS = ['Chest', 'Helm', 'Legs', 'Feet', 'Shield', 'Weapon'];

  function empty() {
    return { graphic: 0, r: 0, g: 0, b: 0, a: 0, tinted: false };
  }

  function parse(raw) {
    var tokens = String(raw || '').trim().split(',').map(function (t) { return t.trim(); });
    var slots = [];
    var i = 0;

    while (slots.length < SLOTS.length) {
      if (i >= tokens.length || tokens[i] === '') { slots.push(empty()); i += 1; continue; }

      var graphic = parseInt(tokens[i], 10) || 0;
      i += 1;

      if (tokens[i] === '*') {
        slots.push({ graphic: graphic, r: 0, g: 0, b: 0, a: 0, tinted: false });
        i += 1;
      } else {
        slots.push({
          graphic: graphic,
          r: parseInt(tokens[i], 10) || 0,
          g: parseInt(tokens[i + 1], 10) || 0,
          b: parseInt(tokens[i + 2], 10) || 0,
          a: parseInt(tokens[i + 3], 10) || 0,
          tinted: true,
        });
        i += 4;
      }
    }

    return slots;
  }

  function format(slots) {
    var parts = [];
    for (var i = 0; i < SLOTS.length; i++) {
      var s = slots[i] || empty();
      // a === 0 means no blend, so emit the compact form — matches how existing rows look.
      if (!s.tinted || !s.a) parts.push(s.graphic + ',*');
      else parts.push([s.graphic, s.r, s.g, s.b, s.a].join(','));
    }
    return parts.join(',');
  }

  return { SLOTS: SLOTS, parse: parse, format: format, empty: empty };
})();

if (typeof module !== 'undefined') module.exports = { Equipped: Equipped };
```

**Step 4: Run the tests**

Run: `node --test tools/DataEditor/test`
Expected: PASS, 8 new tests.

**Step 5: Commit**

```bash
git add tools/DataEditor/src/equipped.js tools/DataEditor/test/equipped.test.js
git commit -m "feat: parse and format equipped_items slot stream"
```

---

## Task 4: Appearance layering

Which sprites to draw, in what order, at what offset. Pure computation — no canvas.

**Files:**
- Create: `tools/DataEditor/src/appearance.js`
- Test: `tools/DataEditor/test/appearance.test.js`

**Step 1: Write the failing test**

```javascript
import { test } from 'node:test';
import assert from 'node:assert/strict';

const { Appearance } = await import('../src/appearance.js');

const base = {
  bodyId: 1, bodyR: 0, bodyG: 0, bodyB: 0, bodyA: 0,
  hairId: 26, hairR: 10, hairG: 20, hairB: 30, hairA: 128,
  faceId: 70,
  equippedItems: '0,*,0,*,0,*,0,*,0,*,0,*',
};

test('draw order is back to front by CharacterLayout.SortOrder', () => {
  const layers = Appearance.layers({ ...base, bodyId: 1 });
  const order = layers.map((l) => l.slot);
  // CharacterLayout.cs:23-38 — (int)slot + 2, Shield/Weapon flip by facing.
  assert.deepEqual(order.slice(0, 3), ['Body', 'Eyes', 'Feet']);
  assert.ok(order.indexOf('Hair') > order.indexOf('Chest'));
  assert.ok(order.indexOf('Helm') > order.indexOf('Hair'));
});

test('monster bodies render only the body sprite', () => {
  // Character.cs:218-223 — body_id >= 100 zeroes everything else.
  const layers = Appearance.layers({ ...base, bodyId: 10113 });
  assert.deepEqual(layers.map((l) => l.slot), ['Body']);
  assert.equal(layers[0].id, 10113);
});

test('male body gets underwear legs 3 when the slot is empty', () => {
  // CharacterLayout.cs:56-62.
  const layers = Appearance.layers({ ...base, bodyId: 1 });
  const legs = layers.find((l) => l.slot === 'Legs');
  assert.equal(legs.id, 3);
  assert.deepEqual([legs.r, legs.g, legs.b, legs.a], [0, 0, 0, 0]);
});

test('female body gets underwear legs 4 and chest 8', () => {
  const layers = Appearance.layers({ ...base, bodyId: 11 });
  assert.equal(layers.find((l) => l.slot === 'Legs').id, 4);
  assert.equal(layers.find((l) => l.slot === 'Chest').id, 8);
});

test('equipped legs suppress underwear', () => {
  const layers = Appearance.layers({
    ...base, bodyId: 1, equippedItems: '0,*,0,*,42,*,0,*,0,*,0,*',
  });
  assert.equal(layers.find((l) => l.slot === 'Legs').id, 42);
});

test('slots with graphic 0 are omitted entirely', () => {
  const layers = Appearance.layers({ ...base, bodyId: 1 });
  assert.equal(layers.find((l) => l.slot === 'Weapon'), undefined);
  assert.equal(layers.find((l) => l.slot === 'Shield'), undefined);
});

test('per-slot tints come from equipped_items', () => {
  const layers = Appearance.layers({
    ...base, bodyId: 1, equippedItems: '9,255,128,64,200,0,*,0,*,0,*,0,*,0,*',
  });
  const chest = layers.find((l) => l.slot === 'Chest');
  assert.deepEqual([chest.id, chest.r, chest.g, chest.b, chest.a], [9, 255, 128, 64, 200]);
});

test('hair carries its own tint and eyes never tint', () => {
  const layers = Appearance.layers({ ...base, bodyId: 1 });
  const hair = layers.find((l) => l.slot === 'Hair');
  assert.deepEqual([hair.r, hair.g, hair.b, hair.a], [10, 20, 30, 128]);
  const eyes = layers.find((l) => l.slot === 'Eyes');
  assert.equal(eyes.a, 0);
});

test('each layer names its sprite category', () => {
  // CharacterLayout.cs:41-54 — Shield and Weapon both come from Hands.
  const layers = Appearance.layers({
    ...base, bodyId: 1, equippedItems: '0,*,0,*,0,*,0,*,3,*,4,*',
  });
  assert.equal(layers.find((l) => l.slot === 'Shield').category, 'Hands');
  assert.equal(layers.find((l) => l.slot === 'Weapon').category, 'Hands');
  assert.equal(layers.find((l) => l.slot === 'Eyes').category, 'Eyes');
  assert.equal(layers.find((l) => l.slot === 'Helm'), undefined);
});

test('vertical anchor matches CharacterAnchor.OffsetY', () => {
  // CharacterAnchor.cs:12 — max((h-48)/2, 0) - h/2, C# integer division.
  assert.equal(Appearance.offsetY(48), -24);
  assert.equal(Appearance.offsetY(80), -24);
  assert.equal(Appearance.offsetY(24), -12);
  assert.equal(Appearance.offsetY(96), -24);
});
```

**Step 2: Run it to verify it fails**

Run: `node --test tools/DataEditor/test`
Expected: FAIL — cannot resolve `../src/appearance.js`.

**Step 3: Write the implementation**

```javascript
// Reproduces Character.ApplyAppearance (Scripts/Character/Character.cs:202-245) for a static
// south-facing preview.
//
// NOTE: the draw order is CharacterLayout.SortOrder ((int)slot + 2), NOT the order ApplySlot
// is called in. Using the call order draws hair beneath the chest.
var Appearance = (function () {
  // CharacterLayout.cs:6 — enum order defines the base sort order.
  var SLOT_INDEX = {
    Mount: 0, Body: 1, Eyes: 2, Feet: 3, Legs: 4, Chest: 5, Hair: 6, Helm: 7,
    Shield: 8, Weapon: 9,
  };

  // CharacterLayout.cs:41-54.
  var CATEGORY = {
    Body: 'Bodies', Mount: 'Bodies', Hair: 'Hair', Eyes: 'Eyes', Chest: 'Chest',
    Helm: 'Helms', Legs: 'Legs', Feet: 'Feet', Shield: 'Hands', Weapon: 'Hands',
  };

  /// Down-facing sort order. Shield/Weapon only flip for Right/Up (CharacterLayout.cs:29-37),
  /// so for Down they keep their base order.
  function sortOrder(slot) {
    return SLOT_INDEX[slot] + 2;
  }

  /// CharacterAnchor.cs:12. C# integer division truncates toward zero, so use Math.trunc.
  function offsetY(height) {
    return Math.max(Math.trunc((height - 48) / 2), 0) - Math.trunc(height / 2);
  }

  function layers(a) {
    var eq = Equipped.parse(a.equippedItems);

    var chest = eq[0], helm = eq[1], legs = eq[2];
    var feet = eq[3], shield = eq[4], weapon = eq[5];

    var hairId = a.hairId || 0;
    var faceId = a.faceId || 0;
    var bodyId = a.bodyId || 0;

    // Character.cs:218-223 — monster and morph bodies render the body only.
    if (bodyId >= 100) {
      hairId = 0; faceId = 0;
      chest = helm = legs = feet = shield = weapon = Equipped.empty();
    }

    // CharacterLayout.cs:56-69 — underwear only for the two player bodies, only when empty.
    if (legs.graphic === 0) {
      if (bodyId === 1) legs = { graphic: 3, r: 0, g: 0, b: 0, a: 0, tinted: false };
      else if (bodyId === 11) legs = { graphic: 4, r: 0, g: 0, b: 0, a: 0, tinted: false };
    }
    if (chest.graphic === 0 && bodyId === 11) {
      chest = { graphic: 8, r: 0, g: 0, b: 0, a: 0, tinted: false };
    }

    var out = [];

    function push(slot, id, r, g, b, alpha) {
      if (!id) return;
      out.push({
        slot: slot, category: CATEGORY[slot], id: id,
        r: r || 0, g: g || 0, b: b || 0, a: alpha || 0,
        order: sortOrder(slot),
      });
    }

    push('Body', bodyId, a.bodyR, a.bodyG, a.bodyB, a.bodyA);
    push('Hair', hairId, a.hairR, a.hairG, a.hairB, a.hairA);
    push('Eyes', faceId, 0, 0, 0, 0);            // Character.cs:233 — NoTint
    push('Chest', chest.graphic, chest.r, chest.g, chest.b, chest.a);
    push('Helm', helm.graphic, helm.r, helm.g, helm.b, helm.a);
    push('Legs', legs.graphic, legs.r, legs.g, legs.b, legs.a);
    push('Feet', feet.graphic, feet.r, feet.g, feet.b, feet.a);
    push('Shield', shield.graphic, shield.r, shield.g, shield.b, shield.a);
    push('Weapon', weapon.graphic, weapon.r, weapon.g, weapon.b, weapon.a);

    out.sort(function (x, y) { return x.order - y.order; });
    return out;
  }

  return { layers: layers, offsetY: offsetY, sortOrder: sortOrder, CATEGORY: CATEGORY };
})();

if (typeof module !== 'undefined') module.exports = { Appearance: Appearance };
```

The test file must load `equipped.js` first since `Appearance` uses `Equipped`. Add to the top
of `tools/DataEditor/test/appearance.test.js`:

```javascript
const { Equipped } = await import('../src/equipped.js');
globalThis.Equipped = Equipped;
```

**Step 4: Run the tests**

Run: `node --test tools/DataEditor/test`
Expected: PASS, 10 new tests.

**Step 5: Commit**

```bash
git add tools/DataEditor/src/appearance.js tools/DataEditor/test/appearance.test.js
git commit -m "feat: compute character appearance layers for preview"
```

---

## Task 5: Sprite rendering helpers

**Files:**
- Create: `tools/DataEditor/src/sprites.js`
- Test: `tools/DataEditor/test/sprites.test.js`

**Step 1: Write the failing test**

```javascript
import { test } from 'node:test';
import assert from 'node:assert/strict';

const { Sprites } = await import('../src/sprites.js');

const bundles = {
  icons: { width: 64, height: 64, png: 'data:image/png;base64,AAA', rects: { '20107:810003': [96, 0, 32, 32] } },
  parts: { width: 64, height: 64, png: 'data:image/png;base64,AAA', rects: { 'Bodies:1:idle-down': [0, 48, 24, 48] } },
  effects: { width: 64, height: 64, png: 'data:image/png;base64,AAA', rects: { '1080:0': [0, 0, 16, 16], '1080:1': [16, 0, 16, 16] } },
};

test('icon lookup uses sheet:graphic', () => {
  assert.deepEqual(Sprites.icon(bundles, 20107, 810003), [96, 0, 32, 32]);
});

test('missing icon returns null', () => {
  assert.equal(Sprites.icon(bundles, 999, 1), null);
});

test('graphic 0 is treated as none', () => {
  assert.equal(Sprites.icon(bundles, 0, 0), null);
});

test('part lookup falls back through the clip candidates', () => {
  // AnimationNames.Candidates: unarmed prefers idle-no-equip, then idle, then idle-equip.
  const r = Sprites.part(bundles, 'Bodies', 1, false);
  assert.deepEqual(r, [0, 48, 24, 48]);
});

test('missing part returns null rather than a wrong sprite', () => {
  assert.equal(Sprites.part(bundles, 'Helms', 999, false), null);
});

test('effect frames enumerate until a gap', () => {
  assert.equal(Sprites.effectFrames(bundles, 1080).length, 2);
  assert.equal(Sprites.effectFrames(bundles, 9999).length, 0);
});

test('tint blends rgb and preserves source alpha', () => {
  // Icon.cs:9-11 — mix(t.rgb, tint.rgb, tint.a), COLOR.a = t.a.
  const out = Sprites.applyTint([100, 100, 100, 200], { r: 200, g: 0, b: 0, a: 128 });
  assert.equal(out[3], 200, 'alpha preserved');
  assert.equal(out[0], Math.round(100 + (200 - 100) * (128 / 255)));
  assert.equal(out[1], Math.round(100 + (0 - 100) * (128 / 255)));
});

test('zero blend alpha leaves pixels untouched', () => {
  assert.deepEqual(Sprites.applyTint([10, 20, 30, 40], { r: 255, g: 255, b: 255, a: 0 }),
                   [10, 20, 30, 40]);
});

test('fully transparent pixels stay transparent', () => {
  const out = Sprites.applyTint([0, 0, 0, 0], { r: 255, g: 0, b: 0, a: 255 });
  assert.equal(out[3], 0);
});
```

**Step 2: Run it to verify it fails**

Run: `node --test tools/DataEditor/test`
Expected: FAIL — cannot resolve `../src/sprites.js`.

**Step 3: Write the implementation**

```javascript
// Sprite lookup and tinting against the GOOSE_SPRITES bundles from tools/SpriteBundle.
var Sprites = (function () {
  /// Resting-pose clip preference. AnimationNames.Candidates (client) orders idle candidates
  /// by whether a weapon is equipped; body_state's 4/5/6/7 variants only affect attack clips.
  function clipCandidates(equipped) {
    return equipped
      ? ['idle-equip-down', 'idle-down', 'idle-no-equip-down']
      : ['idle-no-equip-down', 'idle-down', 'idle-equip-down'];
  }

  function icon(bundles, sheet, graphic) {
    if (!sheet || !graphic) return null;   // 0 / blank means "no graphic"
    var b = bundles.icons;
    if (!b) return null;
    return b.rects[sheet + ':' + graphic] || null;
  }

  function part(bundles, category, id, equipped) {
    if (!id) return null;
    var b = bundles.parts;
    if (!b) return null;

    var candidates = clipCandidates(equipped);
    for (var i = 0; i < candidates.length; i++) {
      var rect = b.rects[category + ':' + id + ':' + candidates[i]];
      if (rect) return rect;
    }
    return null;   // Missing art hides the slot — never substitute another clip.
  }

  function mount(bundles, id) {
    if (!id) return null;
    var b = bundles.parts;
    return (b && b.rects['Bodies:' + id + ':mounted-idle-down']) || null;
  }

  function effectFrames(bundles, effectId) {
    var b = bundles.effects;
    if (!b || !effectId) return [];

    var frames = [];
    for (var i = 0; ; i++) {
      var rect = b.rects[effectId + ':' + i];
      if (!rect) break;
      frames.push(rect);
    }
    return frames;
  }

  /// Icon.cs:9-11. tint.a is a BLEND FACTOR, not opacity: COLOR = mix(t.rgb, tint.rgb, tint.a)
  /// with the source alpha carried through unchanged.
  function applyTint(px, tint) {
    if (!tint || !tint.a) return px;

    var f = tint.a / 255;
    return [
      Math.round(px[0] + (tint.r - px[0]) * f),
      Math.round(px[1] + (tint.g - px[1]) * f),
      Math.round(px[2] + (tint.b - px[2]) * f),
      px[3],
    ];
  }

  /// Draws one rect from a bundle onto a canvas context, applying the tint per-pixel when
  /// needed. Tinting requires pixel access, so it goes through an offscreen canvas.
  function draw(ctx, image, rect, dx, dy, tint) {
    if (!rect) return;

    if (!tint || !tint.a) {
      ctx.drawImage(image, rect[0], rect[1], rect[2], rect[3], dx, dy, rect[2], rect[3]);
      return;
    }

    var off = document.createElement('canvas');
    off.width = rect[2];
    off.height = rect[3];
    var octx = off.getContext('2d');
    octx.drawImage(image, rect[0], rect[1], rect[2], rect[3], 0, 0, rect[2], rect[3]);

    var data = octx.getImageData(0, 0, rect[2], rect[3]);
    var p = data.data;
    for (var i = 0; i < p.length; i += 4) {
      var out = applyTint([p[i], p[i + 1], p[i + 2], p[i + 3]], tint);
      p[i] = out[0]; p[i + 1] = out[1]; p[i + 2] = out[2]; p[i + 3] = out[3];
    }
    octx.putImageData(data, 0, 0);

    ctx.drawImage(off, dx, dy);
  }

  return {
    icon: icon, part: part, mount: mount, effectFrames: effectFrames,
    applyTint: applyTint, draw: draw, clipCandidates: clipCandidates,
  };
})();

if (typeof module !== 'undefined') module.exports = { Sprites: Sprites };
```

**Step 4: Run the tests**

Run: `node --test tools/DataEditor/test`
Expected: PASS, 9 new tests. `draw` is not unit-tested — it needs a DOM and is exercised by the
manual smoke in Task 12.

**Step 5: Commit**

```bash
git add tools/DataEditor/src/sprites.js tools/DataEditor/test/sprites.test.js
git commit -m "feat: add sprite lookup and tinting helpers"
```

---

## Task 6: Sheet I/O in `Code.gs`

Server-side Apps Script. Cannot be unit-tested; verified manually in Task 12.

**Files:**
- Create: `tools/DataEditor/Code.gs`

**Step 1: Write it**

```javascript
/**
 * Server side of the game data editor. Runs as the user accessing it
 * (appsscript.json executeAs USER_ACCESSING), so the spreadsheet's own sharing
 * permissions are the access control.
 */

function onOpen() {
  SpreadsheetApp.getUi()
    .createMenu('Game Data')
    .addItem('Open editor', 'showSidebar')
    .addToUi();
}

function doGet() {
  return HtmlService.createTemplateFromFile('Editor')
    .evaluate()
    .setTitle('Goose Game Data Editor')
    .addMetaTag('viewport', 'width=device-width, initial-scale=1');
}

function showSidebar() {
  var html = HtmlService.createTemplateFromFile('Editor')
    .evaluate()
    .setTitle('Game Data Editor');
  SpreadsheetApp.getUi().showSidebar(html);
}

/** Used by Editor.html to inline the built modules. */
function include(name) {
  return HtmlService.createHtmlOutputFromFile(name).getContent();
}

/**
 * Reads a whole worksheet. Returns the header row separately from the data rows so the
 * client can map positionally — the importer reads cells by index (CsvToSqlBase.cs:26),
 * not by header name.
 */
function readSheet(sheetName) {
  var sheet = SpreadsheetApp.getActiveSpreadsheet().getSheetByName(sheetName);
  if (!sheet) throw new Error('No worksheet named "' + sheetName + '"');

  var range = sheet.getDataRange();
  var values = range.getDisplayValues();

  return {
    sheet: sheetName,
    header: values.length ? values[0] : [],
    rows: values.slice(1),
    lastRow: sheet.getLastRow(),
  };
}

/** Reads only the first two columns of a sheet, for FK pickers (id + name). */
function readSheetIndex(sheetName) {
  var sheet = SpreadsheetApp.getActiveSpreadsheet().getSheetByName(sheetName);
  if (!sheet) throw new Error('No worksheet named "' + sheetName + '"');

  var lastRow = sheet.getLastRow();
  if (lastRow < 2) return { sheet: sheetName, entries: [] };

  var values = sheet.getRange(2, 1, lastRow - 1, 2).getDisplayValues();
  var entries = [];
  for (var i = 0; i < values.length; i++) {
    if (values[i][0] === '') continue;
    entries.push({ id: values[i][0], name: values[i][1] });
  }

  return { sheet: sheetName, entries: entries };
}

/**
 * Writes one record. `cells` is a sparse array aligned to column order; null or undefined
 * entries are written as empty, which the importer treats as "use the SQL default"
 * (CsvToSqlBase.cs:27). rowNumber is 1-based including the header; pass 0 to append.
 *
 * Re-checks for a duplicate id immediately before writing, so two editors adding records at
 * the same time cannot both take the same suggested id.
 */
function writeRow(sheetName, rowNumber, cells, idColumnIndex) {
  var sheet = SpreadsheetApp.getActiveSpreadsheet().getSheetByName(sheetName);
  if (!sheet) throw new Error('No worksheet named "' + sheetName + '"');

  var target = rowNumber > 0 ? rowNumber : sheet.getLastRow() + 1;

  if (idColumnIndex >= 0 && cells[idColumnIndex] !== null) {
    var newId = String(cells[idColumnIndex]);
    var lastRow = sheet.getLastRow();

    if (lastRow >= 2) {
      var ids = sheet.getRange(2, idColumnIndex + 1, lastRow - 1, 1).getDisplayValues();
      for (var i = 0; i < ids.length; i++) {
        if (ids[i][0] === newId && (i + 2) !== target) {
          throw new Error('id ' + newId + ' was just taken by another editor — reload and retry');
        }
      }
    }
  }

  var out = cells.map(function (c) { return (c === null || c === undefined) ? '' : c; });
  sheet.getRange(target, 1, 1, out.length).setValues([out]);
  SpreadsheetApp.flush();

  return { row: target };
}

/** Who is editing, for the UI header. */
function whoAmI() {
  return Session.getActiveUser().getEmail() || 'unknown';
}
```

**Step 2: Verify it parses**

Apps Script cannot be run locally. Check syntax with node:

```bash
node --check tools/DataEditor/Code.gs
```

Expected: no output (valid syntax).

**Step 3: Commit**

```bash
git add tools/DataEditor/Code.gs
git commit -m "feat: add Apps Script sheet IO for the data editor"
```

---

## Task 7: Field layout for the four wide sheets

Grouping lives in the editor, not the descriptors — per the design decision.

**Files:**
- Create: `tools/DataEditor/src/layout.js`
- Test: `tools/DataEditor/test/layout.test.js`

**Step 1: Write the failing test**

```javascript
import { test } from 'node:test';
import assert from 'node:assert/strict';

const { Layout } = await import('../src/layout.js');

test('sheets without a layout fall back to one generic group', () => {
  const groups = Layout.groupsFor('NPC Drops',
    [{ name: 'npc_template_id' }, { name: 'item_template_id' }]);
  assert.equal(groups.length, 1);
  assert.equal(groups[0].title, 'Fields');
  assert.deepEqual(groups[0].columns, ['npc_template_id', 'item_template_id']);
});

test('Items has the designed groups', () => {
  const groups = Layout.groupsFor('Items', []);
  assert.deepEqual(groups.map((g) => g.title), [
    'Identity', 'Requirements', 'Stats', 'Weapon', 'Flags', 'Value', 'Effects', 'Scripting',
  ]);
});

test('every laid-out column is assigned exactly once', () => {
  const columns = [
    { name: 'item_template_id' }, { name: 'item_name' }, { name: 'stat_str' },
    { name: 'lore' }, { name: 'script_path' },
  ];
  const groups = Layout.groupsFor('Items', columns);
  const placed = groups.flatMap((g) => g.columns);
  assert.equal(new Set(placed).size, placed.length);
});

test('columns missing from the layout land in an overflow group', () => {
  const groups = Layout.groupsFor('Items', [{ name: 'brand_new_column' }]);
  const other = groups.find((g) => g.title === 'Other');
  assert.ok(other, 'unlisted columns must still be editable');
  assert.deepEqual(other.columns, ['brand_new_column']);
});

test('restart-only sheets are flagged', () => {
  // ReloadSQLCommandEvent.cs:33-41 — these loaders are commented out.
  assert.equal(Layout.needsRestart('Maps'), true);
  assert.equal(Layout.needsRestart('Classes'), true);
  assert.equal(Layout.needsRestart('Class Info'), true);
  assert.equal(Layout.needsRestart('Combinations'), true);
  assert.equal(Layout.needsRestart('Combination Item Required'), true);
  assert.equal(Layout.needsRestart('Combination Item Result'), true);
  assert.equal(Layout.needsRestart('Items'), false);
  assert.equal(Layout.needsRestart('NPCs'), false);
});
```

**Step 2: Run it to verify it fails**

Run: `node --test tools/DataEditor/test`
Expected: FAIL — cannot resolve `../src/layout.js`.

**Step 3: Write the implementation**

```javascript
// Presentation-only: field grouping and order for the four wide sheets. Kept out of the C#
// descriptors deliberately — the descriptors describe the data, this describes the form.
var Layout = (function () {
  var LAYOUTS = {
    Items: [
      { title: 'Identity', columns: ['item_template_id', 'item_name', 'item_description',
          'item_usetype', 'item_slot', 'item_type', 'stack_size'] },
      { title: 'Requirements', columns: ['min_level', 'max_level', 'min_experience',
          'max_experience', 'class_restrictions'] },
      { title: 'Stats', columns: ['player_hp', 'player_mp', 'player_sp', 'stat_ac', 'stat_str',
          'stat_sta', 'stat_dex', 'stat_int', 'res_fire', 'res_water', 'res_spirit', 'res_air',
          'res_earth'] },
      { title: 'Weapon', columns: ['weapon_damage', 'weapon_delay', 'body_state'] },
      { title: 'Flags', columns: ['lore', 'bindonpickup', 'bindonequip', 'event'] },
      { title: 'Value', columns: ['item_value', 'credits_value'] },
      { title: 'Effects', columns: ['spell_effect_id', 'spell_effect_chance', 'learn_spell_id'] },
      { title: 'Scripting', columns: ['script_path', 'script_params'] },
    ],
    Spells: [
      { title: 'Identity', columns: ['spell_id', 'spell_name', 'spell_description'] },
      { title: 'Target', columns: ['spell_target', 'spell_aether'] },
      { title: 'Restrictions', columns: ['class_restrictions'] },
      { title: 'Costs', columns: ['hp_static_cost', 'hp_percent_cost', 'mp_static_cost',
          'mp_percent_cost', 'sp_static_cost', 'sp_percent_cost'] },
      { title: 'Effect', columns: ['spell_effect_id'] },
    ],
    NPCs: [
      { title: 'Identity', columns: ['npc_id', 'npc_name', 'npc_title', 'npc_surname',
          'npc_type', 'npc_level', 'npc_alliance'] },
      { title: 'Appearance', columns: ['body_state', 'body_id', 'face_id', 'hair_id',
          'equipped_items'] },
      { title: 'Combat', columns: ['npc_hp', 'npc_mp', 'npc_sp', 'stat_ac', 'stat_str',
          'stat_sta', 'stat_dex', 'stat_int', 'res_fire', 'res_water', 'res_spirit', 'res_air',
          'res_earth', 'weapon_damage', 'armor_pierce', 'attack_range', 'attack_speed',
          'experience'] },
      { title: 'Behaviour', columns: ['npc_facing', 'aggro_range', 'move_speed', 'stationary',
          'stunnable', 'rootable', 'slowable', 'invincible', 'stuck_behaviour', 'stuck_timeout',
          'respawn_time'] },
      { title: 'Regen', columns: ['hp_percent_regen', 'hp_static_regen', 'mp_percent_regen',
          'mp_static_regen'] },
      { title: 'Links', columns: ['class_id', 'quest_ids', 'credit_dealer'] },
      { title: 'Scripting', columns: ['script_path', 'script_params'] },
    ],
    'Spell Effects': [
      { title: 'Identity', columns: ['spell_effect_id', 'spell_effect_name', 'effect_type',
          'effect_duration'] },
      { title: 'Graphics', columns: ['spell_animation', 'spell_animation_file', 'spell_display',
          'buff_graphic', 'buff_graphic_file', 'do_attack_animation', 'do_cast_animation'] },
      { title: 'Targeting', columns: ['target_type', 'target_size', 'spell_effected',
          'min_level_effected', 'max_level_effected', 'only_hits_one_npc'] },
      { title: 'Damage', columns: ['spell_energy_type', 'spell_damage_effects',
          'hp_change_formula', 'mp_change_formula', 'sp_change_formula'] },
      { title: 'Modifiers', columns: ['hp', 'mp', 'sp', 'stat_ac', 'stat_str', 'stat_sta',
          'stat_dex', 'stat_int', 'res_fire', 'res_water', 'res_spirit', 'res_air', 'res_earth',
          'hp_percent_regen', 'hp_static_regen', 'mp_percent_regen', 'mp_static_regen', 'haste',
          'spell_damage', 'spell_crit', 'melee_damage', 'melee_crit', 'damage_reduce',
          'move_speed', 'snare_percent'] },
      { title: 'Appearance override', columns: ['body_id', 'face_id', 'hair_id'] },
      { title: 'Buff', columns: ['buff_removable', 'buff_doesnt_stack_over', 'buff_stacks_over',
          'oneffect_text', 'offeffect_text', 'taunt_aggro', 'works_in_pvp', 'works_not_in_pvp',
          'random_join_chance'] },
      { title: 'Teleport', columns: ['teleport_map', 'teleport_x', 'teleport_y'] },
      { title: 'Chained effects', columns: ['on_hit_spell_effect_id', 'on_hit_spell_chance',
          'on_attack_spell_effect_id', 'on_attack_spell_chance'] },
      { title: 'Scripting', columns: ['script_path', 'script_params'] },
    ],
  };

  /// ReloadSQLCommandEvent.cs:33-41 reloads spell effects, spells, items, quests and NPC
  /// templates. LoadMaps, LoadClasses, LoadCombinations and LoadNPCs are commented out, so
  /// edits to these sheets need a full server restart, not /reloadsql.
  var RESTART_ONLY = ['Maps', 'Classes', 'Class Info', 'Combinations',
                      'Combination Item Required', 'Combination Item Result'];

  function needsRestart(sheet) {
    return RESTART_ONLY.indexOf(sheet) !== -1;
  }

  /// Groups for a sheet. Columns absent from the layout still appear, under "Other", so a new
  /// descriptor column is never silently uneditable.
  function groupsFor(sheet, columns) {
    var names = columns.map(function (c) { return c.name; });
    var layout = LAYOUTS[sheet];

    if (!layout) return [{ title: 'Fields', columns: names }];

    var placed = {};
    var groups = layout.map(function (g) {
      var present = g.columns.filter(function (n) {
        if (names.indexOf(n) === -1) return false;
        placed[n] = true;
        return true;
      });
      return { title: g.title, columns: present };
    });

    var leftover = names.filter(function (n) { return !placed[n]; });
    if (leftover.length) groups.push({ title: 'Other', columns: leftover });

    return groups;
  }

  return { groupsFor: groupsFor, needsRestart: needsRestart, RESTART_ONLY: RESTART_ONLY };
})();

if (typeof module !== 'undefined') module.exports = { Layout: Layout };
```

Note `groupsFor('Items', [])` returns groups with empty column lists, which the first test
relies on for titles. The form builder skips empty groups.

**Step 4: Run the tests**

Run: `node --test tools/DataEditor/test`
Expected: PASS, 6 new tests.

**Step 5: Commit**

```bash
git add tools/DataEditor/src/layout.js tools/DataEditor/test/layout.test.js
git commit -m "feat: add editor field grouping and restart warnings"
```

---

## Task 8: Form builder

DOM-heavy; no unit tests. Verified in the Task 12 smoke.

**Files:**
- Create: `tools/DataEditor/src/forms.js`

**Step 1: Write it**

```javascript
// Builds a record form from GOOSE_SCHEMA plus Layout's grouping. One control per column,
// except where a composite claims several (see Composites).
var Forms = (function () {
  function el(tag, attrs, text) {
    var node = document.createElement(tag);
    if (attrs) Object.keys(attrs).forEach(function (k) { node.setAttribute(k, attrs[k]); });
    if (text !== undefined) node.textContent = text;
    return node;
  }

  /// Placeholder shows the SQL default so a blank field reads as "will use 0", not "unset".
  /// Blank must stay blank on write (CsvToSqlBase.cs:27).
  function placeholderFor(column) {
    if (column.required) return 'required';
    if (column.default === undefined || column.default === null) return '';
    return 'default ' + String(column.default).replace(/^'|'$/g, '');
  }

  function scalarControl(column, value) {
    if (column.kind === 'Enum') {
      var select = el('select', { name: column.name });
      if (!column.required) select.appendChild(el('option', { value: '' }, ''));
      (column.enumNames || []).forEach(function (n) {
        select.appendChild(el('option', { value: n }, n));
      });
      select.value = value || '';
      return select;
    }

    if (column.kind === 'Bool') {
      var box = el('select', { name: column.name });
      box.appendChild(el('option', { value: '' }, ''));
      box.appendChild(el('option', { value: '0' }, 'No'));
      box.appendChild(el('option', { value: '1' }, 'Yes'));
      box.value = value || '';
      return box;
    }

    var input = el('input', {
      name: column.name,
      type: column.kind === 'Text' ? 'text' : 'text',
      placeholder: placeholderFor(column),
      autocomplete: 'off',
    });
    input.value = value === undefined || value === null ? '' : value;
    return input;
  }

  /// Renders the whole record. `ctx` carries idSets, sprite bundles and picker data.
  function render(container, schema, values, ctx) {
    container.innerHTML = '';

    var byName = {};
    schema.columns.forEach(function (c) { byName[c.name] = c; });

    // Composites claim their columns so no duplicate control is rendered.
    var claimed = {};
    var compositeFor = {};
    (schema.composites || []).forEach(function (comp) {
      compositeFor[comp.columns[0]] = comp;
      comp.columns.forEach(function (n) { claimed[n] = comp; });
    });

    if (Layout.needsRestart(schema.sheet)) {
      var warn = el('div', { class: 'warn' },
        'Changes to ' + schema.sheet + ' need a full server restart — /reloadsql does not ' +
        'reload this table.');
      container.appendChild(warn);
    }

    Layout.groupsFor(schema.sheet, schema.columns).forEach(function (group) {
      if (!group.columns.length) return;

      var section = el('section');
      section.appendChild(el('h3', null, group.title));

      group.columns.forEach(function (name) {
        var column = byName[name];
        if (!column) return;

        var comp = claimed[name];
        if (comp && compositeFor[name] !== comp) return;   // rendered by its leader

        var row = el('div', { class: 'field' });
        row.appendChild(el('label', { for: name }, name));

        if (comp) {
          row.appendChild(Composites.control(comp, byName, values, ctx));
        } else {
          row.appendChild(scalarControl(column, values[name]));
        }

        row.appendChild(el('div', { class: 'error', 'data-error-for': name }));
        section.appendChild(row);
      });

      container.appendChild(section);
    });
  }

  /// Reads the form back into a name -> string map. Missing and blank both come back as ''.
  function collect(container, schema) {
    var values = {};
    schema.columns.forEach(function (c) { values[c.name] = ''; });

    var inputs = container.querySelectorAll('[name]');
    for (var i = 0; i < inputs.length; i++) {
      values[inputs[i].getAttribute('name')] = inputs[i].value;
    }

    (schema.composites || []).forEach(function (comp) {
      Object.assign(values, Composites.collect(comp, container));
    });

    return values;
  }

  function showErrors(container, errors) {
    var slots = container.querySelectorAll('[data-error-for]');
    for (var i = 0; i < slots.length; i++) slots[i].textContent = '';

    errors.forEach(function (e) {
      var slot = container.querySelector('[data-error-for="' + e.column + '"]');
      if (slot) slot.textContent = e.message;
    });
  }

  return { render: render, collect: collect, showErrors: showErrors, el: el };
})();

if (typeof module !== 'undefined') module.exports = { Forms: Forms };
```

**Step 2: Verify syntax**

```bash
node --check tools/DataEditor/src/forms.js
```

Expected: no output.

**Step 3: Commit**

```bash
git add tools/DataEditor/src/forms.js
git commit -m "feat: add schema-driven form builder"
```

---

## Task 9: Pickers

**Files:**
- Create: `tools/DataEditor/src/pickers.js`
- Test: `tools/DataEditor/test/pickers.test.js`

Only the search/filter logic is unit-tested; the dropdown DOM is not.

**Step 1: Write the failing test**

```javascript
import { test } from 'node:test';
import assert from 'node:assert/strict';

const { Pickers } = await import('../src/pickers.js');

const entries = [
  { id: '1', name: 'Gold' },
  { id: '42', name: 'Iron Sword' },
  { id: '43', name: 'Iron Shield' },
  { id: '100', name: 'Steel Sword' },
];

test('matches on id prefix', () => {
  assert.deepEqual(Pickers.search(entries, '4').map((e) => e.id), ['42', '43']);
});

test('matches on name substring, case-insensitively', () => {
  assert.deepEqual(Pickers.search(entries, 'sword').map((e) => e.id), ['42', '100']);
});

test('exact id match sorts first', () => {
  assert.equal(Pickers.search(entries, '1')[0].id, '1');
});

test('empty query returns the head of the list, capped', () => {
  const many = Array.from({ length: 500 }, (_, i) => ({ id: String(i), name: 'x' + i }));
  assert.equal(Pickers.search(many, '').length, Pickers.LIMIT);
});

test('no match returns empty', () => {
  assert.deepEqual(Pickers.search(entries, 'zzz'), []);
});
```

**Step 2: Run it to verify it fails**

Run: `node --test tools/DataEditor/test`
Expected: FAIL — cannot resolve `../src/pickers.js`.

**Step 3: Write the implementation**

```javascript
// Typeahead over another sheet's id + name, and the graphic picker over GOOSE_SPRITES.icons.
var Pickers = (function () {
  var LIMIT = 50;

  function search(entries, query) {
    var q = String(query || '').trim().toLowerCase();
    if (q === '') return entries.slice(0, LIMIT);

    var exact = [], idPrefix = [], nameHit = [];

    for (var i = 0; i < entries.length; i++) {
      var e = entries[i];
      var id = String(e.id).toLowerCase();
      var name = String(e.name || '').toLowerCase();

      if (id === q) exact.push(e);
      else if (id.indexOf(q) === 0) idPrefix.push(e);
      else if (name.indexOf(q) !== -1) nameHit.push(e);
    }

    return exact.concat(idPrefix, nameHit).slice(0, LIMIT);
  }

  /// FK control: a text input holding the id, a live label showing the resolved name, and a
  /// results list. Writes only the id back to the sheet.
  function fkControl(column, value, ctx) {
    var wrap = Forms.el('div', { class: 'picker' });
    var input = Forms.el('input', {
      name: column.name, autocomplete: 'off', placeholder: 'id or name',
    });
    input.value = value || '';

    var label = Forms.el('span', { class: 'resolved' });
    var list = Forms.el('div', { class: 'results', hidden: 'hidden' });

    var entries = (ctx.pickerData && ctx.pickerData[column.ref]) || [];

    function resolve() {
      var v = String(input.value || '').trim();
      if (v === '' || v === '0') { label.textContent = 'none'; label.className = 'resolved'; return; }

      var hit = entries.filter(function (e) { return String(e.id) === v; })[0];
      label.textContent = hit ? hit.name : 'not found in ' + column.ref;
      label.className = hit ? 'resolved' : 'resolved bad';
    }

    input.addEventListener('input', function () {
      var results = search(entries, input.value);
      list.innerHTML = '';
      results.forEach(function (e) {
        var row = Forms.el('button', { type: 'button', class: 'result' },
                           e.id + ' — ' + (e.name || ''));
        row.addEventListener('click', function () {
          input.value = e.id;
          list.hidden = true;
          resolve();
        });
        list.appendChild(row);
      });
      list.hidden = results.length === 0;
      resolve();
    });

    input.addEventListener('blur', function () {
      setTimeout(function () { list.hidden = true; }, 150);
    });

    resolve();
    wrap.appendChild(input);
    wrap.appendChild(label);
    wrap.appendChild(list);
    return wrap;
  }

  /// Graphic control: two hidden-ish inputs (graphic and sheet) plus a canvas preview.
  /// Blank or 0 means "no graphic".
  function graphicControl(graphicColumn, fileColumn, values, ctx, tintProvider) {
    var wrap = Forms.el('div', { class: 'graphic' });

    var gInput = Forms.el('input', { name: graphicColumn.name, placeholder: 'graphic id' });
    gInput.value = values[graphicColumn.name] || '';

    var fInput = Forms.el('input', { name: fileColumn.name, placeholder: 'sheet' });
    fInput.value = values[fileColumn.name] || '';

    var canvas = Forms.el('canvas', { width: 48, height: 48, class: 'preview' });

    function redraw() {
      var ctx2d = canvas.getContext('2d');
      ctx2d.clearRect(0, 0, canvas.width, canvas.height);

      var rect = Sprites.icon(ctx.bundles, Number(fInput.value), Number(gInput.value));
      if (!rect) return;

      var dx = Math.floor((canvas.width - rect[2]) / 2);
      var dy = Math.floor((canvas.height - rect[3]) / 2);
      Sprites.draw(ctx2d, ctx.images.icons, rect, dx, dy,
                   tintProvider ? tintProvider() : null);
    }

    gInput.addEventListener('input', redraw);
    fInput.addEventListener('input', redraw);
    ctx.onImagesReady(redraw);

    wrap.appendChild(canvas);
    wrap.appendChild(gInput);
    wrap.appendChild(fInput);
    wrap.__redraw = redraw;
    return wrap;
  }

  return { search: search, fkControl: fkControl, graphicControl: graphicControl, LIMIT: LIMIT };
})();

if (typeof module !== 'undefined') module.exports = { Pickers: Pickers };
```

**Step 4: Run the tests**

Run: `node --test tools/DataEditor/test`
Expected: PASS, 5 new tests.

**Step 5: Commit**

```bash
git add tools/DataEditor/src/pickers.js tools/DataEditor/test/pickers.test.js
git commit -m "feat: add FK typeahead and graphic pickers"
```

---

## Task 10: Composite controls

**Files:**
- Create: `tools/DataEditor/src/composites.js`
- Test: `tools/DataEditor/test/composites.test.js`

**Step 1: Write the failing test**

Only the value conversions are unit-tested.

```javascript
import { test } from 'node:test';
import assert from 'node:assert/strict';

const { Composites } = await import('../src/composites.js');

test('bitmask decodes set bits to class ids', () => {
  // class_restrictions 31 = bits 0-4 set = classes 1-5.
  assert.deepEqual(Composites.bitsToIds(31), [1, 2, 3, 4, 5]);
  assert.deepEqual(Composites.bitsToIds(22), [2, 3, 5]);
  assert.deepEqual(Composites.bitsToIds(0), []);
});

test('bitmask encodes class ids back to a mask', () => {
  assert.equal(Composites.idsToBits([1, 2, 3, 4, 5]), 31);
  assert.equal(Composites.idsToBits([2, 3, 5]), 22);
  assert.equal(Composites.idsToBits([]), 0);
});

test('bitmask round-trips', () => {
  [0, 1, 22, 31, 38, 55, 1023].forEach((mask) => {
    assert.equal(Composites.idsToBits(Composites.bitsToIds(mask)), mask);
  });
});

test('idList splits on space or comma', () => {
  // NPCHandler.cs:107 accepts both.
  assert.deepEqual(Composites.parseIdList('1 2 3'), ['1', '2', '3']);
  assert.deepEqual(Composites.parseIdList('1,2,3'), ['1', '2', '3']);
  assert.deepEqual(Composites.parseIdList('1, 2  3'), ['1', '2', '3']);
  assert.deepEqual(Composites.parseIdList(''), []);
});

test('idList writes space-separated', () => {
  assert.equal(Composites.formatIdList(['1', '2', '3']), '1 2 3');
  assert.equal(Composites.formatIdList([]), '');
});

test('rgba blend alpha of zero means no tint', () => {
  assert.equal(Composites.isTinted({ r: 255, g: 0, b: 0, a: 0 }), false);
  assert.equal(Composites.isTinted({ r: 255, g: 0, b: 0, a: 1 }), true);
});

test('hex conversion round-trips', () => {
  assert.equal(Composites.toHex(255, 128, 0), '#ff8000');
  assert.deepEqual(Composites.fromHex('#ff8000'), { r: 255, g: 128, b: 0 });
});
```

**Step 2: Run it to verify it fails**

Run: `node --test tools/DataEditor/test`
Expected: FAIL — cannot resolve `../src/composites.js`.

**Step 3: Write the implementation**

```javascript
// Controls spanning several columns. The flat column list is untouched — these read and write
// the same underlying cells (see the note in CsvToSql.Core/Schema/Composite.cs).
var Composites = (function () {
  function bitsToIds(mask) {
    var ids = [];
    var m = Number(mask) || 0;
    for (var bit = 0; bit < 53; bit++) {
      if (m & Math.pow(2, bit)) ids.push(bit + 1);
    }
    return ids;
  }

  function idsToBits(ids) {
    var mask = 0;
    (ids || []).forEach(function (id) { mask += Math.pow(2, Number(id) - 1); });
    return mask;
  }

  function parseIdList(raw) {
    return String(raw || '').split(/[\s,]+/).filter(function (t) { return t !== ''; });
  }

  function formatIdList(ids) {
    return (ids || []).join(' ');
  }

  function isTinted(t) {
    return !!(t && Number(t.a));
  }

  function toHex(r, g, b) {
    function h(v) { return ('0' + (Number(v) || 0).toString(16)).slice(-2); }
    return '#' + h(r) + h(g) + h(b);
  }

  function fromHex(hex) {
    var s = String(hex || '').replace('#', '');
    return {
      r: parseInt(s.slice(0, 2), 16) || 0,
      g: parseInt(s.slice(2, 4), 16) || 0,
      b: parseInt(s.slice(4, 6), 16) || 0,
    };
  }

  /// RGBA: one swatch plus a blend slider. The alpha channel is a BLEND FACTOR, not opacity
  /// (Scripts/UI/Icon.cs:9-11), so it is labelled "blend" rather than "alpha".
  function rgbaControl(comp, values) {
    var cols = comp.columns;   // [r, g, b, a]
    var wrap = Forms.el('div', { class: 'rgba' });

    var swatch = Forms.el('input', { type: 'color' });
    swatch.value = toHex(values[cols[0]], values[cols[1]], values[cols[2]]);

    var blend = Forms.el('input', { type: 'range', min: '0', max: '255' });
    blend.value = Number(values[cols[3]]) || 0;

    var readout = Forms.el('span', { class: 'readout' }, blend.value + ' / 255 blend');

    var hidden = cols.map(function (name) {
      var h = Forms.el('input', { type: 'hidden', name: name });
      h.value = values[name] === undefined ? '' : values[name];
      return h;
    });

    function sync() {
      var rgb = fromHex(swatch.value);
      // Only write when the user has actually set a blend — otherwise leave the cells blank
      // so they keep tracking the SQL default.
      if (Number(blend.value) === 0) {
        hidden.forEach(function (h) { h.value = ''; });
      } else {
        hidden[0].value = rgb.r;
        hidden[1].value = rgb.g;
        hidden[2].value = rgb.b;
        hidden[3].value = blend.value;
      }
      readout.textContent = blend.value + ' / 255 blend';
      if (wrap.__onChange) wrap.__onChange();
    }

    swatch.addEventListener('input', sync);
    blend.addEventListener('input', sync);

    wrap.appendChild(swatch);
    wrap.appendChild(blend);
    wrap.appendChild(readout);
    hidden.forEach(function (h) { wrap.appendChild(h); });
    return wrap;
  }

  /// Bitmask: checkbox list built from the referenced sheet.
  function bitmaskControl(comp, values, ctx) {
    var column = comp.columns[0];
    var wrap = Forms.el('div', { class: 'bitmask' });

    var hidden = Forms.el('input', { type: 'hidden', name: column });
    hidden.value = values[column] === undefined ? '' : values[column];

    var selected = bitsToIds(values[column]);
    var entries = (ctx.pickerData && ctx.pickerData[comp.source]) || [];

    var boxes = entries.map(function (e) {
      var label = Forms.el('label', { class: 'check' });
      var box = Forms.el('input', { type: 'checkbox', value: e.id });
      box.checked = selected.indexOf(Number(e.id)) !== -1;
      box.addEventListener('change', sync);
      label.appendChild(box);
      label.appendChild(Forms.el('span', null, e.id + ' ' + (e.name || '')));
      wrap.appendChild(label);
      return box;
    });

    function sync() {
      var ids = boxes.filter(function (b) { return b.checked; })
                     .map(function (b) { return b.value; });
      hidden.value = ids.length ? String(idsToBits(ids)) : '';
    }

    wrap.appendChild(hidden);
    return wrap;
  }

  /// IdList: repeated picker writing a space-separated list.
  function idListControl(comp, values, ctx) {
    var column = comp.columns[0];
    var wrap = Forms.el('div', { class: 'idlist' });

    var hidden = Forms.el('input', { type: 'hidden', name: column });
    hidden.value = values[column] === undefined ? '' : values[column];

    var ids = parseIdList(values[column]);
    var entries = (ctx.pickerData && ctx.pickerData[comp.source]) || [];
    var chips = Forms.el('div', { class: 'chips' });

    function sync() {
      hidden.value = ids.length ? formatIdList(ids) : '';
      renderChips();
    }

    function renderChips() {
      chips.innerHTML = '';
      ids.forEach(function (id, index) {
        var hit = entries.filter(function (e) { return String(e.id) === String(id); })[0];
        var chip = Forms.el('span', { class: 'chip' },
                            id + (hit ? ' ' + hit.name : ' (not found)'));
        var x = Forms.el('button', { type: 'button' }, '×');
        x.addEventListener('click', function () { ids.splice(index, 1); sync(); });
        chip.appendChild(x);
        chips.appendChild(chip);
      });
    }

    var add = Forms.el('input', { placeholder: 'add id', autocomplete: 'off' });
    add.addEventListener('change', function () {
      var v = add.value.trim();
      if (v !== '' && ids.indexOf(v) === -1) { ids.push(v); sync(); }
      add.value = '';
    });

    renderChips();
    wrap.appendChild(chips);
    wrap.appendChild(add);
    wrap.appendChild(hidden);
    return wrap;
  }

  /// EquipSlots: six labelled graphic pickers over the equipped_items token stream.
  function equipSlotsControl(comp, values, ctx) {
    var column = comp.columns[0];
    var wrap = Forms.el('div', { class: 'equip' });

    var hidden = Forms.el('input', { type: 'hidden', name: column });
    var slots = Equipped.parse(values[column]);

    function sync() {
      var formatted = Equipped.format(slots);
      // All-empty is the common case; keep the literal string since the column has no default.
      hidden.value = formatted;
      if (wrap.__onChange) wrap.__onChange();
    }

    Equipped.SLOTS.forEach(function (slotName, index) {
      var row = Forms.el('div', { class: 'equip-slot' });
      row.appendChild(Forms.el('label', null, slotName));

      var input = Forms.el('input', { placeholder: 'graphic id', autocomplete: 'off' });
      input.value = slots[index].graphic || '';

      var canvas = Forms.el('canvas', { width: 40, height: 56, class: 'preview' });

      function redraw() {
        var c = canvas.getContext('2d');
        c.clearRect(0, 0, canvas.width, canvas.height);

        var category = index === 0 ? 'Chest' : index === 1 ? 'Helms' :
                       index === 2 ? 'Legs' : index === 3 ? 'Feet' : 'Hands';
        var rect = Sprites.part(ctx.bundles, category, slots[index].graphic, true);
        if (!rect) return;

        Sprites.draw(c, ctx.images.parts, rect,
                     Math.floor((canvas.width - rect[2]) / 2),
                     Math.floor((canvas.height - rect[3]) / 2),
                     slots[index]);
      }

      input.addEventListener('input', function () {
        slots[index].graphic = Number(input.value) || 0;
        sync();
        redraw();
      });

      ctx.onImagesReady(redraw);

      row.appendChild(input);
      row.appendChild(canvas);
      wrap.appendChild(row);
    });

    sync();
    wrap.appendChild(hidden);
    return wrap;
  }

  function control(comp, byName, values, ctx) {
    switch (comp.kind) {
      case 'Graphic':
        return Pickers.graphicControl(byName[comp.columns[0]], byName[comp.columns[1]],
                                      values, ctx, null);
      case 'Rgba': return rgbaControl(comp, values);
      case 'Bitmask': return bitmaskControl(comp, values, ctx);
      case 'IdList': return idListControl(comp, values, ctx);
      case 'EquipSlots': return equipSlotsControl(comp, values, ctx);
      default: return Forms.el('div', null, 'unsupported composite: ' + comp.kind);
    }
  }

  /// Composite controls keep their state in hidden inputs, so Forms.collect already picks
  /// them up by name. Nothing extra to gather.
  function collect() { return {}; }

  return {
    control: control, collect: collect,
    bitsToIds: bitsToIds, idsToBits: idsToBits,
    parseIdList: parseIdList, formatIdList: formatIdList,
    isTinted: isTinted, toHex: toHex, fromHex: fromHex,
  };
})();

if (typeof module !== 'undefined') module.exports = { Composites: Composites };
```

**Step 4: Run the tests**

Run: `node --test tools/DataEditor/test`
Expected: PASS, 7 new tests.

**Step 5: Commit**

```bash
git add tools/DataEditor/src/composites.js tools/DataEditor/test/composites.test.js
git commit -m "feat: add composite controls for rgba, bitmask, id lists and equipment"
```

---

## Task 11: Previews, app shell and publish panel

**Files:**
- Create: `tools/DataEditor/src/preview.js`
- Create: `tools/DataEditor/src/app.js`
- Create: `tools/DataEditor/Editor.html`

**Step 1: Appearance and effect previews**

`tools/DataEditor/src/preview.js`:

```javascript
// Canvas previews. Layer computation lives in Appearance (tested); this is rendering only.
var Preview = (function () {
  var CANVAS_W = 96;
  var CANVAS_H = 112;
  var ORIGIN_Y = 88;   // where the feet land

  /// Composite character preview. Draws Appearance.layers in order, anchoring each sprite the
  /// way the client does (CharacterAnchor.cs:12).
  function character(canvas, appearance, ctx) {
    var c = canvas.getContext('2d');
    c.clearRect(0, 0, canvas.width, canvas.height);
    c.imageSmoothingEnabled = false;

    var layers = Appearance.layers(appearance);
    var equipped = Number(appearance.bodyState) !== 3;

    layers.forEach(function (layer) {
      var rect = layer.slot === 'Mount'
        ? Sprites.mount(ctx.bundles, layer.id)
        : Sprites.part(ctx.bundles, layer.category, layer.id, equipped);

      if (!rect) return;   // Missing art hides the slot, as the client does.

      var dx = Math.floor((canvas.width - rect[2]) / 2);
      var dy = ORIGIN_Y + Appearance.offsetY(rect[3]) - Math.trunc(rect[3] / 2);

      Sprites.draw(c, ctx.images.parts, rect, dx, dy, layer);
    });

    return layers.length;
  }

  /// Effect animation: 4-frame loop. Returns a stop function.
  function effect(canvas, effectId, ctx) {
    var frames = Sprites.effectFrames(ctx.bundles, effectId);
    var c = canvas.getContext('2d');
    c.imageSmoothingEnabled = false;

    if (!frames.length) {
      c.clearRect(0, 0, canvas.width, canvas.height);
      return function () {};
    }

    var i = 0;
    var timer = setInterval(function () {
      var rect = frames[i % frames.length];
      i += 1;

      c.clearRect(0, 0, canvas.width, canvas.height);
      Sprites.draw(c, ctx.images.effects, rect,
                   Math.floor((canvas.width - rect[2]) / 2),
                   Math.floor((canvas.height - rect[3]) / 2), null);
    }, 125);   // speed 8.0 in the .tres clips

    return function () { clearInterval(timer); };
  }

  return { character: character, effect: effect, CANVAS_W: CANVAS_W, CANVAS_H: CANVAS_H };
})();

if (typeof module !== 'undefined') module.exports = { Preview: Preview };
```

**Step 2: App shell**

`tools/DataEditor/src/app.js`:

```javascript
// Wires everything together: sheet selection, record list, form, save, publish check.
var App = (function () {
  var state = {
    schema: null,
    sheetName: null,
    rows: [],
    header: [],
    rowNumber: 0,
    idSets: {},
    pickerData: {},
    bundles: {},
    images: {},
    imageCallbacks: [],
  };

  function ctx() {
    return {
      bundles: state.bundles,
      images: state.images,
      pickerData: state.pickerData,
      onImagesReady: function (fn) {
        if (state.images.icons) fn();
        else state.imageCallbacks.push(fn);
      },
    };
  }

  /// Decodes each bundle's data URI once. Bundles load lazily: icons up front, parts and
  /// effects only when a sheet needs them.
  function loadBundle(name, done) {
    if (state.images[name]) { done(); return; }
    if (typeof GOOSE_SPRITES === 'undefined' || !GOOSE_SPRITES[name]) { done(); return; }

    state.bundles[name] = GOOSE_SPRITES[name];

    var img = new Image();
    img.onload = function () {
      state.images[name] = img;
      state.imageCallbacks.forEach(function (fn) { fn(); });
      state.imageCallbacks = [];
      done();
    };
    img.onerror = function () {
      status('Failed to decode the ' + name + ' sprite bundle', true);
      done();
    };
    img.src = GOOSE_SPRITES[name].png;
  }

  function schemaFor(sheetName) {
    return GOOSE_SCHEMA.sheets.filter(function (s) { return s.sheet === sheetName; })[0];
  }

  function status(message, isError) {
    var el = document.getElementById('status');
    el.textContent = message;
    el.className = isError ? 'error' : '';
  }

  function openSheet(sheetName) {
    status('Loading ' + sheetName + '…');
    state.sheetName = sheetName;
    state.schema = schemaFor(sheetName);

    google.script.run
      .withFailureHandler(function (e) { status(e.message, true); })
      .withSuccessHandler(function (data) {
        state.header = data.header;
        state.rows = data.rows;
        collectIds();
        loadReferencedSheets(function () {
          renderList();
          status(state.rows.length + ' records');
        });
      })
      .readSheet(sheetName);
  }

  /// Own-sheet id set, for duplicate detection.
  function collectIds() {
    var pk = state.schema.columns.filter(function (c) { return c.pk; })[0];
    var ids = [];

    if (pk) {
      var index = state.schema.columns.indexOf(pk);
      state.rows.forEach(function (r) {
        if (r[index] !== '' && r[index] !== undefined) ids.push(Number(r[index]));
      });
    }

    state.idSets.__self = new Set(ids);
    state.ids = ids;
  }

  /// FK targets and Bitmask sources need their id + name lists.
  function loadReferencedSheets(done) {
    var needed = {};
    state.schema.columns.forEach(function (c) { if (c.ref) needed[c.ref] = true; });
    (state.schema.composites || []).forEach(function (comp) {
      if (comp.source) needed[comp.source] = true;
    });

    var names = Object.keys(needed).filter(function (n) { return !state.pickerData[n]; });
    if (!names.length) { done(); return; }

    var remaining = names.length;
    names.forEach(function (name) {
      google.script.run
        .withFailureHandler(function () { remaining -= 1; if (!remaining) done(); })
        .withSuccessHandler(function (data) {
          state.pickerData[name] = data.entries;
          state.idSets[name] = new Set(data.entries.map(function (e) { return Number(e.id); }));
          remaining -= 1;
          if (!remaining) done();
        })
        .readSheetIndex(name);
    });
  }

  function rowToValues(row) {
    var values = {};
    state.schema.columns.forEach(function (c, i) {
      values[c.name] = row && row[i] !== undefined ? row[i] : '';
    });
    return values;
  }

  function renderList() {
    var list = document.getElementById('records');
    list.innerHTML = '';

    state.rows.forEach(function (row, index) {
      var button = Forms.el('button', { type: 'button', class: 'record' },
                            (row[0] || '?') + ' — ' + (row[1] || ''));
      button.addEventListener('click', function () { editRow(index); });
      list.appendChild(button);
    });
  }

  function editRow(index) {
    state.rowNumber = index + 2;   // 1-based, plus the header row
    var values = rowToValues(state.rows[index]);
    renderForm(values);
  }

  function newRecord() {
    state.rowNumber = 0;
    var values = rowToValues(null);

    var pk = state.schema.columns.filter(function (c) { return c.pk; })[0];
    if (pk) values[pk.name] = String(Validation.nextId(state.ids));

    renderForm(values);
  }

  function renderForm(values) {
    var needsParts = ['NPCs', 'Spell Effects'].indexOf(state.sheetName) !== -1 ||
                     (state.schema.composites || []).some(function (c) {
                       return c.kind === 'EquipSlots';
                     });
    var needsEffects = ['Spells', 'Spell Effects'].indexOf(state.sheetName) !== -1;

    loadBundle('icons', function () {
      loadBundle(needsParts ? 'parts' : 'icons', function () {
        loadBundle(needsEffects ? 'effects' : 'icons', function () {
          var container = document.getElementById('form');
          Forms.render(container, state.schema, values, ctx());
          renderPreviews(values);
        });
      });
    });
  }

  function renderPreviews(values) {
    var host = document.getElementById('previews');
    host.innerHTML = '';

    if (['NPCs', 'Spell Effects'].indexOf(state.sheetName) !== -1) {
      var canvas = Forms.el('canvas',
        { width: Preview.CANVAS_W, height: Preview.CANVAS_H, class: 'appearance' });
      host.appendChild(canvas);

      Preview.character(canvas, {
        bodyId: Number(values.body_id) || 0,
        bodyR: values.body_r, bodyG: values.body_g, bodyB: values.body_b, bodyA: values.body_a,
        hairId: Number(values.hair_id) || 0,
        hairR: values.hair_r, hairG: values.hair_g, hairB: values.hair_b, hairA: values.hair_a,
        faceId: Number(values.face_id) || 0,
        bodyState: values.body_state,
        equippedItems: values.equipped_items || '',
      }, ctx());
    }

    if (['Spells', 'Spell Effects'].indexOf(state.sheetName) !== -1) {
      var effectId = state.sheetName === 'Spells'
        ? Number(values.spell_effect_id) || 0
        : Number(values.spell_animation) || 0;

      if (effectId) {
        var anim = Forms.el('canvas', { width: 96, height: 96, class: 'effect' });
        host.appendChild(anim);
        if (state.stopEffect) state.stopEffect();
        state.stopEffect = Preview.effect(anim, effectId, ctx());
      }
    }
  }

  function save() {
    var container = document.getElementById('form');
    var values = Forms.collect(container, state.schema);

    var pk = state.schema.columns.filter(function (c) { return c.pk; })[0];
    var ownId = state.rowNumber > 0 && pk ? Number(values[pk.name]) : null;

    var result = Validation.validateRecord(state.schema.columns, values, state.idSets, ownId);
    Forms.showErrors(container, result.errors);

    if (!result.ok) {
      status(result.errors.length + ' problem(s) — fix them before saving', true);
      return;
    }

    // Blank stays blank: a cell is only written when the user supplied a value.
    var cells = state.schema.columns.map(function (c) {
      var check = Validation.validateCell(c, values[c.name], state.idSets);
      return check.write ? values[c.name] : null;
    });

    var idIndex = pk ? state.schema.columns.indexOf(pk) : -1;

    status('Saving…');
    google.script.run
      .withFailureHandler(function (e) { status(e.message, true); })
      .withSuccessHandler(function () {
        status('Saved. Run /updatesql then /reloadsql in game to publish.');
        openSheet(state.sheetName);
      })
      .writeRow(state.sheetName, state.rowNumber, cells, idIndex);
  }

  /// Publish check: validate every record on every sheet before telling the user to publish.
  function publishCheck() {
    var panel = document.getElementById('publish-results');
    panel.innerHTML = 'Checking all 21 sheets…';

    var sheets = GOOSE_SCHEMA.sheets.slice();
    var problems = [];
    var index = 0;

    function next() {
      if (index >= sheets.length) {
        panel.innerHTML = '';

        if (!problems.length) {
          panel.appendChild(Forms.el('p', { class: 'ok' },
            'All sheets valid. Publish with /updatesql then /reloadsql in game.'));
          var restart = Layout.RESTART_ONLY.join(', ');
          panel.appendChild(Forms.el('p', { class: 'warn' },
            'These need a full restart rather than /reloadsql: ' + restart));
        } else {
          panel.appendChild(Forms.el('p', { class: 'error' },
            problems.length + ' problem(s) — do not publish yet:'));
          problems.slice(0, 100).forEach(function (p) {
            panel.appendChild(Forms.el('div', { class: 'problem' },
              p.sheet + ' row ' + p.row + ': ' + p.message));
          });
          if (problems.length > 100) {
            panel.appendChild(Forms.el('p', null,
              'and ' + (problems.length - 100) + ' more not shown'));
          }
        }
        return;
      }

      var schema = sheets[index];
      index += 1;

      google.script.run
        .withFailureHandler(function (e) {
          problems.push({ sheet: schema.sheet, row: '-', message: e.message });
          next();
        })
        .withSuccessHandler(function (data) {
          data.rows.forEach(function (row, i) {
            var values = {};
            schema.columns.forEach(function (c, ci) {
              values[c.name] = row[ci] === undefined ? '' : row[ci];
            });

            var r = Validation.validateRecord(schema.columns, values, state.idSets, null);
            r.errors.forEach(function (e) {
              problems.push({ sheet: schema.sheet, row: i + 2, message: e.message });
            });
          });
          next();
        })
        .readSheet(schema.sheet);
    }

    next();
  }

  function init() {
    var picker = document.getElementById('sheet-picker');
    GOOSE_SCHEMA.sheets.forEach(function (s) {
      picker.appendChild(Forms.el('option', { value: s.sheet }, s.sheet));
    });

    picker.addEventListener('change', function () { openSheet(picker.value); });
    document.getElementById('new-record').addEventListener('click', newRecord);
    document.getElementById('save').addEventListener('click', save);
    document.getElementById('publish-check').addEventListener('click', publishCheck);

    loadBundle('icons', function () { openSheet(GOOSE_SCHEMA.sheets[0].sheet); });
  }

  return { init: init, publishCheck: publishCheck };
})();

if (typeof module !== 'undefined') module.exports = { App: App };
```

**Step 3: The page**

`tools/DataEditor/Editor.html`:

```html
<!-- Assembled by build.mjs; modules are inlined because Apps Script has no .js file type. -->
<style>
  body { font: 13px/1.5 -apple-system, system-ui, sans-serif; margin: 0; color: #222; }
  header { display: flex; gap: 8px; align-items: center; padding: 8px;
           border-bottom: 1px solid #ddd; position: sticky; top: 0; background: #fff; }
  main { display: grid; grid-template-columns: 200px 1fr 220px; gap: 12px; padding: 12px; }
  #records { display: flex; flex-direction: column; gap: 2px; max-height: 80vh; overflow: auto; }
  .record { text-align: left; padding: 4px 6px; border: 1px solid #eee; background: #fafafa;
            cursor: pointer; }
  section { border: 1px solid #eee; padding: 8px; margin-bottom: 10px; }
  section h3 { margin: 0 0 6px; font-size: 12px; text-transform: uppercase; color: #666; }
  .field { display: grid; grid-template-columns: 160px 1fr; gap: 6px; align-items: center;
           margin-bottom: 4px; }
  .field label { color: #444; font-family: ui-monospace, monospace; font-size: 11px; }
  input, select { padding: 3px 5px; border: 1px solid #ccc; width: 100%; box-sizing: border-box; }
  .error { color: #b00; font-size: 11px; grid-column: 2; }
  .warn { background: #fff6d8; border: 1px solid #e8cf7a; padding: 6px; margin-bottom: 10px; }
  .ok { color: #060; }
  canvas.preview, canvas.appearance, canvas.effect { image-rendering: pixelated;
    border: 1px solid #eee; background: repeating-conic-gradient(#f4f4f4 0% 25%, #fff 0% 50%)
    50% / 12px 12px; }
  .picker { position: relative; }
  .results { position: absolute; z-index: 10; background: #fff; border: 1px solid #ccc;
             max-height: 200px; overflow: auto; width: 100%; }
  .result { display: block; width: 100%; text-align: left; border: 0; background: none;
            padding: 3px 6px; cursor: pointer; }
  .resolved { font-size: 11px; color: #555; }
  .resolved.bad { color: #b00; }
  .chip { display: inline-flex; gap: 4px; background: #eef; border: 1px solid #ccd;
          padding: 1px 4px; margin: 1px; }
  .equip-slot { display: grid; grid-template-columns: 60px 1fr 44px; gap: 4px;
                align-items: center; }
  .check { display: block; font-size: 11px; }
  @media (prefers-color-scheme: dark) {
    body { background: #1c1c1c; color: #eee; }
    header, section, .record, .results { background: #242424; border-color: #3a3a3a; }
    input, select { background: #2c2c2c; color: #eee; border-color: #444; }
  }
</style>

<header>
  <select id="sheet-picker"></select>
  <button type="button" id="new-record">New</button>
  <button type="button" id="save">Save</button>
  <button type="button" id="publish-check">Check all sheets</button>
  <span id="status"></span>
</header>

<main>
  <div id="records"></div>
  <div id="form"></div>
  <div>
    <div id="previews"></div>
    <div id="publish-results"></div>
  </div>
</main>

<?!= include('schema'); ?>
<?!= include('sprites-icons'); ?>
<?!= include('sprites-parts'); ?>
<?!= include('sprites-effects'); ?>
<?!= include('validation'); ?>
<?!= include('equipped'); ?>
<?!= include('appearance'); ?>
<?!= include('sprites'); ?>
<?!= include('layout'); ?>
<?!= include('pickers'); ?>
<?!= include('composites'); ?>
<?!= include('forms'); ?>
<?!= include('preview'); ?>
<?!= include('app'); ?>

<script>App.init();</script>
```

Include order matters — `appearance` uses `Equipped`, `composites` uses `Pickers` and
`Sprites`, `app` uses everything.

**Step 4: Verify syntax and build**

```bash
node --check tools/DataEditor/src/preview.js
node --check tools/DataEditor/src/app.js
node --test tools/DataEditor/test
node tools/DataEditor/build.mjs
ls tools/DataEditor/dist
```

Expected: all checks clean, `Built 9 modules into dist/`, and `dist/` containing
`Code.gs`, `Editor.html`, `appsscript.json`, `schema.html`, `sprites-*.html`, and the nine
module `.html` files.

**Step 5: Commit**

```bash
git add tools/DataEditor
git commit -m "feat: add previews, app shell and publish check"
```

---

## Task 12: Deploy and smoke test

The first time the editor runs against a real spreadsheet. Nothing here is automatable.

**Step 1: Build**

```bash
dotnet run --project tools/SchemaGen -- tools/DataEditor/schema.js
dotnet run --project tools/SpriteBundle -- ../Goose2ClientGodot/Assets/Sprites tools/DataEditor
node tools/DataEditor/build.mjs
du -ch tools/DataEditor/dist/* | tail -1
```

Expected: total around 4 MB, under the ~10 MB Apps Script project ceiling.

**Step 2: Create the bound script**

In the spreadsheet (`DataLinkId` from `Goose/GooseSettings.json`), choose
**Extensions → Apps Script**. That creates the container-bound project. Then either:

- `clasp login`, `clasp clone <scriptId>`, copy `dist/*` over it, `clasp push`; or
- create each file by hand in the editor and paste the contents of `dist/`.

Set the manifest by enabling **Show "appsscript.json"** in project settings and pasting it.

**Step 3: Deploy as a web app**

**Deploy → New deployment → Web app**, execute as *user accessing*, access *anyone with a
Google account*. Authorise when prompted — it will ask for spreadsheet and UI scopes.

**Step 4: Smoke checklist**

Work through each item and confirm it before considering Part 3 done:

1. Reload the spreadsheet — a **Game Data** menu appears with **Open editor**.
2. The sidebar and the web app URL both load without console errors.
3. The sheet picker lists all 21 sheets.
4. Select **Items** — the record list populates and clicking a record fills the form.
5. Field groups appear in the designed order (Identity, Requirements, Stats, …).
6. The item icon preview renders. Change `graphic_tile` to another id and it updates.
7. Blank an optional numeric field, save, and confirm in the sheet that the cell is **empty**, not `0`.
8. Type a bad enum name — save is refused and the field shows the expected-values message.
9. Set `NPC Drops.item_template_id` to a nonexistent id — save is refused and the message names the id.
10. **New** on Items suggests `max + 1`; typing an existing id blocks the save.
11. Select **NPCs** — the composite appearance preview renders a layered character.
12. Change `body_id` to a value ≥ 100 and confirm only the body sprite draws.
13. Set `body_id` to 1 with empty legs and confirm underwear legs 3 appears.
14. Edit an `equipped_items` slot and confirm both the slot preview and the composite update.
15. Adjust a hair RGBA swatch and blend slider — the preview tint changes; blend 0 leaves it untinted.
16. Select **Spells** — the linked effect animation loops.
17. Select **Maps** — the restart warning banner appears.
18. **Check all sheets** reports either clean or a specific list of problems.
19. Publish path: run `/updatesql` then `/reloadsql` in game and confirm an edit takes effect.

**Step 5: Record the outcome**

Append a short "Deployment notes" section to `tools/README.md` with the script id location
(project settings), the deployment URL, and anything that needed adjusting. Do not commit the
script id if the spreadsheet is private — note where to find it instead.

```bash
git add tools/README.md
git commit -m "docs: record data editor deployment steps"
```

---

## Definition of done

- `node --test tools/DataEditor/test` passes (validation, equipped, appearance, sprites, layout, pickers, composites).
- `node tools/DataEditor/build.mjs` produces a complete `dist/`.
- All 19 smoke items above verified against a real spreadsheet.
- Saving is refused for invalid records, and the message names the offending column and id.
- Blank optional fields stay blank in the sheet.
- NPC previews layer in `SortOrder` order and collapse to the body alone at `body_id >= 100`.
- Restart-only sheets show the warning.
- Total deployed size under 10 MB.

## Known limitations, by design

- Validation logic exists in JavaScript as well as C#. `schema.js` is generated from the
  descriptors, so enum members, defaults and required-ness cannot drift — but the *rules that
  consume them* (range checks, FK semantics) are a second implementation.
- The publish check reads all 21 sheets sequentially, so it takes a few seconds. It is an
  explicit button, not a live check.
- No live collaboration. Two editors on the same record last-write-wins; only id collisions are
  caught, via the pre-write re-check in `writeRow`.
- Each spreadsheet needs its own deployment, and a copied sheet inherits the code but not
  future updates.
