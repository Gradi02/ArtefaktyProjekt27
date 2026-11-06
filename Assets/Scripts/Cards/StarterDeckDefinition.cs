using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Starter Deck", menuName = "ANTK/Cards/Starter Deck")]
public sealed class StarterDeckDefinition : ScriptableObject
{
    [SerializeField] private List<CardQuantity> _cards = new List<CardQuantity>();

    public CardCollection CreateCollection()
    {
        CardCollection collection = new CardCollection();
        if (_cards == null)
        {
            throw new InvalidOperationException("Starter deck card list cannot be null.");
        }

        foreach (CardQuantity entry in _cards)
        {
            if (entry == null || !entry.Definition)
            {
                throw new InvalidOperationException("Starter deck entries must reference a card definition.");
            }
            if (entry.Quantity < 1)
            {
                throw new InvalidOperationException("Starter deck card quantities must be positive.");
            }

            for (int copy = 0; copy < entry.Quantity; copy++)
            {
                collection.Obtain(entry.Definition);
            }
        }

        return collection;
    }

    [Serializable]
    private sealed class CardQuantity
    {
        [SerializeField] private CardDefinition _definition;
        [SerializeField, Min(1)] private int _quantity = 1;

        public CardDefinition Definition => _definition;
        public int Quantity => _quantity;
    }
}
