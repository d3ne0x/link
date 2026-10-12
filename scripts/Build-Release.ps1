# XELO Link release build. FrameworkDependent is the default for Store MSIX.
[CmdletBinding()]
param(
 [string]$Runtime = 'win-x64',
 [ValidateSet('FrameworkDependent','SingleFile','Certification')][string]$BuildVariant = 'FrameworkDependent'
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root 'Symbolic11.csproj'
$output = Join-Path $root "artifacts\publish\$Runtime"
$singleFile = $BuildVariant -eq 'SingleFile'
$selfContained = $BuildVariant -ne 'FrameworkDependent'
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw '.NET SDK is required.' }
Push-Location $root
try {
    dotnet restore $project -r $Runtime
    if ($LASTEXITCODE -ne 0) { throw 'Restore failed.' }
    if (Test-Path -LiteralPath $output) { Remove-Item -LiteralPath $output -Recurse -Force }
    $sc = $selfContained.ToString().ToLowerInvariant()
    $sf = $singleFile.ToString().ToLowerInvariant()
    $extract = $singleFile.ToString().ToLowerInvariant()
    dotnet publish $project -c Release -r $Runtime --self-contained $sc "-p:PublishSingleFile=$sf" "-p:IncludeNativeLibrariesForSelfExtract=$extract" -p:DebugType=None -p:DebugSymbols=false -o $output
    if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
    $exe = Join-Path $output 'Link.exe'
    if (-not (Test-Path -LiteralPath $exe)) { throw "Missing executable: $exe" }
    $runtimeConfig = Join-Path $output 'Link.runtimeconfig.json'
    if (-not (Test-Path -LiteralPath $runtimeConfig)) { throw 'Missing Link.runtimeconfig.json.' }
    $config = Get-Content -LiteralPath $runtimeConfig -Raw | ConvertFrom-Json
    if ($BuildVariant -eq 'FrameworkDependent') {
        $frameworks = @($config.runtimeOptions.frameworks)
        if ($frameworks.Count -eq 0 -or @($frameworks | Where-Object name -eq 'Microsoft.WindowsDesktop.App').Count -eq 0) {
            throw 'Framework-dependent publish does not reference Microsoft.WindowsDesktop.App.'
        }
        foreach ($unexpected in @('coreclr.dll','hostfxr.dll','System.Private.CoreLib.dll','PresentationFramework.dll')) {
            if (Test-Path -LiteralPath (Join-Path $output $unexpected)) {
                throw "Unexpected bundled framework component: $unexpected"
            }
        }
    }
    [xml]$manifest = Get-Content -LiteralPath (Join-Path $root 'app.manifest') -Raw
    if ($manifest.OuterXml -notmatch 'asInvoker' -or $manifest.OuterXml -notmatch 'dpiAwareness') {
        throw 'Application manifest is missing UAC or DPI-awareness configuration.'
    }
    $files = @(Get-ChildItem -LiteralPath $output -File)
    Write-Host "Release build ready ($BuildVariant): $exe ($($files.Count) files)"
    if ($BuildVariant -eq 'FrameworkDependent') {
        Write-Warning 'Requires matching Microsoft .NET Desktop Runtime on the target PC. MSIX alone does not install it.'
    }
    Write-Warning 'Unpackaged build only; sign and test MSIX separately.'
} finally { Pop-Location }
