<#
.SYNOPSIS
    Signs all executable and library binaries in a target directory with an Authenticode certificate.

.DESCRIPTION
    Discovers all .exe and .dll files within the specified target directory (including extension DLLs)
    and signs each with SHA256 Authenticode signatures using the indoctrinatedrecluse code-signing
    certificate. If no certificate is found, automatically invokes ensure-ca-cert.ps1 to generate one.
    Also copies the public Root CA certificate into the target directory.

.PARAMETER TargetDir
    The root directory containing compiled binaries to sign. Defaults to bin/Release/net10.0-windows.

.PARAMETER CertPath
    Optional path to an explicit .pfx certificate file.

.PARAMETER CertPassword
    Optional password for the explicit .pfx certificate file.

.PARAMETER OrgName
    Organization name for certificate resolution. Defaults to "indoctrinatedrecluse".

.PARAMETER HashAlgorithm
    Digest algorithm for Authenticode signing. Defaults to "SHA256".
#>
[CmdletBinding()]
param (
    [Parameter(Position = 0)]
    [string]$TargetDir,

    [string]$CertPath,

    [string]$CertPassword = "RecluseEditSigning2026!",

    [string]$OrgName = "indoctrinatedrecluse",

    [string]$HashAlgorithm = "SHA256"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot

if ([string]::IsNullOrWhiteSpace($TargetDir)) {
    $TargetDir = Join-Path $repoRoot "bin/Release/net10.0-windows"
}

if (-not (Test-Path $TargetDir)) {
    throw "Target directory does not exist: $TargetDir"
}
$TargetDir = (Resolve-Path $TargetDir).Path

# 1. Obtain Code Signing Certificate
$codeSignCert = $null

if (-not [string]::IsNullOrWhiteSpace($CertPath) -and (Test-Path $CertPath)) {
    Write-Host "Loading Code Signing Certificate from explicit file: $CertPath" -ForegroundColor Cyan
    $secPass = ConvertTo-SecureString -String $CertPassword -AsPlainText -Force
    $imported = Import-PfxCertificate -FilePath $CertPath -CertStoreLocation "Cert:\CurrentUser\My" -Password $secPass -Exportable
    $codeSignCert = $imported | Where-Object { $_.HasPrivateKey } | Select-Object -First 1
} else {
    Write-Host "Resolving Code Signing Certificate for '$OrgName'..." -ForegroundColor Cyan
    $ensureScript = Join-Path $PSScriptRoot "ensure-ca-cert.ps1"
    $codeSignCert = & $ensureScript -OrgName $OrgName
}

if ($null -eq $codeSignCert -or -not $codeSignCert.HasPrivateKey) {
    throw "Failed to locate or generate a valid Code Signing Certificate with private key for '$OrgName'."
}

Write-Host "Using Code Signing Certificate:" -ForegroundColor Green
Write-Host "  Subject:    $($codeSignCert.Subject)"
Write-Host "  Issuer:     $($codeSignCert.Issuer)"
Write-Host "  Thumbprint: $($codeSignCert.Thumbprint)"
Write-Host "  Expires:    $($codeSignCert.NotAfter.ToString('yyyy-MM-dd'))"

# 2. Discover binaries to sign (.exe, .dll)
$binaries = Get-ChildItem -Path $TargetDir -Include "*.exe", "*.dll" -Recurse -File |
    Where-Object { $_.Name -notlike "*Tests.dll" -and $_.Name -notlike "*.vshost.*" }

if ($binaries.Count -eq 0) {
    Write-Warning "No binaries (.exe, .dll) found to sign in: $TargetDir"
    return
}

Write-Host "`nSigning $($binaries.Count) binaries in: $TargetDir`n" -ForegroundColor Cyan

$successCount = 0
$failCount = 0
$results = @()

foreach ($bin in $binaries) {
    $relPath = $bin.FullName.Substring($TargetDir.Length).TrimStart('\', '/')
    try {
        $sig = Set-AuthenticodeSignature -FilePath $bin.FullName -Certificate $codeSignCert -HashAlgorithm $HashAlgorithm
        
        $verified = Get-AuthenticodeSignature -FilePath $bin.FullName
        $hasSigner = ($null -ne $verified.SignerCertificate)
        $signerSubject = if ($hasSigner) { $verified.SignerCertificate.Subject } else { "None" }

        if ($hasSigner) {
            $successCount++
            $results += [PSCustomObject]@{
                File = $relPath
                Signed = "Yes"
                Status = $sig.Status
                Signer = $signerSubject
            }
            Write-Host "  [OK] $relPath" -ForegroundColor Green
        } else {
            $failCount++
            $results += [PSCustomObject]@{
                File = $relPath
                Signed = "No"
                Status = $sig.Status
                Signer = "Failed"
            }
            Write-Host "  [FAIL] $relPath (Status: $($sig.Status))" -ForegroundColor Red
        }
    } catch {
        $failCount++
        $results += [PSCustomObject]@{
            File = $relPath
            Signed = "Error"
            Status = $_.Exception.Message
            Signer = "Error"
        }
        Write-Host "  [ERROR] $($relPath): $($_.Exception.Message)" -ForegroundColor Red
    }
}

# 3. Copy public Root CA certificate into target directory
$rootCaCer = Join-Path $repoRoot "certs/$OrgName-RootCA.cer"
if (Test-Path $rootCaCer) {
    Copy-Item -Path $rootCaCer -Destination $TargetDir -Force
    Write-Host "`nIncluded public Root CA certificate in target directory: $OrgName-RootCA.cer" -ForegroundColor Cyan
}

Write-Host "`n=======================================================" -ForegroundColor Cyan
Write-Host " Code Signing Summary" -ForegroundColor Cyan
Write-Host "=======================================================" -ForegroundColor Cyan
Write-Host "  Total Processed: $($binaries.Count)"
Write-Host "  Successfully Signed: $successCount" -ForegroundColor Green
if ($failCount -gt 0) {
    Write-Host "  Failed: $failCount" -ForegroundColor Red
    throw "Code signing failed for $failCount binary files."
}

Write-Host "All binaries signed successfully with SHA256 Authenticode signature." -ForegroundColor Green
