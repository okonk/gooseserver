import { test } from 'node:test';
import assert from 'node:assert/strict';

const { Equipped } = await import('../src/equipped.js');
globalThis.Equipped = Equipped;

const { Appearance } = await import('../src/appearance.js');

const base = {
  bodyId: 1, bodyR: 0, bodyG: 0, bodyB: 0, bodyA: 0,
  hairId: 26, hairR: 10, hairG: 20, hairB: 30, hairA: 128,
  faceId: 70,
  equippedItems: '0,*,0,*,0,*,0,*,0,*,0,*',
};

const slotsOf = (layers) => layers.map((l) => l.slot);
const find = (layers, slot) => layers.find((l) => l.slot === slot);

test('draw order is back to front by CharacterLayout.SortOrder', () => {
  const order = slotsOf(Appearance.layers({ ...base, bodyId: 1 }));
  // CharacterLayout.cs:20-37 — (int)slot + 2, Shield/Weapon flip by facing. This character
  // has nothing on its feet, so Feet is absent and underwear Legs follows Eyes; the test
  // below pins the order with every slot filled.
  assert.deepEqual(order.slice(0, 3), ['Body', 'Eyes', 'Legs']);

  // Hair above chest, helm above hair — the specific inversion the ApplySlot call order at
  // Character.cs:231-240 would have produced.
  const dressed = slotsOf(Appearance.layers({
    ...base, bodyId: 1, equippedItems: '5,*,6,*,0,*,0,*,0,*,0,*',
  }));
  assert.ok(dressed.indexOf('Chest') > -1 && dressed.indexOf('Helm') > -1);
  assert.ok(dressed.indexOf('Hair') > dressed.indexOf('Chest'));
  assert.ok(dressed.indexOf('Helm') > dressed.indexOf('Hair'));
});

test('the full down-facing order matches (int)slot + 2 for every slot', () => {
  // CharacterLayout.cs:6 + :22. Mount is never emitted as a layer (see below) but its
  // order still has to be right for anything that composes one.
  assert.deepEqual(
    ['Mount', 'Body', 'Eyes', 'Feet', 'Legs', 'Chest', 'Hair', 'Helm', 'Shield', 'Weapon']
      .map(Appearance.sortOrder),
    [2, 3, 4, 5, 6, 7, 8, 9, 10, 11],
  );
});

test('a fully equipped character emits all nine slots back to front', () => {
  const layers = Appearance.layers({
    ...base, bodyId: 1, equippedItems: '5,*,6,*,7,*,8,*,9,*,10,*',
  });
  assert.deepEqual(slotsOf(layers),
    ['Body', 'Eyes', 'Feet', 'Legs', 'Chest', 'Hair', 'Helm', 'Shield', 'Weapon']);
  assert.deepEqual(layers.map((l) => l.order), [3, 4, 5, 6, 7, 8, 9, 10, 11]);
  // equipped_items order is Chest, Helm, Legs, Feet, Shield, Weapon
  // (MakeCharacterPacket.cs:97-99) — pin each token to the slot it lands in.
  assert.deepEqual(layers.map((l) => l.id), [1, 70, 8, 7, 5, 26, 6, 9, 10]);
});

test('monster bodies render only the body sprite', () => {
  // Character.cs:218-223 — body_id >= 100 zeroes everything else.
  const layers = Appearance.layers({ ...base, bodyId: 10113 });
  assert.deepEqual(slotsOf(layers), ['Body']);
  assert.equal(layers[0].id, 10113);
});

test('the monster cutoff is exactly >= 100', () => {
  const eq = '5,*,6,*,7,*,8,*,9,*,10,*';
  assert.equal(slotsOf(Appearance.layers({ ...base, bodyId: 99, equippedItems: eq })).length, 9);
  assert.deepEqual(slotsOf(Appearance.layers({ ...base, bodyId: 100, equippedItems: eq })), ['Body']);
  assert.deepEqual(slotsOf(Appearance.layers({ ...base, bodyId: 101, equippedItems: eq })), ['Body']);
});

test('a monster body keeps its own tint', () => {
  // Character.cs:220 zeroes the other ids; bodyR..bodyA are untouched.
  const layers = Appearance.layers({
    ...base, bodyId: 200, bodyR: 1, bodyG: 2, bodyB: 3, bodyA: 4,
  });
  assert.deepEqual([layers[0].r, layers[0].g, layers[0].b, layers[0].a], [1, 2, 3, 4]);
});

test('male body gets underwear legs 3 when the slot is empty', () => {
  // CharacterLayout.cs:56-62.
  const legs = find(Appearance.layers({ ...base, bodyId: 1 }), 'Legs');
  assert.equal(legs.id, 3);
  assert.deepEqual([legs.r, legs.g, legs.b, legs.a], [0, 0, 0, 0]);
});

test('male body gets no underwear chest', () => {
  // CharacterLayout.cs:64-69 — chest 8 is female-only.
  assert.equal(find(Appearance.layers({ ...base, bodyId: 1 }), 'Chest'), undefined);
});

