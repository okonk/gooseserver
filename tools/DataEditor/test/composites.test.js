import { test } from 'node:test';
import assert from 'node:assert/strict';
import { installFakeDom, fire, walk } from './fake-dom.js';

installFakeDom();

const { Layout } = await import('../src/layout.js');
globalThis.Layout = Layout;
const { Forms } = await import('../src/forms.js');
globalThis.Forms = Forms;
const { Sprites } = await import('../src/sprites.js');
globalThis.Sprites = Sprites;
const { Equipped } = await import('../src/equipped.js');
globalThis.Equipped = Equipped;
const { Appearance } = await import('../src/appearance.js');
globalThis.Appearance = Appearance;
const { Pickers } = await import('../src/pickers.js');
globalThis.Pickers = Pickers;
const { ColorPicker } = await import('../src/colorpicker.js');
globalThis.ColorPicker = ColorPicker;

const { Composites } = await import('../src/composites.js');
globalThis.Composites = Composites;

// --- bit conventions ------------------------------------------------------------------------
//
// Goose/Class.cs:34 — CanUse(mask) is `(mask & 2^ClassID) == 0`. So the bit INDEX is the
// class_id itself (bit 0 belongs to no class; the shipped classes are 1-6), and a SET bit means
// the class is RESTRICTED. These helpers are therefore a plain bit-index <-> id mapping.

test('bitsToIds returns the set bit indices, which ARE the class ids', () => {
  // 31 = 0b011111: every bit but 5 — the Priest-only scrolls in the shipped data.
  assert.deepEqual(Composites.bitsToIds(31), [0, 1, 2, 3, 4]);
  assert.deepEqual(Composites.bitsToIds(0), []);
  assert.deepEqual(Composites.bitsToIds(1), [0]);
  assert.deepEqual(Composites.bitsToIds(64), [6]);
});

test('bitsToIds covers every class_restrictions value in the shipped data', () => {
  // THE COMPLETE SET, from every INSERT in CsvToSql/CsvToSql.Console/illutiaData.sql — the file
  // the CSVs SchemaGen reads actually produce. (Goose/bin/Debug/IllutiaGoose.db is a different,
  // legacy dataset with 7 classes and disjoint item names; its masks are not tested here
  // because the editor will never open one.) 253 is the quests value; Quests declares
  // class_restrictions but has no Bitmask composite, so this control never sees it.
  //
  // Decoded as "clear bit = may use", every one of these names the class the item is for,
  // which is what makes the convention in Class.cs:34 checkable rather than merely asserted.
  const mayUse = (mask) => [1, 2, 3, 4, 5, 6]
    .filter((id) => Composites.bitsToIds(mask).indexOf(id) === -1);

  assert.deepEqual(mayUse(59), [2, 6]);          // Scroll: Backstab      -> Rogue
  assert.deepEqual(mayUse(55), [3, 6]);          // Scroll: Taunt         -> Warrior
  assert.deepEqual(mayUse(47), [4, 6]);          // Elemental Strike      -> Magus
  assert.deepEqual(mayUse(31), [5, 6]);          // Scroll: Healing       -> Priest
  assert.deepEqual(mayUse(15), [4, 5, 6]);       // Root, Gate            -> Magus + Priest
  assert.deepEqual(mayUse(51), [2, 3, 6]);       // Winter Blade          -> Rogue + Warrior
  assert.deepEqual(mayUse(19), [2, 3, 5, 6]);    // Leather Cap
  assert.deepEqual(mayUse(37), [1, 3, 4, 6]);    // Blushing Coat
  assert.deepEqual(mayUse(1), [1, 2, 3, 4, 5, 6]);   // Snake Tiara -> everybody

  // The four masks that leave BIT 0 CLEAR. Bit 0 belongs to no class, so it is a foreign bit
  // whichever way it sits, and these are the only shipped values that exercise that.
  assert.deepEqual(mayUse(22), [3, 5, 6]);       // Small Hammer  -> Warrior + Priest
  assert.deepEqual(mayUse(34), [2, 3, 4, 6]);    // Small Dagger
  assert.deepEqual(mayUse(38), [3, 4, 6]);       // Wooden Stave  -> Warrior + Magus
  assert.deepEqual(mayUse(50), [2, 3, 6]);       // Small Sword   -> Rogue + Warrior

  // Class 6 (Game Master) is unrestricted by every shipped mask, and bit 7 is set by none of
  // the item or spell ones.
  [1, 15, 19, 22, 31, 34, 37, 38, 47, 50, 51, 55, 59].forEach((mask) => {
    assert.ok(mayUse(mask).indexOf(6) !== -1, mask + ' restricts the Game Master');
    assert.ok(Composites.bitsToIds(mask).indexOf(7) === -1, mask + ' sets bit 7');
  });
});

test('bitsToIds handles bits past 31 — & would wrap to int32 there', () => {
  assert.deepEqual(Composites.bitsToIds(Math.pow(2, 31)), [31]);
  assert.deepEqual(Composites.bitsToIds(Math.pow(2, 32)), [32]);
  assert.deepEqual(Composites.bitsToIds(Math.pow(2, 52)), [52]);
  // The sign bit of an int32: `mask & 2**31` is NEGATIVE, and a naive loop using `&` would
  // still report it, but 2**32 would come back as 0 — silently unrestricting every class.
  assert.deepEqual(Composites.bitsToIds(Math.pow(2, 32) + 1), [0, 32]);
});

test('bitsToIds coerces a spreadsheet cell', () => {
  assert.deepEqual(Composites.bitsToIds('22'), [1, 2, 4]);
  assert.deepEqual(Composites.bitsToIds(''), []);
  assert.deepEqual(Composites.bitsToIds(null), []);
  assert.deepEqual(Composites.bitsToIds(undefined), []);
  assert.deepEqual(Composites.bitsToIds('abc'), []);
  // A negative mask is not a mask. Reporting bits for it would invent restrictions.
  assert.deepEqual(Composites.bitsToIds(-1), []);
});

test('idsToBits sets 2^id per id', () => {
  assert.equal(Composites.idsToBits([0, 1, 2, 3, 4, 6, 7]), 223);
  assert.equal(Composites.idsToBits([]), 0);
  assert.equal(Composites.idsToBits([6]), 64);
  assert.equal(Composites.idsToBits([32]), Math.pow(2, 32));
});

test('idsToBits does not double-count a duplicate id', () => {
  // `mask += 2^id` without a guard turns [1,1] into 4 — the Rogue restriction silently
  // becomes a Warrior one.
  assert.equal(Composites.idsToBits([1, 1]), 2);
  assert.equal(Composites.idsToBits(['3', 3]), 8);
});

test('idsToBits ignores an id that is not a whole number in range', () => {
  assert.equal(Composites.idsToBits(['abc']), 0);
  assert.equal(Composites.idsToBits([-1]), 0);
  assert.equal(Composites.idsToBits([1.5]), 0);
  assert.equal(Composites.idsToBits([53]), 0);
  assert.equal(Composites.idsToBits([null, undefined]), 0);
  assert.equal(Composites.idsToBits(null), 0);
});

