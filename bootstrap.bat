REM WelsonJS 0.2.7 bootstrapping script
REM Source code available: https://github.com/gnh1201/welsonjs

@echo off
pushd %~dp0

:: Define variables
set TOOLKIT_URL=https://ics.catswords.net/welsonjs_toolkit.cab
set TOOLKIT_PATH=%APPDATA%\welsonjs\welsonjs_toolkit.cab
set TOOLKIT_EXTRACT_PATH=%APPDATA%\welsonjs
set REGASM_PATH=%WINDIR%\Microsoft.NET\Framework\v2.0.50727\RegAsm.exe
set LOCAL_TOOLKIT_DLL=bin\x86\WelsonJS.Toolkit.dll
set DOWNLOADED_TOOLKIT_DLL=%APPDATA%\welsonjs\WelsonJS.Toolkit.dll

:: Ensure directory exists
if not exist "%APPDATA%\welsonjs" mkdir "%APPDATA%\welsonjs"

echo [*] Initializing WelsonJS pre-configuration...

:: Register HTA file association
echo [*] Configuring HTA file association...
reg import app\assets\reg\Default_HTA.reg

:: Determine which toolkit to use
if exist "%LOCAL_TOOLKIT_DLL%" (
    echo [*] Local toolkit found. Using "%LOCAL_TOOLKIT_DLL%" for registration.
    set TOOLKIT_DLL=%LOCAL_TOOLKIT_DLL%
) else if exist "%DOWNLOADED_TOOLKIT_DLL%" (
    echo [*] Downloaded toolkit found. Using "%DOWNLOADED_TOOLKIT_DLL%" for registration.
    set TOOLKIT_DLL=%DOWNLOADED_TOOLKIT_DLL%
) else (
    echo [*] Toolkit not found locally. Downloading from external source...
    :: Download the latest WelsonJS.Toolkit component
    bitsadmin /transfer toolkit_download /download /priority normal %TOOLKIT_URL% %TOOLKIT_PATH%
    
    :: Extract the downloaded CAB file
    echo [*] Extracting WelsonJS.Toolkit component...
    expand %TOOLKIT_PATH% -F:* %TOOLKIT_EXTRACT_PATH%
    
    :: Set the downloaded DLL as the target
    set TOOLKIT_DLL=%DOWNLOADED_TOOLKIT_DLL%
)

:: Register the WelsonJS.Toolkit component
echo [*] Registering WelsonJS.Toolkit component...
%REGASM_PATH% /codebase %TOOLKIT_DLL%

:: Final step
echo [*] Pre-configuration complete. Starting bootstrap script...
cscript app.js bootstrap
