USE Goose;

DROP TABLE combination_item_required;
CREATE TABLE combination_item_required (
	combination_id INT NOT NULL,
	item_template_id INT NOT NULL,
);

DROP TABLE combination_item_results;
CREATE TABLE combination_item_results (
	combination_id INT NOT NULL,
	item_template_id INT NOT NULL,
);

DROP TABLE combinations;
CREATE TABLE combinations (
	combination_id INT IDENTITY(1,1) NOT NULL,
	combination_name VARCHAR(64) NOT NULL,
	min_level INT DEFAULT 1 NOT NULL,
	max_level INT DEFAULT 50 NOT NULL,
	min_experience BIGINT DEFAULT 0 NOT NULL,
	max_experience BIGINT DEFAULT 0 NOT NULL,
	class_restrictions BIGINT DEFAULT 0 NOT NULL,
	
	PRIMARY KEY(combination_id)
);

SET IDENTITY_INSERT combinations ON;

INSERT INTO combinations (combination_id, combination_name) VALUES (1, 'Cloth');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (1, 340); /* Needle */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (1, 348); /* Spool of colour Thread */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (1, 449);

INSERT INTO combinations (combination_id, combination_name) VALUES (2, 'Cloth');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (2, 340); /* Needle */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (2, 349); /* Spool of colour Thread */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (2, 449);

INSERT INTO combinations (combination_id, combination_name) VALUES (3, 'Cloth');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (3, 340); /* Needle */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (3, 350); /* Spool of colour Thread */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (3, 449);

INSERT INTO combinations (combination_id, combination_name) VALUES (4, 'Cloth');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (4, 340); /* Needle */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (4, 351); /* Spool of colour Thread */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (4, 449);

INSERT INTO combinations (combination_id, combination_name) VALUES (5, 'Cloth');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (5, 340); /* Needle */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (5, 352); /* Spool of colour Thread */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (5, 449);

INSERT INTO combinations (combination_id, combination_name) VALUES (6, 'Cloth');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (6, 340); /* Needle */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (6, 353); /* Spool of colour Thread */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (6, 449);

INSERT INTO combinations (combination_id, combination_name) VALUES (7, 'Cloth Shirt');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (7, 449); /* Cloth */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (7, 346); /* Shirt Pattern */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (7, 340); /* Needle */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (7, 347); /* Spool of Thread */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (7, 450);

INSERT INTO combinations (combination_id, combination_name) VALUES (8, 'Practice Katana');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (8, 449); /* Cloth */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (8, 344); /* Rope */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (8, 339); /* Low Quality Hilt */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (8, 338); /* Low Quality Blade */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (8, 451);

INSERT INTO combinations (combination_id, combination_name) VALUES (9, 'Soft Belt');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (9, 8); /* Rabbit Pelt */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (9, 345); /* Sharp Scissors */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (9, 452);

INSERT INTO combinations (combination_id, combination_name) VALUES (10, 'Cat Ears');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (10, 350); /* Spool of Red Thread */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (10, 355); /* Cats Hair */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (10, 340); /* Needle */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (10, 455); /* Leather Padding */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (10, 453);

INSERT INTO combinations (combination_id, combination_name) VALUES (11, 'Black Cat Ears');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (11, 351); /* Spool of Black Thread */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (11, 355); /* Cats Hair */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (11, 340); /* Needle */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (11, 455); /* Leather Padding */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (11, 454);

INSERT INTO combinations (combination_id, combination_name) VALUES (12, 'Bonfire');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (12, 330); /* Flint */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (12, 3); /* Stick */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (12, 331);

INSERT INTO combinations (combination_id, combination_name) VALUES (13, 'Low Quality Walde');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (13, 338); /* Low Quality Blade */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (13, 339); /* Low Quality Hilt */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (13, 342); /* Liquid Ore */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (13, 332); /* Chisel */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (13, 363);

INSERT INTO combinations (combination_id, combination_name) VALUES (14, 'Medium Quality Walde');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (14, 336); /* Medium Quality Blade */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (14, 337); /* Medium Quality Hilt */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (14, 342); /* Liquid Ore */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (14, 332); /* Chisel */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (14, 362);

