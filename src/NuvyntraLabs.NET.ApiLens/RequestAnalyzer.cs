namespace NuvyntraLabs.NET.ApiLens;

public static class RequestAnalyzer
{
    public static RequestReport Analyze(
        IReadOnlyList<OperationInterval> operations,
        string method,
        string route,
        int statusCode,
        string traceId,
        string? exceptionType,
        TimeSpan duration,
        ApiLensOptions options,
        ApiLensCaptureMode capture)
    {
        ArgumentNullException.ThrowIfNull(operations);
        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(route);
        ArgumentNullException.ThrowIfNull(traceId);
        ArgumentNullException.ThrowIfNull(options);
        if (duration < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(duration));

        var includeText = capture == ApiLensCaptureMode.Development;
        var findings = NPlusOneDetector.Detect(operations, options.NPlusOneRepeatThreshold, includeText);
        var contributors = TimelineAttributor.Attribute(duration, operations);
        var concurrent = TimelineAttributor.Concurrent(duration, operations);
        var explanation = ExplanationWriter.Write(duration, options.SlowRequestThreshold, contributors, findings, concurrent);

        IReadOnlyList<OperationInterval> stored = operations;
        if (!includeText)
        {
            stored = operations
                .Select(operation => operation with { Detail = null })
                .ToArray();
        }

        return new RequestReport(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            method,
            route,
            statusCode,
            traceId,
            exceptionType,
            duration,
            stored,
            explanation);
    }
}
