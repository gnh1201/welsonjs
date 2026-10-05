# WelsonJS post-install script
# SPDX-License-Identifier: GPL-3.0-or-later
# SPDX-FileCopyrightText: Namhyeon Go <gnh1201@catswords.re.kr>, and Catswords OSS contributors.
# Updated on: 2026-09-27
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

# ================================
# OBJECT STORAGE ENDPOINTS
# ================================
$ObjectStorageEndpoints = @(
    "https://catswords.blob.core.windows.net/welsonjs/",
    "https://kr.object.iwinv.kr/welsonjs/",
    "https://welsonjs.s3.jp-tok.cloud-object-storage.appdomain.cloud/"
)
$ObjectStorageHealthPath = ".well-known/welsonjs.txt"
$ActiveObjectStorageEndpoint = $null

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
# MCP CLIENT CONFIGURATION
# ================================
# These settings register the local stdio server only. They do not start it.
# Existing client settings are preserved; only the welsonjs-mcp entry is updated.
function Write-Utf8File {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$Content
    )

    $parent = Split-Path -Parent $Path
    if (-not (Test-Path $parent)) {
        New-Item -ItemType Directory -Path $parent -Force | Out-Null
    }

    [System.IO.File]::WriteAllText($Path, $Content, (New-Object System.Text.UTF8Encoding($false)))
}

function Set-JsonMcpServer {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [object]$Server
    )

    try {
        if (Test-Path $Path) {
            $content = [System.IO.File]::ReadAllText($Path)
            $config = if ($content.Trim()) { $content | ConvertFrom-Json -ErrorAction Stop } else { [pscustomobject]@{} }
        }
        else {
            $config = [pscustomobject]@{}
        }

        if (-not $config.PSObject.Properties['mcpServers']) {
            $config | Add-Member -MemberType NoteProperty -Name 'mcpServers' -Value ([pscustomobject]@{})
        }

        $servers = $config.mcpServers
        if ($servers.PSObject.Properties['welsonjs-mcp']) {
            $servers.'welsonjs-mcp' = $Server
        }
        else {
            $servers | Add-Member -MemberType NoteProperty -Name 'welsonjs-mcp' -Value $Server
        }

        Write-Utf8File -Path $Path -Content (($config | ConvertTo-Json -Depth 20) + [Environment]::NewLine)
        Write-Host "[*] MCP server configured: $Path"
    }
    catch {
        Write-Host "[WARN] Could not configure MCP client file ${Path}: $($_.Exception.Message)"
    }
}

function Set-TomlMcpServer {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$WorkingDirectory
    )

    try {
        $escapedWorkingDirectory = $WorkingDirectory.Replace('\', '\\').Replace('"', '\"')
        $serverBlock = @"
[mcp_servers.welsonjs-mcp]
command = "cscript.exe"
args = ["/nologo", "app.js", "mcploader", "/quiet"]
cwd = "$escapedWorkingDirectory"
"@

        $content = if (Test-Path $Path) { [System.IO.File]::ReadAllText($Path) } else { "" }
        $pattern = '(?ms)^\[mcp_servers\.welsonjs-mcp\]\r?\n.*?(?=^\[|\z)'

        if ([regex]::IsMatch($content, $pattern)) {
            $content = [regex]::Replace($content, $pattern, ($serverBlock.TrimEnd() + [Environment]::NewLine))
        }
        else {
            if ($content -and -not $content.EndsWith("`n")) {
                $content += [Environment]::NewLine
            }
            if ($content.Trim()) {
                $content += [Environment]::NewLine
            }
            $content += $serverBlock
        }

        Write-Utf8File -Path $Path -Content $content
        Write-Host "[*] MCP server configured: $Path"
    }
    catch {
        Write-Host "[WARN] Could not configure TOML MCP file ${Path}: $($_.Exception.Message)"
    }
}

