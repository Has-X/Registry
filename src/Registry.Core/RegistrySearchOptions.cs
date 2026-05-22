namespace Registry.Core;

public sealed record RegistrySearchOptions(
    string Query,
    bool MatchKeys = true,
    bool MatchValueNames = true,
    bool MatchValueData = true,
    bool MatchCase = false,
    bool MatchWholeString = false);
