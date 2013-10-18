USE Goose;

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
  
  PRIMARY KEY(npc_id)
);

SET IDENTITY_INSERT npc_templates ON;

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, 
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen, 
	attack_range, attack_speed) 
VALUES (1, 'Mouse', 113, 1, 0, 0, 0, 2.0, 1, 4, 4, 20, 40, 0, 1, 1.5);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, 
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen, 
	attack_range, attack_speed) 
VALUES (2, 'Lamb', 114, 1, 0, 0, 0, 2.0, 2, 4, 6, 40, 40, 0, 1, 1.5);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, 
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen, 
	attack_range, attack_speed, npc_alliance,stationary,slowable) 
VALUES (3, 'Fluffy Little Bunny', 140, 1, 0, 0, 1, 1.5, 4, 4, 9, 104, 30, 0, 1, 1.4,'3','1','1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, 
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen, 
	attack_range, attack_speed, npc_alliance,stationary,slowable) 
VALUES (4, 'Rabid Rabbit', 141, 1, 0, 0, 2, 1.3, 8, 4, 18, 186, 30, 0, 1, 1.4, '3 4','1','1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance,stationary,slowable)
VALUES (5, 'Cottontail', 136, 1, 0, 0, 1, 1.5, 9, 2, 28, 250, 1800, 0, 1, 1.5,'3 4','1','1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance,slowable)
VALUES (6, 'Asp', 123, 1, 0, 0, 1, 1.3, 6, 2, 13, 124, 30, 0, 1, 1.4,'6','1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance,slowable)
VALUES (7, 'Bat', 100, 1, 0, 0, 3, 1.5, 12, 2, 38, 266, 30, 0, 1, 1.4,'7','1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed,slowable)
VALUES (8, 'Cow', 117, 1, 0, 0, 0, 1.5, 21, 2, 90, 448, 30, 0, 1, 1.5,'1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed,slowable)
VALUES (9, 'Ram', 104, 1, 0, 0, 0, 1.5, 18, 2, 72, 368, 30, 0, 1, 1.5,'1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, slowable,npc_alliance)
VALUES (10, 'Weak Zombie', 112, 1, 0, 0, 2, 1.3, 20, 4, 80, 404, 30, 0, 1, 1.3,'1','10');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, slowable,rootable,stationary,npc_alliance)
VALUES (11, 'Weak Skeleton', 106, 1, 0, 0, 3, 2, 30, 3, 150, 609, 60, 0, 1, 1.5, '1','1','1','11 12 13 14 15 16 17');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, slowable,rootable,stationary,npc_alliance)
VALUES (12, 'Weak Skeleton', 106, 1, 0, 0, 3, 2, 30, 3, 150, 609, 60, 0, 1, 1.5, '1','1','0','11 12 13 14 15 16 17'); /* Moving */

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, slowable,stunnable,stationary,npc_alliance)
VALUES (13, 'Skeleton', 106, 1, 0, 0, 4, 2, 35, 3, 180, 709, 60, 0, 1, 1.5, '1','1','1','11 12 13 15 16 17');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, slowable,stationary,npc_alliance)
VALUES (14, 'Servant of the Dead', 125, 1, 0, 0, 4, 1.3, 40, 3, 210, 809, 60, 0, 1, 1.5,'1','1','11 12 13 14 15 16 17');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, slowable,stationary,npc_alliance)
VALUES (15, 'Boney', 125, 1, 0, 0, 4, 1.3, 42, 3, 240, 1000, 3600, 0, 1, 1.5,'1','1','11 12 13 14 15 16 17');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, slowable,stationary,npc_alliance)
VALUES (16, 'Misplaced Spouse', 125, 1, 0, 0, 4, 1.3, 43, 3, 240, 1100, 3600, 0, 1, 1.5,'1','1','11 12 13 14 15 16 17');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed,slowable,stationary,npc_alliance)
VALUES (17, 'Record Keeper', 125, 1, 0, 0, 5, 1.3, 45, 3, 270, 1200, 3600, 0, 1, 1.,'1','1','11 12 13 14 15 16 17');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance,slowable,rootable,stunnable)
VALUES (18, 'Piglet', 161, 1, 0, 0, 1, 1.4, 21, 4, 57, 400, 30, 0, 1, 1.4, '18 19','1','1','1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance, slowable,rootable)
VALUES (19, 'Flying Piglet', 162, 1, 0, 0, 3, 1.2, 23, 3, 72, 600, 3600, 0, 1, 1.2, '18 19','1','1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance,slowable,stationary)
VALUES (20, 'Lost Wabbit', 139, 1, 0, 0, 2, 1.5, 12, 4, 51, 246, 60, 0, 1, 1.3,'20 21 22 23','1','1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance,slowable,stationary)
VALUES (21, 'Cute Defenseless Stalker', 138, 1, 0, 0, 2, 1.5, 15, 2, 58, 306, 60, 0, 1, 1.3, '20 21 22 23','1','1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance,slowable,stationary)
VALUES (22, 'Roger', 136, 1, 0, 0, 3, 1.3, 14, 3, 60, 356, 3600, 0, 1, 1.3, '20 21 22 23','1','1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance,slowable,stationary)
VALUES (23, 'Leafeater', 136, 1, 0, 0, 3, 1.3, 17, 3, 69, 406, 3600, 0, 1, 1.3, '20 21 22 23','1','1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance,slowable,rootable,stunnable)
VALUES (24, 'Pipsqueek', 161, 1, 0, 0, 1, 1.4, 12, 4, 57, 246, 30, 0, 1, 1.4, '24 25','1','1','1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance,slowable,rootable)
VALUES (25, 'Flying Pipsqueek', 162, 1, 0, 0, 3, 1.2, 14, 3, 60, 400, 3600, 0, 1, 1.2, '24 25','1','1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance,slowable)
VALUES (26, 'Fire Asp', 122, 1, 0, 0, 1, 1.3, 25, 4, 96, 508, 30, 0, 1, 1.3,'26 27 28','1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance,slowable, equipped_items, rootable)
VALUES (27, 'Naga Warrior', 2, 4, 71, 0, 2, 1.3, 28, 3, 112, 568, 30, 0, 1, 1.5,'26 27 28','1',
'11,*,4,148,231,148,160,0,*,0,*,0,*, 4,*','1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance,slowable, equipped_items, rootable)
VALUES (28, 'Naga Rogue', 2, 4, 71, 0, 2, 1.3, 30, 2, 105, 608, 30, 0, 1, 1.1,'26 27 28','1',
'11,*,4,148,231,148,160,0,*,0,*,0,*,1,*','1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance,slowable,stationary)
VALUES (29, 'Frozen Waste', 134, 1, 0, 0, 2, 1.2, 35, 3, 126, 709, 60, 0, 1, 1.5, '29 30 31 32 33','1','1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance,slowable) /* Moving */
VALUES (30, 'Frozen Waste', 134, 1, 0, 0, 2, 1.2, 35, 3, 126, 709, 60, 0, 1, 1.5, '29 30 31 32 33','1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance,slowable,stationary)
VALUES (31, 'Iceman', 134, 1, 0, 0, 2, 1.4, 30, 3, 111, 609, 60, 0, 1, 1.5, '29 30 31 32 33','1','1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance,slowable) /* Moving */
VALUES (32, 'Iceman', 134, 1, 0, 0, 2, 1.3, 30, 3, 111, 609, 60, 0, 1, 1.5, '29 30 31 32 33','1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance,slowable,stationary,npc_hp)
VALUES (33, 'Frosty', 144, 1, 0, 0, 6, 1.0, 45, 3, 808, 2109, 14400, 0, 2, 1.0, '29 30 31 32 33','1', '1',12000);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance,slowable, stunnable)
VALUES (34, 'Weak Persecution', 119, 1, 0, 0, 1, 1.4, 35, 2, 135, 708, 30, 0, 1, 1.4,'34 35', '1', '1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance, slowable, stunnable)
VALUES (35, 'Moldy Persecution', 119, 1, 0, 0, 3, 1.4, 40, 2, 147, 830, 30, 0, 1, 1.4,'34 35','1', '1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance,slowable, stunnable, stationary)
VALUES (36, 'Private Persecution', 119, 1, 0, 0, 2, 1.4, 40, 4, 141, 809, 30, 0, 1, 1.4,'36 37','1','1', '1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance,slowable, stationary)
VALUES (37, 'Sergeant Persecution', 105, 1, 0, 0, 2, 1.4, 45, 3, 665, 1309, 3600, 0, 3, 1.4,'36','1','1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance,slowable, npc_hp, stationary)
VALUES (38, 'Strong Persecution', 105, 1, 0, 0, 3, 1.3, 50, 3, 330, 1310, 30, 0, 3, 1.3,'34 35 36 37 38','1', 13362, '1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance,slowable)
VALUES (39, 'Persecution', 105, 1, 0, 0, 2, 1.3, 50, 3, 165, 909, 30, 0, 1, 1.4,'39 49','1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance,slowable,rootable,stunnable)
VALUES (40, 'Green Slime', 110, 1, 0, 0, 3, 1.3, 12, 3, 48, 246, 30, 0, 1, 1.3, '40 41 42 43 44 45','1','1','1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance,slowable,rootable,stunnable)
VALUES (41, 'Slime', 109, 1, 0, 0, 3, 1.3, 13, 3, 54, 266, 30, 0, 1, 1.3, '40 41 42 43 44 45','1','1','1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance,slowable,rootable,stunnable)
VALUES (42, 'Pink Slime', 111, 1, 0, 0, 3, 1.3, 14, 3, 57, 286, 30, 0, 1, 1.3, '40 41 42 43 44 45','1','1','1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance,slowable,rootable,stunnable)
VALUES (43, 'Gold Slime', 108, 1, 0, 0, 3, 1.3, 15, 3, 63, 308, 30, 0, 1, 1.3, '40 41 42 43 44 45','1','1','1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance,slowable,rootable,stunnable)
VALUES (44, 'Blue Slime', 120, 1, 0, 0, 3, 1.3, 20, 3, 81, 408, 30, 0, 1, 1.3, '40 41 42 43 44 45','1','1','1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance,slowable,rootable,stunnable)
VALUES (45, 'Red Slime', 107, 1, 0, 0, 3, 1.3, 16, 3, 66, 338, 30, 0, 1, 1.3, '40 41 42 43 44 45','1','1','1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance,slowable, stunnable, stationary)
VALUES (46, 'King Goo', 120, 1, 0, 0, 2, 1.3, 20, 3, 87, 448, 3600, 0, 1, 1.3, '40 41 42 43 44 45','1','1', '1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance,slowable, stationary)
VALUES (47, 'Sliminator', 120, 1, 0, 0, 2, 1.3, 22, 3, 90, 448, 3600, 0, 1, 1.3, '40 41 42 43 44 45','1','1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance,slowable,stationary)
VALUES (48, 'Goo Jr.', 124, 1, 0, 0, 2, 1.3, 28, 3, 114, 600, 7200, 0, 1, 1.3, '40 41 42 43 44 45','1','1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance,slowable, npc_hp) /* Moving mindless mines version */
VALUES (49, 'Strong Persecution', 105, 1, 0, 0, 3, 1.3, 50, 3, 330, 1310, 30, 0, 3, 1.3,'39 49','1', 13362);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance,slowable, equipped_items, rootable, stunnable, npc_hp, stationary)
VALUES (50, 'Nagan Beast', 2, 1, 71, 0, 2, 1, 50, 3, 1100, 1049, 60, 0, 1, 1,'50 51 52 53','1',
'0,*,4,*,0,*,0,*,0,*,0,*','1', '0', 9934, '1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance,slowable, equipped_items, rootable, stunnable, npc_hp, stationary)
VALUES (51, 'Nagan Magus', 2, 3, 71, 0, 3, 1, 50, 4, 949, 949, 60, 0, 4, 1,'50 51 52 53','1',
'13,*,4,*,0,*,0,*,0,*,5,*','1', '1', 4948, '1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance,slowable, equipped_items, rootable, stunnable, npc_hp, stationary)
VALUES (52, 'Nagan Priest', 2, 3, 71, 0, 3, 1, 50, 5, 800, 949, 60, 0, 1, 1,'50 51 52 53','1',
'2,*,6,*,0,*,0,*,0,*,9,*','1', '1', 7142, '1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance, slowable, equipped_items, rootable, stunnable, npc_hp)
VALUES (53, 'Udyana', 2, 4, 71, 0, 4, 1, 50, 3, 5000, 6000, 7200, 0, 3, 1,'50 51 52 53','1',
'10,74,121,41,160,4,*,0,*,0,*,0,*,17,*', '1', '0', 262366);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance,slowable, npc_hp, stationary)
VALUES (54, 'Complex', 105, 1, 0, 0, 4, 1.2, 50, 3, 1500, 1210, 7200, 0, 2, 1.2,'54 55','1', 73130, '1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance,slowable, npc_hp, stationary)
VALUES (55, 'Simple', 105, 1, 0, 0, 4, 1.2, 50, 3, 1000, 1009, 45, 0, 1, 1.2,'54 55','1', 15206, '1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance,slowable, npc_hp, rootable, stunnable, stationary)
VALUES (56, 'Spook', 101, 1, 0, 0, 3, 0.8, 50, 3, 400, 949, 30, 0, 1, 1.1,'56 57 58 59 60','1', 7170, '1', '0', '1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance,slowable, npc_hp, rootable, stunnable, stationary)
VALUES (57, 'Ghast', 102, 1, 0, 0, 3, 0.8, 50, 3, 500, 969, 30, 0, 1, 1.1,'56 57 58 59 60', '1', 11508, '0', '0', '1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance,slowable, npc_hp, rootable, stunnable, stationary)
VALUES (58, 'ImaBitStale', 103, 1, 0, 0, 4, 0.8, 50, 3, 1600, 1210, 7200, 0, 1, 1.1,'56 57 58 59 60', '1', 60206, '0', '0', '1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance,slowable, npc_hp, rootable, stunnable, stationary)
VALUES (59, 'Ecto', 103, 1, 0, 0, 3, 0.8, 50, 3, 750, 1820, 7200, 0, 1, 1.1, '56 57 58 59 60', '1', 38206, '0', '0', '1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance,slowable, npc_hp, rootable, stunnable, stationary, stuck_behaviour)
VALUES (60, 'Punchy', 115, 1, 0, 0, 4, 1.1, 50, 3, 5000, 2510, 9600, 0, 2, 1.1,'56 57 58 59 60', '1', 140774, '0', '0', '1', 2);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance,slowable, equipped_items, rootable, stunnable, npc_hp, stationary)
VALUES (61, 'Rabid Savage', 1, 4, 72, 9, 3, 1, 50, 3, 1400, 1910, 30, 0, 1, 1, '61 62 63 64 65 66 67 68','1',
'0,*,0,*,0,*,0,*,0,*,47,*','0', '1', 17130, '1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance,slowable, equipped_items, rootable, stunnable, npc_hp, stationary)
VALUES (62, 'Hungry Savage', 11, 4, 71, 55, 3, 1, 50, 3, 2300, 2410, 30, 0, 1, 1,'61 62 63 64 65 66 67 68','1',
'0,*,0,*,0,*,0,*,0,*,47,*','0', '1', 46774, '1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance,slowable, equipped_items, rootable, stunnable, npc_hp, stationary)
VALUES (63, 'Savage', 1, 4, 73, 8, 3, 1, 50, 3, 1200, 1010, 30, 0, 1, 1, '61 62 63 64 65 66 67 68','1',
'0,*,0,*,0,*,0,*,0,*,47,*','0', '1', 3206, '1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance,slowable, equipped_items, rootable, stunnable, npc_hp, stationary)
VALUES (64, 'Paranoid Savage', 1, 4, 72, 10, 2, 1, 50, 3, 3500, 3510, 30, 0, 1, 1, '61 62 63 64 65 66 67 68','1',
'0,*,0,*,0,*,0,*,0,*,46,*','1', '1', 79366, '1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, npc_level, class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, npc_alliance,slowable, rootable, stunnable, npc_hp, stationary, stuck_behaviour)
VALUES (65, 'Beefcake', 3, 1, 0, 0, 4, 1, 50, 3, 20000, 6000, 28800, 0.01, 3, 1, '61 62 63 64 65 66 67 68', '1', '0', '0', 1021221, '1', 2);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance,slowable, equipped_items, rootable, stunnable, npc_hp, stationary, stuck_behaviour)
VALUES (66, 'Copycat', 11, 4, 71, 53, 3, 1, 50, 5, 10000, 1910, 14400, 0, 3, 1,'61 62 63 64 65 66 67 68','1',
'0,*,0,*,3,*,0,*,0,*,50,*','0', '0', 407690, '1', 2);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance,slowable, equipped_items, rootable, stunnable, npc_hp, stationary, stuck_behaviour)
VALUES (67, 'Insanity', 1, 4, 72, 6, 3, 1, 50, 3, 11200, 2610, 14400, 0, 3, 1, '61 62 63 64 65 66 67 68','1',
'0,*,11,*,3,0,0,0,160,0,*,0,*,6,*','0', '0', 666888, '1', 2);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance,slowable, equipped_items, rootable, stunnable, npc_hp, stationary, stuck_behaviour)
VALUES (68, 'Showboat', 1, 3, 72, 6, 3, 1, 50, 3, 10800, 2310, 14400, 0, 3, 1, '61 62 63 64 65 66 67 68','1',
'11,*,5,*,6,*,0,*,0,*,29,*','0', '0', 399502, '1', 2);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance,slowable, equipped_items, rootable, stunnable, npc_hp, stationary)
VALUES (69, 'Savage Isle Guard', 11, 1, 72, 25, 3, 1, 50, 3, 2000, 1000, 30, 0, 3, 1, '69','1',
'0,*,6,*,0,*,0,*,0,*,0,*','0', '0', 4206, '1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance,slowable, npc_hp, stationary)
VALUES (70, 'Worthless Guardian', 119, 1, 0, 0, 4, 1, 50, 3, 900, 1400, 40, 0, 2, 1,'70 71 72 73 74','1', 41206, '1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance,slowable, npc_hp, stationary, rootable)
VALUES (71, 'Little Fat Bastard', 105, 1, 0, 0, 3, 1.3, 50, 3, 20100, 1100, 9000, 0, 2, 1.3,'70 71 72 73 74','1', 1017188, '1', '1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance,slowable, equipped_items, rootable, stunnable, npc_hp, stationary, stuck_behaviour)
VALUES (72, 'Hay', 1, 1, 0, 1, 4, 1, 50, 3, 4200, 3000, 10800, 0, 3, 1, '70 71 72 73 74','1',
'10,66,73,165,160,12,*,3,*,2,*,0,*,15,*','0', '0', 215502, '1', 2);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance,slowable, equipped_items, rootable, stunnable, npc_hp, stationary, stuck_behaviour)
VALUES (73, 'Fray', 1, 3, 0, 51, 4, 1, 50, 3, 3800, 2710, 10800, 0, 3, 1, '70 71 72 73 74','1',
'11,*,0,*,3,*,5,*,0,*,40,*','0', '0', 169502, '1', 2);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance,slowable, npc_hp, stationary)
VALUES (74, 'One with Head Thingy', 105, 1, 0, 0, 4, 1.2, 50, 3, 1100, 1440, 50, 0, 1, 1.2,'70 71 72 73 74','1', 16188, '1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, npc_level, 
	class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, npc_alliance,slowable, equipped_items, rootable, stunnable, npc_hp, stationary, stuck_behaviour)
VALUES (75, 'Ancient Warrior', 2, 4, 0, 16, 4, 1.3, 50, 3, 20000, 23000, 1800, 0.02, 1, 1.2, '75 76','1',
'21,130,60,150,100,0,*,0,*,0,*,0,*,56,*','0', '0', 460502, '1', 1);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, npc_level, 
	class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, npc_alliance,slowable, equipped_items, rootable, stunnable, npc_hp, stuck_behaviour)
VALUES (76, 'Elite Ancient Warrior', 2, 4, 0, 15, 7, 1.3, 50, 3, 40000, 50000, 3600, 0.02, 2, 1.0, '76','1',
'10,60,80,230,140,2,*,0,*,0,*,0,*,55,*','0', '0', 890502, 2);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, npc_level, 
	class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, npc_alliance,slowable, rootable, stunnable, npc_hp, stationary, stuck_behaviour)
VALUES (77, 'Comissioned Blacksmith', 126, 1, 0, 0, 1, 2.0, 50, 3, 12000, 75000, 14400, 0.02, 1, 2.0, '','1','0', '0', 1000502, '1', 1);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, npc_level, 
	class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, npc_alliance,slowable, rootable, stunnable, npc_hp, stationary, stuck_behaviour)
VALUES (78, 'Comissioned Tailor', 116, 1, 0, 0, 1, 2.0, 50, 3, 12000, 75000, 14400, 0.02, 1, 2.0, '','1','0', '0', 1000502, '1', 1);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, npc_level, 
	class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, npc_alliance,slowable, rootable, stunnable, npc_hp, stationary, equipped_items, stuck_behaviour)
VALUES (79, 'Dalph Von''Ownu', 1, 3, 71, 0, 4, 1.0, 50, 3, 90000, 100000, 28800, 0.02, 2, 1.0, '75 76 79 80 81 82 83','1','0', '0', 1000502, '1',
'21,130,60,150,100,21,130,60,150,100,2,170,80,100,100,2,170,80,100,100,0,*,45,*', 1);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, npc_level, 
	class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, npc_alliance,slowable, rootable, stunnable, npc_hp, stationary, equipped_items, stuck_behaviour)
VALUES (80, 'Hairy Fairy Princess', 1, 4, 0, 11, 4, 1.0, 50, 3, 80000, 100000, 28800, 0.02, 2, 1.0, '75 76 79 80 81 82 83','1','0', '0', 1000502, '1',
'6,*,0,*,6,*,5,*,0,*,38,*', 1);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, npc_level, 
	class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, npc_alliance,slowable, rootable, stunnable, npc_hp, stationary, equipped_items, stuck_behaviour)
VALUES (81, 'Lady Slither', 12, 1, 0, 54, 5, 1.0, 50, 3, 70000, 100000, 28800, 0.02, 3, 1.0, '75 76 79 80 81 82 83','1','0', '0', 1000502, '1',
'3,*,26,*,0,*,0,*,0,*,0,*', 1);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, npc_level, 
	class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, npc_alliance,slowable, rootable, stunnable, npc_hp, stationary, equipped_items, stuck_behaviour)
VALUES (82, 'Sir Insidious', 2, 1, 0, 4, 5, 1.0, 50, 3, 70000, 100000, 28800, 0.02, 3, 1.0, '75 76 79 80 81 82 83','1','0', '0', 1000502, '1',
'7,*,25,*,0,*,0,*,0,*,0,*', 1);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, npc_level, 
	class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, npc_alliance,slowable, rootable, stunnable, npc_hp, stationary, equipped_items)
VALUES (83, 'Ztwel Tahp', 1, 1, 71, 20, 3, 1.0, 50, 3, 90000, 100000, 28800, 0.02, 3, 1.0, '75 76 79 80 81 82 83','1','1', '0', 1000502, '1',
'0,*,15,*,0,*,0,*,0,*,0,*');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, npc_level, 
	class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, invincible, stationary, equipped_items, npc_type)
VALUES (84, 'Hair Dye Guy', 1, 1, 71, 20, 0, 0.0, 50, 3, 70000, 0, 30, 0.20, 0, 0.0, '1', '1',
'23,*,0,*,0,*,0,*,0,*,0,*', 13);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, npc_level, 
	class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, invincible, stationary, npc_type)
VALUES (85, 'Hair 1', 1, 1, 0, 20, 0, 0.0, 50, 3, 70000, 0, 30, 0.20, 0, 0.0, '1', '1', 13);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, npc_level, 
	class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, invincible, stationary, npc_type)
VALUES (86, 'Hair 2', 1, 1, 0, 21, 0, 0.0, 50, 3, 70000, 0, 30, 0.20, 0, 0.0, '1', '1', 13);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, npc_level, 
	class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, invincible, stationary, npc_type)
VALUES (87, 'Hair 3', 1, 1, 0, 22, 0, 0.0, 50, 3, 70000, 0, 30, 0.20, 0, 0.0, '1', '1', 13);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, npc_level, 
	class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, invincible, stationary, npc_type)
VALUES (88, 'Hair 4', 1, 1, 0, 23, 0, 0.0, 50, 3, 70000, 0, 30, 0.20, 0, 0.0, '1', '1', 13);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, npc_level, 
	class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, invincible, stationary, npc_type)
VALUES (89, 'Hair 5', 1, 1, 0, 24, 0, 0.0, 50, 3, 70000, 0, 30, 0.20, 0, 0.0, '1', '1', 13);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, npc_level, 
	class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, invincible, stationary, npc_type)
VALUES (90, 'Hair 6', 1, 1, 0, 25, 0, 0.0, 50, 3, 70000, 0, 30, 0.20, 0, 0.0, '1', '1', 13);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, npc_level, 
	class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, invincible, stationary, npc_type)
VALUES (91, 'Hair 7', 1, 1, 0, 26, 0, 0.0, 50, 3, 70000, 0, 30, 0.20, 0, 0.0, '1', '1', 13);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, npc_level, 
	class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, invincible, stationary, npc_type)
VALUES (92, 'Face 1', 1, 1, 70, 0, 0, 0.0, 50, 3, 70000, 0, 30, 0.20, 0, 0.0, '1', '1', 13);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, npc_level, 
	class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, invincible, stationary, npc_type)
VALUES (93, 'Face 2', 1, 1, 71, 0, 0, 0.0, 50, 3, 70000, 0, 30, 0.20, 0, 0.0, '1', '1', 13);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, npc_level, 
	class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, invincible, stationary, npc_type)
VALUES (94, 'Face 3', 1, 1, 72, 0, 0, 0.0, 50, 3, 70000, 0, 30, 0.20, 0, 0.0, '1', '1', 13);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, npc_level, 
	class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, invincible, stationary, npc_type)
VALUES (95, 'Face 4', 1, 1, 73, 0, 0, 0.0, 50, 3, 70000, 0, 30, 0.20, 0, 0.0, '1', '1', 13);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, npc_level, 
	class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, invincible, stationary, npc_type)
VALUES (96, 'Male', 1, 1, 0, 0, 0, 0.0, 50, 3, 70000, 0, 30, 0.20, 0, 0.0, '1', '1', 13);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, npc_level, 
	class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, invincible, stationary, npc_type)
VALUES (97, 'Female', 11, 1, 0, 0, 0, 0.0, 50, 3, 70000, 0, 30, 0.20, 0, 0.0, '1', '1', 13);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, npc_level, 
	class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, invincible, stationary, npc_type, equipped_items)
VALUES (98, 'Magus Trainer', 1, 3, 71, 23, 0, 0.0, 50, 3, 70000, 0, 30, 0.20, 0, 0.0, '1', '1', 13,
'11,*,5,*,6,*,5,*,0,*,3,*');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, npc_level, 
	class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, invincible, stationary, npc_type, equipped_items)
VALUES (99, 'Priest Trainer', 1, 4, 71, 22, 0, 0.0, 50, 3, 70000, 0, 30, 0.20, 0, 0.0, '1', '1', 13,
'7,*,8,*,1,*,1,*,0,*,13,*');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, npc_level, 
	class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, invincible, stationary, equipped_items, npc_type)
VALUES (100, 'Potion Guy', 1, 1, 71, 23, 0, 0.0, 50, 3, 70000, 0, 30, 0.20, 0, 0.0, '1', '1',
'23,*,0,*,0,*,0,*,0,*,0,*', 13);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, npc_level, 
	class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, invincible, stationary, equipped_items, npc_type)
VALUES (101, 'Bronze Armor Guy', 1, 1, 71, 23, 0, 0.0, 50, 3, 70000, 0, 30, 0.20, 0, 0.0, '1', '1',
'10,250,150,50,140,1,20,65,30,160,5,250,150,50,140,4,250,150,50,140,0,*,0,*', 13);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, npc_level, 
	class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, invincible, stationary, equipped_items, npc_type)
VALUES (102, 'Iron Armor Guy', 1, 1, 71, 23, 0, 0.0, 50, 3, 70000, 0, 30, 0.20, 0, 0.0, '1', '1',
'10,*,1,70,70,70,140,5,*,4,*,0,*,0,*', 13);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, npc_level, 
	class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, invincible, stationary, equipped_items, npc_type)
VALUES (103, 'Steel Armor Guy', 1, 1, 71, 23, 0, 0.0, 50, 3, 70000, 0, 30, 0.20, 0, 0.0, '1', '1',
'10,255,255,255,70,1,100,100,100,100,5,255,255,255,70,5,255,255,255,70,0,*,0,*', 13);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, npc_level, 
	class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, invincible, stationary, equipped_items, npc_type)
VALUES (104, 'Shield Guy', 1, 1, 71, 23, 0, 0.0, 50, 3, 70000, 0, 30, 0.20, 0, 0.0, '1', '1',
'10,*,1,*,2,*,2,*,42,*,0,*', 13);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, npc_level, 
	class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, invincible, stationary, equipped_items, npc_type)
VALUES (105, 'Weapon Guy', 1, 4, 71, 23, 0, 0.0, 50, 3, 70000, 0, 30, 0.20, 0, 0.0, '1', '1',
'2,*,6,*,0,*,0,*,43,189,93,90,160,1,*', 13);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, npc_level, 
	class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, invincible, stationary, equipped_items, npc_type)
VALUES (106, 'Cloth Armor Guy', 1, 1, 71, 23, 0, 0.0, 50, 3, 70000, 0, 30, 0.20, 0, 0.0, '1', '1',
'1,*,1,*,3,*,1,*,0,*,0,*', 13);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, npc_level, 
	class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, invincible, stationary, equipped_items, npc_type)
VALUES (107, 'Leather Armor Guy', 1, 1, 71, 23, 0, 0.0, 50, 3, 70000, 0, 30, 0.20, 0, 0.0, '1', '1',
'1,*,1,*,3,*,1,*,0,*,0,*', 13);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, npc_level, 
	class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, invincible, stationary, equipped_items, npc_type)
VALUES (108, 'Silk Armor Guy', 1, 1, 71, 23, 0, 0.0, 50, 3, 70000, 0, 30, 0.20, 0, 0.0, '1', '1',
'2,*,6,*,0,*,0,*,0,*,0,*', 13);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance,slowable, equipped_items, rootable, npc_hp, stationary)
VALUES (109, 'Nagan Sentry', 2, 4, 71, 0, 3, 1.2, 50, 3, 400, 2829, 1980, 0, 1, 1.2,'26 27 28','1',
'21,231,223,107,160,4,*,0,*,0,*,0,*, 16,*','0', 1000, '1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, 
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen, 
	attack_range, attack_speed, npc_alliance,stationary,slowable) 
VALUES (110, 'Carrots', 137, 1, 0, 0, 4, 1.2, 33, 3, 135, 608, 7200, 0, 1, 1.2,'111 112 113 114','1','1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, 
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen, 
	attack_range, attack_speed, npc_alliance,stationary,slowable) 
VALUES (111, 'Fluffzilla', 136, 1, 0, 0, 4, 1.2, 32, 3, 147, 608, 7200, 0, 1, 1.2,'110 112 113 114','1','1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, 
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen, 
	attack_range, attack_speed, npc_alliance,stationary,slowable) 
VALUES (112, 'Snowball', 136, 1, 0, 0, 2, 1.2, 24, 3, 87, 489, 30, 0, 1, 1.2,'110 111 112 113 114','1','1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, 
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen, 
	attack_range, attack_speed, npc_alliance,stationary,slowable) 
VALUES (113, 'Midnight Madness', 138, 1, 0, 0, 2, 1.2, 21, 3, 78, 408, 30, 0, 1, 1.2,'110 111 112 113 114','1','1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, 
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen, 
	attack_range, attack_speed, npc_alliance,stationary,slowable) 
VALUES (114, 'Melty', 134, 1, 0, 0, 3, 1.1, 28, 3, 102, 548, 30, 0, 1, 1.1,'110 111 112 113 114','1','1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, slowable,npc_alliance, stunnable, npc_hp, stationary)
VALUES (115, 'Unloved', 112, 1, 0, 0, 3, 1.2, 35, 2, 132, 909, 30, 0, 1, 1.2,'1','115 116', '1', 540, '1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance,slowable, npc_hp, stationary)
VALUES (116, 'Nibbles', 100, 1, 0, 0, 4, 1.4, 45, 3, 180, 1200, 9000, 0, 2, 1.4,'115','1',3488, '1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance,slowable, npc_hp, rootable, stunnable, stationary)
VALUES (117, 'Wraith', 153, 1, 0, 0, 4, 1.2, 50, 3, 9000, 3000, 30, 0, 2, 1.2,'117 118', '1', 50130, '0', '0', '1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, npc_alliance,slowable, npc_hp, stationary, stuck_behaviour)
VALUES (118, 'Nibbles II', 127, 1, 0, 0, 4, 1.3, 50, 3, 30000, 8000, 14400, 0, 2, 1.3,'117','1',353488, '1', 2);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, npc_level, 
	class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, invincible, stationary, equipped_items, npc_type)
VALUES (119, 'Teleporter Vendor Guy', 1, 1, 71, 20, 0, 0.0, 50, 3, 70000, 0, 30, 0.20, 0, 0.0, '1', '1',
'23,*,0,*,0,*,0,*,0,*,0,*', 13);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, npc_level, 
	class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, invincible, stationary, equipped_items, npc_type)
VALUES (120, 'Stranger with Candy', 1, 1, 71, 20, 0, 0.0, 50, 3, 70000, 0, 30, 0.20, 0, 0.0, '1', '1',
'15,*,0,*,0,*,0,*,0,*,0,*', 13);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, npc_level, 
	class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, invincible, stationary, equipped_items, npc_type, npc_hp)
VALUES (121, 'Paradise Teleporter Vendor Guy', 1, 1, 71, 20, 0, 0.0, 50, 3, 7000, 0, 30, 0.02, 1, 0.3, '0', '1',
'23,*,0,*,0,*,0,*,0,*,0,*', 13, 100000);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, npc_level, 
	class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, invincible, stationary, equipped_items, npc_type)
VALUES (122, 'The Bat Man', 1, 1, 71, 20, 0, 2.0, 50, 3, 70000, 0, 30, 0.20, 0, 0.0, '1', '0',
'15,*,0,*,0,*,0,*,0,*,0,*', 13);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, slowable,stunnable,stationary,npc_alliance, equipped_items, npc_hp, rootable, stuck_behaviour)
VALUES (123, 'Ancient Defender', 3, 4, 0, 0, 3, 1.3, 50, 3, 80000, 64000, 2700, 0.02, 1, 1.5, '1','0','1','123 138','0,*,0,*,0,*,0,*,0,*,4,*', 4135155, '1', 1);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, slowable,stunnable,stationary,npc_alliance, equipped_items, npc_hp, rootable, stuck_behaviour)
VALUES (124, 'Ancient Guard', 3, 4, 0, 0, 3, 1.5, 50, 3, 90000, 72000, 2700, 0.02, 1, 1.5, '1','1','1','124 143','21,*,10,*,5,*,4,*,0,*,37,*', 5021222, '0', 1);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, slowable,stunnable,stationary,npc_alliance, equipped_items, npc_hp, stuck_behaviour)
VALUES (125, 'Ancient Safeguard', 3, 1, 0, 0, 3, 1.3, 50, 3, 200000, 80000, 300, 0.02, 1, 1.5, '1','1','1','125','0,*,0,*,0,*,0,*,0,*,0,*', 40016888, 1);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, slowable,stunnable,stationary,npc_alliance, equipped_items, npc_hp, stuck_behaviour)
VALUES (126, 'General Mehoff', 3, 3, 0, 0, 4, 1.5, 50, 3, 100000, 108000, 28800, 0.02, 3, 1.5, '1','0','1','123 124 125','20,*,0,*,0,*,0,*,0,*,3,*', 20143269, 1);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, 
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_percent_regen, 
	attack_range, attack_speed, stationary, npc_hp, stunnable, slowable, rootable, npc_alliance) 
