@echo off

set f=C:\Goose\backups\%1GooseBackup

echo BACKUP DATABASE Goose TO DISK='%f%' WITH FORMAT> cmds.txt
echo GO>> cmds.txt

osql -U gooseserver -P password1 -S localhost\sqlexpress -i cmds.txt

del cmds.txt