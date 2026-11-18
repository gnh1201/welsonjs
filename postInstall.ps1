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

# ================================
# LOAD DOWNLOAD HASHES (DownloadHashes.txt in /data folder)
# ================================
# Every file this script downloads must hash to one of the SHA-256 values listed
# in this file. The file holds nothing else: one lowercase hex hash per line, no
# component name and no URL, so allowing a download means adding its hash.
$DownloadHashesPath = Join-Path $ScriptRoot "data/DownloadHashes.txt"
$script:DownloadHashes = $null

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

# The name a component's download is saved under. It comes from the URL, and both
# the download and the extraction phase ask here, so an archive is unpacked from the
# name it was saved as. Every entry of data\DownloadUrls.psd1 ends in a file name
# now that curl, the Android platform-tools and NuGet are pinned to versioned files;
# one that has none falls back to <component>.download.
function Get-DownloadFileName {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Component,
        [Parameter(Mandatory = $true)]
        [string]$Url
    )

    $fileName = [System.IO.Path]::GetFileName(([uri]$Url).AbsolutePath)
    if (-not $fileName) {
        $fileName = "$Component.download"
    }

    return $fileName
}

function Get-DownloadHashes {
    # The SHA-256 values a downloaded file is allowed to have, read from
    # data\DownloadHashes.txt: one hash per line, blank lines and surrounding
    # spaces ignored. The set is case-insensitive, because a hash written in upper
    # case is still the same hash. The file is read once per run.
    if ($null -ne $script:DownloadHashes) {
        return $script:DownloadHashes
    }

    $hashes = New-Object -TypeName 'System.Collections.Generic.HashSet[string]' -ArgumentList ([System.StringComparer]::OrdinalIgnoreCase)

    if (-not (Test-Path -LiteralPath $DownloadHashesPath -PathType Leaf)) {
        Write-Host "[WARN] data\DownloadHashes.txt not found at: $DownloadHashesPath"
        Write-Host "[WARN] No download can be verified in this run."
        $script:DownloadHashes = $hashes
        return $hashes
    }

    foreach ($line in @(Get-Content -LiteralPath $DownloadHashesPath -ErrorAction SilentlyContinue)) {
        $hash = ""
        if ($null -ne $line) { $hash = ([string]$line).Trim() }
        if ($hash -eq "") { continue }
        [void]$hashes.Add($hash)
    }

    if ($hashes.Count -eq 0) {
        Write-Host "[WARN] data\DownloadHashes.txt lists no hash. No download can be verified in this run."
    }

    $script:DownloadHashes = $hashes
    return $hashes
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

# Setup passes the components the user ticked, and "none" when that selection is
# empty: WizardSelectedComponents returns an empty string, which would otherwise
# read as "no filter" and select every component. A run started by hand without
# -Components keeps meaning every component.
$componentList = if ($Components) { $Components.Trim() } else { "" }

if ($componentList -eq "none") {
    $AllComponentsSelected = $false
}
elseif ($componentList -ne "") {
    $SelectedComponents =
        $componentList.Split(",") |
        ForEach-Object { $_.Trim().ToLowerInvariant() }

    $AllComponentsSelected = $false
}

# All GGUF models in this installer are served by llama.cpp. Accept both
# short names and nested Inno Setup component paths for every model.
$llamaModelComponents = @(
    "hy_mt2_1_8b_1_25bit",
    "frognano_4b_2609",
    "bitnet_b1_58_2b_4t",
    "qwen3guard_06b"
)

if (-not $AllComponentsSelected) {
    $llamaModelSelected = $false
    foreach ($modelComponent in $llamaModelComponents) {
        $nestedComponent = "llamacpp\$modelComponent"
        if ($SelectedComponents -contains $modelComponent -or
            $SelectedComponents -contains $nestedComponent) {
            $llamaModelSelected = $true
            if ($SelectedComponents -contains $nestedComponent -and
                $SelectedComponents -notcontains $modelComponent) {
                $SelectedComponents += $modelComponent
            }
        }
    }

    if ($llamaModelSelected -and $SelectedComponents -notcontains "llamacpp") {
        $SelectedComponents += "llamacpp"
        Write-Host "[*] LLM model selected: llamacpp dependency enabled."
    }

    # llama.cpp is the WelsonJS build in the artifacts package, not a download.
    if ($SelectedComponents -contains "llamacpp" -and $SelectedComponents -notcontains "artifacts") {
        $SelectedComponents += "artifacts"
        Write-Host "[*] llamacpp selected: artifacts dependency enabled (the llama.cpp build is in the artifacts package)."
    }
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
# APPLICATION ASSETS
# ================================
# app.js needs the application assets under app\assets (currently the vendor
# JavaScript files under app\assets\js) to start. The installer normally ships
# them, so this verifies them and downloads only the files that are missing or
# do not match app\assets\integrity.json.
# Runs regardless of the component selection.
$restoreAppAssets = Join-Path $ScriptRoot "restoreAppAssets.ps1"
if (Test-Path -LiteralPath $restoreAppAssets -PathType Leaf) {
    Write-Host "[*] Restoring application assets..."
    try {
        & $restoreAppAssets -Root $ScriptRoot
        if ($LASTEXITCODE -ne 0) {
            throw "restoreAppAssets.ps1 failed with exit code $LASTEXITCODE"
        }
    }
    catch {
        Write-Host "[WARN] Failed to restore the application assets: $($_.Exception.Message)"
        Write-Host "[WARN] app.js cannot start until they are restored. Run: powershell -ExecutionPolicy Bypass -NoProfile -File `"$restoreAppAssets`""
    }
}
else {
    Write-Host "[WARN] restoreAppAssets.ps1 not found at: $restoreAppAssets"
}
Write-Host ""

# ================================
# URI SCHEME REGISTRATION
# ================================
# Runs before the downloads so a network failure cannot skip it. Requires
# administrator privileges because it writes to HKEY_CLASSES_ROOT.
function Register-UriScheme {
    $root = [Microsoft.Win32.Registry]::ClassesRoot
    $key = $root.CreateSubKey($AppName)
    try {
        $key.SetValue("", "URL:$AppName")
        $key.SetValue("URL Protocol", "")
    }
    finally {
        $key.Dispose()
    }

    $iconKey = $root.CreateSubKey("$AppName\DefaultIcon")
    try {
        $iconKey.SetValue("", "$(Join-Path $ScriptRoot 'app\favicon.ico'),0")
    }
    finally {
        $iconKey.Dispose()
    }

    # Start cscript directly: the URI comes from web pages and must not pass through cmd.exe.
    $commandKey = $root.CreateSubKey("$AppName\shell\open\command")
    try {
        $commandKey.SetValue("", "`"$env:SystemRoot\System32\cscript.exe`" `"$(Join-Path $ScriptRoot 'app.js')`" uriloader `"%1`"")
    }
    finally {
        $commandKey.Dispose()
    }
}

# Removes the scheme only when it points at this installation.
function Unregister-UriScheme {
    $root = [Microsoft.Win32.Registry]::ClassesRoot
    $commandKey = $root.OpenSubKey("$AppName\shell\open\command")
    if (-not $commandKey) { return }
    try {
        $command = [string]$commandKey.GetValue("")
    }
    finally {
        $commandKey.Dispose()
    }

    $appScript = Join-Path $ScriptRoot "app.js"
    if ($command.IndexOf($appScript, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        $root.DeleteSubKeyTree($AppName)
        Write-Host "[*] Removed the ${AppName}:// URI scheme."
    }
}

try {
    if (Test-ComponentSelected -Name "urischeme") {
        Write-Host "[*] Registering the ${AppName}:// URI scheme..."
        Register-UriScheme
    }
    else {
        Write-Host "[*] URI scheme component not selected."
        Unregister-UriScheme
    }
}
catch {
    Write-Host "[WARN] Failed to update the URI scheme: $($_.Exception.Message)"
}

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
        # Fallback when the processor query is unavailable. The environment names
        # the native architecture, which a plain 64-bit check cannot: it would
        # answer x64 on an ARM64 machine and download the emulated build.
        $processorArchitecture = if ($env:PROCESSOR_ARCHITEW6432) {
            $env:PROCESSOR_ARCHITEW6432
        }
        else {
            $env:PROCESSOR_ARCHITECTURE
        }

        switch ($processorArchitecture) {
            "ARM64" { $arch = "arm64" }
            "AMD64" { $arch = "x64"   }
            "x86"   { $arch = "x86"   }
            default {
                if ([System.Environment]::Is64BitOperatingSystem) {
                    $arch = "x64"
                }
                else {
                    $arch = "x86"
                }
            }
        }
    }

    return $arch
}

# ================================
# PRIVILEGE LEVEL
# ================================
# The installer runs elevated (PrivilegesRequired=admin in setup.iss), but this
# script can also be started by hand, so a component that needs a machine-wide
# installation asks first.
function Test-Administrator {
    try {
        $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
        $principal = New-Object Security.Principal.WindowsPrincipal($identity)
        return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    }
    catch {
        return $false
    }
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

function Get-Sha256Hash {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if (Get-Command Get-FileHash -ErrorAction SilentlyContinue) {
        return (Get-FileHash -LiteralPath $Path -Algorithm SHA256 -ErrorAction Stop).Hash.ToLowerInvariant()
    }

    # Get-FileHash arrived with PowerShell 4.0 (Windows 8.1); the .NET class is
    # there on every version this script runs on.
    $stream = [System.IO.File]::OpenRead($Path)
    try {
        $sha256 = [System.Security.Cryptography.SHA256]::Create()
        try {
            $bytes = $sha256.ComputeHash($stream)
        }
        finally {
            $sha256.Dispose()
        }
    }
    finally {
        $stream.Dispose()
    }

    $hex = New-Object System.Text.StringBuilder
    foreach ($byte in $bytes) {
        [void]$hex.Append($byte.ToString("x2"))
    }

    return $hex.ToString()
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
            # A connection that stays open but stops delivering would otherwise
            # hold the whole installation here.
            Invoke-WebRequest -Uri $Url -OutFile $DestinationPath -UseBasicParsing -TimeoutSec 900
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
            "--connect-timeout", "30"
            "-o", $DestinationPath
            $Url
        )

        $curlProcess = Start-Process -FilePath $curlPath -ArgumentList $curlArgs -NoNewWindow -Wait -PassThru
        if ($curlProcess.ExitCode -ne 0 -or -not (Test-Path $DestinationPath)) {
            throw "curl download failed with exit code $($curlProcess.ExitCode)."
        }
    }

    # Every download must be a file whose SHA-256 is listed in
    # data\DownloadHashes.txt. One that is not is deleted here, before a component
    # can run or unpack it, and the error names the hash it did have so the file
    # can be identified and the list updated deliberately.
    $allowedHashes = Get-DownloadHashes
    if ($allowedHashes.Count -gt 0) {
        try {
            $actual = Get-Sha256Hash -Path $DestinationPath
        }
        catch {
            Remove-Item -LiteralPath $DestinationPath -Force -ErrorAction SilentlyContinue
            throw "Could not hash the downloaded file: $($_.Exception.Message)"
        }

        if (-not $allowedHashes.Contains($actual)) {
            Remove-Item -LiteralPath $DestinationPath -Force -ErrorAction SilentlyContinue
            Write-Host "[ERROR] The downloaded file is not listed in data\DownloadHashes.txt, so it was deleted:"
            Write-Host "        $Url"
            Write-Host "        SHA-256: $actual"
            throw "SHA-256 $actual, for $DestinationPath, is not listed in data\DownloadHashes.txt."
        }

        Write-Host "[+] SHA-256 verified: $actual"
    }
    else {
        Write-Host "[WARN] data\DownloadHashes.txt lists no hash, so this download is not verified."
    }
}

function Invoke-7zr {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    $sevenZip = Join-Path $ScriptRoot "bin\x86\7zr.exe"
    if (-not (Test-Path $sevenZip)) {
        throw "7zr.exe is missing: $sevenZip"
    }

    Write-Host "[INFO] Using 7zr.exe:"
    Write-Host "       $sevenZip"
    Write-Host "[DEBUG] 7zr args:"
    Write-Host "        $($Arguments -join ' ')"

    & $sevenZip @Arguments

    if ($LASTEXITCODE -ne 0) {
        throw "7zr exited with code $LASTEXITCODE."
    }
}

function Extract-CompressedFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$CompressedPath,

        [Parameter(Mandatory = $true)]
        [string]$DestinationDirectory,

        # A vendor package whose own top folder carries meaning (a JDK names the
        # folder after its build) is unpacked as it is; the default unwraps a
        # single root folder, because most archives here only use one as a wrapper.
        [switch]$KeepArchiveRoot
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

    # Unwrap repeated single-folder roots (some archives include redundant
    # directory levels, e.g. package/package/rg.exe).
    $entries    = Get-ChildItem -Path $tmpExtractDir -Force
    $SourceRoot = $tmpExtractDir

    while (-not $KeepArchiveRoot -and $entries.Count -eq 1 -and $entries[0].PSIsContainer) {
        $SourceRoot = $entries[0].FullName
        Write-Host "[*] Detected single root folder inside archive: $($entries[0].Name)"
        Write-Host "[*] Unwrapping folder content..."
        $entries = Get-ChildItem -Path $SourceRoot -Force
    }
    if ($KeepArchiveRoot) {
        Write-Host "[*] Keeping the archive's own top folder."
    }
    elseif ($SourceRoot -eq $tmpExtractDir) {
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

# Installs the artifacts package (artifacts.zip). It holds the WelsonJS binaries
# and the llama.cpp build (llamacpp\<arch>, native\llamacpp\build-llamacpp.ps1),
# which CI puts there so that it is signed with the rest. The package is
# extracted to a staging folder; llamacpp\<arch> is copied to llamacpp\ (the
# only place the launcher runs llama-server.exe from) when the llamacpp
# component is selected, and everything else goes to bin\ as before.
function Install-ArtifactsPackage {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ArchivePath
    )

    $staging = Join-Path $TmpDir "artifacts-staging"
    if (Test-Path $staging) { Remove-Item -Path $staging -Recurse -Force }
    Extract-CompressedFile -CompressedPath $ArchivePath -DestinationDirectory $staging

    $llamaBuildRoot = Join-Path $staging "llamacpp"
    if (Test-ComponentSelected -Name "llamacpp") {
        $llamaArchCandidates = @($arch)
        if ($arch -eq "arm64") { $llamaArchCandidates += "x64" }  # x64 emulation on Windows 11 on Arm
        $llamaSource = $null
        foreach ($llamaArch in $llamaArchCandidates) {
            $candidate = Join-Path $llamaBuildRoot $llamaArch
            if (Test-Path (Join-Path $candidate "llama-server.exe")) { $llamaSource = $candidate; break }
        }

        if ($llamaSource) {
            $llamaDestination = Join-Path $TargetDir "llamacpp"
            Ensure-EmptyDirectory -Path $llamaDestination
            # Remove files of a previous installation (for example, the official package).
            Get-ChildItem -Path $llamaDestination -Force | Remove-Item -Recurse -Force
            Copy-Item -Path (Join-Path $llamaSource "*") -Destination $llamaDestination -Recurse -Force
            Write-Host "[*] Installed llama.cpp from the artifacts package: llamacpp\$(Split-Path $llamaSource -Leaf) -> $llamaDestination"
        }
        else {
            Write-Host "[WARN] The artifacts package has no llama.cpp build for $arch (llamacpp\$arch). Skipping llama.cpp."
        }
    }
    if (Test-Path $llamaBuildRoot) { Remove-Item -Path $llamaBuildRoot -Recurse -Force }

    # The rest of the package goes to bin\, replacing items of the same name.
    $binDestination = Join-Path $TargetDir "bin"
    Ensure-EmptyDirectory -Path $binDestination
    Get-ChildItem -Path $staging -Force | ForEach-Object {
        $targetPath = Join-Path $binDestination $_.Name
        if (Test-Path $targetPath) { Remove-Item -Path $targetPath -Recurse -Force }
        Move-Item -Path $_.FullName -Destination $targetPath
    }
    Remove-Item -Path $staging -Recurse -Force
}

function Expand-TarFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$TarPath,

        [Parameter(Mandatory = $true)]
        [string]$DestinationDirectory
    )

    function Read-TarBytes([System.IO.Stream]$Stream, [long]$Size) {
        $buffer = New-Object byte[] $Size
        $offset = 0
        while ($offset -lt $Size) {
            $count = $Stream.Read($buffer, $offset, $Size - $offset)
            if ($count -le 0) { throw "Unexpected end of TAR archive: $TarPath" }
            $offset += $count
        }
        return ,$buffer
    }

    function Get-TarText([byte[]]$Bytes, [int]$Offset, [int]$Length) {
        $text = [System.Text.Encoding]::UTF8.GetString($Bytes, $Offset, $Length)
        $end = $text.IndexOf([char]0)
        if ($end -ge 0) { return $text.Substring(0, $end) }
        return $text
    }

    $root = [System.IO.Path]::GetFullPath($DestinationDirectory).TrimEnd('\') + '\'
    $stream = [System.IO.File]::OpenRead($TarPath)
    try {
        $overrideName = $null
        while ($stream.Position -lt $stream.Length) {
            $header = Read-TarBytes $stream 512
            if ([Array]::TrueForAll($header, [Predicate[byte]]{ param($b) $b -eq 0 })) { break }

            $size = [Convert]::ToInt64((Get-TarText $header 124 12).Trim(), 8)
            $type = [char]$header[156]
            $name = Get-TarText $header 0 100
            if ((Get-TarText $header 257 5) -eq "ustar") {
                $prefix = Get-TarText $header 345 155
                if ($prefix) { $name = "$prefix/$name" }
            }
            if ($overrideName) {
                $name = $overrideName
                $overrideName = $null
            }

            $content = $null
            if (@('L', 'x', '0', [char]0) -contains $type) {
                $content = Read-TarBytes $stream $size
            }
            else {
                [void]$stream.Seek($size, [System.IO.SeekOrigin]::Current)
            }
            [void]$stream.Seek((512 - ($size % 512)) % 512, [System.IO.SeekOrigin]::Current)

            if ($type -ceq 'L') {
                $overrideName = Get-TarText $content 0 $content.Length
                continue
            }
            if ($type -ceq 'x') {
                $pax = [System.Text.Encoding]::UTF8.GetString($content)
                if ($pax -match '(?m)^\d+ path=(.+)$') { $overrideName = $Matches[1] }
                continue
            }
            if (@('0', '5', [char]0) -notcontains $type) {
                Write-Host "[WARN] Skipping unsupported TAR entry type '$type': $name"
                continue
            }

            $target = [System.IO.Path]::GetFullPath((Join-Path $root ($name -replace '/', '\')))
            if (-not ($target + '\').StartsWith($root, [System.StringComparison]::OrdinalIgnoreCase)) {
                throw "TAR entry escapes the destination directory: $name"
            }
            if ($type -eq '5') {
                [void][System.IO.Directory]::CreateDirectory($target)
                continue
            }
            [void][System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($target))
            [System.IO.File]::WriteAllBytes($target, $content)
        }
    }
    finally {
        $stream.Dispose()
    }
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

    # 7zr.exe only supports the 7z format, so expand the archive with .NET instead.
    Write-Host "[WARN] 'tar' not found. Falling back to the built-in TAR reader."

    $tarPath = Join-Path $DestinationDirectory "_tmp_archive.tar"
    try {
        Extract-GZipFile -CompressedPath $ArchivePath -DestinationPath $tarPath
        Expand-TarFile -TarPath $tarPath -DestinationDirectory $DestinationDirectory
    }
    finally {
        if (Test-Path $tarPath) { Remove-Item -Path $tarPath -Force }
    }
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
    }
    catch {
        throw "GZIP extraction failed for ${CompressedPath}: $($_.Exception.Message)"
    }
    finally {
        if ($gzipStream) { $gzipStream.Dispose() }
        if ($outputStream) { $outputStream.Dispose() }
        if ($inputStream) { $inputStream.Dispose() }
    }
}


function Add-UserPathEntry {
    # Adds a directory to the current user's PATH when it is not there yet, and
    # says whether it added it. Entries are compared as resolved paths, so a
    # trailing backslash or a change of case does not add the same directory twice.
    param(
        [Parameter(Mandatory = $true)]
        [string]$Directory
    )

    $userPath   = [Environment]::GetEnvironmentVariable("Path", "User")
    $pathEntries = @($userPath -split ";" | Where-Object { $_ -and $_.Trim() -ne "" })
    $normalizedDirectory = [System.IO.Path]::GetFullPath($Directory).TrimEnd("\")

    foreach ($pathEntry in $pathEntries) {
        try {
            if ([System.IO.Path]::GetFullPath($pathEntry).TrimEnd("\") -ieq $normalizedDirectory) {
                return $false
            }
        }
        catch { }
    }

    $pathEntries += $normalizedDirectory
    $newUserPath  = $pathEntries -join ";"
    [Environment]::SetEnvironmentVariable("Path", $newUserPath, "User")
    $env:Path = "$newUserPath;$env:Path"
    return $true
}

function Remove-UserPathEntriesUnder {
    # Removes the current user's PATH entries that point into a directory which is
    # being deleted, and answers how many it removed. Entries are compared as
    # resolved paths, so a trailing backslash or a change of case still counts as
    # inside the directory. The copy of PATH in this process is left as it is: an
    # entry into a directory that no longer exists does nothing, and the value saved
    # for later shells is what has to be right.
    param(
        [Parameter(Mandatory = $true)]
        [string]$Directory
    )

    $userPath = [Environment]::GetEnvironmentVariable("Path", "User")
    if (-not $userPath) { return 0 }

    $removed       = 0
    $keptEntries   = @()
    $rootDirectory = [System.IO.Path]::GetFullPath($Directory).TrimEnd("\") + "\"

    foreach ($pathEntry in ($userPath -split ";")) {
        $trimmedEntry = $pathEntry.Trim()
        if ($trimmedEntry -eq "") { continue }

        $resolvedEntry = $trimmedEntry
        try { $resolvedEntry = [System.IO.Path]::GetFullPath($trimmedEntry) }
        catch { }

        if ($resolvedEntry.StartsWith($rootDirectory, [System.StringComparison]::OrdinalIgnoreCase)) {
            $removed++
            continue
        }

        $keptEntries += $trimmedEntry
    }

    if ($removed -gt 0) {
        [Environment]::SetEnvironmentVariable("Path", ($keptEntries -join ";"), "User")
    }

    return $removed
}

function Find-JdkHome {
    # The JDK root is the directory whose bin\java.exe is the launcher. A vendor
    # archive puts it in one folder named after the build, but the depth is not
    # guaranteed, so the search goes down a few levels and answers the shallowest
    # directory that has bin\java.exe. $null means the archive holds no JDK.
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Container)) {
        return $null
    }

    $level = @((Get-Item -LiteralPath $Path).FullName)

    for ($depth = 0; $depth -lt 3; $depth++) {
        $nextLevel = @()

        foreach ($directory in $level) {
            if (Test-Path -LiteralPath (Join-Path $directory "bin\java.exe") -PathType Leaf) {
                return $directory
            }

            $nextLevel += @(Get-ChildItem -LiteralPath $directory -Force -ErrorAction SilentlyContinue |
                Where-Object { $_.PSIsContainer } |
                ForEach-Object { $_.FullName })
        }

        if ($nextLevel.Count -eq 0) { break }
        $level = $nextLevel
    }

    return $null
}


# ================================
# COMPRESSED / INSTALLER PATHS
# ================================
$GtkRuntimeInstaller     = Join-Path $TmpDir "gtk-runtime.exe"
$NpcapInstaller          = Join-Path $TmpDir "npcap-setup.exe"
$NmapInstaller           = Join-Path $TmpDir "nmap-setup.exe"
$JdkArchive             = $null  # set in the download phase: the .zip named in the URL table
$HwpAutomationCompressed = Join-Path $TmpDir "FilePathCheckerModuleExample.dll.gz"

# ================================
# DOWNLOAD PHASE
# ================================
try {
    # Download archive/file components through one shared path.
    # Nmap/Npcap, GTK runtime, OpenJDK (jdk) and HWP keep dedicated
    # installation handling.
    $downloadComponents = @(
        "python", "curl", "hy_mt2_1_8b_1_25bit", "frognano_4b_2609", "bitnet_b1_58_2b_4t", "qwen3guard_06b", "yara", "wamr", "websocat", "artifacts",
        "gtkserver", "tessdata", "tessdata_best", "tessdata_fast",
        "windivert", "android_platform_tools", "tun2socks", "sendboxie",
        "ldplayer", "tap_windows6", "thc_hydra", "shadowsocks_libev",
        "winlibs_mingw", "golang", "x86dbg", "w7zip", "hashcat",
        "nuget", "ripgrep"
    )

    foreach ($component in $downloadComponents) {
        if (-not (Test-ComponentSelected -Name $component)) { continue }
        $url = Get-DownloadUrl -Component $component -Arch $arch
        if (-not $url) {
            Write-Host "[*] $component URL not available for arch: $arch. Skipping download."
            continue
        }
        $fileName = Get-DownloadFileName -Component $component -Url $url
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

    # OpenJDK (component: jdk) – the vendor's .zip archive is downloaded
    # here and unpacked in the installation phase. All three architectures are a
    # .zip: the Microsoft Build of OpenJDK (x64, ARM64) and Azul Zulu 17 (32-bit
    # Windows), so the file name comes from the URL.
    if (Test-ComponentSelected -Name "jdk") {
        $url = Get-DownloadUrl -Component "jdk" -Arch $arch
        if ($url) {
            $jdkFileName = [System.IO.Path]::GetFileName(([uri]$url).AbsolutePath)
            if (-not $jdkFileName) { $jdkFileName = "openjdk.zip" }

            # A directory of its own for this run: the archive is unpacked into the
            # user's AppData while the script may be elevated, so it must not sit at
            # a path that an earlier run, or a second run at the same time, also writes.
            $jdkDownloadDirectory = Join-Path $TmpDir "jdk-$([guid]::NewGuid().ToString('N'))"
            New-Item -ItemType Directory -Path $jdkDownloadDirectory -Force | Out-Null
            $JdkArchive = Join-Path $jdkDownloadDirectory $jdkFileName

            try {
                Download-File -Url $url -DestinationPath $JdkArchive
            }
            catch {
                # An optional component that cannot be fetched must not stop the rest.
                Write-Host "[WARN] Failed to download the OpenJDK archive: $($_.Exception.Message)"
                Write-Host "[WARN] The OpenJDK component is skipped for this run."
                Remove-Item -LiteralPath $jdkDownloadDirectory -Recurse -Force -ErrorAction SilentlyContinue
                $JdkArchive = $null
            }
        }
        else {
            Write-Host "[*] jdk has no archive in the URL table for arch: $arch. Skipping download."
            Write-Host "    Microsoft publishes Windows builds of JDK 25 for x64 and ARM64; the 32-bit Windows build in the table is Azul Zulu 17 for x86."
        }
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
        $fileName = Get-DownloadFileName -Component $component -Url $url
        $archivePath = Join-Path $TmpDir "$component-$fileName"
        if (-not (Test-Path $archivePath)) {
            Write-Host "[WARN] $component download not found. Skipping installation."
            continue
        }

        if ($component -eq "artifacts") {
            Install-ArtifactsPackage -ArchivePath $archivePath
            continue
        }

        $destination = Join-Path $TargetDir $component
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

    # Make command-line tools available from new shells for the current user.
    foreach ($tool in @(
        @{ Name = "nuget"; Executable = "nuget.exe" },
        @{ Name = "ripgrep"; Executable = "rg.exe" },
        @{ Name = "llamacpp"; Executable = "llama-server.exe" },
        @{ Name = "llamacpp"; Executable = "llama-cli.exe" }
    )) {
        if (Test-ComponentSelected -Name $tool.Name) {
            $toolDirectory = Join-Path $TargetDir $tool.Name
            $toolExecutable = Join-Path $toolDirectory $tool.Executable
            if (Test-Path $toolExecutable) {
                if (Add-UserPathEntry -Directory $toolDirectory) {
                    Write-Host "[*] Added $($tool.Name) to the current user's PATH: $toolDirectory"
                }
                else {
                    Write-Host "[*] $($tool.Name) is already present in the current user's PATH."
                }
            }
            else {
                Write-Host "[WARN] $($tool.Executable) not found. Skipping PATH registration."
            }
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

    # Unpack an OpenJDK build (component: jdk)
    # The component unpacks a vendor .zip into the WelsonJS AppData directory and
    # registers it itself:
    #   %APPDATA%\welsonjs\jdk\<the archive's own top folder>\
    # The folder carries the component's own name, `jdk`, and the build's own
    # version folder inside it is what keeps two builds apart. A folder that this
    # component wrote under its earlier name, %APPDATA%\welsonjs\microsoft_jdk,
    # is removed once the new JDK is registered.
    # The archive holds Microsoft Build of OpenJDK 25 on x64 and ARM64, and Azul
    # Zulu 17 LTS on 32-bit Windows. Unpacking instead of installing leaves nothing
    # in Program Files and creates no Programs and Features entry: the JDK lives in
    # WelsonJS's own data directory, next to the components postInstall.ps1
    # downloads, and deleting that directory removes it. This is an optional
    # component: a download, hash, extraction or registration failure is reported
    # and the other components run.
    if (Test-ComponentSelected -Name "jdk") {
        $jdkTargetDirectory = Join-Path $TargetDir "jdk"
        $jdkUnpacked = $false

        # The hash was checked once after the download and is checked again here,
        # immediately before the archive is unpacked, against the same list in
        # data\DownloadHashes.txt. The temporary directory is writable by the user
        # this script runs for, so an archive replaced between the download and the
        # extraction would otherwise write files of someone else's choosing into
        # the user's AppData. A file that is not listed skips this component only.
        if ($JdkArchive) {
            $jdkAllowedHashes = Get-DownloadHashes
            if ($jdkAllowedHashes.Count -gt 0) {
                try {
                    $jdkHashActual = Get-Sha256Hash -Path $JdkArchive
                }
                catch {
                    $jdkHashActual = $null
                }

                if (-not $jdkHashActual -or -not $jdkAllowedHashes.Contains($jdkHashActual)) {
                    Write-Host "[ERROR] The OpenJDK archive is not a file listed in data\DownloadHashes.txt, so it will not be unpacked."
                    Write-Host "        SHA-256: $jdkHashActual"
                    Write-Host "[WARN] The OpenJDK component is skipped for this run."
                    Remove-Item -LiteralPath $JdkArchive -Force -ErrorAction SilentlyContinue
                    $JdkArchive = $null
                }
            }
        }

        if ($JdkArchive -and (Test-Path -LiteralPath $JdkArchive -PathType Leaf)) {
            # Unpack into a directory of this run first: an archive that fails to
            # unpack, or holds no bin\java.exe, then leaves the JDK of an earlier
            # run untouched instead of replacing it with a half-written tree.
            $jdkStagingDirectory = Join-Path $TmpDir "jdk-extract-$([guid]::NewGuid().ToString('N'))"

            try {
                Extract-CompressedFile -CompressedPath $JdkArchive -DestinationDirectory $jdkStagingDirectory -KeepArchiveRoot
                $jdkStagedHome = Find-JdkHome -Path $jdkStagingDirectory
                if (-not $jdkStagedHome) {
                    throw "The archive holds no bin\java.exe. The layout of the vendor package may have changed."
                }

                # The archive's own folder name carries the build, so the unpacked JDK
                # keeps its version in the path: jdk\jdk-25.0.5.0.2+1\bin.
                # An archive that holds the JDK at its root (no folder of its own) is
                # named after the archive file instead, so the target keeps a stable name.
                if ($jdkStagedHome -ieq $jdkStagingDirectory) {
                    $jdkFolderName = [System.IO.Path]::GetFileNameWithoutExtension($JdkArchive)
                }
                else {
                    $jdkFolderName = Split-Path -Leaf $jdkStagedHome
                }
                $jdkHome = Join-Path $jdkTargetDirectory $jdkFolderName

                if (Test-Path -LiteralPath $jdkHome) {
                    # The same build is already unpacked here: replace it with this copy.
                    # A java.exe still running from it holds the folder open, and the
                    # error below says so instead of leaving a mixed tree behind.
                    Remove-Item -LiteralPath $jdkHome -Recurse -Force -ErrorAction Stop
                }
                New-Item -ItemType Directory -Path $jdkTargetDirectory -Force | Out-Null
                Move-Item -LiteralPath $jdkStagedHome -Destination $jdkHome -Force -ErrorAction Stop

                # A JDK of an earlier version, or the installer file that earlier
                # versions of this component only staged here, is no longer used. What
                # cannot be removed is reported, not fatal.
                foreach ($jdkLeftover in @(Get-ChildItem -LiteralPath $jdkTargetDirectory -Force -ErrorAction SilentlyContinue)) {
                    if ($jdkLeftover.FullName -ieq $jdkHome) { continue }
                    try {
                        Remove-Item -LiteralPath $jdkLeftover.FullName -Recurse -Force -ErrorAction Stop
                        Write-Host "[*] Removed a file of an earlier installation: $($jdkLeftover.Name)"
                    }
                    catch {
                        Write-Host "[WARN] Could not remove $($jdkLeftover.FullName). Close any program using it and delete it by hand."
                    }
                }

                # JAVA_HOME and PATH are written for the current user, not the machine,
                # even when the setup runs elevated: the JDK is under this user's
                # %APPDATA%, so a machine-wide value would point every account at one
                # user's profile.
                $previousJavaHome = [Environment]::GetEnvironmentVariable("JAVA_HOME", "User")
                [Environment]::SetEnvironmentVariable("JAVA_HOME", $jdkHome, "User")
                $env:JAVA_HOME = $jdkHome

                $jdkBinDirectory = Join-Path $jdkHome "bin"
                $jdkPathAdded = Add-UserPathEntry -Directory $jdkBinDirectory

                Write-Host "[*] OpenJDK unpacked to: $jdkHome"
                if ($previousJavaHome -and ($previousJavaHome -ine $jdkHome)) {
                    Write-Host "    JAVA_HOME for the current user pointed at $previousJavaHome and now points at this JDK."
                }
                else {
                    Write-Host "    JAVA_HOME for the current user now points at this JDK."
                }
                if ($jdkPathAdded) {
                    Write-Host "    Added $jdkBinDirectory to the current user's PATH."
                }
                else {
                    Write-Host "    $jdkBinDirectory is already on the current user's PATH."
                }
                Write-Host "    New shells and processes see both; open ones keep the environment they started with."

                # What this component left under its earlier name is no longer used:
                # a JDK unpacked into %APPDATA%\welsonjs\microsoft_jdk by an earlier
                # build, or the installer file even earlier versions only staged there.
                # JAVA_HOME was just pointed at the new folder, so a PATH entry into
                # the old one is dropped with it. A folder that cannot be removed (a
                # java.exe still running from it, for instance) is reported and the
                # component still counts as installed.
                $jdkLegacyDirectory = Join-Path $TargetDir "microsoft_jdk"
                if ($jdkTargetDirectory -ine $jdkLegacyDirectory) {
                    $jdkLegacyPathEntries = Remove-UserPathEntriesUnder -Directory $jdkLegacyDirectory
                    if ($jdkLegacyPathEntries -eq 1) {
                        Write-Host "    Removed a PATH entry that pointed into $jdkLegacyDirectory."
                    }
                    elseif ($jdkLegacyPathEntries -gt 1) {
                        Write-Host "    Removed $jdkLegacyPathEntries PATH entries that pointed into $jdkLegacyDirectory."
                    }

                    if (Test-Path -LiteralPath $jdkLegacyDirectory) {
                        try {
                            Remove-Item -LiteralPath $jdkLegacyDirectory -Recurse -Force -ErrorAction Stop
                            Write-Host "[*] Removed what earlier versions left in $jdkLegacyDirectory"
                        }
                        catch {
                            Write-Host "[WARN] Could not remove $jdkLegacyDirectory. Close any program using it and delete it by hand."
                        }
                    }
                }

                $jdkUnpacked = $true
            }
            catch {
                Write-Host "[ERROR] The OpenJDK archive could not be unpacked and registered: $($_.Exception.Message)"
                Write-Host "[WARN] The OpenJDK component is skipped for this run."
                Write-Host "[WARN] The archive is left here, and can be unpacked by hand: $JdkArchive"
            }
            finally {
                Remove-Item -LiteralPath $jdkStagingDirectory -Recurse -Force -ErrorAction SilentlyContinue
            }

            if ($jdkUnpacked) {
                # The archive is up to 221 MB and has done its job; the directory is
                # this run's own.
                $jdkDownloadDirectory = Split-Path -Parent $JdkArchive
                Remove-Item -LiteralPath $jdkDownloadDirectory -Recurse -Force -ErrorAction SilentlyContinue
            }
        }
        else {
            Write-Host "[WARN] No verified OpenJDK archive is available for this run. Skipping installation."
        }
    }
    else {
        Write-Host "[*] jdk component not selected. Skipping installation."
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
