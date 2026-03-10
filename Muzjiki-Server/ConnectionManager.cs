using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace Muzjiki_Server;

public class ConnectionManager
{
    private readonly ConcurrentDictionary<Guid, WebSocket> _connections = new();

    public IReadOnlyDictionary<Guid, WebSocket> Connections => _connections;

    public Guid Add(WebSocket socket)
    {
        var connectionId = Guid.NewGuid();
        _connections[connectionId] = socket;
        return connectionId;
    }

    public bool Remove(Guid connectionId)
    {
        return _connections.TryRemove(connectionId, out _);
    }

    public bool TryGet(Guid connectionId, out WebSocket? socket)
    {
        return _connections.TryGetValue(connectionId, out socket);
    }
}
