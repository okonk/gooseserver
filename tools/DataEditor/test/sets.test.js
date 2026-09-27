import { test } from 'node:test';
import assert from 'node:assert/strict';

const { Sets } = await import('../src/sets.js');

const CLASSES = { 2: 'Rogue', 3: 'Warrior', 4: 'Magus', 5: 'Priest', 6: 'Bard' };

function row(id, slot, name, graphic, extra) {
  return Object.assign({ id, slot, name, graphic }, extra);
}

test('consecutive items sharing a name prefix form one named set', () => {
  const sets = Sets.detect([
    row(32, 'Helmet', 'Warrior Helmet', 20, { classes: 8, minLevel: 18 }),
    row(33, 'Chest', 'Warrior Chestplate', 7, { classes: 8, minLevel: 18 }),
    row(34, 'Pants', 'Warrior Leggings', 6, { classes: 8, minLevel: 18 }),
    row(35, 'Shoes', 'Warrior Boots', 4, { classes: 8, minLevel: 18 }),
  ], CLASSES);

  assert.equal(sets.length, 1);
  assert.equal(sets[0].name, 'Warrior');
  assert.deepEqual(sets[0].classNames, ['Warrior']);
  assert.deepEqual(Object.keys(sets[0].slots).sort(), ['Chest', 'Feet', 'Helm', 'Legs']);
});

test('a class change splits adjacent sets', () => {
  const sets = Sets.detect([
    row(32, 'Helmet', 'Warrior Helmet', 20, { classes: 8, minLevel: 18 }),
    row(33, 'Chest', 'Warrior Chestplate', 7, { classes: 8, minLevel: 18 }),
    row(36, 'Helmet', 'Rogue Helmet', 20, { classes: 4, minLevel: 18, r: 40, a: 160 }),
    row(37, 'Chest', 'Rogue Chestplate', 7, { classes: 4, minLevel: 18, r: 40, a: 160 }),
  ], CLASSES);

  assert.deepEqual(sets.map((s) => s.name), ['Warrior', 'Rogue']);
  assert.deepEqual(sets[0].variants, [sets[1].id]);
});

test('a repeated slot starts a new set', () => {
  const sets = Sets.detect([
    row(1, 'Helmet', 'Oculus Warrior Helm Graphic', 174),
    row(2, 'Chest', 'Oculus Warrior CP Graphic', 88),
    row(3, 'Helmet', 'Oculus Rogue Helm Graphic', 175),
    row(4, 'Chest', 'Oculus Rogue CP Graphic', 89),
  ], CLASSES);

  assert.equal(sets.length, 2);
  assert.ok(sets.every((s) => s.placeholder));
});

test('a shared tint links items with unrelated names', () => {
  const tint = { r: 153, g: 102, b: 0, a: 190, minLevel: 48 };
  const sets = Sets.detect([
    row(522, 'Helmet', 'Busted Shades', 4, tint),
    row(523, 'Shoes', 'Cracked Boots', 3, tint),
    row(524, 'Chest', 'Torn Shirt', 4, tint),
  ], CLASSES);

  assert.equal(sets.length, 1);
  assert.equal(sets[0].items.length, 3);
});

test('an "of the X" suffix names the set', () => {
  const sets = Sets.detect([
    row(12, 'OneHanded', 'Blunt Sword of the Cow Slayer', 9),
    row(13, 'Shield', 'Shield of the Cow Slayer', 21),
    row(15, 'Helmet', 'Helmet of the Cow Slayer', 12),
  ], CLASSES);

  assert.equal(sets[0].name, 'Cow Slayer');
});

test('identical graphics and tints collapse into the first set as a duplicate', () => {
  const sets = Sets.detect([
    row(279, 'Helmet', 'Helmet of Vitality', 55),
    row(280, 'Chest', 'Chestplate of Vitality', 36),
    row(473, 'Helmet', 'Dragon Helmet', 55),
    row(474, 'Chest', 'Dragon Chestplate', 36),
  ], CLASSES);

  assert.equal(sets.length, 1);
  assert.deepEqual(sets[0].duplicates, [{ name: 'Dragon', itemIds: [473, 474] }]);
});

test('unworn slots and items without a graphic are ignored', () => {
  assert.equal(Sets.normalizeItem(row(1, 'Ring', 'Ring', 5)), null);
  assert.equal(Sets.normalizeItem(row(1, 'Helmet', 'Invisible Hat', '')), null);
  assert.equal(Sets.normalizeItem(row(1, 'TwoHanded', 'Staff', 6)).slot, 'Weapon');
});

test('a zero blend factor drops the parked colour', () => {
  const item = Sets.normalizeItem(row(1, 'Helmet', 'Hat', 5, { r: 90, g: 5, b: 60, a: 0 }));
  assert.deepEqual([item.r, item.g, item.b, item.a], [0, 0, 0, 0]);
});

