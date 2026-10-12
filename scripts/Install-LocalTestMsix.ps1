# Install signed XELO Link MSIX for local functional testing.
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$package = Join-Path $root 'artifacts\msix\local-test\XELO-Link-1.0.0.0-x64-LOCAL-TEST.msix'
if (-not (Test-Path -LiteralPath $package)) { throw 'Signed test MSIX not found. Run Sign-LocalTestMsix.ps1 -TrustForCurrentUser.' }
Add-AppxPackage -Path $package -ErrorAction Stop
$app = Get-AppxPackage -Name 'XELOLabs.XELOLink' | Sort-Object Version -Descending | Select-Object -First 1
if (-not $app) { throw 'MSIX installation did not register XELO Link.' }
Write-Host "Installed: $($app.PackageFullName)"
Write-Host 'Launch XELO Link from Start menu. Test creation, scanning, deletion, then uninstall.'
