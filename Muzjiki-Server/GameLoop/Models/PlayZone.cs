namespace Muzjiki_Server.GameLoop.Models;

public sealed class PlayZone
{
    public Card? ActiveCard { get; private set; }

    public bool HasActiveCard => ActiveCard is not null;

    public void SetActiveCard(Card card)
    {
        ArgumentNullException.ThrowIfNull(card);

        if (ActiveCard is not null)
        {
            throw new InvalidOperationException("Play zone can contain only one active card.");
        }

        ActiveCard = card;
    }

    public Card? RemoveActiveCard()
    {
        var card = ActiveCard;
        ActiveCard = null;
        return card;
    }
}
