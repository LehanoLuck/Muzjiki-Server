namespace Muzjiki_Server.GameLoop.Models;

public sealed class CardProperty
{
    private readonly List<CardEffect> _effects;

    public IReadOnlyList<CardEffect> Effects => _effects;

    private CardProperty(IEnumerable<CardEffect> effects)
    {
        _effects = effects.ToList();
    }

    public static CardProperty Create(IEnumerable<CardEffect> effects)
    {
        var validatedEffects = CardValidator.ValidateCardProperty(effects);
        return new CardProperty(validatedEffects);
    }
}
