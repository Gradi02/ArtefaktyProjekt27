using System;
using System.Threading.Tasks;
using ANTK.Game.StateEngine.Actions;
using UnityEngine;

public sealed class HandCardPresentationAnimation : PresentationAnimationBehaviour
{
    [SerializeField] private HandCardAnimator handCardAnimator;
    [SerializeField] private PresentationEventType eventType = PresentationEventType.CardsDrawn;

    public override PresentationEventType EventType => eventType;

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
        if (handCardAnimator == null)
        {
            throw new InvalidOperationException("Hand card presentation requires a HandCardAnimator.");
        }

        if (eventType == PresentationEventType.CardsDrawn)
        {
            foreach (GameAction action in presentationEvent.Actions)
            {
                CardDrawnAction cardDrawn = action as CardDrawnAction;
                if (cardDrawn == null)
                {
                    throw new InvalidOperationException("Card draw presentation contains an unexpected action.");
                }

                await handCardAnimator.AddCardAsync(cardDrawn, playbackContext);
            }
            return;
        }

        if (eventType == PresentationEventType.CardPlayed)
        {
            foreach (GameAction action in presentationEvent.Actions)
            {
                CardPlayedAction cardPlayed = action as CardPlayedAction;
                if (cardPlayed == null)
                {
                    throw new InvalidOperationException("Card play presentation contains an unexpected action.");
                }

                await handCardAnimator.PlayCardAsync(cardPlayed.CardInstanceId, playbackContext);
            }
            return;
        }

        throw new InvalidOperationException(
            $"Hand card presentation does not support event '{eventType}'.");
    }

    private void OnValidate()
    {
        if (eventType != PresentationEventType.CardsDrawn &&
            eventType != PresentationEventType.CardPlayed)
        {
            eventType = PresentationEventType.CardsDrawn;
        }
    }
}
