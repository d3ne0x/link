# XELO Link Store release readiness check (PowerShell 5.1)
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root 'Symbolic11.csproj'
$manifest = Join-Path $root 'packaging\AppxManifest.xml'
$package = Join-Path $root 'artifacts\msix\XELO-Link-1.0.0.0-x64.msix'
if (-not (Test-Path $package)) { throw 'MSIX not found. Run Build-Release.ps1 then Build-StoreMsix.ps1.' }
[xml]$app = Get-Content -LiteralPath (Join-Path $root 'app.manifest') -Raw
if ((Get-Content -LiteralPath (Join-Path $root 'app.manifest') -Raw) -notmatch 'requestedExecutionLevel level="asInvoker"') {
 throw 'App manifest must use asInvoker for MSIX.'
}
& (Join-Path $PSScriptRoot 'Test-StoreMsix.ps1') -PackagePath $package
if ($LASTEXITCODE -ne 0) { throw 'MSIX validation failed.' }
Write-Host 'PASS: MSIX structure and application execution level.'
Write-Warning 'Final logo assets, installed-MSIX regression tests, WACK and Partner Center certification still require manual verification.'
Write-Host 'Windows App Certification Kit: launch appcert.exe from the installed Windows SDK, select Validate Windows App, and test the signed local MSIX installation.'
