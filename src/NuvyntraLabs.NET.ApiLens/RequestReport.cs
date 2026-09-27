namespace NuvyntraLabs.NET.ApiLens;

public sealed record RequestReport(
    Guid Id,
    DateTimeOffset CompletedAt,
    string Method,
    string Route,
    int StatusCode,
    string TraceId,
    string? ExceptionType,
    TimeSpan Duration,
    IReadOnlyList<OperationInterval> Operations,
    RequestExplanation Explanation);
