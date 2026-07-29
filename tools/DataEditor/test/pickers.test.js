import { test } from 'node:test';
import assert from 'node:assert/strict';
import { installFakeDom, fire } from './fake-dom.js';

installFakeDom();

const { Forms } = await import('../src/forms.js');
globalThis.Forms = Forms;
const { Sprites } = await import('../src/sprites.js');
globalThis.Sprites = Sprites;

const { Pickers } = await import('../src/pickers.js');

const entries = [
  { id: '1', name: 'Gold' },
  { id: '42', name: 'Iron Sword' },
  { id: '43', name: 'Iron Shield' },
  { id: '100', name: 'Steel Sword' },
];

// --- search ---------------------------------------------------------------------------------

test('matches on id prefix', () => {
  assert.deepEqual(Pickers.search(entries, '4').map((e) => e.id), ['42', '43']);
});

test('matches on name substring, case-insensitively', () => {
  assert.deepEqual(Pickers.search(entries, 'sword').map((e) => e.id), ['42', '100']);
  assert.deepEqual(Pickers.search(entries, 'SWORD').map((e) => e.id), ['42', '100']);
});

test('an id prefix is a PREFIX, not a substring', () => {
  // '14' contains '4' but is not a 4-something. Listing it would bury the real hits.
  assert.deepEqual(Pickers.search([{ id: '14' }, { id: '40' }], '4').map((e) => e.id), ['40']);
});

test('LIMIT is 50', () => {
  // Pinned rather than derived: every other test compares against Pickers.LIMIT, so without
  // this the constant could drift to anything and the suite would follow it.
  assert.equal(Pickers.LIMIT, 50);
});

test('exact id match sorts first', () => {
  assert.equal(Pickers.search(entries, '1')[0].id, '1');
  // Above the row order, not merely with it: 12 comes first in the sheet and 1 must still win.
  assert.deepEqual(Pickers.search([{ id: '12' }, { id: '1' }], '1').map((e) => e.id),
    ['1', '12']);
});

test('empty query returns the head of the list, capped', () => {
  const many = Array.from({ length: 500 }, (_, i) => ({ id: String(i), name: 'x' + i }));
  assert.equal(Pickers.search(many, '').length, Pickers.LIMIT);
});

test('no match returns empty', () => {
  assert.deepEqual(Pickers.search(entries, 'zzz'), []);
});

test('a whitespace-only query is the empty query', () => {
  assert.equal(Pickers.search(entries, '  ').length, entries.length);
  assert.equal(Pickers.search(entries, null).length, entries.length);
  assert.equal(Pickers.search(entries, undefined).length, entries.length);
});

test('the query is trimmed before matching', () => {
  assert.deepEqual(Pickers.search(entries, ' sword ').map((e) => e.id), ['42', '100']);
  assert.deepEqual(Pickers.search(entries, ' 42 ').map((e) => e.id), ['42']);
});

test('an entry is listed once even when its id and its name both match', () => {
  // '1' is an id prefix of 100 AND a substring of its name; one row, in the id bucket.
  const hits = Pickers.search([{ id: '100', name: 'Potion 1' }], '1');
  assert.deepEqual(hits.map((e) => e.id), ['100']);
});

test('exact id beats a longer id that also starts with the query', () => {
  const hits = Pickers.search(entries, '1').map((e) => e.id);
  assert.deepEqual(hits, ['1', '100']);
});

test('numeric ids are compared canonically, so leading zeros still resolve', () => {
  // Validation.validateCell compares Number(value) against the id set, so '042' IS 42 there.
  // A picker that called it "not found" would disagree with the validator on the same cell.
  assert.deepEqual(Pickers.search(entries, '042').map((e) => e.id), ['42']);
  assert.deepEqual(Pickers.search([{ id: 42, name: 'n' }], '42').map((e) => e.id), [42]);
  assert.deepEqual(Pickers.search([{ id: ' 42 ', name: 'n' }], '42').map((e) => e.id), [' 42 ']);
});

