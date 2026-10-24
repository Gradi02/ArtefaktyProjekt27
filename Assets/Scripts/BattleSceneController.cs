using CardRogue.StateEngine;
using CardRogue.StateEngine.StatusSystem;
using UnityEngine;

public sealed class BattleSceneController : MonoBehaviour
{
    [SerializeField] private Player scenePlayer;
    [SerializeField] private Enemy sceneEnemy;
    [SerializeField] private PoisonStatusDefinition poisonDefinition;

    private RunManager _runManager;
    private FightManager _fightManager;
    private FightEndedEventArgs _pendingFightEnd;
    private bool _isCompletingFight;

    public event System.EventHandler<FightEndedEventArgs> FightEnded;

    private void Start()
    {
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
            _fightManager.StartFight();
        }
    }

    private void OnDestroy()
    {
        if (_fightManager != null)
        {
            _fightManager.FightEnded -= OnFightEnded;
            _fightManager.Dispose();
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
        System.Collections.Generic.IReadOnlyList<CardRogue.StateEngine.StatusSystem.StatusInstance> statuses =
            ((IStateEntity)entity).ActiveStatuses;
        if (statuses.Count == 0)
        {
            return "none";
        }

        string result = string.Empty;
        foreach (CardRogue.StateEngine.StatusSystem.StatusInstance status in statuses)
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
