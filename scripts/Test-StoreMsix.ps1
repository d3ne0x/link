# Validate XELO Link MSIX structure without installing it.
[CmdletBinding()]
param(
    [string]$PackagePath = (Join-Path (Split-Path -Parent $PSScriptRoot) 'artifacts\msix\XELO-Link-1.0.0.0-x64.msix')
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

if (-not (Test-Path -LiteralPath $PackagePath -PathType Leaf)) {
    throw "Package not found: $PackagePath"
}
$zip = [System.IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $PackagePath).Path)
try {
    $entries = @($zip.Entries | ForEach-Object { $_.FullName.Replace('\', '/') })
    $requiredFiles = @(
        'AppxManifest.xml',
        'Link.exe',
        'Link.dll',
        'Link.runtimeconfig.json',
        'Assets/StoreLogo.png',
        'Assets/Square150x150Logo.png',
        'Assets/Square44x44Logo.png'
    )
    foreach ($required in $requiredFiles) {
        if ($entries -notcontains $required) {
            throw "Missing MSIX payload: $required"
        }
    }

    foreach ($forbidden in @('coreclr.dll','hostfxr.dll','System.Private.CoreLib.dll','PresentationFramework.dll')) {
        if ($entries -contains $forbidden) { throw "Bundled .NET runtime found: $forbidden. Build framework-dependent MSIX." }
    }
    $runtimeEntry = $zip.GetEntry('Link.runtimeconfig.json')
    $runtimeReader = New-Object System.IO.StreamReader($runtimeEntry.Open())
    try { $runtimeConfig = $runtimeReader.ReadToEnd() | ConvertFrom-Json }
    finally { $runtimeReader.Dispose() }
    if (@($runtimeConfig.runtimeOptions.frameworks | Where-Object { $_.name -eq 'Microsoft.WindowsDesktop.App' }).Count -eq 0) {
        throw 'MSIX runtimeconfig does not declare Microsoft.WindowsDesktop.App.'
    }

    # MakeAppx adds AppxBlockMap.xml and [Content_Types].xml as package metadata.
    $allowedXml = @('AppxManifest.xml', 'AppxBlockMap.xml', '[Content_Types].xml')
    $unexpected = @($entries | Where-Object {
        ($_ -match '\.pdb$') -or (($_ -match '\.xml$') -and ($_ -notin $allowedXml))
    })
    if ($unexpected.Count -gt 0) {
        throw "Unexpected debug or documentation payload: $($unexpected -join ', ')"
    }

    $manifestEntry = $zip.GetEntry('AppxManifest.xml')
    if ($null -eq $manifestEntry) { throw 'Missing AppxManifest.xml entry.' }
    $reader = New-Object System.IO.StreamReader($manifestEntry.Open())
    try {
        [xml]$manifest = $reader.ReadToEnd()
    }
    finally {
        $reader.Dispose()
    }
    $identity = $manifest.Package.Identity
    if ($identity.Name -ne 'XELOLabs.XELOLink') {
        throw "Incorrect package name: $($identity.Name)"
    }
    if ($identity.Publisher -ne 'CN=DCC526A0-4D1E-44E7-8949-898FE66548B5') {
        throw "Incorrect publisher: $($identity.Publisher)"
    }
    if ($identity.Version -ne '1.0.0.0') {
        throw "Unexpected version: $($identity.Version)"
    }
    if ($identity.ProcessorArchitecture -ne 'x64') {
        throw "Unexpected architecture: $($identity.ProcessorArchitecture)"
    }
    Write-Host 'PASS: XELO Link package contents and Partner Center identity verified.'
    Write-Host "Package: $PackagePath"
    Write-Warning 'Structure only: signature, installation, functionality and Store certification are not tested.'
}
finally {
    $zip.Dispose()
}
