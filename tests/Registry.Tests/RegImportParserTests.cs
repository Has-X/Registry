using Microsoft.Win32;
using Registry.Core;

namespace Registry.Tests;

public sealed class RegImportParserTests
{
    [Fact]
    public void ParseSupportsCommonRegValueKinds()
    {
        var document = RegImportParser.Parse("""
            Windows Registry Editor Version 5.00

            [HKEY_CURRENT_USER\Software\Registry.Tests]
            "Text"="hello"
            "Number"=dword:0000002a
            "Big"=hex(b):2a,00,00,00,00,00,00,00
            "Bytes"=hex:04,00,ff
            "Expanded"=hex(2):25,00,55,00,53,00,45,00,52,00,50,00,52,00,4f,00,46,00,49,00,4c,00,45,00,25,00,00,00
            "Many"=hex(7):6f,00,6e,00,65,00,00,00,74,00,77,00,6f,00,00,00,00,00
            """);

        Assert.Equal(7, document.Operations.Count);
        Assert.Contains(document.Operations, op => op.OperationKind == RegImportOperationKind.SetValue && op.ValueName == "Text" && op.ValueKind == RegistryValueKind.String && (string)op.ValueData! == "hello");
        Assert.Contains(document.Operations, op => op.OperationKind == RegImportOperationKind.SetValue && op.ValueName == "Number" && op.ValueKind == RegistryValueKind.DWord && (int)op.ValueData! == 42);
        Assert.Contains(document.Operations, op => op.OperationKind == RegImportOperationKind.SetValue && op.ValueName == "Big" && op.ValueKind == RegistryValueKind.QWord && (ulong)op.ValueData! == 42UL);
        Assert.Contains(document.Operations, op => op.OperationKind == RegImportOperationKind.SetValue && op.ValueName == "Bytes" && op.ValueKind == RegistryValueKind.Binary && ((byte[])op.ValueData!).SequenceEqual(new byte[] { 0x04, 0x00, 0xff }));
        Assert.Contains(document.Operations, op => op.OperationKind == RegImportOperationKind.SetValue && op.ValueName == "Expanded" && op.ValueKind == RegistryValueKind.ExpandString && (string)op.ValueData! == "%USERPROFILE%");
        Assert.Contains(document.Operations, op => op.OperationKind == RegImportOperationKind.SetValue && op.ValueName == "Many" && op.ValueKind == RegistryValueKind.MultiString && ((string[])op.ValueData!).SequenceEqual(["one", "two"]));
    }

    [Fact]
    public void ApplyImportWritesValues()
    {
        var browser = new RegistryBrowser();
        var root = RegistryPath.Parse($@"HKCU\Software\Registry.Import.Tests.{Guid.NewGuid():N}");
        try
        {
            var document = RegImportParser.Parse($$"""
                Windows Registry Editor Version 5.00

                [{{root}}]
                "Text"="hello"
                "Number"=dword:0000002a
                """);

            browser.ApplyImport(document);

            var snapshot = browser.ReadKey(root);
            Assert.Contains(snapshot.Values, value => value.Name == "Text" && value.DisplayData == "hello");
            Assert.Contains(snapshot.Values, value => value.Name == "Number" && value.DisplayData == "42");
        }
        finally
        {
            try
            {
                browser.DeleteSubKeyTree(root);
            }
            catch
            {
                // Cleanup must not hide the original test result.
            }
        }
    }
}
