REM startInteractiveService.bat
REM WelsonJS 0.2.7
REM https://github.com/gnh1201/welsonjs
@echo off

REM Get the current directory
set CURRENT_DIR=%~dp0
set CURRENT_DIR=%CURRENT_DIR:~0,-1%

REM Set the paths
set EXE_PATH=%CURRENT_DIR%\bin\x86\WelsonJS.Service.exe

"%EXE_PATH%" --working-directory=%CURRENT_DIR% --script-name=defaultService
