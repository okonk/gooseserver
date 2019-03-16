USE IllutiaGoose;

DROP TABLE quests;
CREATE TABLE quests (
  id INT IDENTITY(1,1) NOT NULL,
  name TEXT NOT NULL,
  description TEXT DEFAULT '' NOT NULL,
  fail_text TEXT DEFAULT '' NOT NULL,
  pass_text TEXT DEFAULT '' NOT NULL,
  min_experience BIGINT DEFAULT 0,
  max_experience BIGINT DEFAULT 0,
  min_level INT DEFAULT 0,
  max_level INT DEFAULT 0,
  repeatable CHAR(1) DEFAULT '0',
  only_one_player_can_complete CHAR(1) DEFAULT '0',
  prerequisite_quests TEXT DEFAULT '' NOT NULL,
 
  PRIMARY KEY (id)
);

SET IDENTITY_INSERT quests ON;

INSERT INTO quests (id, name, description, pass_text, fail_text)
VALUES (1, 'Rat Infestation', 'Hey there, you look new. Why dont you help me out\n by killing some of these dirty Rats over here and i''ll point\n you towards something better?\n Kill some rats and bring back a Thread for me.', 'Good job newbie. Head North, make a right\n and go straight up to reach the lamb and sheep area. Be careful!', 'Looks like you still dont have that rat thread\n for me eh? Go kill some of these rats and find one.');

INSERT INTO quests (id, name, description, pass_text, fail_text, min_level)
VALUES (2, 'Annoying Lambs', 'These damn lambs and their annoying little bitch voices.\n Kill me 20 of these lambs and i shall reward you.', 'Good job. I can sleep a little more peaceful now.', 'How hard is it to kill 20 lambs eh? Go do that.', 2);

INSERT INTO quests (id, name, description, pass_text, fail_text, min_level, prerequisite_quests)
VALUES (3, 'Woolen Sweater', 'Hey there, I want to make me a nice woolen sweater.\n Hows about you slaughter some of these sheep and\n bring me back 10 pieces of wool?\n Theres a handy little weapon in it for ya.', 'Sweet. I''ll tell the missus to get stichin right now.', 'Still dont have those 10 pieces of wool?\n Kill some of these sheep.', 3, '2');

SET IDENTITY_INSERT quests OFF;

DROP TABLE quest_requirements;
CREATE TABLE quest_requirements (
  id INT IDENTITY(1,1) NOT NULL,
  quest_id INT NOT NULL,
  requirement_type INT NOT NULL,
  requirement_value BIGINT NOT NULL,
  requirement_value2 BIGINT DEFAULT 0,
  keep_requirement CHAR(1) DEFAULT '0',
  
  PRIMARY KEY (id)
);

INSERT INTO quest_requirements (quest_id, requirement_type, requirement_value, requirement_value2)
VALUES (1, 1, 54, 1);

INSERT INTO quest_requirements (quest_id, requirement_type, requirement_value, requirement_value2)
VALUES (2, 2, 1, 20);

INSERT INTO quest_requirements (quest_id, requirement_type, requirement_value, requirement_value2)
VALUES (3, 1, 55, 10);

DROP TABLE quest_rewards;
CREATE TABLE quest_rewards (
  id INT IDENTITY(1,1) NOT NULL,
  quest_id INT NOT NULL,
  reward_type INT NOT NULL,
  long_value BIGINT DEFAULT 0,
  long_value2 BIGINT DEFAULT 0,
  string_value TEXT DEFAULT '',
  
  PRIMARY KEY (id)
);

INSERT INTO quest_rewards (quest_id, reward_type, long_value)
VALUES (1, 5, 160);

INSERT INTO quest_rewards (quest_id, reward_type, long_value)
VALUES (1, 0, 250);

INSERT INTO quest_rewards (quest_id, reward_type, long_value)
VALUES (2, 5, 500);

INSERT INTO quest_rewards (quest_id, reward_type, long_value)
VALUES (3, 5, 1000);

INSERT INTO quest_rewards (quest_id, reward_type, long_value, long_value2)
VALUES (3, 1, 10, 1);


CREATE TABLE quest_progress (
  id INT IDENTITY(1,1) NOT NULL,
  requirement_id INT NOT NULL,
  player_id INT NOT NULL,
  progress_value BIGINT NOT NULL,
  
  PRIMARY KEY (id),
  INDEX quest_progress_player_id_idx (player_id)
);

CREATE TABLE quest_completed (
  id INT IDENTITY(1,1) NOT NULL,
  quest_id INT NOT NULL,
  player_id INT NOT NULL,
  
  PRIMARY KEY (id),
  INDEX quest_completed_player_id_idx (player_id)
);

CREATE TABLE quest_started (
  id INT IDENTITY(1,1) NOT NULL,
  quest_id INT NOT NULL,
  player_id INT NOT NULL,
  
  PRIMARY KEY (id),
  INDEX quest_started_player_id_idx (player_id)
);