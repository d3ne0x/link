# XELO Link Windows certification diagnostics. Windows PowerShell 5.1 compatible.
[CmdletBinding()]
param([string]$PublishPath = (Join-Path (Split-Path -Parent $PSScriptRoot) 'artifacts\publish\win-x64'))
$ErrorActionPreference = 'Stop'
$exe = Join-Path $PublishPath 'Link.exe'
if (-not (Test-Path -LiteralPath $exe)) { throw "Build XELO Link first. Missing: $exe" }
$root = Split-Path -Parent $PSScriptRoot
$report = Join-Path $root 'artifacts\certification'
New-Item -ItemType Directory -Path $report -Force | Out-Null
$kit = Join-Path ([Environment]::GetFolderPath('ProgramFilesX86')) 'Windows Kits\10\bin'
$mt = Get-ChildItem -LiteralPath $kit -Filter mt.exe -Recurse -ErrorAction SilentlyContinue |
 Where-Object { $_.FullName -match '\\x64\\mt.exe$' } |
 Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
if (-not $mt) { throw 'Windows SDK mt.exe not found. Install the Windows SDK.' }
$manifestPath = Join-Path $report 'Link.embedded.manifest'
if (Test-Path -LiteralPath $manifestPath) { Remove-Item -LiteralPath $manifestPath -Force }
& $mt "-inputresource:$exe;#1" "-out:$manifestPath"
if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $manifestPath)) {
 throw 'Failed to extract embedded manifest from Link.exe. This is a release blocker.'
}
$xml = Get-Content -LiteralPath $manifestPath -Raw
foreach ($required in @('asInvoker', 'dpiAwareness', 'PerMonitor')) {
 if (-not $xml.Contains($required)) { throw "Published Link.exe manifest missing $required. Inspect $manifestPath" }
}
Write-Host "Embedded executable manifest: PASS ($manifestPath)"
$assemblies = @(Get-ChildItem -LiteralPath $PublishPath -Filter '*.dll' -File)
$summary = [pscustomobject]@{
 Executable = $exe
 SizeMB = [Math]::Round((Get-Item -LiteralPath $exe).Length / 1MB, 2)
 DllCount = $assemblies.Count
 WindowsFormsDllCount = @($assemblies | Where-Object Name -like '*Windows.Forms*').Count
 ManifestPath = $manifestPath
}
$summary | Format-List
$summary | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $report 'publish-summary.json') -Encoding UTF8
Write-Host 'NOTE: WACK blocked-executable references are static scanner findings, not missing DLLs.'
Write-Host 'NOTE: This script verifies the embedded manifest, not actual runtime DPI awareness.'