test('bitmask round-trips', () => {
  [0, 1, 15, 19, 22, 31, 34, 37, 38, 47, 50, 51, 55, 59, 253, 1023,
    Math.pow(2, 31), Math.pow(2, 40), Math.pow(2, 52) + 1].forEach((mask) => {
    assert.equal(Composites.idsToBits(Composites.bitsToIds(mask)), mask,
      'round trip failed for ' + mask);
  });
});

// --- id lists -------------------------------------------------------------------------------

test('idList splits on space or comma', () => {
  // NPCHandler.cs accepts both.
  assert.deepEqual(Composites.parseIdList('1 2 3'), ['1', '2', '3']);
  assert.deepEqual(Composites.parseIdList('1,2,3'), ['1', '2', '3']);
  assert.deepEqual(Composites.parseIdList('1, 2  3'), ['1', '2', '3']);
  assert.deepEqual(Composites.parseIdList('  1\t2\n3  '), ['1', '2', '3']);
  assert.deepEqual(Composites.parseIdList(''), []);
  assert.deepEqual(Composites.parseIdList('   '), []);
  assert.deepEqual(Composites.parseIdList(null), []);
  // A numeric cell from the Sheets API, not a string.
  assert.deepEqual(Composites.parseIdList(7), ['7']);
});

test('idList writes space-separated', () => {
  assert.equal(Composites.formatIdList(['1', '2', '3']), '1 2 3');
  assert.equal(Composites.formatIdList([]), '');
  assert.equal(Composites.formatIdList(null), '');
});

// --- colour ---------------------------------------------------------------------------------

test('rgba blend alpha of zero means no tint', () => {
  assert.equal(Composites.isTinted({ r: 255, g: 0, b: 0, a: 0 }), false);
  assert.equal(Composites.isTinted({ r: 255, g: 0, b: 0, a: 1 }), true);
  assert.equal(Composites.isTinted({ r: 255, g: 0, b: 0, a: '0' }), false);
  assert.equal(Composites.isTinted({ r: 255, g: 0, b: 0, a: '128' }), true);
  assert.equal(Composites.isTinted(null), false);
  assert.equal(Composites.isTinted({}), false);
});

// --- control plumbing -----------------------------------------------------------------------

function named(node, name) {
  return node.querySelector('[name="' + name + '"]');
}

function valuesOf(node, names) {
  return names.map((n) => {
    const found = named(node, n);
    return found ? found.value : null;
  });
}

const RGBA = { kind: 'Rgba', columns: ['body_r', 'body_g', 'body_b', 'body_a'] };
const BITMASK = { kind: 'Bitmask', columns: ['class_restrictions'], source: 'Classes' };
const IDLIST = { kind: 'IdList', columns: ['quest_ids'], source: 'Quests' };
const EQUIP = { kind: 'EquipSlots', columns: ['equipped_items'] };

const CLASSES = [
  { id: 1, name: 'Commoner' }, { id: 2, name: 'Rogue' }, { id: 3, name: 'Warrior' },
  { id: 4, name: 'Magus' }, { id: 5, name: 'Priest' }, { id: 6, name: 'Game Master' },
];

function ctx(extra) {
  return Object.assign({
    pickerData: { Classes: CLASSES, Quests: [{ id: '10', name: 'Rat Hunt' }] },
    bundles: {},
    images: {},
    onImagesReady() {},
  }, extra || {});
}

function byName(names) {
  const map = Object.create(null);
  names.forEach((n) => { map[n] = { name: n, kind: 'Int', default: '0' }; });
  return map;
}

// The rgba control's colour and blend now live in a ColorPicker popover, so its tests drive it
// the way a user does: open the swatch, then type a hex or click down the blend strip. The
// picker's own behaviour is colorpicker.test.js's business; these only care that the four cells
// follow.
function swatchOf(node) {
  return node.querySelector('[class="swatch"]');
}

function openPicker(node) {
  const swatch = swatchOf(node);
  if (node.querySelector('[class="cp-pop"]').hidden) fire(swatch, 'click');
  return node;
}

function setColour(node, hex) {
  openPicker(node);
  const field = node.querySelector('[class="cp-hex"]');
  field.value = hex;
  fire(field, 'input');
}

function setBlend(node, blend) {
  openPicker(node);
  const strip = node.querySelector('[class="cp-alpha"]');
  // A 255-tall strip with full blend at the top, so the click lands on a whole byte.
  strip.rect = { left: 0, top: 0, width: 14, height: 255 };
  fire(strip, 'mousedown', { clientX: 0, clientY: 255 - blend });
}

// --- rgbaControl ----------------------------------------------------------------------------

test('rgba writes the four cells VERBATIM until the user touches the control', () => {
  // Merely opening a record must not rewrite it. Task 3's review established that a stored
  // zero-alpha tint with non-zero rgb is real data; blanking all four cells on a zero blend
  // would destroy exactly that.
  const values = { body_r: '12', body_g: '34', body_b: '56', body_a: 0 };
  const node = Composites.control({
    comp: RGBA, byName: byName(RGBA.columns), values: values, ctx: ctx(),
  });
  assert.deepEqual(valuesOf(node, RGBA.columns), ['12', '34', '56', '0']);
});

test('rgba leaves blank cells blank until touched', () => {
  const node = Composites.control({
    comp: RGBA, byName: byName(RGBA.columns), values: {}, ctx: ctx(),
  });
  assert.deepEqual(valuesOf(node, RGBA.columns), ['', '', '', '']);
});

test('rgba shows the stored colour in the swatch', () => {
  const values = { body_r: 255, body_g: 128, body_b: 0, body_a: 64 };
  const node = Composites.control({
    comp: RGBA, byName: byName(RGBA.columns), values: values, ctx: ctx(),
  });
  assert.equal(swatchOf(node).getAttribute('data-color'), '#ff8000');
  assert.equal(node.querySelector('[class="readout"]').textContent, '64 / 255 blend');
});

test('rgba clamps an out-of-range stored channel into the swatch', () => {
  const values = { body_r: 999, body_g: -4, body_b: 'x', body_a: 900 };
  const node = Composites.control({
    comp: RGBA, byName: byName(RGBA.columns), values: values, ctx: ctx(),
  });
  assert.equal(swatchOf(node).getAttribute('data-color'), '#ff0000');
  assert.equal(node.querySelector('[class="readout"]').textContent, '255 / 255 blend');
  // ...but the cells still hold the originals, because nothing was touched.
  assert.deepEqual(valuesOf(node, RGBA.columns), ['999', '-4', 'x', '900']);
});

test('rgba writes all four cells once the blend moves', () => {
  const values = { body_r: '12', body_g: '34', body_b: '56', body_a: '0' };
  const node = Composites.control({
    comp: RGBA, byName: byName(RGBA.columns), values: values, ctx: ctx(),
  });
  setBlend(node, 200);
  assert.deepEqual(valuesOf(node, RGBA.columns), ['12', '34', '56', '200']);
});

