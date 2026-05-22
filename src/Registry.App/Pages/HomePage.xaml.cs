using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.Win32;
using Registry.Core;
using Registry_App;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using WinRT.Interop;

namespace Registry_App.Pages;

public sealed partial class HomePage : Page
{
    private const int RecentLimit = 12;
    private const int TreePageSize = 120;
    private readonly RegistryBrowser _browser = new();
    private readonly RegistryReadCache _readCache;
    private readonly List<RegistryPath> _recent = [];
    private readonly List<RegistryPath> _backStack = [];
    private readonly List<RegistryPath> _forwardStack = [];
    private readonly Dictionary<TreeViewNode, RegistryTreeItem> _treeItems = [];
    private readonly Dictionary<TreeViewNode, LoadMoreState> _loadMoreItems = [];
    private readonly DispatcherTimer _filterTimer = new();
    private readonly DispatcherTimer _monitorTimer = new();
    private RegistryPath? _currentPath;
    private RegistryKeySummary? _currentSnapshot;
    private string? _monitorSignature;
    private RegistrySearchOptions? _lastSearchOptions;
    private RegistryPath? _initialPath;
    private bool _isHistoryNavigation;
    private bool _isMonitorReading;
    private int _refreshVersion;
    private double _valueNameColumnWidth = 120;
    private double _valueTypeColumnWidth = 84;
    private bool _isInitialized;

    public HomePage()
    {
        _readCache = new RegistryReadCache(_browser);
        _filterTimer.Interval = TimeSpan.FromMilliseconds(180);
        _filterTimer.Tick += FilterTimer_Tick;
        _monitorTimer.Interval = TimeSpan.FromSeconds(2);
        _monitorTimer.Tick += MonitorTimer_Tick;
        InitializeComponent();
        Loaded += HomePage_Loaded;
        Unloaded += HomePage_Unloaded;
    }

    private void HomePage_Loaded(object sender, RoutedEventArgs e)
    {
        AppSettings.Changed -= AppSettings_Changed;
        AppSettings.Changed += AppSettings_Changed;
        RegistryFavoriteStore.FavoritesChanged -= RegistryFavoriteStore_FavoritesChanged;
        RegistryFavoriteStore.FavoritesChanged += RegistryFavoriteStore_FavoritesChanged;

        ApplyToolbarAlignment();
        ApplyToolbarDetail();
        UpdateCommandState();

        if (!_isInitialized)
        {
            _isInitialized = true;
            LoadRoots();
        }
    }

    private void HomePage_Unloaded(object sender, RoutedEventArgs e)
    {
        _monitorTimer.Stop();
        AppSettings.Changed -= AppSettings_Changed;
        RegistryFavoriteStore.FavoritesChanged -= RegistryFavoriteStore_FavoritesChanged;
    }

    private void AppSettings_Changed(object? sender, EventArgs e)
    {
        ApplyToolbarAlignment();
        ApplyToolbarDetail();
    }

    private void RegistryFavoriteStore_FavoritesChanged(object? sender, EventArgs e)
    {
        UpdateCommandState();
    }

    public void NavigateTo(string path)
    {
        try
        {
            var registryPath = RegistryPath.Parse(path);
            if (KeyTree.RootNodes.Count == 0)
            {
                _initialPath = registryPath;
                return;
            }

            SelectPath(registryPath);
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException)
        {
            ShowStatus("Invalid path", ex.Message, InfoBarSeverity.Error);
        }
    }

    private void LoadRoots()
    {
        KeyTree.RootNodes.Clear();
        _treeItems.Clear();
        _loadMoreItems.Clear();

        foreach (var root in _browser.GetRootPaths())
        {
            var node = CreateNode(root, root.ToString(), mayHaveChildren: true);
            KeyTree.RootNodes.Add(node);
        }

        var pathToSelect = _initialPath ?? _currentPath;
        if (pathToSelect is not null)
        {
            _initialPath = null;
            SelectPath(pathToSelect);
        }
        else if (KeyTree.RootNodes.Count > 0)
        {
            SelectPath(_treeItems[KeyTree.RootNodes[0]].Path);
        }
    }

    private TreeViewNode CreateNode(RegistryPath path, string label, bool mayHaveChildren)
    {
        var item = new RegistryTreeItem(path, label);
        var node = new TreeViewNode
        {
            Content = item.Name,
            HasUnrealizedChildren = mayHaveChildren
        };
        _treeItems[node] = item;
        return node;
    }

    private TreeViewNode CreateLoadMoreNode(TreeViewNode parent, RegistryPath parentPath, IReadOnlyList<string> names, int nextIndex)
    {
        var remaining = names.Count - nextIndex;
        var item = new RegistryTreeItem(parentPath, $"Load {Math.Min(TreePageSize, remaining):N0} more... ({remaining:N0} remaining)", isLoadMore: true);
        var node = new TreeViewNode
        {
            Content = item.Name,
            HasUnrealizedChildren = false
        };
        _treeItems[node] = item;
        _loadMoreItems[node] = new LoadMoreState(parent, parentPath, names, nextIndex);
        return node;
    }

    private async void KeyTree_Expanding(TreeView sender, TreeViewExpandingEventArgs args)
    {
        if (!_treeItems.TryGetValue(args.Node, out var item) || args.Node.Children.Count > 0)
        {
            return;
        }

        try
        {
            var view = GetCurrentView();
            UpdateStatus($"Loading children for {item.Path}");
            var subKeyNames = await _readCache.GetSubKeyNamesAsync(item.Path, view);
            item = new RegistryTreeItem(item.Path, item.Name, subKeyNames.Count, null);
            _treeItems[args.Node] = item;
            args.Node.Content = FormatTreeNodeLabel(item);
            args.Node.HasUnrealizedChildren = subKeyNames.Count > 0;
            AppendTreeChildrenPage(args.Node, item.Path, subKeyNames, 0);
        }
        catch (Exception ex) when (IsRecoverableRegistryException(ex))
        {
            args.Node.HasUnrealizedChildren = false;
            ShowStatus("Unable to expand key", ex.Message, InfoBarSeverity.Warning);
        }
    }

    private void KeyTree_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
    {
        if (args.InvokedItem is TreeViewNode node && _treeItems.TryGetValue(node, out var item))
        {
            if (item.IsLoadMore)
            {
                LoadMoreTreeChildren(node);
                return;
            }

            SelectPath(item.Path);
        }
        else if (args.InvokedItem is string label)
        {
            var matchingNode = FindNode(KeyTree.RootNodes, candidate => candidate.Content?.ToString() == label);
            if (matchingNode is not null && _treeItems.TryGetValue(matchingNode, out var labelItem))
            {
                if (labelItem.IsLoadMore)
                {
                    LoadMoreTreeChildren(matchingNode);
                    return;
                }

                SelectPath(labelItem.Path);
            }
        }
    }

    private void KeyTree_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
    {
        if (!TryGetTreePathFromContext(args, out var targetPath))
        {
            return;
        }

        ShowCurrentKeyContextMenu(sender, GetFlyoutOptions(sender, args), targetPath, includeCreate: true);
        args.Handled = true;
    }

    private static string FormatTreeNodeLabel(RegistryTreeItem item)
    {
        if (item.IsLoadMore)
        {
            return item.Name;
        }

        return item.SubKeyCount is null
            ? item.Name
            : $"{item.Name} ({item.SubKeyCount} keys)";
    }

    private void AppendTreeChildrenPage(TreeViewNode parent, RegistryPath parentPath, IReadOnlyList<string> names, int startIndex)
    {
        var endIndex = Math.Min(startIndex + TreePageSize, names.Count);
        for (var index = startIndex; index < endIndex; index++)
        {
            var name = names[index];
            parent.Children.Add(CreateNode(_browser.Combine(parentPath, name), name, mayHaveChildren: true));
        }

        if (endIndex < names.Count)
        {
            parent.Children.Add(CreateLoadMoreNode(parent, parentPath, names, endIndex));
        }
    }

    private void LoadMoreTreeChildren(TreeViewNode loadMoreNode)
    {
        if (!_loadMoreItems.TryGetValue(loadMoreNode, out var state))
        {
            return;
        }

        state.Parent.Children.Remove(loadMoreNode);
        _treeItems.Remove(loadMoreNode);
        _loadMoreItems.Remove(loadMoreNode);
        AppendTreeChildrenPage(state.Parent, state.ParentPath, state.Names, state.NextIndex);
    }

    private void InvalidateRealizedTreePath(RegistryPath? path)
    {
        if (path is null)
        {
            return;
        }

        var node = FindNode(KeyTree.RootNodes, candidate =>
            _treeItems.TryGetValue(candidate, out var item)
            && !item.IsLoadMore
            && item.Path.Equals(path));
        if (node is null || !_treeItems.TryGetValue(node, out var treeItem))
        {
            return;
        }

        foreach (var child in node.Children.ToArray())
        {
            RemoveTreeMappings(child);
        }

        node.Children.Clear();
        node.HasUnrealizedChildren = true;
        var resetItem = new RegistryTreeItem(treeItem.Path, treeItem.Name);
        _treeItems[node] = resetItem;
        node.Content = resetItem.Name;
    }

    private void RemoveTreeMappings(TreeViewNode node)
    {
        foreach (var child in node.Children.ToArray())
        {
            RemoveTreeMappings(child);
        }

        _treeItems.Remove(node);
        _loadMoreItems.Remove(node);
    }

    private void AddressBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        var query = args.ChosenSuggestion as string ?? args.QueryText;
        if (string.IsNullOrWhiteSpace(query))
        {
            return;
        }

