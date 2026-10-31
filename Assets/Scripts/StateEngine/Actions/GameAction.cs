using System;

namespace ANTK.Game.StateEngine.Actions
{
    public abstract class GameAction
    {
        public abstract string TypeId { get; }
    }

    public sealed class TurnStartedAction : GameAction
    {
        public override string TypeId => "turn.started";
        public string EntityId { get; }

        public TurnStartedAction(string entityId)
        {
            EntityId = RequireValue(entityId, nameof(entityId));
        }

        internal static string RequireValue(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value must not be empty.", parameterName);
            }

            return value;
        }
    }

    public sealed class TurnEndedAction : GameAction
    {
        public override string TypeId => "turn.ended";
        public string EntityId { get; }

        public TurnEndedAction(string entityId)
        {
            EntityId = TurnStartedAction.RequireValue(entityId, nameof(entityId));
        }
    }

    public sealed class StatusAppliedAction : GameAction
    {
        public override string TypeId => "status.applied";
        public string EntityId { get; }
        public string StatusId { get; }
        public int AddedStacks { get; }
        public int TotalStacks { get; }
        public int DurationTurns { get; }

        public StatusAppliedAction(
            string entityId,
            string statusId,
            int addedStacks,
            int totalStacks,
            int durationTurns)
        {
            EntityId = TurnStartedAction.RequireValue(entityId, nameof(entityId));
            StatusId = TurnStartedAction.RequireValue(statusId, nameof(statusId));
            if (addedStacks <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(addedStacks));
            }
            if (totalStacks < addedStacks)
            {
                throw new ArgumentOutOfRangeException(nameof(totalStacks));
            }
            if (durationTurns < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(durationTurns));
            }

            AddedStacks = addedStacks;
            TotalStacks = totalStacks;
            DurationTurns = durationTurns;
        }
    }

    public sealed class StatusRemovedAction : GameAction
    {
        public override string TypeId => "status.removed";
        public string EntityId { get; }
        public string StatusId { get; }

        public StatusRemovedAction(string entityId, string statusId)
        {
            EntityId = TurnStartedAction.RequireValue(entityId, nameof(entityId));
            StatusId = TurnStartedAction.RequireValue(statusId, nameof(statusId));
        }
    }

    public sealed class DamageResolvedAction : GameAction
    {
        public override string TypeId => "damage.resolved";
        public string SourceEntityId { get; }
        public string TargetEntityId { get; }
        public string ReasonId { get; }
        public float RequestedAmount { get; }
        public int AppliedAmount { get; }
        public int RemainingHealth { get; }

        public DamageResolvedAction(
            string sourceEntityId,
            string targetEntityId,
            string reasonId,
            float requestedAmount,
            int appliedAmount,
            int remainingHealth)
        {
            SourceEntityId = TurnStartedAction.RequireValue(sourceEntityId, nameof(sourceEntityId));
            TargetEntityId = TurnStartedAction.RequireValue(targetEntityId, nameof(targetEntityId));
            ReasonId = reasonId ?? throw new ArgumentNullException(nameof(reasonId));
            if (appliedAmount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(appliedAmount));
            }
            if (remainingHealth < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(remainingHealth));
            }

            RequestedAmount = requestedAmount;
            AppliedAmount = appliedAmount;
            RemainingHealth = remainingHealth;
        }
    }

    public sealed class CardDrawnAction : GameAction
    {
        public override string TypeId => "card.drawn";
        public string RecipientEntityId { get; }
        public string CardInstanceId { get; }
        public string CardDefinitionId { get; }
        public string SourceZoneId { get; }
        public string DestinationZoneId { get; }
        public string ReasonId { get; }

        public CardDrawnAction(
            string recipientEntityId,
            string cardInstanceId,
            string cardDefinitionId,
            string sourceZoneId,
            string destinationZoneId,
            string reasonId)
        {
            RecipientEntityId = TurnStartedAction.RequireValue(recipientEntityId, nameof(recipientEntityId));
            CardInstanceId = TurnStartedAction.RequireValue(cardInstanceId, nameof(cardInstanceId));
            CardDefinitionId = TurnStartedAction.RequireValue(cardDefinitionId, nameof(cardDefinitionId));
            SourceZoneId = TurnStartedAction.RequireValue(sourceZoneId, nameof(sourceZoneId));
            DestinationZoneId = TurnStartedAction.RequireValue(destinationZoneId, nameof(destinationZoneId));
            ReasonId = TurnStartedAction.RequireValue(reasonId, nameof(reasonId));
        }
    }
}
