# WelsonJS post-install script
# Namhyeon Go <gnh1201@catswords.re.kr>, and Catswords OSS contributors.
# Updated on: 2025-12-20
# https://github.com/gnh1201/welsonjs

# ================================
# PARAMETERS
# ================================
param(
    [string]$TelemetryProvider = "",
    [string]$TelemetryApiKey   = "",
    [string]$Version           = "",
    [string]$DistinctId        = "",
    [string]$Components        = ""
)

# ================================
# LOGO
# ================================
$logo = @"
 __        __   _                     _ ____  
 \ \      / /__| |___  ___  _ __     | / ___| 
  \ \ /\ / / _ \ / __|/ _ \| '_ \ _  | \___ \ 
   \ V  V /  __/ \__ \ (_) | | | | |_| |___) |
    \_/\_/ \___|_|___/\___/|_| |_| \___/|____/ 

  WelsonJS post-install script
  https://github.com/gnh1201/welsonjs

"@

Write-Host $logo

# Fix TLS 1.2 connectivity issue (Tested in Windows 8.1)
# Enable TLS 1.2 and TLS 1.3 (if available) - avoid deprecated TLS 1.0/1.1
try {
    [Net.ServicePointManager]::SecurityProtocol = `
        [Net.SecurityProtocolType]::Tls12 -bor `
        [Net.SecurityProtocolType]::Tls13
} catch {
    # TLS 1.3 not available, fall back to TLS 1.2 only
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
}

# ================================
# SCRIPT ROOT RESOLUTION
# ================================
# Ensure $ScriptRoot is available even on older PowerShell
$ScriptRoot = if ($PSScriptRoot) {
    $PSScriptRoot
}
elseif ($MyInvocation.MyCommand.Path) {
    Split-Path -Parent $MyInvocation.MyCommand.Path
}
else {
    (Get-Location).Path
}

# ================================
# LOAD DOWNLOAD URL TABLE (DownloadUrls.psd1 in /data folder)
# ================================
$DownloadUrls = @{}
$urlsFilePath = Join-Path $ScriptRoot "data/DownloadUrls.psd1"

if (Test-Path $urlsFilePath) {
    try {
        if (Get-Command Import-PowerShellDataFile -ErrorAction SilentlyContinue) {
            $DownloadUrls = Import-PowerShellDataFile -Path $urlsFilePath
        } else {
            $DownloadUrls = Invoke-Expression (Get-Content $urlsFilePath -Raw)  # Tested in Windows 8.1
        }
    }
    catch {
        Write-Host "[WARN] Failed to load DownloadUrls.psd1. Falling back to empty URL table."
        $DownloadUrls = @{}
    }
}
else {
    Write-Host "[WARN] DownloadUrls.psd1 not found at: $urlsFilePath"
    $DownloadUrls = @{}
}

function Get-DownloadUrl {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Component,
        [Parameter(Mandatory = $true)]
        [string]$Arch  # x64, arm64, x86
    )

    $componentKey = $Component.ToLowerInvariant()

    if (-not $DownloadUrls.ContainsKey($componentKey)) {
        return $null
    }

    $entry = $DownloadUrls[$componentKey]

    # Prefer arch-specific URL
    if ($entry.ContainsKey($Arch)) {
        return $entry[$Arch]
    }

    # Fallback to "any" (arch-independent)
    if ($entry.ContainsKey("any")) {
        return $entry["any"]
    }

    return $null
}

