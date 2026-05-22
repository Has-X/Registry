using Microsoft.Win32;
using Registry.Core;

namespace Registry.Tests;

public sealed class RegistryBrowserWriteTests : IDisposable
{
    private readonly RegistryBrowser _browser = new();
    private readonly RegistryPath _testRoot;

    public RegistryBrowserWriteTests()
    {
        _testRoot = RegistryPath.Parse($@"HKCU\Software\Registry.Tests.{Guid.NewGuid():N}");
        _browser.CreateSubKey(RegistryPath.Parse(@"HKCU\Software"), RegistryBrowser.GetLeafName(_testRoot));
    }

    [Fact]
    public void SetValueReadValueAndDeleteValueRoundTrips()
    {
        _browser.SetValue(_testRoot, "Name", RegistryValueKind.String, "Registry test");
        _browser.SetValue(_testRoot, "Number", RegistryValueKind.DWord, 42);

        var snapshot = _browser.ReadKey(_testRoot);

        Assert.Contains(snapshot.Values, value => value.Name == "Name" && value.DisplayData == "Registry test");
        Assert.Contains(snapshot.Values, value => value.Name == "Number" && value.DisplayData == "42");

        _browser.DeleteValue(_testRoot, "Name");
        snapshot = _browser.ReadKey(_testRoot);

        Assert.DoesNotContain(snapshot.Values, value => value.Name == "Name");
    }

    [Fact]
    public void CreateSubKeyAppearsInParentSnapshot()
    {
        _browser.CreateSubKey(_testRoot, "Child");

        var snapshot = _browser.ReadKey(_testRoot);

        Assert.Contains("Child", snapshot.SubKeyNames);
    }

    [Fact]
    public void ReadKeySummaryReturnsCountsWithoutSubkeyNames()
    {
        _browser.CreateSubKey(_testRoot, "Child");
        _browser.SetValue(_testRoot, "Name", RegistryValueKind.String, "Registry test");

        var summary = _browser.ReadKeySummary(_testRoot);

        Assert.Equal(1, summary.SubKeyCount);
        Assert.Equal(1, summary.ValueCount);
        Assert.Contains(summary.Values, value => value.Name == "Name");
    }

    [Fact]
    public void ExportRegTreeIncludesNestedKeys()
    {
        _browser.SetValue(_testRoot, "RootValue", RegistryValueKind.String, "root");
        _browser.CreateSubKey(_testRoot, "Child");
        _browser.SetValue(_browser.Combine(_testRoot, "Child"), "ChildValue", RegistryValueKind.String, "child");

        var export = _browser.ExportRegTree(_testRoot);

        Assert.Contains($"[{_testRoot}]", export);
        Assert.Contains($@"[{_testRoot}\Child]", export);
        Assert.Contains("\"RootValue\"=\"root\"", export);
        Assert.Contains("\"ChildValue\"=\"child\"", export);
    }

    [Fact]
    public void ExtendedValueKindsRoundTripAndExport()
    {
        _browser.SetValue(_testRoot, "Bytes", RegistryValueKind.Binary, new byte[] { 0x04, 0x00, 0xff });
        _browser.SetValue(_testRoot, "Big", RegistryValueKind.QWord, 42UL);
        _browser.SetValue(_testRoot, "Many", RegistryValueKind.MultiString, new[] { "one", "two" });

        var snapshot = _browser.ReadKey(_testRoot);

        Assert.Contains(snapshot.Values, value => value.Name == "Bytes" && value.DisplayData == "04 00 FF");
        Assert.Contains(snapshot.Values, value => value.Name == "Big" && value.DisplayData == "42");
        Assert.Contains(snapshot.Values, value => value.Name == "Many" && value.DisplayData == "one; two");

        var export = _browser.ExportReg(_testRoot);

        Assert.Contains("\"Bytes\"=hex:04,00,ff", export);
        Assert.Contains("\"Big\"=hex(b):2a,00,00,00,00,00,00,00", export);
        Assert.Contains("\"Many\"=hex(7):", export);
    }

    [Fact]
    public void RenameValuePreservesKindAndData()
    {
        _browser.SetValue(_testRoot, "OldName", RegistryValueKind.DWord, 42);

        _browser.RenameValue(_testRoot, "OldName", "NewName");

        var snapshot = _browser.ReadKey(_testRoot);
        Assert.DoesNotContain(snapshot.Values, value => value.Name == "OldName");
        Assert.Contains(snapshot.Values, value => value.Name == "NewName" && value.Kind == nameof(RegistryValueKind.DWord) && value.DisplayData == "42");
    }

    [Fact]
    public void RenameSubKeyPreservesValuesAndChildren()
    {
        _browser.CreateSubKey(_testRoot, "OldKey");
        var oldPath = _browser.Combine(_testRoot, "OldKey");
        _browser.SetValue(oldPath, "Name", RegistryValueKind.String, "root");
        _browser.CreateSubKey(oldPath, "Child");
        _browser.SetValue(_browser.Combine(oldPath, "Child"), "ChildName", RegistryValueKind.String, "child");

        var newPath = _browser.RenameSubKey(oldPath, "NewKey");

        var parent = _browser.ReadKey(_testRoot);
        Assert.DoesNotContain("OldKey", parent.SubKeyNames);
        Assert.Contains("NewKey", parent.SubKeyNames);
        Assert.Contains(_browser.ReadKey(newPath).Values, value => value.Name == "Name" && value.DisplayData == "root");
        Assert.Contains(_browser.ReadKey(_browser.Combine(newPath, "Child")).Values, value => value.Name == "ChildName" && value.DisplayData == "child");
    }

    [Fact]
    public void FindFirstMatchesKeysValueNamesAndValueData()
    {
        _browser.CreateSubKey(_testRoot, "SearchChild");
        var child = _browser.Combine(_testRoot, "SearchChild");
        _browser.SetValue(child, "NeedleName", RegistryValueKind.String, "quiet");
        _browser.SetValue(child, "Other", RegistryValueKind.String, "needle-data");

        var keyResult = _browser.FindFirst(_testRoot, new RegistrySearchOptions("SearchChild", MatchValueNames: false, MatchValueData: false));
        var nameResult = _browser.FindFirst(_testRoot, new RegistrySearchOptions("NeedleName", MatchKeys: false, MatchValueData: false));
        var dataResult = _browser.FindFirst(_testRoot, new RegistrySearchOptions("needle-data", MatchKeys: false, MatchValueNames: false));

        Assert.Equal(RegistrySearchMatchKind.KeyName, keyResult?.MatchKind);
        Assert.Equal(child, keyResult?.Path);
        Assert.Equal(RegistrySearchMatchKind.ValueName, nameResult?.MatchKind);
        Assert.Equal("NeedleName", nameResult?.ValueName);
        Assert.Equal(RegistrySearchMatchKind.ValueData, dataResult?.MatchKind);
        Assert.Equal("Other", dataResult?.ValueName);
    }

    [Fact]
    public void ReadPermissionsReturnsOwnerAndRules()
    {
        var permissions = _browser.ReadPermissions(_testRoot);

        Assert.False(string.IsNullOrWhiteSpace(permissions.Owner));
        Assert.NotEmpty(permissions.Rules);
    }

    public void Dispose()
    {
        try
        {
            _browser.DeleteSubKeyTree(_testRoot);
        }
        catch
        {
            // Test cleanup must not hide the original test result.
        }
    }
}
