BEGIN TRANSACTION;

DROP TABLE IF EXISTS item_templates;
CREATE TABLE item_templates (
  item_template_id INTEGER PRIMARY KEY,
  item_usetype SMALLINT NOT NULL,
  item_name TEXT NOT NULL,
  item_description TEXT DEFAULT '' NOT NULL,
  player_hp INT DEFAULT 0 NOT NULL,
  player_mp INT DEFAULT 0 NOT NULL,
  player_sp INT DEFAULT 0 NOT NULL,
  stat_ac SMALLINT DEFAULT 0 NOT NULL,
  stat_str SMALLINT DEFAULT 0 NOT NULL,
  stat_sta SMALLINT DEFAULT 0 NOT NULL,
  stat_dex SMALLINT DEFAULT 0 NOT NULL,
  stat_int SMALLINT DEFAULT 0 NOT NULL,
  res_fire SMALLINT DEFAULT 0 NOT NULL,
  res_water SMALLINT DEFAULT 0 NOT NULL,
  res_spirit SMALLINT DEFAULT 0 NOT NULL,
  res_air SMALLINT DEFAULT 0 NOT NULL,
  res_earth SMALLINT DEFAULT 0 NOT NULL,
  min_experience BIGINT DEFAULT 0 NOT NULL,
  min_level SMALLINT DEFAULT 0 NOT NULL,
  max_experience BIGINT DEFAULT 0 NOT NULL,
  max_level SMALLINT DEFAULT 0 NOT NULL,
  weapon_damage SMALLINT DEFAULT 0 NOT NULL,
  weapon_delay SMALLINT DEFAULT 10 NOT NULL,
  item_slot SMALLINT DEFAULT 20 NOT NULL,
  item_type SMALLINT DEFAULT 0 NOT NULL,
  item_value BIGINT DEFAULT 0 NOT NULL,
  lore CHAR(1) DEFAULT '0' NOT NULL,
  bindonpickup CHAR(1) DEFAULT '0' NOT NULL,
  bindonequip CHAR(1) DEFAULT '0' NOT NULL,
  event CHAR(1) DEFAULT '0' NOT NULL,
  graphic_tile INT NOT NULL,
  graphic_file INT DEFAULT 0 NOT NULL,
  graphic_equip SMALLINT DEFAULT 0 NOT NULL,
  graphic_r SMALLINT DEFAULT 0 NOT NULL,
  graphic_g SMALLINT DEFAULT 0 NOT NULL,
  graphic_b SMALLINT DEFAULT 0 NOT NULL,
  graphic_a SMALLINT DEFAULT 0 NOT NULL,
  class_restrictions BIGINT DEFAULT 0 NOT NULL,
  stack_size SMALLINT DEFAULT 1 NOT NULL,
  body_state SMALLINT DEFAULT 3 NOT NULL,
  spell_effect_id INT DEFAULT 0 NOT NULL,
  spell_effect_chance DECIMAL(9,4) DEFAULT 100 NOT NULL,
  learn_spell_id INT DEFAULT 0 NOT NULL,
  credits_value INT DEFAULT 0 NOT NULL,
  script_path TEXT DEFAULT '' NOT NULL,
  script_params TEXT DEFAULT '' NOT NULL
);

{{item_templates}}

