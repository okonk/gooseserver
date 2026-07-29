import { test } from 'node:test';
import assert from 'node:assert/strict';
import { createContext, runInContext } from 'node:vm';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';

const { Layout } = await import('../src/layout.js');

// The authoritative column list, loaded the same way Task 5 loads a sprite bundle.
const schemaPath = fileURLToPath(new URL('../schema.js', import.meta.url));
const schemaContext = createContext({});
runInContext(readFileSync(schemaPath, 'utf8'), schemaContext);
const SCHEMA = schemaContext.GOOSE_SCHEMA;

function sheet(name) {
  const s = SCHEMA.sheets.find((x) => x.sheet === name);
  assert.ok(s, `schema.js has no sheet named ${JSON.stringify(name)}`);
  return s;
}

test('sheets without a layout fall back to one generic group', () => {
  const groups = Layout.groupsFor('NPC Drops',
    [{ name: 'npc_template_id' }, { name: 'item_template_id' }]);
  assert.equal(groups.length, 1);
  assert.equal(groups[0].title, 'Fields');
  assert.deepEqual(groups[0].columns, ['npc_template_id', 'item_template_id']);
});

test('Items has the designed groups', () => {
  const groups = Layout.groupsFor('Items', []);
  assert.deepEqual(groups.map((g) => g.title), [
    'Identity', 'Graphics', 'Requirements', 'Stats', 'Weapon', 'Flags', 'Value', 'Effects',
    'Scripting',
  ]);
});

test('every laid-out column is assigned exactly once', () => {
  const columns = [
    { name: 'item_template_id' }, { name: 'item_name' }, { name: 'stat_str' },
    { name: 'lore' }, { name: 'script_path' },
  ];
  const groups = Layout.groupsFor('Items', columns);
  const placed = groups.flatMap((g) => g.columns);
  assert.equal(new Set(placed).size, placed.length);
});

test('groups keep the layout order, not the caller order', () => {
  const groups = Layout.groupsFor('Items',
    [{ name: 'item_name' }, { name: 'item_template_id' }]);
  assert.deepEqual(groups[0].columns, ['item_template_id', 'item_name']);
});

test('columns missing from the layout land in an overflow group', () => {
  const groups = Layout.groupsFor('Items', [{ name: 'brand_new_column' }]);
  const other = groups.find((g) => g.title === 'Other');
  assert.ok(other, 'unlisted columns must still be editable');
  assert.deepEqual(other.columns, ['brand_new_column']);
  // Last, so an unrecognised column never pushes the designed groups down the form.
  assert.equal(groups[groups.length - 1], other);
});

test('no Other group when every column is laid out', () => {
  const groups = Layout.groupsFor('Items', [{ name: 'item_name' }]);
  assert.equal(groups.find((g) => g.title === 'Other'), undefined);
});

test('restart-only sheets are flagged', () => {
  // ReloadSQLCommandEvent.cs:33-41 — these loaders are commented out.
  assert.equal(Layout.needsRestart('Maps'), true);
  assert.equal(Layout.needsRestart('Classes'), true);
  assert.equal(Layout.needsRestart('Class Info'), true);
  assert.equal(Layout.needsRestart('Combinations'), true);
  assert.equal(Layout.needsRestart('Combination Item Required'), true);
  assert.equal(Layout.needsRestart('Combination Item Result'), true);
  assert.equal(Layout.needsRestart('Items'), false);
  assert.equal(Layout.needsRestart('NPCs'), false);
});

test('the sheets the commented-out loaders reach are all restart-only', () => {
  // ClassHandler.LoadClasses reads classes, class_info AND classes_levelup_spells
  // (ClassHandler.cs:47,66,117); MapHandler.LoadMaps reads maps then Map.LoadData, which reads
  // warptiles and map_required_items (MapHandler.cs:41,78 / Map.cs:488,507);
  // NPCHandler.LoadNPCs reads npc_spawns (NPCHandler.cs:263). Titles and surnames are only ever
  // loaded from GameWorld startup (GameWorld.cs:276,289), never from /reloadsql.
  ['Class Levelup Spells', 'Warptiles', 'Map Required Items', 'NPC Spawns', 'Titles', 'Surnames']
    .forEach((s) => assert.equal(Layout.needsRestart(s), true, s));
});

