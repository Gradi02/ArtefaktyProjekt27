using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public interface IActionPresentationAnimation
{
    string EventTypeId { get; }
    Task PlayAsync(PresentationEvent presentationEvent, PresentationPlaybackContext playbackContext);
}

public sealed class PresentationAnimationRegistry
{
    public const string FallbackEventTypeId = "*";

    private readonly Dictionary<string, IActionPresentationAnimation> _animations =
        new Dictionary<string, IActionPresentationAnimation>();

    public void Register(string eventTypeId, IActionPresentationAnimation animation)
    {
        if (string.IsNullOrWhiteSpace(eventTypeId))
        {
            throw new ArgumentException("Event type ID must not be empty.", nameof(eventTypeId));
        }
        if (animation == null)
        {
            throw new ArgumentNullException(nameof(animation));
        }
        if (_animations.ContainsKey(eventTypeId))
        {
            throw new InvalidOperationException($"An animation is already registered for '{eventTypeId}'.");
        }

        _animations.Add(eventTypeId, animation);
    }

    public IActionPresentationAnimation Resolve(string eventTypeId)
    {
        IActionPresentationAnimation animation;
        if (_animations.TryGetValue(eventTypeId, out animation))
        {
            return animation;
        }
        if (_animations.TryGetValue(FallbackEventTypeId, out animation))
        {
            return animation;
        }

        throw new InvalidOperationException(
            $"No presentation animation is registered for event '{eventTypeId}'.");
    }
}

public sealed class PresentationPlaybackContext
{
    private readonly object _pauseLock = new object();
    private TaskCompletionSource<bool> _resumeSignal;
    private float _speed = 1f;

    public event Action<PresentationPlaybackContext> PlaybackStateChanged;

    public bool IsPaused { get; private set; }

    public float Speed
    {
        get => _speed;
        set
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Playback speed must be positive and finite.");
            }

            _speed = value;
            PlaybackStateChanged?.Invoke(this);
        }
    }

    public void Pause()
    {
        lock (_pauseLock)
        {
            if (IsPaused)
            {
                return;
            }

            IsPaused = true;
            _resumeSignal = new TaskCompletionSource<bool>();
        }

        PlaybackStateChanged?.Invoke(this);
    }

    public void Resume()
    {
        TaskCompletionSource<bool> resumeSignal;
        lock (_pauseLock)
        {
            if (!IsPaused)
            {
                return;
            }

            IsPaused = false;
            resumeSignal = _resumeSignal;
            _resumeSignal = null;
        }

        resumeSignal.TrySetResult(true);
        PlaybackStateChanged?.Invoke(this);
    }

    public async Task DelayAsync(TimeSpan duration, CancellationToken cancellationToken = default(CancellationToken))
    {
        if (duration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration));
        }

        double remainingGameSeconds = duration.TotalSeconds;
        while (remainingGameSeconds > 0d)
        {
            await WaitUntilResumedAsync(cancellationToken);

            double speed = Speed;
            double realSeconds = Math.Min(0.05d, remainingGameSeconds / speed);
            await Task.Delay(TimeSpan.FromSeconds(realSeconds), cancellationToken);
            remainingGameSeconds -= realSeconds * Speed;
        }
    }

    private Task WaitUntilResumedAsync(CancellationToken cancellationToken)
    {
        Task resumeTask;
        lock (_pauseLock)
        {
            if (!IsPaused)
            {
                return Task.CompletedTask;
            }

            resumeTask = _resumeSignal.Task;
        }

        return WaitWithCancellationAsync(resumeTask, cancellationToken);
    }

    private static async Task WaitWithCancellationAsync(Task task, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        TaskCompletionSource<bool> cancellationSignal = new TaskCompletionSource<bool>();
        using (cancellationToken.Register(() => cancellationSignal.TrySetCanceled()))
        {
            Task completedTask = await Task.WhenAny(task, cancellationSignal.Task);
            await completedTask;
        }
    }
}

public sealed class PresentationPlayer
{
    private readonly PresentationEventQueue _queue;
    private readonly PresentationAnimationRegistry _animations;
    private readonly PresentationPlaybackContext _playbackContext = new PresentationPlaybackContext();
    private bool _isPlaying;

    public event Action<string> SnapshotPlaybackCompleted;

    public PresentationPlayer(PresentationEventQueue queue, PresentationAnimationRegistry animations)
    {
        _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        _animations = animations ?? throw new ArgumentNullException(nameof(animations));
    }

    public bool IsPlaying => _isPlaying;
    public PresentationEventQueue Queue => _queue;
    public bool IsPaused => _playbackContext.IsPaused;
    public float Speed
    {
        get => _playbackContext.Speed;
        set => _playbackContext.Speed = value;
    }

    public void Pause() => _playbackContext.Pause();
    public void Resume() => _playbackContext.Resume();

    public async Task PlayQueuedAsync()
    {
        if (_isPlaying)
        {
            throw new InvalidOperationException("Presentation playback is already running.");
        }

        _isPlaying = true;
        try
        {
            while (_queue.TryPeek(out PresentationEvent nextEvent))
            {
                IActionPresentationAnimation animation = _animations.Resolve(nextEvent.EventTypeId);
                await animation.PlayAsync(nextEvent, _playbackContext);

                if (!_queue.TryDequeue(out PresentationEvent completedEvent) ||
                    !ReferenceEquals(completedEvent, nextEvent))
                {
                    throw new InvalidOperationException("The presentation queue changed during playback.");
                }

                if (!_queue.HasNextForSnapshot(completedEvent.SnapshotId))
                {
                    SnapshotPlaybackCompleted?.Invoke(completedEvent.SnapshotId);
                }
            }
        }
        finally
        {
            _isPlaying = false;
        }
    }
}
