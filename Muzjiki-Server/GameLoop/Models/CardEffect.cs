namespace Muzjiki_Server.GameLoop.Models;

public sealed class CardEffect
{
    public CardEffectType Type { get; }
    public int? Value { get; }

    private CardEffect(CardEffectType type, int? value)
    {
        Type = type;
        Value = value;
    }

    public static CardEffect Create(CardEffectType type, int? value)
    {
        CardValidator.ValidateCardEffect(type, value);
        return new CardEffect(type, value);
    }
}
