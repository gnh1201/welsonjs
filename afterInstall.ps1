# ================================
# CONFIGURATION
# ================================
$AppName   = "welsonjs"
$TargetDir = Join-Path $env:APPDATA $AppName
$TmpDir    = Join-Path $env:TEMP    "$AppName-downloads"

Write-Host ""
Write-Host "[*] Target directory   : $TargetDir"
Write-Host "[*] Temporary directory: $TmpDir"
Write-Host ""

# Ensure base directories exist
New-Item -ItemType Directory -Path $TargetDir -Force | Out-Null
New-Item -ItemType Directory -Path $TmpDir    -Force | Out-Null


# ================================
# ARCHITECTURE DETECTION
# ================================
$arch = "x86"
if ($env:PROCESSOR_ARCHITECTURE -eq "AMD64") { $arch = "x64" }
elseif ($env:PROCESSOR_ARCHITECTURE -eq "ARM64") { $arch = "arm64" }
if ($env:PROCESSOR_ARCHITEW6432 -eq "AMD64") { $arch = "x64" }
elseif ($env:PROCESSOR_ARCHITEW6432 -eq "ARM64") { $arch = "arm64" }

Write-Host "[*] Detected architecture: $arch"
Write-Host ""


# ================================
# HELPER FUNCTIONS
# ================================

function Ensure-EmptyDirectory {
    param(
        [Parameter(Mandatory=$true)]
        [string]$Path
    )

    # If a file exists at the path, remove it
    if (Test-Path $Path -PathType Leaf) {
        Write-Host "[WARN] A file exists at path '$Path'. Deleting it..."
        Remove-Item -Path $Path -Force
    }

    # Ensure directory exists
    if (-not (Test-Path $Path -PathType Container)) {
        Write-Host "[*] Creating directory: $Path"
        New-Item -ItemType Directory -Path $Path -Force | Out-Null
    }
}

function Download-File {
    param(
        [Parameter(Mandatory=$true)]
        [string]$Url,
        [Parameter(Mandatory=$true)]
        [string]$Destination
    )

    Write-Host "[*] Downloading file..."
    Write-Host "    URL : $Url"
    Write-Host "    OUT : $Destination"

    try {
        Invoke-WebRequest -Uri $Url -OutFile $Destination -UseBasicParsing -ErrorAction Stop
        Write-Host "[OK] Download completed."
        Write-Host ""
    }
    catch {
        Write-Host "[ERROR] Failed to download: $Url"
        Write-Host $_.Exception.Message
        throw
    }
}

function Extract-Zip {
    param(
        [Parameter(Mandatory=$true)]
        [string]$ZipPath,
        [Parameter(Mandatory=$true)]
        [string]$DestDir
    )

    Write-Host "[*] Extracting ZIP:"
    Write-Host "    $ZipPath"
    Write-Host "    -> $DestDir"

    # Make sure destination directory exists and is a directory
    Ensure-EmptyDirectory -Path $DestDir

    # Temporary extraction folder inside destination
    $TmpExtract = Join-Path $DestDir "__tmp_extract__"
    Ensure-EmptyDirectory -Path $TmpExtract

    Write-Host "[DEBUG] PowerShell command:"
    Write-Host "        Expand-Archive -LiteralPath `"$ZipPath`" -DestinationPath `"$TmpExtract`" -Force"

    try {
        Expand-Archive -LiteralPath $ZipPath -DestinationPath $TmpExtract -Force -ErrorAction Stop
    }
    catch {
        Write-Host "[ERROR] Failed to extract ZIP with Expand-Archive: $ZipPath"
        Write-Host $_.Exception.Message
        throw
    }

    # Check extracted entries
    $entries = Get-ChildItem -LiteralPath $TmpExtract

    if (-not $entries -or $entries.Count -eq 0) {
        Write-Host "[ERROR] No entries were extracted from ZIP: $ZipPath"
        throw "ZIP appears to be empty or extraction failed."
    }

    if ($entries.Count -eq 1 -and $entries[0].PSIsContainer) {
        Write-Host "[*] Detected single root directory inside ZIP. Flattening..."

        # Move children of the single root directory into destination
        $innerItems = Get-ChildItem -LiteralPath $entries[0].FullName
        foreach ($item in $innerItems) {
            Move-Item -LiteralPath $item.FullName -Destination $DestDir -Force
        }
    }
    else {
        Write-Host "[*] ZIP has multiple top-level entries. Copying all..."

        foreach ($item in $entries) {
            Move-Item -LiteralPath $item.FullName -Destination $DestDir -Force
        }
    }

    # Clean up temp extraction folder
    Remove-Item -LiteralPath $TmpExtract -Recurse -Force

    Write-Host "[OK] ZIP extraction completed."
    Write-Host ""
}

