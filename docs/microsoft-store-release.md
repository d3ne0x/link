# XELO Link — Microsoft Store v1.0.0 release checklist

## Product
- Name: XELO Link
- Publisher display name: XELO Labs
- Package identity: XELOLabs.XELOLink
- Publisher certificate subject: CN=DCC526A0-4D1E-44E7-8949-898FE66548B5
- Version: 1.0.0.0
- Architecture: x64
- Category suggestion: Developer tools
- Price: Free

## Store short description
Create and manage Windows symbolic links, hard links and directory junctions with a straightforward desktop interface.

## Store description
XELO Link helps you create and manage Windows filesystem links without memorising command-line syntax.

Choose between symbolic links, hard links and directory junctions in a guided workflow. Select a source and destination, review your choices and create the link. Browse existing links and remove links you no longer need.

Supported link types:
- Symbolic links: files and directories, subject to Windows privileges or Developer Mode.
- Hard links: files only, on the same NTFS volume.
- Directory junctions: directories only.

Important: Deleting a link should not delete its target. Always review the selected item before deleting it. Windows permissions and filesystem limitations apply.

## Release gates
- [x] .NET Release build and self-contained executable built successfully on Windows.
- [x] Unpackaged app link operations user-tested.
- [x] MakeAppx generated an unsigned MSIX.
- [x] Debug symbols excluded from MSIX.
- [ ] Run scripts/Test-StoreMsix.ps1 successfully.
- [ ] Replace provisional tile art and legacy executable icon with final approved XELO brand assets.
- [ ] Confirm Partner Center package identity matches exactly.
- [ ] Test *installed MSIX* launch, create, scan, delete, update and uninstall.
- [ ] Confirm link creation privilege/Developer Mode guidance from packaged app.
- [ ] Run Windows App Certification Kit and resolve issues.
- [ ] Capture real screenshots of current app at production resolution.
- [ ] Provide support contact, privacy statement/URL if required by Partner Center, age ratings and accessibility disclosures.
- [ ] Submit the package through Partner Center; review certification feedback.

## Notes
Do not treat successful MakeAppx output as installation or Store certification. The generated package is unsigned. A local sideload test requires a valid trusted code-signing certificate with matching publisher subject; Store distribution uses the Partner Center workflow. Do not upload private signing keys to GitHub.
Hard links are not filesystem reparse points, so a reparse-point-only scan does not discover all hard links. Avoid claiming complete hard-link discovery.
