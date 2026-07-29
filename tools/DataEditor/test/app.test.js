import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { installFakeDom, installFakeImage, installGoogleScriptRun, fire, walk } from './fake-dom.js';

installFakeDom();

// The REAL schema, not a hand-written stand-in. Every hazard in this module is about a specific
// sheet's column order — the name column of Items being index 2, the nine sheets with no pk, the
// composites NPCs carries — so a fake schema would test the fake.
const schemaSource = readFileSync(fileURLToPath(new URL('../schema.js', import.meta.url)), 'utf8');
globalThis.GOOSE_SCHEMA = new Function(schemaSource + '\nreturn GOOSE_SCHEMA;')();

const { Validation } = await import('../src/validation.js');
globalThis.Validation = Validation;
const { Equipped } = await import('../src/equipped.js');
globalThis.Equipped = Equipped;
const { Appearance } = await import('../src/appearance.js');
globalThis.Appearance = Appearance;
const { Sprites } = await import('../src/sprites.js');
globalThis.Sprites = Sprites;
const { Layout } = await import('../src/layout.js');
globalThis.Layout = Layout;
const { Pickers } = await import('../src/pickers.js');
globalThis.Pickers = Pickers;
const { Composites } = await import('../src/composites.js');
globalThis.Composites = Composites;
const { Forms } = await import('../src/forms.js');
globalThis.Forms = Forms;
const { Preview } = await import('../src/preview.js');
globalThis.Preview = Preview;

const { App } = await import('../src/app.js');

// Bundles with one usable body sprite, so the character preview has something to draw and the
// effects bundle has nothing — an effect preview that starts a real interval would outlive the
// test. The one test that needs frames supplies its own.
globalThis.GOOSE_SPRITES = {
  icons: { png: 'data:image/png;base64,ICONS', rects: { '1:1': [0, 0, 16, 16] } },
  parts: { png: 'data:image/png;base64,PARTS', rects: { 'Bodies:1:idle-down': [0, 0, 48, 48] } },
  effects: { png: 'data:image/png;base64,EFFECTS', rects: {} },
};

const schemaOf = (sheet) => GOOSE_SCHEMA.sheets.filter((s) => s.sheet === sheet)[0];

// A full-width row for a sheet, blank except for what the caller names. Blank is a legal cell
// everywhere it is not required, so this is a valid record whenever the required columns are
// supplied — which is what makes "the publish check reports nothing" a meaningful assertion.
function rowFor(sheet, values) {
  return schemaOf(sheet).columns.map((c) => (values[c.name] === undefined ? '' : String(values[c.name])));
}

const ITEM = (id, name) => rowFor('Items', {
  item_template_id: id, item_usetype: 'NoUse', item_name: name, graphic_tile: 1,
});
const NPC = (id, name, extra) => rowFor('NPCs', Object.assign({
  npc_id: id, npc_name: name, npc_type: 'Monster', body_id: 1, body_state: 1,
  equipped_items: '0,*,0,*,0,*,0,*,0,*,0,*',
}, extra || {}));
const DROP = (npcId, itemId) => rowFor('NPC Drops', {
  npc_template_id: npcId, item_template_id: itemId, stack: 1, droprate: '0.10',
});

// The default server: every sheet empty unless `sheets` names it. writeRow records what it was
// asked to write and answers the way Code.gs does, with the row it landed on.
function makeServer(sheets, options) {
  const opts = options || {};
  const writes = [];

  const server = {
    readSheet(name) {
      if (opts.failOn === name) throw new Error('boom: ' + name);
      const rows = sheets[name] || [];
      return {
        sheet: name,
        header: schemaOf(name).columns.map((c) => c.name),
        rows,
        lastRow: rows.length + 1,
      };
    },
    readSheetIndex(name, nameColumnIndex) {
      const rows = sheets[name] || [];
      const at = typeof nameColumnIndex === 'number' && nameColumnIndex >= 1 ? nameColumnIndex : 1;
      return {
        sheet: name,
        entries: rows.filter((r) => String(r[0]).trim() !== '')
          .map((r) => ({ id: r[0], name: r[at] })),
      };
    },
    writeRow(sheet, rowNumber, cells, idColumnIndex) {
      writes.push({ sheet, rowNumber, cells, idColumnIndex });
      const target = Number(rowNumber) > 0 ? Number(rowNumber) : (sheets[sheet] || []).length + 2;
      if (opts.writeFails) throw new Error('write refused');
      return { row: target };
    },
  };

  return { server, writes };
}

