using Microsoft.UI.Xaml;
using Registry_App.Services;
using Windows.Storage;
using System.Text;

namespace Registry_App;

public partial class App : Application
{
    private Window? _window;
    public static Window? MainWindow { get; private set; }

    public App()
    {
        InitializeComponent();
        UnhandledException += (_, args) => WriteStartupFailure(args.Message, args.Exception);
        LocalizationService.Initialize();
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        var registryFilePath = GetRegistryFilePath(args.Arguments);
        if (registryFilePath is not null && RegistryFileActivationRouter.TryForwardToExistingInstance(registryFilePath))
        {
            Environment.Exit(0);
        }

        try
        {
            RegOpenWithRegistration.Register();
        }
        catch
        {
            // File integration is optional and must not prevent Registry from opening.
        }

        try
        {
            _window = new MainWindow();
            MainWindow = _window;
            _window.Activate();
            RegistryFileActivationRouter.Start(HandleForwardedRegistryFile);
            if (registryFilePath is not null)
            {
                ((MainWindow)_window).OpenRegistryFilePath(registryFilePath);
            }
        }
        catch (Exception exception)
        {
            WriteStartupFailure("Main window initialization failed.", exception);
            throw;
        }
    }

    private static void WriteStartupFailure(string message, Exception? exception)
    {
        try
        {
            var directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Chromatic",
                "Registry");
            Directory.CreateDirectory(directory);
            File.AppendAllText(
                Path.Combine(directory, "startup-failures.log"),
                $"{DateTimeOffset.Now:O}{Environment.NewLine}{message}{Environment.NewLine}{exception}{Environment.NewLine}{Environment.NewLine}",
                Encoding.UTF8);
        }
        catch
        {
            // Diagnostics must never interfere with the original failure.
        }
    }

    private static string? GetRegistryFilePath(string arguments)
    {
        var candidates = new[] { arguments }
            .Concat(Environment.GetCommandLineArgs().Skip(1));
        return candidates
            .Select(candidate => candidate.Trim().Trim('"'))
            .FirstOrDefault(candidate => candidate.EndsWith(".reg", StringComparison.OrdinalIgnoreCase) && File.Exists(candidate));
    }

    private void HandleForwardedRegistryFile(string path)
    {
        if (_window is MainWindow mainWindow)
        {
            mainWindow.DispatcherQueue.TryEnqueue(() => mainWindow.OpenRegistryFilePath(path));
        }
    }
}
