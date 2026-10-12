# XELO Link release build (run on Windows with .NET 8 SDK)
[CmdletBinding()]
param([string]$Runtime = 'win-x64')
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root 'Symbolic11.csproj'
$output = Join-Path $root "artifacts\\publish\\$Runtime"
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw '.NET SDK is required.' }
Push-Location $root
try {
    dotnet restore $project -r $Runtime
    if ($LASTEXITCODE -ne 0) { throw 'Restore failed.' }
    dotnet build $project -c Release -r $Runtime --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
    dotnet publish $project -c Release -r $Runtime --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $output
    if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
    $exe = Join-Path $output 'Link.exe'
    if (-not (Test-Path $exe)) { throw "Missing executable: $exe" }
    Write-Host "Release build ready: $exe"
    Write-Warning 'This is an unpackaged release build, NOT a signed MSIX or Microsoft Store submission.'
} finally { Pop-Location }
