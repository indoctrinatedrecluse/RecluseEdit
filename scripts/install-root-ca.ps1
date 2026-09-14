<#
.SYNOPSIS
    Installs the indoctrinatedrecluse Root CA certificate into the Windows Trusted Root store.

.DESCRIPTION
    Installs certs/indoctrinatedrecluse-RootCA.cer into the CurrentUser (or LocalMachine if running as
    Administrator) Trusted Root Certification Authorities certificate store. Once installed, Windows
    will recognize all RecluseEdit executables and DLLs as trusted, eliminating unknown-publisher warnings.

.PARAMETER CertPath
    Path to the public Root CA certificate (.cer). Defaults to certs/indoctrinatedrecluse-RootCA.cer.

.PARAMETER StoreLocation
    Certificate store scope: CurrentUser (default) or LocalMachine (requires Administrator).
#>
[CmdletBinding()]
param (
    [string]$CertPath,

    [ValidateSet("CurrentUser", "LocalMachine")]
    [string]$StoreLocation = "CurrentUser"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot

if ([string]::IsNullOrWhiteSpace($CertPath)) {
    $CertPath = Join-Path $repoRoot "certs/indoctrinatedrecluse-RootCA.cer"
}

if (-not (Test-Path $CertPath)) {
    throw "Root CA certificate file not found: $CertPath"
}
$CertPath = (Resolve-Path $CertPath).Path

Write-Host "Installing Root CA Certificate into $StoreLocation\Root..." -ForegroundColor Cyan
Write-Host "  Certificate Path: $CertPath"

try {
    $cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($CertPath)
    Write-Host "  Subject:    $($cert.Subject)"
    Write-Host "  Thumbprint: $($cert.Thumbprint)"
    Write-Host "  Valid Till: $($cert.NotAfter.ToString('yyyy-MM-dd'))"

    $store = New-Object System.Security.Cryptography.X509Certificates.X509Store("Root", $StoreLocation)
    $store.Open("ReadWrite")
    
    # Check if already installed
    $existing = $store.Certificates.Find([System.Security.Cryptography.X509Certificates.X509FindType]::FindByThumbprint, $cert.Thumbprint, $false)
    if ($existing.Count -gt 0) {
        Write-Host "Root CA certificate is already installed in $StoreLocation\Root." -ForegroundColor Green
    } else {
        $store.Add($cert)
        Write-Host "Successfully installed Root CA certificate into $StoreLocation\Root." -ForegroundColor Green
    }
    $store.Close()
} catch {
    Write-Error "Failed to install Root CA certificate: $($_.Exception.Message)"
    throw
}
