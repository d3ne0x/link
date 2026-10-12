# Checks the installed .NET Desktop Runtime for XELO Link.
# Use for direct-download installations; MSIX itself cannot run this script during install.
[CmdletBinding()]
param([switch]$OpenDownloadPage)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$configPath = Join-Path $root 'artifacts\publish\win-x64\Link.runtimeconfig.json'
if (-not (Test-Path -LiteralPath $configPath)) { throw 'Build framework-dependent release first.' }
$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
$framework = @($config.runtimeOptions.frameworks | Where-Object name -eq 'Microsoft.WindowsDesktop.App') | Select-Object -First 1
if (-not $framework) { throw 'Published application does not require Microsoft.WindowsDesktop.App.' }
$required = [version]$framework.version
$dotnet = Join-Path $env:ProgramFiles 'dotnet\dotnet.exe'
$installed = @()
if (Test-Path -LiteralPath $dotnet) {
    $lines = & $dotnet --list-runtimes
    if ($LASTEXITCODE -ne 0) { throw 'Unable to query installed .NET runtimes.' }
    foreach ($line in $lines) {
        if ($line -match '^Microsoft\.WindowsDesktop\.App\s+(\d+\.\d+\.\d+)\s+') {
            $installed += [version]$Matches[1]
        }
    }
}
$compatible = @($installed | Where-Object { $_.Major -eq $required.Major -and $_.Minor -eq $required.Minor -and $_ -ge $required })
if ($compatible.Count -gt 0) {
    Write-Host "PASS: .NET Desktop Runtime $($compatible | Sort-Object -Descending | Select-Object -First 1) is installed (x64 location)."
    exit 0
}
Write-Warning "XELO Link requires Microsoft .NET Desktop Runtime $($required.Major).$($required.Minor) (x64) or a compatible runtime. It was not found."
Write-Host 'Official download: https://dotnet.microsoft.com/en-us/download/dotnet'
if ($OpenDownloadPage) { Start-Process 'https://dotnet.microsoft.com/en-us/download/dotnet' }
exit 2
