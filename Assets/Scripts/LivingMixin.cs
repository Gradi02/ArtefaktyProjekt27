using System;
using UnityEngine;

public class LivingMixin : MonoBehaviour
{
    private int _health;
    [SerializeField] private int _maxHealth;

    public int health
    {
        get => _health;
        private set => _health = value;
    }

    public int maxHealth => _maxHealth;
    public bool isAlive => health > 0;
    public bool isDead => !isAlive;

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
    }
}
