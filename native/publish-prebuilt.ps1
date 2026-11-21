# WelsonJS prebuilt native binaries: publish
# SPDX-License-Identifier: GPL-3.0-or-later
# SPDX-FileCopyrightText: Namhyeon Go <gnh1201@catswords.re.kr>, and Catswords OSS contributors.
# https://github.com/gnh1201/welsonjs
#
# Publishes a llama.cpp, CrispEmbed or ChakraCore build made on a developer PC (the
# <OutDir>\<arch> folder of native\<component>\build-<component>.ps1)
# to the WelsonJS blob storage, where CI restores it with native\restore-prebuilt.ps1:
#
#   <Container>/packages/native/<component>/<version>-<arch>/<file>
#   <Container>/packages/native/<component>/<version>-<arch>/<file>.gz
#
# <component> is llamacpp, crispembed or ChakraCore (for chakracore).
#
# Each file is uploaded as it is and gzip-compressed (<file>.gz, which CI downloads).
#
# Before uploading, it checks that the folder matches its SHA256SUMS.txt and that
# BUILDINFO.txt names the current version.txt and commit.txt. It uploads with AzCopy
# (azcopy login first; an existing blob is never overwritten, so a published build
# does not change), then copies SHA256SUMS.txt to native\<component>\prebuilt\<arch>.sha256,
# which is what CI trusts: commit that file to use the build.
#
# Usage:
#   powershell -ExecutionPolicy Bypass -NoProfile -File native\publish-prebuilt.ps1 -Component llamacpp|crispembed|chakracore -Arch x64|arm64|x86 -BuildDir <dir>\<arch> [-AzCopy <path>] [-WhatIf]

param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("llamacpp", "crispembed", "chakracore")]
    [string]$Component,
    [Parameter(Mandatory = $true)]
    [ValidateSet("x64", "arm64", "x86")]
    [string]$Arch,
    [Parameter(Mandatory = $true)]
    [string]$BuildDir,
    [string]$Container = "https://catswords.blob.core.windows.net/welsonjs",
    [string]$AzCopy    = "",
    [switch]$WhatIf
)

$ErrorActionPreference = "Stop"

function Get-FileSha256 {
    param([string]$Path)
    $sha = [Security.Cryptography.SHA256]::Create()
    $stream = [IO.File]::OpenRead($Path)
    try { return ([BitConverter]::ToString($sha.ComputeHash($stream)) -replace "-", "").ToLowerInvariant() }
    finally { $stream.Dispose(); $sha.Dispose() }
}

$componentDir = Join-Path $PSScriptRoot $Component
$version = (Get-Content -LiteralPath (Join-Path $componentDir "version.txt") -Raw).Trim()
$commit = (Get-Content -LiteralPath (Join-Path $componentDir "commit.txt") -Raw).Trim().ToLowerInvariant()
$BuildDir = [IO.Path]::GetFullPath($BuildDir)
$sums = Join-Path $BuildDir "SHA256SUMS.txt"
$buildInfo = Join-Path $BuildDir "BUILDINFO.txt"
if (-not (Test-Path -LiteralPath $sums)) { throw "SHA256SUMS.txt was not found in $BuildDir" }
if (-not (Test-Path -LiteralPath $buildInfo)) { throw "BUILDINFO.txt was not found in $BuildDir" }

# The build must be of the current version and commit, and for this architecture.
$info = Get-Content -LiteralPath $buildInfo -Raw
if ($info -notmatch [regex]::Escape("$version ($commit)")) { throw "BUILDINFO.txt is not for $version ($commit):`n$info" }
if ($info -notmatch "Architecture: $Arch\b") { throw "BUILDINFO.txt is not for $Arch" }

# The folder must hold exactly the files of SHA256SUMS.txt, with matching hashes.
$listed = @{}
foreach ($line in Get-Content -LiteralPath $sums) {
    if ($line -notmatch '^([0-9a-f]{64})  ([A-Za-z0-9._+-]+)$') { throw "Unexpected line in ${sums}: $line" }
    $listed[$Matches[2]] = $Matches[1]
}
$present = @(Get-ChildItem -LiteralPath $BuildDir -File | Where-Object { $_.Name -ne "SHA256SUMS.txt" })
if ($present.Count -ne $listed.Count) { throw "$BuildDir holds $($present.Count) files, SHA256SUMS.txt lists $($listed.Count)" }
foreach ($file in $present) {
    if (-not $listed.ContainsKey($file.Name)) { throw "$($file.Name) is not listed in SHA256SUMS.txt" }
    if ((Get-FileSha256 $file.FullName) -ne $listed[$file.Name]) { throw "$($file.Name) does not match SHA256SUMS.txt" }
}

# The folder of the component in the storage. ChakraCore keeps the casing of
# packages/native/ChakraCore, the folder Catswords.Phantomizer loads native modules from.
$storageName = @{ "llamacpp" = "llamacpp"; "crispembed" = "crispembed"; "chakracore" = "ChakraCore" }[$Component]
$destination = "$($Container.TrimEnd('/'))/packages/native/$storageName/$version-$Arch/"
Write-Host "[*] $Component $version ($Arch): $($present.Count) files -> $destination"

if (-not $AzCopy) {
    $onPath = Get-Command "azcopy.exe" -ErrorAction SilentlyContinue
    if ($onPath) { $AzCopy = $onPath.Source }
    else {
        $AzCopy = Get-ChildItem -Path (Join-Path $env:LOCALAPPDATA "Microsoft\WinGet\Packages") -Recurse -Filter "azcopy.exe" -ErrorAction SilentlyContinue |
            Select-Object -First 1 -ExpandProperty FullName
    }
}
if (-not $AzCopy -or -not (Test-Path -LiteralPath $AzCopy)) { throw "azcopy.exe was not found; install it (winget install Microsoft.Azure.AZCopy.10) or pass -AzCopy" }

if ($WhatIf) {
    Write-Host "[*] -WhatIf: not uploading."
}
else {
    # Upload the listed files only (SHA256SUMS.txt stays in the repository), each
    # as it is and as <file>.gz.
    $staging = Join-Path ([IO.Path]::GetTempPath()) "welsonjs-publish-$Component-$Arch"
    if (Test-Path -LiteralPath $staging) { Remove-Item -LiteralPath $staging -Recurse -Force }
    New-Item -ItemType Directory -Path $staging -Force | Out-Null
    foreach ($file in $present) {
        Copy-Item -LiteralPath $file.FullName -Destination $staging
        $source = [IO.File]::OpenRead($file.FullName)
        try {
            $target = [IO.File]::Create((Join-Path $staging "$($file.Name).gz"))
            try {
                $gzip = New-Object IO.Compression.GZipStream($target, [IO.Compression.CompressionLevel]::Optimal)
                try { $source.CopyTo($gzip) } finally { $gzip.Dispose() }
            }
            finally { $target.Dispose() }
        }
        finally { $source.Dispose() }
    }
    & $AzCopy copy (Join-Path $staging "*") $destination --overwrite=false
    $code = $LASTEXITCODE
    Remove-Item -LiteralPath $staging -Recurse -Force
    if ($code -ne 0) { throw "azcopy failed with exit code $code" }
}

$recorded = Join-Path $componentDir "prebuilt\$Arch.sha256"
New-Item -ItemType Directory -Path (Split-Path -Parent $recorded) -Force | Out-Null
Copy-Item -LiteralPath $sums -Destination $recorded -Force
Write-Host "[*] Recorded the file list and hashes in $recorded; commit it to use this build in CI."
exit 0
