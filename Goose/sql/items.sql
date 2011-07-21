USE Goose;

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
	graphic_tile, graphic_equip, stack_size) 
VALUES (1, 2, 'Gold', 0, 0, 120100, 0, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_ac, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip) 
VALUES (2, 0, 'Old Rags', 3, 10, 1, 10, 120241, 4);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state) 
VALUES (3, 0, 'Stick', 3, 2, 4, 10, 120014, 6, 4);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, stack_size, spell_effect_id)
VALUES (4, 1, 'Small Health Potion', 15, 0, 50, 120115, 0, 99, 1);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, stack_size, spell_effect_id) 
VALUES (5, 1, 'Small Mana Potion', 15, 0, 50, 120112, 0, 99, 2);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	weapon_damage, stat_str, player_hp, 
	item_value, graphic_tile, graphic_equip, body_state) 
VALUES (6, 0, 'Tin Can', 2, 5, 13, 1, 5, 120, 120020, 7, 4);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	graphic_tile, graphic_equip, stack_size, item_value) 
VALUES (7, 2, 'Rabbit Fur', 0, 0,120601, 0, 99, 40);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	graphic_tile, graphic_equip, stack_size, item_value) 
VALUES (8, 2, 'Rabbit Pelt', 0, 0, 120604, 0, 99, 80);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, player_hp, player_mp, 
	graphic_tile, graphic_equip) 
VALUES (9, 0, 'Bunny Ears', 0, 0, 20, 3, 20, 20, 120222, 13);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (10, 3, 'Scroll: Healing 1', 0, 0, 1, 100, 31, 1, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, class_restrictions, min_level) 
VALUES (11, 0, 'Small Hammer', 12, 2, 4, 500, 120039, 8, 4, 22, 5);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, class_restrictions, min_level) 
VALUES (12, 0, 'Wooden Stave', 9, 3, 7, 500, 120021, 5, 3, 38, 5);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, class_restrictions, min_level) 
VALUES (13, 0, 'Small Dagger', 12, 2, 6, 500, 120013, 1, 4, 34, 5);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, class_restrictions, min_level) 
VALUES (14, 0, 'Small Sword', 14, 2, 5, 500, 120015, 4, 4, 50, 5);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, class_restrictions, min_level) 
VALUES (15, 0, 'Stone Hammer', 15, 2, 4, 1500, 120039, 8, 4, 22, 15);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, class_restrictions, min_level) 
VALUES (16, 0, 'Hardwood Stave', 15, 3, 7, 1500, 120021, 5, 3, 38, 15);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, class_restrictions, min_level) 
VALUES (17, 0, 'Grim Dagger', 19, 2, 6, 1500, 120013, 1, 4, 34, 15);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, class_restrictions, min_level) 
VALUES (18, 0, 'Long Sword', 24, 2, 5, 1500, 120015, 4, 4, 50, 15);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	graphic_tile, graphic_equip, stack_size, item_value) 
VALUES (19, 2, 'Sun Flower', 0, 0, 120120, 0, 1, 50);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	graphic_tile, graphic_equip, stack_size, item_value) 
VALUES (20, 2, 'Pile of Crap', 0, 0, 120124, 0, 99, 20);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	graphic_tile, graphic_equip, stack_size, item_value) 
VALUES (21, 2, 'Rubber Ducky', 0, 0, 120111, 0, 1, 100);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (22, 3, 'Scroll: Fortify 1', 0, 0, 2, 600, 31, 6, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (23, 3, 'Scroll: Backstab 1', 0, 0, 3, 100, 59, 5, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (24, 3, 'Scroll: Taunt', 0, 0, 4, 100, 55, 1, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (25, 3, 'Scroll: Elemental Strike 1', 0, 0, 5, 100, 47, 1, '1', 120110, 0);
	
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (26, 3, 'Scroll: Elemental Strike 2', 0, 0, 6, 600, 47, 6, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (27, 3, 'Scroll: Arcane Shield 1', 0, 0, 7, 700, 47, 7, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (28, 3, 'Scroll: Elemental Strike 3', 0, 0, 8, 1100, 47, 11, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (29, 3, 'Scroll: Elemental Shield 1', 0, 0, 9, 1200, 47, 12, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (30, 3, 'Scroll: Teleportation', 0, 0, 10, 1400, 47, 14, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (31, 3, 'Scroll: Root', 0, 0, 11, 1500, 15, 15, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (32, 3, 'Scroll: Elemental Strike 4', 0, 0, 12, 1600, 47, 16, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (33, 3, 'Scroll: Snare', 0, 0, 13, 2000, 47, 20, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (34, 3, 'Scroll: Gate', 0, 0, 15, 1300, 15, 13, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (35, 3, 'Scroll: Regeneration 1', 0, 0, 16, 2300, 47, 23, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (36, 3, 'Scroll: Elemental Strike 5', 0, 0, 14, 2100, 47, 21, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (37, 3, 'Scroll: Bind Self', 0, 0, 17, 2300, 15, 23, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (38, 3, 'Scroll: Group Teleportation', 0, 0, 18, 2500, 47, 25, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (39, 3, 'Scroll: Elemental Strike 6', 0, 0, 19, 2600, 47, 26, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (40, 3, 'Scroll: Rampant Rage', 0, 0, 20, 100, 55, 5, '1', 120110, 0);
	
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	weapon_damage, stat_dex, player_hp, 
	item_value, graphic_tile, graphic_equip, body_state, min_level, class_restrictions, lore) 
VALUES (41, 0, 'Winter Blade', 2, 6, 26, 5, 5, 500, 120211, 27, 4, 12, 51, '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, stack_size, spell_effect_id)
VALUES (42, 1, 'Health Potion', 15, 0, 150, 120116, 0, 99, 31);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, stack_size, spell_effect_id) 
VALUES (43, 1, 'Mana Potion', 15, 0, 150, 120113, 0, 99, 32);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, stack_size, spell_effect_id)
VALUES (44, 1, 'Large Health Potion', 15, 0, 300, 120117, 0, 99, 33);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, stack_size, spell_effect_id) 
VALUES (45, 1, 'Large Mana Potion', 15, 0, 300, 120114, 0, 99, 34);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_ac, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, min_level, stat_sta, player_hp, player_mp, lore) 
VALUES (46, 0, 'Frozen Mittens', 5, 9, 0, 100, 120210, 0, 10, 1, 10, 10, '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_ac, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, min_level, player_hp, lore, spell_effect_id) 
VALUES (47, 0, 'Cloak of Chilling Speed', 10, 7, 0, 250, 110034, 0, 15, 20, '1', 73);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_ac, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, min_level, player_hp, player_mp, stat_int, lore, class_restrictions) 
VALUES (48, 0, 'Blizzard Robes', 30, 10, 1, 500, 120241, 14, 12, 10, 30, 5, '1', 15);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_ac, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, min_level, player_hp, player_mp, stat_int, stat_str, stat_sta, stat_dex, lore)
VALUES (49, 0, 'Hair Bow', 10, 0, 1, 100, 120008, 3, 15, 10, 10, 2, 2, 2, 2, '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_ac, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, min_level, player_hp, player_mp, stat_str, stat_sta, lore, class_restrictions)
VALUES (50, 0, 'Cow Skull Helmet', 20, 0, 0, 200, 120030, 4, 15, 20, 10, 5, 2, '1', 51);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	graphic_tile, graphic_equip, stack_size, item_value) 
VALUES (51, 2, 'Dollar Bill', 0, 0, 120125, 0, 99, 100);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_ac, player_hp, player_mp, stat_str, stat_sta, stat_int, stat_dex, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, min_level) 
VALUES (52, 0, 'Shirt #297741384', 20, 20, 20, 3, 3, 3, 3, 10, 1, 0, 120040, 12, 25);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, min_level, stat_ac, stat_str, player_hp) 
VALUES (53, 0, 'Chicken Leg', 23, 2, 4, 0, 120012, 2, 4, 20, 10, 10, 20);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	graphic_tile, graphic_equip, stack_size, item_value) 
VALUES (54, 2, 'Taco', 0, 0, 120108, 0, 99, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, min_level, stat_ac, stat_str, player_hp, class_restrictions, lore) 
VALUES (55, 0, 'Encased Blade of Wet', 90, 2, 5, 5000, 120213, 19, 4, 45, 50, 30, 100, 55, '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, min_level, stat_ac, stat_str, stat_dex, player_hp, class_restrictions, lore) 
VALUES (56, 0, 'Encased Claw of Wet', 77, 2, 5, 5000, 120050, 15, 1, 45, 25, 10, 25, 75, 51, '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, min_level, stat_ac, stat_int, player_hp, player_mp, class_restrictions, lore) 
VALUES (57, 0, 'Staff of Frozen Rain', 35, 3, 7, 0, 120228, 29, 3, 40, 10, 25, 30, 100, 15, '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	graphic_tile, graphic_equip, stack_size, item_value) 
VALUES (58, 2, 'Gem of Power', 0, 0, 120502, 0, 99, 100);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, graphic_tile, graphic_equip) 
VALUES (59, 3, 'Scroll: Snowman Illusion', 0, 0, 29, 200, 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	graphic_tile, graphic_equip, stack_size, item_value) 
VALUES (60, 2, 'Ruby', 0, 0, 120401, 0, 5, 100);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (61, 3, 'Scroll: Healing 2', 0, 0, 30, 1100, 31, 11, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (62, 3, 'Scroll: Healing 3', 0, 0, 31, 2100, 31, 21, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (63, 3, 'Scroll: Healing 4', 0, 0, 32, 3100, 31, 31, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (64, 3, 'Scroll: Healing 5', 0, 0, 33, 1000, 31, 41, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (65, 3, 'Scroll: Fortify 2', 0, 0, 34, 1600, 31, 16, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (66, 3, 'Scroll: Fortify 3', 0, 0, 35, 2600, 31, 26, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (67, 3, 'Scroll: Fortify 4', 0, 0, 36, 3600, 31, 36, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (68, 3, 'Scroll: Fortify 5', 0, 0, 37, 1000, 31, 46, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (69, 3, 'Scroll: Strength 1', 0, 0, 38, 900, 31, 9, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (70, 3, 'Scroll: Strength 2', 0, 0, 39, 1900, 31, 19, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (71, 3, 'Scroll: Strength 3', 0, 0, 40, 2900, 31, 29, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (72, 3, 'Scroll: Strength 4', 0, 0, 41, 1000, 31, 39, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (73, 3, 'Scroll: Strength 5', 0, 0, 42, 1000, 31, 49, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (74, 3, 'Scroll: Stamina 1', 0, 0, 43, 1200, 31, 12, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (75, 3, 'Scroll: Stamina 2', 0, 0, 44, 2200, 31, 22, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (76, 3, 'Scroll: Stamina 3', 0, 0, 45, 3200, 31, 32, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (77, 3, 'Scroll: Stamina 4', 0, 0, 46, 1000, 31, 42, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (78, 3, 'Scroll: Intelligence 1', 0, 0, 47, 2500, 31, 25, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (79, 3, 'Scroll: Intelligence 2', 0, 0, 48, 3500, 31, 35, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (80, 3, 'Scroll: Dexterity 1', 0, 0, 49, 1000, 31, 37, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (81, 3, 'Scroll: Dexterity 2', 0, 0, 50, 1000, 31, 47, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (82, 3, 'Scroll: Mana Regeneration 1', 0, 0, 51, 3400, 31, 34, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (83, 3, 'Scroll: Mana Regeneration 2', 0, 0, 52, 500, 31, 48, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (84, 3, 'Scroll: See Invisible', 0, 0, 53, 1800, 31, 18, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (85, 3, 'Scroll: Sacrifice', 0, 0, 54, 3400, 31, 50, '1', 120110, 0);
	
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (86, 3, 'Rune: Fearsome Lash', 0, 0, 55, 0, 59, 50, '1', 120144, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (87, 3, 'Rune: Sunder of Spirits', 0, 0, 56, 0, 55, 50, '1', 120145, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, stack_size, spell_effect_id, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (88, 1, 'Hair Dye: Black', 15, 0, 100, 121122, 0, 99, 65, 0, 0, 0, 180);
	
/* Helmets */
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, player_hp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (89, 0, 'Bronze Helmet', 0, 3, 13, 0, 0, 120018, 1, 18, 51, 800, 20, 65, 30, 160);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, player_hp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore, event) 
VALUES (90, 0, 'Champions Helmet', 0, 3, 36, 6, 6, 0, 120023, 2, 50, 55, 0, '1', '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, player_hp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore, event) 
VALUES (91, 0, 'Cloth Cap', 0, 1, 4, 0, 0, 0, 120018, 1, 1, 0, 200, '0', '0');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, player_hp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore, event) 
VALUES (92, 0, 'Curious Skull Helmet', 0, 1, 77, 0, 0, 0, 120019, 1, 20, 55, 35000, '1', '0');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_int, stat_sta, player_hp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore, event) 
VALUES (93, 0, 'Deceivers Helmet', 0, 3, 29, 6, 6, 0, 120022, 10, 50, 59, 0, '1', '0');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_int, stat_sta, stat_dex, stat_str, player_mp, player_hp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore, event, graphic_a) 
VALUES (94, 0, 'Devastators Helmet', 0, 3, 95, 15, 15, 15, 15, 100, 100, 120030, 4, 49, 51, 0, '1', '0', 160);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_int, stat_sta, stat_dex, stat_str, player_mp, player_hp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore, event, graphic_r, graphic_g, graphic_b, graphic_a, res_fire, res_water, res_earth, res_spirit, res_air) 
VALUES (95, 0, 'Gold Helmet', 0, 3, 100, 0, 0, 5, 5, 50, 50, 120251, 21, 50, 51, 0, '0', '0', 231, 223, 107, 160, 2, 2, 2, 2, 2);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_int, stat_sta, stat_str, stat_dex, player_mp, 
	graphic_tile, graphic_equip, min_level, item_value, lore, event) 
VALUES (96, 0, 'Hays Tail', 0, 1, 15, 5, 5, 5, 5, 50, 120051, 12, 1, 0, '1', '0');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, player_hp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (97, 0, 'Iron Helmet', 0, 3, 19, 0, 0, 120018, 1, 26, 51, 1200, 70, 70, 70, 140);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, player_hp, 
	graphic_tile, graphic_equip, min_level, item_value, lore, event, class_restrictions) 
VALUES (98, 0, 'Leather Cap', 0, 1, 7, 0, 0, 0, 120018, 1, 10, 400, '0', '0', 19);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_int, stat_dex, player_hp, player_mp,  
	graphic_tile, graphic_equip, min_level, item_value, lore, event, class_restrictions, graphic_r, graphic_g, graphic_b, graphic_a, spell_effect_id, credits_value) /* Donation */
VALUES (99, 0, 'Lucky Laurels', 0, 1, 50, 10, 10, 10, 10, 75, 150, 120288, 28, 1, 300000, '0', '0', 0, 24, 81, 33, 160, 71, 10);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_int, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore, event) 
VALUES (100, 0, 'Priests Crown', 0, 1, 24, 2, 2, 2, 120031, 8, 40, 31, 0, '0', '0');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_int, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore, event) 
VALUES (101, 0, 'Silk Cap', 0, 1, 8, 0, 0, 0, 120033, 6, 20, 15, 800, '0', '0');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_int, stat_dex, player_hp, player_mp,  
	graphic_tile, graphic_equip, min_level, item_value, lore, event, class_restrictions, graphic_r, graphic_g, graphic_b, graphic_a, spell_effect_id) 
VALUES (102, 0, 'Spicy Laurels', 0, 1, 45, 0, 0, 20, 0, 150, 0, 120288, 28, 45, 0, '1', '0', 0, 148, 48, 49, 160, 71);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, player_hp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (103, 0, 'Steel Helmet', 0, 3, 25, 0, 0, 120018, 1, 34, 55, 2000, 100, 100, 100, 100);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_int, stat_dex, player_hp, player_mp,
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore, event, spell_effect_id) 
VALUES (104, 0, 'True Ewe', 0, 1, 55, 5, 5, 5, 5, 25, 25, 120051, 24, 50, 0, 0, '1', '0', 74);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_int, stat_dex, player_hp, player_mp,
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore, event, graphic_r, graphic_g, graphic_b, graphic_a, spell_effect_id, credits_value) /* Donation */
VALUES (105, 0, 'Wolfs Essence', 0, 1, 100, 5, 5, 5, 5, 100, 100, 120051, 24, 1, 0, 50000, '0', '0', 152, 88, 196, 170, 182, 5);


/* Legs */
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, player_hp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (106, 0, 'Bronze Legplates', 11, 3, 20, 0, 0, 120037, 5, 18, 51, 1000, 250, 150, 50, 140);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_int, stat_dex, player_hp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value) 
VALUES (107, 0, 'Champions Legplates', 11, 3, 75, 4, 4, 4, 4, 0, 120038, 2, 50, 55, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_int, stat_dex, player_hp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value) 
VALUES (108, 0, 'Cloth Leggings', 11, 1, 7, 0, 0, 0, 0, 0, 120029, 3, 1, 0, 250);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_int, stat_dex, player_hp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (109, 0, 'Gold Legplates', 11, 3, 75, 7, 7, 0, 0, 75, 120037, 2, 50, 51, 0, 231, 223, 107, 160);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, player_hp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value) 
VALUES (110, 0, 'Iron Legplates', 11, 3, 29, 0, 0, 120036, 5, 26, 51, 1500);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_int, stat_dex, player_hp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value) 
VALUES (111, 0, 'Leather Leggings', 11, 1, 9, 0, 0, 0, 0, 0, 120029, 3, 10, 19, 500);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_int, stat_dex, player_hp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (112, 0, 'Lucky Legplates', 11, 3, 35, 3, 3, 0, 0, 50, 120037, 2, 1, 0, 5000, 24, 81, 33, 160);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_int, stat_dex, player_hp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value) 
VALUES (113, 0, 'Silk Leggings', 11, 1, 15, 0, 0, 0, 0, 0, 120029, 3, 20, 15, 1000);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, player_hp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (114, 0, 'Steel Legplates', 11, 3, 40, 0, 0, 120036, 5, 34, 55, 2500, 255, 255, 255, 70);


/* Shields */
INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, min_level, stat_ac, lore) 
VALUES (115, 0, 'Moon Shield', 0, 1, 0, 0, 120259, 57, 4, 45, 25, '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, min_level, stat_ac, lore, class_restrictions) 
VALUES (116, 0, 'Fiber Buckler', 0, 1, 0, 2000, 120258, 41, 4, 20, 30, '0', 59);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, min_level, stat_ac, lore, class_restrictions, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (117, 0, 'Firebrand Buckler', 0, 1, 0, 3500, 120258, 41, 4, 35, 60, '0', 59, 189, 93, 90, 160);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, min_level, stat_ac, player_mp, lore, res_fire, res_water, res_earth, res_spirit, res_air, spell_effect_id,
	stat_sta, stat_str, stat_int, stat_dex, credits_value) 
VALUES (118, 0, 'Firebreather Shield', 0, 1, 0, 500000, 120281, 54, 4, 1, 75, 100, '0', 10, 10, 10, 10, 10, 71, 3, 3, 3, 3, 10);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, min_level, stat_ac, player_mp, lore, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions) 
VALUES (119, 0, 'Firebrand Guard', 0, 1, 0, 3500, 120302, 43, 4, 35, 40, 40, '0', 189, 93, 90, 160, 15);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, min_level, stat_ac, player_mp, lore, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions) 
VALUES (120, 0, 'Light Guard', 0, 1, 0, 2000, 120302, 43, 4, 20, 20, 20, '0', 66, 69, 189, 160, 15);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, min_level, stat_ac, lore, class_restrictions) 
VALUES (121, 0, 'Kite Shield', 0, 1, 0, 2000, 120259, 42, 4, 20, 50, '0', 55);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, min_level, stat_ac, lore, class_restrictions) 
VALUES (122, 0, 'Wooden Buckler', 0, 1, 0, 500, 120258, 41, 4, 5, 10, '0', 0);


/* Shoes */
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, player_hp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (123, 0, 'Bronze Boots', 12, 3, 12, 0, 0, 120042, 4, 18, 51, 800, 250, 150, 50, 140);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, player_hp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value) 
VALUES (124, 0, 'Cloth Shoes', 12, 1, 4, 0, 0, 120005, 1, 1, 0, 200);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_int, stat_dex, player_hp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value) 
VALUES (125, 0, 'Deceivers Boots', 12, 3, 29, 3, 3, 3, 3, 0, 120041, 4, 50, 59, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_int, stat_dex, player_hp, player_mp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore, graphic_a) 
VALUES (126, 0, 'Devastators Boots', 12, 3, 50, 5, 5, 5, 5, 50, 50, 120041, 4, 49, 0, 0, '1', 160);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_int, stat_dex, player_hp, player_mp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (127, 0, 'Gold Boots', 12, 3, 50, 5, 5, 0, 0, 50, 50, 120042, 2, 50, 51, 0, '0', 231, 223, 107, 160);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, player_hp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value) 
VALUES (128, 0, 'Iron Boots', 12, 3, 19, 0, 0, 120041, 4, 26, 51, 1200);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, player_hp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value) 
VALUES (129, 0, 'Leather Boots', 12, 1, 10, 0, 0, 120005, 1, 10, 19, 400);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, player_hp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (130, 0, 'Lucky Boots', 12, 3, 55, 2, 2, 50, 120042, 2, 1, 0, 3000, 24, 81, 33, 160);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, player_hp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value) 
VALUES (131, 0, 'Silk Slippers', 12, 1, 7, 0, 0, 120005, 1, 20, 0, 800);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_int, stat_sta, stat_dex, player_hp, player_mp,
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (132, 0, 'Slippers of the Poo Flinger', 12, 1, 18, 5, 5, 10, 10, 50, 25, 120005, 1, 15, 0, 0, 132, 77, 49, 160);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, player_hp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (133, 0, 'Steel Boots', 12, 3, 25, 0, 0, 120041, 4, 34, 55, 2000, 255, 255, 255, 70);

/* Accessories */
INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, stat_ac, stat_str, stat_dex, stat_int, stat_sta, player_hp, player_mp, min_level, event, lore, spell_effect_id) /* Donation */
VALUES (134, 0, 'Azkuros Gloves', 0, 9, 0, 0, 120210, 0, 5, 5, 5, 5, 50, 50, 1, '1', '1', 66);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, stat_ac, stat_str, stat_dex, stat_int, stat_sta, player_hp, player_mp, min_level, event, lore, res_fire, res_water, res_earth, res_spirit, res_air) 
VALUES (135, 0, 'Beefs Immortality', 0, 4, 0, 0, 120054, 80, 20, 20, 20, 20, 600, 600, 50, '0', '1', 3, 3, 3, 3, 3);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, stat_ac, stat_str, stat_dex, stat_int, stat_sta, player_hp, player_mp, min_level, event, lore, res_fire, res_water, res_earth, res_spirit, res_air, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (136, 0, 'Beefs Protection', 0, 7, 0, 0, 110034, 60, 10, 10, 10, 10, 200, 200, 50, '0', '1', 5, 5, 5, 5, 5, 255, 255, 255, 100);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, stat_ac, stat_str, stat_dex, stat_int, stat_sta, player_hp, player_mp, min_level, event, lore, res_fire, res_water, res_earth, res_spirit, res_air, spell_effect_id) 
VALUES (137, 0, 'Bracelet of Valiance', 0, 4, 0, 0, 120059, 0, 0, 0, 0, 0, 0, 0, 40, '0', '1', 0, 0, 0, 0, 0, 69);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, stat_ac, stat_str, stat_dex, stat_int, stat_sta, player_hp, player_mp, min_level, event, lore, spell_effect_id) 
VALUES (138, 0, 'Candy Necklace', 0, 5, 0, 700000, 120082, 0, 5, 5, 5, 5, 300, 300, 50, '1', '1', 71);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, stat_ac, stat_str, stat_dex, stat_int, stat_sta, player_hp, player_mp, min_level, event, lore, spell_effect_id, credits_value) /* Donation */
VALUES (139, 0, 'Divine Pauldrons', 0, 6, 0, 50000, 120278, 60, 0, 0, 6, 6, 0, 90, 0, '1', '1', 66, 5);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, stat_ac, stat_str, stat_dex, stat_int, stat_sta, player_hp, player_mp, min_level, event, lore, res_fire, res_water, res_earth, res_spirit, res_air) 
VALUES (140, 0, 'Gloves of the Poo Flinger', 0, 9, 0, 0, 120209, 25, 20, 10, 5, 10, 150, 75, 40, '0', '0', 10, 10, 10, 5, 10);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, stat_ac, stat_str, stat_dex, stat_int, stat_sta, player_hp, player_mp, min_level, event, lore, class_restrictions) 
VALUES (141, 0, 'Harvest Medallion', 0, 5, 0, 50, 121117, 0, 0, 0, 1, 0, 0, 30, 18, '0', '1', 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, stat_ac, stat_str, stat_dex, stat_int, stat_sta, player_hp, player_mp, min_level, event, lore) 
VALUES (142, 0, 'Leather Gloves', 0, 9, 0, 0, 120209, 5, 1, 1, 1, 1, 25, 25, 1, '0', '0');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, stat_ac, stat_str, stat_dex, stat_int, stat_sta, player_hp, player_mp, min_level, event, lore, graphic_r, graphic_g, graphic_b, graphic_a, spell_effect_id, credits_value) /* Donation */
VALUES (143, 0, 'Lucky Necklace', 0, 5, 0, 800000, 120082, 50, 5, 5, 5, 5, 50, 50, 1, '0', '0', 24, 81, 33, 160, 71, 10);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, stat_ac, stat_str, stat_dex, stat_int, stat_sta, player_hp, player_mp, min_level, event, lore, res_fire, res_water, res_earth, res_spirit, res_air, spell_effect_id) 
VALUES (144, 0, 'Ring of Valiance', 0, 4, 0, 0, 120061, 0, 0, 0, 0, 0, 0, 0, 40, '0', '1', 0, 0, 0, 0, 0, 67);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, stat_ac, stat_str, stat_dex, stat_int, stat_sta, player_hp, player_mp, min_level, event, lore, res_fire, res_water, res_earth, res_spirit, res_air) 
VALUES (145, 0, 'Savage Belt', 0, 8, 0, 0, 120093, 50, 5, 5, 5, 5, 25, 25, 50, '0', '0', 0, 0, 0, 0, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, stat_ac, stat_str, stat_dex, stat_int, stat_sta, player_hp, player_mp, min_level, event, lore, res_fire, res_water, res_earth, res_spirit, res_air) 
VALUES (146, 0, 'Dragon Scale Belt', 0, 8, 0, 0, 120096, 40, 5, 0, 15, 15, 0, 0, 43, '0', '1', 0, 0, 0, 0, 0);


/* Chest */
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_ac, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, min_level, player_hp, player_mp, stat_dex, lore, class_restrictions, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (147, 0, 'Blushing Coat', 35, 10, 1, 75000, 120010, 14, 30, 0, 0, 16, '1', 37, 255, 128, 255, 160);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, player_hp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (148, 0, 'Bronze Chestplate', 10, 3, 35, 0, 0, 120011, 10, 18, 51, 2000, 250, 150, 50, 140);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_dex, player_hp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (149, 0, 'Champions Chestplate', 10, 3, 100, 8, 8, 4, 0, 120034, 10, 50, 55, 0, 66, 69, 189, 150);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_dex, player_hp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value) 
VALUES (150, 0, 'Cloth Tunic', 10, 1, 10, 0, 0, 0, 0, 120003, 1, 1, 0, 400);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_int, stat_dex, player_hp, player_mp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore) 
VALUES (151, 0, 'Devastators Robes', 10, 1, 150, 15, 15, 50, 200, 120009, 13, 49, 15, 0, '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_dex, player_hp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (152, 0, 'Gold Chestplate', 10, 3, 250, 15, 15, 10, 150, 120246, 21, 50, 51, 0, 231, 223, 107, 160);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_dex, stat_int, player_hp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (153, 0, 'High Priests Tunic', 10, 1, 70, 5, 5, 5, 5, 0, 120025, 11, 50, 31, 0, 49, 65, 148, 160);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, player_hp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value) 
VALUES (154, 0, 'Iron Chestplate', 10, 3, 40, 0, 0, 120011, 10, 26, 51, 3500);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_dex, player_hp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value) 
VALUES (155, 0, 'Leather Tunic', 10, 1, 15, 0, 0, 0, 0, 120003, 1, 10, 19, 1000);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_dex, stat_int, player_hp, player_mp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (156, 0, 'Lucky Robes', 10, 1, 80, 5, 5, 5, 5, 50, 50, 120255, 23, 1, 0, 1000, 24, 81, 33, 160);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_dex, stat_int, player_hp, player_mp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (157, 0, 'Lucky Chestplate', 10, 1, 75, 5, 5, 5, 5, 100, 25, 120246, 21, 1, 0, 2000, 24, 81, 33, 160);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_int, stat_dex, player_hp, player_mp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore) 
VALUES (158, 0, 'Silk Tunic', 10, 1, 20, 0, 0, 0, 0, 120002, 2, 20, 15, 2000, '0');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, player_hp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (159, 0, 'Steel Chestplate', 10, 3, 60, 0, 0, 120011, 10, 34, 55, 5000, 255, 255, 255, 70);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_int, stat_dex, player_hp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, graphic_r, graphic_g, graphic_b, graphic_a, spell_effect_id) 
VALUES (160, 0, 'Thick Skin of the Boar', 10, 3, 60, 6, 6, 6, 6, 0, 120011, 10, 20, 0, 1000, 123, 48, 123, 160, 73);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_dex, stat_int, player_hp, player_mp,
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore, event, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (161, 0, 'Tunic of the Poo Flinger', 10, 1, 125, 25, 25, 25, 25, 300, 300, 120025, 11, 25, 0, 0, '1', '1', 132, 77, 49, 160);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_int, stat_dex, stat_sta, player_hp, player_mp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (162, 0, 'Whirling Robes', 10, 1, 190, 30, 5, 5, 80, 300, 120002, 2, 50, 15, 0, '1', 82, 138, 156, 190);


