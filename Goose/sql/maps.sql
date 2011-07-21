USE Goose;

DROP TABLE maps;
GO

CREATE TABLE maps (
  map_id SMALLINT IDENTITY(1, 1) NOT NULL,
  map_name VARCHAR(50) NOT NULL,
  map_x SMALLINT DEFAULT 100 NOT NULL,
  map_y SMALLINT DEFAULT 100 NOT NULL,
  
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
  
  PRIMARY KEY(map_id)
);

SET IDENTITY_INSERT maps ON;
INSERT INTO maps (map_id, map_name, bind_enabled) VALUES (1, 'Minita', '1');
INSERT INTO maps (map_id, map_name) VALUES (2,'Its the Bat Cave Robin');
INSERT INTO maps (map_id, map_name) VALUES (3,'Graveyard');
INSERT INTO maps (map_id, map_name, pvp_enabled, pets_enabled) VALUES (4,'Forest Arena', '1', '0');
INSERT INTO maps (map_id, map_name) VALUES (5,'GM Paradise');
INSERT INTO maps (map_id, map_name) VALUES (6,'Undead Hallway');
INSERT INTO maps (map_id, map_name) VALUES (7,'Slime Kingdom');
INSERT INTO maps (map_id, map_name, bind_enabled) VALUES (8,'Roadkill Woods', '1');
INSERT INTO maps (map_id, map_name) VALUES (9,'Punchys Playhouse');
INSERT INTO maps (map_id, map_name) VALUES (10,'Dead Forest');
INSERT INTO maps (map_id, map_name) VALUES (11,'Otherlands');
INSERT INTO maps (map_id, map_name) VALUES (12,'Mindless Mines');
INSERT INTO maps (map_id, map_name) VALUES (13,'Hay and Frays Stronghold');
INSERT INTO maps (map_id, map_name) VALUES (14,'Chiisaiyama');
INSERT INTO maps (map_id, map_name, pvp_enabled, pets_enabled) VALUES (15,'Battle Fields', '1', '0');
INSERT INTO maps (map_id, map_name) VALUES (16,'Arctic Lands');
INSERT INTO maps (map_id, map_name) VALUES (17,'Northern Arctic Lands');
INSERT INTO maps (map_id, map_name) VALUES (18,'Frozen Lake');
INSERT INTO maps (map_id, map_name) VALUES (19,'Frigid Maze');
INSERT INTO maps (map_id, map_name) VALUES (20,'Tower of the Ancients');
INSERT INTO maps (map_id, map_name) VALUES (21,'21');
INSERT INTO maps (map_id, map_name, pvp_enabled) VALUES (22,'22', '1');
INSERT INTO maps (map_id, map_name) VALUES (23,'23');
INSERT INTO maps (map_id, map_name, pvp_enabled, min_experience, pets_enabled) VALUES (24,'The Ancients Dungeon', '1', 100000000, '0');
INSERT INTO maps (map_id, map_name) VALUES (25,'Boondocks');
INSERT INTO maps (map_id, map_name) VALUES (26,'26');
INSERT INTO maps (map_id, map_name) VALUES (27,'Nagan Oasis');
INSERT INTO maps (map_id, map_name) VALUES (28,'Savage Isle');
INSERT INTO maps (map_id, map_name, min_experience, pets_enabled, pvp_enabled) VALUES (29,'The Ancients Dungeon', 20000000, '0', '1');
INSERT INTO maps (map_id, map_name) VALUES (30,'Where am I?');
INSERT INTO maps (map_id, map_name) VALUES (31,'Event Hall');
INSERT INTO maps (map_id, map_name, min_experience) VALUES (32,'Sorrows Grove',10000000);
INSERT INTO maps (map_id, map_name, min_experience) VALUES (33,'The Passing',60000000);
INSERT INTO maps (map_id, map_name, min_experience) VALUES (34,'Winter Heights',300000000);
INSERT INTO maps (map_id, map_name) VALUES (35,'Paradise');
INSERT INTO maps (map_id, map_name) VALUES (36,'Shops');
INSERT INTO maps (map_id, map_name) VALUES (37,'Marketplace');
INSERT INTO maps (map_id, map_name) VALUES (38,'Pumpkin Grove');
INSERT INTO maps (map_id, map_name, min_level) VALUES (39,'Haunted House', 40);
INSERT INTO maps (map_id, map_name, min_experience, pvp_enabled, pets_enabled) VALUES (40,'The Ancients Dungeon', 400000000, '1', '0');
INSERT INTO maps (map_id, map_name) VALUES (41,'Hall of Heroes');
INSERT INTO maps (map_id, map_name, min_experience) VALUES (42,'Rugged Valley',100000000);
INSERT INTO maps (map_id, map_name, min_experience) VALUES (43,'Bear Kingdom',200000000);
INSERT INTO maps (map_id, map_name) VALUES (44,'Starving Pits');
SET IDENTITY_INSERT maps OFF;

DROP TABLE map_required_items;
CREATE TABLE map_required_items (
  map_id SMALLINT NOT NULL,
  item_template_id INT NOT NULL,
);

INSERT INTO map_required_items (map_id, item_template_id) VALUES (29, 266);
INSERT INTO map_required_items (map_id, item_template_id) VALUES (43, 458);
INSERT INTO map_required_items (map_id, item_template_id) VALUES (40, 516);