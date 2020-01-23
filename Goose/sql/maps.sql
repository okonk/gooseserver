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

INSERT INTO maps (map_id, map_filename, map_name) VALUES (1, 'Map1.map', '1');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (2, 'Map2.map', '1');


DROP TABLE IF EXISTS map_required_items;
CREATE TABLE map_required_items (
  map_id INT NOT NULL,
  item_template_id INT NOT NULL
);

CREATE INDEX map_required_items_map_id_idx ON map_required_items(map_id);