using System.Diagnostics;

namespace NuvyntraLabs.NET.ApiLens.Http;

internal static class ApiLensHttpMarkers
{
    public static readonly HttpRequestOptionsKey<bool> RecordedByHandler = new("NuvyntraLabs.ApiLens.Http");
}

internal sealed class ApiLensHttpHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Options.Set(ApiLensHttpMarkers.RecordedByHandler, true);
        var started = Stopwatch.GetTimestamp();
        HttpResponseMessage? response = null;
        try
        {
            response = await base.SendAsync(request, cancellationToken);
            return response;
        }
        finally
        {
            var scope = RequestScope.Current;
            if (scope is not null)
            {
                var host = request.RequestUri?.Host;
                scope.RecordCompleted(
                    OperationKind.Http,
                    string.IsNullOrWhiteSpace(host) ? "HTTP" : host,
                    Stopwatch.GetElapsedTime(started),
                    detail: null,
                    response is null ? null : (int)response.StatusCode,
                    parameterCount: null);
            }
        }
    }
}
