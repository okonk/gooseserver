import { test } from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import vm from 'node:vm';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { installFakeDom, fire } from './fake-dom.js';

const doc = installFakeDom();

const { Forms } = await import('../src/forms.js');
globalThis.Forms = Forms;
const { Sprites } = await import('../src/sprites.js');
globalThis.Sprites = Sprites;
const { Gallery } = await import('../src/gallery.js');
globalThis.Gallery = Gallery;

const here = path.dirname(fileURLToPath(import.meta.url));

// The REAL bundles, loaded exactly as sprites.test.js loads them and for the same reason: the
// counts this module has to get right — 4,827 icons over 125 sheet files, 8 part categories, 560
// effects — are facts about the committed atlas, and a toy fixture cannot check them.
const real = (() => {
  const ctx = vm.createContext({});
  for (const name of ['icons', 'parts', 'effects']) {
    const html = fs.readFileSync(path.join(here, '..', `sprites-${name}.html`), 'utf8');
    vm.runInContext(html.replace(/<\/?script>/g, ''), ctx);
  }
  return ctx.GOOSE_SPRITES;
})();

// --- the indexes ------------------------------------------------------------------------------

test('the icon index groups every key in the real bundle into its sheet file', () => {
  const entries = Gallery.iconEntries(real.icons);
  assert.equal(entries.length, 4827);
  assert.equal(Object.keys(real.icons.rects).length, 4827);

  const sheets = Gallery.iconSheets(real.icons);
  assert.equal(sheets.length, 125);
  // The counts partition the bundle: no key is dropped and none is counted twice.
  assert.equal(sheets.reduce((sum, s) => sum + s.count, 0), 4827);
  // And each count really is the number of entries for that sheet.
  sheets.forEach((s) => {
    assert.equal(entries.filter((e) => e.sheet === s.sheet).length, s.count);
  });
});

test('the sheet list is in numeric order, not string order', () => {
  const sheets = Gallery.iconSheets(real.icons).map((s) => Number(s.sheet));
  const sorted = sheets.slice().sort((a, b) => a - b);
  assert.deepEqual(sheets, sorted);
});

test('icon entries carry the rect and the two cells a pick writes', () => {
  const entries = Gallery.iconEntries({
    rects: { '104:7': [1, 2, 3, 4] },
  });
  assert.equal(entries.length, 1);
  assert.deepEqual(entries[0].rect, [1, 2, 3, 4]);
  assert.deepEqual(entries[0].pick, { sheet: '104', graphic: '7' });
});

test('the part index deduplicates the four clips down to one tile per id', () => {
  const entries = Gallery.partEntries(real.parts);
  const keys = new Set(entries.map((e) => e.category + ':' + e.id));
  assert.equal(keys.size, entries.length, 'a category:id appears twice');

  const categories = Gallery.partCategories(real.parts);
  assert.deepEqual(categories.map((c) => c.category),
    ['Bodies', 'Chest', 'Eyes', 'Feet', 'Hair', 'Hands', 'Helms', 'Legs']);
  assert.equal(categories.reduce((sum, c) => sum + c.count, 0), entries.length);
});

test('a part tile shows the clip Sprites.clipCandidates would pick', () => {
  // Unarmed order: idle-no-equip-down, idle-down, idle-equip-down. The Hands sheets only ever
  // ship idle-equip, so they must fall all the way through rather than be dropped.
  const bundle = {
    rects: {
      'Chest:5:idle-down': [0, 0, 8, 8],
      'Chest:5:idle-no-equip-down': [8, 0, 8, 8],
      'Hands:9:idle-equip-down': [16, 0, 8, 8],
    },
  };
  const entries = Gallery.partEntries(bundle);
  const chest = entries.find((e) => e.category === 'Chest');
  assert.deepEqual(chest.rect, [8, 0, 8, 8], 'idle-no-equip-down is first for an unarmed pose');
  const hands = entries.find((e) => e.category === 'Hands');
  assert.deepEqual(hands.rect, [16, 0, 8, 8], 'a Hands id with only idle-equip is still listed');
  assert.deepEqual(chest.pick, { category: 'Chest', id: '5' });
});

test('the effect index is one tile per id, at frame 0', () => {
  const entries = Gallery.effectEntries(real.effects);
  assert.equal(entries.length, 560);
  entries.forEach((e) => {
    assert.deepEqual(e.rect, real.effects.rects[e.id + ':0']);
  });
  assert.deepEqual(Gallery.effectEntries({ rects: { '4:0': [0, 0, 1, 1], '4:1': [1, 1, 1, 1] } })
    .map((e) => e.pick), [{ id: '4' }]);
});

