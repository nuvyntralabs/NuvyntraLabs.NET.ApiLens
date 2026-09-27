namespace NuvyntraLabs.NET.ApiLens;

public sealed record RequestExplanation(
    bool IsSlow,
    string PrimarySource,
    TimeSpan Concurrent,
    IReadOnlyList<Contributor> Contributors,
    IReadOnlyList<NPlusOneFinding> NPlusOne,
    IReadOnlyList<string> Statements);