test('female body gets underwear legs 4 and chest 8', () => {
  const layers = Appearance.layers({ ...base, bodyId: 11 });
  assert.equal(find(layers, 'Legs').id, 4);
  assert.equal(find(layers, 'Chest').id, 8);
  assert.deepEqual(find(layers, 'Chest').a, 0);
});

test('any other sub-100 body gets no underwear at all', () => {
  const layers = Appearance.layers({ ...base, bodyId: 12 });
  assert.equal(find(layers, 'Legs'), undefined);
  assert.equal(find(layers, 'Chest'), undefined);
});

test('equipped legs suppress underwear', () => {
  const layers = Appearance.layers({
    ...base, bodyId: 1, equippedItems: '0,*,0,*,42,*,0,*,0,*,0,*',
  });
  assert.equal(find(layers, 'Legs').id, 42);
});

test('equipped chest suppresses the female underwear chest', () => {
  const layers = Appearance.layers({
    ...base, bodyId: 11, equippedItems: '42,*,0,*,0,*,0,*,0,*,0,*',
  });
  assert.equal(find(layers, 'Chest').id, 42);
  assert.equal(find(layers, 'Legs').id, 4);
});

test('a tinted equipped legs slot keeps its tint and suppresses underwear', () => {
  const layers = Appearance.layers({
    ...base, bodyId: 1, equippedItems: '0,*,0,*,42,1,2,3,4,0,*,0,*,0,*',
  });
  const legs = find(layers, 'Legs');
  assert.deepEqual([legs.id, legs.r, legs.g, legs.b, legs.a], [42, 1, 2, 3, 4]);
});

test('underwear does not leak between slots', () => {
  // Guards the aliasing trap: Legs and Chest must be independent objects.
  const layers = Appearance.layers({ ...base, bodyId: 11 });
  assert.notEqual(find(layers, 'Legs').id, find(layers, 'Chest').id);
});

test('slots with graphic 0 are omitted entirely', () => {
  const layers = Appearance.layers({ ...base, bodyId: 1 });
  assert.equal(find(layers, 'Weapon'), undefined);
  assert.equal(find(layers, 'Shield'), undefined);
});

test('a zero body id emits no body layer', () => {
  // Character.cs:264 — ApplySlot removes the slot for graphicId <= 0.
  assert.equal(find(Appearance.layers({ ...base, bodyId: 0 }), 'Body'), undefined);
});

test('negative graphic ids are dropped like zero', () => {
  // Character.cs:264 is `graphicId <= 0`. Equipped.parse does not clamp, so a stored
  // "-5" reaches here intact.
  const layers = Appearance.layers({
    ...base, bodyId: -1, equippedItems: '-5,*,0,*,0,*,0,*,0,*,0,*',
  });
  assert.equal(find(layers, 'Body'), undefined);
  assert.equal(find(layers, 'Chest'), undefined);
});

test('per-slot tints come from equipped_items', () => {
  const layers = Appearance.layers({
    ...base, bodyId: 1, equippedItems: '9,255,128,64,200,0,*,0,*,0,*,0,*,0,*',
  });
  const chest = find(layers, 'Chest');
  assert.deepEqual([chest.id, chest.r, chest.g, chest.b, chest.a], [9, 255, 128, 64, 200]);
});

test('an untinted equipped slot reports all-zero colour', () => {
  const layers = Appearance.layers({
    ...base, bodyId: 1, equippedItems: '9,*,0,*,0,*,0,*,0,*,0,*',
  });
  const chest = find(layers, 'Chest');
  assert.deepEqual([chest.r, chest.g, chest.b, chest.a], [0, 0, 0, 0]);
});

test('a zero-alpha tint is discarded, colour and all', () => {
  // Character.cs:251-258 — RgbaColor/Equip return NoTint unless a > 0, so the parked
  // r/g/b never reach the shader.
  const layers = Appearance.layers({
    ...base, bodyId: 1, hairA: 0, equippedItems: '9,255,128,64,0,0,*,0,*,0,*,0,*,0,*',
  });
  assert.deepEqual([find(layers, 'Chest').r, find(layers, 'Chest').g,
    find(layers, 'Chest').b, find(layers, 'Chest').a], [0, 0, 0, 0]);
  const hair = find(layers, 'Hair');
  assert.deepEqual([hair.r, hair.g, hair.b, hair.a], [0, 0, 0, 0]);
});

test('out-of-range channels are clamped to a byte', () => {
  const layers = Appearance.layers({
    ...base, bodyId: 1, bodyR: 900, bodyG: -5, bodyB: 20, bodyA: 300,
  });
  const body = find(layers, 'Body');
  assert.deepEqual([body.r, body.g, body.b, body.a], [255, 0, 20, 255]);
});

test('hair carries its own tint and eyes never tint', () => {
  const layers = Appearance.layers({ ...base, bodyId: 1 });
  const hair = find(layers, 'Hair');
  assert.deepEqual([hair.r, hair.g, hair.b, hair.a], [10, 20, 30, 128]);
  const eyes = find(layers, 'Eyes');
  assert.deepEqual([eyes.r, eyes.g, eyes.b, eyes.a], [0, 0, 0, 0]);
});

