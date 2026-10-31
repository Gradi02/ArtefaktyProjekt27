using System;
using System.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

public sealed class TextPopupPresentationAnimation : PresentationAnimationBehaviour
{
    [SerializeField] private string eventTypeId = PresentationAnimationRegistry.FallbackEventTypeId;
    [SerializeField] private TMP_Text popupText;
    [SerializeField] private string messageTemplate = "{eventType}";
    [SerializeField, Min(0f)] private float riseDistance = 60f;
    [SerializeField, Min(0f)] private float entranceDuration = 0.18f;
    [SerializeField, Min(0f)] private float holdDuration = 0.45f;
    [SerializeField, Min(0f)] private float fadeDuration = 0.2f;
    [SerializeField, Min(0.01f)] private float entranceScale = 1.15f;
    [SerializeField, Min(0.01f)] private float exitScale = 0.9f;

    private Sequence _activeSequence;

    public override string EventTypeId => eventTypeId;

    public override async Task PlayAsync(
        PresentationEvent presentationEvent,
        PresentationPlaybackContext playbackContext)
    {
        if (presentationEvent == null)
        {
            throw new ArgumentNullException(nameof(presentationEvent));
        }
        if (playbackContext == null)
        {
            throw new ArgumentNullException(nameof(playbackContext));
        }
        if (popupText == null)
        {
            throw new InvalidOperationException(
                $"{nameof(TextPopupPresentationAnimation)} requires a TMP_Text reference.");
        }
        if (_activeSequence != null && _activeSequence.IsActive())
        {
            throw new InvalidOperationException("The text popup animation is already playing.");
        }

        RectTransform popupRect = popupText.rectTransform;
        Vector3 originalScale = popupRect.localScale;
        Vector2 originalPosition = popupRect.anchoredPosition;
        popupText.text = messageTemplate.Replace("{eventType}", presentationEvent.EventTypeId);
        popupText.alpha = 0f;
        popupRect.localScale = originalScale * entranceScale;
        popupRect.anchoredPosition = originalPosition;

        TaskCompletionSource<bool> completion = new TaskCompletionSource<bool>();
        bool completed = false;
        _activeSequence = DOTween.Sequence()
            .SetUpdate(true)
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
            .Append(popupRect.DOScale(originalScale, entranceDuration).SetEase(Ease.OutBack))
            .Join(popupText.DOFade(1f, entranceDuration))
            .Append(popupRect.DOAnchorPosY(originalPosition.y + riseDistance, entranceDuration).SetEase(Ease.OutCubic))
            .AppendInterval(holdDuration)
            .Append(popupText.DOFade(0f, fadeDuration))
            .Join(popupRect.DOScale(originalScale * exitScale, fadeDuration).SetEase(Ease.InCubic))
            .OnComplete(() =>
            {
                completed = true;
                completion.TrySetResult(true);
            })
            .OnKill(() =>
            {
                if (!completed)
                {
                    completion.TrySetException(
                        new InvalidOperationException("The text popup tween was killed before completion."));
                }
            });

        playbackContext.PlaybackStateChanged += ApplyPlaybackState;
        ApplyPlaybackState(playbackContext);
        try
        {
            await completion.Task;
        }
        finally
        {
            playbackContext.PlaybackStateChanged -= ApplyPlaybackState;
            _activeSequence = null;
        }
    }

    private void ApplyPlaybackState(PresentationPlaybackContext playbackContext)
    {
        if (_activeSequence == null || !_activeSequence.IsActive())
        {
            return;
        }

        _activeSequence.timeScale = playbackContext.Speed;
        if (playbackContext.IsPaused)
        {
            _activeSequence.Pause();
        }
        else
        {
            _activeSequence.Play();
        }
    }

    private void OnValidate()
    {
        riseDistance = Mathf.Max(0f, riseDistance);
        entranceDuration = Mathf.Max(0f, entranceDuration);
        holdDuration = Mathf.Max(0f, holdDuration);
        fadeDuration = Mathf.Max(0f, fadeDuration);
        entranceScale = Mathf.Max(0.01f, entranceScale);
        exitScale = Mathf.Max(0.01f, exitScale);
    }
}