/* Additions */
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_int, stat_sta, stat_dex, player_hp, player_mp,
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, graphic_r, graphic_g, graphic_b, graphic_a, lore) 
VALUES (163, 0, 'Leggings of the Poo Flinger', 11, 1, 75, 15, 15, 15, 15, 150, 150, 120029, 3, 25, 0, 0, 132, 77, 49, 160, '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_int, stat_dex, player_hp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value) 
VALUES (164, 0, 'Champions Boots', 12, 3, 25, 3, 3, 0, 3, 50, 120043, 2, 50, 55, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_int, stat_dex, player_hp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value) 
VALUES (165, 0, 'Deceivers Legplates', 11, 3, 35, 5, 5, 0, 5, 75, 120036, 5, 50, 59, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_int, stat_dex, player_hp, player_mp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, graphic_a) 
VALUES (166, 0, 'Devastators Legplates', 11, 3, 100, 10, 10, 10, 10, 75, 75, 120036, 5, 49, 0, 0, 160);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_dex, player_hp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, graphic_a) 
VALUES (167, 0, 'Devastators Chestplate', 10, 3, 200, 15, 15, 15, 150, 120246, 21, 49, 51, 0, 160);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_dex, player_hp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value) 
VALUES (168, 0, 'Deceivers Chestplate', 10, 3, 55, 10, 10, 10, 100, 120011, 10, 50, 59, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, stack_size, spell_effect_id, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (169, 1, 'Hair Dye: Red', 15, 0, 100, 121122, 0, 99, 85, 155, 0, 0, 160);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, stack_size, spell_effect_id, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (170, 1, 'Hair Dye: Blue', 15, 0, 100, 121122, 0, 99, 86, 0, 0, 155, 160);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, stack_size, spell_effect_id, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (171, 1, 'Hair Dye: Grey', 15, 0, 100, 121122, 0, 99, 87, 0, 0, 0, 100);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, stack_size, spell_effect_id) 
VALUES (172, 1, 'Hair Cut: 1', 15, 0, 1000, 121104, 0, 1, 88);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, stack_size, spell_effect_id) 
VALUES (173, 1, 'Hair Cut: 2', 15, 0, 1000, 121104, 0, 1, 89);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, stack_size, spell_effect_id) 
VALUES (174, 1, 'Hair Cut: 3', 15, 0, 1000, 121104, 0, 1, 90);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, stack_size, spell_effect_id) 
VALUES (175, 1, 'Hair Cut: 4', 15, 0, 1000, 121104, 0, 1, 91);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, stack_size, spell_effect_id) 
VALUES (176, 1, 'Hair Cut: 5', 15, 0, 1000, 121104, 0, 1, 92);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, stack_size, spell_effect_id) 
VALUES (177, 1, 'Hair Cut: 6', 15, 0, 1000, 121104, 0, 1, 93);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, stack_size, spell_effect_id) 
VALUES (178, 1, 'Hair Cut: 7', 15, 0, 1000, 121104, 0, 1, 94);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, stack_size, spell_effect_id) 
VALUES (179, 1, 'Face: 1', 15, 0, 1000, 121103, 0, 1, 95);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, stack_size, spell_effect_id) 
VALUES (180, 1, 'Face: 2', 15, 0, 1000, 121103, 0, 1, 96);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, stack_size, spell_effect_id) 
VALUES (181, 1, 'Face: 3', 15, 0, 1000, 121103, 0, 1, 97);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, stack_size, spell_effect_id) 
VALUES (182, 1, 'Face: 4', 15, 0, 1000, 121103, 0, 1, 98);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, stack_size, spell_effect_id) 
VALUES (183, 1, 'Sexchange: Male', 15, 0, 1000, 120711, 0, 1, 99);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, stack_size, spell_effect_id) 
VALUES (184, 1, 'Sexchange: Female', 15, 0, 1000, 121104, 0, 1, 100);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (185, 3, 'Scroll: Arcane Shield 2', 0, 0, 57, 1700, 47, 17, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (186, 3, 'Scroll: Group Elemental Shield 1', 0, 0, 58, 1500, 47, 12, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (187, 3, 'Scroll: Invisibility', 0, 0, 59, 2800, 47, 28, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (188, 3, 'Scroll: Elemental Strike 7', 0, 0, 60, 3100, 47, 31, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (189, 3, 'Scroll: Elemental Shield 2', 0, 0, 61, 3200, 47, 32, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (190, 3, 'Scroll: Group Elemental Shield 2', 0, 0, 62, 4000, 47, 32, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (191, 3, 'Scroll: Regeneration 2', 0, 0, 63, 3300, 47, 33, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (192, 3, 'Scroll: Bind Other', 0, 0, 64, 3300, 47, 33, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (193, 3, 'Scroll: Otherlands Teleport', 0, 0, 65, 3400, 47, 34, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (194, 3, 'Scroll: Group Otherlands Teleport', 0, 0, 66, 3500, 47, 35, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (195, 3, 'Scroll: Elemental Strike 8', 0, 0, 67, 3600, 47, 36, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (196, 3, 'Scroll: Arcane Shield 4', 0, 0, 68, 3700, 47, 37, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (197, 3, 'Scroll: Elemental Strike 9', 0, 0, 69, 4100, 47, 41, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (198, 3, 'Scroll: Regeneration 3', 0, 0, 70, 4300, 47, 43, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (199, 3, 'Scroll: Elemental Strike 10', 0, 0, 71, 4600, 47, 46, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (200, 3, 'Scroll: Arcane Shield 5', 0, 0, 72, 4700, 47, 47, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (201, 3, 'Scroll: Arcane Shield 3', 0, 0, 73, 2700, 47, 27, '1', 120110, 0);
	
INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, class_restrictions, lore, min_level, stat_str, stat_sta) 
VALUES (202, 0, 'Bastard Sword', 40, 2, 5, 2700, 120015, 4, 4, 55, '0', 27, 5, 2);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, class_restrictions, lore, min_level, stat_int, stat_sta) 
VALUES (203, 0, 'Brilliant Hammer', 19, 2, 5, 2700, 120039, 8, 4, 31, '0', 27, 5, 2);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, class_restrictions, lore, min_level, stat_str, stat_sta, stat_dex, player_hp, player_mp) 
VALUES (204, 0, 'Contraband Dagger', 33, 2, 6, 25000, 120704, 35, 4, 59, '1', 25, 2, 4, 4, 50, 10);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, class_restrictions, lore, min_level, stat_str, stat_sta, stat_dex, player_hp, player_mp) 
VALUES (205, 0, 'Deceivers Dagger', 85, 2, 6, 0, 120047, 11, 4, 59, '0', 50, 5, 5, 10, 0, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, class_restrictions, lore, min_level, stat_ac, stat_str, stat_sta, stat_dex, player_hp, spell_effect_id) 
VALUES (206, 0, 'Devastating Dragon Tooth Sword', 120, 2, 5, 0, 120242, 39, 4, 55, '1', 50, 0, 10, 10, 10, 100, 115);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, class_restrictions, lore, min_level, stat_int, stat_sta, stat_dex, player_hp, player_mp) 
VALUES (207, 0, 'Elemental Stave', 35, 3, 8, 0, 120017, 3, 3, 47, '1', 50, 10, 5, 10, 0, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, class_restrictions, lore, min_level, stat_str, stat_sta) 
VALUES (208, 0, 'High Quality Walde', 48, 2, 5, 1500, 120263, 46, 4, 55, '0', 35, 0, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, class_restrictions, lore, min_level, stat_int, stat_sta, stat_dex, stat_str, player_hp, player_mp, credits_value) /* Donation */
VALUES (209, 0, 'Lucky Spear', 100, 3, 9, 20000, 120264, 45, 3, 0, '0', 1, 5, 5, 5, 5, 250, 50, 5);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, class_restrictions, lore, min_level, stat_str, stat_int, stat_sta, stat_dex, player_hp, player_mp, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (210, 0, 'Lucky Staff', 50, 3, 7, 1000, 120228, 29, 3, 0, '0', 1, 5, 5, 5, 5, 5, 50, 24, 81, 33, 160);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, class_restrictions, lore, min_level, stat_str, stat_dex) 
VALUES (211, 0, 'Malignant Dagger', 34, 2, 6, 2700, 120013, 1, 4, 59, '0', 27, 2, 5);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, class_restrictions, lore, min_level, stat_int, stat_sta) 
VALUES (212, 0, 'Priests Hammer', 34, 2, 5, 0, 120049, 13, 4, 31, '0', 40, 7, 5);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, class_restrictions, lore, min_level, stat_int, stat_sta, stat_dex, stat_str, player_hp, player_mp, stat_ac) 
VALUES (213, 0, 'Scythe', 35 , 3, 8, 0, 120044, 10, 3, 0, '1', 30, 4, 4, 4, 4, 20, 20, 10);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, class_restrictions, lore, min_level, stat_str, stat_dex, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (214, 0, 'Searing Whip', 40, 2, 5, 0, 120052, 16, 4, 0, '0', 25, 20, 0, 115, 81, 33, 140);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, class_restrictions, lore, min_level, stat_int, stat_sta, stat_dex, player_hp, player_mp) 
VALUES (215, 0, 'Thicket Stave', 21, 3, 8, 2700, 120021, 5, 3, 47, '0', 27, 5, 0, 2, 0, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, class_restrictions, lore, min_level, stat_int, stat_sta, stat_dex, player_hp, player_mp, stat_ac) 
VALUES (216, 0, 'Devastating Birch Wood Staff', 42, 3, 7, 0, 120227, 28, 3, 15, '1', 50, 20, 10, 5, 50, 500, 50);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, min_level, player_hp, player_mp, stat_str, stat_sta, stat_dex, stat_int) 
VALUES (217, 0, 'Tiny Club', 80, 2, 4, 10000, 120014, 6, 4, 50, 30, 10, 10, 5, 5, 7);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, class_restrictions, lore, min_level, stat_int, stat_sta, stat_dex, player_hp, player_mp) 
VALUES (218, 0, 'Frays Cane', 18, 3, 7, 0, 120244, 40, 3, 15, '1', 50, 25, 10, 5, 0, 125);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, class_restrictions, lore, min_level, stat_str, stat_sta, stat_dex, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (219, 0, 'Nagan Sword', 70, 2, 5, 1500, 120015, 4, 4, 55, '1', 33, 15, 5, 2, 107, 73, 107, 120);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, class_restrictions, lore, min_level, stat_sta, stat_str, stat_dex, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (220, 0, 'Nagan Dagger', 63, 2, 6, 1500, 120013, 1, 4, 59, '1', 33, 3, 8, 10, 107, 73, 107, 120);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, class_restrictions, lore, min_level, stat_int, stat_sta, stat_dex, player_hp, player_mp, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (221, 0, 'Nagan Stave', 22, 3, 8, 0, 120017, 3, 3, 47, '1', 30, 10, 5, 2, 25, 100, 107, 73, 107, 160);	
	
INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, min_level, stat_ac, res_fire, lore, class_restrictions,
	graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (222, 0, 'Firebrand Shield', 0, 1, 0, 3500, 120259, 42, 4, 35, 100, 5, '0', 55, 150, 0, 0, 140);
	
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_ac, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip) 
VALUES (223, 0, 'Peasant Dress', 10, 10, 1, 100, 120027, 6);
	
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_int, stat_dex, player_hp, player_mp,  
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value) 
VALUES (224, 0, 'Priests Leggings', 11, 1, 25, 3, 3, 3, 3, 25, 50, 120001, 1, 40, 31, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_int, stat_dex, stat_sta, player_mp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value) 
VALUES (225, 0, 'Priests Shoes', 12, 1, 15, 0, 3, 0, 2, 30, 120005, 1, 40, 31, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_int, player_mp, player_hp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (226, 0, 'Priests Tunic', 10, 1, 30, 0, 4, 5, 50, 50, 120026, 7, 40, 31, 0, 49, 65, 148, 160);


INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_int, stat_dex, player_hp, player_mp,  
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value) 
VALUES (227, 0, 'Magus Leggings', 11, 1, 20, 0, 0, 5, 0, 25, 50, 120000, 6, 40, 47, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_int, stat_dex, stat_sta, player_mp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value) 
VALUES (228, 0, 'Magus Shoes', 12, 1, 15, 0, 4, 0, 2, 30, 120007, 5, 40, 47, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_int, player_mp, player_hp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value) 
VALUES (229, 0, 'Magus Tunic', 10, 1, 25, 0, 0, 10, 75, 25, 120025, 11, 40, 47, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_int, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore, event) 
VALUES (230, 0, 'Magus Crown', 0, 1, 20, 0, 2, 5, 120032, 5, 40, 47, 0, '0', '0');


INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, player_hp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore, event) 
VALUES (231, 0, 'Warriors Helmet', 0, 3, 28, 4, 4, 0, 120023, 2, 40, 55, 0, '0', '0');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_int, stat_dex, player_hp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value) 
VALUES (232, 0, 'Warriors Legplates', 11, 3, 50, 3, 3, 3, 3, 75, 120038, 2, 40, 55, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_int, stat_dex, player_hp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value) 
VALUES (233, 0, 'Warriors Boots', 12, 3, 18, 2, 2, 0, 2, 30, 120043, 2, 40, 55, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_dex, player_hp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (234, 0, 'Warriors Chestplate', 10, 3, 75, 5, 5, 3, 0, 120034, 10, 40, 55, 0, 66, 69, 189, 120);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_int, stat_sta, player_hp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore, event) 
VALUES (235, 0, 'Rogues Helmet', 0, 3, 19, 3, 4, 40, 120022, 10, 40, 59, 0, '0', '0');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_int, stat_dex, player_hp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value) 
VALUES (236, 0, 'Rogues Legplates', 11, 3, 25, 3, 4, 0, 3, 50, 120036, 5, 40, 59, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_int, stat_dex, player_hp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value) 
VALUES (237, 0, 'Rogues Boots', 12, 3, 19, 2, 2, 2, 2, 0, 120041, 4, 40, 59, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_dex, player_hp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value) 
VALUES (238, 0, 'Rogues Chestplate', 10, 3, 40, 7, 7, 7, 70, 120011, 10, 40, 59, 0);
	
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_int, stat_dex, player_hp, player_mp,  
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value) 
VALUES (239, 0, 'High Priests Leggings', 11, 1, 35, 5, 5, 5, 5, 50, 80, 120001, 1, 50, 31, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_int, stat_dex, stat_sta, player_mp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value) 
VALUES (240, 0, 'High Priests Shoes', 12, 1, 20, 0, 4, 0, 3, 50, 120005, 1, 50, 31, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_int, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore, event) 
VALUES (241, 0, 'High Priests Crown', 0, 1, 35, 4, 4, 4, 120031, 8, 50, 31, 0, '1', '0');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_int, player_mp, player_hp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (242, 0, 'High Priests Tunic', 10, 1, 45, 0, 6, 8, 100, 75, 120026, 7, 50, 31, 0, 49, 65, 148, 160);


INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_int, stat_dex, player_hp, player_mp,  
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value) 
VALUES (243, 0, 'Elemental Leggings', 11, 1, 30, 0, 0, 8, 0, 35, 100, 120000, 6, 50, 47, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_int, stat_dex, stat_sta, player_mp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value) 
VALUES (244, 0, 'Elemental Shoes', 12, 1, 20, 0, 6, 0, 3, 40, 120007, 5, 50, 47, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_int, player_mp, player_hp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value) 
VALUES (245, 0, 'Elemental Tunic', 10, 1, 40, 0, 0, 15, 125, 40, 120025, 11, 50, 47, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_int, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore, event) 
VALUES (246, 0, 'Elemental Crown', 0, 1, 30, 0, 3, 8, 120032, 5, 50, 47, 0, '1', '0');


INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_int, stat_dex, player_hp, player_mp,  
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (247, 0, 'Whirling Leggings', 11, 1, 75, 0, 3, 15, 0, 25, 120, 120000, 3, 50, 15, 0, 82, 138, 156, 190);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_int, stat_dex, stat_sta, player_mp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (248, 0, 'Whirling Slippers', 12, 1, 35, 0, 9, 0, 5, 75, 120005, 1, 50, 15, 0, 82, 138, 156, 190);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_int, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore, event, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (249, 0, 'Whirling Hat', 0, 1, 55, 0, 5, 13, 120033, 6, 50, 15, 0, '1', '0', 82, 138, 156, 190);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_int, stat_dex, stat_sta, player_hp, player_mp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (250, 0, 'Nagan Robes', 10, 1, 75, 20, 0, 3, 25, 200, 120002, 2, 50, 15, 0, '1', 0, 128, 0, 100);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, class_restrictions, lore, min_level, player_hp, player_mp, graphic_r, graphic_g, graphic_b, graphic_a, spell_effect_id,
	bindonequip, bindonpickup) 
VALUES (251, 0, 'Coral Sword', 50, 2, 5, 0, 120272, 51, 4, 0, '1', 50, 125, 125, 185, 40, 29, 160, 76, '1', '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, class_restrictions, lore, min_level, stat_ac, stat_str, stat_sta, stat_dex, player_hp, spell_effect_id, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (252, 0, 'Devastating Dagger of the Fox', 100, 2, 5, 0, 120211, 27, 4, 59, '1', 50, 0, 5, 7, 15, 100, 115, 231, 223, 107, 130);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, class_restrictions, lore, min_level, stat_str, stat_sta) 
VALUES (253, 0, 'Champions Blade', 110, 2, 5, 0, 120046, 12, 4, 55, '1', 50, 25, 10);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, stat_ac, stat_str, stat_dex, stat_int, stat_sta, player_hp, player_mp, min_level, event, lore) 
VALUES (254, 0, 'Cold Beaten Sleeves', 0, 6, 0, 0, 120233, 20, 0, 0, 0, 0, 20, 20, 20, '0', '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, stat_ac, stat_str, stat_dex, stat_int, stat_sta, player_hp, player_mp, min_level, event, lore, res_fire, res_water, res_earth, res_spirit, res_air) 
VALUES (255, 0, 'Spiked Belt of the Bunny', 0, 8, 0, 0, 120205, 10, 3, 3, 3, 3, 25, 15, 20, '0', '0', 0, 0, 0, 0, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_dex, stat_int, player_hp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value) 
VALUES (256, 0, 'Shirt of the Fallen Loser', 10, 1, 35, 2, 2, 2, 2, 25, 120040, 3, 20, 0, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, class_restrictions, lore, min_level, stat_str, stat_sta, stat_dex, player_hp, player_mp) 
VALUES (257, 0, 'Hays Claw', 90, 2, 6, 0, 120050, 15, 1, 51, '1', 50, 5, 4, 9, 0, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, class_restrictions, lore, min_level, stat_str, stat_sta, stat_dex, player_hp, player_mp) 
VALUES (258, 0, 'Bear Claw', 65, 2, 6, 0, 120050, 14, 4, 15, '1', 50, 3, 3, 7, 0, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, class_restrictions, lore, min_level, stat_str, stat_sta, stat_dex, player_hp, player_mp) 
VALUES (259, 0, 'Rusty Claw', 80, 2, 6, 0, 120050, 14, 4, 51, '1', 40, 3, 2, 3, 50, 50);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_int, stat_sta, player_hp, player_mp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value) 
VALUES (260, 0, 'Frays Flippers', 12, 1, 30, 5, 5, 5, 50, 100, 120229, 6, 0, 0, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, min_level, player_hp, player_mp, stat_str, stat_sta, stat_dex, stat_int) 
VALUES (261, 0, 'Beefs Fist', 100, 2, 4, 0, 120014, 0, 1, 50, 300, 300, 20, 10, 10, 10);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	graphic_tile, graphic_equip, stack_size, item_value, lore, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (262, 2, 'Lesser Essence of Earth', 0, 0, 120500, 0, 10, 0, '0', 128, 0, 0, 100);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	graphic_tile, graphic_equip, stack_size, item_value, lore, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (263, 2, 'Lesser Essence of Water', 0, 0, 120500, 0, 10, 0, '0', 0, 128, 255, 100);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	graphic_tile, graphic_equip, stack_size, item_value, lore, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (264, 2, 'Lesser Essence of Fire', 0, 0, 120500, 0, 10, 0, '0', 255, 0, 0, 100);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	graphic_tile, graphic_equip, stack_size, item_value, lore, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (265, 2, 'Lesser Essence of Air', 0, 0, 120500, 0, 10, 0, '0', 255, 255, 255, 130);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	graphic_tile, graphic_equip, stack_size, item_value, lore, bindonpickup) 
VALUES (266, 2, 'Key to the Ancients Dungeon', 0, 0, 120171, 0, 1, 0, '1', '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	graphic_tile, graphic_equip, stack_size, item_value, lore, graphic_r, graphic_a) 
VALUES (267, 2, 'Unadorned Coral', 0, 0, 120501, 0, 1, 0, '1', 180, 150);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	graphic_tile, graphic_equip, stack_size, item_value, lore) 
VALUES (268, 2, 'Present 1', 0, 0, 120102, 0, 1, 0, '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	graphic_tile, graphic_equip, stack_size, item_value, lore) 
VALUES (269, 2, 'Present 2', 0, 0, 120103, 0, 1, 0, '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	graphic_tile, graphic_equip, stack_size, item_value, lore) 
VALUES (270, 2, 'Present 3', 0, 0, 120104, 0, 1, 0, '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	graphic_tile, graphic_equip, stack_size, item_value, lore) 
VALUES (271, 2, 'Present 4', 0, 0, 120105, 0, 1, 0, '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (272, 3, 'Scroll: Covenant', 0, 0, 88, 0, 47, 25, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (273, 3, 'Scroll: Arcane Blast', 0, 0, 89, 0, 47, 50, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (274, 3, 'Rune: Arcane Assault', 0, 0, 90, 0, 47, 50, '1', 120146, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (275, 3, 'Scroll: Spirit Strike', 0, 0, 91, 0, 55, 50, '1', 120110, 0);
	
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (276, 3, 'Scroll: Critical Strike', 0, 0, 92, 0, 59, 50, '1', 120110, 0);
	
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (277, 3, 'Scroll: Rejuvination', 0, 0, 93, 2600, 31, 50, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (278, 3, 'Rune: Restore Health', 0, 0, 94, 0, 31, 50, '1', 120147, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, stack_size, spell_effect_id)
VALUES (279, 1, 'Teleport: Minita', 15, 0, 1000, 120118, 0, 99, 12);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, stack_size, spell_effect_id)
VALUES (280, 1, 'Teleport: Bound', 15, 0, 1000, 120118, 0, 99, 17);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, stack_size, spell_effect_id, min_level)
VALUES (281, 1, 'Teleport: Otherlands', 15, 0, 1000, 120118, 0, 99, 107, 30);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, min_level, stat_sta, stat_int, player_hp, player_mp, class_restrictions, 
	lore, bindonequip, min_experience) 
VALUES (282, 0, 'Ancient Moon Wand', 80, 2, 5, 0, 120243, 38, 4, 50, 5, 15, 250, 1000, 15, '1', '1', 20000000);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_int, player_hp, player_mp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore, 
	graphic_r, graphic_g, graphic_b, graphic_a, bindonequip, min_experience) 
VALUES (283, 0, 'Ancient Garb', 10, 1, 200, 10, 250, 1750, 120706, 20, 50, 15, 0, '1', 150, 40, 40, 150, '1', 20000000);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_sta, player_hp, player_mp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore, 
	graphic_r, graphic_g, graphic_b, graphic_a, bindonequip, min_experience) 
VALUES (284, 0, 'Ancient Robe', 10, 1, 200, 20, 650, 1000, 120254, 23, 50, 15, 0, '1', 150, 40, 40, 150, '1', 20000000);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_sta, stat_int, player_hp, player_mp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore) 
VALUES (285, 0, 'Snake Tiara', 0, 1, 45, 8, 10, 525, 375, 121000, 26, 50, 1, 0, '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_sta, stat_int, player_hp, player_mp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore) 
VALUES (286, 0, 'Snake Helm', 0, 1, 100, 8, 10, 525, 375, 121001, 25, 50, 1, 0, '1');
	
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, min_level, 
	stat_ac, stat_str, stat_sta, stat_int, stat_dex, player_hp, player_mp, 
	lore, spell_effect_id, bindonequip, min_experience) 
VALUES (287, 0, 'Cloak of Power', 7, 0, 0, 110034, 0, 50, 30, 5, 5, 5, 5, 250, 250, '1', 71, '1', 20000000);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, min_level, 
	stat_ac, stat_str, stat_sta, stat_int, stat_dex, player_hp, player_mp, 
	lore, spell_effect_id, bindonequip, min_experience) 
