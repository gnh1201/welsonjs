# WelsonJS CrispEmbed build script
# SPDX-License-Identifier: GPL-3.0-or-later
# SPDX-FileCopyrightText: Namhyeon Go <gnh1201@catswords.re.kr>, and Catswords OSS contributors.
# https://github.com/gnh1201/welsonjs
#
# Builds CrispEmbed (https://github.com/CrispStrobe/CrispEmbed, MIT) at the
# commit in commit.txt (version.txt is a label only), with its ggml submodule at
# the commit that CrispEmbed commit pins and the patches in patches\, and places
# a Windows CPU build in <OutDir>\<arch>:
#   crispembed-server.exe (run by the launcher), crispembed.exe (the CLI),
#   crispembed-quantize.exe, the ggml DLLs (ggml, ggml-base, ggml-cpu), the
#   license and notice files, BUILDINFO.txt and SHA256SUMS.txt.
# The executables carry the WelsonJS icon (app\favicon.ico), and every
# executable and DLL carries version information (patches\
# 0001-windows-resources.patch): the WelsonJS version (-ProductVersion, by
# default the launcher's AssemblyFileVersion), a description that says
# "CrispEmbed built for WelsonJS", CompanyName, and the copyright of all
# authors. The build fails when a binary lacks that information.
# The build follows CrispEmbed's own Windows release: MSVC, a fixed x86-64 CPU
# baseline (GGML_NATIVE=OFF: SSE4.2, AVX, AVX2, FMA, F16C, BMI2; no AVX-512),
# no BLAS. The CrispEmbed shared library (crispembed.dll) is not built: the
# executables link CrispEmbed statically and only need the ggml DLLs. The build
# fails when a built executable or DLL is not listed for signing in
# .artifact-configuration.xml.
#
# A build is skipped when <OutDir>\<arch> was built from the same inputs
# (version, product version, commit, patches, the icon and this script; the
# "Build ID" in BUILDINFO.txt) and its files match SHA256SUMS.txt, so the folder
# can be kept in a CI cache.
#
# Source: the pinned commit and the ggml commit it pins are fetched with git. If
# that fails (-Source auto), or with -Source archive, the source archive in
# source-archive.txt is downloaded instead; it is used only if its SHA-256
# matches. A GitHub archive of CrispEmbed lacks the ggml submodule, so the
# archive is made for WelsonJS: the git archives of both commits in one folder
# "CrispEmbed-<commit>" (see README.md).
#
# Architectures:
#   x64   MSVC (Visual Studio generator), the x86-64 baseline above.
#   x86   MSVC (Visual Studio generator, Win32), the same baseline (AVX2): for
#         32-bit Windows on a CPU with AVX2, and for 32-bit processes.
#   arm64 Cross-compiled on x64 with clang (ggml does not support MSVC on ARM),
#         cmake\arm64-windows-llvm.cmake (-march=armv8.7-a, as the WelsonJS
#         llama.cpp ARM64 build) and Ninja, inside the Visual Studio x64_arm64
#         developer environment; ggml's own thread pool instead of OpenMP (with
#         clang, OpenMP needs a libomp that is not redistributable).
#         Needs LLVM: -LlvmDir, LLVM in Program Files or on PATH, or
#         -InstallLlvm ("choco install llvm" when clang is missing).
#
# Usage:
#   powershell -ExecutionPolicy Bypass -NoProfile -File build-crispembed.ps1 [-Arch x64|x86|arm64] [-OutDir <dir>] [-WorkDir <dir>] [-Source auto|git|archive] [-ProductVersion a.b.c.d] [-LlvmDir <dir>] [-InstallLlvm] [-Force]
#
# Requirements: git, CMake and Visual Studio (MSVC) with the C++ workload; for
# arm64 also the MSVC ARM64 build tools, Ninja (Visual Studio's CMake tools) and
# LLVM (clang).
# Output: <OutDir>\<arch>\* (for example, artifacts\crispembed\x64 in CI)