DROP TABLE IF EXISTS npc_templates;
CREATE TABLE npc_templates (
  npc_id INTEGER PRIMARY KEY,
  npc_type SMALLINT DEFAULT 2 NOT NULL,
  npc_name TEXT NOT NULL,
  npc_title TEXT DEFAULT '' NOT NULL,
  npc_surname TEXT DEFAULT '' NOT NULL,
  respawn_time INT DEFAULT 0 NOT NULL,
  npc_facing SMALLINT DEFAULT 3 NOT NULL,
  npc_level SMALLINT DEFAULT 1 NOT NULL,
  experience BIGINT DEFAULT 0 NOT NULL,
  aggro_range SMALLINT DEFAULT 0 NOT NULL,
  attack_range SMALLINT DEFAULT 0 NOT NULL,
  attack_speed DECIMAL(9,4) DEFAULT 2 NOT NULL,
  move_speed DECIMAL(9,4) DEFAULT 2 NOT NULL,
  stationary CHAR(1) DEFAULT '0' NOT NULL,
  stunnable CHAR(1) DEFAULT '0' NOT NULL,
  rootable CHAR(1) DEFAULT '0' NOT NULL,
  slowable CHAR(1) DEFAULT '0' NOT NULL,
  invincible CHAR(1) DEFAULT '0' NOT NULL,
  npc_hp INT DEFAULT 0 NOT NULL,
  npc_mp INT DEFAULT 0 NOT NULL,
  npc_sp INT DEFAULT 0 NOT NULL,
  class_id SMALLINT DEFAULT 1 NOT NULL,
  stat_ac SMALLINT DEFAULT 0 NOT NULL,
  stat_str SMALLINT DEFAULT 0 NOT NULL,
  stat_sta SMALLINT DEFAULT 0 NOT NULL,
  stat_dex SMALLINT DEFAULT 0 NOT NULL,
  stat_int SMALLINT DEFAULT 0 NOT NULL,
  res_fire SMALLINT DEFAULT 0 NOT NULL,
  res_water SMALLINT DEFAULT 0 NOT NULL,
  res_spirit SMALLINT DEFAULT 0 NOT NULL,
  res_air SMALLINT DEFAULT 0 NOT NULL,
  res_earth SMALLINT DEFAULT 0 NOT NULL,
  body_state SMALLINT DEFAULT 1 NOT NULL,
  body_id SMALLINT DEFAULT 1 NOT NULL,
  body_r SMALLINT DEFAULT 0 NOT NULL,
  body_g SMALLINT DEFAULT 0 NOT NULL,
  body_b SMALLINT DEFAULT 0 NOT NULL,
  body_a SMALLINT DEFAULT 0 NOT NULL,
  face_id SMALLINT DEFAULT 70 NOT NULL,
  hair_id SMALLINT DEFAULT 26 NOT NULL,
  hair_r SMALLINT DEFAULT 0 NOT NULL,
  hair_g SMALLINT DEFAULT 0 NOT NULL,
  hair_b SMALLINT DEFAULT 0 NOT NULL,
  hair_a SMALLINT DEFAULT 0 NOT NULL,
  equipped_items TEXT DEFAULT '0,*,0,*,0,*,0,*,0,*,0,*' NOT NULL,
  weapon_damage INT DEFAULT 1 NOT NULL,
  hp_percent_regen DECIMAL(9,4) DEFAULT 0 NOT NULL,
  hp_static_regen INT DEFAULT 0 NOT NULL,
  mp_percent_regen DECIMAL(9,4) DEFAULT 0 NOT NULL,
  mp_static_regen INT DEFAULT 0 NOT NULL,
  npc_alliance TEXT DEFAULT '' NOT NULL,
  stuck_behaviour SMALLINT DEFAULT 0 NOT NULL,
  stuck_timeout INT DEFAULT 20 NOT NULL, /* Time since last attack to do behaviour in seconds */
  credit_dealer CHAR(1) DEFAULT '0' NOT NULL,
  quest_ids TEXT DEFAULT '' NOT NULL,
  script_path TEXT DEFAULT 'Scripts/NPC/BaseNPC.csx' NOT NULL,
  script_params TEXT DEFAULT '' NOT NULL,
  armor_pierce INT DEFAULT 0 NOT NULL
);

{{npc_templates}}

DROP TABLE IF EXISTS npc_spawns;
CREATE TABLE npc_spawns (
  npc_id INT NOT NULL,
  map_id SMALLINT NOT NULL,
  map_x SMALLINT NOT NULL,
  map_y SMALLINT NOT NULL
);

{{npc_spawns}}

DROP TABLE IF EXISTS npc_drops;
CREATE TABLE npc_drops (
  npc_template_id INT NOT NULL,
  item_template_id INT NOT NULL,
  stack INT NOT NULL,
  droprate DECIMAL(9,4) NOT NULL
);

