using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField, Min(0)] private int _baseCardsDrawnAtTurnStart = 1;

    public int BaseCardsDrawnAtTurnStart => _baseCardsDrawnAtTurnStart;

    private void OnValidate()
    {
        _baseCardsDrawnAtTurnStart = Mathf.Max(0, _baseCardsDrawnAtTurnStart);
    }
}
