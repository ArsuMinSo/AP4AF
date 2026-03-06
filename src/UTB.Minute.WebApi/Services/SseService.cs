using System.Collections.Concurrent;
using System.Text.Json;

namespace UTB.Minute.WebApi.Services;

public class SseService
{
    private readonly ConcurrentDictionary<Guid, Func<string, CancellationToken, Task>> _clients = new();

    public IDisposable Subscribe(Func<string, CancellationToken, Task> handler)
    {
        var id = Guid.NewGuid();
        _clients[id] = handler;
        return new Subscription(() => _clients.TryRemove(id, out _));
    }

    public async Task BroadcastAsync(string eventType, object data, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(data);
        var message = $"event: {eventType}\ndata: {json}\n\n";

        var tasks = _clients.Values.Select(handler =>
            handler(message, cancellationToken)).ToList();

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private sealed class Subscription(Action onDispose) : IDisposable
    {
        public void Dispose() => onDispose();
    }
}
