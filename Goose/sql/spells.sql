DROP TABLE IF EXISTS spells;
CREATE TABLE spells (
  spell_id INTEGER PRIMARY KEY,
  spell_name TEXT NOT NULL,
  spell_description TEXT DEFAULT '' NOT NULL,
  spell_target INT NOT NULL,
  class_restrictions BIGINT DEFAULT 0 NOT NULL, /* allow list: bit set = class id can cast, 0 = all */
  spell_aether BIGINT DEFAULT 100 NOT NULL, /* Aether in milliseconds */
  spellbook_graphic INT NOT NULL,
  spellbook_graphic_file INT NOT NULL,
  
  hp_static_cost INT DEFAULT 0 NOT NULL,
  hp_percent_cost DECIMAL(9,4) DEFAULT 0 NOT NULL,
  mp_static_cost INT DEFAULT 0 NOT NULL,
  mp_percent_cost DECIMAL(9,4) DEFAULT 0 NOT NULL,
  sp_static_cost INT DEFAULT 0 NOT NULL,
  sp_percent_cost DECIMAL(9,4) DEFAULT 0 NOT NULL,

  spell_effect_id INT NOT NULL
);

/*
class_restrictions is an ALLOW list: bit N set means class id N can cast the spell.
0 is the special case and means no restriction, ie every class can cast it.

bit		128	64	32	16	08	04	02	01
class		gm	bard	priest	magus	warrior	rogue	commoner -

class		id	bit value
commoner	1	2
rogue		2	4
warrior		3	8
magus		4	16
priest		5	32
bard		6	64
game master	7	128

Add the values of the classes that may cast it: magus + priest = 48.
Everyone (classes 1-7) = 254, but prefer 0 for that.

*/

/*
INSERT INTO spells (spell_id, spell_name, spell_target, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (1, 'Lightning Strike', 0, 300, 50456, 421, 10, 1);
*/

DROP TABLE IF EXISTS spell_effects;
CREATE TABLE spell_effects (
  spell_effect_id INTEGER PRIMARY KEY,
  spell_effect_name TEXT NOT NULL,
  spell_animation INT NOT NULL,
  spell_animation_file INT NOT NULL,
  spell_display INT NOT NULL,
  target_type INT NOT NULL,
  target_size INT NOT NULL,
  
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

/*
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_animation_file, spell_display, target_type, target_size, spell_effected, effect_type, effect_duration, spell_damage_effects, hp_change_formula)
VALUES (1, 'Lightning Strike', 65023, 420, 0, 0, 1, 6, 0, 0, 1, '-10');
*/