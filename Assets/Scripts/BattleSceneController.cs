using ANTK.Game.StateEngine;
using ANTK.Game.StateEngine.StatusSystem;
using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class BattleSceneController : MonoBehaviour
{
    [SerializeField] private Player scenePlayer;
    [SerializeField] private Enemy sceneEnemy;
    [SerializeField] private PoisonStatusDefinition poisonDefinition;

    private RunManager _runManager;
    private FightManager _fightManager;
    private readonly PresentationEventQueue _presentationQueue = new PresentationEventQueue();
    private PresentationDirector _presentationDirector;
    private PresentationPlayer _presentationPlayer;
    private FightEndedEventArgs _pendingFightEnd;
    private bool _isCompletingFight;

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

        if (player == null || sceneEnemy == null || poisonDefinition == null)
        {
            Debug.LogError("Battle test requires a player, enemy, and poison definition.", this);
            return;
        }

        LivingMixin playerState = player.GetComponent<LivingMixin>();
        LivingMixin enemyState = sceneEnemy.GetComponent<LivingMixin>();
        if (playerState == null || enemyState == null)
        {
            Debug.LogError("Player and enemy must each have a LivingMixin component.", this);
            return;
        }

        if (runManager.RegisterBattleSceneController(this))
        {
            _runManager = runManager;
            _fightManager = new FightManager(
                playerState,
                enemyState,
                runManager.StateEngine,
                poisonDefinition);
            _fightManager.FightEnded += OnFightEnded;
            _fightManager.ActionSnapshotCreated += OnActionSnapshotCreated;
            _fightManager.StartFight();
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
        if (_presentationPlayer != null)
        {
            _presentationPlayer.SnapshotPlaybackCompleted -= OnSnapshotPlaybackCompleted;
        }
    }

    private void OnGUI()
    {
        if (_fightManager == null)
        {
            return;
        }

        GUILayout.BeginArea(new Rect(12f, 12f, 360f, 300f), "StateEngine fight test", GUI.skin.window);
        GUILayout.Label($"Active turn: {_fightManager.CurrentActor.name}");
        GUILayout.Label(
            $"Player: {_fightManager.PlayerState.name} " +
            $"{_fightManager.PlayerState.health}/{_fightManager.PlayerState.maxHealth} HP " +
            $"| Attack {_fightManager.PlayerState.AttackPower.CalculateValue()} " +
            $"| [{GetStatuses(_fightManager.PlayerState)}]");
        GUILayout.Label(
            $"Enemy: {_fightManager.EnemyState.name} " +
            $"{_fightManager.EnemyState.health}/{_fightManager.EnemyState.maxHealth} HP " +
            $"| Attack {_fightManager.EnemyState.AttackPower.CalculateValue()} " +
            $"| [{GetStatuses(_fightManager.EnemyState)}]");
        GUILayout.Space(8f);

        if (_fightManager.IsFightOver)
        {
            GUILayout.Label("Fight ended. Waiting for presentation to finish.");
        }
        else if (_fightManager.IsPresentationPending)
        {
            GUILayout.Label("Waiting for action presentation to finish.");
        }
        else if (_fightManager.IsTurnStarted && ReferenceEquals(_fightManager.CurrentActor, _fightManager.PlayerState))
        {
            if (GUILayout.Button("Apply poison to opponent"))
            {
                _fightManager.ApplyPoisonToOpponent();
            }
            if (GUILayout.Button($"Deal {_fightManager.CurrentActor.AttackPower.CalculateValue()} damage to opponent"))
            {
                _fightManager.DamageOpponent();
            }
            if (GUILayout.Button("End player turn"))
            {
                _fightManager.EndPlayerTurn();
            }
        }
        else
        {
            GUILayout.Label("Enemy turn is resolving automatically.");
        }
        GUILayout.EndArea();
    }

    private static string GetStatuses(LivingMixin entity)
    {
        System.Collections.Generic.IReadOnlyList<ANTK.Game.StateEngine.StatusSystem.StatusInstance> statuses =
            ((IStateEntity)entity).ActiveStatuses;
        if (statuses.Count == 0)
        {
            return "none";
        }

        string result = string.Empty;
        foreach (ANTK.Game.StateEngine.StatusSystem.StatusInstance status in statuses)
        {
            if (result.Length > 0)
            {
                result += ", ";
            }

            string duration = status.IsPermanent
                ? "permanent"
                : $"{status.DurationTurns} turn(s)";
            result += $"{status.Definition.Name} x{status.Stacks} ({duration})";
        }

        return result;
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
        if (_pendingFightEnd == null || _isCompletingFight)
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

    private async void StartPresentationPlayback()
    {
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
        Debug.Log(
            $"Fight ended: {(eventArgs.WasWon ? "win" : "loss")}. " +
            "Waiting for presentation before leaving the battle scene.",
            this);
        FightEnded?.Invoke(this, eventArgs);
    }

    private void EndFight(bool wasWon)
    {
        if (_runManager == null)
        {
            Debug.LogError("Battle scene controller has not been connected to a RunManager.", this);
            return;
        }

        _runManager.EndFight(this, wasWon);
    }
}
