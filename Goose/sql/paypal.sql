CREATE TABLE paypal_logs (
  request TEXT NOT NULL
);

CREATE TABLE paypal_payments (
  txn_id TEXT NOT NULL,
  player_name TEXT NOT NULL, 
  credits SMALLINT NOT NULL, 
  price DECIMAL(5, 2) NOT NULL, 
  redeemed CHAR(1) DEFAULT '0' NOT NULL,
  
  PRIMARY KEY (txn_id)
);