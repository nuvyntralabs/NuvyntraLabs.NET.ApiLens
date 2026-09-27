using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;

namespace NuvyntraLabs.NET.ApiLens.Http;

public static class ApiLensHttpExtensions
{
    public static IServiceCollection AddApiLensHttp(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, HttpDiagnosticListener>());
        services.TryAddTransient<ApiLensHttpHandler>();
        services.ConfigureAll<HttpClientFactoryOptions>(options =>
        {
            options.HttpMessageHandlerBuilderActions.Add(builder =>
            {
                builder.AdditionalHandlers.Add(builder.Services.GetRequiredService<ApiLensHttpHandler>());
            });
        });
        return services;
    }
}

internal sealed class HttpDiagnosticListener : IHostedService, IDisposable
{
    private readonly ConcurrentDictionary<HttpRequestMessage, long> _started = new();
    private readonly List<IDisposable> _subscriptions = [];
    private IDisposable? _listeners;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _listeners = DiagnosticListener.AllListeners.Subscribe(new ListenerObserver(this));
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Dispose();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _listeners?.Dispose();
        _listeners = null;
        foreach (var subscription in _subscriptions)
            subscription.Dispose();
        _subscriptions.Clear();
    }

    private void OnEvent(KeyValuePair<string, object?> value)
    {
        if (value.Value is null)
            return;

        var request = Read<HttpRequestMessage>(value.Value, "Request");
        if (request is null)
            return;

        if (value.Key.EndsWith(".Start", StringComparison.Ordinal))
        {
            _started[request] = Stopwatch.GetTimestamp();
            return;
        }

        if (!value.Key.EndsWith(".Stop", StringComparison.Ordinal))
            return;

        _started.TryRemove(request, out var timestamp);
        if (request.Options.TryGetValue(ApiLensHttpMarkers.RecordedByHandler, out var recorded) && recorded)
            return;

        var scope = RequestScope.Current;
        if (scope is null)
            return;

        var duration = timestamp == 0 ? TimeSpan.Zero : Stopwatch.GetElapsedTime(timestamp);

        var host = request.RequestUri?.Host;
        if (string.IsNullOrWhiteSpace(host))
            host = "HTTP";

        int? status = null;
        if (Read<HttpResponseMessage>(value.Value, "Response") is { } response)
            status = (int)response.StatusCode;

        scope.RecordCompleted(OperationKind.Http, host, duration, detail: null, status, parameterCount: null);
    }

    private static T? Read<T>(object payload, string propertyName) where T : class
    {
        var property = payload.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        return property?.GetValue(payload) as T;
    }

    private sealed class ListenerObserver(HttpDiagnosticListener owner) : IObserver<DiagnosticListener>
    {
        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(DiagnosticListener value)
        {
            if (!string.Equals(value.Name, "HttpHandlerDiagnosticListener", StringComparison.Ordinal))
                return;

            owner._subscriptions.Add(value.Subscribe(
                new EventObserver(owner),
                static (string name, object? _, object? _) =>
                    name is "System.Net.Http.HttpRequestOut.Start" or "System.Net.Http.HttpRequestOut.Stop"));
        }
    }

    private sealed class EventObserver(HttpDiagnosticListener owner) : IObserver<KeyValuePair<string, object?>>
    {
        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(KeyValuePair<string, object?> value) => owner.OnEvent(value);
    }
}
