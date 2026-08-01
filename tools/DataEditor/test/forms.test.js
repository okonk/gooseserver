import { test } from 'node:test';
import assert from 'node:assert/strict';
import { createContext, runInContext } from 'node:vm';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { installFakeDom, fire } from './fake-dom.js';

installFakeDom();

const { Layout } = await import('../src/layout.js');
globalThis.Layout = Layout;

// Composites is Task 10 and does not exist yet. forms.js resolves it as a free global at CALL
// time, exactly as the built page does (Editor.html includes composites.html first), so a stub
// installed here is enough — and every stub call is recorded so the contract can be asserted.
const compositeCalls = { control: [], collect: [] };
globalThis.Composites = {
  control({ comp, byName, values, effective, ctx, sheet }) {
    compositeCalls.control.push({ comp, byName, values, effective, ctx, sheet });
    const node = document.createElement('div');
    node.setAttribute('data-composite', comp.kind);
    comp.columns.forEach((name) => {
      const input = document.createElement('input');
      input.setAttribute('name', name);
      input.value = values[name] === undefined ? '' : String(values[name]);
      node.appendChild(input);
    });
    return node;
  },
  collect(comp, container) {
    compositeCalls.collect.push({ comp, container });
    const out = {};
    comp.columns.forEach((name) => {
      const found = container.querySelector('[name="' + name + '"]');
      out[name] = found ? found.value : '';
    });
    return out;
  },
};

const { Forms } = await import('../src/forms.js');

const schemaPath = fileURLToPath(new URL('../schema.js', import.meta.url));
const schemaContext = createContext({});
runInContext(readFileSync(schemaPath, 'utf8'), schemaContext);
const SCHEMA = schemaContext.GOOSE_SCHEMA;

function sheet(name) {
  const s = SCHEMA.sheets.find((x) => x.sheet === name);
  assert.ok(s, `schema.js has no sheet named ${JSON.stringify(name)}`);
  return s;
}

function column(sheetName, columnName) {
  const c = sheet(sheetName).columns.find((x) => x.name === columnName);
  assert.ok(c, `${sheetName} has no column ${columnName}`);
  return c;
}

const div = () => document.createElement('div');
const labels = (node) => node.getElementsByTagName('label').map((l) => l.textContent);
const named = (node) => node.querySelectorAll('[name]').map((n) => n.getAttribute('name'));

// ---------------------------------------------------------------- el

test('el sets attributes and text, and tolerates null attrs', () => {
  const node = Forms.el('div', { class: 'warn', 'data-error-for': 'x' }, 'boom');
  assert.equal(node.tagName, 'DIV');
  assert.equal(node.getAttribute('class'), 'warn');
  assert.equal(node.getAttribute('data-error-for'), 'x');
  assert.equal(node.textContent, 'boom');

  const bare = Forms.el('h3', null, 'Identity');
  assert.equal(bare.textContent, 'Identity');
  assert.equal(bare.getAttribute('class'), null);

  // No text argument must leave the node empty rather than writing "undefined".
  assert.equal(Forms.el('div').textContent, '');
});

// ---------------------------------------------------------------- placeholderFor

test('placeholderFor: required beats any default', () => {
  assert.equal(Forms.placeholderFor(column('Items', 'item_template_id')), 'required');
  assert.equal(Forms.placeholderFor({ name: 'x', required: true, default: '7' }), 'required');
});

test('placeholderFor accepts a default that is not already a string', () => {
  assert.equal(Forms.placeholderFor({ required: false, default: 0 }), 'default 0');
});

test('placeholderFor: no default gives no placeholder', () => {
  assert.equal(Forms.placeholderFor({ name: 'x', required: false }), '');
  assert.equal(Forms.placeholderFor({ name: 'x', required: false, default: null }), '');
});

test('placeholderFor shows a bare numeric default as-is', () => {
  assert.equal(Forms.placeholderFor(column('Items', 'player_hp')), 'default 0');
  assert.equal(Forms.placeholderFor({ required: false, default: '100' }), 'default 100');
});

test("placeholderFor unwraps a quoted SQL string default", () => {
  assert.equal(Forms.placeholderFor({ required: false, default: "'0'" }), 'default 0');
  assert.equal(Forms.placeholderFor({ required: false, default: "'Scripts/NPC/BaseNPC.csx'" }),
    'default Scripts/NPC/BaseNPC.csx');
});

test("placeholderFor names the empty-string default rather than trailing off", () => {
  // item_description's descriptor default is the SQL literal '' — an empty string, not "unset".
  assert.equal(column('Items', 'item_description').default, "''");
  assert.equal(Forms.placeholderFor(column('Items', 'item_description')), 'default (blank)');
});

test('placeholderFor strips quotes only as a matched pair', () => {
  // No column in today's schema.js has an unpaired quote in its default, so this is guarding
  // the rule rather than a live defect: an unpaired quote is part of the value, and stripping
  // one end of it would advertise a default the database does not have.
  assert.equal(Forms.placeholderFor({ required: false, default: "it's" }), "default it's");
  assert.equal(Forms.placeholderFor({ required: false, default: "'unterminated" }),
    "default 'unterminated");
  assert.equal(Forms.placeholderFor({ required: false, default: "'it''s'" }),
    "default it''s");
  // Anchored at both ends: quotes in the MIDDLE are content, not delimiters.
  assert.equal(Forms.placeholderFor({ required: false, default: "a'b'" }), "default a'b'");
  assert.equal(Forms.placeholderFor({ required: false, default: "'a'b" }), "default 'a'b");
});

