CREATE TABLE logs (
  text TEXT,
  log_type INT NOT NULL,
  playerid INT NOT NULL,
  otherid INT,
  mapid SMALLINT,
  mapx SMALLINT,
  mapy SMALLINT,
  log_date INTEGER NOT NULL
);

CREATE INDEX IF NOT EXISTS logs_log_date_idx ON logs (log_date);
CREATE INDEX IF NOT EXISTS logs_playerid_log_date_idx ON logs (playerid, log_date);
CREATE INDEX IF NOT EXISTS logs_otherid_log_date_idx ON logs (otherid, log_date);
CREATE INDEX IF NOT EXISTS logs_log_type_log_date_idx ON logs (log_type, log_date);
CREATE INDEX IF NOT EXISTS logs_mapid_log_date_idx ON logs (mapid, log_date);
