# Changelog

## 0.1.0 - Preview

Initial public preview.

### Added

- WinUI 3 registry browser with compact rail, key tree, value table, address bar, filters, and 32-bit/64-bit view selection.
- Create, edit, rename, and delete flows for common registry key and value operations.
- Editors for string, expandable string, DWORD, QWORD, binary, and multi-string values.
- `.reg` import preview/apply and export for current key or subtree.
- Favorites, Journal snapshots for app-made changes, and current-key monitoring.
- Read-only permissions view and load/unload hive commands.
- CLI for roots, list, find, get, set, rename, delete, import, export, and hive operations.
- GitHub Actions CI, release artifact build, Pages deployment, Dependabot, and CodeQL.

### Known Gaps

- Permission editing is not implemented.
- Remote registry support is not implemented.
- `.reg` parser hardening is still ongoing for uncommon edge cases.
- Full accessibility, high-contrast, and packaging/signing validation is still pending.
