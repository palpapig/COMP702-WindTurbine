using System.Globalization;
using Simulator.Server.Models;

namespace Simulator.Server.Services;

public sealed class SignalMapper
{
    private readonly Dictionary<string, SignalDefinition> _signals = new(StringComparer.OrdinalIgnoreCase);

    public SignalMapper(string mappingCsvPath)
    {
        using var reader = new StreamReader(mappingCsvPath);
        var header = reader.ReadLine();
        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(line)) continue;

            var parts = ParseCsv(line);
            if (parts.Count < 4) continue;

            var key = parts[1].Trim();
            if (string.IsNullOrWhiteSpace(key)) continue;

            var def = new SignalDefinition(
                Key: key,
                GreenbyteTitle: parts[1].Trim(),
                ManufacturerTitle: parts[2].Trim(),
                Unit: parts[3].Trim(),
                NodeName: NormalizeNodeName(key));

            _signals[key] = def;
        }
    }

    public IReadOnlyCollection<SignalDefinition> GetAll() => _signals.Values;

    public static string NormalizeNodeName(string raw)
    {
        var chars = raw.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray();
        var normalized = new string(chars);
        while (normalized.Contains("__", StringComparison.Ordinal))
        {
            normalized = normalized.Replace("__", "_", StringComparison.Ordinal);
        }
        return normalized.Trim('_');
    }

    private static List<string> ParseCsv(string line)
    {
        var result = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        foreach (var ch in line)
        {
            if (ch == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (ch == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(ch);
        }

        result.Add(current.ToString());
        return result;
    }
}
