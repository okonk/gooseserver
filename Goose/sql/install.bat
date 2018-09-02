@echo off

set server=localhost\sqlexpress

osql -S %server% -E -i create.sql

osql -S %server% -U GooseServer -P password1 -i players.sql
osql -S %server% -U GooseServer -P password1 -i guilds.sql
osql -S %server% -U GooseServer -P password1 -i combinations.sql
osql -S %server% -U GooseServer -P password1 -i classes.sql
osql -S %server% -U GooseServer -P password1 -i items.sql
osql -S %server% -U GooseServer -P password1 -i spells.sql
osql -S %server% -U GooseServer -P password1 -i npcs.sql
osql -S %server% -U GooseServer -P password1 -i warptiles.sql
osql -S %server% -U GooseServer -P password1 -i maps.sql
osql -S %server% -U GooseServer -P password1 -i wordfilter.sql
osql -S %server% -U GooseServer -P password1 -i pets.sql
osql -S %server% -U GooseServer -P password1 -i paypal.sql
osql -S %server% -U GooseServer -P password1 -i logs.sql
osql -S %server% -U GooseServer -P password1 -i quests.sql
osql -S %server% -U GooseServer -P password1 -i banks.sql