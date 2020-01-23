DROP TABLE IF EXISTS quests;
CREATE TABLE quests (
  id INTEGER PRIMARY KEY,
  name TEXT NOT NULL,
  description TEXT DEFAULT '' NOT NULL,
  fail_text TEXT DEFAULT '' NOT NULL,
  pass_text TEXT DEFAULT '' NOT NULL,
  min_experience BIGINT DEFAULT 0,
  max_experience BIGINT DEFAULT 0,
  min_level INT DEFAULT 0,
  max_level INT DEFAULT 0,
  repeatable CHAR(1) DEFAULT '0',
  show_progress CHAR(1) DEFAULT '0',
  only_one_player_can_complete CHAR(1) DEFAULT '0',
  prerequisite_quests TEXT DEFAULT '' NOT NULL
);

/*
INSERT INTO quests (id, name, description, pass_text, fail_text)
VALUES (1, 'Rat Infestation', 'Hey there, you look new. Why dont you help me out\n by killing some of these dirty Rats over here and i''ll point\n you towards something better?\n Kill some rats and bring back a Thread for me.', 'Good job newbie. Head North, make a right\n and go straight up to reach the lamb and sheep area. Be careful!', 'Looks like you still dont have that rat thread\n for me eh? Go kill some of these rats and find one.');
*/

DROP TABLE IF EXISTS quest_requirements;
CREATE TABLE quest_requirements (
  id INTEGER PRIMARY KEY,
  quest_id INT NOT NULL,
  requirement_type INT NOT NULL,
  requirement_value BIGINT NOT NULL,
  requirement_value2 BIGINT DEFAULT 0,
  keep_requirement CHAR(1) DEFAULT '0'
);

/*
INSERT INTO quest_requirements (quest_id, requirement_type, requirement_value, requirement_value2)
VALUES (1, 1, 54, 1);
*/

DROP TABLE IF EXISTS quest_rewards;
CREATE TABLE quest_rewards (
  id INTEGER PRIMARY KEY,
  quest_id INT NOT NULL,
  reward_type INT NOT NULL,
  long_value BIGINT DEFAULT 0,
  long_value2 BIGINT DEFAULT 0,
  string_value TEXT DEFAULT ''
);

/*
INSERT INTO quest_rewards (quest_id, reward_type, long_value)
VALUES (1, 5, 160);

INSERT INTO quest_rewards (quest_id, reward_type, long_value)
VALUES (1, 0, 250);
*/

CREATE TABLE quest_status (
  player_id INT PRIMARY KEY NOT NULL,
  serialized_data TEXT NOT NULL
);