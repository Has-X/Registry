namespace Registry.Core;

public sealed record RegistryPermissionEntry(
    string Identity,
    string Rights,
    string AccessType,
    bool IsInherited,
    string InheritanceFlags,
    string PropagationFlags);
