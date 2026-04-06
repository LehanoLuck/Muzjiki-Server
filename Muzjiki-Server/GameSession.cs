namespace Muzjiki_Server;

public class GameSession
{
    private const int MinPlayers = 2;
    private const int MaxPlayers = 8;

    public Guid SessionId { get; }
    public IReadOnlyList<Guid> PlayerConnectionIds { get; }
    public GameState InitialState { get; }
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;

    public GameSession(IReadOnlyList<Guid> playerConnectionIds)
    {
        ArgumentNullException.ThrowIfNull(playerConnectionIds);

        if (playerConnectionIds.Count is < MinPlayers or > MaxPlayers)
        {
            throw new ArgumentOutOfRangeException(
                nameof(playerConnectionIds),
                $"Game session must have between {MinPlayers} and {MaxPlayers} players.");
        }

        SessionId = Guid.NewGuid();
        PlayerConnectionIds = playerConnectionIds.ToList().AsReadOnly();
        InitialState = GameState.CreateInitial(SessionId, PlayerConnectionIds);
    }
}
