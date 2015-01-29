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
  weapon_damage SMALLINT DEFAULT 0 NOT NULL,
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
  body_state SMALLINT DEFAULT 3 NOT NULL,
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

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, graphic_tile, graphic_file, stack_size)
VALUES (1, 7, 'Gold', 20, 0, 331900, 2275, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, body_state)
VALUES (2, 3, 'Stick', 3, 2, 16, 331405, 2270, 1, 4);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, item_slot, item_type, graphic_tile, graphic_file, graphic_equip)
VALUES (3, 2, 'Old Sack', 15, 15, 5, 10, 12, 331940, 2275, 6);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, item_slot, item_type, graphic_tile, graphic_file, spell_effect_id)
VALUES (4, 2, 'Commoner Ring', 15, 15, 5, 4, 0, 331221, 2268, 1);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip)
VALUES (5, 2, 'Nooblet Helmet', 20, 20, 10, 5, 0, 12, 332292, 2278, 52);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip)
VALUES (6, 2, 'Nooblet Shirt', 15, 10, 5, 5, 10, 12, 332205, 2278, 2);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, stat_ac, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip)
VALUES (7, 2, 'Nooblet Pants', 10, 5, 5, 11, 12, 332204, 2278, 1);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, stat_ac, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip)
VALUES (8, 2, 'Nooblet Shoes', 10, 2, 5, 12, 12, 332213, 2278, 2);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_level, item_slot, item_type, lore, bindonpickup, graphic_tile, graphic_file, graphic_equip, spell_effect_id)
VALUES (9, 2, 'Doom Helmet', 2500, 2500, 500, 100, 100, 100, 100, 50, 0, 12, 1, 1, 332294, 2278, 53, 2);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_description, player_hp, stat_str, stat_sta, stat_dex, stat_int, min_level, weapon_damage, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, body_state)
VALUES (10, 3, 'Hobos Can', 'Smells like Beer', 0, 1, 1, 1, 1, 5, 5, 2, 16, 50, 331350, 2269, 60, 4);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_str, stat_sta, stat_dex, stat_int, min_level, weapon_damage, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, body_state)
VALUES (11, 3, 'Tiny Dagger', 2, 2, 2, 2, 7, 7, 2, 18, 100, 331379, 2269, 11, 4);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_str, stat_sta, min_level, weapon_damage, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, body_state)
VALUES (12, 3, 'Blunt Sword', 2, 2, 7, 8, 2, 14, 100, 331312, 2269, 9, 4);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, stat_sta, min_level, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip)
VALUES (13, 2, 'Wooden Shield', 20, 1, 7, 1, 16, 100, 332225, 2278, 21);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, spell_effect_id)
VALUES (14, 2, 'Mount', 13, 10, 332524, 2281, 120, 3);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_level, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, stack_size)
VALUES (15, 2, 'Helmet of the Cow Slayer', 50, 50, 20, 3, 1, 3, 3, 10, 0, 11, 500, 332215, 2278, 12, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_level, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, stack_size)
VALUES (16, 2, 'Shirt of the Cow Slayer', 30, 20, 10, 2, 1, 2, 2, 10, 10, 11, 500, 332212, 2278, 4, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_level, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, stack_size)
VALUES (17, 2, 'Pants of the Cow Slayer', 25, 15, 10, 2, 1, 2, 2, 10, 11, 11, 500, 332204, 2278, 1, 235, 108, 88, 150, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_level, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, stack_size)
VALUES (18, 2, 'Boots of the Cow Slayer', 20, 10, 5, 1, 1, 1, 1, 10, 12, 11, 500, 332213, 2278, 2, 226, 81, 21, 150, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, item_value, graphic_tile, graphic_file, learn_spell_id)
VALUES (19, 4, 'Lightning Strike', 20, 0, 20, 331907, 2275, 1);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, item_value, graphic_tile, graphic_file, learn_spell_id)
VALUES (20, 4, 'Reave', 20, 0, 20, 331907, 2275, 2);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, item_value, graphic_tile, graphic_file, learn_spell_id)
VALUES (21, 4, 'Chomp', 20, 0, 20, 331907, 2275, 3);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, item_value, graphic_tile, graphic_file, learn_spell_id)
VALUES (22, 4, 'Heal', 20, 0, 20, 331907, 2275, 4);




SET IDENTITY_INSERT item_templates OFF;