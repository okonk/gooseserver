USE IllutiaGoose;

DROP TABLE item_templates;
CREATE TABLE item_templates (
  item_template_id INT IDENTITY(1, 1) NOT NULL,
  item_usetype SMALLINT NOT NULL,
  item_name VARCHAR(64) NOT NULL,
  item_description VARCHAR(64) DEFAULT '' NOT NULL,
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
  weapon_damage SMALLINT DEFAULT 1 NOT NULL,
  weapon_delay SMALLINT DEFAULT 10 NOT NULL,
  item_slot SMALLINT NOT NULL,
  item_type SMALLINT NOT NULL,
  item_value BIGINT DEFAULT 0 NOT NULL,
  lore CHAR(1) DEFAULT '0' NOT NULL,
  bindonpickup CHAR(1) DEFAULT '0' NOT NULL,
  bindonequip CHAR(1) DEFAULT '0' NOT NULL,
  event CHAR(1) DEFAULT '0' NOT NULL,
  graphic_tile INT NOT NULL,
  graphic_file INT NOT NULL,
  graphic_equip SMALLINT DEFAULT 0 NOT NULL,
  graphic_r SMALLINT DEFAULT 0 NOT NULL,
  graphic_g SMALLINT DEFAULT 0 NOT NULL,
  graphic_b SMALLINT DEFAULT 0 NOT NULL,
  graphic_a SMALLINT DEFAULT 0 NOT NULL,
  class_restrictions BIGINT DEFAULT 0 NOT NULL,
  stack_size SMALLINT DEFAULT 1 NOT NULL,
  body_state SMALLINT DEFAULT 1 NOT NULL,
  spell_effect_id INT DEFAULT 0 NOT NULL,
  spell_effect_chance DECIMAL(9,4) DEFAULT 100 NOT NULL,
  learn_spell_id INT DEFAULT 0 NOT NULL,
  credits_value INT DEFAULT 0 NOT NULL,
  
  PRIMARY KEY(item_template_id)
);

/*
class_restrictions:

32 16 08 04 02 01
 p  m  w  r  c  0
 
value = 63 - number above class name
 
class		id	class_restriction value
commoner	1	61
rogue		2	59
warrior		3	55
magus		4	47
priest		5	31

*/

SET IDENTITY_INSERT item_templates ON;

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	graphic_tile, graphic_file, graphic_equip, stack_size) 
VALUES (1, 2, 'Gold', 0, 0, 331900, 2275, 0, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_file, graphic_equip) 
VALUES (2, 3, 'Scroll: Healing', 0, 0, 1, 100, 31, 1, '1', 331907, 2275, 0);

SET IDENTITY_INSERT item_templates OFF;