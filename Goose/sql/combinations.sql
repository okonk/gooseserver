USE IllutiaGoose;

DROP TABLE combination_item_required;
CREATE TABLE combination_item_required (
	combination_id INT NOT NULL,
	item_template_id INT NOT NULL,
);

DROP TABLE combination_item_results;
CREATE TABLE combination_item_results (
	combination_id INT NOT NULL,
	item_template_id INT NOT NULL,
);

DROP TABLE combinations;
CREATE TABLE combinations (
	combination_id INT IDENTITY(1,1) NOT NULL,
	combination_name VARCHAR(64) NOT NULL,
	min_level INT DEFAULT 1 NOT NULL,
	max_level INT DEFAULT 50 NOT NULL,
	min_experience BIGINT DEFAULT 0 NOT NULL,
	max_experience BIGINT DEFAULT 0 NOT NULL,
	class_restrictions BIGINT DEFAULT 0 NOT NULL,
	
	PRIMARY KEY(combination_id)
);

SET IDENTITY_INSERT combinations ON;


SET IDENTITY_INSERT combinations OFF;