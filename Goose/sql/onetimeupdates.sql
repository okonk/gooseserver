USE Goose;

ALTER TABLE players ADD total_playtime BIGINT DEFAULT 0 NOT NULL;
ALTER TABLE players ADD total_afktime BIGINT DEFAULT 0 NOT NULL;