namespace Muzjiki_Server.GameLoop.Models;

public sealed class PlayerHand
{
    private readonly List<Card> _cards = [];

    public int MinSize { get; }
    public int MaxSize { get; }
    public IReadOnlyList<Card> Cards => _cards;

    public PlayerHand(int minSize, int maxSize, IEnumerable<Card>? initialCards = null)
    {
        if (minSize < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minSize), "MinSize must be non-negative.");
        }

        if (maxSize < minSize)
        {
            throw new ArgumentOutOfRangeException(nameof(maxSize), "MaxSize must be greater than or equal to MinSize.");
        }

        MinSize = minSize;
        MaxSize = maxSize;

        if (initialCards is not null)
        {
            foreach (var card in initialCards)
            {
                AddCard(card);
            }
        }
    }

    public bool CanAdd() => _cards.Count < MaxSize;

    public bool CanRemove() => _cards.Count > MinSize;

    public void AddCard(Card card)
    {
        ArgumentNullException.ThrowIfNull(card);

        if (!CanAdd())
        {
            throw new InvalidOperationException("Cannot add card: hand is at maximum size.");
        }

        _cards.Add(card);
    }

    public bool RemoveCard(Card card)
    {
        ArgumentNullException.ThrowIfNull(card);

        if (!CanRemove())
        {
            throw new InvalidOperationException("Cannot remove card: hand is at minimum size.");
        }

        return _cards.Remove(card);
    }
}
