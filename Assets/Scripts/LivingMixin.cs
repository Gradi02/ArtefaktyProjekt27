using System;
using System.Collections.Generic;
using ANTK.Game.StateEngine;
using ANTK.Game.StateEngine.StatSystem;
using ANTK.Game.StateEngine.StatusSystem;
using UnityEngine;

public class LivingMixin : MonoBehaviour, IStateEntity
{
    private int _health;
    [SerializeField] private int _maxHealth;
    [SerializeField, Min(0)] private float _baseAttackPower = 3f;
    private readonly List<StatusInstance> _activeStatuses = new List<StatusInstance>();

    public StatValue AttackPower { get; private set; }

    public int health
    {
        get => _health;
        private set => _health = value;
    }

    public int maxHealth => _maxHealth;
    public bool isAlive => health > 0;
    public bool isDead => !isAlive;

    int IStateEntity.Health => health;
    IReadOnlyList<StatusInstance> IStateEntity.ActiveStatuses => _activeStatuses;

    public event EventHandler<HealthChangeEventArgs> onHeal;
    public event EventHandler<HealthChangeEventArgs> onHurt;
    public event EventHandler<HealthChangeEventArgs> onDeath;
    public event EventHandler<HealthChangeEventArgs> onHealthChange;


    public HealthChangeEventArgs Heal(int amount)
    {
        if (amount < 0)
        {
            amount = -amount;
            Debug.LogWarning("Heal amount should be above zero!", this);
        }
        return SetHealth(health + Mathf.Abs(amount));
    }

    public HealthChangeEventArgs Hurt(int amount)
    {
        if (amount < 0)
        {
            amount = -amount;
            Debug.LogWarning("Hurt amount should be above zero!", this);
        }
        return SetHealth(health - amount);
    }

    void IStateEntity.ApplyDamage(int amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Damage must not be negative.");
        }

        if (amount > 0)
        {
            Hurt(amount);
        }
    }

    void IStateEntity.AddStatus(StatusInstance status)
    {
        _activeStatuses.Add(status);
    }

    bool IStateEntity.RemoveStatus(StatusInstance status)
    {
        return _activeStatuses.Remove(status);
    }

    bool IStateEntity.HasStatus(StatusInstance status)
    {
        return _activeStatuses.Contains(status);
    }

    public HealthChangeEventArgs FullHeal() => SetHealth(maxHealth);
    public HealthChangeEventArgs Kill() => SetHealth(0);
    
    
    public HealthChangeEventArgs SetHealth(int newHP)
    {
        newHP = Mathf.Clamp(newHP, 0, maxHealth);
        
        if (isDead)
        {
            Debug.LogError("You're trying to set dead's entity HP!", this);
            return new HealthChangeEventArgs(0, 0);
        }
        if (newHP == health)
        {
            Debug.LogError("You're trying to set the same HP to an entity!", this);
            return new HealthChangeEventArgs(health, health);
        }
        
        int previous = health;
        health = newHP;
        HealthChangeEventArgs eventArgs = new HealthChangeEventArgs(previous, health);

        onHealthChange?.Invoke(this, eventArgs);
        
        if (health == 0) onDeath?.Invoke(this, eventArgs);
        else if (eventArgs.delta < 0) onHurt?.Invoke(this, eventArgs);
        else onHeal?.Invoke(this, eventArgs);

        return eventArgs;
    }

    private void OnValidate()
    {
        _maxHealth = Mathf.Max(1, _maxHealth);
    }
    private void Awake()
    {
        _health = _maxHealth;
        AttackPower = new StatValue(_baseAttackPower);
    }
}