test('rgba at blend zero keeps the colour rather than blanking it', () => {
  const values = { body_r: '12', body_g: '34', body_b: '56', body_a: '9' };
  const node = Composites.control({
    comp: RGBA, byName: byName(RGBA.columns), values: values, ctx: ctx(),
  });
  setBlend(node, 0);
  assert.deepEqual(valuesOf(node, RGBA.columns), ['12', '34', '56', '0']);
});

test('rgba writes the swatch colour when the colour moves', () => {
  const node = Composites.control({
    comp: RGBA, byName: byName(RGBA.columns), values: {}, ctx: ctx(),
  });
  setColour(node, '#0080ff');
  assert.deepEqual(valuesOf(node, RGBA.columns), ['0', '128', '255', '0']);
});

test('rgba readout names the blend, not opacity', () => {
  // Icon.cs:9-11 — the alpha channel is a blend FACTOR against the sprite, not transparency.
  const node = Composites.control({
    comp: RGBA, byName: byName(RGBA.columns), values: { body_a: '64' }, ctx: ctx(),
  });
  const readout = node.querySelector('[class="readout"]');
  assert.match(readout.textContent, /64/);
  assert.match(readout.textContent, /blend/);
  assert.doesNotMatch(readout.textContent, /opacity|alpha/i);
});

test('rgba readout follows the blend strip', () => {
  const node = Composites.control({
    comp: RGBA, byName: byName(RGBA.columns), values: {}, ctx: ctx(),
  });
  setBlend(node, 31);
  assert.match(node.querySelector('[class="readout"]').textContent, /31/);
});

test('rgba notifies its wrapper hook on change', () => {
  const node = Composites.control({
    comp: RGBA, byName: byName(RGBA.columns), values: {}, ctx: ctx(),
  });
  let calls = 0;
  node.__onChange = () => { calls++; };
  setBlend(node, 12);
  setColour(node, '#010203');
  assert.equal(calls, 2);
});

test('rgba survives no wrapper hook', () => {
  const node = Composites.control({
    comp: RGBA, byName: byName(RGBA.columns), values: {}, ctx: ctx(),
  });
  setBlend(node, 0);
  assert.deepEqual(valuesOf(node, RGBA.columns), ['0', '0', '0', '0']);
});

test('rgba has no native colour input or range slider left', () => {
  // The whole point of the swap: <input type="color"> opens the OS picker, and the range slider
  // that used to carry the blend beside it is now inside the popover.
  const node = Composites.control({
    comp: RGBA, byName: byName(RGBA.columns), values: {}, ctx: ctx(),
  });
  assert.equal(node.querySelector('[type="color"]'), null);
  assert.equal(node.querySelector('[type="range"]'), null);
});

// --- bitmaskControl -------------------------------------------------------------------------

function boxes(node) {
  return node.querySelectorAll('[type="checkbox"]');
}

test('bitmask ticks the restricted classes, not the permitted ones', () => {
  // 31 restricts everything except Priest (class 5) and the Game Master (6) — the shipped
  // Scroll: Healing mask. Bit 0 is set too and belongs to no class, so it gets no box.
  const node = Composites.control({
    comp: BITMASK, byName: byName(BITMASK.columns), values: { class_restrictions: 31 },
    ctx: ctx(),
  });
  const ticked = boxes(node).filter((b) => b.checked).map((b) => b.value);
  assert.deepEqual(ticked, ['1', '2', '3', '4']);
});

test('bitmask labels each class by id and name', () => {
  const node = Composites.control({
    comp: BITMASK, byName: byName(BITMASK.columns), values: {}, ctx: ctx(),
  });
  assert.match(node.textContent, /5 Priest/);
  assert.match(node.textContent, /cannot use/i);
});

test('bitmask writes the mask verbatim until a box is touched', () => {
  const node = Composites.control({
    comp: BITMASK, byName: byName(BITMASK.columns), values: { class_restrictions: 31 },
    ctx: ctx(),
  });
  assert.equal(named(node, 'class_restrictions').value, '31');
});

test('bitmask leaves a blank cell blank until touched', () => {
  const node = Composites.control({
    comp: BITMASK, byName: byName(BITMASK.columns), values: {}, ctx: ctx(),
  });
  assert.equal(named(node, 'class_restrictions').value, '');
});

test('bitmask sets the bit when a class is ticked', () => {
  const node = Composites.control({
    comp: BITMASK, byName: byName(BITMASK.columns), values: { class_restrictions: '0' },
    ctx: ctx(),
  });
  const priest = boxes(node).find((b) => b.value === '5');
  priest.checked = true;
  fire(priest, 'change');
  assert.equal(named(node, 'class_restrictions').value, '32');
});

test('bitmask clears the bit when a class is unticked', () => {
  const node = Composites.control({
    comp: BITMASK, byName: byName(BITMASK.columns), values: { class_restrictions: '31' },
    ctx: ctx(),
  });
  const magus = boxes(node).find((b) => b.value === '4');
  magus.checked = false;
  fire(magus, 'change');
  // 31 - 16 = 15: Magus and Priest may now use it. That is the shipped Root/Gate mask.
  assert.equal(named(node, 'class_restrictions').value, '15');
});

test('bitmask PRESERVES a set bit that belongs to no class', () => {
  // Bit 0 is set by 9 of the 13 shipped item/spell masks (426 rows) and there is no class 0.
  // Rebuilding the mask from the checkboxes alone would drop it and rewrite all of them.
  const node = Composites.control({
    comp: BITMASK, byName: byName(BITMASK.columns), values: { class_restrictions: '31' },
    ctx: ctx(),
  });
  const priest = boxes(node).find((b) => b.value === '5');
  priest.checked = true;
  fire(priest, 'change');
  assert.equal(named(node, 'class_restrictions').value, '63');
});

test('bitmask preserves a CLEAR foreign bit just as carefully', () => {
  // The other four shipped masks — 22, 34, 38, 50 — leave bit 0 clear. Setting it would be the
  // same rewrite in the other direction, and no test using only bit-0-set masks would see it.
  // 22 is Small Hammer: Warrior + Priest may use it.
  const node = Composites.control({
    comp: BITMASK, byName: byName(BITMASK.columns), values: { class_restrictions: '22' },
    ctx: ctx(),
  });
  assert.deepEqual(boxes(node).filter((b) => b.checked).map((b) => b.value), ['1', '2', '4']);
  const priest = boxes(node).find((b) => b.value === '5');
  priest.checked = true;
  fire(priest, 'change');
  assert.equal(named(node, 'class_restrictions').value, '54');
});

test('bitmask preserves a foreign bit ABOVE the class range', () => {
  // 253 = 0b11111101, the shipped Quests value: bit 0 AND bit 7 set, bit 1 clear. Bit 7 is the
  // only high foreign bit in the data, and every other control test here uses bit 0 alone — so
  // without this case the whole `foreign` filter can be replaced by "keep bit 0" unnoticed.
  // It is what must hold if a 7th class is ever added to the Classes sheet.
  const node = Composites.control({
    comp: BITMASK, byName: byName(BITMASK.columns), values: { class_restrictions: '253' },
    ctx: ctx(),
  });
  assert.deepEqual(boxes(node).filter((b) => b.checked).map((b) => b.value),
    ['2', '3', '4', '5', '6']);
  const rogue = boxes(node).find((b) => b.value === '1');
  rogue.checked = true;
  fire(rogue, 'change');
  // 253 + 2 = 255: every class restricted, with bits 0 and 7 carried through untouched.
  assert.equal(named(node, 'class_restrictions').value, '255');
});

