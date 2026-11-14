# WelsonJS llama.cpp build script
# SPDX-License-Identifier: GPL-3.0-or-later
# SPDX-FileCopyrightText: Namhyeon Go <gnh1201@catswords.re.kr>, and Catswords OSS contributors.
# https://github.com/gnh1201/welsonjs
#
# Builds llama.cpp at the commit in commit.txt (version.txt is its release tag,
# used for the package name) with the patches in patches\
# and places a Windows CPU build, with the same files as the official
# llama-<version>-bin-win-cpu-<arch>.zip package, in <OutDir>\<arch>:
#   llama-server, llama-cli, llama-quantize and the ggml backends, with every
#   CPU variant loaded at run time (GGML_BACKEND_DL, GGML_CPU_ALL_VARIANTS),
#   plus BUILDINFO.txt and SHA256SUMS.txt. The executables carry the WelsonJS
#   icon (app\favicon.ico, passed to LLAMA_WIN_ICON, which a patch adds).
#
# A build is skipped when <OutDir>\<arch> was built from the same inputs
# (version, commit, patches, the icon and this script; the "Build ID" in BUILDINFO.txt)
# and its files match SHA256SUMS.txt, so the folder can be kept in a CI cache.
#
# Source: the pinned commit is fetched with git. If that fails (-Source auto),
# or with -Source archive, the source archive in source-archive.txt is
# downloaded instead; it is used only if its SHA-256 matches.
#
# Usage:
#   powershell -ExecutionPolicy Bypass -NoProfile -File build-llamacpp.ps1 [-OutDir <dir>] [-WorkDir <dir>] [-Source auto|git|archive] [-Force]
#
# Requirements: git, CMake and Visual Studio (MSVC) with the C++ workload.
# Output: <OutDir>\<arch>\* (for example, artifacts\llamacpp\x64 in CI)

param(
    [string]$OutDir    = (Join-Path $PSScriptRoot "..\..\out\llamacpp"),
    [string]$WorkDir   = (Join-Path $env:TEMP "welsonjs-llamacpp-build"),
    [string]$Arch      = "x64",
    [string]$Generator = "",          # empty: the CMake default (Visual Studio)
    [int]$Jobs         = [Environment]::ProcessorCount,
    [ValidateSet("auto", "git", "archive")]
    [string]$Source    = "auto",
    [string]$RepositoryUrl = "https://github.com/ggml-org/llama.cpp.git",
    [switch]$Force
)

$ErrorActionPreference = "Stop"
$Targets = @("llama-server", "llama-cli", "llama-quantize")

# Windows PowerShell 5.1 may default to older TLS versions.
[Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12

function Invoke-Checked {
    param([string]$File, [string[]]$Arguments)
    Write-Host "[*] $File $($Arguments -join ' ')"
    & $File @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$File failed with exit code $LASTEXITCODE" }
}

function Get-Sha256 {
    param([byte[]]$Bytes)
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash($Bytes)) -replace "-", "").ToLowerInvariant() }
    finally { $sha.Dispose() }
}

# Relative paths are resolved against the current PowerShell location (not the
# process working directory, which can differ in PowerShell).
function Resolve-FullPath {
    param([string]$Path)
    if (-not [IO.Path]::IsPathRooted($Path)) { $Path = Join-Path (Get-Location).ProviderPath $Path }
    return [IO.Path]::GetFullPath($Path)
}

# True when the folder holds exactly the files listed in SHA256SUMS.txt (plus
# that file) and every hash matches.
function Test-Checksums {
    param([string]$Folder, [string]$ChecksumsFile)
    if (-not (Test-Path -LiteralPath $ChecksumsFile)) { return $false }
    $listed = @{}
    foreach ($line in Get-Content -LiteralPath $ChecksumsFile) {
        if ($line -notmatch '^([0-9a-f]{64})  (.+)$') { return $false }
        $listed[$Matches[2]] = $Matches[1]
    }
    $present = @(Get-ChildItem -LiteralPath $Folder -File | Where-Object { $_.Name -ne [IO.Path]::GetFileName($ChecksumsFile) })
    if ($present.Count -ne $listed.Count -or $listed.Count -eq 0) { return $false }
    foreach ($file in $present) {
        if (-not $listed.ContainsKey($file.Name) -or $listed[$file.Name] -ne (Get-FileSha256 $file.FullName)) { return $false }
    }
    return $true
}