VALUES (127, 'Berry Stealer', 113, 1, 0, 0, 3, 1.3, 50, 3, 3000, 1400, 3600, 0.02, 1, 1.5, '1', 6678, '1', '1', '1', '157 130 127 131 128');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, 
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_percent_regen, 
	attack_range, attack_speed, npc_hp, npc_alliance, slowable, stunnable, rootable, stationary) 
VALUES (128, 'Guardian Bear', 165, 1, 0, 0, 3, 1.5, 50, 3, 30000, 10410, 600, 0.35, 3, 1.5, 505138, '157 130 127 131 128', '1', '0', '0', '1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, 
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_percent_regen, 
	attack_range, attack_speed, npc_hp, npc_alliance, slowable, stunnable, rootable) 
VALUES (129, 'Patrol Bear', 165, 1, 0, 0, 3, 1.5, 50, 3, 20000, 8310, 3600, 0.25, 3, 1.5, 1362366, '129 156', '1', '0', '1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, 
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_percent_regen, 
	attack_range, attack_speed, npc_hp, npc_alliance, slowable, stunnable, rootable) 
VALUES (130, 'Young Bear', 165, 1, 0, 0, 3, 1.5, 50, 3, 5500, 6510, 40, 0.02, 3, 1.5, 262366, '157 130 127 131 128', '1', '0', '1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, npc_level, class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, npc_alliance,slowable, equipped_items, rootable, stunnable, npc_hp, stationary)
VALUES (131, 'Betsy the Bear Charmer', 11, 4, 71, 54, 3, 1.4, 50, 3, 8000, 7210, 3600, 0.02, 1, 1,'157 130 127 131 128','1',
'8,*,0,*,0,*,0,*,0,*,0,*','0', '0', 316298, '0');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, slowable,rootable,stationary,npc_alliance, npc_hp, equipped_items, stuck_behaviour)
VALUES (132, 'Sorrows Hero', 3, 4, 0, 0, 4, 1.2, 50, 3, 15000, 16000, 14400, 0, 2, 1.2, '1','0','1','133 134', 909934, '21,214,214,214,140,2,214,214,214,140,5,214,214,214,140,4,214,214,214,140,0,*,55,*', 1);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, 
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen, 
	attack_range, attack_speed, npc_hp, slowable, stationary, npc_alliance) 
VALUES (133, 'Pancake', 124, 1, 0, 0, 6, 1.5, 50, 3, 3000, 2210, 40, 0, 2, 1.5, 50130, '1', '1', '132 133 134');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, 
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen, 
	attack_range, attack_speed, npc_hp, slowable, stationary, npc_alliance) 
VALUES (134, 'Flapjack', 124, 1, 0, 0, 6, 1.5, 50, 3, 4000, 2810, 40, 0, 2, 1.5, 67362, '1', '1', '132 133 134');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, npc_alliance,slowable, npc_hp, stationary, rootable, stuck_behaviour)
VALUES (135, 'Cranky Ewe', 114, 1, 0, 0, 0, 1.3, 50, 3, 2000, 2000, 32400, 0.01, 2, 1.3,'','1',22326, '1', '1', 2);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, npc_alliance, slowable, stationary, rootable, stuck_behaviour)
VALUES (136, 'Frantic Monkey', 163, 1, 0, 0, 3, 1.5, 35, 3, 300, 600, 32400, 0.01, 2, 1.3,'','1', '0', '0', 1);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, slowable,rootable,stationary,npc_alliance, npc_hp, equipped_items, stuck_behaviour)
VALUES (137, 'Sorrows Lover', 3, 4, 0, 0, 4, 1.2, 50, 3, 10000, 8000, 14400, 0, 2, 1.2, '1','0','1','133 134', 709934, '9,214,214,214,140,28,214,214,214,140,4,214,214,214,140,0,*,0,*,56,*', 1);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, slowable,stunnable,stationary,npc_alliance, equipped_items, npc_hp, stuck_behaviour)
VALUES (138, 'Ancient Sentinel', 3, 4, 0, 0, 3, 1.3, 50, 3, 95000, 80000, 14400, 0.02, 1, 1.5, '1','0','1','123 138','21,*,0,*,5,*,4,*,0,*,46,*', 10016888, 1);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, slowable,stunnable,stationary,npc_alliance, equipped_items, npc_hp, stuck_behaviour)
VALUES (139, 'General Vor Chez', 3, 3, 0, 0, 4, 1.5, 50, 3, 100000, 108000, 28800, 0.02, 3, 1.5, '1','0','1','123 124 125','20,*,5,*,0,*,0,*,0,*,3,*', 20143269, 1);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, slowable,stunnable,stationary,npc_alliance, equipped_items, npc_hp, stuck_behaviour)
VALUES (140, 'Doom', 3, 4, 0, 0, 4, 1.5, 50, 3, 115000, 108000, 43200, 0.02, 3, 1.5, '1','0','1','123 124 125','22,*,29,*,0,*,0,*,0,*,31,*', 30143269, 1);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, slowable,stunnable,stationary,npc_alliance, equipped_items, npc_hp, stuck_behaviour)
VALUES (141, 'Disciple Von Bangs', 3, 3, 0, 0, 4, 1.5, 50, 3, 90000, 108000, 14400, 0.02, 3, 1.5, '1','0','1','123 124 125','9,*,28,*,4,*,0,*,0,*,28,*', 8143269, 1);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, slowable,stunnable,stationary,npc_alliance, equipped_items, npc_hp, stuck_behaviour)
VALUES (142, 'Disciple Von Chief', 3, 3, 0, 0, 4, 1.5, 50, 3, 90000, 108000, 14400, 0.02, 3, 1.5, '1','0','1','123 124 125','9,*,28,*,4,*,0,*,0,*,28,*', 8143269, 1);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, slowable,stunnable,stationary,npc_alliance, equipped_items, npc_hp, rootable, stuck_behaviour) /* Moving */
VALUES (143, 'Ancient Guard', 3, 4, 0, 0, 3, 1.5, 50, 3, 68000, 72000, 2700, 0.005, 1, 1.5, '1','1','0','124 143','21,*,10,*,5,*,4,*,0,*,37,*', 5021222, '0', 1);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, slowable,rootable,stationary,npc_alliance, npc_hp)
VALUES (144, 'Invisible 2', 0, 4, 0, 0, 4, 1.2, 50, 3, 38000, 64000, 14400, 0, 2, 1.2, '1','0','1','146 144', 2109934);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, 
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen, 
	attack_range, attack_speed, npc_hp, slowable, stationary, npc_alliance) 
VALUES (145, 'Giant Wraith', 115, 1, 0, 0, 10, 1.5, 50, 3, 8000, 5810, 60, 0, 2, 1.5, 100130, '1', '1', '145 147');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, 
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen, 
	attack_range, attack_speed, npc_hp, slowable, stationary, npc_alliance) 
VALUES (146, 'Returned', 125, 1, 0, 0, 6, 1.5, 50, 3, 12000, 7910, 60, 0, 2, 1.5, 140362, '1', '1', '146 144');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen,
	attack_range, attack_speed, slowable,rootable,stationary,npc_alliance, npc_hp)
VALUES (147, 'Invisible 1', 0, 4, 0, 0, 4, 1.2, 50, 3, 28000, 32000, 14400, 0, 2, 1.2, '1','0','1','145 147', 1409934);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, npc_level, 
	class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, invincible, stationary, equipped_items, npc_type)
VALUES (148, 'Tailoring Supplies', 1, 1, 71, 23, 0, 0.0, 50, 3, 70000, 0, 30, 0.20, 0, 0.0, '1', '1',
'10,*,1,70,70,70,140,5,*,4,*,0,*,0,*', 13);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, npc_level, 
	class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, invincible, stationary, equipped_items, npc_type)
VALUES (149, 'Smithing Supplies', 1, 1, 71, 23, 0, 0.0, 50, 3, 70000, 0, 30, 0.20, 0, 0.0, '1', '1',
'10,*,1,70,70,70,140,5,*,4,*,0,*,0,*', 13);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, npc_level, 
	class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, invincible, stationary, equipped_items, npc_type)
VALUES (150, 'Scriber', 1, 1, 71, 23, 0, 0.0, 50, 3, 70000, 0, 30, 0.20, 0, 0.0, '1', '1',
'10,*,1,70,70,70,140,5,*,4,*,0,*,0,*', 13);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, 
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_percent_regen, 
	attack_range, attack_speed, npc_hp, npc_alliance, slowable, stunnable, rootable) 
VALUES (151, 'Richard', 144, 1, 0, 0, 4, 1.5, 50, 3, 12000, 12410, 40, 0.02, 2, 1.5, 623138, '151 152', '1', '0', '0');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, 
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_percent_regen, 
	attack_range, attack_speed, npc_hp, npc_alliance, slowable, stunnable, rootable, stationary) 
VALUES (152, 'Abomination', 158, 1, 0, 0, 3, 1.5, 50, 3, 60000, 34010, 14400, 0.015, 2, 1.2, 3823138, '151', '1', '0', '0', '1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, 
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_percent_regen, 
	attack_range, attack_speed, npc_hp, npc_alliance, slowable, stunnable, rootable) 
VALUES (153, 'Simon', 144, 1, 0, 0, 4, 1.5, 50, 3, 15000, 16010, 40, 0.02, 2, 1.5, 853138, '153 154', '1', '0', '0');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, 
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_percent_regen, 
	attack_range, attack_speed, npc_hp, npc_alliance, slowable, stunnable, rootable, stationary) 
VALUES (154, 'Charlie', 164, 1, 0, 0, 3, 1.5, 50, 3, 80000, 54010, 14400, 0.015, 2, 1.2, 5823138, '153', '1', '0', '0', '1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, 
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_percent_regen, 
	attack_range, attack_speed, npc_hp, npc_alliance, slowable, stunnable, rootable, stationary) 
VALUES (155, 'The Patriarch', 165, 1, 0, 0, 2, 1.5, 50, 3, 30000, 15410, 10800, 0.5, 2, 1.5, 1005138, '155', '1', '0', '0', '0');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, 
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_percent_regen, 
	attack_range, attack_speed, npc_hp, npc_alliance, slowable, stunnable, rootable, stationary, stuck_behaviour) 
VALUES (156, 'Mama Bear', 165, 1, 0, 0, 3, 1.5, 50, 3, 150000, 34410, 14400, 0.10, 5, 1.5, 4105138, '156', '1', '0', '0', '0', 1);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, 
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_percent_regen, 
	attack_range, attack_speed, npc_hp, npc_alliance, slowable, stunnable, rootable, stationary) 