test('an entry with no name is still searchable by id', () => {
  const hits = Pickers.search([{ id: '7' }, { id: '8', name: null }], '7');
  assert.deepEqual(hits.map((e) => e.id), ['7']);
});

test('name hits are never crowded out entirely by a flood of id-prefix hits', () => {
  // A designer typing "1" against Items (649 rows) has >100 id-prefix hits — more than LIMIT.
  // Slicing after concatenation would show them nothing but ids and hide every name match.
  const many = [];
  for (let i = 100; i < 400; i++) many.push({ id: String(i), name: 'thing' });
  many.push({ id: '900', name: 'Sword mk1' });
  many.push({ id: '901', name: 'Dagger mk1' });

  const hits = Pickers.search(many, '1');
  assert.equal(hits.length, Pickers.LIMIT);
  assert.deepEqual(hits.slice(-2).map((e) => e.id), ['900', '901'],
    'the two name hits must survive the cap');
});

test('id-prefix hits keep the whole budget when there are no name hits', () => {
  const many = [];
  for (let i = 100; i < 400; i++) many.push({ id: String(i), name: 'thing' });
  assert.equal(Pickers.search(many, '1').length, Pickers.LIMIT);
});

test('name hits are capped at the total limit when there are no id hits', () => {
  const many = [];
  for (let i = 0; i < 200; i++) many.push({ id: 'x' + i, name: 'Sword ' + i });
  assert.equal(Pickers.search(many, 'sword').length, Pickers.LIMIT);
});

test('the reserve never lets the result exceed LIMIT', () => {
  const many = [];
  for (let i = 100; i < 400; i++) many.push({ id: String(i), name: 'sword mk1' });
  const hits = Pickers.search(many, '1');
  assert.equal(hits.length, Pickers.LIMIT);
  // 100-199 are id-prefix hits, 200-399 are name hits: 40 ids and the 10 reserved names.
  assert.deepEqual([hits[39].id, hits[40].id], ['139', '200']);
});

test('the exact hit is charged against the budget, not added on top of it', () => {
  const many = [{ id: '1', name: 'one' }];
  for (let i = 100; i < 145; i++) many.push({ id: String(i), name: 'thing' });
  for (let i = 200; i < 220; i++) many.push({ id: String(i), name: 'mk1' });

  const hits = Pickers.search(many, '1').map((e) => e.id);
  assert.equal(hits.length, Pickers.LIMIT);
  // 1 exact + 39 id-prefix (49 of budget less the 10 reserved) + 10 reserved name hits.
  assert.deepEqual([hits[0], hits[1], hits[39], hits[40], hits[49]],
    ['1', '100', '138', '200', '209']);
});

test('search does not mutate or alias the entry list', () => {
  const list = entries.slice();
  const out = Pickers.search(list, '');
  assert.notEqual(out, list);
  out.length = 0;
  assert.equal(list.length, 4);
});

// --- fkControl ------------------------------------------------------------------------------

const refColumn = { name: 'item_template_id', kind: 'Id', sql: 'INTEGER', ref: 'Items', required: true };
const optionalColumn = {
  name: 'spell_effect_id', kind: 'Id', sql: 'INTEGER', ref: 'Spell Effects',
  required: false, default: '0',
};

function ctxWith(data) {
  return { pickerData: data, bundles: {}, images: {}, onImagesReady() {} };
}

function parts(wrap) {
  return {
    input: wrap.querySelector('[name]'),
    label: wrap.querySelector('[class="resolved"]') || wrap.querySelector('[class="resolved bad"]'),
    list: wrap.querySelector('[class="results"]'),
  };
}

test('fkControl shows the input, the resolved name and a hidden result list', () => {
  const wrap = Pickers.fkControl(refColumn, '42', ctxWith({ Items: entries }));
  const { input, label, list } = parts(wrap);

  assert.equal(input.value, '42');
  assert.equal(input.getAttribute('name'), 'item_template_id');
  assert.equal(label.textContent, 'Iron Sword');
  assert.equal(label.className, 'resolved');
  assert.equal(list.hidden, true);
});

