# WelsonJS llama.cpp build script
# SPDX-License-Identifier: GPL-3.0-or-later
# SPDX-FileCopyrightText: Namhyeon Go <gnh1201@catswords.re.kr>, and Catswords OSS contributors.
# https://github.com/gnh1201/welsonjs
#
# Builds llama.cpp from the fork that bitnet.cpp (https://github.com/microsoft/BitNet)
# uses as its 3rdparty/llama.cpp submodule, at the commit in commit.txt
# (version.txt is a label only), with the patches in patches\, and places a
# Windows CPU build, with the same files as the official
# llama-<version>-bin-win-cpu-<arch>.zip package, in <OutDir>\<arch>:
#   llama-server, llama-cli, llama-quantize and the ggml backends, with every
#   CPU variant loaded at run time (GGML_BACKEND_DL, GGML_CPU_ALL_VARIANTS),
#   plus BUILDINFO.txt and SHA256SUMS.txt. The executables carry the WelsonJS
#   icon (app\favicon.ico, LLAMA_WIN_ICON) and version information whose
#   description says "llama.cpp built for WelsonJS" (LLAMA_WIN_DESCRIPTION);
#   a patch adds both options.
#
# Besides the usual GGUF types, the fork loads BitNet's I2_S GGUF files (for
# example microsoft/BitNet-b1.58-2B-4T-gguf) with its I2_S kernels. The BitNet
# repository itself is not needed: it only holds the TL1/TL2 kernels, which
# are off (patches\0002-bitnet-i2s.patch).
#
# A build is skipped when <OutDir>\<arch> was built from the same inputs
# (version, commit, patches, the icon and this script; the "Build ID" in
# BUILDINFO.txt) and its files match SHA256SUMS.txt, so the folder can be kept
# in a CI cache.
#
# Source: the pinned commit is fetched with git. If that fails (-Source auto),
# or with -Source archive, the source archive in source-archive.txt is
# downloaded instead; it is used only if its SHA-256 matches.
#
# Architectures:
#   x64   MSVC (Visual Studio generator), every x86-64 CPU variant.
#   arm64 Cross-compiled on x64 with clang (ggml does not support MSVC on ARM),
#         the fork's cmake\arm64-windows-llvm.cmake toolchain (-march=armv8.7-a)
#         and Ninja, inside the Visual Studio x64_arm64 developer environment.
#         GGML_CPU_ALL_VARIANTS is not supported for ARM on Windows, so there
#         is one CPU backend (ggml-cpu.dll). -InstallLlvm runs
#         "choco install llvm" when a build is needed and clang is missing.
#
# Usage:
#   powershell -ExecutionPolicy Bypass -NoProfile -File build-llamacpp.ps1 [-Arch x64|arm64] [-OutDir <dir>] [-WorkDir <dir>] [-Source auto|git|archive] [-LlvmDir <dir>] [-InstallLlvm] [-Force]
#
# Requirements: git, CMake and Visual Studio (MSVC) with the C++ workload; for
# arm64 also the MSVC ARM64 build tools, Ninja and LLVM (clang).
# Output: <OutDir>\<arch>\* (for example, artifacts\llamacpp\x64 in CI)

param(
    [string]$OutDir    = (Join-Path $PSScriptRoot "..\..\out\llamacpp"),
    [string]$WorkDir   = (Join-Path $env:TEMP "welsonjs-llamacpp-build"),
    [ValidateSet("x64", "arm64")]
    [string]$Arch      = "x64",
    [string]$Generator = "",          # x64 only; empty: the CMake default (Visual Studio)
    [int]$Jobs         = [Environment]::ProcessorCount,
    [ValidateSet("auto", "git", "archive")]
    [string]$Source    = "auto",
    [string]$RepositoryUrl = "https://github.com/isHuangXin/llama.cpp.git",
    [string]$LlvmDir   = "",          # arm64: folder with clang.exe; empty: search
    [switch]$InstallLlvm,
    [switch]$Force
)

$ErrorActionPreference = "Stop"
$Targets = @("llama-server", "llama-cli", "llama-quantize")

