using System.Collections.Concurrent;
using Registry.Core;

namespace Registry_App;

public sealed class RegistryReadCache
{
    private readonly RegistryBrowser _browser;
    private readonly ConcurrentDictionary<RegistryCacheKey, Task<RegistryKeySummary>> _summaries = [];
    private readonly ConcurrentDictionary<RegistryCacheKey, Task<IReadOnlyList<string>>> _subKeyNames = [];

    public RegistryReadCache(RegistryBrowser browser)
    {
        _browser = browser;
    }

    public Task<RegistryKeySummary> ReadSummaryAsync(RegistryPath path, RegistryViewMode viewMode)
    {
        var key = RegistryCacheKey.Create(path, viewMode);
        return GetOrRemoveFaultedAsync(
            _summaries,
            key,
            () => _browser.ReadKeySummary(path, viewMode));
    }

    public Task<IReadOnlyList<string>> GetSubKeyNamesAsync(RegistryPath path, RegistryViewMode viewMode)
    {
        var key = RegistryCacheKey.Create(path, viewMode);
        return GetOrRemoveFaultedAsync(
            _subKeyNames,
            key,
            () => _browser.GetSubKeyNames(path, viewMode));
    }

    public void Clear()
    {
        _summaries.Clear();
        _subKeyNames.Clear();
    }

    private static async Task<T> GetOrRemoveFaultedAsync<T>(
        ConcurrentDictionary<RegistryCacheKey, Task<T>> cache,
        RegistryCacheKey key,
        Func<T> load)
    {
        var task = cache.GetOrAdd(key, _ => Task.Run(load));
        try
        {
            return await task.ConfigureAwait(false);
        }
        catch
        {
            cache.TryRemove(key, out _);
            throw;
        }
    }

    private readonly record struct RegistryCacheKey(string Path, RegistryViewMode ViewMode)
    {
        public static RegistryCacheKey Create(RegistryPath path, RegistryViewMode viewMode)
        {
            return new RegistryCacheKey(path.ToString().ToUpperInvariant(), viewMode);
        }
    }
}
