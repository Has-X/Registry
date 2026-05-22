using System.Text.RegularExpressions;

namespace Registry.Core;

public sealed partial record RegistryPath(RegistryHiveId Hive, string SubKey)
{
    public static RegistryPath Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException("Registry path cannot be empty.", nameof(input));
        }

        var normalized = SlashRegex().Replace(input.Trim(), @"\").Trim('\\');
        var separatorIndex = normalized.IndexOf('\\');
        var hiveToken = separatorIndex < 0 ? normalized : normalized[..separatorIndex];
        var subKey = separatorIndex < 0 ? string.Empty : normalized[(separatorIndex + 1)..];

        return new RegistryPath(ParseHive(hiveToken), subKey);
    }

    public override string ToString()
    {
        return string.IsNullOrEmpty(SubKey) ? GetHiveDisplayName(Hive) : $@"{GetHiveDisplayName(Hive)}\{SubKey}";
    }

    public static string GetHiveDisplayName(RegistryHiveId hive)
    {
        return hive switch
        {
            RegistryHiveId.ClassesRoot => "HKEY_CLASSES_ROOT",
            RegistryHiveId.CurrentUser => "HKEY_CURRENT_USER",
            RegistryHiveId.LocalMachine => "HKEY_LOCAL_MACHINE",
            RegistryHiveId.Users => "HKEY_USERS",
            RegistryHiveId.CurrentConfig => "HKEY_CURRENT_CONFIG",
            RegistryHiveId.PerformanceData => "HKEY_PERFORMANCE_DATA",
            _ => throw new ArgumentOutOfRangeException(nameof(hive), hive, null)
        };
    }

    public static RegistryHiveId ParseHive(string token)
    {
        return token.Trim().ToUpperInvariant() switch
        {
            "HKCR" or "HKEY_CLASSES_ROOT" => RegistryHiveId.ClassesRoot,
            "HKCU" or "HKEY_CURRENT_USER" => RegistryHiveId.CurrentUser,
            "HKLM" or "HKEY_LOCAL_MACHINE" => RegistryHiveId.LocalMachine,
            "HKU" or "HKEY_USERS" => RegistryHiveId.Users,
            "HKCC" or "HKEY_CURRENT_CONFIG" => RegistryHiveId.CurrentConfig,
            "HKPD" or "HKEY_PERFORMANCE_DATA" => RegistryHiveId.PerformanceData,
            _ => throw new FormatException($"Unknown registry hive '{token}'.")
        };
    }

    [GeneratedRegex(@"[\\/]+")]
    private static partial Regex SlashRegex();
}
