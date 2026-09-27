using System.Diagnostics;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Hosting;

namespace NuvyntraLabs.NET.ApiLens.EntityFrameworkCore;

internal sealed class EfCommandListener : IHostedService, IDisposable
{
    private readonly List<IDisposable> _subscriptions = [];
    private IDisposable? _listeners;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _listeners = DiagnosticListener.AllListeners.Subscribe(new ListenerObserver(_subscriptions));
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

    private sealed class ListenerObserver(List<IDisposable> subscriptions) : IObserver<DiagnosticListener>
    {
        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(DiagnosticListener value)
        {
            if (!string.Equals(value.Name, "Microsoft.EntityFrameworkCore", StringComparison.Ordinal))
                return;

            subscriptions.Add(value.Subscribe(
                new CommandObserver(),
                (string name, object? _, object? _) =>
                    name is "Microsoft.EntityFrameworkCore.Database.Command.CommandExecuted"
                        or "Microsoft.EntityFrameworkCore.Database.Command.CommandError"));
        }
    }

    private sealed class CommandObserver : IObserver<KeyValuePair<string, object?>>
    {
        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(KeyValuePair<string, object?> value)
        {
            if (value.Value is not CommandEndEventData data)
                return;

            var scope = RequestScope.Current;
            if (scope is null)
                return;

            var text = data.Command.CommandText;
            string? redacted = string.IsNullOrEmpty(text) ? null : SqlRedactor.Redact(text);
            scope.RecordCompleted(
                OperationKind.Database,
                "SQL",
                data.Duration,
                redacted,
                statusCode: null,
                parameterCount: data.Command.Parameters.Count);
        }
    }
}