function Get-FileSha256 {
    param([string]$Path)
    $sha = [Security.Cryptography.SHA256]::Create()
    $stream = [IO.File]::OpenRead($Path)
    try { return ([BitConverter]::ToString($sha.ComputeHash($stream)) -replace "-", "").ToLowerInvariant() }
    finally { $stream.Dispose(); $sha.Dispose() }
}

# Fetches exactly the pinned commit (not a tag). The patches are LF; keep the
# checkout LF even where core.autocrlf is true (AppVeyor).
function Get-SourceFromGit {
    param([string]$Destination)
    Invoke-Checked "git" @("init", "--quiet", $Destination)
    Invoke-Checked "git" @("-C", $Destination, "config", "core.autocrlf", "false")
    Invoke-Checked "git" @("-C", $Destination, "config", "advice.detachedHead", "false")
    Invoke-Checked "git" @("-C", $Destination, "remote", "add", "origin", $RepositoryUrl)
    Invoke-Checked "git" @("-C", $Destination, "fetch", "--quiet", "--depth", "1", "origin", $commit)
    Invoke-Checked "git" @("-C", $Destination, "checkout", "--quiet", "FETCH_HEAD")
    $checkedOut = (& git -C $Destination rev-parse HEAD).Trim().ToLowerInvariant()
    if ($checkedOut -ne $commit) { throw "Checked out $checkedOut, but commit.txt pins $commit" }
    return "git $RepositoryUrl"
}

