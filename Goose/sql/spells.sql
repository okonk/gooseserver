USE Goose;

DROP TABLE spells;
CREATE TABLE spells (
  spell_id INT IDENTITY(1,1) NOT NULL,
  spell_name VARCHAR(64) NOT NULL,
  spell_description VARCHAR(128) DEFAULT '' NOT NULL,
  spell_target INT NOT NULL,
  class_restrictions BIGINT DEFAULT 0 NOT NULL, /* if bit not set class id can cast */
  spell_aether BIGINT DEFAULT 100 NOT NULL, /* Aether in milliseconds */
  spellbook_graphic INT NOT NULL,
  
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

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (1, 'Healing 1', 0, 110006, 5, 3, 0, 31);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (2, 'Fortify 1', 0, 110018, 20, 4, 30000, 31);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (3, 'Backstab 1', 1, 110000, 20, 5, 18000, 59);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (4, 'Taunt 1', 0, 110000, 10, 6, 5000, 55);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (5, 'Elemental Strike 1', 0, 110012, 5, 7, 500, 47);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (6, 'Elemental Strike 2', 0, 110013, 10, 8, 500, 47);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (7, 'Arcane Shield 1', 0, 110018, 20, 9, 10000, 47);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (8, 'Elemental Strike 3', 0, 110011, 20, 10, 500, 47);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (9, 'Elemental Shielding 1', 0, 110004, 20, 11, 10000, 47);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (10, 'Teleportation', 1, 110009, 20, 12, 10000, 47);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (11, 'Root', 0, 110024, 20, 13, 0, 15);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (12, 'Elemental Strike 4', 0, 110003, 40, 14, 500, 47);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (13, 'Snare', 0, 110023, 50, 15, 0, 47);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (14, 'Elemental Strike 5', 0, 110012, 80, 16, 1000, 47);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (15, 'Gate', 1, 110009, 30, 17, 20000, 15);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (16, 'Regeneration 1', 0, 110008, 80, 18, 90000, 47);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (17, 'Bind Self', 1, 110007, 80, 19, 20000, 15);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (18, 'Group Teleportation', 2, 110009, 100, 12, 120000, 47);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (19, 'Elemental Strike 6', 0, 110013, 160, 20, 1000, 47);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (20, 'Rampant Rage', 1, 110033, 30, 21, 120000, 55);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (21, 'Insight', 1, 110007, 30, 22, 180000, 55);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (22, 'Area Taunt', 1, 110000, 40, 23, 10000, 55);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (23, 'Ground Slam 1', 1, 110011, 50, 24, 180000, 55);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (24, 'Berserker 1', 1, 110000, 50, 25, 225000, 55);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (25, 'Poison Weapon 1', 1, 110014, 30, 26, 120000, 59);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (26, 'Backstab 2', 1, 110000, 40, 27, 23000, 59);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (27, 'Nimble 1', 1, 110021, 50, 28, 90000, 59);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (28, 'Backstab 3', 1, 110000, 60, 29, 27000, 59);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (29, 'Illusion: Snowman', 0, 110007, 100, 30, 300000, 0);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (30, 'Healing 2', 0, 110006, 10, 35, 0, 31);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (31, 'Healing 3', 0, 110006, 25, 36, 0, 31);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (32, 'Healing 4', 0, 110006, 50, 37, 0, 31);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (33, 'Healing 5', 0, 110006, 100, 38, 0, 31);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (34, 'Fortify 2', 0, 110018, 50, 39, 5000, 31);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (35, 'Fortify 3', 0, 110018, 100, 40, 5000, 31);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (36, 'Fortify 4', 0, 110018, 150, 41, 5000, 31);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (37, 'Fortify 5', 0, 110018, 300, 42, 5000, 31);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (38, 'Strength 1', 0, 110020, 20, 43, 5000, 31);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (39, 'Strength 2', 0, 110020, 50, 44, 5000, 31);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (40, 'Strength 3', 0, 110020, 75, 45, 5000, 31);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (41, 'Strength 4', 0, 110020, 100, 46, 5000, 31);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (42, 'Strength 5', 0, 110020, 150, 47, 5000, 31);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (43, 'Stamina 1', 0, 110019, 20, 48, 5000, 31);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (44, 'Stamina 2', 0, 110019, 50, 49, 5000, 31);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (45, 'Stamina 3', 0, 110019, 75, 50, 5000, 31);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (46, 'Stamina 4', 0, 110019, 100, 51, 5000, 31);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (47, 'Intelligence 1', 0, 110008, 50, 52, 5000, 31);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (48, 'Intelligence 2', 0, 110008, 100, 53, 5000, 31);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (49, 'Dexterity 1', 0, 110008, 75, 54, 5000, 31);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (50, 'Dexterity 2', 0, 110008, 150, 55, 5000, 31);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (51, 'Mana Regeneration 1', 0, 110032, 100, 56, 55000, 31);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (52, 'Mana Regeneration 2', 0, 110032, 250, 57, 105000, 31);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (53, 'See Invisible', 0, 110007, 20, 58, 5000, 31);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, hp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (54, 'Sacrifice', 0, 110007, 2500, 59, 200, 31);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, spell_effect_id, 
	hp_percent_cost, mp_percent_cost, spell_aether, class_restrictions)
VALUES (55, 'Fearsome Lash', 1, 110014, 62, 30, 70, 3000, 59);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, spell_effect_id, 
	hp_percent_cost, mp_static_cost, mp_percent_cost, spell_aether, class_restrictions)
VALUES (56, 'Sunder of Spirits', 1, 110028, 64, 70, 300, 0.15, 3000, 55);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (57, 'Arcane Shield 2', 0, 110018, 40, 101, 10000, 47);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (58, 'Group Elemental Shielding 1', 2, 110004, 80, 11, 10000, 47);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (59, 'Invisibility', 0, 110034, 80, 102, 5000, 47);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (60, 'Elemental Strike 7', 0, 110011, 280, 103, 1000, 47);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (61, 'Elemental Shielding 2', 0, 110004, 400, 104, 10000, 47);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (62, 'Group Elemental Shielding 2', 2, 110004, 800, 104, 10000, 47);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (63, 'Regeneration 2', 0, 110008, 500, 105, 300000, 47);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (64, 'Bind Other', 0, 110007, 400, 106, 10000, 47);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (65, 'Otherlands Teleport', 1, 110009, 500, 107, 10000, 47);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (66, 'Group Otherlands Teleport', 2, 110009, 800, 107, 20000, 47);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (67, 'Elemental Strike 8', 1, 110003, 400, 108, 4000, 47);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (68, 'Arcane Shield 4', 0, 110018, 400, 109, 20000, 47);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (69, 'Elemental Strike 9', 0, 110015, 350, 110, 1000, 47);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (70, 'Regeneration 3', 0, 110008, 800, 111, 330000, 47);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (71, 'Elemental Strike 10', 0, 110015, 450, 117, 1000, 47);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (72, 'Arcane Shield 5', 0, 110018, 800, 112, 20000, 47);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (73, 'Arcane Shield 3', 0, 110018, 200, 113, 20000, 47);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (74, 'Ground Slam 2', 1, 110011, 100, 118, 180000, 55);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (75, 'Taunt 2', 0, 110000, 60, 119, 5000, 55);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (76, 'Fortitude 1', 1, 110001, 130, 120, 180000, 55);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (77, 'Berserker 2', 1, 110000, 150, 121, 240000, 55);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (78, 'Berserker 3', 1, 110000, 300, 122, 270000, 55);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (79, 'Fortitude 2', 1, 110001, 400, 123, 180000, 55);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (80, 'Savage Fury', 1, 110033, 500, 124, 180000, 55);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (81, 'Invisibility', 1, 110034, 80, 102, 60000, 59);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (82, 'Poison Weapon 2', 1, 110014, 100, 125, 120000, 59);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (83, 'Backstab 4', 1, 110000, 160, 126, 23000, 59);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (84, 'Nimble 2', 1, 110021, 200, 127, 90000, 59);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (85, 'Backstab 5', 1, 110000, 320, 128, 18000, 59);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (86, 'Ground Slam 3', 1, 110011, 200, 129, 180000, 55);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (87, 'Ground Slam 4', 1, 110011, 300, 130, 180000, 55);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, hp_percent_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (88, 'Covenant', 1, 110031, 50, 133, 4000, 47);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_percent_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (89, 'Arcane Blast', 0, 110036, 100, 134, 2000, 47);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_percent_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (90, 'Arcane Assault', 0, 110029, 80, 135, 3000, 47);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, hp_percent_cost, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (91, 'Spirit Strike', 1, 110016, 90, 400, 136, 1500, 55);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, spell_effect_id, 
	hp_percent_cost, mp_percent_cost, spell_aether, class_restrictions)