// The page, as Editor.html lays it out.
function buildShell() {
  const doc = installFakeDom();
  ['sheet-picker', 'records', 'form', 'previews', 'publish-results', 'status',
   'new-record', 'save', 'publish-check'].forEach((id) => {
    const tag = id === 'sheet-picker' ? 'select'
      : (['new-record', 'save', 'publish-check'].indexOf(id) !== -1 ? 'button'
        : (id === 'status' ? 'span' : 'div'));
    const node = doc.createElement(tag);
    node.id = id;
    doc.body.appendChild(node);
  });
  return doc;
}

// Boots the app the way the page does. Returns the handles a test needs to drive it.
function boot(sheets, options) {
  const opts = options || {};
  const doc = buildShell();
  const img = installFakeImage();
  const { server, writes } = makeServer(sheets || {}, options);
  const run = installGoogleScriptRun(server);

  Object.assign(App.__state, {
    schema: null, sheetName: null, rows: [], header: [], rowNumber: 0, ids: [], bundleErrors: [],
    idSets: {}, pickerData: {}, bundles: {}, images: {}, imageCallbacks: [],
    loaded: {}, stopEffect: null, checking: false,
  });

  App.init();

  const handles = {
    doc, img, run, writes,
    get: (id) => doc.getElementById(id),
    status: () => doc.getElementById('status').textContent,
    // Lets every queued image decode and every queued server call complete, repeatedly, until
    // the app is quiet. Both queues feed each other: a bundle landing renders a form, a form
    // rendering asks for a sheet.
    settle() {
      for (let i = 0; i < 20; i++) {
        const moved = img.load() + run.flush();
        if (!moved) break;
      }
    },
  };

  if (!opts.hold) handles.settle();
  return handles;
}

function serverCalls(run, name) {
  return run.calls.filter((c) => c.name === name);
}

// --- name column index (carry-forward #16) ---------------------------------------------------

test('nameIndex is 2 for Items and NPCs and 1 for the other six FK targets', () => {
  assert.equal(App.nameIndex(schemaOf('Items')), 2);
  assert.equal(App.nameIndex(schemaOf('NPCs')), 2);
  ['Classes', 'Spells', 'Spell Effects', 'Maps', 'Quests', 'Combinations'].forEach((sheet) => {
    assert.equal(App.nameIndex(schemaOf(sheet)), 1, sheet);
  });
});

test('nameIndex never answers 0, and falls back to 1 for a sheet with no Text column', () => {
  // Column A is always the id, so index 0 is never a label. Code.gs's own default is 1.
  assert.equal(App.nameIndex(schemaOf('NPC Drops')), 1);
  assert.equal(App.nameIndex(null), 1);
});

test('readSheetIndex is called with the name column index, not just the sheet', () => {
  // NPC Drops points at NPCs and Items — the two sheets whose column B is an enum. Passing the
  // default would label all 649 Items entries "Armor"/"Weapon"/"NoUse".
  const h = boot({ Items: [ITEM(1, 'Gold')], NPCs: [NPC(1, 'Rat')] });
  h.get('sheet-picker').value = 'NPC Drops';
  fire(h.get('sheet-picker'), 'change');
  h.settle();

  // Items was opened first by init, so its own refs (Spell Effects, Spells) and Bitmask source
  // (Classes) are in the call list too; those are the six-default sheets.
  const calls = serverCalls(h.run, 'readSheetIndex').map((c) => c.args);
  assert.ok(calls.some((a) => a[0] === 'Items' && a[1] === 2), 'Items at index 2');
  assert.ok(calls.some((a) => a[0] === 'NPCs' && a[1] === 2), 'NPCs at index 2');
  assert.ok(calls.some((a) => a[0] === 'Classes' && a[1] === 1), 'Classes at the default');
  assert.deepEqual(App.__state.pickerData.Items, [{ id: '1', name: 'Gold' }]);
});

// --- the shell ------------------------------------------------------------------------------

test('init lists every sheet and opens the first one', () => {
  const h = boot({ Items: [ITEM(1, 'Gold'), ITEM(2, 'Sword')] });

  const options = h.get('sheet-picker').getElementsByTagName('option');
  assert.equal(options.length, GOOSE_SCHEMA.sheets.length);
  assert.equal(options.length, 21);
  assert.equal(options[0].value, 'Items');

  assert.deepEqual(serverCalls(h.run, 'readSheet').map((c) => c.args), [['Items']]);
  assert.equal(h.status(), '2 records');
});

