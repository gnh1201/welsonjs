@echo off

reg add "HKCU\Software\Microsoft\Internet Explorer\Styles" /f
reg add "HKCU\Software\Microsoft\Internet Explorer\Styles" /v "MaxScriptStatements" /t REG_DWORD /d 0xFFFFFFFF /f
