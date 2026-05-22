using System.Globalization;
using System.Text;
using Microsoft.Win32;

namespace Registry.Core;

public static class RegImportParser
{
    public static RegImportDocument Parse(string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        var operations = new List<RegImportOperation>();
        RegistryPath? currentPath = null;

        foreach (var logicalLine in ReadLogicalLines(content))
        {
            var line = logicalLine.Trim();
            if (line.Length == 0 || line.StartsWith(';') || line.Equals("Windows Registry Editor Version 5.00", StringComparison.OrdinalIgnoreCase) || line.Equals("REGEDIT4", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (line.StartsWith("[", StringComparison.Ordinal) && line.EndsWith("]", StringComparison.Ordinal))
            {
                var section = line[1..^1];
                if (section.StartsWith("-", StringComparison.Ordinal))
                {
                    var deletedPath = RegistryPath.Parse(section[1..]);
                    operations.Add(new RegImportOperation(RegImportOperationKind.DeleteKey, deletedPath));
                    currentPath = null;
                }
                else
                {
                    currentPath = RegistryPath.Parse(section);
                    operations.Add(new RegImportOperation(RegImportOperationKind.CreateKey, currentPath));
                }

                continue;
            }

            if (currentPath is null)
            {
                throw new FormatException($"Value line appears before a key section: {line}");
            }

            operations.Add(ParseValueLine(currentPath, line));
        }

        return new RegImportDocument(operations);
    }

    private static RegImportOperation ParseValueLine(RegistryPath path, string line)
    {
        var equalsIndex = FindUnquotedEquals(line);
        if (equalsIndex <= 0)
        {
            throw new FormatException($"Invalid registry value line: {line}");
        }

        var nameToken = line[..equalsIndex].Trim();
        var dataToken = line[(equalsIndex + 1)..].Trim();
        var name = nameToken == "@" ? string.Empty : Unquote(nameToken);

        if (dataToken == "-")
        {
            return new RegImportOperation(RegImportOperationKind.DeleteValue, path, name);
        }

        if (dataToken.StartsWith("dword:", StringComparison.OrdinalIgnoreCase))
        {
            var value = int.Parse(dataToken["dword:".Length..], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return new RegImportOperation(RegImportOperationKind.SetValue, path, name, RegistryValueKind.DWord, value);
        }

        if (dataToken.StartsWith("hex(b):", StringComparison.OrdinalIgnoreCase))
        {
            var bytes = ParseHexBytes(dataToken["hex(b):".Length..]);
            var value = BitConverter.ToUInt64(Pad(bytes, sizeof(ulong)), 0);
            return new RegImportOperation(RegImportOperationKind.SetValue, path, name, RegistryValueKind.QWord, value);
        }

        if (dataToken.StartsWith("hex(2):", StringComparison.OrdinalIgnoreCase))
        {
            var value = DecodeUtf16(ParseHexBytes(dataToken["hex(2):".Length..]));
            return new RegImportOperation(RegImportOperationKind.SetValue, path, name, RegistryValueKind.ExpandString, value);
        }

        if (dataToken.StartsWith("hex(7):", StringComparison.OrdinalIgnoreCase))
        {
            var value = DecodeUtf16(ParseHexBytes(dataToken["hex(7):".Length..]));
            var values = value.TrimEnd('\0').Split('\0', StringSplitOptions.None);
            return new RegImportOperation(RegImportOperationKind.SetValue, path, name, RegistryValueKind.MultiString, values);
        }

        if (dataToken.StartsWith("hex:", StringComparison.OrdinalIgnoreCase))
        {
            return new RegImportOperation(RegImportOperationKind.SetValue, path, name, RegistryValueKind.Binary, ParseHexBytes(dataToken["hex:".Length..]));
        }

        return new RegImportOperation(RegImportOperationKind.SetValue, path, name, RegistryValueKind.String, Unquote(dataToken));
    }

    private static IEnumerable<string> ReadLogicalLines(string content)
    {
        var builder = new StringBuilder();
        foreach (var raw in content.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n'))
        {
            var line = raw.TrimEnd();
            if (line.EndsWith('\\'))
            {
                builder.Append(line[..^1]);
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append(line);
                yield return builder.ToString();
                builder.Clear();
            }
            else
            {
                yield return line;
            }
        }

        if (builder.Length > 0)
        {
            yield return builder.ToString();
        }
    }

    private static int FindUnquotedEquals(string line)
    {
        var quoted = false;
        for (var i = 0; i < line.Length; i++)
        {
            if (line[i] == '"' && (i == 0 || line[i - 1] != '\\'))
            {
                quoted = !quoted;
            }
            else if (line[i] == '=' && !quoted)
            {
                return i;
            }
        }

        return -1;
    }

    private static string Unquote(string token)
    {
        var trimmed = token.Trim();
        if (trimmed.Length >= 2 && trimmed[0] == '"' && trimmed[^1] == '"')
        {
            trimmed = trimmed[1..^1];
        }

        return trimmed.Replace("\\\"", "\"", StringComparison.Ordinal).Replace(@"\\", @"\", StringComparison.Ordinal);
    }

    private static byte[] ParseHexBytes(string token)
    {
        return token.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => byte.Parse(part, NumberStyles.HexNumber, CultureInfo.InvariantCulture))
            .ToArray();
    }

    private static string DecodeUtf16(byte[] bytes)
    {
        return Encoding.Unicode.GetString(bytes).TrimEnd('\0');
    }

    private static byte[] Pad(byte[] bytes, int length)
    {
        if (bytes.Length >= length)
        {
            return bytes;
        }

        var padded = new byte[length];
        Array.Copy(bytes, padded, bytes.Length);
        return padded;
    }
}
