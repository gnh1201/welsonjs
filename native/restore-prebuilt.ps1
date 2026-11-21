# WelsonJS prebuilt native binaries: restore
# SPDX-License-Identifier: GPL-3.0-or-later
# SPDX-FileCopyrightText: Namhyeon Go <gnh1201@catswords.re.kr>, and Catswords OSS contributors.
# https://github.com/gnh1201/welsonjs
#
# Downloads a prebuilt llama.cpp, CrispEmbed or ChakraCore build (made with
# native\<component>\build-<component>.ps1 and
# published with native\publish-prebuilt.ps1) from the WelsonJS blob storage into
# <OutDir>\<arch>, so that CI does not build them within its time limit:
#
#   <BaseUrl>/<component>/<version>-<arch>/<file>
#   for example .../packages/native/llamacpp/b9871.390c307.1-x64/llama-server.exe
#
# Each file is downloaded gzip-compressed (<file>.gz, published next to the file)
# and decompressed; if that fails (no .gz, a broken one, or one that does not give
# the recorded hash), the file itself is downloaded. The SHA-256 is checked on the
# resulting file either way.
#
# <version> is native\<component>\version.txt. The files to download, and their
# SHA-256, are native\<component>\prebuilt\<arch>.sha256, a copy of the build's
# SHA256SUMS.txt kept in the repository: a file is used only if its hash matches,
# so the repository, not the storage, decides what is shipped. The folder is
# written only when every file has been downloaded and verified, and it is left
# as it is when it already matches.
#
# Usage:
#   powershell -ExecutionPolicy Bypass -NoProfile -File native\restore-prebuilt.ps1 -Component llamacpp|crispembed|chakracore -Arch x64|arm64|x86 [-OutDir <dir>] [-BaseUrl <url>]
#
# Output: <OutDir>\<arch>\* (default artifacts\<component>\<arch>), with SHA256SUMS.txt.
# Exit code: 0 on success; an error is thrown otherwise.

param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("llamacpp", "crispembed", "chakracore")]
    [string]$Component,
    [Parameter(Mandatory = $true)]
    [ValidateSet("x64", "arm64", "x86")]
    [string]$Arch,
    [string]$OutDir  = "",
    [string]$BaseUrl = "https://catswords.blob.core.windows.net/welsonjs/packages/native"
)