test('bitmask keeps a foreign bit even when every class box is cleared', () => {
  const node = Composites.control({
    comp: BITMASK, byName: byName(BITMASK.columns), values: { class_restrictions: '31' },
    ctx: ctx(),
  });
  boxes(node).forEach((b) => { b.checked = false; });
  fire(boxes(node)[0], 'change');
  assert.equal(named(node, 'class_restrictions').value, '1');
});

test('bitmask writes 0, not blank, when everything is unticked', () => {
  // Blank means "use the SQL default". class_restrictions defaults to 0, so blank would be
  // right by luck here — but "no restrictions" is a decision the user just made and it should
  // be stored as one.
  const node = Composites.control({
    comp: BITMASK, byName: byName(BITMASK.columns), values: { class_restrictions: '6' },
    ctx: ctx(),
  });
  boxes(node).forEach((b) => { b.checked = false; });
  const first = boxes(node)[0];
  fire(first, 'change');
  assert.equal(named(node, 'class_restrictions').value, '0');
});

test('bitmask falls back to a raw mask field when the source sheet has not loaded', () => {
  // Checkboxes built from an empty list would be an empty, uneditable control forever.
  const node = Composites.control({
    comp: BITMASK, byName: byName(BITMASK.columns), values: { class_restrictions: '223' },
    ctx: ctx({ pickerData: {} }),
  });
  assert.equal(boxes(node).length, 0);
  const raw = named(node, 'class_restrictions');
  assert.equal(raw.value, '223');
  assert.equal(raw.getAttribute('type'), 'text');
  assert.match(node.textContent, /Classes/);
});

test('bitmask survives a ctx with no pickerData at all', () => {
  const node = Composites.control({
    comp: BITMASK, byName: byName(BITMASK.columns), values: {}, ctx: {},
  });
  assert.equal(named(node, 'class_restrictions').value, '');
});

test('bitmask notifies its wrapper hook', () => {
  const node = Composites.control({
    comp: BITMASK, byName: byName(BITMASK.columns), values: {}, ctx: ctx(),
  });
  let calls = 0;
  node.__onChange = () => { calls++; };
  fire(boxes(node)[0], 'change');
  assert.equal(calls, 1);
});

// --- idListControl --------------------------------------------------------------------------

function chips(node) {
  return node.querySelectorAll('[class="chip"]');
}

test('idList writes its cell verbatim until touched', () => {
  const node = Composites.control({
    comp: IDLIST, byName: byName(IDLIST.columns), values: { quest_ids: '10,11' }, ctx: ctx(),
  });
  assert.equal(named(node, 'quest_ids').value, '10,11');
});

test('idList shows one chip per id, resolved against the source sheet', () => {
  const node = Composites.control({
    comp: IDLIST, byName: byName(IDLIST.columns), values: { quest_ids: '10 11' }, ctx: ctx(),
  });
  const text = chips(node).map((c) => c.textContent);
  assert.equal(text.length, 2);
  assert.match(text[0], /10/);
  assert.match(text[0], /Rat Hunt/);
  assert.match(text[1], /not found/);
});

test('idList adds an id and rewrites the cell space-separated', () => {
  const node = Composites.control({
    comp: IDLIST, byName: byName(IDLIST.columns), values: { quest_ids: '10,11' }, ctx: ctx(),
  });
  const add = node.querySelector('[class="add"]');
  add.value = '12';
  fire(add, 'change');
  assert.equal(named(node, 'quest_ids').value, '10 11 12');
  assert.equal(chips(node).length, 3);
  assert.equal(add.value, '', 'the add field clears itself');
});

test('idList adds through the button too', () => {
  const node = Composites.control({
    comp: IDLIST, byName: byName(IDLIST.columns), values: {}, ctx: ctx(),
  });
  node.querySelector('[class="add"]').value = '10';
  fire(node.querySelector('[class="add-button"]'), 'click');
  assert.equal(named(node, 'quest_ids').value, '10');
});

test('idList dedupes by NUMERIC id, not by text', () => {
  // '1' and '01' are the same quest. Two chips for it would write it into the list twice.
  const node = Composites.control({
    comp: IDLIST, byName: byName(IDLIST.columns), values: { quest_ids: '1' }, ctx: ctx(),
  });
  const add = node.querySelector('[class="add"]');
  add.value = '01';
  fire(add, 'change');
  assert.equal(chips(node).length, 1);
  assert.equal(named(node, 'quest_ids').value, '1');
});

test('idList ignores a blank or non-numeric addition', () => {
  const node = Composites.control({
    comp: IDLIST, byName: byName(IDLIST.columns), values: {}, ctx: ctx(),
  });
  const add = node.querySelector('[class="add"]');
  ['', '   ', 'abc', '-1', '1.5'].forEach((text) => {
    add.value = text;
    fire(add, 'change');
  });
  assert.equal(chips(node).length, 0);
  assert.equal(named(node, 'quest_ids').value, '');
});

test('idList leaves a rejected entry in the field to be fixed', () => {
  // Clearing it would make the id vanish with no explanation at all.
  const node = Composites.control({
    comp: IDLIST, byName: byName(IDLIST.columns), values: {}, ctx: ctx(),
  });
  const add = node.querySelector('[class="add"]');
  add.value = 'abc';
  fire(add, 'change');
  assert.equal(add.value, 'abc');
});

test('idList clears the field after swallowing a duplicate', () => {
  const node = Composites.control({
    comp: IDLIST, byName: byName(IDLIST.columns), values: { quest_ids: '10' }, ctx: ctx(),
  });
  const add = node.querySelector('[class="add"]');
  add.value = '10';
  fire(add, 'change');
  assert.equal(add.value, '');
});

test('idList removes the chip that was clicked, not the one at a stale index', () => {
  // Each render closes over an index; without a rebuild the second removal takes the wrong id.
  const node = Composites.control({
    comp: IDLIST, byName: byName(IDLIST.columns), values: { quest_ids: '10 11 12 13' },
    ctx: ctx(),
  });
  fire(chips(node)[1].querySelector('[class="remove"]'), 'click');
  assert.equal(named(node, 'quest_ids').value, '10 12 13');
  fire(chips(node)[1].querySelector('[class="remove"]'), 'click');
  assert.equal(named(node, 'quest_ids').value, '10 13');
  fire(chips(node)[0].querySelector('[class="remove"]'), 'click');
  assert.equal(named(node, 'quest_ids').value, '13');
});

