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

DROP TABLE npc_drops;
CREATE TABLE npc_drops (
  npc_template_id INT NOT NULL,
  item_template_id INT NOT NULL,
  stack INT NOT NULL,
  droprate DECIMAL(9,4) NOT NULL
);

DROP TABLE npc_vendor_items;
CREATE TABLE npc_vendor_items (
  npc_template_id INT NOT NULL,
  item_template_id INT NOT NULL,
  stack INT DEFAULT 1 NOT NULL,
  stats_visible CHAR(1) DEFAULT '1' NOT NULL,
  slot INT NOT NULL
);