VALUES (288, 0, 'Pauldrons of Power', 6, 0, 0, 120278, 0, 50, 75, 5, 5, 5, 5, 250, 250, '1', 71, '1', 20000000);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, min_level, 
	stat_ac, stat_str, stat_sta, stat_int, stat_dex, player_hp, player_mp, 
	lore, spell_effect_id, bindonequip, min_experience) 
VALUES (289, 0, 'Belt of Power', 8, 0, 0, 120093, 0, 50, 60, 10, 10, 10, 10, 250, 250, '1', 73, '1', 20000000);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_sta, stat_int, player_hp, player_mp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore, 
	graphic_r, graphic_g, graphic_b, graphic_a, bindonequip, min_experience) 
VALUES (290, 0, 'Ancient Armor', 10, 1, 375, 5, 15, 1750, 300, 120245, 21, 50, 51, 0, '1', 255, 0, 0, 77, '1', 20000000);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, min_level, stat_ac, stat_sta, stat_str, player_hp, class_restrictions, 
	lore, bindonequip, min_experience, spell_effect_id) 
VALUES (291, 0, 'Ancient Axe', 150, 2, 5, 0, 120282, 55, 4, 50, 25, 20, 15, 1250, 55, '1', '1', 20000000, 162);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, min_level, stat_sta, stat_int, stat_dex, player_hp, player_mp, class_restrictions, 
	lore, bindonequip, min_experience, spell_effect_id) 
VALUES (292, 0, 'Ancient Dagger', 120, 2, 5, 0, 120266, 49, 4, 50, 10, 10, 50, 600, 600, 59, '1', '1', 20000000, 162);
	
INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, min_level, stat_ac, stat_int, player_mp,
	lore, graphic_r, graphic_g, graphic_b, graphic_a, bindonequip, min_experience, spell_effect_id, class_restrictions) 
VALUES (293, 0, 'Magus Ancient Moon Shield', 0, 1, 0, 0, 120259, 57, 4, 50, 150, 20, 600, '1', 90, 200, 40, 150, '1', 20000000, 71, 47);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, min_level, stat_ac, stat_sta, stat_int, player_hp, player_mp,
	lore, graphic_r, graphic_g, graphic_b, graphic_a, bindonequip, min_experience, spell_effect_id, class_restrictions) 
VALUES (294, 0, 'Priests Ancient Moon Shield', 0, 1, 0, 0, 120259, 57, 4, 50, 150, 5, 15, 100, 500, '1', 40, 160, 200, 130, '1', 20000000, 77, 31);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, min_level, stat_ac, stat_sta, stat_int, stat_dex, player_hp, player_mp,
	lore, graphic_r, graphic_g, graphic_b, graphic_a, bindonequip, min_experience, spell_effect_id, class_restrictions) 
VALUES (295, 0, 'Rogues Ancient Moon Shield', 0, 1, 0, 0, 120259, 57, 4, 50, 150, 10, 10, 25, 300, 300, '1', 20, 70, 130, 150, '1', 20000000, 77, 59);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, min_level, stat_ac, stat_sta, player_hp,
	lore, graphic_r, graphic_g, graphic_b, graphic_a, bindonequip, min_experience, spell_effect_id, class_restrictions) 
VALUES (296, 0, 'Warriors Ancient Moon Shield', 0, 1, 0, 0, 120259, 57, 4, 50, 250, 20, 600, '1', 180, 60, 25, 130, '1', 20000000, 140, 55);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	graphic_tile, graphic_equip, stack_size, item_value, lore, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (297, 2, 'Essence of Earth', 0, 0, 120500, 0, 10, 0, '0', 128, 0, 0, 100);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	graphic_tile, graphic_equip, stack_size, item_value, lore, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (298, 2, 'Essence of Water', 0, 0, 120500, 0, 10, 0, '0', 0, 128, 255, 100);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	graphic_tile, graphic_equip, stack_size, item_value, lore, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (299, 2, 'Essence of Fire', 0, 0, 120500, 0, 10, 0, '0', 255, 0, 0, 100);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	graphic_tile, graphic_equip, stack_size, item_value, lore, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (300, 2, 'Essence of Air', 0, 0, 120500, 0, 10, 0, '0', 255, 255, 255, 130);
	
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, stack_size, spell_effect_id, min_level)
VALUES (301, 1, 'Teleport: Paradise', 15, 0, 2000, 120118, 0, 99, 141, 50);
	
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (302, 3, 'Scroll: Group Teleport Paradise', 0, 0, 95, 0, 47, 50, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	graphic_tile, graphic_equip, stack_size, item_value, lore, bindonpickup) 
VALUES (303, 2, 'Design: Ancient Shield', 0, 0, 121102, 0, 1, 0, '1', '1');
	
INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, stat_ac, stat_str, stat_dex, stat_int, stat_sta, player_hp, player_mp, min_level, 
	lore, res_fire, res_water, res_earth, res_spirit, res_air, min_experience, bindonequip) 
VALUES (304, 0, 'Bracelet of Fire', 0, 4, 0, 0, 120070, 10, 10, 10, 10, 10, 100, 100, 50, '1', 50, 0, 0, 0, 0, 20000000, '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, stat_ac, stat_str, stat_dex, stat_int, stat_sta, player_hp, player_mp, min_level, 
	lore, res_fire, res_water, res_earth, res_spirit, res_air, min_experience, bindonequip) 
VALUES (305, 0, 'Bracelet of Earth', 0, 4, 0, 0, 120072, 10, 10, 10, 10, 10, 100, 100, 50, '1', 0, 0, 50, 0, 0, 20000000, '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, stat_ac, stat_str, stat_dex, stat_int, stat_sta, player_hp, player_mp, min_level, 
	lore, res_fire, res_water, res_earth, res_spirit, res_air, min_experience, bindonequip) 
VALUES (306, 0, 'Bracelet of Air', 0, 4, 0, 0, 120074, 10, 10, 10, 10, 10, 100, 100, 50, '1', 0, 0, 0, 0, 50, 20000000, '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, stat_ac, stat_str, stat_dex, stat_int, stat_sta, player_hp, player_mp, min_level, 
	lore, res_fire, res_water, res_earth, res_spirit, res_air, min_experience, bindonequip) 
VALUES (307, 0, 'Bracelet of Water', 0, 4, 0, 0, 120076, 10, 10, 10, 10, 10, 100, 100, 50, '1', 0, 50, 0, 0, 0, 20000000, '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, stat_ac, stat_str, stat_dex, stat_int, stat_sta, player_hp, player_mp, min_level, 
	lore, res_fire, res_water, res_earth, res_spirit, res_air, min_experience, bindonequip) 
VALUES (308, 0, 'Bracelet of Spirit', 0, 4, 0, 0, 120079, 10, 10, 10, 10, 10, 100, 100, 50, '1', 0, 0, 0, 50, 0, 20000000, '1');
	
INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, min_level, stat_ac,lore, class_restrictions,
	graphic_r, graphic_g, graphic_b, graphic_a, player_hp, player_mp) 
VALUES (309, 0, 'Earthbrand Shield', 0, 1, 0, 20000, 120259, 42, 4, 50, 150,'0', 55, 20, 150, 20, 150,240,0);
	
INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, min_level, stat_ac, lore, class_restrictions, graphic_r, graphic_g, graphic_b, graphic_a, 
	player_hp, player_mp) 
VALUES (310, 0, 'Earthbrand Buckler', 0, 1, 0, 20000, 120258, 41, 4, 50, 90, '0', 59, 20, 150, 20, 150,120,120);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, min_level, stat_ac, lore, graphic_r, graphic_g, graphic_b, graphic_a, 
	class_restrictions, player_hp, player_mp) 
VALUES (311, 0, 'Earthbrand Guard', 0, 1, 0, 20000, 120302, 43, 4, 50, 70, '0', 20, 150, 20, 150, 15,60,180);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_sta, stat_dex, stat_int, player_mp, player_hp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (312, 0, 'Nagan Armor', 10, 1, 150, 20, 0, 3, 25, 200, 120011, 10, 50, 51, 0, '1', 0, 128, 0, 100);
	
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip, bindonpickup, min_experience) 
VALUES (313, 3, 'Scroll: Ancient Healing', 0, 0, 96, 0, 31, 50, '1', 120110, 0, '1', 20000000);
	
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip, bindonpickup, min_experience) 
VALUES (314, 3, 'Scroll: Ancient Root', 0, 0, 97, 0, 47, 50, '1', 120110, 0, '1', 20000000);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip, bindonpickup, min_experience) 
VALUES (315, 3, 'Scroll: Ancient Sturdiness', 0, 0, 98, 0, 47, 50, '1', 120110, 0, '1', 20000000);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip, bindonpickup, min_experience) 
VALUES (316, 3, 'Scroll: Ancient Criticality', 0, 0, 99, 0, 59, 50, '1', 120110, 0, '1', 20000000);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip, bindonpickup, min_experience) 
VALUES (317, 3, 'Scroll: Ancient Augmentation', 0, 0, 100, 0, 55, 50, '1', 120110, 0, '1', 20000000);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip, bindonpickup, min_experience) 
VALUES (318, 3, 'Scroll: Ancient Protection', 0, 0, 101, 0, 31, 50, '1', 120110, 0, '1', 20000000);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip, bindonpickup, min_experience) 
VALUES (319, 3, 'Scroll: Ancient Buffiness', 0, 0, 102, 0, 47, 50, '1', 120110, 0, '1', 20000000);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip, bindonpickup, min_experience) 
VALUES (320, 3, 'Scroll: Ancient Damage', 0, 0, 103, 0, 59, 50, '1', 120110, 0, '1', 20000000);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip, bindonpickup, min_experience) 
VALUES (321, 3, 'Scroll: Ancient Taunt', 0, 0, 104, 0, 55, 50, '1', 120110, 0, '1', 20000000);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip, bindonpickup, min_experience) 
VALUES (322, 3, 'Scroll: Ancient Sacrifice', 0, 0, 105, 0, 31, 50, '1', 120110, 0, '1', 20000000);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (323, 3, 'Scroll: Smokebomb', 0, 0, 106, 0, 59, 25, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (324, 3, 'Scroll: Group Heal', 0, 0, 107, 0, 31, 25, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (325, 3, 'Scroll: Warrior Root', 0, 0, 108, 0, 55, 25, '1', 120110, 0);
	
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, stack_size, spell_effect_id, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (326, 1, 'Hair Dye: Lime Green', 15, 0, 0, 121122, 0, 99, 156, 40, 255, 0, 160);
	
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, stack_size, spell_effect_id, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (327, 1, 'Hair Dye: Zelius'' Dye', 15, 0, 0, 121122, 0, 99, 157, 255, 255, 255, 180);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        graphic_tile, graphic_equip, stack_size, item_value) 
VALUES (328, 2, 'Blank Scroll', 0, 0, 120110, 0, 10, 50);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        learn_spell_id, item_value, class_restrictions, min_level, lore,
        graphic_tile, graphic_equip) 
VALUES (329, 3, 'Scroll: Bat Illusion', 0, 0, 119, 0, 0, 10, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        graphic_tile, graphic_equip, stack_size, item_value) 
VALUES (330, 2, 'Flint', 0, 0, 120402, 0, 1, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        graphic_tile, graphic_equip, stack_size, item_value) 
VALUES (331, 2, 'Bonfire', 0, 0, 121100, 0, 1, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        graphic_tile, graphic_equip, stack_size, item_value) 
VALUES (332, 2, 'Chisel', 0, 0, 121137, 0, 5, 150);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        graphic_tile, graphic_equip, stack_size, item_value, lore) 
VALUES (333, 2, 'Garlic', 0, 0, 121111, 0, 1, 0, '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        graphic_tile, graphic_equip, stack_size, item_value) 
VALUES (334, 2, 'High Quality Blade', 0, 0, 121139, 0, 10, 500);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        graphic_tile, graphic_equip, stack_size, item_value) 
VALUES (335, 2, 'High Quality Hilt', 0, 0, 121140, 0, 10, 400);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        graphic_tile, graphic_equip, stack_size, item_value) 
VALUES (336, 2, 'Medium Quality Blade', 0, 0, 121139, 0, 10, 250);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        graphic_tile, graphic_equip, stack_size, item_value) 
VALUES (337, 2, 'Medium Quality Hilt', 0, 0, 121140, 0, 10, 200);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        graphic_tile, graphic_equip, stack_size, item_value) 
VALUES (338, 2, 'Low Quality Blade', 0, 0, 121139, 0, 10, 125);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        graphic_tile, graphic_equip, stack_size, item_value) 
VALUES (339, 2, 'Low Quality Hilt', 0, 0, 121140, 0, 10, 100);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        graphic_tile, graphic_equip, stack_size, item_value) 
VALUES (340, 2, 'Needle', 0, 0, 121126, 0, 5, 175);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        graphic_tile, graphic_equip, stack_size, item_value) 
VALUES (341, 2, 'Ink', 0, 0, 121141, 0, 5, 150);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        graphic_tile, graphic_equip, stack_size, item_value) 
VALUES (342, 2, 'Liquid Ore', 0, 0, 121136, 0, 10, 500);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        graphic_tile, graphic_equip, stack_size, item_value) 
VALUES (343, 2, 'Pearl', 0, 0, 120400, 0, 5, 200);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        graphic_tile, graphic_equip, stack_size, item_value) 
VALUES (344, 2, 'Rope', 0, 0, 121124, 0, 5, 50);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        graphic_tile, graphic_equip, stack_size, item_value, lore) 
VALUES (345, 2, 'Sharp Scissors', 0, 0, 121104, 0, 1, 250, '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        graphic_tile, graphic_equip, stack_size, item_value) 
VALUES (346, 2, 'Shirt Pattern', 0, 0, 120241, 0, 1, 200);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        graphic_tile, graphic_equip, stack_size, item_value) 
VALUES (347, 2, 'Spool of Thread', 0, 0, 121133, 0, 5, 200);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        graphic_tile, graphic_equip, stack_size, item_value) 
VALUES (348, 2, 'Spool of Blue Thread', 0, 0, 121132, 0, 5, 200);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        graphic_tile, graphic_equip, stack_size, item_value) 
VALUES (349, 2, 'Spool of Green Thread', 0, 0, 121131, 0, 5, 200);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        graphic_tile, graphic_equip, stack_size, item_value) 
VALUES (350, 2, 'Spool of Red Thread', 0, 0, 121130, 0, 5, 200);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        graphic_tile, graphic_equip, stack_size, item_value) 
VALUES (351, 2, 'Spool of Black Thread', 0, 0, 121129, 0, 5, 200);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        graphic_tile, graphic_equip, stack_size, item_value) 
VALUES (352, 2, 'Spool of Pink Thread', 0, 0, 121128, 0, 5, 200);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        graphic_tile, graphic_equip, stack_size, item_value) 
VALUES (353, 2, 'Spool of Purple Thread', 0, 0, 121127, 0, 5, 200);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        graphic_tile, graphic_equip, stack_size, item_value) 
VALUES (354, 2, 'Unrefined Ore', 0, 0, 120504, 0, 5, 500);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        graphic_tile, graphic_equip, stack_size, item_value) 
VALUES (355, 2, 'Cats Hair', 0, 0, 121124, 0, 1, 20);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        learn_spell_id, item_value, class_restrictions, min_level, lore,
        graphic_tile, graphic_equip) 
VALUES (356, 3, 'Scroll: Shroom Illusion', 0, 0, 120, 0, 0, 15, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
        item_value, graphic_tile, stat_ac, stat_str, stat_dex, stat_int, stat_sta, player_hp, player_mp, min_level, event, lore) 
VALUES (357, 0, 'Crude Pearl Ring', 0, 4, 0, 250, 120060, 15, 3, 3, 3, 3, 10, 5, 10, '0', '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
        item_value, graphic_tile, stat_ac, stat_str, stat_dex, stat_int, stat_sta, player_hp, player_mp, min_level, event, lore) 
VALUES (358, 0, 'Crude Gold Ring', 0, 4, 0, 150, 120053, 5, 0, 0, 0, 0, 0, 0, 1, '0', '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
        item_value, graphic_tile, stat_ac, stat_str, stat_dex, stat_int, stat_sta, player_hp, player_mp, min_level, event, lore) 
VALUES (359, 0, 'Crude Ruby Ring', 0, 4, 0, 250, 120055, 15, 3, 3, 3, 3, 10, 5, 10, '0', '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
        item_value, graphic_tile, stat_ac, stat_str, stat_dex, stat_int, stat_sta, player_hp, player_mp, min_level, event, lore) 
VALUES (360, 0, 'Pearl Bracelet', 0, 4, 0, 0, 120078, 15, 4, 4, 4, 4, 50, 50, 10, '0', '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
        item_value, graphic_tile, graphic_equip, body_state, class_restrictions, lore, min_level, stat_str, stat_sta, stat_dex) 
VALUES (361, 0, 'High Quality Walde', 65, 2, 5, 1500, 120263, 46, 4, 55, '0', 35, 12, 4, 2);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
        item_value, graphic_tile, graphic_equip, body_state, class_restrictions, lore, min_level, stat_str, stat_sta) 
VALUES (362, 0, 'Medium Quality Walde', 30, 2, 5, 1000, 120263, 46, 4, 50, '0', 20, 5, 2);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
        item_value, graphic_tile, graphic_equip, body_state, class_restrictions, lore, min_level, stat_str, stat_sta) 
VALUES (363, 0, 'Low Quality Walde', 26, 2, 5, 1000, 120263, 46, 4, 50, '0', 13, 2, 1);
	
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_int, stat_dex, player_hp, player_mp,  
	graphic_tile, graphic_equip, min_level, item_value, lore, event, class_restrictions, graphic_r, graphic_g, graphic_b, graphic_a, spell_effect_id) 
