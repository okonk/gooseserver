USE IllutiaGoose;

DROP TABLE warptiles;
CREATE TABLE warptiles (
  id INT IDENTITY(1,1) NOT NULL,
  map_id SMALLINT NOT NULL,
  map_x SMALLINT NOT NULL,
  map_y SMALLINT NOT NULL,
  warp_id SMALLINT NOT NULL,
  warp_x SMALLINT NOT NULL,
  warp_y SMALLINT NOT NULL,
  PRIMARY KEY (id)
);

SET IDENTITY_INSERT warptiles ON;

SET IDENTITY_INSERT warptiles OFF;