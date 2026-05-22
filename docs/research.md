# Registry Research Log

Last updated: 2026-05-19.

## Current platform choices

- UI framework: WinUI 3, because Microsoft describes it as the modern native Windows desktop UI framework delivered with the Windows App SDK, with Fluent design controls and high-DPI desktop behavior. Source: https://learn.microsoft.com/en-us/windows/apps/winui/winui3/
- Design language: Fluent 2 for Windows, using built-in WinUI iconography and controls instead of custom icon sets. Source: https://fluent2.microsoft.design/components/windows
- App SDK packaging: single-project MSIX for the GUI, because Microsoft documents it as the current clean packaging model for WinUI apps. Source: https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/single-project-msix
- Distribution: MSIX/Store-ready for the GUI, direct self-contained publish for CLI when useful. Microsoft’s publishing guidance recommends Store/MSIX for WinUI apps and calls out signing/update benefits. Source: https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/publish-first-app
- Runtime: .NET 10 LTS for shared engine and CLI. Microsoft lists .NET 10 as LTS supported until November 2028. Source: https://learn.microsoft.com/en-us/dotnet/core/releases-and-support

## Registry facts that shape the product

- Registry access must handle 32-bit and 64-bit views explicitly. Microsoft documents that 32-bit keys are surfaced under `HKEY_LOCAL_MACHINE\Software\WOW6432Node` in the 64-bit Registry Editor view. Source: https://learn.microsoft.com/en-us/troubleshoot/windows-client/performance/view-system-registry-with-64-bit-windows
- Live refresh can use native change notifications via `RegNotifyChangeKeyValue`. Source: https://learn.microsoft.com/en-us/windows/win32/api/winreg/nf-winreg-regnotifychangekeyvalue
- The Win32 registry API surface is broader than simple read/write and includes functions for save/load, notifications, remote connections, transactions, security, and value/key operations. Source: https://learn.microsoft.com/en-us/windows/win32/sysinfo/registry-functions
- Registry virtualization can redirect writes to per-user virtual storage and present merged reads. A replacement editor should expose this clearly instead of hiding it. Source: https://learn.microsoft.com/en-us/windows/win32/sysinfo/registry-virtualization
- Transacted registry APIs exist for creating/opening keys under a transaction, but Microsoft notes that full system backup/restore scenarios should use Volume Shadow Copy Service rather than registry functions alone. Source: https://learn.microsoft.com/en-us/windows/win32/api/winreg/nf-winreg-regcreatekeytransacteda
- Packaged Windows apps can register file type associations in the package manifest using the `windows.fileTypeAssociation` extension and `uap:FileTypeAssociation`. Source: https://learn.microsoft.com/en-us/uwp/schemas/appxpackage/uapmanifestschema/element-uap-filetypeassociation
- Windows App SDK/WinUI apps can inspect activation arguments and handle file activation for registered file extensions. Source: https://learn.microsoft.com/en-us/windows/apps/develop/launch/handle-file-activation
- Windows package registration rejects `.reg` association for this app because `.reg` is reserved for system use. Registry therefore supports in-app `.reg` import rather than default file association.
- Alternate handlers can still be registered per-user under `HKCU\Software\Classes\.reg\OpenWithProgids` and `HKCU\Software\Classes\Applications\<exe>\SupportedTypes`, which adds the app to Open with without taking over default `.reg` import behavior.

## Regedit parity target

- Tree browsing for all standard hives.
- Search by key, value name, value data, whole string, and match case.
- Create, rename, delete keys and values.
- Edit all standard value types: string, expandable string, multi-string, binary, DWORD, QWORD.
- Import/export `.reg`.
- Load/unload hives.
- Connect/disconnect network registry.
- Permissions editor entry points and ownership visibility.
- Favorites/bookmarks.
- Copy key path and value data.

## Improvements over regedit

- First-class 32-bit/64-bit view toggle and side-by-side compare.
- Shadow edits: stage changes in a local journal, preview `.reg` output, validate permissions, then commit.
- Undo history for committed changes where enough old data was captured.
- Live change monitor with clear external-change notices.
- Diff keys/hives/views.
- Safer destructive operations with export-before-delete snapshots.
- Virtualization visibility for per-user redirected keys.
- Admin/elevation boundary made explicit, with per-operation elevation where feasible.
- CLI backed by the same core engine as the GUI.
- Test hives and fixture-based regression tests before touching real system keys.
