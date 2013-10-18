@echo off

set f=backups\%1GooseBackup
set fz=%f%.zip

7z a -tzip %fz% %f%

echo goose> cmds.txt
echo th1s15m1ftpp455w0rd>> cmds.txt
echo put %fz%>> cmds.txt
echo quit>> cmds.txt

ftp -s:cmds.txt haydenz.ath.cx
del cmds.txt