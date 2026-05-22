namespace Registry.Core;

public sealed class RegistryKeyNotFoundException : Exception
{
    public RegistryKeyNotFoundException(RegistryPath path)
        : base($"Registry key '{path}' was not found.")
    {
        Path = path;
    }

    public RegistryPath Path { get; }
}
