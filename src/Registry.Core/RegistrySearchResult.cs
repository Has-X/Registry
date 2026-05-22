namespace Registry.Core;

public enum RegistrySearchMatchKind
{
    KeyName,
    ValueName,
    ValueData
}

public sealed record RegistrySearchResult(
    RegistryPath Path,
    RegistrySearchMatchKind MatchKind,
    string? ValueName,
    string DisplayText);
