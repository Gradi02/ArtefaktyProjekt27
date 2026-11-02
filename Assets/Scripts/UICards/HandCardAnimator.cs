using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class HandCardAnimator : MonoBehaviour
{
    [Header("Cards")]
    [SerializeField] private RectTransform handRoot;
    [SerializeField, Min(1)] private int initialPlaceholderCount = 5;
    [SerializeField] private Vector2 cardSize = new Vector2(170f, 245f);

    [Header("Hand Layout")]
    [SerializeField] private float cardSpacing = 125f;
    [SerializeField] private float cardBottomPadding = 24f;
    [SerializeField] private float fanHeight = 24f;
    [SerializeField] private float sideRotation = 8f;
    [SerializeField] private float selectedLift = 28f;
    [SerializeField] private float selectedScale = 1.08f;
    [SerializeField] private float layoutDuration = 0.22f;

    [Header("Input and Animation")]
    [SerializeField] private float playDragDistance = 100f;
    [SerializeField] private float playAnimationDuration = 0.3f;
    [SerializeField] private float entranceDelay = 0.08f;
    [SerializeField] private float entranceDelayBetweenCards = 0.08f;

    private readonly List<RectTransform> items = new List<RectTransform>();
    private readonly List<int> cardIds = new List<int>();

    private Canvas parentCanvas;
    private RectTransform draggedCard;
    private RectTransform selectedCard;
    private Sequence entranceSequence;
    private Vector2 dragStartPointer;
    private Vector2 dragStartPosition;
    private int nextCardId = 1;
    private bool isInputBlocked;

    public event Action<int> CardPlayed;

    public int CardCount => items.Count;

    private void Awake()
    {
        if (handRoot == null)
        {
            handRoot = GetComponent<RectTransform>();
        }

        parentCanvas = GetComponentInParent<Canvas>();
        if (handRoot == null || parentCanvas == null)
        {
            Debug.LogError("HandCardAnimator requires a RectTransform under a Canvas.", this);
            enabled = false;
            return;
        }

        for (int i = items.Count - 1; i >= 0; i--)
        {
            if (items[i] == null)
            {
                items.RemoveAt(i);
            }
        }

        foreach (RectTransform item in items)
        {
            EnsureCardComponents(item);
        }

        if (items.Count == 0)
        {
            for (int i = 0; i < initialPlaceholderCount; i++)
            {
                CreatePlaceholderCard();
            }
        }
    }

    private void OnEnable()
    {
        if (handRoot != null && items.Count > 0)
        {
            AnimateEntrance();
        }
    }

    private void OnDisable()
    {
        if (entranceSequence != null)
        {
            entranceSequence.Kill();
            entranceSequence = null;
        }

        foreach (RectTransform item in items)
        {
            if (item != null)
            {
                item.DOKill();
            }
        }

        draggedCard = null;
        selectedCard = null;
        isInputBlocked = false;
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || isInputBlocked)
        {
            return;
        }

        Vector2 pointerPosition = mouse.position.ReadValue();
        if (mouse.leftButton.wasPressedThisFrame)
        {
            BeginPointerInteraction(pointerPosition);
        }

        if (draggedCard != null && mouse.leftButton.isPressed)
        {
            DragCard(pointerPosition);
        }

        if (draggedCard != null && mouse.leftButton.wasReleasedThisFrame)
        {
            EndPointerInteraction(pointerPosition);
        }
    }

    public void AddPlaceholderCard()
    {
        CreatePlaceholderCard();
        if (!isInputBlocked)
        {
            ApplyCardLayout(true);
        }
    }

    private void CreatePlaceholderCard()
    {
        int cardId = nextCardId++;
        GameObject cardObject = new GameObject(
            $"Placeholder Card {cardId:00}",
            typeof(RectTransform),
            typeof(CanvasGroup),
            typeof(Image));
        RectTransform card = cardObject.GetComponent<RectTransform>();
        card.SetParent(handRoot, false);
        card.anchorMin = new Vector2(0.5f, 0f);
        card.anchorMax = new Vector2(0.5f, 0f);
        card.pivot = new Vector2(0.5f, 0.5f);
        card.sizeDelta = cardSize;

        Image border = cardObject.GetComponent<Image>();
        border.color = new Color(0.12f, 0.14f, 0.2f);
        border.raycastTarget = false;

        Image face = CreateCardImage(
            card,
            "Card Face",
            cardSize - new Vector2(12f, 12f),
            Vector2.zero,
            new Color(0.91f, 0.87f, 0.74f));
        Color accent = GetPlaceholderAccent(cardId);
        CreateCardImage(
            face.rectTransform,
            "Title Placeholder",
            new Vector2(cardSize.x - 42f, 16f),
            new Vector2(0f, cardSize.y * 0.34f),
            accent);
        CreateCardImage(
            face.rectTransform,
            "Artwork Placeholder",
            new Vector2(72f, 82f),
            new Vector2(0f, 5f),
            accent * 0.72f);
        CreateCardImage(
            face.rectTransform,
            "Description Placeholder",
            new Vector2(cardSize.x - 48f, 10f),
            new Vector2(0f, -cardSize.y * 0.34f),
            new Color(0.54f, 0.5f, 0.42f));

        items.Add(card);
        cardIds.Add(cardId);
    }

    private static Image CreateCardImage(
        RectTransform parent,
        string objectName,
        Vector2 size,
        Vector2 position,
        Color color)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        RectTransform rectTransform = imageObject.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = size;

        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private void EnsureCardComponents(RectTransform card)
    {
        if (card.GetComponent<CanvasGroup>() == null)
        {
            card.gameObject.AddComponent<CanvasGroup>();
        }

        Image face = card.GetComponent<Image>();
        if (face == null)
        {
            face = card.gameObject.AddComponent<Image>();
        }
        face.raycastTarget = false;

        cardIds.Add(nextCardId++);
    }

    private void BeginPointerInteraction(Vector2 pointerPosition)
    {
        RectTransform card = GetCardAt(pointerPosition);
        if (card == null)
        {
            return;
        }

        selectedCard = card;
        draggedCard = card;
        draggedCard.DOKill();
        draggedCard.SetAsLastSibling();
        ApplyCardLayout(false);

        dragStartPointer = pointerPosition;
        dragStartPosition = draggedCard.anchoredPosition;
    }

    private RectTransform GetCardAt(Vector2 pointerPosition)
    {
        RectTransform topmostCard = null;
        int highestSiblingIndex = -1;
        Camera eventCamera = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : parentCanvas.worldCamera;

        foreach (RectTransform item in items)
        {
            if (item != null &&
                item.gameObject.activeInHierarchy &&
                item.GetSiblingIndex() > highestSiblingIndex &&
                RectTransformUtility.RectangleContainsScreenPoint(item, pointerPosition, eventCamera))
            {
                topmostCard = item;
                highestSiblingIndex = item.GetSiblingIndex();
            }
        }

        return topmostCard;
    }

    private void DragCard(Vector2 pointerPosition)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                handRoot,
                pointerPosition,
                GetEventCamera(),
                out Vector2 localPointer))
        {
            return;
        }

        if (draggedCard == null)
        {
            return;
        }

        Vector2 startLocalPointer;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                handRoot,
                dragStartPointer,
                GetEventCamera(),
                out startLocalPointer))
        {
            return;
        }

        draggedCard.anchoredPosition = dragStartPosition + localPointer - startLocalPointer;
    }

    private void EndPointerInteraction(Vector2 pointerPosition)
    {
        if (draggedCard == null)
        {
            return;
        }

        float dragDistance = pointerPosition.y - dragStartPointer.y;
        RectTransform releasedCard = draggedCard;
        draggedCard = null;

        if (dragDistance >= playDragDistance)
        {
            PlayCard(releasedCard);
        }
        else
        {
            ApplyCardLayout(true);
        }
    }

    private void PlayCard(RectTransform card)
    {
        int index = items.IndexOf(card);
        if (index < 0)
        {
            return;
        }

        int cardId = cardIds[index];
        items.RemoveAt(index);
        cardIds.RemoveAt(index);
        if (selectedCard == card)
        {
            selectedCard = null;
        }

        CanvasGroup canvasGroup = card.GetComponent<CanvasGroup>();
        Sequence playSequence = DOTween.Sequence();
        playSequence.Join(card.DOAnchorPosY(card.anchoredPosition.y + cardSize.y, playAnimationDuration)
            .SetEase(Ease.InCubic));
        playSequence.Join(card.DOScale(0.75f, playAnimationDuration));
        playSequence.Join(canvasGroup.DOFade(0f, playAnimationDuration));
        playSequence.OnComplete(() => Destroy(card.gameObject));

        ApplyCardLayout(true);
        Debug.Log($"Played placeholder card {cardId}.", this);
        CardPlayed?.Invoke(cardId);
    }

    private void AnimateEntrance()
    {
        if (items.Count == 0)
        {
            return;
        }

        isInputBlocked = true;
        if (entranceSequence != null)
        {
            entranceSequence.Kill();
        }

        for (int i = 0; i < items.Count; i++)
        {
            RectTransform card = items[i];
            card.DOKill();
            Vector2 targetPosition = GetCardPosition(i);
            card.anchoredPosition = targetPosition + Vector2.down * 80f;
            card.localRotation = Quaternion.Euler(0f, 0f, GetCardRotation(i));
            card.localScale = Vector3.zero;
            card.GetComponent<CanvasGroup>().alpha = 0f;
        }

        entranceSequence = DOTween.Sequence();
        entranceSequence.AppendInterval(entranceDelay);
        for (int i = 0; i < items.Count; i++)
        {
            RectTransform card = items[i];
            float insertTime = entranceDelay + i * entranceDelayBetweenCards;
            entranceSequence.Insert(
                insertTime,
                card.DOAnchorPos(GetCardPosition(i), 0.35f).SetEase(Ease.OutCubic));
            entranceSequence.Insert(
                insertTime,
                card.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack));
            entranceSequence.Insert(
                insertTime,
                card.GetComponent<CanvasGroup>().DOFade(1f, 0.2f));
        }

        entranceSequence.OnComplete(() =>
        {
            isInputBlocked = false;
            ApplyCardLayout(false);
        });
    }

    private void ApplyCardLayout(bool animate)
    {
        for (int i = 0; i < items.Count; i++)
        {
            RectTransform card = items[i];
            if (card == null)
            {
                continue;
            }

            bool isSelected = card == selectedCard;
            Vector2 targetPosition = GetCardPosition(i);
            if (isSelected)
            {
                targetPosition.y += selectedLift;
            }

            Quaternion targetRotation = Quaternion.Euler(0f, 0f, GetCardRotation(i));
            Vector3 targetScale = Vector3.one * (isSelected ? selectedScale : 1f);
            card.DOKill();

            if (animate)
            {
                card.DOAnchorPos(targetPosition, layoutDuration).SetEase(Ease.OutCubic);
                card.DOLocalRotateQuaternion(targetRotation, layoutDuration).SetEase(Ease.OutCubic);
                card.DOScale(targetScale, layoutDuration).SetEase(Ease.OutCubic);
            }
            else
            {
                card.anchoredPosition = targetPosition;
                card.localRotation = targetRotation;
                card.localScale = targetScale;
            }
        }
    }

    private Vector2 GetCardPosition(int index)
    {
        float spacing = cardSpacing;
        if (items.Count > 1)
        {
            float availableWidth = Mathf.Max(0f, handRoot.rect.width - cardSize.x);
            spacing = Mathf.Min(spacing, availableWidth / (items.Count - 1));
        }

        float offset = index - (items.Count - 1) * 0.5f;
        float x = offset * spacing;
        float y = cardBottomPadding + cardSize.y * 0.5f +
            Mathf.Max(0f, 1f - Mathf.Abs(offset) / Mathf.Max(1f, items.Count * 0.5f)) * fanHeight;
        return new Vector2(x, y);
    }

    private float GetCardRotation(int index)
    {
        float offset = index - (items.Count - 1) * 0.5f;
        return -offset * sideRotation;
    }

    private Camera GetEventCamera()
    {
        return parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : parentCanvas.worldCamera;
    }

    private static Color GetPlaceholderAccent(int cardId)
    {
        Color[] colors =
        {
            new Color(0.68f, 0.26f, 0.2f),
            new Color(0.22f, 0.43f, 0.56f),
            new Color(0.36f, 0.52f, 0.31f),
            new Color(0.65f, 0.46f, 0.2f),
            new Color(0.47f, 0.34f, 0.57f)
        };
        return colors[(cardId - 1) % colors.Length];
    }
}
