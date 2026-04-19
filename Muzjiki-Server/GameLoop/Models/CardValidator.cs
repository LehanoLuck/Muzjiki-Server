namespace Muzjiki_Server.GameLoop.Models;

public static class CardValidator
{
    private static readonly HashSet<CardEffectType> NumericEffects =
    [
        CardEffectType.DrawCards,
        CardEffectType.DiscardCards,
        CardEffectType.DealDamage,
        CardEffectType.GainDefense
    ];

    private static readonly HashSet<CardEffectType> NonNumericEffects =
    [
        CardEffectType.ReturnToHand,
        CardEffectType.Stun
    ];

    public static void ValidateCard(CardType type, int cost, CardProperty primaryProperty, CardProperty secondaryProperty)
    {
        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(nameof(type), "Card type is not defined.");
        }

        if (cost is < 0 or > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(cost), "Card cost must be in range 0..5.");
        }

        ArgumentNullException.ThrowIfNull(primaryProperty);
        ArgumentNullException.ThrowIfNull(secondaryProperty);
    }

    public static IReadOnlyCollection<CardEffect> ValidateCardProperty(IEnumerable<CardEffect> effects)
    {
        ArgumentNullException.ThrowIfNull(effects);

        var list = effects.ToList();
        if (list.Count == 0)
        {
            throw new ArgumentException("Card property must contain at least one effect.", nameof(effects));
        }

        if (list.Any(effect => effect is null))
        {
            throw new ArgumentException("Card property cannot contain null effects.", nameof(effects));
        }

        return list;
    }

    public static void ValidateCardEffect(CardEffectType type, int? value)
    {
        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(nameof(type), "Card effect type is not defined.");
        }

        if (NumericEffects.Contains(type))
        {
            if (value is null)
            {
                throw new ArgumentException($"Effect '{type}' requires a numeric value.", nameof(value));
            }

            return;
        }

        if (NonNumericEffects.Contains(type) && value is not null)
        {
            throw new ArgumentException($"Effect '{type}' must not contain a numeric value.", nameof(value));
        }
    }
}
