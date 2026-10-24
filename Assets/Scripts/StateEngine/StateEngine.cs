using System;
using System.Collections.Generic;
using CardRogue.StateEngine.Pipeline;
using CardRogue.StateEngine.StatusSystem;

namespace CardRogue.StateEngine
{
    public sealed class StateEngine
    {
        private readonly EffectPipeline _effectPipeline;

        public StateEngine(EffectPipeline effectPipeline)
        {
            _effectPipeline = effectPipeline;
        }

        public StatusInstance ApplyStatus(
            IStateEntity target,
            StatusDefinition definition,
            int stacks = 1,
            int durationTurns = -1)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }
            if (stacks <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(stacks), "Stacks must be positive.");
            }
            if (durationTurns < -1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(durationTurns),
                    "Duration must be -1 (permanent) or a non-negative number of turns.");
            }

            foreach (StatusInstance activeStatus in target.ActiveStatuses)
            {
                if (ReferenceEquals(activeStatus.Definition, definition))
                {
                    activeStatus.AddStacks(stacks);
                    activeStatus.RefreshDuration(durationTurns);
                    return activeStatus;
                }
            }

            StatusInstance instance = new StatusInstance(definition, stacks, durationTurns);
            target.AddStatus(instance);
            return instance;
        }

        public bool RemoveStatus(IStateEntity target, StatusInstance status)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }
            if (status == null)
            {
                throw new ArgumentNullException(nameof(status));
            }

            return target.RemoveStatus(status);
        }

        public void StartTurn(IStateEntity owner)
        {
            ValidateOwner(owner);
            List<StatusInstance> snapshot = new List<StatusInstance>(owner.ActiveStatuses);

            foreach (StatusInstance status in snapshot)
            {
                if (!owner.HasStatus(status))
                {
                    continue;
                }

                IOnTurnStart hook = status.Definition as IOnTurnStart;
                if (hook != null)
                {
                    hook.OnTurnStart(this, owner, status);
                }
            }
        }

        public void EndTurn(IStateEntity owner)
        {
            ValidateOwner(owner);
            List<StatusInstance> snapshot = new List<StatusInstance>(owner.ActiveStatuses);

            foreach (StatusInstance status in snapshot)
            {
                if (!owner.HasStatus(status))
                {
                    continue;
                }

                IOnTurnEnd hook = status.Definition as IOnTurnEnd;
                if (hook != null)
                {
                    hook.OnTurnEnd(this, owner, status);
                }
            }

            // Status durations tick once at the end of their owner's turn.
            foreach (StatusInstance status in snapshot)
            {
                if (owner.HasStatus(status) && status.TickDuration())
                {
                    owner.RemoveStatus(status);
                }
            }
        }

        public EffectContext ProcessDamage(IStateEntity source, IStateEntity target, float baseDamage)
        {
            return _effectPipeline.ProcessDamage(this, source, target, baseDamage);
        }

        private static void ValidateOwner(IStateEntity owner)
        {
            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner));
            }
        }
    }
}
