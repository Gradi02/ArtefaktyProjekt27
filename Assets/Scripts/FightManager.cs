using System;
using System.Collections.Generic;
using ANTK.Game.StateEngine.Actions;
using ANTK.Game.StateEngine;
using ANTK.Game.StateEngine.StatusSystem;
using UnityEngine;

public sealed class FightManager : IDisposable
{
    private readonly LivingMixin _player;
    private readonly LivingMixin _enemy;
    private readonly StateEngine _stateEngine;
    private readonly PoisonStatusDefinition _poisonDefinition;
    private readonly CombatDeck _playerDeck;
    private readonly int _baseCardsDrawnAtTurnStart;
    private readonly ActionSnapshotRecorder _actionRecorder = new ActionSnapshotRecorder();
    private readonly List<GameAction> _pendingOutcomeActions = new List<GameAction>();
    private readonly Queue<string> _queuedCardPlays = new Queue<string>();
    private readonly HashSet<string> _queuedCardPlayIds = new HashSet<string>();
    private bool _isFightOver;
    private bool _isExecutingCommand;
    private string _pendingSnapshotId;

    public LivingMixin CurrentActor { get; private set; }
    public LivingMixin CurrentOpponent => ReferenceEquals(CurrentActor, _player) ? _enemy : _player;
    public LivingMixin PlayerState => _player;
    public LivingMixin EnemyState => _enemy;
    public CombatDeck PlayerDeck => _playerDeck;
    public bool IsTurnStarted { get; private set; }
    public bool IsFightOver => _isFightOver;
    public bool IsPresentationPending { get; private set; }
    public event EventHandler<FightEndedEventArgs> FightEnded;
    public event EventHandler<GameActionSnapshotEventArgs> ActionSnapshotCreated;

    public FightManager(
        LivingMixin player,
        LivingMixin enemy,
        StateEngine stateEngine,
        PoisonStatusDefinition poisonDefinition,
        CardCollection cardCollection,
        int baseCardsDrawnAtTurnStart)
    {
        if (player == null)
        {
            throw new ArgumentNullException(nameof(player));
        }
        if (enemy == null)
        {
            throw new ArgumentNullException(nameof(enemy));
        }
        if (ReferenceEquals(player, enemy))
        {
            throw new ArgumentException("Player and enemy must be different entities.");
        }
        if (stateEngine == null)
        {
            throw new ArgumentNullException(nameof(stateEngine));
        }
        if (poisonDefinition == null)
        {
            throw new ArgumentNullException(nameof(poisonDefinition));
        }
        if (cardCollection == null)
        {
            throw new ArgumentNullException(nameof(cardCollection));
        }
        if (baseCardsDrawnAtTurnStart < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(baseCardsDrawnAtTurnStart));
        }