VALUES (92, 'Critical Strike', 1, 110014, 137, 50, 50, 1000, 59);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (93, 'Rejuvination', 1, 110005, 500, 138, 2000, 31);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_percent_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (94, 'Restore Health', 0, 110010, 60, 139, 3000, 31);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (95, 'Group Paradise Teleport', 2, 110009, 5000, 141, 20000, 47);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (96, 'Ancient Healing', 0, 110006, 500, 143, 0, 31);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (97, 'Ancient Root', 0, 110024, 1000, 144, 30000, 47);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (98, 'Ancient Sturdiness', 1, 110011, 20000, 145, 1800000, 47);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (99, 'Ancient Criticality', 1, 110035, 10000, 146, 240000, 59);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (100, 'Ancient Augmentation', 1, 110035, 4000, 147, 240000, 55);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (101, 'Ancient Protection', 1, 110030, 30000, 148, 120000, 31);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (102, 'Ancient Buffiness', 1, 110035, 45000, 149, 300000, 47);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (103, 'Ancient Damage', 1, 110000, 10000, 150, 300000, 59);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (104, 'Ancient Taunt', 0, 110000, 700, 151, 2000, 55);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, hp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (105, 'Ancient Sacrifice', 2, 110007, 5000, 152, 1400, 31);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (106, 'Smoke Bomb', 1, 110014, 500, 153, 300000, 59);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (107, 'Group Heal', 2, 110006, 75, 154, 1000, 31);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (108, 'Warrior Root', 1, 110024, 100, 155, 5000, 55);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (109, 'Ancient Bellow', 0, 110000, 1400, 165, 4000, 55);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, hp_percent_cost, mp_percent_cost,mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (110, 'Ancient Awe', 1, 110033, 70, 90, 15000, 168, 600000, 55);
 
INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, hp_percent_cost, mp_percent_cost, hp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (111, 'Ancient Death', 1, 110017, 90, 50, 20000, 167, 600000, 59);
 
INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, hp_percent_cost, mp_percent_cost, hp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (112, 'Ancient Conflagration', 1, 110029, 95, 30, 20000, 166, 600000, 47);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, hp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (113, 'Ancient Blessings', 2, 110035, 30000, 170, 600000, 31);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (114, 'Spiritual Blessings', 2, 110007, 5000, 171, 600000, 31);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, hp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (115, 'Sacrifice II', 0, 110007, 5000, 178, 400, 31);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (116, 'Damage of the Bear', 2, 110035, 45000, 179, 120000, 47);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (117, 'Critical Blow of the Bear', 2, 110035, 45000, 180, 120000, 59);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (118, 'Roar of the Bear', 1, 110000, 4000, 181, 5000, 55);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (119, 'Illusion: Bat', 0, 110007, 100, 183, 60000, 0);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (120, 'Illusion: Shroom', 0, 110007, 100, 184, 60000, 0);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (121, 'Ancient Group Healing', 2, 110006, 1000, 199, 1000, 31);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (122, 'Ancient Group Damage', 2, 110027, 35000, 200, 300000, 59);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (123, 'Ancient Regeneration', 2, 110035, 35000, 201, 45000, 47);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (124, 'Augment', 2, 110001, 500, 202, 60000, 47);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (125, 'Empower', 2, 110020, 300, 203, 60000, 31);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (126, 'Bustle', 2, 110021, 100, 204, 600000, 59);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (127, 'Aggravate', 2, 110021, 100, 205, 60000, 55);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (128, 'Meditate', 1, 110032, 500, 206, 60000, 47);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (129, 'Bulk', 1, 110020, 300, 207, 60000, 31);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (130, 'Tumble', 1, 110027, 100, 208, 600000, 59);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (131, 'Forge', 1, 110008, 100, 209, 60000, 55);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, hp_percent_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (132, 'Mischiefs Craft', 1, 110031, 25, 220, 6000, 59);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (133, 'Wizards Curse', 0, 110035, 65000, 221, 300000, 47);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, hp_static_cost, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (134, 'Clerics Blessing', 2, 110035, 50000, 70000, 222, 600000, 31);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (135, 'Knights Blessing', 1, 110018, 50000, 223, 60000, 55);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (136, 'First Aid', 1, 110006, 50, 226, 250, 0);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (137, 'Recovery', 0, 110006, 250, 227, 50, 0);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (138, 'Clobber', 1, 110012, 50, 228, 250, 0);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (139, 'Pummel', 0, 110012, 250, 229, 50, 0);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (140, 'Tame', 1, 110012, 0, 230, 0, 0);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (141, 'Pet Attack', 0, 110012, 0, 231, 0, 0);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (142, 'Pet Defend', 0, 110012, 0, 232, 0, 0);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (143, 'Pet Recall', 0, 110012, 0, 233, 0, 0);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (144, 'Pet Follow', 0, 110012, 0, 234, 0, 0);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (145, 'Pet Neutral', 0, 110012, 0, 235, 0, 0);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (146, 'Ancient Healing 2', 0, 110006, 1200, 242, 0, 31);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_percent_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (147, 'Death Touch', 0, 110036, 0, 243, 0, 0);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (148, 'Group AD5 Teleport', 2, 110009, 100000, 246, 1000, 47);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (149, 'Paradise Teleportation', 1, 110009, 20, 141, 10000, 0);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, hp_percent_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (150, 'Ancient Covenant', 1, 110031, 65, 247, 1500, 47);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, hp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (151, 'Ancient Sacrifice 2', 2, 110007, 10000, 248, 1000, 31);

INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, mp_static_cost, spell_effect_id, spell_aether, class_restrictions)
VALUES (152, 'Ancient Taunt 2', 0, 110000, 5000, 249, 2000, 55);

SET IDENTITY_INSERT spells OFF;
  
DROP TABLE spell_effects;
CREATE TABLE spell_effects (
  spell_effect_id INT IDENTITY(1,1) NOT NULL,
  spell_effect_name VARCHAR(64) NOT NULL,
  spell_animation INT NOT NULL,
  spell_display INT NOT NULL,
  target_type INT NOT NULL,
  target_size INT NOT NULL,
  
  spell_effected INT NOT NULL,
  min_level_effected INT DEFAULT 1 NOT NULL,
  
  effect_type INT NOT NULL,
  effect_duration BIGINT NOT NULL,
  
  do_attack_animation CHAR(1) DEFAULT '0' NOT NULL,
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

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration, spell_damage_effects,
	hp_change_formula, works_not_in_pvp)
VALUES (1, 'Small Health Potion', 0, 0, 0, 0, 1, 0, 0, '0', '25', '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration, spell_damage_effects,
	mp_change_formula, works_not_in_pvp)
VALUES (2, 'Small Mana Potion', 0, 0, 0, 0, 1, 0, 0, '0', '25', '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration, spell_damage_effects,
	hp_change_formula, works_not_in_pvp)
VALUES (3, 'Healing 1', 115015, 0, 0, 0, 5, 0, 0, '1', '25', '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, stat_ac, hp, buff_graphic, buff_removable,
	buff_doesnt_stack_over)
VALUES (4, 'Fortify 1', 115016, 0, 0, 0, 5, 1, 600, '1', 20, 50, 110018, '1', '39 40 41 42');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	hp_change_formula, do_attack_animation, spell_damage_effects)
VALUES (5, 'Backstab 1', 115010, 0, 1, 1, 6, 0, 0, '-2 * (%cstr + %cwdmg + %clevel)', '1', '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	hp_change_formula, taunt_aggro)
VALUES (6, 'Taunt 1', 115014, 0, 0, 0, 2, 0, 0, '-1', 1000);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	hp_change_formula, spell_damage_effects)
VALUES (7, 'Elemental Strike 1', 115000, 0, 0, 0, 6, 0, 0, '-10', '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	hp_change_formula, spell_damage_effects)
VALUES (8, 'Elemental Strike 2', 115003, 0, 0, 0, 6, 0, 0, '-20', '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, stat_ac, buff_removable, buff_graphic,
	buff_doesnt_stack_over)
VALUES (9, 'Arcane Shield 1', 115016, 0, 0, 0, 5, 1, 600, '1', 10, '1', 110018, '101 113 109 112');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	hp_change_formula, spell_damage_effects)
VALUES (10, 'Elemental Strike 3', 115001, 0, 0, 0, 6, 0, 0, '-40', '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, buff_graphic, buff_removable, on_hit_spell_effect_id,
	buff_doesnt_stack_over)
