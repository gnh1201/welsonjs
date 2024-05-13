@echo off
rem preconfigure.bat
rem Namhyeon Go (gnh1201@gmail.com)
rem https://github.com/gnh1201/welsonjs

pushd %~dp0
echo [*] Starting WelsonJS pre-configure script...

echo [*] Registering HTA file association...
reg import Default_HTA.reg

echo [*] Unlocking the performance limit of the GUI(MSHTML) environment...
reg add "HKCU\Software\Microsoft\Internet Explorer\Styles" /f
reg add "HKCU\Software\Microsoft\Internet Explorer\Styles" /v "MaxScriptStatements" /t REG_DWORD /d 0xFFFFFFFF /f

echo [*] Registering AutoItX component...
regsvr32 /s "%PROGRAMFILES(X86)%\AutoIt3\AutoItX\AutoItX3.dll"
regsvr32 /s "%PROGRAMFILES(X86)%\AutoIt3\AutoItX\AutoItX3_x64.dll"

echo [*] Registering WelsonJS native component...
%WINDIR%\Microsoft.NET\Framework\v2.0.50727\RegAsm.exe /codebase bin\x86\WelsonJS.Toolkit.dll
%WINDIR%\Microsoft.NET\Framework64\v2.0.50727\RegAsm.exe /codebase bin\x64\WelsonJS.Toolkit.dll

echo Done