function Install-McpClientConfiguration {
    $serverArgs = @('/nologo', (Join-Path $ScriptRoot 'app.js'), 'mcploader', '/quiet')

    # Codex and Grok Build use TOML mcp_servers tables.
    Set-TomlMcpServer -Path (Join-Path $env:USERPROFILE '.codex\config.toml') -WorkingDirectory $ScriptRoot
    Set-TomlMcpServer -Path (Join-Path $env:USERPROFILE '.grok\config.toml') -WorkingDirectory $ScriptRoot

    # GitHub Copilot CLI, Claude Desktop, Claude Code, Cursor Agent, and Antigravity CLI use JSON mcpServers maps.
    Set-JsonMcpServer -Path (Join-Path $env:USERPROFILE '.copilot\mcp-config.json') -Server ([pscustomobject]@{
        type = 'local'; command = 'cscript.exe'; args = $serverArgs; tools = @('*')
    })
    Set-JsonMcpServer -Path (Join-Path $env:APPDATA 'Claude\claude_desktop_config.json') -Server ([pscustomobject]@{
        command = 'cscript.exe'; args = $serverArgs
    })
    Set-JsonMcpServer -Path (Join-Path $env:USERPROFILE '.claude.json') -Server ([pscustomobject]@{
        command = 'cscript.exe'; args = $serverArgs
    })
    Set-JsonMcpServer -Path (Join-Path $env:USERPROFILE '.cursor\mcp.json') -Server ([pscustomobject]@{
        command = 'cscript.exe'; args = $serverArgs
    })
    Set-JsonMcpServer -Path (Join-Path $env:USERPROFILE '.gemini\config\mcp_config.json') -Server ([pscustomobject]@{
        command = 'cscript.exe'; args = $serverArgs
    })
}

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
function Test-ObjectStorageEndpoint {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Endpoint
    )

    $healthUrl = $Endpoint.TrimEnd("/") + "/" + $ObjectStorageHealthPath

    Write-Host "[*] Checking object storage:"
    Write-Host "    $healthUrl"

    try {
        $response = Invoke-WebRequest `
            -Uri $healthUrl `
            -Method Get `
            -UseBasicParsing `
            -TimeoutSec 10 `
            -ErrorAction Stop

        $content = $response.Content.Trim()

        if ($content -eq "true") {
            Write-Host "[+] Object storage is healthy."
            return $true
        }

        Write-Host "[WARN] Object storage returned unexpected response: '$content'"
        return $false
    }
    catch {
        Write-Host "[WARN] Object storage health check failed: $($_.Exception.Message)"
        return $false
    }
}

function Get-ActiveObjectStorageEndpoint {

    if ($ActiveObjectStorageEndpoint) {
        return $ActiveObjectStorageEndpoint
    }

    foreach ($endpoint in $ObjectStorageEndpoints) {
        if (Test-ObjectStorageEndpoint -Endpoint $endpoint) {
            $ActiveObjectStorageEndpoint = $endpoint.TrimEnd("/")

            Write-Host "[+] Selected object storage:"
            Write-Host "    $ActiveObjectStorageEndpoint"

            return $ActiveObjectStorageEndpoint
        }
    }

    Write-Host ""
    Write-Host "[ERROR] All object storage endpoints are unavailable."
    Write-Host ""

    return $null
}

function Resolve-ObjectStorageUrl {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Url
    )

    $activeEndpoint = Get-ActiveObjectStorageEndpoint

    if (-not $activeEndpoint) {
        return $Url
    }

    foreach ($endpoint in $ObjectStorageEndpoints) {
        $endpoint = $endpoint.TrimEnd("/")

        if ($Url.StartsWith($endpoint + "/", [System.StringComparison]::OrdinalIgnoreCase)) {
            $relativePath = $Url.Substring($endpoint.Length)
            return $activeEndpoint + $relativePath
        }
    }

    return $Url
}

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

    $OriginalUrl = $Url
    $Url = Resolve-ObjectStorageUrl -Url $Url

    if ($OriginalUrl -ne $Url) {
        Write-Host "[*] Object storage failover:"
        Write-Host "    $OriginalUrl"
        Write-Host "    -> $Url"
    }

    Write-Host "[*] Downloading:"
    Write-Host "    $Url"
    Write-Host "    -> $DestinationPath"

    # Fix TLS connectivity issues (Tested in Windows 8.1)
    try {
        $protocol = [Net.SecurityProtocolType]::Tls12 -bor `
                    [Net.SecurityProtocolType]::Tls11 -bor `
                    [Net.SecurityProtocolType]::Tls
        try {
            $protocol = $protocol -bor [Enum]::Parse([Net.SecurityProtocolType], 'Tls13')
        } catch {}
        [Net.ServicePointManager]::SecurityProtocol = $protocol
    }
    catch {
        Write-Host "[WARN] TLS configuration failed: $($_.Exception.Message)"
    }

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
        Write-Host "[WARN] PowerShell download failed. Falling back to curl."

        $curlPath = Join-Path $ScriptRoot "bin\x86\curl.exe"
        if (-not (Test-Path $curlPath)) {
            throw "curl not found at $curlPath"
        }

        $curlArgs = @(
            "-L"
            "--fail"
            "--retry", "3"
            "--retry-delay", "5"
            "-o", $DestinationPath
            $Url
        )

        $proc = Start-Process -FilePath $curlPath -ArgumentList $curlArgs -NoNewWindow -Wait -PassThru
        if ($proc.ExitCode -ne 0 -or -not (Test-Path $DestinationPath)) {
            throw "curl download failed with exit code $($proc.ExitCode)."
        }
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

function Extract-GZipFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$CompressedPath,

        [Parameter(Mandatory = $true)]
        [string]$DestinationPath
    )

    $inputStream = $null
    $outputStream = $null
    $gzipStream = $null

    try {
        $inputStream = [System.IO.File]::OpenRead($CompressedPath)
        $outputStream = [System.IO.File]::Create($DestinationPath)

        $gzipStream = New-Object System.IO.Compression.GZipStream(
            $inputStream,
            [System.IO.Compression.CompressionMode]::Decompress
        )

        $gzipStream.CopyTo($outputStream)

        return
    }
    catch {
        Write-Host "[!] Native GZIP extraction failed. Falling back to 7-Zip..."
    }
    finally {
        if ($gzipStream) { $gzipStream.Dispose() }
        if ($outputStream) { $outputStream.Dispose() }
        if ($inputStream) { $inputStream.Dispose() }
    }

    $destinationDirectory = Split-Path -Parent $DestinationPath

    Invoke-7zr -Arguments @(
        "e",
        "`"$CompressedPath`"",
        "-o`"$destinationDirectory`"",
        "-y"
    )

    # 7-Zip extracts foo.gz as foo
    $extractedPath = Join-Path `
        $destinationDirectory `
        ([System.IO.Path]::GetFileNameWithoutExtension($CompressedPath))

    if ($extractedPath -ne $DestinationPath) {
        Move-Item -Force $extractedPath $DestinationPath
    }
}


# ================================
# COMPRESSED / INSTALLER PATHS
# ================================
$GtkRuntimeInstaller     = Join-Path $TmpDir "gtk-runtime.exe"
$NpcapInstaller          = Join-Path $TmpDir "npcap-setup.exe"
$NmapInstaller           = Join-Path $TmpDir "nmap-setup.exe"
$HwpAutomationCompressed = Join-Path $TmpDir "FilePathCheckerModuleExample.dll.gz"

# ================================
# DOWNLOAD PHASE
# ================================
try {
    # Download archive/file components through one shared path.
    # Nmap/Npcap, GTK runtime and HWP keep dedicated installation handling.
    $downloadComponents = @(
        "python", "curl", "yara", "wamr", "websocat", "artifacts",
        "gtkserver", "tessdata", "tessdata_best", "tessdata_fast",
        "windivert", "android_platform_tools", "tun2socks", "sendboxie",
        "ldplayer", "tap_windows6", "thc_hydra", "shadowsocks_libev",
        "winlibs_mingw", "golang", "x86dbg", "w7zip", "hashcat",
        "microsoft_jdk", "nuget"
    )

    foreach ($component in $downloadComponents) {
        if (-not (Test-ComponentSelected -Name $component)) { continue }
        $url = Get-DownloadUrl -Component $component -Arch $arch
        if (-not $url) {
            Write-Host "[*] $component URL not available for arch: $arch. Skipping download."
            continue
        }
        $fileName = [System.IO.Path]::GetFileName(([uri]$url).AbsolutePath)
        if ($component -eq "curl") { $fileName = "curl.zip" }
        if (-not $fileName) { $fileName = "$component.download" }
        Download-File -Url $url -DestinationPath (Join-Path $TmpDir "$component-$fileName")
    }

    if (Test-ComponentSelected -Name "gtk3runtime") {
        $url = Get-DownloadUrl -Component "gtk3runtime" -Arch $arch
        if ($url) { Download-File -Url $url -DestinationPath $GtkRuntimeInstaller }
    }

    if (Test-ComponentSelected -Name "nmap") {
        $npcapUrl = Get-DownloadUrl -Component "npcap" -Arch $arch
        if ($npcapUrl) { Download-File -Url $npcapUrl -DestinationPath $NpcapInstaller }
        $nmapUrl = Get-DownloadUrl -Component "nmap" -Arch $arch
        if ($nmapUrl) { Download-File -Url $nmapUrl -DestinationPath $NmapInstaller }
    }

    if (Test-ComponentSelected -Name "hwp_automation") {
        $url = Get-DownloadUrl -Component "hwp_automation" -Arch $arch
        if ($url) { Download-File -Url $url -DestinationPath $HwpAutomationCompressed }
    }

    if (Test-ComponentSelected -Name "tls13") {
        Write-Host "[*] No additional download is required for TLS 1.3 support."
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
    # Extract or stage downloadable components using the shared component list.
    foreach ($component in $downloadComponents) {
        if (-not (Test-ComponentSelected -Name $component)) { continue }
        $url = Get-DownloadUrl -Component $component -Arch $arch
        if (-not $url) { continue }
        $fileName = [System.IO.Path]::GetFileName(([uri]$url).AbsolutePath)
        if (-not $fileName) { $fileName = "$component.download" }
        $archivePath = Join-Path $TmpDir "$component-$fileName"
        if (-not (Test-Path $archivePath)) {
            Write-Host "[WARN] $component download not found. Skipping installation."
            continue
        }

        $destinationName = if ($component -eq "artifacts") { "bin" } else { $component }
        $destination = Join-Path $TargetDir $destinationName
        $lowerPath = $archivePath.ToLowerInvariant()
        if ($lowerPath.EndsWith(".tar.gz")) {
            Extract-TarGzArchive -ArchivePath $archivePath -DestinationDirectory $destination
        }
        elseif ([System.IO.Path]::GetExtension($archivePath).ToLowerInvariant() -in @(".zip", ".7z")) {
            Extract-CompressedFile -CompressedPath $archivePath -DestinationDirectory $destination
        }
        else {
            New-Item -ItemType Directory -Path $destination -Force | Out-Null
            Move-Item -Path $archivePath -Destination (Join-Path $destination $fileName) -Force
        }
    }

    # Make NuGet available from new shells for the current user.
    if (Test-ComponentSelected -Name "nuget") {
        $nugetDirectory = Join-Path $TargetDir "nuget"
        $nugetExecutable = Join-Path $nugetDirectory "nuget.exe"
        if (Test-Path $nugetExecutable) {
            $userPath = [Environment]::GetEnvironmentVariable("Path", "User")
            $pathEntries = @($userPath -split ";" | Where-Object { $_ -and $_.Trim() -ne "" })
            $normalizedNugetDirectory = [System.IO.Path]::GetFullPath($nugetDirectory).TrimEnd("\")
            $alreadyRegistered = $false
            foreach ($pathEntry in $pathEntries) {
                try {
                    if ([System.IO.Path]::GetFullPath($pathEntry).TrimEnd("\") -ieq $normalizedNugetDirectory) {
                        $alreadyRegistered = $true
                        break
                    }
                }
                catch { }
            }

            if (-not $alreadyRegistered) {
                $pathEntries += $normalizedNugetDirectory
                $newUserPath = $pathEntries -join ";"
                [Environment]::SetEnvironmentVariable("Path", $newUserPath, "User")
                $env:Path = "$newUserPath;$env:Path"
                Write-Host "[*] Added NuGet to the current user's PATH: $normalizedNugetDirectory"
            }
            else {
                Write-Host "[*] NuGet is already present in the current user's PATH."
            }
        }
        else {
            Write-Host "[WARN] nuget.exe not found. Skipping PATH registration."
        }
    }

    # Install GTK3 runtime (component: gtk3runtime) – run installer and wait
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

    # Install Nmap bundle (component: nmap) – Npcap → Nmap → VC_redist.x86.exe
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

    # Install HWP Automation (component: hwp_automation)
    if (Test-ComponentSelected -Name "hwp_automation") {
        if (Test-Path $HwpAutomationCompressed) {
            $hwpAutomationDirectory = Join-Path $TargetDir "hwp_automation"
            $modulePath = Join-Path `
                $hwpAutomationDirectory `
                "FilePathCheckerModuleExample.dll"

            # Create destination directory
            if (-not (Test-Path $hwpAutomationDirectory)) {
                New-Item -Path $hwpAutomationDirectory -ItemType Directory -Force | Out-Null
            }

            # Extract HWP Automation module
            Extract-GZipFile `
                -CompressedPath $HwpAutomationCompressed `
                -DestinationPath $modulePath

            # Register HWP Automation module
            $registryPath = "HKCU:\Software\HNC\HwpAutomation\Modules"

            if (Test-Path $modulePath) {
                if (-not (Test-Path $registryPath)) {
                    New-Item -Path $registryPath -Force | Out-Null
                }

                Set-ItemProperty `
                    -Path $registryPath `
                    -Name "FilePathCheckerModuleExample" `
                    -Value ([System.IO.Path]::GetFullPath($modulePath)) `
                    -Type String

                Write-Host "[*] HWP Automation module registered:"
                Write-Host "    $modulePath"
            }
            else {
                throw "HWP Automation module not found after extraction: $modulePath"
            }
        }
        else {
            Write-Host "[WARN] HWP Automation archive not found. Skipping installation."
        }
    }
    else {
        Write-Host "[*] HWP Automation component not selected. Skipping installation."
    }
    
    # Enable TLS 1.3 support (component: tls13)
    # TLS 1.2 is also enabled for compatibility.
    # https://learn.microsoft.com/ko-kr/sql/relational-databases/security/networking/connect-with-tls-1-3?view=sql-server-ver17
    # https://learn.microsoft.com/en-us/windows-server/security/tls/tls-registry-settings?tabs=diffie-hellman
    if (Test-ComponentSelected -Name "tls13") {
        try {
            # Use .NET API for compatibility with older PowerShell versions.
            $version = [Environment]::OSVersion.Version

            $base = 'HKLM:\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols'

            function Enable-TlsClient {
                param (
                    [Parameter(Mandatory = $true)]
                    [string]$Protocol
                )

                $path = Join-Path $base "$Protocol\Client"

                if (-not (Test-Path $path)) {
                    New-Item $path -Force | Out-Null
                }

                $property = Get-ItemProperty -Path $path -ErrorAction SilentlyContinue

                if ($property -and
                    $property.Enabled -eq 1 -and
                    $property.DisabledByDefault -eq 0) {
                    Write-Host "[*] $Protocol Client is already enabled."
                    return $true
                }

                New-ItemProperty -Path $path `
                                 -Name 'Enabled' `
                                 -Value 1 `
                                 -PropertyType 'DWord' `
                                 -Force | Out-Null

                New-ItemProperty -Path $path `
                                 -Name 'DisabledByDefault' `
                                 -Value 0 `
                                 -PropertyType 'DWord' `
                                 -Force | Out-Null

                Write-Host "[*] $Protocol Client support enabled."
                return $true
            }

            # TLS 1.2: Windows 7 and later
            $tls12Enabled = $false

            if ($version -ge [System.Version]'6.1') {
                $tls12Enabled = Enable-TlsClient 'TLS 1.2'
            }

            # TLS 1.3: Windows 10 and later
            $tls13Enabled = $false

            if ($version -ge [System.Version]'10.0') {
                $tls13Enabled = Enable-TlsClient 'TLS 1.3'
            }

            if (-not $tls12Enabled) {
                Write-Host "[!] TLS 1.2 Client is not available on this version of Windows."
            }

            if (-not $tls13Enabled) {
                Write-Host "[!] TLS 1.3 Client is not available on this version of Windows."
            }
        }
        catch {
            Write-Host "[!] Failed to configure TLS support: $($_.Exception.Message)"
        }
    }

    # Register the local MCP server only when the MCP component was selected.
    if (Test-ComponentSelected -Name "mcp") {
        Install-McpClientConfiguration
    }
    else {
        Write-Host "[*] MCP component not selected. Skipping client configuration."
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
