# XELO Link release build (run on Windows with .NET 8 SDK)
[CmdletBinding()]
param(
 [string]$Runtime = 'win-x64',
 [ValidateSet('SingleFile','Certification')][string]$BuildVariant = 'SingleFile'
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root 'Symbolic11.csproj'
$output = Join-Path $root "artifacts\\publish\\$Runtime"
$singleFile = $BuildVariant -eq 'SingleFile'
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw '.NET SDK is required.' }
Push-Location $root
try {
    dotnet restore $project -r $Runtime
    if ($LASTEXITCODE -ne 0) { throw 'Restore failed.' }
    dotnet build $project -c Release -r $Runtime --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
    # Clean the output: switching variants must not leave stale runtime binaries behind.
    if (Test-Path -LiteralPath $output) { Remove-Item -LiteralPath $output -Recurse -Force }
    dotnet publish $project -c Release -r $Runtime --self-contained true "-p:PublishSingleFile=$($singleFile.ToString().ToLowerInvariant())" -p:IncludeNativeLibrariesForSelfExtract=false -p:DebugType=None -p:DebugSymbols=false -o $output
    if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
    $exe = Join-Path $output 'Link.exe'
    if (-not (Test-Path $exe)) { throw "Missing executable: $exe" }
    # The apphost must carry the requestedExecutionLevel and DPI settings.
    # This check is intentionally limited to the source manifest; Windows SDK
    # mt.exe extraction is required to validate the final PE resource.
    [xml]$manifest = Get-Content -LiteralPath (Join-Path $root 'app.manifest') -Raw
    if ($manifest.OuterXml -notmatch 'asInvoker' -or $manifest.OuterXml -notmatch 'dpiAwareness') {
        throw 'Application manifest is missing UAC or DPI-awareness configuration.'
    }
    Write-Host "Release build ready ($BuildVariant): $exe"
    if (-not $singleFile) {
        $runtimeFiles = @(Get-ChildItem -LiteralPath $output -File)
        if ($runtimeFiles.Count -lt 5) { throw 'Certification publish appears incomplete: expected runtime payload files.' }
        Write-Host "Certification variant: $($runtimeFiles.Count) published files, non-single-file."
    }
    Write-Warning 'This is an unpackaged release build, NOT a signed MSIX or Microsoft Store submission.'
} finally { Pop-Location }
