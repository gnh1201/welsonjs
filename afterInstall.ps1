# ================================
# CONFIGURATION
# ================================
$AppName   = "welsonjs"
$TargetDir = Join-Path $env:APPDATA $AppName
$TmpDir    = Join-Path $env:TEMP "$AppName-downloads"

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
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    # If a file exists at this path, delete it
    if (Test-Path $Path -PathType Leaf) {
        Write-Host "[WARN] File exists at '$Path'. Removing..."
        Remove-Item -Path $Path -Force
    }

    # Ensure a directory exists at this path
    if (-not (Test-Path $Path -PathType Container)) {
        Write-Host "[*] Creating directory: $Path"
        New-Item -ItemType Directory -Path $Path -Force | Out-Null
    }
}

function Download-File {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Url,
        [Parameter(Mandatory = $true)]
        [string]$DestinationPath
    )

    Write-Host "[*] Downloading file..."
    Write-Host "    URL : $Url"
    Write-Host "    OUT : $DestinationPath"

    try {
        Invoke-WebRequest -Uri $Url -OutFile $DestinationPath -UseBasicParsing -ErrorAction Stop
        Write-Host "[OK] Download completed."
        Write-Host ""
    }
    catch {
        Write-Host "[ERROR] Failed to download: $Url"
        Write-Host $_.Exception.Message
        throw
    }
}

function Extract-CompressedFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$CompressedPath,
        [Parameter(Mandatory = $true)]
        [string]$DestinationDirectory
    )

    Write-Host "[*] Extracting compressed file:"
    Write-Host "    $CompressedPath"
    Write-Host "    -> $DestinationDirectory"

    # Ensure destination directory exists
    Ensure-EmptyDirectory -Path $DestinationDirectory

    # Temporary extraction workspace inside destination directory
    $TemporaryExtractDirectory = Join-Path $DestinationDirectory "__tmp_extract__"
    Ensure-EmptyDirectory -Path $TemporaryExtractDirectory

    Write-Host "[DEBUG] Expand-Archive command:"
    Write-Host "        Expand-Archive -LiteralPath `"$CompressedPath`" -DestinationPath `"$TemporaryExtractDirectory`" -Force"

    try {
        Expand-Archive -LiteralPath $CompressedPath -DestinationPath $TemporaryExtractDirectory -Force -ErrorAction Stop
    }
    catch {
        Write-Host "[ERROR] Failed to extract: $CompressedPath"
        Write-Host $_.Exception.Message
        throw
    }

    # Move extracted content into final destination
    $entries = Get-ChildItem -LiteralPath $TemporaryExtractDirectory

    if (-not $entries -or $entries.Count -eq 0) {
        Write-Host "[ERROR] No entries were extracted from archive: $CompressedPath"
        throw "Archive appears to be empty or extraction failed."
    }

    if ($entries.Count -eq 1 -and $entries[0].PSIsContainer) {
        # Single root directory inside archive -> flatten
        Write-Host "[*] Archive has a single root directory. Flattening..."
        $innerItems = Get-ChildItem -LiteralPath $entries[0].FullName
        foreach ($item in $innerItems) {
            Move-Item -LiteralPath $item.FullName -Destination $DestinationDirectory -Force
        }
    }
    else {
        # Multiple top-level entries
        Write-Host "[*] Archive has multiple top-level entries. Moving all..."
        foreach ($item in $entries) {
            Move-Item -LiteralPath $item.FullName -Destination $DestinationDirectory -Force
        }
    }

    # Clean up temporary extraction directory
    Remove-Item -LiteralPath $TemporaryExtractDirectory -Recurse -Force

    Write-Host "[OK] Extraction completed."
    Write-Host ""
}

