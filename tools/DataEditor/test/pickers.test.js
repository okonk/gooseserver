import { test } from 'node:test';
import assert from 'node:assert/strict';
import { installFakeDom, fire } from './fake-dom.js';

installFakeDom();

const { Forms } = await import('../src/forms.js');
globalThis.Forms = Forms;
const { Sprites } = await import('../src/sprites.js');
globalThis.Sprites = Sprites;
// partControl reads the client's item_slot -> folder map out of Appearance and the armed/unarmed
// rule out of Preview, rather than keeping second copies of either.
const { Equipped } = await import('../src/equipped.js');
globalThis.Equipped = Equipped;
const { Appearance } = await import('../src/appearance.js');
globalThis.Appearance = Appearance;
const { Preview } = await import('../src/preview.js');
globalThis.Preview = Preview;

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
  // 100-199 are id-prefix hits, 200-399 are name hits: 25 ids and the 25 reserved names.
  assert.deepEqual([hits[24].id, hits[25].id], ['124', '200']);
});

test('the exact hit is charged against the budget, not added on top of it', () => {
  const many = [{ id: '1', name: 'one' }];
  for (let i = 100; i < 145; i++) many.push({ id: String(i), name: 'thing' });
  for (let i = 200; i < 220; i++) many.push({ id: String(i), name: 'mk1' });

  const hits = Pickers.search(many, '1').map((e) => e.id);
  assert.equal(hits.length, Pickers.LIMIT);
  // 1 exact + 29 id-prefix (49 of budget less the 20 name hits held back) + those 20.
  assert.deepEqual([hits[0], hits[1], hits[29], hits[30], hits[49]],
    ['1', '100', '128', '200', '219']);
});

test('search does not mutate or alias the entry list', () => {
  const list = entries.slice();
  const out = Pickers.search(list, '');
  assert.notEqual(out, list);
  out.length = 0;
  assert.equal(list.length, 4);
});

// --- browse ---------------------------------------------------------------------------------
//
// What a filled-in picker shows before the user types. THE COMPLAINT search() produced: a field
// holding "42 — Iron Sword" searched on '42', so the list beside a selected value was that value
// (plus whatever it is a prefix of) and the other rows could not be reached without clearing the
// field first.

test('browse lists the whole head of the list with the current id hoisted to the front', () => {
  assert.deepEqual(Pickers.browse(entries, '43').map((e) => e.id), ['43', '1', '42', '100']);
  // The alternatives are the point: every row is offered, not just the one already chosen.
  assert.equal(Pickers.browse(entries, '43').length, entries.length);
});

test('browse keeps sheet order behind the hoisted row', () => {
  assert.deepEqual(Pickers.browse(entries, '1').map((e) => e.id), ['1', '42', '43', '100']);
});

test('browse with no id, or an unknown one, is just the head of the list', () => {
  ['', ' ', null, undefined, '999', 'nonsense'].forEach((id) => {
    assert.deepEqual(Pickers.browse(entries, id).map((e) => e.id), ['1', '42', '43', '100'],
      JSON.stringify(id));
  });
});

test('browse compares ids canonically, like search and Validation do', () => {
  assert.equal(Pickers.browse(entries, ' 042 ')[0].id, '42');
  assert.equal(Pickers.browse([{ id: 42 }, { id: 1 }], '42')[0].id, 42);
  assert.equal(Pickers.browse([{ id: ' 42 ' }, { id: '1' }], '42')[0].id, ' 42 ');
});

test('browse is capped at LIMIT, so it is the head of a big sheet and not a copy of it', () => {
  // Items is 1,595 rows. Everything past the cap is reached by typing, which is search()'s job.
  const many = Array.from({ length: 500 }, (_, i) => ({ id: String(i), name: 'x' + i }));
  assert.equal(Pickers.browse(many, '3').length, Pickers.LIMIT);
  // And the current row survives the cap even when it sits far past it — which is exactly why it
  // is hoisted rather than left in place.
  assert.equal(Pickers.browse(many, '480')[0].id, '480');
});