param(
    [string]$OutDir    = (Join-Path $PSScriptRoot "..\..\out\crispembed"),
    [string]$WorkDir   = (Join-Path $env:TEMP "welsonjs-crispembed-build"),
    [ValidateSet("x64", "x86", "arm64")]
    [string]$Arch      = "x64",
    [string]$Generator = "",          # x64 and x86 only; empty: the CMake default (Visual Studio)
    [int]$Jobs         = [Environment]::ProcessorCount,
    [string]$RepositoryUrl     = "https://github.com/CrispStrobe/CrispEmbed.git",
    [string]$GgmlRepositoryUrl = "https://github.com/CrispStrobe/ggml.git",
    [ValidateSet("auto", "git", "archive")]
    [string]$Source    = "auto",
    [string]$ProductVersion = "",     # a.b.c.d in the version information; empty: the launcher's AssemblyFileVersion
    [string]$LlvmDir   = "",          # arm64: folder with clang.exe; empty: search
    [switch]$InstallLlvm,             # arm64: install LLVM with choco when clang is missing
    [switch]$CheckOnly,               # only report: exit 0 when the output is up to date, 3 when a build is needed
    [switch]$Force
)

$ErrorActionPreference = "Stop"

# Windows PowerShell 5.1 may default to older TLS versions.
[Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12
# CMake targets and the executables they produce (crispembed-cli is crispembed.exe).
$Targets = [ordered]@{ "crispembed-server" = "crispembed-server.exe"; "crispembed-cli" = "crispembed.exe"; "crispembed-quantize" = "crispembed-quantize.exe" }

# Runs a native command and fails on a non-zero exit code. Native tools write
# warnings and progress to stderr; where PowerShell captures stderr, as
# AppVeyor's "ps:" steps do, each such line would become an error record, which
# $ErrorActionPreference = "Stop" turns into a terminating error. So stderr is
# printed like stdout, and only the exit code decides.
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
        $script:NativeExitCode = $LASTEXITCODE
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

# Relative paths are resolved against the current PowerShell location.
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

# Deletes a folder tree. The "\\?\" prefix lifts the MAX_PATH limit (CrispEmbed's
# test fixtures have long paths), and git keeps its object files read-only.
function Remove-Tree {
    param([string]$Path)
    $long = "\\?\" + [IO.Path]::GetFullPath($Path)
    if (-not [IO.Directory]::Exists($long)) { return }
    foreach ($file in [IO.Directory]::EnumerateFiles($long, "*", [IO.SearchOption]::AllDirectories)) {
        [IO.File]::SetAttributes($file, [IO.FileAttributes]::Normal)
    }
    [IO.Directory]::Delete($long, $true)
}

# arm64: finds clang.exe (-LlvmDir, then LLVM from "choco install llvm", then
# PATH). Returns $null when none is found.
function Find-Clang {
    $candidates = @()
    if ($LlvmDir) { $candidates += (Join-Path $LlvmDir "clang.exe") }
    else {
        $candidates += (Join-Path $env:ProgramFiles "LLVM\bin\clang.exe")
        $onPath = Get-Command "clang.exe" -ErrorAction SilentlyContinue
        if ($onPath) { $candidates += $onPath.Source }
    }
    foreach ($candidate in $candidates) { if (Test-Path -LiteralPath $candidate) { return $candidate } }
    return $null
}

# arm64: finds a Visual Studio installation and MSVC version with the ARM64
# C runtime (VC\Tools\MSVC\<version>\lib\arm64\msvcrt.lib; the folder alone
# can exist with only the AddressSanitizer libraries), preferring one that also
# has Visual Studio's CMake and Ninja. Returns the path and the MSVC version.
function Find-VsForArm64 {
    $vswhere = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer\vswhere.exe"
    if (-not (Test-Path -LiteralPath $vswhere)) { throw "vswhere.exe was not found; install Visual Studio or Build Tools with the C++ workload" }
    $found = @()
    foreach ($path in (Get-NativeOutput $vswhere @("-all", "-products", "*", "-requires", "Microsoft.VisualStudio.Component.VC.Tools.x86.x64", "-sort", "-property", "installationPath"))) {
        $versions = @(Get-ChildItem -Path (Join-Path $path "VC\Tools\MSVC") -Directory -ErrorAction SilentlyContinue |
            Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName "lib\arm64\msvcrt.lib") } |
            Sort-Object { [version]$_.Name } -Descending)
        if ($versions.Count -eq 0) { continue }
        $hasNinja = Test-Path -LiteralPath (Join-Path $path "Common7\IDE\CommonExtensions\Microsoft\CMake\Ninja\ninja.exe")
        $found += [pscustomobject]@{ Path = $path; Version = $versions[0].Name; HasNinja = $hasNinja }
    }
    $best = @($found | Where-Object HasNinja) + @($found | Where-Object { -not $_.HasNinja }) | Select-Object -First 1
    if (-not $best) {
        throw "No Visual Studio installation has the MSVC ARM64 C runtime (VC\Tools\MSVC\*\lib\arm64\msvcrt.lib). Install the 'MSVC ARM64/ARM64EC build tools' component (Microsoft.VisualStudio.Component.VC.Tools.ARM64)."
    }
    return $best
}

