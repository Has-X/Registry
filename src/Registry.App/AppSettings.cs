using Windows.Storage;

namespace Registry_App;

public static class AppSettings
{
    private const string ToolbarAlignmentKey = "ToolbarAlignment";
    private const string ToolbarDetailKey = "ToolbarDetail";
    private const string BackdropStyleKey = "BackdropStyle";
    private const string DefaultToolbarAlignment = "Left";
    private const string DefaultToolbarDetail = "Essential";
    private const string DefaultBackdropStyle = "Mica";

    public static event EventHandler? Changed;

    public static string ToolbarAlignment
    {
        get => ApplicationData.Current.LocalSettings.Values[ToolbarAlignmentKey] as string ?? DefaultToolbarAlignment;
        set
        {
            var normalized = NormalizeToolbarAlignment(value);
            ApplicationData.Current.LocalSettings.Values[ToolbarAlignmentKey] = normalized;
            Changed?.Invoke(null, EventArgs.Empty);
        }
    }

    public static int ToolbarAlignmentIndex
    {
        get => ToolbarAlignment switch
        {
            "Center" => 1,
            "Right" => 2,
            _ => 0
        };
        set => ToolbarAlignment = value switch
        {
            1 => "Center",
            2 => "Right",
            _ => "Left"
        };
    }

    public static string ToolbarDetail
    {
        get => ApplicationData.Current.LocalSettings.Values[ToolbarDetailKey] as string ?? DefaultToolbarDetail;
        set
        {
            var normalized = NormalizeToolbarDetail(value);
            ApplicationData.Current.LocalSettings.Values[ToolbarDetailKey] = normalized;
            Changed?.Invoke(null, EventArgs.Empty);
        }
    }

    public static int ToolbarDetailIndex
    {
        get => ToolbarDetail switch
        {
            "Full" => 1,
            _ => 0
        };
        set => ToolbarDetail = value == 1 ? "Full" : "Essential";
    }

    public static string BackdropStyle
    {
        get => ApplicationData.Current.LocalSettings.Values[BackdropStyleKey] as string ?? DefaultBackdropStyle;
        set
        {
            var normalized = NormalizeBackdropStyle(value);
            ApplicationData.Current.LocalSettings.Values[BackdropStyleKey] = normalized;
            Changed?.Invoke(null, EventArgs.Empty);
        }
    }

    public static int BackdropStyleIndex
    {
        get => BackdropStyle switch
        {
            "StrongMica" => 1,
            "Off" => 2,
            _ => 0
        };
        set => BackdropStyle = value switch
        {
            1 => "StrongMica",
            2 => "Off",
            _ => "Mica"
        };
    }

    private static string NormalizeToolbarAlignment(string value)
    {
        return value switch
        {
            "Center" => "Center",
            "Right" => "Right",
            _ => "Left"
        };
    }

    private static string NormalizeToolbarDetail(string value)
    {
        return value == "Full" ? "Full" : "Essential";
    }

    private static string NormalizeBackdropStyle(string value)
    {
        return value switch
        {
            "StrongMica" => "StrongMica",
            "Off" => "Off",
            _ => "Mica"
        };
    }
}
