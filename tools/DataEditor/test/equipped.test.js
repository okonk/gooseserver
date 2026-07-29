import { test } from 'node:test';
import assert from 'node:assert/strict';

const { Equipped } = await import('../src/equipped.js');

test('parses the common untinted form', () => {
  const slots = Equipped.parse('0,*,0,*,0,*,0,*,0,*,0,*');
  assert.equal(slots.length, 6);
  assert.deepEqual(slots[0], { graphic: 0, r: 0, g: 0, b: 0, a: 0, tinted: false });
});

test('parses a five-token tinted slot', () => {
  // MakeCharacterPacket.cs:109-133 — a slot is either id,* or id,r,g,b,a.
  const slots = Equipped.parse('5,255,0,0,128,0,*,0,*,0,*,0,*,0,*');
  assert.equal(slots.length, 6);
  assert.deepEqual(slots[0], { graphic: 5, r: 255, g: 0, b: 0, a: 128, tinted: true });
  assert.equal(slots[1].graphic, 0);
});

test('slot order is Chest, Helm, Legs, Feet, Shield, Weapon', () => {
  assert.deepEqual(Equipped.SLOTS, ['Chest', 'Helm', 'Legs', 'Feet', 'Shield', 'Weapon']);
});

test('empty string yields six empty slots', () => {
  const slots = Equipped.parse('');
  assert.equal(slots.length, 6);
  assert.ok(slots.every((s) => s.graphic === 0));
});

test('formats back to the untinted form', () => {
  assert.equal(Equipped.format(Equipped.parse('0,*,0,*,0,*,0,*,0,*,0,*')),
               '0,*,0,*,0,*,0,*,0,*,0,*');
});

test('round-trips a tinted slot', () => {
  const input = '5,255,0,0,128,0,*,0,*,0,*,0,*,0,*';
  assert.equal(Equipped.format(Equipped.parse(input)), input);
});

test('a slot with zero blend alpha formats as untinted', () => {
  const slots = Equipped.parse('0,*,0,*,0,*,0,*,0,*,0,*');
  slots[0] = { graphic: 7, r: 10, g: 20, b: 30, a: 0, tinted: true };
  // a === 0 means no blend, so the compact form is equivalent and matches existing data.
  assert.equal(Equipped.format(slots), '7,*,0,*,0,*,0,*,0,*,0,*');
});

test('tolerates trailing whitespace and extra tokens', () => {
  const slots = Equipped.parse(' 0,*,0,*,0,*,0,*,0,*,0,* ');
  assert.equal(slots.length, 6);
  assert.equal(Equipped.format(slots), '0,*,0,*,0,*,0,*,0,*,0,*');
});

test('a tint in a later slot keeps its channels in r,g,b,a order', () => {
  const slots = Equipped.parse('0,*,7,11,22,33,44,0,*,0,*,0,*');
  assert.deepEqual(slots[1], { graphic: 7, r: 11, g: 22, b: 33, a: 44, tinted: true });
  assert.equal(slots[2].graphic, 0);
  assert.equal(slots.length, 6);
});

test('the tinted slot stride is five tokens', () => {
  // Pins i += 4: the token after a tint's alpha is the next slot's graphic.
  const slots = Equipped.parse('1,11,22,33,44,2,*,3,*,4,*,5,*,6,*');
  assert.deepEqual(slots.map((s) => s.graphic), [1, 2, 3, 4, 5, 6]);
});

test('a tint one channel short is demoted, not read past the end', () => {
  const slots = Equipped.parse('5,1,2,3');
  assert.deepEqual(slots[0], { graphic: 5, r: 1, g: 2, b: 3, a: 0, tinted: false });
});

test('a tint with exactly enough channels is kept', () => {
  // The other side of the i + 3 >= tokens.length boundary.
  const slots = Equipped.parse('5,1,2,3,4');
  assert.deepEqual(slots[0], { graphic: 5, r: 1, g: 2, b: 3, a: 4, tinted: true });
});

test('an embedded empty token consumes one slot and does not shift the rest', () => {
  const slots = Equipped.parse('9,*,,8,*');
  assert.equal(slots[0].graphic, 9);
  assert.deepEqual(slots[1], Equipped.empty());
  assert.equal(slots[2].graphic, 8);
});

test('empty() is all zeros and untinted', () => {
  assert.deepEqual(Equipped.empty(), { graphic: 0, r: 0, g: 0, b: 0, a: 0, tinted: false });
});

test('whitespace around every token is stripped', () => {
  const slots = Equipped.parse(' 5 , 1 , 2 , 3 , 4 , 6 , * ,0,*,0,*,0,*,0,*');
  assert.deepEqual(slots[0], { graphic: 5, r: 1, g: 2, b: 3, a: 4, tinted: true });
  // A padded '*' must still be recognised as the untinted marker, not read as a tint.
  assert.deepEqual(slots[1], { graphic: 6, r: 0, g: 0, b: 0, a: 0, tinted: false });
  assert.equal(slots[2].graphic, 0);
  assert.equal(Equipped.format(slots), '5,1,2,3,4,6,*,0,*,0,*,0,*,0,*');
});

