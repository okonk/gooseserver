USE IllutiaGoose;

DROP TABLE pets;
CREATE TABLE pets (
  pet_id INT NOT NULL,
  owner_id INT NOT NULL,
  pet_name VARCHAR(50) NOT NULL,
  pet_title VARCHAR(50) DEFAULT ' ' NOT NULL,
  pet_surname VARCHAR(50) DEFAULT ' ' NOT NULL,
  respawn_time INT DEFAULT 0 NOT NULL,
  next_respawn_time BIGINT DEFAULT 0 NOT NULL,
  pet_facing SMALLINT DEFAULT 3 NOT NULL,
  pet_level SMALLINT DEFAULT 1 NOT NULL,
  experience BIGINT DEFAULT 0 NOT NULL,
  experience_sold BIGINT DEFAULT 0 NOT NULL,
  aggro_range SMALLINT DEFAULT 0 NOT NULL,
  attack_range SMALLINT DEFAULT 0 NOT NULL,
  attack_speed DECIMAL(9,4) DEFAULT 2 NOT NULL,
  move_speed DECIMAL(9,4) DEFAULT 2 NOT NULL,
  
  pet_hp INT DEFAULT 0 NOT NULL,
  pet_mp INT DEFAULT 0 NOT NULL,
  pet_sp INT DEFAULT 0 NOT NULL,
  
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
  
  PRIMARY KEY(pet_id),
  INDEX pets_owner_id_idx (owner_id)
);
