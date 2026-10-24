using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

/// <summary>
/// Manages cards during combat. Draw pile, hand and discrarded pile.
/// </summary>
public class CombatDeck
{
    public readonly ReadOnlyCollection<CombatCard> Hand;
    public readonly ReadOnlyCollection<CombatCard> DrawPile;
    public readonly ReadOnlyCollection<CombatCard> DiscardPile;
    
    public readonly int HandCapacity;
    
    private readonly List<CombatCard> _hand;
    private readonly List<CombatCard> _drawPile;
    private readonly List<CombatCard> _discardPile;

    private static readonly Random Rnd = new();

    public CombatDeck(CardCollection collection, int handCapacity)
    {
        if (collection == null)
            throw new ArgumentNullException("Deck definition can't be null!");
        if (handCapacity < 1)
            throw new ArgumentOutOfRangeException("Hand capacity can't be below 1!");
        
        this.HandCapacity = handCapacity;
        
        _hand = new List<CombatCard>();
        _drawPile = new List<CombatCard>();
        _discardPile = new List<CombatCard>();
        
        this.Hand = _hand.AsReadOnly();
        this.DrawPile = _drawPile.AsReadOnly();
        this.DiscardPile = _discardPile.AsReadOnly();

        foreach (var card in collection.Cards)
        {
            _drawPile.Add(new CombatCard(card));
        }
        Shuffle(_drawPile);
    }


    public bool Draw()
    {
        if (_hand.Count + 1 > HandCapacity)
            return false;
        if (_drawPile.Count == 0)
            return false;

        int lastInd = _drawPile.Count - 1;
        CombatCard targetCard = _drawPile[lastInd];
        _drawPile.RemoveAt(lastInd);
        
        _hand.Add(targetCard);
        return true;
    }
    
    /// <summary>
    /// Draws specified amount of cards from draw pile and places them on hand.
    /// </summary>
    /// <returns>Amount of cards that was actually drawn.</returns>
    /// <exception cref="ArgumentOutOfRangeException">If amount is less than 1</exception>
    public int Draw(int amount)
    {
        if (amount < 1)
            throw new ArgumentOutOfRangeException("Draw amount of cards should be 1 or greater!");

        int drawn = 0;
        for ( ; amount > 0; amount--)
        {
            if (!Draw())
                break;
            drawn++;
        }
        return drawn;
    }

    public bool Discard(CombatCard card)
    {
        if (!_hand.Remove(card))
            return false;
        
        _discardPile.Add(card);
        return true;
    }

    public void ReshuffleDiscardPile()
    {
        _drawPile.AddRange(_discardPile);
        _discardPile.Clear();
        Shuffle(_drawPile);
    }

    // TODO: Use other shuffle method
    private void Shuffle(List<CombatCard> cards)
    {
        int count = cards.Count;
        
        for (int steps = cards.Count; steps > 0; steps--)
        {
            int indA = Rnd.Next(0, count);
            int indB = Rnd.Next(0, count);
            
            (cards[indB], cards[indA]) = (cards[indA], cards[indB]);
        }
    }
}