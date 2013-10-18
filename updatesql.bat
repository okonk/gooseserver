@echo off

del items.sql
del spells.sql
del npcs.sql
del warptiles.sql
del maps.sql
del wordfilter.sql
del onetimeupdates.sql
del combinations.sql

wget ftp://goose:th1s15m1ftpp455w0rd@haydenz.ath.cx/trunk/Goose/sql/items.sql
wget ftp://goose:th1s15m1ftpp455w0rd@haydenz.ath.cx/trunk/Goose/sql/spells.sql
wget ftp://goose:th1s15m1ftpp455w0rd@haydenz.ath.cx/trunk/Goose/sql/npcs.sql
wget ftp://goose:th1s15m1ftpp455w0rd@haydenz.ath.cx/trunk/Goose/sql/warptiles.sql
wget ftp://goose:th1s15m1ftpp455w0rd@haydenz.ath.cx/trunk/Goose/sql/maps.sql
wget ftp://goose:th1s15m1ftpp455w0rd@haydenz.ath.cx/trunk/Goose/sql/wordfilter.sql
wget ftp://goose:th1s15m1ftpp455w0rd@haydenz.ath.cx/trunk/Goose/sql/onetimeupdates.sql
wget ftp://goose:th1s15m1ftpp455w0rd@haydenz.ath.cx/trunk/Goose/sql/combinations.sql

osql -U gooseserver -P password1 -S localhost\sqlexpress -i items.sql
osql -U gooseserver -P password1 -S localhost\sqlexpress -i spells.sql
osql -U gooseserver -P password1 -S localhost\sqlexpress -i npcs.sql
osql -U gooseserver -P password1 -S localhost\sqlexpress -i warptiles.sql
osql -U gooseserver -P password1 -S localhost\sqlexpress -i maps.sql
osql -U gooseserver -P password1 -S localhost\sqlexpress -i wordfilter.sql
osql -U gooseserver -P password1 -S localhost\sqlexpress -i combinations.sql