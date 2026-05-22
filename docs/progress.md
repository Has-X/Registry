# Progress Log

## 2026-05-19

- Confirmed the workspace started empty and was not a git repository.
- Researched current Microsoft guidance for WinUI 3, Windows App SDK packaging, .NET support, registry APIs, 32/64-bit registry views, virtualization, notifications, and transacted registry APIs.
- Installed .NET SDK `10.0.300` because only runtimes were present.
- Installed official `Microsoft.WindowsAppSDK.WinUI.CSharp.Templates`.
- Created a .NET 10 solution with WinUI app, CLI, shared core, and xUnit tests.
- Implemented shared registry path parsing and read-only key browsing.
- Implemented CLI commands: `roots`, `ls`, `get`, and `export`.
- Replaced the WinUI template home page with a first registry browsing screen backed by `Registry.Core`.
- Added an early Safety page for backups and virtualization warnings.
- Added research, plan, README, and this progress log.

## Verification

- `dotnet build Registry.slnx`: passed.
- `dotnet test tests\Registry.Tests\Registry.Tests.csproj --no-build`: passed, 4 tests.
- CLI smoke tests:
  - `roots`: listed standard hives.
  - `ls HKCU\Software`: listed subkeys read-only.
  - `export HKCU\Environment`: produced `.reg`-style output.

## 2026-05-19 Follow-up

- Replaced the simplified root/value lists with a regedit-style workbench:
  - left lazy-loading registry key tree,
  - address bar navigation,
  - 32-bit/64-bit registry view selector,
  - right-side value table with name, type, and data columns,
  - status bar for success, warning, and access-denied feedback.
- Removed the fake Safety navigation item. Safety is now represented by explicit write commands, protected root handling, access-denied messaging, and export-to-clipboard backup snapshots.
- Added app commands for creating keys, deleting keys, creating string values, creating DWORD values, editing string/DWORD values, deleting values, copying paths, and exporting selected keys as `.reg` text.
- Added shared core write operations: create subkey, delete subkey tree, set value, delete value, parent/leaf path helpers.
- Expanded CLI commands with `set-string`, `set-dword`, `delete-value`, `create-key`, and `delete-key`.
- Added HKCU-backed tests for real write/read/delete behavior.

## Follow-up Verification

- `dotnet build Registry.slnx --no-restore`: passed.
- `dotnet test tests\Registry.Tests\Registry.Tests.csproj --no-build`: passed, 6 tests.
- CLI disposable-key smoke test passed: create key, set string value, get string value, delete key.

## 2026-05-19 Navigation Polish

- Made the side pane useful instead of only a key tree:
  - pinned registry locations for startup entries, uninstall entries, policies, file associations, and environment,
  - persisted user favorites,
  - session recents,
  - lazy-loading full hive tree below those shortcuts.
- Added favorite management:
  - `Favorite` command saves the current key,
  - favorites survive app restarts through local app settings,
  - selected favorites can be removed.
- Added better navigation:
  - parent-key button,
  - address suggestions from roots, pinned locations, favorites, and recents,
  - quick click navigation from pinned/favorite/recent lists.
- Improved the value pane:
  - filter box for values by name, type, or data,
  - filtered count feedback,
  - copy selected value data.

## Navigation Polish Verification

- `dotnet build Registry.slnx --no-restore`: passed.
- `dotnet test tests\Registry.Tests\Registry.Tests.csproj --no-build`: passed, 6 tests.

## 2026-05-19 Layout Correction

- Removed the duplicated shortcut/favorites navigator from inside the Registry page content.
- Moved pinned quick-jump registry locations into the real app NavigationView sidebar.
- Restored the Registry page to a cleaner two-pane editor layout: registry key tree on the left, values/details on the right.
- Left the address suggestions and value filtering in place.

## Layout Correction Verification

- `dotnet build Registry.slnx --no-restore`: passed.
- `dotnet test tests\Registry.Tests\Registry.Tests.csproj --no-build`: passed, 6 tests.

## 2026-05-19 Favorites and Backup Pass

- Removed hardcoded permanent sidebar pins.
- Kept favorites, but made them user-owned:
  - pressing `Favorite` stores the current key,
  - stored favorites appear in the app sidebar,
  - favorites persist through app local settings,
  - address suggestions include user favorites and session recents.
