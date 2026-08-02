// Code.gs, executed. Its header lists the checks that can only be done live (authorisation, the
// menu, payload sizes); these are the ones that can be done here, and they are the two claims the
// editor's data integrity rests on:
//
//   1. A CELL'S VALUE IS ITS VALUE, not its formatting. readSheet used getDisplayValues, so a
//      DECIMAL displayed to 2dp reached the client as 2dp — and since a save posts every column,
//      the next save wrote that back over the stored 4dp. Silently, for every field of the record.
//   2. A SAVE MUST NOT ALTER A CELL THE USER DID NOT EDIT. The client posts the whole record
//      whether it changed or not, so "write the row" and "write the edit" are very different
//      things for a sheet holding formulas, or text the sheet would re-parse on entry.
//
// See fake-sheets.js for what this models about Apps Script and what therefore still has to be
// confirmed live.

import { test } from 'node:test';
import assert from 'node:assert/strict';
import { loadCodeGs } from './fake-sheets.js';

// A row shaped like a real one: an id, a name, a DECIMAL shown to fewer places than it stores, a
// separator-formatted integer, a Bool, and a formula.
const HEADER = ['id', 'name', 'rate', 'value', 'flag', 'total'];
const ROW = [
  7,
  'Iron Sword',
  { value: 0.1235, display: '0.12' },
  { value: 1500, display: '1,500' },
  1,
  { formula: '=C2*D2', value: 185.25 },
];

function sheet(rows, options) {
  return loadCodeGs({ Items: [HEADER].concat(rows) }, options ? { Items: options } : undefined);
}

// --- readSheet ------------------------------------------------------------------------------

test('readSheet returns stored values, not what the cells display', () => {
  const gs = sheet([ROW]);
  const row = gs.readSheet('Items').rows[0];

  // The one that cost real data: the other two digits were gone before the client ever saw them.
  assert.equal(row[2], '0.1235');
  // Not '1,500' — text in an INT column outside en-US, and never a number the importer accepts.
  assert.equal(row[3], '1500');
  // A formula reaches the client as its result. It cannot reach it as a formula: there is nothing
  // in a text input that could hold one, which is exactly why the write side must not rebuild the
  // cell from what the input holds.
  assert.equal(row[5], '185.25');
  assert.deepEqual(row.slice(0, 2), ['7', 'Iron Sword']);
});

test('readSheet reports a real boolean as text rather than inventing a 0 or a 1', () => {
  // A Bool column formatted as a checkbox. Validation says "must be 0 or 1" and the user fixes
  // the cell; coercing it here would be the formatting bug again, in the other direction.
  const gs = sheet([[1, 'x', 0, 0, true, 0]]);
  assert.equal(gs.readSheet('Items').rows[0][4], 'true');
});

test('readSheet falls back to the display value for a Date', () => {
  // No column in the schema is a date, so this is a cell somebody formatted by accident. String()
  // on a Date would hand the user 'Mon Jan 01 1900 00:00:00 GMT+0000 (…)'; the sheet's own
  // rendering at least matches what they are looking at.
  const gs = sheet([[1, 'x', { value: new Date(Date.UTC(2026, 0, 2)), display: '02/01/2026' }, 0, 0, 0]]);
  assert.equal(gs.readSheet('Items').rows[0][2], '02/01/2026');
});

test('readSheet reports an empty cell as the empty string', () => {
  const gs = sheet([[1, null, 0, 0, 0, 0]]);
  assert.equal(gs.readSheet('Items').rows[0][1], '');
});

test('readSheet separates the header and numbers rows from row 2', () => {
  const gs = sheet([ROW, [8, 'Iron Shield', 0, 0, 0, 0]]);
  const data = gs.readSheet('Items');

  assert.deepEqual(data.header, HEADER);
  assert.equal(data.rows.length, 2);
  assert.equal(data.lastRow, 3, 'rows[i] is spreadsheet row i + 2');
});

test('readSheet reports a genuinely empty sheet as empty, not as a blank header', () => {
  const gs = loadCodeGs({ Items: [[]] });
  const data = gs.readSheet('Items');
  assert.deepEqual(data.header, []);
  assert.deepEqual(data.rows, []);
});

