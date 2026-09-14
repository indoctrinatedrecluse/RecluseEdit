<#
.SYNOPSIS
    Builds the solution in Release configuration, runs tests, signs all binaries, and optionally packages a distribution archive.

.DESCRIPTION
    Comprehensive developer automation script that restores dependencies, builds the entire
    RecluseEdit solution and all extensions, runs the test suite, signs all output executables
    and DLLs with Authenticode SHA256 signatures, and packages the result into a clean distribution archive.

.PARAMETER Configuration
    Build configuration (default: Release).

.PARAMETER SkipTests
    If specified, skips running the automated test suite.

.PARAMETER Package
    If specified, stages files into package/RecluseEdit and creates a portable zip archive.

.PARAMETER Version
    Release version tag (e.g. v5.5.0) used when packaging the distribution archive.
#>
[CmdletBinding()]
param (
    [string]$Configuration = "Release",

    [switch]$SkipTests,

    [switch]$Package,

    [string]$Version = "v5.5.0"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot

try {
    Write-Host "=======================================================" -ForegroundColor Cyan
    Write-Host " Building RecluseEdit ($Configuration)" -ForegroundColor Cyan
    Write-Host "=======================================================" -ForegroundColor Cyan

    dotnet restore RecluseEdit.slnx
    if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed" }

    if (-not $SkipTests) {
        Write-Host "`nRunning Test Suite..." -ForegroundColor Cyan
        dotnet test RecluseEdit.slnx -c $Configuration --no-restore --nologo
        if ($LASTEXITCODE -ne 0) { throw "dotnet test failed" }
    }

    Write-Host "`nBuilding Solution and Extensions..." -ForegroundColor Cyan
    dotnet build RecluseEdit.slnx -c $Configuration --no-restore --nologo
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed" }

    # Sign build output directory
    $outputDir = Join-Path $repoRoot "bin/$Configuration/net10.0-windows"
    $signScript = Join-Path $PSScriptRoot "sign-release.ps1"

    Write-Host "`nSigning Build Output Binaries..." -ForegroundColor Cyan
    & $signScript -TargetDir $outputDir
    if ($LASTEXITCODE -ne 0) { throw "Code signing failed" }

    # Package distribution archive if requested
    if ($Package) {
        Write-Host "`nStaging Distribution Package..." -ForegroundColor Cyan
        $stagingDir = Join-Path $repoRoot "package/RecluseEdit"
        if (Test-Path $stagingDir) {
            Remove-Item -Path $stagingDir -Recurse -Force
        }
        New-Item -ItemType Directory -Force -Path $stagingDir | Out-Null

        # Copy binaries and extensions
        Copy-Item -Path "$outputDir/*" -Destination $stagingDir -Recurse -Force

        # Copy metadata, license, and public cert
        if (Test-Path "LICENSE") { Copy-Item -Path "LICENSE" -Destination $stagingDir -Force }
        if (Test-Path "README.md") { Copy-Item -Path "README.md" -Destination $stagingDir -Force }

        $caCer = Join-Path $repoRoot "certs/indoctrinatedrecluse-RootCA.cer"
        if (Test-Path $caCer) { Copy-Item -Path $caCer -Destination $stagingDir -Force }

        # Remove test binaries if copied
        $testDll = Join-Path $stagingDir "RecluseEdit.Tests.dll"
        if (Test-Path $testDll) {
            Remove-Item -Path (Join-Path $stagingDir "RecluseEdit.Tests.*") -Force
        }

        # Re-verify and sign staging directory
        Write-Host "`nSigning Staging Directory Binaries..." -ForegroundColor Cyan
        & $signScript -TargetDir $stagingDir

        # Create zip archive
        $zipName = "RecluseEdit-windows-$Version.zip"
        $zipPath = Join-Path $repoRoot $zipName
        if (Test-Path $zipPath) { Remove-Item -Path $zipPath -Force }

        Compress-Archive -Path "$stagingDir/*" -DestinationPath $zipPath -Force
        Write-Host "`nSuccessfully created signed release package: $zipName" -ForegroundColor Green
    }

    Write-Host "`nBuild, test, and signing pipeline completed successfully!" -ForegroundColor Green
} finally {
    Pop-Location
}
