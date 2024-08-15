REM bootstrap.bat
REM WelsonJS 0.2.7
REM https://github.com/gnh1201/welsonjs
@echo off
pushd %~dp0

echo [*] Starting pre-configure script...

echo [*] Registering HTA file association...
reg import app\assets\reg\Default_HTA.reg

echo [*] Unlocking the performance limit of MSHTML...
reg add "HKCU\Software\Microsoft\Internet Explorer\Styles" /f
reg add "HKCU\Software\Microsoft\Internet Explorer\Styles" /v "MaxScriptStatements" /t REG_DWORD /d 0xFFFFFFFF /f

rem echo [*] Registering AutoItX component...
rem regsvr32 /s "%PROGRAMFILES(X86)%\AutoIt3\AutoItX\AutoItX3.dll"
rem regsvr32 /s "%PROGRAMFILES(X86)%\AutoIt3\AutoItX\AutoItX3_x64.dll"

echo [*] Registering WelsonJS.Toolkit component...
%WINDIR%\Microsoft.NET\Framework\v2.0.50727\RegAsm.exe /codebase bin\x86\WelsonJS.Toolkit.dll
REM %WINDIR%\Microsoft.NET\Framework64\v2.0.50727\RegAsm.exe /codebase bin\x64\WelsonJS.Toolkit.dll

echo [*] Done.

cscript app.js bootstrap