VALUES (11, 'Elemental Shielding 1', 115023, 0, 0, 0, 5, 14, 600, '1', 110004, '1', 63, '104');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	teleport_map, teleport_x, teleport_y, works_not_in_pvp)
VALUES (12, 'Teleportation', 115012, 0, 0, 0, 5, 5, 0, 1, 50, 50, '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	buff_removable, buff_graphic)
VALUES (13, 'Root', 115038, 0, 0, 0, 2, 8, 25, '0', 110024);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	hp_change_formula, spell_damage_effects)
VALUES (14, 'Elemental Strike 4', 115002, 0, 0, 0, 6, 0, 0, '-60', '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	buff_removable, snare_percent)
VALUES (15, 'Snare', 115034, 0, 0, 0, 2, 9, 30, '0', 80);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	hp_change_formula, spell_damage_effects, random_join_chance)
VALUES (16, 'Elemental Strike 5', 115000, 1, 4, 4, 6, 0, 0, '-80', '1', 33);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	teleport_map, teleport_x, teleport_y)
VALUES (17, 'Gate', 115012, 0, 0, 0, 1, 5, 0, 0, 50, 50);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, hp_percent_regen, buff_removable, buff_graphic,
	buff_doesnt_stack_over)
VALUES (18, 'Regeneration 1', 115013, 0, 0, 0, 5, 1, 180, '1', 0.02, '1', 110008, '105 111');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration)
VALUES (19, 'Bind Self', 115014, 0, 0, 0, 1, 6, 0);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	hp_change_formula, spell_damage_effects)
VALUES (20, 'Elemental Strike 6', 115003, 1, 3, 1, 6, 0, 0, '-100', '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	buff_graphic, buff_removable, on_attack_spell_effect_id, buff_doesnt_stack_over, buff_stacks_over)
VALUES (21, 'Rampant Rage', 115048, 0, 0, 0, 1, 13, 60, 110033, '1', 60, '124', '');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	buff_graphic, buff_removable)
VALUES (22, 'Insight', 115014, 0, 0, 0, 1, 12, 300, 110007, '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	hp_change_formula, taunt_aggro, spell_damage_effects)
VALUES (23, 'Area Taunt', 115014, 1, 5, 4, 2, 0, 0, '-10', 3000, '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	hp_change_formula, buff_removable, buff_graphic, spell_damage_effects)
VALUES (24, 'Ground Slam 1', 115001, 1, 3, 1, 2, 7, 10, '-30', '0', 110011, '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	buff_graphic, buff_removable, stat_str, haste, buff_doesnt_stack_over, buff_stacks_over)
VALUES (25, 'Berserker 1', 115024, 0, 0, 0, 1, 4, 45, 110000, '1', 50, 0.10, '121 122', '');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	buff_graphic, buff_removable, on_attack_spell_effect_id, buff_doesnt_stack_over, buff_stacks_over)
VALUES (26, 'Poison Weapon 1', 115041, 0, 0, 0, 1, 13, 60, 110014, '1', 61, '125', '');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	hp_change_formula, do_attack_animation, spell_damage_effects)
VALUES (27, 'Backstab 2', 115010, 0, 1, 1, 6, 0, 0, '-3 * (%cstr + %cwdmg + %clevel)', '1', '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	buff_graphic, buff_removable, stat_dex, haste, buff_doesnt_stack_over, buff_stacks_over)
VALUES (28, 'Nimble 1', 115021, 0, 0, 0, 1, 1, 60, 110021, '1', 50, 0.10, '127', '');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	hp_change_formula, do_attack_animation, spell_damage_effects)
VALUES (29, 'Backstab 3', 115010, 0, 1, 1, 6, 0, 0, '-4 * (%cstr + %cwdmg + %clevel)', '1', '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	buff_removable, buff_graphic, works_not_in_pvp, body_id)
VALUES (30, 'Illusion: Snowman', 115014, 0, 0, 0, 5, 1, 300, '1', 110007, '1', 134);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration, spell_damage_effects,
	hp_change_formula, works_not_in_pvp)
VALUES (31, 'Health Potion', 0, 0, 0, 0, 1, 0, 0, '0', '50', '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration, spell_damage_effects,
	mp_change_formula, works_not_in_pvp)
VALUES (32, 'Mana Potion', 0, 0, 0, 0, 1, 0, 0, '0', '50', '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration, spell_damage_effects,
	hp_change_formula, works_not_in_pvp)
VALUES (33, 'Large Health Potion', 0, 0, 0, 0, 1, 0, 0, '0', '150', '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration, spell_damage_effects,
	mp_change_formula, works_not_in_pvp)
VALUES (34, 'Large Mana Potion', 0, 0, 0, 0, 1, 0, 0, '0', '150', '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 	
	target_type, target_size, spell_effected, effect_type, effect_duration, spell_damage_effects,
	hp_change_formula, works_not_in_pvp)
VALUES (35, 'Healing 2', 115015, 0, 0, 0, 5, 0, 0, '1', '50', '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration, spell_damage_effects,
	hp_change_formula, works_not_in_pvp)
VALUES (36, 'Healing 3', 115015, 0, 0, 0, 5, 0, 0, '1', '125', '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration, spell_damage_effects,
	hp_change_formula, works_not_in_pvp)
VALUES (37, 'Healing 4', 115015, 0, 0, 0, 5, 0, 0, '1', '350', '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration, spell_damage_effects,
	hp_change_formula, works_not_in_pvp)
VALUES (38, 'Healing 5', 115015, 0, 0, 0, 5, 0, 0, '1', '1000', '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, stat_ac, hp, buff_graphic, buff_removable,
	buff_doesnt_stack_over, buff_stacks_over, min_level_effected)
VALUES (39, 'Fortify 2', 115016, 0, 0, 0, 5, 1, 600, '1', 35, 100, 110018, '1', '40 41 42', '4', 5);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, stat_ac, hp, buff_graphic, buff_removable,
	buff_doesnt_stack_over, buff_stacks_over, min_level_effected)
VALUES (40, 'Fortify 3', 115016, 0, 0, 0, 5, 1, 600, '1', 50, 200, 110018, '1', '41 42', '4 39', 15);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, stat_ac, hp, buff_graphic, buff_removable,
	buff_doesnt_stack_over, buff_stacks_over, min_level_effected)
VALUES (41, 'Fortify 4', 115016, 0, 0, 0, 5, 1, 600, '1', 75, 400, 110018, '1', '42', '4 39 40', 25);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, stat_ac, hp, buff_graphic, buff_removable,
	buff_doesnt_stack_over, buff_stacks_over, min_level_effected)
VALUES (42, 'Fortify 5', 115016, 0, 0, 0, 5, 1, 600, '1', 100, 1000, 110018, '1', '', '4 39 40 41', 35);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, buff_graphic, buff_removable, stat_str,
	buff_doesnt_stack_over, buff_stacks_over, min_level_effected)
VALUES (43, 'Strength 1', 115013, 0, 0, 0, 5, 1, 600, '1', 110020, '1', 10, '44 45 46 47', '', 1);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, buff_graphic, buff_removable, stat_str,
	buff_doesnt_stack_over, buff_stacks_over, min_level_effected)
VALUES (44, 'Strength 2', 115013, 0, 0, 0, 5, 1, 600, '1', 110020, '1', 20, '45 46 47', '43', 10);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, buff_graphic, buff_removable, stat_str,
	buff_doesnt_stack_over, buff_stacks_over, min_level_effected)
VALUES (45, 'Strength 3', 115013, 0, 0, 0, 5, 1, 600, '1', 110020, '1', 30, '46 47', '43 44', 20);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, buff_graphic, buff_removable, stat_str,
	buff_doesnt_stack_over, buff_stacks_over, min_level_effected)
VALUES (46, 'Strength 4', 115013, 0, 0, 0, 5, 1, 600, '1', 110020, '1', 40, '47', '43 44 45', 30);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, buff_graphic, buff_removable, stat_str,
	buff_doesnt_stack_over, buff_stacks_over, min_level_effected)
VALUES (47, 'Strength 5', 115013, 0, 0, 0, 5, 1, 600, '1', 110020, '1', 50, '', '43 44 45 46', 40);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, buff_graphic, buff_removable, stat_sta,
	buff_doesnt_stack_over, buff_stacks_over, min_level_effected)