test('the fkControl input carries the id and attributes every other control has', () => {
  // 'f-' + name is what Forms.render's <label for> points at, and type=text (not number) is
  // forms.js:78-81 — a number input reads back '' for a typo, which is indistinguishable from
  // "blank, use the SQL default".
  const wrap = Pickers.fkControl(refColumn, '42', ctxWith({ Items: entries }));
  const { input } = parts(wrap);

  assert.equal(input.getAttribute('id'), 'f-item_template_id');
  assert.equal(input.getAttribute('type'), 'text');
  // The browser's own autofill dropdown would sit on top of the results list.
  assert.equal(input.getAttribute('autocomplete'), 'off');
});

test('fkControl keeps a stored 0 rather than blanking it', () => {
  // The falsy-zero bug: `value || ''` writes '' over a real 0, and blank means "use the SQL
  // default" on the next save.
  const wrap = Pickers.fkControl(optionalColumn, 0, ctxWith({}));
  assert.equal(parts(wrap).input.value, '0');
});

test('fkControl treats blank and 0 as none, exactly as Validation does', () => {
  ['', '0', ' 0 ', null, undefined].forEach((value) => {
    const wrap = Pickers.fkControl(optionalColumn, value, ctxWith({ 'Spell Effects': entries }));
    assert.equal(parts(wrap).label.textContent, 'none', JSON.stringify(value));
    assert.equal(parts(wrap).label.className, 'resolved');
  });
});

test('fkControl flags 00 as not found, because Validation does too', () => {
  // Validation.validateCell only exempts the literal '0', so '00' is looked up and reported.
  const wrap = Pickers.fkControl(optionalColumn, '00', ctxWith({ 'Spell Effects': entries }));
  assert.equal(parts(wrap).label.textContent, 'not found in Spell Effects');
});

test('fkControl resolves leading zeros the way Validation does', () => {
  const wrap = Pickers.fkControl(refColumn, '042', ctxWith({ Items: entries }));
  assert.equal(parts(wrap).label.textContent, 'Iron Sword');
});

test('fkControl marks an unresolved id bad', () => {
  const wrap = Pickers.fkControl(refColumn, '999', ctxWith({ Items: entries }));
  const { label } = parts(wrap);
  assert.equal(label.textContent, 'not found in Items');
  assert.equal(label.className, 'resolved bad');
});

test('fkControl says "loading" rather than "not found" before the sheet arrives', () => {
  const wrap = Pickers.fkControl(refColumn, '42', ctxWith({}));
  const { label } = parts(wrap);
  assert.equal(label.textContent, 'loading Items…');
  assert.notEqual(label.className, 'resolved bad');
});

test('fkControl reads pickerData at use time, not at construction', () => {
  // App.loadReferencedSheets fills pickerData asynchronously. Capturing the array in the
  // closure leaves the picker permanently empty if the control is built first.
  const ctx = ctxWith({});
  const wrap = Pickers.fkControl(refColumn, '42', ctx);
  ctx.pickerData.Items = entries;

  const { input, label, list } = parts(wrap);
  fire(input, 'focus');
  assert.equal(list.hidden, false);
  assert.deepEqual(list.children.map((c) => c.textContent), ['42 — Iron Sword']);
  fire(input, 'input');
  assert.equal(label.textContent, 'Iron Sword');
});

test('fkControl survives a ctx with no picker data at all', () => {
  const wrap = Pickers.fkControl(refColumn, '42', {});
  assert.equal(parts(wrap).label.textContent, 'loading Items…');
});

test('fkControl lists results on focus, before any keystroke', () => {
  const wrap = Pickers.fkControl(refColumn, '', ctxWith({ Items: entries }));
  const { input, list } = parts(wrap);
  assert.equal(list.hidden, true);

  fire(input, 'focus');
  assert.equal(list.hidden, false);
  assert.deepEqual(list.children.map((c) => c.textContent),
    ['1 — Gold', '42 — Iron Sword', '43 — Iron Shield', '100 — Steel Sword']);
});

