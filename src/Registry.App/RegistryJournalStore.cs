using Registry.Core;

namespace Registry_App;

public sealed record RegistryJournalEntry(
    DateTimeOffset CreatedAt,
    RegistryPath Path,
    RegistryViewMode ViewMode,
    string ActionName,
    string RegText);

public static class RegistryJournalStore
{
    private const int Limit = 20;
    private static readonly List<RegistryJournalEntry> Entries = [];

    public static event EventHandler? Changed;

    public static IReadOnlyList<RegistryJournalEntry> GetEntries()
    {
        return Entries
            .OrderByDescending(entry => entry.CreatedAt)
            .ToArray();
    }

    public static void Add(RegistryJournalEntry entry)
    {
        Entries.Add(entry);
        if (Entries.Count > Limit)
        {
            Entries.RemoveRange(0, Entries.Count - Limit);
        }

        Changed?.Invoke(null, EventArgs.Empty);
    }

    public static bool Remove(RegistryJournalEntry entry)
    {
        var removed = Entries.Remove(entry);
        if (removed)
        {
            Changed?.Invoke(null, EventArgs.Empty);
        }

        return removed;
    }
}
