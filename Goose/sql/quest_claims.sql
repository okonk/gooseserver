CREATE TABLE quest_claims (
  quest_id INT PRIMARY KEY,
  player_id INT NOT NULL,
  completed_at TEXT NOT NULL
);