test('the record list labels rows with the NAME column, not column B', () => {
  const h = boot({ Items: [ITEM(1, 'Gold'), ITEM(2, 'Sword')] });
  const labels = h.get('records').children.map((n) => n.textContent);
  assert.deepEqual(labels, ['1 — Gold', '2 — Sword']);
});

test('a blank id in the list shows as ? rather than an empty button', () => {
  const h = boot({ Items: [rowFor('Items', { item_name: 'Nameless' })] });
  assert.equal(h.get('records').children[0].textContent, '? — Nameless');
});

test('clicking a record renders the form for that row', () => {
  const h = boot({ Items: [ITEM(1, 'Gold'), ITEM(2, 'Sword')] });
  fire(h.get('records').children[1], 'click');
  h.settle();

  // Row index 1 is spreadsheet row 3: readSheet returns values.slice(1), so index 0 is row 2.
  assert.equal(App.__state.rowNumber, 3);
  const name = h.get('form').querySelector('[name="item_name"]');
  assert.equal(name.value, 'Sword');
});

test('New suggests max + 1 and writes as an append', () => {
  const h = boot({ Items: [ITEM(1, 'Gold'), ITEM(7, 'Sword')] });
  fire(h.get('new-record'), 'click');
  h.settle();

  assert.equal(App.__state.rowNumber, 0);
  assert.equal(h.get('form').querySelector('[name="item_template_id"]').value, '8');
});

test('bundlesFor loads parts only for the sheets that draw a character', () => {
  assert.deepEqual(App.bundlesFor(schemaOf('Items')), ['icons']);
  assert.deepEqual(App.bundlesFor(schemaOf('NPCs')), ['icons', 'parts']);
  assert.deepEqual(App.bundlesFor(schemaOf('Spells')), ['icons', 'effects']);
  assert.deepEqual(App.bundlesFor(schemaOf('Spell Effects')), ['icons', 'parts', 'effects']);
  assert.deepEqual(App.bundlesFor(schemaOf('NPC Drops')), ['icons']);
});

test('the form is rendered only once every bundle it needs has decoded', () => {
  // The plan's nested loadBundle('icons' | 'parts') idiom re-requested icons as a no-op and
  // depended on a shared callback queue that the FIRST bundle emptied. Rendering after the
  // sequence completes is what makes the ordering assertable at all.
  const h = boot({ NPCs: [NPC(1, 'Rat')] }, { hold: true });
  h.settle();

  h.get('sheet-picker').value = 'NPCs';
  fire(h.get('sheet-picker'), 'change');
  h.settle();
  fire(h.get('records').children[0], 'click');

  // The record was clicked; parts is still decoding, so nothing has been rendered yet.
  h.run.flush();
  assert.ok(!App.__state.images.parts, 'parts still decoding');
  assert.equal(h.get('form').children.length, 0, 'the form waits for it');

  h.settle();
  assert.ok(App.__state.images.parts, 'parts decoded');
  assert.ok(h.get('form').children.length > 0, 'and then the form appears');
});

test('image callbacks do not accumulate as records are opened', () => {
  const h = boot({ Items: [ITEM(1, 'Gold'), ITEM(2, 'Sword')] });

  fire(h.get('records').children[0], 'click');
  h.settle();
  const first = App.__state.imageCallbacks.length;
  assert.ok(first > 0, 'the graphic controls registered');

  fire(h.get('records').children[1], 'click');
  h.settle();
  assert.equal(App.__state.imageCallbacks.length, first,
               'the previous record\'s callbacks were dropped, not added to');
});

test('a bundle that never decodes still leaves the form usable', () => {
  const saved = globalThis.GOOSE_SPRITES.parts;
  try {
    delete globalThis.GOOSE_SPRITES.parts;
    const h = boot({ NPCs: [NPC(1, 'Rat')] });
    h.get('sheet-picker').value = 'NPCs';
    fire(h.get('sheet-picker'), 'change');
    h.settle();
    fire(h.get('records').children[0], 'click');
    h.settle();

    assert.equal(h.get('form').querySelector('[name="npc_name"]').value, 'Rat');
    // No art, so nothing was drawn — but the preview canvas is still there.
    assert.equal(h.get('previews').children[0].tagName, 'CANVAS');
  } finally {
    globalThis.GOOSE_SPRITES.parts = saved;
  }
});

