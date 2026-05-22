using Registry.Core;
using System.ComponentModel;
using Microsoft.UI.Xaml;

namespace Registry_App.Pages;

public sealed class RegistryValueRow : INotifyPropertyChanged
{
    public RegistryValueRow(RegistryValueInfo value)
    {
        Name = value.Name;
        DisplayName = value.DisplayName;
        Kind = value.Kind;
        DisplayData = value.DisplayData;
        Raw = value;
    }

    public string Name { get; }

    public string DisplayName { get; }

    public string Kind { get; }

    public string DisplayData { get; }

    public RegistryValueInfo Raw { get; }

    public GridLength NameColumnWidth { get; private set; } = new(120);

    public GridLength TypeColumnWidth { get; private set; } = new(84);

    public GridLength DataColumnWidth { get; private set; } = new(1, GridUnitType.Star);

    public event PropertyChangedEventHandler? PropertyChanged;

    public void SetColumnWidths(double nameWidth, double typeWidth)
    {
        NameColumnWidth = new GridLength(nameWidth);
        TypeColumnWidth = new GridLength(typeWidth);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NameColumnWidth)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TypeColumnWidth)));
    }
}
