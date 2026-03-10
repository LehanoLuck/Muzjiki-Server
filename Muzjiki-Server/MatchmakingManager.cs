using System.Collections.Concurrent;

namespace Muzjiki_Server;

public class MatchmakingManager
{
    private readonly ConcurrentQueue<Guid> _quickMatchQueue = new();
    private readonly HashSet<Guid> _queuedPlayers = new();
    private readonly object _sync = new();

    public bool EnqueueQuickMatch(Guid connectionId)
    {
        lock (_sync)
        {
            if (!_queuedPlayers.Add(connectionId))
            {
                return false;
            }

            _quickMatchQueue.Enqueue(connectionId);
            return true;
        }
    }

    public void Remove(Guid connectionId)
    {
        lock (_sync)
        {
            _queuedPlayers.Remove(connectionId);
        }
    }

    public GameSession? TryCreateMatch(ConnectionManager connectionManager)
    {
        lock (_sync)
        {
            while (_quickMatchQueue.Count >= 2)
            {
                if (!_quickMatchQueue.TryDequeue(out var player1Id) ||
                    !_quickMatchQueue.TryDequeue(out var player2Id))
                {
                    return null;
                }

                if (!_queuedPlayers.Remove(player1Id) || !_queuedPlayers.Remove(player2Id))
                {
                    continue;
                }

                var player1Connected = connectionManager.TryGet(player1Id, out var p1Socket)
                    && p1Socket?.State == System.Net.WebSockets.WebSocketState.Open;
                var player2Connected = connectionManager.TryGet(player2Id, out var p2Socket)
                    && p2Socket?.State == System.Net.WebSockets.WebSocketState.Open;

                if (!player1Connected || !player2Connected)
                {
                    continue;
                }

                return new GameSession
                {
                    Player1ConnectionId = player1Id,
                    Player2ConnectionId = player2Id
                };
            }

            return null;
        }
    }
}
