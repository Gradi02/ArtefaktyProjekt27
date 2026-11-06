using System;
using System.Collections.Generic;
using ANTK.Game.StateEngine;
using UnityEngine;

[CreateAssetMenu(fileName = "Data Driven Card Behaviour", menuName = "ANTK/Cards/Data Driven Behaviour")]
public sealed class DataDrivenCardBehaviour : CardBehaviour
{
    [SerializeField] private List<CardEffect> _effects = new List<CardEffect>();

    public override void Perform(CardContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }
        if (_effects == null || _effects.Count == 0)
        {
            throw new InvalidOperationException("Data-driven card behavior must contain at least one effect.");
        }

        foreach (CardEffect effect in _effects)
        {
            if (effect == null)
            {
                throw new InvalidOperationException("Data-driven card behavior cannot contain a null effect.");
            }

            LivingMixin target = ResolveTarget(context, effect.Target);
            switch (effect.Type)
            {
                case CardEffectType.Damage:
                    if (effect.DamageAmount <= 0f)
                    {
                        throw new InvalidOperationException("Damage effects must have a positive amount.");
                    }

                    context.StateEngine.ProcessDamage(
                        (IStateEntity)context.Source,
                        (IStateEntity)target,
                        effect.DamageAmount,
                        "card.effect.damage");
                    break;

                case CardEffectType.ApplyStatus:
                    if (!effect.StatusDefinition)
                    {
                        throw new InvalidOperationException("Status effects require a status definition.");
                    }
                    if (effect.Stacks <= 0)
                    {
                        throw new InvalidOperationException("Status effects must apply at least one stack.");
                    }
                    if (effect.DurationTurns < -1)
                    {
                        throw new InvalidOperationException(
                            "Status duration must be -1 (permanent) or a non-negative number of turns.");
                    }

                    context.StateEngine.ApplyStatus(
                        (IStateEntity)target,
                        effect.StatusDefinition,
                        effect.Stacks,
                        effect.DurationTurns);
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported card effect type: {effect.Type}.");
            }
        }
    }

    private static LivingMixin ResolveTarget(CardContext context, CardEffectTarget target)
    {
        switch (target)
        {
            case CardEffectTarget.Self:
                return context.Source;
            case CardEffectTarget.Opponent:
                return context.Target;
            default:
                throw new InvalidOperationException($"Unsupported card effect target: {target}.");
        }
    }
}
