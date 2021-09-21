@echo off

REM set required build-tools
set PATH=%WINDIR%\System32
REM set PATH=%PATH%;%CD%\packages\go1.14.6.windows-amd64\go\bin
set PATH=%PATH%;%CD%\packages\git-2.28.0-64-bit\bin
set PATH=%PATH%;%CD%\packages\winlibs-x86_64-posix-seh-gcc-10.2.0-llvm-10.0.0-mingw-w64-7.0.0-r2\mingw64\bin
set GOPATH=%CD%\packages\build\go

REM check a PATH variable
echo %PATH%

REM check a GOPATH variable
REM echo %GOPATH%

REM set destination folder
set BINPATH=%CD%\bin
rmdir %BINPATH% /s /q
mkdir %BINPATH%

REM INSERT YOUR CODE

echo done
