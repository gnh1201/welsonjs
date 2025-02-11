REM installService.bat
REM WelsonJS 0.2.7
REM https://github.com/gnh1201/welsonjs
@echo off

REM Set the service name
set SERVICE_NAME=WelsonJS.Service

REM Get the current directory
set CURRENT_DIR=%~dp0
set CURRENT_DIR=%CURRENT_DIR:~0,-1%

REM Set the paths
if "%PROCESSOR_ARCHITECTURE%"=="x86" (
    set EXE_PATH=%CURRENT_DIR%\bin\x86\WelsonJS.Service.exe
) else if "%PROCESSOR_ARCHITECTURE%"=="AMD64" (
    set EXE_PATH=%CURRENT_DIR%\bin\x64\WelsonJS.Service.exe
) else (
    echo Unsupported platform: %PROCESSOR_ARCHITECTURE%
    exit /b 1
)
set INSTALL_UTIL_PATH=%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\InstallUtil.exe

REM Uninstall the service
sc stop "%SERVICE_NAME%"
"%INSTALL_UTIL_PATH%" /u "%EXE_PATH%"

REM Install the service
"%INSTALL_UTIL_PATH%" "%EXE_PATH%"

REM Set service to auto start and add the required arguments
REM sc config "%SERVICE_NAME%" start= auto
REM sc description "%SERVICE_NAME%" "Service installed via InstallUtil.exe"

REM Add parameters to the ImagePath registry key
REG ADD "HKLM\SYSTEM\CurrentControlSet\Services\%SERVICE_NAME%" /v ImagePath /t REG_EXPAND_SZ /d "\"%EXE_PATH%\" --working-directory=%CURRENT_DIR% --script-name=defaultService" /f

echo Service "%SERVICE_NAME%" installed and configured successfully.

sc start "%SERVICE_NAME%"
echo Service "%SERVICE_NAME%" started.
pause