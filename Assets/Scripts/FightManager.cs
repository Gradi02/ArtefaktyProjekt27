using System;
using ANTK.Game.StateEngine;
using ANTK.Game.StateEngine.StatusSystem;
using UnityEngine;

public sealed class FightManager : IDisposable
{
    private readonly LivingMixin _player;
    private readonly LivingMixin _enemy;
    private readonly StateEngine _stateEngine;
    private readonly PoisonStatusDefinition _poisonDefinition;
    private bool _isFightOver;

    public LivingMixin CurrentActor { get; private set; }
    public LivingMixin CurrentOpponent => ReferenceEquals(CurrentActor, _player) ? _enemy : _player;
    public LivingMixin PlayerState => _player;
    public LivingMixin EnemyState => _enemy;
    public bool IsTurnStarted { get; private set; }
    public bool IsFightOver => _isFightOver;
    public event EventHandler<FightEndedEventArgs> FightEnded;

    public FightManager(
        LivingMixin player,
        LivingMixin enemy,
        StateEngine stateEngine,
        PoisonStatusDefinition poisonDefinition)
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

        _player = player;
        _enemy = enemy;
        _stateEngine = stateEngine;
        _poisonDefinition = poisonDefinition;
        CurrentActor = _player;
        _player.onDeath += OnEntityDeath;
        _enemy.onDeath += OnEntityDeath;
    }

    public void StartFight()
    {
        if (_isFightOver || IsTurnStarted)
        {
            return;
        }

        if (TryFinishFight())
        {
            return;
        }

        BeginTurn();
    }

    public void EndPlayerTurn()
    {
        if (_isFightOver || !IsTurnStarted || !ReferenceEquals(CurrentActor, _player))
        {
            Debug.LogWarning("Only the active player's turn can be ended manually.");
            return;
        }

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

    public void ApplyPoisonToOpponent()
    {
        if (!RequirePlayerTurn())
        {
            return;
        }

        _stateEngine.ApplyStatus(
            CurrentOpponent,
            _poisonDefinition,
            durationTurns: _poisonDefinition.DurationTurns);
        Debug.Log(
            $"Poison applied to {CurrentOpponent.name}. " +
            $"Stacks: {GetPoisonStacks(CurrentOpponent)}, " +
            $"duration: {_poisonDefinition.DurationTurns} owner turns.");
    }

    public void DamageOpponent()
    {
        if (!RequirePlayerTurn())
        {
            return;
        }

        float damage = CurrentActor.AttackPower.CalculateValue();
        _stateEngine.ProcessDamage(CurrentActor, CurrentOpponent, damage);
        Debug.Log(
            $"{CurrentActor.name} dealt {damage} damage to {CurrentOpponent.name}. " +
            $"HP: {CurrentOpponent.health}/{CurrentOpponent.maxHealth}.");
    }

    public void Dispose()
    {
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
        _stateEngine.ProcessDamage(_enemy, _player, damage);
        Debug.Log(
            $"{_enemy.name} dealt {damage} damage to {_player.name}. " +
            $"HP: {_player.health}/{_player.maxHealth}.");
        TryFinishFight();
    }

    private bool RequirePlayerTurn()
    {
        if (_isFightOver || !IsTurnStarted || !ReferenceEquals(CurrentActor, _player))
        {
            Debug.LogWarning("This action is only available during the player's turn.");
            return false;
        }

        return true;
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

        _isFightOver = true;
        IsTurnStarted = false;
        FightEnded?.Invoke(this, new FightEndedEventArgs(wasWon, defeated));
    }
}
