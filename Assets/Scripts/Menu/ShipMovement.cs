using UnityEngine;

public class ShipMovement : MonoBehaviour
{
    [Header("Amplituda ruchu")]
    [SerializeField] private float sideMovement = 8f;
    [SerializeField] private float verticalMovement = 5f;

    [Header("Prędkość")]
    [SerializeField] private float movementSpeed = 1f;

    [Header("Obrót podczas ruchu")]
    [SerializeField] private float movementRotation = 4f;
    [SerializeField] private float rotationSmoothness = 8f;

    private RectTransform rectTransform;
    private Vector2 startingAnchoredPosition;
    private Quaternion startingRotation;
    private float timeOffset;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        startingAnchoredPosition = rectTransform.anchoredPosition;
        startingRotation = rectTransform.localRotation;
        timeOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    private void Update()
    {
        float movementTime = Time.unscaledTime * movementSpeed + timeOffset;

        float sideOffset = Mathf.Sin(movementTime) * sideMovement;
        float verticalOffset = Mathf.Sin(movementTime * 0.8f + 1f) * verticalMovement;
        float sideVelocity = Mathf.Cos(movementTime);
        float targetRotation = sideVelocity * movementRotation;

        rectTransform.anchoredPosition = startingAnchoredPosition + new Vector2(sideOffset, verticalOffset);
        rectTransform.localRotation = Quaternion.Lerp(
            rectTransform.localRotation,
            startingRotation * Quaternion.Euler(0f, 0f, targetRotation),
            Time.unscaledDeltaTime * rotationSmoothness
        );
    }

    private void OnDisable()
    {
        if (rectTransform != null)
        {
            rectTransform.localRotation = startingRotation;
        }
    }
}
