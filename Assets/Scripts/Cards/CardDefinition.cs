using UnityEngine;

[CreateAssetMenu(fileName = "Card Definition", menuName = "ANTK/Card Definition")]
public class CardDefinition : ScriptableObject
{
    public string title => _title;
    public CardType type => _type;
    public CardBehaviour behaviour => _behaviour;

    [SerializeField]
    private string _title;
    [SerializeField]
    private CardType _type;
    [SerializeField] 
    private CardBehaviour _behaviour;
}
