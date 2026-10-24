using UnityEngine;

[CreateAssetMenu(fileName = "Card Definition", menuName = "ANTK/Card Definition")]
public class CardDefinition : ScriptableObject
{
    public string title => _title;
    public CardType type => _type;
    public CardBehaviour behaviour => _behaviour;
    public int baseCost => _baseCost;

    [SerializeField] private string _title;
    [SerializeField] private CardType _type;
    [SerializeField] private CardBehaviour _behaviour;
    [SerializeField] private int _baseCost;
}
