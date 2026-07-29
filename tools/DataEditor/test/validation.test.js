import { test } from 'node:test';
import assert from 'node:assert/strict';

// Modules assign globals and export for node; load once.
const { Validation } = await import('../src/validation.js');

const col = (over = {}) => ({
  name: 'x', kind: 'Int', sql: 'INT', required: false, pk: false, ...over,
});

test('blank optional value is valid and writes nothing', () => {
  const r = Validation.validateCell(col(), '');
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

// --- Decimal precision and scale -------------------------------------------------

test('decimal within DECIMAL(5,2) is accepted', () => {
  const c = col({ kind: 'Decimal', sql: 'DECIMAL(5,2)' });
  assert.equal(Validation.validateCell(c, '999.99').ok, true);
  assert.equal(Validation.validateCell(c, '-999.99').ok, true);
  assert.equal(Validation.validateCell(c, '12').ok, true);
});

test('decimal exceeding DECIMAL(5,2) integer digits is rejected', () => {
  const c = col({ name: 'snare_percent', kind: 'Decimal', sql: 'DECIMAL(5,2)' });
  const r = Validation.validateCell(c, '1000');
  assert.equal(r.ok, false);
  assert.match(r.message, /snare_percent/);
  assert.match(r.message, /999\.99/);
});

test('decimal exceeding DECIMAL(5,4) integer digits is rejected', () => {
  const c = col({ name: 'chance', kind: 'Decimal', sql: 'DECIMAL(5,4)' });
  assert.equal(Validation.validateCell(c, '9.9999').ok, true);
  const r = Validation.validateCell(c, '12.5');
  assert.equal(r.ok, false);
  assert.match(r.message, /9\.9999/);
});

test('decimal with too many fraction digits is rejected', () => {
  const c = col({ kind: 'Decimal', sql: 'DECIMAL(5,2)' });
  const r = Validation.validateCell(c, '99999.99999');
  assert.equal(r.ok, false);
  const s = Validation.validateCell(c, '1.234');
  assert.equal(s.ok, false);
  assert.match(s.message, /2 decimal place/);
});

// --- Edge cases ------------------------------------------------------------------

test('whitespace-only input is treated as blank', () => {
  assert.equal(Validation.validateCell(col(), '   ').write, false);
  const r = Validation.validateCell(col({ required: true }), '   ');
  assert.equal(r.ok, false);
  assert.match(r.message, /required/i);
});

test('negative values are accepted in a signed integer column', () => {
  assert.equal(Validation.validateCell(col({ sql: 'INT' }), '-5').ok, true);
  assert.equal(Validation.validateCell(col({ sql: 'SMALLINT' }), '-32768').ok, true);
  assert.equal(Validation.validateCell(col({ sql: 'SMALLINT' }), '-32769').ok, false);
});

test('a required FK left blank is rejected before the lookup', () => {
  const c = col({ name: 'item_id', kind: 'Id', ref: 'Items', required: true });
  const r = Validation.validateCell(c, '', { Items: new Set([1]) });
  assert.equal(r.ok, false);
  assert.match(r.message, /required/i);
});

test('validateId reports 0 as out of range, not as missing', () => {
  const r = Validation.validateId(0, new Set(), null);
  assert.equal(r.ok, false);
  assert.doesNotMatch(r.message, /required/i);
  const neg = Validation.validateId('-1', new Set(), null);
  assert.equal(neg.ok, false);
  assert.match(neg.message, /positive/i);
});

test('nextId ignores non-numeric entries and accepts a Set', () => {
  assert.equal(Validation.nextId([1, 'a', 3]), 4);
  assert.equal(Validation.nextId(new Set([1, 2, 3])), 4);
  assert.equal(Validation.nextId(['x']), 1);
  assert.equal(Validation.nextId(null), 1);
});

// --- validateRecord id seam ------------------------------------------------------

test('validateRecord rejects an id already used in the sheet', () => {
  const columns = [col({ name: 'id', kind: 'Id', pk: true, required: true })];
  const idSets = { __self: new Set([1, 2, 3]) };
  const r = Validation.validateRecord(columns, { id: '2' }, idSets, null);
  assert.equal(r.ok, false);
  assert.equal(r.errors[0].column, 'id');
  assert.match(r.errors[0].message, /already used/);
});

test('validateRecord allows a row to keep its own id', () => {
  const columns = [col({ name: 'id', kind: 'Id', pk: true, required: true })];
  const idSets = { __self: new Set([1, 2, 3]) };
  assert.equal(Validation.validateRecord(columns, { id: '2' }, idSets, 2).ok, true);
  assert.equal(Validation.validateRecord(columns, { id: '4' }, idSets, 2).ok, true);
});
