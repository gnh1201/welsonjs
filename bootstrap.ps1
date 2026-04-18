# WelsonJS One-Click Bootstrap (bootstrap.ps1)
# Copyright 2019-2025, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
# SPDX-License-Identifier: GPL-3.0-or-later
# https://github.com/gnh1201/welsonjs
#
# Usage:
#   irm https://catswords.blob.core.windows.net/welsonjs/bootstrap.ps1 | iex
#   irm https://catswords.blob.core.windows.net/welsonjs/bootstrap.ps1 | iex -dev main
#   irm https://catswords.blob.core.windows.net/welsonjs/bootstrap.ps1 | iex -file test
#   irm https://catswords.blob.core.windows.net/welsonjs/bootstrap.ps1 | iex -dev dev -file test.js

$defaultBranch = "master"
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
    # Step 0: Parse arguments (iex-compatible)
    $branch = $defaultBranch
    $fileArg = $null

    for ($i = 0; $i -lt $args.Count; $i++) {
        $arg = $args[$i]

        if ($arg -eq "-dev" -and ($i + 1) -lt $args.Count) {
            $branch = $args[$i + 1]
            $i++
        }
        elseif ($arg -eq "-file" -and ($i + 1) -lt $args.Count) {
            $fileArg = $args[$i + 1]
            $i++
        }
        elseif ($arg -notmatch "^-") {
            $branch = $arg
        }
    }

    # Auto-append .js if no extension
    if ($fileArg -and -not ($fileArg -match "\.")) {
        $fileArg = "$fileArg.js"
    }

    Write-Step "Using branch: $branch"
    if ($fileArg) {
        Write-Step "File argument: $fileArg"
    }

    # Step 1: Create temporary workspace
    Write-Step "Creating temporary workspace..."

    $uuid = [guid]::NewGuid().ToString()
    $tempDir = Join-Path $env:TEMP $uuid

    New-Item -ItemType Directory -Path $tempDir | Out-Null

    Write-Ok "Temp directory: $tempDir"

    $repo = "gnh1201/welsonjs"
    $zipPath = Join-Path $tempDir "package.zip"

    # Step 2: Download from branch
    $downloadUrl = "https://github.com/$repo/archive/refs/heads/$branch.zip"

    Write-Ok "Download URL: $downloadUrl"

    Write-Step "Downloading package..."
    irm $downloadUrl -OutFile $zipPath
    Write-Ok "Downloaded: $zipPath"

    # Step 3: Extract
    Write-Step "Extracting package..."
    Expand-Archive -Path $zipPath -DestinationPath $tempDir
    Write-Ok "Extraction completed"

    if ($fileArg) {
        # Step 4A: Run cscript via cmd (with visible console)
        Write-Step "Locating app.js..."

        $app = Get-ChildItem -Path $tempDir -Recurse -Filter "app.js" | Select-Object -First 1

        if (-not $app) {
            throw "app.js not found"
        }

        Write-Ok "Found: $($app.FullName)"

        Write-Step "Executing via cscript (interactive)..."

        Start-Process "cmd.exe" `
            -ArgumentList "/k cscript `"$($app.FullName)`" `"$fileArg`""

        Write-Ok "cscript launched (interactive console)"
    }
    else {
        # Step 4B: Default bootstrap (non-blocking)
        Write-Step "Locating bootstrap.bat..."

        $bootstrap = Get-ChildItem -Path $tempDir -Recurse -Filter "bootstrap.bat" | Select-Object -First 1

        if (-not $bootstrap) {
            throw "bootstrap.bat not found"
        }

        Write-Ok "Found: $($bootstrap.FullName)"

        Write-Step "Executing bootstrap (non-blocking)..."

        Start-Process "cmd.exe" `
            -ArgumentList "/c `"$($bootstrap.FullName)`""

        Write-Ok "Bootstrap launched"
    }

    Write-Host ""
    Write-Host "WelsonJS execution started!" -ForegroundColor Green
    Write-Host ""

}
catch {
    Write-Err $_
    exit 1
}
