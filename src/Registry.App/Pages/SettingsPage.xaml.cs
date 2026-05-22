using Microsoft.UI.Xaml.Controls;
using Registry_App;

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
        _loading = false;
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

    private void ToolbarDetailSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading)
        {
            return;
        }

        AppSettings.ToolbarDetailIndex = ToolbarDetailSelector.SelectedIndex;
    }

    private void RegisterRegOpenWith_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        try
        {
            RegOpenWithRegistration.Register();
            IntegrationStatus.Title = "Registered";
            IntegrationStatus.Message = "Registry was added to the Open with list for .reg files.";
            IntegrationStatus.Severity = InfoBarSeverity.Success;
            IntegrationStatus.IsOpen = true;
        }
        catch (Exception ex)
        {
            IntegrationStatus.Title = "Registration failed";
            IntegrationStatus.Message = ex.Message;
            IntegrationStatus.Severity = InfoBarSeverity.Error;
            IntegrationStatus.IsOpen = true;
        }
    }

}
