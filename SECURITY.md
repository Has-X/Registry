# Security Policy

## Supported Versions

Security fixes are handled on the default branch until the first stable release
series is established.

## Reporting a Vulnerability

Please report security issues privately instead of opening a public issue.

Use GitHub private vulnerability reporting when available for this repository.
If that is not available, contact the maintainer through the GitHub profile for
`Has-X` and include:

- affected version or commit,
- exact registry path or feature involved,
- steps to reproduce,
- expected and actual behavior,
- whether elevation/admin rights are required.

## Scope

Useful reports include:

- unsafe registry writes,
- missing confirmation before destructive actions,
- incorrect 32-bit/64-bit view targeting,
- `.reg` import parsing bugs that write unexpected data,
- privilege or elevation confusion,
- crashes caused by malformed registry data.

Please do not test against other people's machines, networks, or registry hives
without permission.