test('readSheet names the sheet it could not find', () => {
  const gs = sheet([ROW]);
  assert.throws(() => gs.readSheet('Nope'), /No worksheet named "Nope"/);
});

// --- readSheet: how many times the sheet crosses the wire -------------------------------------

test('readSheet reads the sheet ONCE when no cell is a date', () => {
  // The display values exist for Date cells alone, and no schema column is a date — so the second
  // whole-sheet read was paid on every sheet open for a case the shipped data does not contain.
  // ROW is the awkward one deliberately: a format-bearing decimal, a separator-formatted integer, a
  // Bool and a formula all reach the client correctly from the raw values alone.
  const gs = sheet([ROW, [8, 'Shield', 0, 0, 1, 0]]);
  const data = gs.readSheet('Items');

  assert.deepEqual(gs.sheets.Items.reads.map((r) => r.kind), ['values']);
  assert.equal(data.rows[0][2], '0.1235', 'and the values are the same ones as before');
  assert.equal(data.rows[0][3], '1500');
});

test('readSheet fetches the display values when a cell IS a date, for that sheet only', () => {
  // The fallback is what keeps a stray date-formatted cell recognisable rather than showing the user
  // 'Thu Jan 02 2026 00:00:00 GMT+0000 (…)'. It has to survive the single-pass read.
  const gs = sheet([[1, 'x', { value: new Date(Date.UTC(2026, 0, 2)), display: '02/01/2026' }, 0, 0, 0]]);
  const data = gs.readSheet('Items');

  assert.deepEqual(gs.sheets.Items.reads.map((r) => r.kind), ['values', 'display']);
  assert.equal(data.rows[0][2], '02/01/2026');
  // One display read, not one per date cell: it is the whole range, fetched once.
  const gs2 = loadCodeGs({ Items: [HEADER, [1, 'x',
    { value: new Date(Date.UTC(2026, 0, 2)), display: '02/01/2026' },
    { value: new Date(Date.UTC(2026, 0, 3)), display: '03/01/2026' }, 0, 0]] });
  gs2.readSheet('Items');
  assert.equal(gs2.sheets.Items.reads.filter((r) => r.kind === 'display').length, 1);
});

// --- writeRow: what it leaves alone ---------------------------------------------------------

// What the client posts back for a record it has not edited: exactly what readSheet gave it.
function unedited(gs, row) {
  return gs.readSheet('Items').rows[row - 2].slice();
}

test('saving an unchanged record writes nothing at all', () => {
  const gs = sheet([ROW]);
  gs.writeRow('Items', 2, unedited(gs, 2), 0);

  assert.deepEqual(gs.sheets.Items.writes, [], 'not one range may be written');
  // The claims that matter, restated as state rather than as calls.
  assert.equal(gs.sheets.Items.cell(2, 6).formula, '=C2*D2', 'the formula survives');
  assert.equal(gs.sheets.Items.raw()[1][2], 0.1235, 'the stored precision survives');
  assert.equal(gs.sheets.Items.raw()[1][3], 1500);
});

test('editing one field writes only that field', () => {
  const gs = sheet([ROW]);
  const cells = unedited(gs, 2);
  cells[1] = 'Steel Sword';

  gs.writeRow('Items', 2, cells, 0);

  assert.deepEqual(gs.sheets.Items.writes, [{ row: 2, col: 2, values: [['Steel Sword']] }]);
  assert.equal(gs.sheets.Items.cell(2, 6).formula, '=C2*D2');
  assert.equal(gs.sheets.Items.raw()[1][2], 0.1235);
});

test('editing the formatted decimal writes the value the user typed, in full', () => {
  const gs = sheet([ROW]);
  const cells = unedited(gs, 2);
  cells[2] = '0.4321';

  gs.writeRow('Items', 2, cells, 0);

  assert.deepEqual(gs.sheets.Items.writes, [{ row: 2, col: 3, values: [['0.4321']] }]);
  assert.equal(gs.sheets.Items.raw()[1][2], '0.4321');
});