VALUES (364, 0, 'Stunnah Shades', 0, 1, 50, 10, 10, 10, 10, 75, 150, 120048, 11, 1, 0, '0', '0', 0, 0, 255, 0, 200, 71);
	
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        stat_ac, stat_int, stat_sta, stat_dex, stat_str, player_mp, player_hp, 
        graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (365, 0, 'Valiant Helmet', 0, 3, 0, 0, 0, 0, 0, 0, 0, 120250, 21, 50, 0, 250000, '0', 214, 214, 214, 140);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        stat_ac, stat_str, stat_sta, stat_dex, player_hp, 
        graphic_tile, graphic_equip, min_level, class_restrictions, item_value, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (366, 0, 'Valiant Chestplate', 10, 3, 0, 0, 0, 0, 0, 120245, 21, 50, 0, 250000, 214, 214, 214, 140);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        stat_ac, stat_str, stat_sta, stat_int, stat_dex, player_hp, player_mp, 
        graphic_tile, graphic_equip, min_level, class_restrictions, item_value, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (367, 0, 'Valiant Legplates', 11, 3, 0, 0, 0, 0, 0, 0, 0, 120036, 5, 50, 0, 250000, 214, 214, 214, 140);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        stat_ac, stat_str, stat_sta, stat_int, stat_dex, player_hp, player_mp, 
        graphic_tile, graphic_equip, min_level, class_restrictions, item_value, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (368, 0, 'Valiant Boots', 12, 3, 0, 0, 0, 0, 0, 0, 0, 120041, 4, 50, 0, 250000, 214, 214, 214, 140);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        stat_ac, stat_str, stat_sta, stat_int, 
        graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore, event, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (369, 0, 'Valiant Cap', 0, 1, 0, 0, 0, 0, 120033, 6, 50, 0, 250000, '0', '0', 214, 214, 214, 180);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        stat_ac, stat_int, stat_dex, player_hp, player_mp, 
        graphic_tile, graphic_equip, min_level, class_restrictions, item_value, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (370, 0, 'Valiant Robes', 10, 1, 0, 0, 0, 0, 0, 120255, 23, 50, 0, 250000, 214, 214, 214, 140);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        stat_ac, stat_str, stat_sta, stat_dex, player_hp, 
        graphic_tile, graphic_equip, min_level, class_restrictions, item_value, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (371, 0, 'Valiant Mesh', 10, 3, 0, 0, 0, 0, 0, 120238, 17, 50, 0, 250000, 214, 214, 214, 180);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        stat_ac, stat_int, stat_sta, stat_dex, stat_str, player_mp, player_hp, 
        graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (372, 0, 'Valiant Stealth', 0, 3, 0, 0, 0, 0, 0, 0, 0, 120022, 2, 50, 0, 250000, '0', 214, 214, 214, 180);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        stat_ac, stat_int, stat_sta, stat_dex, stat_str, player_mp, player_hp, 
        graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (373, 0, 'Valiant Helmet', 0, 3, 130, 0, 7, 7, 10, 100, 200, 120250, 21, 50, 51, 0, '0', 214, 214, 214, 140);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        stat_ac, stat_str, stat_sta, stat_dex, player_hp, 
        graphic_tile, graphic_equip, min_level, class_restrictions, item_value, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (374, 0, 'Valiant Chestplate', 10, 3, 275, 20, 20, 20, 300, 120245, 21, 50, 51, 0, 214, 214, 214, 140);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        stat_ac, stat_str, stat_sta, stat_int, stat_dex, player_hp, player_mp, 
        graphic_tile, graphic_equip, min_level, class_restrictions, item_value, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (375, 0, 'Valiant Legplates', 11, 3, 120, 15, 15, 15, 15, 175, 150, 120036, 5, 50, 0, 0, 214, 214, 214, 140);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        stat_ac, stat_str, stat_sta, stat_int, stat_dex, player_hp, player_mp, 
        graphic_tile, graphic_equip, min_level, class_restrictions, item_value, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (376, 0, 'Valiant Boots', 12, 3, 75, 10, 10, 0, 0, 150, 100, 120041, 4, 50, 0, 0, 214, 214, 214, 140);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        stat_ac, stat_str, stat_sta, stat_int, player_hp, player_mp,
        graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore, event, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (377, 0, 'Valiant Cap', 0, 1, 70, 0, 8, 18, 200, 100, 120033, 6, 50, 15, 0, '0', '0', 214, 214, 214, 180);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        stat_ac, stat_int, stat_dex, player_hp, player_mp, 
        graphic_tile, graphic_equip, min_level, class_restrictions, item_value, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (378, 0, 'Valiant Robes', 10, 1, 200, 35, 5, 125, 425, 120255, 23, 50, 15, 0, 214, 214, 214, 140);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        stat_ac, stat_str, stat_sta, stat_dex, player_hp, player_mp, 
        graphic_tile, graphic_equip, min_level, class_restrictions, item_value, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (379, 0, 'Valiant Mesh', 10, 3, 235, 20, 20, 20, 250, 150, 120238, 17, 50, 59, 0, 214, 214, 214, 180);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        stat_ac, stat_int, stat_sta, stat_dex, stat_str, player_mp, player_hp, 
        graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (380, 0, 'Valiant Stealth', 0, 3, 120, 10, 10, 20, 20, 150, 150, 120022, 2, 50, 59, 0, '0', 214, 214, 214, 180);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
        item_value, graphic_tile, stat_ac, stat_str, stat_dex, stat_int, stat_sta, player_hp, player_mp, min_level, event, lore, spell_effect_id, credits_value) /* Donation */
VALUES (381, 0, 'Celestial Pauldrons', 0, 6, 0, 50000, 120278, 60, 0, 0, 6, 6, 90, 0, 0, '1', '1', 68, 5);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        stat_ac, stat_str, stat_sta, stat_int, stat_dex, res_fire, res_water, res_earth, res_spirit, res_air,
        graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore, event, player_hp, player_mp, credits_value) /* Donation */
VALUES (382, 0, 'Shades', 0, 1, 100, 10, 10, 10, 10, 10, 10, 10, 10, 10, 120048, 11, 0, 0, 40000, '0', '0', 150, 150, 5);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        stat_ac, stat_str, stat_sta, stat_int, stat_dex, player_mp, player_hp, res_fire, res_water, res_earth, res_spirit, res_air, 
        graphic_tile, graphic_equip, min_level, class_restrictions, item_value, credits_value) /* Donation */
VALUES (383, 0, 'Battle Gown', 10, 1, 200, 20, 20, 20, 20, 200, 200, 20, 20, 20, 20, 20, 120257, 25, 1, 0, 50000, 5);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
        item_value, graphic_tile, stat_ac, stat_str, stat_dex, stat_int, stat_sta, player_hp, player_mp, min_level, event, lore, spell_effect_id, graphic_r, graphic_g, graphic_b, graphic_a, credits_value) /* Donation */
VALUES (384, 0, 'Lucky Belt', 0, 8, 0, 400000, 120093, 50, 5, 5, 5, 5, 75, 75, 0, '0', '0', 71, 24, 81, 33, 160, 10);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        stat_ac, stat_str, stat_dex, player_hp, player_mp, stat_sta, stat_int, 
        graphic_tile, graphic_equip, min_level, class_restrictions, item_value, event) 
VALUES (385, 0, 'Champions Sandals', 12, 1, 25, 10, 10, 125, 125, 5, 5, 120289, 3, 1, 0, 0, '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        stat_ac, stat_str, stat_sta, player_mp, player_hp, 
        graphic_tile, graphic_equip, min_level, class_restrictions, item_value) 
VALUES (386, 0, 'Princess Dress', 10, 1, 50, 10, 10, 100, 100, 120265, 26, 1, 0, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        stat_ac, stat_str, stat_sta, stat_int, player_mp, player_hp, 
        graphic_tile, graphic_equip, min_level, class_restrictions, item_value)
VALUES (387, 0, 'Fire Angel Robe', 10, 1, 100, 10, 10, 10, 150, 150, 120296, 28, 1, 0, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        stat_ac, stat_str, stat_sta, player_mp, player_hp, 
        graphic_tile, graphic_equip, min_level, class_restrictions, item_value) 
VALUES (388, 0, 'Tuxedo', 10, 1, 25, 5, 5, 100, 100, 120293, 5, 1, 0, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
        item_value, graphic_tile, graphic_equip, body_state, class_restrictions, lore, min_level, stat_str, stat_sta, stat_dex, player_hp, player_mp, weapon_delay, graphic_r, graphic_g, graphic_b, graphic_a, credits_value) /* Donation */
VALUES (389, 0, 'Lucky Dagger', 80, 2, 6, 30000, 120249, 37, 4, 0, '0', 0, 10, 5, 5, 125, 125, 7, 24, 81, 33, 160, 5);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
        item_value, graphic_tile, graphic_equip, body_state, class_restrictions, lore, min_level, stat_int, stat_sta, stat_dex, player_hp, player_mp, stat_ac) 
VALUES (390, 0, 'Slime Staff', 50, 3, 7, 0, 120294, 59, 3, 0, '0', 0, 10, 10, 0, 100, 100, 50);
	
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, stack_size, spell_effect_id, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (391, 1, 'Hair Dye: Frozen Spit', 15, 0, 0, 121122, 0, 99, 159, 164, 219, 247, 200);
  
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, stack_size, spell_effect_id, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (392, 1, 'Hair Dye: Fayt Dye', 15, 0, 0, 121122, 0, 99, 158, 148, 0, 0, 209);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
        item_value, graphic_tile, graphic_equip, body_state, class_restrictions, lore, min_level, player_hp, player_mp, graphic_r, graphic_g, graphic_b, graphic_a, spell_effect_id,
        bindonequip, bindonpickup) 
VALUES (393, 0, 'Haze''s Bone Sword', 50, 2, 5, 0, 120272, 31, 4, 0, '1', 50, 125, 125, 255, 10, 10, 120, 76, '1', '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        stat_ac, stat_str, stat_sta, stat_int, stat_dex, res_fire, res_water, res_earth, res_spirit, res_air, player_mp, player_hp, 
        graphic_tile, graphic_equip, min_level, class_restrictions, item_value, spell_effect_id, lore, bindonpickup) 
VALUES (394, 0, 'Doom Robe', 10, 1, 200, 20, 20, 20, 20, 20, 20, 20, 20, 20, 3000, 3000, 120285, 22, 50, 0, 0, 161, '1', '1');
  
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	graphic_tile, graphic_equip, stack_size, item_value, lore, bindonpickup) 
VALUES (395, 2, 'Design: Ancient Slippers', 0, 0, 121102, 0, 1, 0, '1', '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	graphic_tile, graphic_equip, stack_size, item_value, lore, bindonpickup) 
VALUES (396, 2, 'Mold: Ancient Boots', 0, 0, 121138, 0, 1, 0, '1', '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	graphic_tile, graphic_equip, stack_size, item_value, lore, bindonpickup) 
VALUES (397, 2, 'Design: Divine Crown', 0, 0, 121102, 0, 1, 0, '1', '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	graphic_tile, graphic_equip, stack_size, item_value, lore, bindonpickup) 
VALUES (398, 2, 'Mold: Divine Helm', 0, 0, 121138, 0, 1, 0, '1', '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, min_level, stat_sta, stat_int, stat_dex, player_hp, player_mp, class_restrictions, 
	lore, bindonpickup, graphic_r, graphic_g, graphic_b, graphic_a, spell_effect_id) 
VALUES (399, 0, 'Dagger of Contemption', 120, 2, 5, 0, 120047, 11, 4, 50, 10, 10, 10, 500, 1000, 1, '1', '1', 0, 100, 0, 160, 162);
  
INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, min_level, stat_ac, player_hp, player_mp,
	lore, graphic_r, graphic_g, graphic_b, graphic_a, bindonpickup, spell_effect_id) 
VALUES (400, 0, 'Ward of Destruction', 0, 1, 0, 0, 120261, 43, 4, 50, 100, 700, 700, '1', 0, 0, 100, 160, '1', 164);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_int, stat_dex, player_hp, player_mp, lore, bindonpickup, bindonequip, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, spell_effect_id) /* Rogue */
VALUES (401, 0, 'Ancient Boots', 12, 3, 100, 0, 0, 0, 50, 1000, 1000, '1', '1', '1', 120041, 4, 50, 59, 0, 76);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_int, stat_dex, player_hp, player_mp, lore, bindonpickup, bindonequip, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, spell_effect_id) /* Warrior */
VALUES (402, 0, 'Ancient Boots', 12, 3, 100, 0, 50, 0, 0, 2000, 0, '1', '1', '1', 120043, 2, 50, 55, 0, 76);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_int, stat_dex, player_hp, player_mp, lore, bindonpickup, bindonequip, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, spell_effect_id) /* Magus */
VALUES (403, 0, 'Ancient Slippers', 12, 3, 100, 0, 0, 25, 25, 0, 2000, '1', '1', '1', 120007, 5, 50, 47, 0, 70);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_int, stat_dex, player_hp, player_mp, lore, bindonpickup, bindonequip, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, spell_effect_id) /* Priest */
VALUES (404, 0, 'Ancient Slippers', 12, 3, 100, 30, 20, 0, 0, 500, 1500, '1', '1', '1', 120005, 1, 50, 31, 0, 76);
  
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_int, stat_sta, stat_str, stat_dex, player_hp, player_mp, /* Rogue */
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore, bindonequip, bindonpickup, spell_effect_id) 
VALUES (405, 0, 'Divine Helm', 0, 3, 150, 25, 25, 0, 0, 1500, 1500, 120022, 10, 50, 59, 0, '1', '1', '1', 84);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_int, stat_sta, stat_str, stat_dex, player_hp, player_mp, /* Warrior */
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore, bindonequip, bindonpickup, spell_effect_id) 
VALUES (406, 0, 'Divine Helm', 0, 3, 150, 0, 50, 0, 0, 3000, 0, 120023, 2, 50, 55, 0, '1', '1', '1', 84);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_int, stat_sta, stat_str, stat_dex, player_hp, player_mp, /* Magus */
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore, bindonequip, bindonpickup, spell_effect_id) 
VALUES (407, 0, 'Divine Crown', 0, 3, 150, 50, 0, 0, 0, 0, 3000, 120032, 5, 50, 47, 0, '1', '1', '1', 84);
  
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_int, stat_sta, stat_str, stat_dex, player_hp, player_mp, /* Priest */
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore, bindonequip, bindonpickup, spell_effect_id) 
VALUES (408, 0, 'Divine Crown', 0, 3, 150, 30, 20, 0, 0, 1000, 2000, 120031, 8, 50, 31, 0, '1', '1', '1', 84);
  
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
  stat_ac, stat_str, stat_sta, stat_int, stat_dex, 
  graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore, event, player_hp, player_mp) 
VALUES (409, 0, 'Laurels', 0, 1, 50, 20, 20, 20, 20, 120288, 28, 25, 0, 0, '1', '0', 300, 300);
  
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
  stat_ac, stat_str, stat_sta, stat_int, stat_dex, player_mp, player_hp, 
  graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore) 
VALUES (410, 0, 'Frilly Top', 10, 1, 50, 25, 25, 25, 25, 400, 400, 120290, 9, 25, 0, 0, '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
  stat_ac, stat_str, stat_sta, stat_int, stat_dex, player_hp, player_mp, 
  graphic_tile, graphic_equip, min_level, item_value, lore) 
VALUES (411, 0, 'Frilly Skirt', 11, 3, 50, 20, 20, 20, 20, 200, 200, 120291, 4, 25, 0, '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
  stat_ac, stat_str, stat_dex, player_hp, player_mp, stat_sta, stat_int, 
  graphic_tile, graphic_equip, min_level, class_restrictions, item_value, graphic_r, graphic_g, graphic_b, graphic_a, lore) 
VALUES (412, 0, 'Sandals', 12, 1, 50, 10, 10, 100, 100, 10, 10, 120289, 3, 25, 0, 0, 0, 0, 0, 160, '1');
  
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
  learn_spell_id, item_value, class_restrictions, min_level, lore,
  graphic_tile, graphic_equip, bindonpickup) 
VALUES (413, 3, 'Scroll: Ancient Bellow', 0, 0, 109, 0, 55, 50, '1', 120110, 0, '1');
  
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
  learn_spell_id, item_value, class_restrictions, min_level, lore,
  graphic_tile, graphic_equip, bindonpickup) 
VALUES (414, 3, 'Rune: Ancient Awe', 0, 0, 110, 0, 55, 50, '1', 120148, 0, '1')
 
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
  learn_spell_id, item_value, class_restrictions, min_level, lore,
  graphic_tile, graphic_equip, bindonpickup) 
VALUES (415, 3, 'Rune: Ancient Conflagration', 0, 0, 112, 0, 47, 50, '1', 120150, 0, '1')
 
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
  learn_spell_id, item_value, class_restrictions, min_level, lore,
  graphic_tile, graphic_equip, bindonpickup) 
VALUES (416, 3, 'Rune: Ancient Death', 0, 0, 111, 0, 59, 50, '1', 120149, 0, '1')
  
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
  stat_ac, stat_str, stat_sta, stat_int, stat_dex, res_fire, res_water, res_earth, res_spirit, res_air,
  graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore, event, player_hp, player_mp, spell_effect_id, bindonpickup) 
VALUES (417, 0, 'Doom Helm', 0, 1, 150, 10, 10, 10, 10, 10, 10, 10, 10, 10, 120287, 29, 50, 0, 0, '1', '0', 2000, 2000, 69, '1');
  
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
  item_value, graphic_tile, graphic_equip, stack_size, spell_effect_id, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (418, 1, 'Hair Dye: Purple Haze', 15, 0, 0, 121122, 0, 99, 169, 116, 12, 108, 145); 
  
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip, bindonpickup) 
VALUES (419, 3, 'Rune: Ancient Blessings', 0, 0, 113, 0, 31, 50, '1', 120151, 0, '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (420, 3, 'Scroll: Spiritual Blessings', 0, 0, 114, 0, 31, 50, '1', 120110, 0);
  
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, stack_size, spell_effect_id, min_level, min_experience)
VALUES (421, 1, 'Teleport: Ancients Dungeon', 15, 0, 1000, 120118, 0, 99, 172, 50, 100000000);
  
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, min_level, 
	stat_ac, stat_str, stat_sta, stat_int, stat_dex, player_hp, player_mp, 
	lore, res_fire, res_water, res_earth, res_spirit, res_air, class_restrictions) 
VALUES (422, 0, 'Savage Pauldrons of the Boar', 6, 0, 0, 120236, 0, 50, 45, 2, 2, 2, 2, 50, 150, '1', 2, 2, 2, 2, 2, 15);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, min_level, 
	stat_ac, stat_str, stat_sta, stat_int, stat_dex, player_hp, player_mp, 
	lore, res_fire, res_water, res_earth, res_spirit, res_air, class_restrictions) 
