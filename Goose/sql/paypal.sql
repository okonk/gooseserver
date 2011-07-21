USE Goose;

CREATE TABLE paypal_logs (
  request TEXT NOT NULL
);

CREATE TABLE paypal_payments (
  txn_id VARCHAR(17) NOT NULL,
  player_name VARCHAR(50) NOT NULL, 
  credits SMALLINT NOT NULL, 
  price DECIMAL(5, 2) NOT NULL, 
  redeemed CHAR(1) DEFAULT '0' NOT NULL,
  
  PRIMARY KEY (txn_id)
);