test('adjacent edits are one write and separated edits are two', () => {
  // The run grouping is not just an API-call count: a run that merged across an untouched cell
  // would rewrite that cell, which is the whole bug.
  const adjacent = sheet([ROW]);
  const a = unedited(adjacent, 2);
  a[1] = 'Steel Sword';
  a[2] = '0.5';
  adjacent.writeRow('Items', 2, a, 0);
  assert.deepEqual(adjacent.sheets.Items.writes,
    [{ row: 2, col: 2, values: [['Steel Sword', '0.5']] }]);

  const apart = sheet([ROW]);
  const b = unedited(apart, 2);
  b[1] = 'Steel Sword';
  b[4] = '0';
  apart.writeRow('Items', 2, b, 0);
  assert.deepEqual(apart.sheets.Items.writes, [
    { row: 2, col: 2, values: [['Steel Sword']] },
    { row: 2, col: 5, values: [['0']] },
  ]);
  assert.equal(apart.sheets.Items.raw()[1][2], 0.1235, 'the cell between them is untouched');
});

test('clearing a field still overwrites the cell', () => {
  // The other direction of "only write what changed": blank is a change. null is what app.js
  // sends for a cell it must not write a value into, and it has to mean an empty cell here.
  const gs = sheet([ROW]);
  const cells = unedited(gs, 2);
  cells[1] = null;

  gs.writeRow('Items', 2, cells, 0);

  assert.deepEqual(gs.sheets.Items.writes, [{ row: 2, col: 2, values: [['']] }]);
  assert.equal(gs.sheets.Items.raw()[1][1], '');
});

test('a formula cell the user DID edit is replaced, because that is what they asked for', () => {
  const gs = sheet([ROW]);
  const cells = unedited(gs, 2);
  cells[5] = '999';

  gs.writeRow('Items', 2, cells, 0);

  assert.deepEqual(gs.sheets.Items.writes, [{ row: 2, col: 6, values: [['999']] }]);
  assert.equal(gs.sheets.Items.cell(2, 6), '999');
});

test('a record posted back through readSheet round-trips to itself for every cell', () => {
  // The general form of the whole fix: read a record, save it untouched, and the sheet is
  // byte-for-byte what it was. Two rows, so a write that leaked into a neighbour would show.
  const second = [8, 'Shield', { value: 2.5, display: '2.50' }, { value: 20000, display: '20,000' },
                  0, { formula: '=1+1', value: 2 }];
  const gs = sheet([ROW, second]);
  const before = JSON.stringify(gs.sheets.Items.raw());

  gs.writeRow('Items', 2, unedited(gs, 2), 0);
  gs.writeRow('Items', 3, unedited(gs, 3), 0);

  assert.equal(JSON.stringify(gs.sheets.Items.raw()), before);
  assert.deepEqual(gs.sheets.Items.writes, []);
});

// --- writeRow: two editors (the loaded snapshot) ----------------------------------------------

test('a concurrent edit to a cell the user did NOT touch survives the save', () => {
  // Editor A opens the row; editor B fixes `value`; A renames the item and saves. Diffing
  // against the sheet's CURRENT values called B's fix "a change A posted" and reverted it.
  const gs = sheet([ROW]);
  const loaded = unedited(gs, 2);
  gs.sheets.Items.cells[1][3] = 2000;                    // B's edit, after A loaded

  const cells = loaded.slice();
  cells[1] = 'Steel Sword';                              // A's edit
  gs.writeRow('Items', 2, cells, 0, { loaded });

  assert.deepEqual(gs.sheets.Items.writes, [{ row: 2, col: 2, values: [['Steel Sword']] }]);
  assert.equal(gs.sheets.Items.raw()[1][3], 2000, "B's edit is still there");
});

test('a conflicting edit to the SAME cell is refused, naming the column, writing nothing', () => {
  const gs = sheet([ROW]);
  const loaded = unedited(gs, 2);
  gs.sheets.Items.cells[1][1] = 'Renamed by B';

  const cells = loaded.slice();
  cells[1] = 'Renamed by A';
  cells[2] = '0.5';                                      // an edit that WOULD be clean
  assert.throws(() => gs.writeRow('Items', 2, cells, 0, { loaded }),
    /Items row 2: name changed in the sheet while you were editing/);
  assert.deepEqual(gs.sheets.Items.writes, [], 'the clean edit must not half-land');
});

