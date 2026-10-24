using System.Collections.Generic;
using UnityEngine;

public class UI_menuVisualsController : MonoBehaviour
{
    private const int POINT_COUNT = 12; // 12 połączonych punktów tworzy idealną wstęgę

    [Header("Elementy świata")]
    [SerializeField] private Transform ship;
    [SerializeField] private SpriteRenderer waterRenderer;

    [Header("Wyspy - przeszkody")]
    [Tooltip("Prefabry wysp losowane podczas pojawiania się przeszkód.")]
    [SerializeField] private GameObject[] islandPrefabs;
    [Tooltip("Wysokosc gornego pasa. Ustaw poza pionowym zasiegiem statku.")]
    [SerializeField] private float upperIslandLaneY = 2.2f;
    [Tooltip("Wysokosc dolnego pasa. Ustaw poza pionowym zasiegiem statku.")]
    [SerializeField] private float lowerIslandLaneY = -4.2f;
    [Tooltip("Predkosc wysp na gornym pasie.")]
    [SerializeField] private float upperIslandSpeed = 0.5f;
    [Tooltip("Predkosc wysp na dolnym pasie.")]
    [SerializeField] private float lowerIslandSpeed = 2.5f;
    [Tooltip("Minimalna losowa skala wysp na gornym, dalszym pasie.")]
    [SerializeField] private float upperIslandMinScale = 0.45f;
    [Tooltip("Maksymalna losowa skala wysp na gornym, dalszym pasie.")]
    [SerializeField] private float upperIslandMaxScale = 0.7f;
    [Tooltip("Minimalna losowa skala wysp na dolnym, blizszym pasie.")]
    [SerializeField] private float lowerIslandMinScale = 0.85f;
    [Tooltip("Maksymalna losowa skala wysp na dolnym, blizszym pasie.")]
    [SerializeField] private float lowerIslandMaxScale = 1.15f;
    [Tooltip("Mniejszy interwal spawnowania wysp na gornym pasie.")]
    [SerializeField] private float upperIslandSpawnInterval = 1.5f;
    [Tooltip("Interwal spawnowania wysp na dolnym pasie.")]
    [SerializeField] private float lowerIslandSpawnInterval = 3f;
    [SerializeField] private float islandSpawnMargin = 2f;
    [SerializeField] private float islandShipClearance = 0.25f;
    [SerializeField] private Transform islandContainer;

    [Header("Chmury")]
    [SerializeField] private bool spawnClouds = true;
    [SerializeField] private float cloudMinY = 2.4f;
    [SerializeField] private float cloudMaxY = 4.2f;
    [SerializeField] private float cloudMinScale = 0.7f;
    [SerializeField] private float cloudMaxScale = 1.35f;
    [SerializeField] private float cloudMinSpeed = 0.12f;
    [SerializeField] private float cloudMaxSpeed = 0.35f;
    [SerializeField] private float cloudSpawnInterval = 2.2f;
    [SerializeField] private float cloudSpawnMargin = 2f;
    [Tooltip("Liczba chmur tworzonych natychmiast po zaladowaniu menu.")]
    [SerializeField] private int initialCloudCount = 5;
    [Tooltip("Sprite'y chmur losowane podczas generowania.")]
    [SerializeField] private Sprite[] cloudSprites;
    [SerializeField] private Transform cloudContainer;

    [Header("Punkt powstawania piany")]
    [Tooltip("Punkt na rufie/kadłubie gdzie styka się z wodą")]
    [SerializeField] private Vector3 foamSpawnOffset = new Vector3(-0.6f, -0.3f, 0f);

    [Header("Fizyka śladu (Splajnu)")]
    [Tooltip("Prędkość spływania wody do tyłu (w lewo)")]
    [SerializeField] private float foamDriftSpeed = 0.12f;
    [Tooltip("Jak szybko każdy punkt podąża za poprzednim punktem")]
    [SerializeField] private float foamPointFollowTime = 0.18f;

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
    private Vector2[] splinePointVelocities = new Vector2[POINT_COUNT];
    private readonly List<SpawnedIsland> spawnedIslands = new List<SpawnedIsland>();
    private readonly List<Transform> spawnedClouds = new List<Transform>();

    private MaterialPropertyBlock propBlock;
    private static readonly int SplinePointsID = Shader.PropertyToID("_SplinePoints");
    private Camera mainCamera;
    private float upperIslandSpawnTimer;
    private float lowerIslandSpawnTimer;
    private float cloudSpawnTimer;