# ================================
# TELEMETRY
# ================================
if ($TelemetryProvider -and $TelemetryProvider.ToLower() -eq "posthog") {

    # Skip telemetry if API key is missing
    if (-not $TelemetryApiKey -or $TelemetryApiKey.Trim() -eq "") {
        # No-op: continue script
    }
    else {
        # Resolve distinct ID (fallback to machine name, then device UID)
        $finalDistinctId = if ($DistinctId -and $DistinctId.Trim() -ne "") {
            $DistinctId
        } else {
            # Attempt to get the machine name
            $computerName = $env:COMPUTERNAME

            if ($computerName -and $computerName.Trim() -ne "") {
                $computerName
            } else {
                # Fall back to using the device UUID (if COMPUTERNAME is unavailable)
                $deviceUid = (Get-WmiObject -Class Win32_ComputerSystemProduct).UUID
                if ($deviceUid -and $deviceUid.Trim() -ne "") {
                    $deviceUid
                } else {
                    # Optionally, generate a new UUID or use a predefined value if UUID is also unavailable
                    [guid]::NewGuid().ToString()
                }
            }
        }

        if ($finalDistinctId -and $finalDistinctId.Trim() -ne "") {
            # Get current script file name
            $scriptName = if (Get-Variable -Name PSCommandPath -ErrorAction SilentlyContinue) {
                Split-Path $PSCommandPath -Leaf
            } else {
                Split-Path $MyInvocation.MyCommand.Path -Leaf
            }

            # Build single event payload for PostHog /i/v0/e endpoint
            $body = @{
                api_key     = $TelemetryApiKey
                event       = "app_installed"
                distinct_id = $finalDistinctId
                properties  = @{
                    product    = "welsonjs"
                    version    = $Version
                    os         = "windows"
                    source     = $scriptName
                    components = $Components            # Keep raw string here
                }
                timestamp   = (Get-Date).ToString("o")   # ISO 8601 format
            } | ConvertTo-Json -Depth 5

            try {
                Invoke-RestMethod `
                    -Uri "https://us.i.posthog.com/i/v0/e/" `
                    -Method Post `
                    -ContentType "application/json" `
                    -Body $body | Out-Null
            }
            catch {
                # Ignore telemetry failure (installer must not break)
            }
        }
    }
}

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
# COMPONENT SELECTION (SINGLE PARSE)
# ================================
# Convert Components (string) → array exactly once.
# Example: "python,curl,websocat"
# If empty → treat as "all selected" for backward compatibility.

$SelectedComponents    = @()
$AllComponentsSelected = $true

if ($Components -and $Components.Trim() -ne "") {
    $SelectedComponents =
        $Components.Split(",") |
        ForEach-Object { $_.Trim().ToLowerInvariant() }

    $AllComponentsSelected = $false
}

function Test-ComponentSelected {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    if ($AllComponentsSelected) {
        return $true
    }

    return $SelectedComponents -contains $Name.ToLowerInvariant()
}

Write-Host "[*] Selected components (raw): $Components"
if ($AllComponentsSelected) {
    Write-Host "[*] Component filter       : <none> (treat as ALL selected)"
} else {
    Write-Host "[*] Component filter       : $($SelectedComponents -join ', ')"
}
Write-Host ""

# ================================
# ARCHITECTURE DETECTION
# ================================
function Get-NativeArchitecture {
    # https://learn.microsoft.com/windows/win32/cimwin32prov/win32-processor
    $arch = $null

    try {
        $proc = Get-CimInstance -ClassName Win32_Processor -ErrorAction Stop |
                Select-Object -First 1

        switch ($proc.Architecture) {
            0       { $arch = "x86"   }   # 32-bit Intel/AMD
            5       { $arch = "arm32" }   # 32-bit ARM
            12      { $arch = "arm64" }   # treat ARM as arm64 target
            9       { $arch = "x64"   }   # 64-bit Intel/AMD
            6       { $arch = "ia64"  }   # Intel Itanium
            default { $arch = "x86"   }   # fallback
        }
    }
    catch {
        # Fallback: only 32/64 bit detection if WMI/CIM is not available
        if ([System.Environment]::Is64BitOperatingSystem) {
            $arch = "x64"
        }
        else {
            $arch = "x86"
        }
    }

    return $arch
}

$arch = Get-NativeArchitecture

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

    Write-Host "[*] Downloading:"
    Write-Host "    $Url"
    Write-Host "    -> $DestinationPath"

    # Ensure destination directory exists
    $destDir = Split-Path -Parent $DestinationPath
    if ($destDir -and -not (Test-Path $destDir)) {
        New-Item -ItemType Directory -Path $destDir -Force | Out-Null
    }

    $maxRetries = 3
    $attempt    = 0
    $success    = $false

    while (-not $success -and $attempt -lt $maxRetries) {
        $attempt++
        try {
            Invoke-WebRequest -Uri $Url -OutFile $DestinationPath -UseBasicParsing
            $success = $true
        }
        catch {
            Write-Host "[WARN] Download failed (attempt $attempt of $maxRetries): $($_.Exception.Message)"
            if ($attempt -lt $maxRetries) {
                Start-Sleep -Seconds 5
            }
        }
    }

    if (-not $success) {
        throw "Failed to download $Url after $maxRetries attempts."
    }
}