- Added recursive `.reg` export support in `Registry.Core`.
- Added CLI `export-tree <key>` for full subtree export.
- Added automatic `.reg` backup files before destructive app deletes:
  - deleting a key writes a recursive backup,
  - deleting a value writes a backup of the containing key,
  - backups are stored under the app local data `Backups` folder.
- Added test coverage for nested recursive export.

## Favorites and Backup Verification

- `dotnet build Registry.slnx --no-restore`: passed.
- `dotnet test tests\Registry.Tests\Registry.Tests.csproj --no-build`: passed, 7 tests.

## 2026-05-19 Value Editor Polish

- Removed the "editor not implemented for this type" dead end.
- Added editors for existing values:
  - string,
  - expandable string,
  - DWORD,
  - QWORD,
  - binary as editable hex bytes,
  - multi-string as one line per string.
- Added create actions for QWORD, binary, and multi-string values.
- Added an empty-values panel so empty keys and empty filters no longer look broken.
- Added lazy key count badges when tree nodes are expanded, showing loaded subkey/value counts.
- Tightened the parent-key button sizing.
- Added test coverage for Binary, QWORD, and MultiString round-trip/export formatting.

## Value Editor Polish Verification

- `dotnet test tests\Registry.Tests\Registry.Tests.csproj`: passed, 8 tests.
- `dotnet build Registry.slnx --no-restore`: passed.

## 2026-05-19 Toolbar and Tree Polish

- Moved create/delete key and create value actions out of the main toolbar and into the key tree context menu.
- Added a value row context menu for edit, copy data, and delete.
- Replaced inline count text in tree labels with compact badges:
  - neutral badge for subkey count,
  - accent badge for value count.
- Tree count badges appear after a node is expanded and the key snapshot is loaded.
- Made tree expansion asynchronous so registry reads happen off the UI thread and large keys feel less blocking.
- Reduced the parent-key button size so it no longer visually dominates the address bar.

## Toolbar and Tree Polish Verification

- `dotnet build Registry.slnx --no-restore`: passed.
- `dotnet test tests\Registry.Tests\Registry.Tests.csproj --no-build`: passed, 8 tests.

## 2026-05-19 Crash Fix

- Investigated the startup crash from Windows Application event logs:
  - faulting app: `Registry.App.exe`,
  - faulting module: `Microsoft.UI.Xaml.dll`,
  - exception code: `0xc000027b`.
- Replaced the new `TreeView.ItemTemplate` badge implementation with direct per-node content rendering.
- Kept compact count badges, but avoided the fragile TreeView template path that caused the WinUI startup crash.
- Added a launch smoke test: app stayed alive for 8 seconds with no new crash event.

## Crash Fix Verification

- `dotnet build Registry.slnx --no-restore`: passed.
- Launch smoke test: passed.
- `dotnet test tests\Registry.Tests\Registry.Tests.csproj --no-build`: passed, 8 tests.

## 2026-05-19 Startup Crash Stabilization

- The app still crashed with the same WinUI native failure after the first crash fix.
- Removed the remaining risky TreeView custom visuals and reverted tree node content to plain text.
- Removed `MenuFlyoutItem` icon shorthand from context menus, keeping the context menu behavior with safer XAML.
- Kept async tree expansion and context menu actions.
- Launch test now checks for fresh `Registry.App` crash events after launch, not only whether `dotnet run` remains alive.

## Startup Crash Stabilization Verification

- `dotnet build Registry.slnx --no-restore`: passed.
- Strict launch smoke test: app stayed alive for 12 seconds and produced 0 fresh crash events.
- `dotnet test tests\Registry.Tests\Registry.Tests.csproj --no-build`: passed, 8 tests.

## 2026-05-19 Startup Crash Root Cause

- Added temporary startup breadcrumbs to isolate the crash.
- Found the crash happened in `LoadRoots` at root selection.
- Root cause: after reverting TreeView content from `RegistryTreeItem` objects to plain text, one remaining startup line still cast `TreeViewNode.Content` back to `RegistryTreeItem`.
- Fixed the stale cast by reading the selected root from the `_treeItems` node map instead.
- Deferred root loading until `HomePage.Loaded`, which avoids mutating `TreeView.RootNodes` during page construction.
- Removed temporary startup tracing after verification.

## Startup Crash Root Cause Verification

- `dotnet build Registry.slnx --no-restore`: passed.
- `dotnet test tests\Registry.Tests\Registry.Tests.csproj --no-build`: passed, 8 tests.
- Final launch check: `Registry.App.exe` was running after 10 seconds with 0 fresh crash events.

