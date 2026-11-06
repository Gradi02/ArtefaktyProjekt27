using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// Manages cards during combat. Draw pile, hand and discard pile.
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

    private static readonly Random Rnd = new Random();

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

        foreach (Card card in collection.Cards)
        {
            _drawPile.Add(new CombatCard(card));
        }
    }

    public bool Draw()
    {
        return DrawCards(1).Count == 1;
    }

    /// <summary>
    /// Draws specified amount of cards from draw pile and places them on hand.
    /// </summary>
    /// <returns>Amount of cards that was actually drawn.</returns>
    /// <exception cref="ArgumentOutOfRangeException">If amount is less than 1</exception>
    public int Draw(int amount)
    {
        return DrawCards(amount).Count;
    }

    /// <summary>
    /// Draws cards after shuffling the available draw pile once.
    /// </summary>
    public IReadOnlyList<CombatCard> DrawCards(int amount)
    {
        if (amount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Draw amount must be 1 or greater.");
        }

        List<CombatCard> drawnCards = new List<CombatCard>();
        if (_hand.Count >= HandCapacity)
        {
            return drawnCards.AsReadOnly();
        }

        if (_drawPile.Count == 0 && _discardPile.Count > 0)
        {
            _drawPile.AddRange(_discardPile);
            _discardPile.Clear();
        }
        if (_drawPile.Count == 0)
        {
            return drawnCards.AsReadOnly();
        }

        Shuffle(_drawPile);

        while (amount > 0 && _hand.Count < HandCapacity && _drawPile.Count > 0)
        {
            CombatCard targetCard = _drawPile[0];
            _drawPile.RemoveAt(0);
            _hand.Add(targetCard);
            drawnCards.Add(targetCard);
            amount--;
        }

        return drawnCards.AsReadOnly();
    }

    public bool Discard(CombatCard card)
    {
        if (card == null || !_hand.Remove(card))
        {
            return false;
        }

        _discardPile.Add(card);
        return true;
    }

    /// <summary>
    /// Adds a card to this fight only; it does not change the persistent run collection.
    /// </summary>
    public CombatCard AddTemporaryCard(CardDefinition definition)
    {
        CombatCard card = new CombatCard(new Card(definition));
        _drawPile.Add(card);
        return card;
    }

    /// <summary>
    /// Removes a card from any combat zone without changing the persistent run collection.
    /// </summary>
    public bool Remove(CombatCard card)
    {
        if (card == null)
        {
            return false;
        }

        return _hand.Remove(card) || _drawPile.Remove(card) || _discardPile.Remove(card);
    }

    public void ReshuffleDiscardPile()
    {
        _drawPile.AddRange(_discardPile);
        _discardPile.Clear();
        Shuffle(_drawPile);
    }

    private void Shuffle(List<CombatCard> cards)
    {
        for (int index = cards.Count - 1; index > 0; index--)
        {
            int swapIndex = Rnd.Next(index + 1);
            (cards[index], cards[swapIndex]) = (cards[swapIndex], cards[index]);
        }
    }
}