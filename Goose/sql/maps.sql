USE IllutiaGoose;

DROP TABLE maps;
GO

CREATE TABLE maps (
  map_id SMALLINT IDENTITY(1, 1) NOT NULL,
  map_name VARCHAR(50) NOT NULL,
  map_filename VARCHAR(50) NOT NULL,
  map_x SMALLINT DEFAULT 1000 NOT NULL,
  map_y SMALLINT DEFAULT 1000 NOT NULL,
  
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
INSERT INTO maps (map_id, map_filename, map_name) VALUES (1,'Map1.map','1');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (2,'Map2.map','2');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (3,'Map3.map','3');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (4,'Map4.map','4');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (5,'Map5.map','5');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (6,'Map6.map','6');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (7,'Map7.map','7');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (8,'Map8.map','8');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (9,'Map9.map','9');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (10,'Map10.map','10');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (11,'Map11.map','11');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (12,'Map12.map','12');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (13,'Map13.map','13');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (14,'Map14.map','14');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (15,'Map15.map','15');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (16,'Map16.map','16');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (17,'Map17.map','17');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (18,'Map18.map','18');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (19,'Map19.map','19');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (20,'Map20.map','20');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (21,'Map21.map','21');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (22,'Map22.map','22');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (23,'Map23.map','23');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (24,'Map24.map','24');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (25,'Map25.map','25');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (26,'Map26.map','26');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (27,'Map27.map','27');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (28,'Map28.map','28');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (29,'Map29.map','29');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (30,'Map30.map','30');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (31,'Map31.map','31');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (32,'Map32.map','32');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (33,'Map33.map','33');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (34,'Map34.map','34');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (35,'Map35.map','35');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (36,'Map36.map','36');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (37,'Map37.map','37');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (38,'Map38.map','38');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (39,'Map39.map','39');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (40,'Map40.map','40');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (41,'Map41.map','41');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (42,'Map42.map','42');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (43,'Map43.map','43');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (44,'Map44.map','44');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (45,'Map45.map','45');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (46,'Map46.map','46');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (47,'Map47.map','47');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (48,'Map48.map','48');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (49,'Map49.map','49');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (50,'Map50.map','50');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (51,'Map51.map','51');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (52,'Map52.map','52');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (53,'Map53.map','53');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (54,'Map54.map','54');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (55,'Map55.map','55');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (56,'Map56.map','56');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (57,'Map57.map','57');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (58,'Map58.map','58');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (59,'Map59.map','59');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (60,'Map60.map','60');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (61,'Map61.map','61');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (62,'Map62.map','62');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (63,'Map63.map','63');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (64,'Map64.map','64');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (65,'Map65.map','65');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (66,'Map66.map','66');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (67,'Map67.map','67');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (68,'Map68.map','68');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (69,'Map69.map','69');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (70,'Map70.map','70');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (71,'Map71.map','71');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (72,'Map72.map','72');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (73,'Map73.map','73');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (74,'Map74.map','74');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (75,'Map75.map','75');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (76,'Map76.map','76');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (77,'Map77.map','77');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (78,'Map78.map','78');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (79,'Map79.map','79');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (80,'Map80.map','80');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (81,'Map81.map','81');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (82,'Map82.map','82');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (83,'Map83.map','83');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (84,'Map84.map','84');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (85,'Map85.map','85');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (86,'Map86.map','86');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (87,'Map87.map','87');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (88,'Map88.map','88');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (89,'Map89.map','89');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (90,'Map90.map','90');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (91,'Map91.map','91');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (92,'Map92.map','92');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (93,'Map93.map','93');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (94,'Map94.map','94');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (95,'Map95.map','95');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (96,'Map96.map','96');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (97,'Map97.map','97');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (98,'Map98.map','98');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (99,'Map99.map','99');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (100,'Map100.map','100');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (101,'Map101.map','101');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (102,'Map102.map','102');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (103,'Map103.map','103');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (104,'Map104.map','104');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (105,'Map105.map','105');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (106,'Map106.map','106');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (107,'Map107.map','107');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (108,'Map108.map','108');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (109,'Map109.map','109');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (110,'Map110.map','110');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (111,'Map111.map','111');
INSERT INTO maps (map_id, map_filename, map_name) VALUES (112,'Map112.map','112');
SET IDENTITY_INSERT maps OFF;

DROP TABLE map_required_items;
CREATE TABLE map_required_items (
  map_id SMALLINT NOT NULL,
  item_template_id INT NOT NULL,
);