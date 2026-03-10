namespace Muzjiki_Server;

public class GameSession
{
    public required Guid Player1ConnectionId { get; init; }
    public required Guid Player2ConnectionId { get; init; }
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
}
