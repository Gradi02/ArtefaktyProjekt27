using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DG.Tweening;
using ANTK.Game.StateEngine.Actions;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class HandCardAnimator : MonoBehaviour
{
    [Header("Cards")]
    [SerializeField] private RectTransform handRoot;
    [SerializeField] private CardView cardPrefab; // <-- TUTAJ PRZYPISUJESZ SWÓJ PREFAB W INSPEKTORZE
    [SerializeField, Min(0)] private int initialPlaceholderCount;
    [SerializeField] private Vector2 cardSize = new Vector2(170f, 245f);

    [Header("Hand Layout")]
    [SerializeField] private float cardSpacing = 125f;
    [SerializeField] private float cardBottomPadding = 24f;
    [SerializeField] private float fanHeight = 24f;
    [SerializeField] private float sideRotation = 8f;
    [SerializeField] private float selectedLift = 28f;
    [SerializeField] private float selectedScale = 1.08f;
    [SerializeField] private Vector2 queuedCardStackOffset = new Vector2(10f, -10f);
    [SerializeField] private float layoutDuration = 0.22f;

    [Header("Input and Animation")]
    [SerializeField] private float playDragDistance = 100f;
    [SerializeField] private float playAnimationDuration = 0.3f;
    [SerializeField] private float entranceDelay = 0.08f;
    [SerializeField] private float entranceDelayBetweenCards = 0.08f;

    private readonly List<RectTransform> items = new List<RectTransform>();
    private readonly List<string> cardInstanceIds = new List<string>();
    private readonly List<string> queuedCardInstanceIds = new List<string>();

    private Canvas parentCanvas;
    private RectTransform draggedCard;
    private RectTransform selectedCard;
    private RectTransform activePlayedCard;
    private Sequence entranceSequence;
    private Vector2 dragStartPointer;
    private Vector2 dragStartPosition;
    private int nextCardId = 1;
    private bool isInputBlocked;
    private bool isInputDisabledByUi;

    public event Func<string, bool> CardPlayRequested;

    public int CardCount => items.Count;

    public void SetCardQueued(string cardInstanceId, bool isQueued)
    {
        if (string.IsNullOrWhiteSpace(cardInstanceId))
        {
            throw new ArgumentException("Card instance ID must not be empty.", nameof(cardInstanceId));
        }

        if (isQueued)
        {
            if (!queuedCardInstanceIds.Contains(cardInstanceId))
            {
                queuedCardInstanceIds.Add(cardInstanceId);
            }
        }
        else
        {
            queuedCardInstanceIds.Remove(cardInstanceId);
        }

        ApplyCardLayout(true);
        ApplyQueuedCardLayout(true);
    }

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
        if (mouse == null || isInputBlocked || isInputDisabledByUi)
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

    public void SetUiInputEnabled(bool isEnabled)
    {
        isInputDisabledByUi = !isEnabled;
    }

    private void CreatePlaceholderCard()
    {
        if (cardPrefab == null)
        {
            Debug.LogError("Card Prefab is not assigned in HandCardAnimator!", this);
            return;
        }

        int cardId = nextCardId++;
        CardView cardView = Instantiate(cardPrefab, handRoot);
        cardView.name = $"Placeholder Card {cardId:00}";
        cardView.SetupPlaceholder(cardId);

        RectTransform card = cardView.RectTransform;
        card.anchorMin = new Vector2(0.5f, 0f);
        card.anchorMax = new Vector2(0.5f, 0f);
        card.pivot = new Vector2(0.5f, 0.5f);
        card.sizeDelta = cardSize;
        cardView.CanvasGroup.alpha = 0f;

        items.Add(card);
        cardInstanceIds.Add($"placeholder-{cardId}");
    }

    private void EnsureCardComponents(RectTransform card)
    {
        if (card.GetComponent<CanvasGroup>() == null)
        {
            card.gameObject.AddComponent<CanvasGroup>();
        }

        cardInstanceIds.Add($"placeholder-{nextCardId++}");
    }

    public async Task AddCardAsync(
        CardDrawnAction cardAction,
        PresentationPlaybackContext playbackContext)
    {
        if (cardAction == null)
        {
            throw new ArgumentNullException(nameof(cardAction));
        }
        if (playbackContext == null)
        {
            throw new ArgumentNullException(nameof(playbackContext));
        }
        if (cardInstanceIds.Contains(cardAction.CardInstanceId))
        {
            throw new InvalidOperationException(
                $"Card '{cardAction.CardInstanceId}' is already displayed in the hand.");
        }

        RectTransform card = CreateCard(
            cardAction.CardTitle,
            cardAction.CardType,
            cardAction.CardCost);

        ApplyCardLayout(true);
        items.Add(card);
        cardInstanceIds.Add(cardAction.CardInstanceId);

        Sequence sequence = DOTween.Sequence()
            .SetUpdate(true)
            .SetLink(card.gameObject, LinkBehaviour.KillOnDestroy);
        sequence.Join(card.DOAnchorPos(GetCardPosition(items.Count - 1), 0.35f).SetEase(Ease.OutCubic));
        sequence.Join(card.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack));
        sequence.Join(card.GetComponent<CanvasGroup>().DOFade(1f, 0.2f));
        await PlaySequenceAsync(sequence, playbackContext, null, () =>
        {
            int cardIndex = items.IndexOf(card);
            if (cardIndex >= 0)
            {
                items.RemoveAt(cardIndex);
                cardInstanceIds.RemoveAt(cardIndex);
            }
            if (card != null)
            {
                Destroy(card.gameObject);
            }
        });
        ApplyCardLayout(true);
    }

    public async Task PlayCardAsync(
        string cardInstanceId,
        PresentationPlaybackContext playbackContext)
    {
        if (string.IsNullOrWhiteSpace(cardInstanceId))
        {
            throw new ArgumentException("Card instance ID must not be empty.", nameof(cardInstanceId));
        }
        if (playbackContext == null)
        {
            throw new ArgumentNullException(nameof(playbackContext));
        }

        int index = cardInstanceIds.IndexOf(cardInstanceId);
        if (index < 0)
        {
            throw new InvalidOperationException(
                $"Played card '{cardInstanceId}' is not currently displayed in the hand.");
        }

        RectTransform card = items[index];
        Vector2 originalPosition = card.anchoredPosition;
        Vector2 playTargetPosition = GetScreenCenterInHandSpace(card);
        queuedCardInstanceIds.Remove(cardInstanceId);
        items.RemoveAt(index);
        cardInstanceIds.RemoveAt(index);
        if (selectedCard == card)
        {
            selectedCard = null;
        }
        card.SetAsLastSibling();
        activePlayedCard = card;
        ApplyCardLayout(true);
        ApplyQueuedCardLayout(true);

        CanvasGroup canvasGroup = card.GetComponent<CanvasGroup>();
        Sequence sequence = DOTween.Sequence()
            .SetUpdate(true)
            .SetLink(card.gameObject, LinkBehaviour.KillOnDestroy);
        sequence.Join(card.DOAnchorPos(playTargetPosition, playAnimationDuration).SetEase(Ease.InCubic));
        sequence.Join(card.DOScale(0.75f, playAnimationDuration));
        sequence.Join(canvasGroup.DOFade(0f, playAnimationDuration));
        try
        {
            await PlaySequenceAsync(
                sequence,
                playbackContext,
                () =>
                {
                    if (card != null)
                    {
                        Destroy(card.gameObject);
                    }
                },
                () =>
                {
                    if (card != null)
                    {
                        items.Insert(Mathf.Min(index, items.Count), card);
                        cardInstanceIds.Insert(Mathf.Min(index, cardInstanceIds.Count), cardInstanceId);
                        card.anchoredPosition = originalPosition;
                        canvasGroup.alpha = 1f;
                        ApplyCardLayout(true);
                    }
                });
        }
        finally
        {
            if (activePlayedCard == card)
            {
                activePlayedCard = null;
                ApplyQueuedCardLayout(false);
            }
        }
    }

    private RectTransform CreateCard(string title, string cardType, int cost, Sprite icon = null)
    {
        if (cardPrefab == null)
        {
            throw new InvalidOperationException("Card Prefab is not assigned in HandCardAnimator!");
        }

        CardView cardView = Instantiate(cardPrefab, handRoot);
        cardView.name = $"Card - {title}";
        cardView.Setup(title, cardType, cost, icon);

        RectTransform card = cardView.RectTransform;
        card.anchorMin = new Vector2(0.5f, 0f);
        card.anchorMax = new Vector2(0.5f, 0f);
        card.pivot = new Vector2(0.5f, 0.5f);
        card.sizeDelta = cardSize;
        card.anchoredPosition = GetCardPosition(items.Count);
        card.localScale = Vector3.zero;

        cardView.CanvasGroup.alpha = 0f;

        return card;
    }

    private static async Task PlaySequenceAsync(
        Sequence sequence,
        PresentationPlaybackContext playbackContext,
        Action onComplete,
        Action onInterrupted)
    {
        TaskCompletionSource<bool> completion = new TaskCompletionSource<bool>();
        bool completed = false;
        sequence.OnComplete(() =>
        {
            completed = true;
            onComplete?.Invoke();
            completion.TrySetResult(true);
        });
        sequence.OnKill(() =>
        {
            if (!completed)
            {
                onInterrupted?.Invoke();
                completion.TrySetException(
                    new InvalidOperationException("The card animation was killed before completion."));
            }
        });

        void ApplyPlaybackState(PresentationPlaybackContext context)
        {
            if (!sequence.IsActive())
            {
                return;
            }

            sequence.timeScale = context.Speed;
            if (context.IsPaused)
            {
                sequence.Pause();
            }
            else
            {
                sequence.Play();
            }
        }

        playbackContext.PlaybackStateChanged += ApplyPlaybackState;
        ApplyPlaybackState(playbackContext);
        try
        {
            await completion.Task;
        }
        finally
        {
            playbackContext.PlaybackStateChanged -= ApplyPlaybackState;
        }
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

        for (int i = 0; i < items.Count; i++)
        {
            RectTransform item = items[i];
            if (queuedCardInstanceIds.Contains(cardInstanceIds[i]))
            {
                continue;
            }

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

        string cardInstanceId = cardInstanceIds[index];
        try
        {
            CardPlayRequested?.Invoke(cardInstanceId);
        }
        catch
        {
            ApplyCardLayout(true);
            throw;
        }

        if (selectedCard == card)
        {
            selectedCard = null;
        }
        ApplyCardLayout(true);
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
        int handCardCount = 0;
        for (int i = 0; i < items.Count; i++)
        {
            if (!queuedCardInstanceIds.Contains(cardInstanceIds[i]))
            {
                handCardCount++;
            }
        }

        int handIndex = 0;
        for (int i = 0; i < items.Count; i++)
        {
            RectTransform card = items[i];
            if (card == null)
            {
                continue;
            }

            bool isQueued = queuedCardInstanceIds.Contains(cardInstanceIds[i]);
            if (isQueued)
            {
                continue;
            }

            bool isSelected = card == selectedCard;
            Vector2 targetPosition = GetCardPosition(handIndex, handCardCount);
            handIndex++;
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

    private void ApplyQueuedCardLayout(bool animate)
    {
        for (int queuedIndex = queuedCardInstanceIds.Count - 1; queuedIndex >= 0; queuedIndex--)
        {
            int cardIndex = cardInstanceIds.IndexOf(queuedCardInstanceIds[queuedIndex]);
            if (cardIndex < 0)
            {
                continue;
            }

            RectTransform card = items[cardIndex];
            Vector2 targetPosition = GetScreenCenterInHandSpace(card) +
                queuedCardStackOffset * (queuedIndex + 1);
            if (activePlayedCard != null)
            {
                card.SetSiblingIndex(activePlayedCard.GetSiblingIndex());
            }
            else
            {
                card.SetAsLastSibling();
            }
            card.DOKill();
            if (animate)
            {
                card.DOAnchorPos(targetPosition, layoutDuration).SetEase(Ease.OutCubic);
            }
            else
            {
                card.anchoredPosition = targetPosition;
            }
        }
    }

    private Vector2 GetScreenCenterInHandSpace(RectTransform card)
    {
        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                handRoot,
                screenCenter,
                GetEventCamera(),
                out Vector2 centerPosition))
        {
            throw new InvalidOperationException("The screen center could not be mapped to the card hand.");
        }

        Vector2 anchor = card.anchorMin +
            Vector2.Scale(card.anchorMax - card.anchorMin, card.pivot);
        Vector2 anchorPosition = handRoot.rect.min +
            Vector2.Scale(handRoot.rect.size, anchor);
        return centerPosition - anchorPosition;
    }

    private Vector2 GetCardPosition(int index)
    {
        return GetCardPosition(index, items.Count);
    }

    private Vector2 GetCardPosition(int index, int cardCount)
    {
        float spacing = cardSpacing;
        if (cardCount > 1)
        {
            float availableWidth = Mathf.Max(0f, handRoot.rect.width - cardSize.x);
            spacing = Mathf.Min(spacing, availableWidth / (cardCount - 1));
        }

        float offset = index - (cardCount - 1) * 0.5f;
        float x = offset * spacing;
        float y = cardBottomPadding + cardSize.y * 0.5f +
            Mathf.Max(0f, 1f - Mathf.Abs(offset) / Mathf.Max(1f, cardCount * 0.5f)) * fanHeight;
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
}