// --- filtering --------------------------------------------------------------------------------

const iconFixture = {
  width: 64, height: 64, png: 'data:image/png;base64,AAA',
  rects: {
    '104:1': [0, 0, 32, 32], '104:12': [32, 0, 32, 32], '104:2': [0, 32, 32, 32],
    '200:1': [32, 32, 32, 32],
  },
};

test('the sheet filter keeps only that sheet', () => {
  const all = Gallery.iconEntries(iconFixture);
  const only = Gallery.filterEntries(all, { sheet: '104' });
  assert.deepEqual(only.map((e) => e.graphic), ['1', '2', '12']);
});

test('search by id narrows to matching graphics, by prefix', () => {
  const all = Gallery.filterEntries(Gallery.iconEntries(iconFixture), { sheet: '104' });
  assert.deepEqual(Gallery.filterEntries(all, { query: '1' }).map((e) => e.graphic), ['1', '12']);
  assert.deepEqual(Gallery.filterEntries(all, { query: '12' }).map((e) => e.graphic), ['12']);
  // A prefix, not a substring: '2' must not list 12.
  assert.deepEqual(Gallery.filterEntries(all, { query: '2' }).map((e) => e.graphic), ['2']);
  // Whitespace is not a query.
  assert.equal(Gallery.filterEntries(all, { query: '  ' }).length, 3);
});

test('the category filter keeps only that category', () => {
  const entries = Gallery.partEntries({
    rects: { 'Helms:1:idle-down': [0, 0, 8, 8], 'Feet:1:idle-down': [8, 0, 8, 8] },
  });
  assert.deepEqual(Gallery.filterEntries(entries, { category: 'Helms' }).map((e) => e.category),
    ['Helms']);
});

// --- windowing --------------------------------------------------------------------------------

test('the window is the visible rows plus two rows of overscan either side', () => {
  const w = Gallery.windowFor(1000, 0, Gallery.ROW_HEIGHT * 5);
  assert.equal(w.first, 0);
  // 5 visible rows plus 2 rows of overscan below; there is nothing above row 0.
  assert.equal(w.last, 7 * Gallery.COLUMNS);
  assert.equal(w.topPad, 0);
  assert.equal(w.bottomPad, (Math.ceil(1000 / Gallery.COLUMNS) - 7) * Gallery.ROW_HEIGHT);
});

test('a scrolled window starts two rows above the first visible row and pads for the rest', () => {
  const w = Gallery.windowFor(1000, Gallery.ROW_HEIGHT * 10, Gallery.ROW_HEIGHT * 5);
  assert.equal(w.first, 8 * Gallery.COLUMNS);
  assert.equal(w.topPad, 8 * Gallery.ROW_HEIGHT);
  // The spacers plus the rendered rows always add up to the full list height, or the scrollbar
  // would jump as the user scrolled.
  const rows = Math.ceil(1000 / Gallery.COLUMNS);
  const rendered = (w.last - w.first) / Gallery.COLUMNS;
  assert.equal(w.topPad + rendered * Gallery.ROW_HEIGHT + w.bottomPad, rows * Gallery.ROW_HEIGHT);
});

test('the last window stops at the end of the list', () => {
  const w = Gallery.windowFor(10, Gallery.ROW_HEIGHT * 100, Gallery.ROW_HEIGHT * 5);
  assert.equal(w.last, 10);
  assert.equal(w.bottomPad, 0);
});

// --- the modal ---------------------------------------------------------------------------------

const modal = doc.appendChild(doc.createElement('div'));
modal.id = 'modal';

function tiles() {
  return modal.querySelectorAll('[data-gal="tile"]');
}

function node(role) {
  return modal.querySelector('[data-gal="' + role + '"]');
}

function opener() {
  const button = doc.createElement('button');
  doc.body.appendChild(button);
  return button;
}

test.afterEach(() => Gallery.close());

test('opening renders a labelled modal dialog and focuses the search field', () => {
  const button = opener();
  Gallery.open({ bundle: 'icons', bundles: { icons: iconFixture }, opener: button });

  assert.equal(modal.hidden, false);
  const dialog = node('dialog');
  assert.equal(dialog.getAttribute('role'), 'dialog');
  assert.equal(dialog.getAttribute('aria-modal'), 'true');
  const heading = node('title');
  assert.equal(dialog.getAttribute('aria-labelledby'), heading.getAttribute('id'));
  assert.ok(heading.textContent.length > 0);
  assert.equal(node('search').focusCalls, 1);
});