## 2026-05-19 Binary Editor and View Model Decision

- Improved the binary value editor:
  - uses a monospace hex editor,
  - normalizes bytes into 16-byte rows,
  - shows live byte count,
  - includes a Normalize action,
  - explains accepted hex input.
- Recorded the 32/64-bit view decision:
  - keep explicit 32-bit and 64-bit edit targets,
  - add a future merged/compare read mode that tags values as 32-bit only, 64-bit only, same, or different,
  - never make merged editing implicit.

## 2026-05-19 Layout Guide Implementation

- Reworked the Registry page around the supplied Windows-tool layout guide:
  - top row now holds Back, Forward, Up, address/breadcrumb entry, and local value filter,
  - command bar is limited to common actions instead of creation clutter,
  - middle area now has a registry tree, a subkey list, and a value list,
  - bottom area is a real status bar with selection/status text and explicit 32/64-bit view selector.
- Added navigation history:
  - Back,
  - Forward,
  - Up one level.
- Added a middle subkey list:
  - single click previews subkey values on the right,
  - double click navigates into the subkey.
- Added safe programmatic context menus:
  - tree/key menu for new key, new values, copy path, export, delete,
  - subkey row menu for open, copy path, export,
  - value row menu for edit, copy name, copy data, copy `reg add` command, delete.
- Status behavior now reports selected key/value, loaded path, copied commands, and current view mode.

## Layout Guide Verification

- `dotnet build Registry.slnx --no-restore`: passed.
- `dotnet test tests\Registry.Tests\Registry.Tests.csproj --no-build`: passed, 8 tests.
- Real launch check: `Registry.App.exe` was running after 10 seconds with 0 fresh crash events.

## 2026-05-19 Two-Pane Revert and Expansion Performance

- Reverted the temporary three-pane registry/subkey/value layout after hands-on UX feedback.
- Restored the classic workbench shape:
  - tree of hives/keys on the left,
  - value table/details on the right,
  - navigation, address, local filter, and commands above,
  - status/view mode below.
- Removed the unused middle-pane `RegistryKeyRow` model.
- Reduced lag when expanding large roots such as `HKEY_CLASSES_ROOT`:
  - tree expansion now reads subkey names only,
  - value enumeration is deferred until the key is selected,
  - very large child lists are inserted into the tree in UI-yielding chunks,
  - tree labels show a compact `N keys` count after expansion instead of expensive value counts.

## Two-Pane Revert Verification

- `dotnet build Registry.slnx --no-restore`: passed.
- `dotnet test tests\Registry.Tests\Registry.Tests.csproj --no-build`: passed, 8 tests.
- Launch check: packaged `Registry.App.exe` was running with no fresh crash events.

## 2026-05-19 Reactive Toolbar and Lazy Summary Pass

- Added a lightweight key summary read for the GUI:
  - uses registry `SubKeyCount` / `ValueCount` instead of enumerating and sorting subkey names during value-pane refresh,
  - still reads values for the selected key,
  - runs refresh work off the UI thread and ignores stale refresh results if navigation moved on.
- Improved large-tree expansion:
  - leaf nodes now drop their unrealized-child arrow after expansion returns no children,
  - large child batches still yield back to the UI while inserting rows.
- Made the toolbar reactive:
  - value actions such as Edit, Delete, and Copy value are enabled only when a value row is selected,
  - key actions such as Refresh, Favorite, Copy path, and Export are enabled only when a key is loaded.
- Replaced the loose `0 keys · 2 values` text with compact count pills:
  - `No subkeys`,
  - `1 value`,
  - filtered value counts as `N / total shown`.
- Fixed context-menu targeting:
  - right-clicking a value row selects that row and shows value actions,
  - right-clicking empty value-list space clears value selection and shows key/new-value actions,
  - right-clicking a tree row targets that key before building the menu.
- Moved transient status `InfoBar` above the bottom status strip so warning text no longer overlaps the view selector.

## Reactive Toolbar Verification

- `dotnet build Registry.slnx --no-restore`: passed.
- `dotnet test tests\Registry.Tests\Registry.Tests.csproj --no-build`: passed, 9 tests.
- Launch check: packaged `Registry.App.exe` was running after 12 seconds with 0 fresh crash events.

## 2026-05-19 Root Expansion Paging

