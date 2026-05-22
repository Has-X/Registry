using Registry.Core;

var exitCode = Run(args);
Environment.Exit(exitCode);

static int Run(string[] args)
{
    if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
    {
        PrintHelp();
        return 0;
    }

    var browser = new RegistryBrowser();
    var command = args[0].ToLowerInvariant();

    try
    {
        return command switch
        {
            "roots" => PrintRoots(browser),
            "ls" or "list" => ListKey(browser, args),
            "find" => Find(browser, args),
            "get" => GetValue(browser, args),
            "set-string" => SetStringValue(browser, args),
            "set-dword" => SetDwordValue(browser, args),
            "delete-value" => DeleteValue(browser, args),
            "rename-value" => RenameValue(browser, args),
            "create-key" => CreateKey(browser, args),
            "delete-key" => DeleteKey(browser, args),
            "rename-key" => RenameKey(browser, args),
            "load-hive" => LoadHive(browser, args),
            "unload-hive" => UnloadHive(browser, args),
            "import" => ImportReg(browser, args),
            "export" => ExportKey(browser, args),
            "export-tree" => ExportKeyTree(browser, args),
            _ => UnknownCommand(command)
        };
    }
    catch (Exception ex) when (ex is ArgumentException or FormatException or RegistryKeyNotFoundException or UnauthorizedAccessException or InvalidOperationException or System.ComponentModel.Win32Exception)
    {
        Console.Error.WriteLine($"registry: {ex.Message}");
        return 2;
    }
    catch (OperationCanceledException)
    {
        Console.Error.WriteLine("registry: search timed out.");
        return 3;
    }
}

static int PrintRoots(RegistryBrowser browser)
{
    foreach (var root in browser.GetRootPaths())
    {
        Console.WriteLine(root);
    }

    return 0;
}

static int ListKey(RegistryBrowser browser, string[] args)
{
    var path = GetPathArgument(args);
    var view = GetView(args);
    var snapshot = browser.ReadKey(path, view);

    Console.WriteLine(snapshot.Path);
    Console.WriteLine();
    Console.WriteLine("Subkeys");
    foreach (var subKey in snapshot.SubKeyNames)
    {
        Console.WriteLine($"  [{subKey}]");
    }

    Console.WriteLine();
    Console.WriteLine("Values");
    foreach (var value in snapshot.Values)
    {
        Console.WriteLine($"  {value.DisplayName,-32} {value.Kind,-12} {value.DisplayData}");
    }

    return 0;
}

static int GetValue(RegistryBrowser browser, string[] args)
{
    if (args.Length < 3)
    {
        throw new ArgumentException("Usage: registry get <key> <value-name> [--32|--64]. Use @ for the default value.");
    }

    var snapshot = browser.ReadKey(RegistryPath.Parse(args[1]), GetView(args));
    var requestedName = args[2] == "@" ? string.Empty : args[2];
    var value = snapshot.Values.FirstOrDefault(candidate => candidate.Name.Equals(requestedName, StringComparison.OrdinalIgnoreCase));
    if (value is null)
    {
        throw new ArgumentException($"Value '{args[2]}' was not found in '{snapshot.Path}'.");
    }

    Console.WriteLine(value.DisplayData);
    return 0;
}