VALUES (423, 0, 'Savage Pauldrons of the Cow', 6, 0, 0, 120232, 0, 50, 60, 2, 2, 2, 2, 150, 50, '1', 2, 2, 2, 2, 2, 50);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	graphic_tile, graphic_equip, min_level, stat_ac, player_hp, player_mp, 
	lore, res_fire, res_water, res_earth, res_spirit, res_air, class_restrictions,
	graphic_r, graphic_a, spell_effect_id) 
VALUES (424, 0, 'Red Ring', 0, 4, 0, 120063, 0, 50, 5, 50, 50, '1', 2, 2, 2, 2, 2, 15, 200, 200, 58);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	graphic_tile, graphic_equip, min_level, stat_ac, player_hp, player_mp, 
	lore, res_fire, res_water, res_earth, res_spirit, res_air, class_restrictions,
	graphic_a, spell_effect_id) 
VALUES (425, 0, 'Black Ring', 0, 4, 0, 120063, 0, 50, 5, 50, 50, '1', 2, 2, 2, 2, 2, 50, 200, 80);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, player_hp, player_mp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore, bindonpickup,
	graphic_r, graphic_g, graphic_b, graphic_a, spell_effect_id) 
VALUES (426, 0, 'Gero Robes', 10, 1, 50, 1000, 3000, 120009, 14, 50, 15, 0, '1', '1', 220, 50, 50, 140, 71);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, player_hp, player_mp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore, bindonpickup,
	graphic_r, graphic_g, graphic_b, graphic_a, spell_effect_id) 
VALUES (427, 0, 'Mama Chestplate', 10, 1, 400, 2000, 1000, 120011, 10, 50, 50, 0, '1', '1', 220, 50, 50, 140, 140);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, player_hp, player_mp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore, bindonpickup,
	graphic_r, graphic_g, graphic_b, graphic_a, spell_effect_id) 
VALUES (428, 0, 'Mama Headband', 0, 3, 150, 1500, 1500, 120022, 10, 50, 50, 0, '1', '1', 220, 50, 50, 140, 174);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, player_hp, player_mp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore, bindonpickup,
	graphic_r, graphic_g, graphic_b, graphic_a, spell_effect_id) 
VALUES (429, 0, 'Mama Legplates', 11, 3, 150, 1000, 1500, 120036, 5, 50, 50, 0, '1', '1', 220, 50, 50, 140, 174);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, player_hp, player_mp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore, bindonpickup,
	graphic_r, graphic_g, graphic_b, graphic_a, spell_effect_id) 
VALUES (430, 0, 'Mama Boots', 12, 3, 100, 1000, 500, 120041, 4, 50, 50, 0, '1', '1', 220, 50, 50, 140, 174);
  
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip, bindonpickup) 
VALUES (431, 3, 'Scroll: Sacrifice II', 0, 0, 115, 0, 31, 50, '1', 120110, 0, '1');
  
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip, bindonpickup) 
VALUES (432, 3, 'Scroll: Damage of the Bear', 0, 0, 116, 0, 47, 50, '1', 120110, 0, '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip, bindonpickup) 
VALUES (433, 3, 'Scroll: Critical Blow of the Bear', 0, 0, 117, 0, 59, 50, '1', 120110, 0, '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip, bindonpickup) 
VALUES (434, 3, 'Scroll: Roar of the Bear', 0, 0, 118, 0, 55, 50, '1', 120110, 0, '1');
  
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, min_level, 
	stat_ac, stat_str, stat_sta, stat_int, stat_dex, player_hp, player_mp, 
	lore, res_fire, res_water, res_earth, res_spirit, res_air, bindonpickup, bindonequip, spell_effect_id) 
VALUES (435, 0, 'Ducky Pauldrons', 6, 0, 0, 120232, 0, 50, 70, 10, 10, 10, 10, 300, 300, '1', 10, 10, 10, 10, 10, '1', '1', 177);
  
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        graphic_tile, graphic_equip, stack_size, item_value, bindonpickup) 
VALUES (436, 2, 'Priceless Needle', 0, 0, 121126, 0, 5, 0, '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        graphic_tile, graphic_equip, stack_size, item_value, bindonpickup) 
VALUES (437, 2, 'Priceless Pattern', 0, 0, 121102, 0, 5, 0, '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        graphic_tile, graphic_equip, stack_size, item_value, bindonpickup) 
VALUES (438, 2, 'Priceless Thread', 0, 0, 121133, 0, 5, 0, '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        graphic_tile, graphic_equip, stack_size, item_value, bindonpickup) 
VALUES (439, 2, 'Priceless Ore', 0, 0, 120504, 0, 5, 0, '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        graphic_tile, graphic_equip, stack_size, item_value, bindonpickup) 
VALUES (440, 2, 'Priceless Chisel', 0, 0, 121137, 0, 5, 0, '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        graphic_tile, graphic_equip, stack_size, item_value, bindonpickup) 
VALUES (441, 2, 'Priceless Hammer', 0, 0, 120273, 0, 5, 0, '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        graphic_tile, graphic_equip, stack_size, item_value, bindonpickup) 
VALUES (442, 2, 'Wrapping Paper', 0, 0, 150208, 0, 5, 0, '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        graphic_tile, graphic_equip, stack_size, item_value, bindonpickup) 
VALUES (443, 2, 'Empty Box', 0, 0, 121134, 0, 5, 0, '1');
  
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        graphic_tile, graphic_equip, stack_size, item_value) 
VALUES (444, 2, 'Sketch', 0, 0, 121108, 0, 5, 500);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        graphic_tile, graphic_equip, stack_size, item_value, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (445, 2, 'Soft Gold Ore', 0, 0, 121138, 0, 99, 200, 250, 200, 120, 140);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        graphic_tile, graphic_equip, stack_size, item_value, graphic_r, graphic_g, graphic_b, graphic_a, lore, bindonpickup) 
VALUES (446, 2, 'Gramps Fur', 0, 0, 120601, 0, 1, 0, 200, 10, 10, 120, '1', '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        graphic_tile, graphic_equip, stack_size, item_value, lore, bindonpickup) 
VALUES (447, 2, 'Blood', 0, 0, 121101, 0, 1, 0, '1', '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        graphic_tile, graphic_equip, stack_size, item_value, graphic_r, graphic_g, graphic_b, graphic_a, lore, bindonpickup) 
VALUES (448, 2, 'Berrys Hair Strand', 0, 0, 121124, 0, 1, 0, 200, 10, 10, 120, '1', '1');
  
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        graphic_tile, graphic_equip, stack_size, item_value) 
VALUES (449, 2, 'Cloth', 0, 0, 120223, 0, 1, 200);
  
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_dex, stat_int, player_hp, player_mp,
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value) 
VALUES (450, 0, 'Cloth Shirt', 10, 1, 30, 1, 1, 1, 1, 10, 10, 120241, 4, 1, 0, 400);
  
INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
        item_value, graphic_tile, graphic_equip, body_state, class_restrictions, lore, min_level, stat_dex, stat_sta, weapon_delay) 
VALUES (451, 0, 'Practice Katana', 26, 2, 5, 1000, 120275, 53, 4, 50, '0', 10, 5, 1, 9);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, min_level, 
	stat_ac, stat_str, stat_sta, stat_int, stat_dex, player_hp, player_mp) 
VALUES (452, 0, 'Soft Belt', 8, 0, 0, 120096, 0, 10, 5, 1, 1, 1, 1, 10, 10);
  
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, player_hp, player_mp, 
	graphic_tile, graphic_equip, min_level) 
VALUES (453, 0, 'Cat Ears', 0, 0, 30, 4, 30, 30, 120221, 23, 15);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, player_hp, player_mp, 
	graphic_tile, graphic_equip, min_level, graphic_a) 
VALUES (454, 0, 'Black Cat Ears', 0, 0, 30, 4, 30, 30, 120221, 23, 15, 170);
  
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	graphic_tile, graphic_equip, stack_size, item_value) 
VALUES (455, 2, 'Leather Padding', 0, 0, 120605, 0, 99, 100);
  
INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, class_restrictions, lore, 
	min_level, stat_sta, stat_str, stat_dex, player_hp, player_mp, weapon_delay) 
VALUES (456, 0, 'Fighting Katana', 63, 2, 6, 1500, 120276, 52, 4, 50, '1', 30, 3, 12, 10, 50, 50, 9);
  
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        graphic_tile, graphic_equip, stack_size, item_value, graphic_r, graphic_a) 
VALUES (457, 2, 'Red Rope', 0, 0, 121124, 0, 5, 500, 160, 160);
  
INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, lore, graphic_r, graphic_g, graphic_b, graphic_a, spell_effect_id, bindonpickup) 
VALUES (458, 0, 'Gero Necklace', 0, 5, 0, 0, 120601, '1', 200, 10, 10, 120, 185, '1');
  
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        graphic_tile, graphic_equip, stack_size, item_value, lore, graphic_r, graphic_g, graphic_b, graphic_a, min_level, spell_effect_id) 
VALUES (459, 1, 'Shard of Earth', 0, 0, 120500, 0, 1, 0, '0', 128, 0, 0, 125, 50, 186);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        graphic_tile, graphic_equip, stack_size, item_value, lore, graphic_r, graphic_g, graphic_b, graphic_a, min_level, spell_effect_id) 
VALUES (460, 1, 'Shard of Water', 0, 0, 120500, 0, 1, 0, '0', 0, 128, 255, 125, 50, 197);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        graphic_tile, graphic_equip, stack_size, item_value, lore, graphic_r, graphic_g, graphic_b, graphic_a, min_level, spell_effect_id) 
VALUES (461, 1, 'Shard of Fire', 0, 0, 120500, 0, 1, 0, '0', 255, 0, 0, 130, 50, 195);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        graphic_tile, graphic_equip, stack_size, item_value, lore, graphic_r, graphic_g, graphic_b, graphic_a, min_level, spell_effect_id) 
VALUES (462, 1, 'Shard of Air', 0, 0, 120500, 0, 1, 0, '0', 255, 255, 255, 110, 50, 198);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        graphic_tile, graphic_equip, stack_size, item_value, lore, graphic_r, graphic_g, graphic_b, graphic_a, min_level, spell_effect_id) 
VALUES (463, 1, 'Shard of Death', 0, 0, 120500, 0, 1, 0, '0', 20, 20, 20, 130, 50, 196);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        graphic_tile, graphic_equip, stack_size, item_value, lore, graphic_r, graphic_g, graphic_b, graphic_a, min_level, spell_effect_id) 
VALUES (464, 1, 'Shard of Strength', 0, 0, 120500, 0, 1, 0, '0', 64, 0, 0, 130, 50, 187);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        graphic_tile, graphic_equip, stack_size, item_value, lore, graphic_r, graphic_g, graphic_b, graphic_a, min_level, spell_effect_id) 
VALUES (465, 1, 'Shard of Love', 0, 0, 120500, 0, 1, 0, '0', 255, 100, 255, 100, 50, 188);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        graphic_tile, graphic_equip, stack_size, item_value, lore, graphic_r, graphic_g, graphic_b, graphic_a, min_level, spell_effect_id) 
VALUES (466, 1, 'Shard of Life', 0, 0, 120500, 0, 1, 0, '0', 0, 255, 0, 100, 50, 189);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        graphic_tile, graphic_equip, stack_size, item_value, lore, graphic_r, graphic_g, graphic_b, graphic_a, min_level, spell_effect_id) 
VALUES (467, 1, 'Shard of Hope', 0, 0, 120500, 0, 1, 0, '0', 250, 110, 30, 115, 50, 193);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        graphic_tile, graphic_equip, stack_size, item_value, lore, graphic_r, graphic_g, graphic_b, graphic_a, min_level, spell_effect_id) 
VALUES (468, 1, 'Shard of Divinity', 0, 0, 120500, 0, 1, 0, '0', 255, 255, 255, 150, 50, 194);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        graphic_tile, graphic_equip, stack_size, item_value, lore, graphic_r, graphic_g, graphic_b, graphic_a, min_level, spell_effect_id) 
VALUES (469, 1, 'Shard of Power', 0, 0, 120500, 0, 1, 0, '0', 255, 0, 0, 150, 50, 191);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        graphic_tile, graphic_equip, stack_size, item_value, lore, graphic_r, graphic_g, graphic_b, graphic_a, min_level, spell_effect_id) 
VALUES (470, 1, 'Shard of Protection', 0, 0, 120500, 0, 1, 0, '0', 0, 88, 176, 110, 50, 190);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        graphic_tile, graphic_equip, stack_size, item_value, lore, graphic_r, graphic_g, graphic_b, graphic_a, min_level, spell_effect_id) 
VALUES (471, 1, 'Shard of Invincibility', 0, 0, 120500, 0, 1, 0, '0', 128, 128, 128, 120, 50, 192);
  
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        stat_ac, stat_str, stat_sta, stat_int, stat_dex, player_hp, player_mp,  
        graphic_tile, graphic_equip, min_level, item_value, lore, event, class_restrictions, graphic_r, graphic_g, graphic_b, graphic_a, spell_effect_id) 
VALUES (472, 0, 'Hazy Ears', 0, 1, 50, 10, 10, 10, 10, 75, 150, 120222, 13, 1, 0, '0', '0', 0, 1, 1, 1, 215, 71);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore, bindonpickup,
	graphic_tile, graphic_equip) 
VALUES (473, 3, 'Scroll: Ancient Group Healing', 0, 0, 121, 0, 31, 50, '1', '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore, bindonpickup,
	graphic_tile, graphic_equip) 
VALUES (474, 3, 'Scroll: Ancient Group Damage', 0, 0, 122, 0, 59, 50, '1', '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip, bindonpickup, min_experience) 
VALUES (475, 3, 'Scroll: Ancient Regeneration', 0, 0, 123, 0, 47, 50, '1', 120110, 0, '1', 20000000);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, min_level, stat_ac, player_mp, player_hp, lore, res_fire, res_water, res_earth, res_spirit, res_air, spell_effect_id,
	stat_sta, stat_str, stat_int, stat_dex, credits_value) /* Donation */
VALUES (476, 0, 'Star Shield', 0, 1, 0, 10000, 120262, 48, 4, 1, 100, 100, 100, '0', 10, 10, 10, 10, 10, 78, 10, 10, 10, 10, 7);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
        item_value, graphic_tile, graphic_equip, body_state, class_restrictions, lore, min_level, 
        stat_str, stat_sta, stat_dex, stat_int, player_hp, player_mp, weapon_delay, spell_effect_id, credits_value) /* Donation */
VALUES (477, 0, 'Sanguine Chaos', 90, 2, 6, 100000, 120295, 60, 4, 0, '0', 0, 20, 10, 10, 10, 200, 200, 10, 78, 9);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
        item_value, graphic_tile, graphic_equip, body_state, class_restrictions, lore, min_level, 
        stat_str, stat_sta, stat_dex, stat_int, player_hp, player_mp, weapon_delay, credits_value) /* Donation */
VALUES (478, 0, 'Scratch', 70, 2, 6, 30000, 120280, 47, 4, 0, '0', 0, 5, 5, 5, 5, 100, 100, 9, 5);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, min_level, 
	stat_ac, stat_str, stat_sta, stat_int, stat_dex, player_hp, player_mp) 
