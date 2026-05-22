using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Windows.ApplicationModel.Activation;
using Windows.Storage;

namespace Registry_App;

public partial class App : Application
{
    private Window? _window;
    public static Window? MainWindow { get; private set; }

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        MainWindow = _window;
        _window.Activate();
        HandleActivation(AppInstance.GetCurrent().GetActivatedEventArgs());
        HandleLaunchArguments(args.Arguments);
    }

    private void HandleActivation(AppActivationArguments args)
    {
        if (_window is not MainWindow mainWindow)
        {
            return;
        }

        if (args.Kind == ExtendedActivationKind.File
            && args.Data is IFileActivatedEventArgs fileArgs
            && fileArgs.Files.FirstOrDefault() is StorageFile file)
        {
            mainWindow.OpenRegistryFile(file);
        }
    }

    private void HandleLaunchArguments(string arguments)
    {
        if (_window is not MainWindow mainWindow || string.IsNullOrWhiteSpace(arguments))
        {
            return;
        }

        var path = arguments.Trim().Trim('"');
        if (path.EndsWith(".reg", StringComparison.OrdinalIgnoreCase) && File.Exists(path))
        {
            mainWindow.OpenRegistryFilePath(path);
        }
    }
}
