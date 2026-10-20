using UnityEngine;

[CreateAssetMenu(fileName = "Card", menuName = "ANTK/Card")]
public class Card : ScriptableObject
{
    public CardType type => _type;
    public bool usable { get; set; } = true;
    
    [SerializeField]
    private CardType _type;
}
