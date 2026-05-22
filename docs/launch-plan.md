# Registry Launch Plan

This plan tracks the work still needed before Registry can be treated as a serious regedit replacement instead of a promising preview.

## Release Bar

Registry can ship as a preview when the app is stable across normal browsing/editing, all destructive actions are guarded, the CLI covers the same core operations, and every regedit-parity gap is either implemented or clearly labeled as unavailable.

Registry can ship as a full replacement when import/export, rename, permissions, search, hive operations, backup/restore, and stress/accessibility testing are complete.

## P0: Must Finish Before Preview

- Rename support: implemented for keys and values in app context menus, `F2`, CLI, and tests. Remaining polish is inline rename UI and broader keyboard focus validation.
- `.reg` file export/import:
  - save exports to a chosen file: implemented for current key and subtree in the app,
  - copy exports to clipboard remains available from context menus,
  - import `.reg` files through app: implemented with preview/apply for common value forms,
  - `.reg` file association: blocked because Windows reserves `.reg` for system use during package registration,
  - Open with registration: implemented as a per-user Settings action through `OpenWithProgids`,
  - CLI import: implemented,
  - additional parser hardening remains planned for uncommon `.reg` forms.
- Context-menu audit:
  - every key menu acts on the right-clicked key, not just the selected key,
  - every value menu acts on the right-clicked value,
  - empty-space menus only show create/key actions,
  - disabled items are hidden or clearly disabled with a useful reason.
- Keyboard parity:
  - `Del`, `F2`, `Enter`, `Ctrl+C`, `Ctrl+F`, `Ctrl+L`, `Alt+Up`, `F5`,
  - focus states must be visible and predictable.
- Safe write path:
  - automatic backup before delete/overwrite,
  - explicit confirmation for deleting keys and overwriting existing values,
  - clear access-denied feedback.
- Release stability:
  - no crash events during launch and browse smoke tests,
  - large-root expansion remains responsive,
  - app survives access denied keys and malformed input.

## P1: Full Regedit Parity

- Global search:
  - cancellable background worker,
  - streaming/paged results,
  - search keys, value names, value data, or all.
- Permissions:
  - read security descriptor,
  - display owner and ACL entries,
  - launch Windows security editor or implement a guarded editor.
- Hive operations:
  - load hive,
  - unload hive,
  - connect to remote registry where service and permissions allow it.
- Favorites and history polish:
  - rename favorite labels,
  - context menu on favorites,
  - recent locations page,
  - pinning only from user action.
- Merged/compare view:
  - read-only merged 32/64-bit comparison mode,
  - edit requires choosing the concrete target view.

## P2: Better Than Regedit

- Shadow edit workspace:
  - lightweight Journal snapshots: implemented for app-originated writes,
  - restore/remove snapshots from the Journal page with confirmation,
  - full multi-edit staging remains planned.
- Registry monitoring:
  - watch current key for changes: initial polling monitor implemented,
  - show changed values: implemented as added/changed/removed summary,
  - pause/resume live updates: implemented with toolbar toggle.
- Advanced value tools:
  - structured binary editor,
  - DWORD/QWORD base toggle: implemented for create/edit dialogs,
  - environment expansion preview for expandable strings: implemented for edit dialog,
  - multi-string line editor with validation.
- Diagnostics:
  - copy `reg add` / PowerShell commands: implemented for value rows,
  - show virtualization and WOW64 warnings: initial status diagnostics implemented,
  - show access-denied reason where Windows exposes it.

## Test Matrix

- Hives:
  - `HKCU`,
  - `HKLM` read-only paths,
  - `HKCR` large-root browse,
  - `HKU` profiles with access-denied branches.
- Views:
  - 32-bit view,
  - 64-bit view,
  - redirected paths under `Software`.
- Values:
  - default value,
  - string,
  - expandable string,
  - DWORD,
  - QWORD,
  - binary,
  - multi-string,
  - missing/unsupported kind.
- UI:
  - mouse,
  - keyboard-only,
  - high contrast,
  - 100%, 150%, and 200% scaling,
  - narrow and ultrawide windows.
- CLI:
  - parse paths with spaces,
  - import/export round trips,
  - non-admin failure messages,
  - admin-only path behavior.

## Release Checklist

- Build and tests pass from a clean checkout.
- App launches without fresh crash events.
- Smoke test creates, edits, renames, exports, imports, and deletes under a disposable HKCU test key.
- Documentation lists implemented features, known gaps, and safety warnings.
- Package has app icon, display name, version, and install/update notes.
- No hardcoded developer-only paths or sample favorites appear in the app.
