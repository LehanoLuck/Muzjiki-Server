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

        await RunStepDraw(gameSession.InitialState, gameSession.InitialState.CurrentPlayerId);
        await BroadcastStateSyncAsync(gameSession.InitialState, EnvelopeTypes.GameStart, cancellationToken);
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

        if (gameState is null || gameState.CurrentPlayerId != actorId)
        {
            return;
        }

        switch (envelopeType)
        {
            case EnvelopeTypes.DrawCard:
                await RunStepDraw(gameState, actorId);
                gameState.Phase = "play";
                break;
            case EnvelopeTypes.PlayCard:
                RunStepPlay(gameState, actorId);
                gameState.Phase = "resolve";
                break;
            case EnvelopeTypes.EndTurn:
                RunStepResolve(gameState, actorId);
                RunStepDiscard(gameState, actorId);
                AdvanceTurn(gameState);
                await RunStepDraw(gameState, gameState.CurrentPlayerId);
                gameState.Phase = "play";
                break;
        }

        await BroadcastStateSyncAsync(gameState, EnvelopeTypes.GameStateSync, cancellationToken);
    }

    private static Task RunStepDraw(GameState state, Guid actorId)
    {
        var player = state.Players[actorId];
        var activeDeck = GetActiveDeck(player);
        if (activeDeck.Count <= 0)
        {
            return Task.CompletedTask;
        }

        var topCard = activeDeck[0];
        activeDeck.RemoveAt(0);
        player.Hand.Add(topCard);
        return Task.CompletedTask;
    }


    private static List<CardState> GetActiveDeck(PlayerGameState player)
    {
        return player.ActiveCombatDeck == CombatDeckType.Secondary
            ? player.SecondaryCombatDeck
            : player.PrimaryCombatDeck;
    }

    private static void RunStepPlay(GameState state, Guid actorId)
    {
        var player = state.Players[actorId];
        if (player.Hand.Count <= 0)
        {
            return;
        }

        var playedCard = player.Hand[0];
        player.Hand.RemoveAt(0);
        player.PlayZone.Add(playedCard);
    }

    private static void RunStepResolve(GameState state, Guid actorId)
    {
        var actor = state.Players[actorId];

        foreach (var opponentId in state.PlayerOrder.Where(playerId => playerId != actorId))
        {
            var totalPower = actor.PlayZone.Sum(card => card.Power);
            state.Players[opponentId].Health -= totalPower;
        }
    }

    private static void RunStepDiscard(GameState state, Guid actorId)
    {
        var actor = state.Players[actorId];
        actor.Discard.AddRange(actor.PlayZone);
        actor.PlayZone.Clear();
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
                    Energy = opponent.Energy,
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
            You = new PublicPlayerState
            {
                Health = viewer.Health,
                Stamina = viewer.Stamina,
                Strength = viewer.Strength,
                Speed = viewer.Speed,
                Energy = viewer.Energy,
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
