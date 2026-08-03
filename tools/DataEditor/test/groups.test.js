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

function rowValues(sheet, row) {
  const values = {};
  schemaOf(sheet).columns.forEach((c, i) => { values[c.name] = row[i]; });
  return values;
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
  assert.equal(ops.schemaWidth, schemaOf('NPC Drops').columns.length);
  // -1: NPC Drops has no pk, and its column A is an FK that legitimately repeats.
  assert.equal(ops.idColumnIndex, -1);
  assert.deepEqual(ops.textColumns, []);
});

test('ops reports the id column of a sheet that has a pk', () => {
  const ops = Groups.ops(schemaOf('Quest Reqs'), [], [], {});
  assert.equal(ops.idColumnIndex, 0);
});

test('ops reports Text columns so the server pins their format', () => {
  // Quest Rewards, not Quest Reqs: Reqs has no Text column, so asserting over its (empty) list
  // asserts nothing. string_value is column F and the server pins it to '@' so a reward value
  // like "1.10" is not stored as a number.
  const ops = Groups.ops(schemaOf('Quest Rewards'), [], [], {});
  const names = schemaOf('Quest Rewards').columns.map((c) => c.name);
  assert.deepEqual(ops.textColumns, [names.indexOf('string_value')]);
  assert.deepEqual(ops.textColumns, [5]);
});

