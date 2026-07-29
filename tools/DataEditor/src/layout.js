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
          'res_earth', 'weapon_damage', 'armor_pierce', 'attack_range', 'attack_speed',
          'experience'] },
      { title: 'Behaviour', columns: ['npc_facing', 'aggro_range', 'move_speed', 'stationary',
          'stunnable', 'rootable', 'slowable', 'invincible', 'stuck_behaviour', 'stuck_timeout',
          'respawn_time'] },
      { title: 'Regen', columns: ['hp_percent_regen', 'hp_static_regen', 'mp_percent_regen',
          'mp_static_regen'] },
      { title: 'Links', columns: ['class_id', 'quest_ids', 'credit_dealer'] },
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
      { title: 'Modifiers', columns: ['hp', 'mp', 'sp', 'stat_ac', 'stat_str', 'stat_sta',
          'stat_dex', 'stat_int', 'res_fire', 'res_water', 'res_spirit', 'res_air', 'res_earth',
          'hp_percent_regen', 'hp_static_regen', 'mp_percent_regen', 'mp_static_regen', 'haste',
          'spell_damage', 'spell_crit', 'melee_damage', 'melee_crit', 'damage_reduce',
          'move_speed', 'snare_percent'] },
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

    var placed = {};
    var groups = layout.map(function (g) {
      var present = g.columns.filter(function (n) { return names.indexOf(n) !== -1; });
      present.forEach(function (n) { placed[n] = true; });
      return { title: g.title, columns: present };
    });

    var leftover = names.filter(function (n) { return !placed[n]; });
    if (leftover.length) groups.push({ title: 'Other', columns: leftover });

    return groups;
  }

  // A copy: callers hold this array for as long as they like, and a stray push must not change
  // what needsRestart() answers.
  return { groupsFor: groupsFor, needsRestart: needsRestart, RESTART_ONLY: RESTART_ONLY.slice() };
})();

if (typeof module !== 'undefined') module.exports = { Layout: Layout };