# Downloads the source archive from source-archive.txt (a "git archive" of the
# pinned commit, with one top-level folder "llama.cpp-<commit>"), verifies its
# SHA-256 and extracts it with the top-level folder stripped, which keeps the
# deepest paths well under the Windows MAX_PATH limit.
function Get-SourceFromArchive {
    param([string]$Destination)

    $settings = @{}
    foreach ($line in Get-Content -LiteralPath (Join-Path $PSScriptRoot "source-archive.txt")) {
        if ($line -match '^\s*([a-z0-9]+)\s*=\s*(\S+)\s*$') { $settings[$Matches[1]] = $Matches[2] }
    }
    $url = $settings["url"]; $expected = ("" + $settings["sha256"]).ToLowerInvariant()
    if ($url -notmatch '^https://') { throw "source-archive.txt must contain an HTTPS url" }
    if ($expected -notmatch '^[0-9a-f]{64}$') { throw "source-archive.txt must contain a sha256" }
    if ($url -notlike "*$commit*") { throw "The source archive does not match the pinned commit $commit`: $url" }

    $archivePath = Join-Path $WorkDir "llama.cpp-$commit.zip"
    for ($attempt = 1; ; $attempt++) {
        Write-Host "[*] Downloading the source archive (attempt $attempt): $url"
        $client = New-Object Net.WebClient
        try { $client.DownloadFile($url, $archivePath); break }
        catch { if ($attempt -ge 3) { throw "Could not download the source archive: $($_.Exception.Message)" }; Start-Sleep -Seconds 5 }
        finally { $client.Dispose() }
    }

    $actual = Get-FileSha256 $archivePath
    if ($actual -ne $expected) { throw "The source archive SHA-256 does not match (expected $expected, got $actual): $url" }
    Write-Host "[*] Source archive SHA-256 verified: $actual"

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $root = "llama.cpp-$commit/"
    $destinationFull = [IO.Path]::GetFullPath($Destination).TrimEnd('\') + '\'
    $zip = [IO.Compression.ZipFile]::OpenRead($archivePath)
    try {
        foreach ($entry in $zip.Entries) {
            $name = $entry.FullName.Replace('\', '/')
            if (-not $name.StartsWith($root, [StringComparison]::Ordinal)) { throw "Unexpected entry outside $root in the source archive: $name" }
            $relative = $name.Substring($root.Length)
            if ($relative.Length -eq 0) { continue }

            $target = [IO.Path]::GetFullPath((Join-Path $Destination $relative.Replace('/', '\')))
            if (-not $target.StartsWith($destinationFull, [StringComparison]::OrdinalIgnoreCase)) { throw "Unsafe path in the source archive: $name" }

            if ($name.EndsWith('/')) { New-Item -ItemType Directory -Path $target -Force | Out-Null; continue }
            New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($target)) -Force | Out-Null
            [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $target, $true)
        }
    }
    finally { $zip.Dispose() }
    Remove-Item -LiteralPath $archivePath -Force
    return "archive $url (sha256 $actual)"
}

# ================================
# INPUTS
# ================================
$version = (Get-Content -LiteralPath (Join-Path $PSScriptRoot "version.txt") -Raw).Trim()
if ($version -notmatch '^[A-Za-z0-9._-]+$') { throw "Invalid llama.cpp version: $version" }
# The source is pinned to a full commit SHA: a tag can be moved upstream, a commit cannot.
$commit = (Get-Content -LiteralPath (Join-Path $PSScriptRoot "commit.txt") -Raw).Trim().ToLowerInvariant()
if ($commit -notmatch '^[0-9a-f]{40}$') { throw "commit.txt must contain a full 40-character commit SHA: $commit" }
$patches = @(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot "patches") -Filter "*.patch" | Sort-Object Name)
# The icon embedded in the tool executables (LLAMA_WIN_ICON, added by
# patches\0002-tools-windows-icon.patch).
$iconPath = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\..\app\favicon.ico"))
if (-not (Test-Path -LiteralPath $iconPath)) { throw "The icon was not found: $iconPath" }

# The build ID covers every input, so a change to any of them rebuilds.
$inputs = New-Object IO.MemoryStream
foreach ($item in @(@{ Name = "version"; Bytes = [Text.Encoding]::UTF8.GetBytes("$version|$commit|$Arch") }) +
                  @($patches | ForEach-Object { @{ Name = $_.Name; Bytes = [IO.File]::ReadAllBytes($_.FullName) } }) +
                  @(@{ Name = "icon"; Bytes = [IO.File]::ReadAllBytes($iconPath) }) +
                  @(@{ Name = "script"; Bytes = [IO.File]::ReadAllBytes($PSCommandPath) })) {
    $name = [Text.Encoding]::UTF8.GetBytes($item.Name + "`n")
    $inputs.Write($name, 0, $name.Length)
    $inputs.Write($item.Bytes, 0, $item.Bytes.Length)
}
$buildId = Get-Sha256 $inputs.ToArray()

$OutDir = Resolve-FullPath $OutDir
$archDir = Join-Path $OutDir $Arch
$buildInfoPath = Join-Path $archDir "BUILDINFO.txt"
$checksumsPath = Join-Path $archDir "SHA256SUMS.txt"

Write-Host "[*] llama.cpp version: $version, commit $commit ($Arch)"
Write-Host "[*] Patches          : $(if ($patches.Count) { ($patches | ForEach-Object Name) -join ', ' } else { '(none)' })"
Write-Host "[*] Build ID         : $buildId"
Write-Host "[*] Output           : $archDir"

# Skip the build when the output folder (for example, restored from a CI cache)
# was built from the same inputs and every file still matches SHA256SUMS.txt.
if (-not $Force -and (Test-Path -LiteralPath $buildInfoPath) -and
    (Select-String -LiteralPath $buildInfoPath -SimpleMatch "Build ID: $buildId" -Quiet)) {
    if (Test-Checksums $archDir $checksumsPath) {
        Write-Host "[*] The output was built from the same inputs and its files are intact. Skipping the build."
        exit 0
    }
    Write-Host "[WARN] The output does not match SHA256SUMS.txt. Building again."
}

# ================================
# SOURCE
# ================================
$WorkDir = Resolve-FullPath $WorkDir
$sourceDir = Join-Path $WorkDir "llama.cpp"
$buildDir = Join-Path $WorkDir "build-$Arch"
if (Test-Path -LiteralPath $WorkDir) { Remove-Item -LiteralPath $WorkDir -Recurse -Force }
New-Item -ItemType Directory -Path $WorkDir -Force | Out-Null

$stopwatch = [Diagnostics.Stopwatch]::StartNew()

$sourceOrigin = $null
if ($Source -ne "archive") {
    try {
        $sourceOrigin = Get-SourceFromGit $sourceDir
    }
    catch {
        if ($Source -eq "git") { throw }
        Write-Host "[WARN] Could not fetch the source with git: $($_.Exception.Message)"
        Write-Host "[WARN] Falling back to the source archive."
        if (Test-Path -LiteralPath $sourceDir) { Remove-Item -LiteralPath $sourceDir -Recurse -Force }
    }
}
if (-not $sourceOrigin) {
    $sourceOrigin = Get-SourceFromArchive $sourceDir
}
Write-Host "[*] Source: $sourceOrigin"

# git apply also works outside a repository (archive source).
foreach ($patch in $patches) {
    Invoke-Checked "git" @("-C", $sourceDir, "apply", "--whitespace=nowarn", $patch.FullName)
}

# ================================
# BUILD
# ================================
$configure = @("-S", $sourceDir, "-B", $buildDir)
if ($Generator) { $configure += @("-G", $Generator) }
if (-not $Generator -or $Generator -like "Visual Studio*") { $configure += @("-A", $Arch) }
$configure += @(
    "-DCMAKE_BUILD_TYPE=Release",
    "-DBUILD_SHARED_LIBS=ON",
    "-DGGML_NATIVE=OFF",
    "-DGGML_BACKEND_DL=ON",
    "-DGGML_CPU_ALL_VARIANTS=ON",
    "-DLLAMA_BUILD_TESTS=OFF",
    "-DLLAMA_BUILD_EXAMPLES=OFF",
    "-DLLAMA_BUILD_SERVER=ON",
    "-DLLAMA_CURL=OFF",
    # No downloads during the build: the server's Web UI is fetched from a
    # floating Hugging Face location otherwise (the launcher only uses the API),
    # and llama-connect is a prebuilt binary.
    "-DLLAMA_BUILD_UI=OFF",
    "-DLLAMA_USE_PREBUILT_UI=OFF",
    "-DLLAMA_CONNECT=OFF",
    # The WelsonJS icon in every tool executable (patches\0002-tools-windows-icon.patch).
    "-DLLAMA_WIN_ICON=$($iconPath.Replace('\', '/'))",
    # Without a .git folder (archive source) the build cannot read the commit.
    "-DLLAMA_BUILD_COMMIT=$($commit.Substring(0, 7))"
)
Invoke-Checked "cmake" $configure

# With GGML_BACKEND_DL the CPU variants are not link dependencies of the
# tools, so build their targets explicitly.
$backendTargets = @(Get-ChildItem -LiteralPath $buildDir -Recurse -Filter "ggml-cpu*.vcxproj" | ForEach-Object { $_.BaseName } | Sort-Object -Unique)
if ($backendTargets.Count -eq 0) { $backendTargets = @("ggml-cpu") }
Write-Host "[*] CPU backend targets: $($backendTargets -join ', ')"

Invoke-Checked "cmake" (@("--build", $buildDir, "--config", "Release", "--parallel", "$Jobs", "--target") + $Targets + $backendTargets)

# ================================
# PACKAGE
# ================================
$binDir = Join-Path $buildDir "bin\Release"
if (-not (Test-Path -LiteralPath $binDir)) { $binDir = Join-Path $buildDir "bin" }
foreach ($target in $Targets) {
    if (-not (Test-Path -LiteralPath (Join-Path $binDir "$target.exe"))) { throw "$target.exe was not built (looked in $binDir)" }
}

# The files are placed directly in <OutDir>\<arch> (no archive).
if (Test-Path -LiteralPath $archDir) { Remove-Item -LiteralPath $archDir -Recurse -Force }
New-Item -ItemType Directory -Path $archDir -Force | Out-Null
Get-ChildItem -LiteralPath $binDir -File | Where-Object { $_.Extension -in @(".dll") -or ($Targets -contains $_.BaseName -and $_.Extension -eq ".exe") } |
    Copy-Item -Destination $archDir
Copy-Item -LiteralPath (Join-Path $sourceDir "LICENSE") -Destination (Join-Path $archDir "LICENSE-llama.cpp.txt")

@(
    "llama.cpp $version ($commit) built for WelsonJS",
    "Source: $sourceOrigin",
    "Architecture: $Arch (CPU backends: $($backendTargets -join ', '))",
    "Patches: $(if ($patches.Count) { ($patches | ForEach-Object Name) -join ', ' } else { '(none)' })",
    "Build ID: $buildId",
    "Built: $([DateTime]::UtcNow.ToString('u'))"
) | Set-Content -LiteralPath $buildInfoPath -Encoding ASCII

# One "<sha256>  <file name>" line per file, written last.
Get-ChildItem -LiteralPath $archDir -File | Sort-Object Name | ForEach-Object { "$(Get-FileSha256 $_.FullName)  $($_.Name)" } |
    Set-Content -LiteralPath $checksumsPath -Encoding ASCII

Write-Host ("[*] Built llama.cpp into {0} in {1:N1} min" -f $archDir, $stopwatch.Elapsed.TotalMinutes)
Write-Host "[*] Files: $((Get-ChildItem -LiteralPath $archDir -File | ForEach-Object Name) -join ', ')"
exit 0
