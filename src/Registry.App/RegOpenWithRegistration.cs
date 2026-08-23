using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Registry_App;

public static class RegOpenWithRegistration
{
    private const string ProgId = "Chromatic.Registry.reg";
    private const string LegacyProgId = "Registry.regfile";
    private const string DefaultAppsExecutableName = "Registry.exe";
    private const string Extension = ".reg";
    private const string CapabilitiesPath = @"Software\Chromatic\Registry\Capabilities";
    private const uint ShcneAssocchanged = 0x08000000;
    private const uint ShcnfIdlist = 0x0000;

    public static void Register()
    {
        var executablePath = Environment.ProcessPath
            ?? Process.GetCurrentProcess().MainModule?.FileName
            ?? throw new InvalidOperationException("Could not resolve the app executable path.");
        executablePath = Path.GetFullPath(executablePath);
        if (!File.Exists(executablePath))
        {
            throw new FileNotFoundException("The Registry executable does not exist.", executablePath);
        }

        var executableName = Path.GetFileName(executablePath);
        var command = $"\"{executablePath}\" \"%1\"";
        // Default Apps resolves this value independently from context-menu verb
        // icons, so always use the installed executable's first icon resource.
        var icon = $"\"{executablePath}\",0";

        RegisterProgId(ProgId, command, icon);
        RegisterProgId(LegacyProgId, command, icon);

        using (var openWith = Microsoft.Win32.Registry.CurrentUser.CreateSubKey($@"Software\Classes\{Extension}\OpenWithProgids"))
        {
            openWith.SetValue(ProgId, Array.Empty<byte>(), RegistryValueKind.None);
            openWith.SetValue(LegacyProgId, Array.Empty<byte>(), RegistryValueKind.None);
        }

        RegisterApplication(executableName, command, icon);
        if (!string.Equals(executableName, DefaultAppsExecutableName, StringComparison.OrdinalIgnoreCase))
        {
            // Windows 11 can cache an Applications lookup by display executable
            // name. Keep this compatibility alias in sync with the real apphost.
            RegisterApplication(DefaultAppsExecutableName, command, icon);
        }

        using (var capabilities = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(CapabilitiesPath))
        {
            capabilities.SetValue("ApplicationName", "Registry");
            capabilities.SetValue("ApplicationDescription", "A modern editor for Windows registry data.");
            capabilities.SetValue("ApplicationIcon", icon);
        }

        using (var fileAssociations = Microsoft.Win32.Registry.CurrentUser.CreateSubKey($@"{CapabilitiesPath}\FileAssociations"))
        {
            fileAssociations.SetValue(Extension, ProgId);
        }

        using (var registeredApplications = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\RegisteredApplications"))
        {
            registeredApplications.SetValue("Registry", CapabilitiesPath);
        }

        using (var shellVerb = Microsoft.Win32.Registry.CurrentUser.CreateSubKey($@"Software\Classes\SystemFileAssociations\{Extension}\shell\Registry.Chromatic.Open"))
        {
            shellVerb.SetValue(string.Empty, "Open with Registry");
            shellVerb.SetValue("Icon", icon);
        }

        using (var shellCommand = Microsoft.Win32.Registry.CurrentUser.CreateSubKey($@"Software\Classes\SystemFileAssociations\{Extension}\shell\Registry.Chromatic.Open\command"))
        {
            shellCommand.SetValue(string.Empty, command);
        }

        SHChangeNotify(ShcneAssocchanged, ShcnfIdlist, IntPtr.Zero, IntPtr.Zero);
    }

    private static void RegisterProgId(string progId, string command, string icon)
    {
        using var root = Microsoft.Win32.Registry.CurrentUser.CreateSubKey($@"Software\Classes\{progId}");
        root.SetValue(string.Empty, "Registry File");
        using var defaultIcon = Microsoft.Win32.Registry.CurrentUser.CreateSubKey($@"Software\Classes\{progId}\DefaultIcon");
        defaultIcon.SetValue(string.Empty, icon);
        using var commandKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey($@"Software\Classes\{progId}\shell\open\command");
        commandKey.SetValue(string.Empty, command);
    }

    private static void RegisterApplication(string executableName, string command, string icon)
    {
        using var app = Microsoft.Win32.Registry.CurrentUser.CreateSubKey($@"Software\Classes\Applications\{executableName}");
        app.SetValue("FriendlyAppName", "Registry");
        using var defaultIcon = Microsoft.Win32.Registry.CurrentUser.CreateSubKey($@"Software\Classes\Applications\{executableName}\DefaultIcon");
        defaultIcon.SetValue(string.Empty, icon);
        using var commandKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey($@"Software\Classes\Applications\{executableName}\shell\open\command");
        commandKey.SetValue(string.Empty, command);
        using var supported = Microsoft.Win32.Registry.CurrentUser.CreateSubKey($@"Software\Classes\Applications\{executableName}\SupportedTypes");
        supported.SetValue(Extension, string.Empty);
    }

    [DllImport("shell32.dll")]
    private static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
}
