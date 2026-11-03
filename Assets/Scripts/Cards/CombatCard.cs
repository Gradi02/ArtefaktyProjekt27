using ANTK.Game.Common;

/// <summary>
/// Represents a card instance with combat-specific state.
/// </summary>
public class CombatCard
{
    public readonly Card Source;

    public Energy CurrentCost;

    public CombatCard(Card source)
    {
        this.Source = source;

        CurrentCost = source.Definition.baseCost;
    }
}