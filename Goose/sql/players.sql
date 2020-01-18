CREATE TABLE players (
  player_id INT PRIMARY KEY,
  player_name TEXT NOT NULL,
  player_title TEXT DEFAULT '' NOT NULL,
  player_surname TEXT DEFAULT '' NOT NULL,
  password_hash TEXT NOT NULL,
  password_salt TEXT NOT NULL,
  access_status SMALLINT DEFAULT 2 NOT NULL,
  map_id SMALLINT DEFAULT 1 NOT NULL,
  map_x SMALLINT DEFAULT 50 NOT NULL,
  map_y SMALLINT DEFAULT 50 NOT NULL,
  player_facing SMALLINT DEFAULT 2 NOT NULL,
  bound_id SMALLINT DEFAULT 1 NOT NULL,
  bound_x SMALLINT DEFAULT 50 NOT NULL,
  bound_y SMALLINT DEFAULT 50 NOT NULL,
  player_gold BIGINT DEFAULT 5000 NOT NULL,
  player_level SMALLINT DEFAULT 1 NOT NULL,
  experience BIGINT DEFAULT 0 NOT NULL,
  experience_sold BIGINT DEFAULT 0 NOT NULL,
  player_hp INT DEFAULT 0 NOT NULL,
  player_mp INT DEFAULT 0 NOT NULL,
  player_sp INT DEFAULT 0 NOT NULL,
  class_id SMALLINT DEFAULT 1 NOT NULL,
  guild_id SMALLINT DEFAULT 0 NOT NULL,
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
  aether_threshold DECIMAL(9,4) DEFAULT 0 NOT NULL,
  toggle_settings BIGINT DEFAULT 0 NOT NULL,
  donation_credits INT DEFAULT 0 NOT NULL,
  total_playtime BIGINT DEFAULT 0 NOT NULL,
  total_afktime BIGINT DEFAULT 0 NOT NULL,
  move_speed INT DEFAULT 320 NOT NULL,
  bank_pages INT DEFAULT 3 NOT NULL,
  unban_date DATETIME2 DEFAULT NULL,
  macrocheck_failures INT DEFAULT 0 NOT NULL
);

CREATE TABLE inventory (
  player_id INT NOT NULL,
  serialized_data TEXT NOT NULL,

  PRIMARY KEY(player_id)
);

CREATE TABLE equipped (
  player_id INT NOT NULL,
  serialized_data TEXT NOT NULL,

  PRIMARY KEY(player_id)
);

CREATE TABLE combinebag (
  player_id INT NOT NULL,
  serialized_data TEXT NOT NULL,

  PRIMARY KEY(player_id)
);

CREATE TABLE spellbook (
  player_id INT NOT NULL,
  serialized_data TEXT NOT NULL,

  PRIMARY KEY(player_id)
);