@echo off

REM set required build-tools
set PATH=%WINDIR%\System32
set PATH=%PATH%;%CD%\packages\go1.14.6.windows-amd64\go\bin
set PATH=%PATH%;%CD%\packages\git-2.28.0-64-bit\bin
set PATH=%PATH%;%CD%\packages\winlibs-x86_64-posix-seh-gcc-10.2.0-llvm-10.0.0-mingw-w64-7.0.0-r2\mingw64\bin
set GOPATH=%CD%\packages\build\go

REM check a PATH variable
echo %PATH%

REM check a GOPATH variable
echo %GOPATH%

REM build a WinDivert handler
go get -v -ldflags="-s -w" -trimpath -tags=wintun github.com/gnh1201/shadow/executive/shadow

REM set destination folder
set BINPATH=%CD%\bin
rmdir %BINPATH% /s /q
mkdir %BINPATH%

REM copy required files
copy %CD%\packages\shadowsocks-libev-mingw-x86_64\ss-local.exe %BINPATH%\ss-local.exe
copy %CD%\packages\shadowsocks-libev-mingw-x86_64\*.dll %BINPATH%\
copy %CD%\packages\WinDivert-2.2.0-A\x64\WinDivert.dll %BINPATH%\WinDivert.dll
copy %CD%\packages\WinDivert-2.2.0-A\x64\WinDivert64.sys %BINPATH%\WinDivert64.sys
copy %CD%\packages\tun2socks-windows-4.0-amd64.exe %BINPATH%\tun2socks.exe
copy %CD%\packages\tap-windows-9.24.2-I601-Win10.exe %BINPATH%\tap-windows-9.24.2-I601-Win10.exe
copy %GOPATH%\bin\shadow.exe %BINPATH%\shadow.exe

echo done