# Windows PowerShell 5.1 may default to older TLS versions.
[Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12

# Runs a native command and fails on a non-zero exit code. Native tools write
# warnings and progress to stderr (for example, CMake's "Manually-specified
# variables were not used"). Where PowerShell captures stderr, as AppVeyor's
# "ps:" steps do, each such line becomes an error record, which
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

# Runs a native command and returns its stdout lines. Its stderr lines are
# printed, not raised (see Invoke-Checked); for example, vcvarsall.bat prints
# "'vswhere.exe' is not recognized ..." to stderr and still succeeds.
# $script:NativeExitCode holds the exit code.
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

# Deletes a folder tree. The "\\?\" prefix lifts the MAX_PATH limit, which
# Remove-Item hits on the deepest llama.cpp Web UI paths.
function Remove-Tree {
    param([string]$Path)
    $long = "\\?\" + [IO.Path]::GetFullPath($Path)
    if (-not [IO.Directory]::Exists($long)) { return }
    # git keeps its object files read-only, which Directory.Delete refuses.
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

# Loads a Visual Studio developer environment (vcvarsall.bat <arch>) into this
# process: for x64_arm64, the ARM64 MSVC libraries and Windows SDK and the
# x64-hosted ARM64 linker. Visual Studio's CMake and Ninja are put first on
# PATH, ahead of other copies (for example, an older CMake from Strawberry Perl).
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

# Fetches exactly the pinned commit (not a tag). The patches are LF; keep the
# checkout LF even where core.autocrlf is true (AppVeyor). The Web UI sources
# have paths longer than MAX_PATH.
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

# Downloads the source archive from source-archive.txt (a "git archive" of the
# pinned commit, with one top-level folder "llama.cpp-<commit>"), verifies its
# SHA-256 and extracts it with the top-level folder stripped.
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

            # The "\\?\" prefix lifts the MAX_PATH limit: the deepest Web UI
            # paths are 161 characters below the source folder. Symbolic links
            # (the fork's Swift package headers) are written as small text
            # files holding the link target; the build does not use them.
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
# patches\0003-windows-build.patch).
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
# TOOLCHAIN (arm64)
# ================================
# Checked before the source is fetched, so a missing tool fails early.
$compilerInfo = "MSVC"
if ($Arch -eq "arm64") {
    $clang = Find-Clang
    if (-not $clang -and $InstallLlvm) {
        Invoke-Checked "choco" @("install", "llvm", "-y", "--no-progress")
        $clang = Find-Clang
    }
    if (-not $clang) { throw "clang.exe was not found. Install LLVM (choco install llvm), or pass -LlvmDir or -InstallLlvm." }
    $vs = Find-VsForArm64
    $vsPath = $vs.Path
    Import-VsDevEnvironment $vsPath "x64_arm64" $vs.Version
    # The toolchain file names the compilers "clang" and "clang++"; put LLVM first on PATH.
    $env:PATH = (Split-Path -Parent $clang) + ";" + $env:PATH
    foreach ($tool in @("cmake", "ninja")) {
        if (-not (Get-Command "$tool.exe" -ErrorAction SilentlyContinue)) { throw "$tool.exe was not found; install the Visual Studio 'C++ CMake tools for Windows' component" }
    }
    $compilerInfo = "$(Get-NativeOutput $clang @('--version') | Select-Object -First 1), Visual Studio $vsPath (x64_arm64, MSVC $($vs.Version))"
    Write-Host "[*] Compiler         : $compilerInfo"
    Write-Host "[*] CMake            : $((Get-Command cmake.exe).Source)"
    Write-Host "[*] Ninja            : $((Get-Command ninja.exe).Source)"
}

# ================================
# SOURCE
# ================================
$WorkDir = Resolve-FullPath $WorkDir
$sourceDir = Join-Path $WorkDir "llama.cpp"
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

# git apply also works outside a repository (archive source), but there it
# follows the global core.autocrlf (true on AppVeyor) and would write the
# patched files with CRLF; keep them LF like the rest of the source.
foreach ($patch in $patches) {
    Invoke-Checked "git" @("-c", "core.autocrlf=false", "-C", $sourceDir, "apply", "--whitespace=nowarn", $patch.FullName)
}

# ================================
# BUILD
# ================================
$configure = @("-S", $sourceDir, "-B", $buildDir)
if ($Arch -eq "arm64") {
    # clang with the fork's toolchain file (target arm64-pc-windows-msvc,
    # -march=armv8.7-a); one CPU backend, as GGML_CPU_ALL_VARIANTS is not
    # supported for ARM on Windows.
    # tools/ui builds a host (x64) tool, llama-ui-embed, by running the first
    # g++ or clang++ on PATH with only "-O2 -std=c++17 -o". In this
    # environment clang++ targets x64 but links with LIB, which vcvars set to
    # the ARM64 libraries (LNK2019/LNK4272 on AppVeyor); a g++ found elsewhere
    # (e.g. Strawberry Perl) would hide that. Give it a wrapper that sets LIB to
    # the x64 libraries and calls clang++ for x86_64-pc-windows-msvc.
    $hostLib = @(($env:LIB -split ";") | Where-Object { $_ } | ForEach-Object { $_ -replace '\\arm64(?=\\|$)', '\x64' })
    $missingLib = @($hostLib | Where-Object { -not (Test-Path -LiteralPath $_) })
    if ($missingLib.Count -gt 0) { throw "x64 libraries for the host tool were not found: $($missingLib -join '; ')" }
    $hostCxx = Join-Path $WorkDir "host-cxx.cmd"
    $hostClangxx = Join-Path (Split-Path -Parent $clang) "clang++.exe"
    @(
        "@echo off",
        "rem Host (x64) C++ compiler for tools that run during the ARM64 build.",
        "set `"LIB=$($hostLib -join ';')`"",
        "`"$hostClangxx`" --target=x86_64-pc-windows-msvc %*",
        "exit /b %ERRORLEVEL%"
    ) | Set-Content -LiteralPath $hostCxx -Encoding ASCII
    $configure += @(
        "-G", "Ninja",
        "-DCMAKE_BUILD_TYPE=Release",
        "-DCMAKE_TOOLCHAIN_FILE=$((Join-Path $sourceDir 'cmake\arm64-windows-llvm.cmake').Replace('\', '/'))",
        "-DHOST_CXX_COMPILER=$($hostCxx.Replace('\', '/'))",
        "-DGGML_CPU_ALL_VARIANTS=OFF",
        # With clang, OpenMP needs libomp140.aarch64.dll, which Visual Studio
        # ships only as debug_nonredist (not redistributable); use ggml's own
        # thread pool instead. (x64/MSVC uses VCOMP140.DLL from the VC++
        # redistributable.)
        "-DGGML_OPENMP=OFF"
    )
}
else {
    if ($Generator) { $configure += @("-G", $Generator) }
    if (-not $Generator -or $Generator -like "Visual Studio*") {
        # Multi-config generator: the configuration is chosen at build time
        # (--config Release); CMAKE_BUILD_TYPE would only produce an
        # "unused variable" warning.
        $configure += @("-A", $Arch)
    }
    else { $configure += @("-DCMAKE_BUILD_TYPE=Release") }
    $configure += @("-DGGML_CPU_ALL_VARIANTS=ON")
}
$configure += @(
    "-DBUILD_SHARED_LIBS=ON",
    "-DGGML_NATIVE=OFF",
    "-DGGML_BACKEND_DL=ON",
    # Do not pick up a ccache from PATH (for example, Strawberry Perl's).
    "-DGGML_CCACHE=OFF",
    # BitNet: the I2_S kernels only. The TL1/TL2 lookup-table kernels need the
    # BitNet repository and code generated for one model.
    "-DGGML_BITNET_ARM_TL1=OFF",
    "-DGGML_BITNET_X86_TL2=OFF",
    "-DLLAMA_BUILD_TESTS=OFF",
    "-DLLAMA_BUILD_EXAMPLES=OFF",
    "-DLLAMA_BUILD_SERVER=ON",
    "-DLLAMA_BUILD_APP=OFF",
    # The tools only need HTTP on localhost.
    "-DLLAMA_OPENSSL=OFF",
    # No downloads during the build: the server's Web UI is fetched from a
    # floating Hugging Face location otherwise (the launcher only uses the API).
    "-DLLAMA_BUILD_UI=OFF",
    "-DLLAMA_USE_PREBUILT_UI=OFF",
    # The WelsonJS icon and version information in every tool executable
    # (patches\0003-windows-build.patch): the file properties show
    # "<tool> (llama.cpp built for WelsonJS)".
    "-DLLAMA_WIN_ICON=$($iconPath.Replace('\', '/'))",
    "-DLLAMA_WIN_DESCRIPTION=llama.cpp built for WelsonJS",
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
    "Compiler: $compilerInfo",
    "Architecture: $Arch (CPU backends: $($backendTargets -join ', '))",
    "BitNet kernels: I2_S (TL1/TL2 off)",
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
