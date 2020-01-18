DROP TABLE IF EXISTS warptiles;
CREATE TABLE warptiles (
  map_id SMALLINT NOT NULL,
  map_x SMALLINT NOT NULL,
  map_y SMALLINT NOT NULL,
  warp_id SMALLINT NOT NULL,
  warp_x SMALLINT NOT NULL,
  warp_y SMALLINT NOT NULL
);

CREATE INDEX warptiles_map_id_idx ON warptiles(map_id);