test('idList writes blank when the last id is removed', () => {
  const node = Composites.control({
    comp: IDLIST, byName: byName(IDLIST.columns), values: { quest_ids: '10' }, ctx: ctx(),
  });
  fire(chips(node)[0].querySelector('[class="remove"]'), 'click');
  assert.equal(named(node, 'quest_ids').value, '');
});

test('idList reads its source sheet at use time, not at build time', () => {
  // App.loadReferencedSheets fills pickerData over google.script.run; a control built first
  // must still resolve names once the sheet lands.
  const c = ctx({ pickerData: {} });
  const node = Composites.control({
    comp: IDLIST, byName: byName(IDLIST.columns), values: { quest_ids: '10' }, ctx: c,
  });
  // Not "not found": an empty list is a sheet that has not arrived, not a bad id.
  assert.match(chips(node)[0].textContent, /…/);
  c.pickerData.Quests = [{ id: '10', name: 'Rat Hunt' }];
  const add = node.querySelector('[class="add"]');
  add.value = '11';
  fire(add, 'change');
  assert.match(chips(node)[0].textContent, /Rat Hunt/);
});

test('idList notifies its wrapper hook', () => {
  const node = Composites.control({
    comp: IDLIST, byName: byName(IDLIST.columns), values: {}, ctx: ctx(),
  });
  let calls = 0;
  node.__onChange = () => { calls++; };
  const add = node.querySelector('[class="add"]');
  add.value = '10';
  fire(add, 'change');
  assert.equal(calls, 1);
});

// --- equipSlotsControl ----------------------------------------------------------------------

const RAW_EQUIP = '1,*,2,*,3,*,4,*,5,*,6,*';

function slotInputs(node) {
  return node.querySelectorAll('[class="slot-graphic"]');
}

test('equip slots writes the stored string VERBATIM until touched', () => {
  // Five rows in the shipped data are malformed; opening one and saving must not rewrite it.
  const odd = '1,*,2,*,3,*,4,*,5,*,6,*,999';
  const node = Composites.control({
    comp: EQUIP, byName: byName(EQUIP.columns), values: { equipped_items: odd }, ctx: ctx(),
  });
  assert.equal(named(node, 'equipped_items').value, odd);
});

test('equip slots canonicalises a BLANK cell immediately', () => {
  // Goose/Packets.cs:161 splices the value + ',', so a blank cell desynchronises the packet.
  // Repairing it is the one rewrite that is always right.
  const node = Composites.control({
    comp: EQUIP, byName: byName(EQUIP.columns), values: { equipped_items: '' }, ctx: ctx(),
  });
  assert.equal(named(node, 'equipped_items').value, '0,*,0,*,0,*,0,*,0,*,0,*');
});

test('equip slots shows one labelled field per slot, in Equipped.SLOTS order', () => {
  const node = Composites.control({
    comp: EQUIP, byName: byName(EQUIP.columns), values: { equipped_items: RAW_EQUIP }, ctx: ctx(),
  });
  assert.deepEqual(slotInputs(node).map((i) => i.value), ['1', '2', '3', '4', '5', '6']);
  Equipped.SLOTS.forEach((name) => assert.match(node.textContent, new RegExp(name)));
});

test('equip slots rewrites the cell when a graphic changes', () => {
  const node = Composites.control({
    comp: EQUIP, byName: byName(EQUIP.columns), values: { equipped_items: RAW_EQUIP }, ctx: ctx(),
  });
  const chest = slotInputs(node)[0];
  chest.value = '77';
  fire(chest, 'input');
  assert.equal(named(node, 'equipped_items').value, '77,*,2,*,3,*,4,*,5,*,6,*');
});

test('equip slots treats a cleared field as slot 0', () => {
  const node = Composites.control({
    comp: EQUIP, byName: byName(EQUIP.columns), values: { equipped_items: RAW_EQUIP }, ctx: ctx(),
  });
  const helm = slotInputs(node)[1];
  helm.value = '';
  fire(helm, 'input');
  assert.equal(named(node, 'equipped_items').value, '1,*,0,*,3,*,4,*,5,*,6,*');
});

test('equip slots REFUSES to write a typo rather than coercing it to 0', () => {
  // Equipped.format coerces silently and isFaithful only inspects stored strings, so nothing
  // else in the stack catches a typo. Task 9 solved the equivalent problem with a status line.
  const node = Composites.control({
    comp: EQUIP, byName: byName(EQUIP.columns), values: { equipped_items: RAW_EQUIP }, ctx: ctx(),
  });
  const chest = slotInputs(node)[0];
  chest.value = 'abc';
  fire(chest, 'input');
  assert.equal(named(node, 'equipped_items').value, RAW_EQUIP, 'the cell must not change');
  const status = node.querySelectorAll('[class="status bad"]');
  assert.equal(status.length, 1);
  assert.match(status[0].textContent, /whole number/);
});

test('equip slots recovers once the typo is corrected', () => {
  const node = Composites.control({
    comp: EQUIP, byName: byName(EQUIP.columns), values: { equipped_items: RAW_EQUIP }, ctx: ctx(),
  });
  const chest = slotInputs(node)[0];
  chest.value = 'abc';
  fire(chest, 'input');
  chest.value = '9';
  fire(chest, 'input');
  assert.equal(named(node, 'equipped_items').value, '9,*,2,*,3,*,4,*,5,*,6,*');
  assert.equal(node.querySelectorAll('[class="status bad"]').length, 0);
});

test('equip slots raises __frozen the moment a typo appears, not on the next good edit', () => {
  // The contract Task 11's save path blocks on. Raising it late would leave a window in which
  // Save looks fine and silently writes a stale cell.
  const node = Composites.control({
    comp: EQUIP, byName: byName(EQUIP.columns), values: { equipped_items: RAW_EQUIP }, ctx: ctx(),
  });
  assert.equal(node.__frozen, false);
  const [chest, helm] = slotInputs(node);
  chest.value = 'abc';
  fire(chest, 'input');
  assert.equal(node.__frozen, true);

  // Editing another slot while frozen goes nowhere — this is exactly the loss Save must refuse.
  helm.value = '88';
  fire(helm, 'input');
  assert.equal(node.__frozen, true);
  assert.equal(named(node, 'equipped_items').value, RAW_EQUIP);

  chest.value = '77';
  fire(chest, 'input');
  assert.equal(node.__frozen, false);
  assert.equal(named(node, 'equipped_items').value, '77,*,88,*,3,*,4,*,5,*,6,*');
});

test('equip slots blocks the write while ANY slot is bad', () => {
  const node = Composites.control({
    comp: EQUIP, byName: byName(EQUIP.columns), values: { equipped_items: RAW_EQUIP }, ctx: ctx(),
  });
  const [chest, helm] = slotInputs(node);
  chest.value = 'x';
  fire(chest, 'input');
  helm.value = '8';
  fire(helm, 'input');
  assert.equal(named(node, 'equipped_items').value, RAW_EQUIP);
  chest.value = '7';
  fire(chest, 'input');
  assert.equal(named(node, 'equipped_items').value, '7,*,8,*,3,*,4,*,5,*,6,*');
});

