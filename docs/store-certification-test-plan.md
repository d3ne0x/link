# XELO Link v1.0.0 — Windows App Certification Kit checklist

## Prerequisites
- Windows 10/11 x64 with Windows SDK and Windows App Certification Kit installed.
- Build the release EXE and MSIX using `scripts/Build-Release.ps1` and `scripts/Build-StoreMsix.ps1`.
- Run `scripts/Test-StoreMsix.ps1` and `scripts/Test-StoreReadiness.ps1`.
- Sign a **copy** of the unsigned package with `scripts/Sign-LocalTestMsix.ps1 -TrustForCurrentUser`; the local test certificate must be trusted on the test machine before installation. Never use the local test certificate for Store submission.
- Install using `scripts/Install-LocalTestMsix.ps1`.

## Windows App Certification Kit
1. Open the Windows App Certification Kit (`appcert.exe`) from the Windows SDK.
2. Select **Validate a Windows app** (wording may vary with SDK version).
3. Select the installed **XELO Link** package, not the unpackaged executable.
4. Run all applicable automated tests. Save the complete results and report.
5. Resolve each failure; do not interpret a successful MSIX build as certification approval.
6. Rebuild, reinstall, and rerun certification after any changes.

## Installed-app regression checks
1. Start XELO Link from Start; verify there is no elevation prompt.
2. With Developer Mode enabled, create file and directory symbolic links in a disposable test directory.
3. With Developer Mode disabled and without the symlink privilege, confirm a failed symbolic-link operation provides actionable guidance; re-enable Developer Mode afterwards.
4. Create a hard link to a file on the same volume.
5. Create a directory junction.
6. Browse/scan existing links. **Do not expect all hard links to appear in a reparse-point scan.**
7. Delete only test links; verify target files and folders remain intact.
8. Close and relaunch the application; verify no startup errors.
9. Uninstall the package and confirm it disappears from installed apps.

## Submission artifacts
- Approved executable icon and MSIX tiles; replace all provisional art.
- Real app screenshots at Store-supported resolutions.
- Product description, category, age rating, support contact, privacy declarations and any required URLs.
- Unsigned Store-bound MSIX with Partner Center identity `XELOLabs.XELOLink` and publisher `CN=DCC526A0-4D1E-44E7-8949-898FE66548B5`.
- Certification test report and installed-app regression results.

## Local signing trust cleanup
The local test signer creates a new certificate in `CurrentUser\My` and optionally imports its public certificate into `CurrentUser\TrustedPeople`. Self-signed certificates may additionally require explicit `CurrentUser\Root` trust for local installation. Only perform this on an isolated development PC after verifying the thumbprint. Remove test certificates from all trust stores after testing; do not distribute the self-signed MSIX or private key.
