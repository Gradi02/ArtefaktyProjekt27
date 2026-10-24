using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// Manages player's collection of persistent cards instances.
/// </summary>
public class CardCollection
{
    public readonly ReadOnlyCollection<Card> Cards;
    
    private readonly List<Card> _cards;

    public CardCollection() : this(Array.Empty<Card>()) {}
    public CardCollection(IEnumerable<Card> cards)
    {
        _cards = new List<Card>(cards);
        this.Cards = _cards.AsReadOnly();
    }

    
    /// <summary>
    /// Creates an instance of a card based on given definition and adds it to the collection.
    /// </summary>
    /// <param name="definition">Wanted card definition.</param>
    /// <returns>New card instance.</returns>
    public Card Obtain(CardDefinition definition)
    {
        var card = new Card(definition);
        _cards.Add(card);
        return card;
    }

    public bool Remove(Card card)
    {
        return _cards.Remove(card);
    }
    
    public bool Contains(Card card)
    {
        return _cards.Contains(card);
    }
}