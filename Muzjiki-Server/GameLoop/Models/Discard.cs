namespace Muzjiki_Server.GameLoop.Models;

public sealed class Discard
{
    private readonly List<Card> _cards = [];
    private readonly Random _random;

    public IReadOnlyList<Card> Cards => _cards;

    public Discard(Random? random = null)
    {
        _random = random ?? Random.Shared;
    }

    public Card Draw()
    {
        if (_cards.Count == 0)
        {
            throw new InvalidOperationException("Cannot draw from an empty discard pile.");
        }

        var topCard = _cards[^1];
        _cards.RemoveAt(_cards.Count - 1);
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