    private sealed class SpawnedIsland
    {
        public Transform transform;
    }

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
        mainCamera = Camera.main;

        // Inicjalizacja punktów poza ekranem
        for (int i = 0; i < POINT_COUNT; i++)
        {
            splinePoints[i] = new Vector4(-10f, -10f, 0f, 0f);
        }

        if (islandPrefabs != null && islandPrefabs.Length > 0)
        {
            upperIslandSpawnTimer = upperIslandSpawnInterval;
            lowerIslandSpawnTimer = lowerIslandSpawnInterval;
        }

        if (spawnClouds)
        {
            for (int i = 0; i < initialCloudCount; i++)
            {
                SpawnCloud(Random.Range(-0.1f, 1.1f));
            }
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

        UpdateIslands(dt);
        UpdateClouds(dt);

        if (waterRenderer == null) return;

        Bounds bounds = waterRenderer.bounds;
        Vector3 worldSpawn = ship.TransformPoint(foamSpawnOffset);
        float currentUvX = Mathf.InverseLerp(bounds.min.x, bounds.max.x, worldSpawn.x);
        float currentUvY = Mathf.InverseLerp(bounds.min.y, bounds.max.y, worldSpawn.y);

        // 2. Punkt 0 to zawsze aktualny punkt styku statku z wodą
        splinePoints[0] = new Vector4(currentUvX, currentUvY, 1.0f, 0f);

        // 3. Punkty płynnie podążają za poprzednim punktem.
        // Dzięki temu ogon nie teleportuje się przy tworzeniu kolejnego segmentu.
        for (int i = 1; i < POINT_COUNT; i++)
        {
            Vector2 currentPosition = new Vector2(splinePoints[i].x, splinePoints[i].y);
            currentPosition.x -= foamDriftSpeed * dt;

            Vector2 targetPosition = new Vector2(splinePoints[i - 1].x, splinePoints[i - 1].y);
            currentPosition = Vector2.SmoothDamp(
                currentPosition,
                targetPosition,
                ref splinePointVelocities[i],
                foamPointFollowTime,
                Mathf.Infinity,
                dt
            );

            splinePoints[i].x = currentPosition.x;
            splinePoints[i].y = currentPosition.y;
            splinePoints[i].z = 1.0f - ((float)i / POINT_COUNT);
        }

        // 4. Wysłanie punktów splajnu do shadera
        waterRenderer.GetPropertyBlock(propBlock);
        propBlock.SetVectorArray(SplinePointsID, splinePoints);
        waterRenderer.SetPropertyBlock(propBlock);
    }

