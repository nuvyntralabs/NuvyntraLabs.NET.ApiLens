namespace NuvyntraLabs.NET.ApiLens.AspNetCore;

internal static class ReportJson
{
    public static object Brief(RequestReport report) => new
    {
        report.Id,
        report.Method,
        report.Route,
        report.StatusCode,
        durationMs = report.Duration.TotalMilliseconds,
        report.Explanation.IsSlow,
        report.Explanation.PrimarySource
    };

    public static object Detail(RequestReport report) => new
    {
        report.Id,
        report.Method,
        report.Route,
        report.StatusCode,
        report.TraceId,
        report.ExceptionType,
        durationMs = report.Duration.TotalMilliseconds,
        report.Explanation.IsSlow,
        report.Explanation.PrimarySource,
        concurrentMs = report.Explanation.Concurrent.TotalMilliseconds,
        statements = report.Explanation.Statements,
        contributors = report.Explanation.Contributors.Select(contributor => new
        {
            contributor.Name,
            kind = contributor.Kind?.ToString(),
            durationMs = contributor.Duration.TotalMilliseconds,
            percent = contributor.Percent
        }),
        operations = report.Operations.Select(operation => new
        {
            kind = operation.Kind.ToString(),
            operation.Name,
            startMs = operation.Start.TotalMilliseconds,
            durationMs = operation.Duration.TotalMilliseconds,
            operation.Detail,
            operation.StatusCode,
            operation.ParameterCount
        }),
        nPlusOne = report.Explanation.NPlusOne.Select(finding => new
        {
            finding.CommandText,
            finding.Count,
            finding.EstimatedUnnecessary
        })
    };
}
