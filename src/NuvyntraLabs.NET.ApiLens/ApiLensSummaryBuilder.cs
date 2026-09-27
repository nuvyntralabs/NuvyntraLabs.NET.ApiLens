namespace NuvyntraLabs.NET.ApiLens;

public sealed record SlowEndpoint(string Method, string Route, TimeSpan Duration, Guid RequestId);

public sealed record ApiLensSummary(
    int RequestCount,
    int ErrorCount,
    TimeSpan AverageDuration,
    IReadOnlyList<SlowEndpoint> Slowest,
    IReadOnlyList<Contributor> Dependencies);

public static class ApiLensSummaryBuilder
{
    public static ApiLensSummary Build(IReadOnlyList<RequestReport> reports)
    {
        ArgumentNullException.ThrowIfNull(reports);
        if (reports.Count == 0)
        {
            return new ApiLensSummary(0, 0, TimeSpan.Zero, [], []);
        }

        var errors = reports.Count(report => report.StatusCode >= 500 || report.ExceptionType is not null);
        var average = TimeSpan.FromTicks((long)reports.Average(report => report.Duration.Ticks));
        var slowest = reports
            .GroupBy(report => report.Method + " " + report.Route, StringComparer.Ordinal)
            .Select(group =>
            {
                var peak = group.OrderByDescending(report => report.Duration).First();
                return new SlowEndpoint(peak.Method, peak.Route, peak.Duration, peak.Id);
            })
            .OrderByDescending(endpoint => endpoint.Duration)
            .Take(8)
            .ToArray();

        long total = 0;
        var buckets = new Dictionary<string, (OperationKind? Kind, long Ticks)>(StringComparer.Ordinal);
        foreach (var report in reports)
        {
            foreach (var contributor in report.Explanation.Contributors)
            {
                total += contributor.Duration.Ticks;
                buckets.TryGetValue(contributor.Name, out var current);
                buckets[contributor.Name] = (contributor.Kind, current.Ticks + contributor.Duration.Ticks);
            }
        }

        var dependencies = buckets
            .Select(pair => new Contributor(
                pair.Key,
                pair.Value.Kind,
                TimeSpan.FromTicks(pair.Value.Ticks),
                total == 0 ? 0 : pair.Value.Ticks * 100d / total))
            .OrderByDescending(contributor => contributor.Duration)
            .ThenBy(contributor => contributor.Name, StringComparer.Ordinal)
            .ToArray();

        return new ApiLensSummary(reports.Count, errors, average, slowest, dependencies);
    }
}
