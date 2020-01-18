DROP TABLE IF EXISTS combination_item_required;
CREATE TABLE combination_item_required (
	combination_id INT NOT NULL,
	item_template_id INT NOT NULL
);

DROP TABLE IF EXISTS combination_item_results;
CREATE TABLE combination_item_results (
	combination_id INT NOT NULL,
	item_template_id INT NOT NULL
);

DROP TABLE IF EXISTS combinations;
CREATE TABLE combinations (
	combination_id INTEGER PRIMARY KEY,
	combination_name VARCHAR(64) NOT NULL,
	min_level INT DEFAULT 1 NOT NULL,
	max_level INT DEFAULT 50 NOT NULL,
	min_experience BIGINT DEFAULT 0 NOT NULL,
	max_experience BIGINT DEFAULT 0 NOT NULL,
	class_restrictions BIGINT DEFAULT 0 NOT NULL
);