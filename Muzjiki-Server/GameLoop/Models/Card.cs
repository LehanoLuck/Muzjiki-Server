namespace Muzjiki_Server.GameLoop.Models;

public sealed class Card
{
    public CardType Type { get; }
    public int Cost { get; }
    public CardProperty PrimaryProperty { get; }
    public CardProperty SecondaryProperty { get; }

    private Card(CardType type, int cost, CardProperty primaryProperty, CardProperty secondaryProperty)
    {
        Type = type;
        Cost = cost;
        PrimaryProperty = primaryProperty;
        SecondaryProperty = secondaryProperty;
    }

    public static Card Create(CardType type, int cost, CardProperty primaryProperty, CardProperty secondaryProperty)
    {
        CardValidator.ValidateCard(type, cost, primaryProperty, secondaryProperty);
        return new Card(type, cost, primaryProperty, secondaryProperty);
    }
}
