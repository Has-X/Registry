namespace Registry.Core;

public sealed record RegistryKeySummary(
    RegistryPath Path,
    int SubKeyCount,
    int ValueCount,
    IReadOnlyList<RegistryValueInfo> Values);