function Extract-TarGzArchive {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ArchivePath,
        [Parameter(Mandatory = $true)]
        [string]$DestinationDirectory
    )

    Write-Host "[*] Extracting TAR.GZ archive:"
    Write-Host "    $ArchivePath"
    Write-Host "    -> $DestinationDirectory"

    Ensure-EmptyDirectory -Path $DestinationDirectory

    # Validate tar availability
    $tarCommand = Get-Command tar -ErrorAction SilentlyContinue
    if (-not $tarCommand) {
        Write-Host "[ERROR] 'tar' command not found."
        throw "tar not available on this system."
    }

    Write-Host "[DEBUG] tar command:"
    Write-Host "        tar -xzf `"$ArchivePath`" -C `"$DestinationDirectory`""

    try {
        & tar -xzf "$ArchivePath" -C "$DestinationDirectory"
        if ($LASTEXITCODE -ne 0) {
            throw "tar exited with code $LASTEXITCODE"
        }
        Write-Host "[OK] TAR.GZ extraction completed."
        Write-Host ""
    }
    catch {
        Write-Host "[ERROR] Failed to extract TAR.GZ archive: $ArchivePath"
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
$WebsocatUrl  = $null
$ArtifactsUrl = $null

switch ($arch) {
    "x64" {
        # Python embeddable (x64)
        $PythonUrl    = "https://www.python.org/ftp/python/3.13.9/python-3.13.9-embeddable-amd64.zip"

        # curl (x64, mingw)
        $CurlUrl      = "https://curl.se/windows/latest.cgi?p=win64-mingw.zip"

        # YARA (x64)
        $YaraUrl      = "https://github.com/VirusTotal/yara/releases/download/v4.5.5/yara-4.5.5-2368-win64.zip"

        # WAMR (x64)
        $WamrUrl      = "https://github.com/bytecodealliance/wasm-micro-runtime/releases/download/WAMR-2.4.3/iwasm-2.4.3-x86_64-windows-2022.tar.gz"

        # websocat (x64)
        $WebsocatUrl  = "https://catswords.blob.core.windows.net/welsonjs/websocat-1.14.0.x86_64-pc-windows-gnu.zip"
        
        # WelsonJS binary artifacts (x86 compatible)
        $ArtifactsUrl = "https://catswords.blob.core.windows.net/welsonjs/artifacts.zip"
    }

    "arm64" {
        # Python embeddable (ARM64)
        $PythonUrl    = "https://www.python.org/ftp/python/3.13.9/python-3.13.9-embeddable-arm64.zip"

        # curl (ARM64)
        $CurlUrl      = "https://curl.se/windows/latest.cgi?p=win64a-mingw.zip"

        # No YARA / WAMR / websocat / artifacts for ARM64 in this script
        $YaraUrl      = $null
        $WamrUrl      = $null
        $WebsocatUrl  = $null
        $ArtifactsUrl = $null
    }

    default {
        # Python embeddable (x86)
        $PythonUrl    = "https://www.python.org/ftp/python/3.13.9/python-3.13.9-embeddable-win32.zip"

        # curl (x86)
        $CurlUrl      = "https://downloads.sourceforge.net/project/muldersoft/cURL/curl-8.17.0-win-x86-full.2025-11-09.zip"

        # YARA (x86)
        $YaraUrl      = "https://github.com/VirusTotal/yara/releases/download/v4.5.5/yara-4.5.5-2368-win32.zip"

        # No WAMR for x86
        $WamrUrl      = $null

        # websocat (x86)
        $WebsocatUrl  = "https://catswords.blob.core.windows.net/welsonjs/websocat-1.14.0.i686-pc-windows-gnu.zip"
        
        # WelsonJS binary artifacts (x86 compatible)
        $ArtifactsUrl = "https://catswords.blob.core.windows.net/welsonjs/artifacts.zip"
    }
}

Write-Host "[*] Python URL    : $PythonUrl"
Write-Host "[*] curl URL      : $CurlUrl"
if ($YaraUrl) {
    Write-Host "[*] YARA URL      : $YaraUrl"
} else {
    Write-Host "[*] YARA          : skipped on this architecture"
}
if ($WamrUrl) {
    Write-Host "[*] WAMR URL      : $WamrUrl"
} else {
    Write-Host "[*] WAMR          : skipped on this architecture"
}
if ($WebsocatUrl) {
    Write-Host "[*] websocat URL  : $WebsocatUrl"
} else {
    Write-Host "[*] websocat      : skipped on this architecture"
}
if ($ArtifactsUrl) {
    Write-Host "[*] artifacts URL : $ArtifactsUrl"
} else {
    Write-Host "[*] artifacts     : skipped on this architecture"
}
Write-Host ""


# ================================
# DOWNLOAD FILES (websocat before artifacts)
# ================================
$PythonCompressed    = Join-Path $TmpDir "python.zip"
$CurlCompressed      = Join-Path $TmpDir "curl.zip"
$YaraCompressed      = Join-Path $TmpDir "yara.zip"
$WamrArchive         = Join-Path $TmpDir "wamr.tar.gz"
$WebsocatCompressed  = Join-Path $TmpDir "websocat.zip"
$ArtifactsCompressed = Join-Path $TmpDir "artifacts.zip"

try {
    # Python
    Download-File -Url $PythonUrl -DestinationPath $PythonCompressed

    # curl
    Download-File -Url $CurlUrl -DestinationPath $CurlCompressed

    # YARA (optional)
    if ($YaraUrl) {
        Download-File -Url $YaraUrl -DestinationPath $YaraCompressed
    }
    else
    {
        Write-Host "[*] YARA download skipped on this architecture."
    }

    # WAMR (optional)
    if ($WamrUrl) {
        Download-File -Url $WamrUrl -DestinationPath $WamrArchive
    }
    else
    {
        Write-Host "[*] WAMR download skipped on this architecture."
    }

    # websocat (optional)
    if ($WebsocatUrl) {
        Download-File -Url $WebsocatUrl -DestinationPath $WebsocatCompressed
    }
    else {
        Write-Host "[*] websocat download skipped on this architecture."
    }

    # artifacts
    if ($ArtifactsUrl) {
        Download-File -Url $ArtifactsUrl -DestinationPath $ArtifactsCompressed
    }
    else {
        Write-Host "[*] artifacts download skipped on this architecture."
    }
}
catch {
    Write-Host "[FATAL] Download phase faled."
    Write-Host $_.Exception.Message
    exit 1
}


# ================================
# EXTRACT / INSTALL (websocat before artifacts)
# ================================
try {
    # Python
    Extract-CompressedFile -CompressedPath $PythonCompressed -DestinationDirectory (Join-Path $TargetDir "python")

    # curl
    Extract-CompressedFile -CompressedPath $CurlCompressed   -DestinationDirectory (Join-Path $TargetDir "curl")

    # YARA
    if ($YaraUrl) {
        Extract-CompressedFile -CompressedPath $YaraCompressed -DestinationDirectory (Join-Path $TargetDir "yara")
    }
    else {
        Write-Host "[*] YARA installation skipped on this architecture."
    }

    # WAMR (TAR.GZ)
    if ($WamrUrl) {
        Extract-TarGzArchive -ArchivePath $WamrArchive -DestinationDirectory (Join-Path $TargetDir "wamr")
    }
    else {
        Write-Host "[*] WAMR installation skipped on this architecture."
    }

    # websocat
    if ($WebsocatUrl) {
        Extract-CompressedFile -CompressedPath $WebsocatCompressed -DestinationDirectory (Join-Path $TargetDir "websocat")
    }
    else {
        Write-Host "[*] websocat installation skipped on this architecture."
    }

    # artifacts
    if ($ArtifactsUrl) {
        Extract-CompressedFile -CompressedPath $ArtifactsCompressed -DestinationDirectory (Join-Path $TargetDir "bin")
    }
    else {
        Write-Host "[*] artifacts installation skipped on this architecture."
    }
}
catch {
    Write-Host "[FATAL] Extraction/installation phase failed."
    Write-Host $_.Exception.Message
    exit 1
}


# ================================
# FINISH
# ================================
Write-Host "[*] Installation completed successfully."
Write-Host "[*] Installed into: $TargetDir"
Write-Host ""
exit 0