test('sheets the live loaders do reach are not flagged', () => {
  // npc_drops and npc_vendor_items are read inside LoadNPCTemplates (NPCHandler.cs:154,172), and
  // quest requirements/rewards inside LoadQuests (QuestHandler.cs:40,61) — all reloadable.
  ['Spells', 'Spell Effects', 'Quests', 'Quest Reqs', 'Quest Rewards', 'NPC Drops',
    'NPC Vendor Items'].forEach((s) => assert.equal(Layout.needsRestart(s), false, s));
});

test('every schema sheet is classified, and RESTART_ONLY names real sheets', () => {
  const names = SCHEMA.sheets.map((s) => s.sheet);
  Layout.RESTART_ONLY.forEach((s) => assert.ok(names.includes(s), `not a real sheet: ${s}`));
  assert.equal(new Set(Layout.RESTART_ONLY).size, Layout.RESTART_ONLY.length);
  // The full partition of the 21 sheets, so neither a missing nor a spurious entry can hide.
  assert.deepEqual([...Layout.RESTART_ONLY].sort(), [
    'Class Info', 'Class Levelup Spells', 'Classes', 'Combination Item Required',
    'Combination Item Result', 'Combinations', 'Map Required Items', 'Maps', 'NPC Spawns',
    'Surnames', 'Titles', 'Warptiles',
  ]);
  // Spread: arrays built inside the vm context have that context's Array.prototype, which strict
  // deepEqual rejects against a host-realm literal.
  assert.deepEqual([...names.filter((s) => !Layout.needsRestart(s))].sort(), [
    'Items', 'NPC Drops', 'NPC Vendor Items', 'NPCs', 'Quest Reqs', 'Quest Rewards', 'Quests',
    'Spell Effects', 'Spells',
  ]);
});

test('the exported tables cannot be mutated by a consumer', () => {
  // Frozen, so one consumer's stray write can change neither needsRestart nor what the next
  // consumer reads. Strict mode makes the write throw.
  assert.throws(() => Layout.RESTART_ONLY.push('Items'), TypeError);
  assert.equal(Layout.needsRestart('Items'), false);
  assert.throws(() => { Layout.LAYOUTS.Items = []; }, TypeError);
  assert.throws(() => Layout.LAYOUTS.Items.push({ title: 'x', columns: [] }), TypeError);
  assert.throws(() => Layout.LAYOUTS.Items[0].columns.push('x'), TypeError);
});

test('each wide sheet keeps its own designed groups', () => {
  // Pins the sheet keys too: a mis-keyed layout falls back to one flat "Fields" group.
  assert.deepEqual(Layout.groupsFor('Spells', []).map((g) => g.title),
    ['Identity', 'Graphics', 'Target', 'Restrictions', 'Costs', 'Effect']);
  assert.deepEqual(Layout.groupsFor('NPCs', []).map((g) => g.title),
    ['Identity', 'Appearance', 'Combat', 'Behaviour', 'Regen', 'Links', 'Scripting']);
  assert.deepEqual(Layout.groupsFor('Spell Effects', []).map((g) => g.title),
    ['Identity', 'Graphics', 'Targeting', 'Damage', 'Stat modifiers', 'Regen modifiers',
      'Combat modifiers', 'Appearance override', 'Buff', 'Teleport', 'Chained effects',
      'Scripting']);
});

// Every name the table itself lists for a sheet, in table order — read from LAYOUTS, NOT from
// groupsFor, which filters unknown names out and so can only ever see the schema->layout
// direction.
function laidOut(name) {
  return Layout.LAYOUTS[name].flatMap((g) => g.columns);
}

test('the layout table names no column that does not exist', () => {
  Object.keys(Layout.LAYOUTS).forEach((name) => {
    const real = sheet(name).columns.map((c) => c.name);
    const phantom = laidOut(name).filter((n) => !real.includes(n));
    assert.deepEqual(phantom, [],
      `${name} lays out columns that schema.js does not have: ${phantom.join(', ')}`);
  });
});

test('the layout table leaves no real column unmentioned', () => {
  Object.keys(Layout.LAYOUTS).forEach((name) => {
    const placed = laidOut(name);
    // Spread: schema arrays come from the vm realm, whose Array.prototype strict deepEqual
    // rejects against a host-realm literal.
    const missing = [...sheet(name).columns].map((c) => c.name).filter((n) => !placed.includes(n));
    assert.deepEqual(missing, [],
      `${name} would drop these into "Other": ${missing.join(', ')}`);
  });
});

