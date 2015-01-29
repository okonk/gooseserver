USE IllutiaGoose;

DROP TABLE spells;
CREATE TABLE spells (
  spell_id INT IDENTITY(1,1) NOT NULL,
  spell_name VARCHAR(64) NOT NULL,
  spell_description VARCHAR(128) DEFAULT '' NOT NULL,
  spell_target INT NOT NULL,
  class_restrictions BIGINT DEFAULT 0 NOT NULL, /* if bit not set class id can cast */
  spell_aether BIGINT DEFAULT 100 NOT NULL, /* Aether in milliseconds */
  spellbook_graphic INT NOT NULL,
  spellbook_graphic_file INT NOT NULL,
  
  hp_static_cost INT DEFAULT 0 NOT NULL,
  hp_percent_cost DECIMAL(9,4) DEFAULT 0 NOT NULL,
  mp_static_cost INT DEFAULT 0 NOT NULL,
  mp_percent_cost DECIMAL(9,4) DEFAULT 0 NOT NULL,
  sp_static_cost INT DEFAULT 0 NOT NULL,
  sp_percent_cost DECIMAL(9,4) DEFAULT 0 NOT NULL,

  spell_effect_id INT NOT NULL,
  
  PRIMARY KEY(spell_id)
);

/*
class_restrictions:

32 16 08 04 02 01
 p  m  w  r  c  0
 
value = 127 - number above class name
 
class		  id	class_restriction value
commoner	1	  61
rogue		  2	  59
warrior   3	  55
magus		  4	  47
priest		5	  31

*/

SET IDENTITY_INSERT spells ON;
INSERT INTO spells (spell_id, spell_name, spell_target, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (1, 'Lightning Strike', 0, 300, 50456, 421, 10, 4);

INSERT INTO spells (spell_id, spell_name, spell_target, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (2, 'Reave', 1, 4000, 50469, 421, 25, 5);

INSERT INTO spells (spell_id, spell_name, spell_target, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (3, 'Chomp', 1, 6000, 50448, 421, 30, 6);

INSERT INTO spells (spell_id, spell_name, spell_target, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (4, 'Heal', 0, 200, 50459, 421, 15, 7);



SET IDENTITY_INSERT spells OFF;
  
DROP TABLE spell_effects;
CREATE TABLE spell_effects (
  spell_effect_id INT IDENTITY(1,1) NOT NULL,
  spell_effect_name VARCHAR(64) NOT NULL,
  spell_animation INT NOT NULL,
  spell_animation_file INT NOT NULL,
  spell_display INT NOT NULL,
  target_type INT NOT NULL,
  target_size INT NOT NULL,
  
  spell_effected INT NOT NULL,
  min_level_effected INT DEFAULT 1 NOT NULL,
  max_level_effected INT DEFAULT 50 NOT NULL,
  
  effect_type INT NOT NULL,
  effect_duration BIGINT NOT NULL,
  
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
  
  oneffect_text VARCHAR(64) DEFAULT '' NOT NULL,
  offeffect_text VARCHAR(64) DEFAULT '' NOT NULL,
  
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
  
  PRIMARY KEY (spell_effect_id)
);

SET IDENTITY_INSERT spell_effects ON;

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_animation_file, spell_display, target_type, target_size, spell_effected, effect_type, effect_duration, hp_static_regen, mp_static_regen, buff_graphic, buff_graphic_file)
VALUES (1, 'Basic Regeneration', 0, 0, 0, 0, 0, 1, 1, 0, 5, 5, 50754, 104);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_animation_file, spell_display, target_type, target_size, spell_effected, effect_type, effect_duration, hp_static_regen, buff_graphic, buff_graphic_file)
VALUES (2, 'HP Regeneration 500', 0, 0, 0, 0, 0, 1, 1, 0, 500, 50754, 104);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_animation_file, spell_display, target_type, target_size, spell_effected, effect_type, effect_duration, move_speed, buff_graphic, buff_graphic_file)
VALUES (3, 'Mount', 0, 0, 0, 0, 0, 1, 1, 0, 0.5, 50754, 104);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_animation_file, spell_display, target_type, target_size, spell_effected, effect_type, effect_duration, spell_damage_effects, hp_change_formula)
VALUES (4, 'Lightning Strike', 65023, 420, 0, 0, 1, 2, 0, 0, 1, '-10');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_animation_file, spell_display, target_type, target_size, spell_effected, effect_type, effect_duration, hp_change_formula)
VALUES (5, 'Reave', 65032, 2366, 0, 1, 1, 2, 0, 0, '-25');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_animation_file, spell_display, target_type, target_size, spell_effected, effect_type, effect_duration, hp_change_formula)
VALUES (6, 'Chomp', 272730, 3252, 0, 1, 1, 2, 0, 0, '-30');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_animation_file, spell_display, target_type, target_size, spell_effected, effect_type, effect_duration, spell_damage_effects, hp_change_formula)
VALUES (7, 'Heal', 65046, 2369, 0, 0, 1, 5, 0, 0, 1, '20');

SET IDENTITY_INSERT spell_effects OFF;