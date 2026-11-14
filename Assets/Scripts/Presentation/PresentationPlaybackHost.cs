using System;
using UnityEngine;

public sealed class PresentationPlaybackHost : MonoBehaviour
{
    [SerializeField] private BattleSceneController battleSceneController;
    [SerializeField] private PresentationAnimationBehaviour[] animations;

    private PresentationPlayer _presentationPlayer;
    private PresentationAnimationRegistry _animationRegistry;

    public bool IsPaused => _presentationPlayer != null && _presentationPlayer.IsPaused;
    public float PlaybackSpeed
    {
        get => _presentationPlayer == null ? 1f : _presentationPlayer.Speed;
        set
        {
            if (_presentationPlayer == null)
            {
                throw new InvalidOperationException("Presentation player has not been initialized.");
            }

            _presentationPlayer.Speed = value;
        }
    }

    private void Awake()
    {
        if (battleSceneController == null)
        {
            Debug.LogError("PresentationPlaybackHost requires a BattleSceneController.", this);
            enabled = false;
            return;
        }

        _animationRegistry = new PresentationAnimationRegistry();
        if (animations != null)
        {
            foreach (PresentationAnimationBehaviour animation in animations)
            {
                if (animation == null)
                {
                    Debug.LogError("Presentation animation list contains an empty reference.", this);
                    enabled = false;
                    return;
                }

                _animationRegistry.Register(animation.EventType, animation);
            }
        }

        _presentationPlayer = new PresentationPlayer(
            battleSceneController.PresentationQueue,
            _animationRegistry);
        battleSceneController.AttachPresentationPlayer(_presentationPlayer);
    }

    public void RegisterAnimation(IActionPresentationAnimation animation)
    {
        if (_animationRegistry == null)
        {
            throw new InvalidOperationException("Presentation animations have not been initialized.");
        }

        _animationRegistry.Register(animation.EventType, animation);
    }

    public void Pause()
    {
        RequirePlayer().Pause();
    }

    public void Resume()
    {
        RequirePlayer().Resume();
    }

    private PresentationPlayer RequirePlayer()
    {
        if (_presentationPlayer == null)
        {
            throw new InvalidOperationException("Presentation player has not been initialized.");
        }

        return _presentationPlayer;
    }
}
