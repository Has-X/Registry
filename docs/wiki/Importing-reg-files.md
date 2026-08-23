# Importing .reg files

Opening a `.reg` file with Registry creates an import preview. Review the operations before applying them: the target key, value names, value types, and whether an operation creates, changes, or removes data.

## Before you apply

1. Check where the file came from. Do not import a file you do not trust.
2. Read the target paths. `HKEY_LOCAL_MACHINE` and `HKEY_CLASSES_ROOT` can affect every user or application on the PC.
3. Confirm the selected 32-bit or 64-bit view when the paths are under `Software`.
4. Export the affected key before applying a change.
5. Apply only when the preview matches your intent.

Registry adds itself to Windows' Open With and Default Apps surfaces for `.reg` files. It does not silently replace an existing default application. If Registry is selected as the default, opening a `.reg` file still presents the app's preview before operations are applied.

## Troubleshooting

An import can fail when a value type is invalid, a number is outside the range of its registry type, a target key is protected, or the current user lacks permission. Read the import result message, export the affected key if possible, and retry only after correcting the source file or permissions.
