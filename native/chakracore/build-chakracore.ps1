# WelsonJS ChakraCore build script
# SPDX-License-Identifier: GPL-3.0-or-later
# SPDX-FileCopyrightText: Namhyeon Go <gnh1201@catswords.re.kr>, and Catswords OSS contributors.
# https://github.com/gnh1201/welsonjs
#
# Builds ChakraCore (https://github.com/chakra-core/ChakraCore, MIT) at the commit in
# commit.txt (version.txt is a label only), with the patches in patches\, and places a
# Windows build in <OutDir>\<arch>:
#   ChakraCore.dll, its import library ChakraCore.lib, the public JsRT headers
#   (ChakraCore.h, ChakraCommon.h, ChakraCommonWindows.h, ChakraCoreWindows.h,
#   ChakraDebug.h), LICENSE-ChakraCore.txt, THIRD-PARTY-NOTICES-ChakraCore.txt,
#   BUILDINFO.txt and SHA256SUMS.txt.
# ChakraCore.dll carries version information (patches\0001-windows-resources.patch):
# the WelsonJS version (-ProductVersion, by default the launcher's
# AssemblyFileVersion), a description that says "built for WelsonJS", CompanyName,
# and the copyright of Microsoft (ChakraCore) and Catswords OSS (WelsonJS). The build
# fails when the DLL lacks that information or is not listed for signing in
# .artifact-configuration.xml.
#
# The DLL is built from ChakraCore's own Visual Studio projects
# (bin\ChakraCore\ChakraCore.vcxproj, Release) with MSBuild, for x64, x86 (Win32) or
# arm64 (ARM64).
#
# A build is skipped when <OutDir>\<arch> was built from the same inputs (version,
# product version, commit, patches and this script; the "Build ID" in BUILDINFO.txt)
# and its files match SHA256SUMS.txt. CI does not run this script: it restores the
# published build (native\restore-prebuilt.ps1).
#
# Source: the pinned commit is fetched with git. If that fails (-Source auto), or with
# -Source archive, the source archive in source-archive.txt is downloaded instead; it
# is used only if its SHA-256 matches.
#
# Usage:
#   powershell -ExecutionPolicy Bypass -NoProfile -File build-chakracore.ps1 [-Arch x64|x86|arm64] [-OutDir <dir>] [-WorkDir <dir>] [-Source auto|git|archive] [-ProductVersion a.b.c.d] [-MSBuild <path>] [-Force]
#
# Requirements: git, Visual Studio (or Build Tools) with the C++ workload, and for
# arm64 the MSVC ARM64 build tools.
# Output: <OutDir>\<arch>\*

param(
    [string]$OutDir    = (Join-Path $PSScriptRoot "..\..\out\chakracore"),
    [string]$WorkDir   = (Join-Path $env:TEMP "welsonjs-chakracore-build"),
    [ValidateSet("x64", "x86", "arm64")]
    [string]$Arch      = "x64",
    [int]$Jobs         = [Environment]::ProcessorCount,
    [string]$RepositoryUrl = "https://github.com/chakra-core/ChakraCore.git",
    [ValidateSet("auto", "git", "archive")]
    [string]$Source    = "auto",
    [string]$ProductVersion = "",     # a.b.c.d in the version information; empty: the launcher's AssemblyFileVersion
    [string]$MSBuild   = "",          # MSBuild.exe; empty: the newest Visual Studio with the C++ tools
    [switch]$Force
)

$ErrorActionPreference = "Stop"

# Windows PowerShell 5.1 may default to older TLS versions.
[Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12

# Runs a native command and fails on a non-zero exit code. stderr is printed like
# stdout (where PowerShell captures stderr, each line would otherwise become an error
# record that $ErrorActionPreference = "Stop" turns into a terminating error).
function Invoke-Checked {
    param([string]$File, [string[]]$Arguments)
    Write-Host "[*] $File $($Arguments -join ' ')"
    $savedPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        & $File @Arguments 2>&1 | ForEach-Object { Write-Host "$_" }
        $exitCode = $LASTEXITCODE
    }
    finally { $ErrorActionPreference = $savedPreference }
    if ($exitCode -ne 0) { throw "$File failed with exit code $exitCode" }
}

