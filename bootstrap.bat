REM bootstrap.bat - WelsonJS 0.2.7
REM Repository: https://github.com/gnh1201/welsonjs

@echo off
pushd %~dp0

:: Define variables
set TOOLKIT_URL=https://ics.catswords.net/welsonjs_toolkit.cab
set TOOLKIT_PATH=%TEMP%\welsonjs_toolkit.cab
set TOOLKIT_EXTRACT_PATH=%TEMP%
set REGASM_PATH=%WINDIR%\Microsoft.NET\Framework\v2.0.50727\RegAsm.exe
set TOOLKIT_DLL=%TEMP%\WelsonJS.Toolkit.dll

echo [*] Initializing WelsonJS pre-configuration...

:: Register HTA file association
echo [*] Configuring HTA file association...
reg import app\assets\reg\Default_HTA.reg

:: Unlock performance limit for MSHTML
echo [*] Unlocking MSHTML performance limits...
reg add "HKCU\Software\Microsoft\Internet Explorer\Styles" /f
reg add "HKCU\Software\Microsoft\Internet Explorer\Styles" /v "MaxScriptStatements" /t REG_DWORD /d 0xFFFFFFFF /f

:: Check if TOOLKIT_DLL exists
if exist "%TOOLKIT_DLL%" (
    echo [*] Toolkit already exists. Skipping download and extraction.
) else (
    :: Download the latest WelsonJS.Toolkit component
    echo [*] Downloading the latest WelsonJS.Toolkit component...
    bitsadmin /transfer toolkit_download /download /priority normal %TOOLKIT_URL% %TOOLKIT_PATH%
    
    :: Extract the downloaded CAB file
    echo [*] Extracting WelsonJS.Toolkit component...
    expand %TOOLKIT_PATH% -F:* %TOOLKIT_EXTRACT_PATH%
)

:: Register the WelsonJS.Toolkit component
echo [*] Registering WelsonJS.Toolkit component...
%REGASM_PATH% /codebase %TOOLKIT_DLL%

:: Final step
echo [*] Pre-configuration complete. Starting bootstrap script...
cscript app.js bootstrap
