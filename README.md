<p align="center">
  <img src="assets/background-rounded.png" alt="Registry workspace with registry cubes and inspection tools" width="100%">
</p>

<h1 align="center"><img src="assets/registry-logo.png" alt="Registry logo" width="46" height="46" align="absmiddle"> Registry — a modern Windows Registry Editor</h1>

<p align="center">
  <img src="https://img.shields.io/badge/platform-Windows%2010%20%7C%20Windows%2011-0078D4" alt="Windows 10 and Windows 11">
  <img src="https://img.shields.io/badge/architecture-x64%20%7C%20ARM64-0078D4" alt="x64 and ARM64">
  <img src="https://img.shields.io/badge/UI-WinUI%203%20%7C%20Windows%20App%20SDK-0078D4" alt="WinUI 3 and Windows App SDK">
  <img src="https://img.shields.io/badge/security-CodeQL%20Advanced-2EA44F" alt="CodeQL Advanced">
  <img src="https://img.shields.io/badge/Made%20in-the%20EU%20%F0%9F%87%AA%F0%9F%87%BA-003399" alt="Made in the EU 🇪🇺">
  <img src="https://img.shields.io/badge/license-AGPL--3.0--or--later-000000" alt="AGPL 3.0 or later">
</p>

Registry is a native Windows registry editor for browsing, editing, importing, exporting, and checking registry data with a familiar Windows feel. It combines the workflows people expect from `regedit` with explicit 32-bit and 64-bit views, `.reg` import previews, export-before-change safeguards, and a Journal of app-originated changes.

Developed and published by [Chromatic](https://chromatic.hu). Send product feedback to [feedback@chromatic.hu](mailto:feedback@chromatic.hu).

> [!CAUTION]
> The Windows registry controls core system and application behaviour. Export a key before editing it, verify the selected architecture view, and never import an untrusted `.reg` file. Journal snapshots are a convenience recovery aid, not a replacement for a system backup.

## Install

Download the current release from [GitHub Releases](https://github.com/Has-X/Registry/releases):

- `Registry-Setup-x64.exe` for Intel and AMD PCs;
- `Registry-Setup-arm64.exe` for Windows on ARM;
- `Registry-x64.zip` and `Registry-arm64.zip` for portable use.

The installer follows the Windows light or dark mode, registers Registry for `.reg` files, and provides `registry` plus `registry-cli` in the current user's `PATH`. Open a new terminal after installation. Releases include `SHA256SUMS` checksums.

Registry is currently unsigned because Chromatic does not use a code-signing certificate. Windows may show a SmartScreen warning; download only from the release page and verify the published checksum before running an installer.

## What Registry can do

- Browse local registry hives in a compact WinUI 3 interface.
- Create, edit, rename, copy, and delete keys and values.
- Switch deliberately between 32-bit and 64-bit registry views.
- Find key names, value names, and value data from a chosen root.
- Preview and apply `.reg` imports; export keys and subtrees back to `.reg` files.
- Edit string, expandable string, DWORD, QWORD, binary, and multi-string values.
- Save favorites, inspect changes in Journal, and restore app-created snapshots.
- Inspect ownership and access rules where Windows permits it.
- Automate browsing, search, import, export, and supported writes with `registry-cli`.

## Quick start

Open Registry and start under a disposable path such as `HKEY_CURRENT_USER\Software\RegistryPreview`. Before changing anything, export the key and check the 32-bit/64-bit view selector.

To import a `.reg` file, open it with Registry. The app shows an import preview before it applies operations. The installer registers Registry with the Windows Open With and Default Apps surfaces without taking over the user's existing default application.

The CLI uses the same core engine:

```powershell
registry-cli roots
registry-cli ls HKCU\Software
registry-cli find HKCU\Software Registry --keys --names
registry-cli export-tree HKCU\Environment
```

Run `registry-cli help` for the full command reference. Write, import, and hive commands modify machine state; try them only under a disposable `HKCU\Software` path first.

## Documentation

- [Registry Wiki](https://github.com/Has-X/Registry/wiki) — installation, safe editing, imports, Journal recovery, CLI, and architecture.
- [Contributing guide](CONTRIBUTING.md) — development and pull-request expectations.
- [Security policy](SECURITY.md) — report vulnerabilities through [private GitHub security advisories](https://github.com/Has-X/Registry/security/advisories/new), never public issues.
- [Support guide](SUPPORT.md) — include the affected path, architecture view, and safe reproduction details.

## Build from source

Requirements: Windows 10 or Windows 11, the .NET 10 SDK, and Windows App SDK development prerequisites.

```powershell
dotnet restore Registry.slnx --runtime win-x64
dotnet build Registry.slnx --configuration Release --no-restore
dotnet test tests\Registry.Tests\Registry.Tests.csproj --configuration Release --no-build
```

For native x64 and ARM64 installer packaging, see [installer/README.md](installer/README.md). Tagged releases run CI, CodeQL Advanced, native publish, Inno Setup packaging, and SHA-256 checksum generation.

## License

Copyright (C) 2026 Chromatic and contributors. Registry is licensed under the [GNU Affero General Public License v3.0 or later](LICENSE.md), SPDX identifier `AGPL-3.0-or-later`.