VALUES (157, 'Gramps', 165, 1, 0, 0, 3, 1.5, 50, 3, 40000, 16400, 1800, 0.06, 3, 1.5, 1505138, '157 130 127 131 128', '1', '0', '0', '0');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, 
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_percent_regen, 
	attack_range, attack_speed, npc_hp, npc_alliance, slowable, stunnable, rootable, stationary) 
VALUES (158, 'Starved Bear', 165, 1, 0, 0, 3, 1.0, 50, 3, 20000, 20410, 600, 0.05, 2, 2.0, 1005138, '158 155', '1', '0', '0', '0');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, npc_level, 
	class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, invincible, stationary, equipped_items, npc_type)
VALUES (159, 'Wandering Scribe', 1, 1, 71, 23, 0, 2.0, 50, 3, 70000, 0, 30, 0.20, 0, 0.0, '1', '0',
'10,*,1,70,70,70,140,5,*,4,*,0,*,0,*', 13);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, npc_level, 
	class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, invincible, stationary, equipped_items, npc_type)
VALUES (160, 'Wandering Scribe', 1, 1, 71, 23, 0, 2.0, 50, 3, 70000, 0, 30, 0.20, 0, 0.0, '1', '0',
'10,*,1,70,70,70,140,5,*,4,*,0,*,0,*', 13);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, slowable,stunnable,rootable,stationary,npc_alliance, equipped_items, npc_hp, stuck_behaviour, stuck_timeout)
VALUES (161, 'Ancient Royal', 3, 1, 0, 0, 4, 1.5, 50, 3, 75000, 88000, 7200, 0.025, 4, 1.4, 
'1','0','0','1','161 162 163 164 165','0,*,0,*,0,*,0,*,0,*,0,*', 10143269, 1, 5);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, slowable,stunnable,stationary,npc_alliance, equipped_items, npc_hp, stuck_behaviour, stuck_timeout)
VALUES (162, 'King Terror', 3, 1, 0, 0, 4, 1.5, 50, 3, 200000, 120000, 28800, 0.03, 4, 1.8, 
'1','0','1','161 162 163 164 165','16,*,29,*,0,*,0,*,0,*,0,*', 30143269, 1, 5);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, slowable,stunnable,stationary,npc_alliance, equipped_items, npc_hp, stuck_behaviour, stuck_timeout)
VALUES (163, 'Prince Punisher', 3, 4, 0, 0, 4, 1.5, 50, 3, 150000, 120000, 28800, 0.03, 4, 1.5, 
'1','0','1','161 162 163 164 165','15,*,1,*,0,*,0,*,0,*,18,*', 25143269, 1, 5);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, slowable,stunnable,stationary,npc_alliance, equipped_items, npc_hp, stuck_behaviour, stuck_timeout)
VALUES (164, 'Queen Butcher', 3, 4, 0, 0, 3, 1.5, 50, 3, 100000, 120000, 28800, 0.02, 3, 1.1, 
'1','0','1','161 162 163 164 165','9,*,26,*,4,*,0,*,0,*,22,*', 30143269, 1, 5);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, slowable,stunnable,stationary,npc_alliance, equipped_items, npc_hp, stuck_behaviour, stuck_timeout)
VALUES (165, 'Princess Slayer', 3, 1, 0, 0, 3, 1.5, 50, 3, 100000, 120000, 28800, 0.02, 3, 1.5, 
'1','0','1','161 162 163 164 165','26,*,26,*,0,*,0,*,0,*,0,*', 20143269, 1, 5);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, 
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen, 
	attack_range, attack_speed, npc_alliance,stationary,slowable) 
VALUES (166, 'Pumpkin', 145, 1, 0, 0, 1, 1.5, 20, 4, 65, 489, 30, 0, 1, 1.5,'166 167','1','1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, 
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen, 
	attack_range, attack_speed, npc_alliance,stationary,slowable) 
VALUES (167, 'Fail Pumpkin', 147, 1, 0, 0, 1, 1.2, 25, 3, 90, 1000, 3600, 0, 1, 1.3,'166','1','1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, 
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen, 
	attack_range, attack_speed, npc_alliance,stationary,slowable) 
VALUES (168, 'Chief Pumpkin', 146, 1, 0, 0, 1, 1.5, 30, 4, 100, 600, 30, 0, 1, 1.5,'168 169','1','1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, 
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_static_regen, 
	attack_range, attack_speed, npc_alliance,stationary,slowable) 
VALUES (169, 'Boss Pumpkin', 147, 1, 0, 0, 1, 1.2, 35, 3, 140, 2000, 3600, 0, 1, 1.3,'168','1','1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, npc_level, 
	class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, invincible, stationary, equipped_items, npc_type)
VALUES (170, 'Phat Lewtz', 1, 3, 71, 23, 0, 0.0, 50, 3, 70000, 0, 30, 0.20, 0, 0.0, '1', '1',
'25,*,13,*,0,*,0,*,21,*,0,*', 13);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, npc_level, 
	class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, invincible, stationary, equipped_items, npc_type, npc_hp, npc_alliance)
VALUES (171, 'Team 1 Guard', 3, 1, 0, 0, 0, 0.0, 50, 3, 1000, 0, 7200, 0, 1, 1, '0', '1',
'0,*,2,255,0,0,180,0,*,0,*,0,*,0,*', 2, 300000, '172');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, npc_level, 
	class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, invincible, stationary, equipped_items, npc_type, npc_hp, npc_alliance)
VALUES (172, 'Team 1 Boss', 3, 1, 0, 0, 0, 0.0, 50, 3, 3000, 0, 7200, 0, 3, 1, '0', '1',
'14,255,0,0,180,2,255,0,0,180,0,*,0,*,0,*,0,*', 2, 900000, '171');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, npc_level, 
	class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, invincible, stationary, equipped_items, npc_type, npc_hp, npc_alliance)
VALUES (173, 'Team 2 Guard', 3, 1, 0, 0, 0, 0.0, 50, 3, 1000, 0, 7200, 0, 1, 1, '0', '1',
'0,*,2,0,0,255,180,0,*,0,*,0,*,0,*', 2, 300000, '174');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, npc_level, 
	class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, invincible, stationary, equipped_items, npc_type, npc_hp, npc_alliance)
VALUES (174, 'Team 2 Boss', 3, 1, 0, 0, 0, 0.0, 50, 3, 3000, 0, 7200, 0, 3, 1, '0', '1',
'14,0,0,255,180,2,0,0,255,180,0,*,0,*,0,*,0,*', 2, 900000, '173');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, slowable,stunnable,rootable,stationary,npc_alliance, equipped_items, npc_hp, stuck_behaviour, stuck_timeout)
VALUES (175, 'Ancient Prisoner', 128, 1, 0, 0, 4, 1.0, 50, 3, 2000000, 300000, 7200, 0.05, 4, 1.4, 
'1','0','0','1','175 176','0,*,0,*,0,*,0,*,0,*,0,*', 30143269, 1, 5);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, slowable,stunnable,rootable,stationary,npc_alliance, equipped_items, npc_hp, stuck_behaviour, stuck_timeout)
VALUES (176, 'Ancient Prison Superior', 125, 1, 0, 0, 4, 1.0, 50, 3, 3400000, 300000, 7200, 0.05, 4, 1.2, 
'1','0','0','0','176','0,*,0,*,0,*,0,*,0,*,0,*', 50143269, 1, 5);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, slowable,stunnable,rootable,stationary,npc_alliance, equipped_items, npc_hp, stuck_behaviour, stuck_timeout)
VALUES (177, 'Henry', 106, 1, 0, 0, 4, 1.0, 50, 3, 3600000, 300000, 20800, 0.05, 4, 1.2, 
'1','0','0','1','176','0,*,0,*,0,*,0,*,0,*,0,*', 70143269, 1, 5);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, slowable,stunnable,rootable,stationary,npc_alliance, equipped_items, npc_hp, stuck_behaviour, stuck_timeout)
VALUES (178, 'Gordon', 106, 1, 0, 0, 4, 1.0, 50, 3, 4000000, 300000, 20800, 0.05, 4, 1.2, 
'1','0','0','1','176','0,*,0,*,0,*,0,*,0,*,0,*', 100143269, 1, 5);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed,
	npc_level, class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, slowable,stunnable,rootable,stationary,npc_alliance, equipped_items, npc_hp, stuck_behaviour, stuck_timeout)
VALUES (179, 'Rueben', 106, 1, 0, 0, 4, 1.0, 50, 3, 3800000, 300000, 20800, 0.05, 4, 1.2, 
'1','0','0','1','176','0,*,0,*,0,*,0,*,0,*,0,*', 80143269, 1, 5);

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, npc_level, 
	class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, invincible, stationary, equipped_items, npc_type, credit_dealer)
VALUES (180, 'Credit Exchange', 1, 3, 71, 0, 4, 0.0, 50, 3, 70000, 0, 30, 0.20, 0, 0.0, '1', '1',
'21,130,60,150,100,21,130,60,150,100,2,170,80,100,100,2,170,80,100,100,0,*,45,*', 13, '1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, 
  npc_level, class_id, weapon_damage, experience, respawn_time, hp_percent_regen, attack_range, attack_speed, 
  slowable, equipped_items, rootable, stunnable, npc_hp, stationary, npc_type, invincible)
VALUES (181, 'Pet Trainer', 11, 4, 71, 54, 3, 0.00, 50, 3, 70000, 0, 30, 0.20, 0, 0.0,'1',
'8,*,0,*,0,*,0,*,0,*,0,*','0', '0', 316298, '0', 13, '1');

INSERT INTO npc_templates (npc_id, npc_name, body_id, body_state, face_id, hair_id, aggro_range, move_speed, npc_level, 
	class_id, weapon_damage, experience, respawn_time, hp_percent_regen,
	attack_range, attack_speed, invincible, stationary, equipped_items, npc_type)
