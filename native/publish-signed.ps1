# WelsonJS signed native binaries: publish
# SPDX-License-Identifier: GPL-3.0-or-later
# SPDX-FileCopyrightText: Namhyeon Go <gnh1201@catswords.re.kr>, and Catswords OSS contributors.
# https://github.com/gnh1201/welsonjs
#
# Publishes the SignPath-signed copies of a prebuilt component (see
# native\publish-prebuilt.ps1) to the WelsonJS blob storage, where
# Catswords.Phantomizer (AssemblyLoader.LoadNativeModules with a release name)
# loads them for the architecture of the running process:
#
#   <Container>/packages/native/<storage>/<version>-signed/SHA256SUMS.txt
#   <Container>/packages/native/<storage>/<version>-signed/<arch>/<file>
#   <Container>/packages/native/<storage>/<version>-signed/<arch>/<file>.gz
#
# SHA256SUMS.txt lists "<sha256>  <arch>/<file>" for every published file, and
# Phantomizer refuses a file that is not listed or does not match.
#
# The signed files come from a completed release-signing request of the SignPath
# project (the artifacts.zip that AppVeyor submits): by default the newest one,
# or -SigningRequestId. For every architecture the script checks that the request
# holds the build of the current version.txt and commit.txt (its BUILDINFO.txt and
# its SHA256SUMS.txt, which must give the hash recorded in
# native\<component>\prebuilt\<arch>.sha256 for the unsigned file), and that each
# file carries a valid Authenticode signature from the SignPath Foundation. Then
# it uploads with AzCopy (azcopy login first; an existing blob is never
# overwritten) and copies SHA256SUMS.txt to native\<component>\prebuilt\signed.sha256.
#
# The SignPath API token is read from $env:SIGNPATH_API_TOKEN, or else from the
# Azure Key Vault secret signpath-api-token of welsonjs-kv (az login first).
#
# Usage:
#   powershell -ExecutionPolicy Bypass -NoProfile -File native\publish-signed.ps1 -Component chakracore [-SigningRequestId <id>] [-AzCopy <path>] [-WhatIf]

param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("chakracore")]
    [string]$Component,
    [string]$SigningRequestId = "",
    [string]$OrganizationId   = "8ee5ec82-36dc-4619-ac33-5bc1117428fe",
    [string]$ProjectSlug      = "welsonjs",
    [string]$SigningPolicy    = "release-signing",
    [string]$KeyVault         = "welsonjs-kv",
    [string]$KeyVaultSecret   = "signpath-api-token",
    [string]$Container        = "https://catswords.blob.core.windows.net/welsonjs",
    [string]$AzCopy           = "",
    [switch]$WhatIf
)

