# Known limitations

- Registry currently works with local hives; Remote Registry connections are not implemented.
- Permission inspection is read-only. Use the Windows security editor or your organisation's management tooling to alter ACLs.
- Some uncommon legacy and escaped `.reg` forms are still being hardened. Review the import preview and keep an export before applying a file.
- Journal records supported changes made through Registry. It does not observe changes from `regedit`, installers, Group Policy, scripts, or other applications.
- Protected keys require the permissions Windows assigns to the current user. Registry deliberately does not elevate its whole user interface.
- Registry ships unsigned at present. Verify release checksums before installation.
