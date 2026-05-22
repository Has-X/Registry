# Registry

![Registry logo](docs/images/registry-logo-preview.png)

Registry is a modern Windows registry editor and CLI. It targets regedit feature parity first, then adds safer workflows such as guarded writes, export backups, a lightweight Journal for app-made changes, live monitoring, and clear 32-bit/64-bit registry view handling.

## Stack

- WinUI 3 and Fluent 2 for the desktop app.
- Windows App SDK from the official Microsoft WinUI templates.
- .NET 10 LTS for the shared engine, CLI, and tests.
- Single-project MSIX packaging for the GUI.

## Projects

- `src/Registry.App`: WinUI desktop app.
- `src/Registry.Cli`: command-line tool.
- `src/Registry.Core`: shared registry engine.
- `tests/Registry.Tests`: automated tests.

## Current Commands

```powershell
dotnet build Registry.slnx
dotnet test tests\Registry.Tests\Registry.Tests.csproj --no-build
dotnet run --project src\Registry.Cli\Registry.Cli.csproj -- roots
dotnet run --project src\Registry.Cli\Registry.Cli.csproj -- ls HKCU\Software
dotnet run --project src\Registry.Cli\Registry.Cli.csproj -- find HKCU\Software Registry --keys --names
dotnet run --project src\Registry.Cli\Registry.Cli.csproj -- export HKCU\Environment
dotnet run --project src\Registry.Cli\Registry.Cli.csproj -- export-tree HKCU\Environment
dotnet run --project src\Registry.Cli\Registry.Cli.csproj -- set-string HKCU\Software\SomeTestKey Name Data
dotnet run --project src\Registry.Cli\Registry.Cli.csproj -- set-dword HKCU\Software\SomeTestKey DwordValue 0x2a
dotnet run --project src\Registry.Cli\Registry.Cli.csproj -- rename-value HKCU\Software\SomeTestKey OldName NewName
dotnet run --project src\Registry.Cli\Registry.Cli.csproj -- rename-key HKCU\Software\SomeTestKey SomeRenamedKey
dotnet run --project src\Registry.Cli\Registry.Cli.csproj -- import C:\Path\To\file.reg
dotnet run --project src\Registry.Cli\Registry.Cli.csproj -- load-hive HKLM TempHive C:\Path\To\hive.dat
dotnet run --project src\Registry.Cli\Registry.Cli.csproj -- unload-hive HKLM\TempHive
```

## Current Status

- Implemented: browsing, favorites, local filtering, search, create/edit/rename/delete key and value flows, import/export, live monitoring, Journal restore, read-only permissions view, load/unload hive, and CLI coverage for core operations.
- Still being hardened: remote registry, permission editing, uncommon `.reg` parser edge cases, high-contrast/accessibility pass, and signed packaging/release notes.

## Documentation

- `docs/research.md`: technology and registry research log with source links.
- `docs/plan.md`: architecture, milestones, current status, and open decisions.
- `docs/performance.md`: speed architecture and remaining bottlenecks.
- `docs/launch-plan.md`: preview/full-release feature gates and test matrix.
