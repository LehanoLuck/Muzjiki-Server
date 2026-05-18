using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Muzjiki_Server;

public class GameLoopService
{
    private readonly ConnectionManager _connectionManager;
    private readonly MatchmakingManager _matchmakingManager;
    private readonly Dictionary<Guid, GameState> _sessionStates = new();
    private readonly Dictionary<Guid, Guid> _playerToSession = new();
    private readonly object _sync = new();
    private readonly CombatEngine _combatEngine = new();

    public GameLoopService(ConnectionManager connectionManager, MatchmakingManager matchmakingManager)
    {
        _connectionManager = connectionManager;
        _matchmakingManager = matchmakingManager;
    }

    public async Task HandleEnvelopeAsync(Guid connectionId, Envelope envelope, CancellationToken cancellationToken)
    {
        switch (envelope.Type)
        {
            case EnvelopeTypes.QueueQuickMatch:
                await HandleQueueQuickMatchAsync(connectionId, cancellationToken);
                return;
            case EnvelopeTypes.DrawCard:
                await ExecuteTurnStepAsync(connectionId, EnvelopeTypes.DrawCard, cancellationToken);
                return;
            case EnvelopeTypes.PlayCard:
                await ExecuteTurnStepAsync(connectionId, EnvelopeTypes.PlayCard, cancellationToken);
                return;
            case EnvelopeTypes.EndTurn:
                await ExecuteTurnStepAsync(connectionId, EnvelopeTypes.EndTurn, cancellationToken);
                return;
            case EnvelopeTypes.Hello:
                Console.WriteLine($"Hello from: {connectionId}");
                return;
            default:
                Console.WriteLine($"Unknown envelope type: {envelope.Type}");
                return;
        }
    }

    public void HandleDisconnect(Guid connectionId)
    {
        lock (_sync)
        {
            _matchmakingManager.Remove(connectionId);

            if (!_playerToSession.Remove(connectionId, out var sessionId))
            {
                return;
            }

            if (!_sessionStates.Remove(sessionId, out var gameState))
            {
                return;
            }

            foreach (var playerId in gameState.PlayerOrder)
            {
                _playerToSession.Remove(playerId);
            }
        }
    }

    private async Task HandleQueueQuickMatchAsync(Guid connectionId, CancellationToken cancellationToken)
    {
        var queued = _matchmakingManager.EnqueueQuickMatch(connectionId);
        Console.WriteLine(queued
            ? $"Player queued: {connectionId}"
            : $"Player already in queue: {connectionId}");

        var gameSession = _matchmakingManager.TryCreateMatch(_connectionManager);
        if (gameSession is null)
        {
            return;
        }

        lock (_sync)
        {
            _sessionStates[gameSession.SessionId] = gameSession.InitialState;
            foreach (var playerId in gameSession.PlayerConnectionIds)
            {
                _playerToSession[playerId] = gameSession.SessionId;
            }
        }

        Console.WriteLine($"Match created: {string.Join(" vs ", gameSession.PlayerConnectionIds)}");

        _combatEngine.TryDrawCard(gameSession.InitialState, gameSession.InitialState.CurrentPlayerId);
        gameSession.InitialState.Phase = TurnPhase.Play;
        await BroadcastStateSyncAsync(gameSession.InitialState, EnvelopeTypes.GameStart, cancellationToken);
    }

    private static bool IsActionAllowedInCurrentPhase(GameState state, string envelopeType)
    {
        return (state.Phase, envelopeType) switch
        {
            (TurnPhase.Draw, EnvelopeTypes.DrawCard) => true,
            (TurnPhase.Play, EnvelopeTypes.PlayCard) => true,
            (TurnPhase.Resolve, EnvelopeTypes.EndTurn) => true,
            _ => false
        };
    }

