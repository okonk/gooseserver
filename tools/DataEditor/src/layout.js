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
    if (comp.kind === 'Graphic') return name + ' + sheet';
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
    RESTART_ONLY: deepFreeze(RESTART_ONLY),
    LAYOUTS: deepFreeze(LAYOUTS),
  };
})();

if (typeof module !== 'undefined') module.exports = { Layout: Layout };
