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
  weapon_damage INT DEFAULT 0 NOT NULL,
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

INSERT INTO item_templates (item_template_id, item_usetype, item_name, graphic_tile, graphic_file)
VALUES (1, 7, 'Gold', 820100, 20398);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_ac, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip)
VALUES (2, 2, 'Old Rags', 3, 10, 12, 10, 332205, 2278, 2);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, body_state)
VALUES (3, 3, 'Stick', 3, 2, 16, 10, 331405, 2270, 1, 4);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, stack_size, spell_effect_id)
VALUES (4, 1, 'Small Health Potion', 50, 820115, 20398, 99, 1);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, stack_size, spell_effect_id)
VALUES (5, 1, 'Small Mana Potion', 50, 820112, 20398, 99, 2);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, stat_str, weapon_damage, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, body_state)
VALUES (6, 3, 'Tin Can', 5, 1, 13, 2, 16, 120, 331350, 2269, 60, 4);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, stack_size)
VALUES (7, 0, 'Rabbit Fur', 40, 820601, 20403, 99);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, stack_size)
VALUES (8, 0, 'Rabbit Pelt', 80, 820604, 20403, 99);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, item_slot, graphic_tile, graphic_file, graphic_equip)
VALUES (9, 2, 'Bunny Ears', 20, 20, 20, 3, 0, 332209, 2278, 3);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (10, 4, 'Scroll: Healing 1', 1, 100, '1', 820110, 20398, 31, 1);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, weapon_damage, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, class_restrictions, body_state)
VALUES (11, 3, 'Small Hammer', 5, 12, 2, 16, 500, 820039, 20397, 5, 22, 4);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, weapon_damage, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, class_restrictions, body_state)
VALUES (12, 3, 'Wooden Stave', 5, 9, 3, 17, 500, 820021, 20397, 13, 38, 5);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, weapon_damage, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, class_restrictions, body_state)
VALUES (13, 3, 'Small Dagger', 5, 12, 2, 18, 500, 331379, 2269, 11, 34, 4);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, weapon_damage, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, class_restrictions, body_state)
VALUES (14, 3, 'Small Sword', 5, 14, 2, 14, 500, 820015, 20397, 10, 50, 4);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, weapon_damage, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, class_restrictions, body_state)
VALUES (15, 3, 'Stone Hammer', 15, 15, 2, 16, 1500, 820039, 20397, 5, 22, 4);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, weapon_damage, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, class_restrictions, body_state)
VALUES (16, 3, 'Hardwood Stave', 15, 15, 3, 17, 1500, 820021, 20397, 13, 38, 5);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, weapon_damage, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, class_restrictions, body_state)
VALUES (17, 3, 'Grim Dagger', 15, 19, 2, 18, 1500, 331379, 2269, 11, 34, 4);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, weapon_damage, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, class_restrictions, body_state)
VALUES (18, 3, 'Long Sword', 15, 24, 2, 14, 1500, 820015, 20397, 10, 50, 4);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file)
VALUES (19, 0, 'Sun Flower', 50, 820120, 20398);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, stack_size)
VALUES (20, 0, 'Pile of Crap', 20, 820124, 20398, 99);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file)
VALUES (21, 0, 'Rubber Ducky', 100, 820111, 20398);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (22, 4, 'Scroll: Fortify 1', 6, 600, '1', 820110, 20398, 31, 2);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (23, 4, 'Scroll: Backstab 1', 5, 100, '1', 820110, 20398, 59, 3);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (24, 4, 'Scroll: Taunt', 1, 100, '1', 820110, 20398, 55, 4);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (25, 4, 'Scroll: Elemental Strike 1', 1, 100, '1', 820110, 20398, 47, 5);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (26, 4, 'Scroll: Elemental Strike 2', 6, 600, '1', 820110, 20398, 47, 6);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (27, 4, 'Scroll: Arcane Shield 1', 7, 700, '1', 820110, 20398, 47, 7);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (28, 4, 'Scroll: Elemental Strike 3', 11, 1100, '1', 820110, 20398, 47, 8);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (29, 4, 'Scroll: Elemental Shield 1', 12, 1200, '1', 820110, 20398, 47, 9);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (30, 4, 'Scroll: Teleportation', 14, 1400, '1', 820110, 20398, 47, 10);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (31, 4, 'Scroll: Root', 15, 1500, '1', 820110, 20398, 15, 11);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (32, 4, 'Scroll: Elemental Strike 4', 16, 1600, '1', 820110, 20398, 47, 12);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (33, 4, 'Scroll: Snare', 20, 2000, '1', 820110, 20398, 47, 13);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (34, 4, 'Scroll: Gate', 13, 1300, '1', 820110, 20398, 15, 15);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (35, 4, 'Scroll: Regeneration 1', 23, 2300, '1', 820110, 20398, 47, 16);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (36, 4, 'Scroll: Elemental Strike 5', 21, 2100, '1', 820110, 20398, 47, 14);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (37, 4, 'Scroll: Bind Self', 23, 2300, '1', 820110, 20398, 15, 17);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (38, 4, 'Scroll: Group Teleportation', 25, 2500, '1', 820110, 20398, 47, 18);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (39, 4, 'Scroll: Elemental Strike 6', 26, 2600, '1', 820110, 20398, 47, 19);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (40, 4, 'Scroll: Rampant Rage', 5, 100, '1', 820110, 20398, 55, 20);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, stat_dex, min_level, weapon_damage, item_slot, item_type, item_value, lore, graphic_tile, graphic_file, graphic_equip, class_restrictions, body_state)
VALUES (41, 3, 'Winter Blade', 5, 5, 12, 26, 2, 18, 500, '1', 331356, 2269, 73, 51, 4);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, stack_size, spell_effect_id)
VALUES (42, 1, 'Health Potion', 150, 820116, 20398, 99, 31);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, stack_size, spell_effect_id)
VALUES (43, 1, 'Mana Potion', 150, 820113, 20398, 99, 32);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, stack_size, spell_effect_id)
VALUES (44, 1, 'Large Health Potion', 300, 820117, 20398, 99, 33);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, stack_size, spell_effect_id)
VALUES (45, 1, 'Large Mana Potion', 300, 820114, 20398, 99, 34);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_sta, min_level, item_slot, item_value, lore, graphic_tile, graphic_file)
VALUES (46, 2, 'Frozen Mittens', 10, 10, 5, 1, 10, 9, 100, '1', 820210, 20399);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, stat_ac, min_level, item_slot, item_value, lore, graphic_tile, graphic_file, spell_effect_id)
VALUES (47, 2, 'Cloak of Chilling Speed', 20, 10, 15, 7, 250, '1', 810034, 20107, 73);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_int, min_level, item_slot, item_type, item_value, lore, graphic_tile, graphic_file, graphic_equip, class_restrictions)
VALUES (48, 2, 'Blizzard Robes', 10, 30, 30, 5, 12, 10, 12, 500, '1', 332201, 2278, 20, 15);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_level, item_slot, item_type, item_value, lore, graphic_tile, graphic_file, graphic_equip)
VALUES (49, 2, 'Hair Bow', 10, 10, 10, 2, 2, 2, 2, 15, 0, 12, 100, '1', 332270, 2278, 46);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, min_level, item_slot, item_value, lore, graphic_tile, graphic_file, graphic_equip, class_restrictions)
VALUES (50, 2, 'Cow Skull Helmet', 20, 10, 20, 5, 2, 15, 0, 200, '1', 332215, 2278, 12, 51);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, stack_size)
VALUES (51, 0, 'Dollar Bill', 100, 820125, 20398, 99);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip)
VALUES (52, 2, 'Shirt #297741384', 20, 20, 20, 3, 3, 3, 3, 25, 10, 12, 332212, 2278, 4);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, stat_ac, stat_str, min_level, weapon_damage, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, body_state)
VALUES (53, 3, 'Chicken Leg', 20, 10, 10, 20, 23, 2, 16, 820012, 20397, 39, 4);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, graphic_tile, graphic_file, stack_size)
VALUES (54, 0, 'Taco', 820108, 20398, 99);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, stat_ac, stat_str, min_level, weapon_damage, item_slot, item_type, item_value, lore, graphic_tile, graphic_file, graphic_equip, class_restrictions, body_state)
VALUES (55, 3, 'Encased Blade of Wet', 100, 50, 30, 45, 90, 2, 14, 5000, '1', 331381, 2269, 104, 55, 4);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, stat_ac, stat_str, stat_dex, min_level, weapon_damage, item_slot, item_type, item_value, lore, graphic_tile, graphic_file, graphic_equip, class_restrictions)
VALUES (56, 3, 'Encased Claw of Wet', 75, 25, 10, 25, 45, 77, 2, 14, 5000, '1', 331371, 2269, 90, 51);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_int, min_level, weapon_damage, item_slot, item_type, lore, graphic_tile, graphic_file, graphic_equip, class_restrictions, body_state)
VALUES (57, 3, 'Staff of Frozen Rain', 30, 100, 10, 25, 40, 35, 3, 17, '1', 331349, 2269, 61, 15, 5);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, stack_size)
VALUES (58, 0, 'Gem of Power', 100, 820502, 20402, 99);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, learn_spell_id)
VALUES (59, 4, 'Scroll: Snowman Illusion', 200, 820110, 20398, 29);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, stack_size)
VALUES (60, 0, 'Ruby', 100, 820401, 20401, 5);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (61, 4, 'Scroll: Healing 2', 11, 1100, '1', 820110, 20398, 31, 30);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (62, 4, 'Scroll: Healing 3', 21, 2100, '1', 820110, 20398, 31, 31);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (63, 4, 'Scroll: Healing 4', 31, 3100, '1', 820110, 20398, 31, 32);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (64, 4, 'Scroll: Healing 5', 41, 1000, '1', 820110, 20398, 31, 33);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (65, 4, 'Scroll: Fortify 2', 16, 1600, '1', 820110, 20398, 31, 34);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (66, 4, 'Scroll: Fortify 3', 26, 2600, '1', 820110, 20398, 31, 35);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (67, 4, 'Scroll: Fortify 4', 36, 3600, '1', 820110, 20398, 31, 36);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (68, 4, 'Scroll: Fortify 5', 46, 1000, '1', 820110, 20398, 31, 37);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (69, 4, 'Scroll: Strength 1', 9, 900, '1', 820110, 20398, 31, 38);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (70, 4, 'Scroll: Strength 2', 19, 1900, '1', 820110, 20398, 31, 39);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (71, 4, 'Scroll: Strength 3', 29, 2900, '1', 820110, 20398, 31, 40);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (72, 4, 'Scroll: Strength 4', 39, 1000, '1', 820110, 20398, 31, 41);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (73, 4, 'Scroll: Strength 5', 49, 1000, '1', 820110, 20398, 31, 42);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (74, 4, 'Scroll: Stamina 1', 12, 1200, '1', 820110, 20398, 31, 43);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (75, 4, 'Scroll: Stamina 2', 22, 2200, '1', 820110, 20398, 31, 44);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (76, 4, 'Scroll: Stamina 3', 32, 3200, '1', 820110, 20398, 31, 45);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (77, 4, 'Scroll: Stamina 4', 42, 1000, '1', 820110, 20398, 31, 46);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (78, 4, 'Scroll: Intelligence 1', 25, 2500, '1', 820110, 20398, 31, 47);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (79, 4, 'Scroll: Intelligence 2', 35, 3500, '1', 820110, 20398, 31, 48);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (80, 4, 'Scroll: Dexterity 1', 37, 1000, '1', 820110, 20398, 31, 49);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (81, 4, 'Scroll: Dexterity 2', 47, 1000, '1', 820110, 20398, 31, 50);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (82, 4, 'Scroll: Mana Regeneration 1', 34, 3400, '1', 820110, 20398, 31, 51);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (83, 4, 'Scroll: Mana Regeneration 2', 48, 500, '1', 820110, 20398, 31, 52);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (84, 4, 'Scroll: See Invisible', 18, 1800, '1', 820110, 20398, 31, 53);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (85, 4, 'Scroll: Sacrifice', 50, 3400, '1', 820110, 20398, 31, 54);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (86, 4, 'Rune: Fearsome Lash', 50, '1', 820144, 20398, 59, 55);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (87, 4, 'Rune: Sunder of Spirits', 50, '1', 820145, 20398, 55, 56);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, graphic_a, stack_size, spell_effect_id)
VALUES (88, 1, 'Hair Dye: Black', 100, 821122, 20408, 180, 99, 65);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_ac, min_level, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions)
VALUES (89, 2, 'Bronze Helmet', 13, 18, 0, 10, 800, 332292, 2278, 52, 20, 65, 30, 160, 51);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_ac, stat_str, stat_sta, min_level, item_slot, item_type, lore, event, graphic_tile, graphic_file, graphic_equip, class_restrictions)
VALUES (90, 2, 'Champions Helmet', 36, 6, 6, 50, 0, 10, '1', '1', 332238, 2278, 20, 55);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_ac, min_level, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip)
VALUES (91, 2, 'Cloth Cap', 4, 1, 0, 12, 200, 332292, 2278, 52);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_ac, min_level, item_slot, item_type, item_value, lore, graphic_tile, graphic_file, graphic_equip, class_restrictions)
VALUES (92, 2, 'Curious Skull Helmet', 77, 20, 0, 12, 35000, '1', 332292, 2278, 52, 55);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_ac, stat_sta, stat_int, min_level, item_slot, item_type, lore, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions)
VALUES (93, 2, 'Deceivers Helmet', 29, 6, 6, 50, 0, 10, '1', 332238, 2278, 20, 58, 56, 56, 180, 59);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_level, item_slot, item_type, lore, graphic_tile, graphic_file, graphic_equip, graphic_a, class_restrictions)
VALUES (94, 2, 'Devastators Helmet', 100, 100, 95, 15, 15, 15, 15, 49, 0, 10, '1', 332215, 2278, 12, 160, 51);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_dex, res_fire, res_water, res_spirit, res_air, res_earth, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions)
VALUES (95, 2, 'Gold Helmet', 50, 50, 100, 5, 5, 2, 2, 2, 2, 2, 50, 0, 10, 820251, 20399, 68, 231, 223, 107, 160, 51);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_level, item_slot, item_type, lore, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a)
VALUES (96, 2, 'Hays Tail', 50, 15, 5, 5, 5, 5, 1, 0, 12, '1', 820051, 20397, 122, 255, 120, 0, 180);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_ac, min_level, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions)
VALUES (97, 2, 'Iron Helmet', 19, 26, 0, 10, 1200, 332292, 2278, 52, 70, 70, 70, 140, 51);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_ac, min_level, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, class_restrictions)
VALUES (98, 2, 'Leather Cap', 7, 10, 0, 12, 400, 332292, 2278, 52, 19);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_level, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, spell_effect_id, credits_value)
VALUES (99, 2, 'Lucky Laurels', 75, 150, 50, 10, 10, 10, 10, 1, 0, 12, 300000, 332295, 2278, 54, 24, 81, 33, 160, 71, 10);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_ac, stat_str, stat_sta, stat_int, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions)
VALUES (100, 2, 'Priests Crown', 24, 2, 2, 2, 40, 0, 12, 332240, 2278, 22, 28, 113, 216, 180, 31);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_ac, min_level, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, class_restrictions)
VALUES (101, 2, 'Silk Cap', 8, 20, 0, 12, 800, 332240, 2278, 22, 15);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, stat_ac, stat_int, min_level, item_slot, item_type, lore, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, spell_effect_id)
VALUES (102, 2, 'Spicy Laurels', 150, 45, 20, 45, 0, 12, '1', 332295, 2278, 54, 148, 48, 49, 160, 71);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_ac, min_level, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions)
VALUES (103, 2, 'Steel Helmet', 25, 34, 0, 10, 2000, 332292, 2278, 52, 100, 100, 100, 100, 55);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_level, item_slot, item_type, lore, graphic_tile, graphic_file, graphic_equip, spell_effect_id)
VALUES (104, 2, 'True Ewe', 25, 25, 55, 5, 5, 5, 5, 50, 0, 12, '1', 332246, 2278, 32, 74);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_level, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, spell_effect_id, credits_value)
VALUES (105, 2, 'Wolfs Essence', 100, 100, 100, 5, 5, 5, 5, 1, 0, 12, 50000, 332246, 2278, 32, 152, 88, 196, 170, 182, 5);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_ac, min_level, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions)
VALUES (106, 2, 'Bronze Legplates', 20, 18, 11, 10, 1000, 51332, 2282, 14, 250, 150, 50, 140, 51);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, class_restrictions)
VALUES (107, 2, 'Champions Legplates', 75, 4, 4, 4, 4, 50, 11, 10, 332232, 2278, 6, 55);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_ac, min_level, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip)
VALUES (108, 2, 'Cloth Leggings', 7, 1, 11, 12, 250, 332204, 2278, 1);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, stat_ac, stat_str, stat_sta, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions)
VALUES (109, 2, 'Gold Legplates', 75, 75, 7, 7, 50, 11, 10, 332232, 2278, 6, 231, 223, 107, 160, 51);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_ac, min_level, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, class_restrictions)
VALUES (110, 2, 'Iron Legplates', 29, 26, 11, 10, 1500, 51332, 2282, 14, 51);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_ac, min_level, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, class_restrictions)
VALUES (111, 2, 'Leather Leggings', 9, 10, 11, 12, 500, 332204, 2278, 1, 19);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, stat_ac, stat_str, stat_sta, min_level, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a)
VALUES (112, 2, 'Lucky Legplates', 50, 35, 3, 3, 1, 11, 10, 5000, 332232, 2278, 6, 24, 81, 33, 160);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_ac, min_level, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, class_restrictions)
VALUES (113, 2, 'Silk Leggings', 15, 20, 11, 12, 1000, 332204, 2278, 1, 15);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_ac, min_level, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions)
VALUES (114, 2, 'Steel Legplates', 40, 34, 11, 10, 2500, 51332, 2282, 14, 255, 255, 255, 70, 55);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_ac, min_level, item_slot, lore, graphic_tile, graphic_file, graphic_equip, body_state)
VALUES (115, 2, 'Moon Shield', 25, 45, 1, '1', 332277, 2278, 70, 4);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_ac, min_level, item_slot, item_value, graphic_tile, graphic_file, graphic_equip, class_restrictions, body_state)
VALUES (116, 2, 'Fiber Buckler', 30, 20, 1, 2000, 332225, 2278, 21, 59, 4);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_ac, min_level, item_slot, item_value, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions, body_state)
VALUES (117, 2, 'Firebrand Buckler', 60, 35, 1, 3500, 332225, 2278, 21, 189, 93, 90, 160, 59, 4);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, res_fire, res_water, res_spirit, res_air, res_earth, min_level, item_slot, item_value, graphic_tile, graphic_file, graphic_equip, body_state, spell_effect_id, credits_value)
VALUES (118, 2, 'Firebreather Shield', 100, 75, 3, 3, 3, 3, 10, 10, 10, 10, 10, 1, 1, 500000, 820281, 20399, 355, 4, 71, 10);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_mp, stat_ac, min_level, item_slot, item_value, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions, body_state)
VALUES (119, 2, 'Firebrand Guard', 40, 40, 35, 1, 3500, 332287, 2278, 68, 189, 93, 90, 160, 15, 4);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_mp, stat_ac, min_level, item_slot, item_value, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions, body_state)
VALUES (120, 2, 'Light Guard', 20, 20, 20, 1, 2000, 332287, 2278, 68, 66, 69, 189, 160, 15, 4);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_ac, min_level, item_slot, item_value, graphic_tile, graphic_file, graphic_equip, class_restrictions, body_state)
VALUES (121, 2, 'Kite Shield', 50, 20, 1, 2000, 820259, 20399, 263, 55, 4);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_ac, min_level, item_slot, item_value, graphic_tile, graphic_file, graphic_equip, body_state)
VALUES (122, 2, 'Wooden Buckler', 10, 5, 1, 500, 332225, 2278, 21, 4);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_ac, min_level, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions)
VALUES (123, 2, 'Bronze Boots', 12, 18, 12, 10, 800, 332288, 2278, 9, 250, 150, 50, 140, 51);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_ac, min_level, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a)
VALUES (124, 2, 'Cloth Shoes', 4, 1, 12, 12, 200, 332213, 2278, 2, 181, 131, 90, 180);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, class_restrictions)
VALUES (125, 2, 'Deceivers Boots', 29, 3, 3, 3, 3, 50, 12, 10, 332288, 2278, 9, 59);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_level, item_slot, item_type, lore, graphic_tile, graphic_file, graphic_equip, graphic_a)
VALUES (126, 2, 'Devastators Boots', 50, 50, 50, 5, 5, 5, 5, 49, 12, 10, '1', 332288, 2278, 9, 160);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions)
VALUES (127, 2, 'Gold Boots', 50, 50, 50, 5, 5, 50, 12, 10, 332233, 2278, 4, 231, 223, 107, 160, 51);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_ac, min_level, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, class_restrictions)
VALUES (128, 2, 'Iron Boots', 19, 26, 12, 10, 1200, 332288, 2278, 9, 51);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_ac, min_level, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions)
VALUES (129, 2, 'Leather Boots', 10, 10, 12, 12, 400, 332213, 2278, 2, 181, 131, 90, 180, 19);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, stat_ac, stat_str, stat_sta, min_level, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a)
VALUES (130, 2, 'Lucky Boots', 50, 55, 2, 2, 1, 12, 10, 3000, 332233, 2278, 4, 24, 81, 33, 160);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_ac, min_level, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a)
VALUES (131, 2, 'Silk Slippers', 7, 20, 12, 12, 800, 332213, 2278, 2, 181, 131, 90, 180);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a)
VALUES (132, 2, 'Slippers of the Poo Flinger', 50, 25, 18, 5, 10, 10, 5, 15, 12, 12, 332213, 2278, 2, 132, 77, 49, 160);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_ac, min_level, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions)
VALUES (133, 2, 'Steel Boots', 25, 34, 12, 10, 2000, 332288, 2278, 9, 255, 255, 255, 70, 55);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_str, stat_sta, stat_dex, stat_int, min_level, item_slot, lore, event, graphic_tile, graphic_file, spell_effect_id)
VALUES (134, 2, 'Azkuros Gloves', 50, 50, 5, 5, 5, 5, 1, 9, '1', '1', 820210, 20399, 66);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, res_fire, res_water, res_spirit, res_air, res_earth, min_level, item_slot, lore, graphic_tile, graphic_file)
VALUES (135, 2, 'Beefs Immortality', 600, 600, 80, 20, 20, 20, 20, 3, 3, 3, 3, 3, 50, 4, '1', 820054, 20397);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, res_fire, res_water, res_spirit, res_air, res_earth, min_level, item_slot, lore, graphic_tile, graphic_file, graphic_r, graphic_g, graphic_b, graphic_a)
VALUES (136, 2, 'Beefs Protection', 200, 200, 60, 10, 10, 10, 10, 5, 5, 5, 5, 5, 50, 7, '1', 810034, 20107, 255, 255, 255, 100);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_slot, lore, graphic_tile, graphic_file, spell_effect_id)
VALUES (137, 2, 'Bracelet of Valiance', 40, 4, '1', 820059, 20397, 69);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_str, stat_sta, stat_dex, stat_int, min_level, item_slot, item_value, lore, event, graphic_tile, graphic_file, spell_effect_id)
VALUES (138, 2, 'Candy Necklace', 300, 300, 5, 5, 5, 5, 50, 5, 700000, '1', '1', 820082, 20397, 71);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_mp, stat_ac, stat_sta, stat_int, item_slot, item_value, lore, event, graphic_tile, graphic_file, spell_effect_id, credits_value)
VALUES (139, 2, 'Divine Pauldrons', 90, 60, 6, 6, 6, 50000, '1', '1', 820278, 20399, 66, 5);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, res_fire, res_water, res_spirit, res_air, res_earth, min_level, item_slot, graphic_tile, graphic_file)
VALUES (140, 2, 'Gloves of the Poo Flinger', 150, 75, 25, 20, 10, 10, 5, 10, 10, 5, 10, 10, 40, 9, 820209, 20399);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_mp, stat_int, min_level, item_slot, item_value, lore, graphic_tile, graphic_file)
VALUES (141, 2, 'Harvest Medallion', 30, 1, 18, 5, 50, '1', 821117, 20408);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_level, item_slot, graphic_tile, graphic_file)
VALUES (142, 2, 'Leather Gloves', 25, 25, 5, 1, 1, 1, 1, 1, 9, 820209, 20399);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_level, item_slot, item_value, graphic_tile, graphic_file, graphic_r, graphic_g, graphic_b, graphic_a, spell_effect_id, credits_value)
VALUES (143, 2, 'Lucky Necklace', 50, 50, 50, 5, 5, 5, 5, 1, 5, 800000, 820082, 20397, 24, 81, 33, 160, 71, 10);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_slot, lore, graphic_tile, graphic_file, spell_effect_id)
VALUES (144, 2, 'Ring of Valiance', 40, 4, '1', 820061, 20397, 67);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_level, item_slot, graphic_tile, graphic_file)
VALUES (145, 2, 'Savage Belt', 25, 25, 50, 5, 5, 5, 5, 50, 8, 820093, 20397);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_ac, stat_str, stat_sta, stat_int, min_level, item_slot, lore, graphic_tile, graphic_file)
VALUES (146, 2, 'Dragon Scale Belt', 40, 5, 15, 15, 43, 8, '1', 820096, 20397);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_ac, stat_dex, min_level, item_slot, item_type, item_value, lore, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions)
VALUES (147, 2, 'Blushing Coat', 35, 16, 30, 10, 12, 75000, '1', 332201, 2278, 20, 255, 128, 255, 160, 37);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_ac, min_level, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions)
VALUES (148, 2, 'Bronze Chestplate', 35, 18, 10, 10, 2000, 332231, 2278, 11, 250, 150, 50, 140, 51);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_ac, stat_str, stat_sta, stat_dex, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions)
VALUES (149, 2, 'Champions Chestplate', 100, 8, 8, 4, 50, 10, 10, 332231, 2278, 11, 66, 69, 189, 150, 55);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_ac, min_level, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip)
VALUES (150, 2, 'Cloth Tunic', 10, 1, 10, 12, 400, 332272, 2278, 21);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_dex, stat_int, min_level, item_slot, item_type, lore, graphic_tile, graphic_file, graphic_equip, class_restrictions)
VALUES (151, 2, 'Devastators Robes', 50, 200, 150, 15, 15, 49, 10, 12, '1', 332201, 2278, 20, 15);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, stat_ac, stat_str, stat_sta, stat_dex, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions)
VALUES (152, 2, 'Gold Chestplate', 150, 250, 15, 15, 10, 50, 10, 10, 51331, 2282, 36, 231, 223, 107, 160, 51);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions)
VALUES (153, 2, 'High Priests Tunic', 70, 5, 5, 5, 5, 50, 10, 12, 332245, 2278, 15, 49, 65, 148, 160, 31);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_ac, min_level, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, class_restrictions)
VALUES (154, 2, 'Iron Chestplate', 40, 26, 10, 10, 3500, 332231, 2278, 11, 51);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_ac, min_level, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, class_restrictions)
VALUES (155, 2, 'Leather Tunic', 15, 10, 10, 12, 1000, 332272, 2278, 21, 19);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_level, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a)
VALUES (156, 2, 'Lucky Robes', 50, 50, 80, 5, 5, 5, 5, 1, 10, 12, 1000, 332236, 2278, 12, 24, 81, 33, 160);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_level, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a)
VALUES (157, 2, 'Lucky Chestplate', 100, 25, 75, 5, 5, 5, 5, 1, 10, 12, 2000, 51331, 2282, 36, 24, 81, 33, 160);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_ac, min_level, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, class_restrictions)
VALUES (158, 2, 'Silk Tunic', 20, 20, 10, 12, 2000, 820002, 20397, 118, 15);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_ac, min_level, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions)
VALUES (159, 2, 'Steel Chestplate', 60, 34, 10, 10, 5000, 332231, 2278, 11, 255, 255, 255, 70, 55);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_level, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, spell_effect_id)
VALUES (160, 2, 'Thick Skin of the Boar', 60, 6, 6, 6, 6, 20, 10, 10, 1000, 332231, 2278, 11, 123, 48, 123, 160, 73);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_level, item_slot, item_type, lore, event, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a)
VALUES (161, 2, 'Tunic of the Poo Flinger', 300, 300, 125, 25, 25, 25, 25, 25, 10, 12, '1', '1', 332245, 2278, 15, 132, 77, 49, 160);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_sta, stat_dex, stat_int, min_level, item_slot, item_type, lore, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions)
VALUES (162, 2, 'Whirling Robes', 80, 300, 190, 5, 5, 30, 50, 10, 12, '1', 820002, 20397, 118, 82, 138, 156, 190, 15);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_level, item_slot, item_type, lore, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a)
VALUES (163, 2, 'Leggings of the Poo Flinger', 150, 150, 75, 15, 15, 15, 15, 25, 11, 12, '1', 332204, 2278, 1, 132, 77, 49, 160);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, stat_ac, stat_str, stat_sta, stat_dex, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, class_restrictions)
VALUES (164, 2, 'Champions Boots', 50, 25, 3, 3, 3, 50, 12, 10, 332233, 2278, 4, 55);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, stat_ac, stat_str, stat_sta, stat_dex, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, class_restrictions)
VALUES (165, 2, 'Deceivers Legplates', 75, 35, 5, 5, 5, 50, 11, 10, 51332, 2282, 14, 59);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, graphic_a)
VALUES (166, 2, 'Devastators Legplates', 75, 75, 100, 10, 10, 10, 10, 49, 11, 10, 51332, 2282, 14, 160);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, stat_ac, stat_str, stat_sta, stat_dex, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, graphic_a, class_restrictions)
VALUES (167, 2, 'Devastators Chestplate', 150, 200, 15, 15, 15, 49, 10, 10, 51331, 2282, 36, 160, 51);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, stat_ac, stat_str, stat_sta, stat_dex, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, class_restrictions)
VALUES (168, 2, 'Deceivers Chestplate', 100, 55, 10, 10, 10, 50, 10, 10, 332231, 2278, 11, 59);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, graphic_r, graphic_a, stack_size, spell_effect_id)
VALUES (169, 1, 'Hair Dye: Red', 100, 821122, 20408, 155, 160, 99, 85);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, graphic_b, graphic_a, stack_size, spell_effect_id)
VALUES (170, 1, 'Hair Dye: Blue', 100, 821122, 20408, 155, 160, 99, 86);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, graphic_a, stack_size, spell_effect_id)
VALUES (171, 1, 'Hair Dye: Grey', 100, 821122, 20408, 100, 99, 87);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, spell_effect_id)
VALUES (172, 1, 'Hair Cut: 1', 1000, 821104, 20408, 88);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, spell_effect_id)
VALUES (173, 1, 'Hair Cut: 2', 1000, 821104, 20408, 89);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, spell_effect_id)
VALUES (174, 1, 'Hair Cut: 3', 1000, 821104, 20408, 90);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, spell_effect_id)
VALUES (175, 1, 'Hair Cut: 4', 1000, 821104, 20408, 91);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, spell_effect_id)
VALUES (176, 1, 'Hair Cut: 5', 1000, 821104, 20408, 92);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, spell_effect_id)
VALUES (177, 1, 'Hair Cut: 6', 1000, 821104, 20408, 93);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, spell_effect_id)
VALUES (178, 1, 'Hair Cut: 7', 1000, 821104, 20408, 94);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, spell_effect_id)
VALUES (179, 1, 'Face: 1', 1000, 821103, 20408, 95);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, spell_effect_id)
VALUES (180, 1, 'Face: 2', 1000, 821103, 20408, 96);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, spell_effect_id)
VALUES (181, 1, 'Face: 3', 1000, 821103, 20408, 97);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, spell_effect_id)
VALUES (182, 1, 'Face: 4', 1000, 821103, 20408, 98);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, spell_effect_id)
VALUES (183, 1, 'Sexchange: Male', 1000, 820711, 20404, 99);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, spell_effect_id)
VALUES (184, 1, 'Sexchange: Female', 1000, 821104, 20408, 100);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (185, 4, 'Scroll: Arcane Shield 2', 17, 1700, '1', 820110, 20398, 47, 57);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (186, 4, 'Scroll: Group Elemental Shield 1', 12, 1500, '1', 820110, 20398, 47, 58);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (187, 4, 'Scroll: Invisibility', 28, 2800, '1', 820110, 20398, 47, 59);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (188, 4, 'Scroll: Elemental Strike 7', 31, 3100, '1', 820110, 20398, 47, 60);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (189, 4, 'Scroll: Elemental Shield 2', 32, 3200, '1', 820110, 20398, 47, 61);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (190, 4, 'Scroll: Group Elemental Shield 2', 32, 4000, '1', 820110, 20398, 47, 62);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (191, 4, 'Scroll: Regeneration 2', 33, 3300, '1', 820110, 20398, 47, 63);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (192, 4, 'Scroll: Bind Other', 33, 3300, '1', 820110, 20398, 47, 64);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (193, 4, 'Scroll: Otherlands Teleport', 34, 3400, '1', 820110, 20398, 47, 65);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (194, 4, 'Scroll: Group Otherlands Teleport', 35, 3500, '1', 820110, 20398, 47, 66);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (195, 4, 'Scroll: Elemental Strike 8', 36, 3600, '1', 820110, 20398, 47, 67);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (196, 4, 'Scroll: Arcane Shield 4', 37, 3700, '1', 820110, 20398, 47, 68);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (197, 4, 'Scroll: Elemental Strike 9', 41, 4100, '1', 820110, 20398, 47, 69);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (198, 4, 'Scroll: Regeneration 3', 43, 4300, '1', 820110, 20398, 47, 70);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (199, 4, 'Scroll: Elemental Strike 10', 46, 4600, '1', 820110, 20398, 47, 71);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (200, 4, 'Scroll: Arcane Shield 5', 47, 4700, '1', 820110, 20398, 47, 72);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (201, 4, 'Scroll: Arcane Shield 3', 27, 2700, '1', 820110, 20398, 47, 73);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_str, stat_sta, min_level, weapon_damage, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, class_restrictions, body_state)
VALUES (202, 3, 'Bastard Sword', 5, 2, 27, 40, 2, 14, 2700, 820015, 20397, 10, 55, 4);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_sta, stat_int, min_level, weapon_damage, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, class_restrictions, body_state)
VALUES (203, 3, 'Brilliant Hammer', 2, 5, 27, 19, 2, 14, 2700, 820039, 20397, 5, 31, 4);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_str, stat_sta, stat_dex, min_level, weapon_damage, item_slot, item_type, item_value, lore, graphic_tile, graphic_file, graphic_equip, class_restrictions, body_state)
VALUES (204, 3, 'Contraband Dagger', 50, 10, 2, 4, 4, 25, 33, 2, 18, 25000, '1', 331326, 2269, 30, 59, 4);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_str, stat_sta, stat_dex, min_level, weapon_damage, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, class_restrictions, body_state)
VALUES (205, 3, 'Deceivers Dagger', 5, 5, 10, 50, 85, 2, 18, 270168, 3081, 177, 59, 4);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, stat_str, stat_sta, stat_dex, min_level, weapon_damage, item_slot, item_type, lore, graphic_tile, graphic_file, graphic_equip, class_restrictions, body_state, spell_effect_id)
VALUES (206, 3, 'Devastating Dragon Tooth Sword', 100, 10, 10, 10, 50, 120, 2, 14, '1', 331353, 2269, 69, 55, 4, 115);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_sta, stat_dex, stat_int, min_level, weapon_damage, item_slot, item_type, lore, graphic_tile, graphic_file, graphic_equip, class_restrictions, body_state)
VALUES (207, 3, 'Elemental Stave', 5, 10, 10, 50, 35, 3, 15, '1', 270155, 3081, 171, 47, 6);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, weapon_damage, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, class_restrictions, body_state)
VALUES (208, 3, 'High Quality Walde', 35, 48, 2, 14, 1500, 331380, 2269, 98, 55, 4);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_str, stat_sta, stat_dex, stat_int, min_level, weapon_damage, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, body_state, credits_value)
VALUES (209, 3, 'Lucky Spear', 250, 50, 5, 5, 5, 5, 1, 100, 3, 19, 20000, 331321, 2269, 20, 6, 5);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_str, stat_sta, stat_dex, stat_int, min_level, weapon_damage, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, body_state)
VALUES (210, 3, 'Lucky Staff', 5, 50, 5, 5, 5, 5, 1, 50, 3, 19, 1000, 331349, 2269, 61, 24, 81, 33, 160, 6);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_str, stat_dex, min_level, weapon_damage, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, class_restrictions, body_state)
VALUES (211, 3, 'Malignant Dagger', 2, 5, 27, 34, 2, 18, 2700, 331379, 2269, 11, 59, 4);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_sta, stat_int, min_level, weapon_damage, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, class_restrictions, body_state)
VALUES (212, 3, 'Priests Hammer', 5, 7, 40, 34, 2, 14, 331355, 2269, 72, 31, 4);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_level, weapon_damage, item_slot, item_type, lore, graphic_tile, graphic_file, graphic_equip, body_state)
VALUES (213, 3, 'Scythe', 20, 20, 10, 4, 4, 4, 4, 30, 35, 3, 15, '1', 331351, 2269, 62, 6);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_str, min_level, weapon_damage, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, body_state)
VALUES (214, 3, 'Searing Whip', 20, 25, 40, 2, 14, 331333, 2269, 38, 115, 81, 33, 140, 4);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_dex, stat_int, min_level, weapon_damage, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, class_restrictions, body_state)
VALUES (215, 3, 'Thicket Stave', 2, 5, 27, 21, 3, 15, 2700, 820021, 20397, 13, 47, 6);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_sta, stat_dex, stat_int, min_level, weapon_damage, item_slot, item_type, lore, graphic_tile, graphic_file, graphic_equip, class_restrictions, body_state)
VALUES (216, 3, 'Devastating Birch Wood Staff', 50, 500, 50, 10, 5, 20, 50, 42, 3, 17, '1', 820227, 20399, 41, 15, 5);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_str, stat_sta, stat_dex, stat_int, min_level, weapon_damage, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, body_state)
VALUES (217, 3, 'Tiny Club', 30, 10, 10, 5, 5, 7, 50, 80, 2, 16, 10000, 331405, 2270, 1, 4);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_mp, stat_sta, stat_dex, stat_int, min_level, weapon_damage, item_slot, item_type, lore, graphic_tile, graphic_file, graphic_equip, class_restrictions, body_state)
VALUES (218, 3, 'Frays Cane', 125, 10, 5, 25, 50, 18, 3, 17, '1', 331322, 2269, 22, 15, 5);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_str, stat_sta, stat_dex, min_level, weapon_damage, item_slot, item_type, item_value, lore, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions, body_state)
VALUES (219, 3, 'Nagan Sword', 15, 5, 2, 33, 70, 2, 14, 1500, '1', 820015, 20397, 10, 107, 73, 107, 120, 55, 4);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_str, stat_sta, stat_dex, min_level, weapon_damage, item_slot, item_type, item_value, lore, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions, body_state)
VALUES (220, 3, 'Nagan Dagger', 8, 3, 10, 33, 63, 2, 18, 1500, '1', 331379, 2269, 11, 107, 73, 107, 120, 59, 4);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_sta, stat_dex, stat_int, min_level, weapon_damage, item_slot, item_type, lore, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions, body_state)
VALUES (221, 3, 'Nagan Stave', 25, 100, 5, 2, 10, 30, 22, 3, 15, '1', 270155, 3081, 171, 107, 73, 107, 160, 47, 6);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_ac, res_fire, min_level, item_slot, item_value, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_a, class_restrictions, body_state)
VALUES (222, 2, 'Firebrand Shield', 100, 5, 35, 1, 3500, 820259, 20399, 263, 150, 140, 55, 4);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_ac, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a)
VALUES (223, 2, 'Peasant Dress', 10, 10, 12, 100, 820027, 20397, 3, 251, 170, 255, 180);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions)
VALUES (224, 2, 'Priests Leggings', 25, 50, 25, 3, 3, 3, 3, 40, 11, 12, 332204, 2278, 1, 28, 113, 216, 180, 31);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_mp, stat_ac, stat_sta, stat_int, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions)
VALUES (225, 2, 'Priests Shoes', 30, 15, 2, 3, 40, 12, 12, 332213, 2278, 2, 181, 131, 90, 180, 31);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_sta, stat_int, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions)
VALUES (226, 2, 'Priests Tunic', 50, 50, 30, 4, 5, 40, 10, 12, 332245, 2278, 15, 49, 65, 148, 160, 31);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_int, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions)
VALUES (227, 2, 'Magus Leggings', 25, 50, 20, 5, 40, 11, 12, 332204, 2278, 1, 224, 27, 36, 180, 47);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_mp, stat_ac, stat_sta, stat_int, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions)
VALUES (228, 2, 'Magus Shoes', 30, 15, 2, 4, 40, 12, 12, 332213, 2278, 2, 237, 51, 59, 180, 47);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_int, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions)
VALUES (229, 2, 'Magus Tunic', 25, 75, 25, 10, 40, 10, 12, 332245, 2278, 15, 192, 28, 40, 180, 47);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_ac, stat_sta, stat_int, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions)
VALUES (230, 2, 'Magus Crown', 20, 2, 5, 40, 0, 12, 332240, 2278, 22, 224, 27, 36, 180, 47);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_ac, stat_str, stat_sta, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, class_restrictions)
VALUES (231, 2, 'Warriors Helmet', 28, 4, 4, 40, 0, 10, 332238, 2278, 20, 55);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, class_restrictions)
VALUES (232, 2, 'Warriors Legplates', 75, 50, 3, 3, 3, 3, 40, 11, 10, 332232, 2278, 6, 55);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, stat_ac, stat_str, stat_sta, stat_dex, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, class_restrictions)
VALUES (233, 2, 'Warriors Boots', 30, 18, 2, 2, 2, 40, 12, 10, 332233, 2278, 4, 55);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_ac, stat_str, stat_sta, stat_dex, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions)
VALUES (234, 2, 'Warriors Chestplate', 75, 5, 5, 3, 40, 10, 10, 332231, 2278, 11, 66, 69, 189, 120, 55);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, stat_ac, stat_sta, stat_int, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions)
VALUES (235, 2, 'Rogues Helmet', 40, 19, 4, 3, 40, 0, 10, 332238, 2278, 20, 58, 56, 56, 180, 59);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, stat_ac, stat_str, stat_sta, stat_dex, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, class_restrictions)
VALUES (236, 2, 'Rogues Legplates', 50, 25, 3, 4, 3, 40, 11, 10, 51332, 2282, 14, 59);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, class_restrictions)
VALUES (237, 2, 'Rogues Boots', 19, 2, 2, 2, 2, 40, 12, 10, 332288, 2278, 9, 59);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, stat_ac, stat_str, stat_sta, stat_dex, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, class_restrictions)
VALUES (238, 2, 'Rogues Chestplate', 70, 40, 7, 7, 7, 40, 10, 10, 332231, 2278, 11, 59);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions)
VALUES (239, 2, 'High Priests Leggings', 50, 80, 35, 5, 5, 5, 5, 50, 11, 12, 332204, 2278, 1, 28, 113, 216, 180, 31);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_mp, stat_ac, stat_sta, stat_int, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions)
VALUES (240, 2, 'High Priests Shoes', 50, 20, 3, 4, 50, 12, 12, 332213, 2278, 2, 181, 131, 90, 180, 31);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_ac, stat_str, stat_sta, stat_int, min_level, item_slot, item_type, lore, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions)
VALUES (241, 2, 'High Priests Crown', 35, 4, 4, 4, 50, 0, 12, '1', 332240, 2278, 22, 28, 113, 216, 180, 31);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_sta, stat_int, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions)
VALUES (242, 2, 'High Priests Tunic', 75, 100, 45, 6, 8, 50, 10, 12, 332245, 2278, 15, 49, 65, 148, 160, 31);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_int, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions)
VALUES (243, 2, 'Elemental Leggings', 35, 100, 30, 8, 50, 11, 12, 332204, 2278, 1, 224, 27, 36, 180, 47);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_mp, stat_ac, stat_sta, stat_int, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions)
VALUES (244, 2, 'Elemental Shoes', 40, 20, 3, 6, 50, 12, 12, 332213, 2278, 2, 237, 51, 59, 180, 47);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_int, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions)
VALUES (245, 2, 'Elemental Tunic', 40, 125, 40, 15, 50, 10, 12, 332245, 2278, 15, 192, 28, 40, 180, 47);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_ac, stat_sta, stat_int, min_level, item_slot, item_type, lore, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions)
VALUES (246, 2, 'Elemental Crown', 30, 3, 8, 50, 0, 12, '1', 332240, 2278, 22, 224, 27, 36, 180, 47);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_sta, stat_int, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions)
VALUES (247, 2, 'Whirling Leggings', 25, 120, 75, 3, 15, 50, 11, 12, 332204, 2278, 1, 82, 138, 156, 190, 15);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_mp, stat_ac, stat_sta, stat_int, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions)
VALUES (248, 2, 'Whirling Slippers', 75, 35, 5, 9, 50, 12, 12, 332213, 2278, 2, 82, 138, 156, 190, 15);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_ac, stat_sta, stat_int, min_level, item_slot, item_type, lore, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions)
VALUES (249, 2, 'Whirling Hat', 55, 5, 13, 50, 0, 12, '1', 332240, 2278, 22, 82, 138, 156, 190, 15);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_sta, stat_int, min_level, item_slot, item_type, lore, graphic_tile, graphic_file, graphic_equip, graphic_g, graphic_a, class_restrictions)
VALUES (250, 2, 'Nagan Robes', 25, 200, 75, 3, 20, 50, 10, 12, '1', 820002, 20397, 118, 128, 100, 15);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, min_level, weapon_damage, item_slot, item_type, lore, bindonpickup, bindonequip, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, body_state, spell_effect_id)
VALUES (251, 3, 'Coral Sword', 125, 125, 50, 50, 2, 14, '1', '1', '1', 331352, 2269, 64, 185, 40, 29, 160, 4, 76);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, stat_str, stat_sta, stat_dex, min_level, weapon_damage, item_slot, item_type, lore, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions, body_state, spell_effect_id)
VALUES (252, 3, 'Devastating Dagger of the Fox', 100, 5, 7, 15, 50, 100, 2, 14, '1', 331356, 2269, 73, 231, 223, 107, 130, 59, 4, 115);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_str, stat_sta, min_level, weapon_damage, item_slot, item_type, lore, graphic_tile, graphic_file, graphic_equip, class_restrictions, body_state)
VALUES (253, 3, 'Champions Blade', 25, 10, 50, 110, 2, 14, '1', 331385, 2269, 109, 55, 4);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, min_level, weapon_damage, item_slot, lore, graphic_tile, graphic_file)
VALUES (254, 2, 'Cold Beaten Sleeves', 20, 20, 20, 20, 0, 6, '1', 820233, 20399);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_level, weapon_damage, item_slot, graphic_tile, graphic_file)
VALUES (255, 2, 'Spiked Belt of the Bunny', 25, 15, 10, 3, 3, 3, 3, 20, 0, 8, 820205, 20399);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip)
VALUES (256, 2, 'Shirt of the Fallen Loser', 25, 35, 2, 2, 2, 2, 20, 10, 12, 820040, 20397, 13);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_str, stat_sta, stat_dex, min_level, weapon_damage, item_slot, item_type, lore, graphic_tile, graphic_file, graphic_equip, class_restrictions)
VALUES (257, 3, 'Hays Claw', 5, 4, 9, 50, 90, 2, 18, '1', 331371, 2269, 90, 51);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_str, stat_sta, stat_dex, min_level, weapon_damage, item_slot, item_type, lore, graphic_tile, graphic_file, graphic_equip, class_restrictions, body_state)
VALUES (258, 3, 'Bear Claw', 3, 3, 7, 50, 65, 2, 18, '1', 820050, 20397, 85, 15, 4);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_str, stat_sta, stat_dex, min_level, weapon_damage, item_slot, item_type, lore, graphic_tile, graphic_file, graphic_equip, class_restrictions, body_state)
VALUES (259, 3, 'Rusty Claw', 50, 50, 3, 2, 3, 40, 80, 2, 18, '1', 820050, 20397, 85, 51, 4);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_int, item_slot, item_type, graphic_tile, graphic_file, graphic_equip)
VALUES (260, 2, 'Frays Flippers', 50, 100, 30, 5, 5, 5, 12, 12, 331404, 2270, 7);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_str, stat_sta, stat_dex, stat_int, min_level, weapon_damage, item_slot, item_type, graphic_tile, graphic_file)
VALUES (261, 3, 'Beefs Fist', 300, 300, 20, 10, 10, 10, 50, 100, 2, 16, 820014, 20397);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, graphic_tile, graphic_file, graphic_r, graphic_a, stack_size)
VALUES (262, 0, 'Lesser Essence of Earth', 820500, 20402, 128, 100, 10);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, graphic_tile, graphic_file, graphic_g, graphic_b, graphic_a, stack_size)
VALUES (263, 0, 'Lesser Essence of Water', 820500, 20402, 128, 255, 100, 10);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, graphic_tile, graphic_file, graphic_r, graphic_a, stack_size)
VALUES (264, 0, 'Lesser Essence of Fire', 820500, 20402, 255, 100, 10);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, graphic_tile, graphic_file, graphic_r, graphic_g, graphic_b, graphic_a, stack_size)
VALUES (265, 0, 'Lesser Essence of Air', 820500, 20402, 255, 255, 255, 130, 10);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, lore, bindonpickup, graphic_tile, graphic_file)
VALUES (266, 0, 'Key to the Ancients Dungeon', '1', '1', 820171, 20398);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, lore, graphic_tile, graphic_file, graphic_r, graphic_a)
VALUES (267, 0, 'Unadorned Coral', '1', 820501, 20402, 180, 150);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, lore, graphic_tile, graphic_file)
VALUES (268, 0, 'Present 1', '1', 820102, 20398);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, lore, graphic_tile, graphic_file)
VALUES (269, 0, 'Present 2', '1', 820103, 20398);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, lore, graphic_tile, graphic_file)
VALUES (270, 0, 'Present 3', '1', 820104, 20398);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, lore, graphic_tile, graphic_file)
VALUES (271, 0, 'Present 4', '1', 820105, 20398);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (272, 4, 'Scroll: Covenant', 25, '1', 820110, 20398, 47, 88);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (273, 4, 'Scroll: Arcane Blast', 50, '1', 820110, 20398, 47, 89);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (274, 4, 'Rune: Arcane Assault', 50, '1', 820146, 20398, 47, 90);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (275, 4, 'Scroll: Spirit Strike', 50, '1', 820110, 20398, 55, 91);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (276, 4, 'Scroll: Critical Strike', 50, '1', 820110, 20398, 59, 92);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (277, 4, 'Scroll: Rejuvination', 50, 2600, '1', 820110, 20398, 31, 93);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (278, 4, 'Rune: Restore Health', 50, '1', 820147, 20398, 31, 94);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, stack_size, spell_effect_id)
VALUES (279, 1, 'Teleport: Minita', 1000, 820118, 20398, 99, 12);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, stack_size, spell_effect_id)
VALUES (280, 1, 'Teleport: Bound', 1000, 820118, 20398, 99, 17);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, graphic_tile, graphic_file, stack_size, spell_effect_id)
VALUES (281, 1, 'Teleport: Otherlands', 30, 1000, 820118, 20398, 99, 107);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_sta, stat_int, min_experience, min_level, weapon_damage, item_slot, item_type, lore, bindonequip, graphic_tile, graphic_file, graphic_equip, class_restrictions, body_state)
VALUES (282, 3, 'Ancient Moon Wand', 250, 1000, 5, 15, 20000000, 50, 80, 2, 14, '1', '1', 331328, 2269, 29, 15, 4);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_int, min_experience, min_level, item_slot, item_type, lore, bindonequip, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions)
VALUES (283, 2, 'Ancient Garb', 250, 1750, 200, 10, 20000000, 50, 10, 12, '1', '1', 332224, 2278, 9, 150, 40, 40, 150, 15);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_sta, min_experience, min_level, item_slot, item_type, lore, bindonequip, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions)
VALUES (284, 2, 'Ancient Robe', 650, 1000, 200, 20, 20000000, 50, 10, 12, '1', '1', 332236, 2278, 12, 150, 40, 40, 150, 15);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_sta, stat_int, min_level, item_slot, item_type, lore, graphic_tile, graphic_file, graphic_equip, class_restrictions)
VALUES (285, 2, 'Snake Tiara', 525, 375, 45, 8, 10, 50, 0, 12, '1', 51323, 2282, 62, 1);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_sta, stat_int, min_level, item_slot, item_type, lore, graphic_tile, graphic_file, graphic_equip, class_restrictions)
VALUES (286, 2, 'Snake Helm', 525, 375, 100, 8, 10, 50, 0, 12, '1', 51322, 2282, 61, 1);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_experience, min_level, item_slot, lore, bindonequip, graphic_tile, graphic_file, spell_effect_id)
VALUES (287, 2, 'Cloak of Power', 250, 250, 30, 5, 5, 5, 5, 20000000, 50, 7, '1', '1', 810034, 20107, 71);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_experience, min_level, item_slot, lore, bindonequip, graphic_tile, graphic_file, spell_effect_id)
VALUES (288, 2, 'Pauldrons of Power', 250, 250, 75, 5, 5, 5, 5, 20000000, 50, 6, '1', '1', 820278, 20399, 71);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_experience, min_level, item_slot, lore, bindonequip, graphic_tile, graphic_file, spell_effect_id)
VALUES (289, 2, 'Belt of Power', 250, 250, 60, 10, 10, 10, 10, 20000000, 50, 8, '1', '1', 820093, 20397, 73);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_sta, stat_int, min_experience, min_level, item_slot, item_type, lore, bindonequip, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_a, class_restrictions)
VALUES (290, 3, 'Ancient Armor', 1750, 300, 375, 5, 15, 20000000, 50, 10, 12, '1', '1', 51331, 2282, 36, 255, 77, 51);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, stat_ac, stat_str, stat_sta, min_experience, min_level, weapon_damage, item_slot, item_type, lore, bindonequip, graphic_tile, graphic_file, graphic_equip, class_restrictions, body_state, spell_effect_id)
VALUES (291, 3, 'Ancient Axe', 1250, 25, 15, 20, 20000000, 50, 150, 2, 14, '1', '1', 820282, 20399, 316, 55, 4, 162);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_sta, stat_dex, stat_int, min_experience, min_level, weapon_damage, item_slot, item_type, lore, bindonequip, graphic_tile, graphic_file, graphic_equip, class_restrictions, body_state, spell_effect_id)
VALUES (292, 3, 'Ancient Dagger', 600, 600, 10, 50, 10, 20000000, 50, 120, 2, 14, '1', '1', 331313, 2269, 27, 59, 4, 162);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_mp, stat_ac, stat_int, min_experience, min_level, weapon_damage, item_slot, lore, bindonequip, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions, body_state, spell_effect_id)
VALUES (293, 2, 'Magus Ancient Moon Shield', 600, 150, 20, 20000000, 50, 0, 1, '1', '1', 332277, 2278, 70, 90, 200, 40, 150, 47, 4, 71);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_sta, stat_int, min_experience, min_level, weapon_damage, item_slot, lore, bindonequip, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions, body_state, spell_effect_id)
VALUES (294, 2, 'Priests Ancient Moon Shield', 100, 500, 150, 5, 15, 20000000, 50, 0, 1, '1', '1', 332277, 2278, 70, 40, 160, 200, 130, 31, 4, 77);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_sta, stat_dex, stat_int, min_experience, min_level, weapon_damage, item_slot, lore, bindonequip, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions, body_state, spell_effect_id)
VALUES (295, 2, 'Rogues Ancient Moon Shield', 300, 300, 150, 10, 25, 10, 20000000, 50, 0, 1, '1', '1', 332277, 2278, 70, 20, 70, 130, 150, 59, 4, 77);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, stat_ac, stat_sta, min_experience, min_level, weapon_damage, item_slot, lore, bindonequip, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions, body_state, spell_effect_id)
VALUES (296, 2, 'Warriors Ancient Moon Shield', 600, 250, 20, 20000000, 50, 0, 1, '1', '1', 332277, 2278, 70, 180, 60, 25, 130, 55, 4, 140);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, graphic_tile, graphic_file, graphic_r, graphic_a, stack_size)
VALUES (297, 0, 'Essence of Earth', 820500, 20402, 128, 100, 10);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, graphic_tile, graphic_file, graphic_g, graphic_b, graphic_a, stack_size)
VALUES (298, 0, 'Essence of Water', 820500, 20402, 128, 255, 100, 10);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, graphic_tile, graphic_file, graphic_r, graphic_a, stack_size)
VALUES (299, 0, 'Essence of Fire', 820500, 20402, 255, 100, 10);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, graphic_tile, graphic_file, graphic_r, graphic_g, graphic_b, graphic_a, stack_size)
VALUES (300, 0, 'Essence of Air', 820500, 20402, 255, 255, 255, 130, 10);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, graphic_tile, graphic_file, stack_size, spell_effect_id)
VALUES (301, 1, 'Teleport: Paradise', 50, 2000, 820118, 20398, 99, 141);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (302, 4, 'Scroll: Group Teleport Paradise', 50, '1', 820110, 20398, 47, 95);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, lore, bindonpickup, graphic_tile, graphic_file)
VALUES (303, 0, 'Design: Ancient Shield', '1', '1', 821102, 20408);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, res_fire, min_experience, min_level, weapon_damage, item_slot, lore, bindonequip, graphic_tile, graphic_file)
VALUES (304, 2, 'Bracelet of Fire', 100, 100, 10, 10, 10, 10, 10, 50, 20000000, 50, 0, 4, '1', '1', 820070, 20397);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, res_earth, min_experience, min_level, weapon_damage, item_slot, lore, bindonequip, graphic_tile, graphic_file)
VALUES (305, 2, 'Bracelet of Earth', 100, 100, 10, 10, 10, 10, 10, 50, 20000000, 50, 0, 4, '1', '1', 820072, 20397);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, res_air, min_experience, min_level, weapon_damage, item_slot, lore, bindonequip, graphic_tile, graphic_file)
VALUES (306, 2, 'Bracelet of Air', 100, 100, 10, 10, 10, 10, 10, 50, 20000000, 50, 0, 4, '1', '1', 820074, 20397);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, res_water, min_experience, min_level, weapon_damage, item_slot, lore, bindonequip, graphic_tile, graphic_file)
VALUES (307, 2, 'Bracelet of Water', 100, 100, 10, 10, 10, 10, 10, 50, 20000000, 50, 0, 4, '1', '1', 820076, 20397);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, res_spirit, min_experience, min_level, weapon_damage, item_slot, lore, bindonequip, graphic_tile, graphic_file)
VALUES (308, 2, 'Bracelet of Spirit', 100, 100, 10, 10, 10, 10, 10, 50, 20000000, 50, 0, 4, '1', '1', 820079, 20397);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, stat_ac, min_level, weapon_damage, item_slot, item_value, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions, body_state)
VALUES (309, 2, 'Earthbrand Shield', 240, 150, 50, 0, 1, 20000, 820259, 20399, 263, 20, 150, 20, 150, 55, 4);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, min_level, weapon_damage, item_slot, item_value, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions, body_state)
VALUES (310, 2, 'Earthbrand Buckler', 120, 120, 90, 50, 0, 1, 20000, 332225, 2278, 21, 20, 150, 20, 150, 59, 4);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, min_level, weapon_damage, item_slot, item_value, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions, body_state)
VALUES (311, 2, 'Earthbrand Guard', 60, 180, 70, 50, 0, 1, 20000, 332287, 2278, 68, 20, 150, 20, 150, 15, 4);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_sta, stat_int, min_level, item_slot, item_type, lore, graphic_tile, graphic_file, graphic_equip, graphic_g, graphic_a, class_restrictions)
VALUES (312, 2, 'Nagan Armor', 200, 25, 150, 20, 3, 50, 10, 12, '1', 332231, 2278, 11, 128, 100, 51);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_experience, min_level, lore, bindonpickup, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (313, 4, 'Scroll: Ancient Healing', 20000000, 50, '1', '1', 820110, 20398, 31, 96);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_experience, min_level, lore, bindonpickup, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (314, 4, 'Scroll: Ancient Root', 20000000, 50, '1', '1', 820110, 20398, 47, 97);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_experience, min_level, lore, bindonpickup, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (315, 4, 'Scroll: Ancient Sturdiness', 20000000, 50, '1', '1', 820110, 20398, 47, 98);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_experience, min_level, lore, bindonpickup, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (316, 4, 'Scroll: Ancient Criticality', 20000000, 50, '1', '1', 820110, 20398, 59, 99);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_experience, min_level, lore, bindonpickup, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (317, 4, 'Scroll: Ancient Augmentation', 20000000, 50, '1', '1', 820110, 20398, 55, 100);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_experience, min_level, lore, bindonpickup, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (318, 4, 'Scroll: Ancient Protection', 20000000, 50, '1', '1', 820110, 20398, 31, 101);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_experience, min_level, lore, bindonpickup, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (319, 4, 'Scroll: Ancient Buffiness', 20000000, 50, '1', '1', 820110, 20398, 47, 102);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_experience, min_level, lore, bindonpickup, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (320, 4, 'Scroll: Ancient Damage', 20000000, 50, '1', '1', 820110, 20398, 59, 103);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_experience, min_level, lore, bindonpickup, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (321, 4, 'Scroll: Ancient Taunt', 20000000, 50, '1', '1', 820110, 20398, 55, 104);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_experience, min_level, lore, bindonpickup, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (322, 4, 'Scroll: Ancient Sacrifice', 20000000, 50, '1', '1', 820110, 20398, 31, 105);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (323, 4, 'Scroll: Smokebomb', 25, '1', 820110, 20398, 59, 106);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (324, 4, 'Scroll: Group Heal', 25, '1', 820110, 20398, 31, 107);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (325, 4, 'Scroll: Warrior Root', 25, '1', 820110, 20398, 55, 108);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, graphic_tile, graphic_file, graphic_r, graphic_g, graphic_a, stack_size, spell_effect_id)
VALUES (326, 1, 'Hair Dye: Lime Green', 821122, 20408, 40, 255, 160, 99, 156);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, graphic_tile, graphic_file, graphic_r, graphic_g, graphic_b, graphic_a, stack_size, spell_effect_id)
VALUES (327, 1, 'Hair Dye: Zelius'' Dye', 821122, 20408, 255, 255, 255, 180, 99, 157);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, stack_size)
VALUES (328, 0, 'Blank Scroll', 50, 820110, 20398, 10);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, lore, graphic_tile, graphic_file, learn_spell_id)
VALUES (329, 4, 'Scroll: Bat Illusion', 10, '1', 820110, 20398, 119);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, graphic_tile, graphic_file)
VALUES (330, 0, 'Flint', 820402, 20401);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, graphic_tile, graphic_file)
VALUES (331, 0, 'Bonfire', 331808, 2274);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, stack_size)
VALUES (332, 0, 'Chisel', 150, 821137, 20408, 5);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, lore, graphic_tile, graphic_file)
VALUES (333, 0, 'Garlic', '1', 821111, 20408);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, stack_size)
VALUES (334, 0, 'High Quality Blade', 500, 821139, 20408, 10);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, stack_size)
VALUES (335, 0, 'High Quality Hilt', 400, 821140, 20408, 10);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, stack_size)
VALUES (336, 0, 'Medium Quality Blade', 250, 821139, 20408, 10);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, stack_size)
VALUES (337, 0, 'Medium Quality Hilt', 200, 821140, 20408, 10);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, stack_size)
VALUES (338, 0, 'Low Quality Blade', 125, 821139, 20408, 10);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, stack_size)
VALUES (339, 0, 'Low Quality Hilt', 100, 821140, 20408, 10);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, stack_size)
VALUES (340, 0, 'Needle', 175, 821126, 20408, 5);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, stack_size)
VALUES (341, 0, 'Ink', 150, 821141, 20408, 5);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, stack_size)
VALUES (342, 0, 'Liquid Ore', 500, 821136, 20408, 10);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, stack_size)
VALUES (343, 0, 'Pearl', 100, 820400, 20401, 99);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, stack_size)
VALUES (344, 0, 'Rope', 50, 821124, 20408, 5);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, lore, graphic_tile, graphic_file)
VALUES (345, 0, 'Sharp Scissors', 250, '1', 821104, 20408);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file)
VALUES (346, 0, 'Shirt Pattern', 200, 820241, 20399);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, stack_size)
VALUES (347, 0, 'Spool of Thread', 200, 821133, 20408, 5);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, stack_size)
VALUES (348, 0, 'Spool of Blue Thread', 200, 821132, 20408, 5);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, stack_size)
VALUES (349, 0, 'Spool of Green Thread', 200, 821131, 20408, 5);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, stack_size)
VALUES (350, 0, 'Spool of Red Thread', 200, 821130, 20408, 5);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, stack_size)
VALUES (351, 0, 'Spool of Black Thread', 200, 821129, 20408, 5);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, stack_size)
VALUES (352, 0, 'Spool of Pink Thread', 200, 821128, 20408, 5);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, stack_size)
VALUES (353, 0, 'Spool of Purple Thread', 200, 821127, 20408, 5);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, stack_size)
VALUES (354, 0, 'Unrefined Ore', 500, 820504, 20402, 5);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file)
VALUES (355, 0, 'Cats Hair', 20, 821124, 20408);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, lore, graphic_tile, graphic_file, learn_spell_id)
VALUES (356, 4, 'Scroll: Shroom Illusion', 15, '1', 820110, 20398, 120);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_level, weapon_damage, item_slot, item_value, lore, graphic_tile, graphic_file)
VALUES (357, 2, 'Crude Pearl Ring', 10, 5, 15, 3, 3, 3, 3, 10, 0, 4, 250, '1', 820060, 20397);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_ac, min_level, weapon_damage, item_slot, item_value, lore, graphic_tile, graphic_file)
VALUES (358, 2, 'Crude Gold Ring', 5, 1, 0, 4, 150, '1', 820053, 20397);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_level, weapon_damage, item_slot, item_value, lore, graphic_tile, graphic_file)
VALUES (359, 2, 'Crude Ruby Ring', 10, 5, 15, 3, 3, 3, 3, 10, 0, 4, 250, '1', 820055, 20397);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_level, weapon_damage, item_slot, lore, graphic_tile, graphic_file)
VALUES (360, 2, 'Pearl Bracelet', 50, 50, 15, 4, 4, 4, 4, 10, 0, 4, '1', 820078, 20397);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_str, stat_sta, stat_dex, min_level, weapon_damage, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, class_restrictions, body_state)
VALUES (361, 3, 'High Quality Walde', 12, 4, 2, 35, 65, 2, 14, 1500, 331380, 2269, 98, 55, 4);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_str, stat_sta, min_level, weapon_damage, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, class_restrictions, body_state)
VALUES (362, 3, 'Medium Quality Walde', 5, 2, 20, 30, 2, 14, 1000, 331380, 2269, 98, 50, 4);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_str, stat_sta, min_level, weapon_damage, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, class_restrictions, body_state)
VALUES (363, 3, 'Low Quality Walde', 2, 1, 13, 26, 2, 14, 1000, 331380, 2269, 98, 50, 4);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, graphic_g, graphic_a, spell_effect_id)
VALUES (364, 2, 'Stunnah Shades', 75, 150, 50, 10, 10, 10, 10, 1, 0, 12, 332210, 2278, 4, 255, 200, 71);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a)
VALUES (365, 2, 'Valiant Helmet', 50, 0, 10, 250000, 820250, 20399, 68, 214, 214, 214, 140);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a)
VALUES (366, 2, 'Valiant Chestplate', 50, 10, 10, 250000, 51331, 2282, 36, 214, 214, 214, 140);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a)
VALUES (367, 2, 'Valiant Legplates', 50, 11, 10, 250000, 51332, 2282, 14, 214, 214, 214, 140);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a)
VALUES (368, 2, 'Valiant Boots', 50, 12, 10, 250000, 332288, 2278, 9, 214, 214, 214, 140);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a)
VALUES (369, 2, 'Valiant Cap', 50, 0, 12, 250000, 332240, 2278, 22, 214, 214, 214, 180);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a)
VALUES (370, 2, 'Valiant Robes', 50, 10, 12, 250000, 332236, 2278, 12, 214, 214, 214, 140);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a)
VALUES (371, 2, 'Valiant Mesh', 50, 10, 10, 250000, 283266, 3770, 58, 214, 214, 214, 180);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a)
VALUES (372, 2, 'Valiant Stealth', 50, 0, 10, 250000, 332238, 2278, 20, 214, 214, 214, 180);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions)
VALUES (373, 2, 'Valiant Helmet', 200, 100, 130, 10, 7, 7, 50, 0, 10, 820250, 20399, 68, 214, 214, 214, 140, 51);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, stat_ac, stat_str, stat_sta, stat_dex, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions)
VALUES (374, 2, 'Valiant Chestplate', 300, 275, 20, 20, 20, 50, 10, 10, 51331, 2282, 36, 214, 214, 214, 140, 51);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a)
VALUES (375, 2, 'Valiant Legplates', 175, 150, 120, 15, 15, 15, 15, 50, 11, 10, 51332, 2282, 14, 214, 214, 214, 140);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a)
VALUES (376, 2, 'Valiant Boots', 150, 100, 75, 10, 10, 50, 12, 10, 332288, 2278, 9, 214, 214, 214, 140);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_sta, stat_int, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions)
VALUES (377, 2, 'Valiant Cap', 200, 100, 70, 8, 18, 50, 0, 12, 332240, 2278, 22, 214, 214, 214, 180, 15);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_dex, stat_int, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions)
VALUES (378, 2, 'Valiant Robes', 125, 425, 200, 5, 35, 50, 10, 12, 332236, 2278, 12, 214, 214, 214, 140, 15);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions)
VALUES (379, 2, 'Valiant Mesh', 250, 150, 235, 20, 20, 20, 50, 10, 10, 283266, 3770, 58, 214, 214, 214, 180, 59);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions)
VALUES (380, 2, 'Valiant Stealth', 150, 150, 120, 20, 10, 20, 10, 50, 0, 10, 332238, 2278, 20, 214, 214, 214, 180, 59);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, stat_ac, stat_sta, stat_int, weapon_damage, item_slot, item_value, lore, event, graphic_tile, graphic_file, spell_effect_id, credits_value)
VALUES (381, 2, 'Celestial Pauldrons', 90, 60, 6, 6, 0, 6, 50000, '1', '1', 820278, 20399, 68, 5);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, res_fire, res_water, res_spirit, res_air, res_earth, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, credits_value)
VALUES (382, 2, 'Shades', 150, 150, 100, 10, 10, 10, 10, 10, 10, 10, 10, 10, 0, 12, 40000, 332210, 2278, 4, 5);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, res_fire, res_water, res_spirit, res_air, res_earth, min_level, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, credits_value)
VALUES (383, 2, 'Battle Gown', 200, 200, 200, 20, 20, 20, 20, 20, 20, 20, 20, 20, 1, 10, 12, 50000, 332248, 2278, 16, 224, 27, 36, 180, 5);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, weapon_damage, item_slot, item_value, graphic_tile, graphic_file, graphic_r, graphic_g, graphic_b, graphic_a, spell_effect_id, credits_value)
VALUES (384, 2, 'Lucky Belt', 75, 75, 50, 5, 5, 5, 5, 0, 8, 400000, 820093, 20397, 24, 81, 33, 160, 71, 10);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_level, item_slot, item_type, event, graphic_tile, graphic_file, graphic_equip)
VALUES (385, 2, 'Champions Sandals', 125, 125, 25, 10, 5, 10, 5, 1, 12, 12, '1', 820289, 20399, 11);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip)
VALUES (386, 2, 'Princess Dress', 100, 100, 50, 10, 10, 1, 10, 12, 820265, 20399, 23);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_int, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip)
VALUES (387, 2, 'Fire Angel Robe', 150, 150, 100, 10, 10, 10, 1, 10, 12, 332227, 2278, 10);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip)
VALUES (388, 2, 'Tuxedo', 100, 100, 25, 5, 5, 1, 10, 12, 820293, 20399, 80);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_str, stat_sta, stat_dex, weapon_damage, weapon_delay, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, body_state, credits_value)
VALUES (389, 3, 'Lucky Dagger', 125, 125, 10, 5, 5, 80, 7, 2, 18, 30000, 331320, 2269, 19, 24, 81, 33, 160, 4, 5);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_sta, stat_int, weapon_damage, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, body_state)
VALUES (390, 3, 'Slime Staff', 100, 100, 50, 10, 10, 50, 3, 17, 331342, 2269, 50, 5);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, graphic_tile, graphic_file, graphic_r, graphic_g, graphic_b, graphic_a, stack_size, spell_effect_id)
VALUES (391, 1, 'Hair Dye: Frozen Spit', 821122, 20408, 164, 219, 247, 200, 99, 159);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, graphic_tile, graphic_file, graphic_r, graphic_a, stack_size, spell_effect_id)
VALUES (392, 1, 'Hair Dye: Fayt Dye', 821122, 20408, 148, 209, 99, 158);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, min_level, weapon_damage, item_slot, item_type, lore, bindonpickup, bindonequip, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, body_state, spell_effect_id)
VALUES (393, 3, 'Haze''s Bone Sword', 125, 125, 50, 50, 2, 14, '1', '1', '1', 332513, 2281, 37, 255, 10, 10, 120, 4, 76);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, res_fire, res_water, res_spirit, res_air, res_earth, min_level, item_slot, item_type, lore, bindonpickup, graphic_tile, graphic_file, graphic_equip, spell_effect_id)
VALUES (394, 2, 'Doom Robe', 3000, 3000, 200, 20, 20, 20, 20, 20, 20, 20, 20, 20, 50, 10, 12, '1', '1', 332243, 2278, 14, 161);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, lore, bindonpickup, graphic_tile, graphic_file)
VALUES (395, 0, 'Design: Ancient Slippers', '1', '1', 821102, 20408);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, lore, bindonpickup, graphic_tile, graphic_file)
VALUES (396, 0, 'Mold: Ancient Boots', '1', '1', 821138, 20408);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, lore, bindonpickup, graphic_tile, graphic_file)
VALUES (397, 0, 'Design: Divine Crown', '1', '1', 821102, 20408);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, lore, bindonpickup, graphic_tile, graphic_file)
VALUES (398, 0, 'Mold: Divine Helm', '1', '1', 821138, 20408);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_sta, stat_dex, stat_int, min_level, weapon_damage, item_slot, item_type, lore, bindonpickup, graphic_tile, graphic_file, graphic_equip, graphic_g, graphic_a, class_restrictions, body_state, spell_effect_id)
VALUES (399, 3, 'Dagger of Contemption', 500, 1000, 10, 10, 10, 50, 120, 2, 14, '1', '1', 270168, 3081, 177, 100, 160, 1, 4, 162);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, min_level, weapon_damage, item_slot, lore, bindonpickup, graphic_tile, graphic_file, graphic_equip, graphic_b, graphic_a, body_state, spell_effect_id)
VALUES (400, 2, 'Ward of Destruction', 700, 700, 100, 50, 0, 1, '1', '1', 332287, 2278, 68, 100, 160, 4, 164);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_dex, min_level, item_slot, item_type, lore, bindonpickup, bindonequip, graphic_tile, graphic_file, graphic_equip, class_restrictions, spell_effect_id)
VALUES (401, 2, 'Ancient Boots', 1000, 1000, 100, 50, 50, 12, 10, '1', '1', '1', 332288, 2278, 9, 59, 76);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, stat_ac, stat_sta, min_level, item_slot, item_type, lore, bindonpickup, bindonequip, graphic_tile, graphic_file, graphic_equip, class_restrictions, spell_effect_id)
VALUES (402, 2, 'Ancient Boots', 2000, 100, 50, 50, 12, 10, '1', '1', '1', 332233, 2278, 4, 55, 76);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_mp, stat_ac, stat_dex, stat_int, min_level, item_slot, item_type, lore, bindonpickup, bindonequip, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions, spell_effect_id)
VALUES (403, 2, 'Ancient Slippers', 2000, 100, 25, 25, 50, 12, 10, '1', '1', '1', 332213, 2278, 2, 237, 51, 59, 180, 47, 70);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, min_level, item_slot, item_type, lore, bindonpickup, bindonequip, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions, spell_effect_id)
VALUES (404, 2, 'Ancient Slippers', 500, 1500, 100, 30, 20, 50, 12, 10, '1', '1', '1', 332213, 2278, 2, 181, 131, 90, 180, 31, 76);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_sta, stat_int, min_level, item_slot, item_type, lore, bindonpickup, bindonequip, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions, spell_effect_id)
VALUES (405, 2, 'Divine Helm', 1500, 1500, 150, 25, 25, 50, 0, 10, '1', '1', '1', 332238, 2278, 20, 58, 56, 56, 180, 59, 84);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, stat_ac, stat_sta, min_level, item_slot, item_type, lore, bindonpickup, bindonequip, graphic_tile, graphic_file, graphic_equip, class_restrictions, spell_effect_id)
VALUES (406, 2, 'Divine Helm', 3000, 150, 50, 50, 0, 10, '1', '1', '1', 332238, 2278, 20, 55, 84);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_mp, stat_ac, stat_int, min_level, item_slot, item_type, lore, bindonpickup, bindonequip, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions, spell_effect_id)
VALUES (407, 2, 'Divine Crown', 3000, 150, 50, 50, 0, 10, '1', '1', '1', 332240, 2278, 22, 224, 27, 36, 180, 47, 84);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_sta, stat_int, min_level, item_slot, item_type, lore, bindonpickup, bindonequip, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions, spell_effect_id)
VALUES (408, 2, 'Divine Crown', 1000, 2000, 150, 20, 30, 50, 0, 10, '1', '1', '1', 332240, 2278, 22, 28, 113, 216, 180, 31, 84);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_level, item_slot, item_type, lore, graphic_tile, graphic_file, graphic_equip)
VALUES (409, 2, 'Laurels', 300, 300, 50, 20, 20, 20, 20, 25, 0, 12, '1', 332295, 2278, 54);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_level, item_slot, item_type, lore, graphic_tile, graphic_file, graphic_equip)
VALUES (410, 2, 'Frilly Top', 400, 400, 50, 25, 25, 25, 25, 25, 10, 12, '1', 820290, 20399, 33);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_level, item_slot, item_type, lore, graphic_tile, graphic_file, graphic_equip)
VALUES (411, 2, 'Frilly Skirt', 200, 200, 50, 20, 20, 20, 20, 25, 11, 10, '1', 820291, 20399, 13);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_level, item_slot, item_type, lore, graphic_tile, graphic_file, graphic_equip, graphic_a)
VALUES (412, 2, 'Sandals', 100, 100, 50, 10, 10, 10, 10, 25, 12, 12, '1', 820289, 20399, 11, 160);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, lore, bindonpickup, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (413, 4, 'Scroll: Ancient Bellow', 50, '1', '1', 820110, 20398, 55, 109);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, lore, bindonpickup, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (414, 4, 'Rune: Ancient Awe', 50, '1', '1', 820148, 20398, 55, 110);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, lore, bindonpickup, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (415, 4, 'Rune: Ancient Conflagration', 50, '1', '1', 820150, 20398, 47, 112);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, lore, bindonpickup, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (416, 4, 'Rune: Ancient Death', 50, '1', '1', 820149, 20398, 59, 111);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, res_fire, res_water, res_spirit, res_air, res_earth, min_level, item_slot, item_type, lore, bindonpickup, graphic_tile, graphic_file, graphic_equip, spell_effect_id)
VALUES (417, 2, 'Doom Helm', 2000, 2000, 150, 10, 10, 10, 10, 10, 10, 10, 10, 10, 50, 0, 12, '1', '1', 332294, 2278, 53, 69);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, graphic_tile, graphic_file, graphic_r, graphic_g, graphic_b, graphic_a, stack_size, spell_effect_id)
VALUES (418, 1, 'Hair Dye: Purple Haze', 821122, 20408, 116, 12, 108, 145, 99, 169);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, lore, bindonpickup, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (419, 4, 'Rune: Ancient Blessings', 50, '1', '1', 820151, 20398, 31, 113);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (420, 4, 'Scroll: Spiritual Blessings', 50, '1', 820110, 20398, 31, 114);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_experience, min_level, item_value, graphic_tile, graphic_file, stack_size, spell_effect_id)
VALUES (421, 1, 'Teleport: Ancients Dungeon', 100000000, 50, 1000, 820118, 20398, 99, 172);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, res_fire, res_water, res_spirit, res_air, res_earth, min_level, item_slot, lore, graphic_tile, graphic_file, class_restrictions)
VALUES (422, 2, 'Savage Pauldrons of the Boar', 50, 150, 45, 2, 2, 2, 2, 2, 2, 2, 2, 2, 50, 6, '1', 820236, 20399, 15);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, res_fire, res_water, res_spirit, res_air, res_earth, min_level, item_slot, lore, graphic_tile, graphic_file, class_restrictions)
VALUES (423, 2, 'Savage Pauldrons of the Cow', 150, 50, 60, 2, 2, 2, 2, 2, 2, 2, 2, 2, 50, 6, '1', 820232, 20399, 50);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, res_fire, res_water, res_spirit, res_air, res_earth, min_level, weapon_damage, item_slot, lore, graphic_tile, graphic_file, graphic_r, graphic_a, class_restrictions, spell_effect_id)
VALUES (424, 2, 'Red Ring', 50, 50, 5, 2, 2, 2, 2, 2, 50, 0, 4, '1', 820063, 20397, 200, 200, 15, 58);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, res_fire, res_water, res_spirit, res_air, res_earth, min_level, weapon_damage, item_slot, lore, graphic_tile, graphic_file, graphic_a, class_restrictions, spell_effect_id)
VALUES (425, 2, 'Black Ring', 50, 50, 5, 2, 2, 2, 2, 2, 50, 0, 4, '1', 820063, 20397, 200, 50, 80);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, min_level, item_slot, item_type, lore, bindonpickup, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions, spell_effect_id)
VALUES (426, 2, 'Gero Robes', 1000, 3000, 50, 50, 10, 12, '1', '1', 332201, 2278, 20, 220, 50, 50, 140, 15, 71);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, min_level, item_slot, item_type, lore, bindonpickup, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions, spell_effect_id)
VALUES (427, 2, 'Mama Chestplate', 2000, 1000, 400, 50, 10, 12, '1', '1', 332231, 2278, 11, 220, 50, 50, 140, 50, 140);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, min_level, item_slot, item_type, lore, bindonpickup, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions, spell_effect_id)
VALUES (428, 2, 'Mama Headband', 1500, 1500, 150, 50, 0, 10, '1', '1', 332238, 2278, 20, 220, 50, 50, 140, 50, 174);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, min_level, item_slot, item_type, lore, bindonpickup, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions, spell_effect_id)
VALUES (429, 2, 'Mama Legplates', 1000, 1500, 150, 50, 11, 10, '1', '1', 51332, 2282, 14, 220, 50, 50, 140, 50, 174);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, min_level, item_slot, item_type, lore, bindonpickup, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions, spell_effect_id)
VALUES (430, 2, 'Mama Boots', 1000, 500, 100, 50, 12, 10, '1', '1', 332288, 2278, 9, 220, 50, 50, 140, 50, 174);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, lore, bindonpickup, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (431, 4, 'Scroll: Sacrifice II', 50, '1', '1', 820110, 20398, 31, 115);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, lore, bindonpickup, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (432, 4, 'Scroll: Damage of the Bear', 50, '1', '1', 820110, 20398, 47, 116);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, lore, bindonpickup, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (433, 4, 'Scroll: Critical Blow of the Bear', 50, '1', '1', 820110, 20398, 59, 117);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, lore, bindonpickup, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (434, 4, 'Scroll: Roar of the Bear', 50, '1', '1', 820110, 20398, 55, 118);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, res_fire, res_water, res_spirit, res_air, res_earth, min_level, item_slot, lore, bindonpickup, bindonequip, graphic_tile, graphic_file, spell_effect_id)
VALUES (435, 2, 'Ducky Pauldrons', 300, 300, 70, 10, 10, 10, 10, 10, 10, 10, 10, 10, 50, 6, '1', '1', '1', 820232, 20399, 177);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, bindonpickup, graphic_tile, graphic_file, stack_size)
VALUES (436, 0, 'Priceless Needle', '1', 821126, 20408, 5);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, bindonpickup, graphic_tile, graphic_file, stack_size)
VALUES (437, 0, 'Priceless Pattern', '1', 821102, 20408, 5);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, bindonpickup, graphic_tile, graphic_file, stack_size)
VALUES (438, 0, 'Priceless Thread', '1', 821133, 20408, 5);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, bindonpickup, graphic_tile, graphic_file, stack_size)
VALUES (439, 0, 'Priceless Ore', '1', 820504, 20402, 5);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, bindonpickup, graphic_tile, graphic_file, stack_size)
VALUES (440, 0, 'Priceless Chisel', '1', 821137, 20408, 5);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, bindonpickup, graphic_tile, graphic_file, stack_size)
VALUES (441, 0, 'Priceless Hammer', '1', 820273, 20399, 5);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, bindonpickup, graphic_tile, graphic_file, stack_size)
VALUES (442, 0, 'Wrapping Paper', '1', 850208, 20444, 5);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, bindonpickup, graphic_tile, graphic_file, stack_size)
VALUES (443, 0, 'Empty Box', '1', 821134, 20408, 5);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, stack_size)
VALUES (444, 0, 'Sketch', 75, 821108, 20408, 99);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, graphic_r, graphic_g, graphic_b, graphic_a, stack_size)
VALUES (445, 0, 'Soft Gold Ore', 200, 821138, 20408, 250, 200, 120, 140, 99);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, lore, bindonpickup, graphic_tile, graphic_file, graphic_r, graphic_g, graphic_b, graphic_a)
VALUES (446, 0, 'Gramps Fur', '1', '1', 820601, 20403, 200, 10, 10, 120);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, lore, bindonpickup, graphic_tile, graphic_file)
VALUES (447, 0, 'Blood', '1', '1', 332138, 2277);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, lore, bindonpickup, graphic_tile, graphic_file, graphic_r, graphic_g, graphic_b, graphic_a)
VALUES (448, 0, 'Berrys Hair Strand', '1', '1', 821124, 20408, 200, 10, 10, 120);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file)
VALUES (449, 0, 'Cloth', 200, 820223, 20399);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_level, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip)
VALUES (450, 2, 'Cloth Shirt', 10, 10, 30, 1, 1, 1, 1, 1, 10, 12, 400, 332205, 2278, 2);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_sta, stat_dex, min_level, weapon_damage, weapon_delay, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, class_restrictions, body_state)
VALUES (451, 3, 'Practice Katana', 1, 5, 10, 26, 9, 2, 14, 1000, 331312, 2269, 9, 50, 4);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_level, item_slot, graphic_tile, graphic_file)
VALUES (452, 2, 'Soft Belt', 10, 10, 5, 1, 1, 1, 1, 10, 8, 820096, 20397);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, min_level, item_slot, graphic_tile, graphic_file, graphic_equip)
VALUES (453, 2, 'Cat Ears', 30, 30, 30, 4, 15, 0, 332214, 2278, 9);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, min_level, item_slot, graphic_tile, graphic_file, graphic_equip, graphic_a)
VALUES (454, 2, 'Black Cat Ears', 30, 30, 30, 4, 15, 0, 332214, 2278, 9, 170);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, stack_size)
VALUES (455, 0, 'Leather Padding', 100, 820605, 20403, 99);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_str, stat_sta, stat_dex, min_level, weapon_damage, weapon_delay, item_slot, item_type, item_value, lore, graphic_tile, graphic_file, graphic_equip, class_restrictions, body_state)
VALUES (456, 3, 'Fighting Katana', 50, 50, 12, 3, 10, 30, 63, 9, 2, 18, 1500, '1', 331327, 2269, 28, 50, 4);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, graphic_r, graphic_a, stack_size)
VALUES (457, 0, 'Red Rope', 75, 821124, 20408, 160, 160, 99);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, weapon_damage, item_slot, lore, bindonpickup, graphic_tile, graphic_file, graphic_r, graphic_g, graphic_b, graphic_a, spell_effect_id)
VALUES (458, 2, 'Gero Necklace', 0, 5, '1', '1', 820601, 20403, 200, 10, 10, 120, 185);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, graphic_tile, graphic_file, graphic_r, graphic_a, spell_effect_id)
VALUES (459, 1, 'Shard of Earth', 50, 820500, 20402, 128, 125, 186);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, graphic_tile, graphic_file, graphic_g, graphic_b, graphic_a, spell_effect_id)
VALUES (460, 1, 'Shard of Water', 50, 820500, 20402, 128, 255, 125, 197);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, graphic_tile, graphic_file, graphic_r, graphic_a, spell_effect_id)
VALUES (461, 1, 'Shard of Fire', 50, 820500, 20402, 255, 130, 195);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, graphic_tile, graphic_file, graphic_r, graphic_g, graphic_b, graphic_a, spell_effect_id)
VALUES (462, 1, 'Shard of Air', 50, 820500, 20402, 255, 255, 255, 110, 198);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, graphic_tile, graphic_file, graphic_r, graphic_g, graphic_b, graphic_a, spell_effect_id)
VALUES (463, 1, 'Shard of Death', 50, 820500, 20402, 20, 20, 20, 130, 196);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, graphic_tile, graphic_file, graphic_r, graphic_a, spell_effect_id)
VALUES (464, 1, 'Shard of Strength', 50, 820500, 20402, 64, 130, 187);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, graphic_tile, graphic_file, graphic_r, graphic_g, graphic_b, graphic_a, spell_effect_id)
VALUES (465, 1, 'Shard of Love', 50, 820500, 20402, 255, 100, 255, 100, 188);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, graphic_tile, graphic_file, graphic_g, graphic_a, spell_effect_id)
VALUES (466, 1, 'Shard of Life', 50, 820500, 20402, 255, 100, 189);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, graphic_tile, graphic_file, graphic_r, graphic_g, graphic_b, graphic_a, spell_effect_id)
VALUES (467, 1, 'Shard of Hope', 50, 820500, 20402, 250, 110, 30, 115, 193);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, graphic_tile, graphic_file, graphic_r, graphic_g, graphic_b, graphic_a, spell_effect_id)
VALUES (468, 1, 'Shard of Divinity', 50, 820500, 20402, 255, 255, 255, 150, 194);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, graphic_tile, graphic_file, graphic_r, graphic_a, spell_effect_id)
VALUES (469, 1, 'Shard of Power', 50, 820500, 20402, 255, 150, 191);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, graphic_tile, graphic_file, graphic_g, graphic_b, graphic_a, spell_effect_id)
VALUES (470, 1, 'Shard of Protection', 50, 820500, 20402, 88, 176, 110, 190);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, graphic_tile, graphic_file, graphic_r, graphic_g, graphic_b, graphic_a, spell_effect_id)
VALUES (471, 1, 'Shard of Invincibility', 50, 820500, 20402, 128, 128, 128, 120, 192);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, spell_effect_id)
VALUES (472, 2, 'Hazy Ears', 75, 150, 50, 10, 10, 10, 10, 1, 0, 12, 332209, 2278, 3, 1, 1, 1, 215, 71);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, lore, bindonpickup, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (473, 4, 'Scroll: Ancient Group Healing', 50, '1', '1', 820110, 20398, 31, 121);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, lore, bindonpickup, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (474, 4, 'Scroll: Ancient Group Damage', 50, '1', '1', 820110, 20398, 59, 122);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_experience, min_level, lore, bindonpickup, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (475, 4, 'Scroll: Ancient Regeneration', 20000000, 50, '1', '1', 820110, 20398, 47, 123);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, res_fire, res_water, res_spirit, res_air, res_earth, min_level, weapon_damage, item_slot, item_value, graphic_tile, graphic_file, graphic_equip, body_state, spell_effect_id, credits_value)
VALUES (476, 2, 'Star Shield', 100, 100, 100, 10, 10, 10, 10, 10, 10, 10, 10, 10, 1, 0, 1, 10000, 332222, 2278, 18, 4, 78, 7);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_str, stat_sta, stat_dex, stat_int, weapon_damage, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, body_state, spell_effect_id, credits_value)
VALUES (477, 3, 'Sanguine Chaos', 200, 200, 20, 10, 10, 10, 90, 2, 14, 100000, 331335, 2269, 40, 4, 78, 9);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_str, stat_sta, stat_dex, stat_int, weapon_damage, weapon_delay, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, body_state, credits_value)
VALUES (478, 3, 'Scratch', 100, 100, 5, 5, 5, 5, 70, 9, 2, 18, 30000, 331329, 2269, 31, 4, 5);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_level, item_slot, graphic_tile, graphic_file)
VALUES (479, 2, 'Bling Belt', 100, 100, 25, 3, 3, 3, 3, 25, 8, 820208, 20399);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_level, weapon_damage, item_slot, graphic_tile, graphic_file)
VALUES (480, 2, 'Enchanted Gloves', 100, 100, 25, 3, 3, 3, 3, 25, 0, 9, 820210, 20399);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (481, 4, 'Scroll: Augment', 25, 10000, '1', 820110, 20398, 47, 124);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (482, 4, 'Scroll: Empower', 25, 10000, '1', 820110, 20398, 31, 125);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (483, 4, 'Scroll: Bustle', 25, 10000, '1', 820110, 20398, 59, 126);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (484, 4, 'Scroll: Aggravate', 25, 10000, '1', 820110, 20398, 55, 127);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (485, 4, 'Scroll: Meditate', 35, 20000, '1', 820110, 20398, 47, 128);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (486, 4, 'Scroll: Bulk', 35, 20000, '1', 820110, 20398, 31, 129);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (487, 4, 'Scroll: Tumble', 35, 20000, '1', 820110, 20398, 59, 130);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, item_value, lore, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (488, 4, 'Scroll: Forge', 35, 20000, '1', 820110, 20398, 55, 131);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, graphic_tile, graphic_file, stack_size, spell_effect_id)
VALUES (489, 1, 'Potion of Restoration', 820109, 20398, 99, 210);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, lore, bindonpickup, graphic_tile, graphic_file)
VALUES (490, 0, 'Design: Royal Leggings', '1', '1', 821102, 20408);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, lore, bindonpickup, graphic_tile, graphic_file)
VALUES (491, 0, 'Mold: Royal Legplates', '1', '1', 821138, 20408);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, lore, bindonpickup, graphic_tile, graphic_file)
VALUES (492, 0, 'Design: Royal Tunic', '1', '1', 821102, 20408);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, lore, bindonpickup, graphic_tile, graphic_file)
VALUES (493, 0, 'Mold: Royal Chestplate', '1', '1', 821138, 20408);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, min_experience, min_level, item_slot, item_type, bindonpickup, graphic_tile, graphic_file, graphic_equip, class_restrictions, spell_effect_id)
VALUES (494, 2, 'Royal Legplates', 2000, 2000, 225, 200000000, 50, 11, 10, '1', 51332, 2282, 14, 59, 211);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, stat_ac, min_experience, min_level, item_slot, item_type, bindonpickup, graphic_tile, graphic_file, graphic_equip, class_restrictions, spell_effect_id)
VALUES (495, 2, 'Royal Legplates', 4000, 225, 200000000, 50, 11, 10, '1', 332232, 2278, 6, 55, 177);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, min_experience, min_level, item_slot, item_type, bindonpickup, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions, spell_effect_id)
VALUES (496, 2, 'Royal Leggings', 1500, 2500, 225, 200000000, 50, 11, 12, '1', 332204, 2278, 1, 28, 113, 216, 180, 31, 212);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_mp, stat_ac, min_experience, min_level, item_slot, item_type, bindonpickup, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions, spell_effect_id)
VALUES (497, 2, 'Royal Leggings', 4000, 225, 200000000, 50, 11, 12, '1', 332204, 2278, 1, 224, 27, 36, 180, 47, 70);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, min_experience, min_level, item_slot, item_type, bindonpickup, graphic_tile, graphic_file, graphic_equip, class_restrictions, spell_effect_id)
VALUES (498, 2, 'Royal Chestplate', 2500, 2500, 450, 200000000, 50, 10, 10, '1', 332231, 2278, 11, 59, 213);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, stat_ac, min_experience, min_level, item_slot, item_type, bindonpickup, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions, spell_effect_id)
VALUES (499, 2, 'Royal Chestplate', 5000, 450, 200000000, 50, 10, 10, '1', 332231, 2278, 11, 66, 69, 189, 120, 55, 214);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, min_experience, min_level, item_slot, item_type, bindonpickup, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions, spell_effect_id)
VALUES (500, 2, 'Royal Tunic', 3000, 2000, 450, 200000000, 50, 10, 12, '1', 332245, 2278, 15, 49, 65, 148, 160, 31, 215);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_mp, stat_ac, min_experience, min_level, item_slot, item_type, bindonpickup, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions, spell_effect_id)
VALUES (501, 2, 'Royal Tunic', 5000, 450, 200000000, 50, 10, 12, '1', 332245, 2278, 15, 192, 28, 40, 180, 47, 215);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, min_experience, min_level, weapon_damage, item_slot, item_type, lore, bindonequip, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions, spell_effect_id)
VALUES (502, 3, 'Mischiefs Claw of Destruction', 1500, 1500, 200000000, 50, 200, 2, 18, '1', '1', 331371, 2269, 90, 100, 100, 150, 150, 59, 216);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, min_experience, min_level, weapon_damage, item_slot, lore, bindonequip, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions, body_state, spell_effect_id)
VALUES (503, 3, 'Mischiefs Shield of Destruction', 1000, 1000, 200, 200000000, 50, 0, 1, '1', '1', 332222, 2278, 18, 100, 100, 150, 150, 59, 4, 215);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_mp, min_experience, min_level, weapon_damage, item_slot, item_type, lore, bindonequip, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions, body_state, spell_effect_id)
VALUES (504, 3, 'Wizards Staff of Enchantment', 5000, 200000000, 50, 150, 3, 15, '1', '1', 331349, 2269, 61, 100, 100, 150, 150, 47, 6, 217);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, stat_ac, min_experience, min_level, weapon_damage, item_slot, item_type, lore, bindonequip, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions, body_state, spell_effect_id)
VALUES (505, 3, 'Knights Sword of Awe', 5000, 275, 200000000, 50, 250, 3, 15, '1', '1', 331382, 2269, 107, 100, 100, 150, 150, 55, 6, 218);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, min_experience, min_level, weapon_damage, item_slot, item_type, lore, bindonequip, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions, body_state, spell_effect_id)
VALUES (506, 3, 'Clerics Testament of Nobility', 1000, 2000, 200000000, 50, 150, 2, 16, '1', '1', 331358, 2269, 75, 100, 100, 150, 150, 31, 4, 77);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, min_experience, min_level, weapon_damage, item_slot, lore, bindonequip, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions, body_state, spell_effect_id)
VALUES (507, 2, 'Clerics Ward of Nobility', 1000, 1000, 200, 200000000, 50, 0, 1, '1', '1', 820259, 20399, 263, 100, 100, 150, 150, 31, 4, 215);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, min_experience, min_level, item_slot, lore, bindonequip, graphic_tile, graphic_file, spell_effect_id)
VALUES (508, 2, 'Slayers Armguards', 1000, 1000, 100, 200000000, 50, 6, '1', '1', 820236, 20399, 219);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, min_experience, min_level, item_slot, lore, bindonequip, graphic_tile, graphic_file, spell_effect_id)
VALUES (509, 2, 'Slayers Belt', 2000, 2000, 80, 200000000, 50, 8, '1', '1', 820201, 20399, 219);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, min_experience, min_level, item_slot, lore, bindonequip, graphic_tile, graphic_file, spell_effect_id)
VALUES (510, 2, 'Slayers Gloves', 2000, 2000, 80, 200000000, 50, 9, '1', '1', 820210, 20399, 219);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, min_experience, min_level, item_slot, lore, bindonequip, graphic_tile, graphic_file, spell_effect_id)
VALUES (511, 2, 'Terror Necklace', 2000, 2000, 75, 200000000, 50, 5, '1', '1', 820086, 20397, 215);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, lore, bindonpickup, graphic_tile, graphic_file)
VALUES (512, 0, 'Broken Key', '1', '1', 820173, 20398);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, lore, bindonpickup, graphic_tile, graphic_file)
VALUES (513, 0, 'Broken Key', '1', '1', 820174, 20398);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, lore, bindonpickup, graphic_tile, graphic_file)
VALUES (514, 0, 'Broken Key', '1', '1', 820175, 20398);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, lore, bindonpickup, graphic_tile, graphic_file)
VALUES (515, 0, 'Broken Key', '1', '1', 820176, 20398);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, lore, bindonpickup, graphic_tile, graphic_file)
VALUES (516, 0, 'Key to the Ancients Dungeon', '1', '1', 820172, 20398);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_experience, min_level, lore, bindonpickup, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (517, 4, 'Rune: Knights Blessing', 200000000, 50, '1', '1', 820152, 20398, 55, 135);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_experience, min_level, lore, bindonpickup, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (518, 4, 'Rune: Wizards Curse', 200000000, 50, '1', '1', 820154, 20398, 47, 133);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_experience, min_level, lore, bindonpickup, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (519, 4, 'Rune: Mischiefs Craft', 200000000, 50, '1', '1', 820153, 20398, 59, 132);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_experience, min_level, lore, bindonpickup, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (520, 4, 'Rune: Clerics Blessing', 200000000, 50, '1', '1', 820155, 20398, 31, 134);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, graphic_tile, graphic_file, graphic_r, graphic_b, graphic_a, stack_size, spell_effect_id)
VALUES (521, 1, 'Hair Dye: Trouble', 821122, 20408, 255, 125, 180, 99, 224);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, graphic_tile, graphic_file, graphic_r, graphic_g, graphic_b, graphic_a, stack_size, spell_effect_id)
VALUES (522, 1, 'Hair Dye: Mald''s Dye', 821122, 20408, 234, 139, 173, 180, 99, 225);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, graphic_tile, graphic_file, learn_spell_id, credits_value)
VALUES (523, 4, 'Scroll: First Aid', 820110, 20398, 136, 3);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, graphic_tile, graphic_file, learn_spell_id, credits_value)
VALUES (524, 4, 'Scroll: Recovery', 820110, 20398, 137, 10);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, graphic_tile, graphic_file, learn_spell_id, credits_value)
VALUES (525, 4, 'Scroll: Clobber', 820110, 20398, 138, 3);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, graphic_tile, graphic_file, learn_spell_id, credits_value)
VALUES (526, 4, 'Scroll: Pummel', 820110, 20398, 139, 10);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, learn_spell_id)
VALUES (527, 4, 'Scroll: First Aid', 250000, 820110, 20398, 136);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, learn_spell_id)
VALUES (528, 4, 'Scroll: Recovery', 400000, 820110, 20398, 137);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, learn_spell_id)
VALUES (529, 4, 'Scroll: Clobber', 250000, 820110, 20398, 138);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, learn_spell_id)
VALUES (530, 4, 'Scroll: Pummel', 400000, 820110, 20398, 139);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_description, graphic_tile, graphic_file, learn_spell_id, credits_value)
VALUES (531, 4, 'Scroll: Tame Pet', 'Has a 6 hour cooldown.', 820110, 20398, 140, 30);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, graphic_tile, graphic_file, learn_spell_id)
VALUES (532, 4, 'Scroll: Pet Attack', 820110, 20398, 141);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, graphic_tile, graphic_file, learn_spell_id)
VALUES (533, 4, 'Scroll: Pet Defend', 820110, 20398, 142);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, graphic_tile, graphic_file, learn_spell_id)
VALUES (534, 4, 'Scroll: Pet Recall', 820110, 20398, 143);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, graphic_tile, graphic_file, learn_spell_id)
VALUES (535, 4, 'Scroll: Pet Follow', 820110, 20398, 144);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, graphic_tile, graphic_file, learn_spell_id)
VALUES (536, 4, 'Scroll: Pet Neutral', 820110, 20398, 145);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, res_fire, res_water, res_spirit, res_air, res_earth, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a)
VALUES (537, 2, 'Mald''s Robe', 200, 200, 200, 20, 20, 20, 20, 20, 20, 20, 20, 20, 1, 10, 12, 332236, 2278, 12, 25, 28, 65, 215);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_level, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, graphic_a, spell_effect_id)
VALUES (538, 2, 'Mald''s Shades', 75, 150, 50, 10, 10, 10, 10, 1, 0, 12, 300000, 332210, 2278, 4, 160, 71);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_sta, stat_dex, stat_int, min_experience, min_level, weapon_damage, item_slot, item_type, lore, bindonequip, graphic_tile, graphic_file, graphic_equip, graphic_a, class_restrictions, body_state, spell_effect_id)
VALUES (539, 3, 'Mald''s Holy Sword', 600, 600, 10, 50, 10, 20000000, 50, 120, 2, 14, '1', '1', 331382, 2269, 107, 160, 59, 4, 162);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, res_fire, res_water, res_spirit, res_air, res_earth, min_level, weapon_damage, item_slot, graphic_tile, graphic_file, graphic_equip, graphic_a, body_state, spell_effect_id)
VALUES (540, 2, 'Mald''s Slime Shield', 100, 75, 3, 3, 3, 3, 10, 10, 10, 10, 10, 1, 0, 1, 332223, 2278, 17, 160, 4, 71);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, graphic_tile, graphic_file, stack_size, spell_effect_id, credits_value)
VALUES (541, 1, 'Pet Bait', 820607, 20403, 10, 230, 3);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, res_fire, res_water, res_spirit, res_air, res_earth, min_level, weapon_damage, item_slot, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, body_state, spell_effect_id)
VALUES (542, 2, 'DPS Shield', 100, 75, 3, 3, 3, 3, 10, 10, 10, 10, 10, 1, 0, 1, 332277, 2278, 70, 255, 255, 255, 185, 4, 71);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, spell_effect_id)
VALUES (543, 2, 'Not DPS'' Helm', 75, 150, 50, 10, 10, 10, 10, 1, 0, 12, 332295, 2278, 54, 255, 255, 255, 185, 71);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_int, min_experience, min_level, item_slot, item_type, lore, bindonequip, graphic_tile, graphic_file, graphic_equip, graphic_a, class_restrictions)
VALUES (544, 2, 'DPS Robe', 250, 1750, 200, 10, 20000000, 50, 10, 12, '1', '1', 332224, 2278, 9, 185, 15);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, min_level, weapon_damage, item_slot, item_type, lore, bindonpickup, bindonequip, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, body_state, spell_effect_id)
VALUES (545, 3, 'DPS Coral', 125, 125, 50, 50, 2, 14, '1', '1', '1', 331352, 2269, 64, 255, 255, 255, 185, 4, 76);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, graphic_g, graphic_a, stack_size, spell_effect_id)
VALUES (546, 1, 'Hair Dye: Green', 100, 821122, 20408, 255, 180, 99, 236);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, graphic_r, graphic_g, graphic_b, graphic_a, stack_size, spell_effect_id)
VALUES (547, 1, 'Hair Dye: Blonde', 100, 821122, 20408, 253, 232, 80, 160, 99, 237);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, graphic_tile, graphic_file, stack_size, spell_effect_id)
VALUES (548, 1, 'Teleport: PVP Event', 820118, 20398, 99, 238);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, stat_ac, stat_sta, stat_int, item_slot, item_type, lore, event, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_a)
VALUES (549, 2, 'Team 1 Headband', 200, 19, 4, 3, 0, 10, '1', '1', 332238, 2278, 20, 255, 180);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, stat_ac, stat_sta, stat_int, item_slot, item_type, lore, event, graphic_tile, graphic_file, graphic_equip, graphic_b, graphic_a)
VALUES (550, 2, 'Team 2 Headband', 200, 19, 4, 3, 0, 10, '1', '1', 332238, 2278, 20, 255, 180);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_str, stat_sta, stat_dex, weapon_damage, weapon_delay, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, graphic_a, body_state)
VALUES (551, 3, 'Mald''s Devastator Sword', 125, 125, 10, 5, 5, 80, 7, 2, 14, 820226, 20399, 117, 200, 4);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, res_fire, res_water, res_spirit, res_air, res_earth, min_level, weapon_damage, item_slot, graphic_tile, graphic_file, graphic_equip, graphic_a, body_state, spell_effect_id)
VALUES (552, 2, 'Mald''s Moon Shield', 100, 100, 100, 10, 10, 10, 10, 10, 10, 10, 10, 10, 1, 0, 1, 332277, 2278, 70, 185, 4, 78);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_int, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, graphic_a)
VALUES (553, 2, 'Mald''s Monster Feet', 50, 100, 30, 5, 5, 5, 12, 12, 51336, 2282, 13, 185);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, graphic_r, graphic_g, graphic_b, graphic_a, stack_size, spell_effect_id)
VALUES (554, 1, 'Hair Dye: Rampant Rape', 100, 821122, 20408, 25, 25, 65, 215, 99, 239);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_sta, stat_int, min_experience, min_level, item_slot, item_type, lore, bindonequip, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_a, class_restrictions)
VALUES (555, 2, 'Sunshine Cloak', 1750, 300, 375, 5, 15, 20000000, 50, 10, 12, '1', '1', 51324, 2282, 34, 255, 255, 180, 51);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_a, spell_effect_id)
VALUES (556, 2, 'Egg Yolk Headband', 75, 150, 50, 10, 10, 10, 10, 1, 0, 12, 332238, 2278, 20, 255, 255, 180, 71);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, res_fire, res_water, res_spirit, res_air, res_earth, min_level, weapon_damage, item_slot, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_b, graphic_a, body_state, spell_effect_id)
VALUES (557, 2, '5 In The Pink', 100, 75, 3, 3, 3, 3, 10, 10, 10, 10, 10, 1, 0, 1, 332277, 2278, 70, 255, 125, 180, 4, 71);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, graphic_r, graphic_g, graphic_b, graphic_a, stack_size, spell_effect_id)
VALUES (558, 1, 'Hair Dye: Beowulf Sperm', 100, 821122, 20408, 280, 113, 39, 5180, 99, 240);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_sta, stat_int, min_experience, min_level, item_slot, item_type, lore, bindonequip, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions)
VALUES (559, 2, 'Mald''s Coat', 1750, 300, 375, 5, 15, 20000000, 50, 10, 12, '1', '1', 332201, 2278, 20, 107, 7, 7, 205, 51);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, item_slot, item_type, lore, event, graphic_tile, graphic_file, graphic_equip, credits_value)
VALUES (560, 2, 'Penguin Costume', 400, 200, 50, 3, 3, 3, 10, 0, 10, '1', '1', 820900, 20406, 11, 2);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, item_slot, item_type, lore, event, graphic_tile, graphic_file, graphic_equip, credits_value)
VALUES (561, 2, 'Grim Reaper Costume', 200, 400, 50, 3, 10, 3, 3, 0, 10, '1', '1', 820901, 20406, 143, 2);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, item_slot, item_type, lore, event, graphic_tile, graphic_file, graphic_equip, credits_value)
VALUES (562, 2, 'Ghost Costume', 400, 200, 50, 10, 3, 3, 3, 0, 10, '1', '1', 332226, 2278, 19, 2);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, item_slot, item_type, lore, event, graphic_tile, graphic_file, graphic_equip, credits_value)
VALUES (563, 2, 'Gingerbread Man Costume', 200, 400, 50, 3, 3, 10, 3, 0, 10, '1', '1', 820714, 20404, 17, 2);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, item_slot, item_type, lore, event, graphic_tile, graphic_file, graphic_equip, credits_value)
VALUES (564, 2, 'Devil Costume', 400, 200, 80, 5, 5, 5, 5, 0, 10, '1', '1', 820902, 20406, 26, 2);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, graphic_r, graphic_g, graphic_b, graphic_a, stack_size, spell_effect_id)
VALUES (565, 1, 'Hair Dye: Sorwind''s Dye', 100, 821122, 20408, 300, 300, 300, 550, 99, 241);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_experience, min_level, lore, bindonpickup, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (566, 4, 'Scroll: Ancient Healing 2', 200000000, 50, '1', '1', 820110, 20398, 31, 146);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, min_experience, min_level, weapon_damage, item_slot, item_type, lore, bindonequip, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions, body_state, spell_effect_id)
VALUES (567, 3, 'Maser''s Vengeance', 1500, 1500, 200000000, 50, 200, 2, 14, '1', '1', 331352, 2269, 64, 1, 1, 1, 1134, 59, 4, 216);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_sta, stat_int, min_experience, min_level, item_slot, item_type, lore, bindonequip, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions)
VALUES (568, 2, 'Maser''s Revolution', 1750, 300, 375, 5, 15, 20000000, 50, 10, 12, '1', '1', 332201, 2278, 20, 14000, 14000, 14000, 1200, 59);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, min_experience, min_level, item_slot, item_type, bindonpickup, graphic_tile, graphic_file, graphic_equip, graphic_b, graphic_a, class_restrictions, spell_effect_id)
VALUES (569, 2, 'Azul''s CP', 2500, 2500, 450, 200000000, 50, 10, 10, '1', 332236, 2278, 12, 89, 185, 59, 213);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, min_experience, min_level, weapon_damage, item_slot, item_type, lore, bindonequip, graphic_tile, graphic_file, graphic_equip, graphic_b, graphic_a, class_restrictions, body_state, spell_effect_id)
VALUES (570, 3, 'Azul''s Sword', 1500, 1500, 200000000, 50, 200, 2, 14, '1', '1', 331352, 2269, 64, 89, 185, 59, 4, 216);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, graphic_b, graphic_a, spell_effect_id)
VALUES (571, 2, 'Azul''s A Princess', 75, 150, 50, 10, 10, 10, 10, 1, 0, 12, 332294, 2278, 53, 89, 185, 71);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, min_experience, min_level, weapon_damage, item_slot, lore, bindonequip, graphic_tile, graphic_file, graphic_equip, graphic_b, graphic_a, class_restrictions, body_state, spell_effect_id)
VALUES (572, 2, 'Azul''s Shield', 1000, 1000, 200, 200000000, 50, 0, 1, '1', '1', 332277, 2278, 70, 89, 185, 59, 4, 215);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, min_experience, min_level, item_slot, item_type, bindonpickup, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions, spell_effect_id)
VALUES (573, 2, 'DPS CP', 2500, 2500, 450, 200000000, 50, 10, 10, '1', 332231, 2278, 11, 255, 255, 255, 230, 59, 213);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, min_experience, min_level, weapon_damage, item_slot, item_type, lore, bindonequip, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions, body_state, spell_effect_id)
VALUES (574, 3, 'DPS Staff', 1500, 1500, 200000000, 50, 200, 2, 18, '1', '1', 331368, 2269, 79, 255, 255, 255, 230, 59, 4, 216);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, min_experience, min_level, weapon_damage, item_slot, lore, bindonequip, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions, body_state, spell_effect_id)
VALUES (575, 2, 'DPS White Shield', 1000, 1000, 200, 200000000, 50, 0, 1, '1', '1', 332277, 2278, 70, 255, 255, 255, 230, 59, 4, 215);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, spell_effect_id)
VALUES (576, 2, 'DPS Doom', 75, 150, 50, 10, 10, 10, 10, 1, 0, 12, 332294, 2278, 53, 255, 255, 255, 230, 71);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_experience, min_level, lore, bindonpickup, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (577, 4, 'Scroll: Death Touch', 2000000000, 50, '1', '1', 820110, 20398, 31, 147);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_dex, min_level, item_slot, item_type, lore, bindonpickup, bindonequip, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions, spell_effect_id)
VALUES (578, 2, 'DPS Boots', 1000, 1000, 100, 50, 50, 12, 10, '1', '1', '1', 332288, 2278, 9, 255, 255, 255, 230, 59, 76);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, min_experience, min_level, item_slot, item_type, bindonpickup, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions, spell_effect_id)
VALUES (579, 2, 'DPS Legplates', 2000, 2000, 225, 200000000, 50, 11, 10, '1', 51332, 2282, 14, 255, 255, 255, 230, 59, 211);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, res_fire, min_experience, min_level, weapon_damage, item_slot, lore, bindonequip, graphic_tile, graphic_file, spell_effect_id)
VALUES (580, 2, 'Enchanted Bracelet of Fire', 400, 200, 40, 20, 20, 20, 40, 70, 400000000, 50, 0, 4, '1', '1', 820070, 20397, 177);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, res_earth, min_experience, min_level, weapon_damage, item_slot, lore, bindonequip, graphic_tile, graphic_file, spell_effect_id)
VALUES (581, 2, 'Enchanted Bracelet of Earth', 200, 400, 10, 20, 20, 20, 40, 70, 400000000, 50, 0, 4, '1', '1', 820072, 20397, 70);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, res_air, min_experience, min_level, weapon_damage, item_slot, lore, bindonequip, graphic_tile, graphic_file, spell_effect_id)
VALUES (582, 2, 'Enchanted Bracelet of Air', 250, 350, 20, 20, 35, 20, 25, 70, 400000000, 50, 0, 4, '1', '1', 820074, 20397, 84);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, res_water, min_experience, min_level, weapon_damage, item_slot, lore, bindonequip, graphic_tile, graphic_file, spell_effect_id)
VALUES (583, 2, 'Enchanted Bracelet of Water', 300, 300, 20, 20, 30, 20, 30, 70, 400000000, 50, 0, 4, '1', '1', 820076, 20397, 244);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, res_spirit, min_experience, min_level, weapon_damage, item_slot, lore, bindonequip, graphic_tile, graphic_file, spell_effect_id)
VALUES (584, 2, 'Enchanted Bracelet of Spirit', 300, 300, 30, 30, 30, 30, 30, 70, 400000000, 50, 0, 4, '1', '1', 820079, 20397, 245);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, bindonpickup, graphic_tile, graphic_file)
VALUES (585, 0, 'Howto: Bracelet Enchantment', '1', 821102, 20408);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_level, weapon_damage, item_slot, lore, bindonpickup, graphic_tile, graphic_file, spell_effect_id)
VALUES (586, 2, 'Prison Gloves', 500, 500, 70, 10, 10, 10, 10, 1, 0, 9, '1', '1', 820210, 20399, 71);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_sta, stat_int, min_level, item_slot, item_type, lore, bindonpickup, bindonequip, graphic_tile, graphic_file, graphic_equip, graphic_a, class_restrictions, spell_effect_id)
VALUES (587, 2, 'Enchanted Divine Helm', 3000, 3000, 150, 25, 25, 50, 0, 10, '1', '1', '1', 332238, 2278, 20, 100, 59, 211);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, stat_ac, stat_sta, min_level, item_slot, item_type, lore, bindonpickup, bindonequip, graphic_tile, graphic_file, graphic_equip, graphic_a, class_restrictions, spell_effect_id)
VALUES (588, 2, 'Enchanted Divine Helm', 6000, 150, 50, 50, 0, 10, '1', '1', '1', 332238, 2278, 20, 100, 55, 176);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_mp, stat_ac, stat_int, min_level, item_slot, item_type, lore, bindonpickup, bindonequip, graphic_tile, graphic_file, graphic_equip, graphic_a, class_restrictions, spell_effect_id)
VALUES (589, 2, 'Enchanted Divine Crown', 6000, 150, 50, 50, 0, 10, '1', '1', '1', 332240, 2278, 22, 100, 47, 244);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_sta, stat_int, min_level, item_slot, item_type, lore, bindonpickup, bindonequip, graphic_tile, graphic_file, graphic_equip, graphic_a, class_restrictions, spell_effect_id)
VALUES (590, 2, 'Enchanted Divine Crown', 2000, 4000, 150, 20, 30, 50, 0, 10, '1', '1', '1', 332240, 2278, 22, 100, 31, 244);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, bindonpickup, graphic_tile, graphic_file)
VALUES (591, 0, 'Howto: Helm Enchantment', '1', 821102, 20408);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_experience, min_level, lore, bindonpickup, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (592, 4, 'Scroll: Group Ancient Dungeons Teleport', 400000000, 50, '1', '1', 820110, 20398, 47, 148);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_experience, min_level, lore, bindonpickup, graphic_tile, graphic_file, learn_spell_id)
VALUES (593, 4, 'Scroll: Paradise Teleport', 400000000, 50, '1', '1', 820110, 20398, 149);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_experience, min_level, lore, bindonpickup, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (594, 4, 'Scroll: Ancient Covenant', 400000000, 50, '1', '1', 820110, 20398, 47, 150);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_experience, min_level, lore, bindonpickup, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (595, 4, 'Scroll: Ancient Sacrifice 2', 400000000, 50, '1', '1', 820110, 20398, 31, 151);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, min_experience, min_level, item_slot, item_type, bindonpickup, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_a, class_restrictions, spell_effect_id)
VALUES (596, 2, 'Slippey''s Robe', 3000, 2000, 450, 200000000, 50, 10, 12, '1', 332236, 2278, 12, 200, 180, 31, 215);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_experience, min_level, lore, bindonpickup, graphic_tile, graphic_file, class_restrictions, learn_spell_id)
VALUES (597, 4, 'Scroll: Ancient Taunt 2', 400000000, 50, '1', '1', 820110, 20398, 55, 152);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, spell_effect_id)
VALUES (600, 2, 'Powerful Beefs Immortality', 75, 150, 50, 10, 10, 10, 10, 1, 0, 12, 332238, 2278, 20, 245, 121, 2, 180, 71);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_sta, stat_dex, stat_int, min_experience, min_level, weapon_damage, item_slot, lore, bindonequip, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions, body_state, spell_effect_id)
VALUES (601, 2, 'UMADBra', 300, 300, 150, 10, 25, 10, 20000000, 50, 0, 1, '1', '1', 332277, 2278, 70, 180, 60, 25, 130, 59, 4, 77);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, spell_effect_id)
VALUES (602, 2, 'Deji''s Black Wolf', 75, 150, 50, 10, 10, 10, 10, 1, 0, 12, 332246, 2278, 32, 1, 1, 1, 210, 71);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, spell_effect_id)
VALUES (603, 2, 'Yuna''s White Wolf', 75, 150, 50, 10, 10, 10, 10, 1, 0, 12, 332246, 2278, 32, 255, 255, 255, 205, 71);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, graphic_r, graphic_g, graphic_b, graphic_a, stack_size, spell_effect_id)
VALUES (604, 1, 'Hair Dye: Wesley Snipers', 100, 821122, 20408, 1, 1, 1, 255, 99, 250);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_sta, min_experience, min_level, item_slot, item_type, lore, bindonequip, graphic_tile, graphic_file, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions)
VALUES (605, 2, 'Nakeds', 650, 1000, 200, 20, 20000000, 50, 10, 12, '1', '1', 820254, 20399, 1, 1, 1, 250, 15);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_level, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, spell_effect_id)
VALUES (606, 2, 'Ranga Suit', 75, 150, 50, 10, 10, 10, 10, 1, 0, 12, 300000, 820714, 20404, 17, 71);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, min_level, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a)
VALUES (607, 2, 'Stichs Pants', 175, 150, 120, 15, 15, 15, 15, 50, 11, 10, 332204, 2278, 1, 224, 27, 36, 180);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_dex, min_level, item_slot, item_type, lore, bindonpickup, bindonequip, graphic_tile, graphic_file, class_restrictions, spell_effect_id)
VALUES (608, 2, 'Stichs Boots', 1000, 1000, 100, 50, 50, 12, 10, '1', '1', '1', 820041, 20397, 59, 76);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, res_fire, res_water, res_spirit, res_air, res_earth, min_level, item_slot, item_type, lore, bindonpickup, graphic_tile, graphic_file, spell_effect_id)
VALUES (609, 2, 'Stichs Robe', 3000, 3000, 200, 20, 20, 20, 20, 20, 20, 20, 20, 20, 50, 10, 12, '1', '1', 820285, 20399, 161);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, min_level, weapon_damage, item_slot, item_type, lore, bindonpickup, bindonequip, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, body_state, spell_effect_id)
VALUES (610, 3, 'Deji''s Boner', 125, 125, 50, 50, 2, 14, '1', '1', '1', 332513, 2281, 37, 10, 10, 255, 70, 4, 76);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_sta, stat_int, min_experience, min_level, item_slot, item_type, lore, bindonequip, graphic_tile, graphic_file, graphic_equip, graphic_a, class_restrictions)
VALUES (611, 2, 'Brokonk Armor', 1750, 300, 375, 5, 15, 20000000, 50, 10, 12, '1', '1', 332236, 2278, 12, 180, 51);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, min_level, weapon_damage, item_slot, item_type, lore, bindonpickup, bindonequip, graphic_tile, graphic_file, graphic_equip, graphic_a, body_state, spell_effect_id)
VALUES (612, 3, 'Brokonk Axe', 125, 125, 50, 50, 2, 14, '1', '1', '1', 820282, 20399, 316, 180, 4, 76);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, res_fire, res_water, res_spirit, res_air, res_earth, min_level, weapon_damage, item_slot, item_value, graphic_tile, graphic_file, graphic_equip, graphic_a, body_state, spell_effect_id)
VALUES (613, 2, 'Brokonk Shield', 100, 75, 3, 3, 3, 3, 10, 10, 10, 10, 10, 1, 0, 1, 500000, 332277, 2278, 70, 200, 4, 71);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_sta, stat_int, min_experience, min_level, item_slot, item_type, lore, bindonequip, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions)
VALUES (614, 2, 'Lava Robes', 1750, 300, 375, 5, 15, 20000000, 50, 10, 12, '1', '1', 332201, 2278, 20, 255, 10, 10, 120, 51);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_sta, min_experience, min_level, item_slot, item_type, lore, bindonequip, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions)
VALUES (615, 2, 'Cloudy Robes', 650, 1000, 200, 20, 20000000, 50, 10, 12, '1', '1', 332201, 2278, 20, 10, 10, 255, 70, 15);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, stack_size, spell_effect_id)
VALUES (616, 1, 'Pet Bait', 250000, 820607, 20403, 10, 230);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, item_slot, graphic_tile, graphic_file, spell_effect_id)
VALUES (617, 2, 'Fuzex Necklace', 3000, 3000, 200, 5, 820084, 20397, 72);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, min_level, weapon_damage, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, graphic_a, class_restrictions, body_state, spell_effect_id)
VALUES (618, 3, 'Theknights Staff of Enchantment', 2000, 7000, 50, 150, 2, 16, 331349, 2269, 61, 200, 47, 4, 72);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, min_level, weapon_damage, item_slot, graphic_tile, graphic_file, graphic_equip, graphic_a, body_state, spell_effect_id)
VALUES (619, 2, 'Theknights Shield', 3000, 3000, 200, 1, 0, 1, 332277, 2278, 70, 200, 4, 72);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, weapon_damage, item_slot, graphic_tile, graphic_file, spell_effect_id)
VALUES (620, 2, 'Theknights Ring of Awesomeness', 3000, 3000, 200, 0, 4, 820079, 20397, 72);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, stack_size)
VALUES (621, 0, 'Empty Bottle', 500, 820183, 20398, 99);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, stack_size)
VALUES (622, 0, 'Magical Liquid', 200, 820123, 20398, 99);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, graphic_r, graphic_g, graphic_b, graphic_a, stack_size)
VALUES (623, 0, 'Red Droplet', 200, 820300, 20400, 255, 65, 7, 200, 99);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, graphic_r, graphic_g, graphic_b, graphic_a, stack_size)
VALUES (624, 0, 'Blue Droplet', 200, 820300, 20400, 7, 89, 255, 200, 99);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, graphic_r, graphic_g, graphic_b, graphic_a, stack_size)
VALUES (625, 0, 'Purple Droplet', 200, 820300, 20400, 130, 7, 255, 200, 99);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, graphic_r, graphic_g, graphic_b, graphic_a, stack_size)
VALUES (626, 0, 'Green Droplet', 200, 820300, 20400, 71, 255, 7, 200, 99);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, graphic_r, graphic_g, graphic_b, graphic_a, stack_size)
VALUES (627, 0, 'Orange Droplet', 200, 820300, 20400, 255, 100, 15, 200, 99);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, stack_size, spell_effect_id)
VALUES (628, 1, 'HP Regen Potion', 1500, 820181, 20398, 99, 251);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, stack_size, spell_effect_id)
VALUES (629, 1, 'MP Regen Potion', 1500, 820178, 20398, 99, 252);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, stack_size, spell_effect_id)
VALUES (630, 1, 'Haste Potion', 1500, 820180, 20398, 99, 253);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, stack_size, spell_effect_id)
VALUES (631, 1, 'Spell Damage Potion', 2000, 820179, 20398, 99, 254);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, stack_size, spell_effect_id)
VALUES (632, 1, 'Spell Critical Potion', 2000, 820177, 20398, 99, 255);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, res_fire, res_water, res_spirit, res_air, res_earth, item_slot, item_type, lore, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, spell_effect_id)
VALUES (633, 2, 'Blind Faith', 1500, 1500, 200, 20, 20, 20, 20, 20, 20, 20, 20, 20, 0, 12, '1', 332294, 2278, 53, 20, 70, 130, 150, 72);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, res_fire, res_water, res_spirit, res_air, res_earth, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, spell_effect_id)
VALUES (634, 2, 'Cowardice', 1500, 1500, 200, 20, 20, 20, 20, 20, 20, 20, 20, 20, 10, 12, 332243, 2278, 14, 20, 70, 130, 150, 72);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, res_fire, res_water, res_spirit, res_air, res_earth, weapon_damage, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, body_state, spell_effect_id)
VALUES (635, 3, 'Gonryomaru', 1500, 1500, 200, 20, 20, 20, 20, 20, 20, 20, 20, 20, 50, 2, 14, 331352, 2269, 64, 20, 70, 130, 180, 4, 72);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, res_fire, res_water, res_spirit, res_air, res_earth, item_slot, item_type, graphic_tile, graphic_file, graphic_equip, graphic_a, spell_effect_id)
VALUES (636, 2, 'Sloth', 1500, 1500, 200, 20, 20, 20, 20, 20, 20, 20, 20, 20, 11, 10, 51332, 2282, 14, 160, 140);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, res_fire, res_water, res_spirit, res_air, res_earth, item_slot, item_type, lore, graphic_tile, graphic_file, graphic_equip, graphic_a, spell_effect_id)
VALUES (637, 2, 'Wrath', 1500, 1500, 200, 20, 20, 20, 20, 20, 20, 20, 20, 20, 12, 10, '1', 332288, 2278, 9, 160, 140);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, res_fire, res_water, res_spirit, res_air, res_earth, item_slot, graphic_tile, graphic_file, spell_effect_id)
VALUES (638, 2, 'Lust', 1500, 1500, 200, 20, 20, 20, 20, 20, 20, 20, 20, 20, 6, 820232, 20399, 140);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, res_fire, res_water, res_spirit, res_air, res_earth, weapon_damage, item_slot, graphic_tile, graphic_file, spell_effect_id)
VALUES (639, 2, 'Gluttony', 1500, 1500, 200, 20, 20, 20, 20, 20, 20, 20, 20, 20, 0, 7, 810034, 20107, 258);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, res_fire, res_water, res_spirit, res_air, res_earth, weapon_damage, item_slot, graphic_tile, graphic_file, spell_effect_id)
VALUES (640, 2, 'Greed', 1500, 1500, 200, 20, 20, 20, 20, 20, 20, 20, 20, 20, 0, 4, 820054, 20397, 257);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, res_fire, res_water, res_spirit, res_air, res_earth, weapon_damage, item_slot, graphic_tile, graphic_file, spell_effect_id)
VALUES (641, 2, 'Pride', 1500, 1500, 200, 20, 20, 20, 20, 20, 20, 20, 20, 20, 0, 4, 820059, 20397, 256);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, res_fire, res_water, res_spirit, res_air, res_earth, weapon_damage, item_slot, graphic_tile, graphic_file, spell_effect_id)
VALUES (642, 2, 'Envy', 1500, 1500, 200, 20, 20, 20, 20, 20, 20, 20, 20, 20, 0, 9, 820209, 20399, 140);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_description, graphic_tile, graphic_file, graphic_r, graphic_g, graphic_b, graphic_a)
VALUES (643, 0, 'Custom Ticket', 'Put ticket in first slot of combine bag and type /custom', 821123, 20408, 224, 40, 40, 150);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, stat_ac, min_level, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a)
VALUES (644, 2, 'Dusty Shoes', 2, 1, 12, 12, 100, 332213, 2278, 2, 1, 1, 1, 100);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, weapon_damage, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions, body_state)
VALUES (645, 3, 'Rusty Hammer', 5, 7, 2, 16, 200, 820039, 20397, 5, 1, 1, 1, 100, 22, 4);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, weapon_damage, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions)
VALUES (646, 3, 'Old Stave', 5, 7, 3, 17, 200, 820021, 20397, 13, 1, 1, 1, 100, 38);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, weapon_damage, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions, body_state)
VALUES (647, 3, 'Rusty Dagger', 5, 7, 2, 18, 200, 331379, 2269, 11, 1, 1, 1, 100, 34, 4);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, min_level, weapon_damage, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, class_restrictions, body_state)
VALUES (648, 3, 'Rusty Sword', 5, 7, 2, 14, 200, 820015, 20397, 10, 1, 1, 1, 100, 50, 4);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_value, graphic_tile, graphic_file, stack_size)
VALUES (649, 0, 'Wool', 30, 820601, 20403, 99);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, player_hp, player_mp, stat_ac, stat_str, min_level, item_slot, item_type, item_value, graphic_tile, graphic_file, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a)
VALUES (650, 2, 'Rabbit Fur Pants', 10, 10, 10, 2, 1, 11, 12, 300, 332204, 2278, 1, 255, 255, 255, 150);
INSERT INTO item_templates (item_template_id, item_usetype, item_name, item_slot, item_type, item_value, lore, graphic_tile, graphic_file, graphic_equip, spell_effect_id)
VALUES (651, 2, 'Tank', 13, 0, 150000, '1', 332524, 2281, 273, 259);


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
  face_id SMALLINT DEFAULT 0 NOT NULL,
  hair_id SMALLINT DEFAULT 0 NOT NULL,
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

INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, experience, attack_range, attack_speed, class_id, body_id, weapon_damage)
VALUES (1, 2, 'Mouse', 40, 20, 1, 1.5, 4, 10113, 4);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, attack_range, attack_speed, class_id, body_id, weapon_damage)
VALUES (2, 2, 'Lamb', 40, 2, 40, 1, 1.5, 4, 10114, 6);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, class_id, body_id, weapon_damage, npc_alliance)
VALUES (3, 2, 'Fluffy Little Bunny', 30, 4, 104, 1, 1, 1.4, 1.5, '1', '1', 4, 10140, 9, '3');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, class_id, body_id, weapon_damage, npc_alliance)
VALUES (4, 2, 'Rabid Rabbit', 30, 8, 186, 2, 1, 1.4, 1.3, '1', '1', 4, 10141, 18, '3 4');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, class_id, body_id, weapon_damage, npc_alliance)
VALUES (5, 2, 'Cottontail', 1800, 9, 250, 1, 1, 1.5, 1.5, '1', '1', 2, 121, 28, '3 4');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, slowable, class_id, body_id, weapon_damage, npc_alliance)
VALUES (6, 2, 'Asp', 30, 6, 124, 1, 1, 1.4, 1.3, '1', 2, 123, 13, '6');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, slowable, class_id, body_id, weapon_damage, npc_alliance)
VALUES (7, 2, 'Bat', 30, 12, 266, 3, 1, 1.4, 1.5, '1', 2, 10100, 38, '7');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, attack_range, attack_speed, move_speed, slowable, class_id, body_id, weapon_damage)
VALUES (8, 2, 'Cow', 30, 21, 448, 1, 1.5, 1.5, '1', 2, 116, 90);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, attack_range, attack_speed, move_speed, slowable, class_id, body_id, weapon_damage)
VALUES (9, 2, 'Ram', 30, 18, 368, 1, 1.5, 1.5, '1', 2, 10104, 72);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, slowable, class_id, body_id, weapon_damage, npc_alliance)
VALUES (10, 2, 'Weak Zombie', 30, 20, 404, 2, 1, 1.3, 1.3, '1', 4, 169, 80, '10');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, stationary, rootable, slowable, class_id, body_id, weapon_damage, npc_alliance)
VALUES (11, 2, 'Weak Skeleton', 60, 30, 609, 3, 1, 1.5, '1', '1', '1', 3, 10106, 150, '11 12 13 14 15 16 17');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, rootable, slowable, class_id, body_id, weapon_damage, npc_alliance)
VALUES (12, 2, 'Weak Skeleton', 60, 30, 609, 3, 1, 1.5, '1', '1', 3, 10106, 150, '11 12 13 14 15 16 17');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, stationary, stunnable, slowable, class_id, body_id, weapon_damage, npc_alliance)
VALUES (13, 2, 'Skeleton', 60, 35, 709, 4, 1, 1.5, '1', '1', '1', 3, 10106, 180, '11 12 13 15 16 17');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, class_id, body_id, weapon_damage, npc_alliance)
VALUES (14, 2, 'Servant of the Dead', 60, 40, 809, 4, 1, 1.5, 1.3, '1', '1', 3, 10125, 210, '11 12 13 14 15 16 17');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, class_id, body_id, weapon_damage, npc_alliance)
VALUES (15, 2, 'Boney', 3600, 42, 1000, 4, 1, 1.5, 1.3, '1', '1', 3, 10125, 240, '11 12 13 14 15 16 17');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, class_id, body_id, weapon_damage, npc_alliance)
VALUES (16, 2, 'Misplaced Spouse', 3600, 43, 1100, 4, 1, 1.5, 1.3, '1', '1', 3, 10125, 240, '11 12 13 14 15 16 17');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, class_id, body_id, weapon_damage, npc_alliance)
VALUES (17, 2, 'Record Keeper', 3600, 45, 1200, 5, 1, 1, 1.3, '1', '1', 3, 10125, 270, '11 12 13 14 15 16 17');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stunnable, rootable, slowable, class_id, body_id, weapon_damage, npc_alliance)
VALUES (18, 2, 'Piglet', 30, 21, 400, 1, 1, 1.4, 1.4, '1', '1', '1', 4, 109, 57, '18 19');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, rootable, slowable, class_id, body_id, weapon_damage, npc_alliance)
VALUES (19, 2, 'Flying Piglet', 3600, 23, 600, 3, 1, 1.2, 1.2, '1', '1', 3, 134, 72, '18 19');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, class_id, body_id, weapon_damage, npc_alliance)
VALUES (20, 2, 'Lost Wabbit', 60, 12, 246, 2, 1, 1.3, 1.5, '1', '1', 4, 10139, 51, '20 21 22 23');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, class_id, body_id, weapon_damage, npc_alliance)
VALUES (21, 2, 'Cute Defenseless Stalker', 60, 15, 306, 2, 1, 1.3, 1.5, '1', '1', 2, 10138, 58, '20 21 22 23');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, class_id, body_id, weapon_damage, npc_alliance)
VALUES (22, 2, 'Roger', 3600, 14, 356, 3, 1, 1.3, 1.3, '1', '1', 3, 121, 60, '20 21 22 23');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, class_id, body_id, weapon_damage, npc_alliance)
VALUES (23, 2, 'Leafeater', 3600, 17, 406, 3, 1, 1.3, 1.3, '1', '1', 3, 121, 69, '20 21 22 23');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stunnable, rootable, slowable, class_id, body_id, weapon_damage, npc_alliance)
VALUES (24, 2, 'Pipsqueek', 30, 12, 246, 1, 1, 1.4, 1.4, '1', '1', '1', 4, 109, 57, '24 25');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, rootable, slowable, class_id, body_id, weapon_damage, npc_alliance)
VALUES (25, 2, 'Flying Pipsqueek', 3600, 14, 400, 3, 1, 1.2, 1.2, '1', '1', 3, 134, 60, '24 25');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, slowable, class_id, body_id, weapon_damage, npc_alliance)
VALUES (26, 2, 'Fire Asp', 30, 25, 508, 1, 1, 1.3, 1.3, '1', 4, 10122, 96, '26 27 28');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, rootable, slowable, class_id, body_state, body_id, face_id, equipped_items, weapon_damage, npc_alliance)
VALUES (27, 2, 'Naga Warrior', 30, 28, 568, 2, 1, 1.5, 1.3, '1', '1', 3, 4, 1, 1, '15,192,28,40,180,12,148,231,148,160,0,*,0,*,0,*,10,*', 112, '26 27 28');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, rootable, slowable, class_id, body_state, body_id, face_id, equipped_items, weapon_damage, npc_alliance)
VALUES (28, 2, 'Naga Rogue', 30, 30, 608, 2, 1, 1.1, 1.3, '1', '1', 2, 4, 1, 1, '15,192,28,40,180,12,148,231,148,160,0,*,0,*,0,*,11,*', 105, '26 27 28');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, class_id, body_id, weapon_damage, npc_alliance)
VALUES (29, 2, 'Frozen Waste', 60, 35, 709, 2, 1, 1.5, 1.2, '1', '1', 3, 114, 126, '29 30 31 32 33');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, slowable, class_id, body_id, weapon_damage, npc_alliance)
VALUES (30, 2, 'Frozen Waste', 60, 35, 709, 2, 1, 1.5, 1.2, '1', 3, 114, 126, '29 30 31 32 33');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, class_id, body_id, weapon_damage, npc_alliance)
VALUES (31, 2, 'Iceman', 60, 30, 609, 2, 1, 1.5, 1.4, '1', '1', 3, 114, 111, '29 30 31 32 33');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, slowable, class_id, body_id, weapon_damage, npc_alliance)
VALUES (32, 2, 'Iceman', 60, 30, 609, 2, 1, 1.5, 1.3, '1', 3, 114, 111, '29 30 31 32 33');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, npc_hp, class_id, body_id, weapon_damage, npc_alliance)
VALUES (33, 2, 'Frosty', 14400, 45, 2109, 6, 2, 1, 1, '1', '1', 12000, 3, 153, 808, '29 30 31 32 33');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stunnable, slowable, class_id, body_id, weapon_damage, npc_alliance)
VALUES (34, 2, 'Weak Persecution', 30, 35, 708, 1, 1, 1.4, 1.4, '1', '1', 2, 128, 135, '34 35');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stunnable, slowable, class_id, body_id, weapon_damage, npc_alliance)
VALUES (35, 2, 'Moldy Persecution', 30, 40, 830, 3, 1, 1.4, 1.4, '1', '1', 2, 128, 147, '34 35');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, stunnable, slowable, class_id, body_id, weapon_damage, npc_alliance)
VALUES (36, 2, 'Private Persecution', 30, 40, 809, 2, 1, 1.4, 1.4, '1', '1', '1', 4, 128, 141, '36 37');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, class_id, body_id, weapon_damage, npc_alliance)
VALUES (37, 2, 'Sergeant Persecution', 3600, 45, 1309, 2, 3, 1.4, 1.4, '1', '1', 3, 128, 665, '36');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, npc_hp, class_id, body_id, weapon_damage, npc_alliance)
VALUES (38, 2, 'Strong Persecution', 30, 50, 1310, 3, 3, 1.3, 1.3, '1', '1', 13362, 3, 128, 330, '34 35 36 37 38');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, slowable, class_id, body_id, weapon_damage, npc_alliance)
VALUES (39, 2, 'Persecution', 30, 50, 909, 2, 1, 1.4, 1.3, '1', 3, 128, 165, '39 49');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stunnable, rootable, slowable, class_id, body_id, weapon_damage, npc_alliance)
VALUES (40, 2, 'Green Slime', 30, 12, 246, 3, 1, 1.3, 1.3, '1', '1', '1', 3, 10110, 48, '40 41 42 43 44 45');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stunnable, rootable, slowable, class_id, body_id, weapon_damage, npc_alliance)
VALUES (41, 2, 'Slime', 30, 13, 266, 3, 1, 1.3, 1.3, '1', '1', '1', 3, 10109, 54, '40 41 42 43 44 45');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stunnable, rootable, slowable, class_id, body_id, weapon_damage, npc_alliance)
VALUES (42, 2, 'Pink Slime', 30, 14, 286, 3, 1, 1.3, 1.3, '1', '1', '1', 3, 10111, 57, '40 41 42 43 44 45');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stunnable, rootable, slowable, class_id, body_id, weapon_damage, npc_alliance)
VALUES (43, 2, 'Gold Slime', 30, 15, 308, 3, 1, 1.3, 1.3, '1', '1', '1', 3, 10108, 63, '40 41 42 43 44 45');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stunnable, rootable, slowable, class_id, body_id, weapon_damage, npc_alliance)
VALUES (44, 2, 'Blue Slime', 30, 20, 408, 3, 1, 1.3, 1.3, '1', '1', '1', 3, 10120, 81, '40 41 42 43 44 45');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stunnable, rootable, slowable, class_id, body_id, weapon_damage, npc_alliance)
VALUES (45, 2, 'Red Slime', 30, 16, 338, 3, 1, 1.3, 1.3, '1', '1', '1', 3, 10107, 66, '40 41 42 43 44 45');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, stunnable, slowable, class_id, body_id, weapon_damage, npc_alliance)
VALUES (46, 2, 'King Goo', 3600, 20, 448, 2, 1, 1.3, 1.3, '1', '1', '1', 3, 10120, 87, '40 41 42 43 44 45');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, class_id, body_id, weapon_damage, npc_alliance)
VALUES (47, 2, 'Sliminator', 3600, 22, 448, 2, 1, 1.3, 1.3, '1', '1', 3, 10120, 90, '40 41 42 43 44 45');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, class_id, body_id, weapon_damage, npc_alliance)
VALUES (48, 2, 'Goo Jr.', 7200, 28, 600, 2, 1, 1.3, 1.3, '1', '1', 3, 117, 114, '40 41 42 43 44 45');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, slowable, npc_hp, class_id, body_id, weapon_damage, npc_alliance)
VALUES (49, 2, 'Strong Persecution', 30, 50, 1310, 3, 3, 1.3, 1.3, '1', 13362, 3, 128, 330, '39 49');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, rootable, slowable, npc_hp, class_id, body_id, face_id, equipped_items, weapon_damage, npc_alliance)
VALUES (50, 2, 'Nagan Beast', 60, 50, 1049, 2, 1, 1, 1, '1', '1', '1', 9934, 3, 1, 1, '0,*,12,*,0,*,0,*,0,*,0,*', 1100, '50 51 52 53');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, stunnable, rootable, slowable, npc_hp, class_id, body_state, body_id, face_id, equipped_items, weapon_damage, npc_alliance)
VALUES (51, 2, 'Nagan Magus', 60, 50, 949, 3, 4, 1, 1, '1', '1', '1', '1', 4948, 4, 5, 1, 1, '20,*,12,*,0,*,0,*,0,*,13,*', 949, '50 51 52 53');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, stunnable, rootable, slowable, npc_hp, class_id, body_state, body_id, face_id, equipped_items, weapon_damage, npc_alliance)
VALUES (52, 2, 'Nagan Priest', 60, 50, 949, 3, 1, 1, 1, '1', '1', '1', '1', 7142, 5, 5, 1, 1, '118,*,22,*,0,*,0,*,0,*,44,*', 800, '50 51 52 53');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, rootable, slowable, npc_hp, class_id, body_state, body_id, face_id, equipped_items, weapon_damage, npc_alliance)
VALUES (53, 2, 'Udyana', 7200, 50, 6000, 4, 3, 1, 1, '1', '1', 262366, 3, 4, 1, 1, '11,74,121,41,160,12,*,0,*,0,*,0,*,246,*', 5000, '50 51 52 53');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, npc_hp, class_id, body_id, weapon_damage, npc_alliance)
VALUES (54, 2, 'Complex', 7200, 50, 1210, 4, 2, 1.2, 1.2, '1', '1', 73130, 3, 128, 1500, '54 55');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, npc_hp, class_id, body_id, weapon_damage, npc_alliance)
VALUES (55, 2, 'Simple', 45, 50, 1009, 4, 1, 1.2, 1.2, '1', '1', 15206, 3, 128, 1000, '54 55');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, rootable, slowable, npc_hp, class_id, body_id, weapon_damage, npc_alliance)
VALUES (56, 2, 'Spook', 30, 50, 949, 3, 1, 1.1, 0.8, '1', '1', '1', 7170, 3, 10101, 400, '56 57 58 59 60');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, npc_hp, class_id, body_id, weapon_damage, npc_alliance)
VALUES (57, 2, 'Ghast', 30, 50, 969, 3, 1, 1.1, 0.8, '1', '1', 11508, 3, 10102, 500, '56 57 58 59 60');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, npc_hp, class_id, body_id, weapon_damage, npc_alliance)
VALUES (58, 2, 'ImaBitStale', 7200, 50, 1210, 4, 1, 1.1, 0.8, '1', '1', 60206, 3, 10103, 1600, '56 57 58 59 60');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, npc_hp, class_id, body_id, weapon_damage, npc_alliance)
VALUES (59, 2, 'Ecto', 7200, 50, 1820, 3, 1, 1.1, 0.8, '1', '1', 38206, 3, 10103, 750, '56 57 58 59 60');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, npc_hp, class_id, body_id, weapon_damage, npc_alliance, stuck_behaviour)
VALUES (60, 2, 'Punchy', 9600, 50, 2510, 4, 2, 1.1, 1.1, '1', '1', 140774, 3, 125, 5000, '56 57 58 59 60', 2);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, stunnable, slowable, npc_hp, class_id, body_state, face_id, equipped_items, weapon_damage, npc_alliance)
VALUES (61, 2, 'Rabid Savage', 30, 50, 1910, 3, 1, 1, 1, '1', '1', '1', 17130, 3, 4, 3, '0,*,0,*,0,*,0,*,0,*,31,*', 1400, '61 62 63 64 65 66 67 68');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, stunnable, slowable, npc_hp, class_id, body_state, body_id, face_id, equipped_items, weapon_damage, npc_alliance)
VALUES (62, 2, 'Hungry Savage', 30, 50, 2410, 3, 1, 1, 1, '1', '1', '1', 46774, 3, 4, 11, 1, '0,*,0,*,0,*,0,*,0,*,31,*', 2300, '61 62 63 64 65 66 67 68');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, stunnable, slowable, npc_hp, class_id, body_state, face_id, equipped_items, weapon_damage, npc_alliance)
VALUES (63, 2, 'Savage', 30, 50, 1010, 3, 1, 1, 1, '1', '1', '1', 3206, 3, 4, 2, '0,*,0,*,0,*,0,*,0,*,31,*', 1200, '61 62 63 64 65 66 67 68');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, stunnable, rootable, slowable, npc_hp, class_id, body_state, face_id, equipped_items, weapon_damage, npc_alliance)
VALUES (64, 2, 'Paranoid Savage', 30, 50, 3510, 2, 1, 1, 1, '1', '1', '1', '1', 79366, 3, 4, 3, '0,*,0,*,0,*,0,*,0,*,98,*', 3500, '61 62 63 64 65 66 67 68');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, npc_hp, class_id, body_id, weapon_damage, hp_percent_regen, npc_alliance, stuck_behaviour)
VALUES (65, 2, 'Beefcake', 28800, 50, 6000, 4, 3, 1, 1, '1', '1', 1021221, 3, 129, 20000, 0.01, '61 62 63 64 65 66 67 68', 2);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, npc_hp, class_id, body_state, body_id, face_id, equipped_items, weapon_damage, npc_alliance, stuck_behaviour)
VALUES (66, 2, 'Copycat', 14400, 50, 1910, 3, 3, 1, 1, '1', '1', 407690, 5, 4, 11, 1, '0,*,0,*,1,*,0,*,0,*,72,*', 10000, '61 62 63 64 65 66 67 68', 2);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, npc_hp, class_id, body_state, face_id, equipped_items, weapon_damage, npc_alliance, stuck_behaviour)
VALUES (67, 2, 'Insanity', 14400, 50, 2610, 3, 3, 1, 1, '1', '1', 666888, 3, 4, 3, '0,*,4,*,1,0,0,0,160,0,*,0,*,1,*', 11200, '61 62 63 64 65 66 67 68', 2);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, npc_hp, class_id, body_state, face_id, equipped_items, weapon_damage, npc_alliance, stuck_behaviour)
VALUES (68, 2, 'Showboat', 14400, 50, 2310, 3, 3, 1, 1, '1', '1', 399502, 3, 5, 3, '15,192,28,40,180,22,224,27,36,180,1,224,27,36,180,0,*,0,*,61,*', 10800, '61 62 63 64 65 66 67 68', 2);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, npc_hp, class_id, body_id, face_id, hair_id, equipped_items, weapon_damage, npc_alliance)
VALUES (69, 2, 'Savage Isle Guard', 30, 50, 1000, 3, 3, 1, 1, '1', '1', 4206, 3, 11, 3, 36, '0,*,22,*,0,*,0,*,0,*,0,*', 2000, '69');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, npc_hp, class_id, body_id, weapon_damage, npc_alliance)
VALUES (70, 2, 'Worthless Guardian', 40, 50, 1400, 4, 2, 1, 1, '1', '1', 41206, 3, 128, 900, '70 71 72 73 74');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, rootable, slowable, npc_hp, class_id, body_id, weapon_damage, npc_alliance)
VALUES (71, 2, 'Little Fat Bastard', 9000, 50, 1100, 3, 2, 1.3, 1.3, '1', '1', '1', 1017188, 3, 128, 20100, '70 71 72 73 74');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, npc_hp, class_id, equipped_items, weapon_damage, npc_alliance, stuck_behaviour)
VALUES (72, 2, 'Hay', 10800, 50, 3000, 4, 3, 1, 1, '1', '1', 215502, 3, '11,66,73,165,160,122,255,120,0,180,1,*,4,*,0,*,90,*', 4200, '70 71 72 73 74', 2);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, npc_hp, class_id, body_state, equipped_items, weapon_damage, npc_alliance, stuck_behaviour)
VALUES (73, 2, 'Fray', 10800, 50, 2710, 4, 3, 1, 1, '1', '1', 169502, 3, 5, '15,192,28,40,180,0,*,1,*,2,237,51,59,180,0,*,22,*', 3800, '70 71 72 73 74', 2);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, npc_hp, class_id, body_id, weapon_damage, npc_alliance)
VALUES (74, 2, 'One with Head Thingy', 50, 50, 1440, 4, 1, 1.2, 1.2, '1', '1', 16188, 3, 128, 1100, '70 71 72 73 74');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, npc_hp, class_id, body_state, body_id, hair_id, equipped_items, weapon_damage, hp_percent_regen, npc_alliance, stuck_behaviour)
VALUES (75, 2, 'Ancient Warrior', 1800, 50, 23000, 4, 1, 1.2, 1.3, '1', '1', 460502, 3, 4, 1, 14, '36,130,60,150,100,0,*,0,*,0,*,0,*,2,*', 20000, 0.02, '75 76', 1);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, slowable, npc_hp, class_id, body_state, body_id, equipped_items, weapon_damage, hp_percent_regen, npc_alliance, stuck_behaviour)
VALUES (76, 2, 'Elite Ancient Warrior', 3600, 50, 50000, 7, 2, 1, 1.3, '1', 890502, 3, 4, 1, '11,60,80,230,140,20,*,0,*,0,*,0,*,316,*', 40000, 0.02, '76', 2);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, stationary, slowable, npc_hp, class_id, body_id, weapon_damage, hp_percent_regen, stuck_behaviour)
VALUES (77, 2, 'Comissioned Blacksmith', 14400, 50, 75000, 1, 1, '1', '1', 1000502, 3, 10126, 12000, 0.02, 1);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, stationary, slowable, npc_hp, class_id, body_id, weapon_damage, hp_percent_regen, stuck_behaviour)
VALUES (78, 2, 'Comissioned Tailor', 14400, 50, 75000, 1, 1, '1', '1', 1000502, 3, 10116, 12000, 0.02, 1);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, npc_hp, class_id, body_state, face_id, equipped_items, weapon_damage, hp_percent_regen, npc_alliance, stuck_behaviour)
VALUES (79, 2, 'Dalph Von''Ownu', 28800, 50, 100000, 4, 2, 1, 1, '1', '1', 1000502, 3, 5, 1, '36,130,60,150,100,68,130,60,150,100,6,170,80,100,100,4,170,80,100,100,0,*,20,*', 90000, 0.02, '75 76 79 80 81 82 83', 1);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, npc_hp, class_id, body_state, equipped_items, weapon_damage, hp_percent_regen, npc_alliance, stuck_behaviour)
VALUES (80, 2, 'Hairy Fairy Princess', 28800, 50, 100000, 4, 2, 1, 1, '1', '1', 1000502, 3, 4, '3,251,170,255,180,0,*,1,224,27,36,180,2,237,51,59,180,0,*,29,*', 80000, 0.02, '75 76 79 80 81 82 83', 1);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, npc_hp, class_id, body_id, equipped_items, weapon_damage, hp_percent_regen, npc_alliance, stuck_behaviour)
VALUES (81, 2, 'Lady Slither', 28800, 50, 100000, 5, 3, 1, 1, '1', '1', 1000502, 3, 11, '13,*,62,*,0,*,0,*,0,*,0,*', 70000, 0.02, '75 76 79 80 81 82 83', 1);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, npc_hp, class_id, body_id, equipped_items, weapon_damage, hp_percent_regen, npc_alliance, stuck_behaviour)
VALUES (82, 2, 'Sir Insidious', 28800, 50, 100000, 5, 3, 1, 1, '1', '1', 1000502, 3, 1, '15,*,61,*,0,*,0,*,0,*,0,*', 70000, 0.02, '75 76 79 80 81 82 83', 1);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, rootable, slowable, npc_hp, class_id, face_id, hair_id, equipped_items, weapon_damage, hp_percent_regen, npc_alliance)
VALUES (83, 2, 'Ztwel Tahp', 28800, 50, 100000, 3, 3, 1, 1, '1', '1', '1', 1000502, 3, 1, 31, '0,*,11,*,0,*,0,*,0,*,0,*', 90000, 0.02, '75 76 79 80 81 82 83');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, attack_speed, move_speed, stationary, invincible, class_id, face_id, hair_id, equipped_items, weapon_damage, hp_percent_regen)
VALUES (84, 13, 'Hair Dye Guy', 30, 50, 0, 0, '1', '1', 3, 1, 31, '12,*,0,*,0,*,0,*,0,*,0,*', 70000, 0.2);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, attack_speed, move_speed, stationary, invincible, class_id, hair_id, weapon_damage, hp_percent_regen)
VALUES (85, 13, 'Hair 1', 30, 50, 0, 0, '1', '1', 3, 31, 70000, 0.2);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, attack_speed, move_speed, stationary, invincible, class_id, hair_id, weapon_damage, hp_percent_regen)
VALUES (86, 13, 'Hair 2', 30, 50, 0, 0, '1', '1', 3, 32, 70000, 0.2);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, attack_speed, move_speed, stationary, invincible, class_id, hair_id, weapon_damage, hp_percent_regen)
VALUES (87, 13, 'Hair 3', 30, 50, 0, 0, '1', '1', 3, 33, 70000, 0.2);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, attack_speed, move_speed, stationary, invincible, class_id, hair_id, weapon_damage, hp_percent_regen)
VALUES (88, 13, 'Hair 4', 30, 50, 0, 0, '1', '1', 3, 34, 70000, 0.2);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, attack_speed, move_speed, stationary, invincible, class_id, hair_id, weapon_damage, hp_percent_regen)
VALUES (89, 13, 'Hair 5', 30, 50, 0, 0, '1', '1', 3, 35, 70000, 0.2);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, attack_speed, move_speed, stationary, invincible, class_id, hair_id, weapon_damage, hp_percent_regen)
VALUES (90, 13, 'Hair 6', 30, 50, 0, 0, '1', '1', 3, 36, 70000, 0.2);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, attack_speed, move_speed, stationary, invincible, class_id, weapon_damage, hp_percent_regen)
VALUES (91, 13, 'Hair 7', 30, 50, 0, 0, '1', '1', 3, 70000, 0.2);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, attack_speed, move_speed, stationary, invincible, class_id, face_id, weapon_damage, hp_percent_regen)
VALUES (92, 13, 'Face 1', 30, 50, 0, 0, '1', '1', 3, 1, 70000, 0.2);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, attack_speed, move_speed, stationary, invincible, class_id, face_id, weapon_damage, hp_percent_regen)
VALUES (93, 13, 'Face 2', 30, 50, 0, 0, '1', '1', 3, 1, 70000, 0.2);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, attack_speed, move_speed, stationary, invincible, class_id, face_id, weapon_damage, hp_percent_regen)
VALUES (94, 13, 'Face 3', 30, 50, 0, 0, '1', '1', 3, 3, 70000, 0.2);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, attack_speed, move_speed, stationary, invincible, class_id, face_id, weapon_damage, hp_percent_regen)
VALUES (95, 13, 'Face 4', 30, 50, 0, 0, '1', '1', 3, 2, 70000, 0.2);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, attack_speed, move_speed, stationary, invincible, class_id, weapon_damage, hp_percent_regen)
VALUES (96, 13, 'Male', 30, 50, 0, 0, '1', '1', 3, 70000, 0.2);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, attack_speed, move_speed, stationary, invincible, class_id, body_id, weapon_damage, hp_percent_regen)
VALUES (97, 13, 'Female', 30, 50, 0, 0, '1', '1', 3, 11, 70000, 0.2);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, attack_speed, move_speed, stationary, invincible, class_id, body_state, face_id, hair_id, equipped_items, weapon_damage, hp_percent_regen, quest_ids)
VALUES (98, 13, 'Magus Trainer', 30, 50, 0, 0, '1', '1', 3, 5, 1, 34, '15,192,28,40,180,22,224,27,36,180,1,224,27,36,180,2,237,51,59,180,0,*,171,*', 70000, 0.2, '2');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, attack_speed, move_speed, stationary, invincible, class_id, body_state, face_id, hair_id, equipped_items, weapon_damage, hp_percent_regen, quest_ids)
VALUES (99, 13, 'Priest Trainer', 30, 50, 0, 0, '1', '1', 3, 4, 1, 33, '15,*,22,28,113,216,180,1,28,113,216,180,2,181,131,90,180,0,*,72,*', 70000, 0.2, '4');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, attack_speed, move_speed, stationary, invincible, class_id, face_id, hair_id, equipped_items, weapon_damage, hp_percent_regen)
VALUES (100, 13, 'Potion Guy', 30, 50, 0, 0, '1', '1', 3, 1, 34, '12,*,0,*,0,*,0,*,0,*,0,*', 70000, 0.2);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, attack_speed, move_speed, stationary, invincible, class_id, face_id, hair_id, equipped_items, weapon_damage, hp_percent_regen)
VALUES (101, 13, 'Bronze Armor Guy', 30, 50, 0, 0, '1', '1', 3, 1, 34, '11,250,150,50,140,52,20,65,30,160,14,250,150,50,140,9,250,150,50,140,0,*,0,*', 70000, 0.2);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, attack_speed, move_speed, stationary, invincible, class_id, face_id, hair_id, equipped_items, weapon_damage, hp_percent_regen)
VALUES (102, 13, 'Iron Armor Guy', 30, 50, 0, 0, '1', '1', 3, 1, 34, '11,*,52,70,70,70,140,14,*,9,*,0,*,0,*', 70000, 0.2);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, attack_speed, move_speed, stationary, invincible, class_id, face_id, hair_id, equipped_items, weapon_damage, hp_percent_regen)
VALUES (103, 13, 'Steel Armor Guy', 30, 50, 0, 0, '1', '1', 3, 1, 34, '11,255,255,255,70,52,100,100,100,100,14,255,255,255,70,2,255,255,255,70,0,*,0,*', 70000, 0.2);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, attack_speed, move_speed, stationary, invincible, class_id, face_id, hair_id, equipped_items, weapon_damage, hp_percent_regen)
VALUES (104, 13, 'Shield Guy', 30, 50, 0, 0, '1', '1', 3, 1, 34, '11,*,52,*,6,*,4,*,263,*,0,*', 70000, 0.2);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, attack_speed, move_speed, stationary, invincible, class_id, body_state, face_id, hair_id, equipped_items, weapon_damage, hp_percent_regen)
VALUES (105, 13, 'Weapon Guy', 30, 50, 0, 0, '1', '1', 3, 4, 1, 34, '118,*,22,*,0,*,0,*,68,189,93,90,160,11,*', 70000, 0.2);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, attack_speed, move_speed, stationary, invincible, class_id, face_id, hair_id, equipped_items, weapon_damage, hp_percent_regen)
VALUES (106, 13, 'Cloth Armor Guy', 30, 50, 0, 0, '1', '1', 3, 1, 34, '21,*,52,*,1,*,2,181,131,90,180,0,*,0,*', 70000, 0.2);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, attack_speed, move_speed, stationary, invincible, class_id, face_id, hair_id, equipped_items, weapon_damage, hp_percent_regen)
VALUES (107, 13, 'Leather Armor Guy', 30, 50, 0, 0, '1', '1', 3, 1, 34, '21,*,52,*,1,*,2,181,131,90,180,0,*,0,*', 70000, 0.2);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, attack_speed, move_speed, stationary, invincible, class_id, face_id, hair_id, equipped_items, weapon_damage, hp_percent_regen)
VALUES (108, 13, 'Silk Armor Guy', 30, 50, 0, 0, '1', '1', 3, 1, 34, '118,*,22,*,0,*,0,*,0,*,0,*', 70000, 0.2);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, npc_hp, class_id, body_state, body_id, face_id, equipped_items, weapon_damage, npc_alliance)
VALUES (109, 2, 'Nagan Sentry', 1980, 50, 2829, 3, 1, 1.2, 1.2, '1', '1', 1000, 3, 4, 1, 1, '36,231,223,107,160,12,*,0,*,0,*,0,*,38,*', 400, '26 27 28');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, class_id, body_id, weapon_damage, npc_alliance)
VALUES (110, 2, 'Carrots', 7200, 33, 608, 4, 1, 1.2, 1.2, '1', '1', 3, 10137, 135, '111 112 113 114');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, class_id, body_id, weapon_damage, npc_alliance)
VALUES (111, 2, 'Fluffzilla', 7200, 32, 608, 4, 1, 1.2, 1.2, '1', '1', 3, 121, 147, '110 112 113 114');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, class_id, body_id, weapon_damage, npc_alliance)
VALUES (112, 2, 'Snowball', 30, 24, 489, 2, 1, 1.2, 1.2, '1', '1', 3, 121, 87, '110 111 112 113 114');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, class_id, body_id, weapon_damage, npc_alliance)
VALUES (113, 2, 'Midnight Madness', 30, 21, 408, 2, 1, 1.2, 1.2, '1', '1', 3, 10138, 78, '110 111 112 113 114');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, class_id, body_id, weapon_damage, npc_alliance)
VALUES (114, 2, 'Melty', 30, 28, 548, 3, 1, 1.1, 1.1, '1', '1', 3, 114, 102, '110 111 112 113 114');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, stunnable, slowable, npc_hp, class_id, body_id, weapon_damage, npc_alliance)
VALUES (115, 2, 'Unloved', 30, 35, 909, 3, 1, 1.2, 1.2, '1', '1', '1', 540, 2, 169, 132, '115 116');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, npc_hp, class_id, body_id, weapon_damage, npc_alliance)
VALUES (116, 2, 'Nibbles', 9000, 45, 1200, 4, 2, 1.4, 1.4, '1', '1', 3488, 3, 10100, 180, '115');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, npc_hp, class_id, body_id, weapon_damage, npc_alliance)
VALUES (117, 2, 'Wraith', 30, 50, 3000, 4, 2, 1.2, 1.2, '1', '1', 50130, 3, 10153, 9000, '117 118');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, npc_hp, class_id, body_id, weapon_damage, npc_alliance, stuck_behaviour)
VALUES (118, 2, 'Nibbles II', 14400, 50, 8000, 4, 2, 1.3, 1.3, '1', '1', 353488, 3, 10127, 30000, '117', 2);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, attack_speed, move_speed, stationary, invincible, class_id, face_id, hair_id, equipped_items, weapon_damage, hp_percent_regen)
VALUES (119, 13, 'Teleporter Vendor Guy', 30, 50, 0, 0, '1', '1', 3, 1, 31, '12,*,0,*,0,*,0,*,0,*,0,*', 70000, 0.2);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, attack_speed, move_speed, stationary, invincible, class_id, face_id, hair_id, equipped_items, weapon_damage, hp_percent_regen)
VALUES (120, 13, 'Stranger with Candy', 30, 50, 0, 0, '1', '1', 3, 1, 31, '34,*,0,*,0,*,0,*,0,*,0,*', 70000, 0.2);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, attack_range, attack_speed, move_speed, stationary, npc_hp, class_id, face_id, hair_id, equipped_items, weapon_damage, hp_percent_regen)
VALUES (121, 13, 'Paradise Teleporter Vendor Guy', 30, 50, 1, 0.3, 0, '1', 100000, 3, 1, 31, '12,*,0,*,0,*,0,*,0,*,0,*', 7000, 0.02);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, attack_speed, invincible, class_id, face_id, hair_id, equipped_items, weapon_damage, hp_percent_regen)
VALUES (122, 13, 'The Bat Man', 30, 50, 0, '1', 3, 1, 31, '34,*,0,*,0,*,0,*,0,*,0,*', 70000, 0.2);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, rootable, slowable, npc_hp, class_id, body_state, body_id, equipped_items, weapon_damage, hp_percent_regen, npc_alliance, stuck_behaviour)
VALUES (123, 2, 'Ancient Defender', 2700, 50, 64000, 3, 1, 1.5, 1.3, '1', '1', '1', 4135155, 3, 3, 129, '0,*,0,*,0,*,0,*,0,*,10,*', 80000, 0.02, '123 138', 1);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, stunnable, slowable, npc_hp, class_id, body_state, body_id, equipped_items, weapon_damage, hp_percent_regen, npc_alliance, stuck_behaviour)
VALUES (124, 2, 'Ancient Guard', 2700, 50, 72000, 3, 1, 1.5, 1.5, '1', '1', '1', 5021222, 3, 3, 129, '36,*,20,58,56,56,180,14,*,9,*,0,*,19,*', 90000, 0.02, '124 143', 1);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, stunnable, slowable, npc_hp, class_id, body_id, weapon_damage, hp_percent_regen, npc_alliance, stuck_behaviour)
VALUES (125, 2, 'Ancient Safeguard', 300, 50, 80000, 3, 1, 1.5, 1.3, '1', '1', '1', 40016888, 3, 129, 200000, 0.02, '125', 1);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, npc_hp, class_id, body_state, body_id, equipped_items, weapon_damage, hp_percent_regen, npc_alliance, stuck_behaviour)
VALUES (126, 2, 'General Mehoff', 28800, 50, 108000, 4, 3, 1.5, 1.5, '1', '1', 20143269, 3, 3, 129, '9,*,0,*,0,*,0,*,0,*,171,*', 100000, 0.02, '123 124 125', 1);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, stunnable, rootable, slowable, npc_hp, class_id, body_id, weapon_damage, hp_percent_regen, npc_alliance)
VALUES (127, 2, 'Berry Stealer', 3600, 50, 1400, 3, 1, 1.5, 1.3, '1', '1', '1', '1', 6678, 3, 10113, 3000, 0.02, '157 130 127 131 128');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, npc_hp, class_id, body_id, weapon_damage, hp_percent_regen, npc_alliance)
VALUES (128, 2, 'Guardian Bear', 600, 50, 10410, 3, 3, 1.5, 1.5, '1', '1', 505138, 3, 120, 30000, 0.35, '157 130 127 131 128');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, rootable, slowable, npc_hp, class_id, body_id, weapon_damage, hp_percent_regen, npc_alliance)
VALUES (129, 2, 'Patrol Bear', 3600, 50, 8310, 3, 3, 1.5, 1.5, '1', '1', 1362366, 3, 120, 20000, 0.25, '129 156');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, rootable, slowable, npc_hp, class_id, body_id, weapon_damage, hp_percent_regen, npc_alliance)
VALUES (130, 2, 'Young Bear', 40, 50, 6510, 3, 3, 1.5, 1.5, '1', '1', 262366, 3, 120, 5500, 0.02, '157 130 127 131 128');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, slowable, npc_hp, class_id, body_state, body_id, face_id, equipped_items, weapon_damage, hp_percent_regen, npc_alliance)
VALUES (131, 2, 'Betsy the Bear Charmer', 3600, 50, 7210, 3, 1, 1, 1.4, '1', 316298, 3, 4, 11, 1, '3,*,0,*,0,*,0,*,0,*,0,*', 8000, 0.02, '157 130 127 131 128');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, npc_hp, class_id, body_state, body_id, equipped_items, weapon_damage, npc_alliance, stuck_behaviour)
VALUES (132, 2, 'Sorrows Hero', 14400, 50, 16000, 4, 2, 1.2, 1.2, '1', '1', 909934, 3, 3, 129, '36,214,214,214,140,20,214,214,214,140,14,214,214,214,140,9,214,214,214,140,0,*,316,*', 15000, '133 134', 1);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, npc_hp, class_id, body_id, weapon_damage, npc_alliance)
VALUES (133, 2, 'Pancake', 40, 50, 2210, 6, 2, 1.5, 1.5, '1', '1', 50130, 3, 117, 3000, '132 133 134');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, npc_hp, class_id, body_id, weapon_damage, npc_alliance)
VALUES (134, 2, 'Flapjack', 40, 50, 2810, 6, 2, 1.5, 1.5, '1', '1', 67362, 3, 117, 4000, '132 133 134');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, attack_range, attack_speed, move_speed, stationary, rootable, slowable, npc_hp, class_id, body_id, weapon_damage, hp_percent_regen, stuck_behaviour)
VALUES (135, 2, 'Cranky Ewe', 32400, 50, 2000, 2, 1.3, 1.3, '1', '1', '1', 22326, 3, 10114, 2000, 0.01, 2);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, slowable, class_id, body_id, weapon_damage, hp_percent_regen, stuck_behaviour)
VALUES (136, 2, 'Frantic Monkey', 32400, 35, 600, 3, 2, 1.3, 1.5, '1', 3, 115, 300, 0.01, 1);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, npc_hp, class_id, body_state, body_id, equipped_items, weapon_damage, npc_alliance, stuck_behaviour)
VALUES (137, 2, 'Sorrows Lover', 14400, 50, 8000, 4, 2, 1.2, 1.2, '1', '1', 709934, 3, 3, 129, '33,214,214,214,140,54,214,214,214,140,13,214,214,214,140,0,*,0,*,2,*', 10000, '133 134', 1);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, npc_hp, class_id, body_state, body_id, equipped_items, weapon_damage, hp_percent_regen, npc_alliance, stuck_behaviour)
VALUES (138, 2, 'Ancient Sentinel', 14400, 50, 80000, 3, 1, 1.5, 1.3, '1', '1', 10016888, 3, 3, 129, '36,*,0,*,14,*,9,*,0,*,98,*', 95000, 0.02, '123 138', 1);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, npc_hp, class_id, body_state, body_id, equipped_items, weapon_damage, hp_percent_regen, npc_alliance, stuck_behaviour)
VALUES (139, 2, 'General Vor Chez', 28800, 50, 108000, 4, 3, 1.5, 1.5, '1', '1', 20143269, 3, 3, 129, '9,*,22,224,27,36,180,0,*,0,*,0,*,171,*', 100000, 0.02, '123 124 125', 1);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, npc_hp, class_id, body_state, body_id, equipped_items, weapon_damage, hp_percent_regen, npc_alliance, stuck_behaviour)
VALUES (140, 2, 'Doom', 43200, 50, 108000, 4, 3, 1.5, 1.5, '1', '1', 30143269, 3, 3, 129, '14,*,53,*,0,*,0,*,0,*,37,*', 115000, 0.02, '123 124 125', 1);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, npc_hp, class_id, body_state, body_id, equipped_items, weapon_damage, hp_percent_regen, npc_alliance, stuck_behaviour)
VALUES (141, 2, 'Disciple Von Bangs', 14400, 50, 108000, 4, 3, 1.5, 1.5, '1', '1', 8143269, 3, 3, 129, '33,*,54,*,13,*,0,*,0,*,41,*', 90000, 0.02, '123 124 125', 1);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, npc_hp, class_id, body_state, body_id, equipped_items, weapon_damage, hp_percent_regen, npc_alliance, stuck_behaviour)
VALUES (142, 2, 'Disciple Von Chief', 14400, 50, 108000, 4, 3, 1.5, 1.5, '1', '1', 8143269, 3, 3, 129, '33,*,54,*,13,*,0,*,0,*,41,*', 90000, 0.02, '123 124 125', 1);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stunnable, slowable, npc_hp, class_id, body_state, body_id, equipped_items, weapon_damage, hp_percent_regen, npc_alliance, stuck_behaviour)
VALUES (143, 2, 'Ancient Guard', 2700, 50, 72000, 3, 1, 1.5, 1.5, '1', '1', 5021222, 3, 3, 129, '36,*,20,58,56,56,180,14,*,9,*,0,*,19,*', 68000, 0.005, '124 143', 1);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, npc_hp, class_id, body_state, body_id, weapon_damage, npc_alliance)
VALUES (144, 2, 'Invisible 2', 14400, 50, 64000, 4, 2, 1.2, 1.2, '1', '1', 2109934, 3, 4, 0, 38000, '146 144');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, npc_hp, class_id, body_id, weapon_damage, npc_alliance)
VALUES (145, 2, 'Giant Wraith', 60, 50, 5810, 10, 2, 1.5, 1.5, '1', '1', 100130, 3, 125, 8000, '145 147');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, npc_hp, class_id, body_id, weapon_damage, npc_alliance)
VALUES (146, 2, 'Returned', 60, 50, 7910, 6, 2, 1.5, 1.5, '1', '1', 140362, 3, 10125, 12000, '146 144');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, npc_hp, class_id, body_state, body_id, weapon_damage, npc_alliance)
VALUES (147, 2, 'Invisible 1', 14400, 50, 32000, 4, 2, 1.2, 1.2, '1', '1', 1409934, 3, 4, 0, 28000, '145 147');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, attack_speed, move_speed, stationary, invincible, class_id, face_id, hair_id, equipped_items, weapon_damage, hp_percent_regen)
VALUES (148, 13, 'Tailoring Supplies', 30, 50, 0, 0, '1', '1', 3, 1, 34, '11,*,52,70,70,70,140,14,*,9,*,0,*,0,*', 70000, 0.2);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, attack_speed, move_speed, stationary, invincible, class_id, face_id, hair_id, equipped_items, weapon_damage, hp_percent_regen)
VALUES (149, 13, 'Smithing Supplies', 30, 50, 0, 0, '1', '1', 3, 1, 34, '11,*,52,70,70,70,140,14,*,9,*,0,*,0,*', 70000, 0.2);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, attack_speed, move_speed, stationary, invincible, class_id, face_id, hair_id, equipped_items, weapon_damage, hp_percent_regen)
VALUES (150, 13, 'Scriber', 30, 50, 0, 0, '1', '1', 3, 1, 34, '11,*,52,70,70,70,140,14,*,9,*,0,*,0,*', 70000, 0.2);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, slowable, npc_hp, class_id, body_id, weapon_damage, hp_percent_regen, npc_alliance)
VALUES (151, 2, 'Richard', 40, 50, 12410, 4, 2, 1.5, 1.5, '1', 623138, 3, 153, 12000, 0.02, '151 152');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, npc_hp, class_id, body_id, weapon_damage, hp_percent_regen, npc_alliance)
VALUES (152, 2, 'Abomination', 14400, 50, 34010, 3, 2, 1.2, 1.5, '1', '1', 3823138, 3, 233, 60000, 0.015, '151');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, slowable, npc_hp, class_id, body_id, weapon_damage, hp_percent_regen, npc_alliance)
VALUES (153, 2, 'Simon', 40, 50, 16010, 4, 2, 1.5, 1.5, '1', 853138, 3, 153, 15000, 0.02, '153 154');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, npc_hp, class_id, body_id, weapon_damage, hp_percent_regen, npc_alliance)
VALUES (154, 2, 'Charlie', 14400, 50, 54010, 3, 2, 1.2, 1.5, '1', '1', 5823138, 3, 160, 80000, 0.015, '153');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, slowable, npc_hp, class_id, body_id, weapon_damage, hp_percent_regen, npc_alliance)
VALUES (155, 2, 'The Patriarch', 10800, 50, 15410, 2, 2, 1.5, 1.5, '1', 1005138, 3, 120, 30000, 0.5, '155');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, slowable, npc_hp, class_id, body_id, weapon_damage, hp_percent_regen, npc_alliance, stuck_behaviour)
VALUES (156, 2, 'Mama Bear', 14400, 50, 34410, 3, 5, 1.5, 1.5, '1', 4105138, 3, 120, 150000, 0.1, '156', 1);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, slowable, npc_hp, class_id, body_id, weapon_damage, hp_percent_regen, npc_alliance)
VALUES (157, 2, 'Gramps', 1800, 50, 16400, 3, 3, 1.5, 1.5, '1', 1505138, 3, 120, 40000, 0.06, '157 130 127 131 128');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, move_speed, slowable, npc_hp, class_id, body_id, weapon_damage, hp_percent_regen, npc_alliance)
VALUES (158, 2, 'Starved Bear', 600, 50, 20410, 3, 2, 1, '1', 1005138, 3, 120, 20000, 0.05, '158 155');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, attack_speed, invincible, class_id, face_id, hair_id, equipped_items, weapon_damage, hp_percent_regen)
VALUES (159, 13, 'Wandering Scribe', 30, 50, 0, '1', 3, 1, 34, '11,*,52,70,70,70,140,14,*,9,*,0,*,0,*', 70000, 0.2);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, attack_speed, invincible, class_id, face_id, hair_id, equipped_items, weapon_damage, hp_percent_regen)
VALUES (160, 13, 'Wandering Scribe', 30, 50, 0, '1', 3, 1, 34, '11,*,52,70,70,70,140,14,*,9,*,0,*,0,*', 70000, 0.2);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, npc_hp, class_id, body_id, weapon_damage, hp_percent_regen, npc_alliance, stuck_behaviour, stuck_timeout)
VALUES (161, 2, 'Ancient Royal', 7200, 50, 88000, 4, 4, 1.4, 1.5, '1', '1', 10143269, 3, 129, 75000, 0.025, '161 162 163 164 165', 1, 5);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, npc_hp, class_id, body_id, equipped_items, weapon_damage, hp_percent_regen, npc_alliance, stuck_behaviour, stuck_timeout)
VALUES (162, 2, 'King Terror', 28800, 50, 120000, 4, 4, 1.8, 1.5, '1', '1', 30143269, 3, 129, '34,*,53,*,0,*,0,*,0,*,0,*', 200000, 0.03, '161 162 163 164 165', 1, 5);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, npc_hp, class_id, body_state, body_id, equipped_items, weapon_damage, hp_percent_regen, npc_alliance, stuck_behaviour, stuck_timeout)
VALUES (163, 2, 'Prince Punisher', 28800, 50, 120000, 4, 4, 1.5, 1.5, '1', '1', 25143269, 3, 3, 129, '34,*,52,*,0,*,0,*,0,*,104,*', 150000, 0.03, '161 162 163 164 165', 1, 5);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, npc_hp, class_id, body_state, body_id, equipped_items, weapon_damage, hp_percent_regen, npc_alliance, stuck_behaviour, stuck_timeout)
VALUES (164, 2, 'Queen Butcher', 28800, 50, 120000, 3, 3, 1.1, 1.5, '1', '1', 30143269, 3, 3, 129, '33,*,62,*,13,*,0,*,0,*,107,*', 100000, 0.02, '161 162 163 164 165', 1, 5);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, npc_hp, class_id, body_id, equipped_items, weapon_damage, hp_percent_regen, npc_alliance, stuck_behaviour, stuck_timeout)
VALUES (165, 2, 'Princess Slayer', 28800, 50, 120000, 3, 3, 1.5, 1.5, '1', '1', 20143269, 3, 129, '23,*,62,*,0,*,0,*,0,*,0,*', 100000, 0.02, '161 162 163 164 165', 1, 5);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, class_id, body_id, weapon_damage, npc_alliance)
VALUES (166, 2, 'Pumpkin', 30, 20, 489, 1, 1, 1.5, 1.5, '1', '1', 4, 127, 65, '166 167');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, class_id, body_id, weapon_damage, npc_alliance)
VALUES (167, 2, 'Fail Pumpkin', 3600, 25, 1000, 1, 1, 1.3, 1.2, '1', '1', 3, 10147, 90, '166');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, class_id, body_id, weapon_damage, npc_alliance)
VALUES (168, 2, 'Chief Pumpkin', 30, 30, 600, 1, 1, 1.5, 1.5, '1', '1', 4, 10146, 100, '168 169');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, class_id, body_id, weapon_damage, npc_alliance)
VALUES (169, 2, 'Boss Pumpkin', 3600, 35, 2000, 1, 1, 1.3, 1.2, '1', '1', 3, 10147, 140, '168');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, attack_speed, move_speed, stationary, invincible, class_id, body_state, face_id, hair_id, equipped_items, weapon_damage, hp_percent_regen)
VALUES (170, 13, 'Phat Lewtz', 30, 50, 0, 0, '1', '1', 3, 5, 1, 34, '16,224,27,36,180,3,*,0,*,0,*,79,*,0,*', 70000, 0.2);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, attack_range, attack_speed, move_speed, stationary, npc_hp, class_id, body_id, equipped_items, weapon_damage, npc_alliance)
VALUES (171, 2, 'Team 1 Guard', 7200, 50, 1, 1, 0, '1', 300000, 3, 129, '0,*,20,255,0,0,180,0,*,0,*,0,*,0,*', 1000, '172');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, attack_range, attack_speed, move_speed, stationary, npc_hp, class_id, body_id, equipped_items, weapon_damage, npc_alliance)
VALUES (172, 2, 'Team 1 Boss', 7200, 50, 3, 1, 0, '1', 900000, 3, 129, '20,255,0,0,180,20,255,0,0,180,0,*,0,*,0,*,0,*', 3000, '171');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, attack_range, attack_speed, move_speed, stationary, npc_hp, class_id, body_id, equipped_items, weapon_damage, npc_alliance)
VALUES (173, 2, 'Team 2 Guard', 7200, 50, 1, 1, 0, '1', 300000, 3, 129, '0,*,20,0,0,255,180,0,*,0,*,0,*,0,*', 1000, '174');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, attack_range, attack_speed, move_speed, stationary, npc_hp, class_id, body_id, equipped_items, weapon_damage, npc_alliance)
VALUES (174, 2, 'Team 2 Boss', 7200, 50, 3, 1, 0, '1', 900000, 3, 129, '20,0,0,255,180,20,0,0,255,180,0,*,0,*,0,*,0,*', 3000, '173');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, npc_hp, class_id, body_id, weapon_damage, hp_percent_regen, npc_alliance, stuck_behaviour, stuck_timeout)
VALUES (175, 2, 'Ancient Prisoner', 7200, 50, 300000, 4, 4, 1.4, 1, '1', '1', 30143269, 3, 10128, 2000000, 0.05, '175 176', 1, 5);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, slowable, npc_hp, class_id, body_id, weapon_damage, hp_percent_regen, npc_alliance, stuck_behaviour, stuck_timeout)
VALUES (176, 2, 'Ancient Prison Superior', 7200, 50, 300000, 4, 4, 1.2, 1, '1', 50143269, 3, 10125, 3400000, 0.05, '176', 1, 5);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, npc_hp, class_id, body_id, weapon_damage, hp_percent_regen, npc_alliance, stuck_behaviour, stuck_timeout)
VALUES (177, 2, 'Henry', 20800, 50, 300000, 4, 4, 1.2, 1, '1', '1', 70143269, 3, 10106, 3600000, 0.05, '176', 1, 5);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, npc_hp, class_id, body_id, weapon_damage, hp_percent_regen, npc_alliance, stuck_behaviour, stuck_timeout)
VALUES (178, 2, 'Gordon', 20800, 50, 300000, 4, 4, 1.2, 1, '1', '1', 100143269, 3, 10106, 4000000, 0.05, '176', 1, 5);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, experience, aggro_range, attack_range, attack_speed, move_speed, stationary, slowable, npc_hp, class_id, body_id, weapon_damage, hp_percent_regen, npc_alliance, stuck_behaviour, stuck_timeout)
VALUES (179, 2, 'Rueben', 20800, 50, 300000, 4, 4, 1.2, 1, '1', '1', 80143269, 3, 10106, 3800000, 0.05, '176', 1, 5);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, aggro_range, attack_speed, move_speed, stationary, invincible, class_id, body_state, face_id, equipped_items, weapon_damage, hp_percent_regen, credit_dealer)
VALUES (180, 13, 'Credit Exchange', 30, 50, 4, 0, 0, '1', '1', 3, 5, 1, '36,130,60,150,100,68,130,60,150,100,6,170,80,100,100,4,170,80,100,100,0,*,20,*', 70000, 0.2, '1');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, aggro_range, attack_speed, move_speed, slowable, invincible, npc_hp, class_id, body_state, body_id, face_id, equipped_items, weapon_damage, hp_percent_regen)
VALUES (181, 13, 'Pet Trainer', 30, 50, 3, 0, 0, '1', '1', 316298, 3, 4, 11, 1, '3,*,0,*,0,*,0,*,0,*,0,*', 70000, 0.2);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, attack_speed, move_speed, stationary, invincible, class_id, face_id, hair_id, equipped_items, weapon_damage, hp_percent_regen)
VALUES (182, 13, 'Alchemy Supplier Guy', 30, 50, 0, 0, '1', '1', 3, 1, 34, '12,*,0,*,0,*,0,*,0,*,0,*', 70000, 0.2);
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, attack_speed, move_speed, stationary, invincible, class_id, body_state, body_id, face_id, hair_id, equipped_items, weapon_damage, hp_percent_regen, quest_ids)
VALUES (183, 12, 'Bruno the Warrior Trainer', 30, 50, 0, 0, '1', '1', 3, 4, 1, 1, 34, '11,66,69,189,150,20,*,6,*,4,*,0,*,109,*', 70000, 0.2, '1');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, attack_speed, move_speed, stationary, invincible, class_id, body_state, body_id, face_id, hair_id, equipped_items, weapon_damage, hp_percent_regen, quest_ids)
VALUES (184, 12, 'Aryn the Rogue Trainer', 30, 50, 0, 0, '1', '1', 3, 4, 1, 3, 35, '11,*,20,58,56,56,180,14,*,9,*,0,*,177,*', 70000, 0.2, '3');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, attack_speed, move_speed, stationary, invincible, class_id, body_id, face_id, hair_id, equipped_items, weapon_damage, hp_percent_regen, quest_ids)
VALUES (185, 12, 'Tavon', 30, 50, 0, 0, '1', '1', 3, 1, 1, 31, '12,*,0,*,0,*,0,*,0,*,0,*', 70000, 0.2, '5');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, attack_speed, move_speed, stationary, invincible, class_id, body_id, face_id, hair_id, equipped_items, weapon_damage, hp_percent_regen, quest_ids)
VALUES (186, 12, 'Farmer Pete', 30, 50, 0, 0, '1', '1', 3, 1, 1, 31, '12,*,0,*,0,*,0,*,0,*,0,*', 70000, 0.2, '6');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, attack_speed, move_speed, stationary, invincible, class_id, body_id, face_id, hair_id, equipped_items, weapon_damage, hp_percent_regen, quest_ids)
VALUES (187, 12, 'Jack', 30, 50, 0, 0, '1', '1', 3, 1, 1, 31, '12,*,0,*,0,*,0,*,0,*,0,*', 70000, 0.2, '7');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, attack_speed, move_speed, stationary, invincible, class_id, body_id, face_id, hair_id, equipped_items, weapon_damage, hp_percent_regen, quest_ids)
VALUES (188, 12, 'Jill', 30, 50, 0, 0, '1', '1', 3, 1, 1, 31, '12,*,0,*,0,*,0,*,0,*,0,*', 70000, 0.2, '8');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, attack_speed, move_speed, stationary, invincible, class_id, body_id, face_id, hair_id, equipped_items, weapon_damage, hp_percent_regen, quest_ids)
VALUES (189, 12, 'Talfen', 30, 50, 0, 0, '1', '1', 3, 1, 1, 31, '12,*,0,*,0,*,0,*,0,*,0,*', 70000, 0.2, '9');
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, npc_level, attack_speed, move_speed, stationary, invincible, class_id, body_id, face_id, hair_id, equipped_items, weapon_damage, hp_percent_regen, quest_ids)
VALUES (190, 12, 'Gerald', 30, 50, 0, 0, '1', '1', 3, 1, 1, 31, '12,*,0,*,0,*,0,*,0,*,0,*', 70000, 0.2, '10');


DROP TABLE IF EXISTS npc_spawns;
CREATE TABLE npc_spawns (
  npc_id INT NOT NULL,
  map_id SMALLINT NOT NULL,
  map_x SMALLINT NOT NULL,
  map_y SMALLINT NOT NULL
);

INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (1, 1, 80, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (1, 1, 85, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (1, 1, 90, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (1, 1, 80, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (1, 1, 85, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (1, 1, 90, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (1, 1, 80, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (1, 1, 85, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (1, 1, 90, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (1, 1, 80, 53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (1, 1, 85, 53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (1, 1, 90, 53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (1, 1, 80, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (1, 1, 85, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (1, 1, 90, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (1, 1, 80, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (1, 1, 85, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (1, 1, 90, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (1, 1, 80, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (1, 1, 85, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (1, 1, 90, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (1, 1, 80, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (1, 1, 85, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (1, 1, 90, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (1, 1, 80, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (1, 1, 85, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (1, 1, 90, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (1, 1, 90, 100);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (2, 1, 75, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (2, 1, 78, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (2, 1, 81, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (2, 1, 84, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (2, 1, 87, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (2, 1, 90, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (2, 1, 77, 13);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (2, 1, 80, 13);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (2, 1, 85, 13);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (2, 1, 90, 13);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (2, 1, 75, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (2, 1, 78, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (2, 1, 81, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (2, 1, 84, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (2, 1, 87, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (2, 1, 90, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (2, 1, 77, 19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (2, 1, 80, 19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (2, 1, 85, 19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (2, 1, 90, 19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (2, 1, 75, 22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (2, 1, 78, 22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (2, 1, 81, 22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (2, 1, 84, 22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (2, 1, 87, 22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (2, 1, 90, 22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (2, 1, 91, 22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (3, 16, 43, 93);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (3, 16, 37, 97);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (3, 16, 32, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (3, 16, 24, 98);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (3, 16, 19, 89);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (3, 16, 12, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (3, 16, 23, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (3, 16, 20, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (3, 16, 29, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (3, 16, 45, 48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (3, 16, 53, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (3, 16, 47, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (3, 16, 41, 71);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (3, 16, 38, 79);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (3, 16, 56, 91);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (3, 16, 62, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (3, 16, 69, 93);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (3, 16, 76, 97);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (3, 16, 78, 89);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (3, 16, 95, 91);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (3, 16, 74, 81);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (3, 16, 67, 82);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (3, 16, 62, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (3, 16, 55, 82);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (3, 16, 77, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (3, 16, 86, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (3, 16, 93, 79);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (3, 16, 98, 71);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (3, 16, 86, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (3, 16, 80, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (3, 16, 93, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (3, 16, 48, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (3, 16, 50, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 16, 8, 89);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 16, 8, 89);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 16, 10, 93);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 16, 92, 89);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 16, 96, 94);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 16, 91, 93);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 16, 48, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 16, 43, 52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 16, 36, 54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 16, 38, 58);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 16, 37, 62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 16, 44, 61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 16, 62, 54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 16, 66, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 16, 69, 54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 16, 61, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 16, 70, 61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 16, 4, 48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 16, 2, 52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 16, 7, 53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 16, 5, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 16, 8, 46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 16, 4, 41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 16, 32, 46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 16, 38, 41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 16, 47, 46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 16, 43, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 16, 31, 36);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 16, 23, 32);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 16, 13, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 16, 19, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 16, 16, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 16, 10, 13);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 16, 6, 6);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 16, 25, 8);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 16, 34, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 16, 40, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 16, 43, 13);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 16, 39, 22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 16, 95, 47);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 16, 92, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 16, 83, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 16, 72, 44);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 16, 68, 48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 16, 62, 41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 16, 54, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 16, 65, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 16, 59, 27);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 16, 71, 29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 16, 83, 27);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 16, 95, 29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 16, 92, 18);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 16, 80, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 16, 73, 19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 16, 63, 22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 16, 54, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 16, 85, 8);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 16, 75, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 16, 82, 2);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (5, 16, 7, 61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (5, 16, 7, 91);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (5, 16, 94, 93);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (5, 16, 44, 58);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (6, 8, 93, 22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (6, 8, 86, 27);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (6, 8, 89, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (6, 8, 84, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (6, 8, 93, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (6, 8, 98, 47);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (6, 8, 86, 47);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (6, 8, 73, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (6, 8, 72, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (6, 8, 82, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (6, 8, 89, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (6, 8, 93, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (6, 8, 96, 74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (6, 8, 91, 81);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (6, 8, 90, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (6, 8, 84, 86);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (6, 8, 81, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (6, 8, 84, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (6, 8, 79, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (6, 8, 66, 51);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (6, 8, 83, 23);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (6, 8, 76, 18);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (6, 8, 69, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (6, 8, 62, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (6, 8, 56, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (6, 8, 67, 11);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (6, 8, 77, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (6, 8, 90, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (6, 8, 96, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (6, 8, 95, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (6, 8, 82, 6);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (6, 8, 55, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (6, 8, 51, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (6, 8, 55, 27);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (6, 8, 56, 37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (6, 8, 44, 23);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (6, 8, 35, 18);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (6, 8, 31, 9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (6, 8, 39, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (6, 8, 53, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (6, 8, 81, 27);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (6, 8, 92, 29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (6, 8, 56, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (6, 8, 48, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (6, 8, 44, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (6, 8, 44, 66);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (6, 8, 56, 69);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (6, 8, 64, 78);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (6, 8, 65, 89);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (6, 8, 53, 93);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (6, 8, 40, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (6, 8, 31, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (6, 8, 31, 77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (6, 8, 15, 81);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (6, 8, 14, 64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (6, 8, 7, 58);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (6, 8, 9, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (7, 2, 12, 13);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (7, 2, 16, 11);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (7, 2, 9, 19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (7, 2, 15, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (7, 2, 17, 38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (7, 2, 9, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (7, 2, 7, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (7, 2, 16, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (7, 2, 10, 54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (7, 2, 17, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (7, 2, 22, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (7, 2, 23, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (7, 2, 27, 32);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (7, 2, 29, 28);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (7, 2, 26, 23);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (7, 2, 23, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (7, 2, 23, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (7, 2, 31, 48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (7, 2, 31, 41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (7, 2, 35, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (7, 2, 41, 41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (7, 2, 47, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (7, 2, 50, 44);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (7, 2, 48, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (7, 2, 42, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (7, 2, 39, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (7, 2, 41, 23);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (7, 2, 47, 19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (7, 2, 42, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (7, 2, 37, 19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (8, 14, 41, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (8, 14, 31, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (8, 14, 24, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (8, 14, 16, 9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (8, 14, 19, 3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (8, 14, 9, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (8, 14, 2, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (8, 14, 6, 13);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (8, 14, 3, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (8, 14, 7, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (8, 14, 7, 26);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (8, 14, 16, 27);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (8, 14, 18, 23);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (8, 14, 24, 19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (8, 14, 30, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (8, 14, 35, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (8, 14, 33, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (8, 14, 27, 9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (8, 14, 25, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (8, 14, 42, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (8, 14, 46, 22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (8, 14, 53, 24);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (8, 14, 54, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (8, 14, 61, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (8, 14, 59, 11);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (8, 14, 57, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (8, 14, 62, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (8, 14, 66, 2);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (8, 14, 65, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (8, 14, 67, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (8, 14, 68, 28);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (8, 14, 78, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (8, 14, 75, 37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (8, 14, 66, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (8, 14, 57, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (8, 14, 55, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (9, 14, 60, 48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (9, 14, 62, 54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (9, 14, 58, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (9, 14, 60, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (9, 14, 59, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (9, 14, 58, 72);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (9, 14, 58, 77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (9, 14, 62, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (9, 14, 69, 78);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (9, 14, 74, 79);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (9, 14, 77, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (9, 14, 82, 79);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (9, 14, 77, 73);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (9, 14, 72, 74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (9, 14, 70, 69);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (9, 14, 68, 66);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (9, 14, 66, 62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (9, 14, 70, 61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (9, 14, 71, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (9, 14, 68, 58);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (9, 14, 68, 53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (9, 14, 75, 53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (9, 14, 76, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (9, 14, 81, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (9, 14, 84, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (9, 14, 85, 53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (9, 14, 86, 63);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (9, 14, 81, 64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (9, 14, 84, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (9, 14, 75, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (9, 14, 74, 62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (9, 14, 72, 63);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (10, 3, 44, 93);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (10, 3, 56, 92);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (10, 3, 63, 94);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (10, 3, 66, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (10, 3, 61, 84);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (10, 3, 51, 79);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (10, 3, 54, 71);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (10, 3, 54, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (10, 3, 52, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (10, 3, 38, 86);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (10, 3, 35, 92);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (10, 3, 25, 92);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (10, 3, 16, 89);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (10, 3, 9, 94);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (10, 3, 6, 91);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (10, 3, 12, 78);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (10, 3, 19, 74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (10, 3, 7, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (10, 3, 9, 46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (10, 3, 9, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (10, 3, 16, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (10, 3, 23, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (10, 3, 33, 63);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (10, 3, 70, 63);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (10, 3, 81, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (10, 3, 89, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (10, 3, 92, 52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (10, 3, 86, 46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (10, 3, 78, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (10, 3, 69, 52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (10, 3, 62, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (10, 3, 49, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 86, 95);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 82, 95);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 7, 84);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 12, 81);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 11, 74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 8, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 6, 71);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 6, 69);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 9, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 9, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 9, 64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 6, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 5, 61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 5, 92);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 11, 93);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 35, 64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 36, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 38, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 37, 62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 40, 64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 46, 64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 39, 69);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 39, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 39, 84);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 43, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 43, 82);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 41, 78);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 43, 73);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 44, 74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 47, 77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 56, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 56, 94);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 55, 86);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 56, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 48, 81);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 50, 77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 49, 72);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 55, 74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 52, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 57, 61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 58, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 61, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 61, 62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 48, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 48, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 48, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 47, 46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 46, 41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 49, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 24, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 21, 13);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 21, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 17, 6);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 15, 9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 12, 9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 14, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 8, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 6, 9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 7, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 2, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 3, 6);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 6, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 4, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 3, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 4, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 3, 28);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 3, 29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 3, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 3, 36);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 5, 38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 6, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 7, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 38, 38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (11, 6, 39, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (12, 6, 33, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (12, 6, 39, 66);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (12, 6, 48, 64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (12, 6, 55, 82);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (12, 6, 45, 78);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (12, 6, 2, 93);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (12, 6, 13, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (12, 6, 8, 74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (12, 6, 6, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (12, 6, 13, 61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (12, 6, 43, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (12, 6, 39, 41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (12, 6, 20, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (12, 6, 36, 28);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (12, 6, 33, 8);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (12, 6, 9, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (12, 6, 3, 11);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (13, 6, 37, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (13, 6, 38, 24);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (13, 6, 39, 23);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (13, 6, 62, 22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (13, 6, 63, 22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (13, 6, 65, 24);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (13, 6, 63, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (13, 6, 68, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (13, 6, 71, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (13, 6, 62, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (13, 6, 65, 6);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (13, 6, 61, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (13, 6, 86, 6);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (13, 6, 84, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (13, 6, 84, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (13, 6, 86, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (13, 6, 84, 27);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (13, 6, 87, 48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (13, 6, 87, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (13, 6, 83, 64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (13, 6, 84, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (13, 6, 85, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (13, 6, 86, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (13, 6, 87, 64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (14, 6, 5, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (14, 6, 12, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (14, 6, 12, 89);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (14, 6, 10, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (14, 6, 7, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (14, 6, 5, 89);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (14, 6, 25, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (14, 6, 25, 9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (14, 6, 31, 9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (14, 6, 31, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (14, 6, 76, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (14, 6, 75, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (14, 6, 75, 74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (14, 6, 77, 74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (14, 6, 77, 69);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (14, 6, 76, 69);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (14, 6, 75, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (14, 6, 75, 66);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (14, 6, 76, 66);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (14, 6, 76, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (14, 6, 79, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (14, 6, 83, 71);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (14, 6, 84, 71);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (14, 6, 82, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (14, 6, 82, 77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (14, 6, 83, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (14, 6, 84, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (14, 6, 92, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (14, 6, 92, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (14, 6, 91, 69);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (14, 6, 91, 71);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (14, 6, 90, 71);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (14, 6, 90, 73);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (14, 6, 89, 73);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (14, 6, 89, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (14, 6, 90, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (14, 6, 90, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (14, 6, 91, 74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (14, 6, 92, 73);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (15, 6, 28, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (16, 6, 9, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (17, 6, 90, 74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (18, 25, 17, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (18, 25, 23, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (18, 25, 29, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (18, 25, 35, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (18, 25, 42, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (18, 25, 49, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (18, 25, 55, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (18, 25, 60, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (18, 25, 70, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (18, 25, 85, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (18, 25, 90, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (18, 25, 23, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (18, 25, 29, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (18, 25, 35, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (18, 25, 42, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (18, 25, 49, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (18, 25, 55, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (18, 25, 60, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (18, 25, 70, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (18, 25, 85, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (18, 25, 90, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (18, 25, 23, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (18, 25, 29, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (18, 25, 35, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (18, 25, 42, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (18, 25, 49, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (18, 25, 55, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (18, 25, 60, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (18, 25, 70, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (18, 25, 85, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (18, 25, 90, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (18, 25, 23, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (18, 25, 29, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (18, 25, 35, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (18, 25, 42, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (18, 25, 49, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (18, 25, 55, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (18, 25, 60, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (18, 25, 70, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (18, 25, 85, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (18, 25, 90, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (18, 25, 90, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (19, 25, 28, 84);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (19, 25, 8, 62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 6, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 10, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 11, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 11, 44);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 11, 46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 20, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 20, 36);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 15, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 15, 38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 15, 41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 15, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 13, 29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 11, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 11, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 11, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 5, 22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 4, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 2, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 2, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 2, 11);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 2, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 2, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 2, 3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 10, 19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 13, 19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 14, 19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 17, 22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 17, 23);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 46, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 49, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 98, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 10, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 19, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 21, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 21, 22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 21, 28);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 20, 28);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 22, 38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 22, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 34, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 34, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 34, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 34, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 34, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 33, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 27, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 24, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 24, 18);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 24, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 29, 44);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 27, 22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 28, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 45, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 43, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 50, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 50, 32);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 46, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 76, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 73, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 6, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 6, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 6, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 12, 58);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 14, 51);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 14, 52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 22, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 27, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 34, 58);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 37, 58);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 37, 61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 38, 61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 36, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 94, 61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 97, 61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 78, 52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 43, 54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 47, 54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 48, 54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 53, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 64, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 65, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 75, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 85, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 90, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 30, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 30, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 26, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 18, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 19, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 35, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 41, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 51, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 25, 48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 28, 48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 31, 51);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 19, 53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 33, 53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 34, 53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 40, 53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 43, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 43, 52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 44, 52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 45, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 48, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 54, 52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 53, 52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 52, 52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 52, 48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 70, 52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 85, 52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 62, 61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 60, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 59, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (20, 19, 55, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 34, 32);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 32, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 32, 36);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 32, 37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 30, 44);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 24, 44);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 26, 23);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 26, 24);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 28, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 28, 36);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 28, 37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 37, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 45, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 45, 19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 47, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 48, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 46, 38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 42, 46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 51, 46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 52, 46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 59, 46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 65, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 77, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 48, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 47, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 72, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 75, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 81, 37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 81, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 81, 44);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 84, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 99, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 93, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 89, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 91, 44);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 91, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 90, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 84, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 86, 47);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 37, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 45, 63);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 61, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 64, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 70, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 91, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 92, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 93, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 25, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 23, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 34, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 36, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 52, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 68, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 66, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 66, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 73, 61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 80, 61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 81, 61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 82, 61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 95, 61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 96, 61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 98, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 98, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 98, 54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 98, 53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 61, 52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 62, 52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 77, 52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 84, 52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 86, 52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 92, 52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (21, 19, 93, 52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (22, 19, 36, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (23, 19, 91, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 7, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 12, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 14, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 17, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 20, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 25, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 40, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 45, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 48, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 51, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 57, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 75, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 80, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 85, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 90, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 12, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 14, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 17, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 20, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 25, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 40, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 45, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 48, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 51, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 57, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 75, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 80, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 85, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 90, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 12, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 14, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 17, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 20, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 25, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 40, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 45, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 48, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 51, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 57, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 75, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 80, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 85, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 90, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 12, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 14, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 17, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 20, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 25, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 40, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 45, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 48, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 51, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 57, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 75, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 80, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 85, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 90, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 12, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 14, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 17, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 20, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 25, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 40, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 45, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 48, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 51, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 57, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 75, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 80, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 85, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 90, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 12, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 14, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 17, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 20, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 25, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 40, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 45, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 48, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 51, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 57, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 75, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 80, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 85, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 90, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (24, 25, 90, 47);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (25, 25, 16, 9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (25, 25, 90, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (26, 10, 3, 3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (26, 10, 3, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (26, 10, 3, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (26, 10, 17, 3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (26, 10, 17, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (26, 10, 17, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (26, 10, 31, 3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (26, 10, 31, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (26, 10, 31, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (26, 10, 45, 3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (26, 10, 45, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (26, 10, 45, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (26, 10, 59, 3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (26, 10, 59, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (26, 10, 59, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (26, 10, 73, 3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (26, 10, 73, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (26, 10, 73, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (26, 10, 87, 3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (26, 10, 87, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (26, 10, 87, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (26, 10, 97, 3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (26, 10, 97, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (26, 10, 97, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (26, 10, 65, 1);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (26, 10, 79, 2);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (26, 10, 93, 3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (26, 10, 65, 13);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (26, 10, 79, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (26, 10, 93, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (26, 10, 65, 27);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (26, 10, 79, 28);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (26, 10, 93, 29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (26, 10, 65, 41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (26, 10, 79, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (26, 10, 93, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (26, 10, 65, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (26, 10, 79, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (26, 10, 93, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (26, 10, 65, 69);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (26, 10, 79, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (26, 10, 93, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (26, 10, 65, 83);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (26, 10, 79, 82);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (26, 10, 93, 84);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (26, 10, 65, 97);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (26, 10, 79, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (26, 10, 93, 98);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (26, 10, 39, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (26, 10, 48, 37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (26, 10, 51, 44);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (26, 10, 43, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (26, 10, 62, 47);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (26, 10, 61, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (27, 10, 25, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (28, 10, 25, 28);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (27, 10, 17, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (28, 10, 8, 28);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (27, 10, 5, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (28, 10, 4, 41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (27, 10, 15, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (28, 10, 24, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (27, 10, 29, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (28, 10, 33, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (27, 10, 39, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (28, 10, 48, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (27, 10, 54, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (28, 10, 55, 66);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (27, 10, 56, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (28, 10, 59, 82);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (27, 10, 55, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (28, 10, 60, 95);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (27, 10, 48, 92);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (28, 10, 39, 98);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (27, 10, 36, 95);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (28, 10, 28, 93);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (27, 10, 16, 89);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (28, 10, 9, 86);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (27, 10, 6, 81);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (28, 10, 10, 71);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (27, 10, 20, 69);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (28, 10, 22, 77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (27, 10, 29, 82);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (28, 10, 37, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (28, 10, 42, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (28, 10, 48, 62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (28, 10, 24, 58);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (28, 10, 16, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (109, 10, 20, 94);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (29, 18, 83, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (29, 18, 81, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (29, 18, 83, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (29, 18, 79, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (29, 18, 83, 28);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (29, 18, 79, 28);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (29, 18, 83, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (29, 18, 79, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (29, 18, 79, 22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (29, 18, 83, 22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (30, 18, 66, 3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (30, 18, 40, 3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (30, 18, 46, 3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (30, 18, 50, 3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (30, 18, 57, 3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (30, 18, 70, 3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (30, 18, 90, 3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (30, 18, 95, 3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (30, 18, 40, 9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (30, 18, 46, 9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (30, 18, 50, 9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (30, 18, 57, 9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (30, 18, 70, 9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (30, 18, 90, 9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (30, 18, 95, 9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (30, 18, 40, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (30, 18, 46, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (30, 18, 50, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (30, 18, 57, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (30, 18, 70, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (30, 18, 90, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (30, 18, 95, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (30, 18, 40, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (30, 18, 46, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (30, 18, 50, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (30, 18, 57, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (30, 18, 70, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (30, 18, 90, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (30, 18, 95, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (30, 18, 40, 29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (30, 18, 46, 29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (30, 18, 50, 29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (30, 18, 57, 29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (30, 18, 70, 29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (30, 18, 90, 29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (30, 18, 95, 29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (30, 18, 98, 32);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (31, 18, 80, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (31, 18, 82, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (31, 18, 82, 13);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (32, 18, 66, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (32, 18, 73, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (32, 18, 79, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (32, 18, 87, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (32, 18, 95, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (32, 18, 73, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (32, 18, 79, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (32, 18, 87, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (32, 18, 95, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (32, 18, 73, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (32, 18, 79, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (32, 18, 87, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (32, 18, 95, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (32, 18, 73, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (32, 18, 79, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (32, 18, 87, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (32, 18, 95, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (32, 18, 73, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (32, 18, 79, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (32, 18, 87, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (32, 18, 95, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (32, 18, 98, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (33, 18, 82, 11);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (37, 11, 6, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (36, 11, 10, 99);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (36, 11, 10, 97);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (36, 11, 11, 99);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (36, 11, 11, 98);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (36, 11, 16, 93);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (36, 11, 18, 93);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (36, 11, 17, 91);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (36, 11, 18, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (36, 11, 7, 92);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (36, 11, 6, 91);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (36, 11, 4, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (36, 11, 7, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (36, 11, 9, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (36, 11, 12, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (36, 11, 5, 86);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (36, 11, 5, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (36, 11, 8, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (36, 11, 10, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (36, 11, 9, 83);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (36, 11, 6, 83);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (36, 11, 7, 81);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (38, 11, 80, 2);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (38, 11, 80, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (38, 11, 80, 6);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (38, 11, 85, 2);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (38, 11, 85, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (38, 11, 85, 6);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (35, 11, 60, 6);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (35, 11, 64, 6);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (35, 11, 68, 6);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (35, 11, 75, 6);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (35, 11, 79, 6);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (35, 11, 60, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (35, 11, 64, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (35, 11, 68, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (35, 11, 75, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (35, 11, 79, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (35, 11, 60, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (35, 11, 64, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (35, 11, 68, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (35, 11, 75, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (35, 11, 79, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (35, 11, 60, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (35, 11, 64, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (35, 11, 68, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (35, 11, 75, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (35, 11, 79, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (35, 11, 80, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (35, 11, 58, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (35, 11, 65, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (35, 11, 68, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (35, 11, 72, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (35, 11, 76, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (35, 11, 58, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (35, 11, 65, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (35, 11, 68, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (35, 11, 72, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (35, 11, 76, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (35, 11, 58, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (35, 11, 65, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (35, 11, 68, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (35, 11, 72, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (35, 11, 76, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (35, 11, 58, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (35, 11, 65, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (35, 11, 68, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (35, 11, 72, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (35, 11, 76, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (35, 11, 78, 51);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (34, 11, 56, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (34, 11, 59, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (34, 11, 64, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (34, 11, 69, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (34, 11, 75, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (34, 11, 56, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (34, 11, 59, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (34, 11, 64, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (34, 11, 69, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (34, 11, 75, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (34, 11, 56, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (34, 11, 59, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (34, 11, 64, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (34, 11, 69, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (34, 11, 75, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (34, 11, 56, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (34, 11, 59, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (34, 11, 64, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (34, 11, 69, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (34, 11, 75, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (34, 11, 56, 94);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (34, 11, 59, 94);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (34, 11, 64, 94);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (34, 11, 69, 94);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (34, 11, 75, 94);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (34, 11, 77, 95);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 20, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 25, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 30, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 35, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 40, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 45, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 50, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 55, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 60, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 65, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 80, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 85, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 90, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 92, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 97, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 20, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 25, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 30, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 35, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 40, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 45, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 50, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 55, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 60, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 65, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 80, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 85, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 90, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 92, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 97, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 20, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 25, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 30, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 35, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 40, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 45, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 50, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 55, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 60, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 65, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 80, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 85, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 90, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 92, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 97, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 20, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 25, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 30, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 35, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 40, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 45, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 50, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 55, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 60, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 65, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 80, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 85, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 90, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 92, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 97, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 20, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 25, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 30, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 35, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 40, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 45, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 50, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 55, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 60, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 65, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 80, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 85, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 90, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 92, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 97, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 20, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 25, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 30, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 35, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 40, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 45, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 50, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 55, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 60, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 65, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 80, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 85, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 90, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 92, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 97, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 20, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 25, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 30, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 35, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 40, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 45, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 50, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 55, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 60, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 65, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 80, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 85, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 90, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 92, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 97, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 20, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 25, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 30, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 35, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 40, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 45, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 50, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 55, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 60, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 65, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 80, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 85, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 90, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 92, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 97, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 99, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 2, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 10, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 20, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 30, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 40, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 50, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 60, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 65, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 2, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 10, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 20, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 30, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 40, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 50, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 60, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 65, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 2, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 10, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 20, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 30, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 40, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 50, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 60, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 65, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 2, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 10, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 20, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 30, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 40, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 50, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 60, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (49, 12, 65, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (39, 12, 67, 98);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 33, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 42, 9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 57, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 63, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 73, 8);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 79, 3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 88, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 93, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 83, 11);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 86, 18);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 96, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 92, 24);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 81, 24);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 87, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 95, 32);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 95, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 87, 37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 81, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 95, 46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 86, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 95, 52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 86, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 95, 58);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 98, 63);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 90, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 94, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 88, 82);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 97, 86);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 85, 93);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 79, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 72, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 64, 84);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 64, 78);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 64, 72);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 81, 66);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 71, 63);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 66, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 76, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 74, 48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 64, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 67, 41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 76, 37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 66, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 72, 29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 66, 22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 64, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 73, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 60, 62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 50, 91);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 55, 84);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 49, 77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 54, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 49, 61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 55, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 54, 47);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 47, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 55, 32);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 54, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 55, 18);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 41, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 44, 23);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 41, 32);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 33, 37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 37, 44);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 45, 52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 35, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 43, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 34, 63);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 31, 71);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 25, 66);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 26, 58);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 26, 79);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 38, 78);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 25, 51);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 27, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 24, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 31, 29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 35, 22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 26, 19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 26, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 17, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 20, 28);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 18, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 18, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 17, 54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 17, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 15, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 17, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 15, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 7, 79);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 7, 72);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 7, 66);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 7, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 10, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 10, 36);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 10, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 10, 24);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 10, 18);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (50, 27, 10, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 34, 8);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 43, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 58, 11);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 64, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 74, 9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 80, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 89, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 94, 11);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 84, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 87, 19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 97, 18);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 93, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 82, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 88, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 96, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 96, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 88, 38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 82, 44);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 96, 47);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 87, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 96, 53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 87, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 96, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 99, 64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 91, 71);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 95, 77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 89, 83);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 98, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 86, 94);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 80, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 73, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 65, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 65, 79);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 65, 73);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 82, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 72, 64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 67, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 77, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 75, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 65, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 68, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 77, 38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 67, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 73, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 67, 23);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 65, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 74, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 61, 63);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 51, 92);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 56, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 50, 78);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 55, 69);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 50, 62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 56, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 55, 48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 48, 41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 56, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 55, 26);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 56, 19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 42, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 45, 24);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 42, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 34, 38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 38, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 46, 53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 36, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 44, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 35, 64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 32, 72);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 26, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 27, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 27, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 39, 79);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 26, 52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 28, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 25, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 32, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 36, 23);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 27, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 27, 13);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 18, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 21, 29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 19, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 19, 46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 18, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 18, 61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 16, 69);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 18, 77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 16, 86);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 8, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 8, 73);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 8, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 8, 61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 11, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 11, 37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 11, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 11, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 11, 19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (51, 27, 11, 13);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 32, 8);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 41, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 56, 11);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 62, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 72, 9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 78, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 87, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 92, 11);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 82, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 85, 19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 95, 18);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 91, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 80, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 86, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 94, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 94, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 86, 38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 80, 44);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 94, 47);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 85, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 94, 53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 85, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 94, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 97, 64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 89, 71);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 93, 77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 87, 83);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 96, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 84, 94);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 78, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 71, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 63, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 63, 79);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 63, 73);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 80, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 70, 64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 65, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 75, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 73, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 63, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 66, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 75, 38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 65, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 71, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 65, 23);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 63, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 72, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 59, 63);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 49, 92);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 54, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 48, 78);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 53, 69);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 48, 62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 54, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 53, 48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 46, 41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 54, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 53, 26);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 54, 19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 40, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 43, 24);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 40, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 32, 38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 36, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 44, 53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 34, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 42, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 33, 64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 30, 72);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 24, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 25, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 25, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 37, 79);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 24, 52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 26, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 23, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 30, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 34, 23);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 25, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 25, 13);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 16, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 19, 29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 17, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 17, 46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 16, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 16, 61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 14, 69);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 16, 77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 14, 86);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 6, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 6, 73);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 6, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 6, 61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 9, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 9, 37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 9, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 9, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 9, 19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (52, 27, 9, 13);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (53, 27, 76, 83);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (54, 12, 84, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (55, 12, 85, 86);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (55, 12, 86, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (55, 12, 85, 84);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (55, 12, 86, 83);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (55, 12, 89, 86);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (55, 12, 89, 84);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (55, 12, 92, 83);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (55, 12, 94, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (55, 12, 95, 82);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (55, 12, 99, 82);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (55, 12, 98, 83);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (55, 12, 98, 86);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 34, 3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 35, 8);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 36, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 38, 8);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 37, 6);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 41, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 47, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 47, 8);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 44, 8);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 45, 6);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 48, 3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 13, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 15, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 17, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 17, 11);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 2, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 3, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 8, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 9, 13);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 10, 13);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 11, 13);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 12, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 3, 19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 4, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 5, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 6, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 14, 19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 17, 19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 6, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 12, 24);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 13, 24);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 11, 29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 13, 29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 14, 29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 19, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 20, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 22, 27);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 22, 28);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 29, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 30, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 30, 27);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 28, 32);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 30, 32);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 29, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 28, 38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 30, 38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 29, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 24, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 26, 54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 29, 44);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 5, 41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 6, 41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 11, 41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 5, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 6, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 16, 46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 17, 47);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 5, 47);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 9, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 11, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 16, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 4, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 16, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 8, 61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 12, 61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 3, 74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 10, 74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 4, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 9, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 11, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 16, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 17, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 17, 78);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 6, 81);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 12, 82);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 16, 84);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 16, 89);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 14, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 5, 89);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 4, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 3, 91);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 15, 91);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 16, 92);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 4, 92);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 8, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 9, 97);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 22, 93);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 23, 95);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (56, 9, 21, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 24, 97);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 25, 94);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 33, 92);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 34, 91);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 37, 95);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 40, 97);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 28, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 29, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 30, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 29, 86);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 36, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 37, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 39, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 32, 79);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 37, 79);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 40, 74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 34, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 35, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 31, 69);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 31, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 31, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 39, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 38, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 35, 62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 33, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 43, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 44, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 48, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 50, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 52, 66);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 54, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 54, 64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 52, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 53, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 55, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 58, 72);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 61, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 64, 78);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 63, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 66, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 67, 71);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 71, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 81, 82);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 82, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 76, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 77, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 83, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 90, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 91, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 92, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 97, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 85, 98);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 91, 98);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 99, 98);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 95, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 93, 92);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 89, 94);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 94, 81);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 98, 81);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 98, 77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 94, 77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 94, 73);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 98, 73);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 98, 69);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 94, 69);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 95, 63);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 88, 63);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 85, 63);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 84, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 85, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 91, 54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 97, 54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 79, 51);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 78, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 79, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 85, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 91, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 92, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 97, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 98, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 69, 46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 66, 44);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 63, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 74, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 71, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 69, 37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 77, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 78, 32);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 94, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 96, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 95, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 82, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 83, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 83, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 97, 36);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 98, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 94, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 88, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 89, 36);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 61, 36);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 61, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 61, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 61, 32);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 61, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 61, 29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 63, 22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 97, 22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 98, 22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 98, 28);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 97, 28);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 91, 28);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 86, 28);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 92, 24);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 87, 24);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 81, 24);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 68, 28);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 72, 28);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 71, 29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 80, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 84, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 77, 9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 87, 9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 75, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 89, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 77, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 87, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 84, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (57, 9, 80, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (58, 9, 41, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (59, 9, 92, 93);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (60, 9, 82, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 91, 91);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 91, 89);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 91, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 96, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 96, 89);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 96, 91);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 92, 83);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 95, 83);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 95, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 92, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 93, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 92, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 94, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 85, 77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 82, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 81, 83);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 81, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 85, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 71, 86);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 72, 86);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 72, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 67, 78);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 66, 77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 67, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 58, 79);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 57, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 59, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 50, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 50, 89);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 49, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 31, 89);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 38, 89);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 31, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 38, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 18, 89);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 18, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 16, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 9, 89);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 7, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 5, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 4, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 9, 84);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 13, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 15, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 21, 82);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 5, 79);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 7, 79);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 6, 78);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 13, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 13, 73);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 12, 74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 23, 72);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 25, 74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 25, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 30, 77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 28, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 36, 81);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 37, 82);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 43, 82);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 43, 83);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 45, 78);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 44, 77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 47, 74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 49, 74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 50, 74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 45, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 47, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 46, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 48, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 47, 58);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 54, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 51, 54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 59, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 61, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 67, 63);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 64, 63);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 60, 51);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 56, 51);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 56, 47);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 60, 47);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 61, 41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 62, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 62, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 70, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 72, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 81, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 89, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 97, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 71, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 80, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 90, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 97, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 82, 44);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 90, 44);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 96, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 95, 69);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 96, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 97, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 95, 62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 98, 62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 97, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 98, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 96, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 97, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 95, 37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 98, 37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 96, 32);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 95, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 97, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 94, 26);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 99, 26);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 98, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 99, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 95, 18);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 94, 19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 94, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 93, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 82, 22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 82, 19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 86, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 87, 18);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 85, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 55, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 56, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 56, 32);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 61, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 65, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 63, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 64, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 64, 27);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 64, 26);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 64, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 56, 27);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 59, 27);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 58, 26);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 57, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 60, 18);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 61, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 62, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 64, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 64, 6);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 66, 6);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 65, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 59, 11);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 59, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 59, 9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 53, 6);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 53, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 46, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 45, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 44, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 44, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 54, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 55, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 44, 11);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 47, 11);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 47, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 45, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 53, 13);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 53, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 53, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 52, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 52, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 42, 22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 45, 22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 44, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 47, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 43, 27);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 44, 28);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 46, 28);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 49, 28);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 45, 29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 48, 29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 44, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 43, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 49, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 42, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 50, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 49, 36);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 43, 41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 50, 41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 45, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 47, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 49, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 44, 44);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 45, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 51, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 50, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 44, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 39, 46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 38, 46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 35, 46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 33, 46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 31, 46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 35, 47);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 35, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 35, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 35, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 33, 48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 32, 48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 33, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 32, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 32, 58);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 26, 58);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 25, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 26, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 27, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 22, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 6, 58);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 5, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 5, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 12, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 16, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 16, 52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 16, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 16, 46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 19, 41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 19, 37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 19, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 19, 29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 19, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 12, 52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 12, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 12, 46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 9, 41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 9, 37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 9, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 9, 29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 9, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 16, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 16, 19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 16, 18);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 15, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 9, 19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 8, 19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (74, 13, 9, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (70, 13, 17, 9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (70, 13, 11, 9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (70, 13, 12, 11);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (70, 13, 14, 11);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (70, 13, 16, 11);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (72, 13, 13, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (73, 13, 15, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (71, 13, 90, 8);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (69, 28, 93, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (69, 28, 93, 53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (63, 28, 11, 46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (63, 28, 19, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (63, 28, 21, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (63, 28, 25, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (63, 28, 17, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (63, 28, 31, 38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (63, 28, 33, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (63, 28, 37, 36);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (63, 28, 41, 38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (64, 28, 56, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (64, 28, 55, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (64, 28, 55, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (64, 28, 56, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (64, 28, 41, 66);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (64, 28, 41, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (64, 28, 41, 64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (64, 28, 41, 63);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (64, 28, 40, 66);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (64, 28, 40, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (64, 28, 40, 64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (64, 28, 40, 63);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (64, 28, 37, 69);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (64, 28, 39, 69);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (64, 28, 39, 71);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (64, 28, 40, 71);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (64, 28, 42, 73);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (64, 28, 37, 69);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (64, 28, 39, 69);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (64, 28, 57, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (64, 28, 58, 72);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (64, 28, 57, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (64, 28, 54, 77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (64, 28, 51, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (64, 28, 60, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (64, 28, 60, 18);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (64, 28, 57, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (64, 28, 57, 18);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (64, 28, 58, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (64, 28, 54, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (61, 28, 36, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (61, 28, 28, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (61, 28, 25, 61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (61, 28, 25, 72);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (61, 28, 29, 36);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (61, 28, 25, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (61, 28, 48, 36);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (61, 28, 47, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (61, 28, 51, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (61, 28, 51, 37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (61, 28, 53, 36);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (61, 28, 55, 36);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (61, 28, 45, 48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (61, 28, 34, 47);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (61, 28, 27, 52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (61, 28, 20, 58);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (61, 28, 11, 58);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (61, 28, 12, 61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (61, 28, 11, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (61, 28, 9, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (61, 28, 13, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (62, 28, 56, 9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (62, 28, 56, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (62, 28, 57, 9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (62, 28, 57, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (62, 28, 58, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (62, 28, 59, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (62, 28, 58, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (62, 28, 59, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (62, 28, 49, 69);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (62, 28, 52, 66);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (62, 28, 57, 63);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (62, 28, 55, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (62, 28, 58, 54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (62, 28, 49, 53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (62, 28, 41, 54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (62, 28, 39, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (62, 28, 26, 62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (62, 28, 24, 63);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (62, 28, 25, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (62, 28, 22, 74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (62, 28, 54, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (62, 28, 55, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (62, 28, 56, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (62, 28, 48, 47);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (62, 28, 42, 48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (62, 28, 38, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (62, 28, 22, 54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (62, 28, 16, 58);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (62, 28, 12, 66);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (62, 28, 10, 66);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (62, 28, 11, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (62, 28, 15, 77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (62, 28, 19, 77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (62, 28, 19, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (67, 28, 38, 72);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (68, 28, 37, 72);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (66, 28, 37, 73);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (65, 28, 56, 2);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (40, 7, 9, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (40, 7, 9, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (40, 7, 26, 52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (40, 7, 28, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (40, 7, 35, 54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (40, 7, 34, 47);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (40, 7, 14, 73);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (40, 7, 17, 69);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (40, 7, 54, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (40, 7, 44, 73);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (41, 7, 35, 52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (41, 7, 29, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (41, 7, 34, 26);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (41, 7, 31, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (41, 7, 24, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (41, 7, 28, 29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (41, 7, 15, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (41, 7, 8, 29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (41, 7, 11, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (41, 7, 6, 36);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (41, 7, 2, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (41, 7, 2, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (41, 7, 3, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (41, 7, 14, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (41, 7, 11, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (41, 7, 21, 71);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (41, 7, 19, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (41, 7, 44, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (41, 7, 40, 78);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (41, 7, 36, 78);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (41, 7, 36, 74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (41, 7, 39, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (41, 7, 43, 69);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (41, 7, 42, 66);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (41, 7, 47, 78);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (42, 7, 13, 24);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (42, 7, 14, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (42, 7, 20, 32);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (42, 7, 19, 27);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (42, 7, 24, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (42, 7, 27, 19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (42, 7, 38, 23);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (42, 7, 70, 82);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (42, 7, 75, 82);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (42, 7, 52, 78);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (42, 7, 52, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (42, 7, 54, 72);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (42, 7, 52, 69);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (42, 7, 51, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (42, 7, 47, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (42, 7, 78, 97);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (42, 7, 75, 95);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (42, 7, 75, 91);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (42, 7, 69, 94);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (42, 7, 68, 91);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (42, 7, 68, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (42, 7, 68, 84);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (42, 7, 74, 86);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (43, 7, 34, 23);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (43, 7, 6, 26);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (43, 7, 2, 29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (43, 7, 9, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (43, 7, 38, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (43, 7, 50, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (43, 7, 49, 72);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (43, 7, 97, 95);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (43, 7, 92, 94);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (43, 7, 92, 97);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (43, 7, 87, 97);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (43, 7, 87, 92);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (43, 7, 82, 91);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (43, 7, 83, 97);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (43, 7, 79, 93);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (44, 7, 73, 48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (44, 7, 74, 53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (44, 7, 71, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (44, 7, 71, 62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (44, 7, 76, 62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (44, 7, 78, 58);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (44, 7, 79, 53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (44, 7, 79, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (44, 7, 84, 48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (44, 7, 84, 53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (44, 7, 84, 58);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (44, 7, 88, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (44, 7, 88, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (44, 7, 87, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (44, 7, 89, 51);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (44, 7, 91, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (44, 7, 94, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (44, 7, 94, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (44, 7, 91, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (44, 7, 94, 86);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (44, 7, 97, 86);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (44, 7, 97, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (44, 7, 93, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (45, 7, 49, 3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (45, 7, 51, 3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (45, 7, 54, 3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (45, 7, 57, 3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (45, 7, 61, 3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (45, 7, 49, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (45, 7, 51, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (45, 7, 55, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (45, 7, 58, 6);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (45, 7, 61, 8);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (45, 7, 55, 9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (45, 7, 55, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (45, 7, 58, 13);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (45, 7, 59, 11);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (46, 7, 95, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (47, 7, 62, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (48, 7, 93, 53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 6, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 6, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 6, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 6, 8);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 7, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 9, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 11, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 13, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 14, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 17, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 20, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 26, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 37, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 39, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 36, 11);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 34, 9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 24, 24);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 22, 24);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 16, 24);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 14, 24);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 12, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 18, 38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 15, 38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 16, 41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 17, 41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 8, 41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 7, 38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 6, 38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 5, 41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 3, 47);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 3, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 8, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 8, 47);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 15, 47);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 15, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 20, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 20, 47);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 17, 58);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 16, 58);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 15, 58);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 20, 63);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 13, 63);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 15, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 18, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 18, 71);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 15, 71);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 15, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 18, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 7, 82);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 3, 82);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 3, 77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 7, 77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 7, 72);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 3, 72);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 3, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 7, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 7, 62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 3, 62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 3, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 7, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 13, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 11, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 4, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 11, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 14, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 18, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 23, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 26, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 29, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 23, 73);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 26, 73);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 23, 81);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 21, 81);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 28, 83);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 26, 86);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 33, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 36, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 62, 95);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 64, 95);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 69, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 71, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 73, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 75, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 87, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 89, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 93, 93);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 90, 83);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 95, 83);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 95, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 90, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 90, 77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 95, 77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 95, 74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 90, 74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 81, 73);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 78, 73);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 83, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 87, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 92, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 96, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 84, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 84, 47);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 89, 47);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 89, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 94, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 94, 47);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 95, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 92, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 84, 41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 81, 41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 81, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 84, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 82, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 76, 27);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 87, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 89, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 93, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 95, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 95, 29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 97, 29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 91, 29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 89, 29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 87, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 87, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 87, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 87, 8);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 93, 8);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 93, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 93, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 93, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 76, 6);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 74, 6);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 65, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (75, 29, 63, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (76, 29, 57, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (76, 29, 77, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (76, 29, 90, 11);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (76, 29, 97, 26);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (76, 29, 88, 41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (76, 29, 89, 54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (76, 29, 93, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (76, 29, 89, 93);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (76, 29, 22, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (76, 29, 9, 95);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (76, 29, 5, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (76, 29, 20, 53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (76, 29, 18, 64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (76, 29, 16, 64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (76, 29, 11, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (76, 29, 18, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (76, 29, 28, 24);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (76, 29, 35, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (76, 29, 45, 6);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (77, 29, 80, 28);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (77, 29, 94, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (77, 29, 94, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (77, 29, 65, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (77, 29, 2, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (77, 29, 4, 53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (78, 29, 82, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (78, 29, 77, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (78, 29, 14, 81);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (78, 29, 16, 54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (78, 29, 17, 66);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (78, 29, 10, 36);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (79, 29, 92, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (80, 29, 6, 93);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (81, 29, 89, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (82, 29, 91, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (83, 29, 8, 9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (84, 36, 38, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (85, 36, 33, 38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (86, 36, 34, 38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (87, 36, 35, 38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (88, 36, 36, 38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (89, 36, 40, 38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (90, 36, 41, 38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (91, 36, 42, 38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (92, 36, 33, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (93, 36, 34, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (94, 36, 35, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (95, 36, 36, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (96, 36, 33, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (97, 36, 34, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (98, 36, 7, 66);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (99, 36, 35, 94);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (100, 36, 84, 6);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (101, 36, 4, 92);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (102, 36, 7, 92);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (103, 36, 9, 92);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (104, 36, 5, 37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (105, 36, 8, 37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (106, 36, 56, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (107, 36, 57, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (108, 36, 59, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (114, 17, 8, 82);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (114, 17, 33, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (114, 17, 25, 82);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (114, 17, 45, 92);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (114, 17, 45, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (114, 17, 53, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (114, 17, 51, 77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (114, 17, 61, 93);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (114, 17, 78, 74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (114, 17, 88, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (114, 17, 23, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (114, 17, 27, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (114, 17, 28, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (114, 17, 26, 27);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (114, 17, 29, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (114, 17, 31, 28);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (114, 17, 10, 51);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (114, 17, 49, 62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (114, 17, 51, 62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (114, 17, 41, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (114, 17, 39, 22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (114, 17, 38, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (114, 17, 48, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (114, 17, 47, 13);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (114, 17, 52, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (114, 17, 53, 13);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (114, 17, 95, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (114, 17, 62, 23);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (114, 17, 60, 23);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (114, 17, 86, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (114, 17, 89, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (113, 17, 44, 95);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (113, 17, 39, 94);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (113, 17, 57, 94);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (113, 17, 60, 98);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (113, 17, 68, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (113, 17, 75, 98);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (113, 17, 76, 93);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (113, 17, 73, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (113, 17, 37, 99);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (113, 17, 27, 99);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (113, 17, 19, 99);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (113, 17, 12, 98);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (113, 17, 22, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (113, 17, 28, 93);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (113, 17, 19, 91);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (113, 17, 13, 91);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (113, 17, 1, 79);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (113, 17, 5, 81);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (113, 17, 20, 77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (113, 17, 27, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (113, 17, 41, 81);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (113, 17, 48, 83);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (113, 17, 42, 86);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (113, 17, 61, 86);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (113, 17, 85, 71);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (113, 17, 71, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (113, 17, 80, 77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (113, 17, 31, 63);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (113, 17, 42, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (113, 17, 42, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (113, 17, 46, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (113, 17, 54, 58);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (113, 17, 48, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (113, 17, 52, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (113, 17, 48, 44);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (113, 17, 52, 44);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (113, 17, 62, 54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (113, 17, 60, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (113, 17, 4, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (113, 17, 9, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (113, 17, 38, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (113, 17, 33, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (113, 17, 37, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (113, 17, 41, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (113, 17, 33, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (113, 17, 37, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (113, 17, 41, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (113, 17, 65, 66);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (113, 17, 77, 63);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (113, 17, 85, 63);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (113, 17, 92, 91);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (113, 17, 83, 94);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (113, 17, 97, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (113, 17, 97, 83);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (113, 17, 63, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 5, 86);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 50, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 45, 78);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 39, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 86, 89);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 84, 95);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 91, 95);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 94, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 87, 82);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 71, 74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 73, 77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 84, 74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 31, 83);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 19, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 19, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 26, 74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 32, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 35, 69);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 15, 71);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 10, 71);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 10, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 3, 71);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 5, 61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 13, 62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 21, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 31, 62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 37, 64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 17, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 26, 54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 35, 53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 38, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 25, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 18, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 2, 48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 2, 53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 5, 32);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 13, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 48, 37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 52, 37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 48, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 52, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 48, 22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 52, 22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 49, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 46, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 54, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 54, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 62, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 67, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 8, 22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 15, 22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 19, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 20, 27);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 22, 26);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 23, 19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 15, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 22, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 33, 18);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 60, 61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 98, 71);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 92, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 76, 58);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 66, 38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 72, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 79, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 84, 38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 96, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 84, 54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 87, 58);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 93, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 98, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 95, 53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 85, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 80, 26);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 69, 28);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 75, 24);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 86, 24);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 94, 24);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 79, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 92, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 90, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 85, 8);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 80, 8);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 72, 6);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (112, 17, 71, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (110, 17, 99, 82);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (111, 17, 50, 13);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 97, 94);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 94, 94);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 94, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 90, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 90, 83);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 90, 77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 90, 71);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 91, 69);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 91, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 93, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 93, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 95, 69);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 95, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 93, 66);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 95, 64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 89, 61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 88, 61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 82, 61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 80, 62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 80, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 78, 53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 84, 52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 84, 54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 89, 52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 90, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 92, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 90, 47);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 78, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 78, 69);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 41, 72);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 42, 71);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 41, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 44, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 52, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 59, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 65, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 71, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 85, 92);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 81, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 77, 91);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 71, 89);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 71, 79);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 67, 77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 59, 77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 57, 82);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 49, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 47, 83);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 46, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 47, 91);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 49, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 56, 97);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 62, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 68, 98);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 68, 95);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 70, 95);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 41, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 32, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 24, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 14, 95);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 12, 94);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 9, 93);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 12, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 15, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 15, 91);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 18, 91);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 17, 94);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 20, 89);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 8, 84);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 9, 84);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 9, 78);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (115, 2, 8, 78);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (117, 2, 95, 9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (117, 2, 92, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (117, 2, 88, 9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (117, 2, 92, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (117, 2, 80, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (117, 2, 80, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (117, 2, 80, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (117, 2, 82, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (117, 2, 77, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (117, 2, 74, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (117, 2, 71, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (117, 2, 68, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (117, 2, 64, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (117, 2, 64, 11);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (117, 2, 67, 11);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (117, 2, 67, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (117, 2, 74, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (117, 2, 64, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (117, 2, 65, 23);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (117, 2, 64, 26);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (117, 2, 64, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (117, 2, 65, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (117, 2, 68, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (117, 2, 70, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (117, 2, 72, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (117, 2, 76, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (117, 2, 76, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (117, 2, 71, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (117, 2, 69, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (117, 2, 67, 26);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (117, 2, 70, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (117, 2, 73, 26);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (117, 2, 73, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (117, 2, 70, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (117, 2, 67, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (116, 2, 89, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (118, 2, 70, 28);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (119, 1, 56, 52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (119, 8, 3, 46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (120, 3, 46, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (121, 35, 76, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (122, 2, 95, 95);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (133, 32, 32, 51);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (133, 32, 31, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (133, 32, 30, 58);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (133, 32, 29, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (133, 32, 28, 58);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (133, 32, 27, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (133, 32, 28, 63);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (133, 32, 22, 63);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (133, 32, 25, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (133, 32, 14, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (133, 32, 18, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (133, 32, 13, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (133, 32, 8, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (133, 32, 9, 69);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (133, 32, 10, 78);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (133, 32, 17, 72);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (133, 32, 20, 78);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (133, 32, 20, 82);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (133, 32, 8, 82);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (133, 32, 12, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (133, 32, 10, 93);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (133, 32, 4, 93);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (133, 32, 16, 98);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (133, 32, 19, 98);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (133, 32, 27, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (133, 32, 31, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (133, 32, 30, 84);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (133, 32, 36, 78);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (133, 32, 28, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (133, 32, 33, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (133, 32, 22, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (133, 32, 40, 93);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (133, 32, 47, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (133, 32, 50, 99);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (133, 32, 53, 95);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (133, 32, 64, 97);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (133, 32, 62, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (133, 32, 54, 86);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (133, 32, 44, 84);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (133, 32, 48, 79);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (133, 32, 39, 74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (133, 32, 54, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (133, 32, 61, 74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (133, 32, 67, 83);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (133, 32, 71, 89);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (133, 32, 79, 84);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (133, 32, 87, 84);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (133, 32, 94, 91);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (133, 32, 94, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (133, 32, 94, 98);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (133, 32, 96, 95);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (133, 32, 98, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (133, 32, 99, 98);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (137, 32, 96, 99);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 7, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 14, 19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 20, 19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 27, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 33, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 25, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 19, 13);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 17, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 15, 13);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 10, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 17, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 31, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 33, 3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 31, 1);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 47, 2);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 41, 8);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 51, 8);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 53, 8);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 60, 3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 67, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 67, 1);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 76, 3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 78, 8);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 86, 6);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 85, 3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 95, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 97, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 87, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 79, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 77, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 70, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 67, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 67, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 64, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 67, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 85, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 91, 26);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 93, 26);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 96, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 91, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 89, 38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 93, 44);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 93, 52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 87, 47);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 92, 52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 89, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 93, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 92, 64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 79, 64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 76, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 68, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 65, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 63, 54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 53, 58);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 52, 53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 52, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 60, 47);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 62, 47);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 74, 52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 76, 46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 77, 46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 77, 24);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 68, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 67, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 66, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 65, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 65, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 66, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 55, 37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 58, 37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 65, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 70, 29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 68, 29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 58, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 53, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 49, 23);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 57, 23);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 57, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 57, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 57, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 49, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (134, 32, 45, 18);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (132, 32, 17, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (135, 14, 76, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (136, 8, 32, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (123, 24, 96, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (123, 24, 97, 83);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (123, 24, 82, 81);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (123, 24, 78, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (123, 24, 81, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (123, 24, 78, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (123, 24, 73, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (123, 24, 70, 95);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (123, 24, 68, 86);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (123, 24, 73, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (123, 24, 69, 81);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (123, 24, 35, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (123, 24, 33, 83);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (123, 24, 28, 86);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (123, 24, 28, 92);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (123, 24, 33, 91);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (123, 24, 19, 95);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (123, 24, 23, 92);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (123, 24, 23, 86);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (123, 24, 25, 81);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (123, 24, 19, 81);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (123, 24, 5, 81);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (123, 24, 5, 89);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (123, 24, 52, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (123, 24, 49, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (123, 24, 49, 69);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (123, 24, 52, 69);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (123, 24, 56, 66);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (123, 24, 54, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (123, 24, 55, 63);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (123, 24, 51, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (123, 24, 50, 66);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (123, 24, 47, 66);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (123, 24, 44, 66);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (123, 24, 45, 64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (123, 24, 49, 61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (123, 24, 52, 61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (125, 24, 54, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (125, 24, 54, 84);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (125, 24, 47, 84);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (125, 24, 47, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (140, 24, 51, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (138, 24, 76, 93);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (138, 24, 26, 94);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (124, 24, 4, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (124, 24, 7, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (124, 24, 3, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (124, 24, 5, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (124, 24, 11, 64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (124, 24, 14, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (124, 24, 21, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (124, 24, 21, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (124, 24, 14, 58);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (124, 24, 12, 53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (124, 24, 20, 53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (124, 24, 13, 48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (124, 24, 13, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (124, 24, 20, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (124, 24, 25, 41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (124, 24, 31, 41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (124, 24, 31, 53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (124, 24, 33, 51);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (124, 24, 97, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (124, 24, 99, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (124, 24, 97, 64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (124, 24, 94, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (124, 24, 93, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (124, 24, 88, 62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (124, 24, 84, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (124, 24, 84, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (124, 24, 79, 64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (124, 24, 80, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (124, 24, 84, 58);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (124, 24, 81, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (124, 24, 76, 53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (124, 24, 80, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (124, 24, 81, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (124, 24, 86, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (124, 24, 89, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (124, 24, 76, 41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (124, 24, 70, 41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (143, 24, 89, 54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (143, 24, 88, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (143, 24, 5, 48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (143, 24, 11, 61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (143, 24, 23, 64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (143, 24, 16, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (143, 24, 74, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (139, 24, 29, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (126, 24, 72, 46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (141, 24, 87, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (142, 24, 12, 19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 45, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 45, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 40, 54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 36, 51);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 38, 46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 39, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 36, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 30, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 27, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 36, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 33, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 25, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 25, 23);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 21, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 17, 23);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 17, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 21, 28);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 29, 18);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 29, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 33, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 37, 18);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 38, 9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 33, 9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 33, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 38, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 26, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 26, 1);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 20, 3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 14, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 8, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 4, 6);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 4, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 10, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 10, 19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 4, 19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 6, 26);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 10, 26);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 8, 28);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 14, 19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 12, 32);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 19, 36);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 14, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 12, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 8, 36);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 4, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 2, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 4, 32);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 9, 47);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 4, 47);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 4, 54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 9, 54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 16, 46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 22, 46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 27, 46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 33, 46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 32, 54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 24, 54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 25, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 18, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 15, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 15, 61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 7, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 6, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 6, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 7, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 24, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 25, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 32, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 40, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 36, 62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 32, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 36, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 40, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 41, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 32, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 26, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 22, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 18, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 12, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 5, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 10, 71);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 12, 71);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 9, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 3, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 9, 81);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 4, 86);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 13, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 16, 91);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 21, 89);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 25, 91);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 29, 89);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 33, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 35, 92);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 37, 95);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 41, 95);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 39, 92);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 37, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 39, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 35, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 37, 86);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 38, 82);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 36, 82);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 33, 78);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 41, 78);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 38, 74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 36, 74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 9, 91);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 4, 92);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 9, 95);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 4, 97);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 13, 93);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 13, 99);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 16, 95);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 23, 95);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 25, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 23, 99);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 29, 99);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 27, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (145, 33, 29, 95);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (147, 33, 25, 94);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 58, 48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 63, 48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 63, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 66, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 66, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 68, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 63, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 63, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 60, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 73, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 73, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 68, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 63, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 63, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 68, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 73, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 73, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 68, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 63, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 63, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 68, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 73, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 63, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 63, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 68, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 73, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 78, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 78, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 78, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 78, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 78, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 78, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 78, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 83, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 83, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 83, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 83, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 83, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 83, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 83, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 88, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 88, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 88, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 88, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 88, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 88, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 88, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 88, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 93, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 93, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 93, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 93, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 93, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 93, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 93, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 98, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 98, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 98, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 98, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 98, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 98, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 98, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 98, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 98, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 93, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 88, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 88, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 93, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 98, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 98, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 93, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 88, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 83, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 78, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 73, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 68, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 63, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 63, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 68, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 63, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 63, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 63, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 63, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 63, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 63, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 63, 95);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 63, 99);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 68, 99);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 68, 95);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 68, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 68, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 68, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 68, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 68, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 68, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 73, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 73, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 73, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 73, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 73, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 78, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 78, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 78, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 78, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 78, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 83, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 83, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 83, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 83, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 83, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 88, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 93, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 98, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 98, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 98, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 98, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 98, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 93, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 93, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 88, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 88, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 88, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 93, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 93, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 88, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 88, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 93, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 98, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 98, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 93, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 88, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 88, 95);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 93, 95);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 98, 95);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 98, 99);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 93, 99);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 88, 99);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 83, 99);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 83, 95);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 78, 95);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 78, 99);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 73, 99);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (146, 33, 73, 95);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (144, 33, 81, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (148, 36, 14, 6);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (149, 36, 2, 41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (150, 36, 24, 6);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 52, 44);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 57, 44);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 59, 38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 57, 32);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 50, 36);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 45, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 38, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 37, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 30, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 25, 48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 18, 48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 21, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 12, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 7, 37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 17, 37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 25, 37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 6, 28);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 14, 24);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 17, 27);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 20, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 30, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 37, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 38, 26);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 33, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 33, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 27, 23);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 8, 18);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 22, 22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 25, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 31, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 31, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 19, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 13, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 16, 1);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 7, 8);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 42, 6);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 40, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 47, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 46, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 53, 28);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 63, 27);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 64, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 70, 28);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 70, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 67, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 76, 46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 77, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 81, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 81, 36);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 86, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 88, 46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 96, 46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 94, 38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 90, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 74, 23);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 80, 26);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 82, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 90, 22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 89, 27);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 97, 27);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 93, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 91, 13);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 90, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 94, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 92, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 93, 3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 94, 3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 75, 9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 74, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 92, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 66, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 56, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 52, 11);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 56, 6);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 60, 11);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 66, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (151, 34, 67, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (152, 34, 10, 9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 62, 69);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 62, 72);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 62, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 67, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 69, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 69, 64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 76, 66);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 75, 72);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 82, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 80, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 80, 81);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 90, 77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 96, 77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 93, 82);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 82, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 89, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 94, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 94, 95);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 86, 94);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 82, 98);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 78, 94);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 73, 98);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 68, 94);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 63, 97);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 57, 99);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 58, 94);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 55, 94);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 61, 89);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 63, 89);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 72, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 73, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 70, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 65, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 74, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 73, 79);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 53, 74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 41, 78);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 39, 74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 43, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 34, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 28, 71);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 31, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 32, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 29, 64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 34, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 35, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 39, 62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 17, 62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 23, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 17, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 16, 77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 16, 81);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 18, 81);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 18, 86);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 23, 86);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 26, 86);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 31, 81);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 35, 86);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 38, 86);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 40, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 40, 95);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 46, 98);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 48, 94);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 47, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 52, 89);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 50, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 51, 83);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 53, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 59, 83);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 46, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 23, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 11, 73);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 11, 66);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 6, 64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 5, 64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 7, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 7, 77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 8, 82);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 8, 84);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 4, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 4, 95);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 9, 95);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 7, 99);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 15, 92);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 19, 98);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 21, 98);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 26, 94);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 31, 98);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 31, 92);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 46, 62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (153, 34, 58, 63);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (154, 34, 47, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (127, 42, 92, 47);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (128, 42, 71, 3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (128, 42, 63, 3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (128, 42, 54, 3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (131, 42, 30, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (157, 42, 10, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 5, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 10, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 15, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 20, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 25, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 30, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 35, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 40, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 45, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 50, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 55, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 60, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 65, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 75, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 85, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 95, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 5, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 15, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 25, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 35, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 40, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 50, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 55, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 60, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 65, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 70, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 75, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 80, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 85, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 90, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 95, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 5, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 10, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 15, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 20, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 25, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 30, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 35, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 40, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 45, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 50, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 55, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 60, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 65, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 75, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 85, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 95, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 5, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 15, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 25, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 35, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 40, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 50, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 55, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 60, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 65, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 70, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 75, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 80, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 85, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 90, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 95, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 5, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 10, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 15, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 20, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 25, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 30, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 35, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 40, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 45, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 50, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 55, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 60, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 65, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 75, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 85, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 95, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 5, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 15, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 25, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 35, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 40, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 50, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 55, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 60, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 65, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 70, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 75, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 80, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 85, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 90, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 95, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 5, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 10, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 15, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 20, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 25, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 30, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 35, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 40, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 45, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 50, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 55, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 60, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 65, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 75, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 85, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 95, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 5, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 15, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 25, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 35, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 40, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 50, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 55, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 60, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 65, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 70, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 75, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 80, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 85, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 90, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 95, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 5, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 10, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 15, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 20, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 25, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 30, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 35, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 40, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 45, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 50, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 55, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 60, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 65, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 75, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 85, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 95, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 5, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 15, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 25, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 35, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 40, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 50, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 55, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 60, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 65, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 70, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 75, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 80, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 85, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 90, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (130, 42, 95, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (156, 43, 10, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 5, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 10, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 15, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 20, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 25, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 30, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 35, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 40, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 45, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 50, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 55, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 60, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 65, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 75, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 85, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 95, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 5, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 15, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 25, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 35, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 40, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 50, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 55, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 60, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 65, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 70, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 75, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 80, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 85, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 90, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 95, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 5, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 10, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 15, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 20, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 25, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 30, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 35, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 40, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 45, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 50, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 55, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 60, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 65, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 75, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 85, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 95, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 5, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 15, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 25, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 35, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 40, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 50, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 55, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 60, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 65, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 70, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 75, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 80, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 85, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 90, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 95, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 5, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 10, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 15, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 20, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 25, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 30, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 35, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 40, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 45, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 50, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 55, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 60, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 65, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 75, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 85, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 95, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 5, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 15, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 25, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 35, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 40, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 50, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 55, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 60, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 65, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 70, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 75, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 80, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 85, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 90, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 95, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 5, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 10, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 15, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 20, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 25, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 30, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 35, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 40, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 45, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 50, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 55, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 60, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 65, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 75, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 85, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 95, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 5, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 15, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 25, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 35, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 40, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 50, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 55, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 60, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 65, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 70, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 75, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 80, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 85, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 90, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 95, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 5, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 10, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 15, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 20, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 25, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 30, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 35, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 40, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 45, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 50, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 55, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 60, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 65, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 75, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 85, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 95, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 5, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 15, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 25, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 35, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 40, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 50, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 55, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 60, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 65, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 70, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 75, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 80, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 85, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 90, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (129, 43, 95, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (155, 44, 50, 86);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (158, 44, 50, 83);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (158, 44, 55, 83);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (158, 44, 60, 83);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (158, 44, 62, 84);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (158, 44, 65, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (158, 44, 55, 86);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (158, 44, 47, 86);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (158, 44, 44, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (158, 44, 52, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (158, 44, 57, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (158, 44, 61, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (158, 44, 65, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (158, 44, 61, 92);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (158, 44, 62, 94);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (158, 44, 59, 97);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (158, 44, 57, 93);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (158, 44, 54, 97);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (158, 44, 52, 95);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (158, 44, 52, 93);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (158, 44, 47, 91);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (158, 44, 44, 93);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (158, 44, 43, 95);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (158, 44, 44, 98);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (158, 44, 48, 97);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (158, 44, 48, 94);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (159, 10, 50, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (160, 6, 50, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (161, 24, 46, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (161, 24, 46, 36);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (161, 24, 49, 36);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (161, 24, 49, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (161, 24, 52, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (161, 24, 52, 36);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (161, 24, 55, 36);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (161, 24, 55, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (161, 24, 55, 29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (161, 24, 55, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (161, 24, 46, 29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (161, 24, 46, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (161, 24, 46, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (161, 24, 55, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (161, 24, 55, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (161, 24, 55, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (161, 24, 51, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (161, 24, 50, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (161, 24, 49, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (161, 24, 46, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (161, 24, 46, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (161, 29, 46, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (161, 29, 47, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (161, 29, 43, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (161, 29, 47, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (161, 29, 47, 53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (161, 29, 42, 53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (161, 29, 44, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (161, 29, 52, 46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (161, 29, 53, 44);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (161, 29, 56, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (161, 29, 58, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (161, 29, 56, 46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (161, 29, 56, 51);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (161, 29, 52, 51);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (161, 29, 53, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (161, 29, 57, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (162, 29, 44, 44);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (163, 24, 46, 8);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (164, 24, 55, 8);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (165, 24, 52, 8);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (170, 1, 46, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (171, 22, 10, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (171, 22, 10, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (171, 22, 10, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (171, 22, 10, 48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (171, 22, 5, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (171, 22, 4, 44);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (171, 22, 5, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (171, 22, 6, 44);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (171, 22, 14, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (171, 22, 14, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (171, 22, 14, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (171, 22, 14, 48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (171, 22, 18, 48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (171, 22, 18, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (171, 22, 18, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (171, 22, 18, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (171, 22, 22, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (171, 22, 22, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (171, 22, 22, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (171, 22, 22, 48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (172, 22, 5, 44);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (173, 22, 28, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (173, 22, 28, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (173, 22, 28, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (173, 22, 28, 48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (173, 22, 32, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (173, 22, 32, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (173, 22, 32, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (173, 22, 32, 48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (173, 22, 36, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (173, 22, 36, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (173, 22, 36, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (173, 22, 36, 48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (173, 22, 40, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (173, 22, 40, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (173, 22, 40, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (173, 22, 40, 48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (173, 22, 45, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (173, 22, 46, 44);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (173, 22, 45, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (173, 22, 44, 44);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (174, 22, 45, 44);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 37, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 37, 99);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 42, 94);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 42, 97);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 47, 91);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 47, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 52, 97);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 52, 92);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 58, 97);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 58, 95);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 58, 92);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 65, 95);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 71, 91);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 77, 91);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 82, 99);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 77, 99);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 72, 99);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 84, 94);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 71, 86);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 77, 86);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 77, 81);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 71, 81);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 82, 79);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 82, 74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 77, 73);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 73, 73);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 67, 72);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 65, 72);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 58, 77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 65, 79);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 45, 81);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 45, 72);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 38, 72);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 38, 81);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 28, 74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 28, 78);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 33, 74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 33, 78);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 20, 81);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 16, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 21, 72);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 21, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 16, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 16, 62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 21, 62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 26, 62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 20, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 23, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 25, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 33, 64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 33, 61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 33, 58);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 42, 64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 40, 62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 42, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 40, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 45, 54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 47, 54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 49, 54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 51, 54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 56, 54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 61, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 58, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 63, 64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 66, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 68, 54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 71, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 68, 64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 79, 64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 82, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 79, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 76, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 53, 48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 47, 48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 47, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 53, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 53, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 47, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 49, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 50, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 51, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 49, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 50, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 51, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 51, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 50, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 49, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 49, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 50, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 51, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 59, 37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 66, 36);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 73, 37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 79, 37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 87, 38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 85, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 82, 32);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 92, 37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 92, 32);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 87, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 82, 27);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 98, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 98, 32);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 97, 27);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 92, 27);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 99, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 97, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 92, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 90, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 85, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 83, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 78, 19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 81, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 83, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 85, 11);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 90, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 90, 8);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 91, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 92, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 72, 19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 74, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 77, 9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 82, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 39, 37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 33, 37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 25, 37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 18, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 17, 38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 15, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 13, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 12, 38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 10, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 6, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 3, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 2, 37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 2, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 7, 37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 7, 32);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 2, 28);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 3, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 11, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 11, 23);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 11, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 6, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 11, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 4, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 3, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 4, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 8, 8);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 14, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 14, 8);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 14, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 16, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 21, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 21, 8);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 21, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 28, 9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 30, 6);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (175, 40, 26, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (176, 40, 74, 95);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (176, 40, 62, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (176, 40, 41, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (176, 40, 27, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (176, 40, 74, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (176, 40, 50, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (176, 40, 91, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (176, 40, 79, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (176, 40, 4, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (176, 40, 10, 9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (177, 40, 70, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (178, 40, 50, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (179, 40, 28, 19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 94, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 93, 22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 93, 23);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 93, 24);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 93, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 93, 26);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 93, 27);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 93, 28);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 94, 29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 95, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 96, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 97, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 98, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 99, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 98, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 97, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 96, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 94, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 93, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 92, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 97, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 95, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 93, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 91, 41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 90, 36);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 89, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 88, 23);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 88, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 88, 27);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 88, 29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 83, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 81, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 80, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 81, 22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 82, 24);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 83, 26);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 76, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 74, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 70, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 67, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 68, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 72, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 72, 24);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 69, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 72, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 76, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 80, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 83, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 78, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 82, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 80, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 74, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 69, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 91, 71);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 92, 71);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 93, 71);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 94, 71);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 94, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 95, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 96, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 97, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 98, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 91, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 92, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 93, 66);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 94, 66);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 95, 66);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 97, 66);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 96, 66);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 98, 66);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 90, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 91, 64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 92, 63);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 93, 62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 94, 62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 95, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 96, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 97, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 98, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 91, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 91, 51);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 91, 52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 89, 52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 90, 53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 91, 53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 91, 54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 92, 53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 93, 52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 91, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 83, 46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 83, 47);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 82, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 82, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 82, 51);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 82, 52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 76, 54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 71, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 67, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 70, 52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 70, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 71, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 72, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 73, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 73, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 72, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 70, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 71, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 77, 62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 70, 63);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 66, 62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (166, 38, 64, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (167, 38, 95, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 73, 73);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 70, 73);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 77, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 80, 78);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 76, 83);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 75, 89);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 71, 94);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 67, 94);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 65, 91);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 66, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 69, 86);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 70, 82);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 70, 79);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 65, 78);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 64, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 65, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 66, 81);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 65, 83);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 72, 79);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 72, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 60, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 61, 91);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 58, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 55, 86);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 60, 83);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 60, 79);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 58, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 61, 71);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 53, 73);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 50, 71);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 51, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 52, 81);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 49, 84);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 51, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 49, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 54, 92);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 52, 97);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 47, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 44, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 45, 92);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 45, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 44, 84);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 46, 81);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 45, 77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 46, 74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 43, 71);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 40, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 37, 72);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 39, 77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 41, 82);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 37, 84);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 39, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 41, 89);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 41, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 37, 94);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 32, 93);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 34, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 33, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 31, 86);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 31, 83);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 31, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 31, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 31, 72);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 31, 64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 33, 62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 34, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 31, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 34, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 33, 83);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 29, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 24, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 19, 91);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 20, 89);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 26, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 25, 84);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 23, 79);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 26, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 24, 74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 25, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 27, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 21, 64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 20, 62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 24, 63);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 19, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 19, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 17, 81);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 18, 82);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 15, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 11, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 6, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 4, 89);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 3, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 7, 84);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 9, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 14, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 12, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 4, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 6, 72);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 10, 74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 13, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 16, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 22, 71);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 16, 66);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 16, 62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 11, 64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 9, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 4, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 13, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 13, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 8, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 3, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 6, 54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 3, 53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 7, 52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 12, 51);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 15, 54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 13, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 9, 58);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 9, 46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 6, 48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 4, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 3, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 6, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 9, 37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 9, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 4, 37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 5, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 8, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 5, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 8, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 9, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 9, 24);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 8, 24);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 8, 26);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 9, 26);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 8, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 9, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 10, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 12, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 14, 9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 18, 8);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 24, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 28, 6);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 33, 6);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 36, 6);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 39, 8);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 39, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 38, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 33, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 30, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 26, 11);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 24, 13);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 22, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 19, 11);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 18, 13);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 17, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 16, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 15, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 15, 22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 17, 24);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 20, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 25, 23);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 24, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 21, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 22, 18);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 26, 18);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 28, 19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 28, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 19, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 20, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 22, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 23, 32);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 19, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 21, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 27, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 33, 23);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 44, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 45, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 45, 19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 46, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 47, 23);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 48, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 46, 27);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 41, 26);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 43, 23);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 44, 22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 38, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 39, 18);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 36, 18);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 48, 32);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 47, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 47, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 46, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 41, 41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 40, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 37, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 34, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 33, 41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 32, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 28, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 22, 44);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 21, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 19, 41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 23, 41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 26, 41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 24, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 41, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 42, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 43, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 44, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 43, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 42, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 41, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 41, 11);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 41, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 41, 13);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 45, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 45, 11);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 45, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 45, 13);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 45, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 40, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 43, 9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 43, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 31, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 32, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 33, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 32, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 33, 11);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 34, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 33, 13);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 32, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (168, 38, 29, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (169, 38, 43, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (180, 36, 58, 78);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (181, 36, 58, 74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (182, 36, 96, 6);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (183, 36, 60, 72);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (184, 36, 65, 72);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (185, 1, 73, 54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (186, 1, 73, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (187, 16, 51, 97);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (188, 16, 66, 54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (189, 8, 83, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (190, 2, 14, 10);


DROP TABLE IF EXISTS npc_drops;
CREATE TABLE npc_drops (
  npc_template_id INT NOT NULL,
  item_template_id INT NOT NULL,
  stack INT NOT NULL,
  droprate DECIMAL(9,4) NOT NULL
);

INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (1, 1, 10, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (1, 2, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (1, 3, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (1, 4, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (1, 5, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (2, 1, 10, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (2, 2, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (2, 3, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (2, 4, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (2, 5, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (3, 4, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (3, 5, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (3, 7, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (3, 8, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (3, 455, 1, 3);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (4, 4, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (4, 5, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (4, 7, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (4, 8, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (4, 6, 1, 3);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (4, 455, 1, 3);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (5, 7, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (5, 8, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (5, 6, 1, 3);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (5, 9, 1, 2);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (5, 455, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (6, 4, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (6, 5, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (6, 11, 1, 3);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (6, 12, 1, 3);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (6, 13, 1, 3);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (6, 14, 1, 3);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (7, 4, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (7, 5, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (7, 15, 1, 3);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (7, 16, 1, 3);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (7, 17, 1, 3);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (7, 18, 1, 3);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (7, 333, 1, 3);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (8, 19, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (8, 20, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (8, 455, 1, 3);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (10, 4, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (10, 5, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (10, 21, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (13, 80, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (13, 67, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (13, 195, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (13, 196, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (14, 489, 1, 3);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (15, 225, 1, 7);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (15, 228, 1, 7);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (15, 233, 1, 7);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (15, 237, 1, 7);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (16, 213, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (16, 100, 1, 7);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (16, 230, 1, 7);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (16, 231, 1, 7);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (16, 235, 1, 7);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (16, 264, 1, 7);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (16, 268, 1, 7);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (16, 269, 1, 7);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (17, 64, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (17, 77, 1, 15);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (17, 72, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (17, 198, 1, 15);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (17, 197, 1, 15);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (17, 81, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (17, 224, 1, 7);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (17, 227, 1, 7);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (17, 232, 1, 7);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (17, 236, 1, 7);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (26, 214, 1, 4);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (26, 355, 1, 2);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (26, 623, 1, 4);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (27, 623, 1, 4);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (28, 623, 1, 4);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (56, 623, 1, 2);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (56, 624, 1, 2);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (57, 623, 1, 2);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (57, 624, 1, 2);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (56, 44, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (56, 45, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (57, 44, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (57, 45, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (58, 212, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (58, 68, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (58, 83, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (58, 73, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (58, 200, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (58, 199, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (58, 259, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (58, 273, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (58, 277, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (58, 420, 1, 7);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (58, 64, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (59, 68, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (59, 83, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (59, 73, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (59, 200, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (59, 199, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (59, 226, 1, 7);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (59, 230, 1, 7);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (59, 234, 1, 7);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (59, 238, 1, 7);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (59, 270, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (59, 275, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (59, 276, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (60, 68, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (60, 83, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (60, 73, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (60, 200, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (60, 199, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (60, 90, 1, 7);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (60, 107, 1, 7);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (60, 164, 1, 7);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (60, 149, 1, 7);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (60, 93, 1, 7);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (60, 165, 1, 7);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (60, 125, 1, 7);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (60, 168, 1, 7);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (60, 239, 1, 7);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (60, 240, 1, 7);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (60, 241, 1, 7);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (60, 242, 1, 7);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (60, 243, 1, 7);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (60, 244, 1, 7);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (60, 245, 1, 7);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (60, 246, 1, 7);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (60, 258, 1, 6);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (60, 253, 1, 6);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (60, 205, 1, 6);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (60, 273, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (60, 277, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (60, 275, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (60, 276, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (60, 302, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (60, 420, 1, 7);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (60, 266, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (60, 58, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (60, 622, 2, 2);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (60, 64, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (61, 343, 1, 2.5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (61, 457, 1, 2.5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (61, 444, 1, 2.5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (61, 445, 1, 2.5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (61, 623, 1, 1);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (61, 624, 1, 1);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (61, 626, 1, 1);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (61, 627, 1, 1);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (62, 343, 1, 2.5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (62, 457, 1, 2.5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (62, 444, 1, 2.5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (62, 445, 1, 2.5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (62, 623, 1, 1.5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (62, 624, 1, 1.5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (62, 626, 1, 1.5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (62, 627, 1, 1.5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (63, 343, 1, 2.5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (63, 457, 1, 2.5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (63, 444, 1, 2.5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (63, 445, 1, 2.5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (63, 623, 1, 1);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (63, 624, 1, 1);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (64, 343, 1, 2.5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (64, 457, 1, 2.5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (64, 444, 1, 2.5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (64, 445, 1, 2.5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (64, 623, 1, 1.5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (64, 624, 1, 1.5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (64, 626, 1, 1.5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (64, 627, 1, 1.5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (18, 42, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (18, 43, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (19, 160, 1, 6);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (19, 1, 5000, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (20, 4, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (20, 5, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (21, 4, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (21, 5, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (22, 46, 1, 6);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (22, 47, 1, 6);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (22, 265, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (23, 41, 1, 4);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (23, 48, 1, 4);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (24, 4, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (24, 5, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (25, 1, 2500, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (25, 142, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (34, 343, 1, 3);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (35, 343, 1, 3);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (38, 354, 1, 3);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (49, 354, 1, 3);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (39, 279, 1, 3);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (39, 280, 1, 3);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (39, 281, 1, 3);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (40, 42, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (40, 43, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (40, 44, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (40, 45, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (41, 42, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (41, 43, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (41, 44, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (41, 45, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (42, 42, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (42, 43, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (42, 44, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (42, 45, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (43, 42, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (43, 43, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (43, 44, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (43, 45, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (44, 42, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (44, 43, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (44, 44, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (44, 45, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (45, 42, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (45, 43, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (45, 44, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (45, 45, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (40, 330, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (41, 330, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (42, 330, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (43, 330, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (44, 330, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (45, 330, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (46, 45, 10, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (46, 50, 1, 15);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (46, 58, 1, 2);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (47, 45, 10, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (47, 49, 1, 15);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (47, 51, 1, 15);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (47, 263, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (48, 52, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (48, 53, 1, 15);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (48, 54, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (48, 50, 1, 15);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (48, 51, 1, 15);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (48, 58, 1, 4);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (50, 354, 1, 3);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (50, 623, 1, 2);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (50, 624, 1, 2);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (51, 623, 1, 2);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (51, 624, 1, 2);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (52, 623, 1, 2);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (52, 624, 1, 2);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (29, 60, 1, 3);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (30, 60, 1, 3);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (31, 60, 1, 3);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (32, 60, 1, 3);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (33, 55, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (33, 56, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (33, 57, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (33, 58, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (33, 59, 1, 1);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (53, 250, 1, 7);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (53, 312, 1, 7);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (53, 1, 10000, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (65, 115, 1, 15);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (65, 135, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (65, 136, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (65, 261, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (65, 271, 1, 2);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (66, 247, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (66, 248, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (66, 249, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (66, 252, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (66, 162, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (66, 146, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (66, 216, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (66, 622, 5, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (67, 95, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (67, 109, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (67, 127, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (67, 152, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (67, 167, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (67, 206, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (67, 622, 5, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (68, 94, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (68, 166, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (68, 126, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (68, 151, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (68, 422, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (68, 423, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (68, 424, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (68, 425, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (68, 622, 5, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (71, 266, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (71, 217, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (71, 115, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (71, 58, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (72, 266, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (72, 257, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (72, 96, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (72, 86, 1, 15);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (72, 87, 1, 15);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (72, 274, 1, 15);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (72, 278, 1, 15);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (72, 302, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (72, 420, 1, 7);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (72, 58, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (73, 266, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (73, 260, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (73, 218, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (73, 274, 1, 15);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (73, 278, 1, 15);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (73, 86, 1, 15);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (73, 87, 1, 15);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (73, 302, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (73, 420, 1, 7);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (73, 58, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (76, 313, 1, 7);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (76, 315, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (76, 316, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (76, 321, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (76, 318, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (76, 314, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (77, 439, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (77, 440, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (77, 441, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (78, 436, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (78, 437, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (78, 438, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (79, 291, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (79, 292, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (79, 282, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (79, 303, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (79, 442, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (80, 283, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (80, 284, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (80, 290, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (81, 285, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (81, 297, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (81, 298, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (81, 299, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (81, 300, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (81, 442, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (81, 512, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (82, 286, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (82, 304, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (82, 305, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (82, 306, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (82, 307, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (82, 308, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (82, 442, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (82, 512, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (83, 287, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (83, 288, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (83, 289, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (109, 219, 1, 4);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (109, 220, 1, 4);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (109, 221, 1, 4);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (109, 58, 1, 3);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (110, 254, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (110, 256, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (111, 255, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (111, 262, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (53, 267, 1, 4);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (53, 58, 1, 4);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (37, 267, 1, 2);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (54, 267, 1, 4);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (54, 58, 1, 4);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (115, 202, 1, 2);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (115, 203, 1, 2);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (115, 211, 1, 2);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (115, 215, 1, 2);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (116, 137, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (116, 144, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (118, 373, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (118, 374, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (118, 375, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (118, 376, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (118, 377, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (118, 378, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (118, 379, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (118, 380, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (135, 104, 1, 8);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (136, 132, 1, 8);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (136, 140, 1, 8);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (136, 161, 1, 8);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (136, 163, 1, 8);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (123, 319, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (123, 413, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (123, 473, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (123, 474, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (123, 421, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (123, 58, 1, 2);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (124, 414, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (143, 414, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (124, 415, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (143, 415, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (124, 416, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (143, 416, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (124, 419, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (143, 419, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (126, 397, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (126, 398, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (126, 514, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (138, 395, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (138, 396, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (139, 399, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (139, 400, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (139, 514, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (140, 417, 1, 8);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (140, 394, 1, 8);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (140, 513, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (140, 513, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (140, 566, 1, 50);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (141, 409, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (141, 412, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (142, 410, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (142, 411, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (132, 459, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (132, 459, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (132, 459, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (132, 463, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (132, 463, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (132, 463, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (132, 464, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (132, 464, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (132, 464, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (132, 465, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (132, 465, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (132, 465, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (132, 466, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (132, 466, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (132, 466, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (137, 459, 1, 25);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (137, 459, 1, 25);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (137, 459, 1, 25);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (137, 463, 1, 25);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (137, 463, 1, 25);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (137, 463, 1, 25);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (137, 464, 1, 25);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (137, 464, 1, 25);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (137, 464, 1, 25);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (147, 461, 1, 35);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (147, 461, 1, 35);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (147, 462, 1, 35);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (147, 462, 1, 35);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (144, 461, 1, 25);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (144, 461, 1, 25);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (144, 462, 1, 25);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (144, 462, 1, 25);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (144, 468, 1, 25);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (144, 468, 1, 25);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (144, 467, 1, 25);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (144, 467, 1, 25);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (152, 470, 1, 30);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (152, 470, 1, 30);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (152, 470, 1, 30);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (152, 460, 1, 30);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (152, 460, 1, 30);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (152, 460, 1, 30);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (154, 469, 1, 40);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (154, 470, 1, 40);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (154, 471, 1, 40);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (154, 460, 1, 40);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (127, 448, 1, 15);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (129, 431, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (129, 433, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (129, 434, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (129, 432, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (157, 446, 1, 7);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (157, 447, 1, 8);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (156, 427, 1, 12);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (156, 428, 1, 12);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (156, 429, 1, 12);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (156, 430, 1, 12);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (155, 426, 1, 12);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (161, 517, 1, 8);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (161, 518, 1, 8);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (161, 519, 1, 8);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (161, 520, 1, 8);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (162, 515, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (162, 515, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (162, 511, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (163, 297, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (163, 298, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (163, 299, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (163, 300, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (163, 297, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (163, 298, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (163, 299, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (163, 300, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (163, 490, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (163, 491, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (163, 492, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (163, 493, 1, 10);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (164, 502, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (164, 503, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (164, 504, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (164, 505, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (164, 506, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (164, 507, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (165, 508, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (165, 509, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (165, 510, 1, 20);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (175, 593, 1, 1);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (175, 594, 1, 2);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (175, 595, 1, 2);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (175, 597, 1, 2);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (176, 593, 1, 4);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (176, 594, 1, 25);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (176, 595, 1, 25);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (176, 597, 1, 25);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (177, 591, 1, 7);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (177, 591, 1, 7);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (177, 591, 1, 7);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (177, 592, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (178, 586, 1, 25);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (178, 592, 1, 5);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (179, 585, 1, 8);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (179, 585, 1, 8);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (179, 585, 1, 8);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (179, 585, 1, 8);
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (179, 592, 1, 5);


DROP TABLE IF EXISTS npc_vendor_items;
CREATE TABLE npc_vendor_items (
  npc_template_id INT NOT NULL,
  item_template_id INT NOT NULL,
  stack INT DEFAULT 1 NOT NULL,
  stats_visible CHAR(1) DEFAULT '1' NOT NULL,
  slot INT NOT NULL
);

CREATE INDEX npc_vendor_items_npc_template_id_idx ON npc_vendor_items(npc_template_id);

INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (84, 88, 1);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (84, 169, 2);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (84, 170, 3);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (84, 171, 4);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (84, 546, 5);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (84, 547, 6);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (85, 172, 1);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (86, 173, 1);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (87, 174, 1);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (88, 175, 1);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (89, 176, 1);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (90, 177, 1);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (91, 178, 1);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (92, 179, 1);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (93, 180, 1);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (94, 181, 1);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (95, 182, 1);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (96, 183, 1);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (97, 184, 1);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (98, 25, 1);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (98, 26, 2);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (98, 27, 3);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (98, 28, 4);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (98, 29, 5);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (98, 186, 6);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (98, 34, 7);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (98, 30, 8);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (98, 31, 9);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (98, 32, 10);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (98, 185, 11);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (98, 33, 12);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (98, 36, 13);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (98, 35, 14);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (98, 37, 15);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (98, 38, 16);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (98, 39, 17);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (98, 201, 18);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (98, 187, 19);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (98, 188, 20);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (98, 189, 21);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (98, 190, 22);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (98, 191, 23);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (98, 192, 24);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (98, 193, 25);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (98, 194, 26);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (99, 10, 1);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (99, 22, 2);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (99, 69, 3);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (99, 61, 4);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (99, 74, 5);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (99, 34, 6);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (99, 31, 7);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (99, 65, 8);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (99, 84, 9);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (99, 70, 10);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (99, 62, 11);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (99, 75, 12);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (99, 37, 13);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (99, 78, 14);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (99, 66, 15);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (99, 71, 16);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (99, 63, 17);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (99, 76, 18);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (99, 82, 19);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (99, 79, 20);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (99, 85, 21);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (100, 4, 1);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (100, 5, 2);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (100, 42, 6);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (100, 43, 7);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (100, 44, 11);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (100, 45, 12);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (101, 89, 1);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (101, 148, 2);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (101, 106, 3);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (101, 123, 4);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (102, 97, 1);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (102, 154, 2);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (102, 110, 3);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (102, 128, 4);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (103, 103, 1);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (103, 159, 2);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (103, 114, 3);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (103, 133, 4);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (104, 122, 1);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (104, 121, 6);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (104, 222, 7);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (104, 309, 8);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (104, 116, 11);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (104, 117, 12);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (104, 310, 13);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (104, 120, 16);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (104, 119, 17);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (104, 311, 18);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (105, 3, 1);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (105, 14, 2);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (105, 18, 7);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (105, 202, 12);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (105, 13, 3);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (105, 17, 8);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (105, 211, 13);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (105, 12, 4);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (105, 16, 9);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (105, 215, 14);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (105, 11, 5);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (105, 15, 10);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (105, 203, 15);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (106, 91, 1);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (106, 150, 2);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (106, 108, 3);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (106, 124, 4);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (106, 2, 6);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (106, 223, 7);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (107, 98, 1);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (107, 155, 2);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (107, 111, 3);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (107, 129, 4);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (108, 101, 1);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (108, 158, 2);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (108, 113, 3);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (108, 131, 4);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (119, 279, 1);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (119, 280, 2);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (119, 281, 3);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (120, 138, 1);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (121, 279, 1);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (121, 280, 2);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (121, 281, 3);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (121, 301, 4);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (122, 365, 1);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (122, 366, 2);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (122, 367, 3);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (122, 368, 4);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (122, 369, 5);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (122, 370, 6);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (122, 371, 7);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (122, 372, 8);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (148, 340, 1);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (148, 345, 2);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (148, 346, 3);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (148, 347, 4);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (148, 348, 5);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (148, 349, 6);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (148, 350, 7);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (148, 351, 8);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (148, 352, 9);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (148, 353, 10);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (149, 332, 1);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (149, 334, 2);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (149, 335, 3);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (149, 336, 4);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (149, 337, 5);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (149, 338, 6);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (149, 339, 7);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (149, 342, 8);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (149, 344, 9);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (150, 341, 1);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (150, 328, 2);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (159, 481, 1);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (159, 482, 2);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (159, 483, 3);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (159, 484, 4);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (160, 485, 1);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (160, 486, 2);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (160, 487, 3);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (160, 488, 4);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (170, 210, 1);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (170, 156, 2);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (170, 112, 3);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (170, 130, 4);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (170, 157, 5);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (170, 209, 6);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (170, 389, 7);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (170, 478, 8);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (170, 476, 9);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (170, 477, 10);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (170, 105, 11);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (170, 383, 12);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (170, 139, 13);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (170, 381, 14);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (170, 99, 15);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (170, 384, 16);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (170, 143, 17);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (170, 118, 18);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (170, 527, 19);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (170, 528, 20);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (170, 529, 21);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (170, 530, 22);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (170, 382, 23);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (170, 616, 24);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (180, 541, 1);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (180, 209, 2);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (180, 389, 3);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (180, 478, 4);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (180, 476, 5);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (180, 477, 6);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (180, 105, 7);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (180, 383, 8);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (180, 139, 9);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (180, 381, 10);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (180, 99, 11);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (180, 384, 12);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (180, 143, 13);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (180, 118, 14);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (180, 523, 15);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (180, 524, 16);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (180, 525, 17);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (180, 526, 18);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (180, 382, 19);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (180, 531, 20);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (181, 532, 1);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (181, 533, 2);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (181, 534, 3);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (181, 535, 4);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (181, 536, 5);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, slot)
VALUES (182, 621, 1);


DROP TABLE IF EXISTS quests;
CREATE TABLE quests (
  id INTEGER PRIMARY KEY,
  name TEXT NOT NULL,
  description TEXT DEFAULT '' NOT NULL,
  fail_text TEXT DEFAULT '' NOT NULL,
  pass_text TEXT DEFAULT '' NOT NULL,
  class_restrictions BIGINT DEFAULT 0,
  min_experience BIGINT DEFAULT 0,
  max_experience BIGINT DEFAULT 0,
  min_level INT DEFAULT 0,
  max_level INT DEFAULT 0,
  repeatable CHAR(1) DEFAULT '0',
  show_progress CHAR(1) DEFAULT '0',
  only_one_player_can_complete CHAR(1) DEFAULT '0',
  prerequisite_quests TEXT DEFAULT '' NOT NULL
);

INSERT INTO quests (id, name, description, pass_text, fail_text, class_restrictions, min_level, max_level, repeatable, show_progress)
VALUES (1, 'Path of the Warrior', 'Hey there, if you''re looking to follow the\npath of the warrior, I can make you one.\n\nWarriors rely on their HP and strength,\ntanking for the party.\n\nIf raw power and high health interests\nyou, then speak with me.', 'I have transformed you into a warrior.\nI have given you some items to help you\nalong your journey. Make me proud.', 'You do not meet the requirements.', 253, 5, 5, '1', '1');
INSERT INTO quests (id, name, description, pass_text, fail_text, class_restrictions, min_level, max_level, repeatable, show_progress)
VALUES (2, 'Path of the Magus', 'Hey there, if you''re looking to follow the\npath of the Magus, I can make you one.\n\nMagus rely on their MP and intelligence,\ndoing high damage from a range.\n\nIf high damage with safety and utility\ninterests you, then speak with me.', 'I have transformed you into a Magus.\nI have given you some items to help you\nalong your journey. Make me proud.', 'You do not meet the requirements.', 253, 5, 5, '1', '1');
INSERT INTO quests (id, name, description, pass_text, fail_text, class_restrictions, min_level, max_level, repeatable, show_progress)
VALUES (3, 'Path of the Rogue', 'Hey there, if you''re looking to follow the\npath of the Rogue, I can make you one.\n\nRogues rely on doing high damage using a\ncombination of HP and MP and critical\nstrikes.\n\nIf the path of the Rogue interests you,\nthen speak with me.', 'I have transformed you into a Rogue.\nI have given you some items to help you\nalong your journey. Make me proud.', 'You do not meet the requirements.', 253, 5, 5, '1', '1');
INSERT INTO quests (id, name, description, pass_text, fail_text, class_restrictions, min_level, max_level, repeatable, show_progress)
VALUES (4, 'Path of the Priest', 'Hey there, if you''re looking to follow the\npath of the Priest, I can make you one.\n\nPriests are vital to the survival of a\nparty. They heal and provide buffs.\n\nIf the path of the Priest interests you,\nthen speak with me.', 'I have transformed you into a Priest.\nI have given you some items to help you\nalong your journey. Make me proud.', 'You do not meet the requirements.', 253, 5, 5, '1', '1');
INSERT INTO quests (id, name, description, pass_text, fail_text, min_level, show_progress)
VALUES (5, 'Mouse Killer', 'Hello there, adventurer. These mice are\ngetting into everything and eating our\ncrops.\n\nHelp me kill 15 of these darn mice and I\nwill reward you.', 'Thank you for eliminating those pesky\nmice. Take these potions and gold.', 'You do not meet the requirements.', 1, '1');
INSERT INTO quests (id, name, description, pass_text, fail_text, min_level)
VALUES (6, 'Sheep Wool', 'Hello! These sheep are getting out of\nhand, there are too many of them and I\nneed their wool.\n\nPlease lend a hand and bring me 10 wool.\nYou will be rewarded.', 'Phew! That was hard work. Thank you.\nTake this as payment.', 'You do not meet the requirements.', 2);
INSERT INTO quests (id, name, description, pass_text, fail_text, min_level, show_progress)
VALUES (7, 'Jack''s Jacket', 'Hi there! Brrr. It''s cold out here. Please\nbring me 10 rabbit pelts and 10 rabbit fur\nso that I can make a jacket.\n\nBy the way, my friend Jill is out here\nsomewhere. I don''t know where she went.\nLet me know where she is so I know she''s\nalright.', 'Much better! Glad to know Jill is alright.\nHere, take these pants.', 'You do not meet the requirements.', 5, '1');
INSERT INTO quests (id, name, description, pass_text, fail_text, min_level, show_progress)
VALUES (8, 'Angry Rabbits', 'Helppppp! These bunnies are staring at me\nlike they want to kill me. I''m scared.\nPlease kill 20 of them so I get out of\nhere.', 'Thank you so much! Now I can go and see\nJack!', 'You do not meet the requirements.', 5, '1');
INSERT INTO quests (id, name, description, pass_text, fail_text, min_level, show_progress)
VALUES (9, 'Biting Asps', 'I keep stepping on these damn Asps and\nget bitten by them. Wipe them out. Kill\n20 Asps and you will be rewarded.', 'You squashed those Asps. Thank you.', 'You do not meet the requirements.', 7, '1');
INSERT INTO quests (id, name, description, pass_text, fail_text, min_level, show_progress)
VALUES (10, 'Screeching Bats', 'Hello there! These bats are driving me\ncrazy with their constant screeching.\nKill 25 of them for me, would you?', 'Muchhhh better! I can finally hear my own\nthoughts. Here take this.', 'You do not meet the requirements.', 10, '1');


DROP TABLE IF EXISTS quest_requirements;
CREATE TABLE quest_requirements (
  id INTEGER PRIMARY KEY,
  quest_id INT NOT NULL,
  requirement_type INT NOT NULL,
  requirement_value BIGINT NOT NULL,
  requirement_value2 BIGINT DEFAULT 0,
  keep_requirement CHAR(1) DEFAULT '0'
);

INSERT INTO quest_requirements (id, quest_id, requirement_type, requirement_value, requirement_value2)
VALUES (1, 5, 2, 1, 15);
INSERT INTO quest_requirements (id, quest_id, requirement_type, requirement_value, requirement_value2)
VALUES (2, 6, 1, 649, 10);
INSERT INTO quest_requirements (id, quest_id, requirement_type, requirement_value, requirement_value2)
VALUES (3, 7, 1, 7, 10);
INSERT INTO quest_requirements (id, quest_id, requirement_type, requirement_value, requirement_value2)
VALUES (4, 7, 1, 8, 10);
INSERT INTO quest_requirements (id, quest_id, requirement_type, requirement_value)
VALUES (5, 7, 3, 188);
INSERT INTO quest_requirements (id, quest_id, requirement_type, requirement_value, requirement_value2)
VALUES (6, 8, 2, 4, 20);
INSERT INTO quest_requirements (id, quest_id, requirement_type, requirement_value, requirement_value2)
VALUES (7, 9, 2, 6, 20);
INSERT INTO quest_requirements (id, quest_id, requirement_type, requirement_value, requirement_value2)
VALUES (8, 10, 2, 7, 25);


DROP TABLE IF EXISTS quest_rewards;
CREATE TABLE quest_rewards (
  id INTEGER PRIMARY KEY,
  quest_id INT NOT NULL,
  reward_type INT NOT NULL,
  long_value BIGINT DEFAULT 0,
  long_value2 BIGINT DEFAULT 0,
  string_value TEXT DEFAULT ''
);

INSERT INTO quest_rewards (id, quest_id, reward_type, long_value)
VALUES (1, 1, 11, 3);
INSERT INTO quest_rewards (id, quest_id, reward_type, long_value, long_value2)
VALUES (2, 1, 1, 644, 1);
INSERT INTO quest_rewards (id, quest_id, reward_type, long_value, long_value2)
VALUES (3, 1, 1, 648, 1);
INSERT INTO quest_rewards (id, quest_id, reward_type, long_value)
VALUES (4, 2, 11, 4);
INSERT INTO quest_rewards (id, quest_id, reward_type, long_value, long_value2)
VALUES (5, 2, 1, 644, 1);
INSERT INTO quest_rewards (id, quest_id, reward_type, long_value, long_value2)
VALUES (6, 2, 1, 646, 1);
INSERT INTO quest_rewards (id, quest_id, reward_type, long_value, long_value2)
VALUES (7, 2, 1, 25, 1);
INSERT INTO quest_rewards (id, quest_id, reward_type, long_value)
VALUES (8, 3, 11, 2);
INSERT INTO quest_rewards (id, quest_id, reward_type, long_value, long_value2)
VALUES (9, 3, 1, 644, 1);
INSERT INTO quest_rewards (id, quest_id, reward_type, long_value, long_value2)
VALUES (10, 3, 1, 647, 1);
INSERT INTO quest_rewards (id, quest_id, reward_type, long_value)
VALUES (11, 4, 11, 5);
INSERT INTO quest_rewards (id, quest_id, reward_type, long_value, long_value2)
VALUES (12, 4, 1, 644, 1);
INSERT INTO quest_rewards (id, quest_id, reward_type, long_value, long_value2)
VALUES (13, 4, 1, 645, 1);
INSERT INTO quest_rewards (id, quest_id, reward_type, long_value, long_value2)
VALUES (14, 4, 1, 10, 1);
INSERT INTO quest_rewards (id, quest_id, reward_type, long_value, long_value2)
VALUES (15, 5, 1, 4, 5);
INSERT INTO quest_rewards (id, quest_id, reward_type, long_value, long_value2)
VALUES (16, 5, 1, 5, 5);
INSERT INTO quest_rewards (id, quest_id, reward_type, long_value)
VALUES (17, 5, 0, 2000);
INSERT INTO quest_rewards (id, quest_id, reward_type, long_value)
VALUES (18, 6, 0, 2500);
INSERT INTO quest_rewards (id, quest_id, reward_type, long_value)
VALUES (19, 6, 5, 1000);
INSERT INTO quest_rewards (id, quest_id, reward_type, long_value)
VALUES (20, 7, 0, 3000);
INSERT INTO quest_rewards (id, quest_id, reward_type, long_value)
VALUES (21, 7, 5, 2000);
INSERT INTO quest_rewards (id, quest_id, reward_type, long_value, long_value2)
VALUES (22, 7, 1, 650, 1);
INSERT INTO quest_rewards (id, quest_id, reward_type, long_value)
VALUES (23, 8, 5, 2500);
INSERT INTO quest_rewards (id, quest_id, reward_type, long_value)
VALUES (24, 9, 5, 3000);
INSERT INTO quest_rewards (id, quest_id, reward_type, long_value)
VALUES (25, 10, 5, 5000);


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

INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (1, 'Healing 1', 0, 31, 810006, 20107, 5, 3);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (2, 'Fortify 1', 0, 31, 30000, 810018, 20107, 20, 4);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (3, 'Backstab 1', 1, 59, 18000, 810000, 20107, 20, 5);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (4, 'Taunt 1', 0, 55, 5000, 810000, 20107, 10, 6);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (5, 'Elemental Strike 1', 0, 47, 500, 810012, 20107, 5, 7);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (6, 'Elemental Strike 2', 0, 47, 500, 810013, 20107, 10, 8);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (7, 'Arcane Shield 1', 0, 47, 10000, 810018, 20107, 20, 9);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (8, 'Elemental Strike 3', 0, 47, 500, 810011, 20107, 20, 10);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (9, 'Elemental Shielding 1', 0, 47, 10000, 810004, 20107, 20, 11);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (10, 'Teleportation', 1, 47, 10000, 810009, 20107, 20, 12);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (11, 'Root', 0, 15, 810024, 20107, 20, 13);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (12, 'Elemental Strike 4', 0, 47, 500, 810003, 20107, 40, 14);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (13, 'Snare', 0, 47, 810023, 20107, 50, 15);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (14, 'Elemental Strike 5', 0, 47, 1000, 810012, 20107, 80, 16);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (15, 'Gate', 1, 15, 20000, 810009, 20107, 30, 17);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (16, 'Regeneration 1', 0, 47, 90000, 810008, 20107, 80, 18);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (17, 'Bind Self', 1, 15, 20000, 810007, 20107, 80, 19);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (18, 'Group Teleportation', 2, 47, 120000, 810009, 20107, 100, 12);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (19, 'Elemental Strike 6', 0, 47, 1000, 810013, 20107, 160, 20);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (20, 'Rampant Rage', 1, 55, 120000, 810033, 20107, 30, 21);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (21, 'Insight', 1, 55, 180000, 810007, 20107, 30, 22);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (22, 'Area Taunt', 1, 55, 10000, 810000, 20107, 40, 23);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (23, 'Ground Slam 1', 1, 55, 180000, 810011, 20107, 50, 24);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (24, 'Berserker 1', 1, 55, 225000, 810000, 20107, 50, 25);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (25, 'Poison Weapon 1', 1, 59, 120000, 810014, 20107, 30, 26);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (26, 'Backstab 2', 1, 59, 23000, 810000, 20107, 40, 27);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (27, 'Nimble 1', 1, 59, 90000, 810021, 20107, 50, 28);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (28, 'Backstab 3', 1, 59, 27000, 810000, 20107, 60, 29);
INSERT INTO spells (spell_id, spell_name, spell_target, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (29, 'Illusion: Snowman', 0, 300000, 810007, 20107, 100, 30);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (30, 'Healing 2', 0, 31, 810006, 20107, 10, 35);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (31, 'Healing 3', 0, 31, 810006, 20107, 25, 36);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (32, 'Healing 4', 0, 31, 810006, 20107, 50, 37);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (33, 'Healing 5', 0, 31, 810006, 20107, 100, 38);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (34, 'Fortify 2', 0, 31, 5000, 810018, 20107, 50, 39);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (35, 'Fortify 3', 0, 31, 5000, 810018, 20107, 100, 40);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (36, 'Fortify 4', 0, 31, 5000, 810018, 20107, 150, 41);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (37, 'Fortify 5', 0, 31, 5000, 810018, 20107, 300, 42);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (38, 'Strength 1', 0, 31, 5000, 810020, 20107, 20, 43);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (39, 'Strength 2', 0, 31, 5000, 810020, 20107, 50, 44);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (40, 'Strength 3', 0, 31, 5000, 810020, 20107, 75, 45);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (41, 'Strength 4', 0, 31, 5000, 810020, 20107, 100, 46);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (42, 'Strength 5', 0, 31, 5000, 810020, 20107, 150, 47);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (43, 'Stamina 1', 0, 31, 5000, 810019, 20107, 20, 48);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (44, 'Stamina 2', 0, 31, 5000, 810019, 20107, 50, 49);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (45, 'Stamina 3', 0, 31, 5000, 810019, 20107, 75, 50);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (46, 'Stamina 4', 0, 31, 5000, 810019, 20107, 100, 51);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (47, 'Intelligence 1', 0, 31, 5000, 810008, 20107, 50, 52);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (48, 'Intelligence 2', 0, 31, 5000, 810008, 20107, 100, 53);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (49, 'Dexterity 1', 0, 31, 5000, 810008, 20107, 75, 54);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (50, 'Dexterity 2', 0, 31, 5000, 810008, 20107, 150, 55);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (51, 'Mana Regeneration 1', 0, 31, 55000, 810032, 20107, 100, 56);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (52, 'Mana Regeneration 2', 0, 31, 105000, 810032, 20107, 250, 57);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (53, 'See Invisible', 0, 31, 5000, 810007, 20107, 20, 58);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, hp_static_cost, spell_effect_id)
VALUES (54, 'Sacrifice', 0, 31, 200, 810007, 20107, 2500, 59);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, hp_percent_cost, mp_percent_cost, spell_effect_id)
VALUES (55, 'Fearsome Lash', 1, 59, 3000, 810014, 20107, 30, 70, 62);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, hp_percent_cost, mp_static_cost, mp_percent_cost, spell_effect_id)
VALUES (56, 'Sunder of Spirits', 1, 55, 3000, 810028, 20107, 70, 300, 15, 64);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (57, 'Arcane Shield 2', 0, 47, 10000, 810018, 20107, 40, 101);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (58, 'Group Elemental Shielding 1', 2, 47, 10000, 810004, 20107, 80, 11);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (59, 'Invisibility', 0, 47, 5000, 810034, 20107, 80, 102);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (60, 'Elemental Strike 7', 0, 47, 1000, 810011, 20107, 280, 103);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (61, 'Elemental Shielding 2', 0, 47, 10000, 810004, 20107, 400, 104);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (62, 'Group Elemental Shielding 2', 2, 47, 10000, 810004, 20107, 800, 104);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (63, 'Regeneration 2', 0, 47, 300000, 810008, 20107, 500, 105);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (64, 'Bind Other', 0, 47, 10000, 810007, 20107, 400, 106);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (65, 'Otherlands Teleport', 1, 47, 10000, 810009, 20107, 500, 107);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (66, 'Group Otherlands Teleport', 2, 47, 20000, 810009, 20107, 800, 107);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (67, 'Elemental Strike 8', 1, 47, 4000, 810003, 20107, 400, 108);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (68, 'Arcane Shield 4', 0, 47, 20000, 810018, 20107, 400, 109);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (69, 'Elemental Strike 9', 0, 47, 1000, 810015, 20107, 350, 110);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (70, 'Regeneration 3', 0, 47, 330000, 810008, 20107, 800, 111);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (71, 'Elemental Strike 10', 0, 47, 1000, 810015, 20107, 450, 117);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (72, 'Arcane Shield 5', 0, 47, 20000, 810018, 20107, 800, 112);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (73, 'Arcane Shield 3', 0, 47, 20000, 810018, 20107, 200, 113);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (74, 'Ground Slam 2', 1, 55, 180000, 810011, 20107, 100, 118);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (75, 'Taunt 2', 0, 55, 5000, 810000, 20107, 60, 119);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (76, 'Fortitude 1', 1, 55, 180000, 810001, 20107, 130, 120);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (77, 'Berserker 2', 1, 55, 240000, 810000, 20107, 150, 121);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (78, 'Berserker 3', 1, 55, 270000, 810000, 20107, 300, 122);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (79, 'Fortitude 2', 1, 55, 180000, 810001, 20107, 400, 123);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (80, 'Savage Fury', 1, 55, 180000, 810033, 20107, 500, 124);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (81, 'Invisibility', 1, 59, 60000, 810034, 20107, 80, 102);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (82, 'Poison Weapon 2', 1, 59, 120000, 810014, 20107, 100, 125);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (83, 'Backstab 4', 1, 59, 23000, 810000, 20107, 160, 126);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (84, 'Nimble 2', 1, 59, 90000, 810021, 20107, 200, 127);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (85, 'Backstab 5', 1, 59, 18000, 810000, 20107, 320, 128);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (86, 'Ground Slam 3', 1, 55, 180000, 810011, 20107, 200, 129);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (87, 'Ground Slam 4', 1, 55, 180000, 810011, 20107, 300, 130);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, hp_percent_cost, spell_effect_id)
VALUES (88, 'Covenant', 1, 47, 4000, 810031, 20107, 50, 133);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_percent_cost, spell_effect_id)
VALUES (89, 'Arcane Blast', 0, 47, 2000, 810036, 20107, 100, 134);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_percent_cost, spell_effect_id)
VALUES (90, 'Arcane Assault', 0, 47, 3000, 810029, 20107, 80, 135);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, hp_percent_cost, mp_static_cost, spell_effect_id)
VALUES (91, 'Spirit Strike', 1, 55, 1500, 810016, 20107, 90, 400, 136);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, hp_percent_cost, mp_percent_cost, spell_effect_id)
VALUES (92, 'Critical Strike', 1, 59, 1000, 810014, 20107, 50, 50, 137);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (93, 'Rejuvination', 1, 31, 2000, 810005, 20107, 500, 138);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_percent_cost, spell_effect_id)
VALUES (94, 'Restore Health', 0, 31, 3000, 810010, 20107, 60, 139);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (95, 'Group Paradise Teleport', 2, 47, 20000, 810009, 20107, 5000, 141);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (96, 'Ancient Healing', 0, 31, 810006, 20107, 500, 143);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (97, 'Ancient Root', 0, 47, 30000, 810024, 20107, 1000, 144);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (98, 'Ancient Sturdiness', 1, 47, 1800000, 810011, 20107, 20000, 145);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (99, 'Ancient Criticality', 1, 59, 240000, 810035, 20107, 10000, 146);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (100, 'Ancient Augmentation', 1, 55, 240000, 810035, 20107, 4000, 147);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (101, 'Ancient Protection', 1, 31, 120000, 810030, 20107, 30000, 148);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (102, 'Ancient Buffiness', 1, 47, 300000, 810035, 20107, 45000, 149);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (103, 'Ancient Damage', 1, 59, 300000, 810000, 20107, 10000, 150);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (104, 'Ancient Taunt', 0, 55, 2000, 810000, 20107, 700, 151);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, hp_static_cost, spell_effect_id)
VALUES (105, 'Ancient Sacrifice', 2, 31, 1400, 810007, 20107, 5000, 152);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (106, 'Smoke Bomb', 1, 59, 300000, 810014, 20107, 500, 153);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (107, 'Group Heal', 2, 31, 1000, 810006, 20107, 75, 154);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (108, 'Warrior Root', 1, 55, 5000, 810024, 20107, 100, 155);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (109, 'Ancient Bellow', 0, 55, 4000, 810000, 20107, 1400, 165);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, hp_percent_cost, mp_static_cost, mp_percent_cost, spell_effect_id)
VALUES (110, 'Ancient Awe', 1, 55, 600000, 810033, 20107, 70, 15000, 90, 168);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, hp_static_cost, hp_percent_cost, mp_percent_cost, spell_effect_id)
VALUES (111, 'Ancient Death', 1, 59, 600000, 810017, 20107, 20000, 90, 50, 167);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, hp_static_cost, hp_percent_cost, mp_percent_cost, spell_effect_id)
VALUES (112, 'Ancient Conflagration', 1, 47, 600000, 810029, 20107, 20000, 95, 30, 166);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, hp_static_cost, spell_effect_id)
VALUES (113, 'Ancient Blessings', 2, 31, 600000, 810035, 20107, 30000, 170);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (114, 'Spiritual Blessings', 2, 31, 600000, 810007, 20107, 5000, 171);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, hp_static_cost, spell_effect_id)
VALUES (115, 'Sacrifice II', 0, 31, 400, 810007, 20107, 5000, 178);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (116, 'Damage of the Bear', 2, 47, 120000, 810035, 20107, 45000, 179);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (117, 'Critical Blow of the Bear', 2, 59, 120000, 810035, 20107, 45000, 180);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (118, 'Roar of the Bear', 1, 55, 5000, 810000, 20107, 4000, 181);
INSERT INTO spells (spell_id, spell_name, spell_target, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (119, 'Illusion: Bat', 0, 60000, 810007, 20107, 100, 183);
INSERT INTO spells (spell_id, spell_name, spell_target, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (120, 'Illusion: Shroom', 0, 60000, 810007, 20107, 100, 184);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (121, 'Ancient Group Healing', 2, 31, 1000, 810006, 20107, 1000, 199);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (122, 'Ancient Group Damage', 2, 59, 300000, 810027, 20107, 35000, 200);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (123, 'Ancient Regeneration', 2, 47, 45000, 810035, 20107, 35000, 201);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (124, 'Augment', 2, 47, 60000, 810001, 20107, 500, 202);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (125, 'Empower', 2, 31, 60000, 810020, 20107, 300, 203);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (126, 'Bustle', 2, 59, 600000, 810021, 20107, 100, 204);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (127, 'Aggravate', 2, 55, 60000, 810021, 20107, 100, 205);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (128, 'Meditate', 1, 47, 60000, 810032, 20107, 500, 206);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (129, 'Bulk', 1, 31, 60000, 810020, 20107, 300, 207);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (130, 'Tumble', 1, 59, 600000, 810027, 20107, 100, 208);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (131, 'Forge', 1, 55, 60000, 810008, 20107, 100, 209);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, hp_percent_cost, spell_effect_id)
VALUES (132, 'Mischiefs Craft', 1, 59, 6000, 810031, 20107, 25, 220);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (133, 'Wizards Curse', 0, 47, 300000, 810035, 20107, 65000, 221);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, hp_static_cost, mp_static_cost, spell_effect_id)
VALUES (134, 'Clerics Blessing', 2, 31, 600000, 810035, 20107, 50000, 70000, 222);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (135, 'Knights Blessing', 1, 55, 60000, 810018, 20107, 50000, 223);
INSERT INTO spells (spell_id, spell_name, spell_target, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (136, 'First Aid', 1, 250, 810006, 20107, 50, 226);
INSERT INTO spells (spell_id, spell_name, spell_target, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (137, 'Recovery', 0, 50, 810006, 20107, 250, 227);
INSERT INTO spells (spell_id, spell_name, spell_target, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (138, 'Clobber', 1, 250, 810012, 20107, 50, 228);
INSERT INTO spells (spell_id, spell_name, spell_target, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (139, 'Pummel', 0, 50, 810012, 20107, 250, 229);
INSERT INTO spells (spell_id, spell_name, spell_target, spell_aether, spellbook_graphic, spellbook_graphic_file, spell_effect_id)
VALUES (140, 'Tame', 1, 21600000, 810012, 20107, 230);
INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, spellbook_graphic_file, spell_effect_id)
VALUES (141, 'Pet Attack', 0, 810012, 20107, 231);
INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, spellbook_graphic_file, spell_effect_id)
VALUES (142, 'Pet Defend', 0, 810012, 20107, 232);
INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, spellbook_graphic_file, spell_effect_id)
VALUES (143, 'Pet Recall', 0, 810012, 20107, 233);
INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, spellbook_graphic_file, spell_effect_id)
VALUES (144, 'Pet Follow', 0, 810012, 20107, 234);
INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, spellbook_graphic_file, spell_effect_id)
VALUES (145, 'Pet Neutral', 0, 810012, 20107, 235);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (146, 'Ancient Healing 2', 0, 31, 810006, 20107, 1200, 242);
INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, spellbook_graphic_file, spell_effect_id)
VALUES (147, 'Death Touch', 0, 810036, 20107, 243);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (148, 'Group AD5 Teleport', 2, 47, 1000, 810009, 20107, 100000, 246);
INSERT INTO spells (spell_id, spell_name, spell_target, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (149, 'Paradise Teleportation', 1, 10000, 810009, 20107, 20, 141);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, hp_percent_cost, spell_effect_id)
VALUES (150, 'Ancient Covenant', 1, 47, 1500, 810031, 20107, 65, 247);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, hp_static_cost, spell_effect_id)
VALUES (151, 'Ancient Sacrifice 2', 2, 31, 1000, 810007, 20107, 10000, 248);
INSERT INTO spells (spell_id, spell_name, spell_target, class_restrictions, spell_aether, spellbook_graphic, spellbook_graphic_file, mp_static_cost, spell_effect_id)
VALUES (152, 'Ancient Taunt 2', 0, 55, 2000, 810000, 20107, 5000, 249);


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
  move_speed SMALLINT DEFAULT 0 NOT NULL,
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

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, hp_change_formula, works_not_in_pvp)
VALUES (1, 'Small Health Potion', 1, 0, '25', '1');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, mp_change_formula, works_not_in_pvp)
VALUES (2, 'Small Mana Potion', 1, 0, '25', '1');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, spell_damage_effects, hp_change_formula, works_not_in_pvp)
VALUES (3, 'Healing 1', 815015, 5, 0, '1', '25', '1');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, effect_duration, hp, stat_ac, works_not_in_pvp, buff_graphic, buff_graphic_file, buff_doesnt_stack_over)
VALUES (4, 'Fortify 1', 815016, 5, 1, 600, 50, 20, '1', 810018, 20107, '39 40 41 42');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, target_type, target_size, spell_effected, effect_type, do_attack_animation, spell_damage_effects, hp_change_formula)
VALUES (5, 'Backstab 1', 815010, 1, 1, 6, 0, '1', '1', '-2 * (%cstr + %cwdmg + %clevel)');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, hp_change_formula, taunt_aggro)
VALUES (6, 'Taunt 1', 815014, 2, 0, '-1', 1000);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, spell_damage_effects, hp_change_formula)
VALUES (7, 'Elemental Strike 1', 815000, 6, 0, '1', '-10');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, spell_damage_effects, hp_change_formula)
VALUES (8, 'Elemental Strike 2', 815003, 6, 0, '1', '-20');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, effect_duration, stat_ac, works_not_in_pvp, buff_graphic, buff_graphic_file, buff_doesnt_stack_over)
VALUES (9, 'Arcane Shield 1', 815016, 5, 1, 600, 10, '1', 810018, 20107, '101 113 109 112');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, spell_damage_effects, hp_change_formula)
VALUES (10, 'Elemental Strike 3', 815001, 6, 0, '1', '-40');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, effect_duration, works_not_in_pvp, buff_graphic, buff_graphic_file, buff_doesnt_stack_over, on_hit_spell_effect_id)
VALUES (11, 'Elemental Shielding 1', 815023, 5, 14, 600, '1', 810004, 20107, '104', 63);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, works_not_in_pvp)
VALUES (12, 'Teleportation', 815012, 5, 5, '1');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, effect_duration, buff_removable, buff_graphic, buff_graphic_file)
VALUES (13, 'Root', 815038, 2, 8, 25, '0', 810024, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, spell_damage_effects, hp_change_formula)
VALUES (14, 'Elemental Strike 4', 815002, 6, 0, '1', '-60');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, effect_duration, buff_removable, snare_percent)
VALUES (15, 'Snare', 815034, 2, 9, 30, '0', 80);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, target_type, target_size, spell_effected, effect_type, spell_damage_effects, hp_change_formula, random_join_chance)
VALUES (16, 'Elemental Strike 5', 815000, 1, 4, 4, 6, 0, '1', '-80', 33);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, teleport_map)
VALUES (17, 'Gate', 815012, 1, 5, 0);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, effect_duration, hp_percent_regen, works_not_in_pvp, buff_graphic, buff_graphic_file, buff_doesnt_stack_over)
VALUES (18, 'Regeneration 1', 815013, 5, 1, 180, 0.02, '1', 810008, 20107, '105 111');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type)
VALUES (19, 'Bind Self', 815014, 1, 6);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, target_type, target_size, spell_effected, effect_type, spell_damage_effects, hp_change_formula)
VALUES (20, 'Elemental Strike 6', 815003, 1, 3, 1, 6, 0, '1', '-100');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, effect_duration, buff_graphic, buff_graphic_file, buff_doesnt_stack_over, on_attack_spell_effect_id)
VALUES (21, 'Rampant Rage', 815048, 1, 13, 60, 810033, 20107, '124', 60);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, effect_duration, buff_graphic, buff_graphic_file)
VALUES (22, 'Insight', 815014, 1, 12, 300, 810007, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, target_type, target_size, spell_effected, effect_type, spell_damage_effects, hp_change_formula, taunt_aggro)
VALUES (23, 'Area Taunt', 815014, 1, 5, 4, 2, 0, '1', '-10', 3000);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, target_type, target_size, spell_effected, effect_type, effect_duration, spell_damage_effects, hp_change_formula, buff_removable, buff_graphic, buff_graphic_file)
VALUES (24, 'Ground Slam 1', 815001, 1, 3, 1, 2, 7, 10, '1', '-30', '0', 810011, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, effect_duration, stat_str, haste, buff_graphic, buff_graphic_file, buff_doesnt_stack_over)
VALUES (25, 'Berserker 1', 815024, 1, 4, 45, 50, 0.1, 810000, 20107, '121 122');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, effect_duration, buff_graphic, buff_graphic_file, buff_doesnt_stack_over, on_attack_spell_effect_id)
VALUES (26, 'Poison Weapon 1', 815041, 1, 13, 60, 810014, 20107, '125', 61);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, target_type, target_size, spell_effected, effect_type, do_attack_animation, spell_damage_effects, hp_change_formula)
VALUES (27, 'Backstab 2', 815010, 1, 1, 6, 0, '1', '1', '-3 * (%cstr + %cwdmg + %clevel)');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, effect_duration, stat_dex, haste, buff_graphic, buff_graphic_file, buff_doesnt_stack_over)
VALUES (28, 'Nimble 1', 815021, 1, 1, 60, 50, 0.1, 810021, 20107, '127');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, target_type, target_size, spell_effected, effect_type, do_attack_animation, spell_damage_effects, hp_change_formula)
VALUES (29, 'Backstab 3', 815010, 1, 1, 6, 0, '1', '1', '-4 * (%cstr + %cwdmg + %clevel)');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, effect_duration, body_id, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (30, 'Illusion: Snowman', 815014, 5, 1, 300, 114, '1', 810007, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, hp_change_formula, works_not_in_pvp)
VALUES (31, 'Health Potion', 1, 0, '50', '1');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, mp_change_formula, works_not_in_pvp)
VALUES (32, 'Mana Potion', 1, 0, '50', '1');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, hp_change_formula, works_not_in_pvp)
VALUES (33, 'Large Health Potion', 1, 0, '150', '1');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, mp_change_formula, works_not_in_pvp)
VALUES (34, 'Large Mana Potion', 1, 0, '150', '1');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, spell_damage_effects, hp_change_formula, works_not_in_pvp)
VALUES (35, 'Healing 2', 815015, 5, 0, '1', '50', '1');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, spell_damage_effects, hp_change_formula, works_not_in_pvp)
VALUES (36, 'Healing 3', 815015, 5, 0, '1', '125', '1');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, spell_damage_effects, hp_change_formula, works_not_in_pvp)
VALUES (37, 'Healing 4', 815015, 5, 0, '1', '350', '1');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, spell_damage_effects, hp_change_formula, works_not_in_pvp)
VALUES (38, 'Healing 5', 815015, 5, 0, '1', '1000', '1');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, min_level_effected, effect_type, effect_duration, hp, stat_ac, works_not_in_pvp, buff_graphic, buff_graphic_file, buff_doesnt_stack_over, buff_stacks_over)
VALUES (39, 'Fortify 2', 815016, 5, 5, 1, 600, 100, 35, '1', 810018, 20107, '40 41 42', '4');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, min_level_effected, effect_type, effect_duration, hp, stat_ac, works_not_in_pvp, buff_graphic, buff_graphic_file, buff_doesnt_stack_over, buff_stacks_over)
VALUES (40, 'Fortify 3', 815016, 5, 15, 1, 600, 200, 50, '1', 810018, 20107, '41 42', '4 39');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, min_level_effected, effect_type, effect_duration, hp, stat_ac, works_not_in_pvp, buff_graphic, buff_graphic_file, buff_doesnt_stack_over, buff_stacks_over)
VALUES (41, 'Fortify 4', 815016, 5, 25, 1, 600, 400, 75, '1', 810018, 20107, '42', '4 39 40');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, min_level_effected, effect_type, effect_duration, hp, stat_ac, works_not_in_pvp, buff_graphic, buff_graphic_file, buff_stacks_over)
VALUES (42, 'Fortify 5', 815016, 5, 35, 1, 600, 1000, 100, '1', 810018, 20107, '4 39 40 41');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, effect_duration, stat_str, works_not_in_pvp, buff_graphic, buff_graphic_file, buff_doesnt_stack_over)
VALUES (43, 'Strength 1', 815013, 5, 1, 600, 10, '1', 810020, 20107, '44 45 46 47');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, min_level_effected, effect_type, effect_duration, stat_str, works_not_in_pvp, buff_graphic, buff_graphic_file, buff_doesnt_stack_over, buff_stacks_over)
VALUES (44, 'Strength 2', 815013, 5, 10, 1, 600, 20, '1', 810020, 20107, '45 46 47', '43');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, min_level_effected, effect_type, effect_duration, stat_str, works_not_in_pvp, buff_graphic, buff_graphic_file, buff_doesnt_stack_over, buff_stacks_over)
VALUES (45, 'Strength 3', 815013, 5, 20, 1, 600, 30, '1', 810020, 20107, '46 47', '43 44');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, min_level_effected, effect_type, effect_duration, stat_str, works_not_in_pvp, buff_graphic, buff_graphic_file, buff_doesnt_stack_over, buff_stacks_over)
VALUES (46, 'Strength 4', 815013, 5, 30, 1, 600, 40, '1', 810020, 20107, '47', '43 44 45');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, min_level_effected, effect_type, effect_duration, stat_str, works_not_in_pvp, buff_graphic, buff_graphic_file, buff_stacks_over)
VALUES (47, 'Strength 5', 815013, 5, 40, 1, 600, 50, '1', 810020, 20107, '43 44 45 46');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, effect_duration, stat_sta, works_not_in_pvp, buff_graphic, buff_graphic_file, buff_doesnt_stack_over)
VALUES (48, 'Stamina 1', 815013, 5, 1, 600, 10, '1', 810019, 20107, '49 50 51');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, min_level_effected, effect_type, effect_duration, stat_sta, works_not_in_pvp, buff_graphic, buff_graphic_file, buff_doesnt_stack_over, buff_stacks_over)
VALUES (49, 'Stamina 2', 815013, 5, 10, 1, 600, 20, '1', 810019, 20107, '50 51', '48');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, min_level_effected, effect_type, effect_duration, stat_sta, works_not_in_pvp, buff_graphic, buff_graphic_file, buff_doesnt_stack_over, buff_stacks_over)
VALUES (50, 'Stamina 3', 815013, 5, 20, 1, 600, 30, '1', 810019, 20107, '51', '48 49');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, min_level_effected, effect_type, effect_duration, stat_sta, works_not_in_pvp, buff_graphic, buff_graphic_file, buff_stacks_over)
VALUES (51, 'Stamina 4', 815013, 5, 30, 1, 600, 40, '1', 810019, 20107, '48 49 50');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, min_level_effected, effect_type, effect_duration, stat_int, works_not_in_pvp, buff_graphic, buff_graphic_file, buff_doesnt_stack_over)
VALUES (52, 'Intelligence 1', 815013, 5, 15, 1, 600, 10, '1', 810008, 20107, '53');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, min_level_effected, effect_type, effect_duration, stat_int, works_not_in_pvp, buff_graphic, buff_graphic_file, buff_stacks_over)
VALUES (53, 'Intelligence 2', 815013, 5, 25, 1, 600, 20, '1', 810008, 20107, '52');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, min_level_effected, effect_type, effect_duration, stat_dex, works_not_in_pvp, buff_graphic, buff_graphic_file, buff_doesnt_stack_over)
VALUES (54, 'Dexterity 1', 815013, 5, 25, 1, 600, 20, '1', 810008, 20107, '55');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, min_level_effected, effect_type, effect_duration, stat_dex, works_not_in_pvp, buff_graphic, buff_graphic_file, buff_stacks_over)
VALUES (55, 'Dexterity 2', 815013, 5, 35, 1, 600, 40, '1', 810008, 20107, '54');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, min_level_effected, effect_type, effect_duration, mp_percent_regen, works_not_in_pvp, buff_graphic, buff_graphic_file, buff_doesnt_stack_over)
VALUES (56, 'Mana Regeneration 1', 815045, 5, 20, 1, 120, 0.02, '1', 810032, 20107, '57 138');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, min_level_effected, effect_type, effect_duration, mp_percent_regen, works_not_in_pvp, buff_graphic, buff_graphic_file, buff_doesnt_stack_over, buff_stacks_over)
VALUES (57, 'Mana Regeneration 2', 815045, 5, 35, 1, 150, 0.04, '1', 810032, 20107, '138', '56');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, effect_duration, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (58, 'See Invisible', 815014, 5, 1, 120, '1', 810007, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, min_level_effected, effect_type, mp_change_formula, works_not_in_pvp)
VALUES (59, 'Sacrifice', 815014, 5, 50, 0, '5000', '1');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, target_type, target_size, spell_effected, effect_type, spell_damage_effects, hp_change_formula)
VALUES (60, 'Rampant Rage Chomp', 815048, 1, 1, 1, 6, 0, '1', '-1 * (%cstr + %cwdmg + %clevel)');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, target_type, target_size, spell_effected, effect_type, effect_duration, hp_change_formula)
VALUES (61, 'Poison Weapon Bubble', 815027, 1, 1, 6, 3, 12, '-15');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, target_type, target_size, spell_effected, effect_type, do_attack_animation, spell_damage_effects, hp_change_formula)
VALUES (62, 'Fearsome Lash', 815008, 1, 1, 3, 6, 0, '1', '1', '-2.7 * ((%ccmp * 0.7) + (%cchp * 0.3))');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, spell_damage_effects, hp_change_formula)
VALUES (63, 'Elemental Shielding 1 Rocks', 815001, 6, 0, '1', '-15');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, target_type, target_size, spell_effected, effect_type, do_attack_animation, spell_damage_effects, hp_change_formula, taunt_aggro)
VALUES (64, 'Sunder of Spirits', 815042, 1, 6, 2, 6, 0, '1', '1', '-((%ccmp * 0.15) + (%cchp * 0.8))', 50000);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, target_size, spell_effected, effect_type, hair_a)
VALUES (65, 'Hair Dye: Black', 1, 1, 2, 180);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, mp_static_regen, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (66, 'Mana Point Regeneration C', 1, 1, 100, '1', 810032, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, mp_static_regen, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (67, 'Mana Point Regeneration D', 1, 1, 500, '1', 810032, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, hp_static_regen, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (68, 'Hit Point Regeneration C', 1, 1, 100, '1', 810008, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, hp_static_regen, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (69, 'Hit Point Regeneration D', 1, 1, 500, '1', 810008, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, spell_damage, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (70, 'Increased Spell Damage V', 1, 1, 0.05, '1', 810035, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, spell_damage, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (71, 'Increased Spell Damage X', 1, 1, 0.1, '1', 810035, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, spell_damage, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (72, 'Increased Spell Damage XX', 1, 1, 0.2, '1', 810035, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, haste, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (73, 'Haste V', 1, 1, 0.05, '1', 810027, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, haste, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (74, 'Haste X', 1, 1, 0.1, '1', 810027, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, haste, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (75, 'Haste XX', 1, 1, 0.2, '1', 810027, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, spell_crit, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (76, 'Spell Critical Damage V', 1, 1, 0.05, '1', 810035, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, spell_crit, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (77, 'Spell Critical Damage X', 1, 1, 0.1, '1', 810035, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, melee_crit, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (78, 'Melee Critical Damage V', 1, 1, 0.05, '1', 810035, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, melee_crit, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (79, 'Melee Critical Damage X', 1, 1, 0.1, '1', 810035, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, mp_percent_regen, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (80, 'Mana Point Regeneration 1', 1, 1, 0.01, '1', 810032, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, hp_percent_regen, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (81, 'Hit Point Regeneration 1', 1, 1, 0.01, '1', 810008, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, mp_percent_regen, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (82, 'Mana Point Regeneration 2', 1, 1, 0.02, '1', 810032, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, hp_percent_regen, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (83, 'Hit Point Regeneration 2', 1, 1, 0.02, '1', 810008, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, hp_percent_regen, mp_percent_regen, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (84, 'HP MP Regeneration', 1, 1, 0.01, 0.01, '1', 810008, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, target_size, spell_effected, effect_type, hair_r, hair_a)
VALUES (85, 'Hair Dye: Red', 1, 1, 2, 155, 160);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, target_size, spell_effected, effect_type, hair_b, hair_a)
VALUES (86, 'Hair Dye: Blue', 1, 1, 2, 155, 160);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, target_size, spell_effected, effect_type, hair_a)
VALUES (87, 'Hair Dye: Grey', 1, 1, 2, 100);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, target_size, spell_effected, effect_type, hair_id)
VALUES (88, 'Hair Cut: 1', 1, 1, 2, 31);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, target_size, spell_effected, effect_type, hair_id)
VALUES (89, 'Hair Cut: 2', 1, 1, 2, 32);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, target_size, spell_effected, effect_type, hair_id)
VALUES (90, 'Hair Cut: 3', 1, 1, 2, 33);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, target_size, spell_effected, effect_type, hair_id)
VALUES (91, 'Hair Cut: 4', 1, 1, 2, 34);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, target_size, spell_effected, effect_type, hair_id)
VALUES (92, 'Hair Cut: 5', 1, 1, 2, 35);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, target_size, spell_effected, effect_type, hair_id)
VALUES (93, 'Hair Cut: 6', 1, 1, 2, 36);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, target_size, spell_effected, effect_type)
VALUES (94, 'Hair Cut: 7', 1, 1, 2);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, target_size, spell_effected, effect_type, face_id)
VALUES (95, 'Face: 1', 1, 1, 2, 1);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, target_size, spell_effected, effect_type, face_id)
VALUES (96, 'Face: 2', 1, 1, 2, 1);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, target_size, spell_effected, effect_type, face_id)
VALUES (97, 'Face: 3', 1, 1, 2, 3);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, target_size, spell_effected, effect_type, face_id)
VALUES (98, 'Face: 4', 1, 1, 2, 2);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, target_size, spell_effected, effect_type, body_id)
VALUES (99, 'Sexchange: Male', 1, 1, 2, 1);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, target_size, spell_effected, effect_type, body_id)
VALUES (100, 'Sexchange: Female', 1, 1, 2, 11);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, min_level_effected, effect_type, effect_duration, stat_ac, works_not_in_pvp, buff_graphic, buff_graphic_file, buff_doesnt_stack_over, buff_stacks_over)
VALUES (101, 'Arcane Shield 2', 815016, 5, 10, 1, 600, 40, '1', 810018, 20107, '113 109 112', '9');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, effect_duration, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (102, 'Invisibility', 815047, 5, 11, 300, '1', 810034, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, target_type, target_size, spell_effected, effect_type, spell_damage_effects, hp_change_formula)
VALUES (103, 'Elemental Strike 7', 815001, 1, 2, 3, 6, 0, '1', '-200');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, min_level_effected, effect_type, effect_duration, works_not_in_pvp, buff_graphic, buff_graphic_file, buff_stacks_over, on_hit_spell_effect_id)
VALUES (104, 'Elemental Shielding 2', 815023, 5, 20, 14, 600, '1', 810004, 20107, '11', 114);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, min_level_effected, effect_type, effect_duration, hp_percent_regen, works_not_in_pvp, buff_graphic, buff_graphic_file, buff_doesnt_stack_over, buff_stacks_over)
VALUES (105, 'Regeneration 2', 815013, 5, 20, 1, 300, 0.04, '1', 810008, 20107, '111', '18');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, works_not_in_pvp)
VALUES (106, 'Bind Other', 815014, 4, 6, '1');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, teleport_map, teleport_x, teleport_y, works_not_in_pvp)
VALUES (107, 'Otherlands Teleport', 815012, 5, 5, 11, 10, 10, '1');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, target_type, target_size, spell_effected, effect_type, spell_damage_effects, hp_change_formula)
VALUES (108, 'Elemental Strike 8', 815002, 1, 1, 3, 6, 0, '1', '-250');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, min_level_effected, effect_type, effect_duration, stat_ac, works_not_in_pvp, buff_graphic, buff_graphic_file, buff_doesnt_stack_over, buff_stacks_over)
VALUES (109, 'Arcane Shield 4', 815016, 5, 25, 1, 600, 80, '1', 810018, 20107, '112', '9 101 113');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, spell_damage_effects, hp_change_formula)
VALUES (110, 'Elemental Strike 9', 815004, 6, 0, '1', '-300');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, min_level_effected, effect_type, effect_duration, hp_percent_regen, works_not_in_pvp, buff_graphic, buff_graphic_file, buff_stacks_over)
VALUES (111, 'Regeneration 3', 815013, 5, 30, 1, 300, 0.06, '1', 810008, 20107, '18 105');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, min_level_effected, effect_type, effect_duration, stat_ac, works_not_in_pvp, buff_graphic, buff_graphic_file, buff_stacks_over)
VALUES (112, 'Arcane Shield 5', 815016, 5, 35, 1, 600, 100, '1', 810018, 20107, '9 101 113 109');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, min_level_effected, effect_type, effect_duration, stat_ac, works_not_in_pvp, buff_graphic, buff_graphic_file, buff_doesnt_stack_over, buff_stacks_over)
VALUES (113, 'Arcane Shield 3', 815016, 5, 15, 1, 600, 60, '1', 810018, 20107, '109 112', '9 101');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, spell_damage_effects, hp_change_formula)
VALUES (114, 'Elemental Shielding 2 Rocks', 815001, 6, 0, '1', '-30');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, buff_removable, on_attack_spell_effect_id, on_attack_spell_chance)
VALUES (115, 'DDTS Effect', 6, 13, '0', 116, 7);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, target_type, target_size, spell_effected, effect_type, spell_damage_effects, hp_change_formula)
VALUES (116, 'DDTS Damage', 815002, 1, 3, 1, 6, 0, '1', '-2500');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, spell_damage_effects, hp_change_formula)
VALUES (117, 'Elemental Strike 10', 815005, 6, 0, '1', '-400');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, target_type, target_size, spell_effected, effect_type, effect_duration, spell_damage_effects, hp_change_formula, buff_removable, buff_graphic, buff_graphic_file)
VALUES (118, 'Ground Slam 2', 815001, 1, 5, 2, 2, 7, 15, '1', '-50', '0', 810011, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, hp_change_formula, taunt_aggro)
VALUES (119, 'Taunt 2', 815014, 2, 0, '-20', 10000);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, effect_duration, stat_ac, stat_sta, works_not_in_pvp, buff_graphic, buff_graphic_file, buff_doesnt_stack_over)
VALUES (120, 'Fortitude 1', 815016, 1, 1, 60, 150, 20, '1', 810001, 20107, '123');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, effect_duration, stat_str, haste, buff_graphic, buff_graphic_file, buff_doesnt_stack_over, buff_stacks_over)
VALUES (121, 'Berserker 2', 815024, 1, 4, 60, 100, 0.2, 810000, 20107, '122', '25');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, effect_duration, stat_str, haste, buff_graphic, buff_graphic_file, buff_stacks_over)
VALUES (122, 'Berserker 3', 815024, 1, 4, 90, 200, 0.3, 810000, 20107, '25 121');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, effect_duration, stat_ac, stat_sta, works_not_in_pvp, buff_graphic, buff_graphic_file, buff_stacks_over)
VALUES (123, 'Fortitude 2', 815016, 1, 1, 90, 300, 40, '1', 810001, 20107, '120');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, spell_effected, effect_type, effect_duration, buff_graphic, buff_graphic_file, buff_stacks_over, on_attack_spell_effect_id)
VALUES (124, 'Savage Fury', 815048, 1, 1, 13, 30, 810033, 20107, '25', 131);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, effect_duration, buff_graphic, buff_graphic_file, buff_stacks_over, on_attack_spell_effect_id)
VALUES (125, 'Poison Weapon 2', 815041, 1, 13, 60, 810014, 20107, '26', 132);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, target_type, target_size, spell_effected, effect_type, do_attack_animation, spell_damage_effects, hp_change_formula)
VALUES (126, 'Backstab 4', 815010, 1, 1, 6, 0, '1', '1', '-5 * (%cstr + %cwdmg + %clevel)');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, effect_duration, stat_dex, haste, buff_graphic, buff_graphic_file, buff_stacks_over)
VALUES (127, 'Nimble 2', 815021, 1, 1, 60, 100, 0.3, 810021, 20107, '28');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, target_type, target_size, spell_effected, effect_type, do_attack_animation, spell_damage_effects, hp_change_formula)
VALUES (128, 'Backstab 5', 815010, 1, 1, 6, 0, '1', '1', '-6 * (%cstr + %cwdmg + %clevel)');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, target_type, target_size, spell_effected, effect_type, effect_duration, spell_damage_effects, hp_change_formula, buff_removable, buff_graphic, buff_graphic_file)
VALUES (129, 'Ground Slam 3', 815001, 1, 5, 3, 2, 7, 20, '1', '-70', '0', 810011, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, target_type, target_size, spell_effected, effect_type, effect_duration, spell_damage_effects, hp_change_formula, buff_removable, buff_graphic, buff_graphic_file)
VALUES (130, 'Ground Slam 4', 815040, 5, 4, 2, 7, 30, '1', '-100', '0', 810011, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, target_type, target_size, spell_effected, effect_type, spell_damage_effects, hp_change_formula)
VALUES (131, 'Savage Fury Chomp', 815048, 1, 3, 1, 6, 0, '1', '-2 * (%cstr + %cwdmg + %clevel)');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, target_type, target_size, spell_effected, effect_type, effect_duration, hp_change_formula)
VALUES (132, 'Poison Weapon 2 Bubble', 815027, 1, 1, 6, 3, 18, '-35');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, mp_change_formula)
VALUES (133, 'Covenant', 815046, 1, 0, '%cchp');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, spell_damage_effects, hp_change_formula)
VALUES (134, 'Arcane Blast', 815032, 6, 0, '1', '-1.9 * %ccmp');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, target_type, target_size, spell_effected, effect_type, spell_damage_effects, hp_change_formula)
VALUES (135, 'Arcane Assault', 815043, 1, 3, 1, 6, 0, '1', '-1.2 * ((1 * %ccmp) + (0.25 * %cchp))');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, target_type, target_size, spell_effected, effect_type, do_attack_animation, spell_damage_effects, hp_change_formula)
VALUES (136, 'Spirit Strike', 815009, 1, 1, 1, 6, 0, '1', '1', '-1.3 * %cchp');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, target_type, target_size, spell_effected, effect_type, do_attack_animation, spell_damage_effects, hp_change_formula)
VALUES (137, 'Critical Strike', 815008, 1, 1, 1, 6, 0, '1', '1', '-2.0 * ((%ccmp * 0.5) + (%cchp * 0.5))');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, effect_duration, mp_percent_regen, buff_graphic, buff_graphic_file, buff_stacks_over)
VALUES (138, 'Rejuvination', 815020, 1, 1, 45, 0.08, 810005, 20107, '56 57');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, min_level_effected, effect_type, spell_damage_effects, hp_change_formula, works_not_in_pvp)
VALUES (139, 'Restore Health', 815028, 5, 50, 0, '1', '%ccmp', '1');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, damage_reduce, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (140, 'Damage Reduction X', 1, 1, 0.1, '1', 810035, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, teleport_map, teleport_x, teleport_y, works_not_in_pvp)
VALUES (141, 'Paradise Teleportation', 815012, 5, 5, 35, 82, 33, '1');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, haste, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (142, 'Haste XXX', 1, 1, 0.3, '1', 810027, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, spell_damage_effects, hp_change_formula, works_not_in_pvp)
VALUES (143, 'Ancient Healing', 815015, 5, 0, '1', '5000', '1');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, target_type, target_size, spell_effected, effect_type, effect_duration, buff_removable, buff_graphic, buff_graphic_file)
VALUES (144, 'Ancient Root', 815038, 5, 5, 2, 8, 25, '0', 810024, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, effect_duration, hp, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (145, 'Ancient Sturdiness', 815016, 1, 1, 180, 30000, '1', 810011, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, effect_duration, spell_crit, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (146, 'Ancient Criticality', 815026, 1, 1, 240, 0.05, '1', 810035, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, effect_duration, spell_crit, damage_reduce, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (147, 'Ancient Augmentation', 815041, 1, 1, 120, 0.1, 0.1, '1', 810035, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, target_type, target_size, spell_effected, min_level_effected, effect_type, effect_duration, hp, mp, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (148, 'Ancient Protection', 815044, 5, 4, 5, 50, 1, 300, 2500, 2500, '1', 810030, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, effect_duration, spell_damage, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (149, 'Ancient Buffiness', 815052, 1, 1, 180, 0.25, '1', 810035, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, effect_duration, spell_damage, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (150, 'Ancient Damage', 815052, 1, 1, 180, 0.05, '1', 810000, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, hp_change_formula, taunt_aggro, buff_graphic, buff_graphic_file)
VALUES (151, 'Ancient Taunt', 815014, 2, 0, '-10', 100000, 810000, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, min_level_effected, effect_type, mp_change_formula, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (152, 'Ancient Sacrifice', 815014, 4, 50, 0, '5000', '1', 810007, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, target_type, target_size, spell_effected, effect_type, effect_duration, buff_graphic, buff_graphic_file)
VALUES (153, 'Smoke Bomb', 815051, 1, 5, 7, 2, 7, 30, 810026, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, spell_damage_effects, hp_change_formula, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (154, 'Group Heal', 815015, 5, 0, '1', '300', '1', 810006, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, target_type, target_size, spell_effected, effect_type, effect_duration, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (155, 'Warrior Root', 815038, 1, 1, 2, 8, 25, '1', 810024, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, target_size, spell_effected, effect_type, hair_r, hair_g, hair_a)
VALUES (156, 'Hair Dye: Lime Green', 1, 1, 2, 40, 255, 160);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, target_size, spell_effected, effect_type, hair_r, hair_g, hair_b, hair_a)
VALUES (157, 'Hair Dye: Zelius'' Dye', 1, 1, 2, 255, 255, 255, 180);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, target_size, spell_effected, effect_type, hair_r, hair_a)
VALUES (158, 'Hair Dye: Fayt Dye', 1, 1, 2, 148, 209);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, target_size, spell_effected, effect_type, hair_r, hair_g, hair_b, hair_a)
VALUES (159, 'Hair Dye: Frozen Spit', 1, 1, 2, 164, 219, 247, 200);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, hp_static_regen, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (160, 'Hit Point Regeneration M', 1, 1, 1000, '1', 810008, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, mp_static_regen, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (161, 'Mana Point Regeneration M', 1, 1, 1000, '1', 810032, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, buff_graphic, buff_graphic_file, on_attack_spell_effect_id, on_attack_spell_chance)
VALUES (162, 'Ancient Poison', 815041, 1, 13, 810014, 20107, 163, 75);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, target_type, target_size, spell_effected, effect_type, effect_duration, hp_change_formula)
VALUES (163, 'Ancient Poison', 815027, 1, 1, 6, 3, 20, '-500');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, spell_damage, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (164, 'Increased Spell Damage XIII', 1, 1, 0.13, '1', 810035, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, hp_change_formula, taunt_aggro, buff_graphic, buff_graphic_file)
VALUES (165, 'Ancient Bellow', 815014, 2, 0, '-200', 1000000, 810000, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, target_type, target_size, spell_effected, effect_type, spell_damage_effects, hp_change_formula)
VALUES (166, 'Ancient Conflagration', 815031, 1, 5, 7, 2, 0, '1', '-((%cchp * 1.5) + (%ccmp * 0.3))');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, target_type, target_size, spell_effected, effect_type, spell_damage_effects, hp_change_formula)
VALUES (167, 'Ancient Death', 815026, 1, 6, 7, 2, 0, '1', '-((%cchp * 2) + (%ccmp * 0.5))');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, target_type, target_size, spell_effected, effect_type, spell_damage_effects, hp_change_formula)
VALUES (168, 'Ancient Awe', 815048, 1, 5, 7, 2, 0, '1', '-((%cchp * .2) + (%ccmp * 2.8))');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, target_size, spell_effected, effect_type, hair_r, hair_g, hair_b, hair_a)
VALUES (169, 'Hair Dye: Purple Haze', 1, 1, 2, 116, 12, 108, 145);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, min_level_effected, effect_type, effect_duration, hp, mp, stat_ac, hp_percent_regen, mp_percent_regen, spell_damage, spell_crit, works_not_in_pvp, buff_graphic, buff_graphic_file, buff_doesnt_stack_over, buff_stacks_over)
VALUES (170, 'Ancient Blessings', 815030, 5, 50, 4, 30, 2500, 2500, 200, 0.25, 0.25, 0.25, 0.25, '1', 810035, 20107, '222', '171');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, min_level_effected, effect_type, effect_duration, hp, mp, stat_ac, hp_percent_regen, mp_percent_regen, spell_damage, spell_crit, works_not_in_pvp, buff_graphic, buff_graphic_file, buff_doesnt_stack_over)
VALUES (171, 'Spiritual Blessings', 815014, 5, 50, 4, 20, 1000, 1000, 100, 0.1, 0.1, 0.1, 0.1, '1', 810007, 20107, '170 222');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, min_level_effected, effect_type, teleport_map, teleport_x, teleport_y, works_not_in_pvp)
VALUES (172, 'Ancients Dungeon Teleportation', 815012, 5, 50, 5, 24, 5, 92, '1');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, damage_reduce, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (173, 'Damage Reduction I', 1, 1, 0.01, '1', 810035, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, damage_reduce, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (174, 'Damage Reduction II', 1, 1, 0.02, '1', 810035, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, damage_reduce, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (175, 'Damage Reduction III', 1, 1, 0.03, '1', 810035, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, damage_reduce, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (176, 'Damage Reduction IV', 1, 1, 0.04, '1', 810035, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, damage_reduce, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (177, 'Damage Reduction V', 1, 1, 0.05, '1', 810035, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, min_level_effected, effect_type, mp_change_formula, works_not_in_pvp)
VALUES (178, 'Sacrifice II', 815014, 5, 50, 0, '10000', '1');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, effect_duration, spell_damage, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (179, 'Damage of the Bear', 815052, 5, 1, 240, 0.1, '1', 810035, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, effect_duration, spell_crit, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (180, 'Critical Blow of the Bear', 815026, 5, 1, 240, 0.1, '1', 810035, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, target_type, target_size, spell_effected, effect_type, spell_damage_effects, hp_change_formula, taunt_aggro)
VALUES (181, 'Roar of the Bear', 815014, 1, 5, 4, 2, 0, '1', '-1000', 1000000);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, haste, body_id, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (182, 'Wolfs Essence', 1, 1, 0.3, 160, '1', 810027, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, effect_duration, body_id, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (183, 'Illusion: Bat', 815014, 5, 1, 300, 10100, '1', 810007, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, effect_duration, body_id, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (184, 'Illusion: Shroom', 815014, 5, 1, 300, 108, '1', 810007, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, effect_duration, body_id, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (185, 'Illusion: Bear', 815014, 5, 1, 300, 120, '1', 810007, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, effect_duration, stat_ac, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (186, 'Shard of Earth', 815016, 1, 1, 60, 500, '1', 810018, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, effect_duration, stat_str, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (187, 'Shard of Strength', 815013, 1, 1, 60, 1000, '1', 810020, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, effect_duration, spell_damage, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (188, 'Shard of Love', 815052, 1, 1, 60, 0.2, '1', 810035, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, effect_duration, hp, mp, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (189, 'Shard of Life', 815044, 1, 1, 60, 5000, 5000, '1', 810030, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, effect_duration, damage_reduce, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (190, 'Shard of Protection', 815041, 1, 1, 60, 0.2, '1', 810035, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, effect_duration, hp, mp, hp_percent_regen, mp_percent_regen, spell_damage, spell_crit, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (191, 'Shard of Power', 815024, 1, 1, 60, 7000, 7000, 0.02, 0.02, 0.2, 0.1, '1', 810001, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, effect_duration, damage_reduce, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (192, 'Shard of Invincibility', 815041, 1, 1, 60, 1, '1', 810035, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, effect_duration, spell_crit, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (193, 'Shard of Hope', 815026, 1, 1, 60, 0.2, '1', 810035, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, effect_duration, hp_percent_regen, mp_percent_regen, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (194, 'Shard of Divinity', 815045, 1, 1, 60, 0.04, 0.04, '1', 810032, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, target_type, target_size, spell_effected, effect_type, hp_change_formula)
VALUES (195, 'Shard of Fire', 815002, 1, 6, 4, 6, 0, '-20000');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, target_type, target_size, spell_effected, effect_type, hp_change_formula)
VALUES (196, 'Shard of Death', 815026, 1, 1, 1, 6, 0, '-10000');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, target_type, target_size, spell_effected, effect_type, hp_change_formula)
VALUES (197, 'Shard of Water', 815003, 1, 6, 5, 6, 0, '-30000');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, target_type, target_size, spell_effected, effect_type, hp_change_formula, random_join_chance)
VALUES (198, 'Shard of Air', 815004, 1, 4, 4, 6, 0, '-25000', 40);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, spell_damage_effects, hp_change_formula, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (199, 'Ancient Group Healing', 815015, 5, 0, '1', '6000', '1', 810006, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, effect_duration, spell_damage, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (200, 'Ancient Group Damage', 815041, 5, 1, 240, 0.05, '1', 810027, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, effect_duration, hp_percent_regen, mp_percent_regen, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (201, 'Ancient Regeneration', 5, 1, 300, 0.01, 0.01, '1', 810008, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, effect_duration, hp, mp, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (202, 'Augment', 815016, 5, 1, 300, 250, 500, '1', 810001, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, effect_duration, stat_str, hp_static_regen, mp_static_regen, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (203, 'Empower', 815013, 5, 1, 300, 30, 25, 25, '1', 810020, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, effect_duration, haste, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (204, 'Bustle', 815022, 5, 1, 300, 0.05, '1', 810021, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, effect_duration, melee_damage, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (205, 'Aggravate', 815022, 5, 1, 300, 0.1, '1', 810021, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, effect_duration, mp_static_regen, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (206, 'Meditate', 815045, 1, 1, 120, 50, '1', 810032, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, effect_duration, stat_str, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (207, 'Bulk', 815013, 1, 1, 120, 70, '1', 810020, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, effect_duration, melee_crit, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (208, 'Tumble', 815041, 1, 1, 120, 0.1, '1', 810027, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, effect_duration, hp_static_regen, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (209, 'Forge', 815013, 1, 1, 120, 100, '1', 810008, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, hp_change_formula, mp_change_formula, works_not_in_pvp)
VALUES (210, 'Potion of Restoration', 815028, 5, 0, '%chp', '%cmp', '1');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, spell_crit, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (211, 'Spell Critical Damage III', 1, 1, 0.03, '1', 810035, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, spell_damage, spell_crit, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (212, 'Spell Critical and Damage III', 1, 1, 0.03, 0.03, '1', 810035, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, spell_damage, damage_reduce, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (213, 'Royal Mischief Blessing', 1, 1, 0.08, 0.08, '1', 810035, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, spell_damage, damage_reduce, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (214, 'Royal Knight Blessing', 1, 1, 0.02, 0.13, '1', 810035, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, spell_damage, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (215, 'Increased Spell Damage XV', 1, 1, 0.15, '1', 810035, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, spell_crit, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (216, 'Spell Critical Damage VIII', 1, 1, 0.08, '1', 810035, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, spell_damage, spell_crit, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (217, 'Spell Critical V and Damage XX', 1, 1, 0.2, 0.05, '1', 810035, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, spell_crit, damage_reduce, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (218, 'Spell Critical V and Reduction XV', 1, 1, 0.05, 0.15, '1', 810035, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, spell_damage, spell_crit, damage_reduce, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (219, 'Slayers Blessing', 1, 1, 0.03, 0.03, 0.03, '1', 810035, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, mp_change_formula)
VALUES (220, 'Mischiefs Craft', 815046, 1, 0, '%cchp * .5');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, effect_duration, hp_percent_regen, works_not_in_pvp, buff_removable, buff_graphic, buff_graphic_file)
VALUES (221, 'Wizards Curse', 815013, 2, 1, 30, -0.02, '1', '0', 810008, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, min_level_effected, effect_type, effect_duration, hp, mp, stat_ac, hp_percent_regen, mp_percent_regen, spell_damage, spell_crit, works_not_in_pvp, buff_graphic, buff_graphic_file, buff_stacks_over)
VALUES (222, 'Clerics Blessing', 815030, 5, 50, 4, 20, 3500, 3500, 350, 0.35, 0.35, 0.35, 0.35, '1', 810035, 20107, '171 170');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, min_level_effected, effect_type, effect_duration, hp, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (223, 'Knights Blessing', 815016, 1, 50, 1, 180, 100000, '1', 810018, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, target_size, spell_effected, effect_type, hair_r, hair_b, hair_a)
VALUES (224, 'Hair Dye: Trouble', 1, 1, 2, 255, 125, 180);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, target_size, spell_effected, effect_type, hair_r, hair_g, hair_b, hair_a)
VALUES (225, 'Hair Dye: Mald''s Dye', 1, 1, 2, 234, 139, 173, 180);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, spell_damage_effects, hp_change_formula, works_not_in_pvp)
VALUES (226, 'First Aid', 815015, 1, 0, '1', '250', '1');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, spell_damage_effects, hp_change_formula, works_not_in_pvp)
VALUES (227, 'Recovery', 815015, 5, 0, '1', '1000', '1');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, target_type, target_size, spell_effected, effect_type, spell_damage_effects, hp_change_formula)
VALUES (228, 'Clobber', 815000, 1, 1, 1, 6, 0, '1', '-250');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, spell_effected, effect_type, spell_damage_effects, hp_change_formula)
VALUES (229, 'Pummel', 815000, 1, 6, 0, '1', '-1000');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, target_type, target_size, spell_effected, effect_type, only_hits_one_npc)
VALUES (230, 'Tame', 815000, 1, 5, 5, 2, 15, '1');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, spell_effected, effect_type)
VALUES (231, 'Pet Attack', 815000, 1, 7, 16);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, spell_effected, effect_type)
VALUES (232, 'Pet Defend', 815000, 1, 4, 17);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, spell_effected, effect_type)
VALUES (233, 'Pet Recall', 815000, 1, 4, 18);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, spell_effected, effect_type)
VALUES (234, 'Pet Follow', 815000, 1, 4, 19);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, spell_effected, effect_type)
VALUES (235, 'Pet Neutral', 815000, 1, 4, 20);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, target_size, spell_effected, effect_type, hair_g, hair_a)
VALUES (236, 'Hair Dye: Green', 1, 1, 2, 255, 180);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, target_size, spell_effected, effect_type, hair_r, hair_g, hair_b, hair_a)
VALUES (237, 'Hair Dye: Blonde', 1, 1, 2, 253, 232, 80, 160);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, min_level_effected, effect_type, teleport_map, teleport_x, teleport_y, works_not_in_pvp)
VALUES (238, 'PVP Event Teleport', 815012, 5, 0, 5, 22, 25, 24, '1');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, target_size, spell_effected, effect_type, hair_r, hair_g, hair_b, hair_a)
VALUES (239, 'Hair Dye: Rampant Rape', 1, 1, 2, 25, 25, 65, 215);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, target_size, spell_effected, effect_type, hair_r, hair_g, hair_b, hair_a)
VALUES (240, 'Hair Dye: Beowulf Sperm', 1, 1, 2, 280, 113, 39, 5180);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, target_size, spell_effected, effect_type, hair_r, hair_g, hair_b, hair_a)
VALUES (241, 'Hair Dye: Sorwind''s Dye', 1, 1, 2, 300, 300, 300, 550);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, spell_damage_effects, hp_change_formula, works_not_in_pvp)
VALUES (242, 'Ancient Healing 2', 815015, 5, 0, '1', '10000', '1');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, spell_damage_effects, hp_change_formula, works_not_in_pvp)
VALUES (243, 'Death Touch', 815032, 6, 0, '1', '-(%thp * 100)', '1');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, spell_crit, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (244, 'Increased Spell Critical V', 1, 1, 0.05, '1', 810035, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, hp_static_regen, mp_static_regen, spell_damage, spell_crit, damage_reduce, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (245, 'Spirit Power', 1, 1, 500, 500, 0.025, 0.025, 0.025, '1', 810035, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, min_level_effected, effect_type, teleport_map, teleport_x, teleport_y, works_not_in_pvp)
VALUES (246, 'Ancients Dungeon Teleport', 815012, 5, 0, 5, 40, 19, 98, '1');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, effect_type, mp_change_formula)
VALUES (247, 'Ancient Covenant', 815046, 1, 0, '0.9 * %cchp');
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_effected, min_level_effected, effect_type, mp_change_formula, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (248, 'Ancient Sacrifice 2', 815014, 4, 50, 0, '10000', '1', 810007, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, target_type, target_size, spell_effected, effect_type, hp_change_formula, taunt_aggro)
VALUES (249, 'Ancient Taunt 2', 815014, 1, 5, 4, 2, 0, '-5000', 5000000);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, target_size, spell_effected, effect_type, hair_r, hair_g, hair_b, hair_a)
VALUES (250, 'Hair Dye: Wesley Snipers', 1, 1, 2, 1, 1, 1, 255);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, target_size, spell_effected, effect_type, effect_duration, hp_percent_regen, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (251, 'HP Regeneration', 815013, 1, 1, 1, 1, 1800, 0.02, '1', 810008, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, target_size, spell_effected, effect_type, effect_duration, mp_percent_regen, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (252, 'MP Regeneration', 815013, 1, 1, 1, 1, 1800, 0.02, '1', 810008, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, target_size, spell_effected, effect_type, effect_duration, haste, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (253, 'Haste', 815013, 1, 1, 1, 1, 600, 0.2, '1', 810027, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, target_size, spell_effected, effect_type, effect_duration, spell_damage, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (254, 'Spell Damage', 815052, 1, 1, 1, 1, 900, 0.15, '1', 810035, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, target_size, spell_effected, effect_type, effect_duration, spell_crit, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (255, 'Spell Crit', 815041, 1, 1, 1, 1, 900, 0.15, '1', 810035, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, hp_percent_regen, spell_crit, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (256, 'Spell Critical XX and HP Reg', 1, 1, 0.01, 0.2, '1', 810035, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, mp_percent_regen, spell_damage, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (257, 'Spell Damage XX and MP Reg', 1, 1, 0.01, 0.2, '1', 810035, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, spell_crit, works_not_in_pvp, buff_graphic, buff_graphic_file)
VALUES (258, 'Spell Critical XX', 1, 1, 0.2, '1', 810035, 20107);
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_animation_file, spell_display, target_type, target_size, spell_effected, effect_type, move_speed, buff_graphic, buff_graphic_file, buff_stacks_over)
VALUES (259, 'Mount Speed II', 0, 0, 0, 0, 0, 1, 1, 128, 50754, 104, '282 283 284');


DROP TABLE IF EXISTS warptiles;
CREATE TABLE warptiles (
  map_id SMALLINT NOT NULL,
  map_x SMALLINT NOT NULL,
  map_y SMALLINT NOT NULL,
  warp_id SMALLINT NOT NULL,
  warp_x SMALLINT NOT NULL,
  warp_y SMALLINT NOT NULL
);

INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (4, 25, 14, 1, 42, 28);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (1, 42, 27, 4, 24, 14);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (4, 25, 15, 1, 41, 28);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (1, 41, 27, 4, 24, 15);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (1, 40, 27, 4, 24, 15);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (15, 13, 15, 1, 39, 28);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (15, 12, 15, 1, 39, 28);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (15, 11, 15, 1, 38, 28);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (15, 10, 15, 1, 37, 28);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (15, 9, 15, 1, 37, 28);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (1, 37, 27, 15, 9, 14);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (1, 38, 27, 15, 11, 14);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (1, 39, 27, 15, 13, 14);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (8, 100, 25, 1, 2, 49);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (8, 100, 26, 1, 2, 50);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (8, 100, 27, 1, 2, 51);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (8, 100, 24, 1, 2, 48);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (1, 1, 48, 8, 99, 24);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (1, 1, 49, 8, 99, 25);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (1, 1, 50, 8, 99, 26);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (1, 1, 51, 8, 99, 27);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (8, 1, 49, 28, 99, 50);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (8, 1, 50, 28, 99, 51);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (8, 1, 51, 28, 99, 52);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (8, 1, 52, 28, 99, 53);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (28, 100, 50, 8, 2, 49);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (28, 100, 51, 8, 2, 50);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (28, 100, 52, 8, 2, 51);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (28, 100, 53, 8, 2, 52);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (28, 91, 50, 28, 9, 37);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (28, 91, 51, 28, 10, 37);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (28, 91, 52, 28, 11, 37);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (28, 91, 53, 28, 12, 37);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (28, 12, 36, 28, 92, 53);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (28, 11, 36, 28, 92, 52);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (28, 10, 36, 28, 92, 51);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (28, 9, 36, 28, 92, 50);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (28, 54, 72, 28, 61, 23);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (28, 55, 72, 28, 62, 23);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (28, 55, 73, 28, 63, 23);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (28, 54, 73, 28, 64, 23);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (28, 61, 24, 28, 54, 71);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (28, 62, 24, 28, 54, 71);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (28, 63, 24, 28, 55, 71);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (28, 64, 24, 28, 55, 71);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (16, 50, 100, 1, 64, 2);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (16, 49, 100, 1, 64, 2);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (16, 48, 100, 1, 63, 2);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (1, 63, 1, 16, 48, 99);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (1, 64, 1, 16, 49, 99);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (2, 10, 9, 8, 62, 7);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (8, 62, 5, 2, 10, 11);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (7, 1, 51, 1, 99, 52);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (1, 100, 52, 7, 2, 51);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (7, 1, 52, 1, 99, 53);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (1, 100, 53, 7, 2, 52);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (25, 44, 1, 1, 67, 99);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (25, 45, 1, 1, 68, 99);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (25, 46, 1, 1, 69, 99);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (1, 69, 100, 25, 46, 2);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (1, 68, 100, 25, 45, 2);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (1, 67, 100, 25, 44, 2);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (14, 48, 1, 25, 34, 99);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (14, 49, 1, 25, 35, 99);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (14, 50, 1, 25, 36, 99);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (25, 36, 100, 14, 50, 2);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (25, 35, 100, 14, 49, 2);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (25, 34, 100, 14, 48, 2);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (10, 100, 40, 14, 2, 22);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (10, 100, 41, 14, 2, 23);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (10, 100, 42, 14, 2, 24);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (14, 1, 24, 10, 99, 42);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (14, 1, 23, 10, 99, 41);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (14, 1, 22, 10, 99, 40);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (8, 65, 100, 10, 29, 2);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (8, 66, 100, 10, 29, 2);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (8, 67, 100, 10, 30, 2);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (10, 31, 1, 8, 67, 99);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (10, 30, 1, 8, 66, 99);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (10, 29, 1, 8, 65, 99);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (8, 68, 100, 10, 30, 2);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (27, 51, 1, 10, 34, 99);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (27, 52, 1, 10, 35, 99);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (27, 53, 1, 10, 36, 99);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (10, 36, 100, 27, 53, 2);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (10, 34, 100, 27, 51, 2);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (10, 33, 100, 27, 51, 2);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (10, 37, 100, 27, 53, 2);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (3, 50, 100, 1, 30, 2);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (3, 51, 100, 1, 31, 2);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (3, 52, 100, 1, 32, 2);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (1, 30, 1, 3, 50, 99);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (1, 31, 1, 3, 51, 99);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (1, 32, 1, 3, 52, 99);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (6, 100, 99, 3, 50, 55);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (6, 100, 98, 3, 51, 55);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (6, 100, 97, 3, 52, 55);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (6, 100, 96, 3, 53, 55);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (3, 53, 54, 6, 99, 96);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (3, 51, 54, 6, 99, 98);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (3, 50, 54, 6, 99, 99);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (3, 52, 54, 6, 99, 97);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (9, 1, 3, 6, 99, 74);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (9, 1, 4, 6, 99, 75);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (9, 1, 5, 6, 99, 76);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (9, 1, 6, 6, 99, 77);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (6, 100, 74, 9, 2, 3);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (6, 100, 75, 9, 2, 4);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (6, 100, 76, 9, 2, 5);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (6, 100, 77, 9, 2, 6);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (18, 98, 49, 16, 2, 49);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (18, 98, 50, 16, 2, 50);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (18, 98, 51, 16, 2, 51);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (16, 1, 49, 18, 97, 49);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (16, 1, 50, 18, 97, 50);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (16, 1, 51, 18, 97, 51);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (16, 100, 50, 19, 2, 46);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (19, 1, 46, 16, 99, 50);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (17, 49, 100, 16, 49, 2);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (17, 50, 100, 16, 50, 2);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (17, 51, 100, 16, 51, 2);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (16, 49, 1, 17, 49, 99);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (16, 50, 1, 17, 50, 99);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (16, 51, 1, 17, 51, 99);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (16, 50, 36, 36, 96, 99);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (36, 96, 100, 16, 50, 37);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (36, 97, 100, 16, 51, 37);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (36, 95, 100, 16, 49, 37);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (13, 92, 100, 11, 82, 2);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (13, 93, 100, 11, 83, 2);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (13, 94, 100, 11, 84, 2);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (11, 82, 1, 13, 92, 99);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (11, 83, 1, 13, 93, 99);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (11, 84, 1, 13, 94, 99);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (11, 87, 54, 11, 7, 98);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (11, 6, 98, 11, 83, 52);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (11, 6, 99, 11, 84, 52);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (1, 21, 25, 36, 22, 15);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (36, 21, 16, 1, 21, 27);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (1, 23, 25, 36, 24, 15);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (1, 22, 25, 36, 23, 15);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (36, 22, 16, 1, 21, 27);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (36, 23, 16, 1, 22, 27);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (36, 24, 16, 1, 23, 27);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (36, 6, 48, 1, 22, 48);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (36, 7, 48, 1, 23, 48);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (36, 5, 48, 1, 22, 48);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (36, 8, 48, 1, 23, 48);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (1, 22, 46, 36, 6, 46);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (1, 23, 46, 36, 7, 46);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (1, 12, 46, 36, 52, 13);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (1, 13, 46, 36, 53, 13);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (36, 51, 15, 1, 12, 48);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (36, 52, 15, 1, 12, 48);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (36, 53, 15, 1, 13, 48);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (36, 54, 15, 1, 13, 48);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (1, 54, 98, 36, 6, 98);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (1, 55, 98, 36, 6, 98);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (1, 56, 98, 36, 7, 98);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (36, 5, 100, 1, 54, 100);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (36, 6, 100, 1, 55, 100);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (36, 7, 100, 1, 55, 100);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (36, 8, 100, 1, 56, 100);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (1, 61, 82, 36, 61, 81);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (36, 60, 83, 1, 60, 84);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (36, 61, 83, 1, 61, 84);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (36, 62, 83, 1, 61, 84);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (36, 63, 83, 1, 62, 84);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (36, 6, 75, 1, 37, 81);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (36, 7, 75, 1, 37, 81);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (36, 8, 75, 1, 38, 81);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (36, 5, 75, 1, 36, 81);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (1, 37, 79, 36, 6, 73);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (1, 60, 51, 36, 38, 46);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (36, 38, 48, 1, 60, 53);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (36, 39, 48, 1, 60, 53);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (36, 40, 48, 1, 61, 53);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (36, 37, 48, 1, 59, 53);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (1, 59, 51, 36, 38, 46);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (1, 61, 51, 36, 39, 46);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (15, 9, 3, 15, 73, 36);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (15, 73, 38, 15, 9, 5);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (15, 5, 3, 15, 34, 36);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (15, 34, 38, 15, 5, 5);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (15, 35, 38, 15, 6, 5);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (11, 10, 3, 11, 85, 98);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (11, 9, 3, 11, 84, 98);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (11, 11, 3, 11, 86, 98);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (11, 85, 100, 11, 10, 5);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (11, 84, 100, 11, 9, 5);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (11, 86, 100, 11, 11, 5);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (12, 1, 19, 11, 98, 21);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (12, 1, 20, 11, 98, 22);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (12, 1, 21, 11, 98, 23);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (11, 100, 21, 12, 2, 19);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (11, 100, 22, 12, 2, 20);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (11, 100, 23, 12, 2, 21);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (12, 86, 15, 12, 96, 97);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (12, 96, 98, 12, 86, 14);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (12, 97, 98, 12, 87, 15);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (9, 1, 34, 9, 42, 19);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (9, 41, 21, 9, 2, 29);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (9, 42, 21, 9, 3, 29);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (9, 43, 21, 9, 4, 29);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (29, 50, 94, 35, 78, 17);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (35, 77, 15, 29, 49, 93);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (35, 78, 15, 29, 50, 93);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (35, 79, 15, 29, 51, 93);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (35, 79, 43, 1, 50, 61);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (35, 80, 43, 1, 51, 61);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (1, 40, 50, 36, 35, 98);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (36, 35, 100, 1, 40, 51);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (36, 36, 100, 1, 41, 51);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (36, 37, 100, 1, 41, 51);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (36, 34, 100, 1, 39, 51);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (24, 37, 81, 24, 49, 94);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (24, 49, 96, 24, 35, 81);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (24, 64, 81, 24, 52, 94);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (24, 52, 96, 24, 66, 81);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (24, 96, 94, 29, 55, 15);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (29, 53, 15, 24, 96, 92);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (29, 48, 6, 24, 5, 92);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (24, 5, 94, 29, 46, 6);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (24, 37, 67, 24, 4, 45);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (24, 4, 43, 24, 39, 67);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (24, 64, 67, 24, 97, 45);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (24, 97, 43, 24, 62, 67);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (24, 56, 42, 24, 73, 40);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (24, 73, 38, 24, 56, 40);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (24, 28, 37, 24, 45, 40);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (24, 45, 42, 24, 28, 39);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (2, 99, 89, 2, 29, 25);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (2, 29, 22, 2, 97, 90);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (2, 9, 69, 2, 95, 8);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (2, 91, 8, 2, 11, 73);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (2, 92, 8, 2, 11, 73);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (2, 92, 9, 2, 11, 73);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (2, 91, 9, 2, 11, 73);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (42, 42, 100, 8, 37, 3);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (42, 43, 100, 8, 38, 3);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (8, 37, 1, 42, 42, 98);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (42, 44, 100, 8, 39, 3);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (8, 38, 1, 42, 43, 98);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (42, 45, 100, 8, 40, 3);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (8, 39, 1, 42, 44, 98);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (42, 46, 100, 8, 41, 3);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (8, 40, 1, 42, 45, 98);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (8, 41, 1, 42, 46, 98);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (42, 54, 2, 43, 53, 98);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (43, 53, 100, 42, 54, 4);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (42, 63, 2, 43, 54, 98);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (43, 54, 100, 42, 63, 4);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (43, 55, 100, 42, 71, 4);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (42, 71, 2, 43, 55, 98);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (31, 76, 56, 31, 61, 53);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (31, 60, 52, 31, 63, 53);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (31, 62, 52, 31, 61, 54);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (31, 60, 53, 31, 63, 54);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (31, 62, 53, 31, 61, 55);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (31, 60, 54, 31, 62, 55);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (31, 61, 54, 31, 61, 56);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (31, 60, 55, 31, 63, 56);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (31, 62, 55, 31, 61, 57);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (31, 60, 56, 31, 63, 57);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (31, 62, 56, 31, 65, 54);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (31, 64, 53, 31, 67, 54);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (31, 68, 53, 31, 71, 54);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (31, 66, 53, 31, 65, 55);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (31, 64, 54, 31, 67, 55);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (31, 66, 54, 31, 65, 56);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (31, 64, 55, 31, 67, 56);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (31, 66, 55, 31, 65, 57);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (31, 64, 56, 31, 67, 57);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (31, 66, 56, 31, 69, 54);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (31, 70, 53, 31, 69, 55);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (31, 68, 54, 31, 71, 55);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (31, 70, 54, 31, 69, 56);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (31, 68, 55, 31, 71, 56);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (31, 70, 55, 31, 69, 57);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (31, 68, 56, 31, 71, 57);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (31, 70, 56, 31, 73, 53);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (31, 72, 52, 31, 75, 53);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (31, 74, 52, 31, 73, 54);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (31, 72, 53, 31, 75, 54);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (31, 74, 53, 31, 73, 55);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (31, 72, 54, 31, 74, 55);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (31, 73, 54, 31, 73, 56);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (31, 72, 55, 31, 75, 56);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (31, 74, 55, 31, 73, 57);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (31, 72, 56, 31, 75, 57);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (31, 74, 57, 31, 77, 54);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (31, 77, 53, 31, 77, 55);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (31, 74, 56, 31, 77, 54);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (31, 76, 53, 31, 78, 54);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (31, 76, 54, 31, 78, 56);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (31, 77, 55, 31, 78, 57);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (36, 83, 12, 1, 22, 80);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (36, 84, 12, 1, 23, 80);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (36, 85, 12, 1, 24, 80);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (1, 23, 79, 36, 84, 11);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (35, 87, 20, 32, 34, 49);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (32, 35, 49, 35, 87, 21);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (35, 89, 20, 32, 48, 49);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (32, 47, 49, 35, 89, 21);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (35, 87, 16, 33, 43, 43);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (33, 43, 42, 35, 87, 17);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (35, 89, 16, 33, 58, 43);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (33, 58, 42, 35, 89, 17);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (35, 87, 12, 34, 54, 49);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (34, 54, 50, 35, 87, 13);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (34, 55, 50, 35, 87, 13);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (35, 89, 12, 34, 52, 60);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (34, 52, 59, 35, 89, 13);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (34, 53, 59, 35, 89, 13);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (43, 43, 26, 44, 57, 91);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (44, 57, 91, 43, 43, 26);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (24, 50, 4, 29, 54, 62);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (24, 51, 4, 29, 56, 62);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (29, 54, 63, 24, 50, 5);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (29, 55, 63, 24, 50, 5);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (29, 56, 63, 24, 51, 5);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (25, 1, 53, 38, 99, 23);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (25, 1, 54, 38, 99, 24);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (25, 1, 55, 38, 99, 25);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (25, 1, 56, 38, 99, 26);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (25, 1, 57, 38, 99, 27);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (38, 100, 23, 25, 2, 53);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (38, 100, 24, 25, 2, 54);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (38, 100, 25, 25, 2, 55);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (38, 100, 26, 25, 2, 56);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (38, 100, 27, 25, 2, 57);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (38, 37, 35, 39, 5, 98);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (38, 38, 35, 39, 6, 98);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (38, 39, 35, 39, 7, 98);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (39, 5, 99, 38, 37, 36);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (39, 6, 99, 38, 38, 36);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (39, 7, 99, 38, 39, 36);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (39, 40, 4, 39, 93, 99);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (39, 41, 4, 39, 94, 99);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (39, 42, 4, 39, 95, 99);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (39, 93, 100, 39, 40, 5);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (39, 94, 100, 39, 41, 5);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (39, 95, 100, 39, 42, 5);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (29, 59, 48, 40, 19, 98);
INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y)
VALUES (40, 17, 98, 29, 60, 48);


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

INSERT INTO maps (map_id, map_name, map_filename, bind_enabled)
VALUES (1, 'Minita', 'Map10001.map', '1');
INSERT INTO maps (map_id, map_name, map_filename)
VALUES (2, 'Its the Bat Cave Robin', 'Map10002.map');
INSERT INTO maps (map_id, map_name, map_filename)
VALUES (3, 'Graveyard', 'Map10003.map');
INSERT INTO maps (map_id, map_name, map_filename, pvp_enabled, pets_enabled)
VALUES (4, 'Forest Arena', 'Map10004.map', '1', '0');
INSERT INTO maps (map_id, map_name, map_filename)
VALUES (5, 'GM Paradise', 'Map10005.map');
INSERT INTO maps (map_id, map_name, map_filename)
VALUES (6, 'Undead Hallway', 'Map10006.map');
INSERT INTO maps (map_id, map_name, map_filename)
VALUES (7, 'Slime Kingdom', 'Map10007.map');
INSERT INTO maps (map_id, map_name, map_filename, bind_enabled)
VALUES (8, 'Roadkill Woods', 'Map10008.map', '1');
INSERT INTO maps (map_id, map_name, map_filename)
VALUES (9, 'Punchys Playhouse', 'Map10009.map');
INSERT INTO maps (map_id, map_name, map_filename)
VALUES (10, 'Dead Forest', 'Map10010.map');
INSERT INTO maps (map_id, map_name, map_filename)
VALUES (11, 'Otherlands', 'Map10011.map');
INSERT INTO maps (map_id, map_name, map_filename)
VALUES (12, 'Mindless Mines', 'Map10012.map');
INSERT INTO maps (map_id, map_name, map_filename)
VALUES (13, 'Hay and Frays Stronghold', 'Map10013.map');
INSERT INTO maps (map_id, map_name, map_filename)
VALUES (14, 'Chiisaiyama', 'Map10014.map');
INSERT INTO maps (map_id, map_name, map_filename, pvp_enabled, pets_enabled)
VALUES (15, 'Battle Fields', 'Map10015.map', '1', '0');
INSERT INTO maps (map_id, map_name, map_filename)
VALUES (16, 'Arctic Lands', 'Map10016.map');
INSERT INTO maps (map_id, map_name, map_filename)
VALUES (17, 'Northern Arctic Lands', 'Map10017.map');
INSERT INTO maps (map_id, map_name, map_filename)
VALUES (18, 'Frozen Lake', 'Map10018.map');
INSERT INTO maps (map_id, map_name, map_filename)
VALUES (19, 'Frigid Maze', 'Map10019.map');
INSERT INTO maps (map_id, map_name, map_filename)
VALUES (20, 'Tower of the Ancients', 'Map10020.map');
INSERT INTO maps (map_id, map_name, map_filename)
VALUES (21, '21', 'Map10021.map');
INSERT INTO maps (map_id, map_name, map_filename, pvp_enabled)
VALUES (22, '22', 'Map10022.map', '1');
INSERT INTO maps (map_id, map_name, map_filename)
VALUES (23, '23', 'Map10023.map');
INSERT INTO maps (map_id, map_name, map_filename, min_experience, pvp_enabled, pets_enabled)
VALUES (24, 'The Ancients Dungeon', 'Map10024.map', 100000000, '1', '0');
INSERT INTO maps (map_id, map_name, map_filename)
VALUES (25, 'Boondocks', 'Map10025.map');
INSERT INTO maps (map_id, map_name, map_filename)
VALUES (26, '26', 'Map10026.map');
INSERT INTO maps (map_id, map_name, map_filename)
VALUES (27, 'Nagan Oasis', 'Map10027.map');
INSERT INTO maps (map_id, map_name, map_filename)
VALUES (28, 'Savage Isle', 'Map10028.map');
INSERT INTO maps (map_id, map_name, map_filename, min_experience, pvp_enabled, pets_enabled)
VALUES (29, 'The Ancients Dungeon', 'Map10029.map', 20000000, '1', '0');
INSERT INTO maps (map_id, map_name, map_filename)
VALUES (30, 'Where am I?', 'Map10030.map');
INSERT INTO maps (map_id, map_name, map_filename)
VALUES (31, 'Event Hall', 'Map10031.map');
INSERT INTO maps (map_id, map_name, map_filename, min_experience)
VALUES (32, 'Sorrows Grove', 'Map10032.map', 10000000);
INSERT INTO maps (map_id, map_name, map_filename, min_experience)
VALUES (33, 'The Passing', 'Map10033.map', 60000000);
INSERT INTO maps (map_id, map_name, map_filename, min_experience)
VALUES (34, 'Winter Heights', 'Map10034.map', 300000000);
INSERT INTO maps (map_id, map_name, map_filename)
VALUES (35, 'Paradise', 'Map10035.map');
INSERT INTO maps (map_id, map_name, map_filename)
VALUES (36, 'Shops', 'Map10036.map');
INSERT INTO maps (map_id, map_name, map_filename)
VALUES (37, 'Marketplace', 'Map10037.map');
INSERT INTO maps (map_id, map_name, map_filename)
VALUES (38, 'Pumpkin Grove', 'Map10038.map');
INSERT INTO maps (map_id, map_name, map_filename, min_level)
VALUES (39, 'Haunted House', 'Map10039.map', 40);
INSERT INTO maps (map_id, map_name, map_filename, min_experience, pvp_enabled, pets_enabled)
VALUES (40, 'The Ancients Dungeon', 'Map10040.map', 400000000, '1', '0');
INSERT INTO maps (map_id, map_name, map_filename)
VALUES (41, 'Hall of Heroes', 'Map10041.map');
INSERT INTO maps (map_id, map_name, map_filename, min_experience)
VALUES (42, 'Rugged Valley', 'Map10042.map', 100000000);
INSERT INTO maps (map_id, map_name, map_filename, min_experience)
VALUES (43, 'Bear Kingdom', 'Map10043.map', 200000000);
INSERT INTO maps (map_id, map_name, map_filename)
VALUES (44, 'Starving Pits', 'Map10044.map');


DROP TABLE IF EXISTS map_required_items;
CREATE TABLE map_required_items (
  map_id INT NOT NULL,
  item_template_id INT NOT NULL
);

CREATE INDEX map_required_items_map_id_idx ON map_required_items(map_id);

INSERT INTO map_required_items (map_id, item_template_id)
VALUES (29, 266);
INSERT INTO map_required_items (map_id, item_template_id)
VALUES (43, 458);
INSERT INTO map_required_items (map_id, item_template_id)
VALUES (40, 516);


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

INSERT INTO combinations (combination_id, combination_name)
VALUES (1, 'Cloth');
INSERT INTO combinations (combination_id, combination_name)
VALUES (2, 'Cloth');
INSERT INTO combinations (combination_id, combination_name)
VALUES (3, 'Cloth');
INSERT INTO combinations (combination_id, combination_name)
VALUES (4, 'Cloth');
INSERT INTO combinations (combination_id, combination_name)
VALUES (5, 'Cloth');
INSERT INTO combinations (combination_id, combination_name)
VALUES (6, 'Cloth');
INSERT INTO combinations (combination_id, combination_name)
VALUES (7, 'Cloth Shirt');
INSERT INTO combinations (combination_id, combination_name)
VALUES (8, 'Practice Katana');
INSERT INTO combinations (combination_id, combination_name)
VALUES (9, 'Soft Belt');
INSERT INTO combinations (combination_id, combination_name)
VALUES (10, 'Cat Ears');
INSERT INTO combinations (combination_id, combination_name)
VALUES (11, 'Black Cat Ears');
INSERT INTO combinations (combination_id, combination_name)
VALUES (12, 'Bonfire');
INSERT INTO combinations (combination_id, combination_name)
VALUES (13, 'Low Quality Walde');
INSERT INTO combinations (combination_id, combination_name)
VALUES (14, 'Medium Quality Walde');
INSERT INTO combinations (combination_id, combination_name)
VALUES (15, 'High Quality Walde');
INSERT INTO combinations (combination_id, combination_name)
VALUES (16, 'Scroll: Bat Illusion');
INSERT INTO combinations (combination_id, combination_name)
VALUES (17, 'Crude Gold Ring');
INSERT INTO combinations (combination_id, combination_name)
VALUES (18, 'Crude Pearl Ring');
INSERT INTO combinations (combination_id, combination_name)
VALUES (19, 'Crude Ruby Ring');
INSERT INTO combinations (combination_id, combination_name)
VALUES (20, 'Fighting Katana');
INSERT INTO combinations (combination_id, combination_name)
VALUES (21, 'Coral Sword');
INSERT INTO combinations (combination_id, combination_name)
VALUES (22, 'Harvest Medallion');
INSERT INTO combinations (combination_id, combination_name)
VALUES (23, 'Scroll: Smokebomb');
INSERT INTO combinations (combination_id, combination_name)
VALUES (24, 'Scroll: Warrior Root');
INSERT INTO combinations (combination_id, combination_name)
VALUES (25, 'Scroll: Covenant');
INSERT INTO combinations (combination_id, combination_name)
VALUES (26, 'Scroll: Group Heal');
INSERT INTO combinations (combination_id, combination_name)
VALUES (27, 'Scroll: Ancient Damage');
INSERT INTO combinations (combination_id, combination_name)
VALUES (28, 'Scroll: Ancient Augmentation');
INSERT INTO combinations (combination_id, combination_name)
VALUES (29, 'Scroll: Ancient Regeneration');
INSERT INTO combinations (combination_id, combination_name)
VALUES (30, 'Scroll: Ancient Sacrifice');
INSERT INTO combinations (combination_id, combination_name)
VALUES (31, 'Empty Box');
INSERT INTO combinations (combination_id, combination_name)
VALUES (32, 'Magus Moon Shield');
INSERT INTO combinations (combination_id, combination_name)
VALUES (33, 'Rogue Moon Shield');
INSERT INTO combinations (combination_id, combination_name)
VALUES (34, 'Warrior Moon Shield');
INSERT INTO combinations (combination_id, combination_name)
VALUES (35, 'Priest Moon Shield');
INSERT INTO combinations (combination_id, combination_name)
VALUES (36, 'Scroll: Shroom Illusion');
INSERT INTO combinations (combination_id, combination_name)
VALUES (37, 'Pearl Bracelet');
INSERT INTO combinations (combination_id, combination_name)
VALUES (38, 'Ducky Pauldrons');
INSERT INTO combinations (combination_id, combination_name)
VALUES (39, 'Magus Ancient Slippers');
INSERT INTO combinations (combination_id, combination_name)
VALUES (40, 'Rogue Ancient Boots');
INSERT INTO combinations (combination_id, combination_name)
VALUES (41, 'Warrior Ancient Boots');
INSERT INTO combinations (combination_id, combination_name)
VALUES (42, 'Priest Ancient Slippers');
INSERT INTO combinations (combination_id, combination_name)
VALUES (43, 'Magus Divine Crown');
INSERT INTO combinations (combination_id, combination_name)
VALUES (44, 'Rogue Divine Helm');
INSERT INTO combinations (combination_id, combination_name)
VALUES (45, 'Warrior Divine Helm');
INSERT INTO combinations (combination_id, combination_name)
VALUES (46, 'Priest Divine Crown');
INSERT INTO combinations (combination_id, combination_name)
VALUES (47, 'Gero Necklace');
INSERT INTO combinations (combination_id, combination_name)
VALUES (48, 'Bling Belt');
INSERT INTO combinations (combination_id, combination_name)
VALUES (49, 'Enchanted Gloves');
INSERT INTO combinations (combination_id, combination_name)
VALUES (50, 'Magus Royal Leggings');
INSERT INTO combinations (combination_id, combination_name)
VALUES (51, 'Rogue Royal Legplates');
INSERT INTO combinations (combination_id, combination_name)
VALUES (52, 'Warrior Royal Legplates');
INSERT INTO combinations (combination_id, combination_name)
VALUES (53, 'Priest Royal Leggings');
INSERT INTO combinations (combination_id, combination_name)
VALUES (54, 'Magus Royal Tunic');
INSERT INTO combinations (combination_id, combination_name)
VALUES (55, 'Rogue Royal Chestplate');
INSERT INTO combinations (combination_id, combination_name)
VALUES (56, 'Warrior Royal Chestplate');
INSERT INTO combinations (combination_id, combination_name)
VALUES (57, 'Priest Royal Tunic');
INSERT INTO combinations (combination_id, combination_name)
VALUES (58, 'Key to the Ancients Dungeon');
INSERT INTO combinations (combination_id, combination_name)
VALUES (59, 'Enchanted Bracelet of Fire');
INSERT INTO combinations (combination_id, combination_name)
VALUES (60, 'Enchanted Bracelet of Earth');
INSERT INTO combinations (combination_id, combination_name)
VALUES (61, 'Enchanted Bracelet of Air');
INSERT INTO combinations (combination_id, combination_name)
VALUES (62, 'Enchanted Bracelet of Water');
INSERT INTO combinations (combination_id, combination_name)
VALUES (63, 'Enchanted Bracelet of Spirit');
INSERT INTO combinations (combination_id, combination_name)
VALUES (64, 'Rogue Enchanted Divine Helm');
INSERT INTO combinations (combination_id, combination_name)
VALUES (65, 'Warrior Enchanted Divine Helm');
INSERT INTO combinations (combination_id, combination_name)
VALUES (66, 'Magus Enchanted Divine Crown');
INSERT INTO combinations (combination_id, combination_name)
VALUES (67, 'Priest Enchanted Divine Crown');
INSERT INTO combinations (combination_id, combination_name)
VALUES (68, 'HP Regeneration Potion');
INSERT INTO combinations (combination_id, combination_name)
VALUES (69, 'MP Regeneration Potion');
INSERT INTO combinations (combination_id, combination_name)
VALUES (70, 'Haste Potion');
INSERT INTO combinations (combination_id, combination_name)
VALUES (71, 'Spell Damage Potion');
INSERT INTO combinations (combination_id, combination_name)
VALUES (72, 'Spell Critical Potion');


DROP TABLE IF EXISTS combination_item_required;
CREATE TABLE combination_item_required (
	combination_id INT NOT NULL,
	item_template_id INT NOT NULL
);

INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (1, 340);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (1, 348);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (2, 340);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (2, 349);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (3, 340);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (3, 350);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (4, 340);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (4, 351);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (5, 340);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (5, 352);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (6, 340);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (6, 353);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (7, 449);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (7, 346);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (7, 340);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (7, 347);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (8, 449);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (8, 344);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (8, 339);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (8, 338);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (9, 8);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (9, 345);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (10, 350);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (10, 355);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (10, 340);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (10, 455);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (11, 351);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (11, 355);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (11, 340);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (11, 455);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (12, 330);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (12, 3);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (13, 338);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (13, 339);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (13, 342);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (13, 332);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (14, 336);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (14, 337);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (14, 342);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (14, 332);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (15, 334);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (15, 335);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (15, 342);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (15, 332);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (16, 333);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (16, 328);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (16, 341);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (17, 445);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (17, 331);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (18, 358);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (18, 343);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (19, 358);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (19, 60);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (20, 451);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (20, 354);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (20, 332);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (20, 58);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (21, 267);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (21, 335);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (21, 332);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (21, 342);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (21, 334);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (21, 58);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (22, 331);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (22, 445);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (22, 457);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (22, 444);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (22, 332);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (23, 263);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (23, 328);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (23, 341);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (23, 58);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (24, 264);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (24, 328);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (24, 341);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (24, 58);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (25, 262);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (25, 328);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (25, 341);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (25, 58);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (26, 265);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (26, 328);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (26, 341);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (26, 58);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (27, 298);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (27, 328);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (27, 341);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (27, 58);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (28, 299);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (28, 328);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (28, 341);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (28, 58);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (29, 297);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (29, 328);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (29, 341);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (29, 58);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (30, 300);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (30, 328);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (30, 341);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (30, 58);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (31, 268);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (31, 269);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (31, 270);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (31, 271);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (32, 297);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (32, 303);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (32, 115);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (32, 58);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (33, 298);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (33, 303);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (33, 115);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (33, 58);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (34, 299);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (34, 303);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (34, 115);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (34, 58);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (35, 300);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (35, 303);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (35, 115);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (35, 58);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (36, 20);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (36, 328);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (36, 341);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (37, 347);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (37, 343);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (37, 343);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (37, 343);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (37, 343);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (37, 343);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (37, 343);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (37, 343);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (37, 343);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (38, 443);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (38, 442);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (38, 21);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (39, 297);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (39, 395);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (39, 436);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (39, 438);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (39, 437);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (39, 58);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (40, 298);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (40, 396);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (40, 439);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (40, 440);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (40, 441);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (40, 58);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (41, 299);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (41, 396);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (41, 439);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (41, 440);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (41, 441);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (41, 58);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (42, 300);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (42, 395);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (42, 436);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (42, 438);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (42, 437);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (42, 58);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (43, 297);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (43, 397);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (43, 436);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (43, 438);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (43, 437);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (43, 58);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (44, 298);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (44, 398);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (44, 439);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (44, 440);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (44, 441);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (44, 58);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (45, 299);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (45, 398);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (45, 439);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (45, 440);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (45, 441);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (45, 58);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (46, 300);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (46, 397);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (46, 436);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (46, 438);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (46, 437);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (46, 58);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (47, 448);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (47, 447);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (47, 446);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (48, 452);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (48, 340);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (48, 347);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (48, 51);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (48, 445);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (48, 331);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (49, 20);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (49, 19);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (49, 142);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (49, 54);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (49, 345);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (49, 351);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (49, 340);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (50, 297);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (50, 490);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (50, 436);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (50, 438);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (50, 437);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (50, 58);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (51, 298);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (51, 491);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (51, 439);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (51, 440);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (51, 441);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (51, 58);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (52, 299);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (52, 491);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (52, 439);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (52, 440);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (52, 441);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (52, 58);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (53, 300);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (53, 490);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (53, 436);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (53, 438);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (53, 437);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (53, 58);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (54, 297);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (54, 492);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (54, 436);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (54, 438);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (54, 437);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (54, 58);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (55, 298);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (55, 493);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (55, 439);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (55, 440);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (55, 441);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (55, 58);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (56, 299);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (56, 493);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (56, 439);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (56, 440);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (56, 441);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (56, 58);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (57, 300);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (57, 492);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (57, 436);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (57, 438);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (57, 437);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (57, 58);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (58, 512);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (58, 513);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (58, 514);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (58, 515);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (58, 331);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (59, 299);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (59, 304);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (59, 585);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (60, 297);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (60, 305);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (60, 585);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (61, 300);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (61, 306);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (61, 585);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (62, 298);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (62, 307);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (62, 585);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (63, 58);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (63, 308);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (63, 585);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (64, 58);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (64, 298);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (64, 405);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (64, 591);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (65, 58);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (65, 299);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (65, 406);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (65, 591);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (66, 58);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (66, 297);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (66, 407);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (66, 591);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (67, 58);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (67, 300);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (67, 408);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (67, 591);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (68, 621);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (68, 489);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (68, 623);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (69, 621);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (69, 489);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (69, 624);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (70, 621);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (70, 489);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (70, 625);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (71, 621);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (71, 489);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (71, 622);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (71, 626);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (72, 621);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (72, 489);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (72, 622);
INSERT INTO combination_item_required (combination_id, item_template_id)
VALUES (72, 627);


DROP TABLE IF EXISTS combination_item_results;
CREATE TABLE combination_item_results (
	combination_id INT NOT NULL,
	item_template_id INT NOT NULL
);

INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (1, 449);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (2, 449);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (3, 449);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (4, 449);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (5, 449);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (6, 449);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (7, 450);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (8, 451);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (9, 452);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (10, 453);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (11, 454);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (12, 331);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (13, 363);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (14, 362);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (15, 361);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (16, 329);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (17, 358);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (18, 357);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (19, 359);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (20, 456);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (21, 251);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (22, 141);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (23, 323);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (24, 325);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (25, 272);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (26, 324);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (27, 320);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (28, 317);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (29, 475);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (30, 322);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (31, 443);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (32, 293);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (33, 295);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (34, 296);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (35, 294);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (36, 356);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (37, 360);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (38, 435);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (39, 403);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (40, 401);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (41, 402);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (42, 404);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (43, 407);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (44, 405);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (45, 406);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (46, 408);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (47, 458);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (48, 479);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (49, 480);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (50, 497);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (51, 494);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (52, 495);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (53, 496);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (54, 501);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (55, 498);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (56, 499);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (57, 500);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (58, 516);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (59, 580);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (60, 581);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (61, 582);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (62, 583);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (63, 584);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (64, 587);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (65, 588);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (66, 589);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (67, 590);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (68, 628);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (69, 629);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (70, 630);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (71, 631);
INSERT INTO combination_item_results (combination_id, item_template_id)
VALUES (72, 632);


DROP TABLE IF EXISTS item_titles;
CREATE TABLE item_titles (
  id INTEGER PRIMARY KEY,
  name TEXT NOT NULL,
  min_level INT DEFAULT 1,
  max_level INT DEFAULT 50,
  min_experience BIGINT DEFAULT 0,
  max_experience BIGINT DEFAULT 0,
  item_usetype SMALLINT DEFAULT 0,
  item_slot SMALLINT DEFAULT 20,
  chance DECIMAL(5,4) NOT NULL,
  script_path TEXT DEFAULT '' NOT NULL,
  script_params TEXT DEFAULT '' NOT NULL
);

INSERT INTO item_titles (id, name, chance, script_path, script_params)
VALUES (1, 'Powerful', 0.5, 'Scripts/Item/ItemModifierScript.csx', '[{ "type": "statMultiplier", "value": 2.0 }]');
INSERT INTO item_titles (id, name, chance, script_path, script_params)
VALUES (2, 'Strong', 0.5, 'Scripts/Item/ItemModifierScript.csx', '[{ "type": "statMultiplier", "value": 1.5 }]');
INSERT INTO item_titles (id, name, chance, script_path, script_params)
VALUES (3, 'Broken', 0.5, 'Scripts/Item/ItemModifierScript.csx', '[{ "type": "statMultiplier", "value": 0.5 }]');
INSERT INTO item_titles (id, name, item_usetype, chance, script_path, script_params)
VALUES (4, 'Sharp', 3, 0.5, 'Scripts/Item/ItemModifierScript.csx', '[{ "type": "weaponDamage", "min": 5, "max": 10 }]');


DROP TABLE IF EXISTS item_surnames;
CREATE TABLE item_surnames (
  id INTEGER PRIMARY KEY,
  name TEXT NOT NULL,
  min_level INT DEFAULT 1,
  max_level INT DEFAULT 50,
  min_experience BIGINT DEFAULT 0,
  max_experience BIGINT DEFAULT 0,
  item_usetype SMALLINT DEFAULT 0,
  item_slot SMALLINT DEFAULT 20,
  chance DECIMAL(5,4) NOT NULL,
  script_path TEXT DEFAULT '' NOT NULL,
  script_params TEXT DEFAULT '' NOT NULL
);

INSERT INTO item_surnames (id, name, chance, script_path, script_params)
VALUES (1, 'of Vitality', 0.5, 'Scripts/Item/ItemModifierScript.csx', '[{ "type": "hp", "min": 10, "max": 20 }]');
INSERT INTO item_surnames (id, name, chance, script_path, script_params)
VALUES (2, 'of Intelligence', 0.5, 'Scripts/Item/ItemModifierScript.csx', '[{ "type": "int", "min": 1, "max": 10 }]');
INSERT INTO item_surnames (id, name, item_usetype, chance, script_path, script_params)
VALUES (3, 'of the Bear', 3, 0.5, 'Scripts/Item/ItemModifierScript.csx', '[{ "type": "str", "min": 1, "max": 10 }]');
INSERT INTO item_surnames (id, name, item_usetype, chance, script_path, script_params)
VALUES (4, 'of the Turtle', 2, 0.5, 'Scripts/Item/ItemModifierScript.csx', '[{ "type": "ac", "min": 1, "max": 10 }]');
INSERT INTO item_surnames (id, name, chance, script_path, script_params)
VALUES (5, 'of Spell Damage', 0.5, 'Scripts/Item/ItemModifierScript.csx', '[{ "type": "spellDamage", "min": 1, "max": 10 }]');


DROP TABLE IF EXISTS classes;
CREATE TABLE classes (
  class_id INTEGER PRIMARY KEY,
  class_name TEXT NOT NULL,
  ac_multiplier DECIMAL(9,2) DEFAULT 1 NOT NULL,
  vita_cost BIGINT DEFAULT 200000 NOT NULL,
  mana_cost BIGINT DEFAULT 200000 NOT NULL
);

INSERT INTO classes (class_id, class_name, ac_multiplier, vita_cost, mana_cost)
VALUES (1, 'Commoner', 1, 10000, 10000);
INSERT INTO classes (class_id, class_name, ac_multiplier, vita_cost, mana_cost)
VALUES (2, 'Rogue', 0.65, 150000, 150000);
INSERT INTO classes (class_id, class_name, ac_multiplier, vita_cost, mana_cost)
VALUES (3, 'Warrior', 1, 100000, 200000);
INSERT INTO classes (class_id, class_name, ac_multiplier, vita_cost, mana_cost)
VALUES (4, 'Magus', 0.5, 200000, 100000);
INSERT INTO classes (class_id, class_name, ac_multiplier, vita_cost, mana_cost)
VALUES (5, 'Priest', 0.7, 180000, 120000);
INSERT INTO classes (class_id, class_name, ac_multiplier, vita_cost, mana_cost)
VALUES (6, 'Game Master', 1, 100000, 100000);


DROP TABLE IF EXISTS class_info;
CREATE TABLE class_info (
  class_id INT NOT NULL,
  level SMALLINT NOT NULL,
  level_up_exp BIGINT DEFAULT 0 NOT NULL,
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
  hp_percent_regen DECIMAL(9,4) DEFAULT 0 NOT NULL,
  hp_static_regen INT DEFAULT 0 NOT NULL,
  mp_percent_regen DECIMAL(9,4) DEFAULT 0 NOT NULL,
  mp_static_regen INT DEFAULT 0 NOT NULL,
  haste DECIMAL(9,4) DEFAULT 0 NOT NULL,
  spell_damage DECIMAL(9,4) DEFAULT 0 NOT NULL,
  spell_crit DECIMAL(9,4) DEFAULT 0 NOT NULL,
  melee_damage DECIMAL(9,4) DEFAULT 0 NOT NULL,
  melee_crit DECIMAL(9,4) DEFAULT 0 NOT NULL,
  damage_reduce DECIMAL(9,4) DEFAULT 0 NOT NULL
);

INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (1, 1, 200, 30, 30);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (1, 2, 800, 38, 38);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (1, 3, 2000, 50, 50);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (1, 4, 4000, 66, 66);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (1, 5, 0, 86, 86);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (2, 1, 200, 30, 30);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (2, 2, 800, 38, 38);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (2, 3, 2000, 50, 50);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (2, 4, 4000, 66, 66);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (2, 5, 7000, 86, 86);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (2, 6, 11200, 110, 110);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (2, 7, 16800, 138, 138);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (2, 8, 24000, 170, 170);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (2, 9, 33000, 206, 206);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (2, 10, 44000, 246, 246);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (2, 11, 57200, 290, 290);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (2, 12, 72800, 338, 338);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (2, 13, 91000, 390, 390);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (2, 14, 112000, 446, 446);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (2, 15, 136000, 506, 506);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (2, 16, 163200, 570, 570);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (2, 17, 193800, 638, 638);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (2, 18, 228000, 710, 710);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (2, 19, 266000, 786, 786);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (2, 20, 308000, 866, 866);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (2, 21, 354200, 950, 950);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (2, 22, 404800, 1038, 1038);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (2, 23, 460000, 1130, 1130);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (2, 24, 520000, 1226, 1226);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (2, 25, 585000, 1326, 1326);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (2, 26, 655200, 1430, 1430);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (2, 27, 730800, 1538, 1538);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (2, 28, 812000, 1650, 1650);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (2, 29, 899000, 1766, 1766);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (2, 30, 992000, 1886, 1886);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (2, 31, 1091200, 2010, 2010);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (2, 32, 1196800, 2138, 2138);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (2, 33, 1309000, 2270, 2270);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (2, 34, 1428000, 2406, 2406);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (2, 35, 1554000, 2546, 2546);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (2, 36, 1687200, 2690, 2690);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (2, 37, 1827800, 2838, 2838);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (2, 38, 1976000, 2990, 2990);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (2, 39, 2132000, 3146, 3146);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (2, 40, 2296000, 3306, 3306);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (2, 41, 2468200, 3470, 3470);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (2, 42, 2648800, 3638, 3638);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (2, 43, 2838000, 3810, 3810);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (2, 44, 3036000, 3986, 3986);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (2, 45, 3243000, 4166, 4166);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (2, 46, 3459200, 4350, 4350);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (2, 47, 3684800, 4538, 4538);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (2, 48, 3920000, 4730, 4730);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (2, 49, 4165000, 4926, 4926);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (2, 50, 0, 5200, 5200);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp, stat_ac)
VALUES (3, 1, 200, 30, 30, 10);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp, stat_ac)
VALUES (3, 2, 800, 42, 36, 20);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp, stat_ac)
VALUES (3, 3, 2000, 60, 45, 30);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp, stat_ac)
VALUES (3, 4, 4000, 84, 57, 40);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp, stat_ac)
VALUES (3, 5, 7000, 114, 72, 50);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp, stat_ac)
VALUES (3, 6, 11200, 150, 90, 60);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp, stat_ac)
VALUES (3, 7, 16800, 192, 111, 70);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp, stat_ac)
VALUES (3, 8, 24000, 240, 135, 80);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp, stat_ac)
VALUES (3, 9, 33000, 294, 162, 90);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp, stat_ac)
VALUES (3, 10, 44000, 354, 192, 100);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp, stat_ac)
VALUES (3, 11, 57200, 420, 225, 110);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp, stat_ac)
VALUES (3, 12, 72800, 492, 261, 120);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp, stat_ac)
VALUES (3, 13, 91000, 570, 300, 130);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp, stat_ac)
VALUES (3, 14, 112000, 654, 342, 140);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp, stat_ac)
VALUES (3, 15, 136000, 744, 387, 150);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp, stat_ac)
VALUES (3, 16, 163200, 840, 435, 160);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp, stat_ac)
VALUES (3, 17, 193800, 942, 486, 170);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp, stat_ac)
VALUES (3, 18, 228000, 1050, 540, 180);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp, stat_ac)
VALUES (3, 19, 266000, 1164, 597, 190);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp, stat_ac)
VALUES (3, 20, 308000, 1284, 657, 200);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp, stat_ac)
VALUES (3, 21, 354200, 1410, 720, 210);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp, stat_ac)
VALUES (3, 22, 404800, 1542, 786, 220);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp, stat_ac)
VALUES (3, 23, 460000, 1680, 855, 230);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp, stat_ac)
VALUES (3, 24, 520000, 1824, 927, 240);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp, stat_ac)
VALUES (3, 25, 585000, 1974, 1002, 250);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp, stat_ac)
VALUES (3, 26, 655200, 2130, 1080, 260);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp, stat_ac)
VALUES (3, 27, 730800, 2292, 1161, 270);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp, stat_ac)
VALUES (3, 28, 812000, 2460, 1245, 280);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp, stat_ac)
VALUES (3, 29, 899000, 2634, 1332, 290);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp, stat_ac)
VALUES (3, 30, 992000, 2814, 1422, 300);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp, stat_ac)
VALUES (3, 31, 1091200, 3000, 1515, 310);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp, stat_ac)
VALUES (3, 32, 1196800, 3192, 1611, 320);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp, stat_ac)
VALUES (3, 33, 1309000, 3390, 1710, 330);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp, stat_ac)
VALUES (3, 34, 1428000, 3594, 1812, 340);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp, stat_ac)
VALUES (3, 35, 1554000, 3804, 1917, 350);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp, stat_ac)
VALUES (3, 36, 1687200, 4020, 2025, 360);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp, stat_ac)
VALUES (3, 37, 1827800, 4242, 2136, 370);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp, stat_ac)
VALUES (3, 38, 1976000, 4470, 2250, 380);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp, stat_ac)
VALUES (3, 39, 2132000, 4704, 2367, 390);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp, stat_ac)
VALUES (3, 40, 2296000, 4944, 2487, 400);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp, stat_ac)
VALUES (3, 41, 2468200, 5190, 2610, 410);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp, stat_ac)
VALUES (3, 42, 2648800, 5442, 2736, 420);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp, stat_ac)
VALUES (3, 43, 2838000, 5700, 2865, 430);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp, stat_ac)
VALUES (3, 44, 3036000, 5964, 2997, 440);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp, stat_ac)
VALUES (3, 45, 3243000, 6234, 3132, 450);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp, stat_ac)
VALUES (3, 46, 3459200, 6510, 3270, 460);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp, stat_ac)
VALUES (3, 47, 3684800, 6792, 3411, 470);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp, stat_ac)
VALUES (3, 48, 3920000, 7080, 3555, 480);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp, stat_ac)
VALUES (3, 49, 4165000, 7374, 3702, 490);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp, stat_ac)
VALUES (3, 50, 0, 7700, 3900, 500);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (4, 1, 200, 30, 30);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (4, 2, 800, 36, 42);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (4, 3, 2000, 45, 60);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (4, 4, 4000, 57, 84);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (4, 5, 7000, 72, 114);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (4, 6, 11200, 90, 150);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (4, 7, 16800, 111, 192);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (4, 8, 24000, 135, 240);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (4, 9, 33000, 162, 294);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (4, 10, 44000, 192, 354);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (4, 11, 57200, 225, 420);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (4, 12, 72800, 261, 492);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (4, 13, 91000, 300, 570);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (4, 14, 112000, 342, 654);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (4, 15, 136000, 387, 744);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (4, 16, 163200, 435, 840);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (4, 17, 193800, 486, 942);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (4, 18, 228000, 540, 1050);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (4, 19, 266000, 597, 1164);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (4, 20, 308000, 657, 1284);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (4, 21, 354200, 720, 1410);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (4, 22, 404800, 786, 1542);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (4, 23, 460000, 855, 1680);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (4, 24, 520000, 927, 1824);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (4, 25, 585000, 1002, 1974);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (4, 26, 655200, 1080, 2130);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (4, 27, 730800, 1161, 2292);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (4, 28, 812000, 1245, 2460);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (4, 29, 899000, 1332, 2634);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (4, 30, 992000, 1422, 2814);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (4, 31, 1091200, 1515, 3000);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (4, 32, 1196800, 1611, 3192);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (4, 33, 1309000, 1710, 3390);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (4, 34, 1428000, 1812, 3594);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (4, 35, 1554000, 1917, 3804);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (4, 36, 1687200, 2025, 4020);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (4, 37, 1827800, 2136, 4242);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (4, 38, 1976000, 2250, 4470);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (4, 39, 2132000, 2367, 4704);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (4, 40, 2296000, 2487, 4944);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (4, 41, 2468200, 2610, 5190);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (4, 42, 2648800, 2736, 5442);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (4, 43, 2838000, 2865, 5700);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (4, 44, 3036000, 2997, 5964);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (4, 45, 3243000, 3132, 6234);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (4, 46, 3459200, 3270, 6510);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (4, 47, 3684800, 3411, 6792);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (4, 48, 3920000, 3555, 7080);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (4, 49, 4165000, 3700, 7400);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (4, 50, 0, 3852, 7674);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (5, 1, 200, 30, 30);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (5, 2, 800, 37, 40);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (5, 3, 2000, 47, 55);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (5, 4, 4000, 61, 75);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (5, 5, 7000, 78, 100);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (5, 6, 11200, 98, 130);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (5, 7, 16800, 122, 165);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (5, 8, 24000, 149, 205);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (5, 9, 33000, 179, 250);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (5, 10, 44000, 213, 300);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (5, 11, 57200, 250, 355);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (5, 12, 72800, 290, 415);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (5, 13, 91000, 334, 480);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (5, 14, 112000, 381, 550);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (5, 15, 136000, 431, 625);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (5, 16, 163200, 485, 705);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (5, 17, 193800, 542, 790);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (5, 18, 228000, 602, 880);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (5, 19, 266000, 666, 975);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (5, 20, 308000, 733, 1075);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (5, 21, 354200, 803, 1180);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (5, 22, 404800, 877, 1290);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (5, 23, 460000, 954, 1405);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (5, 24, 520000, 1034, 1525);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (5, 25, 585000, 1118, 1650);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (5, 26, 655200, 1205, 1780);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (5, 27, 730800, 1295, 1915);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (5, 28, 812000, 1389, 2055);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (5, 29, 899000, 1486, 2200);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (5, 30, 992000, 1586, 2350);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (5, 31, 1091200, 1690, 2505);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (5, 32, 1196800, 1797, 2665);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (5, 33, 1309000, 1907, 2830);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (5, 34, 1428000, 2021, 3000);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (5, 35, 1554000, 2138, 3175);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (5, 36, 1687200, 2258, 3355);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (5, 37, 1827800, 2382, 3540);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (5, 38, 1976000, 2509, 3730);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (5, 39, 2132000, 2639, 3925);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (5, 40, 2296000, 2773, 4125);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (5, 41, 2468200, 2910, 4330);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (5, 42, 2648800, 3050, 4540);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (5, 43, 2838000, 3194, 4755);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (5, 44, 3036000, 3341, 4975);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (5, 45, 3243000, 3491, 5200);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (5, 46, 3459200, 3645, 5430);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (5, 47, 3684800, 3802, 5665);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (5, 48, 3920000, 3962, 5905);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (5, 49, 4165000, 4126, 6150);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (5, 50, 0, 4300, 6400);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (6, 1, 200, 30, 30);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (6, 2, 800, 37, 40);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (6, 3, 2000, 47, 55);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (6, 4, 4000, 61, 75);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (6, 5, 7000, 78, 100);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (6, 6, 11200, 98, 130);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (6, 7, 16800, 122, 165);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (6, 8, 24000, 149, 205);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (6, 9, 33000, 179, 250);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (6, 10, 44000, 213, 300);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (6, 11, 57200, 250, 355);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (6, 12, 72800, 290, 415);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (6, 13, 91000, 334, 480);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (6, 14, 112000, 381, 550);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (6, 15, 136000, 431, 625);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (6, 16, 163200, 485, 705);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (6, 17, 193800, 542, 790);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (6, 18, 228000, 602, 880);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (6, 19, 266000, 666, 975);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (6, 20, 308000, 733, 1075);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (6, 21, 354200, 803, 1180);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (6, 22, 404800, 877, 1290);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (6, 23, 460000, 954, 1405);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (6, 24, 520000, 1034, 1525);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (6, 25, 585000, 1118, 1650);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (6, 26, 655200, 1205, 1780);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (6, 27, 730800, 1295, 1915);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (6, 28, 812000, 1389, 2055);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (6, 29, 899000, 1486, 2200);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (6, 30, 992000, 1586, 2350);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (6, 31, 1091200, 1690, 2505);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (6, 32, 1196800, 1797, 2665);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (6, 33, 1309000, 1907, 2830);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (6, 34, 1428000, 2021, 3000);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (6, 35, 1554000, 2138, 3175);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (6, 36, 1687200, 2258, 3355);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (6, 37, 1827800, 2382, 3540);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (6, 38, 1976000, 2509, 3730);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (6, 39, 2132000, 2639, 3925);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (6, 40, 2296000, 2773, 4125);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (6, 41, 2468200, 2910, 4330);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (6, 42, 2648800, 3050, 4540);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (6, 43, 2838000, 3194, 4755);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (6, 44, 3036000, 3341, 4975);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (6, 45, 3243000, 3491, 5200);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (6, 46, 3459200, 3645, 5430);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (6, 47, 3684800, 3802, 5665);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (6, 48, 3920000, 3962, 5905);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (6, 49, 4165000, 4126, 6150);
INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)
VALUES (6, 50, 0, 4300, 6400);


DROP TABLE IF EXISTS classes_levelup_spells;
CREATE TABLE classes_levelup_spells (
  class_id INT NOT NULL,
  level SMALLINT NOT NULL,
  spell_id INT NOT NULL
);

INSERT INTO classes_levelup_spells (class_id, level, spell_id)
VALUES (2, 4, 3);
INSERT INTO classes_levelup_spells (class_id, level, spell_id)
VALUES (2, 11, 25);
INSERT INTO classes_levelup_spells (class_id, level, spell_id)
VALUES (2, 14, 26);
INSERT INTO classes_levelup_spells (class_id, level, spell_id)
VALUES (2, 19, 27);
INSERT INTO classes_levelup_spells (class_id, level, spell_id)
VALUES (2, 24, 28);
INSERT INTO classes_levelup_spells (class_id, level, spell_id)
VALUES (2, 28, 81);
INSERT INTO classes_levelup_spells (class_id, level, spell_id)
VALUES (2, 31, 82);
INSERT INTO classes_levelup_spells (class_id, level, spell_id)
VALUES (2, 34, 83);
INSERT INTO classes_levelup_spells (class_id, level, spell_id)
VALUES (2, 39, 84);
INSERT INTO classes_levelup_spells (class_id, level, spell_id)
VALUES (2, 44, 85);
INSERT INTO classes_levelup_spells (class_id, level, spell_id)
VALUES (3, 1, 4);
INSERT INTO classes_levelup_spells (class_id, level, spell_id)
VALUES (3, 5, 20);
INSERT INTO classes_levelup_spells (class_id, level, spell_id)
VALUES (3, 9, 21);
INSERT INTO classes_levelup_spells (class_id, level, spell_id)
VALUES (3, 14, 22);
INSERT INTO classes_levelup_spells (class_id, level, spell_id)
VALUES (3, 19, 23);
INSERT INTO classes_levelup_spells (class_id, level, spell_id)
VALUES (3, 25, 24);
INSERT INTO classes_levelup_spells (class_id, level, spell_id)
VALUES (3, 29, 74);
INSERT INTO classes_levelup_spells (class_id, level, spell_id)
VALUES (3, 30, 75);
INSERT INTO classes_levelup_spells (class_id, level, spell_id)
VALUES (3, 32, 76);
INSERT INTO classes_levelup_spells (class_id, level, spell_id)
VALUES (3, 35, 77);
INSERT INTO classes_levelup_spells (class_id, level, spell_id)
VALUES (3, 39, 86);
INSERT INTO classes_levelup_spells (class_id, level, spell_id)
VALUES (3, 45, 78);
INSERT INTO classes_levelup_spells (class_id, level, spell_id)
VALUES (3, 46, 79);
INSERT INTO classes_levelup_spells (class_id, level, spell_id)
VALUES (3, 48, 80);
INSERT INTO classes_levelup_spells (class_id, level, spell_id)
VALUES (3, 49, 87);


COMMIT;