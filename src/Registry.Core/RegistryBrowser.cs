using System.Collections;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.AccessControl;
using Microsoft.Win32;

namespace Registry.Core;

public sealed class RegistryBrowser
{
    public IReadOnlyList<RegistryPath> GetRootPaths()
    {
        return Enum.GetValues<RegistryHiveId>()
            .Where(hive => hive != RegistryHiveId.PerformanceData)
            .Select(hive => new RegistryPath(hive, string.Empty))
            .ToArray();
    }

    public RegistryKeySnapshot ReadKey(RegistryPath path, RegistryViewMode viewMode = RegistryViewMode.Default)
    {
        using var root = OpenBaseKey(path.Hive, viewMode);
        using var key = string.IsNullOrEmpty(path.SubKey)
            ? root
            : root.OpenSubKey(path.SubKey, writable: false) ?? throw new RegistryKeyNotFoundException(path);

        var subKeys = key.GetSubKeyNames()
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var values = key.GetValueNames()
            .OrderBy(name => name.Length == 0 ? 0 : 1)
            .ThenBy(name => name, StringComparer.OrdinalIgnoreCase)
            .Select(name => ToValueInfo(key, name))
            .ToArray();

        return new RegistryKeySnapshot(path, subKeys, values);
    }

    public RegistryKeySummary ReadKeySummary(RegistryPath path, RegistryViewMode viewMode = RegistryViewMode.Default)
    {
        using var root = OpenBaseKey(path.Hive, viewMode);
        using var key = string.IsNullOrEmpty(path.SubKey)
            ? root
            : root.OpenSubKey(path.SubKey, writable: false) ?? throw new RegistryKeyNotFoundException(path);

        var values = key.GetValueNames()
            .OrderBy(name => name.Length == 0 ? 0 : 1)
            .ThenBy(name => name, StringComparer.OrdinalIgnoreCase)
            .Select(name => ToValueInfo(key, name))
            .ToArray();

        return new RegistryKeySummary(path, key.SubKeyCount, key.ValueCount, values);
    }

    public IReadOnlyList<string> GetSubKeyNames(RegistryPath path, RegistryViewMode viewMode = RegistryViewMode.Default)
    {
        using var root = OpenBaseKey(path.Hive, viewMode);
        using var key = string.IsNullOrEmpty(path.SubKey)
            ? root
            : root.OpenSubKey(path.SubKey, writable: false) ?? throw new RegistryKeyNotFoundException(path);

        return key.GetSubKeyNames()
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public RegistrySearchResult? FindFirst(
        RegistryPath start,
        RegistrySearchOptions options,
        RegistryViewMode viewMode = RegistryViewMode.Default,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Query);

        var pending = new Stack<RegistryPath>();
        pending.Push(start);
        while (pending.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var path = pending.Pop();
            RegistryKeySnapshot snapshot;
            try
            {
                snapshot = ReadKey(path, viewMode);
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or SecurityException or IOException)
            {
                continue;
            }

            if (options.MatchKeys)
            {
                var keyName = string.IsNullOrEmpty(path.SubKey) ? path.ToString() : GetLeafName(path);
                if (IsSearchMatch(keyName, options))
                {
                    return new RegistrySearchResult(path, RegistrySearchMatchKind.KeyName, null, keyName);
                }
            }

            foreach (var value in snapshot.Values)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (options.MatchValueNames && IsSearchMatch(value.DisplayName, options))
                {
                    return new RegistrySearchResult(path, RegistrySearchMatchKind.ValueName, value.Name, value.DisplayName);
                }

                if (options.MatchValueData && IsSearchMatch(value.DisplayData, options))
                {
                    return new RegistrySearchResult(path, RegistrySearchMatchKind.ValueData, value.Name, value.DisplayData);
                }
            }

            for (var index = snapshot.SubKeyNames.Count - 1; index >= 0; index--)
            {
                pending.Push(Combine(path, snapshot.SubKeyNames[index]));
            }
        }

