DROP DATABASE IllutiaGoose;

CREATE DATABASE IllutiaGoose;
go

USE IllutiaGoose;
go

CREATE LOGIN GooseServer WITH password='password1';
go
CREATE USER GooseServer FOR LOGIN GooseServer;
go
GRANT ALTER TO GooseServer;
go
GRANT CONTROL TO GooseServer;

go

CREATE TABLE blockedtiles (
  /*id INT IDENTITY(1,1) NOT NULL,*/
  map_id SMALLINT NOT NULL,
  map_x SMALLINT NOT NULL,
  map_y SMALLINT NOT NULL,
  /*PRIMARY KEY (id)*/
);

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
  graphic_file INT NOT NULL,
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
	graphic_tile, graphic_file, graphic_equip) VALUES (5001, 1, 'Gold', 331900, 2275, 0);