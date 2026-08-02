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
const { ColorPicker } = await import('../src/colorpicker.js');
globalThis.ColorPicker = ColorPicker;
const { Composites } = await import('../src/composites.js');
globalThis.Composites = Composites;
const { Forms } = await import('../src/forms.js');
globalThis.Forms = Forms;
const { Preview } = await import('../src/preview.js');
globalThis.Preview = Preview;
const { Groups } = await import('../src/groups.js');
globalThis.Groups = Groups;

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

const ITEM = (id, name, extra) => rowFor('Items', Object.assign({
  item_template_id: id, item_usetype: 'NoUse', item_name: name, graphic_tile: 1,
}, extra || {}));
const NPC = (id, name, extra) => rowFor('NPCs', Object.assign({
  npc_id: id, npc_name: name, npc_type: 'Monster', body_id: 1, body_state: 1,
  equipped_items: '0,*,0,*,0,*,0,*,0,*,0,*',
}, extra || {}));
const MAP = (id, name) => rowFor('Maps', { map_id: id, map_name: name, map_filename: name + '.map' });
const COMBO = (sheet, comboId, itemId) => rowFor(sheet,
  { combination_id: comboId, item_template_id: itemId });
const DROP = (npcId, itemId) => rowFor('NPC Drops', {
  npc_template_id: npcId, item_template_id: itemId, stack: 1, droprate: '0.10',
});

// The default server: every sheet empty unless `sheets` names it. writeRow records what it was
// asked to write and answers the way Code.gs does, with the row it landed on.
function makeServer(sheets, options) {
  const opts = options || {};
  const writes = [];
  const batches = [];

  // Per-call answers for one sheet, so two requests for the SAME sheet are distinguishable —
  // which is what separates "guard by generation" from "guard by sheet name".
  const perCall = opts.perCall || {};
  const seen = {};
  const seenIndex = {};

  const server = {
    readSheet(name) {
      if (opts.failOn === name) throw new Error('boom: ' + name);
      let rows = sheets[name] || [];
      if (perCall[name]) {
        const at = seen[name] || 0;
        seen[name] = at + 1;
        rows = perCall[name][Math.min(at, perCall[name].length - 1)];
      }
      return {
        sheet: name,
        header: schemaOf(name).columns.map((c) => c.name),
        rows,
        lastRow: rows.length + 1,
      };
    },
    readSheetIndex(name, nameColumnIndex, extraColumnIndex) {
      // Fails a NAMED sheet's id + name list while every other one loads — the shape of a
      // transient Apps Script error, and the shape that used to leave fk validation open.
      // `indexFailsOnce` fails the first request and answers the retry, which is the case a
      // retry has to be told apart from a permanent failure.
      if (opts.failIndexOn === name) {
        if (opts.indexFailsOnce && seenIndex[name]) { /* fall through: the retry succeeds */ }
        else { seenIndex[name] = true; throw new Error('index boom: ' + name); }
      }
      const rows = sheets[name] || [];
      const at = typeof nameColumnIndex === 'number' && nameColumnIndex >= 1 ? nameColumnIndex : 1;
      const xat = typeof extraColumnIndex === 'number' && extraColumnIndex >= 0
        ? extraColumnIndex : -1;
      return {
        sheet: name,
        entries: rows.filter((r) => String(r[0]).trim() !== '')
          .map((r) => {
            const entry = { id: r[0], name: r[at] };
            if (xat >= 0) entry.extra = r[xat];
            return entry;
          }),
      };
    },
    writeRow(sheet, rowNumber, cells, idColumnIndex, options) {
      writes.push({ sheet, rowNumber, cells, idColumnIndex, options });
      const target = Number(rowNumber) > 0 ? Number(rowNumber) : (sheets[sheet] || []).length + 2;
      if (opts.writeFails) throw new Error('write refused');
      return { row: target };
    },
    // The grouped-sheet path's one write call: a whole sheet's edits, appends and deletes in a
    // single request. Recorded whole, so a test can assert on the SHAPE of the batch and not
    // only on the fact that something was saved.
    saveBatch(batch) {
      if (opts.saveBatchFails) throw new Error('batch boom');
      batches.push(batch);
      return batch.map((entry) => ({
        sheet: entry.sheet,
        written: (entry.writes || []).length,
        appended: (entry.appends || []).length,
        deleted: (entry.deletes || []).length,
      }));
    },
  };

  return { server, writes, batches };
}

// The page, as Editor.html lays it out.
function buildShell() {
  const doc = installFakeDom();
  ['sheet-picker', 'records', 'form', 'previews', 'publish-results', 'status',
   'new-record', 'save', 'publish-check', 'modal'].forEach((id) => {
    const tag = id === 'sheet-picker' ? 'select'
      : (['new-record', 'save', 'publish-check'].indexOf(id) !== -1 ? 'button'
        : (id === 'status' ? 'span' : 'div'));
    const node = doc.createElement(tag);
    node.id = id;
    // As Editor.html ships it: the modal backdrop starts hidden and empty.
    if (id === 'modal') node.hidden = true;
    doc.body.appendChild(node);
  });
  return doc;
}

// Boots the app the way the page does. Returns the handles a test needs to drive it.
function boot(sheets, options) {
  const opts = options || {};
  const doc = buildShell();
  const img = installFakeImage();
  const { server, writes, batches } = makeServer(sheets || {}, options);
  const run = installGoogleScriptRun(server);

  Object.assign(App.__state, {
    schema: null, sheetName: null, rows: [], rowNumber: 0, ids: [], bundleErrors: [],
    // Reset like every other accumulator: a sheet that failed to load in one test would
    // otherwise still be distrusted in the next, and the save gate would refuse a save the
    // test never broke.
    refErrors: [], retrying: false,
    idSets: {}, pickerData: {}, bundles: {}, images: {}, imageCallbacks: [], formCallbacks: [],
    loaded: {}, stopEffect: null, previewKey: null, checking: false,
    sheetToken: 0, formToken: 0, saving: false, formPending: false, loading: {},
    // The grouped-sheet bookkeeping, reset for the same reason as the rest: a group left open by
    // one test would otherwise let the next one's save collect rows it never built.
    groupToken: 0, group: null, groups: [], groupsReady: false,
    reopenGroup: null, pendingStatus: null,
  });

  App.init();

  const handles = {
    doc, img, run, writes, batches,
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

  // Spell Effects alone is asked for one EXTRA cell — spell_animation, the id the effects atlas
  // is actually keyed by, which the Spells preview resolves spell_effect_id through.
  const animationAt = schemaOf('Spell Effects').columns
    .map((c) => c.name).indexOf('spell_animation');
  assert.ok(animationAt > 0, 'the schema still has spell_animation');
  assert.ok(calls.some((a) => a[0] === 'Spell Effects' && a[2] === animationAt),
    'Spell Effects requested with the spell_animation column');
  assert.ok(calls.filter((a) => a[0] !== 'Spell Effects').every((a) => a[2] === -1),
    'no other sheet asks for an extra column');
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
  // Items joined this list when graphic_equip gained a preview: it has neither body_id nor an
  // EquipSlots composite, but its worn preview and its partControl both draw a character sprite,
  // so without parts they would sit blank with "no character art loaded" forever.
  assert.deepEqual(App.bundlesFor(schemaOf('Items')), ['icons', 'parts']);
  assert.deepEqual(App.bundlesFor(schemaOf('NPCs')), ['icons', 'parts']);
  assert.deepEqual(App.bundlesFor(schemaOf('Spells')), ['icons', 'effects']);
  assert.deepEqual(App.bundlesFor(schemaOf('Spell Effects')), ['icons', 'parts', 'effects']);
  assert.deepEqual(App.bundlesFor(schemaOf('NPC Drops')), ['icons']);
});

test('the form renders as soon as icons has decoded, without waiting on the parts bundle', () => {
  // THE COST OF THE OTHER ORDER. Waiting on every bundle put a second multi-megabyte PNG decode
  // (parts, 1.98MB, on top of icons' 1.75MB) in front of the FIRST FIELD of the first Items record
  // of a session — the most-used sheet, and normally the first one opened. The art is not the
  // urgent half: the fields are, and every control that needs a sprite subscribes to
  // onImagesReady, so parts landing afterwards fills its canvas in place.
  const h = boot({ Items: [ITEM(1, 'Gold', { graphic_equip: 5, item_slot: 'Helmet' })] });

  fire(h.get('records').children[0], 'click');
  // The server reply only: the record's values are in hand, and icons decoded during init.
  h.run.flush();

  assert.ok(!App.__state.images.parts, 'the parts bundle is still decoding');
  assert.ok(h.get('form').children.length > 0, 'and the form is already on screen');
  // walk(), not a class selector: the fake DOM parses attribute selectors only.
  const status = () => walk(h.get('form').querySelector('[name="graphic_equip"]').parentNode)
    .filter((n) => /^status/.test(n.getAttribute('class') || ''))[0].textContent;
  assert.match(status(), /no character art loaded/,
               'the one canvas that needs parts says so rather than the form being absent');

  const before = h.get('form').children.length;
  h.settle();
  assert.ok(App.__state.images.parts, 'parts decoded');
  assert.equal(h.get('form').children.length, before, 'the same form, not a second render');
  assert.ok(!/no character art loaded/.test(status()),
            'and the late bundle reached the control through imagesChanged');
});

