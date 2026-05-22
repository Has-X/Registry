using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Registry_App;

public static class RegOpenWithRegistration
{
    private const string ProgId = "Registry.regfile";
    private const string Extension = ".reg";
    private const uint ShcneAssocchanged = 0x08000000;
    private const uint ShcnfIdlist = 0x0000;

    public static void Register()
    {
        var executablePath = Environment.ProcessPath
            ?? Process.GetCurrentProcess().MainModule?.FileName
            ?? throw new InvalidOperationException("Could not resolve the app executable path.");
        var executableName = Path.GetFileName(executablePath);
        var command = $"\"{executablePath}\" \"%1\"";

        using (var progId = Microsoft.Win32.Registry.CurrentUser.CreateSubKey($@"Software\Classes\{ProgId}"))
        {
            progId.SetValue(string.Empty, "Registry File");
        }

        using (var commandKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey($@"Software\Classes\{ProgId}\shell\open\command"))
        {
            commandKey.SetValue(string.Empty, command);
        }

        using (var openWith = Microsoft.Win32.Registry.CurrentUser.CreateSubKey($@"Software\Classes\{Extension}\OpenWithProgids"))
        {
            openWith.SetValue(ProgId, Array.Empty<byte>(), RegistryValueKind.None);
        }

        using (var app = Microsoft.Win32.Registry.CurrentUser.CreateSubKey($@"Software\Classes\Applications\{executableName}"))
        {
            app.SetValue("FriendlyAppName", "Registry");
        }

        using (var supported = Microsoft.Win32.Registry.CurrentUser.CreateSubKey($@"Software\Classes\Applications\{executableName}\SupportedTypes"))
        {
            supported.SetValue(Extension, string.Empty);
        }

        SHChangeNotify(ShcneAssocchanged, ShcnfIdlist, IntPtr.Zero, IntPtr.Zero);
    }

    [DllImport("shell32.dll")]
    private static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
}