# Runs a native command and returns its stdout lines (stderr lines are printed).
function Get-NativeOutput {
    param([string]$File, [string[]]$Arguments)
    $savedPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        $output = @(& $File @Arguments 2>&1 | ForEach-Object {
            if ($_ -is [System.Management.Automation.ErrorRecord]) { Write-Host "$_"; return }
            "$_"
        })
    }
    finally { $ErrorActionPreference = $savedPreference }
    return $output
}

function Get-Sha256 {
    param([byte[]]$Bytes)
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash($Bytes)) -replace "-", "").ToLowerInvariant() }
    finally { $sha.Dispose() }
}

function Get-FileSha256 {
    param([string]$Path)
    $sha = [Security.Cryptography.SHA256]::Create()
    $stream = [IO.File]::OpenRead($Path)
    try { return ([BitConverter]::ToString($sha.ComputeHash($stream)) -replace "-", "").ToLowerInvariant() }
    finally { $stream.Dispose(); $sha.Dispose() }
}

function Resolve-FullPath {
    param([string]$Path)
    if (-not [IO.Path]::IsPathRooted($Path)) { $Path = Join-Path (Get-Location).ProviderPath $Path }
    return [IO.Path]::GetFullPath($Path)
}

# True when the folder holds exactly the files listed in SHA256SUMS.txt (plus that
# file) and every hash matches.
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

# Deletes a folder tree; the "\\?\" prefix lifts the MAX_PATH limit, and git keeps
# its object files read-only.
function Remove-Tree {
    param([string]$Path)
    $long = "\\?\" + [IO.Path]::GetFullPath($Path)
    if (-not [IO.Directory]::Exists($long)) { return }
    foreach ($file in [IO.Directory]::EnumerateFiles($long, "*", [IO.SearchOption]::AllDirectories)) {
        [IO.File]::SetAttributes($file, [IO.FileAttributes]::Normal)
    }
    [IO.Directory]::Delete($long, $true)
}

# Fetches exactly the pinned commit into a new folder. The checkout stays LF even
# where core.autocrlf is true, so the patches apply.
function Get-SourceFromGit {
    param([string]$Destination)
    Invoke-Checked "git" @("init", "--quiet", $Destination)
    Invoke-Checked "git" @("-C", $Destination, "config", "core.autocrlf", "false")
    Invoke-Checked "git" @("-C", $Destination, "config", "core.longpaths", "true")
    Invoke-Checked "git" @("-C", $Destination, "config", "advice.detachedHead", "false")
    Invoke-Checked "git" @("-C", $Destination, "remote", "add", "origin", $RepositoryUrl)
    Invoke-Checked "git" @("-C", $Destination, "fetch", "--quiet", "--depth", "1", "origin", $commit)
    Invoke-Checked "git" @("-C", $Destination, "checkout", "--quiet", "FETCH_HEAD")
    $checkedOut = ("" + (Get-NativeOutput "git" @("-C", $Destination, "rev-parse", "HEAD") | Select-Object -First 1)).Trim().ToLowerInvariant()
    if ($checkedOut -ne $commit) { throw "Checked out $checkedOut, but commit.txt pins $commit" }
    return "git $RepositoryUrl"
}

# Downloads the source archive from source-archive.txt (a "git archive" of the commit
# in one top-level folder "ChakraCore-<commit>"), verifies its SHA-256 and extracts it
# with the top-level folder stripped.
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

    $archivePath = Join-Path $WorkDir "ChakraCore-$commit.zip"
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
    $root = "ChakraCore-$commit/"
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
            $longTarget = "\\?\" + $target
            if ($name.EndsWith('/')) { [void][IO.Directory]::CreateDirectory($longTarget); continue }
            [void][IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($longTarget))
            [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $longTarget, $true)
        }
    }
    finally { $zip.Dispose() }
    Remove-Item -LiteralPath $archivePath -Force
    return "archive $url (sha256 $actual)"
}