test('both editors arriving at the same value is not a conflict', () => {
  const gs = sheet([ROW]);
  const loaded = unedited(gs, 2);
  gs.sheets.Items.cells[1][1] = 'Steel Sword';

  const cells = loaded.slice();
  cells[1] = 'Steel Sword';
  gs.writeRow('Items', 2, cells, 0, { loaded });
  assert.deepEqual(gs.sheets.Items.writes, [], 'the cell already holds it');
});

test('a shifted row on a no-pk sheet is refused instead of overwritten', () => {
  // The nine no-pk sheets have no duplicate scan, so a row inserted above a stale client used
  // to hand its save a DIFFERENT record to overwrite wholesale. With the snapshot, the current
  // row matches nothing the client loaded and the edited cell conflicts.
  const gs = loadCodeGs(
    { Drops: [['npc_id', 'item_id'], [5, 1], [9, 4]] }, { Drops: { maxRows: 10 } });
  const loaded = ['9', '4'];                             // the client opened what WAS row 2...
  gs.sheets.Drops.cells.splice(1, 0, [2, 7]);            // ...then someone inserted a row above

  assert.throws(() => gs.writeRow('Drops', 2, ['9', '8'], -1, { loaded }),
    /changed in the sheet while you were editing/);
  assert.deepEqual(gs.sheets.Drops.writes, []);
});

test('writeRow refuses a loaded snapshot that is not the header width', () => {
  const gs = sheet([ROW]);
  assert.throws(() => gs.writeRow('Items', 2, unedited(gs, 2), 0, { loaded: ['7', 'x'] }),
    /loaded snapshot is 2 values wide for a header 6/);
});

test('without a snapshot writeRow still diffs against the sheet, as before', () => {
  const gs = sheet([ROW]);
  const cells = unedited(gs, 2);
  cells[1] = 'Steel Sword';
  gs.writeRow('Items', 2, cells, 0);
  assert.deepEqual(gs.sheets.Items.writes, [{ row: 2, col: 2, values: [['Steel Sword']] }]);
});

// --- writeRow: Text cells and Sheets' entry parsing -------------------------------------------

test('an edited Text cell is pinned to plain text before its value is written', () => {
  // setValues parses strings like typed entry: "1-2" becomes a Date, "01" becomes 1. The pin
  // must come BEFORE the value lands and must touch only the edited Text cell.
  const gs = sheet([ROW]);
  const cells = unedited(gs, 2);
  cells[1] = '1-2';
  cells[3] = '9';                                        // an INT edit gets no pin

  gs.writeRow('Items', 2, cells, 0, { textColumns: [1] });

  assert.deepEqual(gs.sheets.Items.writes, [
    { row: 2, col: 2, format: '@' },
    { row: 2, col: 2, values: [['1-2']] },
    { row: 2, col: 4, values: [['9']] },
  ]);
});

test('an appended row pins its non-blank Text cells too', () => {
  const gs = sheet([ROW], { maxRows: 10 });
  gs.writeRow('Items', 0, ['8', '01', '1', '2', '0', '3'], 0, { textColumns: [1] });

  assert.deepEqual(gs.sheets.Items.writes, [
    { row: 3, col: 2, format: '@' },
    { row: 3, col: 1, values: [['8', '01', '1', '2', '0', '3']] },
  ]);
});

test('clearing a Text cell does not bother pinning a format onto a blank', () => {
  const gs = sheet([ROW]);
  const cells = unedited(gs, 2);
  cells[1] = null;
  gs.writeRow('Items', 2, cells, 0, { textColumns: [1] });
  assert.deepEqual(gs.sheets.Items.writes, [{ row: 2, col: 2, values: [['']] }]);
});

// --- writeRow: appending --------------------------------------------------------------------

test('an append writes the whole row, since there is nothing to compare against', () => {
  const gs = sheet([ROW], { maxRows: 10 });
  gs.writeRow('Items', 0, ['8', 'Shield', '1', '2', '0', '3'], 0);

  assert.deepEqual(gs.sheets.Items.writes,
    [{ row: 3, col: 1, values: [['8', 'Shield', '1', '2', '0', '3']] }]);
  assert.equal(gs.sheets.Items.getLastRow(), 3);
});

