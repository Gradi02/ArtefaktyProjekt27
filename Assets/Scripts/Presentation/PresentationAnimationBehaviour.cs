using UnityEngine;

public abstract class PresentationAnimationBehaviour : MonoBehaviour, IActionPresentationAnimation
{
    public abstract string EventTypeId { get; }

    public abstract System.Threading.Tasks.Task PlayAsync(
        PresentationEvent presentationEvent,
        PresentationPlaybackContext playbackContext);
}
