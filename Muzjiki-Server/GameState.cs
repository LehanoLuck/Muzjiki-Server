namespace Muzjiki_Server;

public enum CombatDeckType
{
    Primary = 1,
    Secondary = 2
}

public enum CardClass
{
    Placeholder = 0
}

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
            var (primaryDeck, secondaryDeck) = BuildDefaultDecks();

            players[connectionId] = new PlayerGameState
            {
                ConnectionId = connectionId,
                Health = 15,
                Stamina = 1,
                Strength = 1,
                Speed = 1,
                // TODO: replace with finalized start-phase energy rule when game design is locked.
                Energy = 0,
                PrimaryCombatDeck = primaryDeck,
                SecondaryCombatDeck = secondaryDeck,
                ActiveCombatDeck = CombatDeckType.Primary,
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

    private static (List<CardState> primaryDeck, List<CardState> secondaryDeck) BuildDefaultDecks()
    {
        var cards = Enumerable.Range(1, 10)
            .Select(index => new CardState
            {
                Id = Guid.NewGuid(),
                Name = $"Dummy-Card-{index}",
                Power = 0,
                Class = CardClass.Placeholder,
                DeckType = index <= 5 ? CombatDeckType.Primary : CombatDeckType.Secondary
            })
            .OrderBy(_ => Random.Next())
            .ToList();

        var primaryDeck = cards.Where(card => card.DeckType == CombatDeckType.Primary).ToList();
        var secondaryDeck = cards.Where(card => card.DeckType == CombatDeckType.Secondary).ToList();
        return (primaryDeck, secondaryDeck);
    }
}

public class PlayerGameState
{
    public Guid ConnectionId { get; init; }
    public int Health { get; set; }
    public int Stamina { get; set; }
    public int Strength { get; set; }
    public int Speed { get; set; }
    public int Energy { get; set; }
    public List<CardState> PrimaryCombatDeck { get; set; } = [];
    public List<CardState> SecondaryCombatDeck { get; set; } = [];
    public CombatDeckType ActiveCombatDeck { get; set; } = CombatDeckType.Primary;
    public List<CardState> Hand { get; set; } = [];
    public List<CardState> Discard { get; set; } = [];
    public List<CardState> PlayZone { get; set; } = [];
}

public class CardState
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int Power { get; init; }
    public CardClass Class { get; init; } = CardClass.Placeholder;
    public CombatDeckType DeckType { get; init; } = CombatDeckType.Primary;
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
    public int Stamina { get; init; }
    public int Strength { get; init; }
    public int Speed { get; init; }
    public int Energy { get; init; }
    public IReadOnlyList<CardState> Hand { get; init; } = [];
    public IReadOnlyList<CardState> PlayZone { get; init; } = [];
    public int PrimaryDeckCount { get; init; }
    public int SecondaryDeckCount { get; init; }
    public int DiscardCount { get; init; }
}

public class PublicOpponentState
{
    public Guid ConnectionId { get; init; }
    public int Health { get; init; }
    public int Stamina { get; init; }
    public int Strength { get; init; }
    public int Speed { get; init; }
    public int Energy { get; init; }
    public int HandCount { get; init; }
    public IReadOnlyList<CardState> PlayZone { get; init; } = [];
    public int PrimaryDeckCount { get; init; }
    public int SecondaryDeckCount { get; init; }
    public int DiscardCount { get; init; }
}