INSERT INTO combinations (combination_id, combination_name) VALUES (15, 'High Quality Walde');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (15, 334); /* High Quality Blade */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (15, 335); /* High Quality Hilt */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (15, 342); /* Liquid Ore */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (15, 332); /* Chisel */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (15, 361);

INSERT INTO combinations (combination_id, combination_name) VALUES (16, 'Scroll: Bat Illusion');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (16, 333); /* Garlic */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (16, 328); /* Blank Scroll */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (16, 341); /* Ink */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (16, 329);

INSERT INTO combinations (combination_id, combination_name) VALUES (17, 'Crude Gold Ring');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (17, 445); /* Soft Gold Ore */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (17, 331); /* Bonfire */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (17, 358);

INSERT INTO combinations (combination_id, combination_name) VALUES (18, 'Crude Pearl Ring');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (18, 358); /* Crude Gold Ring */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (18, 343); /* Pearl */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (18, 357);

INSERT INTO combinations (combination_id, combination_name) VALUES (19, 'Crude Ruby Ring');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (19, 358); /* Crude Gold Ring */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (19, 60); /* Ruby */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (19, 359);

INSERT INTO combinations (combination_id, combination_name) VALUES (20, 'Fighting Katana');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (20, 451); /* Practice Katana */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (20, 354); /* Unrefined Ore */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (20, 332); /* Chisel */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (20, 58); /* Gem of Power */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (20, 456);

INSERT INTO combinations (combination_id, combination_name) VALUES (21, 'Coral Sword');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (21, 267); /* Unadorned Coral */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (21, 335); /* High Quality Hilt */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (21, 332); /* Chisel */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (21, 342); /* Liquid Ore */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (21, 334); /* High Quality Blade */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (21, 58); /* Gem of Power */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (21, 251);

INSERT INTO combinations (combination_id, combination_name) VALUES (22, 'Harvest Medallion');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (22, 331); /* Bonfire */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (22, 445); /* Soft Gold Ore */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (22, 457); /* Red Rope */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (22, 444); /* Sketch */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (22, 332); /* Chisel */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (22, 141);

INSERT INTO combinations (combination_id, combination_name) VALUES (23, 'Scroll: Smokebomb');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (23, 263); /* Lesser Essence of Water */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (23, 328); /* Blank Scroll */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (23, 341); /* Ink */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (23, 58); /* Gem of Power */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (23, 323);

INSERT INTO combinations (combination_id, combination_name) VALUES (24, 'Scroll: Warrior Root');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (24, 264); /* Lesser Essence of Fire */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (24, 328); /* Blank Scroll */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (24, 341); /* Ink */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (24, 58); /* Gem of Power */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (24, 325);

INSERT INTO combinations (combination_id, combination_name) VALUES (25, 'Scroll: Covenant');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (25, 262); /* Lesser Essence of Earth */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (25, 328); /* Blank Scroll */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (25, 341); /* Ink */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (25, 58); /* Gem of Power */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (25, 272);

INSERT INTO combinations (combination_id, combination_name) VALUES (26, 'Scroll: Group Heal');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (26, 265); /* Lesser Essence of Air */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (26, 328); /* Blank Scroll */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (26, 341); /* Ink */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (26, 58); /* Gem of Power */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (26, 324);

INSERT INTO combinations (combination_id, combination_name) VALUES (27, 'Scroll: Ancient Damage');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (27, 298); /* Essence of Water */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (27, 328); /* Blank Scroll */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (27, 341); /* Ink */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (27, 58); /* Gem of Power */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (27, 320);

INSERT INTO combinations (combination_id, combination_name) VALUES (28, 'Scroll: Ancient Augmentation');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (28, 299); /* Essence of Fire */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (28, 328); /* Blank Scroll */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (28, 341); /* Ink */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (28, 58); /* Gem of Power */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (28, 317);

INSERT INTO combinations (combination_id, combination_name) VALUES (29, 'Scroll: Ancient Regeneration');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (29, 297); /* Essence of Earth */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (29, 328); /* Blank Scroll */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (29, 341); /* Ink */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (29, 58); /* Gem of Power */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (29, 475);

INSERT INTO combinations (combination_id, combination_name) VALUES (30, 'Scroll: Ancient Sacrifice');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (30, 300); /* Essence of Air */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (30, 328); /* Blank Scroll */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (30, 341); /* Ink */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (30, 58); /* Gem of Power */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (30, 322);