VALUES (182, 'Alchemy Supplier Guy', 1, 1, 71, 23, 0, 0.0, 50, 3, 70000, 0, 30, 0.20, 0, 0.0, '1', '1',
'23,*,0,*,0,*,0,*,0,*,0,*', 13);

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
/* Mice */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (1, 1, 80, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (1, 1, 85, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (1, 1, 90, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (1, 1, 80, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (1, 1, 85, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (1, 1, 90, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (1, 1, 80, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (1, 1, 85, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (1, 1, 90, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (1, 1, 80, 53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (1, 1, 85, 53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (1, 1, 90, 53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (1, 1, 80, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (1, 1, 85, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (1, 1, 90, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (1, 1, 80, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (1, 1, 85, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (1, 1, 90, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (1, 1, 80, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (1, 1, 85, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (1, 1, 90, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (1, 1, 80, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (1, 1, 85, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (1, 1, 90, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (1, 1, 80, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (1, 1, 85, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (1, 1, 90, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (1, 1, 90, 100);

/* Lambs */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (2, 1, 75, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (2, 1, 78, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (2, 1, 81, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (2, 1, 84, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (2, 1, 87, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (2, 1, 90, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (2, 1, 77, 13);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (2, 1, 80, 13);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (2, 1, 85, 13);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (2, 1, 90, 13);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (2, 1, 75, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (2, 1, 78, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (2, 1, 81, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (2, 1, 84, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (2, 1, 87, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (2, 1, 90, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (2, 1, 77, 19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (2, 1, 80, 19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (2, 1, 85, 19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (2, 1, 90, 19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (2, 1, 75, 22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (2, 1, 78, 22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (2, 1, 81, 22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (2, 1, 84, 22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (2, 1, 87, 22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (2, 1, 90, 22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (2, 1, 91, 22);

/* Fluffy Little Bunny */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (3, 16, 43, 93);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (3, 16, 37, 97);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (3, 16, 32, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (3, 16, 24, 98);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (3, 16, 19, 89);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (3, 16, 12, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (3, 16, 23, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (3, 16, 20, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (3, 16, 29, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (3, 16, 45, 48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (3, 16, 53, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (3, 16, 47, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (3, 16, 41, 71);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (3, 16, 38, 79);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (3, 16, 56, 91);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (3, 16, 62, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (3, 16, 69, 93);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (3, 16, 76, 97);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (3, 16, 78, 89);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (3, 16, 95, 91);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (3, 16, 74, 81);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (3, 16, 67, 82);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (3, 16, 62, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (3, 16, 55, 82);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (3, 16, 77, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (3, 16, 86, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (3, 16, 93, 79);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (3, 16, 98, 71);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (3, 16, 86, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (3, 16, 80, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (3, 16, 93, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (3, 16, 48, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (3, 16, 50, 85);

/* Rabid Rabbit */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (4, 16, 8, 89);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (4, 16, 8, 89);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (4, 16, 10, 93);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (4, 16, 92, 89);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (4, 16, 96, 94);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (4, 16, 91, 93);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (4, 16, 48, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (4, 16, 43, 52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (4, 16, 36, 54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (4, 16, 38, 58);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (4, 16, 37, 62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (4, 16, 44, 61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (4, 16, 62, 54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (4, 16, 66, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (4, 16, 69, 54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (4, 16, 61, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (4, 16, 70, 61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (4, 16, 4, 48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (4, 16, 2, 52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (4, 16, 7, 53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (4, 16, 5, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (4, 16, 8, 46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (4, 16, 4, 41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (4, 16, 32, 46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (4, 16, 38, 41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (4, 16, 47, 46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (4, 16, 43, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (4, 16, 31, 36);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (4, 16, 23, 32);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (4, 16, 13, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (4, 16, 19, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (4, 16, 16, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (4, 16, 10, 13);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (4, 16, 6, 6);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (4, 16, 25, 8);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (4, 16, 34, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (4, 16, 40, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (4, 16, 43, 13);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (4, 16, 39, 22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (4, 16, 95, 47);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (4, 16, 92, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (4, 16, 83, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (4, 16, 72, 44);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (4, 16, 68, 48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (4, 16, 62, 41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (4, 16, 54, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (4, 16, 65, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (4, 16, 59, 27);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (4, 16, 71, 29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (4, 16, 83, 27);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (4, 16, 95, 29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (4, 16, 92, 18);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (4, 16, 80, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (4, 16, 73, 19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (4, 16, 63, 22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (4, 16, 54, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (4, 16, 85, 8);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (4, 16, 75, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (4, 16, 82, 2);

/* Cottontail */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (5, 16, 7, 61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (5, 16, 7, 91);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (5, 16, 94, 93);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (5, 16, 44, 58);

/* Asp */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (6, 8, 93, 22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (6, 8, 86, 27);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (6, 8, 89, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (6, 8, 84, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (6, 8, 93, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (6, 8, 98, 47);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (6, 8, 86, 47);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (6, 8, 73, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (6, 8, 72, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (6, 8, 82, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (6, 8, 89, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (6, 8, 93, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (6, 8, 96, 74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (6, 8, 91, 81);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (6, 8, 90, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (6, 8, 84, 86);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (6, 8, 81, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (6, 8, 84, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (6, 8, 79, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (6, 8, 66, 51);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (6, 8, 83, 23);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (6, 8, 76, 18);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (6, 8, 69, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (6, 8, 62, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (6, 8, 56, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (6, 8, 67, 11);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (6, 8, 77, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (6, 8, 90, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (6, 8, 96, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (6, 8, 95, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (6, 8, 82, 6);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (6, 8, 55, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (6, 8, 51, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (6, 8, 55, 27);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (6, 8, 56, 37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (6, 8, 44, 23);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (6, 8, 35, 18);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (6, 8, 31, 9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (6, 8, 39, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (6, 8, 53, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (6, 8, 81, 27);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (6, 8, 92, 29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (6, 8, 56, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (6, 8, 48, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (6, 8, 44, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (6, 8, 44, 66);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (6, 8, 56, 69);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (6, 8, 64, 78);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (6, 8, 65, 89);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (6, 8, 53, 93);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (6, 8, 40, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (6, 8, 31, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (6, 8, 31, 77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (6, 8, 15, 81);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (6, 8, 14, 64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (6, 8, 7, 58);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (6, 8, 9, 34);

/* Bat */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (7, 2, 12, 13);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (7, 2, 16, 11);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (7, 2, 9, 19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (7, 2, 15, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (7, 2, 17, 38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (7, 2, 9, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (7, 2, 7, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (7, 2, 16, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (7, 2, 10, 54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (7, 2, 17, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (7, 2, 22, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (7, 2, 23, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (7, 2, 27, 32);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (7, 2, 29, 28);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (7, 2, 26, 23);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (7, 2, 23, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (7, 2, 23, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (7, 2, 31, 48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (7, 2, 31, 41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (7, 2, 35, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (7, 2, 41, 41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (7, 2, 47, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (7, 2, 50, 44);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (7, 2, 48, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (7, 2, 42, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (7, 2, 39, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (7, 2, 41, 23);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (7, 2, 47, 19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (7, 2, 42, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (7, 2, 37, 19);

/* Cow */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (8, 14, 41, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (8, 14, 31, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (8, 14, 24, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (8, 14, 16, 9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (8, 14, 19, 3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (8, 14, 9, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (8, 14, 2, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (8, 14, 6, 13);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (8, 14, 3, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (8, 14, 7, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (8, 14, 7, 26);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (8, 14, 16, 27);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (8, 14, 18, 23);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (8, 14, 24, 19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (8, 14, 30, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (8, 14, 35, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (8, 14, 33, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (8, 14, 27, 9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (8, 14, 25, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (8, 14, 42, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (8, 14, 46, 22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (8, 14, 53, 24);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (8, 14, 54, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (8, 14, 61, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (8, 14, 59, 11);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (8, 14, 57, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (8, 14, 62, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (8, 14, 66, 2);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (8, 14, 65, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (8, 14, 67, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (8, 14, 68, 28);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (8, 14, 78, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (8, 14, 75, 37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (8, 14, 66, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (8, 14, 57, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (8, 14, 55, 45);

/* Ram */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (9, 14, 60, 48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (9, 14, 62, 54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (9, 14, 58, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (9, 14, 60, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (9, 14, 59, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (9, 14, 58, 72);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (9, 14, 58, 77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (9, 14, 62, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (9, 14, 69, 78);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (9, 14, 74, 79);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (9, 14, 77, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (9, 14, 82, 79);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (9, 14, 77, 73);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (9, 14, 72, 74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (9, 14, 70, 69);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (9, 14, 68, 66);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (9, 14, 66, 62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (9, 14, 70, 61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (9, 14, 71, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (9, 14, 68, 58);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (9, 14, 68, 53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (9, 14, 75, 53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (9, 14, 76, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (9, 14, 81, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (9, 14, 84, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (9, 14, 85, 53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (9, 14, 86, 63);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (9, 14, 81, 64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (9, 14, 84, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (9, 14, 75, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (9, 14, 74, 62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (9, 14, 72, 63);

/* Weak Zombie */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (10, 3, 44, 93);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (10, 3, 56, 92);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (10, 3, 63, 94);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (10, 3, 66, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (10, 3, 61, 84);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (10, 3, 51, 79);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (10, 3, 54, 71);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (10, 3, 54, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (10, 3, 52, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (10, 3, 38, 86);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (10, 3, 35, 92);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (10, 3, 25, 92);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (10, 3, 16, 89);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (10, 3, 9, 94);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (10, 3, 6, 91);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (10, 3, 12, 78);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (10, 3, 19, 74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (10, 3, 7, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (10, 3, 9, 46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (10, 3, 9, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (10, 3, 16, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (10, 3, 23, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (10, 3, 33, 63);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (10, 3, 70, 63);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (10, 3, 81, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (10, 3, 89, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (10, 3, 92, 52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (10, 3, 86, 46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (10, 3, 78, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (10, 3, 69, 52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (10, 3, 62, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (10, 3, 49, 39);

/* Weak Skeleton */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 86,95);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 82,95);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 7,84);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 12,81);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 11,74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 8,76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 6,71);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 6,69);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 9,70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 9,68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 9,64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 6,60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 5,61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 5,92);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 11,93);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 35,64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 36,60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 38,60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 37,62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 40,64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 46,64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 39,69);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 39,85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 39,84);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 43,87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 43,82);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 41,78);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 43,73);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 44,74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 47,77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 56,90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 56,94);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 55,86);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 56,80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 48,81);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 50,77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 49,72);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 55,74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 52,68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 57,61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 58,60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 61,60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 61,62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 48,60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 48,59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 48,57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 47,46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 46,41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 49,40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 24,12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 21,13);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 21,4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 17,6);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 15,9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 12,9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 14,14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 8,12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 6,9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 7,5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 2,5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 3,6);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 6,14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 4,16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 3,16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 4,21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 3,28);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 3,29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 3,31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 3,36);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 5,38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 6,42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 7,42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 38,38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (11, 6, 39,31);

/* (Wandering) Weak Skeleton */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (12, 6, 33,60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (12, 6, 39,66);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (12, 6, 48,64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (12, 6, 55,82);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (12, 6, 45,78);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (12, 6, 2,93);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (12, 6, 13,96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (12, 6, 8,74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (12, 6, 6,65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (12, 6, 13,61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (12, 6, 43,43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (12, 6, 39,41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (12, 6, 20,43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (12, 6, 36,28);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (12, 6, 33,8);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (12, 6, 9,7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (12, 6, 3,11);

/* Skeleton */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (13, 6, 37,25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (13, 6, 38,24);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (13, 6, 39,23);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (13, 6, 62,22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (13, 6, 63,22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (13, 6, 65,24);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (13, 6, 63,15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (13, 6, 68,14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (13, 6, 71,14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (13, 6, 62,10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (13, 6, 65,6);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (13, 6, 61,4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (13, 6, 86,6);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (13, 6, 84,4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (13, 6, 84,14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (13, 6, 86,14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (13, 6, 84,27);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (13, 6, 87,48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (13, 6, 87,49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (13, 6, 83,64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (13, 6, 84,65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (13, 6, 85,65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (13, 6, 86,65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (13, 6, 87,64);


/* Servant of the Dead */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (14, 6, 5,96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (14, 6, 12,96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (14, 6, 12,89);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (14, 6, 10,87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (14, 6, 7,87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (14, 6, 5,89);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (14, 6, 25,5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (14, 6, 25,9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (14, 6, 31,9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (14, 6, 31,5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (14, 6, 76,76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (14, 6, 75,76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (14, 6, 75,74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (14, 6, 77,74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (14, 6, 77,69);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (14, 6, 76,69);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (14, 6, 75,67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (14, 6, 75,66);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (14, 6, 76,66);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (14, 6, 76,65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (14, 6, 79,65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (14, 6, 83,71);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (14, 6, 84,71);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (14, 6, 82,70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (14, 6, 82,77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (14, 6, 83,76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (14, 6, 84,76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (14, 6, 92,67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (14, 6, 92,68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (14, 6, 91,69);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (14, 6, 91,71);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (14, 6, 90,71);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (14, 6, 90,73);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (14, 6, 89,73);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (14, 6, 89,75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (14, 6, 90,76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (14, 6, 90,75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (14, 6, 91,74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (14, 6, 92,73);

/* Boney -Boss- */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (15, 6, 28,7);

/* Misplaced Spouse -Boss- */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (16, 6, 9,96);

/* Record Keeper -Boss- */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (17, 6, 90,74);

/* Piglet spawns */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (18, 25, 17, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (18, 25, 23, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (18, 25, 29, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (18, 25, 35, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (18, 25, 42, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (18, 25, 49, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (18, 25, 55, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (18, 25, 60, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (18, 25, 70, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (18, 25, 85, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (18, 25, 90, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (18, 25, 23, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (18, 25, 29, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (18, 25, 35, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (18, 25, 42, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (18, 25, 49, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (18, 25, 55, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (18, 25, 60, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (18, 25, 70, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (18, 25, 85, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (18, 25, 90, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (18, 25, 23, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (18, 25, 29, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (18, 25, 35, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (18, 25, 42, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (18, 25, 49, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (18, 25, 55, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (18, 25, 60, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (18, 25, 70, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (18, 25, 85, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (18, 25, 90, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (18, 25, 23, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (18, 25, 29, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (18, 25, 35, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (18, 25, 42, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (18, 25, 49, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (18, 25, 55, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (18, 25, 60, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (18, 25, 70, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (18, 25, 85, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (18, 25, 90, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (18, 25, 90, 90);

/* Flying Piglet Spawns */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (19, 25, 28, 84);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (19, 25, 8, 62);

/* Lost Wabbit */

INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 6,43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 10,39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 11,39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 11,44);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 11,46);
/* INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 20,46); removed */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 20,42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 20,36);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 15,35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 15,38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 15,41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 15,42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 13,29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 11,30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 11,31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 11,35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 5,22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 4,21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 2,17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 2,12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 2,11);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 2,5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 2,4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 2,3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 10,19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 13,19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 14,19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 17,22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 17,23);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 46,42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 49,42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 98,35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 10,14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 19,14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 21,15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 21,22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 21,28);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 20,28);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 22,38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 22,39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 34,43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 34,42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 34,33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 34,31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 34,20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 33,16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 27,16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 24,17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 24,18);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 24,33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 29,44);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 27,22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 28,35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 45,34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 43,10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 50,30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 50,32);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 46,49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 76,49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 73,42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 6,50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 6,56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 6,57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 12,58);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 14,51);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 14,52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 22,55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 27,55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 34,58);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 37,58);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 37,61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 38,61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 36,59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 94,61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 97,61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 78,52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 43,54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 47,54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 48,54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 53,56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 64,60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 65,56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 75,56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 85,56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 90,56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 30,59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 30,60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 26,60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 18,60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 19,59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 35,65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 41,65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 51,65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 25,48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 28,48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 31,51);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 19,53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 33,53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 34,53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 40,53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 43,50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 43,52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 44,52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 45,50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 48,50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 54,52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 53,52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 52,52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 52,48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 70,52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 85,52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 62,61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 60,59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 59,59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (20, 19, 55,59);

/* Cute Defenseless Stalker */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 34,32);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 32,30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 32,36);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 32,37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 30,44);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 24,44);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 26,23);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 26,24);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 28,34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 28,36);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 28,37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 37,40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 45,33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 45,19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 47,10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 48,10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 46,38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 42,46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 51,46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 52,46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 59,46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 65,49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 77,49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 48,42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 47,42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 72,42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 75,42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 81,37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 81,35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 81,44);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 84,45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 99,49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 93,42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 89,42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 91,44);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 91,40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 90,39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 84,40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 86,47);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 37,60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 45,63);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 61,56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 64,56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 70,56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 91,56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 92,56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 93,56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 25,60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 23,59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 34,65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 36,65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 52,65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 68,65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 66,60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 66,59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 73,61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 80,61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 81,61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 82,61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 95,61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 96,61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 98,56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 98,55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 98,54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 98,53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 61,52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 62,52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 77,52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 84,52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 86,52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 92,52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (21, 19, 93,52);

/* Roger */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (22, 19, 36,60);

/* Leafeater */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (23, 19, 91,42);

/* Pipsqueek spawns */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 7, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 12, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 14, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 17, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 20, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 25, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 40, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 45, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 48, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 51, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 57, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 75, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 80, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 85, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 90, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 12, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 14, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 17, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 20, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 25, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 40, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 45, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 48, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 51, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 57, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 75, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 80, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 85, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 90, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 12, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 14, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 17, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 20, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 25, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 40, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 45, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 48, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 51, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 57, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 75, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 80, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 85, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 90, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 12, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 14, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 17, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 20, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 25, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 40, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 45, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 48, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 51, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 57, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 75, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 80, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 85, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 90, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 12, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 14, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 17, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 20, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 25, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 40, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 45, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 48, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 51, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 57, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 75, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 80, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 85, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 90, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 12, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 14, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 17, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 20, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 25, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 40, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 45, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 48, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 51, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 57, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 75, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 80, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 85, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 90, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (24, 25, 90, 47);

/* Flying Pipsqueek spawns */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (25, 25, 16, 9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (25, 25, 90, 20);

/* Fire Asp */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (26, 10, 3, 3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (26, 10, 3, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (26, 10, 3, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (26, 10, 17, 3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (26, 10, 17, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (26, 10, 17, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (26, 10, 31, 3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (26, 10, 31, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (26, 10, 31, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (26, 10, 45, 3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (26, 10, 45, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (26, 10, 45, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (26, 10, 59, 3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (26, 10, 59, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (26, 10, 59, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (26, 10, 73, 3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (26, 10, 73, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (26, 10, 73, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (26, 10, 87, 3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (26, 10, 87, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (26, 10, 87, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (26, 10, 97, 3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (26, 10, 97, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (26, 10, 97, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (26, 10, 65, 1);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (26, 10, 79, 2);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (26, 10, 93, 3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (26, 10, 65, 13);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (26, 10, 79, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (26, 10, 93, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (26, 10, 65, 27);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (26, 10, 79, 28);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (26, 10, 93, 29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (26, 10, 65, 41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (26, 10, 79, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (26, 10, 93, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (26, 10, 65, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (26, 10, 79, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (26, 10, 93, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (26, 10, 65, 69);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (26, 10, 79, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (26, 10, 93, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (26, 10, 65, 83);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (26, 10, 79, 82);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (26, 10, 93, 84);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (26, 10, 65, 97);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (26, 10, 79, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (26, 10, 93, 98);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (26, 10, 39, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (26, 10, 48, 37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (26, 10, 51, 44);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (26, 10, 43, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (26, 10, 62, 47);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (26, 10, 61, 33);

/* Naga Warrior/Rogue */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (27, 10, 25, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (28, 10, 25, 28);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (27, 10, 17, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (28, 10, 8, 28);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (27, 10, 5, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (28, 10, 4, 41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (27, 10, 15, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (28, 10, 24, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (27, 10, 29, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (28, 10, 33, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (27, 10, 39, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (28, 10, 48, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (27, 10, 54, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (28, 10, 55, 66);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (27, 10, 56, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (28, 10, 59, 82);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (27, 10, 55, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (28, 10, 60, 95);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (27, 10, 48, 92);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (28, 10, 39, 98);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (27, 10, 36, 95);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (28, 10, 28, 93);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (27, 10, 16, 89);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (28, 10, 9, 86);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (27, 10, 6, 81);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (28, 10, 10, 71);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (27, 10, 20, 69);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (28, 10, 22, 77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (27, 10, 29, 82);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (28, 10, 37, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (28, 10, 42, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (28, 10, 48, 62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (28, 10, 24, 58);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (28, 10, 16, 65);

/* Nagan Sentry */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (109, 10, 20, 94);

/* Frozen Waste -Non Moving- */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (29, 18, 83,12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (29, 18, 81,12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (29, 18, 83,31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (29, 18, 79,31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (29, 18, 83,28);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (29, 18, 79,28);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (29, 18, 83,25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (29, 18, 79,25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (29, 18, 79,22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (29, 18, 83,22);

/* Frozen Waste -Moving- */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (30, 18, 66,3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (30, 18, 40,3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (30, 18, 46,3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (30, 18, 50,3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (30, 18, 57,3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (30, 18, 70,3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (30, 18, 90,3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (30, 18, 95,3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (30, 18, 40,9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (30, 18, 46,9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (30, 18, 50,9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (30, 18, 57,9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (30, 18, 70,9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (30, 18, 90,9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (30, 18, 95,9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (30, 18, 40,15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (30, 18, 46,15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (30, 18, 50,15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (30, 18, 57,15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (30, 18, 70,15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (30, 18, 90,15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (30, 18, 95,15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (30, 18, 40,21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (30, 18, 46,21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (30, 18, 50,21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (30, 18, 57,21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (30, 18, 70,21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (30, 18, 90,21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (30, 18, 95,21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (30, 18, 40,29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (30, 18, 46,29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (30, 18, 50,29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (30, 18, 57,29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (30, 18, 70,29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (30, 18, 90,29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (30, 18, 95,29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (30, 18, 98,32);

/* Iceman -Non Moving- */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (31, 18, 80,34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (31, 18, 82,34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (31, 18, 82,13);

/* Iceman -Moving- */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (32, 18, 66,35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (32, 18, 73,35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (32, 18, 79,35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (32, 18, 87,35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (32, 18, 95,35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (32, 18, 73,40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (32, 18, 79,40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (32, 18, 87,40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (32, 18, 95,40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (32, 18, 73,45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (32, 18, 79,45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (32, 18, 87,45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (32, 18, 95,45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (32, 18, 73,55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (32, 18, 79,55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (32, 18, 87,55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (32, 18, 95,55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (32, 18, 73,60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (32, 18, 79,60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (32, 18, 87,60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (32, 18, 95,60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (32, 18, 98,65);

/* Frosty -Boss- */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (33, 18, 82,11);



/* Sergeant -Boss- */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (37, 11, 6,85)
/* Private Persecution */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (36, 11, 10,99)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (36, 11, 10,97)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (36, 11, 11,99)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (36, 11, 11,98)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (36, 11, 16,93)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (36, 11, 18,93)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (36, 11, 17,91)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (36, 11, 18,90)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (36, 11, 7,92)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (36, 11, 6,91)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (36, 11, 4,90)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (36, 11, 7,87)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (36, 11, 9,87)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (36, 11, 12,87)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (36, 11, 5,86)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (36, 11, 5,85)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (36, 11, 8,85)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (36, 11, 10,85)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (36, 11, 9,83)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (36, 11, 6,83)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (36, 11, 7,81)
/* Strong Persecution - HnF ent */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (38, 11, 80,2);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (38, 11, 80,4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (38, 11, 80,6);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (38, 11, 85,2);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (38, 11, 85,4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (38, 11, 85,6);
/* Moldy Persecution */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (35, 11, 60, 6);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (35, 11, 64, 6);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (35, 11, 68, 6);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (35, 11, 75, 6);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (35, 11, 79, 6);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (35, 11, 60, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (35, 11, 64, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (35, 11, 68, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (35, 11, 75, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (35, 11, 79, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (35, 11, 60, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (35, 11, 64, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (35, 11, 68, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (35, 11, 75, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (35, 11, 79, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (35, 11, 60, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (35, 11, 64, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (35, 11, 68, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (35, 11, 75, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (35, 11, 79, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (35, 11, 80, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (35, 11, 58, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (35, 11, 65, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (35, 11, 68, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (35, 11, 72, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (35, 11, 76, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (35, 11, 58, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (35, 11, 65, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (35, 11, 68, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (35, 11, 72, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (35, 11, 76, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (35, 11, 58, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (35, 11, 65, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (35, 11, 68, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (35, 11, 72, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (35, 11, 76, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (35, 11, 58, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (35, 11, 65, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (35, 11, 68, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (35, 11, 72, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (35, 11, 76, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (35, 11, 78, 51);

/* Weak Persecution */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (34, 11, 56, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (34, 11, 59, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (34, 11, 64, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (34, 11, 69, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (34, 11, 75, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (34, 11, 56, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (34, 11, 59, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (34, 11, 64, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (34, 11, 69, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (34, 11, 75, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (34, 11, 56, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (34, 11, 59, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (34, 11, 64, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (34, 11, 69, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (34, 11, 75, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (34, 11, 56, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (34, 11, 59, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (34, 11, 64, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (34, 11, 69, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (34, 11, 75, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (34, 11, 56, 94);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (34, 11, 59, 94);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (34, 11, 64, 94);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (34, 11, 69, 94);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (34, 11, 75, 94);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (34, 11, 77, 95);

/* Strong Persecution - Mindless Mines */
/* and */
/* Persecution */
/* mixed =[ */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 20, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 25, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 30, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 35, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 40, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 45, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 50, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 55, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 60, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 65, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 80, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 85, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 90, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 92, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 97, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 20, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 25, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 30, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 35, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 40, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 45, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 50, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 55, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 60, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 65, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 80, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 85, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 90, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 92, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 97, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 20, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 25, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 30, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 35, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 40, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 45, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 50, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 55, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 60, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 65, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 80, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 85, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 90, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 92, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 97, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 20, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 25, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 30, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 35, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 40, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 45, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 50, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 55, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 60, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 65, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 80, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 85, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 90, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 92, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 97, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 20, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 25, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 30, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 35, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 40, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 45, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 50, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 55, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 60, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 65, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 80, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 85, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 90, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 92, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 97, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 20, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 25, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 30, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 35, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 40, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 45, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 50, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 55, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 60, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 65, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 80, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 85, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 90, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 92, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 97, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 20, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 25, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 30, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 35, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 40, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 45, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 50, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 55, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 60, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 65, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 80, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 85, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 90, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 92, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 97, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 20, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 25, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 30, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 35, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 40, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 45, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 50, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 55, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 60, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 65, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 80, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 85, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 90, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 92, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 97, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 99, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 2, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 10, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 20, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 30, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 40, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 50, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 60, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 65, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 2, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 10, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 20, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 30, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 40, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 50, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 60, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 65, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 2, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 10, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 20, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 30, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 40, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 50, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 60, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 65, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 2, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 10, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 20, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 30, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 40, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 50, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 60, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (49, 12, 65, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (39, 12, 67, 98);

/* Nagan Beast */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 33, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 42, 9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 57, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 63, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 73, 8);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 79, 3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 88, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 93, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 83, 11);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 86, 18);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 96, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 92, 24);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 81, 24);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 87, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 95, 32);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 95, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 87, 37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 81, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 95, 46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 86, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 95, 52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 86, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 95, 58);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 98, 63);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 90, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 94, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 88, 82);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 97, 86);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 85, 93);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 79, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 72, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 64, 84);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 64, 78);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 64, 72);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 81, 66);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 71, 63);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 66, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 76, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 74, 48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 64, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 67, 41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 76, 37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 66, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 72, 29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 66, 22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 64, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 73, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 60, 62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 50, 91);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 55, 84);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 49, 77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 54, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 49, 61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 55, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 54, 47);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 47, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 55, 32);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 54, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 55, 18);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 41, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 44, 23);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 41, 32);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 33, 37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 37, 44);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 45, 52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 35, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 43, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 34, 63);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 31, 71);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 25, 66);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 26, 58);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 26, 79);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 38, 78);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 25, 51);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 27, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 24, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 31, 29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 35, 22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 26, 19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 26, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 17, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 20, 28);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 18, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 18, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 17, 54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 17, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 15, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 17, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 15, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 7, 79);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 7, 72);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 7, 66);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 7, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 10, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 10, 36);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 10, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 10, 24);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 10, 18);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (50, 27, 10, 12);

/* Nagan Magus */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 34, 8);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 43, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 58, 11);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 64, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 74, 9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 80, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 89, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 94, 11);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 84, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 87, 19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 97, 18);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 93, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 82, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 88, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 96, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 96, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 88, 38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 82, 44);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 96, 47);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 87, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 96, 53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 87, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 96, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 99, 64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 91, 71);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 95, 77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 89, 83);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 98, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 86, 94);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 80, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 73, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 65, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 65, 79);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 65, 73);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 82, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 72, 64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 67, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 77, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 75, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 65, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 68, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 77, 38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 67, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 73, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 67, 23);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 65, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 74, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 61, 63);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 51, 92);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 56, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 50, 78);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 55, 69);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 50, 62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 56, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 55, 48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 48, 41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 56, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 55, 26);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 56, 19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 42, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 45, 24);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 42, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 34, 38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 38, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 46, 53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 36, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 44, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 35, 64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 32, 72);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 26, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 27, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 27, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 39, 79);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 26, 52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 28, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 25, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 32, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 36, 23);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 27, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 27, 13);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 18, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 21, 29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 19, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 19, 46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 18, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 18, 61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 16, 69);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 18, 77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 16, 86);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 8, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 8, 73);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 8, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 8, 61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 11, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 11, 37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 11, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 11, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 11, 19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (51, 27, 11, 13);

/* Nagan Priest */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 32, 8);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 41, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 56, 11);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 62, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 72, 9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 78, 4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 87, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 92, 11);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 82, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 85, 19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 95, 18);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 91, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 80, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 86, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 94, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 94, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 86, 38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 80, 44);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 94, 47);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 85, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 94, 53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 85, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 94, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 97, 64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 89, 71);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 93, 77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 87, 83);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 96, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 84, 94);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 78, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 71, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 63, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 63, 79);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 63, 73);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 80, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 70, 64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 65, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 75, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 73, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 63, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 66, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 75, 38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 65, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 71, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 65, 23);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 63, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 72, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 59, 63);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 49, 92);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 54, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 48, 78);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 53, 69);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 48, 62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 54, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 53, 48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 46, 41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 54, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 53, 26);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 54, 19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 40, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 43, 24);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 40, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 32, 38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 36, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 44, 53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 34, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 42, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 33, 64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 30, 72);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 24, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 25, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 25, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 37, 79);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 24, 52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 26, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 23, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 30, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 34, 23);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 25, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 25, 13);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 16, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 19, 29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 17, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 17, 46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 16, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 16, 61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 14, 69);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 16, 77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 14, 86);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 6, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 6, 73);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 6, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 6, 61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 9, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 9, 37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 9, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 9, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 9, 19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (52, 27, 9, 13);

/* Udyana */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (53, 27, 76, 83);

/* Complex */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (54, 12, 84,85);

/* Simple */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (55, 12, 85,86);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (55, 12, 86,87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (55, 12, 85,84);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (55, 12, 86,83);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (55, 12, 89,86);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (55, 12, 89,84);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (55, 12, 92,83);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (55, 12, 94,85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (55, 12, 95,82);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (55, 12, 99,82);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (55, 12, 98,83);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (55, 12, 98,86);

/* Spook */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 34,3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 35,8);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 36,10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 38,8);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 37,6);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 41,7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 47,10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 47,8);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 44,8);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 45,6);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 48,3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 13,5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 15,5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 17,10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 17,11);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 2,14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 3,14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 8,14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 9,13);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 10,13);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 11,13);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 12,14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 3,19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 4,20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 5,20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 6,20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 14,19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 17,19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 6,25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 12,24);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 13,24);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 11,29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 13,29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 14,29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 19,25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 20,25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 22,27);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 22,28);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 29,25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 30,25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 30,27);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 28,32);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 30,32);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 29,33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 28,38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 30,38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 29,39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 24,43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 26,54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 29,44);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 5,41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 6,41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 11,41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 5,45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 6,45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 16,46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 17,47);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 5,47);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 9,49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 11,49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 16,50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 4,59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 16,59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 8,61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 12,61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 3,74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 10,74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 4,75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 9,75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 11,75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 16,75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 17,75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 17,78);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 6,81);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 12,82);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 16,84);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 16,89);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 14,90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 5,89);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 4,90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 3,91);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 15,91);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 16,92);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 4,92);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 8,96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 9,97);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 22,93);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 23,95);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (56, 9, 21,96);

/* Ghast */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 24,97);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 25,94);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 33,92);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 34,91);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 37,95);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 40,97);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 28,88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 29,87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 30,85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 29,86);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 36,85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 37,85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 39,85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 32,79);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 37,79);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 40,74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 34,76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 35,76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 31,69);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 31,68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 31,67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 39,68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 38,65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 35,62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 33,59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 43,59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 44,59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 48,67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 50,67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 52,66);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 54,65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 54,64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 52,59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 53,59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 55,59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 58,72);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 61,75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 64,78);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 63,67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 66,67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 67,71);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 71,75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 81,82);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 82,80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 76,87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 77,88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 83,87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 90,87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 91,87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 92,88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 97,88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 85,98);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 91,98);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 99,98);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 95,96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 93,92);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 89,94);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 94,81);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 98,81);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 98,77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 94,77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 94,73);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 98,73);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 98,69);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 94,69);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 95,63);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 88,63);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 85,63);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 84,60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 85,57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 91,54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 97,54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 79,51);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 78,50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 79,49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 85,50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 91,49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 92,49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 97,49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 98,49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 69,46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 66,44);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 63,43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 74,42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 71,10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 69,37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 77,33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 78,32);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 94,43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 96,43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 95,42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 82,39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 83,39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 83,40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 97,36);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 98,34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 94,34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 88,34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 89,36);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 61,36);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 61,35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 61,33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 61,32);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 61,30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 61,29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 63,22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 97,22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 98,22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 98,28);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 97,28);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 91,28);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 86,28);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 92,24);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 87,24);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 81,24);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 68,28);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 72,28);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 71,29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 80,7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 84,7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 77,9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 87,9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 75,12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 89,12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 77,15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 87,15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 84,17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (57, 9, 80,17);

/* ImaBitStale */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (58, 9, 41,5);

/* Ecto */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (59, 9, 92,93);

/* Punchy */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (60, 9, 82,12);

/* One With Head Thingy	*/
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 91,91);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 91,89);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 91,87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 96,87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 96,89);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 96,91);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 92,83);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 95,83);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 95,80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 92,80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 93,76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 92,75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 94,75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 85,77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 82,76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 81,83);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 81,85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 85,87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 71,86);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 72,86);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 72,85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 67,78);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 66,77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 67,76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 58,79);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 57,80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 59,80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 50,87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 50,89);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 49,88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 31,89);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 38,89);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 31,87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 38,87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 18,89);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 18,87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 16,88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 9,89);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 7,88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 5,87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 4,85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 9,84);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 13,80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 15,80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 21,82);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 5,79);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 7,79);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 6,78);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 13,75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 13,73);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 12,74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 23,72);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 25,74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 25,75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 30,77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 28,80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 36,81);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 37,82);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 43,82);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 43,83);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 45,78);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 44,77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 47,74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 49,74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 50,74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 45,68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 47,68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 46,67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 48,59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 47,58);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 54,59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 51,54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 59,60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 61,59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 67,63);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 64,63);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 60,51);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 56,51);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 56,47);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 60,47);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 61,41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 62,42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 62,40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 70,43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 72,43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 81,43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 89,43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 97,43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 71,42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 80,42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 90,42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 97,42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 82,44);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 90,44);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 96,45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 95,69);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 96,68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 97,67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 95,62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 98,62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 97,55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 98,56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 96,50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 97,50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 95,37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 98,37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 96,32);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 95,31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 97,31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 94,26);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 99,26);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 98,25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 99,20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 95,18);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 94,19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 94,20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 93,21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 82,22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 82,19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 86,21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 87,18);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 85,17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 55,34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 56,33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 56,32);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 61,35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 65,35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 63,33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 64,33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 64,27);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 64,26);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 64,25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 56,27);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 59,27);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 58,26);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 57,25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 60,18);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 61,17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 62,16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 64,14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 64,6);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 66,6);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 65,7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 59,11);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 59,10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 59,9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 53,6);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 53,5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 46,5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 45,5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 44,5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 44,4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 54,4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 55,4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 44,11);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 47,11);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 47,10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 45,10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 53,13);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 53,14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 53,17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 52,16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 52,15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 42,22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 45,22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 44,21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 47,21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 43,27);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 44,28);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 46,28);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 49,28);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 45,29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 48,29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 44,35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 43,35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 49,35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 42,34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 50,34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 49,36);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 43,41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 50,41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 45,49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 47,49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 49,49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 44,44);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 45,43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 51,43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 50,42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 44,42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 39,46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 38,46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 35,46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 33,46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 31,46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 35,47);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 35,49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 35,50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 35,50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 33,48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 32,48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 33,57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 32,56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 32,58);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 26,58);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 25,57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 26,56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 27,55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 22,55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 6,58);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 5,57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 5,56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 12,55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 16,55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 16,52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 16,49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 16,46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 19,41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 19,37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 19,33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 19,29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 19,25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 12,52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 12,49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 12,46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 9,41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 9,37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 9,33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 9,29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 9,25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 16,21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 16,19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 16,18);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 15,20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 9,19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 8,19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (74, 13, 9,17);

/* Worthless Guardian */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (70, 13, 17,9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (70, 13, 11,9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (70, 13, 12,11);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (70, 13, 14,11);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (70, 13, 16,11);

/* Hay */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (72, 13, 13,7);

/* Fray	*/
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (73, 13, 15,7);

/* LIttle Fat Bastard */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (71, 13, 90,8);

/* Savage Isle Guard */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (69, 28, 93,50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (69, 28, 93,53);

/* Savage */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (63, 28, 11,46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (63, 28, 19,45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (63, 28, 21,42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (63, 28, 25,45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (63, 28, 17,50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (63, 28, 31,38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (63, 28, 33,39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (63, 28, 37,36);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (63, 28, 41,38);

/* Paranoid Savage */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (64, 28, 56,5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (64, 28, 55,5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (64, 28, 55,4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (64, 28, 56,4);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (64, 28, 41,66);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (64, 28, 41,65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (64, 28, 41,64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (64, 28, 41,63);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (64, 28, 40,66);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (64, 28, 40,65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (64, 28, 40,64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (64, 28, 40,63);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (64, 28, 37,69);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (64, 28, 39,69);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (64, 28, 39,71);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (64, 28, 40,71);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (64, 28, 42,73);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (64, 28, 37,69);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (64, 28, 39,69);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (64, 28, 57,67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (64, 28, 58,72);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (64, 28, 57,76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (64, 28, 54,77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (64, 28, 51,75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (64, 28, 60,17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (64, 28, 60,18);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (64, 28, 57,17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (64, 28, 57,18);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (64, 28, 58,56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (64, 28, 54,55);

/* Rabid Savage */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (61, 28, 36,56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (61, 28, 28,60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (61, 28, 25,61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (61, 28, 25,72);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (61, 28, 29,36);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (61, 28, 25,35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (61, 28, 48,36);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (61, 28, 47,40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (61, 28, 51,39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (61, 28, 51,37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (61, 28, 53,36);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (61, 28, 55,36);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (61, 28, 45,48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (61, 28, 34,47);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (61, 28, 27,52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (61, 28, 20,58);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (61, 28, 11,58);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (61, 28, 12,61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (61, 28, 11,65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (61, 28, 9,70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (61, 28, 13,70);

/* Hungry Savage */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (62, 28, 56,9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (62, 28, 56,10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (62, 28, 57,9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (62, 28, 57,10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (62, 28, 58,14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (62, 28, 59,14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (62, 28, 58,16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (62, 28, 59,16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (62, 28, 49,69);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (62, 28, 52,66);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (62, 28, 57,63);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (62, 28, 55,57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (62, 28, 58,54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (62, 28, 49,53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (62, 28, 41,54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (62, 28, 39,56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (62, 28, 26,62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (62, 28, 24,63);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (62, 28, 25,68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (62, 28, 22,74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (62, 28, 54,45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (62, 28, 55,45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (62, 28, 56,45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (62, 28, 48,47);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (62, 28, 42,48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (62, 28, 38,45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (62, 28, 22,54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (62, 28, 16,58);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (62, 28, 12,66);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (62, 28, 10,66);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (62, 28, 11,76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (62, 28, 15,77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (62, 28, 19,77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (62, 28, 19,75);

/* Insanity */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (67, 28, 38,72);

/* Showboat */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (68, 28, 37,72);

/* Copycat */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (66, 28, 37,73);

/* Beefcake */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (65, 28, 56,2);

/* Green Slime */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (40, 7, 9, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (40, 7, 9, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (40, 7, 26, 52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (40, 7, 28, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (40, 7, 35, 54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (40, 7, 34, 47);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (40, 7, 14, 73);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (40, 7, 17, 69);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (40, 7, 54, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (40, 7, 44, 73);

/* Slime */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (41, 7, 35, 52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (41, 7, 29, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (41, 7, 34, 26);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (41, 7, 31, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (41, 7, 24, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (41, 7, 28, 29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (41, 7, 15, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (41, 7, 8, 29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (41, 7, 11, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (41, 7, 6, 36);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (41, 7, 2, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (41, 7, 2, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (41, 7, 3, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (41, 7, 14, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (41, 7, 11, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (41, 7, 21, 71);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (41, 7, 19, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (41, 7, 44, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (41, 7, 40, 78);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (41, 7, 36, 78);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (41, 7, 36, 74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (41, 7, 39, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (41, 7, 43, 69);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (41, 7, 42, 66);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (41, 7, 47, 78);

/* Pink Slime */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (42, 7, 13, 24);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (42, 7, 14, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (42, 7, 20, 32);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (42, 7, 19, 27);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (42, 7, 24, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (42, 7, 27, 19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (42, 7, 38, 23);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (42, 7, 70, 82);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (42, 7, 75, 82);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (42, 7, 52, 78);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (42, 7, 52, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (42, 7, 54, 72);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (42, 7, 52, 69);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (42, 7, 51, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (42, 7, 47, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (42, 7, 78, 97);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (42, 7, 75, 95);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (42, 7, 75, 91);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (42, 7, 69, 94);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (42, 7, 68, 91);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (42, 7, 68, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (42, 7, 68, 84);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (42, 7, 74, 86);

/* Gold Slime */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (43, 7, 34, 23);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (43, 7, 6, 26);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (43, 7, 2, 29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (43, 7, 9, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (43, 7, 38, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (43, 7, 50, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (43, 7, 49, 72);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (43, 7, 97, 95);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (43, 7, 92, 94);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (43, 7, 92, 97);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (43, 7, 87, 97);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (43, 7, 87, 92);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (43, 7, 82, 91);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (43, 7, 83, 97);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (43, 7, 79, 93);

/* Blue Slime */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (44, 7, 73, 48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (44, 7, 74, 53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (44, 7, 71, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (44, 7, 71, 62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (44, 7, 76, 62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (44, 7, 78, 58);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (44, 7, 79, 53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (44, 7, 79, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (44, 7, 84, 48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (44, 7, 84, 53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (44, 7, 84, 58);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (44, 7, 88, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (44, 7, 88, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (44, 7, 87, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (44, 7, 89, 51);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (44, 7, 91, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (44, 7, 94, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (44, 7, 94, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (44, 7, 91, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (44, 7, 94, 86);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (44, 7, 97, 86);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (44, 7, 97, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (44, 7, 93, 90);

/* Red Slime */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (45, 7, 49, 3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (45, 7, 51, 3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (45, 7, 54, 3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (45, 7, 57, 3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (45, 7, 61, 3);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (45, 7, 49, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (45, 7, 51, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (45, 7, 55, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (45, 7, 58, 6);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (45, 7, 61, 8);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (45, 7, 55, 9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (45, 7, 55, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (45, 7, 58, 13);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (45, 7, 59, 11);

/* King Goo */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (46, 7, 95, 87);

/* Sliminator */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (47, 7, 62, 4);

/* Goo Jr */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (48, 7, 93, 53);

/* Ancient Warrior */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 6, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 6, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 6, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 6, 8);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 7, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 9, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 11, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 13, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 14, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 17, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 20, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 26, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 37, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 39, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 36, 11);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 34, 9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 24, 24);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 22, 24);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 16, 24);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 14, 24);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 12, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 18, 38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 15, 38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 16, 41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 17, 41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 8, 41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 7, 38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 6, 38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 5, 41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 3, 47);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 3, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 8, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 8, 47);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 15, 47);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 15, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 20, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 20, 47);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 17, 58);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 16, 58);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 15, 58);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 20, 63);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 13, 63);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 15, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 18, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 18, 71);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 15, 71);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 15, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 18, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 7, 82);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 3, 82);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 3, 77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 7, 77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 7, 72);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 3, 72);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 3, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 7, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 7, 62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 3, 62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 3, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 7, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 13, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 11, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 4, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 11, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 14, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 18, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 23, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 26, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 29, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 23, 73);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 26, 73);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 23, 81);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 21, 81);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 28, 83);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 26, 86);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 33, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 36, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 62, 95);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 64, 95);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 69, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 71, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 73, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 75, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 87, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 89, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 93, 93);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 90, 83);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 95, 83);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 95, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 90, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 90, 77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 95, 77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 95, 74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 90, 74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 81, 73);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 78, 73);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 83, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 87, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 92, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 96, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 84, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 84, 47);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 89, 47);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 89, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 94, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 94, 47);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 95, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 92, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 84, 41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 81, 41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 81, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 84, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 82, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 76, 27);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 87, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 89, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 93, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 95, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 95, 29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 97, 29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 91, 29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 89, 29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 87, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 87, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 87, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 87, 8);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 93, 8);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 93, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 93, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 93, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 76, 6);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 74, 6);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 65, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (75, 29, 63, 10);

/* Elite Ancient Warrior */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (76, 29, 57, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (76, 29, 77, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (76, 29, 90, 11);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (76, 29, 97, 26);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (76, 29, 88, 41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (76, 29, 89, 54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (76, 29, 93, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (76, 29, 89, 93);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (76, 29, 22, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (76, 29, 9, 95);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (76, 29, 5, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (76, 29, 20, 53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (76, 29, 18, 64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (76, 29, 16, 64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (76, 29, 11, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (76, 29, 18, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (76, 29, 28, 24);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (76, 29, 35, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (76, 29, 45, 6);

/* Comissioned Blacksmith */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (77, 29, 80, 28);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (77, 29, 94, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (77, 29, 94, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (77, 29, 65, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (77, 29, 2, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (77, 29, 4, 53);

/* Comissioned Tailor */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (78, 29, 82, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (78, 29, 77, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (78, 29, 14, 81);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (78, 29, 16, 54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (78, 29, 17, 66);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (78, 29, 10, 36);

/* Dalph Von'Ownu */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (79, 29, 92, 70);

/* Hairy Fairy Princess */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (80, 29, 6, 93);

/* Lady Slither */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (81, 29, 89, 7);

/* Sir Insidious */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (82, 29, 91, 7);

/* Ztwel Tahp */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (83, 29, 8, 9);

/* Hair Dye shop */
/* Hair Dye Guy */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (84, 36, 38, 35);
/* Hair changers */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (85, 36, 33, 38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (86, 36, 34, 38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (87, 36, 35, 38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (88, 36, 36, 38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (89, 36, 40, 38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (90, 36, 41, 38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (91, 36, 42, 38);
/* Face changers */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (92, 36, 33, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (93, 36, 34, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (94, 36, 35, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (95, 36, 36, 42);
/* Sex changers */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (96, 36, 33, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (97, 36, 34, 35);

/* Magus Trainer */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (98, 36, 7, 66);
/* Priest Trainer */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (99, 36, 35, 94);
/* Potion Vendor */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (100, 36, 84, 6);
/* Bronze Armor Vendor */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (101, 36, 4, 92);
/* Iron Armor Vendor */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (102, 36, 7, 92);
/* Steel Armor Vendor */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (103, 36, 9, 92);
/* Weapon Vendor */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (104, 36, 5, 37);
/* Shield Vendor */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (105, 36, 8, 37);
/* Cloth Armor Vendor */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (106, 36, 56, 5);
/* Leather Armor Vendor */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (107, 36, 57, 5);
/* Silk Armor Vendor */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (108, 36, 59, 5);

/* Melty 	(snowman) */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (114, 17, 8,82)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (114, 17, 33,90)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (114, 17, 25,82)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (114, 17, 45,92)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (114, 17, 45,88)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (114, 17, 53,88)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (114, 17, 51,77)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (114, 17, 61,93)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (114, 17, 78,74)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (114, 17, 88,75)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (114, 17, 23,45)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (114, 17, 27,45)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (114, 17, 28,30)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (114, 17, 26,27)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (114, 17, 29,25)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (114, 17, 31,28)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (114, 17, 10,51)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (114, 17, 49,62)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (114, 17, 51,62)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (114, 17, 41,20)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (114, 17, 39,22)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (114, 17, 38,21)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (114, 17, 48,15)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (114, 17, 47,13)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (114, 17, 52,15)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (114, 17, 53,13)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (114, 17, 95,80)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (114, 17, 62,23)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (114, 17, 60,23)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (114, 17, 86,17)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (114, 17, 89,16)

/* Midnight Madness	(purple bunny) */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (113, 17, 44,95)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (113, 17, 39,94)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (113, 17, 57,94)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (113, 17, 60,98)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (113, 17, 68,96)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (113, 17, 75,98)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (113, 17, 76,93)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (113, 17, 73,90)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (113, 17, 37,99)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (113, 17, 27,99)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (113, 17, 19,99)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (113, 17, 12,98)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (113, 17, 22,96)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (113, 17, 28,93)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (113, 17, 19,91)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (113, 17, 13,91)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (113, 17, 1,79)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (113, 17, 5,81)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (113, 17, 20,77)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (113, 17, 27,88)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (113, 17, 41,81)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (113, 17, 48,83)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (113, 17, 42,86)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (113, 17, 61,86)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (113, 17, 85,71)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (113, 17, 71,70)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (113, 17, 80,77)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (113, 17, 31,63)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (113, 17, 42,65)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (113, 17, 42,57)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (113, 17, 46,60)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (113, 17, 54,58)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (113, 17, 48,50)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (113, 17, 52,50)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (113, 17, 48,44)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (113, 17, 52,44)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (113, 17, 62,54)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (113, 17, 60,33)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (113, 17, 4,43)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (113, 17, 9,43)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (113, 17, 38,14)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (113, 17, 33,35)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (113, 17, 37,33)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (113, 17, 41,30)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (113, 17, 33,35)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (113, 17, 37,33)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (113, 17, 41,30)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (113, 17, 65,66)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (113, 17, 77,63)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (113, 17, 85,63)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (113, 17, 92,91)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (113, 17, 83,94)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (113, 17, 97,80)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (113, 17, 97,83)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (113, 17, 63,45)

/* Snowball	(brown bunny) */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 5,86)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 50,85)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 45,78)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 39,76)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 86,89)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 84,95)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 91,95)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 94,85)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 87,82)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 71,74)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 73,77)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 84,74)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 31,83)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 19,80)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 19,75)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 26,74)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 32,76)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 35,69)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 15,71)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 10,71)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 10,68)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 3,71)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 5,61)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 13,62)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 21,65)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 31,62)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 37,64)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 17,57)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 26,54)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 35,53)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 38,50)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 25,45)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 18,49)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 2,48)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 2,53)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 5,32)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 13,34)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 48,37)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 52,37)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 48,30)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 52,30)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 48,22)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 52,22)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 49,17)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 46,16)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 54,17)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 54,15)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 62,16)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 67,17)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 8,22)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 15,22)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 19,21)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 20,27)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 22,26)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 23,19)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 15,16)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 22,14)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 33,18)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 60,61)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 98,71)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 92,68)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 76,58)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 66,38)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 72,35)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 79,34)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 84,38)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 96,31)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 84,54)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 87,58)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 93,57)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 98,57)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 95,53)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 85,30)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 80,26)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 69,28)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 75,24)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 86,24)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 94,24)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 79,16)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 92,10)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 90,5)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 85,8)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 80,8)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 72,6)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (112, 17, 71,10)

/* Carrots -Boss- (Green Color) */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (110, 17, 99,82)

/* Fluffzilla -Boss- */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (111, 17, 50,13)


/* Unloveds */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 97,94)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 94,94)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 94,90)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 90,88)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 90,83)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 90,77)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 90,71)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 91,69)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 91,67)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 93,68)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 93,70)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 95,69)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 95,67)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 93,66)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 95,64)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 89,61)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 88,61)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 82,61)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 80,62)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 80,60)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 78,53)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 84,52)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 84,54)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 89,52)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 90,50)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 92,50)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 90,47)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 78,67)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 78,69)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 41,72)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 42,71)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 41,70)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 44,67)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 52,68)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 59,68)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 65,68)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 71,68)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 85,92)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 81,90)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 77,91)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 71,89)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 71,79)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 67,77)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 59,77)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 57,82)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 49,85)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 47,83)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 46,85)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 47,91)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 49,96)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 56,97)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 62,96)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 68,98)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 68,95)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 70,95)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 41,96)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 32,96)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 24,96)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 14,95)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 12,94)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 9,93)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 12,88)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 15,87)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 15,91)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 18,91)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 17,94)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 20,89)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 8,84)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 9,84)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 9,78)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (115, 2, 8,78)


/* Giant WRAITH(Batcaves attached 2nd level) */

INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (117, 2, 95,9)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (117, 2, 92,12)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (117, 2, 88,9)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (117, 2, 92,5)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (117, 2, 80,4)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (117, 2, 80,7)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (117, 2, 80,12)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (117, 2, 82,15)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (117, 2, 77,17)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (117, 2, 74,15)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (117, 2, 71,17)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (117, 2, 68,15)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (117, 2, 64,17)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (117, 2, 64,11)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (117, 2, 67,11)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (117, 2, 67,5)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (117, 2, 74,5)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (117, 2, 64,5)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (117, 2, 65,23)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (117, 2, 64,26)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (117, 2, 64,30)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (117, 2, 65,33)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (117, 2, 68,34)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (117, 2, 70,35)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (117, 2, 72,34)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (117, 2, 76,31)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (117, 2, 76,25)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (117, 2, 71,21)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (117, 2, 69,21)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (117, 2, 67,26)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (117, 2, 70,25)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (117, 2, 73,26)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (117, 2, 73,31)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (117, 2, 70,31)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (117, 2, 67,31)

/* NIBBLES */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (116, 2, 89,49)

/* NIBBLES 2 */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (118, 2, 70,28)

/* Teleport vendor guy vendor */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (119, 1, 56,52)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (119, 8, 3, 46)

/* Candy Necklace Vendor */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (120, 3, 46, 34)

/* Paradise Teleport vendor guy vendor */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (121, 35, 76,31)

/* The Bat Man */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (122, 2, 95,95)

/* Pancake */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (133, 32, 32,51)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (133, 32, 31,57)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (133, 32, 30,58)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (133, 32, 29,59)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (133, 32, 28,58)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (133, 32, 27,57)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (133, 32, 28,63)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (133, 32, 22,63)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (133, 32, 25,68)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (133, 32, 14,65)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (133, 32, 18,59)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (133, 32, 13,55)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (133, 32, 8,59)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (133, 32, 9,69)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (133, 32, 10,78)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (133, 32, 17,72)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (133, 32, 20,78)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (133, 32, 20,82)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (133, 32, 8,82)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (133, 32, 12,87)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (133, 32, 10,93)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (133, 32, 4,93)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (133, 32, 16,98)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (133, 32, 19,98)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (133, 32, 27,96)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (133, 32, 31,90)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (133, 32, 30,84)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (133, 32, 36,78)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (133, 32, 28,75)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (133, 32, 33,70)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (133, 32, 22,90)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (133, 32, 40,93)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (133, 32, 47,96)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (133, 32, 50,99)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (133, 32, 53,95)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (133, 32, 64,97)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (133, 32, 62,90)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (133, 32, 54,86)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (133, 32, 44,84)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (133, 32, 48,79)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (133, 32, 39,74)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (133, 32, 54,76)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (133, 32, 61,74)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (133, 32, 67,83)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (133, 32, 71,89)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (133, 32, 79,84)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (133, 32, 87,84)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (133, 32, 94,91)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (133, 32, 94,96)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (133, 32, 94,98)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (133, 32, 96,95)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (133, 32, 98,96)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (133, 32, 99,98)

/* ENDLESS HERO */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (137, 32, 96,99)

/* Flapjack */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 7,17)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 14,19)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 20,19)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 27,17)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 33,14)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 25,10)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 19,13)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 17,12)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 15,13)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 10,10)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 17,5)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 31,5)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 33,3)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 31,1)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 47,2)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 41,8)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 51,8)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 53,8)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 60,3)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 67,4)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 67,1)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 76,3)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 78,8)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 86,6)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 85,3)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 95,10)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 97,17)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 87,15)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 79,17)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 77,17)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 70,14)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 67,14)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 67,16)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 64,14)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 67,12)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 85,21)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 91,26)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 93,26)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 96,33)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 91,30)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 89,38)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 93,44)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 93,52)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 87,47)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 92,52)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 89,59)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 93,59)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 92,64)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 79,64)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 76,68)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 68,59)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 65,59)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 63,54)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 53,58)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 52,53)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 52,45)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 60,47)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 62,47)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 74,52)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 76,46)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 77,46)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 77,24)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 68,25)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 67,25)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 66,25)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 65,35)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 65,39)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 66,42)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 55,37)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 58,37)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 65,35)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 70,29)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 68,29)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 58,30)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 53,30)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 49,23)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 57,23)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 57,16)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 57,14)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 57,12)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 49,14)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (134, 32, 45,18)

/* Sorrows HERO */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (132, 32, 17,14)


/* Cranky Ewe */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (135, 14, 76, 60)
/* Frantic Monkey */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (136, 8, 32, 5);

/* Ancient Defender */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (123, 24, 96,88)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (123, 24,  97,83)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (123, 24,  82,81)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (123, 24,  78,85)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (123, 24,  81,88)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (123, 24,  78,90)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (123, 24,  73,90)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (123, 24,  70,95)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (123, 24,  68,86)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (123, 24,  73,85)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (123, 24,  69,81)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (123, 24,  35,80)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (123, 24,  33,83)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (123, 24,  28,86)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (123, 24,  28,92)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (123, 24,  33,91)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (123, 24,  19,95)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (123, 24,  23,92)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (123, 24,  23,86)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (123, 24,  25,81)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (123, 24,  19,81)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (123, 24,  5,81)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (123, 24,  5,89)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (123, 24,  52,75)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (123, 24,  49,75)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (123, 24,  49,69)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (123, 24,  52,69)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (123, 24,  56,66)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (123, 24,  54,65)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (123, 24,  55,63)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (123, 24,  51,65)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (123, 24,  50,66)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (123, 24,  47,66)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (123, 24,  44,66)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (123, 24,  45,64)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (123, 24,  49,61)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (123, 24,  52,61)

/* Ancient Safeguard */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (125, 24,  54,87)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (125, 24,  54,84)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (125, 24,  47,84)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (125, 24,  47,87)

/* DOOM */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (140, 24,  51,55)

/* Ancient Sentinel (Right side) */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (138, 24,  76,93)

/* Ancient Sentinel (L Side) */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (138, 24,  26,94)

/* Ancient Guards */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (124, 24,  4,50)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (124, 24,  7,57)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (124, 24,  3,60)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (124, 24,  5,65)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (124, 24,  11,64)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (124, 24,  14,65)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (124, 24,  21,67)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (124, 24,  21,59)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (124, 24,  14,58)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (124, 24,  12,53)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (124, 24,  20,53)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (124, 24,  13,48)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (124, 24,  13,43)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (124, 24,  20,42)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (124, 24,  25,41)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (124, 24,  31,41)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (124, 24,  31,53)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (124, 24,  33,51)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (124, 24,  97,50)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (124, 24,  99,59)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (124, 24,  97,64)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (124, 24,  94,67)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (124, 24,  93,57)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (124, 24,  88,62)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (124, 24,  84,65)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (124, 24,  84,68)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (124, 24,  79,64)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (124, 24,  80,59)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (124, 24,  84,58)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (124, 24,  81,55)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (124, 24,  76,53)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (124, 24,  80,49)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (124, 24,  81,45)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (124, 24,  86,43)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (124, 24,  89,50)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (124, 24,  76,41)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (124, 24,  70,41)

/* Ancient Guards (MOVING) */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (143, 24,  89,54)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (143, 24,  88,67)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (143, 24,  5,48)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (143, 24,  11,61)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (143, 24,  23,64)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (143, 24,  16,45)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (143, 24,  74,49)

/* General Vor Chez (L Side) */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (139, 24,  29,49)

/* General Mehoff (R Side) */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (126, 24,  72,46)

/* Disciple Von Bangs (R Side) */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (141, 24,  87,20)

/* Disciple Von Chief (L Side) */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (142, 24,  12,19)

/* Giants Wraiths */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  45,45)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  45,50)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  40,54)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  36,51)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  38,46)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  39,39)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  36,30)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  30,34)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  27,31)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  36,30)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  33,25)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  25,25)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  25,23)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  21,20)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  17,23)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  17,25)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  21,28)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  29,18)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  29,14)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  33,16)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  37,18)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  38,9)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  33,9)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  33,4)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  38,4)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  26,5)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  26,1)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  20,3)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  14,5)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  8,5)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  4,6)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  4,12)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  10,12)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  10,19)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  4,19)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  6,26)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  10,26)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  8,28)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  14,19)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  12,32)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  19,36)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  14,40)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  12,40)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  8,36)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  4,40)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  2,40)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  4,32)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  9,47)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  4,47)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  4,54)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  9,54)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  16,46)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  22,46)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  27,46)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  33,46)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  32,54)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  24,54)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  25,50)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  18,50)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  15,56)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  15,61)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  7,59)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  6,59)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  6,60)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  7,60)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  24,59)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  25,59)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  32,59)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  40,59)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  36,62)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  32,65)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  36,65)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  40,65)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  41,70)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  32,70)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  26,67)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  22,67)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  18,67)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  12,67)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  5,67)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  10,71)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  12,71)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  9,76)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  3,75)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  9,81)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  4,86)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  13,87)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  16,91)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  21,89)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  25,91)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  29,89)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  33,90)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  35,92)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  37,95)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  41,95)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  39,92)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  37,90)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  39,88)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  35,88)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  37,86)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  38,82)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  36,82)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  33,78)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  41,78)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  38,74)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  36,74)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  9,91)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  4,92)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  9,95)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  4,97)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  13,93)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  13,99)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  16,95)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  23,95)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  25,96)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  23,99)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  29,99)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  27,96)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (145, 33,  29,95)

