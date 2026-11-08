using UnityEngine;

public class ShipSwayBattleScene : MonoBehaviour
{
    [Header("Ustawienia bujania")]
    [Tooltip("Maksymalny kąt wychylenia w stopniach (w jedną stronę)")]
    [SerializeField] private float maxAngle = 5f; // Lekkie wychylenie (np. 5 stopni)

    [Tooltip("Prędkość bujania")]
    [SerializeField] private float speed = 2f; 

    private float startZRotation;

    void Start()
    {
        // Zapisujemy początkową rotację statku, żeby bujał się wokół niej
        startZRotation = transform.eulerAngles.z;
    }

    void Update()
    {
        // Mathf.Sin zwraca płynną wartość od -1 do 1 na osi czasu
        float currentAngle = Mathf.Sin(Time.time * speed) * maxAngle;

        // Obracamy obiekt tylko w osi Z (właściwej dla 2D)
        transform.rotation = Quaternion.Euler(0f, 0f, startZRotation + currentAngle);
    }
}