static int Find(RegistryBrowser browser, string[] args)
{
    if (args.Length < 3)
    {
        throw new ArgumentException("Usage: registry find <start-key> <text> [--keys] [--names] [--data] [--case] [--whole] [--32|--64].");
    }

    var hasExplicitScope = args.Contains("--keys", StringComparer.OrdinalIgnoreCase)
        || args.Contains("--names", StringComparer.OrdinalIgnoreCase)
        || args.Contains("--data", StringComparer.OrdinalIgnoreCase);
    var options = new RegistrySearchOptions(
        args[2],
        !hasExplicitScope || args.Contains("--keys", StringComparer.OrdinalIgnoreCase),
        !hasExplicitScope || args.Contains("--names", StringComparer.OrdinalIgnoreCase),
        !hasExplicitScope || args.Contains("--data", StringComparer.OrdinalIgnoreCase),
        args.Contains("--case", StringComparer.OrdinalIgnoreCase),
        args.Contains("--whole", StringComparer.OrdinalIgnoreCase));
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
    var result = browser.FindFirst(RegistryPath.Parse(args[1]), options, GetView(args), timeout.Token);
    if (result is null)
    {
        return 1;
    }

    Console.WriteLine(result.Path);
    Console.WriteLine($"{result.MatchKind}: {result.DisplayText}");
    if (!string.IsNullOrEmpty(result.ValueName))
    {
        Console.WriteLine($"Value: {(result.ValueName.Length == 0 ? "@" : result.ValueName)}");
    }

    return 0;
}

static int ExportKey(RegistryBrowser browser, string[] args)
{
    var path = GetPathArgument(args);
    var view = GetView(args);
    Console.Write(browser.ExportReg(path, view));
    return 0;
}

static int ExportKeyTree(RegistryBrowser browser, string[] args)
{
    var path = GetPathArgument(args);
    var view = GetView(args);
    Console.Write(browser.ExportRegTree(path, view));
    return 0;
}

static int SetStringValue(RegistryBrowser browser, string[] args)
{
    if (args.Length < 4)
    {
        throw new ArgumentException("Usage: registry set-string <key> <value-name|@> <data> [--32|--64].");
    }

    var name = args[2] == "@" ? string.Empty : args[2];
    browser.SetValue(RegistryPath.Parse(args[1]), name, Microsoft.Win32.RegistryValueKind.String, args[3], GetView(args));
    return 0;
}

static int SetDwordValue(RegistryBrowser browser, string[] args)
{
    if (args.Length < 4)
    {
        throw new ArgumentException("Usage: registry set-dword <key> <value-name|@> <data> [--32|--64].");
    }

    if (!TryParseDword(args[3], out var value))
    {
        throw new ArgumentException("DWORD data must be decimal or hexadecimal like 0x2a.");
    }

    var name = args[2] == "@" ? string.Empty : args[2];
    browser.SetValue(RegistryPath.Parse(args[1]), name, Microsoft.Win32.RegistryValueKind.DWord, value, GetView(args));
    return 0;
}

static int DeleteValue(RegistryBrowser browser, string[] args)
{
    if (args.Length < 3)
    {
        throw new ArgumentException("Usage: registry delete-value <key> <value-name|@> [--32|--64].");
    }

    var name = args[2] == "@" ? string.Empty : args[2];
    browser.DeleteValue(RegistryPath.Parse(args[1]), name, GetView(args));
    return 0;
}

static int RenameValue(RegistryBrowser browser, string[] args)
{
    if (args.Length < 4)
    {
        throw new ArgumentException("Usage: registry rename-value <key> <old-name|@> <new-name|@> [--32|--64].");
    }

    var oldName = args[2] == "@" ? string.Empty : args[2];
    var newName = args[3] == "@" ? string.Empty : args[3];
    browser.RenameValue(RegistryPath.Parse(args[1]), oldName, newName, GetView(args));
    return 0;
}

static int CreateKey(RegistryBrowser browser, string[] args)
{
    if (args.Length < 3)
    {
        throw new ArgumentException("Usage: registry create-key <parent-key> <name> [--32|--64].");
    }

    browser.CreateSubKey(RegistryPath.Parse(args[1]), args[2], GetView(args));
    return 0;
}

static int DeleteKey(RegistryBrowser browser, string[] args)
{
    if (args.Length < 2)
    {
        throw new ArgumentException("Usage: registry delete-key <key> [--32|--64].");
    }

    browser.DeleteSubKeyTree(RegistryPath.Parse(args[1]), GetView(args));
    return 0;
}