- Changed tree expansion from "realize every child row" to paged realization:
  - root/key expansion now adds the first 300 children,
  - a `Load more...` row appends the next page on demand,
  - huge roots such as `HKEY_CLASSES_ROOT` no longer force thousands of WinUI tree items into the visual tree at once.
- Kept the lightweight key summary path for value-pane refreshes.
- Split context menus more strictly:
  - tree/key context menus show key actions,
  - value-row context menus show value actions only,
  - blank value-list context menus show create/copy/export actions for the loaded key.
- Updated the binary editor helper text to use a native-looking subtle bordered info strip instead of plain loose text.

## Root Paging Verification

- `dotnet build Registry.slnx --no-restore`: passed.
- `dotnet test tests\Registry.Tests\Registry.Tests.csproj --no-build`: passed, 9 tests.
- Launch check: packaged `Registry.App.exe` was running after 12 seconds with 0 fresh crash events.

## 2026-05-19 Favorites Rail Page

- Added a real Favorites destination to the left navigation rail.
- Added a Favorites page that lists user-saved keys with:
  - Open,
  - Copy path,
  - Remove.
- Favorites are still user-owned local app data, not hardcoded pins.
- Clicking a favorite navigates back to the Registry page and opens that key.
- The Favorite command in the registry toolbar now gives clear feedback and points users to the Favorites rail item.

## Favorites Rail Verification

- `dotnet build Registry.slnx --no-restore`: passed with 0 warnings.
- `dotnet test tests\Registry.Tests\Registry.Tests.csproj --no-build`: passed, 9 tests.
- Launch check: packaged `Registry.App.exe` was running after 12 seconds with 0 fresh crash events.

## 2026-05-19 Favorites Navigation Fix

- Fixed favorite opening from a fresh Registry page:
  - `NavigateTo` now stores the requested path if the tree has not loaded yet,
  - `LoadRoots` consumes that pending path instead of selecting `HKEY_CLASSES_ROOT`.
- Polished the Favorites page row actions:
  - the favorite icon sits in a subtle square affordance,
  - Open, Copy, and Remove now have text labels,
  - Open is visually promoted as the primary row action.

## Favorites Navigation Fix Verification

- `dotnet build Registry.slnx --no-restore`: passed with 0 warnings.
- `dotnet test tests\Registry.Tests\Registry.Tests.csproj --no-build`: passed, 9 tests.
- Launch check: packaged `Registry.App.exe` was running after 12 seconds with 0 fresh crash events.

## 2026-05-19 Performance Architecture Pass

- Added `RegistryReadCache` as a read-through app cache for hot browsing paths:
  - key summaries,
  - subkey-name lists,
  - view-specific cache keys.
- Replaced direct background registry reads in the UI with cached async reads.
- Reduced tree page size from 300 to 120 realized rows for faster first paint on huge roots.
- Added debounced value filtering so typing does not rebuild the value list on every keystroke.
- Writes now clear cached reads and reset the realized branch for the current key.
- 32/64-bit view changes clear the cache and rebuild the tree while preserving the current path.
- Added [performance.md](performance.md) to document the current speed architecture, remaining bottlenecks, and next optimization targets.

## Performance Architecture Verification

- `dotnet build Registry.slnx --no-restore`: passed with 0 warnings.
- `dotnet test tests\Registry.Tests\Registry.Tests.csproj --no-build`: passed, 9 tests.
- Launch check: packaged `Registry.App.exe` was running after 12 seconds with 0 fresh crash events.

## 2026-05-19 Toolbar Alignment and Binary Editor Polish

- Fixed the binary editor helper strip:
  - simplified the helper copy,
  - rebuilt the icon/text row so wrapping is stable,
  - made the strip stretch cleanly across the dialog content.
- Added persisted toolbar alignment settings:
  - Left,
  - Center,
  - Right.
- Chose Left as the default toolbar alignment because this is a dense desktop tool and the primary scan path starts at the left tree/content edge.
- Settings changes apply live to an open Registry page through `AppSettings.Changed`.

## Toolbar Alignment Verification

- `dotnet build Registry.slnx --no-restore`: passed with 0 warnings.
- `dotnet test tests\Registry.Tests\Registry.Tests.csproj --no-build`: passed, 9 tests.
- CLI scratch-path read still works.
- Launch check: packaged `Registry.App.exe` was running after 12 seconds with 0 fresh crash events.

## 2026-05-19 Value Pane Header and Type Filtering

