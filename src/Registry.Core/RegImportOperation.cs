using Microsoft.Win32;

namespace Registry.Core;

public enum RegImportOperationKind
{
    CreateKey,
    DeleteKey,
    SetValue,
    DeleteValue
}

public sealed record RegImportOperation(
    RegImportOperationKind OperationKind,
    RegistryPath Path,
    string? ValueName = null,
    RegistryValueKind? ValueKind = null,
    object? ValueData = null);
