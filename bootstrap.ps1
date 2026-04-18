# WelsonJS One-Click Bootstrap (bootstrap.ps1)
# Copyright 2019-2025, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
# SPDX-License-Identifier: GPL-3.0-or-later
# https://github.com/gnh1201/welsonjs
# 
# Usage:
#   irm https://catswords.blob.core.windows.net/welsonjs/bootstrap.ps1 | iex
#   irm https://catswords.blob.core.windows.net/welsonjs/bootstrap.ps1 | iex -dev main
#   irm https://catswords.blob.core.windows.net/welsonjs/bootstrap.ps1 | iex -dev dev
# 
# Central default branch configuration for this install script.
# Update this value if the repository's default branch changes.
$DefaultBranch = "master"

param(
    # Branch to install from; defaults to the repository's configured primary branch.
    [string]$dev = $DefaultBranch
)

$ErrorActionPreference = "Stop"

function Write-Step($msg) {
    Write-Host "[*] $msg" -ForegroundColor Cyan
}

function Write-Ok($msg) {
    Write-Host "[+] $msg" -ForegroundColor Green
}

function Write-Err($msg) {
    Write-Host "[!] $msg" -ForegroundColor Red
}

try {
    # Step 1: Create a temporary working directory using a UUID
    Write-Step "Creating temporary workspace..."

    $uuid = [guid]::NewGuid().ToString()
    $tempDir = Join-Path $env:TEMP $uuid

    New-Item -ItemType Directory -Path $tempDir | Out-Null

    Write-Ok "Temp directory: $tempDir"

    $repo = "gnh1201/welsonjs"
    $zipPath = Join-Path $tempDir "package.zip"

    # Step 2: Determine branch (default: master)
    $branch = $dev

    Write-Step "Using branch: $branch"

    # GitHub branch ZIP URL
    $downloadUrl = "https://github.com/$repo/archive/refs/heads/$branch.zip"

    Write-Ok "Download URL: $downloadUrl"

    # Step 3: Download the ZIP package
    Write-Step "Downloading package..."

    irm $downloadUrl -OutFile $zipPath

    Write-Ok "Downloaded: $zipPath"

    # Step 4: Extract the ZIP archive
    Write-Step "Extracting package..."

    Expand-Archive -Path $zipPath -DestinationPath $tempDir

    Write-Ok "Extraction completed"

    # Step 5: Locate bootstrap.bat within extracted files
    Write-Step "Locating bootstrap.bat..."

    $bootstrap = Get-ChildItem -Path $tempDir -Recurse -Filter "bootstrap.bat" | Select-Object -First 1

    if (-not $bootstrap) {
        throw "bootstrap.bat not found"
    }

    Write-Ok "Found: $($bootstrap.FullName)"

    # Step 6: Execute bootstrap.bat via cmd.exe for compatibility
    Write-Step "Executing bootstrap..."

    $proc = Start-Process -FilePath "cmd.exe" `
        -ArgumentList "/c `"$($bootstrap.FullName)`"" `
        -Wait -PassThru

    if ($proc.ExitCode -ne 0) {
        throw "bootstrap failed with exit code $($proc.ExitCode)"
    }

    Write-Ok "Bootstrap executed successfully"

    # Step 7: Final message
    Write-Host ""
    Write-Host "WelsonJS installation completed!" -ForegroundColor Green
    Write-Host ""

}
catch {
    Write-Err $_
    exit 1
}