function Invoke-7zr {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,

        [Parameter(Mandatory = $false)]
        [string[]]$PipeToArguments
    )

    $sevenZip = Join-Path $ScriptRoot "bin\x86\7zr.exe"
    if (-not (Test-Path $sevenZip)) {
        throw "7zr.exe is missing: $sevenZip"
    }

    Write-Host "[INFO] Using 7zr.exe:"
    Write-Host "       $sevenZip"
    Write-Host "[DEBUG] 7zr args:"
    Write-Host "        $($Arguments -join ' ')"

    if ($PipeToArguments) {
        Write-Host "[DEBUG] 7zr pipe-to args:"
        Write-Host "        $($PipeToArguments -join ' ')"

        & $sevenZip @Arguments | & $sevenZip @PipeToArguments
    }
    else {
        & $sevenZip @Arguments
    }

    if ($LASTEXITCODE -ne 0) {
        throw "7zr exited with code $LASTEXITCODE."
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

    Ensure-EmptyDirectory -Path $DestinationDirectory

    $tmpExtractDir = Join-Path $DestinationDirectory "_tmp_extract"
    Ensure-EmptyDirectory -Path $tmpExtractDir

    $extractedOk = $false
    $zipErrorMsg = $null

    # Try ZipFile first
    try {
        Add-Type -AssemblyName System.IO.Compression.FileSystem -ErrorAction Stop
        [System.IO.Compression.ZipFile]::ExtractToDirectory($CompressedPath, $tmpExtractDir)
        $extractedOk = $true
    }
    catch {
        $zipErrorMsg = $_.Exception.Message
        Write-Host "[WARN] ZipFile extraction failed. Falling back to 7zr.exe."
        Write-Host "       $zipErrorMsg"
    }

    # Fallback: 7zr.exe
    if (-not $extractedOk) {
        Invoke-7zr -Arguments @("x", $CompressedPath, "-o$tmpExtractDir", "-y")
        $extractedOk = $true
    }

    # Detect root folder unwrap
    $entries    = Get-ChildItem -Path $tmpExtractDir -Force
    $SourceRoot = $tmpExtractDir

    if ($entries.Count -eq 1 -and $entries[0].PSIsContainer) {
        $SourceRoot = $entries[0].FullName
        Write-Host "[*] Detected single root folder inside archive: $($entries[0].Name)"
        Write-Host "[*] Unwrapping folder content..."
    }
    else {
        Write-Host "[*] Extracting multi-item archive (no root folder unwrapping needed)."
    }

    # Move items into final destination
    Get-ChildItem -Path $SourceRoot -Force | ForEach-Object {
        $targetPath = Join-Path $DestinationDirectory $_.Name

        if (Test-Path $targetPath) {
            Remove-Item -Path $targetPath -Recurse -Force
        }
        Move-Item -Path $_.FullName -Destination $targetPath
    }

    Remove-Item -Path $tmpExtractDir -Recurse -Force
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

    # Try tar first
    $tarCommand = Get-Command tar -ErrorAction SilentlyContinue
    if ($tarCommand) {
        Write-Host "[DEBUG] tar command:"
        Write-Host "        tar -xzf `"$ArchivePath`" -C `"$DestinationDirectory`""

        & tar -xzf "$ArchivePath" -C "$DestinationDirectory"
        if ($LASTEXITCODE -ne 0) {
            throw "tar exited with code $LASTEXITCODE."
        }
        return
    }

    Write-Host "[WARN] 'tar' not found. Falling back to 7zr.exe."

    Invoke-7zr `
        -Arguments @("x", $ArchivePath, "-so") `
        -PipeToArguments @("x", "-ttar", "-si", "-o$DestinationDirectory", "-y")
}