test('fkControl filters as you type', () => {
  const wrap = Pickers.fkControl(refColumn, '', ctxWith({ Items: entries }));
  const { input, list } = parts(wrap);

  input.value = 'iron';
  fire(input, 'input');
  assert.deepEqual(list.children.map((c) => c.getAttribute('data-id')), ['42', '43']);
});

test('fkControl hides the list when nothing matches', () => {
  const wrap = Pickers.fkControl(refColumn, '', ctxWith({ Items: entries }));
  const { input, list } = parts(wrap);

  fire(input, 'focus');
  input.value = 'zzz';
  fire(input, 'input');
  assert.equal(list.hidden, true);
  assert.equal(list.children.length, 0);
});

test('fkControl updates the resolved name as you type', () => {
  const wrap = Pickers.fkControl(refColumn, '', ctxWith({ Items: entries }));
  const { input, label } = parts(wrap);

  input.value = '43';
  fire(input, 'input');
  assert.equal(label.textContent, 'Iron Shield');
});

test('clicking a result writes the id, resolves it and closes the list', () => {
  const wrap = Pickers.fkControl(refColumn, '', ctxWith({ Items: entries }));
  const { input, label, list } = parts(wrap);

  fire(input, 'focus');
  fire(list.children[2], 'click');

  assert.equal(input.value, '43');
  assert.equal(label.textContent, 'Iron Shield');
  assert.equal(list.hidden, true);
});

test('clicking a result writes the canonical id, not the raw cell text', () => {
  const messy = ctxWith({ Items: [{ id: ' 042 ', name: 'Iron Sword' }] });
  const wrap = Pickers.fkControl(refColumn, '', messy);
  const { input, list } = parts(wrap);

  fire(input, 'focus');
  assert.equal(list.children[0].textContent, '42 — Iron Sword');
  fire(list.children[0], 'click');
  assert.equal(input.value, '42');
});

test('rebuilding the list detaches the old rows entirely', () => {
  // The rows are rebuilt on every keystroke. Each one closes over the input and its own entry,
  // so a row left hanging in the tree would be a stale closure the user could still click.
  const wrap = Pickers.fkControl(refColumn, '', ctxWith({ Items: entries }));
  const { input, list } = parts(wrap);

  fire(input, 'focus');
  const stale = list.children[0];
  input.value = 'iron';
  fire(input, 'input');

  assert.equal(list.children.indexOf(stale), -1);
  assert.equal(stale.parentNode, null, 'the old row must be unreachable from the tree');
  assert.deepEqual(list.children.map((c) => c.getAttribute('data-id')), ['42', '43']);
});

test('mousedown inside the list is cancelled so the click is never lost to blur', () => {
  // The plan hid the list on a 150ms timer and hoped the click landed first. Cancelling
  // mousedown keeps focus on the input, so blur does not fire at all — no timer, no race.
  const wrap = Pickers.fkControl(refColumn, '', ctxWith({ Items: entries }));
  const { input, list } = parts(wrap);

  fire(input, 'focus');
  const notCancelled = fire(list.children[0], 'mousedown');
  assert.equal(notCancelled, false, 'mousedown default must be prevented');
});

test('blur hides the list immediately', () => {
  const wrap = Pickers.fkControl(refColumn, '', ctxWith({ Items: entries }));
  const { input, list } = parts(wrap);

  fire(input, 'focus');
  assert.equal(list.hidden, false);
  fire(input, 'blur');
  assert.equal(list.hidden, true);
});

test('fkControl shows the SQL default as its placeholder, like every other control', () => {
  const wrap = Pickers.fkControl(optionalColumn, '', ctxWith({}));
  assert.equal(parts(wrap).input.getAttribute('placeholder'), 'default 0');
  const required = Pickers.fkControl(refColumn, '', ctxWith({}));
  assert.equal(parts(required).input.getAttribute('placeholder'), 'required');
});

