using UnityEngine;

public class CardFactory
{
    public CardDefinition[] allDefinitions => _allDefinitions;
    
    [SerializeField]
    private CardDefinition[] _allDefinitions;

    public Card CreateCard(CardDefinition def)
    {
        Card c = new Card(def);
        return c;
    }
}