    private void UpdateIslands(float dt)
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null || islandPrefabs == null || islandPrefabs.Length == 0)
        {
            return;
        }

        float leftEdge = GetCameraWorldX(0f) - islandSpawnMargin;
        for (int i = spawnedIslands.Count - 1; i >= 0; i--)
        {
            SpawnedIsland spawnedIsland = spawnedIslands[i];
            Transform island = spawnedIsland.transform;
            if (island == null)
            {
                spawnedIslands.RemoveAt(i);
                continue;
            }

            if (GetRendererBounds(island).max.x < leftEdge)
            {
                Destroy(island.gameObject);
                spawnedIslands.RemoveAt(i);
            }
        }

        upperIslandSpawnTimer += dt;
        if (upperIslandSpawnTimer >= upperIslandSpawnInterval)
        {
            upperIslandSpawnTimer -= upperIslandSpawnInterval;
            SpawnIsland(true);
        }

        lowerIslandSpawnTimer += dt;
        if (lowerIslandSpawnTimer >= lowerIslandSpawnInterval)
        {
            lowerIslandSpawnTimer -= lowerIslandSpawnInterval;
            SpawnIsland(false);
        }
    }

    private void UpdateClouds(float dt)
    {
        if (!spawnClouds || mainCamera == null || cloudSprites == null || cloudSprites.Length == 0)
        {
            return;
        }

        float leftEdge = GetCameraWorldX(0f) - cloudSpawnMargin;
        for (int i = spawnedClouds.Count - 1; i >= 0; i--)
        {
            Transform cloud = spawnedClouds[i];
            if (cloud == null)
            {
                spawnedClouds.RemoveAt(i);
                continue;
            }

            if (GetRendererBounds(cloud).max.x < leftEdge)
            {
                Destroy(cloud.gameObject);
                spawnedClouds.RemoveAt(i);
            }
        }

        cloudSpawnTimer += dt;
        if (cloudSpawnTimer < cloudSpawnInterval)
        {
            return;
        }

        cloudSpawnTimer -= cloudSpawnInterval;
        SpawnCloud(1f);
    }

    private void SpawnCloud(float viewportX)
    {
        float spawnX = GetCameraWorldX(viewportX);
        if (viewportX >= 1f)
        {
            spawnX += cloudSpawnMargin;
        }

        float spawnY = Random.Range(cloudMinY, cloudMaxY);
        GameObject cloudObject = new GameObject("Cloud");
        cloudObject.transform.SetParent(cloudContainer);
        cloudObject.transform.position = new Vector3(spawnX, spawnY, 0f);
        cloudObject.transform.localScale = Vector3.one * Random.Range(cloudMinScale, cloudMaxScale);

        SpriteRenderer renderer = cloudObject.AddComponent<SpriteRenderer>();
        renderer.sprite = cloudSprites[Random.Range(0, cloudSprites.Length)];
        renderer.color = new Color(0.92f, 0.97f, 1f, Random.Range(0.75f, 0.95f));
        renderer.sortingOrder = -2;

        CloudMovement movement = cloudObject.AddComponent<CloudMovement>();
        movement.Initialize(Random.Range(cloudMinSpeed, cloudMaxSpeed));
        spawnedClouds.Add(cloudObject.transform);
    }

    private void SpawnIsland(bool upperLane)
    {
        GameObject prefab = islandPrefabs[Random.Range(0, islandPrefabs.Length)];
        if (prefab == null)
        {
            Debug.LogError($"{nameof(UI_menuVisualsController)} contains an empty island prefab reference.", this);
            return;
        }

        float spawnX = GetCameraWorldX(1f) + islandSpawnMargin;
        float laneY = upperLane ? upperIslandLaneY : lowerIslandLaneY;
        Vector3 spawnPosition = new Vector3(spawnX, laneY, 0f);
        Transform island = Instantiate(prefab, spawnPosition, Quaternion.identity, islandContainer).transform;
        float scaleMultiplier = upperLane
            ? Random.Range(upperIslandMinScale, upperIslandMaxScale)
            : Random.Range(lowerIslandMinScale, lowerIslandMaxScale);
        island.localScale *= scaleMultiplier;

        IslandMovement movement = island.GetComponent<IslandMovement>();
        if (movement == null)
        {
            movement = island.gameObject.AddComponent<IslandMovement>();
        }

        float scaleSpeedMultiplier = Mathf.InverseLerp(
            upperIslandMinScale,
            lowerIslandMaxScale,
            scaleMultiplier
        );
        float baseSpeed = upperLane ? upperIslandSpeed : lowerIslandSpeed;
        float movementSpeed = baseSpeed * Mathf.Lerp(0.8f, 1.2f, scaleSpeedMultiplier);
        movement.Initialize(movementSpeed);

        KeepIslandOutsideShipLane(island, laneY);
        spawnedIslands.Add(new SpawnedIsland
        {
            transform = island
        });
    }

    private void KeepIslandOutsideShipLane(Transform island, float requestedLaneY)
    {
        Bounds shipBounds = GetRendererBounds(ship);
        shipBounds.Expand(new Vector3(0f, verticalMovement * 2f, 0f));

        Bounds islandBounds = GetRendererBounds(island);
        if (!shipBounds.Intersects(islandBounds))
        {
            return;
        }

        float direction = requestedLaneY >= ship.position.y ? 1f : -1f;
        float requiredCenterY = direction > 0f
            ? shipBounds.max.y + islandBounds.extents.y + islandShipClearance
            : shipBounds.min.y - islandBounds.extents.y - islandShipClearance;

        Vector3 position = island.position;
        position.y += requiredCenterY - islandBounds.center.y;
        island.position = position;
    }

    private float GetCameraWorldX(float viewportX)
    {
        float distanceToWorldPlane = -mainCamera.transform.position.z;
        return mainCamera.ViewportToWorldPoint(new Vector3(viewportX, 0.5f, distanceToWorldPlane)).x;
    }

    private static Bounds GetRendererBounds(Transform target)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return new Bounds(target.position, Vector3.zero);
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }
}