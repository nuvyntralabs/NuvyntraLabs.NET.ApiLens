namespace NuvyntraLabs.NET.ApiLens;

public interface IApiLensStore
{
    void Add(RequestReport report);

    IReadOnlyList<RequestReport> List();

    RequestReport? Find(Guid id);
}
