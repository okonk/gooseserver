import { test } from 'node:test';
import assert from 'node:assert/strict';

const { Balance } = await import('../src/balance.js');

function item(id, fields) {
  return Object.assign({ id, name: 'Item ' + id, slot: 'Chest', level: 50, step: 'Levelling',
    classes: 0, hp: 0, mp: 0, ac: 0, str: 0, sta: 0, dex: 0, int: 0, wd: 0, effect: {},
    tag: { source: 'RareBoss' } }, fields);
}

function close(actual, expected) {
  assert.ok(Math.abs(actual - expected) < 1e-3, `${actual} != ${expected}`);
}

// Reference values from the Python scorer used for the balance passes, on live item stats.
test('scores match the reference scorer', () => {
  const elemental = item(245, { classes: 16, hp: 75, mp: 550, ac: 55, sta: 8, int: 30 });
  close(Balance.score(elemental, 'Magus', { level: 50, step: 'Punchy' }), 84.6);

  const highPriest = item(242, { classes: 32, hp: 250, mp: 400, ac: 60, sta: 14, int: 24 });
  close(Balance.score(highPriest, 'Priest', { level: 50, step: 'Punchy' }), 70.0);

  const valiant = item(374, { classes: 12, hp: 300, ac: 275, str: 20, sta: 20, dex: 20 });
  close(Balance.score(valiant, 'Warrior', { level: 50, step: 'Nibbles' }, 'tank'), 182.8);
  close(Balance.score(valiant, 'Warrior', { level: 50, step: 'Nibbles' }, 'damage'), 127.8);

  const dragonTooth = item(206, { slot: 'Weapon', classes: 8, hp: 100, str: 20, sta: 10, dex: 10, wd: 130 });
  close(Balance.score(dragonTooth, 'Warrior', { level: 50, step: 'Savage' }), 116.8);

  const katana = item(456, { slot: 'Weapon', level: 30, classes: 12, hp: 50, mp: 50, str: 14, sta: 5, dex: 10, wd: 70 });
  close(Balance.score(katana, 'Rogue', { level: 30, step: 'Levelling' }), 179.9);

  const gero = item(426, { classes: 48, hp: 1000, mp: 3000, ac: 50, effect: { spell_damage: 0.1 } });
  close(Balance.score(gero, 'Magus', { level: 50, step: 'XP200M' }), 482.124);

  const poo = item(140, { slot: 'Gloves', level: 40, hp: 200, mp: 100, ac: 35, str: 24, sta: 12, dex: 12,
    int: 6, effect: { melee_damage: 0.04 } });
  close(Balance.score(poo, 'Warrior', { level: 40, step: 'Levelling' }, 'tank'), 125.92);
});

test('effect scale grows with the class pool from 20M and never drops below 1', () => {
  assert.equal(Balance.effectScale('Magus', 'Nibbles'), 1);
  assert.equal(Balance.effectScale('Rogue', 'XP20M'), 1);
  close(Balance.effectScale('Warrior', 'XP20M'), 1.2091);
  close(Balance.effectScale('Magus', 'XP100M'), 3.08);
  close(Balance.effectScale('Priest', 'XP400M'), 7.7532);
});

test('Sewers sits between Hay & Fray and Nagan', () => {
  assert.ok(Balance.stepIndex('HayFray') < Balance.stepIndex('Sewers'));
  assert.ok(Balance.stepIndex('Sewers') < Balance.stepIndex('Nagan'));
  assert.equal(Balance.strengthWeight('Warrior', 50, 'Sewers'), 15);
});

test('STR is worth full value below L50, 15 HP for melee at L50, and nothing at 20M', () => {
  assert.equal(Balance.strengthWeight('Warrior', 40, 'Levelling'), 50);
  assert.equal(Balance.strengthWeight('Priest', 40, 'Levelling'), 35);
  assert.equal(Balance.strengthWeight('Magus', 40, 'Levelling'), 20);
  assert.equal(Balance.strengthWeight('Rogue', 50, 'Nibbles'), 15);
  assert.equal(Balance.strengthWeight('Magus', 50, 'Punchy'), 0);
  assert.equal(Balance.strengthWeight('Warrior', 50, 'XP20M'), 0);
});

test('a caster-only weapon is its own slot and only Priests value its damage', () => {
  const staff = item(1, { slot: 'Weapon', level: 40, classes: 48, wd: 100 });
  assert.equal(Balance.slotOf(staff), 'Caster weapon');
  assert.equal(Balance.score(staff, 'Magus', { level: 40, step: 'Levelling' }), 0);
  assert.equal(Balance.score(staff, 'Priest', { level: 40, step: 'Levelling' }), 140);
});

test('the audience tag narrows which classes an item is checked for', () => {
  const a = item(1, { classes: 0, tag: { audience: 12, source: 'RareBoss' } });
  assert.deepEqual([...Balance.audienceOf(a)], ['Warrior', 'Rogue']);
  const b = item(2, { classes: 16, tag: { audience: 0 } });
  assert.deepEqual([...Balance.audienceOf(b)], ['Magus']);
});