static int RenameKey(RegistryBrowser browser, string[] args)
{
    if (args.Length < 3)
    {
        throw new ArgumentException("Usage: registry rename-key <key> <new-name> [--32|--64].");
    }

    browser.RenameSubKey(RegistryPath.Parse(args[1]), args[2], GetView(args));
    return 0;
}

static int ImportReg(RegistryBrowser browser, string[] args)
{
    if (args.Length < 2)
    {
        throw new ArgumentException("Usage: registry import <file.reg> [--32|--64].");
    }

    var content = File.ReadAllText(args[1]);
    var document = RegImportParser.Parse(content);
    browser.ApplyImport(document, GetView(args));
    Console.WriteLine($"Applied {document.Operations.Count:N0} operations.");
    return 0;
}

static int LoadHive(RegistryBrowser browser, string[] args)
{
    if (args.Length < 4)
    {
        throw new ArgumentException("Usage: registry load-hive <HKLM|HKU> <mount-name> <hive-file> [--32|--64].");
    }

    var hive = RegistryPath.Parse(args[1]).Hive;
    var path = browser.LoadHive(hive, args[2], args[3], GetView(args));
    Console.WriteLine(path);
    return 0;
}

static int UnloadHive(RegistryBrowser browser, string[] args)
{
    if (args.Length < 2)
    {
        throw new ArgumentException("Usage: registry unload-hive <HKLM\\mount-name|HKU\\mount-name>.");
    }

    browser.UnloadHive(RegistryPath.Parse(args[1]));
    return 0;
}

static RegistryPath GetPathArgument(string[] args)
{
    if (args.Length < 2)
    {
        throw new ArgumentException("A registry key path is required.");
    }

    return RegistryPath.Parse(args[1]);
}

static RegistryViewMode GetView(string[] args)
{
    if (args.Contains("--32", StringComparer.OrdinalIgnoreCase))
    {
        return RegistryViewMode.Registry32;
    }

    if (args.Contains("--64", StringComparer.OrdinalIgnoreCase))
    {
        return RegistryViewMode.Registry64;
    }

    return RegistryViewMode.Default;
}

static bool TryParseDword(string input, out int value)
{
    var trimmed = input.Trim();
    if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
    {
        return int.TryParse(trimmed[2..], System.Globalization.NumberStyles.HexNumber, null, out value);
    }

    return int.TryParse(trimmed, out value);
}

static int UnknownCommand(string command)
{
    Console.Error.WriteLine($"registry: unknown command '{command}'.");
    PrintHelp();
    return 2;
}

static void PrintHelp()
{
    Console.WriteLine("Registry CLI");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  registry roots");
    Console.WriteLine("  registry ls <key> [--32|--64]");
    Console.WriteLine("  registry find <start-key> <text> [--keys] [--names] [--data] [--case] [--whole] [--32|--64]");
    Console.WriteLine("  registry get <key> <value-name|@> [--32|--64]");
    Console.WriteLine("  registry set-string <key> <value-name|@> <data> [--32|--64]");
    Console.WriteLine("  registry set-dword <key> <value-name|@> <data> [--32|--64]");
    Console.WriteLine("  registry delete-value <key> <value-name|@> [--32|--64]");
    Console.WriteLine("  registry rename-value <key> <old-name|@> <new-name|@> [--32|--64]");
    Console.WriteLine("  registry create-key <parent-key> <name> [--32|--64]");
    Console.WriteLine("  registry delete-key <key> [--32|--64]");
    Console.WriteLine("  registry rename-key <key> <new-name> [--32|--64]");
    Console.WriteLine("  registry load-hive <HKLM|HKU> <mount-name> <hive-file> [--32|--64]");
    Console.WriteLine("  registry unload-hive <HKLM\\mount-name|HKU\\mount-name>");
    Console.WriteLine("  registry import <file.reg> [--32|--64]");
    Console.WriteLine("  registry export <key> [--32|--64]");
    Console.WriteLine("  registry export-tree <key> [--32|--64]");
}
