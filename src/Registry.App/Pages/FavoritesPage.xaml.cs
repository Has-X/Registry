using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Registry.Core;
using Windows.ApplicationModel.DataTransfer;

namespace Registry_App.Pages;

public sealed partial class FavoritesPage : Page
{
    public static event EventHandler<RegistryPath>? FavoriteOpenRequested;

    public FavoritesPage()
    {
        InitializeComponent();
        Loaded += FavoritesPage_Loaded;
        Unloaded += FavoritesPage_Unloaded;
    }

    private void FavoritesPage_Loaded(object sender, RoutedEventArgs e)
    {
        RegistryFavoriteStore.FavoritesChanged += RegistryFavoriteStore_FavoritesChanged;
        LoadFavorites();
    }

    private void FavoritesPage_Unloaded(object sender, RoutedEventArgs e)
    {
        RegistryFavoriteStore.FavoritesChanged -= RegistryFavoriteStore_FavoritesChanged;
    }

    private void RegistryFavoriteStore_FavoritesChanged(object? sender, EventArgs e)
    {
        LoadFavorites();
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        LoadFavorites();
    }

    private void FavoritesList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is RegistryLocationRow row)
        {
            FavoriteOpenRequested?.Invoke(this, row.Path);
        }
    }

    private void OpenFavorite_Click(object sender, RoutedEventArgs e)
    {
        if (TryGetPath(sender, out var path))
        {
            FavoriteOpenRequested?.Invoke(this, path);
        }
    }

    private void CopyFavorite_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetPath(sender, out var path))
        {
            return;
        }

        var package = new DataPackage();
        package.SetText(path.ToString());
        Clipboard.SetContent(package);
    }

    private void RemoveFavorite_Click(object sender, RoutedEventArgs e)
    {
        if (TryGetPath(sender, out var path))
        {
            RegistryFavoriteStore.Remove(path);
        }
    }

    private void LoadFavorites()
    {
        var rows = RegistryFavoriteStore.GetFavorites()
            .Select(path => new RegistryLocationRow(GetLocationName(path), path))
            .ToArray();

        FavoritesList.ItemsSource = rows;
        EmptyState.Visibility = rows.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        FavoriteCountText.Text = rows.Length switch
        {
            0 => "No saved keys",
            1 => "1 saved key",
            _ => $"{rows.Length:N0} saved keys"
        };
    }

    private static bool TryGetPath(object sender, out RegistryPath path)
    {
        if (sender is FrameworkElement { Tag: string pathText })
        {
            path = RegistryPath.Parse(pathText);
            return true;
        }

        path = new RegistryPath(RegistryHiveId.CurrentUser, string.Empty);
        return false;
    }

    private static string GetLocationName(RegistryPath path)
    {
        if (string.IsNullOrEmpty(path.SubKey))
        {
            return path.ToString();
        }

        var leaf = RegistryBrowser.GetLeafName(path);
        return string.IsNullOrEmpty(leaf) ? path.ToString() : leaf;
    }
}
