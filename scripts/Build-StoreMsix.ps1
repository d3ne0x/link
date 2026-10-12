[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$stage = Join-Path $root 'artifacts\msix\staging'
$out = Join-Path $root 'artifacts\msix\XELO-Link-1.0.0.0-x64.msix'
$publish = Join-Path $root 'artifacts\publish\win-x64'
$exe = Join-Path $publish 'Link.exe'
if (-not (Test-Path $exe)) { throw 'Run .\scripts\Build-Release.ps1 before packaging.' }
$kit = Join-Path $env:ProgramFiles 'Windows Kits\10\bin'
if (-not (Test-Path $kit)) { $kit = Join-Path ([Environment]::GetFolderPath('ProgramFilesX86')) 'Windows Kits\10\bin' }
$makeappx = Get-ChildItem $kit -Filter MakeAppx.exe -Recurse -ErrorAction SilentlyContinue |
 Where-Object { $_.FullName -match '\\x64\\MakeAppx.exe$' } |
 Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
if (-not $makeappx) { throw 'Install Windows 10/11 SDK (MakeAppx.exe required).' }
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
New-Item -ItemType Directory -Path (Join-Path $stage 'Assets') -Force | Out-Null
Get-ChildItem -LiteralPath $publish -Force | Where-Object { $_.Extension -notin @('.pdb', '.xml') } | Copy-Item -Destination $stage -Recurse -Force
Copy-Item (Join-Path $root 'packaging\AppxManifest.xml') (Join-Path $stage 'AppxManifest.xml')
# Provisional brand tiles: replace with approved XELO artwork before submission.
Add-Type -AssemblyName System.Drawing
foreach ($item in @(@('StoreLogo.png',50),@('Square150x150Logo.png',150),@('Square44x44Logo.png',44))) {
 $name = [string]$item[0]; $size = [int]$item[1]
 $bmp = New-Object System.Drawing.Bitmap($size,$size)
 $g = [System.Drawing.Graphics]::FromImage($bmp)
 try {
  $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
  $g.Clear([System.Drawing.Color]::FromArgb(16,63,124))
  $pen = New-Object System.Drawing.Pen([System.Drawing.Color]::White,[Math]::Max(3,$size * 0.105))
  try {
   $g.DrawLine($pen,$size*0.24,$size*0.22,$size*0.76,$size*0.78)
   $g.DrawLine($pen,$size*0.76,$size*0.22,$size*0.24,$size*0.78)
  } finally { $pen.Dispose() }
  $bmp.Save((Join-Path $stage "Assets\$name"),[System.Drawing.Imaging.ImageFormat]::Png)
 } finally { $g.Dispose(); $bmp.Dispose() }
}
if (Test-Path $out) { Remove-Item $out -Force }
& $makeappx pack /d $stage /p $out /o
if ($LASTEXITCODE -ne 0) { throw 'MakeAppx packaging failed.' }
Write-Host "Unsigned MSIX ready: $out"
Write-Warning 'Unsigned MSIX is not directly installable. Store submission/signing and certification remain.'
Write-Warning 'Replace provisional brand tiles with approved XELO assets before release.'
