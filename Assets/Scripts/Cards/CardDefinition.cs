using System;
using ANTK.Game.Common;
using UnityEngine;

/// <summary>
/// Defines card's shared properties, stats, and behaviors.
/// </summary>
[CreateAssetMenu(fileName = "Card Definition", menuName = "ANTK/Card Definition")]
public class CardDefinition : ScriptableObject
{
    public string title => _title;
    public CardType type => _type;
    public CardBehaviour behaviour => _behaviour;
    public Energy baseCost => (Energy) _baseCost;

    [SerializeField] private string _title;
    [SerializeField] private CardType _type;
    [SerializeField] private CardBehaviour _behaviour;
    [SerializeField] private int _baseCost;

    private void OnValidate()
    {
        _baseCost = Math.Max(0, _baseCost);
    }
}