test('a bundle that fails to decode is reported and does not block the sheet', () => {
  const h = boot({ Items: [ITEM(1, 'Gold')] }, { hold: true });
  // The first image is the icons bundle requested by init.
  assert.equal(h.img.fail(), 1);
  // The failure message is written and then immediately buried: done() runs straight into
  // openSheet's 'Loading …'. That is exactly why the failure is REMEMBERED rather than only
  // announced — the assertion that matters is the one after the sheet has loaded.
  assert.equal(h.status(), 'Loading Items…');

  h.settle();
  assert.equal(h.get('records').children.length, 1);
  // And it is still on screen after the sheet loads: done() runs straight into openSheet, whose
  // own status line would otherwise bury the only explanation for a page full of blank previews.
  assert.match(h.status(), /1 records — Failed to decode the icons sprite bundle/);
  assert.equal(h.get('status').className, 'error');
});

// --- previews -------------------------------------------------------------------------------

test('an NPC record draws a character preview', () => {
  const h = boot({ NPCs: [NPC(1, 'Rat')] });
  h.get('sheet-picker').value = 'NPCs';
  fire(h.get('sheet-picker'), 'change');
  h.settle();
  fire(h.get('records').children[0], 'click');
  h.settle();

  const canvas = h.get('previews').children[0];
  assert.equal(canvas.tagName, 'CANVAS');
  assert.equal(canvas.width, Preview.CANVAS_W);
  assert.equal(canvas.getContext('2d').calls.filter((c) => c[0] === 'drawImage').length, 1);
});

test('the NPC preview uses the row\'s body_state to pick the clip (#15)', () => {
  // Both clips exist for body 1, so the ONLY thing that decides which rect is drawn is
  // body_state. A preview that hardcoded the flag, or forgot to pass it, would draw the other
  // one and look perfectly plausible.
  const saved = globalThis.GOOSE_SPRITES.parts.rects;
  try {
    globalThis.GOOSE_SPRITES.parts.rects = {
      'Bodies:1:idle-equip-down': [0, 0, 48, 48],
      'Bodies:1:idle-no-equip-down': [64, 0, 48, 48],
    };
    const h = boot({ NPCs: [NPC(1, 'Armed', { body_state: 1 }), NPC(2, 'Unarmed', { body_state: 3 })] });
    h.get('sheet-picker').value = 'NPCs';
    fire(h.get('sheet-picker'), 'change');
    h.settle();

    const sxOf = (row) => {
      fire(h.get('records').children[row], 'click');
      h.settle();
      const calls = h.get('previews').children[0].getContext('2d').calls;
      return calls.filter((c) => c[0] === 'drawImage')[0][2];
    };

    assert.equal(sxOf(0), 0, 'body_state 1 is armed');
    assert.equal(sxOf(1), 64, 'body_state 3 is unarmed');
  } finally {
    globalThis.GOOSE_SPRITES.parts.rects = saved;
  }
});

test('Items draws no character preview', () => {
  const h = boot({ Items: [ITEM(1, 'Gold')] });
  fire(h.get('records').children[0], 'click');
  h.settle();
  assert.equal(h.get('previews').children.length, 0);
});

test('a composite change redraws the preview through __onChange', () => {
  const h = boot({ NPCs: [NPC(1, 'Rat')] });
  h.get('sheet-picker').value = 'NPCs';
  fire(h.get('sheet-picker'), 'change');
  h.settle();
  fire(h.get('records').children[0], 'click');
  h.settle();

  const before = h.get('previews').children[0];
  const slot = h.get('form').querySelectorAll('[class="slot-graphic"]')[0];
  slot.value = '1';
  fire(slot, 'input');

  const after = h.get('previews').children[0];
  assert.notEqual(after, before, 'the preview was rebuilt');
});

test('a rejected equip-slot typo does NOT redraw the preview', () => {
  // Composites deliberately skips __onChange while frozen: the cell genuinely did not change.
  const h = boot({ NPCs: [NPC(1, 'Rat')] });
  h.get('sheet-picker').value = 'NPCs';
  fire(h.get('sheet-picker'), 'change');
  h.settle();
  fire(h.get('records').children[0], 'click');
  h.settle();

  const before = h.get('previews').children[0];
  const slot = h.get('form').querySelectorAll('[class="slot-graphic"]')[0];
  slot.value = 'abc';
  fire(slot, 'input');

  assert.equal(h.get('previews').children[0], before);
});

