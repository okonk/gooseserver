import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';

const schemaSource = readFileSync(fileURLToPath(new URL('../schema.js', import.meta.url)), 'utf8');
globalThis.GOOSE_SCHEMA = new Function(schemaSource + '\nreturn GOOSE_SCHEMA;')();
const { Balance } = await import('../src/balance.js');
globalThis.Balance = Balance;
const { BalanceView } = await import('../src/balanceview.js');

const schemaOf = (sheet) => GOOSE_SCHEMA.sheets.filter((s) => s.sheet === sheet)[0];
function rowFor(sheet, values) {
  return schemaOf(sheet).columns.map((c) => (values[c.name] === undefined ? '' : String(values[c.name])));
}

test('build joins stats, the equip effect and the tag row, and skips non-equipment', () => {
  const items = BalanceView.build(schemaOf('Items'), [
    rowFor('Items', { item_template_id: 1, item_name: 'Staff', item_slot: 'TwoHanded', min_level: 40,
                      class_restrictions: 48, player_mp: 300, weapon_damage: 30, spell_effect_id: 9 }),
    rowFor('Items', { item_template_id: 2, item_name: 'Potion', item_slot: '' }),
  ], [
    rowFor('Spell Effects', { spell_effect_id: 9, spell_effect_name: 'Spell Damage V', spell_damage: 0.05 }),
  ], [
    rowFor('Item Balance', { item_template_id: 1, audience: 16, profile: 'MP', step: 'Nagan', source: 'RareBoss' }),
  ]);

  assert.equal(items.length, 1);
  const staff = items[0];
  assert.equal(staff.slot, 'Weapon');
  assert.equal(Balance.slotOf(staff), 'Caster weapon');
  assert.equal(staff.mp, 300);
  assert.equal(staff.wd, 30);
  assert.equal(staff.effect.spell_damage, 0.05);
  assert.equal(staff.effectName, 'Spell Damage V');
  assert.equal(staff.tagRow, 2);
  assert.equal(staff.step, 'Nagan');
  assert.deepEqual([...Balance.audienceOf(staff)], ['Magus']);
});

test('an untagged item gets editable defaults and no tag row', () => {
  const items = BalanceView.build(schemaOf('Items'), [
    rowFor('Items', { item_template_id: 5, item_name: 'Cap', item_slot: 'Helmet', class_restrictions: 12 }),
  ], [], []);

  assert.equal(items[0].tagRow, 0);
  assert.equal(items[0].tagValues.audience, '12');
  assert.equal(items[0].tagValues.power, 'Normal');
  assert.equal(items[0].step, 'Levelling');
});

test('slots are listed in display order with failing and untagged counts', () => {
  const items = BalanceView.build(schemaOf('Items'), [
    rowFor('Items', { item_template_id: 1, item_slot: 'Chest', min_level: 40, class_restrictions: 16, player_mp: 500 }),
    rowFor('Items', { item_template_id: 2, item_slot: 'Chest', min_level: 42, class_restrictions: 16, player_mp: 510 }),
    rowFor('Items', { item_template_id: 3, item_slot: 'Helmet', min_level: 10, class_restrictions: 16, player_mp: 50 }),
  ], [], [
    rowFor('Item Balance', { item_template_id: 1, profile: 'MP', step: 'Levelling', source: 'Common' }),
  ]);

  assert.deepEqual(BalanceView.slots(items).map((s) => [s.slot, s.count, s.failing, s.untagged]),
    [['Helmet', 1, 0, 1], ['Chest', 2, 1, 1]]);
});
