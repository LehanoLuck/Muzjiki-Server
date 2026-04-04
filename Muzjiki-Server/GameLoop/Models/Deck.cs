namespace Muzjiki_Server.GameLoop.Models;

public sealed class Deck
{
    private readonly List<Card> _cards;
    private readonly Random _random;

    public IReadOnlyList<Card> Cards => _cards;

    public Deck(IEnumerable<Card>? cards = null, Random? random = null)
    {
        _cards = cards?.ToList() ?? [];
        _random = random ?? Random.Shared;
    }

    public Card Draw()
    {
        if (_cards.Count == 0)
        {
            throw new InvalidOperationException("Cannot draw from an empty deck.");
        }

        var topCard = _cards[0];
        _cards.RemoveAt(0);
        return topCard;
    }

    public void Shuffle()
    {
        for (var i = _cards.Count - 1; i > 0; i--)
        {
            var swapIndex = _random.Next(i + 1);
            (_cards[i], _cards[swapIndex]) = (_cards[swapIndex], _cards[i]);
        }
    }

    public void Add(Card card)
    {
        ArgumentNullException.ThrowIfNull(card);
        _cards.Add(card);
    }
}
