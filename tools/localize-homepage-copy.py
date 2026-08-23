from pathlib import Path


PATH = Path(__file__).resolve().parents[1] / "src" / "Registry.App" / "Pages" / "HomePage.xaml.cs"
REPLACEMENTS = {
    '$"{ex.Message} The current key remains open."': 'LocalizationService.Format("status.invalid-path.message", "{0} The current key remains open.", ex.Message)',
    '$"Loading children for {item.Path}"': 'LocalizationService.Format("status.loading-children", "Loading children for {0}", item.Path)',
    '"Root hives cannot be deleted."': 'LocalizationService.Get("status.root-hive-protected.delete-message", "Root hives cannot be deleted.")',
    '$"Backup saved to {backupPath}"': 'LocalizationService.Format("status.backup-saved", "Backup saved to {0}", backupPath)',
    '"Use hex bytes separated by spaces, commas, or new lines."': 'LocalizationService.Get("status.invalid-binary-data.message", "Use hex bytes separated by spaces, commas, or new lines.")',
    '"Select a value in the details pane before editing."': 'LocalizationService.Get("status.no-value-selected.edit-message", "Select a value in the details pane before editing.")',
    '"Select a value in the details pane before deleting."': 'LocalizationService.Get("status.no-value-selected.delete-message", "Select a value in the details pane before deleting.")',
    '"Select a value before copying value data."': 'LocalizationService.Get("status.no-value-selected.copy-message", "Select a value before copying value data.")',
    '"Open a key before starting live monitoring."': 'LocalizationService.Get("status.no-key-loaded.monitor-message", "Open a key before starting live monitoring.")',
    '$"Watching {_currentPath} every 2 seconds."': 'LocalizationService.Format("status.monitoring-started.message", "Watching {0} every 2 seconds.", _currentPath)',
    '$"Monitoring {_currentPath}"': 'LocalizationService.Format("status.monitoring-current", "Monitoring {0}", _currentPath)',
    '"Live refresh is paused for the current key."': 'LocalizationService.Get("status.monitoring-paused.message", "Live refresh is paused for the current key.")',
    '$"Removed favorite {_currentPath}"': 'LocalizationService.Format("status.favorite-removed.current", "Removed favorite {0}", _currentPath)',
    '$"Added favorite {_currentPath}"': 'LocalizationService.Format("status.favorite-added.current", "Added favorite {0}", _currentPath)',
    '"This key is already in Favorites."': 'LocalizationService.Get("status.already-a-favorite.message", "This key is already in Favorites.")',
    '$"Favorite already saved: {_currentPath}"': 'LocalizationService.Format("status.already-a-favorite.current", "Favorite already saved: {0}", _currentPath)',
    '"Load hive canceled"': 'LocalizationService.Get("status.load-hive-canceled", "Load hive canceled")',
    '$"Mounted {file.Path} at {loadedPath}."': 'LocalizationService.Format("status.hive-loaded.message", "Mounted {0} at {1}.", file.Path, loadedPath)',
    '$"{ex.Message} Run elevated and choose a hive file not currently in use."': 'LocalizationService.Format("status.load-hive-failed.message", "{0} Run elevated and choose a hive file not currently in use.", ex.Message)',
    '$"{ex.Message} Run elevated and close any handles opened inside the mounted hive."': 'LocalizationService.Format("status.unload-hive-failed.message", "{0} Run elevated and close any handles opened inside the mounted hive.", ex.Message)',
    '"The key and subtree were copied as .reg text."': 'LocalizationService.Get("status.export-copied.subtree-message", "The key and subtree were copied as .reg text.")',
    '"The selected key values were copied as .reg text."': 'LocalizationService.Get("status.export-copied.values-message", "The selected key values were copied as .reg text.")',
    '$"Copied value name {selected.DisplayName}"': 'LocalizationService.Format("status.copy-value-name", "Copied value name {0}", selected.DisplayName)',
    '$"Copied path {menuPath}"': 'LocalizationService.Format("status.copy-path", "Copied path {0}", menuPath)',
    '$"{row.DisplayName} · {row.Kind}"': 'LocalizationService.Format("status.value-summary", "{0} · {1}", row.DisplayName, row.Kind)',
    '"Select a value before renaming."': 'LocalizationService.Get("status.no-value-selected.rename-message", "Select a value before renaming.")',
    '"Use @ in the CLI for the default value; the app keeps value rename targets named for safety."': 'LocalizationService.Get("status.invalid-value-name.message", "Use @ in the CLI for the default value; the app keeps value rename targets named for safety.")',
    '"Root hives cannot be renamed."': 'LocalizationService.Get("status.root-hive-protected.rename-message", "Root hives cannot be renamed.")',
    '$"Monitoring update: {changeText}"': 'LocalizationService.Format("status.monitoring-update", "Monitoring update: {0}", changeText)',
    '"Export canceled"': 'LocalizationService.Get("status.export-canceled", "Export canceled")',
    '$"Copied reg add command for {row.DisplayName}"': 'LocalizationService.Format("status.copy-reg-add", "Copied reg add command for {0}", row.DisplayName)',
    '$"Copied PowerShell command for {row.DisplayName}"': 'LocalizationService.Format("status.copy-powershell", "Copied PowerShell command for {0}", row.DisplayName)',
    'CreateDialog("Permissions", content, "Copy ACL")': 'CreateDialog(LocalizationService.Get("dialog.permissions", "Permissions"), content, LocalizationService.Get("action.copy-acl", "Copy ACL"))',
    '"The ACL summary was copied to the clipboard."': 'LocalizationService.Get("status.permissions-copied.message", "The ACL summary was copied to the clipboard.")',
    '"Open a registry key before searching."': 'LocalizationService.Get("status.no-key-loaded.search-message", "Open a registry key before searching.")',
    '"Choose at least one place to search."': 'LocalizationService.Get("status.search-needs-a-scope.message", "Choose at least one place to search.")',
    '$"Searching for {options.Query}"': 'LocalizationService.Format("status.searching-query", "Searching for {0}", options.Query)',
    '$"No match for \'{options.Query}\' under {startPath}."': 'LocalizationService.Format("status.finished-searching.no-match-message", "No match for \'{0}\' under {1}.", options.Query, startPath)',
    '$"No search match: {options.Query}"': 'LocalizationService.Format("status.no-search-match", "No search match: {0}", options.Query)',
    '$"{FormatSearchKind(result.MatchKind)}: {result.DisplayText}"': 'LocalizationService.Format("status.search-match-summary", "{0}: {1}", FormatSearchKind(result.MatchKind), result.DisplayText)',
    '$"Found {FormatSearchKind(result.MatchKind).ToLowerInvariant()} {result.DisplayText}"': 'LocalizationService.Format("status.search-found", "Found {0} {1}", FormatSearchKind(result.MatchKind).ToLowerInvariant(), result.DisplayText)',
    '"The search timed out before reaching the end of this branch."': 'LocalizationService.Get("status.search-stopped.message", "The search timed out before reaching the end of this branch.")',
    '$"Search timed out: {options.Query}"': 'LocalizationService.Format("status.search-timed-out", "Search timed out: {0}", options.Query)',
    '$"Write journal skipped: {ex.Message}"': 'LocalizationService.Format("status.write-journal-skipped", "Write journal skipped: {0}", ex.Message)',
    '"Use a value in range for the selected base."': 'LocalizationService.Get("editor.range-invalid", "Use a value in range for the selected base.")',
}

source = PATH.read_text(encoding="utf-8")
for old, new in REPLACEMENTS.items():
    if old not in source:
        raise RuntimeError(f"Expected source fragment is missing: {old}")
    source = source.replace(old, new)
PATH.write_text(source, encoding="utf-8")