test('an append to a sheet trimmed to exactly its data grows the grid first', () => {
  // getRange past the bottom throws, so without insertRowsAfter the first append to a trimmed
  // sheet fails outright.
  const gs = sheet([ROW]);
  assert.equal(gs.sheets.Items.getMaxRows(), 2);

  gs.writeRow('Items', 0, ['8', 'Shield', '1', '2', '0', '3'], 0);

  assert.equal(gs.sheets.Items.getMaxRows(), 3);
  assert.equal(gs.sheets.Items.raw()[2][0], '8');
});

test('writeRow answers with the row it landed on', () => {
  const gs = sheet([ROW], { maxRows: 10 });
  assert.deepEqual(gs.writeRow('Items', 0, ['8', 'x', '1', '2', '0', '3'], 0), { row: 3 });
  assert.deepEqual(gs.writeRow('Items', 2, unedited(gs, 2), 0), { row: 2 });
});

test('writeRow flushes, so a following read sees the write', () => {
  const gs = sheet([ROW], { maxRows: 10 });
  gs.writeRow('Items', 0, ['8', 'x', '1', '2', '0', '3'], 0);
  assert.equal(gs.flushes(), 1);
});

// --- writeRow: the guards that were only ever reasoning --------------------------------------

test('writeRow refuses a cells array that is not the header width', () => {
  const gs = sheet([ROW]);
  assert.throws(() => gs.writeRow('Items', 2, ['7', 'x'], 0),
    /got 2 values for a header 6 columns wide/);
  assert.deepEqual(gs.sheets.Items.writes, [], 'and writes nothing');
});

test('writeRow refuses the header row, as a number and as a string', () => {
  const gs = sheet([ROW]);
  [1, '1'].forEach((row) => {
    assert.throws(() => gs.writeRow('Items', row, unedited(gs, 2), 0),
      /refusing to overwrite the header row/);
  });
});

test('writeRow refuses a row past the end instead of inserting thousands of blanks', () => {
  const gs = sheet([ROW], { maxRows: 5000 });
  assert.throws(() => gs.writeRow('Items', 900, unedited(gs, 2), 0),
    /row 900 is past the end of the sheet \(2 rows\)/);
  assert.equal(gs.sheets.Items.getLastRow(), 2);
});

test('writeRow refuses a row number that is not one', () => {
  const gs = sheet([ROW]);
  // '' and null coerce to 0 through Number(), which is the APPEND sentinel — a client reading a
  // missing DOM attribute would silently add a record instead of editing one.
  ['', null, undefined, 'x', 1.5, -1].forEach((row) => {
    assert.throws(() => gs.writeRow('Items', row, unedited(gs, 2), 0), /invalid row/,
      JSON.stringify(row));
  });
});

test('writeRow rejects an id another row already has, naming that row', () => {
  const gs = sheet([ROW, [8, 'Shield', 0, 0, 0, 0]], { maxRows: 10 });
  assert.throws(() => gs.writeRow('Items', 0, ['8', 'Dagger', '0', '0', '0', '0'], 0),
    /id 8 is already used by row 3/);
  assert.deepEqual(gs.sheets.Items.writes, []);
});

test('writeRow lets a row keep its own id', () => {
  const gs = sheet([ROW]);
  const cells = unedited(gs, 2);
  cells[1] = 'Steel Sword';
  gs.writeRow('Items', 2, cells, 0);
  assert.equal(gs.sheets.Items.raw()[1][1], 'Steel Sword');
});

test('the duplicate check compares ids by value, not by text', () => {
  // An id column with a numeric format displays 8 as '8.00'. A client holding that posts '8.00',
  // and a raw comparison against 8 would miss the collision — the one check that stops two
  // editors taking the same id.
  const gs = sheet([ROW, [{ value: 8, display: '8.00' }, 'Shield', 0, 0, 0, 0]], { maxRows: 10 });
  assert.throws(() => gs.writeRow('Items', 0, ['8.00', 'Dagger', '0', '0', '0', '0'], 0),
    /id 8 is already used by row 3/);
});

// The id column read, which is the one the duplicate scan makes: column 1, rows 2..lastRow.
function idColumnReads(sheetObject, rows) {
  return sheetObject.reads.filter((r) => r.kind === 'values' && r.col === 1 && r.row === 2 &&
                                        r.numCols === 1 && r.numRows === rows);
}

