import { test } from 'node:test';
import assert from 'node:assert/strict';
import { createContext, runInContext } from 'node:vm';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';

const { Layout } = await import('../src/layout.js');
// The client's own slot -> sprite folder map. A fixed `category` in PART_GRAPHICS has to be one of
// its values: the parts atlas is keyed by those names, so anything else is a preview that is blank
// forever and a browser with no tiles, and nothing on screen would say why.
const { Appearance } = await import('../src/appearance.js');

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

// ---------------------------------------------------------------- TINTS / PART_GRAPHICS

test('the tint table names only columns the sheet really has', () => {
  Object.keys(Layout.TINTS).forEach((name) => {
    const real = sheet(name).columns.map((c) => c.name);
    Object.keys(Layout.TINTS[name]).forEach((graphic) => {
      assert.ok(real.includes(graphic), `${name} tints a column that does not exist: ${graphic}`);
      Layout.TINTS[name][graphic].forEach((source) => {
        assert.ok(real.includes(source),
          `${name}.${graphic} reads a tint column that does not exist: ${source}`);
      });
    });
  });
});

test('no sheet has a tint column the table forgets', () => {
  // The other direction, which is the one a typo hides in: a graphic column whose sheet DOES carry
  // graphic_r/g/b/a but which is absent from TINTS draws plain, and a plain preview looks fine.
  SCHEMA.sheets.forEach((s) => {
    const names = [...s.columns].map((c) => c.name);
    const tintish = names.filter((n) => /_[rgba]$/.test(n));
    if (!tintish.length) return;

    // Only the tint of an item's own graphic is in scope: body_/hair_ tints belong to the
    // character preview, which reads them through Appearance.layers and not through this table.
    const graphicTints = tintish.filter((n) => n.indexOf('graphic_') === 0);
    if (!graphicTints.length) return;

    const table = Layout.TINTS[s.sheet] || {};
    const covered = Object.keys(table);
    assert.ok(covered.length > 0,
      `${s.sheet} has ${graphicTints.join(', ')} but no entry in TINTS — its graphics draw plain`);
    // Every graphic column of that sheet is tinted, not just the first one found.
    const graphics = names.filter((n) => /^graphic(_tile|_equip)?$/.test(n));
    assert.deepEqual([...graphics].sort(), [...covered].sort(),
      `${s.sheet} tints some of its graphics and not others`);
  });
});

test('the body and hair part previews read the same tints the character panel does', () => {
  // Both previews are on screen together — the small one beside body_id, the whole character in the
  // panel — and the panel tints its layers through Appearance.layers, from these same cells. Two
  // pictures of one sprite disagreeing about its colour is the bug the Items tint edit was.
  assert.deepEqual(Layout.tintColumns('NPCs', 'body_id'),
    ['body_r', 'body_g', 'body_b', 'body_a']);
  assert.deepEqual(Layout.tintColumns('NPCs', 'hair_id'),
    ['hair_r', 'hair_g', 'hair_b', 'hair_a']);
  assert.deepEqual(Layout.tintColumns('Spell Effects', 'hair_id'),
    ['hair_r', 'hair_g', 'hair_b', 'hair_a']);
  // NO FACE ENTRY: Character.cs:233 forces NoTint on the eyes layer, so a tint for one is not a
  // cell either sheet has. Appearance.layer's own eyes call passes 0,0,0,0 for the same reason.
  assert.equal(Layout.tintColumns('NPCs', 'face_id'), null);
  assert.equal(Layout.tintColumns('Spell Effects', 'face_id'), null);
});

test('every part-graphic column that has a tint reads all four of its channels', () => {
  // The other direction, per part graphic rather than per sheet: a tinted preview reading three
  // channels and inventing the fourth would draw a colour the game does not.
  Object.keys(Layout.PART_GRAPHICS).forEach((name) => {
    const real = sheet(name).columns.map((c) => c.name);
    Object.keys(Layout.PART_GRAPHICS[name]).forEach((column) => {
      const tint = Layout.tintColumns(name, column);
      if (!tint) return;
      assert.equal(tint.length, 4, `${name}.${column}`);
      tint.forEach((c) => assert.ok(real.includes(c), `${name}.${column} reads a missing ${c}`));
    });
  });
});