test('an existing row with no loaded snapshot is written, never appended', () => {
  // The only path in the module that could ADD data nobody asked for. A row that exists in the
  // sheet is an update whatever state the client is in; an empty snapshot posts as a row of
  // blanks, which the server compares and refuses. Refusing is the safe failure, duplicating
  // the record is not.
  const ops = opsFor('NPC Drops', [{ rowNumber: 2, values: LOADED(1, 10, '0.25'), loaded: null }]);
  assert.deepEqual(ops.appends, []);
  assert.equal(ops.writes.length, 1);
  assert.equal(ops.writes[0].row, 2);
  assert.deepEqual(ops.writes[0].loaded, ['', '', '', '']);
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

test('rows differing only in their coordinates are not duplicates', () => {
  // Warptiles and NPC Spawns have no key entry precisely for this: a wall of warp tiles all
  // leading to one destination is how the sheet is authored, and the position is the identity.
  const tile = (x, y) => {
    const values = {};
    schemaOf('Warptiles').columns.forEach((c) => { values[c.name] = ''; });
    values.map_id = '1';
    values.map_x = String(x);
    values.map_y = String(y);
    values.warp_id = '2';
    values.warp_x = '5';
    values.warp_y = '5';
    return values;
  };
  const rows = [{ rowNumber: 2, values: tile(1, 1), loaded: tile(1, 1) },
                { rowNumber: 3, values: tile(1, 2), loaded: tile(1, 2) }];
  assert.equal(Groups.validate(schemaOf('Warptiles'), rows, {}).duplicates, 0);
});

test('duplicate detection folds ids, so 1 and "1.00" are the same row', () => {
  const a = LOADED('1', '10', '0.25');
  const b = LOADED('1.00', '10.00', '0.25');
  const result = Groups.validate(schemaOf('NPC Drops'),
                                 [{ rowNumber: 2, values: a, loaded: a },
                                  { rowNumber: 3, values: b, loaded: b }], {});
  assert.equal(result.duplicates, 2);
});

test('three rows sharing a key count as three duplicates, not two', () => {
  const rows = ['0.10', '0.20', '0.30'].map((rate, i) => {
    const values = LOADED(1, 10, rate);
    return { rowNumber: i + 2, values, loaded: values };
  });
  assert.equal(Groups.validate(schemaOf('NPC Drops'), rows, {}).duplicates, 3);
});

const REQ = (type, value) => {
  const values = {};
  schemaOf('Quest Reqs').columns.forEach((c) => { values[c.name] = ''; });
  values.id = '1';
  values.quest_id = '1';
  values.requirement_type = type;
  values.requirement_value = value;
  values.requirement_value2 = '0';
  values.keep_requirement = 'FALSE';
  return values;
};

test('an unkeyed sheet falls back to every non-pk column', () => {
  // Quest Reqs has no meaningful subset key, so two rows are the same record only when their
  // whole content matches — the strictest reading, which never flags a row that really differs.
  const a = REQ('Item', '5');
  const b = REQ('Item', '5');
  const same = Groups.validate(schemaOf('Quest Reqs'),
                               [{ rowNumber: 2, values: a, loaded: a },
                                { rowNumber: 3, values: b, loaded: b }], {});
  assert.equal(same.duplicates, 2);
});

test('the fallback key ignores the pk, so two rows differing only in id are one record', () => {
  const a = REQ('Item', '5');
  const b = REQ('Item', '5');
  b.id = '2';
  assert.equal(Groups.validate(schemaOf('Quest Reqs'),
                               [{ rowNumber: 2, values: a, loaded: a },
                                { rowNumber: 3, values: b, loaded: b }], {}).duplicates, 2);
});

test('a row differing in one non-pk column is not a duplicate under the fallback', () => {
  const a = REQ('Item', '5');
  const b = REQ('Item', '6');
  assert.equal(Groups.validate(schemaOf('Quest Reqs'),
                               [{ rowNumber: 2, values: a, loaded: a },
                                { rowNumber: 3, values: b, loaded: b }], {}).duplicates, 0);
});

test('an existing row is not flagged as a duplicate of ITSELF', () => {
  // The sheet the group was read from is what built idSets.__self, so every existing row's id is
  // already in it. Without an ownId taken from the loaded state, every row of Quest Reqs and
  // Quest Rewards reports "id N is already used" and the whole group is unsavable.
  const row = REQ('Item', '5');
  row.keep_requirement = '0';
  const result = Groups.validate(schemaOf('Quest Reqs'),
                                 [{ rowNumber: 2, values: row, loaded: row }],
                                 { __self: new Set([1]) });
  assert.deepEqual(result.rows[0].errors, []);
  assert.equal(result.ok, true);
});

test('a row given ANOTHER row\'s id is still flagged', () => {
  // ownId comes from the loaded state, never from the field — the single-record save()'s rule,
  // and the reason typing over the id cannot exempt itself.
  const row = REQ('Item', '5');
  const loaded = REQ('Item', '5');
  row.keep_requirement = '0';
  row.id = '9';
  const result = Groups.validate(schemaOf('Quest Reqs'),
                                 [{ rowNumber: 2, values: row, loaded: loaded }],
                                 { __self: new Set([1, 9]) });
  assert.equal(result.ok, false);
  assert.equal(result.rows[0].errors[0].column, 'id');
});

test('an APPENDED row carries no loaded state and is checked against the whole id set', () => {
  // addRow allocated its id with nextId over state.ids, so a clean allocation is not in __self
  // and validates — but it has nothing to be exempted from if it ever collides.
  const fresh = REQ('Item', '5');
  fresh.keep_requirement = '0';
  fresh.id = '4';
  assert.equal(Groups.validate(schemaOf('Quest Reqs'),
                               [{ rowNumber: 0, values: fresh, loaded: null }],
                               { __self: new Set([1, 2, 3]) }).ok, true);
  assert.equal(Groups.validate(schemaOf('Quest Reqs'),
                               [{ rowNumber: 0, values: fresh, loaded: null }],
                               { __self: new Set([1, 4]) }).ok, false);
});

test('an existing orphan row may edit another cell while keeping its hidden parent', () => {
  const values = rowValues('NPC Drops', DROP(4471, 10, '0.25'));
  const loaded = Object.assign({}, values, { droprate: '0.10' });
  const result = Groups.validate(schemaOf('NPC Drops'),
                                 [{ rowNumber: 2, values, loaded }],
                                 { NPCs: new Set([1, 2]), Items: new Set([10]) });

  assert.equal(result.ok, true);
  assert.deepEqual(result.rows[0].errors, []);
});

test('an existing blank-parent row may edit another cell while keeping its hidden parent', () => {
  const values = rowValues('NPC Drops', DROP('', 10, '0.25'));
  const loaded = Object.assign({}, values, { droprate: '0.10' });
  const result = Groups.validate(schemaOf('NPC Drops'),
                                 [{ rowNumber: 2, values, loaded }],
                                 { NPCs: new Set([1, 2]), Items: new Set([10]) });

  assert.equal(result.ok, true);
  assert.deepEqual(result.rows[0].errors, []);
});

test('a new row under an orphan parent must still pass parent validation', () => {
  const values = rowValues('NPC Drops', DROP(4471, 10, '0.25'));
  const result = Groups.validate(schemaOf('NPC Drops'),
                                 [{ rowNumber: 0, values, loaded: null }],
                                 { NPCs: new Set([1, 2]), Items: new Set([10]) });

  assert.equal(result.ok, false);
  assert.equal(result.rows[0].errors[0].column, 'npc_template_id');
});

test('rowKeyOf separates columns unambiguously', () => {
  // ['a b', 'c'] and ['a', 'b c'] must not produce one key, so the join cannot be a space.
  const schema = { sheet: 'Nothing', columns: [{ name: 'x' }, { name: 'y' }] };
  assert.notEqual(Groups.rowKeyOf(schema, { x: 'a b', y: 'c' }),
                  Groups.rowKeyOf(schema, { x: 'a', y: 'b c' }));
});

// ------------------------------------------------------------------ large groups

test('render draws at most the first 100 rows', () => {
  // NPC Spawns is 4,322 rows grouped by map, and how many land on the busiest map is not
  // knowable from the repo. Several hundred FK typeaheads in one panel is worth not finding out
  // the hard way; the rest are one click away.
  const rows = [];
  for (let i = 0; i < Groups.RENDER_CAP + 50; i++) rows.push(DROP(1, i + 1));
  const { container } = panelFor('NPC Drops', rows, NPCS);
  assert.equal(container.querySelectorAll('[data-group-row]').length, Groups.RENDER_CAP);
  assert.equal(container.querySelectorAll('[data-show-all]').length, 1);
});

test('show all draws the rest', () => {
  const rows = [];
  for (let i = 0; i < Groups.RENDER_CAP + 50; i++) rows.push(DROP(1, i + 1));
  const { container } = panelFor('NPC Drops', rows, NPCS);
  fire(container.querySelectorAll('[data-show-all]')[0], 'click');
  assert.equal(container.querySelectorAll('[data-group-row]').length, Groups.RENDER_CAP + 50);
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
  for (let i = 0; i < Groups.RENDER_CAP + 50; i++) rows.push(DROP(1, i + 1));
  const { container, schema } = panelFor('NPC Drops', rows, NPCS);
  // The count is the assertion that bites: ops() derives deletes from removed() alone, so a
  // collect that dropped the undrawn rows would still produce no deletes here and look fine.
  assert.equal(Groups.collect(container, schema).length, Groups.RENDER_CAP + 50);
  const ops = Groups.ops(schema, Groups.collect(container, schema),
                         Groups.removed(container), {});
  assert.deepEqual(ops.deletes, [], 'undrawn rows must not be posted as deletions');
  assert.deepEqual(ops.writes, [], 'undrawn rows are unchanged and must produce no write');
});

test('an edit to a drawn row writes that row alone, and the undrawn rows ride along', () => {
  // The other half of the cap's contract: a real save from a capped group posts exactly the one
  // record the user touched. The 50 undrawn rows must be present in collect() — otherwise they
  // are records the save simply forgot — and must still produce nothing.
  const rows = [];
  for (let i = 0; i < Groups.RENDER_CAP + 50; i++) rows.push(DROP(1, i + 1));
  const { container, schema } = panelFor('NPC Drops', rows, NPCS);
  container.querySelectorAll('[name=droprate]')[7].value = '0.99';

  const present = Groups.collect(container, schema);
  assert.equal(present.length, Groups.RENDER_CAP + 50, 'the undrawn rows must survive collect()');

  const ops = Groups.ops(schema, present, Groups.removed(container), {});
  assert.deepEqual(ops.deletes, []);
  assert.equal(ops.writes.length, 1, 'only the edited row is written');
  assert.equal(ops.writes[0].row, 9);          // rows[7] is sheet row 9
  const at = schemaOf('NPC Drops').columns.map((c) => c.name).indexOf('droprate');
  assert.equal(ops.writes[0].cells[at], '0.99');
});

test('the header counts the whole group, not the drawn part of it', () => {
  // The group HAS 150 rows; 100 of them being on screen is what the show-all button says.
  const rows = [];
  for (let i = 0; i < Groups.RENDER_CAP + 50; i++) rows.push(DROP(1, i + 1));
  const { container } = panelFor('NPC Drops', rows, NPCS);
  assert.equal(container.querySelectorAll('.count')[0].textContent,
               (Groups.RENDER_CAP + 50) + ' rows');
});

// ------------------------------------------------------------------ duplicates

test('duplicate rows are marked on screen', () => {
  const { container, schema } = panelFor('NPC Drops',
                                         [DROP(1, 10, '0.25'), DROP(1, 10, '0.25')], NPCS);
  Groups.markDuplicates(container, schema);
  assert.equal(container.querySelectorAll('.duplicate').length, 2);
});

test('marking duplicates twice does not leave stale marks', () => {
  const { container, schema } = panelFor('NPC Drops',
                                         [DROP(1, 10, '0.25'), DROP(1, 10, '0.25')], NPCS);
  Groups.markDuplicates(container, schema);
  // The key is npc + item, so the rows only stop being duplicates when the ITEM differs —
  // editing the droprate would leave them duplicates under the ratified key.
  container.querySelectorAll('[name=item_template_id]')[1].value = '20';
  Groups.markDuplicates(container, schema);
  assert.equal(container.querySelectorAll('.duplicate').length, 0);
});

test('markDuplicates counts duplicates the cap has not drawn, and marks nothing for them', () => {
  // The count feeds the status line, which is the only way the user learns they exist at all.
  const rows = [];
  for (let i = 0; i < Groups.RENDER_CAP + 50; i++) rows.push(DROP(1, i + 1));
  rows[rows.length - 1] = DROP(1, rows.length - 1);   // a copy of the second-to-last, both undrawn
  const { container, schema } = panelFor('NPC Drops', rows, NPCS);
  assert.equal(Groups.markDuplicates(container, schema), 2);
  assert.equal(container.querySelectorAll('.duplicate').length, 0,
               'an undrawn row has no element to mark');
});

test('a drawn row duplicating an undrawn one is counted twice and marked once', () => {
  const rows = [];
  for (let i = 0; i < Groups.RENDER_CAP + 50; i++) rows.push(DROP(1, i + 1));
  rows[rows.length - 1] = DROP(1, 1);                 // the same record as the FIRST, drawn row
  const { container, schema } = panelFor('NPC Drops', rows, NPCS);
  assert.equal(Groups.markDuplicates(container, schema), 2);
  assert.equal(container.querySelectorAll('.duplicate').length, 1);
});

test('show all marks the duplicates it draws, on a panel already marked', () => {
  // Otherwise the note says "1 of them is not on screen yet" and the click that puts it on screen
  // leaves it untinted — the one thing the note promised.
  const rows = [];
  for (let i = 0; i < Groups.RENDER_CAP + 50; i++) rows.push(DROP(1, i + 1));
  rows[rows.length - 1] = DROP(1, 1);
  const { container, schema } = panelFor('NPC Drops', rows, NPCS);
  Groups.markDuplicates(container, schema);
  fire(container.querySelectorAll('[data-show-all]')[0], 'click');
  assert.equal(container.querySelectorAll('.duplicate').length, 2);
});

test('marking a row keeps the classes it already had', () => {
  const { container, schema } = panelFor('NPC Drops',
                                         [DROP(1, 10, '0.25'), DROP(1, 10, '0.25')], NPCS);
  Groups.markDuplicates(container, schema);
  const row = container.querySelectorAll('[data-group-row]')[0];
  assert.match(row.getAttribute('class'), /\bgroup-row\b/);
  assert.match(row.getAttribute('class'), /\bduplicate\b/);
});

test('drawnRows answers this panel rows only, in collect order', () => {
  const a = panelFor('NPC Drops', [DROP(1, 10), DROP(1, 20)], NPCS);
  const b = panelFor('NPC Drops', [DROP(2, 30)], NPCS);
  const both = createElement('div');
  both.appendChild(a.container);
  both.appendChild(b.container);

  assert.equal(Groups.drawnRows(a.container).length, 2);
  assert.equal(Groups.drawnRows(b.container).length, 1);
  assert.deepEqual(Groups.drawnRows(a.container).map((r) => r.__rowNumber),
                   Groups.collect(a.container, a.schema).map((r) => r.rowNumber));
});

test('drawnRows answers empty for a container that holds no panel', () => {
  assert.deepEqual(Groups.drawnRows(createElement('div')), []);
});

// ------------------------------------------------------------------ changeCount

test('an untouched panel holds zero changes', () => {
  const { container, schema } = panelFor('NPC Drops', [DROP(1, 10)], NPCS);
  assert.equal(Groups.changeCount(container, schema), 0);
});

test('an edited cell counts its row as one change', () => {
  const { container, schema } = panelFor('NPC Drops', [DROP(1, 10), DROP(1, 20)], NPCS);
  container.querySelectorAll('[name=droprate]')[0].value = '0.99';
  assert.equal(Groups.changeCount(container, schema), 1);
});

test('an added row counts as a change', () => {
  const { container, schema } = panelFor('NPC Drops', [DROP(1, 10)], NPCS);
  Groups.addRow(container);
  assert.equal(Groups.changeCount(container, schema), 1);
});

test('a removed row counts as a change', () => {
  const { container, schema } = panelFor('NPC Drops', [DROP(1, 10), DROP(1, 20)], NPCS);
  fire(container.querySelectorAll('[data-remove]')[0], 'click');
  assert.equal(Groups.changeCount(container, schema), 1);
});

test('undrawn rows past the render cap count as nothing', () => {
  // collect() hands them back with values === loaded, so the comparison finds no difference —
  // which is exactly right: the user never saw them, so they cannot have edited them, and a
  // capped panel must not ask "discard changes?" the moment it opens.
  const rows = [];
  for (let i = 0; i < 150; i++) rows.push(DROP(1, i + 1));
  const { container, schema } = panelFor('NPC Drops', rows, NPCS);
  assert.equal(Groups.changeCount(container, schema), 0);
});
