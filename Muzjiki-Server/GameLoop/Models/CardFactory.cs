namespace Muzjiki_Server.GameLoop.Models;

public static class CardFactory
{
    public static CardEffect Effect(CardEffectType type, int? value = null) =>
        CardEffect.Create(type, value);

    public static CardProperty Property(params CardEffect[] effects) =>
        CardProperty.Create(effects);

    public static Card CreateCard(CardType type, int cost, CardProperty primaryProperty, CardProperty secondaryProperty) =>
        global::Muzjiki_Server.GameLoop.Models.Card.Create(type, cost, primaryProperty, secondaryProperty);
}
