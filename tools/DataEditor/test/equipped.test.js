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
  // A fractional graphic is the same FormatException as a fractional alpha.
  assert.equal(Equipped.format(six({ graphic: 1.5 })), '1,*,0,*,0,*,0,*,0,*,0,*');
  assert.equal(Equipped.format(six({ graphic: Infinity })), '0,*,0,*,0,*,0,*,0,*,0,*');
  // Graphic 1 is a common id and must survive coercion untouched.
  assert.equal(Equipped.format(six({ graphic: 1 })), '1,*,0,*,0,*,0,*,0,*,0,*');
  // The tinted branch emits the graphic too, so it needs the same coercion.
  assert.equal(Equipped.format(six({ graphic: 2.7, r: 1, g: 2, b: 3, a: 4, tinted: true })),
               '2,1,2,3,4,0,*,0,*,0,*,0,*,0,*');
  assert.equal(Equipped.format(six({ graphic: '', r: 1, g: 2, b: 3, a: 4, tinted: true })),
               '0,1,2,3,4,0,*,0,*,0,*,0,*,0,*');
});

test('format treats a negative alpha as no blend', () => {
  // The clamp runs before the compact-form test, so -5 collapses like 0 rather than
  // emitting a tint the client would render as untinted anyway.
  const six = (first) => [first, {}, {}, {}, {}, {}];
  assert.equal(Equipped.format(six({ graphic: 5, r: 1, g: 2, b: 3, a: -5, tinted: true })),
               '5,*,0,*,0,*,0,*,0,*,0,*');
});

test('parse and format agree on how they coerce a number', () => {
  // Two coercion rules in one module invites drift: '1e3' must not parse as 1 and format
  // as 1000.
  const six = (first) => [first, {}, {}, {}, {}, {}];
  assert.equal(Equipped.format(six({ graphic: '1e3' })), '1,*,0,*,0,*,0,*,0,*,0,*');
  assert.equal(Equipped.format(six({ graphic: '0x10' })), '0,*,0,*,0,*,0,*,0,*,0,*');
  assert.equal(Equipped.parse('1e3,*')[0].graphic, 1);
  assert.equal(Equipped.parse('0x10,*')[0].graphic, 0);
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
  // Anchored at BOTH ends: '*54' is the real defect in npc_templates and must not read as 54.
  assert.equal(Equipped.isFaithful('*54,*,0,*,0,*,0,*,0,*,0,*'), false);
  // A signed graphic is not valid either — format() would erase '-1' to 0.
  assert.equal(Equipped.isFaithful('-1,*,0,*,0,*,0,*,0,*,0,*'), false);
  assert.equal(Equipped.isFaithful('+1,*,0,*,0,*,0,*,0,*,0,*'), false);
});

test('isFaithful rejects a tint channel outside 0-255', () => {
  // format() clamps it, so saving would silently change the colour.
  assert.equal(Equipped.isFaithful('5,300,0,0,128,0,*,0,*,0,*,0,*,0,*'), false);
  assert.equal(Equipped.isFaithful('5,255,0,0,128,0,*,0,*,0,*,0,*,0,*'), true);
  // Every channel is checked, alpha included — it is the last of the four.
  assert.equal(Equipped.isFaithful('5,0,0,0,300,0,*,0,*,0,*,0,*,0,*'), false);
});

test('isFaithful rejects a tint channel that is not a whole number', () => {
  // Convert.ToInt32 throws on both; format() would silently truncate them.
  assert.equal(Equipped.isFaithful('5,1,2,3,1.5,0,*,0,*,0,*,0,*,0,*'), false);
  assert.equal(Equipped.isFaithful('5,1,2,3,12x,0,*,0,*,0,*,0,*,0,*'), false);
});

test('the untinted marker must be exactly *, not merely contain one', () => {
  // The client peeks a single character, so it reads '*54' as the marker; we require the
  // whole token. Diverging here is deliberate — '*54' means the row is corrupt, and the
  // client throws two slots later anyway — but it must be a decision, not an accident.
  const slots = Equipped.parse('5,*54,0,0,0,0,*,0,*,0,*,0,*');
  assert.equal(slots[0].tinted, true);
  assert.equal(Equipped.isFaithful('5,*54,0,0,0,0,*,0,*,0,*,0,*'), false);
});

test('a graphic id above 255 is not confused with a tint channel', () => {
  // Only the four channel tokens are range-checked; the graphic that follows them is not.
  assert.equal(Equipped.isFaithful('5,1,2,3,4,300,*,0,*,0,*,0,*,0,*'), true);
  assert.equal(Equipped.parse('5,1,2,3,4,300,*,0,*,0,*,0,*,0,*')[1].graphic, 300);
});

test('the tint channel range check is inclusive of 255', () => {
  assert.equal(Equipped.isFaithful('5,255,0,0,128,0,*,0,*,0,*,0,*,0,*'), true);
  assert.equal(Equipped.isFaithful('5,256,0,0,128,0,*,0,*,0,*,0,*,0,*'), false);
});

test('format lets the tinted flag win over stale channel values', () => {
  // An untinted slot that still carries channels from a previous edit must not emit them.
  const six = (first) => [first, {}, {}, {}, {}, {}];
  assert.equal(Equipped.format(six({ graphic: 5, r: 1, g: 2, b: 3, a: 4, tinted: false })),
               '5,*,0,*,0,*,0,*,0,*,0,*');
});

test('isFaithful rejects a colour parked behind a zero alpha', () => {
  // Well-formed by every token check, but format()'s compact-form collapse drops r/g/b
  // because a === 0. Certifying this would let Task 11 save away the parked colour.
  // Reachable from the UI (set a colour, zero the alpha) and from Pet.cs:360/431.
  assert.equal(Equipped.isFaithful('5,10,20,30,0,0,*,0,*,0,*,0,*,0,*'), false);
  // Any one of the three channels being nonzero is enough to lose data.
  assert.equal(Equipped.isFaithful('5,10,0,0,0,0,*,0,*,0,*,0,*,0,*'), false);
  assert.equal(Equipped.isFaithful('5,0,20,0,0,0,*,0,*,0,*,0,*,0,*'), false);
  assert.equal(Equipped.isFaithful('5,0,0,30,0,0,*,0,*,0,*,0,*,0,*'), false);
  assert.equal(Equipped.format(Equipped.parse('5,10,20,30,0,0,*,0,*,0,*,0,*,0,*')),
               '5,*,0,*,0,*,0,*,0,*,0,*');
  // An all-zero tint loses nothing when collapsed, so it stays faithful.
  assert.equal(Equipped.isFaithful('5,0,0,0,0,0,*,0,*,0,*,0,*,0,*'), true);
});

test('isFaithful rejects the malformed rows in the shipped data', () => {
  // Verbatim npc_templates.equipped_items values from Goose/bin/Debug/IllutiaGoose.db and
  // the SQL dumps. parse+format rewrites each into different, valid-looking equipment, so
  // the write-back path must refuse them.
  const malformed = [
    '36,*,0,*,14*,9,*,58,*,103,* ',
    '0,*,5*,0,*,0,*,0,*,19*',
    '92,204,14,210,160,13,204,14,210,160,0,*,0,*,55,*54,*',
    '5,207,12,12,140,24,183,0,0,150,3,6,18,64,140,,*,0,*,23,*',
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