test('a pick reports the two icon cells and closes', () => {
  const picked = [];
  const button = opener();
  Gallery.open({
    bundle: 'icons', bundles: { icons: iconFixture }, opener: button,
    onPick: (v) => picked.push(v),
  });

  const tile = tiles().find((t) => t.getAttribute('data-id') === '12');
  fire(tile, 'click');

  assert.deepEqual(picked, [{ sheet: '104', graphic: '12' }]);
  assert.equal(modal.hidden, true);
  assert.equal(modal.innerHTML, '', 'the modal must not keep stale tiles behind hidden');
  assert.equal(button.focusCalls, 1, 'focus returns to the invoking Browse button');
});

test('Escape closes without picking', () => {
  let picks = 0;
  const button = opener();
  Gallery.open({
    bundle: 'icons', bundles: { icons: iconFixture }, opener: button, onPick: () => { picks++; },
  });

  fire(node('search'), 'keydown', { key: 'Escape' });
  assert.equal(picks, 0);
  assert.equal(modal.hidden, true);
  assert.equal(button.focusCalls, 1);
});

test('Enter picks the tile the cursor is on', () => {
  const picked = [];
  Gallery.open({
    bundle: 'icons', bundles: { icons: iconFixture }, opener: opener(),
    filter: { sheet: '104' }, onPick: (v) => picked.push(v),
  });

  const grid = node('scroll');
  fire(grid, 'keydown', { key: 'ArrowRight' });
  fire(grid, 'keydown', { key: 'Enter' });
  assert.deepEqual(picked, [{ sheet: '104', graphic: '2' }]);
});

test('the sheet chooser defaults to the sheet the record already names', () => {
  Gallery.open({
    bundle: 'icons', bundles: { icons: iconFixture }, opener: opener(),
    filter: { sheet: '200' },
  });
  assert.equal(node('sheet').value, '200');
  assert.deepEqual(tiles().map((t) => t.getAttribute('data-id')), ['1']);
});

test('an unknown sheet falls back to the first one rather than showing nothing', () => {
  Gallery.open({
    bundle: 'icons', bundles: { icons: iconFixture }, opener: opener(),
    filter: { sheet: '999' },
  });
  assert.equal(node('sheet').value, '104');
  assert.equal(tiles().length, 3);
});

test('the current graphic is selected and scrolled to on open', () => {
  const many = { width: 64, height: 64, png: 'x', rects: {} };
  for (let i = 1; i <= 400; i++) many.rects['104:' + i] = [0, 0, 32, 32];

  Gallery.open({
    bundle: 'icons', bundles: { icons: many }, opener: opener(),
    filter: { sheet: '104' }, current: { sheet: '104', graphic: '200' },
  });

  const scroll = node('scroll');
  assert.ok(scroll.scrollTop > 0, 'the current tile was not scrolled to');
  const selected = tiles().filter((t) => t.getAttribute('aria-selected') === 'true');
  assert.equal(selected.length, 1);
  assert.equal(selected[0].getAttribute('data-id'), '200');
  assert.equal(scroll.getAttribute('aria-activedescendant'), selected[0].getAttribute('id'));
});

test('windowing renders a bounded number of tiles and the slice the scroll offset lands on', () => {
  const many = { width: 64, height: 64, png: 'x', rects: {} };
  for (let i = 1; i <= 4000; i++) many.rects['104:' + i] = [0, 0, 32, 32];

  Gallery.open({
    bundle: 'icons', bundles: { icons: many }, opener: opener(), filter: { sheet: '104' },
  });

  const scroll = node('scroll');
  scroll.clientHeight = Gallery.ROW_HEIGHT * 6;
  scroll.scrollTop = 0;
  fire(scroll, 'scroll');

  const shown = tiles();
  assert.ok(shown.length < 60, '4,000 tiles in the DOM is a hang: got ' + shown.length);
  // 6 visible rows plus 2 rows of overscan below, from row 0.
  assert.equal(shown.length, 8 * Gallery.COLUMNS);
  assert.equal(shown[0].getAttribute('data-id'), '1');

  scroll.scrollTop = Gallery.ROW_HEIGHT * 100;
  fire(scroll, 'scroll');
  const later = tiles();
  assert.equal(later.length, 10 * Gallery.COLUMNS);
  // Ids are 1-based and the entries are sorted numerically, so row 98 starts at id 98*4 + 1.
  assert.equal(later[0].getAttribute('data-id'), String(98 * Gallery.COLUMNS + 1));
  assert.equal(node('pad-top').style.height, (98 * Gallery.ROW_HEIGHT) + 'px');
});

