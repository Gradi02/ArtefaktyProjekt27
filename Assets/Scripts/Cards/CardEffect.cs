using System;
using ANTK.Game.StateEngine.StatusSystem;
using UnityEngine;

public enum CardEffectType
{
    Damage,
    ApplyStatus
}

public enum CardEffectTarget
{
    Self,
    Opponent
}

[Serializable]
public sealed class CardEffect
{
    [SerializeField] private CardEffectType _type;
    [SerializeField] private CardEffectTarget _target;
    [SerializeField, Min(0f)] private float _damageAmount;
    [SerializeField] private StatusDefinition _statusDefinition;
    [SerializeField, Min(1)] private int _stacks = 1;
    [SerializeField] private int _durationTurns = -1;

    public CardEffectType Type => _type;
    public CardEffectTarget Target => _target;
    public float DamageAmount => _damageAmount;
    public StatusDefinition StatusDefinition => _statusDefinition;
    public int Stacks => _stacks;
    public int DurationTurns => _durationTurns;

    public CardEffect(
        CardEffectType type,
        CardEffectTarget target,
        float damageAmount = 0f,
        StatusDefinition statusDefinition = null,
        int stacks = 1,
        int durationTurns = -1)
    {
        _type = type;
        _target = target;
        _damageAmount = damageAmount;
        _statusDefinition = statusDefinition;
        _stacks = stacks;
        _durationTurns = durationTurns;
    }

    private CardEffect()
    {
    }
}