test('browse does not mutate or alias the entry list', () => {
  const list = entries.slice();
  const out = Pickers.browse(list, '42');
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

// `input` is the visible combobox the user types into; `cell` is the hidden input that carries
// the column and is what Forms.collect sweeps. They split when the display became "id — name":
// the display is not a value the sheet may ever receive.
function parts(wrap) {
  return {
    input: wrap.querySelector('[role="combobox"]'),
    cell: wrap.querySelector('[name]'),
    label: wrap.querySelector('[class="resolved"]') || wrap.querySelector('[class="resolved bad"]'),
    list: wrap.querySelector('[class="results"]'),
  };
}

test('fkControl shows "id — name" in the field, the cell in a named hidden input', () => {
  const wrap = Pickers.fkControl(refColumn, '42', ctxWith({ Items: entries }));
  const { input, cell, label, list } = parts(wrap);

  assert.equal(input.value, '42 — Iron Sword');
  assert.equal(input.getAttribute('name'), null,
    'the display text must never reach Forms.collect');
  assert.equal(cell.value, '42');
  assert.equal(cell.getAttribute('name'), 'item_template_id');
  // The name is IN the field, so a label repeating it underneath would say it twice.
  assert.equal(label.textContent, '');
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
  assert.equal(parts(wrap).cell.value, '0');
  assert.equal(parts(wrap).input.value, '0');
});

test('fkControl round-trips a padded stored id verbatim while displaying it canonically', () => {
  // Opening a record must not change it: the CELL keeps ' 042 ' until the user edits something,
  // even though the field reads back the canonical "42 — name".
  const wrap = Pickers.fkControl(refColumn, ' 042 ', ctxWith({ Items: entries }));
  assert.equal(parts(wrap).cell.value, ' 042 ');
  assert.equal(parts(wrap).input.value, '42 — Iron Sword');
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
  assert.equal(parts(wrap).input.value, '42 — Iron Sword');
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

test('fkControl says a list FAILED rather than waiting on it forever', () => {
  // "loading Items…" on a list that is never coming is a wait with no end — and it hides the
  // reason App is about to refuse the save. ctx.refErrors is the same array App mutates, so a
  // control built before the failure sees it too.
  const ctx = ctxWith({});
  ctx.refErrors = [];
  const wrap = Pickers.fkControl(refColumn, '42', ctx);
  const { input, label } = parts(wrap);
  assert.equal(label.textContent, 'loading Items…');

  ctx.refErrors.push('Items');
  fire(input, 'input');
  assert.equal(label.textContent, 'could not load Items');
  assert.equal(label.className, 'resolved bad');
});

test('a failed list does not make a resolvable id look broken', () => {
  // Only reached when the lookup found nothing. A sheet that failed on a RETRY, after a first
  // load succeeded, still has its entries — and an id in them still resolves.
  const ctx = ctxWith({ Items: entries });
  ctx.refErrors = ['Items'];
  assert.equal(parts(Pickers.fkControl(refColumn, '42', ctx)).input.value, '42 — Iron Sword');
  // …and one that is genuinely absent from a list we DO have is still "not found", not "could
  // not load".
  assert.equal(parts(Pickers.fkControl(refColumn, '999', ctx)).label.textContent,
               'not found in Items');
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
  // The whole list, with the field's own id first — the control has not been typed into, so it is
  // browsing (see the browse tests above). What this asserts is that the entries were read at
  // FOCUS time: a control that captured them at construction would still be showing nothing.
  assert.deepEqual(list.children.map((c) => c.textContent),
    ['42 — Iron Sword', '1 — Gold', '43 — Iron Shield', '100 — Steel Sword']);
  fire(input, 'input');
  assert.equal(label.textContent, 'Iron Sword');
});

// --- fkControl: keyboard --------------------------------------------------------------------

const DOWN = { key: 'ArrowDown' };
const UP = { key: 'ArrowUp' };

// Which row the arrow keys have made current. The class is what a sighted user sees, and
// aria-activedescendant is what a screen reader follows — a fix that set one and not the other
// would leave half the users it was for with no cursor at all.
function activeRow(wrap) {
  const { input, list } = parts(wrap);
  const marked = list.children.filter((row) => row.className === 'result active');
  assert.ok(marked.length <= 1, 'at most one row may be active');
  const pointer = input.getAttribute('aria-activedescendant');

  if (!marked.length) {
    assert.equal(pointer, null, 'aria-activedescendant must be cleared when no row is active');
    return null;
  }
  assert.equal(pointer, marked[0].getAttribute('id'), 'the two markers must name the same row');
  assert.equal(marked[0].getAttribute('aria-selected'), 'true');
  return marked[0];
}

test('the fkControl input and list carry the combobox ARIA a screen reader needs', () => {
  const wrap = Pickers.fkControl(refColumn, '', ctxWith({ Items: entries }));
  const { input, list } = parts(wrap);

  assert.equal(input.getAttribute('role'), 'combobox');
  // 'list', not 'both': typing filters the list and never completes the field's text.
  assert.equal(input.getAttribute('aria-autocomplete'), 'list');
  assert.equal(input.getAttribute('aria-controls'), list.getAttribute('id'));
  assert.equal(list.getAttribute('role'), 'listbox');
  // A closed list is announced as closed, not left unsaid.
  assert.equal(input.getAttribute('aria-expanded'), 'false');

  fire(input, 'focus');
  assert.equal(input.getAttribute('aria-expanded'), 'true');
  assert.deepEqual(list.children.map((row) => row.getAttribute('role')),
    ['option', 'option', 'option', 'option']);
  // Rows are <button>s, so without tabindex=-1 a Tab out of the input walks through every one of
  // them (up to LIMIT) before reaching the next field.
  assert.deepEqual(list.children.map((row) => row.getAttribute('tabindex')),
    ['-1', '-1', '-1', '-1']);
  // aria-activedescendant can only point at one row, so each needs an id of its own.
  assert.equal(new Set(list.children.map((row) => row.getAttribute('id'))).size, 4);
});

test('the list opens with no row active, so the first Enter cannot pick one by accident', () => {
  const wrap = Pickers.fkControl(refColumn, '', ctxWith({ Items: entries }));
  const { input } = parts(wrap);

  fire(input, 'focus');
  assert.equal(activeRow(wrap), null);
  assert.equal(fire(input, 'keydown', { key: 'Enter' }), true, 'Enter must not be swallowed');
  assert.equal(input.value, '');
});

test('ArrowDown and ArrowUp walk the list and wrap', () => {
  const wrap = Pickers.fkControl(refColumn, '', ctxWith({ Items: entries }));
  const { input } = parts(wrap);
  fire(input, 'focus');

  const walk = (init, times) => {
    for (let i = 0; i < times; i++) {
      // preventDefault, or the arrow ALSO jumps the caret to the end of the field and the next
      // typed character lands somewhere the user did not ask for.
      assert.equal(fire(input, 'keydown', init), false, 'the arrow default must be prevented');
    }
    return activeRow(wrap).getAttribute('data-id');
  };

  assert.equal(walk(DOWN, 1), '1');
  assert.equal(walk(DOWN, 2), '43');
  assert.equal(walk(DOWN, 2), '1', 'past the last row wraps to the first');
  assert.equal(walk(UP, 1), '100', 'before the first row wraps to the last');
});

test('ArrowUp with nothing active enters the list from the bottom', () => {
  const wrap = Pickers.fkControl(refColumn, '', ctxWith({ Items: entries }));
  const { input } = parts(wrap);

  fire(input, 'focus');
  fire(input, 'keydown', UP);
  assert.equal(activeRow(wrap).getAttribute('data-id'), '100');
});

test('the active row is scrolled into view', () => {
  // .results is a 200px box over as many as LIMIT rows, so arrowing past the fifth one walks out
  // of sight otherwise.
  const wrap = Pickers.fkControl(refColumn, '', ctxWith({ Items: entries }));
  const { input, list } = parts(wrap);

  fire(input, 'focus');
  fire(input, 'keydown', DOWN);
  fire(input, 'keydown', DOWN);
  assert.deepEqual(list.children[1].scrollCalls, [[{ block: 'nearest' }]]);
  assert.deepEqual(list.children[0].scrollCalls, [[{ block: 'nearest' }]]);
});

test('Enter on the active row writes the canonical id, resolves it and closes the list', () => {
  const wrap = Pickers.fkControl(refColumn, '', ctxWith({ Items: [{ id: ' 042 ', name: 'Iron Sword' }] }));
  const { input, cell, list } = parts(wrap);

  fire(input, 'focus');
  fire(input, 'keydown', DOWN);
  // Prevented, so Enter does not also do whatever the surrounding form does with it.
  assert.equal(fire(input, 'keydown', { key: 'Enter' }), false);

  assert.equal(cell.value, '42');
  assert.equal(input.value, '42 — Iron Sword');
  assert.equal(list.hidden, true);
  assert.equal(input.getAttribute('aria-expanded'), 'false');
  assert.equal(activeRow(wrap), null, 'closing the list clears the cursor');
});

test('the keyboard and mouse paths accept a row through the same code', () => {
  // Both read the id back off the row, so a picked value cannot depend on how it was picked.
  const pick = (howToPick) => {
    const wrap = Pickers.fkControl(refColumn, '', ctxWith({ Items: entries }));
    const { input, list } = parts(wrap);
    fire(input, 'focus');
    howToPick(input, list);
    return parts(wrap).cell.value;
  };

  assert.equal(pick((input, list) => fire(list.children[2], 'click')), '43');
  assert.equal(pick((input) => {
    fire(input, 'keydown', DOWN);
    fire(input, 'keydown', DOWN);
    fire(input, 'keydown', DOWN);
    fire(input, 'keydown', { key: 'Enter' });
  }), '43');
});

test('Escape closes the list without touching the typed value', () => {
  const wrap = Pickers.fkControl(refColumn, '', ctxWith({ Items: entries }));
  const { input, list } = parts(wrap);

  input.value = 'iron';
  fire(input, 'input');
  fire(input, 'keydown', DOWN);
  assert.equal(fire(input, 'keydown', { key: 'Escape' }), false);

  assert.equal(list.hidden, true);
  assert.equal(input.value, 'iron', 'Escape dismisses the list, it does not revert the field');
  assert.equal(activeRow(wrap), null);
});

test('Escape with the list already closed is left to whatever encloses the control', () => {
  // The editor runs in a Sheets sidebar; swallowing an idle Escape would break its own handling.
  const wrap = Pickers.fkControl(refColumn, '', ctxWith({ Items: entries }));
  const { input } = parts(wrap);

  assert.equal(fire(input, 'keydown', { key: 'Escape' }), true);
});

test('an arrow key reopens a list dismissed with Escape', () => {
  // Without this the only way back is to retype a character, which for a field already holding
  // the right id means deleting and retyping it.
  const wrap = Pickers.fkControl(refColumn, '', ctxWith({ Items: entries }));
  const { input, list } = parts(wrap);

  fire(input, 'focus');
  fire(input, 'keydown', { key: 'Escape' });
  assert.equal(list.hidden, true);

  fire(input, 'keydown', DOWN);
  assert.equal(list.hidden, false);
  assert.equal(activeRow(wrap).getAttribute('data-id'), '1');
});

test('arrow keys on an empty result list do nothing at all', () => {
  const wrap = Pickers.fkControl(refColumn, '', ctxWith({ Items: entries }));
  const { input, list } = parts(wrap);

  input.value = 'zzz';
  fire(input, 'input');
  assert.equal(list.hidden, true);

  fire(input, 'keydown', DOWN);
  fire(input, 'keydown', UP);
  assert.equal(list.hidden, true);
  assert.equal(activeRow(wrap), null);
  assert.equal(input.value, 'zzz');
});

test('Tab with a row active accepts it and still lets focus move on', () => {
  const wrap = Pickers.fkControl(refColumn, '', ctxWith({ Items: entries }));
  const { input, cell, list } = parts(wrap);

  fire(input, 'focus');
  fire(input, 'keydown', DOWN);
  // NOT prevented: having arrowed to a row, leaving the field commits it — but Tab must still
  // move focus, or the user is trapped in the control.
  assert.equal(fire(input, 'keydown', { key: 'Tab' }), true);
  assert.equal(cell.value, '1');
  assert.equal(input.value, '1 — Gold');
  assert.equal(list.hidden, true);
});

test('Tab with no row active is an ordinary Tab', () => {
  const wrap = Pickers.fkControl(refColumn, '', ctxWith({ Items: entries }));
  const { input } = parts(wrap);

  input.value = '999';
  fire(input, 'input');
  assert.equal(fire(input, 'keydown', { key: 'Tab' }), true);
  assert.equal(input.value, '999', 'a hand-typed id must survive tabbing out');
});

test('typing after arrowing to a row clears the cursor rather than leaving it on a stale row', () => {
  // refresh() replaces every row, so an active index kept across a keystroke would point at a
  // detached node — and aria-activedescendant would name an id no longer in the document.
  const wrap = Pickers.fkControl(refColumn, '', ctxWith({ Items: entries }));
  const { input, list } = parts(wrap);

  fire(input, 'focus');
  fire(input, 'keydown', DOWN);
  const stale = activeRow(wrap);

  input.value = 'iron';
  fire(input, 'input');
  assert.equal(stale.parentNode, null);
  assert.equal(activeRow(wrap), null);

  // And the cursor works again on the NEW rows, from their own top.
  fire(input, 'keydown', DOWN);
  assert.equal(activeRow(wrap).getAttribute('data-id'), '42');
  assert.equal(list.children.indexOf(activeRow(wrap)), 0);
});

test('a hand-typed id earns its display on blur, and the cell stays bare', () => {
  const wrap = Pickers.fkControl(refColumn, '', ctxWith({ Items: entries }));
  const { input, cell } = parts(wrap);

  input.value = '43';
  fire(input, 'input');
  assert.equal(cell.value, '43');
  assert.equal(input.value, '43', 'no reformatting mid-keystroke — it would fight the caret');

  fire(input, 'blur');
  assert.equal(input.value, '43 — Iron Shield');
  assert.equal(cell.value, '43');
});

test('editing the formatted display keeps writing the bare id, never the display text', () => {
  // Backspacing through "43 — Iron Shield" passes through "43 — Iron Shiel", "43 — ...": every
  // one of those must reach the cell as '43', or a save mid-edit stores the display string.
  const wrap = Pickers.fkControl(refColumn, '43', ctxWith({ Items: entries }));
  const { input, cell } = parts(wrap);
  assert.equal(input.value, '43 — Iron Shield');

  input.value = '43 — Iron Shiel';
  fire(input, 'input');
  assert.equal(cell.value, '43');
});

test('unresolvable text is never rewritten under the user', () => {
  const wrap = Pickers.fkControl(refColumn, '', ctxWith({ Items: entries }));
  const { input, cell, label } = parts(wrap);

  input.value = 'zzz';
  fire(input, 'input');
  fire(input, 'blur');
  assert.equal(input.value, 'zzz');
  assert.equal(cell.value, 'zzz', 'the typo reaches the cell verbatim, for Validation to report');
  assert.equal(label.textContent, 'not found in Items');
});

test('the display heals when the referenced sheet arrives after a blur', () => {
  // The list loads asynchronously; a record opened before it lands shows the raw id. The next
  // time the user leaves the field, display() runs against the now-present entries.
  const ctx = ctxWith({});
  const wrap = Pickers.fkControl(refColumn, '42', ctx);
  assert.equal(parts(wrap).input.value, '42');

  ctx.pickerData.Items = entries;
  fire(parts(wrap).input, 'blur');
  assert.equal(parts(wrap).input.value, '42 — Iron Sword');
});

test('fkControl shows the SQL default as its placeholder, like every other control', () => {
  const wrap = Pickers.fkControl(optionalColumn, '', ctxWith({}));
  assert.equal(parts(wrap).input.getAttribute('placeholder'), 'default 0');
  const required = Pickers.fkControl(refColumn, '', ctxWith({}));
  assert.equal(parts(required).input.getAttribute('placeholder'), 'required');
});

test('fkControl labels an entry with a blank name rather than showing nothing', () => {
  const wrap = Pickers.fkControl(refColumn, '5', ctxWith({ Items: [{ id: '5', name: '' }] }));
  assert.equal(parts(wrap).input.value, '5 — (unnamed)');
});

test('the fkControl input is the only named node, so Forms.collect sees one value', () => {
  const wrap = Pickers.fkControl(refColumn, '42', ctxWith({ Items: entries }));
  assert.equal(wrap.querySelectorAll('[name]').length, 1);
});

// --- fkControl: a selected value does not filter the list to itself ---------------------------

test('focusing a filled-in picker offers every other row, not just the one selected', () => {
  // THE COMPLAINT. The field reads "42 — Iron Sword"; picking something else used to require
  // clearing it first, because the display text was searched as a query.
  const wrap = Pickers.fkControl(refColumn, '42', ctxWith({ Items: entries }));
  const { input, list } = parts(wrap);

  fire(input, 'focus');
  assert.deepEqual(list.children.map((c) => c.getAttribute('data-id')),
    ['42', '1', '43', '100']);
});

test('the selected row opens as the active one, so the arrows walk out from it', () => {
  const wrap = Pickers.fkControl(refColumn, '42', ctxWith({ Items: entries }));
  const { input } = parts(wrap);

  fire(input, 'focus');
  assert.equal(activeRow(wrap).getAttribute('data-id'), '42');
  // Down moves to the next row rather than back to the first.
  fire(input, 'keydown', DOWN);
  assert.equal(activeRow(wrap).getAttribute('data-id'), '1');
});

test('a value that does not resolve marks no row, so nothing is offered as "current"', () => {
  const wrap = Pickers.fkControl(refColumn, '999', ctxWith({ Items: entries }));
  const { input, list } = parts(wrap);

  fire(input, 'focus');
  assert.deepEqual(list.children.map((c) => c.getAttribute('data-id')), ['1', '42', '43', '100']);
  assert.equal(activeRow(wrap), null);
});

test('typing switches back to filtering, and the value is no longer hoisted', () => {
  const wrap = Pickers.fkControl(refColumn, '42', ctxWith({ Items: entries }));
  const { input, list } = parts(wrap);

  fire(input, 'focus');
  input.value = 'iron';
  fire(input, 'input');
  // The matches, in sheet order — 42 is a hit here on its own merits, not because it was selected.
  assert.deepEqual(list.children.map((c) => c.getAttribute('data-id')), ['42', '43']);
  assert.equal(activeRow(wrap), null, 'a query offers no row until the user picks one');

  // And a query that matches nothing still says so, rather than falling back to the whole sheet.
  input.value = 'zzz';
  fire(input, 'input');
  assert.equal(list.hidden, true);
});

test('a query left in the field goes back to browsing once the field is re-entered', () => {
  // blur re-displays the field FROM THE CELL, and every keystroke has already written the cell
  // verbatim — that is the control's existing contract, so 'iron' is now the stored value and
  // Validation is what reports it. What this pins is that the list stops treating it as a query:
  // re-focusing offers the sheet again rather than the two rows 'iron' matched.
  const wrap = Pickers.fkControl(refColumn, '42', ctxWith({ Items: entries }));
  const { input, cell } = parts(wrap);

  fire(input, 'focus');
  input.value = 'iron';
  fire(input, 'input');
  fire(input, 'blur');
  assert.equal(cell.value, 'iron');
  assert.equal(input.value, 'iron', 'nothing the user typed is rewritten under them');

  fire(input, 'focus');
  assert.deepEqual(parts(wrap).list.children.map((c) => c.getAttribute('data-id')),
    ['1', '42', '43', '100']);
  // Nothing is hoisted or marked: 'iron' is not a row of Items.
  assert.equal(activeRow(wrap), null);
  assert.equal(parts(wrap).label.textContent, 'not found in Items');
});

test('a hand-typed id that resolves on blur is then hoisted and marked', () => {
  // The other half: display() turns '43' into "43 — Iron Shield" on blur, which makes it the
  // current value — so the next focus browses from it, exactly as a picked row would.
  const wrap = Pickers.fkControl(refColumn, '', ctxWith({ Items: entries }));
  const { input } = parts(wrap);

  input.value = '43';
  fire(input, 'input');
  fire(input, 'blur');
  assert.equal(input.value, '43 — Iron Shield');

  fire(input, 'focus');
  assert.deepEqual(parts(wrap).list.children.map((c) => c.getAttribute('data-id')),
    ['43', '1', '42', '100']);
  assert.equal(activeRow(wrap).getAttribute('data-id'), '43');
});

test('picking a different row leaves the list browsing from the NEW value', () => {
  const wrap = Pickers.fkControl(refColumn, '42', ctxWith({ Items: entries }));
  const { input, list } = parts(wrap);

  fire(input, 'focus');
  fire(list.children[2], 'click');          // 43 — Iron Shield
  assert.equal(parts(wrap).cell.value, '43');

  fire(input, 'focus');
  assert.deepEqual(parts(wrap).list.children.map((c) => c.getAttribute('data-id')),
    ['43', '1', '42', '100']);
});

test('an empty picker still opens on the head of the list with nothing active', () => {
  // Unchanged behaviour, asserted next to the new one: with no value there is nothing to hoist and
  // no row to offer, so the "no row active" rule still holds where it always did.
  const wrap = Pickers.fkControl(refColumn, '', ctxWith({ Items: entries }));
  const { input, list } = parts(wrap);

  fire(input, 'focus');
  assert.deepEqual(list.children.map((c) => c.getAttribute('data-id')), ['1', '42', '43', '100']);
  assert.equal(activeRow(wrap), null);
});

test('a stored 0 browses the list rather than hoisting a row', () => {
  // 0 is "none", which is not a row of the referenced sheet at all.
  const wrap = Pickers.fkControl(optionalColumn, '0', ctxWith({ 'Spell Effects': entries }));
  const { input, list } = parts(wrap);

  fire(input, 'focus');
  assert.deepEqual(list.children.map((c) => c.getAttribute('data-id')), ['1', '42', '43', '100']);
  assert.equal(activeRow(wrap), null);
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
  // type=button, not the default submit: these rows sit inside the record form, and a submit
  // button would submit it on every single result click.
  assert.deepEqual(list.children.map((c) => c.getAttribute('type')),
    ['button', 'button', 'button', 'button']);
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
  const { input, cell, label, list } = parts(wrap);

  fire(input, 'focus');
  fire(list.children[2], 'click');

  assert.equal(cell.value, '43', 'the cell holds the bare id and nothing else');
  assert.equal(input.value, '43 — Iron Shield');
  assert.equal(label.textContent, '', 'the name is in the field, not repeated below it');
  assert.equal(list.hidden, true);
});

test('clicking a result writes the canonical id, not the raw cell text', () => {
  const messy = ctxWith({ Items: [{ id: ' 042 ', name: 'Iron Sword' }] });
  const wrap = Pickers.fkControl(refColumn, '', messy);
  const { input, cell, list } = parts(wrap);

  fire(input, 'focus');
  assert.equal(list.children[0].textContent, '42 — Iron Sword');
  fire(list.children[0], 'click');
  assert.equal(cell.value, '42');
  // The DISPLAY too: resolving '42' against a stored ' 042 ' is the lookup's own
  // canonicalisation, and without this the row text alone would pass with a raw string
  // comparison in find().
  assert.equal(input.value, '42 — Iron Sword');
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

test('blur into the list itself leaves it open', () => {
  // The mousedown cancel above means focus normally never leaves the input, so this is the other
  // routes in: assistive tech calling row.focus(), or a browser that focuses the mousedown target
  // regardless. Hiding then would pull the list out from under the focus that just arrived.
  const wrap = Pickers.fkControl(refColumn, '', ctxWith({ Items: entries }));
  const { input, list } = parts(wrap);

  fire(input, 'focus');
  fire(input, 'blur', { relatedTarget: list.children[1] });
  assert.equal(list.hidden, false);

  // …and focus moving anywhere else still closes it.
  fire(input, 'blur', { relatedTarget: parts(wrap).label });
  assert.equal(list.hidden, true);
});

// --- graphicControl -------------------------------------------------------------------------

const graphicColumn = { name: 'graphic_tile', kind: 'Id', sql: 'INTEGER', required: false, default: '0' };
const fileColumn = { name: 'graphic_file', kind: 'Id', sql: 'INTEGER', required: false, default: '0' };

const bundles = {
  // A non-square rect with ODD dimensions, for two reasons at once: on a square box the two
  // centring offsets would be equal, so swapping width for height would go unnoticed — and with
  // even dimensions the centring divides exactly, so dropping Math.floor would go unnoticed as
  // well. 31x17 in a 64 box gives 16.5 and 23.5 before flooring.
  icons: { rects: { '20107:810003': [96, 0, 31, 17] } },
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
  const wrap = Pickers.graphicControl({
    graphicColumn: graphicColumn, fileColumn: fileColumn,
    values: { graphic_tile: '810003', graphic_file: '20107' }, ctx: gctx(),
  });
  const { graphic, file } = gparts(wrap);

  assert.equal(graphic.value, '810003');
  assert.equal(file.value, '20107');
  assert.equal(wrap.querySelectorAll('[name]').length, 2);
});

test('graphicControl keeps a stored 0 rather than blanking it', () => {
  const wrap = Pickers.graphicControl({
    graphicColumn: graphicColumn, fileColumn: fileColumn,
    values: { graphic_tile: 0, graphic_file: 0 }, ctx: gctx(),
  });
  assert.equal(gparts(wrap).graphic.value, '0');
  assert.equal(gparts(wrap).file.value, '0');
});

test('graphicControl draws the resolved icon centred on the canvas', () => {
  const wrap = Pickers.graphicControl({
    graphicColumn: graphicColumn, fileColumn: fileColumn,
    values: { graphic_tile: '810003', graphic_file: '20107' }, ctx: gctx(),
  });
  const ctx2d = gparts(wrap).canvas.getContext('2d');

  assert.deepEqual(ctx2d.calls, [
    ['setTransform', 2, 0, 0, 2, 0, 0],
    ['imageSmoothingEnabled', false],
    ['clearRect', 0, 0, 64, 64],
    ['drawImage', 'ICONS', 96, 0, 31, 17, 16, 23, 31, 17],
  ]);
});

test('graphicControl passes sheet and graphic in the right order', () => {
  // The Graphic composite declares [graphic, file] (schema.js: graphic_tile+graphic_file) and
  // Sprites.icon takes (bundles, sheet, graphic). Swapping them resolves nothing, silently.
  const wrap = Pickers.graphicControl({
    graphicColumn: graphicColumn, fileColumn: fileColumn,
    values: { graphic_tile: '20107', graphic_file: '810003' }, ctx: gctx(),
  });
  const drew = gparts(wrap).canvas.getContext('2d').calls
    .filter((c) => c[0] === 'drawImage');
  assert.deepEqual(drew, [], 'the reversed pair must not resolve');
});

test('graphicControl redraws when the graphic alone is edited', () => {
  const wrap = Pickers.graphicControl({
    graphicColumn: graphicColumn, fileColumn: fileColumn,
    values: { graphic_tile: '810003', graphic_file: '20107' }, ctx: gctx(),
  });
  const { graphic, status } = gparts(wrap);

  graphic.value = '999';
  fire(graphic, 'input');
  assert.equal(status.textContent, 'no art for sheet 20107 graphic 999');
});

test('graphicControl redraws when the sheet alone is edited', () => {
  const wrap = Pickers.graphicControl({
    graphicColumn: graphicColumn, fileColumn: fileColumn,
    values: { graphic_tile: '810003', graphic_file: '20107' }, ctx: gctx(),
  });
  const { file, status } = gparts(wrap);

  file.value = '999';
  fire(file, 'input');
  assert.equal(status.textContent, 'no art for sheet 999 graphic 810003');
});

test('graphicControl redraws as either field is edited', () => {
  const wrap = Pickers.graphicControl({
    graphicColumn: graphicColumn, fileColumn: fileColumn, values: {}, ctx: gctx(),
  });
  const { canvas, graphic, file } = gparts(wrap);
  const ctx2d = canvas.getContext('2d');

  graphic.value = '810003';
  fire(graphic, 'input');
  assert.deepEqual(ctx2d.calls.filter((c) => c[0] === 'drawImage'), []);

  file.value = '20107';
  fire(file, 'input');
  assert.deepEqual(ctx2d.calls.filter((c) => c[0] === 'drawImage'),
    [['drawImage', 'ICONS', 96, 0, 31, 17, 16, 23, 31, 17]]);
});

test('graphicControl coerces exactly once, leaving the rule to Sprites', () => {
  // Sprites.icon coerces with parseInt (matching Equipped and Appearance). A Number() here as
  // well is a SECOND rule for what a cell means, and the two disagree: Number('20107abc') is
  // NaN and finds nothing, parseInt is 20107 and finds the icon. The preview shows what the
  // lookup key actually resolves; the status line is what says the cell is still a typo.
  const wrap = Pickers.graphicControl({
    graphicColumn: graphicColumn, fileColumn: fileColumn,
    values: { graphic_tile: '0810003', graphic_file: '20107abc' }, ctx: gctx(),
  });
  assert.deepEqual(gparts(wrap).canvas.getContext('2d').calls.filter((c) => c[0] === 'drawImage'),
    [['drawImage', 'ICONS', 96, 0, 31, 17, 16, 23, 31, 17]]);
  assert.equal(gparts(wrap).status.textContent, 'graphic and sheet must be whole numbers');
});

test('graphicControl accepts a leading-zero pair, as Sprites and Validation both do', () => {
  const wrap = Pickers.graphicControl({
    graphicColumn: graphicColumn, fileColumn: fileColumn,
    values: { graphic_tile: '0810003', graphic_file: '020107' }, ctx: gctx(),
  });
  assert.deepEqual(gparts(wrap).canvas.getContext('2d').calls.filter((c) => c[0] === 'drawImage'),
    [['drawImage', 'ICONS', 96, 0, 31, 17, 16, 23, 31, 17]]);
  assert.equal(gparts(wrap).status.textContent, '');
});

test('graphicControl reports both cells blank as "no graphic", not as an error', () => {
  const wrap = Pickers.graphicControl({
    graphicColumn: graphicColumn, fileColumn: fileColumn,
    values: { graphic_tile: '', graphic_file: '' }, ctx: gctx(),
  });
  assert.equal(gparts(wrap).status.textContent, 'no graphic');
  assert.equal(gparts(wrap).status.className, 'status');

  const zeros = Pickers.graphicControl({
    graphicColumn: graphicColumn, fileColumn: fileColumn,
    values: { graphic_tile: '0', graphic_file: '0' }, ctx: gctx(),
  });
  assert.equal(gparts(zeros).status.textContent, 'no graphic');
});

test('graphicControl reports a non-numeric graphic id rather than silently showing nothing', () => {
  // Prerequisite #13: neither Equipped.format nor isFaithful catches a UI typo, so the field
  // that accepts it has to say so.
  const wrap = Pickers.graphicControl({
    graphicColumn: graphicColumn, fileColumn: fileColumn,
    values: { graphic_tile: '81o003', graphic_file: '20107' }, ctx: gctx(),
  });
  assert.equal(gparts(wrap).status.textContent, 'graphic and sheet must be whole numbers');
  assert.equal(gparts(wrap).status.className, 'status bad');
});

test('graphicControl reports half a pair as incomplete', () => {
  const wrap = Pickers.graphicControl({
    graphicColumn: graphicColumn, fileColumn: fileColumn,
    values: { graphic_tile: '810003', graphic_file: '' }, ctx: gctx(),
  });
  assert.equal(gparts(wrap).status.textContent, 'graphic and sheet must both be set');
  assert.equal(gparts(wrap).status.className, 'status bad');
});

test('graphicControl reports half a pair as incomplete the other way round too', () => {
  // A BLANK cell is half a pair, not a typo — in either position. Testing only one of the two
  // lets `(g !== '' && !isWhole(g))` decay to `!isWhole(g)`, which calls a blank graphic a
  // malformed number.
  const wrap = Pickers.graphicControl({
    graphicColumn: graphicColumn, fileColumn: fileColumn,
    values: { graphic_tile: '', graphic_file: '20107' }, ctx: gctx(),
  });
  assert.equal(gparts(wrap).status.textContent, 'graphic and sheet must both be set');
  assert.equal(gparts(wrap).status.className, 'status bad');
});

test('graphicControl reports a pair with no art in the bundle', () => {
  const wrap = Pickers.graphicControl({
    graphicColumn: graphicColumn, fileColumn: fileColumn,
    values: { graphic_tile: '9', graphic_file: '8' }, ctx: gctx(),
  });
  assert.equal(gparts(wrap).status.textContent, 'no art for sheet 8 graphic 9');
  assert.equal(gparts(wrap).status.className, 'status bad');
});

test('graphicControl clears the status once the pair resolves', () => {
  const wrap = Pickers.graphicControl({
    graphicColumn: graphicColumn, fileColumn: fileColumn,
    values: { graphic_tile: '810003', graphic_file: '20107' }, ctx: gctx(),
  });
  assert.equal(gparts(wrap).status.textContent, '');
  assert.equal(gparts(wrap).status.className, 'status');
});

test('graphicControl redraws when the bundle image finishes decoding', () => {
  const ctx = gctx();
  ctx.images = {};
  const wrap = Pickers.graphicControl({
    graphicColumn: graphicColumn, fileColumn: fileColumn,
    values: { graphic_tile: '810003', graphic_file: '20107' }, ctx: ctx,
  });
  const ctx2d = gparts(wrap).canvas.getContext('2d');

  assert.deepEqual(ctx2d.calls.filter((c) => c[0] === 'drawImage'), [],
    'Sprites.draw must skip a layer whose PNG has not decoded');
  assert.equal(ctx.__ready.length, 1);

  ctx.images.icons = 'ICONS';
  ctx.__ready[0]();
  assert.deepEqual(ctx2d.calls.filter((c) => c[0] === 'drawImage'),
    [['drawImage', 'ICONS', 96, 0, 31, 17, 16, 23, 31, 17]]);
});

test('graphicControl survives a ctx with no bundles, images or ready hook', () => {
  const wrap = Pickers.graphicControl({
    graphicColumn: graphicColumn, fileColumn: fileColumn,
    values: { graphic_tile: '810003', graphic_file: '20107' }, ctx: {},
  });
  // "cannot check", not "no art": with no bundle there is nothing to have checked against, and
  // the save gate must not read a missing include as 800 broken records.
  assert.equal(gparts(wrap).status.textContent,
    'cannot check sheet 20107 graphic 810003 — no icon art loaded');
  assert.equal(wrap.__graphicError, null);
});

// --- what the save gate reads (review #2) ----------------------------------------------------

test('an unresolvable pair is published for the save gate, named by its column', () => {
  const wrap = Pickers.graphicControl({
    graphicColumn: graphicColumn, fileColumn: fileColumn,
    values: { graphic_tile: '9', graphic_file: '8' }, ctx: gctx(),
  });
  assert.equal(wrap.__graphicError, 'graphic_tile: no art for sheet 8 graphic 9');
});

test('the gate flag is raised and cleared as the cells are edited', () => {
  const wrap = Pickers.graphicControl({
    graphicColumn: graphicColumn, fileColumn: fileColumn,
    values: { graphic_tile: '810003', graphic_file: '20107' }, ctx: gctx(),
  });
  assert.equal(wrap.__graphicError, null);

  const { graphic } = gparts(wrap);
  graphic.value = '999';
  fire(graphic, 'input');
  assert.equal(wrap.__graphicError, 'graphic_tile: no art for sheet 20107 graphic 999');

  graphic.value = '810003';
  fire(graphic, 'input');
  assert.equal(wrap.__graphicError, null);
});

test('a blank or zero pair publishes nothing to gate on', () => {
  const blank = Pickers.graphicControl({
    graphicColumn: graphicColumn, fileColumn: fileColumn, values: {}, ctx: gctx(),
  });
  const zeros = Pickers.graphicControl({
    graphicColumn: graphicColumn, fileColumn: fileColumn,
    values: { graphic_tile: '0', graphic_file: '0' }, ctx: gctx(),
  });
  assert.equal(blank.__graphicError, null);
  assert.equal(zeros.__graphicError, null);
});

test('half a pair is SHOWN but not gated on — 176 shipped Spell Effects rows are half pairs', () => {
  const wrap = Pickers.graphicControl({
    graphicColumn: graphicColumn, fileColumn: fileColumn,
    values: { graphic_tile: '815015', graphic_file: '0' }, ctx: gctx(),
  });
  assert.equal(gparts(wrap).status.className, 'status bad');
  assert.equal(wrap.__graphicError, null);
});

test('a non-numeric cell is not gated on either — Validation reports it under the field', () => {
  const wrap = Pickers.graphicControl({
    graphicColumn: graphicColumn, fileColumn: fileColumn,
    values: { graphic_tile: 'abc', graphic_file: '20107' }, ctx: gctx(),
  });
  assert.equal(gparts(wrap).status.className, 'status bad');
  assert.equal(wrap.__graphicError, null);
});

test('graphicControl shows each column default as its placeholder', () => {
  const wrap = Pickers.graphicControl({
    graphicColumn: graphicColumn, fileColumn: fileColumn, values: {}, ctx: gctx(),
  });
  assert.equal(gparts(wrap).graphic.getAttribute('placeholder'), 'default 0');
  assert.equal(gparts(wrap).file.getAttribute('placeholder'), 'default 0');
});

test('a column with no default still gets a useful placeholder', () => {
  const bare = { name: 'graphic_tile', kind: 'Id', sql: 'INTEGER', required: false };
  const bareFile = { name: 'graphic_file', kind: 'Id', sql: 'INTEGER', required: false };
  const wrap = Pickers.graphicControl({
    graphicColumn: bare, fileColumn: bareFile, values: {}, ctx: gctx(),
  });
  assert.equal(gparts(wrap).graphic.getAttribute('placeholder'), 'graphic');
  assert.equal(gparts(wrap).file.getAttribute('placeholder'), 'sheet');
});

test('the graphic cells carry the same id and attributes as every other control', () => {
  const wrap = Pickers.graphicControl({
    graphicColumn: graphicColumn, fileColumn: fileColumn, values: {}, ctx: gctx(),
  });
  const { graphic, file } = gparts(wrap);

  assert.equal(graphic.getAttribute('id'), 'f-graphic_tile');
  assert.equal(file.getAttribute('id'), 'f-graphic_file');
  assert.equal(graphic.getAttribute('type'), 'text');
  assert.equal(graphic.getAttribute('autocomplete'), 'off');
});

test('a padded cell is not mistaken for a typo', () => {
  // Sheets hands back whatever was typed. Sprites.icon's parseInt tolerates the padding, so
  // the status line has to as well or it contradicts the preview sitting next to it.
  const wrap = Pickers.graphicControl({
    graphicColumn: graphicColumn, fileColumn: fileColumn,
    values: { graphic_tile: ' 810003 ', graphic_file: ' 20107 ' }, ctx: gctx(),
  });
  assert.equal(gparts(wrap).status.textContent, '');
  assert.deepEqual(gparts(wrap).canvas.getContext('2d').calls.filter((c) => c[0] === 'drawImage'),
    [['drawImage', 'ICONS', 96, 0, 31, 17, 16, 23, 31, 17]]);
});

test('the icon preview canvas is a 64 logical box drawn at 2x', () => {
  // 128 CSS pixels of canvas for a 64-pixel box. 64 rather than 48 because the bundle holds
  // sprites up to 128x128 and 48 clipped them; bigger still clips, which is a bundle fact.
  const wrap = Pickers.graphicControl({
    graphicColumn: graphicColumn, fileColumn: fileColumn, values: {}, ctx: gctx(),
  });
  const canvas = gparts(wrap).canvas;
  assert.equal(canvas.width, 128);
  assert.equal(canvas.height, 128);
  assert.deepEqual(canvas.getContext('2d').calls[0], ['setTransform', 2, 0, 0, 2, 0, 0]);
});

// --- graphicControl tinting (Layout.TINTS + ctx.onFormChange) --------------------------------

const TINT_COLUMNS = ['graphic_r', 'graphic_g', 'graphic_b', 'graphic_a'];

// A ctx that also records onFormChange subscribers, so a test can play the delegated listener.
function tctx(values) {
  const c = gctx(values);
  const changed = [];
  c.onFormChange = (fn) => changed.push(fn);
  c.__changed = changed;
  // Fires the way app.js's refreshPreviews does: every subscriber, with the whole collected form.
  c.emit = (form) => changed.forEach((fn) => fn(form));
  return c;
}

// A tinted draw goes through an OFFSCREEN canvas, so it lands as drawImage(canvasNode, dx, dy) —
// three arguments and a node — where an untinted one is the nine-argument blit of the bundle image.
// That difference is the only observable "was it tinted", and it is the one the game cares about.
const tintedDraws = (canvas) => canvas.getContext('2d').calls
  .filter((c) => c[0] === 'drawImage' && c.length === 4);
const plainDraws = (canvas) => canvas.getContext('2d').calls
  .filter((c) => c[0] === 'drawImage' && c.length === 10);

test('an untinted graphic control ignores the tint cells entirely', () => {
  // Spells' spellbook_graphic has no tint columns, so Layout.tintColumns answers null and the
  // control is handed null. A stray graphic_r in the values map must not tint it.
  const wrap = Pickers.graphicControl({
    graphicColumn: graphicColumn, fileColumn: fileColumn,
    values: { graphic_tile: '810003', graphic_file: '20107', graphic_r: 255, graphic_a: 255 },
    ctx: tctx(), tintColumns: null,
  });
  assert.equal(tintedDraws(gparts(wrap).canvas).length, 0);
  assert.equal(plainDraws(gparts(wrap).canvas).length, 1);
});

test('a graphic with no tint columns does not subscribe to form changes at all', () => {
  // Otherwise every keystroke in item_description would run a redraw that cannot change anything.
  const c = tctx();
  Pickers.graphicControl({
    graphicColumn: graphicColumn, fileColumn: fileColumn,
    values: { graphic_tile: '810003', graphic_file: '20107' }, ctx: c, tintColumns: null,
  });
  assert.equal(c.__changed.length, 0);
});

test('the stored tint is applied on the FIRST draw, not only after an edit', () => {
  // A preview that picked the tint up only from onFormChange would show every freshly opened
  // record plain, and the tint would appear on the first unrelated keystroke.
  const wrap = Pickers.graphicControl({
    graphicColumn: graphicColumn, fileColumn: fileColumn,
    values: { graphic_tile: '810003', graphic_file: '20107',
              graphic_r: 255, graphic_g: 0, graphic_b: 0, graphic_a: 255 },
    ctx: tctx(), tintColumns: TINT_COLUMNS,
  });
  assert.equal(tintedDraws(gparts(wrap).canvas).length, 1);
});

test('a zero blend is drawn plain, colour and all', () => {
  // Icon.cs:23 mixes BY the alpha, so a parked colour behind a zero blend never renders.
  const wrap = Pickers.graphicControl({
    graphicColumn: graphicColumn, fileColumn: fileColumn,
    values: { graphic_tile: '810003', graphic_file: '20107',
              graphic_r: 255, graphic_g: 0, graphic_b: 0, graphic_a: 0 },
    ctx: tctx(), tintColumns: TINT_COLUMNS,
  });
  assert.equal(tintedDraws(gparts(wrap).canvas).length, 0);
  assert.equal(plainDraws(gparts(wrap).canvas).length, 1);
});

test('a tint edit in another control redraws the tile with the new tint', () => {
  // THE BUG THIS FIXES: graphic_r/g/b/a live in the Rgba composite's hidden inputs, so this
  // control cannot see them and the tile visibly ignored every tint edit.
  const c = tctx();
  const wrap = Pickers.graphicControl({
    graphicColumn: graphicColumn, fileColumn: fileColumn,
    values: { graphic_tile: '810003', graphic_file: '20107' }, ctx: c, tintColumns: TINT_COLUMNS,
  });
  const canvas = gparts(wrap).canvas;

  assert.equal(tintedDraws(canvas).length, 0, 'no tint stored, so the first draw is plain');
  assert.equal(c.__changed.length, 1, 'a tinted graphic subscribes exactly once');

  c.emit({ graphic_tile: '810003', graphic_file: '20107',
           graphic_r: 12, graphic_g: 164, graphic_b: 51, graphic_a: 200 });

  assert.equal(tintedDraws(canvas).length, 1, 'the redraw is tinted');
  // Cleared first, so the tinted sprite replaces the plain one rather than landing on top of it.
  const calls = canvas.getContext('2d').calls.map((x) => x[0]);
  assert.ok(calls.lastIndexOf('clearRect') < calls.lastIndexOf('drawImage'));
});

test('a tint edit that clears the blend goes back to a plain draw', () => {
  const c = tctx();
  const wrap = Pickers.graphicControl({
    graphicColumn: graphicColumn, fileColumn: fileColumn,
    values: { graphic_tile: '810003', graphic_file: '20107', graphic_r: 255, graphic_a: 255 },
    ctx: c, tintColumns: TINT_COLUMNS,
  });
  const canvas = gparts(wrap).canvas;
  assert.equal(tintedDraws(canvas).length, 1);

  c.emit({ graphic_tile: '810003', graphic_file: '20107', graphic_r: 255, graphic_a: 0 });
  assert.equal(tintedDraws(canvas).length, 1, 'no second tinted draw');
  assert.equal(plainDraws(canvas).length, 1);
});

test('editing the graphic cell keeps the tint that arrived through onFormChange', () => {
  // The two redraw paths share one `latest`; if the input handler read the build-time values map
  // instead, typing a new id would silently drop the tint.
  const c = tctx();
  const wrap = Pickers.graphicControl({
    graphicColumn: graphicColumn, fileColumn: fileColumn, values: {}, ctx: c,
    tintColumns: TINT_COLUMNS,
  });
  const { canvas, graphic, file } = gparts(wrap);

  c.emit({ graphic_r: 255, graphic_g: 0, graphic_b: 0, graphic_a: 255 });
  graphic.value = '810003';
  fire(graphic, 'input');
  file.value = '20107';
  fire(file, 'input');

  assert.equal(tintedDraws(canvas).length, 1);
  assert.equal(plainDraws(canvas).length, 0);
});

// --- partControl ----------------------------------------------------------------------------

const equipColumn = {
  name: 'graphic_equip', kind: 'Int', sql: 'SMALLINT', required: false, default: '0',
};
const SPEC = { categoryFrom: 'item_slot' };

// One rect per folder, each with a DIFFERENT source x, so the drawn sprite says which folder the
// control resolved. Mount is the odd one out: only a mounted-idle-down clip, which Sprites.part
// deliberately refuses to fall back to.
const partRects = {
  'Helms:5:idle-down': [10, 0, 24, 32],
  'Chest:5:idle-down': [20, 0, 24, 32],
  'Legs:5:idle-down': [30, 0, 24, 32],
  'Feet:5:idle-down': [40, 0, 24, 32],
  'Hands:5:idle-down': [50, 0, 24, 32],
  'Bodies:5:mounted-idle-down': [60, 0, 24, 32],
};

function pctx(extra) {
  const changed = [];
  return {
    bundles: { parts: { rects: partRects } },
    images: { parts: 'PARTS' },
    onImagesReady() {},
    onFormChange(fn) { changed.push(fn); },
    __changed: changed,
    emit(form) { changed.forEach((fn) => fn(form)); },
    ...extra,
  };
}

function pparts(wrap) {
  return {
    canvas: wrap.querySelector('[class="preview"]'),
    input: wrap.querySelector('[name="graphic_equip"]'),
    status: wrap.querySelectorAll('[class]').filter((n) => /^status/.test(n.className))[0],
  };
}

// The source x of what was drawn, which identifies the folder the control chose.
function drawnFrom(wrap) {
  const drew = pparts(wrap).canvas.getContext('2d').calls.filter((c) => c[0] === 'drawImage');
  return drew.length ? drew[drew.length - 1][2] : null;
}

test('partControl renders the cell named for Forms.collect, and nothing else named', () => {
  const wrap = Pickers.partControl({
    column: equipColumn, values: { graphic_equip: '5', item_slot: 'Helmet' }, ctx: pctx(),
    spec: SPEC, tintColumns: null,
  });
  assert.equal(pparts(wrap).input.value, '5');
  assert.equal(pparts(wrap).input.getAttribute('id'), 'f-graphic_equip');
  assert.equal(wrap.querySelectorAll('[name]').length, 1);
});

test('partControl keeps a stored 0 rather than blanking it', () => {
  // Blank means "use the SQL default"; 0 means "no equip graphic". Writing one for the other
  // would silently change the row.
  const wrap = Pickers.partControl({
    column: equipColumn, values: { graphic_equip: 0, item_slot: 'Helmet' }, ctx: pctx(),
    spec: SPEC, tintColumns: null,
  });
  assert.equal(pparts(wrap).input.value, '0');
});

test('the worn preview canvas is a 40x56 logical box drawn at 2x', () => {
  const wrap = Pickers.partControl({
    column: equipColumn, values: {}, ctx: pctx(), spec: SPEC, tintColumns: null,
  });
  const canvas = pparts(wrap).canvas;
  assert.equal(canvas.width, 80);
  assert.equal(canvas.height, 112);
  assert.deepEqual(canvas.getContext('2d').calls[0], ['setTransform', 2, 0, 0, 2, 0, 0]);
});

test('each of the seven drawn item slots resolves to its own sprite folder', () => {
  // Asserted through the DRAWN RECT, not through a returned category: the rect is what a designer
  // sees, and each folder here carries a different source x so a wrong folder cannot pass.
  const from = (slot) => drawnFrom(Pickers.partControl({
    column: equipColumn, values: { graphic_equip: '5', item_slot: slot }, ctx: pctx(), spec: SPEC,
    tintColumns: null,
  }));

  assert.equal(from('Helmet'), 10, 'Helms');
  assert.equal(from('Chest'), 20, 'Chest');
  assert.equal(from('Pants'), 30, 'Legs');
  assert.equal(from('Shoes'), 40, 'Feet');
  // Three item slots, one folder: a shield and a weapon are both drawn from Hands.
  assert.equal(from('Shield'), 50, 'Hands');
  assert.equal(from('OneHanded'), 50, 'Hands');
  assert.equal(from('TwoHanded'), 50, 'Hands');
  // A mount is a body in a MOUNTED clip — Sprites.part would find nothing here at all.
  assert.equal(from('Mount'), 60, 'Bodies, mounted');
});

test('all seven drawn slots report no problem', () => {
  ['Helmet', 'Chest', 'Pants', 'Shoes', 'Shield', 'OneHanded', 'TwoHanded', 'Mount']
    .forEach((slot) => {
      const wrap = Pickers.partControl({
        column: equipColumn, values: { graphic_equip: '5', item_slot: slot }, ctx: pctx(),
        spec: SPEC, tintColumns: null,
      });
      assert.equal(pparts(wrap).status.textContent, '', slot);
      assert.equal(pparts(wrap).status.className, 'status', slot);
    });
});

test('the seven undrawn slots say so, and are not called an error', () => {
  // A ring with a graphic id is not a broken row — the client simply has no layer for it. Calling
  // it bad would flag most of the Ring and Misc rows in the sheet.
  ['Ring', 'Necklace', 'Pauldrons', 'Cloak', 'Belt', 'Gloves', 'Misc'].forEach((slot) => {
    const wrap = Pickers.partControl({
      column: equipColumn, values: { graphic_equip: '5', item_slot: slot }, ctx: pctx(),
      spec: SPEC, tintColumns: null,
    });
    assert.equal(pparts(wrap).status.textContent, 'this slot is not drawn on the character', slot);
    assert.equal(pparts(wrap).status.className, 'status', slot);
    assert.equal(drawnFrom(wrap), null, slot + ' must draw nothing');
  });
});

test('an empty or unrecognised item_slot is "not drawn" rather than a guessed folder', () => {
  [undefined, '', 'Nonsense', 0, 'helmet'].forEach((slot) => {
    const wrap = Pickers.partControl({
      column: equipColumn, values: { graphic_equip: '5', item_slot: slot }, ctx: pctx(),
      spec: SPEC, tintColumns: null,
    });
    assert.equal(pparts(wrap).status.textContent, 'this slot is not drawn on the character',
      String(slot));
    assert.equal(drawnFrom(wrap), null);
  });
});

test('the undrawn-slot message wins over a blank graphic', () => {
  // It is a fact about the ROW, not about this cell: "no graphic" would invite the designer to
  // type an id that could never be drawn.
  const wrap = Pickers.partControl({
    column: equipColumn, values: { graphic_equip: '', item_slot: 'Ring' }, ctx: pctx(),
    spec: SPEC, tintColumns: null,
  });
  assert.equal(pparts(wrap).status.textContent, 'this slot is not drawn on the character');
});

test('a blank or zero graphic in a drawn slot is "no graphic", not an error', () => {
  ['', '0', ' '].forEach((value) => {
    const wrap = Pickers.partControl({
      column: equipColumn, values: { graphic_equip: value, item_slot: 'Helmet' }, ctx: pctx(),
      spec: SPEC, tintColumns: null,
    });
    assert.equal(pparts(wrap).status.textContent, 'no graphic', JSON.stringify(value));
    assert.equal(pparts(wrap).status.className, 'status');
    assert.equal(drawnFrom(wrap), null);
  });
});

test('a non-whole graphic is reported, and names the column\'s own rule', () => {
  const wrap = Pickers.partControl({
    column: equipColumn, values: { graphic_equip: '1o', item_slot: 'Helmet' }, ctx: pctx(),
    spec: SPEC, tintColumns: null,
  });
  assert.equal(pparts(wrap).status.textContent, 'graphic must be a whole number');
  assert.equal(pparts(wrap).status.className, 'status bad');
});

test('a graphic with no art names the folder it looked in', () => {
  // The folder is the half of the lookup the designer cannot see in the form, so a bare "no art"
  // would leave them unable to tell a missing sprite from a wrong item_slot.
  const wrap = Pickers.partControl({
    column: equipColumn, values: { graphic_equip: '999', item_slot: 'Helmet' }, ctx: pctx(),
    spec: SPEC, tintColumns: null,
  });
  assert.equal(pparts(wrap).status.textContent, 'no art for Helms graphic 999');
  assert.equal(pparts(wrap).status.className, 'status bad');
});

test('a missing parts bundle is reported as unknown, not as missing art', () => {
  const wrap = Pickers.partControl({
    column: equipColumn, values: { graphic_equip: '5', item_slot: 'Helmet' },
    ctx: pctx({ bundles: {} }), spec: SPEC, tintColumns: null,
  });
  assert.equal(pparts(wrap).status.textContent,
    'cannot check Helms graphic 5 — no character art loaded');
});

test('partControl publishes no save gate — that rule is graphicControl\'s alone', () => {
  // A missing equip sprite is reported, not refused: no design rule has been established for
  // graphic_equip, and inventing one could lock rows that ship today.
  const wrap = Pickers.partControl({
    column: equipColumn, values: { graphic_equip: '999', item_slot: 'Helmet' }, ctx: pctx(),
    spec: SPEC, tintColumns: null,
  });
  assert.equal(wrap.__graphicError, undefined);
});

test('editing the id redraws from the same folder', () => {
  const wrap = Pickers.partControl({
    column: equipColumn, values: { graphic_equip: '999', item_slot: 'Helmet' }, ctx: pctx(),
    spec: SPEC, tintColumns: null,
  });
  const { input, status } = pparts(wrap);
  assert.equal(status.textContent, 'no art for Helms graphic 999');

  input.value = '5';
  fire(input, 'input');
  assert.equal(status.textContent, '');
  assert.equal(drawnFrom(wrap), 10);
});

test('changing item_slot moves the preview into the new folder, live', () => {
  // THE OTHER HALF OF THE COMPLAINT: item_slot is a <select> in another field, so without
  // onFormChange the preview keeps showing a helmet after the item became a pair of boots.
  const c = pctx();
  const wrap = Pickers.partControl({
    column: equipColumn, values: { graphic_equip: '5', item_slot: 'Helmet' }, ctx: c, spec: SPEC,
    tintColumns: null,
  });
  assert.equal(drawnFrom(wrap), 10, 'Helms');

  c.emit({ graphic_equip: '5', item_slot: 'Shoes' });
  assert.equal(drawnFrom(wrap), 40, 'Feet');

  c.emit({ graphic_equip: '5', item_slot: 'Ring' });
  assert.equal(pparts(wrap).status.textContent, 'this slot is not drawn on the character');
});

test('partControl subscribes even when untinted — the folder alone is cross-field', () => {
  const c = pctx();
  Pickers.partControl({ column: equipColumn, values: {}, ctx: c, spec: SPEC, tintColumns: null });
  assert.equal(c.__changed.length, 1);
});

test('body_state picks the clip, live, by the same rule as an equip slot', () => {
  // Preview.isArmed: 3 is unarmed, anything else armed. Both clips exist for this id, so the flag
  // is the only thing that decides which rect is drawn.
  const rects = { 'Hands:5:idle-equip-down': [70, 0, 8, 8],
                  'Hands:5:idle-no-equip-down': [80, 0, 8, 8] };
  const c = pctx({ bundles: { parts: { rects } } });
  const wrap = Pickers.partControl({
    column: equipColumn, values: { graphic_equip: '5', item_slot: 'OneHanded', body_state: 1 },
    ctx: c, spec: SPEC, tintColumns: null,
  });
  assert.equal(drawnFrom(wrap), 70, 'body_state 1 is armed');

  c.emit({ graphic_equip: '5', item_slot: 'OneHanded', body_state: 3 });
  assert.equal(drawnFrom(wrap), 80, 'body_state 3 is unarmed');
});

test('the worn preview is tinted by the same four columns as the tile', () => {
  const c = pctx();
  const wrap = Pickers.partControl({
    column: equipColumn,
    values: { graphic_equip: '5', item_slot: 'Helmet',
              graphic_r: 255, graphic_g: 0, graphic_b: 0, graphic_a: 255 },
    ctx: c, spec: SPEC, tintColumns: TINT_COLUMNS,
  });
  assert.equal(tintedDraws(pparts(wrap).canvas).length, 1);

  c.emit({ graphic_equip: '5', item_slot: 'Helmet', graphic_a: 0 });
  assert.equal(tintedDraws(pparts(wrap).canvas).length, 1, 'no second tinted draw');
  assert.equal(plainDraws(pparts(wrap).canvas).length, 1);
});

test('partControl survives a ctx with no bundles, images or hooks', () => {
  // Same contract as graphicControl's: a missing bundle leaves the form usable without art.
  const wrap = Pickers.partControl({
    column: equipColumn, values: { graphic_equip: '5', item_slot: 'Helmet' }, ctx: {}, spec: SPEC,
    tintColumns: TINT_COLUMNS,
  });
  assert.equal(pparts(wrap).status.textContent,
    'cannot check Helms graphic 5 — no character art loaded');
});

test('partControl redraws when the parts bundle finishes decoding', () => {
  // The build-time draw happens before the multi-megabyte PNG has decoded, so without this hook
  // the preview stays blank until the record is reopened.
  const ready = [];
  const images = {};
  const wrap = Pickers.partControl({
    column: equipColumn, values: { graphic_equip: '5', item_slot: 'Helmet' },
    ctx: pctx({ images, onImagesReady(fn) { ready.push(fn); } }), spec: SPEC, tintColumns: null,
  });
  // Sprites.draw is a no-op with no image, so nothing has landed on the canvas yet.
  assert.equal(drawnFrom(wrap), null);
  assert.equal(ready.length, 1);

  images.parts = 'PARTS';
  ready[0]();
  assert.equal(drawnFrom(wrap), 10);
});

// --- partControl over a FIXED category (body_id, hair_id, face_id) ---------------------------
//
// The other shape of Layout.partGraphic's spec: the folder is a fact about the COLUMN, not about
// another cell. body_id is always a body, hair_id always hair, face_id always Eyes.

const bodyColumn = { name: 'body_id', kind: 'Int', sql: 'SMALLINT', required: false, default: '1' };
const hairColumn = { name: 'hair_id', kind: 'Int', sql: 'SMALLINT', required: false, default: '0' };
const faceColumn = { name: 'face_id', kind: 'Int', sql: 'SMALLINT', required: false, default: '0' };

// One rect per appearance folder, each with its own source x so the drawn sprite says which folder
// the control resolved.
const appearanceRects = {
  'Bodies:5:idle-down': [100, 0, 24, 32],
  'Hair:5:idle-down': [110, 0, 24, 32],
  'Eyes:5:idle-down': [120, 0, 24, 32],
};

function actx(extra) {
  return pctx({ bundles: { parts: { rects: appearanceRects } }, ...extra });
}

function aparts(wrap, name) {
  return {
    canvas: wrap.querySelector('[class="preview"]'),
    input: wrap.querySelector(`[name="${name}"]`),
    status: wrap.querySelectorAll('[class]').filter((n) => /^status/.test(n.className))[0],
  };
}

test('each appearance id resolves to its own sprite folder', () => {
  const from = (column, spec) => {
    const wrap = Pickers.partControl({
      column, values: { [column.name]: '5' }, ctx: actx(), spec, tintColumns: null,
    });
    const drew = aparts(wrap, column.name).canvas.getContext('2d').calls
      .filter((c) => c[0] === 'drawImage');
    return drew.length ? drew[drew.length - 1][2] : null;
  };

  assert.equal(from(bodyColumn, { category: 'Bodies' }), 100);
  assert.equal(from(hairColumn, { category: 'Hair' }), 110);
  // A FACE id draws from Eyes — the folder whose name does not follow from the column's.
  assert.equal(from(faceColumn, { category: 'Eyes' }), 120);
});

test('a fixed-category control renders exactly its own cell, and reports no problem', () => {
  const wrap = Pickers.partControl({
    column: bodyColumn, values: { body_id: '5' }, ctx: actx(), spec: { category: 'Bodies' },
    tintColumns: null,
  });
  const { input, status } = aparts(wrap, 'body_id');
  assert.equal(input.value, '5');
  assert.equal(input.getAttribute('id'), 'f-body_id');
  assert.equal(wrap.querySelectorAll('[name]').length, 1);
  assert.equal(status.textContent, '');
});

test('a fixed category is never "not drawn on the character" — there is no slot to read', () => {
  // The undrawn-slot message belongs to graphic_equip, whose folder comes from item_slot. A body is
  // always drawn, so an item_slot in the form (or the absence of one) may not reach this control.
  ['Ring', '', undefined, 'Nonsense'].forEach((slot) => {
    const wrap = Pickers.partControl({
      column: bodyColumn, values: { body_id: '5', item_slot: slot }, ctx: actx(),
      spec: { category: 'Bodies' }, tintColumns: null,
    });
    assert.equal(aparts(wrap, 'body_id').status.textContent, '', String(slot));
  });
});

test('a fixed-category miss names the folder it looked in', () => {
  const wrap = Pickers.partControl({
    column: hairColumn, values: { hair_id: '999' }, ctx: actx(), spec: { category: 'Hair' },
    tintColumns: null,
  });
  assert.equal(aparts(wrap, 'hair_id').status.textContent, 'no art for Hair graphic 999');
  assert.equal(aparts(wrap, 'hair_id').status.className, 'status bad');
  // Reported, not gated — the same rule the equip-graphic control follows.
  assert.equal(wrap.__graphicError, undefined);
});

test('a blank or zero appearance id is "no graphic", not an error', () => {
  ['', '0', ' '].forEach((value) => {
    const wrap = Pickers.partControl({
      column: faceColumn, values: { face_id: value }, ctx: actx(), spec: { category: 'Eyes' },
      tintColumns: null,
    });
    assert.equal(aparts(wrap, 'face_id').status.textContent, 'no graphic', JSON.stringify(value));
    assert.equal(aparts(wrap, 'face_id').status.className, 'status');
  });
});

test('a body preview is tinted by the same four cells the character panel reads', () => {
  const c = actx();
  const wrap = Pickers.partControl({
    column: bodyColumn,
    values: { body_id: '5', body_r: 255, body_g: 0, body_b: 0, body_a: 255 },
    ctx: c, spec: { category: 'Bodies' },
    tintColumns: ['body_r', 'body_g', 'body_b', 'body_a'],
  });
  assert.equal(tintedDraws(aparts(wrap, 'body_id').canvas).length, 1);

  // And it follows the tint live: the colour picker writes those cells from another control.
  c.emit({ body_id: '5', body_a: 0 });
  assert.equal(plainDraws(aparts(wrap, 'body_id').canvas).length, 1);
});

test('a fixed-category control still follows body_state, which is another field', () => {
  const rects = { 'Bodies:5:idle-equip-down': [70, 0, 8, 8],
                  'Bodies:5:idle-no-equip-down': [80, 0, 8, 8] };
  const c = pctx({ bundles: { parts: { rects } } });
  const wrap = Pickers.partControl({
    column: bodyColumn, values: { body_id: '5', body_state: 1 }, ctx: c,
    spec: { category: 'Bodies' }, tintColumns: null,
  });
  const at = () => {
    const drew = aparts(wrap, 'body_id').canvas.getContext('2d').calls
      .filter((x) => x[0] === 'drawImage');
    return drew.length ? drew[drew.length - 1][2] : null;
  };

  assert.equal(at(), 70, 'body_state 1 is armed');
  c.emit({ body_id: '5', body_state: 3 });
  assert.equal(at(), 80, 'body_state 3 is unarmed');
});

// --- clicking a preview canvas opens the browser ----------------------------------------------
//
// A SPY GALLERY, not the real module: what these assert is the SEAM — that the control hands over
// the right bundle, filter and current graphic, and that what comes back reaches both cells and
// triggers a redraw. gallery.test.js owns whether the browser itself works.

function spyGallery() {
  const opens = [];
  return {
    opens,
    open(options) { opens.push(options); return { close() {} }; },
    // The last onPick handed over, so a test can pick without a DOM.
    pick(choice) { opens[opens.length - 1].onPick(choice); },
  };
}

// The clickable preview canvas — there is no separate Browse button; the picture opens the
// dialog and is the opener focus returns to, while the text field stays an ordinary field a
// designer can click into and retype.
function browseOf(wrap) {
  return wrap.querySelectorAll('[data-browse]')[0];
}

test('clicking the preview canvas opens the icons browser on the current sheet', () => {
  const gallery = spyGallery();
  const wrap = Pickers.graphicControl({
    graphicColumn, fileColumn, ctx: gctx(), gallery,
    values: { graphic_tile: '810003', graphic_file: '20107' },
  });

  const field = browseOf(wrap);
  assert.equal(field, gparts(wrap).canvas, 'the preview canvas is the opener');
  assert.notEqual(field, gparts(wrap).graphic,
    'the graphic field must stay an ordinary text box');
  assert.equal(field.getAttribute('aria-haspopup'), 'dialog');
  // A canvas is not natively interactive: without these a keyboard user cannot reach or
  // activate the browser at all.
  assert.equal(field.getAttribute('role'), 'button');
  assert.equal(field.getAttribute('tabindex'), '0');

  fire(field, 'click');
  assert.equal(gallery.opens.length, 1);
  const opened = gallery.opens[0];
  assert.equal(opened.bundle, 'icons');
  assert.equal(opened.bundles, bundles);
  assert.equal(opened.opener, field, 'focus has to have somewhere to return to');
  // The sheet the record already names, so the browser opens on this item's neighbourhood.
  assert.deepEqual(opened.filter, { sheet: '20107' });
  assert.deepEqual(opened.current, { sheet: '20107', graphic: '810003' });
});

test('Enter and Space on the preview canvas open the browser too', () => {
  // role=button promises button behaviour, and a canvas gets none of it natively: without this
  // path a keyboard user can Tab to the canvas and then do nothing with it.
  const gallery = spyGallery();
  const wrap = Pickers.graphicControl({
    graphicColumn, fileColumn, ctx: gctx(), gallery, values: {},
  });

  // Prevented, so Space does not also scroll the page — the suppression a real button gets free.
  assert.equal(fire(browseOf(wrap), 'keydown', { key: 'Enter' }), false);
  assert.equal(fire(browseOf(wrap), 'keydown', { key: ' ' }), false);
  assert.equal(gallery.opens.length, 2);

  fire(browseOf(wrap), 'keydown', { key: 'a' });
  assert.equal(gallery.opens.length, 2, 'an ordinary key must not open anything');
});

test('the sheet the browser opens on follows the field, not the stored record', () => {
  const gallery = spyGallery();
  const wrap = Pickers.graphicControl({
    graphicColumn, fileColumn, ctx: gctx(), gallery,
    values: { graphic_tile: '810003', graphic_file: '20107' },
  });

  gparts(wrap).file.value = '999';
  fire(browseOf(wrap), 'click');
  assert.deepEqual(gallery.opens[0].filter, { sheet: '999' });
});

test('an icon pick writes BOTH cells and triggers a redraw', () => {
  const gallery = spyGallery();
  const wrap = Pickers.graphicControl({
    graphicColumn, fileColumn, ctx: gctx(), gallery, values: {},
  });
  const { graphic, file, canvas, status } = gparts(wrap);
  // Half a pair is legal but does not resolve, so the control starts out complaining.
  assert.equal(status.textContent, 'no graphic');
  const before = canvas.getContext('2d').calls.length;

  fire(browseOf(wrap), 'click');
  gallery.pick({ sheet: '20107', graphic: '810003' });

  assert.equal(graphic.value, '810003');
  assert.equal(file.value, '20107');
  // Both cells at once is what makes "graphic and sheet must both be set" hard to trip.
  assert.equal(status.textContent, '');
  assert.ok(canvas.getContext('2d').calls.length > before, 'the preview did not redraw');
});

test('an icon pick bubbles an input event, so the panel previews follow too', () => {
  const gallery = spyGallery();
  const wrap = Pickers.graphicControl({
    graphicColumn, fileColumn, ctx: gctx(), gallery, values: {},
  });
  // app.js listens on the form container, above the control.
  const container = document.createElement('div');
  container.appendChild(wrap);
  let seen = 0;
  container.addEventListener('input', () => { seen++; });

  fire(browseOf(wrap), 'click');
  gallery.pick({ sheet: '20107', graphic: '810003' });
  assert.equal(seen, 1);
});

test('a graphic column over the effects bundle browses effects and writes only the id', () => {
  const gallery = spyGallery();
  const wrap = Pickers.graphicControl({
    graphicColumn, fileColumn, ctx: gctx(), gallery, galleryBundle: 'effects',
    values: { graphic_tile: '9', graphic_file: '0' },
  });

  fire(browseOf(wrap), 'click');
  assert.equal(gallery.opens[0].bundle, 'effects');
  assert.deepEqual(gallery.opens[0].current, { id: '9' });

  gallery.pick({ id: '44' });
  assert.equal(gparts(wrap).graphic.value, '44');
  // 176 of the 259 shipped spell_animation rows store 0 in the file cell and the server sends both
  // through verbatim, so filling it in would be inventing data.
  assert.equal(gparts(wrap).file.value, '0');
});

// An effects ctx: two frames of effect 44, so frame ORDER is observable and picking the wrong
// frame cannot pass. Same odd non-square dimensions as the icons fixture, for the same reason.
function ectx(values) {
  return {
    bundles: { effects: { rects: { '44:0': [8, 0, 31, 17], '44:1': [40, 0, 20, 20] } },
               icons: bundles.icons },
    images: { effects: 'EFFECTS', icons: 'ICONS' },
    onImagesReady() {},
    ...values,
  };
}

test('an effects column resolves its preview in the effects atlas, not the icons one', () => {
  const wrap = Pickers.graphicControl({
    graphicColumn, fileColumn, ctx: ectx(), galleryBundle: 'effects',
    values: { graphic_tile: '44', graphic_file: '0' },
  });
  const { canvas, status } = gparts(wrap);

  // Frame 0, drawn from the EFFECTS image — the resting frame, as the gallery tiles show.
  assert.deepEqual(canvas.getContext('2d').calls.filter((c) => c[0] === 'drawImage'),
    [['drawImage', 'EFFECTS', 8, 0, 31, 17, 16, 23, 31, 17]]);
  assert.equal(status.textContent, '');
});

test('an effects column ignores the file cell, which is not part of the lookup', () => {
  // 181 of the 183 shipped rows that set spell_animation also set spell_animation_file, and
  // Sprites.effectFrames takes the id alone. Resolving the pair as a sheet:graphic is what used
  // to blank the preview and block the save on 179 of them.
  const wrap = Pickers.graphicControl({
    graphicColumn, fileColumn, ctx: ectx(), galleryBundle: 'effects',
    values: { graphic_tile: '44', graphic_file: '4386' },
  });
  const { canvas, status } = gparts(wrap);

  assert.deepEqual(canvas.getContext('2d').calls.filter((c) => c[0] === 'drawImage'),
    [['drawImage', 'EFFECTS', 8, 0, 31, 17, 16, 23, 31, 17]]);
  assert.equal(status.textContent, '');
  assert.equal(wrap.__graphicError, null, 'a resolvable effect must not block the save');
});

test('an effects column never blocks the save, even on an effect the bundle lacks', () => {
  // 13 shipped rows name an effect the committed bundle does not have. Blocking would lock them.
  const wrap = Pickers.graphicControl({
    graphicColumn, fileColumn, ctx: ectx(), galleryBundle: 'effects',
    values: { graphic_tile: '286986', graphic_file: '4386' },
  });
  const { canvas, status } = gparts(wrap);

  assert.equal(status.textContent, 'no art for effect 286986');
  assert.equal(wrap.__graphicError, null);
  assert.deepEqual(canvas.getContext('2d').calls.filter((c) => c[0] === 'drawImage'), []);
});

test('an effects column says nothing about a sheet, which it has no use for', () => {
  const blank = Pickers.graphicControl({
    graphicColumn, fileColumn, ctx: ectx(), galleryBundle: 'effects',
    values: { graphic_tile: '0', graphic_file: '0' },
  });
  assert.equal(gparts(blank).status.textContent, 'no effect');

  // The icons path would demand the sheet cell here. An effect has no pair to be half of.
  const set = Pickers.graphicControl({
    graphicColumn, fileColumn, ctx: ectx(), galleryBundle: 'effects',
    values: { graphic_tile: '44', graphic_file: '' },
  });
  assert.equal(gparts(set).status.textContent, '');
});

test('an effects pick redraws the preview, not only the cell', () => {
  const gallery = spyGallery();
  const wrap = Pickers.graphicControl({
    graphicColumn, fileColumn, ctx: ectx(), gallery, galleryBundle: 'effects',
    values: { graphic_tile: '0', graphic_file: '0' },
  });
  const ctx2d = gparts(wrap).canvas.getContext('2d');
  assert.deepEqual(ctx2d.calls.filter((c) => c[0] === 'drawImage'), []);

  fire(browseOf(wrap), 'click');
  gallery.pick({ id: '44' });

  assert.deepEqual(ctx2d.calls.filter((c) => c[0] === 'drawImage'),
    [['drawImage', 'EFFECTS', 8, 0, 31, 17, 16, 23, 31, 17]]);
});

test('an effects column with no effect art loaded reports it without blocking', () => {
  const wrap = Pickers.graphicControl({
    graphicColumn, fileColumn, galleryBundle: 'effects',
    ctx: ectx({ bundles: { icons: bundles.icons } }),
    values: { graphic_tile: '44', graphic_file: '0' },
  });
  assert.equal(gparts(wrap).status.textContent,
    'cannot check effect 44 — no effect art loaded');
  assert.equal(wrap.__graphicError, null);
});

test('graphicControl without a gallery leaves the click doing nothing rather than throwing', () => {
  const saved = globalThis.Gallery;
  delete globalThis.Gallery;
  try {
    const wrap = Pickers.graphicControl({ graphicColumn, fileColumn, ctx: gctx(), values: {} });
    fire(browseOf(wrap), 'click');
  } finally {
    if (saved !== undefined) globalThis.Gallery = saved;
  }
});

test('partControl browses the parts bundle locked to the slot the row names', () => {
  const gallery = spyGallery();
  const wrap = Pickers.partControl({
    column: equipColumn, values: { graphic_equip: '5', item_slot: 'Shoes' }, ctx: pctx(),
    spec: SPEC, tintColumns: null, gallery,
  });

  fire(browseOf(wrap), 'click');
  const opened = gallery.opens[0];
  assert.equal(opened.bundle, 'parts');
  // Shoes -> Feet -> the Feet folder, through the client's own map.
  assert.deepEqual(opened.filter, { category: 'Feet', locked: true });
  assert.deepEqual(opened.current, { category: 'Feet', id: '5' });

  gallery.pick({ category: 'Feet', id: '77' });
  assert.equal(pparts(wrap).input.value, '77');
});

test('the locked category follows a live edit of item_slot', () => {
  const gallery = spyGallery();
  const ctx = pctx();
  const wrap = Pickers.partControl({
    column: equipColumn, values: { graphic_equip: '5', item_slot: 'Shoes' }, ctx,
    spec: SPEC, tintColumns: null, gallery,
  });

  ctx.emit({ graphic_equip: '5', item_slot: 'Helmet' });
  fire(browseOf(wrap), 'click');
  assert.deepEqual(gallery.opens[0].filter, { category: 'Helms', locked: true });
});

test('a slot the character never draws has nothing to browse', () => {
  const gallery = spyGallery();
  const wrap = Pickers.partControl({
    column: equipColumn, values: { graphic_equip: '5', item_slot: 'Ring' }, ctx: pctx(),
    spec: SPEC, tintColumns: null, gallery,
  });

  // The field stays an ordinary text field — typing an id must still work — so the click
  // handler's guard is the whole protection.
  fire(browseOf(wrap), 'click');
  assert.equal(gallery.opens.length, 0);
});

test('an appearance id browses its own fixed folder, locked, and takes the pick', () => {
  // The picker the NPC form was missing. Locked for the same reason an equip slot is: a Hair sprite
  // in body_id is a sprite the client would never draw there.
  const cases = [
    [bodyColumn, { category: 'Bodies' }, 'Bodies'],
    [hairColumn, { category: 'Hair' }, 'Hair'],
    [faceColumn, { category: 'Eyes' }, 'Eyes'],
  ];

  cases.forEach(([column, spec, folder]) => {
    const gallery = spyGallery();
    const wrap = Pickers.partControl({
      column, values: { [column.name]: '5' }, ctx: actx(), spec, tintColumns: null, gallery,
    });

    const opener = browseOf(wrap);
    assert.equal(opener, aparts(wrap, column.name).canvas, 'the preview canvas is the opener');
    fire(opener, 'click');

    const opened = gallery.opens[0];
    assert.equal(opened.bundle, 'parts', folder);
    assert.deepEqual(opened.filter, { category: folder, locked: true });
    assert.deepEqual(opened.current, { category: folder, id: '5' });

    gallery.pick({ category: folder, id: '77' });
    assert.equal(aparts(wrap, column.name).input.value, '77');
  });
});

test('an appearance pick bubbles an input event, so the character panel follows', () => {
  const gallery = spyGallery();
  const wrap = Pickers.partControl({
    column: bodyColumn, values: { body_id: '5' }, ctx: actx(), spec: { category: 'Bodies' },
    tintColumns: null, gallery,
  });
  const container = document.createElement('div');
  container.appendChild(wrap);
  let seen = 0;
  container.addEventListener('input', () => { seen++; });

  fire(browseOf(wrap), 'click');
  gallery.pick({ category: 'Bodies', id: '77' });
  assert.equal(seen, 1);
});

test('a Mount has nothing to browse either, because the gallery indexes no mounted clips', () => {
  // Sprites.part deliberately never falls back to a mounted clip, so the Bodies folder would offer
  // 305 standing bodies of which four have the pose this cell needs.
  const gallery = spyGallery();
  const wrap = Pickers.partControl({
    column: equipColumn, values: { graphic_equip: '5', item_slot: 'Mount' }, ctx: pctx(),
    spec: SPEC, tintColumns: null, gallery,
  });

  fire(browseOf(wrap), 'click');
  assert.equal(gallery.opens.length, 0);
});
