# WelsonJS llama.cpp build script
# SPDX-License-Identifier: GPL-3.0-or-later
# SPDX-FileCopyrightText: Namhyeon Go <gnh1201@catswords.re.kr>, and Catswords OSS contributors.
# https://github.com/gnh1201/welsonjs
#
# Builds llama.cpp (the version in version.txt) with the patches in patches\
# and packages a Windows CPU build that can replace the official
# llama-<version>-bin-win-cpu-<arch>.zip package:
#   llama-server, llama-cli, llama-quantize and the ggml backends, with every
#   CPU variant loaded at run time (GGML_BACKEND_DL, GGML_CPU_ALL_VARIANTS).
#
# A build is skipped when -OutDir already holds a package built from the same
# inputs (version, patches and this script), so the output folder can be kept
# in a CI cache.
#
# Usage:
#   powershell -ExecutionPolicy Bypass -NoProfile -File build-llamacpp.ps1 [-OutDir <dir>] [-WorkDir <dir>] [-Force]
#
# Requirements: git, CMake and Visual Studio (MSVC) with the C++ workload.
# Output: <OutDir>\llama-<version>-welsonjs-bin-win-cpu-<arch>.zip (+ .sha256)

param(
    [string]$OutDir    = (Join-Path $PSScriptRoot "..\..\out\llamacpp"),
    [string]$WorkDir   = (Join-Path $env:TEMP "welsonjs-llamacpp-build"),
    [string]$Arch      = "x64",
    [string]$Generator = "",          # empty: the CMake default (Visual Studio)
    [int]$Jobs         = [Environment]::ProcessorCount,
    [switch]$Force
)

$ErrorActionPreference = "Stop"
$RepositoryUrl = "https://github.com/ggml-org/llama.cpp.git"
$Targets = @("llama-server", "llama-cli", "llama-quantize")

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

# ================================
# INPUTS
# ================================
$version = (Get-Content -LiteralPath (Join-Path $PSScriptRoot "version.txt") -Raw).Trim()
if ($version -notmatch '^[A-Za-z0-9._-]+$') { throw "Invalid llama.cpp version: $version" }
$patches = @(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot "patches") -Filter "*.patch" | Sort-Object Name)

# The build ID covers every input, so a change to any of them rebuilds.
$inputs = New-Object IO.MemoryStream
foreach ($item in @(@{ Name = "version"; Bytes = [Text.Encoding]::UTF8.GetBytes("$version|$Arch") }) +
                  @($patches | ForEach-Object { @{ Name = $_.Name; Bytes = [IO.File]::ReadAllBytes($_.FullName) } }) +
                  @(@{ Name = "script"; Bytes = [IO.File]::ReadAllBytes($PSCommandPath) })) {
    $name = [Text.Encoding]::UTF8.GetBytes($item.Name + "`n")
    $inputs.Write($name, 0, $name.Length)
    $inputs.Write($item.Bytes, 0, $item.Bytes.Length)
}
$buildId = Get-Sha256 $inputs.ToArray()

$OutDir = [IO.Path]::GetFullPath($OutDir)
$packageName = "llama-$version-welsonjs-bin-win-cpu-$Arch"
$packagePath = Join-Path $OutDir "$packageName.zip"
$buildIdPath = Join-Path $OutDir "BUILD_ID.txt"

Write-Host "[*] llama.cpp version: $version ($Arch)"
Write-Host "[*] Patches          : $(if ($patches.Count) { ($patches | ForEach-Object Name) -join ', ' } else { '(none)' })"
Write-Host "[*] Build ID         : $buildId"
Write-Host "[*] Output           : $packagePath"

if (-not $Force -and (Test-Path -LiteralPath $packagePath) -and (Test-Path -LiteralPath $buildIdPath) -and
    ((Get-Content -LiteralPath $buildIdPath -Raw).Trim() -eq $buildId)) {
    Write-Host "[*] A package built from the same inputs already exists. Skipping the build."
    exit 0
}

# ================================
# SOURCE
# ================================
$WorkDir = [IO.Path]::GetFullPath($WorkDir)
$sourceDir = Join-Path $WorkDir "llama.cpp"
$buildDir = Join-Path $WorkDir "build-$Arch"
if (Test-Path -LiteralPath $WorkDir) { Remove-Item -LiteralPath $WorkDir -Recurse -Force }
New-Item -ItemType Directory -Path $WorkDir -Force | Out-Null

$stopwatch = [Diagnostics.Stopwatch]::StartNew()

# The patches are LF; keep the checkout LF even where core.autocrlf is true (AppVeyor).
Invoke-Checked "git" @("-c", "core.autocrlf=false", "-c", "advice.detachedHead=false", "clone", "--quiet", "--depth", "1", "--branch", $version, $RepositoryUrl, $sourceDir)
Invoke-Checked "git" @("-C", $sourceDir, "config", "core.autocrlf", "false")
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
    "-DLLAMA_CONNECT=OFF"
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

$stageDir = Join-Path $WorkDir $packageName
New-Item -ItemType Directory -Path $stageDir -Force | Out-Null
Get-ChildItem -LiteralPath $binDir -File | Where-Object { $_.Extension -in @(".dll") -or ($Targets -contains $_.BaseName -and $_.Extension -eq ".exe") } |
    Copy-Item -Destination $stageDir
Copy-Item -LiteralPath (Join-Path $sourceDir "LICENSE") -Destination (Join-Path $stageDir "LICENSE-llama.cpp.txt")

$commit = (& git -C $sourceDir rev-parse HEAD).Trim()
@(
    "llama.cpp $version ($commit) built for WelsonJS",
    "Architecture: $Arch (CPU backends: $($backendTargets -join ', '))",
    "Patches: $(if ($patches.Count) { ($patches | ForEach-Object Name) -join ', ' } else { '(none)' })",
    "Build ID: $buildId",
    "Built: $([DateTime]::UtcNow.ToString('u'))"
) | Set-Content -LiteralPath (Join-Path $stageDir "BUILDINFO.txt") -Encoding ASCII

New-Item -ItemType Directory -Path $OutDir -Force | Out-Null
if (Test-Path -LiteralPath $packagePath) { Remove-Item -LiteralPath $packagePath -Force }
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::CreateFromDirectory($stageDir, $packagePath, [IO.Compression.CompressionLevel]::Optimal, $false)

$packageHash = Get-Sha256 ([IO.File]::ReadAllBytes($packagePath))
Set-Content -LiteralPath "$packagePath.sha256" -Value "$packageHash  $packageName.zip" -Encoding ASCII
Set-Content -LiteralPath $buildIdPath -Value $buildId -Encoding ASCII

Write-Host ("[*] Built {0} in {1:N1} min" -f "$packageName.zip", $stopwatch.Elapsed.TotalMinutes)
Write-Host "[*] Files  : $((Get-ChildItem -LiteralPath $stageDir -File | ForEach-Object Name) -join ', ')"
Write-Host "[*] SHA-256: $packageHash"
exit 0
