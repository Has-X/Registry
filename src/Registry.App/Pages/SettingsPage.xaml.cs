using Microsoft.UI.Xaml.Controls;
using Registry_App;
using Registry_App.Services;

namespace Registry_App.Pages;

public sealed partial class SettingsPage : Page
{
    private bool _loading;

    public SettingsPage()
    {
        InitializeComponent();
        _loading = true;
        ToolbarAlignmentSelector.SelectedIndex = AppSettings.ToolbarAlignmentIndex;
        ToolbarDetailSelector.SelectedIndex = AppSettings.ToolbarDetailIndex;
        BackdropSelector.SelectedIndex = AppSettings.BackdropStyleIndex;
        RegImportModeSelector.SelectedIndex = AppSettings.RegImportModeIndex;
        _loading = false;
        Loaded += SettingsPage_Loaded;
    }

    private void SettingsPage_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _loading = true;
        LocalizationService.Apply(this);
        ApplyLocalizedSelectorItems(ToolbarAlignmentSelector);
        ApplyLocalizedSelectorItems(ToolbarDetailSelector);
        ApplyLocalizedSelectorItems(BackdropSelector);
        ApplyLocalizedSelectorItems(RegImportModeSelector);
        _loading = false;
    }

    private static void ApplyLocalizedSelectorItems(ComboBox selector)
    {
        var selectedIndex = selector.SelectedIndex;
        foreach (var item in selector.Items.OfType<ComboBoxItem>())
        {
            if (item.Tag is string key)
            {
                item.Content = LocalizationService.Get(key, item.Content?.ToString() ?? string.Empty);
            }
        }

        // WinUI caches the closed selection box from the pre-localized content.
        // Rebinding the selection refreshes that cache without changing the setting.
        if (selectedIndex >= 0)
        {
            selector.SelectedIndex = -1;
            selector.SelectedIndex = selectedIndex;
        }
    }

    private void ToolbarAlignmentSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading)
        {
            return;
        }

        AppSettings.ToolbarAlignmentIndex = ToolbarAlignmentSelector.SelectedIndex;
    }

    private void BackdropSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading)
        {
            return;
        }

        AppSettings.BackdropStyleIndex = BackdropSelector.SelectedIndex;
    }

    private void RegImportModeSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_loading && RegImportModeSelector is not null && RegImportModeSelector.SelectedIndex >= 0)
        {
            AppSettings.RegImportModeIndex = RegImportModeSelector.SelectedIndex;
        }
    }

    private void ToolbarDetailSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading)
        {
            return;
        }

        AppSettings.ToolbarDetailIndex = ToolbarDetailSelector.SelectedIndex;
    }

}
