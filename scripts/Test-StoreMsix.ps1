# Validate XELO Link's Store package without installing it.
[CmdletBinding()]
param([string]$PackagePath = (Join-Path (Split-Path -Parent $PSScriptRoot) 'artifacts\msix\XELO-Link-1.0.0.0-x64.msix'))
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
if (-not (Test-Path -LiteralPath $PackagePath)) { throw "Package not found: $PackagePath" }
$zip = [System.IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $PackagePath).Path)
try {
 $entries = @($zip.Entries | ForEach-Object FullName)
 foreach ($required in @('AppxManifest.xml','Link.exe','Assets/StoreLogo.png','Assets/Square150x150Logo.png','Assets/Square44x44Logo.png')) {
  if (-not ($entries -contains $required -or $entries -contains ($required -replace '/','\'))) { throw "Missing MSIX payload: $required" }
 }
 # MakeAppx adds mandatory XML package metadata (AppxBlockMap.xml and [Content_Types].xml).
 # Reject debug symbols and unexpected XML documentation, but allow MSIX metadata.
 $allowedXml = @('AppxManifest.xml', 'AppxBlockMap.xml', '[Content_Types].xml', 'AppxMetadata/CodeIntegrity.cat')
 $unexpected = @($entries | Where-Object {
  $_ -match '\.pdb
 $manifestEntry = $zip.GetEntry('AppxManifest.xml')
 $reader = New-Object System.IO.StreamReader($manifestEntry.Open())
 try { [xml]$manifest = $reader.ReadToEnd() } finally { $reader.Dispose() }
 $identity = $manifest.Package.Identity
 if ($identity.Name -ne 'XELOLabs.XELOLink') { throw "Incorrect package name: $($identity.Name)" }
 if ($identity.Publisher -ne 'CN=DCC526A0-4D1E-44E7-8949-898FE66548B5') { throw "Incorrect publisher: $($identity.Publisher)" }
 if ($identity.Version -ne '1.0.0.0' -or $identity.ProcessorArchitecture -ne 'x64') { throw 'Unexpected version or architecture.' }
 Write-Host "PASS: XELO Link package contents and Partner Center identity verified."
 Write-Host "Package: $PackagePath"
 Write-Warning 'This checks package structure only; it does not verify signature, installation, functionality or Store certification.'
} finally { $zip.Dispose() }
 -or ($_.EndsWith('.xml', [System.StringComparison]::OrdinalIgnoreCase) -and $_ -notin $allowedXml)
 })
 if ($unexpected.Count -gt 0) { throw "Unexpected debug or documentation payload: $($unexpected -join ', ')" }
 $manifestEntry = $zip.GetEntry('AppxManifest.xml')
 $reader = New-Object System.IO.StreamReader($manifestEntry.Open())
 try { [xml]$manifest = $reader.ReadToEnd() } finally { $reader.Dispose() }
 $identity = $manifest.Package.Identity
 if ($identity.Name -ne 'XELOLabs.XELOLink') { throw "Incorrect package name: $($identity.Name)" }
 if ($identity.Publisher -ne 'CN=DCC526A0-4D1E-44E7-8949-898FE66548B5') { throw "Incorrect publisher: $($identity.Publisher)" }
 if ($identity.Version -ne '1.0.0.0' -or $identity.ProcessorArchitecture -ne 'x64') { throw 'Unexpected version or architecture.' }
 Write-Host "PASS: XELO Link package contents and Partner Center identity verified."
 Write-Host "Package: $PackagePath"
 Write-Warning 'This checks package structure only; it does not verify signature, installation, functionality or Store certification.'
} finally { $zip.Dispose() }
