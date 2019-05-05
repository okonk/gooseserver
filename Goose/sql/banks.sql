USE IllutiaGoose;

--DROP TABLE bank_items;
CREATE TABLE bank_items (
  npc_id INT NOT NULL,
  player_id INT NOT NULL,
  serialized_data TEXT NOT NULL,
  
  PRIMARY KEY(npc_id, player_id)
);

CREATE INDEX bank_items_player_id_idx ON bank_items (player_id);