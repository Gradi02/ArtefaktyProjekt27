public class CardContext
{
    public Card card { get; }
    public Enemy target { get; }

    public CardContext(
        Enemy target,
        Card card)
    {
        this.target = target;
        this.card = card;
    }
}