test('no column is laid out twice', () => {
  Object.keys(Layout.LAYOUTS).forEach((name) => {
    const placed = laidOut(name);
    assert.equal(new Set(placed).size, placed.length, `${name} lists a column in two groups`);
  });
});

test('a real sheet therefore needs no Other group', () => {
  // The consequence the form builder depends on, asserted end to end through groupsFor.
  Object.keys(Layout.LAYOUTS).forEach((name) => {
    const groups = Layout.groupsFor(name, sheet(name).columns);
    assert.equal(groups.find((g) => g.title === 'Other'), undefined, name);
    assert.equal(groups.flatMap((g) => g.columns).length, sheet(name).columns.length, name);
  });
});

// ---------------------------------------------------------------- labelFor

// Every composite in schema.js, whatever kind, whatever sheet.
const ALL_COMPOSITES = SCHEMA.sheets.flatMap((s) =>
  [...(s.composites || [])].map((c) => ({ sheet: s.sheet, comp: c })));

test('every composite in schema.js gets an honest label', () => {
  assert.ok(ALL_COMPOSITES.length > 0, 'schema.js declares no composites at all');

  ALL_COMPOSITES.forEach(({ sheet: name, comp }) => {
    const columns = [...comp.columns];
    const label = Layout.labelFor(comp, columns[0]);
    const where = `${name} ${comp.kind} [${columns.join(', ')}]`;

    assert.ok(label && label.trim() === label && label !== '',
      `${where} produced a blank or padded label: ${JSON.stringify(label)}`);

    // Every word that looks like a column name must BE one — the label may not invent a cell.
    const known = new Set([...sheet(name).columns].map((c) => c.name));
    label.split(' ').forEach((word) => {
      if (word.indexOf('_') === -1) return;
      assert.ok(known.has(word), `${where} names a column that does not exist: ${word}`);
    });

    if (comp.kind === 'Rgba') {
      // The bug: the tint was labelled after its RED CHANNEL.
      assert.ok(!/_[rgba]\b/.test(label), `${where} is still labelled after a channel: ${label}`);
      assert.match(label, /^\S+ tint$/, where);
    } else if (comp.kind === 'Graphic') {
      assert.equal(label, columns[0] + ' + sheet', where);
    } else {
      assert.equal(columns.length, 1, `${where} claims more than one column`);
      assert.equal(label, columns[0], where);
    }
  });
});

test('the four shipped tints read as tints, not as red channels', () => {
  const label = (kind, columns) => Layout.labelFor({ kind, columns }, columns[0]);
  assert.equal(label('Rgba', ['graphic_r', 'graphic_g', 'graphic_b', 'graphic_a']),
    'graphic tint');
  assert.equal(label('Rgba', ['body_r', 'body_g', 'body_b', 'body_a']), 'body tint');
  assert.equal(label('Rgba', ['hair_r', 'hair_g', 'hair_b', 'hair_a']), 'hair tint');
});

test('a tint whose columns share no prefix keeps the leader name', () => {
  // Nothing may produce " tint" with an empty prefix in front of it.
  assert.equal(Layout.labelFor({ kind: 'Rgba', columns: ['r', 'g', 'b', 'a'] }, 'r'), 'r');
  assert.equal(Layout.labelFor({ kind: 'Rgba', columns: ['red_x', 'green_y'] }, 'red_x'),
    'red_x');
  // A coincidental shared letter is not a field name either.
  assert.equal(Layout.labelFor({ kind: 'Rgba', columns: ['red', 'reg', 'reb'] }, 'red'), 'red');
});

test('labelFor falls back to the leader the form actually rendered', () => {
  // forms.js elects the first SCHEMA-PRESENT column as leader, which is not always columns[0].
  assert.equal(Layout.labelFor({ kind: 'Graphic', columns: ['ghost', 'graphic_file'] },
    'graphic_file'), 'graphic_file + sheet');
  assert.equal(Layout.labelFor({ kind: 'IdList', columns: ['quest_ids'] }, 'quest_ids'),
    'quest_ids');
});

test('an unknown kind is labelled after its leader rather than guessed at', () => {
  assert.equal(Layout.labelFor({ kind: 'Fake', columns: ['a', 'b'] }, 'a'), 'a');
});
