# Installation

Download Registry from [GitHub Releases](https://github.com/Has-X/Registry/releases). Select `Registry-Setup-x64.exe` for Intel and AMD PCs, or `Registry-Setup-arm64.exe` for Windows on ARM.

## Verify before running

Registry releases include a `SHA256SUMS` file. Download the installer and checksum file from the same release, then calculate the installer hash:

```powershell
Get-FileHash .\Registry-Setup-x64.exe -Algorithm SHA256
```

Compare the result with the matching entry in `SHA256SUMS`.

Registry is presently unsigned because Chromatic does not use a code-signing certificate. Windows may show a SmartScreen warning. Do not bypass a warning for a file obtained outside the official release page or when the checksum does not match.

## What setup changes

- Installs Registry for the current user.
- Creates a Start menu shortcut.
- Registers Registry in `.reg` Open With and Default Apps surfaces, without forcing it to become the default application.
- Adds `registry` and `registry-cli` to the current user's `PATH` through a dedicated `Chromatic\bin` shim directory.

Close and reopen a terminal after installation before using `registry` or `registry-cli`. The desktop application remains available from Start as **Registry**.

## Portable archive

The `Registry-x64.zip` and `Registry-arm64.zip` archives contain the same self-contained app and CLI files without an installer. They do not create a Start menu shortcut, register `.reg` files, or add commands to `PATH`. Extract the archive to a trusted folder and run `Registry.App.exe` or `Registry.Cli.exe` directly.

## Uninstalling

Uninstall Registry from Windows Settings or the Start menu entry. The uninstaller removes its command shims and its own user-PATH entry. It does not delete exported `.reg` files, user documents, or data belonging to other applications.
