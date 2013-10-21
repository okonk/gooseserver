USE IllutiaGoose;

DROP TABLE maps;
GO

CREATE TABLE maps (
  map_id SMALLINT IDENTITY(1, 1) NOT NULL,
  map_name VARCHAR(50) NOT NULL,
  map_filename VARCHAR(50) NOT NULL,
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
INSERT INTO maps (map_id, map_filename, map_name) VALUES (1,'Map1.map','Minita');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (2,'Map2.map','Its the Bat Cave Robin');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (3,'Map3.map','Graveyard');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (4,'Map4.map','Forest Arena');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (5,'Map5.map','GM Paradise');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (6,'Map6.map','Undead Hallway');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (7,'Map7.map','Slime Kingdom');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (8,'Map8.map','Roadkill Woods');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (9,'Map9.map','Punchys Playhouse');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (10,'Map10.map','Dead Forest');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (11,'Map11.map','Otherlands');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (12,'Map12.map','Mindless Mines');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (13,'Map13.map','Hay and Frays Stronghold');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (14,'Map14.map','Chiisaiyama');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (15,'Map15.map','Battle Fields');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (16,'Map16.map','Arctic Lands');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (17,'Map17.map','Northern Arctic Lands');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (18,'Map18.map','Frozen Lake');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (19,'Map19.map','Frigid Maze');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (20,'Map20.map','Tower of the Ancients');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (21,'Map21.map','21');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (22,'Map22.map','22');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (23,'Map23.map','23');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (24,'Map24.map','The Ancients Dungeon');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (25,'Map25.map','Boondocks');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (26,'Map26.map','26');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (27,'Map27.map','Nagan Oasis');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (28,'Map28.map','Savage Isle');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (29,'Map29.map','The Ancients Dungeon');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (30,'Map30.map','Where am I?');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (31,'Map31.map','Event Hall');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (32,'Map32.map','Sorrows Grove');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (33,'Map33.map','The Passing');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (34,'Map34.map','Winter Heights');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (35,'Map35.map','Paradise');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (36,'Map36.map','Shops');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (37,'Map37.map','Marketplace');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (38,'Map38.map','Pumpkin Grove');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (39,'Map39.map','Haunted House');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (40,'Map40.map','The Ancients Dungeon');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (41,'Map41.map','Hall of Heroes');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (42,'Map42.map','Rugged Valley');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (43,'Map43.map','Bear Kingdom');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (44,'Map44.map','Starving Pits');
SET IDENTITY_INSERT maps OFF;

DROP TABLE map_required_items;
CREATE TABLE map_required_items (
  map_id SMALLINT NOT NULL,
  item_template_id INT NOT NULL,
);