@echo off
mkdir %SYSTEMDRIVE%\iotscan
xcopy /s *.* %SYSTEMDRIVE%\iotscan
pushd %SYSTEMDRIVE%\iotscan
bootstrap.bat