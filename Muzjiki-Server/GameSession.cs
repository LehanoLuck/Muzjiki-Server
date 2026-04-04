namespace Muzjiki_Server;

public class GameSession
{
    private const int MinPlayers = 2;
    private const int MaxPlayers = 8;

    public IReadOnlyList<Guid> PlayerConnectionIds { get; }
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

        PlayerConnectionIds = playerConnectionIds.ToList().AsReadOnly();
    }
}