VALUES (479, 0, 'Bling Belt', 8, 0, 0, 120208, 0, 25, 25, 3, 3, 3, 3, 100, 100);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, stat_ac, stat_str, stat_dex, stat_int, stat_sta, player_hp, player_mp, min_level, event, lore) 
VALUES (480, 0, 'Enchanted Gloves', 0, 9, 0, 0, 120210, 25, 3, 3, 3, 3, 100, 100, 25, '0', '0');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (481, 3, 'Scroll: Augment', 0, 0, 124, 10000, 47, 25, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (482, 3, 'Scroll: Empower', 0, 0, 125, 10000, 31, 25, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (483, 3, 'Scroll: Bustle', 0, 0, 126, 10000, 59, 25, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (484, 3, 'Scroll: Aggravate', 0, 0, 127, 10000, 55, 25, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (485, 3, 'Scroll: Meditate', 0, 0, 128, 20000, 47, 35, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (486, 3, 'Scroll: Bulk', 0, 0, 129, 20000, 31, 35, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (487, 3, 'Scroll: Tumble', 0, 0, 130, 20000, 59, 35, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip) 
VALUES (488, 3, 'Scroll: Forge', 0, 0, 131, 20000, 55, 35, '1', 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        graphic_tile, graphic_equip, stack_size, spell_effect_id) 
VALUES (489, 1, 'Potion of Restoration', 0, 0, 120109, 0, 99, 210);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	graphic_tile, graphic_equip, stack_size, item_value, lore, bindonpickup) 
VALUES (490, 2, 'Design: Royal Leggings', 0, 0, 121102, 0, 1, 0, '1', '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	graphic_tile, graphic_equip, stack_size, item_value, lore, bindonpickup) 
VALUES (491, 2, 'Mold: Royal Legplates', 0, 0, 121138, 0, 1, 0, '1', '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	graphic_tile, graphic_equip, stack_size, item_value, lore, bindonpickup) 
VALUES (492, 2, 'Design: Royal Tunic', 0, 0, 121102, 0, 1, 0, '1', '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	graphic_tile, graphic_equip, stack_size, item_value, lore, bindonpickup) 
VALUES (493, 2, 'Mold: Royal Chestplate', 0, 0, 121138, 0, 1, 0, '1', '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, player_hp, player_mp, /* Rogue */
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, min_experience, spell_effect_id, bindonpickup) 
VALUES (494, 0, 'Royal Legplates', 11, 3, 225, 2000, 2000, 120036, 5, 50, 59, 0, 200000000, 211, '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, player_hp, /* Warrior */
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, min_experience, spell_effect_id, bindonpickup) 
VALUES (495, 0, 'Royal Legplates', 11, 3, 225, 4000, 120038, 2, 50, 55, 0, 200000000, 177, '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, player_hp, player_mp,  /* Priest */
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, min_experience, spell_effect_id, bindonpickup) 
VALUES (496, 0, 'Royal Leggings', 11, 1, 225, 1500, 2500, 120001, 1, 50, 31, 0, 200000000, 212, '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, player_mp, /* Magus */
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, min_experience, spell_effect_id, bindonpickup) 
VALUES (497, 0, 'Royal Leggings', 11, 1, 225, 4000, 120000, 6, 50, 47, 0, 200000000, 70, '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, player_hp, player_mp, /* Rogue */
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, min_experience, spell_effect_id, bindonpickup) 
VALUES (498, 0, 'Royal Chestplate', 10, 3, 450, 2500, 2500, 120011, 10, 50, 59, 0, 200000000, 213, '1');
	
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, player_hp, /* Warrior */
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, graphic_r, graphic_g, graphic_b, graphic_a, min_experience, spell_effect_id, bindonpickup) 
VALUES (499, 0, 'Royal Chestplate', 10, 3, 450, 5000, 120034, 10, 50, 55, 0, 66, 69, 189, 120, 200000000, 214, '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, player_mp, player_hp, /* Priest */
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, graphic_r, graphic_g, graphic_b, graphic_a, min_experience, spell_effect_id, bindonpickup) 
VALUES (500, 0, 'Royal Tunic', 10, 1, 450, 2000, 3000, 120026, 7, 50, 31, 0, 49, 65, 148, 160, 200000000, 215, '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, player_mp, /* Magus */
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, min_experience, spell_effect_id, bindonpickup) 
VALUES (501, 0, 'Royal Tunic', 10, 1, 450, 5000, 120025, 11, 50, 47, 0, 200000000, 215, '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, class_restrictions, lore, min_level, 
  player_hp, player_mp, min_experience, spell_effect_id, bindonequip, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (502, 0, 'Mischiefs Claw of Destruction', 200, 2, 6, 0, 120050, 15, 1, 59, '1', 50, 1500, 1500, 200000000, 216, '1', 100, 100, 150, 150);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, class_restrictions, lore, min_level, stat_ac, player_mp, player_hp,
  min_experience, spell_effect_id, bindonequip, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (503, 0, 'Mischiefs Shield of Destruction', 0, 1, 0, 0, 120262, 48, 4, 59, '1', 50, 200, 1000, 1000, 200000000, 215, '1', 100, 100, 150, 150);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, class_restrictions, lore, 
  min_level, player_mp, min_experience, spell_effect_id, bindonequip, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (504, 0, 'Wizards Staff of Enchantment', 150, 3, 8, 0, 120228, 29, 3, 47, '1', 50, 5000, 200000000, 217, '1', 100, 100, 150, 150);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, class_restrictions, lore, 
  min_level, stat_ac, player_hp, min_experience, spell_effect_id, bindonequip, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (505, 0, 'Knights Sword of Awe', 250, 3, 8, 0, 120286, 22, 4, 55, '1', 50, 275, 5000, 200000000, 218, '1', 100, 100, 150, 150);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, class_restrictions, lore, min_level, 
  player_hp, player_mp, min_experience, spell_effect_id, bindonequip, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (506, 0, 'Clerics Testament of Nobility', 150, 2, 6, 0, 120230, 30, 4, 31, '1', 50, 1000, 2000, 200000000, 77, '1', 100, 100, 150, 150);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, class_restrictions, lore, min_level, stat_ac, player_mp, player_hp, 
  min_experience, spell_effect_id, bindonequip, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (507, 0, 'Clerics Ward of Nobility', 0, 1, 0, 0, 120259, 42, 4, 31, '1', 50, 200, 1000, 1000, 200000000, 215, '1', 100, 100, 150, 150);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, min_level, 
	stat_ac, player_hp, player_mp, 
	lore, bindonequip, spell_effect_id, min_experience) 
VALUES (508, 0, 'Slayers Armguards', 6, 0, 0, 120236, 0, 50, 100, 1000, 1000, '1', '1', 219, 200000000);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, min_level, 
	stat_ac, player_hp, player_mp, 
	lore, spell_effect_id, bindonequip, min_experience) 
VALUES (509, 0, 'Slayers Belt', 8, 0, 0, 120201, 0, 50, 80, 2000, 2000, '1', 219, '1', 200000000);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, min_level, 
	stat_ac, player_hp, player_mp, 
	lore, spell_effect_id, bindonequip, min_experience) 
VALUES (510, 0, 'Slayers Gloves', 9, 0, 0, 120210, 0, 50, 80, 2000, 2000, '1', 219, '1', 200000000);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, min_level, 
	stat_ac, player_hp, player_mp, 
	lore, spell_effect_id, bindonequip, min_experience) 
VALUES (511, 0, 'Terror Necklace', 5, 0, 0, 120086, 0, 50, 75, 2000, 2000, '1', 215, '1', 200000000);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	graphic_tile, graphic_equip, stack_size, item_value, lore, bindonpickup) 
VALUES (512, 2, 'Broken Key', 0, 0, 120173, 0, 1, 0, '1', '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	graphic_tile, graphic_equip, stack_size, item_value, lore, bindonpickup) 
VALUES (513, 2, 'Broken Key', 0, 0, 120174, 0, 1, 0, '1', '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	graphic_tile, graphic_equip, stack_size, item_value, lore, bindonpickup) 
VALUES (514, 2, 'Broken Key', 0, 0, 120175, 0, 1, 0, '1', '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	graphic_tile, graphic_equip, stack_size, item_value, lore, bindonpickup) 
VALUES (515, 2, 'Broken Key', 0, 0, 120176, 0, 1, 0, '1', '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	graphic_tile, graphic_equip, stack_size, item_value, lore, bindonpickup) 
VALUES (516, 2, 'Key to the Ancients Dungeon', 0, 0, 120172, 0, 1, 0, '1', '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
  learn_spell_id, item_value, class_restrictions, min_level, lore,
  graphic_tile, graphic_equip, bindonpickup, min_experience) 
VALUES (517, 3, 'Rune: Knights Blessing', 0, 0, 135, 0, 55, 50, '1', 120152, 0, '1', 200000000)
 
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
  learn_spell_id, item_value, class_restrictions, min_level, lore,
  graphic_tile, graphic_equip, bindonpickup, min_experience) 
VALUES (518, 3, 'Rune: Wizards Curse', 0, 0, 133, 0, 47, 50, '1', 120154, 0, '1', 200000000)
 
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
  learn_spell_id, item_value, class_restrictions, min_level, lore,
  graphic_tile, graphic_equip, bindonpickup, min_experience) 
VALUES (519, 3, 'Rune: Mischiefs Craft', 0, 0, 132, 0, 59, 50, '1', 120153, 0, '1', 200000000)
  
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip, bindonpickup, min_experience) 
VALUES (520, 3, 'Rune: Clerics Blessing', 0, 0, 134, 0, 31, 50, '1', 120155, 0, '1', 200000000);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
  item_value, graphic_tile, graphic_equip, stack_size, spell_effect_id, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (521, 1, 'Hair Dye: Trouble', 15, 0, 0, 121122, 0, 99, 224, 255, 0, 125, 180); 

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
  item_value, graphic_tile, graphic_equip, stack_size, spell_effect_id, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (522, 1, 'Hair Dye: Mald''s Dye', 15, 0, 0, 121122, 0, 99, 225, 234, 139, 173, 180); 

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, 
	graphic_tile, graphic_equip, credits_value) /* Donation */
VALUES (523, 3, 'Scroll: First Aid', 0, 0, 136, 0, 0, 0, 120110, 0, 3);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, 
	graphic_tile, graphic_equip, credits_value) /* Donation */
VALUES (524, 3, 'Scroll: Recovery', 0, 0, 137, 0, 0, 0, 120110, 0, 10);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, 
	graphic_tile, graphic_equip, credits_value) /* Donation */
VALUES (525, 3, 'Scroll: Clobber', 0, 0, 138, 0, 0, 0, 120110, 0, 3);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, 
	graphic_tile, graphic_equip, credits_value) /* Donation */
VALUES (526, 3, 'Scroll: Pummel', 0, 0, 139, 0, 0, 0, 120110, 0, 10);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, 
	graphic_tile, graphic_equip) 
VALUES (527, 3, 'Scroll: First Aid', 0, 0, 136, 250000, 0, 0, 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, 
	graphic_tile, graphic_equip) 
VALUES (528, 3, 'Scroll: Recovery', 0, 0, 137, 1000000, 0, 0, 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, 
	graphic_tile, graphic_equip) 
VALUES (529, 3, 'Scroll: Clobber', 0, 0, 138, 250000, 0, 0, 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, 
	graphic_tile, graphic_equip) 
VALUES (530, 3, 'Scroll: Pummel', 0, 0, 139, 1000000, 0, 0, 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, 
	graphic_tile, graphic_equip) 
VALUES (531, 3, 'Scroll: Tame Pet', 0, 0, 140, 0, 0, 0, 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, 
	graphic_tile, graphic_equip) 
VALUES (532, 3, 'Scroll: Pet Attack', 0, 0, 141, 0, 0, 0, 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, 
	graphic_tile, graphic_equip) 
VALUES (533, 3, 'Scroll: Pet Defend', 0, 0, 142, 0, 0, 0, 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, 
	graphic_tile, graphic_equip) 
VALUES (534, 3, 'Scroll: Pet Recall', 0, 0, 143, 0, 0, 0, 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, 
	graphic_tile, graphic_equip) 
VALUES (535, 3, 'Scroll: Pet Follow', 0, 0, 144, 0, 0, 0, 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, 
	graphic_tile, graphic_equip) 
VALUES (536, 3, 'Scroll: Pet Neutral', 0, 0, 145, 0, 0, 0, 120110, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        stat_ac, stat_str, stat_sta, stat_int, stat_dex, player_mp, player_hp, res_fire, res_water, res_earth, res_spirit, res_air, 
        graphic_tile, graphic_equip, min_level, class_restrictions, item_value, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (537, 0, 'Mald''s Robe', 10, 1, 200, 20, 20, 20, 20, 200, 200, 20, 20, 20, 20, 20, 120255, 23, 1, 0, 0, 25, 28, 65, 215);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_int, stat_dex, player_hp, player_mp,  
	graphic_tile, graphic_equip, min_level, item_value, lore, event, class_restrictions, graphic_a, spell_effect_id) 
VALUES (538, 0, 'Mald''s Shades', 0, 1, 50, 10, 10, 10, 10, 75, 150, 120048, 11, 1, 300000, '0', '0', 0, 160, 71);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, min_level, stat_sta, stat_int, stat_dex, player_hp, player_mp, class_restrictions, 
	lore, bindonequip, min_experience, spell_effect_id, graphic_a) 
VALUES (539, 0, 'Mald''s Holy Sword', 120, 2, 5, 0, 120286, 22, 4, 50, 10, 10, 50, 600, 600, 59, '1', '1', 20000000, 162, 160);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, min_level, stat_ac, player_mp, lore, res_fire, res_water, res_earth, res_spirit, res_air, spell_effect_id,
	stat_sta, stat_str, stat_int, stat_dex, graphic_a) 
VALUES (540, 0, 'Mald''s Slime Shield', 0, 1, 0, 0, 120260, 44, 4, 1, 75, 100, '0', 10, 10, 10, 10, 10, 71, 3, 3, 3, 3, 160);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, stack_size, spell_effect_id, credits_value) /* Donation */
VALUES (541, 1, 'Pet Bait', 15, 0, 0, 120607, 0, 10, 230, 3);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, min_level, stat_ac, player_mp, lore, res_fire, res_water, res_earth, res_spirit, res_air, spell_effect_id,
	stat_sta, stat_str, stat_int, stat_dex, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (542, 0, 'DPS Shield', 0, 1, 0, 0, 120259, 57, 4, 1, 75, 100, '0', 10, 10, 10, 10, 10, 71, 3, 3, 3, 3, 255, 255, 255, 185);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_int, stat_dex, player_hp, player_mp,  
	graphic_tile, graphic_equip, min_level, item_value, lore, event, class_restrictions, graphic_r, graphic_g, graphic_b, graphic_a, spell_effect_id) 
VALUES (543, 0, 'Not DPS'' Helm', 0, 1, 50, 10, 10, 10, 10, 75, 150, 120288, 28, 1, 0, '0', '0', 0, 255, 255, 255, 185, 71);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_int, player_hp, player_mp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore, 
	graphic_r, graphic_g, graphic_b, graphic_a, bindonequip, min_experience) 
VALUES (544, 0, 'DPS Robe', 10, 1, 200, 10, 250, 1750, 120706, 20, 50, 15, 0, '1', 0, 0, 0, 185, '1', 20000000);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
        item_value, graphic_tile, graphic_equip, body_state, class_restrictions, lore, min_level, player_hp, player_mp, graphic_r, graphic_g, graphic_b, graphic_a, spell_effect_id,
        bindonequip, bindonpickup) 
VALUES (545, 0, 'DPS Coral', 50, 2, 5, 0, 120272, 51, 4, 0, '1', 50, 125, 125, 255, 255, 255, 185, 76, '1', '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, stack_size, spell_effect_id, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (546, 1, 'Hair Dye: Green', 15, 0, 100, 121122, 0, 99, 236, 0, 255, 0, 180);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, stack_size, spell_effect_id, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (547, 1, 'Hair Dye: Blonde', 15, 0, 100, 121122, 0, 99, 237, 253, 232, 80, 160);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, stack_size, spell_effect_id)
VALUES (548, 1, 'Teleport: PVP Event', 15, 0, 0, 120118, 0, 99, 238);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_int, stat_sta, player_hp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore, event, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (549, 0, 'Team 1 Headband', 0, 3, 19, 3, 4, 200, 120023, 2, 0, 0, 0, '1', '1', 255, 0, 0, 180);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_int, stat_sta, player_hp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore, event, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (550, 0, 'Team 2 Headband', 0, 3, 19, 3, 4, 200, 120023, 2, 0, 0, 0, '1', '1', 0, 0, 255, 180);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
        item_value, graphic_tile, graphic_equip, body_state, class_restrictions, lore, min_level, 
        stat_str, stat_sta, stat_dex, player_hp, player_mp, weapon_delay, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (551, 0, 'Mald''s Devastator Sword', 80, 2, 6, 0, 120226, 25, 4, 0, '0', 0, 10, 5, 5, 125, 125, 7, 0, 0, 0, 200);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, min_level, stat_ac, player_mp, player_hp, lore, res_fire, res_water, res_earth, res_spirit, res_air, spell_effect_id,
	stat_sta, stat_str, stat_int, stat_dex, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (552, 0, 'Mald''s Moon Shield', 0, 1, 0, 0, 120259, 57, 4, 1, 100, 100, 100, '0', 10, 10, 10, 10, 10, 78, 10, 10, 10, 10, 0, 0, 0, 185);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_int, stat_sta, player_hp, player_mp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value,graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (553, 0, 'Mald''s Monster Feet', 12, 1, 30, 5, 5, 5, 50, 100, 120705, 7, 0, 0, 0, 0, 0, 0, 185);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, stack_size, spell_effect_id, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (554, 1, 'Hair Dye: Rampant Rape', 15, 0, 100, 121122, 0, 99, 239, 25, 25, 65, 215);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_sta, stat_int, player_hp, player_mp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore, 
	graphic_r, graphic_g, graphic_b, graphic_a, bindonequip, min_experience) 
VALUES (555, 0, 'Sunshine Cloak', 10, 1, 375, 5, 15, 1750, 300, 120709, 15, 50, 51, 0, '1', 255, 255, 0, 180, '1', 20000000);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_int, stat_dex, player_hp, player_mp,  
	graphic_tile, graphic_equip, min_level, item_value, lore, event, class_restrictions, graphic_r, graphic_g, graphic_b, graphic_a, spell_effect_id) 
VALUES (556, 0, 'Egg Yolk Headband', 0, 1, 50, 10, 10, 10, 10, 75, 150, 120023, 2, 1, 0, '0', '0', 0, 255, 255, 0, 180, 71);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, min_level, stat_ac, player_mp, lore, res_fire, res_water, res_earth, res_spirit, res_air, spell_effect_id,
	stat_sta, stat_str, stat_int, stat_dex, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (557, 0, '5 In The Pink', 0, 1, 0, 0, 120259, 57, 4, 1, 75, 100, '0', 10, 10, 10, 10, 10, 71, 3, 3, 3, 3, 255, 0, 125, 180);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, stack_size, spell_effect_id, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (558, 1, 'Hair Dye: Beowulf Sperm', 15, 0, 100, 121122, 0, 99, 240, 280, 113, 39, 5180);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_sta, stat_int, player_hp, player_mp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore, 
	graphic_r, graphic_g, graphic_b, graphic_a, bindonequip, min_experience) 
VALUES (559, 0, 'Mald''s Coat', 10, 1, 375, 5, 15, 1750, 300, 120009, 14, 50, 51, 0, '1', 107, 7, 7, 205, '1', 20000000);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_int, stat_sta, stat_str, stat_dex, player_hp, player_mp,
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore, event, credits_value) /* Donation */
VALUES (560, 0, 'Penguin Costume', 0, 3, 50, 10, 3, 3, 3, 400, 200, 120900, 15, 0, 0, 0, '1', '1', 2);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_int, stat_sta, stat_str, stat_dex, player_hp, player_mp,
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore, event, credits_value) /* Donation */
VALUES (561, 0, 'Grim Reaper Costume', 0, 3, 50, 3, 10, 3, 3, 200, 400, 120901, 17, 0, 0, 0, '1', '1', 2);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_int, stat_sta, stat_str, stat_dex, player_hp, player_mp,
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore, event, credits_value) /* Donation */
VALUES (562, 0, 'Ghost Costume', 0, 3, 50, 3, 3, 10, 3, 400, 200, 120708, 19, 0, 0, 0, '1', '1', 2);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_int, stat_sta, stat_str, stat_dex, player_hp, player_mp,
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore, event, credits_value) /* Donation */
VALUES (563, 0, 'Gingerbread Man Costume', 0, 3, 50, 3, 3, 3, 10, 200, 400, 120714, 20, 0, 0, 0, '1', '1', 2);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_int, stat_sta, stat_str, stat_dex, player_hp, player_mp,
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore, event, credits_value) /* Donation */
VALUES (564, 0, 'Devil Costume', 0, 3, 80, 5, 5, 5, 5, 400, 200, 120902, 22, 0, 0, 0, '1', '1', 2);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, stack_size, spell_effect_id, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (565, 1, 'Hair Dye: Sorwind''s Dye', 15, 0, 100, 121122, 0, 99, 241, 300, 300, 300, 550);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip, bindonpickup, min_experience) 
VALUES (566, 3, 'Scroll: Ancient Healing 2', 0, 0, 146, 0, 31, 50, '1', 120110, 0, '1', 200000000);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, class_restrictions, lore, min_level, 
  player_hp, player_mp, min_experience, spell_effect_id, bindonequip, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (567, 0, 'Maser''s Vengeance', 200, 2, 6, 0, 120272, 51, 4, 59, '1', 50, 1500, 1500, 200000000, 216, '1', 1, 1, 1, 1134);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_sta, stat_int, player_hp, player_mp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore, 
	graphic_r, graphic_g, graphic_b, graphic_a, bindonequip, min_experience) 