test('switching to a record with no effect stops the previous animation', () => {
  // The leak: stopping the old effect only inside `if (effectId)` leaves a timer running
  // forever the moment the user opens a spell with no effect.
  const realSet = globalThis.setInterval;
  const realClear = globalThis.clearInterval;
  const live = new Set();
  let next = 1;
  globalThis.setInterval = () => { live.add(next); return next++; };
  globalThis.clearInterval = (id) => { live.delete(id); };

  try {
    globalThis.GOOSE_SPRITES.effects.rects = { '4:0': [0, 0, 16, 16] };
    const h = boot({
      Spells: [
        rowFor('Spells', { spell_id: 1, spell_name: 'Fire', spell_target: 'Self',
                           spellbook_graphic: 1, spell_effect_id: 4 }),
        rowFor('Spells', { spell_id: 2, spell_name: 'Nothing', spell_target: 'Self',
                           spellbook_graphic: 1, spell_effect_id: 0 }),
      ],
      'Spell Effects': [rowFor('Spell Effects', {
        spell_effect_id: 4, spell_effect_name: 'Flame', effect_type: 'Instant',
        spell_effected: 'Anyone',
      })],
    });

    h.get('sheet-picker').value = 'Spells';
    fire(h.get('sheet-picker'), 'change');
    h.settle();

    fire(h.get('records').children[0], 'click');
    h.settle();
    assert.equal(live.size, 1, 'the effect animation is running');

    fire(h.get('records').children[1], 'click');
    h.settle();
    assert.equal(live.size, 0, 'switching to a spell with no effect stopped it');
  } finally {
    globalThis.GOOSE_SPRITES.effects.rects = {};
    globalThis.setInterval = realSet;
    globalThis.clearInterval = realClear;
  }
});

// --- save -----------------------------------------------------------------------------------

test('save writes the row it opened, with the pk column index', () => {
  const h = boot({ Items: [ITEM(1, 'Gold'), ITEM(2, 'Sword')] });
  fire(h.get('records').children[1], 'click');
  h.settle();
  fire(h.get('save'), 'click');
  h.settle();

  assert.equal(h.writes.length, 1);
  assert.equal(h.writes[0].sheet, 'Items');
  assert.equal(h.writes[0].rowNumber, 3);
  assert.equal(h.writes[0].idColumnIndex, 0);
  assert.equal(h.writes[0].cells.length, schemaOf('Items').columns.length);
  assert.match(h.status(), /Saved\./);
});

test('a sheet with no pk writes idColumnIndex -1, not 0', () => {
  // Code.gs rejects every second row of the nine no-pk sheets if this is 0 — their column A is
  // an Id-kind FK that legitimately repeats.
  const h = boot({ 'NPC Drops': [DROP(1, 1), DROP(1, 2)], NPCs: [NPC(1, 'Rat')],
                   Items: [ITEM(1, 'Gold'), ITEM(2, 'Sword')] });
  h.get('sheet-picker').value = 'NPC Drops';
  fire(h.get('sheet-picker'), 'change');
  h.settle();
  fire(h.get('records').children[1], 'click');
  h.settle();
  fire(h.get('save'), 'click');
  h.settle();

  assert.equal(h.writes[0].idColumnIndex, -1);
  assert.equal(h.writes[0].rowNumber, 3);
});

test('a blank optional cell is written as null, not as its default', () => {
  const h = boot({ Items: [ITEM(1, 'Gold')] });
  fire(h.get('records').children[0], 'click');
  h.settle();
  fire(h.get('save'), 'click');
  h.settle();

  const at = (name) => schemaOf('Items').columns.findIndex((c) => c.name === name);
  assert.equal(h.writes[0].cells[at('player_hp')], null);
  assert.equal(h.writes[0].cells[at('item_name')], 'Gold');
});

test('an invalid record is refused and the message lands under the field', () => {
  const h = boot({ Items: [ITEM(1, 'Gold')] });
  fire(h.get('records').children[0], 'click');
  h.settle();

  h.get('form').querySelector('[name="player_hp"]').value = 'twelve';
  fire(h.get('save'), 'click');
  h.settle();

  assert.equal(h.writes.length, 0);
  assert.match(h.status(), /1 problem\(s\)/);
  assert.equal(h.get('form').querySelector('[data-error-for="player_hp"]').textContent,
               'player_hp must be a number');
});

