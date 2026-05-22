namespace Registry.Core;

public sealed record RegistryValueInfo(
    string Name,
    string DisplayName,
    string Kind,
    object? Data,
    string DisplayData);
