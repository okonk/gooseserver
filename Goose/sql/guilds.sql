CREATE TABLE guilds (
  guild_id INTEGER PRIMARY KEY,
  guild_name TEXT NOT NULL,
  guild_motd TEXT DEFAULT '' NOT NULL
);

CREATE TABLE guild_members (
	guild_id INT NOT NULL,
	player_id INT NOT NULL,
	guild_rank SMALLINT DEFAULT 1 NOT NULL,

	PRIMARY KEY (guild_id, player_id)
);

CREATE INDEX guild_members_guild_id_idx ON guild_members(guild_id); -- not currently active, could be dodgy since DB contains multiple values for guild_id, player_id already which violates the index