test('a tile is a CSS atlas window, not a canvas', () => {
  Gallery.open({
    bundle: 'icons', bundles: { icons: iconFixture }, opener: opener(), filter: { sheet: '104' },
  });

  // Graphic 2 sits at y=32 in the atlas, so its offset is one that a zero would also satisfy.
  const tile = tiles().find((t) => t.getAttribute('data-id') === '2');
  assert.equal(tile.tagName, 'BUTTON');
  assert.equal(modal.getElementsByTagName('canvas').length, 0);
  // 32px art in a 64px cell is drawn at 2x, so the whole 64x64 atlas is scaled to 128.
  assert.equal(tile.style.backgroundSize, '128px 128px');
  assert.equal(tile.style.backgroundPosition, '0px -64px');
  // The atlas PNG is named ONCE, in a stylesheet, not copied into 4,827 inline styles.
  assert.ok(node('atlas').textContent.indexOf(iconFixture.png) !== -1);
});

test('a sprite too big for the cell is scaled down rather than clipped', () => {
  const big = { width: 256, height: 256, png: 'x', rects: { '104:1': [0, 128, 128, 128] } };
  Gallery.open({ bundle: 'icons', bundles: { icons: big }, opener: opener() });
  const tile = tiles()[0];
  // 128 art into a 64 cell is 0.5x: the atlas halves and the offset halves with it.
  assert.equal(tile.style.backgroundSize, '128px 128px');
  assert.equal(tile.style.backgroundPosition, '0px -64px');
});

test('the parts filter locked to a category never lists another one', () => {
  const bundle = {
    width: 64, height: 64, png: 'x',
    rects: {
      'Helms:1:idle-down': [0, 0, 8, 8], 'Helms:2:idle-down': [8, 0, 8, 8],
      'Feet:1:idle-down': [16, 0, 8, 8],
    },
  };
  Gallery.open({
    bundle: 'parts', bundles: { parts: bundle }, opener: opener(),
    filter: { category: 'Helms', locked: true },
  });

  assert.equal(tiles().length, 2);
  // No chooser at all: a disabled <select> that cannot change is a control that lies.
  assert.equal(node('category'), null);
  assert.ok(node('locked').textContent.indexOf('Helms') !== -1);
  // And the search cannot reach out of the category either.
  const search = node('search');
  search.value = '1';
  fire(search, 'input');
  assert.deepEqual(tiles().map((t) => t.getAttribute('data-id')), ['1']);
});

test('an unlocked parts filter offers every category', () => {
  const bundle = {
    width: 64, height: 64, png: 'x',
    rects: { 'Helms:1:idle-down': [0, 0, 8, 8], 'Feet:1:idle-down': [8, 0, 8, 8] },
  };
  Gallery.open({ bundle: 'parts', bundles: { parts: bundle }, opener: opener() });
  const chooser = node('category');
  assert.equal(chooser.getElementsByTagName('option').length, 2);
  assert.equal(node('locked'), null);
});

test('a part pick reports the category and the id', () => {
  const picked = [];
  Gallery.open({
    bundle: 'parts', opener: opener(), onPick: (v) => picked.push(v),
    bundles: { parts: { width: 64, height: 64, png: 'x',
                        rects: { 'Helms:7:idle-down': [0, 0, 8, 8] } } },
  });
  fire(tiles()[0], 'click');
  assert.deepEqual(picked, [{ category: 'Helms', id: '7' }]);
});

test('an effect pick reports the id alone', () => {
  const picked = [];
  Gallery.open({
    bundle: 'effects', opener: opener(), onPick: (v) => picked.push(v),
    bundles: { effects: { width: 64, height: 64, png: 'x',
                          rects: { '9:0': [0, 0, 16, 16], '9:1': [16, 0, 16, 16] } } },
  });
  assert.deepEqual(tiles().length, 1);
  fire(tiles()[0], 'click');
  assert.deepEqual(picked, [{ id: '9' }]);
});

test('opening twice leaves no duplicate nodes', () => {
  const first = opener();
  Gallery.open({ bundle: 'icons', bundles: { icons: iconFixture }, opener: first });
  const before = tiles().length;

  const second = opener();
  Gallery.open({ bundle: 'icons', bundles: { icons: iconFixture }, opener: second });

  assert.equal(modal.querySelectorAll('[data-gal="dialog"]').length, 1);
  assert.equal(tiles().length, before);
  // The first opener does NOT get focus back: the second open supersedes it, and throwing focus
  // at a button the user has moved on from would take it off the dialog they just opened.
  assert.equal(first.focusCalls, 0);
});

