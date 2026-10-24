using UnityEngine;

/// <summary>
/// Defines the behavior executed when a card is played.
/// </summary>
public abstract class CardBehaviour : ScriptableObject
{
    public abstract void Perform(CardContext context);
}