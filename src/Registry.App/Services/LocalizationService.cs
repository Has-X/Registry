using System.Globalization;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Registry_App.Services;

/// <summary>Runtime UI localization with an authored English safety fallback.</summary>
public static class LocalizationService
{
    private static readonly string[] SupportedLanguages =
    {
        "en", "hu", "es", "de", "fr", "it", "pl", "pt-BR", "pt-PT", "tr", "id", "ro", "cs", "sk", "ru", "uk",
        "zh-CN", "zh-TW", "ar", "vi", "th", "hi", "ja", "ko", "nl", "el", "bg", "hr", "sr", "sl", "sv", "da", "fi", "nb"
    };
    private static IReadOnlyDictionary<string, string> _strings = new Dictionary<string, string>();

    public static string CurrentLanguage { get; private set; } = "en";

    public static void Initialize(string? requestedLanguage = null)
    {
        CurrentLanguage = ResolveLanguage(requestedLanguage ?? CultureInfo.CurrentUICulture.Name);
        var english = LoadCatalog("en");
        var requested = string.Equals(CurrentLanguage, "en", StringComparison.OrdinalIgnoreCase) ? english : LoadCatalog(CurrentLanguage);
        _strings = english.Concat(requested).GroupBy(pair => pair.Key, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Last().Value, StringComparer.Ordinal);
    }

    public static string Get(string key, string fallback) =>
        _strings.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;

    public static string Format(string key, string fallback, params object?[] arguments) =>
        string.Format(CultureInfo.CurrentCulture, Get(key, fallback), arguments);

    public static string StatusTitle(string authoredTitle)
    {
        var slug = new string(authoredTitle.Trim().ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '-')
            .ToArray());
        while (slug.Contains("--", StringComparison.Ordinal)) slug = slug.Replace("--", "-", StringComparison.Ordinal);
        return Get($"status.{slug.Trim('-')}", authoredTitle);
    }

    public static void Apply(FrameworkElement root)
    {
        ApplyTree(root);
    }

    private static IReadOnlyDictionary<string, string> LoadCatalog(string language)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Resources", "locales", language, "windows.json");
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path)) ?? new Dictionary<string, string>();
        }
        catch
        {
            return new Dictionary<string, string>();
        }
    }

    private static string ResolveLanguage(string requested)
    {
        if (SupportedLanguages.Contains(requested, StringComparer.OrdinalIgnoreCase))
            return SupportedLanguages.First(language => string.Equals(language, requested, StringComparison.OrdinalIgnoreCase));
        var baseLanguage = requested.Split('-', '_')[0].ToLowerInvariant();
        return baseLanguage switch
        {
            "pt" => "pt-BR",
            "zh" => requested.Contains("TW", StringComparison.OrdinalIgnoreCase) ? "zh-TW" : "zh-CN",
            _ => SupportedLanguages.FirstOrDefault(language => language.Equals(baseLanguage, StringComparison.OrdinalIgnoreCase)) ?? "en"
        };
    }

    private static void ApplyTree(FrameworkElement element)
    {
        ApplyElement(element);
        ApplyLogicalChildren(element);
        ApplyVisualChildren(element);
    }

    private static void ApplyLogicalChildren(FrameworkElement element)
    {
        IEnumerable<FrameworkElement> children = element switch
        {
            NavigationView navigationView => navigationView.MenuItems.Concat(navigationView.FooterMenuItems).OfType<FrameworkElement>(),
            CommandBar commandBar => commandBar.PrimaryCommands.Concat(commandBar.SecondaryCommands).OfType<FrameworkElement>(),
            ComboBox comboBox => comboBox.Items.OfType<FrameworkElement>(),
            AppBarButton { Flyout: MenuFlyout flyout } => flyout.Items.OfType<FrameworkElement>(),
            _ => Enumerable.Empty<FrameworkElement>()
        };

        foreach (var child in children)
        {
            ApplyTree(child);
        }
    }

    private static void ApplyVisualChildren(DependencyObject parent)
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            if (VisualTreeHelper.GetChild(parent, index) is FrameworkElement child)
            {
                ApplyTree(child);
            }
        }
    }

    private static void ApplyElement(FrameworkElement element)
    {
        var key = element.Tag as string;
        if (element is NavigationViewItem && key is not null && !_strings.ContainsKey(key))
        {
            key = key switch
            {
                "home" => "app.title",
                "favorites" => "nav.favorites",
                "journal" => "nav.journal",
                "about" => "nav.about",
                _ => key
            };
        }
        if (string.IsNullOrWhiteSpace(key) || !_strings.ContainsKey(key))
        {
            var automationKey = AutomationProperties.GetName(element);
            key = !string.IsNullOrWhiteSpace(automationKey) && automationKey.Contains('.') ? automationKey : null;
        }
        if (string.IsNullOrWhiteSpace(key) || !_strings.TryGetValue(key, out var value)) return;
        var tooltipKey = AutomationProperties.GetHelpText(element);
        if (string.IsNullOrWhiteSpace(tooltipKey))
        {
            tooltipKey = AutomationProperties.GetName(element);
        }
        if (ToolTipService.GetToolTip(element) is not null && !string.IsNullOrWhiteSpace(tooltipKey) && _strings.TryGetValue(tooltipKey, out var tooltipValue))
        {
            ToolTipService.SetToolTip(element, tooltipValue);
        }
        switch (element)
        {
            case TextBlock textBlock: textBlock.Text = value; break;
            case Button button when button.Content is not UIElement: button.Content = value; break;
            case NavigationViewItem navigationItem: navigationItem.Content = value; break;
            case AppBarButton appBarButton: appBarButton.Label = value; break;
            case AppBarToggleButton appBarToggleButton: appBarToggleButton.Label = value; break;
            case ComboBoxItem comboBoxItem: comboBoxItem.Content = value; break;
            case TextBox textBox: textBox.PlaceholderText = value; break;
            case CheckBox checkBox: checkBox.Content = value; break;
            case MenuFlyoutItem menuFlyoutItem: menuFlyoutItem.Text = value; break;
        }
    }
}
