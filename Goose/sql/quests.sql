DROP TABLE quests;
CREATE TABLE quests (
  id INT IDENTITY(1,1) NOT NULL,
  name TEXT NOT NULL,
  description TEXT DEFAULT '' NOT NULL,
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



SET IDENTITY_INSERT quests OFF;

DROP TABLE quest_requirements;
CREATE TABLE quest_requirements (
  id INT IDENTITY(1,1) NOT NULL,
  quest_id INT NOT NULL,
  requirement_type INT NOT NULL,
  requirement_value BIGINT NOT NULL,
  requirement_value2 BIGINT NOT NULL,
  keep_requirement CHAR(1) DEFAULT '0',
  
  PRIMARY KEY (id)
);

DROP TABLE quest_rewards;
CREATE TABLE quest_rewards (
  id INT IDENTITY(1,1) NOT NULL,
  quest_id INT NOT NULL,
  reward_type INT NOT NULL,
  long_value BIGINT,
  string_value TEXT,
  
  PRIMARY KEY (id)
);

SET IDENTITY_INSERT quest_rewards ON;



SET IDENTITY_INSERT quest_rewards OFF;

CREATE TABLE quest_progress (
  id INT IDENTITY(1,1) NOT NULL,
  requirement_id INT NOT NULL,
  player_id INT NOT NULL,
  progress_value BIGINT NOT NULL,
  
  PRIMARY KEY (id)
);

CREATE TABLE quest_completed (
  id INT IDENTITY(1,1) NOT NULL,
  quest_id INT NOT NULL,
  player_id INT NOT NULL,
  
  PRIMARY KEY (id)
);

CREATE TABLE quest_started (
  id INT IDENTITY(1,1) NOT NULL,
  quest_id INT NOT NULL,
  player_id INT NOT NULL,
  
  PRIMARY KEY (id)
);