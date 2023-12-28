# WelsonJS-WebRequest.ps1
# Namhyeon Go <abuse@catswords.net>
# https://github.com/gnh1201/welsonjs
param(
    [string]$url,
    [string]$method = "GET",
    [string]$data,
    [string]$outputFile,
    [switch]$insecure,
    [string]$proxy,
    [string[]]$headers
)

# Set headers
$headersHashtable = @{}
if ($headers) {
    foreach ($header in $headers) {
        $headerParts = $header -split ':', 2
        if ($headerParts.Count -eq 2) {
            $headersHashtable[$headerParts[0].Trim()] = $headerParts[1].Trim()
        }
    }
}

# Set options for Invoke-RestMethod or Invoke-WebRequest
$options = @{
    Uri         = $url
    Method      = $method
    Headers     = $headersHashtable
    OutFile     = $outputFile
    ContentType = "application/json"
}

# Add data if present
if ($data) {
    $options["Body"] = $data
}

# Set proxy if provided
if ($proxy) {
    $options["Proxy"] = $proxy
}

# Disable SSL certificate validation if insecure option is specified
if ($insecure) {
    [System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}
}

# Perform the HTTP request
try {
    if ($outputFile) {
        Invoke-WebRequest @options
    } else {
        Invoke-RestMethod @options
    }
} finally {
    # Restore SSL certificate validation callback
    [System.Net.ServicePointManager]::ServerCertificateValidationCallback = $null
}
