@echo off
title Remove Chrome policies created by potentially unwanted programs
color 0C

echo [INFO] This script must be run with administrator privileges.
echo [INFO] It will remove Chrome policies, enrollment tokens, forced extensions, and user profiles.
echo.

:: Step 1: Remove policy-related registry keys
echo [STEP 1] Removing Chrome policy registry keys...

reg delete "HKCU\Software\Google\Chrome" /f >nul 2>&1
reg delete "HKCU\Software\Policies\Google\Chrome" /f >nul 2>&1
reg delete "HKLM\Software\Google\Chrome" /f >nul 2>&1
reg delete "HKLM\Software\Policies\Google\Chrome" /f >nul 2>&1
reg delete "HKLM\Software\Policies\Google\Update" /f >nul 2>&1
reg delete "HKLM\Software\WOW6432Node\Google\Enrollment" /f >nul 2>&1

:: Step 2: Remove CloudManagementEnrollmentToken value only
echo [STEP 2] Removing CloudManagementEnrollmentToken value...

reg delete "HKLM\Software\WOW6432Node\Google\Update\ClientState\{430FD4D0-B729-4F61-AA34-91526481799D}" /v CloudManagementEnrollmentToken /f >nul 2>&1

:: Step 3: Remove policy-enforced extension installations
echo [STEP 3] Removing extension force-install policies...

reg delete "HKCU\Software\Policies\Google\Chrome\ExtensionInstallForcelist" /f >nul 2>&1
reg delete "HKLM\Software\Policies\Google\Chrome\ExtensionInstallForcelist" /f >nul 2>&1

:: Step 4: Remove Chrome policy directories
echo [STEP 4] Removing Chrome policy directories...

if exist "%ProgramFiles(x86)%\Google\Policies" (
    rmdir /s /q "%ProgramFiles(x86)%\Google\Policies"
)

if exist "%ProgramFiles%\Google\Policies" (
    rmdir /s /q "%ProgramFiles%\Google\Policies"
)

if exist "%ProgramData%\Google\Policies" (
    rmdir /s /q "%ProgramData%\Google\Policies"
)

:: Step 5: Remove entire user Chrome profile
echo [STEP 5] Removing entire Chrome user profile directory...
echo This includes all settings, cache, cookies, history, saved logins, extensions, etc.

RD /S /Q "%LocalAppData%\Google\Chrome"

echo.
echo [COMPLETE] Chrome has been fully reset and cleaned.
echo Restart Chrome or reboot the system to apply all changes.
pause