test('equip slots preserves a stored tint through an unrelated edit', () => {
  const raw = '1,255,0,0,128,2,*,3,*,4,*,5,*,6,*';
  const node = Composites.control({
    comp: EQUIP, byName: byName(EQUIP.columns), values: { equipped_items: raw }, ctx: ctx(),
  });
  const helm = slotInputs(node)[1];
  helm.value = '9';
  fire(helm, 'input');
  assert.equal(named(node, 'equipped_items').value, '1,255,0,0,128,9,*,3,*,4,*,5,*,6,*');
});

test('equip slots draws each slot from the category Appearance already maps', () => {
  // Shield and Weapon are both 'Hands' (CharacterLayout.cs); the mapping is not duplicated
  // here, it is read from Appearance.CATEGORY.
  const drawn = [];
  const bundles = {
    parts: {
      rects: {
        'Chest:1:idle-no-equip-down': [0, 0, 10, 10],
        'Helms:2:idle-no-equip-down': [0, 0, 10, 10],
        'Legs:3:idle-no-equip-down': [0, 0, 10, 10],
        'Feet:4:idle-no-equip-down': [0, 0, 10, 10],
        'Hands:5:idle-no-equip-down': [0, 0, 10, 10],
        'Hands:6:idle-no-equip-down': [0, 0, 10, 10],
      },
    },
  };
  const stubbed = Sprites.part;
  Sprites.part = (b, category, id, equipped) => {
    drawn.push([category, String(id), equipped]);
    return stubbed(b, category, id, equipped);
  };
  try {
    Composites.control({
      comp: EQUIP, byName: byName(EQUIP.columns),
      values: { equipped_items: RAW_EQUIP, body_state: '3' }, ctx: ctx({ bundles }),
    });
  } finally {
    Sprites.part = stubbed;
  }
  assert.deepEqual(drawn.map((d) => d[0]),
    ['Chest', 'Helms', 'Legs', 'Feet', 'Hands', 'Hands']);
  assert.deepEqual(drawn.map((d) => d[1]), ['1', '2', '3', '4', '5', '6']);
});

test('equip slots derives the equipped pose from body_state, not a hardcoded true', () => {
  // Sprites.clipCandidates prefers idle-equip-down when equipped. body_state 3 is unarmed.
  const seen = [];
  const stubbed = Sprites.part;
  Sprites.part = (b, category, id, equipped) => { seen.push(equipped); return null; };
  try {
    Composites.control({
      comp: EQUIP, byName: byName(EQUIP.columns),
      values: { equipped_items: RAW_EQUIP, body_state: '3' }, ctx: ctx(),
    });
    Composites.control({
      comp: EQUIP, byName: byName(EQUIP.columns),
      values: { equipped_items: RAW_EQUIP, body_state: '1' }, ctx: ctx(),
    });
  } finally {
    Sprites.part = stubbed;
  }
  assert.deepEqual(seen.slice(0, 6), [false, false, false, false, false, false]);
  assert.deepEqual(seen.slice(6), [true, true, true, true, true, true]);
});

test('equip slots redraws when the bundle images arrive', () => {
  const ready = [];
  const bundles = { parts: { rects: { 'Chest:1:idle-no-equip-down': [0, 0, 10, 10] } } };
  const c = ctx({ bundles, images: { parts: null }, onImagesReady: (fn) => ready.push(fn) });
  const node = Composites.control({
    comp: EQUIP, byName: byName(EQUIP.columns), values: { equipped_items: RAW_EQUIP }, ctx: c,
  });
  assert.equal(ready.length, 6, 'one redraw per slot');
  const canvas = node.querySelectorAll('[class="preview"]')[0];
  const drawn = () => canvas.getContext('2d').calls.filter((k) => k[0] === 'drawImage').length;
  assert.equal(drawn(), 0, 'nothing drawn while the PNG is undecoded');
  c.images.parts = { fake: true };
  ready.forEach((fn) => fn());
  assert.equal(drawn(), 1);
});

test('equip slots hands the slot TINT to the preview', () => {
  // Sprites.draw takes the tinted path only when a blend factor is present, and that path is
  // the offscreen canvas. Passing null instead would preview the wrong colour entirely.
  const bundles = { parts: { rects: { 'Chest:1:idle-no-equip-down': [0, 0, 2, 2] } } };
  const node = Composites.control({
    comp: EQUIP, byName: byName(EQUIP.columns),
    values: { equipped_items: '1,255,0,0,128,2,*,3,*,4,*,5,*,6,*' },
    ctx: ctx({ bundles, images: { parts: { fake: true } } }),
  });
  // The tinted path composites offscreen and blits the result: drawImage(off, dx, dy), three
  // arguments. The untinted path blits the bundle directly, with all nine.
  const drawn = node.querySelectorAll('[class="preview"]')[0].getContext('2d').calls
    .filter((c) => c[0] === 'drawImage');
  assert.equal(drawn.length, 1);
  assert.equal(drawn[0].length, 4, 'expected the offscreen (tinted) blit');
});

test('equip slots survives a ctx with no onImagesReady', () => {
  const node = Composites.control({
    comp: EQUIP, byName: byName(EQUIP.columns), values: { equipped_items: RAW_EQUIP }, ctx: {},
  });
  assert.equal(named(node, 'equipped_items').value, RAW_EQUIP);
});

test('equip slots notifies its wrapper hook', () => {
  const node = Composites.control({
    comp: EQUIP, byName: byName(EQUIP.columns), values: { equipped_items: RAW_EQUIP }, ctx: ctx(),
  });
  let calls = 0;
  node.__onChange = () => { calls++; };
  const chest = slotInputs(node)[0];
  chest.value = '7';
  fire(chest, 'input');
  assert.equal(calls, 1);
});

// --- equip slot COLOURS ---------------------------------------------------------------------
//
// equipped_items has carried r,g,b,a per slot all along and Equipped.parse has been reading it;
// until now there was no way to change it. Each slot row owns a picker, so these drive it
// through the row rather than through the control — swatchOf and friends take whatever node
// they are given.

const TINTABLE = '12,*,2,*,3,*,4,*,5,*,6,*';

function slotRows(node) {
  return node.querySelectorAll('[class="equip-slot"]');
}

function statusOf(row) {
  return row.querySelector('[class="status"]');
}

test('equip slots gives every slot a blend-capable picker', () => {
  const node = Composites.control({
    comp: EQUIP, byName: byName(EQUIP.columns), values: { equipped_items: TINTABLE }, ctx: ctx(),
  });
  const rows = slotRows(node);
  assert.equal(rows.length, 6);
  rows.forEach((row) => {
    assert.ok(swatchOf(row), 'a swatch per slot');
    assert.ok(row.querySelector('[class="cp-alpha"]'), 'the blend strip, since a is a channel');
  });
});