INSERT INTO combinations (combination_id, combination_name) VALUES (31, 'Empty Box');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (31, 268); /* Present 1 */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (31, 269); /* Present 2 */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (31, 270); /* Present 3 */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (31, 271); /* Present 4 */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (31, 443);

INSERT INTO combinations (combination_id, combination_name) VALUES (32, 'Magus Moon Shield');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (32, 297); /* Essence of Earth */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (32, 303); /* Shield Design */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (32, 115); /* Moon Shield */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (32, 58); /* Gem of Power */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (32, 293);

INSERT INTO combinations (combination_id, combination_name) VALUES (33, 'Rogue Moon Shield');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (33, 298); /* Essence of Water */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (33, 303); /* Shield Design */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (33, 115); /* Moon Shield */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (33, 58); /* Gem of Power */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (33, 295);

INSERT INTO combinations (combination_id, combination_name) VALUES (34, 'Warrior Moon Shield');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (34, 299); /* Essence of Fire */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (34, 303); /* Shield Design */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (34, 115); /* Moon Shield */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (34, 58); /* Gem of Power */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (34, 296);

INSERT INTO combinations (combination_id, combination_name) VALUES (35, 'Priest Moon Shield');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (35, 300); /* Essence of Air */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (35, 303); /* Shield Design */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (35, 115); /* Moon Shield */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (35, 58); /* Gem of Power */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (35, 294);

INSERT INTO combinations (combination_id, combination_name) VALUES (36, 'Scroll: Shroom Illusion');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (36, 20); /* Pile of Crap */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (36, 328); /* Blank Scroll */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (36, 341); /* Ink */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (36, 356);

INSERT INTO combinations (combination_id, combination_name) VALUES (37, 'Pearl Bracelet');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (37, 347); /* Spool of Thread */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (37, 343); /* Pearl */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (37, 343); /* Pearl */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (37, 343); /* Pearl */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (37, 343); /* Pearl */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (37, 343); /* Pearl */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (37, 343); /* Pearl */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (37, 343); /* Pearl */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (37, 343); /* Pearl */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (37, 360);

INSERT INTO combinations (combination_id, combination_name) VALUES (38, 'Ducky Pauldrons');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (38, 443); /* Empty Box */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (38, 442); /* Wrapping Paper */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (38, 21); /* Rubber Ducky */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (38, 435);

INSERT INTO combinations (combination_id, combination_name) VALUES (39, 'Magus Ancient Slippers');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (39, 297); /* Essence of Earth */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (39, 395); /* Slippers Pattern */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (39, 436); /* Priceless Needle */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (39, 438); /* Priceless Thread */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (39, 437); /* Priceless Pattern */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (39, 58); /* Gem of Power */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (39, 403);

INSERT INTO combinations (combination_id, combination_name) VALUES (40, 'Rogue Ancient Boots');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (40, 298); /* Essence of Water */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (40, 396); /* Boot Mold */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (40, 439); /* Priceless Ore */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (40, 440); /* Priceless Chisel */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (40, 441); /* Priceless Hammer */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (40, 58); /* Gem of Power */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (40, 401);

INSERT INTO combinations (combination_id, combination_name) VALUES (41, 'Warrior Ancient Boots');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (41, 299); /* Essence of Fire */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (41, 396); /* Boot Mold */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (41, 439); /* Priceless Ore */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (41, 440); /* Priceless Chisel */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (41, 441); /* Priceless Hammer */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (41, 58); /* Gem of Power */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (41, 402);

INSERT INTO combinations (combination_id, combination_name) VALUES (42, 'Priest Ancient Slippers');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (42, 300); /* Essence of Air */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (42, 395); /* Slippers Pattern */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (42, 436); /* Priceless Needle */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (42, 438); /* Priceless Thread */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (42, 437); /* Priceless Pattern */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (42, 58); /* Gem of Power */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (42, 404);

INSERT INTO combinations (combination_id, combination_name) VALUES (43, 'Magus Divine Crown');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (43, 297); /* Essence of Earth */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (43, 397); /* Divine Pattern */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (43, 436); /* Priceless Needle */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (43, 438); /* Priceless Thread */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (43, 437); /* Priceless Pattern */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (43, 58); /* Gem of Power */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (43, 407);