        try
        {
            SelectPath(RegistryPath.Parse(query));
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException)
        {
            ShowStatus("Invalid path", ex.Message, InfoBarSeverity.Error);
        }
    }

    private void AddressBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
        {
            return;
        }

        var query = sender.Text.Trim();
        sender.ItemsSource = GetAddressSuggestions(query);
    }

    private void AddressBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is string path)
        {
            sender.Text = path;
        }
    }

    private void AddressBox_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Tab)
        {
            return;
        }

        var suggestion = GetAddressSuggestions(AddressBox.Text).FirstOrDefault();
        if (string.IsNullOrWhiteSpace(suggestion))
        {
            return;
        }

        AddressBox.Text = suggestion;
        e.Handled = true;
    }

    private void RegistryViewSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var pathToKeep = _currentPath;
        _refreshVersion++;
        _readCache.Clear();
        if (KeyTree?.RootNodes.Count > 0)
        {
            _initialPath = pathToKeep;
            LoadRoots();
            return;
        }

        RefreshCurrent();
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        _readCache.Clear();
        RefreshCurrent();
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        if (_backStack.Count == 0 || _currentPath is null)
        {
            return;
        }

        var target = _backStack[^1];
        _backStack.RemoveAt(_backStack.Count - 1);
        _forwardStack.Add(_currentPath);
        _isHistoryNavigation = true;
        SelectPath(target);
        _isHistoryNavigation = false;
    }

    private void Forward_Click(object sender, RoutedEventArgs e)
    {
        if (_forwardStack.Count == 0 || _currentPath is null)
        {
            return;
        }

        var target = _forwardStack[^1];
        _forwardStack.RemoveAt(_forwardStack.Count - 1);
        _backStack.Add(_currentPath);
        _isHistoryNavigation = true;
        SelectPath(target);
        _isHistoryNavigation = false;
    }

    private void UpButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPath is null || string.IsNullOrEmpty(_currentPath.SubKey))
        {
            return;
        }

        SelectPath(RegistryBrowser.GetParent(_currentPath));
    }

    private async void NewKey_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPath is null)
        {
            return;
        }

        var name = await PromptTextAsync("New Key", "Name", "New Key #1");
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        await RunWriteAsync(
            "Key created",
            () => _browser.CreateSubKey(_currentPath, name.Trim(), GetCurrentView()));
    }

    private async void NewString_Click(object sender, RoutedEventArgs e)
    {
        await CreateOrEditStringValueAsync(null);
    }

    private async void DeleteKey_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPath is null)
        {
            return;
        }

        if (string.IsNullOrEmpty(_currentPath.SubKey))
        {
            ShowStatus("Root hive protected", "Root hives cannot be deleted.", InfoBarSeverity.Warning);
            return;
        }

        var confirmed = await ConfirmAsync("Delete Key", $"Delete {_currentPath} and all of its subkeys?");
        if (!confirmed)
        {
            return;
        }

        var parent = RegistryBrowser.GetParent(_currentPath);
        var backupPath = await WriteBackupAsync(_currentPath, includeSubtree: true);
        await RunWriteAsync(
            "Key deleted",
            () => _browser.DeleteSubKeyTree(_currentPath, GetCurrentView()),
            refreshAfterWrite: false);
        SelectPath(parent);
        LoadRoots();
        ShowStatus("Key deleted", $"Backup saved to {backupPath}", InfoBarSeverity.Success);
    }

    private async void RenameKey_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPath is null)
        {
            return;
        }

        await RenameKeyAsync(_currentPath);
    }

    private async void NewDword_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPath is null)
        {
            return;
        }

        var name = await PromptTextAsync("New DWORD (32-bit) Value", "Name", "New Value #1");
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var numericValue = await PromptIntegerValueAsync("New DWORD (32-bit) Value", 32, 0);
        if (numericValue is null)
        {
            return;
        }

        await RunWriteAsync(
            "DWORD value written",
            () => _browser.SetValue(_currentPath, name.Trim(), RegistryValueKind.DWord, unchecked((int)(uint)numericValue.Value), GetCurrentView()));
    }

    private async void NewQword_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPath is null)
        {
            return;
        }

        var name = await PromptTextAsync("New QWORD (64-bit) Value", "Name", "New Value #1");
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var numericValue = await PromptIntegerValueAsync("New QWORD (64-bit) Value", 64, 0);
        if (numericValue is null)
        {
            return;
        }

        await RunWriteAsync(
            "QWORD value written",
            () => _browser.SetValue(_currentPath, name.Trim(), RegistryValueKind.QWord, numericValue.Value, GetCurrentView()));
    }

    private async void NewBinary_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPath is null)
        {
            return;
        }

        var name = await PromptTextAsync("New Binary Value", "Name", "New Value #1");
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var valueText = await PromptBinaryTextAsync("New Binary Value", "00");
        if (valueText is null)
        {
            return;
        }

        if (!TryParseHexBytes(valueText, out var bytes))
        {
            ShowStatus("Invalid binary data", "Use hex bytes separated by spaces, commas, or new lines.", InfoBarSeverity.Error);
            return;
        }

        await RunWriteAsync(
            "Binary value written",
            () => _browser.SetValue(_currentPath, name.Trim(), RegistryValueKind.Binary, bytes, GetCurrentView()));
    }

    private async void NewMultiString_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPath is null)
        {
            return;
        }

        var name = await PromptTextAsync("New Multi-String Value", "Name", "New Value #1");
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var valueText = await PromptMultilineTextAsync("New Multi-String Value", "One string per line", string.Empty);
        if (valueText is null)
        {
            return;
        }

        var values = valueText
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.None);

        await RunWriteAsync(
            "Multi-string value written",
            () => _browser.SetValue(_currentPath, name.Trim(), RegistryValueKind.MultiString, values, GetCurrentView()));
    }

    private async void EditValue_Click(object sender, RoutedEventArgs e)
    {
        if (ValueList.SelectedItem is not RegistryValueRow row)
        {
            ShowStatus("No value selected", "Select a value in the details pane before editing.", InfoBarSeverity.Warning);
            return;
        }

        switch (row.Kind)
        {
            case nameof(RegistryValueKind.String):
            case nameof(RegistryValueKind.ExpandString):
                await CreateOrEditStringValueAsync(row);
                break;
            case nameof(RegistryValueKind.DWord):
                await EditDwordValueAsync(row);
                break;
            case nameof(RegistryValueKind.QWord):
                await EditQwordValueAsync(row);
                break;
            case nameof(RegistryValueKind.Binary):
                await EditBinaryValueAsync(row);
                break;
            case nameof(RegistryValueKind.MultiString):
                await EditMultiStringValueAsync(row);
                break;
            default:
                await EditRawStringFallbackAsync(row);
                break;
        }
    }

    private async void DeleteValue_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPath is null || ValueList.SelectedItem is not RegistryValueRow row)
        {
            ShowStatus("No value selected", "Select a value in the details pane before deleting.", InfoBarSeverity.Warning);
            return;
        }

        var confirmed = await ConfirmAsync("Delete Value", $"Delete {row.DisplayName} from {_currentPath}?");
        if (!confirmed)
        {
            return;
        }

        var backupPath = await WriteBackupAsync(_currentPath, includeSubtree: false);
        await RunWriteAsync(
            "Value deleted",
            () => _browser.DeleteValue(_currentPath, row.Name, GetCurrentView()));
        ShowStatus("Value deleted", $"Backup saved to {backupPath}", InfoBarSeverity.Success);
    }

    private async void RenameValue_Click(object sender, RoutedEventArgs e)
    {
        await RenameSelectedValueAsync();
    }

    private void CopyPath_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPath is null)
        {
            return;
        }

        CopyText(_currentPath.ToString());
        ShowStatus("Copied", _currentPath.ToString(), InfoBarSeverity.Success);
    }

    private void CopyValue_Click(object sender, RoutedEventArgs e)
    {
        if (ValueList.SelectedItem is not RegistryValueRow row)
        {
            ShowStatus("No value selected", "Select a value before copying value data.", InfoBarSeverity.Warning);
            return;
        }

        CopyText(row.DisplayData);
        ShowStatus("Value copied", row.DisplayName, InfoBarSeverity.Success);
    }

    private void Monitor_Click(object sender, RoutedEventArgs e)
    {
        if (ReferenceEquals(sender, MonitorMenuItem))
        {
            MonitorCommand.IsChecked = MonitorCommand.IsChecked != true;
        }

        if (_currentPath is null)
        {
            MonitorCommand.IsChecked = false;
            ShowStatus("No key loaded", "Open a key before starting live monitoring.", InfoBarSeverity.Warning);
            return;
        }

        if (MonitorCommand.IsChecked == true)
        {
            _monitorSignature = GetSnapshotSignature(_currentSnapshot);
            _monitorTimer.Start();
            ShowStatus("Monitoring started", $"Watching {_currentPath} every 2 seconds.", InfoBarSeverity.Informational);
            UpdateStatus($"Monitoring {_currentPath}");
        }
        else
        {
            _monitorTimer.Stop();
            _monitorSignature = null;
            ShowStatus("Monitoring paused", "Live refresh is paused for the current key.", InfoBarSeverity.Informational);
            UpdateStatus(_currentPath is null ? "Ready" : $"Loaded {_currentPath}");
        }

        UpdateCommandState();
    }

    private void AddFavorite_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPath is null)
        {
            return;
        }

        var isFavorite = IsCurrentPathFavorite();
        if (isFavorite && RegistryFavoriteStore.Remove(_currentPath))
        {
            ShowStatus("Favorite removed", "This key was removed from Favorites.", InfoBarSeverity.Informational);
            UpdateStatus($"Removed favorite {_currentPath}");
        }
        else if (RegistryFavoriteStore.Add(_currentPath))
        {
            ShowStatus("Favorite added", "Open Favorites from the left rail to jump back to this key.", InfoBarSeverity.Success);
            UpdateStatus($"Added favorite {_currentPath}");
        }
        else
        {
            ShowStatus("Already a favorite", "This key is already in Favorites.", InfoBarSeverity.Informational);
            UpdateStatus($"Favorite already saved: {_currentPath}");
        }

        UpdateCommandState();
    }

    private async void Export_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPath is null)
        {
            return;
        }

        await SaveExportAsync(_currentPath, includeSubtree: false);
    }

    private void CopyExport_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPath is null)
        {
            return;
        }

        CopyExport(_currentPath, includeSubtree: false);
    }

    private async void ExportTree_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPath is null)
        {
            return;
        }

        await SaveExportAsync(_currentPath, includeSubtree: true);
    }

    private async void Import_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary
        };
        picker.FileTypeFilter.Add(".reg");

        if (App.MainWindow is not null)
        {
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.MainWindow));
        }

        var file = await picker.PickSingleFileAsync();
        if (file is not null)
        {
            await ImportRegistryFileAsync(file);
        }
    }

    private async void LoadHive_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary
        };
        picker.FileTypeFilter.Add("*");

        if (App.MainWindow is not null)
        {
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.MainWindow));
        }

        var file = await picker.PickSingleFileAsync();
        if (file is null)
        {
            UpdateStatus("Load hive canceled");
            return;
        }

        var targetHive = _currentPath?.Hive == RegistryHiveId.Users ? RegistryHiveId.Users : RegistryHiveId.LocalMachine;
        var mountName = await PromptTextAsync("Load Hive", "Mount name under HKLM or HKU", Path.GetFileNameWithoutExtension(file.Name));
        if (string.IsNullOrWhiteSpace(mountName))
        {
            UpdateStatus("Load hive canceled");
            return;
        }

        try
        {
            var loadedPath = await Task.Run(() => _browser.LoadHive(targetHive, mountName.Trim(), file.Path, GetCurrentView()));
            _readCache.Clear();
            SelectPath(loadedPath);
            ShowStatus("Hive loaded", $"Mounted {file.Path} at {loadedPath}.", InfoBarSeverity.Success);
        }
        catch (Exception ex) when (IsRecoverableRegistryException(ex) || ex is System.ComponentModel.Win32Exception)
        {
            ShowStatus("Load hive failed", $"{ex.Message} Run elevated and choose a hive file not currently in use.", InfoBarSeverity.Error);
        }
    }

    private async void UnloadHive_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPath is null)
        {
            return;
        }

        var confirmed = await ConfirmAsync("Unload Hive", $"Unload {_currentPath}? Make sure no key inside this hive is in use.");
        if (!confirmed)
        {
            return;
        }

        try
        {
            var unloadedPath = _currentPath;
            var parent = RegistryBrowser.GetParent(_currentPath);
            await Task.Run(() => _browser.UnloadHive(_currentPath));
            _readCache.Clear();
            SelectPath(parent);
            ShowStatus("Hive unloaded", unloadedPath.ToString(), InfoBarSeverity.Success);
        }
        catch (Exception ex) when (IsRecoverableRegistryException(ex) || ex is System.ComponentModel.Win32Exception)
        {
            ShowStatus("Unload hive failed", $"{ex.Message} Run elevated and close any handles opened inside the mounted hive.", InfoBarSeverity.Error);
        }
    }

    private async void Find_Click(object sender, RoutedEventArgs e)
    {
        await ShowFindDialogAsync();
    }

    private async void Permissions_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPath is null)
        {
            return;
        }

        var path = _currentPath;
        ShowStatus("Loading permissions", path.ToString(), InfoBarSeverity.Informational);
        await Task.Delay(80);
        await ShowPermissionsAsync(path);
    }

    private async void FindAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        await ShowFindDialogAsync();
    }

    public void ImportRegistryFile(StorageFile file)
    {
        _ = ImportRegistryFileAsync(file);
    }

    private async Task ImportRegistryFileAsync(StorageFile file)
    {
        try
        {
            var text = await FileIO.ReadTextAsync(file);
            var document = RegImportParser.Parse(text);
            var confirmed = await ConfirmImportAsync(file.Name, document);
            if (!confirmed)
            {
                UpdateStatus($"Import canceled: {file.Name}");
                return;
            }

            await RunWriteAsync(
                "Import applied",
                () => _browser.ApplyImport(document, GetCurrentView()));
        }
        catch (Exception ex) when (IsRecoverableRegistryException(ex) || ex is FormatException)
        {
            ShowStatus("Import failed", ex.Message, InfoBarSeverity.Error);
        }
    }

    private void CopyExport(RegistryPath path, bool includeSubtree)
    {
        try
        {
            var export = includeSubtree
                ? _browser.ExportRegTree(path, GetCurrentView())
                : _browser.ExportReg(path, GetCurrentView());
            CopyText(export);
            ShowStatus("Export copied", includeSubtree ? "The key and subtree were copied as .reg text." : "The selected key values were copied as .reg text.", InfoBarSeverity.Success);
        }
        catch (Exception ex) when (IsRecoverableRegistryException(ex))
        {
            ShowStatus("Export failed", ex.Message, InfoBarSeverity.Error);
        }
    }

    private void ValueList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        EditValue_Click(sender, e);
    }

    private void CurrentPathText_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
    {
        if (_currentPath is not null)
        {
            ShowCurrentKeyContextMenu(sender, GetFlyoutOptions(sender, args), _currentPath, includeCreate: false);
        }

        args.Handled = true;
    }

    private void ValueList_PointerPressed(object sender, PointerRoutedEventArgs args)
    {
        var point = args.GetCurrentPoint(ValueList);
        if (!point.Properties.IsRightButtonPressed)
        {
            return;
        }

        var options = new FlyoutShowOptions { Position = point.Position };
        if (TrySelectValueRowFromSource(args.OriginalSource))
        {
            ShowValueContextMenu(ValueList, options);
        }
        else
        {
            ShowBlankValueListContextMenu(ValueList, options);
        }

        args.Handled = true;
    }

    private void ShowValueContextMenu(UIElement target, FlyoutShowOptions options)
    {
        if (ValueList.SelectedItem is not RegistryValueRow)
        {
            return;
        }

        var flyout = new MenuFlyout();
        flyout.Items.Add(CreateMenuItem("Edit", () => EditValue_Click(target, new RoutedEventArgs())));
        flyout.Items.Add(CreateMenuItem("Rename", () => RenameValue_Click(target, new RoutedEventArgs())));
        flyout.Items.Add(CreateMenuItem("Copy name", () =>
        {
            if (ValueList.SelectedItem is RegistryValueRow selected)
            {
                CopyText(selected.DisplayName);
                UpdateStatus($"Copied value name {selected.DisplayName}");
            }
        }));
        flyout.Items.Add(CreateMenuItem("Copy data", () => CopyValue_Click(target, new RoutedEventArgs())));
        flyout.Items.Add(CreateMenuItem("Copy reg add command", CopyRegAddCommand));
        flyout.Items.Add(CreateMenuItem("Copy PowerShell command", CopyPowerShellSetCommand));
        flyout.Items.Add(new MenuFlyoutSeparator());
        flyout.Items.Add(CreateMenuItem("Delete", () => DeleteValue_Click(target, new RoutedEventArgs())));
        flyout.ShowAt(target, options);
    }

    private void ShowBlankValueListContextMenu(UIElement target, FlyoutShowOptions options)
    {
        if (_currentPath is null)
        {
            return;
        }

        ValueList.SelectedItem = null;
        var flyout = new MenuFlyout();
        flyout.Items.Add(CreateMenuItem("New string value", () => NewString_Click(target, new RoutedEventArgs())));
        flyout.Items.Add(CreateMenuItem("New DWORD value", () => NewDword_Click(target, new RoutedEventArgs())));
        flyout.Items.Add(CreateMenuItem("New QWORD value", () => NewQword_Click(target, new RoutedEventArgs())));
        flyout.Items.Add(CreateMenuItem("New binary value", () => NewBinary_Click(target, new RoutedEventArgs())));
        flyout.Items.Add(CreateMenuItem("New multi-string value", () => NewMultiString_Click(target, new RoutedEventArgs())));
        flyout.Items.Add(new MenuFlyoutSeparator());
        flyout.Items.Add(CreateMenuItem("Copy key path", () => CopyPath_Click(target, new RoutedEventArgs())));
        flyout.Items.Add(CreateMenuItem("Copy key export", () => CopyExport_Click(target, new RoutedEventArgs())));
        flyout.Items.Add(CreateMenuItem("Save key export", async () => await SaveExportAsync(_currentPath, includeSubtree: false)));
        flyout.Items.Add(CreateMenuItem("Save subtree export", () => ExportTree_Click(target, new RoutedEventArgs())));
        flyout.ShowAt(target, options);
    }

    private void ShowCurrentKeyContextMenu(UIElement target, FlyoutShowOptions options, RegistryPath menuPath, bool includeCreate)
    {
        var flyout = new MenuFlyout();
        if (includeCreate)
        {
            var isCurrent = _currentPath is not null && _currentPath.Equals(menuPath);
            flyout.Items.Add(CreateMenuItem("Open", () => OpenKeyFromMenu(menuPath), !isCurrent));
            flyout.Items.Add(new MenuFlyoutSeparator());
            flyout.Items.Add(CreateMenuItem("New key", () => RunAfterSelecting(menuPath, () => NewKey_Click(target, new RoutedEventArgs()))));
            flyout.Items.Add(new MenuFlyoutSeparator());
        }

        flyout.Items.Add(CreateMenuItem("Copy key path", () =>
        {
            CopyText(menuPath.ToString());
            UpdateStatus($"Copied path {menuPath}");
        }));
        flyout.Items.Add(CreateMenuItem("Favorite key", () =>
        {
            RegistryFavoriteStore.Add(menuPath);
            ShowStatus("Favorite added", "Open Favorites from the left rail to jump back to this key.", InfoBarSeverity.Success);
        }));
        flyout.Items.Add(CreateMenuItem("Copy key export", () => CopyExport(menuPath, includeSubtree: false)));
        flyout.Items.Add(CreateMenuItem("Save key export", async () => await SaveExportAsync(menuPath, includeSubtree: false)));
        flyout.Items.Add(CreateMenuItem("Save subtree export", async () => await SaveExportAsync(menuPath, includeSubtree: true)));
        flyout.Items.Add(CreateMenuItem("Permissions", async () => await ShowPermissionsAsync(menuPath)));
        if (!string.IsNullOrEmpty(menuPath.SubKey))
        {
            flyout.Items.Add(new MenuFlyoutSeparator());
            flyout.Items.Add(CreateMenuItem("Rename key", () => RunAfterSelecting(menuPath, () => RenameKey_Click(target, new RoutedEventArgs()))));
            flyout.Items.Add(CreateMenuItem("Delete key", () => RunAfterSelecting(menuPath, () => DeleteKey_Click(target, new RoutedEventArgs()))));
        }

        flyout.ShowAt(target, options);
    }

    private void ValueList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ValueList.SelectedItem is RegistryValueRow row)
        {
            UpdateStatus($"{row.DisplayName} · {row.Kind}");
        }
        else
        {
            UpdateStatus(_currentPath is null ? "Ready" : $"Loaded {_currentPath}");
        }

        UpdateCommandState();
    }

    private async void Page_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.F2)
        {
            return;
        }

        e.Handled = true;
        await RenameFocusedItemAsync();
    }

    private async Task RenameFocusedItemAsync()
    {
        if (ValueList.SelectedItem is RegistryValueRow)
        {
            await RenameSelectedValueAsync();
        }
        else if (_currentPath is not null)
        {
            await RenameKeyAsync(_currentPath);
        }
    }

    private void ValueFilterBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_currentSnapshot is null)
        {
            return;
        }

        ValueCountText.Text = "Filtering";
        _filterTimer.Stop();
        _filterTimer.Start();
    }

    private void ValueTypeFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_currentSnapshot is null)
        {
            return;
        }

        ApplyValueFilter();
    }

    private void FilterTimer_Tick(object? sender, object e)
    {
        _filterTimer.Stop();
        ApplyValueFilter();
    }

    private async Task CreateOrEditStringValueAsync(RegistryValueRow? row)
    {
        if (_currentPath is null)
        {
            return;
        }

        var creating = row is null;
        var name = creating
            ? await PromptTextAsync("New String Value", "Name", "New Value #1")
            : row!.Name;

        if (name is null)
        {
            return;
        }

        var currentValue = creating ? string.Empty : row!.DisplayData;
        var isExpandable = row?.Kind == nameof(RegistryValueKind.ExpandString);
        var value = isExpandable
            ? await PromptExpandableStringAsync($"Edit {row!.DisplayName}", currentValue)
            : await PromptTextAsync(creating ? "New String Value" : $"Edit {row!.DisplayName}", "Value data", currentValue);
        if (value is null)
        {
            return;
        }

        var kind = isExpandable ? RegistryValueKind.ExpandString : RegistryValueKind.String;
        await RunWriteAsync(
            creating ? "String value written" : "String value updated",
            () => _browser.SetValue(_currentPath, name.Trim(), kind, value, GetCurrentView()));
    }

    private async Task EditDwordValueAsync(RegistryValueRow row)
    {
        if (_currentPath is null)
        {
            return;
        }

        var initial = Convert.ToUInt64(row.Raw.Data, System.Globalization.CultureInfo.InvariantCulture);
        var value = await PromptIntegerValueAsync($"Edit {row.DisplayName}", 32, initial);
        if (value is null)
        {
            return;
        }

        await RunWriteAsync(
            "DWORD value updated",
            () => _browser.SetValue(_currentPath, row.Name, RegistryValueKind.DWord, unchecked((int)(uint)value.Value), GetCurrentView()));
    }

    private async Task EditQwordValueAsync(RegistryValueRow row)
    {
        if (_currentPath is null)
        {
            return;
        }

        var initial = Convert.ToUInt64(row.Raw.Data, System.Globalization.CultureInfo.InvariantCulture);
        var value = await PromptIntegerValueAsync($"Edit {row.DisplayName}", 64, initial);
        if (value is null)
        {
            return;
        }

        await RunWriteAsync(
            "QWORD value updated",
            () => _browser.SetValue(_currentPath, row.Name, RegistryValueKind.QWord, value.Value, GetCurrentView()));
    }

    private async Task EditBinaryValueAsync(RegistryValueRow row)
    {
        if (_currentPath is null)
        {
            return;
        }

        var valueText = await PromptBinaryTextAsync($"Edit {row.DisplayName}", row.DisplayData);
        if (valueText is null)
        {
            return;
        }

        if (!TryParseHexBytes(valueText, out var bytes))
        {
            ShowStatus("Invalid binary data", "Use hex bytes separated by spaces, commas, or new lines.", InfoBarSeverity.Error);
            return;
        }

        await RunWriteAsync(
            "Binary value updated",
            () => _browser.SetValue(_currentPath, row.Name, RegistryValueKind.Binary, bytes, GetCurrentView()));
    }

    private async Task EditMultiStringValueAsync(RegistryValueRow row)
    {
        if (_currentPath is null)
        {
            return;
        }

        var initial = row.Raw.Data is string[] values ? string.Join(Environment.NewLine, values) : row.DisplayData;
        var valueText = await PromptMultilineTextAsync($"Edit {row.DisplayName}", "One string per line", initial);
        if (valueText is null)
        {
            return;
        }

        var newValues = valueText
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.None);

        await RunWriteAsync(
            "Multi-string value updated",
            () => _browser.SetValue(_currentPath, row.Name, RegistryValueKind.MultiString, newValues, GetCurrentView()));
    }

    private async Task EditRawStringFallbackAsync(RegistryValueRow row)
    {
        if (_currentPath is null)
        {
            return;
        }

        var valueText = await PromptTextAsync($"Edit {row.DisplayName}", "Value data", row.DisplayData);
        if (valueText is null)
        {
            return;
        }

        await RunWriteAsync(
            "Value updated",
            () => _browser.SetValue(_currentPath, row.Name, RegistryValueKind.String, valueText, GetCurrentView()));
    }

    private async Task RenameSelectedValueAsync()
    {
        if (_currentPath is null || ValueList.SelectedItem is not RegistryValueRow row)
        {
            ShowStatus("No value selected", "Select a value before renaming.", InfoBarSeverity.Warning);
            return;
        }

        var initial = string.IsNullOrEmpty(row.Name) ? string.Empty : row.Name;
        var newName = await PromptTextAsync($"Rename {row.DisplayName}", "New name", initial);
        if (newName is null)
        {
            return;
        }

        var normalized = newName.Trim();
        if (string.IsNullOrEmpty(normalized) && !string.IsNullOrEmpty(row.Name))
        {
            ShowStatus("Invalid value name", "Use @ in the CLI for the default value; the app keeps value rename targets named for safety.", InfoBarSeverity.Warning);
            return;
        }

        await RunWriteAsync(
            "Value renamed",
            () => _browser.RenameValue(_currentPath, row.Name, normalized, GetCurrentView()));
    }

    private async Task RenameKeyAsync(RegistryPath path)
    {
        if (string.IsNullOrEmpty(path.SubKey))
        {
            ShowStatus("Root hive protected", "Root hives cannot be renamed.", InfoBarSeverity.Warning);
            return;
        }

        var currentName = RegistryBrowser.GetLeafName(path);
        var newName = await PromptTextAsync("Rename Key", "New name", currentName);
        if (string.IsNullOrWhiteSpace(newName))
        {
            return;
        }

        RegistryPath? renamedPath = null;
        await RunWriteAsync(
            "Key renamed",
            () => renamedPath = _browser.RenameSubKey(path, newName.Trim(), GetCurrentView()),
            refreshAfterWrite: false);

        if (renamedPath is not null)
        {
            SelectPath(renamedPath);
            LoadRoots();
        }
    }

    private async Task RunWriteAsync(string successTitle, Action action, bool refreshAfterWrite = true)
    {
        try
        {
            CaptureWriteJournalEntry(successTitle);
            action();
            _readCache.Clear();
            InvalidateRealizedTreePath(_currentPath);
            if (refreshAfterWrite)
            {
                RefreshCurrent();
            }

            ShowStatus(
                successTitle,
                refreshAfterWrite ? "The key was refreshed after the write." : "The registry cache was invalidated after the write.",
                InfoBarSeverity.Success);
        }
        catch (Exception ex) when (IsRecoverableRegistryException(ex))
        {
            var message = ex is UnauthorizedAccessException
                ? "Access denied. Run the app elevated for protected hives, or choose a writable key under HKCU."
                : ex.Message;
            ShowStatus("Write failed", message, InfoBarSeverity.Error);
        }
    }

    private async Task<string> WriteBackupAsync(RegistryPath path, bool includeSubtree)
    {
        var backupsFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("Backups", CreationCollisionOption.OpenIfExists);
        var fileName = $"registry-backup-{DateTimeOffset.Now:yyyyMMdd-HHmmss}-{SanitizeFileName(path)}.reg";
        var file = await backupsFolder.CreateFileAsync(fileName, CreationCollisionOption.GenerateUniqueName);
        var content = includeSubtree ? _browser.ExportRegTree(path, GetCurrentView()) : _browser.ExportReg(path, GetCurrentView());
        await FileIO.WriteTextAsync(file, content);
        return file.Path;
    }

    private static string SanitizeFileName(RegistryPath path)
    {
        var text = path.ToString().Replace('\\', '_').Replace(':', '_');
        foreach (var invalid in Path.GetInvalidFileNameChars())
        {
            text = text.Replace(invalid, '_');
        }

        return text.Length > 80 ? text[..80] : text;
    }

    private void SelectPath(RegistryPath path)
    {
        if (!_isHistoryNavigation && _currentPath is not null && !_currentPath.Equals(path))
        {
            _backStack.Add(_currentPath);
            _forwardStack.Clear();
        }

        _currentPath = path;
        AddressBox.Text = path.ToString();
        CurrentPathText.Text = path.ToString();
        AddRecent(path);
        RefreshCurrent();
        UpdateNavigationState();
    }

    private async void RefreshCurrent()
    {
        if (_currentPath is null)
        {
            UpdateCommandState();
            return;
        }

        var path = _currentPath;
        var view = GetCurrentView();
        var version = ++_refreshVersion;
        try
        {
            RefreshCommand.IsEnabled = false;
            KeyCountText.Text = "Loading";
            ValueCountText.Text = "Loading";
            _currentSnapshot = await _readCache.ReadSummaryAsync(path, view);
            if (version != _refreshVersion || _currentPath is null || !_currentPath.Equals(path))
            {
                return;
            }

            ApplyValueFilter();
            CurrentPathText.Text = _currentPath.ToString();
            AddressBox.Text = _currentPath.ToString();
            UpButton.IsEnabled = !string.IsNullOrEmpty(_currentPath.SubKey);
            if (MonitorCommand.IsChecked == true)
            {
                _monitorSignature = GetSnapshotSignature(_currentSnapshot);
            }

            UpdateStatus($"Loaded {_currentPath}");
        }
        catch (Exception ex) when (IsRecoverableRegistryException(ex))
        {
            if (version != _refreshVersion)
            {
                return;
            }

            _currentSnapshot = null;
            ValueList.ItemsSource = Array.Empty<RegistryValueRow>();
            KeyCountText.Text = "No key";
            ValueCountText.Text = "No values";
            ShowStatus("Unable to read key", ex.Message, InfoBarSeverity.Error);
        }
        finally
        {
            UpdateCommandState();
        }
    }

    private void ApplyValueFilter()
    {
        if (_currentSnapshot is null)
        {
            ValueList.ItemsSource = Array.Empty<RegistryValueRow>();
            KeyCountText.Text = "No key";
            ValueCountText.Text = "No values";
            EmptyValuesPanel.Visibility = Visibility.Visible;
            return;
        }

        var filter = ValueFilterBox.Text?.Trim();
        var typeFilter = GetSelectedValueKindFilter();
        var values = _currentSnapshot.Values.AsEnumerable();
        if (!string.IsNullOrEmpty(typeFilter))
        {
            values = values.Where(value => value.Kind.Equals(typeFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(filter))
        {
            values = values.Where(value =>
                value.DisplayName.Contains(filter, StringComparison.OrdinalIgnoreCase)
                || value.Kind.Contains(filter, StringComparison.OrdinalIgnoreCase)
                || value.DisplayData.Contains(filter, StringComparison.OrdinalIgnoreCase));
        }

        var rows = values.Select(value => new RegistryValueRow(value)).ToArray();
        foreach (var row in rows)
        {
            row.SetColumnWidths(_valueNameColumnWidth, _valueTypeColumnWidth);
        }

        ValueList.SelectedItem = null;
        ValueList.ItemsSource = rows;
        EmptyValuesPanel.Visibility = rows.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        EmptyValuesText.Text = string.IsNullOrWhiteSpace(filter) && string.IsNullOrEmpty(typeFilter)
            ? "This key has no values."
            : "No values match the current filters.";
        KeyCountText.Text = FormatCount(_currentSnapshot.SubKeyCount, "subkey", "subkeys");
        ValueCountText.Text = string.IsNullOrWhiteSpace(filter) && string.IsNullOrEmpty(typeFilter)
            ? FormatCount(_currentSnapshot.ValueCount, "value", "values")
            : $"{rows.Length:N0} / {_currentSnapshot.ValueCount:N0} shown";
        UpdateCommandState();
    }

    private string GetSelectedValueKindFilter()
    {
        if (ValueTypeFilter?.SelectedItem is not ComboBoxItem item || item.Content is not string text || text == "All types")
        {
            return string.Empty;
        }

        return text;
    }

    private void AddRecent(RegistryPath path)
    {
        _recent.RemoveAll(candidate => candidate.ToString().Equals(path.ToString(), StringComparison.OrdinalIgnoreCase));
        _recent.Insert(0, path);
        if (_recent.Count > RecentLimit)
        {
            _recent.RemoveRange(RecentLimit, _recent.Count - RecentLimit);
        }

    }

    private string[] GetAddressSuggestions(string query)
    {
        var roots = _browser.GetRootPaths().Select(path => path.ToString());
        var favorites = RegistryFavoriteStore.GetFavorites().Select(path => path.ToString());
        var recent = _recent.Select(path => path.ToString());

        return roots.Concat(favorites)
            .Concat(recent)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(path => string.IsNullOrEmpty(query) || path.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path.Length)
            .ThenBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Take(12)
            .ToArray();
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

    private static string FormatCount(int count, string singular, string plural)
    {
        return count switch
        {
            0 => $"No {plural}",
            1 => $"1 {singular}",
            _ => $"{count:N0} {plural}"
        };
    }

    private RegistryViewMode GetCurrentView()
    {
        return RegistryViewSelector.SelectedIndex == 0 ? RegistryViewMode.Registry32 : RegistryViewMode.Registry64;
    }

    private void UpdateNavigationState()
    {
        BackButton.IsEnabled = _backStack.Count > 0;
        ForwardButton.IsEnabled = _forwardStack.Count > 0;
        UpButton.IsEnabled = _currentPath is not null && !string.IsNullOrEmpty(_currentPath.SubKey);
        UpdateCommandState();
    }

    private void UpdateStatus(string message)
    {
        StatusText.Text = message;
    }

    private async void MonitorTimer_Tick(object? sender, object e)
    {
        if (_isMonitorReading || _currentPath is null || MonitorCommand.IsChecked != true)
        {
            return;
        }

        _isMonitorReading = true;
        var path = _currentPath;
        var view = GetCurrentView();
        try
        {
            var snapshot = await Task.Run(() => _browser.ReadKeySummary(path, view));
            if (_currentPath is null || !_currentPath.Equals(path) || MonitorCommand.IsChecked != true)
            {
                return;
            }

            var signature = GetSnapshotSignature(snapshot);
            if (_monitorSignature is null)
            {
                _monitorSignature = signature;
                return;
            }

            if (!string.Equals(signature, _monitorSignature, StringComparison.Ordinal))
            {
                var changeText = DescribeSnapshotChange(_currentSnapshot, snapshot);
                _monitorSignature = signature;
                _readCache.Clear();
                _currentSnapshot = snapshot;
                ApplyValueFilter();
                ShowStatus("Registry change detected", changeText, InfoBarSeverity.Informational);
                UpdateStatus($"Monitoring update: {changeText}");
            }
        }
        catch (Exception ex) when (IsRecoverableRegistryException(ex))
        {
            MonitorCommand.IsChecked = false;
            _monitorTimer.Stop();
            ShowStatus("Monitoring stopped", ex.Message, InfoBarSeverity.Warning);
        }
        finally
        {
            _isMonitorReading = false;
        }
    }

    private void UpdateCommandState()
    {
        var hasKey = _currentPath is not null;
        var hasValue = ValueList?.SelectedItem is RegistryValueRow;
        if (RefreshCommand is null)
        {
            return;
        }

        RefreshCommand.IsEnabled = hasKey;
        FindCommand.IsEnabled = hasKey;
        EditCommand.IsEnabled = hasValue;
        DeleteValueCommand.IsEnabled = hasValue;
        CopyValueCommand.IsEnabled = hasValue;
        CopyValueMenuItem.IsEnabled = hasValue;
        FavoriteCommand.IsEnabled = hasKey;
        var isFavorite = IsCurrentPathFavorite();
        FavoriteCommand.Label = "Favorite";
        FavoriteCommand.IsChecked = isFavorite;
        FavoriteCommand.Icon = new SymbolIcon(Symbol.Favorite);
        CopyPathCommand.IsEnabled = hasKey;
        ExportCommand.IsEnabled = hasKey;
        ExportMenuItem.IsEnabled = hasKey;
        PermissionsCommand.IsEnabled = hasKey;
        PermissionsMenuItem.IsEnabled = hasKey;
        MonitorCommand.IsEnabled = hasKey;
        MonitorMenuItem.IsEnabled = hasKey;
        MonitorCommand.Label = MonitorCommand.IsChecked == true ? "Monitoring" : "Monitor";
        if (MonitorCommand.IsChecked == true)
        {
            MonitorMenuItem.Text = "Stop monitoring";
            MonitorMenuItem.Icon = new SymbolIcon(Symbol.Cancel);
        }
        else
        {
            MonitorMenuItem.Text = "Monitor";
            MonitorMenuItem.Icon = new SymbolIcon(Symbol.View);
        }
        LoadHiveCommand.IsEnabled = true;
        LoadHiveMenuItem.IsEnabled = true;
        UnloadHiveCommand.IsEnabled = CanUnloadHive(_currentPath);
        UnloadHiveMenuItem.IsEnabled = UnloadHiveCommand.IsEnabled;
    }

    private void TopBar_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var compact = e.NewSize.Width < 690;
        Grid.SetRow(FilterHost, compact ? 1 : 0);
        Grid.SetColumn(FilterHost, compact ? 0 : 4);
        Grid.SetColumnSpan(FilterHost, compact ? 6 : 2);
        Grid.SetColumnSpan(AddressBox, compact ? 3 : 1);
        FilterHost.Margin = new Thickness(0);
        AddressColumn.Width = new GridLength(1, GridUnitType.Star);
        FilterColumn.Width = compact ? new GridLength(0) : new GridLength(240);
        TypeColumn.Width = compact ? new GridLength(0) : new GridLength(132);
        AddressBox.MinWidth = compact ? 0 : 210;
        ValueFilterBox.MinWidth = compact ? 0 : 180;
        ValueFilterBox.MaxWidth = compact ? double.PositiveInfinity : 240;
        ValueTypeFilter.MinWidth = compact ? 128 : 138;
    }

    private bool IsCurrentPathFavorite()
    {
        return _currentPath is not null
            && RegistryFavoriteStore.GetFavorites().Any(path => path.ToString().Equals(_currentPath.ToString(), StringComparison.OrdinalIgnoreCase));
    }

    private static bool CanUnloadHive(RegistryPath? path)
    {
        return path is not null
            && path.Hive is RegistryHiveId.LocalMachine or RegistryHiveId.Users
            && !string.IsNullOrWhiteSpace(path.SubKey)
            && !path.SubKey.Contains('\\', StringComparison.Ordinal);
    }

    private void PaneSplitter_DragDelta(object sender, DragDeltaEventArgs e)
    {
        var requested = TreePaneColumn.ActualWidth + e.HorizontalChange;
        TreePaneColumn.Width = new GridLength(Math.Clamp(requested, 240, 620));
    }

    private void ApplyToolbarAlignment()
    {
        if (RegistryCommandBar is null)
        {
            return;
        }

        RegistryCommandBar.HorizontalAlignment = AppSettings.ToolbarAlignment switch
        {
            "Center" => HorizontalAlignment.Center,
            "Right" => HorizontalAlignment.Right,
            _ => HorizontalAlignment.Left
        };
    }

    private void ApplyToolbarDetail()
    {
        if (RegistryCommandBar is null)
        {
            return;
        }

        var full = AppSettings.ToolbarDetail == "Full";
        var fullVisibility = full ? Visibility.Visible : Visibility.Collapsed;
        var menuVisibility = full ? Visibility.Collapsed : Visibility.Visible;

        CopyValueCommand.Visibility = fullVisibility;
        PermissionsCommand.Visibility = fullVisibility;
        MonitorCommand.Visibility = fullVisibility;
        ExportCommand.Visibility = fullVisibility;
        ImportCommand.Visibility = fullVisibility;

        CopyValueMenuItem.Visibility = menuVisibility;
        PermissionsMenuItem.Visibility = menuVisibility;
        MonitorMenuItem.Visibility = menuVisibility;
        ExportMenuItem.Visibility = menuVisibility;
        ImportMenuItem.Visibility = menuVisibility;
    }

    private void ValueNameSplitter_DragDelta(object sender, DragDeltaEventArgs e)
    {
        _valueNameColumnWidth = Math.Clamp(_valueNameColumnWidth + e.HorizontalChange, 72, 360);
        ApplyValueColumnWidths();
    }

    private void ValueTypeSplitter_DragDelta(object sender, DragDeltaEventArgs e)
    {
        _valueTypeColumnWidth = Math.Clamp(_valueTypeColumnWidth + e.HorizontalChange, 64, 220);
        ApplyValueColumnWidths();
    }

    private void ApplyValueColumnWidths()
    {
        ValueNameHeaderColumn.Width = new GridLength(_valueNameColumnWidth);
        ValueTypeHeaderColumn.Width = new GridLength(_valueTypeColumnWidth);
        foreach (var item in ValueList.Items.OfType<RegistryValueRow>())
        {
            item.SetColumnWidths(_valueNameColumnWidth, _valueTypeColumnWidth);
        }
    }

    private MenuFlyoutItem CreateMenuItem(string text, Action action, bool isEnabled = true)
    {
        var item = new MenuFlyoutItem { Text = text, IsEnabled = isEnabled };
        item.Click += (_, _) => action();
        return item;
    }

    private static FlyoutShowOptions GetFlyoutOptions(UIElement sender, ContextRequestedEventArgs args)
    {
        var options = new FlyoutShowOptions();
        if (args.TryGetPosition(sender, out var point))
        {
            options.Position = point;
        }

        return options;
    }

    private bool TrySelectValueRowFromSource(object source)
    {
        if (source is not DependencyObject dependencyObject)
        {
            return false;
        }

        var item = FindAncestor<ListViewItem>(dependencyObject);
        if (item?.Content is RegistryValueRow row)
        {
            ValueList.SelectedItem = row;
            return true;
        }

        if (item?.DataContext is RegistryValueRow dataContextRow)
        {
            ValueList.SelectedItem = dataContextRow;
            return true;
        }

        return false;
    }

    private bool TryGetTreePathFromContext(ContextRequestedEventArgs args, out RegistryPath path)
    {
        path = new RegistryPath(RegistryHiveId.CurrentUser, string.Empty);
        if (args.OriginalSource is not DependencyObject source)
        {
            return false;
        }

        var treeItem = FindAncestor<TreeViewItem>(source);
        if (treeItem?.Content is not string label)
        {
            return false;
        }

        var node = FindNode(KeyTree.RootNodes, candidate => candidate.Content?.ToString() == label);
        if (node is not null && _treeItems.TryGetValue(node, out var item))
        {
            if (item.IsLoadMore)
            {
                LoadMoreTreeChildren(node);
                return false;
            }

            path = item.Path;
            SelectPath(item.Path);
            return true;
        }

        return false;
    }

    private void OpenKeyFromMenu(RegistryPath path)
    {
        DispatcherQueue.TryEnqueue(() => SelectPath(path));
    }

    private void RunAfterSelecting(RegistryPath path, Action action)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if (_currentPath is null || !_currentPath.Equals(path))
            {
                SelectPath(path);
            }

            action();
        });
    }

    private async Task SaveExportAsync(RegistryPath path, bool includeSubtree)
    {
        try
        {
            var picker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                SuggestedFileName = includeSubtree
                    ? $"{SanitizeFileName(path)}-subtree"
                    : SanitizeFileName(path)
            };
            picker.FileTypeChoices.Add("Registration entries", [".reg"]);
            picker.DefaultFileExtension = ".reg";

            if (App.MainWindow is not null)
            {
                InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.MainWindow));
            }

            var file = await picker.PickSaveFileAsync();
            if (file is null)
            {
                UpdateStatus("Export canceled");
                return;
            }

            var export = includeSubtree
                ? _browser.ExportRegTree(path, GetCurrentView())
                : _browser.ExportReg(path, GetCurrentView());
            await FileIO.WriteTextAsync(file, export);
            ShowStatus("Export saved", file.Path, InfoBarSeverity.Success);
        }
        catch (Exception ex) when (IsRecoverableRegistryException(ex))
        {
            ShowStatus("Export failed", ex.Message, InfoBarSeverity.Error);
        }
    }

    private static T? FindAncestor<T>(DependencyObject source)
        where T : DependencyObject
    {
        var current = source;
        while (current is not null)
        {
            if (current is T match)
            {
                return match;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    private void CopyRegAddCommand()
    {
        if (_currentPath is null || ValueList.SelectedItem is not RegistryValueRow row)
        {
            return;
        }

        var valueName = string.IsNullOrEmpty(row.Name) ? "/ve" : $"/v \"{row.Name}\"";
        var type = row.Kind switch
        {
            nameof(RegistryValueKind.String) => "REG_SZ",
            nameof(RegistryValueKind.ExpandString) => "REG_EXPAND_SZ",
            nameof(RegistryValueKind.DWord) => "REG_DWORD",
            nameof(RegistryValueKind.QWord) => "REG_QWORD",
            nameof(RegistryValueKind.Binary) => "REG_BINARY",
            nameof(RegistryValueKind.MultiString) => "REG_MULTI_SZ",
            _ => "REG_SZ"
        };
        var data = row.Kind == nameof(RegistryValueKind.Binary)
            ? row.DisplayData.Replace(" ", string.Empty, StringComparison.Ordinal)
            : row.DisplayData;

        CopyText($"reg add \"{_currentPath}\" {valueName} /t {type} /d \"{data}\" /f");
        UpdateStatus($"Copied reg add command for {row.DisplayName}");
    }

    private void CopyPowerShellSetCommand()
    {
        if (_currentPath is null || ValueList.SelectedItem is not RegistryValueRow row)
        {
            return;
        }

        var path = $"Registry::{_currentPath}";
        var type = row.Kind switch
        {
            nameof(RegistryValueKind.String) => "String",
            nameof(RegistryValueKind.ExpandString) => "ExpandString",
            nameof(RegistryValueKind.DWord) => "DWord",
            nameof(RegistryValueKind.QWord) => "QWord",
            nameof(RegistryValueKind.Binary) => "Binary",
            nameof(RegistryValueKind.MultiString) => "MultiString",
            _ => "String"
        };
        var namePart = string.IsNullOrEmpty(row.Name)
            ? "-Name '(default)'"
            : $"-Name {QuotePowerShell(row.Name)}";
        var valuePart = row.Raw.Data switch
        {
            byte[] bytes => $"-Value ([byte[]]({string.Join(',', bytes)}))",
            string[] values => $"-Value @({string.Join(", ", values.Select(QuotePowerShell))})",
            string value => $"-Value {QuotePowerShell(value)}",
            null => "-Value $null",
            _ => $"-Value {QuotePowerShell(row.DisplayData)}"
        };

        CopyText($"New-Item -Path {QuotePowerShell(path)} -Force | Out-Null; New-ItemProperty -Path {QuotePowerShell(path)} {namePart} -PropertyType {type} {valuePart} -Force");
        UpdateStatus($"Copied PowerShell command for {row.DisplayName}");
    }

    private async Task ShowPermissionsAsync(RegistryPath path)
    {
        try
        {
            var summary = await Task.Run(() => _browser.ReadPermissions(path, GetCurrentView()));
            ShowStatus("Permissions loaded", path.ToString(), InfoBarSeverity.Success);
            var ruleList = new StackPanel { Spacing = 8 };
            foreach (var rule in summary.Rules.Take(16))
            {
                ruleList.Children.Add(new Border
                {
                    Padding = new Thickness(10, 8, 10, 8),
                    Background = Microsoft.UI.Xaml.Application.Current.Resources["SubtleFillColorSecondaryBrush"] as Brush,
                    BorderBrush = Microsoft.UI.Xaml.Application.Current.Resources["ControlStrokeColorDefaultBrush"] as Brush,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Child = new StackPanel
                    {
                        Spacing = 2,
                        Children =
                        {
                            new TextBlock
                            {
                                Text = rule.Identity,
                                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                                TextTrimming = TextTrimming.CharacterEllipsis
                            },
                            new TextBlock
                            {
                                Text = $"{rule.AccessType} - {rule.Rights}",
                                Foreground = Microsoft.UI.Xaml.Application.Current.Resources["TextFillColorSecondaryBrush"] as Brush,
                                TextWrapping = TextWrapping.Wrap
                            },
                            new TextBlock
                            {
                                Text = rule.IsInherited ? "Inherited" : "Explicit",
                                Foreground = Microsoft.UI.Xaml.Application.Current.Resources["TextFillColorTertiaryBrush"] as Brush,
                                FontSize = 12
                            }
                        }
                    }
                });
            }

            var content = new StackPanel
            {
                Spacing = 12,
                MinWidth = 520,
                Children =
                {
                    new TextBlock
                    {
                        Text = path.ToString(),
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                        TextTrimming = TextTrimming.CharacterEllipsis
                    },
                    new TextBlock
                    {
                        Text = $"Owner: {summary.Owner}",
                        TextWrapping = TextWrapping.Wrap
                    },
                    ruleList,
                    new TextBlock
                    {
                        Foreground = Microsoft.UI.Xaml.Application.Current.Resources["TextFillColorSecondaryBrush"] as Brush,
                        Text = "Read-only view. Permission editing will use a separate elevated flow.",
                        TextWrapping = TextWrapping.Wrap
                    }
                }
            };

            var dialog = CreateDialog("Permissions", content, "Copy ACL");
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var text = string.Join(Environment.NewLine, summary.Rules.Select(rule => $"{rule.Identity}\t{rule.AccessType}\t{rule.Rights}\t{(rule.IsInherited ? "Inherited" : "Explicit")}"));
                CopyText($"Path: {summary.Path}{Environment.NewLine}Owner: {summary.Owner}{Environment.NewLine}{text}");
                ShowStatus("Permissions copied", "The ACL summary was copied to the clipboard.", InfoBarSeverity.Success);
            }
        }
        catch (Exception ex)
        {
            ShowStatus("Permissions unavailable", GetPermissionsFailureMessage(path, ex), InfoBarSeverity.Warning);
        }
    }

    private static string GetPermissionsFailureMessage(RegistryPath path, Exception exception)
    {
        if (path.Hive == RegistryHiveId.ClassesRoot && string.IsNullOrEmpty(path.SubKey))
        {
            return @"HKEY_CLASSES_ROOT is a merged Windows view. Open HKEY_CURRENT_USER\Software\Classes or HKEY_LOCAL_MACHINE\Software\Classes to inspect the underlying permissions.";
        }

        return string.IsNullOrWhiteSpace(exception.Message)
            ? "Permission details are not available for this key. Try running Registry as administrator or open the underlying hive key."
            : exception.Message;
    }

    private async Task ShowFindDialogAsync()
    {
        if (_currentPath is null)
        {
            ShowStatus("No key loaded", "Open a registry key before searching.", InfoBarSeverity.Warning);
            return;
        }

        var queryBox = new TextBox
        {
            Header = "Find what",
            PlaceholderText = "Key, value, or data",
            Text = _lastSearchOptions?.Query ?? ValueFilterBox.Text ?? string.Empty
        };
        var keysBox = new CheckBox { Content = "Keys", IsChecked = _lastSearchOptions?.MatchKeys ?? true };
        var valueNamesBox = new CheckBox { Content = "Value names", IsChecked = _lastSearchOptions?.MatchValueNames ?? true };
        var valueDataBox = new CheckBox { Content = "Value data", IsChecked = _lastSearchOptions?.MatchValueData ?? true };
        var matchCaseBox = new CheckBox { Content = "Match case", IsChecked = _lastSearchOptions?.MatchCase ?? false };
        var wholeStringBox = new CheckBox { Content = "Match whole string only", IsChecked = _lastSearchOptions?.MatchWholeString ?? false };

        var content = new StackPanel
        {
            Spacing = 10,
            MinWidth = 360,
            Children =
            {
                queryBox,
                new TextBlock
                {
                    Text = "Look at",
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
                },
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 16,
                    Children = { keysBox, valueNamesBox, valueDataBox }
                },
                matchCaseBox,
                wholeStringBox
            }
        };

        var dialog = CreateDialog("Find", content, "Find next");
        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary)
        {
            return;
        }

        var query = queryBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            ShowStatus("Search needs text", "Enter text to find in keys, value names, or value data.", InfoBarSeverity.Warning);
            return;
        }

        var options = new RegistrySearchOptions(
            query,
            keysBox.IsChecked == true,
            valueNamesBox.IsChecked == true,
            valueDataBox.IsChecked == true,
            matchCaseBox.IsChecked == true,
            wholeStringBox.IsChecked == true);

        if (!options.MatchKeys && !options.MatchValueNames && !options.MatchValueData)
        {
            ShowStatus("Search needs a scope", "Choose at least one place to search.", InfoBarSeverity.Warning);
            return;
        }

        _lastSearchOptions = options;
        await RunFindAsync(options);
    }

    private async Task RunFindAsync(RegistrySearchOptions options)
    {
        if (_currentPath is null)
        {
            return;
        }

        var startPath = _currentPath;
        var view = GetCurrentView();
        FindCommand.IsEnabled = false;
        ShowStatus("Searching registry", $"Searching from {startPath}.", InfoBarSeverity.Informational);
        UpdateStatus($"Searching for {options.Query}");
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
            var result = await Task.Run(() => _browser.FindFirst(startPath, options, view, timeout.Token));
            if (result is null)
            {
                ShowStatus("Finished searching", $"No match for '{options.Query}' under {startPath}.", InfoBarSeverity.Informational);
                UpdateStatus($"No search match: {options.Query}");
                return;
            }

            SelectPath(result.Path);
            if (!string.IsNullOrEmpty(result.ValueName))
            {
                DispatcherQueue.TryEnqueue(() => SelectValueByName(result.ValueName));
            }

            ShowStatus("Search match found", $"{FormatSearchKind(result.MatchKind)}: {result.DisplayText}", InfoBarSeverity.Success);
            UpdateStatus($"Found {FormatSearchKind(result.MatchKind).ToLowerInvariant()} {result.DisplayText}");
        }
        catch (OperationCanceledException)
        {
            ShowStatus("Search stopped", "The search timed out before reaching the end of this branch.", InfoBarSeverity.Warning);
            UpdateStatus($"Search timed out: {options.Query}");
        }
        finally
        {
            FindCommand.IsEnabled = _currentPath is not null;
        }
    }

    private void SelectValueByName(string valueName)
    {
        foreach (var item in ValueList.Items)
        {
            if (item is RegistryValueRow row && row.Name.Equals(valueName, StringComparison.OrdinalIgnoreCase))
            {
                ValueList.SelectedItem = row;
                ValueList.ScrollIntoView(row);
                break;
            }
        }
    }

    private static string FormatSearchKind(RegistrySearchMatchKind kind)
    {
        return kind switch
        {
            RegistrySearchMatchKind.KeyName => "Key",
            RegistrySearchMatchKind.ValueName => "Value name",
            RegistrySearchMatchKind.ValueData => "Value data",
            _ => "Match"
        };
    }

    private async Task<string?> PromptTextAsync(string title, string label, string initialValue)
    {
        var input = new TextBox
        {
            Header = label,
            Text = initialValue,
            AcceptsReturn = false,
            SelectionStart = 0,
            SelectionLength = initialValue.Length
        };

        var dialog = CreateDialog(title, input, "OK");
        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary ? input.Text : null;
    }

    private async Task<ulong?> PromptIntegerValueAsync(string title, int bits, ulong initialValue)
    {
        var max = bits == 32 ? uint.MaxValue : ulong.MaxValue;
        var input = new TextBox
        {
            Header = "Value data",
            Text = initialValue.ToString(System.Globalization.CultureInfo.InvariantCulture),
            MinWidth = 360
        };
        var baseSelector = new ComboBox
        {
            Header = "Base",
            SelectedIndex = 0,
            MinWidth = 160,
            Items =
            {
                new ComboBoxItem { Content = "Decimal" },
                new ComboBoxItem { Content = "Hexadecimal" }
            }
        };
        var decimalPreview = new TextBlock { FontWeight = Microsoft.UI.Text.FontWeights.SemiBold };
        var hexPreview = new TextBlock { FontWeight = Microsoft.UI.Text.FontWeights.SemiBold };
        var invalidPreview = new TextBlock
        {
            Foreground = Microsoft.UI.Xaml.Application.Current.Resources["TextFillColorSecondaryBrush"] as Microsoft.UI.Xaml.Media.Brush,
            Visibility = Visibility.Collapsed,
            TextWrapping = TextWrapping.Wrap
        };
        var preview = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Children =
            {
                CreatePreviewPill("Decimal", decimalPreview),
                CreatePreviewPill("Hex", hexPreview)
            }
        };

        void UpdatePreview()
        {
            if (TryParseInteger(input.Text, baseSelector.SelectedIndex == 1, max, out var parsed))
            {
                decimalPreview.Text = parsed.ToString(System.Globalization.CultureInfo.InvariantCulture);
                hexPreview.Text = $"0x{parsed:X}";
                preview.Visibility = Visibility.Visible;
                invalidPreview.Visibility = Visibility.Collapsed;
            }
            else
            {
                preview.Visibility = Visibility.Collapsed;
                invalidPreview.Visibility = Visibility.Visible;
                invalidPreview.Text = bits == 32
                    ? "Enter a DWORD from 0 to 4,294,967,295."
                    : "Enter a QWORD from 0 to 18,446,744,073,709,551,615.";
            }
        }

        input.TextChanged += (_, _) => UpdatePreview();
        baseSelector.SelectionChanged += (_, _) =>
        {
            if (TryParseInteger(input.Text, baseSelector.SelectedIndex != 1, max, out var parsed))
            {
                input.Text = baseSelector.SelectedIndex == 1
                    ? parsed.ToString("X", System.Globalization.CultureInfo.InvariantCulture)
                    : parsed.ToString(System.Globalization.CultureInfo.InvariantCulture);
                input.SelectionStart = input.Text.Length;
            }

            UpdatePreview();
        };

        var content = new StackPanel { Spacing = 10 };
        content.Children.Add(input);
        content.Children.Add(baseSelector);
        content.Children.Add(preview);
        content.Children.Add(invalidPreview);
        UpdatePreview();

        var dialog = CreateDialog(title, content, "OK");
        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary)
        {
            return null;
        }

        if (!TryParseInteger(input.Text, baseSelector.SelectedIndex == 1, max, out var value))
        {
            ShowStatus(bits == 32 ? "Invalid DWORD" : "Invalid QWORD", "Use a value in range for the selected base.", InfoBarSeverity.Error);
            return null;
        }

        return value;
    }

    private static Border CreatePreviewPill(string label, TextBlock valueText)
    {
        return new Border
        {
            Padding = new Thickness(10, 7, 10, 7),
            Background = Microsoft.UI.Xaml.Application.Current.Resources["SubtleFillColorSecondaryBrush"] as Brush,
            BorderBrush = Microsoft.UI.Xaml.Application.Current.Resources["ControlStrokeColorDefaultBrush"] as Brush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Child = new StackPanel
            {
                Spacing = 2,
                Children =
                {
                    new TextBlock
                    {
                        Text = label,
                        FontSize = 12,
                        Foreground = Microsoft.UI.Xaml.Application.Current.Resources["TextFillColorSecondaryBrush"] as Brush
                    },
                    valueText
                }
            }
        };
    }

    private async Task<string?> PromptMultilineTextAsync(string title, string label, string initialValue)
    {
        var input = new TextBox
        {
            Header = label,
            Text = initialValue,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            MinWidth = 520,
            MinHeight = 220
        };

        var dialog = CreateDialog(title, input, "OK");
        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary ? input.Text : null;
    }

    private async Task<string?> PromptExpandableStringAsync(string title, string initialValue)
    {
        var input = new TextBox
        {
            Header = "Value data",
            Text = initialValue,
            AcceptsReturn = false,
            SelectionStart = 0,
            SelectionLength = initialValue.Length,
            MinWidth = 520
        };
        var preview = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Foreground = Microsoft.UI.Xaml.Application.Current.Resources["TextFillColorSecondaryBrush"] as Microsoft.UI.Xaml.Media.Brush
        };

        void UpdatePreview()
        {
            preview.Text = $"Expanded preview: {Environment.ExpandEnvironmentVariables(input.Text)}";
        }

        input.TextChanged += (_, _) => UpdatePreview();
        UpdatePreview();

        var content = new StackPanel { Spacing = 10 };
        content.Children.Add(input);
        content.Children.Add(new Border
        {
            Padding = new Thickness(12, 9, 12, 9),
            Background = Microsoft.UI.Xaml.Application.Current.Resources["SubtleFillColorSecondaryBrush"] as Microsoft.UI.Xaml.Media.Brush,
            BorderBrush = Microsoft.UI.Xaml.Application.Current.Resources["ControlStrokeColorDefaultBrush"] as Microsoft.UI.Xaml.Media.Brush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Child = preview
        });

        var dialog = CreateDialog(title, content, "OK");
        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary ? input.Text : null;
    }

    private async Task<string?> PromptBinaryTextAsync(string title, string initialValue)
    {
        var input = new TextBox
        {
            Header = "Hex bytes",
            Text = NormalizeHexText(initialValue),
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
            MinWidth = 560,
            MinHeight = 260
        };

        var countText = new TextBlock
        {
            Foreground = Microsoft.UI.Xaml.Application.Current.Resources["TextFillColorSecondaryBrush"] as Microsoft.UI.Xaml.Media.Brush
        };

        var normalizeButton = new Button
        {
            Content = "Normalize"
        };

        void UpdateCount()
        {
            countText.Text = TryParseHexBytes(input.Text, out var bytes)
                ? $"{bytes.Length} bytes"
                : "Invalid hex. Use bytes like 04 00 FF.";
        }

        input.TextChanged += (_, _) => UpdateCount();
        normalizeButton.Click += (_, _) =>
        {
            if (TryParseHexBytes(input.Text, out var bytes))
            {
                input.Text = FormatHexDump(bytes);
                input.SelectionStart = input.Text.Length;
            }
        };

        var content = new Grid
        {
            RowSpacing = 8
        };
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        content.Children.Add(input);

        var footer = new Grid { ColumnSpacing = 8 };
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetRow(footer, 1);
        footer.Children.Add(countText);
        Grid.SetColumn(normalizeButton, 1);
        footer.Children.Add(normalizeButton);
        content.Children.Add(footer);

        var help = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Padding = new Thickness(12, 9, 12, 9),
            Background = Microsoft.UI.Xaml.Application.Current.Resources["SubtleFillColorSecondaryBrush"] as Microsoft.UI.Xaml.Media.Brush,
            BorderBrush = Microsoft.UI.Xaml.Application.Current.Resources["ControlStrokeColorDefaultBrush"] as Microsoft.UI.Xaml.Media.Brush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Child = new TextBlock
            {
                Text = "Hex bytes can be separated by spaces, commas, or new lines. The value is stored as raw REG_BINARY data.",
                TextWrapping = TextWrapping.Wrap,
                Foreground = Microsoft.UI.Xaml.Application.Current.Resources["TextFillColorSecondaryBrush"] as Microsoft.UI.Xaml.Media.Brush
            }
        };
        Grid.SetRow(help, 2);
        content.Children.Add(help);

        UpdateCount();

        var dialog = CreateDialog(title, content, "OK");
        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary ? input.Text : null;
    }

    private async Task<bool> ConfirmAsync(string title, string message)
    {
        var dialog = CreateDialog(title, new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap }, "Delete");
        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }

    private async Task<bool> ConfirmImportAsync(string fileName, RegImportDocument document)
    {
        var dialog = CreateDialog("Import Registry File", CreateImportPreview(fileName, document), "Apply");
        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }

    private static UIElement CreateImportPreview(string fileName, RegImportDocument document)
    {
        var createKeys = document.Operations.Count(op => op.OperationKind == RegImportOperationKind.CreateKey);
        var deleteKeys = document.Operations.Count(op => op.OperationKind == RegImportOperationKind.DeleteKey);
        var setValues = document.Operations.Count(op => op.OperationKind == RegImportOperationKind.SetValue);
        var deleteValues = document.Operations.Count(op => op.OperationKind == RegImportOperationKind.DeleteValue);
        var affectedKeys = document.Operations
            .Select(op => op.Path.ToString())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(6)
            .ToArray();

        var content = new StackPanel
        {
            Spacing = 12,
            MaxWidth = 560
        };

        content.Children.Add(new StackPanel
        {
            Spacing = 2,
            Children =
            {
                new TextBlock
                {
                    Text = fileName,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    TextTrimming = TextTrimming.CharacterEllipsis
                },
                new TextBlock
                {
                    Text = "Review the operations before applying them to the registry.",
                    Foreground = Microsoft.UI.Xaml.Application.Current.Resources["TextFillColorSecondaryBrush"] as Brush,
                    TextWrapping = TextWrapping.Wrap
                }
            }
        });

        var statGrid = new Grid { ColumnSpacing = 8, RowSpacing = 8 };
        statGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        statGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        statGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        statGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        AddImportStat(statGrid, 0, 0, "Keys opened", createKeys);
        AddImportStat(statGrid, 0, 1, "Keys deleted", deleteKeys);
        AddImportStat(statGrid, 1, 0, "Values set", setValues);
        AddImportStat(statGrid, 1, 1, "Values deleted", deleteValues);
        content.Children.Add(statGrid);

        if (affectedKeys.Length > 0)
        {
            var keyList = new StackPanel { Spacing = 6 };
            keyList.Children.Add(new TextBlock
            {
                Text = "Affected keys",
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            });

            foreach (var key in affectedKeys)
            {
                keyList.Children.Add(new TextBlock
                {
                    Text = key,
                    FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
                    FontSize = 12,
                    TextTrimming = TextTrimming.CharacterEllipsis
                });
            }

            content.Children.Add(new Border
            {
                Padding = new Thickness(12),
                Background = Microsoft.UI.Xaml.Application.Current.Resources["SubtleFillColorSecondaryBrush"] as Brush,
                BorderBrush = Microsoft.UI.Xaml.Application.Current.Resources["ControlStrokeColorDefaultBrush"] as Brush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Child = keyList
            });
        }

        content.Children.Add(new InfoBar
        {
            IsOpen = true,
            IsClosable = false,
            Severity = deleteKeys > 0 || deleteValues > 0 ? InfoBarSeverity.Warning : InfoBarSeverity.Informational,
            Title = deleteKeys > 0 || deleteValues > 0 ? "This import deletes registry data" : "Ready to apply",
            Message = "The changes are applied directly. Export the target key first if you need a backup copy."
        });

        return content;
    }

    private static void AddImportStat(Grid grid, int row, int column, string label, int count)
    {
        var block = new Border
        {
            Padding = new Thickness(12, 9, 12, 9),
            Background = Microsoft.UI.Xaml.Application.Current.Resources["CardBackgroundFillColorDefaultBrush"] as Brush,
            BorderBrush = Microsoft.UI.Xaml.Application.Current.Resources["ControlStrokeColorDefaultBrush"] as Brush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Child = new StackPanel
            {
                Spacing = 2,
                Children =
                {
                    new TextBlock
                    {
                        Text = count.ToString("N0"),
                        FontSize = 18,
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
                    },
                    new TextBlock
                    {
                        Text = label,
                        Foreground = Microsoft.UI.Xaml.Application.Current.Resources["TextFillColorSecondaryBrush"] as Brush
                    }
                }
            }
        };
        Grid.SetRow(block, row);
        Grid.SetColumn(block, column);
        grid.Children.Add(block);
    }

    private ContentDialog CreateDialog(string title, UIElement content, string primaryText)
    {
        return new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = title,
            Content = content,
            PrimaryButtonText = primaryText,
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary
        };
    }

    private void ShowStatus(string title, string message, InfoBarSeverity severity)
    {
        StatusBar.Title = title;
        StatusBar.Message = message;
        StatusBar.Severity = severity;
        StatusBar.IsOpen = true;
    }

    private static bool TryParseDword(string input, out int value)
    {
        var trimmed = input.Trim();
        if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return int.TryParse(trimmed[2..], System.Globalization.NumberStyles.HexNumber, null, out value);
        }

        return int.TryParse(trimmed, out value);
    }

    private static bool TryParseQword(string input, out ulong value)
    {
        var trimmed = input.Trim();
        if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return ulong.TryParse(trimmed[2..], System.Globalization.NumberStyles.HexNumber, null, out value);
        }

        return ulong.TryParse(trimmed, out value);
    }

    private static bool TryParseInteger(string input, bool hexadecimal, ulong max, out ulong value)
    {
        var trimmed = input.Trim();
        if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[2..];
            hexadecimal = true;
        }

        var ok = hexadecimal
            ? ulong.TryParse(trimmed, System.Globalization.NumberStyles.HexNumber, null, out value)
            : ulong.TryParse(trimmed, out value);
        return ok && value <= max;
    }

    private static bool TryParseHexBytes(string input, out byte[] bytes)
    {
        var tokens = input
            .Replace(',', ' ')
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var parsed = new byte[tokens.Length];
        for (var index = 0; index < tokens.Length; index++)
        {
            var token = tokens[index].StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? tokens[index][2..] : tokens[index];
            if (token.Length is 0 or > 2 || !byte.TryParse(token, System.Globalization.NumberStyles.HexNumber, null, out parsed[index]))
            {
                bytes = [];
                return false;
            }
        }

        bytes = parsed;
        return true;
    }

    private static string NormalizeHexText(string input)
    {
        return TryParseHexBytes(input, out var bytes) ? FormatHexDump(bytes) : input;
    }

    private static string FormatHexDump(byte[] bytes)
    {
        var lines = bytes
            .Select((value, index) => new { value, index })
            .GroupBy(item => item.index / 16)
            .Select(group => string.Join(' ', group.Select(item => item.value.ToString("X2"))));

        return string.Join(Environment.NewLine, lines);
    }

    private static string GetSnapshotSignature(RegistryKeySummary? snapshot)
    {
        if (snapshot is null)
        {
            return string.Empty;
        }

        return string.Join('\n', snapshot.Values
            .OrderBy(value => value.Name, StringComparer.OrdinalIgnoreCase)
            .Select(value => $"{value.Name}\t{value.Kind}\t{value.DisplayData}"))
            + $"\n#subkeys={snapshot.SubKeyCount};#values={snapshot.ValueCount}";
    }

    private static string DescribeSnapshotChange(RegistryKeySummary? before, RegistryKeySummary after)
    {
        if (before is null)
        {
            return $"Now {after.SubKeyCount:N0} subkeys and {after.ValueCount:N0} values.";
        }

        var beforeValues = before.Values.ToDictionary(value => value.Name, StringComparer.OrdinalIgnoreCase);
        var afterValues = after.Values.ToDictionary(value => value.Name, StringComparer.OrdinalIgnoreCase);
        var added = afterValues.Keys.Except(beforeValues.Keys, StringComparer.OrdinalIgnoreCase).ToArray();
        var removed = beforeValues.Keys.Except(afterValues.Keys, StringComparer.OrdinalIgnoreCase).ToArray();
        var changed = afterValues
            .Where(pair => beforeValues.TryGetValue(pair.Key, out var old)
                && (!old.Kind.Equals(pair.Value.Kind, StringComparison.OrdinalIgnoreCase)
                    || !old.DisplayData.Equals(pair.Value.DisplayData, StringComparison.Ordinal)))
            .Select(pair => pair.Key)
            .ToArray();

        var parts = new List<string>();
        if (added.Length > 0)
        {
            parts.Add($"{added.Length:N0} added");
        }

        if (changed.Length > 0)
        {
            parts.Add($"{changed.Length:N0} changed");
        }

        if (removed.Length > 0)
        {
            parts.Add($"{removed.Length:N0} removed");
        }

        if (before.SubKeyCount != after.SubKeyCount)
        {
            parts.Add($"subkeys {before.SubKeyCount:N0} -> {after.SubKeyCount:N0}");
        }

        return parts.Count == 0 ? "Counts changed." : string.Join(", ", parts);
    }

    private static string QuotePowerShell(string value)
    {
        return $"'{value.Replace("'", "''", StringComparison.Ordinal)}'";
    }

    private void CaptureWriteJournalEntry(string actionName)
    {
        if (_currentPath is null)
        {
            return;
        }

        try
        {
            RegistryJournalStore.Add(new RegistryJournalEntry(DateTimeOffset.Now, _currentPath, GetCurrentView(), actionName, _browser.ExportRegTree(_currentPath, GetCurrentView())));
        }
        catch (Exception ex) when (IsRecoverableRegistryException(ex))
        {
            UpdateStatus($"Write journal skipped: {ex.Message}");
        }
    }

    private static bool IsRecoverableRegistryException(Exception ex)
    {
        return ex is UnauthorizedAccessException
            or System.Security.SecurityException
            or IOException
            or ArgumentException
            or InvalidOperationException
            or RegistryKeyNotFoundException;
    }

    private static void CopyText(string text)
    {
        var package = new DataPackage();
        package.SetText(text);
        Clipboard.SetContent(package);
    }

    private static TreeViewNode? FindNode(IList<TreeViewNode> nodes, Func<TreeViewNode, bool> predicate)
    {
        foreach (var node in nodes)
        {
            if (predicate(node))
            {
                return node;
            }

            var child = FindNode(node.Children, predicate);
            if (child is not null)
            {
                return child;
            }
        }

        return null;
    }

    private sealed record LoadMoreState(
        TreeViewNode Parent,
        RegistryPath ParentPath,
        IReadOnlyList<string> Names,
        int NextIndex);

}