VALUES (48, 'Stamina 1', 115013, 0, 0, 0, 5, 1, 600, '1', 110019, '1', 10, '49 50 51', '', 1);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, buff_graphic, buff_removable, stat_sta,
	buff_doesnt_stack_over, buff_stacks_over, min_level_effected)
VALUES (49, 'Stamina 2', 115013, 0, 0, 0, 5, 1, 600, '1', 110019, '1', 20, '50 51', '48', 10);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, buff_graphic, buff_removable, stat_sta,
	buff_doesnt_stack_over, buff_stacks_over, min_level_effected)
VALUES (50, 'Stamina 3', 115013, 0, 0, 0, 5, 1, 600, '1', 110019, '1', 30, '51', '48 49', 20);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, buff_graphic, buff_removable, stat_sta,
	buff_doesnt_stack_over, buff_stacks_over, min_level_effected)
VALUES (51, 'Stamina 4', 115013, 0, 0, 0, 5, 1, 600, '1', 110019, '1', 40, '', '48 49 50', 30);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, buff_graphic, buff_removable, stat_int,
	buff_doesnt_stack_over, buff_stacks_over, min_level_effected)
VALUES (52, 'Intelligence 1', 115013, 0, 0, 0, 5, 1, 600, '1', 110008, '1', 10, '53', '', 15);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, buff_graphic, buff_removable, stat_int,
	buff_doesnt_stack_over, buff_stacks_over, min_level_effected)
VALUES (53, 'Intelligence 2', 115013, 0, 0, 0, 5, 1, 600, '1', 110008, '1', 20, '', '52', 25);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, buff_graphic, buff_removable, stat_dex,
	buff_doesnt_stack_over, buff_stacks_over, min_level_effected)
VALUES (54, 'Dexterity 1', 115013, 0, 0, 0, 5, 1, 600, '1', 110008, '1', 20, '55', '', 25);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, buff_graphic, buff_removable, stat_dex,
	buff_doesnt_stack_over, buff_stacks_over, min_level_effected)
VALUES (55, 'Dexterity 2', 115013, 0, 0, 0, 5, 1, 600, '1', 110008, '1', 40, '', '54', 35);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, buff_graphic, buff_removable, mp_percent_regen,
	buff_doesnt_stack_over, buff_stacks_over, min_level_effected)
VALUES (56, 'Mana Regeneration 1', 115045, 0, 0, 0, 5, 1, 120, '1', 110032, '1', 0.02, '57 138', '', 20);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, buff_graphic, buff_removable, mp_percent_regen,
	buff_doesnt_stack_over, buff_stacks_over, min_level_effected)
VALUES (57, 'Mana Regeneration 2', 115045, 0, 0, 0, 5, 1, 150, '1', 110032, '1', 0.04, '138', '56', 35);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, buff_graphic, buff_removable)
VALUES (58, 'See Invisible', 115014, 0, 0, 0, 5, 1, 120, '1', 110007, '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration, spell_damage_effects,
	mp_change_formula, works_not_in_pvp, min_level_effected)
VALUES (59, 'Sacrifice', 115014, 0, 0, 0, 5, 0, 0, '0', '5000', '1', 50);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	hp_change_formula, spell_damage_effects)
VALUES (60, 'Rampant Rage Chomp', 115048, 1, 1, 1, 6, 0, 0, '-1 * (%cstr + %cwdmg + %clevel)', '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	hp_change_formula)
VALUES (61, 'Poison Weapon Bubble', 115027, 0, 1, 1, 6, 3, 12, '-15');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	hp_change_formula, spell_damage_effects, do_attack_animation)
VALUES (62, 'Fearsome Lash', 115008, 1, 1, 3, 6, 0, 0, '-2.7 * ((%ccmp * 0.7) + (%cchp * 0.3))', '1', '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	hp_change_formula, spell_damage_effects)
VALUES (63, 'Elemental Shielding 1 Rocks', 115001, 0, 0, 0, 6, 0, 0, '-15', '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	hp_change_formula, spell_damage_effects, do_attack_animation, taunt_aggro)
VALUES (64, 'Sunder of Spirits', 115042, 1, 6, 2, 6, 0, 0, '-((%ccmp * 0.15) + (%cchp * 0.8))', '1', '1', 50000);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	hair_r, hair_g, hair_b, hair_a)
VALUES (65, 'Hair Dye: Black', 0, 0, 0, 1, 1, 2, 0, 0, 0, 0, 180);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, buff_graphic, mp_static_regen)
VALUES (66, 'Mana Point Regeneration C', 0, 0, 0, 0, 1, 1, 0, '1', 110032, 100);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, buff_graphic, mp_static_regen)
VALUES (67, 'Mana Point Regeneration D', 0, 0, 0, 0, 1, 1, 0, '1', 110032, 500);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, buff_graphic, hp_static_regen)
VALUES (68, 'Hit Point Regeneration C', 0, 0, 0, 0, 1, 1, 0, '1', 110008, 100);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, buff_graphic, hp_static_regen)
VALUES (69, 'Hit Point Regeneration D', 0, 0, 0, 0, 1, 1, 0, '1', 110008, 500);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, buff_graphic, spell_damage)
VALUES (70, 'Increased Spell Damage V', 0, 0, 0, 0, 1, 1, 0, '1', 110035, 0.05);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, buff_graphic, spell_damage)
VALUES (71, 'Increased Spell Damage X', 0, 0, 0, 0, 1, 1, 0, '1', 110035, 0.1);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, buff_graphic, spell_damage)
VALUES (72, 'Increased Spell Damage XX', 0, 0, 0, 0, 1, 1, 0, '1', 110035, 0.2);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, buff_graphic, haste)
VALUES (73, 'Haste V', 0, 0, 0, 0, 1, 1, 0, '1', 110027, 0.05);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, buff_graphic, haste)
VALUES (74, 'Haste X', 0, 0, 0, 0, 1, 1, 0, '1', 110027, 0.1);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, buff_graphic, haste)
VALUES (75, 'Haste XX', 0, 0, 0, 0, 1, 1, 0, '1', 110027, 0.2);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, buff_graphic, spell_crit)
VALUES (76, 'Spell Critical Damage V', 0, 0, 0, 0, 1, 1, 0, '1', 110035, 0.05);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, buff_graphic, spell_crit)
VALUES (77, 'Spell Critical Damage X', 0, 0, 0, 0, 1, 1, 0, '1', 110035, 0.1);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, buff_graphic, melee_crit)
VALUES (78, 'Melee Critical Damage V', 0, 0, 0, 0, 1, 1, 0, '1', 110035, 0.05);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, buff_graphic, melee_crit)
VALUES (79, 'Melee Critical Damage X', 0, 0, 0, 0, 1, 1, 0, '1', 110035, 0.1);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, buff_graphic, mp_percent_regen)
VALUES (80, 'Mana Point Regeneration 1', 0, 0, 0, 0, 1, 1, 0, '1', 110032, 0.01);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, buff_graphic, hp_percent_regen)
VALUES (81, 'Hit Point Regeneration 1', 0, 0, 0, 0, 1, 1, 0, '1', 110008, 0.01);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, buff_graphic, mp_percent_regen)
VALUES (82, 'Mana Point Regeneration 2', 0, 0, 0, 0, 1, 1, 0, '1', 110032, 0.02);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, buff_graphic, hp_percent_regen)
VALUES (83, 'Hit Point Regeneration 2', 0, 0, 0, 0, 1, 1, 0, '1', 110008, 0.02);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, buff_graphic, mp_percent_regen, hp_percent_regen)
VALUES (84, 'HP MP Regeneration', 0, 0, 0, 0, 1, 1, 0, '1', 110008, 0.01, 0.01);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	hair_r, hair_g, hair_b, hair_a)
VALUES (85, 'Hair Dye: Red', 0, 0, 0, 1, 1, 2, 0, 155, 0, 0, 160);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	hair_r, hair_g, hair_b, hair_a)
VALUES (86, 'Hair Dye: Blue', 0, 0, 0, 1, 1, 2, 0, 0, 0, 155, 160);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	hair_r, hair_g, hair_b, hair_a)
VALUES (87, 'Hair Dye: Grey', 0, 0, 0, 1, 1, 2, 0, 0, 0, 0, 100);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	hair_id)
VALUES (88, 'Hair Cut: 1', 0, 0, 0, 1, 1, 2, 0, 20);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	hair_id)
VALUES (89, 'Hair Cut: 2', 0, 0, 0, 1, 1, 2, 0, 21);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	hair_id)
VALUES (90, 'Hair Cut: 3', 0, 0, 0, 1, 1, 2, 0, 22);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	hair_id)
VALUES (91, 'Hair Cut: 4', 0, 0, 0, 1, 1, 2, 0, 23);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	hair_id)
VALUES (92, 'Hair Cut: 5', 0, 0, 0, 1, 1, 2, 0, 24);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	hair_id)
VALUES (93, 'Hair Cut: 6', 0, 0, 0, 1, 1, 2, 0, 25);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	hair_id)
VALUES (94, 'Hair Cut: 7', 0, 0, 0, 1, 1, 2, 0, 26);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	face_id)
VALUES (95, 'Face: 1', 0, 0, 0, 1, 1, 2, 0, 70);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	face_id)
VALUES (96, 'Face: 2', 0, 0, 0, 1, 1, 2, 0, 71);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	face_id)
VALUES (97, 'Face: 3', 0, 0, 0, 1, 1, 2, 0, 72);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	face_id)
VALUES (98, 'Face: 4', 0, 0, 0, 1, 1, 2, 0, 73);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	body_id)
VALUES (99, 'Sexchange: Male', 0, 0, 0, 1, 1, 2, 0, 1);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	body_id)
VALUES (100, 'Sexchange: Female', 0, 0, 0, 1, 1, 2, 0, 11);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, stat_ac, buff_removable, buff_graphic,
	buff_doesnt_stack_over, buff_stacks_over, min_level_effected)