# A string for a resource script: quotes and backslashes escaped.
function ConvertTo-RcString {
    param([string]$Text)
    return '"' + $Text.Replace('\', '\\').Replace('"', '""') + '"'
}

# ================================
# INPUTS
# ================================
$version = (Get-Content -LiteralPath (Join-Path $PSScriptRoot "version.txt") -Raw).Trim()
if ($version -notmatch '^[A-Za-z0-9._-]+$') { throw "Invalid ChakraCore version: $version" }
# The source is pinned to a full commit SHA: a tag can be moved upstream, a commit cannot.
$commit = (Get-Content -LiteralPath (Join-Path $PSScriptRoot "commit.txt") -Raw).Trim().ToLowerInvariant()
if ($commit -notmatch '^[0-9a-f]{40}$') { throw "commit.txt must contain a full 40-character commit SHA: $commit" }
$patchDir = Join-Path $PSScriptRoot "patches"
$patches = @(if (Test-Path -LiteralPath $patchDir) { Get-ChildItem -LiteralPath $patchDir -Filter "*.patch" | Sort-Object Name })

# Version information of ChakraCore.dll (patches\0001-windows-resources.patch).
if (-not $ProductVersion) {
    $assemblyInfo = Join-Path $PSScriptRoot "..\WelsonJS.Augmented\WelsonJS.Launcher\Properties\AssemblyInfo.cs"
    $match = Select-String -LiteralPath $assemblyInfo -Pattern 'AssemblyFileVersion\("([0-9.]+)"\)' | Select-Object -First 1
    if (-not $match) { throw "AssemblyFileVersion was not found in $assemblyInfo" }
    $ProductVersion = $match.Matches[0].Groups[1].Value
}
if ($ProductVersion -notmatch '^\d+\.\d+\.\d+\.\d+$') { throw "The product version must be a.b.c.d: $ProductVersion" }
$companyName = "Catswords Research"
$copyright = "Copyright (c) Microsoft Corporation and contributors (ChakraCore), Copyright (c) 2026 Catswords OSS (WelsonJS)"
$description = "ChakraCore JavaScript engine (built for WelsonJS)"
$comments = "ChakraCore $version, commit $($commit.Substring(0, 7)), built for WelsonJS"
# Every executable and DLL must be listed (without wildcards) for SignPath.
$signingConfigPath = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\..\.artifact-configuration.xml"))
$platform = @{ "x64" = "x64"; "x86" = "Win32"; "arm64" = "ARM64" }[$Arch]

# The build ID covers every input, so a change to any of them rebuilds.
$inputs = New-Object IO.MemoryStream
foreach ($item in @(@{ Name = "version"; Bytes = [Text.Encoding]::UTF8.GetBytes("$version|$ProductVersion|$commit|$Arch") }) +
                  @($patches | ForEach-Object { @{ Name = $_.Name; Bytes = [IO.File]::ReadAllBytes($_.FullName) } }) +
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

Write-Host "[*] ChakraCore version: $version, commit $commit ($Arch, MSBuild platform $platform)"
Write-Host "[*] Product version   : $ProductVersion"
Write-Host "[*] Patches           : $(if ($patches.Count) { ($patches | ForEach-Object Name) -join ', ' } else { '(none)' })"
Write-Host "[*] Build ID          : $buildId"
Write-Host "[*] Output            : $archDir"

if (-not $Force -and (Test-Path -LiteralPath $buildInfoPath) -and
    (Select-String -LiteralPath $buildInfoPath -SimpleMatch "Build ID: $buildId" -Quiet)) {
    if (Test-Checksums $archDir $checksumsPath) {
        Write-Host "[*] The output was built from the same inputs and its files are intact. Skipping the build."
        exit 0
    }
    Write-Host "[WARN] The output does not match SHA256SUMS.txt. Building again."
}

# ================================
# TOOLCHAIN
# ================================
if (-not $MSBuild) {
    $vswhere = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer\vswhere.exe"
    if (-not (Test-Path -LiteralPath $vswhere)) { throw "vswhere.exe was not found; install Visual Studio or Build Tools with the C++ workload, or pass -MSBuild" }
    $requires = @("-requires", "Microsoft.VisualStudio.Component.VC.Tools.x86.x64")
    if ($Arch -eq "arm64") { $requires += @("Microsoft.VisualStudio.Component.VC.Tools.ARM64") }
    $MSBuild = ("" + (Get-NativeOutput $vswhere (@("-latest", "-products", "*") + $requires + @("-find", "MSBuild\**\Bin\MSBuild.exe")) | Select-Object -First 1)).Trim()
    if (-not $MSBuild) { throw "No Visual Studio installation has the C++ tools$(if ($Arch -eq 'arm64') { ' and the MSVC ARM64 build tools' })" }
}
if (-not (Test-Path -LiteralPath $MSBuild)) { throw "MSBuild was not found: $MSBuild" }
Write-Host "[*] MSBuild           : $MSBuild"

# ================================
# SOURCE
# ================================
$WorkDir = Resolve-FullPath $WorkDir
$sourceDir = Join-Path $WorkDir "ChakraCore"
Remove-Tree $WorkDir
New-Item -ItemType Directory -Path $WorkDir -Force | Out-Null
$stopwatch = [Diagnostics.Stopwatch]::StartNew()

$sourceOrigin = $null
if ($Source -ne "archive") {
    try { $sourceOrigin = Get-SourceFromGit $sourceDir }
    catch {
        if ($Source -eq "git") { throw }
        Write-Host "[WARN] Could not fetch the source with git: $($_.Exception.Message)"
        Write-Host "[WARN] Falling back to the source archive."
        Remove-Tree $sourceDir
    }
}
if (-not $sourceOrigin) { $sourceOrigin = Get-SourceFromArchive $sourceDir }
Write-Host "[*] Source: $sourceOrigin"

# git apply follows the global core.autocrlf and would write the patched files with
# CRLF; keep them LF like the rest of the source.
foreach ($patch in $patches) {
    Invoke-Checked "git" @("-c", "core.autocrlf=false", "-C", $sourceDir, "apply", "--whitespace=nowarn", $patch.FullName)
}

# The version information of the WelsonJS build (read by bin\ChakraCore\ChakraCore.rc).
$versionNumbers = $ProductVersion.Replace('.', ',')
@(
    "// Written by native\chakracore\build-chakracore.ps1.",
    "#define WELSONJS_WIN_VERSION 1",
    "#define WELSONJS_WIN_VERSION_NUMBERS $versionNumbers",
    "#define WELSONJS_WIN_VERSION_STRING $(ConvertTo-RcString $ProductVersion)",
    "#define WELSONJS_WIN_COMPANY $(ConvertTo-RcString $companyName)",
    "#define WELSONJS_WIN_DESCRIPTION $(ConvertTo-RcString $description)",
    "#define WELSONJS_WIN_COPYRIGHT $(ConvertTo-RcString $copyright)",
    "#define WELSONJS_WIN_COMMENTS $(ConvertTo-RcString $comments)"
) | Set-Content -LiteralPath (Join-Path $sourceDir "bin\ChakraCore\WelsonJSVersion.h") -Encoding ASCII

# ================================
# BUILD
# ================================
# ChakraCore.vcxproj and the projects it references, with the solution folder as
# SolutionDir (the projects use it for their output paths).
Invoke-Checked $MSBuild @(
    (Join-Path $sourceDir "bin\ChakraCore\ChakraCore.vcxproj"),
    "-nologo", "-verbosity:minimal", "-maxCpuCount:$Jobs",
    "-p:Configuration=Release",
    "-p:Platform=$platform",
    # (a trailing / rather than \, which PowerShell would turn into an escaped quote
    # when it quotes a path with spaces)
    "-p:SolutionDir=$((Join-Path $sourceDir 'Build').Replace([char]92, [char]47))/"
)

# ================================
# PACKAGE
# ================================
$binRoot = Join-Path $sourceDir "Build\VcBuild\bin"
$dll = Get-ChildItem -LiteralPath $binRoot -Recurse -Filter "ChakraCore.dll" -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $dll) { throw "ChakraCore.dll was not built (looked in $binRoot)" }
$lib = Join-Path $dll.DirectoryName "ChakraCore.lib"
if (-not (Test-Path -LiteralPath $lib)) { throw "ChakraCore.lib was not built next to $($dll.FullName)" }

if (Test-Path -LiteralPath $archDir) { Remove-Item -LiteralPath $archDir -Recurse -Force }
New-Item -ItemType Directory -Path $archDir -Force | Out-Null
Copy-Item -LiteralPath $dll.FullName -Destination $archDir
Copy-Item -LiteralPath $lib -Destination $archDir
foreach ($header in @("ChakraCore.h", "ChakraCommon.h", "ChakraCommonWindows.h", "ChakraCoreWindows.h", "ChakraDebug.h")) {
    Copy-Item -LiteralPath (Join-Path $sourceDir "lib\Jsrt\$header") -Destination $archDir
}
Copy-Item -LiteralPath (Join-Path $sourceDir "LICENSE.txt") -Destination (Join-Path $archDir "LICENSE-ChakraCore.txt")
Copy-Item -LiteralPath (Join-Path $sourceDir "THIRD-PARTY-NOTICES.txt") -Destination (Join-Path $archDir "THIRD-PARTY-NOTICES-ChakraCore.txt")

# The DLL must carry the version information, and be listed for signing.
$binaries = @(Get-ChildItem -LiteralPath $archDir -File | Where-Object { $_.Extension -in @(".exe", ".dll") } | Sort-Object Name)
$problems = @()
foreach ($file in $binaries) {
    $info = $file.VersionInfo
    if (-not $info.FileDescription -or -not $info.FileDescription.StartsWith($file.BaseName + " ")) { $problems += "$($file.Name): FileDescription is '$($info.FileDescription)'" }
    if ($info.FileVersion -ne $ProductVersion) { $problems += "$($file.Name): FileVersion is '$($info.FileVersion)', expected $ProductVersion" }
    if ($info.ProductVersion -ne $ProductVersion) { $problems += "$($file.Name): ProductVersion is '$($info.ProductVersion)', expected $ProductVersion" }
    if ($info.CompanyName -ne $companyName) { $problems += "$($file.Name): CompanyName is '$($info.CompanyName)'" }
    if ($info.LegalCopyright -ne $copyright) { $problems += "$($file.Name): LegalCopyright is '$($info.LegalCopyright)'" }
}
if (Test-Path -LiteralPath $signingConfigPath) {
    $listed = @(([xml](Get-Content -LiteralPath $signingConfigPath -Raw)).SelectNodes("//*[local-name()='include']") |
        ForEach-Object { $_.GetAttribute("path").ToLowerInvariant() })
    foreach ($file in $binaries) {
        if ($listed -notcontains "artifacts/chakracore/$Arch/$($file.Name)".ToLowerInvariant()) {
            $problems += "$($file.Name): not listed for signing in .artifact-configuration.xml (artifacts/chakracore/$Arch/$($file.Name))"
        }
    }
}
else {
    Write-Host "[WARN] $signingConfigPath was not found; the signing list was not checked."
}
if ($problems.Count) { throw "The build output is incomplete:`n  " + ($problems -join "`n  ") }
Write-Host "[*] Version information and signing list checked: $($binaries.Count) DLL"

$msbuildVersion = ("" + (Get-NativeOutput $MSBuild @("-nologo", "-version") | Select-Object -Last 1)).Trim()
@(
    "ChakraCore $version ($commit) built for WelsonJS",
    "Version: $ProductVersion",
    "Source: $sourceOrigin",
    "Compiler: MSBuild $msbuildVersion ($MSBuild), Release|$platform",
    "Architecture: $Arch",
    "Patches: $(if ($patches.Count) { ($patches | ForEach-Object Name) -join ', ' } else { '(none)' })",
    "Build ID: $buildId",
    "Built: $([DateTime]::UtcNow.ToString('u'))"
) | Set-Content -LiteralPath $buildInfoPath -Encoding ASCII

Get-ChildItem -LiteralPath $archDir -File | Sort-Object Name | ForEach-Object { "$(Get-FileSha256 $_.FullName)  $($_.Name)" } |
    Set-Content -LiteralPath $checksumsPath -Encoding ASCII

Write-Host ("[*] Built ChakraCore into {0} in {1:N1} min" -f $archDir, $stopwatch.Elapsed.TotalMinutes)
Write-Host "[*] Files: $((Get-ChildItem -LiteralPath $archDir -File | ForEach-Object Name) -join ', ')"
exit 0
