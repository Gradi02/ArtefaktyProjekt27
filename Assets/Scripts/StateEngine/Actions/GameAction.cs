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
        public string CardTitle { get; }
        public string CardType { get; }
        public int CardCost { get; }
        public string SourceZoneId { get; }
        public string DestinationZoneId { get; }
        public string ReasonId { get; }

        public CardDrawnAction(
            string recipientEntityId,
            string cardInstanceId,
            string cardDefinitionId,
            string cardTitle,
            string cardType,
            int cardCost,
            string sourceZoneId,
            string destinationZoneId,
            string reasonId)
        {
            RecipientEntityId = TurnStartedAction.RequireValue(recipientEntityId, nameof(recipientEntityId));
            CardInstanceId = TurnStartedAction.RequireValue(cardInstanceId, nameof(cardInstanceId));
            CardDefinitionId = TurnStartedAction.RequireValue(cardDefinitionId, nameof(cardDefinitionId));
            CardTitle = TurnStartedAction.RequireValue(cardTitle, nameof(cardTitle));
            CardType = TurnStartedAction.RequireValue(cardType, nameof(cardType));
            if (cardCost < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cardCost));
            }
            CardCost = cardCost;
            SourceZoneId = TurnStartedAction.RequireValue(sourceZoneId, nameof(sourceZoneId));
            DestinationZoneId = TurnStartedAction.RequireValue(destinationZoneId, nameof(destinationZoneId));
            ReasonId = TurnStartedAction.RequireValue(reasonId, nameof(reasonId));
        }
    }

    public sealed class CardPlayedAction : GameAction
    {
        public override string TypeId => "card.played";
        public string PlayerEntityId { get; }
        public string CardInstanceId { get; }
        public string CardDefinitionId { get; }

        public CardPlayedAction(string playerEntityId, string cardInstanceId, string cardDefinitionId)
        {
            PlayerEntityId = TurnStartedAction.RequireValue(playerEntityId, nameof(playerEntityId));
            CardInstanceId = TurnStartedAction.RequireValue(cardInstanceId, nameof(cardInstanceId));
            CardDefinitionId = TurnStartedAction.RequireValue(cardDefinitionId, nameof(cardDefinitionId));
        }
    }

    public sealed class HexCommandResolvedAction : GameAction
    {
        public override string TypeId => "hex.command-resolved";
        public string EntityId { get; }
        public string CommandName { get; }
        public string Result { get; }
        public int FromQ { get; }
        public int FromR { get; }
        public int ToQ { get; }
        public int ToR { get; }
        public int FromFacing { get; }
        public int ToFacing { get; }

        public HexCommandResolvedAction(
            string entityId,
            string commandName,
            string result,
            int fromQ,
            int fromR,
            int toQ,
            int toR,
            int fromFacing,
            int toFacing)
        {
            EntityId = TurnStartedAction.RequireValue(entityId, nameof(entityId));
            CommandName = TurnStartedAction.RequireValue(commandName, nameof(commandName));
            Result = TurnStartedAction.RequireValue(result, nameof(result));
            if (fromFacing < 0 || fromFacing >= 6)
            {
                throw new ArgumentOutOfRangeException(nameof(fromFacing));
            }
            if (toFacing < 0 || toFacing >= 6)
            {
                throw new ArgumentOutOfRangeException(nameof(toFacing));
            }

            FromQ = fromQ;
            FromR = fromR;
            ToQ = toQ;
            ToR = toR;
            FromFacing = fromFacing;
            ToFacing = toFacing;
        }
    }

    public sealed class OpponentKilledAction : GameAction
    {
        public override string TypeId => "opponent.killed";
        public string EntityId { get; }

        public OpponentKilledAction(string entityId)
        {
            EntityId = TurnStartedAction.RequireValue(entityId, nameof(entityId));
        }
    }

    public sealed class FightWonAction : GameAction
    {
        public override string TypeId => "fight.won";
        public string WinnerEntityId { get; }
        public string DefeatedEntityId { get; }

        public FightWonAction(string winnerEntityId, string defeatedEntityId)
        {
            WinnerEntityId = TurnStartedAction.RequireValue(winnerEntityId, nameof(winnerEntityId));
            DefeatedEntityId = TurnStartedAction.RequireValue(defeatedEntityId, nameof(defeatedEntityId));
        }
    }

    public sealed class PlayerDiedAction : GameAction
    {
        public override string TypeId => "player.died";
        public string EntityId { get; }

        public PlayerDiedAction(string entityId)
        {
            EntityId = TurnStartedAction.RequireValue(entityId, nameof(entityId));
        }
    }
}
