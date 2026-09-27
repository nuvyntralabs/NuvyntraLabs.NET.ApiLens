namespace NuvyntraLabs.NET.ApiLens;

public static class TimelineAttributor
{
    public static IReadOnlyList<Contributor> Attribute(TimeSpan total, IReadOnlyList<OperationInterval> operations)
    {
        ArgumentNullException.ThrowIfNull(operations);
        if (total < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(total));

        var totalTicks = total.Ticks;
        if (totalTicks == 0)
            return [new Contributor("Application", null, TimeSpan.Zero, 0)];

        var clamped = new List<Clamped>();
        for (var i = 0; i < operations.Count; i++)
        {
            var operation = operations[i];
            var start = operation.Start.Ticks;
            var end = start + Math.Max(0, operation.Duration.Ticks);
            if (start < 0)
                start = 0;
            if (end > totalTicks)
                end = totalTicks;
            if (end <= start)
                continue;

            clamped.Add(new Clamped(i, operation, start, end));
        }

        var points = new List<Point>(clamped.Count * 2);
        for (var i = 0; i < clamped.Count; i++)
        {
            points.Add(new Point(clamped[i].Start, false, i));
            points.Add(new Point(clamped[i].End, true, i));
        }

        points.Sort(static (left, right) =>
        {
            var compared = left.Tick.CompareTo(right.Tick);
            if (compared != 0)
                return compared;
            if (left.IsEnd != right.IsEnd)
                return left.IsEnd ? -1 : 1;
            return left.Index.CompareTo(right.Index);
        });

        var attributed = new long[clamped.Count];
        long application = 0;
        var active = new List<int>();
        long cursor = 0;

        void Consume(long next)
        {
            var span = next - cursor;
            if (span <= 0)
                return;

            if (active.Count == 0)
                application += span;
            else
                attributed[Pick(active, clamped)] += span;

            cursor = next;
        }

        foreach (var point in points)
        {
            var tick = point.Tick < 0 ? 0 : point.Tick;
            if (tick > totalTicks)
                tick = totalTicks;
            Consume(tick);
            if (point.IsEnd)
                active.Remove(point.Index);
            else
                active.Add(point.Index);
        }

        Consume(totalTicks);

        var database = attributed
            .Select((ticks, index) => (ticks, clamped[index].Operation))
            .Where(item => item.Operation.Kind == OperationKind.Database)
            .Sum(item => item.ticks);

        var http = new Dictionary<string, long>(StringComparer.Ordinal);
        for (var i = 0; i < clamped.Count; i++)
        {
            if (clamped[i].Operation.Kind != OperationKind.Http || attributed[i] == 0)
                continue;

            var name = string.IsNullOrWhiteSpace(clamped[i].Operation.Name) ? "HTTP" : clamped[i].Operation.Name;
            http.TryGetValue(name, out var current);
            http[name] = current + attributed[i];
        }

        var contributors = new List<Contributor>();
        if (database > 0)
            contributors.Add(New("Database", OperationKind.Database, database, totalTicks));

        foreach (var pair in http.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            contributors.Add(New(pair.Key, OperationKind.Http, pair.Value, totalTicks));

        contributors.Add(New("Application", null, application, totalTicks));
        return contributors;
    }

    public static TimeSpan Concurrent(TimeSpan total, IReadOnlyList<OperationInterval> operations)
    {
        ArgumentNullException.ThrowIfNull(operations);
        if (total <= TimeSpan.Zero)
            return TimeSpan.Zero;

        var totalTicks = total.Ticks;
        var points = new List<Point>();
        for (var i = 0; i < operations.Count; i++)
        {
            var start = Math.Max(0, operations[i].Start.Ticks);
            var end = Math.Min(totalTicks, start + Math.Max(0, operations[i].Duration.Ticks));
            if (end <= start)
                continue;
            points.Add(new Point(start, false, i));
            points.Add(new Point(end, true, i));
        }

        points.Sort(static (left, right) =>
        {
            var compared = left.Tick.CompareTo(right.Tick);
            if (compared != 0)
                return compared;
            if (left.IsEnd != right.IsEnd)
                return left.IsEnd ? -1 : 1;
            return left.Index.CompareTo(right.Index);
        });

        long concurrent = 0;
        var depth = 0;
        long cursor = 0;
        foreach (var point in points)
        {
            if (depth > 1 && point.Tick > cursor)
                concurrent += point.Tick - cursor;
            cursor = point.Tick;
            depth += point.IsEnd ? -1 : 1;
        }

        return TimeSpan.FromTicks(concurrent);
    }

    private static int Pick(List<int> active, List<Clamped> clamped)
    {
        var winner = active[0];
        for (var i = 1; i < active.Count; i++)
        {
            var candidate = active[i];
            var candidateDuration = clamped[candidate].Operation.Duration.Ticks;
            var winnerDuration = clamped[winner].Operation.Duration.Ticks;
            if (candidateDuration > winnerDuration
                || (candidateDuration == winnerDuration && clamped[candidate].Start < clamped[winner].Start)
                || (candidateDuration == winnerDuration && clamped[candidate].Start == clamped[winner].Start && candidate < winner))
            {
                winner = candidate;
            }
        }

        return winner;
    }

    private static Contributor New(string name, OperationKind? kind, long ticks, long totalTicks)
        => new(name, kind, TimeSpan.FromTicks(ticks), ticks * 100d / totalTicks);

    private readonly record struct Point(long Tick, bool IsEnd, int Index);

    private readonly record struct Clamped(int Index, OperationInterval Operation, long Start, long End);
}