test('an edit that does not touch the id does not scan the id column at all', () => {
  // The scan reads every id in the sheet — 4,322 cells on NPC Spawns — to answer a question this
  // save cannot have changed the answer to: it is not taking an id, it is editing a name.
  const gs = sheet([ROW, [8, 'Shield', 0, 0, 0, 0]], { maxRows: 10 });
  const loaded = unedited(gs, 2);
  const cells = loaded.slice();
  cells[1] = 'Steel Sword';

  gs.sheets.Items.reads.length = 0;
  gs.writeRow('Items', 2, cells, 0, { loaded });

  assert.deepEqual(idColumnReads(gs.sheets.Items, 2), []);
  assert.deepEqual(gs.sheets.Items.writes, [{ row: 2, col: 2, values: [['Steel Sword']] }]);
});

test('RENUMBERING a row onto another row\'s id is still refused', () => {
  // The skip is exactly "the id did not move". The moment it does, the scan is the only thing
  // standing between two records and one id.
  const gs = sheet([ROW, [8, 'Shield', 0, 0, 0, 0]], { maxRows: 10 });
  const loaded = unedited(gs, 2);
  const cells = loaded.slice();
  cells[0] = '8';

  assert.throws(() => gs.writeRow('Items', 2, cells, 0, { loaded }),
    /id 8 is already used by row 3/);
  assert.deepEqual(gs.sheets.Items.writes, []);
});

test('an id that changed only in FORMATTING counts as unchanged', () => {
  // The client may hold '7.00' for a stored 7 and post it back verbatim. idKey_ folds both, on both
  // sides of the comparison, so this is an ordinary edit and not a renumber.
  const gs = sheet([ROW, [8, 'Shield', 0, 0, 0, 0]], { maxRows: 10 });
  const loaded = unedited(gs, 2);
  const cells = loaded.slice();
  cells[0] = '7.00';
  cells[1] = 'Steel Sword';

  gs.sheets.Items.reads.length = 0;
  gs.writeRow('Items', 2, cells, 0, { loaded });
  assert.deepEqual(idColumnReads(gs.sheets.Items, 2), []);
});

test('an APPEND is scanned even when a snapshot is supplied', () => {
  // app.js sends no snapshot for an append, but the skip must not rest on that: a new row takes an
  // id nobody held, so there is nothing for a snapshot to prove.
  const gs = sheet([ROW, [8, 'Shield', 0, 0, 0, 0]], { maxRows: 10 });
  const loaded = unedited(gs, 2);
  assert.throws(() => gs.writeRow('Items', 0, ['8', 'Dagger', '0', '0', '0', '0'], 0, { loaded }),
    /id 8 is already used by row 3/);
});

test('without a snapshot the id column is still scanned', () => {
  // A caller that predates options.loaded has nothing to compare the id against, so it keeps the
  // scan rather than losing the check.
  const gs = sheet([ROW, [8, 'Shield', 0, 0, 0, 0]], { maxRows: 10 });
  const cells = unedited(gs, 2);
  cells[1] = 'Steel Sword';

  gs.sheets.Items.reads.length = 0;
  gs.writeRow('Items', 2, cells, 0);
  assert.equal(idColumnReads(gs.sheets.Items, 2).length, 1);
});

test('a whitespace id skips the duplicate check rather than matching every blank id cell', () => {
  // idKey_ folds '  ' to '', and so does every blank id cell in the column — so a raw comparison
  // would report a clash with the first of them and refuse a legitimate write.
  const gs = sheet([ROW, [null, 'Shield', 0, 0, 0, 0]], { maxRows: 10 });
  gs.writeRow('Items', 0, ['  ', 'Dagger', '0', '0', '0', '0'], 0);
  assert.equal(gs.sheets.Items.raw()[3][1], 'Dagger');
});

test('the nine sheets with no pk pass -1 and may repeat column A', () => {
  // NPC Drops: two drops for one npc. idColumnIndex 0 would reject every second row.
  const gs = loadCodeGs({ Drops: [['npc_id', 'item_id'], [5, 1]] }, { Drops: { maxRows: 10 } });
  gs.writeRow('Drops', 0, ['5', '2'], -1);
  assert.deepEqual(gs.sheets.Drops.raw()[2], [5, 2].map(String));
});

