USE Goose;

CREATE TABLE players (
  player_id INT NOT NULL,
  player_name VARCHAR(50) NOT NULL,
  player_title VARCHAR(50) DEFAULT '' NOT NULL,
  player_surname VARCHAR(50) DEFAULT '' NOT NULL,
  password_hash CHAR(32) NOT NULL,
  password_salt VARCHAR(50) NOT NULL,
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
  
  PRIMARY KEY(player_id)
);

CREATE TABLE inventory (
  player_id INT NOT NULL,
  item_id INT NOT NULL,
  slot SMALLINT NOT NULL,
  stack INT NOT NULL,
);

SELECT * FROM players;

SELECT * FROM inventory;

CREATE TABLE equipped (
  player_id INT NOT NULL,
  item_id INT NOT NULL,
  slot SMALLINT NOT NULL
);

SELECT * FROM equipped;

CREATE TABLE combinebag (
  player_id INT NOT NULL,
  item_id INT NOT NULL,
  slot SMALLINT NOT NULL,
  stack INT NOT NULL
);

DROP TABLE spellbook
CREATE TABLE spellbook (
  player_id INT NOT NULL,
  spell_id INT NOT NULL,
  slot SMALLINT NOT NULL,
  last_casted BIGINT DEFAULT 0 NOT NULL
);

GO
DROP PROCEDURE AddPlayerSpells
GO
CREATE PROCEDURE AddPlayerSpells @PlayerName VARCHAR(32) AS

DECLARE @PlayerID INT
DECLARE @PlayerClass INT
DECLARE @PlayerLevel INT
SELECT @PlayerID=player_id, @PlayerClass=class_id, @PlayerLevel=player_level 
FROM players WHERE player_name=@PlayerName

DECLARE @SpellClass INT
DECLARE @SpellLevel INT
DECLARE @SpellID INT

DECLARE @Counter INT = 1

DECLARE ClassSpellCursor CURSOR FOR (SELECT class_id, level, spell_id FROM classes_levelup_spells)
OPEN ClassSpellCursor

FETCH NEXT FROM ClassSpellCursor INTO @SpellClass, @SpellLevel, @SpellID
WHILE @@FETCH_STATUS = 0
BEGIN

	IF (@PlayerClass = @SpellClass) AND (@PlayerLevel >= @SpellLevel) BEGIN
		INSERT INTO spellbook (player_id, spell_id, slot) VALUES (@PlayerID, @SpellID, @Counter)
		SET @Counter = @Counter + 1
	END

	FETCH NEXT FROM ClassSpellCursor INTO @SpellClass, @SpellLevel, @SpellID
END

CLOSE ClassSpellCursor
DEALLOCATE ClassSpellCursor

GO
SELECT (experience+experience_sold) AS exp,player_name,class_id FROM players WHERE access_status=2 ORDER BY exp;

UPDATE players SET experience=experience+experience_sold,experience_sold=0,player_hp=0,player_mp=0 WHERE player_name='Magus'

UPDATE players SET player_level=25,experience=520000,player_gold=50000,class_id=4 WHERE player_name=''

UPDATE players SET player_name='Solo' WHERE player_name='Conflict'

SELECT player_name,(experience + experience_sold) AS expz FROM players WHERE player_level=50 ORDER BY expz DESC
SELECT * FROM players WHERE player_name='Cipher'

EXEC AddPlayerSpells ''

SELECT * FROM inventory WHERE player_id=4 AND slot=30
UPDATE items SET graphic_tile=120048,graphic_equip=11,graphic_r=0,graphic_g=255,graphic_b=0,graphic_a=200,item_name='Stunnah Shades' WHERE item_id=35967

DELETE FROM spellbook WHERE player_id=112 AND slot IN (1,2,3,4,5)

RESTORE DATABASE Goose FROM Disk='I:\gooseserver\20100505GooseBackup' with recovery