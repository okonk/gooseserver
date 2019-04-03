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

  script_path TEXT DEFAULT '' NOT NULL,
  script_data TEXT DEFAULT '' NOT NULL,
  
  PRIMARY KEY(map_id)
);

SET IDENTITY_INSERT maps ON;
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (1, 'Map1.map', '1', 286, 194);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (2, 'Map2.map', '2', 500, 215);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (3, 'Map3.map', '3', 500, 500);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (4, 'Map4.map', '4', 200, 200);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (5, 'Map5.map', '5', 243, 143);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (6, 'Map6.map', '6', 500, 226);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (7, 'Map7.map', '7', 201, 271);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (8, 'Map8.map', '8', 201, 200);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (9, 'Map9.map', '9', 111, 188);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (10, 'Map10.map', '10', 140, 180);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (11, 'Map11.map', '11', 500, 73);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (12, 'Map12.map', '12', 133, 194);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (13, 'Map13.map', '13', 58, 62);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (14, 'Map14.map', '14', 140, 100);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (15, 'Map15.map', '15', 38, 41);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (16, 'Map16.map', '16', 86, 142);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (17, 'Map17.map', '17', 125, 125);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (18, 'Map18.map', '18', 125, 155);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (19, 'Map19.map', '19', 109, 109);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (20, 'Map20.map', '20', 58, 32);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (21, 'map21.map', '21', 500, 60);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (22, 'Map22.map', '22', 64, 70);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (23, 'Map23.map', '23', 48, 50);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (24, 'Map24.map', '24', 112, 108);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (25, 'Map25.map', '25', 161, 121);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (26, 'Map26.map', '26', 200, 125);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (27, 'Map27.map', '27', 61, 121);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (28, 'Map28.map', '28', 121, 79);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (29, 'Map29.map', '29', 65, 52);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (30, 'Map30.map', '30', 51, 37);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (31, 'Map31.map', '31', 110, 105);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (32, 'Map32.map', '32', 100, 100);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (33, 'Map33.map', '33', 133, 220);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (34, 'Map34.map', '34', 135, 94);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (35, 'Map35.map', '35', 59, 103);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (36, 'Map36.map', '36', 292, 297);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (37, 'Map37.map', '37', 100, 100);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (38, 'Map38.map', '38', 100, 100);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (39, 'Map39.map', '39', 100, 100);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (40, 'Map40.map', '40', 100, 100);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (41, 'Map41.map', '41', 500, 100);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (42, 'Map42.map', '42', 136, 145);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (43, 'Map43.map', '43', 95, 127);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (44, 'Map44.map', '44', 500, 99);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (45, 'Map45.map', '45', 200, 200);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (46, 'Map46.map', '46', 131, 140);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (47, 'Map47.map', '47', 194, 212);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (48, 'Map48.map', '48', 194, 135);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (49, 'Map49.map', '49', 52, 55);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (50, 'Map50.map', '50', 350, 350);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (51, 'Map51.map', '51', 100, 99);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (52, 'Map52.map', '52', 156, 285);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (53, 'Map53.map', '53', 214, 296);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (54, 'Map54.map', '54', 150, 150);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (55, 'Map55.map', '55', 150, 150);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (56, 'Map56.map', '56', 62, 66);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (57, 'Map57.map', '57', 83, 121);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (58, 'Map58.map', '58', 83, 116);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (59, 'Map59.map', '59', 83, 116);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (60, 'Map60.map', '60', 150, 150);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (61, 'Map61.map', '61', 125, 88);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (62, 'Map62.map', '62', 207, 163);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (63, 'Map63.map', '63', 495, 500);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (64, 'Map64.map', '64', 500, 225);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (65, 'Map65.map', '65', 222, 251);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (66, 'Map66.map', '66', 149, 49);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (67, 'Map67.map', '67', 160, 137);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (68, 'Map68.map', '68', 228, 205);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (69, 'Map69.map', '69', 15, 13);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (70, 'Map70.map', '70', 124, 197);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (71, 'Map71.map', '71', 104, 86);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (72, 'Map72.map', '72', 54, 37);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (73, 'Map73.map', '73', 54, 37);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (74, 'Map74.map', '74', 176, 103);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (75, 'Map75.map', '75', 80, 58);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (76, 'Map76.map', '76', 80, 58);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (77, 'Map77.map', '77', 80, 58);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (78, 'Map78.map', '78', 80, 58);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (79, 'Map79.map', '79', 80, 58);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (80, 'Map80.map', '80', 192, 79);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (81, 'Map81.map', '81', 194, 128);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (82, 'Map82.map', '82', 277, 136);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (83, 'Map83.map', '83', 200, 80);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (84, 'Map84.map', '84', 210, 148);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (85, 'Map85.map', '85', 247, 183);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (86, 'Map86.map', '86', 500, 106);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (87, 'Map87.map', '87', 161, 141);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (88, 'Map88.map', '88', 60, 45);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (89, 'Map89.map', '89', 128, 47);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (90, 'Map90.map', '90', 194, 201);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (91, 'Map91.map', '91', 500, 277);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (92, 'Map92.map', '92', 187, 140);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (93, 'Map93.map', '93', 114, 115);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (94, 'Map94.map', '94', 83, 109);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (95, 'Map95.map', '95', 143, 157);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (96, 'Map96.map', '96', 31, 51);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (97, 'Map97.map', '97', 54, 37);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (98, 'Map98.map', '98', 54, 37);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (99, 'Map99.map', '99', 500, 215);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (100, 'Map100.map', '100', 277, 183);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (101, 'Map101.map', '101', 500, 135);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (102, 'Map102.map', '102', 175, 163);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (103, 'Map103.map', '103', 145, 328);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (104, 'Map104.map', '104', 134, 257);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (105, 'Map105.map', '105', 203, 291);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (106, 'Map106.map', '106', 127, 124);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (107, 'Map107.map', '107', 333, 191);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (108, 'Map108.map', '108', 236, 201);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (109, 'Map109.map', '109', 199, 153);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (110, 'Map110.map', '110', 198, 153);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (111, 'Map111.map', '111', 204, 185);
INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES (112, 'Map112.map', '112', 500, 150);
SET IDENTITY_INSERT maps OFF;

DROP TABLE map_required_items;
CREATE TABLE map_required_items (
  map_id SMALLINT NOT NULL,
  item_template_id INT NOT NULL,

  INDEX map_required_items_map_id_idx (map_id)
);