using Registry.Core;

namespace Registry_App.Pages;

public sealed class RegistryTreeItem
{
    public RegistryTreeItem(RegistryPath path, string name, int? subKeyCount = null, int? valueCount = null, bool isLoadMore = false)
    {
        Path = path;
        Name = name;
        SubKeyCount = subKeyCount;
        ValueCount = valueCount;
        IsLoadMore = isLoadMore;
    }

    public RegistryPath Path { get; }

    public string Name { get; }

    public int? SubKeyCount { get; }

    public int? ValueCount { get; }

    public bool IsLoadMore { get; }

    public override string ToString()
    {
        return Name;
    }
}