test('an FK to a row that does not exist is refused, naming the id and the sheet', () => {
  const h = boot({ 'NPC Drops': [DROP(1, 1)], NPCs: [NPC(1, 'Rat')], Items: [ITEM(1, 'Gold')] });
  h.get('sheet-picker').value = 'NPC Drops';
  fire(h.get('sheet-picker'), 'change');
  h.settle();
  fire(h.get('records').children[0], 'click');
  h.settle();

  h.get('form').querySelector('[name="item_template_id"]').value = '999';
  fire(h.get('save'), 'click');
  h.settle();

  assert.equal(h.writes.length, 0);
  assert.equal(h.get('form').querySelector('[data-error-for="item_template_id"]').textContent,
               'item_template_id = 999 does not exist in Items');
});

test('a duplicate id is refused, and a row keeping its own id is not', () => {
  const h = boot({ Items: [ITEM(1, 'Gold'), ITEM(2, 'Sword')] });
  fire(h.get('records').children[1], 'click');
  h.settle();

  fire(h.get('save'), 'click');
  h.settle();
  assert.equal(h.writes.length, 1, 'its own id is fine');

  fire(h.get('records').children[1], 'click');
  h.settle();
  h.get('form').querySelector('[name="item_template_id"]').value = '1';
  fire(h.get('save'), 'click');
  h.settle();
  assert.equal(h.writes.length, 1, 'no second write');
  assert.match(h.get('form').querySelector('[data-error-for="item_template_id"]').textContent,
               /already used/);
});

test('after a save the sheet reloads and the same record is still open', () => {
  const h = boot({ Items: [ITEM(1, 'Gold'), ITEM(2, 'Sword')] });
  fire(h.get('records').children[1], 'click');
  h.settle();
  fire(h.get('save'), 'click');
  h.settle();

  assert.equal(App.__state.rowNumber, 3);
  assert.equal(h.get('form').querySelector('[name="item_name"]').value, 'Sword');
});

test('a server-side write failure is reported, not swallowed', () => {
  const h = boot({ Items: [ITEM(1, 'Gold')] }, { writeFails: true });
  fire(h.get('records').children[0], 'click');
  h.settle();
  fire(h.get('save'), 'click');
  h.settle();
  assert.equal(h.status(), 'write refused');
});

// --- save gate 1: frozen composites -----------------------------------------------------------

test('save is REFUSED while an equip slot holds a typo', () => {
  const h = boot({ NPCs: [NPC(1, 'Rat', { equipped_items: '4,*,0,*,0,*,0,*,0,*,0,*' })] });
  h.get('sheet-picker').value = 'NPCs';
  fire(h.get('sheet-picker'), 'change');
  h.settle();
  fire(h.get('records').children[0], 'click');
  h.settle();

  const slots = h.get('form').querySelectorAll('[class="slot-graphic"]');
  // A valid edit to slot 2, then a typo in slot 0. The valid edit lives only in the control's
  // memory while the cell is frozen, so a save here would silently discard it.
  slots[2].value = '9';
  fire(slots[2], 'input');
  slots[0].value = 'abc';
  fire(slots[0], 'input');

  assert.ok(walk(h.get('form')).some((n) => n.__frozen === true), 'the control is frozen');

  fire(h.get('save'), 'click');
  h.settle();

  assert.equal(h.writes.length, 0);
  assert.match(h.status(), /invalid graphic id/);
  assert.match(h.status(), /not being recorded/);
});

test('save proceeds once the typo is corrected', () => {
  const h = boot({ NPCs: [NPC(1, 'Rat', { equipped_items: '4,*,0,*,0,*,0,*,0,*,0,*' })] });
  h.get('sheet-picker').value = 'NPCs';
  fire(h.get('sheet-picker'), 'change');
  h.settle();
  fire(h.get('records').children[0], 'click');
  h.settle();

  const slots = h.get('form').querySelectorAll('[class="slot-graphic"]');
  slots[0].value = 'abc';
  fire(slots[0], 'input');
  slots[0].value = '5';
  fire(slots[0], 'input');

  fire(h.get('save'), 'click');
  h.settle();

  const at = schemaOf('NPCs').columns.findIndex((c) => c.name === 'equipped_items');
  assert.equal(h.writes.length, 1);
  assert.equal(h.writes[0].cells[at], '5,*,0,*,0,*,0,*,0,*,0,*');
});

