namespace Muzjiki_Server.GameLoop.Models;

public sealed class PlayerState
{
    public Guid PlayerId { get; }
    public Guid ConnectionId { get; }
    public PlayerInfo PlayerInfo { get; }
    public Deck Deck { get; }
    public Discard Discard { get; }
    public PlayZone PlayZone { get; }
    public PlayerHand PlayerHand { get; }

    public PlayerState(
        Guid playerId,
        Guid connectionId,
        PlayerInfo playerInfo,
        Deck deck,
        Discard discard,
        PlayZone playZone,
        PlayerHand playerHand)
    {
        if (playerId == Guid.Empty)
        {
            throw new ArgumentException("PlayerId cannot be empty.", nameof(playerId));
        }

        if (connectionId == Guid.Empty)
        {
            throw new ArgumentException("ConnectionId cannot be empty.", nameof(connectionId));
        }

        PlayerId = playerId;
        ConnectionId = connectionId;
        PlayerInfo = playerInfo ?? throw new ArgumentNullException(nameof(playerInfo));
        Deck = deck ?? throw new ArgumentNullException(nameof(deck));
        Discard = discard ?? throw new ArgumentNullException(nameof(discard));
        PlayZone = playZone ?? throw new ArgumentNullException(nameof(playZone));
        PlayerHand = playerHand ?? throw new ArgumentNullException(nameof(playerHand));
    }
}