test('a far-away item carrying the set name replaces an off-name neighbour', () => {
  const t = { classes: 48, minLevel: 50, r: 82, g: 138, b: 156, a: 190 };
  const sets = Sets.detect([
    row(162, 'Chest', 'Whirling Robes', 118, t),
    row(247, 'Pants', 'Whirling Leggings', 1, t),
    row(248, 'Shoes', 'Whirling Slippers', 2, t),
    row(249, 'Helmet', 'Whirling Hat', 22, t),
    row(250, 'Chest', 'Nagan Robes', 118, { classes: 48, minLevel: 50, g: 128, a: 100 }),
  ], CLASSES);

  assert.equal(sets.length, 1);
  assert.deepEqual(sets[0].items.map((i) => i.id), [162, 247, 248, 249]);
});

test('a class-named set is not completed with far-away weapons', () => {
  const t = { classes: 4, minLevel: 18, r: 40, a: 160 };
  const sets = Sets.detect([
    row(36, 'Helmet', 'Rogue Helmet', 20, t),
    row(37, 'Chest', 'Rogue Chestplate', 7, t),
    row(1049, 'OneHanded', 'Rogue 20B Concept Sword', 209, { classes: 4, minLevel: 50 }),
  ], CLASSES);

  assert.deepEqual(sets[0].items.map((i) => i.id), [36, 37]);
});

test('pieces spread across the sheet by name form one set', () => {
  const t = { classes: 8, minLevel: 50 };
  const sets = Sets.detect([
    row(90, 'Helmet', 'Champions Helmet', 20, t),
    row(91, 'Helmet', 'Cloth Cap', 22),
    row(107, 'Pants', 'Champions Legplates', 6, t),
    row(149, 'Chest', 'Champions Chestplate', 11, t),
    row(164, 'Shoes', 'Champions Boots', 4, t),
  ], CLASSES);

  assert.equal(sets.length, 1);
  assert.equal(sets[0].name, 'Champions');
  assert.deepEqual(sets[0].items.map((i) => i.id), [90, 107, 149, 164]);
});

test('a class listed one slot at a time chains across the other classes', () => {
  const lvl = { minLevel: 50 };
  const sets = Sets.detect([
    row(401, 'Shoes', 'Ancient Boots', 9, { classes: 4, ...lvl }),
    row(402, 'Shoes', 'Ancient Boots', 4, { classes: 8, ...lvl }),
    row(403, 'Shoes', 'Ancient Slippers', 2, { classes: 16, ...lvl }),
    row(404, 'Shoes', 'Ancient Slippers', 2, { classes: 32, r: 181, g: 131, b: 90, a: 180, ...lvl }),
    row(405, 'Helmet', 'Divine Helm', 20, { classes: 4, ...lvl }),
    row(406, 'Helmet', 'Divine Helm', 20, { classes: 8, ...lvl }),
    row(407, 'Helmet', 'Divine Crown', 22, { classes: 16, ...lvl }),
    row(408, 'Helmet', 'Divine Crown', 22, { classes: 32, r: 28, g: 113, b: 216, a: 180, ...lvl }),
  ], CLASSES);

  assert.deepEqual(sets.map((s) => s.items.map((i) => i.id)), [[401, 405], [402, 406], [403, 407], [404, 408]]);
  assert.equal(sets[0].name, 'Ancient / Divine');
});

test('an unrelated piece a few ids on is not pulled in outside a class block', () => {
  const t = { classes: 8, minLevel: 50 };
  const sets = Sets.detect([
    row(1009, 'Chest', "Vampire Knight's Chestplace", 66, { ...t, a: 110 }),
    row(1010, 'Pants', "Vampire Knight's Legplates", 26, { ...t, a: 110 }),
    row(1012, 'Helmet', 'Slimey Mask', 16, { classes: 68, minLevel: 50 }),
    row(1020, 'Helmet', 'Hardened Helmet', 73, { ...t, r: 255, a: 150 }),
    row(1021, 'Chest', 'Hardened Chestplate', 46, { ...t, r: 255, a: 150 }),
  ], CLASSES);

  assert.deepEqual(sets.map((s) => s.items.map((i) => i.id)), [[1009, 1010], [1020, 1021]]);
});

test('same-named halves with different class requirements merge into one mixed set', () => {
  const sets = Sets.detect([
    row(94, 'Helmet', 'Devastators Helmet', 148, { classes: 12, minLevel: 50 }),
    row(126, 'Shoes', 'Devastators Boots', 4, { minLevel: 50 }),
    row(166, 'Pants', 'Devastators Legplates', 6, { minLevel: 50 }),
    row(167, 'Chest', 'Devastators Chestplate', 72, { classes: 12, minLevel: 50 }),
  ], CLASSES);

  assert.equal(sets.length, 1);
  assert.deepEqual(sets[0].items.map((i) => i.id), [94, 126, 166, 167]);
  assert.equal(sets[0].mixedClasses, true);
  assert.deepEqual(sets[0].classNames, ['Rogue', 'Warrior']);
});