# ================================
# COMPRESSED / INSTALLER PATHS
# ================================
$PythonCompressed        = Join-Path $TmpDir "python.zip"
$CurlCompressed          = Join-Path $TmpDir "curl.zip"
$YaraCompressed          = Join-Path $TmpDir "yara.zip"
$WamrArchive             = Join-Path $TmpDir "wamr.tar.gz"
$WebsocatCompressed      = Join-Path $TmpDir "websocat.zip"
$ArtifactsCompressed     = Join-Path $TmpDir "artifacts.zip"
$GtkRuntimeInstaller     = Join-Path $TmpDir "gtk-runtime.exe"
$TessdataCompressed      = Join-Path $TmpDir "tessdata.zip"
$TessdataBestCompressed  = Join-Path $TmpDir "tessdata_best.zip"
$TessdataFastCompressed  = Join-Path $TmpDir "tessdata_fast.zip"
$NpcapInstaller          = Join-Path $TmpDir "npcap-setup.exe"
$NmapInstaller           = Join-Path $TmpDir "nmap-setup.exe"
$GtkServerCompressed     = Join-Path $TmpDir "gtkserver.zip"
$WinDivertCompressed     = Join-Path $TmpDir "windivert.zip"
$AndroidPlatformToolsCompressed = Join-Path $TmpDir "android-platform-tools.zip"