test('a click on the backdrop closes the dialog', () => {
  const button = opener();
  Gallery.open({ bundle: 'icons', bundles: { icons: iconFixture }, opener: button });
  fire(modal, 'click');
  assert.equal(modal.hidden, true);
  assert.equal(button.focusCalls, 1);
});

test('a missing bundle says so rather than opening an empty grid', () => {
  Gallery.open({ bundle: 'icons', bundles: {}, opener: opener() });
  assert.equal(tiles().length, 0);
  assert.ok(node('count').textContent.length > 0);
});

test('keyboard navigation walks the grid and clamps at both ends', () => {
  const many = { width: 64, height: 64, png: 'x', rects: {} };
  for (let i = 1; i <= 40; i++) many.rects['104:' + i] = [0, 0, 32, 32];
  Gallery.open({
    bundle: 'icons', bundles: { icons: many }, opener: opener(), filter: { sheet: '104' },
  });

  const scroll = node('scroll');
  const current = () => tiles().find((t) => t.getAttribute('aria-selected') === 'true')
    .getAttribute('data-id');

  assert.equal(current(), '1');
  fire(scroll, 'keydown', { key: 'ArrowLeft' });
  assert.equal(current(), '1', 'clamped at the start');
  fire(scroll, 'keydown', { key: 'ArrowDown' });
  assert.equal(current(), String(1 + Gallery.COLUMNS));
  fire(scroll, 'keydown', { key: 'ArrowUp' });
  assert.equal(current(), '1');
  fire(scroll, 'keydown', { key: 'End' });
  assert.equal(current(), '40', 'End goes to the last graphic');
  fire(scroll, 'keydown', { key: 'ArrowRight' });
  assert.equal(current(), '40', 'clamped at the end');
  fire(scroll, 'keydown', { key: 'Home' });
  assert.equal(current(), '1');
  fire(scroll, 'keydown', { key: 'PageDown' });
  assert.notEqual(current(), '1');
  fire(scroll, 'keydown', { key: 'PageUp' });
  assert.equal(current(), '1');
});

test('an arrow key is not also allowed to scroll the page', () => {
  Gallery.open({ bundle: 'icons', bundles: { icons: iconFixture }, opener: opener() });
  assert.equal(fire(node('scroll'), 'keydown', { key: 'ArrowDown' }), false);
});

test('ArrowDown in the search field moves into the grid', () => {
  Gallery.open({ bundle: 'icons', bundles: { icons: iconFixture }, opener: opener() });
  fire(node('search'), 'keydown', { key: 'ArrowDown' });
  assert.equal(node('scroll').focusCalls, 1);
});

test('a search that matches nothing says so and picks nothing on Enter', () => {
  let picks = 0;
  Gallery.open({
    bundle: 'icons', bundles: { icons: iconFixture }, opener: opener(),
    filter: { sheet: '104' }, onPick: () => { picks++; },
  });
  const search = node('search');
  search.value = '9999';
  fire(search, 'input');

  assert.equal(tiles().length, 0);
  assert.ok(node('count').textContent.indexOf('0') !== -1);
  fire(node('scroll'), 'keydown', { key: 'Enter' });
  assert.equal(picks, 0);
});

test('close() on a dialog that is not open does nothing', () => {
  Gallery.close();
  assert.equal(modal.hidden, true);
});

test('closing leaves no listener on the modal host, which outlives the dialog', () => {
  // #modal is in the markup and every open adds a backdrop listener to it. One left behind would
  // hold that open's closure — and its 4,827 entries — alive, one per open, for as long as the
  // sidebar stayed up.
  const before = modal._listeners.get('click');
  const count = before ? before.length : 0;

  for (let i = 0; i < 3; i++) {
    Gallery.open({ bundle: 'icons', bundles: { icons: iconFixture }, opener: opener() });
    Gallery.close();
  }

  const after = modal._listeners.get('click');
  assert.equal(after ? after.length : 0, count);
});

test('a stale scroll offset past the end of a narrowed list still renders the tail', () => {
  // The search shrinks the list under a scrollTop the browser set for the long one. Left
  // unclamped, the whole window lands past the last row: no tiles, and a topPad tall enough that
  // it looks like a grid that simply has not painted.
  const w = Gallery.windowFor(8, Gallery.ROW_HEIGHT * 500, Gallery.ROW_HEIGHT * 5);
  assert.equal(w.first, 0);
  assert.equal(w.last, 8);
  assert.equal(w.topPad, 0);
  assert.equal(w.bottomPad, 0);
});
