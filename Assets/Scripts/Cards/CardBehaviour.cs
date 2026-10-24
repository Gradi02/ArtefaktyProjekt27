using UnityEngine;

public abstract class CardBehaviour : ScriptableObject
{
    public abstract void Perform(CardContext context);
}