VALUES (568, 0, 'Maser''s Revolution', 10, 1, 375, 5, 15, 1750, 300, 120009, 13, 50, 59, 0, '1', 14000, 14000, 14000, 1200, '1', 20000000);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, player_hp, player_mp, /* Rogue */
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, min_experience, spell_effect_id, bindonpickup,
  graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (569, 0, 'Azul''s CP', 10, 3, 450, 2500, 2500, 120255, 23, 50, 59, 0, 200000000, 213, '1', 0, 0, 89, 185);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, class_restrictions, lore, min_level, 
  player_hp, player_mp, min_experience, spell_effect_id, bindonequip, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (570, 0, 'Azul''s Sword', 200, 2, 6, 0, 120272, 51, 4, 59, '1', 50, 1500, 1500, 200000000, 216, '1', 0, 0, 89, 185);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_int, stat_dex, player_hp, player_mp,  
	graphic_tile, graphic_equip, min_level, item_value, lore, event, class_restrictions, graphic_r, graphic_g, graphic_b, graphic_a, spell_effect_id) 
VALUES (571, 0, 'Azul''s A Princess', 0, 1, 50, 10, 10, 10, 10, 75, 150, 120287, 29, 1, 0, '0', '0', 0, 0, 0, 89, 185, 71);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, class_restrictions, lore, min_level, stat_ac, player_mp, player_hp,
  min_experience, spell_effect_id, bindonequip, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (572, 0, 'Azul''s Shield', 0, 1, 0, 0, 120259, 57, 4, 59, '1', 50, 200, 1000, 1000, 200000000, 215, '1', 0, 0, 89, 185);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, player_hp, player_mp, /* Rogue */
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, min_experience, spell_effect_id, bindonpickup,
  graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (573, 0, 'DPS CP', 10, 3, 450, 2500, 2500, 120011, 10, 50, 59, 0, 200000000, 213, '1', 255, 255, 255, 230);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, class_restrictions, lore, min_level, 
  player_hp, player_mp, min_experience, spell_effect_id, bindonequip, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (574, 0, 'DPS Staff', 200, 2, 6, 0, 120284, 21, 3, 59, '1', 50, 1500, 1500, 200000000, 216, '1', 255, 255, 255, 230);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, class_restrictions, lore, min_level, stat_ac, player_mp, player_hp,
  min_experience, spell_effect_id, bindonequip, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (575, 0, 'DPS White Shield', 0, 1, 0, 0, 120259, 57, 4, 59, '1', 50, 200, 1000, 1000, 200000000, 215, '1', 255, 255, 255, 230);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_int, stat_dex, player_hp, player_mp,  
	graphic_tile, graphic_equip, min_level, item_value, lore, event, class_restrictions, graphic_r, graphic_g, graphic_b, graphic_a, spell_effect_id) 
VALUES (576, 0, 'DPS Doom', 0, 1, 50, 10, 10, 10, 10, 75, 150, 120287, 29, 1, 0, '0', '0', 0, 255, 255, 255, 230, 71);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip, bindonpickup, min_experience) 
VALUES (577, 3, 'Scroll: Death Touch', 0, 0, 147, 0, 31, 50, '1', 120110, 0, '1', 2000000000);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_int, stat_dex, player_hp, player_mp, lore, bindonpickup, bindonequip, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, spell_effect_id, graphic_r, graphic_g, graphic_b, graphic_a) /* Rogue */
VALUES (578, 0, 'DPS Boots', 12, 3, 100, 0, 0, 0, 50, 1000, 1000, '1', '1', '1', 120041, 4, 50, 59, 0, 76, 255, 255, 255, 230);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, player_hp, player_mp, /* Rogue */
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, min_experience, spell_effect_id, bindonpickup, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (579, 0, 'DPS Legplates', 11, 3, 225, 2000, 2000, 120036, 5, 50, 59, 0, 200000000, 211, '1', 255, 255, 255, 230);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, stat_ac, stat_str, stat_dex, stat_int, stat_sta, player_hp, player_mp, min_level, 
	lore, res_fire, res_water, res_earth, res_spirit, res_air, min_experience, bindonequip, spell_effect_id) 
VALUES (580, 0, 'Enchanted Bracelet of Fire', 0, 4, 0, 0, 120070, 40, 20, 20, 40, 20, 400, 200, 50, '1', 70, 0, 0, 0, 0, 400000000, '1', 177);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, stat_ac, stat_str, stat_dex, stat_int, stat_sta, player_hp, player_mp, min_level, 
	lore, res_fire, res_water, res_earth, res_spirit, res_air, min_experience, bindonequip, spell_effect_id) 
VALUES (581, 0, 'Enchanted Bracelet of Earth', 0, 4, 0, 0, 120072, 10, 20, 20, 40, 20, 200, 400, 50, '1', 0, 0, 70, 0, 0, 400000000, '1', 70);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, stat_ac, stat_str, stat_dex, stat_int, stat_sta, player_hp, player_mp, min_level, 
	lore, res_fire, res_water, res_earth, res_spirit, res_air, min_experience, bindonequip, spell_effect_id) 
VALUES (582, 0, 'Enchanted Bracelet of Air', 0, 4, 0, 0, 120074, 20, 20, 20, 25, 35, 250, 350, 50, '1', 0, 0, 0, 0, 70, 400000000, '1', 84);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, stat_ac, stat_str, stat_dex, stat_int, stat_sta, player_hp, player_mp, min_level, 
	lore, res_fire, res_water, res_earth, res_spirit, res_air, min_experience, bindonequip, spell_effect_id) 
VALUES (583, 0, 'Enchanted Bracelet of Water', 0, 4, 0, 0, 120076, 20, 20, 20, 30, 30, 300, 300, 50, '1', 0, 70, 0, 0, 0, 400000000, '1', 244);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, stat_ac, stat_str, stat_dex, stat_int, stat_sta, player_hp, player_mp, min_level, 
	lore, res_fire, res_water, res_earth, res_spirit, res_air, min_experience, bindonequip, spell_effect_id) 
VALUES (584, 0, 'Enchanted Bracelet of Spirit', 0, 4, 0, 0, 120079, 30, 30, 30, 30, 30, 300, 300, 50, '1', 0, 0, 0, 70, 0, 400000000, '1', 245);
	
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	graphic_tile, graphic_equip, stack_size, item_value, lore, bindonpickup) 
VALUES (585, 2, 'Howto: Bracelet Enchantment', 0, 0, 121102, 0, 1, 0, '0', '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, stat_ac, stat_str, stat_dex, stat_int, stat_sta, player_hp, player_mp, min_level, event, lore, spell_effect_id, bindonpickup) 
VALUES (586, 0, 'Prison Gloves', 0, 9, 0, 0, 120210, 70, 10, 10, 10, 10, 500, 500, 1, '0', '1', 71, '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_int, stat_sta, stat_str, stat_dex, player_hp, player_mp, /* Rogue */
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore, bindonequip, bindonpickup, spell_effect_id, graphic_a) 
VALUES (587, 0, 'Enchanted Divine Helm', 0, 3, 150, 25, 25, 0, 0, 3000, 3000, 120022, 10, 50, 59, 0, '1', '1', '1', 211, 100);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_int, stat_sta, stat_str, stat_dex, player_hp, player_mp, /* Warrior */
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore, bindonequip, bindonpickup, spell_effect_id, graphic_a) 
VALUES (588, 0, 'Enchanted Divine Helm', 0, 3, 150, 0, 50, 0, 0, 6000, 0, 120023, 2, 50, 55, 0, '1', '1', '1', 176, 100);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_int, stat_sta, stat_str, stat_dex, player_hp, player_mp, /* Magus */
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore, bindonequip, bindonpickup, spell_effect_id, graphic_a) 
VALUES (589, 0, 'Enchanted Divine Crown', 0, 3, 150, 50, 0, 0, 0, 0, 6000, 120032, 5, 50, 47, 0, '1', '1', '1', 244, 100);
  
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_int, stat_sta, stat_str, stat_dex, player_hp, player_mp, /* Priest */
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore, bindonequip, bindonpickup, spell_effect_id, graphic_a) 
VALUES (590, 0, 'Enchanted Divine Crown', 0, 3, 150, 30, 20, 0, 0, 2000, 4000, 120031, 8, 50, 31, 0, '1', '1', '1', 244, 100);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	graphic_tile, graphic_equip, stack_size, item_value, lore, bindonpickup) 
VALUES (591, 2, 'Howto: Helm Enchantment', 0, 0, 121102, 0, 1, 0, '0', '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip, bindonpickup, min_experience) 
VALUES (592, 3, 'Scroll: Group Ancient Dungeons Teleport', 0, 0, 148, 0, 47, 50, '1', 120110, 0, '1', 400000000);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip, bindonpickup, min_experience) 
VALUES (593, 3, 'Scroll: Paradise Teleport', 0, 0, 149, 0, 0, 50, '1', 120110, 0, '1', 400000000);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip, bindonpickup, min_experience) 
VALUES (594, 3, 'Scroll: Ancient Covenant', 0, 0, 150, 0, 47, 50, '1', 120110, 0, '1', 400000000);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip, bindonpickup, min_experience) 
VALUES (595, 3, 'Scroll: Ancient Sacrifice 2', 0, 0, 151, 0, 31, 50, '1', 120110, 0, '1', 400000000);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, player_mp, player_hp, /* Priest */
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, graphic_r, graphic_g, graphic_b, graphic_a, min_experience, spell_effect_id, bindonpickup) 
VALUES (596, 0, 'Slippey''s Robe', 10, 1, 450, 2000, 3000, 120254, 23, 50, 31, 0, 200, 0, 0, 180, 200000000, 215, '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	learn_spell_id, item_value, class_restrictions, min_level, lore,
	graphic_tile, graphic_equip, bindonpickup, min_experience) 
VALUES (597, 3, 'Scroll: Ancient Taunt 2', 0, 0, 152, 0, 55, 50, '1', 120110, 0, '1', 400000000);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type,
        stat_ac, stat_str, stat_sta, stat_int, stat_dex, player_hp, player_mp, /* stich's custom */
        graphic_tile, graphic_equip, min_level, item_value, lore, event, class_restrictions, graphic_r, graphic_g, graphic_b, graphic_a, spell_effect_id)
VALUES (600, 0, 'Powerful Beefs Immortality', 0, 1, 50, 10, 10, 10, 10, 75, 150, 120024, 9, 1, 0, '0', '0', 0, 0, 0, 0, 0, 71);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
        item_value, graphic_tile, graphic_equip, body_state, min_level, stat_ac, stat_sta, stat_int, stat_dex, player_hp, player_mp,
        lore, graphic_r, graphic_g, graphic_b, graphic_a, bindonequip, min_experience, spell_effect_id, class_restrictions)  /* stich's custom */
VALUES (601, 0, 'UMADBra', 0, 1, 0, 0, 120259, 57, 4, 50, 150, 10, 10, 25, 300, 300, '1', 180, 60, 25, 130, '1', 20000000, 77, 59);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        stat_ac, stat_str, stat_sta, stat_int, stat_dex, player_hp, player_mp,  
        graphic_tile, graphic_equip, min_level, item_value, lore, event, class_restrictions, graphic_r, graphic_g, graphic_b, graphic_a, spell_effect_id) 
VALUES (602, 0, 'Deji''s Black Wolf ', 0, 1, 50, 10, 10, 10, 10, 75, 150, 120222, 24, 1, 0, '0', '0', 0, 1, 1, 1, 210, 71);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        stat_ac, stat_str, stat_sta, stat_int, stat_dex, player_hp, player_mp,  
        graphic_tile, graphic_equip, min_level, item_value, lore, event, class_restrictions, graphic_r, graphic_g, graphic_b, graphic_a, spell_effect_id) 
VALUES (603, 0, 'Yuna''s White Wolf', 0, 1, 50, 10, 10, 10, 10, 75, 150, 120222, 24, 1, 0, '0', '0', 0, 255, 255, 255, 205, 71);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, stack_size, spell_effect_id, graphic_r, graphic_g, graphic_b, graphic_a) 
VALUES (604, 1, 'Hair Dye: Wesley Snipers', 15, 0, 100, 121122, 0, 99, 250, 1, 1, 1, 255);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_sta, player_hp, player_mp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore, 
	graphic_r, graphic_g, graphic_b, graphic_a, bindonequip, min_experience) 
VALUES (605, 0, 'Nakeds', 10, 1, 200, 20, 650, 1000, 120254, 0, 50, 15, 0, '1', 1, 1, 1, 250, '1', 20000000);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_int, stat_dex, player_hp, player_mp,  
	graphic_tile, graphic_equip, min_level, item_value, lore, event, class_restrictions, graphic_r, graphic_g, graphic_b, graphic_a, spell_effect_id)
VALUES (606, 0, 'Ranga Suit', 0, 1, 50, 10, 10, 10, 10, 75, 150, 120714, 20, 1, 300000, '0', '0', 0, 0, 0, 0, 0, 71);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        stat_ac, stat_str, stat_sta, stat_int, stat_dex, player_hp, player_mp, 
        graphic_tile, graphic_equip, min_level, class_restrictions, item_value) 
VALUES (607, 0, 'Stichs Pants', 11, 3, 120, 15, 15, 15, 15, 175, 150, 120000, 6, 50, 0, 0);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_str, stat_sta, stat_int, stat_dex, player_hp, player_mp, lore, bindonpickup, bindonequip, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, spell_effect_id) /* Rogue */
VALUES (608, 0, 'Stichs Boots', 12, 3, 100, 0, 0, 0, 50, 1000, 1000, '1', '1', '1', 120041, 0, 50, 59, 0, 76);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
        stat_ac, stat_str, stat_sta, stat_int, stat_dex, res_fire, res_water, res_earth, res_spirit, res_air, player_mp, player_hp, 
        graphic_tile, graphic_equip, min_level, class_restrictions, item_value, spell_effect_id, lore, bindonpickup) 
VALUES (609, 0, 'Stichs Robe', 10, 1, 200, 20, 20, 20, 20, 20, 20, 20, 20, 20, 3000, 3000, 120285, 0, 50, 0, 0, 161, '1', '1');

 INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
        item_value, graphic_tile, graphic_equip, body_state, class_restrictions, lore, min_level, player_hp, player_mp, graphic_r, graphic_g, graphic_b, graphic_a, spell_effect_id,
        bindonequip, bindonpickup) 
VALUES (610, 0, 'Deji''s Boner', 50, 2, 5, 0, 120272, 31, 4, 0, '1', 50, 125, 125, 10, 10, 255, 70, 76, '1', '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_sta, stat_int, player_hp, player_mp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore, 
	graphic_r, graphic_g, graphic_b, graphic_a, bindonequip, min_experience) 
VALUES (611, 0, 'Brokonk Armor', 10, 1, 375, 5, 15, 1750, 300, 120254, 23, 50, 51, 0, '1', 0, 0, 0, 180, '1', 20000000);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, class_restrictions, lore, min_level, player_hp, player_mp, graphic_r, graphic_g, graphic_b, graphic_a, spell_effect_id,
	bindonequip, bindonpickup) 
VALUES (612, 0, 'Brokonk Axe', 50, 2, 5, 0, 120282, 55, 4, 0, '1', 50, 125, 125, 0, 0, 0, 180, 76, '1', '1');

INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, body_state, min_level, stat_ac, player_mp, lore, res_fire, res_water, res_earth, res_spirit, res_air, spell_effect_id,
	stat_sta, stat_str, stat_int, stat_dex, graphic_a) 
VALUES (613, 0, 'Brokonk Shield', 0, 1, 0, 500000, 120259, 57, 4, 1, 75, 100, '0', 10, 10, 10, 10, 10, 71, 3, 3, 3, 3, 200);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_sta, stat_int, player_hp, player_mp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore, 
	graphic_r, graphic_g, graphic_b, graphic_a, bindonequip, min_experience) 
VALUES (614, 0, 'Lava Robes', 10, 1, 375, 5, 15, 1750, 300, 120241, 14, 50, 51, 0, '1', 255, 10, 10, 120, '1', 20000000);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	stat_ac, stat_sta, player_hp, player_mp, 
	graphic_tile, graphic_equip, min_level, class_restrictions, item_value, lore, 
	graphic_r, graphic_g, graphic_b, graphic_a, bindonequip, min_experience) 
VALUES (615, 0, 'Cloudy Robes', 10, 1, 200, 20, 650, 1000, 120241, 14, 50, 15, 0, '1', 10, 10, 255, 70, '1', 20000000);

INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, 
	item_value, graphic_tile, graphic_equip, stack_size, spell_effect_id, credits_value) /* Donation */
VALUES (616, 1, 'Pet Bait', 15, 0, 250000, 120607, 0, 10, 230, 0);

SET IDENTITY_INSERT item_templates OFF;

/*
CREATE TABLE items (
  item_id INT NOT NULL,
  item_template_id INT NOT NULL,
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
  weapon_damage SMALLINT DEFAULT 1 NOT NULL,
  item_value BIGINT DEFAULT 0 NOT NULL,
  graphic_tile INT NOT NULL,
  graphic_equip SMALLINT NOT NULL,
  graphic_r SMALLINT DEFAULT 0 NOT NULL,
  graphic_g SMALLINT DEFAULT 0 NOT NULL,
  graphic_b SMALLINT DEFAULT 0 NOT NULL,
  graphic_a SMALLINT DEFAULT 0 NOT NULL,
  stat_multiplier DECIMAL DEFAULT 1 NOT NULL,
  body_state SMALLINT DEFAULT 1 NOT NULL,
  bound CHAR(1) DEFAULT '0' NOT NULL,
  
  PRIMARY KEY(item_id)
);

INSERT INTO items (item_id, item_template_id, item_name,  
	graphic_tile, graphic_equip) VALUES (5001, 1, 'Gold', 120100, 0);

*/