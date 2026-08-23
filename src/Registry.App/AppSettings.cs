namespace Registry_App;

public static class AppSettings
{
    private const string ToolbarAlignmentKey = "ToolbarAlignment";
    private const string ToolbarDetailKey = "ToolbarDetail";
    private const string BackdropStyleKey = "BackdropStyle";
    private const string RegImportModeKey = "RegImportMode";
    private const string DefaultToolbarAlignment = "Left";
    private const string DefaultToolbarDetail = "Essential";
    private const string DefaultBackdropStyle = "Mica";
    private const string DefaultRegImportMode = "Modal";

    public static event EventHandler? Changed;

    public static string ToolbarAlignment
    {
        get => RegistryAppData.GetSetting(ToolbarAlignmentKey) ?? DefaultToolbarAlignment;
        set
        {
            var normalized = NormalizeToolbarAlignment(value);
            RegistryAppData.SetSetting(ToolbarAlignmentKey, normalized);
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
        get => RegistryAppData.GetSetting(ToolbarDetailKey) ?? DefaultToolbarDetail;
        set
        {
            var normalized = NormalizeToolbarDetail(value);
            RegistryAppData.SetSetting(ToolbarDetailKey, normalized);
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
        get => RegistryAppData.GetSetting(BackdropStyleKey) ?? DefaultBackdropStyle;
        set
        {
            var normalized = NormalizeBackdropStyle(value);
            RegistryAppData.SetSetting(BackdropStyleKey, normalized);
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

    public static string RegImportMode
    {
        get => RegistryAppData.GetSetting(RegImportModeKey) ?? DefaultRegImportMode;
        set
        {
            var normalized = value == "NewWindow" ? "NewWindow" : "Modal";
            RegistryAppData.SetSetting(RegImportModeKey, normalized);
            Changed?.Invoke(null, EventArgs.Empty);
        }
    }

    public static int RegImportModeIndex
    {
        get => RegImportMode == "NewWindow" ? 1 : 0;
        set => RegImportMode = value == 1 ? "NewWindow" : "Modal";
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