test('the eyes layer never picks up the body tint', () => {
  // Character.cs:233 passes NoTint explicitly.
  const layers = Appearance.layers({
    ...base, bodyId: 1, bodyR: 9, bodyG: 9, bodyB: 9, bodyA: 9,
  });
  assert.deepEqual(find(layers, 'Eyes').a, 0);
});

test('a zero hair or face id drops that layer', () => {
  const layers = Appearance.layers({ ...base, bodyId: 1, hairId: 0, faceId: 0 });
  assert.equal(find(layers, 'Hair'), undefined);
  assert.equal(find(layers, 'Eyes'), undefined);
});

test('each layer names its sprite category', () => {
  // CharacterLayout.cs:39-52 — Shield and Weapon both come from Hands.
  const layers = Appearance.layers({
    ...base, bodyId: 1, equippedItems: '0,*,0,*,0,*,0,*,3,*,4,*',
  });
  assert.equal(find(layers, 'Shield').category, 'Hands');
  assert.equal(find(layers, 'Weapon').category, 'Hands');
  assert.equal(find(layers, 'Eyes').category, 'Eyes');
  assert.equal(find(layers, 'Helm'), undefined);
});

test('every slot maps to its client sprite folder', () => {
  // CharacterLayout.cs:39-52, including Mount, which layers() never emits.
  assert.deepEqual(Appearance.CATEGORY, {
    Mount: 'Bodies', Body: 'Bodies', Eyes: 'Eyes', Feet: 'Feet', Legs: 'Legs',
    Chest: 'Chest', Hair: 'Hair', Helm: 'Helms', Shield: 'Hands', Weapon: 'Hands',
  });
});

test('the remaining equipment categories come through on a dressed character', () => {
  const layers = Appearance.layers({
    ...base, bodyId: 1, equippedItems: '5,*,6,*,7,*,8,*,0,*,0,*',
  });
  assert.equal(find(layers, 'Body').category, 'Bodies');
  assert.equal(find(layers, 'Hair').category, 'Hair');
  assert.equal(find(layers, 'Chest').category, 'Chest');
  assert.equal(find(layers, 'Helm').category, 'Helms');
  assert.equal(find(layers, 'Legs').category, 'Legs');
  assert.equal(find(layers, 'Feet').category, 'Feet');
});

test('spreadsheet cells arrive as strings and must still be treated as numbers', () => {
  // Task 11 reads values straight out of the sheet, so every field can be a string.
  const layers = Appearance.layers({
    bodyId: '11', bodyR: '0', bodyG: '0', bodyB: '0', bodyA: '0',
    hairId: '26', hairR: '10', hairG: '20', hairB: '30', hairA: '128',
    faceId: '70', equippedItems: '0,*,0,*,0,*,0,*,0,*,0,*',
  });
  assert.equal(find(layers, 'Legs').id, 4);      // female underwear, not string-compared away
  assert.equal(find(layers, 'Chest').id, 8);
  assert.equal(find(layers, 'Hair').id, 26);
  assert.deepEqual([find(layers, 'Hair').r, find(layers, 'Hair').a], [10, 128]);
});

test('a string monster body id still suppresses equipment', () => {
  const layers = Appearance.layers({
    ...base, bodyId: '10113', equippedItems: '5,*,6,*,7,*,8,*,9,*,10,*',
  });
  assert.deepEqual(slotsOf(layers), ['Body']);
});

test('missing appearance fields default to nothing rather than throwing', () => {
  assert.deepEqual(Appearance.layers({}), []);
  assert.deepEqual(slotsOf(Appearance.layers({ bodyId: 1 })), ['Body', 'Legs']);
});

test('vertical anchor matches CharacterAnchor.OffsetY', () => {
  // CharacterAnchor.cs:13 — max((h-48)/2, 0) - h/2, C# integer division.
  assert.equal(Appearance.offsetY(48), -24);
  assert.equal(Appearance.offsetY(80), -24);
  assert.equal(Appearance.offsetY(24), -12);
  assert.equal(Appearance.offsetY(96), -24);
});

test('vertical anchor over every height in the sprite bundle', () => {
  // Distinct heights in Assets/Resources/AnimationHeights.txt, 95 being the only odd one.
  const expected = {
    20: -10, 32: -16, 44: -22, 48: -24, 50: -24, 60: -24,
    80: -24, 95: -24, 96: -24, 128: -24, 288: -24,
  };
  for (const h of Object.keys(expected)) {
    assert.equal(Appearance.offsetY(Number(h)), expected[h], 'height ' + h);
  }
});

test('vertical anchor truncates toward zero like C# integer division', () => {
  // 47 exercises both truncations with a negative numerator: C# gives (-1)/2 == 0 and
  // -47/2 == -23. Math.floor would give -1 and -24.
  assert.equal(Appearance.offsetY(47), -23);
  assert.equal(Appearance.offsetY(49), -24);
  assert.equal(Appearance.offsetY(0), 0);
});
