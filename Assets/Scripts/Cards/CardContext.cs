/// <summary>
/// Information required to execute a card's behavior.
/// </summary>
public class CardContext
{
    public CombatCard card { get; }
    public Enemy target { get; }

    public CardContext(
        Enemy target,
        CombatCard card)
    {
        this.target = target;
        this.card = card;
    }
}