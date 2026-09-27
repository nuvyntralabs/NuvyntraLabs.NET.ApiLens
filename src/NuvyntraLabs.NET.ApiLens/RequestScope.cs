using System.Diagnostics;

namespace NuvyntraLabs.NET.ApiLens;

public sealed class RequestScope
{
    private static readonly AsyncLocal<RequestScope?> Ambient = new();
    private readonly long _startTimestamp = Stopwatch.GetTimestamp();
    private readonly DateTimeOffset _startedAt = DateTimeOffset.UtcNow;
    private readonly List<OperationInterval> _operations = [];

    public static RequestScope? Current => Ambient.Value;

    public DateTimeOffset StartedAt => _startedAt;

    public static RequestScope Begin()
    {
        var scope = new RequestScope();
        Ambient.Value = scope;
        return scope;
    }

    public void Release()
    {
        if (ReferenceEquals(Ambient.Value, this))
            Ambient.Value = null;
    }

    public TimeSpan Elapsed => Stopwatch.GetElapsedTime(_startTimestamp);

    public void RecordCompleted(
        OperationKind kind,
        string name,
        TimeSpan duration,
        string? detail,
        int? statusCode,
        int? parameterCount)
    {
        var start = Elapsed - duration;
        if (start < TimeSpan.Zero)
            start = TimeSpan.Zero;

        Add(kind, name, start, duration, detail, statusCode, parameterCount);
    }

    public void RecordFromUtc(
        OperationKind kind,
        string name,
        DateTimeOffset startedUtc,
        TimeSpan duration,
        string? detail,
        int? statusCode,
        int? parameterCount)
    {
        var start = startedUtc - _startedAt;
        if (start < TimeSpan.Zero)
            start = TimeSpan.Zero;

        Add(kind, name, start, duration, detail, statusCode, parameterCount);
    }

    public IReadOnlyList<OperationInterval> Snapshot()
    {
        lock (_operations)
            return _operations.ToArray();
    }

    private void Add(
        OperationKind kind,
        string name,
        TimeSpan start,
        TimeSpan duration,
        string? detail,
        int? statusCode,
        int? parameterCount)
    {
        lock (_operations)
        {
            _operations.Add(new OperationInterval(kind, name, start, duration, detail, statusCode, parameterCount));
        }
    }
}
