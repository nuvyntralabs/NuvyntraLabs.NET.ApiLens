using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace NuvyntraLabs.NET.ApiLens.AspNetCore;

internal sealed class ApiLensMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context,
        IOptions<ApiLensOptions> options,
        IApiLensStore store,
        IHostEnvironment environment)
    {
        if (context.Request.Path.StartsWithSegments(ApiLensApplicationBuilderExtensions.DashboardPath))
        {
            if (options.Value.EnableDashboard && environment.IsDevelopment())
                await ApiLensDashboard.WriteAsync(context, store);
            else
                context.Response.StatusCode = StatusCodes.Status404NotFound;

            return;
        }

        var scope = RequestScope.Begin();
        Exception? error = null;
        var started = Stopwatch.GetTimestamp();
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            error = exception;
            throw;
        }
        finally
        {
            try
            {
                error ??= context.Features.Get<IExceptionHandlerFeature>()?.Error;
                var duration = Stopwatch.GetElapsedTime(started);
                var status = context.Response.StatusCode;
                if (error is not null && status < StatusCodes.Status400BadRequest)
                    status = StatusCodes.Status500InternalServerError;

                var report = RequestAnalyzer.Analyze(
                    scope.Snapshot(),
                    context.Request.Method,
                    RouteOf(context),
                    status,
                    TraceIdOf(context),
                    error?.GetType().Name,
                    duration,
                    options.Value,
                    options.Value.ResolveCapture(environment.IsDevelopment()));
                store.Add(report);
            }
            finally
            {
                scope.Release();
            }
        }
    }

    private static string RouteOf(HttpContext context)
    {
        if (context.GetEndpoint() is RouteEndpoint endpoint && !string.IsNullOrEmpty(endpoint.RoutePattern.RawText))
        {
            var raw = endpoint.RoutePattern.RawText;
            return raw.StartsWith('/') ? raw : "/" + raw;
        }

        return context.Request.Path.Value ?? "/";
    }

    private static string TraceIdOf(HttpContext context)
    {
        var traceId = Activity.Current?.TraceId.ToHexString();
        if (!string.IsNullOrEmpty(traceId) && traceId != default(ActivityTraceId).ToHexString())
            return traceId;

        return context.TraceIdentifier;
    }
}