test('tintColumns answers per sheet and per column, and null for everything else', () => {
  assert.deepEqual(Layout.tintColumns('Items', 'graphic_tile'),
    ['graphic_r', 'graphic_g', 'graphic_b', 'graphic_a']);
  assert.deepEqual(Layout.tintColumns('Items', 'graphic_equip'),
    ['graphic_r', 'graphic_g', 'graphic_b', 'graphic_a']);
  // Spells has a graphic and no tint columns: the game draws its spellbook icon plain.
  assert.equal(Layout.tintColumns('Spells', 'spellbook_graphic'), null);
  assert.equal(Layout.tintColumns('Spell Effects', 'spell_animation'), null);
  assert.equal(Layout.tintColumns('Items', 'item_name'), null);
  assert.equal(Layout.tintColumns('Quests', 'graphic_tile'), null);
});

test('partGraphic marks the character-part columns and nothing else', () => {
  // The two shapes, which are not interchangeable: an equip graphic's folder comes from a CELL
  // (item_slot decides whether id 5 is a helmet or a boot), an appearance id's is fixed by the
  // column itself.
  assert.deepEqual(Layout.partGraphic('Items', 'graphic_equip'), { categoryFrom: 'item_slot' });
  assert.deepEqual(Layout.partGraphic('NPCs', 'body_id'), { category: 'Bodies' });
  assert.deepEqual(Layout.partGraphic('NPCs', 'hair_id'), { category: 'Hair' });
  // A FACE id draws from Eyes — the one folder whose name does not follow from the column's, which
  // is why it is stated in the table rather than derived anywhere.
  assert.deepEqual(Layout.partGraphic('NPCs', 'face_id'), { category: 'Eyes' });
  // The appearance override carries the same three ids and gets the same three folders.
  assert.deepEqual(Layout.partGraphic('Spell Effects', 'body_id'), { category: 'Bodies' });
  assert.deepEqual(Layout.partGraphic('Spell Effects', 'face_id'), { category: 'Eyes' });

  // graphic_tile is an inventory ICON, drawn from the icons bundle, not a character part.
  assert.equal(Layout.partGraphic('Items', 'graphic_tile'), null);
  assert.equal(Layout.partGraphic('NPCs', 'graphic_equip'), null);
  // NPCs has body_state, and it is a POSE rather than a graphic id — a picker for it would be a
  // browser over sprites the cell does not name.
  assert.equal(Layout.partGraphic('NPCs', 'body_state'), null);
});

test('every part-graphic spec names real columns and a real sprite folder', () => {
  const folders = Object.keys(Appearance.CATEGORY).map((slot) => Appearance.CATEGORY[slot]);
  let fixed = 0;
  let derived = 0;

  Object.keys(Layout.PART_GRAPHICS).forEach((name) => {
    const real = sheet(name).columns.map((c) => c.name);
    Object.keys(Layout.PART_GRAPHICS[name]).forEach((column) => {
      assert.ok(real.includes(column), `${name} has no column ${column}`);
      const spec = Layout.PART_GRAPHICS[name][column];

      // EXACTLY ONE of the two shapes. A spec with both would leave every consumer free to pick,
      // and a spec with neither resolves no folder at all — which reads as "the bundle is missing"
      // rather than as a table entry nobody finished.
      const has = ['category', 'categoryFrom'].filter((k) => spec[k] !== undefined);
      assert.deepEqual(has.length, 1,
        `${name}.${column} must state either category or categoryFrom, not ${has.join(' + ')}`);

      if (spec.categoryFrom !== undefined) {
        derived++;
        assert.ok(real.includes(spec.categoryFrom),
          `${name}.${column} reads a missing column: ${spec.categoryFrom}`);
      } else {
        fixed++;
        assert.ok(folders.includes(spec.category),
          `${name}.${column} names a folder the parts atlas does not have: ${spec.category}`);
      }
    });
  });

  // Both shapes are actually exercised by the shipped table, so neither branch above is dead.
  assert.equal(derived, 1);
  assert.equal(fixed, 6);
});

