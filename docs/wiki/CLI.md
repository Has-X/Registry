# CLI reference

After installer-based setup, use `registry-cli` from a new terminal. Portable releases use `Registry.Cli.exe` from the extracted folder. The CLI shares Registry's core path parsing, architecture views, import/export behavior, and supported writes.

```powershell
registry-cli help
registry-cli roots
registry-cli ls HKCU\Software
registry-cli get HKCU\Environment Path
registry-cli find HKCU\Software Registry --keys --names
registry-cli export-tree HKCU\Environment
```

## Read-only commands

`roots`, `ls`, `get`, `find`, `export`, and `export-tree` inspect or write an export file. Use `--32` or `--64` to select a registry view where it matters.

## Commands that modify the registry

`set-string`, `set-dword`, `delete-value`, `rename-value`, `create-key`, `delete-key`, `rename-key`, `import`, `load-hive`, and `unload-hive` can change system state. Export the target key before use and start under a disposable `HKCU\Software` path.

```powershell
registry-cli create-key HKCU\Software RegistryPreview
registry-cli set-string HKCU\Software\RegistryPreview Example "test value"
registry-cli export HKCU\Software\RegistryPreview
```

Run `registry-cli help` for the complete argument syntax. Quote paths and values containing spaces.
