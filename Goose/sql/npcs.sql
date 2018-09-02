USE IllutiaGoose;

DROP TABLE npc_templates;
CREATE TABLE npc_templates (
  npc_id INT IDENTITY(1,1) NOT NULL,
  npc_type SMALLINT DEFAULT 2 NOT NULL,
  npc_name VARCHAR(50) NOT NULL,
  npc_title VARCHAR(50) DEFAULT ' ' NOT NULL,
  npc_surname VARCHAR(50) DEFAULT ' ' NOT NULL,
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
  equipped_items VARCHAR(100) DEFAULT '0,*,0,*,0,*,0,*,0,*,0,*' NOT NULL,
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
  script_path TEXT DEFAULT 'Scripts/BaseNPC.csx' NOT NULL,
  
  PRIMARY KEY(npc_id)
);

SET IDENTITY_INSERT npc_templates ON;

INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, experience, aggro_range, attack_range, attack_speed, move_speed, stunnable, rootable, slowable, npc_hp, class_id, body_id, weapon_damage)
VALUES (1, 2, 'Lamb', 40, 40, 0, 1, 1.5, 2, 1, 1, 1, 50, 4, 104, 6);

INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, experience, aggro_range, attack_range, attack_speed, move_speed, stunnable, rootable, slowable, npc_hp, class_id, body_id, weapon_damage, script_path)
VALUES (2, 2, 'Mouse', 30, 20, 0, 1, 1.5, 2, 1, 1, 1, 20, 4, 122, 4, 'Scripts/SpellNPC.csx');

INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, experience, aggro_range, attack_range, attack_speed, move_speed, stunnable, rootable, slowable, npc_hp, class_id, body_id, weapon_damage)
VALUES (3, 2, 'Sheep', 40, 60, 0, 1, 1.5, 2, 1, 1, 1, 75, 4, 112, 10);

INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, experience, aggro_range, attack_range, attack_speed, move_speed, stunnable, rootable, slowable, npc_hp, class_id, body_id, weapon_damage)
VALUES (4, 2, 'Cow', 45, 100, 0, 1, 1.2, 1.5, 1, 1, 1, 120, 4, 116, 25);

INSERT INTO npc_templates (npc_id, npc_type, npc_name, npc_title, respawn_time, experience, aggro_range, attack_range, attack_speed, move_speed, rootable, slowable, npc_hp, class_id, body_id, body_r, body_g, body_b, body_a, weapon_damage, hp_static_regen, stuck_behaviour)
VALUES (5, 2, 'Cow', 'Enraged', 1800, 600, 2, 2, 1.2, 1.5, 1, 1, 750, 4, 116, 229, 58, 31, 150, 55, 10, 2);

INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, experience, stationary, invincible, npc_hp, class_id, body_id, face_id, hair_id, quest_ids)
VALUES (6, 12, 'Snusnu', 5, 1, 1, 1, 999, 4, 1, 1, 1, '1');

INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, experience, stationary, invincible, npc_hp, class_id, body_id, face_id, hair_id, quest_ids)
VALUES (7, 12, 'Nusnus', 5, 1, 1, 1, 999, 4, 1, 1, 1, '2');

INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, experience, stationary, invincible, npc_hp, class_id, body_id, face_id, hair_id, quest_ids)
VALUES (8, 12, 'UsnUsn', 5, 1, 1, 1, 999, 4, 1, 1, 1, '3');


SET IDENTITY_INSERT npc_templates OFF;

DROP TABLE npc_spawns;
CREATE TABLE npc_spawns (
  id INT IDENTITY(1, 1) NOT NULL,
  npc_id INT NOT NULL,
  map_id SMALLINT NOT NULL,
  map_x SMALLINT NOT NULL,
  map_y SMALLINT NOT NULL,
  
  PRIMARY KEY(id)
);

INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (5, 2, 34, 132);

INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 2, 47, 130);

INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 2, 43, 132);

INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 2, 40, 129);

INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 2, 34, 130);

INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 2, 30, 133);

INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 2, 27, 129);

INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 2, 24, 132);

INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 2, 20, 129);

INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 2, 19, 134);

INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 2, 28, 136);

INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 2, 34, 133);

INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 2, 37, 137);

INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 2, 42, 138);

INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (4, 2, 46, 135);

INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (3, 2, 56, 132);

INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (3, 2, 58, 134);

INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (3, 2, 59, 137);

INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (3, 2, 57, 133);

INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (3, 2, 61, 135);

INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (3, 2, 60, 136);

INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (3, 2, 62, 133);

INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (3, 2, 63, 138);

INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (1, 2, 66, 155);

INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (1, 2, 65, 145);

INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (1, 2, 68, 137);

INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (1, 2, 72, 132);

INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (1, 2, 77, 131);

INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (1, 2, 74, 128);

INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (1, 2, 83, 128);

INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (1, 2, 84, 131);

INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (1, 2, 88, 130);

INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (1, 2, 87, 128);

INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (2, 2, 65, 179);

INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (2, 2, 48, 182);

INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (2, 2, 45, 177);

INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (2, 2, 55, 174);

INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (6, 2, 45, 182);

INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (7, 2, 81, 127);

INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y)
VALUES (8, 2, 63, 132);

DROP TABLE npc_drops;
CREATE TABLE npc_drops (
  npc_template_id INT NOT NULL,
  item_template_id INT NOT NULL,
  stack INT NOT NULL,
  droprate DECIMAL(9,4) NOT NULL
);

INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (5, 15, 1, 10);

INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (5, 16, 1, 10);

INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (5, 17, 1, 10);

INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (5, 18, 1, 10);

INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (1, 48, 1, 10);

INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (3, 48, 1, 10);

INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (4, 48, 1, 10);

INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (1, 51, 1, 10);

INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (3, 51, 1, 10);

INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (4, 51, 1, 10);

INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (2, 54, 1, 30);

INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (3, 55, 1, 20);

INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate)
VALUES (4, 56, 1, 20);


DROP TABLE npc_vendor_items;
CREATE TABLE npc_vendor_items (
  npc_template_id INT NOT NULL,
  item_template_id INT NOT NULL,
  stack INT DEFAULT 1 NOT NULL,
  stats_visible CHAR(1) DEFAULT '1' NOT NULL,
  slot INT NOT NULL
);