/* Invisible1 */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (147, 33,  25,94)

/* Invisible1,2 */
/* 27,94 */

/* Returned */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  58,48)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  63,48)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  63,45)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  66,45)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  66,40)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  68,40)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  63,40)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  63,35)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  60,35)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  73,35)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  73,30)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  68,30)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  63,30)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  63,25)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  68,25)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  73,25)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  73,20)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  68,20)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  63,20)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  63,15)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  68,15)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  73,15)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  63,10)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  63,5)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  68,5)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  73,5)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  78,5)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  78,10)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  78,15)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  78,20)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  78,25)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  78,30)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  78,35)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  83,35)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  83,30)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  83,25)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  83,20)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  83,15)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  83,10)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  83,5)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  88,5)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  88,10)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  88,15)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  88,20)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  88,25)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  88,30)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  88,35)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  88,40)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  93,40)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  93,35)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  93,25)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  93,20)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  93,15)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  93,10)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  93,5)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  98,5)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  98,10)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  98,15)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  98,20)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  98,25)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  98,30)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  98,35)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  98,40)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  98,45)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  93,45)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  88,45)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  88,50)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  93,50)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  98,50)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  98,55)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  93,55)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  88,55)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  83,55)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  78,55)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  73,55)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  68,55)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  63,55)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  63,60)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  68,60)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  63,65)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  63,70)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  63,75)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  63,80)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  63,85)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  63,90)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  63,95)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  63,99)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  68,99)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  68,95)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  68,90)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  68,85)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  68,80)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  68,75)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  68,70)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  68,65)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  73,60)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  73,65)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  73,70)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  73,75)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  73,80)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  78,80)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  78,75)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  78,70)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  78,65)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  78,60)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  83,60)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  83,65)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  83,70)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  83,75)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  83,80)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  88,80)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  93,80)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  98,80)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  98,75)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  98,70)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  98,65)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  98,60)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  93,60)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  93,65)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  88,60)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  88,65)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  88,70)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  93,70)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  93,75)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  88,75)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  88,85)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  93,85)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  98,85)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  98,90)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  93,90)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  88,90)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  88,95)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  93,95)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  98,95)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  98,99)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  93,99)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  88,99)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  83,99)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  83,95)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  78,95)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  78,99)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  73,99)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (146, 33,  73,95)