INSERT INTO combinations (combination_id, combination_name) VALUES (44, 'Rogue Divine Helm');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (44, 298); /* Essence of Water */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (44, 398); /* Divine Mold */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (44, 439); /* Priceless Ore */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (44, 440); /* Priceless Chisel */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (44, 441); /* Priceless Hammer */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (44, 58); /* Gem of Power */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (44, 405);

INSERT INTO combinations (combination_id, combination_name) VALUES (45, 'Warrior Divine Helm');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (45, 299); /* Essence of Fire */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (45, 398); /* Divine Mold */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (45, 439); /* Priceless Ore */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (45, 440); /* Priceless Chisel */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (45, 441); /* Priceless Hammer */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (45, 58); /* Gem of Power */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (45, 406);

INSERT INTO combinations (combination_id, combination_name) VALUES (46, 'Priest Divine Crown');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (46, 300); /* Essence of Air */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (46, 397); /* Divine Pattern */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (46, 436); /* Priceless Needle */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (46, 438); /* Priceless Thread */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (46, 437); /* Priceless Pattern */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (46, 58); /* Gem of Power */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (46, 408);

INSERT INTO combinations (combination_id, combination_name) VALUES (47, 'Gero Necklace');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (47, 448); /* Berrys Hair Strand */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (47, 447); /* Blood */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (47, 446); /* Gramps Fur */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (47, 458);

INSERT INTO combinations (combination_id, combination_name) VALUES (48, 'Bling Belt');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (48, 452); /* Soft Belt */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (48, 340); /* Needle */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (48, 347); /* Spool of Thread */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (48, 51); /* Dollar Bill */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (48, 445); /* Soft Gold Ore */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (48, 331); /* Bonfire */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (48, 479);

INSERT INTO combinations (combination_id, combination_name) VALUES (49, 'Enchanted Gloves');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (49, 20); /* Poo */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (49, 19); /* Sunflower */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (49, 142); /* Leather Gloves */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (49, 54); /* Taco */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (49, 345); /* Sharp Scissors */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (49, 351); /* Spool of Black Thread */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (49, 340); /* Needle */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (49, 480);

INSERT INTO combinations (combination_id, combination_name) VALUES (50, 'Magus Royal Leggings');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (50, 297); /* Essence of Earth */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (50, 490); /* Royal Leg Pattern */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (50, 436); /* Priceless Needle */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (50, 438); /* Priceless Thread */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (50, 437); /* Priceless Pattern */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (50, 58); /* Gem of Power */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (50, 497);

INSERT INTO combinations (combination_id, combination_name) VALUES (51, 'Rogue Royal Legplates');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (51, 298); /* Essence of Water */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (51, 491); /* Royal Leg Mold */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (51, 439); /* Priceless Ore */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (51, 440); /* Priceless Chisel */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (51, 441); /* Priceless Hammer */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (51, 58); /* Gem of Power */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (51, 494);

INSERT INTO combinations (combination_id, combination_name) VALUES (52, 'Warrior Royal Legplates');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (52, 299); /* Essence of Fire */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (52, 491); /* Royal Leg Mold */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (52, 439); /* Priceless Ore */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (52, 440); /* Priceless Chisel */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (52, 441); /* Priceless Hammer */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (52, 58); /* Gem of Power */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (52, 495);

INSERT INTO combinations (combination_id, combination_name) VALUES (53, 'Priest Royal Leggings');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (53, 300); /* Essence of Air */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (53, 490); /* Royal Leg Pattern */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (53, 436); /* Priceless Needle */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (53, 438); /* Priceless Thread */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (53, 437); /* Priceless Pattern */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (53, 58); /* Gem of Power */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (53, 496);

INSERT INTO combinations (combination_id, combination_name) VALUES (54, 'Magus Royal Tunic');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (54, 297); /* Essence of Earth */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (54, 492); /* Royal Tunic Pattern */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (54, 436); /* Priceless Needle */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (54, 438); /* Priceless Thread */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (54, 437); /* Priceless Pattern */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (54, 58); /* Gem of Power */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (54, 501);

