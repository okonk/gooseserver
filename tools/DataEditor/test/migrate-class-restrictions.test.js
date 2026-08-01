// MigrateClassRestrictions.gs, executed against the fake spreadsheet. The migration runs once,
// over data nobody has a second copy of, so the things worth pinning are the ones that are
// invisible afterwards: that the inversion is the inversion (and not an off-by-one), that the
// two values which have no honest conversion are left alone rather than guessed at, and that
// running it twice is refused.
//
// The masks used here are the shipped ones, decoded in composites.test.js: 59 is Rogue-only
// under the deny convention, 31 is Priest-only, 1 restricts nobody.

import { test } from 'node:test';
import assert from 'node:assert/strict';
import { loadMigrationGs } from './fake-sheets.js';

const CLASSES = [
  ['id', 'name'],
  [1, 'Commoner'],
  [2, 'Rogue'],
  [3, 'Warrior'],
  [4, 'Magus'],
  [5, 'Priest'],
  [6, 'Game Master'],
];

// The class_restrictions column sits at a fixed index per sheet (Items 38, Spells 5,
// Combinations 7, Quests 6, all 1-based) and the migration finds it by position. A row is
// therefore mostly padding: an id, and the mask in the right column.
function row(id, mask, width) {
  const cells = new Array(width).fill('');
  cells[0] = id;
  cells[width - 1] = mask;
  return cells;
}

const ITEMS_COL = 38;
const SPELLS_COL = 5;

function itemRow(id, mask) { return row(id, mask, ITEMS_COL); }
function spellRow(id, mask) { return row(id, mask, SPELLS_COL); }

function header(width) { return new Array(width).fill('h'); }

function workbook({ items = [], spells = [], classes = CLASSES } = {}) {
  return {
    Classes: classes,
    Items: [header(ITEMS_COL)].concat(items),
    Spells: [header(SPELLS_COL)].concat(spells),
    Combinations: [header(7)],
    Quests: [header(6)],
  };
}

function masks(sheet, column) {
  return sheet.raw().slice(1).map((r) => r[column - 1]);
}

test('a deny mask becomes the allow mask for exactly the classes it did not deny', () => {
  const gs = loadMigrationGs(workbook({
    items: [itemRow(1, 59), itemRow(2, 31), itemRow(3, 22)],
  }));
  gs.gs.applyClassRestrictionsMigration();

  // 59 denied 1,3,4,5 (and bit 0) -> Rogue.            4
  // 31 denied 1,2,3,4 (and bit 0) -> Priest.          32
  // 22 denied 1,2,4 (bit 0 clear) -> Warrior, Priest.  8 + 32 = 40
  // The Game Master was denied by none of them and is in none of the results — see below.
  assert.deepEqual(masks(gs.sheets.Items, ITEMS_COL), [4, 32, 40]);
});

test('the Game Master is not added to an allow list it merely was not denied by', () => {
  // Nothing in the old data ever restricted the GM, so its absence from a deny mask was not a
  // decision. Carried across, every "Rogue only" row would read "Rogue or Game Master" — in the
  // client's item info line as much as in the editor.
  const gs = loadMigrationGs(workbook({ items: [itemRow(1, 59)] }));
  const plan = gs.gs.previewClassRestrictionsMigration();

  assert.deepEqual(plan.ignoredIds, [6]);
  assert.deepEqual(plan.sheets[0].changes, [{ row: 2, from: 59, to: 4 }]);
});

test('a GM-only row keeps the Game Master, because that one IS a decision', () => {
  // 62 denies classes 1-5 and leaves only the GM. Dropping it would leave an empty allow list,
  // which is 0 — the unrestricted sentinel, and the exact opposite of what the row says.
  const gs = loadMigrationGs(workbook({ items: [itemRow(1, 62), itemRow(2, 63)] }));
  gs.gs.applyClassRestrictionsMigration();
  assert.deepEqual(masks(gs.sheets.Items, ITEMS_COL), [64, 64]);
});