test('a part graphic is claimed by no composite, which is why it needs its own route', () => {
  // If a Graphic composite ever claimed graphic_equip, forms.js would render it through
  // Composites.control and partControl would never be reached — silently.
  Object.keys(Layout.PART_GRAPHICS).forEach((name) => {
    const claimed = [...(sheet(name).composites || [])].flatMap((c) => [...c.columns]);
    Object.keys(Layout.PART_GRAPHICS[name]).forEach((column) => {
      assert.ok(!claimed.includes(column), `${name}.${column} is claimed by a composite`);
    });
  });
});

test('the new tables cannot be mutated by a consumer either', () => {
  assert.throws(() => { Layout.TINTS.Items = {}; }, TypeError);
  assert.throws(() => Layout.TINTS.Items.graphic_tile.push('x'), TypeError);
  assert.throws(() => { Layout.PART_GRAPHICS.Items.graphic_equip.categoryFrom = 'x'; }, TypeError);
  assert.throws(() => { Layout.GALLERIES['Spell Effects'].spell_animation = 'x'; }, TypeError);
  assert.throws(() => { Layout.WEARABLE.Items.column = 'x'; }, TypeError);
});

// ---------------------------------------------------------------- WEARABLE

test('wearableGate answers for Items and nothing else', () => {
  assert.deepEqual(Layout.wearableGate('Items'),
    { column: 'item_usetype', values: ['Armor', 'Weapon'],
      columns: ['graphic_equip', 'item_slot'] });
  assert.equal(Layout.wearableGate('NPCs'), null);
  assert.equal(Layout.wearableGate('constructor'), null);
});

test('every wearable gate names real columns and real enum values', () => {
  // A typo'd name here fails silently in both consumers: forms.js gates no row and app.js
  // never draws (or always draws) the worn canvas, and nothing on screen says why.
  Object.keys(Layout.WEARABLE).forEach((name) => {
    const columns = sheet(name).columns;
    const real = columns.map((c) => c.name);
    const gate = Layout.WEARABLE[name];
    assert.ok(real.includes(gate.column), `${name} has no column ${gate.column}`);
    gate.columns.forEach((c) => assert.ok(real.includes(c), `${name} has no column ${c}`));
    const enumNames = columns.find((c) => c.name === gate.column).enumNames;
    gate.values.forEach((v) =>
      assert.ok(enumNames.includes(v), `${name}.${gate.column} has no value ${v}`));
  });
});

// ---------------------------------------------------------------- MONSTER_BODY

test('monsterBodyGate answers for NPCs and nothing else', () => {
  assert.deepEqual(Layout.monsterBodyGate('NPCs'), {
    column: 'body_id', from: 100,
    columns: ['face_id', 'hair_id', 'hair_r', 'equipped_items'],
    clear: ['face_id', 'hair_id', 'equipped_items'],
  });
  // Spell Effects carries body_id/face_id/hair_id too, but as an OVERRIDE with no equipment of its
  // own and a default of 0 — there is no "this row is a monster" state to gate on.
  assert.equal(Layout.monsterBodyGate('Spell Effects'), null);
  assert.equal(Layout.monsterBodyGate('Items'), null);
  assert.equal(Layout.monsterBodyGate('constructor'), null);
});