# ================================
# DOWNLOAD PHASE
# ================================
try {
    # Python (component: python)
    if (Test-ComponentSelected -Name "python") {
        $url = Get-DownloadUrl -Component "python" -Arch $arch
        if ($url) {
            Download-File -Url $url -DestinationPath $PythonCompressed
        }
        else {
            Write-Host "[*] Python URL not available for arch: $arch. Skipping download."
        }
    }
    else {
        Write-Host "[*] Python component not selected. Skipping download."
    }

    # curl (component: curl)
    if (Test-ComponentSelected -Name "curl") {
        $url = Get-DownloadUrl -Component "curl" -Arch $arch
        if ($url) {
            Download-File -Url $url -DestinationPath $CurlCompressed
        }
        else {
            Write-Host "[*] curl URL not available for arch: $arch. Skipping download."
        }
    }
    else {
        Write-Host "[*] curl component not selected. Skipping download."
    }

    # YARA (component: yara)
    if (Test-ComponentSelected -Name "yara") {
        $url = Get-DownloadUrl -Component "yara" -Arch $arch
        if ($url) {
            Download-File -Url $url -DestinationPath $YaraCompressed
        }
        else {
            Write-Host "[*] YARA URL not available for arch: $arch. Skipping download."
        }
    }
    else {
        Write-Host "[*] YARA component not selected. Skipping download."
    }

    # WAMR (component: wamr)
    if (Test-ComponentSelected -Name "wamr") {
        $url = Get-DownloadUrl -Component "wamr" -Arch $arch
        if ($url) {
            Download-File -Url $url -DestinationPath $WamrArchive
        }
        else {
            Write-Host "[*] WAMR URL not available for arch: $arch. Skipping download."
        }
    }
    else {
        Write-Host "[*] WAMR component not selected. Skipping download."
    }

    # websocat (component: websocat)
    if (Test-ComponentSelected -Name "websocat") {
        $url = Get-DownloadUrl -Component "websocat" -Arch $arch
        if ($url) {
            Download-File -Url $url -DestinationPath $WebsocatCompressed
        }
        else {
            Write-Host "[*] websocat URL not available for arch: $arch. Skipping download."
        }
    }
    else {
        Write-Host "[*] websocat component not selected. Skipping download."
    }

    # artifacts (component: artifacts)
    if (Test-ComponentSelected -Name "artifacts") {
        $url = Get-DownloadUrl -Component "artifacts" -Arch $arch
        if ($url) {
            Download-File -Url $url -DestinationPath $ArtifactsCompressed
        }
        else {
            Write-Host "[*] artifacts URL not available for arch: $arch. Skipping download."
        }
    }
    else {
        Write-Host "[*] artifacts component not selected. Skipping download."
    }

    # GTK3 runtime (component: gtk3runtime)
    if (Test-ComponentSelected -Name "gtk3runtime") {
        $url = Get-DownloadUrl -Component "gtk3runtime" -Arch $arch
        if ($url) {
            Download-File -Url $url -DestinationPath $GtkRuntimeInstaller
        }
        else {
            Write-Host "[*] gtk3runtime URL not available for arch: $arch. Skipping download."
        }
    }
    else {
        Write-Host "[*] gtk3runtime component not selected. Skipping download."
    }

    # GTK server (component: gtkserver)
    if (Test-ComponentSelected -Name "gtkserver") {
        $url = Get-DownloadUrl -Component "gtkserver" -Arch $arch
        if ($url) {
            Download-File -Url $url -DestinationPath $GtkServerCompressed
        }
        else {
            Write-Host "[*] gtkserver URL not available for arch: $arch. Skipping download."
        }
    }
    else {
        Write-Host "[*] gtkserver component not selected. Skipping download."
    }

    # tessdata (component: tessdata)
    if (Test-ComponentSelected -Name "tessdata") {
        $url = Get-DownloadUrl -Component "tessdata" -Arch $arch
        if ($url) {
            Download-File -Url $url -DestinationPath $TessdataCompressed
        }
        else {
            Write-Host "[*] tessdata URL not available. Skipping download."
        }
    }
    else {
        Write-Host "[*] tessdata component not selected. Skipping download."
    }

    # tessdata_best (component: tessdata_best)
    if (Test-ComponentSelected -Name "tessdata_best") {
        $url = Get-DownloadUrl -Component "tessdata_best" -Arch $arch
        if ($url) {
            Download-File -Url $url -DestinationPath $TessdataBestCompressed
        }
        else {
            Write-Host "[*] tessdata_best URL not available. Skipping download."
        }
    }
    else {
        Write-Host "[*] tessdata_best component not selected. Skipping download."
    }

    # tessdata_fast (component: tessdata_fast)
    if (Test-ComponentSelected -Name "tessdata_fast") {
        $url = Get-DownloadUrl -Component "tessdata_fast" -Arch $arch
        if ($url) {
            Download-File -Url $url -DestinationPath $TessdataFastCompressed
        }
        else {
            Write-Host "[*] tessdata_fast URL not available. Skipping download."
        }
    }
    else {
        Write-Host "[*] tessdata_fast component not selected. Skipping download."
    }

    # Nmap bundle (component: nmap) – includes Npcap + Nmap installer
    if (Test-ComponentSelected -Name "nmap") {
        # Npcap
        $url = Get-DownloadUrl -Component "npcap" -Arch $arch
        if ($url) {
            Download-File -Url $url -DestinationPath $NpcapInstaller
        }
        else {
            Write-Host "[*] npcap URL not available. Skipping npcap download."
        }

        # Nmap
        $url = Get-DownloadUrl -Component "nmap" -Arch $arch
        if ($url) {
            Download-File -Url $url -DestinationPath $NmapInstaller
        }
        else {
            Write-Host "[*] nmap URL not available. Skipping nmap download."
        }
    }
    else {
        Write-Host "[*] nmap component not selected. Skipping Npcap/Nmap download."
    }
    
    # windivert (component: windivert)
    if (Test-ComponentSelected -Name "windivert") {
        $url = Get-DownloadUrl -Component "windivert" -Arch $arch
        if ($url) {
            Download-File -Url $url -DestinationPath $WinDivertCompressed
        }
        else {
            Write-Host "[*] WinDivert URL not available. Skipping download."
        }
    }
    else {
        Write-Host "[*] WinDivert component not selected. Skipping download."
    }

    # Android Platform Tools (component: android_platform_tools)
    if (Test-ComponentSelected -Name "android_platform_tools") {
        $url = Get-DownloadUrl -Component "android_platform_tools" -Arch $arch
        if ($url) {
            Download-File -Url $url -DestinationPath $AndroidPlatformToolsCompressed
        }
        else {
            Write-Host "[*] Android Platform Tools URL not available. Skipping download."
        }
    }
    else {
        Write-Host "[*] Android Platform Tools component not selected. Skipping download."
    }
}
catch {
    Write-Host "[FATAL] Download phase failed."
    if ($_ -is [System.Exception]) {
        Write-Host $_.Exception.Message
    } else {
        Write-Host $_
    }
    exit 1
}

