USE Goose;

CREATE TABLE guilds (
  guild_id INT IDENTITY(1,1) NOT NULL,
  guild_name VARCHAR(64) NOT NULL,
  guild_motd VARCHAR(256) DEFAULT '' NOT NULL,
  
  PRIMARY KEY (guild_id)
);

CREATE TABLE guild_members (
	guild_id INT NOT NULL,
	player_id INT NOT NULL,
	guild_rank SMALLINT DEFAULT 1 NOT NULL,
)