test('an all-whitespace cell is six empty slots and is faithful', () => {
  assert.deepEqual(Equipped.parse('   '), Equipped.parse(''));
  assert.equal(Equipped.isFaithful('   '), true);
});

test('format coerces values the UI can produce into a legal stream', () => {
  // equipSlotsControl binds graphic to a text input, so clearing the field yields ''.
  // PacketParser.GetInt32 is Convert.ToInt32, which throws FormatException on '',
  // 'undefined' and '1.5' — killing the entire MakeCharacter packet.
  const six = (first) => [first, {}, {}, {}, {}, {}];
  assert.equal(Equipped.format(six({ graphic: '' })), '0,*,0,*,0,*,0,*,0,*,0,*');
  assert.equal(Equipped.format(six({})), '0,*,0,*,0,*,0,*,0,*,0,*');
  assert.equal(Equipped.format(six({ graphic: -3 })), '0,*,0,*,0,*,0,*,0,*,0,*');
  assert.equal(Equipped.format(six({ graphic: 5, a: 1.5, tinted: true })),
               '5,0,0,0,1,0,*,0,*,0,*,0,*,0,*');
});

test('format clamps tint channels to 0-255', () => {
  // Icon.cs:9-11 divides by 255 without clamping, so out-of-range values skew the blend.
  const slots = Equipped.parse('0,*,0,*,0,*,0,*,0,*,0,*');
  slots[0] = { graphic: 5, r: 300, g: -5, b: 1.5, a: 200, tinted: true };
  assert.equal(Equipped.format(slots), '5,255,0,1,200,0,*,0,*,0,*,0,*,0,*');
});

test('isFaithful accepts the canonical forms', () => {
  assert.equal(Equipped.isFaithful('0,*,0,*,0,*,0,*,0,*,0,*'), true);
  assert.equal(Equipped.isFaithful('5,255,0,0,128,0,*,0,*,0,*,0,*,0,*'), true);
  // A blank cell means "nothing equipped"; canonicalising it loses nothing.
  assert.equal(Equipped.isFaithful(''), true);
});

test('isFaithful rejects a stream with a missing or embedded-empty slot', () => {
  // Both reach the zero-fill branch, which invents slots the row never had.
  assert.equal(Equipped.isFaithful('9,*,,8,*,0,*,0,*,0,*'), false);
  assert.equal(Equipped.isFaithful('5,*'), false);
});

test('isFaithful rejects tokens past the sixth slot', () => {
  // Six good slots parse fine, but the trailing pair is data format() would drop.
  assert.equal(Equipped.isFaithful('0,*,0,*,0,*,0,*,0,*,0,*,7,*'), false);
});

test('isFaithful rejects a graphic token that is not purely a number', () => {
  // '14*' is the real defect in the shipped rows: Convert.ToInt32 throws on it, and parse()
  // reads it as 14, quietly inventing equipment. The match must be anchored — '14*'
  // contains digits but is not one.
  assert.equal(Equipped.isFaithful('14*,*,0,*,0,*,0,*,0,*,0,*'), false);
  assert.equal(Equipped.isFaithful('14,*,0,*,0,*,0,*,0,*,0,*'), true);
});

test('isFaithful rejects a tint channel outside 0-255', () => {
  // format() clamps it, so saving would silently change the colour.
  assert.equal(Equipped.isFaithful('5,300,0,0,128,0,*,0,*,0,*,0,*,0,*'), false);
  assert.equal(Equipped.isFaithful('5,255,0,0,128,0,*,0,*,0,*,0,*,0,*'), true);
});

test('isFaithful rejects the malformed rows in the shipped data', () => {
  // Real values from GooseData.sql / illutiaData.sql / npcs.sql. parse+format rewrites each
  // into different, valid-looking equipment, so the write-back path must refuse them.
  const malformed = [
    '36,*,0,*,14*,9,*,58,*,103,* ',
    '0,*,5*,0,*,0,*,0,*,19*',
    '92,29,150,190,120,0,*,0,*,0,*,0,*,55,*54,*',
    '5,29,150,190,120,140,,*,0,*,23,*',
  ];
  for (const raw of malformed) {
    assert.equal(Equipped.isFaithful(raw), false, raw);
    assert.notEqual(Equipped.format(Equipped.parse(raw)), raw.trim());
  }
});

test('a tinted slot truncated by the end of the stream is not marked tinted', () => {
  // There is no tint data to preserve, so reporting tinted:true would tell appearance.js
  // to blend a colour nobody supplied.
  const slots = Equipped.parse('5,255');
  assert.deepEqual(slots[0], { graphic: 5, r: 255, g: 0, b: 0, a: 0, tinted: false });
  assert.equal(slots.length, 6);
});

test('a short stream still yields six slots and formats legally', () => {
  assert.equal(Equipped.format(Equipped.parse('4,*')), '4,*,0,*,0,*,0,*,0,*,0,*');
});
