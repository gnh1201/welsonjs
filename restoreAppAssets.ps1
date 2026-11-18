# WelsonJS application asset restore script
# SPDX-License-Identifier: GPL-3.0-or-later
# SPDX-FileCopyrightText: Namhyeon Go <gnh1201@catswords.re.kr>, and Catswords OSS contributors.
# https://github.com/gnh1201/welsonjs
#
# Restores the application assets under app/assets that are not stored in the
# repository. Currently these are the third-party (vendor) JavaScript files under
# app/assets/js.
#
# Every asset has a sidecar file "<file name>.integrity" that holds its
# Subresource Integrity digest (for example, "sha384-..."). The digest is looked
# up in app/assets/integrity.json to find the download URL. A downloaded file is saved
# only if its digest matches; an existing file is kept if it already matches.
#
# An asset that WelsonJS modifies is downloaded as the unmodified upstream file and
# patched here. Its index entry names the upstream file's digest and the patch:
#
#   "sha384-<digest of the patched file>": {
#     "url": "https://.../numbers.js",
#     "size": <size of the patched file>,
#     "source": { "integrity": "sha384-<digest of the download>", "size": <size> },
#     "patch": "app/assets/js/<file name>.patch"
#   }
#
# The download must match "source.integrity" before the patch (a unified diff for one
# file, relative to the application root) is applied, and the result must match the
# digest of the entry. Patching needs no external tool, and every hunk must match the
# file exactly.
#
# Usage:
#   powershell -ExecutionPolicy Bypass -NoProfile -File restoreAppAssets.ps1
#   powershell -ExecutionPolicy Bypass -NoProfile -File restoreAppAssets.ps1 -Force
#
# Requirements: Windows PowerShell 5.1 or later, and HTTPS access to the asset URLs.
# Exit code: 0 on success, 1 if any asset could not be downloaded or verified.

