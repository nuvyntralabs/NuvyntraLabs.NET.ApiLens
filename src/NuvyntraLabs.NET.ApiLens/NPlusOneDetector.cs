using System.Text.RegularExpressions;

namespace NuvyntraLabs.NET.ApiLens;

public static partial class NPlusOneDetector
{
    public static IReadOnlyList<NPlusOneFinding> Detect(
        IReadOnlyList<OperationInterval> operations,
        int threshold,
        bool includeCommandText)
    {
        ArgumentNullException.ThrowIfNull(operations);
        if (threshold < 2)
            threshold = 2;

        var groups = new Dictionary<string, Group>(StringComparer.Ordinal);
        foreach (var operation in operations)
        {
            if (operation.Kind != OperationKind.Database || string.IsNullOrWhiteSpace(operation.Detail))
                continue;

            var display = Collapse().Replace(SqlRedactor.Redact(operation.Detail), " ").Trim();
            var key = display.ToLowerInvariant();
            if (key.Length == 0)
                continue;

            if (!groups.TryGetValue(key, out var group))
            {
                group = new Group(display);
                groups[key] = group;
            }

            group.Count++;
        }

        return groups.Values
            .Where(group => group.Count >= threshold)
            .OrderByDescending(group => group.Count)
            .ThenBy(group => group.Display, StringComparer.Ordinal)
            .Select(group => new NPlusOneFinding(
                includeCommandText ? Truncate(group.Display, 160) : null,
                group.Count,
                group.Count - 1))
            .ToArray();
    }

    private static string Truncate(string value, int max)
        => value.Length <= max ? value : value[..max] + "…";

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex Collapse();

    private sealed class Group(string display)
    {
        public string Display { get; } = display;
        public int Count { get; set; }
    }
}
