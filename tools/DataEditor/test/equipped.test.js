import { test } from 'node:test';
import assert from 'node:assert/strict';

const { Equipped } = await import('../src/equipped.js');

test('parses the common untinted form', () => {
  const slots = Equipped.parse('0,*,0,*,0,*,0,*,0,*,0,*');
  assert.equal(slots.length, 6);
  assert.deepEqual(slots[0], { graphic: 0, r: 0, g: 0, b: 0, a: 0, tinted: false });
});

test('parses a five-token tinted slot', () => {
  // MakeCharacterPacket.cs:113-128 — a slot is either id,* or id,r,g,b,a.
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
