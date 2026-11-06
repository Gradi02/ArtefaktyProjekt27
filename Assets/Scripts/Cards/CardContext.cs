using System;
using ANTK.Game.StateEngine;

/// <summary>
/// Information required to execute a card's behavior.
/// </summary>
public class CardContext
{
    public CombatCard Card { get; }
    public LivingMixin Source { get; }
    public LivingMixin Target { get; }
    public StateEngine StateEngine { get; }

    public CardContext(
        StateEngine stateEngine,
        LivingMixin source,
        LivingMixin target,
        CombatCard card)
    {
        StateEngine = stateEngine ?? throw new ArgumentNullException(nameof(stateEngine));
        Source = source ?? throw new ArgumentNullException(nameof(source));
        Target = target ?? throw new ArgumentNullException(nameof(target));
        Card = card ?? throw new ArgumentNullException(nameof(card));
    }
}