# Registry Plan

Registry is a modern, native Windows registry editor and CLI. The goal is drop-in regedit parity first, then safer and more powerful workflows that regedit does not provide.

## Architecture

- `Registry.App`: WinUI 3 desktop app using Fluent 2/WinUI controls, packaged as single-project MSIX.
- `Registry.Cli`: command-line companion for scripts, diagnostics, export/import, search, and core write operations.
- `Registry.Core`: shared registry engine, path parsing, view selection, value formatting, native interop, journaling, validation, import/export.
- `Registry.Tests`: fast tests for parsing, formatting, `.reg` import/export, and fixture-backed registry operations.

## Milestones

1. Foundation: solution, docs, read-only browser, CLI roots/list/get/export, build/test pipeline.
2. Browse UX: lazy registry tree, value grid, breadcrumb/address bar, refresh, copy path/data, 32/64-bit toggle. Initial implementation complete with history navigation, user-owned favorites, address suggestions, local value filtering, context menus, and status bar.
3. Editing parity: create/rename/delete keys and values, edit dialogs for each value kind, import/export `.reg`. Create/delete/rename key and create/edit/delete/rename string, expandable string, DWORD, QWORD, binary, and multi-string are implemented; app export and import preview/apply are implemented for common `.reg` value forms. Default `.reg` file association is blocked by Windows because `.reg` is reserved for system use.
4. Safety layer: backup-before-destructive-actions, explicit confirmations, clear access-denied feedback, and a lightweight Journal for app-originated changes.
5. Advanced parity: favorites, search, load/unload hive, remote registry, permissions, live monitoring.
6. Polish/release: accessibility, high contrast, keyboard parity, packaging, signed release notes, stress tests.

Detailed release gates now live in `docs/launch-plan.md`.

## Current Status

- Created the solution scaffold with official Microsoft WinUI templates.
- Installed .NET SDK `10.0.300`.
- Added `Registry.Core`, `Registry.Cli`, and `Registry.Tests`.
- Started the research log and implementation plan.
- Implemented a first read-only shared registry browser and CLI.
- Implemented a first WinUI browsing screen over standard hives.
- Verified `dotnet build` and `dotnet test`.
- Upgraded the app into a tree-and-details workbench with address navigation, value columns, basic editing, delete, export-to-clipboard, and a single explicit 32/64-bit view selector.
- Expanded CLI write/delete commands and added real HKCU-backed write tests.
- Polished the navigator with user-owned favorites in the real app sidebar, address suggestions, parent navigation, value filtering, and copy-value actions.
- Added recursive export and automatic backup files before destructive app deletes.
- Added editors and create flows for Binary, QWORD, and MultiString values plus empty-key states and lazy tree count badges.
- Moved creation actions into context menus, replaced tree count text with badges, and made tree expansion asynchronous.
- Reverted the temporary subkey-list pane after UX testing. The main page is now a classic tree + values workbench with top navigation/search and bottom status/view mode bar.
- Reduced expansion lag on large roots by enumerating subkey names only during tree expansion, paging realized tree children with `Load more...`, and using lightweight key summaries for value-pane counts; values load only for the selected key.
- Toolbar and context menus now react to the current target instead of exposing invalid value commands when no value row is selected. Value-list right-click handling is pointer-based so row menus and blank-space create menus no longer compete.
- About is a standalone lower-rail page again; Settings now focuses on configurable appearance/backdrop and safety defaults.
- Added a read-through app cache for summaries and subkey lists, debounced value filtering, and documented the speed architecture in `docs/performance.md`.
- Added an Appearance setting for toolbar alignment. Default is Left for utility/workbench ergonomics; Center and Right remain configurable.
- Added key/value rename support in the shared engine, CLI, app context menus, and `F2`.
- The left app rail starts compact by default to keep the registry workspace wider.

## Open Engineering Decisions

- Decide whether to pin Windows App SDK to the template-resolved `2.0.1` or Microsoft’s documented stable `1.8.6` if `2.0.1` behaves poorly.
- Decide how much native C++/WinRT interop is needed versus C# P/Invoke for APIs not covered by `Microsoft.Win32.RegistryKey`.
- Decide CLI command grammar before the first public release so scripts do not churn.
- 32/64-bit registry views should stay explicit for editing. A merged view is useful as a compare/unified read mode, but edits from a merged view must require choosing `32-bit` or `64-bit` because redirected keys can otherwise write to the wrong view.
- The Journal should stay lightweight. A full staged multi-edit transaction workspace is only worth adding if it reduces real risk without slowing ordinary regedit-style edits.
- Regedit parity is not complete yet. Remaining major gaps include permission editing, remote registry, stronger import edge-case coverage, and full accessibility/keyboard parity validation.