{{npc_drops}}

DROP TABLE IF EXISTS npc_vendor_items;
CREATE TABLE npc_vendor_items (
  npc_template_id INT NOT NULL,
  item_template_id INT NOT NULL,
  stack INT DEFAULT 1 NOT NULL,
  stats_visible CHAR(1) DEFAULT '1' NOT NULL,
  slot INT NOT NULL
);

CREATE INDEX npc_vendor_items_npc_template_id_idx ON npc_vendor_items(npc_template_id);

{{npc_vendor_items}}

DROP TABLE IF EXISTS quests;
CREATE TABLE quests (
  id INTEGER PRIMARY KEY,
  name TEXT NOT NULL,
  description TEXT DEFAULT '' NOT NULL,
  fail_text TEXT DEFAULT '' NOT NULL,
  pass_text TEXT DEFAULT '' NOT NULL,
  min_experience BIGINT DEFAULT 0,
  max_experience BIGINT DEFAULT 0,
  min_level INT DEFAULT 0,
  max_level INT DEFAULT 0,
  repeatable CHAR(1) DEFAULT '0',
  show_progress CHAR(1) DEFAULT '0',
  only_one_player_can_complete CHAR(1) DEFAULT '0',
  prerequisite_quests TEXT DEFAULT '' NOT NULL
);

{{quests}}

DROP TABLE IF EXISTS quest_requirements;
CREATE TABLE quest_requirements (
  id INTEGER PRIMARY KEY,
  quest_id INT NOT NULL,
  requirement_type INT NOT NULL,
  requirement_value BIGINT NOT NULL,
  requirement_value2 BIGINT DEFAULT 0,
  keep_requirement CHAR(1) DEFAULT '0'
);

{{quest_requirements}}

DROP TABLE IF EXISTS quest_rewards;
CREATE TABLE quest_rewards (
  id INTEGER PRIMARY KEY,
  quest_id INT NOT NULL,
  reward_type INT NOT NULL,
  long_value BIGINT DEFAULT 0,
  long_value2 BIGINT DEFAULT 0,
  string_value TEXT DEFAULT ''
);

{{quest_rewards}}

DROP TABLE IF EXISTS spells;
CREATE TABLE spells (
  spell_id INTEGER PRIMARY KEY,
  spell_name TEXT NOT NULL,
  spell_description TEXT DEFAULT '' NOT NULL,
  spell_target INT NOT NULL,
  class_restrictions BIGINT DEFAULT 0 NOT NULL, /* if bit not set class id can cast */
  spell_aether BIGINT DEFAULT 100 NOT NULL, /* Aether in milliseconds */
  spellbook_graphic INT NOT NULL,
  spellbook_graphic_file INT DEFAULT 0 NOT NULL,
  
  hp_static_cost INT DEFAULT 0 NOT NULL,
  hp_percent_cost DECIMAL(9,4) DEFAULT 0 NOT NULL,
  mp_static_cost INT DEFAULT 0 NOT NULL,
  mp_percent_cost DECIMAL(9,4) DEFAULT 0 NOT NULL,
  sp_static_cost INT DEFAULT 0 NOT NULL,
  sp_percent_cost DECIMAL(9,4) DEFAULT 0 NOT NULL,

  spell_effect_id INT NOT NULL
);

{{spells}}
  