- Wrapped the selected registry path in a compact header pill so it reads like part of the value pane instead of loose text.
- Added a value type filter next to the text filter:
  - All types,
  - String,
  - ExpandString,
  - DWord,
  - QWord,
  - Binary,
  - MultiString.
- Combined text and type filters in the value list.
- Moved value-row context menus onto the row template itself:
  - right-clicking an entry now shows Edit/Copy/Delete value actions,
  - right-clicking empty value-list space still shows New value/key-scope actions.
- Removed the old fuzzy value-row hit-test helper.

## Value Pane Header Verification

- `dotnet build Registry.slnx --no-restore`: passed with 0 warnings.
- `dotnet test tests\Registry.Tests\Registry.Tests.csproj --no-build`: passed, 9 tests.
- Launch check: packaged `Registry.App.exe` was running after 12 seconds with 0 fresh crash events.

## 2026-05-19 Context Menu Reliability Pass

- Tightened value pane spacing:
  - increased header height,
  - added path-pill margin,
  - kept count pills visually separate from the path.
- Made context menus target-specific and smaller:
  - value rows get only value actions,
  - empty value-list space gets only create/key actions,
  - root key menus omit delete instead of showing a disabled command.
- Added `RightTapped` handlers as a reliable fallback alongside `ContextRequested`.
- Consolidated value and empty-space context menu construction into dedicated helpers.
- Removed disabled/dead menu items from the common context menu paths.

## Context Menu Reliability Verification

- `dotnet build Registry.slnx --no-restore`: passed with 0 warnings.
- `dotnet test tests\Registry.Tests\Registry.Tests.csproj --no-build`: passed, 9 tests.
- CLI scratch-path read still works.
- Launch check: packaged `Registry.App.exe` was running after 12 seconds with 0 fresh crash events.

## 2026-05-19 Value Row Hit Target Polish

- Removed the parallel `RightTapped` context-menu path to avoid duplicate right-click routing and pointer flicker.
- Made value rows stretch to the full list width with a transparent hit surface, so right-clicking anywhere on a row targets the row.
- Kept empty-space context menus on the ListView itself.
- Removed the extra icon from the selected-path pill and balanced header spacing/count-pill widths.

## Value Row Hit Target Verification

- `dotnet build Registry.slnx --no-restore`: passed with 0 warnings.
- `dotnet test tests\Registry.Tests\Registry.Tests.csproj --no-build`: passed, 9 tests.
- Launch check: packaged `Registry.App.exe` was running after 12 seconds with 0 fresh crash events.

## 2026-05-19 Fluent Header and Toolbar Spacing

- Replaced the boxed current-path pill with a Fluent-style value-pane header band.
- Added a small `Current key` metadata label above the path to avoid clipped text and fake-input styling.
- Kept count chips on the right, with lighter sizing and better spacing.
- Removed command bar separators and replaced them with modest command group margins:
  - navigation/action group,
  - selected-value group,
  - key/copy/export group.

## Fluent Header Verification

- `dotnet build Registry.slnx --no-restore`: passed with 0 warnings.
- `dotnet test tests\Registry.Tests\Registry.Tests.csproj --no-build`: passed, 9 tests.
- Launch check: packaged `Registry.App.exe` was running after 12 seconds with 0 fresh crash events.

## 2026-05-19 Path Header Context Menu Polish

- Removed the `Current key` label from the value-pane header.
- Reduced the header height so the path line sits more naturally above the value columns.
- Added a right-click context menu to the path text:
  - Copy key path,
  - Favorite key,
  - Export key,
  - Delete key for non-root keys.
- Reused the same key-menu helper for tree keys and the path header so key actions stay consistent.

## Path Header Context Verification

- `dotnet build Registry.slnx --no-restore`: passed with 0 warnings.
- `dotnet test tests\Registry.Tests\Registry.Tests.csproj --no-build`: passed, 9 tests.
- Launch check: packaged `Registry.App.exe` was running after 12 seconds with 0 fresh crash events.

## 2026-05-19 Backdrop and Navigation Cleanup

- Switched the window backdrop to `MicaBackdrop Kind="BaseAlt"` for a stronger Mica treatment where Windows supports it.
- Removed About from the primary left rail.
- Moved About content into Settings and deleted the old standalone About page.
- Simplified value context menu routing:
  - removed row-level context routing,
  - kept one ListView context path that detects row versus empty-space clicks,
  - avoids competing context/right-click routes that could cause cursor flicker.

