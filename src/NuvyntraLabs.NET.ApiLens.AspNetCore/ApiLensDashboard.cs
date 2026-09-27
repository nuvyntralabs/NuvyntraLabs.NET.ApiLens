using System.Text.Json;
using Microsoft.AspNetCore.Http;
using NuvyntraLabs.NET.ApiLens.UI;

namespace NuvyntraLabs.NET.ApiLens.AspNetCore;

internal static class ApiLensDashboard
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static async Task WriteAsync(HttpContext context, IApiLensStore store)
    {
        context.Response.Headers.CacheControl = "no-store";
        context.Response.Headers.XContentTypeOptions = "nosniff";

        var rest = Remainder(context.Request.Path);
        if (rest is "/" or "")
        {
            context.Response.ContentType = "text/html; charset=utf-8";
            await using var html = ApiLensDashboardAsset.Open();
            await html.CopyToAsync(context.Response.Body, context.RequestAborted);
            return;
        }

        if (rest == "/api/summary")
        {
            await WriteJsonAsync(context, Summary(store));
            return;
        }

        if (rest == "/api/requests")
        {
            var list = store.List().Select(ReportJson.Brief).ToArray();
            await WriteJsonAsync(context, list);
            return;
        }

        const string prefix = "/api/requests/";
        if (rest.StartsWith(prefix, StringComparison.Ordinal) && Guid.TryParse(rest[prefix.Length..], out var id))
        {
            var report = store.Find(id);
            if (report is null)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            await WriteJsonAsync(context, ReportJson.Detail(report));
            return;
        }

        context.Response.StatusCode = StatusCodes.Status404NotFound;
    }

    private static object Summary(IApiLensStore store)
    {
        var summary = ApiLensSummaryBuilder.Build(store.List());
        return new
        {
            summary.RequestCount,
            summary.ErrorCount,
            averageDurationMs = summary.AverageDuration.TotalMilliseconds,
            slowest = summary.Slowest.Select(endpoint => new
            {
                endpoint.Method,
                endpoint.Route,
                durationMs = endpoint.Duration.TotalMilliseconds,
                endpoint.RequestId
            }),
            dependencies = summary.Dependencies.Select(contributor => new
            {
                contributor.Name,
                kind = contributor.Kind?.ToString(),
                durationMs = contributor.Duration.TotalMilliseconds,
                percent = contributor.Percent
            })
        };
    }

    private static async Task WriteJsonAsync(HttpContext context, object value)
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        await JsonSerializer.SerializeAsync(context.Response.Body, value, Json, context.RequestAborted);
    }

    private static string Remainder(PathString path)
    {
        if (!path.StartsWithSegments(ApiLensApplicationBuilderExtensions.DashboardPath, out var rest))
            return "/";

        return rest.HasValue ? rest.Value! : "/";
    }
}
