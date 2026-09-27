using System.Globalization;

namespace NuvyntraLabs.NET.ApiLens;

public static class ExplanationWriter
{
    public static RequestExplanation Write(
        TimeSpan duration,
        TimeSpan slowThreshold,
        IReadOnlyList<Contributor> contributors,
        IReadOnlyList<NPlusOneFinding> nPlusOne,
        TimeSpan concurrent)
    {
        ArgumentNullException.ThrowIfNull(contributors);
        ArgumentNullException.ThrowIfNull(nPlusOne);

        var ordered = contributors
            .OrderByDescending(contributor => contributor.Duration)
            .ThenBy(contributor => contributor.Name, StringComparer.Ordinal)
            .ToArray();

        var primary = ordered.FirstOrDefault() ?? new Contributor("Application", null, duration, 100);
        var isSlow = duration > slowThreshold;
        var statements = new List<string>();
        if (isSlow)
        {
            statements.Add(
                $"Duration {Format(duration)} exceeds the slow-request threshold of {Format(slowThreshold)}.");
        }

        statements.Add($"Primary latency source: {primary.Name} ({primary.Percent.ToString("0.#", CultureInfo.InvariantCulture)}% of this request).");
        statements.Add($"{primary.Name} accounts for approximately {primary.Percent.ToString("0", CultureInfo.InvariantCulture)}% of this request's observed duration.");

        if (concurrent > TimeSpan.Zero)
            statements.Add($"{Format(concurrent)} ran concurrently and is counted once.");

        foreach (var finding in nPlusOne)
        {
            statements.Add(finding.CommandText is null
                ? $"Possible N+1: one SQL shape repeated {finding.Count.ToString(CultureInfo.InvariantCulture)} times. Estimated unnecessary queries: {finding.EstimatedUnnecessary.ToString(CultureInfo.InvariantCulture)}."
                : $"Possible N+1: {finding.CommandText} repeated {finding.Count.ToString(CultureInfo.InvariantCulture)} times. Estimated unnecessary queries: {finding.EstimatedUnnecessary.ToString(CultureInfo.InvariantCulture)}.");
        }

        return new RequestExplanation(isSlow, primary.Name, concurrent, ordered, nPlusOne, statements);
    }

    public static string Format(TimeSpan duration)
    {
        var milliseconds = duration.TotalMilliseconds;
        if (milliseconds >= 1000)
            return (milliseconds / 1000d).ToString("0.##", CultureInfo.InvariantCulture) + " s";

        return milliseconds.ToString("0", CultureInfo.InvariantCulture) + " ms";
    }
}
