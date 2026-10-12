# Certification A/B comparison — WACK 2026-10-12

## Baseline (single-file)
Uploaded WACK report overall WARNING. One optional test failed: Blocked executables (CreateProcessW, ShellExecuteW, and executable-name references including bash, REG, cmd and MSBuild). DPI-awareness test WARNING: scanner failed to process Link.exe and reported not DPI aware. Package manifest, UAC and branding tests passed.

These are **scanner findings**, not evidence that XELO Link actually launches those executables. The original WPF app.manifest already declares PerMonitor DPI awareness. Avoid changing the manifest and publishing format simultaneously: first isolate the bundling hypothesis.

## Reproduce baseline (optional)
```powershell
cd C:\Source\link
.\scripts\Build-Release.ps1 -BuildVariant SingleFile
.\scripts\Build-StoreMsix.ps1
.\scripts\Test-StoreMsix.ps1
```

## Certification variant (non-single-file, self-contained)
```powershell
cd C:\Source\link
git pull --ff-only
.\scripts\Build-Release.ps1 -BuildVariant Certification
Get-ChildItem .\artifacts\publish\win-x64 | Measure-Object
.\scripts\Build-StoreMsix.ps1
.\scripts\Test-StoreMsix.ps1
.\scripts\Test-StoreReadiness.ps1
Get-AppxPackage -Name XELOLabs.XELOLink | Remove-AppxPackage
.\scripts\Sign-LocalTestMsix.ps1 -TrustForCurrentUser
.\scripts\Install-LocalTestMsix.ps1
```

If signing reports 0x800B0109, follow the printed trust instructions on your isolated development PC only, then rerun the installer. Do not distribute the local-test MSIX.

## WACK run
Run appcert.exe > Validate a Windows app > installed XELO Link. Save the new XML report with a different name. Compare:
- Package sanity > Blocked executables: FAIL vs PASS/FAIL
- High-DPI support > DPIAwarenessValidation: WARNING vs PASS/WARNING
- Overall status
- App launch, file/folder symbolic links, hard links, junctions, scan, and safe deletion

## Interpretation
If both findings disappear, prefer the non-single-file MSIX publish for Store submission. If blocked executable references remain, inspect the precise binary and distinguish framework API imports from actual app process launches; do not suppress security checks. If DPI warning remains, inspect the **published** Link.exe embedded manifest and verify actual process DPI awareness before modifying DPI settings.

This branch is experimental; do not merge or submit until results are reviewed.