INSERT INTO combinations (combination_id, combination_name) VALUES (55, 'Rogue Royal Chestplate');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (55, 298); /* Essence of Water */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (55, 493); /* Royal Chestplate Mold */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (55, 439); /* Priceless Ore */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (55, 440); /* Priceless Chisel */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (55, 441); /* Priceless Hammer */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (55, 58); /* Gem of Power */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (55, 498);

INSERT INTO combinations (combination_id, combination_name) VALUES (56, 'Warrior Royal Chestplate');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (56, 299); /* Essence of Fire */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (56, 493); /* Royal Chestplate Mold */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (56, 439); /* Priceless Ore */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (56, 440); /* Priceless Chisel */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (56, 441); /* Priceless Hammer */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (56, 58); /* Gem of Power */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (56, 499);

INSERT INTO combinations (combination_id, combination_name) VALUES (57, 'Priest Royal Tunic');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (57, 300); /* Essence of Air */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (57, 492); /* Royal Tunic Pattern */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (57, 436); /* Priceless Needle */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (57, 438); /* Priceless Thread */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (57, 437); /* Priceless Pattern */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (57, 58); /* Gem of Power */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (57, 500);

INSERT INTO combinations (combination_id, combination_name) VALUES (58, 'Key to the Ancients Dungeon');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (58, 512); /* Broken Key */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (58, 513); /* Broken Key */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (58, 514); /* Broken Key */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (58, 515); /* Broken Key */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (58, 331); /* Bonfire */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (58, 516);

INSERT INTO combinations (combination_id, combination_name) VALUES (59, 'Enchanted Bracelet of Fire');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (59, 299); /* Essence of Fire */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (59, 304); /* Bracelet of Fire */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (59, 585); /* Howto: Bracelet Enchantment */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (59, 580);

INSERT INTO combinations (combination_id, combination_name) VALUES (60, 'Enchanted Bracelet of Earth');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (60, 297); /* Essence of Earth */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (60, 305); /* Bracelet of Earth */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (60, 585); /* Howto: Bracelet Enchantment */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (60, 581);

INSERT INTO combinations (combination_id, combination_name) VALUES (61, 'Enchanted Bracelet of Air');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (61, 300); /* Essence of Air */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (61, 306); /* Bracelet of Air */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (61, 585); /* Howto: Bracelet Enchantment */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (61, 582);

INSERT INTO combinations (combination_id, combination_name) VALUES (62, 'Enchanted Bracelet of Water');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (62, 298); /* Essence of Water*/
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (62, 307); /* Bracelet of Water */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (62, 585); /* Howto: Bracelet Enchantment */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (62, 583);

INSERT INTO combinations (combination_id, combination_name) VALUES (63, 'Enchanted Bracelet of Spirit');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (63, 58); /* Gem of Power */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (63, 308); /* Bracelet of Spirit */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (63, 585); /* Howto: Bracelet Enchantment */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (63, 584);

INSERT INTO combinations (combination_id, combination_name) VALUES (64, 'Rogue Enchanted Divine Helm');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (64, 58); /* Gem of Power */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (64, 298); /* Essence of Water */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (64, 405); /* Divine Helm */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (64, 591); /* Howto: Helm Enchantment */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (64, 587);

INSERT INTO combinations (combination_id, combination_name) VALUES (65, 'Warrior Enchanted Divine Helm');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (65, 58); /* Gem of Power */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (65, 299); /* Essence of Fire */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (65, 406); /* Divine Helm */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (65, 591); /* Howto: Helm Enchantment */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (65, 588);

INSERT INTO combinations (combination_id, combination_name) VALUES (66, 'Magus Enchanted Divine Crown');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (66, 58); /* Gem of Power */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (66, 297); /* Essence of Earth */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (66, 407); /* Divine Crown */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (66, 591); /* Howto: Helm Enchantment */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (66, 589);

INSERT INTO combinations (combination_id, combination_name) VALUES (67, 'Priest Enchanted Divine Crown');
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (67, 58); /* Gem of Power */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (67, 300); /* Essence of Air */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (67, 408); /* Divine Helm */
INSERT INTO combination_item_required (combination_id, item_template_id) VALUES (67, 591); /* Howto: Helm Enchantment */
INSERT INTO combination_item_results (combination_id, item_template_id) VALUES (67, 590);

SET IDENTITY_INSERT combinations OFF;