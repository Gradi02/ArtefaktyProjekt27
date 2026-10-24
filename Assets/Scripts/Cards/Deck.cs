using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public class Deck
{
    public readonly ReadOnlyCollection<Card> hand;
    public readonly int capacity;
    
    private List<Card> _hand;

    public Deck(int capacity, IEnumerable<Card> hand)
    {
        this.capacity = capacity;
        _hand = new List<Card>(hand);
        this.hand = _hand.AsReadOnly();
    }
    public Deck(int capacity) : this(capacity, Array.Empty<Card>()) {}


    #region On Hand

    public bool Insert(Card card)
    {
        // TODO: Handle if capacity was exceeded!
        _hand.Insert(0, card);
        return true;
    }
    public bool InsertAt(Card card, int index)
    {
        // TODO: Handle if capacity was exceeded!
        _hand.Insert(index, card);
        return true;
    }
    public Card WithdrawAt(int i)
    {
        Card c = _hand[i];
        _hand.RemoveAt(i);
        return c;
    }
    public bool Withdraw(Card card) => _hand.Remove(card);
    
    #endregion
}