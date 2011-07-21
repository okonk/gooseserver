USE Goose

DROP TABLE warptiles;
CREATE TABLE warptiles (
  id INT IDENTITY(1,1) NOT NULL,
  map_id SMALLINT NOT NULL,
  map_x SMALLINT NOT NULL,
  map_y SMALLINT NOT NULL,
  warp_id SMALLINT NOT NULL,
  warp_x SMALLINT NOT NULL,
  warp_y SMALLINT NOT NULL,
  PRIMARY KEY (id)
);

SET IDENTITY_INSERT warptiles ON;
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('2','4','25','14','1','42','28');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('3','1','42','27','4','24','14');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('4','4','25','15','1','41','28');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('5','1','41','27','4','24','15');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('6','1','40','27','4','24','15');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('12','15','13','15','1','39','28');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('13','15','12','15','1','39','28');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('14','15','11','15','1','38','28');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('15','15','10','15','1','37','28');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('16','15','9','15','1','37','28');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('17','1','37','27','15','9','14');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('18','1','38','27','15','11','14');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('20','1','39','27','15','13','14');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('21','8','100','25','1','2','49');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('22','8','100','26','1','2','50');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('23','8','100','27','1','2','51');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('24','8','100','24','1','2','48');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('25','1','1','48','8','99','24');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('26','1','1','49','8','99','25');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('27','1','1','50','8','99','26');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('28','1','1','51','8','99','27');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('29','8','1','49','28','99','50');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('30','8','1','50','28','99','51');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('31','8','1','51','28','99','52');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('32','8','1','52','28','99','53');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('33','28','100','50','8','2','49');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('34','28','100','51','8','2','50');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('35','28','100','52','8','2','51');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('36','28','100','53','8','2','52');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('38','28','91','50','28','9','37');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('39','28','91','51','28','10','37');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('40','28','91','52','28','11','37');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('41','28','91','53','28','12','37');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('42','28','12','36','28','92','53');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('43','28','11','36','28','92','52');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('44','28','10','36','28','92','51');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('45','28','9','36','28','92','50');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('50','28','54','72','28','61','23');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('51','28','55','72','28','62','23');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('52','28','55','73','28','63','23');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('53','28','54','73','28','64','23');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('54','28','61','24','28','54','71');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('55','28','62','24','28','54','71');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('56','28','63','24','28','55','71');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('57','28','64','24','28','55','71');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('58','16','50','100','1','64','2');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('59','16','49','100','1','64','2');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('60','16','48','100','1','63','2');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('61','1','63','1','16','48','99');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('62','1','64','1','16','49','99');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('63','2','10','9','8','62','7');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('64','8','62','5','2','10','11');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('65','7','1','51','1','99','52');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('66','1','100','52','7','2','51');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('67','7','1','52','1','99','53');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('68','1','100','53','7','2','52');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('69','25','44','1','1','67','99');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('70','25','45','1','1','68','99');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('71','25','46','1','1','69','99');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('72','1','69','100','25','46','2');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('73','1','68','100','25','45','2');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('74','1','67','100','25','44','2');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('75','14','48','1','25','34','99');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('76','14','49','1','25','35','99');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('77','14','50','1','25','36','99');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('78','25','36','100','14','50','2');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('79','25','35','100','14','49','2');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('80','25','34','100','14','48','2');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('81','10','100','40','14','2','22');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('82','10','100','41','14','2','23');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('83','10','100','42','14','2','24');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('84','14','1','24','10','99','42');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('85','14','1','23','10','99','41');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('86','14','1','22','10','99','40');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('87','8','65','100','10','29','2');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('88','8','66','100','10','29','2');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('89','8','67','100','10','30','2');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('90','10','31','1','8','67','99');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('91','10','30','1','8','66','99');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('92','10','29','1','8','65','99');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('93','8','68','100','10','30','2');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('94','27','51','1','10','34','99');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('95','27','52','1','10','35','99');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('96','27','53','1','10','36','99');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('97','10','36','100','27','53','2');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('98','10','34','100','27','51','2');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('99','10','33','100','27','51','2');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('100','10','37','100','27','53','2');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('101','3','50','100','1','30','2');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('102','3','51','100','1','31','2');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('103','3','52','100','1','32','2');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('104','1','30','1','3','50','99');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('105','1','31','1','3','51','99');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('106','1','32','1','3','52','99');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('107','6','100','99','3','50','55');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('108','6','100','98','3','51','55');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('109','6','100','97','3','52','55');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('110','6','100','96','3','53','55');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('111','3','53','54','6','99','96');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('113','3','51','54','6','99','98');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('114','3','50','54','6','99','99');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('115','3','52','54','6','99','97');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('117','9','1','3','6','99','74');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('118','9','1','4','6','99','75');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('119','9','1','5','6','99','76');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('120','9','1','6','6','99','77');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('121','6','100','74','9','2','3');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('122','6','100','75','9','2','4');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('123','6','100','76','9','2','5');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('124','6','100','77','9','2','6');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('125','18','98','49','16','2','49');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('126','18','98','50','16','2','50');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('127','18','98','51','16','2','51');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('128','16','1','49','18','97','49');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('129','16','1','50','18','97','50');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('130','16','1','51','18','97','51');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('131','16','100','50','19','2','46');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('132','19','1','46','16','99','50');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('133','17','49','100','16','49','2');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('134','17','50','100','16','50','2');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('135','17','51','100','16','51','2');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('136','16','49','1','17','49','99');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('137','16','50','1','17','50','99');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('138','16','51','1','17','51','99');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('141','16','50','36','36','96','99');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('142','36','96','100','16','50','37');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('143','36','97','100','16','51','37');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('144','36','95','100','16','49','37');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('149','13','92','100','11','82','2');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('150','13','93','100','11','83','2');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('151','13','94','100','11','84','2');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('152','11','82','1','13','92','99');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('153','11','83','1','13','93','99');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('154','11','84','1','13','94','99');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('155','11','87','54','11','7','98');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('156','11','6','98','11','83','52');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('157','11','6','99','11','84','52');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('158','1','21','25','36','22','15');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('159','36','21','16','1','21','27');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('162','1','23','25','36','24','15');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('163','1','22','25','36','23','15');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('164','36','22','16','1','21','27');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('165','36','23','16','1','22','27');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('166','36','24','16','1','23','27');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('169','36','6','48','1','22','48');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('170','36','7','48','1','23','48');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('171','36','5','48','1','22','48');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('172','36','8','48','1','23','48');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('173','1','22','46','36','6','46');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('174','1','23','46','36','7','46');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('182','1','12','46','36','52','13');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('183','1','13','46','36','53','13');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('185','36','51','15','1','12','48');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('186','36','52','15','1','12','48');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('187','36','53','15','1','13','48');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('188','36','54','15','1','13','48');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('190','1','54','98','36','6','98');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('191','1','55','98','36','6','98');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('193','1','56','98','36','7','98');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('194','36','5','100','1','54','100');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('195','36','6','100','1','55','100');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('196','36','7','100','1','55','100');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('197','36','8','100','1','56','100');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('200','1','61','82','36','61','81');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('201','36','60','83','1','60','84');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('202','36','61','83','1','61','84');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('203','36','62','83','1','61','84');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('204','36','63','83','1','62','84');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('210','36','6','75','1','37','81');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('211','36','7','75','1','37','81');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('212','36','8','75','1','38','81');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('213','36','5','75','1','36','81');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('214','1','37','79','36','6','73');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('215','1','60','51','36','38','46');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('216','36','38','48','1','60','53');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('217','36','39','48','1','60','53');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('218','36','40','48','1','61','53');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('219','36','37','48','1','59','53');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('220','1','59','51','36','38','46');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('223','1','61','51','36','39','46');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('224','15','9','3','15','73','36');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('225','15','73','38','15','9','5');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('226','15','5','3','15','34','36');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('227','15','34','38','15','5','5');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('228','15','35','38','15','6','5');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('230','11','10','3','11','85','98');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('231','11','9','3','11','84','98');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('232','11','11','3','11','86','98');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('233','11','85','100','11','10','5');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('234','11','84','100','11','9','5');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('236','11','86','100','11','11','5');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('238','12','1','19','11','98','21');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('239','12','1','20','11','98','22');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('240','12','1','21','11','98','23');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('255','11','100','21','12','2','19');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('256','11','100','22','12','2','20');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('257','11','100','23','12','2','21');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('259','12','86','15','12','96','97');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('260','12','96','98','12','86','14');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('261','12','97','98','12','87','15');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('263','9','1','34','9','42','19');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('264','9','41','21','9','2','29');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('265','9','42','21','9','3','29');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('266','9','43','21','9','4','29');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('275','29','50','94','35','78','17');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('276','35','77','15','29','49','93');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('277','35','78','15','29','50','93');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('278','35','79','15','29','51','93');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('279','35','79','43','1','50','61');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('280','35','80','43','1','51','61');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('282','1','40','50','36','35','98');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('283','36','35','100','1','40','51');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('284','36','36','100','1','41','51');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('288','36','37','100','1','41','51');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('289','36','34','100','1','39','51');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('290','24','37','81','24','49','94');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('291','24','49','96','24','35','81');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('292','24','64','81','24','52','94');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('293','24','52','96','24','66','81');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('294','24','96','94','29','55','15');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('295','29','53','15','24','96','92');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('296','29','48','6','24','5','92');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('297','24','5','94','29','46','6');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('298','24','37','67','24','4','45');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('299','24','4','43','24','39','67');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('300','24','64','67','24','97','45');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('301','24','97','43','24','62','67');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('302','24','56','42','24','73','40');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('303','24','73','38','24','56','40');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('304','24','28','37','24','45','40');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('305','24','45','42','24','28','39');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('307','2','99','89','2','29','25');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('308','2','29','22','2','97','90');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('309','2','9','69','2','95','8');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('310','2','91','8','2','11','73');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('311','2','92','8','2','11','73');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('312','2','92','9','2','11','73');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('313','2','91','9','2','11','73');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('314','42','42','100','8','37','3');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('315','42','43','100','8','38','3');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('316','8','37','1','42','42','98');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('317','42','44','100','8','39','3');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('318','8','38','1','42','43','98');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('319','42','45','100','8','40','3');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('320','8','39','1','42','44','98');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('321','42','46','100','8','41','3');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('322','8','40','1','42','45','98');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('323','8','41','1','42','46','98');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('324','42','54','2','43','53','98');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('325','43','53','100','42','54','4');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('326','42','63','2','43','54','98');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('327','43','54','100','42','63','4');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('328','43','55','100','42','71','4');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('329','42','71','2','43','55','98');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('361','31','76','56','31','61','53');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('362','31','60','52','31','63','53');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('363','31','62','52','31','61','54');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('364','31','60','53','31','63','54');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('365','31','62','53','31','61','55');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('366','31','60','54','31','62','55');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('367','31','61','54','31','61','56');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('368','31','60','55','31','63','56');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('369','31','62','55','31','61','57');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('370','31','60','56','31','63','57');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('371','31','62','56','31','65','54');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('372','31','64','53','31','67','54');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('381','31','68','53','31','71','54');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('385','31','66','53','31','65','55');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('386','31','64','54','31','67','55');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('387','31','66','54','31','65','56');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('388','31','64','55','31','67','56');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('389','31','66','55','31','65','57');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('390','31','64','56','31','67','57');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('391','31','66','56','31','69','54');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('392','31','70','53','31','69','55');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('393','31','68','54','31','71','55');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('394','31','70','54','31','69','56');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('395','31','68','55','31','71','56');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('396','31','70','55','31','69','57');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('397','31','68','56','31','71','57');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('398','31','70','56','31','73','53');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('399','31','72','52','31','75','53');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('400','31','74','52','31','73','54');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('401','31','72','53','31','75','54');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('403','31','74','53','31','73','55');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('404','31','72','54','31','74','55');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('407','31','73','54','31','73','56');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('408','31','72','55','31','75','56');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('409','31','74','55','31','73','57');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('410','31','72','56','31','75','57');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('411','31','74','57','31','77','54');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('413','31','77','53','31','77','55');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('414','31','74','56','31','77','54');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('415','31','76','53','31','78','54');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('416','31','76','54','31','78','56');
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
	('417','31','77','55','31','78','57');
	
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
(418, 36, 83, 12, 1, 22, 80);
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
(419, 36, 84, 12, 1, 23, 80);
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
(420, 36, 85, 12, 1, 24, 80);

INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
(421, 1, 23, 79, 36, 84, 11);

/* Sorrows Grove */
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
(422, 35, 87, 20, 32, 34, 49);
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
(423, 32, 35, 49, 35, 87, 21);
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
(424, 35, 89, 20, 32, 48, 49);
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
(425, 32, 47, 49, 35, 89, 21);

/* The Passing */
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
(426, 35, 87, 16, 33, 43, 43);
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
(427, 33, 43, 42, 35, 87, 17);
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
(428, 35, 89, 16, 33, 58, 43);
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
(429, 33, 58, 42, 35, 89, 17);

/* Winter Heights */
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
(430, 35, 87, 12, 34, 54, 49);
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
(431, 34, 54, 50, 35, 87, 13);
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
(432, 34, 55, 50, 35, 87, 13);
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
(433, 35, 89, 12, 34, 52, 60);
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
(434, 34, 52, 59, 35, 89, 13);
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
(435, 34, 53, 59, 35, 89, 13);

/* The Patriarch */
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
(436, 43, 43, 26, 44, 57, 91);
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
(437, 44, 57, 91, 43, 43, 26);

/* AD4 King Room */
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
(438, 24, 50, 4, 29, 54, 62);
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
(439, 24, 51, 4, 29, 56, 62);
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
(440, 29, 54, 63, 24, 50, 5);
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
(441, 29, 55, 63, 24, 50, 5);
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
(442, 29, 56, 63, 24, 51, 5);

