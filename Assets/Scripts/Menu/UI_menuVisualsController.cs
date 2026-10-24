using UnityEngine;

public class UI_menuVisualsController : MonoBehaviour
{
    [Header("Element świata")]
    [SerializeField] private Transform ship;

    [Header("Amplituda ruchu")]
    [SerializeField] private float sideMovement = 8f;
    [SerializeField] private float verticalMovement = 5f;

    [Header("Prędkość")]
    [SerializeField] private float movementSpeed = 1f;

    [Header("Obrót podczas ruchu")]
    [SerializeField] private float movementRotation = 4f;
    [SerializeField] private float rotationSmoothness = 8f;

    private Vector3 startingLocalPosition;
    private Quaternion startingRotation;
    private float timeOffset;

    private void Awake()
    {
        if (ship == null)
        {
            Debug.LogError($"{nameof(UI_menuVisualsController)} requires a ship Transform reference.", this);
            enabled = false;
            return;
        }

        startingLocalPosition = ship.localPosition;
        startingRotation = ship.localRotation;
        timeOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    private void Update()
    {
        float movementTime = Time.unscaledTime * movementSpeed + timeOffset;

        float sideOffset = Mathf.Sin(movementTime) * sideMovement;
        float verticalOffset = Mathf.Sin(movementTime * 0.8f + 1f) * verticalMovement;
        float sideVelocity = Mathf.Cos(movementTime);
        float targetRotation = sideVelocity * movementRotation;

        ship.localPosition = startingLocalPosition + new Vector3(sideOffset, verticalOffset, 0f);
        ship.localRotation = Quaternion.Lerp(
            ship.localRotation,
            startingRotation * Quaternion.Euler(0f, 0f, targetRotation),
            Time.unscaledDeltaTime * rotationSmoothness
        );
    }

    private void OnDisable()
    {
        if (ship != null)
        {
            ship.localPosition = startingLocalPosition;
            ship.localRotation = startingRotation;
        }
    }
}