test('equip slots seeds each picker from the stored slot colour and does NOT write', () => {
  // Six pickers built at construction must not, between them, rewrite the cell — the same rule
  // that keeps a malformed row openable.
  const raw = '1,164,51,31,128,2,*,3,*,4,*,5,*,6,*';
  const node = Composites.control({
    comp: EQUIP, byName: byName(EQUIP.columns), values: { equipped_items: raw }, ctx: ctx(),
  });
  assert.equal(named(node, 'equipped_items').value, raw, 'written back verbatim');
  assert.equal(swatchOf(slotRows(node)[0]).getAttribute('data-color'), '#a4331f');
  assert.equal(swatchOf(slotRows(node)[1]).getAttribute('data-color'), '#000000');
});

test('equip slots writes the five-token form when a slot colour changes', () => {
  const node = Composites.control({
    comp: EQUIP, byName: byName(EQUIP.columns), values: { equipped_items: TINTABLE }, ctx: ctx(),
  });
  const chest = slotRows(node)[0];
  setBlend(chest, 128);
  setColour(chest, '#a4331f');
  assert.equal(named(node, 'equipped_items').value, '12,164,51,31,128,2,*,3,*,4,*,5,*,6,*');
});

test('equip slots leaves the other five slots alone when one is tinted', () => {
  const node = Composites.control({
    comp: EQUIP, byName: byName(EQUIP.columns), values: { equipped_items: TINTABLE }, ctx: ctx(),
  });
  setBlend(slotRows(node)[4], 200);
  setColour(slotRows(node)[4], '#00ff00');
  assert.equal(named(node, 'equipped_items').value, '12,*,2,*,3,*,4,*,5,0,255,0,200,6,*');
});

test('equip slots discards the colour when the blend drops to 0, and says so', () => {
  // Equipped.format collapses a zero-alpha slot to the compact form, so a parked colour behind
  // a zero blend is genuinely gone. The row has to say that while the swatch still shows it.
  const node = Composites.control({
    comp: EQUIP, byName: byName(EQUIP.columns), values: { equipped_items: TINTABLE }, ctx: ctx(),
  });
  const chest = slotRows(node)[0];
  setBlend(chest, 128);
  setColour(chest, '#a4331f');
  assert.equal(statusOf(chest).textContent, '');

  setBlend(chest, 0);
  assert.equal(named(node, 'equipped_items').value, '12,*,2,*,3,*,4,*,5,*,6,*');
  assert.match(statusOf(chest).textContent, /colour not stored while blend is 0/);

  setBlend(chest, 5);
  assert.equal(named(node, 'equipped_items').value, '12,164,51,31,5,2,*,3,*,4,*,5,*,6,*');
  assert.equal(statusOf(chest).textContent, '');
});

test('a graphic typo outranks the zero-blend note on the same row', () => {
  // The typo is the blocking condition; burying it under an informational note would hide the
  // reason the cell has stopped updating.
  const node = Composites.control({
    comp: EQUIP, byName: byName(EQUIP.columns), values: { equipped_items: TINTABLE }, ctx: ctx(),
  });
  const chest = slotRows(node)[0];
  setBlend(chest, 0);
  assert.match(statusOf(chest).textContent, /blend is 0/);

  const input = chest.querySelector('[class="slot-graphic"]');
  input.value = 'abc';
  fire(input, 'input');
  assert.match(chest.querySelector('[class="status bad"]').textContent, /whole number/);

  input.value = '12';
  fire(input, 'input');
  assert.match(statusOf(chest).textContent, /blend is 0/, 'the note comes back');
});

test('equip slots notifies its wrapper hook on a colour change too', () => {
  const node = Composites.control({
    comp: EQUIP, byName: byName(EQUIP.columns), values: { equipped_items: TINTABLE }, ctx: ctx(),
  });
  let calls = 0;
  node.__onChange = () => { calls++; };
  setBlend(slotRows(node)[0], 90);
  assert.equal(calls, 1);
});

test('equip slots repaints the slot preview with the new tint', () => {
  // Sprites.draw takes the offscreen (three-argument drawImage) path only when a blend factor
  // is present, so the argument count is the proof the tint reached it.
  const bundles = { parts: { rects: { 'Chest:12:idle-no-equip-down': [0, 0, 2, 2] } } };
  const node = Composites.control({
    comp: EQUIP, byName: byName(EQUIP.columns), values: { equipped_items: TINTABLE },
    ctx: ctx({ bundles, images: { parts: { fake: true } } }),
  });
  const chest = slotRows(node)[0];
  const calls = () => chest.querySelector('[class="preview"]').getContext('2d').calls
    .filter((c) => c[0] === 'drawImage');

  assert.equal(calls().length, 1);
  assert.equal(calls()[0].length, 10, 'untinted: the bundle is blitted directly');

  setBlend(chest, 128);
  const after = calls();
  assert.equal(after.length, 2, 'the colour change redrew the preview');
  assert.equal(after[1].length, 4, 'expected the offscreen (tinted) blit');
});

test('equip slots anchors the slot pickers to the right of their track', () => {
  // The swatch sits in a 28px track mid-row; a left-anchored 182px popover would hang off the
  // edge of the sidebar.
  const node = Composites.control({
    comp: EQUIP, byName: byName(EQUIP.columns), values: { equipped_items: TINTABLE }, ctx: ctx(),
  });
  slotRows(node).forEach((row) => {
    assert.equal(row.querySelector('[class="colorpicker"]').getAttribute('data-align'), 'right');
  });
});

// --- control dispatch -----------------------------------------------------------------------

test('control routes Graphic to the picker, in the schema column order', () => {
  // columns is [graphic, file] but Sprites.icon takes (bundles, SHEET, graphic). Swapping the
  // two arguments resolves nothing at all and does it silently, so the preview is the assertion:
  // sheet 2 graphic 5 has art, and 5:2 does not.
  const comp = { kind: 'Graphic', columns: ['graphic_tile', 'graphic_file'] };
  const bundles = { icons: { rects: { '2:5': [0, 0, 8, 8] } } };
  const node = Composites.control({
    comp: comp, byName: byName(comp.columns), values: { graphic_tile: '5', graphic_file: '2' },
    ctx: ctx({ bundles, images: { icons: { fake: true } } }),
  });

  assert.equal(node.getAttribute('class'), 'graphic');
  assert.equal(named(node, 'graphic_tile').value, '5');
  assert.equal(named(node, 'graphic_file').value, '2');
  const calls = node.querySelector('[class="preview"]').getContext('2d').calls;
  assert.equal(calls.filter((c) => c[0] === 'drawImage').length, 1);
  assert.equal(node.querySelector('[class="status"]').textContent, '');
});

test('an unknown composite kind still renders every one of its columns', () => {
  // The default branch must not make columns uneditable — a control with no [name] would drop
  // them from Forms.collect and blank them on the next save.
  const comp = { kind: 'Nonesuch', columns: ['body_r', 'body_g'] };
  const node = Composites.control({
    comp: comp, byName: byName(comp.columns), values: { body_r: '1', body_g: '2' }, ctx: ctx(),
  });
  assert.match(node.textContent, /Nonesuch/);
  assert.deepEqual(valuesOf(node, comp.columns), ['1', '2']);
});

test('every composite kind in the schema is handled', () => {
  // The default branch is a safety net, not a shipping path.
  const kinds = ['Graphic', 'Rgba', 'Bitmask', 'IdList', 'EquipSlots'];
  assert.deepEqual(Composites.KINDS, kinds);
});

