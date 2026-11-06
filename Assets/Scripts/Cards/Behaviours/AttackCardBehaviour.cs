using System;
using ANTK.Game.StateEngine;
using ANTK.Game.StateEngine.StatusSystem;
using UnityEngine;

[CreateAssetMenu(fileName = "Attack Card Behaviour", menuName = "ANTK/Cards/Attack Behaviour")]
public class AttackCardBehaviour : CardBehaviour
{
    [SerializeField] private bool _useSourceAttackPower = true;
    [SerializeField, Min(0f)] private float _damageAmount;
    [SerializeField] private StatusDefinition _statusDefinition;
    [SerializeField, Min(1)] private int _statusStacks = 1;
    [SerializeField] private int _statusDurationTurns = -1;

    public override void Perform(CardContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }


        float damageAmount = _useSourceAttackPower
            ? context.Source.AttackPower.CalculateValue()
            : _damageAmount;
        if (float.IsNaN(damageAmount) || float.IsInfinity(damageAmount) || damageAmount < 0f)
        {
            throw new InvalidOperationException("Attack damage must be finite and non-negative.");
        }

        if (damageAmount > 0f)
        {
            context.StateEngine.ProcessDamage(
                (IStateEntity)context.Source,
                (IStateEntity)context.Target,
                damageAmount,
                "card.attack");
        }

        if (_statusDefinition)
        {
            context.StateEngine.ApplyStatus(
                (IStateEntity)context.Target,
                _statusDefinition,
                _statusStacks,
                _statusDurationTurns);
        }
    }
}