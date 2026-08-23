# Architecture

Registry is a native Windows desktop application built with WinUI 3, the Windows App SDK, and .NET. It is not a web wrapper.

## Components

- **Registry.App** is the Windows interface: navigation, address bar, search, editors, import preview, Journal, and settings.
- **Registry.Core** contains path parsing, registry views, import and export handling, value formatting, and the supported write operations shared by the app and CLI.
- **Registry.Cli** is the terminal companion for inspection, export, search, import, and supported registry writes.

The release is self-contained for both x64 and ARM64. The Inno Setup installer installs per user, registers the `.reg` Open With and Default Apps metadata, and adds safe `registry` and `registry-cli` command shims to the user's `PATH`.

The app uses the `Chromatic.Registry` manifest identity. It runs as the invoking user; Windows protects elevated registry locations according to the user's existing permissions.