$ErrorActionPreference = "Stop"
[Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

# The folder of each component in the storage, and the files that are published
# signed. ChakraCore keeps the casing of the folder Phantomizer loads from.
$components = @{
    "chakracore" = @{ Storage = "ChakraCore"; Files = @("ChakraCore.dll") }
}
$architectures = @("x64", "arm64", "x86")
$storageName = $components[$Component].Storage
$signedFiles = $components[$Component].Files

function Get-FileSha256 {
    param([string]$Path)
    $sha = [Security.Cryptography.SHA256]::Create()
    $stream = [IO.File]::OpenRead($Path)
    try { return ([BitConverter]::ToString($sha.ComputeHash($stream)) -replace "-", "").ToLowerInvariant() }
    finally { $stream.Dispose(); $sha.Dispose() }
}

function Read-Sums {
    param([string[]]$Lines, [string]$Source)
    $sums = @{}
    foreach ($line in $Lines) {
        if (-not $line.Trim()) { continue }
        if ($line -notmatch '^([0-9a-f]{64})  ([A-Za-z0-9._+-]+)$') { throw "Unexpected line in ${Source}: $line" }
        $sums[$Matches[2]] = $Matches[1]
    }
    return $sums
}

function Read-ZipText {
    param([IO.Compression.ZipArchive]$Zip, [string]$Name)
    $entry = $Zip.GetEntry($Name)
    if (-not $entry) { throw "$Name was not found in the signed artifact" }
    $reader = New-Object IO.StreamReader($entry.Open())
    try { return $reader.ReadToEnd() } finally { $reader.Dispose() }
}

$componentDir = Join-Path $PSScriptRoot $Component
$version = (Get-Content -LiteralPath (Join-Path $componentDir "version.txt") -Raw).Trim()
$commit = (Get-Content -LiteralPath (Join-Path $componentDir "commit.txt") -Raw).Trim().ToLowerInvariant()
if ($version -notmatch '^[A-Za-z0-9._-]+$') { throw "Invalid version in $Component\version.txt: $version" }
$release = "$version-signed"

# The SignPath API token.
$token = $env:SIGNPATH_API_TOKEN
if (-not $token) {
    $az = Get-Command "az" -ErrorAction SilentlyContinue
    $azPath = if ($az) { $az.Source } else { Join-Path ${env:ProgramFiles} "Microsoft SDKs\Azure\CLI2\wbin\az.cmd" }
    if (-not (Test-Path -LiteralPath $azPath)) { throw "Set SIGNPATH_API_TOKEN, or install the Azure CLI and az login to read it from Key Vault $KeyVault" }
    $token = (& $azPath keyvault secret show --vault-name $KeyVault --name $KeyVaultSecret --query value -o tsv)
    if ($LASTEXITCODE -ne 0 -or -not $token) { throw "Could not read the secret $KeyVaultSecret from Key Vault $KeyVault (az login first)" }
    $token = $token.Trim()
}
$api = "https://app.signpath.io/Api/v1/$OrganizationId"
$headers = @{ Authorization = "Bearer $token" }

# The signing request: the given one, or the newest completed one of the policy.
if (-not $SigningRequestId) {
    $list = Invoke-RestMethod -Uri "$api/SigningRequests?projectSlug=$ProjectSlug&signingPolicySlug=$SigningPolicy&signingRequestStatus=Completed" -Headers $headers
    $newest = @($list.signingRequests) | Sort-Object { [datetime]$_.submittedAt } -Descending | Select-Object -First 1
    if (-not $newest) { throw "No completed $SigningPolicy signing request was found in the SignPath project $ProjectSlug" }
    $SigningRequestId = $newest.signingRequestId
}
$request = Invoke-RestMethod -Uri "$api/SigningRequests/$SigningRequestId" -Headers $headers
if ($request.status -ne "Completed") { throw "Signing request $SigningRequestId is $($request.status), not Completed" }
if ($request.signingPolicySlug -ne $SigningPolicy) { throw "Signing request $SigningRequestId used $($request.signingPolicySlug), not $SigningPolicy" }
Write-Host "[*] Signing request $SigningRequestId ($($request.signingPolicySlug)), commit $($request.origin.repositoryData.commitId), $($request.origin.buildData.url)"

$work = Join-Path ([IO.Path]::GetTempPath()) "welsonjs-signed-$Component"
if (Test-Path -LiteralPath $work) { Remove-Item -LiteralPath $work -Recurse -Force }
$staging = Join-Path $work "staging"
New-Item -ItemType Directory -Path $staging -Force | Out-Null
try {
    $zipPath = Join-Path $work "signed.zip"
    Write-Host "[*] Downloading the signed artifact..."
    $previous = $ProgressPreference; $ProgressPreference = "SilentlyContinue"
    try { Invoke-WebRequest -Uri "$api/SigningRequests/$SigningRequestId/SignedArtifact" -Headers $headers -OutFile $zipPath -UseBasicParsing }
    finally { $ProgressPreference = $previous }

    $published = New-Object System.Collections.Generic.List[string]
    $zip = [IO.Compression.ZipFile]::OpenRead($zipPath)
    try {
        foreach ($arch in $architectures) {
            $prefix = "artifacts/$Component/$arch"
            $info = Read-ZipText $zip "$prefix/BUILDINFO.txt"
            if ($info -notmatch [regex]::Escape("$version ($commit)")) { throw "The signed artifact holds another build for ${arch}:`n$info" }
            if ($info -notmatch "Architecture: $arch\b") { throw "$prefix/BUILDINFO.txt is not for $arch" }
            $artifactSums = Read-Sums ((Read-ZipText $zip "$prefix/SHA256SUMS.txt") -split "`r?`n") "$prefix/SHA256SUMS.txt"
            $recordedPath = Join-Path $componentDir "prebuilt\$arch.sha256"
            $recorded = Read-Sums (Get-Content -LiteralPath $recordedPath) $recordedPath

            New-Item -ItemType Directory -Path (Join-Path $staging $arch) -Force | Out-Null
            foreach ($name in $signedFiles) {
                # The artifact must hold the recorded build ...
                if (-not $recorded.ContainsKey($name)) { throw "$name is not recorded in $recordedPath" }
                if ($artifactSums[$name] -ne $recorded[$name]) { throw "$prefix/$name was not built from the recorded $Component $arch build" }

                # ... and its file must be signed by the SignPath Foundation.
                $target = Join-Path (Join-Path $staging $arch) $name
                $entry = $zip.GetEntry("$prefix/$name")
                if (-not $entry) { throw "$prefix/$name was not found in the signed artifact" }
                [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $target)
                $signature = Get-AuthenticodeSignature -LiteralPath $target
                if ($signature.Status -ne "Valid") { throw "$prefix/$name is not validly signed ($($signature.Status)). Is it listed in the SignPath artifact configuration?" }
                if ($signature.SignerCertificate.Subject -notmatch 'CN=SignPath Foundation') { throw "$prefix/$name is signed by $($signature.SignerCertificate.Subject), not the SignPath Foundation" }
                if (-not $signature.TimeStamperCertificate) { throw "$prefix/$name has no timestamp" }
                $published.Add("$(Get-FileSha256 $target)  $arch/$name")
                Write-Host "[*] $arch/${name}: signed by $($signature.SignerCertificate.Subject.Split(',')[0])"
            }
        }
    }
    finally { $zip.Dispose() }
    Remove-Item -LiteralPath $zipPath -Force

    $sumsPath = Join-Path $staging "SHA256SUMS.txt"
    [IO.File]::WriteAllText($sumsPath, (($published -join "`n") + "`n"))

    # Each file as it is and as <file>.gz; SHA256SUMS.txt as it is.
    foreach ($file in @(Get-ChildItem -LiteralPath $staging -Recurse -File | Where-Object { $_.Name -ne "SHA256SUMS.txt" })) {
        $source = [IO.File]::OpenRead($file.FullName)
        try {
            $target = [IO.File]::Create("$($file.FullName).gz")
            try {
                $gzip = New-Object IO.Compression.GZipStream($target, [IO.Compression.CompressionLevel]::Optimal)
                try { $source.CopyTo($gzip) } finally { $gzip.Dispose() }
            }
            finally { $target.Dispose() }
        }
        finally { $source.Dispose() }
    }

    $destination = "$($Container.TrimEnd('/'))/packages/native/$storageName/$release/"
    Write-Host "[*] $Component $release`: $($published.Count) signed files -> $destination"
    if ($WhatIf) {
        Write-Host "[*] -WhatIf: not uploading."
    }
    else {
        if (-not $AzCopy) {
            $onPath = Get-Command "azcopy.exe" -ErrorAction SilentlyContinue
            if ($onPath) { $AzCopy = $onPath.Source }
            else {
                $AzCopy = Get-ChildItem -Path (Join-Path $env:LOCALAPPDATA "Microsoft\WinGet\Packages") -Recurse -Filter "azcopy.exe" -ErrorAction SilentlyContinue |
                    Select-Object -First 1 -ExpandProperty FullName
            }
        }
        if (-not $AzCopy -or -not (Test-Path -LiteralPath $AzCopy)) { throw "azcopy.exe was not found; install it (winget install Microsoft.Azure.AZCopy.10) or pass -AzCopy" }
        & $AzCopy copy (Join-Path $staging "*") $destination --recursive --overwrite=false
        if ($LASTEXITCODE -ne 0) { throw "azcopy failed with exit code $LASTEXITCODE" }
    }

    $record = Join-Path $componentDir "prebuilt\signed.sha256"
    Copy-Item -LiteralPath $sumsPath -Destination $record -Force
    Write-Host "[*] Recorded the signed files and hashes in $record."
}
finally {
    if (Test-Path -LiteralPath $work) { Remove-Item -LiteralPath $work -Recurse -Force }
}
exit 0