## Backdrop and Navigation Verification

- `dotnet build Registry.slnx --no-restore`: passed with 0 warnings.
- `dotnet test tests\Registry.Tests\Registry.Tests.csproj --no-build`: passed, 9 tests.
- Launch check: packaged `Registry.App.exe` was running with 0 fresh crash events.

## 2026-05-19 Release Cleanup: Settings and Backdrop

- Kept Mica enabled by default and made the backdrop configurable in Settings:
  - Mica,
  - Strong Mica,
  - Off.
- Moved backdrop setup from fixed XAML into `MainWindow` so setting changes apply live.
- Kept About inside Settings instead of the primary rail at this stage; a later pass restored a standalone lower-rail About page.
- Removed leftover WinUI template comments/imports from `App.xaml.cs` and `MainWindow.xaml.cs`.

## Release Cleanup Verification

- `dotnet build Registry.slnx --no-restore`: passed with 0 warnings.
- `dotnet test tests\Registry.Tests\Registry.Tests.csproj --no-build`: passed, 9 tests.
- CLI scratch-path read still works.
- Launch check: packaged `Registry.App.exe` was running after 12 seconds with 0 fresh crash events.

## 2026-05-19 Release Cleanup: About, Status, and Context Menus

- Restored About as a standalone page and placed it in the lower navigation rail so it is available without taking primary Registry/Favorites space.
- Reworked Settings into grouped Fluent-style sections for appearance, safety, and About pointers.
- Removed the duplicate text-only 32/64-bit status label; the bottom selector is now the single visible view-mode control.
- Changed value-list context menus from routed `ContextRequested` to direct right-button pointer handling:
  - right-click on a value row shows value actions,
  - right-click on empty value-list space shows create/key actions,
  - selection is updated before the menu opens.
- Simplified the binary editor helper strip by removing the cramped icon column and using one clean wrapped helper message.

## About/Context Cleanup Verification

- `dotnet build Registry.slnx --no-restore`: passed with 0 warnings.
- `dotnet test tests\Registry.Tests\Registry.Tests.csproj --no-build`: passed, 9 tests.
- Launch check: packaged `Registry.App.exe` was running after 14 seconds with 0 fresh crash events.

## 2026-05-19 Key Context Menu Stabilization and Launch Planning

- Made key context menus act on a captured right-click target path instead of mutable `_currentPath`.
- Right-clicking non-key tree space no longer opens a menu for the old selection, which lets open menus dismiss normally.
- The tree `Open` action is disabled when the right-clicked key is already current.
- Key context actions now copy/favorite/export the targeted key directly; create/delete select the target before running.
- Added `docs/launch-plan.md` with P0 preview gates, P1 regedit parity, P2 power features, a test matrix, and a release checklist.

## 2026-05-19 Launch Plan P0: Rename and Rail Defaults

- Added shared engine rename APIs:
  - `RenameValue` preserves value kind and raw data,
  - `RenameSubKey` copies the key tree, then removes the old key after the copy succeeds.
- Added CLI commands:
  - `rename-value`,
  - `rename-key`.
- Added app rename flows:
  - value-row context menu `Rename`,
  - key context menu `Rename key`,
  - `F2` renames the selected value or the current key.
- Added tests for value rename and recursive key rename.
- Set the left NavigationView rail to start compact/collapsed by default.
- Preserved the current path when rebuilding the tree after writes/view changes so operations no longer jump back to the first hive.

## Rename/Rail Verification

- `dotnet build Registry.slnx --no-restore`: passed with 0 warnings.
- `dotnet test tests\Registry.Tests\Registry.Tests.csproj --no-build`: passed, 11 tests.
- CLI HKCU smoke test created a scratch key, renamed a value, renamed the key, read the renamed value, and deleted the scratch key.

## 2026-05-19 Export and Safety Roadmap Cleanup

- Removed Settings copy that implied shadow journals/elevation-aware commit flows were an immediate roadmap item.
- Clarified the preview safety model as:
  - automatic backups,
  - explicit confirmations,
  - disabled invalid commands,
  - clear access-denied feedback.
- Improved app export:
  - toolbar Export saves the current key to a chosen `.reg` file,
  - context menus can copy key export text,
  - context menus can save current-key export,
  - context menus can save recursive subtree export.
- Updated launch planning so shadow journals are treated as optional P2 staging/rollback research, not preview bloat.

