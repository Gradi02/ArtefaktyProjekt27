using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField, Min(0)] private int _baseCardsDrawnAtTurnStart = 1;
    [SerializeField, Range(1, 6)] private int _programmingCapacity = 4;

    public int BaseCardsDrawnAtTurnStart => _baseCardsDrawnAtTurnStart;
    public int ProgrammingCapacity => _programmingCapacity;

    private void OnValidate()
    {
        _baseCardsDrawnAtTurnStart = Mathf.Max(0, _baseCardsDrawnAtTurnStart);
        _programmingCapacity = Mathf.Clamp(_programmingCapacity, 1, 6);
    }
}