# ================================
# EXTRACT / INSTALL PHASE
# ================================
try {
    # Python (component: python)
    if (Test-ComponentSelected -Name "python") {
        if (Test-Path $PythonCompressed) {
            Extract-CompressedFile `
                -CompressedPath $PythonCompressed `
                -DestinationDirectory (Join-Path $TargetDir "python")
        }
        else {
            Write-Host "[WARN] Python archive not found. Skipping installation."
        }
    }
    else {
        Write-Host "[*] Python component not selected. Skipping installation."
    }

    # curl (component: curl)
    if (Test-ComponentSelected -Name "curl") {
        if (Test-Path $CurlCompressed) {
            Extract-CompressedFile `
                -CompressedPath $CurlCompressed `
                -DestinationDirectory (Join-Path $TargetDir "curl")
        }
        else {
            Write-Host "[WARN] curl archive not found. Skipping installation."
        }
    }
    else {
        Write-Host "[*] curl component not selected. Skipping installation."
    }

    # YARA (component: yara)
    if (Test-ComponentSelected -Name "yara") {
        if (Test-Path $YaraCompressed) {
            Extract-CompressedFile `
                -CompressedPath $YaraCompressed `
                -DestinationDirectory (Join-Path $TargetDir "yara")
        }
        else {
            Write-Host "[WARN] YARA archive not found. Skipping installation."
        }
    }
    else {
        Write-Host "[*] YARA component not selected. Skipping installation."
    }

    # WAMR (component: wamr, TAR.GZ)
    if (Test-ComponentSelected -Name "wamr") {
        if (Test-Path $WamrArchive) {
            Extract-TarGzArchive `
                -ArchivePath $WamrArchive `
                -DestinationDirectory (Join-Path $TargetDir "wamr")
        }
        else {
            Write-Host "[WARN] WAMR archive not found. Skipping installation."
        }
    }
    else {
        Write-Host "[*] WAMR component not selected. Skipping installation."
    }

    # websocat (component: websocat)
    if (Test-ComponentSelected -Name "websocat") {
        if (Test-Path $WebsocatCompressed) {
            Extract-CompressedFile `
                -CompressedPath $WebsocatCompressed `
                -DestinationDirectory (Join-Path $TargetDir "websocat")
        }
        else {
            Write-Host "[WARN] websocat archive not found. Skipping installation."
        }
    }
    else {
        Write-Host "[*] websocat component not selected. Skipping installation."
    }

    # artifacts (component: artifacts)
    if (Test-ComponentSelected -Name "artifacts") {
        if (Test-Path $ArtifactsCompressed) {
            Extract-CompressedFile `
                -CompressedPath $ArtifactsCompressed `
                -DestinationDirectory (Join-Path $TargetDir "bin")
        }
        else {
            Write-Host "[WARN] artifacts archive not found. Skipping installation."
        }
    }
    else {
        Write-Host "[*] artifacts component not selected. Skipping installation."
    }

    # GTK3 runtime (component: gtk3runtime) – run installer and wait
    if (Test-ComponentSelected -Name "gtk3runtime") {
        if (Test-Path $GtkRuntimeInstaller) {
            Write-Host "[*] Running GTK runtime installer (wait): $GtkRuntimeInstaller"
            Start-Process -FilePath $GtkRuntimeInstaller -Wait -ErrorAction Stop
        }
        else {
            Write-Host "[WARN] GTK runtime installer not found. Skipping."
        }
    }
    else {
        Write-Host "[*] gtk3runtime component not selected. Skipping installation."
    }

    # GTK server (component: gtkserver) – extract ZIP into AppData
    if (Test-ComponentSelected -Name "gtkserver") {
        if (Test-Path $GtkServerCompressed) {
            Extract-CompressedFile `
                -CompressedPath $GtkServerCompressed `
                -DestinationDirectory (Join-Path $TargetDir "gtkserver")
        }
        else {
            Write-Host "[WARN] gtkserver archive not found. Skipping installation."
        }
    }
    else {
        Write-Host "[*] gtkserver component not selected. Skipping installation."
    }

    # tessdata (component: tessdata)
    if (Test-ComponentSelected -Name "tessdata") {
        if (Test-Path $TessdataCompressed) {
            Extract-CompressedFile `
                -CompressedPath $TessdataCompressed `
                -DestinationDirectory (Join-Path $TargetDir "tessdata")
        }
        else {
            Write-Host "[WARN] tessdata archive not found. Skipping installation."
        }
    }
    else {
        Write-Host "[*] tessdata component not selected. Skipping installation."
    }

    # tessdata_best (component: tessdata_best)
    if (Test-ComponentSelected -Name "tessdata_best") {
        if (Test-Path $TessdataBestCompressed) {
            Extract-CompressedFile `
                -CompressedPath $TessdataBestCompressed `
                -DestinationDirectory (Join-Path $TargetDir "tessdata_best")
        }
        else {
            Write-Host "[WARN] tessdata_best archive not found. Skipping installation."
        }
    }
    else {
        Write-Host "[*] tessdata_best component not selected. Skipping installation."
    }

    # tessdata_fast (component: tessdata_fast)
    if (Test-ComponentSelected -Name "tessdata_fast") {
        if (Test-Path $TessdataFastCompressed) {
            Extract-CompressedFile `
                -CompressedPath $TessdataFastCompressed `
                -DestinationDirectory (Join-Path $TargetDir "tessdata_fast")
        }
        else {
            Write-Host "[WARN] tessdata_fast archive not found. Skipping installation."
        }
    }
    else {
        Write-Host "[*] tessdata_fast component not selected. Skipping installation."
    }

    # Nmap bundle (component: nmap) – Npcap → Nmap → VC_redist.x86.exe
    if (Test-ComponentSelected -Name "nmap") {

        # Npcap
        if (Test-Path $NpcapInstaller) {
            Write-Host "[*] Running Npcap installer (wait): $NpcapInstaller"
            Start-Process -FilePath $NpcapInstaller -Wait -ErrorAction Stop
        }
        else {
            Write-Host "[WARN] Npcap installer not found. Skipping Npcap."
        }

        # Nmap
        if (Test-Path $NmapInstaller) {
            Write-Host "[*] Running Nmap installer (wait): $NmapInstaller"
            Start-Process -FilePath $NmapInstaller -Wait -ErrorAction Stop
        }
        else {
            Write-Host "[WARN] Nmap installer not found. Skipping Nmap."
        }

        # Find and run VC_redist.x86.exe inside Nmap installation directory
        $searchDirs = @()

        if (${env:ProgramFiles(x86)}) {
            $searchDirs += (Join-Path ${env:ProgramFiles(x86)} "Nmap")
        }
        if ($env:ProgramFiles) {
            $searchDirs += (Join-Path $env:ProgramFiles "Nmap")
        }

        $vcRedist = $null
        foreach ($dir in $searchDirs) {
            if (Test-Path $dir) {
                $candidate = Get-ChildItem -Path $dir -Filter "vc_redist.x86.exe" -Recurse -ErrorAction SilentlyContinue |
                             Select-Object -First 1
                if ($candidate) {
                    $vcRedist = $candidate
                    break
                }
            }
        }

        if ($vcRedist) {
            Write-Host "[*] Running VC_redist.x86 installer: $($vcRedist.FullName)"
            Start-Process -FilePath $vcRedist.FullName -Wait -ErrorAction SilentlyContinue
        }
        else {
            Write-Host "[WARN] VC_redist.x86.exe not found under expected Nmap directories."
        }
    }
    else {
        Write-Host "[*] nmap component not selected. Skipping Npcap/Nmap installation."
    }

    # windivert (component: windivert)
    if (Test-ComponentSelected -Name "windivert") {
        if (Test-Path $WinDivertCompressed) {
            Extract-CompressedFile `
                -CompressedPath $WinDivertCompressed `
                -DestinationDirectory (Join-Path $TargetDir "windivert")
        }
        else {
            Write-Host "[WARN] WinDivert archive not found. Skipping installation."
        }
    }
    else {
        Write-Host "[*] WinDivert component not selected. Skipping installation."
    }
    
    # Android Platform Tools (component: android_platform_tools)
    if (Test-ComponentSelected -Name "android_platform_tools") {
        if (Test-Path $AndroidPlatformToolsCompressed) {
            Extract-CompressedFile `
                -CompressedPath $AndroidPlatformToolsCompressed `
                -DestinationDirectory (Join-Path $TargetDir "android_platform_tools")
        }
        else {
            Write-Host "[WARN] Android Platform Tools archive not found. Skipping installation."
        }
    }
    else {
        Write-Host "[*] Android Platform Tools component not selected. Skipping installation."
    }
}
catch {
    Write-Host "[FATAL] Extraction/installation phase failed."
    if ($_ -is [System.Exception]) {
        Write-Host $_.Exception.Message
    } else {
        Write-Host $_
    }
    exit 1
}

# ================================
# FINISH
# ================================
Write-Host "[*] Installation completed successfully."
Write-Host "[*] Installed into: $TargetDir"
Write-Host ""
exit 0
