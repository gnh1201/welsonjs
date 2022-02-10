@echo off
regsvr32 /s "%PROGRAMFILES(X86)%\AutoIt3\AutoItX\AutoItX3.dll"
regsvr32 /s "%PROGRAMFILES(X86)%\AutoIt3\AutoItX\AutoItX3_x64.dll"
%WINDIR%\Microsoft.NET\Framework\v2.0.50727\RegAsm.exe /codebase WelsonJS.Toolkit.dll
%WINDIR%\Microsoft.NET\Framework64\v2.0.50727\RegAsm.exe /codebase WelsonJS.Toolkit.dll
pause