$ErrorActionPreference = "Stop"
[Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12

function Get-FileSha256 {
    param([string]$Path)
    $sha = [Security.Cryptography.SHA256]::Create()
    $stream = [IO.File]::OpenRead($Path)
    try { return ([BitConverter]::ToString($sha.ComputeHash($stream)) -replace "-", "").ToLowerInvariant() }
    finally { $stream.Dispose(); $sha.Dispose() }
}

# Downloads a URL to a file, with three attempts; throws when all fail.
function Save-Download {
    param([string]$Url, [string]$Path)
    for ($attempt = 1; ; $attempt++) {
        $client = New-Object Net.WebClient
        try { $client.DownloadFile($Url, $Path); return }
        catch { if ($attempt -ge 3) { throw "Could not download ${Url}: $($_.Exception.Message)" }; Start-Sleep -Seconds 5 }
        finally { $client.Dispose() }
    }
}

# Decompresses a gzip file.
function Expand-GzipFile {
    param([string]$Source, [string]$Destination)
    $sourceStream = [IO.File]::OpenRead($Source)
    try {
        $gzip = New-Object IO.Compression.GZipStream($sourceStream, [IO.Compression.CompressionMode]::Decompress)
        try {
            $output = [IO.File]::Create($Destination)
            try { $gzip.CopyTo($output) } finally { $output.Dispose() }
        }
        finally { $gzip.Dispose() }
    }
    finally { $sourceStream.Dispose() }
}

$componentDir = Join-Path $PSScriptRoot $Component
$version = (Get-Content -LiteralPath (Join-Path $componentDir "version.txt") -Raw).Trim()
if ($version -notmatch '^[A-Za-z0-9._-]+$') { throw "Invalid version in $Component\version.txt: $version" }
$sumsPath = Join-Path $componentDir "prebuilt\$Arch.sha256"
if (-not (Test-Path -LiteralPath $sumsPath)) { throw "No prebuilt build is recorded for $Component $Arch ($sumsPath)" }

# The list: "<sha256>  <file name>" per line, as SHA256SUMS.txt.
$files = [ordered]@{}
foreach ($line in Get-Content -LiteralPath $sumsPath) {
    if ($line -notmatch '^([0-9a-f]{64})  ([A-Za-z0-9._+-]+)$') { throw "Unexpected line in ${sumsPath}: $line" }
    $files[$Matches[2]] = $Matches[1]
}
if ($files.Count -eq 0) { throw "$sumsPath lists no files" }

if (-not $OutDir) { $OutDir = Join-Path (Get-Location).ProviderPath "artifacts\$Component" }
if (-not [IO.Path]::IsPathRooted($OutDir)) { $OutDir = Join-Path (Get-Location).ProviderPath $OutDir }
$archDir = [IO.Path]::GetFullPath((Join-Path $OutDir $Arch))
$url = "$($BaseUrl.TrimEnd('/'))/$Component/$version-$Arch"

Write-Host "[*] $Component $version ($Arch): $url -> $archDir"

# Already restored: exactly the listed files (and SHA256SUMS.txt), every hash matching.
if (Test-Path -LiteralPath $archDir) {
    $present = @(Get-ChildItem -LiteralPath $archDir -File | Where-Object { $_.Name -ne "SHA256SUMS.txt" })
    $intact = $present.Count -eq $files.Count
    if ($intact) {
        foreach ($file in $present) {
            if (-not $files.Contains($file.Name) -or $files[$file.Name] -ne (Get-FileSha256 $file.FullName)) { $intact = $false; break }
        }
    }
    if ($intact) {
        Write-Host "[*] The folder already holds the recorded files. Nothing to download."
        exit 0
    }
}

$staging = "$archDir.download"
if (Test-Path -LiteralPath $staging) { Remove-Item -LiteralPath $staging -Recurse -Force }
New-Item -ItemType Directory -Path $staging -Force | Out-Null
try {
    foreach ($name in $files.Keys) {
        $target = Join-Path $staging $name
        $compressed = "$target.gz"

        # First <file>.gz, decompressed. If it cannot be downloaded, cannot be
        # decompressed or does not give the recorded hash, the file itself is
        # downloaded instead; either way the hash must match.
        $restored = $false
        try {
            Save-Download "$url/$name.gz" $compressed
            Expand-GzipFile $compressed $target
            $actual = Get-FileSha256 $target
            if ($actual -eq $files[$name]) { $restored = $true }
            else { Write-Host "[WARN] $url/$name.gz decompresses to SHA-256 $actual, expected $($files[$name]). Downloading $name instead." }
        }
        catch {
            Write-Host "[WARN] $url/$name.gz could not be used ($($_.Exception.Message)). Downloading $name instead."
        }
        finally {
            if (Test-Path -LiteralPath $compressed) { Remove-Item -LiteralPath $compressed -Force }
        }

        if (-not $restored) {
            if (Test-Path -LiteralPath $target) { Remove-Item -LiteralPath $target -Force }
            Save-Download "$url/$name" $target
            $actual = Get-FileSha256 $target
            if ($actual -ne $files[$name]) { throw "SHA-256 mismatch for $url/${name}: expected $($files[$name]), got $actual" }
        }
    }
    Copy-Item -LiteralPath $sumsPath -Destination (Join-Path $staging "SHA256SUMS.txt")

    if (Test-Path -LiteralPath $archDir) { Remove-Item -LiteralPath $archDir -Recurse -Force }
    Move-Item -LiteralPath $staging -Destination $archDir
}
catch {
    if (Test-Path -LiteralPath $staging) { Remove-Item -LiteralPath $staging -Recurse -Force }
    throw
}
Write-Host "[*] Restored $($files.Count) files into $archDir"
exit 0