VALUES (101, 'Arcane Shield 2', 115016, 0, 0, 0, 5, 1, 600, '1', 40, '1', 110018, '113 109 112', '9', 10);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, buff_removable, buff_graphic,
	buff_doesnt_stack_over, buff_stacks_over, min_level_effected)
VALUES (102, 'Invisibility', 115047, 0, 0, 0, 5, 11, 300, '1', '1', 110034, '', '', 1);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	hp_change_formula, spell_damage_effects)
VALUES (103, 'Elemental Strike 7', 115001, 1, 2, 3, 6, 0, 0, '-200', '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, buff_graphic, buff_removable, on_hit_spell_effect_id,
	buff_doesnt_stack_over, buff_stacks_over, min_level_effected)
VALUES (104, 'Elemental Shielding 2', 115023, 0, 0, 0, 5, 14, 600, '1', 110004, '1', 114, '', '11', 20);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, hp_percent_regen, buff_removable, buff_graphic,
	buff_doesnt_stack_over, buff_stacks_over, min_level_effected)
VALUES (105, 'Regeneration 2', 115013, 0, 0, 0, 5, 1, 300, '1', 0.04, '1', 110008, '111', '18', 20);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration, works_not_in_pvp)
VALUES (106, 'Bind Other', 115014, 0, 0, 0, 4, 6, 0, '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	teleport_map, teleport_x, teleport_y, min_level_effected, works_not_in_pvp)
VALUES (107, 'Otherlands Teleport', 115012, 0, 0, 0, 5, 5, 0, 11, 10, 10, 1, '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	hp_change_formula, spell_damage_effects)
VALUES (108, 'Elemental Strike 8', 115002, 1, 1, 3, 6, 0, 0, '-250', '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, stat_ac, buff_removable, buff_graphic,
	buff_doesnt_stack_over, buff_stacks_over, min_level_effected)
VALUES (109, 'Arcane Shield 4', 115016, 0, 0, 0, 5, 1, 600, '1', 80, '1', 110018, '112', '9 101 113', 25);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	hp_change_formula, spell_damage_effects)
VALUES (110, 'Elemental Strike 9', 115004, 0, 0, 0, 6, 0, 0, '-300', '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, hp_percent_regen, buff_removable, buff_graphic,
	buff_doesnt_stack_over, buff_stacks_over, min_level_effected)
VALUES (111, 'Regeneration 3', 115013, 0, 0, 0, 5, 1, 300, '1', 0.06, '1', 110008, '', '18 105', 30);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, stat_ac, buff_removable, buff_graphic,
	buff_doesnt_stack_over, buff_stacks_over, min_level_effected)
VALUES (112, 'Arcane Shield 5', 115016, 0, 0, 0, 5, 1, 600, '1',100, '1', 110018, '', '9 101 113 109', 35);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, stat_ac, buff_removable, buff_graphic,
	buff_doesnt_stack_over, buff_stacks_over, min_level_effected)
VALUES (113, 'Arcane Shield 3', 115016, 0, 0, 0, 5, 1, 600, '1', 60, '1', 110018, '109 112', '9 101', 15);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	hp_change_formula, spell_damage_effects)
VALUES (114, 'Elemental Shielding 2 Rocks', 115001, 0, 0, 0, 6, 0, 0, '-30', '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	buff_graphic, buff_removable, on_attack_spell_effect_id, on_attack_spell_chance)
VALUES (115, 'DDTS Effect', 0, 0, 0, 0, 6, 13, 0, 0, '0', 116,7);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	hp_change_formula, spell_damage_effects)
VALUES (116, 'DDTS Damage', 115002, 1, 3, 1, 6, 0, 0, '-2500', '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	hp_change_formula, spell_damage_effects)
VALUES (117, 'Elemental Strike 10', 115005, 0, 0, 0, 6, 0, 0, '-400', '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	hp_change_formula, buff_removable, buff_graphic, spell_damage_effects)
VALUES (118, 'Ground Slam 2', 115001, 1, 5, 2, 2, 7, 15, '-50', '0', 110011, '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	hp_change_formula, taunt_aggro)
VALUES (119, 'Taunt 2', 115014, 0, 0, 0, 2, 0, 0, '-20', 10000);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, stat_ac, stat_sta, buff_removable, buff_graphic,
	buff_doesnt_stack_over, buff_stacks_over)
VALUES (120, 'Fortitude 1', 115016, 0, 0, 0, 1, 1, 60, '1', 150, 20, '1', 110001, '123', '');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	buff_graphic, buff_removable, stat_str, haste, buff_doesnt_stack_over, buff_stacks_over)
VALUES (121, 'Berserker 2', 115024, 0, 0, 0, 1, 4, 60, 110000, '1', 100, 0.20, '122', '25');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	buff_graphic, buff_removable, stat_str, haste, buff_doesnt_stack_over, buff_stacks_over)
VALUES (122, 'Berserker 3', 115024, 0, 0, 0, 1, 4, 90, 110000, '1', 200, 0.30, '', '25 121');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, stat_ac, stat_sta, buff_removable, buff_graphic,
	buff_doesnt_stack_over, buff_stacks_over)
VALUES (123, 'Fortitude 2', 115016, 0, 0, 0, 1, 1, 90, '1', 300, 40, '1', 110001, '', '120');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	buff_removable, buff_graphic,
	buff_doesnt_stack_over, buff_stacks_over, on_attack_spell_effect_id)