test('a harder item that does not clear the easier one by 10% fails', () => {
  const old = item(1, { level: 40, classes: 16, mp: 500 });
  const next = item(2, { level: 42, classes: 16, mp: 520 });
  const { failures } = Balance.check([old, next]);
  assert.equal(failures.length, 1);
  assert.equal(failures[0].item.id, 2);
  assert.equal(failures[0].vs.id, 1);
  close(failures[0].need, 1.1 * 500 / 520);
});

test('a big level gap only needs to beat the easier item, not clear it by 10%', () => {
  const old = item(1, { level: 30, classes: 16, mp: 500 });
  const next = item(2, { level: 40, classes: 16, mp: 520 });
  assert.equal(Balance.check([old, next]).failures.length, 0);
});

test('a Warrior item passes on the damage line when the tank line fails', () => {
  const tank = item(1, { level: 40, classes: 8, hp: 300, ac: 200 });
  const damage = item(2, { level: 45, classes: 8, hp: 900, ac: 40 });
  const result = Balance.check([tank, damage]);
  const row = result.rows.filter((r) => r.item.id === 2)[0];
  assert.equal(row.line, 'damage');
  assert.ok(row.ratio >= 1.1);
  assert.equal(result.failures.length, 0);
});

test('Special items are never the bar, and Weak and Exempt items are never checked', () => {
  const doom = item(1, { step: 'XP100M', classes: 48, hp: 4000, mp: 3000, tag: { power: 'Special', source: 'RareBoss' } });
  const gero = item(2, { step: 'XP200M', classes: 48, hp: 1000, mp: 3000, tag: { source: 'RareBoss' } });
  assert.equal(Balance.check([doom, gero]).failures.length, 0);

  const strong = item(3, { level: 10, classes: 16, mp: 900 });
  const weak = item(4, { level: 20, classes: 16, mp: 100, tag: { power: 'Weak', source: 'RareBoss' } });
  const exempt = item(5, { level: 20, classes: 16, mp: 100, tag: { power: 'Exempt', source: 'RareBoss' } });
  assert.equal(Balance.check([strong, weak, exempt]).failures.length, 0);
});

test('group members are alternatives: each class needs one of them to be the upgrade', () => {
  const base = { step: 'Nibbles', classes: 48 };
  const valiant = item(1, Object.assign({}, base, { hp: 400, mp: 1600 }));
  const hpPick = item(2, { step: 'XP20M', classes: 48, hp: 2400, mp: 1000, tag: { group: 'ancient', source: 'RareBoss' } });
  const mpPick = item(3, { step: 'XP20M', classes: 48, hp: 300, mp: 2400, tag: { group: 'ancient', source: 'RareBoss' } });
  assert.equal(Balance.check([valiant, hpPick, mpPick]).failures.length, 0);
});

test('a failing group is fixed through the member that suits the class', () => {
  const old = item(1, { step: 'Nibbles', classes: 12, hp: 1000, ac: 100, dex: 10 });
  const tank = item(2, { step: 'Savage', level: 50, classes: 12, hp: 0, ac: 0, tag: { source: 'RareBoss' } });
  const gold = item(3, { step: 'XP20M', classes: 12, hp: 600, ac: 300, tag: { group: 'pair', source: 'RareBoss' } });
  const dev = item(4, { step: 'XP20M', classes: 12, hp: 950, ac: 60, dex: 40, tag: { group: 'pair', source: 'RareBoss' } });
  const rogueFix = Balance.check([old, tank, gold, dev]).failures.filter((f) => f.cls === 'Rogue');
  assert.equal(rogueFix.length, 1);
  assert.equal(rogueFix[0].item.id, 4);
});

test('every group member must be the upgrade for at least one class', () => {
  const old = item(1, { step: 'Nibbles', classes: 48, hp: 400, mp: 1600 });
  const good = item(2, { step: 'XP20M', classes: 48, hp: 600, mp: 2600, tag: { group: 'g', source: 'RareBoss' } });
  const dud = item(3, { step: 'XP20M', classes: 48, hp: 300, mp: 900, tag: { group: 'g', source: 'RareBoss' } });
  const ids = Balance.check([old, good, dud]).failures.map((f) => f.item.id);
  assert.deepEqual(ids, [3]);
});

test('melee weapons are not checked from 20M, and crafts are not measured against boss drops there', () => {
  const sword = item(1, { slot: 'Weapon', step: 'Savage', classes: 8, wd: 150, str: 20 });
  const axe = item(2, { slot: 'Weapon', step: 'XP20M', classes: 8, wd: 50 });
  assert.equal(Balance.check([sword, axe]).failures.length, 0);

  const boss = item(3, { step: 'XP20M', classes: 16, mp: 3000, tag: { source: 'Crafted' } });
  const craft = item(4, { step: 'XP100M', classes: 16, mp: 1000, tag: { source: 'RareBoss' } });
  assert.equal(Balance.easierThan(boss, craft), false);
});
