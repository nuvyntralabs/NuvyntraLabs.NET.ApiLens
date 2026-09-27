namespace NuvyntraLabs.NET.ApiLens;

/// <summary>
/// One measured operation inside a request. <see cref="Duration"/> is the operation's own clock time.
/// Overlapping operations are not added together in the explain view; see <see cref="RequestExplanation"/>.
/// </summary>
public sealed record OperationInterval(
    OperationKind Kind,
    string Name,
    TimeSpan Start,
    TimeSpan Duration,
    string? Detail,
    int? StatusCode,
    int? ParameterCount);
