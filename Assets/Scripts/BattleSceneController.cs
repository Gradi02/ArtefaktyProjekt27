using ANTK.Game.StateEngine;
using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class BattleSceneController : MonoBehaviour
{
    [SerializeField] private Player scenePlayer;
    [SerializeField] private Enemy sceneEnemy;
    [SerializeField] private HandCardAnimator handCardAnimator;

    private RunManager _runManager;
    private FightManager _fightManager;
    private BattleHudController _battleHudController;
    private HexBoardView _hexBoardView;
    private readonly PresentationEventQueue _presentationQueue = new PresentationEventQueue();
    private PresentationDirector _presentationDirector;
    private PresentationPlayer _presentationPlayer;
    private FightEndedEventArgs _pendingFightEnd;
    private bool _isCompletingFight;
    private bool _hasPublishedFightEnded;

    public event System.EventHandler<FightEndedEventArgs> FightEnded;
    public event Action<PresentationEventQueue> PresentationQueueUpdated;

    public PresentationEventQueue PresentationQueue => _presentationQueue;

    public void AttachPresentationPlayer(PresentationPlayer presentationPlayer)
    {
        if (presentationPlayer == null)
        {
            throw new ArgumentNullException(nameof(presentationPlayer));
        }
        if (!ReferenceEquals(presentationPlayer.Queue, _presentationQueue))
        {
            throw new ArgumentException(
                "The PresentationPlayer must consume this battle's presentation queue.",
                nameof(presentationPlayer));
        }
        if (_presentationPlayer != null)
        {
            throw new InvalidOperationException("A presentation player is already attached.");
        }

        _presentationPlayer = presentationPlayer;
        _presentationPlayer.SnapshotPlaybackCompleted += OnSnapshotPlaybackCompleted;
        _presentationPlayer.PresentationEventCompleted += OnPresentationEventCompleted;
        if (!_presentationQueue.IsEmpty)
        {
            StartPresentationPlayback();
        }
    }

    private void Start()
    {
        EnsurePresentationDirector();

        GameManager gameManager = GameManager.Instance;
        if (gameManager == null || gameManager.runManager == null)
        {
            Debug.LogError("Battle scene loaded without an active RunManager.", this);
            return;
        }

        RunManager runManager = gameManager.runManager;
        if (runManager.HasSelectedCharacter && runManager.SelectedCharacter == null)
        {
            Debug.LogError("RunManager's selected player no longer exists in the loaded scenes.", this);
            return;
        }

        Player player = runManager.HasSelectedCharacter
            ? runManager.SelectedCharacter
            : scenePlayer;

        if (player == null || sceneEnemy == null)
        {
            Debug.LogError("Battle scene requires a player and enemy.", this);
            return;
        }

        LivingMixin playerState = player.GetComponent<LivingMixin>();
        LivingMixin enemyState = sceneEnemy.GetComponent<LivingMixin>();
        if (playerState == null || enemyState == null)
        {
            Debug.LogError("Player and enemy must each have a LivingMixin component.", this);
            return;
        }

        if (handCardAnimator == null)
        {
            Debug.LogError("Battle scene requires a HandCardAnimator.", this);
            return;
        }

        Canvas battleCanvas = handCardAnimator.GetComponentInParent<Canvas>();
        PresentationPlaybackHost presentationPlaybackHost = GetComponent<PresentationPlaybackHost>();
        if (battleCanvas == null || presentationPlaybackHost == null)
        {
            Debug.LogError(
                "Battle scene requires a Canvas for the hand and a PresentationPlaybackHost.",
                this);
            return;
        }

        if (runManager.RegisterBattleSceneController(this))
        {
            _runManager = runManager;
            HexBoard board = HexBoardGenerator.Generate(runManager.RunSeed);
            HexEncounterState encounter = new HexEncounterState(board, playerState, enemyState);
            _hexBoardView = gameObject.AddComponent<HexBoardView>();
            _hexBoardView.Initialize(battleCanvas, encounter);
            presentationPlaybackHost.RegisterAnimation(
                new HexBoardPresentationAnimation(
                    _hexBoardView,
                    TimeSpan.FromSeconds(0.18d)));
            _fightManager = new FightManager(
                playerState,
                enemyState,
                runManager.StateEngine,
                runManager.CardCollection,
                player.BaseCardsDrawnAtTurnStart,
                player.ProgrammingCapacity,
                encounter);
            handCardAnimator.CardQueueRequested += OnCardQueueRequested;
            _fightManager.FightEnded += OnFightEnded;
            _fightManager.ActionSnapshotCreated += OnActionSnapshotCreated;
            _battleHudController = gameObject.AddComponent<BattleHudController>();
            _battleHudController.Initialize(
                battleCanvas,
                _fightManager,
                handCardAnimator,
                presentationPlaybackHost);
            _fightManager.StartFight();
            _battleHudController.Refresh();
        }
    }

    private void OnDestroy()
    {
        if (_fightManager != null)
        {
            _fightManager.FightEnded -= OnFightEnded;
            _fightManager.ActionSnapshotCreated -= OnActionSnapshotCreated;
            _fightManager.Dispose();
        }
        if (handCardAnimator != null)
        {
            handCardAnimator.CardQueueRequested -= OnCardQueueRequested;
        }
        if (_presentationPlayer != null)
        {
            _presentationPlayer.SnapshotPlaybackCompleted -= OnSnapshotPlaybackCompleted;
            _presentationPlayer.PresentationEventCompleted -= OnPresentationEventCompleted;
        }
    }

    [ContextMenu("Test/Win Fight")]
    public void WinFight()
    {
        EndFight(wasWon: true);
    }

    [ContextMenu("Test/Lose Fight")]
    public void LoseFight()
    {
        EndFight(wasWon: false);
    }

    public void CompleteFightAfterPresentation()
    {
        if (_isCompletingFight)
        {
            return;
        }
        if (_pendingFightEnd == null)
        {
            Debug.LogWarning("There is no pending fight-end event to complete.", this);
            return;
        }

        if (_runManager == null)
        {
            Debug.LogError("Battle scene controller has not been connected to a RunManager.", this);
            return;
        }

        _isCompletingFight = _runManager.EndFight(this, _pendingFightEnd.WasWon);
    }

    public bool CompleteActionPresentation(string snapshotId)
    {
        if (_fightManager == null)
        {
            Debug.LogError("Fight manager is not available.", this);
            return false;
        }
        if (_presentationQueue.HasNextForSnapshot(snapshotId))
        {
            Debug.LogWarning($"Snapshot '{snapshotId}' still has queued presentation events.", this);
            return false;
        }

        return _fightManager.AcknowledgePresentationCompleted(snapshotId);
    }

    private void OnActionSnapshotCreated(object sender, GameActionSnapshotEventArgs eventArgs)
    {
        if (_battleHudController != null)
        {
            _battleHudController.Refresh();
        }
        _presentationDirector.ProcessSnapshot(eventArgs.Snapshot);
        PresentationQueueUpdated?.Invoke(_presentationQueue);
        if (_presentationPlayer == null)
        {
            Debug.LogWarning(
                "No PresentationPlayer is attached; gameplay remains locked until the snapshot is played.",
                this);
            return;
        }

        StartPresentationPlayback();
    }

    private bool OnCardQueueRequested(string cardInstanceId)
    {
        if (_fightManager == null)
        {
            return false;
        }

        handCardAnimator.SetCardQueued(cardInstanceId, true);
        bool accepted = _fightManager.TryQueuePlayerCard(cardInstanceId);
        if (!accepted)
        {
            handCardAnimator.SetCardQueued(cardInstanceId, false);
        }
        else if (_battleHudController != null)
        {
            _battleHudController.Refresh();
        }

        return accepted;
    }

    private async void StartPresentationPlayback()
    {
        if (_presentationPlayer.IsPlaying)
        {
            return;
        }

        try
        {
            await _presentationPlayer.PlayQueuedAsync();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
        }
    }

    private void OnSnapshotPlaybackCompleted(string snapshotId)
    {
        CompleteActionPresentation(snapshotId);
        if (_battleHudController != null)
        {
            _battleHudController.Refresh();
        }
    }

    private void OnPresentationEventCompleted(PresentationEvent presentationEvent)
    {
        foreach (ANTK.Game.StateEngine.Actions.GameAction action in presentationEvent.Actions)
        {
            if (action is ANTK.Game.StateEngine.Actions.FightWonAction)
            {
                WinFight();
                PublishFightEnded();
                return;
            }
            if (action is ANTK.Game.StateEngine.Actions.PlayerDiedAction)
            {
                LoseFight();
                PublishFightEnded();
                return;
            }
        }
    }

    private void EnsurePresentationDirector()
    {
        if (_presentationDirector == null)
        {
            List<IActionPresentationRule> rules = new List<IActionPresentationRule>
            {
                new ConsecutiveCardDrawsRule()
            };
            _presentationDirector = new PresentationDirector(_presentationQueue, rules);
        }
    }

    private void OnFightEnded(object sender, FightEndedEventArgs eventArgs)
    {
        _pendingFightEnd = eventArgs;
        if (_battleHudController != null)
        {
            _battleHudController.Refresh();
        }
        Debug.Log(
            $"Fight ended: {(eventArgs.WasWon ? "win" : "loss")}. " +
            "Waiting for the outcome action presentation before leaving the battle scene.",
            this);
    }

    private void PublishFightEnded()
    {
        if (_pendingFightEnd == null || _hasPublishedFightEnded)
        {
            return;
        }

        _hasPublishedFightEnded = true;
        FightEnded?.Invoke(this, _pendingFightEnd);
    }

    private void EndFight(bool wasWon)
    {
        if (_runManager == null)
        {
            Debug.LogError("Battle scene controller has not been connected to a RunManager.", this);
            return;
        }

        _isCompletingFight = _runManager.EndFight(this, wasWon);
    }
}