test('fkControl labels an entry with a blank name rather than showing nothing', () => {
  const wrap = Pickers.fkControl(refColumn, '5', ctxWith({ Items: [{ id: '5', name: '' }] }));
  assert.equal(parts(wrap).label.textContent, '(unnamed)');
});

test('the fkControl input is the only named node, so Forms.collect sees one value', () => {
  const wrap = Pickers.fkControl(refColumn, '42', ctxWith({ Items: entries }));
  assert.equal(wrap.querySelectorAll('[name]').length, 1);
});

// --- graphicControl -------------------------------------------------------------------------

const graphicColumn = { name: 'graphic_tile', kind: 'Id', sql: 'INTEGER', required: false, default: '0' };
const fileColumn = { name: 'graphic_file', kind: 'Id', sql: 'INTEGER', required: false, default: '0' };

const bundles = {
  // A non-square rect: with a 32x32 sprite on a 48x48 canvas the two centring
  // offsets are both 8, and swapping width for height would go unnoticed.
  icons: { rects: { '20107:810003': [96, 0, 32, 16] } },
};

function gctx(values) {
  const calls = [];
  return {
    bundles,
    images: { icons: 'ICONS' },
    onImagesReady(fn) { calls.push(fn); },
    __ready: calls,
    ...values,
  };
}

function gparts(wrap) {
  return {
    canvas: wrap.querySelector('[class="preview"]'),
    graphic: wrap.querySelector('[name="graphic_tile"]'),
    file: wrap.querySelector('[name="graphic_file"]'),
    status: wrap.querySelectorAll('[class]').filter((n) => /^status/.test(n.className))[0],
  };
}

test('graphicControl renders both cells, named for Forms.collect', () => {
  const wrap = Pickers.graphicControl(graphicColumn, fileColumn,
    { graphic_tile: '810003', graphic_file: '20107' }, gctx());
  const { graphic, file } = gparts(wrap);

  assert.equal(graphic.value, '810003');
  assert.equal(file.value, '20107');
  assert.equal(wrap.querySelectorAll('[name]').length, 2);
});

test('graphicControl keeps a stored 0 rather than blanking it', () => {
  const wrap = Pickers.graphicControl(graphicColumn, fileColumn,
    { graphic_tile: 0, graphic_file: 0 }, gctx());
  assert.equal(gparts(wrap).graphic.value, '0');
  assert.equal(gparts(wrap).file.value, '0');
});

test('graphicControl draws the resolved icon centred on the canvas', () => {
  const wrap = Pickers.graphicControl(graphicColumn, fileColumn,
    { graphic_tile: '810003', graphic_file: '20107' }, gctx());
  const ctx2d = gparts(wrap).canvas.getContext('2d');

  assert.deepEqual(ctx2d.calls, [
    ['clearRect', 0, 0, 48, 48],
    ['drawImage', 'ICONS', 96, 0, 32, 16, 8, 16, 32, 16],
  ]);
});

test('graphicControl passes sheet and graphic in the right order', () => {
  // The Graphic composite declares [graphic, file] (schema.js: graphic_tile+graphic_file) and
  // Sprites.icon takes (bundles, sheet, graphic). Swapping them resolves nothing, silently.
  const wrap = Pickers.graphicControl(graphicColumn, fileColumn,
    { graphic_tile: '20107', graphic_file: '810003' }, gctx());
  const drew = gparts(wrap).canvas.getContext('2d').calls
    .filter((c) => c[0] === 'drawImage');
  assert.deepEqual(drew, [], 'the reversed pair must not resolve');
});

test('graphicControl redraws when the graphic alone is edited', () => {
  const wrap = Pickers.graphicControl(graphicColumn, fileColumn,
    { graphic_tile: '810003', graphic_file: '20107' }, gctx());
  const { graphic, status } = gparts(wrap);

  graphic.value = '999';
  fire(graphic, 'input');
  assert.equal(status.textContent, 'no art for sheet 20107 graphic 999');
});

