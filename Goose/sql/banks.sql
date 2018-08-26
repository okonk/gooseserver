USE Goose;

--DROP TABLE bank_items;
CREATE TABLE bank_items (
  npc_id INT NOT NULL,
  player_id INT NOT NULL,
  item_id INT NOT NULL,
  slot SMALLINT NOT NULL,
  stack INT NOT NULL,
  
  PRIMARY KEY(npc_id, player_id, slot)
);

CREATE INDEX bank_items_player_id_idx ON bank_items (player_id);

