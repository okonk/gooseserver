@echo off

osql -U GooseServer -P password1 -S localhost\sqlexpress -i combinations.sql
osql -U GooseServer -P password1 -S localhost\sqlexpress -i classes.sql
osql -U GooseServer -P password1 -S localhost\sqlexpress -i items.sql
osql -U GooseServer -P password1 -S localhost\sqlexpress -i spells.sql
osql -U GooseServer -P password1 -S localhost\sqlexpress -i npcs.sql
osql -U GooseServer -P password1 -S localhost\sqlexpress -i warptiles.sql
osql -U GooseServer -P password1 -S localhost\sqlexpress -i maps.sql
osql -U GooseServer -P password1 -S localhost\sqlexpress -i wordfilter.sql