test('a row that denied ONLY the Game Master keeps it out', () => {
  // 64 denies the GM and nobody else: classes 1-5 may use it, the GM may not. That is the one
  // shape where the GM's absence from the result was asked for.
  const gs = loadMigrationGs(workbook({ items: [itemRow(1, 64)] }));
  gs.gs.applyClassRestrictionsMigration();
  assert.deepEqual(masks(gs.sheets.Items, ITEMS_COL), [2 + 4 + 8 + 16 + 32]);
});

test('the ignored class is matched by name, however it is spaced or cased', () => {
  const gs = loadMigrationGs(workbook({
    items: [itemRow(1, 59)],
    classes: [['id', 'name'], [1, 'Commoner'], [2, 'Rogue'], [3, 'Warrior'], [4, 'Magus'],
      [5, 'Priest'], [6, '  game  MASTER ']],
  }));
  assert.deepEqual(gs.gs.previewClassRestrictionsMigration().ignoredIds, [6]);
});

test('a Classes sheet with no Game Master row says so rather than saying nothing', () => {
  // Silence would look like "this workbook has no GM", when it more likely means the name has
  // changed and every migrated mask is about to name a staff class.
  const gs = loadMigrationGs(workbook({
    items: [itemRow(1, 59)],
    classes: [['id', 'name'], [1, 'Commoner'], [2, 'Rogue'], [6, 'Admin']],
  }));
  gs.gs.applyClassRestrictionsMigration();
  assert.deepEqual(gs.gs.previewClassRestrictionsMigration().ignoredIds, []);
  assert.match(gs.logs.join('\n'), /WARNING: no class matched game master/);
  // ...and it is a warning, not a refusal — 59 denies class 1, leaving 2 and 6 — which is why
  // the log line matters: class 6 is in the result precisely because nothing recognised it.
  assert.deepEqual(masks(gs.sheets.Items, ITEMS_COL), [4 + 64]);
});

test('a mask that denied nobody becomes 0, not the all-classes mask', () => {
  // 126 would mean the same thing today and something different tomorrow: a class added to the
  // sheet later inherits the 0 rows and nothing else. 1 denies only bit 0, which is no class.
  const gs = loadMigrationGs(workbook({ items: [itemRow(1, 0), itemRow(2, 1)] }));
  gs.gs.applyClassRestrictionsMigration();
  assert.deepEqual(masks(gs.sheets.Items, ITEMS_COL), [0, 0]);
});

test('a bit belonging to no class is dropped, not carried over', () => {
  // The one that must not be carried: bit 0 survives as an allow-list 1, which reads as
  // "only class 0 may use this" — nobody — where the row meant the opposite.
  const gs = loadMigrationGs(workbook({ items: [itemRow(1, 1)] }));
  const plan = gs.gs.previewClassRestrictionsMigration();
  assert.deepEqual(plan.sheets[0].changes, [{ row: 2, from: 1, to: 0 }]);
});

test('a mask that denied EVERY class is left alone and reported', () => {
  // 126 denies classes 1-6. The allow list has no value for "nobody" — 0 is the unrestricted
  // sentinel — so there is nothing to write and a human has to decide.
  const gs = loadMigrationGs(workbook({ items: [itemRow(1, 126), itemRow(2, 127)] }));
  gs.gs.applyClassRestrictionsMigration();

  assert.deepEqual(masks(gs.sheets.Items, ITEMS_COL), [126, 127]);
  assert.deepEqual(gs.gs.previewClassRestrictionsMigration().sheets[0].unusable,
    [{ row: 2, from: 126 }, { row: 3, from: 127 }]);
  assert.match(gs.logs.join('\n'), /denied every class/);
});

test('a cell that is not a mask is left alone and reported', () => {
  const gs = loadMigrationGs(workbook({ items: [itemRow(1, 'rogue only'), itemRow(2, -4)] }));
  gs.gs.applyClassRestrictionsMigration();

  assert.deepEqual(masks(gs.sheets.Items, ITEMS_COL), ['rogue only', -4]);
  assert.equal(gs.gs.previewClassRestrictionsMigration().sheets[0].unreadable.length, 2);
});