test('a composite renders an error slot for each NON-leader column', () => {
  // Forms.render appends exactly one slot, keyed on the leader, so body_g/b/a would have
  // nowhere to report. The slots here come FIRST in document order, and Forms.showErrors
  // keeps the first slot it finds per column.
  const node = Composites.control({
    comp: RGBA, byName: byName(RGBA.columns), values: {}, ctx: ctx(),
  });
  const slots = node.querySelectorAll('[data-error-for]');
  assert.deepEqual(slots.map((s) => s.getAttribute('data-error-for')),
    ['body_g', 'body_b', 'body_a']);
});

test('a single-column composite renders no extra error slot', () => {
  const node = Composites.control({
    comp: BITMASK, byName: byName(BITMASK.columns), values: {}, ctx: ctx(),
  });
  assert.equal(node.querySelectorAll('[data-error-for]').length, 0);
});

test('a composite naming a column the schema does not have gets no slot for it', () => {
  const comp = { kind: 'Rgba', columns: ['body_r', 'body_g', 'body_b', 'body_a'] };
  const map = byName(['body_r', 'body_g']);
  const node = Composites.control({ comp: comp, byName: map, values: {}, ctx: ctx() });
  assert.deepEqual(node.querySelectorAll('[data-error-for]').map((s) =>
    s.getAttribute('data-error-for')), ['body_g']);
});

test('collect returns nothing — every composite writes named cells', () => {
  const node = Composites.control({
    comp: RGBA, byName: byName(RGBA.columns), values: {}, ctx: ctx(),
  });
  assert.deepEqual(Composites.collect(RGBA, node), {});
});

// --- Forms integration ----------------------------------------------------------------------

test('Forms.collect gathers every composite cell by name', () => {
  const schema = {
    sheet: 'NPCs',
    columns: RGBA.columns.map((n) => ({ name: n, kind: 'Int', default: '0' })),
    composites: [RGBA],
  };
  const container = document.createElement('div');
  Forms.render(container, schema, { body_r: '1', body_g: '2', body_b: '3', body_a: '4' },
    ctx());
  assert.deepEqual(Forms.collect(container, schema),
    { body_r: '1', body_g: '2', body_b: '3', body_a: '4' });
});

test('Forms.render routes an FK column to the picker', () => {
  // Logged task #19: fkControl had no call site, so all 26 FK columns rendered as plain text.
  const schema = {
    sheet: 'NPCs',
    columns: [{ name: 'quest_id', kind: 'Id', ref: 'Quests', required: false }],
    composites: [],
  };
  const container = document.createElement('div');
  Forms.render(container, schema, { quest_id: '10' }, ctx());
  assert.ok(container.querySelector('[class="picker"]'), 'expected a picker wrapper');
  assert.equal(container.querySelector('[name="quest_id"]').value, '10');
  assert.equal(container.querySelector('[class="resolved"]').textContent, 'Rat Hunt');
});

test('Forms.render leaves a non-FK column on the scalar control', () => {
  const schema = {
    sheet: 'NPCs',
    columns: [{ name: 'npc_name', kind: 'Text', default: "''" }],
    composites: [],
  };
  const container = document.createElement('div');
  Forms.render(container, schema, { npc_name: 'Rat' }, ctx());
  assert.equal(container.querySelector('[class="picker"]'), null);
  assert.equal(container.querySelector('[name="npc_name"]').value, 'Rat');
});

test('Forms.render falls back to the scalar control when Pickers is absent', () => {
  const schema = {
    sheet: 'NPCs',
    columns: [{ name: 'quest_id', kind: 'Id', ref: 'Quests', required: false }],
    composites: [],
  };
  const previous = globalThis.Pickers;
  delete globalThis.Pickers;
  try {
    const container = document.createElement('div');
    Forms.render(container, schema, { quest_id: '10' }, ctx());
    assert.equal(container.querySelector('[class="picker"]'), null);
    assert.equal(container.querySelector('[name="quest_id"]').value, '10');
  } finally {
    globalThis.Pickers = previous;
  }
});

test('the whole NPCs row round-trips through render and collect unchanged', () => {
  // The end-to-end claim: opening a record and saving it without touching anything must
  // return exactly what came in.
  const columns = [
    { name: 'npc_id', kind: 'Id', required: true, pk: true },
    { name: 'body_state', kind: 'Int', default: '1' },
    ...RGBA.columns.map((n) => ({ name: n, kind: 'Int', default: '0' })),
    { name: 'equipped_items', kind: 'Text', default: "'0,*,0,*,0,*,0,*,0,*,0,*'" },
    { name: 'quest_ids', kind: 'Text', default: "''" },
    { name: 'class_restrictions', kind: 'Int', default: '0' },
  ];
  const schema = {
    sheet: 'NPCs',
    columns,
    composites: [RGBA, EQUIP, IDLIST, { ...BITMASK }],
  };
  const values = {
    npc_id: '4', body_state: '1',
    body_r: '10', body_g: '20', body_b: '30', body_a: '0',
    equipped_items: '1,*,2,*,3,*,4,*,5,*,6,*,999',
    quest_ids: '10,11', class_restrictions: '31',
  };
  const container = document.createElement('div');
  Forms.render(container, schema, values, ctx());
  assert.deepEqual(Forms.collect(container, schema), values);
  assert.ok(walk(container).length > 0);
});

test('rgba names the four cells it writes', () => {
  // The field's <label> now says "body tint", so the column names have to live in the control
  // or a designer cannot find the cells in the sheet. This is the only place they appear: the
  // blend slider that used to carry body_a's name went into the picker's popover.
  const node = Composites.control({
    comp: RGBA, byName: byName(RGBA.columns), values: {}, ctx: ctx(),
  });
  assert.equal(node.querySelector('[class="hint"]').textContent, 'body_r body_g body_b body_a');
});

test('the equip slot preview is scaled, with its centring still in logical pixels', () => {
  // 11x13 in a 40x56 box: (40-11)/2 = 14.5 and (56-13)/2 = 21.5 before flooring, so a preview
  // that centred on the 80x112 BACKING STORE instead would put the sprite somewhere else.
  const bundles = { parts: { rects: { 'Chest:1:idle-no-equip-down': [0, 0, 11, 13] } } };
  const node = Composites.control({
    comp: EQUIP, byName: byName(EQUIP.columns), values: { equipped_items: RAW_EQUIP },
    ctx: ctx({ bundles, images: { parts: 'PARTS' } }),
  });
  const canvas = node.querySelectorAll('[class="preview"]')[0];

  assert.equal(canvas.width, 80);
  assert.equal(canvas.height, 112);
  assert.deepEqual(canvas.getContext('2d').calls, [
    ['setTransform', 2, 0, 0, 2, 0, 0],
    ['imageSmoothingEnabled', false],
    ['clearRect', 0, 0, 40, 56],
    ['drawImage', 'PARTS', 0, 0, 11, 13, 14, 21, 11, 13],
  ]);
});
