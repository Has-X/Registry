using Registry.Core;
using Windows.Storage;

namespace Registry_App;

public static class RegistryFavoriteStore
{
    private const string FavoritesSettingKey = "Favorites";

    public static event EventHandler? FavoritesChanged;

    public static IReadOnlyList<RegistryPath> GetFavorites()
    {
        if (ApplicationData.Current.LocalSettings.Values[FavoritesSettingKey] is not string payload)
        {
            return [];
        }

        var favorites = new List<RegistryPath>();
        foreach (var line in payload.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            try
            {
                favorites.Add(RegistryPath.Parse(line));
            }
            catch (FormatException)
            {
            }
        }

        return favorites;
    }

    public static bool Add(RegistryPath path)
    {
        var favorites = GetFavorites().ToList();
        if (favorites.Any(candidate => candidate.ToString().Equals(path.ToString(), StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        favorites.Insert(0, path);
        Save(favorites);
        FavoritesChanged?.Invoke(null, EventArgs.Empty);
        return true;
    }

    public static bool Remove(RegistryPath path)
    {
        var favorites = GetFavorites().ToList();
        var removed = favorites.RemoveAll(candidate => candidate.ToString().Equals(path.ToString(), StringComparison.OrdinalIgnoreCase)) > 0;
        if (!removed)
        {
            return false;
        }

        Save(favorites);
        FavoritesChanged?.Invoke(null, EventArgs.Empty);
        return true;
    }

    private static void Save(IReadOnlyList<RegistryPath> favorites)
    {
        ApplicationData.Current.LocalSettings.Values[FavoritesSettingKey] = string.Join('\n', favorites.Select(path => path.ToString()));
    }
}