## 2026-05-19 P2 Start: Monitoring, Diagnostics, Advanced Editors

- Added current-key monitoring as an optional toolbar toggle:
  - polls the loaded key every 2 seconds,
  - reports added/changed/removed values and subkey count changes,
  - can be paused from the toolbar.
- Added value diagnostics:
  - context menu can copy `reg add`,
  - context menu can copy a PowerShell `New-ItemProperty` command.
- Improved numeric value editing:
  - DWORD/QWORD create/edit dialogs now expose Decimal/Hexadecimal base selection,
  - dialogs show decimal and hex previews.
- Improved expandable string editing:
  - editor shows an expanded environment-variable preview.

## 2026-05-19 P2 Safety and Backdrop Polish

- Removed the About card from Settings; About remains a standalone lower-rail page only.
- Reworked the expanded NavigationView pane to use a semi-transparent rail surface instead of either the heavy default fill or a fully missing background.
- Added a lightweight shadow workspace:
  - app-originated writes capture a rollback `.reg` snapshot before applying the write,
  - the toolbar `Rollback` action saves the latest snapshot to a chosen file,
  - the journal keeps the latest 20 snapshots in memory.
- Added status diagnostics for the active view:
  - shows the active 32-bit/64-bit view,
  - flags Software/HKCR paths that can differ between 32-bit and 64-bit registry views,
  - flags protected HKLM areas that may require elevation for writes.

## 2026-05-19 Logo Asset Pipeline

- Processed `regisstrylogov1.png` into app-ready Windows assets:
  - cropped to the real alpha silhouette,
  - trimmed excessive external glow,
  - applied light contrast/color/sharpness polish,
  - generated padded taskbar/titlebar/tile/splash assets,
  - generated a multi-size `AppIcon.ico`.
- Added the new logo to the About page and README.
- Preserved the polished master under `Assets\RegistryLogo.png`.

## 2026-05-19 Logo Micro-Tuning

- Regenerated the app icon assets with a larger visual fill:
  - reduced padding around the alpha silhouette,
  - increased small-target fill for 24 px and 48 px assets,
  - increased tile/splash logo size while keeping safe transparent margins.
- Recompressed PNG outputs with maximum PNG compression and regenerated the multi-size ICO.

## 2026-05-19 Navigation Rail Surface Fix

- Restored the original compact left rail behavior for the collapsed state.
- Replaced the hardcoded dark NavigationView rail fill with a code-applied acrylic-style pane brush that tracks the backdrop setting, with a solid fallback when backdrop is off.
- Split the bottom status text into separated WinUI status segments for message, active view, and diagnostics instead of one dot/semicolon sentence.
- Renamed the visible app title/package display name from `Registry.App` to `Registry`.

## 2026-05-19 Reg File Import

- Added app import flow:
  - file picker import from the toolbar,
  - parsed operation preview,
  - explicit Apply confirmation.
- Added core `.reg` parser/apply support for common forms:
  - key create/open,
  - key delete,
  - value delete,
  - string,
  - expandable string,
  - DWORD,
  - QWORD,
  - binary,
  - multi-string.
- Added tests for parsing and applying `.reg` files.
- Attempted package manifest `.reg` association, but Windows rejected package registration because `.reg` is reserved for system use. Kept in-app import instead.
- Added per-user Open with registration from Settings using `HKCU\Software\Classes\.reg\OpenWithProgids` and `Applications\<exe>\SupportedTypes`.
- Added command-line `.reg` path handling so Open with can pass a selected file into the app import preview.
- Added CLI `import <file.reg>`.

## 2026-05-19 Regedit Parity Round

- Removed repeated 32-bit/64-bit and WOW64 Software-path chatter from the bottom status bar; the view selector remains the source of truth.
- Added recursive Find support:
  - searches keys, value names, and value data,
  - supports match case and whole-string matching,
  - runs off the UI thread with timeout protection,
  - opens the matched key and selects the matched value when applicable,
  - added `registry find` to the CLI.
- Added registry ACL visibility:
  - reads owner and access rules,
  - shows explicit/inherited permissions in a WinUI dialog,
  - supports copying the ACL summary.
- Added hive load/unload plumbing:
  - core Win32 `RegLoadKey` / `RegUnLoadKey` wrappers with privilege enablement,
  - toolbar actions for Load hive and Unload hive,
  - CLI `load-hive` and `unload-hive`.
- Added tests for recursive search and permission reads.
