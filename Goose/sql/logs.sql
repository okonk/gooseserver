USE IllutiaGoose;

CREATE TABLE logs (
  text VARCHAR(255), 
  log_type INT NOT NULL, 
  playerid INT NOT NULL, 
  otherid INT, 
  mapid SMALLINT, 
  mapx SMALLINT, 
  mapy SMALLINT,
  log_date DATETIME2 NOT NULL,
);