function Extract-TarGz {
    param(
        [Parameter(Mandatory=$true)]
        [string]$TarGzPath,
        [Parameter(Mandatory=$true)]
        [string]$DestDir
    )

    Write-Host "[*] Extracting TAR.GZ:"
    Write-Host "    $TarGzPath"
    Write-Host "    -> $DestDir"

    Ensure-EmptyDirectory -Path $DestDir

    # Modern Windows ships with tar, but we validate its presence
    $tarCmd = Get-Command tar -ErrorAction SilentlyContinue
    if (-not $tarCmd) {
        Write-Host "[ERROR] 'tar' command not found. Cannot extract TAR.GZ."
        throw "tar command not found."
    }

    Write-Host "[DEBUG] tar command:"
    Write-Host "        tar -xzf `"$TarGzPath`" -C `"$DestDir`""

    try {
        & tar -xzf "$TarGzPath" -C "$DestDir"
        if ($LASTEXITCODE -ne 0) {
            throw "tar exit code $LASTEXITCODE"
        }
        Write-Host "[OK] TAR.GZ extraction completed."
        Write-Host ""
    }
    catch {
        Write-Host "[ERROR] Failed to extract TAR.GZ: $TarGzPath"
        Write-Host $_.Exception.Message
        throw
    }
}


# ================================
# SET DOWNLOAD URLS BASED ON ARCH
# ================================
$PythonUrl    = $null
$CurlUrl      = $null
$YaraUrl      = $null
$WamrUrl      = $null

# WelsonJS binary artifacts
$ArtifactsUrl = "https://catswords.blob.core.windows.net/welsonjs/artifacts.zip"

switch ($arch) {
    "x64" {
        # Python embeddable (x64)
        $PythonUrl = "https://www.python.org/ftp/python/3.13.9/python-3.13.9-embeddable-amd64.zip"

        # curl (x64, mingw)
        $CurlUrl   = "https://curl.se/windows/latest.cgi?p=win64-mingw.zip"

        # YARA (x64, GitHub — as you specified)
        $YaraUrl   = "https://github.com/VirusTotal/yara/releases/download/v4.5.5/yara-4.5.5-2368-win64.zip"

        # WAMR (x64 only)
        $WamrUrl   = "https://github.com/bytecodealliance/wasm-micro-runtime/releases/download/WAMR-2.4.3/iwasm-2.4.3-x86_64-windows-2022.tar.gz"
    }

    "arm64" {
        # Python embeddable (ARM64)
        $PythonUrl = "https://www.python.org/ftp/python/3.13.9/python-3.13.9-embeddable-arm64.zip"

        # curl (ARM64)
        $CurlUrl   = "https://curl.se/windows/latest.cgi?p=win64a-mingw.zip"

        # DO NOT install YARA/WAMR on ARM64
        $YaraUrl   = $null
        $WamrUrl   = $null
    }

    default {
        # Treat anything else as x86
        # Python embeddable (x86)
        $PythonUrl = "https://www.python.org/ftp/python/3.13.9/python-3.13.9-embeddable-win32.zip"

        # curl (x86)
        $CurlUrl   = "https://downloads.sourceforge.net/project/muldersoft/cURL/curl-8.17.0-win-x86-full.2025-11-09.zip";

        # Do NOT install YARA/WAMR on x86 (same policy as before)
        $YaraUrl   = $null
        $WamrUrl   = $null
    }
}

Write-Host "[*] Python URL   : $PythonUrl"
Write-Host "[*] curl URL    : $CurlUrl"
if ($YaraUrl) {
    Write-Host "[*] YARA URL   : $YaraUrl"
} else {
    Write-Host "[*] YARA       : skipped on this architecture"
}
if ($WamrUrl) {
    Write-Host "[*] WAMR URL   : $WamrUrl"
} else {
    Write-Host "[*] WAMR       : skipped on this architecture"
}
Write-Host "[*] artifacts URL: $ArtifactsUrl"
Write-Host ""


# ================================
# DOWNLOAD FILES
# ================================
$PythonZip    = Join-Path $TmpDir "python.zip"
$CurlZip      = Join-Path $TmpDir "curl.zip"
$YaraZip      = Join-Path $TmpDir "yara.zip"
$WamrTgz      = Join-Path $TmpDir "wamr.tar.gz"
$ArtifactsZip = Join-Path $TmpDir "artifacts.zip"

try {
    Download-File -Url $PythonUrl    -Destination $PythonZip
    Download-File -Url $CurlUrl      -Destination $CurlZip

    if ($YaraUrl) {
        Download-File -Url $YaraUrl  -Destination $YaraZip
    }

    if ($WamrUrl) {
        Download-File -Url $WamrUrl  -Destination $WamrTgz
    }

    Download-File -Url $ArtifactsUrl -Destination $ArtifactsZip
}
catch {
    Write-Host "[FATAL] Download phase failed."
    exit 1
}


# ================================
# EXTRACT FILES
# ================================
try {
    Extract-Zip  -ZipPath $PythonZip    -DestDir (Join-Path $TargetDir "python")
    Extract-Zip  -ZipPath $CurlZip      -DestDir (Join-Path $TargetDir "curl")

    if ($YaraUrl) {
        Extract-Zip -ZipPath $YaraZip   -DestDir (Join-Path $TargetDir "yara")
    }

    if ($WamrUrl) {
        Extract-TarGz -TarGzPath $WamrTgz -DestDir (Join-Path $TargetDir "wamr")
    }

    Extract-Zip  -ZipPath $ArtifactsZip -DestDir (Join-Path $TargetDir "bin")
}
catch {
    Write-Host "[FATAL] Extraction phase failed."
    exit 1
}


# ================================
# FINISH
# ================================
Write-Host "[*] All tools installed successfully."
Write-Host "[*] Installed into: $TargetDir"
Write-Host ""
exit 0