test('a late parts bundle reaches the preview panel too, not only the form controls', () => {
  // The panel canvases cannot subscribe to onImagesReady — renderPreviews runs on every keystroke,
  // so a registration inside it would stack one callback per edit — so renderForm pushes the late
  // bundle to them instead. Without that the worn preview stays blank until the next keystroke.
  // Armor, because only a wearable record draws the worn canvas at all.
  const h = boot({ Items: [ITEM(1, 'Gold',
    { item_usetype: 'Armor', graphic_equip: 1, item_slot: 'Chest' })] });

  fire(h.get('records').children[0], 'click');
  h.run.flush();
  const wornCanvas = () => walk(h.doc.getElementById('previews'))
    .filter((n) => n.getAttribute('class') === 'worn')[0];
  const draws = () => wornCanvas().getContext('2d').calls
    .filter((c) => c[0] === 'drawImage').length;
  assert.equal(draws(), 0, 'nothing to draw with yet');

  h.settle();
  assert.ok(draws() > 0, 'the base body was drawn once parts landed');
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

test('a failed bundle decode is cached — record opens render at once, with no re-decode', () => {
  const h = boot({ Items: [ITEM(1, 'Gold'), ITEM(2, 'Sword')] }, { hold: true });
  assert.equal(h.img.fail(), 1);       // the boot decode of icons fails
  h.settle();

  // The failure is a RESULT: the form renders synchronously off it, which both spares a
  // multi-megabyte decode per record open and closes the async window in which rowNumber
  // already names the new record while the DOM still shows the old one.
  fire(h.get('records').children[0], 'click');
  assert.ok(h.get('form').children.length > 0, 'the form rendered without waiting');
  assert.equal(h.img.pending.length, 1, 'only the parts bundle is decoding');
  assert.match(h.img.pending[0].src, /PARTS/);

  fire(h.get('records').children[1], 'click');
  assert.equal(h.get('form').querySelector('[name="item_name"]').value, 'Sword');
  assert.equal(h.img.pending.length, 1, 'and no second icons decode was ever queued');
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
  assert.equal(canvas.width, Preview.CANVAS_W * Preview.CHARACTER_SCALE);
  assert.equal(canvas.height, Preview.CANVAS_H * Preview.CHARACTER_SCALE);
  assert.equal(canvas.getContext('2d').calls.filter((c) => c[0] === 'drawImage').length, 1);
});

test('the three NPC appearance ids get a part preview each, and no item pair in the panel', () => {
  // They are Int columns with no composite, so without Layout.PART_GRAPHICS they were text boxes
  // with nothing to look at and no way to browse the art.
  const h = boot({ NPCs: [NPC(1, 'Rat', { hair_id: 1, face_id: 1 })] });
  h.get('sheet-picker').value = 'NPCs';
  fire(h.get('sheet-picker'), 'change');
  h.settle();
  fire(h.get('records').children[0], 'click');
  h.settle();

  ['body_id', 'hair_id', 'face_id'].forEach((name) => {
    const row = h.get('form').querySelector('[name="' + name + '"]').parentNode;
    const canvas = row.querySelector('[class="preview"]');
    assert.ok(canvas, name + ' has no preview canvas');
    // Clickable, and it is what opens the browser (pickers.js owns which folder).
    assert.equal(canvas.getAttribute('role'), 'button');
    assert.equal(canvas.getAttribute('aria-haspopup'), 'dialog');
  });

  // THE PANEL IS UNCHANGED: one character canvas. The item-icon + worn pair belongs to a sheet
  // whose worn sprite hangs off another cell (Items), and these three ids are layers of the
  // character the panel already draws.
  assert.deepEqual(h.get('previews').children.map((n) => n.className), ['appearance']);
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

test('a blank body_state previews the pose the row will IMPORT as, not a bare 0', () => {
  // The reported bug. A blank cell is skipped on import (CsvToSqlBase.cs:27) and the column
  // default lands — body_state defaults to 3 on both sheets that have it, which is UNARMED — but
  // the panel read the raw cell, got 0, and 0 !== 3 is armed. Every unfilled NPC previewed in the
  // wrong pose, and nothing on screen said so.
  const saved = globalThis.GOOSE_SPRITES.parts.rects;
  try {
    globalThis.GOOSE_SPRITES.parts.rects = {
      'Bodies:1:idle-equip-down': [0, 0, 48, 48],
      'Bodies:1:idle-no-equip-down': [64, 0, 48, 48],
    };
    const h = boot({ NPCs: [NPC(1, 'Unset', { body_state: '' })] });
    h.get('sheet-picker').value = 'NPCs';
    fire(h.get('sheet-picker'), 'change');
    h.settle();
    fire(h.get('records').children[0], 'click');
    h.settle();

    const drawn = h.get('previews').children[0].getContext('2d').calls
      .filter((c) => c[0] === 'drawImage');
    assert.equal(drawn[0][2], 64, 'a blank body_state is the unarmed default');
    // The CELL is untouched: blank still means "use the SQL default" on the next save.
    assert.equal(h.get('form').querySelector('[name="body_state"]').value, '');
  } finally {
    globalThis.GOOSE_SPRITES.parts.rects = saved;
  }
});

test('a blank body_id previews the default body rather than nothing', () => {
  // Same rule, the other cell: body_id defaults to 1, so the row imports as a player body. Read
  // raw it was 0, which Appearance.layers drops entirely — an empty canvas that reads as missing
  // art rather than as an unfilled cell.
  const saved = globalThis.GOOSE_SPRITES.parts.rects;
  try {
    globalThis.GOOSE_SPRITES.parts.rects = { 'Bodies:1:idle-no-equip-down': [64, 0, 48, 48] };
    const h = boot({ NPCs: [NPC(1, 'Unset', { body_id: '', body_state: 3 })] });
    h.get('sheet-picker').value = 'NPCs';
    fire(h.get('sheet-picker'), 'change');
    h.settle();
    fire(h.get('records').children[0], 'click');
    h.settle();

    const drawn = h.get('previews').children[0].getContext('2d').calls
      .filter((c) => c[0] === 'drawImage');
    assert.equal(drawn.length, 1, 'the default body is drawn');
    assert.equal(drawn[0][2], 64);
    assert.equal(h.get('form').querySelector('[name="body_id"]').value, '');
  } finally {
    globalThis.GOOSE_SPRITES.parts.rects = saved;
  }
});

test('Items draws no NPC character preview — it draws its own two canvases', () => {
  // Items has no body_id, so the body_id branch must not fire. What it gets instead is the item
  // pair: the inventory icon and — for a wearable usetype — the worn sprite.
  const h = boot({ Items: [ITEM(1, 'Gold', { item_usetype: 'Armor' })] });
  fire(h.get('records').children[0], 'click');
  h.settle();

  const classes = h.get('previews').children.map((n) => n.className);
  assert.deepEqual(classes, ['item-icon', 'worn']);
  assert.equal(classes.indexOf('appearance'), -1, 'not the NPC character preview');
});

test('an unwearable usetype gets the icon alone — no worn canvas, no equip fields', () => {
  // A NoUse item is never drawn on a character, so the worn-character canvas and the
  // graphic_equip / item_slot rows are noise; the cells still round-trip verbatim underneath.
  const h = boot({ Items: [ITEM(1, 'Gold', { graphic_equip: 5, item_slot: 'Helmet' })] });
  fire(h.get('records').children[0], 'click');
  h.settle();

  assert.deepEqual(h.get('previews').children.map((n) => n.className), ['item-icon']);
  const rowOf = (name) => {
    let n = h.get('form').querySelector('[name="' + name + '"]');
    while (n && n.className !== 'field') n = n.parentNode;
    return n;
  };
  assert.equal(rowOf('graphic_equip').hidden, true);
  assert.equal(rowOf('item_slot').hidden, true);
  assert.equal(rowOf('item_name').hidden, false, 'only the equip rows are gated');

  // Flipping the usetype to Armor brings both back, live, with their stored values intact.
  const usetype = h.get('form').querySelector('[name="item_usetype"]');
  usetype.value = 'Armor';
  fire(usetype, 'change');
  assert.equal(rowOf('graphic_equip').hidden, false);
  assert.equal(rowOf('item_slot').hidden, false);
  assert.equal(h.get('form').querySelector('[name="graphic_equip"]').value, '5');
  assert.deepEqual(h.get('previews').children.map((n) => n.className), ['item-icon', 'worn']);
});

// --- the Items preview panel -----------------------------------------------------------------

// An item that resolves in the test bundle both ways: icon 20107:810003 and a Helms sprite.
const WEARABLE = (values) => rowFor('Items', Object.assign({
  item_template_id: 1, item_usetype: 'Armor', item_name: 'Helm', item_slot: 'Helmet',
  graphic_tile: 810003, graphic_file: 20107, graphic_equip: 5, body_state: 1,
}, values || {}));

// The extra rects the Items panel needs, installed around one test at a time so the shared bundle
// stays what every other test in this file expects.
function withItemArt(body) {
  const icons = globalThis.GOOSE_SPRITES.icons.rects;
  const parts = globalThis.GOOSE_SPRITES.parts.rects;
  globalThis.GOOSE_SPRITES.icons.rects = Object.assign({ '20107:810003': [0, 0, 16, 16] }, icons);
  globalThis.GOOSE_SPRITES.parts.rects = Object.assign({ 'Helms:5:idle-down': [10, 0, 24, 32] },
    parts);
  try { body(); } finally {
    globalThis.GOOSE_SPRITES.icons.rects = icons;
    globalThis.GOOSE_SPRITES.parts.rects = parts;
  }
}

function openItem(row) {
  const h = boot({ Items: [row] });
  fire(h.get('records').children[0], 'click');
  h.settle();
  return h;
}

const drewOn = (canvas) => canvas.getContext('2d').calls.filter((c) => c[0] === 'drawImage');

test('an Items record draws the icon and the worn sprite, in that order', () => {
  withItemArt(() => {
    const h = openItem(WEARABLE());
    const [icon, worn] = h.get('previews').children;

    assert.equal(icon.className, 'item-icon');
    assert.equal(icon.width, Preview.ICON_BOX * Preview.ICON_SCALE);
    assert.equal(drewOn(icon).length, 1, 'the inventory tile');

    assert.equal(worn.className, 'worn');
    assert.equal(worn.width, Preview.CANVAS_W * Preview.CHARACTER_SCALE);
    // Body 1, its underwear legs (no art in this bundle), and the helmet.
    assert.deepEqual(drewOn(worn).map((c) => c[2]), [0, 10]);
  });
});

test('a Quests record draws neither of them', () => {
  // The branch is keyed on the sheet, so a sheet with none of these columns must not reach it —
  // Forms.collect would hand it blanks and the panel would show a bare body for no reason.
  const h = boot({ Quests: [rowFor('Quests', { quest_id: 1, quest_name: 'Errand' })] });
  h.get('sheet-picker').value = 'Quests';
  fire(h.get('sheet-picker'), 'change');
  h.settle();
  fire(h.get('records').children[0], 'click');
  h.settle();
  assert.deepEqual(h.get('previews').children.map((n) => n.className), []);
});

test('the worn canvas is there even for an item nothing wears', () => {
  // A Misc item still gets the panel; the worn canvas shows the bare body, which reads as "not
  // worn" where a missing canvas would read as a broken preview.
  withItemArt(() => {
    const h = openItem(WEARABLE({ item_slot: 'Misc' }));
    const [icon, worn] = h.get('previews').children;
    assert.equal(drewOn(icon).length, 1, 'the icon is unaffected by the slot');
    assert.deepEqual(drewOn(worn).map((c) => c[2]), [0], 'the body alone');
  });
});

// Driving the colour picker THE WAY A USER DOES. Every write path in that control is a
// mousedown/mousemove drag, an arrow key on a focused strip, or a click on a recent chip — the
// browser turns none of those into a bubbling input/change, so the picker dispatches one itself
// and these helpers are what prove it arrives. This test used to set the hidden graphic_r cell by
// hand and fire `change` ON A HIDDEN INPUT: an event no browser produces and nothing in the
// editor dispatches, so it passed while the real path left every preview stale.
function openPopover(host) {
  fire(host.querySelector('[class="swatch"]'), 'click');
}

// A 255-tall strip with full blend at the top, so the click lands on a whole byte.
function dragBlend(host, blend) {
  const strip = host.querySelector('[class="cp-alpha"]');
  strip.rect = { left: 0, top: 0, width: 14, height: 255 };
  fire(strip, 'mousedown', { clientX: 0, clientY: 255 - blend });
}

function tintedDraws(canvas) {
  // A tinted draw goes through an offscreen canvas: drawImage(node, dx, dy) — three arguments
  // plus the method name. An untinted one passes the source rect too.
  return drewOn(canvas).filter((c) => c.length === 4).length;
}

test('DRAGGING a tint rebuilds the Items panel', () => {
  // The complaint this task exists for: graphic_r/g/b/a visibly affected nothing.
  withItemArt(() => {
    const h = openItem(WEARABLE());
    const before = h.get('previews').children[0];
    // .preview is graphicControl's own canvas, beside the graphic_tile field.
    const tile = h.get('form').querySelector('[class="preview"]');
    const tileTintsBefore = tintedDraws(tile);

    const rgba = h.get('form').querySelector('[class="rgba"]');
    openPopover(rgba);
    dragBlend(rgba, 255);

    assert.equal(h.get('form').querySelector('[name="graphic_a"]').value, '255',
      'the drag wrote the cell');

    const [icon, worn] = h.get('previews').children;
    assert.notEqual(icon, before, 'the panel was rebuilt');
    assert.equal(tintedDraws(icon), 1, 'the tile is tinted');
    assert.equal(tintedDraws(worn), 1, 'and so is the helmet');
    // The graphic_tile control's own canvas, which redraws through state.formCallbacks rather
    // than through the panel. Same delegated event, second consumer.
    assert.ok(tintedDraws(tile) > tileTintsBefore, 'the control canvas followed the tint');
  });
});

test('an ARROW KEY on the blend strip rebuilds the Items panel too', () => {
  // The keyboard path writes the same cells from a `keydown`, which bubbles but is not an edit
  // event — the delegated listener does not hear it, so it needs the same dispatch the drag does.
  withItemArt(() => {
    const h = openItem(WEARABLE({ graphic_r: 255, graphic_a: 100 }));
    const before = h.get('previews').children[0];

    const rgba = h.get('form').querySelector('[class="rgba"]');
    openPopover(rgba);
    fire(rgba.querySelector('[class="cp-alpha"]'), 'keydown', { key: 'ArrowUp' });

    assert.equal(h.get('form').querySelector('[name="graphic_a"]').value, '101');
    assert.notEqual(h.get('previews').children[0], before, 'the panel was rebuilt');
    assert.equal(tintedDraws(h.get('previews').children[0]), 1, 'and still tinted');
  });
});

test('changing item_slot moves the worn preview into another folder', () => {
  withItemArt(() => {
    const h = openItem(WEARABLE());
    assert.deepEqual(drewOn(h.get('previews').children[1]).map((c) => c[2]), [0, 10]);

    const slot = h.get('form').querySelector('[name="item_slot"]');
    slot.value = 'Ring';
    fire(slot, 'change');
    assert.deepEqual(drewOn(h.get('previews').children[1]).map((c) => c[2]), [0],
      'a ring is not drawn on the character');
  });
});

// --- previewKey ------------------------------------------------------------------------------

test('previewKey moves for every cell the Items panel reads', () => {
  withItemArt(() => {
    const h = openItem(WEARABLE());
    const form = h.get('form');
    const edit = (name, value) => {
      const node = form.querySelector('[name="' + name + '"]');
      assert.ok(node, name + ' has no control in the form');
      const was = App.__state.previewKey;
      node.value = value;
      fire(node, 'change');
      assert.notEqual(App.__state.previewKey, was, name + ' must move the preview key');
    };

    ['graphic_tile', 'graphic_file', 'graphic_equip', 'graphic_r', 'graphic_g', 'graphic_b',
     'graphic_a', 'body_state'].forEach((name, i) => edit(name, String(11 + i)));
    edit('item_slot', 'Shoes');
  });
});

test('previewKey ignores the cells no preview reads', () => {
  // The key is what stops a keystroke in item_description from rebuilding two canvases forty
  // times a second, so a column added to it by accident is a real cost.
  withItemArt(() => {
    const h = openItem(WEARABLE());
    const form = h.get('form');
    ['item_name', 'item_description', 'item_value', 'weapon_damage', 'stat_str', 'min_level']
      .forEach((name) => {
        const node = form.querySelector('[name="' + name + '"]');
        assert.ok(node, name + ' has no control in the form');
        const was = App.__state.previewKey;
        // Node IDENTITY, not the array: renderPreviews empties the host and builds fresh canvases,
        // so the same node still being there is the proof that it did not run.
        const icon = h.get('previews').children[0];
        node.value = 'x1';
        fire(node, 'change');
        assert.equal(App.__state.previewKey, was, name + ' must not move the preview key');
        assert.equal(h.get('previews').children[0], icon, name + ' rebuilt the panel');
      });
  });
});

test('a composite change redraws the preview', () => {
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

test('COLOURING an NPC slot updates the character panel', () => {
  // The other half of the stale-preview bug. The slot row redraws its own 80x112 canvas from
  // inside onChange, so the row looked right and the 384x448 character panel beside it did not
  // move — the drag on the blend strip reaches it only through the dispatched `input`.
  withItemArt(() => {
    const h = boot({ NPCs: [NPC(1, 'Rat', { equipped_items: '0,*,5,*,0,*,0,*,0,*,0,*' })] });
    h.get('sheet-picker').value = 'NPCs';
    fire(h.get('sheet-picker'), 'change');
    h.settle();
    fire(h.get('records').children[0], 'click');
    h.settle();

    const before = h.get('previews').children[0];
    assert.equal(tintedDraws(before), 0, 'the helm starts untinted');

    const row = h.get('form').querySelectorAll('[class="equip-slot"]')[1];
    openPopover(row);
    dragBlend(row, 200);

    assert.match(h.get('form').querySelector('[name="equipped_items"]').value,
      /^0,\*,5,\d+,\d+,\d+,200,/, 'the drag wrote the Helm slot');
    const after = h.get('previews').children[0];
    assert.notEqual(after, before, 'the character panel was rebuilt');
    assert.equal(tintedDraws(after), 1, 'and the helm is drawn tinted');
  });
});

test('typing in body_id updates the character preview live (smoke items 12 and 13)', () => {
  // body_id, hair_id, face_id and body_state belong to NO composite on NPCs — they are plain
  // text inputs, with no control-specific wiring. Without a delegated listener on the form,
  // typing 150 here changes nothing until the record is saved and re-opened.
  const h = boot({ NPCs: [NPC(1, 'Rat', { hair_id: 5 })] });
  h.get('sheet-picker').value = 'NPCs';
  fire(h.get('sheet-picker'), 'change');
  h.settle();
  fire(h.get('records').children[0], 'click');
  h.settle();

  const layers = () => {
    const calls = h.get('previews').children[0].getContext('2d').calls;
    return calls.filter((c) => c[0] === 'drawImage').length;
  };
  assert.equal(layers(), 1, 'body 1 has art; hair 5 and the underwear legs do not');

  const bodyId = h.get('form').querySelector('[name="body_id"]');
  bodyId.value = '150';
  fire(bodyId, 'input');

  // A monster body draws alone — and body 150 has no art in the test bundle, so the proof is
  // that the canvas is a NEW one showing nothing, not the old one still showing body 1.
  assert.equal(layers(), 0);
  assert.equal(App.__state.previewKey.indexOf('body_id=150') !== -1, true);
});

test('a value committed with `change` alone still refreshes the preview', () => {
  // Both event types are delegated. `input` covers typing; `change` covers what a <select>, an
  // autofill or a paste-then-blur commits — a browser fires `change` there whether or not an
  // `input` preceded it, and a handler wired for only one of the two silently misses it.
  const h = boot({ NPCs: [NPC(1, 'Rat')] });
  h.get('sheet-picker').value = 'NPCs';
  fire(h.get('sheet-picker'), 'change');
  h.settle();
  fire(h.get('records').children[0], 'click');
  h.settle();

  const before = h.get('previews').children[0];
  const bodyId = h.get('form').querySelector('[name="body_id"]');
  bodyId.value = '150';
  fire(bodyId, 'change');

  assert.notEqual(h.get('previews').children[0], before);
  assert.ok(App.__state.previewKey.indexOf('body_id=150') !== -1);
});

test('the delegated listeners are registered once, not once per record opened', () => {
  // The form container outlives every record — Forms.render empties it rather than replacing
  // it — so a per-render registration would stack a handler for every record ever opened.
  const h = boot({ Items: [ITEM(1, 'Gold'), ITEM(2, 'Sword')] });
  const form = h.get('form');
  const count = () => form._listeners.get('input').length + form._listeners.get('change').length;

  fire(h.get('records').children[0], 'click');
  h.settle();
  const first = count();

  for (let i = 0; i < 5; i++) {
    fire(h.get('records').children[i % 2], 'click');
    h.settle();
  }
  assert.equal(count(), first);
  assert.equal(first, 2);
});

test('a field no preview reads does not rebuild the preview', () => {
  // Content-keyed, so the two redraw paths cannot double-fire for one keystroke either.
  const h = boot({ NPCs: [NPC(1, 'Rat')] });
  h.get('sheet-picker').value = 'NPCs';
  fire(h.get('sheet-picker'), 'change');
  h.settle();
  fire(h.get('records').children[0], 'click');
  h.settle();

  const before = h.get('previews').children[0];
  const name = h.get('form').querySelector('[name="npc_name"]');
  name.value = 'Giant Rat';
  fire(name, 'input');
  assert.equal(h.get('previews').children[0], before);
});

test('a composite edit redraws exactly once, not twice', () => {
  // A slot edit redraws the row's own canvas directly AND bubbles to the delegated listener.
  // Both ask for a redraw of the panel; only one may happen.
  const h = boot({ NPCs: [NPC(1, 'Rat')] });
  h.get('sheet-picker').value = 'NPCs';
  fire(h.get('sheet-picker'), 'change');
  h.settle();
  fire(h.get('records').children[0], 'click');
  h.settle();

  let renders = 0;
  const host = h.get('previews');
  const realAppend = host.appendChild.bind(host);
  host.appendChild = (child) => { if (child.tagName === 'CANVAS') renders++; return realAppend(child); };

  const slot = h.get('form').querySelectorAll('[class="slot-graphic"]')[0];
  slot.value = '1';
  fire(slot, 'input');
  assert.equal(renders, 1);
});

test('a rejected equip-slot typo does NOT redraw the preview', () => {
  // The `input` still bubbles — the cell is what did not change, so previewKey is unmoved and
  // renderPreviews short-circuits. A typo must not rebuild the panel.
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

// The canvas size and the scale the context is given are two expressions that must agree: a
// canvas sized at 4x whose context is scaled 1x is a small sprite adrift in a big box, and
// nothing about the drawing itself looks wrong. Sprites.scaled now derives the size FROM the
// scale, so they cannot disagree — these assert that from the outside, on the real app path.
test('the character preview canvas is sized by the scale its context is given', () => {
  const h = boot({ NPCs: [NPC(1, 'Rat')] });
  h.get('sheet-picker').value = 'NPCs';
  fire(h.get('sheet-picker'), 'change');
  h.settle();
  fire(h.get('records').children[0], 'click');
  h.settle();

  const canvas = h.get('previews').children[0];
  const transform = canvas.getContext('2d').calls.find((c) => c[0] === 'setTransform');
  const scale = transform[1];
  assert.equal(scale, Preview.CHARACTER_SCALE);
  assert.deepEqual(transform, ['setTransform', scale, 0, 0, scale, 0, 0]);
  assert.equal(canvas.width, Preview.CANVAS_W * scale);
  assert.equal(canvas.height, Preview.CANVAS_H * scale);
});

test('the effect preview canvas is sized by the scale its context is given', () => {
  const realSet = globalThis.setInterval;
  const realClear = globalThis.clearInterval;
  globalThis.setInterval = () => 1;
  globalThis.clearInterval = () => {};

  try {
    // The atlas is keyed by ANIMATION id (77), not by the effect row id (4) the Spells record
    // holds — the preview must resolve one to the other through the Spell Effects list.
    globalThis.GOOSE_SPRITES.effects.rects = { '77:0': [0, 0, 16, 16] };
    const h = boot({
      Spells: [rowFor('Spells', { spell_id: 1, spell_name: 'Fire', spell_target: 'Self',
                                  spellbook_graphic: 1, spell_effect_id: 4 })],
      'Spell Effects': [rowFor('Spell Effects', {
        spell_effect_id: 4, spell_effect_name: 'Flame', effect_type: 'Instant',
        spell_effected: 'Anyone', spell_animation: 77,
      })],
    });

    h.get('sheet-picker').value = 'Spells';
    fire(h.get('sheet-picker'), 'change');
    h.settle();
    fire(h.get('records').children[0], 'click');
    h.settle();

    const canvas = h.get('previews').querySelector('[class="effect"]');
    const transform = canvas.getContext('2d').calls.find((c) => c[0] === 'setTransform');
    const scale = transform[1];
    assert.equal(scale, Preview.EFFECT_SCALE);
    assert.deepEqual(transform, ['setTransform', scale, 0, 0, scale, 0, 0]);
    assert.equal(canvas.width, Preview.EFFECT_SIZE * scale);
    assert.equal(canvas.height, Preview.EFFECT_SIZE * scale);
  } finally {
    globalThis.GOOSE_SPRITES.effects.rects = {};
    globalThis.setInterval = realSet;
    globalThis.clearInterval = realClear;
  }
});

test('changing spell_effect_id restarts the effect animation live', () => {
  const realSet = globalThis.setInterval;
  const realClear = globalThis.clearInterval;
  const live = new Set();
  let next = 1;
  globalThis.setInterval = () => { live.add(next); return next++; };
  globalThis.clearInterval = (id) => { live.delete(id); };

  try {
    globalThis.GOOSE_SPRITES.effects.rects = { '77:0': [0, 0, 16, 16], '88:0': [16, 0, 16, 16] };
    const h = boot({
      Spells: [rowFor('Spells', { spell_id: 1, spell_name: 'Fire', spell_target: 'Self',
                                  spellbook_graphic: 1, spell_effect_id: 4 })],
      'Spell Effects': [rowFor('Spell Effects', {
        spell_effect_id: 4, spell_effect_name: 'Flame', effect_type: 'Instant',
        spell_effected: 'Anyone', spell_animation: 77,
      }), rowFor('Spell Effects', {
        spell_effect_id: 9, spell_effect_name: 'Frost', effect_type: 'Instant',
        spell_effected: 'Anyone', spell_animation: 88,
      })],
    });

    h.get('sheet-picker').value = 'Spells';
    fire(h.get('sheet-picker'), 'change');
    h.settle();
    fire(h.get('records').children[0], 'click');
    h.settle();
    assert.equal(live.size, 1, 'effect 4 is animating');
    const first = [...live][0];

    // Retargeting the field must stop the old animation and start the new one — which only
    // happens if spell_effect_id is part of what the preview watches.
    const field = h.get('form').querySelector('[name="spell_effect_id"]');
    field.value = '9';
    fire(field, 'input');

    assert.equal(live.size, 1, 'exactly one animation is running');
    assert.equal(live.has(first), false, 'and it is not the old one');
    // The new canvas is drawing effect 9's frame, at sx 16.
    const calls = h.get('previews').children[0].getContext('2d').calls;
    assert.equal(calls.filter((c) => c[0] === 'drawImage')[0][2], 16);
  } finally {
    globalThis.GOOSE_SPRITES.effects.rects = {};
    globalThis.setInterval = realSet;
    globalThis.clearInterval = realClear;
  }
});

test('the Spells preview draws the row\'s ANIMATION, not the row id, when both exist in the atlas', () => {
  // The regression this pins: spell_effect_id is the pk of a Spell Effects ROW, and the atlas is
  // keyed by animation id. The two spaces overlap at small numbers, and drawing the row id used
  // to play a completely unrelated animation — worse than a blank panel, because it tells a
  // designer their correct id is wrong.
  const realSet = globalThis.setInterval;
  const realClear = globalThis.clearInterval;
  globalThis.setInterval = () => 1;
  globalThis.clearInterval = () => {};

  try {
    // Animation 4 EXISTS (at sx 0) — the collision — but row 4's animation is 77 (at sx 16).
    globalThis.GOOSE_SPRITES.effects.rects = { '4:0': [0, 0, 16, 16], '77:0': [16, 0, 16, 16] };
    const h = boot({
      Spells: [rowFor('Spells', { spell_id: 1, spell_name: 'Fire', spell_target: 'Self',
                                  spellbook_graphic: 1, spell_effect_id: 4 })],
      'Spell Effects': [rowFor('Spell Effects', {
        spell_effect_id: 4, spell_effect_name: 'Flame', effect_type: 'Instant',
        spell_effected: 'Anyone', spell_animation: 77,
      })],
    });

    h.get('sheet-picker').value = 'Spells';
    fire(h.get('sheet-picker'), 'change');
    h.settle();
    fire(h.get('records').children[0], 'click');
    h.settle();

    const calls = h.get('previews').querySelector('[class="effect"]').getContext('2d').calls;
    assert.equal(calls.filter((c) => c[0] === 'drawImage')[0][2], 16,
      'the frame drawn is animation 77\'s, not animation 4\'s');
  } finally {
    globalThis.GOOSE_SPRITES.effects.rects = {};
    globalThis.setInterval = realSet;
    globalThis.clearInterval = realClear;
  }
});

test('a spell_effect_id naming no known row shows no effect panel rather than a guess', () => {
  globalThis.GOOSE_SPRITES.effects.rects = { '4:0': [0, 0, 16, 16] };
  try {
    const h = boot({
      Spells: [rowFor('Spells', { spell_id: 1, spell_name: 'Fire', spell_target: 'Self',
                                  spellbook_graphic: 1, spell_effect_id: 4 })],
      // No Spell Effects rows at all: the list is empty, so row 4 cannot be resolved.
    });

    h.get('sheet-picker').value = 'Spells';
    fire(h.get('sheet-picker'), 'change');
    h.settle();
    fire(h.get('records').children[0], 'click');
    h.settle();

    assert.equal(h.get('previews').querySelector('[class="effect"]'), null);
  } finally {
    globalThis.GOOSE_SPRITES.effects.rects = {};
  }
});

test('switching SHEETS stops the previous sheet\'s animation and clears the panel', () => {
  const realSet = globalThis.setInterval;
  const realClear = globalThis.clearInterval;
  const live = new Set();
  let next = 1;
  globalThis.setInterval = () => { live.add(next); return next++; };
  globalThis.clearInterval = (id) => { live.delete(id); };

  try {
    globalThis.GOOSE_SPRITES.effects.rects = { '77:0': [0, 0, 16, 16] };
    const h = boot({
      Items: [ITEM(1, 'Gold')],
      Spells: [rowFor('Spells', { spell_id: 1, spell_name: 'Fire', spell_target: 'Self',
                                  spellbook_graphic: 1, spell_effect_id: 4 })],
      'Spell Effects': [rowFor('Spell Effects', {
        spell_effect_id: 4, spell_effect_name: 'Flame', effect_type: 'Instant',
        spell_effected: 'Anyone', spell_animation: 77,
      })],
    });

    h.get('sheet-picker').value = 'Spells';
    fire(h.get('sheet-picker'), 'change');
    h.settle();
    fire(h.get('records').children[0], 'click');
    h.settle();
    assert.equal(live.size, 1);

    // openSheet stops at renderList — no record is selected, so renderPreviews is never
    // reached and cannot be the thing that stops the timer.
    h.get('sheet-picker').value = 'Items';
    fire(h.get('sheet-picker'), 'change');
    h.settle();

    assert.equal(live.size, 0, 'the animation stopped');
    assert.equal(h.get('previews').children.length, 0, 'and its canvas is gone');
  } finally {
    globalThis.GOOSE_SPRITES.effects.rects = {};
    globalThis.setInterval = realSet;
    globalThis.clearInterval = realClear;
  }
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
    globalThis.GOOSE_SPRITES.effects.rects = { '77:0': [0, 0, 16, 16] };
    const h = boot({
      Spells: [
        rowFor('Spells', { spell_id: 1, spell_name: 'Fire', spell_target: 'Self',
                           spellbook_graphic: 1, spell_effect_id: 4 }),
        rowFor('Spells', { spell_id: 2, spell_name: 'Nothing', spell_target: 'Self',
                           spellbook_graphic: 1, spell_effect_id: 0 }),
      ],
      'Spell Effects': [rowFor('Spell Effects', {
        spell_effect_id: 4, spell_effect_name: 'Flame', effect_type: 'Instant',
        spell_effected: 'Anyone', spell_animation: 77,
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

test('save sends the loaded snapshot and the Text column indexes to writeRow', () => {
  // The snapshot is what lets writeRow tell the user's edit from another editor's: without it a
  // whole-record post silently reverts every cell someone else changed during the edit. The Text
  // indexes are what let it pin '@' so "1-2" stays text rather than becoming a Date.
  const h = boot({ Items: [ITEM(1, 'Gold'), ITEM(2, 'Sword')] });
  fire(h.get('records').children[1], 'click');
  h.settle();
  const name = h.get('form').querySelector('[name="item_name"]');
  name.value = 'Steel Sword';
  fire(name, 'input');
  fire(h.get('save'), 'click');
  h.settle();

  // As loaded — the pre-edit row — not as posted.
  assert.deepEqual(h.writes[0].options.loaded, ITEM(2, 'Sword'));
  assert.deepEqual(h.writes[0].options.textColumns,
    schemaOf('Items').columns.reduce((out, c, i) => {
      if (c.kind === 'Text') out.push(i);
      return out;
    }, []));

  // An append has nothing loaded to compare against.
  fire(h.get('new-record'), 'click');
  h.settle();
  [['item_name', 'Dagger'], ['item_usetype', 'NoUse'], ['graphic_tile', '1']].forEach((pair) => {
    const field = h.get('form').querySelector('[name="' + pair[0] + '"]');
    field.value = pair[1];
    fire(field, 'input');
  });
  fire(h.get('save'), 'click');
  h.settle();

  assert.equal(h.writes.length, 2);
  assert.equal(h.writes[1].rowNumber, 0);
  assert.equal(h.writes[1].options.loaded, null);
});

test('a sheet with no pk writes idColumnIndex -1, not 0', () => {
  // Code.gs rejects every second row of the nine no-pk sheets if this is 0 — their column A is
  // an Id-kind FK that legitimately repeats.
  //
  // CLASS INFO, not NPC Drops: it is the one no-pk sheet still edited a record at a time. The
  // other eight are grouped, and a grouped sheet never reaches writeRow — its save goes through
  // saveBatch, whose own no-pk handling is covered by the grouped tests below.
  const h = boot({ 'Class Info': [rowFor('Class Info', { class_id: 1, level: 1 }),
                                  rowFor('Class Info', { class_id: 1, level: 2 })],
                   Classes: [rowFor('Classes', { class_id: 1, class_name: 'Mage' })] });
  h.get('sheet-picker').value = 'Class Info';
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

  // The same rule for every Bool, which is what makes the checkbox tri-state: a two-state box
  // reads back 0 or 1 and nothing else, so opening this record and saving it untouched would
  // write 0 over all four of these and take them off the SQL default for good.
  ['lore', 'bindonpickup', 'bindonequip', 'event'].forEach((name) => {
    assert.equal(schemaOf('Items').columns[at(name)].kind, 'Bool', name);
    assert.equal(h.writes[0].cells[at(name)], null, name);
  });
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

test('a save keeps the record open WITHOUT reloading the sheet', () => {
  // The reload used to clearForm() and re-render — which also discarded anything typed after
  // Save was clicked. The local patch keeps the form exactly as the user left it and still
  // refreshes the list label, the id set and state.loaded.
  const h = boot({ Items: [ITEM(1, 'Gold'), ITEM(2, 'Sword')] });
  fire(h.get('records').children[1], 'click');
  h.settle();
  const reads = serverCalls(h.run, 'readSheet').length;

  const name = h.get('form').querySelector('[name="item_name"]');
  name.value = 'Steel Sword';
  fire(name, 'input');
  fire(h.get('save'), 'click');
  h.settle();

  assert.equal(App.__state.rowNumber, 3);
  assert.equal(h.get('form').querySelector('[name="item_name"]').value, 'Steel Sword');
  assert.equal(serverCalls(h.run, 'readSheet').length, reads, 'no reload round-trip');
  assert.equal(h.get('records').children[1].textContent, '2 — Steel Sword',
    'the list label reflects the save');
  assert.equal(App.__state.loaded.item_name, 'Steel Sword',
    'loaded moves with the sheet, so the next save diffs against what is now stored');
  assert.match(h.status(), /Saved\./);
});

test('a save landing after the user opened ANOTHER record leaves that record alone', () => {
  // Batching edits: save record A, click record B, start typing. The reply used to force-reopen
  // A, silently discarding everything typed into B.
  const h = boot({ Items: [ITEM(1, 'Gold'), ITEM(2, 'Sword')] }, { hold: true });
  h.settle();

  fire(h.get('records').children[0], 'click');
  h.settle();
  const gold = h.get('form').querySelector('[name="item_name"]');
  gold.value = 'Golden Coin';
  fire(gold, 'input');
  fire(h.get('save'), 'click');            // the write is now in flight

  fire(h.get('records').children[1], 'click');  // the user moves on within the same sheet
  const sword = h.get('form').querySelector('[name="item_name"]');
  sword.value = 'Sword of Typing';
  fire(sword, 'input');

  h.settle();                              // the save answers

  assert.equal(App.__state.rowNumber, 3, 'still on the record the user opened');
  assert.equal(h.get('form').querySelector('[name="item_name"]').value, 'Sword of Typing',
    'nothing typed into it was thrown away');
  assert.equal(App.__state.loaded.item_name, 'Sword', 'loaded still describes the OPEN record');
  assert.equal(h.get('records').children[0].textContent, '1 — Golden Coin',
    'while the saved row still reached the list');
});

test('the row an append landed on becomes the open record, so a second save edits it', () => {
  const h = boot({ Items: [ITEM(1, 'Gold')] });
  fire(h.get('new-record'), 'click');
  h.settle();
  [['item_name', 'Dagger'], ['item_usetype', 'NoUse'], ['graphic_tile', '1']].forEach((pair) => {
    const field = h.get('form').querySelector('[name="' + pair[0] + '"]');
    field.value = pair[1];
    fire(field, 'input');
  });
  fire(h.get('save'), 'click');
  h.settle();

  assert.equal(h.writes[0].rowNumber, 0, 'the first save appends');
  assert.equal(App.__state.rowNumber, 3, 'and the form is now bound to the row it landed on');
  assert.equal(h.get('records').children.length, 2, 'the list gained the new record');

  const name = h.get('form').querySelector('[name="item_name"]');
  name.value = 'Dirk';
  fire(name, 'input');
  fire(h.get('save'), 'click');
  h.settle();

  assert.equal(h.writes.length, 2);
  assert.equal(h.writes[1].rowNumber, 3, 'the second save edits, not appends a duplicate');
  assert.deepEqual(h.writes[1].options.loaded, h.writes[0].cells.map((c) => (c === null ? '' : c)),
    'and diffs against the record as it was saved');
});

test('a save during a record\'s render window is refused, not written to the wrong row', () => {
  // Between clicking a record and its form landing, rowNumber names the NEW record while the
  // DOM still holds the OLD one — a save in that window would collect the old fields under the
  // new row number, and on a no-pk sheet nothing downstream could catch it.
  const h = boot({ Items: [ITEM(1, 'Gold'), ITEM(2, 'Sword')] });
  fire(h.get('records').children[0], 'click');
  h.settle();

  // Make the next render wait on a decode again, as the first open on a slow machine does.
  delete App.__state.images.icons;
  fire(h.get('records').children[1], 'click');

  assert.equal(App.__state.rowNumber, 3, 'the bookkeeping has moved on');
  assert.equal(h.get('form').querySelector('[name="item_name"]').value, 'Gold',
    'but the old form is still on screen');

  fire(h.get('save'), 'click');
  assert.equal(h.writes.length, 0);
  assert.match(h.status(), /Still opening the record/);

  h.settle();                          // the decode lands and the new form renders
  assert.equal(h.get('form').querySelector('[name="item_name"]').value, 'Sword');
  fire(h.get('save'), 'click');
  h.settle();
  assert.equal(h.writes.length, 1);
  assert.equal(h.writes[0].rowNumber, 3, 'the save that went through matches what was shown');
});

test('save with no record open says so instead of counting invisible problems', () => {
  const h = boot({ Items: [ITEM(1, 'Gold')] });
  fire(h.get('save'), 'click');
  h.settle();
  assert.equal(h.writes.length, 0);
  assert.match(h.status(), /Open a record first/);
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

// --- a monster body clears the cells it renders without ---------------------------------------
//
// Character.cs:218-223 renders a body >= 100 ALONE: no hair, no face, no equipment. forms.js hides
// those rows live; app.js clears the cells when a row actually crosses over and saves. This is the
// only place in the editor that writes a cell the user did not touch, so each half of the rule —
// when it fires, when it does not, and what it says — is pinned here.

// An NPC with a face, hair and one equipped chest piece, so there is something to lose.
const DRESSED = (extra) => NPC(1, 'Rat', Object.assign({
  face_id: 70, hair_id: 26, hair_r: 10, hair_g: 20, hair_b: 30, hair_a: 40,
  body_r: 1, body_g: 2, body_b: 3, body_a: 4,
  equipped_items: '5,*,0,*,0,*,0,*,0,*,0,*',
}, extra || {}));

function openNpc(row) {
  const h = boot({ NPCs: [row] });
  h.get('sheet-picker').value = 'NPCs';
  fire(h.get('sheet-picker'), 'change');
  h.settle();
  fire(h.get('records').children[0], 'click');
  h.settle();
  return h;
}

const npcAt = (name) => schemaOf('NPCs').columns.findIndex((c) => c.name === name);
const npcRowOf = (h, name) => {
  let n = h.get('form').querySelector('[name="' + name + '"]');
  while (n && n.className !== 'field') n = n.parentNode;
  return n;
};

test('the face, hair and equipment rows hide the moment body_id crosses 100', () => {
  const h = openNpc(DRESSED());
  ['face_id', 'hair_id', 'hair_r', 'equipped_items'].forEach((name) =>
    assert.equal(npcRowOf(h, name).hidden, false, name + ' starts visible'));

  const body = h.get('form').querySelector('[name="body_id"]');
  body.value = '150';
  fire(body, 'input');

  ['face_id', 'hair_id', 'hair_r', 'equipped_items'].forEach((name) =>
    assert.equal(npcRowOf(h, name).hidden, true, name + ' must be hidden'));
  // The way back out, and the tint the client keeps, stay on screen.
  ['body_id', 'body_state', 'body_r'].forEach((name) =>
    assert.equal(npcRowOf(h, name).hidden, false, name + ' must stay visible'));
  assert.equal(npcRowOf(h, 'npc_name').hidden, false, 'only the dead rows are gated');

  // 100 itself is a monster — the client's test is >= 100 — and 99 is not.
  body.value = '100';
  fire(body, 'input');
  assert.equal(npcRowOf(h, 'face_id').hidden, true);
  body.value = '99';
  fire(body, 'input');
  assert.equal(npcRowOf(h, 'face_id').hidden, false);
});

test('hidden is not cleared: thinking better of a monster body loses nothing', () => {
  const h = openNpc(DRESSED());
  const body = h.get('form').querySelector('[name="body_id"]');

  body.value = '150';
  fire(body, 'input');
  body.value = '1';
  fire(body, 'input');

  fire(h.get('save'), 'click');
  h.settle();

  assert.equal(h.writes.length, 1);
  assert.equal(h.writes[0].cells[npcAt('face_id')], '70');
  assert.equal(h.writes[0].cells[npcAt('hair_id')], '26');
  assert.equal(h.writes[0].cells[npcAt('equipped_items')], '5,*,0,*,0,*,0,*,0,*,0,*');
});

test('saving a row that has just become a monster clears the face, hair and equipment', () => {
  const h = openNpc(DRESSED());
  const body = h.get('form').querySelector('[name="body_id"]');
  body.value = '150';
  fire(body, 'input');

  fire(h.get('save'), 'click');
  h.settle();

  assert.equal(h.writes.length, 1);
  const cells = h.writes[0].cells;
  assert.equal(cells[npcAt('body_id')], '150');
  assert.equal(cells[npcAt('face_id')], '0');
  assert.equal(cells[npcAt('hair_id')], '0');
  // NOT blank, and not '0': a blank or zero equipped_items emits a malformed token stream into the
  // MakeCharacter packet. The empty value is the six-slot form.
  assert.equal(cells[npcAt('equipped_items')], '0,*,0,*,0,*,0,*,0,*,0,*');

  // The body's own tint survives — the client zeroes the ids, not the colour — and so does the
  // hair tint, which is a parked colour rather than something that renders.
  assert.equal(cells[npcAt('body_r')], '1');
  assert.equal(cells[npcAt('hair_r')], '10');

  // And the user is told, because the cleared fields are hidden and the change is invisible.
  assert.match(h.status(), /^Saved\./);
  assert.match(h.status(), /Cleared face_id, hair_id, equipped_items/);
  assert.match(h.status(), /body of 150 renders alone/);
});

test('a row that was ALREADY a monster is saved untouched', () => {
  // Opening a record must not change it. Re-clearing on every save would rewrite cells the user
  // has no field for, on a row they only looked at.
  const h = openNpc(DRESSED({ body_id: 150 }));
  const name = h.get('form').querySelector('[name="npc_name"]');
  name.value = 'Renamed';
  fire(name, 'input');

  fire(h.get('save'), 'click');
  h.settle();

  const cells = h.writes[0].cells;
  assert.equal(cells[npcAt('face_id')], '70');
  assert.equal(cells[npcAt('hair_id')], '26');
  assert.equal(cells[npcAt('equipped_items')], '5,*,0,*,0,*,0,*,0,*,0,*');
  assert.doesNotMatch(h.status(), /Cleared/);
});

test('a new NPC given a monster body clears too — a blank row is a player body', () => {
  const h = boot({ NPCs: [] });
  h.get('sheet-picker').value = 'NPCs';
  fire(h.get('sheet-picker'), 'change');
  h.settle();
  fire(h.get('new-record'), 'click');
  h.settle();

  const form = h.get('form');
  form.querySelector('[name="npc_name"]').value = 'Beast';
  fire(form.querySelector('[name="npc_name"]'), 'input');
  const body = form.querySelector('[name="body_id"]');
  body.value = '10113';
  fire(body, 'input');
  const face = form.querySelector('[name="face_id"]');
  face.value = '70';
  fire(face, 'input');

  fire(h.get('save'), 'click');
  h.settle();

  assert.equal(h.writes.length, 1);
  assert.equal(h.writes[0].cells[npcAt('face_id')], '0');
  assert.equal(h.writes[0].cells[npcAt('equipped_items')], '0,*,0,*,0,*,0,*,0,*,0,*');
});

test('the cleared record is re-rendered, so a slot edit cannot resurrect the equipment', () => {
  // equipSlotsControl parsed the six slots at build time and holds them in memory. Without the
  // re-render, moving body_id back under 100 and touching one slot would write the chest piece the
  // save just cleared straight back into the cell.
  const h = openNpc(DRESSED());
  const body = h.get('form').querySelector('[name="body_id"]');
  body.value = '150';
  fire(body, 'input');
  fire(h.get('save'), 'click');
  h.settle();

  const form = h.get('form');
  assert.equal(form.querySelector('[name="face_id"]').value, '0');
  assert.equal(form.querySelector('[name="equipped_items"]').value,
    '0,*,0,*,0,*,0,*,0,*,0,*');
  assert.equal(form.querySelector('[name="body_id"]').value, '150');

  const back = form.querySelector('[name="body_id"]');
  back.value = '1';
  fire(back, 'input');
  const slots = form.querySelectorAll('[class="slot-graphic"]');
  assert.deepEqual(slots.map((s) => s.value), ['0', '0', '0', '0', '0', '0']);
  slots[1].value = '7';
  fire(slots[1], 'input');

  fire(h.get('save'), 'click');
  h.settle();
  assert.equal(h.writes.length, 2);
  assert.equal(h.writes[1].cells[npcAt('equipped_items')], '0,*,7,*,0,*,0,*,0,*,0,*');
});

test('a malformed equipped_items is cleared rather than refused as an unfaithful edit', () => {
  // Gate 2 exists because rewriting six slots from a GUESS loses equipment. This writes the EMPTY
  // stream on a body that has no equipment at all, so the guess is not involved and the save must
  // go through — which is why the clear runs after that gate rather than before it.
  const h = openNpc(DRESSED({ equipped_items: '5,*,0,*' }));
  assert.equal(Equipped.isFaithful('5,*,0,*'), false, 'the fixture must be a malformed row');

  const body = h.get('form').querySelector('[name="body_id"]');
  body.value = '150';
  fire(body, 'input');
  fire(h.get('save'), 'click');
  h.settle();

  assert.equal(h.writes.length, 1);
  assert.equal(h.writes[0].cells[npcAt('equipped_items')], '0,*,0,*,0,*,0,*,0,*,0,*');
});

test('a monster save that is REFUSED writes nothing and clears nothing', () => {
  // The clear runs before validation, so a refused save must leave the record exactly as it was —
  // including the cells it was about to zero.
  const h = openNpc(DRESSED());
  const body = h.get('form').querySelector('[name="body_id"]');
  body.value = '150';
  fire(body, 'input');
  const level = h.get('form').querySelector('[name="npc_level"]');
  level.value = 'not a number';
  fire(level, 'input');

  fire(h.get('save'), 'click');
  h.settle();

  assert.equal(h.writes.length, 0);
  assert.match(h.status(), /problem\(s\)/);
  assert.equal(h.get('form').querySelector('[name="face_id"]').value, '70');
  assert.equal(h.get('form').querySelector('[name="equipped_items"]').value,
    '5,*,0,*,0,*,0,*,0,*,0,*');
});

test('Items is subject to no monster-body rule, whatever its body_state says', () => {
  // The gate is keyed on body_id, which Items does not have. A sheet-wide guess would hide fields
  // on every wearable item.
  const h = boot({ Items: [ITEM(1, 'Gold', { body_state: 150 })] });
  fire(h.get('records').children[0], 'click');
  h.settle();
  fire(h.get('save'), 'click');
  h.settle();
  assert.equal(h.writes.length, 1);
  assert.doesNotMatch(h.status(), /Cleared/);
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

test('setting a slot COLOUR on a malformed equipped_items is refused too', () => {
  // Gate 2 is about the whole cell, not about the graphic fields: a colour edit reformats all
  // six slots from the same best guess a graphic edit would.
  const raw = '4,*,0,*';
  const h = boot({ NPCs: [NPC(1, 'Rat', { equipped_items: raw })] });
  h.get('sheet-picker').value = 'NPCs';
  fire(h.get('sheet-picker'), 'change');
  h.settle();
  fire(h.get('records').children[0], 'click');
  h.settle();

  const row = h.get('form').querySelectorAll('[class="equip-slot"]')[0];
  fire(row.querySelector('[class="swatch"]'), 'click');
  const strip = row.querySelector('[class="cp-alpha"]');
  strip.rect = { left: 0, top: 0, width: 14, height: 255 };
  fire(strip, 'mousedown', { clientX: 0, clientY: 127 });

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

// --- asynchrony: no callback may write into a state the user has moved on from -------------

test('a stale readSheet reply cannot put one sheet\'s rows under another sheet\'s schema', () => {
  // The failure this prevents: open Items, switch to Maps, let the replies land out of order,
  // and Items' rows sit under Maps' schema. Saving then writes an Items record into Maps — well
  // formed, correct width, unique id, so no guard in Code.gs can see it.
  const h = boot({ Items: [ITEM(7, 'Sword')], Maps: [MAP(1, 'Town')] }, { hold: true });
  h.settle();

  h.get('sheet-picker').value = 'Maps';
  fire(h.get('sheet-picker'), 'change');
  h.get('sheet-picker').value = 'Items';
  fire(h.get('sheet-picker'), 'change');

  // Maps was asked for first but answers last.
  h.run.queue.reverse();
  h.settle();

  assert.equal(App.__state.sheetName, 'Items');
  assert.equal(App.__state.rows[0][0], '7', 'the rows are Items\' rows');
  assert.equal(h.get('records').children[0].textContent, '7 — Sword');

  fire(h.get('records').children[0], 'click');
  h.settle();
  fire(h.get('save'), 'click');
  h.settle();

  assert.equal(h.writes.length, 1);
  assert.equal(h.writes[0].sheet, 'Items');
  assert.equal(h.writes[0].cells.length, schemaOf('Items').columns.length);
});

test('the staleness guard is a generation counter, so A -> B -> A still discards B', () => {
  // By NAME, the first Maps reply looks current the moment the user has gone back to Maps. Only
  // a counter distinguishes "the sheet I asked for" from "the request I am waiting on" — so the
  // server answers each Maps request differently and the stale answer is identifiable.
  const h = boot({ Items: [ITEM(7, 'Sword')] }, {
    hold: true,
    // Answers go out in the order the requests are SERVED, and the queue below is reversed —
    // so the current (third-issued) request is served first and the stale one last.
    perCall: { Maps: [[MAP(1, 'Town'), MAP(2, 'Cave')], [MAP(1, 'Stale')]] },
  });
  h.settle();

  h.get('sheet-picker').value = 'Maps';
  fire(h.get('sheet-picker'), 'change');
  h.get('sheet-picker').value = 'Items';
  fire(h.get('sheet-picker'), 'change');
  h.get('sheet-picker').value = 'Maps';
  fire(h.get('sheet-picker'), 'change');

  // The FIRST Maps request answers last; the current one is the third.
  h.run.queue.reverse();
  h.settle();

  assert.equal(App.__state.sheetName, 'Maps');
  assert.equal(h.get('records').children.length, 2, 'the CURRENT request won');
  assert.equal(h.get('records').children[0].textContent, '1 — Town');
});

test('picker replies that land after a sheet switch do not render the old sheet', () => {
  // The referenced-sheet phase is the second round-trip, and its done() renders. Switch sheets
  // while it is in flight and, unguarded, NPC Drops\' rows are drawn under Items\' schema.
  const h = boot({
    Items: [ITEM(1, 'Gold')], NPCs: [NPC(1, 'Rat')],
    'NPC Drops': [DROP(1, 1), DROP(1, 1), DROP(1, 1)],
  }, { hold: true });
  h.settle();

  h.get('sheet-picker').value = 'NPC Drops';
  fire(h.get('sheet-picker'), 'change');
  h.run.step();          // NPC Drops' rows arrive; its two readSheetIndex calls go out

  h.get('sheet-picker').value = 'Items';
  fire(h.get('sheet-picker'), 'change');

  // NPC Drops' picker replies land while Items is still loading. Its done() renders and writes
  // the status line, so unguarded it announces "0 records" for a sheet that has not answered
  // yet — and, before openSheet learned to clear the list, drew NPC Drops' rows under Items'
  // schema, one click away from a cross-sheet write.
  h.run.step();
  h.run.step();
  // THIS assertion is the one that kills the done() guard mutant, and it carries the test alone.
  // Do not "tidy" it away: the children check below is vacuous now that openSheet clears the
  // list — both the guarded and unguarded paths leave it at 0.
  assert.equal(h.status(), 'Loading Items…', 'the abandoned load did not report on Items');
  assert.equal(h.get('records').children.length, 0);

  h.settle();
  assert.equal(App.__state.sheetName, 'Items');
  assert.equal(h.get('records').children.length, 1);
  assert.equal(h.get('records').children[0].textContent, '1 — Gold');
});

test('switching sheets empties the form, so Save cannot append a phantom row', () => {
  // The invariant, not the worst case it was found through: a sheet switch leaves NO form and no
  // record behind, so there is nothing for Save to collect and Save says so. Asserted directly
  // because the downstream damage depends on which pair of sheets happens to share columns —
  // Class Info and Items share thirteen, and Class Info has no pk, so a leftover form on that
  // pair could still validate clean and append under idColumnIndex -1 with Code.gs\'s duplicate
  // scan disabled by design. The empty container is what makes that unreachable for every pair
  // at once, including the pairs a future sheet introduces.
  //
  // Class Info rather than the Combination Item pair the hazard was first found on: those two
  // are grouped now and never render a single-record form at all.
  const h = boot({
    Items: [ITEM(9, 'Flour')],
    Classes: [rowFor('Classes', { class_id: 1, class_name: 'Mage' })],
    'Class Info': [rowFor('Class Info', { class_id: 1, level: 4, stat_str: 9 })],
  });

  h.get('sheet-picker').value = 'Class Info';
  fire(h.get('sheet-picker'), 'change');
  h.settle();
  fire(h.get('records').children[0], 'click');
  h.settle();
  assert.equal(h.get('form').querySelector('[name="stat_str"]').value, '9');

  h.get('sheet-picker').value = 'Items';
  fire(h.get('sheet-picker'), 'change');
  h.settle();

  assert.equal(h.get('form').children.length, 0, 'the previous sheet\'s form is gone');
  assert.deepEqual(App.__state.loaded, {}, 'and so is the record it held');

  fire(h.get('save'), 'click');
  h.settle();
  assert.equal(h.writes.length, 0);
  assert.match(h.status(), /Open a record first/);
});

test('two Save clicks in flight issue ONE write', () => {
  // On the nine sheets with no pk, idColumnIndex is -1 and Code.gs\'s duplicate scan is
  // disabled by design — both writes would append.
  // Class Info: the one no-pk sheet still edited a record at a time, so the one that still
  // reaches this guard. The grouped sheets go through saveGroup, which has its own.
  const h = boot({
    Classes: [rowFor('Classes', { class_id: 1, class_name: 'Mage' })],
    'Class Info': [rowFor('Class Info', { class_id: 1, level: 1 })],
  });
  h.get('sheet-picker').value = 'Class Info';
  fire(h.get('sheet-picker'), 'change');
  h.settle();
  fire(h.get('records').children[0], 'click');
  h.settle();

  fire(h.get('save'), 'click');
  fire(h.get('save'), 'click');   // before the first round-trip resolves
  h.settle();

  assert.equal(h.writes.length, 1);
});

test('Save is available again after the write resolves', () => {
  const h = boot({ Items: [ITEM(1, 'Gold')] });
  fire(h.get('records').children[0], 'click');
  h.settle();
  fire(h.get('save'), 'click');
  h.settle();
  fire(h.get('save'), 'click');
  h.settle();
  assert.equal(h.writes.length, 2);
});

test('Save stays available after a FAILED write', () => {
  const h = boot({ Items: [ITEM(1, 'Gold')] }, { writeFails: true });
  fire(h.get('records').children[0], 'click');
  h.settle();
  fire(h.get('save'), 'click');
  h.settle();
  fire(h.get('save'), 'click');
  h.settle();
  assert.equal(h.writes.length, 2, 'a failed save must not wedge the button');
});

test('a bundle decodes ONCE however many records are opened while it is in flight', () => {
  const h = boot({ NPCs: [NPC(1, 'Rat'), NPC(2, 'Bat')] }, { hold: true });
  h.settle();

  h.get('sheet-picker').value = 'NPCs';
  fire(h.get('sheet-picker'), 'change');
  h.settle();

  fire(h.get('records').children[0], 'click');
  h.run.flush();
  assert.equal(h.img.pending.length, 1, 'parts is decoding');

  fire(h.get('records').children[1], 'click');
  h.run.flush();
  assert.equal(h.img.pending.length, 1, 'the second record waits on the SAME decode');
});

test('a stale ICONS decode cannot render the previous record over this one', () => {
  // Both records are opened while icons is still decoding, so both leave a queued continuation on
  // it — the one that builds the form and paints the panel. Only the current record's may run.
  //
  // THE PANEL, NOT Forms.render, is the probe. Counting renders cannot tell a dropped
  // continuation from a live one — with icons already decoded both records render synchronously
  // on click, so the count is zero either way. Preview.character carries the body_id it was asked
  // to draw, so a superseded continuation is visible as Rat's 7 turning up in Bat's panel.
  //
  // THE DROPPED CACHE is what makes the icons half of that reachable at all. loadBundle caches
  // success AND failure, so after boot no second icons decode ever exists to race — the window
  // exists only while the FIRST decode is in flight, which is before any record can be clicked.
  // Dropping the cached image below recreates exactly that first-open-on-a-slow-machine state
  // with records already on screen.
  const h = boot({
    NPCs: [NPC(1, 'Rat', { body_id: 7 }), NPC(2, 'Bat', { body_id: 9 })],
  });
  h.get('sheet-picker').value = 'NPCs';
  fire(h.get('sheet-picker'), 'change');
  h.settle();
  assert.equal(h.get('records').children.length, 2);
  delete App.__state.images.icons;

  const realCharacter = Preview.character;
  const drawn = [];
  Preview.character = function (canvas, values) {
    drawn.push(String(values.bodyId));
    return realCharacter.apply(this, arguments);
  };
  try {
    fire(h.get('records').children[0], 'click');   // Rat
    h.run.flush();
    fire(h.get('records').children[1], 'click');   // Bat, before either bundle decoded
    h.run.flush();
    assert.deepEqual(drawn, [], 'nothing is painted until icons decodes');
    h.settle();
  } finally {
    Preview.character = realCharacter;
  }

  // Two paints, both Bat's: one when icons lands and the form is built, one when parts lands and
  // the panel is repainted from the form. Rat's two continuations were dropped, not merely
  // overwritten — a 7 anywhere in this list is Rat's record painted into Bat's panel.
  assert.deepEqual(drawn, ['9', '9']);
  assert.equal(h.get('form').querySelector('[name="npc_name"]').value, 'Bat');
  assert.equal(App.__state.rowNumber, 3);
  assert.equal(App.__state.loaded.npc_id, '2');

  fire(h.get('save'), 'click');
  h.settle();
  const at = schemaOf('NPCs').columns.findIndex((c) => c.name === 'npc_name');
  assert.equal(h.writes[0].rowNumber, 3);
  assert.equal(h.writes[0].cells[at], 'Bat');
});

test('a stale PARTS decode cannot repaint the previous record into this record\'s panel', () => {
  // The other half of the token scheme, and the one that needs a different window: Rat is opened
  // and icons lands, so Rat's parts continuation is queued; Bat is then opened with icons already
  // cached, so it renders synchronously and queues a parts continuation of its own. When parts
  // lands, Rat's must be dropped rather than allowed to repaint the panel over Bat's form.
  const h = boot({
    NPCs: [NPC(1, 'Rat', { body_id: 7 }), NPC(2, 'Bat', { body_id: 9 })],
  }, { hold: true });
  h.settle();
  h.get('sheet-picker').value = 'NPCs';
  fire(h.get('sheet-picker'), 'change');
  h.settle();

  const realCharacter = Preview.character;
  const drawn = [];
  Preview.character = function (canvas, values) {
    drawn.push(String(values.bodyId));
    return realCharacter.apply(this, arguments);
  };
  try {
    fire(h.get('records').children[0], 'click');   // Rat: form and panel, parts in flight
    h.run.flush();
    assert.deepEqual(drawn, ['7']);
    assert.equal(h.img.pending.length, 1, 'parts is decoding');

    fire(h.get('records').children[1], 'click');   // Bat, before parts decoded
    h.run.flush();
    h.settle();
  } finally {
    Preview.character = realCharacter;
  }

  // Rat once, Bat twice — its own render, then its own post-parts repaint. A fourth entry is
  // Rat's superseded continuation repainting the panel it no longer owns.
  assert.deepEqual(drawn, ['7', '9', '9']);
  assert.equal(h.get('form').querySelector('[name="npc_name"]').value, 'Bat');
  assert.equal(App.__state.rowNumber, 3);
});

test('a bundle landing after a SHEET SWITCH paints no panel for a record that is not open', () => {
  // The third window, and the one clearForm has to close. A record is open with parts in flight,
  // then the user switches sheet: the form is emptied but the queued continuation is neither in
  // imageCallbacks nor formCallbacks — it is a closure in loadBundle's waiter list — so nothing
  // but the form token can stop it. Left unbumped it collected {} off the emptied container, read
  // the NEW sheet's schema, and appended a blank character canvas under an empty form.
  const h = boot({ Items: [ITEM(1, 'Gold')], NPCs: [NPC(1, 'Rat')] });
  fire(h.get('records').children[0], 'click');
  h.run.flush();
  assert.equal(h.img.pending.length, 1, 'parts is decoding');
  assert.ok(h.get('previews').children.length > 0, 'the Items panel is up');

  h.get('sheet-picker').value = 'NPCs';
  fire(h.get('sheet-picker'), 'change');
  h.settle();

  assert.equal(h.get('form').children.length, 0, 'no record is open');
  assert.equal(h.get('previews').children.length, 0, 'and so no panel is drawn');
});

test('a successful save drops that sheet\'s cached id list', () => {
  // validation.js fails OPEN on a missing id set and CLOSED on a stale one, so a cached Items
  // list that stops at 651 reports a real item 652 as nonexistent.
  const h = boot({
    NPCs: [NPC(1, 'Rat')], Items: [ITEM(1, 'Gold')],
    'NPC Drops': [DROP(1, 1)],
  });

  h.get('sheet-picker').value = 'NPC Drops';
  fire(h.get('sheet-picker'), 'change');
  h.settle();
  assert.ok(App.__state.pickerData.Items, 'Items is cached');

  h.get('sheet-picker').value = 'Items';
  fire(h.get('sheet-picker'), 'change');
  h.settle();
  fire(h.get('records').children[0], 'click');
  h.settle();
  fire(h.get('save'), 'click');
  h.settle();

  assert.equal(App.__state.pickerData.Items, undefined, 'the cache entry is gone');
  assert.equal(App.__state.idSets.Items, undefined);

  const before = serverCalls(h.run, 'readSheetIndex').length;
  h.get('sheet-picker').value = 'NPC Drops';
  fire(h.get('sheet-picker'), 'change');
  h.settle();
  assert.ok(serverCalls(h.run, 'readSheetIndex').some((c) => c.args[0] === 'Items'),
            'and it is fetched again');
  assert.ok(serverCalls(h.run, 'readSheetIndex').length > before);
});

// --- an fk whose id list never arrived ------------------------------------------------------
//
// Validation.validateCell passes an fk whose id set is ABSENT, and must: the sets load
// asynchronously, so failing closed there would report every id as broken while a reply is in
// flight. That makes a readSheetIndex FAILURE a hole — the id set is absent for good, so the only
// check that exists for that column silently waves everything through. These cover the closed half.
//
// Items is the sheet for it: spell_effect_id and learn_spell_id are both OPTIONAL fks (default 0),
// so the same record can be saved with the column unused and refused with it used.

test('a failed id-list load still renders the record, but says so', () => {
  const h = boot({ Items: [ITEM(1, 'Gold')] }, { failIndexOn: 'Spell Effects' });
  fire(h.get('records').children[0], 'click');
  h.settle();

  // Rendered, deliberately: every other column is editable and an fk column is a text input, so
  // refusing to draw the form would be a bigger loss than the one being reported.
  assert.ok(h.get('form').children.length > 0, 'the form is still usable');
  assert.deepEqual(App.__state.refErrors, ['Spell Effects']);
  assert.match(h.status(), /Could not load Spell Effects/);
  assert.match(h.status(), /will not save/);
  assert.equal(h.get('status').className, 'error');
});

test('the fk label says the list failed rather than "loading" forever', () => {
  const h = boot({ Items: [ITEM(1, 'Gold')] }, { failIndexOn: 'Spell Effects' });
  fire(h.get('records').children[0], 'click');
  h.settle();

  // The visible combobox, not the [name] input — that one is the hidden cell now, and the fk
  // control's own input listener lives on the field the user actually types into.
  const field = h.get('form').querySelector('[id="f-spell_effect_id"]');
  field.value = '5';
  fire(field, 'input');

  const label = field.parentNode.querySelector('[class="resolved bad"]');
  assert.equal(label.textContent, 'could not load Spell Effects');
});

test('a record that USES the unchecked fk is refused, and nothing is written', () => {
  const h = boot({ Items: [ITEM(1, 'Gold')] }, { failIndexOn: 'Spell Effects' });
  fire(h.get('records').children[0], 'click');
  h.settle();

  h.get('form').querySelector('[name="spell_effect_id"]').value = '9';
  fire(h.get('save'), 'click');
  // Before the retry it kicked off has answered: this is the message about the SAVE.
  assert.match(h.status(), /Cannot check this record's ids against Spell Effects/);
  assert.match(h.status(), /try saving again in a moment/);

  h.settle();
  assert.equal(h.writes.length, 0, 'an id nothing can check must not reach the sheet');
  // And once the retry has failed too, the standing warning replaces it — still an error, still
  // naming the sheet, and now saying the condition rather than the moment.
  assert.match(h.status(), /Could not load Spell Effects/);
  assert.equal(h.get('status').className, 'error');
});

test('a record that does not use it saves normally', () => {
  // The gate is per-column and per-value, not per-sheet: blank and 0 mean "none", which
  // validateCell exempts anyway, so there is nothing to fail open about.
  const h = boot({ Items: [ITEM(1, 'Gold')] }, { failIndexOn: 'Spell Effects' });
  fire(h.get('records').children[0], 'click');
  h.settle();

  h.get('form').querySelector('[name="item_name"]').value = 'Silver';
  fire(h.get('save'), 'click');
  h.settle();

  assert.equal(h.writes.length, 1);
  const at = schemaOf('Items').columns.findIndex((c) => c.name === 'item_name');
  assert.equal(h.writes[0].cells[at], 'Silver');
});

test('an explicit 0 in the unchecked fk is exempt too, not just a blank', () => {
  const h = boot({ Items: [ITEM(1, 'Gold')] }, { failIndexOn: 'Spell Effects' });
  fire(h.get('records').children[0], 'click');
  h.settle();

  h.get('form').querySelector('[name="spell_effect_id"]').value = '0';
  fire(h.get('save'), 'click');
  h.settle();

  assert.equal(h.writes.length, 1);
});

test('the refused save re-requests the list, and the next save goes through', () => {
  const h = boot({ Items: [ITEM(1, 'Gold')], 'Spell Effects': [rowFor('Spell Effects',
    { spell_effect_id: 9, spell_effect_name: 'Burn' })] },
    { failIndexOn: 'Spell Effects', indexFailsOnce: true });
  fire(h.get('records').children[0], 'click');
  h.settle();

  const before = serverCalls(h.run, 'readSheetIndex').filter((c) => c.args[0] === 'Spell Effects').length;
  h.get('form').querySelector('[name="spell_effect_id"]').value = '9';
  fire(h.get('save'), 'click');
  h.settle();

  assert.equal(h.writes.length, 0, 'the save that triggered the retry is still refused');
  assert.ok(serverCalls(h.run, 'readSheetIndex')
    .filter((c) => c.args[0] === 'Spell Effects').length > before, 'and the list is re-requested');
  assert.deepEqual(App.__state.refErrors, [], 'a successful retry clears the distrust');
  assert.match(h.status(), /Reloaded Spell Effects/);

  // The user does what the message says. This time the id is checked — against a list that has
  // it — and the record is written.
  fire(h.get('save'), 'click');
  h.settle();
  assert.equal(h.writes.length, 1);
});

test('a retry that fails again reports it and keeps the gate shut', () => {
  const h = boot({ Items: [ITEM(1, 'Gold')] }, { failIndexOn: 'Spell Effects' });
  fire(h.get('records').children[0], 'click');
  h.settle();

  h.get('form').querySelector('[name="spell_effect_id"]').value = '9';
  fire(h.get('save'), 'click');
  h.settle();
  fire(h.get('save'), 'click');
  h.settle();

  assert.equal(h.writes.length, 0);
  assert.deepEqual(App.__state.refErrors, ['Spell Effects']);
  assert.match(h.status(), /Could not load Spell Effects|Cannot check/);
});

test('a reloaded list checks ids again, so a bad one is still refused after a retry', () => {
  // The gate opening must not mean the FK check stops happening — it means it starts happening.
  const h = boot({ Items: [ITEM(1, 'Gold')], 'Spell Effects': [rowFor('Spell Effects',
    { spell_effect_id: 9, spell_effect_name: 'Burn' })] },
    { failIndexOn: 'Spell Effects', indexFailsOnce: true });
  fire(h.get('records').children[0], 'click');
  h.settle();

  h.get('form').querySelector('[name="spell_effect_id"]').value = '404';
  fire(h.get('save'), 'click');   // refused by the gate; kicks off the retry
  h.settle();
  fire(h.get('save'), 'click');   // refused by validation, which can now see the list
  h.settle();

  assert.equal(h.writes.length, 0);
  assert.equal(h.get('form').querySelector('[data-error-for="spell_effect_id"]').textContent,
               'spell_effect_id = 404 does not exist in Spell Effects');
});

test('both warnings are reported when a bundle and a list both fail', () => {
  // Independent causes; one overwriting the other would leave a user fixing half a problem.
  const h = boot({ Items: [ITEM(1, 'Gold')] }, { failIndexOn: 'Spell Effects', hold: true });
  h.img.fail();
  h.settle();

  assert.match(h.status(), /sprite bundle/);
  assert.match(h.status(), /Could not load Spell Effects/);
});

// --- save gate 4: a graphic with no art in the bundle (review #2) -----------------------------

// The fake icons bundle holds exactly one rect, '1:1', so graphic_file 1 + graphic_tile 1
// resolves and anything else does not.
const ART_ITEM = (id, name) => rowFor('Items', {
  item_template_id: id, item_usetype: 'NoUse', item_name: name, graphic_tile: 1, graphic_file: 1,
});

test('a save is REFUSED when a graphic pair names art the bundle does not have', () => {
  const h = boot({ Items: [ART_ITEM(1, 'Gold')] });
  fire(h.get('records').children[0], 'click');
  h.settle();

  const tile = h.get('form').querySelector('[name="graphic_tile"]');
  tile.value = '999';
  fire(tile, 'input');

  fire(h.get('save'), 'click');
  h.settle();

  assert.equal(h.writes.length, 0);
  // Named: Spell Effects carries two of these controls, so the column has to be in the message.
  assert.match(h.status(), /graphic_tile: no art for sheet 1 graphic 999/);
});

test('the save goes through once the graphic resolves again', () => {
  const h = boot({ Items: [ART_ITEM(1, 'Gold')] });
  fire(h.get('records').children[0], 'click');
  h.settle();

  const tile = h.get('form').querySelector('[name="graphic_tile"]');
  tile.value = '999';
  fire(tile, 'input');
  tile.value = '1';
  fire(tile, 'input');

  fire(h.get('save'), 'click');
  h.settle();
  assert.equal(h.writes.length, 1);
});

test('half a pair still saves — 176 shipped Spell Effects rows are half pairs', () => {
  // ITEM leaves graphic_file blank. The control says so in red; the gate must not join in, or
  // two thirds of Spell Effects becomes uneditable.
  const h = boot({ Items: [ITEM(1, 'Gold')] });
  fire(h.get('records').children[0], 'click');
  h.settle();

  fire(h.get('save'), 'click');
  h.settle();
  assert.equal(h.writes.length, 1);
});

test('a graphic nothing can be checked against does not block the save', () => {
  // A deploy whose icons include failed to load. Every pair on every sheet would look broken,
  // and refusing them all would brick an editor that is otherwise perfectly usable.
  const saved = globalThis.GOOSE_SPRITES;
  globalThis.GOOSE_SPRITES = { parts: saved.parts, effects: saved.effects };
  try {
    const h = boot({ Items: [ART_ITEM(1, 'Gold')] });
    fire(h.get('records').children[0], 'click');
    h.settle();

    fire(h.get('save'), 'click');
    h.settle();
    assert.equal(h.writes.length, 1);
  } finally {
    globalThis.GOOSE_SPRITES = saved;
  }
});

// --- the interval between a request and its reply -------------------------------------------

test('the record list is emptied the moment a sheet switch is requested', () => {
  // state.schema swaps synchronously; the rows do not. Every button left on screen in between
  // is a row of the OLD sheet under the schema of the NEW one.
  const h = boot({ Items: [ITEM(7, 'Sword')], Maps: [MAP(3, 'Town')] }, { hold: true });
  h.settle();
  assert.equal(h.get('records').children.length, 1);

  h.get('sheet-picker').value = 'Maps';
  fire(h.get('sheet-picker'), 'change');

  assert.equal(h.get('records').children.length, 0, 'nothing stale is clickable');
  assert.deepEqual(App.__state.rows, []);
  assert.deepEqual(App.__state.ids, []);
  assert.equal(App.__state.idSets.__self.size, 0);

  h.settle();
  assert.equal(h.get('records').children.length, 1);
  assert.equal(h.get('records').children[0].textContent, '3 — Town');
});

test('a FAILED read leaves nothing of the previous sheet behind — permanently', () => {
  // The failure handler reports and returns; it never rebuilds the list. Without the clear, the
  // editor would sit indefinitely on Items' records with Maps' schema behind them, and one
  // click plus Save writes an Items record into Maps. Transient Apps Script errors are ordinary.
  const h = boot({ Items: [ITEM(7, 'Sword')], Maps: [MAP(3, 'Town')] }, { failOn: 'Maps' });
  assert.equal(h.get('records').children.length, 1);

  h.get('sheet-picker').value = 'Maps';
  fire(h.get('sheet-picker'), 'change');
  h.settle();

  assert.equal(h.status(), 'boom: Maps');
  assert.equal(h.get('records').children.length, 0);
  assert.equal(h.get('form').children.length, 0);

  // Nothing to click, nothing loaded, and Save writes nothing.
  fire(h.get('save'), 'click');
  h.settle();
  assert.equal(h.writes.length, 0);
});

test('editRow refuses an index the rows do not have', () => {
  // rowToValues(undefined) builds an all-blank record under a real rowNumber — a form that
  // looks empty and saves like a wipe of that row.
  const h = boot({ Items: [ITEM(7, 'Sword')] });
  App.editRow(99);
  h.settle();
  assert.equal(h.get('form').children.length, 0);
  assert.equal(App.__state.rowNumber, 0);
});

test('New during an in-flight read does not suggest an id from the previous sheet', () => {
  const h = boot({ Items: [ITEM(602, 'Sword')], Maps: [MAP(3, 'Town')] }, { hold: true });
  h.settle();

  h.get('sheet-picker').value = 'Maps';
  fire(h.get('sheet-picker'), 'change');
  fire(h.get('new-record'), 'click');
  h.settle();

  // 1 from an empty id set, not 603 from Items'.
  assert.equal(h.get('form').querySelector('[name="map_id"]').value, '1');
});

test('a save that answers after a sheet switch does not hijack the new sheet', () => {
  const h = boot({ Items: [ITEM(7, 'Sword')], Maps: [MAP(3, 'Town'), MAP(4, 'Cave')] },
                 { hold: true });
  h.settle();

  fire(h.get('records').children[0], 'click');
  h.settle();
  fire(h.get('save'), 'click');          // writeRow queued, answering for Items

  h.get('sheet-picker').value = 'Maps';
  fire(h.get('sheet-picker'), 'change'); // the user moves on
  h.settle();

  assert.equal(h.writes.length, 1);
  assert.equal(h.writes[0].sheet, 'Items', 'the write itself was always bound to Items');

  // Maps is what the user asked for and Maps is what they get — no record force-opened under a
  // row number that belongs to another sheet.
  assert.equal(App.__state.sheetName, 'Maps');
  assert.equal(h.get('records').children.length, 2);
  assert.equal(h.get('form').children.length, 0);
  assert.equal(App.__state.rowNumber, 0);
});

test('a save that FAILS after a sheet switch does not overwrite the new sheet\'s status', () => {
  // The error belongs to a sheet the user has left. Reporting it against the sheet now on screen
  // reads as "loading Maps failed", which is a different and untrue thing.
  const h = boot({ Items: [ITEM(7, 'Sword')], Maps: [MAP(3, 'Town')] },
                 { hold: true, writeFails: true });
  h.settle();
  fire(h.get('records').children[0], 'click');
  h.settle();

  fire(h.get('save'), 'click');
  h.get('sheet-picker').value = 'Maps';
  fire(h.get('sheet-picker'), 'change');
  // Maps finishes first and the doomed write answers last — the order in which a late error
  // would be the last thing written to the status line.
  h.run.queue.reverse();
  h.settle();

  assert.equal(h.status(), '1 records');
  assert.equal(App.__state.saving, false, 'and the button is free again');
});

test('a save that answers after a sheet switch still invalidates the RIGHT cache', () => {
  const h = boot({
    NPCs: [NPC(1, 'Rat')], Items: [ITEM(1, 'Gold')], Maps: [MAP(3, 'Town')],
    'NPC Drops': [DROP(1, 1)],
  }, { hold: true });
  h.settle();

  // Cache Items by opening a sheet that references it, then edit Items itself.
  h.get('sheet-picker').value = 'NPC Drops';
  fire(h.get('sheet-picker'), 'change');
  h.settle();
  h.get('sheet-picker').value = 'Items';
  fire(h.get('sheet-picker'), 'change');
  h.settle();
  fire(h.get('records').children[0], 'click');
  h.settle();

  fire(h.get('save'), 'click');
  h.get('sheet-picker').value = 'Maps';
  fire(h.get('sheet-picker'), 'change');
  h.settle();

  assert.equal(App.__state.pickerData.Items, undefined, 'the sheet that CHANGED was dropped');
  assert.equal(App.__state.pickerData.Maps, undefined, 'and Maps was never cached to begin with');
});

test('a save landing after a switch to a REFERENCING sheet refills the list it dropped', () => {
  // The order that used to strand the pickers: NPC Drops loads while the Items write is in
  // flight, sees Items cached and skips it; then the save's invalidation deletes the entry with
  // nothing left to re-request it. Every item picker on the open sheet read "loading Items…"
  // forever, and the first save was refused with "that list failed to load".
  const h = boot({
    NPCs: [NPC(1, 'Rat')], Items: [ITEM(1, 'Gold')], 'NPC Drops': [DROP(1, 1)],
  }, { hold: true });
  h.settle();

  // Cache Items by opening NPC Drops once, then go edit Items itself.
  h.get('sheet-picker').value = 'NPC Drops';
  fire(h.get('sheet-picker'), 'change');
  h.settle();
  h.get('sheet-picker').value = 'Items';
  fire(h.get('sheet-picker'), 'change');
  h.settle();
  fire(h.get('records').children[0], 'click');
  h.settle();

  fire(h.get('save'), 'click');            // writeRow queued
  h.get('sheet-picker').value = 'NPC Drops';
  fire(h.get('sheet-picker'), 'change');   // readSheet queued behind it
  h.run.queue.reverse();                   // NPC Drops answers first and skips cached Items
  h.settle();

  assert.deepEqual(App.__state.pickerData.Items, [{ id: '1', name: 'Gold' }],
    'the invalidated list was re-requested for the sheet on screen');
  assert.ok(App.__state.idSets.Items.has(1), 'so FK validation has its id set back');
});

// --- the paths the sweep found unasserted ---------------------------------------------------

test('an unknown sheet name is refused before any server call', () => {
  const h = boot({ Items: [ITEM(1, 'Gold')] });
  const before = serverCalls(h.run, 'readSheet').length;

  App.openSheet('Not A Sheet');
  h.settle();

  assert.equal(h.status(), 'No schema for sheet Not A Sheet');
  assert.equal(h.get('status').className, 'error');
  assert.equal(serverCalls(h.run, 'readSheet').length, before, 'no readSheet was issued');
});

test('nameIndex reads the schema, not the shipping data', () => {
  // Every shipped sheet has an Id in column A, so a loop starting at 0 would still answer 1 for
  // all 21 — the property has to be checked against a schema that could tell the difference.
  assert.equal(App.nameIndex({ columns: [{ name: 'a', kind: 'Text' }, { name: 'b', kind: 'Text' }] }), 1);
  assert.equal(App.nameIndex({ columns: [{ name: 'a', kind: 'Id' }, { name: 'b', kind: 'Int' },
                                         { name: 'c', kind: 'Text' }] }), 2);
  assert.equal(App.nameIndex({ columns: [{ name: 'a', kind: 'Id' }] }), 1);
});

test('a Spells record with no effect draws no canvas at all', () => {
  const h = boot({
    Spells: [rowFor('Spells', { spell_id: 1, spell_name: 'Nothing', spell_target: 'Self',
                                spellbook_graphic: 1, spell_effect_id: 0 })],
  });
  h.get('sheet-picker').value = 'Spells';
  fire(h.get('sheet-picker'), 'change');
  h.settle();
  fire(h.get('records').children[0], 'click');
  h.settle();

  // Not "no timer" — an empty 96x96 box with nothing in it is its own kind of wrong.
  assert.equal(h.get('previews').children.length, 0);
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

test('the publish check fires all 21 reads at once rather than one after the next', () => {
  // Phase two needs every sheet before it can validate anything, so ordering the reads buys nothing
  // and costs 21 serialised round trips — most of the wall clock of the run. The assertion is made
  // BEFORE anything is flushed: sequentially there would be exactly one call in flight.
  const h = boot({ Items: [ITEM(1, 'Gold')] });
  const before = serverCalls(h.run, 'readSheet').length;

  fire(h.get('publish-check'), 'click');
  assert.equal(serverCalls(h.run, 'readSheet').length - before, GOOSE_SCHEMA.sheets.length);
  assert.equal(GOOSE_SCHEMA.sheets.length, 21);

  h.settle();
  assert.match(h.get('publish-results').textContent, /All sheets valid/);
});

test('the report reads in SHEET order however the replies interleave', () => {
  // Each reply lands in its own slot and the slots are walked in schema order, so the run is
  // diffable against the last one. Reversing the queue is the shape of the real hazard: 21 concurrent
  // reads of very different sizes (NPC Spawns is 4,322 rows, Titles a handful) do not answer in the
  // order they were asked.
  const broken = {
    Items: [rowFor('Items', { item_template_id: 1, item_usetype: 'NoUse' })],   // no item_name
    NPCs: [rowFor('NPCs', { npc_id: 1, npc_type: 'Monster' })],                 // no npc_name
  };
  const order = () => {
    const h = boot(broken);
    fire(h.get('publish-check'), 'click');
    h.run.queue.reverse();
    h.settle();
    return h.get('publish-results').querySelectorAll('[class="problem"]')
      .map((n) => n.textContent.split(' row ')[0]);
  };

  const sheets = GOOSE_SCHEMA.sheets.map((s) => s.sheet);
  const reported = order();
  assert.ok(reported.indexOf('Items') !== -1 && reported.indexOf('NPCs') !== -1);
  assert.ok(sheets.indexOf('Items') < sheets.indexOf('NPCs'), 'Items comes first in the schema');
  assert.ok(reported.indexOf('Items') < reported.indexOf('NPCs'),
            'and first in the report, even though its reply landed last');
});

test('a read that fails is reported in ITS slot, not wherever it happened to fail', () => {
  const h = boot({ Items: [ITEM(1, 'Gold')] }, { failOn: 'Maps' });
  fire(h.get('publish-check'), 'click');
  h.run.queue.reverse();
  h.settle();
  assert.match(h.get('publish-results').textContent, /Maps row -: boom: Maps/);
});

test('a second publish check cannot start while one is running', () => {
  const h = boot({ Items: [ITEM(1, 'Gold')] });
  fire(h.get('publish-check'), 'click');
  fire(h.get('publish-check'), 'click');   // ignored: the first is still in flight
  h.settle();
  assert.equal(serverCalls(h.run, 'readSheet').filter((c) => c.args[0] === 'Items').length, 2);
});

test('the problem list is capped and says how many are hidden', () => {
  // 400 rows x 3 missing required cells = 1,200 problems, past MAX_PROBLEMS — so this exercises
  // the DROPPED counter as well as the shown/hidden split. A smaller fixture leaves the
  // overflow arithmetic unexecuted.
  const rows = [];
  for (let i = 0; i < 400; i++) rows.push(rowFor('Items', { item_template_id: i + 1 }));
  const h = boot({ Items: rows });
  fire(h.get('publish-check'), 'click');
  h.settle();

  const panel = h.get('publish-results');
  assert.equal(panel.querySelectorAll('[class="problem"]').length, 100);
  // 1,200 problems, 1,000 kept and 200 dropped at the cap; the count is reported as "1000+" so
  // it never claims to be exact, and the hidden tail is 1000 - 100 shown + 200 dropped.
  assert.match(panel.textContent, /1000\+ problem\(s\)/);
  assert.match(panel.textContent, /and 1100 more not shown/);
});

// --- grouped sheets --------------------------------------------------------------------------

test('opening a grouped sheet lists parents, not rows', () => {
  const { run } = boot({ 'NPC Drops': [DROP(1, 10), DROP(1, 20), DROP(2, 30)],
                         NPCs: [NPC(1, 'Mouse'), NPC(2, 'Bat')] });
  App.openSheet('NPC Drops');
  run.flush();

  const records = document.getElementById('records').querySelectorAll('.record');
  assert.deepEqual([...records].map((n) => n.textContent),
                   ['1 — Mouse (2)', '2 — Bat (1)']);
});

test('opening an ungrouped sheet is unchanged', () => {
  const { run } = boot({ Items: [ITEM(1, 'Sword'), ITEM(2, 'Shield')] });
  App.openSheet('Items');
  run.flush();
  const records = document.getElementById('records').querySelectorAll('.record');
  assert.equal(records.length, 2);
  assert.match(records[0].textContent, /Sword/);
});

test('clicking a parent opens all of its rows at once', () => {
  const { run } = boot({ 'NPC Drops': [DROP(1, 10), DROP(1, 20), DROP(2, 30)],
                         NPCs: [NPC(1, 'Mouse')], Items: [ITEM(10, 'Cheese'), ITEM(20, 'Tail')] });
  App.openSheet('NPC Drops');
  run.flush();
  fire(document.getElementById('records').querySelectorAll('.record')[0], 'click');
  run.flush();

  assert.equal(document.getElementById('form').querySelectorAll('[data-group-row]').length, 2);
});

test('saving a group posts one batch for the sheet', () => {
  const { run } = boot({ 'NPC Drops': [DROP(1, 10)], NPCs: [NPC(1, 'Mouse')],
                         Items: [ITEM(10, 'Cheese')] });
  App.openSheet('NPC Drops');
  run.flush();
  fire(document.getElementById('records').querySelectorAll('.record')[0], 'click');
  run.flush();

  const cell = document.getElementById('form').querySelectorAll('[name=droprate]')[0];
  cell.value = '0.75';
  App.save();
  run.flush();

  const call = run.calls.filter((c) => c.name === 'saveBatch').pop();
  assert.ok(call, 'a group save must go through saveBatch, not writeRow');
  assert.equal(call.args[0].length, 1);
  assert.equal(call.args[0][0].sheet, 'NPC Drops');
  assert.equal(call.args[0][0].writes.length, 1);
});

test('a group save re-reads the sheet afterwards', () => {
  // Deletion shifts every row below it, so no cached row number survives a save. A reload is
  // the only honest position, and it is unconditional rather than only-when-something-was-deleted
  // so there is one path to get right.
  const { run } = boot({ 'NPC Drops': [DROP(1, 10)], NPCs: [NPC(1, 'Mouse')],
                         Items: [ITEM(10, 'Cheese')] });
  App.openSheet('NPC Drops');
  run.flush();
  fire(document.getElementById('records').querySelectorAll('.record')[0], 'click');
  run.flush();
  const before = run.calls.filter((c) => c.name === 'readSheet').length;

  document.getElementById('form').querySelectorAll('[name=droprate]')[0].value = '0.75';
  App.save();
  run.flush();

  assert.ok(run.calls.filter((c) => c.name === 'readSheet').length > before);
});

test('a FAILED group save also re-reads the sheet', () => {
  // Load-bearing, and easy to miss: without it a retry after a batch that threw part-way would
  // re-append rows that already landed. After the reload the diff sees them as existing rows.
  const { run } = boot({ 'NPC Drops': [DROP(1, 10)], NPCs: [NPC(1, 'Mouse')],
                         Items: [ITEM(10, 'Cheese')] }, { saveBatchFails: true });
  App.openSheet('NPC Drops');
  run.flush();
  fire(document.getElementById('records').querySelectorAll('.record')[0], 'click');
  run.flush();
  const before = run.calls.filter((c) => c.name === 'readSheet').length;

  document.getElementById('form').querySelectorAll('[name=droprate]')[0].value = '0.75';
  App.save();
  run.flush();

  // AFTER the reload, not before it: openSheet's own status lines land last, so an error shown
  // at the moment of failure would be buried and a failed save would read as a successful one.
  assert.match(document.getElementById('status').textContent, /boom/);
  assert.ok(run.calls.filter((c) => c.name === 'readSheet').length > before);
});

test('a successful group save reports what it wrote, after the reload has settled', () => {
  // The counts are the only confirmation a save gives, and openSheet's own status lines land
  // AFTER the save resolves — shown at the moment of success they are overwritten by the
  // reopened group's row count within the same tick and the user sees nothing.
  const { run } = boot({ 'NPC Drops': [DROP(1, 10)], NPCs: [NPC(1, 'Mouse')],
                         Items: [ITEM(10, 'Cheese')] });
  App.openSheet('NPC Drops');
  run.flush();
  fire(document.getElementById('records').querySelectorAll('.record')[0], 'click');
  run.flush();

  document.getElementById('form').querySelectorAll('[name=droprate]')[0].value = '0.75';
  App.save();
  run.flush();

  assert.match(document.getElementById('status').textContent, /Saved 1 edited, 0 added, 0 removed/);
  assert.doesNotMatch(document.getElementById('status').textContent, /duplicate/);
});

test('the duplicate note survives the reload, and says how many are still off screen', () => {
  // 101 rows, the last a copy of the first — two duplicates counted, one of them past the render
  // cap and so impossible to tint. A bare '2 rows duplicate another row' over a single tinted row
  // reads as a bug, and the note is worthless if the reload eats it.
  const rows = [];
  for (let i = 0; i < Groups.RENDER_CAP + 1; i++) rows.push(DROP(1, i + 1));
  rows[Groups.RENDER_CAP] = DROP(1, 1);
  const items = [];
  for (let i = 0; i < Groups.RENDER_CAP + 1; i++) items.push(ITEM(i + 1, 'Thing ' + (i + 1)));

  const { run } = boot({ 'NPC Drops': rows, NPCs: [NPC(1, 'Mouse')], Items: items });
  App.openSheet('NPC Drops');
  run.flush();
  fire(document.getElementById('records').querySelectorAll('.record')[0], 'click');
  run.flush();

  document.getElementById('form').querySelectorAll('[name=droprate]')[0].value = '0.75';
  App.save();
  run.flush();

  const text = document.getElementById('status').textContent;
  assert.match(text, /Saved 1 edited/);
  assert.match(text, /2 rows duplicate another row in this group — 1 of them are not on screen yet\./);
});

test('a group save is not blocked by bad data in a row the render cap did not draw', () => {
  // A group of 101 whose LAST row carries a droprate the sheet should never have held. It is
  // past the cap, so it is never drawn — and showErrors can only highlight a row that has an
  // element. Validating it anyway would report a problem with nothing on screen to point at and
  // no way for the user to reach it, wedging the whole group. It produces no write either, so
  // letting the save through leaves that row exactly as it was found.
  const rows = [];
  for (let i = 0; i < 101; i++) rows.push(DROP(1, i + 1));
  const bad = schemaOf('NPC Drops').columns.map((c) => c.name).indexOf('droprate');
  rows[100][bad] = 'nonsense';

  const items = [];
  for (let i = 0; i < 101; i++) items.push(ITEM(i + 1, 'Thing ' + (i + 1)));
  const { run } = boot({ 'NPC Drops': rows, NPCs: [NPC(1, 'Mouse')], Items: items });
  App.openSheet('NPC Drops');
  run.flush();
  fire(document.getElementById('records').querySelectorAll('.record')[0], 'click');
  run.flush();
  assert.equal(document.getElementById('form').querySelectorAll('[data-group-row]').length, 100);

  document.getElementById('form').querySelectorAll('[name=droprate]')[0].value = '0.75';
  App.save();
  run.flush();

  const call = run.calls.filter((c) => c.name === 'saveBatch').pop();
  assert.ok(call, 'the undrawn row must not block a save of the rows on screen');
  assert.equal(call.args[0][0].writes.length, 1, 'only the drawn row the user edited is written');
});

test('a group save refuses while an invalid row is on screen', () => {
  const { run } = boot({ 'NPC Drops': [DROP(1, 10)], NPCs: [NPC(1, 'Mouse')],
                         Items: [ITEM(10, 'Cheese')] });
  App.openSheet('NPC Drops');
  run.flush();
  fire(document.getElementById('records').querySelectorAll('.record')[0], 'click');
  run.flush();

  document.getElementById('form').querySelectorAll('[name=droprate]')[0].value = 'nonsense';
  App.save();
  run.flush();

  assert.equal(run.calls.filter((c) => c.name === 'saveBatch').length, 0);
  assert.match(document.getElementById('status').textContent, /problem/);
});

test('switching sheets while a group read is in flight discards it', () => {
  // The token discipline app.js is built around, one level down. Without it the reply for
  // NPC Drops renders its parents under whatever sheet is now open.
  const { run } = boot({ 'NPC Drops': [DROP(1, 10)], NPCs: [NPC(1, 'Mouse')],
                         Items: [ITEM(1, 'Sword')] });
  App.openSheet('NPC Drops');
  App.openSheet('Items');
  run.flush();

  assert.equal(App.__state.sheetName, 'Items');
  assert.equal(document.getElementById('form').querySelectorAll('[data-group-row]').length, 0);
});

test('opening a second group discards the first one pending controls', () => {
  // Group 1 has ONE row and group 2 has TWO, so a first render landing over the second is
  // visible in the count. With both groups the same size the assertion passes whether or not the
  // token is checked — Groups.render empties the container either way — and the guard this test
  // exists for goes untested.
  const { run } = boot({ 'NPC Drops': [DROP(1, 10), DROP(2, 20), DROP(2, 30)],
                         NPCs: [NPC(1, 'Mouse'), NPC(2, 'Bat')],
                         Items: [ITEM(10, 'Cheese'), ITEM(20, 'Tail'), ITEM(30, 'Ear')] });
  App.openSheet('NPC Drops');
  run.flush();
  // The FK lists are dropped first, so each open has to fetch them and its render lands in a
  // callback rather than inline — which is the only arrangement in which the token can be stale.
  // With everything cached both renders run synchronously in click order and the second wins
  // whatever the guard does, so the guard would go untested.
  App.__state.pickerData = {};
  App.__state.idSets = {};

  const records = document.getElementById('records').querySelectorAll('.record');
  fire(records[0], 'click');
  fire(records[1], 'click');

  // Group 1's replies, landing after the user has moved to group 2 — two of them, because
  // NPC Drops references NPCs and Items and the render waits on both. They must draw nothing.
  run.step();
  run.step();
  assert.equal(document.getElementById('form').querySelectorAll('[data-group-row]').length, 0,
               'a reply for the group the user has left may not draw over the one they are on');

  run.flush();
  const rows = document.getElementById('form').querySelectorAll('[data-group-row]');
  assert.equal(rows.length, 2, 'only the second group may be on screen');
  assert.equal(App.__state.group.key, '2');
});

test('a group save refuses while another one is still in flight', () => {
  // Same guard, and the same reason, as the single-record path: a grouped sheet has no pk, so
  // Code.gs's duplicate scan is disabled by design and two batches in flight would BOTH append.
  const { run } = boot({ 'NPC Drops': [DROP(1, 10)], NPCs: [NPC(1, 'Mouse')],
                         Items: [ITEM(10, 'Cheese')] });
  App.openSheet('NPC Drops');
  run.flush();
  fire(document.getElementById('records').querySelectorAll('.record')[0], 'click');
  run.flush();

  document.getElementById('form').querySelectorAll('[name=droprate]')[0].value = '0.75';
  App.save();
  App.save();   // before the first round-trip resolves
  run.flush();

  assert.equal(run.calls.filter((c) => c.name === 'saveBatch').length, 1);
});

test('a reopen request left over from another sheet is discarded, not applied', () => {
  // A post-save reload whose read FAILS never consumes the request. Applied to the next grouped
  // sheet opened it would build an empty panel under a parent id borrowed from another sheet —
  // one Add-row away from appending a row under the wrong quest.
  const { run } = boot({ 'Quest Rewards': [], Quests: [rowFor('Quests', { id: 7, name: 'Bread' })],
                         Items: [ITEM(10, 'Cheese')] });
  App.__state.reopenGroup = { sheet: 'NPC Drops', key: '1' };

  App.openSheet('Quest Rewards');
  run.flush();

  assert.equal(App.__state.group, null, 'no group may be opened from another sheet\'s request');
  assert.equal(App.__state.reopenGroup, null, 'and the stale request is dropped, not kept');
  assert.equal(document.getElementById('form').children.length, 0);
});

test('New group is unavailable until the current grouped sheet has loaded', () => {
  const { run } = boot({
    'NPC Drops': [DROP(1, 10)],
    'NPC Vendor Items': [rowFor('NPC Vendor Items', {
      npc_template_id: 1, item_template_id: 10, price: 5,
    })],
    NPCs: [NPC(1, 'Mouse')],
    Items: [ITEM(10, 'Cheese')],
  });

  App.openSheet('NPC Drops');
  run.flush();
  assert.equal(App.__state.groups[0].rows[0].values.droprate, '0.10');

  App.openSheet('NPC Vendor Items');
  assert.equal(document.getElementById('new-record').disabled, true);
  fire(document.getElementById('new-record'), 'click');
  assert.equal(document.getElementById('modal').hidden, true,
               'a loading sheet must not open a parent picker over stale groups');
  assert.equal(document.getElementById('form').querySelectorAll('[data-group-row]').length, 0);

  run.flush();
  assert.equal(document.getElementById('new-record').disabled, false);
});

test('a stale sheet-read failure cannot erase the current save reload state', () => {
  const { run } = boot({ 'NPC Drops': [DROP(1, 10)] }, { hold: true, failOn: 'Items' });

  App.openSheet('Items');
  App.openSheet('NPC Drops');
  App.__state.reopenGroup = { sheet: 'NPC Drops', key: '1' };
  App.__state.pendingStatus = { sheet: 'NPC Drops', message: 'Saved 1 edited', warn: false };

  run.step(); // the abandoned Items read fails after NPC Drops became current

  assert.deepEqual(App.__state.reopenGroup, { sheet: 'NPC Drops', key: '1' });
  assert.deepEqual(App.__state.pendingStatus,
                   { sheet: 'NPC Drops', message: 'Saved 1 edited', warn: false });
});

test('the header Save is hidden on a grouped sheet and back on an ungrouped one', () => {
  const { run } = boot({ 'NPC Drops': [DROP(1, 10)], NPCs: [NPC(1, 'Mouse')],
                         Items: [ITEM(1, 'Sword')] });
  App.openSheet('NPC Drops');
  run.flush();
  assert.equal(document.getElementById('save').hidden, true);

  App.openSheet('Items');
  run.flush();
  assert.equal(document.getElementById('save').hidden, false);
});

test('New opens the parent picker with every parent and a filter box', () => {
  // Every parent is offered, so there is one way to reach any of them — the ones that already
  // have a group jump to it rather than starting a second. And a filter, because Maps, NPCs and
  // Quests run to hundreds of entries: an unfiltered list of 649 buttons is not a control.
  const { run } = boot({ 'NPC Drops': [DROP(1, 10)], NPCs: [NPC(1, 'Mouse'), NPC(2, 'Bat')] });
  App.openSheet('NPC Drops');
  run.flush();
  App.newRecord();

  const modal = document.getElementById('modal');
  assert.ok(modal.querySelectorAll('[data-parent]').length >= 2);
  assert.equal(modal.querySelectorAll('[data-filter]').length, 1);
});

test('the parent picker filters as you type', () => {
  const { run } = boot({ 'NPC Drops': [DROP(1, 10)], NPCs: [NPC(1, 'Mouse'), NPC(2, 'Bat')] });
  App.openSheet('NPC Drops');
  run.flush();
  App.newRecord();

  const filter = document.getElementById('modal').querySelectorAll('[data-filter]')[0];
  filter.value = 'bat';
  fire(filter, 'input');

  const shown = document.getElementById('modal').querySelectorAll('[data-parent]');
  assert.equal(shown.length, 1);
  assert.match(shown[0].textContent, /Bat/);
});

test('Enter in the filter picks the first visible parent', () => {
  const { run } = boot({ 'NPC Drops': [DROP(1, 10)], NPCs: [NPC(1, 'Mouse'), NPC(2, 'Bat')],
                         Items: [ITEM(10, 'Cheese')] });
  App.openSheet('NPC Drops');
  run.flush();
  App.newRecord();

  const filter = document.getElementById('modal').querySelectorAll('[data-filter]')[0];
  filter.value = 'bat';
  fire(filter, 'input');
  fire(filter, 'keydown', { key: 'Enter' });
  run.flush();

  assert.equal(document.getElementById('modal').hidden, true);
  assert.match(document.getElementById('form').textContent, /Bat/);
});

test('Escape closes the parent picker and empties the modal', () => {
  // Emptied, not merely hidden — the gallery's rule, for the same reason.
  const { run } = boot({ 'NPC Drops': [DROP(1, 10)], NPCs: [NPC(1, 'Mouse')] });
  App.openSheet('NPC Drops');
  run.flush();
  App.newRecord();

  const dialog = document.getElementById('modal').querySelectorAll('[role=dialog]')[0];
  fire(dialog, 'keydown', { key: 'Escape' });

  assert.equal(document.getElementById('modal').hidden, true);
  assert.equal(document.getElementById('modal').querySelectorAll('*').length, 0);
});

test('a backdrop click closes the parent picker; a click inside does not', () => {
  const { run } = boot({ 'NPC Drops': [DROP(1, 10)], NPCs: [NPC(1, 'Mouse')] });
  App.openSheet('NPC Drops');
  run.flush();
  App.newRecord();

  const modal = document.getElementById('modal');
  fire(modal.querySelectorAll('[role=dialog]')[0], 'click');
  assert.equal(modal.hidden, false, 'a click inside the dialog must not close it');
  fire(modal, 'click');
  assert.equal(modal.hidden, true);
});

test('the Close button closes the parent picker', () => {
  const { run } = boot({ 'NPC Drops': [DROP(1, 10)], NPCs: [NPC(1, 'Mouse')] });
  App.openSheet('NPC Drops');
  run.flush();
  App.newRecord();

  fire(document.getElementById('modal').querySelectorAll('[data-close]')[0], 'click');
  assert.equal(document.getElementById('modal').hidden, true);
});

test('the parent picker says so when the parent list is absent, and still closes', () => {
  // Without this the modal opens EMPTY when loadReferencedSheets is slow or failed — no rows,
  // no message, no way out.
  const { run } = boot({ 'NPC Drops': [DROP(1, 10)], NPCs: [NPC(1, 'Mouse')] });
  App.openSheet('NPC Drops');
  run.flush();
  delete App.__state.pickerData.NPCs;
  App.newRecord();

  const modal = document.getElementById('modal');
  assert.equal(modal.querySelectorAll('[data-parent]').length, 0);
  assert.match(modal.textContent, /NPCs/);
  fire(modal.querySelectorAll('[data-close]')[0], 'click');
  assert.equal(modal.hidden, true);
});

test('adding the first row to a parent with none appends it', () => {
  const { run } = boot({ 'NPC Drops': [DROP(1, 10)], NPCs: [NPC(1, 'Mouse'), NPC(2, 'Bat')],
                         Items: [ITEM(10, 'Cheese')] });
  App.openSheet('NPC Drops');
  run.flush();
  App.openGroup('2');
  run.flush();
  fire(document.getElementById('form').querySelectorAll('[data-add]')[0], 'click');
  // Every column of NPC Drops is required, and addRow fills only the parent — so the blank row
  // has to be completed before it can be saved at all.
  const form = document.getElementById('form');
  form.querySelectorAll('[name=item_template_id]')[0].value = '10';
  form.querySelectorAll('[name=stack]')[0].value = '1';
  form.querySelectorAll('[name=droprate]')[0].value = '0.10';
  App.save();
  run.flush();

  const call = run.calls.filter((c) => c.name === 'saveBatch').pop();
  assert.ok(call, 'the first row of an empty group must be appended');
  assert.equal(call.args[0][0].appends.length, 1);
  const names = schemaOf('NPC Drops').columns.map((c) => c.name);
  assert.equal(call.args[0][0].appends[0].cells[names.indexOf('npc_template_id')], '2');
});

// A grouped sheet WITH a pk. Every row's id is in idSets.__self — the sheet it was read from is
// what built that set — so these are the rows that go unsavable if validate does not exempt a row
// from its own id.
const QUEST = (id, name) => rowFor('Quests', { id, name });
const REWARD = (id, questId, type, value) => rowFor('Quest Rewards', {
  id, quest_id: questId, reward_type: type, long_value: value,
});

test('a group on a sheet WITH a pk can be saved at all', () => {
  const { run } = boot({ 'Quest Rewards': [REWARD(1, 7, 'Gold', 100), REWARD(2, 7, 'Gold', 200)],
                         Quests: [QUEST(7, 'Bread')] });
  App.openSheet('Quest Rewards');
  run.flush();
  App.openGroup('7');
  run.flush();

  document.getElementById('form').querySelectorAll('[name=long_value]')[0].value = '150';
  App.save();
  run.flush();

  const call = run.calls.filter((c) => c.name === 'saveBatch').pop();
  assert.ok(call, 'every row is its own id\'s only owner; none may be flagged against itself');
  assert.equal(call.args[0][0].writes.length, 1);
});

test('a row added to a group on a sheet with a pk is appended with an allocated id', () => {
  const { run } = boot({ 'Quest Rewards': [REWARD(1, 7, 'Gold', 100)],
                         Quests: [QUEST(7, 'Bread')] });
  App.openSheet('Quest Rewards');
  run.flush();
  App.openGroup('7');
  run.flush();

  fire(document.getElementById('form').querySelectorAll('[data-add]')[0], 'click');
  const form = document.getElementById('form');
  const types = form.querySelectorAll('[name=reward_type]');
  types[types.length - 1].value = 'Gold';
  App.save();
  run.flush();

  const call = run.calls.filter((c) => c.name === 'saveBatch').pop();
  assert.ok(call, 'a freshly allocated id must not collide with the row it was allocated from');
  assert.equal(call.args[0][0].appends.length, 1);
  const names = schemaOf('Quest Rewards').columns.map((c) => c.name);
  assert.equal(call.args[0][0].appends[0].cells[names.indexOf('id')], '2');
});

test('a group save with no group open says so and writes nothing', () => {
  const { run } = boot({ 'NPC Drops': [DROP(1, 10)], NPCs: [NPC(1, 'Mouse')],
                         Items: [ITEM(10, 'Cheese')] });
  App.openSheet('NPC Drops');
  run.flush();

  App.save();
  run.flush();

  assert.equal(run.calls.filter((c) => c.name === 'saveBatch').length, 0);
  assert.match(document.getElementById('status').textContent, /Open a group first/);
});

test('a group save refuses while a referenced sheet failed to load', () => {
  // Validation waves an fk through when its id set is absent — it must, because the sets load
  // asynchronously — so a list that FAILED to load is a hole in validation, not a cosmetic gap.
  const { run } = boot({ 'NPC Drops': [DROP(1, 10)], NPCs: [NPC(1, 'Mouse')],
                         Items: [ITEM(10, 'Cheese')] }, { failIndexOn: 'Items' });
  App.openSheet('NPC Drops');
  run.flush();
  fire(document.getElementById('records').querySelectorAll('.record')[0], 'click');
  run.flush();

  document.getElementById('form').querySelectorAll('[name=droprate]')[0].value = '0.75';
  App.save();

  assert.equal(run.calls.filter((c) => c.name === 'saveBatch').length, 0);
  assert.match(document.getElementById('status').textContent, /Items/);
});

test('a successful group save reopens the same group', () => {
  // The reload is unconditional, so without this every save drops the user back to the parent
  // list having lost the place they were editing.
  const { run } = boot({ 'NPC Drops': [DROP(1, 10), DROP(2, 20)],
                         NPCs: [NPC(1, 'Mouse'), NPC(2, 'Bat')],
                         Items: [ITEM(10, 'Cheese'), ITEM(20, 'Tail')] });
  App.openSheet('NPC Drops');
  run.flush();
  fire(document.getElementById('records').querySelectorAll('.record')[1], 'click');
  run.flush();

  document.getElementById('form').querySelectorAll('[name=droprate]')[0].value = '0.75';
  App.save();
  run.flush();

  assert.equal(App.__state.group.key, '2');
  assert.equal(document.getElementById('form').querySelectorAll('[data-group-row]').length, 1);
});

test('a group cannot be opened while a save is in flight', () => {
  // Every save reloads the sheet, and the reload empties the form — so a group opened and edited
  // during the round trip would have those edits wiped with no warning. The panel is the only
  // edit surface on a grouped sheet, so refusing to leave the one being saved closes the window.
  const { run } = boot({ 'NPC Drops': [DROP(1, 10), DROP(2, 20)],
                         NPCs: [NPC(1, 'Mouse'), NPC(2, 'Bat')],
                         Items: [ITEM(10, 'Cheese'), ITEM(20, 'Tail')] });
  App.openSheet('NPC Drops');
  run.flush();
  fire(document.getElementById('records').querySelectorAll('.record')[0], 'click');
  run.flush();

  document.getElementById('form').querySelectorAll('[name=droprate]')[0].value = '0.75';
  App.save();
  App.openGroup('2');   // before the batch resolves

  assert.equal(App.__state.group.key, '1', 'the group being saved must stay on screen');
  assert.match(document.getElementById('status').textContent, /Still saving/);

  // And once it resolves, the reload's own reopen still works.
  run.flush();
  assert.equal(App.__state.group.key, '1');
});

test('a finished save\'s message is shown by openSheet when no group is reopened', () => {
  // The group a save was made on can be gone by the time the reload lands — its last row deleted
  // — leaving nothing to reopen and openSheet as the only place left to report the outcome.
  const { run } = boot({ Items: [ITEM(1, 'Sword')] });
  App.__state.pendingStatus = { sheet: 'Items', message: 'batch boom', warn: true };

  App.openSheet('Items');
  run.flush();

  assert.match(document.getElementById('status').textContent, /batch boom/,
               'the message must outlive the reload, not be buried by its record count');
  assert.equal(App.__state.pendingStatus, null, 'and it is consumed, not left to strike twice');
});

test('a finished save\'s message does not surface on a different sheet', () => {
  // The message outlives its save when the reload's read never lands. Unqualified, it then
  // reports 'batch boom' over a clean list of Items, blaming a sheet that was never saved.
  const { run } = boot({ 'NPC Drops': [DROP(1, 10)], NPCs: [NPC(1, 'Mouse')],
                         Items: [ITEM(1, 'Sword')] });
  App.__state.pendingStatus = { sheet: 'NPC Drops', message: 'batch boom', warn: true };

  App.openSheet('Items');
  run.flush();

  assert.doesNotMatch(document.getElementById('status').textContent, /boom/);
  assert.equal(App.__state.pendingStatus, null, 'and it is dropped, not left to strike later');
});

// --- discarding unsaved edits ----------------------------------------------------------------

function openDirtyGroup(run) {
  fire(document.getElementById('records').querySelectorAll('.record')[0], 'click');
  run.flush();
  const cell = document.getElementById('form').querySelectorAll('[name=droprate]')[0];
  cell.value = '0.99';
}

test('clicking another group with unsaved edits asks first, and Keep editing keeps them', () => {
  const { run } = boot({ 'NPC Drops': [DROP(1, 10), DROP(2, 20)],
                         NPCs: [NPC(1, 'Mouse'), NPC(2, 'Bat')], Items: [ITEM(10, 'Cheese')] });
  App.openSheet('NPC Drops');
  run.flush();
  openDirtyGroup(run);

  fire(document.getElementById('records').querySelectorAll('.record')[1], 'click');

  const modal = document.getElementById('modal');
  assert.equal(modal.hidden, false, 'the ask must appear');
  fire(modal.querySelectorAll('[data-keep]')[0], 'click');
  assert.equal(modal.hidden, true);
  // Group A is still on screen, edit intact.
  assert.equal(document.getElementById('form')
    .querySelectorAll('[name=droprate]')[0].value, '0.99');
});

test('Discard changes proceeds to the other group', () => {
  const { run } = boot({ 'NPC Drops': [DROP(1, 10), DROP(2, 20)],
                         NPCs: [NPC(1, 'Mouse'), NPC(2, 'Bat')], Items: [ITEM(10, 'Cheese')] });
  App.openSheet('NPC Drops');
  run.flush();
  openDirtyGroup(run);

  fire(document.getElementById('records').querySelectorAll('.record')[1], 'click');
  fire(document.getElementById('modal').querySelectorAll('[data-discard]')[0], 'click');
  run.flush();

  assert.match(document.getElementById('form').textContent, /Bat/);
});

test('an untouched panel navigates without asking', () => {
  const { run } = boot({ 'NPC Drops': [DROP(1, 10), DROP(2, 20)],
                         NPCs: [NPC(1, 'Mouse'), NPC(2, 'Bat')], Items: [ITEM(10, 'Cheese')] });
  App.openSheet('NPC Drops');
  run.flush();
  fire(document.getElementById('records').querySelectorAll('.record')[0], 'click');
  run.flush();

  fire(document.getElementById('records').querySelectorAll('.record')[1], 'click');
  assert.equal(document.getElementById('modal').hidden, true, 'no edits, no ask');
});

test('a removed row alone is enough to ask', () => {
  const { run } = boot({ 'NPC Drops': [DROP(1, 10), DROP(1, 20), DROP(2, 30)],
                         NPCs: [NPC(1, 'Mouse'), NPC(2, 'Bat')], Items: [ITEM(10, 'Cheese')] });
  App.openSheet('NPC Drops');
  run.flush();
  fire(document.getElementById('records').querySelectorAll('.record')[0], 'click');
  run.flush();
  fire(document.getElementById('form').querySelectorAll('[data-remove]')[0], 'click');

  fire(document.getElementById('records').querySelectorAll('.record')[1], 'click');
  assert.equal(document.getElementById('modal').hidden, false);
});

test('switching sheets with unsaved edits asks, and declining resets the sheet picker', () => {
  // The <select> flips to the new sheet the moment it is chosen; a declined switch must put it
  // back, or the control lies about what is on screen.
  const { run } = boot({ 'NPC Drops': [DROP(1, 10)], NPCs: [NPC(1, 'Mouse')],
                         Items: [ITEM(10, 'Cheese')] });
  App.openSheet('NPC Drops');
  run.flush();
  openDirtyGroup(run);

  const picker = document.getElementById('sheet-picker');
  picker.value = 'Items';
  fire(picker, 'change');

  const modal = document.getElementById('modal');
  assert.equal(modal.hidden, false);
  fire(modal.querySelectorAll('[data-keep]')[0], 'click');
  assert.equal(picker.value, 'NPC Drops');
  assert.equal(App.__state.sheetName, 'NPC Drops');
});

test('Escape declines and empties the modal, leaving the panel and its edit alone', () => {
  // Escape is an answer, and the only safe one to read into it is "keep editing" — destruction
  // happens by pressing the button that names it, never by a key that means "get out of here".
  const { run } = boot({ 'NPC Drops': [DROP(1, 10), DROP(2, 20)],
                         NPCs: [NPC(1, 'Mouse'), NPC(2, 'Bat')], Items: [ITEM(10, 'Cheese')] });
  App.openSheet('NPC Drops');
  run.flush();
  openDirtyGroup(run);

  fire(document.getElementById('records').querySelectorAll('.record')[1], 'click');
  const modal = document.getElementById('modal');
  fire(modal.querySelectorAll('[role=dialog]')[0], 'keydown', { key: 'Escape' });
  run.flush();

  assert.equal(modal.hidden, true);
  // Emptied, not merely hidden — the parent picker's rule, for the same reason.
  assert.equal(modal.querySelectorAll('*').length, 0);
  assert.equal(document.getElementById('form')
    .querySelectorAll('[name=droprate]')[0].value, '0.99', 'declined, not proceeded');
});

test('a backdrop click declines; a click inside the dialog decides nothing', () => {
  // The inside-click half pins confirmDiscard's `event.target === modal` check: the dialog is a
  // child of #modal, so without it every click on the message text would answer for the user —
  // and the answer the backdrop gives is one that walks away from their work.
  const { run } = boot({ 'NPC Drops': [DROP(1, 10), DROP(2, 20)],
                         NPCs: [NPC(1, 'Mouse'), NPC(2, 'Bat')], Items: [ITEM(10, 'Cheese')] });
  App.openSheet('NPC Drops');
  run.flush();
  openDirtyGroup(run);

  fire(document.getElementById('records').querySelectorAll('.record')[1], 'click');
  const modal = document.getElementById('modal');

  fire(modal.querySelectorAll('[role=dialog]')[0], 'click');
  assert.equal(modal.hidden, false, 'a click inside the dialog must not answer');

  fire(modal, 'click');
  run.flush();
  assert.equal(modal.hidden, true);
  assert.equal(modal.querySelectorAll('*').length, 0);
  assert.equal(document.getElementById('form')
    .querySelectorAll('[name=droprate]')[0].value, '0.99', 'declined, not proceeded');
});

test('Keep editing takes the focus when the ask opens', () => {
  // The safe answer is the default: a keyboard user who hits Enter on reflex must not lose work.
  const { run } = boot({ 'NPC Drops': [DROP(1, 10), DROP(2, 20)],
                         NPCs: [NPC(1, 'Mouse'), NPC(2, 'Bat')], Items: [ITEM(10, 'Cheese')] });
  App.openSheet('NPC Drops');
  run.flush();
  openDirtyGroup(run);

  fire(document.getElementById('records').querySelectorAll('.record')[1], 'click');
  const modal = document.getElementById('modal');
  assert.equal(modal.querySelectorAll('[data-keep]')[0].focusCalls, 1);
  assert.equal(modal.querySelectorAll('[data-discard]')[0].focusCalls, 0);
  // Named by its own message, not left as a bare "dialog" for a screen reader to announce.
  const dialog = modal.querySelectorAll('[role=dialog]')[0];
  const named = modal.querySelectorAll('[id=' + dialog.getAttribute('aria-labelledby') + ']');
  assert.equal(named.length, 1);
  assert.match(named[0].textContent, /unsaved change/);
});

test('closing the ask hands focus back to whatever raised it', () => {
  // The fake DOM has no activeElement model, so the control the dialog should return to is named
  // here rather than inferred — what is under test is that close() reads it and calls focus().
  const { run } = boot({ 'NPC Drops': [DROP(1, 10), DROP(2, 20)],
                         NPCs: [NPC(1, 'Mouse'), NPC(2, 'Bat')], Items: [ITEM(10, 'Cheese')] });
  App.openSheet('NPC Drops');
  run.flush();
  openDirtyGroup(run);

  const opener = document.getElementById('records').querySelectorAll('.record')[1];
  document.activeElement = opener;
  const before = opener.focusCalls;
  fire(opener, 'click');
  fire(document.getElementById('modal').querySelectorAll('[data-keep]')[0], 'click');

  assert.equal(opener.focusCalls, before + 1);
});

test('the reload after a save never asks', () => {
  // clearForm nulls #form.__group before openSheet re-reads, so the guard sees a clean panel.
  // Without that, every successful save would be followed by its own "discard changes?" — the
  // panel's controls still hold the edited values while the reload is in flight.
  const { run } = boot({ 'NPC Drops': [DROP(1, 10)], NPCs: [NPC(1, 'Mouse')],
                         Items: [ITEM(10, 'Cheese')] });
  App.openSheet('NPC Drops');
  run.flush();
  openDirtyGroup(run);
  App.save();
  run.flush();

  assert.equal(document.getElementById('modal').hidden, true);
  assert.match(document.getElementById('status').textContent, /Saved/);
});