test('placeholderFor covers every default in the real schema without producing junk', () => {
  let checked = 0;
  [...SCHEMA.sheets].forEach((s) => [...s.columns].forEach((c) => {
    const text = Forms.placeholderFor(c);
    if (c.required) { assert.equal(text, 'required'); return; }
    if (c.default === undefined) { assert.equal(text, ''); return; }
    checked++;
    assert.ok(text.length > 'default '.length, `${c.name}: ${JSON.stringify(text)}`);
    assert.ok(!/'$/.test(text), `${c.name} kept a trailing quote: ${text}`);
  }));
  assert.ok(checked > 100, `expected many defaulted columns, saw ${checked}`);
});

// ---------------------------------------------------------------- defaultOf / effective

test('defaultOf gives the default as a cell would hold it', () => {
  assert.equal(Forms.defaultOf({ default: '3' }), '3');
  assert.equal(Forms.defaultOf({ default: 0 }), '0');
  assert.equal(Forms.defaultOf({ default: "'0,*,0,*'" }), '0,*,0,*');
  assert.equal(Forms.defaultOf({ default: "''" }), '');
  // No default at all, and no descriptor at all, are both "nothing to fall back to" — a caller
  // holding a subset of the schema (Composites.control) asks about columns it may not have.
  assert.equal(Forms.defaultOf({ name: 'x' }), '');
  assert.equal(Forms.defaultOf({ name: 'x', default: null }), '');
  assert.equal(Forms.defaultOf(undefined), '');
  // Same matched-pair rule placeholderFor states, because it is now the same code.
  assert.equal(Forms.defaultOf({ default: "'unterminated" }), "'unterminated");
});

test('effective replaces a blank cell with the column default and leaves the rest alone', () => {
  const columns = [
    { name: 'body_state', kind: 'Int', default: '3' },
    { name: 'body_id', kind: 'Int', default: '1' },
    { name: 'hair_id', kind: 'Int', default: '0' },
    { name: 'script_path', kind: 'String', default: "'Scripts/NPC/BaseNPC.csx'" },
    { name: 'npc_id', kind: 'Int', required: true },
  ];
  const out = Forms.effective({ body_state: '', body_id: '11', hair_id: '', npc_id: '' }, columns);
  assert.equal(out.body_state, '3', 'the reported bug: a blank pose is the unarmed default');
  assert.equal(out.body_id, '11', 'a stored value is never overwritten');
  assert.equal(out.hair_id, '0');
  assert.equal(out.script_path, 'Scripts/NPC/BaseNPC.csx');
  assert.equal(out.npc_id, '', 'a column with no default has nothing to fall back to');
});

test('effective coerces like collect: a numeric 0 is a stored value, not a blank', () => {
  const columns = [{ name: 'body_state', kind: 'Int', default: '3' }];
  assert.equal(Forms.effective({ body_state: 0 }, columns).body_state, '0');
  assert.equal(Forms.effective({}, columns).body_state, '3');
  assert.equal(Forms.effective(undefined, columns).body_state, '3');
});

test('effective leaves an Enum blank — the default is in the wrong value space', () => {
  // item_slot cells hold the enum NAME ('Helmet'); the SQL default is the C# enum's number (20),
  // and enumNames carries no numbering to map one to the other. Substituting it would hand a
  // reader '20' where a slot name belongs.
  const c = column('Items', 'item_slot');
  assert.equal(c.kind, 'Enum');
  assert.equal(c.default, '20');
  assert.equal(Forms.effective({ item_slot: '' }, [c]).item_slot, '');
  assert.equal(Forms.effective({ item_slot: 'Helmet' }, [c]).item_slot, 'Helmet');
});

test('effective carries through a cell no column names, and is idempotent', () => {
  const columns = [{ name: 'body_state', kind: 'Int', default: '3' }];
  const once = Forms.effective({ body_state: '', npc_name: 'Rat' }, columns);
  assert.equal(once.npc_name, 'Rat');
  assert.deepEqual(Forms.effective(once, columns), once);
});

test('effective over a real record leaves no blank that the importer would fill', () => {
  const npcs = sheet('NPCs');
  const blank = {};
  npcs.columns.forEach((c) => { blank[c.name] = ''; });
  const out = Forms.effective(blank, npcs.columns);
  npcs.columns.forEach((c) => {
    if (c.default === undefined || c.default === null || c.kind === 'Enum') {
      assert.equal(out[c.name], '', c.name);
    } else {
      assert.equal(out[c.name], Forms.defaultOf(c), c.name);
    }
  });
  // The two the previews turn on, spelled out: an unarmed pose and a player body.
  assert.equal(out.body_state, '3');
  assert.equal(out.body_id, '1');
  assert.equal(out.equipped_items, '0,*,0,*,0,*,0,*,0,*,0,*');
});

// ---------------------------------------------------------------- scalarControl

test('Enum renders a select with a blank option when optional', () => {
  const c = column('NPCs', 'npc_type');
  assert.equal(c.kind, 'Enum');
  const control = Forms.scalarControl({ ...c, required: false }, c.enumNames[1]);
  assert.equal(control.tagName, 'SELECT');
  assert.equal(control.getAttribute('name'), c.name);
  const options = control.getElementsByTagName('option').map((o) => o.value);
  assert.deepEqual(options, ['', ...c.enumNames]);
  assert.equal(control.value, c.enumNames[1]);
});

test('a required Enum has no blank option', () => {
  const c = column('Items', 'item_usetype');
  assert.equal(c.required, true);
  const control = Forms.scalarControl(c, 'Weapon');
  assert.notEqual(c.enumNames[0], 'Weapon');
  const options = control.getElementsByTagName('option').map((o) => o.value);
  assert.deepEqual(options, [...c.enumNames]);
  assert.equal(control.value, 'Weapon');
});

test('the FIRST enum name is recognised, not duplicated as unknown', () => {
  const c = column('Items', 'item_usetype');
  const control = Forms.scalarControl(c, c.enumNames[0]);
  assert.deepEqual(control.getElementsByTagName('option').map((o) => o.value),
    [...c.enumNames]);
  assert.equal(control.value, c.enumNames[0]);
});

test('the LAST enum name is recognised, not duplicated as unknown', () => {
  const c = column('Items', 'item_usetype');
  const last = c.enumNames[c.enumNames.length - 1];
  const control = Forms.scalarControl(c, last);
  assert.deepEqual(control.getElementsByTagName('option').map((o) => o.value),
    [...c.enumNames]);
  assert.equal(control.value, last);
});

test('an Enum value that is not in enumNames survives instead of blanking', () => {
  // A browser select silently shows nothing for a value it has no option for, and then reads
  // back as '' — so a renamed enum member would be saved as blank, destroying the cell.
  const c = column('Items', 'item_usetype');
  assert.equal(c.enumNames.indexOf('Antique'), -1);
  const control = Forms.scalarControl(c, 'Antique');
  assert.equal(control.value, 'Antique');
  const last = control.getElementsByTagName('option').pop();
  assert.equal(last.value, 'Antique');
  assert.match(last.textContent, /not a valid value/);
});

test('the fake select really does refuse an unlisted value', () => {
  // Guards the test above: if this assertion ever passes trivially, the fake DOM has stopped
  // reproducing the hazard and the data-loss test is worthless.
  const select = document.createElement('select');
  const option = document.createElement('option');
  option.setAttribute('value', 'Armor');
  select.appendChild(option);
  select.value = 'Antique';
  assert.equal(select.value, '');
});

test('a blank value never invents an option', () => {
  // A required Enum has no blank option by design; manufacturing one for a blank cell would
  // let "nothing chosen" be saved as a legitimate-looking choice.
  const c = column('Items', 'item_usetype');
  const control = Forms.scalarControl(c, '');
  assert.deepEqual(control.getElementsByTagName('option').map((o) => o.value),
    [...c.enumNames]);
  assert.equal(control.value, '');
});

test('the unknown-value option is in place BEFORE the value is assigned', () => {
  // Appending it afterwards would not help: a select with nothing selected snaps to its first
  // option when an option is added, so the stored value would still be lost.
  const select = document.createElement('select');
  const known = document.createElement('option');
  known.setAttribute('value', 'Armor');
  select.appendChild(known);
  select.value = 'Antique';
  const late = document.createElement('option');
  late.setAttribute('value', 'Antique');
  select.appendChild(late);
  assert.equal(select.value, 'Armor');
});

test('an Enum with no enumNames still renders', () => {
  const control = Forms.scalarControl({ name: 'x', kind: 'Enum', required: false }, '');
  assert.deepEqual(control.getElementsByTagName('option').map((o) => o.value), ['']);
  assert.equal(control.value, '');
});

// A Bool is a TRI-STATE checkbox over a hidden cell: blank means "use the SQL default", and a
// two-state box would write 0 into every blank boolean on every save.
const boolBox = (node) => node.querySelector('[type="checkbox"]');
const boolCell = (node) => node.querySelector('[type="hidden"]');
const boolClear = (node) => node.querySelector('[class="clear"]');

// What Forms.collect would read back off this control, with no interaction in between.
function collectOne(c, control) {
  const host = div();
  host.appendChild(control);
  return Forms.collect(host, { columns: [c] })[c.name];
}

test('Bool renders a tri-state checkbox over a hidden cell', () => {
  const c = column('Items', 'lore');
  assert.equal(c.kind, 'Bool');

  const blank = Forms.scalarControl(c, '');
  assert.equal(blank.tagName, 'SPAN');
  assert.equal(blank.getAttribute('class'), 'boolean');
  assert.equal(boolBox(blank).indeterminate, true);
  assert.equal(boolBox(blank).checked, false);
  // The checkbox carries the id the field's <label for> points at — and NO name, so
  // Forms.collect's [name] sweep reads the hidden cell rather than the box's 'on'.
  assert.equal(boolBox(blank).getAttribute('id'), 'f-lore');
  assert.equal(boolBox(blank).getAttribute('name'), null);
  assert.equal(boolCell(blank).getAttribute('name'), 'lore');

  const no = Forms.scalarControl(c, '0');
  assert.equal(boolBox(no).indeterminate, false);
  assert.equal(boolBox(no).checked, false);

  const yes = Forms.scalarControl(c, '1');
  assert.equal(boolBox(yes).indeterminate, false);
  assert.equal(boolBox(yes).checked, true);
});

test('an untouched Bool collects back exactly what it was given', () => {
  const c = column('Items', 'lore');
  ['', '0', '1'].forEach((stored) => {
    assert.equal(collectOne(c, Forms.scalarControl(c, stored)), stored, stored);
  });
  // A NUMERIC 0 from the Sheets API is the same cell as '0', not a blank.
  assert.equal(collectOne(c, Forms.scalarControl(c, 0)), '0');
});

test('ticking a blank Bool writes 1, and unticking it writes 0', () => {
  const c = column('Items', 'lore');
  const control = Forms.scalarControl(c, '');
  const box = boolBox(control);

  // The fake DOM does not run a click's default action, so the test does what the browser
  // would: flip .checked, then fire the change the flip causes.
  box.checked = true;
  fire(box, 'change');
  assert.equal(box.indeterminate, false, 'the third state is gone once a decision is made');
  assert.equal(collectOne(c, control), '1');

  box.checked = false;
  fire(box, 'change');
  assert.equal(collectOne(c, control), '0');
});

test('the clear button hands a Bool back to the SQL default', () => {
  const c = column('Items', 'lore');
  const control = Forms.scalarControl(c, '1');
  const box = boolBox(control);

  // The preview must follow the clear, so the button dispatches a bubbling change of its own.
  let heard = 0;
  const host = div();
  host.appendChild(control);
  host.addEventListener('change', () => { heard++; });

  fire(boolClear(control), 'click');
  assert.equal(box.indeterminate, true);
  assert.equal(box.checked, false);
  assert.equal(Forms.collect(host, { columns: [c] })[c.name], '');
  assert.equal(heard, 1);
});

test('a required Bool has no clear button — blank is not a value it may hold', () => {
  const optional = column('Items', 'lore');
  assert.equal(optional.required, false);
  assert.ok(boolClear(Forms.scalarControl(optional, '')));

  const required = { name: 'x', kind: 'Bool', required: true };
  assert.equal(boolClear(Forms.scalarControl(required, '1')), null);
});

test('a NUMERIC 0 from the Sheets API is not mistaken for blank', () => {
  // Sheets hands back a JS number for a numeric cell. `value || ''` would turn 0 into '',
  // which means "use the default" on write — silently losing an explicit 0.
  assert.equal(boolCell(Forms.scalarControl(column('Items', 'lore'), 0)).value, '0');
  assert.equal(Forms.scalarControl(column('Items', 'player_hp'), 0).value, '0');
});

test('an out-of-range Bool falls back to a text input instead of being normalised', () => {
  // A checkbox has nowhere to put a 2. Coercing it to ticked would silently rewrite the cell;
  // this keeps it, flags it, and lets Validation report it.
  const c = column('Items', 'lore');
  const control = Forms.scalarControl(c, '2');
  assert.equal(boolBox(control), null);
  const raw = control.querySelector('[name="lore"]');
  assert.equal(raw.getAttribute('type'), 'text');
  assert.equal(raw.getAttribute('id'), 'f-lore');
  assert.equal(control.querySelector('[class="status bad"]').textContent, 'not a 0/1 value');
  assert.equal(collectOne(c, control), '2');
});

test('Text, Int, Id and Decimal all render a text input', () => {
  const cases = [
    ['Items', 'item_name', 'Text'],
    ['Items', 'player_hp', 'Int'],
    ['Items', 'item_template_id', 'Id'],
    ['Titles', 'chance', 'Decimal'],
  ];
  cases.forEach(([sheetName, columnName, kind]) => {
    const c = column(sheetName, columnName);
    assert.equal(c.kind, kind, `${columnName} kind`);
    const control = Forms.scalarControl(c, '12');
    assert.equal(control.tagName, 'INPUT', columnName);
    // Never type="number": it reads back '' for input it cannot parse, and '' means "use the
    // SQL default", so a typo would silently write the default instead of being reported.
    assert.equal(control.getAttribute('type'), 'text', columnName);
    assert.equal(control.getAttribute('autocomplete'), 'off');
    assert.equal(control.getAttribute('placeholder'), Forms.placeholderFor(c));
    assert.equal(control.value, '12');
  });
});

test('an input value is set live, not merely as an attribute', () => {
  // A clean input mirrors its value CONTENT ATTRIBUTE, so an attribute-only write would read
  // back correctly — right up until the user types, which raises the dirty value flag and
  // decouples the two. render() assigns .value and emits no value attribute at all, so the
  // two can never disagree about what is in the field.
  const control = Forms.scalarControl(column('Items', 'item_name'), 'Rusty Sword');
  assert.equal(control.value, 'Rusty Sword');
  assert.equal(control.getAttribute('value'), null);
});

test('the fake input models the dirty value flag', () => {
  // Guards Task 10, whose bitmask checkboxes are built as el('input', { value: id }) and read
  // back through .value: a fake that ignored the attribute would report '' for every one.
  const clean = document.createElement('input');
  clean.setAttribute('value', '7');
  assert.equal(clean.value, '7');

  clean.value = '9';               // raises the dirty flag
  clean.setAttribute('value', '7');
  assert.equal(clean.value, '9');  // the attribute no longer speaks for the field

  assert.equal(document.createElement('input').value, '');
});

test('a column absent from the values map renders blank, not "undefined"', () => {
  assert.equal(Forms.scalarControl(column('Items', 'item_name'), undefined).value, '');
  assert.equal(Forms.scalarControl(column('Items', 'item_name'), null).value, '');
});

// ---------------------------------------------------------------- render

const TOY = {
  sheet: 'Toybox',
  columns: [
    { name: 'a', kind: 'Int', required: false, default: '0' },
    { name: 'b', kind: 'Text', required: false },
    { name: 'c', kind: 'Text', required: false },
  ],
};

test('render lays out every column with a label, control and error slot', () => {
  const host = div();
  Forms.render(host, TOY, { a: '1', b: 'two', c: '' }, {});
  assert.deepEqual(labels(host), ['a', 'b', 'c']);
  assert.deepEqual(named(host), ['a', 'b', 'c']);
  assert.deepEqual(host.querySelectorAll('[data-error-for]').map(
    (n) => n.getAttribute('data-error-for')), ['a', 'b', 'c']);
  assert.equal(host.getElementsByTagName('h3').length, 1);
});

test('every scalar label points at its own control', () => {
  const host = div();
  Forms.render(host, sheet('Items'), {}, {});
  const ids = new Set(host.querySelectorAll('[id]').map((n) => n.getAttribute('id')));
  const targets = host.getElementsByTagName('label')
    .map((l) => l.getAttribute('for')).filter((f) => f !== null);
  assert.ok(targets.length > 20, `expected many labelled scalars, saw ${targets.length}`);
  targets.forEach((t) => assert.ok(ids.has(t), `label for="${t}" points at nothing`));
  assert.equal(new Set(targets).size, targets.length, 'duplicate for= targets');
});

test('the class hooks Task 11 styles against are present', () => {
  const host = div();
  Forms.render(host, TOY, {}, {});
  assert.equal(host.querySelectorAll('[class="field"]').length, 3);
  assert.equal(host.querySelectorAll('[class="error"]').length, 3);
});

test('render clears whatever was there before', () => {
  const host = div();
  host.appendChild(Forms.el('p', null, 'stale'));
  Forms.render(host, TOY, {}, {});
  assert.equal(host.getElementsByTagName('p').length, 0);
  Forms.render(host, TOY, {}, {});
  assert.deepEqual(named(host), ['a', 'b', 'c']);
});

test('the restart warning appears exactly for RESTART_ONLY sheets', () => {
  const warned = [];
  [...SCHEMA.sheets].forEach((s) => {
    const host = div();
    Forms.render(host, s, {}, {});
    const warn = host.querySelector('[class="warn"]');
    if (warn) {
      warned.push(s.sheet);
      assert.equal(warn.textContent,
        'Changes to ' + s.sheet + ' need a full server restart — /reloadsql does not ' +
        'reload this table.');
    }
  });
  const expected = [...SCHEMA.sheets].map((s) => s.sheet).filter(Layout.needsRestart);
  assert.deepEqual(warned, expected);
  assert.ok(expected.length > 0 && expected.length < SCHEMA.sheets.length);
});

test('a live sheet gets no warning', () => {
  const host = div();
  Forms.render(host, sheet('Items'), {}, {});
  assert.equal(host.querySelector('[class="warn"]'), null);
});

// ---------------------------------------------------------------- fk routing

const FK_SHEET = {
  sheet: 'NPCs',
  columns: [{ name: 'quest_id', kind: 'Id', ref: 'Quests', required: false }],
  composites: [],
};

test('render sends a column with a ref to Pickers.fkControl', () => {
  const seen = [];
  globalThis.Pickers = {
    fkControl(column, value, ctx) {
      seen.push({ column, value, ctx });
      const node = document.createElement('input');
      node.setAttribute('name', column.name);
      node.value = String(value);
      return node;
    },
  };
  try {
    const host = div();
    const ctx = { pickerData: {} };
    Forms.render(host, FK_SHEET, { quest_id: '10' }, ctx);
    assert.equal(seen.length, 1);
    assert.equal(seen[0].column.name, 'quest_id');
    assert.equal(seen[0].value, '10');
    assert.equal(seen[0].ctx, ctx, 'ctx is threaded through so the picker can read pickerData');
  } finally {
    delete globalThis.Pickers;
  }
});

test('render sends every part-graphic column to Pickers.partControl with its own spec', () => {
  // The routing that gives graphic_equip and the three appearance ids a preview at all. Both spec
  // shapes travel: the equip graphic's folder comes from item_slot, an appearance id's is fixed.
  const seen = [];
  globalThis.Pickers = {
    partControl(opts) {
      seen.push(opts);
      const node = document.createElement('input');
      node.setAttribute('name', opts.column.name);
      node.value = String(opts.values[opts.column.name]);
      return node;
    },
  };
  try {
    const host = div();
    Forms.render(host, {
      sheet: 'NPCs', composites: [],
      columns: ['body_id', 'hair_id', 'face_id', 'npc_name'].map((n) => column('NPCs', n)),
    }, { body_id: '5', hair_id: '6', face_id: '7', npc_name: 'Rat' }, {});

    // In LAYOUT order, which is the sheet's own: face_id sits before hair_id in the Appearance
    // group.
    assert.deepEqual(seen.map((o) => o.column.name), ['body_id', 'face_id', 'hair_id']);
    assert.deepEqual(seen.map((o) => o.spec.category), ['Bodies', 'Eyes', 'Hair']);
    // The WHOLE values map, not just the cell: a part control reads other columns (body_state, and
    // for graphic_equip the slot). And the tint columns come from Layout, per column.
    assert.equal(seen[0].values.npc_name, 'Rat');
    assert.deepEqual(seen[0].tintColumns, ['body_r', 'body_g', 'body_b', 'body_a']);
    assert.deepEqual(seen[2].tintColumns, ['hair_r', 'hair_g', 'hair_b', 'hair_a']);
    assert.equal(seen[1].tintColumns, null, 'the eyes layer is never tinted');
    assert.equal(host.querySelector('[name="npc_name"]').tagName, 'INPUT');
  } finally {
    delete globalThis.Pickers;
  }
});

test('render hands a part control the raw cell AND the resolved record', () => {
  // The split the reported bug came down to: the FIELD must show the blank cell (blank means "use
  // the SQL default" and has to write back blank), while everything the control DRAWS from has to
  // read the row the importer will produce — a blank body_state is an unarmed 3, not a 0.
  const seen = [];
  globalThis.Pickers = {
    partControl(opts) {
      seen.push(opts);
      const node = document.createElement('input');
      node.setAttribute('name', opts.column.name);
      node.value = String(opts.values[opts.column.name]);
      return node;
    },
  };
  try {
    const host = div();
    Forms.render(host, {
      sheet: 'NPCs', composites: [],
      columns: ['body_id', 'body_state', 'npc_name'].map((n) => column('NPCs', n)),
    }, { body_id: '', body_state: '', npc_name: 'Rat' }, {});

    assert.equal(seen.length, 1);
    assert.equal(seen[0].values.body_id, '', 'the cell stays blank');
    assert.equal(host.querySelector('[name="body_id"]').value, '', 'and so does the field');
    assert.equal(seen[0].effective.body_id, '1');
    assert.equal(seen[0].effective.body_state, '3');
    assert.equal(seen[0].effective.npc_name, 'Rat');
  } finally {
    delete globalThis.Pickers;
  }
});

test('render hands a composite the resolved record alongside the raw one', () => {
  compositeCalls.control.length = 0;
  const host = div();
  Forms.render(host, {
    sheet: 'Toybox',
    columns: [
      { name: 'a', kind: 'Int', required: false, default: '7' },
      { name: 'r', kind: 'Int', required: false, default: '9' },
      { name: 'g', kind: 'Int', required: false, default: '9' },
      { name: 'b', kind: 'Int', required: false, default: '9' },
    ],
    composites: [{ kind: 'Rgba', columns: ['r', 'g', 'b'] }],
  }, { a: '1', r: '', g: '0', b: '0' }, {});
  const call = compositeCalls.control[0];
  assert.equal(call.values.r, '', 'the cells the control writes back stay raw');
  assert.equal(call.effective.r, '9', 'and the ones it draws from are resolved');
  assert.equal(call.effective.a, '1', 'a stored value is never overwritten');
});

test('the monster-body rows hide with the cell, and keep their values while hidden', () => {
  // Two gates now feed the same mechanism (item_usetype on Items, body_id on NPCs). Hidden, not
  // skipped: the inputs stay in the tree so Forms.collect still reads the stored cells verbatim.
  const host = div();
  const changed = [];
  const ctx = { onFormChange(fn) { changed.push(fn); } };
  const sheet = {
    sheet: 'NPCs', composites: [],
    columns: ['body_id', 'face_id', 'hair_id', 'npc_name'].map((n) => column('NPCs', n)),
  };
  const values = { body_id: '1', face_id: '70', hair_id: '26', npc_name: 'Rat' };
  Forms.render(host, sheet, values, ctx);

  const rowOf = (name) => {
    let n = host.querySelector('[name="' + name + '"]');
    while (n && n.className !== 'field') n = n.parentNode;
    return n;
  };
  assert.equal(rowOf('face_id').hidden, false);

  changed.forEach((fn) => fn({ body_id: '150' }));
  assert.equal(rowOf('face_id').hidden, true);
  assert.equal(rowOf('hair_id').hidden, true);
  assert.equal(rowOf('body_id').hidden, false, 'the cell that decides must stay editable');
  assert.equal(rowOf('npc_name').hidden, false);
  assert.deepEqual(Forms.collect(host, sheet), values, 'a hidden row still collects its cell');

  changed.forEach((fn) => fn({ body_id: '1' }));
  assert.equal(rowOf('face_id').hidden, false);
});

test('render leaves a column with no ref on the scalar control', () => {
  globalThis.Pickers = { fkControl() { throw new Error('must not be called'); } };
  try {
    const host = div();
    Forms.render(host, { sheet: 'NPCs', composites: [], columns: [column('NPCs', 'npc_name')] },
      { npc_name: 'Rat' }, {});
    assert.equal(host.querySelector('[name="npc_name"]').value, 'Rat');
  } finally {
    delete globalThis.Pickers;
  }
});

test('render falls back to a text box when Pickers is not included', () => {
  // Losing every field of a sheet to one missing include would be far worse than 26 columns
  // rendering as plain text boxes that still hold, validate and save the right value.
  assert.equal(typeof globalThis.Pickers, 'undefined');
  const host = div();
  Forms.render(host, FK_SHEET, { quest_id: '10' }, {});
  const node = host.querySelector('[name="quest_id"]');
  assert.equal(node.tagName, 'INPUT');
  assert.equal(node.value, '10');
});

// ---------------------------------------------------------------- composites

const COMPOSITE_TOY = {
  sheet: 'Toybox',
  columns: [
    { name: 'a', kind: 'Int', required: false },
    { name: 'r', kind: 'Int', required: false },
    { name: 'g', kind: 'Int', required: false },
    { name: 'b', kind: 'Int', required: false },
  ],
  composites: [{ kind: 'Rgba', columns: ['r', 'g', 'b'] }],
};

test('a composite renders once, at its leader, and its siblings are skipped', () => {
  compositeCalls.control.length = 0;
  const host = div();
  Forms.render(host, COMPOSITE_TOY, { a: '1', r: '255', g: '0', b: '0' }, { tag: 'ctx' });
  assert.deepEqual(labels(host), ['a', 'r']);
  assert.equal(compositeCalls.control.length, 1);
  assert.equal(host.querySelectorAll('[data-composite]').length, 1);

  const call = compositeCalls.control[0];
  assert.equal(call.comp, COMPOSITE_TOY.composites[0]);
  assert.equal(call.byName.g.name, 'g');
  assert.deepEqual(call.values, { a: '1', r: '255', g: '0', b: '0' });
  assert.deepEqual(call.ctx, { tag: 'ctx' });
});

test('a composite naming a column the schema lacks still renders its real columns', () => {
  // Hardening, not a live defect: every composite in today's schema.js has its columns[0]
  // present. But taking columns[0] as leader on faith means that the day a descriptor drops a
  // composite column, the absent column is elected leader, nothing renders, and every sibling
  // is skipped as "rendered by its leader" — the columns go quietly uneditable rather than
  // failing loudly.
  const host = div();
  Forms.render(host, {
    sheet: 'Toybox',
    columns: [{ name: 'r', kind: 'Int', required: false }],
    composites: [{ kind: 'Rgba', columns: ['ghost', 'r'] }],
  }, {}, {});
  assert.deepEqual(labels(host), ['r']);
  assert.equal(host.querySelectorAll('[data-composite]').length, 1);
});

test('a composite naming an Object.prototype member as its first column', () => {
  // The nastiest form of the missing-leader case: byName['toString'] reads truthy from the
  // prototype of a plain object, so the absent column would be elected leader and the one
  // column that DOES exist would be skipped as already rendered — an empty form.
  const host = div();
  Forms.render(host, {
    sheet: 'Toybox',
    columns: [{ name: 'r', kind: 'Int', required: false }],
    composites: [{ kind: 'Rgba', columns: ['toString', 'r'] }],
  }, {}, {});
  assert.deepEqual(labels(host), ['r']);
  assert.equal(host.querySelectorAll('[data-composite]').length, 1);
});

test('a composite whose columns are all absent renders nothing at all', () => {
  const host = div();
  Forms.render(host, {
    sheet: 'Toybox',
    columns: [{ name: 'a', kind: 'Int', required: false }],
    composites: [{ kind: 'Rgba', columns: ['ghost'] }],
  }, {}, {});
  assert.deepEqual(labels(host), ['a']);
  assert.equal(host.querySelectorAll('[data-composite]').length, 0);
});

test('a composite spanning two Layout groups renders in the leader group only', () => {
  // Items puts graphic_tile and item_name in different groups; a composite across them must
  // still produce exactly one control, and no orphan label in the other group.
  compositeCalls.control.length = 0;
  const host = div();
  const items = sheet('Items');
  Forms.render(host, {
    sheet: 'Items',
    columns: items.columns,
    composites: [{ kind: 'Fake', columns: ['graphic_tile', 'item_name'] }],
  }, {}, {});
  assert.equal(compositeCalls.control.length, 1);
  const names = labels(host);
  assert.equal(names.filter((n) => n === 'graphic_tile').length, 1);
  assert.equal(names.filter((n) => n === 'item_name').length, 0);
  // The leader is graphic_tile — the composite's first schema-present column — so the single
  // control sits in Graphics, where Layout puts graphic_tile, and NOT in Identity.
  const sections = host.getElementsByTagName('section');
  const titled = (t) => sections.find((s) => s.getElementsByTagName('h3')[0].textContent === t);
  assert.equal(titled('Graphics').querySelectorAll('[data-composite]').length, 1);
  assert.equal(titled('Identity').querySelectorAll('[data-composite]').length, 0);
});

test('a group left empty by a composite claim renders no heading', () => {
  const host = div();
  Forms.render(host, {
    sheet: 'Items',
    columns: [
      { name: 'item_name', kind: 'Text', required: true },
      { name: 'graphic_tile', kind: 'Int', required: false },
    ],
    composites: [{ kind: 'Fake', columns: ['item_name', 'graphic_tile'] }],
  }, {}, {});
  assert.deepEqual(host.getElementsByTagName('h3').map((h) => h.textContent), ['Identity']);
});

test('a composite label carries no dangling for=', () => {
  const host = div();
  Forms.render(host, COMPOSITE_TOY, {}, {});
  const composite = host.getElementsByTagName('label').find((l) => l.textContent === 'r');
  assert.equal(composite.getAttribute('for'), null);
});

test('the real Items sheet renders every column exactly once', () => {
  const host = div();
  const items = sheet('Items');
  Forms.render(host, items, {}, {});
  const claimed = new Set([...items.composites].flatMap((c) => [...c.columns]));
  const leaders = new Set([...items.composites].map((c) => c.columns[0]));
  const compFor = new Map([...items.composites].map((c) => [c.columns[0], c]));
  const expected = [...items.columns].map((c) => c.name)
    .filter((n) => !claimed.has(n) || leaders.has(n))
    // A composite's label names the whole field, not its leader column.
    .map((n) => (compFor.has(n) ? Layout.labelFor(compFor.get(n), n) : n));
  assert.deepEqual(labels(host).sort(), expected.sort());
});

// ---------------------------------------------------------------- collect

test('collect round-trips what render produced', () => {
  const host = div();
  const values = { a: '1', b: 'two', c: '' };
  Forms.render(host, TOY, values, {});
  assert.deepEqual(Forms.collect(host, TOY), values);
});

test('collect returns blank for every column with no control', () => {
  const host = div();
  assert.deepEqual(Forms.collect(host, TOY), { a: '', b: '', c: '' });
});

test('collect round-trips a real Items row through the form', () => {
  const host = div();
  const items = sheet('Items');
  const values = {};
  [...items.columns].forEach((c, i) => {
    values[c.name] = c.kind === 'Enum' ? c.enumNames[0]
      : c.kind === 'Bool' ? String(i % 2)
      : c.kind === 'Text' ? 'text ' + i
      : String(i);
  });
  Forms.render(host, items, values, {});
  assert.deepEqual(Forms.collect(host, items), values);
});

test('collect asks Composites for the columns it owns and lets it win', () => {
  compositeCalls.collect.length = 0;
  const host = div();
  Forms.render(host, COMPOSITE_TOY, { a: '1', r: '255', g: '0', b: '0' }, {});
  const out = Forms.collect(host, COMPOSITE_TOY);
  assert.deepEqual(out, { a: '1', r: '255', g: '0', b: '0' });
  assert.equal(compositeCalls.collect.length, 1);
  assert.equal(compositeCalls.collect[0].comp, COMPOSITE_TOY.composites[0]);
  assert.equal(compositeCalls.collect[0].container, host);
});

test('Composites.collect outranks the named inputs inside its own control', () => {
  // A composite is free to render raw sub-inputs (an RGBA picker's four channels, a hex box)
  // and hand back a canonicalised value. Whatever it returns must be what is saved, including
  // when the sub-input for that very column says something else.
  const host = div();
  Forms.render(host, COMPOSITE_TOY, { a: '1', r: 'ff', g: '0', b: '0' }, {});
  const previous = globalThis.Composites.collect;
  globalThis.Composites.collect = () => ({ r: '255', g: '0', b: '0' });
  try {
    assert.equal(host.querySelector('[name="r"]').value, 'ff');
    assert.deepEqual(Forms.collect(host, COMPOSITE_TOY), { a: '1', r: '255', g: '0', b: '0' });
  } finally {
    globalThis.Composites.collect = previous;
  }
});

test('collect finds a control by name even when it has no id', () => {
  // Composite sub-controls are named after their columns but carry no id; keying the sweep on
  // anything but [name] would skip them.
  const host = div();
  const input = document.createElement('input');
  input.setAttribute('name', 'b');
  input.value = 'from a composite';
  host.appendChild(input);
  assert.equal(input.getAttribute('id'), null);
  assert.deepEqual(Forms.collect(host, TOY), { a: '', b: 'from a composite', c: '' });
});

test('collect coerces a non-string handed back by Composites', () => {
  // A packed bitmask is the obvious shape for Composites.collect to return, and it is a
  // NUMBER. Everything downstream — Validation, writeRow — is written against strings.
  const host = div();
  Forms.render(host, COMPOSITE_TOY, {}, {});
  const previous = globalThis.Composites.collect;
  globalThis.Composites.collect = () => ({ r: 255, g: 0, b: 0 });
  try {
    assert.deepEqual(Forms.collect(host, COMPOSITE_TOY), { a: '', r: '255', g: '0', b: '0' });
  } finally {
    globalThis.Composites.collect = previous;
  }
});

test('collect coerces whatever a control hands back to a string', () => {
  const host = div();
  Forms.render(host, TOY, {}, {});
  const control = host.querySelector('[name="a"]');
  control._value = 5;              // bypass the setter's own coercion
  control._dirty = true;
  assert.deepEqual(Forms.collect(host, TOY).a, '5');
});

test('collect returns exactly the schema columns, dropping stray named nodes', () => {
  // A composite is free to name its sub-controls whatever it likes (a hex box, a search
  // field). Those must not arrive in the record as if they were columns.
  const host = div();
  Forms.render(host, TOY, { a: '1', b: '', c: '' }, {});
  const stray = document.createElement('input');
  stray.setAttribute('name', 'not_a_column');
  stray.value = 'leaked';
  host.appendChild(stray);

  const out = Forms.collect(host, TOY);
  assert.deepEqual(Object.keys(out).sort(), ['a', 'b', 'c']);
  assert.equal(out.a, '1');
});

test('collect drops a key Composites.collect invents', () => {
  const host = div();
  Forms.render(host, COMPOSITE_TOY, {}, {});
  const previous = globalThis.Composites.collect;
  globalThis.Composites.collect = () => ({ r: '1', hex: '#ff0000' });
  try {
    assert.deepEqual(Object.keys(Forms.collect(host, COMPOSITE_TOY)).sort(),
      ['a', 'b', 'g', 'r']);
  } finally {
    globalThis.Composites.collect = previous;
  }
});

test('every real sheet round-trips blank through render and collect', () => {
  [...SCHEMA.sheets].forEach((s) => {
    const host = div();
    Forms.render(host, s, {}, {});
    const out = Forms.collect(host, s);
    assert.deepEqual(Object.keys(out).sort(), [...s.columns].map((c) => c.name).sort(),
      s.sheet);
    Object.keys(out).forEach((k) => assert.equal(out[k], '', `${s.sheet}.${k}`));
  });
});

test('a column named after an Object.prototype member is handled like any other', () => {
  // A plain-object lookup reads truthy for 'toString' from the prototype: claimed and leaders
  // would both answer Object.prototype.toString, compare equal, and the column would be
  // rendered as a composite — handing Composites.control a function.
  const evil = {
    sheet: 'Toybox',
    columns: [
      { name: 'toString', kind: 'Text', required: false },
      { name: 'constructor', kind: 'Text', required: false },
    ],
  };
  const host = div();
  Forms.render(host, evil, { toString: 'x', constructor: 'y' }, {});
  assert.deepEqual(labels(host), ['toString', 'constructor']);
  assert.equal(host.querySelectorAll('[data-composite]').length, 0);
  assert.deepEqual(Forms.collect(host, evil), { toString: 'x', constructor: 'y' });

  Forms.showErrors(host, [{ column: 'toString', message: 'nope' }]);
  assert.equal(host.querySelector('[data-error-for="toString"]').textContent, 'nope');
});

// ---------------------------------------------------------------- showErrors

test('showErrors writes each message into its own slot', () => {
  const host = div();
  Forms.render(host, TOY, {}, {});
  Forms.showErrors(host, [{ column: 'b', message: 'b is required' }]);
  assert.equal(host.querySelector('[data-error-for="b"]').textContent, 'b is required');
  assert.equal(host.querySelector('[data-error-for="a"]').textContent, '');
});

test('showErrors clears previous messages before writing new ones', () => {
  const host = div();
  Forms.render(host, TOY, {}, {});
  Forms.showErrors(host, [{ column: 'a', message: 'first' }]);
  Forms.showErrors(host, [{ column: 'c', message: 'second' }]);
  assert.equal(host.querySelector('[data-error-for="a"]').textContent, '');
  assert.equal(host.querySelector('[data-error-for="c"]').textContent, 'second');
  Forms.showErrors(host, []);
  assert.equal(host.querySelector('[data-error-for="c"]').textContent, '');
});

test('showErrors tolerates an error for a column with no slot', () => {
  const host = div();
  Forms.render(host, TOY, {}, {});
  Forms.showErrors(host, [
    { column: 'nonexistent', message: 'ignored' },
    { column: 'a', message: 'kept' },
  ]);
  assert.equal(host.querySelector('[data-error-for="a"]').textContent, 'kept');
});

test('showErrors reaches a column claimed by a composite', () => {
  const host = div();
  Forms.render(host, COMPOSITE_TOY, {}, {});
  Forms.showErrors(host, [
    { column: 'r', message: 'bad red' },
    { column: 'g', message: 'no slot for me' },
  ]);
  assert.equal(host.querySelector('[data-error-for="r"]').textContent, 'bad red');
});

test('a duplicated slot resolves to the first in document order', () => {
  // Task 10 may well give a composite its own per-column error slot, which would sit inside
  // the control and duplicate the row's. The message must go to exactly one of them, and to
  // the same one every time — the innermost, which is the one next to the offending field.
  const host = div();
  Forms.render(host, COMPOSITE_TOY, {}, {});
  const inner = Forms.el('div', { 'data-error-for': 'r' });
  host.querySelector('[data-composite]').appendChild(inner);
  const outer = host.querySelectorAll('[data-error-for="r"]').filter((n) => n !== inner)[0];

  Forms.showErrors(host, [{ column: 'r', message: 'bad red' }]);
  const slots = host.querySelectorAll('[data-error-for="r"]');
  assert.equal(slots[0], inner);
  assert.equal(slots[1], outer);
  assert.equal(slots[0].textContent, 'bad red');
  assert.equal(slots[1].textContent, '');
});

test('every Validation error for a real sheet lands somewhere or is knowingly dropped', () => {
  const items = sheet('Items');
  const host = div();
  Forms.render(host, items, {}, {});
  const slots = new Set(host.querySelectorAll('[data-error-for]')
    .map((n) => n.getAttribute('data-error-for')));
  const claimed = new Set([...items.composites].flatMap((c) => [...c.columns]));
  [...items.columns].forEach((c) => {
    assert.ok(slots.has(c.name) || claimed.has(c.name),
      `${c.name} can produce an error with nowhere to show it`);
  });
});

test("an Rgba field's label is the tint, not its red channel", () => {
  const host = div();
  const items = sheet('Items');
  Forms.render(host, items, {}, {});
  const names = labels(host);
  assert.ok(names.includes('graphic tint'),
    `no tint label among ${JSON.stringify(names)}`);
  assert.equal(names.filter((n) => n === 'graphic_r').length, 0);

  // Renaming the label must not cost the field its composite control: the stub emits one [name]
  // per column, so this only guards that forms.js still renders the control here. That the real
  // control reaches all four cells is composites.test.js's job.
  const field = host.getElementsByTagName('label')
    .find((l) => l.textContent === 'graphic tint').parentNode;
  ['graphic_r', 'graphic_g', 'graphic_b', 'graphic_a'].forEach((n) => {
    assert.ok(named(field).includes(n), `${n} is not reachable from the tint field`);
  });
});
