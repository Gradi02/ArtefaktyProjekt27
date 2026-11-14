using System;
using System.Collections.Generic;
using ANTK.Game.StateEngine.Actions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class HexBoardView : MonoBehaviour
{
    private const float TileRadius = 34f;
    private readonly Dictionary<HexCoordinate, RectTransform> _tileTransforms =
        new Dictionary<HexCoordinate, RectTransform>();

    private RectTransform _boardRoot;
    private RectTransform _playerMarker;
    private RectTransform _enemyMarker;
    private HexEncounterState _encounter;
    private Sprite _hexSprite;
    private Texture2D _hexTexture;

    public void Initialize(Canvas canvas, HexEncounterState encounter)
    {
        if (canvas == null)
        {
            throw new ArgumentNullException(nameof(canvas));
        }
        if (encounter == null)
        {
            throw new ArgumentNullException(nameof(encounter));
        }
        if (_boardRoot != null)
        {
            throw new InvalidOperationException("The hex board view has already been initialized.");
        }

        _encounter = encounter;
        _hexSprite = CreateHexSprite(out _hexTexture);
        _boardRoot = CreateRect(
            canvas.transform,
            "Hex Board",
            new Vector2(0f, 150f),
            new Vector2(600f, 390f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f));
        _boardRoot.SetAsFirstSibling();

        foreach (KeyValuePair<HexCoordinate, HexTerrain> cell in _encounter.Board.Cells)
        {
            CreateTile(cell.Key, cell.Value);
        }

        _playerMarker = CreateShipMarker("Player Ship", "P", new Color(0.18f, 0.82f, 0.82f, 1f));
        _enemyMarker = CreateShipMarker("Enemy Ship", "E", new Color(0.92f, 0.32f, 0.22f, 1f));
        SetShipMarker(_playerMarker, _encounter.Player.Position, _encounter.Player.Facing);
        SetShipMarker(_enemyMarker, _encounter.Enemy.Position, _encounter.Enemy.Facing);
    }

    public void ApplyResolvedAction(HexCommandResolvedAction action)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }
        if (_encounter == null)
        {
            throw new InvalidOperationException("The hex board view has not been initialized.");
        }

        if (action.EntityId == ((ANTK.Game.StateEngine.IStateEntity)_encounter.Player.Unit).EntityId)
        {
            SetShipMarker(
                _playerMarker,
                new HexCoordinate(action.ToQ, action.ToR),
                action.ToFacing);
        }
        else if (action.EntityId == ((ANTK.Game.StateEngine.IStateEntity)_encounter.Enemy.Unit).EntityId)
        {
            SetShipMarker(
                _enemyMarker,
                new HexCoordinate(action.ToQ, action.ToR),
                action.ToFacing);
        }
    }

    private void CreateTile(HexCoordinate coordinate, HexTerrain terrain)
    {
        GameObject tileObject = new GameObject(
            $"Hex {coordinate.Q},{coordinate.R}",
            typeof(RectTransform),
            typeof(Image));
        tileObject.transform.SetParent(_boardRoot, false);

        RectTransform tileRect = tileObject.GetComponent<RectTransform>();
        tileRect.sizeDelta = new Vector2(TileRadius * 1.76f, TileRadius * 2f);
        tileRect.anchoredPosition = GetPosition(coordinate);

        Image tileImage = tileObject.GetComponent<Image>();
        tileImage.sprite = _hexSprite;
        tileImage.color = terrain == HexTerrain.Water
            ? new Color(0.17f, 0.43f, 0.55f, 0.92f)
            : new Color(0.34f, 0.31f, 0.27f, 1f);
        tileImage.raycastTarget = false;

        _tileTransforms.Add(coordinate, tileRect);
    }

    private RectTransform CreateShipMarker(string objectName, string label, Color color)
    {
        GameObject markerObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(Image));
        markerObject.transform.SetParent(_boardRoot, false);

        RectTransform marker = markerObject.GetComponent<RectTransform>();
        marker.sizeDelta = new Vector2(42f, 42f);
        Image image = markerObject.GetComponent<Image>();
        image.sprite = _hexSprite;
        image.color = color;
        image.raycastTarget = false;

        GameObject labelObject = new GameObject(label, typeof(RectTransform));
        labelObject.transform.SetParent(marker, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        TextMeshProUGUI text = labelObject.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 24;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return marker;
    }

    private void SetShipMarker(RectTransform marker, HexCoordinate coordinate, int facing)
    {
        if (marker == null || !_tileTransforms.ContainsKey(coordinate))
        {
            return;
        }

        marker.anchoredPosition = GetPosition(coordinate);
        marker.localRotation = Quaternion.Euler(0f, 0f, -60f * facing);
    }

    private static Vector2 GetPosition(HexCoordinate coordinate)
    {
        float x = Mathf.Sqrt(3f) * TileRadius * (coordinate.Q + coordinate.R * 0.5f);
        float y = 1.5f * TileRadius * coordinate.R;
        return new Vector2(x, y);
    }

    private static Sprite CreateHexSprite(out Texture2D texture)
    {
        const int size = 64;
        texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        Color32[] pixels = new Color32[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float normalizedX = (x + 0.5f) / size * 2f - 1f;
                float normalizedY = (y + 0.5f) / size * 2f - 1f;
                float distance = Mathf.Abs(normalizedX) * 0.8660254f + Mathf.Abs(normalizedY) * 0.5f;
                pixels[y * size + x] = distance <= 0.87f
                    ? new Color32(255, 255, 255, 255)
                    : new Color32(255, 255, 255, 0);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
    }

    private static RectTransform CreateRect(
        Transform parent,
        string objectName,
        Vector2 position,
        Vector2 size,
        Vector2 anchorMin,
        Vector2 anchorMax)
    {
        GameObject child = new GameObject(objectName, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        RectTransform rect = child.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(
            (anchorMin.x + anchorMax.x) * 0.5f,
            (anchorMin.y + anchorMax.y) * 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private void OnDestroy()
    {
        if (_boardRoot != null)
        {
            Destroy(_boardRoot.gameObject);
        }
        if (_hexSprite != null)
        {
            Destroy(_hexSprite);
        }
        if (_hexTexture != null)
        {
            Destroy(_hexTexture);
        }
    }
}
