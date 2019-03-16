USE IllutiaGoose;


CREATE INDEX inventory_player_id_idx ON inventory (player_id);
CREATE INDEX equipped_player_id_idx ON equipped (player_id);
CREATE INDEX combinebag_player_id_idx ON combinebag (player_id);
CREATE INDEX spellbook_player_id_idx ON spellbook (player_id);

ALTER TABLE inventory ADD PRIMARY KEY CLUSTERED (player_id ASC, slot ASC);
ALTER TABLE equipped ADD PRIMARY KEY CLUSTERED (player_id ASC, slot ASC);
ALTER TABLE combinebag ADD PRIMARY KEY CLUSTERED (player_id ASC, slot ASC);
ALTER TABLE spellbook ADD PRIMARY KEY CLUSTERED (player_id ASC, slot ASC);

CREATE INDEX quest_progress_player_id_idx ON quest_progress (player_id);
CREATE INDEX quest_completed_player_id_idx ON quest_completed (player_id);
CREATE INDEX quest_started_player_id_idx ON quest_started (player_id);

CREATE INDEX pets_owner_id_idx ON pets (owner_id);

CREATE INDEX guild_members_guild_id_idx ON guild_members (guild_id);
ALTER TABLE guild_members ADD PRIMARY KEY CLUSTERED (guild_id ASC, player_id ASC); -- not currently active, could be dodgy since DB contains multiple values for guild_id, player_id already which violates the index