using Microsoft.UI.Windowing;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Registry_App.Pages;
using Windows.Storage;
using Windows.UI;

namespace Registry_App;

public sealed partial class MainWindow : Window
{
    private HomePage? _homePage;
    private string? _appliedBackdropStyle;

    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        AppWindow.SetIcon("Assets/AppIcon.ico");
        ApplyBackdrop();
        ApplyNavigationPaneSurface();
        AppSettings.Changed += AppSettings_Changed;
        FavoritesPage.FavoriteOpenRequested += FavoritesPage_FavoriteOpenRequested;
        NavigateHome();
    }

    private void AppSettings_Changed(object? sender, EventArgs e)
    {
        ApplyBackdrop();
        ApplyNavigationPaneSurface();
    }

    private void ApplyBackdrop()
    {
        if (string.Equals(_appliedBackdropStyle, AppSettings.BackdropStyle, StringComparison.Ordinal))
        {
            return;
        }

        _appliedBackdropStyle = AppSettings.BackdropStyle;
        SystemBackdrop = AppSettings.BackdropStyle switch
        {
            "StrongMica" => new MicaBackdrop { Kind = MicaKind.BaseAlt },
            "Off" => null,
            _ => new MicaBackdrop { Kind = MicaKind.Base }
        };
    }

    private void ApplyNavigationPaneSurface()
    {
        Brush paneBrush = AppSettings.BackdropStyle == "Off"
            ? new SolidColorBrush(Color.FromArgb(255, 31, 31, 34))
            : new AcrylicBrush
            {
                FallbackColor = Color.FromArgb(208, 31, 31, 34),
                TintColor = Color.FromArgb(255, 32, 29, 35),
                TintOpacity = 0.42,
                TintLuminosityOpacity = 0.68
            };

        NavView.Resources["NavigationViewDefaultPaneBackground"] = paneBrush;
        NavView.Resources["NavigationViewExpandedPaneBackground"] = paneBrush;
        NavView.Resources["NavigationViewTopPaneBackground"] = paneBrush;
        NavView.Resources["NavigationViewMinimalPaneBackground"] = paneBrush;
    }

    private void TitleBar_PaneToggleRequested(TitleBar sender, object args)
    {
        NavView.IsPaneOpen = !NavView.IsPaneOpen;
    }

    private void TitleBar_BackRequested(TitleBar sender, object args)
    {
        NavFrame.GoBack();
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            NavFrame.Navigate(typeof(SettingsPage));
        }
        else if (args.SelectedItem is NavigationViewItem item)
        {
            switch (item.Tag)
            {
                case "home":
                    NavigateHome();
                    break;
                case "favorites":
                    NavFrame.Navigate(typeof(FavoritesPage));
                    break;
                case "journal":
                    NavFrame.Navigate(typeof(JournalPage));
                    break;
                case "about":
                    NavFrame.Navigate(typeof(AboutPage));
                    break;
                default:
                    var tag = item.Tag?.ToString() ?? string.Empty;
                    if (tag.StartsWith("goto:", StringComparison.Ordinal))
                    {
                        NavigateHome(tag["goto:".Length..]);
                    }
                    else
                    {
                        throw new InvalidOperationException($"Unknown navigation item tag: {item.Tag}");
                    }

                    break;
            }
        }
    }

    private void NavigateHome(string? path = null)
    {
        if (NavFrame.Content is not HomePage homePage)
        {
            NavFrame.Navigate(typeof(HomePage));
            homePage = (HomePage)NavFrame.Content;
            _homePage = homePage;
        }

        _homePage ??= homePage;

        if (!string.IsNullOrWhiteSpace(path))
        {
            _homePage.NavigateTo(path);
        }
    }

    private void FavoritesPage_FavoriteOpenRequested(object? sender, Registry.Core.RegistryPath path)
    {
        SelectNavigationItem("home");
        NavigateHome(path.ToString());
    }

    public void OpenRegistryFile(StorageFile file)
    {
        SelectNavigationItem("home");
        NavigateHome();
        _homePage?.ImportRegistryFile(file);
    }

    public async void OpenRegistryFilePath(string path)
    {
        try
        {
            var file = await StorageFile.GetFileFromPathAsync(path);
            OpenRegistryFile(file);
        }
        catch
        {
            // File activation should never prevent the app shell from opening.
        }
    }

    private void SelectNavigationItem(string tag)
    {
        foreach (var item in NavView.MenuItems.OfType<NavigationViewItem>())
        {
            item.IsSelected = string.Equals(item.Tag?.ToString(), tag, StringComparison.Ordinal);
        }
    }
}