test('every monster-body gate names real columns, and clears only ones it hides', () => {
  Object.keys(Layout.MONSTER_BODY).forEach((name) => {
    const real = sheet(name).columns.map((c) => c.name);
    const gate = Layout.MONSTER_BODY[name];
    assert.ok(real.includes(gate.column), `${name} has no column ${gate.column}`);
    gate.columns.forEach((c) => assert.ok(real.includes(c), `${name} has no column ${c}`));
    // A cell cleared on save whose row is still on screen would be a field that empties itself
    // under the user with no explanation.
    gate.clear.forEach((c) => assert.ok(gate.columns.includes(c),
      `${name} clears ${c} without hiding it`));
  });
});

test('the gate does not hide the cell it reads, or the body tint that survives', () => {
  // body_id itself must stay editable — it is the way back out — and the body's OWN tint is applied
  // to a monster body just as to a player one (Character.cs zeroes the ids, not the body colour).
  const gate = Layout.monsterBodyGate('NPCs');
  ['body_id', 'body_state', 'body_r', 'body_g', 'body_b', 'body_a']
    .forEach((c) => assert.ok(!gate.columns.includes(c), `${c} must stay visible`));
});

test('isMonsterBody is the client\'s own >= 100 test, on a spreadsheet cell', () => {
  const at = (body) => Layout.isMonsterBody('NPCs', { body_id: body });

  // 100 ITSELF IS A MONSTER. Character.cs:218 is `>= 100` and Appearance.layers ports it verbatim,
  // so a form that gated on > 100 would show the face and hair rows for a body whose preview draws
  // neither.
  assert.equal(at(100), true);
  assert.equal(at(99), false);
  assert.equal(at(10113), true);

  // A CELL, so a string is the normal case, and parseInt is what Appearance.num uses.
  assert.equal(at('100'), true);
  assert.equal(at(' 150 '), true);
  assert.equal(at('99'), false);

  // Blank means "use the SQL default", which for both sheets carrying body_id is a player body.
  // Reading it as a monster would hide the face and hair of every half-filled row.
  [undefined, null, '', ' ', 'nonsense'].forEach((v) =>
    assert.equal(at(v), false, JSON.stringify(v)));

  // A sheet with no rule is never a monster, so a caller needs no branch of its own first.
  assert.equal(Layout.isMonsterBody('Items', { body_id: 150 }), false);
  assert.equal(Layout.isMonsterBody('NPCs', undefined), false);
});

test('the monster-body table cannot be mutated by a consumer', () => {
  assert.throws(() => { Layout.MONSTER_BODY.NPCs.from = 0; }, TypeError);
  assert.throws(() => Layout.MONSTER_BODY.NPCs.clear.push('body_id'), TypeError);
});

// ---------------------------------------------------------------- GALLERIES

test('galleryBundle answers icons for every graphic column but the animation ones', () => {
  assert.equal(Layout.galleryBundle('Spell Effects', 'spell_animation'), 'effects');
  // The buff graphic on the SAME sheet is an inventory icon, so the table is per column and not
  // per sheet.
  assert.equal(Layout.galleryBundle('Spell Effects', 'buff_graphic'), 'icons');
  assert.equal(Layout.galleryBundle('Items', 'graphic_tile'), 'icons');
  assert.equal(Layout.galleryBundle('Spells', 'spellbook_graphic'), 'icons');
  // Never null: callers need no fallback of their own, so none of them can disagree about it.
  assert.equal(Layout.galleryBundle('No Such Sheet', 'no_such_column'), 'icons');
});

test('every gallery entry names a real column that a Graphic composite leads', () => {
  Object.keys(Layout.GALLERIES).forEach((name) => {
    const real = sheet(name).columns.map((c) => c.name);
    Object.keys(Layout.GALLERIES[name]).forEach((column) => {
      assert.ok(real.includes(column), `${name} has no column ${column}`);
      // The entry is read by Composites.control for a Graphic composite's LEADER. Named on any
      // other column it would be a table nothing consults, which no other test could see.
      const led = [...(sheet(name).composites || [])]
        .filter((c) => c.kind === 'Graphic' && c.columns[0] === column);
      assert.equal(led.length, 1, `${name}.${column} leads no Graphic composite`);
    });
  });
});

