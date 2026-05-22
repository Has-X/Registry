namespace Registry.Core;

public sealed record RegistryPermissionSummary(
    RegistryPath Path,
    string Owner,
    IReadOnlyList<RegistryPermissionEntry> Rules);
