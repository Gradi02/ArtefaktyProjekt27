using UnityEngine;

public class UI_menuVisualsController : MonoBehaviour
{
    [Header("Element świata")]
    [SerializeField] private Transform ship;
    [SerializeField] private SpriteRenderer waterRenderer; // Przeciągnij tu obiekt wody (Square)

    [Header("Punkt zanurzenia statku")]
    [Tooltip("Lokalny punkt statku, w którym dotyka on wody (np. spód kadłuba)")]
    [SerializeField] private Vector3 waterContactOffset = new Vector3(0f, -0.5f, 0f);

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
    private MaterialPropertyBlock propBlock;
    private static readonly int ShipUVProperty = Shader.PropertyToID("_ShipUV");

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
        propBlock = new MaterialPropertyBlock();
    }

    private void Update()
    {
        // Ruch statku
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

        // Przekazanie pozycji do wody
        UpdateShipWaterFoam();
    }

    private void UpdateShipWaterFoam()
    {
        if (waterRenderer == null) return;

        Bounds bounds = waterRenderer.bounds;
        Vector3 shipWorldPos = ship.TransformPoint(waterContactOffset);

        // Obliczenie pozycji statku w koordynatach UV wody (0 do 1)
        float uvX = Mathf.InverseLerp(bounds.min.x, bounds.max.x, shipWorldPos.x);
        float uvY = Mathf.InverseLerp(bounds.min.y, bounds.max.y, shipWorldPos.y);

        waterRenderer.GetPropertyBlock(propBlock);
        propBlock.SetVector(ShipUVProperty, new Vector4(uvX, uvY, 0, 0));
        waterRenderer.SetPropertyBlock(propBlock);
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