using Registry.Core;

namespace Registry.Tests;

public sealed class RegistryPathTests
{
    [Theory]
    [InlineData(@"HKLM\Software", RegistryHiveId.LocalMachine, "Software", @"HKEY_LOCAL_MACHINE\Software")]
    [InlineData(@"HKEY_CURRENT_USER/Software/Microsoft", RegistryHiveId.CurrentUser, @"Software\Microsoft", @"HKEY_CURRENT_USER\Software\Microsoft")]
    [InlineData(@"HKCR", RegistryHiveId.ClassesRoot, "", "HKEY_CLASSES_ROOT")]
    [InlineData(@"Computer\HKEY_CURRENT_USER\Software", RegistryHiveId.CurrentUser, "Software", @"HKEY_CURRENT_USER\Software")]
    [InlineData(@"HKCU:\Software", RegistryHiveId.CurrentUser, "Software", @"HKEY_CURRENT_USER\Software")]
    [InlineData(@"Registry::HKEY_LOCAL_MACHINE\Software", RegistryHiveId.LocalMachine, "Software", @"HKEY_LOCAL_MACHINE\Software")]
    public void ParseAcceptsCanonicalNamesAndAliases(string input, RegistryHiveId hive, string subKey, string display)
    {
        var path = RegistryPath.Parse(input);

        Assert.Equal(hive, path.Hive);
        Assert.Equal(subKey, path.SubKey);
        Assert.Equal(display, path.ToString());
    }

    [Fact]
    public void ParseRejectsUnknownHive()
    {
        Assert.Throws<FormatException>(() => RegistryPath.Parse(@"NOPE\Software"));
    }

    [Theory]
    [InlineData(@"HKCU:\Software", @"HKEY_CURRENT_USER\Software")]
    [InlineData(@"Computer\HKLM\Software", @"HKEY_LOCAL_MACHINE\Software")]
    public void NormalizeForSuggestionUsesCanonicalPath(string input, string expected)
    {
        Assert.Equal(expected, RegistryPath.NormalizeForSuggestion(input));
    }
}