DROP TABLE IF EXISTS spell_effects;
CREATE TABLE spell_effects (
  spell_effect_id INTEGER PRIMARY KEY,
  spell_effect_name TEXT NOT NULL,
  spell_animation INT DEFAULT 0 NOT NULL,
  spell_animation_file INT DEFAULT 0 NOT NULL,
  spell_display INT DEFAULT 0 NOT NULL,
  target_type INT DEFAULT 0 NOT NULL,
  target_size INT DEFAULT 0 NOT NULL,
  
  spell_effected INT NOT NULL,
  min_level_effected INT DEFAULT 1 NOT NULL,
  max_level_effected INT DEFAULT 50 NOT NULL,
  
  effect_type INT NOT NULL,
  effect_duration BIGINT DEFAULT 0 NOT NULL,
  
  do_attack_animation CHAR(1) DEFAULT '0' NOT NULL,
  do_cast_animation CHAR(1) DEFAULT '1' NOT NULL,
  spell_damage_effects CHAR(1) DEFAULT '0' NOT NULL, /* does spell damage/crit effect this spell */
  spell_energy_type INT DEFAULT 0 NOT NULL, /* bitfield fire, water, spirit, air, earth, none? */
  
  /* for damage/heal kinda spells */
  hp_change_formula TEXT DEFAULT '0' NOT NULL, /* change_formulas are what to do to the */
  mp_change_formula TEXT DEFAULT '0' NOT NULL, /* effected persons stat */
  sp_change_formula TEXT DEFAULT '0' NOT NULL, /* for damage/heals */
  
  /* Stuff for buffs/permanent */
  hp INT DEFAULT 0 NOT NULL,
  mp INT DEFAULT 0 NOT NULL,
  sp INT DEFAULT 0 NOT NULL,
  stat_ac SMALLINT DEFAULT 0 NOT NULL,
  stat_str SMALLINT DEFAULT 0 NOT NULL,
  stat_sta SMALLINT DEFAULT 0 NOT NULL,
  stat_dex SMALLINT DEFAULT 0 NOT NULL,
  stat_int SMALLINT DEFAULT 0 NOT NULL,
  res_fire SMALLINT DEFAULT 0 NOT NULL,
  res_water SMALLINT DEFAULT 0 NOT NULL,
  res_spirit SMALLINT DEFAULT 0 NOT NULL,
  res_air SMALLINT DEFAULT 0 NOT NULL,
  res_earth SMALLINT DEFAULT 0 NOT NULL,
  hp_percent_regen DECIMAL(9,4) DEFAULT 0 NOT NULL,
  hp_static_regen INT DEFAULT 0 NOT NULL,
  mp_percent_regen DECIMAL(9,4) DEFAULT 0 NOT NULL,
  mp_static_regen INT DEFAULT 0 NOT NULL,
  haste DECIMAL(9,4) DEFAULT 0 NOT NULL,
  spell_damage DECIMAL(9,4) DEFAULT 0 NOT NULL,
  spell_crit DECIMAL(9,4) DEFAULT 0 NOT NULL,
  melee_damage DECIMAL(9,4) DEFAULT 0 NOT NULL,
  melee_crit DECIMAL(9,4) DEFAULT 0 NOT NULL,
  damage_reduce DECIMAL(9,4) DEFAULT 0 NOT NULL,
  move_speed DECIMAL(9,4) DEFAULT 0 NOT NULL,
  body_id SMALLINT DEFAULT 0 NOT NULL,
  
  oneffect_text TEXT DEFAULT '' NOT NULL,
  offeffect_text TEXT DEFAULT '' NOT NULL,
  
  /* For permanent */
  face_id SMALLINT DEFAULT 0 NOT NULL,
  hair_id SMALLINT DEFAULT 0 NOT NULL,
  hair_r SMALLINT DEFAULT 0 NOT NULL,
  hair_g SMALLINT DEFAULT 0 NOT NULL,
  hair_b SMALLINT DEFAULT 0 NOT NULL,
  hair_a SMALLINT DEFAULT 0 NOT NULL,
  body_r SMALLINT DEFAULT 0 NOT NULL,
  body_g SMALLINT DEFAULT 0 NOT NULL,
  body_b SMALLINT DEFAULT 0 NOT NULL,
  body_a SMALLINT DEFAULT 0 NOT NULL,
  
  /* Stuff for teleport */
  teleport_map INT DEFAULT 1 NOT NULL,
  teleport_x INT DEFAULT 50 NOT NULL,
  teleport_y INT DEFAULT 50 NOT NULL,
  
  /* Aggro for taunt */
  taunt_aggro INT DEFAULT 0 NOT NULL,
  
  works_in_pvp CHAR(1) DEFAULT '1' NOT NULL,
  works_not_in_pvp CHAR(1) DEFAULT '0' NOT NULL,
  
  buff_removable CHAR(1) DEFAULT '1' NOT NULL,
  buff_graphic INT DEFAULT 0 NOT NULL,
  buff_graphic_file INT DEFAULT 0 NOT NULL,
  buff_doesnt_stack_over TEXT DEFAULT '' NOT NULL,
  buff_stacks_over TEXT DEFAULT '' NOT NULL,
  
  random_join_chance DECIMAL(5,2) DEFAULT 0 NOT NULL,
  
  on_hit_spell_effect_id INT DEFAULT 0 NOT NULL,
  on_hit_spell_chance DECIMAL(5,2) DEFAULT 100 NOT NULL,
  on_attack_spell_effect_id INT DEFAULT 0 NOT NULL,
  on_attack_spell_chance DECIMAL(5,2) DEFAULT 100 NOT NULL,
  
  snare_percent DECIMAL(5,2) DEFAULT 0 NOT NULL,
  
  only_hits_one_npc CHAR(1) DEFAULT '0' NOT NULL,

  script_path TEXT DEFAULT '' NOT NULL,
  script_params TEXT DEFAULT '' NOT NULL
);