# arm64: loads a Visual Studio developer environment (vcvarsall.bat x64_arm64)
# into this process: the ARM64 MSVC libraries and Windows SDK and the x64-hosted
# ARM64 linker. Visual Studio's CMake and Ninja are put first on PATH, ahead of
# other copies (for example, an older CMake from Strawberry Perl).
function Import-VsDevEnvironment {
    param([string]$VsPath, [string]$VcvarsArch, [string]$VcToolsVersion = "")
    $vcvars = Join-Path $VsPath "VC\Auxiliary\Build\vcvarsall.bat"
    if (-not (Test-Path -LiteralPath $vcvars)) { throw "vcvarsall.bat was not found: $vcvars" }
    $versionArg = if ($VcToolsVersion) { " -vcvars_ver=$VcToolsVersion" } else { "" }
    $lines = Get-NativeOutput "cmd.exe" @("/d", "/s", "/c", "`"$vcvars`" $VcvarsArch$versionArg >nul && set")
    if ($script:NativeExitCode -ne 0) { throw "vcvarsall.bat $VcvarsArch$versionArg failed with exit code $($script:NativeExitCode)" }
    foreach ($line in $lines) {
        if ($line -match '^([^=]+)=(.*)$') { [Environment]::SetEnvironmentVariable($Matches[1], $Matches[2], "Process") }
    }
    foreach ($dir in @("Common7\IDE\CommonExtensions\Microsoft\CMake\Ninja", "Common7\IDE\CommonExtensions\Microsoft\CMake\CMake\bin")) {
        $full = Join-Path $VsPath $dir
        if (Test-Path -LiteralPath $full) { $env:PATH = $full + ";" + $env:PATH }
    }
}

# Fetches exactly one commit of a repository into a new folder. The checkout
# stays LF even where core.autocrlf is true (AppVeyor), so the patches apply.
function Get-GitCommit {
    param([string]$Url, [string]$Commit, [string]$Destination)
    Invoke-Checked "git" @("init", "--quiet", $Destination)
    Invoke-Checked "git" @("-C", $Destination, "config", "core.autocrlf", "false")
    Invoke-Checked "git" @("-C", $Destination, "config", "core.longpaths", "true")
    Invoke-Checked "git" @("-C", $Destination, "config", "advice.detachedHead", "false")
    Invoke-Checked "git" @("-C", $Destination, "remote", "add", "origin", $Url)
    Invoke-Checked "git" @("-C", $Destination, "fetch", "--quiet", "--depth", "1", "origin", $Commit)
    Invoke-Checked "git" @("-C", $Destination, "checkout", "--quiet", "FETCH_HEAD")
    $checkedOut = ("" + (Get-NativeOutput "git" @("-C", $Destination, "rev-parse", "HEAD") | Select-Object -First 1)).Trim().ToLowerInvariant()
    if ($checkedOut -ne $Commit) { throw "Checked out $checkedOut from $Url, but $Commit is pinned" }
}

# Fetches the pinned CrispEmbed commit, then the ggml submodule at the commit
# its tree records (a gitlink), from the URL in its .gitmodules.
function Get-SourceFromGit {
    param([string]$Destination)
    Get-GitCommit $RepositoryUrl $commit $Destination
    $gitlink = ("" + (Get-NativeOutput "git" @("-C", $Destination, "ls-tree", "HEAD", "ggml") | Select-Object -First 1)).Trim()
    if ($gitlink -notmatch '^160000 commit ([0-9a-f]{40})\s+ggml$') { throw "The CrispEmbed commit has no ggml submodule: $gitlink" }
    $script:ggmlCommit = $Matches[1]
    $ggmlUrl = ("" + (Get-NativeOutput "git" @("-C", $Destination, "config", "--file", ".gitmodules", "submodule.ggml.url") | Select-Object -First 1)).Trim()
    if ($ggmlUrl -ne $GgmlRepositoryUrl) { throw "The ggml submodule URL is $ggmlUrl, expected $GgmlRepositoryUrl (pass -GgmlRepositoryUrl after checking it)" }
    $ggmlDir = Join-Path $Destination "ggml"
    if (Test-Path -LiteralPath $ggmlDir) { Remove-Tree $ggmlDir }
    Get-GitCommit $ggmlUrl $script:ggmlCommit $ggmlDir
    return "git $RepositoryUrl, ggml $ggmlUrl at $($script:ggmlCommit)"
}

# Downloads the source archive from source-archive.txt (CrispEmbed and its ggml
# submodule in one top-level folder "CrispEmbed-<commit>"), verifies its SHA-256
# and extracts it with the top-level folder stripped.
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

    $archivePath = Join-Path $WorkDir "CrispEmbed-$commit.zip"
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
    $root = "CrispEmbed-$commit/"
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

            # The "\\?\" prefix lifts the MAX_PATH limit (test fixtures have long paths).
            $longTarget = "\\?\" + $target
            if ($name.EndsWith('/')) { [void][IO.Directory]::CreateDirectory($longTarget); continue }
            [void][IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($longTarget))
            [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $longTarget, $true)
        }
    }
    finally { $zip.Dispose() }
    Remove-Item -LiteralPath $archivePath -Force
    if (-not (Test-Path -LiteralPath (Join-Path $Destination "ggml\CMakeLists.txt"))) { throw "The source archive has no ggml submodule: $url" }
    $script:ggmlCommit = "(from the source archive)"
    return "archive $url (sha256 $actual)"
}

# ================================
# INPUTS
# ================================
$version = (Get-Content -LiteralPath (Join-Path $PSScriptRoot "version.txt") -Raw).Trim()
if ($version -notmatch '^[A-Za-z0-9._-]+$') { throw "Invalid CrispEmbed version: $version" }
# The source is pinned to a full commit SHA: a tag can be moved upstream, a commit cannot.
$commit = (Get-Content -LiteralPath (Join-Path $PSScriptRoot "commit.txt") -Raw).Trim().ToLowerInvariant()
if ($commit -notmatch '^[0-9a-f]{40}$') { throw "commit.txt must contain a full 40-character commit SHA: $commit" }
$patchDir = Join-Path $PSScriptRoot "patches"
$patches = @(if (Test-Path -LiteralPath $patchDir) { Get-ChildItem -LiteralPath $patchDir -Filter "*.patch" | Sort-Object Name })
# The icon embedded in the executables (CRISPEMBED_WIN_ICON, added by
# patches\0001-windows-resources.patch).
$iconPath = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\..\app\favicon.ico"))
if (-not (Test-Path -LiteralPath $iconPath)) { throw "The icon was not found: $iconPath" }

# Version information of every executable and DLL (CRISPEMBED_WIN_*, added by
# patches\0001-windows-resources.patch): the WelsonJS version, and the authors
# of the code: CrispEmbed (MIT, CrispStrobe), ggml (MIT, The ggml authors), and
# the WelsonJS patches and build (Catswords Research).
if (-not $ProductVersion) {
    $assemblyInfo = Join-Path $PSScriptRoot "..\WelsonJS.Augmented\WelsonJS.Launcher\Properties\AssemblyInfo.cs"
    $match = Select-String -LiteralPath $assemblyInfo -Pattern 'AssemblyFileVersion\("([0-9.]+)"\)' | Select-Object -First 1
    if (-not $match) { throw "AssemblyFileVersion was not found in $assemblyInfo" }
    $ProductVersion = $match.Matches[0].Groups[1].Value
}
if ($ProductVersion -notmatch '^\d+\.\d+\.\d+\.\d+$') { throw "The product version must be a.b.c.d: $ProductVersion" }
$companyName = "Catswords Research"
$copyright = "Copyright (c) 2026 CrispStrobe (CrispEmbed), Copyright (c) 2023-2026 The ggml authors (ggml), " +
             "Copyright (c) 2026 Catswords Research (WelsonJS)"
$winDescription = "CrispEmbed built for WelsonJS"
# Every executable and DLL must be listed (without wildcards) for SignPath.
$signingConfigPath = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\..\.artifact-configuration.xml"))
# arm64: the clang toolchain file.
$toolchainPath = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot "cmake\arm64-windows-llvm.cmake"))
if ($Arch -eq "arm64" -and -not (Test-Path -LiteralPath $toolchainPath)) { throw "The toolchain file was not found: $toolchainPath" }

# The build ID covers every input, so a change to any of them rebuilds. The ggml
# commit is part of the CrispEmbed commit.
$inputs = New-Object IO.MemoryStream
foreach ($item in @(@{ Name = "version"; Bytes = [Text.Encoding]::UTF8.GetBytes("$version|$ProductVersion|$commit|$Arch") }) +
                  @($patches | ForEach-Object { @{ Name = $_.Name; Bytes = [IO.File]::ReadAllBytes($_.FullName) } }) +
                  @(if ($Arch -eq "arm64") { @{ Name = "toolchain"; Bytes = [IO.File]::ReadAllBytes($toolchainPath) } }) +
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

Write-Host "[*] CrispEmbed version: $version, commit $commit ($Arch)"
Write-Host "[*] Product version   : $ProductVersion"
Write-Host "[*] Patches           : $(if ($patches.Count) { ($patches | ForEach-Object Name) -join ', ' } else { '(none)' })"
Write-Host "[*] Build ID          : $buildId"
Write-Host "[*] Output            : $archDir"

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
if ($CheckOnly) {
    Write-Host "[*] A build is needed."
    exit 3
}

# ================================
# TOOLCHAIN
# ================================
# Checked before the source is fetched, so a missing tool fails early.
if ($Arch -eq "arm64") {
    $clang = Find-Clang
    if (-not $clang -and $InstallLlvm) {
        Invoke-Checked "choco" @("install", "llvm", "-y", "--no-progress")
        $clang = Find-Clang
    }
    if (-not $clang) { throw "clang.exe was not found. Install LLVM (choco install llvm), or pass -LlvmDir or -InstallLlvm." }
    $vs = Find-VsForArm64
    Import-VsDevEnvironment $vs.Path "x64_arm64" $vs.Version
    # The toolchain file names the compilers "clang" and "clang++"; put LLVM first on PATH.
    $env:PATH = (Split-Path -Parent $clang) + ";" + $env:PATH
    foreach ($tool in @("cmake", "ninja")) {
        if (-not (Get-Command "$tool.exe" -ErrorAction SilentlyContinue)) { throw "$tool.exe was not found; install the Visual Studio 'C++ CMake tools for Windows' component" }
    }
    $cmake = (Get-Command cmake.exe).Source
    Write-Host "[*] Compiler          : $(Get-NativeOutput $clang @('--version') | Select-Object -First 1), Visual Studio $($vs.Path) (x64_arm64, MSVC $($vs.Version))"
    Write-Host "[*] Ninja             : $((Get-Command ninja.exe).Source)"
}
else {
    # Visual Studio's own CMake, which knows its generator; a CMake found first on
    # PATH (for example Strawberry Perl's) may default to another generator.
    $cmake = "cmake"
    $vswhere = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer\vswhere.exe"
    if (Test-Path -LiteralPath $vswhere) {
        $vsPath = ("" + (Get-NativeOutput $vswhere @("-latest", "-products", "*", "-requires", "Microsoft.VisualStudio.Component.VC.Tools.x86.x64", "-property", "installationPath") | Select-Object -First 1)).Trim()
        $vsCmake = if ($vsPath) { Join-Path $vsPath "Common7\IDE\CommonExtensions\Microsoft\CMake\CMake\bin\cmake.exe" } else { "" }
        if ($vsCmake -and (Test-Path -LiteralPath $vsCmake)) { $cmake = $vsCmake }
    }
}
Write-Host "[*] CMake             : $cmake"

# ================================
# SOURCE
# ================================
$WorkDir = Resolve-FullPath $WorkDir
$sourceDir = Join-Path $WorkDir "CrispEmbed"
$buildDir = Join-Path $WorkDir "build-$Arch"
Remove-Tree $WorkDir
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
        Remove-Tree $sourceDir
    }
}
if (-not $sourceOrigin) {
    $sourceOrigin = Get-SourceFromArchive $sourceDir
}
Write-Host "[*] Source: $sourceOrigin"

# git apply follows the global core.autocrlf (true on AppVeyor) and would write
# the patched files with CRLF; keep them LF like the rest of the source.
foreach ($patch in $patches) {
    Invoke-Checked "git" @("-c", "core.autocrlf=false", "-C", $sourceDir, "apply", "--whitespace=nowarn", $patch.FullName)
}

# ================================
# BUILD
# ================================
$configure = @("-S", $sourceDir, "-B", $buildDir)
if ($Arch -eq "arm64") {
    # clang with cmake\arm64-windows-llvm.cmake; clang reads the sources as
    # UTF-8 already (no /utf-8, which the clang driver would take for a file).
    $configure += @(
        "-G", "Ninja",
        "-DCMAKE_BUILD_TYPE=Release",
        "-DCMAKE_TOOLCHAIN_FILE=$($toolchainPath.Replace('\', '/'))",
        "-DGGML_OPENMP=OFF"
    )
}
else {
    if ($Generator) { $configure += @("-G", $Generator) }
    if (-not $Generator -or $Generator -like "Visual Studio*") {
        # Multi-config generator: the configuration is chosen at build time.
        $configure += @("-A", $(if ($Arch -eq "x86") { "Win32" } else { $Arch }))
    }
    else { $configure += @("-DCMAKE_BUILD_TYPE=Release") }
    $configure += @(
        # The sources are UTF-8 without a BOM. Without /utf-8, MSVC reads them in the
        # system code page, which breaks the build on a non-English Windows (for
        # example code page 949: "newline in constant" in string literals with
        # non-ASCII text). _INIT keeps CMake's default flags (/EHsc and the others).
        "-DCMAKE_C_FLAGS_INIT=/utf-8",
        "-DCMAKE_CXX_FLAGS_INIT=/utf-8"
    )
    if ($Arch -eq "x86") {
        # ggml's BMI2 code uses _pdep_u64, a 64-bit-only intrinsic that 32-bit
        # MSVC does not provide (LNK2019 in ggml-cpu).
        # /LARGEADDRESSAWARE: up to 4 GB of address space on 64-bit Windows
        # instead of 2 GB, for the model and its buffers.
        $configure += @("-DGGML_BMI2=OFF", "-DCMAKE_EXE_LINKER_FLAGS_INIT=/LARGEADDRESSAWARE")
    }
}
$configure += @(
    # A fixed CPU baseline instead of the build machine's CPU (CrispEmbed's
    # Windows release does the same; with GGML_NATIVE, MSVC can enable AVX-512).
    "-DGGML_NATIVE=OFF",
    "-DGGML_BLAS=OFF",
    # Do not pick up a ccache from PATH (for example, Strawberry Perl's).
    "-DGGML_CCACHE=OFF",
    # The executables link CrispEmbed statically.
    "-DCRISPEMBED_BUILD_SHARED=OFF",
    # The WelsonJS icon and version information in every executable and DLL
    # (patches\0001-windows-resources.patch): the file properties show
    # "<file> - <role> (CrispEmbed built for WelsonJS)".
    "-DCRISPEMBED_WIN_ICON=$($iconPath.Replace('\', '/'))",
    "-DCRISPEMBED_WIN_DESCRIPTION=$winDescription",
    "-DCRISPEMBED_WIN_VERSION=$ProductVersion",
    "-DCRISPEMBED_WIN_COMPANY=$companyName",
    "-DCRISPEMBED_WIN_COPYRIGHT=$copyright",
    "-DCRISPEMBED_WIN_COMMIT=$($commit.Substring(0, 7))"
)
Invoke-Checked $cmake $configure
Invoke-Checked $cmake (@("--build", $buildDir, "--config", "Release", "--parallel", "$Jobs", "--target") + @($Targets.Keys))

# ================================
# PACKAGE
# ================================
# The executables are in <build>\Release (MSVC) or <build>; the ggml DLLs in
# <build>\bin\Release or <build>\bin (ggml sets CMAKE_RUNTIME_OUTPUT_DIRECTORY).
$exeDirs = @((Join-Path $buildDir "Release"), $buildDir, (Join-Path $buildDir "bin\Release"), (Join-Path $buildDir "bin"))
if (Test-Path -LiteralPath $archDir) { Remove-Item -LiteralPath $archDir -Recurse -Force }
New-Item -ItemType Directory -Path $archDir -Force | Out-Null
foreach ($exe in $Targets.Values) {
    $found = $exeDirs | ForEach-Object { Join-Path $_ $exe } | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
    if (-not $found) { throw "$exe was not built (looked in $($exeDirs -join ', '))" }
    Copy-Item -LiteralPath $found -Destination $archDir
}
$dlls = @(Get-ChildItem -LiteralPath $buildDir -Recurse -Filter "ggml*.dll" | Where-Object { $_.DirectoryName -notlike "*\CMakeFiles\*" })
if ($dlls.Count -eq 0) { throw "No ggml DLL was built" }
foreach ($dll in $dlls) { Copy-Item -LiteralPath $dll.FullName -Destination $archDir -Force }

# The licenses that CrispEmbed's own Windows package ships.
$notices = [ordered]@{
    "LICENSE"                              = "LICENSE-CrispEmbed.txt"
    "NOTICE.marker"                        = "NOTICE.marker"
    "NOTICE.teleocr"                       = "NOTICE.teleocr"
    "NOTICE.sheenbidi"                     = "NOTICE.sheenbidi"
    "models\licenses\apache-2.0\LICENSE"   = "Apache-2.0.txt"
    "vendor\sheenbidi\LICENSE"             = "SheenBidi-LICENSE.txt"
    "ggml\LICENSE"                         = "LICENSE-ggml.txt"
}
foreach ($notice in $notices.GetEnumerator()) {
    $noticeSource = Join-Path $sourceDir $notice.Key
    if (-not (Test-Path -LiteralPath $noticeSource)) { throw "The license file was not found: $($notice.Key)" }
    Copy-Item -LiteralPath $noticeSource -Destination (Join-Path $archDir $notice.Value)
}

# Every executable and DLL must carry the version information, and be listed
# for signing in .artifact-configuration.xml.
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
        if ($listed -notcontains "artifacts/crispembed/$Arch/$($file.Name)".ToLowerInvariant()) {
            $problems += "$($file.Name): not listed for signing in .artifact-configuration.xml (artifacts/crispembed/$Arch/$($file.Name))"
        }
    }
}
else {
    Write-Host "[WARN] $signingConfigPath was not found; the signing list was not checked."
}
if ($problems.Count) {
    throw "The build output is incomplete:`n  " + ($problems -join "`n  ")
}
Write-Host "[*] Version information and signing list checked: $($binaries.Count) executables and DLLs"

# The compiler CMake found (CMakeFiles\<CMake version>\CMakeCXXCompiler.cmake).
$compiler = ""
$compilerFile = Get-ChildItem -LiteralPath (Join-Path $buildDir "CMakeFiles") -Recurse -Filter "CMakeCXXCompiler.cmake" -ErrorAction SilentlyContinue | Select-Object -First 1
if ($compilerFile) {
    $compilerText = Get-Content -LiteralPath $compilerFile.FullName -Raw
    $compilerId = if ($compilerText -match 'set\(CMAKE_CXX_COMPILER_ID "([^"]*)"\)') { $Matches[1] } else { "" }
    $compilerVersion = if ($compilerText -match 'set\(CMAKE_CXX_COMPILER_VERSION "([^"]*)"\)') { $Matches[1] } else { "" }
    $compiler = ("$compilerId $compilerVersion").Trim()
}
@(
    "CrispEmbed $version ($commit) built for WelsonJS",
    "Version: $ProductVersion",
    "ggml: $ggmlCommit",
    "Source: $sourceOrigin",
    "Compiler: $(if ($compiler) { $compiler } else { 'CMake default' })",
    "Architecture: $Arch ($(if ($Arch -eq 'arm64') { 'clang -march=armv8.7-a, no OpenMP' } else { 'GGML_NATIVE=OFF: SSE4.2, AVX, AVX2, FMA, F16C, BMI2' }))",
    "Patches: $(if ($patches.Count) { ($patches | ForEach-Object Name) -join ', ' } else { '(none)' })",
    "Build ID: $buildId",
    "Built: $([DateTime]::UtcNow.ToString('u'))"
) | Set-Content -LiteralPath $buildInfoPath -Encoding ASCII

# One "<sha256>  <file name>" line per file, written last.
Get-ChildItem -LiteralPath $archDir -File | Sort-Object Name | ForEach-Object { "$(Get-FileSha256 $_.FullName)  $($_.Name)" } |
    Set-Content -LiteralPath $checksumsPath -Encoding ASCII

Write-Host ("[*] Built CrispEmbed into {0} in {1:N1} min" -f $archDir, $stopwatch.Elapsed.TotalMinutes)
Write-Host "[*] Files: $((Get-ChildItem -LiteralPath $archDir -File | ForEach-Object Name) -join ', ')"
exit 0
