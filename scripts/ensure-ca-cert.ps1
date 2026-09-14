<#
.SYNOPSIS
    Ensures that a self-signed Root CA and Code Signing certificate pair exists for indoctrinatedrecluse.

.DESCRIPTION
    Checks if a valid code-signing certificate for the organization exists in Cert:\CurrentUser\My
    or as an exported PFX in the certs/ directory. If not present, generates a new Root CA
    and an Authenticode Code Signing certificate issued by that Root CA, and exports the public
    CA certificate (.cer) and PFX bundle (.pfx).

.PARAMETER CertDir
    Directory where certificates and PFX files are stored. Defaults to <repo-root>/certs.

.PARAMETER OrgName
    Organization name for Subject and Issuer. Defaults to "indoctrinatedrecluse".

.PARAMETER PfxPassword
    Password used to encrypt the exported .pfx bundle.

.PARAMETER Force
    If specified, generates a new certificate pair even if one already exists.
#>
[CmdletBinding()]
param (
    [Parameter(Position = 0)]
    [string]$CertDir,

    [string]$OrgName = "indoctrinatedrecluse",

    [string]$PfxPassword = "RecluseEditSigning2026!",

    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($CertDir)) {
    $CertDir = Join-Path (Split-Path -Parent $PSScriptRoot) "certs"
}

if (-not (Test-Path $CertDir)) {
    New-Item -ItemType Directory -Force -Path $CertDir | Out-Null
}
$CertDir = (Resolve-Path $CertDir).Path

$caCerPath = Join-Path $CertDir "$OrgName-RootCA.cer"
$codeSignPfxPath = Join-Path $CertDir "$OrgName-CodeSigning.pfx"

# 1. Check if a valid certificate already exists in Cert:\CurrentUser\My
$existingCert = $null

if (-not $Force) {
    $now = Get-Date
    $candidates = Get-ChildItem "Cert:\CurrentUser\My" -ErrorAction SilentlyContinue | Where-Object {
        $_.HasPrivateKey -and
        $_.NotAfter -gt $now.AddDays(30) -and
        $_.Subject -like "*O=$OrgName*" -and
        ($_.EnhancedKeyUsageList.ObjectId -contains "1.3.6.1.5.5.7.3.3" -or $_.Subject -like "*Code Signing*")
    }

    if ($candidates) {
        $existingCert = $candidates | Sort-Object NotAfter -Descending | Select-Object -First 1
        Write-Host "Found existing valid Code Signing Certificate in store:" -ForegroundColor Green
        Write-Host "  Subject:    $($existingCert.Subject)"
        Write-Host "  Thumbprint: $($existingCert.Thumbprint)"
        Write-Host "  Expires:    $($existingCert.NotAfter.ToString('yyyy-MM-dd'))"
    } elseif (Test-Path $codeSignPfxPath) {
        # Try importing from PFX
        try {
            $secPassword = ConvertTo-SecureString -String $PfxPassword -AsPlainText -Force
            $importedCerts = Import-PfxCertificate -FilePath $codeSignPfxPath -CertStoreLocation "Cert:\CurrentUser\My" -Password $secPassword -Exportable
            $existingCert = $importedCerts | Where-Object { $_.HasPrivateKey } | Select-Object -First 1
            if ($existingCert) {
                Write-Host "Imported Code Signing Certificate from PFX ($codeSignPfxPath):" -ForegroundColor Green
                Write-Host "  Subject:    $($existingCert.Subject)"
                Write-Host "  Thumbprint: $($existingCert.Thumbprint)"
            }
        } catch {
            Write-Warning "Failed to import from PFX: $($_.Exception.Message). Generating new certificates."
        }
    }
}

if ($existingCert -and (-not $Force)) {
    # Ensure public Root CA file is present if Root CA exists in store
    if (-not (Test-Path $caCerPath)) {
        $rootCandidates = Get-ChildItem "Cert:\CurrentUser\My" -ErrorAction SilentlyContinue | Where-Object {
            $_.Subject -like "*CN=$OrgName Root CA*"
        }
        if ($rootCandidates) {
            $rootCa = $rootCandidates | Sort-Object NotAfter -Descending | Select-Object -First 1
            Export-Certificate -Cert $rootCa -FilePath $caCerPath -Force | Out-Null
            Write-Host "Exported public Root CA to: $caCerPath" -ForegroundColor Cyan
        }
    }
    return $existingCert
}

# 2. Generate new Root CA & Code Signing certificate pair
Write-Host "Generating self-signed Root CA and Code Signing certificate for '$OrgName'..." -ForegroundColor Cyan

# Generate Root CA
$rootCaSubject = "CN=$OrgName Root CA, O=$OrgName"
$rootCa = New-SelfSignedCertificate `
    -Subject $rootCaSubject `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -KeyUsage CertSign, CRLSign `
    -KeyExportPolicy Exportable `
    -KeyLength 2048 `
    -HashAlgorithm SHA256 `
    -NotAfter (Get-Date).AddYears(10) `
    -TextExtension @("2.5.29.19={critical}{text}ca=1")

Write-Host "  Root CA Created:" -ForegroundColor Green
Write-Host "    Subject:    $($rootCa.Subject)"
Write-Host "    Thumbprint: $($rootCa.Thumbprint)"
Write-Host "    Valid Till: $($rootCa.NotAfter.ToString('yyyy-MM-dd'))"

# Generate Code Signing Certificate signed by the Root CA
$codeSignSubject = "CN=$OrgName Code Signing, O=$OrgName"
$codeSignCert = New-SelfSignedCertificate `
    -Subject $codeSignSubject `
    -Signer $rootCa `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -Type CodeSigningCert `
    -KeyExportPolicy Exportable `
    -KeyLength 2048 `
    -HashAlgorithm SHA256 `
    -NotAfter (Get-Date).AddYears(5)

Write-Host "  Code Signing Certificate Created:" -ForegroundColor Green
Write-Host "    Subject:    $($codeSignCert.Subject)"
Write-Host "    Issuer:     $($codeSignCert.Issuer)"
Write-Host "    Thumbprint: $($codeSignCert.Thumbprint)"
Write-Host "    Valid Till: $($codeSignCert.NotAfter.ToString('yyyy-MM-dd'))"

# 3. Export Public Root CA (.cer)
Export-Certificate -Cert $rootCa -FilePath $caCerPath -Force | Out-Null
Write-Host "  Public Root CA exported to: $caCerPath" -ForegroundColor Cyan

# 4. Export Code Signing PFX (.pfx)
$secPassword = ConvertTo-SecureString -String $PfxPassword -AsPlainText -Force
Export-PfxCertificate -Cert $codeSignCert -FilePath $codeSignPfxPath -Password $secPassword -Force | Out-Null
Write-Host "  Code Signing PFX exported to: $codeSignPfxPath" -ForegroundColor Cyan

Write-Host "Certificate provisioning completed successfully." -ForegroundColor Green

return $codeSignCert
