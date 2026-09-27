namespace NuvyntraLabs.NET.ApiLens;

/// <summary>
/// Wall-clock share of a request. Overlapping operations are counted once, on the longer operation.
/// </summary>
public sealed record Contributor(string Name, OperationKind? Kind, TimeSpan Duration, double Percent);