    private async Task ExecuteTurnStepAsync(Guid actorId, string envelopeType, CancellationToken cancellationToken)
    {
        GameState? gameState;

        lock (_sync)
        {
            if (!_playerToSession.TryGetValue(actorId, out var sessionId) ||
                !_sessionStates.TryGetValue(sessionId, out gameState))
            {
                return;
            }
        }

        if (gameState is null || gameState.IsGameOver || gameState.CurrentPlayerId != actorId)
        {
            return;
        }

        if (!IsActionAllowedInCurrentPhase(gameState, envelopeType))
        {
            await BroadcastStateSyncAsync(gameState, EnvelopeTypes.GameStateSync, cancellationToken);
            return;
        }

        switch (envelopeType)
        {
            case EnvelopeTypes.DrawCard:
                _combatEngine.TryDrawCard(gameState, actorId);
                gameState.Phase = TurnPhase.Play;
                break;
            case EnvelopeTypes.PlayCard:
                _combatEngine.TryPlayCard(gameState, actorId);
                gameState.Phase = TurnPhase.Resolve;
                break;
            case EnvelopeTypes.EndTurn:
                _combatEngine.ResolveAndDiscard(gameState, actorId);
                _combatEngine.CheckGameEnd(gameState);
                if (gameState.IsGameOver)
                {
                    gameState.Phase = TurnPhase.Finished;
                    break;
                }
                AdvanceTurn(gameState);
                _combatEngine.TryDrawCard(gameState, gameState.CurrentPlayerId);
                gameState.Phase = TurnPhase.Play;
                break;
        }

        await BroadcastStateSyncAsync(gameState, EnvelopeTypes.GameStateSync, cancellationToken);
    }

    private static void AdvanceTurn(GameState state)
    {
        state.CurrentPlayerIndex = (state.CurrentPlayerIndex + 1) % state.PlayerOrder.Count;
        state.TurnNumber += 1;
    }

    private async Task BroadcastStateSyncAsync(GameState state, string envelopeType, CancellationToken cancellationToken)
    {
        foreach (var playerId in state.PlayerOrder)
        {
            if (!_connectionManager.TryGet(playerId, out var socket) || socket?.State != WebSocketState.Open)
            {
                continue;
            }

            var payload = BuildPublicGameState(state, playerId);
            await SendEnvelopeAsync(socket, envelopeType, payload, cancellationToken);
        }
    }

    private static PublicGameState BuildPublicGameState(GameState state, Guid viewerId)
    {
        var viewer = state.Players[viewerId];
        var opponents = state.PlayerOrder
            .Where(playerId => playerId != viewerId)
            .Select(playerId =>
            {
                var opponent = state.Players[playerId];
                return new PublicOpponentState
                {
                    ConnectionId = playerId,
                    Health = opponent.Health,
                    Stamina = opponent.Stamina,
                    Strength = opponent.Strength,
                    Speed = opponent.Speed,
                    Energy = state.SharedEnergy.GetEnergy(playerId),
                    HandCount = opponent.Hand.Count,
                    PlayZone = opponent.PlayZone.ToList(),
                    PrimaryDeckCount = opponent.PrimaryCombatDeck.Count,
                    SecondaryDeckCount = opponent.SecondaryCombatDeck.Count,
                    DiscardCount = opponent.Discard.Count
                };
            })
            .ToList();

        return new PublicGameState
        {
            SessionId = state.SessionId,
            TurnNumber = state.TurnNumber,
            Phase = state.Phase,
            CurrentPlayerId = state.CurrentPlayerId,
            IsGameOver = state.IsGameOver,
            WinnerId = state.WinnerId,
            You = new PublicPlayerState
            {
                Health = viewer.Health,
                Stamina = viewer.Stamina,
                Strength = viewer.Strength,
                Speed = viewer.Speed,
                Energy = state.SharedEnergy.GetEnergy(viewerId),
                Hand = viewer.Hand.ToList(),
                PlayZone = viewer.PlayZone.ToList(),
                PrimaryDeckCount = viewer.PrimaryCombatDeck.Count,
                SecondaryDeckCount = viewer.SecondaryCombatDeck.Count,
                DiscardCount = viewer.Discard.Count
            },
            Opponents = opponents
        };
    }

    private static async Task SendEnvelopeAsync(
        WebSocket socket,
        string type,
        object payload,
        CancellationToken cancellationToken)
    {
        var envelope = new Envelope
        {
            Type = type,
            Payload = JsonSerializer.Serialize(payload)
        };

        var json = JsonSerializer.Serialize(envelope);
        var bytes = Encoding.UTF8.GetBytes(json);
        await socket.SendAsync(bytes, WebSocketMessageType.Text, true, cancellationToken);
    }
}
