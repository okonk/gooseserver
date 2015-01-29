@echo off

osql -E -i create.sql

osql -U GooseServer -P password1 -i players.sql
osql -U GooseServer -P password1 -i guilds.sql
osql -U GooseServer -P password1 -i combinations.sql
osql -U GooseServer -P password1 -i classes.sql
osql -U GooseServer -P password1 -i items.sql
osql -U GooseServer -P password1 -i spells.sql
osql -U GooseServer -P password1 -i npcs.sql
osql -U GooseServer -P password1 -i warptiles.sql
osql -U GooseServer -P password1 -i maps.sql
osql -U GooseServer -P password1 -i wordfilter.sql
osql -U GooseServer -P password1 -i pets.sql
osql -U GooseServer -P password1 -i paypal.sql
osql -U GooseServer -P password1 -i logs.sql
osql -U GooseServer -P password1 -i quests.sql