test('every bundle a gallery entry names is one the editor actually ships', () => {
  const bundles = ['icons', 'parts', 'effects'];
  Object.keys(Layout.GALLERIES).forEach((name) => {
    Object.keys(Layout.GALLERIES[name]).forEach((column) => {
      assert.ok(bundles.includes(Layout.GALLERIES[name][column]),
        `${name}.${column} names a bundle that does not exist`);
    });
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

    assert.ok(label && label.trim() === label,
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

test('the three shipped tint prefixes read as tints, not as red channels', () => {
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
    'graphic_file'), 'graphic_file');
  assert.equal(Layout.labelFor({ kind: 'IdList', columns: ['quest_ids'] }, 'quest_ids'),
    'quest_ids');
});

test('an unknown kind is labelled after its leader rather than guessed at', () => {
  assert.equal(Layout.labelFor({ kind: 'Fake', columns: ['a', 'b'] }, 'a'), 'a');
});

// ------------------------------------------------------------------ GROUP_PARENT

test('every grouped sheet names a column that sheet really has', () => {
  Object.keys(Layout.GROUP_PARENT).forEach((name) => {
    const names = sheet(name).columns.map((c) => c.name);
    assert.ok(names.includes(Layout.GROUP_PARENT[name]),
      name + ' has no column ' + Layout.GROUP_PARENT[name]);
  });
});

test('every parent column is a foreign key, so the parent sheet is derivable', () => {
  // The table names only the COLUMN. The parent SHEET comes from that column's ref in the
  // schema, so the two cannot drift — but only if every entry actually has a ref.
  Object.keys(Layout.GROUP_PARENT).forEach((name) => {
    const column = sheet(name).columns
      .filter((c) => c.name === Layout.GROUP_PARENT[name])[0];
    assert.ok(column, name + ' has no column ' + Layout.GROUP_PARENT[name]);
    assert.ok(column.ref, name + '.' + column.name + ' has no ref');
  });
});

test('groupParent answers null for a sheet that is not grouped', () => {
  assert.equal(Layout.groupParent('Items'), null);
  assert.equal(Layout.groupParent('NPCs'), null);
  // Class Info is class_id + level, 26 columns wide and ~99 rows per class. Deliberately out:
  // that shape is not what the inline table is for.
  assert.equal(Layout.groupParent('Class Info'), null);
});

test('groupParent answers the parent column for a grouped sheet', () => {
  assert.equal(Layout.groupParent('NPC Drops'), 'npc_template_id');
  // Spawns group by MAP, not by NPC: spawns are authored a zone at a time, and it gives more
  // evenly sized groups than 4,322 rows split across every NPC.
  assert.equal(Layout.groupParent('NPC Spawns'), 'map_id');
  // Warptiles refs Maps twice; map_id is where the tile IS, not where it goes.
  assert.equal(Layout.groupParent('Warptiles'), 'map_id');
});

test('GROUP_PARENT is frozen', () => {
  assert.ok(Object.isFrozen(Layout.GROUP_PARENT));
});

test('no grouped sheet has a composite', () => {
  // The group table builds every cell with Forms.columnControl, which routes a composite's
  // columns nowhere. If a grouped sheet ever gains one, the table would silently render its
  // columns as bare text boxes — so this fails first instead.
  Object.keys(Layout.GROUP_PARENT).forEach((name) => {
    // Length, not deepEqual: SCHEMA is built in a vm context, so its arrays are cross-realm and
    // deepStrictEqual would reject them on prototype identity alone, empty or not.
    assert.equal((sheet(name).composites || []).length, 0, name + ' gained a composite');
  });
});

test('no grouped sheet has a part graphic', () => {
  // Same reasoning: partControl needs a canvas and the parts atlas, neither of which a table
  // cell has room for.
  Object.keys(Layout.GROUP_PARENT).forEach((name) => {
    sheet(name).columns.forEach((c) => {
      assert.equal(Layout.partGraphic(name, c.name), null, name + '.' + c.name);
    });
  });
});
