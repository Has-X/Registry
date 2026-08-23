; Registry native Windows installer. Pass /DAppVersion, /DSourceDir and /DOutputDir from CI.
#ifndef AppVersion
  #define AppVersion "0.2.0"
#endif
#ifndef SourceDir
  #define SourceDir "..\\native-publish"
#endif
#ifndef OutputDir
  #define OutputDir "..\\dist"
#endif
#ifndef AppArchitecture
  #define AppArchitecture "x64"
#endif
#if AppArchitecture == "x64"
  #define InstallArchitecture "x64compatible"
#else
  #define InstallArchitecture "arm64"
#endif

[Setup]
AppId={{1D6E095B-75F1-4CFB-9B12-2E42CD60DFE1}}
AppName=Registry
AppVersion={#AppVersion}
AppVerName=Registry {#AppVersion}
AppPublisher=Chromatic
AppPublisherURL=https://chromatic.hu
AppSupportURL=mailto:feedback@chromatic.hu
AppUpdatesURL=https://github.com/Has-X/Registry/releases
DefaultDirName={localappdata}\Programs\Registry
DefaultGroupName=Registry
UninstallDisplayIcon={app}\Registry.App.exe
SetupIconFile=assets\app.ico
OutputDir={#OutputDir}
OutputBaseFilename=Registry-Setup-{#AppArchitecture}
VersionInfoVersion={#AppVersion}
VersionInfoCompany=Chromatic
VersionInfoDescription=Registry Installer
VersionInfoProductName=Registry
VersionInfoProductVersion={#AppVersion}
ArchitecturesAllowed={#InstallArchitecture}
ArchitecturesInstallIn64BitMode={#InstallArchitecture}
#if AppArchitecture == "x64"
SetupArchitecture=x64
#endif
PrivilegesRequired=lowest
DisableWelcomePage=yes
DisableProgramGroupPage=yes
DisableReadyPage=yes
DisableDirPage=auto
ShowLanguageDialog=no
WizardStyle=modern dynamic windows11 hidebevels
WizardSizePercent=120,120
WizardKeepAspectRatio=yes
WizardBackColor=$FFFFFF
WizardBackColorDynamicDark=$000000
WizardBackImageFile=assets\installer-background-light-v2.png
WizardBackImageFileDynamicDark=assets\installer-background-dark-v2.png
WizardImageFile=
WizardSmallImageFile=
Compression=lzma2/ultra64
SolidCompression=yes
CompressionThreads=auto
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "hungarian"; MessagesFile: "compiler:Languages\Hungarian.isl"
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "german"; MessagesFile: "compiler:Languages\German.isl"
Name: "french"; MessagesFile: "compiler:Languages\French.isl"
Name: "italian"; MessagesFile: "compiler:Languages\Italian.isl"
Name: "polish"; MessagesFile: "compiler:Languages\Polish.isl"
Name: "portuguese"; MessagesFile: "compiler:Languages\Portuguese.isl"
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"
Name: "czech"; MessagesFile: "compiler:Languages\Czech.isl"
Name: "slovak"; MessagesFile: "compiler:Languages\Slovak.isl"
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "ukrainian"; MessagesFile: "compiler:Languages\Ukrainian.isl"
Name: "chinese_simplified"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"
Name: "chinese_traditional"; MessagesFile: "compiler:Languages\ChineseTraditional.isl"
Name: "arabic"; MessagesFile: "compiler:Languages\Arabic.isl"
Name: "thai"; MessagesFile: "compiler:Languages\Thai.isl"
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"
Name: "korean"; MessagesFile: "compiler:Languages\Korean.isl"
Name: "dutch"; MessagesFile: "compiler:Languages\Dutch.isl"
Name: "bulgarian"; MessagesFile: "compiler:Languages\Bulgarian.isl"
Name: "slovenian"; MessagesFile: "compiler:Languages\Slovenian.isl"
Name: "swedish"; MessagesFile: "compiler:Languages\Swedish.isl"
Name: "danish"; MessagesFile: "compiler:Languages\Danish.isl"
Name: "finnish"; MessagesFile: "compiler:Languages\Finnish.isl"
Name: "norwegian"; MessagesFile: "compiler:Languages\Norwegian.isl"

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[InstallDelete]
Type: files; Name: "{app}\*.dll"
Type: files; Name: "{app}\*.exe"
Type: files; Name: "{app}\*.json"
Type: files; Name: "{app}\*.pri"
Type: files; Name: "{app}\*.winmd"

[Icons]
Name: "{autoprograms}\Registry"; Filename: "{app}\Registry.App.exe"; WorkingDir: "{app}"

[Registry]
Root: HKCU; Subkey: "Software\Classes\Applications\Registry.App.exe"; ValueType: string; ValueName: "FriendlyAppName"; ValueData: "Registry"
Root: HKCU; Subkey: "Software\Classes\Applications\Registry.App.exe"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\Applications\Registry.App.exe\DefaultIcon"; ValueType: string; ValueData: """{app}\Registry.App.exe"",0"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\Applications\Registry.App.exe\shell\open\command"; ValueType: string; ValueData: """{app}\Registry.App.exe"" ""%1"""; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\Applications\Registry.App.exe\SupportedTypes"; ValueType: string; ValueName: ".reg"; ValueData: ""; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\Applications\Registry.exe"; ValueType: string; ValueName: "FriendlyAppName"; ValueData: "Registry"; Flags: uninsdeletekeyifempty uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\Applications\Registry.exe\DefaultIcon"; ValueType: string; ValueData: """{app}\Registry.App.exe"",0"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\Applications\Registry.exe\shell\open\command"; ValueType: string; ValueData: """{app}\Registry.App.exe"" ""%1"""; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\Applications\Registry.exe\SupportedTypes"; ValueType: string; ValueName: ".reg"; ValueData: ""; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\Chromatic.Registry.reg"; ValueType: string; ValueData: "Registry File"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\Chromatic.Registry.reg\DefaultIcon"; ValueType: string; ValueData: """{app}\Registry.App.exe"",0"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\Chromatic.Registry.reg\shell\open\command"; ValueType: string; ValueData: """{app}\Registry.App.exe"" ""%1"""; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\.reg\OpenWithProgids"; ValueType: string; ValueName: "Chromatic.Registry.reg"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\.reg\shell\Registry.Chromatic.Open"; ValueType: string; ValueData: "Open with Registry"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\.reg\shell\Registry.Chromatic.Open"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\Registry.App.exe"",0"; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\.reg\shell\Registry.Chromatic.Open\command"; ValueType: string; ValueData: """{app}\Registry.App.exe"" ""%1"""; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Chromatic\Registry\Capabilities"; ValueType: string; ValueName: "ApplicationName"; ValueData: "Registry"; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Chromatic\Registry\Capabilities"; ValueType: string; ValueName: "ApplicationDescription"; ValueData: "A modern editor for Windows registry data."; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Chromatic\Registry\Capabilities"; ValueType: string; ValueName: "ApplicationIcon"; ValueData: """{app}\Registry.App.exe"",0"; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Chromatic\Registry\Capabilities\FileAssociations"; ValueType: string; ValueName: ".reg"; ValueData: "Chromatic.Registry.reg"; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Chromatic\Registry\Capabilities"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\RegisteredApplications"; ValueType: string; ValueName: "Registry"; ValueData: "Software\Chromatic\Registry\Capabilities"; Flags: uninsdeletevalue

[Code]
const
  UserEnvironmentKey = 'Environment';
  CommandDirectory = '{localappdata}\Chromatic\bin';
  WM_SETTINGCHANGE = $001A;
  SMTO_ABORTIFHUNG = $0002;

procedure SHChangeNotify(wEventId: Cardinal; uFlags: Cardinal; dwItem1, dwItem2: Integer);
  external 'SHChangeNotify@shell32.dll stdcall';

function SendMessageTimeout(hWnd: HWND; Msg: UINT; wParam: Longint; lParam: String;
  fuFlags, uTimeout: UINT; var lpdwResult: DWORD): LRESULT;
  external 'SendMessageTimeoutW@user32.dll stdcall';

procedure NotifyEnvironmentChanged;
var
  ResultCode: DWORD;
begin
  SendMessageTimeout(HWND_BROADCAST, WM_SETTINGCHANGE, 0, 'Environment',
    SMTO_ABORTIFHUNG, 5000, ResultCode);
end;

function PathContains(const PathValue, Directory: String): Boolean;
begin
  Result := Pos(';' + Lowercase(Directory) + ';',
    ';' + Lowercase(PathValue) + ';') > 0;
end;

procedure AddCommandDirectoryToPath;
var
  PathValue: String;
  Directory: String;
  UpdatedPath: String;
begin
  Directory := ExpandConstant(CommandDirectory);
  if not RegQueryStringValue(HKCU, UserEnvironmentKey, 'Path', PathValue) then
    PathValue := '';
  if PathContains(PathValue, Directory) then begin
    Log('Chromatic command directory is already present on the user PATH.');
    exit;
  end;
  if PathValue = '' then
    UpdatedPath := Directory
  else
    UpdatedPath := PathValue + ';' + Directory;
  if RegWriteExpandStringValue(HKCU, UserEnvironmentKey, 'Path', UpdatedPath) then begin
    NotifyEnvironmentChanged;
    Log('Added Chromatic command directory to the user PATH.');
  end else
    Log('Unable to add the Chromatic command directory to the user PATH.');
end;

procedure RemoveCommandDirectoryFromPath;
var
  PathValue: String;
  Directory: String;
  UpdatedPath: String;
begin
  Directory := ExpandConstant(CommandDirectory);
  if not RegQueryStringValue(HKCU, UserEnvironmentKey, 'Path', PathValue) then exit;
  UpdatedPath := PathValue;
  StringChangeEx(UpdatedPath, ';' + Directory, '', True);
  StringChangeEx(UpdatedPath, Directory + ';', '', True);
  if CompareText(UpdatedPath, Directory) = 0 then UpdatedPath := '';
  if UpdatedPath = PathValue then exit;
  if RegWriteExpandStringValue(HKCU, UserEnvironmentKey, 'Path', UpdatedPath) then begin
    NotifyEnvironmentChanged;
    Log('Removed Chromatic command directory from the user PATH.');
  end;
end;

procedure WriteCommandShim(const FileName, Command: String);
var
  ShimPath: String;
begin
  ForceDirectories(ExpandConstant(CommandDirectory));
  ShimPath := AddBackslash(ExpandConstant(CommandDirectory)) + FileName;
  if not SaveStringToFile(ShimPath, Command + #13#10, False) then
    Log('Unable to create command shim: ' + ShimPath);
end;

procedure CreateCommandLineShims;
begin
  WriteCommandShim('registry.cmd',
    '@start "" "' + ExpandConstant('{app}\Registry.App.exe') + '" %*');
  WriteCommandShim('registry-cli.cmd',
    '@"' + ExpandConstant('{app}\Registry.Cli.exe') + '" %*' + #13#10 + '@exit /b %errorlevel%');
end;

procedure RemoveCommandLineShims;
var
  Directory: String;
begin
  Directory := ExpandConstant(CommandDirectory);
  DeleteFile(AddBackslash(Directory) + 'registry.cmd');
  DeleteFile(AddBackslash(Directory) + 'registry-cli.cmd');
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then begin
    CreateCommandLineShims;
    AddCommandDirectoryToPath;
    SHChangeNotify($08000000, $0000, 0, 0);
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then begin
    RemoveCommandLineShims;
    RemoveCommandDirectoryFromPath;
  end;
end;

[Run]
Filename: "{app}\Registry.App.exe"; Description: "{cm:LaunchProgram,Registry}"; Flags: nowait postinstall skipifsilent
