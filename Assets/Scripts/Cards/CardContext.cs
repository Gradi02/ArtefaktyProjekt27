public class CardContext
{
    public Enemy target { get; }

    public CardContext(Enemy target)
    {
        this.target = target;
    }
}