param(
    [string]$Root  = $PSScriptRoot,   # WelsonJS application root
    [switch]$Force                    # Download every asset again
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $Root -PathType Container)) {
    Write-Host "[ERROR] The application root was not found: $Root"
    exit 1
}
$Root = (Resolve-Path -LiteralPath $Root).Path.TrimEnd("\")

$IndexPath     = Join-Path $Root "app\assets\integrity.json"
$AssetsPath    = Join-Path $Root "app\assets\js"
$SidecarSuffix = ".integrity"
$MaxRetries    = 3

# Windows PowerShell 5.1 may default to older TLS versions.
[Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12

# ISO-8859-1 maps every byte to one character, so CRLF can be replaced without decoding the text.
$Latin1 = [Text.Encoding]::GetEncoding(28591)

function Get-Digest {
    param([byte[]]$Bytes, [string]$Algorithm)

    $hasher = switch ($Algorithm) {
        "sha256" { [Security.Cryptography.SHA256]::Create() }
        "sha384" { [Security.Cryptography.SHA384]::Create() }
        "sha512" { [Security.Cryptography.SHA512]::Create() }
        default  { throw "Unsupported integrity algorithm: $Algorithm" }
    }

    try {
        return "$Algorithm-" + [Convert]::ToBase64String($hasher.ComputeHash($Bytes))
    }
    finally {
        $hasher.Dispose()
    }
}

function Get-Algorithm {
    param([string]$Integrity)
    return $Integrity.Split("-")[0].ToLowerInvariant()
}

# Digests are computed over LF line endings. Git may check text files out with
# CRLF (core.autocrlf), so an existing file is also accepted if it matches after
# normalizing CRLF to LF.
function Test-AssetFile {
    param([string]$Path, [string]$Integrity)

    $algorithm = Get-Algorithm $Integrity
    $bytes = [IO.File]::ReadAllBytes($Path)
    if ((Get-Digest $bytes $algorithm) -ceq $Integrity) {
        return $true
    }

    $normalized = $Latin1.GetBytes($Latin1.GetString($bytes).Replace("`r`n", "`n"))
    return (Get-Digest $normalized $algorithm) -ceq $Integrity
}

# Applies a unified diff for one file to $Bytes and returns the patched bytes, with
# LF line endings (the line endings digests are computed over). Text is handled as
# ISO-8859-1, so every byte is kept as it is. Hunks are applied at the line numbers
# they state, and every context and removed line must match exactly.
function Invoke-AssetPatch {
    param([byte[]]$Bytes, [string]$PatchPath)

    $text = $Latin1.GetString($Bytes).Replace("`r`n", "`n")
    $newlineAtEnd = $text.EndsWith("`n")
    if ($newlineAtEnd) { $text = $text.Substring(0, $text.Length - 1) }
    $source = $text.Split("`n")

    $patch = $Latin1.GetString([IO.File]::ReadAllBytes($PatchPath)).Replace("`r`n", "`n").Split("`n")
    $output = New-Object 'Collections.Generic.List[string]'
    $pos = 0

    # Skip the description up to the file header ("--- " and "+++ ").
    $i = 0
    while ($i -lt $patch.Length -and -not $patch[$i].StartsWith("--- ")) { $i++ }
    if ($i + 1 -ge $patch.Length -or -not $patch[$i + 1].StartsWith("+++ ")) {
        throw "The patch has no file header: $PatchPath"
    }
    $i += 2

    while ($i -lt $patch.Length) {
        $line = $patch[$i]
        if ($line.Length -eq 0 -and $i -eq $patch.Length - 1) { break }
        if ($line -notmatch '^@@ -(\d+)(?:,(\d+))? \+\d+(?:,\d+)? @@') {
            throw "Unexpected line $($i + 1) in the patch (one file per patch): $PatchPath"
        }

        $oldStart = [int]$Matches[1]
        $oldCount = if ($Matches[2]) { [int]$Matches[2] } else { 1 }
        $index = if ($oldCount -eq 0) { $oldStart } else { $oldStart - 1 }
        if ($index -lt $pos -or $index -gt $source.Length) {
            throw "The hunk at line $($i + 1) of the patch is out of order or out of range: $PatchPath"
        }
        while ($pos -lt $index) { $output.Add($source[$pos]); $pos++ }
        $i++

        $lastTag = $null
        while ($i -lt $patch.Length -and -not $patch[$i].StartsWith("@@")) {
            $line = $patch[$i]
            if ($line.Length -eq 0) {
                if ($i -eq $patch.Length - 1) { break }
                throw "Empty line $($i + 1) in the patch: $PatchPath"
            }

            $tag = $line[0]
            $body = $line.Substring(1)
            switch ($tag) {
                ' ' {
                    if ($pos -ge $source.Length -or $source[$pos] -cne $body) {
                        throw "The patch does not match the file at line $($pos + 1) (patch line $($i + 1)): $PatchPath"
                    }
                    $output.Add($body); $pos++
                }
                '-' {
                    if ($pos -ge $source.Length -or $source[$pos] -cne $body) {
                        throw "The patch does not match the file at line $($pos + 1) (patch line $($i + 1)): $PatchPath"
                    }
                    $pos++
                }
                '+' { $output.Add($body) }
                '\' {
                    # "\ No newline at end of file" after a line of the new file.
                    if ($lastTag -eq '+' -or $lastTag -eq ' ') { $newlineAtEnd = $false }
                }
                default { throw "Unexpected line $($i + 1) in the patch: $PatchPath" }
            }
            if ($tag -ne '\') { $lastTag = [string]$tag }
            $i++
        }
    }
    while ($pos -lt $source.Length) { $output.Add($source[$pos]); $pos++ }

    $result = [string]::Join("`n", $output.ToArray())
    if ($newlineAtEnd) { $result += "`n" }
    return ,$Latin1.GetBytes($result)
}

function Get-RemoteBytes {
    param([string]$Url)

    if ($Url -notmatch '^https://') {
        throw "Only HTTPS URLs are allowed: $Url"
    }

    $attempt = 0
    while ($true) {
        $attempt++
        $client = New-Object Net.WebClient
        try {
            return $client.DownloadData($Url)
        }
        catch {
            if ($attempt -ge $MaxRetries) {
                throw "Download failed after $MaxRetries attempts: $Url ($($_.Exception.Message))"
            }
            Write-Host "[WARN] Download failed (attempt $attempt of $MaxRetries): $Url"
            Start-Sleep -Seconds 2
        }
        finally {
            $client.Dispose()
        }
    }
}

# ================================
# MAIN
# ================================
if (-not (Test-Path -LiteralPath $IndexPath -PathType Leaf)) {
    Write-Host "[ERROR] The integrity index was not found: $IndexPath"
    exit 1
}
if (-not (Test-Path -LiteralPath $AssetsPath -PathType Container)) {
    Write-Host "[ERROR] The asset folder was not found: $AssetsPath"
    exit 1
}

$hashes   = (Get-Content -LiteralPath $IndexPath -Raw -Encoding UTF8 | ConvertFrom-Json).hashes
$sidecars = @(Get-ChildItem -LiteralPath $AssetsPath -Recurse -File -Filter "*$SidecarSuffix" | Sort-Object FullName)
$rootFull = (Resolve-Path -LiteralPath $Root).Path.TrimEnd("\")

$kept = 0; $downloaded = 0; $failed = 0

Write-Host "[*] Integrity index : $IndexPath"
Write-Host "[*] Assets          : $($sidecars.Count) ($AssetsPath)"

foreach ($sidecar in $sidecars) {
    $target = $sidecar.FullName.Substring(0, $sidecar.FullName.Length - $SidecarSuffix.Length)
    $name   = $target.Substring($rootFull.Length + 1)

    try {
        $integrity = (Get-Content -LiteralPath $sidecar.FullName -Raw -Encoding UTF8).Trim()
        $entry = $hashes.PSObject.Properties[$integrity]
        if ($null -eq $entry) {
            throw "The digest is not listed in app\assets\integrity.json: $integrity"
        }

        if (-not $Force -and (Test-Path -LiteralPath $target -PathType Leaf) -and (Test-AssetFile $target $integrity)) {
            $kept++
            continue
        }

        $url    = $entry.Value.url
        $bytes  = Get-RemoteBytes $url

        if ($entry.Value.patch) {
            $sourceIntegrity = if ($entry.Value.source) { [string]$entry.Value.source.integrity } else { "" }
            if (-not $sourceIntegrity) {
                throw "The entry has a patch but no source.integrity: $integrity"
            }
            $sourceActual = Get-Digest $bytes (Get-Algorithm $sourceIntegrity)
            if ($sourceActual -cne $sourceIntegrity) {
                throw "Integrity verification failed for the upstream file (expected $sourceIntegrity, got $sourceActual): $url"
            }

            $patchPath = [IO.Path]::GetFullPath((Join-Path $rootFull ($entry.Value.patch -replace '/', '\')))
            if (-not $patchPath.StartsWith($rootFull + "\", [StringComparison]::OrdinalIgnoreCase) -or
                -not (Test-Path -LiteralPath $patchPath -PathType Leaf)) {
                throw "The patch was not found under the application root: $($entry.Value.patch)"
            }
            $bytes = Invoke-AssetPatch $bytes $patchPath
        }

        $actual = Get-Digest $bytes (Get-Algorithm $integrity)
        if ($actual -cne $integrity) {
            throw "Integrity verification failed (expected $integrity, got $actual): $url"
        }

        [IO.File]::WriteAllBytes($target, $bytes)
        $downloaded++
        if ($entry.Value.patch) {
            Write-Host "[*] Downloaded and patched: $name"
        }
        else {
            Write-Host "[*] Downloaded: $name"
        }
    }
    catch {
        $failed++
        Write-Host "[ERROR] ${name}: $($_.Exception.Message)"
    }
}

Write-Host "[*] Done: $downloaded downloaded, $kept already valid, $failed failed"
if ($failed -gt 0) { exit 1 }
exit 0