VALUES (124, 'Savage Fury', 115048, 1, 0, 0, 1, 13, 30, '1', 110033, '', '25', 131);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	buff_graphic, buff_removable, on_attack_spell_effect_id, buff_doesnt_stack_over, buff_stacks_over)
VALUES (125, 'Poison Weapon 2', 115041, 0, 0, 0, 1, 13, 60, 110014, '1', 132, '', '26');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	hp_change_formula, do_attack_animation, spell_damage_effects)
VALUES (126, 'Backstab 4', 115010, 0, 1, 1, 6, 0, 0, '-5 * (%cstr + %cwdmg + %clevel)', '1', '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	buff_graphic, buff_removable, stat_dex, haste, buff_doesnt_stack_over, buff_stacks_over)
VALUES (127, 'Nimble 2', 115021, 0, 0, 0, 1, 1, 60, 110021, '1', 100, 0.30, '', '28');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	hp_change_formula, do_attack_animation, spell_damage_effects)
VALUES (128, 'Backstab 5', 115010, 0, 1, 1, 6, 0, 0, '-6 * (%cstr + %cwdmg + %clevel)', '1', '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	hp_change_formula, buff_removable, buff_graphic, spell_damage_effects)
VALUES (129, 'Ground Slam 3', 115001, 1, 5, 3, 2, 7, 20, '-70', '0', 110011, '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	hp_change_formula, buff_removable, buff_graphic, spell_damage_effects)
VALUES (130, 'Ground Slam 4', 115040, 0, 5, 4, 2, 7, 30, '-100', '0', 110011, '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	hp_change_formula, spell_damage_effects)
VALUES (131, 'Savage Fury Chomp', 115048, 1, 3, 1, 6, 0, 0, '-2 * (%cstr + %cwdmg + %clevel)', '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	hp_change_formula)
VALUES (132, 'Poison Weapon 2 Bubble', 115027, 0, 1, 1, 6, 3, 18, '-35');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	mp_change_formula)
VALUES (133, 'Covenant', 115046, 0, 0, 0, 1, 0, 0, '%cchp');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	hp_change_formula, spell_damage_effects)
VALUES (134, 'Arcane Blast', 115032, 0, 0, 0, 6, 0, 0, '-1.9 * %ccmp', '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	hp_change_formula, spell_damage_effects)
VALUES (135, 'Arcane Assault', 115043, 1, 3, 1, 6, 0, 0, '-1.2 * ((1 * %ccmp) + (0.25 * %cchp))', '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	hp_change_formula, spell_damage_effects, do_attack_animation)
VALUES (136, 'Spirit Strike', 115009, 1, 1, 1, 6, 0, 0, '-1.3 * %cchp', '1', '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	hp_change_formula, spell_damage_effects, do_attack_animation)
VALUES (137, 'Critical Strike', 115008, 1, 1, 1, 6, 0, 0, '-2.0 * ((%ccmp * 0.5) + (%cchp * 0.5))', '1', '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	mp_percent_regen, buff_removable, buff_graphic,
	buff_doesnt_stack_over, buff_stacks_over)
VALUES (138, 'Rejuvination', 115020, 0, 0, 0, 1, 1, 45, 0.08, '1', 110005, '', '56 57');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, hp_change_formula, spell_damage_effects, min_level_effected)
VALUES (139, 'Restore Health', 115028, 0, 0, 0, 5, 0, 0, '1', '%ccmp', '1', 50);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, buff_graphic, damage_reduce)
VALUES (140, 'Damage Reduction X', 0, 0, 0, 0, 1, 1, 0, '1', 110035, 0.1);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	teleport_map, teleport_x, teleport_y, works_not_in_pvp, min_level_effected)
VALUES (141, 'Paradise Teleportation', 115012, 0, 0, 0, 5, 5, 0, 35, 82, 33, '1', 1);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, buff_graphic, haste)
VALUES (142, 'Haste XXX', 0, 0, 0, 0, 1, 1, 0, '1', 110027, 0.3);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration, spell_damage_effects,
	hp_change_formula, works_not_in_pvp)
VALUES (143, 'Ancient Healing', 115015, 0, 0, 0, 5, 0, 0, '1', '5000', '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	buff_removable, buff_graphic)
VALUES (144, 'Ancient Root', 115038, 0, 5, 5, 2, 8, 25, '0', 110024);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	buff_removable, buff_graphic, hp, works_not_in_pvp)
VALUES (145, 'Ancient Sturdiness', 115016, 0, 0, 0, 1, 1, 180, '1', 110011, 30000, '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	buff_removable, buff_graphic, spell_crit, works_not_in_pvp)
VALUES (146, 'Ancient Criticality', 115026, 0, 0, 0, 1, 1, 240, '1', 110035, 0.05, '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	buff_removable, buff_graphic, spell_crit, damage_reduce, works_not_in_pvp)
VALUES (147, 'Ancient Augmentation', 115041, 0, 0, 0, 1, 1, 120, '1', 110035, 0.1, 0.1, '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	buff_removable, buff_graphic, hp, mp, works_not_in_pvp, min_level_effected)
VALUES (148, 'Ancient Protection', 115044, 0, 5, 4, 5, 1, 300, '1', 110030, 2500, 2500, '1', 50);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	buff_removable, buff_graphic, spell_damage, works_not_in_pvp)
VALUES (149, 'Ancient Buffiness', 115052, 0, 0, 0, 1, 1, 180, '1', 110035, 0.25, '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	buff_removable, buff_graphic, spell_damage, works_not_in_pvp)
VALUES (150, 'Ancient Damage', 115052, 0, 0, 0, 1, 1, 180, '1', 110000, 0.05, '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration, buff_graphic, hp_change_formula, taunt_aggro)
VALUES (151, 'Ancient Taunt', 115014, 0, 0, 0, 2, 0, 0, 110000, '-10', 100000);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration, buff_graphic, mp_change_formula, works_not_in_pvp, spell_damage_effects, min_level_effected)
VALUES (152, 'Ancient Sacrifice', 115014, 0, 0, 0, 4, 0, 0, 110007, '5000', '1', '0', 50);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	buff_graphic)
VALUES (153, 'Smoke Bomb', 115051, 1, 5, 7, 2, 7, 30, 110026);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration, buff_graphic, works_not_in_pvp, 
	hp_change_formula, spell_damage_effects)
VALUES (154, 'Group Heal', 115015, 0, 0, 0, 5, 0, 0, 110006, '1', '300', '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	buff_graphic, works_not_in_pvp)
VALUES (155, 'Warrior Root', 115038, 0, 1, 1, 2, 8, 25, 110024, '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	hair_r, hair_g, hair_b, hair_a)
VALUES (156, 'Hair Dye: Lime Green', 0, 0, 0, 1, 1, 2, 0, 40, 255, 0, 160);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	hair_r, hair_g, hair_b, hair_a)
VALUES (157, 'Hair Dye: Zelius'' Dye', 0, 0, 0, 1, 1, 2, 0, 255, 255, 255, 180);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	hair_r, hair_g, hair_b, hair_a)
VALUES (158, 'Hair Dye: Fayt Dye', 0, 0, 0, 1, 1, 2, 0, 148, 0, 0, 209);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	hair_r, hair_g, hair_b, hair_a)
VALUES (159, 'Hair Dye: Frozen Spit', 0, 0, 0, 1, 1, 2, 0, 164, 219, 247, 200);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, buff_graphic, hp_static_regen)
VALUES (160, 'Hit Point Regeneration M', 0, 0, 0, 0, 1, 1, 0, '1', 110008, 1000);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, buff_graphic, mp_static_regen)
VALUES (161, 'Mana Point Regeneration M', 0, 0, 0, 0, 1, 1, 0, '1', 110032, 1000);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	buff_graphic, buff_removable, on_attack_spell_effect_id, buff_doesnt_stack_over, buff_stacks_over, on_attack_spell_chance)
VALUES (162, 'Ancient Poison', 115041, 0, 0, 0, 1, 13, 0, 110014, '1', 163, '', '', 75);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	hp_change_formula)
VALUES (163, 'Ancient Poison', 115027, 0, 1, 1, 6, 3, 20, '-500');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, buff_graphic, spell_damage)
VALUES (164, 'Increased Spell Damage XIII', 0, 0, 0, 0, 1, 1, 0, '1', 110035, 0.13);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration, buff_graphic, hp_change_formula, taunt_aggro)
VALUES (165, 'Ancient Bellow', 115014, 0, 0, 0, 2, 0, 0, 110000, '-200', 1000000);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
  target_type, target_size, spell_effected, effect_type, effect_duration,
  hp_change_formula, spell_damage_effects)
VALUES (166, 'Ancient Conflagration', 115031, 1, 5, 7, 2, 0, 0, '-((%cchp * 1.5) + (%ccmp * 0.3))', '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
  target_type, target_size, spell_effected, effect_type, effect_duration,
  hp_change_formula, spell_damage_effects)
VALUES (167, 'Ancient Death', 115026, 1, 6, 7, 2, 0, 0, '-((%cchp * 2) + (%ccmp * 0.5))', '1');
 
INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
  target_type, target_size, spell_effected, effect_type, effect_duration,
  hp_change_formula, spell_damage_effects)
VALUES (168, 'Ancient Awe', 115048, 1, 5, 7, 2, 0, 0, '-((%cchp * .2) + (%ccmp * 2.8))', '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
        target_type, target_size, spell_effected, effect_type, effect_duration,
        hair_r, hair_g, hair_b, hair_a)
VALUES (169, 'Hair Dye: Purple Haze', 0, 0, 0, 1, 1, 2, 0, 116, 12, 108, 145);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	buff_removable, buff_graphic, hp, mp, works_not_in_pvp, min_level_effected, 
	spell_crit, spell_damage, hp_percent_regen, mp_percent_regen, stat_ac, buff_stacks_over, buff_doesnt_stack_over)
VALUES (170, 'Ancient Blessings', 115030, 0, 0, 0, 5, 4, 30, '1', 110035, 2500, 2500, '1', 50, 0.25, 0.25, 0.25, 0.25, 200, '171', '222');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	buff_removable, buff_graphic, hp, mp, works_not_in_pvp, min_level_effected, 
	spell_crit, spell_damage, hp_percent_regen, mp_percent_regen, stat_ac, buff_doesnt_stack_over)
