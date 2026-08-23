using System.Text.Json;

namespace Registry_App;

/// <summary>
/// Per-user storage for the unpackaged Inno Setup build. Windows.Storage.ApplicationData
/// requires MSIX package identity and therefore cannot be used by Registry.App.exe.
/// </summary>
public static class RegistryAppData
{
    private static readonly object SettingsLock = new();
    private static readonly string RootPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Chromatic",
        "Registry");
    private static readonly string SettingsPath = Path.Combine(RootPath, "settings.json");

    public static string DataDirectory
    {
        get
        {
            Directory.CreateDirectory(RootPath);
            return RootPath;
        }
    }

    public static string? GetSetting(string key)
    {
        lock (SettingsLock)
        {
            return ReadSettings().GetValueOrDefault(key);
        }
    }

    public static void SetSetting(string key, string value)
    {
        lock (SettingsLock)
        {
            var settings = ReadSettings();
            settings[key] = value;
            Directory.CreateDirectory(RootPath);
            var temporaryPath = SettingsPath + ".tmp";
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(settings));
            File.Move(temporaryPath, SettingsPath, true);
        }
    }

    private static Dictionary<string, string> ReadSettings()
    {
        try
        {
            return File.Exists(SettingsPath)
                ? JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(SettingsPath)) ?? []
                : [];
        }
        catch (JsonException)
        {
            return [];
        }
        catch (IOException)
        {
            return [];
        }
    }
}