{{spell_effects}}

DROP TABLE IF EXISTS warptiles;
CREATE TABLE warptiles (
  map_id SMALLINT NOT NULL,
  map_x SMALLINT NOT NULL,
  map_y SMALLINT NOT NULL,
  warp_id SMALLINT NOT NULL,
  warp_x SMALLINT NOT NULL,
  warp_y SMALLINT NOT NULL
);

{{warptiles}}

DROP TABLE IF EXISTS maps;
CREATE TABLE maps (
  map_id INTEGER PRIMARY KEY,
  map_name TEXT NOT NULL,
  map_filename TEXT NOT NULL,
  
  min_level SMALLINT DEFAULT 0 NOT NULL,
  max_level SMALLINT DEFAULT 0 NOT NULL,
  min_experience BIGINT DEFAULT 0 NOT NULL,
  max_experience BIGINT DEFAULT 0 NOT NULL,
  
  pvp_enabled CHAR(1) DEFAULT '0' NOT NULL,
  chat_enabled CHAR(1) DEFAULT '1' NOT NULL,
  auction_enabled CHAR(1) DEFAULT '1' NOT NULL,
  shout_enabled CHAR(1) DEFAULT '1' NOT NULL,
  spells_enabled CHAR(1) DEFAULT '1' NOT NULL,
  bind_enabled CHAR(1) DEFAULT '0' NOT NULL,
  items_enabled CHAR(1) DEFAULT '1' NOT NULL,
  pets_enabled CHAR(1) DEFAULT '1' NOT NULL,

  script_path TEXT DEFAULT '' NOT NULL,
  script_params TEXT DEFAULT '' NOT NULL
);

{{maps}}

DROP TABLE IF EXISTS map_required_items;
CREATE TABLE map_required_items (
  map_id INT NOT NULL,
  item_template_id INT NOT NULL
);

CREATE INDEX map_required_items_map_id_idx ON map_required_items(map_id);

{{map_required_items}}

DROP TABLE IF EXISTS combinations;
CREATE TABLE combinations (
	combination_id INTEGER PRIMARY KEY,
	combination_name VARCHAR(64) NOT NULL,
	min_level INT DEFAULT 1 NOT NULL,
	max_level INT DEFAULT 50 NOT NULL,
	min_experience BIGINT DEFAULT 0 NOT NULL,
	max_experience BIGINT DEFAULT 0 NOT NULL,
	class_restrictions BIGINT DEFAULT 0 NOT NULL
);

{{combinations}}

DROP TABLE IF EXISTS combination_item_required;
CREATE TABLE combination_item_required (
	combination_id INT NOT NULL,
	item_template_id INT NOT NULL
);

{{combination_item_required}}

DROP TABLE IF EXISTS combination_item_results;
CREATE TABLE combination_item_results (
	combination_id INT NOT NULL,
	item_template_id INT NOT NULL
);

{{combination_item_results}}

COMMIT;