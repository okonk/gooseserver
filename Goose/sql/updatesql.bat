@echo off

osql -U GooseServer -P password1 -i combinations.sql
osql -U GooseServer -P password1 -i classes.sql
osql -U GooseServer -P password1 -i items.sql
osql -U GooseServer -P password1 -i spells.sql
osql -U GooseServer -P password1 -i npcs.sql
osql -U GooseServer -P password1 -i warptiles.sql
osql -U GooseServer -P password1 -i maps.sql
osql -U GooseServer -P password1 -i wordfilter.sql