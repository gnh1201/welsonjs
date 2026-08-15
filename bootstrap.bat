REM WelsonJS 0.2.7 bootstrapping script
REM Source code available: https://github.com/gnh1201/welsonjs

@echo off
pushd %~dp0

:: Define variables
set MANAGEDOBJECT_URL=https://catswords.blob.core.windows.net/welsonjs/welsonjs_managedobject_latest.cab
set MANAGEDOBJECT_PATH=%APPDATA%\welsonjs\welsonjs_managedobject_latest.cab
set MANAGEDOBJECT_EXTRACT_PATH=%APPDATA%\welsonjs
set REGASM_PATH=%WINDIR%\Microsoft.NET\Framework\v2.0.50727\RegAsm.exe
set REGASM_PATH64=%WINDIR%\Microsoft.NET\Framework64\v2.0.50727\RegAsm.exe
set LOCAL_MANAGEDOBJECT_DLL=bin\x86\WelsonJS.ManagedObject.dll
set DOWNLOADED_MANAGEDOBJECT_DLL=%APPDATA%\welsonjs\WelsonJS.ManagedObject.dll

:: Ensure directory exists
if not exist "%APPDATA%\welsonjs" mkdir "%APPDATA%\welsonjs"

echo [*] Initializing WelsonJS pre-configuration...

:: Register HTA file association
echo [*] Configuring HTA file association...
reg import app\assets\reg\Default_HTA.reg

:: Determine which managed object to use
if exist "%LOCAL_MANAGEDOBJECT_DLL%" (
    echo [*] Local managed object found. Using "%LOCAL_MANAGEDOBJECT_DLL%" for registration.
    set MANAGEDOBJECT_DLL=%LOCAL_MANAGEDOBJECT_DLL%
) else if exist "%DOWNLOADED_MANAGEDOBJECT_DLL%" (
    echo [*] Downloaded managed object found. Using "%DOWNLOADED_MANAGEDOBJECT_DLL%" for registration.
    set MANAGEDOBJECT_DLL=%DOWNLOADED_MANAGEDOBJECT_DLL%
) else (
    echo [*] Managed object not found locally. Downloading from external source...
    :: Download the latest WelsonJS.ManagedObject component
    bitsadmin /transfer managedobject_download /download /priority normal %MANAGEDOBJECT_URL% %MANAGEDOBJECT_PATH%
    
    :: Extract the downloaded CAB file
    echo [*] Extracting WelsonJS.ManagedObject component...
    expand %MANAGEDOBJECT_PATH% -F:* %MANAGEDOBJECT_EXTRACT_PATH%
    
    :: Set the downloaded DLL as the target
    set MANAGEDOBJECT_DLL=%DOWNLOADED_MANAGEDOBJECT_DLL%
)

:: Final step
if /I "%PROCESSOR_ARCHITECTURE%%PROCESSOR_ARCHITEW6432%"=="x86" (
    rem 32-bit Windows

    echo [*] Registering WelsonJS.ManagedObject component...
    %REGASM_PATH% /codebase %MANAGEDOBJECT_DLL%
    
    echo [*] Pre-configuration complete. Starting bootstrap script...
    cscript.exe app.js bootstrap
) else (
    rem 64-bit Windows

    echo [*] Registering WelsonJS.ManagedObject component...
    %REGASM_PATH% /codebase %MANAGEDOBJECT_DLL%
    %REGASM_PATH64% /codebase %MANAGEDOBJECT_DLL%

    echo [*] Pre-configuration complete. Starting bootstrap script...
    %SystemRoot%\SysWOW64\cscript.exe app.js bootstrap
)
