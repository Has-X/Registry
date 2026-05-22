# Registry Performance Notes

Registry is optimized around a simple rule: never make the UI wait for registry enumeration, and never realize more WinUI elements than the user can inspect.

## Current Fast Paths

- Value-pane refreshes use `RegistryBrowser.ReadKeySummary`.
  - This reads values for the selected key.
  - It uses native `SubKeyCount` and `ValueCount` for counts.
  - It avoids sorting and materializing subkey names when the right pane only needs counts.
- Tree expansion uses a read-through cache.
  - The first expansion reads and sorts subkey names off the UI thread.
  - Later expansions of the same key/view reuse the cached list.
  - Faulted reads are removed from the cache so transient access errors do not poison browsing.
- Tree rows are paged.
  - Only 120 child rows are realized at first.
  - `Load more...` appends the next page.
  - This prevents huge roots such as `HKEY_CLASSES_ROOT` from creating thousands of WinUI tree items at once.
- Writes invalidate cached registry reads.
  - Create, edit, and delete actions clear the read cache.
  - The realized tree branch for the current key is reset so a later expansion reloads fresh children.
- Value filtering is debounced.
  - Typing waits briefly before rebuilding the value list.
  - This keeps large value lists from doing repeated UI work for every keystroke.
- Navigation is stale-result safe.
  - Key refreshes carry a version.
  - If navigation moves on before an async read returns, the old result is ignored.
- Value-list context menus use direct pointer handling.
  - A right-click selects the row and opens the matching menu in one path.
  - Blank-space right-clicks skip value actions and show create/key actions.
  - This avoids duplicate routed context-menu work during right-click.

## Why This Should Beat Regedit In Feel

Regedit is mostly synchronous from the user's point of view: large nodes and repeated visits can feel like the same work is happening again. Registry keeps browsing state hot during a session, avoids unnecessary subkey enumeration for the value pane, and deliberately pages heavy tree nodes so the UI remains interactive.

## Remaining Bottlenecks

- `Microsoft.Win32.RegistryKey.GetSubKeyNames()` still returns the whole child-name array. Paging prevents UI overload, but the first expansion of a giant key still pays for the native enumeration and sorting.
- TreeView itself is still a general WinUI control. A future custom virtualized tree/list hybrid could render huge keys faster than TreeView.
- Exporting large recursive trees is intentionally not cached because it must reflect current disk state and can be expensive by nature.

## Next Optimization Targets

- Add a global search worker with cancellation, streaming results, and result paging.
- Add a custom virtualized key list for very large roots, with search/filter inside the tree pane.
- Add per-path cache invalidation instead of clearing the full read cache after every write.
- Add an explicit "Refresh subtree" command for power users who want to invalidate only part of the session cache.
