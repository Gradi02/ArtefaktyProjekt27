using System;
using System.Collections.Generic;
using ANTK.Game.StateEngine.Actions;
using ANTK.Game.StateEngine.Pipeline;
using ANTK.Game.StateEngine.StatusSystem;

namespace ANTK.Game.StateEngine
{
    public sealed class StateEngine
    {
        private readonly EffectPipeline _effectPipeline;

        public event Action<GameAction> ActionExecuted;

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
            ValidateEntityIdentity(target);
            ValidateStatusIdentity(definition);

            foreach (StatusInstance activeStatus in target.ActiveStatuses)
            {
                if (ReferenceEquals(activeStatus.Definition, definition))
                {
                    activeStatus.AddStacks(stacks);
                    activeStatus.RefreshDuration(durationTurns);
                    RecordAction(new StatusAppliedAction(
                        target.EntityId,
                        definition.Id,
                        stacks,
                        activeStatus.Stacks,
                        activeStatus.DurationTurns));
                    return activeStatus;
                }
            }

            StatusInstance instance = new StatusInstance(definition, stacks, durationTurns);
            target.AddStatus(instance);
            RecordAction(new StatusAppliedAction(
                target.EntityId,
                definition.Id,
                stacks,
                instance.Stacks,
                instance.DurationTurns));
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
            ValidateEntityIdentity(target);
            ValidateStatusIdentity(status.Definition);

            bool removed = target.RemoveStatus(status);
            if (removed)
            {
                RecordAction(new StatusRemovedAction(target.EntityId, status.Definition.Id));
            }

            return removed;
        }

        public void StartTurn(IStateEntity owner)
        {
            ValidateOwner(owner);
            ValidateEntityIdentity(owner);
            RecordAction(new TurnStartedAction(owner.EntityId));
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
            ValidateEntityIdentity(owner);
            RecordAction(new TurnEndedAction(owner.EntityId));
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
                    RemoveStatus(owner, status);
                }
            }
        }

        public EffectContext ProcessDamage(
            IStateEntity source,
            IStateEntity target,
            float baseDamage,
            string reasonId = "effect")
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }
            if (reasonId == null)
            {
                throw new ArgumentNullException(nameof(reasonId));
            }
            ValidateEntityIdentity(source);
            ValidateEntityIdentity(target);

            int previousHealth = target.Health;
            EffectContext context = _effectPipeline.ProcessDamage(this, source, target, baseDamage);
            RecordAction(new DamageResolvedAction(
                source.EntityId,
                target.EntityId,
                reasonId,
                baseDamage,
                Math.Max(0, previousHealth - target.Health),
                target.Health));
            return context;
        }

        public void RecordExecutedAction(GameAction action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            RecordAction(action);
        }

        private void RecordAction(GameAction action)
        {
            ActionExecuted?.Invoke(action);
        }

        private static void ValidateOwner(IStateEntity owner)
        {
            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner));
            }
        }

        private static void ValidateEntityIdentity(IStateEntity entity)
        {
            if (string.IsNullOrWhiteSpace(entity.EntityId))
            {
                throw new InvalidOperationException("State entities must have a non-empty EntityId.");
            }
        }

        private static void ValidateStatusIdentity(StatusDefinition definition)
        {
            if (string.IsNullOrWhiteSpace(definition.Id))
            {
                throw new InvalidOperationException("Status definitions must have a non-empty ID.");
            }
        }
    }
}