VALUES (171, 'Spiritual Blessings', 115014, 0, 0, 0, 5, 4, 20, '1', 110007, 1000, 1000, '1', 50, 0.10, 0.10, 0.10, 0.10, 100, '170 222');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	teleport_map, teleport_x, teleport_y, works_not_in_pvp, min_level_effected)
VALUES (172, 'Ancients Dungeon Teleportation', 115012, 0, 0, 0, 5, 5, 0, 24, 5, 92, '1', 50);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, buff_graphic, damage_reduce)
VALUES (173, 'Damage Reduction I', 0, 0, 0, 0, 1, 1, 0, '1', 110035, 0.01);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, buff_graphic, damage_reduce)
VALUES (174, 'Damage Reduction II', 0, 0, 0, 0, 1, 1, 0, '1', 110035, 0.02);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, buff_graphic, damage_reduce)
VALUES (175, 'Damage Reduction III', 0, 0, 0, 0, 1, 1, 0, '1', 110035, 0.03);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, buff_graphic, damage_reduce)
VALUES (176, 'Damage Reduction IV', 0, 0, 0, 0, 1, 1, 0, '1', 110035, 0.04);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, buff_graphic, damage_reduce)
VALUES (177, 'Damage Reduction V', 0, 0, 0, 0, 1, 1, 0, '1', 110035, 0.05);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration, spell_damage_effects,
	mp_change_formula, works_not_in_pvp, min_level_effected)
VALUES (178, 'Sacrifice II', 115014, 0, 0, 0, 5, 0, 0, '0', '10000', '1', 50);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	buff_removable, buff_graphic, spell_damage, works_not_in_pvp)
VALUES (179, 'Damage of the Bear', 115052, 0, 0, 0, 5, 1, 240, '1', 110035, 0.1, '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	buff_removable, buff_graphic, spell_crit, works_not_in_pvp)
VALUES (180, 'Critical Blow of the Bear', 115026, 0, 0, 0, 5, 1, 240, '1', 110035, 0.1, '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	hp_change_formula, taunt_aggro, spell_damage_effects)
VALUES (181, 'Roar of the Bear', 115014, 1, 5, 4, 2, 0, 0, '-1000', 1000000, '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, buff_graphic, haste, body_id)
VALUES (182, 'Wolfs Essence', 0, 0, 0, 0, 1, 1, 0, '1', 110027, 0.3, 164);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	buff_removable, buff_graphic, works_not_in_pvp, body_id)
VALUES (183, 'Illusion: Bat', 115014, 0, 0, 0, 5, 1, 300, '1', 110007, '1', 100);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	buff_removable, buff_graphic, works_not_in_pvp, body_id)
VALUES (184, 'Illusion: Shroom', 115014, 0, 0, 0, 5, 1, 300, '1', 110007, '1', 132);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	buff_removable, buff_graphic, works_not_in_pvp, body_id)
VALUES (185, 'Illusion: Bear', 115014, 0, 0, 0, 5, 1, 300, '1', 110007, '1', 165);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
        target_type, target_size, spell_effected, effect_type, effect_duration,
        works_not_in_pvp, stat_ac, buff_graphic, buff_removable)
VALUES (186, 'Shard of Earth', 115016, 0, 0, 0, 1, 1, 60, '1', 500, 110018, '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
        target_type, target_size, spell_effected, effect_type, effect_duration,
        works_not_in_pvp, stat_str, buff_graphic, buff_removable)
VALUES (187, 'Shard of Strength', 115013, 0, 0, 0, 1, 1, 60, '1', 1000, 110020, '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
        target_type, target_size, spell_effected, effect_type, effect_duration,
        works_not_in_pvp, spell_damage, buff_graphic, buff_removable)
VALUES (188, 'Shard of Love', 115052, 0, 0, 0, 1, 1, 60, '1', 0.2, 110035, '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
        target_type, target_size, spell_effected, effect_type, effect_duration,
        works_not_in_pvp, hp, mp, buff_graphic, buff_removable)
VALUES (189, 'Shard of Life', 115044, 0, 0, 0, 1, 1, 60, '1', 5000, 5000, 110030, '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
        target_type, target_size, spell_effected, effect_type, effect_duration,
        works_not_in_pvp, damage_reduce, buff_graphic, buff_removable)
VALUES (190, 'Shard of Protection', 115041, 0, 0, 0, 1, 1, 60, '1', 0.2, 110035, '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
        target_type, target_size, spell_effected, effect_type, effect_duration,
        works_not_in_pvp, hp, mp, spell_damage, spell_crit, hp_percent_regen, mp_percent_regen, buff_graphic, buff_removable)
VALUES (191, 'Shard of Power', 115024, 0, 0, 0, 1, 1, 60, '1', 7000, 7000, 0.2, 0.1, 0.02, 0.02, 110001, '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
        target_type, target_size, spell_effected, effect_type, effect_duration,
        works_not_in_pvp, damage_reduce, buff_graphic, buff_removable)
VALUES (192, 'Shard of Invincibility', 115041, 0, 0, 0, 1, 1, 60, '1', 1.0, 110035, '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
        target_type, target_size, spell_effected, effect_type, effect_duration,
        works_not_in_pvp, spell_crit, buff_graphic, buff_removable)
VALUES (193, 'Shard of Hope', 115026, 0, 0, 0, 1, 1, 60, '1', 0.2, 110035, '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
        target_type, target_size, spell_effected, effect_type, effect_duration,
        works_not_in_pvp, hp_percent_regen, mp_percent_regen, buff_graphic, buff_removable)
VALUES (194, 'Shard of Divinity', 115045, 0, 0, 0, 1, 1, 60, '1', 0.04, 0.04, 110032, '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
        target_type, target_size, spell_effected, effect_type, effect_duration,
        hp_change_formula)
VALUES (195, 'Shard of Fire', 115002, 1, 6, 4, 6, 0, 0, '-20000');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
        target_type, target_size, spell_effected, effect_type, effect_duration,
        hp_change_formula)
VALUES (196, 'Shard of Death', 115026, 1, 1, 1, 6, 0, 0, '-10000');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
        target_type, target_size, spell_effected, effect_type, effect_duration,
        hp_change_formula)
VALUES (197, 'Shard of Water', 115003, 1, 6, 5, 6, 0, 0, '-30000');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
        target_type, target_size, spell_effected, effect_type, effect_duration,
        hp_change_formula, random_join_chance)
VALUES (198, 'Shard of Air', 115004, 1, 4, 4, 6, 0, 0, '-25000', 40);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration, buff_graphic, works_not_in_pvp, 
	hp_change_formula, spell_damage_effects)