/* Invisible2 */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (144, 33,  81,20)
/* Invisible2,2 */
/* 89,72 */

/* Tailoring Supplies */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (148, 36, 14, 6);
/* Smithing Supplies */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (149, 36, 2, 41);
/* Scriber */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (150, 36, 24, 6);

/* Richard */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 52,44)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 57,44)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 59,38)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 57,32)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 50,36)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 45,42)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 38,39)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 37,39)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 30,42)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 25,48)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 18,48)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 21,42)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 12,42)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 7,37)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 17,37)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 25,37)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 6,28)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 14,24)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 17,27)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 20,31)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 30,30)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 37,33)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 38,26)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 33,25)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 33,21)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 27,23)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 8,18)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 22,22)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 25,16)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 31,10)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 31,4)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 19,4)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 13,4)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 16,1)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 7,8)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 42,6)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 40,14)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 47,20)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 46,25)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 53,28)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 63,27)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 64,25)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 70,28)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 70,35)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 67,42)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 76,46)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 77,40)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 81,40)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 81,36)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 86,40)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 88,46)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 96,46)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 94,38)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 90,33)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 74,23)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 80,26)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 82,21)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 90,22)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 89,27)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 97,27)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 93,20)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 91,13)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 90,7)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 94,7)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 92,5)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 93,3)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 94,3)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 75,9)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 74,16)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 92,14)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 66,16)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 56,21)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 52,11)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 56,6)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 60,11)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 66,4)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (151, 34, 67,4)

/* Abomination */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (152, 34, 10,9)

/* Simon */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 62,69)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 62,72)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 62,75)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 67,75)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 69,67)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 69,64)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 76,66)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 75,72)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 82,70)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 80,76)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 80,81)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 90,77)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 96,77)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 93,82)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 82,87)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 89,87)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 94,87)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 94,95)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 86,94)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 82,98)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 78,94)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 73,98)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 68,94)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 63,97)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 57,99)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 58,94)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 55,94)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 61,89)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 63,89)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 72,90)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 73,85)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 70,85)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 65,80)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 74,70)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 73,79)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 53,74)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 41,78)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 39,74)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 43,68)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 34,70)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 28,71)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 31,76)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 32,76)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 29,64)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 34,65)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 35,65)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 39,62)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 17,62)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 23,67)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 17,70)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 16,77)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 16,81)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 18,81)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 18,86)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 23,86)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 26,86)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 31,81)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 35,86)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 38,86)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 40,90)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 40,95)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 46,98)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 48,94)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 47,90)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 52,89)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 50,85)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 51,83)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 53,80)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 59,83)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 46,80)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 23,75)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 11,73)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 11,66)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 6,64)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 5,64)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 7,75)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 7,77)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 8,82)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 8,84)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 4,88)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 4,95)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 9,95)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 7,99)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 15,92)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 19,98)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 21,98)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 26,94)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 31,98)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 31,92)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 46,62)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (153, 34, 58,63)

/* Charlie */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (154, 34, 47,75)


/* Berry Stealer */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (127, 42, 92,47)
/* Guardian Bear */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (128, 42, 71,3)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (128, 42, 63,3)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (128, 42, 54,3)
/* Betsy the Bear Charmer */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (131, 42, 30,39)
/* Gramps */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (157, 42, 10,10)
/* Young Bear */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 5,5)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 10,5)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 15,5)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 20,5)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 25,5)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 30,5)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 35,5)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 40,5)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 45,5)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 50,5)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 55,5)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 60,5)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 65,5)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 75,5)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 85,5)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 95,5)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 5,10)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 15,10)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 25,10)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 35,10)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 40,10)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 50,10)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 55,10)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 60,10)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 65,10)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 70,10)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 75,10)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 80,10)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 85,10)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 90,10)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 95,10)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 5,15)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 10,15)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 15,15)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 20,15)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 25,15)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 30,15)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 35,15)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 40,15)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 45,15)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 50,15)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 55,15)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 60,15)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 65,15)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 75,15)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 85,15)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 95,15)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 5,20)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 15,20)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 25,20)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 35,20)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 40,20)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 50,20)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 55,20)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 60,20)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 65,20)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 70,20)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 75,20)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 80,20)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 85,20)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 90,20)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 95,20)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 5,35)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 10,35)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 15,35)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 20,35)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 25,35)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 30,35)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 35,35)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 40,35)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 45,35)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 50,35)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 55,35)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 60,35)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 65,35)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 75,35)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 85,35)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 95,35)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 5,50)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 15,50)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 25,50)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 35,50)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 40,50)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 50,50)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 55,50)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 60,50)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 65,50)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 70,50)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 75,50)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 80,50)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 85,50)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 90,50)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 95,50)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 5,55)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 10,55)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 15,55)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 20,55)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 25,55)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 30,55)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 35,55)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 40,55)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 45,55)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 50,55)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 55,55)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 60,55)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 65,55)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 75,55)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 85,55)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 95,55)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 5,70)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 15,70)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 25,70)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 35,70)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 40,70)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 50,70)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 55,70)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 60,70)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 65,70)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 70,70)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 75,70)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 80,70)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 85,70)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 90,70)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 95,70)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 5,75)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 10,75)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 15,75)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 20,75)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 25,75)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 30,75)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 35,75)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 40,75)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 45,75)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 50,75)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 55,75)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 60,75)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 65,75)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 75,75)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 85,75)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 95,75)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 5,90)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 15,90)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 25,90)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 35,90)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 40,90)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 50,90)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 55,90)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 60,90)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 65,90)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 70,90)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 75,90)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 80,90)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 85,90)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 90,90)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (130, 42, 95,90)

/* Mama Bear */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (156, 43, 10,15)
/* Patrol Bear */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 5,5)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 10,5)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 15,5)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 20,5)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 25,5)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 30,5)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 35,5)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 40,5)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 45,5)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 50,5)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 55,5)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 60,5)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 65,5)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 75,5)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 85,5)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 95,5)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 5,10)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 15,10)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 25,10)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 35,10)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 40,10)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 50,10)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 55,10)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 60,10)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 65,10)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 70,10)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 75,10)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 80,10)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 85,10)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 90,10)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 95,10)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 5,15)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 10,15)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 15,15)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 20,15)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 25,15)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 30,15)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 35,15)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 40,15)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 45,15)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 50,15)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 55,15)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 60,15)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 65,15)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 75,15)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 85,15)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 95,15)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 5,20)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 15,20)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 25,20)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 35,20)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 40,20)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 50,20)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 55,20)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 60,20)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 65,20)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 70,20)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 75,20)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 80,20)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 85,20)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 90,20)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 95,20)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 5,35)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 10,35)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 15,35)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 20,35)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 25,35)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 30,35)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 35,35)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 40,35)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 45,35)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 50,35)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 55,35)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 60,35)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 65,35)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 75,35)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 85,35)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 95,35)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 5,50)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 15,50)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 25,50)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 35,50)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 40,50)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 50,50)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 55,50)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 60,50)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 65,50)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 70,50)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 75,50)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 80,50)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 85,50)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 90,50)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 95,50)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 5,55)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 10,55)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 15,55)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 20,55)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 25,55)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 30,55)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 35,55)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 40,55)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 45,55)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 50,55)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 55,55)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 60,55)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 65,55)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 75,55)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 85,55)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 95,55)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 5,70)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 15,70)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 25,70)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 35,70)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 40,70)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 50,70)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 55,70)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 60,70)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 65,70)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 70,70)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 75,70)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 80,70)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 85,70)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 90,70)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 95,70)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 5,75)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 10,75)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 15,75)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 20,75)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 25,75)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 30,75)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 35,75)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 40,75)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 45,75)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 50,75)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 55,75)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 60,75)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 65,75)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 75,75)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 85,75)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 95,75)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 5,90)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 15,90)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 25,90)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 35,90)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 40,90)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 50,90)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 55,90)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 60,90)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 65,90)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 70,90)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 75,90)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 80,90)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 85,90)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 90,90)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (129, 43, 95,90)

/* The Patriarch */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (155, 44, 50,86)
/* Starved Bear */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (158, 44, 50,83)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (158, 44, 55,83)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (158, 44, 60,83)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (158, 44, 62,84)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (158, 44, 65,85)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (158, 44, 55,86)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (158, 44, 47,86)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (158, 44, 44,88)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (158, 44, 52,88)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (158, 44, 57,88)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (158, 44, 61,88)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (158, 44, 65,90)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (158, 44, 61,92)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (158, 44, 62,94)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (158, 44, 59,97)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (158, 44, 57,93)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (158, 44, 54,97)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (158, 44, 52,95)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (158, 44, 52,93)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (158, 44, 47,91)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (158, 44, 44,93)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (158, 44, 43,95)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (158, 44, 44,98)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (158, 44, 48,97)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (158, 44, 48,94)

/* Level 25 Wandering Scribe */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (159, 10, 50,50)
/* Level 35 Wandering Scribe */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (160, 6, 50,70)

/* Ancient Royal */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (161, 24, 46,34)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (161, 24, 46,36)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (161, 24, 49,36)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (161, 24, 49,34)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (161, 24, 52,34)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (161, 24, 52,36)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (161, 24, 55,36)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (161, 24, 55,34)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (161, 24, 55,29)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (161, 24, 55,25)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (161, 24, 46,29)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (161, 24, 46,25)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (161, 24, 46,21)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (161, 24, 55,21)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (161, 24, 55,16)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (161, 24, 55,12)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (161, 24, 51,14)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (161, 24, 50,14)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (161, 24, 49,14)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (161, 24, 46,16)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (161, 24, 46,12)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (161, 29, 46,45)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (161, 29, 47,43)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (161, 29, 43,50)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (161, 29, 47,50)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (161, 29, 47,53)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (161, 29, 42,53)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (161, 29, 44,56)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (161, 29, 52,46)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (161, 29, 53,44)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (161, 29, 56,43)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (161, 29, 58,45)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (161, 29, 56,46)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (161, 29, 56,51)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (161, 29, 52,51)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (161, 29, 53,56)
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (161, 29, 57,56)
/* King Terror */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (162, 29, 44, 44)
/* Prince Punisher */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (163, 24, 46, 8)
/* Queen Butcher */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (164, 24, 55, 8)
/* Princess Slayer */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (165, 24, 52, 8)

/* Phat Lewtz */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (170, 1, 46, 50)

/* Event NPCs */
/* Team 1 Guards */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (171, 22, 10, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (171, 22, 10, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (171, 22, 10, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (171, 22, 10, 48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (171, 22, 5, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (171, 22, 4, 44);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (171, 22, 5, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (171, 22, 6, 44);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (171, 22, 14, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (171, 22, 14, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (171, 22, 14, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (171, 22, 14, 48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (171, 22, 18, 48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (171, 22, 18, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (171, 22, 18, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (171, 22, 18, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (171, 22, 22, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (171, 22, 22, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (171, 22, 22, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (171, 22, 22, 48);
/* Team 1 Boss */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (172, 22, 5, 44);
/* Team 2 Guards */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (173, 22, 28, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (173, 22, 28, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (173, 22, 28, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (173, 22, 28, 48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (173, 22, 32, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (173, 22, 32, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (173, 22, 32, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (173, 22, 32, 48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (173, 22, 36, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (173, 22, 36, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (173, 22, 36, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (173, 22, 36, 48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (173, 22, 40, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (173, 22, 40, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (173, 22, 40, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (173, 22, 40, 48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (173, 22, 45, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (173, 22, 46, 44);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (173, 22, 45, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (173, 22, 44, 44);
/* Team 2 Boss */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (174, 22, 45, 44);

/* Ancient Prisoner */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 37, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 37, 99);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 42, 94);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 42, 97);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 47, 91);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 47, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 52, 97);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 52, 92);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 58, 97);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 58, 95);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 58, 92);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 65, 95);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 71, 91);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 77, 91);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 82, 99);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 77, 99);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 72, 99);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 84, 94);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 71, 86);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 77, 86);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 77, 81);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 71, 81);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 82, 79);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 82, 74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 77, 73);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 73, 73);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 67, 72);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 65, 72);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 58, 77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 65, 79);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 45, 81);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 45, 72);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 38, 72);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 38, 81);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 28, 74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 28, 78);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 33, 74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 33, 78);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 20, 81);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 16, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 21, 72);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 21, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 16, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 16, 62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 21, 62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 26, 62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 20, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 23, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 25, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 33, 64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 33, 61);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 33, 58);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 42, 64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 40, 62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 42, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 40, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 45, 54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 47, 54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 49, 54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 51, 54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 56, 54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 61, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 58, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 63, 64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 66, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 68, 54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 71, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 68, 64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 79, 64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 82, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 79, 55);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 76, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 53, 48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 47, 48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 47, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 53, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 53, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 47, 45);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 49, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 50, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 51, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 49, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 50, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 51, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 51, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 50, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 49, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 49, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 50, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 51, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 59, 37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 66, 36);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 73, 37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 79, 37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 87, 38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 85, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 82, 32);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 92, 37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 92, 32);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 87, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 82, 27);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 98, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 98, 32);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 97, 27);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 92, 27);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 99, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 97, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 92, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 90, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 85, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 83, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 78, 19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 81, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 83, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 85, 11);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 90, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 90, 8);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 91, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 92, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 72, 19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 74, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 77, 9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 82, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 39, 37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 33, 37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 25, 37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 18, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 17, 38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 15, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 13, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 12, 38);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 10, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 6, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 3, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 2, 37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 2, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 7, 37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 7, 32);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 2, 28);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 3, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 11, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 11, 23);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 11, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 6, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 11, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 4, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 3, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 4, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 8, 8);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 14, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 14, 8);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 14, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 16, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 21, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 21, 8);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 21, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 28, 9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 30, 6);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (175, 40, 26, 12);
/* Ancient Prison Superior */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (176, 40, 74, 95);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (176, 40, 62, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (176, 40, 41, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (176, 40, 27, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (176, 40, 74, 59);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (176, 40, 50, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (176, 40, 91, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (176, 40, 79, 5);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (176, 40, 4, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (176, 40, 10, 9);
/* 177 */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (177, 40, 70, 7);
/* 178 */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (178, 40, 50, 14);
/* 179 */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (179, 40, 28, 19);

/* 166 */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 94, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 93, 22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 93, 23);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 93, 24);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 93, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 93, 26);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 93, 27);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 93, 28);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 94, 29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 95, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 96, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 97, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 98, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 99, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 98, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 97, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 96, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 94, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 93, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 92, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 97, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 95, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 93, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 91, 41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 90, 36);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 89, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 88, 23);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 88, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 88, 27);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 88, 29);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 83, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 81, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 80, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 81, 22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 82, 24);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 83, 26);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 76, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 74, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 70, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 67, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 68, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 72, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 72, 24);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 69, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 72, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 76, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 80, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 83, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 78, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 82, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 80, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 74, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 69, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 91, 71);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 92, 71);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 93, 71);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 94, 71);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 94, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 95, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 96, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 97, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 98, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 91, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 92, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 93, 66);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 94, 66);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 95, 66);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 97, 66);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 96, 66);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 98, 66);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 90, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 91, 64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 92, 63);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 93, 62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 94, 62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 95, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 96, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 97, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 98, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 91, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 91, 51);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 91, 52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 89, 52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 90, 53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 91, 53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 91, 54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 92, 53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 93, 52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 91, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 83, 46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 83, 47);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 82, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 82, 50);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 82, 51);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 82, 52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 76, 54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 71, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 67, 56);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 70, 52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 70, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 71, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 72, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 73, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 73, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 72, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 70, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 71, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 77, 62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 70, 63);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 66, 62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (166, 38, 64, 60);
/* 167 */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (167, 38, 95, 75);
/* 168 */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 73, 73);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 70, 73);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 77, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 80, 78);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 76, 83);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 75, 89);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 71, 94);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 67, 94);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 65, 91);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 66, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 69, 86);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 70, 82);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 70, 79);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 65, 78);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 64, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 65, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 66, 81);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 65, 83);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 72, 79);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 72, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 60, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 61, 91);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 58, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 55, 86);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 60, 83);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 60, 79);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 58, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 61, 71);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 53, 73);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 50, 71);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 51, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 52, 81);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 49, 84);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 51, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 49, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 54, 92);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 52, 97);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 47, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 44, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 45, 92);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 45, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 44, 84);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 46, 81);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 45, 77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 46, 74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 43, 71);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 40, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 37, 72);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 39, 77);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 41, 82);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 37, 84);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 39, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 41, 89);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 41, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 37, 94);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 32, 93);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 34, 96);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 33, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 31, 86);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 31, 83);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 31, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 31, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 31, 72);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 31, 64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 33, 62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 34, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 31, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 34, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 33, 83);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 29, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 24, 90);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 19, 91);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 20, 89);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 26, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 25, 84);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 23, 79);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 26, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 24, 74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 25, 68);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 27, 65);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 21, 64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 20, 62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 24, 63);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 19, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 19, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 17, 81);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 18, 82);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 15, 88);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 11, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 6, 87);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 4, 89);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 3, 85);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 7, 84);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 9, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 14, 80);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 12, 76);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 4, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 6, 72);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 10, 74);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 13, 70);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 16, 75);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 22, 71);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 16, 66);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 16, 62);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 11, 64);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 9, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 4, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 13, 67);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 13, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 8, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 3, 60);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 6, 54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 3, 53);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 7, 52);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 12, 51);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 15, 54);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 13, 57);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 9, 58);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 9, 46);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 6, 48);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 4, 49);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 3, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 6, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 9, 37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 9, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 4, 37);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 5, 34);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 8, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 5, 30);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 8, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 9, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 9, 24);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 8, 24);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 8, 26);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 9, 26);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 8, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 9, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 10, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 12, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 14, 9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 18, 8);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 24, 7);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 28, 6);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 33, 6);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 36, 6);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 39, 8);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 39, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 38, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 33, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 30, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 26, 11);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 24, 13);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 22, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 19, 11);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 18, 13);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 17, 16);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 16, 17);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 15, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 15, 22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 17, 24);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 20, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 25, 23);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 24, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 21, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 22, 18);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 26, 18);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 28, 19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 28, 21);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 19, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 20, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 22, 31);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 23, 32);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 19, 33);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 21, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 27, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 33, 23);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 44, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 45, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 45, 19);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 46, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 47, 23);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 48, 25);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 46, 27);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 41, 26);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 43, 23);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 44, 22);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 38, 20);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 39, 18);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 36, 18);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 48, 32);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 47, 35);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 47, 39);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 46, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 41, 41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 40, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 37, 42);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 34, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 33, 41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 32, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 28, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 22, 44);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 21, 43);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 19, 41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 23, 41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 26, 41);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 24, 40);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 41, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 42, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 43, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 44, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 43, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 42, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 41, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 41, 11);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 41, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 41, 13);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 45, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 45, 11);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 45, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 45, 13);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 45, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 40, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 43, 9);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 43, 15);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 31, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 32, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 33, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 32, 10);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 33, 11);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 34, 12);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 33, 13);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 32, 14);
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (168, 38, 29, 12);
/* 169 */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (169, 38, 43, 12);

