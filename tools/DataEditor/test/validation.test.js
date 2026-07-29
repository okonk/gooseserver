import { test } from 'node:test';
import assert from 'node:assert/strict';

// Modules assign globals and export for node; load once.
const { Validation } = await import('../src/validation.js');

const col = (over = {}) => ({
  name: 'x', kind: 'Int', sql: 'INT', required: false, pk: false, ...over,
});

test('blank optional value is valid and writes nothing', () => {
  const r = Validation.validateCell(col({ default: '0' }), '');
  assert.equal(r.ok, true);
  assert.equal(r.write, false);
});

test('blank required value is invalid', () => {
  const r = Validation.validateCell(col({ required: true }), '');
  assert.equal(r.ok, false);
  assert.match(r.message, /required/i);
});

test('enum accepts a declared member and rejects others', () => {
  const c = col({ kind: 'Enum', enumNames: ['Weapon', 'Armour'] });
  assert.equal(Validation.validateCell(c, 'Weapon').ok, true);
  const bad = Validation.validateCell(c, 'weapon');
  assert.equal(bad.ok, false);
  assert.match(bad.message, /Weapon/);
});

test('integer range follows the SQL width', () => {
  assert.equal(Validation.validateCell(col({ sql: 'SMALLINT' }), '32767').ok, true);
  assert.equal(Validation.validateCell(col({ sql: 'SMALLINT' }), '32768').ok, false);
  assert.equal(Validation.validateCell(col({ sql: 'INT' }), '2147483647').ok, true);
  assert.equal(Validation.validateCell(col({ sql: 'INT' }), '2147483648').ok, false);
  assert.equal(Validation.validateCell(col({ sql: 'BIGINT' }), '9223372036854775807').ok, true);
});

test('non-numeric text in a numeric column is rejected', () => {
  const r = Validation.validateCell(col({ sql: 'INT' }), 'abc');
  assert.equal(r.ok, false);
  assert.match(r.message, /number/i);
});

test('bool accepts only 0 and 1', () => {
  const c = col({ kind: 'Bool', sql: 'CHAR(1)' });
  assert.equal(Validation.validateCell(c, '0').ok, true);
  assert.equal(Validation.validateCell(c, '1').ok, true);
  assert.equal(Validation.validateCell(c, 'true').ok, false);
});

test('text is accepted as-is', () => {
  assert.equal(Validation.validateCell(col({ kind: 'Text', sql: 'TEXT' }), "Bob's Hat").ok, true);
});

test('empty optional FK is valid', () => {
  const c = col({ kind: 'Id', ref: 'Items' });
  const r = Validation.validateCell(c, '', { Items: new Set([1, 2]) });
  assert.equal(r.ok, true);
});

test('unresolvable FK is rejected and names the id', () => {
  const c = col({ kind: 'Id', ref: 'Items' });
  const r = Validation.validateCell(c, '4471', { Items: new Set([1, 2]) });
  assert.equal(r.ok, false);
  assert.match(r.message, /4471/);
  assert.match(r.message, /Items/);
});

test('resolvable FK passes', () => {
  const c = col({ kind: 'Id', ref: 'Items' });
  assert.equal(Validation.validateCell(c, '2', { Items: new Set([1, 2]) }).ok, true);
});

test('zero FK is treated as none, not a broken reference', () => {
  const c = col({ kind: 'Id', ref: 'Items' });
  assert.equal(Validation.validateCell(c, '0', { Items: new Set([1]) }).ok, true);
});

test('duplicate id is rejected, own id is allowed', () => {
  const existing = new Set([1, 2, 3]);
  assert.equal(Validation.validateId('4', existing, null).ok, true);
  assert.equal(Validation.validateId('2', existing, null).ok, false);
  // Editing row with id 2 — its own id must not count as a duplicate.
  assert.equal(Validation.validateId('2', existing, 2).ok, true);
});

test('nextId returns max plus one, and 1 for an empty sheet', () => {
  assert.equal(Validation.nextId([3, 1, 7]), 8);
  assert.equal(Validation.nextId([]), 1);
});

test('validateRecord collects every failure', () => {
  const columns = [
    col({ name: 'id', kind: 'Id', pk: true, required: true }),
    col({ name: 'name', kind: 'Text', sql: 'TEXT', required: true }),
    col({ name: 'kind', kind: 'Enum', enumNames: ['A'] }),
  ];
  const r = Validation.validateRecord(columns, { id: '5', name: '', kind: 'B' }, {});
  assert.equal(r.ok, false);
  assert.equal(r.errors.length, 2);
  assert.deepEqual(r.errors.map((e) => e.column).sort(), ['kind', 'name']);
});
