@echo off
xcopy /s welsonjs\* %SYSTEMDRIVE%\welsonjs\
pushd %SYSTEMDRIVE%\welsonjs
bootstrap.bat
