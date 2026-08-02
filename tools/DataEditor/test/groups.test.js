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

test('build folds the entry side ids too, so a numeric entry id matches a text cell', () => {
  const groups = Groups.build(schemaOf('NPC Drops'), [DROP('1', 10)], [{ id: 1, name: 'Mouse' }]);
  assert.equal(groups[0].orphan, false);
  assert.equal(groups[0].label, '1 — Mouse');
});

test('build gives a row whose parent is not in the parent sheet its own group, sorted last', () => {
  // Without this the row is invisible under grouping — dead data you cannot find, let alone fix.
  const groups = Groups.build(schemaOf('NPC Drops'), [DROP(4471, 1), DROP(1, 1)], NPCS);
  assert.deepEqual(groups.map((g) => g.label), ['1 — Mouse', '4471 — (not in NPCs)']);
  assert.equal(groups[1].orphan, true);
});

test('build sorts two orphans numerically between themselves', () => {
  const groups = Groups.build(schemaOf('NPC Drops'), [DROP(4471, 1), DROP(900, 1)], NPCS);
  assert.deepEqual(groups.map((g) => g.id), ['900', '4471']);
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