// --- save gate 2: an unfaithful equipped_items (carry-forward #12) -----------------------------

test('a malformed equipped_items round-trips untouched when nothing is edited', () => {
  // Opening a broken row must be safe. '4,*,0,*' is truncated — Equipped.parse zero-fills the
  // last four slots, which is a guess, so isFaithful says no.
  const raw = '4,*,0,*';
  assert.equal(Equipped.isFaithful(raw), false);

  const h = boot({ NPCs: [NPC(1, 'Rat', { equipped_items: raw })] });
  h.get('sheet-picker').value = 'NPCs';
  fire(h.get('sheet-picker'), 'change');
  h.settle();
  fire(h.get('records').children[0], 'click');
  h.settle();
  fire(h.get('save'), 'click');
  h.settle();

  const at = schemaOf('NPCs').columns.findIndex((c) => c.name === 'equipped_items');
  assert.equal(h.writes.length, 1);
  assert.equal(h.writes[0].cells[at], raw, 'written back byte-identically');
});

test('EDITING a malformed equipped_items is refused, and says what is stored', () => {
  const raw = '4,*,0,*';
  const h = boot({ NPCs: [NPC(1, 'Rat', { equipped_items: raw })] });
  h.get('sheet-picker').value = 'NPCs';
  fire(h.get('sheet-picker'), 'change');
  h.settle();
  fire(h.get('records').children[0], 'click');
  h.settle();

  const slot = h.get('form').querySelectorAll('[class="slot-graphic"]')[1];
  slot.value = '9';
  fire(slot, 'input');

  fire(h.get('save'), 'click');
  h.settle();

  assert.equal(h.writes.length, 0);
  assert.match(h.status(), /malformed/);
  assert.match(h.status(), /Stored value: 4,\*,0,\*/);
});

test('a WELL-FORMED equipped_items may be edited freely', () => {
  const raw = '4,*,0,*,0,*,0,*,0,*,0,*';
  assert.equal(Equipped.isFaithful(raw), true);

  const h = boot({ NPCs: [NPC(1, 'Rat', { equipped_items: raw })] });
  h.get('sheet-picker').value = 'NPCs';
  fire(h.get('sheet-picker'), 'change');
  h.settle();
  fire(h.get('records').children[0], 'click');
  h.settle();

  const slot = h.get('form').querySelectorAll('[class="slot-graphic"]')[1];
  slot.value = '9';
  fire(slot, 'input');
  fire(h.get('save'), 'click');
  h.settle();

  const at = schemaOf('NPCs').columns.findIndex((c) => c.name === 'equipped_items');
  assert.equal(h.writes[0].cells[at], '4,*,9,*,0,*,0,*,0,*,0,*');
});

test('a BLANK equipped_items is repaired rather than blocked', () => {
  // Blank is the one value the server cannot use at all (Packets.cs:161 splices it into the
  // packet), and isFaithful calls the repair faithful — so the gate must not fire on it.
  const h = boot({ NPCs: [NPC(1, 'Rat', { equipped_items: '' })] });
  h.get('sheet-picker').value = 'NPCs';
  fire(h.get('sheet-picker'), 'change');
  h.settle();
  fire(h.get('records').children[0], 'click');
  h.settle();
  fire(h.get('save'), 'click');
  h.settle();

  const at = schemaOf('NPCs').columns.findIndex((c) => c.name === 'equipped_items');
  assert.equal(h.writes.length, 1);
  assert.equal(h.writes[0].cells[at], '0,*,0,*,0,*,0,*,0,*,0,*');
});

test('the gate does not fire on a sheet with no EquipSlots composite', () => {
  const h = boot({ Items: [ITEM(1, 'Gold')] });
  fire(h.get('records').children[0], 'click');
  h.settle();
  fire(h.get('save'), 'click');
  h.settle();
  assert.equal(h.writes.length, 1);
});

// --- publish check --------------------------------------------------------------------------

