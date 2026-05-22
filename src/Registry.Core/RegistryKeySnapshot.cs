namespace Registry.Core;

public sealed record RegistryKeySnapshot(
    RegistryPath Path,
    IReadOnlyList<string> SubKeyNames,
    IReadOnlyList<RegistryValueInfo> Values);
