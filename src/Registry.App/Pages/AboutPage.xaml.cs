using Microsoft.UI.Xaml.Controls;
using System.Reflection;
using Windows.ApplicationModel;
using Windows.System;
using Registry_App.Services;

namespace Registry_App.Pages;

public sealed partial class AboutPage : Page
{
    public AboutPage()
    {
        InitializeComponent();
        Loaded += (_, _) => LocalizationService.Apply(this);
        VersionText.Text = $"Version {GetVersionText()}";
    }

    private static string GetVersionText()
    {
        try
        {
            var version = Package.Current.Id.Version;
            return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }
        catch
        {
            return Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0.0";
        }
    }

    private async void WebsiteButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await Launcher.LaunchUriAsync(new Uri("https://chromatic.hu"));
    }

    private async void FeedbackButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await Launcher.LaunchUriAsync(new Uri("mailto:feedback@chromatic.hu"));
    }
}