test('graphicControl redraws when the sheet alone is edited', () => {
  const wrap = Pickers.graphicControl(graphicColumn, fileColumn,
    { graphic_tile: '810003', graphic_file: '20107' }, gctx());
  const { file, status } = gparts(wrap);

  file.value = '999';
  fire(file, 'input');
  assert.equal(status.textContent, 'no art for sheet 999 graphic 810003');
});

test('graphicControl redraws as either field is edited', () => {
  const wrap = Pickers.graphicControl(graphicColumn, fileColumn, {}, gctx());
  const { canvas, graphic, file } = gparts(wrap);
  const ctx2d = canvas.getContext('2d');

  graphic.value = '810003';
  fire(graphic, 'input');
  assert.deepEqual(ctx2d.calls.filter((c) => c[0] === 'drawImage'), []);

  file.value = '20107';
  fire(file, 'input');
  assert.deepEqual(ctx2d.calls.filter((c) => c[0] === 'drawImage'),
    [['drawImage', 'ICONS', 96, 0, 32, 16, 8, 16, 32, 16]]);
});

test('graphicControl coerces exactly once, leaving the rule to Sprites', () => {
  // Sprites.icon coerces with parseInt (matching Equipped and Appearance). A Number() here as
  // well is a SECOND rule for what a cell means, and the two disagree: Number('20107abc') is
  // NaN and finds nothing, parseInt is 20107 and finds the icon. The preview shows what the
  // lookup key actually resolves; the status line is what says the cell is still a typo.
  const wrap = Pickers.graphicControl(graphicColumn, fileColumn,
    { graphic_tile: '0810003', graphic_file: '20107abc' }, gctx());
  assert.deepEqual(gparts(wrap).canvas.getContext('2d').calls.filter((c) => c[0] === 'drawImage'),
    [['drawImage', 'ICONS', 96, 0, 32, 16, 8, 16, 32, 16]]);
  assert.equal(gparts(wrap).status.textContent, 'graphic and sheet must be whole numbers');
});

test('graphicControl accepts a leading-zero pair, as Sprites and Validation both do', () => {
  const wrap = Pickers.graphicControl(graphicColumn, fileColumn,
    { graphic_tile: '0810003', graphic_file: '020107' }, gctx());
  assert.deepEqual(gparts(wrap).canvas.getContext('2d').calls.filter((c) => c[0] === 'drawImage'),
    [['drawImage', 'ICONS', 96, 0, 32, 16, 8, 16, 32, 16]]);
  assert.equal(gparts(wrap).status.textContent, '');
});

test('graphicControl reports both cells blank as "no graphic", not as an error', () => {
  const wrap = Pickers.graphicControl(graphicColumn, fileColumn,
    { graphic_tile: '', graphic_file: '' }, gctx());
  assert.equal(gparts(wrap).status.textContent, 'no graphic');
  assert.equal(gparts(wrap).status.className, 'status');

  const zeros = Pickers.graphicControl(graphicColumn, fileColumn,
    { graphic_tile: '0', graphic_file: '0' }, gctx());
  assert.equal(gparts(zeros).status.textContent, 'no graphic');
});

test('graphicControl reports a non-numeric graphic id rather than silently showing nothing', () => {
  // Prerequisite #13: neither Equipped.format nor isFaithful catches a UI typo, so the field
  // that accepts it has to say so.
  const wrap = Pickers.graphicControl(graphicColumn, fileColumn,
    { graphic_tile: '81o003', graphic_file: '20107' }, gctx());
  assert.equal(gparts(wrap).status.textContent, 'graphic and sheet must be whole numbers');
  assert.equal(gparts(wrap).status.className, 'status bad');
});

test('graphicControl reports half a pair as incomplete', () => {
  const wrap = Pickers.graphicControl(graphicColumn, fileColumn,
    { graphic_tile: '810003', graphic_file: '' }, gctx());
  assert.equal(gparts(wrap).status.textContent, 'graphic and sheet must both be set');
  assert.equal(gparts(wrap).status.className, 'status bad');
});

