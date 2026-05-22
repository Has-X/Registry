using Registry.Core;

namespace Registry_App.Pages;

public sealed class RegistryLocationRow
{
    public RegistryLocationRow(string name, RegistryPath path)
    {
        Name = name;
        Path = path;
        PathText = path.ToString();
    }

    public string Name { get; }

    public RegistryPath Path { get; }

    public string PathText { get; }
}
