// Presentation-only: field grouping and order for the four wide sheets, plus which sheets a
// designer cannot publish live. Kept out of the C# descriptors deliberately — the descriptors
// describe the data, this describes the form.
//
// Every name below is cross-checked against schema.js by layout.test.js, in both directions: a
// typo'd name would silently drop its column into "Other", and a new descriptor column would
// silently arrive there too. Neither is allowed to pass.
var Layout = (function () {
  var LAYOUTS = {
    Items: [
      { title: 'Identity', columns: ['item_template_id', 'item_name', 'item_description',
          'item_usetype', 'item_slot', 'item_type', 'stack_size'] },
      { title: 'Graphics', columns: ['graphic_tile', 'graphic_file', 'graphic_equip',
          'graphic_r', 'graphic_g', 'graphic_b', 'graphic_a'] },
      { title: 'Requirements', columns: ['min_level', 'max_level', 'min_experience',
          'max_experience', 'class_restrictions'] },
      { title: 'Stats', columns: ['player_hp', 'player_mp', 'player_sp', 'stat_ac', 'stat_str',
          'stat_sta', 'stat_dex', 'stat_int', 'res_fire', 'res_water', 'res_spirit', 'res_air',
          'res_earth'] },
      { title: 'Weapon', columns: ['weapon_damage', 'weapon_delay', 'body_state'] },
      { title: 'Flags', columns: ['lore', 'bindonpickup', 'bindonequip', 'event'] },
      { title: 'Value', columns: ['item_value', 'credits_value'] },
      { title: 'Effects', columns: ['spell_effect_id', 'spell_effect_chance', 'learn_spell_id'] },
      { title: 'Scripting', columns: ['script_path', 'script_params'] },
    ],
    Spells: [
      { title: 'Identity', columns: ['spell_id', 'spell_name', 'spell_description'] },
      { title: 'Graphics', columns: ['spellbook_graphic', 'spellbook_graphic_file'] },
      { title: 'Target', columns: ['spell_target', 'spell_aether'] },
      { title: 'Restrictions', columns: ['class_restrictions'] },
      { title: 'Costs', columns: ['hp_static_cost', 'hp_percent_cost', 'mp_static_cost',
          'mp_percent_cost', 'sp_static_cost', 'sp_percent_cost'] },
      { title: 'Effect', columns: ['spell_effect_id'] },
    ],
    NPCs: [
      { title: 'Identity', columns: ['npc_id', 'npc_name', 'npc_title', 'npc_surname',
          'npc_type', 'npc_level', 'npc_alliance'] },
      { title: 'Appearance', columns: ['body_state', 'body_id', 'body_r', 'body_g', 'body_b',
          'body_a', 'face_id', 'hair_id', 'hair_r', 'hair_g', 'hair_b', 'hair_a',
          'equipped_items'] },
      { title: 'Combat', columns: ['npc_hp', 'npc_mp', 'npc_sp', 'stat_ac', 'stat_str',
          'stat_sta', 'stat_dex', 'stat_int', 'res_fire', 'res_water', 'res_spirit', 'res_air',
          'res_earth', 'weapon_damage', 'armor_pierce', 'attack_range', 'attack_speed'] },
      { title: 'Behaviour', columns: ['npc_facing', 'aggro_range', 'move_speed', 'stationary',
          'stunnable', 'rootable', 'slowable', 'invincible', 'stuck_behaviour', 'stuck_timeout',
          'respawn_time'] },
      { title: 'Regen', columns: ['hp_percent_regen', 'hp_static_regen', 'mp_percent_regen',
          'mp_static_regen'] },
      // experience is the XP reward for killing this NPC, not a combat stat of its own.
      { title: 'Links', columns: ['class_id', 'quest_ids', 'credit_dealer', 'experience'] },
      { title: 'Scripting', columns: ['script_path', 'script_params'] },
    ],
    'Spell Effects': [
      { title: 'Identity', columns: ['spell_effect_id', 'spell_effect_name', 'effect_type',
          'effect_duration'] },
      { title: 'Graphics', columns: ['spell_animation', 'spell_animation_file', 'spell_display',
          'buff_graphic', 'buff_graphic_file', 'do_attack_animation', 'do_cast_animation'] },
      { title: 'Targeting', columns: ['target_type', 'target_size', 'spell_effected',
          'min_level_effected', 'max_level_effected', 'only_hits_one_npc'] },
      { title: 'Damage', columns: ['spell_energy_type', 'spell_damage_effects',
          'hp_change_formula', 'mp_change_formula', 'sp_change_formula'] },
      // Split three ways rather than one 25-column block. A heading that long is a wall of
      // fields, which is the problem this whole table exists to solve.
      { title: 'Stat modifiers', columns: ['hp', 'mp', 'sp', 'stat_ac', 'stat_str', 'stat_sta',
          'stat_dex', 'stat_int', 'res_fire', 'res_water', 'res_spirit', 'res_air',
          'res_earth'] },
      { title: 'Regen modifiers', columns: ['hp_percent_regen', 'hp_static_regen',
          'mp_percent_regen', 'mp_static_regen'] },
      { title: 'Combat modifiers', columns: ['haste', 'spell_damage', 'spell_crit',
          'melee_damage', 'melee_crit', 'damage_reduce', 'move_speed', 'snare_percent'] },
      { title: 'Appearance override', columns: ['body_id', 'body_r', 'body_g', 'body_b',
          'body_a', 'face_id', 'hair_id', 'hair_r', 'hair_g', 'hair_b', 'hair_a'] },
      { title: 'Buff', columns: ['buff_removable', 'buff_doesnt_stack_over', 'buff_stacks_over',
          'oneffect_text', 'offeffect_text', 'taunt_aggro', 'works_in_pvp', 'works_not_in_pvp',
          'random_join_chance'] },
      { title: 'Teleport', columns: ['teleport_map', 'teleport_x', 'teleport_y'] },
      { title: 'Chained effects', columns: ['on_hit_spell_effect_id', 'on_hit_spell_chance',
          'on_attack_spell_effect_id', 'on_attack_spell_chance'] },
      { title: 'Scripting', columns: ['script_path', 'script_params'] },
    ],
  };

  // WHICH COLUMNS TINT WHICH GRAPHIC. The game applies an item's graphic_r/g/b/a to BOTH the
  // inventory tile and the worn sprite, so both of Items' graphics take the same four columns —
  // which is why a tint edit had to visibly change nothing in the editor to be a bug worth fixing.
  //
  // ONLY ITEMS, and that is the whole table rather than an omission. Spells' spellbook_graphic and
  // Spell Effects' four graphics have NO tint columns in their sheets at all, so there is nothing
  // to read; inventing a source for them would tint a preview the game draws plain, which is worse
  // than the untinted preview it replaced. layout.test.js asserts every name here exists in that
  // sheet's schema, and — for the tint columns — that the sheet has no tint column this table
  // leaves out.
  //
  // THE BODY AND HAIR TINTS ARE HERE FOR THE SAME REASON, one level along: body_id and hair_id now
  // carry a part preview of their own (see PART_GRAPHICS), and the character panel beside it draws
  // both layers tinted — through Appearance.layers, which reads those cells itself. Two previews of
  // one sprite disagreeing about its colour is exactly the bug the Items tint edit was, so the small
  // one reads the same four cells. There is no face entry: Character.cs:233 forces NoTint on the
  // eyes layer, so a tint for one is not a cell the game has.
  var TINTS = {
    Items: {
      graphic_tile: ['graphic_r', 'graphic_g', 'graphic_b', 'graphic_a'],
      graphic_equip: ['graphic_r', 'graphic_g', 'graphic_b', 'graphic_a'],
    },
    NPCs: {
      body_id: ['body_r', 'body_g', 'body_b', 'body_a'],
      hair_id: ['hair_r', 'hair_g', 'hair_b', 'hair_a'],
    },
    'Spell Effects': {
      body_id: ['body_r', 'body_g', 'body_b', 'body_a'],
      hair_id: ['hair_r', 'hair_g', 'hair_b', 'hair_a'],
    },
  };

  // Columns holding a CHARACTER PART graphic rather than an inventory icon, and where the sprite
  // folder for one comes from. Such a column is a plain Int with no composite, so without an entry
  // here forms.js renders it as a text box with no preview and no way to browse the art — which is
  // the complaint, first for graphic_equip and now for the three appearance ids.
  //
  // TWO SHAPES, and which one a column takes is a fact about the column rather than a style choice:
  //
  //   `categoryFrom` — the folder comes from ANOTHER CELL. graphic_equip is a helmet or a pair of
  //     boots depending on item_slot, so the same id resolves differently row by row and the folder
  //     has to be read live. The mapping from that cell's value to a folder is a CLIENT fact and
  //     lives in appearance.js (slotFor -> CATEGORY), not here.
  //   `category` — the folder is FIXED by the column itself. body_id is always a body, hair_id
  //     always hair, face_id always eyes: Appearance.layers pushes them into the Body, Hair and Eyes
  //     slots unconditionally (Character.cs:218-233), so there is no cell to read and nothing about
  //     the row can move them. The names are Appearance.CATEGORY's own values, which is what the
  //     parts atlas is keyed by — Eyes for a FACE id being the one that does not read off the column
  //     name, and exactly why it is stated once here rather than derived per consumer.
  //
  // Every column below is checked against the schema by layout.test.js, and every `category` against
  // Appearance.CATEGORY's values — a folder that is not one of the eight the bundle holds would be a
  // preview that is blank forever and a browser with no tiles.
  var PART_GRAPHICS = {
    Items: {
      graphic_equip: { categoryFrom: 'item_slot' },
    },
    NPCs: {
      body_id: { category: 'Bodies' },
      hair_id: { category: 'Hair' },
      face_id: { category: 'Eyes' },
    },
    // The appearance OVERRIDE, which is the same three ids applied to whoever the effect hits — the
    // client feeds them through the same ApplyAppearance path, so the same three folders answer.
    'Spell Effects': {
      body_id: { category: 'Bodies' },
      hair_id: { category: 'Hair' },
      face_id: { category: 'Eyes' },
    },
  };

  // FIELDS ONLY A WEARABLE ITEM HAS A USE FOR, and the cell that decides. Only Armor and Weapon
  // items are ever drawn on a character, so for every other usetype graphic_equip and item_slot
  // are noise — the form hides those rows and the preview panel skips the worn-character canvas.
  // HIDDEN, NOT CLEARED: the cells keep whatever they store and round-trip verbatim through
  // Forms.collect, honouring the rule that opening a record must not change it. The values are
  // enum NAMES, which is what an Enum cell holds and what Forms.collect reads back.
  var WEARABLE = {
    Items: { column: 'item_usetype', values: ['Armor', 'Weapon'],
             columns: ['graphic_equip', 'item_slot'] },
  };

  // A MONSTER OR MORPH BODY RENDERS ALONE, and this is the cell that decides plus everything that
  // decision makes dead. Character.cs:218-223 drops the hair id, the face id and all six equipment
  // slots for a body >= 100, and the server does not even send equipment for such a row
  // (Goose/Packets.cs:161) — so those cells are not "usually ignored", they are unreachable.
  //
  // >= 100, NOT > 100. The client's test is `CurrentBodyID >= 100` and Appearance.layers already
  // ports it verbatim, so anything else here would put the form and the preview beside it into
  // disagreement about body 100 itself.
  //
  // TWO LISTS, because hiding and clearing are different promises:
  //   `columns` are HIDDEN, the way the wearable gate hides its rows — the cells keep their stored
  //     text and round-trip verbatim, so typing 150 into body_id and thinking better of it loses
  //     nothing. hair_r leads the hair tint composite, which is as dead as the id it colours.
  //   `clear` is what a SAVE writes to zero once the row has actually crossed into monster
  //     territory, which the user asked for and which the hidden rows cannot do for themselves:
  //     leaving a face and a hair id behind on a body that will never draw them is data that reads
  //     as equipment the NPC has. body_r/g/b/a are NOT cleared — the body's own tint survives the
  //     client's rule (only the ids are zeroed) — and neither is the hair tint, which is a parked
  //     colour rather than a thing that renders.
  var MONSTER_BODY = {
    NPCs: {
      column: 'body_id', from: 100,
      columns: ['face_id', 'hair_id', 'hair_r', 'equipped_items'],
      clear: ['face_id', 'hair_id', 'equipped_items'],
    },
  };

  // WHICH SPRITE BUNDLE A GRAPHIC COLUMN'S BROWSER SHOWS, where it is not the inventory icons.
  //
  // Presentation, so it is here rather than in the descriptors: nothing about the COLUMN says which
  // atlas its number indexes. Every Graphic composite in the editor is an inventory icon except
  // one — Spell Effects' spell_animation is an EFFECT id (app.js draws it through Preview.effect,
  // and Sprites.effectFrames is the lookup) — so the table has exactly one entry and the default is
  // 'icons'. A second sheet gaining an animation column gets its browser from here rather than from
  // a name spelled out in pickers.js.
  var GALLERIES = {
    'Spell Effects': { spell_animation: 'effects' },
  };

  // WHICH COLUMN MAKES A JOIN SHEET'S ROWS BELONG TO SOMETHING. A sheet listed here is edited as
  // one table per parent — "1 — Mouse" and all three of its drops at once — instead of as a flat
  // list of id pairs.
  //
  // Presentation, so it lives here rather than in the descriptors: the importer does not care how
  // rows are grouped, and two of these have more than one defensible parent. Only the COLUMN is
  // named; the parent SHEET is that column's `ref` in the schema, so the two cannot drift.
  //
  // The two judgement calls, both checked by layout.test.js:
  //   NPC Spawns refs NPCs and Maps. By map, because spawns are authored a zone at a time and it
  //     splits 4,322 rows more evenly than by NPC.
  //   Warptiles refs Maps twice (map_id, warp_id). By map_id — where the tile IS, not where it
  //     goes.
  // Quest Reqs and Quest Rewards keep their own `id` pk; the parent here is quest_id, which is
  // the second column, not the first.
  //
  // NOT LISTED, deliberately: Class Info. It is class_id + level, 26 columns wide with a row per
  // level, so a group would be a ~99 x 25 grid — the flat form is the better shape for it.
  var GROUP_PARENT = {
    'NPC Drops': 'npc_template_id',
    'NPC Vendor Items': 'npc_template_id',
    'NPC Spawns': 'map_id',
    'Warptiles': 'map_id',
    'Map Required Items': 'map_id',
    'Quest Reqs': 'quest_id',
    'Quest Rewards': 'quest_id',
    'Combination Item Required': 'combination_id',
    'Combination Item Result': 'combination_id',
    'Class Levelup Spells': 'class_id',
  };

  // Own-property lookups throughout: a sheet named 'constructor' is not a thing, but neither is
  // reaching Object.prototype for one, and the rest of this file is prototype-free for the same
  // reason.
  function own(table, key) {
    return Object.prototype.hasOwnProperty.call(table, key) ? table[key] : undefined;
  }

  function twoLevel(table, sheet, column) {
    var forSheet = own(table, String(sheet));
    if (!forSheet) return null;
    var hit = own(forSheet, String(column));
    return hit === undefined ? null : hit;
  }

  /// The tint columns for one graphic column, or null when that graphic is drawn plain.
  function tintColumns(sheet, column) {
    return twoLevel(TINTS, sheet, column);
  }

  /// The part-graphic spec for one column, or null when the column is not a character part.
  function partGraphic(sheet, column) {
    return twoLevel(PART_GRAPHICS, sheet, column);
  }

  /// The sheet's wearable gate — `{ column, values, columns }` — or null when everything on the
  /// sheet is always relevant. One accessor for both consumers (forms.js hides the rows,
  /// app.js skips the worn preview) so the two cannot disagree about what "wearable" means.
  function wearableGate(sheet) {
    var gate = own(WEARABLE, String(sheet));
    return gate === undefined ? null : gate;
  }

  /// The sheet's monster-body rule — `{ column, from, columns, clear }` — or null when the sheet has
  /// no such cell. One accessor for all three consumers (forms.js hides the rows, app.js clears the
  /// cells on save, and both go through isMonsterBody below for the test itself) so no two of them
  /// can disagree about which cells a monster body kills.
  function monsterBodyGate(sheet) {
    var gate = own(MONSTER_BODY, String(sheet));
    return gate === undefined ? null : gate;
  }

  /// True when this record's body id is a monster or morph body, i.e. when the columns the gate
  /// names are dead. False for every sheet without such a rule, so a caller needs no branch of its
  /// own before asking.
  ///
  /// parseInt(v, 10), matching Appearance.num() — this is the same comparison Appearance.layers
  /// makes, on the same cell, and the two must agree about what ' 150 ' means. A blank or
  /// non-numeric cell reads 0, which is a PLAYER body: it means "use the SQL default", and the
  /// default for both sheets that carry the column is 1.
  function isMonsterBody(sheet, values) {
    var gate = monsterBodyGate(sheet);
    if (!gate) return false;
    var n = parseInt((values || {})[gate.column], 10);
    return !isNaN(n) && n >= gate.from;
  }

  /// Which sprite bundle the graphic browser should show for one column. Never null: 'icons' is the
  /// answer for every graphic column but the one in GALLERIES, so callers need no fallback of their
  /// own and cannot disagree about what it is.
  function galleryBundle(sheet, column) {
    return twoLevel(GALLERIES, sheet, column) || 'icons';
  }

  /// The column a sheet's rows are grouped by, or null when the sheet is edited flat. One
  /// accessor for both consumers — app.js branches on it, groups.js builds the grouping from it —
  /// so no two of them can disagree about which sheets are grouped.
  function groupParent(sheet) {
    var column = own(GROUP_PARENT, String(sheet));
    return column === undefined ? null : column;
  }

  // ReloadSQLCommandEvent.cs:30-40 reloads spell effects, spells, item templates, quests and NPC
  // templates. LoadMaps, LoadClasses, LoadNPCs and LoadCombinations are commented out, so edits
  // to the sheets those loaders read need a full server restart, not /reloadsql.
  //
  // The mapping from loader to sheet is wider than the loader names suggest:
  //   LoadClasses      -> classes, class_info, classes_levelup_spells (ClassHandler.cs:47,66,117)
  //   LoadMaps         -> maps, then Map.LoadData -> warptiles, map_required_items
  //                       (MapHandler.cs:41,78; Map.cs:488,507)
  //   LoadNPCs         -> npc_spawns (NPCHandler.cs:263)
  //   LoadCombinations -> combinations, combination_item_required, combination_item_results
  //                       (CombinationHandler.cs:22,48,85)
  // Titles and surnames are in here for a different reason: LoadTitles/LoadSurnames are only
  // called from GameWorld startup (GameWorld.cs:276,289) and /reloadsql never touches them.
  //
  // Conversely npc_drops and npc_vendor_items are read from inside LoadNPCTemplates
  // (NPCHandler.cs:154,172), and quest requirements/rewards from inside LoadQuests
  // (QuestHandler.cs:40,61) — those sheets ARE live.
  var RESTART_ONLY = ['Maps', 'Warptiles', 'Map Required Items', 'Classes', 'Class Info',
                      'Class Levelup Spells', 'NPC Spawns', 'Combinations',
                      'Combination Item Required', 'Combination Item Result', 'Titles',
                      'Surnames'];

  // The longest shared leading text of every name, cut back to the last '_' it contains and
  // with that '_' dropped: ['graphic_r','graphic_g','graphic_b','graphic_a'] -> 'graphic'.
  // Cutting at '_' rather than taking the raw prefix is what stops a coincidental shared letter
  // — ['red','reg','reb'] — from being read as a field name.
  function sharedPrefix(names) {
    if (names.length < 2) return '';
    var prefix = names[0];
    names.forEach(function (name) {
      var i = 0;
      while (i < prefix.length && i < name.length && prefix.charAt(i) === name.charAt(i)) i++;
      prefix = prefix.slice(0, i);
    });
    var cut = prefix.lastIndexOf('_');
    return cut === -1 ? '' : prefix.slice(0, cut);
  }

  // What the form calls a composite. A composite spans several columns, so naming it after the
  // leader — the first column it claims — is honest only for the three kinds that claim exactly
  // one. For Rgba the leader is the RED CHANNEL, so the tint on Items read 'graphic_r'; for
  // Graphic the leader is the tile, which hides the sheet column entirely.
  //
  // `leader` is the column the form actually rendered the control at, which is not always
  // columns[0] (forms.js skips a column the descriptors dropped). Everything falls back to it,
  // so a composite whose names share no prefix can never produce a label like ' tint'.
  function labelFor(comp, leader) {
    var columns = (comp && comp.columns) || [];
    var name = leader || columns[0] || '';
    if (!comp) return name;

    if (comp.kind === 'Rgba') {
      var prefix = sharedPrefix(columns);
      return prefix ? prefix + ' tint' : name;
    }
    // ' + sheet' is only true when the leader IS the tile column; if the descriptors dropped it
    // and the form led with the sheet column instead, the promise would name the sheet twice.
    if (comp.kind === 'Graphic') return leader === columns[0] ? name + ' + sheet' : name;
    return name;
  }

  function needsRestart(sheet) {
    return RESTART_ONLY.indexOf(sheet) !== -1;
  }

  // Groups for a sheet. Columns absent from the layout still appear, under "Other", so a new
  // descriptor column is never silently uneditable. The form builder skips empty groups, which is
  // why passing no columns still yields every title.
  function groupsFor(sheet, columns) {
    var names = columns.map(function (c) { return c.name; });
    var layout = LAYOUTS[sheet];

    if (!layout) return [{ title: 'Fields', columns: names }];

    // Prototype-free: a column named 'constructor' or 'toString' would otherwise read truthy
    // from Object.prototype, drop out of the leftover filter, and vanish from the form —
    // defeating the one safety net that exists for a column nobody anticipated.
    var placed = Object.create(null);
    var groups = layout.map(function (g) {
      var present = g.columns.filter(function (n) { return names.indexOf(n) !== -1; });
      present.forEach(function (n) { placed[n] = true; });
      return { title: g.title, columns: present };
    });

    var leftover = names.filter(function (n) { return !placed[n]; });
    if (leftover.length) groups.push({ title: 'Other', columns: leftover });

    return groups;
  }

  // Frozen, not copied: callers hold these for as long as they like, and a stray push must change
  // neither what needsRestart() answers nor what the next consumer reads. A copy handed out once
  // would still be shared between consumers.
  function deepFreeze(value) {
    if (value && typeof value === 'object') {
      Object.keys(value).forEach(function (k) { deepFreeze(value[k]); });
      Object.freeze(value);
    }
    return value;
  }

  // LAYOUTS is exported so the table can be checked against schema.js in BOTH directions: a name
  // here for a column that does not exist is invisible to groupsFor, which simply filters it out.
  return {
    groupsFor: groupsFor,
    labelFor: labelFor,
    needsRestart: needsRestart,
    tintColumns: tintColumns,
    partGraphic: partGraphic,
    wearableGate: wearableGate,
    monsterBodyGate: monsterBodyGate,
    isMonsterBody: isMonsterBody,
    galleryBundle: galleryBundle,
    groupParent: groupParent,
    RESTART_ONLY: deepFreeze(RESTART_ONLY),
    LAYOUTS: deepFreeze(LAYOUTS),
    // Exported for the same reason LAYOUTS is: a name here for a column that does not exist is
    // invisible to every consumer, which simply reads null and draws no tint.
    TINTS: deepFreeze(TINTS),
    PART_GRAPHICS: deepFreeze(PART_GRAPHICS),
    WEARABLE: deepFreeze(WEARABLE),
    MONSTER_BODY: deepFreeze(MONSTER_BODY),
    GALLERIES: deepFreeze(GALLERIES),
    GROUP_PARENT: deepFreeze(GROUP_PARENT),
  };
})();

if (typeof module !== 'undefined') module.exports = { Layout: Layout };