        _player = player;
        _enemy = enemy;
        _stateEngine = stateEngine;
        _poisonDefinition = poisonDefinition;
        _playerDeck = new CombatDeck(cardCollection, int.MaxValue);
        _baseCardsDrawnAtTurnStart = baseCardsDrawnAtTurnStart;
        CurrentActor = _player;
        _player.onDeath += OnEntityDeath;
        _enemy.onDeath += OnEntityDeath;
        _stateEngine.ActionExecuted += OnActionExecuted;
    }

    public void StartFight()
    {
        if (_isFightOver || IsTurnStarted || IsPresentationPending)
        {
            return;
        }

        ExecuteCommand("fight.start", () =>
        {
            if (!TryFinishFight())
            {
                BeginTurn();
            }
        });
    }

    public void EndPlayerTurn()
    {
        if (_isFightOver || IsPresentationPending ||
            !IsTurnStarted || !ReferenceEquals(CurrentActor, _player))
        {
            Debug.LogWarning("The player's turn cannot be ended in the current state.");
            return;
        }

        ExecuteCommand("player.end-turn", ResolveEndPlayerTurn);
    }

    public bool TryExecutePlayerCommand(string commandTypeId, Action command)
    {
        if (string.IsNullOrWhiteSpace(commandTypeId))
        {
            throw new ArgumentException("Command type ID must not be empty.", nameof(commandTypeId));
        }
        if (command == null)
        {
            throw new ArgumentNullException(nameof(command));
        }
        if (!RequirePlayerTurn())
        {
            return false;
        }

        ExecuteCommand(commandTypeId, command);
        return true;
    }

    public void ApplyPoisonToOpponent()
    {
        if (!RequirePlayerTurn())
        {
            return;
        }

        ExecuteCommand("player.apply-poison", () =>
        {
            _stateEngine.ApplyStatus(
                CurrentOpponent,
                _poisonDefinition,
                durationTurns: _poisonDefinition.DurationTurns);
            Debug.Log(
                $"Poison applied to {CurrentOpponent.name}. " +
                $"Stacks: {GetPoisonStacks(CurrentOpponent)}, " +
                $"duration: {_poisonDefinition.DurationTurns} owner turns.");
        });
    }

    public void DamageOpponent()
    {
        if (!RequirePlayerTurn())
        {
            return;
        }

        ExecuteCommand("player.attack", () =>
        {
            float damage = CurrentActor.AttackPower.CalculateValue();
            _stateEngine.ProcessDamage(CurrentActor, CurrentOpponent, damage, "player.attack");
            Debug.Log(
                $"{CurrentActor.name} dealt {damage} damage to {CurrentOpponent.name}. " +
                $"HP: {CurrentOpponent.health}/{CurrentOpponent.maxHealth}.");
        });
    }

    public bool TryPlayCard(CombatCard card)
    {
        bool queueForPresentation = IsPresentationPending;
        if (queueForPresentation &&
            (_isFightOver || !IsTurnStarted || !ReferenceEquals(CurrentActor, _player)))
        {
            Debug.LogWarning("This action is only available during the player's turn.");
            return false;
        }
        if (!queueForPresentation && !RequirePlayerTurn())
        {
            return false;
        }
        if (card == null || !_playerDeck.Hand.Contains(card))
        {
            Debug.LogWarning("The card is not in the player's hand.");
            return false;
        }
        if (card.Source.Definition.behaviour == null)
        {
            Debug.LogError($"Card '{card.Source.Definition.title}' has no behaviour.");
            return false;
        }

        if (queueForPresentation)
        {
            string cardInstanceId = card.Source.InstanceId;
            if (!_queuedCardPlayIds.Add(cardInstanceId))
            {
                Debug.LogWarning($"Card '{cardInstanceId}' is already queued to be played.");
                return false;
            }

            _queuedCardPlays.Enqueue(cardInstanceId);
            return true;
        }

        ExecuteCommand("player.play-card", () =>
        {
            card.Source.Definition.behaviour.Perform(
                new CardContext(_stateEngine, _player, _enemy, card));
            if (!_playerDeck.Discard(card))
            {
                throw new InvalidOperationException("The played card could not be moved to the discard pile.");
            }
            _stateEngine.RecordExecutedAction(new CardPlayedAction(
                ((IStateEntity)_player).EntityId,
                card.Source.InstanceId,
                card.Source.Definition.Id));
        });
        return true;
    }

    public bool TryPlayCard(string cardInstanceId)
    {
        if (string.IsNullOrWhiteSpace(cardInstanceId))
        {
            throw new ArgumentException("Card instance ID must not be empty.", nameof(cardInstanceId));
        }
        if (!IsPresentationPending && !RequirePlayerTurn())
        {
            return false;
        }
        if (IsPresentationPending &&
            (_isFightOver || !IsTurnStarted || !ReferenceEquals(CurrentActor, _player)))
        {
            Debug.LogWarning("This action is only available during the player's turn.");
            return false;
        }

        foreach (CombatCard card in _playerDeck.Hand)
        {
            if (card.Source.InstanceId == cardInstanceId)
            {
                return TryPlayCard(card);
            }
        }

        Debug.LogWarning($"Card '{cardInstanceId}' is not in the player's hand.");
        return false;
    }

    public bool AcknowledgePresentationCompleted(string snapshotId)
    {
        if (!IsPresentationPending || snapshotId != _pendingSnapshotId)
        {
            Debug.LogWarning($"Snapshot '{snapshotId}' is not the active presentation.");
            return false;
        }

        _pendingSnapshotId = null;
        IsPresentationPending = false;
        ResolveQueuedCardPlays();
        return true;
    }

    public void Dispose()
    {
        if (_stateEngine != null)
        {
            _stateEngine.ActionExecuted -= OnActionExecuted;
        }
        if (_player != null)
        {
            _player.onDeath -= OnEntityDeath;
        }
        if (_enemy != null)
        {
            _enemy.onDeath -= OnEntityDeath;
        }
    }

    private void BeginTurn()
    {
        if (_isFightOver)
        {
            return;
        }

        IsTurnStarted = true;
        _stateEngine.StartTurn(CurrentActor);
        if (TryFinishFight())
        {
            return;
        }

        if (ReferenceEquals(CurrentActor, _player) && _baseCardsDrawnAtTurnStart > 0)
        {
            IReadOnlyList<CombatCard> drawnCards = _playerDeck.DrawCards(_baseCardsDrawnAtTurnStart);
            foreach (CombatCard card in drawnCards)
            {
                _stateEngine.RecordExecutedAction(new CardDrawnAction(
                    ((IStateEntity)_player).EntityId,
                    card.Source.InstanceId,
                    card.Source.Definition.Id,
                    card.Source.Definition.title,
                    card.Source.Definition.type.ToString(),
                    card.Source.Definition.baseCost.value,
                    "draw_pile",
                    "hand",
                    "turn.start"));
            }
        }

        Debug.Log($"Turn started: {CurrentActor.name} ({CurrentActor.health}/{CurrentActor.maxHealth} HP).");
        TryFinishFight();
    }

    private void EndCurrentTurn()
    {
        if (!IsTurnStarted || _isFightOver)
        {
            return;
        }

        _stateEngine.EndTurn(CurrentActor);
        IsTurnStarted = false;
        Debug.Log($"Turn ended: {CurrentActor.name} ({CurrentActor.health}/{CurrentActor.maxHealth} HP).");
        TryFinishFight();
    }

    private void PerformEnemyAction()
    {
        if (_isFightOver)
        {
            return;
        }

        float damage = _enemy.AttackPower.CalculateValue();
        _stateEngine.ProcessDamage(_enemy, _player, damage, "enemy.attack");
        Debug.Log(
            $"{_enemy.name} dealt {damage} damage to {_player.name}. " +
            $"HP: {_player.health}/{_player.maxHealth}.");
        TryFinishFight();
    }

    private bool RequirePlayerTurn()
    {
        if (_isFightOver || IsPresentationPending ||
            !IsTurnStarted || !ReferenceEquals(CurrentActor, _player))
        {
            Debug.LogWarning("This action is only available during the player's turn.");
            return false;
        }

        return true;
    }

    private void ResolveQueuedCardPlays()
    {
        while (!IsPresentationPending && !_isFightOver && _queuedCardPlays.Count > 0)
        {
            string cardInstanceId = _queuedCardPlays.Dequeue();
            _queuedCardPlayIds.Remove(cardInstanceId);
            if (TryPlayCard(cardInstanceId))
            {
                return;
            }
        }
    }

    private void ResolveEndPlayerTurn()
    {
        EndCurrentTurn();
        if (_isFightOver)
        {
            return;
        }

        CurrentActor = _enemy;
        BeginTurn();
        if (_isFightOver)
        {
            return;
        }

        PerformEnemyAction();
        if (_isFightOver)
        {
            return;
        }

        EndCurrentTurn();
        if (_isFightOver)
        {
            return;
        }

        CurrentActor = _player;
        BeginTurn();
    }

    private void ExecuteCommand(string commandTypeId, Action command)
    {
        if (_isExecutingCommand)
        {
            throw new InvalidOperationException("Fight commands cannot be nested.");
        }

        _isExecutingCommand = true;
        _actionRecorder.BeginCommand(commandTypeId);
        try
        {
            command();
        }
        finally
        {
            _isExecutingCommand = false;
            foreach (GameAction action in _pendingOutcomeActions)
            {
                _stateEngine.RecordExecutedAction(action);
            }
            _pendingOutcomeActions.Clear();

            GameActionSnapshot snapshot = _actionRecorder.CompleteCommand();
            if (snapshot != null)
            {
                _pendingSnapshotId = snapshot.SnapshotId;
                IsPresentationPending = true;
                ActionSnapshotCreated?.Invoke(this, new GameActionSnapshotEventArgs(snapshot));
            }
        }
    }

    private void OnActionExecuted(GameAction action)
    {
        _actionRecorder.Record(action);
    }

    private int GetPoisonStacks(LivingMixin target)
    {
        foreach (StatusInstance status in ((IStateEntity)target).ActiveStatuses)
        {
            if (ReferenceEquals(status.Definition, _poisonDefinition))
            {
                return status.Stacks;
            }
        }

        return 0;
    }

    private void OnEntityDeath(object sender, HealthChangeEventArgs eventArgs)
    {
        if (_isFightOver)
        {
            return;
        }

        LivingMixin defeated = sender as LivingMixin;
        if (defeated == null)
        {
            return;
        }

        FinishFight(ReferenceEquals(defeated, _enemy), defeated);
    }

    private bool TryFinishFight()
    {
        if (_isFightOver)
        {
            return true;
        }
        if (_player.health <= 0)
        {
            FinishFight(false, _player);
            return true;
        }
        if (_enemy.health <= 0)
        {
            FinishFight(true, _enemy);
            return true;
        }

        return false;
    }

    private void FinishFight(bool wasWon, LivingMixin defeated)
    {
        if (_isFightOver)
        {
            return;
        }

        if (!_isExecutingCommand)
        {
            ExecuteCommand("fight.entity-defeated", () => FinishFight(wasWon, defeated));
            return;
        }

        _isFightOver = true;
        IsTurnStarted = false;
        IStateEntity defeatedState = defeated;
        if (wasWon)
        {
            _pendingOutcomeActions.Add(new OpponentKilledAction(((IStateEntity)_enemy).EntityId));
            _pendingOutcomeActions.Add(
                new FightWonAction(((IStateEntity)_player).EntityId, defeatedState.EntityId));
        }
        else
        {
            _pendingOutcomeActions.Add(new PlayerDiedAction(((IStateEntity)_player).EntityId));
        }

        FightEnded?.Invoke(this, new FightEndedEventArgs(wasWon, defeated));
    }
}
