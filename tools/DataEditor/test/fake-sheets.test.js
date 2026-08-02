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
  gs.writeRow('Items', 2, ['1', 'UNO'], -1);
  assert.deepEqual(gs.locks(), { acquired: 1, released: 1, held: false });
});

test('a lock that cannot be obtained throws from waitLock', () => {
  const gs = loadCodeGs({ Items: GRID.map((row) => row.slice()) }, {}, { lockFails: true });
  assert.throws(() => gs.writeRow('Items', 2, ['1', 'ONE'], -1), /lock/i);
});
