using UnityEngine;

public class UI_menuVisualsController : MonoBehaviour
{
    private const int POINT_COUNT = 12; // 12 połączonych punktów tworzy idealną wstęgę

    [Header("Elementy świata")]
    [SerializeField] private Transform ship;
    [SerializeField] private SpriteRenderer waterRenderer;

    [Header("Punkt powstawania piany")]
    [Tooltip("Punkt na rufie/kadłubie gdzie styka się z wodą")]
    [SerializeField] private Vector3 foamSpawnOffset = new Vector3(-0.6f, -0.3f, 0f);

    [Header("Fizyka śladu (Splajnu)")]
    [Tooltip("Prędkość spływania wody do tyłu (w lewo)")]
    [SerializeField] private float foamDriftSpeed = 0.12f;
    [Tooltip("Co ile sekund dodajemy nowy segment")]
    [SerializeField] private float segmentInterval = 0.08f;

    [Header("Ruch statku")]
    [SerializeField] private float sideMovement = 8f;
    [SerializeField] private float verticalMovement = 5f;
    [SerializeField] private float movementSpeed = 1f;
    [SerializeField] private float movementRotation = 4f;
    [SerializeField] private float rotationSmoothness = 8f;

    private Vector3 startingLocalPosition;
    private Quaternion startingRotation;
    private float timeOffset;

    // Punkty splajnu: x = uv.x, y = uv.y, z = szerokość/waga (0..1)
    private Vector4[] splinePoints = new Vector4[POINT_COUNT];
    private float spawnTimer;

    private MaterialPropertyBlock propBlock;
    private static readonly int SplinePointsID = Shader.PropertyToID("_SplinePoints");

    private void Awake()
    {
        if (ship == null)
        {
            Debug.LogError($"{nameof(UI_menuVisualsController)} requires a ship reference.", this);
            enabled = false;
            return;
        }

        startingLocalPosition = ship.localPosition;
        startingRotation = ship.localRotation;
        timeOffset = Random.Range(0f, Mathf.PI * 2f);
        propBlock = new MaterialPropertyBlock();

        // Inicjalizacja punktów poza ekranem
        for (int i = 0; i < POINT_COUNT; i++)
        {
            splinePoints[i] = new Vector4(-10f, -10f, 0f, 0f);
        }
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;

        // 1. Ruch kołysania statku
        float movementTime = Time.unscaledTime * movementSpeed + timeOffset;
        float sideOffset = Mathf.Sin(movementTime) * sideMovement;
        float verticalOffset = Mathf.Sin(movementTime * 0.8f + 1f) * verticalMovement;
        float sideVelocity = Mathf.Cos(movementTime);
        float targetRotation = sideVelocity * movementRotation;

        ship.localPosition = startingLocalPosition + new Vector3(sideOffset, verticalOffset, 0f);
        ship.localRotation = Quaternion.Lerp(
            ship.localRotation,
            startingRotation * Quaternion.Euler(0f, 0f, targetRotation),
            dt * rotationSmoothness
        );

        if (waterRenderer == null) return;

        Bounds bounds = waterRenderer.bounds;
        Vector3 worldSpawn = ship.TransformPoint(foamSpawnOffset);
        float currentUvX = Mathf.InverseLerp(bounds.min.x, bounds.max.x, worldSpawn.x);
        float currentUvY = Mathf.InverseLerp(bounds.min.y, bounds.max.y, worldSpawn.y);

        // 2. Punkt 0 to zawsze aktualny punkt styku statku z wodą
        splinePoints[0] = new Vector4(currentUvX, currentUvY, 1.0f, 0f);

        // 3. Spływanie w tył wszystkich starszych punktów splajnu
        for (int i = 1; i < POINT_COUNT; i++)
        {
            splinePoints[i].x -= foamDriftSpeed * dt;
        }

        // 4. Cykliczne przesuwanie łańcucha (tworzenie nowego segmentu splajnu)
        spawnTimer += dt;
        if (spawnTimer >= segmentInterval)
        {
            spawnTimer = 0f;
            // Przesuwamy węzły w tył (od ogona do głowy)
            for (int i = POINT_COUNT - 1; i > 0; i--)
            {
                splinePoints[i] = splinePoints[i - 1];
                // Waga/życie zanika z każdym segmentem dalej
                splinePoints[i].z = 1.0f - ((float)i / POINT_COUNT);
            }
        }

        // 5. Wysłanie punktów splajnu do shadera
        waterRenderer.GetPropertyBlock(propBlock);
        propBlock.SetVectorArray(SplinePointsID, splinePoints);
        waterRenderer.SetPropertyBlock(propBlock);
    }
}