        return null;
    }

    public RegistryPermissionSummary ReadPermissions(
        RegistryPath path,
        RegistryViewMode viewMode = RegistryViewMode.Default)
    {
        using var root = OpenBaseKey(path.Hive, viewMode);
        using var key = string.IsNullOrEmpty(path.SubKey)
            ? root
            : root.OpenSubKey(
                path.SubKey,
                RegistryKeyPermissionCheck.ReadSubTree,
                RegistryRights.ReadPermissions) ?? throw new RegistryKeyNotFoundException(path);

        var security = key.GetAccessControl(AccessControlSections.Owner | AccessControlSections.Access);
        var owner = security.GetOwner(typeof(System.Security.Principal.NTAccount))?.Value ?? "(unknown)";
        var rules = security
            .GetAccessRules(includeExplicit: true, includeInherited: true, targetType: typeof(System.Security.Principal.NTAccount))
            .OfType<RegistryAccessRule>()
            .Select(rule => new RegistryPermissionEntry(
                rule.IdentityReference.Value,
                rule.RegistryRights.ToString(),
                rule.AccessControlType.ToString(),
                rule.IsInherited,
                rule.InheritanceFlags.ToString(),
                rule.PropagationFlags.ToString()))
            .OrderBy(rule => rule.Identity, StringComparer.OrdinalIgnoreCase)
            .ThenBy(rule => rule.AccessType, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new RegistryPermissionSummary(path, owner, rules);
    }

    public RegistryPath Combine(RegistryPath parent, string childName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(childName);

        var subKey = string.IsNullOrEmpty(parent.SubKey)
            ? childName
            : $@"{parent.SubKey}\{childName}";

        return parent with { SubKey = subKey };
    }

    public void CreateSubKey(RegistryPath parent, string name, RegistryViewMode viewMode = RegistryViewMode.Default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        RejectPathSegment(name);

        using var root = OpenBaseKey(parent.Hive, viewMode);
        using var key = string.IsNullOrEmpty(parent.SubKey)
            ? root
            : root.OpenSubKey(parent.SubKey, writable: true) ?? throw new RegistryKeyNotFoundException(parent);

        using var created = key.CreateSubKey(name, writable: true);
    }

    public void CreateKey(RegistryPath path, RegistryViewMode viewMode = RegistryViewMode.Default)
    {
        if (string.IsNullOrEmpty(path.SubKey))
        {
            return;
        }

        using var root = OpenBaseKey(path.Hive, viewMode);
        using var created = root.CreateSubKey(path.SubKey, writable: true);
    }

    public void DeleteSubKeyTree(RegistryPath path, RegistryViewMode viewMode = RegistryViewMode.Default)
    {
        if (string.IsNullOrEmpty(path.SubKey))
        {
            throw new InvalidOperationException("Root hives cannot be deleted.");
        }

        var parentPath = GetParent(path);
        var leaf = GetLeafName(path);

        using var root = OpenBaseKey(parentPath.Hive, viewMode);
        using var parent = string.IsNullOrEmpty(parentPath.SubKey)
            ? root
            : root.OpenSubKey(parentPath.SubKey, writable: true) ?? throw new RegistryKeyNotFoundException(parentPath);

        parent.DeleteSubKeyTree(leaf, throwOnMissingSubKey: true);
    }

    public RegistryPath RenameSubKey(RegistryPath path, string newName, RegistryViewMode viewMode = RegistryViewMode.Default)
    {
        if (string.IsNullOrEmpty(path.SubKey))
        {
            throw new InvalidOperationException("Root hives cannot be renamed.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(newName);
        RejectPathSegment(newName);

        var parentPath = GetParent(path);
        var oldName = GetLeafName(path);
        if (oldName.Equals(newName, StringComparison.OrdinalIgnoreCase))
        {
            return path;
        }

        using var root = OpenBaseKey(parentPath.Hive, viewMode);
        using var parent = string.IsNullOrEmpty(parentPath.SubKey)
            ? root
            : root.OpenSubKey(parentPath.SubKey, writable: true) ?? throw new RegistryKeyNotFoundException(parentPath);

        using (var existing = parent.OpenSubKey(newName, writable: false))
        {
            if (existing is not null)
            {
                throw new InvalidOperationException($"A subkey named '{newName}' already exists.");
            }
        }

        using var source = parent.OpenSubKey(oldName, writable: false) ?? throw new RegistryKeyNotFoundException(path);
        using var destination = parent.CreateSubKey(newName, writable: true)
            ?? throw new InvalidOperationException($"Could not create renamed key '{newName}'.");
        CopyKeyTree(source, destination);
        parent.DeleteSubKeyTree(oldName, throwOnMissingSubKey: true);
        return Combine(parentPath, newName);
    }

    public void SetValue(
        RegistryPath path,
        string name,
        RegistryValueKind kind,
        object value,
        RegistryViewMode viewMode = RegistryViewMode.Default)
    {
        using var root = OpenBaseKey(path.Hive, viewMode);
        using var key = string.IsNullOrEmpty(path.SubKey)
            ? root
            : root.OpenSubKey(path.SubKey, writable: true) ?? throw new RegistryKeyNotFoundException(path);

        key.SetValue(name, value, kind);
    }

    public void DeleteValue(RegistryPath path, string name, RegistryViewMode viewMode = RegistryViewMode.Default)
    {
        using var root = OpenBaseKey(path.Hive, viewMode);
        using var key = string.IsNullOrEmpty(path.SubKey)
            ? root
            : root.OpenSubKey(path.SubKey, writable: true) ?? throw new RegistryKeyNotFoundException(path);

        key.DeleteValue(name, throwOnMissingValue: true);
    }

    public void ApplyImport(RegImportDocument document, RegistryViewMode viewMode = RegistryViewMode.Default)
    {
        foreach (var operation in document.Operations)
        {
            switch (operation.OperationKind)
            {
                case RegImportOperationKind.CreateKey:
                    CreateKey(operation.Path, viewMode);
                    break;
                case RegImportOperationKind.DeleteKey:
                    DeleteSubKeyTree(operation.Path, viewMode);
                    break;
                case RegImportOperationKind.SetValue:
                    SetValue(operation.Path, operation.ValueName ?? string.Empty, operation.ValueKind ?? RegistryValueKind.String, operation.ValueData ?? string.Empty, viewMode);
                    break;
                case RegImportOperationKind.DeleteValue:
                    DeleteValue(operation.Path, operation.ValueName ?? string.Empty, viewMode);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(document), $"Unknown import operation {operation.OperationKind}.");
            }
        }
    }

    public void RenameValue(
        RegistryPath path,
        string oldName,
        string newName,
        RegistryViewMode viewMode = RegistryViewMode.Default)
    {
        ArgumentNullException.ThrowIfNull(oldName);
        ArgumentNullException.ThrowIfNull(newName);

        if (oldName.Equals(newName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        using var root = OpenBaseKey(path.Hive, viewMode);
        using var key = string.IsNullOrEmpty(path.SubKey)
            ? root
            : root.OpenSubKey(path.SubKey, writable: true) ?? throw new RegistryKeyNotFoundException(path);

        if (key.GetValueNames().Any(name => name.Equals(newName, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"A value named '{newName}' already exists.");
        }

        var kind = key.GetValueKind(oldName);
        var data = key.GetValue(oldName, null, RegistryValueOptions.DoNotExpandEnvironmentNames)
            ?? throw new InvalidOperationException($"Value '{oldName}' has no data to rename.");
        key.SetValue(newName, data, kind);
        key.DeleteValue(oldName, throwOnMissingValue: true);
    }

    public RegistryPath LoadHive(
        RegistryHiveId mountHive,
        string mountName,
        string hiveFilePath,
        RegistryViewMode viewMode = RegistryViewMode.Default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mountName);
        ArgumentException.ThrowIfNullOrWhiteSpace(hiveFilePath);
        RejectPathSegment(mountName);

        if (mountHive is not (RegistryHiveId.LocalMachine or RegistryHiveId.Users))
        {
            throw new InvalidOperationException("Registry hives can only be loaded under HKEY_LOCAL_MACHINE or HKEY_USERS.");
        }

        EnablePrivilege("SeRestorePrivilege");
        EnablePrivilege("SeBackupPrivilege");
        var result = RegLoadKey(GetHiveHandle(mountHive), mountName, hiveFilePath);
        if (result != 0)
        {
            throw new Win32Exception(result, $"Could not load hive '{hiveFilePath}'.");
        }

        return new RegistryPath(mountHive, mountName);
    }

    public void UnloadHive(RegistryPath mountedHive)
    {
        if (mountedHive.Hive is not (RegistryHiveId.LocalMachine or RegistryHiveId.Users) || string.IsNullOrWhiteSpace(mountedHive.SubKey))
        {
            throw new InvalidOperationException("Only a loaded hive under HKEY_LOCAL_MACHINE or HKEY_USERS can be unloaded.");
        }

        if (mountedHive.SubKey.Contains('\\', StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Choose the mounted hive root before unloading.");
        }

        EnablePrivilege("SeRestorePrivilege");
        EnablePrivilege("SeBackupPrivilege");
        var result = RegUnLoadKey(GetHiveHandle(mountedHive.Hive), mountedHive.SubKey);
        if (result != 0)
        {
            throw new Win32Exception(result, $"Could not unload hive '{mountedHive}'.");
        }
    }

    public static RegistryPath GetParent(RegistryPath path)
    {
        var index = path.SubKey.LastIndexOf('\\');
        return index < 0 ? path with { SubKey = string.Empty } : path with { SubKey = path.SubKey[..index] };
    }

    public static string GetLeafName(RegistryPath path)
    {
        var index = path.SubKey.LastIndexOf('\\');
        return index < 0 ? path.SubKey : path.SubKey[(index + 1)..];
    }

    public string ExportReg(RegistryPath path, RegistryViewMode viewMode = RegistryViewMode.Default)
    {
        var snapshot = ReadKey(path, viewMode);
        var lines = new List<string>
        {
            "Windows Registry Editor Version 5.00",
            string.Empty,
            $"[{snapshot.Path}]"
        };

        foreach (var value in snapshot.Values)
        {
            lines.Add(FormatRegValue(value));
        }

        lines.Add(string.Empty);
        return string.Join(Environment.NewLine, lines);
    }

    public string ExportRegTree(RegistryPath path, RegistryViewMode viewMode = RegistryViewMode.Default)
    {
        var lines = new List<string>
        {
            "Windows Registry Editor Version 5.00",
            string.Empty
        };

        AppendKeyExport(lines, path, viewMode);
        return string.Join(Environment.NewLine, lines);
    }

    private void AppendKeyExport(List<string> lines, RegistryPath path, RegistryViewMode viewMode)
    {
        var snapshot = ReadKey(path, viewMode);
        lines.Add($"[{snapshot.Path}]");

        foreach (var value in snapshot.Values)
        {
            lines.Add(FormatRegValue(value));
        }

        lines.Add(string.Empty);

        foreach (var subKey in snapshot.SubKeyNames)
        {
            AppendKeyExport(lines, Combine(path, subKey), viewMode);
        }
    }

    private static void CopyKeyTree(RegistryKey source, RegistryKey destination)
    {
        foreach (var valueName in source.GetValueNames())
        {
            var kind = source.GetValueKind(valueName);
            var data = source.GetValue(valueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
            if (data is not null)
            {
                destination.SetValue(valueName, data, kind);
            }
        }

        foreach (var subKeyName in source.GetSubKeyNames())
        {
            using var childSource = source.OpenSubKey(subKeyName, writable: false);
            if (childSource is null)
            {
                continue;
            }

            using var childDestination = destination.CreateSubKey(subKeyName, writable: true);
            if (childDestination is not null)
            {
                CopyKeyTree(childSource, childDestination);
            }
        }
    }

    private static void RejectPathSegment(string value)
    {
        if (value.Contains('\\') || value.Contains('/'))
        {
            throw new ArgumentException("Registry key names cannot contain path separators.", nameof(value));
        }
    }

    private static bool IsSearchMatch(string value, RegistrySearchOptions options)
    {
        var comparison = options.MatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        return options.MatchWholeString
            ? value.Equals(options.Query, comparison)
            : value.Contains(options.Query, comparison);
    }

    private static RegistryKey OpenBaseKey(RegistryHiveId hive, RegistryViewMode viewMode)
    {
        return RegistryKey.OpenBaseKey(ToRegistryHive(hive), ToRegistryView(viewMode));
    }

    private static RegistryValueInfo ToValueInfo(RegistryKey key, string name)
    {
        var kind = key.GetValueKind(name);
        var data = key.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
        var displayName = string.IsNullOrEmpty(name) ? "(Default)" : name;
        return new RegistryValueInfo(name, displayName, kind.ToString(), data, FormatDisplayData(data));
    }

    private static string FormatDisplayData(object? data)
    {
        return data switch
        {
            null => "(value not set)",
            string value => value,
            string[] values => string.Join("; ", values),
            byte[] bytes => BitConverter.ToString(bytes).Replace("-", " "),
            IEnumerable values and not string => string.Join(", ", values.Cast<object>()),
            _ => Convert.ToString(data, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty
        };
    }

    private static string FormatRegValue(RegistryValueInfo value)
    {
        var name = string.IsNullOrEmpty(value.Name) ? "@" : Quote(value.Name);
        return value.Kind switch
        {
            nameof(RegistryValueKind.String) => $"{name}={Quote(value.DisplayData)}",
            nameof(RegistryValueKind.ExpandString) => $"{name}=hex(2):{ToHexWithUtf16Terminator(value.DisplayData)}",
            nameof(RegistryValueKind.DWord) => $"{name}=dword:{Convert.ToUInt32(value.Data):x8}",
            nameof(RegistryValueKind.QWord) => $"{name}=hex(b):{ToLittleEndianHex(Convert.ToUInt64(value.Data))}",
            nameof(RegistryValueKind.Binary) when value.Data is byte[] bytes => $"{name}=hex:{string.Join(',', bytes.Select(b => b.ToString("x2")))}",
            nameof(RegistryValueKind.MultiString) when value.Data is string[] values => $"{name}=hex(7):{ToHexWithUtf16Terminator(string.Join('\0', values) + '\0')}",
            _ => $"; {name} has unsupported export kind {value.Kind}: {value.DisplayData}"
        };
    }

    private static string Quote(string value)
    {
        return $"\"{value.Replace(@"\", @"\\").Replace("\"", "\\\"")}\"";
    }

    private static string ToLittleEndianHex(ulong value)
    {
        return string.Join(',', BitConverter.GetBytes(value).Select(b => b.ToString("x2")));
    }

    private static string ToHexWithUtf16Terminator(string value)
    {
        var bytes = System.Text.Encoding.Unicode.GetBytes(value + '\0');
        return string.Join(',', bytes.Select(b => b.ToString("x2")));
    }

    private static RegistryHive ToRegistryHive(RegistryHiveId hive)
    {
        return hive switch
        {
            RegistryHiveId.ClassesRoot => RegistryHive.ClassesRoot,
            RegistryHiveId.CurrentUser => RegistryHive.CurrentUser,
            RegistryHiveId.LocalMachine => RegistryHive.LocalMachine,
            RegistryHiveId.Users => RegistryHive.Users,
            RegistryHiveId.CurrentConfig => RegistryHive.CurrentConfig,
            RegistryHiveId.PerformanceData => RegistryHive.PerformanceData,
            _ => throw new ArgumentOutOfRangeException(nameof(hive), hive, null)
        };
    }

    private static RegistryView ToRegistryView(RegistryViewMode viewMode)
    {
        return viewMode switch
        {
            RegistryViewMode.Default => RegistryView.Default,
            RegistryViewMode.Registry64 => RegistryView.Registry64,
            RegistryViewMode.Registry32 => RegistryView.Registry32,
            _ => throw new ArgumentOutOfRangeException(nameof(viewMode), viewMode, null)
        };
    }

    private static IntPtr GetHiveHandle(RegistryHiveId hive)
    {
        return hive switch
        {
            RegistryHiveId.LocalMachine => new IntPtr(unchecked((int)0x80000002)),
            RegistryHiveId.Users => new IntPtr(unchecked((int)0x80000003)),
            _ => throw new ArgumentOutOfRangeException(nameof(hive), hive, null)
        };
    }

    private static void EnablePrivilege(string privilegeName)
    {
        if (!OpenProcessToken(GetCurrentProcess(), TokenAdjustPrivileges | TokenQuery, out var token))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not open the process token.");
        }

        try
        {
            if (!LookupPrivilegeValue(null, privilegeName, out var luid))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), $"Could not look up {privilegeName}.");
            }

            var privileges = new TokenPrivileges
            {
                PrivilegeCount = 1,
                Luid = luid,
                Attributes = PrivilegeEnabled
            };

            if (!AdjustTokenPrivileges(token, false, ref privileges, 0, IntPtr.Zero, IntPtr.Zero))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), $"Could not enable {privilegeName}.");
            }
        }
        finally
        {
            CloseHandle(token);
        }
    }

    private const uint TokenAdjustPrivileges = 0x0020;
    private const uint TokenQuery = 0x0008;
    private const uint PrivilegeEnabled = 0x00000002;

    [StructLayout(LayoutKind.Sequential)]
    private struct Luid
    {
        public uint LowPart;
        public int HighPart;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TokenPrivileges
    {
        public uint PrivilegeCount;
        public Luid Luid;
        public uint Attributes;
    }

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int RegLoadKey(IntPtr hKey, string lpSubKey, string lpFile);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int RegUnLoadKey(IntPtr hKey, string lpSubKey);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool OpenProcessToken(IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool LookupPrivilegeValue(string? systemName, string name, out Luid luid);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool AdjustTokenPrivileges(IntPtr tokenHandle, bool disableAllPrivileges, ref TokenPrivileges newState, uint bufferLength, IntPtr previousState, IntPtr returnLength);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentProcess();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr handle);
}
