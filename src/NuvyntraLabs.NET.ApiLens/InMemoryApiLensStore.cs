namespace NuvyntraLabs.NET.ApiLens;

public sealed class InMemoryApiLensStore : IApiLensStore
{
    private readonly int _capacity;
    private readonly object _gate = new();
    private readonly LinkedList<RequestReport> _items = new();

    public InMemoryApiLensStore(int capacity)
    {
        _capacity = Math.Max(1, capacity);
    }

    public void Add(RequestReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        lock (_gate)
        {
            _items.AddFirst(report);
            while (_items.Count > _capacity)
                _items.RemoveLast();
        }
    }

    public IReadOnlyList<RequestReport> List()
    {
        lock (_gate)
            return _items.ToArray();
    }

    public RequestReport? Find(Guid id)
    {
        lock (_gate)
            return _items.FirstOrDefault(report => report.Id == id);
    }
}
