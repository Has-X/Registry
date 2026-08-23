# Safe editing

Registry edits Windows configuration. The safest workflow is deliberate, reversible, and limited to the smallest affected key.

## Before changing a key

1. Confirm the full path in the address bar.
2. For paths under `Software`, confirm the 32-bit or 64-bit view. The two views can expose different values.
3. Export the key or subtree to a `.reg` file.
4. Use `HKEY_CURRENT_USER\Software\RegistryPreview` for experiments instead of system-wide locations.
5. Read the confirmation dialog and the type of every value being changed.

## Protected paths

`HKEY_LOCAL_MACHINE`, file associations, services, and policy keys can affect other users or Windows itself. Registry runs as the current user; it does not elevate the whole interface. A denied write means Windows has protected the target. Do not weaken ACLs merely to make an edit succeed.

## Value types

Enter DWORD and QWORD data using the selected decimal or hexadecimal base. Binary and multi-string values require their intended format; an apparently valid text value can still change application behaviour. Prefer an export and a small, verified edit over a broad import.

## After changing a key

Refresh the key and the affected application. Review Registry's Journal entry when the change was made in the app. If the result is wrong, use a saved snapshot only when it corresponds to the change you just made; otherwise restore the export you created before editing.