test('graphicControl reports a pair with no art in the bundle', () => {
  const wrap = Pickers.graphicControl(graphicColumn, fileColumn,
    { graphic_tile: '9', graphic_file: '8' }, gctx());
  assert.equal(gparts(wrap).status.textContent, 'no art for sheet 8 graphic 9');
  assert.equal(gparts(wrap).status.className, 'status bad');
});

test('graphicControl clears the status once the pair resolves', () => {
  const wrap = Pickers.graphicControl(graphicColumn, fileColumn,
    { graphic_tile: '810003', graphic_file: '20107' }, gctx());
  assert.equal(gparts(wrap).status.textContent, '');
  assert.equal(gparts(wrap).status.className, 'status');
});

test('graphicControl redraws when the bundle image finishes decoding', () => {
  const ctx = gctx();
  ctx.images = {};
  const wrap = Pickers.graphicControl(graphicColumn, fileColumn,
    { graphic_tile: '810003', graphic_file: '20107' }, ctx);
  const ctx2d = gparts(wrap).canvas.getContext('2d');

  assert.deepEqual(ctx2d.calls.filter((c) => c[0] === 'drawImage'), [],
    'Sprites.draw must skip a layer whose PNG has not decoded');
  assert.equal(ctx.__ready.length, 1);

  ctx.images.icons = 'ICONS';
  ctx.__ready[0]();
  assert.deepEqual(ctx2d.calls.filter((c) => c[0] === 'drawImage'),
    [['drawImage', 'ICONS', 96, 0, 32, 16, 8, 16, 32, 16]]);
});

test('graphicControl survives a ctx with no bundles, images or ready hook', () => {
  const wrap = Pickers.graphicControl(graphicColumn, fileColumn,
    { graphic_tile: '810003', graphic_file: '20107' }, {});
  assert.equal(gparts(wrap).status.textContent, 'no art for sheet 20107 graphic 810003');
});

test('graphicControl shows each column default as its placeholder', () => {
  const wrap = Pickers.graphicControl(graphicColumn, fileColumn, {}, gctx());
  assert.equal(gparts(wrap).graphic.getAttribute('placeholder'), 'default 0');
  assert.equal(gparts(wrap).file.getAttribute('placeholder'), 'default 0');
});

test('a column with no default still gets a useful placeholder', () => {
  const bare = { name: 'graphic_tile', kind: 'Id', sql: 'INTEGER', required: false };
  const bareFile = { name: 'graphic_file', kind: 'Id', sql: 'INTEGER', required: false };
  const wrap = Pickers.graphicControl(bare, bareFile, {}, gctx());
  assert.equal(gparts(wrap).graphic.getAttribute('placeholder'), 'graphic');
  assert.equal(gparts(wrap).file.getAttribute('placeholder'), 'sheet');
});

test('the graphic cells carry the same id and attributes as every other control', () => {
  const wrap = Pickers.graphicControl(graphicColumn, fileColumn, {}, gctx());
  const { graphic, file } = gparts(wrap);

  assert.equal(graphic.getAttribute('id'), 'f-graphic_tile');
  assert.equal(file.getAttribute('id'), 'f-graphic_file');
  assert.equal(graphic.getAttribute('type'), 'text');
  assert.equal(graphic.getAttribute('autocomplete'), 'off');
});

test('a padded cell is not mistaken for a typo', () => {
  // Sheets hands back whatever was typed. Sprites.icon's parseInt tolerates the padding, so
  // the status line has to as well or it contradicts the preview sitting next to it.
  const wrap = Pickers.graphicControl(graphicColumn, fileColumn,
    { graphic_tile: ' 810003 ', graphic_file: ' 20107 ' }, gctx());
  assert.equal(gparts(wrap).status.textContent, '');
  assert.deepEqual(gparts(wrap).canvas.getContext('2d').calls.filter((c) => c[0] === 'drawImage'),
    [['drawImage', 'ICONS', 96, 0, 32, 16, 8, 16, 32, 16]]);
});
