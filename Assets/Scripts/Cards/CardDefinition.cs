using System;
using ANTK.Game.Common;
using UnityEngine;

/// <summary>
/// Defines card's shared properties, stats, and behaviors.
/// </summary>
[CreateAssetMenu(fileName = "Card Definition", menuName = "ANTK/Card Definition")]
public class CardDefinition : ScriptableObject
{
    public string Id
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_id))
            {
                throw new InvalidOperationException("Card definitions must have a non-empty ID.");
            }

            return _id;
        }
    }
    public Sprite icon => _icon;
    public string title => _title;
    public CardType type => _type;
    public CardBehaviour behaviour => _behaviour;
    public Energy baseCost => (Energy) _baseCost;

    [SerializeField] private string _id;
    [SerializeField] private string _title;
    [SerializeField] private CardType _type;
    [SerializeField] private CardBehaviour _behaviour;
    [SerializeField] private int _baseCost;
    [SerializeField] private Sprite _icon;

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(_id))
        {
            _id = Guid.NewGuid().ToString("N");
        }

        _baseCost = Math.Max(0, _baseCost);
    }
}
