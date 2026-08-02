// The grouping model: which parent each child row belongs to, and what the list of parents looks
// like — and the table that model is drawn as. The model half is pure data because that is where
// the awkward cases live and they are far easier to state as values than as markup.

import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { installFakeDom, fire, createElement } from './fake-dom.js';

installFakeDom();

const schemaSource = readFileSync(fileURLToPath(new URL('../schema.js', import.meta.url)), 'utf8');
globalThis.GOOSE_SCHEMA = new Function(schemaSource + '\nreturn GOOSE_SCHEMA;')();

const { Validation } = await import('../src/validation.js');
globalThis.Validation = Validation;
const { Layout } = await import('../src/layout.js');
globalThis.Layout = Layout;
const { Pickers } = await import('../src/pickers.js');
globalThis.Pickers = Pickers;
const { Forms } = await import('../src/forms.js');
globalThis.Forms = Forms;
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
  // Both, deliberately: the attribute is what the markup says and the property is what a real
  // input actually enforces, and only one of them being set is a field that looks locked and
  // is not (or vice versa).
  assert.equal(existing.readOnly, true);
  assert.notEqual(existing.getAttribute('readonly'), null);

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

test('two panels in one document never share a control id', () => {
  // The prefix counter has to be module-wide, not per panel: ids live in one document, and two
  // panels each starting their own count at 0 both mint g0- — the collision the prefix exists
  // to prevent, and mounting several panels at once is why render takes a container at all.
  const a = panelFor('NPC Drops', [DROP(1, 10), DROP(1, 20)], NPCS);
  const b = panelFor('NPC Drops', [DROP(2, 30)], NPCS);
  const idsOf = (c) => [...c.querySelectorAll('input[type=text]')].map((n) => n.id);
  const ids = idsOf(a.container).concat(idsOf(b.container));
  assert.equal(new Set(ids).size, ids.length, 'duplicate control ids: ' + ids.join(', '));
});

test('re-rendering a container replaces its rows and forgets the removals', () => {
  // Switching group in place is a re-render, so a deletion staged against the group you have
  // navigated away from must not still be pending against the one now on screen.
  const schema = schemaOf('NPC Drops');
  const groups = Groups.build(schema, [DROP(1, 10), DROP(1, 20), DROP(2, 30)], NPCS);
  const container = createElement('div');
  const ctx = ctxFor({ NPCs: NPCS, Items: [] });

  Groups.render({ container, schema, group: groups[0], ctx, ids: [] });
  fire(container.querySelectorAll('[data-remove]')[0], 'click');
  assert.equal(Groups.removed(container).length, 1);

  Groups.render({ container, schema, group: groups[1], ctx, ids: [] });
  assert.deepEqual(Groups.removed(container), []);
  assert.deepEqual(Groups.collect(container, schema).map((r) => r.rowNumber), [4]);
});

test('every editable cell carries its column name as an accessible name', () => {
  // The table draws no labels — Forms.render is where those come from and this does not use it
  // — so a control with nothing but a header span above it is unnamed to a screen reader.
  const { container } = panelFor('NPC Drops', [DROP(1, 10)], NPCS);
  const named = [...container.querySelectorAll('[name]')];
  assert.ok(named.length);
  named.forEach((n) => {
    assert.ok(n.getAttribute('aria-label') || n.getAttribute('aria-labelledby'),
              n.getAttribute('name') + ' has no accessible name');
  });
});

test('the header count follows the rows as they are added and removed', () => {
  const { container } = panelFor('NPC Drops', [DROP(1, 10)], NPCS);
  const count = container.querySelectorAll('[class=count]')[0];
  assert.equal(count.textContent, '1 row');

  Groups.addRow(container);
  assert.equal(count.textContent, '2 rows');

  fire(container.querySelectorAll('[data-remove]')[0], 'click');
  assert.equal(count.textContent, '1 row');
});
