DROP TABLE IF EXISTS npc_templates;
CREATE TABLE npc_templates (
  npc_id INTEGER PRIMARY KEY,
  npc_type SMALLINT DEFAULT 2 NOT NULL,
  npc_name TEXT NOT NULL,
  npc_title TEXT DEFAULT ' ' NOT NULL,
  npc_surname TEXT DEFAULT ' ' NOT NULL,
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

/*
INSERT INTO npc_templates (npc_id, npc_type, npc_name, respawn_time, experience, aggro_range, attack_range, attack_speed, move_speed, stunnable, rootable, slowable, npc_hp, class_id, body_id, weapon_damage)
VALUES (1, 2, 'Lamb', 40, 40, 0, 1, 1.5, 2, 1, 1, 1, 50, 4, 104, 6);
*/

DROP TABLE IF EXISTS npc_spawns;
CREATE TABLE npc_spawns (
  npc_id INT NOT NULL,
  map_id SMALLINT NOT NULL,
  map_x SMALLINT NOT NULL,
  map_y SMALLINT NOT NULL
);


DROP TABLE IF EXISTS npc_drops;
CREATE TABLE npc_drops (
  npc_template_id INT NOT NULL,
  item_template_id INT NOT NULL,
  stack INT NOT NULL,
  droprate DECIMAL(9,4) NOT NULL
);

CREATE INDEX npc_drops_npc_template_id_idx ON npc_drops(npc_template_id);


DROP TABLE IF EXISTS npc_vendor_items;
CREATE TABLE npc_vendor_items (
  npc_template_id INT NOT NULL,
  item_template_id INT NOT NULL,
  stack INT DEFAULT 1 NOT NULL,
  stats_visible CHAR(1) DEFAULT '1' NOT NULL,
  slot INT NOT NULL
);

CREATE INDEX npc_vendor_items_npc_template_id_idx ON npc_vendor_items(npc_template_id);