/* Credit Exchange */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (180, 36, 58, 78);
/* Pet Trainer */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (181, 36, 58, 74);

/* Alchemy Vendor */
INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y) VALUES (182, 36, 96, 6);

DROP TABLE npc_drops;
CREATE TABLE npc_drops (
  npc_template_id INT NOT NULL,
  item_template_id INT NOT NULL,
  stack INT NOT NULL,
  droprate DECIMAL(9,4) NOT NULL
)

/* Mice drops */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (1, 1, 10, 10); /* Gold */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (1, 2, 1, 5); /* Old Rags */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (1, 3, 1, 5); /* Stick */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (1, 4, 1, 5); /* Small Health Potion */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (1, 5, 1, 5); /* Small Mana Potion */
/* Lamb drops */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (2, 1, 10, 10); /* Gold */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (2, 2, 1, 5); /* Old Rags */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (2, 3, 1, 5); /* Stick */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (2, 4, 1, 5); /* Small Health Potion */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (2, 5, 1, 5); /* Small Mana Potion */
/* Fluffy Little Bunny drops */ 
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (3, 4, 1, 5); /* Small Health Potion */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (3, 5, 1, 5); /* Small Mana Potion */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (3, 7, 1, 5); /* Rabbit Fur */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (3, 8, 1, 5); /* Rabbit Pelt */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (3, 455, 1, 3); /* Leather Padding */
/* Rabid Rabbit drops */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (4, 4, 1, 5); /* Small Health Potion */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (4, 5, 1, 5); /* Small Mana Potion */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (4, 7, 1, 5); /* Rabbit Fur */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (4, 8, 1, 5); /* Rabbit Pelt */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (4, 6, 1, 3); /* Tin Can */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (4, 455, 1, 3); /* Leather Padding */
/* Cottontail drops */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (5, 7, 1, 5); /* Rabbit Fur */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (5, 8, 1, 5); /* Rabbit Pelt */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (5, 6, 1, 3); /* Tin Can */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (5, 9, 1, 2); /* Bunny ears */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (5, 455, 1, 10); /* Leather Padding */
/* Asp drops */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (6, 4, 1, 5); /* Small Health Potion */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (6, 5, 1, 5); /* Small Mana Potion */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (6, 11, 1, 3); /* Small Hammer */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (6, 12, 1, 3); /* Wooden Stave */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (6, 13, 1, 3); /* Small Dagger */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (6, 14, 1, 3); /* Small Sword */
/* Bat drops */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (7, 4, 1, 5); /* Small Health Potion */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (7, 5, 1, 5); /* Small Mana Potion */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (7, 15, 1, 3); /* Stone Hammer */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (7, 16, 1, 3); /* Hardwood Stave */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (7, 17, 1, 3); /* Grim Dagger */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (7, 18, 1, 3); /* Long Sword */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (7, 333, 1, 3); /* Garlic */
/* Cow drops */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (8, 19, 1, 5); /* Sun Flower */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (8, 20, 1, 10); /* Pile of Crap */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (8, 455, 1, 3); /* Leather Padding */
/* Weak Zombie drops */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (10, 4, 1, 5); /* Small Health Potion */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (10, 5, 1, 5); /* Small Mana Potion */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (10, 21, 1, 5); /* Rubber Ducky */
/* Skeleton drops */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (13, 80, 1, 5); /* Scroll: Dexterity 1 */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (13, 67, 1, 5); /* Scroll: Fortify 4 */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (13, 195, 1, 5); /* Scroll: Elemental Strike 8 */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (13, 196, 1, 5); /* Scroll: Arcane Shield 4 */
/* Servant of the Dead drops */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (14, 489, 1, 3); /* Potion of Restoration */
/* Boney */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (15, 225, 1, 7); /*  Boney Shoes*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (15, 228, 1, 7); /*  Boney Shoes*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (15, 233, 1, 7); /*  Boney Shoes*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (15, 237, 1, 7); /*  Boney Shoes*/
/* Misplaced Spouse */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (16, 213, 1, 5); /*  Scythe Misplaced spouse */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (16, 100, 1, 7); /*  Spouse hats */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (16, 230, 1, 7); /*  Spouse hats */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (16, 231, 1, 7); /*  Spouse hats */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (16, 235, 1, 7); /*  Spouse hats */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (16, 264, 1, 7); /* Misplaced spouse - Essence of fire*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (16, 268, 1, 7); /* Misplaced spouse - Present 1*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (16, 269, 1, 7); /* Misplaced spouse - Present 2*/
/* Record Keeper drops */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (17, 64, 1, 20); /* Scroll: Healing 5 */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (17, 77, 1, 15); /* Scroll: Stamina 4 */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (17, 72, 1, 10); /* Scroll: Strength 4 */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (17, 198, 1, 15); /* Scroll: Regeneration 3 */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (17, 197, 1, 15); /* Scroll: Elemental Strike 9 */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (17, 81, 1, 10); /* Scroll: Dexterity 2 */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (17, 224, 1, 7); /*  RK Leggings */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (17, 227, 1, 7); /*  RK Leggings */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (17, 232, 1, 7); /*  RK Leggings */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (17, 236, 1, 7); /*  RK Leggings */
/* Fire Asp */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (26, 214, 1, 4); /* Searing Whip */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (26, 355, 1, 2); /* Cats Hair */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (26, 623, 1, 4); /* Fire Asp - Purple Droplet */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (27, 623, 1, 4); /* Naga Warrior - Purple Droplet */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (28, 623, 1, 4); /* Naga Rogue - Purple Droplet */

/* Spook/Ghasts */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (56, 623, 1, 2); /* Spook - Red Droplet */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (56, 624, 1, 2); /* Spook - Blue Droplet */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (57, 623, 1, 2); /* Ghast - Red Droplet */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (57, 624, 1, 2); /* Ghast - Blue Droplet */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (56, 44, 1, 10); /* Large Health Potiion */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (56, 45, 1, 10); /* Large Mana Potion */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (57, 44, 1, 10); /* Large Health Potiion */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (57, 45, 1, 10); /* Large Mana Potion */

/* ImaBitStale drops */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (58, 212, 1, 5); /*  Ima - Priests hammer */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (58, 68, 1, 10); /* Scroll: Fortify 5 */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (58, 83, 1, 10); /* Scroll: Mana Regeneration 2 */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (58, 73, 1, 10); /* Scroll: Strength 5 */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (58, 200, 1, 10); /* Scroll: Arcane Shield 5 */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (58, 199, 1, 10); /* Scroll: Elemental Strike 10 */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (58, 259, 1, 5); /* ImaBitstale - Rusty Claw*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (58, 273, 1, 10); /* Scroll: Arcane Blast */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (58, 277, 1, 10); /* Scroll: Rejuvination */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (58, 420, 1, 7); /* Scroll: Spiritual Blessings */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (58, 64, 1, 10); /* Scroll: Healing 5 */
/* Ecto drops */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (59, 68, 1, 10); /* Scroll: Fortify 5 */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (59, 83, 1, 10); /* Scroll: Mana Regeneration 2 */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (59, 73, 1, 10); /* Scroll: Strength 5 */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (59, 200, 1, 10); /* Scroll: Arcane Shield 5 */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (59, 199, 1, 10); /* Scroll: Elemental Strike 10 */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (59, 226, 1, 7); /* Ecto CPs */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (59, 230, 1, 7); /* Ecto CPs */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (59, 234, 1, 7); /* Ecto CPs */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (59, 238, 1, 7); /* Ecto CPs */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (59, 270, 1, 5); /* Ecto - Present 3*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (59, 275, 1, 10); /* Scroll: Spirit Strike */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (59, 276, 1, 10); /* Scroll: Critical Strike */
/* Punchy drops */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (60, 68, 1, 10); /* Scroll: Fortify 5 */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (60, 83, 1, 10); /* Scroll: Mana Regeneration 2 */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (60, 73, 1, 10); /* Scroll: Strength 5 */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (60, 200, 1, 10); /* Scroll: Arcane Shield 5 */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (60, 199, 1, 10); /* Scroll: Elemental Strike 10 */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (60, 90, 1, 7); /* Punchy - Champs Helmet*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (60, 107, 1, 7); /* Punchy - Champs Legplates*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (60, 164, 1, 7); /* Punchy - Champs Boots*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (60, 149, 1, 7); /* Punchy - Champs CP*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (60, 93, 1, 7); /* Punchy - Deceivers Helmet*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (60, 165, 1, 7); /* Punchy - Deceivers Legs*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (60, 125, 1, 7); /* Punchy - Deceivers Boots*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (60, 168, 1, 7); /* Punchy - Deceivers CP*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (60, 239, 1, 7); /* Punchy - High priest legs*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (60, 240, 1, 7); /* Punchy - High priest shoes*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (60, 241, 1, 7); /* Punchy - High priest crown*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (60, 242, 1, 7); /* Punchy - High priest tunic*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (60, 243, 1, 7); /* Punchy - Elemental legs*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (60, 244, 1, 7); /* Punchy - Elemental shoes*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (60, 245, 1, 7); /* Punchy - Elemental tunic*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (60, 246, 1, 7); /* Punchy - Elemental crown*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (60, 258, 1, 6); /* Punchy - Bear claw*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (60, 253, 1, 6); /* Punchy - Champions blade*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (60, 205, 1, 6); /* Punchy - Deceivers dagger*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (60, 273, 1, 10); /* Scroll: Arcane Blast */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (60, 277, 1, 10); /* Scroll: Rejuvination */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (60, 275, 1, 10); /* Scroll: Spirit Strike */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (60, 276, 1, 10); /* Scroll: Critical Strike */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (60, 302, 1, 5); /* Scroll: Group Paradise Tele */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (60, 420, 1, 7); /* Scroll: Spiritual Blessings */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (60, 266, 1, 10); /* AD Key*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (60, 58, 1, 5); /* GOP - Punchy */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (60, 622, 2, 2); /* magical liquid - Punchy */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (60, 64, 1, 10); /* Scroll: Healing 5 */
/* Savage drops */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (61, 343, 1, 5); /* Pearl */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (61, 457, 1, 5); /* Red Rope */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (61, 444, 1, 5); /* Sketch */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (61, 445, 1, 5); /* Soft Gold Ore */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (61, 623, 1, 2); /* Rabid - Red Droplet */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (61, 624, 1, 2); /* Rabid - Blue Droplet */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (61, 626, 1, 2); /* Rabid - Green Droplet */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (61, 627, 1, 2); /* Rabid - Orange Droplet */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (62, 343, 1, 5); /* Pearl */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (62, 457, 1, 5); /* Red Rope */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (62, 444, 1, 5); /* Sketch */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (62, 445, 1, 5); /* Soft Gold Ore */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (62, 623, 1, 3); /* Hungry - Red Droplet */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (62, 624, 1, 3); /* Hungry - Blue Droplet */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (62, 626, 1, 3); /* Hungry - Green Droplet */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (62, 627, 1, 3); /* Hungry - Orange Droplet */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (63, 343, 1, 5); /* Pearl */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (63, 457, 1, 5); /* Red Rope */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (63, 444, 1, 5); /* Sketch */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (63, 445, 1, 5); /* Soft Gold Ore */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (63, 623, 1, 2); /* Savage - Red Droplet */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (63, 624, 1, 2); /* Savage - Blue Droplet */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (64, 343, 1, 5); /* Pearl */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (64, 457, 1, 5); /* Red Rope */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (64, 444, 1, 5); /* Sketch */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (64, 445, 1, 5); /* Soft Gold Ore */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (64, 623, 1, 3); /* Paranoid - Red Droplet */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (64, 624, 1, 3); /* Paranoid - Blue Droplet */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (64, 626, 1, 3); /* Paranoid - Green Droplet */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (64, 627, 1, 3); /* Paranoid - Orange Droplet */
/* Piglet drops */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (18, 42, 1, 10); /* Health Potion */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (18, 43, 1, 10); /* Mana Potion */
/* Flying piglet drops */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (19, 160, 1, 6); /* Thick skin of the boar*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (19, 1, 5000, 10); /* Gold (5000) */
/* Lost Wabbit drops */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (20, 4, 1, 10); /* Small Health Potion */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (20, 5, 1, 10); /* Small Mana Potion */
/* Cute Defenseless Stalker drops */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (21, 4, 1, 10); /* Small Health Potion */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (21, 5, 1, 10); /* Small Mana Potion */
/* Roger drops */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (22, 46, 1, 6); /* Frozen Mittens */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (22, 47, 1, 6); /* Cloak of chilling speed */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (22, 265, 1, 5); /* Roger - Essence of Air*/
/* Leafeater drops */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (23, 41, 1, 4); /* Winter Blade */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (23, 48, 1, 4); /* Blizzard Robes */
/* Pipsqueek drops */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (24, 4, 1, 10); /* Small Health Potion */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (24, 5, 1, 10); /* Small Mana Potion */
/* Flying pipsqueek drops */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (25, 1, 2500, 10); /* Gold (2500) */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (25, 142, 1, 10); /* Leather Gloves */
/* Weak Persecution drops */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (34, 343, 1, 3); /* Pearl */
/* Moldy Persecution drops */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (35, 343, 1, 3); /* Pearl */
/* Strong Persecution */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (38, 354, 1, 3); /* Unrefined Ore */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (49, 354, 1, 3); /* Unrefined Ore */
/* Persecution */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (39, 279, 1, 3); /* Teleport: Minita */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (39, 280, 1, 3); /* Teleport: Bound */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (39, 281, 1, 3); /* Teleport: Otherlands */
/* Slime drops */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (40, 42, 1, 5); /* Potion */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (40, 43, 1, 5); /* Potion */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (40, 44, 1, 5); /* Potion */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (40, 45, 1, 5); /* Potion */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (41, 42, 1, 5); /* Potion */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (41, 43, 1, 5); /* Potion */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (41, 44, 1, 5); /* Potion */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (41, 45, 1, 5); /* Potion */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (42, 42, 1, 5); /* Potion */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (42, 43, 1, 5); /* Potion */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (42, 44, 1, 5); /* Potion */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (42, 45, 1, 5); /* Potion */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (43, 42, 1, 5); /* Potion */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (43, 43, 1, 5); /* Potion */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (43, 44, 1, 5); /* Potion */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (43, 45, 1, 5); /* Potion */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (44, 42, 1, 5); /* Potion */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (44, 43, 1, 5); /* Potion */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (44, 44, 1, 5); /* Potion */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (44, 45, 1, 5); /* Potion */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (45, 42, 1, 5); /* Potion */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (45, 43, 1, 5); /* Potion */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (45, 44, 1, 5); /* Potion */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (45, 45, 1, 5); /* Potion */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (40, 330, 1, 5); /* Flint */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (41, 330, 1, 5); /* Flint */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (42, 330, 1, 5); /* Flint */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (43, 330, 1, 5); /* Flint */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (44, 330, 1, 5); /* Flint */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (45, 330, 1, 5); /* Flint */
/* Goo Jr drops */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (46, 45, 10, 10); /* Mana Potions */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (46, 50, 1, 15); /* Cow skull helmet */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (46, 58, 1, 2); /* GOP */
/* Sliminator drops */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (47, 45, 10, 10); /* Mana Potions */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (47, 49, 1, 15); /* Hair bow */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (47, 51, 1, 15); /* Dollar bill */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (47, 263, 1, 5); /* Sliminator - Essence of water*/
/* King Goo drops */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (48, 52, 1, 10); /* Shirt #297741384 */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (48, 53, 1, 15); /* Chicken leg */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (48, 54, 1, 20); /* Taco */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (48, 50, 1, 15); /* Cow skull helmet */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (48, 51, 1, 15); /* Dollar bill */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (48, 58, 1, 4); /* GOP */
/* Nagan Beast */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (50, 354, 1, 3); /* Unrefined Ore */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (50, 623, 1, 2); /* Beast - Red Droplet */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (50, 624, 1, 2); /* Beast - Blue Droplet */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (51, 623, 1, 2); /* Magus - Red Droplet */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (51, 624, 1, 2); /* Magus - Blue Droplet */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (52, 623, 1, 2); /* Priest - Red Droplet */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (52, 624, 1, 2); /* Priest - Blue Droplet */
/* Frozen Waste drops */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (29, 60, 1, 3); /* Iceman, Frozen Waste - Ruby */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (30, 60, 1, 3); /* Iceman, Frozen Waste - Ruby */
/* Iceman drops */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (31, 60, 1, 3); /* Iceman, Frozen Waste - Ruby */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (32, 60, 1, 3); /* Iceman, Frozen Waste - Ruby */
/* Frosty drops */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (33, 55, 1, 10); /* Encased Blade of wet*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (33, 56, 1, 10); /* Encased Claw of wet*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (33, 57, 1, 10); /* Staff of frozen rain*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (33, 58, 1, 5); /* Gem of Power*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (33, 59, 1, 1); /* Snowman illusion scroll*/
/* Udyana drops */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (53, 250, 1, 7); /* Nagan robes */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (53, 312, 1, 7); /* Nagan Armor */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (53, 1, 10000, 10); /* Gold (10000) */
/* Beef drops */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (65, 115, 1, 15); /* Beef - Moon shield */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (65, 135, 1, 5); /* Beef - Protection */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (65, 136, 1, 5); /* Beef - Immortality */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (65, 261, 1, 5); /* Beef - Beefs Fist */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (65, 271, 1, 2); /* Beef - Present 4 */

/* Savage bosses drops */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (66, 247, 1, 10); /* Copycat - Whirling Leggings */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (66, 248, 1, 10); /* Copycat - Whirling Shoes */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (66, 249, 1, 10); /* Copycat - Whirling Hat */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (66, 252, 1, 10); /* Copycat - DDTF */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (66, 162, 1, 10); /* Whirling robes*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (66, 146, 1, 10); /* Dragon belt*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (66, 216, 1, 10); /* Dev staff */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (66, 622, 5, 10); /* Magical Liquid */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (67, 95, 1, 10); /* Gold Helmet*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (67, 109, 1, 10); /* Gold Leggings*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (67, 127, 1, 10); /* Gold Boots*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (67, 152, 1, 10); /* Gold Chestplate*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (67, 167, 1, 10); /* Dev Chestplate*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (67, 206, 1, 10); /* DDTS*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (67, 622, 5, 10); /* Magical Liquid */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (68, 94, 1, 10); /* Dev Helmet */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (68, 166, 1, 10); /* Dev Leggings */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (68, 126, 1, 10); /* Dev Boots */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (68, 151, 1, 10); /* Dev Robes */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (68, 422, 1, 10); /* Savage Pauldrons of the Boar */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (68, 423, 1, 10); /* Savage Pauldrons of the Cow */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (68, 424, 1, 10); /* Red Ring */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (68, 425, 1, 10); /* Black Ring */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (68, 622, 5, 10); /* Magical Liquid */
/* LFB */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (71, 266, 1, 20); /* LFB -  AD Key */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (71, 217, 1, 10); /* LFB -  Tiny club */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (71, 115, 1, 10); /* LFB - Moon shield */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (71, 58, 1, 5); /* GOP - LFB */
/* Hay */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (72, 266, 1, 20); /* Hay -  AD Key*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (72, 257, 1, 5); /* Hay -  Hays claw*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (72, 96, 1, 5); /* Hay -  Hays tail*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (72, 86, 1, 15); /* Hay -  FL Rune */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (72, 87, 1, 15); /* Hay -  SoS Rune */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (72, 274, 1, 15); /* Hay AA Rune */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (72, 278, 1, 15); /* Hay RH Rune */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (72, 302, 1, 5); /* Scroll: Group Paradise Tele */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (72, 420, 1, 7); /* Scroll: Spiritual Blessings */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (72, 58, 1, 5); /* GOP - Hay */
/* Fray */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (73, 266, 1, 20); /* Fray -  AD Key*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (73, 260, 1, 5); /* Fray - Frays Flippers  */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (73, 218, 1, 5); /* Frays cane */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (73, 274, 1, 15); /* AA Rune */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (73, 278, 1, 15); /* RH Rune */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (73, 86, 1, 15); /* Fray -  FL Rune */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (73, 87, 1, 15); /* Fray -  SoS Rune */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (73, 302, 1, 5); /* Scroll: Group Paradise Tele */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (73, 420, 1, 7); /* Scroll: Spiritual Blessings */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (73, 58, 1, 5); /* GOP - Fray */
/* Elite Ancient Warrior drops */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (76, 313, 1, 7); /* Scroll: Ancient Healing */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (76, 315, 1, 5); /* Scroll: Ancient Sturdiness */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (76, 316, 1, 5); /* Scroll: Ancient Criticality */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (76, 321, 1, 5); /* Scroll: Ancient Taunt */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (76, 318, 1, 5); /* Scroll: Ancient Protection */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (76, 314, 1, 5); /* Scroll: Ancient Root */
/* Comissioned Blacksmith drops */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (77, 439, 1, 20); /* Priceless Ore */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (77, 440, 1, 20); /* Priceless Chisel */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (77, 441, 1, 20); /* Priceless Hammer */
/* Comissioned Tailor drops */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (78, 436, 1, 20); /* Priceless Needle */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (78, 437, 1, 20); /* Priceless Pattern */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (78, 438, 1, 20); /* Priceless Thread */
/* Dalph Von Ownu drops */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (79, 291, 1, 20); /* Ancient Axe */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (79, 292, 1, 20); /* Ancient Dagger */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (79, 282, 1, 20); /* Ancient Moon Wand */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (79, 303, 1, 10); /* Design: Ancient Shield */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (79, 442, 1, 5); /* Wrapping Paper */
/* Hairy Fairy Princess drops */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (80, 283, 1, 20); /* Ancient Garb */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (80, 284, 1, 20); /* Ancient Robe */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (80, 290, 1, 20); /* Ancient Armor */
/* Lady Slither drops */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (81, 285, 1, 10); /* Snake Tiara */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (81, 297, 1, 20); /* Essence of Earth */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (81, 298, 1, 20); /* Essence of Fire */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (81, 299, 1, 20); /* Essence of Air */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (81, 300, 1, 20); /* Essence of Water */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (81, 442, 1, 5); /* Wrapping Paper */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (81, 512, 1, 20); /* Broken Key */
/* Sir Insideous drops */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (82, 286, 1, 10); /* Snake Helm */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (82, 304, 1, 20); /* Bracelet of Fire */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (82, 305, 1, 20); /* Bracelet of Earth */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (82, 306, 1, 20); /* Bracelet of Air */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (82, 307, 1, 20); /* Bracelet of Water */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (82, 308, 1, 20); /* Bracelet of Spirit */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (82, 442, 1, 5); /* Wrapping Paper */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (82, 512, 1, 20); /* Broken Key */
/* Ztwel Tahp drops */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (83, 287, 1, 10); /* Cloak of Power */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (83, 288, 1, 10); /* Pauldrons of Power */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (83, 289, 1, 10); /* Belt of Power */
/* Nagan Sentry drops */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (109, 219, 1, 4); /* Nagan weapon */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (109, 220, 1, 4); /* Nagan weapon */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (109, 221, 1, 4); /* Nagan weapon */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (109, 58, 1, 3); /* GOP Sentry */

INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (110, 254, 1, 10); /* Carrots  - Cold beaten sleeves*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (110, 256, 1, 10); /* Carrots  - Shirt of the fallen loser*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (111, 255, 1, 10); /* Fluffzilla - Spiked belt  */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (111, 262, 1, 10); /* Fluffzilla - Earth essence */

INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (53, 267, 1, 4); /* Coral - Udyana */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (53, 58, 1, 4); /* GOP - Udyana */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (37, 267, 1, 2); /* Coral - Sergeant Persecution */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (54, 267, 1, 4); /* Coral - Complex */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (54, 58, 1, 4); /* GOP - Complex */

INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (115, 202, 1, 2); /* Brainless - Bastard Sword */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (115, 203, 1, 2); /* Brainless - Brilliant Hammer */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (115, 211, 1, 2); /* Brainless - Malignant dagger */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (115, 215, 1, 2); /* Brainless - Thicket Stave*/

INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (116, 137, 1, 5); /* Nibbles - Val brace */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (116, 144, 1, 5); /* Nibbles - Val ring */
/* Nibbles II drops */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (118, 373, 1, 5); /* Nibbles II - Valiant Helmet */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (118, 374, 1, 5); /* Nibbles II - Valiant Chestplate */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (118, 375, 1, 5); /* Nibbles II - Valiant Legplates*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (118, 376, 1, 5); /* Nibbles II - Valiant Boots */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (118, 377, 1, 5); /* Nibbles II - Valiant Cap */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (118, 378, 1, 5); /* Nibbles II - Valiant Robes */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (118, 379, 1, 5); /* Nibbles II - Valiant Mesh */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (118, 380, 1, 5); /* Nibbles II - Valiant Stealth */
/* Cranky Ewe drops */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (135, 104, 1, 8); /* Cranky Ewe - True Ewe */
/* Frantic Monkey drops */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (136, 132, 1, 8); /* Frantic Monkey -  Poo Slippers */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (136, 140, 1, 8); /* Frantic Monkey -  Poo Gloves */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (136, 161, 1, 8); /* Frantic Monkey -  Poo Tunic */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (136, 163, 1, 8); /* Frantic Monkey -  Poo Leggings */

/* Ancient Defender drops */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (123, 319, 1, 5); /* Scroll: Ancient Buffiness */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (123, 413, 1, 5); /* Scroll: Ancient Bellow */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (123, 473, 1, 5); /* Scroll: Ancient Group Healing */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (123, 474, 1, 5); /* Scroll: Ancient Group Damage */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (123, 421, 1, 5); /* Teleport: Ancients Dungeon */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (123, 58, 1, 2); /* Gem of Power */
/* Ancient Guard drops */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (124, 414, 1, 5); /* Rune: Ancient Awe */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (143, 414, 1, 5); /* Rune: Ancient Awe */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (124, 415, 1, 5); /* Rune: Ancient Conflagration */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (143, 415, 1, 5); /* Rune: Ancient Conflagration */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (124, 416, 1, 5); /* Rune: Ancient Death */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (143, 416, 1, 5); /* Rune: Ancient Death */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (124, 419, 1, 5); /* Rune: Ancient Blessings */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (143, 419, 1, 5); /* Rune: Ancient Blessings */
/* General Mehoff drops */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (126, 397, 1, 10); /* Design: Divine Crown */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (126, 398, 1, 10); /* Mold: Divine Helm */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (126, 514, 1, 20); /* Broken Key */
/* Ancient Sentinel drops */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (138, 395, 1, 5); /* Design: Ancient Slippers */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (138, 396, 1, 5); /* Mold: Ancient Boots */
/* General Vor Chez drops */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (139, 399, 1, 10); /* Dagger of Contemption */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (139, 400, 1, 10); /* Ward of Destruction */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (139, 514, 1, 20); /* Broken Key */
/* Doom drops */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (140, 417, 1, 8); /* Doom Helm */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (140, 394, 1, 8); /* Doom Robe */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (140, 513, 1, 20); /* Broken Key */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (140, 513, 1, 20); /* Broken Key */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (140, 566, 1, 50); /* Ancient Healing 2 */
/* Disciple Von Bangs */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (141, 409, 1, 20); /* Laurels */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (141, 412, 1, 20); /* Sandals */
/* Disciple Von Chief */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (142, 410, 1, 20); /* Frilly Top */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (142, 411, 1, 20); /* Frilly Skirt */

/* Sorrows hero */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (132, 459, 1, 20); /* Shard of Earth*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (132, 459, 1, 20); /* Shard of Earth*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (132, 459, 1, 20); /* Shard of Earth*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (132, 463, 1, 20); /* Shard of Death*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (132, 463, 1, 20); /* Shard of Death*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (132, 463, 1, 20); /* Shard of Death*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (132, 464, 1, 20); /* Shard of Strength*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (132, 464, 1, 20); /* Shard of Strength*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (132, 464, 1, 20); /* Shard of Strength*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (132, 465, 1, 20); /* Shard of Love*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (132, 465, 1, 20); /* Shard of Love*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (132, 465, 1, 20); /* Shard of Love*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (132, 466, 1, 20); /* Shard of Life*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (132, 466, 1, 20); /* Shard of Life*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (132, 466, 1, 20); /* Shard of Life*/
/* Sorrows Lover */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (137, 459, 1, 25); /* Shard of Earth*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (137, 459, 1, 25); /* Shard of Earth*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (137, 459, 1, 25); /* Shard of Earth*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (137, 463, 1, 25); /* Shard of Death*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (137, 463, 1, 25); /* Shard of Death*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (137, 463, 1, 25); /* Shard of Death*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (137, 464, 1, 25); /* Shard of Strength*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (137, 464, 1, 25); /* Shard of Strength*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (137, 464, 1, 25); /* Shard of Strength*/
/* Invisible 1 */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (147, 461, 1, 35); /* Shard of Fire*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (147, 461, 1, 35); /* Shard of Fire*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (147, 462, 1, 35); /* Shard of Air*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (147, 462, 1, 35); /* Shard of Air*/
/* Invisible 2 */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (144, 461, 1, 25); /* Shard of Fire*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (144, 461, 1, 25); /* Shard of Fire*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (144, 462, 1, 25); /* Shard of Air*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (144, 462, 1, 25); /* Shard of Air*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (144, 468, 1, 25); /* Shard of Divinity*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (144, 468, 1, 25); /* Shard of Divinity*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (144, 467, 1, 25); /* Shard of Hope*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (144, 467, 1, 25); /* Shard of Hope*/
/* Abomination */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (152, 470, 1, 30); /* Shard of Protection*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (152, 470, 1, 30); /* Shard of Protection*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (152, 470, 1, 30); /* Shard of Protection*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (152, 460, 1, 30); /* Shard of Water*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (152, 460, 1, 30); /* Shard of Water*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (152, 460, 1, 30); /* Shard of Water*/
/* Charlie */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (154, 469, 1, 40); /* Shard of Power*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (154, 470, 1, 40); /* Shard of Protection*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (154, 471, 1, 40); /* Shard of Invincibility*/
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (154, 460, 1, 40); /* Shard of Water*/

/* Berry stealer */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (127, 448, 1, 15); /* Berrys Hair Strand */
/* Patrol bear */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (129, 431, 1, 5); /* Scroll: Sacrifice II */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (129, 433, 1, 5); /* Scroll: Critical Blow of the bear */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (129, 434, 1, 5); /* Scroll: Roar of the Bear */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (129, 432, 1, 5); /* Scroll: Damage of the bear */
/* Gramps */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (157, 446, 1, 7); /* Gramps Fur */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (157, 447, 1, 8); /* Blood */
/* Mama Bear */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (156, 427, 1, 12); /* Mama CP */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (156, 428, 1, 12); /* Mama Headband */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (156, 429, 1, 12); /* Mama Legplates */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (156, 430, 1, 12); /* Mama Boots */
/* The Patriarch */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (155, 426, 1, 12); /* Gero robes */
/* Ancient Royal */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (161, 517, 1, 8); /* Rune: Knights Blessing */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (161, 518, 1, 8); /* Rune: Wizards Curse */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (161, 519, 1, 8); /* Rune: Mischiefs Craft */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (161, 520, 1, 8); /* Rune: Clerics Blessing */
/* King Terror */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (162, 515, 1, 20); /* Broken Key */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (162, 515, 1, 20); /* Broken Key */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (162, 511, 1, 20); /* Terror Necklace */
/* Prince Punisher */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (163, 297, 1, 20); /* Essence of Earth */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (163, 298, 1, 20); /* Essence of Fire */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (163, 299, 1, 20); /* Essence of Air */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (163, 300, 1, 20); /* Essence of Water */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (163, 297, 1, 20); /* Essence of Earth */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (163, 298, 1, 20); /* Essence of Fire */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (163, 299, 1, 20); /* Essence of Air */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (163, 300, 1, 20); /* Essence of Water */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (163, 490, 1, 10); /* Design: Royal Leggings */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (163, 491, 1, 10); /* Mold: Royal Legplates */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (163, 492, 1, 10); /* Design: Royal Tunic */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (163, 493, 1, 10); /* Mold: Royal Chestplate */
/* Queen Butcher */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (164, 502, 1, 20); /* Mischiefs Claw */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (164, 503, 1, 20); /* Mischiefs Shield */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (164, 504, 1, 20); /* Wizards Staff */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (164, 505, 1, 20); /* Knights Sword */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (164, 506, 1, 20); /* Clerics Testament */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (164, 507, 1, 20); /* Clerics Ward */
/* Princess Slayer */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (165, 508, 1, 20); /* Slayer Armguards */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (165, 509, 1, 20); /* Slayer Belt */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (165, 510, 1, 20); /* Slayer Gloves */

/* Ancient Prisoner */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (175, 593, 1, 1); /* Scroll: Paradise Teleport */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (175, 594, 1, 2); /* Scroll: Ancient Covenant */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (175, 595, 1, 2); /* Scroll: Ancient Sacrifice 2 */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (175, 597, 1, 2); /* Scroll: Ancient Taunt 2 */

/* Ancient Prison Superior */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (176, 593, 1, 4); /* Scroll: Paradise Teleport */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (176, 594, 1, 25); /* Scroll: Ancient Covenant */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (176, 595, 1, 25); /* Scroll: Ancient Sacrifice 2 */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (176, 597, 1, 25); /* Scroll: Ancient Taunt 2 */

INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (177, 591, 1, 7); /* Howto: Helm Enchantment */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (177, 591, 1, 7); /* Howto: Helm Enchantment */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (177, 591, 1, 7); /* Howto: Helm Enchantment */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (177, 592, 1, 5); /* Scroll: ad5 teleport */

INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (178, 586, 1, 25); /* Prison Gloves */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (178, 592, 1, 5); /* Scroll: ad5 teleport */

INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (179, 585, 1, 8); /* Howto: Bracelet Enchantment */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (179, 585, 1, 8); /* Howto: Bracelet Enchantment */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (179, 585, 1, 8); /* Howto: Bracelet Enchantment */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (179, 585, 1, 8); /* Howto: Bracelet Enchantment */
INSERT INTO npc_drops (npc_template_id, item_template_id, stack, droprate) VALUES (179, 592, 1, 5); /* Scroll: ad5 teleport */


DROP TABLE npc_vendor_items;
CREATE TABLE npc_vendor_items (
  npc_template_id INT NOT NULL,
  item_template_id INT NOT NULL,
  stack INT DEFAULT 1 NOT NULL,
  stats_visible CHAR(1) DEFAULT '1' NOT NULL,
  slot INT NOT NULL
);

/* Hair Dye Guy vendor */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (84, 88, 1, '1', 1);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (84, 169, 1, '1', 2);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (84, 170, 1, '1', 3);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (84, 171, 1, '1', 4);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (84, 546, 1, '1', 5);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (84, 547, 1, '1', 6);
/* Hair changers vendor */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (85, 172, 1, '1', 1);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (86, 173, 1, '1', 1);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (87, 174, 1, '1', 1);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (88, 175, 1, '1', 1);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (89, 176, 1, '1', 1);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (90, 177, 1, '1', 1);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (91, 178, 1, '1', 1);
/* Face changers vendor */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (92, 179, 1, '1', 1);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (93, 180, 1, '1', 1);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (94, 181, 1, '1', 1);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (95, 182, 1, '1', 1);
/* Sex changers vendor */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (96, 183, 1, '1', 1);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (97, 184, 1, '1', 1);
/* Magus Trainer vendor */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (98, 25, 1, '1', 1);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (98, 26, 1, '1', 2);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (98, 27, 1, '1', 3);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (98, 28, 1, '1', 4);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (98, 29, 1, '1', 5);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (98, 186, 1, '1', 6);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (98, 34, 1, '1', 7);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (98, 30, 1, '1', 8);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (98, 31, 1, '1', 9);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (98, 32, 1, '1', 10);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (98, 185, 1, '1', 11);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (98, 33, 1, '1', 12);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (98, 36, 1, '1', 13);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (98, 35, 1, '1', 14);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (98, 37, 1, '1', 15);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (98, 38, 1, '1', 16);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (98, 39, 1, '1', 17);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (98, 201, 1, '1', 18);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (98, 187, 1, '1', 19);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (98, 188, 1, '1', 20);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (98, 189, 1, '1', 21);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (98, 190, 1, '1', 22);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (98, 191, 1, '1', 23);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (98, 192, 1, '1', 24);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (98, 193, 1, '1', 25);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (98, 194, 1, '1', 26);
/* Priest Trainer vendor */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (99, 10, 1, '1', 1);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (99, 22, 1, '1', 2);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (99, 69, 1, '1', 3);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (99, 61, 1, '1', 4);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (99, 74, 1, '1', 5);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (99, 34, 1, '1', 6);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (99, 31, 1, '1', 7);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (99, 65, 1, '1', 8);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (99, 84, 1, '1', 9);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (99, 70, 1, '1', 10);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (99, 62, 1, '1', 11);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (99, 75, 1, '1', 12);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (99, 37, 1, '1', 13);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (99, 78, 1, '1', 14);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (99, 66, 1, '1', 15);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (99, 71, 1, '1', 16);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (99, 63, 1, '1', 17);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (99, 76, 1, '1', 18);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (99, 82, 1, '1', 19);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (99, 79, 1, '1', 20);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (99, 85, 1, '1', 21);
/* Potion vendor */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (100, 4, 1, '1', 1);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (100, 5, 1, '1', 2);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (100, 42, 1, '1', 6);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (100, 43, 1, '1', 7);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (100, 44, 1, '1', 11);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (100, 45, 1, '1', 12);
/* Bronze Armor vendor */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (101, 89, 1, '1', 1);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (101, 148, 1, '1', 2);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (101, 106, 1, '1', 3);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (101, 123, 1, '1', 4);
/* Iron Armor vendor */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (102, 97, 1, '1', 1);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (102, 154, 1, '1', 2);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (102, 110, 1, '1', 3);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (102, 128, 1, '1', 4);
/* Steel Armor vendor */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (103, 103, 1, '1', 1);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (103, 159, 1, '1', 2);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (103, 114, 1, '1', 3);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (103, 133, 1, '1', 4);
/* Shield vendor */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (104, 122, 1, '1', 1);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (104, 121, 1, '1', 6);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (104, 222, 1, '1', 7);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (104, 309, 1, '1', 8);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (104, 116, 1, '1', 11);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (104, 117, 1, '1', 12);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (104, 310, 1, '1', 13);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (104, 120, 1, '1', 16);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (104, 119, 1, '1', 17);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (104, 311, 1, '1', 18);
/* Weapon vendor */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (105, 3, 1, '1', 1);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (105, 14, 1, '1', 2);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (105, 18, 1, '1', 7);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (105, 202, 1, '1', 12);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (105, 13, 1, '1', 3);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (105, 17, 1, '1', 8);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (105, 211, 1, '1', 13);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (105, 12, 1, '1', 4);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (105, 16, 1, '1', 9);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (105, 215, 1, '1', 14);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (105, 11, 1, '1', 5);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (105, 15, 1, '1', 10);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (105, 203, 1, '1', 15);
/* Cloth Armor vendor */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (106, 91, 1, '1', 1);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (106, 150, 1, '1', 2);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (106, 108, 1, '1', 3);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (106, 124, 1, '1', 4);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (106, 2, 1, '1', 6);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (106, 223, 1, '1', 7);
/* Leather Armor vendor */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (107, 98, 1, '1', 1);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (107, 155, 1, '1', 2);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (107, 111, 1, '1', 3);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (107, 129, 1, '1', 4);
/* Silk Armor vendor */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (108, 101, 1, '1', 1);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (108, 158, 1, '1', 2);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (108, 113, 1, '1', 3);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (108, 131, 1, '1', 4);
/* Teleporter Vendor Guy vendor */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (119, 279, 1, '1', 1);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (119, 280, 1, '1', 2);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (119, 281, 1, '1', 3);
/* Candy Necklace Vendor */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (120, 138, 1, '1', 1);
/* Paradise Teleporter Vendor Guy vendor */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (121, 279, 1, '1', 1);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (121, 280, 1, '1', 2);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (121, 281, 1, '1', 3);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (121, 301, 1, '1', 4);
/* The Bat Man */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (122, 365, 1, '1', 1);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (122, 366, 1, '1', 2);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (122, 367, 1, '1', 3);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (122, 368, 1, '1', 4);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (122, 369, 1, '1', 5);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (122, 370, 1, '1', 6);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (122, 371, 1, '1', 7);
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (122, 372, 1, '1', 8);
/* Tailoring Supplies */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (148, 340, 1, '1', 1); /* Needle */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (148, 345, 1, '1', 2); /* Sharp Scissors */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (148, 346, 1, '1', 3); /* Shirt Pattern */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (148, 347, 1, '1', 4); /* Spool of Thread */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (148, 348, 1, '1', 5); /* Spool of coloured Thread */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (148, 349, 1, '1', 6); /* Spool of coloured Thread */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (148, 350, 1, '1', 7); /* Spool of coloured Thread */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (148, 351, 1, '1', 8); /* Spool of coloured Thread */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (148, 352, 1, '1', 9); /* Spool of coloured Thread */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (148, 353, 1, '1', 10); /* Spool of coloured Thread */
/* Smithing Supplies */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (149, 332, 1, '1', 1); /* Chisel */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (149, 334, 1, '1', 2); /* High Quality Blade */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (149, 335, 1, '1', 3); /* High Quality Hilt */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (149, 336, 1, '1', 4); /* Medium Quality Blade */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (149, 337, 1, '1', 5); /* Medium Quality Hilt */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (149, 338, 1, '1', 6); /* Low Quality Blade */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (149, 339, 1, '1', 7); /* Low Quality Hilt */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (149, 342, 1, '1', 8); /* Liquid Ore */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (149, 344, 1, '1', 9); /* Rope */
/* Scriber */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (150, 341, 1, '1', 1); /* Ink */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (150, 328, 1, '1', 2); /* Blank Scroll */
/* Level 25 Wandering Scribe */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (159, 481, 1, '1', 1); /* Scroll: Augment */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (159, 482, 1, '1', 2); /* Scroll: Empower */ 
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (159, 483, 1, '1', 3); /* Scroll: Bustle */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (159, 484, 1, '1', 4); /* Scroll: Aggravate */
/* Level 35 Wandering Scribe */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (160, 485, 1, '1', 1); /* Scroll: Meditate */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (160, 486, 1, '1', 2); /* Scroll: Bulk */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (160, 487, 1, '1', 3); /* Scroll: Tumble */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (160, 488, 1, '1', 4); /* Scroll: Forge */
/* Phat Lewtz */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (170, 210, 1, '1', 1); /* Lucky Staff */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (170, 156, 1, '1', 2); /* Lucky Robe */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (170, 112, 1, '1', 3); /* Lucky Legs */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (170, 130, 1, '1', 4); /* Lucky Boots */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (170, 157, 1, '1', 5); /* Lucky CP */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (170, 209, 1, '1', 6); /* Lucky Spear */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (170, 389, 1, '1', 7); /* Lucky Dagger */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (170, 478, 1, '1', 8); /* Scratch */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (170, 476, 1, '1', 9); /* Star Shield */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (170, 477, 1, '1', 10); /* Sanguine Chaos */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (170, 105, 1, '1', 11); /* Wolfs Essence */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (170, 383, 1, '1', 12); /* Battle Gown */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (170, 139, 1, '1', 13); /* Divine Pauldrons */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (170, 381, 1, '1', 14); /* Celestial Pauldrons */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (170, 99, 1, '1', 15); /* Lucky Laurels */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (170, 384, 1, '1', 16); /* Lucky Belt */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (170, 143, 1, '1', 17); /* Lucky Necklace */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (170, 118, 1, '1', 18); /*  FireBreather Shield */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (170, 527, 1, '1', 19); /* First Aid*/
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (170, 528, 1, '1', 20); /* Recovery */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (170, 529, 1, '1', 21); /* Clobber */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (170, 530, 1, '1', 22); /* Pummel */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (170, 382, 1, '1', 23); /* Shades */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (170, 616, 1, '1', 24); /* Pet Bait */
/* Credit Exchange */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (180, 541, 1, '1', 1); /* Pet Bait */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (180, 209, 1, '1', 2); /* Lucky Spear */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (180, 389, 1, '1', 3); /* Lucky Dagger */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (180, 478, 1, '1', 4); /* Scratch */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (180, 476, 1, '1', 5); /* Star Shield */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (180, 477, 1, '1', 6); /* Sanguine Chaos */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (180, 105, 1, '1', 7); /* Wolfs Essence */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (180, 383, 1, '1', 8); /* Battle Gown */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (180, 139, 1, '1', 9); /* Divine Pauldrons */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (180, 381, 1, '1', 10); /* Celestial Pauldrons */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (180, 99, 1, '1', 11); /* Lucky Laurels */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (180, 384, 1, '1', 12); /* Lucky Belt */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (180, 143, 1, '1', 13); /* Lucky Necklace */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (180, 118, 1, '1', 14); /*  FireBreather Shield */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (180, 523, 1, '1', 15); /* First Aid*/
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (180, 524, 1, '1', 16); /* Recovery */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (180, 525, 1, '1', 17); /* Clobber */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (180, 526, 1, '1', 18); /* Pummel */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (180, 382, 1, '1', 19); /* Shades */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (180, 531, 1, '1', 20); /* Scroll: Tame */
/* Pet Trainer */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (181, 532, 1, '1', 1); /* Pet Attack */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (181, 533, 1, '1', 2); /* Pet Defend */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (181, 534, 1, '1', 3); /* Pet Recall */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (181, 535, 1, '1', 4); /* Pet Follow */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (181, 536, 1, '1', 5); /* Pet Neutral */

/* Alchemy guy */
INSERT INTO npc_vendor_items (npc_template_id, item_template_id, stack, stats_visible, slot) VALUES (182, 621, 1, '1', 1); /* Pet Neutral */