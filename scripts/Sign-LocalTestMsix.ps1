# Sign a COPY of the XELO Link MSIX for local installation testing.
# Windows PowerShell 5.1, Windows SDK SignTool, and Windows required.
[CmdletBinding()]
param(
 [string]$PackagePath = (Join-Path (Split-Path -Parent $PSScriptRoot) 'artifacts\msix\XELO-Link-1.0.0.0-x64.msix'),
 [switch]$TrustForCurrentUser
)
$ErrorActionPreference = 'Stop'
$subject = 'CN=DCC526A0-4D1E-44E7-8949-898FE66548B5'
$root = Split-Path -Parent $PSScriptRoot
$testDir = Join-Path $root 'artifacts\msix\local-test'
New-Item -ItemType Directory -Path $testDir -Force | Out-Null
if (-not (Test-Path -LiteralPath $PackagePath -PathType Leaf)) { throw "Package not found: $PackagePath" }
$kit = Join-Path ([Environment]::GetFolderPath('ProgramFilesX86')) 'Windows Kits\10\bin'
$signTool = Get-ChildItem -LiteralPath $kit -Filter signtool.exe -Recurse -ErrorAction SilentlyContinue |
 Where-Object { $_.FullName -match '\\x64\\signtool.exe$' } |
 Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
if (-not $signTool) { throw 'SignTool.exe not found. Install Windows SDK signing tools.' }
$cert = New-SelfSignedCertificate -Type Custom -Subject $subject -KeyUsage DigitalSignature -KeyAlgorithm RSA -KeyLength 3072 -HashAlgorithm SHA256 -CertStoreLocation 'Cert:\CurrentUser\My' -KeyExportPolicy NonExportable -NotAfter (Get-Date).AddMonths(6) -TextExtension @('2.5.29.37={text}1.3.6.1.5.5.7.3.3')
if (-not $cert) { throw 'Failed to create local test certificate.' }
$publicCert = Join-Path $testDir 'XELO-Link-LOCAL-TEST.cer'
Export-Certificate -Cert $cert -FilePath $publicCert -Force | Out-Null
if ($TrustForCurrentUser) {
 Import-Certificate -FilePath $publicCert -CertStoreLocation 'Cert:\CurrentUser\TrustedPeople' | Out-Null
 Write-Warning 'Trusted local test certificate in CurrentUser\TrustedPeople. Remove it after testing.'
}
$signed = Join-Path $testDir 'XELO-Link-1.0.0.0-x64-LOCAL-TEST.msix'
Copy-Item -LiteralPath $PackagePath -Destination $signed -Force
& $signTool sign /fd SHA256 /sha1 $cert.Thumbprint /s My $signed
if ($LASTEXITCODE -ne 0) { throw 'SignTool signing failed.' }
& $signTool verify /pa /v $signed
if ($LASTEXITCODE -ne 0) { Write-Warning 'Signature verification did not pass. Verify local certificate trust.' }
Write-Host "Local test MSIX: $signed"
Write-Host "Certificate thumbprint: $($cert.Thumbprint)"
Write-Host "Public certificate: $publicCert"
Write-Warning 'LOCAL TEST ONLY: Do not upload this self-signed package to Microsoft Store.'
