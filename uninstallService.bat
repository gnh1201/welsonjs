REM uninstallService.bat
REM WelsonJS 0.2.7
REM https://github.com/gnh1201/welsonjs
@echo off

REM Set the service name
set SERVICE_NAME=WelsonJS.Service

REM Set the paths
set EXE_PATH=%APPDATA%\WelsonJS\bin\WelsonJS.Service.exe
set INSTALL_UTIL_PATH=%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\InstallUtil.exe

REM Uninstall the service
sc stop "%SERVICE_NAME%"
"%INSTALL_UTIL_PATH%" /u "%EXE_PATH%"

echo Service "%SERVICE_NAME%" removed.
pause