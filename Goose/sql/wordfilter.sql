USE IllutiaGoose;

DROP TABLE wordfilter;

CREATE TABLE wordfilter (
  word VARCHAR(32) NOT NULL,
  filtered VARCHAR(32) NOT NULL
);

/*INSERT INTO wordfilter VALUES ('raped', '*****');
INSERT INTO wordfilter VALUES ('rapes', '*****');
INSERT INTO wordfilter VALUES ('rape', '****');
INSERT INTO wordfilter VALUES ('raping', '******');
INSERT INTO wordfilter VALUES ('fuck', '****');
INSERT INTO wordfilter VALUES ('nigger', '******');
INSERT INTO wordfilter VALUES ('nigga', '*****');
INSERT INTO wordfilter VALUES ('cunt', '****');*/