/* Pumpkin Grove */
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
(443, 25, 1, 53, 38, 99, 23);
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
(444, 25, 1, 54, 38, 99, 24);
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
(445, 25, 1, 55, 38, 99, 25);
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
(446, 25, 1, 56, 38, 99, 26);
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
(447, 25, 1, 57, 38, 99, 27);
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
(448, 38, 100, 23, 25, 2, 53);
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
(449, 38, 100, 24, 25, 2, 54);
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
(450, 38, 100, 25, 25, 2, 55);
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
(451, 38, 100, 26, 25, 2, 56);
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
(452, 38, 100, 27, 25, 2, 57);

/* Haunted House */
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
(453, 38, 37, 35, 39, 5, 98);
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
(454, 38, 38, 35, 39, 6, 98);
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
(455, 38, 39, 35, 39, 7, 98);
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
(456, 39, 5, 99, 38, 37, 36);
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
(457, 39, 6, 99, 38, 38, 36);
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
(458, 39, 7, 99, 38, 39, 36);

INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
(459, 39, 40, 4, 39, 93, 99);
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
(460, 39, 41, 4, 39, 94, 99);
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
(461, 39, 42, 4, 39, 95, 99);
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
(462, 39, 93, 100, 39, 40, 5);
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
(463, 39, 94, 100, 39, 41, 5);
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
(464, 39, 95, 100, 39, 42, 5);

/* AD5 */
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
(465, 29, 59, 48, 40, 19, 98);
INSERT INTO warptiles (id, map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES
(466, 40, 17, 98, 29, 60, 48);



SET IDENTITY_INSERT warptiles OFF;