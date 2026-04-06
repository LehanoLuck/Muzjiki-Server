namespace Muzjiki_Server;

public class GameState
{
    private static readonly Random Random = new();

    public Guid SessionId { get; init; }
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
    public IReadOnlyList<Guid> PlayerOrder { get; init; } = [];
    public Dictionary<Guid, PlayerGameState> Players { get; init; } = new();
    public int CurrentPlayerIndex { get; set; }
    public int TurnNumber { get; set; } = 1;
    public string Phase { get; set; } = "draw";

    public Guid CurrentPlayerId => PlayerOrder[CurrentPlayerIndex];

    public static GameState CreateInitial(Guid sessionId, IReadOnlyList<Guid> playerConnectionIds)
    {
        ArgumentNullException.ThrowIfNull(playerConnectionIds);

        var players = new Dictionary<Guid, PlayerGameState>();

        foreach (var connectionId in playerConnectionIds)
        {
            var shuffledDeck = BuildDefaultDeck();
            shuffledDeck = shuffledDeck.OrderBy(_ => Random.Next()).ToList();

            players[connectionId] = new PlayerGameState
            {
                ConnectionId = connectionId,
                Health = 20,
                Deck = shuffledDeck,
                Hand = [],
                Discard = [],
                PlayZone = []
            };
        }

        return new GameState
        {
            SessionId = sessionId,
            PlayerOrder = playerConnectionIds.ToList().AsReadOnly(),
            Players = players
        };
    }

    private static List<CardState> BuildDefaultDeck()
    {
        return Enumerable.Range(1, 10)
            .Select(index => new CardState
            {
                Id = Guid.NewGuid(),
                Name = $"Card-{index}",
                Power = (index % 3) + 1
            })
            .ToList();
    }
}

public class PlayerGameState
{
    public Guid ConnectionId { get; init; }
    public int Health { get; set; }
    public List<CardState> Deck { get; set; } = [];
    public List<CardState> Hand { get; set; } = [];
    public List<CardState> Discard { get; set; } = [];
    public List<CardState> PlayZone { get; set; } = [];
}

public class CardState
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int Power { get; init; }
}

public class PublicGameState
{
    public Guid SessionId { get; init; }
    public int TurnNumber { get; init; }
    public string Phase { get; init; } = string.Empty;
    public Guid CurrentPlayerId { get; init; }
    public PublicPlayerState You { get; init; } = new();
    public IReadOnlyList<PublicOpponentState> Opponents { get; init; } = [];
}

public class PublicPlayerState
{
    public int Health { get; init; }
    public IReadOnlyList<CardState> Hand { get; init; } = [];
    public IReadOnlyList<CardState> PlayZone { get; init; } = [];
    public int DeckCount { get; init; }
    public int DiscardCount { get; init; }
}

public class PublicOpponentState
{
    public Guid ConnectionId { get; init; }
    public int Health { get; init; }
    public int HandCount { get; init; }
    public IReadOnlyList<CardState> PlayZone { get; init; } = [];
    public int DeckCount { get; init; }
    public int DiscardCount { get; init; }
}
