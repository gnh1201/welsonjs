@echo off

:: https://stackoverflow.com/questions/12322308/batch-file-to-check-64bit-or-32bit-os
:: https://support.microsoft.com/ko-kr/help/556009
reg Query "HKLM\Hardware\Description\System\CentralProcessor\0" | find /i "x86" > NUL && set OS=32BIT || set OS=64BIT

if %OS%==32BIT bin\streams.exe -d app.hta
if %OS%==64BIT bin\streams64.exe -d app.hta

echo done