# Contributing

Thanks for helping make Registry better.

## Development Setup

Requirements:

- Windows 11 or a current Windows 10 build.
- .NET SDK 10.
- Visual Studio with WinUI/Windows App SDK support, or the .NET CLI.

Useful commands:

```powershell
dotnet restore Registry.slnx
dotnet build Registry.slnx
dotnet test tests\Registry.Tests\Registry.Tests.csproj --no-build
dotnet run --project src\Registry.App\Registry.App.csproj
dotnet run --project src\Registry.Cli\Registry.Cli.csproj -- roots
```

## Safety Rules

Registry editing is sensitive. Contributions should keep these rules intact:

- destructive actions need confirmation,
- writes must target the selected 32-bit or 64-bit view explicitly,
- import/export behavior must match `.reg` expectations,
- UI must stay responsive on large hives such as `HKEY_CLASSES_ROOT`,
- tests should use disposable `HKCU\Software\...` keys only.

## Pull Requests

Before opening a pull request:

- run `dotnet build Registry.slnx`,
- run `dotnet test tests\Registry.Tests\Registry.Tests.csproj --no-build`,
- update docs when behavior changes,
- keep feature claims accurate.

Prefer small, focused changes. Avoid unrelated formatting churn.