test('the publish check reads every sheet and reports clean data as clean', () => {
  const h = boot({
    Items: [ITEM(1, 'Gold'), ITEM(2, 'Sword')],
    NPCs: [NPC(1, 'Rat')],
    'NPC Drops': [DROP(1, 1), DROP(1, 2)],
  });

  fire(h.get('publish-check'), 'click');
  h.settle();

  assert.equal(serverCalls(h.run, 'readSheet').filter((c) => c.args[0]).length >= 21, true);
  const text = h.get('publish-results').textContent;
  assert.match(text, /All sheets valid/);
});

test('every row of a valid sheet is NOT reported as a duplicate of itself', () => {
  // The hazard: validateRecord with ownId null and __self holding every id flags every row.
  const h = boot({ Items: [ITEM(1, 'Gold'), ITEM(2, 'Sword'), ITEM(3, 'Shield')] });
  fire(h.get('publish-check'), 'click');
  h.settle();
  assert.doesNotMatch(h.get('publish-results').textContent, /already used/);
});

test('a real duplicate id IS reported, naming both rows', () => {
  const h = boot({ Items: [ITEM(1, 'Gold'), ITEM(1, 'Sword')] });
  fire(h.get('publish-check'), 'click');
  h.settle();
  assert.match(h.get('publish-results').textContent,
               /Items row 3: id 1 is already used by row 2/);
});

test('the publish check validates each FK against the sheet it points at', () => {
  // The hazard: reusing the id sets of whatever sheet happens to be open. Items 1 exists and
  // Items 5 does not; NPC Drops points at both NPCs and Items.
  const h = boot({
    Items: [ITEM(1, 'Gold')],
    NPCs: [NPC(1, 'Rat')],
    'NPC Drops': [DROP(1, 1), DROP(1, 5)],
  });
  fire(h.get('publish-check'), 'click');
  h.settle();

  const text = h.get('publish-results').textContent;
  assert.match(text, /NPC Drops row 3: item_template_id = 5 does not exist in Items/);
  assert.doesNotMatch(text, /row 2: item_template_id/);
});

test('a missing required cell is reported with its sheet and row', () => {
  const h = boot({ Items: [rowFor('Items', { item_template_id: 1, item_usetype: 'NoUse' })] });
  fire(h.get('publish-check'), 'click');
  h.settle();
  assert.match(h.get('publish-results').textContent, /Items row 2: item_name is required/);
});

test('a sheet the server refuses is reported rather than aborting the run', () => {
  const h = boot({ Items: [ITEM(1, 'Gold')] }, { failOn: 'Quests' });
  fire(h.get('publish-check'), 'click');
  h.settle();
  const text = h.get('publish-results').textContent;
  assert.match(text, /Quests row -: boom: Quests/);
  // …and the sheets after it were still read.
  assert.equal(serverCalls(h.run, 'readSheet').some((c) => c.args[0] === 'Surnames'), true);
});

test('the restart warning names all twelve sheets, whatever the result', () => {
  const h = boot({ Items: [ITEM(1, 'Gold')] });
  fire(h.get('publish-check'), 'click');
  h.settle();

  const text = h.get('publish-results').textContent;
  assert.equal(Layout.RESTART_ONLY.length, 12);
  assert.match(text, /These 12 sheets need a full server restart/);
  Layout.RESTART_ONLY.forEach((sheet) => assert.ok(text.indexOf(sheet) !== -1, sheet));
  // The four the plan's list forgot, spelled out so a regression is unmissable.
  ['Class Levelup Spells', 'Warptiles', 'Titles', 'Surnames'].forEach((sheet) => {
    assert.ok(text.indexOf(sheet) !== -1, sheet);
  });
});

test('a second publish check cannot start while one is running', () => {
  const h = boot({ Items: [ITEM(1, 'Gold')] });
  fire(h.get('publish-check'), 'click');
  fire(h.get('publish-check'), 'click');   // ignored: the first is still in flight
  h.settle();
  assert.equal(serverCalls(h.run, 'readSheet').filter((c) => c.args[0] === 'Items').length, 2);
});

test('the problem list is capped and says how many are hidden', () => {
  const rows = [];
  for (let i = 0; i < 140; i++) rows.push(rowFor('Items', { item_template_id: i + 1 }));
  const h = boot({ Items: rows });
  fire(h.get('publish-check'), 'click');
  h.settle();

  const panel = h.get('publish-results');
  assert.equal(panel.querySelectorAll('[class="problem"]').length, 100);
  // Two problems per row (item_usetype and item_name are both required, graphic_tile too).
  assert.match(panel.textContent, /more not shown/);
});
