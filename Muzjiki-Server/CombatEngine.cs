namespace Muzjiki_Server;

public sealed class CombatEngine
{
    public bool TryDrawCard(GameState state, Guid actorId)
    {
        var player = state.Players[actorId];
        var activeDeck = GetActiveDeck(player);
        if (activeDeck.Count == 0)
        {
            return false;
        }

        var topCard = activeDeck[0];
        activeDeck.RemoveAt(0);
        player.Hand.Add(topCard);
        return true;
    }

    public bool TryPlayCard(GameState state, Guid actorId)
    {
        var player = state.Players[actorId];
        if (player.Hand.Count == 0)
        {
            return false;
        }

        var playedCard = player.Hand[0];
        player.Hand.RemoveAt(0);
        player.PlayZone.Add(playedCard);
        return true;
    }

    public void ResolveAndDiscard(GameState state, Guid actorId)
    {
        ApplyCardEffects(state, actorId);
        DiscardPlayedCards(state, actorId);
    }

    public GameEndState CheckGameEnd(GameState state)
    {
        if (state.IsGameOver)
        {
            return new GameEndState(true, state.WinnerId);
        }

        var alivePlayers = state.PlayerOrder.Where(playerId => state.Players[playerId].Health > 0).ToList();
        if (alivePlayers.Count == 1)
        {
            state.IsGameOver = true;
            state.WinnerId = alivePlayers[0];
            return new GameEndState(true, state.WinnerId);
        }

        if (alivePlayers.Count == 0)
        {
            state.IsGameOver = true;
            state.WinnerId = null;
            return new GameEndState(true, null);
        }

        return new GameEndState(false, null);
    }

    private static List<CardState> GetActiveDeck(PlayerGameState player)
    {
        return player.ActiveCombatDeck == CombatDeckType.Secondary
            ? player.SecondaryCombatDeck
            : player.PrimaryCombatDeck;
    }

    private static void ApplyCardEffects(GameState state, Guid actorId)
    {
        var actor = state.Players[actorId];
        var totalPower = actor.PlayZone.Sum(card => card.Power);

        foreach (var opponentId in state.PlayerOrder.Where(playerId => playerId != actorId))
        {
            state.Players[opponentId].Health -= totalPower;
        }
    }

    private static void DiscardPlayedCards(GameState state, Guid actorId)
    {
        var actor = state.Players[actorId];
        actor.Discard.AddRange(actor.PlayZone);
        actor.PlayZone.Clear();
    }
}

public sealed record GameEndState(bool IsOver, Guid? WinnerId);