test('a null idColumnIndex does not silently check column A', () => {
  // `null >= 0` is true in JS, so this one is a real hazard rather than a hypothetical.
  const gs = loadCodeGs({ Drops: [['npc_id', 'item_id'], [5, 1]] }, { Drops: { maxRows: 10 } });
  gs.writeRow('Drops', 0, ['5', '2'], null);
  assert.equal(gs.sheets.Drops.getLastRow(), 3);
});

test('writeRow refuses a sheet with no header row', () => {
  const gs = loadCodeGs({ Items: [[]] }, { Items: { maxRows: 5, maxCols: 3 } });
  assert.throws(() => gs.writeRow('Items', 0, ['1', '2', '3'], 0), /has no header row/);
});

test('writeRow refuses a cells argument that is not an array', () => {
  const gs = sheet([ROW]);
  assert.throws(() => gs.writeRow('Items', 2, '7,x', 0), /cells must be an array/);
});

// --- readSheetIndex -------------------------------------------------------------------------

test('readSheetIndex labels entries from the column it is told to, not always B', () => {
  // Items holds item_usetype in B and item_name at index 2; hardcoding 1 labels all 649 entries
  // 'Armor'/'Weapon'.
  const gs = loadCodeGs({
    Items: [['id', 'usetype', 'name'], [7, 'Weapon', 'Iron Sword'], [8, 'Armor', 'Iron Shield']],
  });

  assert.deepEqual(gs.readSheetIndex('Items', 2).entries,
    [{ id: '7', name: 'Iron Sword' }, { id: '8', name: 'Iron Shield' }]);
  // Omitted, it defaults to 1 — right for the six sheets whose column B IS the name.
  assert.deepEqual(gs.readSheetIndex('Items').entries.map((e) => e.name), ['Weapon', 'Armor']);
});

test('readSheetIndex skips a row whose id is blank or whitespace', () => {
  // A phantom id 0 in the client's FK set would validate a nonexistent reference as fine.
  const gs = loadCodeGs({ Items: [['id', 'name'], [null, 'orphan'], ['  ', 'orphan'], [7, 'Sword']] });
  assert.deepEqual(gs.readSheetIndex('Items', 1).entries, [{ id: '7', name: 'Sword' }]);
});

test('readSheetIndex on a sheet with only a header returns no entries', () => {
  const gs = loadCodeGs({ Items: [['id', 'name']] });
  assert.deepEqual(gs.readSheetIndex('Items', 1).entries, []);
});

test('readSheetIndex ships the RAW id, not the display text', () => {
  // The client builds its FK set with Number(entry.id), so a separator or decimal format on the
  // id column would ship "1,024" / "651.00", read as NaN / mismatch, and block every save that
  // references the sheet — FK validation fails closed. The label stays display text: it is only
  // read by humans.
  const gs = loadCodeGs({ Items: [
    ['id', 'name'],
    [{ value: 1024, display: '1,024' }, 'Crown'],
    [{ value: 651, display: '651.00' }, 'Sword'],
  ] });

  assert.deepEqual(gs.readSheetIndex('Items', 1).entries,
    [{ id: '1024', name: 'Crown' }, { id: '651', name: 'Sword' }]);
});

test('readSheetIndex returns the extra column raw when asked, and omits it otherwise', () => {
  // The one caller today: the Spells preview resolves spell_effect_id to the row's
  // spell_animation, the id space the effects atlas is keyed by.
  const gs = loadCodeGs({ Effects: [
    ['id', 'name', 'animation'],
    [4, 'Flame', { value: 77, display: '77.00' }],
    [9, 'Frost', null],
  ] });

  assert.deepEqual(gs.readSheetIndex('Effects', 1, 2).entries,
    [{ id: '4', name: 'Flame', extra: '77' }, { id: '9', name: 'Frost', extra: '' }]);
  assert.deepEqual(gs.readSheetIndex('Effects', 1).entries,
    [{ id: '4', name: 'Flame' }, { id: '9', name: 'Frost' }]);
});

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
  // writeRow's rule: a duplicate already in the column is the publish check's to report. Only an
  // id this batch CLAIMS can collide.
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
