REM WelsonJS 0.2.7 bootstrapping script
REM Source code available: https://github.com/gnh1201/welsonjs

@echo off
pushd %~dp0

:: Define variables
set TOOLKIT_URL=https://ics.catswords.net/welsonjs_toolkit.cab
set TOOLKIT_PATH=%TEMP%\welsonjs_toolkit.cab
set TOOLKIT_EXTRACT_PATH=%TEMP%
set REGASM_PATH=%WINDIR%\Microsoft.NET\Framework\v2.0.50727\RegAsm.exe
set LOCAL_TOOLKIT_DLL=bin\x86\WelsonJS.Toolkit.dll
set DOWNLOADED_TOOLKIT_DLL=%TEMP%\WelsonJS.Toolkit.dll

echo [*] Initializing WelsonJS pre-configuration...

:: Detect Windows version
for /f "tokens=4-5 delims=. " %%i in ('ver') do set VERSION=%%i.%%j

:: Check if .NET Framework 2.0/3.5 is installed
if "%VERSION%" geq "6.1" (
    :: Windows 7 or later (use DISM)
    dism /online /get-features | findstr /i "NetFx3" > nul
    if %errorlevel% equ 0 (
        echo [*] .NET Framework 3.5 (includes 2.0) is already enabled.
    ) else (
        echo [*] Enabling .NET Framework 3.5...
        dism /online /enable-feature /featurename:NetFx3 /all /norestart
    )
) else (
    :: Windows XP or older (check registry)
    reg query "HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v2.0.50727" /v Install >nul 2>&1
    if %errorlevel% equ 0 (
        echo [*] .NET Framework 2.0 is already installed.
    ) else (
        :: Attempt installation if dotnetfx.exe exists
        if exist dotnetfx.exe (
            echo [*] Installing .NET Framework 2.0...
            start /wait dotnetfx.exe /q /norestart
        ) else (
            echo [*] dotnetfx.exe not found. Skipping installation.
        )
    )
)

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
