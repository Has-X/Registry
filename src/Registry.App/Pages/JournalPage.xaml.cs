using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Registry.Core;
using Registry_App;

namespace Registry_App.Pages;

public sealed partial class JournalPage : Page
{
    private readonly RegistryBrowser _browser = new();

    public JournalPage()
    {
        InitializeComponent();
        Loaded += JournalPage_Loaded;
        Unloaded += JournalPage_Unloaded;
        RegistryJournalStore.Changed += RegistryJournalStore_Changed;
    }

    private void JournalPage_Loaded(object sender, RoutedEventArgs e)
    {
        RefreshRows();
    }

    private void JournalPage_Unloaded(object sender, RoutedEventArgs e)
    {
        RegistryJournalStore.Changed -= RegistryJournalStore_Changed;
    }

    private void RegistryJournalStore_Changed(object? sender, EventArgs e)
    {
        DispatcherQueue.TryEnqueue(RefreshRows);
    }

    private void RefreshRows()
    {
        var rows = RegistryJournalStore.GetEntries()
            .Select(entry => new JournalRow(entry))
            .ToArray();

        JournalList.ItemsSource = rows;
        JournalList.Visibility = rows.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
        EmptyState.Visibility = rows.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        JournalCountText.Text = rows.Length == 1 ? "1 snapshot" : $"{rows.Length} snapshots";
    }

    private async void Restore_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: RegistryJournalEntry entry })
        {
            return;
        }

        try
        {
            if (!await ConfirmRestoreAsync(entry))
            {
                return;
            }

            var document = RegImportParser.Parse(entry.RegText);
            await Task.Run(() => _browser.ApplyImport(document, entry.ViewMode));
            RegistryJournalStore.Remove(entry);
            JournalStatus.Title = "Restored";
            JournalStatus.Message = entry.Path.ToString();
            JournalStatus.Severity = InfoBarSeverity.Success;
            JournalStatus.IsOpen = true;
        }
        catch (Exception ex)
        {
            JournalStatus.Title = "Restore failed";
            JournalStatus.Message = ex.Message;
            JournalStatus.Severity = InfoBarSeverity.Error;
            JournalStatus.IsOpen = true;
        }
    }

    private async void Remove_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: RegistryJournalEntry entry })
        {
            return;
        }

        if (!await ConfirmRemoveAsync(entry))
        {
            return;
        }

        RegistryJournalStore.Remove(entry);
        JournalStatus.Title = "Removed";
        JournalStatus.Message = "The snapshot was removed from Journal.";
        JournalStatus.Severity = InfoBarSeverity.Informational;
        JournalStatus.IsOpen = true;
    }

    private async Task<bool> ConfirmRestoreAsync(RegistryJournalEntry entry)
    {
        var content = new StackPanel
        {
            Spacing = 10,
            Children =
            {
                new TextBlock
                {
                    Text = entry.Path.ToString(),
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    TextTrimming = TextTrimming.CharacterEllipsis
                },
                new TextBlock
                {
                    Text = "This will write the captured snapshot back to the registry. Current values in that snapshot scope may be replaced.",
                    TextWrapping = TextWrapping.Wrap
                }
            }
        };

        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = "Restore journal snapshot?",
            Content = content,
            PrimaryButtonText = "Restore",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close
        };

        return await dialog.ShowAsync() == ContentDialogResult.Primary;
    }

    private async Task<bool> ConfirmRemoveAsync(RegistryJournalEntry entry)
    {
        var content = new StackPanel
        {
            Spacing = 10,
            Children =
            {
                new TextBlock
                {
                    Text = entry.Path.ToString(),
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    TextTrimming = TextTrimming.CharacterEllipsis
                },
                new TextBlock
                {
                    Text = "This only removes the saved snapshot from Journal. It does not change the registry.",
                    TextWrapping = TextWrapping.Wrap
                }
            }
        };

        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = "Remove journal snapshot?",
            Content = content,
            PrimaryButtonText = "Remove",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close
        };

        return await dialog.ShowAsync() == ContentDialogResult.Primary;
    }
}

public sealed class JournalRow
{
    public JournalRow(RegistryJournalEntry entry)
    {
        Entry = entry;
    }

    public RegistryJournalEntry Entry { get; }

    public string ActionName => Entry.ActionName;

    public string Path => Entry.Path.ToString();

    public string CreatedText => Entry.CreatedAt.LocalDateTime.ToString("g");
}
