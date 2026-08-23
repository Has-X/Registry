# Windows installer

`Registry.iss` builds native x64 and ARM64 installers with Inno Setup 7. The installer follows Windows 11 light or dark mode, uses the current system language when an Inno language pack is available, and installs only a native payload for the selected architecture.

```powershell
dotnet publish src\Registry.App\Registry.App.csproj -c Release -r win-x64 -p:Platform=x64 --self-contained true -p:DebugType=None -p:DebugSymbols=false -o native-publish
dotnet publish src\Registry.Cli\Registry.Cli.csproj -c Release -r win-x64 --self-contained true -p:DebugType=None -p:DebugSymbols=false -o cli-publish
Copy-Item cli-publish\* native-publish -Recurse -Force
& "$env:LOCALAPPDATA\Programs\Inno Setup 7\ISCC.exe" "/DAppVersion=0.2.0" "/DAppArchitecture=x64" "/DSourceDir=$PWD\native-publish" "/DOutputDir=$PWD\installer\out" installer\Registry.iss
```

For ARM64, publish both projects with `-r win-arm64` (and `-p:Platform=ARM64` for the app) and pass `/DAppArchitecture=arm64`. Inno Setup's x64 bootstrapper runs through Windows on ARM's x64 emulation and installs only ARM64 application files. The installer includes `Registry.Cli.exe` and adds `registry` plus `registry-cli` shims to the current user's PATH. It is unsigned until a release-signing certificate is configured; unsigned builds must not be represented as signed.
