CREATE TABLE IF NOT EXISTS community_goal_runs (
  goal_id INT NOT NULL,
  run INT NOT NULL,
  status TEXT NOT NULL,
  started_at TEXT NOT NULL,
  completed_at TEXT NULL,
  totals TEXT NOT NULL DEFAULT '{}',
  results TEXT NULL,
  PRIMARY KEY (goal_id, run)
);
CREATE TABLE IF NOT EXISTS community_goal_contributions (
  goal_id INT NOT NULL,
  run INT NOT NULL,
  player_id INT NOT NULL,
  amounts TEXT NOT NULL DEFAULT '{}',
  last_at TEXT NOT NULL,
  claimed_bracket INT NULL,
  claimed_at TEXT NULL,
  PRIMARY KEY (goal_id, run, player_id)
);
CREATE TABLE IF NOT EXISTS world_flags (
  name TEXT PRIMARY KEY,
  value INT NOT NULL,
  set_at TEXT NOT NULL,
  source_goal_id INT NULL
);