test('a blank cell stays blank — blank means the SQL default, which is 0 either way', () => {
  const gs = loadMigrationGs(workbook({ items: [itemRow(1, '')] }));
  gs.gs.applyClassRestrictionsMigration();
  assert.deepEqual(gs.sheets.Items.writes, []);
});

test('every sheet holding the column is migrated, each at its own column', () => {
  const gs = loadMigrationGs(workbook({
    items: [itemRow(1, 59)],
    spells: [spellRow(1, 15)],
  }));
  gs.gs.applyClassRestrictionsMigration();

  assert.deepEqual(masks(gs.sheets.Items, ITEMS_COL), [4]);
  // 15 denied classes 1-3 -> Magus + Priest. 16 + 32 = 48, the Root/Gate mask.
  assert.deepEqual(masks(gs.sheets.Spells, SPELLS_COL), [48]);
});

test('preview writes nothing at all', () => {
  const gs = loadMigrationGs(workbook({ items: [itemRow(1, 59)] }));
  gs.gs.previewClassRestrictionsMigration();
  assert.deepEqual(gs.sheets.Items.writes, []);
  assert.deepEqual(masks(gs.sheets.Items, ITEMS_COL), [59]);
});

test('unchanged cells are never written, and changed ones batch into runs', () => {
  // Rows 2,3 and 5 change; row 4 holds 0, the one value the migration leaves where it is. Two
  // writes, not three and not one per row.
  const gs = loadMigrationGs(workbook({
    items: [itemRow(1, 59), itemRow(2, 31), itemRow(3, 0), itemRow(4, 15)],
  }));
  gs.gs.applyClassRestrictionsMigration();

  assert.deepEqual(gs.sheets.Items.writes.map((w) => ({ row: w.row, values: w.values })), [
    { row: 2, values: [[4], [32]] },
    { row: 5, values: [[48]] },
  ]);
});

test('a second run is refused, because it would invert the data back', () => {
  const gs = loadMigrationGs(workbook({ items: [itemRow(1, 59)] }));
  gs.gs.applyClassRestrictionsMigration();
  assert.throws(() => gs.gs.applyClassRestrictionsMigration(), /already been migrated/);
  assert.deepEqual(masks(gs.sheets.Items, ITEMS_COL), [4]);

  // ...and force is the way past it, for the restore-from-history case.
  gs.gs.applyClassRestrictionsMigration(true);
  // Read as a deny mask, 4 denies the Rogue alone -> 1,3,4,5 (the GM dropped). This is the
  // round trip NOT being the identity, which is exactly why the guard exists.
  assert.deepEqual(masks(gs.sheets.Items, ITEMS_COL), [58]);
});

test('preview is not gated by the guard', () => {
  const gs = loadMigrationGs(workbook({ items: [itemRow(1, 59)] }),
    { properties: { class_restrictions_migrated_to_allow_list: '2026-08-01T00:00:00.000Z' } });
  assert.equal(gs.gs.previewClassRestrictionsMigration().changed, 1);
});

test('a workbook with no class rows is refused rather than blanking every mask', () => {
  // Without ids there is nothing to allow, so every row would come out "denied every class" —
  // or, if that check were ever loosened, 0 for everything. Stop instead.
  const gs = loadMigrationGs(workbook({ items: [itemRow(1, 59)], classes: [['id', 'name']] }));
  assert.throws(() => gs.gs.previewClassRestrictionsMigration(), /nothing to invert against/);
});

test('a missing target sheet is named', () => {
  const book = workbook({ items: [itemRow(1, 59)] });
  delete book.Quests;
  const gs = loadMigrationGs(book);
  assert.throws(() => gs.gs.previewClassRestrictionsMigration(), /No worksheet named "Quests"/);
});

test('the log names the classes it inverted against and totals the outcome', () => {
  const gs = loadMigrationGs(workbook({ items: [itemRow(1, 59), itemRow(2, 126)] }));
  gs.gs.applyClassRestrictionsMigration();
  const log = gs.logs.join('\n');
  assert.match(log, /classes: 1, 2, 3, 4, 5, 6/);
  assert.match(log, /total: 1 changed, 1 unusable, 0 unreadable/);
});
