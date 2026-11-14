using System;
using System.Threading.Tasks;
using ANTK.Game.StateEngine.Actions;

public sealed class HexBoardPresentationAnimation : IActionPresentationAnimation
{
    private readonly HexBoardView _boardView;
    private readonly TimeSpan _stepDuration;

    public PresentationEventType EventType => PresentationEventType.HexCommandResolved;

    public HexBoardPresentationAnimation(HexBoardView boardView, TimeSpan stepDuration)
    {
        _boardView = boardView ?? throw new ArgumentNullException(nameof(boardView));
        if (stepDuration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(stepDuration));
        }

        _stepDuration = stepDuration;
    }

    public async Task PlayAsync(
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

        foreach (GameAction action in presentationEvent.Actions)
        {
            HexCommandResolvedAction resolvedAction = action as HexCommandResolvedAction;
            if (resolvedAction == null)
            {
                throw new InvalidOperationException(
                    "Hex-board presentation contains an unexpected action.");
            }

            _boardView.ApplyResolvedAction(resolvedAction);
            await playbackContext.DelayAsync(_stepDuration);
        }
    }
}