VALUES (199, 'Ancient Group Healing', 115015, 0, 0, 0, 5, 0, 0, 110006, '1', '6000', '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	buff_removable, buff_graphic, spell_damage, works_not_in_pvp)
VALUES (200, 'Ancient Group Damage', 115041, 0, 0, 0, 5, 1, 240, '1', 110027, 0.05, '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, buff_graphic, mp_percent_regen, hp_percent_regen)
VALUES (201, 'Ancient Regeneration', 0, 0, 0, 0, 5, 1, 300, '1', 110008, 0.01, 0.01);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	buff_removable, buff_graphic, hp, mp, works_not_in_pvp)
VALUES (202, 'Augment', 115016, 0, 0, 0, 5, 1, 300, '1', 110001, 250, 500, '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	buff_removable, buff_graphic, hp_static_regen, mp_static_regen, stat_str, works_not_in_pvp)
VALUES (203, 'Empower', 115013, 0, 0, 0, 5, 1, 300, '1', 110020, 25, 25, 30, '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	buff_removable, buff_graphic, haste, works_not_in_pvp)
VALUES (204, 'Bustle', 115022, 0, 0, 0, 5, 1, 300, '1', 110021, 0.05, '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	buff_removable, buff_graphic, melee_damage, works_not_in_pvp)
VALUES (205, 'Aggravate', 115022, 0, 0, 0, 5, 1, 300, '1', 110021, 0.10, '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	buff_removable, buff_graphic, mp_static_regen, works_not_in_pvp)
VALUES (206, 'Meditate', 115045, 0, 0, 0, 1, 1, 120, '1', 110032, 50, '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	buff_removable, buff_graphic, stat_str, works_not_in_pvp)
VALUES (207, 'Bulk', 115013, 0, 0, 0, 1, 1, 120, '1', 110020, 70, '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	buff_removable, buff_graphic, melee_crit, works_not_in_pvp)
VALUES (208, 'Tumble', 115041, 0, 0, 0, 1, 1, 120, '1', 110027, 0.10, '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	buff_removable, buff_graphic, hp_static_regen, works_not_in_pvp)
VALUES (209, 'Forge', 115013, 0, 0, 0, 1, 1, 120, '1', 110008, 100, '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, hp_change_formula, mp_change_formula, spell_damage_effects, min_level_effected)
VALUES (210, 'Potion of Restoration', 115028, 0, 0, 0, 5, 0, 0, '1', '%chp', '%cmp', '0', 1);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, buff_graphic, spell_crit)
VALUES (211, 'Spell Critical Damage III', 0, 0, 0, 0, 1, 1, 0, '1', 110035, 0.03);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, buff_graphic, spell_crit, spell_damage)
VALUES (212, 'Spell Critical and Damage III', 0, 0, 0, 0, 1, 1, 0, '1', 110035, 0.03, 0.03);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, buff_graphic, spell_damage, damage_reduce)
VALUES (213, 'Royal Mischief Blessing', 0, 0, 0, 0, 1, 1, 0, '1', 110035, 0.08, 0.08);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, buff_graphic, spell_damage, damage_reduce)
VALUES (214, 'Royal Knight Blessing', 0, 0, 0, 0, 1, 1, 0, '1', 110035, 0.02, 0.13);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, buff_graphic, spell_damage)
VALUES (215, 'Increased Spell Damage XV', 0, 0, 0, 0, 1, 1, 0, '1', 110035, 0.15);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, buff_graphic, spell_crit)
VALUES (216, 'Spell Critical Damage VIII', 0, 0, 0, 0, 1, 1, 0, '1', 110035, 0.08);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, buff_graphic, spell_crit, spell_damage)
VALUES (217, 'Spell Critical V and Damage XX', 0, 0, 0, 0, 1, 1, 0, '1', 110035, 0.05, 0.20);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, buff_graphic, spell_crit, damage_reduce)
VALUES (218, 'Spell Critical V and Reduction XV', 0, 0, 0, 0, 1, 1, 0, '1', 110035, 0.05, 0.15);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, buff_graphic, spell_crit, damage_reduce, spell_damage)
VALUES (219, 'Slayers Blessing', 0, 0, 0, 0, 1, 1, 0, '1', 110035, 0.03, 0.03, 0.03);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	mp_change_formula)
VALUES (220, 'Mischiefs Craft', 115046, 0, 0, 0, 1, 0, 0, '%cchp * .5');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, hp_percent_regen, buff_removable, buff_graphic,
	buff_doesnt_stack_over)
VALUES (221, 'Wizards Curse', 115013, 0, 0, 0, 2, 1, 30, '1', -0.02, '0', 110008, '');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	buff_removable, buff_graphic, hp, mp, works_not_in_pvp, min_level_effected, 
	spell_crit, spell_damage, hp_percent_regen, mp_percent_regen, stat_ac, buff_stacks_over)
VALUES (222, 'Clerics Blessing', 115030, 0, 0, 0, 5, 4, 20, '1', 110035, 3500, 3500, '1', 50, 0.35, 0.35, 0.35, 0.35, 350, '171 170');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, hp, buff_graphic, buff_removable,
	buff_doesnt_stack_over, buff_stacks_over, min_level_effected)
VALUES (223, 'Knights Blessing', 115016, 0, 0, 0, 1, 1, 180, '1', 100000, 110018, '1', '', '', 50);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	hair_r, hair_g, hair_b, hair_a)
VALUES (224, 'Hair Dye: Trouble', 0, 0, 0, 1, 1, 2, 0, 255, 0, 125, 180);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	hair_r, hair_g, hair_b, hair_a)
VALUES (225, 'Hair Dye: Mald''s Dye', 0, 0, 0, 1, 1, 2, 0, 234, 139, 173, 180);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration, spell_damage_effects,
	hp_change_formula, works_not_in_pvp)
VALUES (226, 'First Aid', 115015, 0, 0, 0, 1, 0, 0, '1', '250', '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration, spell_damage_effects,
	hp_change_formula, works_not_in_pvp)
VALUES (227, 'Recovery', 115015, 0, 0, 0, 5, 0, 0, '1', '1000', '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration, spell_damage_effects,
	hp_change_formula)
VALUES (228, 'Clobber', 115000, 1, 1, 1, 6, 0, 0, '1', '-250');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration, spell_damage_effects,
	hp_change_formula)
VALUES (229, 'Pummel', 115000, 1, 0, 0, 6, 0, 0, '1', '-1000');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration, only_hits_one_npc)
VALUES (230, 'Tame', 115000, 1, 5, 5, 2, 15, 0, '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration)
VALUES (231, 'Pet Attack', 115000, 1, 0, 0, 7, 16, 0);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration)
VALUES (232, 'Pet Defend', 115000, 1, 0, 0, 4, 17, 0);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration)
VALUES (233, 'Pet Recall', 115000, 1, 0, 0, 4, 18, 0);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration)
VALUES (234, 'Pet Follow', 115000, 1, 0, 0, 4, 19, 0);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration)
VALUES (235, 'Pet Neutral', 115000, 1, 0, 0, 4, 20, 0);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	hair_r, hair_g, hair_b, hair_a)
VALUES (236, 'Hair Dye: Green', 0, 0, 0, 1, 1, 2, 0, 0, 255, 0, 180);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	hair_r, hair_g, hair_b, hair_a)
VALUES (237, 'Hair Dye: Blonde', 0, 0, 0, 1, 1, 2, 0, 253, 232, 80, 160);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	teleport_map, teleport_x, teleport_y, min_level_effected, works_not_in_pvp)
VALUES (238, 'PVP Event Teleport', 115012, 0, 0, 0, 5, 5, 0, 22, 25, 24, 0, '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	hair_r, hair_g, hair_b, hair_a)
VALUES (239, 'Hair Dye: Rampant Rape', 0, 0, 0, 1, 1, 2, 0, 25, 25, 65, 215);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	hair_r, hair_g, hair_b, hair_a)
VALUES (240, 'Hair Dye: Beowulf Sperm', 0, 0, 0, 1, 1, 2, 0, 280, 113, 39, 5180);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	hair_r, hair_g, hair_b, hair_a)
VALUES (241, 'Hair Dye: Sorwind''s Dye', 0, 0, 0, 1, 1, 2, 0, 300, 300, 300, 550);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration, spell_damage_effects,
	hp_change_formula, works_not_in_pvp)
VALUES (242, 'Ancient Healing 2', 115015, 0, 0, 0, 5, 0, 0, '1', '10000', '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	hp_change_formula, spell_damage_effects, works_not_in_pvp)
VALUES (243, 'Death Touch', 115032, 0, 0, 0, 6, 0, 0, '-(%thp * 100)', '1', '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, buff_graphic, spell_crit)
VALUES (244, 'Increased Spell Critical V', 0, 0, 0, 0, 1, 1, 0, '1', 110035, 0.05);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	works_not_in_pvp, buff_graphic, spell_crit, spell_damage, damage_reduce, hp_static_regen, mp_static_regen)
VALUES (245, 'Spirit Power', 0, 0, 0, 0, 1, 1, 0, '1', 110035, 0.025, 0.025, 0.025, 500, 500);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	teleport_map, teleport_x, teleport_y, min_level_effected, works_not_in_pvp)
VALUES (246, 'Ancients Dungeon Teleport', 115012, 0, 0, 0, 5, 5, 0, 40, 19, 98, 0, '1');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	mp_change_formula)
VALUES (247, 'Ancient Covenant', 115046, 0, 0, 0, 1, 0, 0, '0.9 * %cchp');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration, buff_graphic, mp_change_formula, works_not_in_pvp, spell_damage_effects, min_level_effected)
VALUES (248, 'Ancient Sacrifice 2', 115014, 0, 0, 0, 4, 0, 0, 110007, '10000', '1', '0', 50);

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	hp_change_formula, taunt_aggro, spell_damage_effects)
VALUES (249, 'Ancient Taunt 2', 115014, 1, 5, 4, 2, 0, 0, '-5000', 5000000, '0');

INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_animation, spell_display, 
	target_type, target_size, spell_effected, effect_type, effect_duration,
	hair_r, hair_g, hair_b, hair_a)
VALUES (250, 'Hair Dye: Wesley Snipers', 0, 0, 0, 1, 1, 2, 0, 1, 1, 1, 255);

SET IDENTITY_INSERT spell_effects OFF;